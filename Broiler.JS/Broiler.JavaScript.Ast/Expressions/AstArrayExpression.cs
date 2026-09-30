using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.ExpressionCompiler.Core;

namespace Broiler.JavaScript.Ast.Expressions;

// Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=5; Fingerprint=TBF
// Broiler-Human:        PENDING
public class AstArrayExpression(FastToken start, FastToken end, IFastEnumerable<AstExpression> nodes) : AstExpression(start, FastNodeType.ArrayExpression, end)
{
    public readonly IFastEnumerable<AstExpression> Elements = nodes;

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Low; Resources=5; Fingerprint=TBF
    // Broiler-Human:        PENDING
    public override string ToString() => $"[{Elements.Join()}]";
}
