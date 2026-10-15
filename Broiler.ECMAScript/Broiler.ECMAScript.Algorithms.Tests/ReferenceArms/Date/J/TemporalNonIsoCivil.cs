// REFERENCE ARM J9: a test-only copy of Broiler.JS's implementation before the cut-over, for the
// reference-arm comparison of bundle sjl-6-001 (docs/evidence/sjl-6-001/reference-arms.md). It is
// never referenced outside this test project, and it is retired at SJL-16.
//
// Copied from Broiler.JS at bc3597f (the commit the area verdict's correspondence reads):
//   Broiler.JS/Broiler.JavaScript.BuiltIns/Temporal/TemporalNonIso.cs
//     blob 0161bf2557061ea5817454033829212f697503bc: TemporalNonIso's DaysFromCivil (:605-613) and CivilFromDays
//     (:615-627)
//
// Changes from the source, none of which changes a result:
//   - the source's line endings are LF already, and it has no byte-order mark;
//   - the namespace; the members sit in the static class TemporalNonIsoCivil (TemporalNonIso is a static class in
//     the source); both members are internal in the source already.

namespace Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.Date.J;

/// <summary>Broiler.JS's non-ISO calendars' civil members, as copied (arm J9).</summary>
internal static class TemporalNonIsoCivil
{
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
