// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0

using System;
using Broiler.ECMAScript.Time;

namespace Broiler.ECMAScript.Intl;

/// <summary>
/// What kind of zone a date-time format writes in (Broiler.VM's JSD-0053): UTC, the zero offset ICU names GMT, another fixed
/// offset, or an IANA zone whose offset changes. It is the primary identifier's kind, so it decides how the zone is named.
/// </summary>
public enum TimeZoneKind
{
    /// <summary>UTC, and every name whose primary identifier is <c>UTC</c>, named CLDR's <c>zone.utc</c> names.</summary>
    Utc = 0,

    /// <summary>The offset <c>+00:00</c>, which ICU names by the GMT metazone.</summary>
    Gmt = 1,

    /// <summary>Any other fixed offset: an offset string, or IANA's <c>Etc/GMT</c> zones with an hour.</summary>
    Offset = 2,

    /// <summary>An IANA zone whose offsets its rules give.</summary>
    Named = 3,
}

/// <summary>
/// A date-time format's resolved time zone (Broiler.VM's JSD-0082, decision D15): the identifier [[TimeZone]] reports, its
/// primary identifier, by which the zone is named, its kind, and the rules or the fixed offset it is written in.
/// </summary>
/// <remarks>
/// <b>Both names, by decision D15.</b> <c>tc39/proposal-temporal@e8cc03fc</c>'s amendment of CreateDateTimeFormat
/// (<c>intl.html</c>, <c>sec-createdatetimeformat</c>) sets [[TimeZone]] to GetAvailableNamedTimeZoneIdentifier's
/// [[Identifier]], the identifier as given in the database's case, where Broiler.VM's realm reported its primary identifier
/// (row Z8, a correction of Broiler.VM). The zone keeps its names: its metazone and its generic, specific and location names
/// are the primary identifier's, as Broiler.VM names them (row T26, implementation-defined).
/// </remarks>
public sealed class ResolvedTimeZone
{
    private ResolvedTimeZone(string identifier, string primaryIdentifier, int offsetMinutes, TimeZoneKind kind, ITimeZoneRules? rules)
    {
        Identifier = identifier;
        PrimaryIdentifier = primaryIdentifier;
        OffsetMinutes = offsetMinutes;
        Kind = kind;
        Rules = rules;
    }

    /// <summary>UTC: the zone a format writes in where neither the options nor an engine's host-zone provider name another.</summary>
    public static ResolvedTimeZone Utc { get; } = new("UTC", "UTC", 0, TimeZoneKind.Utc, null);

    /// <summary>
    /// [[TimeZone]]: the identifier as given, in the database's case (<c>Asia/Calcutta</c> for <c>asia/calcutta</c>), or an
    /// offset in its <c>+HH:MM</c> form.
    /// </summary>
    public string Identifier { get; }

    /// <summary>The primary identifier the zone is named by: <c>UTC</c> for every name resolving to it, an offset as <see cref="Identifier"/>.</summary>
    public string PrimaryIdentifier { get; }

    /// <summary>The fixed offset from UTC in minutes, positive east of UTC; zero for an IANA zone, whose offsets its rules give.</summary>
    public int OffsetMinutes { get; }

    /// <summary>The kind of zone, by its primary identifier.</summary>
    public TimeZoneKind Kind { get; }

    /// <summary>An IANA zone's rules, the shared database's; <see langword="null"/> for UTC and a fixed offset.</summary>
    public ITimeZoneRules? Rules { get; }

    /// <summary>
    /// CreateDateTimeFormat's resolution of a <c>timeZone</c> option already converted to a String
    /// (<c>sec-createdatetimeformat</c>, as <c>e8cc03fc</c> amends it): an offset string by IsTimeZoneOffsetString and its
    /// <c>+HH:MM</c> form, and otherwise an available named time zone identifier, matched without regard to ASCII case
    /// (GetAvailableNamedTimeZoneIdentifier, <c>sec-getavailablenamedtimezoneidentifier</c>).
    /// </summary>
    /// <param name="timeZone">The option's String.</param>
    /// <param name="timeZones">The IANA Time Zone Database.</param>
    /// <param name="resolved">The zone, or <see langword="null"/> when it is refused.</param>
    /// <param name="charge">The caller's meter: one unit per code unit of <paramref name="timeZone"/>, before it is read;
    /// Broiler.VM's realm charges nothing here, so its adapter passes a meter that charges nothing (decision D18).</param>
    /// <returns><see cref="IntlStatus.Completed"/>; <see cref="IntlStatus.TimeZoneOffsetHasSeconds"/> for an offset string with
    /// seconds or a fraction, which ECMA-402 refuses with a <c>RangeError</c>; or <see cref="IntlStatus.InvalidTimeZone"/>, the
    /// <c>RangeError</c> of a name the database lacks.</returns>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    public static IntlStatus Resolve(string timeZone, ITimeZoneDatabase timeZones, out ResolvedTimeZone? resolved, WorkCharge? charge = null)
    {
        ArgumentNullException.ThrowIfNull(timeZone);
        ArgumentNullException.ThrowIfNull(timeZones);
        charge?.Invoke((ulong)timeZone.Length);
        resolved = null;

        if (OffsetTimeZones.IsTimeZoneOffsetString(timeZone))
        {
            if (!OffsetTimeZones.TryParseOffsetIdentifier(timeZone, out var minutes))
            {
                return IntlStatus.TimeZoneOffsetHasSeconds;
            }

            var offset = OffsetTimeZones.FormatOffsetIdentifier(minutes);
            resolved = new ResolvedTimeZone(offset, offset, minutes, minutes == 0 ? TimeZoneKind.Gmt : TimeZoneKind.Offset, null);
            return IntlStatus.Completed;
        }

        if (!timeZones.TryFind(timeZone, out var id))
        {
            return IntlStatus.InvalidTimeZone;
        }

        var primary = timeZones.Primary(id);

        if (primary.Identifier == "UTC")
        {
            resolved = new ResolvedTimeZone(id.Identifier, "UTC", 0, TimeZoneKind.Utc, null);
            return IntlStatus.Completed;
        }

        // IANA'S ETC/GMT ZONES WITH AN HOUR ARE FIXED OFFSETS by definition, with POSIX's inverted sign.
        if (OffsetTimeZones.TryGetEtcGmtOffset(primary.Identifier, out var etcGmt))
        {
            resolved = new ResolvedTimeZone(id.Identifier, primary.Identifier, etcGmt, TimeZoneKind.Offset, null);
            return IntlStatus.Completed;
        }

        resolved = new ResolvedTimeZone(id.Identifier, primary.Identifier, 0, TimeZoneKind.Named, timeZones.Rules(primary));
        return IntlStatus.Completed;
    }
}
