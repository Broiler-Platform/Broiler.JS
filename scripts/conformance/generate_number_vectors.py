#!/usr/bin/env python3
"""Generate the number area of the shared conformance corpus.

    python3 scripts/conformance/generate_number_vectors.py \
        [--proplist <UCD 17.0.0 PropList.txt>] --out conformance/vectors/numbers

Every expected value comes from `numref.py`, the corpus's independent reference computation in
exact arithmetic; no expected value is taken from either engine. The populations are milestone
SJL-1.4's: the special values, every power of two and of ten across the exponent range, the
subnormal boundaries, the neighbours of 1e21 and 1e-7, a seeded random population, radix
conversion at every radix, the argument edges and exact ties of toFixed, toExponential and
toPrecision, StringToNumber's whitespace set and prefixes, parseInt and parseFloat, and the values
of numeric literals.

The output is deterministic: the same inputs give the same bytes. Vector identifiers are stable
and are never reused; a later revision appends.
"""

from __future__ import annotations

import argparse
import hashlib
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import numref as R  # noqa: E402

# The corpus revision this generator writes, and the revision most of its vectors were added in.
# Revisions 2 and 3 classified differences (number-divergences.txt) and added no vector; revision 4
# appended the parseInt vectors of revision_4_vectors, after every vector of revision 1; revisions 5
# and 6 retired divergences (number-divergences.txt) and added no vector to this area (revision 6
# added the numerics area, which generate_numerics_vectors.py writes, and revisions 7 to 9 changed it;
# revision 10 added the text area, which generate_text_vectors.py writes; revision 11 widened the
# schema's clause ids and changed no vector).
CORPUS_REVISION = 11
FIRST_REVISION = 1
GENERATOR = "ref:numref@1"
SPEC = "spec:es2026@0248456c"
UCD = "ucd:17.0.0/PropList.txt"
# The SHA-256 of the pinned PropList.txt (conformance/vectors/sources/unicode.pin).
PROPLIST_SHA256 = "130dcddcaadaf071008bdfce1e7743e04fdfbc910886f017d9f9ac931d8c64dd"
ENGINES = ("broiler-js", "broiler-vm")
DIVERGENCES = Path(__file__).resolve().parent / "number-divergences.txt"


# --------------------------------------------------------------------------------------------------
# Rendering in the corpus's notation (SCHEMA.md).

def f64(bits: int) -> str:
    if R.is_nan(bits):
        bits = R.CANONICAL_NAN
    return f"f64:0x{bits:016X}"


def js_string_body(s: str) -> str:
    out = []
    for ch in s:
        code = ord(ch)
        if ch == '"':
            out.append('\\"')
        elif ch == "\\":
            out.append("\\\\")
        elif 0x20 <= code <= 0x7E:
            out.append(ch)
        else:
            out.append(f"\\u{code:04X}")
    return "".join(out)


def string_value(s: str) -> str:
    return f'str:"{js_string_body(s)}"'


def js_string_literal(s: str) -> str:
    return f'"{js_string_body(s)}"'


def double_expression(bits: int) -> str:
    return f"$f64(0x{bits >> 32:08X}, 0x{bits & 0xFFFFFFFF:08X})"


def value_out(rendered: list[str]) -> str:
    return " | ".join(f"value: {r}" for r in rendered)


# --------------------------------------------------------------------------------------------------
# Records.

