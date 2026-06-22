using Broiler.JavaScript.Engine;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.LinqExpressions.LambdaGen;

namespace Broiler.JavaScript.LinqExpressions.LinqExpressions;

public static class CallStackItemBuilder
{
    public static BExpression New(BExpression context, BExpression scriptInfo, int nameOffset, int nameLength, int line, int column) =>
        NewLambdaExpression.NewExpression<CallStackItem>(() => () => new CallStackItem(null, null, 0, 0, 0, 0), context, scriptInfo,
            BExpression.Constant(nameOffset), BExpression.Constant(nameLength), BExpression.Constant(line), BExpression.Constant(column));

    public static BExpression Step(BExpression target, int line, int column) => target.CallExpression<CallStackItem, int, int>(() => (x, a, b) => x.Step(a, b),
            BExpression.Constant(line), BExpression.Constant(column));
}
