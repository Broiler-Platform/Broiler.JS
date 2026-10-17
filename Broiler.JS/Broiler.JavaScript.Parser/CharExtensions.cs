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

    // IdentifierStartChar, IdentifierPartChar and WhiteSpace are Broiler.Unicode's, over this engine's
    // pinned Unicode 17.0.0 tables (milestone SJL-7.5): JSUnicode in Broiler.JavaScript.Runtime.
}
