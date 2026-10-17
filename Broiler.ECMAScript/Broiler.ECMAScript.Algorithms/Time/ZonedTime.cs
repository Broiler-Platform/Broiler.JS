// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   30
// Annotated:        30/30
// Exempt:           9
// Human-reviewed:   0/30
// IP risk:          Low
// Security risk:    High
// Criteria:         5/5
// Resource impact:  2/10 max
// Unverified:       30
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Numerics;

namespace Broiler.ECMAScript.Time;

/// <summary>An ISO Date Record of the Temporal proposal: a year, a month from 1 to 12 and a day of that month.</summary>
/// <param name="Year">The year, at most 2^53 in magnitude.</param>
/// <param name="Month">The month, from 1 to 12.</param>
/// <param name="Day">The day, from 1 to the month's length.</param>
// Broiler-AI:           Origin=Ported; Spec=JSD-0076, Temporal ISO Date Record via Broiler.VM JsIsoDate; IP=Low; Security=Low; Resources=1; Fingerprint=0CD3AF
// Broiler-Human:        PENDING
public readonly record struct IsoDate(long Year, int Month, int Day);

/// <summary>A Time Record of the Temporal proposal with no overflow days: a valid wall-clock time.</summary>
/// <param name="Hour">The hour, from 0 to 23.</param>
/// <param name="Minute">The minute, from 0 to 59.</param>
/// <param name="Second">The second, from 0 to 59.</param>
/// <param name="Millisecond">The millisecond, from 0 to 999.</param>
/// <param name="Microsecond">The microsecond, from 0 to 999.</param>
/// <param name="Nanosecond">The nanosecond, from 0 to 999.</param>
// Broiler-AI:           Origin=Ported; Spec=JSD-0076, Temporal Time Record via Broiler.VM JsTimeRecord; IP=Low; Security=Low; Resources=1; Fingerprint=82E751
// Broiler-Human:        PENDING
public readonly record struct IsoTime(int Hour, int Minute, int Second, int Millisecond, int Microsecond, int Nanosecond);

/// <summary>An ISO Date-Time Record of the Temporal proposal: a wall-clock date and time, in no zone.</summary>
/// <param name="Date">The date.</param>
/// <param name="Time">The time.</param>
// Broiler-AI:           Origin=Ported; Spec=JSD-0076, Temporal ISO Date-Time Record via Broiler.VM JsIsoDateTime; IP=Low; Security=Low; Resources=1; Fingerprint=DBC57C
// Broiler-Human:        PENDING
public readonly record struct IsoDateTime(IsoDate Date, IsoTime Time);

/// <summary>How a wall-clock time a zone repeats or skips is read: the Temporal proposal's <c>disambiguation</c> option.</summary>
// Broiler-AI:           Origin=Ported; Spec=JSD-0076, Temporal DisambiguatePossibleEpochNanoseconds; IP=Low; Security=Low; Resources=1; Fingerprint=191804
// Broiler-Human:        PENDING
public enum Disambiguation
{
    /// <summary><c>"compatible"</c>: a repeated time is its earlier instant and a skipped one is moved forward by the gap, as <c>Date</c> reads them.</summary>
    Compatible = 0,

    /// <summary><c>"earlier"</c>: a repeated time is its earlier instant, and a skipped one is moved back by the gap.</summary>
    Earlier = 1,

    /// <summary><c>"later"</c>: a repeated time is its later instant, and a skipped one is moved forward by the gap.</summary>
    Later = 2,

    /// <summary><c>"reject"</c>: a repeated or a skipped time is refused, with <see cref="TimeStatus.Ambiguous"/> or <see cref="TimeStatus.Skipped"/>.</summary>
    Reject = 3,
}

/// <summary>
/// How a zone or calendar operation ended (JSD-0076's X3: a failure is data). Each engine maps a status
/// other than <see cref="Completed"/> to its own error, a <c>RangeError</c> in both today.
/// </summary>
// Broiler-AI:           Origin=Ported; Spec=JSD-0076 X3; IP=Low; Security=Low; Resources=1; Fingerprint=FC4534
// Broiler-Human:        PENDING
public enum TimeStatus
{
    /// <summary>The operation completed, and its result is valid.</summary>
    Completed = 0,

