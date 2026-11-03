#nullable enable

using System.Collections.Generic;
using Broiler.ECMAScript.Semantics;
using Broiler.ECMAScript.Syntax;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.ExpressionCompiler.Core;

namespace Broiler.JavaScript.Parser.Bridge;

/// <summary>
/// The scope facts the bridge gives this engine's blocks (milestone SJL-14.2, decision D44): each block's
/// <c>HoistingScope</c> from the library's declarations side table, and each function body's and program's
/// <c>AnnexBFunctionNames</c> by the old parser's rule, over a stack of scopes the walk keeps in source order.
/// </summary>
/// <remarks>
/// <para>
/// <b>HoistingScope</b> is the names a scope of the side table declares, in source order, each once, as the old
/// parser's <c>FastScopeItem</c> enumerated its dictionary in insertion order: a function's or a program's
/// var-scoped and lexical names (its <c>var</c>s from every nested block among them), a block's or a
/// <c>switch</c>'s lexical names, an Annex B.3.4 block's function. Import bindings and a module's
/// <c>*default*</c> are left out, as the old parser registered neither; so is a <c>var</c> or a top-level function
/// whose name is a non-arrow function's parameter, which the old parser merged into the parameter (an arrow's
/// parameters it never registered).
/// </para>
/// <para>
/// <b>AnnexBFunctionNames</b> follows the old parser's <c>RegisterAnnexBFunctionHoisting</c>: a plain function
/// declared in a nested block, or as an <c>if</c> clause, is hoisted to the nearest function body or program unless a
/// parameter of that function has its name, the name is <c>arguments</c> in a non-arrow function, or a lexical
/// declaration of the name that the walk has already passed stands in a scope between them; strictness is the
/// compiler's to apply. One departure: a class static block ends the search (B.3.3 changes no static block), where the
/// old parser, which gave a static block no scope boundary, searched on into the enclosing function.
/// </para>
/// </remarks>
internal sealed class BridgeScopes
{
    private readonly Dictionary<SyntaxNode, Scope> scopes = new(System.Collections.Generic.ReferenceEqualityComparer.Instance);
    private readonly Dictionary<SyntaxNode, FunctionFacts> functions = new(System.Collections.Generic.ReferenceEqualityComparer.Instance);
    private readonly BridgeTokens tokens;
    private Frame? top;

    internal BridgeScopes(DeclarationTables tables, BridgeTokens tokens)
    {
        this.tokens = tokens;
        foreach (var scope in tables.Scopes)
            scopes.TryAdd(scope.Node, scope);
        foreach (var facts in tables.Functions)
            functions.TryAdd(facts.Node, facts);
    }

    private enum FrameKind
    {
        Function,
        Block,
        For,
    }

    private sealed class Frame(FrameKind kind, Frame? parent)
    {
        internal readonly FrameKind Kind = kind;
        internal readonly Frame? Parent = parent;
        internal bool IsArrow;
        internal bool IsStaticBlock;
        internal HashSet<string>? Lexical;
        internal HashSet<string>? Parameters;
        internal Sequence<StringSpan>? AnnexB;

        // A function body or a program: the old parser's hoisting target.
        internal bool IsBody => Kind == FrameKind.Block && (Parent is null || Parent.Kind == FrameKind.Function);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // The walk's scopes.

    internal void EnterFunction(bool isArrow) => top = new Frame(FrameKind.Function, top) { IsArrow = isArrow };

    internal void LeaveFunction() => top = top!.Parent;

    /// <summary>A function body, or the program when no function is entered.</summary>
    internal void EnterBody() => top = new Frame(FrameKind.Block, top);

    /// <summary>Leaves a function body or the program, answering its Annex B names.</summary>
    internal IFastEnumerable<StringSpan> LeaveBody()
    {
        var names = top!.AnnexB;
        top = top.Parent;
        return names is { Count: > 0 } ? names : Sequence<StringSpan>.Empty;
    }

    internal void EnterBlock() => top = new Frame(FrameKind.Block, top);

    internal void EnterStaticBlock() => top = new Frame(FrameKind.Block, top) { IsStaticBlock = true };

    internal void LeaveBlock() => top = top!.Parent;

    internal void EnterFor() => top = new Frame(FrameKind.For, top);

