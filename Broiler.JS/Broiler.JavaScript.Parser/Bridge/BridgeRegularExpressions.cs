#nullable enable

using Broiler.ECMAScript.Syntax;

namespace Broiler.JavaScript.Parser.Bridge;

/// <summary>
/// This engine's regular-expression validator behind the shared library's provider (milestone SJL-14.2, decision
/// D51): the check <c>FastScanner</c> makes of a literal, Broiler.Regex's validate entry since SJL-11.5, answered as the
/// library's verdict.
/// </summary>
/// <remarks>
/// <para>
/// Decision D51's order: SJL-11.5 lands before W2, so the provider wraps Broiler.Regex's validate entry
/// (<see cref="FastScanner.IsValidRegularExpressionLiteral"/>), as the scanner does. Until SJL-11.5 it answered the
/// scanner's earlier checks, the parenthesis-balance patch, the braced-escape decoding under <c>u</c> and <c>v</c>, and
/// the parser's own .NET-based validator, which SJL-11.5 deleted; the body the compiler reads is now the body as written.
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
        => FastScanner.IsValidRegularExpressionLiteral(pattern, flags)
            ? RegularExpressionVerdict.Valid
            : RegularExpressionVerdict.Invalid("regexp.literal.invalid", "/" + pattern + "/" + flags, 0, 0, inFlags: false);
}
