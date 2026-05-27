using System.Globalization;
using System.Runtime.CompilerServices;

namespace Broiler.JavaScript.Parser;

public static class CharExtensions
{

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static string FromCodePoint(this int cp) => char.ConvertFromUtf32(cp);

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
                if (binary || octal)
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsIdentifierStart(this char ch)
    {
        return ((int)ch).IsIdentifierStart();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsIdentifierStart(this int codePoint)
    {
        if (codePoint is '$' or '_' or 0x2118 or 0x212E or 0x309B or 0x309C)
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
        if (codePoint.IsIdentifierStart())
            return true;

        if (codePoint is 0x200C or 0x200D or 0x00B7 or 0x0387 or 0x19DA)
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
