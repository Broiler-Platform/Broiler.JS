"""The Date area's reference computation: the specification's algorithms, written from its text.

Milestone SJL-6 of the shared JavaScript libraries programme moves the Date algorithms into
`Broiler.ECMAScript.Algorithms`. This module is the corpus's independent computation of their expected
results (`ref:dateref@1`): every function below follows the clause of ECMA-262 (the corpus's pinned edition)
it names, step by step, and no result is taken from either engine. The civil-day helpers follow the Temporal
proposal's ISO 8601 calendar operations, which the pinned edition does not hold.

**A Number is a Python `float`**: an IEEE 754 binary64 value, whose `+`, `-`, `*` and `/` are the
specification's Number operations, so that a step the specification writes in Number arithmetic is computed
in it. A step it writes on mathematical values (ℝ) is computed on Python integers or `Fraction`s, exactly, and
𝔽 of an exact value is `float()` of it, which rounds to nearest, ties to even.

**Local time is UTC here.** The reference has no time zone: the vectors that read local time are written so
that their outcome does not depend on the zone (a local time put in and read back, or a relation that holds in
every zone), and the reference computes them in UTC. **From milestone SJL-6.3** it also computes `LocalTime` and
`UTC` (21.4.1.25 and 21.4.1.26) in a named zone, and `TimeZoneString` (21.4.4.41.3) at a given offset and name,
for bundle sjl-6-001's reference-arm comparison; no vector reads them. A zone's offsets are read from the
host's tzdata through Python's zoneinfo, or, where the caller passes them, from the offsets an engine's
provider read; `parse_format` gains the strict reading of a day past its month's length, off by default.
"""

from __future__ import annotations

import math
from datetime import datetime, timedelta, timezone
from fractions import Fraction

MS_PER_SECOND = 1000.0
MS_PER_MINUTE = 60000.0
MS_PER_HOUR = 3600000.0
MS_PER_DAY = 86400000.0
MAX_TIME = 8.64e15
NAN = float("nan")

WEEKDAYS = ("Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat")
MONTHS = ("Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec")


class RangeError(Exception):
    """An abrupt completion with a RangeError."""


def finite(x: float) -> bool:
    return math.isfinite(x)


def to_integer_or_infinity(x: float):
    """ToIntegerOrInfinity (7.1.5), for a Number: a mathematical integer, or ±∞ as a float."""
    if math.isnan(x) or x == 0:
        return 0
    if math.isinf(x):
        return x
    return math.trunc(Fraction(x))


def number(v) -> float:
    """𝔽 of a mathematical value: the nearest double, ties to even."""
    return float(v)


def real(x: float):
    """ℝ of a finite Number, exactly."""
    return Fraction(x)


# --------------------------------------------------------------------------------------------------
# 21.4.1.3 to 21.4.1.15: days, years, months and dates.


def day(t: float) -> float:
    """Day (21.4.1.3): 𝔽(floor(ℝ(t / msPerDay))), the division being Number division."""
    return number(math.floor(real(t / MS_PER_DAY)))


def time_within_day(t: float) -> float:
    """TimeWithinDay (21.4.1.4)."""
    return number(real(t) % real(MS_PER_DAY))


def days_in_year(y: float) -> float:
    """DaysInYear (21.4.1.5)."""
    ry = int(real(y))
    if ry % 400 == 0:
        return 366.0
    if ry % 100 == 0:
        return 365.0
    if ry % 4 == 0:
        return 366.0
    return 365.0


def day_from_year_exact(ry: int) -> int:
    """DayFromYear (21.4.1.6), on a mathematical year."""
    return 365 * (ry - 1970) + (ry - 1969) // 4 - (ry - 1901) // 100 + (ry - 1601) // 400


def day_from_year(y: float) -> float:
    return number(day_from_year_exact(int(real(y))))


def time_from_year(y: float) -> float:
    """TimeFromYear (21.4.1.7): msPerDay × DayFromYear(y), a Number multiplication."""
    return MS_PER_DAY * day_from_year(y)


def year_from_time(t: float) -> float:
    """YearFromTime (21.4.1.8): the largest integral Number y such that TimeFromYear(y) ≤ t."""
    y = math.floor(real(t) / (real(MS_PER_DAY) * Fraction(146097, 400))) + 1970
    while time_from_year(number(y)) > t:
        y -= 1
    while time_from_year(number(y + 1)) <= t:
        y += 1
    return number(y)


