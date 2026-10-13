using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.Numerics;

/// <summary>
/// The population scripts/conformance/generate_numerics_population.py writes: one gzip file per class,
/// one input per line. A class is read a chunk at a time, since R1I alone is millions of lines; B1V and
/// B1C are held whole, because B1P, B1S and B1N name B1V's lines and every value is compared with
/// every double of B1C.
/// </summary>
internal sealed class NumericsPopulation(string directory)
{
    private BigInteger[]? values;
    private double[]? compared;

    public string Directory { get; } = directory;

    /// <summary>B1V, every BigInt value, by line.</summary>
    public BigInteger[] Values => values ??= ReadLines("B1V").Select(static line => Hex.Parse(line)).ToArray();

    /// <summary>B1C, the doubles every value is compared with.</summary>
    public double[] Compared => compared ??= ReadLines("B1C").Select(Double).ToArray();

    /// <summary>A class's lines, in order, with their numbers.</summary>
    public IEnumerable<string> ReadLines(string name)
    {
        using var file = File.OpenRead(Path.Combine(Directory, name + ".txt.gz"));
        using var gzip = new GZipStream(file, CompressionMode.Decompress);
        using var reader = new StreamReader(gzip, new UTF8Encoding(false, true));
        while (reader.ReadLine() is { } line)
            yield return line;
    }

    /// <summary>A class's inputs, decoded, in chunks of <paramref name="size"/> lines.</summary>
    public IEnumerable<List<NumericsInput>> Chunks(string name, int size)
    {
        var chunk = new List<NumericsInput>(size);
        long number = 0;
        foreach (var line in ReadLines(name))
        {
            chunk.Add(Parse(name, number++, line));
            if (chunk.Count == size)
            {
                yield return chunk;
                chunk = new List<NumericsInput>(size);
            }
        }

        if (chunk.Count > 0)
            yield return chunk;
    }

    /// <summary>Decodes one line of a class.</summary>
    public NumericsInput Parse(string name, long number, string line)
    {
        var input = new NumericsInput { Class = name, Line = number, Raw = line };
        switch (name)
        {
            case "N1" or "N2E" or "N2N" or "N3E" or "N3C" or "B1C":
                return input with { A = Double(line) };
            case "N2D":
                return input with { Half = ushort.Parse(line, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture) };
            case "N3P":
                var pair = line.Split(',');
                return input with { A = Double(pair[0]), B = Double(pair[1]) };
            case "N3H" or "N4":
                return input with { Values = line == "-" ? [] : line.Split(',').Select(Double).ToArray() };
            case "R1M":
                return input with { Text = Unescape(line) };
            case "R1I":
            {
                var f = Hex.Fields(line);
                return input with { X = f[0], Y = f[1], Z = f[2] };
            }
            case "R1W":
            {
                var f = Hex.Fields(line);
                return input with { X = f[0], Y = f[1], Z = f[2], W = f[3] };
            }
            case "R1D":
            {
                var f = Hex.Fields(line);
                return input with { X = f[0], Y = f[1] };
            }
            case "B1V" or "B1E":
                return input with { X = name == "B1V" ? Values[number] : Hex.Parse(line) };
            case "B1P":
            {
                var p = line.Split(' ');
                return input with { X = Values[int.Parse(p[0], CultureInfo.InvariantCulture)], Y = Values[int.Parse(p[1], CultureInfo.InvariantCulture)] };
            }
            case "B1S":
            {
                var p = line.Split(' ');
                return input with { X = Values[int.Parse(p[0], CultureInfo.InvariantCulture)], Y = Hex.Parse(p[1]) };
            }
            case "B1N":
            {
                var p = line.Split(' ');
                return input with { X = Values[int.Parse(p[0], CultureInfo.InvariantCulture)], Z = Hex.Parse(p[1]) };
            }
            default:
                throw new ArgumentException("No population class " + name, nameof(name));
        }
    }

    public static double Double(string text) =>
        BitConverter.UInt64BitsToDouble(ulong.Parse(text, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture));

    public static string Unescape(string line)
    {
        if (!line.Contains('\\', StringComparison.Ordinal))
            return line;

        var text = new StringBuilder(line.Length);
        for (var at = 0; at < line.Length; at++)
        {
            if (line[at] == '\\' && at + 5 < line.Length && line[at + 1] == 'u')
            {
                text.Append((char)int.Parse(line.AsSpan(at + 2, 4), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture));
                at += 5;
                continue;
            }

            text.Append(line[at]);
        }

        return text.ToString();
    }

    /// <summary>The generator's own escaping, for writing inputs and results the same way.</summary>
    public static string Escape(string text)
    {
        var escaped = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            if (c is >= '!' and <= '~' && c != '\\')
                escaped.Append(c);
            else
                escaped.Append("\\u").Append(((int)c).ToString("X4", CultureInfo.InvariantCulture));
        }

        return escaped.ToString();
    }
}
