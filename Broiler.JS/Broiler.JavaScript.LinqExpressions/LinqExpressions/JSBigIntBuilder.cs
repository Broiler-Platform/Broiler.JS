using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.LinqExpressions.LambdaGen;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.LinqExpressions.LinqExpressions;

public class JSBigIntBuilder
{
    public static BExpression New(string value) => NewLambdaExpression.StaticCallExpression<JSValue>(
        () => () => JSValue.CreateBigIntFromString(""), BExpression.Constant(value));
}

public class JSDecimalBuilder
{
    public static BExpression New(string value) => NewLambdaExpression.StaticCallExpression<JSValue>(
        () => () => JSValue.CreateDecimalFromString(""), BExpression.Constant(value));
}
