using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;

namespace Broiler.JavaScript.Ast;


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
    public string InferenceName
    {
        get => inferenceName ?? Name.Value;
        set => inferenceName = value;
    }

    public AstIdentifier(FastToken identifier) : base(identifier, FastNodeType.Identifier, identifier) => Name = identifier.CookedText ?? identifier.Span;

    public AstIdentifier(FastToken token, string id) : base(token, FastNodeType.Identifier, token) => Name = id;

    public override string ToString() => Name.Value;
}
