// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   18
// Annotated:        18/18
// Exempt:           42
// Human-reviewed:   0/18
// IP risk:          Low
// Security risk:    Low
// Criteria:         0/0
// Resource impact:  1/10 max
// Unverified:       18
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Generic;
using System.Numerics;
using Broiler.ECMAScript.Time;

namespace Broiler.ECMAScript.Intl;

/// <summary>
/// The date-time components an <c>Intl.DateTimeFormat</c>'s options name (ECMA-402 Table 16, <c>table-datetimeformat-components</c>),
/// each as GetOption read it, or <see langword="null"/> where the option is absent.
/// </summary>
/// <remarks>
/// The values are the ones the table admits, spelled as ECMA-402 spells them; an engine's GetOption has refused any other
/// with a <c>RangeError</c> before the library sees it, and <see cref="DateTimeFormat.Create"/> refuses one with
/// <see cref="ArgumentException"/>.
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-createdatetimeformat Table 16 components and options, proposal-temporal e8cc03fc Temporal values; IP=Low; Security=Low; Resources=0; Fingerprint=B1319F
// Broiler-Human:        PENDING
public sealed record DateTimeComponents
{
    /// <summary>The components in the table's order, which CreateDateTimeFormat reads them in and resolvedOptions reports them in.</summary>
    public static IReadOnlyList<string> Names { get; } =
        ["weekday", "era", "year", "month", "day", "dayPeriod", "hour", "minute", "second", "fractionalSecondDigits", "timeZoneName"];

    /// <summary>No component.</summary>
    public static DateTimeComponents None { get; } = new();

    /// <summary><c>weekday</c>: <c>narrow</c>, <c>short</c> or <c>long</c>.</summary>
    public string? Weekday { get; init; }

    /// <summary><c>era</c>: <c>narrow</c>, <c>short</c> or <c>long</c>.</summary>
    public string? Era { get; init; }

    /// <summary><c>year</c>: <c>2-digit</c> or <c>numeric</c>.</summary>
    public string? Year { get; init; }

    /// <summary><c>month</c>: <c>2-digit</c>, <c>numeric</c>, <c>narrow</c>, <c>short</c> or <c>long</c>.</summary>
    public string? Month { get; init; }

    /// <summary><c>day</c>: <c>2-digit</c> or <c>numeric</c>.</summary>
    public string? Day { get; init; }

    /// <summary><c>dayPeriod</c>: <c>narrow</c>, <c>short</c> or <c>long</c>.</summary>
    public string? DayPeriod { get; init; }

    /// <summary><c>hour</c>: <c>2-digit</c> or <c>numeric</c>.</summary>
    public string? Hour { get; init; }

    /// <summary><c>minute</c>: <c>2-digit</c> or <c>numeric</c>.</summary>
    public string? Minute { get; init; }

    /// <summary><c>second</c>: <c>2-digit</c> or <c>numeric</c>.</summary>
    public string? Second { get; init; }

    /// <summary><c>fractionalSecondDigits</c>, as GetNumberOption read it: 1 to 3.</summary>
    public int? FractionalSecondDigits { get; init; }

    /// <summary><c>timeZoneName</c>: <c>short</c>, <c>long</c>, <c>shortOffset</c>, <c>longOffset</c>, <c>shortGeneric</c> or <c>longGeneric</c>.</summary>
    public string? TimeZoneName { get; init; }

    /// <summary>A component's value by its name, <c>fractionalSecondDigits</c> as that many <c>S</c>, as Broiler.VM keeps it.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-createdatetimeformat Table 16 components and options, proposal-temporal e8cc03fc Temporal values; IP=Low; Security=Low; Resources=1; Fingerprint=07E07B
    // Broiler-Human:        PENDING
    internal string? Value(string name) => name switch
    {
        "weekday" => Weekday,
        "era" => Era,
        "year" => Year,
        "month" => Month,
        "day" => Day,
        "dayPeriod" => DayPeriod,
        "hour" => Hour,
        "minute" => Minute,
        "second" => Second,
        "fractionalSecondDigits" => FractionalSecondDigits is { } digits ? new string('S', digits) : null,
        "timeZoneName" => TimeZoneName,
        _ => null,
    };

