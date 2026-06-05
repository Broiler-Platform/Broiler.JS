using System;
using System.Reflection.Emit;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.ExpressionCompiler.Generator;

namespace Broiler.JavaScript.ExpressionCompiler.Core;

public class ILTryBlock(ILWriter iLWriter, Label label) : LinkedStackItem<ILTryBlock>
{
    private bool isCatch = false;
    private bool isFinally = false;
    private bool hasFinally = false;

    internal readonly ILWriter il = iLWriter;
    private readonly ILWriterLabel label = iLWriter.DefineLabel("tryEnd");

    private Sequence<(ILWriterLabel hop, ILWriterLabel final, int localIndex)> pendingJumps = [];
    private Sequence<(int state, ILWriterLabel target, int localIndex)> pendingFinallyJumps = [];
    private ILWriter.TempVariable? finallyJumpState;
    private ILWriterLabel? finallyJumpLabel;

    internal int SavedLocal;

    internal void CollectLabels(YTryCatchFinallyExpression exp, LabelInfo labels) => TryCatchLabelMarker.Collect(exp, this, labels);

    internal void MarkHasFinally()
    {
        hasFinally = true;
        finallyJumpState ??= il.NewTemp(typeof(int));
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

        if (label.TryBlock == this)
        {
            il.Goto(label, index);
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
