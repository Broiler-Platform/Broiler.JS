// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// HAIRS Code Assurance
// --------------------
// Relevant units:   42
// Annotated:        42/42
// Exempt:           18
// Human-reviewed:   0/42
// IP risk:          Low
// Security risk:    Medium
// Criteria:         0/0
// Resource impact:  4/10 max
// Unverified:       42
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Broiler.Unicode.Algorithms;

namespace Broiler.ECMAScript.Intl;

/// <summary>
/// The collation data an engine hands in, decoded: the root collation, its tailorings and their settings, read from the
/// tables <see cref="IntlTable.CollationRoot"/>, <see cref="IntlTable.CollationTailorings"/> and
/// <see cref="IntlTable.CollationSettings"/> of an <see cref="IIntlDataSet"/>; and the questions an engine's
/// <c>Intl.Collator</c> asks of them - which tailoring a locale, usage and collation select, which collations a locale has,
/// and whether a tailoring shifts by default (Broiler.VM's JSD-0084; milestone SJL-17.3).
/// </summary>
/// <remarks>
/// <para>
/// <b>Ported from Broiler.VM's <c>JsCollationData</c></b> (its decoder and the Unicode Collation Algorithm's element lookup,
/// UTS #10 as CLDR's root modifies it), and from its <c>JsRealm.NewCollator</c>'s choice of a tailoring and its
/// <c>CollatorKeyData</c>, at its SJL-17 rule commit <c>4e6a41e9</c>. The element lookup and the layouts are unchanged; the
/// choice of a tailoring and the collations of a locale are read from the tailorings the data carries, so that Broiler.VM's
/// three - root search and German phonebook and search - give its answers, and Broiler.JS's list (JSD-0084, decision E3) gives
/// the same rule for <c>es</c>, <c>sv</c> and <c>th</c>.
/// </para>
/// <para>
/// <b>Weights are scaled by 256 and packed into one <see cref="ulong"/></b>: the primary in the top 24 bits, the secondary in
/// the next 17, the tertiary in the next 13 and the variable flag in the lowest bit. A root weight is a multiple of 256, so a
/// tailored weight - which the generator placed one above its predecessor - sorts between two root weights without moving
/// either.
/// </para>
/// <para>
/// <b>The layouts are the generator's</b> (<c>Broiler.ECMAScript.Generation</c>'s <c>CldrTableGenerator</c>), little-endian. The
/// root: a count and runs of single elements whose primary rises with the code point (first code point, length, then one
/// element in five bytes: primary, secondary, tertiary with the variable flag in its top bit); a count and the other mappings
/// (key length, key code points, element count, elements); the siniform implicit-weight ranges; and the unified-ideograph
/// ranges. The tailorings: a count, then per tailoring its name, its entries in packed scaled weights, and the code points
/// whose root contractions it suppresses. The settings, a text table: <c>tailoring|alternate|shifted</c>, and
/// <c>tailoring|reorder|first|last|before</c>, a script's root primaries moved before another root primary, each four
/// hexadecimal digits; a data set without the table states no setting, as Broiler.VM's.
/// </para>
/// <para>
/// <b>Han across blocks</b> takes UCA 17's implicit weights by block, as Broiler.VM's data does: the core unified ideographs,
/// then the other blocks' (the owner's decision of 2026-11-02 on row O3, recorded in Broiler.JS's
/// <c>docs/intl/design.md</c>).
/// </para>
/// <para>
/// <b>Decoded once</b>, on first use, thread-safely; the instance is then immutable and may be shared (convention 10).
/// </para>
/// </remarks>
// Hairs-AI:           Origin=Ported; Spec=UTS10, ECMA-402 sec-intl-collator-internal-slots via Broiler.VM JsCollationData; IP=Low; Security=Medium; Resources=3; Fingerprint=5F83B6
// Hairs-Human:        PENDING
public sealed class UcaCollationData
{
    private readonly IIntlDataSet data;

    private readonly Lazy<Tables> tables;

