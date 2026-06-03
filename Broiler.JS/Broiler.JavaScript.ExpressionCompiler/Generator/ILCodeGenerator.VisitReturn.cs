using System;
using System.Reflection.Emit;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.ExpressionCompiler.Expressions;

namespace Broiler.JavaScript.ExpressionCompiler.Generator;

public partial class ILCodeGenerator
{
    protected override CodeInfo VisitReturn(YReturnExpression yReturnExpression)
    {
        var label = labels[yReturnExpression.Target];
        var def = yReturnExpression.Default;
        if(def != null)
        {

            if(!il.IsTryBlock)
            {
                if(def.NodeType == YExpressionType.Call)
                {
                    if(yReturnExpression.Type.IsAssignableFrom(def.Type))
                    {
                        // tail call....
                        if (VisitTailCall(def as YCallExpression))
                            return true;
                    }
                    Visit(yReturnExpression.Default);
                    il.Emit(OpCodes.Ret);
                    return true;
                }
            }
            var temp = il.NewTemp(def.Type);
            if (!il.IsTryBlock)
            {
                using (temp)
                    return VisitReturn(def, label, temp.LocalIndex);
            }

            return VisitReturn(def, label, temp.LocalIndex);
        }
        il.Branch(label);
        return true;
    }

    private CodeInfo VisitReturn(YExpression exp, ILWriterLabel label, int localIndex)
    {
        switch (exp.NodeType)
        {
            case YExpressionType.Assign:
                return VisitReturnAssign(exp as YAssignExpression, label, localIndex);
            case YExpressionType.Block:
                return VisitReturnBlock(exp as YBlockExpression, label, localIndex);

            // The branches of a conditional / short-circuit operator in return
            // position are themselves in tail position, so recurse into them to
            // preserve proper tail calls (e.g. `return c ? a : f()`, `return a && f()`).
            case YExpressionType.Conditional
                when exp is YConditionalExpression { @false: not null } conditional
                    && !conditional.Type.IsValueType
                    && conditional.@true.Type.IsAssignableTo(conditional.Type)
                    && conditional.@false.Type.IsAssignableTo(conditional.Type):
                return VisitReturnConditional(conditional, label, localIndex);
            case YExpressionType.Coalesce
                when exp is YCoalesceExpression coalesce
                    && !coalesce.Type.IsValueType
                    && coalesce.Left.Type.IsAssignableTo(coalesce.Type)
                    && coalesce.Right.Type.IsAssignableTo(coalesce.Type):
                return VisitReturnCoalesce(coalesce, label, localIndex);
        }
        if (exp is not YCallExpression call || !TryEmitJavaScriptTailCallValue(call))
            Visit(exp);
        return EmitReturnOnStack(label, localIndex);
    }

    // Emits the return of a value already on the evaluation stack: a real Ret
    // outside a protected region, or save-local + branch when inside one.
    private CodeInfo EmitReturnOnStack(ILWriterLabel label, int localIndex)
    {
        if (!il.IsTryBlock)
        {
            il.Emit(OpCodes.Ret);
            return true;
        }
        il.EmitSaveLocal(localIndex);
        il.Branch(label, localIndex);
        return true;
    }

    private CodeInfo VisitReturnConditional(YConditionalExpression conditional, ILWriterLabel label, int localIndex)
    {
        var falseBegin = il.DefineLabel("retCondFalse", il.Top);
        Visit(conditional.test);
        il.Emit(OpCodes.Brfalse, falseBegin);
        VisitReturn(conditional.@true, label, localIndex);
        il.MarkLabel(falseBegin);
        VisitReturn(conditional.@false, label, localIndex);
        return true;
    }

    private CodeInfo VisitReturnCoalesce(YCoalesceExpression coalesce, ILWriterLabel label, int localIndex)
    {
        var notNull = il.DefineLabel("retCoalesce", il.Top);
        Visit(coalesce.Left);
        il.Emit(OpCodes.Dup);
        il.Emit(OpCodes.Brtrue, notNull);
        il.Emit(OpCodes.Pop);
        VisitReturn(coalesce.Right, label, localIndex);
        il.MarkLabel(notNull);
        return EmitReturnOnStack(label, localIndex);
    }

    private CodeInfo VisitReturnAssign(YAssignExpression assign, ILWriterLabel label, int localIndex)
    {
        VisitAssign(assign, localIndex);
        if (!il.IsTryBlock)
        {
            il.EmitLoadLocal(localIndex);
            il.Emit(OpCodes.Ret);
            return true;
        }
        il.Branch(label, localIndex);
        return true;
    }

    private CodeInfo VisitReturnBlock(YBlockExpression block, ILWriterLabel label, int localIndex)
    {
        using var tvs = tempVariables.Push();

        foreach (var p in block.FlattenVariables)
            variables.Create(p, tvs);

        foreach(var (exp, last) in block.FlattenExpressions)
        {
            if(!last)
            {
                VisitSave(exp, false);
                continue;
            }

            // last item...
            return VisitReturn(exp, label, localIndex);
        }

        throw new InvalidOperationException($"This code is not reachable");
    }
}
