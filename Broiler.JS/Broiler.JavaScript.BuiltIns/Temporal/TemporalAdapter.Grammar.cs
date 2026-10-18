using System;
using System.Numerics;
using System.Text.RegularExpressions;
using Broiler.ECMAScript.Temporal;
using Broiler.ECMAScript.Time;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Temporal;

// The grammar: the proposal's ISO 8601 / RFC 9557 grammar is the shared library's TemporalGrammar, a linear
// parser that answers a record or a TemporalParseError (JSD-0078's X6); this engine reads the string first,
// as each ToTemporal… does, and turns each refusal into its RangeError. The coercions of the fields whose
// value must be a String (monthCode, offset) are here too.
internal static partial class TemporalAdapter
{
    /// <summary>
    /// The proposal's ParseISODateTime over a list of goals, the shared library's: the record of the first goal the
    /// string matches, with its annotations checked. Each refusal is the <c>RangeError</c>.
    /// </summary>
    /// <param name="text">The string, already converted.</param>
    /// <param name="goals">The goals, in the order the specification lists them.</param>
    /// <param name="what">What the string was to be, for the message: for example <c>Temporal.PlainDate</c>.</param>
    internal static IsoParseResult ParseIsoString(string text, ReadOnlySpan<TemporalGoal> goals, string what)
    {
        if (TemporalGrammar.TryParse(text, goals, out var parsed, out var error))
            return parsed;

        throw error switch
        {
            TemporalParseError.CriticalCalendarConflict =>
                JSEngine.NewRangeError($"Temporal: more than one calendar annotation, one of them critical, in \"{text}\""),
            TemporalParseError.UnknownCriticalAnnotation =>
                JSEngine.NewRangeError($"Temporal: unknown annotation with critical flag in \"{text}\""),
            TemporalParseError.YearMonthOutsideIsoCalendar =>
                JSEngine.NewRangeError($"Temporal.PlainYearMonth: a non-iso8601 calendar requires a day in \"{text}\""),
            TemporalParseError.MonthDayOutsideIsoCalendar =>
                JSEngine.NewRangeError($"Temporal.PlainMonthDay: a non-iso8601 calendar requires a year in \"{text}\""),
            _ => JSEngine.NewRangeError($"Cannot parse {what} from \"{text}\""),
        };
    }

    /// <summary>The ISO date of a parse record that has one: its year, month and day.</summary>
    internal static IsoDate DateOf(IsoParseResult parsed) => new(parsed.Year!.Value, parsed.Month, parsed.Day);

    /// <summary>
    /// The calendar of a parse record: its first <c>u-ca</c> annotation, or <c>iso8601</c> where it has none,
    /// canonicalized by the shared library's calendars (a <c>RangeError</c> for one they do not know).
    /// </summary>
    internal static string CalendarOf(IsoParseResult parsed) =>
        parsed.Calendar is null ? "iso8601" : CanonicalizeCalendar(parsed.Calendar);

    /// <summary>
    /// The proposal's ParseTemporalInstant, the shared library's: the instant a string names, its offset applied. A
    /// string that is not an instant string, or names no instant, is the <c>RangeError</c>.
    /// </summary>
    internal static BigInteger ParseInstant(string text)
    {
        if (TemporalGrammar.TryParseInstant(text, out var epochNanoseconds, out var error))
            return epochNanoseconds;

        throw error switch
        {
            TemporalParseError.OutOfRange => JSEngine.NewRangeError("Temporal.Instant: parsed value is out of range"),
            TemporalParseError.CriticalCalendarConflict or TemporalParseError.UnknownCriticalAnnotation =>
                JSEngine.NewRangeError($"Temporal: invalid annotation in \"{text}\""),
            _ => JSEngine.NewRangeError($"Cannot parse Temporal.Instant from \"{text}\""),
        };
    }

    /// <summary>
    /// The proposal's ParseTemporalDurationString, the shared library's: the ten fields, each part exact and made a
    /// Number once. A string that is not a duration string is the <c>RangeError</c>; validity is the caller's.
    /// </summary>
    internal static DurationFields ParseDuration(string text)
    {
        if (TemporalGrammar.TryParseDuration(text, out var duration, out _))
            return duration;

        throw JSEngine.NewRangeError($"Cannot parse Temporal.Duration from \"{text}\"");
    }

