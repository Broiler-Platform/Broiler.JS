// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   37
// Annotated:        37/37
// Exempt:           0
// Human-reviewed:   0/37
// IP risk:          Low
// Security risk:    Medium
// Criteria:         0/0
// Resource impact:  2/10 max
// Unverified:       37
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Numerics;
using Broiler.ECMAScript.Numbers;

namespace Broiler.ECMAScript.Time;

/// <summary>
/// The time-value arithmetic of ECMA-262 21.4.1: days, years, months and dates of a time value, and
/// <c>MakeTime</c>, <c>MakeDay</c>, <c>MakeDate</c>, <c>MakeFullYear</c> and <c>TimeClip</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every operation is the clause's, in its arithmetic.</b> A step the specification writes in Number
/// arithmetic (<c>t / msPerDay</c>, <c>day × msPerDay + time</c>) is computed in <see cref="double"/>
/// arithmetic, rounded to nearest as the Number operations are; a step written on mathematical values
/// (<c>ℝ(t) modulo msPerDay</c>, <c>floor(ℝ(m) / 12)</c>, DayFromYear's sum) is computed exactly, on
/// 64-bit integers or <see cref="BigInteger"/>, and rounded once, as <c>𝔽</c> rounds.
/// </para>
/// <para>
/// <b>The field operations take a finite Number</b>: a time value, or a local time, which is a time value
/// moved by an offset of less than a day. A NaN or an infinity answers NaN, as every getter does for an
/// invalid date; a finite argument of a magnitude above 2^62, which no Date can hold, is a broken
/// precondition. The conversions of the arguments of <c>Date.UTC</c>, the constructor and the setters,
/// and their order, are the caller's (contract convention 2); <c>MakeTime</c> and <c>MakeDay</c> apply
/// <c>ToIntegerOrInfinity</c> themselves, as their clauses do.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.1; IP=Low; Security=Medium; Resources=1; Fingerprint=BBDF3D
// Broiler-Human:        PENDING
public static class EcmaTime
{
    /// <summary><c>msPerSecond</c> (ECMA-262 21.4.1.14): 1000.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.1.14; IP=Low; Security=Low; Resources=1; Fingerprint=EE88F5
    // Broiler-Human:        PENDING
    public const double MsPerSecond = 1000;

    /// <summary><c>msPerMinute</c> (ECMA-262 21.4.1.14): 60,000.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.1.14; IP=Low; Security=Low; Resources=1; Fingerprint=329818
    // Broiler-Human:        PENDING
    public const double MsPerMinute = 60000;

    /// <summary><c>msPerHour</c> (ECMA-262 21.4.1.14): 3,600,000.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.1.14; IP=Low; Security=Low; Resources=1; Fingerprint=FE6072
    // Broiler-Human:        PENDING
    public const double MsPerHour = 3600000;

    /// <summary><c>msPerDay</c> (ECMA-262 21.4.1.3): 86,400,000.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.1.3; IP=Low; Security=Low; Resources=1; Fingerprint=A9D8F0
    // Broiler-Human:        PENDING
    public const double MsPerDay = 86400000;

    /// <summary>The largest magnitude of a time value (ECMA-262 21.4.1.1 and 21.4.1.31): 8.64 × 10^15 ms.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.1.31; IP=Low; Security=Low; Resources=1; Fingerprint=5A39FC
    // Broiler-Human:        PENDING
    public const double MaxTimeValue = 8.64e15;

    // The largest magnitude a field operation reads: far past any time value or local time.
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.1.1; IP=Low; Security=Low; Resources=1; Fingerprint=C8E120
    // Broiler-Human:        PENDING
    private const double FieldLimit = 4611686018427387904.0; // 2^62

    // The magnitude below which an integral double fits a long and the day arithmetic stays exact in one.
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.1.6; IP=Low; Security=Low; Resources=1; Fingerprint=1662E5
    // Broiler-Human:        PENDING
    private const double TwoTo53 = 9007199254740992.0;

