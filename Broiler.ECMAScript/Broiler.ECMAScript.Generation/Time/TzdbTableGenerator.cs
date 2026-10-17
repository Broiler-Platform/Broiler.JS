// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   14
// Annotated:        14/14
// Exempt:           5
// Human-reviewed:   0/14
// IP risk:          Low
// Security risk:    High
// Criteria:         2/2
// Resource impact:  6/10 max
// Unverified:       14
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Broiler.ECMAScript.Generation.Time;

/// <summary>A compiled zone with its Zone name.</summary>
/// <param name="Name">The Zone's name in the tzdb source.</param>
/// <param name="Zone">The compiled zone.</param>
// Broiler-AI:           Origin=Ported; Spec=JSD-0053 tz tables via Broiler.VM TzdbTableGenerator; IP=Low; Security=Low; Resources=1; Fingerprint=FB5F32
// Broiler-Human:        PENDING
public sealed record TzdbCompiledZone(string Name, TzdbZone Zone);

/// <summary>
/// The time zone tables of one tzdb release, as <see cref="TzdbTableGenerator.Generate"/> writes them: the
/// bytes an engine embeds, whatever file it embeds them in.
/// </summary>
/// <remarks>
/// <b>The layout</b> is Broiler.VM's, which its <c>JsTzdbTables</c> holds at Broiler.VM <c>9508bd5</c>
/// byte for byte (Broiler.VM's decision JSD-0053, the design's compact format).
/// <list type="bullet">
/// <item><see cref="TimeZones"/> is binary: a count of zones, then each zone as a zig-zag first offset,
/// its palette of offsets, each twice the offset and one more where it is daylight time, its transitions
/// as a time delta and a palette index, and its recurring rules. A time delta is in minutes where it is a
/// whole number of them, its low bit clear, and in seconds otherwise, its low bit set; the first
/// transition's delta is from the epoch. Each recurring rule is its month, the kind of its ON field (0 a
/// day, 1 the last weekday, 2 a weekday on or after, 3 a weekday on or before), the weekday (Monday 0),
/// the day, its AT time and kind (0 wall, 1 standard, 2 universal) and its save. Unsigned numbers are
/// little-endian base-128 varints, and signed ones zig-zag encoded first.</item>
/// <item><see cref="TimeZoneIds"/> is ASCII text, one line per Zone and Link name in ordinal order,
/// lines separated by LF and the last unterminated: <c>identifier|zone index|primary</c>, the primary
/// empty where it is the identifier itself.</item>
/// <item><see cref="TimeZoneRegions"/> is ASCII text in the same shape: <c>region|zone|zone...</c>,
/// <c>zone.tab</c>'s zones of each country, both in ordinal order.</item>
/// </list>
/// </remarks>
// Broiler-AI:           Origin=Ported; Spec=JSD-0053 tz tables via Broiler.VM TzdbTableGenerator; IP=Low; Security=Low; Resources=1; Fingerprint=7E8319
// Broiler-Human:        PENDING
public sealed class TzdbTables
{
    /// <summary>Holds the tables the generator wrote.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0053 tz tables via Broiler.VM TzdbTableGenerator; IP=Low; Security=Low; Resources=1; Fingerprint=B372DA
    // Broiler-Human:        PENDING
    internal TzdbTables(string release, byte[] timeZones, byte[] timeZoneIds, byte[] timeZoneRegions)
    {
        Release = release;
        TimeZones = timeZones;
        TimeZoneIds = timeZoneIds;
        TimeZoneRegions = timeZoneRegions;
    }

    /// <summary>The tzdb release the tables were generated from, <see cref="TimeDataVersions.TzdbRelease"/>.</summary>
    public string Release { get; }

    /// <summary>Each Zone's UTC offsets, in Zone name order: its first offset, its transitions, and the rules that continue them.</summary>
    public ReadOnlyMemory<byte> TimeZones { get; }

    /// <summary>Every Zone and Link name, in ordinal order: identifier, the index of the Zone whose offsets it reads, and its primary identifier where that is another.</summary>
    public ReadOnlyMemory<byte> TimeZoneIds { get; }

    /// <summary><c>zone.tab</c>'s zones of each country, in ordinal order: region, zones.</summary>
    public ReadOnlyMemory<byte> TimeZoneRegions { get; }
}

