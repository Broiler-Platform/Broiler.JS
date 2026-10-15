// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   16
// Annotated:        16/16
// Exempt:           0
// Human-reviewed:   0/16
// IP risk:          Low
// Security risk:    Medium
// Criteria:         0/0
// Resource impact:  1/10 max
// Unverified:       16
//
// GENERATED - DO NOT EDIT MANUALLY

using System;

namespace Broiler.ECMAScript.Time;

/// <summary>
/// The ISO 8601 calendar's civil-day arithmetic of the Temporal proposal, over 64-bit integers: leap
/// years, month and year lengths, epoch days in both directions, the weekday, the day of the year, the
/// ISO week, <c>BalanceISOYearMonth</c> and <c>IsValidISODate</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b>The calendar is ECMA-262's</b>: the proleptic Gregorian calendar of <c>DaysInYear</c> (21.4.1.5),
/// with no year zero skipped, so that the epoch days of a date are the day number <c>MakeDay</c> gives
/// it (21.4.1.28). The Temporal proposal's ISO 8601 calendar operations are written on that calendar, and
/// ECMA-262 at the corpus's pinned edition does not yet hold them; each member names the operation it is.
/// </para>
/// <para>
/// <b>Arguments are mathematical integers</b>, already converted and checked by the caller (contract
/// convention 2). A year is at most 2^53 in magnitude and an epoch day count at most 2^60, far past any
/// limit Temporal sets; a month outside 1 to 12, where an operation needs a valid one, is a broken
/// precondition. Floor division is used throughout, so that a negative year has the same weeks and
/// weekdays as the year 400 after it.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=Temporal ISO 8601 calendar; IP=Low; Security=Medium; Resources=1; Fingerprint=3DCDAC
// Broiler-Human:        PENDING
public static class IsoCalendarMath
{
    // Broiler-AI:           Origin=AI; Spec=Temporal ISO 8601 calendar; IP=Low; Security=Low; Resources=1; Fingerprint=5C4590
    // Broiler-Human:        PENDING
    private const long YearLimit = 9007199254740992L; // 2^53

    // Broiler-AI:           Origin=AI; Spec=Temporal ISO 8601 calendar; IP=Low; Security=Low; Resources=1; Fingerprint=D8BC08
    // Broiler-Human:        PENDING
    private const long DayLimit = 1152921504606846976L; // 2^60

    /// <summary>
    /// Whether a year is a leap year: <c>MathematicalInLeapYear</c> of the Temporal proposal, the year that
    /// ECMA-262's <c>DaysInYear</c> (21.4.1.5) gives 366 days.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <returns>Whether it is divisible by 4, and by 400 when it is divisible by 100.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.1.5; IP=Low; Security=Low; Resources=1; Fingerprint=AEDB68
    // Broiler-Human:        PENDING
    public static bool IsLeapYear(long year) => year % 4 == 0 && (year % 100 != 0 || year % 400 == 0);

    /// <summary>The days in a year: <c>MathematicalDaysInYear</c> of the Temporal proposal.</summary>
    /// <param name="year">The year.</param>
    /// <returns>366 or 365.</returns>
    // Broiler-AI:           Origin=AI; Spec=Temporal MathematicalDaysInYear; IP=Low; Security=Low; Resources=1; Fingerprint=110D79
    // Broiler-Human:        PENDING
    public static int DaysInYear(long year) => IsLeapYear(year) ? 366 : 365;

    /// <summary>The days in a month: <c>ISODaysInMonth</c> of the Temporal proposal.</summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month, from 1 to 12.</param>
    /// <returns>From 28 to 31.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The month is outside 1 to 12.</exception>
    // Broiler-AI:           Origin=AI; Spec=Temporal ISODaysInMonth; IP=Low; Security=Low; Resources=1; Fingerprint=50127B
    // Broiler-Human:        PENDING
    public static int DaysInMonth(long year, int month) => CheckMonth(month) switch
    {
        2 => IsLeapYear(year) ? 29 : 28,
        4 or 6 or 9 or 11 => 30,
        _ => 31,
    };

