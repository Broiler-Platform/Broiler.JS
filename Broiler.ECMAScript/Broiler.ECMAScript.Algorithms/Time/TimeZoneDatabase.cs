// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;

namespace Broiler.ECMAScript.Time;

/// <summary>
/// An available named time zone identifier, as one <see cref="ITimeZoneDatabase"/> answers it: its
/// place in that database's identifier table and the identifier in the IANA Time Zone Database's case
/// (ECMA-262 21.4.1.22's Time Zone Identifier Record, <c>[[Identifier]]</c>).
/// </summary>
/// <remarks>
/// A handle is meaningful only to the database that gave it; another database refuses it as a broken
/// precondition. Its primary identifier is <see cref="ITimeZoneDatabase.Primary"/>'s answer.
/// </remarks>
/// <param name="Index">The identifier's place in the database's identifier table, from zero.</param>
/// <param name="Identifier">The identifier in the database's case, for example <c>America/New_York</c>.</param>
// Broiler-AI:           Origin=Derived; Spec=JSD-0076, ECMA-262 21.4.1.22 via Broiler.VM JsTimeZoneId; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
// Broiler-Human:        PENDING
public readonly record struct TimeZoneId(int Index, string Identifier);

/// <summary>
/// The IANA Time Zone Database as the shared zone algorithms read it (JSD-0076, the area design's
/// section 3.2): the release, every Zone and Link name with its primary identifier, the zones in use
/// in each region, and each zone's rules.
/// </summary>
/// <remarks>
/// <para>
/// <b>Lookup is the policy's</b> (decision SJL-D-20): an identifier matches only where it is an
/// ASCII-case-insensitive match for a name, as ECMA-402's <c>GetAvailableNamedTimeZoneIdentifier</c>
/// asks, and a string holding a character above U+007F matches nothing. An unknown name is a
/// <see langword="false"/> from <see cref="TryFind"/>, never an offset of zero.
/// </para>
/// <para>
/// <b>An identifier reads its primary identifier's zone</b>, so two identifiers that resolve to one
/// primary never answer different offsets (ECMA-402 6.5.1).
/// </para>
/// </remarks>
// Broiler-AI:           Origin=Derived; Spec=JSD-0076, ECMA-402 6.5 via Broiler.VM JsTimeZones; IP=Low; Security=Medium; Resources=1; Fingerprint=TBF
// Broiler-Human:        PENDING
public interface ITimeZoneDatabase
{
    /// <summary>The release of the IANA Time Zone Database the tables were compiled from, for example <c>2026e</c> (convention 13).</summary>
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 decision 1; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    string Release { get; }

    /// <summary>
    /// The identifiers whose primary identifier is themselves, in the tables' ordinal order: the list
    /// <c>AvailablePrimaryTimeZoneIdentifiers</c> (ECMA-402 6.5.3) and <c>Intl.supportedValuesOf("timeZone")</c> read.
    /// </summary>
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076, ECMA-402 6.5.3 via Broiler.VM JsTimeZones.PrimaryIdentifiers; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    IReadOnlyList<string> PrimaryIdentifiers { get; }

    /// <summary>
    /// Finds an available named time zone identifier: <c>GetAvailableNamedTimeZoneIdentifier</c> (ECMA-402
    /// 6.5.2) under the policy's case rule, an ASCII-case-insensitive match.
    /// </summary>
    /// <param name="identifier">The name to find, in any ASCII case.</param>
    /// <param name="id">The identifier found, in the database's case; <see langword="default"/> when none is.</param>
    /// <returns>Whether the name is available; <see langword="false"/> for a name holding a character above U+007F.</returns>
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076, ECMA-402 6.5.2 via Broiler.VM JsTimeZones.Find; IP=Low; Security=Medium; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    bool TryFind(ReadOnlySpan<char> identifier, out TimeZoneId id);

    /// <summary>
    /// The primary identifier an identifier resolves to (ECMA-262 21.4.1.22's <c>[[PrimaryIdentifier]]</c>):
    /// CLDR's, with <c>Etc/UTC</c>, <c>Etc/GMT</c> and <c>GMT</c> resolving to <c>UTC</c>; itself where it is one.
    /// </summary>
    /// <param name="id">An identifier this database found.</param>
    /// <returns>The primary identifier.</returns>
    /// <exception cref="ArgumentException">The identifier is not one this database gave.</exception>
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076, ECMA-262 21.4.1.22 via Broiler.VM JsTimeZoneId.Primary; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    TimeZoneId Primary(TimeZoneId id);

    /// <summary>
    /// The zones in use in a region, as <c>zone.tab</c> lists them, in ordinal order: what the Intl Locale
    /// Info proposal's <c>TimeZonesOfLocale</c> answers for <c>Intl.Locale.prototype.getTimeZones</c>.
    /// </summary>
    /// <param name="region">A region code, matched ordinally, for example <c>US</c>.</param>
    /// <returns>The region's zones; empty for a region <c>zone.tab</c> does not list.</returns>
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 Z12 via Broiler.VM JsTimeZones.Region; IP=Low; Security=Medium; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    IReadOnlyList<string> RegionTimeZones(ReadOnlySpan<char> region);

