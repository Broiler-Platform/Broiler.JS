// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   40
// Annotated:        40/40
// Exempt:           15
// Human-reviewed:   0/40
// IP risk:          Low
// Security risk:    High
// Criteria:         5/5
// Resource impact:  2/10 max
// Unverified:       40
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using Broiler.ECMAScript.Temporal;
using Broiler.ECMAScript.Time;

namespace Broiler.ECMAScript.Intl;

/// <summary>
/// A part of a formatted date (ECMA-402 FormatDateTimeToParts, <c>sec-formatdatetimetoparts</c>, and
/// FormatDateTimeRangeToParts, <c>sec-formatdatetimerangetoparts</c>): its type, its text, and for a range its source.
/// </summary>
/// <param name="Type">The part's type: <c>era</c>, <c>year</c>, <c>month</c>, <c>day</c>, <c>weekday</c>, <c>dayPeriod</c>,
/// <c>hour</c>, <c>minute</c>, <c>second</c>, <c>fractionalSecond</c>, <c>timeZoneName</c>, <c>relatedYear</c>,
/// <c>yearName</c> or <c>literal</c>.</param>
/// <param name="Value">The part's text.</param>
/// <param name="Source">For a range, <c>startRange</c>, <c>endRange</c> or <c>shared</c>; <see langword="null"/> for one date.</param>
// Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-formatdatetimetoparts parts, JSD-0082 D15 the zone named by its primary; IP=Low; Security=Low; Resources=0; Fingerprint=B39385
// Broiler-Human:        PENDING
public sealed record DateTimePart(string Type, string Value, string? Source);

/// <summary>A formatted field: its pattern letter and where its text is.</summary>
// Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-formatdatetimepattern sec-partitiondatetimepattern UTS35 Part 4 section 8 via Broiler.VM JsDateTimeFormatter; IP=Low; Security=Low; Resources=0; Fingerprint=D66AD3
// Broiler-Human:        PENDING
internal readonly record struct DateTimeField(char Letter, int Start, int End);

/// <summary>
/// A time value broken into proleptic Gregorian fields in a zone's local time, with the era year
/// ICU's <c>y</c> writes.
/// </summary>
// Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-formatdatetimepattern sec-partitiondatetimepattern UTS35 Part 4 section 8 via Broiler.VM JsDateTimeFormatter; IP=Low; Security=Low; Resources=0; Fingerprint=ABDA16
// Broiler-Human:        PENDING
internal readonly record struct LocalDateTime(long Year, int Month, int Day, int Weekday, int Hour, int Minute, int Second, int Millisecond, int DayOfYear, long Utc)
{
    /// <summary>The CLDR era index of a calendar's era; null for the Gregorian calendar's, which the year decides.</summary>
    internal string? CalendarEra { get; init; }

    /// <summary>A calendar's era year; null for the Gregorian calendar's.</summary>
    internal long? CalendarEraYear { get; init; }

    /// <summary>The CLDR key of a calendar's month name, where it is not the month's number.</summary>
    internal string? MonthKey { get; init; }

    /// <summary>The number a calendar writes for its month, where it is not the ordinal: a Chinese month's.</summary>
    internal int? MonthNumber { get; init; }

    /// <summary>Whether the month is a leap month that CLDR's month patterns mark (the Chinese calendars').</summary>
    internal bool LeapMonth { get; init; }

    /// <summary>The year of the sixty-year cycle (ICU's <c>y</c> in the Chinese calendars); null elsewhere.</summary>
    internal int? CyclicYear { get; init; }

    /// <summary>The CLDR era index: the calendar's, or the Gregorian one the year decides.</summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-formatdatetimepattern sec-partitiondatetimepattern UTS35 Part 4 section 8 via Broiler.VM JsDateTimeFormatter; IP=Low; Security=Low; Resources=0; Fingerprint=745584
    // Broiler-Human:        PENDING
    internal string Era => CalendarEra ?? (Year <= 0 ? "0" : "1");

    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-formatdatetimepattern sec-partitiondatetimepattern UTS35 Part 4 section 8 via Broiler.VM JsDateTimeFormatter; IP=Low; Security=Low; Resources=0; Fingerprint=1BA541
    // Broiler-Human:        PENDING
    internal long EraYear => CalendarEraYear ?? (Year <= 0 ? 1 - Year : Year);

    /// <summary>
    /// The same time's fields in a calendar other than the Gregorian one (JSD-0057): its arithmetic
    /// year, ordinal month, day and day of the year, its era as CLDR numbers it, and how its month is
    /// named and numbered. The calendar's date is the shared calendars' CalendarISOToDate since SJL-10.4
    /// (JSD-0078); how CLDR names and numbers it stays here.
    /// </summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-formatdatetimepattern sec-partitiondatetimepattern UTS35 Part 4 section 8 via Broiler.VM JsDateTimeFormatter; IP=Low; Security=Medium; Resources=1; Fingerprint=4150DE
    // Broiler-Human:        PENDING
    internal LocalDateTime InCalendar(TemporalCalendars calendars, CalendarId calendar)
    {
        // A TIME VALUE'S LOCAL DATE IS WITHIN THE CALENDARS' YEARS: its year is within ±275,760, so the
        // shared calendars answer it; a status other than completion would be an engine defect.
        if (calendars.IsoToDate(calendar, new IsoDate(Year, Month, Day), out var date) != CalendarStatus.Completed)
        {
            throw new InvalidOperationException("A time value's local date is outside the calendars' years.");
        }

        var id = TemporalCalendars.Identifier(calendar);
        var code = date.MonthCode;
        var number = code.Number;
        var lunisolar = calendar is CalendarId.Chinese or CalendarId.Dangi;

        string? monthKey = calendar switch
        {
            CalendarId.Hebrew => code.IsLeapMonth && number == 5 ? "6" : !code.IsLeapMonth && number == 6 && date.InLeapYear ? "7-yeartype-leap" : (number < 6 ? number : number + 1).ToString(NumberFormatInfo.InvariantInfo),
            CalendarId.Chinese or CalendarId.Dangi => number.ToString(NumberFormatInfo.InvariantInfo),
            _ => null,
        };

        return this with
        {
            Year = date.Year,
            Month = date.Month,
            Day = date.Day,
            DayOfYear = date.DayOfYear,
            CalendarEra = lunisolar ? string.Empty : EraIndex(id, date.Era!),
            CalendarEraYear = lunisolar ? date.Year : date.EraYear,
            MonthKey = monthKey,
            MonthNumber = lunisolar ? number : null,
            LeapMonth = lunisolar && code.IsLeapMonth,
            CyclicYear = lunisolar ? (int)((((date.Year - 4) % 60) + 60) % 60) + 1 : null,
        };
    }

    /// <summary>The index CLDR gives a Temporal era of a calendar (the Intl era and month code proposal's Table 2).</summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-formatdatetimepattern sec-partitiondatetimepattern UTS35 Part 4 section 8 via Broiler.VM JsDateTimeFormatter; IP=Low; Security=Low; Resources=1; Fingerprint=43DD64
    // Broiler-Human:        PENDING
    private static string EraIndex(string calendar, string era) => (calendar, era) switch
    {
        ("japanese", "meiji") => "232",
        ("japanese", "taisho") => "233",
        ("japanese", "showa") => "234",
        ("japanese", "heisei") => "235",
        ("japanese", "reiwa") => "236",
        (_, "bce" or "broc") => "0",
        (_, "ce" or "roc") => "1",
        (_, "bh") => "1",
        ("coptic" or "ethiopic", "am") => "1",
        _ => "0",
    };

    /// <summary>The fields of <paramref name="time"/>, an integral time value, <paramref name="offsetSeconds"/> seconds from UTC.</summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-formatdatetimepattern sec-partitiondatetimepattern UTS35 Part 4 section 8 via Broiler.VM JsDateTimeFormatter; IP=Low; Security=Medium; Resources=1; Fingerprint=CF0BB6
    // Broiler-Human:        PENDING
    internal static LocalDateTime From(double time, int offsetSeconds)
    {
        // THE SHARED FIELD OPERATIONS (JSD-0073): an integral time value and an offset of at most a
        // day keep the local time well inside the 2^62 they take.
        var local = time + (offsetSeconds * 1_000.0);

        return new LocalDateTime(
            (long)EcmaTime.YearFromTime(local),
            (int)EcmaTime.MonthFromTime(local) + 1,
            (int)EcmaTime.DateFromTime(local),
            (int)EcmaTime.WeekDay(local),
            (int)EcmaTime.HourFromTime(local),
            (int)EcmaTime.MinFromTime(local),
            (int)EcmaTime.SecFromTime(local),
            (int)EcmaTime.MsFromTime(local),
            (int)EcmaTime.DayWithinYear(local) + 1,
            (long)time);
    }
}

