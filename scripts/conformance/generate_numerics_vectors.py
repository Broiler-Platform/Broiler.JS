#!/usr/bin/env python3
"""Generate the numerics area of the shared conformance corpus.

    python3 scripts/conformance/generate_numerics_vectors.py --out conformance/vectors/numerics

Every expected value comes from `numericsref.py`, the corpus's independent reference computation in
exact arithmetic; no expected value is taken from either engine. The populations are milestone
SJL-4.2's: the specification's integer conversions with their edges; Float16 encoding, decoding and
rounding, every NaN payload included; the Math kernels an engine implements itself, with their
special cases; the rounding modes of Temporal at every mode, a range of increments and their ties;
and BigInt arithmetic at Broiler.VM's width ceiling and past it, its text at every radix,
StringToBigInt with the specification's white space, asIntN and asUintN with very large bit counts,
and its conversion to and comparison with Number at 2^53 and beyond.

The output is deterministic: the same inputs give the same bytes. Vector identifiers are stable and
are never reused; a later revision appends.
"""

from __future__ import annotations

import argparse
import sys
from fractions import Fraction
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
# A vector writes BigInts of up to 70,000 bits in decimal, past CPython's default conversion limit.
sys.set_int_max_str_digits(0)

import numericsref as N  # noqa: E402
import numref as R  # noqa: E402
from generate_number_vectors import double_expression, f64, js_string_literal, splitmix64, string_value  # noqa: E402

# The corpus revision this generator writes, and the revision its vectors were added in. Revision 7
# classified the area's first runs (numerics-divergences.txt) and rebuilt one vector's input
# (big_literal); revision 8 named the operations and inputs so that each vector's op and in decide its
# outcome, as the library-level runner reads them; revision 9 retired every divergence the two
# engines' cut-overs corrected (numerics-divergences.txt).
CORPUS_REVISION = 9
FIRST_REVISION = 6
GENERATOR = "ref:numericsref@1"
SPEC = "spec:es2026@0248456c"
ENGINES = ("broiler-js", "broiler-vm")
DIVERGENCES = Path(__file__).resolve().parent / "numerics-divergences.txt"

# Broiler.VM's BigInt policy: a value whose width (numericsref.bit_width) exceeds this is refused with a
# RangeError (Broiler.VM JsBigInt.MaximumBits, kept by JSD-0067 decision 3). Broiler.JS passes the
# unbounded policy (decision SJL-D-15 (a)) and refuses only at its representation's limit.
VM_MAXIMUM_BITS = 1 << 20
VM_POLICY = ("Broiler.VM's BigInt width policy (JsBigInt.MaximumBits, 2^20 bits by BigInteger.GetBitLength), "
             "which JSD-0067 decision 3 keeps")
JS_POLICY = "Broiler.JS's unbounded BigInt width policy (decision SJL-D-15 (a))"
LIMIT = ("the specification's value cannot be represented by any engine; each plan answers a RangeError at its "
         "representation's limit (JSD-0067 decision 2, Broiler.JS SharedLibraries.md SJL-4.5)")


def clause(*ids: str) -> str:
    return " ".join(f"{SPEC}#{i}" for i in ids)


def number(bits: int) -> str:
    return f"value: {f64(bits)}"


def integer(value: int) -> str:
    return number(N.double(value))


def bigint(value: int) -> str:
    return f"value: bigint:{value}"


def boolean(value: bool) -> str:
    return f"value: bool:{'true' if value else 'false'}"


def alternatives(values: tuple[int, ...]) -> str:
    return " | ".join(number(v) for v in values)


# The widest BigInt literal a vector writes. Broiler.VM's wide manifest admits no BigInt literal wider
# than 65,536 bits, a limit of its compiler and not of the area, so a wider value is built from pieces
# (corpus revision 7).
LITERAL_BITS = 32768


def big_literal(value: int) -> str:
    """A BigInt literal: decimal below 2^64, hexadecimal above, parenthesised when negative, and built
    from shifted pieces of LITERAL_BITS bits when it is wider than twice that."""
    magnitude = abs(value)
    if magnitude.bit_length() > 2 * LITERAL_BITS:
        pieces = []
        while magnitude:
            pieces.append(magnitude & ((1 << LITERAL_BITS) - 1))
            magnitude >>= LITERAL_BITS
        text = f"0x{pieces[-1]:X}n"
        for piece in reversed(pieces[:-1]):
            text = f"(({text} << {LITERAL_BITS}n) | 0x{piece:X}n)"
    else:
        text = f"{magnitude}n" if magnitude < 1 << 64 else f"0x{magnitude:X}n"
    return f"(-{text})" if value < 0 else text


# --------------------------------------------------------------------------------------------------
# Records.


class Corpus:
    def __init__(self) -> None:
        self.files: dict[str, list[dict[str, str]]] = {}
        self.ids: set[str] = set()
        self.counters: dict[str, int] = {}

    def add(self, file: str, group: str, record: dict[str, str], since: int = FIRST_REVISION) -> None:
        self.counters[group] = self.counters.get(group, 0) + 1
        vector_id = f"numerics/{group}/{self.counters[group]:04d}"
        assert vector_id not in self.ids
        self.ids.add(vector_id)
        full = {"id": vector_id, "area": "numerics"}
        full.update(record)
        full.setdefault("class", "normative")
        full["since"] = str(since)
        assert "\n" not in full["script"]
        self.files.setdefault(file, []).append(full)

    def classify(self, path: Path) -> None:
        """Applies numerics-divergences.txt, as generate_number_vectors.py applies its own file."""
        if not path.exists():
            return
        reasons: dict[str, str] = {}
        by_id = {record["id"]: record for records in self.files.values() for record in records}
        for line_number, line in enumerate(path.read_text(encoding="utf-8").splitlines(), 1):
            if not line or line.startswith("#"):
                continue
            if line.startswith("reason "):
                key, _, text = line[len("reason "):].partition(": ")
                if not text or key in reasons:
                    raise SystemExit(f"{path.name}:{line_number}: a malformed or repeated reason")
                reasons[key] = text
                continue
            engine, key, vector_id = line.split(" ")
            record = by_id.get(vector_id)
            if engine not in ENGINES or key not in reasons or record is None:
                raise SystemExit(f"{path.name}:{line_number}: '{line}' names no engine, reason or vector")
            if f"divergence.{engine}" in record or (
                    record["class"] == "impl-defined" and f"per-engine.{engine}" not in record):
                raise SystemExit(f"{path.name}:{line_number}: {vector_id} cannot take this classification")
            if record["class"] == "normative":
                record["class"] = "divergence"
            record[f"divergence.{engine}"] = reasons[key]

    def write(self, out: Path) -> None:
        out.mkdir(parents=True, exist_ok=True)
        order = ["id", "area", "op", "in", "out", "class", "per-engine.broiler-js", "per-engine.broiler-vm",
                 "divergence.broiler-js", "divergence.broiler-vm", "prov", "script", "note", "since"]
        for name, records in sorted(self.files.items()):
            lines = [
                "# Generated by scripts/conformance/generate_numerics_vectors.py; do not edit by hand.",
                "# Expected values: scripts/conformance/numericsref.py, an exact reference computation.",
                "schema: 1",
            ]
            for record in records:
                lines.append("")
                for key in order:
                    if key in record:
                        lines.append(f"{key}: {record[key]}")
            (out / name).write_text("\n".join(lines) + "\n", encoding="utf-8", newline="\n")


def record(op: str, inputs: str, out: str, prov: str, script: str, note: str | None = None,
           js: str | None = None, vm: str | None = None) -> dict[str, str]:
    """A vector; per-engine expectations make it impl-defined."""
    result = {"op": op, "in": inputs, "out": out}
    if js is not None or vm is not None:
        result["class"] = "impl-defined"
        if js is not None:
            result["per-engine.broiler-js"] = js
        if vm is not None:
            result["per-engine.broiler-vm"] = vm
    result["prov"] = f"{prov} {GENERATOR}".strip()
    result["script"] = script
    if note:
        result["note"] = note
    return result


# --------------------------------------------------------------------------------------------------
# Populations.


def unique(values: list[int]) -> list[int]:
    seen: set[int] = set()
    out = []
    for value in values:
        if value not in seen:
            seen.add(value)
            out.append(value)
    return out


def d(text: str) -> int:
    values = R.string_to_number(text)
    assert len(values) == 1, text
    return values[0]


def exact(value: Fraction | int) -> int:
    bits = N.double(Fraction(value))
    assert R.to_fraction(bits) == Fraction(value), value
    return bits


def signed(values: list[int]) -> list[int]:
    return unique([v for b in values for v in (b, b ^ R.SIGN_BIT)])


