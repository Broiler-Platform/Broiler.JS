#!/usr/bin/env python3
"""Generate the input population of bundle sjl-6-001's reference-arm comparison.

    python3 scripts/conformance/generate_date_population.py --out <directory>

The population is the one docs/evidence/sjl-6-001/reference-arms.md declares, class by class (E1 to
E6), with the seeds it names, as its revision of 2026-10-15
(docs/evidence/sjl-6-001/reference-arms-revised-2026-10-15.md) reads the sets the rule leaves open.
The comparison in Broiler.ECMAScript.Algorithms.Tests reads what this writes; nothing here is an
expected value, and the comparison judges no input by it. Two classes carry a flag the reference
computation gives - which E2 tuples MakeDay alone runs over, and which E3 dates are valid - because
the rule names those inputs by the specification's result; the flag selects inputs, it is never
compared with a result.

Every class is one gzip file of one input per line, and manifest.json records each file's line count
and the SHA-256 of its uncompressed text, so a bundle can name the population it ran:

- a Number is the 16 upper-case hexadecimal digits of its IEEE 754 bit pattern;
- an integer of the civil classes is written in decimal;
- a string is written with every character outside '!' to '~', and the backslash, as \\uXXXX, so
  that a line holds no space, and the empty string is the empty line;
- a line of several fields separates them with one space.

**A draw** is one output of SplitMix64 (the vector generator's `splitmix64`, which
tests/test_reference_population.py tests against the published reference outputs). A choice among n
things takes the draw modulo n; an integer drawn from [low, high] is low + (draw modulo (high - low +
1)); a choice that needs a second value from the same draw takes it from the draw shifted right by 16
bits, modulo its own range. Each drawn set below states its draws in that form.

Products the comparison takes over these classes - each engine's renderings of E1's time values, and
E1's time values at every offset and name of E6 - are built by the comparison, because each engine's
rendering is that engine's own code and the products would be gigabytes as text.

The zones of E4 are read from the host's tzdata through Python's zoneinfo, whose release the manifest
records; the transitions are found by reading the offset every six hours from 1800 to 2100 and, where
it changes, bisecting to the millisecond.

The output is deterministic for one tzdata release: the same arguments give the same bytes (gzip's
header carries no time).
"""

from __future__ import annotations

import argparse
import gzip
import hashlib
import json
import math
import struct
import sys
from datetime import datetime, timedelta, timezone
from pathlib import Path
from zoneinfo import ZoneInfo

sys.path.insert(0, str(Path(__file__).resolve().parent))

import dateref as D  # noqa: E402
from generate_number_vectors import splitmix64  # noqa: E402

RULE = "docs/evidence/sjl-6-001/reference-arms.md"
REVISION = "docs/evidence/sjl-6-001/reference-arms-revised-2026-10-15.md"

SEED_E1 = 0x534A4C362D303031  # "SJL6-001"
SEED_E2 = 0x534A4C362D303032
SEED_E3 = 0x534A4C362D303033
SEED_E4 = 0x534A4C362D303034
SEED_E5 = 0x534A4C362D303035

E1_DRAWS = 1 << 16
E2_DRAWS = 1 << 16
E3_DRAWS = 1 << 16
E4_DRAWS = 1 << 14
E5_DRAWS = 1 << 16

MS_PER_DAY = 86_400_000
MAX_TIME = 8_640_000_000_000_000
INF = math.inf
NAN = math.nan

E2_VALUES = [
    -INF, -(2.0 ** 63), -1e19, -(2.0 ** 53) - 2, -(2.0 ** 53), -1e15 - 1, -1e15, -400.0, -13.0, -12.0, -1.5, -1.0,
    -0.5, -0.0, 0.0, 0.5, 1.0, 1.5, 11.0, 12.0, 13.0, 99.0, 100.0, 2.0 ** 31, 2.0 ** 32, 2.0 ** 53, 1e15, 1e15 + 1,
    1e19, 2.0 ** 63, INF, NAN,
]
E2_EPOCH = (1970.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0)

E3_BIG_YEARS = [-271821, 275760, -(2 ** 31), 2 ** 31 - 1] + [s * (10 ** 9 + k) for k in range(401) for s in (1, -1)]
E3_BIG_DAYS = [1, 28, 29, 30, 31]
E3_MONTHS_BALANCE = list(range(-25, 26)) + [2 ** 53, -(2 ** 53)]

