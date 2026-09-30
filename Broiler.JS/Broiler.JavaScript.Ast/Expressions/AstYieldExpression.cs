using Broiler.JavaScript.Ast.Misc;

namespace Broiler.JavaScript.Ast.Expressions;

// Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=1; Fingerprint=TBF
// Broiler-Human:        PENDING
public class AstYieldExpression(FastToken token, FastToken previousToken, AstExpression target, bool @delegate = false) : AstExpression(token, FastNodeType.YieldExpression, previousToken)
{
    public readonly AstExpression Argument = target;
    public readonly bool Delegate = @delegate;
}
