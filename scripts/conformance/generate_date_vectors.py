#!/usr/bin/env python3
"""Generate the Date area of the shared conformance corpus.

    python3 scripts/conformance/generate_date_vectors.py --out conformance/vectors/date

Every expected value comes from `dateref.py`, the corpus's independent reference computation of the Date
algorithms, written from the specification's text; no expected value is taken from either engine. The
populations are milestone SJL-6.2's: the time-value arithmetic (MakeTime with fractional and non-finite
components, MakeDay across the representable range, TimeClip at ±8.64e15 and on -0, MakeFullYear) and the
setters' order and range; field extraction before 1970, around leap days and at the range's ends; the Date
Time String Format in every allowed omission, with expanded years, -000000 refused, exactly three fraction
digits, 24:00 and offsets, and date-only forms read as UTC; each engine's dialect, as its own renderings read
back and as Broiler.JS's numeric slash dates, implementation-defined with an expectation per engine; the
renderings at the range's ends and for negative years; local time, through relations that hold in every time
zone; and the ISO calendar's civil-day helpers, through Temporal.

**Local time.** This engine's local time is its host's and Broiler.VM's is UTC (decision SJL-D-19), so no
vector's outcome depends on a time zone: a vector either reads UTC, or puts a local time in and reads the
same local time back, or tests a relation the specification makes hold in every zone. A run in a zone with
daylight-saving time finds the local-time defects the correspondence names, against the same expectations.

The output is deterministic: the same inputs give the same bytes. Vector identifiers are stable and are
never reused; a later revision appends.
"""

from __future__ import annotations

import argparse
import math
import struct
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import dateref as D  # noqa: E402
from generate_number_vectors import js_string_literal, splitmix64, string_value  # noqa: E402

# The corpus revision this generator writes, and the revision its vectors were added in. Revision 16 adds the
# strings outside the format whose readings the engines' dialects differ on (OUTSIDE_FORMAT_REVISION).
# Revision 17 retires every divergence both engines' cut-overs correct (date-divergences.txt), and
# Broiler.JS's expectation of two of those strings becomes its reading since its cut-over, milestone SJL-6.5.
# Revision 18 adds the Unicode area (generate_unicode_vectors.py) and changes nothing here, nor does revision 19,
# which classifies the Unicode area's divergences, nor revision 20, which retires them, nor revision 21, which
# adds the time-zone area (generate_time_zone_vectors.py), nor revision 22, which classifies its divergences, nor
# revision 23, which retires them, nor revision 24, which adds the Temporal area (generate_temporal_vectors.py) and
# widens the schema's provenance, nor revision 25, which classifies its differences, nor revision 26, which retires
# them.
CORPUS_REVISION = 26
FIRST_REVISION = 14
OUTSIDE_FORMAT_REVISION = 16
GENERATOR = "ref:dateref@1"
SPEC = "spec:es2026@0248456c"
ENGINES = ("broiler-js", "broiler-vm")
DIVERGENCES = Path(__file__).resolve().parent / "date-divergences.txt"
SEED = 0x534A4C362D564543  # "SJL6-VEC"


def clause(*ids: str) -> str:
    return " ".join(f"{SPEC}#{i}" for i in ids)


def prov(*ids: str) -> str:
    return f"{clause(*ids)} {GENERATOR}"


def bits(x: float) -> int:
    return struct.unpack(">Q", struct.pack(">d", x))[0]


def f64(x: float) -> str:
    return "f64:0x7FF8000000000000" if math.isnan(x) else f"f64:0x{bits(x):016X}"


def number_out(x: float) -> str:
    return f"value: {f64(x)}"


def text_out(s: str) -> str:
    return f"value: {string_value(s)}"


def bool_out(b: bool) -> str:
    return f"value: bool:{'true' if b else 'false'}"


def js(x: float) -> str:
    """A Number as script source, never through a decimal conversion the engine might get wrong: an integer
    literal for a safe integer, otherwise the prelude's $f64 of its bit pattern."""
    if math.isnan(x):
        return "NaN"
    if math.isinf(x):
        return "Infinity" if x > 0 else "-Infinity"
    if x == 0:
        return "-0" if math.copysign(1.0, x) < 0 else "0"
    if x == int(x) and abs(x) < 2 ** 53:
        return str(int(x))
    b = bits(x)
    return f"$f64(0x{b >> 32:08X}, 0x{b & 0xFFFFFFFF:08X})"


def ins(*values: float) -> str:
    return " ".join(f64(v) for v in values)


def lit(s: str) -> str:
    return js_string_literal(s)


def utc(y: int, m: int = 0, d: int = 1, h: int = 0, mi: int = 0, s: int = 0, ms: int = 0) -> float:
    """The time value of a UTC date, through MakeDay directly (years 0 to 99 included)."""
    return D.time_clip(D.make_date(D.make_day(float(y), float(m), float(d)), D.make_time(float(h), float(mi), float(s), float(ms))))


# --------------------------------------------------------------------------------------------------
# Records.


class Corpus:
    def __init__(self) -> None:
        self.files: dict[str, list[dict[str, str]]] = {}
        self.ids: set[str] = set()
        self.counters: dict[str, int] = {}

    def add(self, file: str, group: str, record: dict[str, str], since: int = FIRST_REVISION) -> None:
        self.counters[group] = self.counters.get(group, 0) + 1
        vector_id = f"date/{group}/{self.counters[group]:04d}"
        assert vector_id not in self.ids
        self.ids.add(vector_id)
        full = {"id": vector_id, "area": "date"}
        full.update(record)
        full.setdefault("class", "normative")
        full["since"] = str(since)
        assert "\n" not in full["script"], full["script"]
        assert all(ord(ch) < 0x80 for ch in full["script"]), full["script"]
        self.files.setdefault(file, []).append(full)

    def classify(self, path: Path) -> None:
        """Applies date-divergences.txt, as generate_text_vectors.py applies its own file."""
        if not path.exists():
            return
        reasons: dict[str, str] = {}
        by_id = {record["id"]: record for records in self.files.values() for record in records}
        for line_number, line in enumerate(path.read_text(encoding="utf-8").splitlines(), 1):
            if not line or line.startswith("#"):
                continue
            if line.startswith("reason "):
                key, _, text = line[len("reason "):].partition(": ")
                if not text or key in reasons:
                    raise SystemExit(f"{path.name}:{line_number}: a malformed or repeated reason")
                reasons[key] = text
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
                "# Generated by scripts/conformance/generate_date_vectors.py; do not edit by hand.",
                "# Expected values: scripts/conformance/dateref.py, written from the specification's text.",
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


