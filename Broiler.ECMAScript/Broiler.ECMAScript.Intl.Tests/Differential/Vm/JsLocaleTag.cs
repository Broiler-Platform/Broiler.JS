// DIFFERENTIAL CHECK, BROILER.VM'S ARM: a test-only copy of Broiler.VM's code at its SJL-9 rule commit, the
// reference the differential check of Broiler.VM's JSD-0082, decision D22, compares the library with. It is compiled
// into this test project and nowhere else, never ships, and runs under the JIT only; see Differential/Vm/README.md.
//
// Copied from Broiler.VM at 4cf9ca449fc96a1a5be810f6d0ca31c42b855c8f:
//   src/Broiler.VM.Profile.JavaScript/JsLocaleTag.cs
//     blob 7201ae19b2714d2916e7bd5d9b77c4cf2bfa9a59: the whole file: JsLocaleTag
//
// Changes from the source, none of which changes a result:
//   - the namespace;
//   - the generated SPDX and assurance header and the per-member assurance annotations removed.

using System;
using System.Collections.Generic;
using System.Text;

namespace Broiler.ECMAScript.Intl.Tests.Differential.Vm;

/// <summary>
/// A Unicode BCP 47 locale identifier: ECMA-402's <c>IsStructurallyValidLanguageTag</c> and
/// <c>CanonicalizeUnicodeLocaleId</c> over UTS #35's grammar and its Annex C (JSD-0043).
/// </summary>
/// <remarks>
/// <para>
/// <b>Parsed once into fields and written back out in canonical form.</b> Every subtag is lower case
/// but the script, which is title case, and the region, which is upper case; variants and
/// extensions are sorted, a <c>-u-</c> keyword or a <c>-t-</c> field keeps its first occurrence and
/// the keywords and fields are sorted by key; and the language identifier, the transformed
/// extension's <c>tlang</c> and every extension type have their CLDR aliases replaced.
/// </para>
/// <para>
/// <b>Structural validity is ECMA-402's</b>: UTS #35's <c>unicode_locale_id</c> grammar without the
/// backwards-compatible forms - a leading script, <c>root</c>, an underscore - and with no duplicate
/// variant, no duplicate singleton and no duplicate variant in a <c>tlang</c>.
/// </para>
/// </remarks>
internal sealed class JsLocaleTag
{
    /// <summary>The language subtag, lower case.</summary>
    internal string Language { get; set; } = string.Empty;

    /// <summary>The script subtag in title case, or nothing.</summary>
    internal string? Script { get; set; }

    /// <summary>The region subtag in upper case, or nothing.</summary>
    internal string? Region { get; set; }

    /// <summary>The variant subtags, lower case, in the order written until canonicalized.</summary>
    internal List<string> Variants { get; } = [];

    /// <summary>The <c>-u-</c> attributes, in the order written.</summary>
    internal List<string> Attributes { get; } = [];

    /// <summary>The <c>-u-</c> keywords: key, and the type's subtags joined with <c>-</c> or empty for <c>true</c> left implicit.</summary>
    internal List<(string Key, string Type)> Keywords { get; } = [];

    /// <summary>Whether a <c>-u-</c> extension was written, even an empty one after canonicalization.</summary>
    internal bool HasUnicodeExtension { get; set; }

    /// <summary>The <c>-t-</c> extension's language, or nothing.</summary>
    internal JsLocaleTag? TransformLanguage { get; set; }

    /// <summary>The <c>-t-</c> fields: key and value.</summary>
    internal List<(string Key, string Value)> TransformFields { get; } = [];

    /// <summary>Whether a <c>-t-</c> extension was written.</summary>
    internal bool HasTransformExtension { get; set; }

    /// <summary>The other extensions, by singleton: their subtags joined with <c>-</c>.</summary>
    internal SortedDictionary<char, string> OtherExtensions { get; } = [];

    /// <summary>The private-use subtags after <c>x</c>, joined with <c>-</c>, or nothing.</summary>
    internal string? PrivateUse { get; set; }

    // ---- parsing --------------------------------------------------------------------------------

    /// <summary>
    /// The tag <paramref name="text"/> is, or nothing when it is not structurally valid
    /// (ECMA-402 <c>IsStructurallyValidLanguageTag</c>).
    /// </summary>
    internal static JsLocaleTag? Parse(string text)
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

        var subtags = text.ToLowerInvariant().Split('-');

        foreach (var subtag in subtags)
        {
            if (subtag.Length == 0)
            {
                return null;
            }
        }