/// <summary>
/// A resolved <c>Intl.DateTimeFormat</c>'s formatting of one pattern: FormatDateTimePattern (<c>sec-formatdatetimepattern</c>)
/// as ICU's SimpleDateFormat writes a pattern for a time value, and PartitionDateTimeRangePattern
/// (<c>sec-partitiondatetimerangepattern</c>) as ICU's DateIntervalFormat writes a range (Broiler.VM's JSD-0082, rows T12 to
/// T37).
/// </summary>
/// <remarks>
/// <para>
/// <b>Every field is written from CLDR's data for the data locale</b>: names in the context the letter asks for (<c>M</c> and
/// <c>E</c> the format forms, <c>L</c> and <c>c</c> the stand-alone), numbers in the format's numbering system, a flexible
/// day period by the language's rules, and a zone by its CLDR name, named by its primary identifier (decision D15), or
/// CLDR's GMT format.
/// </para>
/// <para>
/// <b>Ported from Broiler.VM's <c>JsDateTimeFormatter</c></b> at its SJL-9 rule commit <c>4cf9ca44</c>, with the invariant
/// culture's integer formatting written with <see cref="NumberFormatInfo.InvariantInfo"/> (decision D19, row T41) and the
/// caller's <see cref="WorkCharge"/> where Broiler.VM charged its engine's meter: the output's length, once per String or
/// range written (decision D18, row T40). The parts arrays and their per-part charge are the engine's.
/// </para>
/// <para><b>Immutable</b> once made, but for its interval formatter, made under a lock the first time a range is written: one
/// may be shared between threads (convention 10).</para>
/// </remarks>
// Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-formatdatetimepattern sec-partitiondatetimepattern UTS35 Part 4 section 8 via Broiler.VM JsDateTimeFormatter; IP=Low; Security=Low; Resources=0; Fingerprint=088114
// Broiler-Human:        PENDING
public sealed class DateTimeFormatter
{
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-formatdatetimepattern sec-partitiondatetimepattern UTS35 Part 4 section 8 via Broiler.VM JsDateTimeFormatter; IP=Low; Security=Low; Resources=0; Fingerprint=B95198
    // Broiler-Human:        PENDING
    private static readonly ImmutableArray<string> DayKeys = ["sun", "mon", "tue", "wed", "thu", "fri", "sat"];

