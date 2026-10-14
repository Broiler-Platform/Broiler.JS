#!/usr/bin/env python3
"""Generate the input population of bundle sjl-4-001's reference-arm comparison.

    python3 scripts/conformance/generate_numerics_population.py --out <directory>

The population is the one docs/evidence/sjl-4-001/reference-arms.md declares, class by class (N1 to
N4, R1 and B1), with the seeds it names, as its revision of 2026-10-13
(docs/evidence/sjl-4-001/reference-arms-revised-2026-10-13.md) reads the counts and sets the rule
leaves open. The comparison in Broiler.ECMAScript.Algorithms.Tests reads what this writes; nothing
here is an expected value, and the comparison judges no input by it.

Every class is one gzip file of one input per line, and manifest.json records each file's line count
and the SHA-256 of its uncompressed text, so a bundle can name the population it ran:

- a double is its IEEE 754 bit pattern in 16 hexadecimal digits, a binary16 in 4;
- a tuple or a sequence of doubles is its patterns joined by commas, and the empty sequence is "-";
- an integer (a BigInt, a numerator, a denominator, an increment) is signed hexadecimal: an optional
  "-", then lower-case digits, "0" for zero;
- a line of several fields separates them with one space;
- a string is written with every character outside '!' to '~', and the backslash, as \\uXXXX.

Three of the rule's inputs are not written here but built by the comparison from a class, because
they would be gigabytes as text: the BigInt texts at every radix (from B1V), StringToBigInt's texts
(from B1V, by the construction the revision states), and the comparison of every value with every
double of B1C (the product of B1V and B1C).

The output is deterministic: the same arguments give the same bytes (gzip's header carries no time).
"""

from __future__ import annotations

import argparse
import gzip
import hashlib
import json
import sys
from fractions import Fraction
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import numref as R  # noqa: E402
from generate_number_vectors import splitmix64  # noqa: E402

RULE = "docs/evidence/sjl-4-001/reference-arms.md"
REVISION = "docs/evidence/sjl-4-001/reference-arms-revised-2026-10-13.md"

SEED_N1 = 0x534A4C342D303031  # "SJL4-001"
SEED_N2 = 0x534A4C342D303032
SEED_N3_UNIFORM = 0x534A4C342D303033
SEED_N3_POW = 0x534A4C342D303034
SEED_N3_HYPOT = 0x534A4C342D303035
SEED_N4 = 0x534A4C342D303036
SEED_R1_INCREMENT = 0x534A4C342D303037
SEED_R1_RATIONAL = 0x534A4C342D303038
SEED_B1_VALUES = 0x534A4C342D303039
SEED_B1_PAIRS = 0x534A4C342D30303A

N1_DRAWS = 1 << 20
N2_NAN_DRAWS = 1 << 12
N3_UNIFORM = 1 << 16
N3_POW_PAIRS = 1 << 18
N3_HYPOT_TUPLES = 1 << 16
N4_SEQUENCES = 1 << 14
# The rule names these sets "drawn" without a count; the revision of 2026-10-13 sets them.
R1_NUMERATORS = 1 << 10
R1_DENOMINATORS = 1 << 10
R1_INCREMENTS = 1 << 10
R1_RATIONALS = 1 << 16
R1_TIE_STEPS = range(-8, 9)
R1_TIES_NEAR_DRAWN = 32
B1_DRAWN = 1 << 12
B1_PAIRS = 1 << 14
B1_COMPARED_DRAWS = 1 << 14
VM_CEILING = 1 << 20

FLOAT16_MAX = 0x7BFF


# --------------------------------------------------------------------------------------------------
# Writing.

def escape(text: str) -> str:
    return "".join(c if "!" <= c <= "~" and c != "\\" else f"\\u{ord(c):04X}" for c in text)


def f64(bits: int) -> str:
    return f"{bits:016X}"


def f16(bits: int) -> str:
    return f"{bits:04X}"


def integer(value: int) -> str:
    return f"-{-value:x}" if value < 0 else f"{value:x}"


def sequence(values: list[int]) -> str:
    return ",".join(f64(v) for v in values) if values else "-"


