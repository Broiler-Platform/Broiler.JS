from __future__ import annotations

from fractions import Fraction
from pathlib import Path
import math
import platform
import random
import struct
import sys
import tempfile
import unittest


CONFORMANCE_DIR = Path(__file__).resolve().parents[1]
REPOSITORY = CONFORMANCE_DIR.parents[1]
if str(CONFORMANCE_DIR) not in sys.path:
    sys.path.insert(0, str(CONFORMANCE_DIR))

import generate_numerics_vectors as generator
import numericsref as N
import numref as R


def bits(value: float) -> int:
    return struct.unpack(">Q", struct.pack(">d", value))[0]


def double(pattern: int) -> float:
    return struct.unpack(">d", struct.pack(">Q", pattern))[0]


def magnitude_distance(a: int, b: int) -> int:
    return abs((a & ~R.SIGN_BIT) - (b & ~R.SIGN_BIT))


# How far the platform's C library may be from a correctly rounded result. glibc documents expm1,
# log1p and pow within one unit in the last place and cbrt within a few; other C libraries document
# no bound, and the Windows runtime's expm1 is two units off at 1.46e-15. Off glibc the cross-check
# is coarser, and still refuses a reference that is wrong rather than misrounded.
GLIBC = platform.libc_ver()[0] == "glibc"
LIBM_ULPS = 1 if GLIBC else 8
CBRT_ULPS = 3 if GLIBC else 8


class FormatTests(unittest.TestCase):
    """CPython packs binary16 and binary32 with round-to-nearest, ties-to-even: a second implementation."""

    def test_binary16_and_binary32_agree_with_cpython(self) -> None:
        rng = random.Random(0x534A4C34)
        for _ in range(20000):
            pattern = rng.getrandbits(64)
            if not R.is_finite(pattern):
                continue
            value = double(pattern)
            try:
                expected = struct.unpack(">H", struct.pack(">e", value))[0]
                self.assertEqual(expected, N.float16_encode(pattern), hex(pattern))
            except OverflowError:
                self.assertEqual(0x7C00, N.float16_encode(pattern) & 0x7FFF, hex(pattern))
            try:
                expected = struct.unpack(">I", struct.pack(">f", value))[0]
                self.assertEqual(expected, N.convert(pattern, N.BINARY64, N.BINARY32), hex(pattern))
            except OverflowError:
                pass

    def test_every_binary16_decodes_exactly(self) -> None:
        for half in range(0x10000):
            decoded = N.float16_decode(half)
            if N.is_nan_in(half, N.BINARY16):
                self.assertEqual(R.CANONICAL_NAN, decoded)
                continue
            self.assertEqual(struct.unpack(">e", struct.pack(">H", half))[0], double(decoded))
            self.assertEqual(half, N.float16_encode(decoded))

    def test_ties_round_to_even_and_overflow(self) -> None:
        self.assertEqual(0x3C00, N.float16_encode(bits(1 + 2 ** -11)))
        self.assertEqual(0x3C02, N.float16_encode(bits(1 + 3 * 2 ** -11)))
        # Directly to binary16, not through binary32: binary32 would round this to the tie first.
        self.assertEqual(0x3C01, N.float16_encode(bits(1 + 2 ** -11 + 2 ** -40)))
        self.assertEqual(0x7C00, N.float16_encode(bits(65520.0)))
        self.assertEqual(0x7BFF, N.float16_encode(bits(65519.99)))
        self.assertEqual(0x0000, N.float16_encode(bits(2 ** -25)))
        self.assertEqual(0x0001, N.float16_encode(bits(2 ** -25 + 2 ** -60)))

    def test_nan_preserving_encoding_keeps_sign_and_top_payload(self) -> None:
        self.assertEqual(0x7E00, N.float16_nan_preserving(0x7FF8000000000000))
        self.assertEqual(0xFE00, N.float16_nan_preserving(0xFFF8000000000000))
        self.assertEqual(0x7E00, N.float16_nan_preserving(0x7FF0000000000001))
        self.assertEqual(0x7F00, N.float16_nan_preserving(0x7FF4000000000000))
        self.assertEqual(0xFFFF, N.float16_nan_preserving(0xFFFFFFFFFFFFFFFF))


