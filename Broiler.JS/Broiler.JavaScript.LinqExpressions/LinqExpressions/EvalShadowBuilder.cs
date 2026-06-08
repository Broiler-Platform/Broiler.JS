using Broiler.JavaScript.Engine;
using Broiler.JavaScript.LinqExpressions.LambdaGen;
using Broiler.JavaScript.Runtime;
using Expression = Broiler.JavaScript.ExpressionCompiler.Expressions.YExpression;

namespace Broiler.JavaScript.LinqExpressions.LinqExpressions;

// Builders for the parameter-environment shadow bindings introduced by a sloppy
// direct eval in a function's parameter list (see EvalShadowVariable).
public static class EvalShadowBuilder
{
    public static Expression New(string name, Expression outer, bool outerIsGlobal) =>
        NewLambdaExpression.StaticCallExpression<JSVariable>(
            () => () => EvalShadowVariable.New("", null, false),
            Expression.Constant(name), outer, Expression.Constant(outerIsGlobal));

    public static Expression GetValue(Expression target) =>
        target.CallExpression<JSVariable, JSValue>(() => x => x.GetValue());

    public static Expression SetValue(Expression target, Expression value) =>
        target.CallExpression<JSVariable, JSValue, JSValue>(() => (x, v) => x.SetValue(v), value);

    public static Expression Register(Expression stackItem, Expression variable) =>
        stackItem.CallExpression<CallStackItem>(() => x => x.RegisterDirectEvalBinding(null), variable);

    // Reference-stable compound assignment (see JSVariable.CaptureReference).
    public static Expression CaptureReference(Expression target) =>
        target.CallExpression<JSVariable, bool>(() => x => x.CaptureReference());

    public static Expression GetCaptured(Expression target, Expression reference) =>
        target.CallExpression<JSVariable, bool, JSValue>(() => (x, r) => x.GetCaptured(r), reference);

    public static Expression SetCaptured(Expression target, Expression reference, Expression value) =>
        target.CallExpression<JSVariable, bool, JSValue, JSValue>(() => (x, r, v) => x.SetCaptured(r, v), reference, value);
}