SPECIALS = [0, R.NEGATIVE_ZERO, R.CANONICAL_NAN, R.POSITIVE_INFINITY, R.NEGATIVE_INFINITY]


def random_doubles(seed: int, count: int, low: int, high: int) -> list[int]:
    """Seeded doubles with binary exponents in [low, high), of both signs."""
    out = []
    for draw in splitmix64(seed):
        exponent = low + (draw >> 52) % (high - low)
        mantissa = draw & R.FRACTION_MASK
        if exponent >= -1022:
            bits = ((exponent + 1023) << 52) | mantissa
        else:
            # A subnormal: the significand shifted down to the exponent, never zero.
            bits = max(((1 << 52) | mantissa) >> (-1022 - exponent), 1)
        out.append(bits | (R.SIGN_BIT if draw & 1 else 0))
        if len(out) == count:
            return out
    raise AssertionError


def random_ints(seed: int, count: int, words: int) -> list[int]:
    out = []
    draws = splitmix64(seed)
    for _ in range(count):
        value = 0
        for _ in range(words):
            value = (value << 64) | next(draws)
        out.append(-value if next(draws) & 1 else value)
    return out


# --------------------------------------------------------------------------------------------------
# The integer conversions.

CONVERSION_EDGES = signed([exact(v) for v in [
    Fraction(1, 2), 1, Fraction(3, 2), Fraction(5, 2), Fraction(255, 2),
    127, 128, Fraction(257, 2), 255, Fraction(511, 2), 256, 32767, 32768, 65535, 65536,
    2 ** 31 - 1, 2 ** 31, Fraction(2 ** 32 + 1, 2), 2 ** 32 - 1, 2 ** 32, 2 ** 32 + 1, 2 ** 52 + 1,
    2 ** 53 - 1, 2 ** 53, 2 ** 53 + 2, 2 ** 63, 2 ** 64, 2 ** 64 + 2 ** 12, 2 ** 84 + 2 ** 33, 10 ** 21]]
    + [d("0.9999999999999999"), d("4294967295.5"), d("1e-7"), R.SMALLEST_SUBNORMAL, R.MAX_FINITE,
       d("1.7976931348623157e308")])
CONVERSION_POPULATION = unique(SPECIALS + CONVERSION_EDGES + random_doubles(0x534A4C342D493031, 24, 0, 90))  # "SJL4-I01"

MODULAR = [
    # op, width, signed, script template, clause
    ("ToInt32", 32, True, "{x} | 0", "sec-toint32"),
    ("ToUint32", 32, False, "{x} >>> 0", "sec-touint32"),
    ("ToInt16", 16, True, "new Int16Array([{x}])[0]", "sec-toint16"),
    ("ToUint16", 16, False, "new Uint16Array([{x}])[0]", "sec-touint16"),
    ("ToInt8", 8, True, "new Int8Array([{x}])[0]", "sec-toint8"),
    ("ToUint8", 8, False, "new Uint8Array([{x}])[0]", "sec-touint8"),
]

# A Proxy that answers a length of 2^53 - 1 and has every index: lastIndexOf on it returns the index
# its fromIndex argument chooses, which shows ToIntegerOrInfinity's result (ECMA-262 23.1.3.20).
PROXY_EVERY_INDEX = ("new Proxy({}, {get: function (t, k) { return k === \"length\" ? 9007199254740991 : 0; }, "
                     "has: function () { return true; }})")


def proxy_with_length(x: str) -> str:
    return ("new Proxy({}, {get: function (t, k) { return k === \"length\" ? " + x + " : 0; }, "
            "has: function () { return true; }})")


def conversion_vectors(corpus: Corpus) -> None:
    file = "integer-conversions.vectors"
    for op, width, is_signed, template, cid in MODULAR:
        for bits in CONVERSION_POPULATION:
            x = double_expression(bits)
            corpus.add(file, op.lower(), record(op, f64(bits), integer(N.modular(bits, width, is_signed)),
                                                clause(cid), template.format(x=x)))
    for bits in CONVERSION_POPULATION:
        corpus.add(file, "touint8clamp", record("ToUint8Clamp", f64(bits), integer(N.to_uint8_clamp(bits)),
                                                clause("sec-touint8clamp"),
                                                f"new Uint8ClampedArray([{double_expression(bits)}])[0]"))
    for bits in CONVERSION_POPULATION:
        n = N.to_integer_or_infinity(bits)
        length = N.MAX_SAFE_INTEGER
        if n == R.NEGATIVE_INFINITY:
            k = -1
        elif n == R.POSITIVE_INFINITY:
            k = length - 1
        else:
            value = int(R.to_fraction(n))
            k = min(value, length - 1) if value >= 0 else length + value
            if k < 0:
                k = -1
        corpus.add(file, "tointegerorinfinity", record(
            "ToIntegerOrInfinity", f64(bits), integer(k),
            clause("sec-tointegerorinfinity", "sec-array.prototype.lastindexof"),
            f"Array.prototype.lastIndexOf.call({PROXY_EVERY_INDEX}, 0, {double_expression(bits)})",
            "the script shows ToIntegerOrInfinity(x) as the index lastIndexOf starts from on an array-like of "
            "length 2^53 - 1 that has every index: min(n, 2^53 - 2) for n >= 0, 2^53 - 1 + n for n < 0, and -1 "
            "for -Infinity or below -(2^53 - 1)"))
    for bits in CONVERSION_POPULATION:
        length = N.to_length(bits)
        corpus.add(file, "tolength", record(
            "ToLength", f64(bits), integer(length - 1),
            clause("sec-tolength", "sec-lengthofarraylike", "sec-array.prototype.lastindexof"),
            f"Array.prototype.lastIndexOf.call({proxy_with_length(double_expression(bits))}, 0)",
            "the script shows ToLength(x) - 1, the index lastIndexOf starts from on an array-like whose length "
            "property is x and that has every index; -1 for a length of 0"))
    index_population = unique(CONVERSION_POPULATION + [exact(2 ** 53 - 2), exact(-(2 ** 53) + 1)])
    for bits in index_population:
        index = N.to_index(bits)
        corpus.add(file, "toindex", record(
            "ToIndex", f64(bits), "error: RangeError" if index is None else bigint(0),
            clause("sec-toindex", "sec-bigint.asuintn"),
            f"BigInt.asUintN({double_expression(bits)}, 0n)",
            "the script shows ToIndex's range rule: BigInt.asUintN converts its bits argument with ToIndex, and "
            "of 0n every width is 0n"))
    big_population = [0, 1, -1, 2 ** 63 - 1, 2 ** 63, -(2 ** 63), -(2 ** 63) - 1, 2 ** 64 - 1, 2 ** 64, 2 ** 64 + 1,
                      -(2 ** 64), -(2 ** 64) - 1, 2 ** 100 + 5, -(2 ** 100) - 5, 2 ** 128 - 1, -(2 ** 128) + 1]
    big_population += random_ints(0x534A4C342D493032, 8, 2)  # "SJL4-I02"
    for value in big_population:
        corpus.add(file, "tobigint64", record(
            "ToBigInt64", f"bigint:{value}", bigint(N.to_big_int64(value)), clause("sec-tobigint64"),
            f"new BigInt64Array([{big_literal(value)}])[0]"))
        corpus.add(file, "tobiguint64", record(
            "ToBigUint64", f"bigint:{value}", bigint(N.to_big_uint64(value)), clause("sec-tobiguint64"),
            f"new BigUint64Array([{big_literal(value)}])[0]"))


# --------------------------------------------------------------------------------------------------
# Float16.

def half_midpoints() -> list[int]:
    """Doubles at, just below and just above the midpoints between binary16 values, and the
    double-rounding traps: values that round one way directly and the other way through binary32."""
    out = []
    for half in [0x0000, 0x0001, 0x0002, 0x03FF, 0x0400, 0x0401, 0x3BFF, 0x3C00, 0x3C01, 0x3C02, 0x7BFE, 0x7BFF]:
        low = N.decode(half, N.BINARY16)
        high = N.decode(half + 1, N.BINARY16) if half < 0x7BFF else Fraction(2 ** 16)
        middle = (low + high) / 2
        bits = exact(middle)
        out += [bits - 1, bits, bits + 1]
        # Just above the midpoint by less than half a binary32 unit: binary32 rounds it to the midpoint.
        out.append(N.double(middle + (high - low) * Fraction(1, 1 << 20)))
    out += [exact(Fraction(1) + Fraction(1, 2 ** 11) + Fraction(1, 2 ** 40)),
            exact(Fraction(1) + Fraction(3, 2 ** 11) - Fraction(1, 2 ** 40)),
            exact(65520), exact(65519), N.double(Fraction(65520) - Fraction(1, 2 ** 30)), exact(65536), exact(2 ** 17)]
    return signed(out)


