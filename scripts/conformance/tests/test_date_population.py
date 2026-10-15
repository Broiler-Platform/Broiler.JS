"""Tests of bundle sjl-6-001's reference-arm population generator and of the reference's local time.

The rule asks that the generator's SplitMix64 be tested against the published reference outputs before
the population is trusted; the rest checks that each class is what the rule and its revision declare, and
that the reference's LocalTime, UTC and TimeZoneString, which arbitrate the comparison's differences, read
the zones as the specification asks.
"""

from __future__ import annotations

import math
from pathlib import Path
import sys
import unittest
from zoneinfo import ZoneInfo

CONFORMANCE_DIR = Path(__file__).resolve().parents[1]
if str(CONFORMANCE_DIR) not in sys.path:
    sys.path.insert(0, str(CONFORMANCE_DIR))

import dateref as D  # noqa: E402
import generate_date_population as P  # noqa: E402
from generate_number_vectors import splitmix64  # noqa: E402

DAY = 86_400_000
HOUR = 3_600_000
MINUTE = 60_000


def wall(year: int, month: int, day: int, hour: int = 0, minute: int = 0) -> int:
    return D.epoch_days(year, month, day) * DAY + hour * HOUR + minute * MINUTE


class SplitMix64Tests(unittest.TestCase):
    def test_the_published_reference_outputs(self) -> None:
        stream = splitmix64(1234567)
        self.assertEqual([next(stream) for _ in range(5)],
                         [6457827717110365317, 3203168211198807973, 9817491932198370423,
                          4593380528125082431, 16408922859458223821])

    def test_a_drawn_integer_stays_in_its_range(self) -> None:
        stream = splitmix64(P.SEED_E1)
        draws = [P.draw_integer(stream, -P.MAX_TIME, P.MAX_TIME) for _ in range(1000)]
        self.assertTrue(all(-P.MAX_TIME <= d <= P.MAX_TIME for d in draws))


class ClassTests(unittest.TestCase):
    def test_e1_holds_the_edges_the_specials_and_its_draws(self) -> None:
        e1 = P.e1()
        for value in (-8.64e15 - 1, -8.64e15, -8.64e15 + 1, 8.64e15 - 1, 8.64e15, 8.64e15 + 1, 2.0 ** 53):
            self.assertIn(value, e1)
        self.assertTrue(any(math.isnan(v) for v in e1))
        self.assertIn(-0.0, [v for v in e1 if v == 0 and math.copysign(1, v) < 0])
        # The last day of year 1972 and the first of year -1, each at its first and last millisecond.
        last_1972 = D.day_from_year_exact(1973) - 1
        self.assertIn(float(last_1972 * DAY), e1)
        self.assertIn(float(last_1972 * DAY + DAY - 1), e1)
        self.assertIn(float(D.day_from_year_exact(-1) * DAY), e1)
        self.assertEqual(6 + 2 * sum(D.day_from_year_exact(y + 1) - D.day_from_year_exact(y)
                                     for y in [-1, 0, 1, 1969, 1970, 1971, 1972, 9999, 10000]) + 9 + P.E1_DRAWS, len(e1))

    def test_e2_varies_each_component_alone_then_draws(self) -> None:
        rows = P.e2()
        self.assertEqual(7 * len(P.E2_VALUES) + P.E2_DRAWS, len(rows))
        self.assertEqual((1e19, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0), rows[len(P.E2_VALUES) * 0 + P.E2_VALUES.index(1e19)])
        self.assertEqual((1970.0, 0.0, 1.0, 0.0, 0.0, 0.0, 2.0 ** 63), rows[len(P.E2_VALUES) * 6 + P.E2_VALUES.index(2.0 ** 63)])

    def test_make_day_alone_runs_where_its_result_is_near_the_time_values_or_nan(self) -> None:
        self.assertTrue(P.make_day_applies(1970.0, 0.0, 1.0))
        self.assertTrue(P.make_day_applies(math.nan, 0.0, 1.0))
        self.assertTrue(P.make_day_applies(275760.0, 8.0, 13.0))
        self.assertFalse(P.make_day_applies(1e15, 0.0, 1.0))

    def test_e3_flags_valid_dates(self) -> None:
        dates = P.e3_dates()
        self.assertIn("2020 2 29 v", dates)
        self.assertIn("2021 2 29 -", dates)
        self.assertIn("2021 13 1 -", dates)
        self.assertIn("-1200 0 0 -", dates)
        self.assertIn("1000000400 12 31 v", dates)

    def test_e4_finds_new_york_s_transitions_of_2021(self) -> None:
        found = {t for t, _, _ in P.transitions(ZoneInfo("America/New_York"))}
        self.assertTrue(wall(2021, 3, 14, 7) in found)
        self.assertTrue(wall(2021, 11, 7, 6) in found)
        self.assertEqual([], P.transitions(ZoneInfo("UTC")))

    def test_e5_combines_every_shape_and_mutates_each(self) -> None:
        strings = set(P.e5_format())
        for s in ("1970", "+275760-12-31T23:59:59.999Z", "-000000", "1970-02-30T24:00", "1970t00:00",
                  "1970-01-01 00:00:00.000Z", "1970-01-01T00:00:00.000z", "1970-01-01T00:00:00.000+0530",
                  "1970-01-01T00:00:00.Z", "1970-01-01T00:00:00.9999999999-24:00", " 1970-01", "10000-13-32 "):
            self.assertTrue(s in strings, s)

    def test_e5_fallback_holds_the_correspondence_s_shapes(self) -> None:
        shapes = set(P.e5_fallback())
        for s in ("Jan 1 70", "Thu Jan 01 1970 00:00:00 GMT+9999", "1970 Jan 01", "Thu, 01 JAN 1970 00:00:00 GMT",
                  "Thu Jan 01 -1970 00:00:00 GMT+0000 (Coordinated Universal Time)", "Thu, 01 Jan 1970 00:00:00 UTC-2499"):
            self.assertTrue(s in shapes, s)

    def test_e5_draws_are_reproducible(self) -> None:
        self.assertEqual(P.e5_drawn()[:16], P.e5_drawn()[:16])
        self.assertEqual(P.E5_DRAWS, len(P.e5_drawn()))


