// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   11
// Annotated:        11/11
// Exempt:           21
// Human-reviewed:   0/11
// IP risk:          Low
// Security risk:    Low
// Criteria:         0/0
// Resource impact:  0/10 max
// Unverified:       11
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Numerics;
using Broiler.ECMAScript.Numbers;
using Broiler.ECMAScript.Time;

namespace Broiler.ECMAScript.Temporal;

/// <summary>
/// The unit groups of the proposal's <c>ValidateTemporalUnitValue</c> (s13.18): the units an option of an operation
/// may name, by Table 21's category.
/// </summary>
// Broiler-AI:           Origin=AI; Spec=Temporal s13.18, Table 21; IP=None; Security=None; Resources=0; Fingerprint=971FAD
// Broiler-Human:        PENDING
public enum TemporalUnitGroup
{
    /// <summary><c>date</c>: year, month, week and day.</summary>
    Date = 0,

    /// <summary><c>time</c>: hour to nanosecond.</summary>
    Time = 1,

    /// <summary><c>datetime</c>: every unit.</summary>
    DateTime = 2,
}

/// <summary>Which of a type's two difference methods runs: the proposal's <c>until</c> or <c>since</c> operation.</summary>
// Broiler-AI:           Origin=AI; Spec=Temporal s13.43; IP=None; Security=None; Resources=0; Fingerprint=6769FF
// Broiler-Human:        PENDING
public enum DifferenceOperation
{
    /// <summary><c>until</c>: from the receiver to the argument.</summary>
    Until = 0,

    /// <summary><c>since</c>: from the argument to the receiver; the rounding mode is negated and the result too.</summary>
    Since = 1,
}

/// <summary>
/// The Temporal type whose method an option rule serves: the rules of <c>until</c>, <c>since</c> and <c>round</c>
/// differ by type (GetDifferenceSettings' arguments, and each <c>round</c>'s units and increments).
/// </summary>
// Broiler-AI:           Origin=AI; Spec=JSD-0078 options rules; IP=None; Security=None; Resources=0; Fingerprint=122ACE
// Broiler-Human:        PENDING
public enum TemporalType
{
    /// <summary><c>Temporal.PlainDate</c>.</summary>
    PlainDate = 0,

    /// <summary><c>Temporal.PlainDateTime</c>.</summary>
    PlainDateTime = 1,

    /// <summary><c>Temporal.PlainTime</c>.</summary>
    PlainTime = 2,

    /// <summary><c>Temporal.PlainYearMonth</c>.</summary>
    PlainYearMonth = 3,

    /// <summary><c>Temporal.Instant</c>.</summary>
    Instant = 4,

    /// <summary><c>Temporal.ZonedDateTime</c>.</summary>
    ZonedDateTime = 5,
}

/// <summary>
/// The record the proposal's <c>GetDifferenceSettings</c> (s13.43) returns, every unit resolved and validated, the
/// rounding mode already negated for <c>since</c>.
/// </summary>
/// <param name="SmallestUnit">The smallest unit, never unset or auto.</param>
/// <param name="LargestUnit">The largest unit, never unset or auto, at least as large as the smallest.</param>
/// <param name="RoundingMode">The rounding mode, negated for <c>since</c>.</param>
/// <param name="RoundingIncrement">The rounding increment, 1 to 10^9, valid for the smallest unit.</param>
// Broiler-AI:           Origin=Ported; Spec=Temporal s13.43 via Broiler.VM JsDifferenceSettings; IP=Low; Security=None; Resources=0; Fingerprint=667BFB
// Broiler-Human:        PENDING
public readonly record struct DifferenceSettings(TemporalUnit SmallestUnit, TemporalUnit LargestUnit, RoundingMode RoundingMode, long RoundingIncrement);

