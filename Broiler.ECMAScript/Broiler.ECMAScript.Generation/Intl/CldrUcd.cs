// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   24
// Annotated:        24/24
// Exempt:           5
// Human-reviewed:   0/24
// IP risk:          Low
// Security risk:    High
// Criteria:         2/2
// Resource impact:  6/10 max
// Unverified:       24
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using Broiler.ECMAScript.Generation.Time;

namespace Broiler.ECMAScript.Generation.Intl;

/// <summary>An inclusive range of code points.</summary>
/// <param name="First">The first code point of the range.</param>
/// <param name="Last">The last code point of the range.</param>
// Broiler-AI:           Origin=Ported; Spec=JSD-0082 D12 UCD subset via Broiler.VM UnicodeDatabase; IP=Low; Security=Low; Resources=0; Fingerprint=A74CB7
// Broiler-Human:        PENDING
internal readonly record struct CodePointRange(int First, int Last);

/// <summary>
/// The part of the Unicode Character Database the CLDR table generator reads: each code point's
/// General_Category and canonical combining class and its full canonical decomposition, from
/// <c>UnicodeData.txt</c>, and the Unified_Ideograph ranges, from <c>PropList.txt</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b>PORTED, in part, from Broiler.VM's <c>UnicodeDatabase</c></b> at Broiler.VM <c>4cf9ca44</c> (decision
/// JSD-0082, decision D12): its <c>UnicodeData.txt</c> reader with each <c>&lt;..., First&gt;</c> and
/// <c>&lt;..., Last&gt;</c> pair joined into one range, its combining classes, its full canonical
/// decompositions (Hangul excluded, as there), its range normalization, and its Hangul syllable
/// decomposition, unchanged. Of everything else that class reads - the other property files, the case
/// mappings, the scripts, the properties of strings - the generator reads nothing, so nothing else is ported.
/// </para>
/// <para>
/// <b>The checks are the ones two files allow.</b> Broiler.VM's reader compared General_Category with
/// <c>DerivedGeneralCategory.txt</c> and the decompositions with <c>DerivedNormalizationProps.txt</c>, files
/// this reader is not given. It checks instead that every record has its fifteen fields and a known
/// category, that a range carries no decomposition, that every code point a decomposition names is assigned,
/// and that every Unified_Ideograph code point is an assigned letter, so that a misread line in either file
/// fails rather than writes a quietly different table. The caller verifies both files' bytes against a pin
/// first: the home's <see cref="IntlUcdPin"/>, or Broiler.VM's own Unicode pin.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=Ported; Spec=JSD-0082 D12 UCD subset via Broiler.VM UnicodeDatabase; IP=Low; Security=Medium; Resources=6; Fingerprint=6B703E
// Broiler-Human:        PENDING
public sealed class CldrUcd
{
    /// <summary>The name <c>UnicodeData.txt</c> is read under, as the pins write it.</summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D12 UCD subset; IP=Low; Security=Low; Resources=0; Fingerprint=B8D9BE
    // Broiler-Human:        PENDING
    public const string UnicodeDataFile = "ucd-17.0.0/UnicodeData.txt";

    /// <summary>The name <c>PropList.txt</c> is read under, as the pins write it.</summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D12 UCD subset; IP=Low; Security=Low; Resources=0; Fingerprint=033B8C
    // Broiler-Human:        PENDING
    public const string PropListFile = "ucd-17.0.0/PropList.txt";

    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D12 UCD subset via Broiler.VM UnicodeDatabase; IP=Low; Security=Low; Resources=0; Fingerprint=EB11D4
    // Broiler-Human:        PENDING
    private const int MaxCodePoint = 0x10FFFF;

    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D12 UCD subset via Broiler.VM UnicodeDatabase; IP=Low; Security=Low; Resources=0; Fingerprint=1AC5B6
    // Broiler-Human:        PENDING
    private const int HangulSBase = 0xAC00;
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D12 UCD subset via Broiler.VM UnicodeDatabase; IP=Low; Security=Low; Resources=0; Fingerprint=367480
    // Broiler-Human:        PENDING
    private const int HangulLBase = 0x1100;
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D12 UCD subset via Broiler.VM UnicodeDatabase; IP=Low; Security=Low; Resources=0; Fingerprint=D57D26
    // Broiler-Human:        PENDING
    private const int HangulVBase = 0x1161;
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D12 UCD subset via Broiler.VM UnicodeDatabase; IP=Low; Security=Low; Resources=0; Fingerprint=D8C7B3
    // Broiler-Human:        PENDING
    private const int HangulTBase = 0x11A7;
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D12 UCD subset via Broiler.VM UnicodeDatabase; IP=Low; Security=Low; Resources=0; Fingerprint=34EF51
    // Broiler-Human:        PENDING
    private const int HangulVCount = 21;
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D12 UCD subset via Broiler.VM UnicodeDatabase; IP=Low; Security=Low; Resources=0; Fingerprint=47F774
    // Broiler-Human:        PENDING
    private const int HangulTCount = 28;
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D12 UCD subset via Broiler.VM UnicodeDatabase; IP=Low; Security=Low; Resources=0; Fingerprint=BE574F
    // Broiler-Human:        PENDING
    private const int HangulNCount = HangulVCount * HangulTCount;
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D12 UCD subset via Broiler.VM UnicodeDatabase; IP=Low; Security=Low; Resources=0; Fingerprint=C230BE
    // Broiler-Human:        PENDING
    private const int HangulSCount = 19 * HangulNCount;

