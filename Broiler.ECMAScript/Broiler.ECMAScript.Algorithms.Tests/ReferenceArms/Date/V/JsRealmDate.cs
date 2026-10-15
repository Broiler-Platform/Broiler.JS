// REFERENCE ARM V: a test-only copy of Broiler.VM's implementation before the cut-over, for the
// reference-arm comparison of bundle sjl-6-001 (docs/evidence/sjl-6-001/reference-arms.md). It is
// never referenced outside this test project, and it is retired at SJL-16.
//
// Copied from Broiler.VM at 9c3d809 (the commit the area verdict's correspondence reads):
//   src/Broiler.VM.Profile.JavaScript/JsRealm.Date.cs
//     blob 2f2c2c68fad380d58740a46de08498d0eaa4ce33: DateMsPerSecond (:53), DateMsPerMinute (:58), DateMsPerHour
//     (:63), DateMsPerDay (:68), DateMaxTimeValue (:73), DateInvalidText (:78) and DateZoneText (:83); the year
//     computation DateFromFields writes inline (:450-460), as DateMakeFullYear; DateFloorMod (:605), DateDay (:610),
//     DateTimeWithinDay (:615), DateDaysInYear (:620-633), DateDayFromYear (:638-642), DateTimeFromYear (:647),
//     DateYearFromTime (:657-672), DateInLeapYear (:677-678), DateDayWithinYear (:683-684), DateMonthStartDay
//     (:689-703), DateMonthFromTime (:708-764), DateDateFromTime (:769-772), DateWeekDay (:780), DateHourFromTime
//     (:785-786), DateMinFromTime (:791-792), DateSecFromTime (:797-798), DateMsFromTime (:803), DateMakeTime
//     (:808-820), DateMakeDay (:830-851), DateMakeDate (:856-864) and DateTimeClip (:869-879); DateParseText
//     (:901-1045), DateParseTextual (:1067-1163), DateMonthByName (:1168-1183), DateWholeNumber (:1188-1209) and
//     DateReadDigits (:1214-1237); DatePadded (:1244-1255), DateWeekDayName (:1260-1274), DateMonthName
//     (:1279-1298), DateToCalendarText (:1303-1305), DateToClockText (:1310-1313), DateToFullText (:1318-1321),
//     DateToUtcText (:1326-1329) and DateToIsoText (:1339-1354); with each member its documentation comment, and
//     the three section comments between them (:594, :881, :1239)
//
// Changes from the source, none of which changes a result or a charge:
//   - the namespace; the members sit in a static class JsRealmDate of their own instead of the partial class
//     JsRealm, whose other members are the engine's; private members, the constants among them, are internal;
//   - the per-member assurance annotations (Broiler-AI / Broiler-Human lines), the generated assurance header
//     and the SPDX lines removed;
//   - JsEngine is the arm's JsEngine below, which carries only the meter: Charge(ulong), the source's signature
//     (JsEngine.Metering.cs :155), adds its units to Charged, which the comparison reads, where the source's
//     charges fuel in poll-window steps and aborts when the allowance is spent. DateParseText's one charge,
//     `engine.Charge((ulong)text.Length + 1)` on entry before anything is read, is the source's line, unchanged,
//     and DateParseTextual and every other copied member charge nothing. No copied member raises an engine
//     error, so the arm has no JsThrow;
//   - MakeFullYear has no member in Broiler.VM: DateFromFields writes it inline (:450-460) on its first
//     argument's Number, converted before it (:442). It is the member DateMakeFullYear, which takes that Number
//     as its parameter `year`, whose body is those lines verbatim followed by `return year;`, and whose
//     documentation comment is the arm's; the rest of DateFromFields is not copied;
//   - IntegerConversions.ToIntegerOrInfinity, which the source calls from the shared library's package
//     (0.1.0-preview.3, the lock file at 9c3d809), is this repository's Broiler.ECMAScript.Algorithms, the test
//     project's reference, as every arm's call of the shared library is.

