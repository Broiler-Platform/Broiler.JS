

using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;

namespace Broiler.JavaScript.Parser;


partial class FastParser
{
    bool PropertyName(out AstExpression node, out bool computed, out bool isPrivate, bool acceptKeywords = false)
    {
        var begin = BeginUndo();
        isPrivate = false;

        if (acceptKeywords)
        {
            var token = begin.Token;
            switch (token.Type)
            {
                case TokenTypes.True:
                case TokenTypes.False:
                case TokenTypes.In:
                case TokenTypes.InstanceOf:
                case TokenTypes.Null:
                case TokenTypes.Identifier:
                    stream.Consume();

                    if (token.ContextualKeyword != FastKeywords.none)
                    {
                        node = new AstIdentifier(token);
                        computed = false;
                        isPrivate = false;

                        return true;
                    }

                    node = new AstIdentifier(token.AsString());
                    computed = false;
                    isPrivate = false;

                    return true;
            }
        }

        if (Identitifer(out var id))
        {
            node = id;
            computed = false;
            isPrivate = false;

            return true;
        }

        if (stream.CheckAndConsume(TokenTypes.Hash, out var hashToken))
        {
            if (!Identitifer(out var privateIdentifier))
                throw stream.Unexpected();

            node = new AstIdentifier(hashToken, $"#{privateIdentifier.Name.Value}");
            computed = false;
            isPrivate = true;

            return true;
        }

        if (StringLiteral(out node))
        {
            computed = false;
            isPrivate = false;
            return true;
        }

        if (NumberLiteral(out node))
        {
            computed = false;
            isPrivate = false;
            return true;
        }

        if (stream.CheckAndConsume(TokenTypes.SquareBracketStart))
        {
            if (!Expression(out node))
                throw stream.Unexpected();

            stream.Expect(TokenTypes.SquareBracketEnd);
            computed = true;
            isPrivate = false;

            return true;
        }

        node = null;
        computed = false;
        isPrivate = false;

        return begin.Reset();
    }
}
