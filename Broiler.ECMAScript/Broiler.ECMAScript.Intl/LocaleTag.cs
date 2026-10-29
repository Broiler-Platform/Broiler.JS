// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   31
// Annotated:        31/31
// Exempt:           13
// Human-reviewed:   0/31
// IP risk:          Low
// Security risk:    High
// Criteria:         3/3
// Resource impact:  3/10 max
// Unverified:       31
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Broiler.ECMAScript.Intl;

/// <summary>
/// A Unicode BCP 47 locale identifier as the library's operations work on it: ECMA-402's IsWellFormedLanguageTag and
/// CanonicalizeUnicodeLocaleId over UTS #35's grammar and its Annex C, and UTS #35's Add and Remove Likely Subtags.
/// <see cref="LocaleId"/> is its public, immutable face.
/// </summary>
/// <remarks>
/// <para>
/// <b>Ported from Broiler.VM's <c>JsLocaleTag</c></b> at its SJL-9 rule commit <c>4cf9ca44</c>, member for member and in
/// the same order of steps, so that a tag parses, canonicalises, maximises and minimises as Broiler.VM's does. The data is
/// asked through <see cref="ILocaleData"/> where Broiler.VM read its <c>JsIntlTables</c>. Three things change:
/// </para>
/// <list type="bullet">
/// <item><b>ASCII casing</b> (Broiler.VM's JSD-0082, decision D19): every <c>ToLowerInvariant</c>, <c>ToUpperInvariant</c> and
/// <c>char.ToUpperInvariant</c> of the source is ASCII casing. Each one reads text <see cref="Parse"/> has already held to
/// <c>A-Z a-z 0-9 -</c> (row L42, code no input reaches), so no answer moves.</item>
/// <item><b>Row L9, a correction of Broiler.VM</b> (decision D32): a <c>-u-rg</c> or <c>-u-sd</c> subdivision alias whose
/// replacement is a region is written as UTS #35 Annex C writes it, the region in lower case followed by <c>zzzz</c>
/// (<c>en-u-sd-fi01</c> is <c>en-u-sd-axzzzz</c>), where Broiler.VM wrote the region as given (<c>en-u-sd-AX</c>), which is no
/// canonical tag.</item>
/// <item><b>The work is metered and the duplicate checks are linear</b> (decision D18): a duplicate variant or attribute is
/// found with a set, where Broiler.VM searched the list, and each round of alias replacement charges the caller's meter
/// for the alias-table keys it looks up, which grow with the square of the number of variants. No answer moves.</item>
/// </list>
/// <para>
/// <b>Parsed once into fields and written back out in canonical form.</b> Every subtag is lower case but the script, which
/// is title case, and the region, which is upper case; variants and extensions are sorted, a <c>-u-</c> keyword or a
/// <c>-t-</c> field keeps its first occurrence, and the keywords and fields are sorted by key; and the language identifier,
/// the transformed extension's <c>tlang</c> and every extension type have their CLDR aliases replaced. Structural validity
/// is ECMA-402's: <c>unicode_locale_id</c> without the backwards-compatible forms (a leading script, <c>root</c>, an
/// underscore), with no duplicate variant, singleton or <c>tlang</c> variant.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-iswellformedlanguagetag sec-canonicalizeunicodelocaleid, UTS35 Annex C via Broiler.VM JsLocaleTag; IP=Low; Security=Medium; Resources=3; Fingerprint=06FBDE
// Broiler-Human:        PENDING
internal sealed class LocaleTag
{
    /// <summary>The language subtag, lower case.</summary>
    internal string Language { get; set; } = string.Empty;

    /// <summary>The script subtag in title case, or nothing.</summary>
    internal string? Script { get; set; }

    /// <summary>The region subtag in upper case, or nothing.</summary>
    internal string? Region { get; set; }

    /// <summary>The variant subtags, lower case, in the order written until canonicalised.</summary>
    internal List<string> Variants { get; } = [];

    /// <summary>The <c>-u-</c> attributes, in the order written.</summary>
    internal List<string> Attributes { get; } = [];

    /// <summary>The <c>-u-</c> keywords: key, and the type's subtags joined with <c>-</c>, or empty for <c>true</c> left implicit.</summary>
    internal List<(string Key, string Type)> Keywords { get; } = [];

    /// <summary>Whether a <c>-u-</c> extension was written, even an empty one after canonicalisation.</summary>
    internal bool HasUnicodeExtension { get; set; }

    /// <summary>The <c>-t-</c> extension's language, or nothing.</summary>
    internal LocaleTag? TransformLanguage { get; set; }

    /// <summary>The <c>-t-</c> fields: key and value.</summary>
    internal List<(string Key, string Value)> TransformFields { get; } = [];

