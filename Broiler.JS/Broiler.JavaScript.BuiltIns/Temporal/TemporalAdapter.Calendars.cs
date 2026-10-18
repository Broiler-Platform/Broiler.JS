using Broiler.ECMAScript.Temporal;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Temporal;

// The calendars: the identifiers of the Intl era and month code proposal's Table 1 are the shared library's
// TemporalCalendars (JSD-0078's X6, decision D3). What stays here is this engine's: a Temporal object's
// [[Calendar]] slot, which holds the canonical identifier as a string, and the calendar argument's coercion.
internal static partial class TemporalAdapter
{
    // ── identifiers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// CanonicalizeCalendar, the shared library's: Table 1's calendar types and its two aliases (<c>ethiopic-amete-alem</c>
    /// and <c>islamicc</c>) under ASCII case folding alone. <c>gregorian</c> and <c>minguo</c> are not calendars (row C2,
    /// decision D5), nor is <c>islamic</c> (row C3).
    /// </summary>
    internal static bool TryCanonicalizeCalendar(string identifier, out string calendarId)
    {
        if (TemporalCalendars.Canonicalize(identifier, out var calendar) == CalendarStatus.Completed)
        {
            calendarId = TemporalCalendars.Identifier(calendar);
            return true;
        }

        calendarId = null;
        return false;
    }

    /// <summary>CanonicalizeCalendar, as <see cref="TryCanonicalizeCalendar"/>; an identifier it does not know is the <c>RangeError</c>.</summary>
    internal static string CanonicalizeCalendar(string identifier) =>
        TryCanonicalizeCalendar(identifier, out var calendarId)
            ? calendarId
            : throw JSEngine.NewRangeError($"Temporal: unsupported calendar \"{identifier}\"");

    /// <summary>
    /// A constructor's calendar argument: undefined is <c>iso8601</c>, a value that is not a String is the
    /// <c>TypeError</c>, and the String is canonicalized as an identifier, never parsed as an ISO string.
    /// </summary>
    internal static string CalendarArgument(JSValue calendar, string typeName)
    {
        if (calendar == null || calendar.IsUndefined)
            return "iso8601";
        if (!calendar.IsString)
            throw JSEngine.NewTypeError($"{typeName}: calendar must be a calendar identifier string");
        return CanonicalizeCalendar(calendar.StringValue);
    }

    /// <summary>
    /// The calendar a Temporal object holds in its <c>[[Calendar]]</c> slot, or <see langword="null"/> for any other
    /// value. A <c>PlainMonthDay</c> lends its own calendar (row C13), as every other type with one does.
    /// </summary>
    internal static string CalendarSlotOf(JSValue value) => value switch
    {
        JSTemporalPlainDate date => date.calendarId,
        JSTemporalPlainDateTime dateTime => dateTime.calendarId,
        JSTemporalPlainMonthDay monthDay => monthDay.calendarId,
        JSTemporalPlainYearMonth yearMonth => yearMonth.calendarId,
        JSTemporalZonedDateTime zoned => zoned.calendarId,
        _ => null,
    };

    /// <summary>
    /// ToTemporalCalendarIdentifier (s12.3.10): a Temporal object's own calendar; else a String, read by the grammar's
    /// ParseTemporalCalendarString and canonicalized; anything else is the <c>TypeError</c>.
    /// </summary>
    internal static string ToCalendarIdentifier(JSValue calendar)
    {
        if (CalendarSlotOf(calendar) is { } held)
            return held;
        if (calendar == null || !calendar.IsString)
            throw JSEngine.NewTypeError("Temporal: calendar must be a calendar identifier string or a Temporal object with a calendar");
        return CanonicalizeCalendar(ParseCalendarString(calendar.StringValue));
    }

    /// <summary>
    /// GetTemporalCalendarIdentifierWithISODefault (s12.3.11) of a property bag: its <c>calendar</c> property, read once,
    /// <c>iso8601</c> where it is undefined.
    /// </summary>
    internal static string CalendarWithIsoDefault(JSObject item)
    {
        var calendar = item[KeyStrings.GetOrCreate("calendar")];
        return calendar == null || calendar.IsUndefined ? "iso8601" : ToCalendarIdentifier(calendar);
    }

    /// <summary>
    /// IsPartialTemporalObject's refusals, each a <c>TypeError</c>: a Temporal object (it carries its own calendar or time
    /// zone), or an object with a <c>calendar</c> or a <c>timeZone</c> property, own or inherited.
    /// </summary>
    internal static void RejectObjectWithCalendarOrTimeZone(JSObject obj)
    {
        if (obj is JSTemporalPlainDate or JSTemporalPlainDateTime or JSTemporalPlainMonthDay
            or JSTemporalPlainTime or JSTemporalPlainYearMonth or JSTemporalZonedDateTime)
            throw JSEngine.NewTypeError("Temporal: a Temporal object is not a valid fields object");

        if (!obj[KeyStrings.GetOrCreate("calendar")].IsUndefined)
            throw JSEngine.NewTypeError("Temporal: a fields object must not have a calendar property");

        if (!obj[KeyStrings.GetOrCreate("timeZone")].IsUndefined)
            throw JSEngine.NewTypeError("Temporal: a fields object must not have a timeZone property");
    }
}
