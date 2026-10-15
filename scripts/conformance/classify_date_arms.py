#!/usr/bin/env python3
"""Classify the differences of bundle sjl-6-001's reference-arm comparison against the reference.

    python3 scripts/conformance/classify_date_arms.py --run <comparison output directory> [--examples 20]

The comparison (Broiler.ECMAScript.Algorithms.Tests, run as `compare-date`) classifies nothing: it lists every
input on which a variant of an arm differs from S called as that arm's engine calls it. This script is the
owner's classification. It was written before the judged run, with the rule's revision of 2026-10-15
(docs/evidence/sjl-6-001/reference-arms-revised-2026-10-15.md), and is applied mechanically, so that every class
can be checked against the reference value line by line. The arbiter is dateref.py, the corpus's reference
computation, written from the specification's text; the agreement of the arms decides nothing.

For each difference of one variant from S:

- the reference computes the specification's result for the input, as the variant projects it;
- a charge that differs is class (c) whatever the values: Broiler.VM's units are the specification of the
  charge (JSD-0072, decision 4), and the rule makes a charge of S that differs from arm V's a defect of S;
- **Broiler.VM's dialect has no arbiter but arm V**: a difference of S's Broiler.VM dialect from arm V is class
  (c), except where correspondence row P9 makes it. On an engine's rendering of a negative year (one of year 0
  at most, since a western zone renders year 0's first hours as -0001) the reference expects the rendered time
  value without its milliseconds (Date.parse's expectation, 21.4.3.2): S's answering it is class (a), P9.
  Elsewhere a string holding a token of a minus sign and digits is P9's when S's reading of it without those
  tokens, which the comparison lists and which Broiler.VM's reader skips, is arm V's: such a string is outside
  the format, so class (b);
- **a string outside the format** - dateref.parse_format, with the strict reading of a day past its month's
  length, refuses it - is class (b), implementation-defined, when S answers its dialect's reading and the arm
  its engine's own (arm J against S's strict dialect then Broiler.VM's, arm V's format path against S's strict
  dialect), with its kind: the mutation or shape that puts it outside;
- **local time** is judged by the specification's algorithm over the zone's offsets: through zoneinfo where
  the offsets S's provider read from the host's TimeZoneInfo, which the comparison lists beside the input,
  are zoneinfo's at every instant concerned (an instant past year 9999 read 400 years earlier as often as
  needed); over the listed offsets where they are not, because there the two readers of the same tzdata
  differ (the revision's section 3, as revised after the first run). A zone's name is the provider's; a
  toString in a host zone that differs only in the name is judged by whether the name follows the instant's
  daylight-saving time as zoneinfo reads it;
- class (a), a correction: S's result is the reference's and the arm's is not. It is listed with the engine it
  corrects and, where the area verdict's correspondence names it, its row;
- otherwise unclassified: S's result is not the reference's (class (c), a defect of S, which is fixed and the
  whole comparison run again), the reference cannot judge the input, or the variant is a negative control.

Outputs, in the run directory: classification.tsv.gz holds every difference with its input, S's result, the
arm's result, the reference, the class, the reason and the correspondence row; a field longer than 256
characters is shown by its length. report.md gives per operation, variant, class and reason the count and the
first --examples differences in full. classification.json gives the counts, the class (b) kinds, and the
SHA-256 of both listings' uncompressed text.

Revised 2026-10-15, after the first judged run and before the run that judges, for four defects of this
script that the run showed (reference-arms-revised-2026-10-15-after-first-run.md says what was seen). First, a
string of class E5 was judged as the population writes it, its spaces and every character outside '!' to '~'
still escaped, because the listing escapes the population's line once more and this script unescaped it only
once; a parse's string is now unescaped twice. Second, a rendering read back was taken to be of a negative year
when its time value's year in UTC was negative; New York's rendering of year 0's first hours shows -0001, so
it is now a year of at most 0. Third, local time was judged through zoneinfo for every instant from 1913, which
section 3 of the revision set on a reading of the two zone readers that held no year from 1901 to 1949; St
John's offsets differ until 1935. Zoneinfo is now used where the listed offsets are its own, and the listed
offsets elsewhere. Fourth, a string was taken to be P9's when arm V refused it and S did not; a signed year
can also make a later number the day rather than the year, with both answering, so a string is now P9's when
S's reading of it without its signed-year tokens, which the comparison now lists, is arm V's. Nothing else
changed.
"""

