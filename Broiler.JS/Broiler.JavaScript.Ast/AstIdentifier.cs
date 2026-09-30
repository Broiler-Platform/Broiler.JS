using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;

namespace Broiler.JavaScript.Ast;


// Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=3; Fingerprint=TBF
// Broiler-Falsified-If: an identifier spelled with a Unicode escape such as `\u0061` is named by its raw escape text instead of `a`, so it binds a different variable than the plain spelling
// Broiler-Human:        PENDING
public class AstIdentifier : AstExpression
{
    public readonly StringSpan Name;

    // The binding name to use for NamedEvaluation (anonymous function name inference),
    // when it differs from the storage name. The `for` desugarer renames pattern binding
    // identifiers to synthetic numeric temps (so they don't collide with the per-iteration
    // copies); without preserving the original name here, an anonymous initializer like
    // `for (const [f = () => {}] = []; ...)` would be named after the temp ("2") instead
    // of the binding ("f"). Defaults to Name; only the desugarer overrides it.
    private string? inferenceName;
    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=3; Fingerprint=TBF
    // Broiler-Falsified-If: an identifier whose inference name was never set returns something other than its Name, so `let f = () => {}` gets a function name other than `f`
    // Broiler-Human:        PENDING
    public string InferenceName
    {
        get => inferenceName ?? Name.Value;
        set => inferenceName = value;
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=0; Fingerprint=TBF
    // Broiler-Falsified-If: an identifier spelled with a Unicode escape such as `\u0061` is named by its raw escape text instead of `a`, so it binds a different variable than the plain spelling
    // Broiler-Human:        PENDING
    public AstIdentifier(FastToken identifier) : base(identifier, FastNodeType.Identifier, identifier) => Name = identifier.CookedText ?? identifier.Span;

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=0; Fingerprint=TBF
    // Broiler-Falsified-If: a synthetic identifier built from a token and a name reports the token's source text as its Name instead of the given string
    // Broiler-Human:        PENDING
    public AstIdentifier(FastToken token, string id) : base(token, FastNodeType.Identifier, token) => Name = id;

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Low; Resources=3; Fingerprint=TBF
    // Broiler-Falsified-If: an identifier renders as text other than its Name, such as the raw escape spelling of a cooked name
    // Broiler-Human:        PENDING
    public override string ToString() => Name.Value;
}
