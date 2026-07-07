using Broiler.JavaScript.BuiltIns.Function;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.BuiltIns.Promise;


public partial class JSPromise
{
    [JSExport("then")]
    public JSValue Then(in Arguments a)
    {
        var (success, fail) = a.Get2();

        // PerformPromiseThen: a non-callable onFulfilled/onRejected is treated as
        // undefined (the reaction handler becomes an identity/thrower pass-through),
        // it must NOT throw.
        JSFunctionDelegate successHandler = success is JSFunction successFx ? successFx.f : null;
        JSFunctionDelegate failHandler = fail is JSFunction failFx ? failFx.f : null;

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

        var species = constructor[(IJSSymbol)Symbol.JSSymbol.species];
        if (species.IsNullOrUndefined)
            return null;

        if (!IsConstructor(species))
            throw JSEngine.NewTypeError("Promise species constructor is not a constructor");

        return IsDefaultPromiseConstructor(species) ? null : species;
    }

    // catch/finally are generic per spec: they require `this` to be an Object and
    // simply Invoke `this.then(...)`. Declaring them as [JSPrototypeMethod] statics
    // keeps them on the prototype without the generator casting `this` to JSPromise,
    // so they work on any thenable (e.g. Promise.prototype.finally.call(thenable)).
    [JSPrototypeMethod]
    [JSExport("catch", Length = 1)]
    public static JSValue Catch(in Arguments a)
    {
        if (!a.This.IsObject)
            throw JSEngine.NewTypeError("Promise.prototype.catch called on non-object");

        var then = a.This[KeyStrings.then];
        return then.InvokeFunction(new Arguments(a.This, JSUndefined.Value, a.Get1()));
    }

    [JSPrototypeMethod]
    [JSExport("finally", Length = 1)]
    public static JSValue Finally(in Arguments a)
    {
        // §27.2.5.3 Promise.prototype.finally ( onFinally )
        var promise = a.This;
        if (!promise.IsObject)
            throw JSEngine.NewTypeError("Promise.prototype.finally called on non-object");

        // step 3: C = ? SpeciesConstructor(promise, %Promise%).
        var c = FinallySpeciesConstructor(promise);

        var onFinally = a.Get1();

        var then = promise[KeyStrings.then];
        if (then is not JSFunction thenFunction)
            throw JSEngine.NewTypeError("Property then is not a function");

        JSValue thenFinally;
        JSValue catchFinally;

        // step 5: a non-callable onFinally is passed through to `then` unchanged.
        if (onFinally is not JSFunction onFinallyFx)
        {
            thenFinally = onFinally;
            catchFinally = onFinally;
        }
        else
        {
            // step 6: wrap onFinally so it runs for both fulfilment and rejection, then
            // forwards the original value/reason after awaiting onFinally's result. Both
            // wrappers are fresh functions with length 1 and the empty name (so they are
            // distinct from onFinally and are not constructors).
            thenFinally = new JSFunction((in Arguments args) =>
            {
                var value = args.Get1();
                var result = onFinallyFx.InvokeFunction(new Arguments(JSUndefined.Value));
                var p = PerformPromiseResolve(c, result);
                var valueThunk = new JSFunction((in Arguments _) => value,
                    "", "function () { [native code] }", length: 0, createPrototype: false);
                return InvokeThen(p, valueThunk);
            }, "", "function () { [native code] }", length: 1, createPrototype: false);

            catchFinally = new JSFunction((in Arguments args) =>
            {
                var reason = args.Get1();
                var result = onFinallyFx.InvokeFunction(new Arguments(JSUndefined.Value));
                var p = PerformPromiseResolve(c, result);
                var thrower = new JSFunction((in Arguments _) => throw new JSException(reason),
                    "", "function () { [native code] }", length: 0, createPrototype: false);
                return InvokeThen(p, thrower);
            }, "", "function () { [native code] }", length: 1, createPrototype: false);
        }

        // step 7: Invoke(promise, "then", « thenFinally, catchFinally »).
        return thenFunction.InvokeFunction(new Arguments(promise, thenFinally, catchFinally));
    }

    // SpeciesConstructor(promise, %Promise%) for finally — like GetThenSpeciesConstructor
    // but returns the concrete constructor (defaulting to %Promise%) rather than null,
    // because finally needs C to drive PromiseResolve for the wrapper functions.
    private static JSValue FinallySpeciesConstructor(JSValue promise)
    {
        var defaultConstructor = (JSEngine.Current as JSObject)?[KeyStrings.Promise];

        var constructor = promise[KeyStrings.constructor];
        if (constructor.IsUndefined)
            return defaultConstructor;

        if (!constructor.IsObject)
            throw JSEngine.NewTypeError("Promise constructor must be an object");

        var species = constructor[(IJSSymbol)Symbol.JSSymbol.species];
        if (species.IsNullOrUndefined)
            return defaultConstructor;

        if (!IsConstructor(species))
            throw JSEngine.NewTypeError("Promise species constructor is not a constructor");

        return species;
    }

    // PromiseResolve(C, x) — §27.2.4.7.1, performed via Invoke(C, "resolve", « x »).
    private static JSValue PerformPromiseResolve(JSValue c, JSValue x)
    {
        var resolve = GetPromiseResolve(c);
        return resolve.InvokeFunction(new Arguments(c, x));
    }

    // Invoke(p, "then", « handler »).
    private static JSValue InvokeThen(JSValue p, JSValue handler)
    {
        var then = p[KeyStrings.then];
        if (then is not JSFunction thenFunction)
            throw JSEngine.NewTypeError("Property then is not a function");

        return thenFunction.InvokeFunction(new Arguments(p, handler));
    }
}
