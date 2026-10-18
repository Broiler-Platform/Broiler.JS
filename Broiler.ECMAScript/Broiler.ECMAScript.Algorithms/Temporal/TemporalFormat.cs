// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   28
// Annotated:        28/28
// Exempt:           0
// Human-reviewed:   0/28
// IP risk:          Low
// Security risk:    Low
// Criteria:         0/0
// Resource impact:  1/10 max
// Unverified:       28
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Globalization;
using System.Numerics;
using System.Text;
using Broiler.ECMAScript.Numbers;
using Broiler.ECMAScript.Time;

namespace Broiler.ECMAScript.Temporal;

/// <summary>
/// Every Temporal string form (JSD-0078's serialization sub-area): dates, times, date-times, year-months, month-days,
/// instants, zoned date-times and durations, with the proposal's padding, fractional seconds, calendar annotations and
/// offsets, and the steps each <c>toString</c> takes after its option reads.
/// </summary>
/// <remarks>
/// <para>
/// <b>A precision</b> is an integer: 0 to 9 digits after the seconds' point, <see cref="AutoPrecision"/> for as many as
/// are not zero, or <see cref="MinutePrecision"/> for no seconds at all, as
/// <see cref="TemporalOptionRules.ToSecondsStringPrecisionRecord"/> answers it.
/// </para>
/// <para>
/// <b>The zone lookup stays the adapter's</b> (JSD-0078's serialization X6): an instant's and a zoned date-time's forms
/// take the offset in force at the instant in nanoseconds - <see cref="ZonedTime.GetOffsetNanosecondsFor"/> answers it
/// over SJL-8's rules - and write the wall clock that offset gives. Nothing here reads an option, a zone or a calendar;
/// identifiers are written as the caller hands them.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=Ported; Spec=JSD-0078 serialization, Temporal string forms via Broiler.VM JsTemporalCore and JsTemporal.Conversions; IP=Low; Security=Low; Resources=1; Fingerprint=1485F3
// Broiler-Human:        PENDING
public static class TemporalFormat
{
    /// <summary>The precision <c>auto</c>: as many fraction digits as are not trailing zeros.</summary>
    // Broiler-AI:           Origin=Ported; Spec=Temporal ToSecondsStringPrecisionRecord via Broiler.VM JsTemporalCore.PrecisionAuto; IP=Low; Security=None; Resources=0; Fingerprint=0871D5
    // Broiler-Human:        PENDING
    public const int AutoPrecision = -1;

    /// <summary>The precision <c>minute</c>: hours and minutes only.</summary>
    // Broiler-AI:           Origin=Ported; Spec=Temporal ToSecondsStringPrecisionRecord via Broiler.VM JsTemporalCore.PrecisionMinute; IP=Low; Security=None; Resources=0; Fingerprint=D12D3D
    // Broiler-Human:        PENDING
    public const int MinutePrecision = -2;

    /// <summary>A year as an ISO string writes it: the proposal's <c>PadISOYear</c>.</summary>
    /// <param name="year">The year.</param>
    /// <returns>Four digits for 0 to 9999; otherwise a sign and at least six digits.</returns>
    // Broiler-AI:           Origin=Ported; Spec=Temporal PadISOYear via Broiler.VM JsTemporalCore.PadYear; IP=Low; Security=Low; Resources=0; Fingerprint=A76BD7
    // Broiler-Human:        PENDING
    public static string PadIsoYear(long year) =>
        year is >= 0 and <= 9999 ? Padded((ulong)year, 4) : (year > 0 ? "+" : "-") + Padded(Magnitude(year), 6);