def draws(salt: int, count: int):
    """`count` SplitMix64 outputs from the area's seed and a salt."""
    generator = splitmix64(SEED ^ salt)
    return [next(generator) for _ in range(count)]


def random_time_values(salt: int, count: int, low: float = -D.MAX_TIME, high: float = D.MAX_TIME) -> list[float]:
    span = int(high - low) + 1
    return [float(int(low) + r % span) for r in draws(salt, count)]


# --------------------------------------------------------------------------------------------------
# The time-value arithmetic.

COMPONENTS = [0.0, -0.0, 0.5, -0.5, 1.0, -1.0, 1.5, -1.5, 23.0, 24.0, 59.0, 60.0, 999.0, 1000.0,
              2147483648.0, -2147483648.0, 4294967296.0, float(2 ** 53), 1e15, 1e20, D.NAN, math.inf, -math.inf]


def arithmetic(corpus: Corpus) -> None:
    # MakeTime, through Date.UTC on the epoch's day, whose MakeDay is +0.
    def make_time(h: float, m: float, s: float, ms: float) -> None:
        corpus.add("arithmetic", "make-time", {
            "op": "MakeTime",
            "in": ins(h, m, s, ms),
            "out": number_out(D.date_utc(1970.0, 0.0, 1.0, h, m, s, ms)),
            "prov": prov("sec-maketime", "sec-makedate", "sec-timeclip", "sec-date.utc"),
            "script": f"Date.UTC(1970, 0, 1, {js(h)}, {js(m)}, {js(s)}, {js(ms)})",
        })

    for position in range(4):
        for value in COMPONENTS:
            parts = [0.0, 0.0, 0.0, 0.0]
            parts[position] = value
            make_time(*parts)
    pool = COMPONENTS + [float(v) for v in range(-61, 62, 7)]
    for r in draws(1, 200):
        parts = []
        for k in range(4):
            choice = (r >> (16 * k)) & 0xFFFF
            if choice % 3 == 0:
                parts.append(pool[choice % len(pool)])
            elif choice % 3 == 1:
                parts.append(float((choice % 4001) - 2000))
            else:
                parts.append(((choice % 20001) - 10000) / 8.0)
        make_time(*parts)

    # MakeDay, through setUTCFullYear on the epoch, which applies no MakeFullYear.
    def make_day(y: float, m: float, d: float, note: str | None = None) -> None:
        record = {
            "op": "MakeDay",
            "in": ins(y, m, d),
            "out": number_out(D.set_utc_full_year(0.0, y, m, d)),
            "prov": prov("sec-makeday", "sec-makedate", "sec-timeclip", "sec-date.prototype.setutcfullyear"),
            "script": f"new Date(0).setUTCFullYear({js(y)}, {js(m)}, {js(d)})",
        }
        if note:
            record["note"] = note
        corpus.add("arithmetic", "make-day", record)

    years = [-271822.0, -271821.0, -271820.0, -100000.0, -401.0, -400.0, -101.0, -100.0, -5.0, -4.0, -1.0, 0.0,
             1.0, 4.0, 99.0, 100.0, 1582.0, 1600.0, 1700.0, 1899.0, 1900.0, 1969.0, 1970.0, 1972.0, 2000.0, 2024.0,
             2100.0, 9999.0, 10000.0, 100000.0, 275759.0, 275760.0, 275761.0, 1e8, -1e8]
    months = [-25.0, -13.0, -12.0, -1.0, 0.0, 1.0, 2.0, 11.0, 12.0, 13.0, 24.0, 1.5, -0.5]
    days = [-366.0, -1.0, 0.0, 1.0, 28.0, 29.0, 30.0, 31.0, 32.0, 366.0, 1.5]
    for y in years:
        for m in months:
            make_day(y, m, 1.0)
        for d in days:
            make_day(y, 1.0, d)
    for y in (0.0, 1970.0):
        for m in (1e6, -1e6, float(2 ** 53), -float(2 ** 53), 1e19, -1e19, D.NAN, math.inf):
            make_day(y, m, 1.0)
    for d in (1e8, -1e8, 2147483648.0, D.NAN, math.inf, -math.inf):
        make_day(1970.0, 0.0, d)
    make_day(-833333333333333333.0, 1e19, 1.0,
             "the month is past 2^53: its modulo 12 and floor of its twelfth are exact (correspondence row T4)")
    make_day(-833333333333333333.0, -1e19, 1.0, "a month past -2^53 (correspondence row T4)")

    # TimeClip, through the Date constructor with one Number.
    for t in (0.0, -0.0, 0.5, -0.5, 0.999, -0.999, 1.5, -1.5, 8.64e15, -8.64e15, 8.64e15 + 1, -8.64e15 - 1,
              8.64e15 - 1, -8.64e15 + 1, float(2 ** 53), -float(2 ** 53), 1e16, 1e300, D.NAN, math.inf, -math.inf):
        corpus.add("arithmetic", "time-clip", {
            "op": "TimeClip",
            "in": ins(t),
            "out": number_out(D.time_clip(t)),
            "prov": prov("sec-timeclip", "sec-date", "sec-date.prototype.gettime"),
            "script": f"new Date({js(t)}).getTime()",
        })

    # MakeFullYear, through Date.UTC with one argument.
    for y in (-1.0, -0.5, -0.0, 0.0, 0.5, 1.0, 49.0, 50.0, 99.0, 99.5, 99.9, 100.0, 100.5, 1900.0, 2000.0,
              1e5, -1e5, 2147483648.0, D.NAN, math.inf, -math.inf):
        corpus.add("arithmetic", "make-full-year", {
            "op": "MakeFullYear",
            "in": ins(y),
            "out": number_out(D.date_utc(y)),
            "prov": prov("sec-makefullyear", "sec-date.utc"),
            "script": f"Date.UTC({js(y)})",
        })

    setters(corpus)
    setter_order(corpus)


