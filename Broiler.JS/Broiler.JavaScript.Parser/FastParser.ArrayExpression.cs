
using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.ExpressionCompiler.Core;

namespace Broiler.JavaScript.Parser;

partial class FastParser
{
    /// <summary>
    /// Parses an ArgumentList (of a call, <c>new</c>, <c>import()</c> or a decorator) up to
    /// <paramref name="endsWith"/>. Line terminators inside it are skipped, but only a comma
    /// separates two arguments.
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

                // The end of the source never closes an argument list: `f(1,` and `f(1` are
                // truncated input and a SyntaxError, not a call with the arguments seen so far.
                if (stream.Current.Type == TokenTypes.EOF)
                    throw stream.Unexpected();

                if (stream.CheckAndConsume(endsWith))
                    break;

                var isSpread = stream.CheckAndConsume(TokenTypes.TripleDots, out var token);

                // An ArgumentList has no elisions: every position between commas holds an
                // argument, so `f(,)`, `f(1,,2)` and `f(...)` are SyntaxErrors. (Only the one
                // trailing comma before the closing parenthesis is allowed, handled above.)
                if (!Expression(out var node))
                    throw stream.Unexpected();

                if (isSpread)
                    node = new AstSpreadElement(token, node.End, node);

                list.Add(node);

                // Arguments are separated by commas only (§13.3 ArgumentList). A line terminator
                // after an argument is skipped like any other whitespace, but it never separates
                // two arguments: `f(1\n2)` is a SyntaxError, not the call f(1, 2).
                stream.SkipNewLines();

                if (stream.CheckAndConsume(endsWith))
                    break;

                if (stream.CheckAndConsume(TokenTypes.Comma))
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
