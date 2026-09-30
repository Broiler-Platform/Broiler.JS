using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.ExpressionCompiler.Core;

namespace Broiler.JavaScript.Ast.Expressions;

// Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=5; Fingerprint=TBF
// Broiler-Human:        PENDING
public class AstNewExpression(FastToken begin, AstExpression node, IFastEnumerable<AstExpression> arguments) : AstExpression(begin, FastNodeType.NewExpression, node.End)
{
    public readonly AstExpression Callee = node;
    public readonly IFastEnumerable<AstExpression> Arguments = arguments;

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Low; Resources=5; Fingerprint=TBF
    // Broiler-Human:        PENDING
    public override string ToString() => $"new {Callee}({Arguments.Join()})";
}
