using Broiler.ECMAScript.Time;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Temporal;

// The table-backed calendars chinese, dangi, islamic-umalqura and persian (milestone SJL-8.6; decision
// SJL-D-21). Each year - its new year, its months and their lengths, and its leap month - is the shared
// library's CalendarYears.GetYear over the calendar tables this engine embeds: the published years inside
// the tables' ranges, and ICU4X's approximation, the civil tabular year or the 33-year rule outside them.
// What is here is this engine's arithmetic over those years, which stays its own until milestone SJL-10:
// finding the year of a day, a month's ordinal from its code, and the eras.
//
// Field conventions (Temporal / Intl era and month code):
//   * chinese and dangi: the year is the related ISO year in which the calendar year begins; there is no
//     era; a leap month carries the previous month's number with "L" ("M05L").
//   * islamic-umalqura: the year AH, eras "ah" (year >= 1) and "bh" (1 - year); months M01..M12.
//   * persian: the year AP, the single era "ap"; months M01..M12.
internal static class TemporalTableCalendar
{
    // The day 1970-01-01 is R.D. 719163; the civil Hijri epoch is R.D. 227015 and the Persian R.D. 226896.
    private const long CivilHijriEpoch = 227015 - 719163;
    private const long PersianEpoch = 226896 - 719163;

    internal static bool IsTableCalendar(string id) => id is "chinese" or "dangi" or "islamic-umalqura" or "persian";

    internal static bool IsLunisolar(string id) => id is "chinese" or "dangi";

    private static TableCalendar Calendar(string id) => id switch
    {
        "chinese" => TableCalendar.Chinese,
        "dangi" => TableCalendar.Dangi,
        "islamic-umalqura" => TableCalendar.IslamicUmmAlQura,
        _ => TableCalendar.Persian,
    };

    // A year from the shared sources; a year no source answers is out of every representable date's range.
    private static CalendarYear Year(string id, long year)
    {
        if (CalendarYears.GetYear(TemporalTimeData.Calendars, Calendar(id), year, out var result) != TimeStatus.Completed)
            throw JSEngine.NewRangeError($"Temporal: year {year} is outside the supported range of the {id} calendar");
        return result;
    }

    internal static int MonthsInYear(string id, int year) => Year(id, year).MonthsInYear;

    internal static bool InLeapYear(string id, int year)
    {
        var y = Year(id, year);
        return id switch
        {
            "chinese" or "dangi" => y.LeapMonth != 0,
            "islamic-umalqura" => y.DaysInYear == 355,
            _ => y.DaysInYear == 366,
        };
    }

    internal static int DaysInYear(string id, int year) => Year(id, year).DaysInYear;

    internal static int DaysInMonth(string id, int year, int month)
    {
        var y = Year(id, year);
        if (month < 1 || month > y.MonthsInYear)
            throw JSEngine.NewRangeError($"Temporal: month {month} is out of range for the {id} calendar");
        return y.DaysInMonth(month);
    }

    internal static long EpochDaysFromYmd(string id, int year, int month, int day)
    {
        var y = Year(id, year);
        if (month < 1 || month > y.MonthsInYear || day < 1 || day > y.DaysInMonth(month))
            throw JSEngine.NewRangeError("Temporal: date is out of range for the resulting month");
        return y.NewYearEpochDays + DaysBefore(y, month) + (day - 1);
    }

    internal static (int year, int month, int day) YmdFromEpochDays(string id, long epoch)
    {
        var y = Year(id, EstimatedYear(id, epoch));
        while (epoch < y.NewYearEpochDays)
            y = Year(id, y.Year - 1);
        while (epoch >= y.NewYearEpochDays + y.DaysInYear)
            y = Year(id, y.Year + 1);

        var dayOfYear = epoch - y.NewYearEpochDays;
        var month = 1;
        while (dayOfYear >= y.DaysInMonth(month))
        {
            dayOfYear -= y.DaysInMonth(month);
            month++;
        }

        return ((int)y.Year, month, (int)dayOfYear + 1);
    }

    internal static int DayOfYear(string id, int year, int month, int day)
        => (int)(EpochDaysFromYmd(id, year, month, day) - Year(id, year).NewYearEpochDays) + 1;