FLOAT16_POPULATION = unique(SPECIALS + half_midpoints() + signed([d("0.1"),
                                                                    d("0.3333333333333333"), d("3.141592653589793"),
                                                                    d("1e-5"), d("6e-8"), d("5.960464477539063e-8"),
                                                                    d("1e10"), R.SMALLEST_SUBNORMAL, R.MAX_FINITE])
                            + random_doubles(0x534A4C342D463031, 24, -26, 17))  # "SJL4-F01"

# Double NaNs of several payloads and both signs, for the encoding the specification leaves open.
NAN_PAYLOADS = [0x7FF8000000000000, 0xFFF8000000000000, 0x7FF0000000000001, 0xFFF0000000000001, 0x7FF4000000000000,
                0x7FF8000000000001, 0x7FF8040000000000, 0x7FFC000000000000, 0x7FFFFFFFFFFFFFFF, 0xFFFFFFFFFFFFFFFF,
                0x7FF0040000000000, 0x7FFA5A5A5A5A5A5A, 0xFFF55AA55AA55AA5, 0x7FF0000000000400, 0x7FF00000003FF000,
                0x7FF000FFC0000000]

ENCODE_SCRIPT = "(function () {{ var v = new DataView(new ArrayBuffer(2)); v.setFloat16(0, {x}); return v.getUint16(0); }})()"
DECODE_SCRIPT = "(function () {{ var v = new DataView(new ArrayBuffer(2)); v.setUint16(0, {h}); return v.getFloat16(0); }})()"
NAN_TEST = ("(function () {{ var v = new DataView(new ArrayBuffer(2)); v.setFloat16(0, {x}); var b = v.getUint16(0); "
            "return (b & 0x7C00) === 0x7C00 && (b & 0x3FF) !== 0; }})()")


def float16_vectors(corpus: Corpus) -> None:
    file = "float16.vectors"
    for bits in FLOAT16_POPULATION:
        x = double_expression(bits)
        corpus.add(file, "f16round", record("Math.f16round", f64(bits), number(N.f16round(bits)),
                                            clause("sec-math.f16round"), f"Math.f16round({x})"))
    for bits in FLOAT16_POPULATION:
        if R.is_nan(bits):
            continue
        corpus.add(file, "encode", record(
            "Float16 encode", f64(bits), integer(N.float16_encode(bits)),
            clause("sec-numerictorawbytes", "sec-dataview.prototype.setfloat16"),
            ENCODE_SCRIPT.format(x=double_expression(bits))))
    for bits in NAN_PAYLOADS:
        x = double_expression(bits)
        corpus.add(file, "encode-nan", record(
            "Float16 encode is a NaN", f"str:\"NaN {bits:016X}\"", boolean(True),
            clause("sec-numerictorawbytes", "sec-rawbytestonumeric"), NAN_TEST.format(x=x),
            "a NaN is written as some binary16 NaN encoding; which one is the implementation's (NumericToRawBytes "
            "step 1.b), and encode-nan-bits vectors pin each engine's choice"))
    for bits in NAN_PAYLOADS:
        x = double_expression(bits)
        corpus.add(file, "encode-nan-bits", record(
            "Float16 encode", f"str:\"NaN {bits:016X}\"", integer(N.FLOAT16_CANONICAL_NAN),
            clause("sec-numerictorawbytes"), ENCODE_SCRIPT.format(x=x),
            "the encoding of a NaN is the implementation's; out holds the canonical quiet NaN 0x7E00. The double "
            "NaN is built from its bits, and an engine that keeps a NaN's payload keeps it to the write. "
            "Broiler.VM writes one canonical NaN (JsFloat16.QuietNaN); Broiler.JS keeps the sign and the ten "
            "most significant bits of the payload and quiets it (SharedLibraries.md, SJL-4.2). The shared "
            "Float16.Encode takes the policy as a parameter (JSD-0067 decision 5)",
            js=integer(N.float16_nan_preserving(bits)), vm=integer(N.FLOAT16_CANONICAL_NAN)))
    halves = []
    for sign in (0, 0x8000):
        for field in range(32):
            for fraction in (0, 1, 2, 0x1FF, 0x200, 0x3FE, 0x3FF):
                halves.append(sign | (field << 10) | fraction)
    halves += [h for h in range(0x10000) if N.is_nan_in(h, N.BINARY16)]
    for half in unique(halves):
        corpus.add(file, "decode", record(
            "Float16 decode", f"int:{half}", number(N.float16_decode(half)),
            clause("sec-rawbytestonumeric", "sec-dataview.prototype.getfloat16"), DECODE_SCRIPT.format(h=half),
            "every binary16 NaN payload decodes to NaN" if N.is_nan_in(half, N.BINARY16) else None))


# --------------------------------------------------------------------------------------------------
# Math.

ROUND_POPULATION = unique(SPECIALS + signed([
    exact(Fraction(1, 2)), d("0.49999999999999994"), exact(Fraction(3, 2)), exact(Fraction(5, 2)),
    d("0.7"), d("1.4999999999999998"), exact(2 ** 52 - Fraction(1, 2)),
    exact(2 ** 52 + 1), exact(2 ** 53), exact(Fraction(2 ** 51 + 1, 2)), d("1e-300"), R.SMALLEST_SUBNORMAL,
    R.MAX_FINITE, d("123.456")]) + random_doubles(0x534A4C342D4D3031, 24, -4, 54))


def pow_population() -> tuple[list[int], list[int]]:
    bases = SPECIALS + signed([exact(1), exact(Fraction(1, 2)), exact(2), exact(3), d("1.0000000000000002"),
                               d("0.9999999999999999")])
    exponents = SPECIALS + signed([exact(1), exact(2), exact(3), exact(Fraction(1, 2)), exact(2 ** 53),
                                   exact(2 ** 53 - 1), exact(2 ** 53 + 2), R.MAX_FINITE, exact(1075), exact(1024)])
    return unique(bases), unique(exponents)


def literal(bits: int) -> str | None:
    """A JavaScript expression for a Number whose decimal is short and exact, or None."""
    if R.is_nan(bits):
        return "NaN"
    if bits == R.POSITIVE_INFINITY:
        return "Infinity"
    if bits == R.NEGATIVE_INFINITY:
        return "(-Infinity)"
    if bits == R.NEGATIVE_ZERO:
        return "(-0)"
    value = R.to_fraction(bits)
    if value.denominator in (1, 2) and abs(value) < 2 ** 53:
        magnitude = abs(value)
        text = str(magnitude.numerator) if magnitude.denominator == 1 else f"{magnitude.numerator // 2}.5"
        return f"(-{text})" if value < 0 else text
    return None