    private readonly DateData data;

    private readonly string language;

    private readonly string[] digits;

    private readonly Lazy<DateIntervalFormatter> interval;

    private readonly ResolvedTimeZone timeZone;

    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-formatdatetimepattern sec-partitiondatetimepattern UTS35 Part 4 section 8 via Broiler.VM JsDateTimeFormatter; IP=Low; Security=Low; Resources=1; Fingerprint=65B070
    // Broiler-Human:        PENDING
    internal DateTimeFormatter(DateData data, string language, string[] digits, ResolvedTimeZone timeZone,
        string pattern, Func<DateIntervalFormatter> intervals, (TemporalCalendars Calendars, CalendarId Id)? calendar = null, ZoneNaming? naming = null)
    {
        this.calendar = calendar;
        this.naming = naming;
        this.data = data;
        this.language = language;
        this.digits = digits;
        this.timeZone = timeZone;
        interval = new(intervals, System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);
        Pattern = pattern;
    }

    /// <summary>The IANA zone whose offsets apply, the shared database's rules of it, or nothing for UTC and a fixed offset.</summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-formatdatetimepattern sec-partitiondatetimepattern UTS35 Part 4 section 8 via Broiler.VM JsDateTimeFormatter; IP=Low; Security=Low; Resources=0; Fingerprint=8C89C8
    // Broiler-Human:        PENDING
    private ITimeZoneRules? Zone => timeZone.Rules;

    /// <summary>The calendar the fields are written in, or nothing for the Gregorian and ISO 8601 ones (JSD-0057).</summary>
    private readonly (TemporalCalendars Calendars, CalendarId Id)? calendar;

    /// <summary>What an IANA zone is named with, or nothing where it is written in the GMT format alone (JSD-0058).</summary>
    private readonly ZoneNaming? naming;

    /// <summary>A time value's local fields, in the format's calendar.</summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-formatdatetimepattern sec-partitiondatetimepattern UTS35 Part 4 section 8 via Broiler.VM JsDateTimeFormatter; IP=Low; Security=Low; Resources=1; Fingerprint=7429C2
    // Broiler-Human:        PENDING
    internal LocalDateTime LocalTime(double time)
    {
        var local = LocalDateTime.From(time, OffsetSecondsAt(time));
        return calendar is { } system ? local.InCalendar(system.Calendars, system.Id) : local;
    }

    /// <summary>[[TimeZone]]: the identifier as given, in the database's case, or an offset such as <c>+05:30</c> (decision D15).</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-formatdatetimetoparts parts, JSD-0082 D15 the zone named by its primary; IP=Low; Security=Low; Resources=0; Fingerprint=19346A
    // Broiler-Human:        PENDING
    public string TimeZone => timeZone.Identifier;

    /// <summary>The zone, with the primary identifier it is named by.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-formatdatetimetoparts parts, JSD-0082 D15 the zone named by its primary; IP=Low; Security=Low; Resources=0; Fingerprint=FFD5EF
    // Broiler-Human:        PENDING
    public ResolvedTimeZone ResolvedTimeZone => timeZone;

    /// <summary>The primary identifier the zone is named by (decision D15): Broiler.VM's [[TimeZone]].</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-formatdatetimetoparts parts, JSD-0082 D15 the zone named by its primary; IP=Low; Security=Low; Resources=0; Fingerprint=357EFF
    // Broiler-Human:        PENDING
    private string NamingZone => timeZone.PrimaryIdentifier;

    /// <summary>The zone's offset from UTC, in minutes, where it is fixed; zero for an IANA zone.</summary>
    internal int OffsetMinutes => timeZone.OffsetMinutes;

    /// <summary>The offset, in seconds, in force at a time value: the fixed one, or the IANA zone's then.</summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-formatdatetimepattern sec-partitiondatetimepattern UTS35 Part 4 section 8 via Broiler.VM JsDateTimeFormatter; IP=Low; Security=Low; Resources=1; Fingerprint=55FCB9
    // Broiler-Human:        PENDING
    internal int OffsetSecondsAt(double time) =>
        Zone is null ? OffsetMinutes * 60 : Zone.OffsetSecondsAt((long)Math.Floor(time / 1000));

    /// <summary>Which kind of zone it is.</summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-formatdatetimepattern sec-partitiondatetimepattern UTS35 Part 4 section 8 via Broiler.VM JsDateTimeFormatter; IP=Low; Security=Low; Resources=0; Fingerprint=A87F66
    // Broiler-Human:        PENDING
    internal TimeZoneKind ZoneKind => timeZone.Kind;

    /// <summary>
    /// The least time value the formatter writes: -271821-04-01T00:00:00.000Z, the first day of the earliest month a Temporal value
    /// holds (the proposal's ISOYearMonthWithinLimits), a PlainYearMonth's reference day. HandleDateTimeValue does not clip a
    /// Temporal value (test262 <c>temporal-objects-no-time-clip</c>), so the noon of <c>PlainDate(-271821, 4, 19)</c>, the
    /// limits' PlainDateTime and <c>PlainYearMonth(-271821, 4)</c> lie below TimeClip's -8.64e15 and are written as Broiler.VM
    /// writes them; a Number below -8.64e15 is HandleDateTimeValue's <see cref="IntlStatus.TimeValueOutOfRange"/> first.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=proposal-temporal e8cc03fc ISOYearMonthWithinLimits and HandleDateTimeValue, JSD-0082 row T38; IP=Low; Security=Medium; Resources=0; Fingerprint=550714
    // Broiler-Human:        PENDING
    public const double MinTime = -8_640_001_641_600_000;