E4_ZONES = ["UTC", "America/New_York", "Europe/Berlin", "Asia/Kolkata", "Australia/Lord_Howe", "America/St_Johns",
            "Pacific/Apia"]
E4_DELTAS = [0, 1, -1, 60_000, -60_000, 1_800_000, -1_800_000, 3_600_000, -3_600_000, 7_200_000, -7_200_000]
E4_YEARS = [1, 1700, 9999, 10000, -1, 100000]

E5_YEARS = ["0000", "0001", "1969", "1970", "9999", "10000", "-000000", "+000001", "-000001", "+271821", "-271821",
            "+275760", "-275760"]
E5_MONTHS = ["00", "01", "02", "12", "13"]
E5_DAYS = ["00", "01", "28", "29", "30", "31", "32"]
E5_HOURS = ["00", "23", "24", "25"]
E5_MINUTES = ["00", "59", "60"]
E5_FRACTIONS = ["000", "999"]
E5_OFFSETS = ["+00:00", "-00:00", "+05:30", "-05:30", "+23:59", "-23:59", "+24:00", "-24:00"]
E5_FRACTION_DIGITS = [0, 1, 2, 4, 9, 10]

WEEKDAYS = ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"]
MONTHS = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"]
MONTH_NAMES = ["January", "February", "March", "April", "May", "June", "July", "August", "September", "October",
               "November", "December"]
E5_MUTATION_CHARACTERS = list("0123456789") + list("aJnTZz") + [" ", ",", "\t", ":", "+", "-", "(", ")", ".", "/"]

E6_OFFSETS = [0, 60_000, -60_000, 1_800_000, -1_800_000, -17_762_000, 19_800_000, 50_400_000, -50_400_000,
              86_340_000, -86_340_000]
E6_NAMES = ["", "Test Zone", "Heure d’été 東京 😀"]


# --------------------------------------------------------------------------------------------------
# Writing.


def hexd(x: float) -> str:
    return f"{struct.unpack('<Q', struct.pack('<d', x))[0]:016X}"


def escape(s: str) -> str:
    return "".join(c if "!" <= c <= "~" and c != "\\" else f"\\u{ord(c):04X}" for c in s)


def unique(values: list) -> list:
    seen = set()
    out = []
    for v in values:
        key = hexd(v) if isinstance(v, float) else v
        if key not in seen:
            seen.add(key)
            out.append(v)
    return out


def tzdata_release() -> str:
    for root in ("/usr/share/zoneinfo", "/usr/lib/zoneinfo"):
        path = Path(root) / "tzdata.zi"
        if path.exists():
            first = path.read_text(encoding="utf-8").splitlines()[0]
            return first.removeprefix("# version ").strip()
    return "unknown"


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
                    "generator": "scripts/conformance/generate_date_population.py",
                    "python": sys.version.split()[0], "tzdata": tzdata_release(),
                    "seeds": seeds, "classes": self.classes}
        (self.out / "manifest.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")


def draw_integer(stream, low: int, high: int) -> int:
    return low + next(stream) % (high - low + 1)


# --------------------------------------------------------------------------------------------------
# E1: time values.


def e1() -> list[float]:
    """The edges -8.64e15 and 8.64e15 with the integers 1 ms inside and outside each; every day's first
    and last millisecond in years -1 to 1, 1969 to 1972 and 9999 to 10000; the nine special Numbers; and
    2^16 integers drawn from [-8.64e15, 8.64e15]."""
    out: list[float] = []
    for edge in (-MAX_TIME, MAX_TIME):
        out += [float(edge - 1), float(edge), float(edge + 1)]
    for first, last in ((-1, 1), (1969, 1972), (9999, 10000)):
        for year in range(first, last + 1):
            for day in range(D.day_from_year_exact(year), D.day_from_year_exact(year + 1)):
                out += [float(day * MS_PER_DAY), float(day * MS_PER_DAY + MS_PER_DAY - 1)]
    out += [-0.0, 0.5, -0.5, 1.5, -1.5, 2.0 ** 53, NAN, -INF, INF]
    stream = splitmix64(SEED_E1)
    out += [float(draw_integer(stream, -MAX_TIME, MAX_TIME)) for _ in range(E1_DRAWS)]
    return out


