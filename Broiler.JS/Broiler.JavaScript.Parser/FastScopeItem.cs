using System.Collections.Generic;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.ExpressionCompiler.Core;

namespace Broiler.JavaScript.Parser;

public partial class FastScopeItem(FastNodeType nodeType) : LinkedStackItem<FastScopeItem>
{
    private Dictionary<string, (StringSpan name, FastVariableKind kind)> Variables = new();
    public readonly FastNodeType NodeType = nodeType;

    private List<StringSpan> annexBFunctionNames;

    /// <summary>
    /// Records a block-nested function declaration name that must be var-hoisted
    /// to this (function/program body) scope per Annex B 3.3.
    /// </summary>
    public void AddAnnexBName(in StringSpan name)
    {
        if (name.IsNullOrWhiteSpace())
            return;

        annexBFunctionNames ??= new List<StringSpan>();
        foreach (var existing in annexBFunctionNames)
        {
            if (existing.Value == name.Value)
                return;
        }

        annexBFunctionNames.Add(name);
    }

    public IFastEnumerable<StringSpan> GetAnnexBNames()
    {
        if (annexBFunctionNames == null || annexBFunctionNames.Count == 0)
            return Sequence<StringSpan>.Empty;

        var list = new Sequence<StringSpan>();
        foreach (var name in annexBFunctionNames)
            list.Add(name);

        return list;
    }

    /// <summary>
    /// Returns true if this scope declares <paramref name="name"/> as a lexical
    /// (let/const/class) binding.
    /// </summary>
    public bool HasLexicalBinding(in StringSpan name)
        => Variables.TryGetValue(name.Value, out var v)
            && v.kind is FastVariableKind.Let or FastVariableKind.Const;

    /// <summary>
    /// Returns true if this scope declares <paramref name="name"/> as any kind
    /// of binding. For a FunctionExpression scope these are exactly the formal
    /// parameter names.
    /// </summary>
    public bool DeclaresVariable(in StringSpan name)
        => Variables.ContainsKey(name.Value);

    public void AddVariable(FastToken token, in StringSpan name, FastVariableKind kind = FastVariableKind.Var, bool throwError = true)
    {
        if (name.IsNullOrWhiteSpace())
            return;

        var n = this;

        while (n != null)
        {
            if (n.Variables.TryGetValue(name.Value, out var pn))
            {
                if (pn.kind != FastVariableKind.Var)
                {
                    if (throwError)
                    {
                        throw new FastParseException(token, $"{name} is already defined in current scope at {token.Start}");
                    }
                    return;
                }
            }

            break;
        }

        // Per spec, let/const declarations in a function body must not
        // shadow parameters: VarDeclaredNames and LexicallyDeclaredNames
        // must not overlap.  Parameters live in the parent function scope
        // while body declarations live in the block scope just below it.
        if (kind is FastVariableKind.Let or FastVariableKind.Const
            && NodeType == FastNodeType.Block
            && Parent is { NodeType: FastNodeType.FunctionExpression } parentScope
            && parentScope.Variables.ContainsKey(name.Value))
        {
            if (throwError)
                throw new FastParseException(token, $"{name} has already been declared");
            return;
        }

        n = this;

        // all `var` variables must be hoisted to
        // to top most scope
        if (kind == FastVariableKind.Var)
        {
            // in case of var...
            // find an existing declaration of the same name, but only within the
            // CURRENT function's var-hoisting region. A `var` is hoisted to the
            // nearest enclosing function/program scope, so the search must stop at
            // the function boundary: a same-named binding in an enclosing function
            // (or the global scope) is a *different* variable and must not absorb
            // this declaration — otherwise `var x` inside a function whose name
            // collides with an outer `var x`/`let x` would never be registered and
            // would wrongly resolve to the outer binding (including reads before its
            // own declaration, which must see the hoisted `undefined`).
            var it = n;

            while (it != null)
            {
                if (it.Variables.TryGetValue(name.Value, out var v))
                    return;

                // The FunctionExpression scope (which owns the parameters) is the
                // outermost scope of the current function; checking it lets a `var`
                // dedupe against a parameter of the same name, but we go no further.
                if (it.NodeType == FastNodeType.FunctionExpression)
                    break;

                it = it.Parent;
            }

            while (true)
            {
                if (n.Parent == null)
                    break;

                // `var` hoists out of nested blocks and for/for-in/for-of head
                // scopes toward the nearest function/program (var-hoisting) scope.
                // A `var` in a for-of/for-in body lives in the ForStatement scope,
                // so that scope must be climbable too — otherwise the binding is
                // stranded there and never reaches the function's HoistingScope.
                if (n.NodeType is FastNodeType.Block or FastNodeType.ForStatement
                    && n.Parent.NodeType is FastNodeType.Block or FastNodeType.ForStatement)
                {
                    n = n.Parent;
                    continue;
                }

                break;
            }
        }

        n.Variables[name.Value] = (name, kind);
    }

    public IFastEnumerable<StringSpan> GetVariables()
    {
        var list = new Sequence<StringSpan>();

        foreach (var (_, Value) in Variables)
            list.Add(Value.name);

        if (list.Count == 0)
            return Sequence<StringSpan>.Empty;

        return list;
    }
}
