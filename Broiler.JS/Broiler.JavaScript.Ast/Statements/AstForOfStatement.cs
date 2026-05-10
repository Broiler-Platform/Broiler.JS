using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;

namespace Broiler.JavaScript.Ast.Statements;

public class AstForOfStatement(FastToken token, FastToken previousToken, AstNode beginNode, AstExpression target, AstStatement statement, bool isAwait = false) : 
    AstStatement(token, FastNodeType.ForOfStatement, previousToken)
{
    public readonly AstNode Init = beginNode;
    public readonly AstExpression Target = target;
    public readonly AstStatement Body = statement;
    public readonly bool IsAwait = isAwait;

    public override string ToString() => IsAwait
        ? $"for await ({Init} of {Target}) {{ {Body} }}"
        : $"for ({Init} of {Target}) {{ {Body} }}";
}