# --------------------------------------------------------------------------------------------------
# E2: components.


def e2() -> list[tuple[float, ...]]:
    """Seven-component tuples (year, month, date, hour, minute, second, millisecond): each of the
    thirty-two values in each position in turn, the others at the epoch's (1970, 0, 1, 0, 0, 0, 0);
    then 2^16 tuples, each component one draw among the thirty-two values and the integers -1000 to
    1000 (a draw below 32 is the value, and one at or above it the integer draw - 32 - 1000)."""
    out = []
    for position in range(7):
        for value in E2_VALUES:
            row = list(E2_EPOCH)
            row[position] = value
            out.append(tuple(row))
    stream = splitmix64(SEED_E2)
    choices = len(E2_VALUES) + 2001
    for _ in range(E2_DRAWS):
        row = []
        for _ in range(7):
            c = next(stream) % choices
            row.append(E2_VALUES[c] if c < len(E2_VALUES) else float(c - len(E2_VALUES) - 1000))
        out.append(tuple(row))
    return out


def make_day_applies(y: float, m: float, d: float) -> bool:
    """MakeDay alone runs where the reference's result is NaN or a day within a day of the time values."""
    day = D.make_day(y, m, d)
    return math.isnan(day) or abs(day) <= 100_000_001


def e2_lines(rows: list[tuple[float, ...]]) -> list[str]:
    return [" ".join(hexd(v) for v in row) + (" d" if make_day_applies(*row[:3]) else " -") for row in rows]


def e2_make_date(rows: list[tuple[float, ...]]) -> list[str]:
    """MakeDate alone: the reference's MakeDay and MakeTime of each E2 tuple, kept where the reference's
    MakeDate of them is NaN or a time value's magnitude, each pair once."""
    out = []
    for y, m, d, h, mi, s, ms in rows:
        day = D.make_day(y, m, d)
        time = D.make_time(h, mi, s, ms)
        date = D.make_date(day, time)
        if math.isnan(date) or abs(date) <= MAX_TIME:
            out.append(hexd(day) + " " + hexd(time))
    return unique(out)


# --------------------------------------------------------------------------------------------------
# E3: civil dates.


def e3_years() -> list[int]:
    return list(range(-1200, 2801)) + E3_BIG_YEARS


def e3_dates() -> list[str]:
    """Every year -1200 to 2800 with every month 0 to 13 and every day 0 to 32 (months 0 and 13, days 0
    and 32 for the validity check), flagged v where the date is valid; then the large years with
    months 1 to 12 and days 1, 28, 29, 30 and 31, flagged alike."""
    out = []
    for year in range(-1200, 2801):
        for month in range(0, 14):
            for day in range(0, 33):
                out.append(f"{year} {month} {day} {'v' if D.iso_valid(year, month, day) else '-'}")
    for year in E3_BIG_YEARS:
        for month in range(1, 13):
            for day in E3_BIG_DAYS:
                out.append(f"{year} {month} {day} {'v' if D.iso_valid(year, month, day) else '-'}")
    return out


def e3_epoch_days() -> list[str]:
    """Every epoch day from -1,000,000 to 1,000,000, then 2^16 drawn from [-1e11, 1e11]."""
    stream = splitmix64(SEED_E3)
    days = list(range(-1_000_000, 1_000_001)) + [draw_integer(stream, -10 ** 11, 10 ** 11) for _ in range(E3_DRAWS)]
    return [str(d) for d in days]


def e3_balance() -> list[str]:
    return [f"{year} {month}" for year in e3_years() for month in E3_MONTHS_BALANCE]


# --------------------------------------------------------------------------------------------------
# E4: local time.

EPOCH = datetime(1970, 1, 1, tzinfo=timezone.utc)


def offset_ms(zone: ZoneInfo, instant: int) -> int:
    off = (EPOCH + timedelta(milliseconds=instant)).astimezone(zone).utcoffset()
    return int(off / timedelta(milliseconds=1))


