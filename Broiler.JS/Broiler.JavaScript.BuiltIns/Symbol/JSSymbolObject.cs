using Broiler.JavaScript.BuiltIns.Function;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Storage;

namespace Broiler.JavaScript.BuiltIns.Symbol;

internal sealed class JSSymbolObject(JSSymbol symbol) : JSObject(GetPrototype())
{
    private readonly JSSymbol symbol = symbol;

    private static JSObject GetPrototype() => ((JSEngine.Current as JSObject)?[KeyStrings.Symbol] as JSFunction)?.prototype;

    public override JSValue ValueOf() => symbol;

    public override string ToString() => symbol.ToString();
}
