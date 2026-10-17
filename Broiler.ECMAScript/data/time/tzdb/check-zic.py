"""Compare the home's generated zone table with the host's zic, 1800 to 2037 (milestone SJL-8, data-home exit clause 3).

The tz compiler in Broiler.ECMAScript.Generation reproduces what zic writes. This script checks that claim
against the host's own zic, independently of the C# code: it compiles the nine source files the pin names
in the default build (africa, antarctica, asia, australasia, europe, northamerica, southamerica, etcetera,
backward) with `zic -b fat`, decodes ../generated/time-zones.bin and ../generated/time-zone-ids.txt with a
reader written here from the layout TzdbTables states, and compares, for every identifier of the
identifier table, the UTC offset at every instant either side changes it and the second before, from
1800-01-01 to 2038-01-01 UTC.

Two readings are compared:

- THE ENGINE'S READING. An engine resolves an identifier to its primary identifier (decision SJL-D-20:
  CLDR's) and reads the primary's zone. Four identifiers do so by design and differ from zic's compilation
  of their own Zone lines before 1967: CST6CDT, EST5EDT, MST7MDT and PST8PDT, which 2026e's `backward`
  states as Zones of their own and CLDR names as America/Chicago, America/New_York, America/Denver and
  America/Los_Angeles. They are reported as expected, and must differ only up to 1966-10-30. Any other
  difference fails the check.
- THE TABLE'S OWN ZONES. Each identifier also reads the zone the table states for it, which is its own
  Zone or its Link's target's; every one must agree with zic everywhere in the range.

Run from the repository root, with the generated tables committed:

    python3 -I Broiler.ECMAScript/data/time/tzdb/check-zic.py

It exits 0 when the check passes and 1 when it does not, and prints what it compared. It reads only the
pinned archive, after checking it against tzdb.pin, and the committed tables, after checking them against
their manifest.
"""

import argparse
import base64
import bisect
import datetime
import hashlib
import io
import math
import struct
import subprocess
import sys
import tarfile
import tempfile
from pathlib import Path

SOURCE_FILES = ["africa", "antarctica", "asia", "australasia", "europe", "northamerica", "southamerica", "etcetera", "backward"]

# 1800-01-01T00:00:00Z and 2038-01-01T00:00:00Z: the range the correspondence of bundle sjl-8-001 compared.
LO, HI = -5364662400, 2145916800

# The four identifiers that read their CLDR primaries by design, and the instant before which they may
# differ: 1966-10-31T00:00:00Z, the day after the last of their pre-1967 differences.
EXPECTED = {"CST6CDT": "America/Chicago", "EST5EDT": "America/New_York", "MST7MDT": "America/Denver", "PST8PDT": "America/Los_Angeles"}
EXPECTED_BEFORE = -100051200

MEAN_YEAR = 31556952.0


def iso(seconds):
    return (datetime.datetime(1970, 1, 1) + datetime.timedelta(seconds=seconds)).isoformat()


def read_pin(path):
    values, members = {}, {}
    for raw in path.read_text(encoding="utf-8").split("\n"):
        line = raw.strip()
        if not line or line.startswith("#"):
            continue
        parts = line.split()
        if parts[0] == "member":
            members[parts[1]] = (int(parts[2]), parts[3].lower())
        else:
            values[parts[0]] = " ".join(parts[1:])
    return values, members


def verified_sources(home):
    """The nine source files, from the pinned tarball, after the tarball and each member match tzdb.pin."""
    values, members = read_pin(home / "tzdb" / "tzdb.pin")
    name, length = values["archive"].split()
    tarball = (home / "tzdb" / name).read_bytes()
    if len(tarball) != int(length) or hashlib.sha256(tarball).hexdigest() != values["archive-sha256"] or \
            base64.b64encode(hashlib.sha512(tarball).digest()).decode() != values["archive-sha512"]:
        sys.exit(f"{name} is not the archive tzdb.pin describes")
    sources = {}
    with tarfile.open(fileobj=io.BytesIO(tarball), mode="r:gz") as archive:
        for member in archive.getmembers():
            if member.isfile() and member.name in SOURCE_FILES:
                sources[member.name] = archive.extractfile(member).read()
    for file in SOURCE_FILES:
        data = sources.get(file)
        if data is None or (len(data), hashlib.sha256(data).hexdigest()) != members[file]:
            sys.exit(f"{file} is not the member tzdb.pin describes")
    return values["version"], hashlib.sha256(tarball).hexdigest(), sources


def verified_tables(home):
    """The committed zone and identifier tables, after each matches the manifest beside it."""
    generated = home / "generated"
    manifest = {}
    for line in (generated / "tables.manifest").read_text(encoding="ascii").split("\n"):
        if line.startswith("table "):
            _, file, length, digest = line.split()
            manifest[file] = (int(length), digest)
    tables = {}
    for file in ("time-zones.bin", "time-zone-ids.txt"):
        data = (generated / file).read_bytes()
        if (len(data), hashlib.sha256(data).hexdigest()) != manifest[file]:
            sys.exit(f"generated/{file} is not the table its manifest describes")
        tables[file] = data
    return tables


