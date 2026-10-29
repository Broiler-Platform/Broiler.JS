#!/usr/bin/env python3
"""Generate the time-zone area of the shared conformance corpus.

    python3 -I scripts/conformance/generate_time_zone_vectors.py --out conformance/vectors/time-zones

Every expected value comes from `tzref.py` (`ref:tzref@1`), the corpus's independent reference computation of the
area: the pinned tzdb release compiled by the host's `zic -b fat`, read by its own TZif reader and POSIX TZ string
evaluator, under decision SJL-D-20's policy, with the specification's zone operations written from their text. No
expected value is taken from either engine, from the data home's generator or from `Broiler.ECMAScript.Algorithms`.

The populations are milestone SJL-8.4's: transition and offset vectors for the zones whose history the policy fixes,
at instants on both sides of every listed policy difference and correction of JSD-0076 and its correspondence from
code (Broiler.VM's `docs/evidence/sjl-8-001/correspondence.md`, whose rows the notes name):

- the release (rows Z1 and Z2): every zone whose rules changed between tzdb 2025b and 2026e, as IANA's NEWS in the
  pinned tarball states them, around each change;
- backzone (row Z3): the 110 names the correspondence finds backzone changes before 1970, before and after 1970;
- extrapolation after the last listed transition (rows Z4, A5, A7, A10): northern and southern zones with recurring
  rules, either side of 2037 and far after it, and zones whose last listed transition is before 2037;
- the identifiers: every one by its own case (Z6), `Factory` absent (Z5), CLDR primaries against tzdb's link targets
  (Z7), the formatter's resolved name (Z8, implementation-defined), and the regions (Z12, implementation-defined);
- lookup by ASCII case only (Z9, Z10), ISO strings refused as the formatter's zone (Z11), offset identifiers (A12),
  with non-ASCII digits refused (A13), `Etc/GMT` zones (A14) and unknown names (A11);
- and a modest sample of ordinary transitions, gaps and overlaps (A1, A3, A4, A6, A9).

**Classes.** ECMA-262 requires a time zone aware implementation to use the IANA Time Zone Database's names and rules
(21.4.1.19, 21.4.1.20); which release and which build is the data pin's, as the Unicode area's version is: tzdb 2026e's
default build without backzone or `factory`, CLDR 48.2.0's primaries, under SJL-D-20, which both engines' plans
commit them to. So every vector whose outcome follows from that data and the specification's algorithms is
`normative`. Two are not: the name an `Intl.DateTimeFormat` resolves a non-primary identifier to (row Z8, which
JSD-0076 leaves each engine's until the specification edition both pin settles it), and the zones a region uses
(row Z12, which the Intl Locale Info proposal leaves to the implementation); both are `impl-defined`, with each
engine's expectation from its plan.

The output is deterministic: the same inputs give the same bytes. Vector identifiers are stable and are never reused;
a later revision appends.
"""

from __future__ import annotations

import argparse
import math
import struct
import sys
from collections import defaultdict
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import tzref as T  # noqa: E402
from generate_number_vectors import js_string_body, string_value  # noqa: E402

# The corpus revision this generator writes, and the revision its vectors were added in. Revision 22 classifies every
# difference of Broiler.JS's first engine-level runs on revision 21, under UTC and America/New_York
# (time-zone-divergences.txt); no expected value changes. Revision 23 retires every one of them, which Broiler.JS's
# cut-over, milestone SJL-8.6, corrects. Revision 24 adds the Temporal area (generate_temporal_vectors.py) and
# changes nothing here, nor does revision 25, which classifies its differences, nor revision 26, which retires them.
CORPUS_REVISION = 30
FIRST_REVISION = 21
GENERATOR = T.GENERATOR
SPEC = "spec:es2026@0248456c"
ENGINES = ("broiler-js", "broiler-vm")
DIVERGENCES = Path(__file__).resolve().parent / "time-zone-divergences.txt"

# The clauses of the pinned ECMA-262 each kind of vector rests on. The Temporal proposal's operations and ECMA-402
# are not pinned: where a vector's outcome is theirs, its provenance is the reference computation, which follows them.
IDENTIFIERS = ("sec-time-zone-identifiers",)
OFFSET = IDENTIFIERS + ("sec-getnamedtimezoneoffsetnanoseconds",)
EPOCH = IDENTIFIERS + ("sec-getnamedtimezoneepochnanoseconds",)
RECORDS = IDENTIFIERS + ("sec-time-zone-identifier-record", "sec-availablenamedtimezoneidentifiers")
OFFSET_IDENTIFIER = ("sec-time-zone-identifiers", "sec-time-zone-offset-strings")
FORMATTER = ("sec-istimezoneoffsetstring", "sec-time-zone-offset-strings", "sec-availablenamedtimezoneidentifiers")

Z8_PLANS = ("row Z8: implementation-defined until the specification edition both engines pin settles it (JSD-0076, X2); "
            "out is the identifier, as the canonical-tz text has it. Broiler.JS keeps the name (its "
            "docs/roadmap/SharedLibraries.md, record of SJL-8.1); Broiler.VM reports the primary (JSD-0054, JSD-0076 Z8)")
Z12_PLANS = ("row Z12: the Intl Locale Info proposal's TimeZonesOfLocale leaves the zones in common use to the "
             "implementation; both plans answer zone.tab's zones of the region, ordinal (JSD-0076 X2 and the adapter "
             "guide's RegionTimeZones; Broiler.JS's record of SJL-8.1 lists Z12 among its corrections)")

FILE_COMMENTS = {
    "release": [
        "# Rows Z1 and Z2: the zones whose rules changed between tzdb 2025b and 2026e, as the NEWS file of the pinned",
        "# tarball states them (2025c Baja California; 2026a Moldova; 2026b British Columbia; 2026c Alberta and",
        "# Morocco; 2026d Northwest Territories, Colombia 1992 and Iran 1979; 2026e Manitoba and Ireland 1925),",
        "# at each transition of 2026e in a window around the change and the second before it, mid-January and",
        "# mid-July of each year, and the next and previous transitions from the first of January and of July.",
    ],
    "backzone": [
        "# Row Z3: the 110 names whose history backzone changes before 1970 (the correspondence's",
        "# correspondence-runs/tz/z10-transitions-compare.txt), read in the default build without backzone: offsets",
        "# before 1970 and in 1980, their first transition, their last before 1970, and how many fall between.",
    ],
    "extrapolation": [
        "# Rows Z4, A5, A7 and A10: after the last listed transition (2037 in zic's fat output) a zone's recurring",
        "# rules continue without limit. Zones with recurring rules, north and south, either side of 2037 and far",
        "# after it; zones whose last listed transition is before 2037 hold their last offset.",
    ],
    "identifiers": [
        "# Rows Z5 to Z8 and Z12: every identifier of the default build, by its own case; Factory, which the build",
        "# does not compile; CLDR 48.2.0's primaries against tzdb's link targets; the primary identifiers listed;",
        "# the name a formatter resolves (Z8, implementation-defined); and the zones of a region (Z12).",
    ],
    "lookup": [
        "# Rows Z9 to Z11, A11 to A14: lookup by ASCII case only, so a non-ASCII case fold matches nothing; an ISO",
        "# date-time string is no formatter's time zone; offset identifiers, whose digits are ASCII; Etc/GMT zones;",
        "# unknown names.",
    ],
    "transitions": [
        "# Rows A1 and A9: offsets and next and previous transitions in ordinary zones, in 2026 and at the edges",
        "# of their histories: a change of the UTC offset is a transition, a change of the daylight flag alone is not.",
    ],
    "wall-times": [
        "# Rows A3, A4 and A6: the instants a wall-clock time names, each disambiguation, start of day and the hours",
        "# in a day, across gaps and overlaps of 30 minutes, an hour, 24 hours and 44 minutes 30 seconds, and",
        "# where midnight is skipped.",
    ],
}


