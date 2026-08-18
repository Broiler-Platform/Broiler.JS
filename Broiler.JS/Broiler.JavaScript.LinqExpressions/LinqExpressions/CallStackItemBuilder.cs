using Broiler.JavaScript.Engine;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.LinqExpressions.LambdaGen;

namespace Broiler.JavaScript.LinqExpressions.LinqExpressions;

public static class CallStackItemBuilder
{
    // A frame is a slot in the context's CallFrameStack, handed back as a FrameToken struct —
    // an ordinary CLR local — so an ordinary call allocates nothing for it
    // (docs/performance-roadmap.md §7). A generator or async body needs a heap frame instead:
    // its slot does not survive suspension.
    // spanStart/spanEnd are the function's own half-open range in the script it was compiled
    // from: its lexical position, which is what a direct-eval binding lookup matches frames
    // against (CallFrameStack.IsLexicallyVisible). An empty range leaves the frame unlocated.
    public static BExpression New(BExpression context, BExpression scriptInfo, int nameOffset, int nameLength, int line, int column, int spanStart = 0, int spanEnd = 0, bool suspendable = false) =>
        (suspendable
            ? NewLambdaExpression.StaticCallExpression<FrameToken>(() => () => CallFrames.EnterSuspendable(null, null, 0, 0, 0, 0, 0, 0), context, scriptInfo,
                BExpression.Constant(nameOffset), BExpression.Constant(nameLength), BExpression.Constant(line), BExpression.Constant(column),
                BExpression.Constant(spanStart), BExpression.Constant(spanEnd))
            : NewLambdaExpression.StaticCallExpression<FrameToken>(() => () => CallFrames.Enter(null, null, 0, 0, 0, 0, 0, 0), context, scriptInfo,
                BExpression.Constant(nameOffset), BExpression.Constant(nameLength), BExpression.Constant(line), BExpression.Constant(column),
                BExpression.Constant(spanStart), BExpression.Constant(spanEnd)));

    public static BExpression Step(BExpression context, BExpression target, int line, int column) =>
        NewLambdaExpression.StaticCallExpression(() => () => CallFrames.Step(null, default, 0, 0), context, target,
            BExpression.Constant(line), BExpression.Constant(column));
}