using System;
using System.Globalization;
using Broiler.ECMAScript.Numbers;

namespace Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.Date.V;

/// <summary>The arm's engine: the meter Broiler.VM's Date members charge.</summary>
internal sealed class JsEngine
{
    public ulong Charged { get; private set; }

    public void Charge(ulong units) => Charged += units;
}

/// <summary>Broiler.VM's Date members, as copied.</summary>
internal static class JsRealmDate
{
    /// <summary>Milliseconds in a second.</summary>
    internal const double DateMsPerSecond = 1000;

    /// <summary>Milliseconds in a minute.</summary>
    internal const double DateMsPerMinute = 60000;

    /// <summary>Milliseconds in an hour.</summary>
    internal const double DateMsPerHour = 3600000;

    /// <summary>Milliseconds in a day.</summary>
    internal const double DateMsPerDay = 86400000;

    /// <summary>The largest magnitude a time value may have before it clips to NaN.</summary>
    internal const double DateMaxTimeValue = 8.64e15;

    /// <summary>What every string-producing method answers for a NaN time value.</summary>
    internal const string DateInvalidText = "Invalid Date";

    /// <summary>The zone tail this profile's fixed UTC offset produces.</summary>
    internal const string DateZoneText = " GMT+0000 (Coordinated Universal Time)";

    /// <summary>
    /// MakeFullYear as <c>DateFromFields</c> writes it inline: the year its first argument's Number makes.
    /// </summary>
    internal static double DateMakeFullYear(double year)
    {
        if (!double.IsNaN(year))
        {
            var integral = IntegerConversions.ToIntegerOrInfinity(year);

            // THE TWO-DIGIT YEAR WINDOW: 0..99 MEANS 1900..1999. It is a compatibility wart the
            // language kept, and `new Date(99, 0, 1)` being 1999 rather than year 99 is observable.
            if (integral >= 0 && integral <= 99)
            {
                year = 1900 + integral;
            }
        }

        return year;
    }

    // ---- the specification's own operations over a time value ------------------------------------

    /// <summary>
    /// The specification's <c>modulo</c>, which takes the sign of the divisor.
    /// </summary>
    /// <remarks>
    /// C#'s <c>%</c> takes the sign of the dividend, so <c>-1 % 24</c> is <c>-1</c> where the
    /// language needs <c>23</c>. Every clock field of a pre-epoch date depends on the difference.
    /// </remarks>
    internal static double DateFloorMod(double left, double right) => left - (right * Math.Floor(left / right));

    /// <summary>The specification's <c>Day</c>.</summary>
    internal static double DateDay(double time) => Math.Floor(time / DateMsPerDay);

    /// <summary>The specification's <c>TimeWithinDay</c>.</summary>
    internal static double DateTimeWithinDay(double time) => DateFloorMod(time, DateMsPerDay);

    /// <summary>The specification's <c>DaysInYear</c>.</summary>
    internal static int DateDaysInYear(double year)
    {
        if (year % 4 != 0)
        {
            return 365;
        }

        if (year % 100 != 0)
        {
            return 366;
        }

        return year % 400 != 0 ? 365 : 366;
    }

    /// <summary>The specification's <c>DayFromYear</c>.</summary>
    internal static double DateDayFromYear(double year) =>
        (365 * (year - 1970)) +
        Math.Floor((year - 1969) / 4) -
        Math.Floor((year - 1901) / 100) +
        Math.Floor((year - 1601) / 400);

    /// <summary>The specification's <c>TimeFromYear</c>.</summary>
    internal static double DateTimeFromYear(double year) => DateMsPerDay * DateDayFromYear(year);

