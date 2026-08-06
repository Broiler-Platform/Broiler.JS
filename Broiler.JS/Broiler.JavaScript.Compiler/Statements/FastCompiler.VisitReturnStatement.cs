
using Broiler.JavaScript.Ast.Statements;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;

namespace Broiler.JavaScript.Compiler;

partial class FastCompiler
{
    protected override BExpression VisitReturnStatement(AstReturnStatement returnStatement) =>
        BExpression.Return(scope.Top.ReturnLabel, returnStatement.Argument != null
            ? VisitConsumedBy(returnStatement.Argument, Runtime.NumberBoxingConversionSite.GuardedTreeRootIntoReturn)
            : JSUndefinedBuilder.Value);
}