    /// <summary>The values each component admits (Table 16); <see langword="null"/> for <c>fractionalSecondDigits</c>, a number.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-createdatetimeformat Table 16 components and options, proposal-temporal e8cc03fc Temporal values; IP=Low; Security=Low; Resources=1; Fingerprint=EB97A6
    // Broiler-Human:        PENDING
    internal static string[]? Admitted(string name) => name switch
    {
        "weekday" or "era" or "dayPeriod" => ["narrow", "short", "long"],
        "year" or "day" or "hour" or "minute" or "second" => ["2-digit", "numeric"],
        "month" => ["2-digit", "numeric", "narrow", "short", "long"],
        "timeZoneName" => ["short", "long", "shortOffset", "longOffset", "shortGeneric", "longGeneric"],
        _ => null,
    };
}

/// <summary>CreateDateTimeFormat's <c>required</c> argument (<c>sec-createdatetimeformat</c>): which fields a format must have.</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-createdatetimeformat Table 16 components and options, proposal-temporal e8cc03fc Temporal values; IP=Low; Security=Low; Resources=0; Fingerprint=1D70C9
// Broiler-Human:        PENDING
public enum DateTimeRequired
{
    /// <summary><c>any</c>: the constructor's, and <c>Date.prototype.toLocaleString</c>'s.</summary>
    Any = 0,

    /// <summary><c>date</c>: <c>Date.prototype.toLocaleDateString</c>'s.</summary>
    Date = 1,

    /// <summary><c>time</c>: <c>Date.prototype.toLocaleTimeString</c>'s.</summary>
    Time = 2,
}

/// <summary>CreateDateTimeFormat's <c>defaults</c> argument (<c>sec-createdatetimeformat</c>): which fields a format without any is given.</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-createdatetimeformat Table 16 components and options, proposal-temporal e8cc03fc Temporal values; IP=Low; Security=Low; Resources=0; Fingerprint=FE9D2A
// Broiler-Human:        PENDING
public enum DateTimeDefaults
{
    /// <summary><c>date</c>: the constructor's, and <c>toLocaleDateString</c>'s.</summary>
    Date = 0,

    /// <summary><c>time</c>: <c>toLocaleTimeString</c>'s.</summary>
    Time = 1,

    /// <summary><c>all</c>: <c>toLocaleString</c>'s.</summary>
    All = 2,
}

/// <summary>
/// What an <c>Intl.DateTimeFormat</c> constructor read after ResolveLocale (<c>sec-createdatetimeformat</c>, as
/// <c>tc39/proposal-temporal@e8cc03fc</c> amends it), each converted and checked by the engine in the specification's order:
/// <c>hour12</c>, the resolved time zone, the components, <c>dateStyle</c> and <c>timeStyle</c>, and the operation's
/// <c>required</c> and <c>defaults</c>. <c>localeMatcher</c>, <c>calendar</c>, <c>numberingSystem</c>, <c>hourCycle</c> and
/// <c>formatMatcher</c> are read too, the first four for ResolveLocale, and so are not here.
/// </summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-createdatetimeformat Table 16 components and options, proposal-temporal e8cc03fc Temporal values; IP=Low; Security=Low; Resources=0; Fingerprint=DCF3BE
// Broiler-Human:        PENDING
public sealed record DateTimeFormatOptions
{
    /// <summary><c>hour12</c>, as GetOption read it, or <see langword="null"/> where it is absent.</summary>
    public bool? Hour12 { get; init; }

    /// <summary>The resolved time zone: <see cref="ResolvedTimeZone.Resolve"/>'s, or an engine's default zone.</summary>
    public ResolvedTimeZone TimeZone { get; init; } = ResolvedTimeZone.Utc;

    /// <summary>The components the options name.</summary>
    public DateTimeComponents Components { get; init; } = DateTimeComponents.None;

    /// <summary><c>dateStyle</c>: <c>full</c>, <c>long</c>, <c>medium</c> or <c>short</c>, or <see langword="null"/>.</summary>
    public string? DateStyle { get; init; }

    /// <summary><c>timeStyle</c>: <c>full</c>, <c>long</c>, <c>medium</c> or <c>short</c>, or <see langword="null"/>.</summary>
    public string? TimeStyle { get; init; }

    /// <summary>The operation's <c>required</c>.</summary>
    public DateTimeRequired Required { get; init; }

    /// <summary>The operation's <c>defaults</c>.</summary>
    public DateTimeDefaults Defaults { get; init; }