def in_leap_year(t: float) -> float:
    """InLeapYear (21.4.1.9)."""
    return 1.0 if days_in_year(year_from_time(t)) == 366.0 else 0.0


def day_within_year(t: float) -> float:
    """DayWithinYear (21.4.1.10)."""
    return day(t) - day_from_year(year_from_time(t))


MONTH_STARTS = (31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334)


def month_from_time(t: float) -> float:
    """MonthFromTime (21.4.1.11)."""
    leap = in_leap_year(t)
    within = day_within_year(t)
    if within < 31.0:
        return 0.0
    for month, start in enumerate(MONTH_STARTS[1:], 1):
        if within < start + leap:
            return float(month)
    assert within < 365.0 + leap
    return 11.0


DATE_OFFSETS = (None, 30.0, 58.0, 89.0, 119.0, 150.0, 180.0, 211.0, 242.0, 272.0, 303.0, 333.0)


def date_from_time(t: float) -> float:
    """DateFromTime (21.4.1.12)."""
    leap = in_leap_year(t)
    within = day_within_year(t)
    month = int(month_from_time(t))
    if month == 0:
        return within + 1.0
    if month == 1:
        return within - 30.0
    return within - DATE_OFFSETS[month] - leap


def week_day(t: float) -> float:
    """WeekDay (21.4.1.13)."""
    return number(real(day(t) + 4.0) % 7)


def hour_from_time(t: float) -> float:
    """HourFromTime (21.4.1.14)."""
    return number(math.floor(real(t / MS_PER_HOUR)) % 24)


def min_from_time(t: float) -> float:
    """MinFromTime (21.4.1.15)."""
    return number(math.floor(real(t / MS_PER_MINUTE)) % 60)


def sec_from_time(t: float) -> float:
    """SecFromTime (21.4.1.16)."""
    return number(math.floor(real(t / MS_PER_SECOND)) % 60)


def ms_from_time(t: float) -> float:
    """msFromTime (21.4.1.17)."""
    return number(real(t) % real(MS_PER_SECOND))


# --------------------------------------------------------------------------------------------------
# 21.4.1.27 to 21.4.1.31: making times, days and dates.


def make_time(hour: float, minute: float, sec: float, ms: float) -> float:
    """MakeTime (21.4.1.27), in Number arithmetic in the clause's order."""
    if not (finite(hour) and finite(minute) and finite(sec) and finite(ms)):
        return NAN
    h = number(to_integer_or_infinity(hour))
    m = number(to_integer_or_infinity(minute))
    s = number(to_integer_or_infinity(sec))
    milli = number(to_integer_or_infinity(ms))
    return ((h * MS_PER_HOUR + m * MS_PER_MINUTE) + s * MS_PER_SECOND) + milli


def make_day(year: float, month: float, date: float) -> float:
    """MakeDay (21.4.1.28).

    Step 8 finds a time value t whose year is ym, whose month is mn and whose date is 1. Such a t is an
    integral number of days times msPerDay; Day(t) is that number of days, which is computed exactly here.
    The vectors keep it within the integers a Number holds exactly, and keep every result whose t could not
    be found within a range TimeClip refuses whatever MakeDay answers.
    """
    if not (finite(year) and finite(month) and finite(date)):
        return NAN
    y = number(to_integer_or_infinity(year))
    m = number(to_integer_or_infinity(month))
    dt = number(to_integer_or_infinity(date))
    ym = y + number(math.floor(real(m) / 12))
    if not finite(ym):
        return NAN
    mn = int(real(m) % 12)
    ry = int(real(ym))
    days = day_from_year_exact(ry) + (0 if mn == 0 else MONTH_STARTS[mn - 1]) + (
        1 if mn >= 2 and days_in_year(ym) == 366.0 else 0)
    try:
        reachable = finite(float(days * 86_400_000))
    except OverflowError:
        reachable = False
    if not reachable:
        return NAN
    return number(days) + dt - 1.0


def make_date(day_: float, time: float) -> float:
    """MakeDate (21.4.1.29)."""
    if not (finite(day_) and finite(time)):
        return NAN
    tv = day_ * MS_PER_DAY + time
    return tv if finite(tv) else NAN


def make_full_year(year: float) -> float:
    """MakeFullYear (21.4.1.30)."""
    if math.isnan(year):
        return NAN
    truncated = to_integer_or_infinity(year)
    if not isinstance(truncated, float) and 0 <= truncated <= 99:
        return 1900.0 + number(truncated)
    return number(truncated)


