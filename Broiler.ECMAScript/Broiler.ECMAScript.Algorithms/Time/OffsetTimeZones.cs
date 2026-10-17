// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   10
// Annotated:        10/10
// Exempt:           4
// Human-reviewed:   0/10
// IP risk:          Low
// Security risk:    High
// Criteria:         3/3
// Resource impact:  1/10 max
// Unverified:       10
//
// GENERATED - DO NOT EDIT MANUALLY

using System;

namespace Broiler.ECMAScript.Time;

/// <summary>
/// The offset time zones: ECMA-262's UTC offset strings (21.4.1.33), the offset time zone identifiers the
/// Temporal proposal and ECMA-402 normalise them to, and IANA's <c>Etc/GMT±n</c> zones, which are fixed
/// offsets by definition.
/// </summary>
/// <remarks>
/// <b>Digits are ASCII only</b>: the grammar's <c>DecimalDigit</c> is one of <c>0</c> to <c>9</c>, so an
/// offset written with other decimal digits is not an offset string (the correspondence's row A13), and
/// a caller that also finds no named zone throws its <c>RangeError</c>.
/// </remarks>
// Broiler-AI:           Origin=Ported; Spec=JSD-0076, ECMA-262 21.4.1.33 via Broiler.VM JsRealm.DateTimeFormat.ParseOffset; IP=Low; Security=Medium; Resources=1; Fingerprint=46E20D
// Broiler-Human:        PENDING
public static class OffsetTimeZones
{
    /// <summary>
    /// Whether a string is a UTC offset string: <c>IsTimeZoneOffsetString</c> (ECMA-262 21.4.1.33.1), the
    /// <c>UTCOffset</c> grammar with seconds and a fraction allowed - a sign, two-digit hours, and optionally
    /// minutes, seconds and up to nine fraction digits, all in the extended form or all in the basic one.
    /// </summary>
    /// <param name="text">The string.</param>
    /// <returns>Whether the whole string is a <c>UTCOffset</c>.</returns>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0076, ECMA-262 21.4.1.33.1 via Broiler.VM JsRealm.DateTimeFormat.ParseOffset; IP=Low; Security=High; Resources=1; Fingerprint=5FE091
    // Broiler-Falsified-If: a string outside the UTCOffset grammar, one with a digit outside 0 to 9 among them, is accepted, or one of the grammar is refused
    // Broiler-Human:        PENDING
    public static bool IsTimeZoneOffsetString(ReadOnlySpan<char> text) => Parse(text, out _, out _);

    /// <summary>
    /// Reads an offset time zone identifier: <c>UTCOffset[~SubMinutePrecision]</c>, the offset branch of the
    /// Temporal proposal's <c>ParseTimeZoneIdentifier</c> - a sign, two-digit hours from 00 to 23 and
    /// optionally minutes from 00 to 59, extended (<c>+05:30</c>) or basic (<c>+0530</c>), with no seconds.
    /// </summary>
    /// <param name="text">The identifier.</param>
    /// <param name="offsetMinutes">The offset in minutes, positive east of UTC; zero when the text is not one.</param>
    /// <returns>Whether the whole string is an offset identifier; <see langword="false"/> for an offset string with seconds, which ECMA-402's <c>CreateDateTimeFormat</c> and Temporal both refuse as a zone.</returns>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0076, Temporal ParseTimeZoneIdentifier via Broiler.VM JsTemporal.ParseZone; IP=Low; Security=High; Resources=1; Fingerprint=203719
    // Broiler-Falsified-If: an identifier with seconds, with hours above 23 or minutes above 59, with a digit outside 0 to 9, or mixing the extended and basic forms is accepted, or the minutes of an accepted one are not its sign times its hours and minutes
    // Broiler-Human:        PENDING
    public static bool TryParseOffsetIdentifier(ReadOnlySpan<char> text, out int offsetMinutes)
    {
        if (Parse(text, out var minutes, out var hasSeconds) && !hasSeconds)
        {
            offsetMinutes = minutes;
            return true;
        }

        offsetMinutes = 0;
        return false;
    }