def setters(corpus: Corpus) -> None:
    """The UTC setters' results, over the whole range and at its ends; and the local setters, a local date put
    in and read back."""
    jan2021 = utc(2021)
    end9999 = utc(9999, 11, 31)
    first = utc(1, 0, 1)
    after_first = utc(1, 0, 1, 0, 0, 0, 1)
    raw = -1e15
    cases = [
        ("setUTCHours", jan2021, (1.5,), D.set_utc_hours(jan2021, 1.5)),
        ("setUTCHours", jan2021, (-0.5,), D.set_utc_hours(jan2021, -0.5)),
        ("setUTCHours", jan2021, (0.0, 0.0, 4294967297.0), D.set_utc_hours(jan2021, 0.0, 0.0, 4294967297.0)),
        ("setUTCHours", jan2021, (0.0, 0.0, 0.0, 2147483648.0), D.set_utc_hours(jan2021, 0.0, 0.0, 0.0, 2147483648.0)),
        ("setUTCHours", jan2021, (D.NAN,), D.NAN),
        ("setUTCHours", raw, (1.0,), D.set_utc_hours(raw, 1.0)),
        ("setUTCDate", end9999, (32.0,), D.set_utc_date(end9999, 32.0)),
        ("setUTCDate", raw, (15.0,), D.set_utc_date(raw, 15.0)),
        ("setUTCDate", jan2021, (1.5,), D.set_utc_date(jan2021, 1.5)),
        ("setUTCMonth", first, (-1.0,), D.set_utc_month(first, -1.0)),
        ("setUTCMonth", end9999, (12.0,), D.set_utc_month(end9999, 12.0)),
        ("setUTCMonth", raw, (3.0,), D.set_utc_month(raw, 3.0)),
        ("setUTCMonth", jan2021, (1e19,), D.set_utc_month(jan2021, 1e19)),
        ("setUTCFullYear", jan2021, (0.0,), D.set_utc_full_year(jan2021, 0.0)),
        ("setUTCFullYear", jan2021, (-1.0,), D.set_utc_full_year(jan2021, -1.0)),
        ("setUTCFullYear", jan2021, (10000.0,), D.set_utc_full_year(jan2021, 10000.0)),
        ("setUTCFullYear", jan2021, (275760.0, 8.0, 13.0), D.set_utc_full_year(jan2021, 275760.0, 8.0, 13.0)),
        ("setUTCFullYear", D.NAN, (2000.0,), D.set_utc_full_year(D.NAN, 2000.0)),
        ("setUTCMilliseconds", after_first, (0.0,), D.set_utc_milliseconds(after_first, 0.0)),
        ("setUTCMilliseconds", jan2021, (2147483648.0,), D.set_utc_milliseconds(jan2021, 2147483648.0)),
        ("setUTCMilliseconds", raw, (5.0,), D.set_utc_milliseconds(raw, 5.0)),
    ]
    clauses = {"setUTCHours": "sec-date.prototype.setutchours", "setUTCDate": "sec-date.prototype.setutcdate",
               "setUTCMonth": "sec-date.prototype.setutcmonth", "setUTCFullYear": "sec-date.prototype.setutcfullyear",
               "setUTCMilliseconds": "sec-date.prototype.setutcmilliseconds"}
    for name, t, args, expected in cases:
        corpus.add("arithmetic", "utc-setter", {
            "op": f"Date.prototype.{name}",
            "in": ins(t, *args),
            "out": number_out(expected),
            "prov": prov(clauses[name], "sec-makeday", "sec-maketime", "sec-timeclip"),
            "script": f"new Date({js(t)}).{name}({', '.join(js(a) for a in args)})",
        })

    # The local setters: a local date put in, set, and its local fields read back. Every date here is a valid
    # one, in a year the Date constructor reads as given (100 and above), at noon or a whole day from it, so
    # no zone's transition can touch it, and the reference computes it on the UTC time scale, which gives the
    # same local fields in every zone.
    local_cases = [
        [("setFullYear", 9999, 11, 31), ("setDate", 32)],
        [("setFullYear", 1, 0, 1), ("setMonth", -1)],
        [("setFullYear", -1, 5, 15)],
        [("setFullYear", 275759, 5, 15), ("setMonth", 13)],
        [("setFullYear", 0, 1, 29)],
        [("setFullYear", 100, 1, 29)],
        [("setMonth", 1, 29)],
        [("setDate", 0)],
        [("setFullYear", -271820, 0, 1), ("setDate", -1)],
        [("setFullYear", 2021, 0, 1), ("setHours", 36)],
    ]
    apply = {
        "setFullYear": lambda t, *a: D.set_utc_full_year(t, *map(float, a)),
        "setMonth": lambda t, *a: D.set_utc_month(t, *map(float, a)),
        "setDate": lambda t, *a: D.set_utc_date(t, *map(float, a)),
        "setHours": lambda t, *a: D.set_utc_hours(t, *map(float, a)),
    }
    for steps in local_cases:
        t = utc(2000, 0, 1, 12)
        for name, *arguments in steps:
            t = apply[name](t, *arguments)
        expected = (f"{int(D.year_from_time(t))},{int(D.month_from_time(t))},{int(D.date_from_time(t))},"
                    f"{int(D.hour_from_time(t))}")
        source = "; ".join(f"d.{name}({', '.join(str(a) for a in arguments)})" for name, *arguments in steps)
        corpus.add("arithmetic", "local-setter", {
            "op": "local setters, read back",
            "in": f"str:{string_value(source)[4:]}",
            "out": text_out(expected),
            "prov": prov("sec-date.prototype.setfullyear", "sec-date.prototype.setmonth", "sec-date.prototype.setdate",
                         "sec-date.prototype.sethours", "sec-makeday", "sec-utc-t", "sec-localtime"),
            "script": (f"var d = new Date(2000, 0, 1, 12); {source}; "
                       "[d.getFullYear(), d.getMonth(), d.getDate(), d.getHours()].join()"),
            "note": "a local date put in and read back, so the outcome holds in every time zone",
        })


SETTERS_WITH_FIELDS = [
    ("setUTCHours", 4, "sec-date.prototype.setutchours"), ("setUTCMinutes", 3, "sec-date.prototype.setutcminutes"),
    ("setUTCSeconds", 2, "sec-date.prototype.setutcseconds"), ("setUTCMonth", 2, "sec-date.prototype.setutcmonth"),
    ("setUTCFullYear", 3, "sec-date.prototype.setutcfullyear"), ("setHours", 4, "sec-date.prototype.sethours"),
    ("setMinutes", 3, "sec-date.prototype.setminutes"), ("setSeconds", 2, "sec-date.prototype.setseconds"),
    ("setMonth", 2, "sec-date.prototype.setmonth"), ("setFullYear", 3, "sec-date.prototype.setfullyear"),
]


