// REFERENCE ARM V: a test-only copy of Broiler.VM's implementation before the cut-over, for the
// reference-arm comparison of bundle sjl-6-001 (docs/evidence/sjl-6-001/reference-arms.md). It is
// never referenced outside this test project, and it is retired at SJL-16.
//
// Copied from Broiler.VM at 9c3d809 (the commit the area verdict's correspondence reads):
//   src/Broiler.VM.Profile.JavaScript/JsTimeZones.cs
//     blob 2b2ebcaa1892164fc52e70b838c03be789ca36a8: of the class JsZone, DaysFromCivil (:626-634), MonthLength
//     (:638-641) and Weekday (:646), with each member its documentation comment
//
// Changes from the source, none of which changes a result or a charge:
//   - the namespace; the members sit in a static class JsZone holding only them, instead of the sealed class
//     JsZone, whose other members (a zone's offsets, its rules and its transitions) are the engine's; the copied
//     members call no other member;
//   - MonthLength and Weekday, private in the source, are internal;
//   - the generated assurance header, the per-member assurance annotations (Broiler-AI / Broiler-Human lines) and
//     the SPDX lines removed.

namespace Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.Date.V;

/// <summary>Broiler.VM's civil members of JsZone, as copied.</summary>
internal static class JsZone
{
    /// <summary>Days since 1970-01-01 of a proleptic Gregorian date.</summary>
    internal static long DaysFromCivil(long year, int month, int day)
    {
        year -= month <= 2 ? 1 : 0;
        var era = (year >= 0 ? year : year - 399) / 400;
        var yearOfEra = year - (era * 400);
        var dayOfYear = ((153 * (month + (month > 2 ? -3 : 9))) + 2) / 5 + day - 1;
        var dayOfEra = (yearOfEra * 365) + (yearOfEra / 4) - (yearOfEra / 100) + dayOfYear;
        return (era * 146097) + dayOfEra - 719468;
    }

    internal static int MonthLength(int year, int month) =>
        month == 2
            ? (year % 4 == 0 && (year % 100 != 0 || year % 400 == 0) ? 29 : 28)
            : month is 4 or 6 or 9 or 11 ? 30 : 31;

    /// <summary>Monday 0 to Sunday 6; the epoch was a Thursday.</summary>
    internal static int Weekday(long day) => (int)(((day + 3) % 7 + 7) % 7);
}