def time_clip(time: float) -> float:
    """TimeClip (21.4.1.31)."""
    if not finite(time):
        return NAN
    if abs(real(time)) > Fraction(8_640_000_000_000_000):
        return NAN
    return number(to_integer_or_infinity(time))


def date_utc(*args: float) -> float:
    """Date.UTC (21.4.3.4), its arguments already Numbers."""
    y = args[0]
    m = args[1] if len(args) > 1 else 0.0
    dt = args[2] if len(args) > 2 else 1.0
    h = args[3] if len(args) > 3 else 0.0
    minute = args[4] if len(args) > 4 else 0.0
    s = args[5] if len(args) > 5 else 0.0
    milli = args[6] if len(args) > 6 else 0.0
    yr = make_full_year(y)
    return time_clip(make_date(make_day(yr, m, dt), make_time(h, minute, s, milli)))


# --------------------------------------------------------------------------------------------------
# The setters (21.4.4.20 to 21.4.4.28), on the UTC time scale.


def set_utc_hours(t: float, h: float, m: float | None = None, s: float | None = None, milli: float | None = None) -> float:
    """Date.prototype.setUTCHours (21.4.4.24): its result, the arguments already Numbers."""
    if math.isnan(t):
        return NAN
    m = min_from_time(t) if m is None else m
    s = sec_from_time(t) if s is None else s
    milli = ms_from_time(t) if milli is None else milli
    return time_clip(make_date(day(t), make_time(h, m, s, milli)))


def set_utc_full_year(t: float, y: float, m: float | None = None, dt: float | None = None) -> float:
    """Date.prototype.setUTCFullYear (21.4.4.21)."""
    if math.isnan(t):
        t = 0.0
    m = month_from_time(t) if m is None else m
    dt = date_from_time(t) if dt is None else dt
    return time_clip(make_date(make_day(y, m, dt), time_within_day(t)))


def set_utc_month(t: float, m: float, dt: float | None = None) -> float:
    """Date.prototype.setUTCMonth (21.4.4.28)."""
    if math.isnan(t):
        return NAN
    dt = date_from_time(t) if dt is None else dt
    return time_clip(make_date(make_day(year_from_time(t), m, dt), time_within_day(t)))


def set_utc_date(t: float, dt: float) -> float:
    """Date.prototype.setUTCDate (21.4.4.20)."""
    if math.isnan(t):
        return NAN
    return time_clip(make_date(make_day(year_from_time(t), month_from_time(t), dt), time_within_day(t)))


def set_utc_milliseconds(t: float, ms: float) -> float:
    """Date.prototype.setUTCMilliseconds (21.4.4.25)."""
    if math.isnan(t):
        return NAN
    return time_clip(make_date(day(t), make_time(hour_from_time(t), min_from_time(t), sec_from_time(t), ms)))


# --------------------------------------------------------------------------------------------------
# 21.4.1.32: the Date Time String Format.


def _digits(text: str, at: int, count: int):
    piece = text[at:at + count]
    if len(piece) != count or not all("0" <= c <= "9" for c in piece):
        return None
    return int(piece)


