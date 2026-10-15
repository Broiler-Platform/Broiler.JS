// REFERENCE ARM J4: a test-only copy of Broiler.JS's implementation before the cut-over, for the
// reference-arm comparison of bundle sjl-6-001 (docs/evidence/sjl-6-001/reference-arms.md). It is
// never referenced outside this test project, and it is retired at SJL-16.
//
// Copied from Broiler.JS at bc3597f (the commit the area verdict's correspondence reads):
//   Broiler.JS/Broiler.JavaScript.BuiltIns/Temporal/JSTemporalPlainMonthDay.cs
//     blob 5c34ab2a89af2936b2bfb1c85bb15a62aae26588: JSTemporalPlainMonthDay's IsLeapYear (:568), DaysInMonthOf
//     (:570-576) and IsValidISODate (:578-579)
//
// Changes from the source, none of which changes a result:
//   - the source's line endings are LF already, and it has no byte-order mark;
//   - the namespace; the members sit in the static class JSTemporalPlainMonthDayCivil (JSTemporalPlainMonthDay is
//     a JSObject partial class in the source); private members are internal.

namespace Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.Date.J;

/// <summary>Broiler.JS's Temporal.PlainMonthDay civil members, as copied (arm J4).</summary>
internal static class JSTemporalPlainMonthDayCivil
{
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
}
