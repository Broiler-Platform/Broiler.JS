// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   4
// Annotated:        4/4
// Exempt:           0
// Human-reviewed:   0/4
// IP risk:          None
// Security risk:    Low
// Criteria:         0/0
// Resource impact:  0/10 max
// Unverified:       4
//
// GENERATED - DO NOT EDIT MANUALLY

using System.Numerics;
using Broiler.ECMAScript.Time;

namespace Broiler.ECMAScript.Temporal;

/// <summary>
/// One time zone's three operations the zoned arithmetic, the relative rounding and the zoned differences call (the
/// area design's <c>ITimeZoneArithmetic</c>; JSD-0078's decision 1: "calendars and zones are reached through provider
/// interfaces"): <c>GetOffsetNanosecondsFor</c> (Temporal s11.1.9), <c>GetEpochNanosecondsFor</c> (s11.1.11) and
/// <c>GetStartOfDay</c> (s11.1.14). <see cref="TemporalTimeZone"/> implements it over SJL-8's zone algorithms.
/// </summary>
/// <remarks>
/// <para>
/// <b>An instance is one zone</b>, a named zone's rules or an offset zone's, as an engine's
/// <c>ZonedDateTime</c> holds one: the engine resolves its identifier once (SJL-8's <see cref="TimeZoneDatabase"/>
/// or <see cref="OffsetTimeZones"/>) and hands the library the zone. These are the callbacks Broiler.VM's
/// <c>JsTemporal.Durations</c> makes - <c>IsoDateTimeFor</c>, which is the offset, and <c>EpochNsFor</c> - and
/// Broiler.JS's zoned code makes through its zone object.
/// </para>
/// <para>
/// Each member answers a <see cref="TimeStatus"/>, never an exception, for an input an engine can reach; the library
/// answers <see cref="TemporalStatus.OutOfRange"/> for a status other than <see cref="TimeStatus.Completed"/>, which
/// each engine maps to its <c>RangeError</c>. Every member must be safe to call concurrently.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=Temporal s11.1.9, s11.1.11 and s11.1.14, JSD-0078 decision 1; IP=None; Security=Low; Resources=0; Fingerprint=1546C9
// Broiler-Human:        PENDING
public interface ITimeZoneArithmetic
{
    /// <summary>The zone's offset from UTC at an instant: the proposal's <c>GetOffsetNanosecondsFor</c>.</summary>
    /// <param name="epochNanoseconds">The instant, a valid epoch nanosecond count.</param>
    /// <returns>The offset, below a day in magnitude.</returns>
    // Broiler-AI:           Origin=AI; Spec=Temporal s11.1.9; IP=None; Security=Low; Resources=0; Fingerprint=ADC661
    // Broiler-Human:        PENDING
    long GetOffsetNanosecondsFor(BigInteger epochNanoseconds);

    /// <summary>
    /// The instant a wall-clock date-time names in the zone: the proposal's <c>GetEpochNanosecondsFor</c>, its possible
    /// instants disambiguated.
    /// </summary>
    /// <param name="dateTime">The date-time, valid, at most a day past the date-time limits.</param>
    /// <param name="disambiguation">How a time the zone repeats or skips is resolved.</param>
    /// <param name="epochNanoseconds">The instant; zero unless the status is <see cref="TimeStatus.Completed"/>.</param>
    /// <returns>
    /// <see cref="TimeStatus.Completed"/>; <see cref="TimeStatus.OutOfRange"/> where the instant is outside the range;
    /// <see cref="TimeStatus.Ambiguous"/> or <see cref="TimeStatus.Skipped"/> under <see cref="Disambiguation.Reject"/>.
    /// </returns>
    // Broiler-AI:           Origin=AI; Spec=Temporal s11.1.11; IP=None; Security=Low; Resources=0; Fingerprint=FF236B
    // Broiler-Human:        PENDING
    TimeStatus GetEpochNanosecondsFor(IsoDateTime dateTime, Disambiguation disambiguation, out BigInteger epochNanoseconds);

    /// <summary>The first instant of a day in the zone: the proposal's <c>GetStartOfDay</c>.</summary>
    /// <param name="date">The day, a valid ISO date at most a day past the date limits.</param>
    /// <param name="epochNanoseconds">The instant; zero unless the status is <see cref="TimeStatus.Completed"/>.</param>
    /// <returns>
    /// <see cref="TimeStatus.Completed"/>; <see cref="TimeStatus.OutOfRange"/> where the instant is outside the range;
    /// <see cref="TimeStatus.NoStartOfDay"/> where midnight is skipped and no transition follows within the range.
    /// </returns>
    // Broiler-AI:           Origin=AI; Spec=Temporal s11.1.14; IP=None; Security=Low; Resources=0; Fingerprint=47D224
    // Broiler-Human:        PENDING
    TimeStatus GetStartOfDay(IsoDate date, out BigInteger epochNanoseconds);
}
