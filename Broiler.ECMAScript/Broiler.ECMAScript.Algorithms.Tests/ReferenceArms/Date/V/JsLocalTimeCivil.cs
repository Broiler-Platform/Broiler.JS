// REFERENCE ARM V: a test-only copy of Broiler.VM's implementation before the cut-over, for the
// reference-arm comparison of bundle sjl-6-001 (docs/evidence/sjl-6-001/reference-arms.md). It is
// never referenced outside this test project, and it is retired at SJL-16.
//
// Copied from Broiler.VM at 9c3d809 (the commit the area verdict's correspondence reads):
//   src/Broiler.VM.Profile.JavaScript/JsDateTimeFormatter.cs
//     blob e4c9728e9666b8070ad4aa23f6a77b7b5a374445: of the record struct JsLocalTime, its declaration with its
//     ten positional fields (:55) and its documentation comment (:49-52), and From (:154-187), whose civil
//     arithmetic is :156-174, with its documentation comment
//
// Changes from the source, none of which changes a result or a charge:
//   - the namespace; the record struct JsLocalTime holds its positional fields (Year, Month, Day, Weekday, Hour,
//     Minute, Second, Millisecond, DayOfYear, Utc) and From only: its init-only calendar properties
//     (CalendarEra, CalendarEraYear, MonthKey, MonthNumber, LeapMonth, CyclicYear), Era, EraYear, InCalendar and
//     EraIndex, which From neither reads nor sets (they keep their defaults in what it returns), are the engine's;
//   - From is unchanged, its signature included: it takes CLR values already (the time value as a double and
//     the zone's offset in seconds as an int; its documentation comment, verbatim, calls the second
//     offsetMinutes) and returns the record of the fields it computes;
//   - the generated assurance header, the per-member assurance annotations (Broiler-AI / Broiler-Human lines) and
//     the SPDX lines removed, and with them the using directives, which name nothing the copied members use.

namespace Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.Date.V;

/// <summary>
/// A time value broken into proleptic Gregorian fields in a zone's local time, with the era year
/// ICU's <c>y</c> writes.
/// </summary>
internal readonly record struct JsLocalTime(long Year, int Month, int Day, int Weekday, int Hour, int Minute, int Second, int Millisecond, int DayOfYear, long Utc)
{
    /// <summary>The fields of <paramref name="time"/>, an integral time value, <paramref name="offsetMinutes"/> from UTC.</summary>
    internal static JsLocalTime From(double time, int offsetSeconds)
    {
        var local = (long)time + (offsetSeconds * 1_000L);
        var days = local >= 0 ? local / 86_400_000L : -((-local + 86_399_999L) / 86_400_000L);
        var within = local - (days * 86_400_000L);

        // HOWARD HINNANT'S civil_from_days, exact over the whole time-value range.
        var z = days + 719_468;
        var era = (z >= 0 ? z : z - 146_096) / 146_097;
        var dayOfEra = z - (era * 146_097);
        var yearOfEra = (dayOfEra - (dayOfEra / 1_460) + (dayOfEra / 36_524) - (dayOfEra / 146_096)) / 365;
        var dayOfYearMarch = dayOfEra - ((365 * yearOfEra) + (yearOfEra / 4) - (yearOfEra / 100));
        var monthMarch = ((5 * dayOfYearMarch) + 2) / 153;
        var day = (int)(dayOfYearMarch - (((153 * monthMarch) + 2) / 5) + 1);
        var month = (int)(monthMarch < 10 ? monthMarch + 3 : monthMarch - 9);
        var year = yearOfEra + (era * 400) + (month <= 2 ? 1 : 0);
        var weekday = (int)(((days % 7) + 11) % 7);

        var leap = year % 4 == 0 && (year % 100 != 0 || year % 400 == 0);
        int[] starts = [0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334];
        var dayOfYear = starts[month - 1] + day + (leap && month > 2 ? 1 : 0);

        return new JsLocalTime(
            year,
            month,
            day,
            weekday,
            (int)(within / 3_600_000),
            (int)(within / 60_000 % 60),
            (int)(within / 1_000 % 60),
            (int)(within % 1_000),
            dayOfYear,
            (long)time);
    }
}
