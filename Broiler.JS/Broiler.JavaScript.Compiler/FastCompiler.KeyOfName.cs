using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Compiler;

partial class FastCompiler
{
    // A private name (a `#x` IdentifierName — never a string-literal or computed
    // key) gets a property key in a marker-prefixed namespace so it cannot collide
    // with a same-spelled public `"#x"` string property. The declaration site
    // (GetName) and every reference (VisitMemberExpression) both route through here,
    // so they agree on the key; the runtime hides marker-prefixed keys from
    // reflection and enumeration (JSObject.IsPrivateName).
    public YExpression KeyOfPrivateName(in StringSpan name)
        => KeyOfName(JSObject.PrivateNameMarker + name.Value);

    public YExpression KeyOfName(string name)
    {
        // search for variable...
        if (KeyStringsBuilder.Fields.TryGetValue(name, out var fx))
            return fx;

        var i = _keyStrings.GetOrAdd(name);
        return ScriptInfoBuilder.KeyString(scriptInfo, (int)i);
    }

    public YExpression KeyOfName(in StringSpan name)
    {
        // search for variable...
        if (KeyStringsBuilder.Fields.TryGetValue(name, out var fx))
            return fx;

        var i = _keyStrings.GetOrAdd(name);
        return ScriptInfoBuilder.KeyString(scriptInfo, (int)i);
    }
}