    /// <summary>
    /// An instant, a date or a year is outside what the operation answers: an epoch-nanoseconds value
    /// outside <see cref="ZonedTime.MinEpochNanoseconds"/> to <see cref="ZonedTime.MaxEpochNanoseconds"/>
    /// (the proposal's <c>IsValidEpochNanoseconds</c>), or a year outside every calendar source.
    /// </summary>
    OutOfRange = 1,

    /// <summary>The wall-clock time names two instants and the disambiguation is <see cref="Disambiguation.Reject"/>.</summary>
    Ambiguous = 2,

    /// <summary>The wall-clock time names no instant, a transition skips it, and the disambiguation is <see cref="Disambiguation.Reject"/>.</summary>
    Skipped = 3,

    /// <summary>
    /// No instant of the day exists: its midnight is skipped and no transition follows within the instant
    /// range. The start of a day never falls back to local midnight.
    /// </summary>
    NoStartOfDay = 4,
}

/// <summary>
/// The Temporal proposal's time-zone operations over a zone's <see cref="ITimeZoneRules"/>: the offset at
/// an instant, the possible instants of a wall-clock time, their disambiguation, the start of a day, the
/// next and previous transitions, and the equality of two zones (JSD-0076, milestone SJL-8.3).
/// </summary>
/// <remarks>
/// <para>
/// <b>Every quantity is exact</b>: instants are <see cref="BigInteger"/> nanoseconds since the epoch, and
/// offsets whole seconds. An offset zone is read through <see cref="FixedOffsetTimeZoneRules"/>; a named
/// zone through its database's rules.
/// </para>
/// <para>
/// <b>The possible instants are found by sampling</b>, as Broiler.VM does: the offsets in force at the
/// wall time read as UTC and a day either side of it, each kept where the instant it implies has that
/// offset, so that data with an offset in force for less than two days still gives every candidate (the
/// correspondence's row A2).
/// </para>
/// <para>
/// <b>A failure is a status</b> (X3): an instant outside the range is <see cref="TimeStatus.OutOfRange"/>,
/// a refused disambiguation <see cref="TimeStatus.Ambiguous"/> or <see cref="TimeStatus.Skipped"/>, and a
/// day with no start <see cref="TimeStatus.NoStartOfDay"/>. A library exception is a broken precondition:
/// an invalid date or time, or rules that break their contract. Nothing here charges a meter: the work is
/// bounded by the zone's data and the instant range (X3).
/// </para>
/// </remarks>
// Broiler-AI:           Origin=Ported; Spec=JSD-0076, Temporal time zone abstract operations via Broiler.VM JsTemporal.Zones; IP=Low; Security=Medium; Resources=1; Fingerprint=9616E9
// Broiler-Human:        PENDING
public static class ZonedTime
{
    /// <summary>The most instants <see cref="GetPossibleEpochNanoseconds"/> can write: one per offset it samples.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0076 A2; IP=Low; Security=Low; Resources=1; Fingerprint=1A3D6F
    // Broiler-Human:        PENDING
    public const int MaxPossibleEpochNanoseconds = 3;

    // Broiler-AI:           Origin=Ported; Spec=JSD-0076 via Broiler.VM JsTemporal.NsPerSecond; IP=Low; Security=Low; Resources=1; Fingerprint=48870E
    // Broiler-Human:        PENDING
    private const long NsPerSecond = 1_000_000_000L;

    // Broiler-AI:           Origin=Ported; Spec=JSD-0076 via Broiler.VM JsTemporalCore.NsPerDay; IP=Low; Security=Low; Resources=1; Fingerprint=52273A
    // Broiler-Human:        PENDING
    private const long NsPerDay = 86_400_000_000_000L;

    // A wall time further than 2 x 10^8 days from the epoch names no instant near the range: it is refused before
    // any sampling, which keeps every instant the rules are asked about within their 10^15 seconds.
    // Broiler-AI:           Origin=Ported; Spec=JSD-0076 X3; IP=Low; Security=Low; Resources=1; Fingerprint=93641F
    // Broiler-Human:        PENDING
    private const long SamplingDayLimit = 200_000_000L;

    /// <summary>The proposal's <c>nsMaxInstant</c>: 10^8 days after the epoch, in nanoseconds.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0076, Temporal nsMaxInstant via Broiler.VM JsTemporalCore.NsMaxInstant; IP=Low; Security=Low; Resources=1; Fingerprint=6FC6E9
    // Broiler-Human:        PENDING
    public static readonly BigInteger MaxEpochNanoseconds = new BigInteger(NsPerDay) * 100_000_000;

