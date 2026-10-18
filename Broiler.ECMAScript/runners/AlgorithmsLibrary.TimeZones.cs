using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using Broiler.ECMAScript.Conformance;
using Broiler.ECMAScript.Time;

namespace Broiler.ECMAScript.Runners;

/// <summary>
/// The time-zone area (corpus revision 21, milestone SJL-8.4): each operation as an engine's adapter calls
/// <c>Broiler.ECMAScript.Time</c>, over the data home's generated tables, with the script's own reading of the
/// result where a vector's script shows it through a built-in.
/// </summary>
/// <remarks>
/// <para>
/// <b>The tables are the data home's</b>, <c>Broiler.ECMAScript/data/time/generated/</c>, read from disk as committed
/// and checked against the lengths and SHA-256 their manifest records, then handed to <see cref="TimeZoneDatabase"/>
/// as an engine hands in the tables it embeds (decision SJL-D-6 (c)). No library carries them, and this file reads
/// them only where a time-zone vector is run.
/// </para>
/// <para>
/// What a vector's script does around the library is written here once more, as each engine's adapter does it: the
/// reading of a time zone argument (an offset identifier, else a name the database finds, else, for Temporal's
/// <c>ToTemporalTimeZoneIdentifier</c>, the zone of an ISO date-time string), the formatter's offset with seconds
/// refused, the mapping of every status to a <c>RangeError</c>, the local date of an instant, the hours between two
/// starts of day, the script's walk through transitions, and the joins. None of it is a zone's offset, a transition,
/// a disambiguation or a lookup; every such step is the library's.
/// </para>
/// <para>
/// The formatter's resolved name for a non-primary identifier is implementation-defined (row Z8): the engines'
/// expectations differ, so the library is compared with <c>out</c>, the identifier the lookup gives.
/// </para>
/// </remarks>
internal sealed partial class AlgorithmsLibrary
{
    private const long NsPerDay = 86_400_000_000_000L;

    private static readonly Lazy<TimeZoneDatabase> TimeZoneTables = new(LoadTimeZoneTables);

    /// <summary>The database every time-zone vector reads.</summary>
    private static TimeZoneDatabase Zones => TimeZoneTables.Value;

