// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   56
// Annotated:        56/56
// Exempt:           47
// Human-reviewed:   0/56
// IP risk:          Low
// Security risk:    High
// Criteria:         36/36
// Resource impact:  3/10 max
// Unverified:       56
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using Broiler.ECMAScript.Numbers;
using Broiler.ECMAScript.Time;

namespace Broiler.ECMAScript.Temporal;

/// <summary>
/// The goal symbols of the Temporal proposal's ISO 8601 / RFC 9557 grammar (<c>#sec-temporal-iso8601grammar</c>) a
/// string is parsed as by <see cref="TemporalGrammar.TryParse"/>.
/// </summary>
// Broiler-AI:           Origin=Ported; Spec=Temporal ISO 8601 grammar goal symbols via Broiler.VM TemporalGoal; IP=Low; Security=None; Resources=0; Fingerprint=C434CF
// Broiler-Human:        PENDING
public enum TemporalGoal
{
    /// <summary><c>TemporalDateTimeString[~Zoned]</c>: a date, with an optional time and offset, no <c>Z</c>; the date, date-time and <c>relativeTo</c> forms.</summary>
    DateTime = 0,

    /// <summary><c>TemporalDateTimeString[+Zoned]</c>: a date-time whose time zone annotation is required, <c>Z</c> allowed.</summary>
    ZonedDateTime = 1,

    /// <summary><c>TemporalInstantString</c>: a date, a time, and <c>Z</c> or an offset.</summary>
    Instant = 2,

    /// <summary><c>TemporalTimeString</c>: a time alone, or a date-time with a time.</summary>
    Time = 3,

    /// <summary><c>TemporalYearMonthString</c>: a year and month alone, or a date.</summary>
    YearMonth = 4,

    /// <summary><c>TemporalMonthDayString</c>: a month and day alone, or a date.</summary>
    MonthDay = 5,
}

/// <summary>
/// Why a Temporal string was refused (JSD-0078's X3: the grammar answers a record or a code). Each engine maps every
/// code to its own <c>RangeError</c>, as the specification throws one for each; the code names the step.
/// </summary>
// Broiler-AI:           Origin=AI; Spec=JSD-0078 X3, Temporal ParseISODateTime; IP=Low; Security=None; Resources=0; Fingerprint=05B5D2
// Broiler-Human:        PENDING
public enum TemporalParseError
{
    /// <summary>The string was accepted.</summary>
    None = 0,

    /// <summary>The string is not one of the goals' (<c>ParseISODateTime</c>'s last step), not a duration, or not a calendar string or an annotation value.</summary>
    Malformed = 1,

    /// <summary>Two calendar annotations, one of them critical (<c>ParseISODateTime</c>, the <c>u-ca</c> step).</summary>
    CriticalCalendarConflict = 2,

    /// <summary>A critical annotation whose key is not <c>u-ca</c>.</summary>
    UnknownCriticalAnnotation = 3,

    /// <summary>A year-month string without a day whose calendar annotation is not <c>iso8601</c>.</summary>
    YearMonthOutsideIsoCalendar = 4,

    /// <summary>A month-day string without a year whose calendar annotation is not <c>iso8601</c>.</summary>
    MonthDayOutsideIsoCalendar = 5,

    /// <summary>An instant string outside the instant range, or more than 10^8 days from the epoch (<c>ParseTemporalInstant</c>).</summary>
    OutOfRange = 6,

    /// <summary>A time zone string with no annotation, no <c>Z</c> and no offset (<c>ParseTemporalTimeZoneString</c>'s last step).</summary>
    NoTimeZone = 7,

    /// <summary>A time zone string whose only zone is an offset with seconds (<c>ParseTimeZoneIdentifier</c> of the offset).</summary>
    SubMinuteTimeZoneOffset = 8,
}

/// <summary>A key-value annotation of an RFC 9557 string, in the string's order: <c>[u-ca=gregory]</c>, <c>[!foo=bar]</c>.</summary>
/// <param name="Critical">Whether the annotation carries the critical flag <c>!</c>.</param>
/// <param name="Key">The key, the grammar's <c>AnnotationKey</c>.</param>
/// <param name="Value">The value, the grammar's <c>AnnotationValue</c>.</param>
// Broiler-AI:           Origin=Ported; Spec=Temporal Annotation via Broiler.VM JsTemporalParser.annotations; IP=Low; Security=None; Resources=0; Fingerprint=8CD16E
// Broiler-Human:        PENDING
public readonly record struct TemporalAnnotation(bool Critical, string Key, string Value);

/// <summary>
/// A Time Zone Identifier Parse Record of the Temporal proposal: a named zone's name, not necessarily available, or an
/// offset zone's offset in minutes.
/// </summary>
/// <param name="Name">The name, or <see langword="null"/> for an offset time zone.</param>
/// <param name="OffsetMinutes">The offset in minutes, from -1439 to 1439, for an offset time zone; zero for a named one.</param>
// Broiler-AI:           Origin=Ported; Spec=Temporal Time Zone Identifier Parse Record via Broiler.VM JsTemporal.ParseZone; IP=Low; Security=None; Resources=0; Fingerprint=505851
// Broiler-Human:        PENDING
public readonly record struct TimeZoneIdentifierRecord(string? Name, int OffsetMinutes);

/// <summary>
/// An ISO Date-Time Parse Record of the Temporal proposal (<c>ParseISODateTime</c>), with the parse nodes an engine
/// reads beside it: the goal matched, the offset's value, and every key-value annotation.
/// </summary>
// Broiler-AI:           Origin=Ported; Spec=Temporal ISO Date-Time Parse Record via Broiler.VM JsTemporalParse; IP=Low; Security=Low; Resources=1; Fingerprint=F80F47
// Broiler-Human:        PENDING
public sealed class IsoParseResult
{
    // Creates the record from the parser's nodes.
    // Broiler-AI:           Origin=Ported; Spec=Temporal ISO Date-Time Parse Record via Broiler.VM JsTemporalParse; IP=Low; Security=Low; Resources=1; Fingerprint=37205E
    // Broiler-Human:        PENDING
    internal IsoParseResult(
        TemporalGoal goal,
        long? year,
        int month,
        int day,
        bool hasDay,
        IsoTime? time,
        bool z,
        string? offset,
        long offsetNanoseconds,
        bool offsetHasSeconds,
        string? timeZone,
        string? calendar,
        IReadOnlyList<TemporalAnnotation> annotations)
    {
        Goal = goal;
        Year = year;
        Month = month;
        Day = day;
        HasDay = hasDay;
        Time = time;
        Z = z;
        Offset = offset;
        OffsetNanoseconds = offsetNanoseconds;
        OffsetHasSeconds = offsetHasSeconds;
        TimeZone = timeZone;
        Calendar = calendar;
        Annotations = annotations;
    }

    /// <summary>The first goal of the caller's list the string matched.</summary>
    public TemporalGoal Goal { get; }

    /// <summary>The <c>[[Year]]</c>: the year, or <see langword="null"/> for a month-day string that omitted it, or a time string.</summary>
    public long? Year { get; }

    /// <summary>The <c>[[Month]]</c>, from 1 to 12; 1 where the string has none.</summary>
    public int Month { get; }

    /// <summary>The <c>[[Day]]</c>, from 1 to 31; 1 where the string has none.</summary>
    public int Day { get; }

    /// <summary>Whether the string held a <c>DateDay</c>: false for a year-month string without one.</summary>
    public bool HasDay { get; }

    /// <summary>The <c>[[Time]]</c>: the time, a second of 60 read as 59, or <see langword="null"/> for <c>start-of-day</c>.</summary>
    public IsoTime? Time { get; }

    /// <summary>The <c>[[TimeZone]].[[Z]]</c>: whether the string held the UTC designator <c>Z</c>.</summary>
    public bool Z { get; }

    /// <summary>The <c>[[TimeZone]].[[OffsetString]]</c>: the UTC offset's source text, sub-minute precision allowed, or <see langword="null"/>.</summary>
    public string? Offset { get; }

    /// <summary>The offset's value, the proposal's <c>ParseDateTimeUTCOffset</c>; zero where there is no offset.</summary>
    public long OffsetNanoseconds { get; }

    /// <summary>Whether the offset has a seconds part, which turns <c>ToTemporalZonedDateTime</c>'s minute matching off.</summary>
    public bool OffsetHasSeconds { get; }

    /// <summary>The <c>[[TimeZone]].[[TimeZoneAnnotation]]</c>: the time zone annotation's identifier, without its critical flag, or <see langword="null"/>.</summary>
    public string? TimeZone { get; }

