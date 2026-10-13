#!/usr/bin/env python3
"""Classify the differences of bundle sjl-4-001's reference-arm comparison against the reference.

    python3 scripts/conformance/classify_numerics_arms.py --run <comparison output directory> \
        --population <population directory> [--corpus conformance/vectors/numerics] [--jobs N] [--examples 20]

The comparison (Broiler.ECMAScript.Algorithms.Tests, run as `compare-numerics`) classifies nothing: it
lists every input on which a variant of an arm differs from S called as that arm's engine calls it.
This script is the owner's classification. It was written before the judged run and is applied
mechanically, so that every class can be checked against the reference value line by line. The arbiter
is numericsref.py, the corpus's exact reference computation; the agreement of the arms decides nothing.
The rule is docs/evidence/sjl-4-001/reference-arms.md, as revised on 2026-10-13
(reference-arms-revised-2026-10-13.md).

For each difference of one variant from S:

- the reference computes the specification's result for the input, under the policy S ran at
  (Broiler.VM's 2^20 bits against arm V, the representation's 2^31 - 1 bits against arm J), and
  composed with the step the variant's engine applies, where the comparison composed S with it;
- a result S and the arm both carry a charge for (arm V's BigInt variants) is judged on its value; when
  the values agree and the charges do not, the difference is unclassified, because the rule makes a
  charge of S that differs from arm V's class (c);
- for an implementation-approximated function (Number::exponentiate's last step, Math.hypot,
  Math.cbrt, Math.expm1, Math.log1p) S is admitted within its documented bound: cbrt correctly
  rounded; hypot, expm1 and log1p within one unit in the last place; a power's last step is the
  platform's Math.Pow, of which S documents no bound, so it is admitted as an arm's is. An arm's
  result is admitted when it is the clause's special case where there is one, and otherwise any finite
  result of the exact value's sign, as the rule's class (b) says; but an arm's expm1 or log1p for
  |x| below 2^-20 is admitted only within one unit in the last place (the rule's class (a));
- a Float16 NaN is written under the arm's engine's policy, which the comparison already applied;
- class (a), a correction: S's result is admitted and the arm's is not. An arm's exception is not
  admitted;
- class (b), implementation-defined: both are admitted. It is "in a vector" when the numerics corpus
  holds a vector of the same operation of class impl-defined, or whose expected outcome admits more
  than one result. The vector's id and revision are written beside it. A class (b) difference that no
  vector covers is unclassified;
- otherwise unclassified: S's result is not admitted (class (c), a defect of S, which is fixed and the
  whole comparison run again), or the reference cannot judge the input.

Outputs, in the run directory: classification.tsv.gz holds every difference with its input, S's
result, the arm's result, the reference, the class and the reason; an input or result longer than 256
characters is shown by its length. report.md gives per operation, variant, class and reason the count
and the first --examples differences in full. classification.json gives the counts and the SHA-256 of
both listings' uncompressed text.
"""

from __future__ import annotations

import argparse
import gzip
import hashlib
import json
import multiprocessing
import re
import sys
from collections import Counter, defaultdict
from fractions import Fraction
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import numericsref as N  # noqa: E402
import numref as R  # noqa: E402

sys.set_int_max_str_digits(0)

VM_BITS = 1 << 20
UNBOUNDED_BITS = (1 << 31) - 1
SHOWN = 256
NEAR_ZERO = Fraction(1, 1 << 20)

MODES = ["ceil", "floor", "expand", "trunc", "halfCeil", "halfFloor", "halfExpand", "halfTrunc", "halfEven"]
UNSIGNED = ["zero", "infinity", "half-zero", "half-infinity", "half-even"]
STR_WHITE_SPACE = [0x0009, 0x000B, 0x000C, 0x0020, 0x00A0, 0xFEFF, 0x1680, *range(0x2000, 0x200B), 0x202F, 0x205F,
                   0x3000, 0x000A, 0x000D, 0x2028, 0x2029]

# The population's B1V and B1C, loaded once in the parent and inherited by the workers.
VALUES: list[int] = []
COMPARED: list[int] = []


# --------------------------------------------------------------------------------------------------
# The comparison's rendering, mirrored.