class Corpus:
    def __init__(self) -> None:
        self.files: dict[str, list[dict[str, str]]] = {}
        self.ids: set[str] = set()

    def add(self, file: str, area_id: str, record: dict[str, str], since: int = FIRST_REVISION) -> None:
        records = self.files.setdefault(file, [])
        vector_id = f"numbers/{area_id}/{len([r for r in records if r['id'].startswith(f'numbers/{area_id}/')]) + 1:04d}"
        assert vector_id not in self.ids
        self.ids.add(vector_id)
        full = {"id": vector_id, "area": "numbers"}
        full.update(record)
        full.setdefault("class", "normative")
        full["since"] = str(since)
        records.append(full)

    def classify(self, path: Path) -> None:
        """Applies number-divergences.txt: each listed vector becomes a divergence of the engine it
        names, with the reason's text. Its expected value is not touched."""
        reasons: dict[str, str] = {}
        by_id = {record["id"]: record for records in self.files.values() for record in records}
        for number, line in enumerate(path.read_text(encoding="utf-8").splitlines(), 1):
            if not line or line.startswith("#"):
                continue
            if line.startswith("reason "):
                key, _, text = line[len("reason "):].partition(": ")
                if not text or key in reasons:
                    raise SystemExit(f"{path.name}:{number}: a malformed or repeated reason")
                reasons[key] = text
                continue
            engine, key, vector_id = line.split(" ")
            record = by_id.get(vector_id)
            if engine not in ENGINES or key not in reasons or record is None:
                raise SystemExit(f"{path.name}:{number}: '{line}' names no engine, reason or vector")
            if f"divergence.{engine}" in record or (
                    record["class"] == "impl-defined" and f"per-engine.{engine}" not in record):
                raise SystemExit(f"{path.name}:{number}: {vector_id} cannot take this classification")
            # An impl-defined vector stays impl-defined: the field classifies the engine's difference
            # from its own per-engine expectation (SCHEMA.md section 5).
            if record["class"] == "normative":
                record["class"] = "divergence"
            record[f"divergence.{engine}"] = reasons[key]

    def write(self, out: Path) -> None:
        out.mkdir(parents=True, exist_ok=True)
        order = ["id", "area", "op", "in", "out", "class", "per-engine.broiler-js", "per-engine.broiler-vm",
                 "divergence.broiler-js", "divergence.broiler-vm", "prov", "script", "note", "since"]
        for name, records in sorted(self.files.items()):
            lines = [
                "# Generated by scripts/conformance/generate_number_vectors.py; do not edit by hand.",
                "# Expected values: scripts/conformance/numref.py, an exact reference computation.",
                "schema: 1",
            ]
            for record in records:
                lines.append("")
                for key in order:
                    if key in record:
                        lines.append(f"{key}: {record[key]}")
            (out / name).write_text("\n".join(lines) + "\n", encoding="utf-8", newline="\n")


# --------------------------------------------------------------------------------------------------
# Populations.

def splitmix64(seed: int):
    state = seed & 0xFFFFFFFFFFFFFFFF
    while True:
        state = (state + 0x9E3779B97F4A7C15) & 0xFFFFFFFFFFFFFFFF
        z = state
        z = ((z ^ (z >> 30)) * 0xBF58476D1CE4E5B9) & 0xFFFFFFFFFFFFFFFF
        z = ((z ^ (z >> 27)) * 0x94D049BB133111EB) & 0xFFFFFFFFFFFFFFFF
        yield z ^ (z >> 31)


def random_doubles(seed: int, count: int) -> list[int]:
    out = []
    for draw in splitmix64(seed):
        bits = draw & ~R.SIGN_BIT
        if R.is_finite(bits) and not R.is_zero(bits):
            out.append(bits)
            if len(out) == count:
                return out
    raise AssertionError


def power_of_two(exponent: int) -> int:
    return R.from_fraction(R.Fraction(2) ** exponent)


def power_of_ten(exponent: int) -> int:
    return R.from_fraction(R.Fraction(10) ** exponent)


def d(value: float | str) -> int:
    """A double written as a decimal in the generator's source, converted exactly."""
    numbers = R.string_to_number(str(value))
    assert len(numbers) == 1
    return numbers[0]


SPECIALS = [0, R.NEGATIVE_ZERO, R.CANONICAL_NAN, R.POSITIVE_INFINITY, R.NEGATIVE_INFINITY]
BOUNDARIES = [R.SMALLEST_SUBNORMAL, R.SMALLEST_SUBNORMAL + 1, R.LARGEST_SUBNORMAL - 1, R.LARGEST_SUBNORMAL,
              R.SMALLEST_NORMAL, R.SMALLEST_NORMAL + 1, R.MAX_FINITE - 1, R.MAX_FINITE]


def neighbours(bits: int, count: int) -> list[int]:
    return [bits + k for k in range(-count, count + 1)]


ORDINARY = [d(v) for v in ["1", "0.1", "0.5", "123.456", "-987.654321", "1e21", "1e-7", "1.5e-7", "9007199254740992",
                          "9007199254740993", "9007199254740994", "18446744073709551616", "4294967295", "255",
                          "3.141592653589793", "2.718281828459045", "0.000001", "1e100", "-1e-100", "2.9802322387695312e-8",
                          "123456789012345680000", "100", "1000000000000000128", "0.3", "1.005", "5e-324"]]


# --------------------------------------------------------------------------------------------------
# Number::toString.

def to_string_record(bits: int, radix: int | None) -> dict[str, str]:
    result = R.number_to_string(bits, radix or 10)
    for value in result.values:
        check_digits(bits, value, radix or 10)
    clause = f"{SPEC}#sec-numeric-types-number-tostring"
    if radix is None:
        script = f"String({double_expression(bits)})"
        op = "Number::toString"
        inputs = f64(bits)
    else:
        script = f"{double_expression(bits)}.toString({radix})"
        op = "Number.prototype.toString"
        inputs = f"{f64(bits)} int:{radix}"
        clause += f" {SPEC}#sec-number.prototype.tostring"
    record = {"op": op, "in": inputs, "out": value_out([string_value(v) for v in result.values]),
              "prov": f"{clause} {GENERATOR}", "script": script}
    if len(result.values) > 1:
        record["note"] = f"the last digit is not uniquely determined (step 5); recommended {result.recommended}"
    return record