def math_vectors(corpus: Corpus) -> None:
    file = "math.vectors"
    for bits in ROUND_POPULATION:
        corpus.add(file, "round", record("Math.round", f64(bits), number(N.math_round(bits)), clause("sec-math.round"),
                                         f"Math.round({double_expression(bits)})"))
    bases, exponents = pow_population()
    for base in bases:
        for exponent in exponents:
            result = N.exponentiate(base, exponent)
            inputs = f"{f64(base)} {f64(exponent)}"
            a, b = double_expression(base), double_expression(exponent)
            forms = [("pow", "Math.pow", f"Math.pow({a}, {b})", ("sec-math.pow", "sec-numeric-types-number-exponentiate")),
                     ("exponentiate", "Number::exponentiate", f"{a} ** {b}", ("sec-numeric-types-number-exponentiate",)),
                     ("exponentiate-numeric", "Number::exponentiate", f"(+{a}) ** (+{b})",
                      ("sec-numeric-types-number-exponentiate",))]
            la, lb = literal(base), literal(exponent)
            if la is not None and lb is not None:
                forms.append(("exponentiate-literal", "Number::exponentiate", f"{la} ** {lb}",
                              ("sec-numeric-types-number-exponentiate",)))
            for group, op, script, ids in forms:
                if isinstance(result, int):
                    corpus.add(file, group, record(op, inputs, number(result), clause(*ids), script))
                elif len(result.faithful) == 1:
                    # The last step is implementation-approximated. Only results every approximation
                    # gives are pinned: an exact double, or a value beyond 2^1100 or below 2^-1100. An
                    # inexact result is System.Math.Pow's in both engines and gets no vector (milestone
                    # SJL-4.2: such functions get vectors only where an engine wraps them).
                    out = number(result.correct)
                    corpus.add(file, group, record(
                        op, inputs, out, clause(*ids), script,
                        "the last step is implementation-approximated; the exact result is a double, or lies beyond "
                        "2^1100 or below 2^-1100, so every approximation the plans admit gives it",
                        js=out, vm=out))
    hypot_vectors(corpus, file)
    for bits in unique(SPECIALS + signed([exact(8), exact(27), exact(Fraction(1, 8)), exact(2 ** -1071), exact(2 ** 1020),
                                          exact(2), exact(3), exact(10), d("1e300"), d("1e-300"), R.SMALLEST_SUBNORMAL,
                                          R.MAX_FINITE, d("0.001"), exact(2 ** 53 + 2)])
                       + random_doubles(0x534A4C342D4D3032, 24, -1074, 1023)):
        result = N.cbrt(bits)
        script = f"Math.cbrt({double_expression(bits)})"
        if isinstance(result, int):
            corpus.add(file, "cbrt", record("Math.cbrt", f64(bits), number(result), clause("sec-math.cbrt"), script))
        else:
            out = number(result.correct)
            corpus.add(file, "cbrt", record(
                "Math.cbrt", f64(bits), out, clause("sec-math.cbrt"), script,
                "implementation-approximated; both plans take the correctly rounded cube root (JSD-0067 decision 5)",
                js=out, vm=out))
    near_zero = signed([exact(Fraction(1, 2 ** k)) for k in (1, 2, 5, 10, 11, 20, 27, 30, 40, 52, 53, 60, 100, 500, 1022, 1074)])
    for name, function, cid, extra in (
            ("expm1", N.expm1, "sec-math.expm1", [d("1"), d("0.5"), d("-0.5"), d("10"), d("-10"), d("-40"), d("-745"),
                                                  d("700"), d("709.78"), d("709.79"), d("1e-5"), d("-1e-5")]),
            ("log1p", N.log1p, "sec-math.log1p", [d("1"), d("-0.5"), d("-0.9999999999999999"), d("-1"), d("-1.5"),
                                                  d("1e-5"), d("-1e-5"), d("10"), d("1e300"), R.MAX_FINITE, d("0.75")])):
        population = unique(SPECIALS + near_zero + extra + random_doubles(0x534A4C342D4D3033 + len(name), 24, -60, 9))
        for bits in population:
            result = function(bits)
            script = f"Math.{name}({double_expression(bits)})"
            if isinstance(result, int):
                corpus.add(file, name, record(f"Math.{name}", f64(bits), number(result), clause(cid), script))
            else:
                faithful = alternatives(result.faithful)
                corpus.add(file, name, record(
                    f"Math.{name}", f64(bits), number(result.correct), clause(cid), script,
                    "implementation-approximated; out is correctly rounded, and both plans commit to within one unit "
                    "in the last place over the whole domain (JSD-0067 decision 5): either double bracketing the exact value",
                    js=faithful, vm=faithful))
    sum_precise_vectors(corpus, file)
    for bits in unique(SPECIALS + signed([exact(Fraction(1, 2)), exact(Fraction(3, 2)), d("0.1"), d("1.0000000596046448"),
                                          exact(1 + Fraction(1, 2 ** 24)), exact(1 + Fraction(3, 2 ** 24)),
                                          exact(Fraction(1, 2 ** 149)), exact(Fraction(1, 2 ** 150)),
                                          exact(Fraction(3, 2 ** 151)), exact(2 ** 128 - 2 ** 104),
                                          exact(2 ** 128 - 2 ** 103), N.double(Fraction(2 ** 128 - 2 ** 103) - 1),
                                          d("3.4028234663852886e38"), R.MAX_FINITE, R.SMALLEST_SUBNORMAL])
                       + random_doubles(0x534A4C342D4D3034, 24, -160, 130)):
        corpus.add(file, "fround", record("Math.fround", f64(bits), number(N.fround(bits)), clause("sec-math.fround"),
                                          f"Math.fround({double_expression(bits)})"))
    for bits in unique(SPECIALS + signed([exact(1), exact(2 ** 31), exact(2 ** 32), exact(2 ** 32 + 1), exact(Fraction(1, 2)),
                                          exact(2 ** 31 - 1), exact(65535), d("1e21"), exact(2 ** 53 + 2)])
                       + random_doubles(0x534A4C342D4D3035, 16, 0, 70)):
        corpus.add(file, "clz32", record("Math.clz32", f64(bits), integer(N.clz32(bits)), clause("sec-math.clz32", "sec-touint32"),
                                         f"Math.clz32({double_expression(bits)})"))
    pairs = [(2 ** 32 - 1, 5), (-5, 12), (2 ** 32 - 1, 2 ** 32 - 1), (2 ** 31, 2), (Fraction(3, 2), Fraction(5, 2)),
             (2 ** 53 + 2, 3), (10 ** 21, 3), (65537, 65537), (-(2 ** 31), -(2 ** 31)), (2 ** 31 - 1, 2 ** 31 - 1)]
    imul_population = [(exact(a), exact(b)) for a, b in pairs] + [(R.CANONICAL_NAN, exact(1)), (R.POSITIVE_INFINITY, exact(2)),
                                                                   (R.NEGATIVE_ZERO, exact(3))]
    randoms = random_doubles(0x534A4C342D4D3036, 16, 0, 70)
    imul_population += list(zip(randoms[::2], randoms[1::2]))
    for a, b in imul_population:
        corpus.add(file, "imul", record("Math.imul", f"{f64(a)} {f64(b)}", integer(N.imul(a, b)), clause("sec-math.imul", "sec-touint32"),
                                        f"Math.imul({double_expression(a)}, {double_expression(b)})"))


def hypot_vectors(corpus: Corpus, file: str) -> None:
    def scaled(values: list[int], k: int) -> list[int]:
        return [exact(Fraction(v) * Fraction(2) ** k) for v in values]

    cases: list[list[int]] = [[], [0], [R.NEGATIVE_ZERO], [R.NEGATIVE_ZERO, R.NEGATIVE_ZERO], [0, R.NEGATIVE_ZERO],
                              [R.CANONICAL_NAN], [R.CANONICAL_NAN, R.POSITIVE_INFINITY], [R.NEGATIVE_INFINITY, R.CANONICAL_NAN],
                              [exact(1), R.CANONICAL_NAN], [R.NEGATIVE_INFINITY], [exact(-3)], [R.SMALLEST_SUBNORMAL],
                              [R.MAX_FINITE], [R.MAX_FINITE, exact(1)]]
    for triple in ([3, 4], [5, 12], [8, 15], [20, 21], [2, 3, 6], [1, 4, 8], [2, 10, 11], [1, 2, 2]):
        for k in (0, 1000, -1000, 1018, -1074, -1060):
            cases.append(scaled(triple, k))
    cases += [[exact(3), exact(-4)], [exact(-5), R.NEGATIVE_ZERO, exact(12)]]
    cases += [[exact(1), exact(1)], [d("1e308"), d("1e308")], [d("1e-310"), d("1e-310")], [exact(1), d("1e-8")],
              [R.MAX_FINITE, R.MAX_FINITE], [d("1e200"), d("1e-200")], [exact(1), exact(2), exact(3)],
              [d("0.1"), d("0.2"), d("0.3"), d("0.4")], [R.SMALLEST_SUBNORMAL, R.SMALLEST_SUBNORMAL]]
    randoms = random_doubles(0x534A4C342D4D3037, 24, -1074, 1023)
    cases += [randoms[i:i + 2] for i in range(0, 12, 2)] + [randoms[i:i + 3] for i in range(12, 24, 3)]
    for values in cases:
        result = N.hypot(values)
        inputs = " ".join(f64(v) for v in values) if values else "undefined"
        script = f"Math.hypot({', '.join(double_expression(v) for v in values)})"
        if isinstance(result, int):
            corpus.add(file, "hypot", record("Math.hypot", inputs, number(result), clause("sec-math.hypot"), script))
        else:
            faithful = alternatives(result.faithful)
            corpus.add(file, "hypot", record(
                "Math.hypot", inputs, number(result.correct), clause("sec-math.hypot"), script,
                "implementation-approximated; out is correctly rounded. JSD-0067 decision 5 computes it with scaling and "
                "a compensated sum and has the library state its bound; until it does, both expectations are within "
                "one unit in the last place: either double bracketing the exact value",
                js=faithful, vm=faithful))