    /// <summary><c>Day</c> (ECMA-262 21.4.1.3): <c>𝔽(floor(ℝ(t / msPerDay)))</c>.</summary>
    /// <param name="t">A finite time value or local time.</param>
    /// <returns>The day number; NaN for a NaN or an infinite <paramref name="t"/>.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.1.3; IP=Low; Security=Medium; Resources=1; Fingerprint=633F91
    // Broiler-Human:        PENDING
    public static double Day(double t) => Field(t) ? Math.Floor(t / MsPerDay) : double.NaN;

    /// <summary><c>TimeWithinDay</c> (ECMA-262 21.4.1.4): <c>𝔽(ℝ(t) modulo ℝ(msPerDay))</c>.</summary>
    /// <param name="t">A finite time value or local time.</param>
    /// <returns>The milliseconds since the day's start, from +0 to 86,399,999; NaN for a NaN or an infinite <paramref name="t"/>.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.1.4; IP=Low; Security=Medium; Resources=1; Fingerprint=26073E
    // Broiler-Human:        PENDING
    public static double TimeWithinDay(double t) => Field(t) ? Modulo(t, MsPerDay) : double.NaN;

    /// <summary><c>DaysInYear</c> (ECMA-262 21.4.1.5): 366 for a leap year of the proleptic Gregorian calendar, 365 otherwise.</summary>
    /// <param name="y">An integral Number, the year.</param>
    /// <returns>366 or 365; NaN for a NaN or an infinite <paramref name="y"/>.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.1.5; IP=Low; Security=Medium; Resources=1; Fingerprint=EB68C5
    // Broiler-Human:        PENDING
    public static double DaysInYear(double y)
    {
        if (!double.IsFinite(y))
            return double.NaN;

        // The remainder of a double is exact, so these are the clause's modulos of ℝ(y).
        if (y % 400 == 0)
            return 366;
        if (y % 100 == 0)
            return 365;
        return y % 4 == 0 ? 366 : 365;
    }

    /// <summary>
    /// <c>DayFromYear</c> (ECMA-262 21.4.1.6): <c>𝔽(365 × (ℝ(y) - 1970) + floor((ℝ(y) - 1969) / 4) -
    /// floor((ℝ(y) - 1901) / 100) + floor((ℝ(y) - 1601) / 400))</c>, computed exactly.
    /// </summary>
    /// <param name="y">An integral Number, the year.</param>
    /// <returns>The day number of the year's first day; NaN for an argument that is not an integral Number.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.1.6; IP=Low; Security=Medium; Resources=1; Fingerprint=E7E315
    // Broiler-Human:        PENDING
    public static double DayFromYear(double y)
    {
        if (!double.IsFinite(y) || Math.Floor(y) != y)
            return double.NaN;

        if (Math.Abs(y) < TwoTo53)
            return DayFromYear((long)y);

        return BigIntMath.ToNumber(DayFromYear(new BigInteger(y)), null);
    }

    /// <summary><c>TimeFromYear</c> (ECMA-262 21.4.1.7): <c>msPerDay × DayFromYear(y)</c>, a Number multiplication.</summary>
    /// <param name="y">An integral Number, the year.</param>
    /// <returns>The time value of the year's first instant; NaN for an argument that is not an integral Number.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.1.7; IP=Low; Security=Medium; Resources=1; Fingerprint=162345
    // Broiler-Human:        PENDING
    public static double TimeFromYear(double y) => MsPerDay * DayFromYear(y);

    /// <summary><c>YearFromTime</c> (ECMA-262 21.4.1.8): the largest integral Number <c>y</c> with <c>TimeFromYear(y) ≤ t</c>.</summary>
    /// <param name="t">A finite time value or local time.</param>
    /// <returns>The year; NaN for a NaN or an infinite <paramref name="t"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The magnitude of <paramref name="t"/> is above 2^62.</exception>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.1.8; IP=Low; Security=Medium; Resources=1; Fingerprint=E34F0F
    // Broiler-Human:        PENDING
    public static double YearFromTime(double t)
    {
        if (!Field(t))
            return double.NaN;

        // The mean Gregorian year puts the estimate within a year or two of the answer; the two walks
        // settle it on the clause's own test.
        var year = Math.Floor(t / (MsPerDay * 365.2425)) + 1970;
        while (TimeFromYear(year) > t)
            year--;
        while (TimeFromYear(year + 1) <= t)
            year++;

        return year;
    }