# The setters whose argument conversion on an invalid date the pinned test262 asserts, in
# test/built-ins/Date/prototype/<setter>/arg-coercion-order.js.
COERCION_ORDER_TESTS = {"setUTCHours", "setUTCMinutes", "setUTCSeconds", "setUTCMonth", "setHours", "setMinutes",
                        "setSeconds", "setMonth"}


def setter_order(corpus: Corpus) -> None:
    """The setters' order: the time value read first, every present argument converted in order, the date
    written last (correspondence row T3)."""
    for name, arity, clause_id in SETTERS_WITH_FIELDS:
        for first in ("undefined", "NaN"):
            corpus.add("arithmetic", "setter-order", {
                "op": f"Date.prototype.{name}, order",
                "in": f"str:{string_value(first)[4:]}",
                "out": number_out(0.0),
                "prov": prov(clause_id),
                "script": (f"var d = new Date(0); var seen = null; d.{name}({first}, "
                           "{ valueOf: function () { seen = d.getTime(); return 0; } }); seen"),
                "note": "a later argument's conversion reads the date: it is unchanged until the setter writes it",
            })
        expected_count = float(arity)
        coercion_test = f"test/built-ins/Date/prototype/{name}/arg-coercion-order.js"
        corpus.add("arithmetic", "setter-order", {
            "op": f"Date.prototype.{name}, conversions on an invalid date",
            "in": f"int:{arity}",
            "out": number_out(expected_count),
            "prov": prov(clause_id) + (f" test262:{coercion_test}" if name in COERCION_ORDER_TESTS else ""),
            "script": (f"var n = 0; var v = {{ valueOf: function () {{ n++; return 1; }} }}; "
                       f"new Date(NaN).{name}({', '.join(['v'] * arity)}); n"),
            "note": "every present argument is converted before the invalid date is noticed",
        })


# --------------------------------------------------------------------------------------------------
# Field extraction.

GETTERS = [
    ("getUTCFullYear", "YearFromTime", "sec-yearfromtime", D.year_from_time),
    ("getUTCMonth", "MonthFromTime", "sec-monthfromtime", D.month_from_time),
    ("getUTCDate", "DateFromTime", "sec-datefromtime", D.date_from_time),
    ("getUTCDay", "WeekDay", "sec-weekday", D.week_day),
    ("getUTCHours", "HourFromTime", "sec-hourfromtime", D.hour_from_time),
    ("getUTCMinutes", "MinFromTime", "sec-minfromtime", D.min_from_time),
    ("getUTCSeconds", "SecFromTime", "sec-secfromtime", D.sec_from_time),
    ("getUTCMilliseconds", "msFromTime", "sec-msfromtime", D.ms_from_time),
]


def field_time_values() -> list[float]:
    values = [D.MAX_TIME, -D.MAX_TIME, D.MAX_TIME - 1, -D.MAX_TIME + 1, 0.0, -1.0, 1.0, utc(1969, 11, 31, 23, 59, 59, 999)]
    for y in (-271820, -100000, -401, -400, -101, -100, -5, -4, -2, -1, 0, 1, 4, 99, 100, 1600, 1700, 1800, 1899, 1900,
              1969, 1970, 1972, 2000, 2004, 2100, 2400, 9999, 10000, 100000, 275759):
        values.append(utc(y, 0, 1))
        values.append(utc(y + 1, 0, 1) - 1)
    for y in (-400, -100, -4, 0, 1600, 1700, 1800, 1900, 2000, 2004, 2100):
        values.append(utc(y, 1, 28, 12))
        values.append(utc(y, 1, 28, 23, 59, 59, 999) + 1)  # the day after the 28th: the 29th or March 1
        values.append(utc(y, 2, 1))
    values.append(utc(-271821, 3, 20))
    values.append(utc(275760, 8, 13))
    values += random_time_values(2, 250)
    out = []
    for v in values:
        if abs(v) <= D.MAX_TIME and v not in out:
            out.append(v)
    return out


def fields(corpus: Corpus) -> None:
    for t in field_time_values():
        for getter, op, clause_id, function in GETTERS:
            corpus.add("fields", "utc", {
                "op": op,
                "in": ins(t),
                "out": number_out(function(t)),
                "prov": prov(clause_id, "sec-date.prototype." + getter.lower()),
                "script": f"new Date({js(t)}).{getter}()",
            })


# --------------------------------------------------------------------------------------------------
# The Date Time String Format.

def format_strings() -> list[str]:
    years = ["0000", "0001", "1969", "1970", "2000", "2024", "9999", "+000000", "+002024", "+010000", "-000001",
             "+275760", "-271821", "-000000"]
    out: list[str] = []
    for year in years:
        for date in ("", "-01", "-12", "-01-01", "-02-28", "-06-15", "-12-31"):
            out.append(year + date)
        if year in ("0000", "2000", "2024", "+000000", "+002024"):
            out.append(year + "-02-29")
    out += ["-271821-04-20", "-271821-04-19", "+275760-09-13", "+275760-09-14", "-271821-04-20T00:00Z",
            "-271821-04-19T23:59:59.999Z", "+275760-09-13T00:00:00.000Z", "+275760-09-13T00:00:00.001Z",
            "-271821-04-20T00:00:00.000+00:01", "+275760-09-12T23:59:59.999-00:01"]
    times = ["T00:00", "T12:30", "T23:59:59", "T23:59:59.999", "T00:00:00.000", "T24:00", "T24:00:00", "T24:00:00.000"]
    offsets = ["Z", "+00:00", "-00:00", "+05:30", "-08:00", "+14:00", "-12:00", "+23:59", "-23:59"]
    for date in ("1970-01-01", "2024-02-29", "0000-01-01", "-000001-12-31", "+010000-06-15", "1969-12-31"):
        for time in times:
            for offset in offsets:
                out.append(date + time + offset)
    for date in ("2024", "2024-06", "+002024", "-000001-06"):
        for time in ("T12:30", "T12:30:45", "T12:30:45.678"):
            for offset in ("Z", "+01:00"):
                out.append(date + time + offset)
    out += ["2024-00-01", "2024-13-01", "2024-01-00", "2024-01-32", "2024-00", "2024-13", "2024-01-01T25:00Z",
            "2024-01-01T24:01Z", "2024-01-01T24:00:01Z", "2024-01-01T24:00:00.001Z", "2024-01-01T12:60Z",
            "2024-01-01T12:30:60Z", "-000000-01-01T00:00Z", "-000000-01-01T00:00:00.000Z"]
    seen = []
    for s in out:
        if s not in seen:
            seen.append(s)
    return seen