class Writer:
    def __init__(self, out: Path) -> None:
        self.out = out
        self.classes: dict[str, dict[str, object]] = {}

    def write(self, name: str, lines: list[str], what: str) -> None:
        text = "".join(line + "\n" for line in lines)
        data = text.encode("utf-8")
        path = self.out / f"{name}.txt.gz"
        with open(path, "wb") as raw:
            with gzip.GzipFile(filename="", mode="wb", fileobj=raw, mtime=0, compresslevel=6) as stream:
                stream.write(data)
        self.classes[name] = {"file": path.name, "count": len(lines), "sha256": hashlib.sha256(data).hexdigest(),
                              "what": what}
        print(f"{name}: {len(lines)}", file=sys.stderr)

    def manifest(self) -> None:
        seeds = {name: hex(value) for name, value in globals().items() if name.startswith("SEED_")}
        manifest = {"rule": RULE, "revision": REVISION,
                    "generator": "scripts/conformance/generate_numerics_population.py",
                    "seeds": seeds, "classes": self.classes}
        (self.out / "manifest.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")


# --------------------------------------------------------------------------------------------------
# Doubles.

def unique(values: list[int]) -> list[int]:
    seen: set[int] = set()
    out = []
    for value in values:
        if value not in seen:
            seen.add(value)
            out.append(value)
    return out


def double(value: Fraction | int) -> int:
    return R.from_fraction(Fraction(value))


def exact(value: Fraction | int) -> int | None:
    """The double whose value is exactly this, or None when there is none."""
    bits = double(value)
    return bits if R.is_finite(bits) and R.to_fraction(bits) == Fraction(value) else None


def next_up(bits: int) -> int:
    """The next double toward +Infinity, of a finite or infinite non-NaN double."""
    if bits == R.NEGATIVE_ZERO:
        return 1
    if bits & R.SIGN_BIT:
        return bits - 1
    return bits + 1 if bits != R.POSITIVE_INFINITY else bits


def next_down(bits: int) -> int:
    return next_up(bits ^ R.SIGN_BIT) ^ R.SIGN_BIT


def both_signs(values: list[int]) -> list[int]:
    return [v for b in values for v in (b, b ^ R.SIGN_BIT)]


def n1() -> list[int]:
    specials = both_signs([0, 1, 0x000FFFFFFFFFFFFF, 0x0010000000000000, R.MAX_FINITE, R.POSITIVE_INFINITY])
    specials.append(R.CANONICAL_NAN)
    powers = []
    for e in range(-1074, 1024):
        p = double(Fraction(2) ** e)
        powers += [next_down(p), p, next_up(p)]
    ks = []
    for k in range(0, 65):
        for offset in (0, Fraction(1, 2), -Fraction(1, 2), 1, -1):
            bits = exact(Fraction(2) ** k + offset)
            if bits is not None:
                ks.append(bits)
    halves = [double(Fraction(2 * n + 1, 2)) for n in range(-300, 301)]
    edges = []
    for text in ("254.5", "255.5", "256", "65504", "65519.99", "65520"):
        bits = double(Fraction(text))
        edges += [next_down(bits), bits, next_up(bits)]
    fixed = unique(specials + powers + both_signs(ks) + halves + edges)
    drawn = []
    for draw in splitmix64(SEED_N1):
        if R.is_nan(draw):
            continue
        drawn.append(draw)
        if len(drawn) == N1_DRAWS:
            break
    return fixed + drawn


def float16_value(half: int) -> Fraction:
    sign = -1 if half & 0x8000 else 1
    field, fraction = (half >> 10) & 0x1F, half & 0x3FF
    if field == 0:
        return sign * Fraction(fraction, 1 << 24)
    return sign * Fraction(1024 + fraction, 1 << 10) * Fraction(2) ** (field - 15)


