using Broiler.JavaScript.Ast.Statements;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;
using Broiler.JavaScript.Runtime;
using System;

namespace Broiler.JavaScript.Compiler;

partial class FastCompiler
{
    protected override BExpression VisitWithStatement(AstWithStatement withStatement)
    {
        var completionVar = BExpression.Variable(typeof(JSValue), "#cv");
        var outerCompletionVars = GetCompletionVariables();
        using var completion = completionScopes.Push(completionVar);
        withBoundaries.Push(scope.Top);
        BExpression body;
        try
        {
            body = TrackCompletion(Visit(withStatement.Body)) ?? BExpression.Empty;
        }
        finally
        {
            withBoundaries.Pop();
        }

        var withBindings = BExpression.Parameter(typeof(IDisposable), "#withBindings");
        var withScope = BExpression.Parameter(typeof(IDisposable), "#withScope");
        return BExpression.Block(
            new Sequence<BParameterExpression> { withBindings, withScope, completionVar },
            BExpression.Assign(completionVar, JSUndefinedBuilder.Value),
            BExpression.Assign(withBindings, JSContextBuilder.PushWithFallbackScope(CaptureWithFallbackBindings(), CaptureWithFallbackShadowedBindings())),
            BExpression.Assign(withScope, JSContextBuilder.PushWithScope(VisitExpression(withStatement.Object))),
            BExpression.TryFinally(
                BExpression.TryFinally(
                    BExpression.TryFinally(body, BExpression.Call(withScope, DisposeMethod)),
                    PropagateCompletion(completionVar, outerCompletionVars)),
                BExpression.Call(withBindings, DisposeMethod)),
            completionVar);
    }
}
