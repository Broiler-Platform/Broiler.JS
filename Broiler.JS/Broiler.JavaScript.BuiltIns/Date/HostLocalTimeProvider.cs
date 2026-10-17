using System;
using Broiler.ECMAScript.Time;
using Broiler.JavaScript.BuiltIns.Temporal;

namespace Broiler.JavaScript.BuiltIns.Date;

/// <summary>
/// This engine's local time zone (decision SJL-D-19): the host's zone, read at each instant from the shared
/// time zone database this engine embeds (milestone SJL-8.6), over which the shared library computes
/// ECMA-262's <c>LocalTime</c>, <c>UTC</c> and <c>TimeZoneString</c>.
/// </summary>
/// <remarks>
/// The host's zone is <see cref="TimeZoneInfo.Local"/>'s IANA name, resolved through the database to its
/// primary identifier, the zone <c>Temporal.Now</c> and the default <c>Intl.DateTimeFormat</c> zone read too;
/// a host zone the database does not hold reads as <c>UTC</c>. Its offsets are the database's at every
/// instant, continued by the zone's recurring rules after its last listed transition, at full second
/// precision. Its names, which <c>TimeZoneString</c> writes, stay the host's display names: the standard or the
/// daylight one, as the database's daylight flag reads. The zone is read again at each call, so a host that
/// changes it, or clears <see cref="TimeZoneInfo"/>'s cache, is seen at the next one.
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.1.21; IP=Low; Security=Medium; Resources=1; Fingerprint=TBF
// Broiler-Falsified-If: an instant inside daylight-saving time is answered with the zone's standard offset, or an offset that is not the shared database's for the host's zone at the instant
// Broiler-Human:        PENDING
internal sealed class HostLocalTimeProvider : ILocalTimeProvider
{
    public double GetOffsetMilliseconds(double epochMilliseconds) =>
        TemporalTimeZones.Host().Rules.OffsetSecondsAt(Seconds(epochMilliseconds)) * 1000d;

    public string? GetName(double epochMilliseconds)
    {
        var zone = TemporalTimeZones.Host();
        return zone.Rules.IsDaylightAt(Seconds(epochMilliseconds)) ? zone.Names.DaylightName : zone.Names.StandardName;
    }

    // The instant's floor second, within the database's bound, which every time value and every day either
    // side of one is far inside.
    private static long Seconds(double epochMilliseconds)
    {
        var seconds = Math.Floor(epochMilliseconds / 1000d);
        return (long)Math.Clamp(seconds, -TimeZoneDatabase.MaxEpochSeconds, TimeZoneDatabase.MaxEpochSeconds);
    }
}