def parse_format(text: str, strict_days: bool = False):
    """Reads `text` as an instance of the Date Time String Format (21.4.1.32).

    Returns None when the text is not a valid instance, and otherwise ("utc", fields) or ("local", fields),
    the fields being (year, month, day, hour, minute, second, millisecond, offset minutes); a date-only form is
    UTC and a date-time form without an offset local (21.4.3.2). Out-of-bounds elements make the text not an
    instance, and so does -000000. A day is out of bounds past 31; with `strict_days`, past its month's length
    (the "illegal element values" of 21.4.1.32, as the library's strict dialect reads them, decision JSD-0072).
    """
    at = 0
    if text[:1] in ("+", "-"):
        year = _digits(text, 1, 6)
        if year is None:
            return None
        if text[0] == "-":
            if year == 0:
                return None
            year = -year
        at = 7
    else:
        year = _digits(text, 0, 4)
        if year is None:
            return None
        at = 4
    month, dday = 1, 1
    if text[at:at + 1] == "-":
        month = _digits(text, at + 1, 2)
        if month is None:
            return None
        at += 3
        if text[at:at + 1] == "-":
            dday = _digits(text, at + 1, 2)
            if dday is None:
                return None
            at += 3
    if not (1 <= month <= 12 and 1 <= dday <= 31):
        return None
    if strict_days and dday > iso_days_in_month(year, month):
        return None
    if at == len(text):
        return ("utc", (year, month, dday, 0, 0, 0, 0, 0))
    if text[at] != "T":
        return None
    hour = _digits(text, at + 1, 2)
    if hour is None or text[at + 3:at + 4] != ":":
        return None
    minute = _digits(text, at + 4, 2)
    if minute is None:
        return None
    at += 6
    second, milli = 0, 0
    if text[at:at + 1] == ":":
        second = _digits(text, at + 1, 2)
        if second is None:
            return None
        at += 3
        if text[at:at + 1] == ".":
            milli = _digits(text, at + 1, 3)
            if milli is None:
                return None
            at += 4
    if hour > 24 or minute > 59 or second > 59 or (hour == 24 and (minute, second, milli) != (0, 0, 0)):
        return None
    kind, offset = "local", 0
    if at < len(text):
        if text[at] == "Z" and at + 1 == len(text):
            kind = "utc"
        elif text[at] in "+-" and len(text) == at + 6 and text[at + 3] == ":":
            oh, om = _digits(text, at + 1, 2), _digits(text, at + 4, 2)
            if oh is None or om is None or oh > 23 or om > 59:
                return None
            kind, offset = "utc", (oh * 60 + om) * (1 if text[at] == "+" else -1)
        else:
            return None
    return (kind, (year, month, dday, hour, minute, second, milli, offset))


def format_time_value(fields) -> float:
    """The time value of a format instance's fields, its offset applied, TimeClip'd (21.4.3.2, 21.4.1.32.1)."""
    year, month, dday, hour, minute, second, milli, offset = fields
    tv = make_date(make_day(float(year), float(month - 1), float(dday)),
                   make_time(float(hour), float(minute), float(second), float(milli)))
    return time_clip(tv - offset * MS_PER_MINUTE)


def date_parse_utc(text: str) -> float | None:
    """Date.parse of a format instance whose result does not depend on the zone: its time value; None for a
    local date-time form; NaN for a string that is not an instance (21.4.3.2: out-of-bounds elements and an
    expanded year outside the range of a time value give NaN, and the vectors give no other non-instance)."""
    parsed = parse_format(text)
    if parsed is None:
        return NAN
    kind, fields = parsed
    if kind == "local":
        return None
    return format_time_value(fields)


# --------------------------------------------------------------------------------------------------
# 21.4.4.41: the renderings.


def padded(value: float, width: int) -> str:
    """ToZeroPaddedDecimalString."""
    return str(int(value)).rjust(width, "0")


def date_string(tv: float) -> str:
    """DateString (21.4.4.41.2)."""
    yv = year_from_time(tv)
    sign = "" if yv >= 0 else "-"
    return (f"{WEEKDAYS[int(week_day(tv))]} {MONTHS[int(month_from_time(tv))]} "
            f"{padded(date_from_time(tv), 2)} {sign}{padded(abs(yv), 4)}")


def time_string(tv: float) -> str:
    """TimeString (21.4.4.41.1)."""
    return f"{padded(hour_from_time(tv), 2)}:{padded(min_from_time(tv), 2)}:{padded(sec_from_time(tv), 2)} GMT"


def to_utc_string(tv: float) -> str:
    """Date.prototype.toUTCString (21.4.4.43)."""
    if math.isnan(tv):
        return "Invalid Date"
    yv = year_from_time(tv)
    sign = "" if yv >= 0 else "-"
    return (f"{WEEKDAYS[int(week_day(tv))]}, {padded(date_from_time(tv), 2)} {MONTHS[int(month_from_time(tv))]} "
            f"{sign}{padded(abs(yv), 4)} {time_string(tv)}")


def to_iso_string(tv: float) -> str:
    """Date.prototype.toISOString (21.4.4.36): the Date Time String Format with every element and "Z"."""
    if math.isnan(tv):
        raise RangeError()
    yv = int(year_from_time(tv))
    year = f"{yv:04d}" if 0 <= yv <= 9999 else ("+" if yv > 0 else "-") + f"{abs(yv):06d}"
    return (f"{year}-{padded(month_from_time(tv) + 1, 2)}-{padded(date_from_time(tv), 2)}"
            f"T{padded(hour_from_time(tv), 2)}:{padded(min_from_time(tv), 2)}:{padded(sec_from_time(tv), 2)}"
            f".{padded(ms_from_time(tv), 3)}Z")