def f64(bits: int) -> str:
    return "NaN" if R.is_nan(bits) else f"f64:{bits:016X}"


def f16(bits: int) -> str:
    return f"f16:{bits:04X}"


def integer(value: int) -> str:
    return f"int:{value}"


def number(value: int | None, infinite: int = 0) -> str:
    if infinite:
        return "num:+Infinity" if infinite > 0 else "num:-Infinity"
    return f"num:{value}"


def big(value: int) -> str:
    if value == 0:
        return "bigint:0"
    return ("bigint:-" if value < 0 else "bigint:") + format(abs(value), "x")


RANGE_ERROR = "error:RangeError"
UNDEFINED = "undefined"


def unescape(text: str) -> str:
    return re.sub(r"\\u([0-9A-F]{4})", lambda m: chr(int(m.group(1), 16)), text)


def field(text: str) -> tuple[str, str | None]:
    """A listed field: its text, or its length-and-digest form when the comparison shortened it."""
    if text.startswith("len:") and ":sha256:" in text:
        return text, None
    return text, unescape(text)


def matches(rendered: str, listed: str) -> bool:
    """Whether a rendering is the listed field, whole or by length and digest."""
    shown, text = field(listed)
    if text is not None:
        return rendered == text
    length, digest = re.fullmatch(r"len:(\d+):sha256:([0-9a-f]+)", shown).groups()
    return len(rendered) == int(length) and hashlib.sha256(rendered.encode("utf-8")).hexdigest() == digest


def charged(listed: str) -> tuple[str, str | None]:
    """A result and its charge, when the field carries one ("value|charge:N")."""
    shown, text = field(listed)
    source = text if text is not None else shown
    if "|charge:" in source:
        value, _, charge = source.rpartition("|charge:")
        return value, charge
    return source, None


def signed_hex(text: str) -> int:
    return -int(text[1:], 16) if text.startswith("-") else int(text, 16)


# --------------------------------------------------------------------------------------------------
# Admission.

class Verdict:
    """What the reference admits for S and for the arm: an exact rendering, or a predicate."""

    def __init__(self, shown: str, s_admits, arm_admits=None, exact: str | None = None) -> None:
        self.shown = shown
        self.s_admits = s_admits
        self.arm_admits = arm_admits or s_admits
        self.exact = exact


def exactly(rendering: str) -> Verdict:
    return Verdict(rendering if len(rendering) <= SHOWN else f"({len(rendering)} characters)",
                   lambda value: value == rendering, exact=rendering)


def approximated(result, s_bound: str, near_zero: bool = False) -> Verdict:
    """An implementation-approximated function's result: an exact special case, or the correctly
    rounded double with the faithful pair."""
    if isinstance(result, int):
        return exactly(f64(result))
    correct, faithful = result.correct, result.faithful
    pair = {f64(b) for b in faithful} | {f64(correct)}

    def same_sign_finite(value: str) -> bool:
        if not value.startswith("f64:"):
            return False
        bits = int(value[4:], 16)
        return R.is_finite(bits) and (bits >> 63) == (correct >> 63)

    if s_bound == "correct":
        s_admits = lambda value: value == f64(correct)  # noqa: E731
    elif s_bound == "faithful":
        s_admits = lambda value: value in pair  # noqa: E731
    else:
        s_admits = lambda value: value in pair or same_sign_finite(value)  # noqa: E731
    arm_admits = (lambda value: value in pair) if near_zero else (lambda value: value in pair or same_sign_finite(value))
    return Verdict(f"{f64(correct)} (faithful {', '.join(f64(b) for b in faithful)})", s_admits, arm_admits)


def within(value: int, limit: int) -> str:
    return big(value) if N.bit_width(value) <= limit else RANGE_ERROR


# --------------------------------------------------------------------------------------------------
# The reference, per operation.

def doubles(raw: str) -> list[int]:
    return [] if raw == "-" else [int(b, 16) for b in raw.split(",")]


def toio_number(bits: int) -> str:
    result = N.to_integer_or_infinity(bits)
    if R.is_infinite(result):
        return number(None, -1 if R.is_negative(result) else 1)
    return number(int(R.to_fraction(result)))


