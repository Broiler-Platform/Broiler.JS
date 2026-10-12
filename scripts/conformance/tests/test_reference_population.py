"""Tests of the reference-arm population generator (bundle sjl-3-001's rule).

The rule asks that the generator's SplitMix64 be tested against the generator's published reference
outputs before the population is trusted; the rest checks that each class is what the rule declares.
"""

from __future__ import annotations

from fractions import Fraction
from pathlib import Path
import sys
import unittest

CONFORMANCE_DIR = Path(__file__).resolve().parents[1]
if str(CONFORMANCE_DIR) not in sys.path:
    sys.path.insert(0, str(CONFORMANCE_DIR))

import generate_reference_population as P  # noqa: E402
import numref as R  # noqa: E402
from generate_number_vectors import splitmix64  # noqa: E402


class SplitMix64Tests(unittest.TestCase):
    def test_the_published_reference_outputs(self) -> None:
        # Seed 1234567, the first five outputs of Vigna's splitmix64.c, as the reference
        # implementations in other languages publish them.
        stream = splitmix64(1234567)
        self.assertEqual([next(stream) for _ in range(5)],
                         [6457827717110365317, 3203168211198807973, 9817491932198370423,
                          4593380528125082431, 16408922859458223821])


class ClassTests(unittest.TestCase):
    def test_d1_holds_the_specials_at_both_signs_and_nan(self) -> None:
        d1 = P.d1()
        self.assertEqual(13, len(d1))
        self.assertIn(R.NEGATIVE_ZERO, d1)
        self.assertIn(R.NEGATIVE_INFINITY, d1)
        self.assertIn(R.CANONICAL_NAN, d1)

    def test_d2_and_d3_hold_each_power_with_its_neighbours(self) -> None:
        self.assertEqual(3 * (1023 + 1074 + 1), len(P.d2()))
        self.assertEqual(3 * (308 + 323 + 1), len(P.d3()))

    def test_d6_skips_nan_draws_without_counting_them(self) -> None:
        stream = splitmix64(P.SEED_D6)
        first = [d for d in (next(stream) for _ in range(64)) if not R.is_nan(d)]
        self.assertEqual(first[:16], P.d6()[:16])

    def test_t1_values_are_exact_ties_at_their_digit_count(self) -> None:
        values = P.t1()
        self.assertEqual(101 * P.TIES_PER_COUNT, len(values))
        for f in (0, 1, 7, 50, 100):
            for bits in values[f * P.TIES_PER_COUNT:(f + 1) * P.TIES_PER_COUNT][:8]:
                scaled = R.to_fraction(bits) * 10 ** f
                self.assertEqual(Fraction(1, 2), scaled - (scaled.numerator // scaled.denominator))

    def test_t2_values_have_one_more_significant_digit_than_their_precision_ending_in_5(self) -> None:
        values = P.t2()
        self.assertEqual(100 * P.TIES_PER_COUNT, len(values))
        for p in (1, 2, 15, 16, 17, 60, 100):
            for bits in values[(p - 1) * P.TIES_PER_COUNT:p * P.TIES_PER_COUNT][:8]:
                digits = P.significant_digits(R.to_fraction(bits))
                self.assertEqual(p + 1, len(digits))
                self.assertTrue(digits.endswith("5"))

    def test_p2_strings_bracket_the_midpoint(self) -> None:
        bits = R.string_to_number("0.1")[0]
        strings = P.p2_for(bits)
        midpoint = (R.to_fraction(bits) + R.to_fraction(R.next_up(bits))) / 2
        self.assertEqual(midpoint, _exact(strings[-1]))
        # Per length: the truncation, one unit above it, one unit below it.
        self.assertLessEqual(_exact(strings[0]), midpoint)
        self.assertLess(midpoint, _exact(strings[1]))
        self.assertLess(_exact(strings[2]), midpoint)
        self.assertEqual(3 * len(P.P2_LENGTHS) + 1, len(strings))

    def test_escaping_survives_every_code_unit(self) -> None:
        for code in (0, 9, 10, 13, 32, 0x5C, 0x7F, 0xA0, 0xD800, 0xDFFF, 0xFEFF, 0xFFFF):
            escaped = P.escape(chr(code) + "1")
            self.assertTrue(all("!" <= c <= "~" for c in escaped))
            self.assertEqual(chr(code) + "1", escaped.encode("ascii").decode("unicode_escape"))


def _exact(text: str) -> Fraction:
    mantissa, _, exponent = text.partition("e")
    whole, _, fraction = mantissa.partition(".")
    return Fraction(int(whole + fraction)) / 10 ** len(fraction) * Fraction(10) ** int(exponent)


if __name__ == "__main__":
    unittest.main()
