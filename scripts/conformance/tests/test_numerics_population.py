"""Tests of the numerics reference-arm population generator (bundle sjl-4-001's rule, as revised on
2026-10-13). The SplitMix64 it draws from is tested against its published outputs in
test_reference_population.py; these check that each class is what the rule and its revision declare.
"""

from __future__ import annotations

from fractions import Fraction
from pathlib import Path
import sys
import unittest

CONFORMANCE_DIR = Path(__file__).resolve().parents[1]
if str(CONFORMANCE_DIR) not in sys.path:
    sys.path.insert(0, str(CONFORMANCE_DIR))

import generate_numerics_population as P  # noqa: E402
import numref as R  # noqa: E402


class ClassTests(unittest.TestCase):
    def test_the_special_set_reads_two_to_the_53_plus_one_as_its_neighbours(self) -> None:
        s = P.special_set()
        self.assertEqual(22, len(s))
        self.assertIn(P.double(1 << 53), s)
        self.assertIn(P.double((1 << 53) + 2), s)
        self.assertIn(R.CANONICAL_NAN, s)

    def test_the_nan_patterns_cover_sign_quiet_bit_and_payload_bits(self) -> None:
        patterns = P.n2_nan()
        self.assertEqual(30 + P.N2_NAN_DRAWS, len(patterns))
        self.assertTrue(all(R.is_nan(b) for b in patterns))
        self.assertIn(R.POSITIVE_INFINITY | 1, patterns)
        self.assertIn(R.SIGN_BIT | R.POSITIVE_INFINITY | (1 << 51) | (1 << 50) | (1 << 25) | 1, patterns)

    def test_every_binary16_midpoint_is_a_double_and_the_top_one_is_the_overflow_threshold(self) -> None:
        values = P.n2_encode()
        self.assertIn(P.double(65520), values)
        self.assertIn(P.double(65520) ^ R.SIGN_BIT, values)
        self.assertIn(P.double(Fraction(1, 1 << 25)), values)

    def test_windows_are_non_negative_and_start_at_a_multiple_of_their_width(self) -> None:
        for line in P.r1_window_lines():
            num, den, r1, r2 = (int(f, 16) if not f.startswith("-") else -int(f[1:], 16) for f in line.split())
            self.assertGreaterEqual(r1, 0)
            self.assertEqual(0, r1 % (r2 - r1))
            self.assertTrue(r1 * den <= num <= r2 * den)

    def test_the_ties_are_exact(self) -> None:
        lines = P.r1_increment_lines()
        tie = f"{P.integer(3 * 10 * 7)} {P.integer(2 * 7)} {P.integer(10)}"
        self.assertIn(tie, lines)  # (1 + 1/2) * 10, over twice the denominator 7

    def test_the_bigint_values_and_the_trivial_operand_pairs(self) -> None:
        values = P.b1_values()
        self.assertEqual(3 + 6 * 4100 + 6 * 4 + P.B1_DRAWN, len(values))
        self.assertEqual([0, 1, -1], values[:3])
        self.assertIn(1 << (1 << 20), values)
        pairs = P.b1_pairs(values)
        self.assertEqual(P.B1_PAIRS + 6 * 256, len(pairs))
        self.assertTrue(any(line.endswith(" 0") for line in pairs[P.B1_PAIRS:]))


if __name__ == "__main__":
    unittest.main()