    /// <summary>
    /// The greatest time value the formatter writes: +275760-09-30T23:59:59.999Z, the last of the latest month a Temporal value
    /// holds; as <see cref="MinTime"/>, above TimeClip's 8.64e15 for the limits' Temporal values only.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=proposal-temporal e8cc03fc ISOYearMonthWithinLimits and HandleDateTimeValue, JSD-0082 row T38; IP=Low; Security=Medium; Resources=0; Fingerprint=203E8A
    // Broiler-Human:        PENDING
    public const double MaxTime = 8_640_001_555_199_999;

    /// <summary>The pattern a single time value is written in, in CLDR's pattern syntax (UTS #35 Part 4, Date Format Patterns).</summary>
    public string Pattern { get; }

    /// <summary>FormatDateTime (<c>sec-formatdatetime</c>): the String the parts of <paramref name="time"/> make.</summary>
    /// <param name="time">An integral time value within the ISO dates of Temporal's values (see <see cref="MinTime"/> and
    /// <see cref="MaxTime"/>): a Number TimeClip keeps (<c>sec-timeclip</c>), or a time HandleDateTimeValue writes for a Temporal
    /// value, which it does not clip (<see cref="DateTimeFormat.HandleDateTimeValue"/>).</param>
    /// <param name="charge">The caller's meter: the String's length in code units (decision D18).</param>
    /// <returns>The formatted String.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="time"/> is not integral or is outside those dates: no value
    /// HandleDateTimeValue completes for writes it, the engine's <c>RangeError</c> for a Number TimeClip refuses coming first.</exception>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-formatdatetimepattern sec-partitiondatetimepattern UTS35 Part 4 section 8 via Broiler.VM JsDateTimeFormatter; IP=Low; Security=Medium; Resources=2; Fingerprint=CEE738
    // Broiler-Human:        PENDING
    public string Format(double time, WorkCharge? charge = null) => Joined(FormatToParts(time, charge));

    /// <summary>FormatDateTimeToParts (<c>sec-formatdatetimetoparts</c>): the parts of <paramref name="time"/>.</summary>
    /// <param name="time">As <see cref="Format"/>'s.</param>
    /// <param name="charge">The caller's meter: the formatted String's length in code units (decision D18); an engine's
    /// per-part charge for the array it builds is its own.</param>
    /// <returns>The parts, in order, each with no source.</returns>
    /// <exception cref="ArgumentOutOfRangeException">As <see cref="Format"/>'s.</exception>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-formatdatetimepattern sec-partitiondatetimepattern UTS35 Part 4 section 8 via Broiler.VM JsDateTimeFormatter; IP=Low; Security=Medium; Resources=2; Fingerprint=D67D3C
    // Broiler-Human:        PENDING
    public IReadOnlyList<DateTimePart> FormatToParts(double time, WorkCharge? charge = null)
    {
        CheckTime(time, nameof(time));
        return Parts(time, null, charge).AsReadOnly();
    }

    /// <summary>FormatDateTimeRange (<c>sec-formatdatetimerange</c>): the String the parts of the range make.</summary>
    /// <param name="x">The start, a time value as <see cref="Format"/> takes.</param>
    /// <param name="y">The end, a time value as <see cref="Format"/> takes.</param>
    /// <param name="charge">The caller's meter: the range's length in code units, and where the two dates collapse to one, the one
    /// date's as well, as Broiler.VM charges its meter (decision D18).</param>
    /// <returns>The formatted String.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="x"/> or <paramref name="y"/> is not a time value <see cref="Format"/> takes.</exception>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-formatdatetimepattern sec-partitiondatetimepattern UTS35 Part 4 section 8 via Broiler.VM JsDateTimeFormatter; IP=Low; Security=Medium; Resources=2; Fingerprint=99C992
    // Broiler-Human:        PENDING
    public string FormatRange(double x, double y, WorkCharge? charge = null) => Joined(FormatRangeToParts(x, y, charge));

    /// <summary>
    /// FormatDateTimeRangeToParts (<c>sec-formatdatetimerangetoparts</c>): the parts of the range from <paramref name="x"/> to
    /// <paramref name="y"/>, each with its source.
    /// </summary>
    /// <param name="x">The start, a time value as <see cref="Format"/> takes.</param>
    /// <param name="y">The end, a time value as <see cref="Format"/> takes.</param>
    /// <param name="charge">As <see cref="FormatRange"/>'s.</param>
    /// <returns>The parts.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="x"/> or <paramref name="y"/> is not a time value <see cref="Format"/> takes.</exception>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-formatdatetimepattern sec-partitiondatetimepattern UTS35 Part 4 section 8 via Broiler.VM JsDateTimeFormatter; IP=Low; Security=Medium; Resources=2; Fingerprint=8D1DC1
    // Broiler-Human:        PENDING
    public IReadOnlyList<DateTimePart> FormatRangeToParts(double x, double y, WorkCharge? charge = null)
    {
        CheckTime(x, nameof(x));
        CheckTime(y, nameof(y));
        return RangeParts(x, y, charge).AsReadOnly();
    }

