using System;
using System.Numerics;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Temporal;

// Temporal.Duration arithmetic: add / subtract / round / total.
//
// The calendar-independent cases — durations whose years/months/weeks are all zero — are
// implemented exactly here (days are treated as 24-hour periods, the whole computation done in
// BigInteger nanoseconds). The calendar-dependent cases (nonzero years/months/weeks, or a
// `relativeTo` whose day length varies with a time zone) require calendar / DST arithmetic that
// is not yet wired; those paths throw a clear "not yet implemented" error (or a RangeError when
// the spec requires a relativeTo that is absent).
public partial class JSTemporalDuration
{
    private const long NanosecondsPerDay = 86_400_000_000_000;

    private bool HasCalendarUnits => years != 0 || months != 0 || weeks != 0;

    // days treated as 24h, in nanoseconds.
    private BigInteger TimePlusDaysNanoseconds() => TotalNanoseconds();

    private static readonly string[] UnitOrder =
        { "year", "month", "week", "day", "hour", "minute", "second", "millisecond", "microsecond", "nanosecond" };

    private static int UnitIndex(string unit) => System.Array.IndexOf(UnitOrder, unit);

    private static long TimeUnitNanoseconds(string unit) => unit switch
    {
        "day" => NanosecondsPerDay,
        "hour" => 3_600_000_000_000,
        "minute" => 60_000_000_000,
        "second" => 1_000_000_000,
        "millisecond" => 1_000_000,
        "microsecond" => 1_000,
        "nanosecond" => 1,
        _ => 0, // year/month/week have no fixed length
    };

    private static string NormalizeUnit(string u, bool allowAuto) => u switch
    {
        "auto" when allowAuto => "auto",
        "year" or "years" => "year",
        "month" or "months" => "month",
        "week" or "weeks" => "week",
        "day" or "days" => "day",
        "hour" or "hours" => "hour",
        "minute" or "minutes" => "minute",
        "second" or "seconds" => "second",
        "millisecond" or "milliseconds" => "millisecond",
        "microsecond" or "microseconds" => "microsecond",
        "nanosecond" or "nanoseconds" => "nanosecond",
        _ => throw JSEngine.NewRangeError($"Temporal.Duration: invalid unit \"{u}\""),
    };

    // The largest non-zero unit of this duration (defaults to "nanosecond" for a zero duration).
    private string DefaultLargestUnit()
    {
        if (years != 0) return "year";
        if (months != 0) return "month";
        if (weeks != 0) return "week";
        if (days != 0) return "day";
        if (hours != 0) return "hour";
        if (minutes != 0) return "minute";
        if (seconds != 0) return "second";
        if (milliseconds != 0) return "millisecond";
        if (microseconds != 0) return "microsecond";
        return "nanosecond";
    }

    private static bool TryGetRelativeTo(JSValue options, out JSValue relativeTo)
    {
        relativeTo = JSUndefined.Value;
        if (options == null || options.IsUndefined) return false;
        if (options is not JSObject optionsObject)
            throw JSEngine.NewTypeError("Temporal.Duration options must be an object or undefined");
        relativeTo = optionsObject[KeyStrings.GetOrCreate("relativeTo")];
        return relativeTo != null && !relativeTo.IsNullOrUndefined;
    }

    // ── add / subtract ────────────────────────────────────────────────────────────

    private JSValue AddSubtract(JSValue otherValue, JSValue options, int sign)
    {
        var other = (JSTemporalDuration)ToTemporalDuration(otherValue);
        var hasRelativeTo = TryGetRelativeTo(options, out _);

        if (HasCalendarUnits || other.HasCalendarUnits)
        {
            if (!hasRelativeTo)
                throw JSEngine.NewRangeError(
                    "Temporal.Duration.prototype.add/subtract with calendar units (years, months, weeks) requires a relativeTo option");
            throw JSEngine.NewError(
                "Temporal.Duration.prototype.add/subtract with a relativeTo calendar/time-zone is not yet implemented");
        }

        var totalNs = TimePlusDaysNanoseconds() + sign * other.TimePlusDaysNanoseconds();
        var largest = MaxUnit(DefaultLargestUnit(), other.DefaultLargestUnit());
        var result = (JSTemporalDuration)BalanceFromNanoseconds(totalNs, largest);

        // The balanced result must itself be a valid duration (e.g. the combined time must not
        // overflow the 2^53-seconds limit); otherwise this is a RangeError.
        RejectDuration(result.years, result.months, result.weeks, result.days, result.hours,
            result.minutes, result.seconds, result.milliseconds, result.microseconds, result.nanoseconds);
        return result;
    }

