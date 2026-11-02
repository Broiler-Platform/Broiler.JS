// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   40
// Annotated:        40/40
// Exempt:           5
// Human-reviewed:   0/40
// IP risk:          Low
// Security risk:    High
// Criteria:         3/3
// Resource impact:  7/10 max
// Unverified:       40
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Broiler.ECMAScript.Generation.Intl;

/// <summary>
/// The collation tables: the root collation, <c>allkeys_CLDR.txt</c> as written with its single-element mappings
/// folded into runs, and the tailorings, built from their CLDR rules into weights scaled by 256 so that a
/// tailored weight has room between two root weights without moving either. Their layouts are the ones
/// Broiler.VM's <c>JsCollationData</c> reads; collation itself is milestone SJL-17's (decision SJL-D-24 (c)),
/// and the generator moves whole, collation included (decision D11).
/// </summary>
// Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 collation tables via Broiler.VM CldrTableGenerator; IP=Low; Security=Medium; Resources=7; Fingerprint=AD2E42
// Broiler-Human:        PENDING
public static partial class CldrTableGenerator
{
    /// <summary>
    /// The rules of one collation type of an LDML collation file, with comments and markup removed: the CDATA
    /// after the first <c>&lt;collation type='type'&gt;</c> element. Broiler.VM's generator found the element
    /// with the expression <c>&lt;collation type=['"]type['"][^&gt;]*&gt;</c>, which this reads by hand (rule
    /// SL4): the same opening, either quote before and after the type, and the next <c>&gt;</c>.
    /// </summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 collation rules via Broiler.VM CldrTableGenerator; IP=Low; Security=Medium; Resources=2; Fingerprint=4559E3
    // Broiler-Human:        PENDING
    internal static string CollationRules(string xml, string type)
    {
        const string Opening = "<collation type=";

        for (var at = xml.IndexOf(Opening, StringComparison.Ordinal); at >= 0; at = xml.IndexOf(Opening, at + 1, StringComparison.Ordinal))
        {
            var value = at + Opening.Length;
            var close = value + 1 + type.Length;

            if (close < xml.Length &&
                xml[value] is '\'' or '"' &&
                string.CompareOrdinal(xml, value + 1, type, 0, type.Length) == 0 &&
                xml[close] is '\'' or '"' &&
                xml.IndexOf('>', close + 1) >= 0)
            {
                var start = xml.IndexOf("<![CDATA[", at, StringComparison.Ordinal) + "<![CDATA[".Length;
                var end = xml.IndexOf("]]></cr>", start, StringComparison.Ordinal);
                return xml[start..end];
            }
        }

        throw new InvalidDataException("no collation of type " + type);
    }

    /// <summary>A code point sequence in NFD, decomposed with the UCD subset and ordered canonically.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 NFD via Broiler.VM CldrTableGenerator; IP=Low; Security=Medium; Resources=3; Fingerprint=2E32AA
    // Broiler-Human:        PENDING
    internal static int[] Nfd(CldrUcd database, IEnumerable<int> codePoints)
    {
        var result = new List<int>();

        void Decompose(int codePoint)
        {
            if (codePoint is >= 0xAC00 and <= 0xD7A3)
            {
                result.AddRange(CldrUcd.Hangul(codePoint));
                return;
            }

            if (database.CanonicalDecomposition.TryGetValue(codePoint, out var parts))
            {
                foreach (var part in parts)
                {
                    Decompose(part);
                }

                return;
            }

            result.Add(codePoint);
        }

        foreach (var codePoint in codePoints)
        {
            Decompose(codePoint);
        }

        // CANONICAL ORDERING: a stable sort of each run of non-starters by combining class.
        for (var i = 1; i < result.Count; i++)
        {
            var at = i;

            while (at > 0 &&
                   database.CombiningClass(result[at]) != 0 &&
                   database.CombiningClass(result[at - 1]) > database.CombiningClass(result[at]))
            {
                (result[at - 1], result[at]) = (result[at], result[at - 1]);
                at--;
            }
        }

        return [.. result];
    }

