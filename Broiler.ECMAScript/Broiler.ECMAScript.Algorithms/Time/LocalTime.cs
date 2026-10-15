// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   12
// Annotated:        12/12
// Exempt:           3
// Human-reviewed:   0/12
// IP risk:          Low
// Security risk:    High
// Criteria:         1/1
// Resource impact:  1/10 max
// Unverified:       12
//
// GENERATED - DO NOT EDIT MANUALLY

using System;

namespace Broiler.ECMAScript.Time;

/// <summary>
/// The local time zone of an engine: the host policy of decision SJL-D-19, which the library defines and
/// each engine supplies. It answers, for an instant, the offset of local time from UTC and the zone's
/// name; <see cref="EcmaLocalTime"/> computes ECMA-262's <c>LocalTime</c> and <c>UTC</c> over it.
/// </summary>
/// <remarks>
/// <para>
/// <b>The contract.</b> For every finite Number of milliseconds since the epoch, a time value or not,
/// <see cref="GetOffsetMilliseconds"/> answers <c>truncate(GetNamedTimeZoneOffsetNanoseconds(ℤ(ℝ(t) ×
/// 10^6)) / 10^6)</c> (ECMA-262 21.4.1.21 and 21.4.1.25): an integral Number of milliseconds of magnitude
/// below one day, the same for the same instant on every call. The zone's offset changes at most once in
/// any two days, which every zone of the IANA Time Zone Database does; <see cref="EcmaLocalTime.Utc"/>
/// finds a local time's possible instants from the offsets one day either side of it.
/// </para>
/// <para>
/// <b>A provider is the engine's own</b>: Broiler.VM's is UTC, <see cref="FixedOffsetLocalTimeProvider"/> at
/// zero; Broiler.JS's reads its host's zone. The library reads no zone of its own (contract convention 9).
/// </para>
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.1.25-26; IP=Low; Security=Medium; Resources=1; Fingerprint=B5630F
// Broiler-Human:        PENDING
public interface ILocalTimeProvider
{
    /// <summary>
    /// The offset of local time from UTC in force at an instant, truncated to milliseconds:
    /// <c>truncate(GetNamedTimeZoneOffsetNanoseconds(ℤ(ℝ(t) × 10^6)) / 10^6)</c> (ECMA-262 21.4.1.21).
    /// </summary>
    /// <param name="epochMilliseconds">The instant, a finite Number of milliseconds since the epoch.</param>
    /// <returns>An integral Number of milliseconds, of magnitude below 86,400,000, positive east of UTC.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.1.21; IP=Low; Security=Medium; Resources=1; Fingerprint=21E1F4
    // Broiler-Human:        PENDING
    double GetOffsetMilliseconds(double epochMilliseconds);

    /// <summary>
    /// The zone's name at an instant, which <c>TimeZoneString</c> (ECMA-262 21.4.4.41.3) writes in
    /// parentheses after the offset: the implementation-defined timezone name of its <c>tzName</c>.
    /// </summary>
    /// <param name="epochMilliseconds">The instant, a time value.</param>
    /// <returns>The name, or <see langword="null"/> for a <c>tzName</c> that is the empty String.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.4.41.3; IP=Low; Security=Low; Resources=1; Fingerprint=3A6A76
    // Broiler-Human:        PENDING
    string? GetName(double epochMilliseconds);
}

/// <summary>
/// A local time zone at a fixed offset with no transition: ECMA-262's time zone of an offset string
/// (<c>IsTimeZoneOffsetString</c>, 21.4.1.33), and Broiler.VM's UTC at an offset of zero.
/// </summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.1.33; IP=Low; Security=Low; Resources=1; Fingerprint=417C7E
// Broiler-Human:        PENDING
public sealed class FixedOffsetLocalTimeProvider : ILocalTimeProvider
{
    private readonly double offset;

    private readonly string? name;

    /// <summary>Creates the zone.</summary>
    /// <param name="offsetMilliseconds">The offset, an integral Number of milliseconds of magnitude below 86,400,000, positive east of UTC.</param>
    /// <param name="name">The zone's name for <c>TimeZoneString</c>, or <see langword="null"/> for none.</param>
    /// <exception cref="ArgumentOutOfRangeException">The offset is not integral, or its magnitude is a day or more.</exception>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.1.33; IP=Low; Security=Low; Resources=1; Fingerprint=DF38DE
    // Broiler-Human:        PENDING
    public FixedOffsetLocalTimeProvider(double offsetMilliseconds, string? name)
    {
        EcmaLocalTime.CheckOffset(offsetMilliseconds, nameof(offsetMilliseconds));
        offset = offsetMilliseconds == 0 ? 0 : offsetMilliseconds;
        this.name = name;
    }

    /// <summary>The fixed offset, whatever the instant (ECMA-262 21.4.1.33's <c>ParseTimeZoneOffsetString</c>, in milliseconds).</summary>
    /// <param name="epochMilliseconds">The instant.</param>
    /// <returns>The offset this zone was created with.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.1.33; IP=Low; Security=Low; Resources=1; Fingerprint=194543
    // Broiler-Human:        PENDING
    public double GetOffsetMilliseconds(double epochMilliseconds) => offset;