    private static string MaxUnit(string a, string b) => UnitIndex(a) <= UnitIndex(b) ? a : b;

    // ── round ─────────────────────────────────────────────────────────────────────

    internal JSValue RoundImpl(JSValue roundTo)
    {
        string smallestUnit = null;
        string largestUnit = "auto";
        var increment = 1d;
        var roundingMode = "halfExpand";
        JSValue options = roundTo;

        if (roundTo == null || roundTo.IsUndefined)
            throw JSEngine.NewTypeError("Temporal.Duration.prototype.round requires an options argument");

        if (roundTo.IsString)
        {
            smallestUnit = NormalizeUnit(roundTo.StringValue, allowAuto: false);
            options = JSUndefined.Value;
        }
        else if (roundTo is JSObject obj)
        {
            var su = obj[KeyStrings.GetOrCreate("smallestUnit")];
            var lu = obj[KeyStrings.GetOrCreate("largestUnit")];
            if (!lu.IsUndefined) largestUnit = NormalizeUnit(lu.StringValue, allowAuto: true);

            var inc = obj[KeyStrings.GetOrCreate("roundingIncrement")];
            if (!inc.IsUndefined)
            {
                var n = inc.DoubleValue;
                if (double.IsNaN(n) || double.IsInfinity(n) || n < 1 || n > 1_000_000_000 || Math.Truncate(n) != n)
                    throw JSEngine.NewRangeError("Temporal.Duration.round: invalid roundingIncrement");
                increment = n;
            }

            var rm = obj[KeyStrings.GetOrCreate("roundingMode")];
            if (!rm.IsUndefined) roundingMode = NormalizeRoundingMode(rm.StringValue);

            if (!su.IsUndefined) smallestUnit = NormalizeUnit(su.StringValue, allowAuto: false);
            if (smallestUnit == null && largestUnit == "auto")
                throw JSEngine.NewRangeError("Temporal.Duration.round requires either smallestUnit or largestUnit");
        }
        else throw JSEngine.NewTypeError("Temporal.Duration.prototype.round requires an options object or string");

        TryResolveRelativeTo(options, out var relZoned, out var relDate);
        smallestUnit ??= "nanosecond";

        if (largestUnit == "auto")
            largestUnit = MaxUnit(DefaultLargestUnit(), smallestUnit);

        // A time-unit smallestUnit caps the rounding increment at the number of those units in the
        // next-larger unit, and the increment must divide it evenly. This validation is independent
        // of the calendar/relativeTo machinery, so run it up front — a bad increment is a RangeError
        // even on the paths that later bail out as "not yet implemented".
        if (UnitIndex(smallestUnit) >= UnitIndex("hour"))
        {
            var maxIncrement = smallestUnit switch
            {
                "hour" => 24L,
                "minute" or "second" => 60L,
                _ => 1000L, // millisecond / microsecond / nanosecond
            };
            TemporalRoundingOptions.ValidateRoundingIncrement((long)increment, maxIncrement, inclusive: false);
        }

        var needsCalendar = HasCalendarUnits || UnitIndex(smallestUnit) < UnitIndex("day") || UnitIndex(largestUnit) < UnitIndex("day");
        if (needsCalendar && relZoned == null && relDate == null)
            throw JSEngine.NewRangeError(
                "Temporal.Duration.prototype.round with calendar units (years, months, weeks) requires a relativeTo option");

        if (relZoned != null)
            return relZoned.RoundDurationRelative(this, smallestUnit, largestUnit, (long)increment, roundingMode);

        if (relDate != null)
            return RoundRelative(relDate, smallestUnit, largestUnit, (long)increment, roundingMode);

        var unitNs = TimeUnitNanoseconds(smallestUnit) * (long)increment;
        var rounded = RoundToIncrement(TimePlusDaysNanoseconds(), unitNs, roundingMode);
        return BalanceFromNanoseconds(rounded, largestUnit);
    }

