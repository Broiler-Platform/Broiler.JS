using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.Numerics;
using Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.Text;
using Broiler.ECMAScript.Time;

namespace Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.Date;

/// <summary>
/// One input of an operation of bundle sjl-6-001's reference-arm comparison: the population line it came
/// from, decoded, and the parameters the operation runs it at.
/// </summary>
internal sealed record DateInput
{
    /// <summary>The population class the line belongs to.</summary>
    public required string Class { get; init; }

    /// <summary>The line's number in its class, from 0.</summary>
    public required long Line { get; init; }

    /// <summary>The line as the population writes it.</summary>
    public required string Raw { get; init; }

    /// <summary>A Number: a time value, a local time, a year as a Number, or an offset.</summary>
    public double Number { get; init; }

    /// <summary>E2's seven components (year, month, date, hour, minute, second, millisecond), or E2M's day and time.</summary>
    public double[] Numbers { get; init; } = [];

    /// <summary>A string: a string to parse, or a zone's name.</summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>A civil year, or an epoch day count.</summary>
    public long Year { get; init; }

    /// <summary>A civil month (for E3B, as a long in <see cref="Long"/>).</summary>
    public int Month { get; init; }

    /// <summary>A civil day.</summary>
    public int Day { get; init; }

    /// <summary>A long second argument: BalanceISOYearMonth's month.</summary>
    public long Long { get; init; }

    /// <summary>The population's flag: E2's "MakeDay alone runs", E3's "valid".</summary>
    public bool Flag { get; init; }

    /// <summary>E4's zone.</summary>
    public string Zone { get; init; } = string.Empty;

    /// <summary>E4's reading: 'u' for a UTC instant (LocalTime), 'l' for a local time (UTC).</summary>
    public char Kind { get; init; }

    /// <summary>E6's offset in milliseconds.</summary>
    public double Offset { get; init; }

    /// <summary>E6's name, or null for none.</summary>
    public string? Name { get; init; }

    /// <summary>For a rendering read back: the engine that rendered it, "J" or "V".</summary>
    public string Source { get; init; } = string.Empty;

    /// <summary>What the listing shows for the parameters and any derived input.</summary>
    public string Detail { get; init; } = "-";
}

/// <summary>
/// The population scripts/conformance/generate_date_population.py writes: one gzip file per class, one
/// input per line, a Number as the 16 hexadecimal digits of its bit pattern and a string with every
/// character outside '!' to '~' (and the backslash) as \uXXXX.
/// </summary>
internal sealed class DatePopulation(string directory)
{
    public string Directory { get; } = directory;

    /// <summary>A class's lines, in order.</summary>
    public IEnumerable<string> ReadLines(string name)
    {
        using var file = File.OpenRead(Path.Combine(Directory, name + ".txt.gz"));
        using var gzip = new GZipStream(file, CompressionMode.Decompress);
        using var reader = new StreamReader(gzip, new UTF8Encoding(false, true));
        while (reader.ReadLine() is { } line)
            yield return line;
    }

    /// <summary>A class's inputs, decoded, in chunks of <paramref name="size"/> lines.</summary>
    public IEnumerable<List<DateInput>> Chunks(string name, int size)
    {
        var chunk = new List<DateInput>(size);
        long number = 0;
        foreach (var line in ReadLines(name))
        {
            chunk.Add(Parse(name, number++, line));
            if (chunk.Count == size)
            {
                yield return chunk;
                chunk = new List<DateInput>(size);
            }
        }

        if (chunk.Count > 0)
            yield return chunk;
    }

    /// <summary>Decodes one line of a class.</summary>
    public static DateInput Parse(string name, long number, string line)
    {
        var input = new DateInput { Class = name, Line = number, Raw = line };
        var fields = line.Split(' ');
        switch (name)
        {
            case "E1T":
            case "E6O":
                return input with { Number = NumericsPopulation.Double(line) };
            case "E2C":
                return input with { Numbers = fields[..7].Select(NumericsPopulation.Double).ToArray(), Flag = fields[7] == "d" };
            case "E2M":
                return input with { Numbers = [NumericsPopulation.Double(fields[0]), NumericsPopulation.Double(fields[1])] };
            case "E3Y":
                return input with { Year = long.Parse(line, CultureInfo.InvariantCulture) };
            case "E3D":
                return input with
                {
                    Year = long.Parse(fields[0], CultureInfo.InvariantCulture),
                    Month = int.Parse(fields[1], CultureInfo.InvariantCulture),
                    Day = int.Parse(fields[2], CultureInfo.InvariantCulture),
                    Flag = fields[3] == "v",
                };
            case "E3E":
                return input with { Year = long.Parse(line, CultureInfo.InvariantCulture) };
            case "E3B":
                return input with { Year = long.Parse(fields[0], CultureInfo.InvariantCulture), Long = long.Parse(fields[1], CultureInfo.InvariantCulture) };
            case "E4L":
                return input with { Zone = fields[0], Kind = fields[1][0], Number = NumericsPopulation.Double(fields[2]) };
            case "E5F":
            case "E5V":
            case "E5G":
                return input with { Text = NumericsPopulation.Unescape(line) };
            case "E6N":
            {
                var text = NumericsPopulation.Unescape(line);
                return input with { Name = text.Length == 0 ? null : text };
            }

            default:
                throw new ArgumentException("No such class: " + name);
        }
    }
}

