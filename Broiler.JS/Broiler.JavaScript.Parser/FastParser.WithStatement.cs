using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Statements;

namespace Broiler.JavaScript.Parser;

partial class FastParser
{
    bool WithStatement(out AstStatement node)
    {
        var begin = stream.Current;
        stream.Consume();
        stream.Expect(TokenTypes.BracketStart);

        if (!ExpressionSequence(out var @object))
            throw stream.Unexpected();

        if (!NonDeclarativeStatement(out var body))
            throw stream.Unexpected();

        node = new AstWithStatement(begin, PreviousToken, @object, body);
        return true;
    }
}