    // ── total ───────────────────────────────────────────────────────────────────

    internal JSValue TotalImpl(JSValue totalOf)
    {
        string unit;
        JSValue options = totalOf;

        if (totalOf == null || totalOf.IsUndefined)
            throw JSEngine.NewTypeError("Temporal.Duration.prototype.total requires an options argument");

        if (totalOf.IsString)
        {
            unit = NormalizeUnit(totalOf.StringValue, allowAuto: false);
            options = JSUndefined.Value;
        }
        else if (totalOf is JSObject obj)
        {
            var u = obj[KeyStrings.GetOrCreate("unit")];
            if (u.IsUndefined)
                throw JSEngine.NewRangeError("Temporal.Duration.prototype.total requires a unit");
            unit = NormalizeUnit(u.StringValue, allowAuto: false);
        }
        else throw JSEngine.NewTypeError("Temporal.Duration.prototype.total requires an options object or string");

        TryResolveRelativeTo(options, out var relZoned, out var relDate);

        if (HasCalendarUnits || UnitIndex(unit) < UnitIndex("day"))
        {
            if (relZoned == null && relDate == null)
                throw JSEngine.NewRangeError(
                    "Temporal.Duration.prototype.total with calendar units (years, months, weeks) requires a relativeTo option");
        }

        if (relZoned != null)
            return new JSNumber(relZoned.TotalDurationRelative(this, unit));

        if (relDate != null)
            return new JSNumber(TotalRelative(relDate, unit));

        var totalNs = TimePlusDaysNanoseconds();
        var unitNs = TimeUnitNanoseconds(unit);
        var whole = BigInteger.DivRem(totalNs, unitNs, out var remainder);
        var result = (double)whole + (double)remainder / unitNs;
        return new JSNumber(result);
    }

    // ── relativeTo (PlainDate) calendar rounding ──────────────────────────────────
    //
    // Implements RoundDuration / TotalDuration for an ISO (or Gregorian-family) Temporal.PlainDate
    // relativeTo, where a day is exactly 24 hours. The duration's date part is added to relativeTo to
    // find the end date; the time part is folded onto the nanosecond timeline. Calendar units
    // (year/month) are rounded by the spec's "nudge" between the two surrounding calendar boundaries,
    // while week/day/time units have a fixed nanosecond length. A ZonedDateTime relativeTo (DST) or a
    // non-ISO calendar relativeTo is not handled here.
    private static BigInteger FloorDivBig(BigInteger a, BigInteger b)
    {
        var q = BigInteger.DivRem(a, b, out var r);
        if (r != 0 && r.Sign != b.Sign) q -= 1;
        return q;
    }

    private BigInteger TimeComponentsNs()
        => ((((((BigInteger)(long)hours * 60 + (long)minutes) * 60 + (long)seconds) * 1000
              + (long)milliseconds) * 1000 + (long)microseconds) * 1000) + (long)nanoseconds;

    // ToRelativeTemporalObject. Resolves relativeTo to either a ZonedDateTime (DST-aware) or an
    // ISO/Gregorian-family PlainDate (a day = 24 h). Returns false when relativeTo is absent. A non-ISO
    // calendar relativeTo is not yet supported.
    private static bool TryResolveRelativeTo(JSValue options, out JSTemporalZonedDateTime zoned, out JSTemporalPlainDate date)
    {
        zoned = null;
        date = null;
        if (!TryGetRelativeTo(options, out var rel)) return false;

        zoned = JSTemporalZonedDateTime.ToZonedRelative(rel);
        if (zoned != null) return true;

        // A PlainDateTime relativeTo contributes only its date (a fixed 24-hour day).
        if (rel is JSTemporalPlainDateTime pdt)
        {
            if (TemporalCalendarMath.IsNonIso(pdt.calendarId))
                throw JSEngine.NewError($"Temporal.Duration: a relativeTo with the \"{pdt.calendarId}\" calendar is not yet implemented");
            date = JSTemporalPlainDate.FromIso(pdt.isoYear, pdt.isoMonth, pdt.isoDay, pdt.calendarId);
            return true;
        }

        // ToRelativeTemporalObject for a string: validate the relativeTo grammar. An unparsable string,
        // a UTC (Z) designator with no time-zone annotation, or a malformed offset is a RangeError. (A
        // string with a [TimeZone] annotation was already handled by ToZonedRelative above.)
        if (rel.IsString)
        {
            var s = rel.ToString();
            if (!TemporalIsoString.TryParseRelative(s, out var tz, out var hasZ, out var offset))
                throw JSEngine.NewRangeError($"Temporal.Duration: cannot parse relativeTo \"{s}\"");
            if (tz == null && hasZ)
                throw JSEngine.NewRangeError("Temporal.Duration: relativeTo with a UTC (Z) designator requires a time-zone annotation");
            if (offset != null && !TemporalIsoString.IsStrictOffset(offset))
                throw JSEngine.NewRangeError($"Temporal.Duration: invalid UTC offset in relativeTo \"{s}\"");
        }

        date = JSTemporalPlainDate.ToRelativeDate(rel);
        if (TemporalCalendarMath.IsNonIso(date.calendarId))
            throw JSEngine.NewError($"Temporal.Duration: a relativeTo with the \"{date.calendarId}\" calendar is not yet implemented");
        return true;
    }