    /// <summary><c>DayWithinYear</c> (ECMA-262 21.4.1.9): <c>Day(t) - DayFromYear(YearFromTime(t))</c>.</summary>
    /// <param name="t">A finite time value or local time.</param>
    /// <returns>The zero-based day of the year, from +0 to 365; NaN for a NaN or an infinite <paramref name="t"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The magnitude of <paramref name="t"/> is above 2^62.</exception>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.1.9; IP=Low; Security=Medium; Resources=1; Fingerprint=75C562
    // Broiler-Human:        PENDING
    public static double DayWithinYear(double t) =>
        Field(t) ? Day(t) - DayFromYear(YearFromTime(t)) : double.NaN;

    /// <summary><c>InLeapYear</c> (ECMA-262 21.4.1.10): 1 when <c>YearFromTime(t)</c> is a leap year, +0 otherwise.</summary>
    /// <param name="t">A finite time value or local time.</param>
    /// <returns>1 or +0; NaN for a NaN or an infinite <paramref name="t"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The magnitude of <paramref name="t"/> is above 2^62.</exception>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.1.10; IP=Low; Security=Medium; Resources=1; Fingerprint=32D8B8
    // Broiler-Human:        PENDING
    public static double InLeapYear(double t)
    {
        if (!Field(t))
            return double.NaN;

        return DaysInYear(YearFromTime(t)) == 366 ? 1 : 0;
    }

    /// <summary><c>MonthFromTime</c> (ECMA-262 21.4.1.11): the zero-based month, January being +0.</summary>
    /// <param name="t">A finite time value or local time.</param>
    /// <returns>The month, from +0 to 11; NaN for a NaN or an infinite <paramref name="t"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The magnitude of <paramref name="t"/> is above 2^62.</exception>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.1.11; IP=Low; Security=Medium; Resources=1; Fingerprint=6E3A8C
    // Broiler-Human:        PENDING
    public static double MonthFromTime(double t)
    {
        if (!Field(t))
            return double.NaN;

        var inLeapYear = InLeapYear(t);
        var dayWithinYear = DayWithinYear(t);
        if (dayWithinYear < 31)
            return 0;

        for (var month = 1; month < 12; month++)
        {
            if (dayWithinYear < MonthStarts[month + 1] + inLeapYear)
                return month;
        }

        return 11;
    }

    /// <summary><c>DateFromTime</c> (ECMA-262 21.4.1.12): the one-based day of the month.</summary>
    /// <param name="t">A finite time value or local time.</param>
    /// <returns>The day of the month, from 1 to 31; NaN for a NaN or an infinite <paramref name="t"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The magnitude of <paramref name="t"/> is above 2^62.</exception>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.1.12; IP=Low; Security=Medium; Resources=1; Fingerprint=318618
    // Broiler-Human:        PENDING
    public static double DateFromTime(double t)
    {
        if (!Field(t))
            return double.NaN;

        var inLeapYear = InLeapYear(t);
        var dayWithinYear = DayWithinYear(t);
        var month = (int)MonthFromTime(t);

        // The clause's table: day + 1 in January, day - 30 in February, and from March on the month's
        // start, which a leap year moves by one.
        return month switch
        {
            0 => dayWithinYear + 1,
            1 => dayWithinYear - 30,
            _ => dayWithinYear - (MonthStarts[month] - 1) - inLeapYear,
        };
    }

    /// <summary><c>WeekDay</c> (ECMA-262 21.4.1.13): <c>𝔽(ℝ(Day(t) + 4𝔽) modulo 7)</c>, Sunday being +0.</summary>
    /// <param name="t">A finite time value or local time.</param>
    /// <returns>The weekday, from +0 (Sunday) to 6 (Saturday); NaN for a NaN or an infinite <paramref name="t"/>.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.1.13; IP=Low; Security=Medium; Resources=1; Fingerprint=1C9092
    // Broiler-Human:        PENDING
    public static double WeekDay(double t) => Field(t) ? Modulo(Day(t) + 4, 7) : double.NaN;