def reference(operation: str, variant: str, klass: str, line: int, detail: str, raw: str) -> Verdict | None:
    arm = variant.split(":", 1)[0]
    limit = VM_BITS if arm == "V" else UNBOUNDED_BITS
    head = detail.split("|", 1)[0]

    if klass in ("N1", "N2E", "N2N", "N3E", "N3C", "B1C"):
        x = int(raw, 16)
    widths = {"ToInt32": (32, True), "ToUint32": (32, False), "ToInt16": (16, True), "ToUint16": (16, False),
              "ToInt8": (8, True), "ToUint8": (8, False)}
    if operation in widths:
        return exactly(integer(N.modular(x, *widths[operation])))
    if operation == "ToUint8Clamp":
        return exactly(integer(N.to_uint8_clamp(x)))
    if operation == "ToIntegerOrInfinity":
        if "JSToString's radix" in variant:
            result = N.to_integer_or_infinity(x)
            value = None if R.is_infinite(result) else int(R.to_fraction(result))
            return exactly(number(value) if value is not None and 2 <= value <= 36 else RANGE_ERROR)
        if "OneReplacement" in variant:
            result = N.to_integer_or_infinity(x)
            value = 1000 if result == R.POSITIVE_INFINITY else 0 if R.is_infinite(result) else int(R.to_fraction(result))
            return exactly(number(min(max(value, 0), 1000)))
        return exactly(toio_number(x))
    if operation == "ToLength":
        return exactly(number(N.to_length(x)))
    if operation == "ToIndex":
        index = N.to_index(x)
        return exactly(RANGE_ERROR if index is None else number(index))
    if operation in ("ToBigInt64", "ToBigUint64"):
        value = VALUES[line]
        return exactly(integer(N.to_big_int64(value) if operation == "ToBigInt64" else N.to_big_uint64(value)))

    if operation == "Float16 encode (canonical policy)":
        encoded = N.float16_encode(x)
        return exactly(f16(N.FLOAT16_CANONICAL_NAN if encoded is None else encoded))
    if operation == "Float16 encode (payload policy)":
        encoded = N.float16_encode(x)
        return exactly(f16(N.float16_nan_preserving(x) if encoded is None else encoded))
    if operation == "Float16 decode":
        return exactly(f64(N.float16_decode(int(raw, 16))))
    if operation == "Math.f16round":
        return exactly(f64(N.f16round(x)))

    if operation == "Math.round":
        return exactly(f64(N.math_round(x)))
    if operation == "Number::exponentiate":
        a, b = doubles(raw)
        return approximated(N.exponentiate(a, b), "platform")
    if operation == "Math.hypot":
        return approximated(N.hypot(doubles(raw)), "faithful")
    if operation == "Math.cbrt":
        return approximated(N.cbrt(x), "correct")
    if operation in ("Math.expm1", "Math.log1p"):
        near = R.is_finite(x) and abs(R.to_fraction(x)) < NEAR_ZERO
        return approximated(N.expm1(x) if operation == "Math.expm1" else N.log1p(x), "faithful", near_zero=near)
    if operation == "Math.sumPrecise":
        return exactly(f64(N.sum_precise(doubles(raw))))

    if operation == "Rounding-mode name":
        name = unescape(raw)
        return exactly(name if name in MODES else RANGE_ERROR)
    if operation == "NegateRoundingMode":
        return exactly(N.negate_rounding_mode(head))
    if operation == "GetUnsignedRoundingMode":
        name, sign = head.split("/")
        return exactly(N.unsigned_rounding_mode(name, sign == "negative"))
    if operation.startswith("ApplyUnsignedRoundingMode"):
        num, den, r1, r2 = (signed_hex(f) for f in raw.split(" "))
        value = Fraction(num, den)
        if operation.endswith("one step"):
            return exactly(integer(N.apply_unsigned_rounding_mode(value, r1, r2, head)))
        name, sign = head.split("/")
        mode = N.unsigned_rounding_mode(name, sign == "negative")
        return exactly(str(N.apply_unsigned_rounding_mode(value, r1, r2, mode) == r2))
    if operation.startswith("RoundNumberToIncrement"):
        num, den, inc = (signed_hex(f) for f in raw.split(" "))
        if operation == "RoundNumberToIncrementAsIfPositive":
            rounded = N.round_number_to_increment_as_if_positive(Fraction(num, den), inc, head)
        else:
            rounded = N.round_number_to_increment(Fraction(num, den), inc, head)
        return exactly(integer(int(rounded)))
    if operation == "A rational's nearest double":
        num, den = (signed_hex(f) for f in raw.split(" "))
        return exactly(f64(R.from_fraction(Fraction(num, den))))
    if operation == "An integer's nearest double":
        value = signed_hex(raw.split(" ")[0])
        return exactly(f64(R.from_fraction(Fraction(value))))

    return bigint_reference(operation, variant, klass, line, detail, raw, limit)