    /// <summary>The rules of the zone an identifier reads: its primary identifier's.</summary>
    /// <param name="id">An identifier this database found.</param>
    /// <returns>The zone's rules.</returns>
    /// <exception cref="ArgumentException">The identifier is not one this database gave.</exception>
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076, ECMA-402 6.5.1 via Broiler.VM JsTimeZones.Zone; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    ITimeZoneRules Rules(TimeZoneId id);
}

/// <summary>
/// One zone's UTC offsets over the instants, in whole seconds: what ECMA-262's
/// <c>GetNamedTimeZoneOffsetNanoseconds</c> (21.4.1.21) and the Temporal proposal's
/// <c>GetNamedTimeZoneNextTransition</c> and <c>GetNamedTimeZonePreviousTransition</c> read.
/// </summary>
/// <remarks>
/// <para>
/// <b>Instants are seconds since the epoch</b>, of magnitude at most <see cref="TimeZoneDatabase.MaxEpochSeconds"/>
/// (10^15, about 31.7 million years, beyond every instant ECMAScript represents); a zone's transitions fall
/// on whole seconds. An implementation refuses an instant beyond that bound with
/// <see cref="ArgumentOutOfRangeException"/>.
/// </para>
/// <para>
/// <b>There is no 2037 cliff</b> (decision SJL-D-20): after the last year the tables list, the zone's
/// recurring rules continue it without limit.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=Derived; Spec=JSD-0076, ECMA-262 21.4.1.21 via Broiler.VM JsZone; IP=Low; Security=Medium; Resources=1; Fingerprint=TBF
// Broiler-Human:        PENDING
public interface ITimeZoneRules
{
    /// <summary>The UTC offset in force at an instant, in seconds, positive east of UTC (ECMA-262 21.4.1.21, in seconds).</summary>
    /// <param name="epochSeconds">The instant, in seconds since the epoch.</param>
    /// <returns>The offset, of magnitude below a day.</returns>
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076, ECMA-262 21.4.1.21 via Broiler.VM JsZone.OffsetAt; IP=Low; Security=Medium; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    int OffsetSecondsAt(long epochSeconds);

    /// <summary>
    /// Whether the offset in force at an instant is daylight saving time, as CLDR's zone names read it
    /// (the compiled <c>isdst</c> flag; JSD-0058 in Broiler.VM's profile).
    /// </summary>
    /// <param name="epochSeconds">The instant, in seconds since the epoch.</param>
    /// <returns>Whether it is daylight time.</returns>
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076, JSD-0058 via Broiler.VM JsZone.DaylightAt; IP=Low; Security=Medium; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    bool IsDaylightAt(long epochSeconds);

    /// <summary>
    /// The first instant after <paramref name="afterEpochSeconds"/> and at or before
    /// <paramref name="limitEpochSeconds"/> at which the offset changes: the Temporal proposal's
    /// <c>GetNamedTimeZoneNextTransition</c>, in seconds.
    /// </summary>
    /// <param name="afterEpochSeconds">The instant to search after.</param>
    /// <param name="limitEpochSeconds">The last instant that may be answered; a limit beyond <see cref="TimeZoneDatabase.MaxEpochSeconds"/> is read as that bound.</param>
    /// <param name="daylightFlag">Whether a listed change of the daylight flag alone counts as a transition too, as zone names need.</param>
    /// <returns>The transition, or <see langword="null"/> where there is none up to the limit.</returns>
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076, Temporal GetNamedTimeZoneNextTransition via Broiler.VM JsZone.NextTransition; IP=Low; Security=Medium; Resources=2; Fingerprint=TBF
    // Broiler-Human:        PENDING
    long? NextTransition(long afterEpochSeconds, long limitEpochSeconds, bool daylightFlag);

    /// <summary>
    /// The last instant before <paramref name="beforeEpochSeconds"/> at which the offset changes: the
    /// Temporal proposal's <c>GetNamedTimeZonePreviousTransition</c>, in seconds.
    /// </summary>
    /// <param name="beforeEpochSeconds">The instant to search before.</param>
    /// <param name="daylightFlag">Whether a listed change of the daylight flag alone counts as a transition too, as zone names need.</param>
    /// <returns>The transition, or <see langword="null"/> where there is none.</returns>
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076, Temporal GetNamedTimeZonePreviousTransition via Broiler.VM JsZone.PreviousTransition; IP=Low; Security=Medium; Resources=2; Fingerprint=TBF
    // Broiler-Human:        PENDING
    long? PreviousTransition(long beforeEpochSeconds, bool daylightFlag);
}

