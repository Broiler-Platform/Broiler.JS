// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   22
// Annotated:        22/22
// Exempt:           6
// Human-reviewed:   0/22
// IP risk:          Low
// Security risk:    Medium
// Criteria:         0/0
// Resource impact:  1/10 max
// Unverified:       22
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Generic;

namespace Broiler.ECMAScript.Intl;

/// <summary>
/// A well-formed Unicode BCP 47 locale identifier, parsed: ECMA-402's IsWellFormedLanguageTag
/// (<c>sec-iswellformedlanguagetag</c>) and CanonicalizeUnicodeLocaleId (<c>sec-canonicalizeunicodelocaleid</c>) over UTS #35
/// Part 1's <c>unicode_locale_id</c> and its Annex C, and UTS #35's Add and Remove Likely Subtags, over the locale data an
/// engine hands in (Broiler.VM's JSD-0082, sub-area (a), rows L1 to L12).
/// </summary>
/// <remarks>
/// <para>
/// <b>Immutable.</b> Every operation answers a new identifier, so one may be shared between threads (convention 10).
/// </para>
/// <para>
/// <b>Ported from Broiler.VM's <c>JsLocaleTag</c></b> at its SJL-9 rule commit <c>4cf9ca44</c>, with ASCII casing for its
/// culture-invariant calls (decision D19) and row L9's correction: a subdivision alias whose replacement is a region is
/// written <c>axzzzz</c>, as UTS #35 Annex C writes it (decision D32).
/// </para>
/// <para>
/// <b>Metering</b> (decision D18, convention 4). Every operation whose work grows with what it is given takes the caller's
/// <see cref="WorkCharge"/> and charges before the work: one unit per UTF-16 code unit of the tag or String it reads, and,
/// where it canonicalises, one unit per alias-table key each round of alias replacement looks up, a number that grows with
/// the square of the tag's variants (UTS #35 Annex C's language rules name up to two). The grammar predicates of a single
/// subtag read at most eight code units and take none, and so do the members that read an identifier already made
/// (its parts, keywords and <see cref="UnicodeExtensionValue"/>), whose making was charged. Broiler.VM charges nothing
/// there (row L43) and passes no meter; Broiler.JS passes none.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-iswellformedlanguagetag sec-canonicalizeunicodelocaleid, UTS35 Add and Remove Likely Subtags; IP=Low; Security=Medium; Resources=0; Fingerprint=24BEA8
// Broiler-Human:        PENDING
public sealed class LocaleId
{
    private readonly LocaleTag tag;

    private readonly string text;

    // Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-iswellformedlanguagetag sec-canonicalizeunicodelocaleid, UTS35 Add and Remove Likely Subtags; IP=Low; Security=Low; Resources=1; Fingerprint=BF2127
    // Broiler-Human:        PENDING
    private LocaleId(LocaleTag tag)
    {
        this.tag = tag;
        text = tag.ToString();
    }

    /// <summary>The language subtag, lower case (ECMA-402 GetLocaleLanguage).</summary>
    public string Language => tag.Language;

    /// <summary>The script subtag in title case, or <see langword="null"/> (ECMA-402 GetLocaleScript).</summary>
    public string? Script => tag.Script;

    /// <summary>The region subtag in upper case, or <see langword="null"/> (ECMA-402 GetLocaleRegion).</summary>
    public string? Region => tag.Region;

    /// <summary>The variant subtags, lower case (ECMA-402 GetLocaleVariants).</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-iswellformedlanguagetag sec-canonicalizeunicodelocaleid, UTS35 Add and Remove Likely Subtags; IP=Low; Security=Low; Resources=0; Fingerprint=84BACB
    // Broiler-Human:        PENDING
    public IReadOnlyList<string> Variants => tag.Variants.AsReadOnly();

    /// <summary>The <c>unicode_language_id</c> alone, without extensions or private use (ECMA-402 GetLocaleBaseName).</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-iswellformedlanguagetag sec-canonicalizeunicodelocaleid, UTS35 Add and Remove Likely Subtags; IP=Low; Security=Low; Resources=0; Fingerprint=52444F
    // Broiler-Human:        PENDING
    public string BaseName => tag.LanguageId();

    /// <summary>Whether the identifier has a <c>-u-</c> extension sequence with an attribute or a keyword.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-iswellformedlanguagetag sec-canonicalizeunicodelocaleid, UTS35 Add and Remove Likely Subtags; IP=Low; Security=Low; Resources=0; Fingerprint=015161
    // Broiler-Human:        PENDING
    public bool HasUnicodeExtension => tag.HasUnicodeExtension && (tag.Attributes.Count != 0 || tag.Keywords.Count != 0);

