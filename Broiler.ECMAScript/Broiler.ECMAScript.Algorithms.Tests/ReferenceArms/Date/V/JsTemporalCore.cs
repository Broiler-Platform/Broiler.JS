// REFERENCE ARM V: a test-only copy of Broiler.VM's implementation before the cut-over, for the
// reference-arm comparison of bundle sjl-6-001 (docs/evidence/sjl-6-001/reference-arms.md). It is
// never referenced outside this test project, and it is retired at SJL-16.
//
// Copied from Broiler.VM at 9c3d809 (the commit the area verdict's correspondence reads):
//   src/Broiler.VM.Profile.JavaScript/JsTemporalCore.cs
//     blob 544b4a6cdbe35903a5fb9041e1109eb680b358fc: the record struct JsIsoDate (:49); of the static class
//     JsTemporalCore, the section comment "---- ISO dates" (:174), IsLeapYear (:179), DaysInMonth (:184-189),
//     DaysInYear (:194), EpochDays(long, int, int) (:199-207), EpochDays(JsIsoDate) (:212), FromEpochDays
//     (:217-228), IsValidIsoDate (:233-234), AddDays (:239), BalanceYearMonth (:244-249), DayOfWeek (:263-267),
//     DayOfYear (:272) and WeekOfYear (:277-302), with each member its documentation comment
//
// Changes from the source, none of which changes a result or a charge:
//   - the namespace; the static class JsTemporalCore holds only the members above, its other members (Compare
//     at :254-258 among them, which sits between BalanceYearMonth and DayOfWeek) and the types TemporalUnit,
//     JsTimeRecord, JsIsoDateTime, JsDateDuration and JsInternalDuration being the engine's; the copied members
//     call no member but each other, and no helper (the floor divisions are written inline in their bodies);
//   - JsIsoDate, the profile's ISO Date Record, which EpochDays(JsIsoDate), FromEpochDays, AddDays, DayOfWeek,
//     DayOfYear and WeekOfYear take or return, is copied whole: it is a plain record of three CLR fields;
//   - the class's documentation comment is the arm's; the source's describes the members not copied;
//   - the generated assurance header, the per-member assurance annotations (Broiler-AI / Broiler-Human lines) and
//     the SPDX lines removed, and with them the using directives, which name nothing the copied members use.

namespace Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.Date.V;

/// <summary>An ISO Date Record (s3.5.1): a valid ISO 8601 date, the year not necessarily within Temporal's range.</summary>
internal readonly record struct JsIsoDate(long Year, int Month, int Day);

/// <summary>Broiler.VM's civil members of JsTemporalCore, as copied.</summary>
internal static class JsTemporalCore
{
    // ---- ISO dates --------------------------------------------------------------------------

    /// <summary>Whether a year is a leap year in the proleptic Gregorian calendar.</summary>
    internal static bool IsLeapYear(long year) => year % 4 == 0 && (year % 100 != 0 || year % 400 == 0);

    /// <summary>The proposal's ISODaysInMonth (s12.3.17).</summary>
    internal static int DaysInMonth(long year, int month) => month switch
    {
        2 => IsLeapYear(year) ? 29 : 28,
        4 or 6 or 9 or 11 => 30,
        _ => 31,
    };

    /// <summary>The proposal's MathematicalDaysInYear.</summary>
    internal static int DaysInYear(long year) => IsLeapYear(year) ? 366 : 365;

    /// <summary>Days since 1970-01-01 of a valid ISO date (ISODateToEpochDays with a 1-based month).</summary>
    internal static long EpochDays(long year, int month, int day)
    {
        var y = year - (month <= 2 ? 1 : 0);
        var era = (y >= 0 ? y : y - 399) / 400;
        var yearOfEra = y - (era * 400);
        var dayOfYear = (((153 * (month + (month > 2 ? -3 : 9))) + 2) / 5) + day - 1;
        var dayOfEra = (yearOfEra * 365) + (yearOfEra / 4) - (yearOfEra / 100) + dayOfYear;
        return (era * 146097) + dayOfEra - 719468;
    }

    /// <summary>Days since the epoch of an ISO date.</summary>
    internal static long EpochDays(JsIsoDate date) => EpochDays(date.Year, date.Month, date.Day);

    /// <summary>The ISO date of a day since the epoch.</summary>
    internal static JsIsoDate FromEpochDays(long days)
    {
        var z = days + 719468;
        var era = (z >= 0 ? z : z - 146096) / 146097;
        var dayOfEra = z - (era * 146097);
        var yearOfEra = (dayOfEra - (dayOfEra / 1460) + (dayOfEra / 36524) - (dayOfEra / 146096)) / 365;
        var dayOfYear = dayOfEra - ((365 * yearOfEra) + (yearOfEra / 4) - (yearOfEra / 100));
        var monthPart = ((5 * dayOfYear) + 2) / 153;
        var day = (int)(dayOfYear - (((153 * monthPart) + 2) / 5) + 1);
        var month = (int)(monthPart < 10 ? monthPart + 3 : monthPart - 9);
        return new JsIsoDate(yearOfEra + (era * 400) + (month <= 2 ? 1 : 0), month, day);
    }

    /// <summary>The proposal's IsValidISODate (s3.5.8).</summary>
    internal static bool IsValidIsoDate(double year, double month, double day) =>
        month is >= 1 and <= 12 && day >= 1 && day <= DaysInMonth((long)year, (int)month);

    /// <summary>The proposal's AddDaysToISODate (s3.5.9).</summary>
    internal static JsIsoDate AddDays(JsIsoDate date, long days) => FromEpochDays(EpochDays(date) + days);

    /// <summary>The proposal's BalanceISOYearMonth: a year and a month 1 to 12.</summary>
    internal static (long Year, int Month) BalanceYearMonth(long year, long month)
    {
        var zeroBased = month - 1;
        var carry = zeroBased >= 0 ? zeroBased / 12 : ((zeroBased + 1) / 12) - 1;
        return (year + carry, (int)(zeroBased - (carry * 12)) + 1);
    }

    /// <summary>The proposal's ISODayOfWeek (s12.3.20): Monday 1 to Sunday 7.</summary>
    internal static int DayOfWeek(JsIsoDate date)
    {
        var weekday = (int)(((EpochDays(date) + 4) % 7 + 7) % 7);
        return weekday == 0 ? 7 : weekday;
    }

    /// <summary>The proposal's ISODayOfYear (s12.3.19).</summary>
    internal static int DayOfYear(JsIsoDate date) => (int)(EpochDays(date) - EpochDays(date.Year, 1, 1)) + 1;

    /// <summary>The proposal's ISOWeekOfYear (s12.3.18): the week and the week-year.</summary>
    internal static (int Week, long Year) WeekOfYear(JsIsoDate date)
    {
        var year = date.Year;
        var dayOfYear = DayOfYear(date);
        var dayOfWeek = DayOfWeek(date);
        var week = (dayOfYear + 7 - dayOfWeek + 3) / 7;

        if (week < 1)
        {
            var jan1 = DayOfWeek(new JsIsoDate(year, 1, 1));

            if (jan1 == 5 || (jan1 == 6 && IsLeapYear(year - 1)))
            {
                return (53, year - 1);
            }

            return (52, year - 1);
        }

        if (week == 53 && DaysInYear(year) - dayOfYear < 4 - dayOfWeek)
        {
            return (1, year + 1);
        }

        return (week, year);
    }
}