def n2_encode() -> list[int]:
    out = []
    for half in range(0x10000):
        if (half >> 10) & 0x1F == 0x1F:
            continue
        bits = double(float16_value(half))
        if half == 0x8000:
            bits = R.NEGATIVE_ZERO
        out += [next_down(bits), bits, next_up(bits)]
    # Every midpoint between adjacent finite binary16 values, at both signs, and the overflow
    # threshold 65520, the midpoint above the largest one.
    for half in range(0, FLOAT16_MAX + 1):
        mid = (float16_value(half) + (float16_value(half + 1) if half < FLOAT16_MAX else Fraction(65536))) / 2
        bits = exact(mid)
        assert bits is not None
        out += both_signs([next_down(bits), bits, next_up(bits)])
    return out


def n2_nan() -> list[int]:
    out = []
    for sign in (0, R.SIGN_BIT):
        for quiet in (0, 1 << 51):
            for payload_bits in range(8):
                payload = quiet
                for bit, position in ((1, 50), (2, 25), (4, 0)):
                    if payload_bits & bit:
                        payload |= 1 << position
                if payload:
                    out.append(sign | R.POSITIVE_INFINITY | payload)
    drawn = 0
    for draw in splitmix64(SEED_N2):
        bits = draw | R.POSITIVE_INFINITY
        if bits & R.FRACTION_MASK:
            out.append(bits)
            drawn += 1
            if drawn == N2_NAN_DRAWS:
                break
    return out


def n3_exp_log() -> list[int]:
    out = both_signs([double(Fraction(1, 2 ** k)) for k in range(1, 1075)])
    drawn = 0
    for draw in splitmix64(SEED_N3_UNIFORM):
        u = Fraction(draw >> 11, 1 << 53)
        if u == 0:
            continue
        out.append(double(-50 + 100 * u))
        drawn += 1
        if drawn == N3_UNIFORM:
            break
    return out


def n3_cubes() -> list[int]:
    return [double(n ** 3) for n in range(-(1 << 17), (1 << 17) + 1)]


def special_set() -> list[int]:
    two53 = 1 << 53
    values = both_signs([double(0), double(1), double(Fraction(1, 2)), double(2), double(3), R.POSITIVE_INFINITY])
    values += [R.CANONICAL_NAN, double(Fraction("1e-300")), double(Fraction("1e300"))]
    values += both_signs([double(two53 - 1)])
    # "±2^53 + 1": 2^53 + 1 is not a double; the revision reads it as the doubles either side of it.
    values += both_signs([double(two53), double(two53 + 2)])
    values.append(double(Fraction("0.1")))
    return values


def draw_index(stream, size: int) -> int:
    return next(stream) % size


def n3_pow(n1_values: list[int]) -> list[str]:
    s = special_set()
    lines = [f"{f64(a)},{f64(b)}" for a in s for b in s]
    stream = splitmix64(SEED_N3_POW)
    for _ in range(N3_POW_PAIRS):
        a = n1_values[draw_index(stream, len(n1_values))]
        b = n1_values[draw_index(stream, len(n1_values))]
        lines.append(f"{f64(a)},{f64(b)}")
    return lines


def with_exponent(stream, exponent: int) -> int:
    """A double of binary exponent `exponent` (a normal one), drawn significand and sign."""
    draw = next(stream)
    return ((exponent + 1023) << 52) | (draw & R.FRACTION_MASK) | (R.SIGN_BIT if draw >> 63 else 0)


def n3_hypot(n1_values: list[int]) -> list[str]:
    s = special_set()
    lines = [sequence([a, b]) for a in s for b in s] + [sequence([a, b, c]) for a in s for b in s for c in s]
    stream = splitmix64(SEED_N3_HYPOT)
    for i in range(N3_HYPOT_TUPLES):
        length = 2 + next(stream) % 4
        kind = i % 3
        if kind == 0:
            terms = [n1_values[draw_index(stream, len(n1_values))] for _ in range(length)]
        elif kind == 1:
            exponent = -1022 + next(stream) % 2046
            terms = [with_exponent(stream, exponent) for _ in range(length)]
        else:
            low = -1022 + next(stream) % 22
            high = low + 1001 + next(stream) % (1023 - low - 1000)
            terms = [with_exponent(stream, low if t % 2 == 0 else high) for t in range(length)]
        lines.append(sequence(terms))
    return lines


# --------------------------------------------------------------------------------------------------
# Precise sums.

