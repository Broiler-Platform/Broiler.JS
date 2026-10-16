#nullable enable

using System.Runtime.CompilerServices;
using Broiler.Unicode.Algorithms;

namespace Broiler.JavaScript.Runtime.Unicode;

/// <summary>
/// The owner's Unicode algorithms over this engine's tables (milestone SJL-7.5): one instance of each,
/// built the first time it is read and shared by every reader, since each is immutable and safe to share
/// between threads. The parser, the runtime and the built-ins reach them here.
/// </summary>
/// <remarks>
/// Each instance sits in a holder of its own, so that the first identifier a script scans builds the
/// property tables alone, and not the casing or normalization tables. The lexical classes of the 128 ASCII
/// code points are a cache of the owner's answers, read from it when the lexical holder is built, never a
/// list of this engine's: a test holds the cache equal to the owner on every one of them.
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=JSD-0074 SJL-7.5 adapter; IP=Low; Security=Medium; Resources=1; Fingerprint=TBF
// Broiler-Falsified-If: an identifier or white-space answer here differs from Broiler.Unicode's EcmaScriptLexical for any code point
// Broiler-Human:        PENDING
internal static class JSUnicode
{
    /// <summary>This engine's tables, as the owner's algorithms read them.</summary>
    internal static IUnicodeTables Tables => JSUnicodeTables.Instance;

    /// <summary>Exact property lookup (ECMA-262 22.2.2.9, UnicodeMatchProperty and UnicodeMatchPropertyValue).</summary>
    internal static UnicodeProperties Properties => PropertiesHolder.Value;

    /// <summary>The lexical classes: IdentifierStartChar, IdentifierPartChar and WhiteSpace.</summary>
    internal static EcmaScriptLexical Lexical => LexicalHolder.Value;

    /// <summary>Default and language-sensitive case mapping.</summary>
    internal static UnicodeCaseMapping CaseMapping => CaseMappingHolder.Value;

    /// <summary>The four normalization forms.</summary>
    internal static UnicodeNormalization Normalization => NormalizationHolder.Value;

    /// <summary>The properties of strings the <c>v</c> flag admits.</summary>
    internal static UnicodeStringProperties StringProperties => StringPropertiesHolder.Value;

    /// <summary>Whether <paramref name="codePoint"/> is an IdentifierStartChar: ID_Start, <c>$</c> or <c>_</c>.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsIdentifierStart(int codePoint) =>
        (uint)codePoint < 0x80
            ? Has(LexicalHolder.AsciiStart, codePoint)
            : LexicalHolder.Value.IsIdentifierStart(codePoint);

    /// <summary>Whether <paramref name="codePoint"/> is an IdentifierPartChar: ID_Continue, <c>$</c>, ZWNJ or ZWJ.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsIdentifierPart(int codePoint) =>
        (uint)codePoint < 0x80
            ? Has(LexicalHolder.AsciiPart, codePoint)
            : LexicalHolder.Value.IsIdentifierPart(codePoint);

    /// <summary>
    /// Whether <paramref name="codePoint"/> is WhiteSpace: U+0009, U+000B, U+000C, U+FEFF or a
    /// Space_Separator. A line terminator is not, and neither is U+0085.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsWhiteSpace(int codePoint) =>
        (uint)codePoint < 0x80
            ? Has(LexicalHolder.AsciiWhiteSpace, codePoint)
            : LexicalHolder.Value.IsWhiteSpace(codePoint);

    /// <summary>Whether <paramref name="codePoint"/> is a LineTerminator: LF, CR, U+2028 or U+2029.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsLineTerminator(int codePoint) => EcmaScriptLexical.IsLineTerminator(codePoint);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool Has(in AsciiBits bits, int codePoint) =>
        ((codePoint < 64 ? bits.Low >> codePoint : bits.High >> (codePoint - 64)) & 1) != 0;

    /// <summary>One bit per ASCII code point.</summary>
    internal readonly struct AsciiBits(ulong low, ulong high)
    {
        internal readonly ulong Low = low;

        internal readonly ulong High = high;

        internal bool Contains(int codePoint) => (uint)codePoint < 0x80 && Has(this, codePoint);

        internal static AsciiBits Of(System.Func<int, bool> member)
        {
            ulong low = 0, high = 0;
            for (var codePoint = 0; codePoint < 0x80; codePoint++)
            {
                if (!member(codePoint))
                    continue;

                if (codePoint < 64)
                    low |= 1UL << codePoint;
                else
                    high |= 1UL << (codePoint - 64);
            }

            return new AsciiBits(low, high);
        }
    }

    private static class PropertiesHolder
    {
        internal static readonly UnicodeProperties Value = new(JSUnicodeTables.Instance);
    }

    internal static class LexicalHolder
    {
        internal static readonly EcmaScriptLexical Value = new(PropertiesHolder.Value);

        // The owner's answers for U+0000 to U+007F, read once.
        internal static readonly AsciiBits AsciiStart = AsciiBits.Of(Value.IsIdentifierStart);

        internal static readonly AsciiBits AsciiPart = AsciiBits.Of(Value.IsIdentifierPart);

        internal static readonly AsciiBits AsciiWhiteSpace = AsciiBits.Of(Value.IsWhiteSpace);
    }

    private static class CaseMappingHolder
    {
        internal static readonly UnicodeCaseMapping Value = new(JSUnicodeTables.Instance);
    }

    private static class NormalizationHolder
    {
        internal static readonly UnicodeNormalization Value = new(JSUnicodeTables.Instance);
    }

    private static class StringPropertiesHolder
    {
        internal static readonly UnicodeStringProperties Value = new(JSUnicodeTables.Instance);
    }
}
