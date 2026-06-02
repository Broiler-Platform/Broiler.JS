using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.BuiltIns.ShadowRealm;

[JSClassGenerator("ShadowRealm")]
public partial class JSShadowRealm : JSObject
{
    [JSExport(Length = 0)]
    public JSShadowRealm(in Arguments a) : base(JSEngine.NewTargetPrototype)
    {
    }

    [JSExport(Length = 1)]
    public JSValue Evaluate(in Arguments a)
    {
        var sourceText = a.Get1().ToString();
        var result = (JSEngine.Current as JSContext)?.Eval(sourceText) ?? JSUndefined.Value;
        if (result.IsObject)
            throw JSEngine.NewTypeError("ShadowRealm.prototype.evaluate cannot return objects");

        return result;
    }
}
