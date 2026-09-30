using Broiler.JavaScript.Ast.Misc;

namespace Broiler.JavaScript.Ast.Expressions;

// Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=5; Fingerprint=TBF
// Broiler-Human:        PENDING
public class AstConditionalExpression(AstExpression previous, AstExpression @true, AstExpression @false) : AstExpression(previous.Start, FastNodeType.ConditionalExpression, @false.End)
{
    public readonly AstExpression Test = previous;
    public readonly AstExpression True = @true;
    public readonly AstExpression False = @false;

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Low; Resources=5; Fingerprint=TBF
    // Broiler-Human:        PENDING
    public override string ToString() => $"{Test} ? {True} : {False}";
}
