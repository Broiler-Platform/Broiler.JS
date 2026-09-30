using Broiler.JavaScript.Ast.Misc;

namespace Broiler.JavaScript.Ast.Expressions;

// Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=1; Fingerprint=TBF
// Broiler-Human:        PENDING
public class AstEmptyExpression(FastToken start, bool isBinding = false) : AstExpression(start, FastNodeType.EmptyExpression, start, isBinding)
{
    public override string ToString() => "<<Empty>>";
}