    /// <summary>The fraction of a second as a string writes it: the proposal's <c>FormatFractionalSeconds</c>.</summary>
    /// <param name="subSecondNanoseconds">The nanoseconds within the second, 0 to 999,999,999.</param>
    /// <param name="precision">0 to 9, or <see cref="AutoPrecision"/>.</param>
    /// <returns>A point and the digits, truncated to the precision or (auto) to the last digit that is not zero; empty for no digit.</returns>
    /// <exception cref="ArgumentOutOfRangeException">An argument is outside its domain.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal FormatFractionalSeconds via Broiler.VM JsTemporalCore.FractionalSeconds; IP=Low; Security=Low; Resources=0; Fingerprint=FE02EA
    // Broiler-Human:        PENDING
    public static string FormatFractionalSeconds(int subSecondNanoseconds, int precision)
    {
        if (subSecondNanoseconds is < 0 or > 999_999_999)
            throw new ArgumentOutOfRangeException(nameof(subSecondNanoseconds), subSecondNanoseconds, "The nanoseconds are within a second.");
        if (precision is < AutoPrecision or > 9)
            throw new ArgumentOutOfRangeException(nameof(precision), precision, "The precision is auto or 0 to 9.");

        if (precision == AutoPrecision)
            return subSecondNanoseconds == 0 ? string.Empty : "." + Padded((ulong)subSecondNanoseconds, 9).TrimEnd('0');

        return precision == 0 ? string.Empty : "." + Padded((ulong)subSecondNanoseconds, 9)[..precision];
    }

    /// <summary>A time of day's fields as a string writes them: the proposal's <c>FormatTimeString</c>, separated.</summary>
    /// <param name="hour">The hour, 0 to 23 (24 where an offset is rounded up to a day).</param>
    /// <param name="minute">The minute, 0 to 59.</param>
    /// <param name="second">The second, 0 to 59.</param>
    /// <param name="subSecondNanoseconds">The nanoseconds within the second.</param>
    /// <param name="precision">0 to 9, <see cref="AutoPrecision"/> or <see cref="MinutePrecision"/>.</param>
    /// <returns><c>HH:MM</c> for minute precision; otherwise <c>HH:MM:SS</c> and the fraction.</returns>
    /// <exception cref="ArgumentOutOfRangeException">An argument is outside its domain.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal FormatTimeString via Broiler.VM JsTemporalCore.FormatTime; IP=Low; Security=Low; Resources=0; Fingerprint=5BCA95
    // Broiler-Human:        PENDING
    public static string FormatTimeString(int hour, int minute, int second, int subSecondNanoseconds, int precision)
    {
        if (hour is < 0 or > 24 || minute is < 0 or > 59 || second is < 0 or > 59)
            throw new ArgumentOutOfRangeException(nameof(hour), "The time fields are outside their ranges.");
        if (precision is < MinutePrecision or > 9)
            throw new ArgumentOutOfRangeException(nameof(precision), precision, "The precision is minute, auto or 0 to 9.");

        var text = Padded((ulong)hour, 2) + ":" + Padded((ulong)minute, 2);
        return precision == MinutePrecision
            ? text
            : text + ":" + Padded((ulong)second, 2) + FormatFractionalSeconds(subSecondNanoseconds, precision);
    }

    /// <summary>A time of day as a <c>Temporal.PlainTime</c> writes it: the proposal's <c>TimeRecordToString</c>.</summary>
    /// <param name="time">The time, valid.</param>
    /// <param name="precision">0 to 9, <see cref="AutoPrecision"/> or <see cref="MinutePrecision"/>.</param>
    /// <returns>The time's string, for example <c>12:30:45.5</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The time is not valid, or the precision is outside its domain.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal TimeRecordToString via Broiler.VM JsTemporalCore.TimeToString; IP=Low; Security=Low; Resources=0; Fingerprint=6220F2
    // Broiler-Human:        PENDING
    public static string TimeToString(IsoTime time, int precision)
    {
        CheckTime(time);
        return FormatTimeString(time.Hour, time.Minute, time.Second, SubSecond(time), precision);
    }

