using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Symbol;

public partial class JSSymbol
{
    [JSExport(IsConstructor = true)]
    public static JSValue Constructor(in Arguments a)
    {
        var name = a.Get1();
        if (name.IsUndefined)
            return new JSSymbol("");

        return new JSSymbol(name.ToString());
    }

    [JSExport("toString", Length = 0)]
    public static JSValue ToString(in Arguments a)
    {
        if (a.This is JSSymbol symbol)
            return JSValue.CreateString(symbol.ToString());

        throw JSEngine.NewTypeError("Symbol.prototype.toString requires a symbol receiver");
    }
}
