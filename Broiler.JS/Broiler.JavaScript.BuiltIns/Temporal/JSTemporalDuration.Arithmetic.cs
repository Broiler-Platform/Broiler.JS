using System;
using System.Numerics;
using Broiler.ECMAScript.Numbers;
using Broiler.ECMAScript.Temporal;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.Runtime;
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
        ["year", "month", "week", "day", "hour", "minute", "second", "millisecond", "microsecond", "nanosecond"];

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
        TemporalAdapter.CheckDuration(new DurationFields(result.years, result.months, result.weeks, result.days, result.hours,
            result.minutes, result.seconds, result.milliseconds, result.microseconds, result.nanoseconds));
        return result;
    }

    private static string MaxUnit(string a, string b) => UnitIndex(a) <= UnitIndex(b) ? a : b;

    // ── round ─────────────────────────────────────────────────────────────────────

    internal JSValue RoundImpl(JSValue roundTo)
    {
        string smallestUnit = null;
        string largestUnit = "auto";
        var increment = 1d;
        var roundingMode = RoundingMode.HalfExpand;
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
                // out-of-range [1, 1e9] result is a RangeError. GetRoundingIncrement computes the
                // same, but with its own messages; these are round's.
                var n = TemporalRoundingOptions.ToIntegerWithTruncation(inc, "Temporal.Duration.round: invalid roundingIncrement");
                if (n < 1 || n > 1_000_000_000)
                    throw JSEngine.NewRangeError("Temporal.Duration.round: invalid roundingIncrement");
                increment = n;
            }

            roundingMode = TemporalRoundingOptions.GetRoundingMode(obj, RoundingMode.HalfExpand);

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

        // largestUnit may not be a finer unit than smallestUnit — the result cannot carry a unit
        // smaller than the one we round to. A defaulted largestUnit is always >= smallestUnit, so
        // this only rejects an explicit largestUnit finer than smallestUnit (a larger UnitIndex is
        // a finer unit).
        if (UnitIndex(largestUnit) > UnitIndex(smallestUnit))
            throw JSEngine.NewRangeError(
                $"Temporal.Duration.prototype.round: largestUnit \"{largestUnit}\" cannot be smaller than smallestUnit \"{smallestUnit}\"");

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

        // A calendar smallestUnit (year/month/week/day) may only be rounded to an increment > 1
        // when the whole result is expressed in that unit: a coarser largestUnit would otherwise
        // have to carry the rounding remainder in a different, non-commensurable unit (a month is
        // not a whole number of weeks, etc.). This is why e.g. rounding to a 99-week increment with
        // a year/month duration requires largestUnit "week" explicitly, while the default increment
        // of 1 rounds freely (test262 round/balances-up-to-weeks, round/roundingmode-*).
        if (increment > 1 && UnitIndex(smallestUnit) <= UnitIndex("day") && largestUnit != smallestUnit)
            throw JSEngine.NewRangeError(
                "Temporal.Duration.prototype.round: a roundingIncrement greater than 1 with a calendar "
                + "smallestUnit (years, months, weeks, days) requires largestUnit to equal smallestUnit");

        var needsCalendar = HasCalendarUnits || UnitIndex(smallestUnit) < UnitIndex("day") || UnitIndex(largestUnit) < UnitIndex("day");
        if (needsCalendar && relZoned == null && relDate == null)
            throw JSEngine.NewRangeError(
                "Temporal.Duration.prototype.round with calendar units (years, months, weeks) requires a relativeTo option");

        if (relZoned != null)
            return relZoned.RoundDurationRelative(this, smallestUnit, largestUnit, (long)increment, roundingMode);

        if (relDate != null)
            return RoundRelative(relDate, smallestUnit, largestUnit, (long)increment, roundingMode);

        // Use BigInteger for the increment-unit span: a large day increment (e.g. 1e7 days =
        // 8.64×10^20 ns) overflows Int64, which silently corrupted the rounding modulus and left
        // the duration unrounded (test262 round/roundingincrement-days-large, -non-integer).
        var unitNs = (BigInteger)TimeUnitNanoseconds(smallestUnit) * (long)increment;
        var rounded = ExactRounding.RoundNumberToIncrement(TimePlusDaysNanoseconds(), BigInteger.One, unitNs, roundingMode);
        return BalanceFromNanoseconds(rounded, largestUnit);
    }

    // ── total ───────────────────────────────────────────────────────────────────

    internal JSValue TotalImpl(JSValue totalOf)
    {
        string unit;
        JSTemporalZonedDateTime relZoned;
        JSTemporalPlainDate relDate;

        if (totalOf == null || totalOf.IsUndefined)
            throw JSEngine.NewTypeError("Temporal.Duration.prototype.total requires an options argument");

        if (totalOf.IsString)
        {
            unit = NormalizeUnit(totalOf.StringValue, allowAuto: false);
            relZoned = null;
            relDate = null;
        }
        else if (totalOf is JSObject obj)
        {
            // GetTemporalRelativeToOption (relativeTo) is read before the unit
            // (test262 Duration/prototype/total order-of-operations).
            TryResolveRelativeTo(totalOf, out relZoned, out relDate);

            var u = obj[KeyStrings.GetOrCreate("unit")];
            if (u.IsUndefined)
                throw JSEngine.NewRangeError("Temporal.Duration.prototype.total requires a unit");
            unit = NormalizeUnit(u.StringValue, allowAuto: false);
        }
        else throw JSEngine.NewTypeError("Temporal.Duration.prototype.total requires an options object or string");

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
        // DivideNormalizedTimeDuration computes total = TotalNanoseconds / divisor on EXACT
        // mathematical values, then 𝔽(): the single ℝ→𝔽 rounding of the exact rational, which is the
        // shared library's ExactRounding.ToDouble. Rounding the integer part and the fraction
        // separately and adding them rounds three times and can be one ULP off
        // (test262 Duration/prototype/total/precision-exact-mathematical-values-6/7).
        return new JSNumber(ExactRounding.ToDouble(totalNs, unitNs));
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

    // The six time fields as one exact time duration (row R7: no field is narrowed to a long first).
    private BigInteger TimeComponentsNs() => TemporalAdapter.TimeDuration(this);

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

        // ToRelativeTemporalObject for a string: ParseISODateTime with the TemporalDateTimeString[+Zoned]
        // and [~Zoned] goals, the shared library's grammar. An unparsable string, a UTC (Z) designator with
        // no time-zone annotation, an annotation out of order or a malformed offset is a RangeError.
        if (rel.IsString)
        {
            var s = rel.ToString();
            var parsed = TemporalAdapter.ParseIsoString(s, [TemporalGoal.ZonedDateTime, TemporalGoal.DateTime], "a Temporal.Duration relativeTo");

            // A string with a [TimeZone] annotation is a ZonedDateTime relativeTo (DST-aware, range-validated,
            // honouring a Z designator / numeric offset), resolved through the zoned timeline, so that adding
            // the duration to a boundary instant overflows exactly as AddZonedDateTime requires (test262
            // relativeto-string-limits).
            if (parsed.TimeZone != null)
            {
                zoned = JSTemporalZonedDateTime.FromParsed(parsed, s, null);
                if (TemporalCalendarMath.IsNonIso(zoned.calendarId))
                    throw JSEngine.NewError($"Temporal.Duration: a relativeTo with the \"{zoned.calendarId}\" calendar is not yet implemented");
                return true;
            }

            date = JSTemporalPlainDate.FromParsed(parsed, s);
            if (TemporalCalendarMath.IsNonIso(date.calendarId))
                throw JSEngine.NewError($"Temporal.Duration: a relativeTo with the \"{date.calendarId}\" calendar is not yet implemented");
            return true;
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
                if (!TemporalAdapter.TryParseOffsetString(off.StringValue, out _))
                    throw JSEngine.NewRangeError($"Temporal.Duration: invalid offset string \"{off.StringValue}\"");
                offsetString = off.StringValue;
            }

            // The timeZone field is resolved through ToTemporalTimeZoneIdentifier: a non-string /
            // non-ZonedDateTime value is a TypeError and a string that is not a valid identifier — a
            // bare date-time with no designator — is a RangeError.
            var tzId = JSTemporalZonedDateTime.ToTimeZoneIdentifier(relObj[KeyStrings.GetOrCreate("timeZone")]);

            // Resolve the bag's wall-clock date-time (used both to validate the offset and to build the
            // ZonedDateTime relativeTo below).
            var bagDateTime = (JSTemporalPlainDateTime)JSTemporalPlainDateTime.From(new Arguments(JSUndefined.Value, relObj));

            // InterpretISODateTimeOffset: a bag offset must EXACTLY match the zone's offset for the
            // local wall clock (no minute rounding, unlike the string form), so a rounded "-00:45" for
            // a -00:44:30 zone is a RangeError.
            if (offsetString != null)
                JSTemporalZonedDateTime.ValidateBagOffsetMatchesZone(
                    tzId, bagDateTime.isoYear, bagDateTime.isoMonth, bagDateTime.isoDay,
                    bagDateTime.hour, bagDateTime.minute, bagDateTime.second, offsetString);

            ValidateRelativeBagTimeFields(rel);

            // A relativeTo property bag with a timeZone is a ZonedDateTime relativeTo (DST-aware): a day
            // may be 23/24/25 hours, exactly like an object or string ZonedDateTime relativeTo — not a
            // fixed 24-hour PlainDate (test262 Duration round/total/compare casts-relativeTo-from-object).
            if (TemporalCalendarMath.IsNonIso(bagDateTime.calendarId))
                throw JSEngine.NewError($"Temporal.Duration: a relativeTo with the \"{bagDateTime.calendarId}\" calendar is not yet implemented");
            zoned = JSTemporalZonedDateTime.BuildRelative(
                bagDateTime.isoYear, bagDateTime.isoMonth, bagDateTime.isoDay,
                bagDateTime.hour, bagDateTime.minute, bagDateTime.second,
                bagDateTime.millisecond, bagDateTime.microsecond, bagDateTime.nanosecond,
                tzId, offsetString, bagDateTime.calendarId);
            return true;
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
        ["hour", "minute", "second", "millisecond", "microsecond", "nanosecond"];

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

        // monthCode and offset are spec-required to BE Strings (after ToPrimitive on an Object) —
        // a Number / null / boolean / BigInt is a TypeError, not a RangeError on the coerced text
        // (test262 Duration/.../relativeto-propertybag-invalid-offset-string).
        void CopyRequiredString(string field)
        {
            var v = rel[KeyStrings.GetOrCreate(field)];
            var s = TemporalAdapter.RequireOffsetString(v, "Temporal.Duration");
            if (s != null) snapshot[KeyStrings.GetOrCreate(field)] = new JSString(s);
        }

        void CopyRaw(string field)
        {
            var v = rel[KeyStrings.GetOrCreate(field)];
            if (v != null && !v.IsUndefined) snapshot[KeyStrings.GetOrCreate(field)] = v;
        }

        // calendar is read first (GetTemporalCalendarIdentifierWithISODefault), then the merged
        // date / time / offset / timeZone fields in alphabetical order. era / eraYear are calendar
        // date fields ONLY for calendars that use eras (gregory / japanese / buddhist / roc and the
        // arithmetic non-iso era calendars); the iso8601 default and the era-less lunisolar
        // calendars (chinese / dangi) do not have them, so PrepareCalendarFields must NOT read them
        // there (test262 Duration/prototype/{total,round}/order-of-operations). The calendar value
        // is read exactly once and reused for both the snapshot and this decision.
        var calendarValue = rel[KeyStrings.GetOrCreate("calendar")];
        if (calendarValue != null && !calendarValue.IsUndefined)
            snapshot[KeyStrings.GetOrCreate("calendar")] = calendarValue;

        string calendarId;
        if (calendarValue == null || calendarValue.IsUndefined)
            calendarId = "iso8601";
        else if (calendarValue.IsString)
        {
            // A bare identifier string; canonicalize to classify it. An unrecognised value is left
            // for the later ToRelativeDate to reject — assume it may use eras so the fields are
            // still read (preserving the pre-existing validation behaviour).
            try { calendarId = TemporalCalendar.ResolveCalendarIdentifierArgument(calendarValue, "Temporal.Duration"); }
            catch { calendarId = null; }
        }
        else
            calendarId = null; // a Temporal object's calendar slot, resolved later; keep era fields.

        var readEraFields = calendarId == null
            || (calendarId != "iso8601" && TemporalCalendarMath.HasEra(calendarId));

        CopyNumber("day");
        if (readEraFields)
        {
            CopyString("era");
            CopyNumber("eraYear");
        }
        CopyNumber("hour");
        CopyNumber("microsecond");
        CopyNumber("millisecond");
        CopyNumber("minute");
        CopyNumber("month");
        CopyRequiredString("monthCode");
        CopyNumber("nanosecond");
        CopyRequiredString("offset");
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

    private JSValue RoundRelative(JSTemporalPlainDate r, string smallestUnit, string largestUnit, long increment, RoundingMode roundingMode)
    {
        var (endNs, startNs, startEpoch) = RelativeEndpoints(r);
        ValidateRelativeEndpoints(startNs, endNs);

        if (smallestUnit is "year" or "month")
        {
            var roundedUnits = NudgeCalendarUnit(r, endNs, startNs, smallestUnit == "year", increment, roundingMode);

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

            // Re-deriving the difference to the rounded end date can re-express the rounded count
            // with fewer large units because of the calendar's month-end clamping (spec
            // BubbleRelativeDuration): from a Feb-29 anchor, the 12-month difference to 2021-02-28
            // comes back out of DiffCalendarDate as 11 months 30 days. Bubble the smaller units up
            // so a whole larger unit is restored (12 months → 1 year).
            var endEpochDays = JSTemporalPlainDate.EpochDaysFor(ny, nm, nd);
            var bubbleSign = endEpochDays >= JSTemporalPlainDate.EpochDaysFor(r.isoYear, r.isoMonth, r.isoDay) ? 1 : -1;
            var (by, bmo, bw, bd) = BubbleDateDuration(
                r, (long)fy, (long)fmo, (long)fw, (long)fd, endEpochDays, bubbleSign, largestUnit, smallestUnit);
            return new JSTemporalDuration(by, bmo, bw, bd, 0, 0, 0, 0, 0, 0, DurationPrototype);
        }

        if (smallestUnit == "week")
        {
            // Weeks are rounded, never balanced, against an anchor date that keeps the coarser
            // calendar units fixed: anchor = relativeTo + (whole years/months toward the end, per
            // largestUnit). A week is a fixed 7-day span, so the signed nanoseconds from the anchor
            // to the duration's end round directly to a whole multiple of weeks; the surviving
            // years/months come from the calendar difference (spec NudgeToCalendarUnit, unit "week").
            var endEpoch = (long)FloorDivBig(endNs, NanosecondsPerDay);
            var (ey, em, ed) = JSTemporalPlainDate.DateFromEpochDays(endEpoch);
            var (diffYears, diffMonths, _, _) =
                JSTemporalPlainDate.DiffCalendarDate(r.isoYear, r.isoMonth, r.isoDay, ey, em, ed, largestUnit);
            var anchorYears = (long)diffYears;
            var anchorMonths = (long)diffMonths;
            var (ay, amo, ad) =
                JSTemporalPlainDate.AddCalendarDate(r.isoYear, r.isoMonth, r.isoDay, anchorYears, anchorMonths, 0, 0);
            var anchorNs = (BigInteger)JSTemporalPlainDate.EpochDaysFor(ay, amo, ad) * NanosecondsPerDay;
            var weekNs = 7 * (BigInteger)NanosecondsPerDay;
            var roundedWeeks = (long)(ExactRounding.RoundNumberToIncrement(endNs - anchorNs, BigInteger.One, weekNs * increment, roundingMode) / weekNs);
            return new JSTemporalDuration(anchorYears, anchorMonths, roundedWeeks, 0, 0, 0, 0, 0, 0, 0, DurationPrototype);
        }

        // BigInteger span: a large day increment (1e7+ days) overflows Int64 here too — see the
        // matching non-relative path (test262 round/roundingincrement-days-large with PlainDate).
        var smallestNs = (BigInteger)TimeUnitNanoseconds(smallestUnit) * increment;
        var roundedNs = ExactRounding.RoundNumberToIncrement(endNs - startNs, BigInteger.One, smallestNs, roundingMode);

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
            var (r1, _, sign, progressed, span) = CalendarNudgeWindow(r, endNs, startNs, unit == "year", 1);
            if (sign == 0) return 0;
            // total = r1 + progress × sign as a single ℝ→𝔽 rounding of the exact rational (the
            // shared library's ExactRounding.ToDouble), not r1 plus a separately rounded fraction
            // (#818 Problem 8).
            var (numerator, denominator) = TemporalRoundingOptions.NudgeTotal(r1, 1, sign, progressed, span);
            return ExactRounding.ToDouble(numerator, denominator);
        }

        var unitNs = unit == "week" ? 7 * (BigInteger)NanosecondsPerDay : TimeUnitNanoseconds(unit);
        // The total is the nearest double to the EXACT rational (endNs − startNs)/unitNs. Converting
        // each BigInteger to double first and then dividing rounds twice and can be off by one ULP
        // (test262 Duration/prototype/total/relativeto-total-of-each-unit "weeks"); the shared
        // library's ExactRounding.ToDouble rounds the exact ratio once, matching the spec and the
        // ZonedDateTime total path.
        return ExactRounding.ToDouble(endNs - startNs, unitNs);
    }

    // Bubbles smaller calendar units up into larger ones so the date difference uses the maximal
    // larger unit (spec BubbleRelativeDuration). `endEpochDays` is relativeTo + the rounded duration;
    // a larger unit absorbs the smaller ones (which are then zeroed) when adding one more of it stays
    // on the same side of the end. Units are processed from the one just above smallestUnit up to
    // largestUnit; weeks bubble only when largestUnit is week.
    private static (long y, long mo, long w, long d) BubbleDateDuration(
        JSTemporalPlainDate r, long y, long mo, long w, long d,
        long endEpochDays, int sign, string largestUnit, string smallestUnit)
    {
        for (var idx = UnitIndex(smallestUnit) - 1; idx >= UnitIndex(largestUnit); idx--)
        {
            var unit = UnitOrder[idx];
            if (unit == "week" && largestUnit != "week") continue;

            long cy = y, cmo = mo, cw = w, cd = d;
            switch (unit)
            {
                case "year": cy = y + sign; cmo = 0; cw = 0; cd = 0; break;
                case "month": cmo = mo + sign; cw = 0; cd = 0; break;
                case "week": cw = w + sign; cd = 0; break;
                default: continue;
            }

            var (ay, am, ad) = JSTemporalPlainDate.AddCalendarDate(r.isoYear, r.isoMonth, r.isoDay, cy, cmo, cw, cd);
            var candEpoch = JSTemporalPlainDate.EpochDaysFor(ay, am, ad);
            var notBeyond = sign > 0 ? candEpoch <= endEpochDays : candEpoch >= endEpochDays;
            if (notBeyond) { y = cy; mo = cmo; w = cw; d = cd; }
            else break;
        }
        return (y, mo, w, d);
    }

    // NudgeToCalendarUnit for a year or month smallestUnit: the number of those units from relativeTo
    // to the duration's end, rounded to a multiple of `increment` under `roundingMode` over the
    // increment's window [r1, r2] (not linearly inside one unit: a two-month increment of P1M from
    // 2023-01-01 is measured over January and February together). A progress of 1 is the window's end;
    // any other progress is rounded by the shared library's ApplyUnsignedRoundingMode.
    private static long NudgeCalendarUnit(JSTemporalPlainDate r, BigInteger endNs, BigInteger startNs,
        bool isYear, long increment, RoundingMode roundingMode)
    {
        var (r1, r2, sign, progressed, span) = CalendarNudgeWindow(r, endNs, startNs, isYear, increment);
        if (sign == 0) return 0;
        if (progressed == span) return r2;

        var (numerator, denominator) = TemporalRoundingOptions.NudgeTotal(r1, increment, sign, progressed, span);
        return TemporalRoundingOptions.NudgeRoundsToEnd(numerator, denominator, r1, r2, sign, roundingMode) ? r2 : r1;
    }

    // ComputeNudgeWindow for a year or month unit: r1 is the whole units from relativeTo toward the
    // end, truncated to a multiple of `increment` (RoundNumberToIncrement with "trunc", the shared
    // library's), and r2 = r1 + increment × sign; `progressed` and `span` are the |end − start| and
    // |windowEnd − start| nanosecond spans that locate the end inside the window.
    private static (long r1, long r2, int sign, BigInteger progressed, BigInteger span) CalendarNudgeWindow(
        JSTemporalPlainDate r, BigInteger endNs, BigInteger startNs, bool isYear, long increment)
    {
        var sign = endNs > startNs ? 1 : (endNs < startNs ? -1 : 0);
        if (sign == 0) return (0, 0, 0, BigInteger.Zero, BigInteger.Zero);

        var (ey, em, ed) = JSTemporalPlainDate.DateFromEpochDays((long)FloorDivBig(endNs, NanosecondsPerDay));
        var diff = JSTemporalPlainDate.DiffCalendarDate(r.isoYear, r.isoMonth, r.isoDay, ey, em, ed, isYear ? "year" : "month");
        var units = (long)(isYear ? diff.years : diff.months);

        BigInteger BoundaryNs(long count)
        {
            var (ay, am, ad) = isYear
                ? JSTemporalPlainDate.AddCalendarDate(r.isoYear, r.isoMonth, r.isoDay, count, 0, 0, 0)
                : JSTemporalPlainDate.AddCalendarDate(r.isoYear, r.isoMonth, r.isoDay, 0, count, 0, 0);
            return (BigInteger)JSTemporalPlainDate.EpochDaysFor(ay, am, ad) * NanosecondsPerDay;
        }

        var r1 = (long)ExactRounding.RoundNumberToIncrement(units, BigInteger.One, increment, RoundingMode.Trunc);
        var windowStartNs = BoundaryNs(r1);
        var windowEndNs = BoundaryNs(r1 + increment * sign);

        // Window shift (spec NudgeToCalendarUnit "additional shift"): the calendar's month-end
        // clamping can place the end OUTSIDE the window — e.g. from 2020-01-31 the 1-month
        // difference clamps to 2020-02-29 so DiffCalendarDate yields 0 months, yet the duration's
        // end is past that boundary. Shift the window one increment toward the end so the progress
        // is measured against the correct surrounding boundaries.
        var inBounds = sign == 1
            ? windowStartNs <= endNs && endNs <= windowEndNs
            : windowEndNs <= endNs && endNs <= windowStartNs;
        if (!inBounds)
        {
            r1 += increment * sign;
            windowStartNs = windowEndNs;
            windowEndNs = BoundaryNs(r1 + increment * sign);
        }

        return (r1, r1 + increment * sign, sign, BigInteger.Abs(endNs - windowStartNs), BigInteger.Abs(windowEndNs - windowStartNs));
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

        double S(BigInteger v) => sign * ExactRounding.ToDouble(v, BigInteger.One);
        double rDays = S(days), rHours = S(hours), rMinutes = S(minutes), rSeconds = S(seconds),
            rMillis = S(millis), rMicros = S(micros), rNanos = S(nanos);

        // TemporalDurationFromInternal: each component is turned into a float64-representable integer,
        // and the assembled duration must still be valid — the nearest double can round a large component
        // up past the maximum time-duration limit, which is then a RangeError (test262 round/
        // result-out-of-range, round/out-of-range-when-converting-from-normalized-duration).
        TemporalAdapter.CheckDuration(new DurationFields(0, 0, 0, rDays, rHours, rMinutes, rSeconds, rMillis, rMicros, rNanos));
        return new JSTemporalDuration(0, 0, 0, rDays, rHours, rMinutes, rSeconds, rMillis, rMicros, rNanos, DurationPrototype);
    }

    // The engine's own rounding helpers (NearestDouble, RatioToDouble, a second NormalizeRoundingMode
    // and RoundToIncrement) were removed at milestone SJL-4.5: an integer's or a ratio's nearest double
    // is the shared library's ExactRounding.ToDouble, correctly rounded over the whole range (a ratio
    // near 2^800 no longer overflows to Infinity); the option read is TemporalRoundingOptions'; and
    // RoundNumberToIncrement is ExactRounding.RoundNumberToIncrement.
}