    /// <summary>
    /// The offset time zone identifier of an offset: <c>FormatOffsetTimeZoneIdentifier</c> of the Temporal
    /// proposal, <c>±HH:MM</c>, the sign <c>+</c> for zero.
    /// </summary>
    /// <param name="offsetMinutes">The offset in minutes, of magnitude below a day.</param>
    /// <returns>The identifier, for example <c>-05:30</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The offset's magnitude is a day or more.</exception>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0076, Temporal FormatOffsetTimeZoneIdentifier via Broiler.VM JsTemporalCore.FormatOffsetMinutes; IP=Low; Security=Low; Resources=1; Fingerprint=4FA022
    // Broiler-Human:        PENDING
    public static string FormatOffsetIdentifier(int offsetMinutes)
    {
        if (offsetMinutes is <= -1440 or >= 1440)
            throw new ArgumentOutOfRangeException(nameof(offsetMinutes), offsetMinutes, "An offset is below a day.");

        return string.Create(6, offsetMinutes, static (span, minutes) =>
        {
            var magnitude = Math.Abs(minutes);
            span[0] = minutes >= 0 ? '+' : '-';
            span[1] = (char)('0' + (magnitude / 600));
            span[2] = (char)('0' + (magnitude / 60 % 10));
            span[3] = ':';
            span[4] = (char)('0' + (magnitude % 60 / 10));
            span[5] = (char)('0' + (magnitude % 10));
        });
    }

    /// <summary>
    /// The fixed offset of one of IANA's <c>Etc/GMT+1</c> to <c>Etc/GMT+12</c> and <c>Etc/GMT-1</c> to
    /// <c>Etc/GMT-14</c>, with POSIX's inverted sign: the zones ECMA-402's <c>Intl.DateTimeFormat</c> formats
    /// as offsets in Broiler.VM (JSD-0053; the correspondence's row A14).
    /// </summary>
    /// <param name="primaryIdentifier">A primary identifier, in the database's case, matched ordinally.</param>
    /// <param name="offsetMinutes">The offset in minutes, positive east of UTC; zero when the identifier is not one of them.</param>
    /// <returns>Whether the identifier is one of the 26 zones.</returns>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0076 A14 via Broiler.VM JsRealm.DateTimeFormat.EtcGmtZones; IP=Low; Security=Medium; Resources=1; Fingerprint=32AB74
    // Broiler-Human:        PENDING
    public static bool TryGetEtcGmtOffset(ReadOnlySpan<char> primaryIdentifier, out int offsetMinutes)
    {
        offsetMinutes = 0;
        const string Prefix = "Etc/GMT";
        if (!primaryIdentifier.StartsWith(Prefix, StringComparison.Ordinal) || primaryIdentifier.Length is < 9 or > 10)
            return false;

        var sign = primaryIdentifier[Prefix.Length];
        var digits = primaryIdentifier[(Prefix.Length + 1)..];
        if (sign is not ('+' or '-') || digits[0] is < '1' or > '9' || (digits.Length == 2 && digits[1] is < '0' or > '9'))
            return false;

        var hours = digits.Length == 1 ? digits[0] - '0' : ((digits[0] - '0') * 10) + (digits[1] - '0');
        if (hours > (sign == '+' ? 12 : 14))
            return false;

        offsetMinutes = sign == '+' ? -hours * 60 : hours * 60;
        return true;
    }

    // ECMA-262's UTCOffset grammar: a sign, two-digit hours, and optionally minutes and seconds, all extended or
    // all basic, and after seconds a fraction of one to nine digits.
    // Broiler-AI:           Origin=Ported; Spec=JSD-0076, ECMA-262 21.4.1.33 via Broiler.VM JsRealm.DateTimeFormat.ParseOffset; IP=Low; Security=High; Resources=1; Fingerprint=5DD51E
    // Broiler-Falsified-If: a string outside the UTCOffset grammar is read as an offset, one of it is refused, or the seconds flag or the minutes read differ from the string's
    // Broiler-Human:        PENDING
    private static bool Parse(ReadOnlySpan<char> text, out int minutes, out bool hasSeconds)
    {
        minutes = 0;
        hasSeconds = false;
        if (text.Length < 3 || text[0] is not ('+' or '-'))
            return false;

        if (!TwoDigits(text, 1, 23, out var hours))
            return false;

        var at = 3;
        var minute = 0;
        if (at < text.Length)
        {
            var extended = text[at] == ':';
            if (extended)
                at++;

            if (!TwoDigits(text, at, 59, out minute))
                return false;

            at += 2;
            if (at < text.Length)
            {
                if ((text[at] == ':') != extended)
                    return false;

                if (extended)
                    at++;

                if (!TwoDigits(text, at, 59, out _))
                    return false;

                at += 2;
                hasSeconds = true;
                if (at < text.Length)
                {
                    if (text[at] is not ('.' or ',') || at + 1 == text.Length || text.Length - at - 1 > 9)
                        return false;

                    for (var digit = at + 1; digit < text.Length; digit++)
                    {
                        if (!char.IsAsciiDigit(text[digit]))
                            return false;
                    }
                }
            }
        }

        minutes = ((hours * 60) + minute) * (text[0] == '-' ? -1 : 1);
        return true;
    }