    /// <summary>The text of a list of parts.</summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-formatdatetimepattern sec-partitiondatetimepattern UTS35 Part 4 section 8 via Broiler.VM JsDateTimeFormatter; IP=Low; Security=Low; Resources=1; Fingerprint=6B03E2
    // Broiler-Human:        PENDING
    internal static string Joined(IReadOnlyList<DateTimePart> parts)
    {
        var text = new StringBuilder();

        foreach (var part in parts)
        {
            text.Append(part.Value);
        }

        return text.ToString();
    }

    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-formatdatetimepattern sec-partitiondatetimepattern UTS35 Part 4 section 8 via Broiler.VM JsDateTimeFormatter; IP=Low; Security=Medium; Resources=1; Fingerprint=7CFB43
    // Broiler-Human:        PENDING
    private static void CheckTime(double time, string name)
    {
        if (double.IsNaN(time) || time < MinTime || time > MaxTime || Math.Truncate(time) != time)
        {
            throw new ArgumentOutOfRangeException(name, "a date-time format writes an integral time value within the ISO dates of Temporal's values");
        }
    }

    /// <summary>The parts of <paramref name="time"/>, an integral time value.</summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-formatdatetimepattern sec-partitiondatetimepattern UTS35 Part 4 section 8 via Broiler.VM JsDateTimeFormatter; IP=Low; Security=Medium; Resources=2; Fingerprint=08B4EA
    // Broiler-Human:        PENDING
    internal List<DateTimePart> Parts(double time, string? source, WorkCharge? charge)
    {
        var text = new StringBuilder();
        var fields = new List<DateTimeField>();
        FormatPattern(Pattern, LocalTime(time), text, fields);
        charge?.Invoke((ulong)text.Length);
        return ToParts(text.ToString(), fields, source is null ? null : (_, _) => source);
    }

    /// <summary>
    /// The parts of the range from <paramref name="x"/> to <paramref name="y"/>: the range ICU writes,
    /// its parts' sources from the spans of the fields it repeats, or <paramref name="x"/> alone,
    /// shared, where it writes one date.
    /// </summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-formatdatetimepattern sec-partitiondatetimepattern UTS35 Part 4 section 8 via Broiler.VM JsDateTimeFormatter; IP=Low; Security=High; Resources=2; Fingerprint=D598B5
    // Broiler-Falsified-If: a range's parts differ from PartitionDateTimeRangePattern's, or its charge from the output's length
    // Broiler-Human:        PENDING
    internal List<DateTimePart> RangeParts(double x, double y, WorkCharge? charge)
    {
        var text = new StringBuilder();
        var fields = new List<DateTimeField>();
        var first = interval.Value.Format(LocalTime(x), LocalTime(y), this, text, fields);
        charge?.Invoke((ulong)text.Length);

        // THE SPANS ARE ICU'S: the first and second occurrences of each repeated field.
        int s1a = int.MaxValue, s1b = 0, s2a = int.MaxValue, s2b = 0;

        for (var i = 0; first >= 0 && i < fields.Count; i++)
        {
            for (var j = i + 1; j < fields.Count; j++)
            {
                if (fields[i].Letter != fields[j].Letter)
                {
                    continue;
                }

                s1a = Math.Min(s1a, fields[i].Start);
                s1b = Math.Max(s1b, fields[i].End);
                s2a = Math.Min(s2a, fields[j].Start);
                s2b = Math.Max(s2b, fields[j].End);
                break;
            }
        }

        if (first < 0 || s1a == int.MaxValue)
        {
            return Parts(x, "shared", charge);
        }

        var (startFrom, startTo, endFrom, endTo) = first == 0 ? (s1a, s1b, s2a, s2b) : (s2a, s2b, s1a, s1b);

        return ToParts(text.ToString(), fields, (start, end) =>
            startFrom <= start && end <= startTo ? "startRange" :
            endFrom <= start && end <= endTo ? "endRange" :
            "shared");
    }

    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-formatdatetimepattern sec-partitiondatetimepattern UTS35 Part 4 section 8 via Broiler.VM JsDateTimeFormatter; IP=Low; Security=Medium; Resources=2; Fingerprint=B1228A
    // Broiler-Human:        PENDING
    private static List<DateTimePart> ToParts(
        string text,
        List<DateTimeField> fields,
        Func<int, int, string>? source)
    {
        var parts = new List<DateTimePart>(fields.Count * 2);
        var previous = 0;

        foreach (var field in fields)
        {
            if (field.Start > previous)
            {
                parts.Add(new DateTimePart("literal", text[previous..field.Start], source?.Invoke(previous, field.Start)));
            }

            parts.Add(new DateTimePart(TypeOf(field.Letter), text[field.Start..field.End], source?.Invoke(field.Start, field.End)));
            previous = field.End;
        }

        if (text.Length > previous)
        {
            parts.Add(new DateTimePart("literal", text[previous..], source?.Invoke(previous, text.Length)));
        }

        return parts;
    }

    /// <summary>The ECMA-402 part type of a pattern letter.</summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-formatdatetimepattern sec-partitiondatetimepattern UTS35 Part 4 section 8 via Broiler.VM JsDateTimeFormatter; IP=Low; Security=Low; Resources=1; Fingerprint=5B6CC9
    // Broiler-Human:        PENDING
    internal static string TypeOf(char letter) => letter switch
    {
        'G' => "era",
        'y' or 'Y' or 'u' => "year",
        'U' => "yearName",
        'r' => "relatedYear",
        'M' or 'L' => "month",
        'd' => "day",
        'E' or 'c' or 'e' => "weekday",
        'a' or 'b' or 'B' => "dayPeriod",
        'h' or 'H' or 'k' or 'K' => "hour",
        'm' => "minute",
        's' => "second",
        'S' => "fractionalSecond",
        'z' or 'Z' or 'O' or 'v' or 'V' or 'X' or 'x' => "timeZoneName",
        _ => "unknown",
    };

