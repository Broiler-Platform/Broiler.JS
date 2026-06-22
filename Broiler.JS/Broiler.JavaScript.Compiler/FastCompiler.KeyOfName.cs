using System.Collections.Generic;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Compiler;

partial class FastCompiler
{
    // Lexical stack of per-class-evaluation private-name keys: maps a private name
    // ("#x") to a class-scope variable holding the unique KeyString minted for the
    // enclosing class evaluation. Innermost class last; a reference resolves against
    // the nearest enclosing class so a nested class's `#x` shadows an outer one.
    private readonly List<Dictionary<string, BExpression>> privateNameScopes = [];

    // Monotonic suffix making each minted-key variable's name unique (closure
    // capture of the key — used by-address in private method calls — resolves by name).
    private static int privateKeyVarCounter;

    internal void PushPrivateNameScope(Dictionary<string, BExpression> names)
        => privateNameScopes.Add(names);

    internal void PopPrivateNameScope()
        => privateNameScopes.RemoveAt(privateNameScopes.Count - 1);

    // A private name (a `#x` IdentifierName — never a string-literal or computed
    // key) gets a property key in a marker-prefixed namespace so it cannot collide
    // with a same-spelled public `"#x"` string property. The declaration site
    // (GetName) and every reference (VisitMemberExpression) both route through here,
    // so they agree on the key; the runtime hides marker-prefixed keys from
    // reflection and enumeration (JSObject.IsPrivateName).
    //
    // When the name is declared by an enclosing class evaluation, it resolves to
    // that evaluation's minted-key variable (each `new`/factory call gets a distinct
    // brand). Otherwise (e.g. a direct-eval fragment with no class scope on the
    // compiler stack) it falls back to a stable marker-prefixed constant key.
    public BExpression KeyOfPrivateName(in StringSpan name)
    {
        for (var i = privateNameScopes.Count - 1; i >= 0; i--)
            if (privateNameScopes[i].TryGetValue(name.Value, out var keyVar))
                return keyVar;

        return KeyOfName(JSObject.PrivateNameMarker + name.Value);
    }

    public BExpression KeyOfName(string name)
    {
        // search for variable...
        if (KeyStringsBuilder.Fields.TryGetValue(name, out var fx))
            return fx;

        var i = _keyStrings.GetOrAdd(name);
        return ScriptInfoBuilder.KeyString(scriptInfo, (int)i);
    }

    public BExpression KeyOfName(in StringSpan name)
    {
        // search for variable...
        if (KeyStringsBuilder.Fields.TryGetValue(name, out var fx))
            return fx;

        var i = _keyStrings.GetOrAdd(name);
        return ScriptInfoBuilder.KeyString(scriptInfo, (int)i);
    }
}
