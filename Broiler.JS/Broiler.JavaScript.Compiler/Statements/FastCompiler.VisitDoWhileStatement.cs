using Broiler.JavaScript.Ast.Statements;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Compiler;

partial class FastCompiler
{
    // In doWhile continue should preced the test
    protected override BExpression VisitDoWhileStatement(AstDoWhileStatement doWhileStatement, string label = null)
    {
        var breakTarget = BExpression.Label();
        var continueTarget = BExpression.Label();
        var completionVar = BExpression.Variable(typeof(JSValue), "#cv");
        var outerCompletionVars = GetCompletionVariables();

        using var completion = completionScopes.Push(completionVar);
        using var s = scope.Top.Loop.Push(new LoopScope(breakTarget, continueTarget, false, label) { CompletionVariable = completionVar });
        var body = TrackCompletion(VisitStatement(doWhileStatement.Body));
        var test = BExpression.Not(JSValueBuilder.BooleanValue(VisitExpression(doWhileStatement.Test)));
        var loop = BExpression.Loop(BExpression.Block(body, BExpression.Label(continueTarget), BExpression.IfThen(test, BExpression.Goto(breakTarget))), breakTarget, null);

        return BExpression.Block(
            new Sequence<BParameterExpression> { completionVar },
            BExpression.Assign(completionVar, JSUndefinedBuilder.Value),
            BExpression.TailCallTransparentTryFinally(loop, PropagateCompletion(completionVar, outerCompletionVars)),
            completionVar);
    }
}