    /// <summary>The specification's <c>YearFromTime</c>.</summary>
    /// <remarks>
    /// The mean Gregorian year puts the estimate within one of the answer for every representable
    /// time value, so the two corrections below run at most twice between them. A search would be
    /// correct too and would cost a hundred times as much on the hot path <c>getFullYear</c> is on.
    /// </remarks>
    internal static double DateYearFromTime(double time)
    {
        var year = Math.Floor(time / (DateMsPerDay * 365.2425)) + 1970;

        while (DateTimeFromYear(year) > time)
        {
            year--;
        }

        while (DateTimeFromYear(year + 1) <= time)
        {
            year++;
        }

        return year;
    }

    /// <summary>The specification's <c>InLeapYear</c>, as the zero or one it adds to a month start.</summary>
    internal static int DateInLeapYear(double time) =>
        DateDaysInYear(DateYearFromTime(time)) == 366 ? 1 : 0;

    /// <summary>The specification's <c>DayWithinYear</c>.</summary>
    internal static double DateDayWithinYear(double time) =>
        DateDay(time) - DateDayFromYear(DateYearFromTime(time));

    /// <summary>The day of the year the given month starts on.</summary>
    internal static double DateMonthStartDay(int month, int leap) => month switch
    {
        0 => 0,
        1 => 31,
        2 => 59 + leap,
        3 => 90 + leap,
        4 => 120 + leap,
        5 => 151 + leap,
        6 => 181 + leap,
        7 => 212 + leap,
        8 => 243 + leap,
        9 => 273 + leap,
        10 => 304 + leap,
        _ => 334 + leap,
    };

    /// <summary>The specification's <c>MonthFromTime</c>.</summary>
    internal static double DateMonthFromTime(double time)
    {
        var within = DateDayWithinYear(time);
        var leap = DateInLeapYear(time);

        if (within < 31)
        {
            return 0;
        }

        if (within < 59 + leap)
        {
            return 1;
        }

        if (within < 90 + leap)
        {
            return 2;
        }

        if (within < 120 + leap)
        {
            return 3;
        }

        if (within < 151 + leap)
        {
            return 4;
        }

        if (within < 181 + leap)
        {
            return 5;
        }

        if (within < 212 + leap)
        {
            return 6;
        }

        if (within < 243 + leap)
        {
            return 7;
        }

        if (within < 273 + leap)
        {
            return 8;
        }

        if (within < 304 + leap)
        {
            return 9;
        }

        return within < 334 + leap ? 10 : 11;
    }

    /// <summary>The specification's <c>DateFromTime</c>: the one-based day of the month.</summary>
    internal static double DateDateFromTime(double time) =>
        DateDayWithinYear(time) -
        DateMonthStartDay((int)DateMonthFromTime(time), DateInLeapYear(time)) +
        1;

    /// <summary>The specification's <c>WeekDay</c>: Sunday is zero.</summary>
    /// <remarks>
    /// The <c>+ 4</c> is the epoch's own weekday: 1970-01-01 was a Thursday.
    /// </remarks>
    internal static double DateWeekDay(double time) => DateFloorMod(DateDay(time) + 4, 7);

    /// <summary>The specification's <c>HourFromTime</c>.</summary>
    internal static double DateHourFromTime(double time) =>
        DateFloorMod(Math.Floor(time / DateMsPerHour), 24);

    /// <summary>The specification's <c>MinFromTime</c>.</summary>
    internal static double DateMinFromTime(double time) =>
        DateFloorMod(Math.Floor(time / DateMsPerMinute), 60);

    /// <summary>The specification's <c>SecFromTime</c>.</summary>
    internal static double DateSecFromTime(double time) =>
        DateFloorMod(Math.Floor(time / DateMsPerSecond), 60);

    /// <summary>The specification's <c>msFromTime</c>.</summary>
    internal static double DateMsFromTime(double time) => DateFloorMod(time, DateMsPerSecond);