# The test cases of the Math.sumPrecise proposal's reference: tc39/proposal-math-sum at 341ddea,
# polyfill/test-polyfill.mjs, every case of its tests "basic ability to handle intermediate
# overflows", "special cases", "1, 2 element lists" and "some edge cases from the fuzzer" (its
# "fuzzing" test draws an unseeded random set, which has no fixed cases).
PROPOSAL_CASES = [
    "1e308 1e308 0.1 0.1 1e30 0.1 -1e30 -1e308 -1e308",
    "1e30 0.1 -1e30",
    "",
    "-0 -0 -0",
    "-0 -0 -0 0",
    "1 Infinity -1e308",
    "1 Infinity -1e308 -Infinity",
    "Infinity",
    "-Infinity",
    "-Infinity Infinity",
    "1e308", "1e308 1e308", "1e308 -1e308", "0.1", "0.1 0.1", "0.1 -0.1",
    "8.98846567431158e+307 8.988465674311579e+307 -1.7976931348623157e+308",
    "-5.630637621603525e+255 9.565271205476345e+307 2.9937604643020797e+292",
    "6.739986666787661e+66 2 -1.2689709186578243e-116 1.7046015739467354e+308 -9.979201547673601e+291 "
    "6.160926733208294e+307 -3.179557053031852e+234 -7.027282978772846e+307 -0.7500000000000001",
    "0.31150493246968836 -8.988465674311582e+307 1.8315037361673755e-270 -15.999999999999996 2.9999999999999996 "
    "7.345200721499384e+164 -2.033582473639399 -8.98846567431158e+307 -3.5737295155405993e+292 4.13894772383715e-124 "
    "-3.6111186457260667e-35 2.387234887098013e+180 7.645295562778372e-298 3.395189016861822e-103 "
    "-2.6331611115768973e-149",
    "-1.1442589134409902e+308 9.593842098384855e+138 4.494232837155791e+307 -1.3482698511467367e+308 "
    "4.494232837155792e+307",
    "-1.1442589134409902e+308 4.494232837155791e+307 -1.3482698511467367e+308 4.494232837155792e+307",
    "9.593842098384855e+138 -6.948356297254111e+307 -1.3482698511467367e+308 4.494232837155792e+307",
    "-2.534858246857893e+115 8.988465674311579e+307 8.98846567431158e+307",
    "1.3588124894186193e+308 1.4803986201152006e+223 6.741349255733684e+307",
    "6.741349255733684e+307 1.7976931348623155e+308 -7.388327292663961e+41",
    "-1.9807040628566093e+28 1.7976931348623157e+308 9.9792015476736e+291",
    "-1.0214557991173964e+61 1.7976931348623157e+308 8.98846567431158e+307 -8.988465674311579e+307",
]


def literal(text: str) -> int:
    if text in ("Infinity", "-Infinity"):
        return R.POSITIVE_INFINITY if text[0] != "-" else R.NEGATIVE_INFINITY
    if text == "-0":
        return R.NEGATIVE_ZERO
    return double(Fraction(text))


