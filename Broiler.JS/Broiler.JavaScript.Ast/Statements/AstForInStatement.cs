using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.ExpressionCompiler.Core;

namespace Broiler.JavaScript.Ast.Statements;


public class AstForInStatement(FastToken token, FastToken previousToken, AstNode beginNode, AstExpression target, AstStatement statement) : AstStatement(token, FastNodeType.ForInStatement, previousToken)
{
    public IFastEnumerable<StringSpan>? HeadTdzNames;
    public readonly AstNode Init = beginNode;
    public readonly AstExpression Target = target;
    public readonly AstStatement Body = statement;

    public override string ToString() => $"for ({Init} in {Target}) {{ {Body} }}";
}