/// <summary>
/// The decoder of the data home's compact time-zone tables (JSD-0076; the format of Broiler.VM's
/// <c>TzdbTableGenerator</c>, JSD-0053): an <see cref="ITimeZoneDatabase"/> over the bytes an engine
/// hands in. The library embeds no table.
/// </summary>
/// <remarks>
/// <para>
/// <b>The three tables</b> are the generator's: the zone table, binary; the identifier table, ASCII lines
/// of <c>identifier|zone index|primary identifier</c>, the primary empty where the identifier is one; and
/// the region table, ASCII lines of <c>region|zone|zone…</c>. Lines end with a line feed, and an empty
/// line is skipped.
/// </para>
/// <para>
/// <b>The zone table's layout</b>: a zone count, then per zone a zig-zag first offset in seconds, a palette
/// of offsets (each twice the offset, and one more where it is daylight time), a transition count and
/// each transition as a time delta - minutes with the low bit clear, seconds with it set - and a palette
/// index byte, then a byte counting the recurring rules and, where there are any, the standard offset,
/// the last listed year and the rules. Every integer is a LEB128 varint, and a signed one zig-zag encoded.
/// </para>
/// <para>
/// <b>The bytes are copied and checked once</b>, when the database is made: a table that does not hold
/// that layout to its last byte is refused with <see cref="ArgumentException"/>, as a broken precondition.
/// A zone is decoded the first time its rules are asked for, so a program that names one zone decodes
/// one. Every member is safe to call concurrently.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=Derived; Spec=JSD-0076, JSD-0053 via Broiler.VM JsTimeZones; IP=Low; Security=Medium; Resources=2; Fingerprint=TBF
// Broiler-Human:        PENDING
public sealed class TimeZoneDatabase : ITimeZoneDatabase
{
    /// <summary>
    /// The largest magnitude of an instant, in seconds since the epoch, that the rules answer: 10^15, about
    /// 31.7 million years, far beyond ECMAScript's ±8.64 × 10^12 seconds, and near enough that every day a
    /// recurring rule is evaluated on is a time value ECMA-262's <c>WeekDay</c> reads exactly.
    /// </summary>
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 X3; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    public const long MaxEpochSeconds = 1_000_000_000_000_000L;

    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 via Broiler.VM JsTimeZones; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private readonly byte[] table;

    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 via Broiler.VM JsTimeZones; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private readonly string[] names;

    // The index of each identifier's primary identifier, itself where it is one.
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 via Broiler.VM JsTimeZones; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private readonly int[] primaryOf;

    // The zone each identifier reads: its primary identifier's.
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 via Broiler.VM JsTimeZones; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private readonly int[] zoneOf;

    // The identifiers folded to ASCII lower case, in ordinal order, and the index each one is.
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 via Broiler.VM JsTimeZones; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private readonly string[] foldedKeys;

    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 via Broiler.VM JsTimeZones; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private readonly int[] foldedIndex;

    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 via Broiler.VM JsTimeZones; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private readonly ReadOnlyCollection<string> primaries;

    // The region codes in ordinal order, and each one's zones.
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 via Broiler.VM JsTimeZones; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private readonly string[] regionCodes;

    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 via Broiler.VM JsTimeZones; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private readonly ReadOnlyCollection<string>[] regionZones;

    // Where each zone starts in the zone table.
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 via Broiler.VM JsTimeZones; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private readonly int[] starts;

    // Each zone, once decoded.
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 via Broiler.VM JsTimeZones; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private readonly TableZoneRules?[] zones;

    /// <summary>Reads the three tables an engine hands in.</summary>
    /// <param name="release">The release the tables were compiled from, for example <c>2026e</c>.</param>
    /// <param name="zoneTable">The binary zone table.</param>
    /// <param name="identifierTable">The identifier table, ASCII lines.</param>
    /// <param name="regionTable">The region table, ASCII lines.</param>
    /// <exception cref="ArgumentException">The release is empty, or a table does not hold the generator's layout.</exception>
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076, JSD-0053 via Broiler.VM JsTimeZones..ctor; IP=Low; Security=Medium; Resources=3; Fingerprint=TBF
    // Broiler-Human:        PENDING
    public TimeZoneDatabase(string release, ReadOnlySpan<byte> zoneTable, ReadOnlySpan<byte> identifierTable, ReadOnlySpan<byte> regionTable)
    {
        ArgumentException.ThrowIfNullOrEmpty(release);
        Release = release;
        table = zoneTable.ToArray();

        // ONE PASS OVER THE ZONE TABLE finds where each zone starts and checks its layout; nothing is decoded yet.
        var at = 0;
        var count = TableReader.Count(table, ref at);
        starts = new int[count];
        zones = new TableZoneRules?[count];
        for (var zone = 0; zone < count; zone++)
        {
            starts[zone] = at;
            TableZoneRules.Skip(table, ref at);
        }

        if (at != table.Length)
            throw TableReader.Malformed("the zone table holds bytes after its last zone");

        var lines = TableReader.Lines(identifierTable);
        names = new string[lines.Count];
        var zoneFields = new int[lines.Count];
        var primaryNames = new string?[lines.Count];
        var primaryList = new List<string>();
        for (var i = 0; i < lines.Count; i++)
        {
            var fields = lines[i].Split('|');
            if (fields.Length < 3 || fields[0].Length == 0)
                throw TableReader.Malformed("an identifier line does not have its three fields");

            names[i] = fields[0];
            zoneFields[i] = TableReader.Index(fields[1], count);
            if (fields[2].Length == 0)
                primaryList.Add(fields[0]);
            else
                primaryNames[i] = fields[2];
        }

        primaries = new ReadOnlyCollection<string>(primaryList.ToArray());
        (foldedKeys, foldedIndex) = FoldedIndex(names);

        // AN IDENTIFIER READS ITS PRIMARY IDENTIFIER'S ZONE, so two identifiers that resolve to one primary
        // never disagree (ECMA-402 6.5.1): EST5EDT, a Zone of its own in tzdb, is CLDR's America/New_York
        // and reads that zone's history.
        primaryOf = new int[names.Length];
        zoneOf = new int[names.Length];
        for (var i = 0; i < names.Length; i++)
        {
            var primary = i;
            if (primaryNames[i] is { } primaryName)
            {
                primary = FindExact(primaryName);
                if (primary < 0 || primaryNames[primary] is not null)
                    throw TableReader.Malformed("a primary identifier is not an identifier that is its own primary");
            }

            primaryOf[i] = primary;
            zoneOf[i] = zoneFields[primary];
        }

        var regionLines = TableReader.Lines(regionTable);
        var regions = new SortedDictionary<string, ReadOnlyCollection<string>>(StringComparer.Ordinal);
        foreach (var line in regionLines)
        {
            var fields = line.Split('|');
            regions[fields[0]] = new ReadOnlyCollection<string>(fields[1..]);
        }

        regionCodes = new string[regions.Count];
        regionZones = new ReadOnlyCollection<string>[regions.Count];
        var r = 0;
        foreach (var (code, zoneNames) in regions)
        {
            regionCodes[r] = code;
            regionZones[r] = zoneNames;
            r++;
        }
    }

