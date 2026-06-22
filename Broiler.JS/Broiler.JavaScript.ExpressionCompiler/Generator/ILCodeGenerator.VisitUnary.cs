using System;
using System.Reflection.Emit;
using Broiler.JavaScript.ExpressionCompiler.Expressions;

namespace Broiler.JavaScript.ExpressionCompiler.Generator;

public partial class ILCodeGenerator
{
    protected override CodeInfo VisitUnary(BUnaryExpression yUnaryExpression)
    {
        Visit(yUnaryExpression.Target);
        switch(yUnaryExpression.Operator)
        {
            case BUnaryOperator.Negative:
                il.Emit(OpCodes.Neg);
                return true;
            case BUnaryOperator.Not:
                il.EmitConstant(0);
                il.Emit(OpCodes.Ceq);
                return true;
            case BUnaryOperator.OnesComplement:
                il.Emit(OpCodes.Not);
                return true;
        }
        throw new NotImplementedException();
    }
}