    /// <summary>The specification's <c>MakeTime</c>.</summary>
    internal static double DateMakeTime(double hour, double minute, double second, double milli)
    {
        if (!double.IsFinite(hour) || !double.IsFinite(minute) ||
            !double.IsFinite(second) || !double.IsFinite(milli))
        {
            return double.NaN;
        }

        return (IntegerConversions.ToIntegerOrInfinity(hour) * DateMsPerHour) +
            (IntegerConversions.ToIntegerOrInfinity(minute) * DateMsPerMinute) +
            (IntegerConversions.ToIntegerOrInfinity(second) * DateMsPerSecond) +
            IntegerConversions.ToIntegerOrInfinity(milli);
    }

    /// <summary>The specification's <c>MakeDay</c>.</summary>
    /// <remarks>
    /// The fields roll over rather than being validated: month 12 is January of the next year and
    /// day 0 is the last day of the previous month. That is not leniency, it is the operation
    /// <c>setMonth</c> and <c>new Date(y, m, 0)</c> are both defined in terms of.
    /// </remarks>
    internal static double DateMakeDay(double year, double month, double day)
    {
        if (!double.IsFinite(year) || !double.IsFinite(month) || !double.IsFinite(day))
        {
            return double.NaN;
        }

        var wholeYear = IntegerConversions.ToIntegerOrInfinity(year);
        var wholeMonth = IntegerConversions.ToIntegerOrInfinity(month);
        var wholeDay = IntegerConversions.ToIntegerOrInfinity(day);
        var carried = wholeYear + Math.Floor(wholeMonth / 12);

        if (!double.IsFinite(carried))
        {
            return double.NaN;
        }

        var within = DateFloorMod(wholeMonth, 12);
        var leap = DateDaysInYear(carried) == 366 ? 1 : 0;

        return DateDayFromYear(carried) + DateMonthStartDay((int)within, leap) + wholeDay - 1;
    }

    /// <summary>The specification's <c>MakeDate</c>.</summary>
    internal static double DateMakeDate(double day, double time)
    {
        if (!double.IsFinite(day) || !double.IsFinite(time))
        {
            return double.NaN;
        }

        return (day * DateMsPerDay) + time;
    }

    /// <summary>The specification's <c>TimeClip</c>.</summary>
    internal static double DateTimeClip(double time)
    {
        if (!double.IsFinite(time) || Math.Abs(time) > DateMaxTimeValue)
        {
            return double.NaN;
        }

        // 𝔽(! ToIntegerOrInfinity(time)), the shared library's (SJL-4.6): a negative zero, and a time in
        // (-1, 0), is +0, which keeps `new Date(-0.5).getTime()` from printing "-0".
        return IntegerConversions.ToIntegerOrInfinity(time);
    }

    // ---- parsing ---------------------------------------------------------------------------------

