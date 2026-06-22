
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
    protected override BExpression VisitIfStatement(AstIfStatement ifStatement)
    {
        var test = JSValueBuilder.BooleanValue(VisitExpression(ifStatement.Test));

        if (scope.Top.Function?.Generator == true)
        {
            var generatorTrueCase = ifStatement.True is AstExpressionStatement { Expression: AstFunctionExpression generatorTrueFunctionDeclaration }
                ? VisitRuntimeFunctionDeclaration(generatorTrueFunctionDeclaration, implicitBlockScoped: true).ToJSValue()
                : VisitStatement(ifStatement.True).ToJSValue();
            var generatorFalseCase = ifStatement.False == null
                ? JSUndefinedBuilder.Value
                : ifStatement.False is AstExpressionStatement { Expression: AstFunctionExpression generatorFalseFunctionDeclaration }
                    ? VisitRuntimeFunctionDeclaration(generatorFalseFunctionDeclaration, implicitBlockScoped: true).ToJSValue()
                    : VisitStatement(ifStatement.False).ToJSValue();

            return BExpression.Condition(test, generatorTrueCase, generatorFalseCase);
        }

        var completionVar = BExpression.Variable(typeof(JSValue), "#cv");
        var outerCompletionVars = GetCompletionVariables();
        using var completion = completionScopes.Push(completionVar);
        var trueCase = ifStatement.True is AstExpressionStatement { Expression: AstFunctionExpression trueFunctionDeclaration }
            ? TrackCompletion(VisitRuntimeFunctionDeclaration(trueFunctionDeclaration, implicitBlockScoped: true).ToJSValue())
            : TrackCompletion(VisitStatement(ifStatement.True).ToJSValue());

        BExpression result;
        if (ifStatement.False != null)
        {
            var elseCase = ifStatement.False is AstExpressionStatement { Expression: AstFunctionExpression falseFunctionDeclaration }
                ? TrackCompletion(VisitRuntimeFunctionDeclaration(falseFunctionDeclaration, implicitBlockScoped: true).ToJSValue())
                : TrackCompletion(VisitStatement(ifStatement.False).ToJSValue());
            result = BExpression.Condition(test, trueCase, elseCase);
        }
        else
        {
            result = BExpression.Condition(test, trueCase, JSUndefinedBuilder.Value);
        }

        return BExpression.Block(
            new Sequence<BParameterExpression> { completionVar },
            BExpression.Assign(completionVar, JSUndefinedBuilder.Value),
            BExpression.TailCallTransparentTryFinally(result, PropagateCompletion(completionVar, outerCompletionVars)),
            completionVar);
    }
}