    /// <summary>A calendar annotation as a string form writes it: the proposal's <c>FormatCalendarAnnotation</c>.</summary>
    /// <param name="calendar">The calendar identifier, canonical.</param>
    /// <param name="show">The <c>calendarName</c> option.</param>
    /// <returns>Empty for <c>never</c>, and for <c>auto</c> with <c>iso8601</c>; <c>[!u-ca=…]</c> for <c>critical</c>; else <c>[u-ca=…]</c>.</returns>
    /// <exception cref="ArgumentNullException">The calendar is <see langword="null"/>.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal FormatCalendarAnnotation via Broiler.VM JsTemporal.CalendarAnnotation; IP=Low; Security=Low; Resources=1; Fingerprint=B9826A
    // Broiler-Human:        PENDING
    public static string FormatCalendarAnnotation(string calendar, ShowCalendarName show)
    {
        ArgumentNullException.ThrowIfNull(calendar);
        return show switch
        {
            ShowCalendarName.Never => string.Empty,
            ShowCalendarName.Auto when calendar == "iso8601" => string.Empty,
            ShowCalendarName.Critical => "[!u-ca=" + calendar + "]",
            _ => "[u-ca=" + calendar + "]",
        };
    }

    /// <summary>A date as a <c>Temporal.PlainDate</c> writes it: the proposal's <c>TemporalDateToString</c>.</summary>
    /// <param name="date">The date.</param>
    /// <param name="calendar">The calendar identifier, canonical.</param>
    /// <param name="show">The <c>calendarName</c> option.</param>
    /// <returns><c>YYYY-MM-DD</c> and the calendar annotation.</returns>
    // Broiler-AI:           Origin=Ported; Spec=Temporal TemporalDateToString via Broiler.VM JsTemporal.DateString; IP=Low; Security=Low; Resources=1; Fingerprint=E2396C
    // Broiler-Human:        PENDING
    public static string DateToString(IsoDate date, string calendar, ShowCalendarName show) =>
        IsoDateString(date) + FormatCalendarAnnotation(calendar, show);

    /// <summary>A date-time as a <c>Temporal.PlainDateTime</c> writes it: the proposal's <c>ISODateTimeToString</c>.</summary>
    /// <param name="dateTime">The date-time; its time valid.</param>
    /// <param name="calendar">The calendar identifier, canonical.</param>
    /// <param name="precision">0 to 9, <see cref="AutoPrecision"/> or <see cref="MinutePrecision"/>.</param>
    /// <param name="show">The <c>calendarName</c> option.</param>
    /// <returns><c>YYYY-MM-DDTHH:MM…</c> and the calendar annotation.</returns>
    // Broiler-AI:           Origin=Ported; Spec=Temporal ISODateTimeToString via Broiler.VM JsTemporal.DateTimeString; IP=Low; Security=Low; Resources=1; Fingerprint=2AFF74
    // Broiler-Human:        PENDING
    public static string DateTimeToString(IsoDateTime dateTime, string calendar, int precision, ShowCalendarName show) =>
        IsoDateString(dateTime.Date) + "T" + TimeToString(dateTime.Time, precision) + FormatCalendarAnnotation(calendar, show);

    /// <summary>
    /// A year-month as a <c>Temporal.PlainYearMonth</c> writes it: the proposal's <c>TemporalYearMonthToString</c>, its
    /// reference day written where the calendar is not <c>iso8601</c> or the annotation is forced.
    /// </summary>
    /// <param name="isoDate">The year-month's ISO date, its day the reference day.</param>
    /// <param name="calendar">The calendar identifier, canonical.</param>
    /// <param name="show">The <c>calendarName</c> option.</param>
    /// <returns><c>YYYY-MM</c>, or <c>YYYY-MM-DD</c>, and the calendar annotation.</returns>
    // Broiler-AI:           Origin=Ported; Spec=Temporal TemporalYearMonthToString via Broiler.VM JsTemporal.YearMonthString; IP=Low; Security=Low; Resources=1; Fingerprint=4F3440
    // Broiler-Human:        PENDING
    public static string YearMonthToString(IsoDate isoDate, string calendar, ShowCalendarName show)
    {
        ArgumentNullException.ThrowIfNull(calendar);
        var result = PadIsoYear(isoDate.Year) + "-" + Padded(isoDate.Month, 2);
        if (show is ShowCalendarName.Always or ShowCalendarName.Critical || calendar != "iso8601")
            result += "-" + Padded(isoDate.Day, 2);

        return result + FormatCalendarAnnotation(calendar, show);
    }

