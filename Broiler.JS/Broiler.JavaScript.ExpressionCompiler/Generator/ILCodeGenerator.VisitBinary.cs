using System;
using System.Reflection.Emit;
using Broiler.JavaScript.ExpressionCompiler.Expressions;

namespace Broiler.JavaScript.ExpressionCompiler.Generator;

public partial class ILCodeGenerator
{
    protected override CodeInfo VisitBinary(BBinaryExpression yBinaryExpression)
    {
        switch (yBinaryExpression.Operator)
        {
            case BOperator.BooleanAnd:
                {
                    var trueEnd = il.DefineLabel("trueEnd", il.Top);
                    var falseEnd = il.DefineLabel("falseEnd", il.Top);
                    Visit(yBinaryExpression.Left);
                    il.Emit(OpCodes.Brfalse, trueEnd);
                    Visit(yBinaryExpression.Right);
                    il.Emit(OpCodes.Br, falseEnd);
                    il.MarkLabel(trueEnd);
                    il.EmitConstant(0);
                    il.MarkLabel(falseEnd);
                }
                return true;
            case BOperator.BooleanOr:
                {
                    var trueEnd = il.DefineLabel("trueEnd", il.Top);
                    var falseEnd = il.DefineLabel("falseEnd", il.Top);
                    Visit(yBinaryExpression.Left);
                    il.Emit(OpCodes.Brtrue, trueEnd);
                    Visit(yBinaryExpression.Right);
                    il.Emit(OpCodes.Br, falseEnd);
                    il.MarkLabel(trueEnd);
                    il.EmitConstant(1);
                    il.MarkLabel(falseEnd);
                }
                return true;
        }


        Visit(yBinaryExpression.Left);
        Visit(yBinaryExpression.Right);
        switch (yBinaryExpression.Operator)
        {
            case BOperator.Add:
                il.Emit(OpCodes.Add);
                break;
            case BOperator.Subtract:
                il.Emit(OpCodes.Sub);
                break;
            case BOperator.Multipley:
                il.Emit(OpCodes.Mul);
                break;
            case BOperator.Divide:
                il.Emit(OpCodes.Div);
                break;
            case BOperator.Mod:
                il.Emit(OpCodes.Rem);
                break;
            case BOperator.Xor:
                il.Emit(OpCodes.Xor);
                break;
            case BOperator.BitwiseAnd:
                il.Emit(OpCodes.And);
                break;
            case BOperator.BitwiseOr:
                il.Emit(OpCodes.Or);
                break;
            case BOperator.Less:
                il.Emit(OpCodes.Clt);
                break;
            case BOperator.LessOrEqual:
                il.Emit(OpCodes.Cgt);
                il.EmitConstant(0);
                il.Emit(OpCodes.Ceq);
                break;
            case BOperator.Greater:
                il.Emit(OpCodes.Cgt);
                break;
            case BOperator.GreaterOrEqual:
                il.Emit(OpCodes.Clt);
                il.EmitConstant(0);
                il.Emit(OpCodes.Ceq);
                break;
            case BOperator.Equal:
                il.Emit(OpCodes.Ceq);
                break;
            case BOperator.NotEqual:
                il.Emit(OpCodes.Ceq);
                il.EmitConstant(0);
                il.Emit(OpCodes.Ceq);
                break;
            case BOperator.LeftShift:
                il.Emit(OpCodes.Shl);
                break;
            case BOperator.RightShift:
                il.Emit(OpCodes.Shr);
                break;
            case BOperator.UnsignedRightShift:
                il.Emit(OpCodes.Shr_Un);
                break;
            default:
                throw new NotSupportedException($"{yBinaryExpression.Operator}");
        }

        return true;
    }
}