        var tag = new JsLocaleTag();
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
    private static bool ParseLanguageId(string[] subtags, ref int at, JsLocaleTag tag)
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
            tag.Region = subtags[at++].ToUpperInvariant();
        }

        while (at < subtags.Length && IsVariant(subtags[at]))
        {
            if (tag.Variants.Contains(subtags[at]))
            {
                return false;
            }

            tag.Variants.Add(subtags[at++]);
        }

        return true;
    }

    private static bool ParseUnicodeExtension(string[] subtags, ref int at, JsLocaleTag tag)
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

    private static bool ParseTransformExtension(string[] subtags, ref int at, JsLocaleTag tag)
    {
        tag.HasTransformExtension = true;
        var start = at;

        if (at < subtags.Length && IsLanguage(subtags[at]))
        {
            var language = new JsLocaleTag();

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

    internal static bool IsLanguage(string subtag) => subtag.Length is (>= 2 and <= 3) or (>= 5 and <= 8) && IsAlpha(subtag);

    internal static bool IsScript(string subtag) => subtag.Length == 4 && IsAlpha(subtag);

    internal static bool IsRegion(string subtag) => subtag.Length == 2 && IsAlpha(subtag) || subtag.Length == 3 && IsDigits(subtag);

    internal static bool IsVariant(string subtag) =>
        IsAlphanumeric(subtag) &&
        (subtag.Length is >= 5 and <= 8 || subtag.Length == 4 && subtag[0] is >= '0' and <= '9');

    private static bool IsUnicodeKey(string subtag) =>
        subtag.Length == 2 && IsAlphanumeric(subtag[..1]) && IsAlpha(subtag[1..]);

    private static bool IsTransformKey(string subtag) =>
        subtag.Length == 2 && IsAlpha(subtag[..1]) && IsDigits(subtag[1..]);

    /// <summary>ECMA-402's ASCII-lowercase: A to Z lowered, and every other code unit as it is.</summary>
    /// <remarks>
    /// The platform's invariant casing is not this: it lowers U+212A KELVIN SIGN to <c>k</c> and, under
    /// ICU, more besides, so a subtag checked after it could pass with a character no tag may hold.
    /// </remarks>
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

    private static bool IsDigits(string text)
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

    private static string TitleCase(string subtag) => char.ToUpperInvariant(subtag[0]) + subtag[1..].ToLowerInvariant();

    // ---- canonicalization -----------------------------------------------------------------------

    /// <summary>
    /// ECMA-402's <c>CanonicalizeUnicodeLocaleId</c>: aliases replaced, extensions canonicalized and
    /// ordered, in canonical case.
    /// </summary>
    internal JsLocaleTag Canonicalize(JsIntlTables tables)
    {
        ReplaceAliases(tables);

        // -u-: ATTRIBUTES SORTED WITHOUT DUPLICATES, KEYWORDS BY FIRST OCCURRENCE, TYPES BY THEIR
        // ALIASES, `true` LEFT IMPLICIT, AND THE KEYWORDS SORTED.
        var attributes = new List<string>();

        foreach (var attribute in Attributes)
        {
            if (!attributes.Contains(attribute))
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

            keywords.Add((key, CanonicalType(tables, key, type)));
        }

        keywords.Sort(static (left, right) => string.CompareOrdinal(left.Key, right.Key));
        Keywords.Clear();
        Keywords.AddRange(keywords);

        // -t-: THE LANGUAGE CANONICALIZED AND THE FIELDS BY FIRST OCCURRENCE, PREFERRED VALUES, SORTED.
        TransformLanguage?.ReplaceAliases(tables);

        var fields = new List<(string Key, string Value)>();

        foreach (var (key, value) in TransformFields)
        {
            if (fields.Exists(field => field.Key == key))
            {
                continue;
            }

            fields.Add((key, tables.TryType('t', key, value, out var preferred) ? preferred : value));
        }

        fields.Sort(static (left, right) => string.CompareOrdinal(left.Key, right.Key));
        TransformFields.Clear();
        TransformFields.AddRange(fields);

        return this;
    }

    /// <summary>A <c>-u-</c> type in canonical form: an alias replaced, a subdivision alias replaced, <c>true</c> made implicit.</summary>
    private static string CanonicalType(JsIntlTables tables, string key, string type)
    {
        var canonical = tables.TryType('u', key, type, out var preferred) ? preferred : type;

        // A REGION OR SUBDIVISION VALUE IS A SUBDIVISION CODE, and CLDR's subdivision aliases
        // replace a deprecated one, by the first of several replacements.
        if (key is "rg" or "sd" && tables.Alias("subdivision", canonical) is { } replacement)
        {
            canonical = replacement.Split(' ')[0];
        }

        return canonical == "true" ? string.Empty : canonical;
    }

    /// <summary>
    /// UTS #35 Annex C's alias replacement over the language identifier: language aliases by rule,
    /// then script, region and variant aliases, until nothing changes; then the variants sorted.
    /// </summary>
    internal void ReplaceAliases(JsIntlTables tables)
    {
        // A BOUND, because the data's rules terminate and a defect in them must not loop.
        for (var round = 0; round < 16; round++)
        {
            var changed = ReplaceLanguage(tables);

            if (Script is { } script && tables.Alias("script", script) is { } scriptAlias)
            {
                Script = scriptAlias;
                changed = true;
            }

            if (Region is { } region && tables.Alias("region", region) is { } regionAlias)
            {
                Region = PickRegion(tables, regionAlias);
                changed = true;
            }

            for (var index = 0; index < Variants.Count; index++)
            {
                if (tables.Alias("variant", Variants[index]) is { } variantAlias)
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
    /// Applies the most specific language alias rule that matches, and answers whether one did. A
    /// rule names a language (or <c>und</c>) and perhaps a script, a region and variants; it matches
    /// when every field it names is in the tag, and it replaces what it matched with its replacement's
    /// fields, adding a replacement field the tag lacks.
    /// </summary>
    private bool ReplaceLanguage(JsIntlTables tables)
    {
        JsLocaleTag? best = null;
        JsLocaleTag? replacement = null;
        var bestRank = -1;

        foreach (var candidate in Candidates())
        {
            if (tables.LanguageAliases.TryGetValue(candidate, out var target) &&
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

        // A REPLACEMENT LANGUAGE OF `und` KEEPS THE TAG'S: `und-aaland` turns a variant into a region
        // and says nothing about the language.
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

        // A REPLACEMENT CARRYING PRIVATE USE - `i-default` is `en-x-i-default` - never reaches a
        // structurally valid tag, because its source is legacy syntax; nothing here adds one.
        return true;
    }

    /// <summary>
    /// The alias-table keys a rule matching this tag could have: its language or <c>und</c>, with or
    /// without its script and its region, and with none, one or two of its variants - the data's
    /// longest rule, <c>und-hepburn-heploc</c>, names two.
    /// </summary>
    private IEnumerable<string> Candidates()
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
    /// The one region of a replacement list: the region likely for this tag's language and script
    /// when the list holds it, and the first otherwise.
    /// </summary>
    private string PickRegion(JsIntlTables tables, string replacements)
    {
        var regions = replacements.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (regions.Length == 1)
        {
            return regions[0];
        }

        var probe = new JsLocaleTag { Language = Language, Script = Script };

        if (probe.Maximize(tables) && probe.Region is { } likely && Array.IndexOf(regions, likely) >= 0)
        {
            return likely;
        }

        return regions[0];
    }

    /// <summary>
    /// UTS #35's Add Likely Subtags over the language, script and region; answers whether the data
    /// held a match.
    /// </summary>
    internal bool Maximize(JsIntlTables tables)
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
            if (tables.Likely(key) is { } found && Parse(found) is { } result)
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
    /// UTS #35's Remove Likely Subtags: the shortest of the language, the language and region, and the
    /// language and script whose likely subtags are this tag's, its variants and extensions kept;
    /// whether the data held a match for the tag at all.
    /// </summary>
    internal bool Minimize(JsIntlTables tables)
    {
        var maximal = Parse(LanguageId())!;

        if (!maximal.Maximize(tables))
        {
            return false;
        }

        foreach (var (script, region) in new[] { ((string?)null, (string?)null), (null, maximal.Region), (maximal.Script, null) })
        {
            var trial = new JsLocaleTag { Language = maximal.Language, Script = script, Region = region };

            if (trial.Maximize(tables) && trial.Language == maximal.Language && trial.Script == maximal.Script && trial.Region == maximal.Region)
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
    public override string ToString()
    {
        var extensions = new SortedDictionary<char, string>(OtherExtensions);

        if (HasTransformExtension && (TransformLanguage is not null || TransformFields.Count != 0))
        {
            var t = new StringBuilder();

            if (TransformLanguage is not null)
            {
                t.Append(TransformLanguage.LanguageId().ToLowerInvariant());
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
    internal string WithoutUnicodeExtension()
    {
        var copy = Parse(ToString())!;
        copy.HasUnicodeExtension = false;
        copy.Attributes.Clear();
        copy.Keywords.Clear();
        return copy.ToString();
    }
}
