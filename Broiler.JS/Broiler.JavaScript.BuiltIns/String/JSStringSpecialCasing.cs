using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Broiler.JavaScript.BuiltIns.String;

// ECMAScript String.prototype.toUpperCase/toLowerCase apply the Unicode Default Case
// Conversion, which uses the FULL case mappings — including the one-to-many expansions
// from SpecialCasing.txt (e.g. ß → SS, ﬁ → FI, İ → i̇). .NET's ToUpperInvariant/
// ToLowerInvariant only apply the simple (1:1) mappings, so those expansions are missed.
//
// This table holds exactly the *unconditional*, locale-independent SpecialCasing.txt
// entries whose result differs from the simple mapping. Simple 1:1 mappings are left to
// char.ToUpperInvariant/ToLowerInvariant. Conditional mappings (Final_Sigma) and
// language-sensitive ones (Turkic, Lithuanian) are handled elsewhere / not here.
internal static class JSStringSpecialCasing
{
    // Full-uppercase mappings (one-to-many). Keys are all BMP code points.
    private static readonly Dictionary<int, string> Upper = BuildUpper();

    // Full-lowercase mappings (one-to-many). The only unconditional, locale-independent
    // entry is LATIN CAPITAL LETTER I WITH DOT ABOVE → "i" + COMBINING DOT ABOVE.
    private static readonly Dictionary<int, string> Lower = new() { [0x0130] = "i̇" };

    // Invariant (non-locale) toUpperCase / toLowerCase.
    public static string ToUpper(string s) => Map(s, Upper, toUpper: true, culture: null);
    public static string ToLower(string s) => ToLowerCore(s, culture: null);

    // GREEK CAPITAL LETTER SIGMA and its two lowercase forms.
    private const char GreekCapitalSigma = 'Σ';
    private const char GreekSmallSigma = 'σ';
    private const char GreekSmallFinalSigma = 'ς';

    // Word_Break ∈ {MidLetter, MidNumLet, Single_Quote}: Case_Ignorable code points
    // that are not covered by the Mn/Me/Cf/Lm/Sk general-category test below. Plus
    // U+180E (MONGOLIAN VOWEL SEPARATOR), which some framework versions still report
    // as a space separator rather than Format.
    private static readonly HashSet<int> CaseIgnorablePunctuation = new()
    {
        0x0027, 0x002E, 0x003A, 0x00B7, 0x0387, 0x055F, 0x05F4, 0x0F0B, 0x180E,
        0x2018, 0x2019, 0x2024, 0x2027, 0xFE13, 0xFE52, 0xFE55, 0xFF07, 0xFF0E, 0xFF1A,
    };

    private static bool IsCaseIgnorable(int cp)
    {
        switch (CharUnicodeInfo.GetUnicodeCategory(cp))
        {
            case UnicodeCategory.NonSpacingMark:   // Mn
            case UnicodeCategory.EnclosingMark:    // Me
            case UnicodeCategory.Format:           // Cf
            case UnicodeCategory.ModifierLetter:   // Lm
            case UnicodeCategory.ModifierSymbol:   // Sk
                return true;
        }

        return CaseIgnorablePunctuation.Contains(cp);
    }

    // \p{Cased}: an upper/lower/title-case letter (the Other_Cased property points are
    // approximated by category — sufficient because any Other_Cased point that is also
    // Case_Ignorable, e.g. U+0345, is skipped by the ignorable test first).
    private static bool IsCased(int cp)
    {
        switch (CharUnicodeInfo.GetUnicodeCategory(cp))
        {
            case UnicodeCategory.UppercaseLetter:  // Lu
            case UnicodeCategory.LowercaseLetter:  // Ll
            case UnicodeCategory.TitlecaseLetter:  // Lt
                return true;
        }

        return false;
    }

    // Final_Sigma "Before C": there is a Cased character before C, with only
    // Case_Ignorable characters between it and C. Scans code points backwards from
    // index i, skipping Case_Ignorable; the first non-ignorable code point decides.
    private static bool BeforeIsCased(string s, int i)
    {
        var idx = i;
        while (idx > 0)
        {
            var prev = idx - 1;
            if (prev > 0 && char.IsLowSurrogate(s[prev]) && char.IsHighSurrogate(s[prev - 1]))
                prev--;

            var cp = char.ConvertToUtf32(s, prev);
            if (IsCaseIgnorable(cp))
            {
                idx = prev;
                continue;
            }

            return IsCased(cp);
        }

        return false;
    }

