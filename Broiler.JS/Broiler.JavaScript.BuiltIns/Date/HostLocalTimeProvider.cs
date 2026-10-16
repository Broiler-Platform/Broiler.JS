using System;
using Broiler.ECMAScript.Time;

namespace Broiler.JavaScript.BuiltIns.Date;

/// <summary>
/// This engine's local time zone (decision SJL-D-19): the host's <see cref="TimeZoneInfo.Local"/>, read at
/// each instant with its daylight-saving time, over which the shared library computes ECMA-262's
/// <c>LocalTime</c>, <c>UTC</c> and <c>TimeZoneString</c>.
/// </summary>
/// <remarks>
/// <see cref="TimeZoneInfo"/> reads instants from year 1 to year 9999 only. An instant past the last of them
/// is read 400 years earlier, as often as needed, where the zone's rules repeat with the Gregorian calendar;
/// an instant before year 1's third day is read at that day's first instant, the zone's earliest offset. The
/// zone is read again at each call, so a host that changes it, or clears <see cref="TimeZoneInfo"/>'s cache,
/// is seen at the next one.
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.1.21; IP=Low; Security=Medium; Resources=1; Fingerprint=TBF
// Broiler-Falsified-If: an instant inside daylight-saving time is answered with the zone's standard offset, or an instant outside years 1 to 9999 with the offset in force at the moment of the call
// Broiler-Human:        PENDING
internal sealed class HostLocalTimeProvider : ILocalTimeProvider
{
    // The first instant read as itself: year 1's third day, so that no offset takes a reading below year 1.
    private const double First = -62135596800000d + (2 * 86400000d);

    // The last instant .NET reads: 9999-12-31T23:59:59.999Z.
    private const double Last = 253402300799999d;

    // The Gregorian calendar's cycle: 400 years of 146,097 days.
    private const double FourCenturies = 146097d * 86400000d;

    public double GetOffsetMilliseconds(double epochMilliseconds) =>
        Math.Truncate(TimeZoneInfo.Local.GetUtcOffset(Instant(epochMilliseconds)).TotalMilliseconds);

    public string? GetName(double epochMilliseconds)
    {
        var zone = TimeZoneInfo.Local;
        return zone.IsDaylightSavingTime(Instant(epochMilliseconds)) ? zone.DaylightName : zone.StandardName;
    }

    // The instant .NET reads for a time: itself within .NET's range, the same instant of a 400-year cycle
    // within it past the range's end, and year 1's third day before it.
    private static DateTimeOffset Instant(double epochMilliseconds)
    {
        var at = Math.Floor(epochMilliseconds);
        if (at > Last)
        {
            var cycles = (at - Last) % FourCenturies;
            at = cycles == 0 ? Last : Last + cycles - FourCenturies;
        }

        if (at < First)
            at = First;

        return DateTimeOffset.FromUnixTimeMilliseconds((long)at);
    }
}
