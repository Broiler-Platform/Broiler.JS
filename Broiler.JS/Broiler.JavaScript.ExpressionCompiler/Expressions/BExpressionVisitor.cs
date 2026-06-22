using System;

namespace Broiler.JavaScript.ExpressionCompiler.Expressions;

public abstract class BExpressionVisitor<T>: StackGuard<T, BExpression>
{
    public override T VisitIn(BExpression exp)
    {
        if (exp == null)
            return default;
        
        return exp.NodeType switch
        {
            BExpressionType.Block => VisitBlock(exp as BBlockExpression),
            BExpressionType.Call => VisitCall(exp as BCallExpression),
            BExpressionType.Binary => VisitBinary(exp as BBinaryExpression),
            BExpressionType.Constant => VisitConstant(exp as BConstantExpression),
            BExpressionType.Conditional => VisitConditional(exp as BConditionalExpression),
            BExpressionType.Assign => VisitAssign(exp as BAssignExpression),
            BExpressionType.Parameter => VisitParameter(exp as BParameterExpression),
            BExpressionType.New => VisitNew(exp as BNewExpression),
            BExpressionType.Field => VisitField(exp as BFieldExpression),
            BExpressionType.Property => VisitProperty(exp as BPropertyExpression),
            BExpressionType.NewArray => VisitNewArray(exp as BNewArrayExpression),
            BExpressionType.GoTo => VisitGoto(exp as BGoToExpression),
            BExpressionType.Return => VisitReturn(exp as BReturnExpression),
            BExpressionType.Loop => VisitLoop(exp as BLoopExpression),
            BExpressionType.Lambda => VisitLambda(exp as BLambdaExpression),
            BExpressionType.Label => VisitLabel(exp as BLabelExpression),
            BExpressionType.TypeAs => VisitTypeAs(exp as BTypeAsExpression),
            BExpressionType.TypeIs => VisitTypeIs(exp as BTypeIsExpression),
            BExpressionType.NewArrayBounds => VisitNewArrayBounds(exp as BNewArrayBoundsExpression),
            BExpressionType.ArrayIndex => VisitArrayIndex(exp as BArrayIndexExpression),
            BExpressionType.Index => VisitIndex(exp as BIndexExpression),
            BExpressionType.Coalesce => VisitCoalesce(exp as BCoalesceExpression),
            BExpressionType.Unary => VisitUnary(exp as BUnaryExpression),
            BExpressionType.ArrayLength => VisitArrayLength(exp as BArrayLengthExpression),
            BExpressionType.TryCatchFinally => VisitTryCatchFinally(exp as BTryCatchFinallyExpression),
            BExpressionType.Throw => VisitThrow(exp as BThrowExpression),
            BExpressionType.Convert => VisitConvert(exp as BConvertExpression),
            BExpressionType.Invoke => VisitInvoke(exp as BInvokeExpression),
            BExpressionType.Delegate => VisitDelegate(exp as BDelegateExpression),
            BExpressionType.MemberInit => VisitMemberInit(exp as BMemberInitExpression),
            //case BExpressionType.Relay:
            //    return VisitRelay(exp as BRelayExpression);
            BExpressionType.Empty => VisitEmpty(exp as BEmptyExpression),
            BExpressionType.Switch => VisitSwitch(exp as BSwitchExpression),
            BExpressionType.Yield => VisitYield(exp as BYieldExpression),
            BExpressionType.DebugInfo => VisitDebugInfo(exp as BDebugInfoExpression),
            BExpressionType.ILOffset => VisitILOffset(exp as BILOffsetExpression),
            BExpressionType.Box => VisitBox(exp as BBoxExpression),
            BExpressionType.Unbox => VisitUnbox(exp as BUnboxExpression),
            BExpressionType.JumpSwitch => VisitJumpSwitch(exp as BJumpSwitchExpression),
            BExpressionType.ListInit => VisitListInit(exp as BListInitExpression),
            BExpressionType.CoalesceCall => VisitCoalesceCall(exp as BCoalesceCallExpression),
            //case BExpressionType.TypeEqual:
            //    break;
            BExpressionType.Int32Constant => VisitInt32Constant(exp as BInt32ConstantExpression),
            BExpressionType.UInt32Constant => VisitUInt32Constant(exp as BUInt32ConstantExpression),
            BExpressionType.Int64Constant => VisitInt64Constant(exp as BInt64ConstantExpression),
            BExpressionType.UInt64Constant => VisitUInt64Constant(exp as BUInt64ConstantExpression),
            BExpressionType.DoubleConstant => VisitDoubleConstant(exp as BDoubleConstantExpression),
            BExpressionType.FloatConstant => VisitFloatConstant(exp as BFloatConstantExpression),
            BExpressionType.BooleanConstant => VisitBooleanConstant(exp as BBooleanConstantExpression),
            BExpressionType.StringConstant => VisitStringConstant(exp as BStringConstantExpression),
            BExpressionType.ByteConstant => VisitByteConstant(exp as BByteConstantExpression),
            BExpressionType.TypeConstant => VisitTypeConstant(exp as BTypeConstantExpression),
            BExpressionType.MethodConstant => VisitMethodConstant(exp as BMethodConstantExpression),
            BExpressionType.AddressOf => VisitAddressOf(exp as BAddressOfExpression),
            _ => throw new NotImplementedException($"{exp.NodeType}"),
        };
    }