    /// <summary>The proposal's <c>nsMinInstant</c>: 10^8 days before the epoch, in nanoseconds.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0076, Temporal nsMinInstant via Broiler.VM JsTemporalCore.NsMinInstant; IP=Low; Security=Low; Resources=1; Fingerprint=038538
    // Broiler-Human:        PENDING
    public static readonly BigInteger MinEpochNanoseconds = -MaxEpochNanoseconds;

    // The rules' bound, 10^15 seconds, in nanoseconds.
    // Broiler-AI:           Origin=Ported; Spec=JSD-0076 X3; IP=Low; Security=Low; Resources=1; Fingerprint=932395
    // Broiler-Human:        PENDING
    private static readonly BigInteger RulesLimitNanoseconds = new BigInteger(TimeZoneDatabase.MaxEpochSeconds) * NsPerSecond;

    /// <summary>
    /// The offset in force at an instant: the proposal's <c>GetOffsetNanosecondsFor</c>, which for a named
    /// zone is ECMA-262's <c>GetNamedTimeZoneOffsetNanoseconds</c> (21.4.1.21) read at the instant's floor second.
    /// </summary>
    /// <param name="rules">The zone's rules.</param>
    /// <param name="epochNanoseconds">The instant, within 10^15 seconds of the epoch.</param>
    /// <returns>The offset in nanoseconds, a whole number of seconds.</returns>
    /// <exception cref="ArgumentNullException">The rules are <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The instant is further than 10^15 seconds from the epoch.</exception>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0076, Temporal GetOffsetNanosecondsFor via Broiler.VM JsTemporal.OffsetNanosecondsFor; IP=Low; Security=Medium; Resources=1; Fingerprint=570A3F
    // Broiler-Human:        PENDING
    public static long GetOffsetNanosecondsFor(ITimeZoneRules rules, BigInteger epochNanoseconds)
    {
        ArgumentNullException.ThrowIfNull(rules);
        return OffsetSeconds(rules, epochNanoseconds) * NsPerSecond;
    }

    /// <summary>
    /// The instants a wall-clock time names in a zone, ascending: the proposal's
    /// <c>GetPossibleEpochNanoseconds</c>, which for a named zone is ECMA-262's
    /// <c>GetNamedTimeZoneEpochNanoseconds</c> (21.4.1.20). One for most times, two for a repeated time,
    /// none for a skipped one.
    /// </summary>
    /// <param name="rules">The zone's rules.</param>
    /// <param name="local">The wall-clock time, a valid ISO date and time.</param>
    /// <param name="destination">Where the instants are written; at least <see cref="MaxPossibleEpochNanoseconds"/> long.</param>
    /// <param name="count">How many were written; zero unless the status is <see cref="TimeStatus.Completed"/>.</param>
    /// <returns>
    /// <see cref="TimeStatus.Completed"/>, or <see cref="TimeStatus.OutOfRange"/> where an instant found is
    /// outside the instant range (the proposal's <c>RangeError</c>); a wall time no instant within a day of
    /// the range could have is refused so too.
    /// </returns>
    /// <exception cref="ArgumentNullException">The rules are <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The destination is shorter than <see cref="MaxPossibleEpochNanoseconds"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The date or the time is not valid, or the year's magnitude is above 2^53.</exception>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0076, Temporal GetPossibleEpochNanoseconds and ECMA-262 21.4.1.20 via Broiler.VM JsTemporal.PossibleEpochNs; IP=Low; Security=High; Resources=1; Fingerprint=57989D
    // Broiler-Falsified-If: an instant the wall time names in the zone is left out, an instant it does not name is written, the instants are not ascending, or an instant outside the range is written with Completed
    // Broiler-Human:        PENDING
    public static TimeStatus GetPossibleEpochNanoseconds(ITimeZoneRules rules, IsoDateTime local, Span<BigInteger> destination, out int count)
    {
        ArgumentNullException.ThrowIfNull(rules);
        if (destination.Length < MaxPossibleEpochNanoseconds)
            throw new ArgumentException("The destination holds fewer than MaxPossibleEpochNanoseconds instants.", nameof(destination));

        CheckDateTime(local);
        return Possible(rules, local, destination, out count);
    }