class ConversionTests(unittest.TestCase):
    def test_modular_conversions(self) -> None:
        self.assertEqual(-1, N.modular(bits(2 ** 32 - 1), 32, True))
        self.assertEqual(4294967295, N.modular(bits(-1.5), 32, False))
        self.assertEqual(0, N.modular(bits(2 ** 64), 32, True))
        self.assertEqual(-32768, N.modular(bits(32768), 16, True))
        self.assertEqual(255, N.modular(bits(-1.0), 8, False))
        self.assertEqual(0, N.modular(R.CANONICAL_NAN, 8, True))
        self.assertEqual(0, N.modular(R.POSITIVE_INFINITY, 32, False))

    def test_clamp_rounds_ties_to_even(self) -> None:
        self.assertEqual([2, 4, 0, 255, 0, 255, 0], [N.to_uint8_clamp(bits(v)) for v in
                                                     (2.5, 3.5, -1.0, 300.0, float("nan"), float("inf"), 0.5)])

    def test_integer_or_infinity_length_and_index(self) -> None:
        self.assertEqual(0, N.to_integer_or_infinity(bits(-0.5)))
        self.assertEqual(0, N.to_integer_or_infinity(R.CANONICAL_NAN))
        self.assertEqual(bits(-1.0), N.to_integer_or_infinity(bits(-1.5)))
        self.assertEqual(N.MAX_SAFE_INTEGER, N.to_length(R.POSITIVE_INFINITY))
        self.assertEqual(0, N.to_length(bits(-5.0)))
        self.assertEqual(0, N.to_index(bits(-0.9)))
        self.assertIsNone(N.to_index(bits(-1.0)))
        self.assertIsNone(N.to_index(bits(2.0 ** 53)))
        self.assertEqual(N.MAX_SAFE_INTEGER, N.to_index(bits(2.0 ** 53 - 1)))

    def test_big_int64(self) -> None:
        self.assertEqual(-(2 ** 63), N.to_big_int64(2 ** 63))
        self.assertEqual(2 ** 64 - 1, N.to_big_uint64(-1))