    /// <summary>The <c>-u-</c> keywords, in the order written (sorted by key once canonicalised); a keyword's value is empty for <c>true</c> left implicit.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-iswellformedlanguagetag sec-canonicalizeunicodelocaleid, UTS35 Add and Remove Likely Subtags; IP=Low; Security=Low; Resources=0; Fingerprint=F94C3B
    // Broiler-Human:        PENDING
    public IReadOnlyList<LocaleKeyword> Keywords => tag.Keywords.ConvertAll(static k => new LocaleKeyword(k.Key, k.Type)).AsReadOnly();

    /// <summary>
    /// The value of the <c>-u-</c> keyword <paramref name="key"/> (ECMA-402 UnicodeExtensionValue): <see langword="null"/> when
    /// the identifier has no such keyword, and the empty string when its value is <c>true</c> left implicit.
    /// </summary>
    /// <param name="key">The key, lower case.</param>
    // Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-iswellformedlanguagetag sec-canonicalizeunicodelocaleid, UTS35 Add and Remove Likely Subtags; IP=Low; Security=Low; Resources=1; Fingerprint=1AD2FB
    // Broiler-Human:        PENDING
    public string? UnicodeExtensionValue(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        var entry = tag.Keywords.FindIndex(pair => pair.Key == key);
        return entry >= 0 ? tag.Keywords[entry].Type : null;
    }

    /// <summary>
    /// Parses a tag (ECMA-402 IsWellFormedLanguageTag): <c>unicode_locale_id</c> without the backwards-compatible syntax, in
    /// ASCII alone (row L2), with no duplicate variant, singleton or <c>tlang</c> variant.
    /// </summary>
    /// <param name="text">The tag, as the caller's ToString gave it.</param>
    /// <param name="locale">The parsed identifier, in the case and order written, or <see langword="null"/>.</param>
    /// <param name="charge">The caller's meter: one unit per code unit of <paramref name="text"/>, charged before parsing.</param>
    /// <returns><see cref="IntlStatus.Completed"/>, or <see cref="IntlStatus.InvalidLanguageTag"/>, which an engine throws as a <c>RangeError</c>.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-iswellformedlanguagetag sec-canonicalizeunicodelocaleid, UTS35 Add and Remove Likely Subtags; IP=Low; Security=Medium; Resources=1; Fingerprint=8D4270
    // Broiler-Human:        PENDING
    public static IntlStatus TryParse(string text, out LocaleId? locale, WorkCharge? charge = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        charge?.Invoke((ulong)text.Length);

        if (LocaleTag.Parse(text) is { } parsed)
        {
            locale = new LocaleId(parsed);
            return IntlStatus.Completed;
        }

        locale = null;
        return IntlStatus.InvalidLanguageTag;
    }

    /// <summary>
    /// Parses a tag and canonicalises it: CanonicalizeLocaleList's step for one element (<c>sec-canonicalizelocalelist</c>,
    /// steps 7.c.v to 7.c.vii) once the engine has the element's String.
    /// </summary>
    /// <param name="text">The tag.</param>
    /// <param name="data">The locale data whose aliases and BCP 47 types canonicalisation reads.</param>
    /// <param name="canonical">The canonical tag, or the empty string when the tag is not well-formed.</param>
    /// <param name="charge">The caller's meter: one unit per code unit of <paramref name="text"/>, charged before parsing, and the alias-table keys canonicalisation looks up.</param>
    /// <returns><see cref="IntlStatus.Completed"/>, or <see cref="IntlStatus.InvalidLanguageTag"/>.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-iswellformedlanguagetag sec-canonicalizeunicodelocaleid, UTS35 Add and Remove Likely Subtags; IP=Low; Security=Medium; Resources=1; Fingerprint=75B8A0
    // Broiler-Human:        PENDING
    public static IntlStatus TryCanonicalize(string text, ILocaleData data, out string canonical, WorkCharge? charge = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(data);
        charge?.Invoke((ulong)text.Length);

        if (LocaleTag.Parse(text) is { } parsed)
        {
            canonical = parsed.Canonicalize(data, charge).ToString();
            return IntlStatus.Completed;
        }

        canonical = string.Empty;
        return IntlStatus.InvalidLanguageTag;
    }

