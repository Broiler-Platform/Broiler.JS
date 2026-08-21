using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Statements;
using System.Runtime.CompilerServices;

namespace Broiler.JavaScript.Ast;


public static class AstExpressionExtensions
{
    public static AstExpression Computed(this AstExpression left, AstExpression right) => new AstMemberExpression(left, right, true);

    public static AstExpression Member(this AstExpression left, AstExpression right, bool computed = false, bool coalesce = false, bool inOptionalChain = false) =>
        left == null ? right : new AstMemberExpression(left, right, computed, coalesce, inOptionalChain);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool Is<T>(this AstNode exp, FastNodeType type, out T value)
    {
        if (exp.Type == type && exp is T texp)
        {
            value = texp;
            return true;
        }

        value = default;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsFunction(this AstNode exp, out AstFunctionExpression value) => Is(exp, FastNodeType.FunctionExpression, out value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsExpressionStatement(this AstNode exp, out AstExpressionStatement value) => Is(exp, FastNodeType.ExpressionStatement, out value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsSpreadElement(this AstNode node, out AstSpreadElement value) => Is(node, FastNodeType.SpreadElement, out value);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsUnaryExpression(this AstNode exp, out AstUnaryExpression unary) => Is(exp, FastNodeType.UnaryExpression, out unary);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsBinaryExpression(this AstNode exp, out AstBinaryExpression binary) => Is(exp, FastNodeType.BinaryExpression, out binary);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsStringLiteral(this AstNode exp, out string value)
    {
        if (Is<AstLiteral>(exp, FastNodeType.Literal, out var literall))
        {
            if (literall.TokenType == TokenTypes.String)
            {
                value = literall.StringValue;
                return true;
            }
        }

        value = default;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsUIntLiteral(this AstNode exp, out uint value)
    {
        if (Is<AstLiteral>(exp, FastNodeType.Literal, out var literall))
        {
            if (literall.TokenType == TokenTypes.Number)
            {
                var n = literall.NumericValue;
                value = 0;

                if (n == 0)
                    return true;

                // Only 0 .. 2^32-2 are array indices, so a whole-number literal is a uint
                // key only when it fits that range. Without the upper bound a large literal
                // (e.g. `{ [1e55]: 'B' }`) casts out of range — (uint)1e55 wraps to 0 — and
                // silently collides with key 0; such keys must take the string path
                // (ToString(1e55) === "1e+55"), matching JSNumber.ToKey.
                if (n > 0 && n < uint.MaxValue && n % 1 == 0)
                {
                    value = (uint)n;
                    return true;
                }

                return false;
            }
        }

        value = default;
        return false;
    }

    /// <summary>
    /// A member access as it was written, from the leftmost token of its base through its own last
    /// token — <c>config.server.tls.enabled</c>, not the <c>tls.enabled</c> that
    /// <see cref="AstNode.Code"/> yields.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="AstNode.Code"/> spans <c>Start</c> to <c>End</c>, and a member expression's
    /// <c>Start</c> is its BASE'S LAST TOKEN — <c>AstMemberExpression</c> passes
    /// <c>target.End</c> — so <c>Code</c> on a chain gives only the final link. That is the right
    /// answer for pointing at the link and the wrong one for naming the expression a reader would
    /// search the file for, which is what a diagnostic wants. The spine is walked instead: down
    /// through bases and callees to the token the whole access starts at.
    /// </para>
    /// <para>
    /// <b>A COMPUTED access declines.</b> Its last token is the key, not the <c>]</c> that closes
    /// it — <c>a.b["x"]</c> would span <c>a.b["x"</c> — and the bracket cannot simply be taken
    /// from the next character, because a key is an arbitrary expression that may itself end in
    /// brackets (<c>a[b[c]]</c>) or contain one inside a string (<c>a["]"]</c>). A computed access
    /// nested INSIDE the span is unaffected: <c>list[0].name</c> ends at <c>name</c> and reads
    /// back whole.
    /// </para>
    /// <para>
    /// <b>A parenthesized base gives up rather than guessing.</b> The start token of
    /// <c>(a || b).c</c>'s base is <c>a</c>, not the <c>(</c> before it, so the span would read
    /// <c>a || b).c</c> — text that is not in the file and does not parse. Where whitespace or a
    /// comment sits between the parenthesis and the token there is no offset to recover it from
    /// either, so the case is declined: for a diagnostic, saying nothing beats saying something
    /// that cannot be found.
    /// </para>
    /// </remarks>
    public static StringSpan AccessCode(this AstMemberExpression member)
    {
        if (member == null || member.Computed)
            return default;

        var start = LeftmostToken(member);
        if (start == null)
            return default;

        var startSpan = start.Span;
        var endSpan = member.End.Type == TokenTypes.EOF ? member.Start.Span : member.End.Span;

        if (startSpan.Source == null
            || !ReferenceEquals(startSpan.Source, endSpan.Source))
        {
            return default;
        }

        var length = endSpan.Offset + endSpan.Length - startSpan.Offset;
        if (length <= 0)
            return default;

        return new StringSpan(startSpan.Source, startSpan.Offset, length);
    }

    /// <summary>
    /// The token an access begins at, or null when the spine passes through a parenthesized
    /// expression whose opening parenthesis no token accounts for.
    /// </summary>
    private static FastToken LeftmostToken(AstExpression expression)
    {
        while (true)
        {
            var next = expression switch
            {
                AstMemberExpression member => member.Object,
                AstCallExpression call => call.Callee,
                _ => null,
            };

            // The end of the spine: this node's own start token is the access's start. The
            // outermost node may itself have been wrapped — `(a.b.c)` — which changes nothing
            // about where its text begins, so only a wrapped BASE declines.
            if (next == null)
                return expression?.Start;

            if (next.WasParenthesized)
                return null;

            expression = next;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNumberLiteral(this AstNode exp, out double value)
    {
        if (Is<AstLiteral>(exp, FastNodeType.Literal, out var literall))
        {
            if (literall.TokenType == TokenTypes.Number)
            {
                value = literall.NumericValue;
                return true;
            }
        }

        value = default;
        return false;
    }
}