    /// <summary>
    /// Parses the Date Time String Format, answering NaN for anything else.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The accepted shapes are <c>YYYY</c>, <c>YYYY-MM</c> and <c>YYYY-MM-DD</c>, each optionally
    /// followed by <c>THH:mm</c>, <c>THH:mm:ss</c> or <c>THH:mm:ss.sss</c>, and that optionally
    /// followed by <c>Z</c> or <c>±HH:mm</c>. Nothing else parses - no leading or trailing space,
    /// no expanded <c>±YYYYYY</c> year, and none of the legacy shapes an engine accepts for
    /// compatibility with the web.
    /// </para>
    /// <para>
    /// A date-only form is UTC and a date-time form without an offset is local time; this profile
    /// fixes local time to UTC, so the two agree and one computation serves both.
    /// </para>
    /// </remarks>
    internal static double DateParseText(JsEngine engine, string text)
    {
        engine.Charge((ulong)text.Length + 1);

        var at = 0;
        double year;

        // THE EXPANDED YEAR: a sign and exactly six digits, which is how `toISOString` writes a
        // year outside 0 to 9999, so `Date.parse` must read it back; `-000000` is not a year
        // (21.4.1.32.1). Until 2026-10-03 every such string parsed as NaN (JSC-252).
        if (text.Length > 0 && (text[0] == '+' || text[0] == '-'))
        {
            var negative = text[0] == '-';
            at = 1;

            if (!DateReadDigits(text, ref at, 6, out year) || (negative && year == 0))
            {
                return double.NaN;
            }

            if (negative)
            {
                year = -year;
            }
        }
        else if (!DateReadDigits(text, ref at, 4, out year))
        {
            // NOT ISO 8601, SO TRY THE TWO FORMS THIS REALM ITSELF PRODUCES. The specification
            // requires `Date.parse` to accept whatever `toString` and `toUTCString` answered, and
            // a parser that read only the ISO form failed its own output: `Date.parse(d.toUTCString())`
            // was `NaN`, so a round trip through the format a program is most likely to have stored
            // did not come back.
            return DateParseTextual(text);
        }

        double month = 1;
        double day = 1;
        double hour = 0;
        double minute = 0;
        double second = 0;
        double milli = 0;
        var offsetMinutes = 0.0;

        if (at < text.Length && text[at] == '-')
        {
            at++;

            if (!DateReadDigits(text, ref at, 2, out month))
            {
                return double.NaN;
            }

            if (at < text.Length && text[at] == '-')
            {
                at++;

                if (!DateReadDigits(text, ref at, 2, out day))
                {
                    return double.NaN;
                }
            }
        }

        if (at < text.Length && text[at] == 'T')
        {
            at++;

            if (!DateReadDigits(text, ref at, 2, out hour) || at >= text.Length || text[at] != ':')
            {
                return double.NaN;
            }

            at++;

            if (!DateReadDigits(text, ref at, 2, out minute))
            {
                return double.NaN;
            }

            if (at < text.Length && text[at] == ':')
            {
                at++;

                if (!DateReadDigits(text, ref at, 2, out second))
                {
                    return double.NaN;
                }

                if (at < text.Length && text[at] == '.')
                {
                    at++;

                    if (!DateReadDigits(text, ref at, 3, out milli))
                    {
                        return double.NaN;
                    }
                }
            }

            if (at < text.Length && text[at] == 'Z')
            {
                at++;
            }
            else if (at < text.Length && (text[at] == '+' || text[at] == '-'))
            {
                var negative = text[at] == '-';
                at++;

                if (!DateReadDigits(text, ref at, 2, out var offsetHour) ||
                    at >= text.Length || text[at] != ':')
                {
                    return double.NaN;
                }

                at++;

                if (!DateReadDigits(text, ref at, 2, out var offsetMinute) ||
                    offsetHour > 23 || offsetMinute > 59)
                {
                    return double.NaN;
                }

                offsetMinutes = (offsetHour * 60) + offsetMinute;

                if (negative)
                {
                    offsetMinutes = -offsetMinutes;
                }
            }
        }

        if (at != text.Length ||
            month < 1 || month > 12 ||
            day < 1 || day > 31 ||
            minute > 59 || second > 59 ||
            hour > 24 || (hour == 24 && (minute != 0 || second != 0 || milli != 0)))
        {
            return double.NaN;
        }

        var parsed = DateMakeDate(
            DateMakeDay(year, month - 1, day), DateMakeTime(hour, minute, second, milli));

        return DateTimeClip(parsed - (offsetMinutes * DateMsPerMinute));
    }

