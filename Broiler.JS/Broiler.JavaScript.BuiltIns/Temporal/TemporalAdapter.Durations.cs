using System.Numerics;
using Broiler.ECMAScript.Numbers;
using Broiler.ECMAScript.Temporal;
using Broiler.ECMAScript.Time;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Temporal;

// Durations, rounding and differences: a duration's addition, compare, round and total with and without relativeTo,
// the relative machinery (RoundRelativeDuration with its nudges and bubbling, TotalRelativeDuration), every type's
// until and since, and the rules of their options are the shared library's DurationMath, TemporalDifferences and
// TemporalOptionRules (JSD-0078's X6; the adapter guide's part C), over the calendars (TemporalCalendars, the library's
// ICalendarArithmetic) and each zone's provider (TemporalTimeZones.Arithmetic, the library's TemporalTimeZone over
// SJL-8's rules). What stays here is this engine's: the options' reads, in the specification's order, with their
// conversions (TemporalRoundingOptions); GetTemporalRelativeToOption's reads, its classification of the value and its
// InterpretISODateTimeOffset; the RelativeTo record the library takes; and the RangeError each status is.
internal static partial class TemporalAdapter
{
    // ── statuses ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Each status of the durations and differences as this engine's <c>RangeError</c>: a calendar unit with no
    /// <c>relativeTo</c> (<paramref name="relativeToRequired"/> where the method has its own words), a duration past its
    /// limits, a day the calendar refuses under <c>reject</c>, and a date, a date-time or an instant past the limits (a
    /// nudge window with no length among them: row D12, decision D11).
    /// </summary>
    internal static void CheckArithmetic(TemporalStatus status, string typeName, string relativeToRequired = null)
    {
        switch (status)
        {
            case TemporalStatus.Completed:
                return;
            case TemporalStatus.RelativeToRequired:
                throw JSEngine.NewRangeError(relativeToRequired ?? $"{typeName}: years, months or weeks require a relativeTo option");
            case TemporalStatus.InvalidDuration:
                throw JSEngine.NewRangeError($"{typeName}: the duration is outside the representable range");
            case TemporalStatus.InvalidDate:
                throw JSEngine.NewRangeError($"{typeName}: day out of range for resulting month");
            default:
                throw JSEngine.NewRangeError($"{typeName}: result is outside the representable range");
        }
    }

    /// <summary>A <c>Temporal.Duration</c> of a result the library made: valid, and every zero +0.</summary>
    internal static JSTemporalDuration DurationOf(DurationFields fields) => new(fields, JSTemporalDuration.DurationPrototype);

    /// <summary>A <c>Temporal.Duration</c> of a library operation's result, its status first checked.</summary>
    internal static JSTemporalDuration DurationOf(TemporalStatus status, DurationFields fields, string typeName)
    {
        CheckArithmetic(status, typeName);
        return DurationOf(fields);
    }

    // ── options ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// GetOptionsObject: <see langword="null"/> for undefined (every option then its default), an object as itself, and
    /// anything else the <c>TypeError</c>.
    /// </summary>
    internal static JSObject OptionsObject(JSValue options, string typeName)
    {
        if (options == null || options.IsUndefined)
            return null;

        return options as JSObject ?? throw JSEngine.NewTypeError($"{typeName}: options must be an object or undefined");
    }