# --------------------------------------------------------------------------------------------------
# The ISO 8601 calendar of the Temporal proposal: the civil-day helpers.

ISO_MIN = (-271821, 4, 19)
ISO_MAX = (275760, 9, 13)


def iso_leap(year: int) -> bool:
    """MathematicalInLeapYear over a year (the proposal's ISO 8601 calendar, as 21.4.1.5 counts it)."""
    return year % 4 == 0 and (year % 100 != 0 or year % 400 == 0)


def iso_days_in_year(year: int) -> int:
    return 366 if iso_leap(year) else 365


def iso_days_in_month(year: int, month: int) -> int:
    """ISODaysInMonth."""
    if month == 2:
        return 29 if iso_leap(year) else 28
    return 30 if month in (4, 6, 9, 11) else 31


def iso_valid(year: int, month: int, dday: int) -> bool:
    """IsValidISODate."""
    return 1 <= month <= 12 and 1 <= dday <= iso_days_in_month(year, month)


def epoch_days(year: int, month: int, dday: int) -> int:
    """The days from 1970-01-01 to an ISO date, through 21.4.1.6's DayFromYear."""
    return day_from_year_exact(year) + sum(iso_days_in_month(year, m) for m in range(1, month)) + dday - 1


def from_epoch_days(days: int) -> tuple[int, int, int]:
    """The ISO date `days` after 1970-01-01."""
    year = math.floor(Fraction(days, 1) / Fraction(146097, 400)) + 1970
    while day_from_year_exact(year) > days:
        year -= 1
    while day_from_year_exact(year + 1) <= days:
        year += 1
    left = days - day_from_year_exact(year)
    month = 1
    while left >= iso_days_in_month(year, month):
        left -= iso_days_in_month(year, month)
        month += 1
    return year, month, left + 1


def iso_day_of_week(year: int, month: int, dday: int) -> int:
    """ISODayOfWeek: Monday 1 to Sunday 7."""
    weekday = (epoch_days(year, month, dday) + 4) % 7  # Sunday 0, as WeekDay (21.4.1.13)
    return 7 if weekday == 0 else weekday


def iso_day_of_year(year: int, month: int, dday: int) -> int:
    """ISODayOfYear."""
    return epoch_days(year, month, dday) - epoch_days(year, 1, 1) + 1


def iso_week_of_year(year: int, month: int, dday: int) -> tuple[int, int]:
    """ISOWeekOfYear, step by step: the week and its year."""
    wednesday, thursday, friday, saturday, days_in_week, max_week = 3, 4, 5, 6, 7, 53
    day_of_year = iso_day_of_year(year, month, dday)
    day_of_week = iso_day_of_week(year, month, dday)
    week = (day_of_year + days_in_week - day_of_week + wednesday) // days_in_week
    if week < 1:
        jan1 = iso_day_of_week(year, 1, 1)
        if jan1 == friday:
            return max_week, year - 1
        if jan1 == saturday and iso_leap(year - 1):
            return max_week, year - 1
        return max_week - 1, year - 1
    if week == max_week:
        days_later = iso_days_in_year(year) - day_of_year
        days_after_thursday = thursday - day_of_week
        if days_later < days_after_thursday:
            return 1, year + 1
    return week, year


def iso_within_limits(year: int, month: int, dday: int) -> bool:
    """ISODateWithinLimits for a date (its time at noon is within the limits exactly when this holds)."""
    return ISO_MIN <= (year, month, dday) <= ISO_MAX


def regulate_iso_date(year: int, month: int, dday: int, overflow: str):
    """RegulateISODate: the date, constrained or refused (None for a RangeError)."""
    if overflow == "reject":
        return (year, month, dday) if iso_valid(year, month, dday) else None
    month = min(max(month, 1), 12)
    return year, month, min(max(dday, 1), iso_days_in_month(year, month))


def plain_month_day_from(year: int, month: int, dday: int, overflow: str):
    """Temporal.PlainMonthDay.from({year, month, day}, {overflow}) in the ISO 8601 calendar: its toString(),
    or None for a RangeError. CalendarMonthDayToISOReferenceDate regulates the date in the given year and
    keeps 1972, the reference year, as the result's."""
    result = regulate_iso_date(year, month, dday, overflow)
    if result is None:
        return None
    return f"{result[1]:02d}-{result[2]:02d}"