    /// <inheritdoc/>
    public string Release { get; }

    /// <inheritdoc/>
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076, ECMA-402 6.5.3 via Broiler.VM JsTimeZones.PrimaryIdentifiers; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    public IReadOnlyList<string> PrimaryIdentifiers => primaries;

    /// <inheritdoc/>
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076, ECMA-402 6.5.2 via Broiler.VM JsTimeZones.Find; IP=Low; Security=High; Resources=2; Fingerprint=TBF
    // Broiler-Falsified-If: a name holding a character above U+007F is found, a name differing from an identifier in anything but ASCII case is found, or an identifier in any ASCII case is not found
    // Broiler-Human:        PENDING
    public bool TryFind(ReadOnlySpan<char> identifier, out TimeZoneId id)
    {
        id = default;
        foreach (var c in identifier)
        {
            if (c > 0x7F)
                return false;
        }

        var low = 0;
        var high = foldedKeys.Length - 1;
        while (low <= high)
        {
            var middle = low + ((high - low) >> 1);
            var order = CompareFolded(foldedKeys[middle], identifier);
            if (order == 0)
            {
                var index = foldedIndex[middle];
                id = new TimeZoneId(index, names[index]);
                return true;
            }

            if (order < 0)
                low = middle + 1;
            else
                high = middle - 1;
        }

        return false;
    }

    /// <inheritdoc/>
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076, ECMA-262 21.4.1.22 via Broiler.VM JsTimeZoneId.Primary; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    public TimeZoneId Primary(TimeZoneId id)
    {
        var primary = primaryOf[Check(id)];
        return new TimeZoneId(primary, names[primary]);
    }

    /// <inheritdoc/>
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 Z12 via Broiler.VM JsTimeZones.Region; IP=Low; Security=Medium; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    public IReadOnlyList<string> RegionTimeZones(ReadOnlySpan<char> region)
    {
        var low = 0;
        var high = regionCodes.Length - 1;
        while (low <= high)
        {
            var middle = low + ((high - low) >> 1);
            var order = regionCodes[middle].AsSpan().SequenceCompareTo(region);
            if (order == 0)
                return regionZones[middle];

            if (order < 0)
                low = middle + 1;
            else
                high = middle - 1;
        }

        return ReadOnlyCollection<string>.Empty;
    }

    /// <inheritdoc/>
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076, ECMA-402 6.5.1 via Broiler.VM JsTimeZones.Zone; IP=Low; Security=Medium; Resources=2; Fingerprint=TBF
    // Broiler-Human:        PENDING
    public ITimeZoneRules Rules(TimeZoneId id)
    {
        var zone = zoneOf[Check(id)];
        var rules = Volatile.Read(ref zones[zone]);
        if (rules is not null)
            return rules;

        // TWO THREADS MAY DECODE ONE ZONE; both decodings are equal, and the first stored is kept.
        rules = TableZoneRules.Decode(table, starts[zone]);
        return Interlocked.CompareExchange(ref zones[zone], rules, null) ?? rules;
    }

    // The identifier's index, where it is one this database gave.
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 X3; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private int Check(TimeZoneId id)
    {
        if (id.Index < 0 || id.Index >= names.Length || !string.Equals(names[id.Index], id.Identifier, StringComparison.Ordinal))
            throw new ArgumentException("The time zone identifier is not one this database gave.", nameof(id));

        return id.Index;
    }

    // The index of an identifier written exactly as the table writes it, or -1.
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 via Broiler.VM JsTimeZones..ctor; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private int FindExact(string identifier) =>
        TryFind(identifier, out var id) && string.Equals(id.Identifier, identifier, StringComparison.Ordinal) ? id.Index : -1;