/// <summary>How the Date comparison writes results: a Number by its bit pattern, a string by its code units, a refusal by its kind.</summary>
internal static class DateRender
{
    /// <summary>A Number by its bit pattern, every NaN as one; -0 and +0 differ.</summary>
    public static string Num(double value) => Render.F64(value);

    /// <summary>An integer an arm answers in a long, as the Number it stands for.</summary>
    public static string Num(long value) => Render.F64(value);

    public static string Str(string text) => TextRender.Str(text);

    public static string Bool(bool value) => TextRender.Bool(value);

    public static string Error(string kind) => TextRender.Error(kind);

    /// <summary>A civil date by its fields.</summary>
    public static string Civil(long year, long month, long day) =>
        "date:" + year.ToString(CultureInfo.InvariantCulture) + "-" + month.ToString(CultureInfo.InvariantCulture) + "-" + day.ToString(CultureInfo.InvariantCulture);

    /// <summary>A pair of integers: a week and its year, a year and a month.</summary>
    public static string Pair(long first, long second) =>
        "pair:" + first.ToString(CultureInfo.InvariantCulture) + "," + second.ToString(CultureInfo.InvariantCulture);
}

/// <summary>
/// S's test provider of bundle sjl-6-001's rule (its E4): the host's <see cref="TimeZoneInfo"/>, read at each
/// instant, as Broiler.JS's provider will read it. An instant past the last of .NET's range is read 400
/// years earlier as often as needed, the zone's rules repeating with the Gregorian calendar there; an instant
/// before year 1's third day is read at that day's first instant, the zone's earliest offset. The reference
/// (dateref.zone_offset_ms) reads zoneinfo the same way.
/// </summary>
internal sealed class HostZoneProvider(TimeZoneInfo zone) : ILocalTimeProvider
{
    public const double FourCenturies = 146097d * 86400000d;

    public const double First = -62135596800000d + (2 * 86400000d);

    public const double Last = 253402300799999d;

    public TimeZoneInfo Zone { get; } = zone;

    public double GetOffsetMilliseconds(double epochMilliseconds)
    {
        var offset = Zone.GetUtcOffset(Instant(epochMilliseconds));
        return Math.Truncate(offset.TotalMilliseconds);
    }

    /// <summary>The zone's name at the instant, as Broiler.JS names it: its daylight name in daylight-saving time, its standard name otherwise.</summary>
    public string? GetName(double epochMilliseconds)
    {
        var instant = Instant(epochMilliseconds);
        return Zone.IsDaylightSavingTime(instant) ? Zone.DaylightName : Zone.StandardName;
    }

    private static DateTimeOffset Instant(double epochMilliseconds)
    {
        var at = Math.Floor(epochMilliseconds);
        while (at > Last)
            at -= FourCenturies;
        if (at < First)
            at = First;

        return DateTimeOffset.FromUnixTimeMilliseconds((long)at);
    }

    /// <summary>The offsets UTC(t) reads, for the listing: a day before and after t, and at t less each.</summary>
    public string Offsets(char kind, double t)
    {
        static string N(double v) => v.ToString("R", CultureInfo.InvariantCulture);
        if (kind == 'u')
            return "zone=" + Zone.Id + " o=" + N(GetOffsetMilliseconds(t));

        var before = GetOffsetMilliseconds(t - 86400000d);
        var after = GetOffsetMilliseconds(t + 86400000d);
        return "zone=" + Zone.Id + " b=" + N(before) + " a=" + N(after) + " pb=" + N(GetOffsetMilliseconds(t - before)) + " pa=" + N(GetOffsetMilliseconds(t - after));
    }
}
