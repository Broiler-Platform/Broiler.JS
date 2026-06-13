using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Broiler.JavaScript.BuiltIns.Temporal;

// Shared parsing of a Temporal ISO 8601 string for the *slot-value* contexts where a calendar or a
// time zone may be supplied as a full date/time string rather than a bare identifier:
//
//   • ToTemporalCalendarSlotValue → ParseTemporalCalendarString: a calendar field given as a
//     Temporal string adopts the string's [u-ca=…] annotation (defaulting to iso8601).
//   • ToTemporalTimeZoneIdentifier → ParseTemporalTimeZoneString: a timeZone field given as a
//     Temporal string adopts the string's time-zone designator — a [TimeZone] annotation, a Z
//     (UTC) designator, or a numeric UTC offset, in that order of preference.
//
// The parser validates that the input is a plausible Temporal string (date, date-time, time-only,
// year-month or month-day, each with optional annotations) and is intentionally permissive about
// the calendar/offset payloads it does not itself canonicalize — the callers run the extracted
// designator through the strict calendar / time-zone canonicalizers.
internal static class TemporalIsoString
{
    // A date or date-time. Time, fraction, and Z / numeric-offset designators are all optional; the
    // date portion is validated separately by IsValidDate.
    private static readonly Regex DateTimePattern = new(
        @"^(?<y>\d{4}|\+\d{6}|-\d{6})-(?<mo>\d{2})-(?<d>\d{2})" +
        @"(?:[Tt ](?<h>\d{2})(?::?(?<mi>\d{2})(?::?(?<s>\d{2})(?:[.,](?<f>\d{1,9}))?)?)?" +
        @"(?<offset>[Zz]|[+-]\d{2}(?::?\d{2}(?::?\d{2}(?:[.,]\d{1,9})?)?)?)?)?$",
        RegexOptions.CultureInvariant);

    // A bare time-of-day string, optionally prefixed with the time designator and carrying a Z or a
    // numeric offset.
    private static readonly Regex TimePattern = new(
        @"^[Tt]?(?<h>\d{2})(?::?(?<mi>\d{2})(?::?(?<s>\d{2})(?:[.,](?<f>\d{1,9}))?)?)?" +
        @"(?<offset>[Zz]|[+-]\d{2}(?::?\d{2}(?::?\d{2}(?:[.,]\d{1,9})?)?)?)?$",
        RegexOptions.CultureInvariant);

    private static readonly Regex YearMonthPattern = new(
        @"^(?<y>\d{4}|\+\d{6}|-\d{6})-?(?<mo>\d{2})$", RegexOptions.CultureInvariant);

    private static readonly Regex MonthDayPattern = new(
        @"^(?:--)?(?<mo>\d{2})-?(?<d>\d{2})$", RegexOptions.CultureInvariant);

    // A trailing [key=value] / [!key=value] / [TimeZone] annotation.
    private static readonly Regex TrailingAnnotation = new(@"\[(!?)([^\]]*)\]$", RegexOptions.CultureInvariant);

    private struct Parsed
    {
        public string CalendarAnnotation; // the value of the first [u-ca=…] annotation, or null
        public string TimeZoneAnnotation; // the value of a [TimeZone] annotation, or null
        public bool HasZ;                 // a Z / z designator was present
        public string Offset;             // a numeric UTC offset (±HH[:MM[:SS]]), or null
        public bool HasTime;              // a date-time / time-only form (carries a wall clock)
    }