    /// <summary>Writes <paramref name="pattern"/> for <paramref name="time"/>, recording each field it writes.</summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-formatdatetimepattern sec-partitiondatetimepattern UTS35 Part 4 section 8 via Broiler.VM JsDateTimeFormatter; IP=Low; Security=High; Resources=2; Fingerprint=EF47EB
    // Broiler-Falsified-If: a pattern's String differs from FormatDateTimePattern's for its fields read as UTS 35 Part 4 section 8 states
    // Broiler-Human:        PENDING
    internal void FormatPattern(string pattern, LocalDateTime time, StringBuilder text, List<DateTimeField> fields)
    {
        var hasMinute = false;
        var hasSecond = false;
        var quoted = false;

        foreach (var c in pattern)
        {
            if (c == '\'')
            {
                quoted = !quoted;
            }
            else if (!quoted)
            {
                hasMinute |= c == 'm';
                hasSecond |= c == 's';
            }
        }

        var at = 0;

        while (at < pattern.Length)
        {
            var c = pattern[at];

            if (c == '\'')
            {
                if (at + 1 < pattern.Length && pattern[at + 1] == '\'')
                {
                    text.Append('\'');
                    at += 2;
                    continue;
                }

                at++;

                while (at < pattern.Length)
                {
                    if (pattern[at] == '\'')
                    {
                        if (at + 1 < pattern.Length && pattern[at + 1] == '\'')
                        {
                            text.Append('\'');
                            at += 2;
                            continue;
                        }

                        at++;
                        break;
                    }

                    text.Append(pattern[at]);
                    at++;
                }

                continue;
            }

            if (!DateTimePatternGenerator.IsLetter(c))
            {
                text.Append(c);
                at++;
                continue;
            }

            var count = 1;

            while (at + count < pattern.Length && pattern[at + count] == c)
            {
                count++;
            }

            var start = text.Length;

            if (Field(c, count, time, hasMinute, hasSecond, text))
            {
                fields.Add(new DateTimeField(c, start, text.Length));
            }

            at += count;
        }
    }

    /// <summary>Writes one field; whether the letter is one this formatter writes.</summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-formatdatetimepattern sec-partitiondatetimepattern UTS35 Part 4 section 8 via Broiler.VM JsDateTimeFormatter; IP=Low; Security=High; Resources=2; Fingerprint=A72821
    // Broiler-Falsified-If: a pattern field's text differs from CLDR's name or digits for the field, width and local time
    // Broiler-Human:        PENDING
    private bool Field(char c, int count, LocalDateTime time, bool hasMinute, bool hasSecond, StringBuilder text)
    {
        switch (c)
        {
            case 'G':
                text.Append(Name("eras." + (count <= 3 ? "eraAbbr" : count == 4 ? "eraNames" : "eraNarrow") + "." + time.Era));
                return true;
            case 'y':
                var year = time.CyclicYear ?? time.EraYear;
                Number(text, count == 2 ? year % 100 : year, count);
                return true;
            case 'Y' or 'u' or 'r':
                Number(text, c == 'Y' ? time.EraYear : time.Year, count);
                return true;
            case 'U':
                text.Append(time.CyclicYear is { } cyclic
                    ? Name("cyclic.years." + (count <= 3 ? "abbreviated" : count == 4 ? "wide" : "narrow") + "." + cyclic.ToString(NumberFormatInfo.InvariantInfo))
                    : time.Year.ToString(NumberFormatInfo.InvariantInfo));
                return true;
            case 'M' or 'L':
                var context = c == 'M' ? "format" : "stand-alone";
                var monthStart = text.Length;

                if (count <= 2)
                {
                    Number(text, time.MonthNumber ?? time.Month, count);
                    Leap(text, monthStart, "monthPatterns.numeric.all.leap", time);
                }
                else
                {
                    var key = time.MonthKey ?? time.Month.ToString(NumberFormatInfo.InvariantInfo);
                    text.Append(Name("months." + context + "." + Width(count) + "." + key));
                    Leap(text, monthStart, "monthPatterns." + context + "." + Width(count) + ".leap", time);
                }

                return true;
            case 'd':
                Number(text, time.Day, count);
                return true;
            case 'D':
                Number(text, time.DayOfYear, count);
                return true;
            case 'F':
                Number(text, ((time.Day - 1) / 7) + 1, count);
                return true;
            case 'E' or 'c' or 'e':
                if (c != 'E' && count <= 2)
                {
                    Number(text, time.Weekday + 1, count);
                }
                else
                {
                    var width = count switch { <= 3 => "abbreviated", 4 => "wide", 5 => "narrow", _ => "short" };
                    text.Append(Name("days." + (c == 'c' ? "stand-alone" : "format") + "." + width + "." + DayKeys[time.Weekday]));
                }

                return true;
            case 'a':
                text.Append(Name("dayPeriods.format." + Width(count) + "." + (time.Hour < 12 ? "am" : "pm")));
                return true;
            case 'b' or 'B':
                text.Append(FlexibleDayPeriod(c, count, time, hasMinute, hasSecond));
                return true;
            case 'h':
                Number(text, time.Hour % 12 == 0 ? 12 : time.Hour % 12, count);
                return true;
            case 'H':
                Number(text, time.Hour, count);
                return true;
            case 'K':
                Number(text, time.Hour % 12, count);
                return true;
            case 'k':
                Number(text, time.Hour == 0 ? 24 : time.Hour, count);
                return true;
            case 'm':
                Number(text, time.Minute, count);
                return true;
            case 's':
                Number(text, time.Second, count);
                return true;
            case 'S':
                Number(text, count switch { 1 => time.Millisecond / 100, 2 => time.Millisecond / 10, _ => time.Millisecond }, Math.Min(count, 3));

                for (var extra = 3; extra < count; extra++)
                {
                    text.Append(digits[0]);
                }

                return true;
            case 'Q' or 'q':
                Number(text, ((time.Month - 1) / 3) + 1, count);
                return true;
            case 'z' or 'Z' or 'O' or 'v' or 'V' or 'X' or 'x':
                text.Append(ZoneName(c, count, time.Utc));
                return true;
            default:
                return false;
        }
    }

    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-formatdatetimepattern sec-partitiondatetimepattern UTS35 Part 4 section 8 via Broiler.VM JsDateTimeFormatter; IP=Low; Security=Low; Resources=1; Fingerprint=0249F3
    // Broiler-Human:        PENDING
    private static string Width(int count) => count switch { <= 3 => "abbreviated", 4 => "wide", _ => "narrow" };