    /// <summary>
    /// Parses the two textual forms this realm renders: <c>toUTCString</c>'s and <c>toString</c>'s.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>It is written to accept what this realm PRODUCES and a little either side, not to be a
    /// date-string parser.</b> The specification leaves every non-ISO format implementation-defined
    /// and requires exactly one thing: that the output of <c>toString</c>, <c>toUTCString</c> and
    /// <c>toISOString</c> parse back. Accepting more than that would be inventing a dialect nobody
    /// asked for, and every program that relied on it would be relying on this implementation.
    /// </para>
    /// <para>
    /// The reader is deliberately positional-by-token rather than by character: a weekday if one is
    /// there, a month name and a day in either order, a four-digit year, an optional clock, and an
    /// optional zone. That covers <c>Thu, 01 Jan 1970 00:00:00 GMT</c>,
    /// <c>Thu Jan 01 1970 00:00:00 GMT+0000 (…)</c> and <c>Thu Jan 01 1970</c> with one walk.
    /// </para>
    /// </remarks>
    internal static double DateParseTextual(string text)
    {
        double month = -1;
        double day = -1;
        double year = double.NaN;
        double hour = 0;
        double minute = 0;
        double second = 0;
        var offsetMinutes = 0.0;

        foreach (var token in text.Split(
            [' ', ',', '\t'], StringSplitOptions.RemoveEmptyEntries))
        {
            if (token.Length != 0 && token[0] == '(')
            {
                // The parenthesised zone NAME, which `toString` appends and which carries nothing
                // the offset has not already said.
                break;
            }

            var named = DateMonthByName(token);

            if (named >= 0 && month < 0)
            {
                month = named;
                continue;
            }

            if (token.Contains(':', StringComparison.Ordinal))
            {
                var parts = token.Split(':');

                if (parts.Length is < 2 or > 3 ||
                    !DateWholeNumber(parts[0], out hour) ||
                    !DateWholeNumber(parts[1], out minute) ||
                    (parts.Length == 3 && !DateWholeNumber(parts[2], out second)))
                {
                    return double.NaN;
                }

                continue;
            }

            if (token.StartsWith("GMT", StringComparison.Ordinal) ||
                token.StartsWith("UTC", StringComparison.Ordinal))
            {
                var zone = token[3..];

                if (zone.Length == 0)
                {
                    continue;
                }

                if (zone.Length != 5 || (zone[0] != '+' && zone[0] != '-') ||
                    !DateWholeNumber(zone[1..3], out var zoneHour) ||
                    !DateWholeNumber(zone[3..], out var zoneMinute))
                {
                    return double.NaN;
                }

                offsetMinutes = (zoneHour * 60) + zoneMinute;

                if (zone[0] == '-')
                {
                    offsetMinutes = -offsetMinutes;
                }

                continue;
            }

            if (!DateWholeNumber(token, out var number))
            {
                // A weekday name, or anything else this form carries and the value does not need.
                continue;
            }

            if (token.Length >= 3 || (day >= 0 && double.IsNaN(year)))
            {
                year = number;
            }
            else
            {
                day = number;
            }
        }

        if (month < 0 || day < 1 || day > 31 || double.IsNaN(year) ||
            hour > 24 || minute > 59 || second > 59)
        {
            return double.NaN;
        }

        var parsed = DateMakeDate(
            DateMakeDay(year, month, day), DateMakeTime(hour, minute, second, 0));

        return DateTimeClip(parsed - (offsetMinutes * DateMsPerMinute));
    }

    /// <summary>The month a three-letter name stands for, or <c>-1</c>.</summary>
    internal static int DateMonthByName(string token) => token switch
    {
        "Jan" => 0,
        "Feb" => 1,
        "Mar" => 2,
        "Apr" => 3,
        "May" => 4,
        "Jun" => 5,
        "Jul" => 6,
        "Aug" => 7,
        "Sep" => 8,
        "Oct" => 9,
        "Nov" => 10,
        "Dec" => 11,
        _ => -1,
    };

    /// <summary>Reads a token that is entirely decimal digits.</summary>
    internal static bool DateWholeNumber(string token, out double value)
    {
        value = 0;

        if (token.Length == 0)
        {
            return false;
        }

        foreach (var character in token)
        {
            if (character is < '0' or > '9')
            {
                value = 0;
                return false;
            }

            value = (value * 10) + (character - '0');
        }

        return true;
    }

    /// <summary>Reads exactly <paramref name="count"/> decimal digits, advancing the cursor.</summary>
    internal static bool DateReadDigits(string text, ref int at, int count, out double value)
    {
        value = 0;

        if (at + count > text.Length)
        {
            return false;
        }

        for (var step = 0; step < count; step++)
        {
            var character = text[at + step];

            if (character is < '0' or > '9')
            {
                return false;
            }

            value = (value * 10) + (character - '0');
        }

        at += count;
        return true;
    }

