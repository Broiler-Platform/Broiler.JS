using System;

namespace Broiler.JavaScript.ExpressionCompiler.Expressions;

public class BExpression<T>(in FunctionName name, BExpression body, BParameterExpression @this, BParameterExpression[] parameters, Type returnType) : BLambdaExpression(typeof(T), in name, body, @this, parameters, returnType)
{
    internal BExpression<T1> WithThis<T1>(Type type)
    {
        if (This != null)
            throw new InvalidOperationException();
        return new BExpression<T1>(in Name, Body, Parameter(type), Parameters, ReturnType);
    }
}
