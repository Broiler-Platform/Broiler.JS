using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Temporal;

// Unifies the two non-Gregorian calendar back ends behind one dispatch surface so Temporal.PlainDate
// can treat them uniformly: the purely-arithmetic family (TemporalArithmeticCalendar — coptic /
// ethiopic / islamic-* / hebrew) and the .NET-backed lunisolar family (TemporalLunisolarCalendar —
// chinese / dangi). Every calendar here addresses months by 1-based ordinal and is converted to and
// from the ISO epoch-day axis; only the arithmetic family exposes eras.
internal static class TemporalCalendarMath
{
    internal static bool IsNonIso(string id)
        => TemporalArithmeticCalendar.IsArithmetic(id) || TemporalLunisolarCalendar.IsLunisolar(id);

    private static bool Luni(string id) => TemporalLunisolarCalendar.IsLunisolar(id);

    internal static int MonthsInYear(string id, int year)
        => Luni(id) ? TemporalLunisolarCalendar.MonthsInYear(id, year) : TemporalArithmeticCalendar.MonthsInYear(id, year);

    internal static bool InLeapYear(string id, int year)
        => Luni(id) ? TemporalLunisolarCalendar.InLeapYear(id, year) : TemporalArithmeticCalendar.InLeapYear(id, year);

    internal static int DaysInYear(string id, int year)
        => Luni(id) ? TemporalLunisolarCalendar.DaysInYear(id, year) : TemporalArithmeticCalendar.DaysInYear(id, year);

    internal static int DaysInMonth(string id, int year, int month)
        => Luni(id) ? TemporalLunisolarCalendar.DaysInMonth(id, year, month) : TemporalArithmeticCalendar.DaysInMonth(id, year, month);

    internal static long EpochDaysFromYmd(string id, int year, int month, int day)
        => Luni(id) ? TemporalLunisolarCalendar.EpochDaysFromYmd(id, year, month, day) : TemporalArithmeticCalendar.EpochDaysFromYmd(id, year, month, day);

    internal static (int year, int month, int day) YmdFromEpochDays(string id, long epoch)
        => Luni(id) ? TemporalLunisolarCalendar.YmdFromEpochDays(id, epoch) : TemporalArithmeticCalendar.YmdFromEpochDays(id, epoch);

    internal static int DayOfYear(string id, int year, int month, int day)
        => Luni(id) ? TemporalLunisolarCalendar.DayOfYear(id, year, month, day) : TemporalArithmeticCalendar.DayOfYear(id, year, month, day);

    internal static string MonthCode(string id, int year, int month)
        => Luni(id) ? TemporalLunisolarCalendar.MonthCode(id, year, month) : TemporalArithmeticCalendar.MonthCode(id, year, month);

    internal static int OrdinalFromMonthCode(string id, int year, int codeNumber, bool leapMonth)
        => Luni(id) ? TemporalLunisolarCalendar.OrdinalFromMonthCode(id, year, codeNumber, leapMonth) : TemporalArithmeticCalendar.OrdinalFromMonthCode(id, year, codeNumber, leapMonth);

    // The lunisolar calendars have no era (era / eraYear are undefined); the arithmetic ones do.
    internal static bool HasEra(string id) => !Luni(id);

    internal static (string code, int eraYear) Era(string id, int year) => TemporalArithmeticCalendar.Era(id, year);

    internal static int YearFromEra(string id, string era, int eraYear) => TemporalArithmeticCalendar.YearFromEra(id, era, eraYear);
}
