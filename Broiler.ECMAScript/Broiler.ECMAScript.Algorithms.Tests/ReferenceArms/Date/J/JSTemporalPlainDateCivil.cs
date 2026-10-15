// REFERENCE ARM J: a test-only copy of Broiler.JS's implementation before the cut-over, for the
// reference-arm comparison of bundle sjl-6-001 (docs/evidence/sjl-6-001/reference-arms.md). It is
// never referenced outside this test project, and it is retired at SJL-16.
//
// Copied from Broiler.JS at bc3597f (the commit the area verdict's correspondence reads):
//   Broiler.JS/Broiler.JavaScript.BuiltIns/Temporal/JSTemporalPlainDate.cs
//     blob c0fec913e70b9b24027d3daa7c52472be6c60144: JSTemporalPlainDate's MinEpochDays and MaxEpochDays (:26-27),
//     IsLeapYear (:799), DaysInMonthOf (:801-807), IsValidISODate (:809-816), EpochDaysFor (:1157),
//     DateFromEpochDays (:1159-1163), IsoDayOfWeek (:1182-1187), IsoWeek (:1190-1203), WeeksInIsoYear
//     (:1205-1209), FloorDiv (:1211-1216), DaysFromCivil (:1219-1227) and CivilFromDays (:1229-1241), and the ISO
//     branch of the dayOfYear getter (:119) as DayOfYear
//
// Changes from the source, none of which changes a result:
//   - the source's line endings are LF already, and it has no byte-order mark;
//   - the namespace; the members sit in the static class JSTemporalPlainDateCivil (JSTemporalPlainDate is a
//     JSObject partial class in the source); private members are internal;
//   - MinEpochDays and MaxEpochDays are copied because IsValidISODate reads them;
//   - DayOfYear is the expression the dayOfYear getter returns for the ISO calendar (:119), verbatim, as a
//     function of the date's fields isoYear, isoMonth and isoDay (the instance's fields in the source); it
//     returns the expression's int, which the getter widens to its double exactly.

namespace Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.Date.J;

/// <summary>Broiler.JS's Temporal.PlainDate civil members, as copied (arm J).</summary>
internal static class JSTemporalPlainDateCivil
{
    internal static readonly long MinEpochDays = DaysFromCivil(-271821, 4, 19);
    internal static readonly long MaxEpochDays = DaysFromCivil(275760, 9, 13);

    // The ISO branch of the dayOfYear getter (:119).
    internal static int DayOfYear(int isoYear, int isoMonth, int isoDay)
        => (int)(DaysFromCivil(isoYear, isoMonth, isoDay) - DaysFromCivil(isoYear, 1, 1)) + 1;

    internal static bool IsLeapYear(long y) => (y % 4 == 0 && y % 100 != 0) || y % 400 == 0;

    internal static int DaysInMonthOf(long year, int month) => month switch
    {
        1 or 3 or 5 or 7 or 8 or 10 or 12 => 31,
        4 or 6 or 9 or 11 => 30,
        2 => IsLeapYear(year) ? 29 : 28,
        _ => 0,
    };

    internal static bool IsValidISODate(int year, int month, int day)
    {
        if (month is < 1 or > 12) return false;
        if (day < 1 || day > DaysInMonthOf(year, month)) return false;

        var epoch = DaysFromCivil(year, month, day);
        return epoch >= MinEpochDays && epoch <= MaxEpochDays;
    }

    internal static long EpochDaysFor(int year, int month, int day) => DaysFromCivil(year, month, day);

    internal static (int y, int m, int d) DateFromEpochDays(long epoch)
    {
        var (y, m, d) = CivilFromDays(epoch);
        return ((int)y, (int)m, (int)d);
    }

    internal static int IsoDayOfWeek(int year, int month, int day)
    {
        var epoch = DaysFromCivil(year, month, day);
        // 1970-01-01 (epoch day 0) is a Thursday (ISO weekday 4).
        return (int)(((epoch + 3) % 7 + 7) % 7) + 1;
    }

    internal static (int week, int year) IsoWeek(int year, int month, int day)
    {
        var ordinal = (int)(DaysFromCivil(year, month, day) - DaysFromCivil(year, 1, 1)) + 1;
        var weekday = IsoDayOfWeek(year, month, day);
        var week = (ordinal - weekday + 10) / 7;

        if (week < 1)
            return (WeeksInIsoYear(year - 1), year - 1);

        if (week > WeeksInIsoYear(year))
            return (1, year + 1);

        return (week, year);
    }

    internal static int WeeksInIsoYear(int year)
    {
        int P(int y) => (y + y / 4 - y / 100 + y / 400) % 7;
        return P(year) == 4 || P(year - 1) == 3 ? 53 : 52;
    }

    internal static long FloorDiv(long a, long b)
    {
        var q = a / b;
        if (a % b != 0 && (a < 0) != (b < 0)) q -= 1;
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