    // Final_Sigma "After C": there is a Cased character after C, with only
    // Case_Ignorable characters between. Returns true when such a character exists
    // (so the caller uses the final-sigma form only when this is false).
    private static bool AfterIsCased(string s, int j)
    {
        var idx = j;
        while (idx < s.Length)
        {
            var cp = char.ConvertToUtf32(s, idx);
            var len = cp > 0xFFFF ? 2 : 1;
            if (IsCaseIgnorable(cp))
            {
                idx += len;
                continue;
            }

            return IsCased(cp);
        }

        return false;
    }

    // Default lowercase with the SpecialCasing.txt Final_Sigma conditional mapping:
    // GREEK CAPITAL LETTER SIGMA lowercases to the final form ς when it is preceded by
    // a cased letter and NOT followed by one (ignoring case-ignorable characters),
    // otherwise to σ. All other characters use the unconditional lower mapping.
    private static string ToLowerCore(string s, CultureInfo culture)
    {
        if (string.IsNullOrEmpty(s) || s.IndexOf(GreekCapitalSigma) < 0)
            return Map(s, Lower, toUpper: false, culture);

        var sb = new StringBuilder(s.Length + 2);
        for (var i = 0; i < s.Length;)
        {
            var cp = char.ConvertToUtf32(s, i);
            var len = cp > 0xFFFF ? 2 : 1;
            if (cp == GreekCapitalSigma)
                sb.Append(BeforeIsCased(s, i) && !AfterIsCased(s, i + len) ? GreekSmallFinalSigma : GreekSmallSigma);
            else if (Lower.TryGetValue(cp, out var mapped))
                sb.Append(mapped);
            else
            {
                var one = char.ConvertFromUtf32(cp);
                sb.Append(culture != null ? culture.TextInfo.ToLower(one) : one.ToLowerInvariant());
            }

            i += len;
        }

        return sb.ToString();
    }

    // Locale toLocaleLowerCase: the culture-specific simple lowercase (Turkic İ → i,
    // etc.) with the locale-independent Final_Sigma conditional applied on top. Σ → σ
    // is length-preserving, so the contextual replacement can be done in place over the
    // lowered result using the original string for context.
    // tr (Turkish) and az (Azerbaijani) share the dotted/dotless-I casing behaviour.
    private static bool IsTurkic(CultureInfo culture)
    {
        var lang = culture?.TwoLetterISOLanguageName;
        return lang is "tr" or "az";
    }

    public static string ToLocaleLower(string s, CultureInfo culture)
    {
        // Turkic (tr/az) lowercasing applies the language-sensitive SpecialCasing.txt
        // rules with their After_I / Before_Dot context, which .NET's culture-aware
        // ToLower does not implement. Handle the whole string here instead.
        if (IsTurkic(culture))
            return TurkicToLower(s);

        var lowered = culture.TextInfo.ToLower(s);
        if (s.IndexOf(GreekCapitalSigma) < 0 || lowered.Length != s.Length)
            return lowered;

        var chars = lowered.ToCharArray();
        for (var i = 0; i < s.Length; i++)
        {
            if (s[i] == GreekCapitalSigma)
                chars[i] = BeforeIsCased(s, i) && !AfterIsCased(s, i + 1) ? GreekSmallFinalSigma : GreekSmallSigma;
        }

        return new string(chars);
    }

    // Canonical_Combining_Class == 230 (Above) code-point ranges (Unicode 17.0.0,
    // UnicodeData.txt field 3). Used by the Turkic After_I / Before_Dot context below to
    // tell a "may intervene" mark (ccc neither 0 nor 230) from a blocking Above mark.
    private const string CombiningAboveRanges =
        "300-314,33D-344,346,34A-34C,350-352,357,35B,363-36F,483-487,592-595,597-599,59C-5A1,5A8-5A9,5AB-5AC,5AF,5C4,610-617,653-654,657-65B,65D-65E,6D6-6DC,6DF-6E2,6E4,6E7-6E8,6EB-6EC,730,732-733,735-736,73A,73D,73F-741,743,745,747,749-74A,7EB-7F1,7F3,816-819,81B-823,825-827,829-82D,897-898,89C-89F,8CA-8CE,8D4-8E1,8E4-8E5,8E7-8E8,8EA-8EC,8F3-8F5,8F7-8F8,8FB-8FF,951,953-954,9FE,F82-F83,F86-F87,135D-135F,17DD,193A,1A17,1A75-1A7C,1AB0-1AB4,1ABB-1ABC,1AC1-1AC2,1AC5-1AC9,1ACB-1ADC,1AE0-1AE5,1AE7-1AEA,1B6B,1B6D-1B73,1CD0-1CD2,1CDA-1CDB,1CE0,1CF4,1CF8-1CF9,1DC0-1DC1,1DC3-1DC9,1DCB-1DCC,1DD1-1DF5,1DFB,1DFE,20D0-20D1,20D4-20D7,20DB-20DC,20E1,20E7,20E9,20F0,2CEF-2CF1,2DE0-2DFF,A66F,A674-A67D,A69E-A69F,A6F0-A6F1,A8E0-A8F1,AAB0,AAB2-AAB3,AAB7-AAB8,AABE-AABF,AAC1,FE20-FE26,FE2E-FE2F,10376-1037A,10A0F,10A38,10AE5,10D24-10D27,10D69-10D6D,10EAB-10EAC,10F48-10F4A,10F4C,10F82,10F84,11100-11102,11366-1136C,11370-11374,1145E,16B30-16B36,1D185-1D189,1D1AA-1D1AD,1D242-1D244,1E000-1E006,1E008-1E018,1E01B-1E021,1E023-1E024,1E026-1E02A,1E08F,1E130-1E136,1E2AE,1E2EC-1E2EF,1E4EF,1E5EE,1E6E3,1E6E6,1E6EE-1E6EF,1E6F5,1E944-1E949";

