using System;
using System.Collections.Generic;
using System.Numerics;

namespace Broiler.JavaScript.BuiltIns.Temporal.Tz;

// A single IANA time zone, backed by the transition table parsed from the bundled TZif data.
// Offsets and transition instants are kept at the full second precision the IANA database records
// (so historical sub-minute offsets such as Africa/Monrovia's -00:44:30 are exact, unlike .NET's
// whole-minute TimeZoneInfo). All instants are UTC seconds since the Unix epoch.
internal sealed class IanaTimeZone
{
    // A local time type: the UTC offset that applies during an interval, whether it is daylight
    // saving, and the IANA abbreviation (e.g. "EST", "GMT", "-04").
    internal readonly struct LocalTimeType(int offsetSeconds, bool isDst, string abbreviation)
    {
        public readonly int OffsetSeconds = offsetSeconds;
        public readonly bool IsDst = isDst;
        public readonly string Abbreviation = abbreviation;
    }

    public string Id { get; }

    // Ascending UTC-second instants at which the offset changes; the type in effect from
    // TransitionTimes[i] (inclusive) until TransitionTimes[i+1] is Types[TransitionTypeIndices[i]].
    private readonly long[] _transitionTimes;
    private readonly byte[] _transitionTypeIndices;
    private readonly LocalTimeType[] _types;
    private readonly int _initialTypeIndex; // the type in effect before the first transition

    public IanaTimeZone(string id, long[] transitionTimes, byte[] transitionTypeIndices,
        LocalTimeType[] types, int initialTypeIndex)
    {
        Id = id;
        _transitionTimes = transitionTimes;
        _transitionTypeIndices = transitionTypeIndices;
        _types = types;
        _initialTypeIndex = initialTypeIndex;
    }

    public IReadOnlyList<long> TransitionTimesUtcSeconds => _transitionTimes;

    private int TypeIndexAt(long utcSeconds)
    {
        var times = _transitionTimes;
        if (times.Length == 0 || utcSeconds < times[0])
            return _initialTypeIndex;

        // Largest i with times[i] <= utcSeconds.
        int lo = 0, hi = times.Length - 1;
        while (lo < hi)
        {
            var mid = (lo + hi + 1) >> 1;
            if (times[mid] <= utcSeconds) lo = mid; else hi = mid - 1;
        }
        return _transitionTypeIndices[lo];
    }

    private LocalTimeType TypeAt(long utcSeconds) => _types[TypeIndexAt(utcSeconds)];

    public int GetOffsetSeconds(long utcSeconds) => TypeAt(utcSeconds).OffsetSeconds;
    public bool IsDaylightSavingTime(long utcSeconds) => TypeAt(utcSeconds).IsDst;

    private const long NsPerSecond = 1_000_000_000L;

    private static long FloorDivToSeconds(BigInteger epochNs)
    {
        var sec = BigInteger.DivRem(epochNs, NsPerSecond, out var rem);
        if (rem < 0) sec -= 1;
        return (long)sec;
    }

    // The UTC offset, in nanoseconds, in effect at the given instant.
    public long GetOffsetNanoseconds(BigInteger epochNs)
        => GetOffsetSeconds(FloorDivToSeconds(epochNs)) * NsPerSecond;

    public bool IsDaylightSavingTimeAt(BigInteger epochNs)
        => IsDaylightSavingTime(FloorDivToSeconds(epochNs));

    // The first transition instant strictly after (forward) / before (!forward) startNs, in
    // nanoseconds, or null when the zone has no such transition. Transition instants are whole
    // seconds in the IANA database, so the nanosecond value is exact.
    public BigInteger? FindTransition(BigInteger startNs, bool forward)
    {
        var times = _transitionTimes;
        if (times.Length == 0)
            return null;

        if (forward)
        {
            // Smallest i with times[i] * 1e9 > startNs.
            for (var lo = LowerBoundByNs(startNs); lo < times.Length; lo++)
            {
                var ns = (BigInteger)times[lo] * NsPerSecond;
                if (ns > startNs) return ns;
            }
            return null;
        }
        else
        {
            // Largest i with times[i] * 1e9 < startNs.
            for (var hi = LowerBoundByNs(startNs); hi >= 0; hi--)
            {
                if (hi >= times.Length) continue;
                var ns = (BigInteger)times[hi] * NsPerSecond;
                if (ns < startNs) return ns;
            }
            return null;
        }
    }

    // Index of the first transition whose instant is >= startNs (a starting point for the small
    // linear probe in FindTransition, which then steps to the strict next/previous neighbour).
    private int LowerBoundByNs(BigInteger startNs)
    {
        var times = _transitionTimes;
        int lo = 0, hi = times.Length;
        while (lo < hi)
        {
            var mid = (lo + hi) >> 1;
            if ((BigInteger)times[mid] * NsPerSecond < startNs) lo = mid + 1; else hi = mid;
        }
        return lo;
    }
}