def check_digits(bits: int, rendered: str, radix: int) -> None:
    """Every admissible string must denote a value that rounds to the double it renders."""
    if rendered in ("NaN", "Infinity", "-Infinity", "0"):
        return
    negative = rendered.startswith("-")
    body = rendered[1:] if negative else rendered
    # Only radix 10 has exponent notation; above radix 14 "e" is a digit.
    mantissa, _, exponent = body.partition("e") if radix == 10 else (body, "", "")
    whole, _, fraction = mantissa.partition(".")
    value = R.Fraction(int(whole + fraction, radix)) / R.Fraction(radix) ** len(fraction)
    if exponent:
        value *= R.Fraction(10) ** int(exponent)
    back = R.from_fraction(-value if negative else value)
    assert back == bits, (hex(bits), rendered)


def number_to_string_vectors(corpus: Corpus) -> None:
    population: list[int] = list(SPECIALS) + BOUNDARIES
    population += [power_of_two(e) for e in range(-1074, 1024)]
    population += [power_of_ten(e) for e in range(-323, 309)]
    for e in range(-323, 309, 8):
        population += [power_of_ten(e) - 1, power_of_ten(e) + 1]
    population += neighbours(d("1e21"), 8) + neighbours(d("1e-7"), 8)
    population += ORDINARY
    population += random_doubles(0x534A4C312D4E3031, 2000)  # "SJL1-N01"
    population += [b | R.SIGN_BIT for b in random_doubles(0x534A4C312D4E3032, 100)]  # "SJL1-N02"
    seen: set[int] = set()
    for bits in population:
        if bits in seen:
            continue
        seen.add(bits)
        corpus.add("number-to-string.vectors", "to-string", to_string_record(bits, None))


RADIX_VALUES = [d(v) for v in ["0", "-0", "1", "-1", "2", "35", "36", "37", "255", "4294967295", "4294967296",
                               "9007199254740991", "9007199254740992", "9007199254740993", "18446744073709551616",
                               "1e21", "0.5", "0.25", "0.1", "-0.1", "0.3333333333333333", "0.6666666666666666",
                               "123.456", "-987.654321", "1e-7", "5e-324", "2.2250738585072014e-308",
                               "1.7976931348623157e308", "1e100"]] + [R.CANONICAL_NAN, R.POSITIVE_INFINITY,
                                                                       R.NEGATIVE_INFINITY]


def radix_vectors(corpus: Corpus) -> None:
    randoms = random_doubles(0x534A4C312D523031, 35 * 6)  # "SJL1-R01"
    for radix in range(2, 37):
        values = RADIX_VALUES + randoms[(radix - 2) * 6:(radix - 1) * 6]
        for bits in values:
            corpus.add("number-to-string-radix.vectors", "to-string-radix", to_string_record(bits, radix))


# --------------------------------------------------------------------------------------------------
# toFixed, toExponential, toPrecision and their argument edges.

DIGIT_VALUES = [d(v) for v in ["0", "-0", "1", "-1", "0.5", "1.5", "2.5", "-2.5", "0.05", "1.005", "123.456",
                               "1000000000000000128", "1e20", "999999999999999999999", "1e21", "1.5e21", "0.000001",
                               "1e-7", "5e-324", "1.7976931348623157e308", "0.1", "3.141592653589793"]] + \
    [R.CANONICAL_NAN, R.POSITIVE_INFINITY, R.NEGATIVE_INFINITY]


def ties(digits_after_point: int, count: int, seed: int) -> list[int]:
    """Doubles a / 2**(f+1), a odd: exact ties at f digits after the point, below 1e21."""
    out = []
    for draw in splitmix64(seed + digits_after_point):
        a = (draw >> 11) | 1
        value = R.Fraction(a, 2 ** (digits_after_point + 1))
        if value < 10 ** 21:
            bits = R.from_fraction(value)
            if R.to_fraction(bits) == value:
                out.append(bits)
        if len(out) == count:
            return out
    raise AssertionError


def method_record(op: str, clause: str, bits: int, argument: str, argument_in: str, out: str,
                  note: str | None = None) -> dict[str, str]:
    record = {"op": op, "in": f"{f64(bits)}{(' ' + argument_in) if argument_in else ''}", "out": out,
              "prov": f"{SPEC}#{clause} {GENERATOR}",
              "script": f"{double_expression(bits)}.{op.split('.')[-1]}({argument})"}
    if note:
        record["note"] = note
    return record


