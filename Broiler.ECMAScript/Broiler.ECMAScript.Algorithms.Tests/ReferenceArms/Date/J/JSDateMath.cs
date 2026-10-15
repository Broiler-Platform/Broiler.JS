// REFERENCE ARM J: a test-only copy of Broiler.JS's implementation before the cut-over, for the
// reference-arm comparison of bundle sjl-6-001 (docs/evidence/sjl-6-001/reference-arms.md). It is
// never referenced outside this test project, and it is retired at SJL-16.
//
// Copied from Broiler.JS at bc3597f (the commit the area verdict's correspondence reads):
//   Broiler.JS/Broiler.JavaScript.BuiltIns/Date/JSDateMath.cs
//     blob e2c6b12ea188478622498a1288cb3d7d8307ae3f: the whole class JSDateMath (:10-212): MsPerDay (:12),
//     FloorDiv (:15), FloorMod (:18-22), DayFromYear (:25), TimeFromYear (:28), IsLeapYear (:31-38), DaysInYear
//     (:41), Day (:44), TimeWithinDay (:47), YearFromTime (:50-61), CumulativeDays and CumulativeDaysLeap
//     (:63-64), MonthFromTime (:67-80), DateFromTime (:83-91), WeekDay (:94), HourFromTime (:97), MinFromTime
//     (:100), SecFromTime (:103), MsFromTime (:106), MakeDay (:112-142), MakeDate (:145), MakeTime (:148-161),
//     UTC (:168), LocalTime (:175), GetLocalOffsetMs (:181-195) and TimeClip (:200-211)
//   Broiler.JS/Broiler.JavaScript.BuiltIns/Date/JSDateStatic.cs
//     blob 9ffcbb962518ee7e6e1b27fc648c58983e0cbdfd: the year line of JSDate.UTC (:68), with the comment above
//     it in the same body (:62-66), as MakeFullYear
//
// Changes from the source, none of which changes a result:
//   - the sources' line endings are LF already; JSDateStatic.cs's byte-order mark dropped;
//   - the namespace; JSDateMath stays a static class, and MakeFullYear sits in the static class JSDate (the
//     source's Date.UTC is a member of the JSObject partial class JSDate); private members are internal;
//   - the [JSExport...] attribute of Date.UTC is not copied;
//   - GetLocalOffsetMs, LocalTime and UTC take a TimeZoneInfo zone as their last parameter: GetLocalOffsetMs's two
//     GetUtcOffset calls read it where the source reads TimeZoneInfo.Local, and LocalTime and UTC pass it on.
//     The out-of-range fallback's DateTimeOffset.UtcNow, which reads the current instant, is kept as it is;
//   - MakeFullYear is Date.UTC's inline year computation (:68) as a function. It takes the year the source has
//     already converted at :67, `double year = IntegerComponent(yD)`, where IntegerComponent
//     (JSDatePrototype.Setters.cs:16) is IntegerConversions.ToIntegerOrInfinity, and returns `year` as :68
//     leaves it. The source never reaches the line with a NaN or an infinite year: the scan at :53-60 returns
//     NaN first;
//   - IntegerConversions.ToIntegerOrInfinity is the shared library's, as the source's is; the copy calls it at
//     the commit under test (Broiler.ECMAScript.Algorithms/Numbers/IntegerConversions.cs is unchanged since
//     bc3597f).

using System;
using Broiler.ECMAScript.Numbers;

namespace Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.Date.J;

/// <summary>
/// ECMAScript date math helpers per ECMA-262 § 21.4.1.
/// Supports proleptic Gregorian calendar (including year 0 and negative years).
/// </summary>
internal static class JSDateMath
{
    internal const long MsPerDay = 86_400_000L;

    /// <summary>Floor division that rounds toward negative infinity.</summary>
    internal static long FloorDiv(long a, long b) => a >= 0 ? a / b : (a - b + 1) / b;

    /// <summary>Floor modulo that always returns a non-negative result.</summary>
    internal static long FloorMod(long a, long b)
    {
        long r = a % b;
        return r < 0 ? r + b : r;
    }

    /// <summary>Number of days from epoch (Jan 1, 1970) to Jan 1 of the given year.</summary>
    internal static long DayFromYear(long y) => 365L * (y - 1970) + FloorDiv(y - 1969, 4) - FloorDiv(y - 1901, 100) + FloorDiv(y - 1601, 400);

    /// <summary>Millisecond timestamp for the start of the given year.</summary>
    internal static double TimeFromYear(long y) => MsPerDay * (double)DayFromYear(y);