    /// <summary>
    /// A month-day as a <c>Temporal.PlainMonthDay</c> writes it: the proposal's <c>TemporalMonthDayToString</c>, its
    /// reference year written where the calendar is not <c>iso8601</c> or the annotation is forced.
    /// </summary>
    /// <param name="isoDate">The month-day's ISO date, its year the reference year.</param>
    /// <param name="calendar">The calendar identifier, canonical.</param>
    /// <param name="show">The <c>calendarName</c> option.</param>
    /// <returns><c>MM-DD</c>, or <c>YYYY-MM-DD</c>, and the calendar annotation.</returns>
    // Broiler-AI:           Origin=Ported; Spec=Temporal TemporalMonthDayToString via Broiler.VM JsTemporal.MonthDayString; IP=Low; Security=Low; Resources=1; Fingerprint=ECBBF5
    // Broiler-Human:        PENDING
    public static string MonthDayToString(IsoDate isoDate, string calendar, ShowCalendarName show)
    {
        ArgumentNullException.ThrowIfNull(calendar);
        var result = Padded(isoDate.Month, 2) + "-" + Padded(isoDate.Day, 2);
        if (show is ShowCalendarName.Always or ShowCalendarName.Critical || calendar != "iso8601")
            result = PadIsoYear(isoDate.Year) + "-" + result;

        return result + FormatCalendarAnnotation(calendar, show);
    }

    /// <summary>
    /// An instant as a <c>Temporal.Instant</c> writes it: the proposal's <c>TemporalInstantToString</c> - the UTC wall clock
    /// and <c>Z</c>, or a zone's wall clock and its offset rounded to the minute.
    /// </summary>
    /// <param name="epochNanoseconds">The instant, already rounded (<see cref="IsoArithmetic.RoundTemporalInstant"/>).</param>
    /// <param name="offsetNanoseconds">The offset in force at the instant in the <c>timeZone</c> option's zone; <see langword="null"/> where the option is undefined, which writes <c>Z</c>.</param>
    /// <param name="precision">0 to 9, <see cref="AutoPrecision"/> or <see cref="MinutePrecision"/>.</param>
    /// <returns>The instant's string, for example <c>2020-01-01T00:00:00Z</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The offset's magnitude is a day or more, or the precision is outside its domain.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal TemporalInstantToString via Broiler.VM JsTemporal.InstantString; IP=Low; Security=Low; Resources=1; Fingerprint=0528FD
    // Broiler-Human:        PENDING
    public static string InstantToString(BigInteger epochNanoseconds, long? offsetNanoseconds, int precision)
    {
        var offset = offsetNanoseconds ?? 0;
        CheckOffset(offset);
        var dateTime = IsoArithmetic.GetIsoPartsFromEpoch(epochNanoseconds + offset);
        var text = IsoDateString(dateTime.Date) + "T" + TimeToString(dateTime.Time, precision);
        return text + (offsetNanoseconds is null ? "Z" : FormatDateTimeUtcOffsetRounded(offset));
    }