    // ---- rendering -------------------------------------------------------------------------------

    /// <summary>A non-negative field as a zero-padded decimal, with a sign when it is negative.</summary>
    internal static string DatePadded(double value, int width)
    {
        var negative = value < 0;
        var digits = ((long)Math.Abs(value)).ToString(CultureInfo.InvariantCulture);

        if (digits.Length < width)
        {
            digits = new string('0', width - digits.Length) + digits;
        }

        return negative ? "-" + digits : digits;
    }

    /// <summary>The three-letter weekday name.</summary>
    internal static string DateWeekDayName(double time)
    {
        var weekDay = (int)DateWeekDay(time);

        return weekDay switch
        {
            0 => "Sun",
            1 => "Mon",
            2 => "Tue",
            3 => "Wed",
            4 => "Thu",
            5 => "Fri",
            _ => "Sat",
        };
    }

    /// <summary>The three-letter month name.</summary>
    internal static string DateMonthName(double time)
    {
        var month = (int)DateMonthFromTime(time);

        return month switch
        {
            0 => "Jan",
            1 => "Feb",
            2 => "Mar",
            3 => "Apr",
            4 => "May",
            5 => "Jun",
            6 => "Jul",
            7 => "Aug",
            8 => "Sep",
            9 => "Oct",
            10 => "Nov",
            _ => "Dec",
        };
    }

    /// <summary>The specification's <c>DateString</c>: <c>"Thu Jan 01 1970"</c>.</summary>
    internal static string DateToCalendarText(double time) =>
        DateWeekDayName(time) + " " + DateMonthName(time) + " " +
        DatePadded(DateDateFromTime(time), 2) + " " + DatePadded(DateYearFromTime(time), 4);

    /// <summary>The clock half of the specification's <c>TimeString</c>: <c>"00:00:00"</c>.</summary>
    internal static string DateToClockText(double time) =>
        DatePadded(DateHourFromTime(time), 2) + ":" +
        DatePadded(DateMinFromTime(time), 2) + ":" +
        DatePadded(DateSecFromTime(time), 2);

    /// <summary>What <c>Date.prototype.toString</c> answers.</summary>
    internal static string DateToFullText(double time) =>
        double.IsNaN(time)
            ? DateInvalidText
            : DateToCalendarText(time) + " " + DateToClockText(time) + DateZoneText;

    /// <summary>What <c>Date.prototype.toUTCString</c> answers.</summary>
    internal static string DateToUtcText(double time) =>
        DateWeekDayName(time) + ", " + DatePadded(DateDateFromTime(time), 2) + " " +
        DateMonthName(time) + " " + DatePadded(DateYearFromTime(time), 4) + " " +
        DateToClockText(time) + " GMT";

    /// <summary>What <c>Date.prototype.toISOString</c> answers.</summary>
    /// <remarks>
    /// A year outside 0000..9999 takes the expanded six-digit form with a mandatory sign, so
    /// <c>new Date(8.64e15).toISOString()</c> is <c>"+275760-09-13T00:00:00.000Z"</c> rather than a
    /// truncation that would parse back as a different year.
    /// </remarks>
    internal static string DateToIsoText(double time)
    {
        var year = DateYearFromTime(time);

        var yearText = year >= 0 && year <= 9999
            ? DatePadded(year, 4)
            : (year < 0 ? "-" : "+") + DatePadded(Math.Abs(year), 6);

        return yearText + "-" +
            DatePadded(DateMonthFromTime(time) + 1, 2) + "-" +
            DatePadded(DateDateFromTime(time), 2) + "T" +
            DatePadded(DateHourFromTime(time), 2) + ":" +
            DatePadded(DateMinFromTime(time), 2) + ":" +
            DatePadded(DateSecFromTime(time), 2) + "." +
            DatePadded(DateMsFromTime(time), 3) + "Z";
    }
}
