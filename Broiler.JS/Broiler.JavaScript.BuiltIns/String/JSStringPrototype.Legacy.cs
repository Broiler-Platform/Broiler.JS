using System.Text;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.BuiltIns.RegExp;
using Broiler.JavaScript.BuiltIns.Symbol;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.Engine.Extensions;

namespace Broiler.JavaScript.BuiltIns.String;

public partial class JSString
{
    private static JSValue CreateHtmlWrapper(in Arguments a, string tagName, string attributeName = null)
    {
        var value = a.This.IsNullOrUndefined
            ? throw JSEngine.NewTypeError("String.prototype HTML wrapper called on null or undefined")
            : a.This.StringValue;
        var sb = new StringBuilder();
        sb.Append('<').Append(tagName);
        if (attributeName != null)
            sb.Append(' ').Append(attributeName).Append("=\"").Append(a.Get1().StringValue).Append('"');
        sb.Append('>').Append(value).Append("</").Append(tagName).Append('>');
        return JSValue.CreateString(sb.ToString());
    }

    [JSPrototypeMethod]
    [JSExport("anchor", Length = 1)]
    internal static JSValue Anchor(in Arguments a) => CreateHtmlWrapper(in a, "a", "name");

    [JSPrototypeMethod]
    [JSExport("bold", Length = 0)]
    internal static JSValue Bold(in Arguments a) => CreateHtmlWrapper(in a, "b");

    [JSPrototypeMethod]
    [JSExport("big", Length = 0)]
    internal static JSValue Big(in Arguments a) => CreateHtmlWrapper(in a, "big");

    [JSPrototypeMethod]
    [JSExport("blink", Length = 0)]
    internal static JSValue Blink(in Arguments a) => CreateHtmlWrapper(in a, "blink");

    [JSPrototypeMethod]
    [JSExport("@fixed", Length = 0)]
    internal static JSValue Fixed(in Arguments a) => CreateHtmlWrapper(in a, "tt");

    [JSPrototypeMethod]
    [JSExport("fontcolor", Length = 1)]
    internal static JSValue FontColor(in Arguments a) => CreateHtmlWrapper(in a, "font", "color");

    [JSPrototypeMethod]
    [JSExport("fontsize", Length = 1)]
    internal static JSValue FontSize(in Arguments a) => CreateHtmlWrapper(in a, "font", "size");

    [JSPrototypeMethod]
    [JSExport("italics", Length = 0)]
    internal static JSValue Italics(in Arguments a) => CreateHtmlWrapper(in a, "i");

    [JSPrototypeMethod]
    [JSExport("link", Length = 1)]
    internal static JSValue Link(in Arguments a) => CreateHtmlWrapper(in a, "a", "href");

    [JSPrototypeMethod]
    [JSExport("small", Length = 0)]
    internal static JSValue Small(in Arguments a) => CreateHtmlWrapper(in a, "small");

    [JSPrototypeMethod]
    [JSExport("strike", Length = 0)]
    internal static JSValue Strike(in Arguments a) => CreateHtmlWrapper(in a, "strike");

    [JSPrototypeMethod]
    [JSExport("sub", Length = 0)]
    internal static JSValue Sub(in Arguments a) => CreateHtmlWrapper(in a, "sub");

    [JSPrototypeMethod]
    [JSExport("sup", Length = 0)]
    internal static JSValue Sup(in Arguments a) => CreateHtmlWrapper(in a, "sup");

    [JSPrototypeMethod]
    [JSExport("matchAll", Length = 1)]
    internal static JSValue MatchAll(in Arguments a)
    {
        var pattern = a.Get1();
        if (a.This.IsNullOrUndefined)
            throw JSEngine.NewTypeError("String.prototype.matchAll called on null or undefined");

        if (!pattern.IsNullOrUndefined)
        {
            var matcher = pattern[(IJSSymbol)JSSymbol.matchAll];
            if (!matcher.IsUndefined && !matcher.IsNull)
            {
                if (!matcher.IsFunction)
                    throw JSEngine.NewTypeError("String.prototype.matchAll requires @@matchAll to be callable");

                return matcher.Call(pattern, JSValue.CreateString(a.This.StringValue));
            }
        }

        var text = JSValue.CreateString(a.This.StringValue);
        var rx = new JSRegExp(pattern.IsUndefined ? string.Empty : pattern.StringValue, "g");
        var matcherFunction = rx[(IJSSymbol)JSSymbol.matchAll];
        return matcherFunction.InvokeFunction(new Arguments(rx, text));
    }
}