class MathTests(unittest.TestCase):
    def test_round(self) -> None:
        self.assertEqual(R.NEGATIVE_ZERO, N.math_round(bits(-0.5)))
        self.assertEqual(0, N.math_round(bits(0.49999999999999994)))
        self.assertEqual(bits(-2.0), N.math_round(bits(-2.5)))
        self.assertEqual(bits(3.0), N.math_round(bits(2.5)))
        self.assertEqual(bits(2.0 ** 52), N.math_round(bits(2.0 ** 52 - 0.5)))

    def test_special_powers(self) -> None:
        nan = R.CANONICAL_NAN
        self.assertEqual(nan, N.exponentiate(bits(1.0), nan))
        self.assertEqual(nan, N.exponentiate(bits(-1.0), R.POSITIVE_INFINITY))
        self.assertEqual(bits(1.0), N.exponentiate(nan, R.NEGATIVE_ZERO))
        self.assertEqual(R.NEGATIVE_INFINITY, N.exponentiate(R.NEGATIVE_ZERO, bits(-3.0)))
        self.assertEqual(R.POSITIVE_INFINITY, N.exponentiate(R.NEGATIVE_ZERO, bits(-2.0 ** 53 - 2)))
        self.assertEqual(nan, N.exponentiate(bits(-8.0), bits(1 / 3)))

    def test_square_root_agrees_with_cpython(self) -> None:
        rng = random.Random(0x534A4C35)
        for _ in range(2000):
            pattern = rng.getrandbits(63)
            if not R.is_finite(pattern) or R.is_zero(pattern):
                continue
            self.assertEqual(bits(math.sqrt(double(pattern))), N._root(R.to_fraction(pattern), 2).correct)
            self.assertEqual(pattern, N.hypot([pattern | R.SIGN_BIT]).correct)
            a, b = rng.uniform(-1e100, 1e100), rng.uniform(-1e100, 1e100)
            exact = Fraction(a) ** 2 + Fraction(b) ** 2
            square = R.from_fraction(exact)
            if R.to_fraction(square) == exact:
                self.assertEqual(bits(math.sqrt(double(square))), N.hypot([bits(a), bits(b)]).correct)

    def test_scaled_hypotenuses_are_exact(self) -> None:
        for k in (0, 1000, -1074, 1018):
            result = N.hypot([bits(3.0 * 2.0 ** k), bits(4.0 * 2.0 ** k)])
            self.assertEqual((bits(5.0 * 2.0 ** k),), result.faithful)
        self.assertEqual(R.POSITIVE_INFINITY, N.hypot([R.MAX_FINITE, R.MAX_FINITE]).correct)

    def test_transcendental_functions_are_within_the_c_library_s_error(self) -> None:
        # The reference is correctly rounded, so the two differ by the C library's error at most: a unit
        # in the last place under glibc (cbrt: a few), and the coarser LIBM_ULPS elsewhere.
        rng = random.Random(0x534A4C36)
        for _ in range(1500):
            x = rng.uniform(-50, 50) * 10.0 ** rng.randint(-20, 1)
            self.assertLessEqual(magnitude_distance(N.expm1(bits(x)).correct, bits(math.expm1(x))), LIBM_ULPS, x)
            self.assertLessEqual(magnitude_distance(N.log1p(bits(abs(x))).correct, bits(math.log1p(abs(x)))), LIBM_ULPS, x)
            self.assertLessEqual(magnitude_distance(N.cbrt(bits(x)).correct, bits(math.cbrt(x))), CBRT_ULPS, x)
            y = rng.uniform(-30, 30)
            self.assertLessEqual(magnitude_distance(N.exponentiate(bits(abs(x) + 0.01), bits(y)).correct,
                                                    bits(math.pow(abs(x) + 0.01, y))), LIBM_ULPS, (x, y))

    def test_cube_roots_bracket_their_value(self) -> None:
        rng = random.Random(0x534A4C37)
        for _ in range(500):
            pattern = rng.getrandbits(63)
            if not R.is_finite(pattern) or R.is_zero(pattern):
                continue
            value = R.to_fraction(pattern)
            result = N.cbrt(pattern)
            root = R.to_fraction(result.correct)
            below, above = R.to_fraction(result.correct - 1), R.to_fraction(result.correct + 1)
            self.assertLessEqual(((below + root) / 2) ** 3, value)
            self.assertGreaterEqual(((root + above) / 2) ** 3, value)

    def test_near_zero(self) -> None:
        self.assertEqual((1, 2), N.expm1(1).faithful)
        self.assertEqual(R.SIGN_BIT | 1, N.log1p(R.SIGN_BIT | 1).correct)
        self.assertEqual(bits(-1.0), N.expm1(R.NEGATIVE_INFINITY))
        self.assertEqual(R.NEGATIVE_INFINITY, N.log1p(bits(-1.0)))

    def test_sum_precise_agrees_with_fsum_and_its_states(self) -> None:
        rng = random.Random(0x534A4C38)
        for _ in range(200):
            values = [rng.uniform(-1e20, 1e20) * 10.0 ** rng.randint(-30, 30) for _ in range(20)]
            self.assertEqual(bits(math.fsum(values)), N.sum_precise([bits(v) for v in values]))
        self.assertEqual(R.NEGATIVE_ZERO, N.sum_precise([]))
        self.assertEqual(0, N.sum_precise([R.NEGATIVE_ZERO, 0]))
        self.assertEqual(1, N.sum_precise([bits(1e308), bits(-1e308), 1]))
        self.assertEqual(bits(1e308), N.sum_precise([bits(1e308), bits(1e308), bits(-1e308)]))
        self.assertEqual(R.POSITIVE_INFINITY, N.sum_precise([R.MAX_FINITE, bits(2.0 ** 970)]))
        self.assertEqual(R.CANONICAL_NAN, N.sum_precise([R.POSITIVE_INFINITY, R.NEGATIVE_INFINITY]))

    def test_clz32_and_imul(self) -> None:
        self.assertEqual(32, N.clz32(bits(2.0 ** 32)))
        self.assertEqual(0, N.clz32(bits(-1.0)))
        self.assertEqual(-5, N.imul(bits(2.0 ** 32 - 1), bits(5.0)))


class RoundingModeTests(unittest.TestCase):
    def test_every_mode_at_a_tie_and_off_it(self) -> None:
        # mode: (5 by 10, -5 by 10, 4 by 10, -6 by 10)
        expected = {
            "ceil": (10, 0, 10, 0), "floor": (0, -10, 0, -10), "expand": (10, -10, 10, -10), "trunc": (0, 0, 0, 0),
            "halfCeil": (10, 0, 0, -10), "halfFloor": (0, -10, 0, -10), "halfExpand": (10, -10, 0, -10),
            "halfTrunc": (0, 0, 0, -10), "halfEven": (0, 0, 0, -10),
        }
        self.assertEqual(set(N.MODES), set(expected))
        for mode, results in expected.items():
            self.assertEqual(results, tuple(N.round_number_to_increment(x, 10, mode) for x in (5, -5, 4, -6)), mode)

    def test_half_even_looks_at_the_quotient(self) -> None:
        self.assertEqual(20, N.round_number_to_increment(15, 10, "halfEven"))
        self.assertEqual(20, N.round_number_to_increment(25, 10, "halfEven"))
        self.assertEqual(-20, N.round_number_to_increment(-25, 10, "halfEven"))

    def test_as_if_positive_and_negation(self) -> None:
        self.assertEqual(-20, N.round_number_to_increment_as_if_positive(-15, 10, "floor"))
        self.assertEqual(-10, N.round_number_to_increment_as_if_positive(-15, 10, "ceil"))
        self.assertEqual(-20, N.round_number_to_increment_as_if_positive(-15, 10, "halfFloor"))
        self.assertEqual(-10, N.round_number_to_increment_as_if_positive(-15, 10, "halfExpand"))
        self.assertEqual("floor", N.negate_rounding_mode("ceil"))
        self.assertEqual("halfEven", N.negate_rounding_mode("halfEven"))


