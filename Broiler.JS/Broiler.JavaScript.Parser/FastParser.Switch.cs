
using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Statements;
using Broiler.JavaScript.ExpressionCompiler.Core;

namespace Broiler.JavaScript.Parser;


partial class FastParser
{
    bool Switch(out AstStatement node)
    {
        var begin = stream.Current;
        stream.Consume();
        node = null;

        stream.Expect(TokenTypes.BracketStart);

        // The switch discriminant is an `Expression` (the full comma production),
        // so `switch (a, b, c)` is valid. ExpressionSequence also consumes ")".
        if (!ExpressionSequence(out var target, TokenTypes.BracketEnd) || target.Type == FastNodeType.EmptyExpression)
            throw stream.Unexpected();

        stream.Expect(TokenTypes.CurlyBracketStart);

        var nodes = new Sequence<Case>();
        var statements = new Sequence<AstStatement>();
        AstExpression test = null;
        // Whether a case/default label has been opened whose body is still being
        // accumulated. A pending `default` has a null test, so flushing on
        // `test != null` would drop a non-last default clause (its statements
        // would merge into the following case); track an explicit flag instead.
        bool pending = false;
        var scope = variableScope.Push(begin, FastNodeType.Block);

        try
        {
            while (!stream.CheckAndConsume(TokenTypes.CurlyBracketEnd))
            {
                if (stream.CheckAndConsume(FastKeywords.@case))
                {
                    if (pending)
                    {
                        nodes.Add(new Case(test, statements));
                        statements = [];
                    }

                    // `case Expression :` — Expression is the full comma
                    // production, so `case a, b, c:` is valid. ExpressionSequence
                    // also consumes the terminating ":".
                    if (!ExpressionSequence(out test, TokenTypes.Colon) || test.Type == FastNodeType.EmptyExpression)
                        throw stream.Unexpected();

                    pending = true;
                }
                else if (stream.CheckAndConsume(FastKeywords.@default))
                {
                    stream.Expect(TokenTypes.Colon);

                    if (pending)
                    {
                        nodes.Add(new Case(test, statements));
                        statements = [];
                    }

                    test = null;
                    pending = true;
                }
                else if (Statement(out var stmt))
                    statements.Add(stmt);
            }

            if (pending)
                nodes.Add(new Case(test, statements));

            node = new AstSwitchStatement(begin, PreviousToken, target, nodes)
            {
                HoistingScope = scope.GetVariables()
            };
        }
        finally
        {
            scope.Dispose();
        }

        return true;
    }
}
