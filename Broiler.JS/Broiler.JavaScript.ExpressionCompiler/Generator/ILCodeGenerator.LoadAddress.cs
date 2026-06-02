using System;
using System.Reflection.Emit;
using Broiler.JavaScript.ExpressionCompiler.Expressions;

namespace Broiler.JavaScript.ExpressionCompiler.Generator;

public partial class ILCodeGenerator
{

    protected override CodeInfo VisitAddressOf(YAddressOfExpression node) => LoadAddress(node.Target);

    private CodeInfo LoadAddress(YExpression exp)
    {
        switch (exp.NodeType)
        {
            case YExpressionType.Parameter:
                return LoadParameterAddress(exp as YParameterExpression);
            case YExpressionType.Field:
                return LoadFieldAddress(exp as YFieldExpression);
            case YExpressionType.ArrayIndex:
                return LoadArrayIndexAddress(exp as YArrayIndexExpression);

        }
        var temp = tempVariables[exp.Type];
        Visit(exp);
        il.EmitSaveLocal(temp.LocalIndex);
        il.EmitLoadLocalAddress(temp.LocalIndex);
        return true;
    }

    private CodeInfo LoadArrayIndexAddress(YArrayIndexExpression yArrayIndexExpression)
    {
        Visit(yArrayIndexExpression.Target);
        Visit(yArrayIndexExpression.Index);

        var type = yArrayIndexExpression.Type;

        il.Emit(OpCodes.Ldelema, type);
        return true;

    }

    private CodeInfo LoadFieldAddress(YFieldExpression yFieldExpression)
    {
        var field = yFieldExpression.FieldInfo;
        if (field.IsStatic)
        {
            if (field.IsLiteral)
            {
                throw new InvalidOperationException();
            }
            il.Emit(OpCodes.Ldsflda, field);
            return true;
        }

        Visit(yFieldExpression.Target);
        il.Emit(OpCodes.Ldflda, field);
        return true;
    }

    private CodeInfo LoadParameterAddress(YParameterExpression yParameterExpression)
    {
        if (!TryResolveVariable(yParameterExpression, out var varInfo))
        {
            if (TryResolveClosureByName(yParameterExpression.Name, out var closure))
                return LoadAddress(closure);

            throw new InvalidOperationException($"Unable to resolve parameter '{yParameterExpression.Name}'.");
        }
        if (varInfo.IsArgument) {
            if(varInfo.IsReference)
            {
                il.EmitLoadArg(varInfo.Index);
                return true;
            }
            il.EmitLoadArgAddress(varInfo.Index);
            return true;
        }
        il.EmitLoadLocalAddress(varInfo.LocalBuilder.LocalIndex);
        return true;
    }
}
