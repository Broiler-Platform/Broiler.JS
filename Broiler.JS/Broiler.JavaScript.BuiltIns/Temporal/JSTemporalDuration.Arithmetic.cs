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
        JSTemporalZonedDateTime relZoned = null;
        JSTemporalPlainDate relDate = null;

        if (roundTo == null || roundTo.IsUndefined)
            throw JSEngine.NewTypeError("Temporal.Duration.prototype.round requires an options argument");

        if (roundTo.IsString)
        {
            smallestUnit = NormalizeUnit(roundTo.StringValue, allowAuto: false);
        }
        else if (roundTo is JSObject obj)
        {
            // The options getters run in this fixed order, regardless of which units the
            // duration carries: largestUnit, relativeTo, roundingIncrement, roundingMode,
            // smallestUnit (test262 round/order-of-operations). Each value is also coerced
            // (toString / valueOf) as it is read, before the next getter.
            var lu = obj[KeyStrings.GetOrCreate("largestUnit")];
            var largestUnitProvided = !lu.IsUndefined;
            if (largestUnitProvided) largestUnit = NormalizeUnit(lu.StringValue, allowAuto: true);

            TryResolveRelativeTo(obj, out relZoned, out relDate);

            var inc = obj[KeyStrings.GetOrCreate("roundingIncrement")];
            if (!inc.IsUndefined)
            {
                // GetRoundingIncrementOption: ToIntegerWithTruncation(value) truncates a
                // non-integer toward zero (so 2.5 -> 2) and only NaN / ±Infinity or an
                // out-of-range [1, 1e9] result is a RangeError.
                var n = inc.DoubleValue;
                if (double.IsNaN(n) || double.IsInfinity(n))
                    throw JSEngine.NewRangeError("Temporal.Duration.round: invalid roundingIncrement");
                n = Math.Truncate(n);
                if (n < 1 || n > 1_000_000_000)
                    throw JSEngine.NewRangeError("Temporal.Duration.round: invalid roundingIncrement");
                increment = n;
            }

            var rm = obj[KeyStrings.GetOrCreate("roundingMode")];
            if (!rm.IsUndefined) roundingMode = NormalizeRoundingMode(rm.StringValue);

            var su = obj[KeyStrings.GetOrCreate("smallestUnit")];
            if (!su.IsUndefined) smallestUnit = NormalizeUnit(su.StringValue, allowAuto: false);
            // Step ordering aside, the receiver is only under-specified when *neither*
            // smallestUnit nor largestUnit was supplied; an explicit largestUnit: "auto"
            // is a valid largestUnit and must not be treated as missing.
            if (smallestUnit == null && !largestUnitProvided)
                throw JSEngine.NewRangeError("Temporal.Duration.round requires either smallestUnit or largestUnit");
        }
        else throw JSEngine.NewTypeError("Temporal.Duration.prototype.round requires an options object or string");

        smallestUnit ??= "nanosecond";

        if (largestUnit == "auto")
            largestUnit = MaxUnit(DefaultLargestUnit(), smallestUnit);

        // Rounding to weeks cannot retain a coarser calendar unit: a month/year is not a
        // whole number of weeks, so a result mixing them is unrepresentable. When the
        // smallest unit is "week" the largest unit must be "week" too — if it defaulted to
        // (or was given as) "year"/"month" because the duration carries those units, the
        // caller must pass largestUnit explicitly (test262 round/balances-up-to-weeks:
        // "largestUnit must be included").
        if (smallestUnit == "week" && UnitIndex(largestUnit) < UnitIndex("week"))
            throw JSEngine.NewRangeError(
                "Temporal.Duration.prototype.round to weeks requires largestUnit \"week\" when the duration has years or months");

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
        // The total is the nearest double to the exact rational totalNs/unitNs. The
        // integer part must round to nearest (ℝ→𝔽), not truncate as (double)BigInteger
        // does; the fraction is below an ulp once |whole| is large (#818 Problem 8).
        var result = NearestDouble(whole) + (double)remainder / (double)unitNs;
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
        if (options == null || options.IsUndefined) return false;
        if (options is not JSObject optionsObject)
            throw JSEngine.NewTypeError("Temporal.Duration options must be an object or undefined");
        var rel = optionsObject[KeyStrings.GetOrCreate("relativeTo")];

        // GetTemporalRelativeToOption: only undefined means "absent". Any other value — including
        // null — is passed to ToRelativeTemporalObject and converted.
        if (rel == null || rel.IsUndefined) return false;

        // ToRelativeTemporalObject: a value that is neither an Object nor a String — null, a boolean,
        // a number, a bigint, a symbol — is a TypeError, not the RangeError used for an unparsable
        // string. (This conversion happens before any unit/calendar validation.)
        if (!rel.IsString && rel is not JSObject)
            throw JSEngine.NewTypeError("Temporal.Duration: relativeTo must be a Temporal object, a property bag, or an ISO string");

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
        // a UTC (Z) designator with no time-zone annotation, or a malformed offset is a RangeError.
        if (rel.IsString)
        {
            var s = rel.ToString();
            if (!TemporalIsoString.TryParseRelative(s, out var tz, out var hasZ, out var offset))
                throw JSEngine.NewRangeError($"Temporal.Duration: cannot parse relativeTo \"{s}\"");

            // A string with a [TimeZone] annotation is a ZonedDateTime relativeTo: parse it as a full
            // ZonedDateTime (DST-aware, range-validated, honouring a Z designator / numeric offset) and
            // resolve it through the zoned timeline, so that adding the duration to a boundary instant
            // overflows exactly as AddZonedDateTime requires (test262 relativeto-string-limits).
            if (tz != null)
            {
                zoned = JSTemporalZonedDateTime.ParseRelativeZoned(s);
                if (TemporalCalendarMath.IsNonIso(zoned.calendarId))
                    throw JSEngine.NewError($"Temporal.Duration: a relativeTo with the \"{zoned.calendarId}\" calendar is not yet implemented");
                return true;
            }

            if (hasZ)
                throw JSEngine.NewRangeError("Temporal.Duration: relativeTo with a UTC (Z) designator requires a time-zone annotation");
            if (offset != null && !TemporalIsoString.IsStrictOffset(offset))
                throw JSEngine.NewRangeError($"Temporal.Duration: invalid UTC offset in relativeTo \"{s}\"");
        }

        // A genuine property bag (not a recognised Temporal object): read every relativeTo field once,
        // in the spec's fixed order — calendar first, then the merged date/time/offset/timeZone names in
        // alphabetical order — coercing each as PrepareCalendarFields does, so the observable getter and
        // toString/valueOf order matches ToRelativeTemporalObject (test262 round/order-of-operations).
        // The snapshot is a plain object, so the downstream slot/field logic re-reads it with no further
        // observable side effects.
        if (rel is JSObject relBag && !IsTemporalRelativeObject(rel))
            rel = BuildOrderedRelativeBagSnapshot(relBag);

        // A relativeTo property bag carrying a timeZone is a ZonedDateTime relativeTo; its offset
        // field (when present) must be a String (else TypeError) and a valid UTC offset (else
        // RangeError), even though it is then consumed here as a 24-hour-day PlainDate.
        if (rel is JSObject relObj && !relObj[KeyStrings.GetOrCreate("timeZone")].IsUndefined)
        {
            var off = relObj[KeyStrings.GetOrCreate("offset")];
            string offsetString = null;
            if (!off.IsUndefined)
            {
                if (!off.IsString)
                    throw JSEngine.NewTypeError("Temporal.Duration: the relativeTo offset field must be a string");
                if (!TemporalIsoString.IsStrictOffset(off.StringValue))
                    throw JSEngine.NewRangeError($"Temporal.Duration: invalid offset string \"{off.StringValue}\"");
                offsetString = off.StringValue;
            }

            // The timeZone field is resolved through ToTemporalTimeZoneIdentifier: a non-string /
            // non-ZonedDateTime value is a TypeError and a string that is not a valid identifier — a
            // bare date-time with no designator — is a RangeError.
            var tzId = JSTemporalZonedDateTime.ToTimeZoneIdentifier(relObj[KeyStrings.GetOrCreate("timeZone")]);

            // InterpretISODateTimeOffset: a bag offset must EXACTLY match the zone's offset for the
            // local wall clock (no minute rounding, unlike the string form), so a rounded "-00:45" for
            // a -00:44:30 zone is a RangeError. Resolve the bag's wall-clock date-time to check.
            if (offsetString != null)
            {
                var bagDateTime = (JSTemporalPlainDateTime)JSTemporalPlainDateTime.From(new Arguments(JSUndefined.Value, relObj));
                JSTemporalZonedDateTime.ValidateBagOffsetMatchesZone(
                    tzId, bagDateTime.isoYear, bagDateTime.isoMonth, bagDateTime.isoDay,
                    bagDateTime.hour, bagDateTime.minute, bagDateTime.second, offsetString);
            }
        }

        ValidateRelativeBagTimeFields(rel);

        date = JSTemporalPlainDate.ToRelativeDate(rel);
        if (TemporalCalendarMath.IsNonIso(date.calendarId))
            throw JSEngine.NewError($"Temporal.Duration: a relativeTo with the \"{date.calendarId}\" calendar is not yet implemented");
        return true;
    }

    // A relativeTo *property bag* (not a Temporal object) has its wall-clock fields read and
    // range-validated as part of ToRelativeTemporalObject, so a non-finite hour/minute/second/…
    // is a RangeError even though only the date is ultimately consumed as a 24-hour-day relativeTo.
    private static readonly string[] RelativeBagTimeFields =
        { "hour", "minute", "second", "millisecond", "microsecond", "nanosecond" };

    private static void ValidateRelativeBagTimeFields(JSValue rel)
    {
        if (rel is not JSObject bag) return;
        if (rel is JSTemporalPlainDate or JSTemporalPlainDateTime or JSTemporalZonedDateTime
            or JSTemporalPlainYearMonth or JSTemporalPlainMonthDay or JSTemporalPlainTime
            or JSTemporalInstant or JSTemporalDuration) return;

        foreach (var key in RelativeBagTimeFields)
        {
            var v = bag[KeyStrings.GetOrCreate(key)];
            if (v == null || v.IsUndefined) continue;
            var number = v.DoubleValue; // ToNumber → triggers the field's valueOf
            if (double.IsNaN(number) || double.IsInfinity(number))
                throw JSEngine.NewRangeError($"Temporal.Duration: relativeTo {key} must be finite");
        }
    }

    // Whether a relativeTo value is a Temporal object resolved through its internal slots (so its
    // property bag is NOT read). ZonedDateTime, PlainDate and PlainDateTime are the spec's three
    // slot-based relativeTo types; the remaining Temporal types are listed so they are never mistaken
    // for a plain property bag (they would instead error later when consumed as a date).
    private static bool IsTemporalRelativeObject(JSValue v)
        => v is JSTemporalPlainDate or JSTemporalPlainDateTime or JSTemporalZonedDateTime
            or JSTemporalPlainYearMonth or JSTemporalPlainMonthDay or JSTemporalPlainTime
            or JSTemporalInstant or JSTemporalDuration;

    // PrepareCalendarFields for a relativeTo property bag: read calendar, then the date, time, offset
    // and timeZone field names in one alphabetical pass, coercing each present value exactly as the
    // spec does (numeric fields via ToNumber/valueOf, monthCode/offset via ToString, calendar/timeZone
    // left raw and resolved later). Absent fields are still read (observable) but not coerced. The
    // already-coerced values are copied into a fresh plain object so the existing relativeTo resolution
    // can re-read them without firing the original bag's getters again.
    private static JSObject BuildOrderedRelativeBagSnapshot(JSObject rel)
    {
        var snapshot = new JSObject();

        void CopyNumber(string field)
        {
            var v = rel[KeyStrings.GetOrCreate(field)];
            if (v == null || v.IsUndefined) return;
            var number = v.DoubleValue; // ToNumber → triggers the field's valueOf
            if (double.IsNaN(number) || double.IsInfinity(number))
                throw JSEngine.NewRangeError($"Temporal.Duration: relativeTo {field} must be finite");
            snapshot[KeyStrings.GetOrCreate(field)] = new JSNumber(number);
        }

        void CopyString(string field)
        {
            var v = rel[KeyStrings.GetOrCreate(field)];
            if (v == null || v.IsUndefined) return;
            snapshot[KeyStrings.GetOrCreate(field)] = new JSString(v.ToString()); // ToString → triggers toString
        }

        void CopyRaw(string field)
        {
            var v = rel[KeyStrings.GetOrCreate(field)];
            if (v != null && !v.IsUndefined) snapshot[KeyStrings.GetOrCreate(field)] = v;
        }

        // calendar is read first (GetTemporalCalendarIdentifierWithISODefault), then the merged
        // date / time / offset / timeZone fields in alphabetical order.
        CopyRaw("calendar");
        CopyNumber("day");
        CopyNumber("hour");
        CopyNumber("microsecond");
        CopyNumber("millisecond");
        CopyNumber("minute");
        CopyNumber("month");
        CopyString("monthCode");
        CopyNumber("nanosecond");
        CopyString("offset");
        CopyNumber("second");
        CopyRaw("timeZone");
        CopyNumber("year");

        return snapshot;
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

    // The PlainDateTime representable range, in epoch nanoseconds: the instant range (±10^8 days) widened
    // by one day at each end and shrunk by 1 ns, i.e. [nsMin − day + 1, nsMax + day − 1]. The minimum
    // PlainDate (-271821-04-19) at midnight is exactly 1 ns below this lower bound.
    private static readonly BigInteger MinInstantNs = (BigInteger)(-100_000_000) * NanosecondsPerDay;
    private static readonly BigInteger MaxInstantNs = (BigInteger)100_000_000 * NanosecondsPerDay;
    private static readonly BigInteger MinDateTimeNs = MinInstantNs - NanosecondsPerDay + 1;
    private static readonly BigInteger MaxDateTimeNs = MaxInstantNs + NanosecondsPerDay - 1;

    // RejectDateTimeRange: a PlainDateTime endpoint of the relativeTo difference (its UTC epoch ns) must
    // lie within the representable PlainDateTime range; otherwise rounding / totalling is a RangeError
    // (e.g. a duration carrying ~2^53 seconds reaches ~10^11 days, far beyond the range; or a relativeTo
    // of -271821-04-19 whose midnight is just below the minimum representable PlainDateTime).
    private static void RejectRelativeDateTimeRange(BigInteger ns)
    {
        if (ns < MinDateTimeNs || ns > MaxDateTimeNs)
            throw JSEngine.NewRangeError("Temporal.Duration: relative date/time is outside of supported range");
    }

    // DifferencePlainDateTimeWithRounding/-Total: when the relativeTo (origin) and the duration's end
    // (target) are the same instant the result is zero and no range check applies (the spec's
    // CompareISODateTime early return); otherwise both endpoints must be representable PlainDateTimes.
    private static void ValidateRelativeEndpoints(BigInteger startNs, BigInteger endNs)
    {
        if (startNs == endNs) return;
        RejectRelativeDateTimeRange(startNs);
        RejectRelativeDateTimeRange(endNs);
    }

    private JSValue RoundRelative(JSTemporalPlainDate r, string smallestUnit, string largestUnit, long increment, string roundingMode)
    {
        var (endNs, startNs, startEpoch) = RelativeEndpoints(r);
        ValidateRelativeEndpoints(startNs, endNs);

        if (smallestUnit is "year" or "month")
        {
            var roundedUnits = (long)NudgeCalendarUnit(r, endNs, startNs, smallestUnit == "year", increment, roundingMode);

            // When the largest and smallest unit coincide, the rounded duration is exactly
            // `roundedUnits` of that unit. Re-deriving it via AddCalendarDate + DiffCalendarDate
            // would reintroduce the calendar's add/subtract asymmetry around month-end clamping
            // and lose the rounded value: with relativeTo 1970-07-31, 1970-07-31 + 2 months clamps
            // to 1970-09-30, yet until(1970-07-31 → 1970-09-30) is 1 month 30 days, so rounding
            // 1m30d to whole months (increment 2) would come back out as 1m30d instead of 2m.
            if (largestUnit == smallestUnit)
            {
                return smallestUnit == "year"
                    ? new JSTemporalDuration(roundedUnits, 0, 0, 0, 0, 0, 0, 0, 0, 0, DurationPrototype)
                    : new JSTemporalDuration(0, roundedUnits, 0, 0, 0, 0, 0, 0, 0, 0, DurationPrototype);
            }

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
        ValidateRelativeEndpoints(startNs, endNs);

        if (unit is "year" or "month")
        {
            var (whole, sign, progress, unitLen) = CalendarNudgeBounds(r, endNs, startNs, unit == "year");
            if (unitLen.IsZero) return whole;
            // total = whole + sign*progress/unitLen as a single ℝ→𝔽 rounding of the exact
            // rational, not whole plus a separately rounded fraction (#818 Problem 8).
            return RatioToDouble((BigInteger)whole * unitLen + sign * progress, unitLen);
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

        double S(BigInteger v) => sign * NearestDouble(v);
        return new JSTemporalDuration(0, 0, 0, S(days), S(hours), S(minutes), S(seconds), S(millis), S(micros), S(nanos), DurationPrototype);
    }

    // Converts a non-negative integer to the nearest IEEE-754 double (ties to even), matching
    // ECMAScript's ℝ→𝔽 — unlike .NET's (double)BigInteger, which truncates toward zero. The two agree
    // for magnitudes below 2^53 (where every integer is representable); they differ only for the very
    // large components a near-limit Temporal.Duration can produce, where rounding a component up may
    // push the duration over the maximum (then rejected as a RangeError).
    internal static double NearestDouble(BigInteger v)
    {
        var neg = v.Sign < 0;
        var a = BigInteger.Abs(v);
        var lo = (double)a; // .NET truncates toward zero, so lo ≤ a
        if (double.IsInfinity(lo)) return neg ? double.NegativeInfinity : double.PositiveInfinity;

        var loBig = new BigInteger(lo);
        double chosen;
        if (loBig == a)
        {
            chosen = lo;
        }
        else
        {
            var hi = Math.BitIncrement(lo);
            if (double.IsInfinity(hi)) return neg ? -lo : lo;
            var dLo = a - loBig;
            var dHi = new BigInteger(hi) - a;
            chosen = dLo < dHi ? lo
                : dHi < dLo ? hi
                : (BitConverter.DoubleToInt64Bits(lo) & 1) == 0 ? lo : hi; // tie → even
        }
        return neg ? -chosen : chosen;
    }

    // The nearest double to the exact rational num/den (den > 0), rounded once — unlike
    // computing the integer and fractional parts as separate doubles, which rounds twice
    // and can land on the adjacent double (#818 Problem 8). The numerator is shifted left
    // so the integer quotient carries far more than 53 significant bits; the sub-unit
    // remainder is then well below the rounding position, so NearestDouble of the shifted
    // quotient (scaled back by the same power of two) is correctly rounded.
    internal static double RatioToDouble(BigInteger num, BigInteger den)
    {
        if (num.IsZero)
            return 0.0;

        const int shift = 256;
        var scaled = (num << shift) / den;
        return Math.ScaleB(NearestDouble(scaled), -shift);
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