def fixed_vectors(corpus: Corpus) -> None:
    op, clause = "Number.prototype.toFixed", "sec-number.prototype.tofixed"
    for bits in DIGIT_VALUES:
        for f in (0, 1, 2, 5, 10, 20, 50, 100):
            out = value_out([string_value(R.to_fixed(bits, f))])
            corpus.add("to-fixed.vectors", "to-fixed", method_record(op, clause, bits, str(f), f"int:{f}", out))
    for f in list(range(0, 21)) + [30, 50, 100]:
        for bits in ties(f, 3, 0x534A4C312D463031):  # "SJL1-F01"
            out = value_out([string_value(R.to_fixed(bits, f))])
            corpus.add("to-fixed.vectors", "to-fixed", method_record(op, clause, bits, str(f), f"int:{f}", out,
                                                                    note=f"an exact tie at {f} fraction digits"))
    one, infinity = d("1"), R.POSITIVE_INFINITY
    edges = [
        (one, "", "undefined", value_out([string_value("1")]), "undefined is 0"),
        (one, "-1", "int:-1", "error: RangeError", None),
        (one, "101", "int:101", "error: RangeError", None),
        (one, "100.5", "f64:0x4059200000000000", value_out([string_value(R.to_fixed(one, 100))]), "truncated to 100"),
        (one, "NaN", f64(R.CANONICAL_NAN), value_out([string_value("1")]), "NaN is 0"),
        (one, "Infinity", f64(R.POSITIVE_INFINITY), "error: RangeError", None),
        (infinity, "1000", "int:1000", "error: RangeError", "the range is checked before the value is"),
        (R.CANONICAL_NAN, "0", "int:0", value_out([string_value("NaN")]), None),
    ]
    for bits, argument, argument_in, out, note in edges:
        argument_in = argument_in if argument != "" else "undefined"
        corpus.add("to-fixed.vectors", "to-fixed-arguments",
                   method_record(op, clause, bits, argument, argument_in, out, note))


def exponential_ties(precision: int, count: int, seed: int) -> list[int]:
    """Integers below 2**53 with precision + 1 significant digits, the last a 5: exact ties."""
    out = []
    for draw in splitmix64(seed + precision):
        head = 10 ** (precision - 1) + draw % (9 * 10 ** (precision - 1))
        value = head * 10 + 5
        if value < 2 ** 53:
            out.append(R.from_fraction(R.Fraction(value)))
        if len(out) == count:
            return out
    raise AssertionError


def exponential_vectors(corpus: Corpus) -> None:
    op, clause = "Number.prototype.toExponential", "sec-number.prototype.toexponential"
    for bits in DIGIT_VALUES:
        for f in (None, 0, 1, 2, 5, 10, 20, 50, 100):
            result = R.to_exponential(bits, f)
            out = value_out([string_value(v) for v in result.values])
            note = None
            if len(result.values) > 1:
                note = f"the last digit is not uniquely determined; recommended {result.recommended}"
            corpus.add("to-exponential.vectors", "to-exponential",
                       method_record(op, clause, bits, "" if f is None else str(f),
                                     "undefined" if f is None else f"int:{f}", out, note))
    for p in range(1, 16):
        for bits in exponential_ties(p, 3, 0x534A4C312D453031):  # "SJL1-E01"
            result = R.to_exponential(bits, p - 1)
            corpus.add("to-exponential.vectors", "to-exponential",
                       method_record(op, clause, bits, str(p - 1), f"int:{p - 1}",
                                     value_out([string_value(result.recommended)]),
                                     note=f"an exact tie at {p} significant digits"))
    one = d("1")
    edges = [
        (one, "-1", "int:-1", "error: RangeError", None),
        (one, "101", "int:101", "error: RangeError", None),
        (R.POSITIVE_INFINITY, "1000", "int:1000", value_out([string_value("Infinity")]),
         "the value is checked before the range is"),
        (R.CANONICAL_NAN, "-1", "int:-1", value_out([string_value("NaN")]), None),
        (one, "NaN", f64(R.CANONICAL_NAN), value_out([string_value("1e+0")]), "NaN is 0"),
    ]
    for bits, argument, argument_in, out, note in edges:
        corpus.add("to-exponential.vectors", "to-exponential-arguments",
                   method_record(op, clause, bits, argument, argument_in, out, note))