class Reader:
    """A cursor over the zone table: little-endian base-128 varints, zig-zag for signed values."""

    def __init__(self, data):
        self.data, self.at = data, 0

    def byte(self):
        value = self.data[self.at]
        self.at += 1
        return value

    def unsigned(self):
        value, shift = 0, 0
        while True:
            b = self.byte()
            value |= (b & 0x7F) << shift
            shift += 7
            if b < 0x80:
                return value

    def signed(self):
        value = self.unsigned()
        return (value >> 1) ^ -(value & 1)


def decode_zones(data):
    """Each zone of the table: (initial offset, [(at, offset)], final rules or None), in the layout TzdbTables states."""
    reader = Reader(data)
    zones = []
    for _ in range(reader.unsigned()):
        initial = reader.signed()
        palette = [reader.signed() >> 1 for _ in range(reader.unsigned())]
        transitions, previous = [], 0
        for _ in range(reader.unsigned()):
            # The low bit says seconds (set) or minutes (clear); above it, the zig-zag delta.
            code = reader.unsigned()
            zigzag = code >> 1
            delta = (zigzag >> 1) ^ -(zigzag & 1)
            previous += delta if code & 1 else delta * 60
            transitions.append((previous, palette[reader.byte()]))
        final = None
        count = reader.byte()
        if count:
            standard, last_listed = reader.signed(), reader.signed()
            rules = []
            for _ in range(count):
                month, kind, weekday, day = reader.byte(), reader.byte(), reader.byte(), reader.byte()
                at = reader.signed()
                at_kind = "wsu"[reader.byte()]
                save = reader.signed()
                rules.append((month, kind, weekday, day, at, at_kind, save))
            final = (standard, last_listed, rules)
        zones.append((initial, transitions, final, [t for t, _ in transitions]))
    if reader.at != len(data):
        sys.exit("the zone table has bytes after its last zone")
    return zones


def days_from_civil(year, month, day):
    year -= 1 if month <= 2 else 0
    era = (year if year >= 0 else year - 399) // 400
    year_of_era = year - era * 400
    day_of_year = (153 * (month + (-3 if month > 2 else 9)) + 2) // 5 + day - 1
    day_of_era = year_of_era * 365 + year_of_era // 4 - year_of_era // 100 + day_of_year
    return era * 146097 + day_of_era - 719468


def month_length(year, month):
    if month == 2:
        return 29 if year % 4 == 0 and (year % 100 != 0 or year % 400 == 0) else 28
    return 30 if month in (4, 6, 9, 11) else 31


def weekday_of(days):
    return (days + 3) % 7  # Monday 0; 1970-01-01 was a Thursday


def on_day(year, month, kind, weekday, day):
    if kind == 1:
        d = days_from_civil(year, month, month_length(year, month))
        while weekday_of(d) != weekday:
            d -= 1
        return d
    if kind in (2, 3):
        d = days_from_civil(year, month, day)
        while weekday_of(d) != weekday:
            d += 1 if kind == 2 else -1
        return d
    return days_from_civil(year, month, day)


def year_of(seconds):
    return 1970 + math.floor(seconds / MEAN_YEAR)


def events(rules, first, last):
    found = []
    for year in range(first, last + 1):
        for (month, kind, weekday, day, at, at_kind, save) in rules:
            found.append((on_day(year, month, kind, weekday, day) * 86400 + at, at_kind, save))
    return [e for _, e in sorted(enumerate(found), key=lambda p: (p[1][0], p[0]))]


def universal(local, kind, standard, save):
    return local if kind == "u" else local - standard if kind == "s" else local - standard - save


def recurring_offset(final, at):
    standard, _, rules = final
    year = year_of(at)
    save = events(rules, year - 2, year - 2)[-1][2]
    offset = standard + save
    for local, kind, following in events(rules, year - 1, year + 1):
        if universal(local, kind, standard, save) > at:
            break
        offset, save = standard + following, following
    return offset


def table_offset(zone, at):
    initial, transitions, final, times = zone
    if final is not None and year_of(at) > final[1]:
        return recurring_offset(final, at)
    i = bisect.bisect_right(times, at) - 1
    return initial if i < 0 else transitions[i][1]


def table_breaks(zone):
    """Every instant at which the table's offset can change in the range: listed transitions, each recurring
    rule's instant under every save its rules state, and each mean-year boundary of the recurring region."""
    _, _, final, times = zone
    points = set(times)
    if final is not None:
        standard, last_listed, rules = final
        saves = {0} | {rule[6] for rule in rules}
        for year in range(last_listed - 1, 2040):
            boundary = math.ceil((year - 1970) * MEAN_YEAR)
            points.add(boundary)
            for local, kind, _ in events(rules, year, year):
                for save in saves:
                    points.add(universal(local, kind, standard, save))
    return points


