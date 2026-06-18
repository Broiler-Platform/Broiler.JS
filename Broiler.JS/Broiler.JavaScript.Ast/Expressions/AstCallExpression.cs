using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.ExpressionCompiler.Core;

namespace Broiler.JavaScript.Ast.Expressions;

public class AstCallExpression(AstExpression previous, IFastEnumerable<AstExpression> plist, bool coalesce = false, bool inOptionalChain = false) :
    AstExpression(previous.Start, FastNodeType.CallExpression, plist.Count > 0 ? plist.Last().End : previous.End)
{
    public readonly AstExpression Callee = previous;
    public readonly IFastEnumerable<AstExpression> Arguments = plist;

    // Coalesce: this call is `?.()` (short-circuits on a nullish callee).
    // InOptionalChain: this call sits inside an optional chain, so it propagates an
    // in-flight short-circuit (e.g. the trailing `()` in `a?.b()()` after a nullish `a`).
    public readonly bool Coalesce = coalesce;
    public readonly bool InOptionalChain = inOptionalChain || coalesce;

    public override string ToString() => $"{Callee}({Arguments.Join()})";
}
