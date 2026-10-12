from __future__ import annotations

from fractions import Fraction
from pathlib import Path
import random
import struct
import sys
import tempfile
import unittest


CONFORMANCE_DIR = Path(__file__).resolve().parents[1]
REPOSITORY = CONFORMANCE_DIR.parents[1]
if str(CONFORMANCE_DIR) not in sys.path:
    sys.path.insert(0, str(CONFORMANCE_DIR))

import generate_number_vectors as generator
import numref as R


def bits(value: float) -> int:
    return struct.unpack(">Q", struct.pack(">d", value))[0]


def double(pattern: int) -> float:
    return struct.unpack(">d", struct.pack(">Q", pattern))[0]


def from_cpython_repr(value: float) -> str:
    """JavaScript's Number::toString at radix 10, built from CPython's shortest repr: a second
    implementation of the shortest round-trip digits, independent of numref and of both engines."""
    if value == 0:
        return "0"
    if value < 0:
        return "-" + from_cpython_repr(-value)
    mantissa, _, exponent = repr(value).partition("e")
    whole, _, fraction = mantissa.partition(".")
    digits = (whole + fraction).lstrip("0")
    leading_zeros = len(whole + fraction) - len(digits)
    n = len(whole) + int(exponent or 0) - leading_zeros
    digits = digits.rstrip("0")
    return R.number_to_string_from(int(digits), len(digits), n, 10)


class RoundingTests(unittest.TestCase):
    def test_from_fraction_agrees_with_cpython_division(self) -> None:
        rng = random.Random(1)
        for _ in range(5000):
            numerator = rng.getrandbits(rng.randint(1, 1200))
            denominator = rng.getrandbits(rng.randint(1, 1200)) or 1
            value = Fraction(numerator, denominator)
            try:
                expected = bits(float(value))
            except OverflowError:
                expected = R.POSITIVE_INFINITY
            self.assertEqual(expected, R.from_fraction(value), value)

    def test_to_fraction_is_exact(self) -> None:
        for pattern in (1, R.LARGEST_SUBNORMAL, R.SMALLEST_NORMAL, R.MAX_FINITE, bits(0.1), bits(-2.5)):
            self.assertEqual(Fraction(double(pattern)), R.to_fraction(pattern))
            self.assertEqual(pattern, R.from_fraction(R.to_fraction(pattern)))

    def test_ties_round_to_even(self) -> None:
        self.assertEqual(bits(9007199254740992.0), R.from_fraction(Fraction(2 ** 53 + 1)))
        self.assertEqual(bits(9007199254740996.0), R.from_fraction(Fraction(2 ** 53 + 3)))
        self.assertEqual(R.POSITIVE_INFINITY, R.from_fraction(Fraction(2 ** 1024 - 2 ** 970)))
        self.assertEqual(R.MAX_FINITE, R.from_fraction(Fraction(2 ** 1024 - 2 ** 970 - 1)))


class NumberToStringTests(unittest.TestCase):
    def test_radix_10_recommendation_agrees_with_cpython(self) -> None:
        rng = random.Random(2)
        patterns = [1, 2, R.LARGEST_SUBNORMAL, R.SMALLEST_NORMAL, R.MAX_FINITE, bits(1e21), bits(1e-7),
                    bits(2.0 ** -25), bits(123.456)]
        while len(patterns) < 5000:
            pattern = rng.getrandbits(64) & ~R.SIGN_BIT
            if R.is_finite(pattern) and pattern:
                patterns.append(pattern)
        for pattern in patterns:
            result = R.number_to_string(pattern)
            expected = from_cpython_repr(double(pattern))
            self.assertIn(expected, result.values, hex(pattern))
            self.assertEqual(expected, result.recommended, hex(pattern))

    def test_every_admissible_value_round_trips(self) -> None:
        for pattern in (1, 2, R.SMALLEST_NORMAL, R.MAX_FINITE, bits(2.0 ** 53)):
            for value in R.number_to_string(pattern).values:
                self.assertEqual((pattern,), R.string_to_number(value), value)

    def test_known_answers(self) -> None:
        self.assertEqual(("NaN",), R.number_to_string(R.CANONICAL_NAN).values)
        self.assertEqual(("0",), R.number_to_string(R.NEGATIVE_ZERO).values)
        self.assertEqual(("-Infinity",), R.number_to_string(R.NEGATIVE_INFINITY).values)
        self.assertEqual("1e+21", R.number_to_string(bits(1e21)).recommended)
        self.assertEqual("1e-7", R.number_to_string(bits(1e-7)).recommended)
        self.assertEqual("0.000001", R.number_to_string(bits(1e-6)).recommended)
        self.assertEqual("ff", R.number_to_string(bits(255.0), 16).recommended)
        self.assertEqual("1" + "0" * 64, R.number_to_string(bits(2.0 ** 64), 2).recommended)
        self.assertEqual(("9007199254740992", "9007199254740993"), R.number_to_string(bits(2.0 ** 53)).values)

    def test_the_smallest_subnormal_admits_every_one_digit_string_that_rounds_to_it(self) -> None:
        self.assertEqual(tuple(f"{d}e-324" for d in range(3, 8)), R.number_to_string(1).values)