    /// <summary>A leap month's text written from <paramref name="start"/> on, put in CLDR's month pattern for it.</summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-formatdatetimepattern sec-partitiondatetimepattern UTS35 Part 4 section 8 via Broiler.VM JsDateTimeFormatter; IP=Low; Security=Low; Resources=1; Fingerprint=1FAD43
    // Broiler-Human:        PENDING
    private void Leap(StringBuilder text, int start, string patternKey, LocalDateTime time)
    {
        if (!time.LeapMonth || data.Value(language, patternKey) is not { } pattern)
        {
            return;
        }

        var month = text.ToString(start, text.Length - start);
        text.Length = start;
        text.Append(pattern.Replace("{0}", month, StringComparison.Ordinal));
    }

    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-formatdatetimepattern sec-partitiondatetimepattern UTS35 Part 4 section 8 via Broiler.VM JsDateTimeFormatter; IP=Low; Security=Low; Resources=1; Fingerprint=F3F6FF
    // Broiler-Human:        PENDING
    private string Name(string key) => data.Value(language, key) ?? string.Empty;

    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-formatdatetimepattern sec-partitiondatetimepattern UTS35 Part 4 section 8 via Broiler.VM JsDateTimeFormatter; IP=Low; Security=Low; Resources=1; Fingerprint=C10E0A
    // Broiler-Human:        PENDING
    private void Number(StringBuilder text, long value, int minimum)
    {
        var plain = Math.Abs(value).ToString(NumberFormatInfo.InvariantInfo);

        if (value < 0)
        {
            text.Append('-');
        }

        for (var pad = plain.Length; pad < minimum; pad++)
        {
            text.Append(digits[0]);
        }

        foreach (var c in plain)
        {
            text.Append(digits[c - '0']);
        }
    }

    /// <summary>
    /// ICU's flexible day period (<c>B</c>) and its noon form (<c>b</c>): noon and midnight at their
    /// exact time where the language names them, midnight written as the period it falls in, and the
    /// language's period for the hour; AM or PM where none is named.
    /// </summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-formatdatetimepattern sec-partitiondatetimepattern UTS35 Part 4 section 8 via Broiler.VM JsDateTimeFormatter; IP=Low; Security=High; Resources=2; Fingerprint=DC97EB
    // Broiler-Falsified-If: a day period's name differs from CLDR's dayPeriodRules period for the locale and minute of the day
    // Broiler-Human:        PENDING
    private string FlexibleDayPeriod(char c, int count, LocalDateTime time, bool hasMinute, bool hasSecond)
    {
        var minute = hasMinute ? time.Minute : 0;
        var second = hasSecond ? time.Second : 0;
        var width = Width(count);
        var amPm = Name("dayPeriods.format." + width + "." + (time.Hour < 12 ? "am" : "pm"));

        // A CALENDAR'S DATA IS ITS LANGUAGE'S FOR DAY PERIODS, which belong to no calendar (JSD-0055).
        var rulesLanguage = DateData.LanguageOf(language);

        if (data.DayPeriods(rulesLanguage) is not { } rules)
        {
            return amPm;
        }

        bool Has(string period)
        {
            foreach (var rule in rules)
            {
                if (rule.Period == period)
                {
                    return true;
                }
            }

            return false;
        }

        if (c == 'b')
        {
            return time.Hour == 12 && minute == 0 && second == 0 && Has("noon") && data.Value(language, "dayPeriods.format." + width + ".noon") is { } noon
                ? noon
                : amPm;
        }

        var period = time.Hour == 12 && minute == 0 && second == 0 && Has("noon") ? "noon" : null;

        if (period is null)
        {
            var at = time.Hour * 60;

            foreach (var rule in rules)
            {
                if (!rule.At && rule.From <= at && at < rule.Before)
                {
                    period = rule.Period;
                    break;
                }
            }
        }

        if (period is null or "am" or "pm")
        {
            return amPm;
        }

        return data.Value(language, "dayPeriods.format." + width + "." + period) ?? amPm;
    }