    /// <summary>
    /// The days from 1970-01-01 to a date: <c>ISODateToEpochDays</c> of the Temporal proposal with a
    /// one-based month, which is ECMA-262's <c>MakeDay(year, month - 1, day)</c> (21.4.1.28) on integers.
    /// </summary>
    /// <param name="year">The year, at most 2^53 in magnitude.</param>
    /// <param name="month">The month, from 1 to 12.</param>
    /// <param name="day">The day; one outside the month carries into the months around it.</param>
    /// <returns>The epoch days, negative before 1970.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The year's magnitude is above 2^53, or the month is outside 1 to 12.</exception>
    // Broiler-AI:           Origin=AI; Spec=Temporal ISODateToEpochDays; IP=Low; Security=Medium; Resources=1; Fingerprint=DA1680
    // Broiler-Human:        PENDING
    public static long EpochDays(long year, int month, int day)
    {
        CheckYear(year);
        CheckMonth(month);

        // Howard Hinnant's days_from_civil (howardhinnant.github.io/date_algorithms.html, donated to the
        // public domain by its author): March is the year's first month, so that a leap day is the last
        // day of its year, and 400-year eras keep the arithmetic free of tables.
        var y = year - (month <= 2 ? 1 : 0);
        var era = EcmaTime.FloorDivide(y, 400);
        var yearOfEra = y - (era * 400);
        var dayOfYear = (((153 * (month + (month > 2 ? -3 : 9))) + 2) / 5) + (long)day - 1;
        var dayOfEra = (yearOfEra * 365) + (yearOfEra / 4) - (yearOfEra / 100) + dayOfYear;
        return (era * 146097) + dayOfEra - 719468;
    }

    /// <summary>The date a number of days from 1970-01-01: <c>EpochDaysToISODate</c>, the inverse of <see cref="EpochDays"/>.</summary>
    /// <param name="days">The epoch days, at most 2^60 in magnitude.</param>
    /// <returns>The year, the month from 1 to 12 and the day from 1 to 31.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The magnitude of <paramref name="days"/> is above 2^60.</exception>
    // Broiler-AI:           Origin=AI; Spec=Temporal EpochDaysToISODate; IP=Low; Security=Medium; Resources=1; Fingerprint=D65FB8
    // Broiler-Human:        PENDING
    public static (long Year, int Month, int Day) FromEpochDays(long days)
    {
        CheckMagnitude(days, DayLimit, nameof(days));

        // Howard Hinnant's civil_from_days, the inverse of days_from_civil (see EpochDays).
        var z = days + 719468;
        var era = EcmaTime.FloorDivide(z, 146097);
        var dayOfEra = z - (era * 146097);
        var yearOfEra = (dayOfEra - (dayOfEra / 1460) + (dayOfEra / 36524) - (dayOfEra / 146096)) / 365;
        var dayOfYear = dayOfEra - ((365 * yearOfEra) + (yearOfEra / 4) - (yearOfEra / 100));
        var monthPart = ((5 * dayOfYear) + 2) / 153;
        var day = (int)(dayOfYear - (((153 * monthPart) + 2) / 5) + 1);
        var month = (int)(monthPart < 10 ? monthPart + 3 : monthPart - 9);
        return (yearOfEra + (era * 400) + (month <= 2 ? 1 : 0), month, day);
    }

    /// <summary>The weekday of a date: <c>ISODayOfWeek</c> of the Temporal proposal, Monday 1 to Sunday 7.</summary>
    /// <param name="year">The year, at most 2^53 in magnitude.</param>
    /// <param name="month">The month, from 1 to 12.</param>
    /// <param name="day">The day.</param>
    /// <returns>From 1 (Monday) to 7 (Sunday).</returns>
    /// <exception cref="ArgumentOutOfRangeException">The year's magnitude is above 2^53, or the month is outside 1 to 12.</exception>
    // Broiler-AI:           Origin=AI; Spec=Temporal ISODayOfWeek; IP=Low; Security=Low; Resources=1; Fingerprint=0597D2
    // Broiler-Human:        PENDING
    public static int DayOfWeek(long year, int month, int day)
    {
        // ECMA-262's WeekDay (21.4.1.13) counts Sunday as 0 from the epoch's Thursday; Temporal counts
        // Monday as 1 and Sunday as 7.
        var shifted = EpochDays(year, month, day) + 4;
        var weekDay = (int)(shifted - (7 * EcmaTime.FloorDivide(shifted, 7)));
        return weekDay == 0 ? 7 : weekDay;
    }

    /// <summary>The day of the year of a date: <c>ISODayOfYear</c> of the Temporal proposal.</summary>
    /// <param name="year">The year, at most 2^53 in magnitude.</param>
    /// <param name="month">The month, from 1 to 12.</param>
    /// <param name="day">The day, from 1 to the month's length.</param>
    /// <returns>From 1 to 366.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The year's magnitude is above 2^53, or the month is outside 1 to 12.</exception>
    // Broiler-AI:           Origin=AI; Spec=Temporal ISODayOfYear; IP=Low; Security=Low; Resources=1; Fingerprint=92131E
    // Broiler-Human:        PENDING
    public static int DayOfYear(long year, int month, int day) =>
        (int)(EpochDays(year, month, day) - EpochDays(year, 1, 1)) + 1;