def local_format_strings() -> list[str]:
    out = []
    for date in ("1970-01-01", "2024-02-29", "2024-06-15", "0050-03-04", "0000-01-01", "-000001-06-15", "+010000-06-15",
                 "9999-12-31", "1969-12-31", "2024-11-03", "2024-03-10"):
        for time in ("T12:30", "T12:30:45", "T12:30:45.678"):
            out.append(date + time)
    for year in ("2024", "2024-06", "+002024"):
        out.append(year + "T12:30")
    return out


def iso_format(corpus: Corpus) -> None:
    for s in format_strings():
        value = D.date_parse_utc(s)
        assert value is not None, s
        corpus.add("iso-format", "utc", {
            "op": "Date.parse, the Date Time String Format",
            "in": f"str:{string_value(s)[4:]}",
            "out": number_out(value),
            "prov": prov("sec-date-time-string-format", "sec-expanded-years", "sec-date.parse"),
            "script": f"Date.parse({lit(s)})",
        })
    for s in local_format_strings():
        parsed = D.parse_format(s)
        assert parsed is not None and parsed[0] == "local", s
        year, month, dday, hour, minute, second, milli, _ = parsed[1]
        tv = D.format_time_value(parsed[1])
        y, m, d = int(D.year_from_time(tv)), int(D.month_from_time(tv)), int(D.date_from_time(tv))
        expected = f"{y},{m},{d},{hour},{minute},{second},{milli}"
        corpus.add("iso-format", "local", {
            "op": "Date.parse, a date-time form without an offset",
            "in": f"str:{string_value(s)[4:]}",
            "out": text_out(expected),
            "prov": prov("sec-date-time-string-format", "sec-date.parse", "sec-utc-t", "sec-localtime"),
            "script": (f"var d = new Date({lit(s)}); [d.getFullYear(), d.getMonth(), d.getDate(), d.getHours(), "
                       "d.getMinutes(), d.getSeconds(), d.getMilliseconds()].join()"),
            "note": "a local time put in and read back, at half past noon, so the outcome holds in every time zone",
        })


# --------------------------------------------------------------------------------------------------
# Each engine's dialect.

def round_trip_values() -> list[float]:
    values = [-D.MAX_TIME, D.MAX_TIME]
    for y in (-271820, -100000, -1, 0, 1, 50, 99, 100, 999, 1000, 1883, 1884, 1970, 2000, 2024, 9999, 10000, 100000,
              275759):
        values.append(utc(y, 0, 1))
        values.append(utc(y, 6, 15, 13, 45, 30))
    return values


def slash_dates() -> list[tuple[str, str]]:
    """Broiler.JS's numeric slash dates, with the reading its heuristic gives each (DateParser's
    TryParseNumericSlashDate, which decision SJL-D-17 keeps in Broiler.JS): a first field above 31 is the
    year (yy/mm/dd), otherwise the order is mm/dd/yy; a year below 100 is 1950 to 2049; the date is local
    midnight; a field out of range, a year past 9999 or a day its month does not have is an invalid date."""
    texts = ["3/4/21", "3/4/2021", "12/31/99", "1/1/49", "1/1/50", "13/1/2021", "1/32/2021", "32/1/1", "1999/12/31",
             "2021/2/29", "2020/2/29", "  3/4/21  ", "123456/1/1", "0/1/1", "1/1/0", "1/1/100", "1/1/9999",
             "12/31/9999", "2/29/2000", "2/29/1900", "9999/12/31", "10000/1/1"]
    out = []
    for text in texts:
        import re
        match = re.fullmatch(r"\s*(\d{1,6})/(\d{1,2})/(\d{1,4})\s*", text)
        assert match
        n1, n2, n3 = (int(g) for g in match.groups())
        year, month, dday = (n1, n2, n3) if n1 > 31 else (n3, n1, n2)
        if year < 100:
            year += 1900 if year >= 50 else 2000
        valid = 1 <= month <= 12 and 1 <= year <= 9999 and 1 <= dday <= D.iso_days_in_month(year, month)
        out.append((text, f"{year},{month - 1},{dday},0,0" if valid else "NaN,NaN,NaN,NaN,NaN"))
    return out


def dialect(corpus: Corpus) -> None:
    for t in round_trip_values():
        for method, clause_id in (("toUTCString", "sec-date.prototype.toutcstring"),
                                  ("toISOString", "sec-date.prototype.toisostring")):
            corpus.add("dialect", "round-trip", {
                "op": f"Date.parse of {method}",
                "in": ins(t),
                "out": number_out(t),
                "prov": prov("sec-date.parse", clause_id),
                "script": f"Date.parse(new Date({js(t)}).{method}())",
                "note": "Date.parse reads back the engine's own renderings of a time value whose milliseconds are zero (21.4.3.2)",
            })
        if 1884 <= D.year_from_time(t) <= 9999:
            corpus.add("dialect", "round-trip", {
                "op": "Date.parse of toString",
                "in": ins(t),
                "out": number_out(t),
                "prov": prov("sec-date.parse", "sec-date.prototype.tostring"),
                "script": f"Date.parse(new Date({js(t)}).toString())",
                "note": ("Date.parse reads back the engine's own renderings (21.4.3.2); from 1884, where every zone "
                         "this corpus runs in has an offset of whole minutes"),
            })
    invalid = "NaN,NaN,NaN,NaN,NaN"
    for text, js_reading in slash_dates():
        corpus.add("dialect", "slash-date", {
            "op": "Date.parse, a numeric slash date",
            "in": f"str:{string_value(text)[4:]}",
            "out": text_out(invalid),
            "class": "impl-defined",
            "per-engine.broiler-js": text_out(js_reading),
            "per-engine.broiler-vm": text_out(invalid),
            "prov": prov("sec-date.parse"),
            "script": (f"var d = new Date({lit(text)}); "
                       "[d.getFullYear(), d.getMonth(), d.getDate(), d.getHours(), d.getMinutes()].join()"),
            "note": ("outside the format, so implementation-defined; out is the strict dialect's answer. Broiler.JS "
                     "reads it by its slash-date heuristic, which decision SJL-D-17 keeps (docs/roadmap/SharedLibraries.md, "
                     "the Date area's record); Broiler.VM's dialect reads no slash and gives an invalid date, as "
                     "JSD-0072 keeps it"),
        })