def precision_vectors(corpus: Corpus) -> None:
    op, clause = "Number.prototype.toPrecision", "sec-number.prototype.toprecision"
    values = DIGIT_VALUES + [d(v) for v in ["1e-6", "1.23e-7", "123456", "1234567", "0.00001234"]]
    for bits in values:
        for p in (1, 2, 3, 7, 10, 16, 17, 21, 50, 100):
            out = value_out([string_value(R.to_precision(bits, p))])
            corpus.add("to-precision.vectors", "to-precision",
                       method_record(op, clause, bits, str(p), f"int:{p}", out))
        out = value_out([string_value(v) for v in R.number_to_string(bits).values])
        corpus.add("to-precision.vectors", "to-precision",
                   method_record(op, clause, bits, "", "undefined", out, note="undefined is ToString"))
    for p in range(1, 16):
        for bits in exponential_ties(p, 3, 0x534A4C312D503031):  # "SJL1-P01"
            out = value_out([string_value(R.to_precision(bits, p))])
            corpus.add("to-precision.vectors", "to-precision",
                       method_record(op, clause, bits, str(p), f"int:{p}", out,
                                     note=f"an exact tie at {p} significant digits"))
    one = d("1")
    edges = [
        (one, "0", "int:0", "error: RangeError", None),
        (one, "101", "int:101", "error: RangeError", None),
        (one, "100.5", "f64:0x4059200000000000", value_out([string_value(R.to_precision(one, 100))]),
         "truncated to 100"),
        (R.CANONICAL_NAN, "0", "int:0", value_out([string_value("NaN")]), "the value is checked before the range is"),
        (R.POSITIVE_INFINITY, "1000", "int:1000", value_out([string_value("Infinity")]), None),
    ]
    for bits, argument, argument_in, out, note in edges:
        corpus.add("to-precision.vectors", "to-precision-arguments",
                   method_record(op, clause, bits, argument, argument_in, out, note))


# --------------------------------------------------------------------------------------------------
# Number::toString's radix argument.

def radix_argument_vectors(corpus: Corpus) -> None:
    clause = f"{SPEC}#sec-number.prototype.tostring"
    five = d("5")
    cases = [
        ("", "undefined", value_out([string_value("5")]), "undefined is 10"),
        ("0", "int:0", "error: RangeError", None),
        ("1", "int:1", "error: RangeError", None),
        ("37", "int:37", "error: RangeError", None),
        ("null", "null", "error: RangeError", "ToIntegerOrInfinity(null) is 0"),
        ("NaN", f64(R.CANONICAL_NAN), "error: RangeError", "ToIntegerOrInfinity(NaN) is 0"),
        ("2.9", f64(d("2.9")), value_out([string_value("101")]), "truncated to 2"),
        ("36.9", f64(d("36.9")), value_out([string_value("5")]), "truncated to 36"),
        ('"16"', 'str:"16"', value_out([string_value("5")]), None),
        ("-0", f64(R.NEGATIVE_ZERO), "error: RangeError", None),
        ("Infinity", f64(R.POSITIVE_INFINITY), "error: RangeError", None),
    ]
    for argument, argument_in, out, note in cases:
        record = {"op": "Number.prototype.toString", "in": f"{f64(five)} {argument_in}", "out": out,
                  "prov": f"{clause} {GENERATOR}", "script": f"{double_expression(five)}.toString({argument})"}
        if note:
            record["note"] = note
        corpus.add("number-to-string-radix.vectors", "to-string-radix-arguments", record)


# --------------------------------------------------------------------------------------------------
# StringToNumber, parseFloat and parseInt.

NOT_WHITE_SPACE = [0x0085, 0x180E, 0x200B, 0x200C, 0x200D, 0x2060, 0x00AD, 0x0000, 0x001C, 0x001F, 0x3164, 0xFFA0,
                   0x0008, 0x000E, 0xD800, 0xDC00]


def string_inputs() -> list[str]:
    out: list[str] = ["", " ", "0", "-0", "+0", "1", "-1", "+1", "00", "007", "1.", ".1", ".", "5.", "-.5", "+.5", "e5",
                      "1e", "1e+", "1e-", "1e5", "1E5", "1e+5", "1e-5", "1.5e3", ".5e-3", "1e400", "-1e400",
                      "1e-400", "Infinity", "-Infinity", "+Infinity", "infinity", "INFINITY", "Infinity1",
                      "NaN", "nan", "1_000", "1__0", "_1", "1_", "0x", "0x1", "0X1", "0xg", "0x1_0", "-0x1",
                      "+0x1", "0b", "0b101", "0B101", "0b102", "0o", "0o17", "0O17", "0o18", "0x7FFFFFFF",
                      "0xFFFFFFFFFFFFFFFF", "1 2", "1\u00a0", "12abc", "abc", "0.0", "1.0e0", "1e0001",
                      "000000000000000000001", "0.00000000000000000000000000000000000001",
                      "9007199254740993", "9007199254740995", "18014398509481985", "18014398509481987",
                      "2.2250738585072011e-308", "2.2250738585072012e-308", "4.9406564584124654e-324",
                      "2.4703282292062327e-324", "2.4703282292062328e-324", "1.7976931348623157e308",
                      "1.7976931348623158e308", "1.7976931348623159e308", "123456789012345678901234567890",
                      "0.1000000000000000055511151231257827", "1.00000000000000011102230246251565404236316680908203125",
                      "1.00000000000000011102230246251565404236316680908203124",
                      "1.00000000000000011102230246251565404236316680908203126"]
    for prefix in ("0b", "0o", "0x"):
        radix = {"0b": 2, "0o": 8, "0x": 16}[prefix]
        for e in (52, 53, 54, 63, 64, 65):
            for k in (-2, -1, 0, 1, 2, 3):
                value = 2 ** e + k
                out.append(prefix + R._digits(value, radix))
            halfway = 2 ** e + 2 ** (e - 52) // 2 if e >= 53 else None
            if halfway:
                for k in (-1, 0, 1):
                    out.append(prefix + R._digits(halfway + k, radix))
    return out


