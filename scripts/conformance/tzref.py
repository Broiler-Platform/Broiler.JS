"""The time-zone area's reference computation (`ref:tzref@1`), independent of the data home's generator.

Milestone SJL-8 of the shared JavaScript libraries programme gives both engines one time-zone dataset: IANA's tz
database at the release the data home pins (`Broiler.ECMAScript/data/time/tzdb/`), compiled under decision
SJL-D-20's policy, and read through `Broiler.ECMAScript.Algorithms`' zone algorithms. This module computes the
expected outcome of every vector of the corpus's `time-zones` area **without** that generator, its tables or that
library: it shares no code with the C# side and reads none of `Broiler.ECMAScript/data/time/generated/`.

**How it reads the data.**

1. It checks the pinned tarball against `tzdb.pin` (length, SHA-256 and SHA-512) and each member it reads against
   the member lines (length and SHA-256), before a byte of either is used.
2. It writes the nine source files of the default build (`africa`, `antarctica`, `asia`, `australasia`, `europe`,
   `northamerica`, `southamerica`, `etcetera`, `backward`; no `backzone`, no `factory`) into a temporary directory and
   compiles them with the host's `zic -b fat`, IANA's own compiler.
3. It reads every TZif file zic wrote with **its own reader** (below, `read_tzif`): the version 2+ data block, with
   64-bit transition times, and the footer's POSIX TZ string, which it evaluates itself (`PosixTz`) for every
   instant after the last listed transition. So recurring rules continue without limit: there is no 2037 cliff.
   Python's `zoneinfo.ZoneInfo.from_file` is not used here; the tests compare this reader with it.
4. It applies SJL-D-20's policy on top: the identifiers are the Zone and Link names of the nine files (so no
   `Factory`); each identifier's primary is CLDR 48.2.0's (`bcp47/timezone.json`, pinned in the data home beside
   the archive, checked against `cldr.pin`): the key's `_iana` or its first alias, with `Etc/UTC`, `Etc/GMT` and `GMT`
   as `UTC`; an identifier reads the zone of its primary, so `CST6CDT`, `EST5EDT`, `MST7MDT` and `PST8PDT` read
   their CLDR primaries by design; lookup is ASCII-case-insensitive and refuses every other character's folding.

On top of the database it computes, from the specification's text, the operations the vectors pin: the offset at an
instant (ECMA-262's GetNamedTimeZoneOffsetNanoseconds), the possible instants of a wall-clock time
(GetNamedTimeZoneEpochNanoseconds, solved exactly over every offset in force within a day of it), the Temporal
proposal's disambiguation, start of day, `hoursInDay` and next and previous transitions (a change of the UTC offset
only), the offset time zone identifiers, `TimeZoneEquals`, and ECMA-402's `Intl.DateTimeFormat` `timeZone` and
`supportedValuesOf("timeZone")`. An abrupt completion with a RangeError is the exception `RangeError`.

Instants are integers: seconds in the zone's data, nanoseconds in the Temporal operations.
"""

from __future__ import annotations

import base64
import bisect
import hashlib
import io
import json
import re
import shutil
import struct
import subprocess
import tarfile
import tempfile
from fractions import Fraction
from pathlib import Path

VERSION = 1
GENERATOR = f"ref:tzref@{VERSION}"

SOURCE_FILES = ("africa", "antarctica", "asia", "australasia", "europe", "northamerica", "southamerica", "etcetera",
                "backward")
CLDR_FILE = "cldr-48.2.0/json/cldr-bcp47/bcp47/timezone.json"
UTC_FAMILY = ("Etc/UTC", "Etc/GMT", "GMT")

NS_PER_SECOND = 10 ** 9
NS_PER_DAY = 86400 * NS_PER_SECOND
NS_MAX_INSTANT = 10 ** 8 * NS_PER_DAY
NS_MIN_INSTANT = -NS_MAX_INSTANT
SECONDS_MAX_INSTANT = NS_MAX_INSTANT // NS_PER_SECOND
SECONDS_MIN_INSTANT = -SECONDS_MAX_INSTANT

DEFAULT_HOME = Path(__file__).resolve().parents[2] / "Broiler.ECMAScript" / "data" / "time"


class RangeError(Exception):
    """An abrupt completion with a RangeError."""


class PinError(Exception):
    """A pinned file is not the one its pin describes."""


# --------------------------------------------------------------------------------------------------
# The proleptic Gregorian calendar, on integers (no datetime, whose years stop at 9999).

def days_from_civil(year: int, month: int, day: int) -> int:
    """Days since 1970-01-01 of a proleptic Gregorian date."""
    year -= 1 if month <= 2 else 0
    era = year // 400
    year_of_era = year - era * 400
    day_of_year = (153 * (month + (-3 if month > 2 else 9)) + 2) // 5 + day - 1
    day_of_era = year_of_era * 365 + year_of_era // 4 - year_of_era // 100 + day_of_year
    return era * 146097 + day_of_era - 719468