def outside_format_strings() -> list[tuple[str, str, float, float]]:
    """Corpus revision 16: one string outside the Date Time String Format for each kind of class (b) difference
    bundle sjl-6-001's reference-arm comparison found (docs/evidence/sjl-6-001/reference-arms-run/), with Broiler.JS's
    reading and the reading of Broiler.VM's dialect, each computed here from the reading's description, not taken
    from either engine. Every string is read as UTC by whichever grammar reads it, so no expectation depends on
    the zone. NaN is an invalid date. Revision 17: Broiler.JS's reading is the one its cut-over, milestone SJL-6.5,
    commits it to, which reads the strict dialect, then its own grammar, then Broiler.VM's dialect; it changes for
    the two strings its own grammar refuses, which Broiler.VM's dialect reads."""
    nan = math.nan
    either = utc(1, 0, 1, 5, 30)  # 0001-01-01T00:00 at the offset -05:30
    year_zero = utc(0)
    return [
        # A day past its month's length (correspondence row P5): Broiler.VM's dialect carries it into the next
        # month, as MakeDay does; Broiler.JS's grammar refuses it, so since its cut-over Broiler.JS reads it the
        # same way, through that dialect.
        ("0000-02-30T00:00Z", "a day past its month's length (P5)", utc(0, 1, 30), utc(0, 1, 30)),
        # A fraction of other than three digits (P4): Broiler.JS's grammar reads it; Broiler.VM's dialect refuses it.
        # Each kind is written as the classifier, scripts/conformance/classify_date_arms.py, names it.
        ("0001-01-01T00:00:00.-05:30", "a fraction of 0 digits (P4)", either, nan),
        ("0000T00:00:00.0Z", "a fraction of 1 digits (P4)", year_zero, nan),
        ("0000T00:00:00.00Z", "a fraction of 2 digits (P4)", year_zero, nan),
        ("0000T00:00:00.0000Z", "a fraction of 4 digits (P4)", year_zero, nan),
        ("0000T00:00:00.000000000Z", "a fraction of 9 digits (P4)", year_zero, nan),
        # The separator and the offset (P6): Broiler.JS's grammar reads a lower-case t, a space for the T and an
        # offset without its colon; Broiler.VM's dialect refuses each.
        ("0001-01-01t00:00-05:30", "a lower-case t (P6)", either, nan),
        ("0001-01-01 00:00-05:30", "a space for the T", either, nan),
        ("0001-01-01T00:00-0530", "an offset without its colon (P6)", either, nan),
        # A signed year outside the renderings (P9's reading): Broiler.VM's dialect reads toString's shape with
        # the year -5 written unpadded, which no rendering writes; Broiler.JS's grammar refuses it, so since its
        # cut-over Broiler.JS reads it through that dialect.
        ("Thu Jan 01 -5 00:00:00 GMT+0000 (Coordinated Universal Time)", "a signed year outside the renderings (P9's reading)",
         utc(-5), utc(-5)),
    ]


def outside_format(corpus: Corpus) -> None:
    note = ("outside the format, so implementation-defined; out is the strict dialect's answer. Broiler.JS's expectation "
            "is its reading since its cut-over, milestone SJL-6.5, which reads the strict dialect, then its own grammar, "
            "then Broiler.VM's dialect, as JSD-0072 orders them. Broiler.VM's is its dialect's reading as "
            "Broiler.ECMAScript.Algorithms ports it (EcmaDateParser, DateParseDialect.BroilerVM), which its cut-over, "
            "milestone SJL-6.4, adopts. The kind is one bundle sjl-6-001's reference-arm comparison classified (b): ")
    for text, kind, js_reading, vm_reading in outside_format_strings():
        corpus.add("dialect", "outside-format", {
            "op": "Date.parse, a string outside the format",
            "in": f"str:{string_value(text)[4:]}",
            "out": number_out(math.nan),
            "class": "impl-defined",
            "per-engine.broiler-js": number_out(js_reading),
            "per-engine.broiler-vm": number_out(vm_reading),
            "prov": prov("sec-date.parse"),
            "script": f"Date.parse({lit(text)})",
            "note": note + kind,
        }, since=OUTSIDE_FORMAT_REVISION)
    # A leading or trailing space: Broiler.JS's grammar reads a date-only form with one as local midnight, so the
    # vector reads the local fields back, which are the same in every zone; Broiler.VM's dialect refuses it.
    invalid = "NaN,NaN,NaN,NaN,NaN"
    for text in (" 2000-01-28", "2000-01-28 "):
        corpus.add("dialect", "outside-format", {
            "op": "Date.parse, a string outside the format, its local fields",
            "in": f"str:{string_value(text)[4:]}",
            "out": text_out(invalid),
            "class": "impl-defined",
            "per-engine.broiler-js": text_out("2000,0,28,0,0"),
            "per-engine.broiler-vm": text_out(invalid),
            "prov": prov("sec-date.parse"),
            "script": (f"var d = new Date({lit(text)}); "
                       "[d.getFullYear(), d.getMonth(), d.getDate(), d.getHours(), d.getMinutes()].join()"),
            "note": note + "a leading or trailing space",
        }, since=OUTSIDE_FORMAT_REVISION)

# --------------------------------------------------------------------------------------------------
# The renderings.

def rendering_values() -> list[float]:
    values = [D.MAX_TIME, -D.MAX_TIME, 0.0, -1.0, utc(1969, 11, 31, 23, 59, 59, 999)]
    for y in (-271820, -100000, -1000, -101, -1, 0, 1, 9, 10, 99, 100, 999, 1000, 1970, 2024, 9999, 10000, 99999,
              100000, 275759):
        values.append(utc(y, 1, 29 if D.iso_leap(y) else 28, 7, 8, 9, 10))
    return values