    /// <summary>
    /// GetDifferenceSettings (s13.43) of an <c>until</c> or <c>since</c>: GetOptionsObject, then the four reads in code unit
    /// order - <c>largestUnit</c>, <c>roundingIncrement</c>, <c>roundingMode</c> (default <c>trunc</c>), <c>smallestUnit</c> -
    /// each converted as it is read, then the type's rules, the library's <see cref="TemporalOptionRules.TryGetDifferenceSettings(TemporalType, DifferenceOperation, TemporalUnit, long, RoundingMode, TemporalUnit, out DifferenceSettings)"/>:
    /// the unit group, the disallowed units, the defaults, the largest unit at least the smallest, the increment for the
    /// smallest unit's maximum, and <c>since</c>'s negated mode. A refusal is the <c>RangeError</c>.
    /// </summary>
    internal static DifferenceSettings DifferenceSettings(TemporalType type, DifferenceOperation operation, JSValue options, string typeName)
    {
        var largestUnit = TemporalUnit.Unset;
        var roundingIncrement = TemporalOptionRules.DefaultRoundingIncrement;
        var roundingMode = TemporalOptionRules.DefaultDifferenceRoundingMode;
        var smallestUnit = TemporalUnit.Unset;

        if (OptionsObject(options, typeName) is { } resolved)
        {
            largestUnit = TemporalRoundingOptions.GetUnit(resolved, "largestUnit", typeName);
            roundingIncrement = TemporalRoundingOptions.GetRoundingIncrement(resolved);
            roundingMode = TemporalRoundingOptions.GetRoundingMode(resolved, TemporalOptionRules.DefaultDifferenceRoundingMode);
            smallestUnit = TemporalRoundingOptions.GetUnit(resolved, "smallestUnit", typeName);
        }

        if (!TemporalOptionRules.TryGetDifferenceSettings(type, operation, largestUnit, roundingIncrement, roundingMode, smallestUnit, out var settings))
            throw JSEngine.NewRangeError($"{typeName}: the largestUnit, smallestUnit and roundingIncrement options are not valid together");
        return settings;
    }

    // ── relativeTo ───────────────────────────────────────────────────────────────

    /// <summary>
    /// GetTemporalRelativeToOption (s13.19) of an options object, as the library's record: undefined is
    /// <see cref="RelativeTo.None"/>; a <c>ZonedDateTime</c> is its instant with its zone's provider, and a
    /// <c>PlainDate</c> or <c>PlainDateTime</c> its date, each in its own calendar (row D11: any calendar); another object
    /// is a property bag, read as PrepareCalendarFields reads it; a String is ParseISODateTime's with the zoned and
    /// date-time goals; anything else is the <c>TypeError</c>.
    /// </summary>
    internal static RelativeTo RelativeToOption(JSObject options)
    {
        var value = options[KeyStrings.GetOrCreate("relativeTo")];
        if (value == null || value.IsUndefined)
            return RelativeTo.None;

        switch (value)
        {
            case JSTemporalZonedDateTime zoned:
                return ZonedRelativeTo(zoned.epochNanoseconds, zoned.timeZoneId, zoned.calendarId);
            case JSTemporalPlainDate date:
                return PlainRelativeTo(date.IsoDate, date.calendarId);
            case JSTemporalPlainDateTime dateTime:
                return PlainRelativeTo(dateTime.IsoDate, dateTime.calendarId);
            case JSObject bag:
                return BagRelativeTo(bag);
        }

        if (!value.IsString)
            throw JSEngine.NewTypeError("Temporal.Duration: relativeTo must be a Temporal object, a property bag or an ISO string");

        // ParseISODateTime with the TemporalDateTimeString[+Zoned] and [~Zoned] goals, the shared library's grammar; then
        // the zone annotation's identifier, the calendar canonicalized, and InterpretISODateTimeOffset (compatible, offset
        // reject, minutes matched unless the offset has seconds) - or, with no zone, CreateTemporalDate's limits.
        var text = value.ToString();
        var parsed = ParseIsoString(text, [TemporalGoal.ZonedDateTime, TemporalGoal.DateTime], "a Temporal.Duration relativeTo");
        if (parsed.TimeZone is null)
        {
            var plain = JSTemporalPlainDate.FromParsed(parsed, text);
            return PlainRelativeTo(plain.IsoDate, plain.calendarId);
        }

        var instant = JSTemporalZonedDateTime.FromParsed(parsed, text, null);
        return ZonedRelativeTo(instant.epochNanoseconds, instant.timeZoneId, instant.calendarId);
    }

    private static RelativeTo PlainRelativeTo(IsoDate date, string calendarId) =>
        RelativeTo.FromPlainDate(date, Calendars, CalendarIdOf(calendarId));

    private static RelativeTo ZonedRelativeTo(BigInteger epochNanoseconds, string timeZoneId, string calendarId) =>
        RelativeTo.FromZoned(epochNanoseconds, TemporalTimeZones.Arithmetic(timeZoneId), Calendars, CalendarIdOf(calendarId));

