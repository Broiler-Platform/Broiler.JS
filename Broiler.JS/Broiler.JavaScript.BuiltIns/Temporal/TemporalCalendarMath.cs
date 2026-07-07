namespace Broiler.JavaScript.BuiltIns.Temporal;

// Unifies the two non-Gregorian calendar back ends behind one dispatch surface so Temporal.PlainDate
// can treat them uniformly: the purely-arithmetic family (TemporalArithmeticCalendar — coptic /
// ethiopic / islamic-* / hebrew) and the .NET-backed lunisolar family (TemporalLunisolarCalendar —
// chinese / dangi). Every calendar here addresses months by 1-based ordinal and is converted to and
// from the ISO epoch-day axis; only the arithmetic family exposes eras.
internal static class TemporalCalendarMath
{
    internal static bool IsNonIso(string id)
        => TemporalArithmeticCalendar.IsArithmetic(id) || TemporalLunisolarCalendar.IsLunisolar(id)
           || TemporalSolarHijriCalendar.IsSolarHijri(id);

    private static bool Luni(string id) => TemporalLunisolarCalendar.IsLunisolar(id);
    private static bool Persian(string id) => TemporalSolarHijriCalendar.IsSolarHijri(id);

    // Whether the calendar can ever contain a leap month (a "MnnL" month code). Only the lunisolar
    // calendars (chinese / dangi) and hebrew (Adar I) do; every solar calendar — persian, indian, the
    // coptic/ethiopic 13-month family and the tabular Islamic calendars — has none, so a leap month
    // code is invalid for them in any year (it must not be silently constrained to the regular month).
    internal static bool HasLeapMonths(string id) => id == "hebrew" || Luni(id);

    internal static int MonthsInYear(string id, int year)
        => Persian(id) ? TemporalSolarHijriCalendar.MonthsInYear(year)
         : Luni(id) ? TemporalLunisolarCalendar.MonthsInYear(id, year) : TemporalArithmeticCalendar.MonthsInYear(id, year);

    internal static bool InLeapYear(string id, int year)
        => Persian(id) ? TemporalSolarHijriCalendar.InLeapYear(year)
         : Luni(id) ? TemporalLunisolarCalendar.InLeapYear(id, year) : TemporalArithmeticCalendar.InLeapYear(id, year);

    internal static int DaysInYear(string id, int year)
        => Persian(id) ? TemporalSolarHijriCalendar.DaysInYear(year)
         : Luni(id) ? TemporalLunisolarCalendar.DaysInYear(id, year) : TemporalArithmeticCalendar.DaysInYear(id, year);

    internal static int DaysInMonth(string id, int year, int month)
        => Persian(id) ? TemporalSolarHijriCalendar.DaysInMonth(year, month)
         : Luni(id) ? TemporalLunisolarCalendar.DaysInMonth(id, year, month) : TemporalArithmeticCalendar.DaysInMonth(id, year, month);

    internal static long EpochDaysFromYmd(string id, int year, int month, int day)
        => Persian(id) ? TemporalSolarHijriCalendar.EpochDaysFromYmd(year, month, day)
         : Luni(id) ? TemporalLunisolarCalendar.EpochDaysFromYmd(id, year, month, day) : TemporalArithmeticCalendar.EpochDaysFromYmd(id, year, month, day);

    internal static (int year, int month, int day) YmdFromEpochDays(string id, long epoch)
        => Persian(id) ? TemporalSolarHijriCalendar.YmdFromEpochDays(epoch)
         : Luni(id) ? TemporalLunisolarCalendar.YmdFromEpochDays(id, epoch) : TemporalArithmeticCalendar.YmdFromEpochDays(id, epoch);

    internal static int DayOfYear(string id, int year, int month, int day)
        => Persian(id) ? TemporalSolarHijriCalendar.DayOfYear(year, month, day)
         : Luni(id) ? TemporalLunisolarCalendar.DayOfYear(id, year, month, day) : TemporalArithmeticCalendar.DayOfYear(id, year, month, day);

    internal static string MonthCode(string id, int year, int month)
        => Persian(id) ? TemporalSolarHijriCalendar.MonthCode(month)
         : Luni(id) ? TemporalLunisolarCalendar.MonthCode(id, year, month) : TemporalArithmeticCalendar.MonthCode(id, year, month);

    internal static int OrdinalFromMonthCode(string id, int year, int codeNumber, bool leapMonth)
        => Persian(id) ? TemporalSolarHijriCalendar.OrdinalFromMonthCode(codeNumber, leapMonth)
         : Luni(id) ? TemporalLunisolarCalendar.OrdinalFromMonthCode(id, year, codeNumber, leapMonth) : TemporalArithmeticCalendar.OrdinalFromMonthCode(id, year, codeNumber, leapMonth);

    // The lunisolar calendars have no era (era / eraYear are undefined); the arithmetic and
    // solar-hijri calendars do.
    internal static bool HasEra(string id) => !Luni(id);

    internal static (string code, int eraYear) Era(string id, int year)
        => Persian(id) ? TemporalSolarHijriCalendar.Era(year) : TemporalArithmeticCalendar.Era(id, year);

    internal static int YearFromEra(string id, string era, int eraYear)
        => Persian(id) ? TemporalSolarHijriCalendar.YearFromEra(era, eraYear) : TemporalArithmeticCalendar.YearFromEra(id, era, eraYear);
}
