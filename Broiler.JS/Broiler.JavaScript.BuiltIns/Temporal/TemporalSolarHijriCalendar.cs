using System;
using System.Globalization;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Temporal;

// The Persian (Solar Hijri / Jalali) calendar, backed by the .NET runtime's
// System.Globalization.PersianCalendar. That implementation's leap-year sequence and Nowrúz (1
// Farvardin) dates agree exactly with the Iranian calendar authority's published table for
// 1206–1498 AP (verified against test262's persian-new-year-dates / persian-calendar-authority
// fixtures), so it matches the ICU-derived data the Temporal proposal uses; reimplementing the
// astronomical determination of the vernal-equinox year is deliberately avoided.
//
// Field conventions (matching Temporal / Intl.Era-monthcode):
//   * the year is the Persian (Anno Persico) year; there is a single era "ap".
//   * 12 months, no leap months: months 1–6 have 31 days, 7–11 have 30, and the 12th has 29
//     (30 in a leap year). Month codes are the plain "M01".."M12".
internal static class TemporalSolarHijriCalendar
{
    internal static bool IsSolarHijri(string id) => id == "persian";

    private static readonly PersianCalendar Persian = new();
    private static readonly DateTime Epoch1970 = new(1970, 1, 1);

    private static long ToEpochDays(DateTime dt) => (long)(dt.Date - Epoch1970).TotalDays;

    private static DateTime FromEpochDays(long epoch)
    {
        if (epoch < ToEpochDays(Persian.MinSupportedDateTime) || epoch > ToEpochDays(Persian.MaxSupportedDateTime))
            throw JSEngine.NewRangeError("Temporal: date is outside the supported range of the persian calendar");
        return Epoch1970.AddDays(epoch);
    }

    internal static (int year, int month, int day) YmdFromEpochDays(long epoch)
    {
        var dt = FromEpochDays(epoch);
        return (Persian.GetYear(dt), Persian.GetMonth(dt), Persian.GetDayOfMonth(dt));
    }

    internal static long EpochDaysFromYmd(int year, int month, int day)
    {
        try { return ToEpochDays(Persian.ToDateTime(year, month, day, 0, 0, 0, 0)); }
        catch (ArgumentOutOfRangeException)
        {
            throw JSEngine.NewRangeError("Temporal: date is out of range for the persian calendar");
        }
    }

    internal static int MonthsInYear(int year) => 12;

    internal static bool InLeapYear(int year) => SafeIsLeapYear(year);

    internal static int DaysInYear(int year) => SafeIsLeapYear(year) ? 366 : 365;

    internal static int DaysInMonth(int year, int month)
    {
        if (month is < 1 or > 12) throw JSEngine.NewRangeError("Temporal: month out of range for the persian calendar");
        if (month <= 6) return 31;
        if (month <= 11) return 30;
        return SafeIsLeapYear(year) ? 30 : 29;
    }

    internal static int DayOfYear(int year, int month, int day)
        => (int)(EpochDaysFromYmd(year, month, day) - EpochDaysFromYmd(year, 1, 1)) + 1;

    internal static string MonthCode(int ordinalMonth) => $"M{ordinalMonth:00}";

    internal static int OrdinalFromMonthCode(int codeNumber, bool leapMonth)
    {
        if (leapMonth)
            throw JSEngine.NewRangeError("Temporal: the persian calendar has no leap months");
        return codeNumber;
    }

    // The persian calendar has the single era "ap" (Anno Persico) for any sign of year.
    internal static (string code, int eraYear) Era(int year) => ("ap", year);

    internal static int YearFromEra(string era, int eraYear)
    {
        if (era.ToLowerInvariant() is "ap" or "persian") return eraYear;
        throw JSEngine.NewRangeError($"Temporal: invalid era \"{era}\" for the persian calendar");
    }

    private static bool SafeIsLeapYear(int year)
    {
        try { return Persian.IsLeapYear(year); }
        catch (ArgumentOutOfRangeException)
        {
            throw JSEngine.NewRangeError($"Temporal: year {year} is outside the supported range of the persian calendar");
        }
    }
}
