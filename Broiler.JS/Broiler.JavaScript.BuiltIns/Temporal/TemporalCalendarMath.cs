namespace Broiler.JavaScript.BuiltIns.Temporal;

// Unifies the two non-Gregorian calendar back ends behind one dispatch surface so Temporal.PlainDate
// can treat them uniformly: the formula calendars (TemporalArithmeticCalendar — coptic / ethiopic /
// ethioaa / islamic / islamic-civil / islamic-tbla / hebrew / indian), which milestone SJL-10 owns, and
// the table-backed calendars (TemporalTableCalendar — chinese / dangi / islamic-umalqura / persian), whose
// years are the shared library's over the calendar tables this engine embeds (milestone SJL-8.6). Every
// calendar here addresses months by 1-based ordinal and is converted to and from the ISO epoch-day axis.
internal static class TemporalCalendarMath
{
    internal static bool IsNonIso(string id)
        => TemporalArithmeticCalendar.IsArithmetic(id) || TemporalTableCalendar.IsTableCalendar(id);

    private static bool Table(string id) => TemporalTableCalendar.IsTableCalendar(id);

    // Whether the calendar can ever contain a leap month (a "MnnL" month code). Only the lunisolar
    // calendars (chinese / dangi) and hebrew (Adar I) do; every solar calendar — persian, indian, the
    // coptic/ethiopic 13-month family and the Islamic calendars — has none, so a leap month code is
    // invalid for them in any year (it must not be silently constrained to the regular month).
    internal static bool HasLeapMonths(string id) => id == "hebrew" || TemporalTableCalendar.IsLunisolar(id);

    internal static int MonthsInYear(string id, int year)
        => Table(id) ? TemporalTableCalendar.MonthsInYear(id, year) : TemporalArithmeticCalendar.MonthsInYear(id, year);

    internal static bool InLeapYear(string id, int year)
        => Table(id) ? TemporalTableCalendar.InLeapYear(id, year) : TemporalArithmeticCalendar.InLeapYear(id, year);

    internal static int DaysInYear(string id, int year)
        => Table(id) ? TemporalTableCalendar.DaysInYear(id, year) : TemporalArithmeticCalendar.DaysInYear(id, year);

    internal static int DaysInMonth(string id, int year, int month)
        => Table(id) ? TemporalTableCalendar.DaysInMonth(id, year, month) : TemporalArithmeticCalendar.DaysInMonth(id, year, month);

    internal static long EpochDaysFromYmd(string id, int year, int month, int day)
        => Table(id) ? TemporalTableCalendar.EpochDaysFromYmd(id, year, month, day) : TemporalArithmeticCalendar.EpochDaysFromYmd(id, year, month, day);

    internal static (int year, int month, int day) YmdFromEpochDays(string id, long epoch)
        => Table(id) ? TemporalTableCalendar.YmdFromEpochDays(id, epoch) : TemporalArithmeticCalendar.YmdFromEpochDays(id, epoch);

    internal static int DayOfYear(string id, int year, int month, int day)
        => Table(id) ? TemporalTableCalendar.DayOfYear(id, year, month, day) : TemporalArithmeticCalendar.DayOfYear(id, year, month, day);

    internal static string MonthCode(string id, int year, int month)
        => Table(id) ? TemporalTableCalendar.MonthCode(id, year, month) : TemporalArithmeticCalendar.MonthCode(id, year, month);

    internal static int OrdinalFromMonthCode(string id, int year, int codeNumber, bool leapMonth)
        => Table(id) ? TemporalTableCalendar.OrdinalFromMonthCode(id, year, codeNumber, leapMonth) : TemporalArithmeticCalendar.OrdinalFromMonthCode(id, year, codeNumber, leapMonth);

    // The lunisolar calendars have no era (era / eraYear are undefined); every other calendar here does.
    internal static bool HasEra(string id) => !TemporalTableCalendar.IsLunisolar(id);

    internal static (string code, int eraYear) Era(string id, int year)
        => Table(id) ? TemporalTableCalendar.Era(id, year) : TemporalArithmeticCalendar.Era(id, year);

    internal static int YearFromEra(string id, string era, int eraYear)
        => Table(id) ? TemporalTableCalendar.YearFromEra(id, era, eraYear) : TemporalArithmeticCalendar.YearFromEra(id, era, eraYear);
}