class BigIntTests(unittest.TestCase):
    def test_division_truncates_and_remainder_takes_the_dividend_s_sign(self) -> None:
        self.assertEqual((-3, -1), (N.bigint_divide(-7, 2), N.bigint_remainder(-7, 2)))
        self.assertEqual((-3, 1), (N.bigint_divide(7, -2), N.bigint_remainder(7, -2)))
        self.assertIsNone(N.bigint_divide(1, 0))

    def test_shifts_floor(self) -> None:
        self.assertEqual(-1, N.bigint_left_shift(-5, -10))
        self.assertEqual(-3, N.bigint_left_shift(-5, -1))
        self.assertEqual(40, N.bigint_left_shift(5, 3))

    def test_as_int_n_and_as_uint_n(self) -> None:
        self.assertEqual(-(2 ** 63), N.as_int_n(64, 2 ** 63))
        self.assertEqual(2 ** 64 - 1, N.as_uint_n(64, -1))
        self.assertEqual(-1, N.as_int_n(2 ** 53 - 1, -1))
        self.assertEqual(5, N.as_uint_n(2 ** 53 - 1, 5))
        self.assertEqual(0, N.as_int_n(0, 12345))
        with self.assertRaises(ValueError):
            N.as_uint_n(2 ** 53 - 1, -1)

    def test_text_and_parse(self) -> None:
        self.assertEqual("-ff", N.bigint_to_string(-255, 16))
        self.assertEqual("z", N.bigint_to_string(35, 36))
        for text, value in (("", 0), (" 　﻿12 ", 12), ("-0", 0), ("0X1F", 31), ("007", 7), ("+12", 12)):
            self.assertEqual(value, N.string_to_big_int(text), repr(text))
        for text in ("-0x1", "0x", "1e3", "1_0", "᠎1", "1n", "١", "1.0", "+-1"):
            self.assertIsNone(N.string_to_big_int(text), repr(text))

    def test_number_conversion_ties_to_even(self) -> None:
        self.assertEqual(bits(2.0 ** 53), N.bigint_to_number(2 ** 53 + 1))
        self.assertEqual(bits(2.0 ** 53 + 4), N.bigint_to_number(2 ** 53 + 3))
        self.assertEqual(R.POSITIVE_INFINITY, N.bigint_to_number(2 ** 1024 - 2 ** 970))
        self.assertEqual(R.MAX_FINITE, N.bigint_to_number(2 ** 1024 - 2 ** 970 - 1))
        self.assertIsNone(N.number_to_big_int(bits(0.5)))
        self.assertEqual(0, N.number_to_big_int(R.NEGATIVE_ZERO))
        self.assertEqual(-1, N.compare_bigint_number(2 ** 53, bits(2.0 ** 53 + 2)))
        self.assertIsNone(N.compare_bigint_number(1, R.CANONICAL_NAN))

    def test_the_width_is_get_bit_length_s(self) -> None:
        self.assertEqual((0, 1, 1, 64, 64), tuple(N.bit_width(v) for v in (-1, 1, -2, 2 ** 63, -(2 ** 64))))


class GeneratorTests(unittest.TestCase):
    def test_the_committed_corpus_is_what_the_generator_writes(self) -> None:
        committed = REPOSITORY / "conformance" / "vectors" / "numerics"
        with tempfile.TemporaryDirectory() as scratch:
            self.assertEqual(0, generator.main(["--out", scratch]))
            produced = sorted(p.name for p in Path(scratch).iterdir())
            self.assertEqual(sorted(p.name for p in committed.glob("*.vectors")), produced)
            for name in produced:
                self.assertEqual((committed / name).read_bytes(), (Path(scratch) / name).read_bytes(), name)

    def test_the_generator_writes_the_revision_REVISIONS_md_ends_at(self) -> None:
        revisions = (REPOSITORY / "conformance" / "vectors" / "REVISIONS.md").read_text(encoding="utf-8")
        rows = [int(line.split("|")[1]) for line in revisions.splitlines() if line[:2] == "| " and line.split("|")[1].strip().isdigit()]
        self.assertEqual(generator.CORPUS_REVISION, rows[-1])


if __name__ == "__main__":
    unittest.main()