    /// <summary>The thirty General_Category values, unassigned first: a code point's category is its index here.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D12 UCD subset via Broiler.VM UnicodeDatabase; IP=Low; Security=Low; Resources=0; Fingerprint=4547CC
    // Broiler-Human:        PENDING
    private static ImmutableArray<string> Categories { get; } = ImmutableArray.Create(
        "Cn", "Lu", "Ll", "Lt", "Lm", "Lo", "Mn", "Mc", "Me", "Nd", "Nl", "No", "Pc", "Pd", "Ps", "Pe", "Pi", "Pf", "Po",
        "Sm", "Sc", "Sk", "So", "Zs", "Zl", "Zp", "Cc", "Cf", "Cs", "Co");

    private readonly byte[] category;

    private readonly byte[] combiningClass;

    private CldrUcd(byte[] category, byte[] combiningClass, FrozenDictionary<int, int[]> canonicalDecomposition, ImmutableArray<CodePointRange> unifiedIdeographs)
    {
        this.category = category;
        this.combiningClass = combiningClass;
        CanonicalDecomposition = canonicalDecomposition;
        UnifiedIdeographs = unifiedIdeographs;
    }

    /// <summary>The Unicode version the files are read as.</summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D12 UCD subset; IP=Low; Security=Low; Resources=0; Fingerprint=3C6364
    // Broiler-Human:        PENDING
    public static string Version => IntlDataVersions.UnicodeVersion;

    /// <summary>Each code point's full canonical decomposition, where it has one; Hangul syllables excluded.</summary>
    internal FrozenDictionary<int, int[]> CanonicalDecomposition { get; }

    /// <summary>The Unified_Ideograph ranges of <c>PropList.txt</c>, sorted and merged: ascending, disjoint and never adjacent.</summary>
    internal ImmutableArray<CodePointRange> UnifiedIdeographs { get; }

    /// <summary>
    /// Reads the two files from <paramref name="files"/>, by <see cref="UnicodeDataFile"/> and
    /// <see cref="PropListFile"/>, whose bytes the caller has verified against a pin.
    /// </summary>
    /// <param name="files">The files' bytes, by the names the pins write them under; other files are not read.</param>
    /// <exception cref="InvalidDataException">A file is missing, or a check above fails.</exception>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D12 UCD subset via Broiler.VM UnicodeDatabase; IP=Low; Security=High; Resources=6; Fingerprint=E61D3D
    // Broiler-Falsified-If: a UnicodeData.txt record with a field missing, an unknown category, or a decomposition on a range, or a Unified_Ideograph code point that is not an assigned letter, is read without an exception
    // Broiler-Human:        PENDING
    public static CldrUcd Read(IReadOnlyDictionary<string, byte[]> files)
    {
        ArgumentNullException.ThrowIfNull(files);

        if (!files.TryGetValue(UnicodeDataFile, out var unicodeDataBytes) || !files.TryGetValue(PropListFile, out var propListBytes))
        {
            throw new InvalidDataException("the UCD files hand neither " + UnicodeDataFile + " nor " + PropListFile + ", or only one of them");
        }

        var unicodeData = ArchiveReading.Decode(unicodeDataBytes);
        var category = new byte[MaxCodePoint + 1];
        var combiningClass = new byte[MaxCodePoint + 1];
        var canonical = new Dictionary<int, int[]>();

        foreach (var (first, last, fields) in UnicodeDataEntries(unicodeData))
        {
            var index = Categories.IndexOf(fields[2]);

            if (index <= 0)
            {
                throw new InvalidDataException("UnicodeData.txt gives " + fields[0] + " the category `" + fields[2] + "`, which is not an assigned General_Category");
            }

            var combining = int.Parse(fields[3], NumberStyles.Integer, NumberFormatInfo.InvariantInfo);

            if (combining is < 0 or > 254)
            {
                throw new InvalidDataException("UnicodeData.txt gives " + fields[0] + " the combining class " + fields[3]);
            }

            for (var codePoint = first; codePoint <= last; codePoint++)
            {
                category[codePoint] = (byte)index;
                combiningClass[codePoint] = (byte)combining;
            }

            if (fields[5].Length == 0)
            {
                continue;
            }

            if (first != last)
            {
                throw new InvalidDataException("the UnicodeData.txt range at " + fields[0] + " carries a decomposition");
            }

            var parts = fields[5].Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts[0][0] != '<')
            {
                canonical[first] = parts.Select(Hex).ToArray();
            }
        }

