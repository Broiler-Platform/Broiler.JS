

using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Statements;

namespace Broiler.JavaScript.Parser;


partial class FastParser
{
    bool DoWhileStatement(out AstStatement node)
    {
        var begin = stream.Current;
        stream.Consume();

        if (!NonDeclarativeStatement(out var statement))
            throw stream.Unexpected();

        stream.CheckAndConsume(TokenTypes.SemiColon);
        stream.Expect(FastKeywords.@while);
        stream.Expect(TokenTypes.BracketStart);

        // `IterationStatement : do Statement while ( Expression ) ;` — the condition is
        // not optional, so `do {} while ()` is a SyntaxError.
        if (!ExpressionSequence(out var test))
            throw stream.Unexpected();

        EndOfStatement();

        node = new AstDoWhileStatement(begin, PreviousToken, test, statement);
        return true;
    }
}
