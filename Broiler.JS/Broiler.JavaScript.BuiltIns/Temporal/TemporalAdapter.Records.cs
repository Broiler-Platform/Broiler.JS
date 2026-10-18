using System;
using System.Numerics;
using Broiler.ECMAScript.Temporal;
using Broiler.ECMAScript.Time;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Temporal;

/// <summary>
/// This engine's adapter of the shared Temporal core, <c>Broiler.ECMAScript.Temporal</c> (milestone SJL-10.5; the
/// adapter guide <c>adapter-guide-temporal.md</c>): it converts this engine's slots and option strings into the
/// library's records, calls the library, and turns each <see cref="TemporalStatus"/> into this engine's
/// <c>RangeError</c> with the caller's message. It holds no algorithm.
/// </summary>
/// <remarks>
/// <para>
/// The class is split by concern: the records, limits, balancing, regulation and addition here; the grammar in
/// <c>TemporalAdapter.Grammar.cs</c>; serialization and its options in <c>TemporalAdapter.Serialization.cs</c>; the
/// calendars, their identifiers and PrepareCalendarFields' reads in <c>TemporalAdapter.Calendars.cs</c>; the durations,
/// rounding, differences, <c>relativeTo</c> and their options in <c>TemporalAdapter.Durations.cs</c>.
/// </para>
/// <para>
/// <b>The records are SJL-8's</b> <see cref="IsoDate"/>, <see cref="IsoTime"/> and <see cref="IsoDateTime"/>
/// (JSD-0078, decision D8). The <c>JSTemporal*</c> objects keep their <see cref="int"/> slots: every value they hold
/// is within the limits, so it fits, and each hands its slots to the library as a record. Arithmetic is the library's,
/// over <see cref="long"/> years and exact time durations, so nothing narrows before a limit is tested (row R7).
/// </para>
/// </remarks>
internal static partial class TemporalAdapter
{
    /// <summary>Each status but <see cref="TemporalStatus.Completed"/> is this engine's <c>RangeError</c>, with the caller's message.</summary>
    internal static void Check(TemporalStatus status, string message)
    {
        if (status != TemporalStatus.Completed)
            throw JSEngine.NewRangeError(message);
    }

    /// <summary>The <c>overflow</c> option as this engine holds it (<c>"constrain"</c> or <c>"reject"</c>), as the library's.</summary>
    internal static TemporalOverflow Overflow(string overflow) =>
        overflow == "reject" ? TemporalOverflow.Reject : TemporalOverflow.Constrain;