    // The identifiers folded to ASCII lower case and sorted ordinally, with the index each one is; two
    // identifiers that fold to one key make the table malformed.
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076, ECMA-402 6.5.2; IP=Low; Security=Low; Resources=2; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private static (string[] Keys, int[] Index) FoldedIndex(string[] identifiers)
    {
        var keys = new string[identifiers.Length];
        var index = new int[identifiers.Length];
        for (var i = 0; i < identifiers.Length; i++)
        {
            foreach (var c in identifiers[i])
            {
                if (c > 0x7F)
                    throw TableReader.Malformed("an identifier holds a character above U+007F");
            }

            keys[i] = string.Create(identifiers[i].Length, identifiers[i], static (span, text) =>
            {
                for (var j = 0; j < text.Length; j++)
                    span[j] = Fold(text[j]);
            });
            index[i] = i;
        }

        Array.Sort(keys, index, StringComparer.Ordinal);
        for (var i = 1; i < keys.Length; i++)
        {
            if (string.Equals(keys[i - 1], keys[i], StringComparison.Ordinal))
                throw TableReader.Malformed("two identifiers differ only in ASCII case");
        }

        return (keys, index);
    }

    // A folded key against an identifier still to fold, ordinally.
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076, ECMA-402 6.5.2; IP=Low; Security=Medium; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private static int CompareFolded(string key, ReadOnlySpan<char> identifier)
    {
        var length = Math.Min(key.Length, identifier.Length);
        for (var i = 0; i < length; i++)
        {
            var difference = key[i] - Fold(identifier[i]);
            if (difference != 0)
                return difference;
        }

        return key.Length - identifier.Length;
    }

    // ASCII lower case of a character; any other character is itself.
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076, ECMA-402 6.5.2; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private static char Fold(char c) => c is >= 'A' and <= 'Z' ? (char)(c + ('a' - 'A')) : c;
}

/// <summary>A recurring tzdb rule a zone continues by after its listed transitions, as the generator encodes it.</summary>
/// <param name="Month">The month, 1 to 12.</param>
/// <param name="OnKind">How the day is given: 0 a fixed day, 1 the last weekday of the month, 2 the first weekday on or after the day, 3 the last weekday on or before it.</param>
/// <param name="Weekday">The weekday, Monday 0 to Sunday 6, for the kinds that name one.</param>
/// <param name="Day">The day of the month, for the kinds that name one.</param>
/// <param name="At">The time of day, in seconds.</param>
/// <param name="AtKind">How the time is read: 2 universal time, 1 standard time, and any other value wall time.</param>
/// <param name="Save">The seconds the rule adds to the standard offset.</param>
// Broiler-AI:           Origin=Derived; Spec=JSD-0076, JSD-0053 via Broiler.VM JsZoneRule; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
// Broiler-Human:        PENDING
internal readonly record struct ZoneRule(int Month, int OnKind, int Weekday, int Day, int At, int AtKind, int Save);

/// <summary>
/// One zone's UTC offsets decoded from the compact table: the offset before its first transition, each
/// listed transition, and the rules that continue them after the last listed year - the reading
/// Broiler.VM's <c>TzdbCompiler</c> encodes and its rule N30 holds to <c>zic</c>'s.
/// </summary>
// Broiler-AI:           Origin=Derived; Spec=JSD-0076, ECMA-262 21.4.1.21 via Broiler.VM JsZone; IP=Low; Security=Medium; Resources=2; Fingerprint=TBF
// Broiler-Human:        PENDING
internal sealed class TableZoneRules : ITimeZoneRules
{
    // The mean Gregorian year in seconds, by which a recurring zone tells which year an instant is in.
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 via Broiler.VM JsZone.SecondsPerYear; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private const double SecondsPerYear = 31556952.0;

    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 via Broiler.VM JsZone; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private readonly int initial;

    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 via Broiler.VM JsZone; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private readonly long[] transitions;

    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 via Broiler.VM JsZone; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private readonly int[] offsets;

    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 via Broiler.VM JsZone; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private readonly bool[] daylight;

    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 via Broiler.VM JsZone; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private readonly int standard;

    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 via Broiler.VM JsZone; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private readonly int lastListedYear;

    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 via Broiler.VM JsZone; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private readonly ZoneRule[] rules;

    // Whether the recurring rules change the offset at all: they do where two of them save differently.
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 via Broiler.VM JsZone.RulesChangeOffset; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private readonly bool rulesChangeOffset;

    // The least save of the recurring rules, or zero: a save above it is daylight time.
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 via Broiler.VM JsZone.DaylightAt; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private readonly int leastSave;

    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 via Broiler.VM JsZone..ctor; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private TableZoneRules(int initial, long[] transitions, int[] offsets, bool[] daylight, int standard, int lastListedYear, ZoneRule[] rules)
    {
        this.initial = initial;
        this.transitions = transitions;
        this.offsets = offsets;
        this.daylight = daylight;
        this.standard = standard;
        this.lastListedYear = lastListedYear;
        this.rules = rules;

        for (var i = 1; i < rules.Length; i++)
        {
            if (rules[i].Save != rules[0].Save)
                rulesChangeOffset = true;
        }

        foreach (var rule in rules)
            leastSave = Math.Min(leastSave, rule.Save);
    }

