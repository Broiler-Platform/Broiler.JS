using Broiler.JavaScript.BuiltIns.Function;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Storage;

namespace Broiler.JavaScript.BuiltIns.Symbol;

internal sealed class JSSymbolObject(JSSymbol symbol) : JSObject(GetPrototype())
{
    private readonly JSSymbol symbol = symbol;

    private static JSObject GetPrototype() => ((JSEngine.Current as JSObject)?[KeyStrings.Symbol] as JSFunction)?.prototype;

    // The wrapped [[SymbolData]]. Exposed directly (rather than via the CLR
    // ValueOf() override) so that abstract operations on the wrapper — relational
    // comparison, addition, ToNumber — go through ToPrimitive and observe a
    // user-overridden Symbol.prototype.valueOf / @@toPrimitive instead of being
    // short-circuited to the raw symbol (which can never be coerced).
    internal JSSymbol WrappedSymbol => symbol;

    // Stringify through the ordinary object ToString (GetMethod("toString") →
    // Symbol.prototype.toString → "Symbol(desc)", or a user-overridden toString),
    // NOT the bare wrapped description. CLR consumers that coerce a value to a string
    // (template literals, String.prototype.concat) must observe the same result the
    // JS ToString abstract operation produces for a Symbol wrapper.
    public override string ToString() => base.ToString();
}
