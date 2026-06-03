using Broiler.JavaScript.Ast.Statements;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Compiler;

partial class FastCompiler
{
    // In doWhile continue should preced the test
    protected override YExpression VisitDoWhileStatement(AstDoWhileStatement doWhileStatement, string label = null)
    {
        var breakTarget = YExpression.Label();
        var continueTarget = YExpression.Label();
        var completionVar = YExpression.Variable(typeof(JSValue), "#cv");
        var outerCompletionVars = GetCompletionVariables();

        using var completion = completionScopes.Push(completionVar);
        using var s = scope.Top.Loop.Push(new LoopScope(breakTarget, continueTarget, false, label) { CompletionVariable = completionVar });
        var body = TrackCompletion(VisitStatement(doWhileStatement.Body));
        var test = YExpression.Not(JSValueBuilder.BooleanValue(VisitExpression(doWhileStatement.Test)));
        var loop = YExpression.Loop(YExpression.Block(body, YExpression.Label(continueTarget), YExpression.IfThen(test, YExpression.Goto(breakTarget))), breakTarget, null);

        return YExpression.Block(
            new Sequence<YParameterExpression> { completionVar },
            YExpression.Assign(completionVar, JSUndefinedBuilder.Value),
            YExpression.TailCallTransparentTryFinally(loop, PropagateCompletion(completionVar, outerCompletionVars)),
            completionVar);
    }
}
