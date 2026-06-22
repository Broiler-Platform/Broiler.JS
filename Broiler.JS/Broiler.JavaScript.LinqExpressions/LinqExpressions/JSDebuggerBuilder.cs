using Expression = Broiler.JavaScript.ExpressionCompiler.Expressions.BExpression;
using Broiler.JavaScript.LinqExpressions.LambdaGen;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.LinqExpressions.LinqExpressions;

public class JSDebuggerBuilder
{
    public static Expression RaiseBreak() => NewLambdaExpression.StaticCallExpression(() => () => JSDebugger.RaiseBreak());
}
