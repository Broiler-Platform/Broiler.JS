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

    public override string ToString() => symbol.ToString();
}