    /// <summary>The fixed name, whatever the instant (ECMA-262 21.4.4.41.3's implementation-defined timezone name).</summary>
    /// <param name="epochMilliseconds">The instant.</param>
    /// <returns>The name this zone was created with, or <see langword="null"/>.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.4.41.3; IP=Low; Security=Low; Resources=1; Fingerprint=C9E2D3
    // Broiler-Human:        PENDING
    public string? GetName(double epochMilliseconds) => name;
}

/// <summary>ECMA-262's conversions between UTC and local time over the engine's <see cref="ILocalTimeProvider"/>.</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.1.25-26; IP=Low; Security=Medium; Resources=1; Fingerprint=FDA9A3
// Broiler-Human:        PENDING
public static class EcmaLocalTime
{
    /// <summary><c>LocalTime</c> (ECMA-262 21.4.1.25): <c>t + 𝔽(offsetMs)</c>, the offset in force at the instant <c>t</c>.</summary>
    /// <param name="t">A time value.</param>
    /// <param name="zone">The local time zone.</param>
    /// <returns>The local time; NaN for a NaN or an infinite <paramref name="t"/>.</returns>
    /// <exception cref="ArgumentNullException">The zone is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The zone answered an offset that breaks its contract.</exception>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.1.25; IP=Low; Security=Medium; Resources=1; Fingerprint=13223A
    // Broiler-Human:        PENDING
    public static double LocalTime(double t, ILocalTimeProvider zone)
    {
        ArgumentNullException.ThrowIfNull(zone);
        if (!double.IsFinite(t))
            return double.NaN;

        return t + Offset(zone, t);
    }

    /// <summary>
    /// <c>UTC</c> (ECMA-262 21.4.1.26): <c>t - 𝔽(offsetMs)</c>, a local time read at the offset of the
    /// earliest instant it names, or, for a local time a transition skips, at the offset before the
    /// transition.
    /// </summary>
    /// <param name="t">A local time, a Number.</param>
    /// <param name="zone">The local time zone.</param>
    /// <returns>The time, not yet clipped; NaN for a NaN or an infinite <paramref name="t"/>.</returns>
    /// <remarks>
    /// <c>GetNamedTimeZoneEpochNanoseconds</c> (21.4.1.20) is found from the offsets one day before and one
    /// day after <paramref name="t"/>, read as instants: each offset whose instant <c>t - offset</c> keeps
    /// it is a possible instant. With one possible instant, that is it; with two, a repeated local time,
    /// the earlier; with none, a skipped one, the offset before the transition, which is the offset of the
    /// last instant of the last local time before it.
    /// </remarks>
    /// <exception cref="ArgumentNullException">The zone is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The zone answered an offset that breaks its contract.</exception>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.1.26; IP=Low; Security=High; Resources=1; Fingerprint=708CAB
    // Broiler-Falsified-If: a repeated local time is read at a later instant than its earliest, a skipped local time is read at an offset other than the one before its transition, or a local time with one instant is read at another
    // Broiler-Human:        PENDING
    public static double Utc(double t, ILocalTimeProvider zone)
    {
        ArgumentNullException.ThrowIfNull(zone);
        if (!double.IsFinite(t))
            return double.NaN;

        var before = Offset(zone, t - EcmaTime.MsPerDay);
        var after = Offset(zone, t + EcmaTime.MsPerDay);

        // The larger offset gives the earlier instant, so it is tried first.
        var (first, second) = before >= after ? (before, after) : (after, before);
        if (Offset(zone, t - first) == first)
            return t - first;
        if (second != first && Offset(zone, t - second) == second)
            return t - second;

        return t - before;
    }

    // The provider's offset at an instant, held to its contract.
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.1.25; IP=Low; Security=Medium; Resources=1; Fingerprint=6BE133
    // Broiler-Human:        PENDING
    private static double Offset(ILocalTimeProvider zone, double epochMilliseconds)
    {
        var offset = zone.GetOffsetMilliseconds(epochMilliseconds);
        if (!double.IsFinite(offset) || Math.Floor(offset) != offset || Math.Abs(offset) >= EcmaTime.MsPerDay)
            throw new InvalidOperationException("The local time zone answered an offset that is not an integral number of milliseconds below a day.");

        return offset == 0 ? 0 : offset;
    }

    // A fixed offset's precondition: integral and below a day.
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.1.33; IP=Low; Security=Low; Resources=1; Fingerprint=602D1E
    // Broiler-Human:        PENDING
    internal static void CheckOffset(double offset, string name)
    {
        if (!double.IsFinite(offset) || Math.Floor(offset) != offset || Math.Abs(offset) >= EcmaTime.MsPerDay)
            throw new ArgumentOutOfRangeException(name, offset, "An offset is an integral number of milliseconds below a day.");
    }

    // The offset TimeZoneString writes, held to the contract.
    internal static double OffsetFor(ILocalTimeProvider zone, double tv) => Offset(zone, tv);
}