    /// <summary>
    /// The one instant a wall-clock time is read at, given the instants it names: the proposal's
    /// <c>DisambiguatePossibleEpochNanoseconds</c>.
    /// </summary>
    /// <param name="rules">The zone's rules.</param>
    /// <param name="possible">The instants <see cref="GetPossibleEpochNanoseconds"/> gave for <paramref name="local"/>, ascending.</param>
    /// <param name="local">The wall-clock time, a valid ISO date and time.</param>
    /// <param name="mode">The disambiguation.</param>
    /// <param name="epochNanoseconds">The instant; zero unless the status is <see cref="TimeStatus.Completed"/>.</param>
    /// <returns>
    /// <see cref="TimeStatus.Completed"/>; <see cref="TimeStatus.Ambiguous"/> or <see cref="TimeStatus.Skipped"/>
    /// under <see cref="Disambiguation.Reject"/>; or <see cref="TimeStatus.OutOfRange"/> where the time moved
    /// across a gap names an instant outside the range.
    /// </returns>
    /// <remarks>
    /// In a gap the wall time is moved by the difference of the offsets a day before and a day after it -
    /// back for <see cref="Disambiguation.Earlier"/>, forward otherwise - and its instants are found again.
    /// </remarks>
    /// <exception cref="ArgumentNullException">The rules are <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The date, the time or the mode is not valid.</exception>
    /// <exception cref="InvalidOperationException">The rules name no instant for the wall time moved across a gap, which no zone with less than a day's gap does.</exception>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0076, Temporal DisambiguatePossibleEpochNanoseconds via Broiler.VM JsTemporal.Disambiguate; IP=Low; Security=High; Resources=1; Fingerprint=DD12D0
    // Broiler-Falsified-If: a repeated time is not its earlier instant under compatible or earlier or its later under later, a skipped time is not moved by the gap in the mode's direction, or reject answers an instant for a time that is repeated or skipped
    // Broiler-Human:        PENDING
    public static TimeStatus DisambiguatePossibleEpochNanoseconds(
        ITimeZoneRules rules, ReadOnlySpan<BigInteger> possible, IsoDateTime local, Disambiguation mode, out BigInteger epochNanoseconds)
    {
        ArgumentNullException.ThrowIfNull(rules);
        CheckDateTime(local);
        if (mode is < Disambiguation.Compatible or > Disambiguation.Reject)
            throw new ArgumentOutOfRangeException(nameof(mode), mode, "The disambiguation is not one of the four.");

        return Disambiguate(rules, possible, local, mode, out epochNanoseconds);
    }

    /// <summary>
    /// The instant a wall-clock time is read at in a zone: the proposal's <c>GetEpochNanosecondsFor</c>,
    /// <see cref="GetPossibleEpochNanoseconds"/> then <see cref="DisambiguatePossibleEpochNanoseconds"/>.
    /// </summary>
    /// <param name="rules">The zone's rules.</param>
    /// <param name="local">The wall-clock time, a valid ISO date and time.</param>
    /// <param name="mode">The disambiguation.</param>
    /// <param name="epochNanoseconds">The instant; zero unless the status is <see cref="TimeStatus.Completed"/>.</param>
    /// <returns>The status of the first step that does not complete, or <see cref="TimeStatus.Completed"/>.</returns>
    /// <exception cref="ArgumentNullException">The rules are <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The date, the time or the mode is not valid.</exception>
    /// <exception cref="InvalidOperationException">The rules name no instant for the wall time moved across a gap.</exception>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0076, Temporal GetEpochNanosecondsFor via Broiler.VM JsTemporal.EpochNsFor; IP=Low; Security=Medium; Resources=1; Fingerprint=23508B
    // Broiler-Human:        PENDING
    public static TimeStatus GetEpochNanosecondsFor(ITimeZoneRules rules, IsoDateTime local, Disambiguation mode, out BigInteger epochNanoseconds)
    {
        ArgumentNullException.ThrowIfNull(rules);
        CheckDateTime(local);
        if (mode is < Disambiguation.Compatible or > Disambiguation.Reject)
            throw new ArgumentOutOfRangeException(nameof(mode), mode, "The disambiguation is not one of the four.");

        Span<BigInteger> possible = new BigInteger[MaxPossibleEpochNanoseconds];
        var status = Possible(rules, local, possible, out var count);
        if (status != TimeStatus.Completed)
        {
            epochNanoseconds = BigInteger.Zero;
            return status;
        }

        return Disambiguate(rules, possible[..count], local, mode, out epochNanoseconds);
    }