    /// <summary>
    /// The proposal's ParseTemporalCalendarString, the shared library's: the calendar annotation of an ISO string of
    /// any goal (<c>iso8601</c> where it has none), or else the whole string where it is an annotation value. The
    /// identifier is not canonicalized; anything else is the <c>RangeError</c>.
    /// </summary>
    internal static string ParseCalendarString(string text)
    {
        if (TemporalGrammar.TryParseCalendarString(text, out var calendar, out _))
            return calendar;

        throw JSEngine.NewRangeError($"Temporal: unsupported calendar \"{text}\"");
    }

    /// <summary>
    /// ToTemporalTimeZoneIdentifier of a String: the proposal's ParseTemporalTimeZoneString, the shared library's - a
    /// time zone identifier, or the zone an ISO string names by its annotation, its <c>Z</c> or its offset - then an
    /// offset identifier normalized to <c>±HH:MM</c>, or an available name in the database's case
    /// (GetAvailableNamedTimeZoneIdentifier; a link keeps its own name). Anything else is the <c>RangeError</c>.
    /// </summary>
    internal static string ToTimeZoneIdentifier(string text)
    {
        if (TemporalGrammar.TryParseTimeZoneString(text, out var zone, out _))
        {
            if (zone.Name is null)
                return OffsetTimeZones.FormatOffsetIdentifier(zone.OffsetMinutes);
            if (TemporalTimeZones.Database.TryFind(zone.Name, out var identifier))
                return identifier.Identifier;
        }

        throw JSEngine.NewRangeError($"Temporal: unknown time zone \"{text}\"");
    }

    /// <summary>
    /// The test of the proposal's ToOffsetString and its value, ParseDateTimeUTCOffset, the shared library's: a whole
    /// <c>UTCOffset[+SubMinutePrecision]</c>, ASCII digits only; <c>Z</c> is not an offset string (row G19).
    /// </summary>
    internal static bool TryParseOffsetString(string text, out long offsetNanoseconds) =>
        TemporalGrammar.TryParseOffsetString(text, out offsetNanoseconds);

    // ── the fields whose value must be a String ─────────────────────────────────

    /// <summary>
    /// PrepareCalendarFields' conversion of <c>monthCode</c>: <c>ToPrimitive</c> with hint string, then a
    /// <c>TypeError</c> for a value that is not a String - before the format and calendar checks, which are
    /// <c>RangeError</c>s. <see langword="null"/> for an undefined or missing field, so the caller keeps its
    /// "field is absent" shortcut.
    /// </summary>
    internal static string RequireMonthCodeString(JSValue value, string typeName)
        => RequireStringField(value, typeName, "monthCode");

    /// <summary>
    /// The <c>offset</c> field's conversion, as <c>monthCode</c>'s: a Number, BigInt, null or boolean is a
    /// <c>TypeError</c>; an Object goes through <c>ToPrimitive</c>, whose result must itself be a String.
    /// </summary>
    internal static string RequireOffsetString(JSValue value, string typeName)
        => RequireStringField(value, typeName, "offset");

    private static string RequireStringField(JSValue value, string typeName, string field)
    {
        if (value == null || value.IsUndefined) return null;
        if (value is JSObject obj)
        {
            var primitive = obj.ToStringPrimitive();
            if (!primitive.IsString)
                throw JSEngine.NewTypeError($"{typeName}: {field} must be a string");
            return primitive.StringValue;
        }
        if (!value.IsString)
            throw JSEngine.NewTypeError($"{typeName}: {field} must be a string");
        return value.StringValue;
    }

    // The calendar-independent monthCode syntax: "M", two digits and an optional leap-month "L" ("M01", "M05L").
    private static readonly System.Text.RegularExpressions.Regex MonthCodeSyntaxPattern = new(@"^M\d{2}L?$", RegexOptions.CultureInvariant);

    /// <summary>
    /// The monthCode syntax, checked as the field is read: an ill-formed code such as <c>L99M</c> is a
    /// <c>RangeError</c> before sibling fields like <c>year</c> are converted and before the calendar's own check of
    /// a well-formed code (so <c>L99M</c> with a Symbol year is a <c>RangeError</c>, and a well-formed <c>M99L</c>
    /// with a Symbol year is the year's <c>TypeError</c>). The calendars' check, which part B of SJL-10.5 moves to
    /// the shared library's <c>MonthCode.Parse</c>.
    /// </summary>
    internal static void RequireWellFormedMonthCode(string code, string typeName)
    {
        if (code != null && !MonthCodeSyntaxPattern.IsMatch(code))
            throw JSEngine.NewRangeError($"{typeName}: invalid monthCode \"{code}\"");
    }
}
