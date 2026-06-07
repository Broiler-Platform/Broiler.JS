using Broiler.JavaScript.Ast.Statements;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;
using Broiler.JavaScript.Runtime;
using System;

namespace Broiler.JavaScript.Compiler;

partial class FastCompiler
{
    protected override YExpression VisitWithStatement(AstWithStatement withStatement)
    {
        var completionVar = YExpression.Variable(typeof(JSValue), "#cv");
        var outerCompletionVars = GetCompletionVariables();
        using var completion = completionScopes.Push(completionVar);
        withBoundaries.Push(scope.Top);
        YExpression body;
        try
        {
            body = TrackCompletion(Visit(withStatement.Body)) ?? YExpression.Empty;
        }
        finally
        {
            withBoundaries.Pop();
        }

        var withBindings = YExpression.Parameter(typeof(IDisposable), "#withBindings");
        var withScope = YExpression.Parameter(typeof(IDisposable), "#withScope");
        return YExpression.Block(
            new Sequence<YParameterExpression> { withBindings, withScope, completionVar },
            YExpression.Assign(completionVar, JSUndefinedBuilder.Value),
            YExpression.Assign(withBindings, JSContextBuilder.PushWithFallbackScope(CaptureWithFallbackBindings(), CaptureWithFallbackShadowedBindings())),
            YExpression.Assign(withScope, JSContextBuilder.PushWithScope(VisitExpression(withStatement.Object))),
            YExpression.TryFinally(
                YExpression.TryFinally(
                    YExpression.TryFinally(body, YExpression.Call(withScope, DisposeMethod)),
                    PropagateCompletion(completionVar, outerCompletionVars)),
                YExpression.Call(withBindings, DisposeMethod)),
            completionVar);
    }
}
