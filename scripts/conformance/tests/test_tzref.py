"""Tests of tzref.py, the time-zone area's reference computation (corpus revision 21, milestone SJL-8.4).

The pin's refusals, the POSIX TZ strings and the offset grammars need no compiler. The tests that compile the pinned
release need the host's `zic` and are skipped where it is absent. They check tzref's own TZif reader against Python's
`zoneinfo` over every compiled zone, the policy of decision SJL-D-20, and the answers bundle sjl-8-001's
correspondence from code records for Broiler.VM (Broiler.VM's docs/evidence/sjl-8-001/correspondence.md), row by row:
those answers are a check of the reference here, never a vector's provenance.
"""

from __future__ import annotations

import io
import shutil
import sys
import tempfile
import unittest
import zoneinfo
from datetime import datetime
from pathlib import Path

CONFORMANCE_DIR = Path(__file__).resolve().parents[1]
if str(CONFORMANCE_DIR) not in sys.path:
    sys.path.insert(0, str(CONFORMANCE_DIR))

import tzref as T  # noqa: E402

HAVE_ZIC = shutil.which("zic") is not None
NEEDS_ZIC = unittest.skipUnless(HAVE_ZIC, "no zic on this host")
S = T.NS_PER_SECOND


def E(text: str) -> int:
    return T.epoch_seconds(text) * S


_DB = None


def database() -> T.Database:
    global _DB
    if _DB is None:
        _DB = T.Database()
    return _DB


def zone(name: str) -> T.TimeZone:
    return T.time_zone_from_identifier(database(), name)


class CalendarTests(unittest.TestCase):
    def test_days_round_trip_and_agree_with_the_platform(self) -> None:
        for days in range(-800_000, 800_000, 997):
            self.assertEqual(days, T.days_from_civil(*T.civil_from_days(days)))
        for year in (1, 1600, 1969, 1970, 2000, 2038, 9999):
            for month in (1, 2, 3, 12):
                expected = (datetime(year, month, 1) - datetime(1970, 1, 1)).days
                self.assertEqual(expected, T.days_from_civil(year, month, 1))
        self.assertEqual((-271821, 4, 20), T.civil_from_days(-100_000_000))
        self.assertEqual((275760, 9, 13), T.civil_from_days(100_000_000))

    def test_weekday_counts_sunday_as_zero(self) -> None:
        self.assertEqual(4, T.weekday(0))  # 1970-01-01, a Thursday
        self.assertEqual(0, T.weekday(T.days_from_civil(2026, 3, 8)))