        int[] Full(int codePoint)
        {
            if (codePoint >= HangulSBase && codePoint < HangulSBase + HangulSCount)
            {
                return Hangul(codePoint);
            }

            return canonical.TryGetValue(codePoint, out var mapping)
                ? mapping.SelectMany(Full).ToArray()
                : [codePoint];
        }

        var decomposition = new Dictionary<int, int[]>();

        foreach (var codePoint in canonical.Keys.Order())
        {
            var full = Full(codePoint);

            if (full.Any(part => category[part] == 0))
            {
                throw new InvalidDataException("UnicodeData.txt decomposes " + codePoint.ToString("X4", NumberFormatInfo.InvariantInfo) + " into an unassigned code point");
            }

            decomposition[codePoint] = full;
        }

        var unified = new List<CodePointRange>();

        foreach (var fields in Records(ArchiveReading.Decode(propListBytes)))
        {
            if (fields.Length == 2 && string.Equals(fields[1], "Unified_Ideograph", StringComparison.Ordinal))
            {
                unified.Add(Range(fields[0]));
            }
        }

        var unifiedIdeographs = Normalize(unified);
        var letter = Categories.IndexOf("Lo");

        if (unifiedIdeographs.Length == 0 ||
            unifiedIdeographs.Any(range => Enumerable.Range(range.First, range.Last - range.First + 1).Any(codePoint => category[codePoint] != letter)))
        {
            throw new InvalidDataException("PropList.txt's Unified_Ideograph is empty or names a code point UnicodeData.txt does not assign as a letter (Lo)");
        }

