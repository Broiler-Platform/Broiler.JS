using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.BuiltIns.Symbol;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.BuiltIns.String;

public partial class JSString
{
    [JSExport(Length = 1, IsConstructor = true)]
    public static JSValue Constructor(in Arguments a)
    {
        if ((JSEngine.Current as IJSExecutionContext)?.CurrentNewTarget == null)
        {
            if (a.Length == 0)
                return CreateString(string.Empty);

            var value = a.Get1();
            if (value is JSSymbol symbol)
                return CreateString(symbol.ToDescriptiveString());

            return CreateString(value.StringValue);
        }

        if (a.Length == 0)
            return new JSPrimitiveObject(new JSString(StringSpan.Empty));

        return new JSPrimitiveObject(new JSString(a.Get1().StringValue));
    }
}