/// <summary>
/// Writes the time zone tables from the tzdb members a <see cref="TzdbPin"/> names and CLDR's time zone
/// keys, which a <see cref="CldrPin"/> names: one function, pure and deterministic.
/// </summary>
/// <remarks>
/// <para>
/// <b>PORTED from Broiler.VM's <c>TzdbTableGenerator</c></b> (its rule N30; Broiler.VM's JSD-0076 moves it
/// to the data home), its data half with its behaviour unchanged: the tables are the bytes it writes into
/// <c>JsTzdbTables</c>. Its source half is <see cref="TzdbTableSource"/>, so that an engine can write the
/// tables into a C# file of its own namespace, or embed the bytes as they are.
/// </para>
/// <para>
/// <b>The primary identifiers are CLDR's</b> (decision SJL-D-20), as ECMA-402 6.5's note recommends: the
/// identifiers CLDR gives one time zone key are one zone, whose primary identifier is the key's
/// <c>_iana</c> name, or its first alias where it states none. An identifier CLDR does not name resolves
/// through tzdb's Links. <c>Etc/UTC</c>, <c>Etc/GMT</c> and <c>GMT</c>, and every identifier resolving to
/// them, resolve to <c>UTC</c>.
/// </para>
/// <para>
/// <b>Its inputs are the pins' files and nothing else</b>, verified first; its output depends on them
/// alone - every table in ordinal order, numbers through the invariant format, LF line endings, and
/// nothing recording a time, a machine or a path.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=Ported; Spec=JSD-0053 tz tables via Broiler.VM TzdbTableGenerator; IP=Low; Security=High; Resources=6; Fingerprint=A7C108
// Broiler-Falsified-If: a table is written from a tzdb member or a CLDR file that failed its pin's check, or two runs over the same inputs write different bytes
// Broiler-Human:        PENDING
public static class TzdbTableGenerator
{
    /// <summary>CLDR's time zone keys, by the path a CLDR pin writes them under.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0053 tz tables via Broiler.VM TzdbTableGenerator; IP=Low; Security=Low; Resources=0; Fingerprint=1BA802
    // Broiler-Human:        PENDING
    public const string TimeZoneKeys = "cldr-48.2.0/json/cldr-bcp47/bcp47/timezone.json";

