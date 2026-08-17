
using Broiler.JavaScript.Ast.Misc;
using System.Runtime.CompilerServices;

namespace Broiler.JavaScript.Parser;


partial class FastParser
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    bool Identitifer(out AstIdentifier node)
    {
        if (stream.CheckAndConsume(TokenTypes.Identifier, out var token))
        {
            node = new AstIdentifier(token);
            return true;
        }

        node = null;
        return false;
    }

    /// <summary>
    /// Parses the name after a <c>#</c>: <c>PrivateIdentifier :: # IdentifierName</c>.
    /// </summary>
    /// <remarks>
    /// An IdentifierName includes every reserved word, so <c>#in</c>, <c>#instanceof</c>,
    /// <c>#null</c>, <c>#true</c> and <c>#false</c> are all valid private names. Most reserved
    /// words reach the parser as <see cref="TokenTypes.Identifier"/> carrying a keyword, so
    /// <c>#class</c> and <c>#if</c> already parsed; these five get a token type of their own
    /// from the scanner and were rejected. A mangler picks private names from the same short
    /// alphabet it uses everywhere else and lands on <c>#in</c> like any other two-letter name.
    /// </remarks>
    bool PrivateIdentifierName(out string name)
    {
        var token = stream.Current;
        if (token.Type == TokenTypes.Identifier || IsKeywordPropertyName(token.Type))
        {
            stream.Consume();
            // Same text an AstIdentifier would take from the token: the cooked form when the
            // name was spelled with a unicode escape, the raw span otherwise.
            name = token.CookedText ?? token.Span.Value;
            return true;
        }

        name = null;
        return false;
    }
}