def civil_from_days(days: int) -> tuple[int, int, int]:
    """The proleptic Gregorian date of a day count since 1970-01-01."""
    days += 719468
    era = days // 146097
    day_of_era = days - era * 146097
    year_of_era = (day_of_era - day_of_era // 1460 + day_of_era // 36524 - day_of_era // 146096) // 365
    day_of_year = day_of_era - (365 * year_of_era + year_of_era // 4 - year_of_era // 100)
    mp = (5 * day_of_year + 2) // 153
    day = day_of_year - (153 * mp + 2) // 5 + 1
    month = mp + 3 if mp < 10 else mp - 9
    return year_of_era + era * 400 + (1 if month <= 2 else 0), month, day


def is_leap(year: int) -> bool:
    return year % 4 == 0 and (year % 100 != 0 or year % 400 == 0)


def days_in_month(year: int, month: int) -> int:
    if month == 2:
        return 29 if is_leap(year) else 28
    return 30 if month in (4, 6, 9, 11) else 31


def weekday(days: int) -> int:
    """0 for Sunday, as POSIX's Mm.w.d counts."""
    return (days + 4) % 7


def utc_epoch_ns(year: int, month: int, day: int, hour: int = 0, minute: int = 0, second: int = 0,
                 nanosecond: int = 0) -> int:
    """GetUTCEpochNanoseconds (ECMA-262 21.4.1.18) of a wall-clock time read as UTC, nanoseconds within the second."""
    return ((days_from_civil(year, month, day) * 86400 + hour * 3600 + minute * 60 + second) * NS_PER_SECOND
            + nanosecond)


def epoch_seconds(text: str) -> int:
    """The instant of an ISO date-time text `[±YY]YYYY-MM-DD[THH:MM[:SS]]Z`, in seconds; for the generator and tests."""
    match = re.fullmatch(r"([+-]\d{6}|\d{4})-(\d{2})-(\d{2})(?:T(\d{2}):(\d{2})(?::(\d{2}))?)?Z?", text)
    if not match:
        raise ValueError(text)
    year, month, day = int(match.group(1)), int(match.group(2)), int(match.group(3))
    hour, minute, second = (int(match.group(i) or 0) for i in (4, 5, 6))
    return days_from_civil(year, month, day) * 86400 + hour * 3600 + minute * 60 + second


def floor_div(a: int, b: int) -> int:
    return a // b


# --------------------------------------------------------------------------------------------------
# TZif (RFC 9636), version 2 and later: the 64-bit data block and the footer.

def read_tzif(data: bytes) -> tuple[list[int], list[int], list[tuple[int, bool]], str]:
    """(transition times, type index of each, (UT offset, isdst) of each type, footer TZ string) of a TZif file."""
    if data[:4] != b"TZif" or data[4:5] not in (b"2", b"3", b"4"):
        raise ValueError("not a TZif file of version 2 or later")
    isut, isstd, leap, timecnt, typecnt, charcnt = struct.unpack_from(">6l", data, 20)
    at = 44 + timecnt * 5 + typecnt * 6 + charcnt + leap * 8 + isstd + isut
    if data[at:at + 4] != b"TZif":
        raise ValueError("no second header")
    isut, isstd, leap, timecnt, typecnt, charcnt = struct.unpack_from(">6l", data, at + 20)
    at += 44
    times = list(struct.unpack_from(f">{timecnt}q", data, at))
    at += 8 * timecnt
    indexes = list(data[at:at + timecnt])
    at += timecnt
    types = []
    for _ in range(typecnt):
        utoff, isdst, _abbreviation = struct.unpack_from(">lBB", data, at)
        types.append((utoff, bool(isdst)))
        at += 6
    at += charcnt + leap * 12 + isstd + isut
    if data[at:at + 1] != b"\n":
        raise ValueError("no footer")
    end = data.index(b"\n", at + 1)
    if end != len(data) - 1:
        raise ValueError("bytes after the footer")
    if any(b <= a for a, b in zip(times, times[1:])):
        raise ValueError("transition times not ascending")
    return times, indexes, types, data[at + 1:end].decode("ascii")


class PosixTz:
    """A POSIX TZ string as RFC 9636 section 3.3 extends it (hours of a rule's time from -167 to 167)."""

    _NAME = r"(?:[A-Za-z]{3,}|<[A-Za-z0-9+-]{3,}>)"
    _TIME = r"[+-]?\d{1,3}(?::\d{2}(?::\d{2})?)?"
    _DATE = r"(?:J\d{1,3}|\d{1,3}|M\d{1,2}\.\d\.\d)"

    def __init__(self, text: str) -> None:
        pattern = (rf"({self._NAME})({self._TIME})(?:({self._NAME})({self._TIME})?"
                   rf"(?:,({self._DATE})(?:/({self._TIME}))?,({self._DATE})(?:/({self._TIME}))?)?)?")
        match = re.fullmatch(pattern, text)
        if not match:
            raise ValueError(f"not a POSIX TZ string: {text!r}")
        self.text = text
        self.standard = -self._seconds(match.group(2))
        self.daylight = None
        if match.group(3):
            self.daylight = -self._seconds(match.group(4)) if match.group(4) else self.standard + 3600
            if not match.group(5):
                raise ValueError(f"a TZ string with daylight time and no rule: {text!r}")
            self.start = (match.group(5), self._seconds(match.group(6)) if match.group(6) else 7200)
            self.end = (match.group(7), self._seconds(match.group(8)) if match.group(8) else 7200)

    @staticmethod
    def _seconds(text: str) -> int:
        sign = -1 if text.startswith("-") else 1
        parts = [int(p) for p in text.lstrip("+-").split(":")] + [0, 0]
        return sign * (parts[0] * 3600 + parts[1] * 60 + parts[2])

    @staticmethod
    def _day(year: int, rule: str) -> int:
        """The day (since the epoch) a rule's date names in a year."""
        if rule.startswith("J"):
            n = int(rule[1:])  # 1 to 365, February 29 never counted
            day = days_from_civil(year, 1, 1) + n - 1
            return day + 1 if is_leap(year) and n >= 60 else day
        if rule.startswith("M"):
            month, week, dow = (int(p) for p in rule[1:].split("."))
            first = days_from_civil(year, month, 1)
            day = first + (dow - weekday(first)) % 7 + 7 * (week - 1)
            while day >= first + days_in_month(year, month):
                day -= 7
            return day
        return days_from_civil(year, 1, 1) + int(rule)  # 0 to 365, February 29 counted

    def changes(self, year: int) -> list[tuple[int, int]]:
        """The year's rule instants, in UT seconds, each with the offset in force from it: ascending, and at one
        instant the start of daylight time after the end of the last."""
        if self.daylight is None:
            return []
        start = self._day(year, self.start[0]) * 86400 + self.start[1] - self.standard
        end = self._day(year, self.end[0]) * 86400 + self.end[1] - self.daylight
        return sorted([(end, 0, self.standard), (start, 1, self.daylight)])

    def offset_at(self, seconds: int) -> int:
        """The UT offset the string gives at an instant."""
        if self.daylight is None:
            return self.standard
        year = civil_from_days(floor_div(seconds, 86400))[0]
        found = None
        for y in (year - 1, year, year + 1):
            for at, _, offset in self.changes(y):
                if at <= seconds:
                    found = offset
        return self.standard if found is None else found


class Zone:
    """One compiled zone: its listed transitions, and its footer after the last."""

    def __init__(self, name: str, data: bytes) -> None:
        self.name = name
        times, indexes, types, footer = read_tzif(data)
        if not types:
            raise ValueError(f"{name}: no local time type")
        self.times = times
        self.offsets = [types[i][0] for i in indexes]
        self.initial = types[0][0]  # RFC 9636: type 0 for every instant before the first transition
        if not footer:
            raise ValueError(f"{name}: an empty footer")
        self.footer = PosixTz(footer)
        # A footer whose rules never change the offset (daylight time all year) has no transitions.
        self.footer_changes = self.footer.daylight is not None and any(
            self.footer.offset_at(at) != self.footer.offset_at(at - 1)
            for y in (2101, 2102) for at, _, _ in self.footer.changes(y))
        self.last = times[-1] if times else None
        # The last listed transition that changes the UT offset (zic's fat output can end in no-op entries).
        self.last_change = next((times[i] for i in range(len(times) - 1, -1, -1)
                                 if self.offsets[i] != (self.offsets[i - 1] if i else self.initial)), None)

    def offset_at(self, seconds: int) -> int:
        """The UT offset at an instant: the listed transitions' up to the last, the footer's after it."""
        if self.last is not None and seconds > self.last:
            return self.footer.offset_at(seconds)
        i = bisect.bisect_right(self.times, seconds) - 1
        if i < 0:
            return self.initial if self.times else self.footer.offset_at(seconds)
        return self.offsets[i]

    def _changes_after(self, seconds: int):
        """Every instant after `seconds` at which the data or the footer may change the offset, ascending."""
        i = bisect.bisect_right(self.times, seconds)
        yield from self.times[i:]
        if not self.footer_changes:
            return
        floor = max(seconds, self.last if self.last is not None else seconds)
        year = civil_from_days(floor_div(floor, 86400))[0] - 1
        while True:
            for at, _, _ in self.footer.changes(year):
                if at > floor:
                    yield at
            year += 1

    def _changes_before(self, seconds: int):
        """Every instant before `seconds` at which the data or the footer may change the offset, descending."""
        if self.footer_changes and self.last is not None and seconds > self.last + 1:
            year = civil_from_days(floor_div(seconds, 86400))[0] + 1
            last_year = civil_from_days(floor_div(self.last, 86400))[0] - 1
            while year >= last_year:
                for at, _, _ in reversed(self.footer.changes(year)):
                    if self.last < at < seconds:
                        yield at
                year -= 1
        i = bisect.bisect_left(self.times, seconds)
        yield from reversed(self.times[:i])

    def next_change(self, seconds: int, limit: int) -> int | None:
        """The first instant after `seconds`, and at or before `limit`, at which the UT offset changes."""
        for at in self._changes_after(seconds):
            if at > limit:
                return None
            if self.offset_at(at) != self.offset_at(at - 1):
                return at
        return None

    def previous_change(self, seconds: int, limit: int) -> int | None:
        """The last instant before `seconds`, and at or after `limit`, at which the UT offset changes."""
        for at in self._changes_before(seconds):
            if at < limit:
                return None
            if self.offset_at(at) != self.offset_at(at - 1):
                return at
        return None


# --------------------------------------------------------------------------------------------------
# The pins.

def read_pin(path: Path) -> tuple[dict[str, str], dict[str, tuple[int, str]]]:
    values: dict[str, str] = {}
    members: dict[str, tuple[int, str]] = {}
    for raw in path.read_text(encoding="utf-8").split("\n"):
        line = raw.strip()
        if not line or line.startswith("#"):
            continue
        parts = line.split()
        if parts[0] in ("member", "file"):
            members[parts[1]] = (int(parts[2]), parts[3].lower())
        else:
            values[parts[0]] = " ".join(parts[1:])
    return values, members


def verified_tzdb(home: Path) -> tuple[str, str, dict[str, bytes]]:
    """(release, archive SHA-256, member bytes) of the pinned tarball, each checked against tzdb.pin first."""
    values, members = read_pin(home / "tzdb" / "tzdb.pin")
    name, length = values["archive"].split()
    tarball = (home / "tzdb" / name).read_bytes()
    digest = hashlib.sha256(tarball).hexdigest()
    if len(tarball) != int(length) or digest != values["archive-sha256"] or \
            base64.b64encode(hashlib.sha512(tarball).digest()).decode("ascii") != values["archive-sha512"]:
        raise PinError(f"{name} is not the archive tzdb.pin describes")
    wanted = set(SOURCE_FILES) | {"zone.tab", "version"}
    found: dict[str, bytes] = {}
    with tarfile.open(fileobj=io.BytesIO(tarball), mode="r:gz") as archive:
        for member in archive.getmembers():
            if member.isfile() and member.name in wanted:
                found[member.name] = archive.extractfile(member).read()
    for file in sorted(wanted):
        data = found.get(file)
        if data is None or file not in members or (len(data), hashlib.sha256(data).hexdigest()) != members[file]:
            raise PinError(f"{file} is not the member tzdb.pin describes")
    release = found["version"].decode("ascii").strip()
    if release != values["version"]:
        raise PinError(f"the archive's version file says {release}, the pin {values['version']}")
    return release, digest, found


def verified_cldr(home: Path) -> bytes:
    """CLDR's bcp47/timezone.json, checked against cldr.pin first."""
    _, files = read_pin(home / "cldr" / "cldr.pin")
    data = (home / "cldr" / CLDR_FILE).read_bytes()
    if CLDR_FILE not in files or (len(data), hashlib.sha256(data).hexdigest()) != files[CLDR_FILE]:
        raise PinError(f"{CLDR_FILE} is not the file cldr.pin describes")
    return data


def source_names(sources: dict[str, bytes]) -> tuple[set[str], dict[str, str]]:
    """The Zone names, and each Link name's target, of the nine source files."""
    zones: set[str] = set()
    links: dict[str, str] = {}
    for file in SOURCE_FILES:
        for raw in sources[file].decode("utf-8").split("\n"):
            line = raw.split("#", 1)[0].split()
            if not line:
                continue
            if line[0] == "Zone":
                zones.add(line[1])
            elif line[0] == "Link":
                links[line[2]] = line[1]
    return zones, links


def cldr_primaries(data: bytes) -> dict[str, str]:
    """Each alias CLDR names, to its key's primary: `_iana`, or the key's first alias; deprecated keys skipped."""
    keys = json.loads(data.decode("utf-8"))["keyword"]["u"]["tz"]
    primaries: dict[str, str] = {}
    for key, entry in keys.items():
        if key.startswith("_") or not isinstance(entry, dict) or entry.get("_deprecated"):
            continue
        aliases = entry.get("_alias", "").split()
        if not aliases:
            continue
        primary = entry.get("_iana", aliases[0])
        for alias in aliases:
            if alias in primaries:
                raise ValueError(f"CLDR names {alias} twice")
            primaries[alias] = primary
    return primaries


def ascii_lower(text: str) -> str:
    return "".join(chr(ord(c) + 32) if "A" <= c <= "Z" else c for c in text)


# --------------------------------------------------------------------------------------------------
# The database.

class Database:
    """The pinned release compiled by zic, under SJL-D-20's policy."""

    def __init__(self, home: Path = DEFAULT_HOME, zic: str = "zic") -> None:
        if shutil.which(zic) is None:
            raise FileNotFoundError(f"no {zic} on this host")
        self.release, self.archive_sha256, sources = verified_tzdb(home)
        self.zic_version = subprocess.run([zic, "--version"], capture_output=True, text=True, check=True).stdout.strip()
        zones, links = source_names(sources)
        names = zones | set(links)
        with tempfile.TemporaryDirectory(prefix="tzref-") as scratch:
            directory = Path(scratch)
            for file in SOURCE_FILES:
                (directory / file).write_bytes(sources[file])
            output = directory / "zoneinfo"
            run = subprocess.run([zic, "-b", "fat", "-d", str(output)] + [str(directory / f) for f in SOURCE_FILES],
                                 capture_output=True, text=True)
            if run.returncode != 0:
                raise RuntimeError(f"zic failed: {run.stdout}{run.stderr}")
            written = {str(p.relative_to(output)) for p in output.rglob("*") if p.is_file()}
            if written != names:
                raise RuntimeError(f"zic wrote {len(written)} files for {len(names)} names")
            self._tzif = {name: (output / name).read_bytes() for name in sorted(names)}

        cldr = cldr_primaries(verified_cldr(home))
        self.identifiers = sorted(names)  # ordinal: every name is ASCII
        self._primary: dict[str, str] = {}
        for name in self.identifiers:
            primary = name
            if name in cldr:
                primary = cldr[name]
            else:
                # tzdb's Links where CLDR names none, followed to a name CLDR keys or a Zone.
                while primary in links and primary not in cldr:
                    primary = links[primary]
                primary = cldr.get(primary, primary)
            if primary in UTC_FAMILY:
                primary = "UTC"
            self._primary[name] = primary
        for name, primary in self._primary.items():
            if self._primary.get(primary) != primary:
                raise ValueError(f"{name}'s primary {primary} is not an identifier that is its own primary")
        self._folded = {ascii_lower(name): name for name in self.identifiers}
        if len(self._folded) != len(self.identifiers):
            raise ValueError("two identifiers differ only in ASCII case")
        self.primaries = sorted({p for p in self._primary.values()})
        self._zones: dict[str, Zone] = {}

        self.regions: dict[str, list[str]] = {}
        for raw in sources["zone.tab"].decode("utf-8").split("\n"):
            if raw and not raw.startswith("#"):
                fields = raw.split("\t")
                self.regions.setdefault(fields[0], []).append(fields[2])
        for region, region_zones in self.regions.items():
            region_zones.sort()
            for name in region_zones:
                if self._primary.get(name) != name:
                    raise ValueError(f"zone.tab's {name} for {region} is not a primary identifier")
        self.links = links
        self.zone_names = zones

    def find(self, text: str) -> str | None:
        """GetAvailableNamedTimeZoneIdentifier's [[Identifier]]: an ASCII-case-insensitive match, or None."""
        if any(ord(c) > 0x7F for c in text):
            return None
        return self._folded.get(ascii_lower(text))

    def primary(self, identifier: str) -> str:
        """The [[PrimaryIdentifier]] of an available identifier."""
        return self._primary[identifier]

    def zone(self, identifier: str) -> Zone:
        """The zone an identifier reads: its primary's compiled data."""
        primary = self._primary[identifier]
        if primary not in self._zones:
            self._zones[primary] = Zone(primary, self._tzif[primary])
        return self._zones[primary]

    def own_zone(self, name: str) -> Zone:
        """The zone zic compiled under the name itself (its own Zone, or its Link's target's), for the tests."""
        return Zone(name, self._tzif[name])

    def tzif(self, name: str) -> bytes:
        return self._tzif[name]

    def region_zones(self, region: str) -> list[str]:
        """zone.tab's zones of a region, ordinal; none for a region it does not list."""
        return list(self.regions.get(region, []))


# --------------------------------------------------------------------------------------------------
# Time zone identifiers (ECMA-262 21.4.1.19 and 21.4.1.33; the Temporal proposal's ParseTimeZoneIdentifier).

_HOUR = r"(?:[01]\d|2[0-3])"
_MINUTE_SECOND = r"[0-5]\d"


def parse_utc_offset(text: str, sub_minute: bool) -> tuple[int, int, int, int, bool] | None:
    """UTCOffset[SubMinutePrecision]: (sign, hours, minutes, nanoseconds within the minute, has seconds), or None.
    The sign is ASCII only, and the digits are ASCII only (DecimalDigit)."""
    pattern = (rf"([+-])({_HOUR})(?::?({_MINUTE_SECOND}))?" if not sub_minute else
               rf"([+-])({_HOUR})(?:(:?)({_MINUTE_SECOND})(?:(:?)({_MINUTE_SECOND})(?:[.,](\d{{1,9}}))?)?)?")
    match = re.fullmatch(pattern, text, re.ASCII)
    if not match:
        return None
    sign = -1 if match.group(1) == "-" else 1
    if not sub_minute:
        return sign, int(match.group(2)), int(match.group(3) or 0), 0, False
    separator, second_separator = match.group(3), match.group(5)
    if match.group(6) is not None and separator != second_separator:
        return None  # TimeSeparator[+Extended] twice or [~Extended] twice
    fraction = match.group(7) or ""
    nanoseconds = int(match.group(6) or 0) * NS_PER_SECOND + int((fraction + "000000000")[:9])
    return sign, int(match.group(2)), int(match.group(4) or 0), nanoseconds, match.group(6) is not None


def is_time_zone_offset_string(text: str) -> bool:
    """IsTimeZoneOffsetString (ECMA-262 21.4.1.33.1)."""
    return parse_utc_offset(text, sub_minute=True) is not None


def parse_offset_identifier(text: str) -> int | None:
    """The offset branch of ParseTimeZoneIdentifier, UTCOffset[~SubMinutePrecision]: minutes, or None."""
    parsed = parse_utc_offset(text, sub_minute=False)
    if parsed is None:
        return None
    sign, hours, minutes, _, _ = parsed
    return sign * (hours * 60 + minutes)


def format_offset_identifier(minutes: int) -> str:
    """FormatOffsetTimeZoneIdentifier: ±HH:MM, +00:00 for zero."""
    sign = "-" if minutes < 0 else "+"
    return f"{sign}{abs(minutes) // 60:02d}:{abs(minutes) % 60:02d}"


class TimeZone:
    """A Temporal time zone: an offset identifier, or an available named one."""

    def __init__(self, identifier: str, offset_minutes: int | None, zone: Zone | None) -> None:
        self.identifier = identifier
        self.offset_minutes = offset_minutes
        self.zone = zone

    def offset_seconds(self, seconds: int) -> int:
        return self.offset_minutes * 60 if self.zone is None else self.zone.offset_at(seconds)


def time_zone_from_identifier(db: Database, text: str) -> TimeZone:
    """The ZonedDateTime constructor's reading of its time zone (ParseTimeZoneIdentifier, then the lookup)."""
    minutes = parse_offset_identifier(text)
    if minutes is not None:
        return TimeZone(format_offset_identifier(minutes), minutes, None)
    identifier = db.find(text)
    if identifier is None:
        raise RangeError(f"{text!r} is not an available time zone")
    return TimeZone(identifier, None, db.zone(identifier))


_ISO_DATE_TIME = re.compile(
    r"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}(?::\d{2})?(Z|[+-]\d{2}:\d{2})?(?:\[(!?)([^\]=]+)\])?", re.ASCII)


def to_temporal_time_zone_identifier(db: Database, text: str) -> TimeZone:
    """ToTemporalTimeZoneIdentifier of a String (ParseTemporalTimeZoneString): an identifier, or else an ISO date-time
    string's zone annotation, its `Z` as UTC, or its offset. Only the date-time form `YYYY-MM-DDTHH:MM[:SS]` is read
    here: the vectors give no other."""
    try:
        return time_zone_from_identifier(db, text)
    except RangeError:
        match = _ISO_DATE_TIME.fullmatch(text)
        if match is None:
            raise
        if match.group(3) is not None:
            return time_zone_from_identifier(db, match.group(3))
        if match.group(1) == "Z":
            return time_zone_from_identifier(db, "UTC")
        if match.group(1) is not None:
            return time_zone_from_identifier(db, match.group(1))
        raise


def time_zone_equals(db: Database, one: str, two: str) -> bool:
    """TimeZoneEquals of two time zones' identifiers (each already a ZonedDateTime's [[TimeZone]])."""
    if one == two:
        return True
    if parse_offset_identifier(one) is not None or parse_offset_identifier(two) is not None:
        return False
    return db.primary(db.find(one)) == db.primary(db.find(two))


def formatter_time_zone(db: Database, text: str) -> tuple[str, str]:
    """Intl.DateTimeFormat's `timeZone` (ECMA-402 CreateDateTimeFormat): the offset, normalised, or the available
    identifier, as (the [[Identifier]], the [[PrimaryIdentifier]]); an offset with seconds or an unknown name is a
    RangeError. An ISO date-time string is neither."""
    parsed = parse_utc_offset(text, sub_minute=True)
    if parsed is not None:
        sign, hours, minutes, _, has_seconds = parsed
        if has_seconds:
            raise RangeError(f"an offset time zone with seconds: {text!r}")
        normalised = format_offset_identifier(sign * (hours * 60 + minutes))
        return normalised, normalised
    identifier = db.find(text)
    if identifier is None:
        raise RangeError(f"{text!r} is not an available time zone")
    return identifier, db.primary(identifier)


# --------------------------------------------------------------------------------------------------
# The zone operations, on nanoseconds.

def floor_seconds(ns: int) -> int:
    return ns // NS_PER_SECOND


def offset_ns(tz: TimeZone, ns: int) -> int:
    """GetOffsetNanosecondsFor: GetNamedTimeZoneOffsetNanoseconds at the instant's floor second."""
    return tz.offset_seconds(floor_seconds(ns)) * NS_PER_SECOND


def next_transition(tz: TimeZone, ns: int) -> int | None:
    """GetNamedTimeZoneNextTransition: the first instant after `ns` at which the UTC offset changes, if it is at or
    before nsMaxInstant; None for an offset zone."""
    if tz.zone is None:
        return None
    found = tz.zone.next_change(floor_seconds(ns), SECONDS_MAX_INSTANT)
    return None if found is None else found * NS_PER_SECOND


def previous_transition(tz: TimeZone, ns: int) -> int | None:
    """GetNamedTimeZonePreviousTransition: the last instant before `ns` at which the UTC offset changes, if it is at
    or after nsMinInstant; None for an offset zone."""
    if tz.zone is None:
        return None
    ceiling = -((-ns) // NS_PER_SECOND)  # a transition t (whole seconds) is before ns when t < ceil(ns / 10^9)
    found = tz.zone.previous_change(ceiling, SECONDS_MIN_INSTANT)
    return None if found is None else found * NS_PER_SECOND


def check_iso_days_range(year: int, month: int, day: int) -> None:
    """CheckISODaysRange."""
    if abs(days_from_civil(year, month, day)) > 10 ** 8:
        raise RangeError("a date more than 10^8 days from the epoch")


def named_epoch_ns(zone: Zone, wall_ns: int) -> list[int]:
    """GetNamedTimeZoneEpochNanoseconds, exactly: every instant e with e + offset(e) = the wall time. An offset is
    below a day, so every such e is within a day of the wall time read as UTC, and its offset is one the zone has
    in force somewhere in that window: each such offset is tried, and kept where it holds at its instant."""
    low = floor_seconds(wall_ns) - 86400 - 1
    high = floor_seconds(wall_ns) + 86400 + 1
    offsets = {zone.offset_at(low)}
    at = low
    while True:
        at = zone.next_change(at, high)
        if at is None:
            break
        offsets.add(zone.offset_at(at))
    found = sorted(wall_ns - o * NS_PER_SECOND for o in offsets
                   if zone.offset_at(floor_seconds(wall_ns - o * NS_PER_SECOND)) == o)
    return found


def possible_epoch_ns(tz: TimeZone, year: int, month: int, day: int, hour: int = 0, minute: int = 0,
                      second: int = 0, nanosecond: int = 0) -> list[int]:
    """GetPossibleEpochNanoseconds: the instants a wall-clock time names, ascending; a RangeError outside the
    range."""
    if tz.zone is None:
        wall = utc_epoch_ns(year, month, day, hour, minute, second, nanosecond) - tz.offset_minutes * 60 * NS_PER_SECOND
        y, m, d = civil_from_days(floor_div(wall, NS_PER_DAY))
        check_iso_days_range(y, m, d)
        possible = [wall]
    else:
        check_iso_days_range(year, month, day)
        possible = named_epoch_ns(tz.zone, utc_epoch_ns(year, month, day, hour, minute, second, nanosecond))
    for ns in possible:
        if not NS_MIN_INSTANT <= ns <= NS_MAX_INSTANT:
            raise RangeError("an instant outside the range")
    return possible


def _moved(year: int, month: int, day: int, time_ns: int) -> tuple[int, int, int, int, int, int, int]:
    """BalanceISODateTime of a date and nanoseconds from its midnight."""
    days, within = divmod(time_ns, NS_PER_DAY)
    y, m, d = civil_from_days(days_from_civil(year, month, day) + days)
    hour, rest = divmod(within, 3600 * NS_PER_SECOND)
    minute, rest = divmod(rest, 60 * NS_PER_SECOND)
    second, nanosecond = divmod(rest, NS_PER_SECOND)
    return y, m, d, hour, minute, second, nanosecond


MODES = ("compatible", "earlier", "later", "reject")


def disambiguate(tz: TimeZone, possible: list[int], year: int, month: int, day: int, hour: int, minute: int,
                 second: int, nanosecond: int, mode: str) -> int:
    """DisambiguatePossibleEpochNanoseconds."""
    if len(possible) == 1:
        return possible[0]
    if possible:
        if mode in ("earlier", "compatible"):
            return possible[0]
        if mode == "later":
            return possible[-1]
        raise RangeError("an ambiguous wall time under reject")
    if mode == "reject":
        raise RangeError("a skipped wall time under reject")
    epoch = utc_epoch_ns(year, month, day, hour, minute, second, nanosecond)
    day_before, day_after = epoch - NS_PER_DAY, epoch + NS_PER_DAY
    if not NS_MIN_INSTANT <= day_before <= NS_MAX_INSTANT or not NS_MIN_INSTANT <= day_after <= NS_MAX_INSTANT:
        raise RangeError("a day either side is outside the range")
    nanoseconds = offset_ns(tz, day_after) - offset_ns(tz, day_before)
    assert abs(nanoseconds) <= NS_PER_DAY
    time_ns = ((hour * 60 + minute) * 60 + second) * NS_PER_SECOND + nanosecond
    if mode == "earlier":
        again = possible_epoch_ns(tz, *_moved(year, month, day, time_ns - nanoseconds))
        assert again
        return again[0]
    again = possible_epoch_ns(tz, *_moved(year, month, day, time_ns + nanoseconds))
    assert again
    return again[-1]


def epoch_ns_for(tz: TimeZone, year: int, month: int, day: int, hour: int = 0, minute: int = 0, second: int = 0,
                 nanosecond: int = 0, mode: str = "compatible") -> int:
    """GetEpochNanosecondsFor."""
    possible = possible_epoch_ns(tz, year, month, day, hour, minute, second, nanosecond)
    return disambiguate(tz, possible, year, month, day, hour, minute, second, nanosecond, mode)


def start_of_day(tz: TimeZone, year: int, month: int, day: int) -> int:
    """GetStartOfDay: midnight's first instant or, where midnight is skipped, the instant of the first local time
    after the transition that skips it."""
    possible = possible_epoch_ns(tz, year, month, day)
    if possible:
        return possible[0]
    assert tz.zone is not None
    midnight = utc_epoch_ns(year, month, day)
    found = []
    at = floor_seconds(midnight) - 86400 - 1
    while True:
        at = tz.zone.next_change(at, floor_seconds(midnight) + 86400 + 1)
        if at is None:
            break
        before, after = tz.zone.offset_at(at - 1), tz.zone.offset_at(at)
        if (at + before) * NS_PER_SECOND <= midnight < (at + after) * NS_PER_SECOND:
            found.append(at * NS_PER_SECOND)
    assert len(found) == 1, found
    if not NS_MIN_INSTANT <= found[0] <= NS_MAX_INSTANT:
        raise RangeError("an instant outside the range")
    return found[0]


def hours_in_day(tz: TimeZone, year: int, month: int, day: int) -> float:
    """ZonedDateTime.prototype.hoursInDay of a day: the hours between its start and the next day's, to a Number."""
    today = start_of_day(tz, year, month, day)
    tomorrow = start_of_day(tz, *civil_from_days(days_from_civil(year, month, day) + 1))
    return float(Fraction(tomorrow - today, 3600 * NS_PER_SECOND))


def local_date(tz: TimeZone, ns: int) -> tuple[int, int, int]:
    """The ISO date of an instant in a zone (GetISODateTimeFor's date)."""
    return civil_from_days(floor_div(ns + offset_ns(tz, ns), NS_PER_DAY))


def local_hour(tz: TimeZone, ns: int) -> int:
    """The hour of an instant's wall-clock time in a zone."""
    return ((ns + offset_ns(tz, ns)) % NS_PER_DAY) // (3600 * NS_PER_SECOND)


if __name__ == "__main__":
    database = Database()
    print(f"tzdb {database.release} (archive SHA-256 {database.archive_sha256}), {database.zic_version}")
    print(f"{len(database.identifiers)} identifiers, {len(database.primaries)} primary")
