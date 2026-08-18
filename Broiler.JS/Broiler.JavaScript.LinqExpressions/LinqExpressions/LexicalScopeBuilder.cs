using Expression = Broiler.JavaScript.ExpressionCompiler.Expressions.BExpression;
using Broiler.JavaScript.LinqExpressions.LambdaGen;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.LinqExpressions.LinqExpressions;

public class LexicalScopeBuilder
{
    // code/codeLength are the program's whole source: a program scope has no position of its
    // own, but it is how the walk recognises the code running inside it (CallFrameStack.
    // IsLexicallyVisible). Passing nothing leaves the frame unlocated, and visible to all.
    public static Expression NewScope(Expression context, Expression fileName, Expression function, int line, int column, bool suspendable = false, Expression code = null, int codeLength = 0)
    {
        code ??= Expression.Constant(null, typeof(string));
        return suspendable
            ? NewLambdaExpression.StaticCallExpression<FrameToken>(() => () =>
                CallFrames.EnterSuspendableScope(null, "", "", 0, 0, null, 0), context, fileName, function, Expression.Constant(line), Expression.Constant(column), code, Expression.Constant(codeLength))
            : NewLambdaExpression.StaticCallExpression<FrameToken>(() => () =>
                CallFrames.EnterScope(null, "", "", 0, 0, null, 0), context, fileName, function, Expression.Constant(line), Expression.Constant(column), code, Expression.Constant(codeLength));
    }

    public static Expression Pop(Expression exp, Expression context) =>
        NewLambdaExpression.StaticCallExpression(() => () => CallFrames.Pop(null, default), context, exp);
}
