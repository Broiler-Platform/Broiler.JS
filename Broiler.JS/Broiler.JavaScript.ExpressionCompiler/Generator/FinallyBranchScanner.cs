using System.Collections.Generic;
using Broiler.JavaScript.ExpressionCompiler.Expressions;

namespace Broiler.JavaScript.ExpressionCompiler.Generator;

/// <summary>
/// Determines whether a <c>finally</c> body can complete abruptly by branching
/// out of the block — i.e. it contains a <c>return</c>, or a <c>break</c>/
/// <c>continue</c> (a <see cref="BGoToExpression"/>/<see cref="BJumpSwitchExpression"/>)
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
internal sealed class FinallyBranchScanner : BExpressionMapVisitor
{
    private readonly HashSet<BLabelTarget> internalLabels = [];
    private readonly List<BLabelTarget> branchTargets = [];
    private bool hasReturn;

    public static bool BranchesOut(BExpression @finally)
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

    protected override BExpression VisitLabel(BLabelExpression yLabelExpression)
    {
        internalLabels.Add(yLabelExpression.Target);
        return base.VisitLabel(yLabelExpression);
    }

    protected override BExpression VisitLoop(BLoopExpression yLoopExpression)
    {
        internalLabels.Add(yLoopExpression.Break);
        internalLabels.Add(yLoopExpression.Continue);
        return base.VisitLoop(yLoopExpression);
    }

    protected override BExpression VisitGoto(BGoToExpression yGoToExpression)
    {
        branchTargets.Add(yGoToExpression.Target);
        return base.VisitGoto(yGoToExpression);
    }

    protected override BExpression VisitJumpSwitch(BJumpSwitchExpression node)
    {
        var en = node.Cases.GetFastEnumerator();
        while (en.MoveNext(out var c, out _))
            branchTargets.Add(c);
        return base.VisitJumpSwitch(node);
    }

    protected override BExpression VisitReturn(BReturnExpression yReturnExpression)
    {
        hasReturn = true;
        return base.VisitReturn(yReturnExpression);
    }

    // A nested function compiles to its own IL stream; its returns/branches are
    // not branch-outs of this finally.
    protected override BExpression VisitLambda(BLambdaExpression yLambdaExpression) => yLambdaExpression;
}
