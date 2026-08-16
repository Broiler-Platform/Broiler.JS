using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Statements;
using Broiler.JavaScript.ExpressionCompiler.Core;

namespace Broiler.JavaScript.Parser;


partial class FastParser
{
    /// <summary>
    /// Expression Sequence represents a comma separated expressions
    /// terminated by new line or semi colon
    /// </summary>
    /// <param name="expressions"></param>
    /// <param name="endWith"></param>
    /// <param name="allowEmpty">
    /// Whether an empty sequence is a legal parse, as in a `for` head's omitted init,
    /// test or update clause. When it is, parsing nothing yields an
    /// <see cref="AstEmptyExpression"/> and still succeeds.
    /// </param>
    /// <returns>
    /// False when no expression was parsed and <paramref name="allowEmpty"/> was not
    /// requested — the caller needs a value and the source does not provide one, so it
    /// must report a syntax error. Reporting success there handed back an
    /// <see cref="AstEmptyExpression"/>, whose compiled type is `void`: `if(){}`,
    /// `while(){}` and `do{}while()` reached the IL generator and produced a branch on
    /// an empty evaluation stack, crashing with InvalidProgramException instead of
    /// throwing a SyntaxError.
    /// </returns>
    bool ExpressionSequence(out AstExpression expressions, TokenTypes endWith = TokenTypes.BracketEnd, bool allowEmpty = false)
        => ExpressionSequence(out expressions, out _, endWith, allowEmpty);

    /// <param name="terminator">
    /// The token the sequence was actually closed by — the <paramref name="endWith"/>, EOF or
    /// <c>;</c> token that was consumed to end it — or <see cref="TokenTypes.Empty"/> when the
    /// loop stopped without consuming any terminator at all (it ran into a <c>}</c>, a line
    /// terminator, or a token it simply cannot continue past).
    /// <para>
    /// A <c>for</c> head needs this to tell an <em>omitted</em> clause from a <em>missing</em>
    /// one: with <paramref name="allowEmpty"/> both produce an <see cref="AstEmptyExpression"/>
    /// and are otherwise indistinguishable. See <c>ForStatement</c>.
    /// </para>
    /// </param>
    bool ExpressionSequence(out AstExpression expressions, out TokenTypes terminator, TokenTypes endWith, bool allowEmpty)
    {
        var begin = stream.Current;
        var nodes = new Sequence<AstExpression>();
        // The loop clears `allowEmpty` after the first element (only the first may be
        // omitted), so the caller's request is captured before it is consumed.
        var emptyAllowed = allowEmpty;
        terminator = TokenTypes.Empty;

        do
        {
            if (allowEmpty && stream.Current.Type == TokenTypes.CurlyBracketEnd)
                break;

            // Same reason as below: a wholly omitted clause may still be written across lines
            // (`for (\n;\n;\n)`), so the newlines before its terminator are skipped first.
            if (emptyAllowed)
                stream.SkipNewLines();

            var emptyCandidate = stream.Current.Type;
            if (allowEmpty && stream.CheckAndConsumeAny(endWith, TokenTypes.EOF, TokenTypes.SemiColon))
            {
                terminator = emptyCandidate;
                break;
            }

            allowEmpty = false;

            if (Expression(out var node))
                nodes.Add(node);

            if (stream.CheckAndConsume(TokenTypes.Comma))
                continue;

            // Inside a `for` head — the only caller that permits an omitted clause — a line
            // terminator is insignificant. The head's semicolons are never supplied by ASI, so
            // a clause may be written on its own line, and `for (i = 0\n; i < 5\n; i++)` must
            // reach the `;` that follows the newline rather than stopping at the newline with
            // the `;` still unread. (Everywhere else the LineTerminator check below still ends
            // the sequence, which is what makes ASI work for an ordinary statement.)
            if (emptyAllowed)
                stream.SkipNewLines();

            var candidate = stream.Current.Type;
            if (stream.CheckAndConsumeAny(endWith, TokenTypes.EOF, TokenTypes.SemiColon))
            {
                terminator = candidate;
                break;
            }

            if (stream.Current.Type == TokenTypes.CurlyBracketEnd)
                break;

            if (stream.LineTerminator())
                break;

            break;
        } while (true);

        if (nodes.Count == 0 && !emptyAllowed)
        {
            expressions = null;
            return false;
        }

        expressions = nodes.Count switch
        {
            0 => new AstEmptyExpression(begin),
            1 => nodes[0],
            _ => new AstSequenceExpression(begin, PreviousToken, nodes),
        };

        return true;
    }

    bool WhileStatement(out AstStatement node)
    {
        var begin = stream.Current;

        stream.Consume();
        stream.Expect(TokenTypes.BracketStart);

        // `IterationStatement : while ( Expression ) Statement` — the condition is not
        // optional, so `while()` is a SyntaxError.
        if (!ExpressionSequence(out var test))
            throw stream.Unexpected();

        if (!NonDeclarativeStatement(out var statement))
            throw stream.Unexpected();

        node = new AstWhileStatement(begin, PreviousToken, test, statement);
        return true;
    }
}
