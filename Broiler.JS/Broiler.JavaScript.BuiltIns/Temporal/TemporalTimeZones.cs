using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Broiler.ECMAScript.Temporal;
using Broiler.ECMAScript.Time;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Temporal;

/// <summary>
/// This engine's adapter of the shared time-zone algorithms (milestone SJL-8.6; the adapter guide
/// <c>adapter-guide-time-zones.md</c>): it reads identifiers in the order the specification gives, hands
/// them to <see cref="TimeZoneDatabase"/>, <see cref="OffsetTimeZones"/> and <see cref="ZonedTime"/> over the
/// tables <see cref="TemporalTimeData"/> embeds, and turns each <see cref="TimeStatus"/> into this engine's
/// <c>RangeError</c>. It holds no algorithm.
/// </summary>
/// <remarks>
/// <para>
/// <b>A time zone here is a canonical identifier</b>: an offset identifier, <c>±HH:MM</c>, or an available
/// named identifier in the database's case. Every entry point validates a name before it reads one, so an
/// unknown name never reaches a zone's rules; if one did, it is a <c>RangeError</c>, never an offset of zero.
/// </para>
/// <para>
/// <b>The host's zone</b> (decision SJL-D-19) is the host's IANA name, resolved through the same database to
/// its primary identifier; a host zone the database does not hold reads as <c>UTC</c>. <c>Date</c>'s local
/// time, <c>Temporal.Now</c> and the default <c>Intl.DateTimeFormat</c> zone all read it, so one zone has one
/// dataset in this engine.
/// </para>
/// </remarks>
internal static class TemporalTimeZones
{
    // The rules of each canonical identifier read so far: at most every name and every offset of a day.
    private static readonly ConcurrentDictionary<string, ITimeZoneRules> RulesById = new(StringComparer.Ordinal);

    // The shared Temporal core's provider of each canonical identifier read so far, one per zone.
    private static readonly ConcurrentDictionary<string, TemporalTimeZone> ArithmeticById = new(StringComparer.Ordinal);

    private static SystemZone? system;

    /// <summary>The database over the embedded tables.</summary>
    internal static ITimeZoneDatabase Database => TemporalTimeData.TimeZones;

    /// <summary>
    /// <c>AvailablePrimaryTimeZoneIdentifiers</c> (ECMA-402 6.5.3), CLDR's primaries in ordinal order: what
    /// <c>Intl.supportedValuesOf("timeZone")</c> lists.
    /// </summary>
    internal static IReadOnlyList<string> PrimaryIdentifiers => Database.PrimaryIdentifiers;

    /// <summary>
    /// Reads a bare time zone identifier (the Temporal proposal's <c>ParseTimeZoneIdentifier</c>, then
    /// <c>GetAvailableNamedTimeZoneIdentifier</c>): an offset identifier with no seconds, normalised to
    /// <c>±HH:MM</c>, or an available name by its ASCII case, in the database's case. A link keeps its own name.
    /// </summary>
    internal static bool TryCanonicalize(string identifier, [NotNullWhen(true)] out string? canonical)
    {
        if (OffsetTimeZones.TryParseOffsetIdentifier(identifier, out var minutes))
        {
            canonical = OffsetTimeZones.FormatOffsetIdentifier(minutes);
            return true;
        }

        if (Database.TryFind(identifier, out var id))
        {
            canonical = id.Identifier;
            return true;
        }

        canonical = null;
        return false;
    }

    /// <summary>
    /// The time zone of <c>Intl.DateTimeFormat</c>'s <c>timeZone</c> option (ECMA-402 <c>CreateDateTimeFormat</c>):
    /// an offset string without seconds, normalised to <c>±HH:MM</c>, or an available name; anything else,
    /// an ISO date-time string among them, is a <c>RangeError</c>. The name is kept, not its primary: which
    /// one the formatter reports stays this engine's until the edition both engines pin settles it (row Z8).
    /// </summary>
    internal static string ResolveFormatterTimeZone(string timeZone)
    {
        if (OffsetTimeZones.IsTimeZoneOffsetString(timeZone))
        {
            if (OffsetTimeZones.TryParseOffsetIdentifier(timeZone, out var minutes))
                return OffsetTimeZones.FormatOffsetIdentifier(minutes);

            throw JSEngine.NewRangeError($"Invalid timeZone option: {timeZone}");
        }

        if (Database.TryFind(timeZone, out var id))
            return id.Identifier;

        throw JSEngine.NewRangeError($"Invalid timeZone option: {timeZone}");
    }