def rounding_boundary_strings() -> list[str]:
    """Decimal strings at and around the exact midpoint between a double and the next."""
    out = []
    bases = [power_of_ten(e) for e in range(-20, 23, 3)] + random_doubles(0x534A4C312D533031, 12)  # "SJL1-S01"
    for bits in bases:
        midpoint = (R.to_fraction(bits) + R.to_fraction(bits + 1)) / 2
        # midpoint = m / 2**k exactly, so it has a finite decimal expansion.
        k = 0
        denominator = midpoint.denominator
        while denominator % 2 == 0:
            denominator //= 2
            k += 1
        assert denominator == 1
        scaled = midpoint.numerator * 5 ** k  # midpoint = scaled * 10**-k
        digits = str(scaled)
        exponent = -k
        for length in (17, 18, 19, 20, 21, 25, 40, len(digits)):
            if length > len(digits):
                continue
            head = int(digits[:length])
            e = exponent + len(digits) - length
            for delta in (-1, 0, 1):
                out.append(f"{head + delta}e{e}")
    return out


def number_vectors(corpus: Corpus) -> None:
    clause = f"{SPEC}#sec-stringtonumber {SPEC}#sec-roundmvresult"
    inputs = string_inputs()
    for code in sorted(R.STR_WHITE_SPACE) + NOT_WHITE_SPACE:
        ch = chr(code)
        inputs += [ch, ch + "1", "1" + ch, ch + "1" + ch, ch + ch + "-1.5" + ch]
    inputs += rounding_boundary_strings()
    seen = set()
    for s in inputs:
        if s in seen:
            continue
        seen.add(s)
        values = R.string_to_number(s)
        record = {"op": "StringToNumber", "in": string_value(s), "out": value_out([f64(b) for b in values]),
                  "prov": f"{clause} {UCD} {GENERATOR}", "script": f"Number({js_string_literal(s)})"}
        if len(values) > 1:
            record["note"] = "more than 20 significant digits: RoundMVResult admits either option"
        corpus.add("string-to-number.vectors", "string-to-number", record)
        values = R.parse_float(s)
        record = {"op": "parseFloat", "in": string_value(s), "out": value_out([f64(b) for b in values]),
                  "prov": f"{SPEC}#sec-parsefloat-string {SPEC}#sec-roundmvresult {UCD} {GENERATOR}",
                  "script": f"parseFloat({js_string_literal(s)})"}
        if len(values) > 1:
            record["note"] = "more than 20 significant digits: RoundMVResult admits either option"
        corpus.add("parse-float.vectors", "parse-float", record)


def parse_int_record(s: str, radix: int | None) -> dict[str, str]:
    result = R.parse_int(s, radix)
    argument = "" if radix is None else f", {radix}"
    record = {"op": "parseInt", "in": f"{string_value(s)} {'undefined' if radix is None else f'int:{radix}'}",
              "out": value_out([f64(b) for b in result.values]),
              "prov": f"{SPEC}#sec-parseint-string-radix {UCD} {GENERATOR}",
              "script": f"parseInt({js_string_literal(s)}{argument})"}
    if result.approximated:
        exact = value_out([f64(result.values[-1] if len(result.values) == 1 else result.values[0])])
        record["class"] = "impl-defined"
        record["out"] = exact
        for engine in ENGINES:
            record[f"per-engine.{engine}"] = exact
        record["note"] = ("the radix is not 2, 4, 8, 10, 16 or 32, so the result may be an implementation-"
                          "approximated integer; both engines' plans require the exact value (SJL-3.3)")
    elif len(result.values) > 1:
        record["note"] = "more than 20 significant digits at radix 10: either value is admitted"
    return record


