// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   5
// Annotated:        5/5
// Exempt:           1
// Human-reviewed:   0/5
// IP risk:          None
// Security risk:    Low
// Criteria:         0/0
// Resource impact:  1/10 max
// Unverified:       5
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Numerics;
using Broiler.ECMAScript.Time;

namespace Broiler.ECMAScript.Temporal;

/// <summary>
/// A time zone as the Temporal core computes through it: <see cref="ITimeZoneArithmetic"/> over a zone's
/// <see cref="ITimeZoneRules"/> - a named zone's from SJL-8's <see cref="TimeZoneDatabase"/>, or an offset zone's
/// <see cref="FixedOffsetTimeZoneRules"/> - each member SJL-8's <see cref="ZonedTime"/> operation.
/// </summary>
/// <remarks>
/// The instance is immutable, and every member is safe to call concurrently where the rules are, as SJL-8's are.
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=Temporal s11.1, JSD-0078 decision 1 over JSD-0076; IP=None; Security=Low; Resources=0; Fingerprint=CFDE55
// Broiler-Human:        PENDING
public sealed class TemporalTimeZone : ITimeZoneArithmetic
{
    /// <summary>Creates the zone over its rules.</summary>
    /// <param name="rules">The zone's rules.</param>
    /// <exception cref="ArgumentNullException">The rules are <see langword="null"/>.</exception>
    // Broiler-AI:           Origin=AI; Spec=JSD-0078 decision 1; IP=None; Security=Low; Resources=0; Fingerprint=1A3F61
    // Broiler-Human:        PENDING
    public TemporalTimeZone(ITimeZoneRules rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        Rules = rules;
    }

    /// <summary>The zone's rules.</summary>
    public ITimeZoneRules Rules { get; }

    /// <inheritdoc/>
    // Broiler-AI:           Origin=AI; Spec=Temporal s11.1.9 over JSD-0076 ZonedTime; IP=None; Security=Low; Resources=1; Fingerprint=EE53EA
    // Broiler-Human:        PENDING
    public long GetOffsetNanosecondsFor(BigInteger epochNanoseconds) => ZonedTime.GetOffsetNanosecondsFor(Rules, epochNanoseconds);

    /// <inheritdoc/>
    // Broiler-AI:           Origin=AI; Spec=Temporal s11.1.11 over JSD-0076 ZonedTime; IP=None; Security=Low; Resources=1; Fingerprint=715B7E
    // Broiler-Human:        PENDING
    public TimeStatus GetEpochNanosecondsFor(IsoDateTime dateTime, Disambiguation disambiguation, out BigInteger epochNanoseconds) =>
        ZonedTime.GetEpochNanosecondsFor(Rules, dateTime, disambiguation, out epochNanoseconds);

    /// <inheritdoc/>
    // Broiler-AI:           Origin=AI; Spec=Temporal s11.1.14 over JSD-0076 ZonedTime; IP=None; Security=Low; Resources=1; Fingerprint=AE0A95
    // Broiler-Human:        PENDING
    public TimeStatus GetStartOfDay(IsoDate date, out BigInteger epochNanoseconds) => ZonedTime.GetStartOfDay(Rules, date, out epochNanoseconds);
}
