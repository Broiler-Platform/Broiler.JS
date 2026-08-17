
using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.ExpressionCompiler.Core;

namespace Broiler.JavaScript.Parser;

partial class FastParser
{
    /// <summary>
    /// ArrayExpression does not break with line terminator. It is used by method parameters and array initialization
    /// </summary>
    /// <param name="nodes"></param>
    /// <param name="endsWith"></param>
    /// <returns></returns>
    bool ArrayExpression(out IFastEnumerable<AstExpression> nodes, TokenTypes endsWith = TokenTypes.BracketEnd)
    {
        var list = new Sequence<AstExpression>();

        // Arguments : ( ArgumentList ) — every argument is an AssignmentExpression[+In],
        // so `in` is an ordinary binary operator inside the parentheses even when the
        // enclosing context suppressed it to disambiguate a for-in head. Without this,
        // `for (var i = 0, f = fn("a" in b); …; …)` was rejected at the `in`.
        var savedIn = considerInOfAsOperators;
        considerInOfAsOperators = true;

        try
        {
            do
            {
                stream.SkipNewLines();

                if (stream.CheckAndConsumeAny(endsWith, TokenTypes.EOF))
                    break;

                var isSpread = stream.CheckAndConsume(TokenTypes.TripleDots, out var token);

                if (Expression(out var node))
                {
                    if (isSpread)
                        node = new AstSpreadElement(token, node.End, node);

                    list.Add(node);
                }

                if (stream.CheckAndConsumeAny(endsWith, TokenTypes.EOF))
                    break;

                if (stream.CheckAndConsume(TokenTypes.Comma))
                    continue;

                if (stream.LineTerminator())
                    continue;

                throw stream.Unexpected();
            } while (true);
        }
        finally
        {
            considerInOfAsOperators = savedIn;
        }

        nodes = list;
        return true;
    }
}
