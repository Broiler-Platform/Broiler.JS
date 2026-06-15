using System;
using System.Globalization;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Core;
using UnicodeCldr.LocaleData;

namespace Broiler.JavaScript.BuiltIns.Temporal;

// The lunisolar calendars chinese and dangi (Korean), backed by the .NET runtime's astronomical
// EastAsianLunisolarCalendar implementations (ChineseLunisolarCalendar / KoreanLunisolarCalendar).
// Those compute true new moons and principal solar terms and agree with ICU / the Temporal proposal
// over their supported span (chinese ≈ 1901–2100, dangi ≈ 918–2050).
//
// Outside the .NET span an astronomical fallback (UnicodeCldr.LocaleData.CldrChineseCalendar, the
// Calendrical Calculations algorithm ICU derives from) takes over. For the chinese calendar it is
// exact and agrees with the .NET calendar across the whole 1901–2100 overlap, so the two meet
// seamlessly at the boundary (both place the 1901 new year on 1901-02-19). Temporal requires the far
// past/future to be representable without throwing but not to be exact, so dangi years outside the
// Korean .NET span (≈ 918–2050) reuse the same Beijing-meridian fallback as an approximation.
//
// Field conventions (matching Temporal / Intl.Era-monthcode):
//   * `year` is the Gregorian year in which the calendar year began (the year of its new-year day).
//   * there is no era (era / eraYear are undefined).
//   * months are addressed by their 1-based ordinal (1..13); the leap month carries the previous
//     month's code with an "L" suffix (e.g. "M05L" for a leap fifth month).
internal static class TemporalLunisolarCalendar
{
    internal static bool IsLunisolar(string id) => id is "chinese" or "dangi";

    private static readonly ChineseLunisolarCalendar Chinese = new();
    private static readonly KoreanLunisolarCalendar Korean = new();
    private static readonly DateTime Epoch1970 = new(1970, 1, 1);

    private static EastAsianLunisolarCalendar Cal(string id) => id == "dangi" ? Korean : Chinese;

    private static long ToEpochDays(DateTime dt) => (long)(dt.Date - Epoch1970).TotalDays;

    // The Gregorian-year span and epoch-day span each .NET back end supports (chinese ≈ 1901–2100,
    // dangi ≈ 918–2050). Years/dates outside a back end's span are served by the astronomical
    // fallback; inside it the .NET calendar is used. The .NET and fallback results agree across the
    // overlap, so the two meet seamlessly at the boundary.
    private static readonly int ChineseNetMinYear =
        Chinese.ToDateTime(Chinese.GetYear(Chinese.MinSupportedDateTime), 1, 1, 0, 0, 0, 0).Year;
    private static readonly int ChineseNetMaxYear =
        Chinese.ToDateTime(Chinese.GetYear(Chinese.MaxSupportedDateTime), 1, 1, 0, 0, 0, 0).Year;
    private static readonly long ChineseNetMinEpoch = ToEpochDays(Chinese.MinSupportedDateTime);
    private static readonly long ChineseNetMaxEpoch = ToEpochDays(Chinese.MaxSupportedDateTime);
    private static readonly int KoreanNetMinYear =
        Korean.ToDateTime(Korean.GetYear(Korean.MinSupportedDateTime), 1, 1, 0, 0, 0, 0).Year;
    private static readonly int KoreanNetMaxYear =
        Korean.ToDateTime(Korean.GetYear(Korean.MaxSupportedDateTime), 1, 1, 0, 0, 0, 0).Year;
    private static readonly long KoreanNetMinEpoch = ToEpochDays(Korean.MinSupportedDateTime);
    private static readonly long KoreanNetMaxEpoch = ToEpochDays(Korean.MaxSupportedDateTime);

    private static int NetMinYear(string id) => id == "dangi" ? KoreanNetMinYear : ChineseNetMinYear;
    private static int NetMaxYear(string id) => id == "dangi" ? KoreanNetMaxYear : ChineseNetMaxYear;
    private static long NetMinEpoch(string id) => id == "dangi" ? KoreanNetMinEpoch : ChineseNetMinEpoch;
    private static long NetMaxEpoch(string id) => id == "dangi" ? KoreanNetMaxEpoch : ChineseNetMaxEpoch;

    // True when (id, year) falls outside the .NET back end's supported span and must use the
    // astronomical fallback. The fallback is the Beijing-meridian chinese algorithm; for the chinese
    // calendar it is exact, and for dangi years outside the Korean .NET span (the far past/future
    // Temporal requires not to throw but does not require to be exact) it is an approximation.
    private static bool UseAstro(string id, int year) => year < NetMinYear(id) || year > NetMaxYear(id);
    private static bool UseAstroEpoch(string id, long epoch) => epoch < NetMinEpoch(id) || epoch > NetMaxEpoch(id);

    // The leap-month ordinal (0 if none) for (id, year), from whichever backend serves the year.
    private static int LeapMonthOrdinal(string id, int year)
        => UseAstro(id, year)
            ? CldrChineseCalendar.GetYear(year).LeapMonthOrdinal
            : LeapOrdinal(Cal(id), NetYear(Cal(id), id, year));