    /// <summary>Whether the given year is a leap year in the proleptic Gregorian calendar.</summary>
    internal static bool IsLeapYear(long y)
    {
        long m4 = FloorMod(y, 4);
        long m100 = FloorMod(y, 100);
        long m400 = FloorMod(y, 400);

        return m4 == 0 && (m100 != 0 || m400 == 0);
    }

    /// <summary>Number of days in the given year.</summary>
    internal static int DaysInYear(long y) => IsLeapYear(y) ? 366 : 365;

    /// <summary>Day number from epoch.</summary>
    internal static long Day(double t) => (long)Math.Floor(t / MsPerDay);

    /// <summary>Milliseconds within the day.</summary>
    internal static long TimeWithinDay(double t) => FloorMod((long)t, MsPerDay);

    /// <summary>Find the year containing the given timestamp.</summary>
    internal static long YearFromTime(double t)
    {
        // Estimate using average year length (365.2425 days)
        long day = Day(t);
        long y = 1970 + (long)(day / 365.2425);

        // Adjust: DayFromYear(y) must be ≤ day, and DayFromYear(y+1) must be > day
        while (DayFromYear(y + 1) <= day) y++;
        while (DayFromYear(y) > day) y--;

        return y;
    }

    internal static readonly int[] CumulativeDays = [0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334];
    internal static readonly int[] CumulativeDaysLeap = [0, 31, 60, 91, 121, 152, 182, 213, 244, 274, 305, 335];

    /// <summary>Month (0-11) from timestamp.</summary>
    internal static int MonthFromTime(double t)
    {
        long y = YearFromTime(t);
        long dayWithinYear = Day(t) - DayFromYear(y);
        var cumDays = IsLeapYear(y) ? CumulativeDaysLeap : CumulativeDays;

        for (int m = 11; m >= 0; m--)
        {
            if (dayWithinYear >= cumDays[m])
                return m;
        }

        return 0;
    }

    /// <summary>Day of month (1-31) from timestamp.</summary>
    internal static int DateFromTime(double t)
    {
        long y = YearFromTime(t);
        long dayWithinYear = Day(t) - DayFromYear(y);
        int m = MonthFromTime(t);
        var cumDays = IsLeapYear(y) ? CumulativeDaysLeap : CumulativeDays;

        return (int)(dayWithinYear - cumDays[m]) + 1;
    }

    /// <summary>Day of week (0=Sunday, 6=Saturday) from timestamp.</summary>
    internal static int WeekDay(double t) => (int)FloorMod(Day(t) + 4, 7);

    /// <summary>Hour (0-23) from timestamp.</summary>
    internal static int HourFromTime(double t) => (int)FloorMod((long)Math.Floor(t / 3_600_000), 24);

    /// <summary>Minute (0-59) from timestamp.</summary>
    internal static int MinFromTime(double t) => (int)FloorMod((long)Math.Floor(t / 60_000), 60);

    /// <summary>Second (0-59) from timestamp.</summary>
    internal static int SecFromTime(double t) => (int)FloorMod((long)Math.Floor(t / 1_000), 60);

    /// <summary>Millisecond (0-999) from timestamp.</summary>
    internal static int MsFromTime(double t) => (int)FloorMod((long)t, 1000);

    /// <summary>
    /// MakeDay per ECMA-262 § 21.4.1.13.
    /// Returns the day number for the given year/month/date combination.
    /// </summary>
    internal static double MakeDay(double year, double month, double date)
    {
        // MakeDay step 1: a non-finite component yields NaN.
        if (!double.IsFinite(year) || !double.IsFinite(month) || !double.IsFinite(date))
            return double.NaN;

        // Steps 2-4: each component is its ToIntegerOrInfinity, the shared library's (SJL-4.5), before
        // the month is split into years: a month of -0.5 is 0, not -1, so it stays in the same year.
        year = IntegerConversions.ToIntegerOrInfinity(year);
        month = IntegerConversions.ToIntegerOrInfinity(month);
        date = IntegerConversions.ToIntegerOrInfinity(date);

        double ymD = year + Math.Floor(month / 12);

        // A component this far out of range can only produce a time value outside the
        // valid Date range (|t| ≤ 8.64e15 ms ≈ ±273790 years), so the spec's
        // MakeDate/TimeClip would map it to NaN anyway. Short-circuit here because the
        // 64-bit integer day math below would otherwise overflow and wrap silently
        // (e.g. Date.UTC(Number.MAX_VALUE, Number.MAX_VALUE)).
        const double SafeComponentLimit = 1e15;
        if (Math.Abs(ymD) > SafeComponentLimit || Math.Abs(date) > SafeComponentLimit)
            return double.NaN;

        long yr = (long)ymD;
        long mn = FloorMod((long)month, 12);

        long dayStart = DayFromYear(yr);
        var cumDays = IsLeapYear(yr) ? CumulativeDaysLeap : CumulativeDays;

        return dayStart + cumDays[(int)mn] + (long)date - 1;
    }