    /// <summary>The rules of a canonical identifier: a fixed offset, or the named zone's, which is its primary's.</summary>
    internal static ITimeZoneRules Rules(string timeZone) =>
        RulesById.TryGetValue(timeZone, out var rules) ? rules : RulesById.GetOrAdd(timeZone, Resolve(timeZone));

    /// <summary>
    /// A canonical identifier as the shared Temporal core's zone provider (<see cref="ITimeZoneArithmetic"/>, the adapter
    /// guide's part C): the library's <see cref="TemporalTimeZone"/> over the zone's <see cref="Rules"/>, one per zone. The
    /// zoned additions, the relative rounding, the zoned differences and the day lengths compute through it.
    /// </summary>
    internal static ITimeZoneArithmetic Arithmetic(string timeZone) =>
        ArithmeticById.TryGetValue(timeZone, out var zone) ? zone : ArithmeticById.GetOrAdd(timeZone, new TemporalTimeZone(Rules(timeZone)));

    /// <summary>The offset in force at an instant: <c>GetOffsetNanosecondsFor</c>.</summary>
    internal static long OffsetNanoseconds(string timeZone, BigInteger epochNanoseconds) =>
        ZonedTime.GetOffsetNanosecondsFor(Rules(timeZone), epochNanoseconds);

    /// <summary>
    /// The instants a wall-clock time names, ascending (<c>GetPossibleEpochNanoseconds</c>): one, two in an
    /// overlap, none in a gap. An instant outside the range is a <c>RangeError</c>.
    /// </summary>
    internal static BigInteger[] PossibleEpochNanoseconds(string timeZone, IsoDateTime local)
    {
        Span<BigInteger> possible = new BigInteger[ZonedTime.MaxPossibleEpochNanoseconds];
        Check(ZonedTime.GetPossibleEpochNanoseconds(Rules(timeZone), local, possible, out var count));
        return possible[..count].ToArray();
    }

    /// <summary>
    /// The instant a wall-clock time is read at (<c>GetEpochNanosecondsFor</c>): a repeated time's earlier or
    /// later instant, a skipped one moved across the gap; <c>reject</c> refuses both with a <c>RangeError</c>.
    /// </summary>
    internal static BigInteger EpochNanosecondsFor(string timeZone, IsoDateTime local, string disambiguation)
    {
        Check(ZonedTime.GetEpochNanosecondsFor(Rules(timeZone), local, ToDisambiguation(disambiguation), out var epochNanoseconds));
        return epochNanoseconds;
    }

    /// <summary>
    /// <c>DisambiguatePossibleEpochNanoseconds</c> over instants <see cref="PossibleEpochNanoseconds"/> gave.
    /// </summary>
    internal static BigInteger Disambiguate(string timeZone, BigInteger[] possible, IsoDateTime local, string disambiguation)
    {
        Check(ZonedTime.DisambiguatePossibleEpochNanoseconds(Rules(timeZone), possible, local, ToDisambiguation(disambiguation), out var epochNanoseconds));
        return epochNanoseconds;
    }

    /// <summary>
    /// The first instant of a day (<c>GetStartOfDay</c>): midnight's first instant, or the transition that skips
    /// it. A day with no instant, or one outside the range, is a <c>RangeError</c>; it never falls back to midnight.
    /// </summary>
    internal static BigInteger StartOfDay(string timeZone, IsoDate date)
    {
        Check(ZonedTime.GetStartOfDay(Rules(timeZone), date, out var epochNanoseconds));
        return epochNanoseconds;
    }

    /// <summary>The next transition after an instant, or <see langword="null"/>, as for every offset zone.</summary>
    internal static BigInteger? NextTransition(string timeZone, BigInteger epochNanoseconds) =>
        ZonedTime.GetNextTransition(Rules(timeZone), epochNanoseconds);

