using System;
using Broiler.Regex.Ast;

namespace Broiler.Regex.Unicode;

/// <summary>
/// Membership tests for the built-in ECMAScript character-class escapes
/// (<c>\d \D \w \W \s \S</c>) and the entry point for Unicode property escapes
/// (<c>\p{…}</c>), per ECMA-262 §22.2.1 / Table 64.
/// </summary>
public static class UnicodeCharSets
{
    public static bool MatchesEscape(ClassEscape escape, int codePoint) => escape switch
    {
        ClassEscape.Digit => IsDigit(codePoint),
        ClassEscape.NonDigit => !IsDigit(codePoint),
        ClassEscape.Word => IsWord(codePoint),
        ClassEscape.NonWord => !IsWord(codePoint),
        ClassEscape.Space => IsSpace(codePoint),
        ClassEscape.NonSpace => !IsSpace(codePoint),
        _ => false,
    };

    /// <summary><c>\d</c> — exactly ASCII <c>0-9</c> (§22.2.1).</summary>
    public static bool IsDigit(int cp) => cp >= '0' && cp <= '9';

    /// <summary><c>\w</c> — ASCII letters, digits, and underscore (§22.2.1).</summary>
    public static bool IsWord(int cp)
        => (cp >= 'a' && cp <= 'z')
        || (cp >= 'A' && cp <= 'Z')
        || (cp >= '0' && cp <= '9')
        || cp == '_';

    /// <summary>
    /// <c>\s</c> — WhiteSpace ∪ LineTerminator (§22.2.1 / Table 36 + §12.2).
    /// This is the full ECMAScript white-space set, broader than .NET's <c>\s</c>.
    /// </summary>
    public static bool IsSpace(int cp) => cp switch
    {
        0x0009 or 0x000A or 0x000B or 0x000C or 0x000D => true, // tab, LF, VT, FF, CR
        0x0020 => true,                                          // space
        0x00A0 => true,                                          // no-break space
        0x1680 => true,                                          // ogham space mark
        >= 0x2000 and <= 0x200A => true,                         // en quad … hair space
        0x2028 or 0x2029 => true,                                // line / paragraph separator
        0x202F => true,                                          // narrow no-break space
        0x205F => true,                                          // medium mathematical space
        0x3000 => true,                                          // ideographic space
        0xFEFF => true,                                          // zero-width no-break space (BOM)
        _ => false,
    };

    /// <summary>
    /// Resolves a Unicode property escape such as <c>\p{Letter}</c> /
    /// <c>\p{Script=Greek}</c> to a membership predicate.
    /// </summary>
    /// <remarks>
    /// TODO: back this with the <c>Broiler.Unicode</c> property tables (the same
    /// data the translator's <c>TransformUnicodePropertyEscapes</c> uses). Until
    /// then it is a recognised-but-unimplemented stub so the parser can accept the
    /// syntax without silently mis-matching.
    /// </remarks>
    public static Func<int, bool> ResolveProperty(string name, string? value)
        => throw new NotSupportedException(
            $"Unicode property escape \\p{{{(value is null ? name : $"{name}={value}")}}} " +
            "is not yet implemented in Broiler.Regex (see README 'Known limitations').");
}
