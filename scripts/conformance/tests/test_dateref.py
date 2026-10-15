from __future__ import annotations

import datetime
import math
import random
import sys
import tempfile
import unittest
from pathlib import Path

CONFORMANCE_DIR = Path(__file__).resolve().parents[1]
REPOSITORY = CONFORMANCE_DIR.parents[1]
if str(CONFORMANCE_DIR) not in sys.path:
    sys.path.insert(0, str(CONFORMANCE_DIR))

import dateref as D
import generate_date_vectors as generator

EPOCH = datetime.datetime(1970, 1, 1)


def utc(y: int, m: int, d: int, h: int = 0, mi: int = 0, s: int = 0, ms: int = 0) -> float:
    """The time value of a UTC date with a month counted from 1, through MakeDay directly (no MakeFullYear)."""
    return generator.utc(y, m - 1, d, h, mi, s, ms)


def is_negative_zero(x: float) -> bool:
    return x == 0 and math.copysign(1.0, x) < 0


class CalendarAgainstThePlatformTests(unittest.TestCase):
    """Python's datetime is a proleptic Gregorian calendar over the years 1 to 9999, written independently of
    the specification's formulas; the two must agree wherever both are defined."""

    def test_every_first_of_the_month_from_1_to_9999(self) -> None:
        for year in range(1, 10000):
            for month in (1, 2, 3, 12):
                expected = (datetime.datetime(year, month, 1) - EPOCH).days
                self.assertEqual(expected, D.epoch_days(year, month, 1), (year, month))
                self.assertEqual(expected * D.MS_PER_DAY, utc(year, month, 1), (year, month))

    def test_fields_of_drawn_time_values(self) -> None:
        draws = random.Random(0x5EED)
        low = (datetime.datetime(1, 1, 1) - EPOCH) // datetime.timedelta(milliseconds=1)
        high = (datetime.datetime(9999, 12, 31, 23, 59, 59, 999000) - EPOCH) // datetime.timedelta(milliseconds=1)
        for _ in range(20000):
            tv = draws.randint(low, high)
            when = EPOCH + datetime.timedelta(milliseconds=tv)
            t = float(tv)
            self.assertEqual(when.year, D.year_from_time(t), tv)
            self.assertEqual(when.month - 1, D.month_from_time(t), tv)
            self.assertEqual(when.day, D.date_from_time(t), tv)
            self.assertEqual((when.weekday() + 1) % 7, D.week_day(t), tv)
            self.assertEqual(when.hour, D.hour_from_time(t), tv)
            self.assertEqual(when.minute, D.min_from_time(t), tv)
            self.assertEqual(when.second, D.sec_from_time(t), tv)
            self.assertEqual(when.microsecond // 1000, D.ms_from_time(t), tv)
            self.assertEqual(when.timetuple().tm_yday - 1, D.day_within_year(t), tv)

    def test_iso_weeks_and_days_against_isocalendar(self) -> None:
        draws = random.Random(0xC1C1)
        days = [datetime.date(1990, 1, 1) + datetime.timedelta(days=n) for n in range(366 * 41)]
        days += [datetime.date.fromordinal(draws.randint(8, datetime.date(9999, 12, 24).toordinal())) for _ in range(20000)]
        for when in days:
            week_year, week, weekday = when.isocalendar()
            self.assertEqual((week, week_year), D.iso_week_of_year(when.year, when.month, when.day), when)
            self.assertEqual(weekday, D.iso_day_of_week(when.year, when.month, when.day), when)
            self.assertEqual(when.timetuple().tm_yday, D.iso_day_of_year(when.year, when.month, when.day), when)
            self.assertEqual((when.year, when.month, when.day),
                             D.from_epoch_days((when - datetime.date(1970, 1, 1)).days), when)


class FourHundredYearPeriodTests(unittest.TestCase):
    """The Gregorian calendar repeats every 400 years, which are 146,097 days; the range below the year 1,
    which no platform calendar here covers, is checked by that period against the range above it."""

    def test_epoch_days_repeat(self) -> None:
        for year in range(-271821, 275761, 397):
            for month, dday in ((1, 1), (2, 28), (2, 29) if D.iso_leap(year) else (3, 1), (12, 31)):
                self.assertEqual(D.epoch_days(year, month, dday) + 146097, D.epoch_days(year + 400, month, dday))
                self.assertEqual((year, month, dday), D.from_epoch_days(D.epoch_days(year, month, dday)))

    def test_fields_repeat(self) -> None:
        period = 146097 * D.MS_PER_DAY
        for t in generator.random_time_values(0x400, 5000, -D.MAX_TIME, D.MAX_TIME - period):
            shifted = t + period
            self.assertEqual(D.year_from_time(t) + 400, D.year_from_time(shifted))
            for read in (D.month_from_time, D.date_from_time, D.week_day, D.hour_from_time, D.min_from_time,
                         D.sec_from_time, D.ms_from_time, D.day_within_year):
                self.assertEqual(read(t), read(shifted), (read.__name__, t))

    def test_iso_weeks_repeat(self) -> None:
        for year in range(-271800, 275700, 1009):
            for month, dday in ((1, 1), (1, 4), (12, 28), (12, 31)):
                week, week_year = D.iso_week_of_year(year, month, dday)
                self.assertEqual((week, week_year + 400), D.iso_week_of_year(year + 400, month, dday))


class ArithmeticTests(unittest.TestCase):
    def test_time_clip(self) -> None:
        self.assertEqual(8.64e15, D.time_clip(8.64e15))
        self.assertEqual(-8.64e15, D.time_clip(-8.64e15))
        self.assertTrue(math.isnan(D.time_clip(8.64e15 + 1)))
        self.assertTrue(math.isnan(D.time_clip(math.inf)))
        clipped = D.time_clip(-0.0)
        self.assertEqual(0.0, clipped)
        self.assertFalse(is_negative_zero(clipped))
        self.assertFalse(is_negative_zero(D.time_clip(-0.5)))
        self.assertEqual(1.0, D.time_clip(1.9))

    def test_make_time_truncates_and_refuses_non_finite_components(self) -> None:
        self.assertEqual(D.MS_PER_HOUR, D.make_time(1.9, 0.0, 0.0, 0.0))
        self.assertEqual(-D.MS_PER_HOUR, D.make_time(-1.9, 0.0, 0.0, 0.0))
        self.assertEqual(-1.0, D.make_time(0.0, 0.0, 0.0, -1.5))
        self.assertTrue(math.isnan(D.make_time(0.0, math.inf, 0.0, 0.0)))
        self.assertTrue(math.isnan(D.make_time(0.0, 0.0, D.NAN, 0.0)))

    def test_make_day_and_the_range_ends(self) -> None:
        self.assertEqual(0.0, D.make_day(1970.0, 0.0, 1.0))
        self.assertEqual(31.0 + 28.0, D.make_day(1970.0, 2.0, 1.0))
        self.assertEqual(D.make_day(1971.0, 0.0, 1.0), D.make_day(1970.0, 12.0, 1.0))
        self.assertEqual(D.make_day(1969.0, 11.0, 1.0), D.make_day(1970.0, -1.0, 1.0))
        self.assertEqual(D.make_day(2000.0, 1.0, 29.0) + 1.0, D.make_day(2000.0, 2.0, 1.0))
        self.assertEqual(D.make_day(1900.0, 1.0, 28.0) + 1.0, D.make_day(1900.0, 2.0, 1.0))
        self.assertEqual(8.64e15, utc(275760, 9, 13))
        self.assertEqual(-8.64e15, utc(-271821, 4, 20))
        self.assertTrue(math.isnan(utc(275760, 9, 13, 0, 0, 0, 1)))
        self.assertTrue(math.isnan(utc(-271821, 4, 19, 23, 59, 59, 999)))
        self.assertTrue(math.isnan(D.make_day(1.7976931348623157e308, 12.0, 1.0)))
        # A month far beyond the year's range, which no double day count reaches; checked once when written.
        self.assertEqual(-62156764800000.0, D.date_utc(-833333333333333333.0, 1e19, 1.0))

    def test_make_full_year(self) -> None:
        self.assertEqual(1900.0, D.make_full_year(0.0))
        self.assertEqual(D.date_utc(1900.0), utc(1900, 1, 1))
        self.assertEqual(D.date_utc(99.0, 11.0), utc(1999, 12, 1))
        self.assertEqual(1999.0, D.make_full_year(99.9))
        self.assertEqual(100.0, D.make_full_year(100.0))
        self.assertEqual(1900.0, D.make_full_year(-0.5))
        self.assertEqual(-1.0, D.make_full_year(-1.0))
        self.assertTrue(math.isnan(D.make_full_year(D.NAN)))
        self.assertEqual(math.inf, D.make_full_year(math.inf))

    def test_setters(self) -> None:
        t = utc(2020, 2, 29, 12, 30, 15, 250)
        self.assertEqual(utc(2021, 3, 1, 12, 30, 15, 250), D.set_utc_full_year(t, 2021.0))
        self.assertEqual(utc(2020, 3, 2, 12, 30, 15, 250), D.set_utc_date(t, 31.0))
        self.assertEqual(utc(2020, 5, 1, 12, 30, 15, 250), D.set_utc_month(t, 3.0, 31.0))
        self.assertEqual(utc(2020, 2, 29, 1, 30, 15, 250), D.set_utc_hours(t, 1.5))
        self.assertEqual(utc(2020, 2, 29, 12, 30, 15, 999), D.set_utc_milliseconds(t, 999.9))
        self.assertEqual(utc(2021, 1, 1), D.set_utc_full_year(D.NAN, 2021.0))
        self.assertTrue(math.isnan(D.set_utc_month(D.NAN, 1.0)))


class FormatTests(unittest.TestCase):
    def test_every_omission(self) -> None:
        expected = {
            "2020": utc(2020, 1, 1), "2020-02": utc(2020, 2, 1), "2020-02-03": utc(2020, 2, 3),
            "2020-02-03T04:05Z": utc(2020, 2, 3, 4, 5), "2020-02-03T04:05:06Z": utc(2020, 2, 3, 4, 5, 6),
            "2020-02-03T04:05:06.789Z": utc(2020, 2, 3, 4, 5, 6, 789),
            "2020-02-03T04:05+01:30": utc(2020, 2, 3, 2, 35), "2020-02-03T04:05-00:30": utc(2020, 2, 3, 4, 35),
            "+002020-02-03": utc(2020, 2, 3), "-000001-01-01T00:00Z": utc(-1, 1, 1), "+000000": utc(0, 1, 1),
            "2020-02-03T24:00Z": utc(2020, 2, 4), "2020-02-03T24:00:00.000Z": utc(2020, 2, 4),
            "+275760-09-13T00:00:00.000Z": 8.64e15, "-271821-04-20T00:00:00.000Z": -8.64e15,
        }
        for text, value in expected.items():
            self.assertEqual(value, D.date_parse_utc(text), text)

    def test_date_time_forms_without_an_offset_are_local(self) -> None:
        for text in ("2020-02-03T04:05", "2020-02-03T04:05:06.789", "2020T04:05", "+002020-02T00:00"):
            self.assertIsNone(D.date_parse_utc(text), text)
            self.assertEqual("local", D.parse_format(text)[0], text)

    def test_what_is_not_an_instance(self) -> None:
        for text in ("-000000", "-000000-01-01T00:00Z", "2020-02-03T04:05:06.7Z", "2020-02-03T04:05:06.7890Z",
                     "2020-02-03T24:00:01Z", "2020-02-03T24:01Z", "2020-13-01", "2020-00-01", "2020-01-32",
                     "2020-01-00", "2020-02-03T04", "2020-02-03T04:60Z", "2020-02-03T04:05:60Z", "2020-02-03 04:05Z",
                     "2020-02-03T04:05+0100", "2020-02-03T04:05+24:00", "20-02-03", "+20200-02-03", "2020-2-3",
                     "2020-02-03t04:05Z", "2020-02-03T04:05z", "", "+275760-09-13T00:00:00.001Z",
                     "-271821-04-19T23:59:59.999Z"):
            self.assertTrue(math.isnan(D.date_parse_utc(text)), text)


class RenderingTests(unittest.TestCase):
    def test_strings(self) -> None:
        self.assertEqual("Thu, 01 Jan 1970 00:00:00 GMT", D.to_utc_string(0.0))
        self.assertEqual("1970-01-01T00:00:00.000Z", D.to_iso_string(0.0))
        self.assertEqual("Sat, 13 Sep 275760 00:00:00 GMT", D.to_utc_string(8.64e15))
        self.assertEqual("+275760-09-13T00:00:00.000Z", D.to_iso_string(8.64e15))
        self.assertEqual("Tue, 20 Apr -271821 00:00:00 GMT", D.to_utc_string(-8.64e15))
        self.assertEqual("-271821-04-20T00:00:00.000Z", D.to_iso_string(-8.64e15))
        # 0000-01-01 is a Saturday, and year 0 is a leap year, so -0001-01-01 is a Friday.
        self.assertEqual("Sat, 01 Jan 0000 00:00:00 GMT", D.to_utc_string(utc(0, 1, 1)))
        self.assertEqual("Fri, 01 Jan -0001 00:00:00 GMT", D.to_utc_string(utc(-1, 1, 1)))
        self.assertEqual("-000001-01-01T00:00:00.000Z", D.to_iso_string(utc(-1, 1, 1)))
        self.assertEqual("0000-01-01T00:00:00.000Z", D.to_iso_string(utc(0, 1, 1)))
        self.assertEqual("+010000-01-01T00:00:00.000Z", D.to_iso_string(utc(10000, 1, 1)))
        self.assertEqual("Fri Jan 01 -0001", D.date_string(utc(-1, 1, 1)))
        self.assertEqual("23:59:59 GMT", D.time_string(utc(1969, 12, 31, 23, 59, 59, 999)))
        self.assertEqual("Invalid Date", D.to_utc_string(D.NAN))
        with self.assertRaises(D.RangeError):
            D.to_iso_string(D.NAN)

    def test_iso_strings_read_back(self) -> None:
        for t in generator.random_time_values(0x150, 5000):
            self.assertEqual(t, D.date_parse_utc(D.to_iso_string(t)), t)

    def test_the_platform_agrees_within_its_years(self) -> None:
        weekdays = ("Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun")
        months = ("Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec")
        for t in generator.random_time_values(0x151, 5000, -62135596800000.0, 253402300799999.0):
            when = EPOCH + datetime.timedelta(milliseconds=t)
            clock = f"{when.hour:02d}:{when.minute:02d}:{when.second:02d}"
            self.assertEqual(f"{weekdays[when.weekday()]}, {when.day:02d} {months[when.month - 1]} {when.year:04d} "
                             f"{clock} GMT", D.to_utc_string(t))
            self.assertEqual(f"{when.year:04d}-{when.month:02d}-{when.day:02d}T{clock}.{when.microsecond // 1000:03d}Z",
                             D.to_iso_string(t))


class CivilTests(unittest.TestCase):
    def test_week_edges(self) -> None:
        self.assertEqual((53, 2020), D.iso_week_of_year(2021, 1, 3))
        self.assertEqual((1, 2020), D.iso_week_of_year(2019, 12, 30))
        self.assertEqual((1, 2026), D.iso_week_of_year(2026, 1, 1))
        self.assertEqual((53, -2), D.iso_week_of_year(-2, 12, 28))

    def test_month_day_regulation(self) -> None:
        self.assertEqual("02-28", D.plain_month_day_from(2021, 2, 29, "constrain"))
        self.assertIsNone(D.plain_month_day_from(2021, 2, 29, "reject"))
        self.assertEqual("02-29", D.plain_month_day_from(2020, 2, 29, "reject"))
        self.assertEqual("12-31", D.plain_month_day_from(2021, 13, 32, "constrain"))
        self.assertIsNone(D.plain_month_day_from(1000000100, 2, 29, "reject"))

    def test_limits(self) -> None:
        self.assertTrue(D.iso_within_limits(*D.ISO_MIN))
        self.assertTrue(D.iso_within_limits(*D.ISO_MAX))
        self.assertFalse(D.iso_within_limits(-271821, 4, 18))
        self.assertFalse(D.iso_within_limits(275760, 9, 14))
        self.assertEqual("+275760-09-13", D.iso_date_string(*D.ISO_MAX))
        self.assertEqual("-271821-04-19", D.iso_date_string(*D.ISO_MIN))


class GeneratorTests(unittest.TestCase):
    def test_the_committed_corpus_is_what_the_generator_writes(self) -> None:
        committed = REPOSITORY / "conformance" / "vectors" / "date"
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