def renderings(corpus: Corpus) -> None:
    for t in rendering_values():
        corpus.add("renderings", "utc-string", {
            "op": "Date.prototype.toUTCString",
            "in": ins(t),
            "out": text_out(D.to_utc_string(t)),
            "prov": prov("sec-date.prototype.toutcstring", "sec-timestring"),
            "script": f"new Date({js(t)}).toUTCString()",
        })
        corpus.add("renderings", "iso-string", {
            "op": "Date.prototype.toISOString",
            "in": ins(t),
            "out": text_out(D.to_iso_string(t)),
            "prov": prov("sec-date.prototype.toisostring", "sec-date-time-string-format", "sec-expanded-years"),
            "script": f"new Date({js(t)}).toISOString()",
        })
        corpus.add("renderings", "iso-string", {
            "op": "Date.prototype.toJSON",
            "in": ins(t),
            "out": text_out(D.to_iso_string(t)),
            "prov": prov("sec-date.prototype.tojson", "sec-date.prototype.toisostring"),
            "script": f"new Date({js(t)}).toJSON()",
        })
    for method, out in (("toString", text_out("Invalid Date")), ("toDateString", text_out("Invalid Date")),
                        ("toTimeString", text_out("Invalid Date")), ("toUTCString", text_out("Invalid Date")),
                        ("toISOString", "error: RangeError"), ("toJSON", "value: null")):
        corpus.add("renderings", "invalid", {
            "op": f"Date.prototype.{method}, an invalid date",
            "in": ins(D.NAN),
            "out": out,
            "prov": prov("sec-date.prototype." + method.lower(), "sec-todatestring"),
            "script": f"new Date(NaN).{method}()",
        })
    # Local renderings: a local date put in and rendered, the zone's part cut off.
    for y, m, d in ((2024, 1, 29), (2021, 6, 4), (1970, 0, 1), (1, 0, 1), (0, 0, 1), (-1, 11, 31), (-271820, 5, 15),
                    (10000, 0, 1), (275759, 5, 15), (99, 11, 31), (100, 0, 1), (999, 6, 15), (-10000, 2, 1)):
        tv = utc(y, m, d, 12, 30, 15)
        date = D.date_string(tv)
        for method, expected in (("toDateString()", date), ("toString().split(\" GMT\")[0]", f"{date} 12:30:15"),
                                 ("toTimeString().split(\" GMT\")[0]", "12:30:15")):
            corpus.add("renderings", "local-string", {
                "op": "Date.prototype." + method.split("(")[0] + ", a local date",
                "in": f"{f64(float(y))} {f64(float(m))} {f64(float(d))}",
                "out": text_out(expected),
                "prov": prov("sec-date.prototype." + method.split("(")[0].lower(), "sec-datestring", "sec-timestring",
                             "sec-todatestring", "sec-localtime"),
                "script": f"var d = new Date(2000, 0, 1, 12, 30, 15); d.setFullYear({y}, {m}, {d}); d.{method}",
                "note": "a local date put in and rendered without its zone, so the outcome holds in every time zone",
            })
    for args in ("2021, 0, 15, 12", "2021, 6, 15, 12", "1990, 3, 1, 12", "2050, 9, 31, 12"):
        for method in ("toString", "toTimeString"):
            corpus.add("renderings", "zone-text", {
                "op": f"Date.prototype.{method}, the zone's form",
                "in": f"str:{string_value(args)[4:]}",
                "out": bool_out(True),
                "prov": prov("sec-timezoneestring", "sec-date.prototype." + method.lower()),
                "script": f"/ GMT[+-][0-9]{{4}}( \\(.+\\))?$/.test(new Date({args}).{method}())",
                "note": "TimeZoneString's sign is U+002B or U+002D and its name optional, in every zone (correspondence row R3)",
            })


# --------------------------------------------------------------------------------------------------
# Local time, through relations that hold in every time zone.

def local_time(corpus: Corpus) -> None:
    instants = []
    for y in (1980, 2007, 2021, 2038, 2100):
        for m in range(12):
            for d in (1, 9, 15):
                for h in (1, 7, 12, 19):
                    instants.append(utc(y, m, d, h, 30))
    for t in instants:
        corpus.add("local-time", "offset", {
            "op": "LocalTime and getTimezoneOffset agree",
            "in": ins(t),
            "out": bool_out(True),
            "prov": prov("sec-localtime", "sec-date.prototype.gettimezoneoffset", "sec-date.utc"),
            "script": (f"var d = new Date({js(t)}); (Date.UTC(d.getFullYear(), d.getMonth(), d.getDate(), d.getHours(), "
                       "d.getMinutes(), d.getSeconds(), d.getMilliseconds()) - d.getTime()) / 60000 === -d.getTimezoneOffset()"),
            "note": "the local fields are LocalTime(t)'s and the offset is (t - LocalTime(t)) / msPerMinute, in every zone",
        })
        corpus.add("local-time", "to-string", {
            "op": "toString and the local fields agree",
            "in": ins(t),
            "out": bool_out(True),
            "prov": prov("sec-todatestring", "sec-localtime", "sec-timezoneestring", "sec-date.prototype.gettimezoneoffset"),
            "script": (f"var d = new Date({js(t)}); var p = function (n) {{ return (n < 10 ? \"0\" : \"\") + n; }}; "
                       "var m = / ([0-9]{2}):([0-9]{2}):([0-9]{2}) GMT([+-])([0-9]{2})([0-9]{2})/.exec(d.toString()); "
                       "m[1] + m[2] + m[3] === p(d.getHours()) + p(d.getMinutes()) + p(d.getSeconds()) && "
                       "(m[4] === \"-\" ? 1 : -1) * (m[5] * 60 + m[6] * 1) === d.getTimezoneOffset()"),
            "note": "toString's clock and offset are LocalTime(t)'s and the zone's at t, in every zone with whole-minute offsets, which every zone has after 1979",
        })
    for y, m, d in ((1970, 0, 1), (2021, 2, 4), (2021, 6, 1), (2024, 1, 29), (2024, 10, 3), (2000, 11, 31), (9999, 11, 31),
                    (2038, 0, 19)):
        date = f"{y:04d}-{m + 1:02d}-{d:02d}"
        corpus.add("local-time", "date-only", {
            "op": "Date.parse, a date-only form is UTC",
            "in": f"str:{string_value(date)[4:]}",
            "out": bool_out(True),
            "prov": prov("sec-date.parse", "sec-date-time-string-format", "sec-date.utc"),
            "script": f"Date.parse({lit(date)}) === Date.UTC({y}, {m}, {d})",
            "note": "a date-only form is a UTC time in every zone (correspondence row P1)",
        })
        corpus.add("local-time", "date-time", {
            "op": "Date.parse, a date-time form is local time",
            "in": f"str:{string_value(date + 'T12:30')[4:]}",
            "out": bool_out(True),
            "prov": prov("sec-date.parse", "sec-date-time-string-format", "sec-date", "sec-utc-t"),
            "script": f"Date.parse({lit(date + 'T12:30')}) === new Date({y}, {m}, {d}, 12, 30).getTime()",
            "note": "a date-time form without an offset and the constructor from components read one local time, in every zone (correspondence row L2)",
        })