    // The end of this duration measured from relativeTo, on the nanosecond timeline (epoch days × a
    // 24-hour day plus the time components), and relativeTo's own start in the same units.
    private (BigInteger endNs, BigInteger startNs, long startEpoch) RelativeEndpoints(JSTemporalPlainDate r)
    {
        var startEpoch = JSTemporalPlainDate.EpochDaysFor(r.isoYear, r.isoMonth, r.isoDay);
        var (ey, em, ed) = JSTemporalPlainDate.AddCalendarDate(r.isoYear, r.isoMonth, r.isoDay,
            (long)years, (long)months, (long)weeks, (long)days);
        var endNs = (BigInteger)JSTemporalPlainDate.EpochDaysFor(ey, em, ed) * NanosecondsPerDay + TimeComponentsNs();
        return (endNs, (BigInteger)startEpoch * NanosecondsPerDay, startEpoch);
    }

    // The instant implied by adding the whole duration to relativeTo must land on a representable ISO
    // date; otherwise rounding / totalling it is a RangeError (e.g. a duration carrying ~2^53 seconds of
    // time reaches ~10^11 days, far beyond the ±10^8-day ISO range).
    private static void ValidateRelativeEndInRange(BigInteger endNs)
    {
        if (!JSTemporalPlainDate.IsEpochDayInIsoRange(FloorDivBig(endNs, NanosecondsPerDay)))
            throw JSEngine.NewRangeError("Temporal.Duration: result is out of range");
    }

    private JSValue RoundRelative(JSTemporalPlainDate r, string smallestUnit, string largestUnit, long increment, string roundingMode)
    {
        var (endNs, startNs, startEpoch) = RelativeEndpoints(r);
        ValidateRelativeEndInRange(endNs);

        if (smallestUnit is "year" or "month")
        {
            var roundedUnits = (long)NudgeCalendarUnit(r, endNs, startNs, smallestUnit == "year", increment, roundingMode);
            var (ny, nm, nd) = smallestUnit == "year"
                ? JSTemporalPlainDate.AddCalendarDate(r.isoYear, r.isoMonth, r.isoDay, roundedUnits, 0, 0, 0)
                : JSTemporalPlainDate.AddCalendarDate(r.isoYear, r.isoMonth, r.isoDay, 0, roundedUnits, 0, 0);
            var (fy, fmo, fw, fd) = JSTemporalPlainDate.DiffCalendarDate(r.isoYear, r.isoMonth, r.isoDay, ny, nm, nd, largestUnit);
            return new JSTemporalDuration(fy, fmo, fw, fd, 0, 0, 0, 0, 0, 0, DurationPrototype);
        }

        var smallestNs = (smallestUnit == "week" ? 7 * (BigInteger)NanosecondsPerDay : TimeUnitNanoseconds(smallestUnit)) * increment;
        var roundedNs = RoundToIncrement(endNs - startNs, smallestNs, roundingMode);

        if (UnitIndex(largestUnit) <= UnitIndex("week"))
        {
            // Rebalance the whole-day part into the calendar; keep the sub-day remainder (same sign as
            // the rounded total) as time components.
            var wholeDays = (long)BigInteger.DivRem(roundedNs, NanosecondsPerDay, out var subNs);
            var (ny, nm, nd) = JSTemporalPlainDate.DateFromEpochDays(startEpoch + wholeDays);
            var (fy, fmo, fw, fd) = JSTemporalPlainDate.DiffCalendarDate(r.isoYear, r.isoMonth, r.isoDay, ny, nm, nd, largestUnit);
            var (h, mi, s, ms, us, ns) = BalanceTimeOnly(subNs);
            return new JSTemporalDuration(fy, fmo, fw, fd, h, mi, s, ms, us, ns, DurationPrototype);
        }

        return BalanceFromNanoseconds(roundedNs, largestUnit);
    }

