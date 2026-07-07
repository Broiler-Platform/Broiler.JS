using System;
using System.Globalization;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Temporal;

// The Persian (Solar Hijri / Jalali) calendar.
//
// Within the range supported by System.Globalization.PersianCalendar (Persian years 1–9378, ISO
// 0622–9999) the .NET implementation is used: its leap-year sequence and Nowrúz (1 Farvardin) dates
// agree exactly with the Iranian calendar authority's published table for 1206–1498 AP, which is the
// data test262's persian-new-year-dates fixture is generated from.
//
// Outside that range — which Temporal still requires (the supported ISO date range spans Persian
// years ≈ −272442…275139) — the calendar falls back to ICU's arithmetic 33-year-cycle algorithm: a
// leap year recurs on the cycle floor((8·year+21)/33), so a year starts day 365·(year−1)+floor((8·
// year+21)/33) after the Persian epoch (Julian day 1948320, i.e. −492268 days from the 1970 epoch).
// The extreme / non-positive-year test262 cases assert only round-trip consistency and the year
// arithmetic (the authority table does not extend that far), which this fallback satisfies.
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
    private static readonly long NetMinEpoch = ToEpochDays(Persian.MinSupportedDateTime);
    private static readonly long NetMaxEpoch = ToEpochDays(Persian.MaxSupportedDateTime);

    private static long ToEpochDays(DateTime dt) => (long)(dt.Date - Epoch1970).TotalDays;

    // ── ICU 33-year-cycle arithmetic (full-range fallback) ────────────────────────

    private const long PersianEpochIn1970Days = 1948320L - 2440588L; // Julian day 1948320 − JD(1970-01-01)
    private static readonly int[] MonthFirstDay = [0, 31, 62, 93, 124, 155, 186, 216, 246, 276, 306, 336];

    private static long FloorDiv(long a, long b)
    {
        var q = a / b;
        if ((a % b != 0) && ((a < 0) != (b < 0))) q--;
        return q;
    }

    // Days from the Persian epoch to 1 Farvardin of the given Persian year (0-based).
    private static long YearStart(long year) => 365L * (year - 1) + FloorDiv(8L * year + 21, 33);

    private static long ArithmeticEpochDaysFromYmd(int year, int month, int day)
        => YearStart(year) + MonthFirstDay[month - 1] + (day - 1) + PersianEpochIn1970Days;

    private static (int year, int month, int day) ArithmeticYmdFromEpochDays(long epoch)
    {
        var d = epoch - PersianEpochIn1970Days; // days since the Persian epoch
        var year = 1 + FloorDiv(33 * d + 3, 12053);
        var doy = d - YearStart(year);          // 0-based day of year
        if (doy < 0) { year--; doy = d - YearStart(year); }
        else { var len = YearStart(year + 1) - YearStart(year); if (doy >= len) { year++; doy = d - YearStart(year); } }

        int month = doy < 216 ? (int)(doy / 31) : (int)((doy - 6) / 30); // 0-based
        int day = (int)(doy - MonthFirstDay[month]) + 1;
        return ((int)year, month + 1, day);
    }

    private static int ArithmeticDaysInYear(int year) => (int)(YearStart(year + 1) - YearStart(year));

    // ── public surface (delegates to .NET in range, arithmetic outside) ───────────

    internal static (int year, int month, int day) YmdFromEpochDays(long epoch)
    {
        if (epoch < NetMinEpoch || epoch > NetMaxEpoch)
            return ArithmeticYmdFromEpochDays(epoch);
        var dt = Epoch1970.AddDays(epoch);
        return (Persian.GetYear(dt), Persian.GetMonth(dt), Persian.GetDayOfMonth(dt));
    }

    internal static long EpochDaysFromYmd(int year, int month, int day)
    {
        if (month is < 1 or > 12)
            throw JSEngine.NewRangeError("Temporal: month out of range for the persian calendar");
        try { return ToEpochDays(Persian.ToDateTime(year, month, day, 0, 0, 0, 0)); }
        catch (ArgumentOutOfRangeException) { return ArithmeticEpochDaysFromYmd(year, month, day); }
    }

    internal static int MonthsInYear(int year) => 12;

    internal static bool InLeapYear(int year) => DaysInYear(year) == 366;

    internal static int DaysInYear(int year)
    {
        try { return Persian.IsLeapYear(year) ? 366 : 365; }
        catch (ArgumentOutOfRangeException) { return ArithmeticDaysInYear(year); }
    }

    internal static int DaysInMonth(int year, int month)
    {
        if (month is < 1 or > 12) throw JSEngine.NewRangeError("Temporal: month out of range for the persian calendar");
        if (month <= 6) return 31;
        if (month <= 11) return 30;
        return InLeapYear(year) ? 30 : 29;
    }

    internal static int DayOfYear(int year, int month, int day)
        => MonthFirstDay[month - 1] + day;

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
}
