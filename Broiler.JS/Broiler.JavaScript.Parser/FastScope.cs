using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.ExpressionCompiler.Core;

namespace Broiler.JavaScript.Parser;

/// <summary>
/// The old parser's scope stack, as a facade (window W2 of milestone SJL-14; decision D50, <c>facade-obsolete</c>): its
/// members are kept, and nothing in the engine reads it; the bridge builds the tree's scopes from the shared syntax
/// library's side tables (decision D44).
/// </summary>
[System.Obsolete(ObsoleteMarks.Parser, DiagnosticId = ObsoleteMarks.DiagnosticId)]
public partial class FastScope : LinkedStack<FastScopeItem>
{
    public FastScope()
    {
    }

    /// <summary>Pushes a new scope of <paramref name="nodeType"/>; <paramref name="token"/> is kept for the signature's sake.</summary>
    public FastScopeItem Push(FastToken token, FastNodeType nodeType) => Push(new FastScopeItem(nodeType));
}