    /// <summary>The compiled zones of the pinned release, by Zone name, in ordinal order.</summary>
    /// <param name="source">The parsed source files.</param>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0053 tz tables via Broiler.VM TzdbTableGenerator; IP=Low; Security=Medium; Resources=5; Fingerprint=5424B0
    // Broiler-Human:        PENDING
    public static IReadOnlyList<TzdbCompiledZone> Zones(TzdbSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.Zones.Keys.Order(StringComparer.Ordinal)
            .Select(name => new TzdbCompiledZone(name, TzdbCompiler.Compile(source.Zones[name], source.Rules)))
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// The three tables, from the two verified archives. Throws when either is not the pinned one; nothing
    /// is generated from input that failed a check.
    /// </summary>
    /// <param name="pin">The tzdb pin.</param>
    /// <param name="tzdb">The tzdb members, as <see cref="TzdbPin.Extract"/> answers them.</param>
    /// <param name="cldrPin">The CLDR pin, which must name <see cref="TimeZoneKeys"/>.</param>
    /// <param name="cldr">Each file the CLDR pin names, by the path it writes it under.</param>
    /// <exception cref="InvalidDataException">An archive is not the pinned one, the CLDR pin does not name the time zone keys, or the data disagrees with itself.</exception>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0053 tz tables via Broiler.VM TzdbTableGenerator; IP=Low; Security=High; Resources=6; Fingerprint=CA2F6C
    // Broiler-Falsified-If: tables are answered for a tzdb member or a CLDR file whose length or hash differs from its pin
    // Broiler-Human:        PENDING
    public static TzdbTables Generate(
        TzdbPin pin,
        IReadOnlyDictionary<string, byte[]> tzdb,
        CldrPin cldrPin,
        IReadOnlyDictionary<string, byte[]> cldr)
    {
        ArgumentNullException.ThrowIfNull(pin);
        ArgumentNullException.ThrowIfNull(cldrPin);

        pin.Verified(tzdb);
        cldrPin.Verified(cldr);
        _ = cldrPin.Entry(TimeZoneKeys);

        var source = TzdbCompiler.Parse(tzdb);
        var zones = Zones(source);

        return new TzdbTables(
            TzdbPin.PinnedVersion,
            Encode(zones),
            Text("TimeZoneIds", Identifiers(source, zones, cldr[TimeZoneKeys])),
            Text("TimeZoneRegions", Regions(tzdb)));
    }

    /// <summary>The binary zone table, in the layout <see cref="TzdbTables.TimeZones"/> states.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0053 tz tables via Broiler.VM TzdbTableGenerator; IP=Low; Security=Medium; Resources=4; Fingerprint=FD865F
    // Broiler-Human:        PENDING
    internal static byte[] Encode(IReadOnlyList<TzdbCompiledZone> zones)
    {
        var bytes = new List<byte>();
        Unsigned(bytes, (ulong)zones.Count);

        foreach (var (_, zone) in zones)
        {
            Signed(bytes, zone.InitialOffset);

            var palette = zone.Transitions.Select(static transition => (transition.Offset, transition.Daylight)).Distinct().ToList();
            Unsigned(bytes, (ulong)palette.Count);

            foreach (var (offset, daylight) in palette)
            {
                Signed(bytes, (offset * 2L) + (daylight ? 1 : 0));
            }

            Unsigned(bytes, (ulong)zone.Transitions.Count);
            var previous = 0L;

            foreach (var (at, offset, daylight) in zone.Transitions)
            {
                var delta = at - previous;
                previous = at;
                Unsigned(bytes, delta % 60 == 0 ? ZigZag(delta / 60) << 1 : (ZigZag(delta) << 1) | 1);
                bytes.Add(checked((byte)palette.IndexOf((offset, daylight))));
            }

            if (zone.Final is not { } final)
            {
                bytes.Add(0);
                continue;
            }

            bytes.Add(checked((byte)final.Rules.Count));
            Signed(bytes, final.StandardOffset);
            Signed(bytes, final.LastListedYear);

            foreach (var rule in final.Rules)
            {
                var (kind, weekday, day) = OnField(rule.On);
                bytes.Add((byte)rule.Month);
                bytes.Add((byte)kind);
                bytes.Add((byte)weekday);
                bytes.Add((byte)day);
                Signed(bytes, rule.At);
                bytes.Add(rule.AtKind switch { 'w' => 0, 's' => 1, _ => 2 });
                Signed(bytes, rule.Save);
            }
        }

        return [.. bytes];
    }

    /// <summary>An ON field as a kind (0 a day, 1 the last weekday, 2 on or after, 3 on or before), a weekday and a day.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0053 tz tables via Broiler.VM TzdbTableGenerator; IP=Low; Security=Medium; Resources=1; Fingerprint=CEF3AE
    // Broiler-Human:        PENDING
    internal static (int Kind, int Weekday, int Day) OnField(string on)
    {
        if (on.StartsWith("last", StringComparison.Ordinal))
        {
            return (1, TzdbCompiler.Weekday(on[4..]), 0);
        }

        foreach (var (token, kind) in new[] { (">=", 2), ("<=", 3) })
        {
            var at = on.IndexOf(token, StringComparison.Ordinal);

            if (at > 0)
            {
                return (kind, TzdbCompiler.Weekday(on[..at]), TzdbCompiler.Integer(on[(at + 2)..]));
            }
        }

        return (0, 0, TzdbCompiler.Integer(on));
    }

    /// <summary>A signed value, zig-zag encoded so that a small magnitude is a small number.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0053 tz tables via Broiler.VM TzdbTableGenerator; IP=Low; Security=Low; Resources=0; Fingerprint=413BE5
    // Broiler-Human:        PENDING
    private static ulong ZigZag(long value) => (ulong)((value << 1) ^ (value >> 63));

    /// <summary>Appends a signed value as a zig-zag varint.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0053 tz tables via Broiler.VM TzdbTableGenerator; IP=Low; Security=Low; Resources=1; Fingerprint=CEE1C1
    // Broiler-Human:        PENDING
    private static void Signed(List<byte> bytes, long value) => Unsigned(bytes, ZigZag(value));

    /// <summary>Appends an unsigned value as a little-endian base-128 varint.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0053 tz tables via Broiler.VM TzdbTableGenerator; IP=Low; Security=Low; Resources=1; Fingerprint=69B5A7
    // Broiler-Human:        PENDING
    private static void Unsigned(List<byte> bytes, ulong value)
    {
        while (value >= 0x80)
        {
            bytes.Add((byte)(value | 0x80));
            value >>= 7;
        }

        bytes.Add((byte)value);
    }

    /// <summary>Every identifier, with the Zone it reads and its primary identifier where that is another.</summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 6.5.1 via Broiler.VM TzdbTableGenerator; IP=Low; Security=Medium; Resources=4; Fingerprint=A34AEF
    // Broiler-Human:        PENDING
    internal static IEnumerable<string> Identifiers(TzdbSource source, IReadOnlyList<TzdbCompiledZone> zones, byte[] timeZoneKeys)
    {
        var index = zones.Select(static (zone, i) => (zone.Name, i)).ToDictionary(static p => p.Name, static p => p.i, StringComparer.Ordinal);
        var identifiers = source.Zones.Keys.Concat(source.Links.Keys).Order(StringComparer.Ordinal).ToArray();
        var known = identifiers.ToHashSet(StringComparer.Ordinal);

        // ECMA-402 6.5.1 asserts that no two identifiers differ only in ASCII case; the tzdb names are
        // ASCII, so the check folds ASCII letters and reads no platform casing.
        if (identifiers.GroupBy(ArchiveReading.AsciiLower, StringComparer.Ordinal).Any(static group => group.Count() > 1))
        {
            throw new InvalidDataException("two tzdb identifiers differ only in case, which ECMA-402 6.5.1 asserts never happens");
        }

        var primaries = new Dictionary<string, string>(StringComparer.Ordinal);

        using (var document = JsonDocument.Parse(timeZoneKeys))
        {
            foreach (var key in document.RootElement.GetProperty("keyword").GetProperty("u").GetProperty("tz").EnumerateObject())
            {
                if (key.Value.ValueKind != JsonValueKind.Object || !key.Value.TryGetProperty("_alias", out var alias))
                {
                    continue;
                }

                var names = alias.GetString()!.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var primary = key.Value.TryGetProperty("_iana", out var iana) ? iana.GetString()! : names[0];

                foreach (var name in names)
                {
                    primaries[name] = primary;
                }
            }
        }

        string Resolve(string name)
        {
            while (source.Links.TryGetValue(name, out var target))
            {
                name = target;
            }

            return name;
        }

        foreach (var identifier in identifiers)
        {
            var primary = primaries.TryGetValue(identifier, out var named) && known.Contains(named) ? named : Resolve(identifier);

            if (primary is "Etc/UTC" or "Etc/GMT" or "GMT")
            {
                primary = "UTC";
            }

            var zone = index[Resolve(identifier)];
            yield return identifier + "|" + zone.ToString(NumberFormatInfo.InvariantInfo) + (primary == identifier ? "|" : "|" + primary);
        }
    }

    /// <summary><c>zone.tab</c>'s zones of each country code.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0053 tz tables via Broiler.VM TzdbTableGenerator; IP=Low; Security=Medium; Resources=4; Fingerprint=2C8BD1
    // Broiler-Human:        PENDING
    internal static IEnumerable<string> Regions(IReadOnlyDictionary<string, byte[]> tzdb) =>
        TzdbPin.Decode(tzdb["zone.tab"]).Split('\n')
            .Where(static line => line.Length > 0 && line[0] != '#')
            .Select(static line => line.Split('\t'))
            .GroupBy(static fields => fields[0], StringComparer.Ordinal)
            .OrderBy(static group => group.Key, StringComparer.Ordinal)
            .Select(static group => group.Key + "|" + string.Join('|', group.Select(static fields => fields[2]).Order(StringComparer.Ordinal)));

    /// <summary>
    /// A text table's bytes: its lines in ASCII, separated by LF, the last unterminated, as a C# raw string
    /// literal of them holds them. A line that is not plain printable ASCII, or holds a quotation mark, is
    /// refused, since no source writer could state it.
    /// </summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0053 tz tables via Broiler.VM TzdbTableGenerator; IP=Low; Security=Medium; Resources=3; Fingerprint=FCDD8A
    // Broiler-Human:        PENDING
    internal static byte[] Text(string name, IEnumerable<string> lines)
    {
        var text = new StringBuilder();
        var separator = string.Empty;

        foreach (var line in lines)
        {
            if (line.Contains('"', StringComparison.Ordinal) || line.Any(static c => c > 0x7E || c < 0x20))
            {
                throw new InvalidDataException("a " + name + " line is not plain ASCII: `" + line + "`");
            }

            text.Append(separator).Append(line);
            separator = "\n";
        }

        return Encoding.ASCII.GetBytes(text.ToString());
    }
}
