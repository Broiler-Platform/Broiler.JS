
using Broiler.JavaScript.Ast.Statements;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;

namespace Broiler.JavaScript.Compiler;

partial class FastCompiler
{
    // `return expr` in an async generator awaits its operand before completing (ReturnStatement
    // evaluation: "If GetGeneratorKind() is async, set exprValue to ? Await(exprValue)"), so the
    // generator completes with the settled value and a rejection is thrown at the `return`. A bare
    // `return` does not await.
    protected override BExpression VisitReturnStatement(AstReturnStatement returnStatement)
    {
        if (returnStatement.Argument == null)
            return BExpression.Return(scope.Top.ReturnLabel, JSUndefinedBuilder.Value);

        var value = VisitConsumedBy(returnStatement.Argument, Runtime.NumberBoxingConversionSite.GuardedTreeRootIntoReturn);
        if (scope.Top.Function is { Async: true, Generator: true })
            value = BExpression.Await(value);

        return BExpression.Return(scope.Top.ReturnLabel, value);
    }
}