def transitions(zone: ZoneInfo) -> list[tuple[int, int, int]]:
    """Every change of the zone's offset from 1800 to 2100: (instant, offset before, offset after), the
    instant the first millisecond at the new offset."""
    start = int((datetime(1800, 1, 1, tzinfo=timezone.utc) - EPOCH) / timedelta(milliseconds=1))
    end = int((datetime(2101, 1, 1, tzinfo=timezone.utc) - EPOCH) / timedelta(milliseconds=1))
    step = 6 * 3_600_000
    out = []
    at, before = start, offset_ms(zone, start)
    while at < end:
        nxt = min(at + step, end)
        after = offset_ms(zone, nxt)
        if after != before:
            low, high = at, nxt
            while high - low > 1:
                mid = (low + high) // 2
                if offset_ms(zone, mid) == before:
                    low = mid
                else:
                    high = mid
            out.append((high, before, offset_ms(zone, high)))
            # Continue from the transition, so that a second change within the step is found too.
            at, before = high, offset_ms(zone, high)
            continue
        at, before = nxt, after
    return out


def utc_ms(year: int, month: int, day: int, hour: int = 0) -> int:
    return D.epoch_days(year, month, day) * MS_PER_DAY + hour * 3_600_000


def e4() -> list[str]:
    """For each zone: at every transition T, with offsets ob before and oa after, the instants T + delta
    read as UTC instants (u, LocalTime) and the wall times T + ob + delta and T + oa + delta read as
    local times (l, UTC), delta each of 0, +-1 ms, +-1 min, +-30 min, +-1 h and +-2 h; January 1 at 00:00
    and July 1 at 12:00 of the years 1, 1700, 9999, 10000, -1 and 100000, both ways; and 2^14 instants
    drawn from the first millisecond of 1800 to the last of 2200, both ways, the same in every zone."""
    low = utc_ms(1800, 1, 1)
    high = utc_ms(2201, 1, 1) - 1
    stream = splitmix64(SEED_E4)
    drawn = [draw_integer(stream, low, high) for _ in range(E4_DRAWS)]
    out = []
    for name in E4_ZONES:
        zone = ZoneInfo(name)
        points: list[tuple[str, int]] = []
        for instant, before, after in transitions(zone):
            for delta in E4_DELTAS:
                points.append(("u", instant + delta))
                points.append(("l", instant + before + delta))
                points.append(("l", instant + after + delta))
        for year in E4_YEARS:
            for month, day, hour in ((1, 1, 0), (7, 1, 12)):
                t = utc_ms(year, month, day, hour)
                points += [("u", t), ("l", t)]
        for t in drawn:
            points += [("u", t), ("l", t)]
        for kind, t in unique(points):
            out.append(f"{name} {kind} {hexd(float(t))}")
    return out


# --------------------------------------------------------------------------------------------------
# E5: strings.


def e5_format() -> list[str]:
    """Every combination of the format's shapes over the rule's values, each followed by its mutations:
    a lower-case t and z, a space for the T, a fraction of 0, 1, 2, 4, 9 and 10 digits (the digit the
    fraction is written in, repeated), the offset without its colon, and a leading and a trailing space."""
    dates = []
    for year in E5_YEARS:
        dates.append(year)
        for month in E5_MONTHS:
            dates.append(f"{year}-{month}")
            for day in E5_DAYS:
                dates.append(f"{year}-{month}-{day}")
    times = []
    for hour in E5_HOURS:
        for minute in E5_MINUTES:
            times.append((f"T{hour}:{minute}", None))
            for second in E5_MINUTES:
                times.append((f"T{hour}:{minute}:{second}", None))
                for fraction in E5_FRACTIONS:
                    times.append((f"T{hour}:{minute}:{second}.{fraction}", fraction))
    zones = [""] + ["Z"] + E5_OFFSETS
    out = []
    for date in dates:
        out.append(date)
        out += [" " + date, date + " "]
        for time, fraction in times:
            for zone in zones:
                s = date + time + zone
                out.append(s)
                out.append(s.replace("T", "t", 1))
                out.append(s.replace("T", " ", 1))
                if zone == "Z":
                    out.append(s[:-1] + "z")
                if zone not in ("", "Z"):
                    out.append(s[:-3] + s[-2:])
                if fraction is not None:
                    head = date + time[:-4]
                    for count in E5_FRACTION_DIGITS:
                        out.append(head + "." + fraction[0] * count + zone)
                out += [" " + s, s + " "]
    return unique(out)


