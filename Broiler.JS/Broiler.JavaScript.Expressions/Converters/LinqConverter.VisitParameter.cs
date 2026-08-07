using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Broiler.JavaScript.ExpressionCompiler.Expressions;

namespace Broiler.JavaScript.ExpressionCompiler.Converters;


public partial class LinqConverter
{
    protected override BExpression VisitAdd(BinaryExpression node) => BExpression.Binary(Visit(node.Left), BOperator.Add, Visit(node.Right));
    protected override BExpression VisitAddAssign(BinaryExpression node) => throw new NotImplementedException();
    protected override BExpression VisitAddAssignChecked(BinaryExpression node) => throw new NotImplementedException();
    protected override BExpression VisitAddChecked(BinaryExpression node) => throw new NotImplementedException();
    protected override BExpression VisitAnd(BinaryExpression node) => BExpression.Binary(Visit(node.Left), BOperator.BitwiseAnd, Visit(node.Right));
    protected override BExpression VisitAndAlso(BinaryExpression node) => BExpression.Binary(Visit(node.Left), BOperator.BooleanAnd, Visit(node.Right));
    protected override BExpression VisitAndAssign(BinaryExpression node) => throw new NotImplementedException();
    protected override BExpression VisitArrayIndex(BinaryExpression node) => BExpression.ArrayIndex(Visit(node.Left), Visit(node.Right));
    protected override BExpression VisitArrayLength(UnaryExpression node) => BExpression.ArrayLength(Visit(node.Operand));
    protected override BExpression VisitAssign(BinaryExpression node)
    {
        if (node.Conversion != null)
            throw new NotSupportedException();
        return BExpression.Assign(Visit(node.Left), Visit(node.Right));

    }
    protected override BExpression VisitConditional(ConditionalExpression node) => BExpression.Conditional(Visit(node.Test), Visit(node.IfTrue), Visit(node.IfFalse));
    protected override BExpression VisitConstant(ConstantExpression node) => BExpression.Constant(node.Value, node.Type);
    protected override BExpression VisitConvert(UnaryExpression node) => BExpression.Convert(Visit(node.Operand), node.Type, true);
    protected override BExpression VisitConvertChecked(UnaryExpression node) => throw new NotImplementedException();
    protected override BExpression VisitDebugInfo(ConstantExpression node) => throw new NotImplementedException();
    protected override BExpression VisitDecrement(UnaryExpression node) => throw new NotImplementedException();
    protected override BExpression VisitDefault(DefaultExpression node)
    {
        if (node.Type == typeof(void))
            return BExpression.Empty;

        return BExpression.Null;
    }
    protected override BExpression VisitDivide(BinaryExpression node) => BExpression.Binary(Visit(node.Left), BOperator.Divide, Visit(node.Right));
    protected override BExpression VisitDivideAssign(BinaryExpression node) => throw new NotImplementedException();
    protected override BExpression VisitDynamic(DynamicExpression node) => throw new NotImplementedException();
    protected override BExpression VisitEqual(BinaryExpression node) => BExpression.Equal(Visit(node.Left), Visit(node.Right));
    protected override BExpression VisitExclusiveOr(BinaryExpression node) => BExpression.Binary(Visit(node.Left), BOperator.Xor, Visit(node.Right));
    protected override BExpression VisitExclusiveOrAssign(BinaryExpression node) => throw new NotImplementedException();
    protected override BExpression VisitExtension(Expression exp) => throw new NotImplementedException();
    protected override BExpression VisitGoto(GotoExpression node) => node.Kind switch
    {
        GotoExpressionKind.Break or GotoExpressionKind.Continue or GotoExpressionKind.Goto => BExpression.GoTo(labels[node.Target], Visit(node.Value)),
        GotoExpressionKind.Return => BExpression.Return(labels[node.Target], Visit(node.Value)),
        _ => throw new NotImplementedException(),
    };
    protected override BExpression VisitGreaterThan(BinaryExpression node) => BExpression.Binary(Visit(node.Left), BOperator.Greater, Visit(node.Right));
    protected override BExpression VisitGreaterThanOrEqual(BinaryExpression node) => BExpression.Binary(Visit(node.Left), BOperator.GreaterOrEqual, Visit(node.Right));
    protected override BExpression VisitIncrement(UnaryExpression node) => throw new NotImplementedException();
    protected override BExpression VisitIndex(IndexExpression node) => BExpression.Index(Visit(node.Object), node.Indexer, VisitList(node.Arguments));
    protected override BExpression VisitInvoke(InvocationExpression node) => BExpression.Invoke(Visit(node.Expression), VisitList(node.Arguments));
    protected override BExpression VisitIsFalse(UnaryExpression node) => throw new NotImplementedException();
    protected override BExpression VisitIsTrue(UnaryExpression node) => throw new NotImplementedException();
    protected override BExpression VisitLabel(LabelExpression node) => BExpression.Label(labels[node.Target], Visit(node.DefaultValue));
    protected override BExpression VisitLeftShift(BinaryExpression node) => BExpression.Binary(Visit(node.Left), BOperator.LeftShift, Visit(node.Right));
    protected override BExpression VisitLeftShiftAssign(BinaryExpression node) => throw new NotImplementedException();
    protected override BExpression VisitLessThan(BinaryExpression node) => BExpression.Binary(Visit(node.Left), BOperator.Less, Visit(node.Right));
    protected override BExpression VisitLessThanOrEqual(BinaryExpression node) => BExpression.Binary(Visit(node.Left), BOperator.LessOrEqual, Visit(node.Right));
    protected override BExpression VisitListInit(ListInitExpression node) => throw new NotImplementedException();
    protected override BExpression VisitLoop(LoopExpression node) => BExpression.Loop(Visit(node.Body), labels[node.BreakLabel], node.ContinueLabel != null ? labels[node.ContinueLabel] : null);
    protected override BExpression VisitMemberAccess(MemberExpression node)
    {
        if (node.Member is FieldInfo field)
            return BExpression.Field(Visit(node.Expression), field);

        if (node.Member is PropertyInfo property)
            return BExpression.Property(Visit(node.Expression), property);

        throw new NotImplementedException();
    }
    protected override BExpression VisitMemberInit(MemberInitExpression node) => BExpression.MemberInit(Visit(node.NewExpression) as BNewExpression, [.. node.Bindings.Select(Visit)]);
    protected override BExpression VisitModulo(BinaryExpression node) => BExpression.Binary(Visit(node.Left), BOperator.Mod, Visit(node.Right));
    protected override BExpression VisitModuloAssign(BinaryExpression node) => throw new NotImplementedException();
    protected override BExpression VisitMultiply(BinaryExpression node) => BExpression.Binary(Visit(node.Left), BOperator.Multipley, Visit(node.Right));
    protected override BExpression VisitMultiplyAssign(BinaryExpression node) => throw new NotImplementedException();
    protected override BExpression VisitMultiplyAssignChecked(BinaryExpression node) => throw new NotImplementedException();
    protected override BExpression VisitMultiplyChecked(BinaryExpression node) => throw new NotImplementedException();
    protected override BExpression VisitNegate(UnaryExpression node) => BExpression.Negative(Visit(node.Operand));
    protected override BExpression VisitNegateChecked(BinaryExpression node) => throw new NotImplementedException();
    protected override BExpression VisitNew(NewExpression node) => BExpression.New(node.Constructor, VisitList(node.Arguments));
    protected override BExpression VisitNewArrayBounds(NewArrayExpression node) => BExpression.NewArrayBounds(node.Type.GetElementType(), Visit(node.Expressions.First()));
    protected override BExpression VisitNewArrayInit(NewArrayExpression node) => BExpression.NewArray(node.Type.GetElementType(), VisitList(node.Expressions));
    protected override BExpression VisitNot(UnaryExpression node) => BExpression.Not(Visit(node.Operand));
    protected override BExpression VisitNotEqual(BinaryExpression node) => BExpression.NotEqual(Visit(node.Left), Visit(node.Right));
    protected override BExpression VisitOnesComplement(UnaryExpression node) => BExpression.OnesComplement(Visit(node.Operand));
    protected override BExpression VisitOr(BinaryExpression node) => BExpression.Binary(Visit(node.Left), BOperator.BitwiseOr, Visit(node.Right));
    protected override BExpression VisitOrAssign(BinaryExpression node) => throw new NotImplementedException();
    protected override BExpression VisitOrElse(BinaryExpression node) => BExpression.OrElse(Visit(node.Left), Visit(node.Right));
    protected override BExpression VisitParameter(ParameterExpression node) => parameters[node];
    protected override BExpression VisitPostDecrementAssign(UnaryExpression node) => throw new NotImplementedException();
    protected override BExpression VisitPostIncrementAssign(UnaryExpression node) => throw new NotImplementedException();
    protected override BExpression VisitPower(BinaryExpression node)
    {
        var m = typeof(Math).GetMethod(nameof(Math.Pow));
        var left = Visit(node.Left);
        var right = Visit(node.Right);

        left = left.Type == typeof(double) ? left : BExpression.Convert(left, typeof(double));
        right = right.Type == typeof(double) ? right : BExpression.Convert(right, typeof(double));

        return BExpression.Call(null, m, left, right);
    }
    protected override BExpression VisitPowerAssign(BinaryExpression node) => throw new NotImplementedException();
    protected override BExpression VisitPreDecrementAssign(UnaryExpression node) => throw new NotImplementedException();
    protected override BExpression VisitPreIncrementAssign(UnaryExpression node) => throw new NotImplementedException();
    protected override BExpression VisitQuote(UnaryExpression node) => throw new NotImplementedException();
    protected override BExpression VisitRightShift(BinaryExpression node) => BExpression.Binary(Visit(node.Left), BOperator.RightShift, Visit(node.Right));
    protected override BExpression VisitRightShiftAssign(BinaryExpression node) => throw new NotImplementedException();
    protected override BExpression VisitRuntimeVariables(RuntimeVariablesExpression node) => throw new NotImplementedException();
    protected override BExpression VisitSubtract(BinaryExpression node) => BExpression.Binary(Visit(node.Left), BOperator.Subtract, Visit(node.Right));
    protected override BExpression VisitSubtractAssign(BinaryExpression node) => throw new NotImplementedException();
    protected override BExpression VisitSubtractAssignChecked(BinaryExpression node) => throw new NotImplementedException();
    protected override BExpression VisitSubtractChecked(BinaryExpression node) => throw new NotImplementedException();
    protected override BExpression VisitSwitch(SwitchExpression node)
    {
        var cases = node.Cases.Select(x =>
            BExpression.SwitchCase(Visit(x.Body),
            [.. x.TestValues.Select(Visit)]
        )).ToArray();

        return BExpression.Switch(
            Visit(node.SwitchValue),
            Visit(node.DefaultBody),
            node.Comparison,
            cases);

    }
    protected override BExpression VisitThrow(UnaryExpression node) => BExpression.Throw(Visit(node.Operand));
    protected override BExpression VisitTry(TryExpression node)
    {
        BCatchBody cb = null;
        if (node.Handlers.Count > 0)
        {
            var first = node.Handlers.First();
            cb = first.Variable != null
                ? BExpression.Catch(parameters[first.Variable], Visit(first.Body))
                : BExpression.Catch(Visit(first.Body));
        }
        return BExpression.TryCatchFinally(Visit(node.Body), cb, Visit(node.Finally));
    }
    protected override BExpression VisitTypeAs(UnaryExpression node) => BExpression.TypeAs(Visit(node.Operand), node.Type);
    protected override BExpression VisitTypeEqual(TypeBinaryExpression node) => BExpression.TypeIs(Visit(node.Expression), node.TypeOperand);
    protected override BExpression VisitTypeIs(TypeBinaryExpression node) => BExpression.TypeIs(Visit(node.Expression), node.TypeOperand);
    protected override BExpression VisitUnaryPlus(UnaryExpression node) => Visit(node.Operand);
    protected override BExpression VisitUnbox(UnaryExpression node) => throw new NotImplementedException();
}