    /// <summary>Steps over one zone of the table, checking its layout.</summary>
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076, JSD-0053 via Broiler.VM JsZone.Skip; IP=Low; Security=Medium; Resources=2; Fingerprint=TBF
    // Broiler-Human:        PENDING
    internal static void Skip(byte[] table, ref int at)
    {
        TableReader.Signed(table, ref at);
        var palette = TableReader.Count(table, ref at);
        for (var i = 0; i < palette; i++)
            TableReader.Signed(table, ref at);

        var count = TableReader.Count(table, ref at);
        for (var i = 0; i < count; i++)
        {
            TableReader.Unsigned(table, ref at);
            if (TableReader.Byte(table, ref at) >= palette)
                throw TableReader.Malformed("a transition names an offset outside its zone's palette");
        }

        var ruleCount = TableReader.Byte(table, ref at);
        if (ruleCount == 0)
            return;

        TableReader.Signed(table, ref at);
        TableReader.Signed(table, ref at);
        for (var i = 0; i < ruleCount; i++)
        {
            var month = TableReader.Byte(table, ref at);
            var kind = TableReader.Byte(table, ref at);
            var weekday = TableReader.Byte(table, ref at);
            var day = TableReader.Byte(table, ref at);

            // A WEEKDAY OUTSIDE THE WEEK WOULD NEVER BE FOUND, and a month outside the year has no days.
            if (month is < 1 or > 12 || (kind is >= 1 and <= 3 && weekday > 6) || (kind != 1 && day is < 1 or > 31))
                throw TableReader.Malformed("a recurring rule names no day");

            TableReader.Signed(table, ref at);
            TableReader.Byte(table, ref at);
            TableReader.Signed(table, ref at);
        }
    }

    /// <summary>Decodes the zone that starts at <paramref name="at"/>, which <see cref="Skip"/> has checked.</summary>
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076, JSD-0053 via Broiler.VM JsZone.Decode; IP=Low; Security=Medium; Resources=2; Fingerprint=TBF
    // Broiler-Human:        PENDING
    internal static TableZoneRules Decode(byte[] table, int at)
    {
        var initial = (int)TableReader.Signed(table, ref at);

        // A PALETTE ENTRY IS TWICE THE OFFSET, and one more where it is daylight time.
        var palette = new long[TableReader.Count(table, ref at)];
        for (var i = 0; i < palette.Length; i++)
            palette[i] = TableReader.Signed(table, ref at);

        var count = TableReader.Count(table, ref at);
        var times = new long[count];
        var offsets = new int[count];
        var daylight = new bool[count];
        var previous = 0L;
        for (var i = 0; i < count; i++)
        {
            var code = TableReader.Unsigned(table, ref at);
            var zigzag = code >> 1;
            var delta = (long)(zigzag >> 1) ^ -(long)(zigzag & 1);
            previous += (code & 1) == 0 ? delta * 60 : delta;
            times[i] = previous;
            var entry = palette[TableReader.Byte(table, ref at)];
            offsets[i] = (int)(entry >> 1);
            daylight[i] = (entry & 1) != 0;
        }

        var ruleCount = TableReader.Byte(table, ref at);
        if (ruleCount == 0)
            return new TableZoneRules(initial, times, offsets, daylight, 0, 0, []);

        var standard = (int)TableReader.Signed(table, ref at);
        var lastListed = (int)TableReader.Signed(table, ref at);
        var rules = new ZoneRule[ruleCount];
        for (var i = 0; i < ruleCount; i++)
        {
            int month = TableReader.Byte(table, ref at), kind = TableReader.Byte(table, ref at);
            int weekday = TableReader.Byte(table, ref at), day = TableReader.Byte(table, ref at);
            var atTime = (int)TableReader.Signed(table, ref at);
            int atKind = TableReader.Byte(table, ref at);
            rules[i] = new ZoneRule(month, kind, weekday, day, atTime, atKind, (int)TableReader.Signed(table, ref at));
        }

        return new TableZoneRules(initial, times, offsets, daylight, standard, lastListed, rules);
    }

    /// <inheritdoc/>
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076, ECMA-262 21.4.1.21 via Broiler.VM JsZone.OffsetAt; IP=Low; Security=Medium; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    public int OffsetSecondsAt(long epochSeconds)
    {
        CheckInstant(epochSeconds, nameof(epochSeconds));
        if (rules.Length > 0 && YearOf(epochSeconds) > lastListedYear)
            return RecurringOffsetAt(epochSeconds);

        var index = Array.BinarySearch(transitions, epochSeconds);
        index = index >= 0 ? index : ~index - 1;
        return index < 0 ? initial : offsets[index];
    }

    /// <inheritdoc/>
    /// <remarks>
    /// The listed transition's flag, or after them a recurring save greater than the rules' least, which is
    /// standard time where it is negative (Europe/Dublin's winter, JSD-0058).
    /// </remarks>
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076, JSD-0058 via Broiler.VM JsZone.DaylightAt; IP=Low; Security=Medium; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    public bool IsDaylightAt(long epochSeconds)
    {
        CheckInstant(epochSeconds, nameof(epochSeconds));
        if (rules.Length > 0 && YearOf(epochSeconds) > lastListedYear)
            return RecurringOffsetAt(epochSeconds) - standard > leastSave;

        var index = Array.BinarySearch(transitions, epochSeconds);
        index = index >= 0 ? index : ~index - 1;
        return index >= 0 && daylight[index];
    }

