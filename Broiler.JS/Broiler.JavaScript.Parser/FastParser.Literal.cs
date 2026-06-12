

using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;

namespace Broiler.JavaScript.Parser;


partial class FastParser
{
    bool Literal(out AstExpression node)
    {
        var token = stream.Current;
        switch (token.Type)
        {
            case TokenTypes.True:
            case TokenTypes.False:
            case TokenTypes.String:
            case TokenTypes.Number:
            case TokenTypes.Null:
            case TokenTypes.RegExLiteral:
            case TokenTypes.BigInt:
            case TokenTypes.Decimal:
                stream.Consume();
                node = new AstLiteral(token.Type, token);
                return true;
        }

        node = null;
        return false;
    }

    bool StringLiteral(out AstExpression node)
    {
        if (stream.CheckAndConsume(TokenTypes.String, out var token))
        {
            node = new AstLiteral(TokenTypes.String, token);
            return true;
        }

        node = default;
        return false;
    }

    bool NumberLiteral(out AstExpression node)
    {
        if (stream.CheckAndConsume(TokenTypes.Number, out var token))
        {
            node = new AstLiteral(TokenTypes.Number, token);
            return true;
        }

        node = default;
        return false;
    }

    // A BigInt literal is a valid LiteralPropertyName (e.g. `{ 0n: 1 }`,
    // `class C { 1n(){} }`, `let { 1n: a } = o`); its key is the string form of the
    // numeric value, computed by the compiler.
    bool BigIntLiteral(out AstExpression node)
    {
        if (stream.CheckAndConsume(TokenTypes.BigInt, out var token))
        {
            node = new AstLiteral(TokenTypes.BigInt, token);
            return true;
        }

        node = default;
        return false;
    }
}
