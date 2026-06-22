using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;

namespace Broiler.JavaScript.Compiler;

partial class FastCompiler
{
    protected override BExpression VisitArrayExpression(AstArrayExpression arrayExpression)
    {
        var e = arrayExpression.Elements.GetFastEnumerator();
        var list = new Sequence<BElementInit>();

        while (e.MoveNext(out var item))
        {
            if (item == null)
            {
                list.Add(BExpression.ElementInit(JSArrayBuilder._Add, [BExpression.Null]));
                continue;
            }

            if (item.Type == FastNodeType.SpreadElement)
            {
                var i = (item as AstSpreadElement).Argument;
                list.Add(BExpression.ElementInit(JSArrayBuilder._AddRange, [Visit(i)]));
                continue;
            }

            list.Add(BExpression.ElementInit(JSArrayBuilder._Add, [Visit(item)]));
        }

        if (list.Count > 0)
            return BExpression.ListInit(BExpression.New(JSArrayBuilder._New), list);

        return BExpression.New(JSArrayBuilder._New);
    }
}