def sum_precise_vectors(corpus: Corpus, file: str) -> None:
    tiny, big, maximum = R.SMALLEST_SUBNORMAL, d("1e308"), R.MAX_FINITE
    half_ulp = exact(Fraction(1, 2 ** 53))
    cases: list[list[int]] = [
        [], [R.NEGATIVE_ZERO], [R.NEGATIVE_ZERO, R.NEGATIVE_ZERO], [R.NEGATIVE_ZERO, 0], [0], [exact(1), exact(-1)],
        [big, big], [big, big, big ^ R.SIGN_BIT], [maximum, maximum, maximum ^ R.SIGN_BIT, maximum ^ R.SIGN_BIT],
        [big, big ^ R.SIGN_BIT, tiny], [exact(1), d("1e100"), exact(1), d("-1e100")], [d("0.1"), d("0.2"), d("0.3")],
        [R.POSITIVE_INFINITY, R.NEGATIVE_INFINITY], [R.CANONICAL_NAN, exact(1)], [R.POSITIVE_INFINITY, exact(1)],
        [big, big, R.NEGATIVE_INFINITY], [tiny, exact(1), exact(-1)], [exact(1), half_ulp],
        [exact(1), half_ulp, exact(Fraction(1, 2 ** 106))], [exact(1), half_ulp ^ R.SIGN_BIT, exact(Fraction(-1, 2 ** 106))],
        [maximum, exact(2 ** 970)], [maximum, exact(2 ** 969)], [maximum, exact(2 ** 970), exact(-1)],
        [maximum, maximum, R.CANONICAL_NAN], [R.NEGATIVE_INFINITY, R.NEGATIVE_INFINITY], [R.NEGATIVE_ZERO, exact(-1), exact(1)],
        [d("1e308"), d("1e308"), d("1e308"), d("-1e308"), d("-1e308"), d("-1e308"), tiny],
        [tiny, tiny ^ R.SIGN_BIT, R.NEGATIVE_ZERO], [d("1e16"), exact(1), d("-1e16")], [exact(2 ** 53), exact(1), exact(1)],
    ]
    randoms = random_doubles(0x534A4C342D4D3038, 120, -60, 60)
    cases += [randoms[i:i + 10] for i in range(0, 120, 10)]
    wide = random_doubles(0x534A4C342D4D3039, 60, -1074, 1023)
    cases += [wide[i:i + 6] for i in range(0, 60, 6)]
    for values in cases:
        inputs = " ".join(f64(v) for v in values) if values else "undefined"
        script = f"Math.sumPrecise([{', '.join(double_expression(v) for v in values)}])"
        corpus.add(file, "sumprecise", record("Math.sumPrecise", inputs, number(N.sum_precise(values)),
                                              clause("sec-math.sumprecise"), script))
    for text, out, script in (("str:\"\"", number(R.NEGATIVE_ZERO), "Math.sumPrecise(\"\")"),
                              ("str:\"1\"", "error: TypeError", "Math.sumPrecise(\"1\")"),
                              ("int:1", "error: TypeError", "Math.sumPrecise(1)"),
                              ("undefined", "error: TypeError", "Math.sumPrecise()"),
                              ("null", "error: TypeError", "Math.sumPrecise(null)")):
        corpus.add(file, "sumprecise-items", record("Math.sumPrecise argument", text, out, clause("sec-math.sumprecise"), script,
                                                    "the items argument: a String is iterable, and an element that is not a "
                                                    "Number throws a TypeError"))
    corpus.add(file, "sumprecise-items", record("Math.sumPrecise", f"{f64(N.double(1))} str:\"2\"", "error: TypeError",
                                                clause("sec-math.sumprecise"), "Math.sumPrecise([1, \"2\"])"))
    corpus.add(file, "sumprecise-items", record("Math.sumPrecise", f"{f64(R.CANONICAL_NAN)} str:\"2\"", "error: TypeError",
                                                clause("sec-math.sumprecise"), "Math.sumPrecise([NaN, \"2\"])",
                                                "an element that is not a Number throws even after a NaN"))


# --------------------------------------------------------------------------------------------------
# Exact rounding, through Temporal.

DURATION_INCREMENTS = [1, 2, 5, 8, 25, 40, 125, 500]
INSTANT_INCREMENTS = [1, 3, 16, 1000, 10 ** 9]
SINCE_INCREMENTS = [1, 2, 5, 500]
UNITS = {"nanosecond": 1, "microsecond": 10 ** 3, "millisecond": 10 ** 6, "second": 10 ** 9, "minute": 6 * 10 ** 10,
         "hour": 36 * 10 ** 11}
# ECMA-262 at the pin has no Temporal: the rounding vectors' reference follows the Temporal proposal's
# RoundNumberToIncrement, RoundNumberToIncrementAsIfPositive, GetUnsignedRoundingMode,
# ApplyUnsignedRoundingMode, NegateRoundingMode and TotalTimeDuration, as both engines implement them,
# and their provenance is the reference computation alone.