def bigint_reference(operation: str, variant: str, klass: str, line: int, detail: str, raw: str, limit: int) -> Verdict | None:
    head = detail.split("|", 1)[0]
    if klass == "B1P":
        a, b = (VALUES[int(i)] for i in raw.split(" "))
    elif klass == "B1S":
        index, count = raw.split(" ")
        a, b = VALUES[int(index)], signed_hex(count)
    elif klass == "B1N":
        index, width = raw.split(" ")
        a, b = VALUES[int(index)], int(width, 16)
    elif klass == "B1V":
        a = VALUES[line]
    elif klass == "B1E":
        a = signed_hex(raw)
    elif klass == "N1":
        a = int(raw, 16)

    simple = {"BigInt::add": lambda: a + b, "BigInt::subtract": lambda: a - b, "BigInt::multiply": lambda: a * b,
              "BigInt::bitwiseAND": lambda: a & b, "BigInt::bitwiseOR": lambda: a | b,
              "BigInt::bitwiseXOR": lambda: a ^ b, "BigInt::divide": lambda: N.bigint_divide(a, b),
              "BigInt::remainder": lambda: N.bigint_remainder(a, b), "BigInt::unaryMinus": lambda: -a,
              "BigInt::bitwiseNOT": lambda: -a - 1}
    if operation in simple:
        return exactly(within(simple[operation](), limit))
    if operation == "BigInt::exponentiate":
        if b == 0:
            return exactly(big(1))
        if a in (0, 1):
            return exactly(big(a))
        if a == -1:
            return exactly(big(1 if b % 2 == 0 else -1))
        if b > limit or b * (N.bit_width(a) - 1) > limit + 1:
            return exactly(RANGE_ERROR)
        return exactly(within(a ** b, limit))
    if operation in ("BigInt::leftShift", "BigInt::signedRightShift"):
        count = b if operation == "BigInt::leftShift" else -b
        if a != 0 and count > 0 and N.bit_width(a) + count > limit:
            return exactly(RANGE_ERROR)
        return exactly(within(N.bigint_left_shift(a, count), limit))
    if operation == "BigInt.asIntN":
        return exactly(within(N.as_int_n(b, a), limit))
    if operation == "BigInt.asUintN":
        if a < 0 and b > limit:
            return exactly(RANGE_ERROR)
        return exactly(within(N.as_uint_n(b, a), limit))
    if operation == "BigInt::toString":
        radix = int(head)
        text = str(a) if radix == 10 else format(a, {16: "x", 8: "o", 2: "b"}[radix]) if radix in (16, 8, 2) \
            else N.bigint_to_string(a, radix)
        return exactly(text)
    if operation == "StringToBigInt":
        text = string_to_bigint_text(line, head)
        value = N.string_to_big_int(text)
        if value is None:
            return exactly(UNDEFINED)
        if N.bit_width(value) > limit:
            return exactly(f"{RANGE_ERROR}:{-1 if value < 0 else 1}")
        return exactly(big(value))
    if operation == "A BigInt's Number":
        return exactly(f64(N.bigint_to_number(a)))
    if operation == "An integral Number's BigInt":
        return exactly(big(N.number_to_big_int(a)))
    if operation == "Comparison with a Number":
        y = COMPARED[int(head.split(":")[1])]
        order = N.compare_bigint_number(a, y)
        if "EqualsLiteral" in variant:
            return exactly(str(order == 0))
        return exactly(integer(order))
    return None