    private static readonly (int Lo, int Hi)[] CombiningAbove = ParseHexRanges(CombiningAboveRanges);

    private static (int Lo, int Hi)[] ParseHexRanges(string encoded)
    {
        var parts = encoded.Split(',');
        var result = new (int Lo, int Hi)[parts.Length];
        for (var i = 0; i < parts.Length; i++)
        {
            var span = parts[i].AsSpan();
            var dash = span.IndexOf('-');
            if (dash < 0)
            {
                var cp = ParseHex(span);
                result[i] = (cp, cp);
            }
            else
            {
                result[i] = (ParseHex(span[..dash]), ParseHex(span[(dash + 1)..]));
            }
        }
        return result;
    }

    private static int ParseHex(ReadOnlySpan<char> span)
    {
        var value = 0;
        foreach (var ch in span)
            value = (value << 4) + (ch <= '9' ? ch - '0' : (ch | 0x20) - 'a' + 10);
        return value;
    }

    private static bool IsCombiningAbove(int cp)
    {
        var ranges = CombiningAbove;
        var lo = 0;
        var hi = ranges.Length - 1;
        while (lo <= hi)
        {
            var mid = (lo + hi) >> 1;
            if (cp < ranges[mid].Lo) hi = mid - 1;
            else if (cp > ranges[mid].Hi) lo = mid + 1;
            else return true;
        }
        return false;
    }

    private static bool IsMark(int cp)
        => CharUnicodeInfo.GetUnicodeCategory(cp) is UnicodeCategory.NonSpacingMark
            or UnicodeCategory.SpacingCombiningMark or UnicodeCategory.EnclosingMark;

    // A character "may intervene" in the After_I / Before_Dot context iff its combining
    // class is neither 0 nor 230. A non-mark is a starter (ccc 0) and blocks; a mark
    // blocks only when its class is 230 (Above).
    private static bool IsContextTransparent(int cp) => IsMark(cp) && !IsCombiningAbove(cp);

    // SpecialCasing.txt Before_Dot: starting at <paramref name="j"/> (the index just
    // after a LATIN CAPITAL LETTER I), is the I followed by COMBINING DOT ABOVE (U+0307)
    // with only context-transparent characters intervening?
    private static bool IsBeforeDot(string s, int j)
    {
        while (j < s.Length)
        {
            var cp = char.ConvertToUtf32(s, j);
            if (cp == 0x0307)
                return true;
            if (!IsContextTransparent(cp))
                return false;
            j += cp > 0xFFFF ? 2 : 1;
        }
        return false;
    }

    // SpecialCasing.txt After_I: scanning backwards from <paramref name="k"/> (the index
    // of a COMBINING DOT ABOVE), is it preceded by a LATIN CAPITAL LETTER I (U+0049) with
    // only context-transparent characters intervening?
    private static bool IsAfterI(string s, int k)
    {
        var idx = k;
        while (idx > 0)
        {
            var prev = idx - 1;
            if (prev > 0 && char.IsLowSurrogate(s[prev]) && char.IsHighSurrogate(s[prev - 1]))
                prev--;

            var cp = char.ConvertToUtf32(s, prev);
            if (cp == 0x0049)
                return true;
            if (!IsContextTransparent(cp))
                return false;
            idx = prev;
        }
        return false;
    }