def clause(*ids: str) -> str:
    return " ".join(f"{SPEC}#{i}" for i in ids)


def prov(clauses: tuple[str, ...] = ()) -> str:
    return " ".join(part for part in (clause(*clauses), GENERATOR) if part)


def bits(x: float) -> int:
    return struct.unpack(">Q", struct.pack(">d", x))[0]


def number_out(x: float) -> str:
    return "value: f64:0x7FF8000000000000" if math.isnan(x) else f"value: f64:0x{bits(x):016X}"


def str_out(s: str) -> str:
    return f"value: {string_value(s)}"


def bool_out(b: bool) -> str:
    return f"value: bool:{'true' if b else 'false'}"


def bigint_out(n: int | None) -> str:
    return "value: null" if n is None else f"value: bigint:{n}"


def sv(s: str) -> str:
    return string_value(s)


def lit(s: str) -> str:
    """A JavaScript string literal, ASCII only: every other code unit is \\uXXXX."""
    return f'"{js_string_body(s)}"'


def E(text: str) -> int:
    """The epoch nanoseconds of `[±YY]YYYY-MM-DD[THH:MM[:SS]]Z`."""
    return T.epoch_seconds(text) * T.NS_PER_SECOND


def iso(ns: int) -> str:
    """An instant as an ISO text, for notes."""
    seconds = ns // T.NS_PER_SECOND
    y, m, d = T.civil_from_days(seconds // 86400)
    r = seconds % 86400
    year = f"{y:04d}" if 0 <= y <= 9999 else f"{'+' if y > 0 else '-'}{abs(y):06d}"
    fraction = ns % T.NS_PER_SECOND
    return f"{year}-{m:02d}-{d:02d}T{r // 3600:02d}:{r % 3600 // 60:02d}:{r % 60:02d}" + (
        f".{fraction:09d}".rstrip("0") if fraction else "") + "Z"


# --------------------------------------------------------------------------------------------------
# Records.


class Corpus:
    def __init__(self, db: T.Database) -> None:
        self.db = db
        self.files: dict[str, list[dict[str, str]]] = {}
        self.ids: set[str] = set()
        self.counters: dict[str, int] = {}
        self.seen: set[tuple[str, str]] = set()

    def add(self, file: str, group: str, record: dict[str, str], since: int = FIRST_REVISION) -> None:
        # One vector per operation and input across the area: the first group to ask for it keeps it.
        key = (record["op"], record["in"])
        if key in self.seen:
            return
        self.seen.add(key)
        self.counters[group] = self.counters.get(group, 0) + 1
        vector_id = f"time-zones/{group}/{self.counters[group]:04d}"
        assert vector_id not in self.ids
        self.ids.add(vector_id)
        full = {"id": vector_id, "area": "time-zones"}
        full.update(record)
        full.setdefault("class", "normative")
        full["since"] = str(since)
        for k, value in full.items():
            assert "\n" not in value and value == value.strip(), (k, value)
        assert all(ord(ch) < 0x80 for ch in full["script"]), full["script"]
        self.files.setdefault(file, []).append(full)

    def classify(self, path: Path) -> None:
        """Applies time-zone-divergences.txt, as the other generators apply their own files; revision 21 had none,
        revision 22 adds it, and revision 23 retires every line of it."""
        if not path.exists():
            return
        reasons: dict[str, str] = {}
        by_id = {record["id"]: record for records in self.files.values() for record in records}
        for line_number, line in enumerate(path.read_text(encoding="utf-8").splitlines(), 1):
            if not line or line.startswith("#"):
                continue
            if line.startswith("reason "):
                key, _, reason = line[len("reason "):].partition(": ")
                if not reason or key in reasons:
                    raise SystemExit(f"{path.name}:{line_number}: a malformed or repeated reason")
                reasons[key] = reason
                continue
            engine, key, vector_id = line.split(" ")
            record = by_id.get(vector_id)
            if engine not in ENGINES or key not in reasons or record is None:
                raise SystemExit(f"{path.name}:{line_number}: '{line}' names no engine, reason or vector")
            if f"divergence.{engine}" in record or (
                    record["class"] == "impl-defined" and f"per-engine.{engine}" not in record):
                raise SystemExit(f"{path.name}:{line_number}: {vector_id} cannot take this classification")
            if record["class"] == "normative":
                record["class"] = "divergence"
            record[f"divergence.{engine}"] = reasons[key]

    def write(self, out: Path) -> None:
        out.mkdir(parents=True, exist_ok=True)
        order = ["id", "area", "op", "in", "out", "class"]
        for name, records in sorted(self.files.items()):
            lines = [
                "# Generated by scripts/conformance/generate_time_zone_vectors.py; do not edit by hand.",
                "# Expected values: scripts/conformance/tzref.py, over tzdb 2026e compiled by zic under SJL-D-20.",
                *FILE_COMMENTS.get(name, []),
                "schema: 1",
                "",
            ]
            for record in records:
                keys = order + sorted(k for k in record if k.startswith("per-engine.")) + sorted(
                    k for k in record if k.startswith("divergence.")) + ["prov", "script"] + (
                    ["note"] if "note" in record else []) + ["since"]
                for key in keys:
                    if key in record:
                        lines.append(f"{key}: {record[key]}")
                lines.append("")
            (out / f"{name}.vectors").write_text("\n".join(lines), encoding="utf-8", newline="\n")

    # ----------------------------------------------------------------------------------------------
    # The kinds of vector. Each computes its outcome through tzref, from the zone's identifier as the script gives it.

    def zone(self, name: str) -> T.TimeZone:
        return T.time_zone_from_identifier(self.db, name)

    def offset(self, file: str, group: str, name: str, ns: int, note: str) -> None:
        self.add(file, group, {
            "op": "GetOffsetNanosecondsFor",
            "in": f"{sv(name)} bigint:{ns}",
            "out": number_out(float(T.offset_ns(self.zone(name), ns))),
            "prov": prov(OFFSET),
            "script": f"Temporal.Instant.fromEpochNanoseconds({ns}n).toZonedDateTimeISO({lit(name)}).offsetNanoseconds",
            "note": f"{note}; at {iso(ns)}",
        })

    def transition(self, file: str, group: str, name: str, ns: int, direction: str, note: str) -> None:
        find = T.next_transition if direction == "next" else T.previous_transition
        self.add(file, group, {
            "op": "GetNamedTimeZoneNextTransition" if direction == "next" else "GetNamedTimeZonePreviousTransition",
            "in": f"{sv(name)} bigint:{ns}",
            "out": bigint_out(find(self.zone(name), ns)),
            "prov": prov(OFFSET),
            "script": (f"var t = Temporal.Instant.fromEpochNanoseconds({ns}n).toZonedDateTimeISO({lit(name)})"
                       f".getTimeZoneTransition(\"{direction}\"); t === null ? null : t.epochNanoseconds"),
            "note": f"{note}; the {direction} transition from {iso(ns)}",
        })

    def count(self, file: str, group: str, name: str, start: int, end: int, note: str) -> None:
        tz = self.zone(name)
        n, at = 0, start
        while (at := T.next_transition(tz, at)) is not None and at < end:
            n += 1
        self.add(file, group, {
            "op": "GetNamedTimeZoneNextTransition, count",
            "in": f"{sv(name)} bigint:{start} bigint:{end}",
            "out": number_out(float(n)),
            "prov": prov(OFFSET),
            "script": (f"var z = Temporal.Instant.fromEpochNanoseconds({start}n).toZonedDateTimeISO({lit(name)}); "
                       f"var n = 0; while ((z = z.getTimeZoneTransition(\"next\")) !== null && z.epochNanoseconds < "
                       f"{end}n) n++; n"),
            "note": f"{note}; the transitions after {iso(start)} and before {iso(end)}",
        })

    def possible(self, file: str, group: str, name: str, wall: tuple[int, ...], note: str) -> None:
        y, mo, d, h, mi, s = wall
        found = T.possible_epoch_ns(self.zone(name), y, mo, d, h, mi, s)
        self.add(file, group, {
            "op": "GetPossibleEpochNanoseconds",
            "in": f"{sv(name)} int:{y} int:{mo} int:{d} int:{h} int:{mi} int:{s}",
            "out": str_out(",".join(str(n) for n in found)),
            "prov": prov(EPOCH),
            "script": (f"var w = new Temporal.PlainDateTime({y}, {mo}, {d}, {h}, {mi}, {s}); var r = []; "
                       f"[\"earlier\", \"later\"].forEach(function (m) {{ var z = w.toZonedDateTime({lit(name)}, "
                       f"{{ disambiguation: m }}); var n = String(z.epochNanoseconds); "
                       f"if (z.toPlainDateTime().equals(w) && r.indexOf(n) < 0) r.push(n); }}); r.join()"),
            "note": f"{note}; the instants the wall time names, through the two that earlier and later pick (at most two in the data)",
        })

    def disambiguation(self, file: str, group: str, name: str, wall: tuple[int, ...], mode: str, note: str) -> None:
        y, mo, d, h, mi, s = wall
        try:
            out = bigint_out(T.epoch_ns_for(self.zone(name), y, mo, d, h, mi, s, mode=mode))
        except T.RangeError:
            out = "error: RangeError"
        self.add(file, group, {
            "op": "GetEpochNanosecondsFor",
            "in": f"{sv(name)} int:{y} int:{mo} int:{d} int:{h} int:{mi} int:{s} {sv(mode)}",
            "out": out,
            "prov": prov(EPOCH),
            "script": (f"Temporal.ZonedDateTime.from({{ year: {y}, month: {mo}, day: {d}, hour: {h}, minute: {mi}, "
                       f"second: {s}, timeZone: {lit(name)} }}, {{ disambiguation: {lit(mode)} }}).epochNanoseconds"),
            "note": f"{note}; disambiguation {mode}",
        })

    def wall_time(self, file: str, group: str, name: str, wall: tuple[int, ...], note: str) -> None:
        self.possible(file, group, name, wall, note)
        for mode in T.MODES:
            self.disambiguation(file, group, name, wall, mode, note)

    def start_of_day(self, file: str, group: str, name: str, date: tuple[int, int, int], note: str) -> None:
        y, mo, d = date
        tz = self.zone(name)
        try:
            out = bigint_out(T.start_of_day(tz, y, mo, d))
        except T.RangeError:
            out = "error: RangeError"
        self.add(file, group, {
            "op": "GetStartOfDay",
            "in": f"{sv(name)} int:{y} int:{mo} int:{d}",
            "out": out,
            "prov": prov(EPOCH),
            "script": f"new Temporal.PlainDate({y}, {mo}, {d}).toZonedDateTime({lit(name)}).epochNanoseconds",
            "note": f"{note}; the start of {y:04d}-{mo:02d}-{d:02d}",
        })
        # hoursInDay is that of the day the start falls in, which is the next day where the whole day is skipped.
        start = T.start_of_day(tz, y, mo, d)
        self.add(file, group, {
            "op": "Temporal.ZonedDateTime.prototype.hoursInDay",
            "in": f"{sv(name)} int:{y} int:{mo} int:{d}",
            "out": number_out(T.hours_in_day(tz, *T.local_date(tz, start))),
            "prov": prov(EPOCH),
            "script": f"new Temporal.PlainDate({y}, {mo}, {d}).toZonedDateTime({lit(name)}).hoursInDay",
            "note": f"{note}; the hours in the day that starts the start of {y:04d}-{mo:02d}-{d:02d}",
        })

    def start_of_day_from(self, file: str, group: str, name: str, ns: int, note: str) -> None:
        tz = self.zone(name)
        self.add(file, group, {
            "op": "Temporal.ZonedDateTime.prototype.startOfDay",
            "in": f"{sv(name)} bigint:{ns}",
            "out": bigint_out(T.start_of_day(tz, *T.local_date(tz, ns))),
            "prov": prov(OFFSET[1:] + EPOCH),
            "script": f"Temporal.Instant.fromEpochNanoseconds({ns}n).toZonedDateTimeISO({lit(name)}).startOfDay().epochNanoseconds",
            "note": f"{note}; the start of the day of {iso(ns)}",
        })

    def zone_id(self, file: str, group: str, text: str, note: str, through_instant: bool = False,
                clauses: tuple[str, ...] | None = None) -> None:
        try:
            tz = T.to_temporal_time_zone_identifier(self.db, text) if through_instant else \
                T.time_zone_from_identifier(self.db, text)
            out = str_out(tz.identifier)
        except T.RangeError:
            out = "error: RangeError"
        if clauses is None:
            clauses = OFFSET_IDENTIFIER if T.parse_utc_offset(text, True) is not None else RECORDS
        self.add(file, group, {
            "op": "Temporal.Instant.prototype.toZonedDateTimeISO, timeZoneId" if through_instant else
            "Temporal.ZonedDateTime, timeZoneId",
            "in": sv(text),
            "out": out,
            "prov": prov(clauses),
            "script": (f"Temporal.Instant.fromEpochNanoseconds(0n).toZonedDateTimeISO({lit(text)}).timeZoneId"
                       if through_instant else f"new Temporal.ZonedDateTime(0n, {lit(text)}).timeZoneId"),
            "note": note,
        })

    def equals(self, file: str, group: str, one: str, two: str, note: str) -> None:
        a = T.time_zone_from_identifier(self.db, one).identifier
        b = T.time_zone_from_identifier(self.db, two).identifier
        self.add(file, group, {
            "op": "TimeZoneEquals",
            "in": f"{sv(one)} {sv(two)}",
            "out": bool_out(T.time_zone_equals(self.db, a, b)),
            "prov": prov(RECORDS),
            "script": f"new Temporal.ZonedDateTime(0n, {lit(one)}).equals(new Temporal.ZonedDateTime(0n, {lit(two)}))",
            "note": note,
        })

    def formatter_zone(self, file: str, group: str, text: str, note: str) -> None:
        record = {
            "op": "Intl.DateTimeFormat, resolved timeZone",
            "in": sv(text),
            "prov": prov(FORMATTER),
            "script": f"new Intl.DateTimeFormat(\"en\", {{ timeZone: {lit(text)} }}).resolvedOptions().timeZone",
            "note": note,
        }
        try:
            identifier, primary = T.formatter_time_zone(self.db, text)
        except T.RangeError:
            record["out"] = "error: RangeError"
        else:
            record["out"] = str_out(identifier)
            if identifier != primary:
                record["class"] = "impl-defined"
                record["per-engine.broiler-js"] = str_out(identifier)
                record["per-engine.broiler-vm"] = str_out(primary)
                record["note"] = f"{note}; {Z8_PLANS}"
        self.add(file, group, record)

    def formatter_hour(self, file: str, group: str, name: str, ns: int, note: str) -> None:
        ms = ns // 1_000_000
        assert ms * 1_000_000 == ns
        self.add(file, group, {
            "op": "Intl.DateTimeFormat, hour",
            "in": f"{sv(name)} int:{ms}",
            "out": number_out(float(T.local_hour(self.zone(name), ns))),
            "prov": prov(OFFSET),
            "script": (f"var p = new Intl.DateTimeFormat(\"en\", {{ timeZone: {lit(name)}, hour: \"numeric\", "
                       f"hourCycle: \"h23\", numberingSystem: \"latn\" }}).formatToParts(new Date({ms})); var h = null; "
                       f"for (var i = 0; i < p.length; i++) if (p[i].type === \"hour\") h = parseInt(p[i].value, 10); h"),
            "note": f"{note}; the hour of {iso(ns)} on a 23-hour clock in Latin digits, its padding aside",
        })

    def supported(self, file: str, group: str, kind: str, text: str, note: str) -> None:
        primaries = self.db.primaries
        if kind == "list":
            op, out, script = ("AvailablePrimaryTimeZoneIdentifiers", str_out(",".join(primaries)),
                               "Intl.supportedValuesOf(\"timeZone\").join()")
        elif kind == "count":
            op, out, script = ("AvailablePrimaryTimeZoneIdentifiers, count", number_out(float(len(primaries))),
                               "Intl.supportedValuesOf(\"timeZone\").length")
        else:
            op, out, script = ("AvailablePrimaryTimeZoneIdentifiers, includes", bool_out(text in primaries),
                               f"Intl.supportedValuesOf(\"timeZone\").indexOf({lit(text)}) !== -1")
        self.add(file, group, {"op": op, "in": sv(text), "out": out, "prov": prov(RECORDS), "script": script,
                               "note": note})

    def region(self, file: str, group: str, locale: str) -> None:
        zones = self.db.region_zones(locale.split("-")[1])
        out = str_out(",".join(zones))
        self.add(file, group, {
            "op": "Intl.Locale.prototype.getTimeZones",
            "in": sv(locale),
            "out": out,
            "class": "impl-defined",
            "per-engine.broiler-js": out,
            "per-engine.broiler-vm": out,
            "prov": prov(),
            "script": f"new Intl.Locale({lit(locale)}).getTimeZones().join()",
            "note": f"the zones of {locale.split('-')[1]}; {Z12_PLANS}",
        })


# --------------------------------------------------------------------------------------------------
# Helpers over the reference's zones.

def changes(corpus: Corpus, name: str, start: int, end: int) -> list[int]:
    """Every instant, in seconds, at which the zone's UTC offset changes in [start, end)."""
    zone = corpus.zone(name).zone
    out, at = [], start - 1
    while (at := zone.next_change(at, end - 1)) is not None:
        out.append(at)
    return out


def year_start(year: int) -> int:
    return T.days_from_civil(year, 1, 1) * 86400


def mid(year: int, month: int, day: int = 15) -> int:
    return (T.days_from_civil(year, month, day) * 86400 + 43200) * T.NS_PER_SECOND


def first(year: int, month: int) -> int:
    return T.days_from_civil(year, month, 1) * 86400 * T.NS_PER_SECOND


def both_sides(corpus: Corpus, file: str, group: str, name: str, first_year: int, last_year: int, note: str) -> None:
    """The offset at every transition of the window and the second before it."""
    for at in changes(corpus, name, year_start(first_year), year_start(last_year + 1)):
        for seconds in (at - 1, at):
            corpus.offset(file, group, name, seconds * T.NS_PER_SECOND, note)


def window(corpus: Corpus, file: str, group: str, name: str, first_year: int, last_year: int, note: str,
           transitions: bool = True) -> None:
    """Both sides of every transition of a window of years, mid-January and mid-July of each year, and the next and
    previous transitions from the first of January and of July; for a name that reads another's zone, the offsets in
    mid-January and mid-July alone."""
    if transitions:
        both_sides(corpus, file, group, name, first_year, last_year, note)
    for year in range(first_year, last_year + 1):
        for month in (1, 7):
            corpus.offset(file, group, name, mid(year, month), note)
        if transitions:
            for month in (1, 7):
                corpus.transition(file, group, name, first(year, month), "next", note)
                corpus.transition(file, group, name, first(year, month), "previous", note)


# --------------------------------------------------------------------------------------------------
# Rows Z1 and Z2: the release. Each change is NEWS's, between 2025b and 2026e, with the window of years around it.

RELEASE_CHANGES = [
    ("America/Tijuana", ("America/Ensenada", "America/Santa_Isabel", "Mexico/BajaNorte"), [(1952, 1954), (1960, 1976)],
     (1953, 1953), "2025c: Baja California agreed with California's DST rules in 1953 and in 1961 through 1975"),
    ("Europe/Chisinau", ("Europe/Tiraspol",), [(2021, 2027)], (2022, 2022),
     "2026a: since 2022 Moldova has observed EU transition times, springing forward at 03:00 and falling back at 04:00"),
    ("America/Vancouver", ("Canada/Pacific",), [(2025, 2028)], (2026, 2026),
     "2026b: British Columbia moved to permanent -07, modelled from 2026-11-01 02:00"),
    ("America/Edmonton", ("Canada/Mountain", "America/Yellowknife"), [(2025, 2028)], (2026, 2026),
     "2026c: Alberta moved to permanent -06, modelled from 2026-11-01 02:00"),
    ("Africa/Casablanca", (), [(2025, 2028)], None, "2026c: Morocco moves to permanent +00 on 2026-09-20"),
    ("Africa/El_Aaiun", (), [(2025, 2028)], None, "2026c: Morocco's change also affects Western Sahara"),
    ("America/Inuvik", (), [(2025, 2028)], None,
     "2026d: Northwest Territories stays on -06 year-round, modelled from 2026-11-01 02:00"),
    ("America/Bogota", (), [(1991, 1993)], None, "2026d: Colombia's 1992-05-02 spring forward was at 00:00, not 24:00"),
    ("Asia/Tehran", ("Iran",), [(1978, 1980)], (1979, 1979),
     "2026d: Iran's 1979-05-26 spring forward was at 00:00, not 24:00"),
    ("America/Winnipeg", ("Canada/Central", "America/Rainy_River"), [(2025, 2028)], (2026, 2026),
     "2026e: Manitoba moves to permanent -05, modelled from 2026-11-01 02:00"),
    ("Europe/Dublin", ("Eire",), [(1924, 1926)], (1925, 1925), "2026e: in 1925 Ireland fell back on 09-20, not 10-04"),
]

# The correspondence's own cells of row Z1, each at its instant.
Z1_OFFSETS = [("America/Vancouver", "2027-01-15T12:00"), ("America/Edmonton", "2027-01-15T12:00"),
              ("America/Winnipeg", "2027-01-15T12:00"), ("America/Inuvik", "2027-01-15T12:00"),
              ("Africa/Casablanca", "2027-01-15T12:00"), ("Europe/Chisinau", "2026-03-29T00:30"),
              ("Europe/Chisinau", "2026-10-25T00:30"), ("Europe/Dublin", "1925-09-25T12:00"),
              ("America/Bogota", "1992-05-02T12:00"), ("America/Vancouver", "2026-07-01T12:00"),
              ("Africa/Casablanca", "2026-07-01T12:00")]
Z1_TRANSITIONS = [("America/Vancouver", "2026-07-01T12:00"), ("Africa/Casablanca", "2026-10-01T12:00")]


def release(corpus: Corpus) -> None:
    for name, aliases, windows, focus, change in RELEASE_CHANGES:
        note = f"rows Z1 and Z2, tzdb {change}"
        for first_year, last_year in windows:
            window(corpus, "release", "release", name, first_year, last_year, note)
        for alias in aliases:
            alias_note = f"{note}; {alias} reads {corpus.db.primary(alias)}'s zone"
            both_sides(corpus, "release", "release", alias, focus[0], focus[1], alias_note)
            for first_year, last_year in windows:
                window(corpus, "release", "release", alias, first_year, last_year, alias_note, transitions=False)
    for name, at in Z1_OFFSETS:
        corpus.offset("release", "release", name, E(at), "row Z1's cell")
    for name, at in Z1_TRANSITIONS:
        corpus.transition("release", "release", name, E(at), "next", "row Z1's cell")


# --------------------------------------------------------------------------------------------------
# Row Z3: backzone. The 110 names of correspondence-runs/tz/z10-transitions-compare.txt that differ only before
# 1970, but Europe/Dublin and Eire, which differ by the release (Z1).

BACKZONE_NAMES = [
    "Africa/Accra", "Africa/Addis_Ababa", "Africa/Asmara", "Africa/Asmera", "Africa/Bamako", "Africa/Bangui",
    "Africa/Banjul", "Africa/Blantyre", "Africa/Brazzaville", "Africa/Bujumbura", "Africa/Conakry", "Africa/Dakar",
    "Africa/Dar_es_Salaam", "Africa/Djibouti", "Africa/Douala", "Africa/Freetown", "Africa/Gaborone", "Africa/Harare",
    "Africa/Kampala", "Africa/Kigali", "Africa/Kinshasa", "Africa/Libreville", "Africa/Lome", "Africa/Luanda",
    "Africa/Lubumbashi", "Africa/Lusaka", "Africa/Malabo", "Africa/Maseru", "Africa/Mbabane", "Africa/Mogadishu",
    "Africa/Niamey", "Africa/Nouakchott", "Africa/Ouagadougou", "Africa/Porto-Novo", "Africa/Timbuktu",
    "America/Anguilla", "America/Antigua", "America/Aruba", "America/Atikokan", "America/Blanc-Sablon",
    "America/Cayman", "America/Coral_Harbour", "America/Creston", "America/Curacao", "America/Dominica",
    "America/Grenada", "America/Guadeloupe", "America/Kralendijk", "America/Lower_Princes", "America/Marigot",
    "America/Montserrat", "America/Nassau", "America/Port_of_Spain", "America/St_Barthelemy", "America/St_Kitts",
    "America/St_Lucia", "America/St_Thomas", "America/St_Vincent", "America/Tortola", "America/Virgin",
    "Antarctica/DumontDUrville", "Antarctica/McMurdo", "Antarctica/South_Pole", "Antarctica/Syowa",
    "Arctic/Longyearbyen", "Asia/Aden", "Asia/Bahrain", "Asia/Brunei", "Asia/Kuala_Lumpur", "Asia/Kuwait",
    "Asia/Muscat", "Asia/Phnom_Penh", "Asia/Vientiane", "Atlantic/Jan_Mayen", "Atlantic/Reykjavik",
    "Atlantic/St_Helena", "Europe/Amsterdam", "Europe/Copenhagen", "Europe/Guernsey", "Europe/Isle_of_Man",
    "Europe/Jersey", "Europe/Ljubljana", "Europe/Luxembourg", "Europe/Monaco", "Europe/Oslo", "Europe/Sarajevo",
    "Europe/Skopje", "Europe/Stockholm", "Europe/Vaduz", "Europe/Zagreb", "Iceland", "Indian/Antananarivo",
    "Indian/Christmas", "Indian/Cocos", "Indian/Comoro", "Indian/Kerguelen", "Indian/Mahe", "Indian/Mayotte",
    "Indian/Reunion", "Pacific/Chuuk", "Pacific/Funafuti", "Pacific/Majuro", "Pacific/Midway", "Pacific/Pohnpei",
    "Pacific/Ponape", "Pacific/Saipan", "Pacific/Truk", "Pacific/Wake", "Pacific/Wallis", "Pacific/Yap",
]

# Row Z3's own cells: Amsterdam and Reykjavik, each at its instants.
Z3_CELLS = [("Europe/Amsterdam", "1850-01-01T00:00"), ("Europe/Amsterdam", "1937-07-01T12:00"),
            ("Europe/Amsterdam", "1939-01-15T12:00"), ("Atlantic/Reykjavik", "1900-01-01T00:00"),
            ("Atlantic/Reykjavik", "1960-01-15T12:00")]


def backzone(corpus: Corpus) -> None:
    assert len(BACKZONE_NAMES) == 110 and len(set(BACKZONE_NAMES)) == 110
    for name in BACKZONE_NAMES:
        note = f"row Z3, without backzone {name} reads {corpus.db.primary(name)}'s zone, which is " \
               f"{corpus.db.links.get(name, name)}'s in the default build"
        for at in ("1800-01-01T00:00", "1940-07-01T12:00", "1960-01-15T12:00", "1980-07-01T12:00"):
            corpus.offset("backzone", "backzone", name, E(at), note)
        corpus.transition("backzone", "backzone", name, E("1800-01-01T00:00"), "next", note)
        corpus.transition("backzone", "backzone", name, E("1970-01-01T00:00"), "previous", note)
        corpus.count("backzone", "backzone", name, E("1800-01-01T00:00"), E("1970-01-01T00:00"), note)
    for name, at in Z3_CELLS:
        corpus.offset("backzone", "backzone", name, E(at), "row Z3's cell")


# --------------------------------------------------------------------------------------------------
# Rows Z4, A5, A7 and A10: extrapolation after the last listed transition.

# Zones whose rules recur after 2037, north and south, with the unusual ones: 30-minute daylight time (Lord Howe),
# negative daylight time (Dublin), rules at negative hours (Nuuk) and at 24:00 (Santiago), two hours (Troll).
RECURRING = [
    "America/New_York", "America/Chicago", "America/Denver", "America/Los_Angeles", "America/Anchorage",
    "America/Halifax", "America/St_Johns", "America/Havana", "America/Nuuk", "America/Miquelon", "Atlantic/Azores",
    "Europe/London", "Europe/Dublin", "Europe/Lisbon", "Europe/Berlin", "Europe/Paris", "Europe/Athens",
    "Africa/Cairo", "Asia/Beirut", "Asia/Jerusalem", "Antarctica/Troll", "America/Santiago", "Pacific/Easter",
    "Australia/Sydney", "Australia/Adelaide", "Australia/Hobart", "Australia/Lord_Howe", "Pacific/Auckland",
    "Pacific/Chatham", "Pacific/Norfolk",
]
# Zones whose last listed transition is before 2037 and which hold their last offset.
SETTLED = [
    "Asia/Riyadh", "Asia/Tokyo", "Asia/Kolkata", "America/Phoenix", "Pacific/Honolulu", "Asia/Shanghai",
    "Asia/Kathmandu", "Pacific/Kiritimati", "Africa/Johannesburg", "America/Sao_Paulo", "America/Asuncion",
    "Asia/Tehran", "Europe/Moscow", "America/Mexico_City", "Europe/Istanbul", "UTC",
]


def extrapolation(corpus: Corpus) -> None:
    file = "extrapolation"
    for name in RECURRING:
        zone = corpus.zone(name).zone
        assert zone.footer_changes and zone.last_change is not None and zone.last_change < year_start(2038), name
        note = (f"row Z4, {name}'s recurring rules ({zone.footer.text}) after its last listed transition, "
                f"{iso(zone.last_change * T.NS_PER_SECOND)}")
        for year in (2037, 2040, 2400):
            for month in (1, 7):
                corpus.offset(file, "extrapolation-offset", name, mid(year, month), note)
        for year in (2037, 2040):
            both_sides(corpus, file, "extrapolation-offset", name, year, year, note)
        corpus.offset(file, "extrapolation-offset", name, mid(9999, 7), note)
        note = note.replace("row Z4", "row A10")
        corpus.transition(file, "extrapolation-transition", name, E("2037-12-31T00:00"), "next", note)
        corpus.transition(file, "extrapolation-transition", name, E("2040-12-31T00:00"), "previous", note)
        corpus.transition(file, "extrapolation-transition", name, E("+275760-09-01T00:00"), "previous", note)
    for name in SETTLED:
        zone = corpus.zone(name).zone
        assert not zone.footer_changes and (zone.last_change is None or zone.last_change < year_start(2037)), name
        note = f"rows Z4 and A10, {name} keeps its last offset after its last listed transition" + (
            f", {iso(zone.last_change * T.NS_PER_SECOND)}" if zone.last_change is not None else "")
        for year in (2037, 2040, 2400):
            corpus.offset(file, "extrapolation-offset", name, mid(year, 7), note)
        corpus.transition(file, "extrapolation-transition", name, E("2037-01-01T00:00"), "next", note)
        corpus.transition(file, "extrapolation-transition", name, E("2040-07-01T00:00"), "previous", note)
    # Row A10's cells.
    for name, at, direction in [("America/New_York", "2037-12-01T12:00", "next"),
                                ("America/New_York", "2040-07-01T12:00", "previous"),
                                ("America/New_York", "2100-01-01T00:00", "previous"),
                                ("America/New_York", "+275760-09-01T00:00", "previous"),
                                ("America/Santiago", "2037-12-01T12:00", "next"),
                                ("Europe/Berlin", "2040-07-01T12:00", "next"),
                                ("America/New_York", "2037-07-01T12:00", "next")]:
        corpus.transition(file, "extrapolation-transition", name, E(at), direction, "row A10's cell")
    # Row Z4's cells.
    for name, at in [("America/New_York", "2040-07-01T12:00"), ("Europe/Berlin", "2040-07-01T12:00"),
                     ("America/New_York", "2400-07-01T12:00"), ("America/Santiago", "2040-07-01T12:00"),
                     ("Australia/Sydney", "2040-07-01T12:00"), ("America/New_York", "2037-07-01T12:00"),
                     ("America/New_York", "+275760-09-13T00:00")]:
        corpus.offset(file, "extrapolation-offset", name, E(at), "row Z4's cell")
    # Row A5: gaps and overlaps after 2037.
    walls = [("America/New_York", (2040, 3, 11, 2, 30, 0)), ("America/New_York", (2040, 11, 4, 1, 30, 0)),
             ("America/New_York", (2400, 3, 12, 2, 30, 0)), ("Europe/Berlin", (2040, 3, 25, 2, 30, 0)),
             ("Europe/Berlin", (2040, 10, 28, 2, 30, 0)), ("Europe/Dublin", (2040, 3, 25, 1, 30, 0)),
             ("Europe/Dublin", (2040, 10, 28, 1, 30, 0)), ("Australia/Sydney", (2040, 10, 7, 2, 30, 0)),
             ("Australia/Sydney", (2040, 4, 1, 2, 30, 0)), ("Australia/Lord_Howe", (2040, 10, 7, 2, 15, 0)),
             ("Australia/Lord_Howe", (2040, 4, 1, 1, 45, 0)), ("America/Santiago", (2040, 9, 2, 0, 30, 0)),
             ("America/Santiago", (2040, 4, 7, 23, 30, 0))]
    for name, wall in walls:
        corpus.wall_time(file, "extrapolation-wall", name, wall, "row A5, a wall time after 2037")
    # Row A7: start of day after 2037, where the recurring rules skip midnight.
    for name, date in [("America/Santiago", (2040, 9, 2)), ("America/Havana", (2040, 3, 11)),
                       ("Asia/Beirut", (2040, 3, 25)), ("America/New_York", (2040, 3, 11)),
                       ("Australia/Lord_Howe", (2040, 10, 7)), ("America/Santiago", (2037, 9, 6))]:
        corpus.start_of_day(file, "extrapolation-day", name, date, "row A7, start of day after 2037")
        y, mo, d = date
        corpus.start_of_day_from(file, "extrapolation-day", name, (T.days_from_civil(y, mo, d) * 86400 + 86400) *
                                 T.NS_PER_SECOND, "row A7, start of day after 2037")
    # Row Z4's formatter cells: the hour a formatter shows after 2037, and before it.
    for name in ("America/New_York", "Europe/Berlin", "Australia/Sydney", "America/Santiago"):
        for at in ("2037-07-01T12:00", "2040-07-01T12:00", "2040-01-15T12:00"):
            corpus.formatter_hour(file, "extrapolation-format", name, E(at), "row Z4, the formatter's zone")


# --------------------------------------------------------------------------------------------------
# Rows Z5 to Z8 and Z12: the identifiers.

def identifiers(corpus: Corpus) -> None:
    db = corpus.db
    file = "identifiers"
    for name in db.identifiers:
        corpus.zone_id(file, "identifier", name, "row Z6, an identifier of the default build, by its own case")
    for text in ("Factory", "factory", "FACTORY"):
        note = "row Z5, Factory is in tzdb's factory file, which the default build of SJL-D-20 does not compile"
        corpus.zone_id(file, "factory", text, note)
        corpus.zone_id(file, "factory", text, note, through_instant=True)
        corpus.formatter_zone(file, "factory", text, note)
    corpus.supported(file, "factory", "includes", "Factory", "row Z5, Factory is not listed")

    # Every non-primary identifier equals its primary; a Link whose CLDR primary is not its tzdb target does not
    # equal the target (row Z7).
    for name in db.identifiers:
        primary = db.primary(name)
        if primary != name:
            corpus.equals(file, "primary", name, primary, f"row Z7, {name}'s primary is {primary}")
    for name in db.identifiers:
        target = name
        while target in db.links:
            target = db.links[target]
        if target != name and db.primary(target) != db.primary(name):
            corpus.equals(file, "primary", name, target,
                          f"row Z7, CLDR keeps {name} a primary of its own ({db.primary(name)}), and tzdb links it to {target}")
    for one, two in [("Etc/GMT", "UTC"), ("GMT", "Etc/UTC"), ("Etc/GMT-0", "UTC"), ("Europe/Amsterdam", "Europe/Brussels"),
                     ("Atlantic/Reykjavik", "Africa/Abidjan"), ("Asia/Calcutta", "Asia/Kolkata"),
                     ("Europe/Kiev", "Europe/Kyiv"), ("EST5EDT", "America/New_York"), ("CET", "Europe/Brussels"),
                     ("Atlantic/Jan_Mayen", "Arctic/Longyearbyen"), ("US/Eastern", "America/New_York"),
                     ("Europe/Bratislava", "Europe/Prague"), ("Arctic/Longyearbyen", "Europe/Oslo"),
                     ("Europe/Busingen", "Europe/Zurich"), ("Europe/Vatican", "Europe/Rome"),
                     ("America/Marigot", "America/Port_of_Spain")]:
        corpus.equals(file, "primary", one, two, "row Z7's cell")

    corpus.supported(file, "supported", "list", "timeZone", "row Z7, the primary identifiers, ordinal")
    corpus.supported(file, "supported", "count", "timeZone", "row Z7, how many primary identifiers")
    for text in ("UTC", "Etc/UTC", "Etc/GMT", "GMT", "Asia/Kolkata", "Asia/Calcutta", "Europe/Kyiv", "Europe/Kiev",
                 "EST5EDT", "America/New_York", "Europe/Prague", "Europe/Oslo", "Europe/Zurich", "Europe/Rome",
                 "America/Port_of_Spain", "America/Curacao", "Europe/Helsinki", "Europe/Belgrade", "America/Toronto"):
        corpus.supported(file, "supported", "includes", text, "row Z7")
    for name in db.identifiers:
        target = name
        while target in db.links:
            target = db.links[target]
        if target != name and db.primary(name) == name:
            corpus.supported(file, "supported", "includes", name,
                             f"row Z7, CLDR keeps {name} a primary, which tzdb links to {target}")

    # The formatter's resolved name: implementation-defined for a non-primary identifier (row Z8).
    for name in db.identifiers:
        if db.primary(name) != name:
            corpus.formatter_zone(file, "formatter-zone", name, f"{name} is not its own primary")
    for text in ("Europe/Berlin", "europe/berlin", "UTC", "utc", "Europe/Bratislava", "Arctic/Longyearbyen",
                 "America/Marigot", "Europe/Vatican", "Etc/GMT+5", "etc/gmt+5", "Asia/Kolkata"):
        corpus.formatter_zone(file, "formatter-zone", text, "row Z8, a primary identifier, which is its own name either way")

    for region in ("US", "DE", "AT", "IN", "UA", "GB", "AQ", "CN", "ZZ", "NL", "NO", "SK"):
        corpus.region(file, "region", f"en-{region}")


# --------------------------------------------------------------------------------------------------
# Rows Z9 to Z11, A11 to A14: lookup.

CASES = ["europe/berlin", "EUROPE/BERLIN", "eUrOpE/bErLiN", "utc", "Utc", "etc/utc", "gmt", "etc/gmt+5", "ETC/GMT-14",
         "america/argentina/buenos_aires", "AMERICA/PORT-AU-PRINCE", "us/eastern", "asia/calcutta", "cst6cdt",
         "europe/kyiv", "AMERICA/LOS_ANGELES", "aMeRiCa/NeW_yOrK"]
# Non-ASCII case folds: U+017F folds to s and its invariant upper case is S; U+212A (Kelvin) folds to k; U+0131 and
# U+0130 are the dotless and dotted Turkic i. None is an ASCII-case-insensitive match for any identifier.
FOLDS = ["America/Lo\u017f_Angeles", "A\u017fia/Tokyo", "Au\u017ftralia/Sydney", "U\u017f/Pacific",
         "Europe/\u212ayiv", "Europe/Berl\u0131n", "Europe/K\u0131ev", "Europe/\u0130stanbul", "\u0130ndian/Maldives",
         "Europe/Ki\u0435v", "Europe/Berlin\u00a0"]
ISO_STRINGS = ["2020-01-01T00:00[Europe/Berlin]", "2020-01-01T00:00Z", "2020-01-01T00:00+01:00[Europe/Berlin]",
               "2020-01-01T00:00[America/Lo\u017f_Angeles]"]
OFFSETS = ["+05:30", "-05:30", "+0530", "+05", "-00:00", "+00:00", "-00", "+23:59", "-23:59", "-12:00", "+14:00"]
REFUSED_OFFSETS = ["+24:00", "+05:60", "+05:30:15", "+05:30:00", "+053015", "+05:30:15.5", "\u221205:00", "+05:3",
                   "05:30", "+5:30", "+05:30 ", "+0530:15"]
DIGITS = ["+\u0660\u0665:\u0663\u0660", "+\u06f0\u06f5:\u06f3\u06f0", "+\u0966\u096b:\u0969\u0966",
          "+\uff10\uff15:\uff13\uff10", "+0\u0665:30", "-\u0660\u0665", "+\u0660\u0665\u0663\u0660"]
ETC_GMT = ["Etc/GMT+5", "Etc/GMT-14", "Etc/GMT+12", "Etc/GMT-1", "Etc/GMT0", "Etc/GMT-0", "GMT+0", "GMT0"]
UNKNOWN = ["Mars/Olympus", "Etc/Unknown", "", "America/New York", "Europe/Berlin ", " Europe/Berlin", "EST5",
           "Etc/GMT+13", "Etc/GMT-15", "Etc/GMT+05", "Europe/Berlin\u0000", "Europe//Berlin", "UT", "Z",
           "America/New_York/", "US/Pacific-New", "posixrules"]


def lookup(corpus: Corpus) -> None:
    file = "lookup"
    for text in CASES:
        note = "row Z9, an ASCII-case-insensitive match takes the identifier's own case"
        corpus.zone_id(file, "case", text, note)
        corpus.zone_id(file, "case", text, note, through_instant=True)
        corpus.formatter_zone(file, "case", text, note)
    corpus.equals(file, "case", "europe/berlin", "Europe/Berlin", "row Z9")
    for text in FOLDS:
        note = "row Z10, GetAvailableNamedTimeZoneIdentifier matches by ASCII case only, so a non-ASCII fold is no match"
        corpus.zone_id(file, "fold", text, note)
        corpus.zone_id(file, "fold", text, note, through_instant=True)
        corpus.formatter_zone(file, "fold", text, note)
    for text in ISO_STRINGS:
        note = "row Z11, an ISO date-time string is neither an offset nor an available name to the formatter"
        corpus.formatter_zone(file, "iso-string", text, note)
        corpus.zone_id(file, "iso-string", text, "row Z11, the ZonedDateTime constructor reads no ISO string either")
        corpus.zone_id(file, "iso-string", text, "row Z11, Temporal's ToTemporalTimeZoneIdentifier reads its annotation",
                       through_instant=True)
    for text in OFFSETS + REFUSED_OFFSETS:
        note = "row A12, an offset identifier: UTCOffset without seconds, normalised"
        corpus.zone_id(file, "offset-id", text, note)
        corpus.formatter_zone(file, "offset-id", text, note + "; the formatter refuses seconds")
    for text in ("+05:30", "-00:00", "+14:00"):
        corpus.offset(file, "offset-id", text, E("2026-07-01T12:00"), "row A12, an offset zone")
        corpus.transition(file, "offset-id", text, E("2026-07-01T12:00"), "next", "row A12, an offset zone has none")
        corpus.transition(file, "offset-id", text, E("2026-07-01T12:00"), "previous", "row A12, an offset zone has none")
    for one, two in [("+00:00", "UTC"), ("+05:30", "+0530"), ("-00:00", "+00:00"), ("+01:00", "Europe/Berlin")]:
        corpus.equals(file, "offset-id", one, two, "row A12, offset zones equal only as the same offset identifier")
    corpus.wall_time(file, "offset-id", "-12:00", (2020, 1, 1, 0, 0, 0), "row A12, an offset zone")
    for text in DIGITS:
        note = "row A13, the offset grammar's digits are ASCII (DecimalDigit), so this is no offset and no name"
        corpus.zone_id(file, "offset-digits", text, note, clauses=OFFSET_IDENTIFIER + RECORDS[1:])
        corpus.zone_id(file, "offset-digits", text, note, through_instant=True, clauses=OFFSET_IDENTIFIER + RECORDS[1:])
        corpus.formatter_zone(file, "offset-digits", text, note)
    for text in ETC_GMT:
        note = "row A14, an Etc/GMT zone, POSIX's sign inverted"
        corpus.zone_id(file, "etc-gmt", text, note)
        corpus.offset(file, "etc-gmt", text, E("1970-01-01T00:00"), note)
        corpus.offset(file, "etc-gmt", text, E("1850-01-01T00:00"), note)
        corpus.transition(file, "etc-gmt", text, E("2026-01-01T00:00"), "next", note)
    corpus.equals(file, "etc-gmt", "Etc/GMT+5", "-05:00", "row A14, a named zone never equals an offset zone")
    corpus.formatter_hour(file, "etc-gmt", "Etc/GMT+5", 0, "row A14")
    corpus.formatter_hour(file, "etc-gmt", "Etc/GMT-14", 0, "row A14")
    for text in UNKNOWN:
        note = "row A11, an unknown name"
        corpus.zone_id(file, "unknown", text, note)
        corpus.zone_id(file, "unknown", text, note, through_instant=True)
        corpus.formatter_zone(file, "unknown", text, note)


# --------------------------------------------------------------------------------------------------
# Rows A1, A3, A4, A6 and A9: an ordinary sample.

ORDINARY = ["America/New_York", "Europe/London", "Australia/Sydney", "Australia/Lord_Howe", "Europe/Dublin",
            "America/Santiago", "Pacific/Chatham", "Antarctica/Troll", "Asia/Jerusalem", "America/St_Johns"]


def ordinary(corpus: Corpus) -> None:
    file = "transitions"
    for name in ORDINARY:
        note = f"row A1, {name} in 2026"
        both_sides(corpus, file, "offset", name, 2026, 2026, note)
        for at in changes(corpus, name, year_start(2026), year_start(2027)):
            ns = at * T.NS_PER_SECOND
            for direction in ("next", "previous"):
                corpus.transition(file, "transition", name, ns - T.NS_PER_SECOND, direction, f"row A9, {name} in 2026")
                corpus.transition(file, "transition", name, ns, direction, f"row A9, {name} in 2026")
    for name, at in [("America/New_York", "1883-11-18T12:00"), ("America/New_York", "1800-01-01T00:00"),
                     ("Asia/Kolkata", "2026-07-01T12:00"), ("Australia/Lord_Howe", "2026-01-15T12:00"),
                     ("Australia/Lord_Howe", "2026-07-15T12:00"), ("Pacific/Kiritimati", "1994-12-30T12:00"),
                     ("Pacific/Kiritimati", "1995-01-02T12:00"), ("Africa/Monrovia", "1970-01-01T00:00"),
                     ("Europe/Paris", "1900-01-01T00:00"), ("Asia/Kathmandu", "2026-07-01T12:00"),
                     ("America/St_Johns", "2026-01-15T12:00"), ("Pacific/Apia", "2011-12-29T12:00"),
                     ("Pacific/Apia", "2011-12-31T12:00"), ("America/New_York", "-271821-04-20T00:00"),
                     ("UTC", "-271821-04-20T00:00"), ("Australia/Sydney", "+275760-09-13T00:00")]:
        corpus.offset(file, "offset", name, E(at), "row A1's cell")
    half = T.NS_PER_SECOND // 2
    for name, ns, direction in [
            ("America/New_York", E("2026-07-01T12:00"), "next"), ("America/New_York", E("2026-07-01T12:00"), "previous"),
            ("America/New_York", E("2026-03-08T07:00"), "next"), ("America/New_York", E("2026-03-08T07:00"), "previous"),
            ("America/New_York", E("2026-03-08T07:00") + half, "previous"),
            ("America/New_York", E("2026-03-08T07:00") - half, "next"),
            ("America/New_York", E("1800-01-01T00:00"), "next"), ("America/New_York", E("1800-01-01T00:00"), "previous"),
            ("Australia/Lord_Howe", E("2026-01-01T00:00"), "next"), ("Europe/London", E("1968-07-01T12:00"), "next"),
            ("Europe/London", E("1971-07-01T12:00"), "previous"), ("Asia/Riyadh", E("2000-01-01T00:00"), "next"),
            ("Asia/Riyadh", E("2000-01-01T00:00"), "previous"), ("Etc/GMT+5", E("2026-01-01T00:00"), "next"),
            ("+05:30", E("2026-01-01T00:00"), "next"), ("UTC", E("2026-01-01T00:00"), "previous"),
            ("America/New_York", E("-271821-04-20T00:00"), "previous"),
            ("America/New_York", E("+275760-09-13T00:00"), "next"), ("Pacific/Apia", E("2012-01-01T00:00"), "previous"),
            ("Pacific/Kiritimati", E("1994-12-01T00:00"), "next")]:
        corpus.transition(file, "transition", name, ns, direction, "row A9's cell")

    file = "wall-times"
    walls = [("America/New_York", (2026, 3, 8, 2, 30, 0), "a gap of an hour"),
             ("America/New_York", (2026, 11, 1, 1, 30, 0), "an overlap of an hour"),
             ("Europe/Berlin", (2026, 3, 29, 2, 30, 0), "a gap of an hour"),
             ("Europe/Berlin", (2026, 10, 25, 2, 30, 0), "an overlap of an hour"),
             ("Australia/Lord_Howe", (2026, 10, 4, 2, 15, 0), "a gap of 30 minutes"),
             ("Australia/Lord_Howe", (2026, 4, 5, 1, 45, 0), "an overlap of 30 minutes"),
             ("Pacific/Kiritimati", (1994, 12, 31, 12, 0, 0), "a gap of 24 hours"),
             ("Pacific/Apia", (2011, 12, 30, 12, 0, 0), "a gap of 24 hours, a skipped day"),
             ("America/Juneau", (1867, 10, 19, 12, 0, 0), "an overlap of 24 hours"),
             ("Africa/Monrovia", (1972, 1, 7, 0, 20, 0), "a gap of 44 minutes 30 seconds"),
             ("Europe/Dublin", (2026, 3, 29, 1, 30, 0), "a gap, negative daylight time"),
             ("Europe/Dublin", (2026, 10, 25, 1, 30, 0), "an overlap, negative daylight time"),
             ("Australia/Sydney", (2026, 10, 4, 2, 30, 0), "a gap of an hour, south"),
             ("Australia/Sydney", (2026, 4, 5, 2, 30, 0), "an overlap of an hour, south"),
             ("America/New_York", (2026, 7, 1, 12, 0, 0), "an ordinary wall time")]
    for name, wall, kind in walls:
        corpus.wall_time(file, "wall", name, wall, f"rows A3 and A4, {kind}")
    for name, date in [("America/Santiago", (2026, 9, 6)), ("America/Havana", (2026, 3, 8)), ("Asia/Beirut", (1920, 3, 28)),
                       ("Asia/Beirut", (2026, 3, 29)), ("America/Toronto", (1919, 3, 31)),
                       ("America/Sao_Paulo", (2018, 11, 4)), ("America/Asuncion", (2024, 10, 6)),
                       ("Pacific/Apia", (2011, 12, 30)), ("Pacific/Apia", (2011, 12, 29)),
                       ("America/New_York", (2026, 3, 8)), ("America/New_York", (2026, 11, 1)),
                       ("Australia/Lord_Howe", (2026, 10, 4))]:
        corpus.start_of_day(file, "day", name, date, "row A6, start of day")
        y, mo, d = date
        if (name, date) != ("Pacific/Apia", (2011, 12, 30)):
            corpus.start_of_day_from(file, "day", name, (T.days_from_civil(y, mo, d) * 86400 + 86400) *
                                     T.NS_PER_SECOND, "row A6, start of day")


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--out", required=True, type=Path)
    parser.add_argument("--home", type=Path, default=T.DEFAULT_HOME, help="the time data home, Broiler.ECMAScript/data/time")
    parser.add_argument("--zic", default="zic", help="the zic to run")
    arguments = parser.parse_args(argv)
    try:
        db = T.Database(arguments.home, arguments.zic)
    except (T.PinError, FileNotFoundError) as error:
        print(f"refused: {error}", file=sys.stderr)
        return 2
    corpus = Corpus(db)
    release(corpus)
    backzone(corpus)
    extrapolation(corpus)
    identifiers(corpus)
    lookup(corpus)
    ordinary(corpus)
    corpus.classify(DIVERGENCES)
    corpus.write(arguments.out)
    for name, records in sorted(corpus.files.items()):
        classes: dict[str, int] = defaultdict(int)
        for record in records:
            classes[record["class"]] += 1
        print(f"{name}.vectors: {len(records)} vectors ({', '.join(f'{n} {c}' for c, n in sorted(classes.items()))})")
    total = sum(len(records) for records in corpus.files.values())
    print(f"{total} vectors in {len(corpus.files)} files; tzdb {db.release}, {db.zic_version}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