    /// <summary>The code points of a string.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 collation via Broiler.VM CldrTableGenerator; IP=Low; Security=Low; Resources=1; Fingerprint=485E66
    // Broiler-Human:        PENDING
    internal static IEnumerable<int> CodePoints(string text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            var codePoint = char.ConvertToUtf32(text, i);

            if (codePoint > 0xFFFF)
            {
                i++;
            }

            yield return codePoint;
        }
    }

    /// <summary>
    /// Whether <paramref name="c"/> is white space as <c>char.IsWhiteSpace</c> answers, which Broiler.VM's
    /// generator called and the conventions forbid (rule SL4): the Unicode White_Space characters, written out.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=Unicode White_Space for Broiler.VM CldrTableGenerator; IP=Low; Security=Medium; Resources=0; Fingerprint=14559D
    // Broiler-Human:        PENDING
    internal static bool IsWhiteSpace(char c) =>
        (int)c is >= 0x09 and <= 0x0D or 0x20 or 0x85 or 0xA0 or 0x1680 or >= 0x2000 and <= 0x200A
            or 0x2028 or 0x2029 or 0x202F or 0x205F or 0x3000;

    /// <summary>One collation element in root weights: primary, secondary, tertiary and whether it is variable.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 collation via Broiler.VM CldrTableGenerator; IP=Low; Security=Low; Resources=0; Fingerprint=48D549
    // Broiler-Human:        PENDING
    internal readonly record struct Element(int Primary, int Secondary, int Tertiary, bool Variable);

    /// <summary>The root collation as <c>allkeys_CLDR.txt</c> states it.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 root collation via Broiler.VM CldrTableGenerator; IP=Low; Security=Medium; Resources=6; Fingerprint=04FC1C
    // Broiler-Human:        PENDING
    internal sealed class RootCollation
    {
        private RootCollation(Dictionary<string, Element[]> entries) => Entries = entries;

        /// <summary>Every mapping, keyed by its code points joined with spaces in hex.</summary>
        internal Dictionary<string, Element[]> Entries { get; }

        /// <summary>
        /// Reads <c>allkeys_CLDR.txt</c>. Broiler.VM's generator matched each element with the expression
        /// <c>\[([.*])([0-9A-F]{4})\.([0-9A-F]{4})\.([0-9A-F]{4})\]</c>; <see cref="Elements"/> reads the same
        /// matches by hand (rule SL4).
        /// </summary>
        // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 root collation via Broiler.VM CldrTableGenerator; IP=Low; Security=High; Resources=5; Fingerprint=33AB30
        // Broiler-Falsified-If: a line that maps nothing, repeats a key or has a weight wider than its encoding is read without an exception
        // Broiler-Human:        PENDING
        internal static RootCollation Read(string text)
        {
            var entries = new Dictionary<string, Element[]>(StringComparer.Ordinal);

            foreach (var raw in text.Split('\n'))
            {
                var line = raw.TrimEnd('\r');

                if (line.Length == 0 || line[0] is '#' or '@')
                {
                    continue;
                }

                var semicolon = line.IndexOf(';', StringComparison.Ordinal);
                var key = string.Join(' ', line[..semicolon].Split(' ', StringSplitOptions.RemoveEmptyEntries));
                var weights = line[semicolon..];
                var hash = weights.IndexOf('#', StringComparison.Ordinal);

                if (hash >= 0)
                {
                    weights = weights[..hash];
                }

                var ces = Elements(weights).ToArray();

                if (ces.Length == 0 || !entries.TryAdd(key, ces))
                {
                    throw new InvalidDataException("allkeys_CLDR.txt: `" + line + "` maps nothing or repeats a key");
                }

                if (ces.Any(static ce => ce.Secondary > 0x1FFF || ce.Tertiary > 0x1F))
                {
                    throw new InvalidDataException("allkeys_CLDR.txt: `" + line + "` has a weight wider than its encoding");
                }
            }

            return new RootCollation(entries);
        }

        /// <summary>
        /// Every collation element written <c>[.PPPP.SSSS.TTTT]</c> or <c>[*PPPP.SSSS.TTTT]</c> in
        /// <paramref name="weights"/>, left to right without overlap, as the expression's matches were: four
        /// upper-case hexadecimal digits a weight.
        /// </summary>
        // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 root collation via Broiler.VM CldrTableGenerator; IP=Low; Security=High; Resources=2; Fingerprint=D4DC2B
        // Broiler-Falsified-If: an element with a lower-case or missing hexadecimal digit, or another marker than . or *, is read, or one the expression matches is not
        // Broiler-Human:        PENDING
        internal static IEnumerable<Element> Elements(string weights)
        {
            static bool IsHex(string text, int at)
            {
                for (var i = at; i < at + 4; i++)
                {
                    if (text[i] is not (>= '0' and <= '9' or >= 'A' and <= 'F'))
                    {
                        return false;
                    }
                }

                return true;
            }

            static int Weight(string text, int at) =>
                int.Parse(text.AsSpan(at, 4), NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo);

            var at = 0;

            while (at + 17 <= weights.Length)
            {
                if (weights[at] == '[' && weights[at + 1] is '.' or '*' &&
                    IsHex(weights, at + 2) && weights[at + 6] == '.' &&
                    IsHex(weights, at + 7) && weights[at + 11] == '.' &&
                    IsHex(weights, at + 12) && weights[at + 16] == ']')
                {
                    yield return new Element(Weight(weights, at + 2), Weight(weights, at + 7), Weight(weights, at + 12), weights[at + 1] == '*');
                    at += 17;
                    continue;
                }

                at++;
            }
        }

        /// <summary>A key's code points.</summary>
        // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 root collation via Broiler.VM CldrTableGenerator; IP=Low; Security=Low; Resources=1; Fingerprint=47B8C8
        // Broiler-Human:        PENDING
        internal static int[] Key(string key) =>
            [.. key.Split(' ').Select(static part => int.Parse(part, NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo))];

        /// <summary>The key of some code points.</summary>
        // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 root collation via Broiler.VM CldrTableGenerator; IP=Low; Security=Low; Resources=1; Fingerprint=5C9ED9
        // Broiler-Human:        PENDING
        internal static string KeyOf(IEnumerable<int> codePoints) =>
            string.Join(' ', codePoints.Select(static codePoint => codePoint.ToString("X4", NumberFormatInfo.InvariantInfo)));

        /// <summary>UCA 17's implicit weights for a code point no key names.</summary>
        // Broiler-AI:           Origin=Ported; Spec=UCA 17 implicit weights via Broiler.VM CldrTableGenerator; IP=Low; Security=Medium; Resources=2; Fingerprint=5AA0B7
        // Broiler-Human:        PENDING
        internal static Element[] Implicit(int codePoint, CldrUcd database)
        {
            foreach (var (first, last, @base) in SiniformRanges)
            {
                if (codePoint >= first && codePoint <= last)
                {
                    var origin = SiniformRanges.Where(range => range.Base == @base).Min(static range => range.First);
                    return [new Element(@base, 0x20, 0x02, false), new Element((codePoint - origin) | 0x8000, 0, 0, false)];
                }
            }

            var unified = database.UnifiedIdeographs.Any(range => codePoint >= range.First && codePoint <= range.Last);
            var core = codePoint is (>= 0x4E00 and <= 0x9FFF) or (>= 0xF900 and <= 0xFAFF);
            var primary = unified && core ? 0xFB40 : unified ? 0xFB80 : 0xFBC0;

            return [new Element(primary + (codePoint >> 15), 0x20, 0x02, false), new Element((codePoint & 0x7FFF) | 0x8000, 0, 0, false)];
        }

        /// <summary>
        /// The binary table: runs of single elements whose primary rises with the code point, then every other
        /// mapping, then the siniform ranges, then the unified ideographs.
        /// </summary>
        // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 root collation via Broiler.VM CldrTableGenerator; IP=Low; Security=Medium; Resources=5; Fingerprint=AB8C1F
        // Broiler-Human:        PENDING
        internal byte[] Encode(CldrUcd database)
        {
            var singles = Entries
                .Where(static entry => !entry.Key.Contains(' ', StringComparison.Ordinal) && entry.Value.Length == 1)
                .Select(static entry => (CodePoint: Key(entry.Key)[0], Element: entry.Value[0]))
                .OrderBy(static entry => entry.CodePoint)
                .ToList();

            var runs = new List<(int First, int Length, Element Element)>();

            foreach (var (codePoint, element) in singles)
            {
                if (runs.Count > 0)
                {
                    var (first, length, start) = runs[^1];

                    if (codePoint == first + length && length < 0xFFFF &&
                        element == start with { Primary = start.Primary + length } &&
                        start.Primary + length <= 0xFFFF)
                    {
                        runs[^1] = (first, length + 1, start);
                        continue;
                    }
                }

                runs.Add((codePoint, 1, element));
            }

            var others = Entries
                .Where(static entry => entry.Key.Contains(' ', StringComparison.Ordinal) || entry.Value.Length != 1)
                .OrderBy(static entry => entry.Key, StringComparer.Ordinal)
                .ToList();

            var bytes = new List<byte>();
            UInt(bytes, runs.Count);

            foreach (var (first, length, element) in runs)
            {
                UInt24(bytes, first);
                UInt16(bytes, length);
                Weights(bytes, element);
            }

            UInt(bytes, others.Count);

            foreach (var (key, ces) in others)
            {
                var codePoints = Key(key);
                bytes.Add(checked((byte)codePoints.Length));

                foreach (var codePoint in codePoints)
                {
                    UInt24(bytes, codePoint);
                }

                bytes.Add(checked((byte)ces.Length));

                foreach (var ce in ces)
                {
                    Weights(bytes, ce);
                }
            }

            UInt(bytes, SiniformRanges.Length);

            foreach (var (first, last, @base) in SiniformRanges)
            {
                UInt24(bytes, first);
                UInt24(bytes, last);
                UInt16(bytes, @base);
            }

            var unified = database.UnifiedIdeographs;
            UInt(bytes, unified.Length);

            foreach (var range in unified)
            {
                UInt24(bytes, range.First);
                UInt24(bytes, range.Last);
            }

            return [.. bytes];
        }

        /// <summary>One element in five bytes: primary, secondary, then tertiary with the variable flag in its top bit.</summary>
        // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 root collation via Broiler.VM CldrTableGenerator; IP=Low; Security=Low; Resources=1; Fingerprint=43EF47
        // Broiler-Human:        PENDING
        private static void Weights(List<byte> bytes, Element element)
        {
            UInt16(bytes, element.Primary);
            UInt16(bytes, element.Secondary);
            bytes.Add((byte)(element.Tertiary | (element.Variable ? 0x80 : 0)));
        }
    }

    /// <summary>
    /// A tailoring built from CLDR collation rules over the root, in weights scaled by 256 (the <c>Scaled</c>
    /// layout Broiler.VM's <c>JsCollationData</c> reads).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The subset of the rule syntax the supported tailorings use</b>: resets, the special reset
    /// <c>[last primary ignorable]</c>, the relations <c>&lt;</c>, <c>&lt;&lt;</c>, <c>&lt;&lt;&lt;</c> and
    /// <c>=</c>, quoting, <c>\u</c> escapes, comments, <c>[import]</c>, <c>[suppressContractions]</c> and
    /// <c>[normalization on]</c>. Anything else is refused by name, so a re-pin that brings syntax this engine
    /// does not know fails generation instead of generating a different order. <i>(Extended 2026-11-02, at milestone
    /// SJL-17.3, for Broiler.JS's tailorings of <c>es</c>, <c>sv</c> and <c>th</c> (Broiler.VM's JSD-0084, decision E3): the
    /// reset <c>[before 1]</c>, an extension (<c>/</c>), and the settings <c>[alternate shifted]</c> and <c>[reorder Thai]</c>,
    /// which the CollationSettings table carries. A prefix (<c>|</c>), any other reset, setting or script, and the star
    /// relations are still refused, so CLDR's <c>eor</c> and <c>emoji</c> rules still are (finding F2).)</i>
    /// </para>
    /// <para>
    /// <b>A relation takes the weight one above its predecessor at its level</b>, and the common weight at the
    /// levels below it: every root weight is a multiple of 256, so a tailored weight lands between its
    /// predecessor and the next root weight. A relation whose weights another tailored item already holds is
    /// moved past it; that is where this engine and ICU's would place a later rule differently, and none of the
    /// supported tailorings reaches it twice from one reset.
    /// </para>
    /// </remarks>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 tailorings via Broiler.VM CldrTableGenerator; IP=Low; Security=Medium; Resources=7; Fingerprint=F6AFB1
    // Broiler-Human:        PENDING
    internal sealed class Tailoring
    {
        /// <summary>The scaled weight of the root's common secondary, 0x0020.</summary>
        // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 tailorings via Broiler.VM CldrTableGenerator; IP=Low; Security=Low; Resources=0; Fingerprint=27B773
        // Broiler-Human:        PENDING
        private const long CommonSecondary = 0x20 << 8;

        /// <summary>The scaled weight of the root's common tertiary, 0x0002.</summary>
        // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 tailorings via Broiler.VM CldrTableGenerator; IP=Low; Security=Low; Resources=0; Fingerprint=95D6FF
        // Broiler-Human:        PENDING
        private const long CommonTertiary = 0x02 << 8;

        private Tailoring()
        {
        }

        /// <summary>Every tailored mapping: NFD key to scaled elements.</summary>
        // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 tailorings via Broiler.VM CldrTableGenerator; IP=Low; Security=Low; Resources=1; Fingerprint=F0F63B
        // Broiler-Human:        PENDING
        internal SortedDictionary<string, ScaledElement[]> Entries { get; } = new(StringComparer.Ordinal);

        /// <summary>The first code points whose root contractions this tailoring suppresses.</summary>
        internal SortedSet<int> Suppressed { get; } = [];

        /// <summary>The root tertiaries that mark an upper-case character, as the profile's reader reads them.</summary>
        // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 tailorings via Broiler.VM CldrTableGenerator; IP=Low; Security=Low; Resources=0; Fingerprint=0E786B
        // Broiler-Human:        PENDING
        internal static ReadOnlySpan<int> UpperTertiaries => [0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x1D];

        /// <summary>
        /// One element in scaled weights, and its case: 0 lower or uncased, 1 mixed, 2 upper, or -1 where nothing
        /// stated it yet.
        /// </summary>
        // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 tailorings via Broiler.VM CldrTableGenerator; IP=Low; Security=Low; Resources=0; Fingerprint=072BD5
        // Broiler-Human:        PENDING
        internal readonly record struct ScaledElement(long Primary, long Secondary, long Tertiary, bool Variable, int Case = -1)
        {
            // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 tailorings via Broiler.VM CldrTableGenerator; IP=Low; Security=Low; Resources=0; Fingerprint=A92CDF
            // Broiler-Human:        PENDING
            internal static ScaledElement From(Element element) =>
                new((long)element.Primary << 8, (long)element.Secondary << 8, (long)element.Tertiary << 8, element.Variable);
        }

        /// <summary>
        /// A tailored string's elements with their case stated, as ICU's <c>CollationBuilder::setCaseBits</c>
        /// states it: the string's own root elements give their case, the first ones to the tailored primaries in
        /// order and the rest, together, to the last; a secondary element is uncased and a tertiary one upper. A
        /// tailored weight says nothing about case, since it sits between two root weights that may disagree.
        /// </summary>
        // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 tailorings via Broiler.VM CldrTableGenerator; IP=Low; Security=Medium; Resources=2; Fingerprint=C95730
        // Broiler-Human:        PENDING
        internal static ScaledElement[] WithCase(ScaledElement[] tailored, IReadOnlyList<Element> rootOfString)
        {
            var primaries = tailored.Count(static element => element.Primary != 0);
            var cases = new int[Math.Max(primaries, 1)];
            var seen = 0;
            var lastCase = 0;

            foreach (var element in rootOfString.Where(static element => element.Primary != 0))
            {
                seen++;
                var stated = UpperTertiaries.Contains(element.Tertiary) ? 2 : 0;

                if (seen < primaries)
                {
                    cases[seen - 1] = stated;
                }
                else if (seen == primaries)
                {
                    lastCase = stated;
                }
                else if (stated != lastCase)
                {
                    lastCase = 1;
                    break;
                }
            }

            if (primaries > 0 && seen >= primaries)
            {
                cases[primaries - 1] = lastCase;
            }

            var result = new ScaledElement[tailored.Length];
            var primary = 0;

            for (var index = 0; index < tailored.Length; index++)
            {
                var element = tailored[index];
                result[index] = element with
                {
                    Case = element.Primary != 0 ? cases[primary++] : element.Secondary != 0 ? 0 : 2,
                };
            }

            return result;
        }

        // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 tailorings via Broiler.VM CldrTableGenerator; IP=Low; Security=Medium; Resources=6; Fingerprint=4F73F8
        // Broiler-Human:        PENDING
        internal static Tailoring Build(
            RootCollation root, CldrUcd database, string rules, Func<string, string?> imports)
        {
            var tailoring = new Tailoring();
            tailoring.Apply(root, database, rules, imports);
            return tailoring;
        }

        // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 tailorings via Broiler.VM CldrTableGenerator; IP=Low; Security=High; Resources=7; Fingerprint=CE9BFD
        // Broiler-Falsified-If: rule syntax outside the supported subset is accepted, or a tailored weight that runs into the next root weight is written
        // Broiler-Human:        PENDING
        private void Apply(RootCollation root, CldrUcd database, string rules, Func<string, string?> imports)
        {
            var used = new HashSet<string>(StringComparer.Ordinal);
            ScaledElement[]? current = null;
            var at = 0;

            // THE ROOT'S OWN ELEMENTS OF A STRING, by longest match, which a tailored string's case is read from.
            List<Element> RootOf(int[] key)
            {
                var result = new List<Element>();
                var index = 0;

                while (index < key.Length)
                {
                    var matched = false;

                    for (var length = key.Length - index; length > 0; length--)
                    {
                        if (root.Entries.TryGetValue(RootCollation.KeyOf(key.Skip(index).Take(length)), out var ces))
                        {
                            result.AddRange(ces);
                            index += length;
                            matched = true;
                            break;
                        }
                    }

                    if (!matched)
                    {
                        result.AddRange(RootCollation.Implicit(key[index], database));
                        index++;
                    }
                }

                return result;
            }

            ScaledElement[] Lookup(int[] key)
            {
                // A TAILORED KEY WINS OVER THE ROOT'S, by longest match, as at run time.
                var result = new List<ScaledElement>();
                var index = 0;

                while (index < key.Length)
                {
                    var matched = false;

                    for (var length = key.Length - index; length > 0; length--)
                    {
                        var part = RootCollation.KeyOf(key.Skip(index).Take(length));

                        if (Entries.TryGetValue(part, out var tailored))
                        {
                            result.AddRange(tailored);
                            index += length;
                            matched = true;
                            break;
                        }

                        if (root.Entries.TryGetValue(part, out var ces))
                        {
                            result.AddRange(ces.Select(ScaledElement.From));
                            index += length;
                            matched = true;
                            break;
                        }
                    }

                    if (!matched)
                    {
                        result.AddRange(RootCollation.Implicit(key[index], database).Select(ScaledElement.From));
                        index++;
                    }
                }

                return [.. result];
            }

            while (true)
            {
                SkipSpace(rules, ref at);

                if (at >= rules.Length)
                {
                    return;
                }

                if (rules[at] == '[')
                {
                    var option = Bracketed(rules, ref at);

                    if (option.StartsWith("import ", StringComparison.Ordinal))
                    {
                        var name = option["import ".Length..].Trim();

                        if (imports(name) is not { } imported)
                        {
                            throw new InvalidDataException("collation rules import `" + name + "`, which the generator was not given");
                        }

                        Apply(root, database, imported, imports);
                        continue;
                    }

                    if (option == "normalization on")
                    {
                        continue;
                    }

                    // THE TAILORING'S SETTINGS (milestone SJL-17.3): the shifted alternate as its default, and a script's
                    // primaries moved before the other scripts', each a line of the CollationSettings table, read by the
                    // collator. Thai's tailoring states both.
                    if (option == "alternate shifted")
                    {
                        AddSetting("alternate|shifted");
                        continue;
                    }

                    if (option.StartsWith("reorder ", StringComparison.Ordinal))
                    {
                        AddSetting(Reorder(root, database, option["reorder ".Length..].Trim()));
                        continue;
                    }

                    if (option.StartsWith("suppressContractions ", StringComparison.Ordinal))
                    {
                        foreach (var codePoint in UnicodeSet(option["suppressContractions ".Length..].Trim()))
                        {
                            Suppressed.Add(codePoint);
                        }

                        continue;
                    }

                    throw new InvalidDataException("collation rules use the option `[" + option + "]`, which this generator does not implement");
                }

                if (rules[at] == '&')
                {
                    at++;
                    SkipSpace(rules, ref at);

                    if (rules[at] == '[')
                    {
                        var position = Bracketed(rules, ref at);

                        // [before 1] (milestone SJL-17.3): the reset is just before the string's primary, after the root's
                        // greatest primary below it, so a relation at the primary level sorts between the two. Swedish's
                        // tailoring puts U+00E5, U+00E4 and U+00F6 before the root's U+01C0 so, and Thai's two before others.
                        if (position == "before 1")
                        {
                            current = [BeforePrimary(root, Lookup(Nfd(database, Text(rules, ref at))))];
                            continue;
                        }

                        if (position != "last primary ignorable")
                        {
                            throw new InvalidDataException("collation rules reset to `[" + position + "]`, which this generator does not implement");
                        }

                        // THE GREATEST ELEMENT THAT IS PRIMARY-IGNORABLE AND NOT SECONDARY-IGNORABLE.
                        var last = root.Entries.Values
                            .Where(static ces => ces.Length == 1 && ces[0].Primary == 0 && ces[0].Secondary != 0)
                            .Select(static ces => ces[0])
                            .MaxBy(static ce => (ce.Secondary, ce.Tertiary));

                        current = [ScaledElement.From(last)];
                        continue;
                    }

                    current = Lookup(Nfd(database, Text(rules, ref at)));
                    continue;
                }

                if (current is null)
                {
                    throw new InvalidDataException("collation rules relate a string before any reset");
                }

                var strength = Relation(rules, ref at);
                var target = Nfd(database, Text(rules, ref at));

                if (target.Length == 0)
                {
                    throw new InvalidDataException("collation rules relate an empty string");
                }

                // AN EXTENSION, relation/extension (milestone SJL-17.3): the tailored string's elements are the relation's
                // and then the extension's, as Swedish's &t<<<U+00FE/h makes U+00FE a t with a tertiary difference followed by an h.
                // The next relation is relative to the tailored string without its extension.
                ScaledElement[]? extension = null;

                if (at < rules.Length && rules[at] == '/')
                {
                    at++;
                    var extended = Nfd(database, Text(rules, ref at));

                    if (extended.Length == 0)
                    {
                        throw new InvalidDataException("collation rules extend a relation by an empty string");
                    }

                    extension = Lookup(extended);
                }

                var next = (ScaledElement[])current.Clone();

                if (strength != 0)
                {
                    var last = next.Length - 1;

                    do
                    {
                        var element = next[last];

                        next[last] = strength switch
                        {
                            1 => element with { Primary = element.Primary + 1, Secondary = CommonSecondary, Tertiary = CommonTertiary },
                            2 => element with { Secondary = element.Secondary + 1, Tertiary = CommonTertiary },
                            _ => element with { Tertiary = element.Tertiary + 1 },
                        };
                    }
                    while (!used.Add(Describe(next)));

                    var overflowed = strength switch
                    {
                        1 => next[last].Primary % 256 == 0,
                        2 => next[last].Secondary % 256 == 0,
                        _ => next[last].Tertiary % 256 == 0,
                    };

                    if (overflowed)
                    {
                        throw new InvalidDataException("a tailored weight ran into the next root weight");
                    }
                }

                if (extension is null)
                {
                    next = WithCase(next, RootOf(target));
                    Entries[RootCollation.KeyOf(target)] = next;
                    current = next;
                    continue;
                }

                // THE CASE OF AN EXTENDED STRING is read over all its elements, as ICU's setCaseBits reads them.
                var stated = WithCase([.. next, .. extension], RootOf(target));
                Entries[RootCollation.KeyOf(target)] = stated;
                current = stated[..next.Length];
            }
        }

        /// <summary>The tailoring's settings, each a line of the CollationSettings table without the tailoring's name, in the order its rules state them.</summary>
        internal List<string> SettingLines { get; } = [];

        // Broiler-AI:           Origin=AI; Spec=UTS35 Part 5 collation settings; IP=Low; Security=Low; Resources=1; Fingerprint=0705CB
        // Broiler-Human:        PENDING
        private void AddSetting(string line)
        {
            if (!SettingLines.Contains(line, StringComparer.Ordinal))
            {
                SettingLines.Add(line);
            }
        }

        /// <summary>
        /// The CollationSettings table's lines: per tailoring, in the options' order, <c>name|alternate|shifted</c> where its rules
        /// state <c>[alternate shifted]</c>, and <c>name|reorder|first|last|before</c> where they state <c>[reorder script]</c>,
        /// the script's root primaries from <c>first</c> to <c>last</c> moved before the root primary <c>before</c>, each four
        /// hexadecimal digits.
        /// </summary>
        // Broiler-AI:           Origin=AI; Spec=UTS35 Part 5 collation settings; IP=Low; Security=Low; Resources=1; Fingerprint=B14694
        // Broiler-Human:        PENDING
        internal static List<string> Settings(IReadOnlyList<(string Name, Tailoring Tailoring)> tailorings)
        {
            var lines = new List<string>();

            foreach (var (name, tailoring) in tailorings)
            {
                foreach (var line in tailoring.SettingLines)
                {
                    lines.Add(name + "|" + line);
                }
            }

            return lines;
        }

        /// <summary>
        /// The scripts a <c>[reorder]</c> may name, each with the block its letters are in: Thai alone, which CLDR 48.2.0's
        /// tailoring of <c>th</c> names. Another script is refused by name, as other syntax is.
        /// </summary>
        // Broiler-AI:           Origin=AI; Spec=UTS35 Part 5 reorder; IP=Low; Security=Low; Resources=0; Fingerprint=9318FF
        // Broiler-Human:        PENDING
        private static ImmutableArray<(string Script, int First, int Last)> ReorderScripts { get; } = [("Thai", 0x0E00, 0x0E7F)];

        /// <summary>
        /// <c>[reorder script]</c>'s setting: the script's primaries in the root, from the least to the greatest primary of a
        /// root mapping whose code points are all in its block, at or after the first script's; and that first primary, the
        /// least primary of a letter after every decimal digit's, where the scripts begin after the space, punctuation, symbol,
        /// currency and digit groups (UTS #35 Part 5, reordering groups). Refused unless no other mapping has a primary in the
        /// script's range, so that moving the range moves that script alone.
        /// </summary>
        // Broiler-AI:           Origin=AI; Spec=UTS35 Part 5 reorder, reordering groups; IP=Low; Security=Medium; Resources=5; Fingerprint=F68279
        // Broiler-Human:        PENDING
        private static string Reorder(RootCollation root, CldrUcd database, string script)
        {
            if (ReorderScripts.FirstOrDefault(entry => string.Equals(entry.Script, script, StringComparison.Ordinal)) is not { Script: not null } named)
            {
                throw new InvalidDataException("collation rules reorder `" + script + "`, which this generator does not implement");
            }

            var singles = root.Entries
                .Where(static entry => !entry.Key.Contains(' ', StringComparison.Ordinal) && entry.Value.Length == 1)
                .Select(static entry => (CodePoint: RootCollation.Key(entry.Key)[0], entry.Value[0].Primary))
                .ToList();
            var digits = singles.Where(entry => database.GeneralCategory(entry.CodePoint) == "Nd").Max(static entry => entry.Primary);
            var scripts = singles.Where(entry => database.GeneralCategory(entry.CodePoint)[0] == 'L' && entry.Primary > digits).Min(static entry => entry.Primary);

            bool InBlock(string key) => RootCollation.Key(key).All(codePoint => codePoint >= named.First && codePoint <= named.Last);

            var primaries = root.Entries
                .Where(entry => InBlock(entry.Key))
                .SelectMany(static entry => entry.Value)
                .Select(static element => element.Primary)
                .Where(primary => primary >= scripts)
                .ToList();

            if (primaries.Count == 0)
            {
                throw new InvalidDataException("collation rules reorder `" + script + "`, whose letters have no primary in the root");
            }

            var first = primaries.Min();
            var last = primaries.Max();

            if (root.Entries.Any(entry => !InBlock(entry.Key) && entry.Value.Any(element => element.Primary >= first && element.Primary <= last)))
            {
                throw new InvalidDataException("collation rules reorder `" + script + "`, whose primaries the root shares with another script");
            }

            return string.Create(NumberFormatInfo.InvariantInfo, $"reorder|{first:X4}|{last:X4}|{scripts:X4}");
        }

        /// <summary>
        /// <c>&amp;[before 1]</c>'s reset: the root's greatest primary below the string's, with the common secondary and tertiary
        /// and that primary's variability, so that a relation at the primary level sorts just before the string. Refused
        /// unless the string has exactly one element with a primary, which is its first.
        /// </summary>
        // Broiler-AI:           Origin=AI; Spec=UTS35 Part 5 reset before; IP=Low; Security=Medium; Resources=4; Fingerprint=0165BE
        // Broiler-Human:        PENDING
        private static ScaledElement BeforePrimary(RootCollation root, ScaledElement[] reset)
        {
            if (reset.Length == 0 || reset[0].Primary == 0 || reset.Skip(1).Any(static element => element.Primary != 0) || reset[0].Primary % 256 != 0)
            {
                throw new InvalidDataException("collation rules reset before a string that is not one root primary, which this generator does not implement");
            }

            var target = reset[0].Primary >> 8;
            var below = root.Entries.Values
                .SelectMany(static elements => elements)
                .Where(element => element.Primary != 0 && element.Primary < target)
                .MaxBy(static element => element.Primary);

            return new ScaledElement((long)below.Primary << 8, CommonSecondary, CommonTertiary, below.Variable);
        }

        // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 tailorings via Broiler.VM CldrTableGenerator; IP=Low; Security=Low; Resources=1; Fingerprint=DAB761
        // Broiler-Human:        PENDING
        private static string Describe(ScaledElement[] elements) =>
            string.Join(',', elements.Select(static element => string.Create(NumberFormatInfo.InvariantInfo, $"{element.Primary}.{element.Secondary}.{element.Tertiary}")));

        // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 tailorings via Broiler.VM CldrTableGenerator; IP=Low; Security=Low; Resources=1; Fingerprint=9A2D73
        // Broiler-Human:        PENDING
        private static void SkipSpace(string rules, ref int at)
        {
            while (at < rules.Length)
            {
                if (IsWhiteSpace(rules[at]))
                {
                    at++;
                    continue;
                }

                if (rules[at] == '#')
                {
                    while (at < rules.Length && rules[at] != '\n')
                    {
                        at++;
                    }

                    continue;
                }

                break;
            }
        }

        // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 tailorings via Broiler.VM CldrTableGenerator; IP=Low; Security=Medium; Resources=1; Fingerprint=A44082
        // Broiler-Human:        PENDING
        private static string Bracketed(string rules, ref int at)
        {
            var depth = 0;
            var start = at + 1;

            for (; at < rules.Length; at++)
            {
                if (rules[at] == '[')
                {
                    depth++;
                }
                else if (rules[at] == ']' && --depth == 0)
                {
                    at++;
                    return rules[start..(at - 1)];
                }
            }

            throw new InvalidDataException("collation rules leave a bracket open");
        }

        /// <summary>The relation at <paramref name="at"/>: 1, 2 or 3 for its level, 0 for <c>=</c>.</summary>
        // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 tailorings via Broiler.VM CldrTableGenerator; IP=Low; Security=Medium; Resources=1; Fingerprint=2077B6
        // Broiler-Human:        PENDING
        private static int Relation(string rules, ref int at)
        {
            if (rules[at] == '=')
            {
                at++;
                return 0;
            }

            var level = 0;

            while (at < rules.Length && rules[at] == '<')
            {
                level++;
                at++;
            }

            if (level is < 1 or > 3 || at < rules.Length && rules[at] == '*')
            {
                throw new InvalidDataException("collation rules use a relation this generator does not implement");
            }

            return level;
        }

        /// <summary>The text after a reset or relation, up to the next operator, with quotes and escapes resolved.</summary>
        // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 tailorings via Broiler.VM CldrTableGenerator; IP=Low; Security=Medium; Resources=2; Fingerprint=891497
        // Broiler-Human:        PENDING
        private static int[] Text(string rules, ref int at)
        {
            var text = new StringBuilder();
            SkipSpace(rules, ref at);

            while (at < rules.Length)
            {
                var c = rules[at];

                if (c is '&' or '<' or '=' or '#' or '[' || IsWhiteSpace(c))
                {
                    break;
                }

                // AN EXTENSION ENDS THE TEXT (milestone SJL-17.3), which the relation reads after it; a prefix is refused.
                if (c == '/')
                {
                    break;
                }

                if (c == '|')
                {
                    throw new InvalidDataException("collation rules use a prefix, which this generator does not implement");
                }

                if (c == '\'')
                {
                    var close = rules.IndexOf('\'', at + 1);
                    text.Append(close == at + 1 ? "'" : rules[(at + 1)..close]);
                    at = close + 1;
                    continue;
                }

                if (c == '\\')
                {
                    text.Append(Escape(rules, ref at));
                    continue;
                }

                text.Append(c);
                at++;
            }

            return [.. CodePoints(text.ToString())];
        }

        // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 tailorings via Broiler.VM CldrTableGenerator; IP=Low; Security=Medium; Resources=1; Fingerprint=8B5574
        // Broiler-Human:        PENDING
        private static string Escape(string rules, ref int at)
        {
            if (rules[at + 1] == 'u')
            {
                var value = int.Parse(rules.AsSpan(at + 2, 4), NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo);
                at += 6;
                return char.ConvertFromUtf32(value);
            }

            if (rules[at + 1] == 'U')
            {
                var value = int.Parse(rules.AsSpan(at + 2, 8), NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo);
                at += 10;
                return char.ConvertFromUtf32(value);
            }

            at += 2;
            return rules[at - 1].ToString();
        }

        /// <summary>A UnicodeSet of single characters and ranges, as <c>[suppressContractions]</c> writes one.</summary>
        // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 tailorings via Broiler.VM CldrTableGenerator; IP=Low; Security=Medium; Resources=2; Fingerprint=477EF3
        // Broiler-Human:        PENDING
        private static List<int> UnicodeSet(string set)
        {
            if (set.Length < 2 || set[0] != '[' || set[^1] != ']')
            {
                throw new InvalidDataException("`" + set + "` is not a set this generator reads");
            }

            var body = set[1..^1];
            var codePoints = new List<int>();
            var at = 0;

            while (at < body.Length)
            {
                if (IsWhiteSpace(body[at]))
                {
                    at++;
                    continue;
                }

                var first = body[at] == '\\' ? char.ConvertToUtf32(Escape(body, ref at), 0) : Next(body, ref at);

                if (at < body.Length && body[at] == '-')
                {
                    at++;
                    var last = body[at] == '\\' ? char.ConvertToUtf32(Escape(body, ref at), 0) : Next(body, ref at);

                    for (var codePoint = first; codePoint <= last; codePoint++)
                    {
                        codePoints.Add(codePoint);
                    }
                }
                else
                {
                    codePoints.Add(first);
                }
            }

            return codePoints;

            static int Next(string text, ref int at)
            {
                var codePoint = char.ConvertToUtf32(text, at);
                at += codePoint > 0xFFFF ? 2 : 1;
                return codePoint;
            }
        }

        /// <summary>
        /// The binary table: per tailoring its name, its entries in scaled weights packed into eight bytes (primary
        /// 24 bits, secondary 17, tertiary 13, the variable flag in the lowest bit), and the code points whose root
        /// contractions it suppresses.
        /// </summary>
        // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 tailorings via Broiler.VM CldrTableGenerator; IP=Low; Security=Medium; Resources=4; Fingerprint=7C354E
        // Broiler-Human:        PENDING
        internal static byte[] Encode(IReadOnlyList<(string Name, Tailoring Tailoring)> tailorings)
        {
            var bytes = new List<byte>();
            UInt(bytes, tailorings.Count);

            foreach (var (name, tailoring) in tailorings)
            {
                bytes.Add(checked((byte)name.Length));
                bytes.AddRange(Encoding.ASCII.GetBytes(name));
                UInt(bytes, tailoring.Entries.Count);

                foreach (var (key, elements) in tailoring.Entries)
                {
                    var codePoints = RootCollation.Key(key);
                    bytes.Add(checked((byte)codePoints.Length));

                    foreach (var codePoint in codePoints)
                    {
                        UInt24(bytes, codePoint);
                    }

                    bytes.Add(checked((byte)elements.Length));

                    foreach (var element in elements)
                    {
                        if (element.Primary >= 1L << 24 || element.Secondary >= 1L << 17 || element.Tertiary >= 1L << 13)
                        {
                            throw new InvalidDataException("a scaled weight is wider than its packing");
                        }

                        if (element.Case is < 0 or > 2)
                        {
                            throw new InvalidDataException("a tailored element's case was never stated");
                        }

                        // THE CASE IS STATED IN BITS 1 AND 2, AND BIT 3 SAYS IT IS: a root element's reader
                        // derives its case from its tertiary, and a tailored one's cannot.
                        var packed = (element.Primary << 40) | (element.Secondary << 23) | (element.Tertiary << 10) |
                                     ((long)element.Case << 1) | (1L << 3) | (element.Variable ? 1L : 0L);

                        for (var shift = 0; shift < 64; shift += 8)
                        {
                            bytes.Add((byte)(packed >> shift));
                        }
                    }
                }

                UInt(bytes, tailoring.Suppressed.Count);

                foreach (var codePoint in tailoring.Suppressed)
                {
                    UInt24(bytes, codePoint);
                }
            }

            return [.. bytes];
        }
    }

    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 collation via Broiler.VM CldrTableGenerator; IP=Low; Security=Low; Resources=0; Fingerprint=C5C173
    // Broiler-Human:        PENDING
    private static void UInt(List<byte> bytes, int value)
    {
        bytes.Add((byte)value);
        bytes.Add((byte)(value >> 8));
        bytes.Add((byte)(value >> 16));
        bytes.Add((byte)(value >> 24));
    }

    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 collation via Broiler.VM CldrTableGenerator; IP=Low; Security=Low; Resources=0; Fingerprint=D4F9E1
    // Broiler-Human:        PENDING
    private static void UInt24(List<byte> bytes, int value)
    {
        bytes.Add((byte)value);
        bytes.Add((byte)(value >> 8));
        bytes.Add((byte)(value >> 16));
    }

    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 collation via Broiler.VM CldrTableGenerator; IP=Low; Security=Low; Resources=0; Fingerprint=DC15B8
    // Broiler-Human:        PENDING
    private static void UInt16(List<byte> bytes, int value)
    {
        bytes.Add((byte)value);
        bytes.Add((byte)(value >> 8));
    }
}