    // Language-sensitive lowercasing for Turkish/Azerbaijani (SpecialCasing.txt):
    //   U+0130 (İ)            -> "i"
    //   U+0049 (I)            -> "i" when Before_Dot, else U+0131 (dotless i)
    //   U+0307 (dot above)    -> removed when After_I, otherwise kept
    // Every other character lowercases with the locale-independent mapping (the Turkic
    // locales differ from the default only for the dotted/dotless I), with the
    // locale-independent Final_Sigma conditional applied to GREEK CAPITAL LETTER SIGMA.
    private static string TurkicToLower(string s)
    {
        if (string.IsNullOrEmpty(s))
            return s;

        var sb = new StringBuilder(s.Length + 1);
        var i = 0;
        while (i < s.Length)
        {
            var cp = char.ConvertToUtf32(s, i);
            var len = cp > 0xFFFF ? 2 : 1;

            switch (cp)
            {
                case 0x0130: // LATIN CAPITAL LETTER I WITH DOT ABOVE
                    sb.Append('i');
                    break;
                case 0x0049: // LATIN CAPITAL LETTER I
                    sb.Append(IsBeforeDot(s, i + 1) ? 'i' : 'ı');
                    break;
                case 0x0307: // COMBINING DOT ABOVE
                    if (!IsAfterI(s, i))
                        sb.Append('̇');
                    break;
                case GreekCapitalSigma:
                    sb.Append(BeforeIsCased(s, i) && !AfterIsCased(s, i + len) ? GreekSmallFinalSigma : GreekSmallSigma);
                    break;
                default:
                    // Locale-independent simple lowercase for every other character.
                    sb.Append(s.Substring(i, len).ToLowerInvariant());
                    break;
            }

            i += len;
        }

        return sb.ToString();
    }

    // Locale toLocaleUpperCase: the unconditional expansions (ß → SS, …) are
    // locale-independent, while the simple 1:1 mappings honour the culture (e.g.
    // Turkish i → İ). (The locale lowercase path keeps its existing behaviour — the
    // İ → "i̇" override is NOT locale-independent for Turkic locales, so it is applied
    // only by the non-locale ToLower above.)
    public static string ToLocaleUpper(string s, CultureInfo culture) => Map(s, Upper, toUpper: true, culture);

    private static string Map(string s, Dictionary<int, string> special, bool toUpper, CultureInfo culture)
    {
        if (string.IsNullOrEmpty(s))
            return s;

        // Fast path: no special-cased character present (the common case), so defer to
        // the framework's simple mapping. All special keys are BMP, so a UTF-16 scan
        // is sufficient to detect them.
        var found = false;
        foreach (var ch in s)
        {
            if (special.ContainsKey(ch))
            {
                found = true;
                break;
            }
        }

        if (!found)
        {
            if (culture != null)
                return toUpper ? culture.TextInfo.ToUpper(s) : culture.TextInfo.ToLower(s);
            return toUpper ? s.ToUpperInvariant() : s.ToLowerInvariant();
        }

        var sb = new StringBuilder(s.Length + 4);
        for (var i = 0; i < s.Length;)
        {
            var cp = char.ConvertToUtf32(s, i);
            var len = cp > 0xFFFF ? 2 : 1;
            if (special.TryGetValue(cp, out var mapped))
                sb.Append(mapped);
            else
            {
                var one = char.ConvertFromUtf32(cp);
                if (culture != null)
                    sb.Append(toUpper ? culture.TextInfo.ToUpper(one) : culture.TextInfo.ToLower(one));
                else
                    sb.Append(toUpper ? one.ToUpperInvariant() : one.ToLowerInvariant());
            }

            i += len;
        }

        return sb.ToString();
    }