from __future__ import annotations

import argparse
import gzip
import hashlib
import json
import math
import re
import struct
import sys
from collections import Counter, defaultdict
from datetime import datetime, timedelta, timezone
from pathlib import Path
from zoneinfo import ZoneInfo

sys.path.insert(0, str(Path(__file__).resolve().parent))

import dateref as D  # noqa: E402

SHOWN = 256
DAY = 86_400_000
FOUR_CENTURIES = 146097 * DAY
VM_NAME = "Coordinated Universal Time"
TEMPORAL_MIN_DAYS, TEMPORAL_MAX_DAYS = -100_000_001, 100_000_000


# --------------------------------------------------------------------------------------------------
# The comparison's rendering, mirrored.

def escape(text: str) -> str:
    return "".join(c if "!" <= c <= "~" and c != "\\" else f"\\u{ord(c):04X}" for c in text)


def unescape(text: str) -> str:
    return re.sub(r"\\u([0-9A-F]{4})", lambda m: chr(int(m.group(1), 16)), text)


def f64(x: float) -> str:
    """Render.F64: every NaN as one, otherwise the bit pattern."""
    if math.isnan(x):
        return "NaN"
    return f"f64:{struct.unpack('<Q', struct.pack('<d', x))[0]:016X}"


def from_f64(text: str) -> float:
    if text == "NaN":
        return math.nan
    return struct.unpack("<d", struct.pack("<Q", int(text.removeprefix("f64:"), 16)))[0]


def num(x) -> str:
    return f64(float(x))


def string(text: str) -> str:
    return "str:" + escape(text)


def boolean(value: bool) -> str:
    return "bool:true" if value else "bool:false"


def pair(a: int, b: int) -> str:
    return f"pair:{a},{b}"


def civil(y: int, m: int, d: int) -> str:
    return f"date:{y}-{m}-{d}"


def charged(value: str) -> tuple[str, str | None]:
    if "|charge:" in value:
        head, _, charge = value.rpartition("|charge:")
        return head, charge
    return value, None


def shown(text: str | None) -> str:
    if text is None:
        return "-"
    return text if len(text) <= SHOWN else f"({len(text)} characters)"


def detail_map(detail: str) -> dict[str, str]:
    out = {}
    for part in detail.split(" "):
        key, sep, value = part.partition("=")
        if sep:
            out[key] = value
    return out


# --------------------------------------------------------------------------------------------------
# Inputs, as the population writes them.

def double(hex_text: str) -> float:
    return struct.unpack("<d", struct.pack("<Q", int(hex_text, 16)))[0]


def components(raw: str) -> list[float]:
    return [double(h) for h in raw.split(" ")[:7]]


# --------------------------------------------------------------------------------------------------
# Local time: zoneinfo from 1913, the provider's listed offsets before.

def agrees_with_zoneinfo(zone: str, listed_at: dict[int, int]) -> bool:
    """Whether every offset S's provider read is zoneinfo's at the same instant."""
    info = ZoneInfo(zone)
    return all(D.zone_offset_ms(info, instant) == offset for instant, offset in listed_at.items())


def local_time_ref(t: int, zone: str, listed: dict[str, str]) -> int:
    o = int(float(listed["o"]))
    if agrees_with_zoneinfo(zone, {t: o}):
        return D.local_time(t, ZoneInfo(zone))
    return t + o


def utc_ref(t: int, zone: str, listed: dict[str, str]) -> int:
    b, a = int(float(listed["b"])), int(float(listed["a"]))
    known = {t - DAY: b, t + DAY: a, t - b: int(float(listed["pb"])), t - a: int(float(listed["pa"]))}
    if agrees_with_zoneinfo(zone, known):
        return D.utc(t, ZoneInfo(zone))
    return D.utc_over(t, lambda instant: known[instant])


