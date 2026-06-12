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

        return char.GetUnicodeCategory(codePoint.FromCodePoint(), 0) switch
        {
            UnicodeCategory.UppercaseLetter
            or UnicodeCategory.LowercaseLetter
            or UnicodeCategory.TitlecaseLetter
            or UnicodeCategory.ModifierLetter
            or UnicodeCategory.OtherLetter
            or UnicodeCategory.LetterNumber => true,
            _ => false,
        };
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

        return char.GetUnicodeCategory(codePoint.FromCodePoint(), 0) switch
        {
            UnicodeCategory.NonSpacingMark
            or UnicodeCategory.SpacingCombiningMark
            or UnicodeCategory.DecimalDigitNumber
            or UnicodeCategory.ConnectorPunctuation => true,
            _ => false,
        };
    }
}
