using Broiler.JavaScript.Ast.Misc;

namespace Broiler.JavaScript.Ast.Expressions;

// Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=1; Fingerprint=TBF
// Broiler-Human:        PENDING
public class AstAwaitExpression(FastToken token, FastToken previousToken, AstExpression target) : AstExpression(token, FastNodeType.AwaitExpression, previousToken)
{
    public readonly AstExpression Argument = target;
}
