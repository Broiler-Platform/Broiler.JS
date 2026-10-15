// REFERENCE ARM J6: a test-only copy of Broiler.JS's implementation before the cut-over, for the
// reference-arm comparison of bundle sjl-6-001 (docs/evidence/sjl-6-001/reference-arms.md). It is
// never referenced outside this test project, and it is retired at SJL-16.
//
// Copied from Broiler.JS at bc3597f (the commit the area verdict's correspondence reads):
//   Broiler.JS/Broiler.JavaScript.BuiltIns/Temporal/JSTemporalZonedDateTime.cs
//     blob d7d3c8a70eb2ad98bd9dc7000d8a64e78492a523: JSTemporalZonedDateTime's IsLeapYear (:2004), DaysInMonthOf
//     (:2006-2012), IsValidISODate (:2014-2015), IsoDayOfWeek (:2017-2021), IsoWeek (:2023-2031), WeeksInIsoYear
//     (:2033-2037), FloorDiv (:2039-2044), DaysFromCivil (:2046-2054) and CivilFromDays (:2056-2068), and the ISO
//     branch of the dayOfYear getter (:239) as DayOfYear
//
// Changes from the source, none of which changes a result:
//   - the source's line endings are LF already, and it has no byte-order mark;
//   - the namespace; the members sit in the static class JSTemporalZonedDateTimeCivil (JSTemporalZonedDateTime is
//     a JSObject partial class in the source); private members are internal;
//   - DayOfYear is the expression the dayOfYear getter returns for the ISO calendar (:239), verbatim, as a
//     function of `l`, the wall-clock fields the getter's `var l = Local()` reads (Local returns nine ints, of
//     which the expression reads y, mo and d, and the parameter carries those three); it returns the
//     expression's int, which the getter widens to its double exactly.

using System.Numerics;

namespace Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.Date.J;

/// <summary>Broiler.JS's Temporal.ZonedDateTime civil members, as copied (arm J6).</summary>
internal static class JSTemporalZonedDateTimeCivil
{
    // The ISO branch of the dayOfYear getter (:239).
    internal static int DayOfYear((int y, int mo, int d) l)
        => (int)(DaysFromCivil(l.y, l.mo, l.d) - DaysFromCivil(l.y, 1, 1)) + 1;

    internal static bool IsLeapYear(long y) => (y % 4 == 0 && y % 100 != 0) || y % 400 == 0;

    internal static int DaysInMonthOf(long year, int month) => month switch
    {
        1 or 3 or 5 or 7 or 8 or 10 or 12 => 31,
        4 or 6 or 9 or 11 => 30,
        2 => IsLeapYear(year) ? 29 : 28,
        _ => 0,
    };

    internal static bool IsValidISODate(int year, int month, int day)
        => month is >= 1 and <= 12 && day >= 1 && day <= DaysInMonthOf(year, month);

    internal static int IsoDayOfWeek(int year, int month, int day)
    {
        var epoch = DaysFromCivil(year, month, day);
        return (int)(((epoch + 3) % 7 + 7) % 7) + 1;
    }

    internal static (int week, int year) IsoWeek(int year, int month, int day)
    {
        var ordinal = (int)(DaysFromCivil(year, month, day) - DaysFromCivil(year, 1, 1)) + 1;
        var weekday = IsoDayOfWeek(year, month, day);
        var week = (ordinal - weekday + 10) / 7;
        if (week < 1) return (WeeksInIsoYear(year - 1), year - 1);
        if (week > WeeksInIsoYear(year)) return (1, year + 1);
        return (week, year);
    }

    internal static int WeeksInIsoYear(int year)
    {
        int P(int y) => (y + y / 4 - y / 100 + y / 400) % 7;
        return P(year) == 4 || P(year - 1) == 3 ? 53 : 52;
    }

    internal static BigInteger FloorDiv(BigInteger a, BigInteger b)
    {
        var q = BigInteger.DivRem(a, b, out var r);
        if (r != 0 && (r < 0) != (b < 0)) q -= 1;
        return q;
    }

    internal static long DaysFromCivil(long y, long m, long d)
    {
        y -= m <= 2 ? 1 : 0;
        var era = (y >= 0 ? y : y - 399) / 400;
        var yoe = y - era * 400;
        var doy = (153 * (m > 2 ? m - 3 : m + 9) + 2) / 5 + d - 1;
        var doe = yoe * 365 + yoe / 4 - yoe / 100 + doy;
        return era * 146097 + doe - 719468;
    }

    internal static (long y, long m, long d) CivilFromDays(long z)
    {
        z += 719468;
        var era = (z >= 0 ? z : z - 146096) / 146097;
        var doe = z - era * 146097;
        var yoe = (doe - doe / 1460 + doe / 36524 - doe / 146096) / 365;
        var y = yoe + era * 400;
        var doy = doe - (365 * yoe + yoe / 4 - yoe / 100);
        var mp = (5 * doy + 2) / 153;
        var d = doy - (153 * mp + 2) / 5 + 1;
        var m = mp < 10 ? mp + 3 : mp - 9;
        return (m <= 2 ? y + 1 : y, m, d);
    }
}
