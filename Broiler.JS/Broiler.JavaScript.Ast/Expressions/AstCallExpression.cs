using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.ExpressionCompiler.Core;

namespace Broiler.JavaScript.Ast.Expressions;

// Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=5; Fingerprint=TBF
// Broiler-Falsified-If: a plain call that follows an optional link, the trailing `()` in `a?.b()()`, is built with InOptionalChain false, so a nullish `a` throws instead of evaluating to undefined
// Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Low; Resources=5; Fingerprint=TBF
    // Broiler-Human:        PENDING
    public override string ToString() => $"{Callee}({Arguments.Join()})";
}
