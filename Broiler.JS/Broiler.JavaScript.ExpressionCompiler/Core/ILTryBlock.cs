using System;
using System.Reflection.Emit;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.ExpressionCompiler.Generator;

namespace Broiler.JavaScript.ExpressionCompiler.Core;

public class ILTryBlock(ILWriter iLWriter, Label label, bool hasOuterGuard = false) : LinkedStackItem<ILTryBlock>
{
    private bool isCatch = false;
    private bool isFinally = false;
    private bool hasFinally = false;

    // When true an outer try/catch wraps this block so a finally that completes
    // abruptly (continue/break/return) overrides a pending throw instead of having
    // endfinally re-raise it. Opened by ILWriter.BeginTry, closed in Dispose.
    private readonly bool hasOuterGuard = hasOuterGuard;

    internal readonly ILWriter il = iLWriter;
    private readonly ILWriterLabel label = iLWriter.DefineLabel("tryEnd");
    private readonly ILWriterLabel outerGuardEnd = hasOuterGuard ? iLWriter.DefineLabel("guardEnd") : null;

    private Sequence<(ILWriterLabel hop, ILWriterLabel final, int localIndex)> pendingJumps = [];
    private Sequence<(int state, ILWriterLabel target, int localIndex)> pendingFinallyJumps = [];
    private ILWriter.TempVariable? finallyJumpState;
    private ILWriterLabel? finallyJumpLabel;

    internal int SavedLocal;

    internal void CollectLabels(BTryCatchFinallyExpression exp, LabelInfo labels) => TryCatchLabelMarker.Collect(exp, this, labels);

    internal void MarkHasFinally()
    {
        hasFinally = true;
        if (finallyJumpState == null)
        {
            finallyJumpState = il.NewTemp(typeof(int));

            // The deferred finally-jump state is a REUSED temp local — a prior
            // try block may have left it non-zero, and the CLR only zero-inits
            // locals once at method entry. Reset it at the top of this try body
            // so the post-finally dispatch (see Dispose) branches only when THIS
            // block actually requested a deferred jump (continue/break/return);
            // otherwise a stale 1 from an earlier loop would spuriously fire the
            // previous block's break/continue.
            il.Emit(OpCodes.Ldc_I4_0);
            il.EmitSaveLocal(finallyJumpState.LocalIndex);
        }
    }

    public void BeginCatch(Type type)
    {
        if (isFinally)
            throw new InvalidOperationException($"Cannot start catch after finally has begin");

        isCatch = true;

        il.Emit(OpCodes.Leave, label);
        il.BeginCatchBlock(type);
    }

    public void BeginFinally()
    {
        if (isFinally)
            throw new InvalidOperationException($"You already in the finally block");

        hasFinally = true;
        isFinally = true;
        isCatch = false;
        il.Emit(OpCodes.Leave, label);

        il.BeginFinallyBlock();
    }

    public override void Dispose()
    {
        if (isCatch)
            il.Emit(OpCodes.Leave, label);

        if (!(isCatch || isFinally))
            throw new InvalidOperationException($"Cannot finish try block without catch/finally");

        if (finallyJumpLabel != null)
            il.MarkLabel(finallyJumpLabel);

        base.Dispose();

        // jump all pending
        il.EndExceptionBlock();

        il.MarkLabel(label);

        if (hasOuterGuard)
        {
            // Normal completion of the inner try/catch/finally: leave the outer
            // guard to the dispatch point below.
            il.Emit(OpCodes.Leave, outerGuardEnd);

            // A CLR exception escaped the inner try/catch/finally. If the finally
            // requested an abrupt jump (continue/break/return), that completion
            // overrides the pending throw: discard the exception and fall through
            // to the deferred-jump dispatch. Otherwise re-raise it.
            il.BeginCatchBlock(typeof(Exception));
            il.Emit(OpCodes.Pop);                                   // drop the exception reference
            var rethrow = il.DefineLabel("guardRethrow");
            il.EmitLoadLocal(finallyJumpState!.LocalIndex);
            il.Emit(OpCodes.Brfalse, rethrow);
            il.Emit(OpCodes.Leave, outerGuardEnd);
            il.MarkLabel(rethrow);
            il.Emit(OpCodes.Rethrow);
            il.EndExceptionBlock();

            il.MarkLabel(outerGuardEnd);
        }

        if (finallyJumpState != null)
        {
            foreach (var (state, target, index) in pendingFinallyJumps)
            {
                var next = il.DefineLabel($"finally jump next {state}");
                il.EmitLoadLocal(finallyJumpState.LocalIndex);
                il.Emit(OpCodes.Ldc_I4, state);
                il.Emit(OpCodes.Bne_Un, next);
                il.Branch(target, index);
                il.MarkLabel(next);
            }

            finallyJumpState.Dispose();
            finallyJumpState = null;
        }

        // The pending-jump hops below are reachable only via their own `leave hop`
        // (an abrupt completion — e.g. `return` — inside the catch body). They are
        // emitted right where normal completion lands (`label`), so without an
        // explicit skip the fall-through path of a try/catch whose try succeeds would
        // run straight into the first hop, loading an unset local and branching to the
        // return label. Jump over the hops on the normal path.
        if (pendingJumps.Count > 0)
        {
            var afterHops = il.DefineLabel("afterHops");
            il.Goto(afterHops);

            foreach (var (hop, jump, index) in pendingJumps)
            {
                il.MarkLabel(hop);
                il.Branch(jump, index);
            }

            il.MarkLabel(afterHops);
        }

        if (SavedLocal >= 0)
            il.EmitLoadLocal(SavedLocal);
    }

    internal void Branch(ILWriterLabel label, int index = -1)
    {
        // A target owned by THIS block is a label declared directly inside this
        // block's own body (e.g. the end-of-statement label of a labelled
        // statement nested in this finally). Branching to it stays within this
        // protected region, so emit a plain branch — even when we are currently
        // emitting this block's finally. This MUST be checked before the
        // `isFinally` deferral below: otherwise a `break L` to a label that lives
        // in this same finally (reached via an inner try's deferred-jump dispatch)
        // would be pushed onto this finally's exit machinery and dispatched AFTER
        // endfinally, producing a branch into the finally region — invalid IL.
        if (label.TryBlock == this)
        {
            il.Goto(label, index);
            return;
        }

        if (isFinally)
        {
            finallyJumpLabel ??= il.DefineLabel($"finally hop for {label.ID}");
            var state = pendingFinallyJumps.Count + 1;
            pendingFinallyJumps.Add((state, label, index));
            il.Emit(OpCodes.Ldc_I4, state);
            il.EmitSaveLocal(finallyJumpState!.LocalIndex);
            il.Emit(OpCodes.Br, finallyJumpLabel);
            return;
        }

        if (hasFinally)
        {
            var state = pendingFinallyJumps.Count + 1;
            pendingFinallyJumps.Add((state, label, index));
            il.Emit(OpCodes.Ldc_I4, state);
            il.EmitSaveLocal(finallyJumpState!.LocalIndex);
            il.Emit(OpCodes.Leave, this.label);
            return;
        }

        var hop = il.DefineLabel($"hop for {label.ID}");

        pendingJumps.Add((hop, label, index));
        il.Emit(OpCodes.Leave, hop);
    }
}