    internal static string MonthCode(string id, int year, int ordinalMonth)
    {
        var leap = IsLunisolar(id) ? Year(id, year).LeapMonth : 0;
        if (leap == 0 || ordinalMonth < leap) return $"M{ordinalMonth:00}";
        if (ordinalMonth == leap) return $"M{ordinalMonth - 1:00}L";
        return $"M{ordinalMonth - 1:00}";
    }

    // The displayed lunar month number (1..12) of a chinese or dangi ordinal month, the number in its month
    // code: a leap month and every month after it carry the previous month's number. Intl.DateTimeFormat
    // numbers these calendars' months so.
    internal static int DisplayMonthNumber(string id, int year, int ordinalMonth)
    {
        var leap = IsLunisolar(id) ? Year(id, year).LeapMonth : 0;
        return leap != 0 && ordinalMonth >= leap ? ordinalMonth - 1 : ordinalMonth;
    }

    internal static int OrdinalFromMonthCode(string id, int year, int codeNumber, bool leapMonth)
    {
        if (!IsLunisolar(id))
        {
            if (leapMonth)
                throw JSEngine.NewRangeError($"Temporal: the {id} calendar has no leap months");
            return codeNumber;
        }

        var leap = Year(id, year).LeapMonth;
        if (leapMonth)
        {
            if (leap == 0 || leap - 1 != codeNumber)
                throw JSEngine.NewRangeError($"Temporal: the {id} {year} year has no leap month \"M{codeNumber:00}L\"");
            return leap;
        }

        // A lunisolar year has exactly 12 regular months (M01..M12); a leap year adds one leap month. A regular
        // month code outside 1..12, such as "M13", does not exist and is a RangeError, not a 13th ordinal.
        if (codeNumber < 1 || codeNumber > 12)
            throw JSEngine.NewRangeError($"Temporal: the {id} calendar has no month \"M{codeNumber:00}\"");
        if (leap == 0 || codeNumber < leap) return codeNumber;
        return codeNumber + 1;
    }

    // persian has the single era "ap" for any sign of year; islamic-umalqura "ah" from year 1 and "bh" before.
    internal static (string code, int eraYear) Era(string id, int year)
        => id == "persian" ? ("ap", year) : year >= 1 ? ("ah", year) : ("bh", 1 - year);

    internal static int YearFromEra(string id, string era, int eraYear)
    {
        var e = era.ToLowerInvariant();
        if (id == "persian")
        {
            if (e is "ap" or "persian") return eraYear;
            throw JSEngine.NewRangeError($"Temporal: invalid era \"{era}\" for the persian calendar");
        }

        return e switch
        {
            "ah" => eraYear,
            "bh" => 1 - eraYear,
            _ => throw JSEngine.NewRangeError($"Temporal: invalid era \"{era}\" for the {id} calendar"),
        };
    }

    private static long FloorDiv(long a, long b)
    {
        var q = a / b;
        if (a % b != 0 && (a < 0) != (b < 0)) q--;
        return q;
    }

    // The days of a year's months before a month.
    private static long DaysBefore(CalendarYear year, int month)
    {
        long days = 0;
        for (var m = 1; m < month; m++)
            days += year.DaysInMonth(m);
        return days;
    }

    // A year near the one a day falls in, from which the search above steps at most a year or two: the ISO year
    // for the lunisolar calendars, whose years begin between 21 January and 20 February; the tabular year for
    // Umm al-Qura; the 33-year rule's for the Persian calendar.
    private static long EstimatedYear(string id, long epoch)
    {
        var estimate = id switch
        {
            "chinese" or "dangi" => IsoCalendarMath.FromEpochDays(epoch).Item1,
            "islamic-umalqura" => FloorDiv((30 * (epoch - CivilHijriEpoch)) + 10646, 10631),
            _ => 1 + FloorDiv((33 * (epoch - PersianEpoch)) + 3, 12053),
        };

        // Within the sources' bound, so that a day at the instant limits never asks for a year no source answers.
        return System.Math.Clamp(estimate, -CalendarYears.MaxYearMagnitude, CalendarYears.MaxYearMagnitude);
    }
}