# --------------------------------------------------------------------------------------------------
# The civil-day helpers, through Temporal's ISO 8601 calendar.

def civil(corpus: Corpus) -> None:
    week_years = [-271821, -401, -400, -8, -5, -4, -2, -1, 0, 1, 1900, 1969, 1970, 2004, 2020, 2021, 2026, 2100, 9999,
                  275759]
    for year in week_years:
        dates = [(year, 12, d) for d in range(25, 32)] + [(year, 1, d) for d in range(1, 8)]
        for y, m, d in dates:
            if not D.iso_within_limits(y, m, d):
                continue
            week, week_year = D.iso_week_of_year(y, m, d)
            for prop, value in (("weekOfYear", week), ("yearOfWeek", week_year)):
                corpus.add("civil", "iso-week", {
                    "op": f"Temporal.PlainDate.prototype.{prop}",
                    "in": f"int:{y} int:{m} int:{d}",
                    "out": number_out(float(value)),
                    "prov": GENERATOR,
                    "script": f"new Temporal.PlainDate({y}, {m}, {d}).{prop}",
                    "note": "ISOWeekOfYear of the Temporal proposal, which the pinned edition does not hold (correspondence row G2)",
                })
    for year in (-271821, -4, -1, 0, 1, 100, 1900, 2000, 2021, 2024, 9999, 275760):
        dates = [(year, m, 1) for m in range(1, 13)] + [(year, 2, 28), (year, 12, 31)]
        if D.iso_leap(year):
            dates.append((year, 2, 29))
        for y, m, d in dates:
            if not D.iso_within_limits(y, m, d):
                continue
            for prop, value in (("dayOfWeek", D.iso_day_of_week(y, m, d)), ("dayOfYear", D.iso_day_of_year(y, m, d)),
                                ("daysInMonth", D.iso_days_in_month(y, m)), ("daysInYear", D.iso_days_in_year(y))):
                corpus.add("civil", "day-of", {
                    "op": f"Temporal.PlainDate.prototype.{prop}",
                    "in": f"int:{y} int:{m} int:{d}",
                    "out": number_out(float(value)),
                    "prov": GENERATOR,
                    "script": f"new Temporal.PlainDate({y}, {m}, {d}).{prop}",
                })
            corpus.add("civil", "day-of", {
                "op": "Temporal.PlainDate.prototype.inLeapYear",
                "in": f"int:{y} int:{m} int:{d}",
                "out": bool_out(D.iso_leap(y)),
                "prov": GENERATOR,
                "script": f"new Temporal.PlainDate({y}, {m}, {d}).inLeapYear",
            })
    for year in (1972, 1900, 2000, 2100, 1000000000, 1000000100, 1000000128, 1000000400, 999999972, 2000000100,
                 -1000000100, -1000000000):
        for month, dday in ((2, 29), (4, 31)):
            for overflow in ("reject", "constrain"):
                result = D.plain_month_day_from(year, month, dday, overflow)
                corpus.add("civil", "month-day", {
                    "op": "Temporal.PlainMonthDay.from, a year's leap day",
                    "in": f"int:{year} int:{month} int:{dday} str:{string_value(overflow)[4:]}",
                    "out": "error: RangeError" if result is None else text_out(result),
                    "prov": GENERATOR,
                    "script": (f"Temporal.PlainMonthDay.from({{ year: {year}, month: {month}, day: {dday} }}, "
                               f"{{ overflow: {lit(overflow)} }}).toString()"),
                    "note": "the date is regulated in the year given, whose leap-ness decides it (correspondence row G3)",
                })
    start = (2000, 1, 1)
    base = D.epoch_days(*start)
    to_max = D.epoch_days(*D.ISO_MAX) - base
    to_min = D.epoch_days(*D.ISO_MIN) - base
    for n in (1, -1, 28, -28, 365, -365, 366, -366, 36524, -36524, 146097, -146097, 1000000, -1000000, to_max, to_min,
              to_max + 1, to_min - 1):
        target = D.from_epoch_days(base + n)
        within = D.iso_within_limits(*target)
        corpus.add("civil", "add-days", {
            "op": "Temporal.PlainDate.prototype.add, days",
            "in": f"int:{n}",
            "out": text_out(D.iso_date_string(*target)) if within else "error: RangeError",
            "prov": GENERATOR,
            "script": f"new Temporal.PlainDate(2000, 1, 1).add({{ days: {n} }}).toString()",
        })
    pairs = [((1970, 1, 1), (2000, 1, 1)), ((-1, 12, 31), (1, 1, 1)), ((-271821, 4, 19), (275760, 9, 13)),
             ((2000, 2, 28), (2000, 3, 1)), ((1900, 2, 28), (1900, 3, 1)), ((-400, 1, 1), (0, 1, 1))]
    for a, b in pairs:
        corpus.add("civil", "until-days", {
            "op": "Temporal.PlainDate.prototype.until, days",
            "in": " ".join(f"int:{v}" for v in a + b),
            "out": number_out(float(D.epoch_days(*b) - D.epoch_days(*a))),
            "prov": GENERATOR,
            "script": f"new Temporal.PlainDate({a[0]}, {a[1]}, {a[2]}).until(new Temporal.PlainDate({b[0]}, {b[1]}, {b[2]})).days",
        })


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--out", required=True, type=Path)
    arguments = parser.parse_args(argv)
    corpus = Corpus()
    arithmetic(corpus)
    fields(corpus)
    iso_format(corpus)
    dialect(corpus)
    renderings(corpus)
    local_time(corpus)
    civil(corpus)
    outside_format(corpus)
    corpus.classify(DIVERGENCES)
    corpus.write(arguments.out)
    total = sum(len(records) for records in corpus.files.values())
    print(f"{total} vectors in {len(corpus.files)} files")
    return 0


if __name__ == "__main__":
    sys.exit(main())