def cases(word: str) -> list[str]:
    return unique([word, word.lower(), word.upper()])


def e5_fallback() -> list[str]:
    """Broiler.VM's fallback shapes, each dimension varied alone in three base strings - that engine's
    toString and toUTCString forms of the epoch and its calendar form: every month abbreviation and full
    name in three letter cases; every weekday, and none; GMT and UTC alone and with each sign and hhmm of
    00 to 24 hours and 00 to 99 minutes; years of one to six digits, and zero-padded to four to six, with
    no sign, + and -; day-first and month-first orders; a comma, ", " and a tab for every space; and a
    parenthesised suffix added, removed or emptied; with the correspondence's P7 strings and clock shapes."""
    full = "Thu Jan 01 1970 00:00:00 GMT+0000 (Coordinated Universal Time)"
    utc = "Thu, 01 Jan 1970 00:00:00 GMT"
    calendar = "Thu Jan 01 1970"
    bases = [full, utc, calendar]
    out = list(bases)
    for base in bases:
        for name in MONTHS + MONTH_NAMES:
            for spelled in cases(name):
                out.append(base.replace("Jan", spelled, 1))
        for weekday in WEEKDAYS:
            out.append(base.replace("Thu", weekday, 1))
        out.append(base.replace("Thu, ", "", 1).replace("Thu ", "", 1))
        for year in ["5", "70", "970", "1970", "11970", "101970", "01970", "001970"]:
            for sign in ("", "+", "-"):
                out.append(base.replace("1970", sign + year, 1))
        for separator in (",", ", ", "\t"):
            out.append(base.replace(" ", separator))
        out.append(base + " (Zone)")
        out.append(base + "(x)")
        out.append(base + " ()")
    out.append(full.replace(" (Coordinated Universal Time)", ""))
    out.append(full.replace("(Coordinated Universal Time)", "(GMT (nested))"))
    out.append("Jan 01 1970 00:00:00 GMT+0000")
    out.append("01 Jan 1970 00:00:00 GMT+0000")
    out.append("Thu 01 Jan 1970 00:00:00 GMT")
    out.append("Thu, Jan 01 1970 00:00:00 GMT")
    for prefix in ("GMT", "UTC"):
        out.append(full.replace("GMT+0000", prefix, 1))
        out.append(utc[:-3] + prefix)
        for sign in ("+", "-"):
            for hours in range(25):
                for minutes in range(100):
                    zone = f"{prefix}{sign}{hours:02d}{minutes:02d}"
                    out.append(full.replace("GMT+0000", zone, 1))
                    out.append(utc[:-3] + zone)
    for clock in ["00:00", "24:00:00", "25:00:00", "00:60:00", "00:00:60", "0:0:0", "000:00:00", "00:00:00:00", "12:"]:
        out.append(full.replace("00:00:00", clock, 1))
        out.append(utc.replace("00:00:00", clock, 1))
    out += ["Jan 1 70", "Thu Jan 01 1970 00:00:00 GMT+9999", "Fri Jan 01 1970 00:00:00 GMT+0000", "Feb 30 2021",
            "1970 Jan 01", "03/04/2021 10:00", "04.03.2021", "", " ", "(", "Jan", "1970", "Thu", "GMT", "Jan 1",
            "1 Jan", "Jan 1970", "32 Jan 1970", "0 Jan 1970", "Jan 00 1970", "Jan 31 1970 24:00:00",
            "Jan 31 1970 24:00:01", "Jan 1 -1", "Jan 1 -0001", "Jan 1 -271821", "Jan 1 275760", "Jan 1 275761"]
    return unique(out)


def e5_token(stream) -> str:
    """One token of the grammar: a draw modulo 8 chooses its kind, and the draw shifted right by 16 its
    value within the kind."""
    draw = next(stream)
    kind = draw % 8
    value = draw >> 16
    if kind == 0:
        return WEEKDAYS[value % 7]
    if kind == 1:
        names = MONTHS + MONTH_NAMES
        name = names[value % 24]
        return cases(name)[(value >> 8) % len(cases(name))]
    if kind == 2:
        digits = 1 + value % 6
        number = str((value >> 4) % (10 ** digits)).zfill(digits)
        return ["", "+", "-"][(value >> 32) % 3] + number
    if kind == 3:
        parts = 2 + value % 2
        return ":".join(f"{(value >> (4 + 8 * i)) % 70:02d}" for i in range(parts))
    if kind == 4:
        prefix = ["GMT", "UTC"][value % 2]
        if (value >> 1) % 3 == 0:
            return prefix
        return prefix + ["+", "-"][(value >> 3) % 2] + f"{(value >> 4) % 25:02d}{(value >> 12) % 100:02d}"
    if kind == 5:
        return "(" + ["Zone", "x", "", "Coordinated Universal Time"][value % 4] + ")"
    if kind == 6:
        return str(1 + value % 31)
    return str(1900 + value % 200)