def n4() -> list[str]:
    cases = [[literal(t) for t in case.split()] for case in PROPOSAL_CASES]
    one, big, tiny, tenth = double(1), double(Fraction("1e308")), R.SMALLEST_SUBNORMAL, double(Fraction("0.1"))
    for length in range(0, 9):
        cases.append([R.NEGATIVE_ZERO] * length)
        if length >= 1:
            cases.append([0] + [R.NEGATIVE_ZERO] * (length - 1))
            cases.append([R.NEGATIVE_ZERO] * (length - 1) + [0])
        if length >= 2:
            for a in (one, big, tiny, tenth, R.MAX_FINITE):
                cases.append([a, a ^ R.SIGN_BIT] + [R.NEGATIVE_ZERO] * (length - 2))
                cases.append([R.NEGATIVE_ZERO] * (length - 2) + [a ^ R.SIGN_BIT, a])
    specials = [R.POSITIVE_INFINITY, R.NEGATIVE_INFINITY, R.CANONICAL_NAN, one, R.NEGATIVE_ZERO]
    for a in specials:
        cases.append([a])
        for b in specials:
            cases.append([a, b])
            for c in specials:
                cases.append([a, b, c])
    m = R.MAX_FINITE
    for k in range(1, 5):
        cases.append([m] * k + [m ^ R.SIGN_BIT] * k)
        cases.append([m] * k + [m ^ R.SIGN_BIT] * (k - 1))
        cases.append([m, m ^ R.SIGN_BIT] * k)
        cases.append([m] * k + [m ^ R.SIGN_BIT] * k + [tiny])
        cases.append([m] * (k + 1) + [m ^ R.SIGN_BIT] * k + [R.NEGATIVE_INFINITY])
    for large in (big, R.MAX_FINITE, double(Fraction(2) ** 1000), one):
        for small in (tiny, 0x0010000000000000, double(Fraction("1e-300"))):
            a, b, c = large, small, large ^ R.SIGN_BIT
            for order in ((a, b, c), (a, c, b), (b, a, c), (b, c, a), (c, a, b), (c, b, a)):
                cases.append(list(order))
    lines = [sequence(case) for case in cases]
    stream = splitmix64(SEED_N4)
    for _ in range(N4_SEQUENCES):
        length = 1 + next(stream) % 64
        values = []
        for _ in range(length):
            draw = next(stream)
            field = draw % 2047
            values.append((draw & R.SIGN_BIT) | (field << 52) | ((draw >> 11) & R.FRACTION_MASK))
        lines.append(sequence(values))
    return lines


# --------------------------------------------------------------------------------------------------
# Exact rounding.

MODE_NAMES = ["ceil", "floor", "expand", "trunc", "halfCeil", "halfFloor", "halfExpand", "halfTrunc", "halfEven"]
UNKNOWN_NAMES = ["", "HalfEven", "halfeven", "half-even", "half_even", "Ceil", "CEIL", "ceiling", "round",
                 "halfExpand ", " trunc", "halfUp", "halfDown", "up", "down", "floor\u0000", "İexpand"]


def r1_names() -> list[str]:
    return [escape(name) for name in MODE_NAMES + UNKNOWN_NAMES]


def drawn_integer(stream, max_bits: int, signed: bool) -> int:
    bits = 1 + next(stream) % max_bits
    value = 0
    while value.bit_length() < bits:
        value = (value << 64) | next(stream)
    value = (value >> (value.bit_length() - bits)) | (1 << (bits - 1))
    return -value if signed and next(stream) & 1 else value