def iso_date_string(year: int, month: int, dday: int) -> str:
    """A PlainDate's toString() in the ISO 8601 calendar: an expanded year outside 0 to 9999."""
    y = f"{year:04d}" if 0 <= year <= 9999 else ("+" if year > 0 else "-") + f"{abs(year):06d}"
    return f"{y}-{month:02d}-{dday:02d}"


# --------------------------------------------------------------------------------------------------
# Local time in a named zone (21.4.1.25, 21.4.1.26) and TimeZoneString (21.4.4.41.3), for bundle sjl-6-001's
# reference-arm comparison. A time is an integral number of milliseconds since the epoch.

_EPOCH = datetime(1970, 1, 1, tzinfo=timezone.utc)
_EPOCH_NAIVE = datetime(1970, 1, 1)
FOUR_CENTURIES_MS = 146097 * 86_400_000
FIRST_MS = day_from_year_exact(1) * 86_400_000
LAST_MS = day_from_year_exact(10000) * 86_400_000 - 1


def _truncated_ms(offset: timedelta) -> int:
    """truncate(offsetNs / 10^6) of an offset."""
    micro = offset // timedelta(microseconds=1)
    return int(micro / 1000) if micro >= 0 else -int(-micro / 1000)


def _into_range(t: int, margin: int = 0) -> tuple[int, int]:
    """A time moved back by whole 400-year steps until it is within datetime's years 1 to 9999 less a margin;
    the zone's rules repeat with the Gregorian calendar every 400 years past its last explicit transition."""
    shift = 0
    while t - shift > LAST_MS - margin:
        shift += FOUR_CENTURIES_MS
    return t - shift, shift


def zone_offset_ms(zone, t: int) -> int:
    """truncate(GetNamedTimeZoneOffsetNanoseconds(zone, t × 10^6) / 10^6) (21.4.1.21), read through zoneinfo:
    within two days of the end of year 9999 or past it at the instant 400 years earlier as often as needed, and
    before year 1's third day at that day's first instant, whose offset is the zone's earliest (two days from
    either end, so that no zone's local date falls outside datetime's years 1 to 9999)."""
    moved, _ = _into_range(t, 2 * 86_400_000)
    moved = max(moved, FIRST_MS + 2 * 86_400_000)
    return _truncated_ms((_EPOCH + timedelta(milliseconds=moved)).astimezone(zone).utcoffset())


def local_time(t: int, zone) -> int:
    """LocalTime (21.4.1.25): t + the offset in force at the instant t."""
    return t + zone_offset_ms(zone, t)


def utc(t: int, zone) -> int:
    """UTC (21.4.1.26) of a local time, through zoneinfo: the offset of the earliest instant the local time
    names, or of the last instant before a transition that skips it, which is PEP 495's fold=0 reading of
    the wall time; before year 1, the zone's earliest offset."""
    moved, shift = _into_range(t, 2 * 86_400_000)
    if moved < FIRST_MS + 2 * 86_400_000:
        return t - zone_offset_ms(zone, FIRST_MS)
    wall = (_EPOCH_NAIVE + timedelta(milliseconds=moved)).replace(tzinfo=zone, fold=0)
    return t - _truncated_ms(wall.utcoffset())


def utc_over(t: int, offset_at) -> int:
    """UTC (21.4.1.26) over an offset function of instants (`offset_at(instant)` in milliseconds), where the
    zone changes its offset at most once within a day either side of t: the possible instants are t - o for
    each offset o in force a day before or after, kept where the offset at t - o is o; the earliest is
    taken, and with none, a skipped local time, the offset a day before, which is the offset before the
    transition."""
    day = 86_400_000
    before, after = offset_at(t - day), offset_at(t + day)
    possible = sorted(t - o for o in {before, after} if offset_at(t - o) == o)
    if possible:
        return possible[0]
    return t - before


def time_zone_string(offset_ms: int, name: str | None) -> str:
    """TimeZoneString (21.4.4.41.3) at an offset: its sign, HourFromTime and MinFromTime of its magnitude, two
    digits each, then " (" + name + ")" when there is a name."""
    magnitude = float(abs(offset_ms))
    text = ("+" if offset_ms >= 0 else "-") + padded(hour_from_time(magnitude), 2) + padded(min_from_time(magnitude), 2)
    return text if not name else text + " (" + name + ")"