    /// <summary><c>HourFromTime</c> (ECMA-262 21.4.1.14): <c>𝔽(floor(ℝ(t / msPerHour)) modulo HoursPerDay)</c>.</summary>
    /// <param name="t">A finite time value or local time.</param>
    /// <returns>The hour, from +0 to 23; NaN for a NaN or an infinite <paramref name="t"/>.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.1.14; IP=Low; Security=Medium; Resources=1; Fingerprint=662EAA
    // Broiler-Human:        PENDING
    public static double HourFromTime(double t) => Field(t) ? Modulo(Math.Floor(t / MsPerHour), 24) : double.NaN;

    /// <summary><c>MinFromTime</c> (ECMA-262 21.4.1.15): <c>𝔽(floor(ℝ(t / msPerMinute)) modulo MinutesPerHour)</c>.</summary>
    /// <param name="t">A finite time value or local time.</param>
    /// <returns>The minute, from +0 to 59; NaN for a NaN or an infinite <paramref name="t"/>.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.1.15; IP=Low; Security=Medium; Resources=1; Fingerprint=37550E
    // Broiler-Human:        PENDING
    public static double MinFromTime(double t) => Field(t) ? Modulo(Math.Floor(t / MsPerMinute), 60) : double.NaN;

    /// <summary><c>SecFromTime</c> (ECMA-262 21.4.1.16): <c>𝔽(floor(ℝ(t / msPerSecond)) modulo SecondsPerMinute)</c>.</summary>
    /// <param name="t">A finite time value or local time.</param>
    /// <returns>The second, from +0 to 59; NaN for a NaN or an infinite <paramref name="t"/>.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.1.16; IP=Low; Security=Medium; Resources=1; Fingerprint=E5964A
    // Broiler-Human:        PENDING
    public static double SecFromTime(double t) => Field(t) ? Modulo(Math.Floor(t / MsPerSecond), 60) : double.NaN;

    /// <summary><c>msFromTime</c> (ECMA-262 21.4.1.17): <c>𝔽(ℝ(t) modulo ℝ(msPerSecond))</c>.</summary>
    /// <param name="t">A finite time value or local time.</param>
    /// <returns>The millisecond, from +0 to 999; NaN for a NaN or an infinite <paramref name="t"/>.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.1.17; IP=Low; Security=Medium; Resources=1; Fingerprint=58E435
    // Broiler-Human:        PENDING
    public static double MsFromTime(double t) => Field(t) ? Modulo(t, MsPerSecond) : double.NaN;

    /// <summary>
    /// <c>MakeTime</c> (ECMA-262 21.4.1.27): <c>((h × msPerHour + m × msPerMinute) + s × msPerSecond) + milli</c>
    /// in Number arithmetic, each component first made integral by <c>ToIntegerOrInfinity</c>.
    /// </summary>
    /// <param name="hour">The hour, a Number.</param>
    /// <param name="minute">The minute, a Number.</param>
    /// <param name="second">The second, a Number.</param>
    /// <param name="millisecond">The millisecond, a Number.</param>
    /// <returns>The milliseconds; NaN when any component is not finite.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.1.27; IP=Low; Security=Medium; Resources=1; Fingerprint=2F775C
    // Broiler-Human:        PENDING
    public static double MakeTime(double hour, double minute, double second, double millisecond)
    {
        if (!double.IsFinite(hour) || !double.IsFinite(minute) || !double.IsFinite(second) || !double.IsFinite(millisecond))
            return double.NaN;

        var h = IntegerConversions.ToIntegerOrInfinity(hour);
        var m = IntegerConversions.ToIntegerOrInfinity(minute);
        var s = IntegerConversions.ToIntegerOrInfinity(second);
        var milli = IntegerConversions.ToIntegerOrInfinity(millisecond);
        var hours = h * MsPerHour;
        var minutes = m * MsPerMinute;
        var seconds = s * MsPerSecond;
        return ((hours + minutes) + seconds) + milli;
    }

