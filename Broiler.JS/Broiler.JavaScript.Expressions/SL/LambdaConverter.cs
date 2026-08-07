using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.ExpressionCompiler.Expressions;

namespace Broiler.JavaScript.ExpressionCompiler.SL;

public class LambdaConverter : BExpressionVisitor<Expression>
{
    private Dictionary<BParameterExpression, ParameterExpression> cache = [];

    public (IFastEnumerable<ParameterExpression> pe, IDisposable disposable) Register(IFastEnumerable<BParameterExpression> plist)
    {
        if (plist == null)
        {
            return (null, null);
        }

        var pe = new Sequence<ParameterExpression>(plist.Count);
        var en = plist.GetFastEnumerator();
        while(en.MoveNext(out var e))
        {
            // var e = plist[i];
            var p = Expression.Parameter(e.Type, e.Name);
            // pe[i] = p;
            pe.Add(p);
            cache[e] = p;
        }

        var d = new DisposableAction(() => {
            var a = plist;
            var en = plist.GetFastEnumerator();
            while(en.MoveNext(out var item))
            {
                cache.Remove(item);
            }
        });

        return (pe, d);
    }

    protected override Expression VisitAddressOf(BAddressOfExpression node) => throw new NotImplementedException();

    protected override Expression VisitArrayIndex(BArrayIndexExpression yArrayIndexExpression) => Expression.ArrayIndex(Visit(yArrayIndexExpression.Target), Visit(yArrayIndexExpression.Index));

    protected override Expression VisitArrayLength(BArrayLengthExpression arrayLengthExpression) => Expression.ArrayLength(Visit(arrayLengthExpression.Target));

    protected override Expression VisitAssign(BAssignExpression yAssignExpression) => Expression.Assign(Visit(yAssignExpression.Left), Visit(yAssignExpression.Right));

    protected override Expression VisitBinary(BBinaryExpression yBinaryExpression)
    {
        var left = Visit(yBinaryExpression.Left);
        var right = Visit(yBinaryExpression.Right);
        return yBinaryExpression.Operator switch
        {
            BOperator.Add => Expression.Add(left, right),
            BOperator.Subtract => Expression.Subtract(left, right),
            BOperator.Multipley => Expression.Multiply(left, right),
            BOperator.Divide => Expression.Divide(left, right),
            BOperator.Mod => Expression.Modulo(left, right),
            BOperator.Power => Expression.Power(left, right),
            BOperator.Xor => Expression.ExclusiveOr(left, right),
            BOperator.BitwiseAnd => Expression.And(left, right),
            BOperator.BitwiseOr => Expression.Or(left, right),
            BOperator.BooleanAnd => Expression.AndAlso(left, right),
            BOperator.BooleanOr => Expression.OrElse(left, right),
            BOperator.Less => Expression.LessThan(left, right),
            BOperator.LessOrEqual => Expression.LessThanOrEqual(left, right),
            BOperator.Greater => Expression.GreaterThan(left, right),
            BOperator.GreaterOrEqual => Expression.GreaterThanOrEqual(left, right),
            BOperator.Equal => Expression.Equal(left, right),
            BOperator.NotEqual => Expression.NotEqual(left, right),
            BOperator.LeftShift => Expression.LeftShift(left, right),
            BOperator.RightShift => Expression.RightShift(left, right),
            BOperator.UnsignedRightShift => Expression.RightShift(Expression.Convert(left, typeof(uint)), right),
            _ => throw new NotImplementedException(),
        };
    }

    protected override Expression VisitBlock(BBlockExpression yBlockExpression)
    {
        var (list, d) = Register(yBlockExpression.Variables);
        using (d)
        {
            return Expression.Block(list, yBlockExpression.Expressions.Select(Visit));
        }
    }

    protected override Expression VisitBooleanConstant(BBooleanConstantExpression node) => throw new NotImplementedException();

    protected override Expression VisitBox(BBoxExpression node) => Expression.Convert(Visit(node.Target), typeof(object));

    protected override Expression VisitByteConstant(BByteConstantExpression node) => throw new NotImplementedException();

    protected override Expression VisitCall(BCallExpression yCallExpression) => Expression.Call(Visit(yCallExpression.Target), yCallExpression.Method, yCallExpression.Arguments.Select(Visit));

    protected override Expression VisitCoalesce(BCoalesceExpression yCoalesceExpression) => Expression.Coalesce(Visit(yCoalesceExpression.Left), Visit(yCoalesceExpression.Right));

    protected override Expression VisitCoalesceCall(BCoalesceCallExpression node) => throw new NotImplementedException();