    /// <summary>The <c>[[Calendar]]</c>: the first <c>u-ca</c> annotation's value, or <see langword="null"/>.</summary>
    public string? Calendar { get; }

    /// <summary>Every key-value annotation in the string's order, the <c>u-ca</c> ones and the unknown non-critical ones.</summary>
    public IReadOnlyList<TemporalAnnotation> Annotations { get; }
}

/// <summary>
/// The Temporal proposal's ISO 8601 / RFC 9557 grammar (<c>#sec-temporal-iso8601grammar</c>) and the operations that
/// parse with it: <c>ParseISODateTime</c> for every goal, <c>ParseTemporalInstant</c>,
/// <c>ParseTemporalTimeZoneString</c>, <c>ParseTimeZoneIdentifier</c>, <c>ParseTemporalCalendarString</c>,
/// <c>ToOffsetString</c>'s test and <c>ParseDateTimeUTCOffset</c>, and <c>ParseTemporalDurationString</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Broiler.VM's linear grammar, moved whole</b> (JSD-0078, the grammar's X6): a recursive-descent parser with one
/// method per production, each advancing over its text or restoring the position and failing, so that the
/// alternatives backtrack as the grammar's do. It refuses every case of the correspondence's rows G1 to G24 that
/// Broiler.VM refuses. Digits are ASCII only, as the grammar's <c>DecimalDigit</c>; no regular expression and no
/// platform culture enters.
/// </para>
/// <para>
/// <b>Failure is a code</b>: a <see cref="TemporalParseError"/>, which each engine maps to its <c>RangeError</c>. No
/// member reads an option or names an engine; <c>ToTemporal…</c>'s conversions, the calendar's canonicalisation and a
/// named zone's lookup stay the adapter's.
/// </para>
/// <para>
/// <b>The charge</b> (JSD-0078's X3, as SJL-6's Date parser charges): every member whose work grows with its input
/// takes the caller's <see cref="WorkCharge"/> and charges the text's length, in UTF-16 code units, once, before any
/// character is read, when the text is not empty, whatever the outcome. Both engines pass none today.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=Ported; Spec=Temporal ISO 8601 grammar via Broiler.VM JsTemporalParser; IP=Low; Security=High; Resources=3; Fingerprint=4DD523
// Broiler-Falsified-If: a string Broiler.VM's JsTemporalParser refuses is accepted for a goal, a string it accepts is refused, or an accepted string answers a field other than Broiler.VM's
// Broiler-Human:        PENDING
public static class TemporalGrammar
{
    /// <summary>
    /// Parses a string as the first of a list of goals it matches, and applies the annotation checks: the proposal's
    /// <c>ParseISODateTime</c>.
    /// </summary>
    /// <param name="text">The string, already converted.</param>
    /// <param name="goals">The goals, in the order the specification lists them for the operation.</param>
    /// <param name="result">The record; <see langword="null"/> unless the string is accepted.</param>
    /// <param name="error">Why the string was refused; <see cref="TemporalParseError.None"/> when it is accepted.</param>
    /// <param name="charge">The caller's meter, charged the text's length before it is read; <see langword="null"/> for none.</param>
    /// <returns>Whether the string is accepted.</returns>
    /// <remarks>
    /// The first goal the string matches decides: its annotations are checked - of several <c>u-ca</c> annotations the
    /// first is the calendar, and a critical one among several, or an unknown critical key, is refused - and a
    /// year-month without a day or a month-day without a year must be in <c>iso8601</c> where it names a calendar.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">A goal is not one of the enumeration's.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal ParseISODateTime via Broiler.VM JsTemporalParser.Parse; IP=Low; Security=High; Resources=3; Fingerprint=2B62FC
    // Broiler-Falsified-If: the meter is charged other than the text's length before the first character is read, a later goal is tried after an earlier one matched, or a calendar other than the first u-ca annotation's is answered
    // Broiler-Human:        PENDING
    public static bool TryParse(
        ReadOnlySpan<char> text,
        ReadOnlySpan<TemporalGoal> goals,
        [NotNullWhen(true)] out IsoParseResult? result,
        out TemporalParseError error,
        WorkCharge? charge = null)
    {
        foreach (var goal in goals)
        {
            if (goal is < TemporalGoal.DateTime or > TemporalGoal.MonthDay)
                throw new ArgumentOutOfRangeException(nameof(goals), goal, "A goal is not one of the enumeration's.");
        }

        Charge(text, charge);
        error = Parse(text, goals, out result);
        return error == TemporalParseError.None;
    }

    /// <summary>
    /// Parses an instant string into its epoch nanoseconds: the proposal's <c>ParseTemporalInstant</c> - the
    /// <c>TemporalInstantString</c> goal, its offset applied, the day range checked, and the result an instant.
    /// </summary>
    /// <param name="text">The string, already converted.</param>
    /// <param name="epochNanoseconds">The instant; zero unless the string is accepted.</param>
    /// <param name="error">Why the string was refused, <see cref="TemporalParseError.OutOfRange"/> for one outside the range.</param>
    /// <param name="charge">The caller's meter, charged the text's length before it is read; <see langword="null"/> for none.</param>
    /// <returns>Whether the string is accepted.</returns>
    // Broiler-AI:           Origin=Ported; Spec=Temporal ParseTemporalInstant via Broiler.VM JsTemporal.ToInstant; IP=Low; Security=High; Resources=3; Fingerprint=3765EF
    // Broiler-Falsified-If: an instant string within the range is refused, or one outside it, or more than 10^8 days from the epoch after its offset, is accepted
    // Broiler-Human:        PENDING
    public static bool TryParseInstant(ReadOnlySpan<char> text, out BigInteger epochNanoseconds, out TemporalParseError error, WorkCharge? charge = null)
    {
        Charge(text, charge);
        epochNanoseconds = BigInteger.Zero;
        error = Parse(text, [TemporalGoal.Instant], out var parsed);
        if (error != TemporalParseError.None)
            return false;

        var offset = parsed!.Z ? 0 : parsed.OffsetNanoseconds;
        var balanced = IsoArithmetic.BalanceIsoDateTime(
            new IsoDate(parsed.Year!.Value, parsed.Month, parsed.Day), IsoArithmetic.TimeToNanoseconds(parsed.Time!.Value) - offset);
        if (TemporalLimits.CheckIsoDaysRange(balanced.Date) != TemporalStatus.Completed)
        {
            error = TemporalParseError.OutOfRange;
            return false;
        }

        var instant = IsoArithmetic.GetUtcEpochNanoseconds(balanced);
        if (TemporalLimits.CheckEpochNanoseconds(instant) != TemporalStatus.Completed)
        {
            error = TemporalParseError.OutOfRange;
            return false;
        }

        epochNanoseconds = instant;
        return true;
    }