def string_to_bigint_text(line: int, recipe: str) -> str:
    """StringToBigInt's text for B1V's line and the comparison's recipe ("form/variant"), as the
    comparison builds it (NumericsArms.StringToBigIntTexts)."""
    form, kind = recipe.split("/", 1)
    if form == "-":
        return "" if kind == "empty" else " \t\u3000\u2028"
    value = VALUES[line]
    radix, prefix, beyond = {"d": (10, "", "a"), "x": (16, "0x", "g"), "o": (8, "0o", "8"), "b": (2, "0b", "2")}[form]
    magnitude = abs(value)
    digits = str(magnitude) if radix == 10 else format(magnitude, {16: "x", 8: "o", 2: "b"}[radix])
    plain = ("-" if value < 0 else "") + digits if form == "d" else prefix + digits
    if kind == "plain":
        return plain
    if kind.startswith("ws:"):
        c = chr(int(kind[3:], 16))
        return c + plain + c
    if kind == "plus":
        return "+" + prefix + digits
    if kind == "minus":
        return "-" + prefix + digits
    if kind == "separator":
        return prefix + digits[:1] + "_" + digits[1:]
    if kind == "beyond-radix":
        return prefix + digits + beyond
    if kind == "n-suffix":
        return plain + "n"
    if kind == "upper-prefix":
        return prefix.upper() + digits
    raise ValueError(recipe)


# --------------------------------------------------------------------------------------------------
# Reasons: what the arm does, in words that group.

def reason_for(operation: str, variant: str, arm_value: str, verdict: Verdict, raw: str) -> str:
    expected = verdict.exact if verdict.exact is not None else verdict.shown
    if arm_value.startswith("exception:"):
        return f"throws {arm_value[10:]} where the clause gives a result or a RangeError"
    if arm_value == RANGE_ERROR and verdict.shown != RANGE_ERROR:
        return "refuses with a RangeError an input the clause answers"
    if expected.startswith(RANGE_ERROR):
        return "answers an input the clause or the policy refuses"
    if operation == "Comparison with a Number":
        return "orders the BigInt and the Number wrongly"
    if operation.startswith("Math.") or operation == "Number::exponentiate":
        if arm_value == "NaN":
            return "answers NaN where the clause gives a Number"
        if verdict.shown.startswith("f64:") and " " not in verdict.shown:
            return "misses the clause's special case or exact result"
        if operation in ("Math.expm1", "Math.log1p"):
            return "is more than one unit in the last place from the exact value near zero"
        return "is outside the admitted bound"
    if operation in ("ToInt32", "ToUint32", "ToInt16", "ToUint16", "ToInt8", "ToUint8") and "Atomics" in variant:
        return "wraps the operand after saturating it to 64 bits, not the integer itself"
    if arm_value.startswith("num:") and expected.startswith("num:"):
        a, r = arm_value[4:], expected[4:]
        if a == "-0":
            return "answers -0 where the clause's integer is 0"
        if r.endswith("Infinity") and not a.endswith("Infinity"):
            return "answers a finite value for an infinity"
        if a.endswith("Infinity") and not r.endswith("Infinity"):
            return "answers an infinity for a finite integer (its saturation sentinel)"
        if a.lstrip("-").isdigit() and int(a) in (2**31 - 1, -2**31, 2**63 - 1, -2**63, 2**32 - 1):
            return "saturates at its type's bound"
        return "answers another integer"
    if operation.startswith("ApplyUnsignedRoundingMode") or operation.startswith("RoundNumberToIncrement"):
        return "rounds to the other end"
    if operation in ("BigInt::toString", "StringToBigInt") and "U+2212" in variant:
        return "follows the process's culture (its negative sign)"
    if operation == "A rational's nearest double":
        return "does not round the rational once to the nearest double"
    if operation == "BigInt.asUintN":
        return "wraps the width or the residue"
    return "answers another value"


# --------------------------------------------------------------------------------------------------

