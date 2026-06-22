using Broiler.JavaScript.Ast.Statements;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Compiler;

partial class FastCompiler
{
    protected override BExpression VisitWhileStatement(AstWhileStatement whileStatement, string label = null)
    {
        var breakTarget = BExpression.Label();
        var continueTarget = BExpression.Label();
        var completionVar = BExpression.Variable(typeof(JSValue), "#cv");
        var outerCompletionVars = GetCompletionVariables();

        using var completion = completionScopes.Push(completionVar);
        using var s = scope.Top.Loop.Push(new LoopScope(breakTarget, continueTarget, false, label) { CompletionVariable = completionVar });
        var body = TrackCompletion(Visit(whileStatement.Body));
        var test = BExpression.Not(JSValueBuilder.BooleanValue(Visit(whileStatement.Test)));
        var loop = BExpression.Loop(BExpression.Block(BExpression.IfThen(test, BExpression.Goto(breakTarget)), body), breakTarget, continueTarget);

        return BExpression.Block(
            new Sequence<BParameterExpression> { completionVar },
            BExpression.Assign(completionVar, JSUndefinedBuilder.Value),
            BExpression.TailCallTransparentTryFinally(loop, PropagateCompletion(completionVar, outerCompletionVars)),
            completionVar);
    }
}
