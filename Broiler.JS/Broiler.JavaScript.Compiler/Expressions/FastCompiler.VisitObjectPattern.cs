using Broiler.JavaScript.Ast.Patterns;
using System;
using Broiler.JavaScript.ExpressionCompiler.Expressions;

namespace Broiler.JavaScript.Compiler;

partial class FastCompiler
{
    protected override BExpression VisitArrayPattern(AstArrayPattern arrayPattern) => throw new NotImplementedException();

    protected override BExpression VisitObjectPattern(AstObjectPattern objectPattern) => throw new NotImplementedException();
}