    /// <summary>Whether a <c>-t-</c> extension was written.</summary>
    internal bool HasTransformExtension { get; set; }

    /// <summary>The other extensions, by singleton: their subtags joined with <c>-</c>.</summary>
    internal SortedDictionary<char, string> OtherExtensions { get; } = [];

    /// <summary>The private-use subtags after <c>x</c>, joined with <c>-</c>, or nothing.</summary>
    internal string? PrivateUse { get; set; }

    /// <summary>The weekday strings of ECMA-402's WeekdayToUValue, by their position in the week from Sunday at 0 and 7.</summary>
    internal static ImmutableArray<string> WeekdayKeys { get; } = ["sun", "mon", "tue", "wed", "thu", "fri", "sat", "sun"];

    // ---- parsing --------------------------------------------------------------------------------

    /// <summary>
    /// The tag <paramref name="text"/> is, or nothing when it is not well-formed (ECMA-402 IsWellFormedLanguageTag,
    /// <c>sec-iswellformedlanguagetag</c>).
    /// </summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-iswellformedlanguagetag sec-canonicalizeunicodelocaleid, UTS35 Annex C via Broiler.VM JsLocaleTag; IP=Low; Security=High; Resources=3; Fingerprint=6AE67A
    // Broiler-Falsified-If: a tag the grammar or ECMA-402's duplicate rules refuse parses, or one they admit does not, or a tag with a code unit outside A-Z a-z 0-9 - parses
    // Broiler-Human:        PENDING
    internal static LocaleTag? Parse(string text)
    {
        if (text.Length == 0 || text[0] == '-' || text[^1] == '-')
        {
            return null;
        }

        foreach (var c in text)
        {
            if (!(c is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9') or '-'))
            {
                return null;
            }
        }

        // D19: ASCII-lowercase, where Broiler.VM's source lowered by the invariant culture; the text is ASCII here.
        var subtags = AsciiLower(text).Split('-');

        foreach (var subtag in subtags)
        {
            if (subtag.Length == 0)
            {
                return null;
            }
        }

        var tag = new LocaleTag();
        var at = 0;

        if (!ParseLanguageId(subtags, ref at, tag))
        {
            return null;
        }

        var singletons = new HashSet<char>();

        while (at < subtags.Length)
        {
            var subtag = subtags[at];

            if (subtag.Length != 1)
            {
                return null;
            }

            var singleton = subtag[0];
            at++;

            if (singleton == 'x')
            {
                var start = at;

                while (at < subtags.Length)
                {
                    if (subtags[at].Length > 8)
                    {
                        return null;
                    }

                    at++;
                }

                if (start == at)
                {
                    return null;
                }

                tag.PrivateUse = string.Join('-', subtags, start, at - start);
                return tag;
            }

            if (!singletons.Add(singleton))
            {
                return null;
            }

            if (singleton == 'u')
            {
                if (!ParseUnicodeExtension(subtags, ref at, tag))
                {
                    return null;
                }

                continue;
            }

            if (singleton == 't')
            {
                if (!ParseTransformExtension(subtags, ref at, tag))
                {
                    return null;
                }

                continue;
            }

            var first = at;

            while (at < subtags.Length && subtags[at].Length is >= 2 and <= 8)
            {
                at++;
            }

            if (first == at)
            {
                return null;
            }

            tag.OtherExtensions[singleton] = string.Join('-', subtags, first, at - first);
        }

        return tag;
    }

    /// <summary>A <c>unicode_language_id</c> at <paramref name="at"/>, without the backwards-compatible forms.</summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-iswellformedlanguagetag sec-canonicalizeunicodelocaleid, UTS35 Annex C via Broiler.VM JsLocaleTag; IP=Low; Security=Medium; Resources=2; Fingerprint=0016B6
    // Broiler-Human:        PENDING
    private static bool ParseLanguageId(string[] subtags, ref int at, LocaleTag tag)
    {
        if (at >= subtags.Length || !IsLanguage(subtags[at]))
        {
            return false;
        }

        tag.Language = subtags[at++];

        if (at < subtags.Length && IsScript(subtags[at]))
        {
            tag.Script = TitleCase(subtags[at++]);
        }

        if (at < subtags.Length && IsRegion(subtags[at]))
        {
            tag.Region = AsciiUpper(subtags[at++]);
        }

        // A SET FOR THE DUPLICATE CHECK, where Broiler.VM searched the list, so that a tag of many variants is checked in
        // linear time; the answer is the same.
        var seen = new HashSet<string>(StringComparer.Ordinal);

        while (at < subtags.Length && IsVariant(subtags[at]))
        {
            if (!seen.Add(subtags[at]))
            {
                return false;
            }

            tag.Variants.Add(subtags[at++]);
        }

        return true;
    }

    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-iswellformedlanguagetag sec-canonicalizeunicodelocaleid, UTS35 Annex C via Broiler.VM JsLocaleTag; IP=Low; Security=Medium; Resources=2; Fingerprint=A03A08
    // Broiler-Human:        PENDING
    private static bool ParseUnicodeExtension(string[] subtags, ref int at, LocaleTag tag)
    {
        tag.HasUnicodeExtension = true;
        var start = at;

        while (at < subtags.Length && subtags[at].Length is >= 3 and <= 8 && IsAlphanumeric(subtags[at]))
        {
            tag.Attributes.Add(subtags[at++]);
        }

        while (at < subtags.Length && IsUnicodeKey(subtags[at]))
        {
            var key = subtags[at++];
            var typeStart = at;

            while (at < subtags.Length && subtags[at].Length is >= 3 and <= 8)
            {
                at++;
            }

            tag.Keywords.Add((key, string.Join('-', subtags, typeStart, at - typeStart)));
        }

        return at > start;
    }

    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-iswellformedlanguagetag sec-canonicalizeunicodelocaleid, UTS35 Annex C via Broiler.VM JsLocaleTag; IP=Low; Security=Medium; Resources=2; Fingerprint=46E47A
    // Broiler-Human:        PENDING
    private static bool ParseTransformExtension(string[] subtags, ref int at, LocaleTag tag)
    {
        tag.HasTransformExtension = true;
        var start = at;

        if (at < subtags.Length && IsLanguage(subtags[at]))
        {
            var language = new LocaleTag();

            if (!ParseLanguageId(subtags, ref at, language))
            {
                return false;
            }

            tag.TransformLanguage = language;
        }

        while (at < subtags.Length && IsTransformKey(subtags[at]))
        {
            var key = subtags[at++];
            var valueStart = at;

            while (at < subtags.Length && subtags[at].Length is >= 3 and <= 8)
            {
                at++;
            }

            if (valueStart == at)
            {
                return false;
            }

            tag.TransformFields.Add((key, string.Join('-', subtags, valueStart, at - valueStart)));
        }

        return at > start;
    }

    /// <summary>Whether a subtag is a <c>unicode_language_subtag</c>: two, three or five to eight ASCII letters.</summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-iswellformedlanguagetag sec-canonicalizeunicodelocaleid, UTS35 Annex C via Broiler.VM JsLocaleTag; IP=Low; Security=Low; Resources=1; Fingerprint=70E31F
    // Broiler-Human:        PENDING
    internal static bool IsLanguage(string subtag) => subtag.Length is (>= 2 and <= 3) or (>= 5 and <= 8) && IsAlpha(subtag);

    /// <summary>Whether a subtag is a <c>unicode_script_subtag</c>: four ASCII letters.</summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-iswellformedlanguagetag sec-canonicalizeunicodelocaleid, UTS35 Annex C via Broiler.VM JsLocaleTag; IP=Low; Security=Low; Resources=1; Fingerprint=6C4030
    // Broiler-Human:        PENDING
    internal static bool IsScript(string subtag) => subtag.Length == 4 && IsAlpha(subtag);

    /// <summary>Whether a subtag is a <c>unicode_region_subtag</c>: two ASCII letters or three ASCII digits.</summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-iswellformedlanguagetag sec-canonicalizeunicodelocaleid, UTS35 Annex C via Broiler.VM JsLocaleTag; IP=Low; Security=Low; Resources=1; Fingerprint=8F28C2
    // Broiler-Human:        PENDING
    internal static bool IsRegion(string subtag) => subtag.Length == 2 && IsAlpha(subtag) || subtag.Length == 3 && IsDigits(subtag);

    /// <summary>Whether a subtag is a <c>unicode_variant_subtag</c>: five to eight ASCII letters and digits, or four beginning with a digit.</summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-iswellformedlanguagetag sec-canonicalizeunicodelocaleid, UTS35 Annex C via Broiler.VM JsLocaleTag; IP=Low; Security=Low; Resources=1; Fingerprint=3FB7E0
    // Broiler-Human:        PENDING
    internal static bool IsVariant(string subtag) =>
        (subtag.Length is >= 5 and <= 8 || subtag.Length == 4 && subtag[0] is >= '0' and <= '9') &&
        IsAlphanumeric(subtag);

    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-iswellformedlanguagetag sec-canonicalizeunicodelocaleid, UTS35 Annex C via Broiler.VM JsLocaleTag; IP=Low; Security=Low; Resources=1; Fingerprint=03DC04
    // Broiler-Human:        PENDING
    private static bool IsUnicodeKey(string subtag) =>
        subtag.Length == 2 && IsAlphanumeric(subtag[..1]) && IsAlpha(subtag[1..]);

    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-iswellformedlanguagetag sec-canonicalizeunicodelocaleid, UTS35 Annex C via Broiler.VM JsLocaleTag; IP=Low; Security=Low; Resources=1; Fingerprint=54B3C1
    // Broiler-Human:        PENDING
    private static bool IsTransformKey(string subtag) =>
        subtag.Length == 2 && IsAlpha(subtag[..1]) && IsDigits(subtag[1..]);

    /// <summary>ECMA-402's ASCII-lowercase: A to Z lowered, and every other code unit as it is.</summary>
    /// <remarks>
    /// The platform's invariant casing is not this: it lowers U+212A KELVIN SIGN to <c>k</c> and, under ICU, more besides,
    /// so a subtag checked after it could pass with a character no tag may hold.
    /// </remarks>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-iswellformedlanguagetag sec-canonicalizeunicodelocaleid, UTS35 Annex C via Broiler.VM JsLocaleTag; IP=Low; Security=Low; Resources=1; Fingerprint=9CA6BE
    // Broiler-Human:        PENDING
    internal static string AsciiLower(string text)
    {
        var chars = text.ToCharArray();

        for (var i = 0; i < chars.Length; i++)
        {
            if (chars[i] is >= 'A' and <= 'Z')
            {
                chars[i] = (char)(chars[i] + 32);
            }
        }

        return new string(chars);
    }

    /// <summary>ASCII-uppercase: a to z raised, and every other code unit as it is (D19, for Broiler.VM's invariant upper-casing).</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-iswellformedlanguagetag sec-canonicalizeunicodelocaleid, UTS35 Annex C via Broiler.VM JsLocaleTag; IP=Low; Security=Low; Resources=1; Fingerprint=810469
    // Broiler-Human:        PENDING
    internal static string AsciiUpper(string text)
    {
        var chars = text.ToCharArray();

        for (var i = 0; i < chars.Length; i++)
        {
            if (chars[i] is >= 'a' and <= 'z')
            {
                chars[i] = (char)(chars[i] - 32);
            }
        }

        return new string(chars);
    }

    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-iswellformedlanguagetag sec-canonicalizeunicodelocaleid, UTS35 Annex C via Broiler.VM JsLocaleTag; IP=Low; Security=Low; Resources=1; Fingerprint=4382E4
    // Broiler-Human:        PENDING
    internal static bool IsAlpha(string text)
    {
        foreach (var c in text)
        {
            if (c is not ((>= 'a' and <= 'z') or (>= 'A' and <= 'Z')))
            {
                return false;
            }
        }

        return text.Length != 0;
    }

    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-iswellformedlanguagetag sec-canonicalizeunicodelocaleid, UTS35 Annex C via Broiler.VM JsLocaleTag; IP=Low; Security=Low; Resources=1; Fingerprint=EF6362
    // Broiler-Human:        PENDING
    internal static bool IsDigits(string text)
    {
        foreach (var c in text)
        {
            if (c is not (>= '0' and <= '9'))
            {
                return false;
            }
        }

        return text.Length != 0;
    }

    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-iswellformedlanguagetag sec-canonicalizeunicodelocaleid, UTS35 Annex C via Broiler.VM JsLocaleTag; IP=Low; Security=Low; Resources=1; Fingerprint=191CB1
    // Broiler-Human:        PENDING
    internal static bool IsAlphanumeric(string text)
    {
        foreach (var c in text)
        {
            if (c is not ((>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9')))
            {
                return false;
            }
        }

        return text.Length != 0;
    }

    /// <summary>Whether a String is a BCP 47 <c>type</c>: subtags of three to eight ASCII letters and digits (Broiler.VM's <c>IsTypeSequence</c>).</summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-iswellformedlanguagetag sec-canonicalizeunicodelocaleid, UTS35 Annex C via Broiler.VM JsLocaleTag; IP=Low; Security=Low; Resources=1; Fingerprint=705907
    // Broiler-Human:        PENDING
    internal static bool IsTypeSequence(string text)
    {
        if (text.Length == 0)
        {
            return false;
        }

        foreach (var part in text.Split('-'))
        {
            if (part.Length is < 3 or > 8 || !IsAlphanumeric(part))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>A script subtag in title case: its first letter upper case and the rest lower case, by ASCII casing (D19).</summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-iswellformedlanguagetag sec-canonicalizeunicodelocaleid, UTS35 Annex C via Broiler.VM JsLocaleTag; IP=Low; Security=Low; Resources=1; Fingerprint=C3C1CB
    // Broiler-Human:        PENDING
    private static string TitleCase(string subtag) => AsciiUpper(subtag[..1]) + AsciiLower(subtag[1..]);

    // ---- canonicalisation -----------------------------------------------------------------------

    /// <summary>
    /// ECMA-402's CanonicalizeUnicodeLocaleId (<c>sec-canonicalizeunicodelocaleid</c>): aliases replaced, extensions
    /// canonicalised and ordered, in canonical case.
    /// </summary>
    /// <param name="data">The locale data whose aliases and BCP 47 types it reads.</param>
    /// <param name="charge">The caller's meter: the alias-table keys each round of alias replacement looks up (<see cref="ReplaceAliases"/>).</param>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-iswellformedlanguagetag sec-canonicalizeunicodelocaleid, UTS35 Annex C via Broiler.VM JsLocaleTag; IP=Low; Security=High; Resources=3; Fingerprint=A64040
    // Broiler-Falsified-If: a canonical tag canonicalizes to anything but itself, or an alias the CLDR data names survives canonicalization
    // Broiler-Human:        PENDING
    internal LocaleTag Canonicalize(ILocaleData data, WorkCharge? charge = null)
    {
        ReplaceAliases(data, charge);

        // -u-: ATTRIBUTES SORTED WITHOUT DUPLICATES, KEYWORDS BY FIRST OCCURRENCE, TYPES BY THEIR ALIASES, `true` LEFT
        // IMPLICIT, AND THE KEYWORDS SORTED. A set finds a duplicate attribute, where Broiler.VM searched the list, so that
        // many attributes are deduplicated in linear time; the answer is the same.
        var attributes = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var attribute in Attributes)
        {
            if (seen.Add(attribute))
            {
                attributes.Add(attribute);
            }
        }

        attributes.Sort(StringComparer.Ordinal);
        Attributes.Clear();
        Attributes.AddRange(attributes);

        var keywords = new List<(string Key, string Type)>();

        foreach (var (key, type) in Keywords)
        {
            if (keywords.Exists(keyword => keyword.Key == key))
            {
                continue;
            }

            keywords.Add((key, CanonicalType(data, key, type)));
        }

        keywords.Sort(static (left, right) => string.CompareOrdinal(left.Key, right.Key));
        Keywords.Clear();
        Keywords.AddRange(keywords);

        // -t-: THE LANGUAGE CANONICALISED AND THE FIELDS BY FIRST OCCURRENCE, PREFERRED VALUES, SORTED.
        TransformLanguage?.ReplaceAliases(data, charge);

        var fields = new List<(string Key, string Value)>();

        foreach (var (key, value) in TransformFields)
        {
            if (fields.Exists(field => field.Key == key))
            {
                continue;
            }

            fields.Add((key, data.TryExtensionType('t', key, value, out var preferred) ? preferred : value));
        }

        fields.Sort(static (left, right) => string.CompareOrdinal(left.Key, right.Key));
        TransformFields.Clear();
        TransformFields.AddRange(fields);

        return this;
    }

    /// <summary>A <c>-u-</c> type in canonical form: an alias replaced, a subdivision alias replaced, <c>true</c> made implicit.</summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-iswellformedlanguagetag sec-canonicalizeunicodelocaleid, UTS35 Annex C via Broiler.VM JsLocaleTag; IP=Low; Security=High; Resources=2; Fingerprint=4D5433
    // Broiler-Falsified-If: a subdivision alias whose replacement is a region is written other than as the region in lower case followed by zzzz (row L9), or a type CLDR's BCP 47 data replaces is kept
    // Broiler-Human:        PENDING
    private static string CanonicalType(ILocaleData data, string key, string type)
    {
        var canonical = data.TryExtensionType('u', key, type, out var preferred) ? preferred : type;

        // A REGION OR SUBDIVISION VALUE IS A SUBDIVISION CODE, and CLDR's subdivision aliases replace a deprecated one, by
        // the first of several replacements.
        if (key is "rg" or "sd" && data.Alias(LocaleAliasKind.Subdivision, canonical) is { } replacement)
        {
            canonical = replacement.Split(' ')[0];

            // ROW L9, A CORRECTION OF BROILER.VM (JSD-0082, decision D32): a replacement that is a region is written as
            // UTS #35 Annex C writes it, as a subdivision code - the region in lower case followed by "zzzz" - so that the
            // result is a canonical tag. Broiler.VM wrote the region as the alias data gives it, upper case and alone.
            if (IsRegion(canonical))
            {
                canonical = AsciiLower(canonical) + "zzzz";
            }
        }

        return canonical == "true" ? string.Empty : canonical;
    }

    /// <summary>
    /// UTS #35 Annex C's alias replacement over the language identifier: language aliases by rule, then script, region and
    /// variant aliases, until nothing changes; then the variants sorted.
    /// </summary>
    /// <param name="data">The locale data whose aliases it reads.</param>
    /// <param name="charge">
    /// The caller's meter: before each round, one unit per alias-table key the round's language rule search looks up
    /// (<see cref="CandidateCount"/>), which grows with the square of the number of variants.
    /// </param>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-iswellformedlanguagetag sec-canonicalizeunicodelocaleid, UTS35 Annex C via Broiler.VM JsLocaleTag; IP=Low; Security=Medium; Resources=3; Fingerprint=D4FB11
    // Broiler-Human:        PENDING
    internal void ReplaceAliases(ILocaleData data, WorkCharge? charge = null)
    {
        // A BOUND, because the data's rules terminate and a defect in them must not loop.
        for (var round = 0; round < 16; round++)
        {
            // THE ROUND'S WORK IS CHARGED BEFORE IT: the language rule search looks up a key for every pair of variants
            // (Candidates), so a tag of many variants costs the square of their number, which its length does not bound.
            charge?.Invoke(CandidateCount());

            var changed = ReplaceLanguage(data);

            if (Script is { } script && data.Alias(LocaleAliasKind.Script, script) is { } scriptAlias)
            {
                Script = scriptAlias;
                changed = true;
            }

            if (Region is { } region && data.Alias(LocaleAliasKind.Region, region) is { } regionAlias)
            {
                Region = PickRegion(data, regionAlias);
                changed = true;
            }

            for (var index = 0; index < Variants.Count; index++)
            {
                if (data.Alias(LocaleAliasKind.Variant, Variants[index]) is { } variantAlias)
                {
                    if (Variants.Contains(variantAlias))
                    {
                        Variants.RemoveAt(index--);
                    }
                    else
                    {
                        Variants[index] = variantAlias;
                    }

                    changed = true;
                }
            }

            if (!changed)
            {
                break;
            }
        }

        Variants.Sort(string.CompareOrdinal);
    }

    /// <summary>
    /// Applies the most specific language alias rule that matches, and answers whether one did. A rule names a language (or
    /// <c>und</c>) and perhaps a script, a region and variants; it matches when every field it names is in the tag, and it
    /// replaces what it matched with its replacement's fields, adding a replacement field the tag lacks.
    /// </summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-iswellformedlanguagetag sec-canonicalizeunicodelocaleid, UTS35 Annex C via Broiler.VM JsLocaleTag; IP=Low; Security=Medium; Resources=3; Fingerprint=69F57A
    // Broiler-Human:        PENDING
    private bool ReplaceLanguage(ILocaleData data)
    {
        LocaleTag? best = null;
        LocaleTag? replacement = null;
        var bestRank = -1;

        foreach (var candidate in Candidates())
        {
            if (data.Alias(LocaleAliasKind.Language, candidate) is { } target &&
                Parse(candidate) is { } rule &&
                Parse(target) is { } to)
            {
                var rank = rule.Variants.Count * 4 + (rule.Region is null ? 0 : 2) + (rule.Script is null ? 0 : 1);

                if (rank > bestRank)
                {
                    best = rule;
                    replacement = to;
                    bestRank = rank;
                }
            }
        }

        if (best is null || replacement is null)
        {
            return false;
        }

        // A REPLACEMENT LANGUAGE OF `und` KEEPS THE TAG'S: `und-aaland` turns a variant into a region and says nothing about
        // the language.
        if (replacement.Language != "und")
        {
            Language = replacement.Language;
        }

        Script = best.Script is not null ? replacement.Script : Script ?? replacement.Script;
        Region = best.Region is not null ? replacement.Region : Region ?? replacement.Region;

        foreach (var variant in best.Variants)
        {
            Variants.Remove(variant);
        }

        foreach (var variant in replacement.Variants)
        {
            if (!Variants.Contains(variant))
            {
                Variants.Add(variant);
            }
        }

        // A REPLACEMENT CARRYING PRIVATE USE - `i-default` is `en-x-i-default` - never reaches a well-formed tag, because its
        // source is legacy syntax; nothing here adds one.
        return true;
    }

    /// <summary>
    /// The alias-table keys a rule matching this tag could have: its language or <c>und</c>, with or without its script and
    /// its region, and with none, one or two of its variants - the data's longest rule, <c>und-hepburn-heploc</c>, names two.
    /// </summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-iswellformedlanguagetag sec-canonicalizeunicodelocaleid, UTS35 Annex C via Broiler.VM JsLocaleTag; IP=Low; Security=Low; Resources=2; Fingerprint=717AE0
    // Broiler-Human:        PENDING
    internal IEnumerable<string> Candidates()
    {
        foreach (var language in new[] { Language, "und" })
        {
            foreach (var script in Script is null ? new string?[] { null } : [null, Script])
            {
                foreach (var region in Region is null ? new string?[] { null } : [null, Region])
                {
                    var stem = language + (script is null ? string.Empty : "-" + script) + (region is null ? string.Empty : "-" + region);

                    if (language != "und" || script is not null || region is not null)
                    {
                        yield return stem;
                    }

                    foreach (var variant in Variants)
                    {
                        yield return stem + "-" + variant;

                        foreach (var second in Variants)
                        {
                            if (!ReferenceEquals(variant, second))
                            {
                                yield return stem + "-" + variant + "-" + second;
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// How many keys <see cref="Candidates"/> yields for this tag as it stands: for each of its stems - the language or
    /// <c>und</c>, with or without the script and the region - the stem itself unless it is <c>und</c> alone, then each
    /// variant, then each ordered pair of distinct variants.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-iswellformedlanguagetag sec-canonicalizeunicodelocaleid, UTS35 Annex C via Broiler.VM JsLocaleTag; IP=Low; Security=Low; Resources=1; Fingerprint=70D5BF
    // Broiler-Human:        PENDING
    internal ulong CandidateCount()
    {
        var variants = (ulong)Variants.Count;
        var stems = 2UL * (Script is null ? 1UL : 2UL) * (Region is null ? 1UL : 2UL);

        // THE STEMS THAT ARE `und` ALONE yield no key of their own: one for the `und` language, and one more when the tag's
        // language is itself `und`.
        var bare = Language == "und" ? 2UL : 1UL;
        var pairs = variants == 0 ? 0UL : variants * (variants - 1);
        return (stems * (variants + pairs)) + (stems - bare);
    }

    /// <summary>The one region of a replacement list: the region likely for this tag's language and script when the list holds it, and the first otherwise.</summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-iswellformedlanguagetag sec-canonicalizeunicodelocaleid, UTS35 Annex C via Broiler.VM JsLocaleTag; IP=Low; Security=Low; Resources=2; Fingerprint=205F4A
    // Broiler-Human:        PENDING
    private string PickRegion(ILocaleData data, string replacements)
    {
        var regions = replacements.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (regions.Length == 1)
        {
            return regions[0];
        }

        var probe = new LocaleTag { Language = Language, Script = Script };

        if (probe.Maximize(data) && probe.Region is { } likely && Array.IndexOf(regions, likely) >= 0)
        {
            return likely;
        }

        return regions[0];
    }

    /// <summary>UTS #35's Add Likely Subtags over the language, script and region; answers whether the data held a match.</summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-iswellformedlanguagetag sec-canonicalizeunicodelocaleid, UTS35 Annex C via Broiler.VM JsLocaleTag; IP=Low; Security=Low; Resources=2; Fingerprint=01C13B
    // Broiler-Human:        PENDING
    internal bool Maximize(ILocaleData data)
    {
        // CLEAN-UP: Zzzz and ZZ are empty, and a language with a script and a region is already maximal.
        if (Script == "Zzzz")
        {
            Script = null;
        }

        if (Region == "ZZ")
        {
            Region = null;
        }

        if (Language != "und" && Script is not null && Region is not null)
        {
            return true;
        }

        // LOOKUP ORDER: language_script_region, language_script, language_region, language.
        var keys = new List<string>();

        if (Script is not null && Region is not null)
        {
            keys.Add(Language + "-" + Script + "-" + Region);
        }

        if (Script is not null)
        {
            keys.Add(Language + "-" + Script);
        }

        if (Region is not null)
        {
            keys.Add(Language + "-" + Region);
        }

        keys.Add(Language);

        foreach (var key in keys)
        {
            if (data.LikelySubtags(key) is { } found && Parse(found) is { } result)
            {
                if (Language == "und")
                {
                    Language = result.Language;
                }

                Script ??= result.Script;
                Region ??= result.Region;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// UTS #35's Remove Likely Subtags: the shortest of the language, the language and region, and the language and script
    /// whose likely subtags are this tag's, its variants and extensions kept; whether the data held a match for the tag at all.
    /// </summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-iswellformedlanguagetag sec-canonicalizeunicodelocaleid, UTS35 Annex C via Broiler.VM JsLocaleTag; IP=Low; Security=Low; Resources=2; Fingerprint=B3DD18
    // Broiler-Human:        PENDING
    internal bool Minimize(ILocaleData data)
    {
        var maximal = Parse(LanguageId())!;

        if (!maximal.Maximize(data))
        {
            return false;
        }

        foreach (var (script, region) in new[] { ((string?)null, (string?)null), (null, maximal.Region), (maximal.Script, null) })
        {
            var trial = new LocaleTag { Language = maximal.Language, Script = script, Region = region };

            if (trial.Maximize(data) && trial.Language == maximal.Language && trial.Script == maximal.Script && trial.Region == maximal.Region)
            {
                Language = maximal.Language;
                Script = script;
                Region = region;
                return true;
            }
        }

        Language = maximal.Language;
        Script = maximal.Script;
        Region = maximal.Region;
        return true;
    }

    // ---- writing --------------------------------------------------------------------------------

    /// <summary>The language identifier alone: language, script, region and variants.</summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-iswellformedlanguagetag sec-canonicalizeunicodelocaleid, UTS35 Annex C via Broiler.VM JsLocaleTag; IP=Low; Security=Low; Resources=2; Fingerprint=13B07A
    // Broiler-Human:        PENDING
    internal string LanguageId()
    {
        var text = new StringBuilder(Language);

        if (Script is not null)
        {
            text.Append('-').Append(Script);
        }

        if (Region is not null)
        {
            text.Append('-').Append(Region);
        }

        foreach (var variant in Variants)
        {
            text.Append('-').Append(variant);
        }

        return text.ToString();
    }

    /// <summary>The whole tag, extensions in singleton order and private use last.</summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-iswellformedlanguagetag sec-canonicalizeunicodelocaleid, UTS35 Annex C via Broiler.VM JsLocaleTag; IP=Low; Security=Low; Resources=2; Fingerprint=10261E
    // Broiler-Human:        PENDING
    public override string ToString()
    {
        var extensions = new SortedDictionary<char, string>(OtherExtensions);

        if (HasTransformExtension && (TransformLanguage is not null || TransformFields.Count != 0))
        {
            var t = new StringBuilder();

            if (TransformLanguage is not null)
            {
                // D19: ASCII-lowercase, where Broiler.VM's source lowered by the invariant culture.
                t.Append(AsciiLower(TransformLanguage.LanguageId()));
            }

            foreach (var (key, value) in TransformFields)
            {
                t.Append(t.Length == 0 ? string.Empty : "-").Append(key).Append('-').Append(value);
            }

            extensions['t'] = t.ToString();
        }

        if (HasUnicodeExtension && (Attributes.Count != 0 || Keywords.Count != 0))
        {
            var u = new StringBuilder();

            foreach (var attribute in Attributes)
            {
                u.Append(u.Length == 0 ? string.Empty : "-").Append(attribute);
            }

            foreach (var (key, type) in Keywords)
            {
                u.Append(u.Length == 0 ? string.Empty : "-").Append(key);

                if (type.Length != 0)
                {
                    u.Append('-').Append(type);
                }
            }

            extensions['u'] = u.ToString();
        }

        var text = new StringBuilder(LanguageId());

        foreach (var (singleton, body) in extensions)
        {
            text.Append('-').Append(singleton).Append('-').Append(body);
        }

        if (PrivateUse is not null)
        {
            text.Append("-x-").Append(PrivateUse);
        }

        return text.ToString();
    }

    /// <summary>The tag with its <c>-u-</c> extension removed: ECMA-402's locale without the extension sequence.</summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-iswellformedlanguagetag sec-canonicalizeunicodelocaleid, UTS35 Annex C via Broiler.VM JsLocaleTag; IP=Low; Security=Low; Resources=2; Fingerprint=A35FCE
    // Broiler-Human:        PENDING
    internal string WithoutUnicodeExtension()
    {
        var copy = Parse(ToString())!;
        copy.HasUnicodeExtension = false;
        copy.Attributes.Clear();
        copy.Keywords.Clear();
        return copy.ToString();
    }

    /// <summary>A copy that shares nothing mutable with this tag.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-iswellformedlanguagetag sec-canonicalizeunicodelocaleid, UTS35 Annex C via Broiler.VM JsLocaleTag; IP=Low; Security=Low; Resources=2; Fingerprint=AFCAE8
    // Broiler-Human:        PENDING
    internal LocaleTag Clone()
    {
        var copy = new LocaleTag
        {
            Language = Language,
            Script = Script,
            Region = Region,
            HasUnicodeExtension = HasUnicodeExtension,
            TransformLanguage = TransformLanguage?.Clone(),
            HasTransformExtension = HasTransformExtension,
            PrivateUse = PrivateUse,
        };

        copy.Variants.AddRange(Variants);
        copy.Attributes.AddRange(Attributes);
        copy.Keywords.AddRange(Keywords);
        copy.TransformFields.AddRange(TransformFields);

        foreach (var (singleton, body) in OtherExtensions)
        {
            copy.OtherExtensions[singleton] = body;
        }

        return copy;
    }
}