/// <summary>
/// What <c>relativeTo</c> resolved to: the record the proposal's <c>GetTemporalRelativeToOption</c> (s13.19) returns,
/// with the providers the library computes through. It is none, a plain date, or a zoned instant.
/// </summary>
/// <remarks>
/// <para>
/// The engine resolves the option itself - its property reads, its string parse and its <c>ToTemporal…</c>
/// classification stay the engine's (JSD-0078) - and hands the library the record. A plain date is a date within the
/// limits (<see cref="TemporalLimits.CheckIsoDate"/>, which the engine has checked); a zoned instant is a valid epoch
/// nanosecond count with its zone and calendar.
/// </para>
/// <para>
/// The default value is <see cref="None"/>: no <c>relativeTo</c>.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=Temporal s13.19, JSD-0078 decision 1; IP=None; Security=Low; Resources=0; Fingerprint=A02E30
// Broiler-Human:        PENDING
public readonly struct RelativeTo : IEquatable<RelativeTo>
{
    // Broiler-AI:           Origin=AI; Spec=Temporal s13.19; IP=None; Security=Low; Resources=0; Fingerprint=E56844
    // Broiler-Human:        PENDING
    private RelativeTo(bool plain, IsoDate date, BigInteger epochNanoseconds, ITimeZoneArithmetic? timeZone, ICalendarArithmetic calendars, CalendarId calendar)
    {
        IsPlain = plain;
        PlainDate = date;
        EpochNanoseconds = epochNanoseconds;
        TimeZone = timeZone;
        Calendars = calendars;
        Calendar = calendar;
    }

    /// <summary>No <c>relativeTo</c>: the option was undefined.</summary>
    public static RelativeTo None => default;

    /// <summary>Whether the record is a plain date (<c>[[PlainRelativeTo]]</c>).</summary>
    public bool IsPlain { get; }

    /// <summary>Whether the record is a zoned instant (<c>[[ZonedRelativeTo]]</c>).</summary>
    // Broiler-AI:           Origin=AI; Spec=Temporal s13.19; IP=None; Security=None; Resources=0; Fingerprint=A7915B
    // Broiler-Human:        PENDING
    public bool IsZoned => TimeZone is not null;

    /// <summary>The plain date; the default unless <see cref="IsPlain"/>.</summary>
    public IsoDate PlainDate { get; }

    /// <summary>The zoned instant's epoch nanoseconds; zero unless <see cref="IsZoned"/>.</summary>
    public BigInteger EpochNanoseconds { get; }

    /// <summary>The zoned instant's time zone; <see langword="null"/> unless <see cref="IsZoned"/>.</summary>
    public ITimeZoneArithmetic? TimeZone { get; }

    /// <summary>The calendar provider the record's calendar computes through; <see langword="null"/> for <see cref="None"/>.</summary>
    public ICalendarArithmetic? Calendars { get; }

    /// <summary>The record's calendar.</summary>
    public CalendarId Calendar { get; }

    /// <summary>A plain date as <c>relativeTo</c>: a <c>PlainDate</c>, a <c>PlainDateTime</c>'s date, or a bag or string with no time zone.</summary>
    /// <param name="date">The date, within the limits.</param>
    /// <param name="calendars">The calendar provider, <see cref="TemporalCalendars"/> or the engine's.</param>
    /// <param name="calendar">The date's calendar.</param>
    /// <returns>The record.</returns>
    /// <exception cref="ArgumentNullException">The provider is <see langword="null"/>.</exception>
    // Broiler-AI:           Origin=AI; Spec=Temporal s13.19; IP=None; Security=Low; Resources=0; Fingerprint=807C11
    // Broiler-Human:        PENDING
    public static RelativeTo FromPlainDate(IsoDate date, ICalendarArithmetic calendars, CalendarId calendar)
    {
        ArgumentNullException.ThrowIfNull(calendars);
        return new RelativeTo(true, date, BigInteger.Zero, null, calendars, calendar);
    }

    /// <summary>A zoned instant as <c>relativeTo</c>: a <c>ZonedDateTime</c>, or a bag or string with a time zone.</summary>
    /// <param name="epochNanoseconds">The instant, a valid epoch nanosecond count.</param>
    /// <param name="timeZone">The instant's time zone.</param>
    /// <param name="calendars">The calendar provider, <see cref="TemporalCalendars"/> or the engine's.</param>
    /// <param name="calendar">The instant's calendar.</param>
    /// <returns>The record.</returns>
    /// <exception cref="ArgumentNullException">A provider is <see langword="null"/>.</exception>
    // Broiler-AI:           Origin=AI; Spec=Temporal s13.19; IP=None; Security=Low; Resources=0; Fingerprint=C09167
    // Broiler-Human:        PENDING
    public static RelativeTo FromZoned(BigInteger epochNanoseconds, ITimeZoneArithmetic timeZone, ICalendarArithmetic calendars, CalendarId calendar)
    {
        ArgumentNullException.ThrowIfNull(timeZone);
        ArgumentNullException.ThrowIfNull(calendars);
        return new RelativeTo(false, default, epochNanoseconds, timeZone, calendars, calendar);
    }

    /// <inheritdoc/>
    // Broiler-AI:           Origin=AI; Spec=Temporal s13.19; IP=None; Security=None; Resources=0; Fingerprint=FF1905
    // Broiler-Human:        PENDING
    public bool Equals(RelativeTo other) =>
        IsPlain == other.IsPlain && PlainDate == other.PlainDate && EpochNanoseconds == other.EpochNanoseconds &&
        ReferenceEquals(TimeZone, other.TimeZone) && ReferenceEquals(Calendars, other.Calendars) && Calendar == other.Calendar;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is RelativeTo other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(IsPlain, PlainDate, EpochNanoseconds, Calendar);

    /// <summary>Whether two records are the same.</summary>
    /// <param name="left">The first record.</param>
    /// <param name="right">The second record.</param>
    /// <returns>Whether they are equal.</returns>
    public static bool operator ==(RelativeTo left, RelativeTo right) => left.Equals(right);

    /// <summary>Whether two records differ.</summary>
    /// <param name="left">The first record.</param>
    /// <param name="right">The second record.</param>
    /// <returns>Whether they differ.</returns>
    // Broiler-AI:           Origin=AI; Spec=Temporal s13.19; IP=None; Security=None; Resources=0; Fingerprint=3F347F
    // Broiler-Human:        PENDING
    public static bool operator !=(RelativeTo left, RelativeTo right) => !left.Equals(right);
}