    /// <summary>This identifier canonicalised (ECMA-402 CanonicalizeUnicodeLocaleId): aliases replaced, extensions canonicalised and ordered, in canonical case.</summary>
    /// <param name="data">The locale data whose aliases and BCP 47 types canonicalisation reads.</param>
    /// <param name="charge">The caller's meter: one unit per code unit of this identifier's text, charged before the work, and the alias-table keys canonicalisation looks up.</param>
    // Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-iswellformedlanguagetag sec-canonicalizeunicodelocaleid, UTS35 Add and Remove Likely Subtags; IP=Low; Security=Medium; Resources=1; Fingerprint=553A3F
    // Broiler-Human:        PENDING
    public LocaleId Canonicalize(ILocaleData data, WorkCharge? charge = null)
    {
        ArgumentNullException.ThrowIfNull(data);
        charge?.Invoke((ulong)text.Length);
        return new LocaleId(tag.Clone().Canonicalize(data, charge));
    }

    /// <summary>
    /// UTS #35's Add Likely Subtags over the language, script and region (<c>Intl.Locale.prototype.maximize</c>), with
    /// <c>Zzzz</c> and <c>ZZ</c> removed first; the variants and extensions are kept.
    /// </summary>
    /// <param name="data">The locale data whose likely subtags it reads.</param>
    /// <param name="found">Whether the data held likely subtags for the identifier; when not, the answer is this identifier with <c>Zzzz</c> and <c>ZZ</c> removed.</param>
    /// <param name="charge">The caller's meter: one unit per code unit of this identifier's text, charged before the work.</param>
    // Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-iswellformedlanguagetag sec-canonicalizeunicodelocaleid, UTS35 Add and Remove Likely Subtags; IP=Low; Security=Low; Resources=1; Fingerprint=85FEBF
    // Broiler-Human:        PENDING
    public LocaleId Maximize(ILocaleData data, out bool found, WorkCharge? charge = null)
    {
        ArgumentNullException.ThrowIfNull(data);
        charge?.Invoke((ulong)text.Length);
        var copy = tag.Clone();
        found = copy.Maximize(data);
        return new LocaleId(copy);
    }

    /// <summary>UTS #35's Remove Likely Subtags (<c>Intl.Locale.prototype.minimize</c>); the variants and extensions are kept.</summary>
    /// <param name="data">The locale data whose likely subtags it reads.</param>
    /// <param name="found">Whether the data held likely subtags for the identifier; when not, the answer is this identifier.</param>
    /// <param name="charge">The caller's meter: one unit per code unit of this identifier's text, charged before the work.</param>
    // Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-iswellformedlanguagetag sec-canonicalizeunicodelocaleid, UTS35 Add and Remove Likely Subtags; IP=Low; Security=Low; Resources=1; Fingerprint=B3F506
    // Broiler-Human:        PENDING
    public LocaleId Minimize(ILocaleData data, out bool found, WorkCharge? charge = null)
    {
        ArgumentNullException.ThrowIfNull(data);
        charge?.Invoke((ulong)text.Length);
        var copy = tag.Clone();
        found = copy.Minimize(data);
        return new LocaleId(copy);
    }

    /// <summary>This identifier without its <c>-u-</c> extension sequence (ECMA-402's locale "with any Unicode locale extension sequences removed").</summary>
    /// <param name="charge">The caller's meter: one unit per code unit of this identifier's text, charged before the work.</param>
    // Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-iswellformedlanguagetag sec-canonicalizeunicodelocaleid, UTS35 Add and Remove Likely Subtags; IP=Low; Security=Low; Resources=1; Fingerprint=5F522F
    // Broiler-Human:        PENDING
    public LocaleId WithoutUnicodeExtension(WorkCharge? charge = null)
    {
        charge?.Invoke((ulong)text.Length);
        return new(LocaleTag.Parse(tag.WithoutUnicodeExtension())!);
    }

    /// <summary>The identifier's text: its subtags in their order and case, extensions in singleton order and private use last.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-iswellformedlanguagetag sec-canonicalizeunicodelocaleid, UTS35 Add and Remove Likely Subtags; IP=Low; Security=Low; Resources=1; Fingerprint=BEDD42
    // Broiler-Human:        PENDING
    public override string ToString() => text;

    /// <summary>Whether a String is a <c>unicode_language_subtag</c>: two, three or five to eight ASCII letters (UTS #35 Part 1, section 3.2).</summary>
    /// <param name="value">The String.</param>
    // Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-iswellformedlanguagetag sec-canonicalizeunicodelocaleid, UTS35 Add and Remove Likely Subtags; IP=Low; Security=Low; Resources=1; Fingerprint=FDD02A
    // Broiler-Human:        PENDING
    public static bool IsUnicodeLanguageSubtag(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return LocaleTag.IsLanguage(value);
    }

