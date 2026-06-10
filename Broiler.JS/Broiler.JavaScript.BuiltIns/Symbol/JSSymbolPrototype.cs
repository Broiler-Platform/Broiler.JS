using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.ExpressionCompiler;

namespace Broiler.JavaScript.BuiltIns.Symbol;

public partial class JSSymbol
{
    [JSExport(IsConstructor = true, Length = 0)]
    public static JSValue Constructor(in Arguments a)
    {
        if ((JSEngine.Current as IJSExecutionContext)?.CurrentNewTarget != null)
            throw JSEngine.NewTypeError("Symbol is not a constructor");

        var name = a.Get1();
        if (name.IsUndefined)
            return new JSSymbol((string)null);

        return new JSSymbol(name.StringValue);
    }

    [JSPrototypeMethod]
    [JSExport("toString", Length = 0)]
    public static JSValue ToString(in Arguments a)
    {
        if (a.This is JSSymbol symbol)
            return JSValue.CreateString(symbol.ToDescriptiveString());

        if (a.This is JSSymbolObject symbolObject)
            return JSValue.CreateString(symbolObject.WrappedSymbol.ToDescriptiveString());

        throw JSEngine.NewTypeError("Symbol.prototype.toString requires a symbol receiver");
    }

    // Symbol.prototype.valueOf returns thisSymbolValue(this). Without it the
    // lookup fell through to Object.prototype.valueOf, so `Symbol.prototype
    // .valueOf()` returned the prototype object instead of throwing a TypeError
    // for a non-symbol receiver.
    [JSPrototypeMethod]
    [JSExport("valueOf", Length = 0)]
    public static JSValue ValueOf(in Arguments a)
    {
        if (a.This is JSSymbol symbol)
            return symbol;

        if (a.This is JSSymbolObject symbolObject)
            return symbolObject.WrappedSymbol;

        throw JSEngine.NewTypeError("Symbol.prototype.valueOf requires a symbol receiver");
    }
}