def e5_drawn() -> list[str]:
    """2^16 strings: a draw gives the token count, 1 to 8; each token is drawn (e5_token) and each gap is
    one of " ", ",", ", " and "\\t" by a draw; then one character is inserted, deleted or replaced, by a draw
    modulo 3, at the draw shifted right by 16 modulo the length (plus one for an insertion), the character
    one of the mutation alphabet by a further draw."""
    stream = splitmix64(SEED_E5)
    separators = [" ", ",", ", ", "\t"]
    out = []
    for _ in range(E5_DRAWS):
        count = 1 + next(stream) % 8
        text = e5_token(stream)
        for _ in range(count - 1):
            text += separators[next(stream) % 4] + e5_token(stream)
        draw = next(stream)
        kind = draw % 3
        c = E5_MUTATION_CHARACTERS[next(stream) % len(E5_MUTATION_CHARACTERS)]
        if kind == 0:
            at = (draw >> 16) % (len(text) + 1)
            text = text[:at] + c + text[at:]
        elif kind == 1:
            at = (draw >> 16) % len(text)
            text = text[:at] + text[at + 1:]
        else:
            at = (draw >> 16) % len(text)
            text = text[:at] + c + text[at + 1:]
        out.append(text)
    return out


# --------------------------------------------------------------------------------------------------

def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--out", required=True, type=Path)
    args = parser.parse_args(argv)
    args.out.mkdir(parents=True, exist_ok=True)
    w = Writer(args.out)

    w.write("E1T", [hexd(x) for x in e1()], f"the edges, the days of six years' ranges, the special Numbers, then {E1_DRAWS} integers drawn with seed {hex(SEED_E1)}")
    rows = e2()
    w.write("E2C", e2_lines(rows), f"each value in each of the seven components in turn, then {E2_DRAWS} tuples drawn with seed {hex(SEED_E2)}; d where MakeDay alone runs")
    w.write("E2M", e2_make_date(rows), "MakeDate alone: the reference's MakeDay and MakeTime of each E2 tuple whose MakeDate is NaN or within the time values")
    w.write("E3Y", [str(y) for y in e3_years()], "the years -1200 to 2800, then the large years")
    w.write("E3D", e3_dates(), "every year -1200 to 2800 with months 0 to 13 and days 0 to 32, then the large years' dates; v where valid")
    w.write("E3E", e3_epoch_days(), f"every epoch day from -1,000,000 to 1,000,000, then {E3_DRAWS} drawn with seed {hex(SEED_E3)}")
    w.write("E3B", e3_balance(), "every year of E3Y with the months -25 to 25 and +-2^53")
    w.write("E4L", e4(), f"each zone's transitions at the deltas both ways, six years' local times, then {E4_DRAWS} instants drawn with seed {hex(SEED_E4)}")
    w.write("E5F", [escape(s) for s in e5_format()], "every combination of the format's shapes over the rule's values, each with its mutations")
    w.write("E5V", [escape(s) for s in e5_fallback()], "Broiler.VM's fallback shapes, one dimension at a time, with the correspondence's strings")
    w.write("E5G", [escape(s) for s in e5_drawn()], f"{E5_DRAWS} strings drawn with seed {hex(SEED_E5)} from the grammar of those tokens, each mutated by one character")
    w.write("E6O", [hexd(float(o)) for o in E6_OFFSETS], "the offsets of E6, in milliseconds")
    w.write("E6N", [escape(n) for n in E6_NAMES], "the names of E6: none (the empty line), an ASCII one and one outside ASCII")
    w.manifest()
    return 0


if __name__ == "__main__":
    sys.exit(main())