    /// <summary>
    /// Whether the format is <c>Temporal.ZonedDateTime.prototype.toLocaleString</c>'s, whose zone is the value's own and whose
    /// Instant format names it by default (the proposal's amendment of CreateDateTimeFormat); its engine has refused a
    /// <c>timeZone</c> option with a <c>TypeError</c>.
    /// </summary>
    public bool ForZonedDateTime { get; init; }
}

/// <summary>The kind of a value a date-time format writes: ToDateTimeFormattable's (the proposal's <c>sec-todatetimeformattable</c>).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-createdatetimeformat Table 16 components and options, proposal-temporal e8cc03fc Temporal values; IP=Low; Security=Low; Resources=0; Fingerprint=864991
// Broiler-Human:        PENDING
public enum DateTimeValueKind
{
    /// <summary>A Number, a time value to be clipped.</summary>
    Time = 0,

    /// <summary>A <c>Temporal.PlainDate</c>.</summary>
    PlainDate = 1,

    /// <summary>A <c>Temporal.PlainYearMonth</c>.</summary>
    PlainYearMonth = 2,

    /// <summary>A <c>Temporal.PlainMonthDay</c>.</summary>
    PlainMonthDay = 3,

    /// <summary>A <c>Temporal.PlainTime</c>.</summary>
    PlainTime = 4,

    /// <summary>A <c>Temporal.PlainDateTime</c>.</summary>
    PlainDateTime = 5,

    /// <summary>A <c>Temporal.Instant</c>.</summary>
    Instant = 6,

    /// <summary>A <c>Temporal.ZonedDateTime</c>.</summary>
    ZonedDateTime = 7,
}