    /// <summary>
    /// The first instant of a day in a zone: the proposal's <c>GetStartOfDay</c>, midnight's first instant
    /// or, where midnight is skipped, the transition that skips it.
    /// </summary>
    /// <param name="rules">The zone's rules.</param>
    /// <param name="date">The day, a valid ISO date.</param>
    /// <param name="epochNanoseconds">The instant; zero unless the status is <see cref="TimeStatus.Completed"/>.</param>
    /// <returns>
    /// <see cref="TimeStatus.Completed"/>; <see cref="TimeStatus.OutOfRange"/> where midnight's instant is
    /// outside the range; or <see cref="TimeStatus.NoStartOfDay"/> where midnight is skipped and no transition
    /// follows within the range. The start of a day never falls back to local midnight.
    /// </returns>
    /// <exception cref="ArgumentNullException">The rules are <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The date is not valid, or the year's magnitude is above 2^53.</exception>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0076, Temporal GetStartOfDay via Broiler.VM JsTemporal.StartOfDay; IP=Low; Security=High; Resources=1; Fingerprint=515EE5
    // Broiler-Falsified-If: a day whose midnight exists starts at another instant, a day whose midnight is skipped starts other than at the transition that skips it, or a day with no instant answers one
    // Broiler-Human:        PENDING
    public static TimeStatus GetStartOfDay(ITimeZoneRules rules, IsoDate date, out BigInteger epochNanoseconds)
    {
        ArgumentNullException.ThrowIfNull(rules);
        var midnight = new IsoDateTime(date, default);
        CheckDateTime(midnight);

        Span<BigInteger> possible = new BigInteger[MaxPossibleEpochNanoseconds];
        var status = Possible(rules, midnight, possible, out var count);
        if (status != TimeStatus.Completed)
        {
            epochNanoseconds = BigInteger.Zero;
            return status;
        }

        if (count != 0)
        {
            epochNanoseconds = possible[0];
            return TimeStatus.Completed;
        }

        // MIDNIGHT IS IN A GAP: the day starts at the transition, the first instant after it.
        if (NextTransition(rules, UtcEpochNanoseconds(midnight) - NsPerDay) is { } transition)
        {
            epochNanoseconds = transition;
            return TimeStatus.Completed;
        }

        epochNanoseconds = BigInteger.Zero;
        return TimeStatus.NoStartOfDay;
    }

    /// <summary>
    /// The first instant after an instant at which a zone's offset changes: the proposal's
    /// <c>GetNamedTimeZoneNextTransition</c>, whose transitions fall on whole seconds, with no 2037 cliff.
    /// </summary>
    /// <param name="rules">The zone's rules.</param>
    /// <param name="epochNanoseconds">The instant, within 10^15 seconds of the epoch.</param>
    /// <returns>The transition, or <see langword="null"/> where none is at or before <see cref="MaxEpochNanoseconds"/>, as for every offset zone.</returns>
    /// <exception cref="ArgumentNullException">The rules are <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The instant is further than 10^15 seconds from the epoch.</exception>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0076, Temporal GetNamedTimeZoneNextTransition via Broiler.VM JsTemporal.NextTransition; IP=Low; Security=Medium; Resources=2; Fingerprint=973AF4
    // Broiler-Human:        PENDING
    public static BigInteger? GetNextTransition(ITimeZoneRules rules, BigInteger epochNanoseconds)
    {
        ArgumentNullException.ThrowIfNull(rules);
        return NextTransition(rules, epochNanoseconds);
    }

