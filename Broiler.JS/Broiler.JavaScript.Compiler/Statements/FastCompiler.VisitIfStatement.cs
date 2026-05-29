
using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Statements;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;
using Broiler.JavaScript.LinqExpressions.Utils;

namespace Broiler.JavaScript.Compiler;

partial class FastCompiler
{
    protected override YExpression VisitIfStatement(AstIfStatement ifStatement)
    {
        var completionVar = YExpression.Variable(typeof(JSValue), "#cv");
        var outerCompletionVars = GetCompletionVariables();
        using var completion = completionScopes.Push(completionVar);
        var test = JSValueBuilder.BooleanValue(VisitExpression(ifStatement.Test));
        var trueCase = ifStatement.True is AstExpressionStatement { Expression: AstFunctionExpression trueFunctionDeclaration }
            ? TrackCompletion(VisitRuntimeFunctionDeclaration(trueFunctionDeclaration).ToJSValue())
            : TrackCompletion(VisitStatement(ifStatement.True).ToJSValue());

        YExpression result;
        if (ifStatement.False != null)
        {
            var elseCase = ifStatement.False is AstExpressionStatement { Expression: AstFunctionExpression falseFunctionDeclaration }
                ? TrackCompletion(VisitRuntimeFunctionDeclaration(falseFunctionDeclaration).ToJSValue())
                : TrackCompletion(VisitStatement(ifStatement.False).ToJSValue());
            result = YExpression.Condition(test, trueCase, elseCase);
        }
        else
        {
            result = YExpression.Condition(test, trueCase, JSUndefinedBuilder.Value);
        }

        return YExpression.Block(
            new Sequence<YParameterExpression> { completionVar },
            YExpression.Assign(completionVar, JSUndefinedBuilder.Value),
            YExpression.TryFinally(result, PropagateCompletion(completionVar, outerCompletionVars)),
            completionVar);
    }
}
