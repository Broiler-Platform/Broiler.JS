using Broiler.JavaScript.BuiltIns.Function;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Storage;

namespace Broiler.JavaScript.BuiltIns.Promise;


public partial class JSPromise
{
    [JSExport("then")]
    public JSValue Then(in Arguments a)
    {
        ValidatePromiseSpeciesConstructor(a.This);

        var (success, fail) = a.Get2();

        JSFunctionDelegate successHandler = null;
        if (!success.IsUndefined)
        {
            if (success is not JSFunction successFx)
                throw JSEngine.NewTypeError($"Parameter for then is not a function");

            successHandler = successFx.f;
        }

        JSFunctionDelegate failHandler = null;
        if (!fail.IsUndefined)
        {
            if (fail is not JSFunction failFx)
                throw JSEngine.NewTypeError($"Parameter for then is not a function");

            failHandler = failFx.f;
        }

        return Then(successHandler, failHandler);
    }

    [JSExport("catch")]
    public JSValue Catch(JSFunction fx)
    {
        Then(null, fx.f);
        return this;
    }

    [JSExport("finally")]
    public JSValue Finally(in Arguments a)
    {
        var onFinally = a.Get1();
        var then = a.This[KeyStrings.then];
        if (then is not JSFunction thenFunction)
            throw JSEngine.NewTypeError("Property then is not a function");

        return thenFunction.InvokeFunction(new Arguments(a.This, onFinally, onFinally));
    }
}
