using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace Broiler.ECMAScript.Algorithms.Tests.ReferenceArms;

/// <summary>
/// The population scripts/conformance/generate_reference_population.py writes: one gzip file per
/// class, one input per line, a double as 16 hexadecimal digits of its bit pattern and a string with
/// <c>\uXXXX</c> escapes.
/// </summary>
internal sealed class Population(string directory)
{
    public string Directory { get; } = directory;

    /// <summary>The lines of a class, decoded: <c>D6:16384</c> is D6's first 2^14 lines, and <c>P4:single</c> P4's one-character lines.</summary>
    public IReadOnlyList<string> Lines(string spec)
    {
        var parts = spec.Split(':');
        var lines = ReadAll(parts[0]);
        if (parts.Length == 1)
            return lines;

        return parts[1] == "single"
            ? lines.Where(static line => line.Length == 1).ToList()
            : lines.Take(int.Parse(parts[1], CultureInfo.InvariantCulture)).ToList();
    }

    private List<string> ReadAll(string name)
    {
        using var file = File.OpenRead(Path.Combine(Directory, name + ".txt.gz"));
        using var gzip = new GZipStream(file, CompressionMode.Decompress);
        using var reader = new StreamReader(gzip, new UTF8Encoding(false, true));
        var lines = new List<string>();
        while (reader.ReadLine() is { } line)
            lines.Add(Unescape(line));
        return lines;
    }

    public static double Double(string line) =>
        BitConverter.UInt64BitsToDouble(ulong.Parse(line, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture));

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

            if (line[at] == '\\')
                throw new FormatException("A population line holds a backslash that is not an escape: " + line);

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