    protected override Expression VisitConditional(BConditionalExpression yConditionalExpression) => Expression.Condition(
            Visit(yConditionalExpression.test),
            Visit(yConditionalExpression.@true),
            Visit(yConditionalExpression.@false));

    protected override Expression VisitConstant(BConstantExpression yConstantExpression) => Expression.Constant(yConstantExpression.Value);

    protected override Expression VisitConvert(BConvertExpression convertExpression) => Expression.Convert(Visit(convertExpression), convertExpression.Type);

    protected override Expression VisitDebugInfo(BDebugInfoExpression node) => Expression.Empty();

    protected override Expression VisitDelegate(BDelegateExpression yDelegateExpression) => throw new NotImplementedException();

    protected override Expression VisitDoubleConstant(BDoubleConstantExpression node) => throw new NotImplementedException();

    protected override Expression VisitEmpty(BEmptyExpression exp) => Expression.Empty();

    protected override Expression VisitField(BFieldExpression yFieldExpression) => Expression.Field(Visit(yFieldExpression.Target), yFieldExpression.FieldInfo);

    protected override Expression VisitFloatConstant(BFloatConstantExpression node) => throw new NotImplementedException();

    protected override Expression VisitGoto(BGoToExpression yGoToExpression) => throw new NotImplementedException();

    protected override Expression VisitILOffset(BILOffsetExpression node) => throw new NotImplementedException();

    protected override Expression VisitIndex(BIndexExpression yIndexExpression) => throw new NotImplementedException();

    protected override Expression VisitInt32Constant(BInt32ConstantExpression node) => throw new NotImplementedException();

    protected override Expression VisitInt64Constant(BInt64ConstantExpression node) => throw new NotImplementedException();

    protected override Expression VisitInvoke(BInvokeExpression invokeExpression) => throw new NotImplementedException();

    protected override Expression VisitJumpSwitch(BJumpSwitchExpression node) => throw new NotImplementedException();

    protected override Expression VisitLabel(BLabelExpression yLabelExpression) => throw new NotImplementedException();

    protected override Expression VisitLambda(BLambdaExpression yLambdaExpression) => throw new NotImplementedException();

    protected override Expression VisitListInit(BListInitExpression node) => throw new NotImplementedException();

    protected override Expression VisitLoop(BLoopExpression yLoopExpression) => throw new NotImplementedException();

    protected override Expression VisitMemberInit(BMemberInitExpression memberInitExpression) => throw new NotImplementedException();

    protected override Expression VisitMethodConstant(BMethodConstantExpression node) => throw new NotImplementedException();

    protected override Expression VisitNew(BNewExpression yNewExpression) => throw new NotImplementedException();

    protected override Expression VisitNewArray(BNewArrayExpression yNewArrayExpression) => throw new NotImplementedException();

    protected override Expression VisitNewArrayBounds(BNewArrayBoundsExpression yNewArrayBoundsExpression) => throw new NotImplementedException();

    protected override Expression VisitParameter(BParameterExpression yParameterExpression) => throw new NotImplementedException();

    protected override Expression VisitProperty(BPropertyExpression yPropertyExpression) => throw new NotImplementedException();

    protected override Expression VisitReturn(BReturnExpression yReturnExpression) => throw new NotImplementedException();

    protected override Expression VisitStringConstant(BStringConstantExpression node) => throw new NotImplementedException();

    protected override Expression VisitSwitch(BSwitchExpression node) => throw new NotImplementedException();

    protected override Expression VisitThrow(BThrowExpression throwExpression) => throw new NotImplementedException();

    protected override Expression VisitTryCatchFinally(BTryCatchFinallyExpression tryCatchFinallyExpression) => throw new NotImplementedException();

    protected override Expression VisitTypeAs(BTypeAsExpression yTypeAsExpression) => throw new NotImplementedException();

    protected override Expression VisitTypeConstant(BTypeConstantExpression node) => throw new NotImplementedException();

    protected override Expression VisitTypeIs(BTypeIsExpression yTypeIsExpression) => throw new NotImplementedException();

    protected override Expression VisitUInt32Constant(BUInt32ConstantExpression node) => throw new NotImplementedException();

    protected override Expression VisitUInt64Constant(BUInt64ConstantExpression node) => throw new NotImplementedException();

    protected override Expression VisitUnary(BUnaryExpression yUnaryExpression) => throw new NotImplementedException();

    protected override Expression VisitUnbox(BUnboxExpression node) => throw new NotImplementedException();

    protected override Expression VisitYield(BYieldExpression node) => throw new NotImplementedException();
}