    /// <summary>The last transition before an instant, or <see langword="null"/>, as for every offset zone.</summary>
    internal static BigInteger? PreviousTransition(string timeZone, BigInteger epochNanoseconds) =>
        ZonedTime.GetPreviousTransition(Rules(timeZone), epochNanoseconds);

    /// <summary><c>TimeZoneEquals</c>: one string, or two names with one primary identifier.</summary>
    internal static bool Equal(string one, string two) => ZonedTime.TimeZoneEquals(Database, one, two);

    /// <summary>
    /// <c>SystemTimeZoneIdentifier</c> (ECMA-262 21.4.1.24): the host's zone as a primary identifier, or
    /// <c>UTC</c> where the database does not hold the host's zone (row A16).
    /// </summary>
    internal static string SystemTimeZoneIdentifier() => Host().Identifier;

    /// <summary>
    /// The host's zone (decision SJL-D-19): its primary identifier and rules from the shared database, and the
    /// .NET zone whose display names <c>Date</c>'s <c>toString</c> writes. It is read again whenever the host's
    /// local zone changes.
    /// </summary>
    internal static SystemZone Host()
    {
        TimeZoneInfo local;
        try
        {
            local = TimeZoneInfo.Local;
        }
        catch (Exception exception) when (exception is TimeZoneNotFoundException or InvalidTimeZoneException or System.Security.SecurityException)
        {
            local = TimeZoneInfo.Utc;
        }

        var current = system;
        if (current is not null && ReferenceEquals(current.Host, local))
            return current;

        var resolved = ResolveHost(local);
        system = resolved;
        return resolved;
    }

    // The host's IANA name, if it has one, through the database to its primary; UTC otherwise.
    private static SystemZone ResolveHost(TimeZoneInfo local)
    {
        string? name = null;
        if (local.HasIanaId)
            name = local.Id;
        else if (TimeZoneInfo.TryConvertWindowsIdToIanaId(local.Id, out var iana))
            name = iana;

        if (name is not null && Database.TryFind(name, out var id))
        {
            var primary = Database.Primary(id);
            return new SystemZone(local, primary.Identifier, Database.Rules(primary), local);
        }

        return new SystemZone(local, "UTC", Rules("UTC"), TimeZoneInfo.Utc);
    }

    private static ITimeZoneRules Resolve(string timeZone)
    {
        if (OffsetTimeZones.TryParseOffsetIdentifier(timeZone, out var minutes))
            return new FixedOffsetTimeZoneRules(minutes * 60);

        if (Database.TryFind(timeZone, out var id))
            return Database.Rules(id);

        throw JSEngine.NewRangeError($"Temporal: unknown time zone \"{timeZone}\"");
    }

    private static Disambiguation ToDisambiguation(string disambiguation) => disambiguation switch
    {
        "earlier" => Disambiguation.Earlier,
        "later" => Disambiguation.Later,
        "reject" => Disambiguation.Reject,
        _ => Disambiguation.Compatible,
    };

    // Each status but Completed is this engine's RangeError.
    private static void Check(TimeStatus status)
    {
        switch (status)
        {
            case TimeStatus.Completed:
                return;
            case TimeStatus.Ambiguous:
                throw JSEngine.NewRangeError("Temporal: wall-clock time is ambiguous in this time zone");
            case TimeStatus.Skipped:
                throw JSEngine.NewRangeError("Temporal: wall-clock time does not exist in this time zone");
            case TimeStatus.NoStartOfDay:
                throw JSEngine.NewRangeError("Temporal: the start of the day cannot be found in this time zone");
            default:
                throw JSEngine.NewRangeError("Temporal: the date-time is outside the representable range");
        }
    }

    /// <summary>The host's zone as this engine reads it.</summary>
    /// <param name="Host">The host's .NET zone it was read from, which identifies it until the host's zone changes.</param>
    /// <param name="Identifier">Its primary identifier, or <c>UTC</c>.</param>
    /// <param name="Rules">The shared database's rules of that identifier.</param>
    /// <param name="Names">The .NET zone whose standard and daylight display names <c>Date</c> writes.</param>
    internal sealed record SystemZone(TimeZoneInfo Host, string Identifier, ITimeZoneRules Rules, TimeZoneInfo Names);
}
