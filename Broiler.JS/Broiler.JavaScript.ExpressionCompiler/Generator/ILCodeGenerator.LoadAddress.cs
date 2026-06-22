using System;
using System.Reflection.Emit;
using Broiler.JavaScript.ExpressionCompiler.Expressions;

namespace Broiler.JavaScript.ExpressionCompiler.Generator;

public partial class ILCodeGenerator
{

    protected override CodeInfo VisitAddressOf(BAddressOfExpression node) => LoadAddress(node.Target);

    private CodeInfo LoadAddress(BExpression exp)
    {
        switch (exp.NodeType)
        {
            case BExpressionType.Parameter:
                return LoadParameterAddress(exp as BParameterExpression);
            case BExpressionType.Field:
                return LoadFieldAddress(exp as BFieldExpression);
            case BExpressionType.ArrayIndex:
                return LoadArrayIndexAddress(exp as BArrayIndexExpression);

        }
        var temp = tempVariables[exp.Type];
        Visit(exp);
        il.EmitSaveLocal(temp.LocalIndex);
        il.EmitLoadLocalAddress(temp.LocalIndex);
        return true;
    }

    private CodeInfo LoadArrayIndexAddress(BArrayIndexExpression yArrayIndexExpression)
    {
        Visit(yArrayIndexExpression.Target);
        Visit(yArrayIndexExpression.Index);

        var type = yArrayIndexExpression.Type;

        if (type.IsValueType)
        {
            il.Emit(OpCodes.Ldelema, type);
            return true;
        }
        switch (Type.GetTypeCode(type))
        {
            case TypeCode.Object:
            case TypeCode.String:
                il.Emit(OpCodes.Ldelem_Ref);
                return true;
        }
        il.Emit(OpCodes.Ldelema, type);
        return true;

    }

    private CodeInfo LoadFieldAddress(BFieldExpression yFieldExpression)
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

    private CodeInfo LoadParameterAddress(BParameterExpression yParameterExpression)
    {
        if (!variables.TryGetValue(yParameterExpression, out var varInfo))
        {
            if (TryResolveClosureByName(yParameterExpression.Name, out var closure))
                return LoadAddress(closure);

            varInfo = variables[yParameterExpression];
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