    // Two ASCII digits at a position, read as a number no greater than a maximum.
    // Broiler-AI:           Origin=Ported; Spec=JSD-0076, ECMA-262 21.4.1.33 via Broiler.VM JsRealm.DateTimeFormat.ParseOffset; IP=Low; Security=Medium; Resources=1; Fingerprint=6CDA57
    // Broiler-Human:        PENDING
    private static bool TwoDigits(ReadOnlySpan<char> text, int at, int maximum, out int value)
    {
        value = 0;
        if (at + 2 > text.Length || !char.IsAsciiDigit(text[at]) || !char.IsAsciiDigit(text[at + 1]))
            return false;

        value = ((text[at] - '0') * 10) + (text[at + 1] - '0');
        return value <= maximum;
    }
}

/// <summary>
/// The rules of a zone at one fixed offset with no transition: an offset time zone (ECMA-262 21.4.1.33),
/// which <see cref="ZonedTime"/> reads as Temporal reads one.
/// </summary>
/// <remarks>
/// Over these rules, <see cref="ZonedTime.GetPossibleEpochNanoseconds"/> gives the one instant the
/// proposal's offset-zone branch gives, and refuses it as the branch does where it is outside the instant
/// range; no transition is ever found.
/// </remarks>
// Broiler-AI:           Origin=Ported; Spec=JSD-0076, Temporal GetPossibleEpochNanoseconds via Broiler.VM JsTemporal.PossibleEpochNs; IP=Low; Security=Low; Resources=1; Fingerprint=F493A4
// Broiler-Human:        PENDING
public sealed class FixedOffsetTimeZoneRules : ITimeZoneRules
{
    // Broiler-AI:           Origin=Ported; Spec=JSD-0076; IP=Low; Security=Low; Resources=1; Fingerprint=65DEBC
    // Broiler-Human:        PENDING
    private readonly int offset;

    /// <summary>Creates the rules of a fixed offset.</summary>
    /// <param name="offsetSeconds">The offset in seconds, positive east of UTC, of magnitude below a day.</param>
    /// <exception cref="ArgumentOutOfRangeException">The offset's magnitude is a day or more.</exception>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0076, ECMA-262 21.4.1.33.2; IP=Low; Security=Low; Resources=1; Fingerprint=B46D4F
    // Broiler-Human:        PENDING
    public FixedOffsetTimeZoneRules(int offsetSeconds)
    {
        if (offsetSeconds is <= -86400 or >= 86400)
            throw new ArgumentOutOfRangeException(nameof(offsetSeconds), offsetSeconds, "An offset is below a day.");

        offset = offsetSeconds;
    }

    /// <summary>The fixed offset, whatever the instant.</summary>
    /// <param name="epochSeconds">The instant.</param>
    /// <returns>The offset the rules were created with.</returns>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0076, ECMA-262 21.4.1.33.2; IP=Low; Security=Low; Resources=1; Fingerprint=377426
    // Broiler-Human:        PENDING
    public int OffsetSecondsAt(long epochSeconds) => offset;

    /// <summary>Never daylight time: an offset zone has no daylight saving time.</summary>
    /// <param name="epochSeconds">The instant.</param>
    /// <returns><see langword="false"/>.</returns>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0076; IP=Low; Security=Low; Resources=1; Fingerprint=4D4035
    // Broiler-Human:        PENDING
    public bool IsDaylightAt(long epochSeconds) => false;

    /// <summary>No transition: the Temporal proposal's <c>GetTimeZoneTransition</c> answers <see langword="null"/> for an offset zone.</summary>
    /// <param name="afterEpochSeconds">The instant to search after.</param>
    /// <param name="limitEpochSeconds">The last instant that may be answered.</param>
    /// <param name="daylightFlag">Whether a change of the daylight flag alone counts.</param>
    /// <returns><see langword="null"/>.</returns>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0076, Temporal GetNamedTimeZoneNextTransition; IP=Low; Security=Low; Resources=1; Fingerprint=287EB7
    // Broiler-Human:        PENDING
    public long? NextTransition(long afterEpochSeconds, long limitEpochSeconds, bool daylightFlag) => null;

    /// <summary>No transition: the Temporal proposal's <c>GetTimeZoneTransition</c> answers <see langword="null"/> for an offset zone.</summary>
    /// <param name="beforeEpochSeconds">The instant to search before.</param>
    /// <param name="daylightFlag">Whether a change of the daylight flag alone counts.</param>
    /// <returns><see langword="null"/>.</returns>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0076, Temporal GetNamedTimeZonePreviousTransition; IP=Low; Security=Low; Resources=1; Fingerprint=042C7A
    // Broiler-Human:        PENDING
    public long? PreviousTransition(long beforeEpochSeconds, bool daylightFlag) => null;
}