    /// <summary>
    /// A zoned date-time as a <c>Temporal.ZonedDateTime</c> writes it: the proposal's <c>TemporalZonedDateTimeToString</c>
    /// after its rounding - the wall clock, the offset rounded to the minute, the time zone annotation and the calendar
    /// annotation, each as its option says.
    /// </summary>
    /// <param name="epochNanoseconds">The instant, already rounded (<see cref="IsoArithmetic.RoundTemporalInstant"/>).</param>
    /// <param name="offsetNanoseconds">The offset in force at that instant in the zone.</param>
    /// <param name="timeZone">The zone's identifier, as the object holds it.</param>
    /// <param name="calendar">The calendar identifier, canonical.</param>
    /// <param name="precision">0 to 9, <see cref="AutoPrecision"/> or <see cref="MinutePrecision"/>.</param>
    /// <param name="showCalendar">The <c>calendarName</c> option.</param>
    /// <param name="showTimeZone">The <c>timeZoneName</c> option.</param>
    /// <param name="showOffset">The <c>offset</c> option.</param>
    /// <returns>The zoned date-time's string, for example <c>2020-07-01T12:00:00+02:00[Europe/Berlin]</c>.</returns>
    /// <exception cref="ArgumentNullException">The zone or the calendar is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The offset's magnitude is a day or more, or the precision is outside its domain.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal TemporalZonedDateTimeToString via Broiler.VM JsTemporal.ZonedDateTimeString; IP=Low; Security=Low; Resources=1; Fingerprint=318DE9
    // Broiler-Human:        PENDING
    public static string ZonedDateTimeToString(
        BigInteger epochNanoseconds,
        long offsetNanoseconds,
        string timeZone,
        string calendar,
        int precision,
        ShowCalendarName showCalendar,
        ShowTimeZoneName showTimeZone,
        ShowOffset showOffset)
    {
        ArgumentNullException.ThrowIfNull(timeZone);
        ArgumentNullException.ThrowIfNull(calendar);
        CheckOffset(offsetNanoseconds);
        var dateTime = IsoArithmetic.GetIsoPartsFromEpoch(epochNanoseconds + offsetNanoseconds);
        var text = new StringBuilder(IsoDateString(dateTime.Date)).Append('T').Append(TimeToString(dateTime.Time, precision));
        if (showOffset != ShowOffset.Never)
            text.Append(FormatDateTimeUtcOffsetRounded(offsetNanoseconds));
        if (showTimeZone != ShowTimeZoneName.Never)
            text.Append('[').Append(showTimeZone == ShowTimeZoneName.Critical ? "!" : string.Empty).Append(timeZone).Append(']');

        return text.Append(FormatCalendarAnnotation(calendar, showCalendar)).ToString();
    }

    /// <summary>A duration as a <c>Temporal.Duration</c> writes it: the proposal's <c>TemporalDurationToString</c>, unrounded.</summary>
    /// <param name="duration">The duration, valid (<see cref="TemporalLimits.CheckDuration"/>).</param>
    /// <param name="precision">0 to 9, or <see cref="AutoPrecision"/>.</param>
    /// <returns>The duration's string, for example <c>-P1DT2H0.5S</c>; <c>PT0S</c> for zero.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The duration is not valid, or the precision is outside its domain.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal TemporalDurationToString via Broiler.VM JsTemporalCore.DurationToString; IP=Low; Security=Low; Resources=1; Fingerprint=351D1C
    // Broiler-Human:        PENDING
    public static string DurationToString(DurationFields duration, int precision)
    {
        if (TemporalLimits.CheckDuration(duration) != TemporalStatus.Completed)
            throw new ArgumentOutOfRangeException(nameof(duration), "The duration is not valid.");
        if (precision is < AutoPrecision or > 9)
            throw new ArgumentOutOfRangeException(nameof(precision), precision, "The precision is auto or 0 to 9.");

        var datePart = new StringBuilder();
        AppendPart(datePart, duration.Years, 'Y');
        AppendPart(datePart, duration.Months, 'M');
        AppendPart(datePart, duration.Weeks, 'W');
        AppendPart(datePart, duration.Days, 'D');

        var timePart = new StringBuilder();
        AppendPart(timePart, duration.Hours, 'H');
        AppendPart(timePart, duration.Minutes, 'M');

        var zeroMinutesAndHigher = DurationCore.DefaultLargestUnit(duration) >= TemporalUnit.Second;
        var secondsDuration = DurationCore.TimeFromComponents(0, 0, duration.Seconds, duration.Milliseconds, duration.Microseconds, duration.Nanoseconds);
        if (!secondsDuration.IsZero || zeroMinutesAndHigher || precision != AutoPrecision)
        {
            var whole = BigInteger.Abs(BigInteger.DivRem(secondsDuration, 1_000_000_000, out var fraction));
            timePart.Append(whole.ToString(NumberFormatInfo.InvariantInfo))
                .Append(FormatFractionalSeconds((int)BigInteger.Abs(fraction), precision))
                .Append('S');
        }

        var result = (DurationCore.Sign(duration) < 0 ? "-P" : "P") + datePart;
        return timePart.Length == 0 ? result : result + "T" + timePart;
    }