    private static DateTime FromEpochDays(EastAsianLunisolarCalendar cal, string id, long epoch)
    {
        if (epoch < ToEpochDays(cal.MinSupportedDateTime) || epoch > ToEpochDays(cal.MaxSupportedDateTime))
            throw JSEngine.NewRangeError($"Temporal: date is outside the supported range of the {id} calendar");
        return Epoch1970.AddDays(epoch);
    }

    // The .NET calendar year whose new-year day falls in the Gregorian `year` (June 1 of that year is
    // always inside it, the new year being a late-January / February date).
    private static int NetYear(EastAsianLunisolarCalendar cal, string id, int year)
    {
        try { return cal.GetYear(new DateTime(year, 6, 1)); }
        catch (ArgumentOutOfRangeException)
        {
            throw JSEngine.NewRangeError($"Temporal: year {year} is outside the supported range of the {id} calendar");
        }
    }

    internal static (int year, int month, int day) YmdFromEpochDays(string id, long epoch)
    {
        if (UseAstroEpoch(id, epoch))
            return CldrChineseCalendar.FromEpochDay(epoch);

        var cal = Cal(id);
        var dt = FromEpochDays(cal, id, epoch);
        var netYear = cal.GetYear(dt);
        var newYear = cal.ToDateTime(netYear, 1, 1, 0, 0, 0, 0);
        return (newYear.Year, cal.GetMonth(dt), cal.GetDayOfMonth(dt));
    }

    internal static long EpochDaysFromYmd(string id, int year, int month, int day)
    {
        if (UseAstro(id, year))
        {
            var info = CldrChineseCalendar.GetYear(year);
            if (month < 1 || month > info.MonthCount || day < 1 || day > info.DaysInMonth(month))
                throw JSEngine.NewRangeError("Temporal: date is out of range for the resulting month");
            return info.FirstEpochDayOfMonth(month) + (day - 1);
        }

        var cal = Cal(id);
        var netYear = NetYear(cal, id, year);
        try { return ToEpochDays(cal.ToDateTime(netYear, month, day, 0, 0, 0, 0)); }
        catch (ArgumentOutOfRangeException)
        {
            throw JSEngine.NewRangeError("Temporal: date is out of range for the resulting month");
        }
    }

    internal static int MonthsInYear(string id, int year)
    {
        if (UseAstro(id, year)) return CldrChineseCalendar.GetYear(year).MonthCount;
        var cal = Cal(id);
        return cal.GetMonthsInYear(NetYear(cal, id, year));
    }

    internal static bool InLeapYear(string id, int year)
    {
        if (UseAstro(id, year)) return CldrChineseCalendar.GetYear(year).LeapMonthOrdinal != 0;
        var cal = Cal(id);
        return cal.IsLeapYear(NetYear(cal, id, year));
    }

    internal static int DaysInYear(string id, int year)
    {
        if (UseAstro(id, year)) return CldrChineseCalendar.GetYear(year).DaysInYear;
        var cal = Cal(id);
        return cal.GetDaysInYear(NetYear(cal, id, year));
    }

    internal static int DaysInMonth(string id, int year, int month)
    {
        if (UseAstro(id, year)) return CldrChineseCalendar.GetYear(year).DaysInMonth(month);
        var cal = Cal(id);
        return cal.GetDaysInMonth(NetYear(cal, id, year), month);
    }

    internal static int DayOfYear(string id, int year, int month, int day)
    {
        if (UseAstro(id, year))
        {
            var info = CldrChineseCalendar.GetYear(year);
            return (int)(info.FirstEpochDayOfMonth(month) - info.NewYearEpochDay) + day;
        }
        var cal = Cal(id);
        var netYear = NetYear(cal, id, year);
        return cal.GetDayOfYear(cal.ToDateTime(netYear, month, day, 0, 0, 0, 0));
    }

    // The .NET leap-month ordinal for the calendar year (0 if none); ordinal L is the leap month and
    // repeats month L-1.
    private static int LeapOrdinal(EastAsianLunisolarCalendar cal, int netYear) => cal.GetLeapMonth(netYear);

    internal static string MonthCode(string id, int year, int ordinalMonth)
    {
        var leap = LeapMonthOrdinal(id, year);
        if (leap == 0 || ordinalMonth < leap) return $"M{ordinalMonth:00}";
        if (ordinalMonth == leap) return $"M{ordinalMonth - 1:00}L";
        return $"M{ordinalMonth - 1:00}";
    }

    internal static int OrdinalFromMonthCode(string id, int year, int codeNumber, bool leapMonth)
    {
        var leap = LeapMonthOrdinal(id, year);
        if (leapMonth)
        {
            if (leap == 0 || leap - 1 != codeNumber)
                throw JSEngine.NewRangeError($"Temporal: the {id} {year} year has no leap month \"M{codeNumber:00}L\"");
            return leap;
        }
        if (leap == 0 || codeNumber < leap) return codeNumber;
        return codeNumber + 1;
    }
}