def daylight(t: int, zone: str) -> bool:
    moved = t
    while moved > D.LAST_MS:
        moved -= FOUR_CENTURIES
    moved = max(moved, D.FIRST_MS + 2 * DAY)
    when = datetime(1970, 1, 1, tzinfo=timezone.utc) + timedelta(milliseconds=moved)
    return bool(when.astimezone(ZoneInfo(zone)).dst())


# --------------------------------------------------------------------------------------------------
# Strings outside the format.

FORMAT_HEAD = r"^([+-]\d{6}|\d{4})(-\d\d(-\d\d)?)?"


def outside_kind(text: str) -> str:
    """The mutation or shape that puts a string outside the format."""
    if text != text.strip(" \t"):
        return "a leading or trailing space"
    if re.match(FORMAT_HEAD + r"t", text):
        return "a lower-case t (P6)"
    if re.match(FORMAT_HEAD + r" \d", text):
        return "a space for the T"
    if re.search(r"\d\dz$", text):
        return "a lower-case z (P6)"
    fraction = re.search(r":\d\d\.(\d*)(Z|z|[+-]|$)", text)
    if fraction and len(fraction.group(1)) != 3:
        return f"a fraction of {len(fraction.group(1))} digits (P4)"
    if re.search(r"T\d\d:\d\d(:\d\d(\.\d*)?)?[+-]\d{4}$", text):
        return "an offset without its colon (P6)"
    if D.parse_format(text) is not None and D.parse_format(text, strict_days=True) is None:
        return "a day past its month's length (P5)"
    if D.parse_format(text) is None and re.match(FORMAT_HEAD, text):
        return "the format's shape with an element out of its bounds"
    return "a shape of the fallback"


def format_value(text: str) -> float | None:
    """The time value of an instance of the format (strict days), local time UTC; None outside the format."""
    parsed = D.parse_format(text, strict_days=True)
    if parsed is None:
        return None
    return D.format_time_value(parsed[1])


def rendered_value(listed: dict[str, str]) -> float:
    """Date.parse's expectation of an engine's rendering: the time value rendered, without its milliseconds."""
    tv = from_f64(listed["tv"])
    return tv - D.ms_from_time(tv)


# --------------------------------------------------------------------------------------------------
# The reference per operation.

FIELDS = {
    "Day": D.day, "TimeWithinDay": D.time_within_day, "YearFromTime": D.year_from_time, "InLeapYear": D.in_leap_year,
    "MonthFromTime": D.month_from_time, "DateFromTime": D.date_from_time, "WeekDay": D.week_day,
    "HourFromTime": D.hour_from_time, "MinFromTime": D.min_from_time, "SecFromTime": D.sec_from_time,
    "msFromTime": D.ms_from_time,
}


def balance(year: int, month: int) -> tuple[int, int]:
    return year + (month - 1) // 12, (month - 1) % 12 + 1


def to_date_string(t: float, offset: float, name: str | None) -> str:
    local = t + offset
    return D.date_string(local) + " " + D.time_string(local) + D.time_zone_string(int(offset), name)