    internal void LeaveFor() => top = top!.Parent;

    /// <summary>A name the walk has passed, declared as the old parser's <c>AddVariable</c> would have.</summary>
    internal void Declare(string name, FastVariableKind kind)
    {
        if (top is null)
            return;
        if (top.Kind == FrameKind.Function)
        {
            (top.Parameters ??= new HashSet<string>(System.StringComparer.Ordinal)).Add(name);
            return;
        }

        if (kind is FastVariableKind.Let or FastVariableKind.Const or FastVariableKind.Function)
            (top.Lexical ??= new HashSet<string>(System.StringComparer.Ordinal)).Add(name);
    }

    /// <summary>A function declaration's name, as the old parser's <c>FunctionExpression</c> registered it.</summary>
    internal void DeclareFunction(in StringSpan name, bool plain, bool ifClause)
    {
        if (top is null)
            return;
        if (plain)
            RegisterAnnexB(name, ifClause);
        if (ifClause)
            return;
        var kind = top.IsBody ? FastVariableKind.Var : FastVariableKind.Function;
        Declare(name.Value ?? string.Empty, kind);
    }

    /// <summary>Whether the nearest function's parameters bind <paramref name="name"/> (the old parser's NestedFunctionShadowsParameter).</summary>
    internal bool ShadowsParameter(string name)
    {
        var frame = top;
        while (frame != null && frame.Kind != FrameKind.Function)
            frame = frame.Parent;
        return frame?.Parameters?.Contains(name) == true;
    }

    private void RegisterAnnexB(in StringSpan name, bool ifClause)
    {
        var value = name.Value ?? string.Empty;
        var target = top;
        while (target != null && !target.IsBody)
        {
            if (target.IsStaticBlock)
                return;
            target = target.Parent;
        }

        if (target == null || (target == top && !ifClause))
            return;
        if (target.Parent is { Kind: FrameKind.Function } function)
        {
            if (function.Parameters?.Contains(value) == true)
                return;
            if (value == "arguments" && !function.IsArrow)
                return;
        }

        for (var scope = top; scope != null; scope = scope.Parent)
        {
            if (scope.Lexical?.Contains(value) == true)
                return;
            if (scope == target)
                break;
        }

        target.AnnexB ??= new Sequence<StringSpan>();
        var known = target.AnnexB.GetFastEnumerator();
        while (known.MoveNext(out var existing))
        {
            if (existing.Value == value)
                return;
        }

        target.AnnexB.Add(name);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // The side table.

    /// <summary>The names <paramref name="node"/>'s scope declares, in source order, each once.</summary>
    internal IFastEnumerable<StringSpan> Hoisting(SyntaxNode node)
    {
        if (!scopes.TryGetValue(node, out var scope) || scope.Declarations.Count == 0)
            return Sequence<StringSpan>.Empty;

        IReadOnlyList<string>? parameters = null;
        if (scope.Kind == ScopeKind.Function && node is FunctionNode { IsArrow: false } && functions.TryGetValue(node, out var facts))
            parameters = facts.ParameterNames;

        var list = new Sequence<StringSpan>();
        var seen = new HashSet<string>(System.StringComparer.Ordinal);
        foreach (var declaration in scope.Declarations)
        {
            if (declaration.Kind is BindingKind.Import or BindingKind.ExportDefault)
                continue;
            if (parameters != null && !declaration.IsLexical && Contains(parameters, declaration.Name))
                continue;
            if (!seen.Add(declaration.Name))
                continue;
            list.Add(NameSpan(declaration.Node, declaration.Name));
        }

        return list.Count == 0 ? Sequence<StringSpan>.Empty : list;
    }

    /// <summary>A declared name as the old parser held it: the source's span, or the cooked name where it is escaped.</summary>
    internal StringSpan NameSpan(SyntaxNode node, string name)
    {
        var span = node.Span;
        return span.Length == name.Length && string.CompareOrdinal(tokens.Source, span.Offset, name, 0, name.Length) == 0
            ? new StringSpan(tokens.Source, span.Offset, span.Length)
            : new StringSpan(name);
    }

    private static bool Contains(IReadOnlyList<string> names, string name)
    {
        foreach (var each in names)
        {
            if (each == name)
                return true;
        }

        return false;
    }
}
