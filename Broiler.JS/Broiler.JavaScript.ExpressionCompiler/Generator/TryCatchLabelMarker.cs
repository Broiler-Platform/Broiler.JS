using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.ExpressionCompiler.Expressions;

namespace Broiler.JavaScript.ExpressionCompiler.Generator;

public class TryCatchLabelMarker(ILTryBlock tryBlock, LabelInfo labels) : BExpressionMapVisitor
{
    public static void Collect(BTryCatchFinallyExpression body, ILTryBlock tryBlock, LabelInfo labels)
    {
        TryCatchLabelMarker t = new(tryBlock, labels);
        t.Visit(body.Try);
        if (body.Catch != null)
            t.Visit(body.Catch.Body);
        if (body.Finally != null)
            t.Visit(body.Finally);
    }

    protected override BExpression VisitLabel(BLabelExpression yLabelExpression)
    {
        labels.Create(yLabelExpression.Target, tryBlock, false);
        return base.VisitLabel(yLabelExpression);
    }

    protected override BExpression VisitLoop(BLoopExpression yLoopExpression)
    {
        labels.Create(yLoopExpression.Break, tryBlock, false);
        labels.Create(yLoopExpression.Continue, tryBlock, false);
        return base.VisitLoop(yLoopExpression);
    }

    protected override BExpression VisitTryCatchFinally(BTryCatchFinallyExpression tryCatchFinallyExpression) => tryCatchFinallyExpression;

    protected override BExpression VisitLambda(BLambdaExpression yLambdaExpression) => yLambdaExpression;
}