def r1_increment_lines() -> list[str]:
    stream = splitmix64(SEED_R1_INCREMENT)
    numerators = [drawn_integer(stream, 200, True) for _ in range(R1_NUMERATORS)]
    denominators = [drawn_integer(stream, 128, False) for _ in range(R1_DENOMINATORS)]
    increments = [drawn_integer(stream, 64, False) for _ in range(R1_INCREMENTS)]
    fixed_denominators = [1, 2, 3, 7, 10, 1000, 10 ** 9] + [2 ** k for k in range(1, 129)]
    fixed_increments = [1, 2, 5, 10, 20, 50, 100, 1000, 10 ** 9]
    pairs = [(d, i) for d in fixed_denominators for i in fixed_increments] + list(zip(denominators, increments))
    integers = list(range(-1000, 1001))
    lines = []
    for den, inc in pairs:
        prefix = f" {integer(den)} {integer(inc)}"
        lines += [integer(n) + prefix for n in integers]
        lines += [integer(n) + prefix for n in numerators]
        # The exact ties (k + 1/2) * inc, over 2 * den, and their neighbours a unit away.
        ks = list(R1_TIE_STEPS) + [n // (den * inc) for n in numerators[:R1_TIES_NEAR_DRAWN]]
        tie_prefix = f" {integer(2 * den)} {integer(inc)}"
        for k in ks:
            tie = (2 * k + 1) * inc * den
            lines += [integer(n) + tie_prefix for n in (tie - 1, tie, tie + 1)]
    return lines


def r1_window_lines() -> list[str]:
    """ApplyUnsignedRoundingMode's windows: "num den r1 r2", the value num / den in [r1, r2].

    The specification applies the operation to magnitudes (NudgeToCalendarUnit passes abs(r1) and
    abs(r2)), so every window is non-negative; the rule's "at both signs" is the sign the comparison
    passes to GetUnsignedRoundingMode. A width of 1 is one step; a wider one is a window, whose start
    is a multiple of its width, as the increment's multiples are.
    """
    stream = splitmix64(SEED_R1_INCREMENT ^ 0xFFFF)
    lines = []
    widths = [1, 2, 3, 10, 1000, 10 ** 9, 2 ** 64]
    starts = list(range(0, 7)) + [drawn_integer(stream, 64, False) for _ in range(16)]
    for width in widths:
        for k in starts:
            r1, r2 = k * width, (k + 1) * width
            for d in (3, 2 ** 64):
                den = 2 * d
                low, high, mid = r1 * den, r2 * den, (r1 + r2) * d
                for num in (low, low + 1, mid - 1, mid, mid + 1, high - 1, high):
                    lines.append(f"{integer(num)} {integer(den)} {integer(r1)} {integer(r2)}")
    return lines


def r1_rationals() -> list[str]:
    stream = splitmix64(SEED_R1_RATIONAL)
    lines = []
    for _ in range(R1_RATIONALS):
        num = drawn_integer(stream, 2400, True)
        den = drawn_integer(stream, 2400, False)
        lines.append(f"{integer(num)} {integer(den)}")
    # The exact midpoints between adjacent doubles, and their neighbours, over the subnormal range,
    # every binade's first and last pair, and the overflow threshold.
    scale = 1 << 1075
    points = list(range(0, 64)) + [(1 << 52) - 2, (1 << 52) - 1, 1 << 52, (1 << 52) + 1]
    for e in range(0, 2046, 7):
        base = e << 52
        points += [base, base + 1, base + (1 << 51), base + R.FRACTION_MASK - 1]
    for bits in unique(points + [R.MAX_FINITE - 1, R.MAX_FINITE]):
        low = R.to_fraction(bits)
        high = R.to_fraction(bits + 1) if bits + 1 < R.POSITIVE_INFINITY else Fraction(2) ** 1024
        mid = (low + high) / 2
        for num, den in ((mid.numerator * scale, mid.denominator * scale),):
            for delta in (-1, 0, 1):
                for sign in (1, -1):
                    lines.append(f"{integer(sign * (num + delta))} {integer(den)}")
    return lines


# --------------------------------------------------------------------------------------------------
# BigInt.

def b1_values() -> list[int]:
    values = [0, 1, -1]
    for k in range(1, 4101):
        p = 1 << k
        values += [p, -p, p + 1, -(p + 1), p - 1, -(p - 1)]
    for k in range(VM_CEILING - 2, VM_CEILING + 2):
        p = 1 << k
        values += [p, -p, p + 1, -(p + 1), p - 1, -(p - 1)]
    stream = splitmix64(SEED_B1_VALUES)
    for _ in range(B1_DRAWN):
        values.append(drawn_integer(stream, 70000, True))
    return values


def b1_pairs(values: list[int]) -> list[str]:
    """Pairs of B1V line numbers: the drawn pairs, then each of the first 256 drawn left operands with
    0n, 1n and -1n (B1V's lines 0, 1 and 2) on either side, so that zero divisors and exponents, and the
    trivial operands, are in the population whatever the draws give."""
    stream = splitmix64(SEED_B1_PAIRS)
    drawn = [(draw_index(stream, len(values)), draw_index(stream, len(values))) for _ in range(B1_PAIRS)]
    small = [(a, z) for a, _ in drawn[:256] for z in (0, 1, 2)] + [(z, a) for a, _ in drawn[:256] for z in (0, 1, 2)]
    return [f"{a} {b}" for a, b in drawn + small]


def b1_shifts(values: list[int]) -> list[str]:
    lines = []
    for index, value in enumerate(values):
        size = value.bit_length()
        counts = unique([0, 1, 63, 64, 65, size - 1, size, size + 1, 1 << 20, 1 << 21])
        lines += [f"{index} {integer(sign * c)}" for c in counts if c >= 0 for sign in ((1, -1) if c else (1,))]
    return lines


def b1_widths(values: list[int]) -> list[str]:
    lines = []
    for index, value in enumerate(values):
        size = value.bit_length()
        widths = unique([0, 1, 63, 64, 65, max(size - 1, 0), size, size + 1, 1 << 20, (1 << 20) + 1,
                         1 << 31, 1 << 32, (1 << 32) + 3, (1 << 53) - 1])
        lines += [f"{index} {w:x}" for w in widths]
    return lines


def b1_to_number() -> list[str]:
    lines = []
    for e in range(52, 1026):
        p = 1 << e
        for k in range(-2, 3):
            lines += [integer(p + k), integer(-(p + k))]
        # The halfway cases between adjacent doubles of this binade, and a unit either side.
        if e >= 53:
            half = 1 << (e - 53)
            for m in (p + half, p + 3 * half, 2 * p - half):
                lines += [integer(m - 1), integer(m), integer(m + 1)]
    return lines


def b1_compared(n1_values: list[int]) -> list[str]:
    # N1's special doubles, its first group.
    specials = both_signs([0, 1, 0x000FFFFFFFFFFFFF, 0x0010000000000000, R.MAX_FINITE, R.POSITIVE_INFINITY])
    specials.append(R.CANONICAL_NAN)
    fixed = len(n1_values) - N1_DRAWS
    return [f64(b) for b in unique(specials) + n1_values[fixed:fixed + B1_COMPARED_DRAWS]]


# --------------------------------------------------------------------------------------------------

def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--out", required=True, type=Path)
    args = parser.parse_args(argv)
    args.out.mkdir(parents=True, exist_ok=True)
    sys.set_int_max_str_digits(0)
    w = Writer(args.out)

    n1_values = n1()
    w.write("N1", [f64(b) for b in n1_values], f"doubles: the fixed set, then {N1_DRAWS} draws of seed {hex(SEED_N1)}")
    w.write("N2D", [f16(h) for h in range(0x10000)], "every binary16 bit pattern")
    w.write("N2E", [f64(b) for b in n2_encode()], "every finite binary16 value and every midpoint, and their neighbours")
    w.write("N2N", [f64(b) for b in n2_nan()], f"NaN patterns, then {N2_NAN_DRAWS} draws of seed {hex(SEED_N2)}")
    w.write("N3E", [f64(b) for b in n3_exp_log()], f"+-2^-k, then {N3_UNIFORM} uniform draws in (-50, 50)")
    w.write("N3C", [f64(b) for b in n3_cubes()], "n^3 for n from -2^17 to 2^17")
    w.write("N3P", n3_pow(n1_values), f"the special set's pairs, then {N3_POW_PAIRS} pairs drawn from N1")
    w.write("N3H", n3_hypot(n1_values), f"the special set's 2- and 3-tuples, then {N3_HYPOT_TUPLES} drawn tuples")
    w.write("N4", n4(), f"the proposal's cases and the constructed sequences, then {N4_SEQUENCES} drawn sequences")
    w.write("R1M", r1_names(), "the nine rounding-mode names and unknown names")
    w.write("R1I", r1_increment_lines(), "num den inc: the increment roundings' inputs")
    w.write("R1W", r1_window_lines(), "num den r1 r2: ApplyUnsignedRoundingMode's windows")
    w.write("R1D", r1_rationals(), "num den: rationals for the nearest double")
    values = b1_values()
    w.write("B1V", [integer(v) for v in values], f"BigInt values, then {B1_DRAWN} draws of seed {hex(SEED_B1_VALUES)}")
    w.write("B1P", b1_pairs(values), f"{B1_PAIRS} pairs of B1V line numbers drawn with seed {hex(SEED_B1_PAIRS)}")
    w.write("B1S", b1_shifts(values), "B1V line number, shift count")
    w.write("B1N", b1_widths(values), "B1V line number, asIntN/asUintN width (hexadecimal)")
    w.write("B1E", b1_to_number(), "2^e + k and the halfway cases, for a BigInt's Number")
    w.write("B1C", b1_compared(n1_values), "the doubles every B1V value is compared with")
    w.manifest()
    return 0


if __name__ == "__main__":
    sys.exit(main())