        return new CldrUcd(category, combiningClass, decomposition.ToFrozenDictionary(), unifiedIdeographs);
    }

    /// <summary>The canonical combining class of <paramref name="codePoint"/>; 0 for a code point UnicodeData.txt does not list.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D12 UCD subset via Broiler.VM UnicodeDatabase; IP=Low; Security=Medium; Resources=0; Fingerprint=E47A71
    // Broiler-Human:        PENDING
    internal int CombiningClass(int codePoint) => combiningClass[codePoint];

    /// <summary>
    /// Whether <paramref name="codePoint"/>'s General_Category is a symbol or a separator (<c>Sm</c>, <c>Sc</c>,
    /// <c>Sk</c>, <c>So</c>, <c>Zs</c>, <c>Zl</c> or <c>Zp</c>), the set CLDR's currency spacing reads as
    /// <c>[[:S:][:Z:]]</c>.
    /// </summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D12 UCD subset via Broiler.VM CldrTableGenerator; IP=Low; Security=Medium; Resources=0; Fingerprint=48AC21
    // Broiler-Human:        PENDING
    internal bool IsSymbolOrSeparator(int codePoint) => Categories[category[codePoint]][0] is 'S' or 'Z';

    /// <summary>The full Hangul syllable decomposition, LV or LVT.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D12 UCD subset via Broiler.VM UnicodeDatabase; IP=Low; Security=Low; Resources=1; Fingerprint=3F88E4
    // Broiler-Human:        PENDING
    internal static int[] Hangul(int syllable)
    {
        var index = syllable - HangulSBase;
        var leading = HangulLBase + (index / HangulNCount);
        var vowel = HangulVBase + (index % HangulNCount / HangulTCount);
        var trailing = HangulTBase + (index % HangulTCount);

        return trailing == HangulTBase ? [leading, vowel] : [leading, vowel, trailing];
    }

    /// <summary><c>UnicodeData.txt</c>'s records, with each <c>&lt;..., First&gt;</c>/<c>&lt;..., Last&gt;</c> pair joined into one range.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D12 UCD subset via Broiler.VM UnicodeDatabase; IP=Low; Security=High; Resources=3; Fingerprint=AA1255
    // Broiler-Falsified-If: a line of a field count other than fifteen, or a Last record no First record opened, is read without an exception
    // Broiler-Human:        PENDING
    private static IEnumerable<(int First, int Last, string[] Fields)> UnicodeDataEntries(string text)
    {
        string[]? opened = null;

        foreach (var raw in text.Split('\n'))
        {
            if (raw.Length == 0)
            {
                continue;
            }

            var fields = raw.Split(';');

            if (fields.Length != 15)
            {
                throw new InvalidDataException(string.Create(NumberFormatInfo.InvariantInfo, $"a UnicodeData.txt line has {fields.Length} fields, not 15: {raw}"));
            }

            if (fields[1].EndsWith(", First>", StringComparison.Ordinal))
            {
                opened = fields;
                continue;
            }

            if (fields[1].EndsWith(", Last>", StringComparison.Ordinal))
            {
                if (opened is null)
                {
                    throw new InvalidDataException("UnicodeData.txt closes a range it did not open: " + raw);
                }

                yield return (Hex(opened[0]), Hex(fields[0]), opened);
                opened = null;
                continue;
            }

            yield return (Hex(fields[0]), Hex(fields[0]), fields);
        }
    }

    /// <summary>Every data line of a UCD file as trimmed fields, with the <c>#</c> comment removed and blank lines skipped.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D12 UCD subset via Broiler.VM UnicodeDataFile; IP=Low; Security=Medium; Resources=2; Fingerprint=4C79F2
    // Broiler-Human:        PENDING
    private static IEnumerable<string[]> Records(string text)
    {
        foreach (var raw in text.Split('\n'))
        {
            var hash = raw.IndexOf('#', StringComparison.Ordinal);
            var line = (hash >= 0 ? raw[..hash] : raw).Trim();

            if (line.Length > 0)
            {
                yield return line.Split(';').Select(static field => field.Trim()).ToArray();
            }
        }
    }

    /// <summary><c>0041</c> or <c>0041..005A</c>.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D12 UCD subset via Broiler.VM UnicodeDataFile; IP=Low; Security=Medium; Resources=1; Fingerprint=58EE94
    // Broiler-Human:        PENDING
    private static CodePointRange Range(string field)
    {
        var dots = field.IndexOf("..", StringComparison.Ordinal);

        return dots < 0
            ? new CodePointRange(Hex(field), Hex(field))
            : new CodePointRange(Hex(field[..dots]), Hex(field[(dots + 2)..]));
    }

    /// <summary>A code point in hexadecimal, read through the invariant format.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D12 UCD subset via Broiler.VM UnicodeDataFile; IP=Low; Security=Medium; Resources=1; Fingerprint=AADBC6
    // Broiler-Human:        PENDING
    private static int Hex(string text)
    {
        var value = int.Parse(text, NumberStyles.AllowHexSpecifier, NumberFormatInfo.InvariantInfo);

        if (value is < 0 or > MaxCodePoint)
        {
            throw new InvalidDataException(text + " is not a code point");
        }

        return value;
    }

    /// <summary>Sorted, merged: ascending, disjoint and never adjacent.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D12 UCD subset via Broiler.VM UnicodeDataFile; IP=Low; Security=Medium; Resources=2; Fingerprint=5FCA06
    // Broiler-Human:        PENDING
    private static ImmutableArray<CodePointRange> Normalize(IEnumerable<CodePointRange> ranges)
    {
        var merged = new List<CodePointRange>();

        foreach (var range in ranges.OrderBy(static range => range.First))
        {
            if (merged.Count > 0 && range.First <= merged[^1].Last + 1)
            {
                merged[^1] = merged[^1] with { Last = Math.Max(merged[^1].Last, range.Last) };
            }
            else
            {
                merged.Add(range);
            }
        }

        return [.. merged];
    }
}
