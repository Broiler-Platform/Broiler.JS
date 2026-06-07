using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.ExpressionCompiler.Core;

namespace Broiler.JavaScript.Ast.Statements;

public class AstClassExpression(FastToken token, FastToken previousToken, AstIdentifier? identifier, AstExpression? @base, IFastEnumerable<AstClassProperty> astClassProperties, bool isDeclaration = false) :
    AstExpression(token, FastNodeType.ClassStatement, previousToken)
{
    public readonly AstIdentifier? Identifier = identifier;
    public readonly AstExpression? Base = @base;
    public readonly IFastEnumerable<AstClassProperty> Members = astClassProperties;

    // True for a ClassDeclaration (a statement, including `export [default] class`),
    // false for a ClassExpression. A declaration binds its name in the enclosing
    // scope; an expression's name is only an immutable binding inside the body.
    public readonly bool IsDeclaration = isDeclaration;

    public override string ToString()
    {
        if (Base != null)
            return $"class {Identifier} extends {Base} {{ {Members.Join("\n\t")} }}";

        if (Identifier == null)
            return $"class {{ {Members.Join("\n\t")} }}";

        return $"class {Identifier} {{ {Members.Join("\n\t")} }}";
    }
}