class PosixTzTests(unittest.TestCase):
    def test_the_us_rules(self) -> None:
        tz = T.PosixTz("EST5EDT,M3.2.0,M11.1.0")
        self.assertEqual([E("2040-03-11T07:00") // S, E("2040-11-04T06:00") // S],
                         sorted(at for at, _, _ in tz.changes(2040)))
        self.assertEqual(-14400, tz.offset_at(E("2040-07-01T12:00") // S))
        self.assertEqual(-18000, tz.offset_at(E("2040-01-01T12:00") // S))
        self.assertEqual(-18000, tz.offset_at(E("2040-03-11T06:59:59") // S))
        self.assertEqual(-14400, tz.offset_at(E("2040-03-11T07:00") // S))

    def test_negative_daylight_time_and_southern_rules(self) -> None:
        dublin = T.PosixTz("IST-1GMT0,M10.5.0,M3.5.0/1")
        self.assertEqual(3600, dublin.offset_at(E("2040-07-01T12:00") // S))
        self.assertEqual(0, dublin.offset_at(E("2040-01-01T12:00") // S))
        sydney = T.PosixTz("AEST-10AEDT,M10.1.0,M4.1.0/3")
        self.assertEqual(36000, sydney.offset_at(E("2040-07-01T12:00") // S))
        self.assertEqual(39600, sydney.offset_at(E("2040-01-01T12:00") // S))

    def test_extended_times_and_quoted_names(self) -> None:
        nuuk = T.PosixTz("<-02>2<-01>,M3.5.0/-1,M10.5.0/0")
        self.assertEqual(-7200, nuuk.standard)
        self.assertEqual(-3600, nuuk.daylight)
        self.assertEqual(-3600, nuuk.start[1])
        santiago = T.PosixTz("<-04>4<-03>,M9.1.6/24,M4.1.6/24")
        # The first Saturday of September 2040 is the 1st: 24:00 there is 2040-09-02T04:00Z.
        self.assertIn(E("2040-09-02T04:00") // S, [at for at, _, _ in santiago.changes(2040)])
        lord_howe = T.PosixTz("<+1030>-10:30<+11>-11,M10.1.0,M4.1.0")
        self.assertEqual(37800, lord_howe.standard)
        self.assertEqual(39600, lord_howe.daylight)

    def test_julian_and_zero_based_days(self) -> None:
        julian = T.PosixTz("AAA3BBB,J60/0,J300/0")
        # J60 is March 1 in every year, February 29 never counted.
        self.assertEqual(T.days_from_civil(2040, 3, 1) * 86400 + 10800, julian.changes(2040)[0][0])
        zero_based = T.PosixTz("AAA3BBB,59/0,300/0")
        self.assertEqual(T.days_from_civil(2040, 2, 29) * 86400 + 10800, zero_based.changes(2040)[0][0])

    def test_a_string_without_rules_has_one_offset(self) -> None:
        self.assertEqual(19800, T.PosixTz("IST-5:30").offset_at(0))
        self.assertEqual(-10800, T.PosixTz("<-03>3").offset_at(10 ** 12))
        with self.assertRaises(ValueError):
            T.PosixTz("EST5EDT")  # daylight time with no rule


class OffsetGrammarTests(unittest.TestCase):
    def test_temporal_offset_identifiers(self) -> None:
        for text, minutes in (("+05:30", 330), ("-05:30", -330), ("+0530", 330), ("+05", 300), ("-00:00", 0),
                              ("-00", 0), ("+23:59", 1439), ("-23:59", -1439)):
            self.assertEqual(minutes, T.parse_offset_identifier(text), text)
        for text in ("+24:00", "+05:60", "+05:30:15", "+05:30:00", "+053015", "+05:30:15.5", "−05:00", "+05:3",
                     "05:30", "+5:30", "+0530:15", "+05:30 ", "+٠٥:٣٠", "+０５", "+0٥:30"):
            self.assertIsNone(T.parse_offset_identifier(text), text)

    def test_ecma_262_offset_strings(self) -> None:
        for text in ("+05:30:15", "+053015", "+05:30:15.5", "+05:30:15,123456789", "+05"):
            self.assertTrue(T.is_time_zone_offset_string(text), text)
        for text in ("+05:3015", "+0530:15", "+05:30:15.", "+05:30:15.1234567890", "−05:00", "+٠٥"):
            self.assertFalse(T.is_time_zone_offset_string(text), text)

    def test_format_offset_identifier(self) -> None:
        self.assertEqual("+00:00", T.format_offset_identifier(0))
        self.assertEqual("-05:30", T.format_offset_identifier(-330))
        self.assertEqual("+14:00", T.format_offset_identifier(840))


class PinTests(unittest.TestCase):
    """A file that is not the pin's is refused before a byte of it is used."""

    def copy_home(self, scratch: Path) -> Path:
        home = scratch / "time"
        shutil.copytree(T.DEFAULT_HOME / "tzdb", home / "tzdb")
        shutil.copytree(T.DEFAULT_HOME / "cldr", home / "cldr")
        return home

    def test_the_pinned_files_are_accepted(self) -> None:
        release, digest, members = T.verified_tzdb(T.DEFAULT_HOME)
        self.assertEqual("2026e", release)
        self.assertEqual("b26882805f26aac59d5b222978e6580484b834ccdc98be89df2f05a6dc53a652", digest)
        self.assertEqual(set(T.SOURCE_FILES) | {"zone.tab", "version"}, set(members))
        self.assertIn("Europe/Bratislava", T.cldr_primaries(T.verified_cldr(T.DEFAULT_HOME)))

    def test_a_changed_archive_byte_is_refused(self) -> None:
        with tempfile.TemporaryDirectory() as scratch:
            home = self.copy_home(Path(scratch))
            archive = home / "tzdb" / "tzdata2026e.tar.gz"
            data = bytearray(archive.read_bytes())
            data[1000] ^= 1
            archive.write_bytes(bytes(data))
            with self.assertRaises(T.PinError):
                T.verified_tzdb(home)

    def test_a_changed_cldr_byte_is_refused(self) -> None:
        with tempfile.TemporaryDirectory() as scratch:
            home = self.copy_home(Path(scratch))
            path = home / "cldr" / T.CLDR_FILE
            path.write_bytes(path.read_bytes().replace(b"Europe/Andorra", b"Europe/Andorre"))
            with self.assertRaises(T.PinError):
                T.verified_cldr(home)

    def test_the_reference_reads_nothing_of_the_generated_tables_or_the_csharp_side(self) -> None:
        source = (CONFORMANCE_DIR / "tzref.py").read_text(encoding="utf-8")
        for name in ("time-zones.bin", "time-zone-ids.txt", "time-zone-regions.txt", "tables.manifest", "\"generated\"",
                     "'generated'", "Broiler.ECMAScript.Generation", "Broiler.ECMAScript.Algorithms\""):
            self.assertNotIn(name, source)


@NEEDS_ZIC
class ReaderTests(unittest.TestCase):
    def test_every_zone_agrees_with_zoneinfo(self) -> None:
        """tzref's reader and footer evaluator against Python's zoneinfo over each compiled file: either side of every
        listed transition, every quarter from 1800 to 2100, a few far years, and the first sixty footer changes."""
        db = database()
        checked = 0
        for name in db.identifiers:
            own = db.own_zone(name)
            reference = zoneinfo.ZoneInfo.from_file(io.BytesIO(db.tzif(name)), key=name)
            points = {t + d for t in own.times for d in (-1, 0, 1)}
            for year in list(range(1800, 2101)) + [2200, 2500, 3000, 5000, 9000, 9999]:
                for month in (1, 4, 7, 10):
                    points.add(T.days_from_civil(year, month, 1) * 86400 + 43200)
            at = T.epoch_seconds("2037-01-01T00:00")
            for _ in range(60):
                at = own.next_change(at, T.epoch_seconds("9999-01-01T00:00"))
                if at is None:
                    break
                points.update((at - 1, at))
            for t in sorted(p for p in points if p >= T.epoch_seconds("1800-01-01T00:00")):
                expected = int(datetime.fromtimestamp(t, tz=reference).utcoffset().total_seconds())
                self.assertEqual(expected, own.offset_at(t), f"{name} at {t}")
                checked += 1
        self.assertGreater(checked, 500_000)


@NEEDS_ZIC
class PolicyTests(unittest.TestCase):
    """Decision SJL-D-20, over the pinned release."""

    def test_the_identifiers_and_the_primaries(self) -> None:
        db = database()
        self.assertEqual("2026e", db.release)
        self.assertEqual(597, len(db.identifiers))
        self.assertEqual(445, len(db.primaries))
        self.assertNotIn("Factory", db.identifiers)
        self.assertEqual(sorted(db.primaries), db.primaries)
        for name, primary in (("UTC", "UTC"), ("Etc/UTC", "UTC"), ("Etc/GMT", "UTC"), ("GMT", "UTC"),
                              ("Etc/GMT0", "UTC"), ("Asia/Calcutta", "Asia/Kolkata"), ("Europe/Kiev", "Europe/Kyiv"),
                              ("US/Eastern", "America/New_York"), ("EST5EDT", "America/New_York"),
                              ("CET", "Europe/Brussels"), ("Europe/Bratislava", "Europe/Bratislava"),
                              ("Arctic/Longyearbyen", "Arctic/Longyearbyen"), ("Atlantic/Jan_Mayen", "Arctic/Longyearbyen"),
                              ("Etc/GMT+5", "Etc/GMT+5"), ("EST", "America/Panama")):
            self.assertEqual(primary, db.primary(name), name)

    def test_twelve_names_keep_a_cldr_primary_that_tzdb_links_away(self) -> None:
        db = database()
        twelve = {"America/Kralendijk", "America/Lower_Princes", "America/Marigot", "America/St_Barthelemy",
                  "Arctic/Longyearbyen", "Atlantic/Jan_Mayen", "Europe/Bratislava", "Europe/Busingen", "Europe/Mariehamn",
                  "Europe/Podgorica", "Europe/San_Marino", "Europe/Vatican"}
        for name in twelve:
            self.assertIn(name, db.links, name)
        self.assertFalse(T.time_zone_equals(db, "Europe/Bratislava", "Europe/Prague"))
        self.assertTrue(T.time_zone_equals(db, "Atlantic/Jan_Mayen", "Arctic/Longyearbyen"))

    def test_lookup_is_by_ascii_case_only(self) -> None:
        db = database()
        self.assertEqual("Europe/Berlin", db.find("eUrOpE/bErLiN"))
        self.assertEqual("America/Port-au-Prince", db.find("AMERICA/PORT-AU-PRINCE"))
        for text in ("America/Loſ_Angeles", "Europe/Kyiv", "Europe/Berlın", "Europe/İstanbul",
                     "Factory", "Europe/Berlin ", ""):
            self.assertIsNone(db.find(text), text)

    def test_the_four_posix_names_read_their_cldr_primaries(self) -> None:
        """As check-zic.py states: each differs from zic's compilation of its own Zone only before 1966-10-31."""
        db = database()
        for name, primary in (("CST6CDT", "America/Chicago"), ("EST5EDT", "America/New_York"),
                              ("MST7MDT", "America/Denver"), ("PST8PDT", "America/Los_Angeles")):
            read, own = db.zone(name), db.own_zone(name)
            self.assertEqual(db.zone(primary).name, read.name)
            differing = [t for t in set(read.times) | set(own.times) for u in (t - 1, t)
                         if read.offset_at(u) != own.offset_at(u)]
            self.assertTrue(differing, name)
            self.assertLess(max(differing), T.epoch_seconds("1966-10-31T00:00"), name)

    def test_regions_are_zone_tab_s(self) -> None:
        db = database()
        self.assertEqual(["Europe/Berlin", "Europe/Busingen"], db.region_zones("DE"))
        self.assertEqual(29, len(db.region_zones("US")))
        self.assertEqual([], db.region_zones("ZZ"))


@NEEDS_ZIC
class CorrespondenceTests(unittest.TestCase):
    """Broiler.VM's answers in bundle sjl-8-001's correspondence, which the reference must give."""

    def offset_hours(self, name: str, at: str) -> float:
        return T.offset_ns(zone(name), E(at)) / 3600 / S

    def test_z1_the_release(self) -> None:
        for name, at, hours in (("America/Vancouver", "2027-01-15T12:00", -7), ("America/Edmonton", "2027-01-15T12:00", -6),
                                ("America/Winnipeg", "2027-01-15T12:00", -5), ("America/Inuvik", "2027-01-15T12:00", -6),
                                ("Africa/Casablanca", "2027-01-15T12:00", 0), ("Europe/Chisinau", "2026-03-29T00:30", 2),
                                ("Europe/Chisinau", "2026-10-25T00:30", 3), ("Europe/Dublin", "1925-09-25T12:00", 0),
                                ("America/Bogota", "1992-05-02T12:00", -4)):
            self.assertEqual(hours, self.offset_hours(name, at), name)
        self.assertIsNone(T.next_transition(zone("America/Vancouver"), E("2026-07-01T12:00")))
        self.assertIsNone(T.next_transition(zone("Africa/Casablanca"), E("2026-10-01T12:00")))

    def test_z3_backzone_is_not_read(self) -> None:
        self.assertEqual(1050, T.offset_ns(zone("Europe/Amsterdam"), E("1850-01-01T00:00")) // S)
        self.assertEqual(-968, T.offset_ns(zone("Atlantic/Reykjavik"), E("1900-01-01T00:00")) // S)

    def test_z4_and_a10_no_2037_cliff(self) -> None:
        for name, at, hours in (("America/New_York", "2040-07-01T12:00", -4), ("Europe/Berlin", "2040-07-01T12:00", 2),
                                ("America/New_York", "2400-07-01T12:00", -4), ("America/Santiago", "2040-07-01T12:00", -4),
                                ("Australia/Sydney", "2040-07-01T12:00", 10), ("America/New_York", "+275760-09-13T00:00", -4)):
            self.assertEqual(hours, self.offset_hours(name, at), name)
        new_york = zone("America/New_York")
        self.assertEqual(E("2038-03-14T07:00"), T.next_transition(new_york, E("2037-12-01T12:00")))
        self.assertEqual(E("2040-03-11T07:00"), T.previous_transition(new_york, E("2040-07-01T12:00")))
        self.assertEqual(E("2099-11-01T06:00"), T.previous_transition(new_york, E("2100-01-01T00:00")))
        self.assertEqual(E("+275760-03-09T07:00"), T.previous_transition(new_york, E("+275760-09-01T00:00")))
        self.assertIsNone(T.next_transition(new_york, E("+275760-09-13T00:00")))
        self.assertEqual(E("2038-04-04T03:00"), T.next_transition(zone("America/Santiago"), E("2037-12-01T12:00")))
        self.assertEqual(E("2040-10-28T01:00"), T.next_transition(zone("Europe/Berlin"), E("2040-07-01T12:00")))

    def test_a1_offsets(self) -> None:
        self.assertEqual(-2670000000000, T.offset_ns(zone("Africa/Monrovia"), E("1970-01-01T00:00")))
        self.assertEqual(-17762, T.offset_ns(zone("America/New_York"), E("1883-11-18T12:00")) // S)
        self.assertEqual(-17762, T.offset_ns(zone("America/New_York"), E("-271821-04-20T00:00")) // S)
        self.assertEqual(39600, T.offset_ns(zone("Australia/Lord_Howe"), E("2026-01-15T12:00")) // S)
        self.assertEqual(37800, T.offset_ns(zone("Australia/Lord_Howe"), E("2026-07-15T12:00")) // S)

    def test_a3_a4_and_a5_disambiguation(self) -> None:
        def answers(name, wall):
            out = []
            for mode in T.MODES:
                try:
                    out.append(T.epoch_ns_for(zone(name), *wall, mode=mode))
                except T.RangeError:
                    out.append(None)
            return out
        self.assertEqual([E("2026-03-08T07:30"), E("2026-03-08T06:30"), E("2026-03-08T07:30"), None],
                         answers("America/New_York", (2026, 3, 8, 2, 30)))
        self.assertEqual([E("2026-11-01T05:30"), E("2026-11-01T05:30"), E("2026-11-01T06:30"), None],
                         answers("America/New_York", (2026, 11, 1, 1, 30)))
        self.assertEqual([E("2040-03-11T07:30"), E("2040-03-11T06:30"), E("2040-03-11T07:30"), None],
                         answers("America/New_York", (2040, 3, 11, 2, 30)))
        self.assertEqual([E("1972-01-07T01:04:30"), E("1972-01-07T00:20"), E("1972-01-07T01:04:30"), None],
                         answers("Africa/Monrovia", (1972, 1, 7, 0, 20)))
        # Juneau 1867: +15:02:19 and then -08:57:41, one day repeated.
        self.assertEqual([E("1867-10-18T20:57:41"), E("1867-10-18T20:57:41"), E("1867-10-19T20:57:41"), None],
                         answers("America/Juneau", (1867, 10, 19, 12, 0)))
        self.assertEqual([], T.possible_epoch_ns(zone("Pacific/Apia"), 2011, 12, 30, 12))

    def test_a6_and_a7_start_of_day(self) -> None:
        for name, date, start in (("America/Santiago", (2026, 9, 6), "2026-09-06T04:00"),
                                  ("America/Havana", (2026, 3, 8), "2026-03-08T05:00"),
                                  ("Asia/Beirut", (1920, 3, 28), "1920-03-27T22:00"),
                                  ("America/Toronto", (1919, 3, 31), "1919-03-31T04:30"),
                                  ("Pacific/Apia", (2011, 12, 30), "2011-12-30T10:00"),
                                  ("America/Santiago", (2040, 9, 2), "2040-09-02T04:00"),
                                  ("America/Havana", (2040, 3, 11), "2040-03-11T05:00"),
                                  ("Asia/Beirut", (2040, 3, 25), "2040-03-24T22:00")):
            self.assertEqual(E(start), T.start_of_day(zone(name), *date), name)
        self.assertEqual(23.5, T.hours_in_day(zone("America/Toronto"), 1919, 3, 31))
        self.assertEqual(23.0, T.hours_in_day(zone("America/New_York"), 2040, 3, 11))
        self.assertEqual(24.0, T.hours_in_day(zone("Pacific/Apia"), 2011, 12, 29))

    def test_a9_transitions(self) -> None:
        new_york = zone("America/New_York")
        self.assertEqual(E("2026-11-01T06:00"), T.next_transition(new_york, E("2026-07-01T12:00")))
        self.assertEqual(E("2025-11-02T06:00"), T.previous_transition(new_york, E("2026-03-08T07:00")))
        self.assertEqual(E("2026-03-08T07:00"), T.previous_transition(new_york, E("2026-03-08T07:00") + S // 2))
        self.assertEqual(E("1883-11-18T17:00"), T.next_transition(new_york, E("1800-01-01T00:00")))
        self.assertIsNone(T.previous_transition(new_york, E("1800-01-01T00:00")))
        # London's change of the daylight flag alone, 1968 to 1971, is no transition.
        self.assertEqual(E("1971-10-31T02:00"), T.next_transition(zone("Europe/London"), E("1968-07-01T12:00")))
        self.assertEqual(E("1968-02-18T02:00"), T.previous_transition(zone("Europe/London"), E("1971-07-01T12:00")))
        self.assertEqual(E("1947-03-13T20:53:08"), T.previous_transition(zone("Asia/Riyadh"), E("2000-01-01T00:00")))
        self.assertIsNone(T.next_transition(zone("Asia/Riyadh"), E("2000-01-01T00:00")))
        self.assertIsNone(T.next_transition(zone("Etc/GMT+5"), E("2026-01-01T00:00")))
        self.assertIsNone(T.next_transition(zone("+05:30"), E("2026-01-01T00:00")))

    def test_z5_z10_z11_a13_refusals(self) -> None:
        db = database()
        for text in ("Factory", "America/Loſ_Angeles", "+٠٥:٣٠", "2020-01-01T00:00[Europe/Berlin]"):
            with self.assertRaises(T.RangeError):
                T.formatter_time_zone(db, text)
            with self.assertRaises(T.RangeError):
                T.time_zone_from_identifier(db, text)
        self.assertEqual("Europe/Berlin", T.to_temporal_time_zone_identifier(db, "2020-01-01T00:00[Europe/Berlin]").identifier)
        self.assertEqual("UTC", T.to_temporal_time_zone_identifier(db, "2020-01-01T00:00Z").identifier)
        self.assertEqual("+05:30", T.to_temporal_time_zone_identifier(db, "2020-01-01T00:00+05:30").identifier)
        with self.assertRaises(T.RangeError):
            T.formatter_time_zone(db, "+05:30:00")
        self.assertEqual(("Asia/Calcutta", "Asia/Kolkata"), T.formatter_time_zone(db, "asia/calcutta"))


if __name__ == "__main__":
    unittest.main()
