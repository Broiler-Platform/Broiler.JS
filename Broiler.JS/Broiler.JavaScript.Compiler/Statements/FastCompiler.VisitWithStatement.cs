using Broiler.JavaScript.Ast.Statements;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;

namespace Broiler.JavaScript.Compiler;

partial class FastCompiler
{
    protected override YExpression VisitWithStatement(AstWithStatement withStatement)
        => YExpression.Block(VisitExpression(withStatement.Object), Visit(withStatement.Body) ?? YExpression.Empty);
}
