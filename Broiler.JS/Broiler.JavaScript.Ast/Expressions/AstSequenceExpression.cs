using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.ExpressionCompiler.Core;

namespace Broiler.JavaScript.Ast.Expressions;

// Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=5; Fingerprint=TBF
// Broiler-Human:        PENDING
public class AstSequenceExpression : AstExpression
{
    public readonly IFastEnumerable<AstExpression> Expressions;

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=0; Fingerprint=TBF
    // Broiler-Human:        PENDING
    public AstSequenceExpression(FastToken start, FastToken end, IFastEnumerable<AstExpression> expressions) : base(start, FastNodeType.SequenceExpression, end) =>
        Expressions = expressions;

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=0; Fingerprint=TBF
    // Broiler-Falsified-If: a sequence built from an empty expression list throws NullReferenceException from the first element's Start lookup instead of being refused with a parse error
    // Broiler-Human:        PENDING
    public AstSequenceExpression(IFastEnumerable<AstExpression> expressions) :
        base(expressions.FirstOrDefault().Start, FastNodeType.SequenceExpression, expressions.LastOrDefault().End) => Expressions = expressions;

    public override string ToString() => Expressions.Join();
}