def tzif(path):
    data = path.read_bytes()
    isut, isstd, leap, count, types, chars = struct.unpack_from(">6i", data, 20)
    at = 44 + count * 5 + types * 6 + chars + leap * 8 + isstd + isut
    isut, isstd, leap, count, types, chars = struct.unpack_from(">6i", data, at + 20)
    at += 44
    times = list(struct.unpack_from(f">{count}q", data, at))
    at += 8 * count
    indexes = data[at:at + count]
    at += count
    offsets = [struct.unpack_from(">i", data, at + 6 * i)[0] for i in range(types)]
    return offsets[0], times, [offsets[i] for i in indexes]


def zic_offset(compiled, at):
    initial, times, offsets = compiled
    i = bisect.bisect_right(times, at) - 1
    return initial if i < 0 else offsets[i]


def differences(zone, compiled):
    _, times, _ = compiled
    points = sorted({LO} | {t for t in table_breaks(zone) if LO <= t < HI} | {t for t in times if LO <= t < HI})
    return [u for t in points for u in (t, t - 1) if LO <= u < HI and table_offset(zone, u) != zic_offset(compiled, u)]


def main():
    parser = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    parser.add_argument("--home", type=Path, default=Path(__file__).resolve().parent.parent, help="the data home, Broiler.ECMAScript/data/time")
    parser.add_argument("--zic", default="zic", help="the zic to run")
    arguments = parser.parse_args()
    home = arguments.home

    version = subprocess.run([arguments.zic, "--version"], capture_output=True, text=True, check=True).stdout.strip()
    release, archive_digest, sources = verified_sources(home)
    tables = verified_tables(home)
    zones = decode_zones(tables["time-zones.bin"])
    rows = [line.split("|") for line in tables["time-zone-ids.txt"].decode("ascii").split("\n")]
    index = {name: int(zone) for name, zone, _ in rows}

    print(f"zic: {version}")
    print(f"tzdb {release}, tzdata archive SHA-256 {archive_digest}; nine source files: {' '.join(SOURCE_FILES)}")
    print(f"zone table: {len(zones)} zones; identifier table: {len(rows)} identifiers")
    print(f"range: {iso(LO)}Z to {iso(HI)}Z, at every instant either side changes the offset and the second before")

    with tempfile.TemporaryDirectory(prefix="check-zic-") as scratch:
        scratch = Path(scratch)
        for file in SOURCE_FILES:
            (scratch / file).write_bytes(sources[file])
        output = scratch / "zoneinfo"
        run = subprocess.run([arguments.zic, "-b", "fat", "-d", str(output)] + [str(scratch / f) for f in SOURCE_FILES],
                             capture_output=True, text=True)
        if run.returncode != 0:
            sys.exit(f"zic failed:\n{run.stdout}{run.stderr}")
        if run.stdout.strip() or run.stderr.strip():
            print("zic said:", (run.stdout + run.stderr).strip().replace("\n", "\n  "))

        engine, own, failures = [], [], []
        for name, zone, primary in rows:
            compiled = tzif(output / name)
            read = index[primary] if primary else int(zone)
            found = differences(zones[read], compiled)
            if found:
                engine.append((name, primary, found, compiled, zones[read]))
            if differences(zones[int(zone)], compiled):
                own.append(name)

    print()
    print(f"THE ENGINE'S READING (identifier -> CLDR primary -> zone): identifiers compared: {len(rows)} ; "
          f"differing from zic -b fat of the same nine files, 1800-2037: {len(engine)}")
    for name, primary, found, compiled, zone in engine:
        first, last = found[0], found[-1]
        expected = EXPECTED.get(name) == primary and last < EXPECTED_BEFORE
        print(f"   {name} reads {primary or 'itself'}: first at {iso(first)}, table {table_offset(zone, first)}, zic {zic_offset(compiled, first)} "
              f"({len(found)} instants, the last at {iso(last)}) - {'EXPECTED: its CLDR primary by design' if expected else 'UNEXPECTED'}")
        if not expected:
            failures.append(name)
    missing = sorted(set(EXPECTED) - {name for name, *_ in engine})
    for name in missing:
        print(f"   {name}: expected to differ by reading {EXPECTED[name]}, and agrees with zic - UNEXPECTED")
        failures.append(name)

    print()
    print(f"THE TABLE'S OWN ZONES (identifier -> its own Zone or its Link's target): identifiers compared: {len(rows)} ; "
          f"differing from zic -b fat: {len(own)}")
    for name in own:
        print(f"   {name} - UNEXPECTED")
        failures.append(name)

    print()
    agreeing = len(rows) - len(engine)
    if failures:
        print(f"FAIL: {len(set(failures))} unexpected: {' '.join(sorted(set(failures)))}")
        return 1
    print(f"PASS: {agreeing} of {len(rows)} identifiers agree with zic -b fat from 1800 to 2037 as an engine reads them; "
          f"the {len(engine)} others ({', '.join(name for name, *_ in engine)}) read their CLDR primaries by design and differ "
          f"only before 1966-10-31; all {len(rows) - len(own)} of the table's own zones agree.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