    /// <summary>
    /// The last instant before an instant at which a zone's offset changes: the proposal's
    /// <c>GetNamedTimeZonePreviousTransition</c>; an instant within a second after a transition sees that
    /// transition.
    /// </summary>
    /// <param name="rules">The zone's rules.</param>
    /// <param name="epochNanoseconds">The instant, within 10^15 seconds of the epoch.</param>
    /// <returns>The transition, or <see langword="null"/> where none is at or after <see cref="MinEpochNanoseconds"/>, as for every offset zone.</returns>
    /// <exception cref="ArgumentNullException">The rules are <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The instant is further than 10^15 seconds from the epoch.</exception>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0076, Temporal GetNamedTimeZonePreviousTransition via Broiler.VM JsTemporal.PreviousTransition; IP=Low; Security=Medium; Resources=2; Fingerprint=96A80B
    // Broiler-Human:        PENDING
    public static BigInteger? GetPreviousTransition(ITimeZoneRules rules, BigInteger epochNanoseconds)
    {
        ArgumentNullException.ThrowIfNull(rules);

        // STRICTLY BEFORE: an instant within a second after a transition sees that transition.
        var seconds = FloorSeconds(epochNanoseconds);
        var exact = (seconds * (BigInteger)NsPerSecond) == epochNanoseconds;
        if (rules.PreviousTransition(exact ? seconds : seconds + 1, daylightFlag: false) is not { } found)
            return null;

        var ns = found * (BigInteger)NsPerSecond;
        return ns >= MinEpochNanoseconds ? ns : null;
    }

    /// <summary>
    /// Whether two time zone identifiers name one zone: the proposal's <c>TimeZoneEquals</c> - the same
    /// string, or two named zones with one primary identifier. Two offset zones are equal only as the same
    /// normalised identifier, and an offset zone never equals a named one.
    /// </summary>
    /// <param name="database">The database that names the zones.</param>
    /// <param name="one">A time zone identifier: an offset identifier or an available named one.</param>
    /// <param name="two">Another.</param>
    /// <returns>Whether they are equal.</returns>
    /// <exception cref="ArgumentNullException">The database is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">A named identifier is not available in the database.</exception>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0076, Temporal TimeZoneEquals via Broiler.VM JsTemporal.TimeZoneEquals; IP=Low; Security=Medium; Resources=1; Fingerprint=182599
    // Broiler-Human:        PENDING
    public static bool TimeZoneEquals(ITimeZoneDatabase database, ReadOnlySpan<char> one, ReadOnlySpan<char> two)
    {
        ArgumentNullException.ThrowIfNull(database);
        if (one.SequenceEqual(two))
            return true;

        if (OffsetTimeZones.TryParseOffsetIdentifier(one, out _) || OffsetTimeZones.TryParseOffsetIdentifier(two, out _))
            return false;

        if (!database.TryFind(one, out var first) || !database.TryFind(two, out var second))
            throw new ArgumentException("A time zone identifier is neither an offset nor an available name.");

        return database.Primary(first).Index == database.Primary(second).Index;
    }

    // GetPossibleEpochNanoseconds over a checked wall time.
    // Broiler-AI:           Origin=Ported; Spec=JSD-0076, ECMA-262 21.4.1.20 via Broiler.VM JsTemporal.PossibleEpochNs; IP=Low; Security=High; Resources=1; Fingerprint=F0F2D2
    // Broiler-Falsified-If: a candidate is kept whose instant does not have the offset it was made from, an offset in force a day before or after the wall time is not sampled, or a candidate outside the range completes
    // Broiler-Human:        PENDING
    private static TimeStatus Possible(ITimeZoneRules rules, IsoDateTime local, Span<BigInteger> destination, out int count)
    {
        count = 0;
        var days = IsoCalendarMath.EpochDays(local.Date.Year, local.Date.Month, local.Date.Day);
        if (days is < -SamplingDayLimit or > SamplingDayLimit)
            return TimeStatus.OutOfRange;

        // GETNAMEDTIMEZONEEPOCHNANOSECONDS: each offset in force within a day either side, kept where the
        // instant it implies does have that offset. No day range is checked here: a wall time a day past
        // the limit can still name a valid instant, west of UTC.
        var utc = UtcEpochNanoseconds(local);
        Span<int> offsets = [OffsetSeconds(rules, utc - NsPerDay), OffsetSeconds(rules, utc), OffsetSeconds(rules, utc + NsPerDay)];
        offsets.Sort();

        var found = 0;
        for (var i = 0; i < offsets.Length; i++)
        {
            if (i > 0 && offsets[i] == offsets[i - 1])
                continue;

            var candidate = utc - ((BigInteger)offsets[i] * NsPerSecond);
            if (OffsetSeconds(rules, candidate) == offsets[i])
                destination[found++] = candidate;
        }

        // A LARGER OFFSET IS AN EARLIER INSTANT: the candidates came in descending order.
        destination[..found].Reverse();

        for (var i = 0; i < found; i++)
        {
            if (destination[i] < MinEpochNanoseconds || destination[i] > MaxEpochNanoseconds)
                return TimeStatus.OutOfRange;
        }

        count = found;
        return TimeStatus.Completed;
    }