    /// <summary>MakeDate per ECMA-262 § 21.4.1.14.</summary>
    internal static double MakeDate(double day, double time) => day * MsPerDay + time;

    /// <summary>MakeTime per ECMA-262 § 21.4.1.12.</summary>
    internal static double MakeTime(double hour, double min, double sec, double ms)
    {
        // Step 1: a non-finite component yields NaN.
        if (!double.IsFinite(hour) || !double.IsFinite(min) || !double.IsFinite(sec) || !double.IsFinite(ms))
            return double.NaN;

        // Steps 2-6: each component is its ToIntegerOrInfinity, the shared library's (SJL-4.5), and the
        // sum is the double arithmetic the clause specifies, in its order.
        var h = IntegerConversions.ToIntegerOrInfinity(hour);
        var m = IntegerConversions.ToIntegerOrInfinity(min);
        var s = IntegerConversions.ToIntegerOrInfinity(sec);
        var milli = IntegerConversions.ToIntegerOrInfinity(ms);
        return h * 3_600_000 + m * 60_000 + s * 1_000 + milli;
    }

    /// <summary>
    /// UTC adjustment: convert local time to UTC.
    /// Uses the timezone offset for the date represented by <paramref name="t"/>
    /// when within .NET DateTimeOffset range; otherwise falls back to current offset.
    /// </summary>
    internal static double UTC(double t, TimeZoneInfo zone) => t - GetLocalOffsetMs(t, zone);

    /// <summary>
    /// LocalTime adjustment: convert UTC to local time.
    /// Uses the timezone offset for the date represented by <paramref name="t"/>
    /// when within .NET DateTimeOffset range; otherwise falls back to current offset.
    /// </summary>
    internal static double LocalTime(double t, TimeZoneInfo zone) => t + GetLocalOffsetMs(t, zone);

    /// <summary>
    /// Returns the local timezone offset in milliseconds for the given UTC timestamp.
    /// For dates outside .NET's supported range, falls back to the current offset.
    /// </summary>
    internal static double GetLocalOffsetMs(double t, TimeZoneInfo zone)
    {
        long ms = (long)t;
        long minMs = DateTimeOffset.MinValue.ToUnixTimeMilliseconds();
        long maxMs = DateTimeOffset.MaxValue.ToUnixTimeMilliseconds();

        if (ms >= minMs && ms <= maxMs)
        {
            var dto = DateTimeOffset.FromUnixTimeMilliseconds(ms);
            return zone.GetUtcOffset(dto).TotalMilliseconds;
        }

        // Fallback for dates outside .NET range (e.g., year 0 or negative years)
        return zone.GetUtcOffset(DateTimeOffset.UtcNow).TotalMilliseconds;
    }

    /// <summary>
    /// TimeClip per ECMA-262 § 21.4.1.15.
    /// </summary>
    internal static double TimeClip(double time)
    {
        if (double.IsInfinity(time) || double.IsNaN(time))
            return double.NaN;

        if (Math.Abs(time) > 8.64e15)
            return double.NaN;

        // 𝔽(! ToIntegerOrInfinity(time)), the shared library's (SJL-4.5): -0 and a time in (-1, 0)
        // are +0, where a truncation kept -0.
        return IntegerConversions.ToIntegerOrInfinity(time);
    }
}

/// <summary>Broiler.JS's Date members, as copied: here, Date.UTC's year computation.</summary>
internal static partial class JSDate
{
    // Date.UTC's year (JSDateStatic.cs:68); `year` is the source's IntegerComponent(yD) of :67.
    internal static double MakeFullYear(double year)
    {
        // MakeDay/MakeTime take ToIntegerOrInfinity (the shared library's) of each component
        // themselves — NOT ToUint32, whose modulo-2^32 reduction would wrap a huge year such as
        // Number.MAX_VALUE down into range (MAX_VALUE mod 2^32 == 0 → year 1900) instead of yielding
        // NaN. MakeFullYear's two-digit test is on the year's ToIntegerOrInfinity. The components are
        // already finite here (the NaN/Infinity scan above returned otherwise).
        year = year >= 0 && year < 100 ? year + 1900 : year;
        return year;
    }
}
