using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.ExpressionCompiler.Expressions;

namespace Broiler.JavaScript.Compiler;

partial class FastCompiler
{
    protected override BExpression VisitAwaitExpression(AstAwaitExpression node)
    {
        var target = VisitExpression(node.Argument);
        return BExpression.Await(target);
    }
}
