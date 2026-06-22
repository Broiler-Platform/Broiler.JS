using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.ExpressionCompiler.Expressions;

namespace Broiler.JavaScript.Compiler;

partial class FastCompiler
{
    protected override BExpression VisitSequenceExpression(AstSequenceExpression sequenceExpression)
    {
        var list = new Sequence<BExpression>();
        var e = sequenceExpression.Expressions.GetFastEnumerator();
        while (e.MoveNext(out var exp))
        {
            if (exp != null) list.Add(Visit(exp));
        }

        var r = BExpression.Block(list);
        return r;
    }
}
