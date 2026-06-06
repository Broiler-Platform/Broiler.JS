using System.Collections.Generic;
using Broiler.JavaScript.ExpressionCompiler.Expressions;

namespace Broiler.JavaScript.ExpressionCompiler.Generator;

/// <summary>
/// Determines whether a <c>finally</c> body can complete abruptly by branching
/// out of the block — i.e. it contains a <c>return</c>, or a <c>break</c>/
/// <c>continue</c> (a <see cref="YGoToExpression"/>/<see cref="YJumpSwitchExpression"/>)
/// whose target label is declared <em>outside</em> the finally.
///
/// Such a finally must override a pending throw per the ECMAScript spec
/// (a <c>finally</c> abrupt completion replaces the completion of the
/// <c>try</c>/<c>catch</c>). Real CLR <c>endfinally</c> re-raises the in-flight
/// exception instead, so <see cref="ILCodeGenerator.VisitTryCatchFinally"/> wraps
/// these — and only these — in an outer exception guard.
///
/// The scan never produces a false negative: only labels actually declared
/// inside the finally are treated as internal, so an external target can never
/// be misclassified as internal. (A false positive merely emits an unnecessary
/// — but still correct — guard.)
/// </summary>
internal sealed class FinallyBranchScanner : YExpressionMapVisitor
{
    private readonly HashSet<YLabelTarget> internalLabels = new();
    private readonly List<YLabelTarget> branchTargets = new();
    private bool hasReturn;

    public static bool BranchesOut(YExpression @finally)
    {
        if (@finally == null)
            return false;

        var scanner = new FinallyBranchScanner();
        scanner.Visit(@finally);

        if (scanner.hasReturn)
            return true;

        foreach (var target in scanner.branchTargets)
        {
            if (!scanner.internalLabels.Contains(target))
                return true;
        }

        return false;
    }

    protected override YExpression VisitLabel(YLabelExpression yLabelExpression)
    {
        internalLabels.Add(yLabelExpression.Target);
        return base.VisitLabel(yLabelExpression);
    }

    protected override YExpression VisitLoop(YLoopExpression yLoopExpression)
    {
        internalLabels.Add(yLoopExpression.Break);
        internalLabels.Add(yLoopExpression.Continue);
        return base.VisitLoop(yLoopExpression);
    }

    protected override YExpression VisitGoto(YGoToExpression yGoToExpression)
    {
        branchTargets.Add(yGoToExpression.Target);
        return base.VisitGoto(yGoToExpression);
    }

    protected override YExpression VisitJumpSwitch(YJumpSwitchExpression node)
    {
        var en = node.Cases.GetFastEnumerator();
        while (en.MoveNext(out var c, out _))
            branchTargets.Add(c);
        return base.VisitJumpSwitch(node);
    }

    protected override YExpression VisitReturn(YReturnExpression yReturnExpression)
    {
        hasReturn = true;
        return base.VisitReturn(yReturnExpression);
    }

    // A nested function compiles to its own IL stream; its returns/branches are
    // not branch-outs of this finally.
    protected override YExpression VisitLambda(YLambdaExpression yLambdaExpression) => yLambdaExpression;
}