    // A relativeTo property bag: GetTemporalCalendarIdentifierWithISODefault, then PrepareCalendarFields of the date
    // fields with the time fields, offset and timeZone, in one pass in code unit order - day, [era, eraYear,] hour,
    // microsecond, millisecond, minute, month, monthCode, nanosecond, offset, second, timeZone, year - each converted as it
    // is read (offset by ToOffsetString, timeZone by ToTemporalTimeZoneIdentifier); then InterpretTemporalDateTimeFields
    // under constrain; then CreateTemporalDate with no zone, or InterpretISODateTimeOffset with one.
    private static RelativeTo BagRelativeTo(JSObject bag)
    {
        const string typeName = "Temporal.Duration";
        var calendarId = CalendarWithIsoDefault(bag);
        var fields = new CalendarFieldReader(bag, calendarId, typeName);

        fields.Day();
        fields.Era();
        var hour = RelativeToTimeField(bag, "hour");
        var microsecond = RelativeToTimeField(bag, "microsecond");
        var millisecond = RelativeToTimeField(bag, "millisecond");
        var minute = RelativeToTimeField(bag, "minute");
        fields.Month();
        fields.MonthCode();
        var nanosecond = RelativeToTimeField(bag, "nanosecond");

        // ToOffsetString: ToPrimitive, a String (else the TypeError), then the grammar's UTC offset (else the RangeError).
        long offsetNanoseconds = 0;
        var offset = RequireOffsetString(bag[KeyStrings.GetOrCreate("offset")], typeName);
        if (offset != null && !TryParseOffsetString(offset, out offsetNanoseconds))
            throw JSEngine.NewRangeError($"{typeName}: invalid relativeTo offset \"{offset}\"");

        var second = RelativeToTimeField(bag, "second");
        var zoneValue = bag[KeyStrings.GetOrCreate("timeZone")];
        var timeZone = zoneValue == null || zoneValue.IsUndefined ? null : JSTemporalZonedDateTime.ToTimeZoneIdentifier(zoneValue);
        fields.Year();

        var date = DateFromFields(calendarId, fields.Fields, "constrain", typeName);
        var time = RegulateTime(hour, minute, second, millisecond, microsecond, nanosecond, "constrain", $"{typeName}: relativeTo time is out of range");
        if (timeZone == null)
            return PlainRelativeTo(date, calendarId);

        return ZonedRelativeTo(RelativeToEpochNanoseconds(new IsoDateTime(date, time), offset != null, offsetNanoseconds, timeZone), timeZone, calendarId);
    }

    private static double RelativeToTimeField(JSObject bag, string name)
    {
        var value = bag[KeyStrings.GetOrCreate(name)];
        return value == null || value.IsUndefined
            ? 0
            : TemporalRoundingOptions.ToIntegerWithTruncation(value, $"Temporal.Duration: relativeTo {name} must be finite");
    }

    // InterpretISODateTimeOffset (s6.5.1) as GetTemporalRelativeToOption calls it for a bag: compatible, offset reject,
    // the offset matched exactly. With no offset the wall clock is read in the zone; with one, CheckISODaysRange, then
    // the candidate instant whose offset is the bag's, else the RangeError.
    private static BigInteger RelativeToEpochNanoseconds(IsoDateTime dateTime, bool hasOffset, long offsetNanoseconds, string timeZone)
    {
        BigInteger epochNanoseconds;
        if (!hasOffset)
        {
            epochNanoseconds = TemporalTimeZones.EpochNanosecondsFor(timeZone, dateTime, "compatible");
        }
        else
        {
            Check(TemporalLimits.CheckIsoDaysRange(dateTime.Date), "Temporal.Duration: relativeTo is outside the representable range");
            var wall = IsoArithmetic.GetUtcEpochNanoseconds(dateTime);
            epochNanoseconds = BigInteger.MinusOne;
            var matched = false;
            foreach (var candidate in TemporalTimeZones.PossibleEpochNanoseconds(timeZone, dateTime))
            {
                if (wall - candidate == offsetNanoseconds)
                {
                    epochNanoseconds = candidate;
                    matched = true;
                    break;
                }
            }

            if (!matched)
                throw JSEngine.NewRangeError($"Temporal.Duration: the relativeTo offset does not match the time zone \"{timeZone}\"");
        }

        if (!IsValidEpochNanoseconds(epochNanoseconds))
            throw JSEngine.NewRangeError("Temporal.Duration: relativeTo is outside the representable range");
        return epochNanoseconds;
    }
}
