using Expression = Broiler.JavaScript.ExpressionCompiler.Expressions.BExpression;
using Broiler.JavaScript.LinqExpressions.LambdaGen;

namespace Broiler.JavaScript.LinqExpressions.LinqExpressions;


public class ObjectBuilder
{
    public static Expression ToString(Expression value) => value.CallExpression<object, string>(() => (x) => x.ToString(), value);
}