    private static Dictionary<int, string> BuildUpper()
    {
        var map = new Dictionary<int, string>();

        void Add(int source, params int[] mapping)
        {
            var sb = new StringBuilder(mapping.Length);
            foreach (var cp in mapping)
                sb.Append((char)cp);
            map[source] = sb.ToString();
        }

        // Latin / German / ligatures
        Add(0x00DF, 0x0053, 0x0053);                  // ß  -> SS
        Add(0x0149, 0x02BC, 0x004E);                  // ŉ  -> ʼN
        Add(0x01F0, 0x004A, 0x030C);                  // ǰ  -> J̌
        Add(0x1E96, 0x0048, 0x0331);                  // ẖ  -> H̱
        Add(0x1E97, 0x0054, 0x0308);                  // ẗ  -> T̈
        Add(0x1E98, 0x0057, 0x030A);                  // ẘ  -> W̊
        Add(0x1E99, 0x0059, 0x030A);                  // ẙ  -> Y̊
        Add(0x1E9A, 0x0041, 0x02BE);                  // ẚ  -> Aʾ
        Add(0xFB00, 0x0046, 0x0046);                  // ﬀ  -> FF
        Add(0xFB01, 0x0046, 0x0049);                  // ﬁ  -> FI
        Add(0xFB02, 0x0046, 0x004C);                  // ﬂ  -> FL
        Add(0xFB03, 0x0046, 0x0046, 0x0049);          // ﬃ  -> FFI
        Add(0xFB04, 0x0046, 0x0046, 0x004C);          // ﬄ  -> FFL
        Add(0xFB05, 0x0053, 0x0054);                  // ﬅ  -> ST
        Add(0xFB06, 0x0053, 0x0054);                  // ﬆ  -> ST

        // Armenian
        Add(0x0587, 0x0535, 0x0552);                  // և  -> ԵՒ
        Add(0xFB13, 0x0544, 0x0546);                  // ﬓ  -> ՄՆ
        Add(0xFB14, 0x0544, 0x0535);                  // ﬔ  -> ՄԵ
        Add(0xFB15, 0x0544, 0x053B);                  // ﬕ  -> ՄԻ
        Add(0xFB16, 0x054E, 0x0546);                  // ﬖ  -> ՎՆ
        Add(0xFB17, 0x0544, 0x053D);                  // ﬗ  -> ՄԽ

        // Greek with combining marks (dialytika / tonos / perispomeni)
        Add(0x0390, 0x0399, 0x0308, 0x0301);          // ΐ
        Add(0x03B0, 0x03A5, 0x0308, 0x0301);          // ΰ
        Add(0x1F50, 0x03A5, 0x0313);
        Add(0x1F52, 0x03A5, 0x0313, 0x0300);
        Add(0x1F54, 0x03A5, 0x0313, 0x0301);
        Add(0x1F56, 0x03A5, 0x0313, 0x0342);
        Add(0x1FB6, 0x0391, 0x0342);
        Add(0x1FC6, 0x0397, 0x0342);
        Add(0x1FD2, 0x0399, 0x0308, 0x0300);
        Add(0x1FD3, 0x0399, 0x0308, 0x0301);
        Add(0x1FD6, 0x0399, 0x0342);
        Add(0x1FD7, 0x0399, 0x0308, 0x0342);
        Add(0x1FE2, 0x03A5, 0x0308, 0x0300);
        Add(0x1FE3, 0x03A5, 0x0308, 0x0301);
        Add(0x1FE4, 0x03A1, 0x0313);
        Add(0x1FE6, 0x03A5, 0x0342);
        Add(0x1FE7, 0x03A5, 0x0308, 0x0342);
        Add(0x1FF6, 0x03A9, 0x0342);

        // Greek iota-subscript (ypogegrammeni) -> base letter(s) + CAPITAL IOTA (0399)
        Add(0x1FB3, 0x0391, 0x0399);
        Add(0x1FBC, 0x0391, 0x0399);
        Add(0x1FC3, 0x0397, 0x0399);
        Add(0x1FCC, 0x0397, 0x0399);
        Add(0x1FF3, 0x03A9, 0x0399);
        Add(0x1FFC, 0x03A9, 0x0399);
        Add(0x1FB2, 0x1FBA, 0x0399);
        Add(0x1FB4, 0x0386, 0x0399);
        Add(0x1FC2, 0x1FCA, 0x0399);
        Add(0x1FC4, 0x0389, 0x0399);
        Add(0x1FF2, 0x1FFA, 0x0399);
        Add(0x1FF4, 0x038F, 0x0399);
        Add(0x1FB7, 0x0391, 0x0342, 0x0399);
        Add(0x1FC7, 0x0397, 0x0342, 0x0399);
        Add(0x1FF7, 0x03A9, 0x0342, 0x0399);

        // Greek alpha/eta/omega with breathing/accents + iota subscript (1F80-1FAF):
        // each maps to the corresponding non-subscript CAPITAL letter + CAPITAL IOTA.
        var bases = new[]
        {
            (0x1F80, 0x1F08), // ἀ-family (alpha) low rows -> 1F08..
            (0x1F90, 0x1F28), // η-family
            (0x1FA0, 0x1F68), // ω-family
        };
        foreach (var (lowStart, upStart) in bases)
        {
            for (var k = 0; k < 8; k++)
            {
                // 1F80-1F87 and the titlecase row 1F88-1F8F both uppercase to (base+k)+0399.
                Add(lowStart + k, upStart + k, 0x0399);
                Add(lowStart + 8 + k, upStart + k, 0x0399);
            }
        }

        return map;
    }
}
