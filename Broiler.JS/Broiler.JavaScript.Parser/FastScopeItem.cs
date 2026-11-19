using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.ExpressionCompiler.Core;

namespace Broiler.JavaScript.Parser;

/// <summary>
/// One scope of <see cref="FastScope"/>, as a facade (window W2 of milestone SJL-14; decision D50, <c>facade-obsolete</c>
/// in bundle <c>sjl-14-001</c>'s <c>dispositions.md</c>): it keeps every public member, records nothing and refuses
/// nothing, and answers as a scope that declares no name.
/// </summary>
/// <remarks>
/// Until W2 the old parser filled these scopes as it parsed and <see cref="AddVariable"/> carried its declaration
/// refusals (the sites of the catalogue's EE0040, EE0062, EE0168, EE0169 and EG0004); that half was the parser's and goes
/// with it (SJL-14.5's deletion list). The shared syntax library checks those rows at the parse, and the compiler reads
/// the hoisting scopes the bridge builds from the library's side tables (decision D44); nothing in the engine reads this
/// type. (Changed in review of W2, 2026-11-19: the members kept a record of the names a caller added, a working name
/// record rather than a facade over the bridge; they now record nothing, since no parser fills them.)
/// </remarks>
[System.Obsolete(ObsoleteMarks.Parser, DiagnosticId = ObsoleteMarks.DiagnosticId)]
public partial class FastScopeItem(FastNodeType nodeType) : LinkedStackItem<FastScopeItem>
{
    public readonly FastNodeType NodeType = nodeType;

    /// <summary>True for an arrow function's scope.</summary>
    public bool IsArrow;

    /// <summary>Kept as a facade member; records nothing.</summary>
    public void MarkVarDeclaredName(string name)
    {
    }

    /// <summary>Kept as a facade member; false, since the facade records no name.</summary>
    public bool HasVarDeclaredName(string name) => false;

    /// <summary>Kept as a facade member; records nothing.</summary>
    public void AddAnnexBName(in StringSpan name)
    {
    }

    /// <summary>Kept as a facade member; empty, since the facade records no name.</summary>
    public IFastEnumerable<StringSpan> GetAnnexBNames() => Sequence<StringSpan>.Empty;

    /// <summary>Kept as a facade member; false, since the facade records no name.</summary>
    public bool HasLexicalBinding(in StringSpan name) => false;

    /// <summary>Kept as a facade member; false, since the facade records no name.</summary>
    public bool DeclaresVariable(in StringSpan name) => false;

    /// <summary>
    /// Kept as a facade member: records nothing and refuses no declaration; <paramref name="token"/>,
    /// <paramref name="name"/>, <paramref name="kind"/> and <paramref name="throwError"/> are kept for the signature's sake.
    /// </summary>
    public void AddVariable(FastToken token, in StringSpan name, FastVariableKind kind = FastVariableKind.Var, bool throwError = true)
    {
    }

    /// <summary>Kept as a facade member; empty, since the facade records no name.</summary>
    public IFastEnumerable<StringSpan> GetVariables() => Sequence<StringSpan>.Empty;
}
