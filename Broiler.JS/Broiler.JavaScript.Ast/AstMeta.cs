using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;

namespace Broiler.JavaScript.Ast;

// Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=0; Fingerprint=TBF
// Broiler-Falsified-If: Identifier and Property are swapped, so `new.target` is lowered as a meta property named `new` on `target`
// Broiler-Human:        PENDING
public class AstMeta(AstIdentifier id, AstIdentifier property) : AstExpression(id.Start, FastNodeType.Meta, property.End)
{
    public readonly AstIdentifier Identifier = id;
    public readonly AstIdentifier Property = property;
}