def residues(k: int) -> list[int]:
    out = {0, 1, k - 1, k // 2, (k + 1) // 2, k // 2 + 1}
    return sorted(r for r in out if 0 <= r < k)


def rounding_vectors(corpus: Corpus) -> None:
    file = "rounding.vectors"
    for mode in N.MODES:
        for k in DURATION_INCREMENTS:
            quotients = (0, 2, 3) if mode == "halfEven" else (0, 3)
            values = sorted({q * k + r for q in quotients for r in residues(k)} - {0})
            near = (2 ** 53 - 1) - (2 ** 53 - 1) % k - k + k // 2
            for value in [v for x in values for v in (x, -x)] + [near, -near]:
                rounded = N.round_number_to_increment(value, k, mode)
                corpus.add(file, "duration-round", record(
                    "Temporal.Duration.prototype.round", f"int:{value} int:{k} str:\"{mode}\"", integer(int(rounded)), "",
                    f"Temporal.Duration.from({{nanoseconds: {value}}}).round({{smallestUnit: \"nanosecond\", "
                    f"roundingIncrement: {k}, roundingMode: \"{mode}\"}}).nanoseconds + 0",
                    "RoundNumberToIncrement, signed (Temporal proposal); + 0 makes a zero +0"))
    limit = 864 * 10 ** 19
    for mode in N.MODES:
        for k in INSTANT_INCREMENTS:
            values = sorted({q * k + r for q in (0, 3, 10 ** 6 + 2) for r in residues(k)})
            for value in [v for x in values for v in ((x, -x) if x else (x,))]:
                rounded = N.round_number_to_increment_as_if_positive(value, k, mode)
                corpus.add(file, "instant-round", record(
                    "Temporal.Instant.prototype.round", f"bigint:{value} str:\"nanosecond\" int:{k} str:\"{mode}\"",
                    bigint(int(rounded)), "",
                    f"Temporal.Instant.fromEpochNanoseconds({big_literal(value)}).round({{smallestUnit: \"nanosecond\", "
                    f"roundingIncrement: {k}, roundingMode: \"{mode}\"}}).epochNanoseconds",
                    "RoundNumberToIncrementAsIfPositive (Temporal proposal)"))
        for unit, k in (("hour", 24), ("second", 86400), ("hour", 3)):
            step = UNITS[unit] * k
            for value in (limit, -limit, limit - 1, -limit + 1, limit - step // 2, -limit + step // 2, step // 2, -(step // 2),
                          step + step // 2, -(step + step // 2)):
                rounded = N.round_number_to_increment_as_if_positive(value, step, mode)
                corpus.add(file, "instant-round", record(
                    "Temporal.Instant.prototype.round", f"bigint:{value} str:\"{unit}\" int:{k} str:\"{mode}\"",
                    bigint(int(rounded)), "",
                    f"Temporal.Instant.fromEpochNanoseconds({big_literal(value)}).round({{smallestUnit: \"{unit}\", "
                    f"roundingIncrement: {k}, roundingMode: \"{mode}\"}}).epochNanoseconds",
                    "RoundNumberToIncrementAsIfPositive at the instant range's limits (Temporal proposal)"))
    for mode in N.MODES:
        for k in SINCE_INCREMENTS:
            for difference in sorted({q * k + r for q in (2, 3) for r in residues(k)}):
                for a, b in ((difference, 0), (0, difference), (10 ** 18 + difference, 10 ** 18), (-(10 ** 18), -(10 ** 18) + difference)):
                    result = -N.round_number_to_increment(b - a, k, N.negate_rounding_mode(mode))
                    corpus.add(file, "instant-since", record(
                        "Temporal.Instant.prototype.since", f"bigint:{a} bigint:{b} int:{k} str:\"{mode}\"",
                        integer(int(result)), "",
                        f"Temporal.Instant.fromEpochNanoseconds({big_literal(a)}).since(Temporal.Instant.fromEpochNanoseconds("
                        f"{big_literal(b)}), {{largestUnit: \"nanosecond\", smallestUnit: \"nanosecond\", roundingIncrement: {k}, "
                        f"roundingMode: \"{mode}\"}}).nanoseconds + 0",
                        "since rounds other - this with NegateRoundingMode of the mode and negates it (Temporal proposal); "
                        "+ 0 makes a zero +0"))
    totals: list[int] = [1, 999, 2 ** 53 + 1, 2 ** 53 + 3, 2 ** 54 + 2, 2 ** 54 + 6, 2 ** 53 * 1000 + 500, (2 ** 53 + 1) * 1000,
                         (2 ** 53 + 1) * 10 ** 6, (2 ** 53 + 1) * 10 ** 9 // 2, 123456789012345678901234,
                         (2 ** 53 - 1) * 10 ** 9 + 999999999, 36 * 10 ** 11 + 1, 10 ** 9 // 3]
    draws = splitmix64(0x534A4C342D523031)  # "SJL4-R01"
    totals += [next(draws) % (2 ** 53 * 10 ** 9) for _ in range(12)]
    for total in totals:
        for sign in (1, -1):
            nanoseconds = sign * total
            seconds, rest = divmod(abs(nanoseconds), 10 ** 9)
            if seconds >= 2 ** 53:
                continue
            for unit, length in UNITS.items():
                value = N.double(Fraction(nanoseconds, length))
                corpus.add(file, "duration-total", record(
                    "Temporal.Duration.prototype.total", f"bigint:{nanoseconds} str:\"{unit}\"", number(value), "",
                    f"Temporal.Duration.from({{seconds: {sign * seconds}, nanoseconds: {sign * rest}}}).total(\"{unit}\")",
                    "the Number nearest the exact quotient by the unit's length, ties to even (TotalTimeDuration, "
                    "Temporal proposal)"))


# --------------------------------------------------------------------------------------------------
# BigInt.

BIG = random_ints(0x534A4C342D423031, 6, 1) + random_ints(0x534A4C342D423032, 6, 3) + \
    random_ints(0x534A4C342D423033, 4, 12) + random_ints(0x534A4C342D423034, 2, 40)  # "SJL4-B01".."B04"
SMALL = [0, 1, -1, 2, -2, 7, -7, 2 ** 63, -(2 ** 63), 2 ** 64 - 1, -(2 ** 64) + 1, 2 ** 64, -(2 ** 64)]

BINARY = [
    ("BigInt::add", "+", lambda x, y: x + y, "sec-numeric-types-bigint-add"),
    ("BigInt::subtract", "-", lambda x, y: x - y, "sec-numeric-types-bigint-subtract"),
    ("BigInt::multiply", "*", lambda x, y: x * y, "sec-numeric-types-bigint-multiply"),
    ("BigInt::divide", "/", N.bigint_divide, "sec-numeric-types-bigint-divide"),
    ("BigInt::remainder", "%", N.bigint_remainder, "sec-numeric-types-bigint-remainder"),
    ("BigInt::bitwiseAND", "&", lambda x, y: x & y, "sec-bigintbitwiseop"),
    ("BigInt::bitwiseOR", "|", lambda x, y: x | y, "sec-bigintbitwiseop"),
    ("BigInt::bitwiseXOR", "^", lambda x, y: x ^ y, "sec-bigintbitwiseop"),
]


def bigint_outcome(value: int | None) -> str:
    return "error: RangeError" if value is None else bigint(value)


def bigint_vectors(corpus: Corpus) -> None:
    file = "bigint-arithmetic.vectors"
    operands = SMALL + BIG
    pairs = [(x, y) for x in operands[:7] for y in operands[:7]] + list(zip(operands, operands[3:] + operands[:3])) \
        + list(zip(BIG, reversed(BIG))) + [(x, y) for x in BIG[6:12] for y in SMALL[1:5]]
    for op, symbol, function, cid in BINARY:
        group = op.split("::")[1].lower()
        for x, y in pairs:
            corpus.add(file, group, record(op, f"bigint:{x} bigint:{y}", bigint_outcome(function(x, y)), clause(cid),
                                           f"{big_literal(x)} {symbol} {big_literal(y)}"))
    for x in operands:
        corpus.add(file, "unaryminus", record("BigInt::unaryMinus", f"bigint:{x}", bigint(-x),
                                              clause("sec-unary-minus-operator-runtime-semantics-evaluation"), f"-{big_literal(x)}"))
        corpus.add(file, "bitwisenot", record("BigInt::bitwiseNOT", f"bigint:{x}", bigint(~x),
                                              clause("sec-bitwise-not-operator-runtime-semantics-evaluation"), f"~{big_literal(x)}"))
    counts = [0, 1, 63, 64, 65, 200, -1, -63, -64, -65, -200, 2 ** 64, -(2 ** 64)]
    shifts = (("leftshift", "BigInt::leftShift", "<<", 1, "sec-left-shift-operator-runtime-semantics-evaluation"),
              ("signedrightshift", "BigInt::signedRightShift", ">>", -1,
               "sec-signed-right-shift-operator-runtime-semantics-evaluation"))
    for x in SMALL[:9] + BIG[:8]:
        for count in counts:
            for group, op, symbol, direction, cid in shifts:
                left = direction * count
                if left > 4096 and x != 0:
                    continue  # past any representation: the policy vectors below cover it
                corpus.add(file, group, record(op, f"bigint:{x} bigint:{count}", bigint(N.bigint_left_shift(x, left)),
                                               clause(cid), f"{big_literal(x)} {symbol} {big_literal(count)}"))
    corpus.add(file, "unsignedrightshift", record("BigInt::unsignedRightShift", "bigint:1 bigint:0", "error: TypeError",
                                                  clause("sec-applystringornumericbinaryoperator"), "1n >>> 0n"))
    for x, y in ((0, 0), (2, 0), (-2, 0), (2, 1), (2, 63), (-2, 63), (3, 40), (-3, 41), (7, 100), (2, -1), (0, -1), (1, 2 ** 64),
                 (-1, 2 ** 64), (-1, 2 ** 64 + 1), (0, 2 ** 64), (BIG[6], 3)):
        value = None if y < 0 else x ** y if y < 4096 else (1 if x == 1 else 0 if x == 0 else (1 if y % 2 == 0 else -1))
        corpus.add(file, "exponentiate", record("BigInt::exponentiate", f"bigint:{x} bigint:{y}", bigint_outcome(value),
                                                clause("sec-numeric-types-bigint-exponentiate"),
                                                f"{big_literal(x)} ** {big_literal(y)}"))
    policy_vectors(corpus, file)


POLICY_CLAUSES = {
    "BigInt::add": clause("sec-numeric-types-bigint-add"),
    "BigInt::subtract": clause("sec-numeric-types-bigint-subtract"),
    "BigInt::multiply": clause("sec-numeric-types-bigint-multiply"),
    "BigInt::exponentiate": clause("sec-numeric-types-bigint-exponentiate"),
    "BigInt::leftShift": clause("sec-left-shift-operator-runtime-semantics-evaluation"),
    "BigInt::signedRightShift": clause("sec-signed-right-shift-operator-runtime-semantics-evaluation"),
    "BigInt::bitwiseNOT": clause("sec-bitwise-not-operator-runtime-semantics-evaluation"),
    "BigInt::bitwiseOR": clause("sec-bigintbitwiseop"),
    "BigInt.asUintN": clause("sec-bigint.asuintn"),
    "BigInt.asIntN": clause("sec-bigint.asintn"),
    "StringToBigInt": clause("sec-stringtobigint"),
}


def policy_vectors(corpus: Corpus, file: str) -> None:
    """At Broiler.VM's width ceiling and past it, and past any representation."""
    m = VM_MAXIMUM_BITS
    cases = [
        # op, inputs, script, the specification's value of the script, the widest intermediate value
        ("BigInt::add", f"bigint:2^{m - 1}-1 bigint:2^{m - 1}", f"((1n << {m - 1}n) - 1n + (1n << {m - 1}n)) >> {m - 64}n",
         ((1 << m) - 1) >> (m - 64), (1 << m) - 1),
        ("BigInt::add", f"bigint:2^{m - 1} bigint:2^{m - 1}", f"((1n << {m - 1}n) + (1n << {m - 1}n)) >> {m - 64}n",
         (1 << m) >> (m - 64), 1 << m),
        ("BigInt::subtract", f"bigint:-(2^{m}) bigint:1", f"(((-1n) << {m}n) - 1n) >> {m - 64}n",
         (-(1 << m) - 1) >> (m - 64), -(1 << m) - 1),
        ("BigInt::leftShift", f"bigint:-1 bigint:{m}", f"((-1n) << {m}n) >> {m - 64}n", -(1 << m) >> (m - 64), -(1 << m)),
        ("BigInt::leftShift", f"bigint:1 bigint:{m - 1}", f"(1n << {m - 1}n) >> {m - 64}n", 1 << 63, 1 << (m - 1)),
        ("BigInt::leftShift", f"bigint:1 bigint:{m}", f"(1n << {m}n) >> {m - 64}n", 1 << 64, 1 << m),
        ("BigInt::multiply", f"bigint:2^{m // 2 - 1} bigint:2^{m // 2}", f"((1n << {m // 2 - 1}n) * (1n << {m // 2}n)) >> {m - 64}n",
         1 << 63, 1 << (m - 1)),
        ("BigInt::multiply", f"bigint:2^{m // 2} bigint:2^{m // 2}", f"((1n << {m // 2}n) * (1n << {m // 2}n)) >> {m - 64}n",
         1 << 64, 1 << m),
        ("BigInt::multiply", f"bigint:-(2^{m // 2}) bigint:2^{m // 2}", f"(((-1n) << {m // 2}n) * (1n << {m // 2}n)) >> {m - 64}n",
         -(1 << 64), -(1 << m)),
        ("BigInt::exponentiate", f"bigint:2 bigint:{m - 1}", f"(2n ** {m - 1}n) >> {m - 64}n", 1 << 63, 1 << (m - 1)),
        ("BigInt::exponentiate", f"bigint:2 bigint:{m}", f"(2n ** {m}n) >> {m - 64}n", 1 << 64, 1 << m),
        ("BigInt::exponentiate", f"bigint:-2 bigint:{m - 1}", f"((-2n) ** {m - 1}n) >> {m - 64}n", -(1 << 63), -(1 << (m - 1))),
        ("BigInt::bitwiseNOT", f"bigint:2^{m}-1", f"(~((1n << {m - 1}n) - 1n + (1n << {m - 1}n))) >> {m - 64}n",
         (~((1 << m) - 1)) >> (m - 64), (1 << m) - 1),
        ("BigInt::bitwiseOR", f"bigint:2^{m - 1} bigint:2^{m - 1}-1", f"((1n << {m - 1}n) | ((1n << {m - 1}n) - 1n)) >> {m - 64}n",
         ((1 << m) - 1) >> (m - 64), (1 << m) - 1),
        ("BigInt.asUintN", f"int:{m} bigint:-1", f"BigInt.asUintN({m}, -1n) >> {m - 64}n", ((1 << m) - 1) >> (m - 64), (1 << m) - 1),
        ("BigInt.asUintN", f"int:{m + 1} bigint:-1", f"BigInt.asUintN({m + 1}, -1n) >> {m - 63}n", ((1 << (m + 1)) - 1) >> (m - 63),
         (1 << (m + 1)) - 1),
        ("BigInt.asIntN", f"int:{m + 1} bigint:-1", f"BigInt.asIntN({m + 1}, -1n)", -1, -1),
        ("StringToBigInt", f"0x followed by {m // 4} digits F", f"BigInt(\"0x\" + \"F\".repeat({m // 4})) >> {m - 64}n",
         ((1 << m) - 1) >> (m - 64), (1 << m) - 1),
        ("StringToBigInt", f"0x1 followed by {m // 4} digits 0", f"BigInt(\"0x1\" + \"0\".repeat({m // 4})) >> {m - 64}n",
         1 << 64, 1 << m),
    ]
    for op, inputs, script, value, widest in cases:
        width = N.bit_width(widest)
        if width <= m:
            corpus.add(file, "policy", record(f"{op} (width policy)", f"str:\"{inputs}\"", bigint(value), POLICY_CLAUSES[op], script,
                                              f"a value of {width} bits, at or below Broiler.VM's ceiling; the script shifts "
                                              "the result down to show its top bits"))
        else:
            corpus.add(file, "policy", record(
                f"{op} (width policy)", f"str:\"{inputs}\"", bigint(value), POLICY_CLAUSES[op], script,
                f"a value of {width} bits, past Broiler.VM's ceiling of {m}: the specification has no width limit, so out "
                f"holds its value; Broiler.VM refuses it under {VM_POLICY}, and Broiler.JS computes it under {JS_POLICY}",
                js=bigint(value), vm="error: RangeError"))
    astronomical = [
        ("BigInt::leftShift", "bigint:1 bigint:2^64", "(1n << (2n ** 64n)) > 0n"),
        ("BigInt::signedRightShift", "bigint:1 bigint:-(2^64)", "(1n >> -(2n ** 64n)) > 0n"),
        ("BigInt::exponentiate", "bigint:2 bigint:2^64", "(2n ** (2n ** 64n)) > 0n"),
        ("BigInt::multiply", "bigint:2^(2^31) bigint:2^(2^31)", "((1n << 2147483648n) * (1n << 2147483648n)) > 0n"),
    ]
    for op, inputs, script in astronomical:
        corpus.add(file, "policy", record(
            f"{op} (width policy)", f"str:\"{inputs}\"", boolean(True), POLICY_CLAUSES[op], script,
            "a value of 2^32 bits or more: out holds the specification's answer; " + LIMIT,
            js="error: RangeError", vm="error: RangeError"))


RADIX_VALUES = [0, 1, -1, 35, 36, -255, 2 ** 53 + 1, 2 ** 64 - 1, -(2 ** 64), 2 ** 100 + 12345, -(3 ** 200)]


def text_vectors(corpus: Corpus) -> None:
    file = "bigint-text.vectors"
    for radix in range(2, 37):
        for value in RADIX_VALUES + random_ints(0x534A4C342D543031 + radix, 1, 4):
            corpus.add(file, "tostring-radix", record(
                "BigInt::toString", f"bigint:{value} int:{radix}", f"value: {string_value(N.bigint_to_string(value, radix))}",
                clause("sec-numeric-types-bigint-tostring", "sec-bigint.prototype.tostring"),
                f"{big_literal(value)}.toString({radix})"))
    # Both sides of Broiler.VM's divide-and-conquer threshold (JsBigInt.FormatLeafBits, 4096 bits), and far past it.
    draws = splitmix64(0x534A4C342D543032)  # "SJL4-T02"
    for index, bits in enumerate((4000, 4096, 4097, 5000, 20000, 70000)):
        value = 0
        for _ in range((bits + 63) // 64):
            value = (value << 64) | next(draws)
        value = (value % (1 << (bits - 1))) | (1 << (bits - 1))
        value = -value if index % 2 else value
        for radix in (10, 7, 16) if bits <= 20000 else (10,):
            script = f"{big_literal(value)}.toString({radix})"
            text = N.bigint_to_string(value, radix)
            note = None
            op = "BigInt::toString"
            if len(text) > 8000:
                script = f"(function () {{ var s = {script}; return s.length + \":\" + s.slice(0, 40) + \":\" + s.slice(-40); }})()"
                text = f"{len(text)}:{text[:40]}:{text[-40:]}"
                note = "the script shows the text's length, its first 40 and its last 40 characters"
                op = "BigInt::toString, length and ends"
            corpus.add(file, "tostring-long", record(
                op, f"bigint:{value} int:{radix}", f"value: {string_value(text)}",
                clause("sec-numeric-types-bigint-tostring"), script, note))
    whitespace = sorted(R.STR_WHITE_SPACE)
    for code in whitespace:
        for text in (chr(code) + "12", "12" + chr(code), chr(code) * 3 + "-12" + chr(code), chr(code) + "0x1f" + chr(code)):
            parse_vector(corpus, file, text)
    for code in (0x180E, 0x200B, 0x0085, 0x00AD, 0x2060, 0x0008):
        parse_vector(corpus, file, chr(code) + "12")
    for text in ("", " ", "　", "0", "-0", "+0", "007", "-12", "+12", "--1", "+-1", "-", "+", "1e3", "1.0", "1.", ".1", "1n",
                 "1_000", "0x", "0X1F", "0x1F", "-0x1F", "+0x1F", "0o17", "0O17", "0o18", "0b101", "0B101", "0b102", "0x1G",
                 "١٢", "１", "12 3", "Infinity", "NaN", "0x" + "f" * 40, "9" * 1200, "-" + "1" * 300, "  0b" + "1" * 200):
        parse_vector(corpus, file, text)
    for x, text, op_result in ((1, "2", True), (1, " 1 ", None), (1, "1.0", None), (2, "0x2", None), (-1, "-1", None), (1, "", None),
                               (0, "", None), (5, "x", None)):
        target = N.string_to_big_int(text)
        less = None if target is None else x < target
        corpus.add(file, "compare-string", record(
            "IsLessThan", f"bigint:{x} {string_value(text)}", boolean(bool(less)), clause("sec-islessthan", "sec-stringtobigint"),
            f"{big_literal(x)} < {js_string(text)}", "a String that is not a StringIntegerLiteral compares as undefined"))
        corpus.add(file, "equal-string", record(
            "IsLooselyEqual", f"bigint:{x} {string_value(text)}", boolean(target is not None and target == x),
            clause("sec-islooselyequal", "sec-stringtobigint"), f"{big_literal(x)} == {js_string(text)}"))
    keys = [(2 ** 40, "var o = {}; o[2n ** 40n] = 1; Object.keys(o)[0]", f"value: {string_value(str(2 ** 40))}"),
            (2 ** 64 + 1, "var o = {}; o[2n ** 64n + 1n] = 1; Object.keys(o)[0]", f"value: {string_value(str(2 ** 64 + 1))}"),
            (-(2 ** 40), "var o = {}; o[-(2n ** 40n)] = 1; Object.keys(o)[0]", f"value: {string_value(str(-(2 ** 40)))}"),
            (1, "[10, 11, 12][1n]", number(N.double(11))),
            (2 ** 32, "var a = []; a[2n ** 32n] = 1; a.length", number(0)),
            (2 ** 32 - 2, "var a = []; a[2n ** 32n - 2n] = 1; a.length", number(N.double(2 ** 32 - 1))),
            (2 ** 70, "var o = {}; o[2n ** 70n] = 7; o[\"1180591620717411303424\"]", number(N.double(7)))]
    for value, script, out in keys:
        corpus.add(file, "property-key", record("ToPropertyKey", f"bigint:{value}", out,
                                                clause("sec-topropertykey", "sec-numeric-types-bigint-tostring"), script))


def js_string(text: str) -> str:
    return js_string_literal(text)


def parse_vector(corpus: Corpus, file: str, text: str) -> None:
    value = N.string_to_big_int(text)
    script = f"BigInt({js_string(text)})"
    if len(text) > 300 and text.strip("9") == "":
        script = f"BigInt(\"9\".repeat({len(text)}))"
    corpus.add(file, "stringtobigint", record(
        "StringToBigInt", string_value(text), "error: SyntaxError" if value is None else bigint(value),
        clause("sec-stringtobigint", "sec-tobigint", "sec-bigint-constructor-number-value"), script))


def asintn_vectors(corpus: Corpus) -> None:
    file = "bigint-asintn.vectors"
    widths = [0, 1, 2, 7, 8, 63, 64, 65, 127, 128, 1000, VM_MAXIMUM_BITS - 1, VM_MAXIMUM_BITS, 2 ** 32, 2 ** 32 + 1, 2 ** 53 - 1]
    values = [0, 1, -1, 5, -5, 2 ** 63, -(2 ** 63), 2 ** 64 - 1, 2 ** 64, -(2 ** 64), 2 ** 1000 + 1, -(2 ** 1000) - 1] + BIG[:4]
    for bits in widths:
        for value in values:
            for name, function in (("asIntN", N.as_int_n), ("asUintN", N.as_uint_n)):
                group = name.lower()
                inputs = f"int:{bits} bigint:{value}"
                cid = clause(f"sec-bigint.{group}")
                if name == "asUintN" and value < 0 and bits > 2048:
                    # The value 2^bits + value: its top 64 bits show it, or it is beyond any representation.
                    if bits <= VM_MAXIMUM_BITS:
                        result = function(bits, value)
                        corpus.add(file, group, record(f"BigInt.{name}, top 64 bits", inputs, bigint(result >> (bits - 64)), cid,
                                                       f"BigInt.{name}({bits}, {big_literal(value)}) >> {bits - 64}n",
                                                       "the script shows the top 64 bits of the result"))
                    else:
                        corpus.add(file, group, record(
                            f"BigInt.{name}", inputs, boolean(True), cid, f"BigInt.{name}({bits}, {big_literal(value)}) > 0n",
                            f"a value of {bits} bits: out holds the specification's answer; " + LIMIT,
                            js="error: RangeError", vm="error: RangeError"))
                    continue
                corpus.add(file, group, record(f"BigInt.{name}", inputs, bigint(function(bits, value)), cid,
                                               f"BigInt.{name}({bits}, {big_literal(value)})"))
    for bits in (exact(2 ** 53), exact(-1), R.POSITIVE_INFINITY, R.CANONICAL_NAN, exact(Fraction(1, 2)), exact(Fraction(-1, 2)),
                 exact(2 ** 53 - 1)):
        index = N.to_index(bits)
        for name, function in (("asIntN", N.as_int_n), ("asUintN", N.as_uint_n)):
            out = "error: RangeError" if index is None else bigint(function(index, 5))
            corpus.add(file, name.lower() + "-bits", record(
                f"BigInt.{name}", f"{f64(bits)} bigint:5", out, clause(f"sec-bigint.{name.lower()}", "sec-toindex"),
                f"BigInt.{name}({double_expression(bits)}, 5n)", "the bits argument is converted by ToIndex"))


def bigint_number_vectors(corpus: Corpus) -> None:
    file = "bigint-number.vectors"
    threshold = 2 ** 1024 - 2 ** 970
    values = [0, 1, -1, 2 ** 53 - 1, 2 ** 53, 2 ** 53 + 1, 2 ** 53 + 2, 2 ** 53 + 3, -(2 ** 53 + 1), -(2 ** 53 + 3), 2 ** 54 + 2,
              2 ** 64 - 1, 2 ** 64 + 2 ** 11, 2 ** 64 + 2 ** 11 + 1, 2 ** 64 + 3 * 2 ** 11, threshold - 1, threshold, threshold + 1,
              2 ** 1024, -threshold, 2 ** 1100] + BIG[6:12]
    for value in values:
        corpus.add(file, "tonumber", record("Number(BigInt)", f"bigint:{value}", number(N.bigint_to_number(value)),
                                            clause("sec-number-constructor-number-value", "sec-tonumber"),
                                            f"Number({big_literal(value)})",
                                            "the Number nearest the BigInt's value, ties to even (Number(value) step 2.a)"))
    numbers = SPECIALS + signed([exact(1), exact(Fraction(1, 2)), exact(Fraction(3, 2)), exact(2 ** 53), exact(2 ** 53 + 2),
                                 exact(2 ** 64), d("1e21"), R.MAX_FINITE, R.SMALLEST_SUBNORMAL, exact(2 ** 53 - 1)])
    for bits in unique(numbers):
        result = N.number_to_big_int(bits)
        corpus.add(file, "numbertobigint", record("NumberToBigInt", f64(bits), bigint_outcome(result),
                                                  clause("sec-numbertobigint", "sec-bigint-constructor-number-value"),
                                                  f"BigInt({double_expression(bits)})"))
    compared = [0, 1, -1, 2 ** 53 - 1, 2 ** 53, 2 ** 53 + 1, 2 ** 64, 2 ** 64 + 1, -(2 ** 64), 2 ** 1024, threshold]
    targets = unique(SPECIALS + [exact(1), exact(Fraction(3, 2)), exact(2 ** 53), exact(2 ** 53 + 2), exact(2 ** 64), R.MAX_FINITE,
                                 exact(-(2 ** 64)), exact(2 ** 53 - 1), exact(Fraction(-1, 2))])
    for x in compared:
        for bits in targets:
            order = N.compare_bigint_number(x, bits)
            n = double_expression(bits)
            corpus.add(file, "lessthan", record("IsLessThan", f"bigint:{x} {f64(bits)}", boolean(order == -1),
                                                clause("sec-islessthan"), f"{big_literal(x)} < {n}"))
            corpus.add(file, "greaterthan", record("IsLessThan", f"{f64(bits)} bigint:{x}", boolean(order == 1),
                                                   clause("sec-islessthan"), f"{n} < {big_literal(x)}"))
            corpus.add(file, "looselyequal", record("IsLooselyEqual", f"bigint:{x} {f64(bits)}", boolean(order == 0),
                                                    clause("sec-islooselyequal"), f"{big_literal(x)} == {n}"))


# --------------------------------------------------------------------------------------------------

def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--out", required=True, type=Path)
    args = parser.parse_args(argv)
    corpus = Corpus()
    conversion_vectors(corpus)
    float16_vectors(corpus)
    math_vectors(corpus)
    rounding_vectors(corpus)
    bigint_vectors(corpus)
    text_vectors(corpus)
    asintn_vectors(corpus)
    bigint_number_vectors(corpus)
    corpus.classify(DIVERGENCES)
    corpus.write(args.out)
    for name, records in sorted(corpus.files.items()):
        print(f"{name}: {len(records)} vectors")
    return 0


if __name__ == "__main__":
    sys.exit(main())