def classify_row(line: str) -> tuple[str, ...]:
    operation, klass, number_text, detail, raw_listed, variant, s_listed, arm_listed = line.rstrip("\n").split("\t")
    raw = field(raw_listed)[1]
    detail_text = field(detail)[1] or detail
    number_ = int(number_text)
    s_value, s_charge = charged(s_listed)
    arm_value, arm_charge = charged(arm_listed)
    shown_input = raw if raw is not None else raw_listed
    try:
        verdict = reference(operation, variant, klass, number_, detail_text, raw if raw is not None else "")
    except Exception as error:  # noqa: BLE001
        return (operation, klass, number_text, detail_text, shown_input, s_listed, variant, arm_listed,
                f"(reference failed: {type(error).__name__})", "unclassified", "the reference cannot judge the input", "")
    if verdict is None:
        return (operation, klass, number_text, detail_text, shown_input, s_listed, variant, arm_listed,
                "", "unclassified", "the reference cannot judge the input", "")

    if s_value == arm_value or (s_charge is not None and matches_value(s_value, arm_value)):
        if s_charge != arm_charge:
            label, why = "unclassified", "the values agree and S's charge differs from arm V's (class (c))"
            return (operation, klass, number_text, detail_text, shown_input, s_listed, variant, arm_listed,
                    verdict.shown, label, why, "")

    s_ok = admitted(verdict, verdict.s_admits, s_value)
    arm_ok = admitted(verdict, verdict.arm_admits, arm_value)
    if s_ok and not arm_ok:
        label, why = "a", reason_for(operation, variant, arm_value, verdict, raw or "")
    elif s_ok and arm_ok:
        label, why = "b", "both admitted"
    elif not s_ok:
        label, why = "unclassified", "S's result is not admitted (class (c))"
    else:
        label, why = "unclassified", "the reference cannot judge the input"
    return (operation, klass, number_text, detail_text, shown_input, s_listed, variant, arm_listed,
            verdict.shown, label, why, "")


def matches_value(left: str, right: str) -> bool:
    return left == right


def admitted(verdict: Verdict, predicate, value: str) -> bool:
    """Whether a result is admitted; a long result, listed by its length and digest, is matched with
    the exact reference's rendering."""
    if value.startswith("len:") and ":sha256:" in value:
        return verdict.exact is not None and matches(verdict.exact, value)
    return predicate(value)


def classify_chunk(lines: list[str]) -> list[tuple[str, ...]]:
    return [classify_row(line) for line in lines]


# --------------------------------------------------------------------------------------------------
# Vectors covering class (b).

COVERING_OPS = {
    "Number::exponentiate": ("Number::exponentiate", "Math.pow"),
    "Math.hypot": ("Math.hypot",), "Math.cbrt": ("Math.cbrt",), "Math.expm1": ("Math.expm1",),
    "Math.log1p": ("Math.log1p",),
    "Float16 encode (canonical policy)": ("Float16 encode",), "Float16 encode (payload policy)": ("Float16 encode",),
}


def covering_vectors(corpus: Path) -> dict[str, tuple[str, str]]:
    found: dict[str, tuple[str, str]] = {}
    for path in sorted(corpus.glob("*.vectors")):
        for block in path.read_text(encoding="utf-8").split("\n\n"):
            record = dict(line.split(": ", 1) for line in block.splitlines() if ": " in line and not line.startswith("#"))
            if "id" not in record:
                continue
            several = " | " in record.get("out", "")
            if record.get("class") != "impl-defined" and not several:
                continue
            for operation, ops in COVERING_OPS.items():
                if record.get("op") in ops:
                    found.setdefault(operation, (record["id"], record.get("since", "?")))
    return found