    private double TotalRelative(JSTemporalPlainDate r, string unit)
    {
        var (endNs, startNs, _) = RelativeEndpoints(r);
        ValidateRelativeEndInRange(endNs);

        if (unit is "year" or "month")
        {
            var (whole, sign, progress, unitLen) = CalendarNudgeBounds(r, endNs, startNs, unit == "year");
            if (unitLen.IsZero) return whole;
            return whole + sign * (double)progress / (double)unitLen;
        }

        var unitNs = unit == "week" ? 7 * (BigInteger)NanosecondsPerDay : TimeUnitNanoseconds(unit);
        return (double)(endNs - startNs) / (double)unitNs;
    }

    // Rounds the (fractional) number of year/month units from relativeTo to the duration's end to a
    // whole multiple of `increment` under `roundingMode`.
    private static BigInteger NudgeCalendarUnit(JSTemporalPlainDate r, BigInteger endNs, BigInteger startNs,
        bool isYear, long increment, string roundingMode)
    {
        var (whole, sign, progress, unitLen) = CalendarNudgeBounds(r, endNs, startNs, isYear);
        if (unitLen.IsZero) return 0;
        var num = (BigInteger)(long)whole * unitLen + sign * progress;
        var rounded = RoundToIncrement(num, unitLen * increment, roundingMode);
        return rounded / unitLen;
    }

    // The whole-unit count toward the end, the direction sign, and the |end − floorBoundary| /
    // |ceilBoundary − floorBoundary| nanosecond spans that locate the end between the two surrounding
    // calendar boundaries.
    private static (double whole, int sign, BigInteger progress, BigInteger unitLen) CalendarNudgeBounds(
        JSTemporalPlainDate r, BigInteger endNs, BigInteger startNs, bool isYear)
    {
        var sign = endNs > startNs ? 1 : (endNs < startNs ? -1 : 0);
        if (sign == 0) return (0, 0, BigInteger.Zero, BigInteger.Zero);

        var (ey, em, ed) = JSTemporalPlainDate.DateFromEpochDays((long)FloorDivBig(endNs, NanosecondsPerDay));
        var diff = JSTemporalPlainDate.DiffCalendarDate(r.isoYear, r.isoMonth, r.isoDay, ey, em, ed, isYear ? "year" : "month");
        var whole = isYear ? diff.years : diff.months;
        var w = (long)whole;

        var (fy, fm, fd) = isYear
            ? JSTemporalPlainDate.AddCalendarDate(r.isoYear, r.isoMonth, r.isoDay, w, 0, 0, 0)
            : JSTemporalPlainDate.AddCalendarDate(r.isoYear, r.isoMonth, r.isoDay, 0, w, 0, 0);
        var (cy, cm, cd) = isYear
            ? JSTemporalPlainDate.AddCalendarDate(r.isoYear, r.isoMonth, r.isoDay, w + sign, 0, 0, 0)
            : JSTemporalPlainDate.AddCalendarDate(r.isoYear, r.isoMonth, r.isoDay, 0, w + sign, 0, 0);

        var floorNs = (BigInteger)JSTemporalPlainDate.EpochDaysFor(fy, fm, fd) * NanosecondsPerDay;
        var ceilNs = (BigInteger)JSTemporalPlainDate.EpochDaysFor(cy, cm, cd) * NanosecondsPerDay;
        return (whole, sign, BigInteger.Abs(endNs - floorNs), BigInteger.Abs(ceilNs - floorNs));
    }