def parse_int_vectors(corpus: Corpus) -> None:
    cases: list[tuple[str, int | None]] = []
    for s in ["0", "-0", "+0", "1", "-1", "123", "  123", "\u00a0123", "\ufeff123", "\u0085123", "123abc", "abc",
              "", " ", "0x1F", "0X1f", "-0x1F", "0x", "0xg", "08", "010", "1e3", "1.9", "-1.9", "Infinity", "null",
              "zz", "ZZ", "Zz", "9007199254740993", "18014398509481985", "123456789012345678901",
              "12345678901234567890123456789", "00000000000000000000000000001", "1000000000000000000000000000000",
              "200000000000011"]:
        for radix in (None, 0, 2, 8, 10, 16, 36):
            cases.append((s, radix))
    for radix in (-1, 1, 3, 7, 32, 37, 4294967296, 4294967312, -4294967280):
        cases.append(("123", radix))
        cases.append(("zz", radix))
    for radix in range(2, 37):
        top = R._digits(radix - 1, radix)
        cases.append((top * 3, radix))
        cases.append((R._digits(2 ** 53 + 1, radix), radix))
        cases.append((R._digits(2 ** 64 + 2 ** 11 + 1, radix), radix))
    seen = set()
    for s, radix in cases:
        if (s, radix) in seen:
            continue
        seen.add((s, radix))
        corpus.add("parse-int.vectors", "parse-int", parse_int_record(s, radix))


def revision_4_vectors(corpus: Corpus) -> None:
    """Corpus revision 4: radix-10 parseInt strings of more than 20 significant digits on which the
    two options the clause admits - every digit, or the first 20 and zeros - round to different
    doubles. Revision 1's long strings all round to one double either way, so no vector held both
    options at radix 10; bundle sjl-3-001's reference-arm comparison found the difference such a
    string makes. Each is 2^e + 2^(e-53) + 1, just above the midpoint between 2^e and the next
    double, and dropping its last digit puts it below; e = 67, 68 and 80."""
    for s in ("147573952589676429313", "-295147905179352858625", "  1208925819614629308923905"):
        for radix in (None, 0, 10):
            corpus.add("parse-int.vectors", "parse-int", parse_int_record(s, radix), since=4)


# --------------------------------------------------------------------------------------------------
# Numeric literal values.

def literal_inputs() -> list[str]:
    out = ["0", "1", "0.5", ".5", "5.", "0.e1", "1e21", "1E21", "1e+21", "1e-7", "1.5e-7", "0x1F", "0X1F", "0b1011",
           "0B1011", "0o777", "0O777", "1_000_000", "1_0.0_1e1_0", ".5e1_0", "0x1_F", "0b1_0", "0o7_7",
           "123.456e-7", "9007199254740993", "9007199254740995", "5e-324", "2.4703282292062327e-324",
           "1.7976931348623157e308", "1.7976931348623159e308", "1e400", "0.1", "3.141592653589793", "12345678901234567890",
           "123456789012345678901234567890", "1.00000000000000011102230246251565404236316680908203125",
           "1.00000000000000011102230246251565404236316680908203124",
           "1.00000000000000011102230246251565404236316680908203126",
           "010", "0777", "08", "09", "019", "08.5", "09e1", "07.5", "0_1", "1__0", "1_", "1_.5", "1._5", "1e_5",
           "1e", "0x", "0x_1", "0x1_", "0b2", "0o8", "0xG", "3in", "1a",
           "0n", "1n", "123n", "0x1Fn", "0b11n", "0o17n", "1_000n", "01n", "08n", "1.5n", "1e3n", ".5n",
           "18446744073709551616n", "0xFFFFFFFFFFFFFFFFFn", "123456789012345678901234567890n"]
    for e in (52, 53, 54, 63, 64, 65):
        for k in (-1, 0, 1):
            value = 2 ** e + k
            out.append("0x" + R._digits(value, 16))
            out.append("0b" + R._digits(value, 2))
            out.append("0o" + R._digits(value, 8))
            out.append(str(value))
            out.append(str(value) + "n")
        if e >= 53:
            halfway = 2 ** e + 2 ** (e - 53)
            for k in (-1, 0, 1):
                out.append("0x" + R._digits(halfway + k, 16))
    return out