    private static string TimeZoneArea(string operation, IReadOnlyList<TypedValue> inputs)
    {
        switch (operation)
        {
            // Offsets and transitions, through Temporal.Instant's toZonedDateTimeISO.
            case "GetOffsetNanosecondsFor":
                return Zoned(inputs[0].Chars) is { } offsetZone
                    ? Number(ZonedTime.GetOffsetNanosecondsFor(offsetZone.Rules, inputs[1].Integer))
                    : RangeError;
            case "GetNamedTimeZoneNextTransition":
            case "GetNamedTimeZonePreviousTransition":
            {
                if (Zoned(inputs[0].Chars) is not { } zone)
                    return RangeError;

                var found = operation == "GetNamedTimeZoneNextTransition"
                    ? ZonedTime.GetNextTransition(zone.Rules, inputs[1].Integer)
                    : ZonedTime.GetPreviousTransition(zone.Rules, inputs[1].Integer);
                return found is { } transition ? BigIntValue(transition) : "value: null";
            }

            case "GetNamedTimeZoneNextTransition, count":
            {
                // The script's walk: getTimeZoneTransition("next") until none, or one at or after the end.
                if (Zoned(inputs[0].Chars) is not { } zone)
                    return RangeError;

                var count = 0;
                var at = inputs[1].Integer;
                while (ZonedTime.GetNextTransition(zone.Rules, at) is { } next && next < inputs[2].Integer)
                {
                    count++;
                    at = next;
                }

                return Number(count);
            }

            // Wall-clock times, through PlainDateTime, ZonedDateTime.from and PlainDate.
            case "GetPossibleEpochNanoseconds":
            {
                if (Zoned(inputs[0].Chars) is not { } zone)
                    return RangeError;

                Span<BigInteger> possible = new BigInteger[ZonedTime.MaxPossibleEpochNanoseconds];
                if (ZonedTime.GetPossibleEpochNanoseconds(zone.Rules, WallTime(inputs), possible, out var count) != TimeStatus.Completed)
                    return RangeError;

                var joined = new StringBuilder();
                for (var i = 0; i < count; i++)
                    joined.Append(i == 0 ? string.Empty : ",").Append(possible[i].ToString(NumberFormatInfo.InvariantInfo));
                return Text(joined.ToString());
            }

            case "GetEpochNanosecondsFor":
                return Zoned(inputs[0].Chars) is { } wallZone
                    ? Instant(ZonedTime.GetEpochNanosecondsFor(wallZone.Rules, WallTime(inputs), DisambiguationOf(inputs[7].Chars), out var instant), instant)
                    : RangeError;
            case "GetStartOfDay":
                return Zoned(inputs[0].Chars) is { } dayZone
                    ? Instant(ZonedTime.GetStartOfDay(dayZone.Rules, DateOf(inputs), out var dayStart), dayStart)
                    : RangeError;
            case "Temporal.ZonedDateTime.prototype.hoursInDay":
            {
                // hoursInDay of the ZonedDateTime the date's start is: its own date's start and the next day's.
                if (Zoned(inputs[0].Chars) is not { } zone || ZonedTime.GetStartOfDay(zone.Rules, DateOf(inputs), out var start) != TimeStatus.Completed)
                    return RangeError;

                var today = LocalDate(zone.Rules, start);
                var tomorrow = IsoCalendarMath.FromEpochDays(IsoCalendarMath.EpochDays(today.Year, today.Month, today.Day) + 1);
                if (ZonedTime.GetStartOfDay(zone.Rules, today, out var todayStart) != TimeStatus.Completed
                    || ZonedTime.GetStartOfDay(zone.Rules, new IsoDate(tomorrow.Year, tomorrow.Month, tomorrow.Day), out var tomorrowStart) != TimeStatus.Completed)
                    return RangeError;

                return Number((double)(tomorrowStart - todayStart) / 3_600_000_000_000.0);
            }

            case "Temporal.ZonedDateTime.prototype.startOfDay":
                return Zoned(inputs[0].Chars) is { } instantZone
                    ? Instant(ZonedTime.GetStartOfDay(instantZone.Rules, LocalDate(instantZone.Rules, inputs[1].Integer), out var instantDayStart), instantDayStart)
                    : RangeError;

            // Identifiers.
            case "Temporal.ZonedDateTime, timeZoneId":
                return Zoned(inputs[0].Chars) is { } named ? Text(named.Identifier) : RangeError;
            case "Temporal.Instant.prototype.toZonedDateTimeISO, timeZoneId":
                return ToTemporalTimeZoneIdentifier(inputs[0].Chars) is { } read ? Text(read.Identifier) : RangeError;
            case "TimeZoneEquals":
                return Zoned(inputs[0].Chars) is { } one && Zoned(inputs[1].Chars) is { } two
                    ? Boolean(ZonedTime.TimeZoneEquals(Zones, one.Identifier, two.Identifier))
                    : RangeError;
            case "AvailablePrimaryTimeZoneIdentifiers": return Text(string.Join(",", Zones.PrimaryIdentifiers));
            case "AvailablePrimaryTimeZoneIdentifiers, count": return Number(Zones.PrimaryIdentifiers.Count);
            case "AvailablePrimaryTimeZoneIdentifiers, includes": return Boolean(Zones.PrimaryIdentifiers.Contains(inputs[0].Chars));

            // Intl.DateTimeFormat's timeZone, and Intl.Locale's regions.
            case "Intl.DateTimeFormat, resolved timeZone":
                return FormatterZone(inputs[0].Chars) is { } resolved ? Text(resolved.Identifier) : RangeError;
            case "Intl.DateTimeFormat, hour":
            {
                if (FormatterZone(inputs[0].Chars) is not { } zone)
                    return RangeError;

                var ns = inputs[1].Integer * 1_000_000;
                var local = ns + ZonedTime.GetOffsetNanosecondsFor(zone.Rules, ns);
                var withinDay = ((local % NsPerDay) + NsPerDay) % NsPerDay;
                return Number((double)(withinDay / 3_600_000_000_000L));
            }

            case "Intl.Locale.prototype.getTimeZones":
            {
                var tag = inputs[0].Chars;
                return Text(string.Join(",", Zones.RegionTimeZones(tag.AsSpan(tag.IndexOf('-', StringComparison.Ordinal) + 1))));
            }
        }

        return TemporalArea(operation, inputs);
    }