    /// <summary>
    /// An offset as the <c>offset</c> getter writes it: the proposal's <c>FormatUTCOffsetNanoseconds</c>, to the minute
    /// where the offset has no seconds, else with its seconds and fraction.
    /// </summary>
    /// <param name="offsetNanoseconds">The offset, of magnitude below a day.</param>
    /// <returns>For example <c>+05:30</c> or <c>-00:44:30</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The offset's magnitude is a day or more.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal FormatUTCOffsetNanoseconds via Broiler.VM JsTemporalCore.FormatOffsetNs; IP=Low; Security=Low; Resources=0; Fingerprint=A9BF5F
    // Broiler-Human:        PENDING
    public static string FormatUtcOffsetNanoseconds(long offsetNanoseconds)
    {
        CheckOffset(offsetNanoseconds);
        var sign = offsetNanoseconds >= 0 ? "+" : "-";
        var absolute = Math.Abs(offsetNanoseconds);
        var hour = (int)(absolute / 3_600_000_000_000L);
        var minute = (int)(absolute / 60_000_000_000L % 60);
        var second = (int)(absolute / 1_000_000_000L % 60);
        var sub = (int)(absolute % 1_000_000_000L);
        return sign + FormatTimeString(hour, minute, second, sub, second == 0 && sub == 0 ? MinutePrecision : AutoPrecision);
    }

    /// <summary>
    /// An offset as a date-time string writes it: the proposal's <c>FormatDateTimeUTCOffsetRounded</c>, rounded to the
    /// minute half away from zero (row S8: Monrovia's -00:44:30 is <c>-00:45</c>).
    /// </summary>
    /// <param name="offsetNanoseconds">The offset, of magnitude below a day.</param>
    /// <returns><c>±HH:MM</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The offset's magnitude is a day or more.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal FormatDateTimeUTCOffsetRounded via Broiler.VM JsTemporalCore.FormatOffsetRounded; IP=Low; Security=Low; Resources=0; Fingerprint=C667F8
    // Broiler-Human:        PENDING
    public static string FormatDateTimeUtcOffsetRounded(long offsetNanoseconds)
    {
        CheckOffset(offsetNanoseconds);
        var rounded = ExactRounding.RoundNumberToIncrement(offsetNanoseconds, BigInteger.One, new BigInteger(60_000_000_000L), RoundingMode.HalfExpand);
        var minutes = (long)(rounded / 60_000_000_000L);
        var magnitude = Math.Abs(minutes);
        return (minutes >= 0 ? "+" : "-") + Padded((ulong)(magnitude / 60), 2) + ":" + Padded((ulong)(magnitude % 60), 2);
    }

    /// <summary>
    /// <c>Temporal.PlainTime.prototype.toString</c>'s steps after its option reads: <c>RoundTime</c> to the precision's unit
    /// and increment, then <c>TimeRecordToString</c>; a time rounded up to midnight is written as midnight.
    /// </summary>
    /// <param name="time">The time, valid.</param>
    /// <param name="precision">The precision record (<see cref="TemporalOptionRules.ToSecondsStringPrecisionRecord"/>).</param>
    /// <param name="mode">The rounding mode.</param>
    /// <returns>The time's string.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The time is not valid, or the precision record is not one the rule answers.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal.PlainTime.prototype.toString via Broiler.VM JsRealm.TemporalPlainTime; IP=Low; Security=Low; Resources=1; Fingerprint=43758B
    // Broiler-Human:        PENDING
    public static string PlainTimeToString(IsoTime time, SecondsStringPrecision precision, RoundingMode mode)
    {
        var rounded = IsoArithmetic.RoundTime(time, precision.Increment, precision.Unit, mode, out _);
        return TimeToString(rounded, precision.Precision);
    }