def literal_vectors(corpus: Corpus) -> None:
    clause = (f"{SPEC}#sec-literals-numeric-literals {SPEC}#sec-numericvalue {SPEC}#sec-static-semantics-mv "
              f"{SPEC}#sec-roundmvresult")
    seen = set()
    for text in literal_inputs():
        for strict in (False, True):
            if (text, strict) in seen:
                continue
            seen.add((text, strict))
            result = R.numeric_literal(text, strict)
            if result.kind == "number":
                out = value_out([f64(b) for b in result.numbers])
            elif result.kind == "bigint":
                out = f"value: bigint:{result.bigint}"
            else:
                out = "error: SyntaxError"
            script = f'"use strict"; {text}' if strict else text
            record = {"op": "NumericLiteral", "in": f"{string_value(text)} bool:{'true' if strict else 'false'}",
                      "out": out, "prov": f"{clause} {GENERATOR}", "script": script}
            if result.kind == "number" and len(result.numbers) > 1:
                record["note"] = "more than 20 significant digits: RoundMVResult admits either option"
            corpus.add("literals.vectors", "literal", record)


# --------------------------------------------------------------------------------------------------
# Array-index and canonical numeric keys.

# Every key's ToString must have one admissible value: "9007199254740992" is left out because
# Number::toString(2**53) admits both "9007199254740992" and "9007199254740993".
KEY_INPUTS = ["0", "1", "10", "4294967294", "4294967295", "4294967296", "-0", "-1", "01", "1.0", "1e3", "1000",
              " 1", "1 ", "+1", "1.5", "0.1", "1e+21", "1e21", "Infinity", "-Infinity", "NaN", "9007199254740991",
              "9007199254740990", "0x10", "", "abc", "1e-7", "1e-6", "0.000001"]


def key_vectors(corpus: Corpus) -> None:
    for s in KEY_INPUTS:
        literal = js_string_literal(s)
        if R.is_array_index(s):
            length = R.to_fraction(R.canonical_numeric_index(s)) + 1
        else:
            length = 0
        record = {"op": "array index", "in": string_value(s),
                  "out": value_out([f64(R.from_fraction(R.Fraction(length)))]),
                  "prov": f"{SPEC}#sec-object-type {SPEC}#sec-canonicalnumericindexstring {GENERATOR}",
                  "script": f"var a = []; a[{literal}] = 0; a.length",
                  "note": "an array index grows the length; any other key leaves it 0"}
        corpus.add("keys.vectors", "array-index", record)
        # A typed array of length 2: a canonical numeric key is an integer-indexed access (valid only
        # for 0 and 1, otherwise silently ignored); any other key is an ordinary property.
        n = R.canonical_numeric_index(s)
        if n is None:
            expected = 13  # set as an ordinary property: read back (10) and one more key (3)
        elif not R.is_nan(n) and R.is_finite(n) and n != R.NEGATIVE_ZERO and R.to_fraction(n) in (0, 1):
            expected = 12  # a valid index: read back (10), keys stay 2
        else:
            expected = 2  # canonical but not a valid index: ignored, reads undefined
        record = {"op": "CanonicalNumericIndexString", "in": string_value(s),
                  "out": value_out([f64(R.from_fraction(R.Fraction(expected)))]),
                  "prov": f"{SPEC}#sec-canonicalnumericindexstring {GENERATOR}",
                  "script": (f"var t = new Uint8Array(2); t[{literal}] = 7; "
                             f"(t[{literal}] === 7 ? 10 : 0) + Object.keys(t).length"),
                  "note": "12: a valid integer index; 13: not canonical, an ordinary property; 2: canonical, not an index"}
        corpus.add("keys.vectors", "canonical-numeric", record)


# --------------------------------------------------------------------------------------------------

def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--proplist", help="the pinned UCD 17.0.0 PropList.txt, to re-derive the Zs set from")
    parser.add_argument("--out", required=True, type=Path)
    args = parser.parse_args(argv)
    if args.proplist:
        digest = hashlib.sha256(Path(args.proplist).read_bytes()).hexdigest()
        if digest != PROPLIST_SHA256:
            print(f"PropList.txt does not match its pin: {digest}", file=sys.stderr)
            return 2
        if R.zs_from_proplist(args.proplist) != R.SPACE_SEPARATORS:
            print("the Zs set derived from PropList.txt differs from numref.SPACE_SEPARATORS", file=sys.stderr)
            return 2
    else:
        print("note: --proplist not given; the Zs set was not re-derived from the pinned file", file=sys.stderr)
    corpus = Corpus()
    number_to_string_vectors(corpus)
    radix_vectors(corpus)
    radix_argument_vectors(corpus)
    fixed_vectors(corpus)
    exponential_vectors(corpus)
    precision_vectors(corpus)
    number_vectors(corpus)
    parse_int_vectors(corpus)
    literal_vectors(corpus)
    key_vectors(corpus)
    revision_4_vectors(corpus)
    corpus.classify(DIVERGENCES)
    corpus.write(args.out)
    for name, records in sorted(corpus.files.items()):
        print(f"{name}: {len(records)} vectors")
    return 0


if __name__ == "__main__":
    sys.exit(main())