    private const string RangeError = "error: RangeError";

    /// <summary>A time zone an adapter has read: the identifier it reports, and the rules the library reads.</summary>
    private sealed record ReadZone(string Identifier, ITimeZoneRules Rules);

    /// <summary>
    /// The ZonedDateTime constructor's reading of a time zone, which Temporal's other entry points share for an
    /// identifier: an offset identifier, normalised, else an available name in its own case; null is the RangeError.
    /// </summary>
    private static ReadZone? Zoned(string text)
    {
        if (OffsetTimeZones.TryParseOffsetIdentifier(text, out var minutes))
        {
            var normalised = OffsetTimeZones.FormatOffsetIdentifier(minutes);
            return new ReadZone(normalised, new FixedOffsetTimeZoneRules(minutes * 60));
        }

        return Zones.TryFind(text, out var id)
            ? new ReadZone(id.Identifier, Zones.Rules(id))
            : null;
    }

    /// <summary>
    /// ToTemporalTimeZoneIdentifier of a String: an identifier, or else the zone of an ISO date-time string - its
    /// annotation, its <c>Z</c> as <c>UTC</c>, or its offset. The ISO reading is the engine's, and only the form
    /// <c>YYYY-MM-DDTHH:MM[:SS][Z|±HH:MM][[annotation]]</c> the vectors give is read here.
    /// </summary>
    private static ReadZone? ToTemporalTimeZoneIdentifier(string text)
    {
        if (Zoned(text) is { } zone)
            return zone;

        var at = 0;
        if (!Digits(text, ref at, 4) || !Literal(text, ref at, '-') || !Digits(text, ref at, 2) || !Literal(text, ref at, '-')
            || !Digits(text, ref at, 2) || !Literal(text, ref at, 'T') || !Digits(text, ref at, 2) || !Literal(text, ref at, ':')
            || !Digits(text, ref at, 2))
            return null;

        var seconds = at;
        if (!(Literal(text, ref seconds, ':') && Digits(text, ref seconds, 2)))
            seconds = at;

        at = seconds;
        string? offset = null;
        var utc = false;
        if (at < text.Length && text[at] == 'Z')
        {
            utc = true;
            at++;
        }
        else if (at < text.Length && text[at] is '+' or '-')
        {
            var start = at++;
            if (!Digits(text, ref at, 2) || !Literal(text, ref at, ':') || !Digits(text, ref at, 2))
                return null;

            offset = text[start..at];
        }

        if (at == text.Length)
            return utc ? Zoned("UTC") : offset is not null ? Zoned(offset) : null;

        if (text[at] != '[' || text[^1] != ']')
            return null;

        var annotation = text[(at + 1)..^1].TrimStart('!');
        return annotation.Length == 0 || annotation.Contains('=', StringComparison.Ordinal) || annotation.Contains(']', StringComparison.Ordinal)
            ? null
            : Zoned(annotation);
    }

    /// <summary>
    /// Intl.DateTimeFormat's reading of its <c>timeZone</c> (ECMA-402's CreateDateTimeFormat): an offset string,
    /// refused if it has seconds and normalised otherwise, else an available name; nothing else, an ISO date-time
    /// string included. Null is the RangeError.
    /// </summary>
    private static ReadZone? FormatterZone(string text)
    {
        if (OffsetTimeZones.IsTimeZoneOffsetString(text))
            return OffsetTimeZones.TryParseOffsetIdentifier(text, out _) ? Zoned(text) : null;

        return Zones.TryFind(text, out _) ? Zoned(text) : null;
    }

    private static bool Digits(string text, ref int at, int count)
    {
        if (at + count > text.Length)
            return false;

        for (var i = at; i < at + count; i++)
        {
            if (text[i] is < '0' or > '9')
                return false;
        }

        at += count;
        return true;
    }