    /// <summary>
    /// A zone field: CLDR's name for UTC or GMT, or an IANA zone's specific or generic name (JSD-0058),
    /// where the field asks for one, otherwise CLDR's GMT format. The GMT zone's name is its metazone's,
    /// which ICU maps only from 1970-01-01T00:00Z to before 9999-12-31T23:59Z; outside those bounds the
    /// zone has no name and the GMT format serves.
    /// </summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-formatdatetimepattern sec-partitiondatetimepattern UTS35 Part 4 section 8 via Broiler.VM JsDateTimeFormatter; IP=Low; Security=High; Resources=2; Fingerprint=726D96
    // Broiler-Falsified-If: a zone's name differs from UTS 35 Part 4 section 7's for its primary identifier, width and instant
    // Broiler-Human:        PENDING
    private string ZoneName(char c, int count, long utc)
    {
        var metazone = ZoneKind == TimeZoneKind.Gmt && utc >= 0 && utc < 253_402_300_740_000L;

        string? Named(string width) => ZoneKind switch
        {
            TimeZoneKind.Utc => data.Value(language, "zone.utc." + width),
            TimeZoneKind.Gmt when metazone => data.Value(language, "zone.gmt." + width),
            TimeZoneKind.Named when naming is not null && Zone is not null => naming.Names.Specific(data, language, NamingZone, Zone, utc, width == "long"),
            _ => null,
        };

        string? Generic(string width) => ZoneKind switch
        {
            TimeZoneKind.Gmt when metazone => data.Value(language, "zone.gmt." + width),
            TimeZoneKind.Named when naming is not null && Zone is not null =>
                naming.Names.Generic(data, language, naming.Region, naming.RegionName, NamingZone, Zone, utc, width == "long"),
            _ => null,
        };

        var offset = OffsetSecondsAt(utc);

        return c switch
        {
            'z' => (count < 4 ? Named("short") ?? Gmt(offset, longForm: false) : Named("long") ?? Gmt(offset, longForm: true)),
            'v' => (count < 4 ? Generic("short") ?? Gmt(offset, longForm: false) : Generic("long") ?? Gmt(offset, longForm: true)),
            'O' => Gmt(offset, longForm: count >= 4),
            'Z' => count == 4 ? Gmt(offset, longForm: true) : count == 5 ? Iso(offset, extended: true, zulu: true) : Iso(offset, extended: false, zulu: false),
            'X' => Iso(offset, extended: count >= 3, zulu: true),
            'x' => Iso(offset, extended: count >= 3, zulu: false),
            _ => count == 2 ? NamingZone : Gmt(offset, longForm: true),
        };
    }

    /// <summary>
    /// CLDR's localized GMT format of the offset, long (<c>GMT+05:30</c>) or short (<c>GMT+5:30</c>),
    /// with its seconds after the minutes where it has any, as ICU writes a local mean time.
    /// </summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-formatdatetimepattern sec-partitiondatetimepattern UTS35 Part 4 section 8 via Broiler.VM JsDateTimeFormatter; IP=Low; Security=Medium; Resources=1; Fingerprint=49FB53
    // Broiler-Human:        PENDING
    private string Gmt(int offsetSeconds, bool longForm)
    {
        if (offsetSeconds == 0)
        {
            return Name("zone.gmtZeroFormat");
        }

        var formats = Name("zone.hourFormat").Split(';');
        var format = offsetSeconds > 0 || formats.Length < 2 ? formats[0] : formats[1];
        var hours = Math.Abs(offsetSeconds) / 3600;
        var minutes = Math.Abs(offsetSeconds) / 60 % 60;
        var seconds = Math.Abs(offsetSeconds) % 60;
        var offset = new StringBuilder();
        var at = 0;

        while (at < format.Length)
        {
            var c = format[at];
            var count = 1;

            while (at + count < format.Length && format[at + count] == c)
            {
                count++;
            }

            if (c == 'H')
            {
                Number(offset, hours, longForm ? count : 1);
            }
            else if (c == 'm')
            {
                if (longForm || minutes != 0 || seconds != 0)
                {
                    Number(offset, minutes, count);

                    if (seconds != 0)
                    {
                        offset.Append(':');
                        Number(offset, seconds, 2);
                    }
                }
            }
            else if (!longForm && minutes == 0 && seconds == 0 && at + count < format.Length && format[at + count] == 'm')
            {
                // THE SEPARATOR BEFORE THE MINUTES GOES WITH THEM in the short form.
            }
            else
            {
                offset.Append(c, count);
            }

            at += count;
        }

        return DateTimePatternGenerator.Substitute(Name("zone.gmtFormat"), offset.ToString(), string.Empty, string.Empty);
    }

    /// <summary>An ISO 8601 offset: <c>+0530</c>, or <c>+05:30</c> extended, or <c>Z</c> for zero where asked.</summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-formatdatetimepattern sec-partitiondatetimepattern UTS35 Part 4 section 8 via Broiler.VM JsDateTimeFormatter; IP=Low; Security=Medium; Resources=1; Fingerprint=F16AE8
    // Broiler-Human:        PENDING
    private static string Iso(int offsetSeconds, bool extended, bool zulu)
    {
        var offsetMinutes = offsetSeconds / 60;

        if (offsetMinutes == 0 && zulu)
        {
            return "Z";
        }

        var hours = Math.Abs(offsetMinutes) / 60;
        var minutes = Math.Abs(offsetMinutes) % 60;
        return (offsetMinutes < 0 ? "-" : "+") +
            hours.ToString("00", NumberFormatInfo.InvariantInfo) +
            (extended ? ":" : string.Empty) +
            minutes.ToString("00", NumberFormatInfo.InvariantInfo);
    }
}
