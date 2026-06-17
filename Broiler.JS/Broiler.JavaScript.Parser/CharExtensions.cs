using System.Globalization;
using System.Runtime.CompilerServices;

namespace Broiler.JavaScript.Parser;

public static class CharExtensions
{

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static string FromCodePoint(this int cp)
        // A `\u{…}` escape may name a lone surrogate code point (e.g. `\u{D83E}`),
        // which is a valid UTF-16 code unit in a JS string/template but rejected by
        // char.ConvertFromUtf32. Emit the single code unit directly for that range;
        // out-of-range code points (> U+10FFFF) still throw and surface as errors.
        => cp is >= 0xD800 and <= 0xDFFF ? ((char)cp).ToString() : char.ConvertFromUtf32(cp);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int HexValue(this char ch)
    {
        if (ch >= 'A')
        {
            if (ch >= 'a')
            {
                if (ch <= 'h')
                    return ch - 'a' + 10;
            }
            else if (ch <= 'H')
            {
                return ch - 'A' + 10;
            }
        }
        else if (ch <= '9')
        {
            return ch - '0';
        }

        return 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsDigitPart(this char ch, bool hex, bool binary, bool octal = false)
    {
        switch (ch)
        {
            case '_':
            case '0':
            case '1':
                return true;
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
                // 2-7 are valid octal (and decimal/hex) digits; only binary rejects them.
                if (binary)
                    return false;

                return true;
            case '8':
            case '9':
                if (binary)
                    return false;

                if (octal)
                    return false;

                return true;
            case 'a':
            case 'b':
            case 'c':
            case 'd':
            case 'e':
            case 'f':
            case 'A':
            case 'B':
            case 'C':
            case 'D':
            case 'E':
            case 'F':
                return hex;
        }
        return false;

    }

    // ECMAScript LineTerminator (§12.3): LF, CR, LINE SEPARATOR (U+2028) and
    // PARAGRAPH SEPARATOR (U+2029). These four — and only these — end a line for
    // the purposes of automatic semicolon insertion.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsLineTerminator(this char ch)
        => ch is '\n' or '\r' or '\u2028' or '\u2029';

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsIdentifierStart(this char ch)
    {
        return ((int)ch).IsIdentifierStart();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsIdentifierStart(this int codePoint)
    {
        if (codePoint < 0 || codePoint > 0x10FFFF || codePoint is >= 0xD800 and <= 0xDFFF)
            return false;

        // Other_ID_Start (grandfathered characters whose Unicode category is not a
        // letter category but which the spec keeps valid as identifier starts):
        // U+1885, U+1886 (Mongolian Ali Gali Baluda/Three Baluda — category Mn),
        // U+2118 (SCRIPT CAPITAL P), U+212E (ESTIMATED SYMBOL), U+309B, U+309C
        // (Katakana-Hiragana voiced/semi-voiced sound marks).
        if (codePoint is '$' or '_' or 0x1885 or 0x1886 or 0x2118 or 0x212E or 0x309B or 0x309C)
            return true;

        var isLetter = char.GetUnicodeCategory(codePoint.FromCodePoint(), 0) switch
        {
            UnicodeCategory.UppercaseLetter
            or UnicodeCategory.LowercaseLetter
            or UnicodeCategory.TitlecaseLetter
            or UnicodeCategory.ModifierLetter
            or UnicodeCategory.OtherLetter
            or UnicodeCategory.LetterNumber => true,
            _ => false,
        };

        // Unicode 17.0.0 (Sept 2025) assigned ID_Start code points that postdate the
        // Unicode version baked into this runtime's char.GetUnicodeCategory data, so they
        // come back unassigned above. Recognise them explicitly to keep
        // §sec-names-and-keywords in step with Unicode 17.0.0.
        return isLetter || InRanges(UnicodeV17IdStart, codePoint);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsIdentifierPart(this char ch)
    {
        return ((int)ch).IsIdentifierPart();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsIdentifierPart(this int codePoint)
    {
        if (codePoint < 0 || codePoint > 0x10FFFF || codePoint is >= 0xD800 and <= 0xDFFF)
            return false;

        if (codePoint.IsIdentifierStart())
            return true;

        // Other_ID_Continue (characters kept valid in identifier tails whose Unicode
        // category is not an identifier-continue category): U+00B7, U+0387 (Po),
        // U+1369..U+1371, U+19DA (No), U+200C/U+200D (ZWNJ/ZWJ, Cf), and U+30FB,
        // U+FF65 (Katakana middle dots, Po).
        if (codePoint is 0x200C or 0x200D or 0x00B7 or 0x0387 or 0x19DA or 0x30FB or 0xFF65)
            return true;

        if (codePoint >= 0x1369 && codePoint <= 0x1371)
            return true;

        var isPart = char.GetUnicodeCategory(codePoint.FromCodePoint(), 0) switch
        {
            UnicodeCategory.NonSpacingMark
            or UnicodeCategory.SpacingCombiningMark
            or UnicodeCategory.DecimalDigitNumber
            or UnicodeCategory.ConnectorPunctuation => true,
            _ => false,
        };

        // ID_Continue-only additions in Unicode 17.0.0 — the new combining marks and
        // digits. (Every new ID_Start char is also ID_Continue and is already covered by
        // the IsIdentifierStart check above.)
        return isPart || InRanges(UnicodeV17IdContinueExtra, codePoint);
    }

    // Unicode 17.0.0 ID_Start / ID_Continue code points not yet known to this runtime's
    // Unicode category data, as inclusive, sorted ranges. Regenerate from the caniunicode
    // test262 identifier tests (language/identifiers/{start,part}-unicode-17.0.0.js) when
    // the runtime's Unicode data advances to 17.0.0 or later, at which point these tables
    // become redundant and can be dropped.
    private static readonly (int Lo, int Hi)[] UnicodeV17IdStart =
    {
        (0x88F, 0x88F), (0xC5C, 0xC5C), (0xCDC, 0xCDC), (0xA7CE, 0xA7CF), (0xA7D2, 0xA7D2),
        (0xA7D4, 0xA7D4), (0xA7F1, 0xA7F1), (0x10940, 0x10959), (0x10EC5, 0x10EC7),
        (0x11DB0, 0x11DDB), (0x16EA0, 0x16EB8), (0x16EBB, 0x16ED3), (0x16FF2, 0x16FF6),
        (0x187F8, 0x187FF), (0x18D09, 0x18D1E), (0x18D80, 0x18DF2), (0x1E6C0, 0x1E6DE),
        (0x1E6E0, 0x1E6E2), (0x1E6E4, 0x1E6E5), (0x1E6E7, 0x1E6ED), (0x1E6F0, 0x1E6F4),
        (0x1E6FE, 0x1E6FF), (0x2B73A, 0x2B73F), (0x2CEA2, 0x2CEAD), (0x323B0, 0x33479),
    };

    private static readonly (int Lo, int Hi)[] UnicodeV17IdContinueExtra =
    {
        (0x1ACF, 0x1ADD), (0x1AE0, 0x1AEB), (0x10EFA, 0x10EFB), (0x11B60, 0x11B67),
        (0x11DE0, 0x11DE9), (0x1E6E3, 0x1E6E3), (0x1E6E6, 0x1E6E6), (0x1E6EE, 0x1E6EF),
        (0x1E6F5, 0x1E6F5),
    };

    private static bool InRanges((int Lo, int Hi)[] ranges, int codePoint)
    {
        int lo = 0, hi = ranges.Length - 1;
        while (lo <= hi)
        {
            int mid = (lo + hi) >> 1;
            if (codePoint < ranges[mid].Lo)
                hi = mid - 1;
            else if (codePoint > ranges[mid].Hi)
                lo = mid + 1;
            else
                return true;
        }
        return false;
    }
}