def reference(operation: str, variant: str, cls: str, listed: dict[str, str], raw: str, text: str) -> str | None:
    """The reference's result as the variant projects it, or None when the reference cannot judge."""
    if operation in FIELDS:
        return num(FIELDS[operation](double(raw)))
    if operation == "DaysInYear":
        return num(D.days_in_year(float(int(raw))))
    if operation == "DayFromYear":
        return num(D.day_from_year(float(int(raw))))
    if operation in ("MakeTime", "MakeFullYear", "MakeDay", "Date.UTC's core"):
        y, m, d, h, mi, s, ms = components(raw)
        if operation == "MakeTime":
            return num(D.make_time(h, mi, s, ms))
        if operation == "MakeFullYear":
            return num(D.make_full_year(y))
        if operation == "MakeDay":
            return num(D.make_day(y, m, d))
        return num(D.time_clip(D.make_date(D.make_day(y, m, d), D.make_time(h, mi, s, ms))))
    if operation == "MakeDate":
        day, time = (double(h) for h in raw.split(" "))
        return num(D.make_date(day, time))
    if operation == "TimeClip":
        return num(D.time_clip(double(raw)))

    if operation in ("the leap year", "the days in a year", "the month length"):
        year = int(raw)
        if operation == "the leap year":
            return boolean(D.iso_leap(year))
        if operation == "the days in a year":
            return num(D.iso_days_in_year(year))
        return num(D.iso_days_in_month(year, int(listed["month"])))
    if operation in ("epoch days from a date", "the weekday", "the day of the year", "the ISO week and its year", "the ISO date's validity"):
        y, m, d, _ = raw.split(" ")
        y, m, d = int(y), int(m), int(d)
        if operation == "the ISO date's validity":
            valid = D.iso_valid(y, m, d)
            if "Temporal's limits" in variant:
                valid = valid and TEMPORAL_MIN_DAYS <= D.epoch_days(y, m, d) <= TEMPORAL_MAX_DAYS
            return boolean(valid)
        if operation == "epoch days from a date":
            return num(D.epoch_days(y, m, d))
        if operation == "the weekday":
            weekday = D.iso_day_of_week(y, m, d)
            return num(weekday - 1 if "Monday 0" in variant else weekday)
        if operation == "the day of the year":
            return num(D.iso_day_of_year(y, m, d))
        week, year = D.iso_week_of_year(y, m, d)
        return pair(week, year)
    if operation == "a date from epoch days":
        return civil(*D.from_epoch_days(int(raw)))
    if operation == "BalanceISOYearMonth":
        y, m = (int(v) for v in raw.split(" "))
        return pair(*balance(y, m))
    if operation == "a local time's civil fields":
        local = double(raw) + float(listed["offset"])
        fields = [D.year_from_time(local), D.month_from_time(local) + 1, D.date_from_time(local), D.week_day(local),
                  D.hour_from_time(local), D.min_from_time(local), D.sec_from_time(local), D.ms_from_time(local),
                  D.day_within_year(local) + 1]
        return ",".join(str(int(f)) for f in fields)

    if operation in ("LocalTime", "UTC"):
        zone, kind, hex_t = raw.split(" ")
        t = int(double(hex_t))
        return num(local_time_ref(t, zone, listed) if operation == "LocalTime" else utc_ref(t, zone, listed))

    if operation.startswith("parse"):
        value = rendered_value(listed) if "tv" in listed else format_value(text)
        return None if value is None else num(value)

    if operation in ("DateString", "TimeString", "toUTCString", "toISOString", "TimeZoneString, Broiler.VM's zone", "toString, Broiler.VM's zone"):
        t = double(raw)
        if operation == "DateString":
            return string(D.date_string(t))
        if operation == "TimeString":
            return string(D.time_string(t))
        if operation == "toUTCString":
            return string(D.to_utc_string(t))
        if operation == "toISOString":
            try:
                return string(D.to_iso_string(t))
            except D.RangeError:
                return "error:RangeError"
        if operation == "TimeZoneString, Broiler.VM's zone":
            return string(" GMT" + D.time_zone_string(0, VM_NAME))
        return string("Invalid Date" if math.isnan(t) else to_date_string(t, 0.0, VM_NAME))
    if operation in ("TimeZoneString", "toString"):
        t = double(raw)
        offset = float(listed["offset"])
        name = None if listed["name"] == "-" else unescape(listed["name"])
        if operation == "TimeZoneString":
            return string("GMT" + D.time_zone_string(int(offset), name))
        return string(to_date_string(t, offset, name))
    if operation == "toString, a host zone":
        t = int(double(raw))
        zone = listed["zone"]
        offset = local_time_ref(t, zone, listed) - t
        return string(to_date_string(float(t), float(offset), None))
    return None


# --------------------------------------------------------------------------------------------------
# Correspondence rows.