/// <summary>
/// A value a date-time format writes, as an engine read it from a Temporal object's slots or converted it with ToNumber
/// (the proposal's ToDateTimeFormattable, <c>sec-todatetimeformattable</c>): no object, no property access (convention 2).
/// </summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-createdatetimeformat Table 16 components and options, proposal-temporal e8cc03fc Temporal values; IP=Low; Security=Low; Resources=0; Fingerprint=6D094C
// Broiler-Human:        PENDING
public readonly record struct DateTimeValue
{
    private DateTimeValue(DateTimeValueKind kind, double time, IsoDate date, IsoTime timeOfDay, BigInteger epochNanoseconds, string calendar)
    {
        Kind = kind;
        Time = time;
        Date = date;
        TimeOfDay = timeOfDay;
        EpochNanoseconds = epochNanoseconds;
        Calendar = calendar;
    }

    /// <summary>The value's kind.</summary>
    public DateTimeValueKind Kind { get; }

    /// <summary>A Number's value, not yet clipped.</summary>
    public double Time { get; }

    /// <summary>A plain value's ISO date: a PlainYearMonth's and a PlainMonthDay's reference date.</summary>
    public IsoDate Date { get; }

    /// <summary>A PlainTime's or a PlainDateTime's time.</summary>
    public IsoTime TimeOfDay { get; }

    /// <summary>An Instant's or a ZonedDateTime's epoch nanoseconds.</summary>
    public BigInteger EpochNanoseconds { get; }

    /// <summary>The value's calendar identifier, <c>iso8601</c> for a value with none.</summary>
    public string Calendar { get; }

    /// <summary>A Number (ToNumber's), to be clipped by TimeClip.</summary>
    /// <param name="time">The Number.</param>
    /// <returns>The value.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-createdatetimeformat Table 16 components and options, proposal-temporal e8cc03fc Temporal values; IP=Low; Security=Low; Resources=1; Fingerprint=28631E
    // Broiler-Human:        PENDING
    public static DateTimeValue Number(double time) => new(DateTimeValueKind.Time, time, default, default, default, "iso8601");

    /// <summary>A <c>Temporal.PlainDate</c>'s [[ISODate]] and [[Calendar]].</summary>
    /// <param name="date">The ISO date.</param>
    /// <param name="calendar">The calendar identifier.</param>
    /// <returns>The value.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-createdatetimeformat Table 16 components and options, proposal-temporal e8cc03fc Temporal values; IP=Low; Security=Low; Resources=1; Fingerprint=7E7E1C
    // Broiler-Human:        PENDING
    public static DateTimeValue PlainDate(IsoDate date, string calendar) => new(DateTimeValueKind.PlainDate, 0, date, default, default, Checked(calendar));

    /// <summary>A <c>Temporal.PlainYearMonth</c>'s [[ISODate]] and [[Calendar]].</summary>
    /// <param name="date">The reference ISO date.</param>
    /// <param name="calendar">The calendar identifier.</param>
    /// <returns>The value.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-createdatetimeformat Table 16 components and options, proposal-temporal e8cc03fc Temporal values; IP=Low; Security=Low; Resources=1; Fingerprint=DE9760
    // Broiler-Human:        PENDING
    public static DateTimeValue PlainYearMonth(IsoDate date, string calendar) => new(DateTimeValueKind.PlainYearMonth, 0, date, default, default, Checked(calendar));

    /// <summary>A <c>Temporal.PlainMonthDay</c>'s [[ISODate]] and [[Calendar]].</summary>
    /// <param name="date">The reference ISO date.</param>
    /// <param name="calendar">The calendar identifier.</param>
    /// <returns>The value.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-createdatetimeformat Table 16 components and options, proposal-temporal e8cc03fc Temporal values; IP=Low; Security=Low; Resources=1; Fingerprint=8C9CAC
    // Broiler-Human:        PENDING
    public static DateTimeValue PlainMonthDay(IsoDate date, string calendar) => new(DateTimeValueKind.PlainMonthDay, 0, date, default, default, Checked(calendar));

    /// <summary>A <c>Temporal.PlainTime</c>'s [[Time]].</summary>
    /// <param name="time">The time.</param>
    /// <returns>The value.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-createdatetimeformat Table 16 components and options, proposal-temporal e8cc03fc Temporal values; IP=Low; Security=Low; Resources=1; Fingerprint=142F59
    // Broiler-Human:        PENDING
    public static DateTimeValue PlainTime(IsoTime time) => new(DateTimeValueKind.PlainTime, 0, default, time, default, "iso8601");

    /// <summary>A <c>Temporal.PlainDateTime</c>'s [[ISODateTime]] and [[Calendar]].</summary>
    /// <param name="dateTime">The ISO date-time.</param>
    /// <param name="calendar">The calendar identifier.</param>
    /// <returns>The value.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-createdatetimeformat Table 16 components and options, proposal-temporal e8cc03fc Temporal values; IP=Low; Security=Low; Resources=1; Fingerprint=E9BC09
    // Broiler-Human:        PENDING
    public static DateTimeValue PlainDateTime(IsoDateTime dateTime, string calendar) =>
        new(DateTimeValueKind.PlainDateTime, 0, dateTime.Date, dateTime.Time, default, Checked(calendar));

    /// <summary>A <c>Temporal.Instant</c>'s [[EpochNanoseconds]].</summary>
    /// <param name="epochNanoseconds">The epoch nanoseconds.</param>
    /// <returns>The value.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-createdatetimeformat Table 16 components and options, proposal-temporal e8cc03fc Temporal values; IP=Low; Security=Low; Resources=1; Fingerprint=AF3A0C
    // Broiler-Human:        PENDING
    public static DateTimeValue Instant(BigInteger epochNanoseconds) => new(DateTimeValueKind.Instant, 0, default, default, epochNanoseconds, "iso8601");

    /// <summary>A <c>Temporal.ZonedDateTime</c>'s [[EpochNanoseconds]] and [[Calendar]].</summary>
    /// <param name="epochNanoseconds">The epoch nanoseconds.</param>
    /// <param name="calendar">The calendar identifier.</param>
    /// <returns>The value.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-createdatetimeformat Table 16 components and options, proposal-temporal e8cc03fc Temporal values; IP=Low; Security=Low; Resources=1; Fingerprint=CCFB02
    // Broiler-Human:        PENDING
    public static DateTimeValue ZonedDateTime(BigInteger epochNanoseconds, string calendar) =>
        new(DateTimeValueKind.ZonedDateTime, 0, default, default, epochNanoseconds, Checked(calendar));

    /// <summary>Whether the value is a Temporal object's rather than a Number's.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-createdatetimeformat Table 16 components and options, proposal-temporal e8cc03fc Temporal values; IP=Low; Security=Low; Resources=0; Fingerprint=99D94D
    // Broiler-Human:        PENDING
    public bool IsTemporal => Kind != DateTimeValueKind.Time;

    // Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-createdatetimeformat Table 16 components and options, proposal-temporal e8cc03fc Temporal values; IP=Low; Security=Low; Resources=1; Fingerprint=7FA7A6
    // Broiler-Human:        PENDING
    private static string Checked(string calendar)
    {
        ArgumentNullException.ThrowIfNull(calendar);
        return calendar;
    }
}