class DigitMethodTests(unittest.TestCase):
    def test_to_fixed(self) -> None:
        self.assertEqual("1000000000000000128", R.to_fixed(bits(1000000000000000128.0), 0))
        self.assertEqual("1", R.to_fixed(bits(0.5), 0))
        self.assertEqual("3", R.to_fixed(bits(2.5), 0))
        self.assertEqual("1.00", R.to_fixed(bits(1.005), 2))
        self.assertEqual("-2", R.to_fixed(bits(-1.5), 0))
        self.assertEqual("1e+21", R.to_fixed(bits(1e21), 2))
        self.assertEqual("0.000", R.to_fixed(bits(-0.0), 3))

    def test_to_exponential(self) -> None:
        self.assertEqual("1.23e+5", R.to_exponential(bits(123456.0), 2).recommended)
        self.assertEqual("0e+0", R.to_exponential(bits(0.0), None).recommended)
        self.assertEqual("1.23e-4", R.to_exponential(bits(0.000123), None).recommended)
        self.assertEqual("1.3e+2", R.to_exponential(bits(125.0), 1).recommended)

    def test_to_precision(self) -> None:
        self.assertEqual("123.5", R.to_precision(bits(123.456), 4))
        self.assertEqual("0.0000012", R.to_precision(bits(0.000001234), 2))
        self.assertEqual("1.00e+21", R.to_precision(bits(1e21), 3))
        self.assertEqual("0.00", R.to_precision(bits(0.0), 3))
        self.assertEqual("1.2e+5", R.to_precision(bits(123456.0), 2))
        self.assertEqual("1.3e+2", R.to_precision(bits(125.0), 2))


class StringTests(unittest.TestCase):
    def test_short_decimal_strings_agree_with_cpython(self) -> None:
        rng = random.Random(3)
        for _ in range(3000):
            digits = str(rng.getrandbits(rng.randint(1, 56)))
            exponent = rng.randint(-340, 320)
            text = f"{digits}e{exponent}"
            if len(digits.strip("0")) > 20:
                continue
            self.assertEqual((bits(float(text)),), R.string_to_number(text), text)

    def test_round_mv_result_admits_both_options_beyond_20_digits(self) -> None:
        # 2**53 + 1 and 3 zeroes after 20 digits: option1 is 2**53 exactly, option2 is above the
        # midpoint, so the two options round to different doubles.
        text = "9007199254740993000000000000001e-15"
        self.assertEqual(2, len(R.string_to_number(text)))

    def test_white_space(self) -> None:
        self.assertEqual((bits(7.0),), R.string_to_number("\u00a07\ufeff"))
        self.assertEqual((R.CANONICAL_NAN,), R.string_to_number("\u00851"))
        self.assertEqual((0,), R.string_to_number("\u2028\u3000"))

    def test_non_decimal_strings_are_exact(self) -> None:
        self.assertEqual((R.from_fraction(Fraction(0x200000000000011)),), R.string_to_number("0x200000000000011"))
        self.assertEqual((R.CANONICAL_NAN,), R.string_to_number("-0x1"))
        self.assertEqual((R.CANONICAL_NAN,), R.string_to_number("1_000"))

    def test_parse_int(self) -> None:
        self.assertEqual((bits(31.0),), R.parse_int("0x1F", None).values)
        self.assertEqual((0,), R.parse_int("0x1F", 10).values)
        self.assertEqual((R.NEGATIVE_ZERO,), R.parse_int("-0", None).values)
        self.assertEqual((R.CANONICAL_NAN,), R.parse_int("123", 37).values)
        self.assertEqual((bits(291.0),), R.parse_int("123", 4294967312).values)
        self.assertTrue(R.parse_int("zz", 36).approximated)
        self.assertFalse(R.parse_int("zz", 32).approximated)
        self.assertEqual((bits(float(0x200000000000011)),), R.parse_int("200000000000011", 16).values)

    def test_parse_float(self) -> None:
        self.assertEqual((0,), R.parse_float("0x1F"))
        self.assertEqual((bits(1.0),), R.parse_float("1e+"))
        self.assertEqual((R.POSITIVE_INFINITY,), R.parse_float("Infinityx"))
        self.assertEqual((R.CANONICAL_NAN,), R.parse_float("."))


class LiteralTests(unittest.TestCase):
    def test_values(self) -> None:
        self.assertEqual((bits(31.0),), R.numeric_literal("0x1_F").numbers)
        self.assertEqual((bits(8.0),), R.numeric_literal("010").numbers)
        self.assertEqual((bits(8.5),), R.numeric_literal("08.5").numbers)
        self.assertEqual(123, R.numeric_literal("123n").bigint)
        self.assertEqual(0, R.numeric_literal("0n").bigint)

    def test_early_errors(self) -> None:
        for text in ("1__0", "1_", "0_1", "07.5", "01n", "08n", "1.5n", "1e3n", "0x", "0x_1", "1._5", "1_.5", "3in"):
            self.assertEqual("error", R.numeric_literal(text).kind, text)
        for text in ("010", "08", "09.5"):
            self.assertEqual("error", R.numeric_literal(text, strict=True).kind, text)


class GeneratorTests(unittest.TestCase):
    def test_the_committed_corpus_is_what_the_generator_writes(self) -> None:
        committed = REPOSITORY / "conformance" / "vectors" / "numbers"
        with tempfile.TemporaryDirectory() as scratch:
            self.assertEqual(0, generator.main(["--out", scratch]))
            produced = sorted(p.name for p in Path(scratch).iterdir())
            self.assertEqual(sorted(p.name for p in committed.glob("*.vectors")), produced)
            for name in produced:
                self.assertEqual((committed / name).read_bytes(), (Path(scratch) / name).read_bytes(), name)

    def test_the_generator_writes_the_revision_REVISIONS_md_ends_at(self) -> None:
        revisions = (REPOSITORY / "conformance" / "vectors" / "REVISIONS.md").read_text(encoding="utf-8")
        rows = [int(line.split("|")[1]) for line in revisions.splitlines() if line[:2] == "| " and line.split("|")[1].strip().isdigit()]
        self.assertEqual(list(range(1, len(rows) + 1)), rows)
        self.assertEqual(generator.CORPUS_REVISION, rows[-1])


if __name__ == "__main__":
    unittest.main()