def row_of(operation: str, variant: str, raw: str, listed: dict[str, str], text: str) -> str:
    arm = variant.split(":")[0]
    if operation in ("MakeDay", "Date.UTC's core"):
        if abs(components(raw)[1]) >= 2.0 ** 53:
            return "T4"
    if operation == "the ISO week and its year" and arm.startswith("J") and int(raw.split(" ")[0]) < 0:
        return "G2"
    if operation in ("LocalTime", "UTC", "toString, a host zone") and arm == "J":
        t = int(double(raw.split(" ")[-1] if operation != "toString, a host zone" else raw))
        if not (-62135596800000 <= t <= 253402300799999):
            return "L4"
        if operation == "UTC":
            return "L3"
    if operation.startswith("parse") and "tv" in listed:
        year = D.year_from_time(from_f64(listed["tv"]))
        if year < 1 or year > 9999:
            return "P9"
        return "P8"
    if arm == "J" and operation in ("TimeZoneString", "toString", "toString, a host zone"):
        return "R3"
    return "-"


# --------------------------------------------------------------------------------------------------

def classify(row: list[str]) -> tuple[str, str, str, str]:
    """(verdict, reason, reference, correspondence row) of one difference."""
    operation, cls, _, detail, listed_input, variant, s_listed, arm_listed = row
    listed = detail_map(unescape(detail))
    raw = unescape(listed_input)
    s_value, s_charge = charged(unescape(s_listed))
    arm_value, arm_charge = charged(unescape(arm_listed))
    # The listing escapes the population's line once more: a string of E5 is the line unescaped again.
    text = unescape(raw) if cls.startswith("E5") else ""
    if operation.startswith("parse") and cls == "E1T":
        text = ""  # the rendering read back is not listed; its time value is in the detail

    if "control" in variant:
        return "unclassified", "a negative control", "-", "-"
    if s_charge != arm_charge:
        return "unclassified", "a charge of S differs from arm V's (class (c))", "-", "-"

    if operation in ("parse, Broiler.VM's dialect", "parse, Broiler.VM's dialect, the renderings"):
        if "tv" in listed:
            ref = num(rendered_value(listed))
            year = D.year_from_time(from_f64(listed["tv"]))
            if year <= 0 and s_value == ref and arm_value != ref:
                return "(a)", "corrects Broiler.VM: its rendering of a negative year read back", ref, "P9"
            return "unclassified", "S's Broiler.VM dialect differs from arm V outside P9 (class (c))", ref, "-"
        if "unsigned" in listed and listed["unsigned"] == arm_value:
            return "(b)", "implementation-defined: a signed year outside the renderings (P9's reading)", "-", "P9"
        return "unclassified", "S's Broiler.VM dialect differs from arm V outside P9 (class (c))", "-", "-"

    ref = reference(operation, variant, cls, listed, raw, text)

    if operation.startswith("parse") and ref is None:
        kind = outside_kind(text)
        if s_value == "NaN" or operation == "parse, Broiler.JS's order":
            return "(b)", "implementation-defined, outside the format: " + kind, "-", "-"
        return "unclassified", "the reference cannot judge the input", "-", "-"
    if ref is None:
        return "unclassified", "the reference cannot judge the input", "-", "-"

    if operation == "toString, a host zone" and s_value != arm_value:
        s_text, arm_text = unescape(s_value[4:]), unescape(arm_value[4:])
        s_head, arm_head = s_text.split(" (")[0], arm_text.split(" (")[0]
        ref_text = unescape(ref[4:])
        if s_head == ref_text and arm_head == ref_text:
            zone = listed["zone"]
            t = int(double(raw))
            names = (s_text.partition(" (")[2], arm_text.partition(" (")[2])
            expects_daylight = daylight(t, zone)
            s_follows = ("Daylight" in names[0] or "Summer" in names[0]) == expects_daylight
            if s_follows:
                return "(a)", "corrects Broiler.JS: the zone's name read at the instant, not at the moment of the call", ref, "L4"
            return "unclassified", "S's zone name does not follow the instant's daylight-saving time (class (c))", ref, "-"
        if s_head == ref_text:
            return "(a)", "corrects Broiler.JS", ref, row_of(operation, variant, raw, listed, text)
        return "unclassified", "S is not the reference's (class (c))", ref, "-"

    if s_value == ref and arm_value != ref:
        engine = "Broiler.JS" if variant.startswith("J") else "Broiler.VM"
        return "(a)", "corrects " + engine, ref, row_of(operation, variant, raw, listed, text)
    if s_value == ref and arm_value == ref:
        return "unclassified", "both agree with the reference but differ from each other (class (c))", ref, "-"
    return "unclassified", "S is not the reference's (class (c))", ref, "-"


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--run", required=True, type=Path)
    parser.add_argument("--examples", type=int, default=20)
    args = parser.parse_args(argv)

    with gzip.open(args.run / "differences.tsv.gz", "rt", encoding="utf-8", newline="\n") as stream:
        lines = stream.read().split("\n")
    header, rows = lines[0], [line.split("\t") for line in lines[1:] if line]
    assert header == "operation\tclass\tline\tdetail\tinput\tvariant\tS\tresult", header

    out_lines = ["operation\tclass\tline\tdetail\tinput\tvariant\tS\tresult\treference\tverdict\treason\trow"]
    counts: Counter = Counter()
    kinds: Counter = Counter()
    rows_by_correspondence: Counter = Counter()
    examples: dict = defaultdict(list)
    for row in rows:
        verdict, reason, ref, correspondence = classify(row)
        out_lines.append("\t".join(row[:6] + [shown(row[6]), shown(row[7]), shown(escape(ref) if ref != "-" else "-"), verdict, reason, correspondence]))
        key = (row[0], row[5], verdict, reason)
        counts[key] += 1
        if verdict == "(b)":
            kinds[(row[5].split(":")[0], reason)] += 1
        if correspondence != "-":
            rows_by_correspondence[(correspondence, row[5].split(":")[0], verdict)] += 1
        if len(examples[key]) < args.examples:
            examples[key].append(row)

    text = "".join(line + "\n" for line in out_lines)
    with gzip.GzipFile(args.run / "classification.tsv.gz", "wb", compresslevel=9, mtime=0) as target:
        target.write(text.encode("utf-8"))
    differences_text = "".join(line + "\n" for line in lines if line)
    by_verdict = Counter()
    for (_, _, verdict, _), n in counts.items():
        by_verdict[verdict] += n
    summary = {
        "differences": len(rows),
        "verdicts": dict(sorted(by_verdict.items())),
        "classes": [{"operation": o, "variant": v, "verdict": c, "reason": r, "count": n} for (o, v, c, r), n in sorted(counts.items())],
        "kinds (b)": [{"arm": a, "kind": k, "count": n} for (a, k), n in sorted(kinds.items())],
        "correspondence rows": [{"row": r, "arm": a, "verdict": v, "count": n} for (r, a, v), n in sorted(rows_by_correspondence.items())],
        "differences.tsv": {"lines": len([line for line in lines if line]), "sha256": hashlib.sha256(differences_text.encode("utf-8")).hexdigest()},
        "classification.tsv": {"lines": len(out_lines), "sha256": hashlib.sha256(text.encode("utf-8")).hexdigest()},
    }
    (args.run / "classification.json").write_text(json.dumps(summary, indent=2) + "\n", encoding="utf-8")

    report = ["# Bundle sjl-6-001's reference-arm comparison, classified", "",
              f"{len(rows)} differences: " + ", ".join(f"{n} {v}" for v, n in sorted(by_verdict.items())) + ".", ""]
    for (operation, variant, verdict, reason), n in sorted(counts.items()):
        report.append(f"## {operation}, {variant}: {verdict}, {n}")
        report.append("")
        report.append(f"{reason}.")
        report.append("")
        for row in examples[(operation, variant, verdict, reason)]:
            report.append(f"- `{row[1]}:{row[2]}` {shown(row[3])} input `{shown(row[4])}`: S `{shown(row[6])}`, arm `{shown(row[7])}`")
        report.append("")
    (args.run / "report.md").write_text("\n".join(report), encoding="utf-8")
    print(json.dumps(summary["verdicts"]))
    return 0 if set(by_verdict) <= {"(a)", "(b)"} else 1


if __name__ == "__main__":
    sys.exit(main())