    /// <summary>
    /// The ISO week of a date and the year it belongs to: <c>ISOWeekOfYear</c> of the Temporal proposal,
    /// whose week 1 is the week holding the year's first Thursday.
    /// </summary>
    /// <param name="year">The year, at most 2^53 - 1 in magnitude.</param>
    /// <param name="month">The month, from 1 to 12.</param>
    /// <param name="day">The day, from 1 to the month's length.</param>
    /// <returns>The week, from 1 to 53, and its year, which is the date's or the one either side of it.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The year's magnitude is above 2^53 - 1, or the month is outside 1 to 12.</exception>
    // Broiler-AI:           Origin=AI; Spec=Temporal ISOWeekOfYear; IP=Low; Security=Medium; Resources=1; Fingerprint=D5B2A2
    // Broiler-Human:        PENDING
    public static (int Week, long Year) WeekOfYear(long year, int month, int day)
    {
        const int Wednesday = 3;
        const int Thursday = 4;
        const int Friday = 5;
        const int Saturday = 6;
        const int DaysInWeek = 7;
        const int MaxWeekNumber = 53;

        CheckMagnitude(year, YearLimit - 1, nameof(year));
        var dayOfYear = DayOfYear(year, month, day);
        var dayOfWeek = DayOfWeek(year, month, day);
        var week = (dayOfYear + DaysInWeek - dayOfWeek + Wednesday) / DaysInWeek;

        if (week < 1)
        {
            var dayOfJan1st = DayOfWeek(year, 1, 1);
            if (dayOfJan1st == Friday || (dayOfJan1st == Saturday && IsLeapYear(year - 1)))
                return (MaxWeekNumber, year - 1);

            return (MaxWeekNumber - 1, year - 1);
        }

        if (week == MaxWeekNumber)
        {
            var daysLaterInYear = DaysInYear(year) - dayOfYear;
            var daysAfterThursday = Thursday - dayOfWeek;
            if (daysLaterInYear < daysAfterThursday)
                return (1, year + 1);
        }

        return (week, year);
    }

    /// <summary>
    /// A year and a month that may lie outside 1 to 12, as a year and a month from 1 to 12:
    /// <c>BalanceISOYearMonth</c> of the Temporal proposal.
    /// </summary>
    /// <param name="year">The year, at most 2^62 in magnitude.</param>
    /// <param name="month">The one-based month, at most 2^62 in magnitude.</param>
    /// <returns><c>year + floor((month - 1) / 12)</c> and <c>((month - 1) modulo 12) + 1</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Either argument's magnitude is above 2^62.</exception>
    // Broiler-AI:           Origin=AI; Spec=Temporal BalanceISOYearMonth; IP=Low; Security=Low; Resources=1; Fingerprint=F28E75
    // Broiler-Human:        PENDING
    public static (long Year, int Month) BalanceYearMonth(long year, long month)
    {
        const long Limit = 4611686018427387904L; // 2^62
        CheckMagnitude(year, Limit, nameof(year));
        CheckMagnitude(month, Limit, nameof(month));

        var carry = EcmaTime.FloorDivide(month - 1, 12);
        return (year + carry, (int)(month - 1 - (carry * 12)) + 1);
    }

    /// <summary>Whether a year, a month and a day form a date: <c>IsValidISODate</c> of the Temporal proposal.</summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month.</param>
    /// <param name="day">The day.</param>
    /// <returns>Whether the month is from 1 to 12 and the day from 1 to the month's length in that year.</returns>
    // Broiler-AI:           Origin=AI; Spec=Temporal IsValidISODate; IP=Low; Security=Low; Resources=1; Fingerprint=B0CF60
    // Broiler-Human:        PENDING
    public static bool IsValidIsoDate(long year, int month, int day) =>
        month is >= 1 and <= 12 && day >= 1 && day <= DaysInMonth(year, month);

    // Broiler-AI:           Origin=AI; Spec=Temporal ISO 8601 calendar; IP=Low; Security=Low; Resources=1; Fingerprint=F37F17
    // Broiler-Human:        PENDING
    private static void CheckYear(long year) => CheckMagnitude(year, YearLimit, nameof(year));

    // Broiler-AI:           Origin=AI; Spec=Temporal ISO 8601 calendar; IP=Low; Security=Low; Resources=1; Fingerprint=4E140C
    // Broiler-Human:        PENDING
    private static void CheckMagnitude(long value, long limit, string name)
    {
        if (value < -limit || value > limit)
            throw new ArgumentOutOfRangeException(name, value, "The magnitude is above the operation's limit.");
    }

    // Broiler-AI:           Origin=AI; Spec=Temporal ISODaysInMonth; IP=Low; Security=Low; Resources=1; Fingerprint=75B846
    // Broiler-Human:        PENDING
    private static int CheckMonth(int month)
    {
        if (month is < 1 or > 12)
            throw new ArgumentOutOfRangeException(nameof(month), month, "The month is from 1 to 12.");

        return month;
    }
}