    /// <summary>The collation data of <paramref name="data"/>, decoded on first use.</summary>
    /// <param name="data">The tables, which answer the same bytes for the life of the process.</param>
    /// <exception cref="ArgumentNullException"><paramref name="data"/> is null.</exception>
    // Hairs-AI:           Origin=AI; Spec=JSD-0084 X3 data through IIntlDataSet; IP=Low; Security=Low; Resources=1; Fingerprint=46BFD1
    // Hairs-Human:        PENDING
    public UcaCollationData(IIntlDataSet data)
    {
        ArgumentNullException.ThrowIfNull(data);
        this.data = data;
        tables = new Lazy<Tables>(() => new Tables(data), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <summary>Whether the data set carries a root collation; an engine without one has no collation data.</summary>
    // Hairs-AI:           Origin=AI; Spec=JSD-0084 X3 data through IIntlDataSet; IP=Low; Security=Low; Resources=0; Fingerprint=3222A7
    // Hairs-Human:        PENDING
    public bool HasRoot => !data.Table(IntlTable.CollationRoot).IsEmpty;

    /// <summary>The tailorings the data carries, by name, in the order the table holds them.</summary>
    /// <exception cref="InvalidDataException">The tables are not in the format the type's remarks state.</exception>
    // Hairs-AI:           Origin=AI; Spec=JSD-0084 E3 tailorings by name; IP=Low; Security=Low; Resources=0; Fingerprint=D4A103
    // Hairs-Human:        PENDING
    public IReadOnlyList<string> TailoringNames => Decoded.Names;

    // Hairs-AI:           Origin=AI; Spec=JSD-0084 X3 data through IIntlDataSet; IP=Low; Security=Low; Resources=1; Fingerprint=16C718
    // Hairs-Human:        PENDING
    internal Tables Decoded => tables.Value;

    /// <summary>Whether the data carries the tailoring <paramref name="name"/>.</summary>
    /// <param name="name">A tailoring's name, such as <c>de-phonebk</c>.</param>
    // Hairs-AI:           Origin=Ported; Spec=JSD-0084 E3 via Broiler.VM JsCollationData.TailoringNamed; IP=Low; Security=Low; Resources=0; Fingerprint=3DD9A4
    // Hairs-Human:        PENDING
    public bool HasTailoring(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return Decoded.TailoringNamed(name) is not null;
    }

    /// <summary>
    /// Whether the tailoring's data states the shifted alternate as its default (<c>[alternate shifted]</c>): Thai's, whose
    /// <c>ignorePunctuation</c> then defaults to true (JSD-0084, decision E5). <see langword="false"/> for the root and for a
    /// tailoring the data does not carry.
    /// </summary>
    /// <param name="tailoring">A tailoring's name, or <see langword="null"/> for the root.</param>
    // Hairs-AI:           Origin=AI; Spec=JSD-0084 E5 Thai alternate from data, UTS35 Part 5 alternate; IP=Low; Security=Low; Resources=0; Fingerprint=5D9794
    // Hairs-Human:        PENDING
    public bool ShiftedByDefault(string? tailoring) =>
        tailoring is not null && Decoded.TailoringNamed(tailoring) is { ShiftedByDefault: true };

    /// <summary>
    /// The tailoring a collator of <paramref name="language"/> compares by, for its usage and resolved collation, or
    /// <see langword="null"/> for the root: Broiler.VM's <c>NewCollator</c>'s choice, read from the tailorings the data carries.
    /// For <c>search</c>, the language's own search collation (<c>de-search</c>), and otherwise the root's
    /// (<c>und-search</c>); for <c>sort</c>, the language's collation of the resolved type (<c>de-phonebk</c>, <c>es-trad</c>),
    /// and otherwise its standard collation (<c>es-standard</c>, <c>th-standard</c>), and otherwise the root.
    /// </summary>
    /// <param name="language">The language subtag of the resolved locale's data locale.</param>
    /// <param name="usage">The resolved <c>usage</c>, <c>sort</c> or <c>search</c>.</param>
    /// <param name="collation">The resolved <c>collation</c>; <c>default</c> or <see langword="null"/> for the default.</param>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-intl.collator, UTS35 Part 5 collation types via Broiler.VM JsRealm.NewCollator; IP=Low; Security=Low; Resources=1; Fingerprint=83990C
    // Hairs-Human:        PENDING
    public string? TailoringFor(string language, string usage, string? collation)
    {
        ArgumentNullException.ThrowIfNull(language);
        ArgumentNullException.ThrowIfNull(usage);

        if (string.Equals(usage, "search", StringComparison.Ordinal))
        {
            return Carried(language + "-search") ?? Carried("und-search");
        }

        if (collation is not null && !string.Equals(collation, "default", StringComparison.Ordinal) && Carried(language + "-" + collation) is { } typed)
        {
            return typed;
        }

        return Carried(language + "-standard");
    }

    /// <summary>
    /// The values a collator of <paramref name="language"/> supports for one relevant extension key, the default first:
    /// ECMA-402's <c>[[SortLocaleData]]</c> and <c>[[SearchLocaleData]]</c>, as Broiler.VM's <c>CollatorKeyData</c> answers them.
    /// <c>co</c>: <see langword="null"/>, then, for <c>sort</c>, the type of each tailoring of the language the data carries
    /// but <c>standard</c> and <c>search</c>, which are never a collation (<c>sec-intl-collator-internal-slots</c>);
    /// <c>kn</c>: <c>false</c>, <c>true</c>; <c>kf</c>: <c>false</c>, <c>lower</c>, <c>upper</c>; any other key, <see langword="null"/>.
    /// </summary>
    /// <param name="language">The language subtag of the locale.</param>
    /// <param name="key">The relevant extension key.</param>
    /// <param name="usage">The resolved <c>usage</c>, <c>sort</c> or <c>search</c>.</param>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-intl-collator-internal-slots via Broiler.VM JsRealm.CollatorKeyData; IP=Low; Security=Low; Resources=1; Fingerprint=C7A5F6
    // Hairs-Human:        PENDING
    public IReadOnlyList<string?> KeyValues(string language, string key, string usage)
    {
        ArgumentNullException.ThrowIfNull(language);
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(usage);

        switch (key)
        {
            case "co":
            {
                var values = new List<string?> { null };

                if (!string.Equals(usage, "sort", StringComparison.Ordinal))
                {
                    return values;
                }

                var prefix = language + "-";

                foreach (var name in Decoded.Names)
                {
                    if (name.StartsWith(prefix, StringComparison.Ordinal) &&
                        name[prefix.Length..] is var type &&
                        !string.Equals(type, "standard", StringComparison.Ordinal) &&
                        !string.Equals(type, "search", StringComparison.Ordinal))
                    {
                        values.Add(type);
                    }
                }

                return values;
            }

            case "kn":
                return ["false", "true"];
            case "kf":
                return ["false", "lower", "upper"];
            default:
                return [null];
        }
    }

    // Hairs-AI:           Origin=AI; Spec=JSD-0084 E3 tailorings by name; IP=Low; Security=Low; Resources=0; Fingerprint=B40B28
    // Hairs-Human:        PENDING
    private string? Carried(string name) => Decoded.TailoringNamed(name) is null ? null : name;

    /// <summary>The decoded tables: Broiler.VM's <c>JsCollationData</c>, with the settings table read beside it.</summary>
    // Hairs-AI:           Origin=Ported; Spec=UTS10 via Broiler.VM JsCollationData; IP=Low; Security=Medium; Resources=3; Fingerprint=DD882D
    // Hairs-Human:        PENDING
    internal sealed class Tables
    {
        /// <summary>The scaled common secondary, 0x0020.</summary>
        // Hairs-AI:           Origin=Ported; Spec=UTS10 via Broiler.VM JsCollationData; IP=Low; Security=Low; Resources=0; Fingerprint=751350
        // Hairs-Human:        PENDING
        internal const ulong CommonSecondary = 0x20 << 8;

        /// <summary>The scaled common tertiary, 0x0002.</summary>
        // Hairs-AI:           Origin=Ported; Spec=UTS10 via Broiler.VM JsCollationData; IP=Low; Security=Low; Resources=0; Fingerprint=CFD8EF
        // Hairs-Human:        PENDING
        internal const ulong CommonTertiary = 0x02 << 8;

        private readonly int[] runFirst;

        private readonly int[] runLength;

        private readonly ulong[] runBase;

        private readonly Dictionary<int, ulong[]> singles = [];

        // Hairs-AI:           Origin=Ported; Spec=UTS10 via Broiler.VM JsCollationData; IP=Low; Security=Low; Resources=1; Fingerprint=0B8B9D
        // Hairs-Human:        PENDING
        private readonly Dictionary<string, ulong[]> contractions = new(StringComparer.Ordinal);

        private readonly HashSet<int> contractionStarts = [];

        private readonly (int First, int Last, int Base)[] siniform;

        private readonly (int First, int Last)[] unified;

        // Hairs-AI:           Origin=Ported; Spec=UTS10 via Broiler.VM JsCollationData; IP=Low; Security=Low; Resources=1; Fingerprint=7B5C7B
        // Hairs-Human:        PENDING
        private readonly Dictionary<string, Tailoring> tailorings = new(StringComparer.Ordinal);

        private readonly List<string> names = [];

        private readonly int maxContraction;

        // Hairs-AI:           Origin=Ported; Spec=UTS10 via Broiler.VM JsCollationData; IP=Low; Security=Medium; Resources=3; Fingerprint=F5458A
        // Hairs-Human:        PENDING
        internal Tables(IIntlDataSet data)
        {
            var root = data.Table(IntlTable.CollationRoot);
            var tailored = data.Table(IntlTable.CollationTailorings);

            if (root.IsEmpty)
            {
                throw new InvalidDataException("the data set carries no root collation");
            }

            try
            {
                var at = 0;
                var runs = ReadInt32(root, ref at);
                runFirst = new int[runs];
                runLength = new int[runs];
                runBase = new ulong[runs];

                for (var index = 0; index < runs; index++)
                {
                    runFirst[index] = ReadInt24(root, ref at);
                    runLength[index] = ReadInt16(root, ref at);
                    runBase[index] = ReadElement(root, ref at);
                }

                var entries = ReadInt32(root, ref at);

                for (var index = 0; index < entries; index++)
                {
                    var key = ReadKey(root, ref at, out var first, out var length);
                    var elements = new ulong[root[at++]];

                    for (var element = 0; element < elements.Length; element++)
                    {
                        elements[element] = ReadElement(root, ref at);
                    }

                    if (length == 1)
                    {
                        singles[first] = elements;
                    }
                    else
                    {
                        contractions[key] = elements;
                        contractionStarts.Add(first);
                        maxContraction = Math.Max(maxContraction, length);
                    }
                }

                siniform = new (int, int, int)[ReadInt32(root, ref at)];

                for (var index = 0; index < siniform.Length; index++)
                {
                    siniform[index] = (ReadInt24(root, ref at), ReadInt24(root, ref at), ReadInt16(root, ref at));
                }

                unified = new (int, int)[ReadInt32(root, ref at)];

                for (var index = 0; index < unified.Length; index++)
                {
                    unified[index] = (ReadInt24(root, ref at), ReadInt24(root, ref at));
                }

                at = 0;
                var count = tailored.IsEmpty ? 0 : ReadInt32(tailored, ref at);

                for (var index = 0; index < count; index++)
                {
                    var nameLength = tailored[at++];
                    var name = Encoding.ASCII.GetString(tailored.Slice(at, nameLength));
                    at += nameLength;

                    var tailoring = new Tailoring();
                    var mappings = ReadInt32(tailored, ref at);

                    for (var mapping = 0; mapping < mappings; mapping++)
                    {
                        var key = ReadKey(tailored, ref at, out var first, out var length);
                        var elements = new ulong[tailored[at++]];

                        for (var element = 0; element < elements.Length; element++)
                        {
                            elements[element] = System.Buffers.Binary.BinaryPrimitives.ReadUInt64LittleEndian(tailored.Slice(at, 8));
                            at += 8;
                        }

                        tailoring.Entries[key] = elements;
                        tailoring.Starts.Add(first);
                        tailoring.MaxKey = Math.Max(tailoring.MaxKey, length);
                    }

                    var suppressed = ReadInt32(tailored, ref at);

                    for (var codePoint = 0; codePoint < suppressed; codePoint++)
                    {
                        tailoring.Suppressed.Add(ReadInt24(tailored, ref at));
                    }

                    tailorings[name] = tailoring;
                    names.Add(name);
                }
            }
            catch (IndexOutOfRangeException exception)
            {
                throw new InvalidDataException("a collation table ends inside a record", exception);
            }
            catch (ArgumentOutOfRangeException exception)
            {
                throw new InvalidDataException("a collation table ends inside a record", exception);
            }

            ReadSettings(IntlTableText.Lines(data.Table(IntlTable.CollationSettings)));
            DigitZero = Primary(Single('0')![0]);
        }

        /// <summary>The scaled primary of the digit zero, where numeric collation puts every number.</summary>
        internal ulong DigitZero { get; }

        /// <summary>The tailorings' names, in the table's order.</summary>
        internal IReadOnlyList<string> Names => names;

        /// <summary>A tailoring by name - <c>und-search</c>, <c>de-phonebk</c>, <c>de-search</c> - or nothing.</summary>
        // Hairs-AI:           Origin=Ported; Spec=UTS10 via Broiler.VM JsCollationData.TailoringNamed; IP=Low; Security=Low; Resources=1; Fingerprint=CB72A2
        // Hairs-Human:        PENDING
        internal Tailoring? TailoringNamed(string name) => tailorings.TryGetValue(name, out var tailoring) ? tailoring : null;

        // Hairs-AI:           Origin=Ported; Spec=UTS10 via Broiler.VM JsCollationData; IP=Low; Security=Low; Resources=0; Fingerprint=66B2C6
        // Hairs-Human:        PENDING
        internal static ulong Primary(ulong element) => element >> 40;

        // Hairs-AI:           Origin=Ported; Spec=UTS10 via Broiler.VM JsCollationData; IP=Low; Security=Low; Resources=0; Fingerprint=E90D92
        // Hairs-Human:        PENDING
        internal static ulong Secondary(ulong element) => (element >> 23) & 0x1FFFF;

        // Hairs-AI:           Origin=Ported; Spec=UTS10 via Broiler.VM JsCollationData; IP=Low; Security=Low; Resources=0; Fingerprint=215174
        // Hairs-Human:        PENDING
        internal static ulong Tertiary(ulong element) => (element >> 10) & 0x1FFF;

        // Hairs-AI:           Origin=Ported; Spec=UTS10 via Broiler.VM JsCollationData; IP=Low; Security=Low; Resources=0; Fingerprint=64F9E2
        // Hairs-Human:        PENDING
        internal static bool Variable(ulong element) => (element & 1) != 0;

        /// <summary>
        /// An element's case: 0 lower or uncased, 1 mixed, 2 upper. A tailored element states it in bits 1 and 2 with bit 3 set,
        /// as the generator read it from the tailored string's root elements; a root element's case is its tertiary's, by
        /// UTS #10's tertiary weight table.
        /// </summary>
        // Hairs-AI:           Origin=Ported; Spec=UTS10 s3.6 via Broiler.VM JsCollationData.Case; IP=Low; Security=Low; Resources=1; Fingerprint=578DAC
        // Hairs-Human:        PENDING
        internal static int Case(ulong element)
        {
            if ((element & 0b1000) != 0)
            {
                return (int)((element >> 1) & 0b11);
            }

            // THE ROOT TERTIARIES THAT MARK AN UPPER-CASE CHARACTER (UTS #10's tertiary weight table), which Broiler.VM held in a
            // static array; a pattern here, since rule SL2 refuses mutable static state, and it answers the same for every weight.
            return (Tertiary(element) >> 8) is 0x08 or 0x09 or 0x0A or 0x0B or 0x0C or 0x1D ? 2 : 0;
        }

        // Hairs-AI:           Origin=Ported; Spec=UTS10 via Broiler.VM JsCollationData; IP=Low; Security=Low; Resources=0; Fingerprint=C92AFD
        // Hairs-Human:        PENDING
        internal static ulong Pack(ulong primary, ulong secondary, ulong tertiary, bool variable) =>
            (primary << 40) | (secondary << 23) | (tertiary << 10) | (variable ? 1UL : 0UL);

        /// <summary>The root's own elements for one code point, or nothing when no key names it.</summary>
        // Hairs-AI:           Origin=Ported; Spec=UTS10 via Broiler.VM JsCollationData.Single; IP=Low; Security=Low; Resources=2; Fingerprint=DFA891
        // Hairs-Human:        PENDING
        internal ulong[]? Single(int codePoint)
        {
            if (singles.TryGetValue(codePoint, out var elements))
            {
                return elements;
            }

            var low = 0;
            var high = runFirst.Length - 1;

            while (low <= high)
            {
                var middle = (low + high) >> 1;

                if (codePoint < runFirst[middle])
                {
                    high = middle - 1;
                }
                else if (codePoint >= runFirst[middle] + runLength[middle])
                {
                    low = middle + 1;
                }
                else
                {
                    return [runBase[middle] + ((ulong)(codePoint - runFirst[middle]) << 48)];
                }
            }

            return null;
        }

        /// <summary>UCA 17's implicit weights for a code point no key names: Han across blocks by block, core ideographs first.</summary>
        // Hairs-AI:           Origin=Ported; Spec=UTS10 s10.1 via Broiler.VM JsCollationData.Implicit; IP=Low; Security=Low; Resources=1; Fingerprint=6B4A2B
        // Hairs-Human:        PENDING
        internal ulong[] Implicit(int codePoint)
        {
            foreach (var (first, last, @base) in siniform)
            {
                if (codePoint >= first && codePoint <= last)
                {
                    var origin = int.MaxValue;

                    foreach (var (other, _, otherBase) in siniform)
                    {
                        if (otherBase == @base)
                        {
                            origin = Math.Min(origin, other);
                        }
                    }

                    return
                    [
                        Pack((ulong)@base << 8, CommonSecondary, CommonTertiary, false),
                        Pack((ulong)((codePoint - origin) | 0x8000) << 8, 0, 0, false),
                    ];
                }
            }

            var isUnified = false;

            foreach (var (first, last) in unified)
            {
                if (codePoint >= first && codePoint <= last)
                {
                    isUnified = true;
                    break;
                }
            }

            var core = codePoint is (>= 0x4E00 and <= 0x9FFF) or (>= 0xF900 and <= 0xFAFF);
            var primary = isUnified && core ? 0xFB40 : isUnified ? 0xFB80 : 0xFBC0;

            return
            [
                Pack((ulong)(primary + (codePoint >> 15)) << 8, CommonSecondary, CommonTertiary, false),
                Pack((ulong)((codePoint & 0x7FFF) | 0x8000) << 8, 0, 0, false),
            ];
        }

        /// <summary>
        /// The collation elements of an NFD code point sequence under a tailoring: UCA's longest match, its discontiguous
        /// extension over unblocked non-starters, and implicit weights for the rest; numeric runs collapsed to their value when
        /// asked for. The combining classes are the normalization owner's, as Broiler.VM read them from its own.
        /// </summary>
        // Hairs-AI:           Origin=Ported; Spec=UTS10 s7 via Broiler.VM JsCollationData.Elements; IP=Low; Security=Medium; Resources=4; Fingerprint=5CF89F
        // Hairs-Human:        PENDING
        internal void Elements(int[] text, Tailoring? tailoring, bool numeric, UnicodeNormalization normalization, List<ulong> output)
        {
            var consumed = new bool[text.Length];

            for (var at = 0; at < text.Length; at++)
            {
                if (consumed[at])
                {
                    continue;
                }

                if (numeric && DigitValue(text[at]) >= 0)
                {
                    at = Number(text, at, consumed, output) - 1;
                    continue;
                }

                var (length, elements) = LongestMatch(text, at, consumed, tailoring);
                var key = length > 1 || CanStartContraction(text[at], tailoring) ? Key(text, at, length) : null;

                // DISCONTIGUOUS MATCHING (UCA S2.1.1-S2.1.3): an unblocked non-starter after the match extends it when the
                // extended key exists, and is consumed out of order.
                if (key is not null)
                {
                    var lastSkipped = 0;

                    for (var next = at + length; next < text.Length; next++)
                    {
                        if (consumed[next])
                        {
                            continue;
                        }

                        var ccc = normalization.CombiningClass(text[next]);

                        if (ccc == 0)
                        {
                            break;
                        }

                        if (lastSkipped != 0 && lastSkipped >= ccc)
                        {
                            lastSkipped = ccc;
                            continue;
                        }

                        var extended = key + char.ConvertFromUtf32(Valid(text[next]));

                        if (Find(extended, text[at], tailoring) is { } found)
                        {
                            key = extended;
                            elements = found;
                            consumed[next] = true;
                            continue;
                        }

                        lastSkipped = ccc;
                    }
                }

                output.AddRange(elements);
                at += length - 1;
            }
        }

        /// <summary>The longest key at <paramref name="at"/> over contiguous unconsumed code points, and its elements.</summary>
        // Hairs-AI:           Origin=Ported; Spec=UTS10 s7 via Broiler.VM JsCollationData.LongestMatch; IP=Low; Security=Low; Resources=3; Fingerprint=104512
        // Hairs-Human:        PENDING
        private (int Length, ulong[] Elements) LongestMatch(int[] text, int at, bool[] consumed, Tailoring? tailoring)
        {
            var first = text[at];

            if (CanStartContraction(first, tailoring))
            {
                var longest = Math.Max(maxContraction, tailoring?.MaxKey ?? 1);

                for (var length = Math.Min(longest, text.Length - at); length > 1; length--)
                {
                    var contiguous = true;

                    for (var index = at + 1; index < at + length; index++)
                    {
                        contiguous &= !consumed[index];
                    }

                    if (contiguous && Find(Key(text, at, length), first, tailoring) is { } found)
                    {
                        return (length, found);
                    }
                }
            }

            if (tailoring is not null && tailoring.Entries.TryGetValue(char.ConvertFromUtf32(Valid(first)), out var tailored))
            {
                return (1, tailored);
            }

            return (1, Single(first) ?? Implicit(first));
        }

        /// <summary>A key's elements in the tailoring, then in the root unless the tailoring suppresses the root's contractions of its first code point.</summary>
        // Hairs-AI:           Origin=Ported; Spec=UTS10 s7 via Broiler.VM JsCollationData.Find; IP=Low; Security=Low; Resources=2; Fingerprint=5F3510
        // Hairs-Human:        PENDING
        private ulong[]? Find(string key, int first, Tailoring? tailoring)
        {
            if (tailoring is not null && tailoring.Entries.TryGetValue(key, out var tailored))
            {
                return tailored;
            }

            if (tailoring is not null && tailoring.Suppressed.Contains(first))
            {
                return null;
            }

            return contractions.TryGetValue(key, out var elements) ? elements : null;
        }

        // Hairs-AI:           Origin=Ported; Spec=UTS10 s7 via Broiler.VM JsCollationData.CanStartContraction; IP=Low; Security=Low; Resources=1; Fingerprint=9554E6
        // Hairs-Human:        PENDING
        private bool CanStartContraction(int codePoint, Tailoring? tailoring) =>
            contractionStarts.Contains(codePoint) && (tailoring is null || !tailoring.Suppressed.Contains(codePoint)) ||
            tailoring is not null && tailoring.Starts.Contains(codePoint);

        // Hairs-AI:           Origin=Ported; Spec=UTS10 s7 via Broiler.VM JsCollationData.Key; IP=Low; Security=Low; Resources=1; Fingerprint=F714CD
        // Hairs-Human:        PENDING
        private static string Key(int[] text, int at, int length)
        {
            var key = new StringBuilder(length * 2);

            for (var index = at; index < at + length; index++)
            {
                key.Append(char.ConvertFromUtf32(Valid(text[index])));
            }

            return key.ToString();
        }

        /// <summary>A code point a key string can hold: a lone surrogate keys as U+FFFD, which no key names.</summary>
        // Hairs-AI:           Origin=Ported; Spec=UTS10 via Broiler.VM JsCollationData.Valid; IP=Low; Security=Low; Resources=0; Fingerprint=548CD9
        // Hairs-Human:        PENDING
        private static int Valid(int codePoint) => codePoint is >= 0xD800 and <= 0xDFFF ? 0xFFFD : codePoint;

        /// <summary>The value of a decimal digit by its root primary, or -1 for anything else.</summary>
        // Hairs-AI:           Origin=Ported; Spec=UTS35 numeric collation via Broiler.VM JsCollationData.DigitValue; IP=Low; Security=Low; Resources=1; Fingerprint=9440FA
        // Hairs-Human:        PENDING
        private int DigitValue(int codePoint)
        {
            if (Single(codePoint) is not { Length: 1 } elements)
            {
                return -1;
            }

            var value = (long)(Primary(elements[0]) >> 8) - (long)(DigitZero >> 8);
            return value is >= 0 and <= 9 && Primary(elements[0]) % 256 == 0 ? (int)value : -1;
        }

        /// <summary>
        /// A run of digits as its value: one element for the count of significant digits, then one per digit, every primary in
        /// the gap after the zero's, so numbers sort among themselves by value and where a digit would among everything else.
        /// </summary>
        // Hairs-AI:           Origin=Ported; Spec=UTS35 numeric collation via Broiler.VM JsCollationData.Number; IP=Low; Security=Low; Resources=2; Fingerprint=5A56C8
        // Hairs-Human:        PENDING
        private int Number(int[] text, int at, bool[] consumed, List<ulong> output)
        {
            var digits = new List<int>();

            while (at < text.Length && !consumed[at] && DigitValue(text[at]) is var digit and >= 0)
            {
                if (digits.Count != 0 || digit != 0)
                {
                    digits.Add(digit);
                }

                at++;
            }

            if (digits.Count == 0)
            {
                digits.Add(0);
            }

            output.Add(Pack(DigitZero + 1 + (ulong)Math.Min(digits.Count, 244), CommonSecondary, CommonTertiary, false));

            foreach (var value in digits)
            {
                output.Add(Pack(DigitZero + 1 + (ulong)value, CommonSecondary, CommonTertiary, false));
            }

            return at;
        }

        /// <summary>
        /// Reads the settings table: <c>tailoring|alternate|shifted</c> and <c>tailoring|reorder|first|last|before</c>, each for a
        /// tailoring the tailorings table carries (milestone SJL-17.3).
        /// </summary>
        // Hairs-AI:           Origin=AI; Spec=UTS35 Part 5 settings alternate and reorder; IP=Low; Security=Medium; Resources=1; Fingerprint=9C587D
        // Hairs-Human:        PENDING
        private void ReadSettings(List<string> lines)
        {
            foreach (var line in lines)
            {
                var fields = IntlTableText.Fields(line, 3, IntlTable.CollationSettings);

                if (TailoringNamed(fields[0]) is not { } tailoring)
                {
                    throw new InvalidDataException("the CollationSettings table names a tailoring the data does not carry: " + line);
                }

                switch (fields[1])
                {
                    case "alternate" when fields.Length == 3 && string.Equals(fields[2], "shifted", StringComparison.Ordinal):
                        tailoring.ShiftedByDefault = true;
                        break;
                    case "reorder" when fields.Length == 5:
                        var first = Hex(fields[2], line) << 8;
                        var last = ((Hex(fields[3], line) + 1) << 8) - 1;
                        var before = Hex(fields[4], line) << 8;

                        if (before > first || first > last)
                        {
                            throw new InvalidDataException("the CollationSettings table moves a range other than one before it: " + line);
                        }

                        tailoring.Reorder = (first, last, before);
                        break;
                    default:
                        throw new InvalidDataException("the CollationSettings table states a setting this library does not read: " + line);
                }
            }
        }

        // Hairs-AI:           Origin=AI; Spec=UTS35 Part 5 settings reorder; IP=Low; Security=Low; Resources=0; Fingerprint=CE197D
        // Hairs-Human:        PENDING
        private static ulong Hex(string field, string line) =>
            field.Length == 4 && ulong.TryParse(field, NumberStyles.AllowHexSpecifier, NumberFormatInfo.InvariantInfo, out var value)
                ? value
                : throw new InvalidDataException("the CollationSettings table has a primary that is not four hexadecimal digits: " + line);

        // Hairs-AI:           Origin=Ported; Spec=UTS10 via Broiler.VM JsCollationData.ReadKey; IP=Low; Security=Low; Resources=1; Fingerprint=D39B8B
        // Hairs-Human:        PENDING
        private static string ReadKey(ReadOnlySpan<byte> data, ref int at, out int first, out int length)
        {
            length = data[at++];
            var key = new StringBuilder(length * 2);
            first = 0;

            for (var index = 0; index < length; index++)
            {
                var codePoint = ReadInt24(data, ref at);

                if (index == 0)
                {
                    first = codePoint;
                }

                key.Append(char.ConvertFromUtf32(Valid(codePoint)));
            }

            return key.ToString();
        }

        // Hairs-AI:           Origin=Ported; Spec=UTS10 via Broiler.VM JsCollationData.ReadElement; IP=Low; Security=Low; Resources=1; Fingerprint=02C7AB
        // Hairs-Human:        PENDING
        private static ulong ReadElement(ReadOnlySpan<byte> data, ref int at)
        {
            var primary = (ulong)ReadInt16(data, ref at);
            var secondary = (ulong)ReadInt16(data, ref at);
            var tertiary = data[at++];
            return Pack(primary << 8, secondary << 8, (ulong)(tertiary & 0x7F) << 8, (tertiary & 0x80) != 0);
        }

        // Hairs-AI:           Origin=Ported; Spec=UTS10 via Broiler.VM JsCollationData.ReadInt32; IP=Low; Security=Low; Resources=0; Fingerprint=1C645E
        // Hairs-Human:        PENDING
        private static int ReadInt32(ReadOnlySpan<byte> data, ref int at)
        {
            var value = data[at] | (data[at + 1] << 8) | (data[at + 2] << 16) | (data[at + 3] << 24);
            at += 4;
            return value;
        }

        // Hairs-AI:           Origin=Ported; Spec=UTS10 via Broiler.VM JsCollationData.ReadInt24; IP=Low; Security=Low; Resources=0; Fingerprint=AE5675
        // Hairs-Human:        PENDING
        private static int ReadInt24(ReadOnlySpan<byte> data, ref int at)
        {
            var value = data[at] | (data[at + 1] << 8) | (data[at + 2] << 16);
            at += 3;
            return value;
        }

        // Hairs-AI:           Origin=Ported; Spec=UTS10 via Broiler.VM JsCollationData.ReadInt16; IP=Low; Security=Low; Resources=0; Fingerprint=EA7518
        // Hairs-Human:        PENDING
        private static int ReadInt16(ReadOnlySpan<byte> data, ref int at)
        {
            var value = data[at] | (data[at + 1] << 8);
            at += 2;
            return value;
        }
    }

    /// <summary>
    /// One tailoring: its mappings by key, the longest key, the code points whose root contractions it suppresses, and its
    /// settings - the shifted alternate as its default, and a script's primaries moved before another primary.
    /// </summary>
    // Hairs-AI:           Origin=Ported; Spec=UTS10, UTS35 Part 5 via Broiler.VM JsCollationData.Tailoring; IP=Low; Security=Low; Resources=1; Fingerprint=F6AFB1
    // Hairs-Human:        PENDING
    internal sealed class Tailoring
    {
        // Hairs-AI:           Origin=Ported; Spec=UTS10 via Broiler.VM JsCollationData; IP=Low; Security=Low; Resources=1; Fingerprint=5E83E8
        // Hairs-Human:        PENDING
        internal Dictionary<string, ulong[]> Entries { get; } = new(StringComparer.Ordinal);

        internal HashSet<int> Starts { get; } = [];

        internal HashSet<int> Suppressed { get; } = [];

        internal int MaxKey { get; set; } = 1;

        internal bool ShiftedByDefault { get; set; }

        /// <summary>The scaled primaries <c>First</c> to <c>Last</c> moved to begin at <c>Before</c>, and those from <c>Before</c> moved after them; or nothing.</summary>
        internal (ulong First, ulong Last, ulong Before)? Reorder { get; set; }
    }
}