    // Parses a Temporal ISO string, separating its trailing annotations from the date/time core and
    // validating the core. Returns false for anything that is not a recognizable Temporal string.
    private static bool TryParse(string text, out Parsed parsed)
    {
        parsed = default;
        if (text == null) return false;

        // Peel off trailing [..] annotations, recording the calendar (u-ca) and time-zone ones. A
        // time-zone annotation is the bracket whose contents are not a key=value pair.
        var core = text;
        var annotations = new List<string>();
        while (true)
        {
            var m = TrailingAnnotation.Match(core);
            if (!m.Success || m.Index + m.Length != core.Length) break;
            annotations.Add(m.Groups[2].Value);
            core = core.Substring(0, m.Index);
        }

        foreach (var ann in annotations) // outermost-first; the leftmost time-zone annotation wins
        {
            var eq = ann.IndexOf('=');
            if (eq < 0)
            {
                parsed.TimeZoneAnnotation ??= ann;
            }
            else if (parsed.CalendarAnnotation == null &&
                     ann.Substring(0, eq).Equals("u-ca", StringComparison.Ordinal))
            {
                parsed.CalendarAnnotation = ann.Substring(eq + 1);
            }
        }

        var dt = DateTimePattern.Match(core);
        if (dt.Success)
        {
            if (!IsValidDate(dt.Groups["y"].Value, dt.Groups["mo"].Value, dt.Groups["d"].Value)) return false;
            FillTime(ref parsed, dt);
            return true;
        }

        if (YearMonthPattern.Match(core) is { Success: true } ym)
        {
            return IsValidDate(ym.Groups["y"].Value, ym.Groups["mo"].Value, "01");
        }

        if (MonthDayPattern.Match(core) is { Success: true } md)
        {
            return IsValidDate("2000", md.Groups["mo"].Value, md.Groups["d"].Value);
        }

        var t = TimePattern.Match(core);
        if (t.Success)
        {
            if (!IsValidTime(t)) return false;
            FillTime(ref parsed, t);
            return true;
        }

        return false;
    }

    private static void FillTime(ref Parsed parsed, Match m)
    {
        if (!m.Groups["h"].Success) return; // a date-only string carries no clock / offset
        parsed.HasTime = true;
        var offset = m.Groups["offset"];
        if (!offset.Success) return;
        var value = offset.Value;
        if (value is "Z" or "z") parsed.HasZ = true;
        else parsed.Offset = value;
    }

    private static bool IsValidDate(string yearText, string monthText, string dayText)
    {
        var year = int.Parse(yearText, CultureInfo.InvariantCulture);
        var month = int.Parse(monthText, CultureInfo.InvariantCulture);
        var day = int.Parse(dayText, CultureInfo.InvariantCulture);
        if (month is < 1 or > 12) return false;
        return day >= 1 && day <= DaysInMonth(year, month);
    }

    private static bool IsValidTime(Match m)
    {
        var hour = int.Parse(m.Groups["h"].Value, CultureInfo.InvariantCulture);
        var minute = m.Groups["mi"].Success ? int.Parse(m.Groups["mi"].Value, CultureInfo.InvariantCulture) : 0;
        var second = m.Groups["s"].Success ? int.Parse(m.Groups["s"].Value, CultureInfo.InvariantCulture) : 0;
        return hour <= 23 && minute <= 59 && second <= 60; // a leap second (60) parses, then collapses
    }

    private static bool IsLeapYear(int y) => (y % 4 == 0 && y % 100 != 0) || y % 400 == 0;

    private static int DaysInMonth(int year, int month) => month switch
    {
        1 or 3 or 5 or 7 or 8 or 10 or 12 => 31,
        4 or 6 or 9 or 11 => 30,
        2 => IsLeapYear(year) ? 29 : 28,
        _ => 0,
    };

    // ParseTemporalCalendarString: succeeds for any valid Temporal ISO string, yielding its
    // [u-ca=…] annotation value (or null, meaning the iso8601 default).
    internal static bool TryExtractCalendar(string text, out string calendar)
    {
        if (TryParse(text, out var parsed))
        {
            calendar = parsed.CalendarAnnotation;
            return true;
        }

        calendar = null;
        return false;
    }

    // ParseTemporalTimeZoneString: succeeds only when the string carries a time-zone designator —
    // a [TimeZone] annotation, a Z (UTC) designator, or a numeric UTC offset, in that order — and
    // returns it for the caller to canonicalize.
    internal static bool TryExtractTimeZone(string text, out string designator)
    {
        designator = null;
        if (!TryParse(text, out var parsed)) return false;

        if (parsed.TimeZoneAnnotation != null) designator = parsed.TimeZoneAnnotation;
        else if (parsed.HasZ) designator = "UTC";
        else if (parsed.Offset != null) designator = parsed.Offset;

        return designator != null;
    }
}