    /// <summary><c>MakeDay</c> (ECMA-262 21.4.1.28): the day number of a year, a month and a date.</summary>
    /// <param name="year">The year, a Number.</param>
    /// <param name="month">The zero-based month, a Number, which may carry into the year.</param>
    /// <param name="date">The one-based date, a Number, which may carry across months.</param>
    /// <returns>
    /// <c>Day(t) + dt - 1</c>, for the first day <c>t</c> of the month <c>ℝ(m) modulo 12</c> of the year
    /// <c>y + 𝔽(floor(ℝ(m) / 12))</c>, both taken exactly; NaN when an argument is not finite, the year is
    /// not finite, or no finite time value lies in that month.
    /// </returns>
    /// <remarks>
    /// The month's modulo and its carry are exact for every Number, which correspondence row T4 of bundle
    /// <c>sjl-6-001</c> shows neither engine took: <c>MakeDay(-833333333333333333, 1e19, 1)</c> is May 1 of
    /// year 0. The day count is exact, and <c>Day(t)</c> is its Number: for a count beyond 2^36 days, where
    /// no Number lies on every day, no Date can hold the result and every caller's <c>TimeClip</c> refuses it.
    /// </remarks>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.1.28; IP=Low; Security=Medium; Resources=2; Fingerprint=9B0C84
    // Broiler-Human:        PENDING
    public static double MakeDay(double year, double month, double date)
    {
        if (!double.IsFinite(year) || !double.IsFinite(month) || !double.IsFinite(date))
            return double.NaN;

        var y = IntegerConversions.ToIntegerOrInfinity(year);
        var m = IntegerConversions.ToIntegerOrInfinity(month);
        var dt = IntegerConversions.ToIntegerOrInfinity(date);
        var ym = y + FloorDivide(m, 12);
        if (!double.IsFinite(ym))
            return double.NaN;

        var mn = (int)Modulo(m, 12);
        var leap = mn >= 2 && DaysInYear(ym) == 366 ? 1 : 0;
        double days;
        if (Math.Abs(ym) < TwoTo53)
        {
            // |DayFromYear| stays below 3.3 × 10^18 here, so the sum is exact in a long, and its
            // product with msPerDay is finite.
            days = DayFromYear((long)ym) + MonthStarts[mn] + leap;
        }
        else
        {
            var exact = DayFromYear(new BigInteger(ym)) + MonthStarts[mn] + leap;
            if (!double.IsFinite(BigIntMath.ToNumber(exact * 86400000, null)))
                return double.NaN;

            days = BigIntMath.ToNumber(exact, null);
        }

        return (days + dt) - 1;
    }

    /// <summary><c>MakeDate</c> (ECMA-262 21.4.1.29): <c>day × msPerDay + time</c> in Number arithmetic.</summary>
    /// <param name="day">The day number, a Number.</param>
    /// <param name="time">The milliseconds within the day, a Number.</param>
    /// <returns>The milliseconds; NaN when either argument or the result is not finite.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.1.29; IP=Low; Security=Medium; Resources=1; Fingerprint=D38B8D
    // Broiler-Human:        PENDING
    public static double MakeDate(double day, double time)
    {
        if (!double.IsFinite(day) || !double.IsFinite(time))
            return double.NaN;

        var days = day * MsPerDay;
        var tv = days + time;
        return double.IsFinite(tv) ? tv : double.NaN;
    }

    /// <summary>
    /// <c>MakeFullYear</c> (ECMA-262 21.4.1.30): the year of <c>Date.UTC</c>, the constructor and
    /// <c>setYear</c>, which reads 0 to 99 as 1900 to 1999.
    /// </summary>
    /// <param name="year">The year, a Number.</param>
    /// <returns>NaN for NaN; <c>1900 + truncated</c> for a truncation from 0 to 99; the truncation, an infinity included, otherwise.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.1.30; IP=Low; Security=Medium; Resources=1; Fingerprint=0DC4D5
    // Broiler-Human:        PENDING
    public static double MakeFullYear(double year)
    {
        if (double.IsNaN(year))
            return double.NaN;

        var truncated = IntegerConversions.ToIntegerOrInfinity(year);
        return truncated is >= 0 and <= 99 ? 1900 + truncated : truncated;
    }