    /// <summary>A unit as this engine names it, singular (<c>"day"</c> … <c>"nanosecond"</c>), as the library's.</summary>
    internal static TemporalUnit Unit(string unit) => unit switch
    {
        "year" => TemporalUnit.Year,
        "month" => TemporalUnit.Month,
        "week" => TemporalUnit.Week,
        "day" => TemporalUnit.Day,
        "hour" => TemporalUnit.Hour,
        "minute" => TemporalUnit.Minute,
        "second" => TemporalUnit.Second,
        "millisecond" => TemporalUnit.Millisecond,
        "microsecond" => TemporalUnit.Microsecond,
        "nanosecond" => TemporalUnit.Nanosecond,
        _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, "Not a Temporal unit."),
    };

    // ── limits ───────────────────────────────────────────────────────────────────

    /// <summary>ISODateWithinLimits of a year, month and day that may not be a date: false for a non-date too.</summary>
    internal static bool IsoDateWithinLimits(long year, int month, int day) =>
        TemporalLimits.CheckIsoDate(new IsoDate(year, month, day)) == TemporalStatus.Completed;

    /// <summary>ISOYearMonthWithinLimits of a year and month: April -271821 to September 275760.</summary>
    internal static bool IsoYearMonthWithinLimits(long year, int month) =>
        TemporalLimits.CheckIsoYearMonth(new IsoDate(year, month, 1)) == TemporalStatus.Completed;

    /// <summary>ISODateTimeWithinLimits of a date-time that may not be one: false for a non-date or non-time too.</summary>
    internal static bool IsoDateTimeWithinLimits(IsoDateTime dateTime) =>
        TemporalLimits.CheckIsoDateTime(dateTime) == TemporalStatus.Completed;

    /// <summary>
    /// IsValidDuration, the test of <c>CreateTemporalDuration</c>: every field finite and of one sign, the years, months
    /// and weeks below 2^32, and the days and time fields together at most <c>maxTimeDuration</c>; otherwise the
    /// <c>RangeError</c>.
    /// </summary>
    internal static void CheckDuration(DurationFields duration) =>
        Check(TemporalLimits.CheckDuration(duration), "Temporal.Duration: the fields must be finite, of one sign, and within the representable range");

    /// <summary>IsValidEpochNanoseconds: whether a count of nanoseconds since the epoch is an instant.</summary>
    internal static bool IsValidEpochNanoseconds(BigInteger epochNanoseconds) =>
        TemporalLimits.CheckEpochNanoseconds(epochNanoseconds) == TemporalStatus.Completed;

    /// <summary>An instant's <c>epochMilliseconds</c>: floor(nanoseconds / 10^6), as a Number.</summary>
    internal static double EpochMilliseconds(BigInteger epochNanoseconds)
    {
        var milliseconds = BigInteger.DivRem(epochNanoseconds, 1_000_000, out var remainder);
        return (double)(remainder.Sign < 0 ? milliseconds - 1 : milliseconds);
    }

    // ── records ──────────────────────────────────────────────────────────────────

    /// <summary>A date-time record of a date's and a time's fields.</summary>
    internal static IsoDateTime IsoDateTimeOf(long year, int month, int day, int hour, int minute, int second, int millisecond, int microsecond, int nanosecond) =>
        new(new IsoDate(year, month, day), new IsoTime(hour, minute, second, millisecond, microsecond, nanosecond));

    /// <summary>
    /// A duration's six time fields as one exact time duration, the date fields left out: the time part of the
    /// proposal's <c>ToInternalDurationRecord</c>. Exact at any magnitude a valid duration has (row R7).
    /// </summary>
    internal static BigInteger TimeDuration(JSTemporalDuration duration) => DurationMath.ToInternal(duration.Fields).TimeNanoseconds;

    /// <summary>
    /// A duration's days and six time fields as one exact time duration, each day 24 hours: the time part of the
    /// proposal's <c>ToInternalDurationRecordWith24HourDays</c>.
    /// </summary>
    internal static BigInteger TimeDurationWith24HourDays(JSTemporalDuration duration) =>
        DurationMath.ToInternalWith24HourDays(duration.Fields).TimeNanoseconds;

    /// <summary>
    /// A year made a <see cref="long"/> with its leap-year rule kept, for a regulation that reads the year only for the
    /// length of February: exact within ±10^9, and reduced modulo 400 beyond, as the shared calendars reduce a
    /// month-day's year (row C12: <c>{year: 2147483652, month: 2, day: 29}</c> is a leap day, not a saturated year's).
    /// </summary>
    internal static long YearForRegulation(double year) => Math.Abs(year) <= 1e9 ? (long)year : (long)(year % 400);

    // ── regulation and addition ──────────────────────────────────────────────────

    /// <summary>
    /// RegulateISODate under the <c>overflow</c> option; a date the fields are not under <c>reject</c> is the
    /// <c>RangeError</c>. The limits are the caller's to test.
    /// </summary>
    internal static IsoDate RegulateIsoDate(long year, double month, double day, string overflow, string message)
    {
        Check(IsoArithmetic.RegulateIsoDate(year, month, day, Overflow(overflow), out var date), message);
        return date;
    }

    /// <summary>RegulateTime under the <c>overflow</c> option; a time the fields are not under <c>reject</c> is the <c>RangeError</c>.</summary>
    internal static IsoTime RegulateTime(
        double hour, double minute, double second, double millisecond, double microsecond, double nanosecond, string overflow, string message)
    {
        Check(IsoArithmetic.RegulateTime(hour, minute, second, millisecond, microsecond, nanosecond, Overflow(overflow), out var time), message);
        return time;
    }

    /// <summary>
    /// The ISO 8601 calendar's CalendarDateAdd: the years and months balanced, the day regulated under the
    /// <c>overflow</c> option, then the weeks and days added, and the result tested against ISODateWithinLimits.
    /// A day the month lacks under <c>reject</c> and a result past the limits are each the <c>RangeError</c>.
    /// </summary>
    /// <param name="date">The date, valid.</param>
    /// <param name="duration">The date duration, each field a finite integer.</param>
    /// <param name="overflow">The <c>overflow</c> option.</param>
    /// <param name="typeName">The type the message names, for example <c>Temporal.PlainDate</c>.</param>
    internal static IsoDate AddIsoDate(IsoDate date, DateDuration duration, string overflow, string typeName)
    {
        var status = IsoArithmetic.AddIsoDate(date, duration, Overflow(overflow), out var result);
        if (status == TemporalStatus.InvalidDate)
            throw JSEngine.NewRangeError($"{typeName}: day out of range for resulting month");
        Check(status, $"{typeName}: result is out of range");
        return result;
    }

    /// <summary>The ISO 8601 calendar's CalendarDateAdd, as <see cref="AddIsoDate(IsoDate, DateDuration, string, string)"/>, over a date's fields.</summary>
    internal static (int Year, int Month, int Day) AddIsoDate(
        int year, int month, int day, long years, long months, long weeks, long days, string overflow, string typeName)
    {
        var result = AddIsoDate(new IsoDate(year, month, day), new DateDuration(years, months, weeks, days), overflow, typeName);
        return ((int)result.Year, result.Month, result.Day);
    }
}
