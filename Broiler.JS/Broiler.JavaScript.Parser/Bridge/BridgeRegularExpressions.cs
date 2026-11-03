#nullable enable

using Broiler.ECMAScript.Syntax;

namespace Broiler.JavaScript.Parser.Bridge;

/// <summary>
/// This engine's regular-expression validator behind the shared library's provider (milestone SJL-14.2, decision
/// D51): the checks <c>FastScanner</c> makes of a literal today - the parenthesis-balance patch, the braced-escape
/// decoding under <c>u</c> and <c>v</c>, and <see cref="RegExpValidator"/> - answered as the library's verdict.
/// </summary>
/// <remarks>
/// <para>
/// Until SJL-11.5 the validator is the engine's own, as decision D51 has it; once SJL-11.5 has landed the provider
/// wraps Broiler.Regex's validate entry instead, and at the cut-over (W2) the scanner's copies of these checks go.
/// </para>
/// <para>
/// The validator tells conditions apart by nothing but its answer, so a refusal is the library's
/// <c>regexp.literal.invalid</c> (catalogue row EE0019) with the literal as written (<c>/body/flags</c>) as its
/// argument and code 0, at the literal's start; the message table renders it with the literal's position. Where <c>FastScanner</c> read an invalid literal as a division (the fallback, row S9), the
/// library refuses it: correction K5.
/// </para>
/// </remarks>
internal sealed class BridgeRegularExpressions : IRegularExpressionValidator
{
    /// <summary>The one validator: it holds no state.</summary>
    internal static BridgeRegularExpressions Instance { get; } = new();

    private BridgeRegularExpressions()
    {
    }

    /// <inheritdoc />
    public RegularExpressionVerdict Validate(string pattern, string flags)
        => Cooked(pattern, flags) is { } cooked && RegExpValidator.IsValid(cooked, flags)
            ? RegularExpressionVerdict.Valid
            : RegularExpressionVerdict.Invalid("regexp.literal.invalid", "/" + pattern + "/" + flags, 0, 0, inFlags: false);

    /// <summary>
    /// The body the compiler reads from the literal's token, as <c>FastScanner</c> gives it: the body as written, its
    /// braced escapes decoded under <c>u</c> or <c>v</c>; null where the parenthesis patch or the decoding refuses it.
    /// </summary>
    internal static string? Cooked(string pattern, string flags)
    {
        if (!ParenthesesBalance(pattern))
            return null;

        if (pattern.Contains("\\u{", System.StringComparison.Ordinal) && (flags.Contains('u') || flags.Contains('v')))
            return FastScanner.TryDecodeBracedUnicodeEscapes(pattern, out var decoded) ? decoded : null;

        return pattern;
    }

    // FastScanner's BROILER-PATCH (ES3 15.10.1): a `)` outside a character class with no `(` before it, or a `(`
    // never closed, refuses the literal. Kept to the letter, the patch's reading of `[]` included.
    private static bool ParenthesesBalance(string pattern)
    {
        var depth = 0;
        var inClass = false;
        for (var i = 0; i < pattern.Length; i++)
        {
            var c = pattern[i];
            if (c == '\\' && i + 1 < pattern.Length)
            {
                i++;
                continue;
            }

            if (inClass)
            {
                if (c == ']')
                    inClass = false;
                continue;
            }

            if (c == '[')
            {
                if (i + 1 < pattern.Length && pattern[i + 1] == ']')
                {
                    i++;
                    continue;
                }

                inClass = true;
                continue;
            }

            if (c == '(')
                depth++;
            if (c == ')' && --depth < 0)
                return false;
        }

        return depth == 0;
    }
}
