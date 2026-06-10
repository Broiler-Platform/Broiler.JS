
using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Statements;
using Broiler.JavaScript.ExpressionCompiler.Core;

namespace Broiler.JavaScript.Parser;


partial class FastParser
{
    bool Class(out AstStatement statement)
    {
        statement = default;

        if (ClassExpression(out var node, isStatement: true))
        {
            statement = new AstExpressionStatement(node);
            return true;
        }

        return false;
    }

    bool ClassExpression(out AstExpression statement, bool isStatement = false)
    {
        var begin = stream.Current;
        statement = default;

        var next = stream.Consume();
        AstIdentifier identifier = null;
        AstExpression @base = null;

        if (next.Type != TokenTypes.CurlyBracketStart)
        {
            if (next.Keyword != FastKeywords.extends)
            {
                if (!Identitifer(out identifier))
                    throw stream.Unexpected();
            }

            if (stream.CheckAndConsume(FastKeywords.extends))
            {
                if (!Expression(out @base))
                    throw stream.Unexpected();
            }
        }

        stream.Expect(TokenTypes.CurlyBracketStart);

        var nodes = new Sequence<AstClassProperty>();
        while (!stream.CheckAndConsume(TokenTypes.CurlyBracketEnd))
        {
            // A ClassElement may be preceded by a DecoratorList. Decorators are parsed
            // and discarded; the element itself is parsed as usual. An empty element
            // (`;`) makes ObjectProperty return false, which is fine — unless a
            // decorator was present, in which case a real element is required.
            var decorated = Decorators();

            if (ObjectProperty(out var property, true, isClass: true))
                nodes.Add(property);
            else if (decorated)
                throw stream.Unexpected();

            stream.CheckAndConsumeWithLineTerminator(TokenTypes.SemiColon);
        }

        // A ClassDeclaration binds its name (block-scoped) in the enclosing scope.
        // A ClassExpression's name is visible only inside the class body (as an
        // immutable binding created by the compiler), so it must not register a
        // binding in the enclosing scope — otherwise the name leaks outward.
        if (identifier != null && isStatement)
            variableScope.Top.AddVariable(identifier.Start, identifier.Name, FastVariableKind.Let, throwError: false);

        statement = new AstClassExpression(begin, PreviousToken, identifier, @base, nodes, isDeclaration: isStatement);

        return true;
    }
}
