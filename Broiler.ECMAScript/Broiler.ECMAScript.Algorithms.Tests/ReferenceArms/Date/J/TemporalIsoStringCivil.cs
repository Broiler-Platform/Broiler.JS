// REFERENCE ARM J7: a test-only copy of Broiler.JS's implementation before the cut-over, for the
// reference-arm comparison of bundle sjl-6-001 (docs/evidence/sjl-6-001/reference-arms.md). It is
// never referenced outside this test project, and it is retired at SJL-16.
//
// Copied from Broiler.JS at bc3597f (the commit the area verdict's correspondence reads):
//   Broiler.JS/Broiler.JavaScript.BuiltIns/Temporal/TemporalIsoString.cs
//     blob 7ed5bc9fb7ea6461f7c808556bc9a9afe03f5d0d: TemporalIsoString's IsLeapYear (:322) and DaysInMonth
//     (:324-330)
//
// Changes from the source, none of which changes a result:
//   - the source's line endings are LF already, and it has no byte-order mark;
//   - the namespace; the members sit in the static class TemporalIsoStringCivil (TemporalIsoString is a static
//     class in the source); private members are internal.

namespace Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.Date.J;

/// <summary>Broiler.JS's Temporal ISO string parser's civil members, as copied (arm J7).</summary>
internal static class TemporalIsoStringCivil
{
    internal static bool IsLeapYear(int y) => (y % 4 == 0 && y % 100 != 0) || y % 400 == 0;

    internal static int DaysInMonth(int year, int month) => month switch
    {
        1 or 3 or 5 or 7 or 8 or 10 or 12 => 31,
        4 or 6 or 9 or 11 => 30,
        2 => IsLeapYear(year) ? 29 : 28,
        _ => 0,
    };
}
