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

        // SpeciesConstructor(this, %Promise%): the default %Promise% keeps the fast
        // native path. A custom species builds the result promise via
        // NewPromiseCapability — so a throwing or non-constructor species surfaces
        // here (Promise.prototype.then ctor-throws).
        var species = GetThenSpeciesConstructor();
        if (species == null)
            return Then(successHandler, failHandler);

        JSValue capabilityResolve = JSUndefined.Value;
        JSValue capabilityReject = JSUndefined.Value;
        var resultPromise = CreatePromiseFromConstructor(species, (resolve, reject) =>
        {
            capabilityResolve = resolve;
            capabilityReject = reject;
        });

        // Drive the reactions through a native promise, then forward its
        // settlement to the species-built capability.
        var native = (JSPromise)Then(successHandler, failHandler);
        native.Then(
            (in Arguments r) =>
            {
                capabilityResolve.InvokeFunction(new Arguments(JSUndefined.Value, r.Get1()));
                return JSUndefined.Value;
            },
            (in Arguments r) =>
            {
                capabilityReject.InvokeFunction(new Arguments(JSUndefined.Value, r.Get1()));
                return JSUndefined.Value;
            });

        return resultPromise;
    }

    // SpeciesConstructor(this, %Promise%) (§27.2.5.4.1 / §7.3.22). Returns the
    // species constructor, or null when it is the default %Promise% (the caller
    // then takes the fast native path). Throws if `constructor` is a non-object or
    // its @@species is a non-constructor.
    private JSValue GetThenSpeciesConstructor()
    {
        var constructor = this[KeyStrings.constructor];
        if (constructor.IsUndefined)
            return null;

        if (!constructor.IsObject)
            throw JSEngine.NewTypeError("Promise constructor must be an object");

        var species = constructor[(IJSSymbol)BuiltIns.Symbol.JSSymbol.species];
        if (species.IsNullOrUndefined)
            return null;

        if (!IsConstructor(species))
            throw JSEngine.NewTypeError("Promise species constructor is not a constructor");

        return IsDefaultPromiseConstructor(species) ? null : species;
    }

    [JSExport("catch")]
    public JSValue Catch(in Arguments a)
    {
        var then = a.This[KeyStrings.then];
        return then.InvokeFunction(new Arguments(a.This, JSUndefined.Value, a.Get1()));
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