    /// <summary>
    /// <c>TimeClip</c> (ECMA-262 21.4.1.31): a time value, or NaN outside ±8.64 × 10^15 ms, truncated, with
    /// -0 folded to +0.
    /// </summary>
    /// <param name="time">The milliseconds, a Number.</param>
    /// <returns>The time value; NaN for a NaN, an infinity or a magnitude above <see cref="MaxTimeValue"/>.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.1.31; IP=Low; Security=Medium; Resources=1; Fingerprint=574C1A
    // Broiler-Human:        PENDING
    public static double TimeClip(double time)
    {
        if (!double.IsFinite(time) || Math.Abs(time) > MaxTimeValue)
            return double.NaN;

        return IntegerConversions.ToIntegerOrInfinity(time);
    }

    // The day of a non-leap year each month starts on, January to December, with the year's end last.
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.1.11; IP=Low; Security=Low; Resources=1; Fingerprint=AB33C6
    // Broiler-Human:        PENDING
    private static ReadOnlySpan<int> MonthStarts => [0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334, 365];

    // Whether a field operation reads t: finite, and within the magnitude its walks are written for.
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.1.1; IP=Low; Security=Low; Resources=1; Fingerprint=83C22D
    // Broiler-Human:        PENDING
    private static bool Field(double t)
    {
        if (!double.IsFinite(t))
            return false;

        ArgumentOutOfRangeException.ThrowIfGreaterThan(Math.Abs(t), FieldLimit, nameof(t));
        return true;
    }

    // 𝔽(ℝ(x) modulo ℝ(m)) for a positive m: a double's remainder is exact, and a negative one moves up by m,
    // which rounds as 𝔽 does; the result is never -0.
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.1.28; IP=Low; Security=Low; Resources=1; Fingerprint=336F52
    // Broiler-Human:        PENDING
    internal static double Modulo(double x, double m)
    {
        var remainder = x % m;
        if (remainder < 0)
            remainder += m;
        return remainder == 0 ? 0 : remainder;
    }

    // 𝔽(floor(ℝ(m) / d)) for an integral m and a positive integral d, exactly.
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.1.28; IP=Low; Security=Low; Resources=1; Fingerprint=1458D1
    // Broiler-Human:        PENDING
    private static double FloorDivide(double m, long d)
    {
        if (Math.Abs(m) < TwoTo53)
            return FloorDivide((long)m, d);

        return BigIntMath.ToNumber(BigInteger.Divide(new BigInteger(m) - Modulo(new BigInteger(m), d), d), null);
    }

    // DayFromYear on a mathematical year below 2^53 in magnitude, exactly.
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.1.6; IP=Low; Security=Low; Resources=1; Fingerprint=E6CA67
    // Broiler-Human:        PENDING
    internal static long DayFromYear(long y) =>
        (365 * (y - 1970)) + FloorDivide(y - 1969, 4) - FloorDivide(y - 1901, 100) + FloorDivide(y - 1601, 400);

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.1.6; IP=Low; Security=Low; Resources=1; Fingerprint=DB42F1
    // Broiler-Human:        PENDING
    private static BigInteger DayFromYear(BigInteger y) =>
        (365 * (y - 1970)) + FloorDivide(y - 1969, 4) - FloorDivide(y - 1901, 100) + FloorDivide(y - 1601, 400);

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.1.6; IP=Low; Security=Low; Resources=1; Fingerprint=17ACC0
    // Broiler-Human:        PENDING
    internal static long FloorDivide(long n, long d)
    {
        var quotient = n / d;
        return (n % d != 0 && (n < 0) != (d < 0)) ? quotient - 1 : quotient;
    }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.1.6; IP=Low; Security=Low; Resources=1; Fingerprint=EB64EB
    // Broiler-Human:        PENDING
    private static BigInteger FloorDivide(BigInteger n, long d) => BigInteger.Divide(n - Modulo(n, d), d);

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.1.6; IP=Low; Security=Low; Resources=1; Fingerprint=6A4CD7
    // Broiler-Human:        PENDING
    private static BigInteger Modulo(BigInteger n, long d)
    {
        var remainder = BigInteger.Remainder(n, d);
        return remainder.Sign < 0 ? remainder + d : remainder;
    }
}
