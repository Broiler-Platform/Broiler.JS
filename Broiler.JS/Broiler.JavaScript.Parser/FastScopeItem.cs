using System.Collections.Generic;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.ExpressionCompiler.Core;

namespace Broiler.JavaScript.Parser;

/// <summary>
/// One scope of <see cref="FastScope"/>, as a facade (window W2 of milestone SJL-14; decision D50, <c>facade-obsolete</c>
/// in bundle <c>sjl-14-001</c>'s <c>dispositions.md</c>): it keeps every public member as a record of the names declared
/// in it, and refuses nothing.
/// </summary>
/// <remarks>
/// Until W2 the old parser filled these scopes as it parsed and <see cref="AddVariable"/> carried its declaration
/// refusals (the sites of the catalogue's EE0040, EE0062, EE0168, EE0169 and EG0004); that half was the parser's and goes
/// with it (SJL-14.5's deletion list). The shared syntax library checks those rows at the parse, and the compiler reads
/// the hoisting scopes the bridge builds from the library's side tables (decision D44); nothing in the engine reads this
/// type.
/// </remarks>
[System.Obsolete(ObsoleteMarks.Parser, DiagnosticId = ObsoleteMarks.DiagnosticId)]
public partial class FastScopeItem(FastNodeType nodeType) : LinkedStackItem<FastScopeItem>
{
    private readonly Dictionary<string, (StringSpan name, FastVariableKind kind)> declared = [];
    private readonly List<StringSpan> annexB = [];
    private readonly HashSet<string> varNames = [];
    public readonly FastNodeType NodeType = nodeType;

    /// <summary>True for an arrow function's scope.</summary>
    public bool IsArrow;

    /// <summary>Records <paramref name="name"/> as a VarDeclaredName of this scope.</summary>
    public void MarkVarDeclaredName(string name) => varNames.Add(name);

    /// <summary>Whether <paramref name="name"/> was recorded as a VarDeclaredName of this scope.</summary>
    public bool HasVarDeclaredName(string name) => varNames.Contains(name);

    /// <summary>Records a block-nested function declaration's name for Annex B 3.3, once.</summary>
    public void AddAnnexBName(in StringSpan name)
    {
        if (name.IsNullOrWhiteSpace())
            return;

        foreach (var existing in annexB)
        {
            if (existing.Value == name.Value)
                return;
        }

        annexB.Add(name);
    }

    /// <summary>The Annex B names recorded, in the order they were.</summary>
    public IFastEnumerable<StringSpan> GetAnnexBNames()
    {
        if (annexB.Count == 0)
            return Sequence<StringSpan>.Empty;

        var list = new Sequence<StringSpan>();
        foreach (var name in annexB)
            list.Add(name);

        return list;
    }

    /// <summary>Whether this scope records <paramref name="name"/> as a lexical (let, const, class or block function) binding.</summary>
    public bool HasLexicalBinding(in StringSpan name)
        => declared.TryGetValue(name.Value, out var v)
            && v.kind is FastVariableKind.Let or FastVariableKind.Const or FastVariableKind.Function;

    /// <summary>Whether this scope records <paramref name="name"/> as any kind of binding.</summary>
    public bool DeclaresVariable(in StringSpan name) => declared.ContainsKey(name.Value);

    /// <summary>
    /// Records <paramref name="name"/> as a binding of <paramref name="kind"/> in this scope. Since window W2 it refuses no
    /// declaration: <paramref name="token"/> and <paramref name="throwError"/> are kept for the signature's sake.
    /// </summary>
    public void AddVariable(FastToken token, in StringSpan name, FastVariableKind kind = FastVariableKind.Var, bool throwError = true)
    {
        if (name.IsNullOrWhiteSpace())
            return;

        if (kind == FastVariableKind.Var)
            varNames.Add(name.Value);

        declared[name.Value] = (name, kind);
    }

    /// <summary>The names this scope records.</summary>
    public IFastEnumerable<StringSpan> GetVariables()
    {
        if (declared.Count == 0)
            return Sequence<StringSpan>.Empty;

        var list = new Sequence<StringSpan>();
        foreach (var (_, value) in declared)
            list.Add(value.name);

        return list;
    }
}