    protected abstract T VisitAddressOf(BAddressOfExpression node);
    protected abstract T VisitMethodConstant(BMethodConstantExpression node);
    protected abstract T VisitTypeConstant(BTypeConstantExpression node);
    protected abstract T VisitByteConstant(BByteConstantExpression node);
    protected abstract T VisitStringConstant(BStringConstantExpression node);
    protected abstract T VisitBooleanConstant(BBooleanConstantExpression node);
    protected abstract T VisitFloatConstant(BFloatConstantExpression node);
    protected abstract T VisitDoubleConstant(BDoubleConstantExpression node);
    protected abstract T VisitUInt64Constant(BUInt64ConstantExpression node);
    protected abstract T VisitInt64Constant(BInt64ConstantExpression node);
    protected abstract T VisitUInt32Constant(BUInt32ConstantExpression node);
    protected abstract T VisitInt32Constant(BInt32ConstantExpression node);
    protected abstract T VisitCoalesceCall(BCoalesceCallExpression node);
    protected abstract T VisitListInit(BListInitExpression node);
    protected abstract T VisitJumpSwitch(BJumpSwitchExpression node);
    protected abstract T VisitUnbox(BUnboxExpression node);
    protected abstract T VisitBox(BBoxExpression node);
    protected abstract T VisitILOffset(BILOffsetExpression node);
    protected abstract T VisitDebugInfo(BDebugInfoExpression node);
    protected abstract T VisitYield(BYieldExpression node);
    protected abstract T VisitSwitch(BSwitchExpression node);
    protected abstract T VisitEmpty(BEmptyExpression exp);
    // protected abstract T VisitRelay(BRelayExpression yRelayExpression);
    protected abstract T VisitMemberInit(BMemberInitExpression memberInitExpression);
    protected abstract T VisitDelegate(BDelegateExpression yDelegateExpression);
    protected abstract T VisitInvoke(BInvokeExpression invokeExpression);
    protected abstract T VisitConvert(BConvertExpression convertExpression);
    protected abstract T VisitThrow(BThrowExpression throwExpression);
    protected abstract T VisitTryCatchFinally(BTryCatchFinallyExpression tryCatchFinallyExpression);
    protected abstract T VisitArrayLength(BArrayLengthExpression arrayLengthExpression);
    protected abstract T VisitUnary(BUnaryExpression yUnaryExpression);
    protected abstract T VisitCoalesce(BCoalesceExpression yCoalesceExpression);
    protected abstract T VisitIndex(BIndexExpression yIndexExpression);
    protected abstract T VisitArrayIndex(BArrayIndexExpression yArrayIndexExpression);
    protected abstract T VisitNewArrayBounds(BNewArrayBoundsExpression yNewArrayBoundsExpression);
    protected abstract T VisitTypeIs(BTypeIsExpression yTypeIsExpression);
    protected abstract T VisitTypeAs(BTypeAsExpression yTypeAsExpression);
    protected abstract T VisitLabel(BLabelExpression yLabelExpression);
    protected abstract T VisitLambda(BLambdaExpression yLambdaExpression);
    protected abstract T VisitLoop(BLoopExpression yLoopExpression);
    protected abstract T VisitReturn(BReturnExpression yReturnExpression);
    protected abstract T VisitGoto(BGoToExpression yGoToExpression);
    protected abstract T VisitNewArray(BNewArrayExpression yNewArrayExpression);
    protected abstract T VisitProperty(BPropertyExpression yPropertyExpression);
    protected abstract T VisitField(BFieldExpression yFieldExpression);
    protected abstract T VisitNew(BNewExpression yNewExpression);
    protected abstract T VisitCall(BCallExpression yCallExpression);
    protected abstract T VisitBlock(BBlockExpression yBlockExpression);
    protected abstract T VisitParameter(BParameterExpression yParameterExpression);
    protected abstract T VisitAssign(BAssignExpression yAssignExpression);
    protected abstract T VisitConditional(BConditionalExpression yConditionalExpression);
    protected abstract T VisitConstant(BConstantExpression yConstantExpression);
    protected abstract T VisitBinary(BBinaryExpression yBinaryExpression);
}