    /// <inheritdoc/>
    /// <remarks>
    /// The listed transitions are authoritative through the last listed year; the recurring rules answer
    /// only for the years after it, where a change of the daylight flag alone is no transition.
    /// </remarks>
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076, Temporal GetNamedTimeZoneNextTransition via Broiler.VM JsZone.NextTransition; IP=Low; Security=High; Resources=2; Fingerprint=TBF
    // Broiler-Falsified-If: a transition at or before the instant is answered, one after the limit is answered, a later transition is answered where an earlier one changes the offset, or none is answered after 2037 where the recurring rules change the offset
    // Broiler-Human:        PENDING
    public long? NextTransition(long afterEpochSeconds, long limitEpochSeconds, bool daylightFlag)
    {
        CheckInstant(afterEpochSeconds, nameof(afterEpochSeconds));
        var limit = Math.Clamp(limitEpochSeconds, -TimeZoneDatabase.MaxEpochSeconds, TimeZoneDatabase.MaxEpochSeconds);

        var index = Array.BinarySearch(transitions, afterEpochSeconds);
        index = index >= 0 ? index + 1 : ~index;
        for (; index < transitions.Length; index++)
        {
            if (Changes(index, daylightFlag))
                return transitions[index] <= limit ? transitions[index] : null;
        }

        if (!rulesChangeOffset)
            return null;

        for (var year = Math.Max(YearOf(afterEpochSeconds) - 1, lastListedYear + 1); year <= YearOf(limit) + 1; year++)
        {
            foreach (var (at, before, after) in RecurringTransitions(year, year))
            {
                if (at > afterEpochSeconds && before != after)
                    return at <= limit ? at : null;
            }
        }

        return null;
    }

    /// <inheritdoc/>
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076, Temporal GetNamedTimeZonePreviousTransition via Broiler.VM JsZone.PreviousTransition; IP=Low; Security=High; Resources=2; Fingerprint=TBF
    // Broiler-Falsified-If: a transition at or after the instant is answered, an earlier transition is answered where a later one before the instant changes the offset, or none is answered after 2037 where the recurring rules change the offset
    // Broiler-Human:        PENDING
    public long? PreviousTransition(long beforeEpochSeconds, bool daylightFlag)
    {
        CheckInstant(beforeEpochSeconds, nameof(beforeEpochSeconds));
        if (rulesChangeOffset)
        {
            for (var year = YearOf(beforeEpochSeconds) + 1; year > lastListedYear; year--)
            {
                var recurring = RecurringTransitions(year, year);
                for (var i = recurring.Count - 1; i >= 0; i--)
                {
                    var (at, before, after) = recurring[i];
                    if (at < beforeEpochSeconds && before != after)
                        return at;
                }
            }
        }

        var index = Array.BinarySearch(transitions, beforeEpochSeconds);
        index = index >= 0 ? index - 1 : ~index - 1;
        for (; index >= 0; index--)
        {
            if (Changes(index, daylightFlag))
                return transitions[index];
        }

        return null;
    }

    // An instant the rules answer: of magnitude at most 10^15 seconds.
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 X3; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private static void CheckInstant(long epochSeconds, string name)
    {
        if (epochSeconds is < -TimeZoneDatabase.MaxEpochSeconds or > TimeZoneDatabase.MaxEpochSeconds)
            throw new ArgumentOutOfRangeException(name, epochSeconds, "An instant is at most 10^15 seconds from the epoch.");
    }

    // The year an instant is in, by the mean Gregorian year from 1970: the reckoning the recurring rules switch on.
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 via Broiler.VM JsZone.YearOf; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private static int YearOf(long seconds) => 1970 + (int)Math.Floor(seconds / SecondsPerYear);

    // Whether a listed transition changes the offset, or, where asked, the daylight flag.
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 via Broiler.VM JsZone.Changes; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private bool Changes(int index, bool daylightFlag)
    {
        var (offset, flag) = index == 0 ? (initial, false) : (offsets[index - 1], daylight[index - 1]);
        return offsets[index] != offset || (daylightFlag && daylight[index] != flag);
    }

    // The offset the recurring rules give an instant: the last save of the year two before, then each
    // transition of the years either side up to the instant.
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 via Broiler.VM JsZone.RecurringOffsetAt; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private int RecurringOffsetAt(long seconds)
    {
        var year = YearOf(seconds);
        var events = Events(year - 2);
        var offset = standard + events[^1].Save;

        foreach (var (at, _, after) in RecurringTransitions(year - 1, year + 1))
        {
            if (at > seconds)
                break;

            offset = after;
        }

        return offset;
    }

    // The recurring rules' instants in a range of years, each with the offsets before and after it.
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 via Broiler.VM JsZone.RecurringTransitions; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private List<(long At, int Before, int After)> RecurringTransitions(int first, int last)
    {
        var save = Events(first - 1)[^1].Save;
        var result = new List<(long At, int Before, int After)>(rules.Length * (last - first + 1));

        for (var year = first; year <= last; year++)
        {
            foreach (var (local, kind, next) in Events(year))
            {
                var at = kind switch
                {
                    2 => local,
                    1 => local - standard,
                    _ => local - standard - save,
                };

                result.Add((at, standard + save, standard + next));
                save = next;
            }
        }

        return result;
    }

