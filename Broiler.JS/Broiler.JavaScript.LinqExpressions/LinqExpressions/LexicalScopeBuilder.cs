using Expression = Broiler.JavaScript.ExpressionCompiler.Expressions.BExpression;
using Broiler.JavaScript.LinqExpressions.LambdaGen;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.LinqExpressions.LinqExpressions;

public class LexicalScopeBuilder
{
    public static Expression NewScope(Expression context, Expression fileName, Expression function, int line, int column, bool suspendable = false) =>
        suspendable
            ? NewLambdaExpression.StaticCallExpression<FrameToken>(() => () =>
                CallFrames.EnterSuspendableScope(null, "", "", 0, 0), context, fileName, function, Expression.Constant(line), Expression.Constant(column))
            : NewLambdaExpression.StaticCallExpression<FrameToken>(() => () =>
                CallFrames.EnterScope(null, "", "", 0, 0), context, fileName, function, Expression.Constant(line), Expression.Constant(column));

    public static Expression Pop(Expression exp, Expression context) =>
        NewLambdaExpression.StaticCallExpression(() => () => CallFrames.Pop(null, default), context, exp);
}