class ReferenceLocalTimeTests(unittest.TestCase):
    NEW_YORK = ZoneInfo("America/New_York")
    LORD_HOWE = ZoneInfo("Australia/Lord_Howe")

    def test_local_time_reads_the_offset_at_the_instant(self) -> None:
        self.assertEqual(-5 * HOUR, D.local_time(wall(2021, 1, 1), self.NEW_YORK) - wall(2021, 1, 1))
        self.assertEqual(-4 * HOUR, D.local_time(wall(2021, 3, 14, 7), self.NEW_YORK) - wall(2021, 3, 14, 7))
        self.assertEqual(-5 * HOUR, D.local_time(wall(2021, 3, 14, 7) - 1, self.NEW_YORK) - (wall(2021, 3, 14, 7) - 1))

    def test_utc_reads_a_skipped_time_at_the_offset_before_and_a_repeated_one_at_its_earlier_instant(self) -> None:
        # Correspondence row L3: 02:30 on 2021-03-14 is skipped, 01:30 on 2021-11-07 repeated.
        self.assertEqual(wall(2021, 3, 14, 7, 30), D.utc(wall(2021, 3, 14, 2, 30), self.NEW_YORK))
        self.assertEqual(wall(2021, 11, 7, 5, 30), D.utc(wall(2021, 11, 7, 1, 30), self.NEW_YORK))
        self.assertEqual(wall(2021, 3, 14, 7, 30), D.utc(wall(2021, 3, 14, 3, 30), self.NEW_YORK))

    def test_a_thirty_minute_transition(self) -> None:
        # Lord Howe moves from +10:30 to +11:00 at 02:00 local on 2021-10-03: 02:15 is skipped.
        self.assertEqual(wall(2021, 10, 2, 15, 45), D.utc(wall(2021, 10, 3, 2, 15), self.LORD_HOWE))

    def test_past_year_9999_the_rules_repeat_every_400_years(self) -> None:
        self.assertEqual(-4 * HOUR, D.local_time(wall(10000, 7, 1), self.NEW_YORK) - wall(10000, 7, 1))
        self.assertEqual(-5 * HOUR, D.local_time(wall(100000, 1, 1), self.NEW_YORK) - wall(100000, 1, 1))

    def test_before_year_1_the_earliest_offset(self) -> None:
        self.assertEqual(-17_762_000, D.local_time(wall(-1, 1, 1), self.NEW_YORK) - wall(-1, 1, 1))
        self.assertEqual(wall(-1, 1, 1) + 17_762_000, D.utc(wall(-1, 1, 1), self.NEW_YORK))

    def test_utc_over_recorded_offsets_agrees_with_zoneinfo(self) -> None:
        def offset_at(instant: int) -> int:
            return D.zone_offset_ms(self.NEW_YORK, instant)

        for local in (wall(2021, 3, 14, 2, 30), wall(2021, 11, 7, 1, 30), wall(2021, 6, 1, 12), wall(1883, 11, 18, 12, 2)):
            self.assertEqual(D.utc(local, self.NEW_YORK), D.utc_over(local, offset_at), local)

    def test_time_zone_string(self) -> None:
        self.assertEqual("+0000", D.time_zone_string(0, None))
        self.assertEqual("-0456 (LMT)", D.time_zone_string(-17_762_000, "LMT"))
        self.assertEqual("+2359 (x)", D.time_zone_string(86_340_000, "x"))

    def test_the_strict_reading_of_a_day_past_its_month(self) -> None:
        self.assertIsNotNone(D.parse_format("2021-02-30"))
        self.assertIsNone(D.parse_format("2021-02-30", strict_days=True))
        self.assertIsNotNone(D.parse_format("2020-02-29T00:00Z", strict_days=True))


if __name__ == "__main__":
    unittest.main()