    // The recurring rules' occurrences in one year, in order of local time; two at one local time keep the
    // generator's order.
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 via Broiler.VM JsZone.Events; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private List<(long Local, int Kind, int Save)> Events(int year)
    {
        var events = new List<(long Local, int Kind, int Save)>(rules.Length);
        foreach (var rule in rules)
            events.Add(((Day(year, rule) * 86400L) + rule.At, rule.AtKind, rule.Save));

        for (var i = 1; i < events.Count; i++)
        {
            var item = events[i];
            var j = i - 1;
            while (j >= 0 && events[j].Local > item.Local)
            {
                events[j + 1] = events[j];
                j--;
            }

            events[j + 1] = item;
        }

        return events;
    }

    // The day, since 1970-01-01, a rule falls on in a year.
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 via Broiler.VM JsZone.Day; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private static long Day(int year, ZoneRule rule)
    {
        long day;
        switch (rule.OnKind)
        {
            case 1:
                day = IsoCalendarMath.EpochDays(year, rule.Month, IsoCalendarMath.DaysInMonth(year, rule.Month));
                while (MondayFirst(day) != rule.Weekday)
                    day--;

                return day;

            case 2:
            case 3:
                day = IsoCalendarMath.EpochDays(year, rule.Month, rule.Day);
                while (MondayFirst(day) != rule.Weekday)
                    day += rule.OnKind == 2 ? 1 : -1;

                return day;

            default:
                return IsoCalendarMath.EpochDays(year, rule.Month, rule.Day);
        }
    }

    // The weekday of a day since 1970-01-01 as a zone rule counts it, Monday 0 to Sunday 6: ECMA-262's
    // WeekDay (21.4.1.13, Sunday 0) of that day's first instant, mapped.
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076, ECMA-262 21.4.1.13 via Broiler.VM JsZone.MondayFirst; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private static int MondayFirst(long day) => ((int)EcmaTime.WeekDay(day * EcmaTime.MsPerDay) + 6) % 7;
}

/// <summary>The readers of the compact tables' integers and lines, each checking that the table holds what it reads.</summary>
// Broiler-AI:           Origin=Derived; Spec=JSD-0076, JSD-0053 via Broiler.VM JsTimeZones.Unsigned; IP=Low; Security=Medium; Resources=1; Fingerprint=TBF
// Broiler-Human:        PENDING
internal static class TableReader
{
    // A LEB128 varint.
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 via Broiler.VM JsTimeZones.Unsigned; IP=Low; Security=Medium; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    internal static ulong Unsigned(byte[] bytes, ref int at)
    {
        ulong value = 0;
        for (var shift = 0; shift < 64; shift += 7)
        {
            var b = Byte(bytes, ref at);
            value |= (ulong)(b & 0x7F) << shift;
            if (b < 0x80)
                return value;
        }

        throw Malformed("a varint runs past 64 bits");
    }

    // A zig-zag encoded LEB128 varint.
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 via Broiler.VM JsTimeZones.Signed; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    internal static long Signed(byte[] bytes, ref int at)
    {
        var value = Unsigned(bytes, ref at);
        return (long)(value >> 1) ^ -(long)(value & 1);
    }

    // A count of what follows, which the rest of the table must be able to hold.
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 via Broiler.VM JsTimeZones..ctor; IP=Low; Security=Medium; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    internal static int Count(byte[] bytes, ref int at)
    {
        var count = Unsigned(bytes, ref at);
        if (count > (ulong)(bytes.Length - at))
            throw Malformed("a count is larger than the rest of the table");

        return (int)count;
    }

    // One byte.
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 via Broiler.VM JsZone.Decode; IP=Low; Security=Medium; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    internal static byte Byte(byte[] bytes, ref int at)
    {
        if ((uint)at >= (uint)bytes.Length)
            throw Malformed("the table ends inside a zone");

        return bytes[at++];
    }

    // A zone index field: ASCII digits naming a zone of the table.
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 via Broiler.VM JsTimeZones..ctor; IP=Low; Security=Medium; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    internal static int Index(string field, int count)
    {
        if (field.Length is 0 or > 9)
            throw Malformed("a zone index is not a number");

        var value = 0;
        foreach (var c in field)
        {
            if (c is < '0' or > '9')
                throw Malformed("a zone index is not a number");

            value = (value * 10) + (c - '0');
        }

        if (value >= count)
            throw Malformed("a zone index names no zone");

        return value;
    }

    // The non-empty lines of an ASCII text table.
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 via Broiler.VM JsIntlTables.Lines; IP=Low; Security=Medium; Resources=2; Fingerprint=TBF
    // Broiler-Human:        PENDING
    internal static List<string> Lines(ReadOnlySpan<byte> text)
    {
        var lines = new List<string>();
        while (!text.IsEmpty)
        {
            var end = text.IndexOf((byte)'\n');
            var line = end < 0 ? text : text[..end];
            if (!line.IsEmpty)
            {
                lines.Add(string.Create(line.Length, line.ToArray(), static (span, bytes) =>
                {
                    for (var i = 0; i < bytes.Length; i++)
                    {
                        if (bytes[i] > 0x7F)
                            throw Malformed("a text table holds a byte above U+007F");

                        span[i] = (char)bytes[i];
                    }
                }));
            }

            text = end < 0 ? default : text[(end + 1)..];
        }

        return lines;
    }

    // The exception a table that does not hold the generator's layout is refused with.
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 X3; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    internal static ArgumentException Malformed(string what) =>
        new("The time-zone or calendar table is malformed: " + what + ".");
}