    /// <summary>
    /// <c>Temporal.PlainDateTime.prototype.toString</c>'s steps after its option reads: <c>RoundISODateTime</c>, the limit
    /// check, then <c>ISODateTimeToString</c>.
    /// </summary>
    /// <param name="dateTime">The date-time, valid and within the limits.</param>
    /// <param name="calendar">The calendar identifier, canonical.</param>
    /// <param name="precision">The precision record.</param>
    /// <param name="mode">The rounding mode.</param>
    /// <param name="show">The <c>calendarName</c> option.</param>
    /// <param name="text">The string; <see langword="null"/> unless the status is <see cref="TemporalStatus.Completed"/>.</param>
    /// <returns><see cref="TemporalStatus.Completed"/>, or <see cref="TemporalStatus.OutOfRange"/> where the rounded date-time is outside the limits.</returns>
    // Broiler-AI:           Origin=Ported; Spec=Temporal.PlainDateTime.prototype.toString via Broiler.VM JsRealm.TemporalPlainDateTime; IP=Low; Security=Low; Resources=1; Fingerprint=84C549
    // Broiler-Human:        PENDING
    public static TemporalStatus TryPlainDateTimeToString(
        IsoDateTime dateTime, string calendar, SecondsStringPrecision precision, RoundingMode mode, ShowCalendarName show, out string? text)
    {
        text = null;
        var rounded = IsoArithmetic.RoundIsoDateTime(dateTime, precision.Increment, precision.Unit, mode);
        var status = TemporalLimits.CheckIsoDateTime(rounded);
        if (status != TemporalStatus.Completed)
            return status;

        text = DateTimeToString(rounded, calendar, precision.Precision, show);
        return TemporalStatus.Completed;
    }

    /// <summary>
    /// <c>Temporal.Duration.prototype.toString</c>'s steps after its option reads: unrounded where the precision is the
    /// nanosecond's; otherwise the time part rounded with <c>RoundTimeDuration</c>, balanced up to the larger of the
    /// default largest unit and second (<c>TemporalDurationFromInternal</c>), and written.
    /// </summary>
    /// <param name="duration">The duration, valid.</param>
    /// <param name="precision">The precision record, its unit second or smaller.</param>
    /// <param name="mode">The rounding mode.</param>
    /// <param name="text">The string; <see langword="null"/> unless the status is <see cref="TemporalStatus.Completed"/>.</param>
    /// <returns>
    /// <see cref="TemporalStatus.Completed"/>; <see cref="TemporalStatus.OutOfRange"/> where the rounded time duration is
    /// past <see cref="TemporalLimits.MaxTimeDuration"/>; <see cref="TemporalStatus.InvalidDuration"/> where the balanced
    /// duration is not valid. Both are the specification's <c>RangeError</c>.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">The duration is not valid, or the precision's unit is minute or larger.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal.Duration.prototype.toString via Broiler.VM JsRealm.TemporalDuration; IP=Low; Security=Low; Resources=1; Fingerprint=1C86A5
    // Broiler-Human:        PENDING
    public static TemporalStatus TryDurationToString(DurationFields duration, SecondsStringPrecision precision, RoundingMode mode, out string? text)
    {
        text = null;
        if (TemporalLimits.CheckDuration(duration) != TemporalStatus.Completed)
            throw new ArgumentOutOfRangeException(nameof(duration), "The duration is not valid.");
        if (precision.Unit is < TemporalUnit.Second or > TemporalUnit.Nanosecond || precision.Increment < 1)
            throw new ArgumentOutOfRangeException(nameof(precision), "The precision's unit is not second or smaller.");

        if (precision.Unit == TemporalUnit.Nanosecond && precision.Increment == 1)
        {
            text = DurationToString(duration, precision.Precision);
            return TemporalStatus.Completed;
        }

        var largest = DurationCore.DefaultLargestUnit(duration);
        var time = DurationCore.TimeFromComponents(
            duration.Hours, duration.Minutes, duration.Seconds, duration.Milliseconds, duration.Microseconds, duration.Nanoseconds);
        var rounded = ExactRounding.RoundNumberToIncrement(time, BigInteger.One, (BigInteger)DurationCore.UnitLength(precision.Unit) * precision.Increment, mode);
        if (BigInteger.Abs(rounded) > TemporalLimits.MaxTimeDuration)
            return TemporalStatus.OutOfRange;

        var date = new DateDuration(duration.Years, duration.Months, duration.Weeks, duration.Days);
        var balanced = DurationCore.FromInternal(date, rounded, DurationCore.Larger(largest, TemporalUnit.Second));
        if (TemporalLimits.CheckDuration(balanced) != TemporalStatus.Completed)
            return TemporalStatus.InvalidDuration;

        text = DurationToString(balanced, precision.Precision);
        return TemporalStatus.Completed;
    }