def load(population: Path, name: str) -> list[str]:
    with gzip.open(population / f"{name}.txt.gz", "rt", encoding="utf-8") as stream:
        return [line.rstrip("\n") for line in stream]


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--run", type=Path, required=True)
    parser.add_argument("--population", type=Path, required=True)
    parser.add_argument("--corpus", type=Path, default=Path(__file__).resolve().parents[2] / "conformance/vectors/numerics")
    parser.add_argument("--jobs", type=int, default=multiprocessing.cpu_count())
    parser.add_argument("--examples", type=int, default=20)
    args = parser.parse_args(argv)

    VALUES.extend(signed_hex(line) for line in load(args.population, "B1V"))
    COMPARED.extend(int(line, 16) for line in load(args.population, "B1C"))
    vectors = covering_vectors(args.corpus)
    differences = args.run / "differences.tsv.gz"
    source_lines = 0

    def chunks():
        nonlocal source_lines
        with gzip.open(differences, "rt", encoding="utf-8", newline="\n") as stream:
            header = stream.readline()
            assert header.startswith("operation\t"), header
            chunk: list[str] = []
            for line in stream:
                chunk.append(line)
                source_lines += 1
                if len(chunk) == 2000:
                    yield chunk
                    chunk = []
            if chunk:
                yield chunk

    counts: Counter[tuple[str, str, str, str]] = Counter()
    examples: dict[tuple[str, str, str, str], list[tuple[str, ...]]] = defaultdict(list)
    listing = hashlib.sha256()
    total = 0
    with open(args.run / "classification.tsv.gz", "wb") as raw, \
            gzip.GzipFile(filename="", mode="wb", fileobj=raw, mtime=0, compresslevel=6) as gz:
        head = "operation\tclass\tline\tdetail\tinput\tS\tvariant\tresult\treference\tlabel\treason\tvector\n"
        gz.write(head.encode("utf-8"))
        listing.update(head.encode("utf-8"))
        with multiprocessing.get_context("fork").Pool(args.jobs) as pool:
            for rows in pool.imap(classify_chunk, chunks(), chunksize=1):
                for row in rows:
                    row = list(row)
                    if row[9] == "b":
                        vector = vectors.get(row[0])
                        if vector is None:
                            row[9], row[10] = "unclassified", "both admitted, and no vector covers it"
                        else:
                            row[11] = f"{vector[0]} (revision {vector[1]})"
                    for at in (3, 4, 5, 7, 8):
                        if len(row[at]) > SHOWN:
                            row[at] = f"({len(row[at])} characters)"
                    text = "\t".join(row) + "\n"
                    gz.write(text.encode("utf-8"))
                    listing.update(text.encode("utf-8"))
                    key = (row[0], row[6], row[9], row[10])
                    counts[key] += 1
                    total += 1
                    if len(examples[key]) < args.examples:
                        examples[key].append(tuple(row))

    with gzip.open(differences, "rb") as stream:
        source = hashlib.sha256()
        for block in iter(lambda: stream.read(1 << 20), b""):
            source.update(block)

    summary = {
        "differences": {"file": "differences.tsv.gz", "lines": source_lines, "sha256": source.hexdigest()},
        "classification": {"file": "classification.tsv.gz", "lines": total, "sha256": listing.hexdigest()},
        "unclassified": sum(c for k, c in counts.items() if k[2] == "unclassified"),
        "groups": [{"operation": k[0], "variant": k[1], "class": k[2], "reason": k[3], "count": c}
                   for k, c in sorted(counts.items())],
    }
    (args.run / "classification.json").write_text(json.dumps(summary, indent=2) + "\n", encoding="utf-8")

    report = ["# Reference-arm comparison of the numerics area: classification", "",
              f"Differences listed: {source_lines}; classified: {total}; unclassified: {summary['unclassified']}.", ""]
    for key, count in sorted(counts.items()):
        report.append(f"## {key[0]} / {key[1]} / class {key[2]} / {key[3]}: {count}")
        report.append("")
        report.append("| population | detail | input | S | arm's result | reference | vector |")
        report.append("|---|---|---|---|---|---|---|")
        for row in examples[key]:
            cells = [f"{row[1]}:{row[2]}", row[3][:80], row[4][:120], row[5][:120], row[7][:120], row[8][:160], row[11]]
            report.append("| " + " | ".join(c.replace("|", "\\|") for c in cells) + " |")
        report.append("")
    (args.run / "report.md").write_text("\n".join(report) + "\n", encoding="utf-8")
    print(f"classified {total} differences; unclassified {summary['unclassified']}", file=sys.stderr)
    return 0


if __name__ == "__main__":
    sys.exit(main())