    // DisambiguatePossibleEpochNanoseconds over a checked wall time and mode.
    // Broiler-AI:           Origin=Ported; Spec=JSD-0076, Temporal DisambiguatePossibleEpochNanoseconds via Broiler.VM JsTemporal.Disambiguate; IP=Low; Security=High; Resources=1; Fingerprint=0AFBEC
    // Broiler-Falsified-If: the gap's two offsets are not read a day before and a day after the wall time, or the moved time's first instant is not answered under earlier and its last otherwise
    // Broiler-Human:        PENDING
    private static TimeStatus Disambiguate(
        ITimeZoneRules rules, ReadOnlySpan<BigInteger> possible, IsoDateTime local, Disambiguation mode, out BigInteger epochNanoseconds)
    {
        epochNanoseconds = BigInteger.Zero;
        if (possible.Length == 1)
        {
            epochNanoseconds = possible[0];
            return TimeStatus.Completed;
        }

        if (possible.Length != 0)
        {
            switch (mode)
            {
                case Disambiguation.Earlier or Disambiguation.Compatible:
                    epochNanoseconds = possible[0];
                    return TimeStatus.Completed;
                case Disambiguation.Later:
                    epochNanoseconds = possible[^1];
                    return TimeStatus.Completed;
                default:
                    return TimeStatus.Ambiguous;
            }
        }

        if (mode == Disambiguation.Reject)
            return TimeStatus.Skipped;

        // A GAP: the offsets before and after it, a day either side.
        var utc = UtcEpochNanoseconds(local);
        var before = (BigInteger)OffsetSeconds(rules, utc - NsPerDay) * NsPerSecond;
        var after = (BigInteger)OffsetSeconds(rules, utc + NsPerDay) * NsPerSecond;
        var nanoseconds = after - before;
        var earlier = mode == Disambiguation.Earlier;
        var moved = BalanceDateTime(local.Date, TimeNanoseconds(local.Time) + (earlier ? -nanoseconds : nanoseconds));

        Span<BigInteger> again = new BigInteger[MaxPossibleEpochNanoseconds];
        var status = Possible(rules, moved, again, out var count);
        if (status != TimeStatus.Completed)
            return status;

        if (count == 0)
            throw new InvalidOperationException("The zone's rules skip the wall time moved across a gap as well.");

        epochNanoseconds = earlier ? again[0] : again[count - 1];
        return TimeStatus.Completed;
    }

    // GetNamedTimeZoneNextTransition, bounded by nsMaxInstant.
    // Broiler-AI:           Origin=Ported; Spec=JSD-0076, Temporal GetNamedTimeZoneNextTransition via Broiler.VM JsTemporal.NextTransition; IP=Low; Security=Medium; Resources=2; Fingerprint=165715
    // Broiler-Human:        PENDING
    private static BigInteger? NextTransition(ITimeZoneRules rules, BigInteger epochNanoseconds)
    {
        // TRANSITIONS FALL ON WHOLE SECONDS: the next after the instant is the next after its second.
        var seconds = FloorSeconds(epochNanoseconds);
        var limitSeconds = (long)(MaxEpochNanoseconds / NsPerSecond);
        if (rules.NextTransition(seconds, limitSeconds, daylightFlag: false) is not { } found)
            return null;

        var ns = found * (BigInteger)NsPerSecond;
        return ns <= MaxEpochNanoseconds ? ns : null;
    }

    // The offset in seconds at an instant's floor second.
    // Broiler-AI:           Origin=Ported; Spec=JSD-0076, ECMA-262 21.4.1.21 via Broiler.VM JsTemporal.NamedOffsetSeconds; IP=Low; Security=Medium; Resources=1; Fingerprint=3DF0E3
    // Broiler-Human:        PENDING
    private static int OffsetSeconds(ITimeZoneRules rules, BigInteger epochNanoseconds)
    {
        var offset = rules.OffsetSecondsAt(FloorSeconds(epochNanoseconds));
        if (offset is <= -86400 or >= 86400)
            throw new InvalidOperationException("The zone's rules answered an offset of a day or more.");

        return offset;
    }

