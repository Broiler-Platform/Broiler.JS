using System.Text;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.BuiltIns.RegExp;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.String;

public partial class JSString
{
    private static JSValue CreateHtmlWrapper(in Arguments a, string tagName, string attributeName = null)
    {
        var value = a.This.IsNullOrUndefined
            ? throw JSEngine.NewTypeError("String.prototype HTML wrapper called on null or undefined")
            : a.This.ToString();
        var sb = new StringBuilder();
        sb.Append('<').Append(tagName);
        if (attributeName != null)
            sb.Append(' ').Append(attributeName).Append("=\"").Append(a.Get1().ToString()).Append('"');
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
    [JSExport("@fixed", Length = 0)]
    internal static JSValue Fixed(in Arguments a) => CreateHtmlWrapper(in a, "tt");

    [JSPrototypeMethod]
    [JSExport("fontcolor", Length = 1)]
    internal static JSValue FontColor(in Arguments a) => CreateHtmlWrapper(in a, "font", "color");

    [JSPrototypeMethod]
    [JSExport("small", Length = 0)]
    internal static JSValue Small(in Arguments a) => CreateHtmlWrapper(in a, "small");

    [JSPrototypeMethod]
    [JSExport("strike", Length = 0)]
    internal static JSValue Strike(in Arguments a) => CreateHtmlWrapper(in a, "strike");

    [JSPrototypeMethod]
    [JSExport("sup", Length = 0)]
    internal static JSValue Sup(in Arguments a) => CreateHtmlWrapper(in a, "sup");

    [JSPrototypeMethod]
    [JSExport("matchAll", Length = 1)]
    internal static JSValue MatchAll(in Arguments a)
    {
        var pattern = a.Get1();
        var text = a.This.IsNullOrUndefined
            ? throw JSEngine.NewTypeError("String.prototype.matchAll called on null or undefined")
            : a.This.ToString();

        if (pattern is JSRegExp regExp)
            return regExp.Match(JSValue.CreateString(text));

        return new JSRegExp(pattern.IsUndefined ? "" : pattern.ToString(), "g").Match(JSValue.CreateString(text));
    }
}
