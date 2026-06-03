using System;
using System.Reflection.Emit;
using Broiler.JavaScript.ExpressionCompiler.Expressions;

namespace Broiler.JavaScript.ExpressionCompiler.Generator;

public partial class ILCodeGenerator
{
    protected override CodeInfo VisitTryCatchFinally(YTryCatchFinallyExpression tryCatchFinallyExpression)
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
                var tcb = il.BeginTry();

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

                    Visit(tryCatchFinallyExpression.Catch.Body);
                    if (hasType)
                    {
                        il.EmitSaveLocal(result.LocalIndex);
                    }
                }

                if (tryCatchFinallyExpression.Finally != null)
                {
                    tcb.BeginFinally();
                    Visit(tryCatchFinallyExpression.Finally);
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