    /// <summary>
    /// Parses a time zone string: the proposal's <c>ParseTemporalTimeZoneString</c> - a time zone identifier, or the
    /// zone an ISO string names by its annotation, its <c>Z</c> (UTC) or its offset, in that order of precedence.
    /// </summary>
    /// <param name="text">The string, already converted.</param>
    /// <param name="zone">The zone's name, not yet looked up, or its offset in minutes; the default unless accepted.</param>
    /// <param name="error">Why the string was refused.</param>
    /// <param name="charge">The caller's meter, charged the text's length before it is read; <see langword="null"/> for none.</param>
    /// <returns>Whether the string names a zone. A name's availability is the adapter's to check (<c>GetAvailableNamedTimeZoneIdentifier</c>).</returns>
    // Broiler-AI:           Origin=Ported; Spec=Temporal ParseTemporalTimeZoneString via Broiler.VM JsTemporal.ToTimeZoneIdentifier; IP=Low; Security=High; Resources=3; Fingerprint=E00095
    // Broiler-Falsified-If: a string that is neither a time zone identifier nor an ISO string of one of the six goals names a zone, or the zone answered is not the annotation's, then UTC for Z, then the offset's
    // Broiler-Human:        PENDING
    public static bool TryParseTimeZoneString(ReadOnlySpan<char> text, out TimeZoneIdentifierRecord zone, out TemporalParseError error, WorkCharge? charge = null)
    {
        Charge(text, charge);
        zone = default;
        if (IsWholeTimeZoneIdentifier(text))
        {
            zone = ParseZone(text);
            error = TemporalParseError.None;
            return true;
        }

        error = Parse(text, AllGoals, out var parsed);
        if (error != TemporalParseError.None)
            return false;

        if (parsed!.TimeZone is { } annotation)
        {
            zone = ParseZone(annotation);
        }
        else if (parsed.Z)
        {
            zone = new TimeZoneIdentifierRecord("UTC", 0);
        }
        else if (parsed.Offset is { } offset)
        {
            if (!IsUtcOffset(offset, allowSubMinute: false))
            {
                error = TemporalParseError.SubMinuteTimeZoneOffset;
                return false;
            }

            zone = new TimeZoneIdentifierRecord(null, (int)(parsed.OffsetNanoseconds / 60_000_000_000L));
        }
        else
        {
            error = TemporalParseError.NoTimeZone;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Parses a time zone identifier: the proposal's <c>ParseTimeZoneIdentifier</c> of a string the grammar's
    /// <c>TimeZoneIdentifier</c> matches whole - an offset without seconds (<c>UTCOffset[~SubMinutePrecision]</c>) or
    /// a <c>TimeZoneIANAName</c>. The <c>ZonedDateTime</c> constructor reads its zone so.
    /// </summary>
    /// <param name="text">The identifier, already converted.</param>
    /// <param name="zone">The name, not yet looked up, or the offset in minutes; the default unless accepted.</param>
    /// <param name="charge">The caller's meter, charged the text's length before it is read; <see langword="null"/> for none.</param>
    /// <returns>Whether the whole string is a time zone identifier.</returns>
    // Broiler-AI:           Origin=Ported; Spec=Temporal ParseTimeZoneIdentifier via Broiler.VM JsTemporal.TimeZoneFromIdentifier; IP=Low; Security=High; Resources=3; Fingerprint=232199
    // Broiler-Falsified-If: a string the grammar's TimeZoneIdentifier does not match whole is accepted, or an offset identifier answers a name
    // Broiler-Human:        PENDING
    public static bool TryParseTimeZoneIdentifier(ReadOnlySpan<char> text, out TimeZoneIdentifierRecord zone, WorkCharge? charge = null)
    {
        Charge(text, charge);
        if (!IsWholeTimeZoneIdentifier(text))
        {
            zone = default;
            return false;
        }

        zone = ParseZone(text);
        return true;
    }

    /// <summary>
    /// Parses a calendar string: the proposal's <c>ParseTemporalCalendarString</c> - the calendar annotation of an ISO
    /// string of any of the six goals (<c>iso8601</c> where it has none), or else the whole string where it is an
    /// <c>AnnotationValue</c>. The identifier is not canonicalised: that is the calendar sub-area's.
    /// </summary>
    /// <param name="text">The string, already converted.</param>
    /// <param name="calendar">The calendar identifier as written; <see langword="null"/> unless accepted.</param>
    /// <param name="error">Why the string was refused: <see cref="TemporalParseError.Malformed"/>.</param>
    /// <param name="charge">The caller's meter, charged the text's length before it is read; <see langword="null"/> for none.</param>
    /// <returns>Whether the string is a calendar string.</returns>
    // Broiler-AI:           Origin=Ported; Spec=Temporal ParseTemporalCalendarString via Broiler.VM JsTemporal.ParseCalendarString; IP=Low; Security=High; Resources=3; Fingerprint=41C0C1
    // Broiler-Falsified-If: a string that neither ParseISODateTime over the six goals nor AnnotationValue accepts is accepted, or the calendar answered is not the first u-ca annotation's
    // Broiler-Human:        PENDING
    public static bool TryParseCalendarString(
        ReadOnlySpan<char> text, [NotNullWhen(true)] out string? calendar, out TemporalParseError error, WorkCharge? charge = null)
    {
        Charge(text, charge);
        if (Parse(text, AllGoals, out var parsed) == TemporalParseError.None)
        {
            calendar = parsed!.Calendar ?? "iso8601";
            error = TemporalParseError.None;
            return true;
        }

        if (IsAnnotationValueCore(text))
        {
            calendar = text.ToString();
            error = TemporalParseError.None;
            return true;
        }

        calendar = null;
        error = TemporalParseError.Malformed;
        return false;
    }

    /// <summary>
    /// Parses an offset string: the test of the proposal's <c>ToOffsetString</c> (the grammar's
    /// <c>UTCOffset[+SubMinutePrecision]</c>), and the offset's value, <c>ParseDateTimeUTCOffset</c>. <c>Z</c> is not an
    /// offset string (row G19).
    /// </summary>
    /// <param name="text">The string, already converted.</param>
    /// <param name="offsetNanoseconds">The offset in nanoseconds, of magnitude below a day; zero unless accepted.</param>
    /// <returns>Whether the whole string is a UTC offset.</returns>
    // Broiler-AI:           Origin=Ported; Spec=Temporal ToOffsetString and ParseDateTimeUTCOffset via Broiler.VM JsTemporal.OffsetString and JsTemporalParser.OffsetNanoseconds; IP=Low; Security=High; Resources=1; Fingerprint=880839
    // Broiler-Falsified-If: a string other than a whole UTCOffset with sub-minute precision is accepted, or an accepted offset answers other than its exact nanoseconds
    // Broiler-Human:        PENDING
    public static bool TryParseOffsetString(ReadOnlySpan<char> text, out long offsetNanoseconds)
    {
        offsetNanoseconds = 0;
        if (!IsUtcOffset(text, allowSubMinute: true))
            return false;

        offsetNanoseconds = OffsetNanoseconds(text);
        return true;
    }

    /// <summary>
    /// Parses a duration string: the proposal's <c>ParseTemporalDurationString</c>, each part exact and converted to a
    /// Number once, a fraction of an hour or a minute carried into the smaller units exactly. Validity is the caller's:
    /// <see cref="TemporalLimits.CheckDuration"/> is <c>CreateTemporalDuration</c>'s test.
    /// </summary>
    /// <param name="text">The string, already converted.</param>
    /// <param name="duration">The ten fields; the default unless accepted.</param>
    /// <param name="error">Why the string was refused: <see cref="TemporalParseError.Malformed"/>.</param>
    /// <param name="charge">The caller's meter, charged the text's length before it is read; <see langword="null"/> for none.</param>
    /// <returns>Whether the string is a <c>TemporalDurationString</c>.</returns>
    // Broiler-AI:           Origin=Ported; Spec=Temporal ParseTemporalDurationString via Broiler.VM JsTemporalParser.Duration; IP=Low; Security=High; Resources=3; Fingerprint=38C6AC
    // Broiler-Falsified-If: a string the TemporalDurationString grammar refuses is accepted (a time designator with no part, a fraction not on the last time part, parts out of order), or a part is not its exact value rounded once
    // Broiler-Human:        PENDING
    public static bool TryParseDuration(ReadOnlySpan<char> text, out DurationFields duration, out TemporalParseError error, WorkCharge? charge = null)
    {
        Charge(text, charge);
        if (Duration(text, out duration))
        {
            error = TemporalParseError.None;
            return true;
        }

        duration = default;
        error = TemporalParseError.Malformed;
        return false;
    }

    /// <summary>Whether a whole string is the grammar's <c>TimeZoneIdentifier</c>: an offset without seconds, or a <c>TimeZoneIANAName</c>.</summary>
    /// <param name="text">The string.</param>
    /// <param name="charge">The caller's meter, charged the text's length before it is read; <see langword="null"/> for none.</param>
    /// <returns>Whether it is one, whether or not such a zone is available.</returns>
    // Broiler-AI:           Origin=Ported; Spec=Temporal TimeZoneIdentifier via Broiler.VM JsTemporalParser.IsTimeZoneIdentifier; IP=Low; Security=High; Resources=3; Fingerprint=FADD2D
    // Broiler-Falsified-If: a string other than a whole UTCOffset without seconds or a TimeZoneIANAName is accepted
    // Broiler-Human:        PENDING
    public static bool IsTimeZoneIdentifier(ReadOnlySpan<char> text, WorkCharge? charge = null)
    {
        Charge(text, charge);
        return IsWholeTimeZoneIdentifier(text);
    }

    /// <summary>Whether a whole string is the grammar's <c>UTCOffset</c>, with or without sub-minute precision.</summary>
    /// <param name="text">The string.</param>
    /// <param name="allowSubMinute">Whether seconds and a fraction of a second may follow the minutes: <c>UTCOffset[+SubMinutePrecision]</c>.</param>
    /// <returns>Whether it is one: a sign, hours 00 to 23, and optionally minutes and seconds 00 to 59, all extended or all basic.</returns>
    // Broiler-AI:           Origin=Ported; Spec=Temporal UTCOffset via Broiler.VM JsTemporalParser.IsUtcOffset; IP=Low; Security=High; Resources=1; Fingerprint=1F7E11
    // Broiler-Falsified-If: a string other than a whole UTCOffset of the precision is accepted, including Z, U+2212 or a digit other than ASCII
    // Broiler-Human:        PENDING
    public static bool IsUtcOffset(ReadOnlySpan<char> text, bool allowSubMinute)
    {
        var parser = new Parser(text);
        return parser.UtcOffset(allowSubMinute, out _) && parser.At == text.Length;
    }

    /// <summary>Whether a string is the grammar's <c>AnnotationValue</c>: ASCII alphanumeric components joined by single hyphens.</summary>
    /// <param name="text">The string.</param>
    /// <param name="charge">The caller's meter, charged the text's length before it is read; <see langword="null"/> for none.</param>
    /// <returns>Whether it is one.</returns>
    // Broiler-AI:           Origin=Ported; Spec=Temporal AnnotationValue via Broiler.VM JsTemporalParser.IsAnnotationValue; IP=Low; Security=High; Resources=3; Fingerprint=E3B3B1
    // Broiler-Falsified-If: an empty string, an empty component or a character other than an ASCII letter, digit or separating hyphen is accepted
    // Broiler-Human:        PENDING
    public static bool IsAnnotationValue(ReadOnlySpan<char> text, WorkCharge? charge = null)
    {
        Charge(text, charge);
        return IsAnnotationValueCore(text);
    }

    // The six goals ParseTemporalCalendarString and ParseTemporalTimeZoneString try, in the specification's order.
    // Broiler-AI:           Origin=Ported; Spec=Temporal ParseTemporalCalendarString via Broiler.VM JsTemporal.ParseCalendarString; IP=Low; Security=Low; Resources=0; Fingerprint=FE8B4C
    // Broiler-Human:        PENDING
    private static ReadOnlySpan<TemporalGoal> AllGoals =>
        [TemporalGoal.ZonedDateTime, TemporalGoal.DateTime, TemporalGoal.Instant, TemporalGoal.Time, TemporalGoal.MonthDay, TemporalGoal.YearMonth];

    // The charge of convention 4: the text's length, before it is read, when it is not empty.
    // Broiler-AI:           Origin=Ported; Spec=JSD-0078 X3 via EcmaDateParser.Parse; IP=Low; Security=Medium; Resources=0; Fingerprint=694734
    // Broiler-Human:        PENDING
    private static void Charge(ReadOnlySpan<char> text, WorkCharge? charge)
    {
        if (charge is not null && text.Length > 0)
            charge((ulong)text.Length);
    }

    // ParseISODateTime over validated goals, uncharged.
    // Broiler-AI:           Origin=Ported; Spec=Temporal ParseISODateTime via Broiler.VM JsTemporalParser.Parse; IP=Low; Security=High; Resources=3; Fingerprint=DB3BE7
    // Broiler-Falsified-If: the annotation checks are applied to a goal other than the first matched, or the record's fields are not the matched parse nodes'
    // Broiler-Human:        PENDING
    private static TemporalParseError Parse(ReadOnlySpan<char> text, ReadOnlySpan<TemporalGoal> goals, out IsoParseResult? result)
    {
        result = null;
        var parser = new Parser(text);
        string? calendar = null;
        var yearAbsent = false;
        var matched = false;
        var matchedGoal = TemporalGoal.DateTime;

        foreach (var goal in goals)
        {
            matched = goal switch
            {
                TemporalGoal.DateTime => parser.AnnotatedDateTime(zoned: false, timeRequired: false, instant: false),
                TemporalGoal.ZonedDateTime => parser.AnnotatedDateTime(zoned: true, timeRequired: false, instant: false),
                TemporalGoal.Instant => parser.AnnotatedDateTime(zoned: false, timeRequired: true, instant: true),
                TemporalGoal.Time => parser.AnnotatedTime() || parser.AnnotatedDateTime(zoned: false, timeRequired: true, instant: false),
                TemporalGoal.YearMonth => parser.AnnotatedYearMonth() || parser.AnnotatedDateTime(zoned: false, timeRequired: false, instant: false),
                _ => parser.AnnotatedMonthDay() || parser.AnnotatedDateTime(zoned: false, timeRequired: false, instant: false),
            };

            if (!matched)
                continue;

            matchedGoal = goal;
            var calendarWasCritical = false;
            for (var i = 0; i < parser.AnnotationCount; i++)
            {
                var annotation = parser.AnnotationAt(i);
                if (text[annotation.KeyStart..annotation.KeyEnd].SequenceEqual("u-ca"))
                {
                    if (calendar is null)
                    {
                        calendar = text[annotation.ValueStart..annotation.ValueEnd].ToString();
                        calendarWasCritical = annotation.Critical;
                    }
                    else if (annotation.Critical || calendarWasCritical)
                    {
                        return TemporalParseError.CriticalCalendarConflict;
                    }
                }
                else if (annotation.Critical)
                {
                    return TemporalParseError.UnknownCriticalAnnotation;
                }
            }

            if (goal == TemporalGoal.YearMonth && parser.Day == 0 && calendar is not null && !IsIso8601(calendar))
                return TemporalParseError.YearMonthOutsideIsoCalendar;

            if (goal == TemporalGoal.MonthDay && !parser.HasYear)
            {
                if (calendar is not null && !IsIso8601(calendar))
                    return TemporalParseError.MonthDayOutsideIsoCalendar;

                yearAbsent = true;
            }

            break;
        }

        if (!matched)
            return TemporalParseError.Malformed;

        IsoTime? time = null;
        if (parser.Hour >= 0)
        {
            var fraction = 0L;
            if (parser.FractionStart >= 0)
            {
                foreach (var digit in text.Slice(parser.FractionStart, parser.FractionLength))
                    fraction = (fraction * 10) + (digit - '0');
                for (var i = parser.FractionLength; i < 9; i++)
                    fraction *= 10;
            }

            var second = parser.Second < 0 ? 0 : parser.Second;
            time = new IsoTime(
                parser.Hour,
                parser.Minute < 0 ? 0 : parser.Minute,
                second == 60 ? 59 : second,
                (int)(fraction / 1_000_000),
                (int)(fraction / 1000 % 1000),
                (int)(fraction % 1000));
        }

        string? offset = null;
        var offsetNanoseconds = 0L;
        if (parser.OffsetStart >= 0)
        {
            var offsetText = text[parser.OffsetStart..parser.OffsetEnd];
            offset = offsetText.ToString();
            offsetNanoseconds = OffsetNanoseconds(offsetText);
        }

        var annotations = parser.AnnotationCount == 0 ? Array.Empty<TemporalAnnotation>() : new TemporalAnnotation[parser.AnnotationCount];
        for (var i = 0; i < annotations.Length; i++)
        {
            var annotation = parser.AnnotationAt(i);
            annotations[i] = new TemporalAnnotation(
                annotation.Critical,
                text[annotation.KeyStart..annotation.KeyEnd].ToString(),
                text[annotation.ValueStart..annotation.ValueEnd].ToString());
        }

        result = new IsoParseResult(
            matchedGoal,
            yearAbsent || !parser.HasYear ? null : parser.Year,
            parser.Month == 0 ? 1 : parser.Month,
            parser.Day == 0 ? 1 : parser.Day,
            parser.Day != 0,
            time,
            parser.Z,
            offset,
            offsetNanoseconds,
            parser.OffsetHasSeconds,
            parser.ZoneStart >= 0 ? text[parser.ZoneStart..parser.ZoneEnd].ToString() : null,
            calendar,
            annotations);
        return TemporalParseError.None;
    }

    // ASCII-lowercase of a calendar is "iso8601": the annotation value is ASCII, so this is the specification's test.
    // Broiler-AI:           Origin=Ported; Spec=Temporal ParseISODateTime via Broiler.VM JsTemporalParser.Parse; IP=Low; Security=Low; Resources=0; Fingerprint=F3572B
    // Broiler-Human:        PENDING
    private static bool IsIso8601(string calendar)
    {
        const string Iso = "iso8601";
        if (calendar.Length != Iso.Length)
            return false;

        for (var i = 0; i < Iso.Length; i++)
        {
            var c = calendar[i];
            if ((c is >= 'A' and <= 'Z' ? (char)(c + 32) : c) != Iso[i])
                return false;
        }

        return true;
    }

    // The proposal's ParseTimeZoneIdentifier of a string the grammar's TimeZoneIdentifier matches: the offset branch is
    // SJL-8's offset identifier reading.
    // Broiler-AI:           Origin=Ported; Spec=Temporal ParseTimeZoneIdentifier via Broiler.VM JsTemporal.ParseZone; IP=Low; Security=Low; Resources=1; Fingerprint=79AE6D
    // Broiler-Human:        PENDING
    private static TimeZoneIdentifierRecord ParseZone(ReadOnlySpan<char> identifier) =>
        OffsetTimeZones.TryParseOffsetIdentifier(identifier, out var minutes)
            ? new TimeZoneIdentifierRecord(null, minutes)
            : new TimeZoneIdentifierRecord(identifier.ToString(), 0);

    // Whether the whole string is a TimeZoneIdentifier, uncharged.
    // Broiler-AI:           Origin=Ported; Spec=Temporal TimeZoneIdentifier via Broiler.VM JsTemporalParser.IsTimeZoneIdentifier; IP=Low; Security=High; Resources=3; Fingerprint=2684CF
    // Broiler-Falsified-If: a string other than a whole UTCOffset without seconds or a TimeZoneIANAName is accepted
    // Broiler-Human:        PENDING
    private static bool IsWholeTimeZoneIdentifier(ReadOnlySpan<char> text)
    {
        var parser = new Parser(text);
        return parser.TimeZoneIdentifier() && parser.At == text.Length;
    }

    // The grammar's AnnotationValue, uncharged.
    // Broiler-AI:           Origin=Ported; Spec=Temporal AnnotationValue via Broiler.VM JsTemporalParser.IsAnnotationValue; IP=Low; Security=High; Resources=3; Fingerprint=B98621
    // Broiler-Falsified-If: an empty string, an empty component or a character other than an ASCII letter, digit or separating hyphen is accepted
    // Broiler-Human:        PENDING
    private static bool IsAnnotationValueCore(ReadOnlySpan<char> value)
    {
        if (value.Length == 0)
            return false;

        var componentLength = 0;
        foreach (var c in value)
        {
            if (c == '-')
            {
                if (componentLength == 0)
                    return false;
                componentLength = 0;
            }
            else if (IsAlpha(c) || IsDigit(c))
            {
                componentLength++;
            }
            else
            {
                return false;
            }
        }

        return componentLength != 0;
    }

    // The grammar's AnnotationKey: a lower-case letter or underscore, then lower-case letters, digits, underscores and hyphens.
    // Broiler-AI:           Origin=Ported; Spec=Temporal AnnotationKey via Broiler.VM JsTemporalParser.IsAnnotationKey; IP=Low; Security=High; Resources=1; Fingerprint=4375B0
    // Broiler-Falsified-If: a key that is empty, starts other than with a lower-case ASCII letter or an underscore, or holds a character outside the key's set is accepted
    // Broiler-Human:        PENDING
    private static bool IsAnnotationKey(ReadOnlySpan<char> key)
    {
        if (key.Length == 0 || !(key[0] is (>= 'a' and <= 'z') or '_'))
            return false;

        foreach (var c in key)
        {
            if (!(c is (>= 'a' and <= 'z') or '_' or '-' || IsDigit(c)))
                return false;
        }

        return true;
    }

    // The proposal's ParseDateTimeUTCOffset of a string the grammar's UTCOffset[+SubMinutePrecision] matches: its nanoseconds.
    // Broiler-AI:           Origin=Ported; Spec=Temporal ParseDateTimeUTCOffset via Broiler.VM JsTemporalParser.OffsetNanoseconds; IP=Low; Security=Medium; Resources=1; Fingerprint=484859
    // Broiler-Human:        PENDING
    private static long OffsetNanoseconds(ReadOnlySpan<char> offset)
    {
        var sign = offset[0] == '-' ? -1L : 1L;
        Span<int> digits = stackalloc int[6];
        var count = 0;
        var fraction = 0L;
        var fractionDigits = 0;
        var inFraction = false;
        for (var i = 1; i < offset.Length; i++)
        {
            var c = offset[i];
            if (c is '.' or ',')
            {
                inFraction = true;
            }
            else if (IsDigit(c))
            {
                if (inFraction)
                {
                    if (fractionDigits < 9)
                    {
                        fraction = (fraction * 10) + (c - '0');
                        fractionDigits++;
                    }
                }
                else if (count < digits.Length)
                {
                    digits[count++] = c - '0';
                }
            }
        }

        for (; fractionDigits < 9; fractionDigits++)
            fraction *= 10;

        long hours = (digits[0] * 10) + digits[1];
        long minutes = count >= 4 ? (digits[2] * 10) + digits[3] : 0;
        long seconds = count >= 6 ? (digits[4] * 10) + digits[5] : 0;
        return sign * (((((hours * 60) + minutes) * 60) + seconds) * 1_000_000_000L + fraction);
    }

    // The proposal's ParseTemporalDurationString: the ten fields, exact, or false where the string is not a duration.
    // Broiler-AI:           Origin=Ported; Spec=Temporal ParseTemporalDurationString via Broiler.VM JsTemporalParser.Duration; IP=Low; Security=High; Resources=3; Fingerprint=A6B684
    // Broiler-Falsified-If: a string the TemporalDurationString grammar refuses is accepted, or a part answers other than its exact value rounded once to a Number
    // Broiler-Human:        PENDING
    private static bool Duration(ReadOnlySpan<char> text, out DurationFields duration)
    {
        duration = default;
        var at = 0;
        var negative = false;

        if (at < text.Length && text[at] is '+' or '-')
        {
            negative = text[at] == '-';
            at++;
        }

        if (at >= text.Length || text[at] is not ('P' or 'p'))
            return false;

        at++;
        BigInteger years = 0, months = 0, weeks = 0, days = 0, hours = 0, minutes = 0, seconds = 0;
        BigInteger totalSubNs = 0;
        var any = false;
        var timeAny = false;
        var inTime = false;
        var order = 0;
        var fractionSeen = false;

        // EACH COMPONENT IS DIGITS, AN OPTIONAL FRACTION (time units only, and only the last), AND ITS DESIGNATOR, in
        // the grammar's order.
        while (at < text.Length)
        {
            if (text[at] is 'T' or 't')
            {
                if (inTime)
                    return false;

                inTime = true;
                at++;
                continue;
            }

            if (fractionSeen)
                return false;

            var start = at;
            while (at < text.Length && IsDigit(text[at]))
                at++;

            if (at == start)
                return false;

            var whole = BigInteger.Parse(text[start..at], NumberStyles.None, NumberFormatInfo.InvariantInfo);
            var fractionStart = -1;
            var fractionLength = 0;

            if (at < text.Length && text[at] is '.' or ',')
            {
                fractionStart = ++at;
                while (at < text.Length && IsDigit(text[at]))
                    at++;

                fractionLength = at - fractionStart;
                if (fractionLength == 0 || fractionLength > 9)
                    return false;
            }

            if (at >= text.Length)
                return false;

            // ASCII only: the platform's casing would read U+017F LATIN SMALL LETTER LONG S as S.
            var designator = text[at++];
            if (designator is >= 'a' and <= 'z')
                designator = (char)(designator - 32);

            int rank;
            if (!inTime)
            {
                rank = designator switch { 'Y' => 1, 'M' => 2, 'W' => 3, 'D' => 4, _ => 0 };
                if (rank == 0 || fractionStart >= 0)
                    return false;
            }
            else
            {
                rank = designator switch { 'H' => 5, 'M' => 6, 'S' => 7, _ => 0 };
                if (rank == 0)
                    return false;

                timeAny = true;
            }

            if (rank <= order)
                return false;

            order = rank;
            any = true;

            // THE NANOSECONDS A FRACTION CONTRIBUTES, exactly: a fraction of an hour or minute is carried into the
            // smaller units by the operation's steps, which together keep its exact value.
            BigInteger fractionNs = 0;
            if (fractionStart >= 0)
            {
                fractionSeen = true;
                var unitNs = rank switch { 5 => 3_600_000_000_000L, 6 => 60_000_000_000L, _ => 1_000_000_000L };
                var fractionValue = 0L;
                foreach (var digit in text.Slice(fractionStart, fractionLength))
                    fractionValue = (fractionValue * 10) + (digit - '0');
                fractionNs = fractionValue * (BigInteger)unitNs / BigInteger.Pow(10, fractionLength);
            }

            switch (rank)
            {
                case 1: years = whole; break;
                case 2: months = whole; break;
                case 3: weeks = whole; break;
                case 4: days = whole; break;
                case 5: hours = whole; totalSubNs = fractionNs; break;
                case 6: minutes = whole; totalSubNs = fractionNs; break;
                default: seconds = whole; totalSubNs = fractionNs; break;
            }
        }

        if (!any || (inTime && !timeAny))
            return false;

        // THE FRACTION'S NANOSECONDS SPLIT INTO WHOLE MINUTES AND SECONDS ONLY WHERE IT WAS AN HOUR'S OR A MINUTE'S; a
        // seconds fraction is milliseconds and smaller.
        BigInteger extraMinutes = 0, extraSeconds = 0;
        if (order == 5)
        {
            extraMinutes = totalSubNs / 60_000_000_000L;
            totalSubNs %= 60_000_000_000L;
        }

        if (order is >= 5 and <= 6)
        {
            extraSeconds = totalSubNs / 1_000_000_000L;
            totalSubNs %= 1_000_000_000L;
        }

        var milliseconds = totalSubNs / 1_000_000;
        var microseconds = totalSubNs / 1000 % 1000;
        var nanoseconds = totalSubNs % 1000;
        var factor = negative ? -1 : 1;

        double Number(BigInteger value) => ExactRounding.ToDouble(value * factor, BigInteger.One);

        duration = new DurationFields(
            Number(years), Number(months), Number(weeks), Number(days), Number(hours), Number(minutes + extraMinutes), Number(seconds + extraSeconds),
            Number(milliseconds), Number(microseconds), Number(nanoseconds));
        return true;
    }

    // Broiler-AI:           Origin=Ported; Spec=Temporal DecimalDigit via Broiler.VM JsTemporalParser.IsDigit; IP=Low; Security=Low; Resources=0; Fingerprint=B3DF33
    // Broiler-Human:        PENDING
    private static bool IsDigit(char c) => c is >= '0' and <= '9';

    // Broiler-AI:           Origin=Ported; Spec=Temporal grammar ASCII letters via Broiler.VM JsTemporalParser.IsAlpha; IP=Low; Security=Low; Resources=0; Fingerprint=05C4FA
    // Broiler-Human:        PENDING
    private static bool IsAlpha(char c) => c is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z');

    // A key-value annotation's place in the text.
    // Broiler-AI:           Origin=Ported; Spec=Temporal Annotation via Broiler.VM JsTemporalParser.annotations; IP=Low; Security=Low; Resources=0; Fingerprint=B73C02
    // Broiler-Human:        PENDING
    private readonly record struct AnnotationSpan(bool Critical, int KeyStart, int KeyEnd, int ValueStart, int ValueEnd);

    // The recursive-descent parser: one method per production, each advancing over its text or restoring the position
    // and failing, so that the alternatives backtrack as the grammar's do. The parse nodes ParseISODateTime reads are
    // captured as the productions match.
    // Broiler-AI:           Origin=Ported; Spec=Temporal ISO 8601 grammar productions via Broiler.VM JsTemporalParser; IP=Low; Security=High; Resources=3; Fingerprint=E21966
    // Broiler-Falsified-If: a production matches text its grammar rule does not derive, fails on text it derives where Broiler.VM's matches, or leaves the position other than after its match or at its start
    // Broiler-Human:        PENDING
    private ref struct Parser
    {
        // Broiler-AI:           Origin=Ported; Spec=Temporal ISO 8601 grammar via Broiler.VM JsTemporalParser.text; IP=Low; Security=Low; Resources=0; Fingerprint=8A5809
        // Broiler-Human:        PENDING
        private readonly ReadOnlySpan<char> text;

        // Broiler-AI:           Origin=Ported; Spec=Temporal ISO 8601 grammar via Broiler.VM JsTemporalParser.annotations; IP=Low; Security=Low; Resources=1; Fingerprint=4362D1
        // Broiler-Human:        PENDING
        private List<AnnotationSpan>? annotations;

        // Broiler-AI:           Origin=Ported; Spec=Temporal ISO 8601 grammar via Broiler.VM JsTemporalParser; IP=Low; Security=Low; Resources=1; Fingerprint=1A87B3
        // Broiler-Human:        PENDING
        internal Parser(ReadOnlySpan<char> text)
        {
            this.text = text;
            annotations = null;
            Reset();
        }

        // The position.
        internal int At { get; private set; }

        // DateYear: whether one matched, and its value.
        internal bool HasYear { get; private set; }

        internal long Year { get; private set; }

        // DateMonth and DateDay, 0 where absent.
        internal int Month { get; private set; }

        internal int Day { get; private set; }

        // Hour, the first MinuteSecond and TimeSecond, -1 where absent.
        internal int Hour { get; private set; }

        internal int Minute { get; private set; }

        internal int Second { get; private set; }

        // The time's TemporalDecimalFraction digits, -1 where absent.
        internal int FractionStart { get; private set; }

        internal int FractionLength { get; private set; }

        // The UTCDesignator.
        internal bool Z { get; private set; }

        // The UTCOffset[+SubMinutePrecision] after the time, -1 where absent.
        internal int OffsetStart { get; private set; }

        internal int OffsetEnd { get; private set; }

        internal bool OffsetHasSeconds { get; private set; }

        // The TimeZoneIdentifier of the time zone annotation, -1 where absent.
        internal int ZoneStart { get; private set; }

        internal int ZoneEnd { get; private set; }

        // Broiler-AI:           Origin=Ported; Spec=Temporal Annotations via Broiler.VM JsTemporalParser.annotations; IP=Low; Security=Low; Resources=0; Fingerprint=29E1E1
        // Broiler-Human:        PENDING
        internal readonly int AnnotationCount => annotations?.Count ?? 0;

        // Broiler-AI:           Origin=Ported; Spec=Temporal Annotations via Broiler.VM JsTemporalParser.annotations; IP=Low; Security=Low; Resources=0; Fingerprint=582E31
        // Broiler-Human:        PENDING
        internal readonly AnnotationSpan AnnotationAt(int index) => annotations![index];

        // ---- Goals ------------------------------------------------------------------------------------------------

        // DateTime[Z, TimeRequired] and its suffixes, for the date-time goals.
        // Broiler-AI:           Origin=Ported; Spec=Temporal AnnotatedDateTime via Broiler.VM JsTemporalParser.AnnotatedDateTime; IP=Low; Security=High; Resources=3; Fingerprint=30151F
        // Broiler-Falsified-If: a date-time without its required time, offset or zone annotation matches, Z matches where the goal has no Z, or text remains after the match
        // Broiler-Human:        PENDING
        internal bool AnnotatedDateTime(bool zoned, bool timeRequired, bool instant)
        {
            Reset();
            if (!DateSpec())
                return false;

            if (EatAny(" Tt"))
            {
                if (!TimeSpec())
                    return false;
                if (!DateTimeOffset(allowZ: zoned || instant))
                    return false;
                if (instant && !Z && OffsetStart < 0)
                    return false;
            }
            else if (timeRequired || instant)
            {
                return false;
            }

            if (!Annotations())
                return false;
            if (zoned && ZoneStart < 0)
                return false;

            return At == text.Length;
        }

        // AnnotatedTime with the early errors of the grammar: a time without its designator must not also be a
        // month-day or a year-month.
        // Broiler-AI:           Origin=Ported; Spec=Temporal AnnotatedTime and its early errors via Broiler.VM JsTemporalParser.AnnotatedTime; IP=Low; Security=High; Resources=3; Fingerprint=8D86A1
        // Broiler-Falsified-If: a designator-less time that is also a whole DateSpecMonthDay or DateSpecYearMonth matches, or Z matches
        // Broiler-Human:        PENDING
        internal bool AnnotatedTime()
        {
            Reset();
            var designated = EatAny("Tt");
            var timeStart = At;
            if (!TimeSpec() || !DateTimeOffset(allowZ: false))
                return false;

            var timeEnd = At;
            if (!Annotations() || At != text.Length)
                return false;

            if (!designated)
            {
                var span = text[timeStart..timeEnd];
                var monthDay = new Parser(span);
                if (monthDay.DateSpecMonthDay() && monthDay.At == span.Length)
                    return false;

                var yearMonth = new Parser(span);
                if (yearMonth.DateSpecYearMonth() && yearMonth.At == span.Length)
                    return false;
            }

            return true;
        }

        // Broiler-AI:           Origin=Ported; Spec=Temporal AnnotatedYearMonth via Broiler.VM JsTemporalParser.AnnotatedYearMonth; IP=Low; Security=High; Resources=3; Fingerprint=45E67D
        // Broiler-Falsified-If: a year-month followed by anything but annotations matches
        // Broiler-Human:        PENDING
        internal bool AnnotatedYearMonth()
        {
            Reset();
            return DateSpecYearMonth() && Annotations() && At == text.Length;
        }

        // Broiler-AI:           Origin=Ported; Spec=Temporal AnnotatedMonthDay via Broiler.VM JsTemporalParser.AnnotatedMonthDay; IP=Low; Security=High; Resources=3; Fingerprint=0C76E0
        // Broiler-Falsified-If: a month-day followed by anything but annotations matches
        // Broiler-Human:        PENDING
        internal bool AnnotatedMonthDay()
        {
            Reset();
            return DateSpecMonthDay() && Annotations() && At == text.Length;
        }

        // ---- Dates ------------------------------------------------------------------------------------------------

        // DateYear: four digits, or an ASCII sign and six, -000000 refused.
        // Broiler-AI:           Origin=Ported; Spec=Temporal DateYear via Broiler.VM JsTemporalParser.DateYear; IP=Low; Security=High; Resources=1; Fingerprint=4FFA3B
        // Broiler-Falsified-If: a year other than four ASCII digits or an ASCII sign and six is matched, or -000000 is
        // Broiler-Human:        PENDING
        private bool DateYear()
        {
            var start = At;
            if (EatAny("+-"))
            {
                var six = Digits(6);
                if (six < 0 || (text[start] == '-' && six == 0))
                {
                    At = start;
                    return false;
                }

                Year = text[start] == '-' ? -six : six;
                HasYear = true;
                return true;
            }

            var four = Digits(4);
            if (four < 0)
            {
                At = start;
                return false;
            }

            Year = four;
            HasYear = true;
            return true;
        }

        // DateMonth: 01 to 12.
        // Broiler-AI:           Origin=Ported; Spec=Temporal DateMonth via Broiler.VM JsTemporalParser.DateMonth; IP=Low; Security=High; Resources=0; Fingerprint=60B3B8
        // Broiler-Falsified-If: a month other than two ASCII digits from 01 to 12 is matched
        // Broiler-Human:        PENDING
        private bool DateMonth()
        {
            var start = At;
            var digits = Digits(2);
            if (digits is < 1 or > 12)
            {
                At = start;
                return false;
            }

            Month = digits;
            return true;
        }

        // DateDay: 01 to 31.
        // Broiler-AI:           Origin=Ported; Spec=Temporal DateDay via Broiler.VM JsTemporalParser.DateDay; IP=Low; Security=High; Resources=0; Fingerprint=21B999
        // Broiler-Falsified-If: a day other than two ASCII digits from 01 to 31 is matched
        // Broiler-Human:        PENDING
        private bool DateDay()
        {
            var start = At;
            var digits = Digits(2);
            if (digits is < 1 or > 31)
            {
                At = start;
                return false;
            }

            Day = digits;
            return true;
        }

        // DateSpec[Extended] with its early error IsValidDate: a date that matches and is not a date fails.
        // Broiler-AI:           Origin=Ported; Spec=Temporal DateSpec and IsValidDate via Broiler.VM JsTemporalParser.DateSpec; IP=Low; Security=High; Resources=1; Fingerprint=3F3E7B
        // Broiler-Falsified-If: a date mixing the extended and basic separators, or a date that is not a valid ISO date, matches
        // Broiler-Human:        PENDING
        private bool DateSpec()
        {
            var start = At;
            for (var form = 0; form < 2; form++)
            {
                var extended = form == 0;
                At = start;
                if (DateYear() && (!extended || Eat('-')) && DateMonth() && (!extended || Eat('-')) && DateDay())
                {
                    if (!IsoCalendarMath.IsValidIsoDate(Year, Month, Day))
                    {
                        // AN EARLY ERROR, not an alternative: the date matched and is not a date.
                        At = start;
                        return false;
                    }

                    return true;
                }
            }

            At = start;
            HasYear = false;
            Year = 0;
            Month = 0;
            Day = 0;
            return false;
        }

        // DateSpecYearMonth: a year, an optional hyphen and a month.
        // Broiler-AI:           Origin=Ported; Spec=Temporal DateSpecYearMonth via Broiler.VM JsTemporalParser.DateSpecYearMonth; IP=Low; Security=High; Resources=1; Fingerprint=313B7C
        // Broiler-Falsified-If: a year-month other than a DateYear, an optional hyphen and a DateMonth matches
        // Broiler-Human:        PENDING
        internal bool DateSpecYearMonth()
        {
            var start = At;
            if (DateYear())
            {
                Eat('-');
                if (DateMonth())
                    return true;
            }

            At = start;
            HasYear = false;
            Year = 0;
            Month = 0;
            return false;
        }

        // DateSpecMonthDay: an optional --, a month, an optional hyphen and a day, valid in the reference leap year.
        // Broiler-AI:           Origin=Ported; Spec=Temporal DateSpecMonthDay and IsValidMonthDay via Broiler.VM JsTemporalParser.DateSpecMonthDay; IP=Low; Security=High; Resources=1; Fingerprint=14CC14
        // Broiler-Falsified-If: a month-day that is not a day of 1972 matches, or one with a single leading hyphen
        // Broiler-Human:        PENDING
        internal bool DateSpecMonthDay()
        {
            var start = At;
            if (Peek() == '-' && Peek(1) == '-')
                At += 2;

            var afterDashes = At;
            for (var form = 0; form < 2; form++)
            {
                var extended = form == 0;
                At = afterDashes;
                if (DateMonth() && (!extended || Eat('-')) && DateDay())
                {
                    // THE PROPOSAL'S REFERENCE YEAR, a leap year: a month-day admits February 29.
                    if (!IsoCalendarMath.IsValidIsoDate(1972, Month, Day))
                    {
                        At = start;
                        return false;
                    }

                    return true;
                }
            }

            At = start;
            Month = 0;
            Day = 0;
            return false;
        }

        // ---- Times ------------------------------------------------------------------------------------------------

        // TemporalDecimalFraction: a period or comma and one to nine digits; the time's is kept, the first only.
        // Broiler-AI:           Origin=Ported; Spec=Temporal TemporalDecimalFraction via Broiler.VM JsTemporalParser.Fraction; IP=Low; Security=High; Resources=1; Fingerprint=B60AF2
        // Broiler-Falsified-If: a fraction of no digit or of ten or more digits matches
        // Broiler-Human:        PENDING
        private bool Fraction(bool keep)
        {
            var start = At;
            if (!EatAny(".,"))
                return false;

            var digitsStart = At;
            while (At < text.Length && IsDigit(text[At]) && At - digitsStart < 9)
                At++;

            if (At == digitsStart || (At < text.Length && IsDigit(text[At])))
            {
                At = start;
                return false;
            }

            if (keep && FractionStart < 0)
            {
                FractionStart = digitsStart;
                FractionLength = At - digitsStart;
            }

            return true;
        }

        // TimeSpec[Extended] of either form: the extended form first, then the basic one.
        // Broiler-AI:           Origin=Ported; Spec=Temporal TimeSpec via Broiler.VM JsTemporalParser.TimeSpec; IP=Low; Security=High; Resources=1; Fingerprint=BC60CA
        // Broiler-Falsified-If: a time mixing the extended and basic separators matches beyond its hour and minute, an hour above 23, a minute above 59 or a second above 60 matches
        // Broiler-Human:        PENDING
        private bool TimeSpec()
        {
            var start = At;
            for (var form = 0; form < 2; form++)
            {
                var extended = form == 0;
                At = start;
                var hour = Bounded(23);
                if (hour < 0)
                    return false;

                Hour = hour;
                var afterHour = At;
                int minute;
                if ((!extended || Eat(':')) && (minute = Bounded(59)) >= 0)
                {
                    Minute = minute;
                    var afterMinute = At;
                    if (!extended || Eat(':'))
                    {
                        var secondStart = At;
                        var second = Digits(2);
                        if (second is >= 0 and <= 60)
                        {
                            Second = second;
                            Fraction(keep: true);
                            return true;
                        }

                        At = secondStart;
                    }

                    // HOUR AND MINUTE ALONE, in the form that wrote its separator.
                    At = afterMinute;
                    return true;
                }

                At = afterHour;
                Minute = -1;
                if (!extended)
                    return true;
            }

            At = start;
            return false;
        }

        // ---- Offsets and time zones -------------------------------------------------------------------------------

        // UTCOffset[SubMinutePrecision]: a sign, hours, and optionally minutes and, with sub-minute precision, seconds
        // and a fraction, all extended or all basic. The match may be shorter than the text that follows.
        // Broiler-AI:           Origin=Ported; Spec=Temporal UTCOffset via Broiler.VM JsTemporalParser.UtcOffset; IP=Low; Security=High; Resources=1; Fingerprint=A4AF4D
        // Broiler-Falsified-If: an offset with a sign other than ASCII + or -, an hour above 23, a minute or second above 59, or seconds without sub-minute precision matches
        // Broiler-Human:        PENDING
        internal bool UtcOffset(bool subMinute, out bool hasSeconds)
        {
            hasSeconds = false;
            var start = At;
            if (!EatAny("+-") || Bounded(23) < 0)
            {
                At = start;
                return false;
            }

            var afterHour = At;
            for (var form = 0; form < 2; form++)
            {
                var extended = form == 0;
                At = afterHour;
                if ((extended && !Eat(':')) || Bounded(59) < 0)
                    continue;

                var afterMinute = At;
                if (subMinute && (!extended || Eat(':')) && Bounded(59) >= 0)
                {
                    Fraction(keep: false);
                    hasSeconds = true;
                    return true;
                }

                At = afterMinute;
                return true;
            }

            At = afterHour;
            return true;
        }

        // TimeZoneIANAName: components of the name's characters joined by slashes, each starting with a letter, a
        // period or an underscore.
        // Broiler-AI:           Origin=Ported; Spec=Temporal TimeZoneIANAName via Broiler.VM JsTemporalParser.TimeZoneIanaName; IP=Low; Security=High; Resources=3; Fingerprint=B4B31D
        // Broiler-Falsified-If: a name with an empty component, a component starting with a digit, a hyphen or a plus sign, or a character outside ASCII letters, digits and . _ - + / matches
        // Broiler-Human:        PENDING
        private bool TimeZoneIanaName()
        {
            var start = At;
            while (true)
            {
                if (!(IsAlpha(Peek()) || Peek() is '.' or '_'))
                {
                    At = start;
                    return false;
                }

                At++;
                while (IsAlpha(Peek()) || IsDigit(Peek()) || Peek() is '.' or '_' or '-' or '+')
                    At++;

                if (!Eat('/'))
                    return true;
            }
        }

        // TimeZoneIdentifier: an offset without seconds, or else an IANA name.
        // Broiler-AI:           Origin=Ported; Spec=Temporal TimeZoneIdentifier via Broiler.VM JsTemporalParser.TimeZoneIdentifier; IP=Low; Security=High; Resources=3; Fingerprint=4F1405
        // Broiler-Falsified-If: an identifier other than a UTCOffset without seconds or a TimeZoneIANAName matches
        // Broiler-Human:        PENDING
        internal bool TimeZoneIdentifier()
        {
            var start = At;
            if (UtcOffset(subMinute: false, out _))
                return true;

            At = start;
            return TimeZoneIanaName();
        }

        // The bracketed suffix: a time zone annotation first if at all, then key-value annotations.
        // Broiler-AI:           Origin=Ported; Spec=Temporal TimeZoneAnnotation and Annotations via Broiler.VM JsTemporalParser.Annotations; IP=Low; Security=High; Resources=3; Fingerprint=91C026
        // Broiler-Falsified-If: an empty or flag-only annotation, a zone annotation after another annotation or one the TimeZoneIdentifier grammar refuses, or a key or value off the grammar matches
        // Broiler-Human:        PENDING
        private bool Annotations()
        {
            var first = true;
            while (Peek() == '[')
            {
                var start = At;
                At++;
                var critical = Eat('!');
                var close = text[At..].IndexOf(']');
                if (close < 0)
                {
                    At = start;
                    return false;
                }

                close += At;
                var inner = text[At..close];
                var equals = inner.IndexOf('=');
                if (equals >= 0)
                {
                    if (!IsAnnotationKey(inner[..equals]) || !IsAnnotationValueCore(inner[(equals + 1)..]))
                    {
                        At = start;
                        return false;
                    }

                    annotations ??= [];
                    annotations.Add(new AnnotationSpan(critical, At, At + equals, At + equals + 1, close));
                    At = close + 1;
                }
                else
                {
                    if (!first)
                    {
                        At = start;
                        return false;
                    }

                    var identifierStart = At;
                    if (!TimeZoneIdentifier() || At != close)
                    {
                        At = start;
                        return false;
                    }

                    ZoneStart = identifierStart;
                    ZoneEnd = close;
                    At = close + 1;
                }

                first = false;
            }

            return true;
        }

        // DateTimeUTCOffset[Z]: a Z where allowed, or an optional offset with sub-minute precision.
        // Broiler-AI:           Origin=Ported; Spec=Temporal DateTimeUTCOffset via Broiler.VM JsTemporalParser.DateTimeOffset; IP=Low; Security=High; Resources=1; Fingerprint=A9D204
        // Broiler-Falsified-If: Z or z matches where the goal has no UTCDesignator
        // Broiler-Human:        PENDING
        private bool DateTimeOffset(bool allowZ)
        {
            if (allowZ && EatAny("Zz"))
            {
                Z = true;
                return true;
            }

            if (Peek() is 'Z' or 'z')
                return false;

            var start = At;
            if (UtcOffset(subMinute: true, out var hasSeconds))
            {
                OffsetStart = start;
                OffsetEnd = At;
                OffsetHasSeconds = hasSeconds;
            }

            return true;
        }

        // ---- Characters -------------------------------------------------------------------------------------------

        // Every parse node cleared, and the position at the start.
        // Broiler-AI:           Origin=Ported; Spec=Temporal ISO 8601 grammar via Broiler.VM JsTemporalParser.Reset; IP=Low; Security=Low; Resources=0; Fingerprint=8894FF
        // Broiler-Human:        PENDING
        private void Reset()
        {
            At = 0;
            HasYear = false;
            Year = 0;
            Month = 0;
            Day = 0;
            Hour = -1;
            Minute = -1;
            Second = -1;
            FractionStart = -1;
            FractionLength = 0;
            Z = false;
            OffsetStart = -1;
            OffsetEnd = -1;
            OffsetHasSeconds = false;
            ZoneStart = -1;
            ZoneEnd = -1;
            annotations?.Clear();
        }

        // The character a number of places ahead, or U+0000 past the end.
        // Broiler-AI:           Origin=Ported; Spec=Temporal ISO 8601 grammar via Broiler.VM JsTemporalParser.Peek; IP=Low; Security=Low; Resources=0; Fingerprint=356AB8
        // Broiler-Human:        PENDING
        private readonly char Peek(int ahead = 0) => At + ahead < text.Length ? text[At + ahead] : '\0';

        // Broiler-AI:           Origin=Ported; Spec=Temporal ISO 8601 grammar via Broiler.VM JsTemporalParser.Eat; IP=Low; Security=Low; Resources=0; Fingerprint=C679A1
        // Broiler-Human:        PENDING
        private bool Eat(char c)
        {
            if (Peek() != c)
                return false;

            At++;
            return true;
        }

        // Broiler-AI:           Origin=Ported; Spec=Temporal ISO 8601 grammar via Broiler.VM JsTemporalParser.EatAny; IP=Low; Security=Low; Resources=0; Fingerprint=DA0F5F
        // Broiler-Human:        PENDING
        private bool EatAny(ReadOnlySpan<char> set)
        {
            if (At < text.Length && set.Contains(text[At]))
            {
                At++;
                return true;
            }

            return false;
        }

        // A run of exactly this many ASCII digits at the position, read; -1, the position kept, where there is none.
        // Broiler-AI:           Origin=Ported; Spec=Temporal DecimalDigit via Broiler.VM JsTemporalParser.Digits; IP=Low; Security=High; Resources=0; Fingerprint=A86237
        // Broiler-Falsified-If: a character other than an ASCII digit is read as one
        // Broiler-Human:        PENDING
        private int Digits(int count)
        {
            if (At + count > text.Length)
                return -1;

            var value = 0;
            for (var i = 0; i < count; i++)
            {
                var c = text[At + i];
                if (!IsDigit(c))
                    return -1;
                value = (value * 10) + (c - '0');
            }

            At += count;
            return value;
        }

        // Two ASCII digits no greater than a maximum; -1, the position kept, where there are none.
        // Broiler-AI:           Origin=Ported; Spec=Temporal Hour and MinuteSecond via Broiler.VM JsTemporalParser.Bounded; IP=Low; Security=High; Resources=0; Fingerprint=757D0F
        // Broiler-Falsified-If: two digits above the maximum are read
        // Broiler-Human:        PENDING
        private int Bounded(int maximum)
        {
            var start = At;
            var digits = Digits(2);
            if (digits < 0 || digits > maximum)
            {
                At = start;
                return -1;
            }

            return digits;
        }
    }
}
