using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;

namespace Broiler.JavaScript.Compiler;

partial class FastCompiler
{
    // Root of an optional chain: the inner member/index/call chain is compiled with
    // skip-aware links that propagate a short-circuit via the skip sentinel; here that
    // sentinel is converted back to the observable `undefined`.
    protected override YExpression VisitOptionalChain(AstOptionalChain node)
        => JSValueBuilder.UnwrapOptionalChain(VisitExpression(node.Expression));
}