    private static bool Literal(string text, ref int at, char expected)
    {
        if (at >= text.Length || text[at] != expected)
            return false;

        at++;
        return true;
    }

    /// <summary>The wall-clock time of inputs 1 to 6: year, month, day, hour, minute and second.</summary>
    private static IsoDateTime WallTime(IReadOnlyList<TypedValue> inputs) => new(
        DateOf(inputs),
        new IsoTime((int)inputs[4].Integer, (int)inputs[5].Integer, (int)inputs[6].Integer, 0, 0, 0));

    /// <summary>The date of inputs 1 to 3.</summary>
    private static IsoDate DateOf(IReadOnlyList<TypedValue> inputs) =>
        new((long)inputs[1].Integer, (int)inputs[2].Integer, (int)inputs[3].Integer);

    /// <summary>The ISO date of an instant in a zone: its wall-clock time's day.</summary>
    private static IsoDate LocalDate(ITimeZoneRules rules, BigInteger epochNanoseconds)
    {
        var local = epochNanoseconds + ZonedTime.GetOffsetNanosecondsFor(rules, epochNanoseconds);
        var days = BigInteger.DivRem(local, NsPerDay, out var remainder);
        if (remainder.Sign < 0)
            days -= 1;

        var (year, month, day) = IsoCalendarMath.FromEpochDays((long)days);
        return new IsoDate(year, month, day);
    }

    private static Disambiguation DisambiguationOf(string name) => name switch
    {
        "compatible" => Disambiguation.Compatible,
        "earlier" => Disambiguation.Earlier,
        "later" => Disambiguation.Later,
        "reject" => Disambiguation.Reject,
        _ => throw new FormatException("no disambiguation is named " + name),
    };

    /// <summary>An instant the library found, or the RangeError each engine maps every other status to.</summary>
    private static string Instant(TimeStatus status, BigInteger epochNanoseconds) =>
        status == TimeStatus.Completed ? BigIntValue(epochNanoseconds) : RangeError;

    /// <summary>
    /// The data home's zone tables, read from disk, each held to the length and SHA-256 the manifest beside them
    /// records, and decoded as an engine decodes the tables it embeds.
    /// </summary>
    private static TimeZoneDatabase LoadTimeZoneTables()
    {
        var generated = TimeZoneTablesDirectory(AppContext.BaseDirectory);
        string? release = null;
        var recorded = new Dictionary<string, (long Length, string Sha256)>(StringComparer.Ordinal);
        foreach (var line in File.ReadAllLines(Path.Combine(generated, "tables.manifest")))
        {
            var fields = line.Split(' ');
            if (fields.Length == 2 && fields[0] == "tzdb-release")
                release = fields[1];
            else if (fields.Length == 4 && fields[0] == "table")
                recorded[fields[1]] = (long.Parse(fields[2], NumberStyles.None, NumberFormatInfo.InvariantInfo), fields[3]);
        }

        byte[] Table(string name)
        {
            var bytes = File.ReadAllBytes(Path.Combine(generated, name));
            if (!recorded.TryGetValue(name, out var expected) || expected.Length != bytes.Length
                || expected.Sha256 != Convert.ToHexStringLower(SHA256.HashData(bytes)))
                throw new InvalidDataException(name + " is not the table its manifest records");

            return bytes;
        }

        return new TimeZoneDatabase(
            release ?? throw new InvalidDataException("the tables' manifest names no tzdb release"),
            Table("time-zones.bin"), Table("time-zone-ids.txt"), Table("time-zone-regions.txt"));
    }

    /// <summary>The data home's generated area, found from <paramref name="start"/> upwards.</summary>
    private static string TimeZoneTablesDirectory(string start)
    {
        for (var directory = new DirectoryInfo(start); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "Broiler.ECMAScript", "data", "time", "generated");
            if (File.Exists(Path.Combine(candidate, "tables.manifest")))
                return candidate;
        }

        throw new DirectoryNotFoundException("no Broiler.ECMAScript/data/time/generated/tables.manifest above " + start);
    }
}
