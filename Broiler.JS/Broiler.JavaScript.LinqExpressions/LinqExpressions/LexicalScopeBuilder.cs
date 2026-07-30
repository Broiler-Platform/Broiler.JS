using Expression = Broiler.JavaScript.ExpressionCompiler.Expressions.BExpression;
using Broiler.JavaScript.LinqExpressions.LambdaGen;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.LinqExpressions.LinqExpressions;

public class LexicalScopeBuilder
{
    public static Expression NewScope(Expression context, Expression fileName, Expression function, int line, int column, bool suspendable = false) =>
        suspendable
            ? NewLambdaExpression.StaticCallExpression<CallStackItem>(() => () =>
                CallStackItem.EnterSuspendableScope(null, "", "", 0, 0), context, fileName, function, Expression.Constant(line), Expression.Constant(column))
            : NewLambdaExpression.StaticCallExpression<CallStackItem>(() => () =>
                CallStackItem.EnterScope(null, "", "", 0, 0), context, fileName, function, Expression.Constant(line), Expression.Constant(column));

    public static Expression Pop(Expression exp, Expression context) => exp.CallExpression<CallStackItem>(() => (x) => x.Pop(null), context);
}