    // Splits a signed sub-day nanosecond count into time components that all share its sign.
    private static (double h, double mi, double s, double ms, double us, double ns) BalanceTimeOnly(BigInteger ns)
    {
        var sign = ns.Sign;
        var a = BigInteger.Abs(ns);
        var h = a / 3_600_000_000_000; a %= 3_600_000_000_000;
        var mi = a / 60_000_000_000; a %= 60_000_000_000;
        var s = a / 1_000_000_000; a %= 1_000_000_000;
        var ms = a / 1_000_000; a %= 1_000_000;
        var us = a / 1_000; a %= 1_000;
        return (sign * (double)h, sign * (double)mi, sign * (double)s, sign * (double)ms, sign * (double)us, sign * (double)a);
    }

    // ── balancing / rounding helpers ──────────────────────────────────────────────

    // Builds a Duration from a signed nanosecond total, distributing into components from
    // `largestUnit` (day…nanosecond) downward.
    private static JSValue BalanceFromNanoseconds(BigInteger totalNs, string largestUnit)
    {
        var sign = totalNs.Sign;
        var ns = BigInteger.Abs(totalNs);

        BigInteger days = 0, hours = 0, minutes = 0, seconds = 0, millis = 0, micros = 0;

        if (UnitIndex(largestUnit) <= UnitIndex("day"))
        {
            days = ns / NanosecondsPerDay; ns %= NanosecondsPerDay;
        }
        if (UnitIndex(largestUnit) <= UnitIndex("hour"))
        {
            hours = ns / 3_600_000_000_000; ns %= 3_600_000_000_000;
        }
        if (UnitIndex(largestUnit) <= UnitIndex("minute"))
        {
            minutes = ns / 60_000_000_000; ns %= 60_000_000_000;
        }
        if (UnitIndex(largestUnit) <= UnitIndex("second"))
        {
            seconds = ns / 1_000_000_000; ns %= 1_000_000_000;
        }
        if (UnitIndex(largestUnit) <= UnitIndex("millisecond"))
        {
            millis = ns / 1_000_000; ns %= 1_000_000;
        }
        if (UnitIndex(largestUnit) <= UnitIndex("microsecond"))
        {
            micros = ns / 1_000; ns %= 1_000;
        }
        var nanos = ns;

        double S(BigInteger v) => sign * (double)v;
        return new JSTemporalDuration(0, 0, 0, S(days), S(hours), S(minutes), S(seconds), S(millis), S(micros), S(nanos), DurationPrototype);
    }

    private static string NormalizeRoundingMode(string mode) => mode switch
    {
        "ceil" or "floor" or "expand" or "trunc"
            or "halfCeil" or "halfFloor" or "halfExpand" or "halfTrunc" or "halfEven" => mode,
        _ => throw JSEngine.NewRangeError($"Temporal.Duration: invalid roundingMode \"{mode}\""),
    };

    // RoundNumberToIncrement: rounds `value` to a multiple of `increment` per `roundingMode`.
    private static BigInteger RoundToIncrement(BigInteger value, BigInteger increment, string roundingMode)
    {
        if (increment <= 1 && roundingMode is "halfExpand" or "trunc") return value;

        var quotient = BigInteger.DivRem(value, increment, out var remainder);
        if (remainder == 0) return value;

        var sign = value.Sign; // remainder has the same sign as value here
        var absRem2 = BigInteger.Abs(remainder) * 2;
        var absInc = BigInteger.Abs(increment);

        bool roundUp = roundingMode switch
        {
            "trunc" => false,
            "expand" => true,
            "ceil" => sign > 0,
            "floor" => sign < 0,
            "halfExpand" => absRem2 >= absInc,
            "halfTrunc" => absRem2 > absInc,
            "halfCeil" => sign > 0 ? absRem2 >= absInc : absRem2 > absInc,
            "halfFloor" => sign < 0 ? absRem2 >= absInc : absRem2 > absInc,
            "halfEven" => absRem2 > absInc || (absRem2 == absInc && !BigInteger.Abs(quotient).IsEven),
            _ => absRem2 >= absInc,
        };

        if (roundUp)
            quotient += sign >= 0 ? 1 : -1;

        return quotient * increment;
    }
}