    /// <summary>Whether a String is a <c>unicode_script_subtag</c>: four ASCII letters.</summary>
    /// <param name="value">The String.</param>
    // Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-iswellformedlanguagetag sec-canonicalizeunicodelocaleid, UTS35 Add and Remove Likely Subtags; IP=Low; Security=Low; Resources=1; Fingerprint=5987E9
    // Broiler-Human:        PENDING
    public static bool IsUnicodeScriptSubtag(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return LocaleTag.IsScript(value);
    }

    /// <summary>Whether a String is a <c>unicode_region_subtag</c>: two ASCII letters or three ASCII digits.</summary>
    /// <param name="value">The String.</param>
    // Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-iswellformedlanguagetag sec-canonicalizeunicodelocaleid, UTS35 Add and Remove Likely Subtags; IP=Low; Security=Low; Resources=1; Fingerprint=837AEB
    // Broiler-Human:        PENDING
    public static bool IsUnicodeRegionSubtag(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return LocaleTag.IsRegion(value);
    }

    /// <summary>Whether a String is a <c>unicode_variant_subtag</c>: five to eight ASCII letters and digits, or four beginning with a digit.</summary>
    /// <param name="value">The String.</param>
    // Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-iswellformedlanguagetag sec-canonicalizeunicodelocaleid, UTS35 Add and Remove Likely Subtags; IP=Low; Security=Low; Resources=1; Fingerprint=93118A
    // Broiler-Human:        PENDING
    public static bool IsUnicodeVariantSubtag(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return LocaleTag.IsVariant(value);
    }

    /// <summary>
    /// Whether a String is matched by UTS #35's <c>type</c> nonterminal: subtags of three to eight ASCII letters and digits
    /// joined by <c>-</c>. The constructors' "is not well-formed" checks of a <c>calendar</c>, <c>collation</c> or
    /// <c>numberingSystem</c> option (rows L13 and L14).
    /// </summary>
    /// <param name="value">The option's String.</param>
    /// <param name="charge">The caller's meter: one unit per code unit of <paramref name="value"/>, charged before the check.</param>
    // Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-iswellformedlanguagetag sec-canonicalizeunicodelocaleid, UTS35 Add and Remove Likely Subtags; IP=Low; Security=Low; Resources=1; Fingerprint=B825FB
    // Broiler-Human:        PENDING
    public static bool IsUnicodeType(string value, WorkCharge? charge = null)
    {
        ArgumentNullException.ThrowIfNull(value);
        charge?.Invoke((ulong)value.Length);
        return LocaleTag.IsTypeSequence(value);
    }

    /// <summary>ECMA-402's ASCII-lowercase (Case Sensitivity and Case Mapping): A to Z lowered, and every other code unit as it is.</summary>
    /// <param name="value">The String.</param>
    /// <param name="charge">The caller's meter: one unit per code unit of <paramref name="value"/>, charged before the work.</param>
    // Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-iswellformedlanguagetag sec-canonicalizeunicodelocaleid, UTS35 Add and Remove Likely Subtags; IP=Low; Security=Low; Resources=1; Fingerprint=9E1A85
    // Broiler-Human:        PENDING
    public static string AsciiLowercase(string value, WorkCharge? charge = null)
    {
        ArgumentNullException.ThrowIfNull(value);
        charge?.Invoke((ulong)value.Length);
        return LocaleTag.AsciiLower(value);
    }

    /// <summary>The internal form, for the library's other operations.</summary>
    internal LocaleTag Tag => tag;

    /// <summary>The identifier of a tag the library has parsed, or <see langword="null"/>.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-iswellformedlanguagetag sec-canonicalizeunicodelocaleid, UTS35 Add and Remove Likely Subtags; IP=Low; Security=Low; Resources=1; Fingerprint=CA5320
    // Broiler-Human:        PENDING
    internal static LocaleId? From(string text) => LocaleTag.Parse(text) is { } parsed ? new LocaleId(parsed) : null;
}

/// <summary>A <c>-u-</c> keyword of a locale identifier (UTS #35 Part 1, <c>keyword</c>): its key and its value, empty for <c>true</c> left implicit.</summary>
/// <param name="Key">The key, two characters, lower case.</param>
/// <param name="Value">The value's subtags joined by <c>-</c>, lower case; empty for <c>true</c> left implicit.</param>
// Broiler-AI:           Origin=AI; Spec=UTS35 Part 1 keyword; IP=Low; Security=Low; Resources=0; Fingerprint=A3C711
// Broiler-Human:        PENDING
public readonly record struct LocaleKeyword(string Key, string Value);
