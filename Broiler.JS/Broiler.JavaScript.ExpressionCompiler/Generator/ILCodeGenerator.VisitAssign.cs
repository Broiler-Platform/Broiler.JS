#nullable enable
using System;
using System.Reflection.Emit;
using Broiler.JavaScript.ExpressionCompiler.Expressions;

namespace Broiler.JavaScript.ExpressionCompiler.Generator;

public readonly struct DataSource(BExpression? exp, int index = -1)
{
    public readonly BExpression? Expression = exp;
    public readonly int Index = index;

    public static implicit operator DataSource(BExpression exp) 
        => new(exp);

    public static implicit  operator DataSource(int index)
        => new(null, index);
}

public partial class ILCodeGenerator
{
    protected override CodeInfo VisitAssign(BAssignExpression yAssignExpression)
    {
        // we need to investigate each type of expression on the left...
        // Visit(yAssignExpression.Right);
        // return Assign(yAssignExpression.Left);

        // from block a non saving expression must be called with -1
        using var temp = il.NewTemp(yAssignExpression.Type);
        VisitAssign(yAssignExpression, temp.LocalIndex);
        il.EmitLoadLocal(temp.LocalIndex);
        return true;
    }

    private CodeInfo VisitSave(DataSource data, int index = -1)
    {
        var exp = data.Expression;
        if (exp == null)
        {
            il.EmitLoadLocal(data.Index);
            return true;
        }

        Visit(exp);
        if(index != -1)
        {
            il.Emit(OpCodes.Dup);
            il.EmitSaveLocal(index);
        }
        return true;
    }

    protected CodeInfo VisitAssign(BAssignExpression exp, int savedIndex)
    {
        switch (exp.Left.NodeType)
        {
            case BExpressionType.Parameter:
                return AssignParameter(exp.Right, exp.Left as BParameterExpression, savedIndex);
            case BExpressionType.Property:
                return AssignProperty(exp.Right, (exp.Left as BPropertyExpression)!, savedIndex);
            case BExpressionType.Field:
                return AssignField(exp.Right, (exp.Left as BFieldExpression)!, savedIndex);
            case BExpressionType.Index:
                return AssignIndex(exp.Right, (exp.Left as BIndexExpression)!, savedIndex);
            case BExpressionType.ArrayIndex:
                return AssignArrayIndex(exp.Right, exp.Left as BArrayIndexExpression, savedIndex);
        }
        throw new NotImplementedException($"Assignment target {exp.Left.NodeType} ({exp.Left.GetType().Name}) is not supported");
    }

    private CodeInfo Assign(BExpression left, DataSource source, int savedIndex = -1)
    {
        switch (left.NodeType)
        {
            case BExpressionType.Parameter:
                return AssignParameter(source, left as BParameterExpression, savedIndex);
            case BExpressionType.Property:
                return AssignProperty(source, (left as BPropertyExpression)!, savedIndex);
            case BExpressionType.Field:
                return AssignField(source, (left as BFieldExpression)!, savedIndex);
            case BExpressionType.Index:
                return AssignIndex(source, (left as BIndexExpression)!, savedIndex);
            case BExpressionType.ArrayIndex:
                return AssignArrayIndex(source, left as BArrayIndexExpression, savedIndex);
        }
        throw new NotImplementedException();
    }

    private CodeInfo AssignIndex(DataSource exp, BIndexExpression yIndexExpression, int savedIndex = -1)
    {
        Visit(yIndexExpression.Target);
        var pa = yIndexExpression.SetMethod!.GetParameters();
        for (int i = 0; i < pa.Length - 1; i++)
        {
            var pe = yIndexExpression.Arguments[i];
            var p = pa[i];
            if(p.IsIn || p.IsOut)
            {
                if(p.ParameterType.IsValueType)
                {
                    LoadAddress(pe);
                    continue;
                }
            }

            if(pe.NodeType == BExpressionType.Assign)
            {
                using var t = il.NewTemp(pe.Type);
                var ti = t.LocalIndex;
                VisitAssign((pe as BAssignExpression)!, ti);
                il.EmitLoadLocal(ti);
                continue;
            }

            Visit(pe);
        }
        VisitSave(exp, savedIndex);
        il.EmitCall(yIndexExpression.SetMethod);
        return true;
    }

    private CodeInfo AssignProperty(DataSource exp, BPropertyExpression yPropertyExpression, int savedIndex = -1)
    {
        if (!yPropertyExpression.IsStatic)
            Visit(yPropertyExpression.Target);
        VisitSave(exp, savedIndex);
        il.EmitCall(yPropertyExpression.SetMethod);
        return true;
    }

    private CodeInfo AssignField(DataSource exp, BFieldExpression yFieldExpression, int savedIndex = -1)
    {
        if (!yFieldExpression.FieldInfo.IsStatic)
        {
            // A generator-rewritten `return` emits a real `ret`/branch as a value
            // sub-expression (completion tracking can lift the assignment target to
            // a boxed field, so a loop body's `return` becomes `box.Value = <return>`,
            // possibly nested inside a conditional). Evaluating that RHS while the
            // target reference sits on the evaluation stack leaves the stack
            // unbalanced where the transfer fires, which the CLR rejects as an
            // invalid program. Spill the target to a temp first — preserving
            // target-before-value evaluation order — so the stack holds only the
            // value when the transfer happens.
            if (exp.Expression != null && ControlTransferScanner.MayTransfer(exp.Expression))
            {
                using var targetTemp = il.NewTemp(yFieldExpression.Target.Type);
                Visit(yFieldExpression.Target);
                il.EmitSaveLocal(targetTemp.LocalIndex);
                VisitSave(exp, savedIndex);
                using var valueTemp = il.NewTemp(yFieldExpression.FieldInfo.FieldType);
                il.EmitSaveLocal(valueTemp.LocalIndex);
                il.EmitLoadLocal(targetTemp.LocalIndex);
                il.EmitLoadLocal(valueTemp.LocalIndex);
                il.Emit(OpCodes.Stfld, yFieldExpression.FieldInfo);
                return true;
            }

            Visit(yFieldExpression.Target);
        }
        VisitSave(exp, savedIndex);
        il.Emit(OpCodes.Stfld, yFieldExpression.FieldInfo);
        return true;
    }

    // Detects whether evaluating an expression may transfer control out of itself
    // (a generator-rewritten `return`) — i.e. emit a `ret`/branch while a value is
    // mid-computation. Nested lambdas have their own IL stream and are skipped.
    private sealed class ControlTransferScanner : BExpressionMapVisitor
    {
        private bool found;

        public static bool MayTransfer(BExpression exp)
        {
            var scanner = new ControlTransferScanner();
            scanner.Visit(exp);
            return scanner.found;
        }

        protected override BExpression VisitReturn(BReturnExpression node)
        {
            found = true;
            return node;
        }

        protected override BExpression VisitLambda(BLambdaExpression node) => node;
    }
}
