using System;
using System.Reflection.Emit;
using Broiler.JavaScript.ExpressionCompiler.Expressions;

namespace Broiler.JavaScript.ExpressionCompiler.Generator;

public partial class ILCodeGenerator
{
    protected override CodeInfo VisitTryCatchFinally(BTryCatchFinallyExpression tryCatchFinallyExpression)
    {
        using (tempVariables.Push())
        {
            // Synthetic completion-tracking wrappers are tail-call transparent: they
            // neither count as an enclosing try (for nested-try blocking) nor block
            // tail calls themselves. Real try/finally still block as before.
            var transparent = tryCatchFinallyExpression.TailCallTransparent;
            var blockTailCalls = !transparent && (tailCallTryDepth != 0 || tryCatchFinallyExpression.Catch != null);
            if (!transparent)
                tailCallTryDepth++;
            if (blockTailCalls)
                tailCallBlockedDepth++;

            try
            {
                // A finally that can complete abruptly (continue/break/return)
                // must override a pending throw; guard it with an outer try/catch
                // (see FinallyBranchScanner / ILTryBlock).
                var guardFinallyBranch = tryCatchFinallyExpression.Finally != null
                    && FinallyBranchScanner.BranchesOut(tryCatchFinallyExpression.Finally);
                var tcb = il.BeginTry(guardFinallyBranch);

                // visit labels...
                tcb.CollectLabels(tryCatchFinallyExpression, labels);

                var hasType = tryCatchFinallyExpression.Try.Type != typeof(void);
                if (tryCatchFinallyExpression.Finally != null)
                    tcb.MarkHasFinally();

                var result = hasType ? tempVariables[tryCatchFinallyExpression.Try.Type] : null;

                tcb.SavedLocal = hasType ? result.LocalIndex : -1;

                // we need to save this in local variable...
                Visit(tryCatchFinallyExpression.Try);
                if (hasType)
                {
                    il.EmitSaveLocal(result.LocalIndex);
                }



                if (tryCatchFinallyExpression.Catch != null)
                {
                    tcb.BeginCatch(typeof(Exception));
                    if (tryCatchFinallyExpression.Catch.Parameter == null)
                    {
                        il.Emit(OpCodes.Pop);
                    }
                    else
                    {
                        var v = variables[tryCatchFinallyExpression.Catch.Parameter];
                        il.EmitSaveLocal(v.LocalBuilder.LocalIndex);
                    }

                    // A catch clause with no finally is itself in tail position when
                    // the surrounding context is (spec: for `try Block Catch`, the
                    // tail position of Catch is the tail position of the try
                    // statement). A `return f()` there exits the function immediately,
                    // so it must be a proper tail call rather than growing the stack.
                    // This node always blocks (Catch != null), so lift exactly the
                    // block it added while visiting the catch body; any enclosing
                    // blocking (e.g. an outer try/finally) survives and keeps the
                    // catch blocked. With a finally present the finally runs after
                    // the catch, so the catch is NOT a tail position and stays blocked.
                    var liftCatchBlock = blockTailCalls && tryCatchFinallyExpression.Finally == null;
                    if (liftCatchBlock)
                        tailCallBlockedDepth--;
                    Visit(tryCatchFinallyExpression.Catch.Body);
                    if (liftCatchBlock)
                        tailCallBlockedDepth++;
                    if (hasType)
                    {
                        il.EmitSaveLocal(result.LocalIndex);
                    }
                }

                if (tryCatchFinallyExpression.Finally != null)
                {
                    tcb.BeginFinally();
                    // The finally block runs last, so a `return f()` there is in tail
                    // position whenever the try statement itself is. Lift this node's
                    // block while visiting the finally so the call can be a proper tail
                    // call; an enclosing try/finally's block still survives and keeps it
                    // non-tail. The finally-jump machinery (ILTryBlock.Branch) defers the
                    // return until after endfinally, so the JSTailCall sentinel reaches
                    // the trampoline correctly.
                    var liftFinallyBlock = blockTailCalls;
                    if (liftFinallyBlock)
                        tailCallBlockedDepth--;
                    Visit(tryCatchFinallyExpression.Finally);
                    if (liftFinallyBlock)
                        tailCallBlockedDepth++;

                    // A finally never contributes a value, but its body expression can
                    // be non-void on the fall-through path (e.g. an `if`/loop statement
                    // lowers to a Block that ends with its completion variable). That
                    // value would sit on the evaluation stack at endfinally, which is
                    // illegal IL, so discard it. (Try/Catch save their value to `result`
                    // above; the finally has nowhere to put it.)
                    if (tryCatchFinallyExpression.Finally.Type != typeof(void))
                        il.Emit(OpCodes.Pop);
                }
                tcb.Dispose();
            }
            finally
            {
                if (blockTailCalls)
                    tailCallBlockedDepth--;
                if (!transparent)
                    tailCallTryDepth--;
            }
        }
        return true;
    }
}
