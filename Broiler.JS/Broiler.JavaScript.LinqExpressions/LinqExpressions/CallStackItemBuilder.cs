using Broiler.JavaScript.Engine;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.LinqExpressions.LambdaGen;

namespace Broiler.JavaScript.LinqExpressions.LinqExpressions;

public static class CallStackItemBuilder
{
    // Enter rather than `new`: frames come from a free list, so an ordinary call no longer
    // allocates its activation record (docs/performance-roadmap.md §7). A generator or async
    // body keeps its caller's frame pinned out of that list, because it holds a reference to
    // it across every suspension.
    public static BExpression New(BExpression context, BExpression scriptInfo, int nameOffset, int nameLength, int line, int column, bool suspendable = false) =>
        (suspendable
            ? NewLambdaExpression.StaticCallExpression<CallStackItem>(() => () => CallStackItem.EnterSuspendable(null, null, 0, 0, 0, 0), context, scriptInfo,
                BExpression.Constant(nameOffset), BExpression.Constant(nameLength), BExpression.Constant(line), BExpression.Constant(column))
            : NewLambdaExpression.StaticCallExpression<CallStackItem>(() => () => CallStackItem.Enter(null, null, 0, 0, 0, 0), context, scriptInfo,
                BExpression.Constant(nameOffset), BExpression.Constant(nameLength), BExpression.Constant(line), BExpression.Constant(column)));

    public static BExpression Step(BExpression target, int line, int column) => target.CallExpression<CallStackItem, int, int>(() => (x, a, b) => x.Step(a, b),
            BExpression.Constant(line), BExpression.Constant(column));
}