    // The floor of an instant in seconds, for an instant within the rules' 10^15 seconds.
    // Broiler-AI:           Origin=Ported; Spec=JSD-0076 via Broiler.VM JsTemporal.FloorSeconds; IP=Low; Security=Medium; Resources=1; Fingerprint=90EA09
    // Broiler-Human:        PENDING
    private static long FloorSeconds(BigInteger epochNanoseconds)
    {
        if (BigInteger.Abs(epochNanoseconds) > RulesLimitNanoseconds)
            throw new ArgumentOutOfRangeException(nameof(epochNanoseconds), epochNanoseconds, "An instant is at most 10^15 seconds from the epoch.");

        var seconds = BigInteger.DivRem(epochNanoseconds, NsPerSecond, out var remainder);
        if (remainder.Sign < 0)
            seconds -= 1;

        return (long)seconds;
    }

    // GetUTCEpochNanoseconds (ECMA-262 21.4.1.18) of a wall time read as UTC.
    // Broiler-AI:           Origin=Ported; Spec=JSD-0076, ECMA-262 21.4.1.18 via Broiler.VM JsTemporalCore.UtcEpochNs; IP=Low; Security=Low; Resources=1; Fingerprint=066518
    // Broiler-Human:        PENDING
    private static BigInteger UtcEpochNanoseconds(IsoDateTime local) =>
        ((BigInteger)IsoCalendarMath.EpochDays(local.Date.Year, local.Date.Month, local.Date.Day) * NsPerDay) + TimeNanoseconds(local.Time);

    // A time's nanoseconds since midnight.
    // Broiler-AI:           Origin=Ported; Spec=JSD-0076 via Broiler.VM JsTemporalCore.TimeNs; IP=Low; Security=Low; Resources=1; Fingerprint=E4E2E6
    // Broiler-Human:        PENDING
    private static BigInteger TimeNanoseconds(IsoTime time) =>
        (((((((BigInteger)time.Hour * 60) + time.Minute) * 60) + time.Second) * 1000 + time.Millisecond) * 1000 + time.Microsecond) * 1000 +
        time.Nanosecond;

    // The proposal's BalanceISODateTime, the time given in nanoseconds from the date's midnight.
    // Broiler-AI:           Origin=Ported; Spec=JSD-0076, Temporal BalanceISODateTime via Broiler.VM JsTemporalCore.BalanceDateTime; IP=Low; Security=Low; Resources=1; Fingerprint=67529E
    // Broiler-Human:        PENDING
    private static IsoDateTime BalanceDateTime(IsoDate date, BigInteger nanoseconds)
    {
        var days = BigInteger.DivRem(nanoseconds, NsPerDay, out var within);
        if (within.Sign < 0)
        {
            days -= 1;
            within += NsPerDay;
        }

        var ns = (long)within;
        var (year, month, day) = IsoCalendarMath.FromEpochDays(IsoCalendarMath.EpochDays(date.Year, date.Month, date.Day) + (long)days);
        var time = new IsoTime(
            (int)(ns / 3_600_000_000_000L),
            (int)(ns / 60_000_000_000L % 60),
            (int)(ns / 1_000_000_000L % 60),
            (int)(ns / 1_000_000L % 1000),
            (int)(ns / 1000 % 1000),
            (int)(ns % 1000));
        return new IsoDateTime(new IsoDate(year, month, day), time);
    }

    // A wall time's precondition: a valid ISO date and a valid time.
    // Broiler-AI:           Origin=Ported; Spec=JSD-0076, Temporal IsValidISODate and IsValidTime; IP=Low; Security=Low; Resources=1; Fingerprint=9BBF14
    // Broiler-Human:        PENDING
    private static void CheckDateTime(IsoDateTime local)
    {
        var (date, time) = (local.Date, local.Time);
        if (!IsoCalendarMath.IsValidIsoDate(date.Year, date.Month, date.Day))
            throw new ArgumentOutOfRangeException(nameof(local), "The date is not a valid ISO date.");

        if (time.Hour is < 0 or > 23 || time.Minute is < 0 or > 59 || time.Second is < 0 or > 59 ||
            time.Millisecond is < 0 or > 999 || time.Microsecond is < 0 or > 999 || time.Nanosecond is < 0 or > 999)
            throw new ArgumentOutOfRangeException(nameof(local), "The time is not a valid time.");
    }
}