    // An ISO date as YYYY-MM-DD.
    // Broiler-AI:           Origin=Ported; Spec=Temporal TemporalDateToString via Broiler.VM JsTemporalCore.DateToString; IP=Low; Security=Low; Resources=0; Fingerprint=0F107A
    // Broiler-Human:        PENDING
    private static string IsoDateString(IsoDate date) =>
        PadIsoYear(date.Year) + "-" + Padded(date.Month, 2) + "-" + Padded(date.Day, 2);

    // A time's nanoseconds within its second.
    // Broiler-AI:           Origin=Ported; Spec=Temporal TimeRecordToString via Broiler.VM JsTemporalCore.SubSecond; IP=Low; Security=Low; Resources=0; Fingerprint=34D0BF
    // Broiler-Human:        PENDING
    private static int SubSecond(IsoTime time) => (time.Millisecond * 1_000_000) + (time.Microsecond * 1000) + time.Nanosecond;

    // A duration field's magnitude and its designator, where the field is not zero.
    // Broiler-AI:           Origin=Ported; Spec=Temporal TemporalDurationToString via Broiler.VM JsTemporalCore.IntegerText; IP=Low; Security=Low; Resources=0; Fingerprint=57AC1B
    // Broiler-Human:        PENDING
    private static void AppendPart(StringBuilder text, double value, char designator)
    {
        if (value != 0)
            text.Append(new BigInteger(Math.Abs(value)).ToString(NumberFormatInfo.InvariantInfo)).Append(designator);
    }

    // A non-negative integer with at least a number of digits.
    // Broiler-AI:           Origin=Ported; Spec=Temporal ToZeroPaddedDecimalString via Broiler.VM JsTemporalCore.Padded; IP=Low; Security=Low; Resources=0; Fingerprint=04E300
    // Broiler-Human:        PENDING
    private static string Padded(ulong value, int width) => value.ToString(NumberFormatInfo.InvariantInfo).PadLeft(width, '0');

    // A field of a valid record, never negative, with at least a number of digits.
    // Broiler-AI:           Origin=Ported; Spec=Temporal ToZeroPaddedDecimalString via Broiler.VM JsTemporalCore.Padded; IP=Low; Security=Low; Resources=0; Fingerprint=653356
    // Broiler-Human:        PENDING
    private static string Padded(int value, int width) => Padded((ulong)Math.Max(value, 0), width);

    // The magnitude of a year, long.MinValue's too.
    // Broiler-AI:           Origin=AI; Spec=Temporal PadISOYear; IP=Low; Security=Low; Resources=0; Fingerprint=8A611D
    // Broiler-Human:        PENDING
    private static ulong Magnitude(long value) => value < 0 ? (ulong)-(value + 1) + 1 : (ulong)value;

    // A valid time's precondition.
    // Broiler-AI:           Origin=AI; Spec=Temporal IsValidTime; IP=Low; Security=Low; Resources=0; Fingerprint=2DCD00
    // Broiler-Human:        PENDING
    private static void CheckTime(IsoTime time)
    {
        if (!TemporalLimits.IsValidTime(time))
            throw new ArgumentOutOfRangeException(nameof(time), "The time is not a valid time.");
    }

    // An offset below a day in magnitude.
    // Broiler-AI:           Origin=AI; Spec=Temporal GetOffsetNanosecondsFor; IP=Low; Security=Low; Resources=0; Fingerprint=D1611A
    // Broiler-Human:        PENDING
    private static void CheckOffset(long offsetNanoseconds)
    {
        if (offsetNanoseconds is <= -TemporalLimits.NanosecondsPerDay or >= TemporalLimits.NanosecondsPerDay)
            throw new ArgumentOutOfRangeException(nameof(offsetNanoseconds), offsetNanoseconds, "An offset is below a day in magnitude.");
    }
}
