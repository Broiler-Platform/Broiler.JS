using System;
using System.Threading;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.BuiltIns.Symbol;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Extensions;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.Storage;

namespace Broiler.JavaScript.BuiltIns.Function;

public class JSAsyncFunction
{
    // Returns the per-realm %AsyncFunction.prototype% intrinsic, creating and
    // caching it on first use. Every async function shares this single object as
    // its [[Prototype]] (so `Object.getPrototypeOf(async () => {})` equals
    // `AsyncFunction.prototype`), instead of receiving a fresh prototype each time.
    private static JSObject GetOrCreateAsyncFunctionPrototype()
    {
        if (JSEngine.Current is IJSExecutionContext context)
        {
            if (context.AsyncFunctionPrototype is JSObject cached)
                return cached;

            var created = CreateAsyncFunctionPrototype();
            context.AsyncFunctionPrototype = created;
            return created;
        }

        return CreateAsyncFunctionPrototype();
    }

    private static JSObject CreateAsyncFunctionPrototype()
    {
        var prototype = new JSObject();
        var functionPrototype = (JSEngine.Current as IJSExecutionContext)?.FunctionPrototype;
        if (functionPrototype != null)
            prototype.BasePrototypeObject = functionPrototype;

        var constructor = (JSFunction)JSValue.CreateFunction((in Arguments a) =>
        {
            var created = JSFunction.CreateDynamicFunction(in a, "async function");
            if (created is JSFunction function)
                function.prototype = null;

            return created;
        }, "AsyncFunction", "function AsyncFunction() { [native code] }", 1, createPrototype: false);
        constructor.FastAddValue(KeyStrings.prototype, prototype, JSPropertyAttributes.ReadonlyValue);
        constructor.prototype = prototype;
        // §27.7.3: %AsyncFunction%.[[Prototype]] is the intrinsic %Function% (the
        // Function constructor), so AsyncFunction is a subclass of Function
        // (test262: built-ins/AsyncFunction/AsyncFunction-is-subclass).
        if (functionPrototype?[KeyStrings.constructor] is JSObject functionConstructor)
            constructor.BasePrototypeObject = functionConstructor;
        // §27.7.3.2: AsyncFunction.prototype.constructor is non-writable
        // (attributes { writable: false, enumerable: false, configurable: true }).
        prototype.FastAddValue(KeyStrings.constructor, constructor, JSPropertyAttributes.ConfigurableReadonlyValue);

        // §27.7.3.3: AsyncFunction.prototype[@@toStringTag] = "AsyncFunction".
        // Async functions inherit this, so Object.prototype.toString on an async
        // function (or a Proxy of one) reports "[object AsyncFunction]".
        prototype.FastAddValue((IJSSymbol)JSSymbol.toStringTag, JSValue.CreateString("AsyncFunction"), JSPropertyAttributes.ConfigurableReadonlyValue);

        return prototype;
    }

    public static JSValue Create(JSValue gf)
    {
        JSValue ToAsync(in Arguments a)
        {
            var gen = gf.InvokeFunction(in a) as IJSGenerator;
            return ToPromise(gen!, JSUndefined.Value);
        }

        var fn = gf as JSFunction;
        var asyncFunction = JSValue.CreateFunction(ToAsync, fn?.name.Value, null, gf.Length, createPrototype: false);
        if (asyncFunction is JSObject asyncObject)
            asyncObject.BasePrototypeObject = GetOrCreateAsyncFunctionPrototype();

        // The visible function is this async wrapper, not the underlying generator, so
        // adopt the generator's source text — otherwise Function.prototype.toString
        // reports the "[native code]" placeholder instead of the async function's body.
        if (fn != null && asyncFunction is JSFunction asyncFn)
        {
            var span = fn.SourceSpan;
            if (!span.IsEmpty)
                asyncFn.OverrideSource(span);

            // An anonymous async function must report name "" (not the "native"
            // placeholder the name-less native-function path assigns) while staying
            // eligible for NamedEvaluation, exactly like an anonymous ordinary function
            // expression — so `[async function(){}][0].name` is "" but
            // `var f = async function(){}` infers "f" (test262
            // sm/Function/function-name-assignment). A named async function keeps its name.
            if (fn.name.IsEmpty || fn.IsAnonymousNamePending)
            {
                asyncFn.SetNameProperty(string.Empty);
                asyncFn.IsAnonymousNamePending = true;
            }
        }

        return asyncFunction;
    }

    private static JSValue ToPromise(IJSGenerator gen, JSValue lastResult)
    {
        try
        {
            if (!gen.MoveNext(lastResult, out var r))
            {
                // §27.7.5.2 (AsyncFunctionStart / AsyncBlockStart): normal completion
                // resolves the async function's result promise WITH the return value,
                // which ADOPTS a thenable return value (Promise Resolve Functions read
                // `then` and, if callable, follow it). CreateResolvedOrRejectedPromise
                // fulfils with the value directly (no adoption), so
                //   async function w(){ return new Promise(r => requestAnimationFrame(r)); }
                // would fulfil w()'s promise with the inner promise as a plain value, and
                // `await w()` would resume immediately instead of awaiting it to settle.
                // Route object return values through the adopting resolve (single `then`
                // read, throwing-`then`-getter handled per spec); primitives are never
                // thenables, so keep the fast path for them.
                if (r.IsObject)
                    return (JSValue)JSEngine.CreatePromiseFromDelegate((resolve, reject) => resolve(r));

                return JSEngine.CreateResolvedOrRejectedPromise(r, true);
            }

            // Is the awaited value a thenable (an object with a callable `then`)? A
            // non-thenable — a primitive, or an object without a `then` method — is
            // NOT the final result of the async function: `await` of it still
            // suspends for one microtask tick and then resumes the function with the
            // value itself, so the continuation after the await (e.g. `(await 1) + 1`)
            // runs. Previously a non-thenable resolved the whole async function with
            // the value, discarding everything after the await.
            var then = r.IsObject ? r[KeyStrings.then] : JSUndefined.Value;
            var isThenable = then.IsFunction;

            // Resume on the synchronization context currently being pumped (matching how
            // JSPromise captures its context), falling back to the context captured at
            // JSContext construction. Posting to the construction-time context directly would
            // bypass a caller-installed pump (Execute/ExecuteAsync via AsyncPump) and strand
            // continuations on a context nobody is draining, deadlocking the awaiting task.
            var continuationContext = SynchronizationContext.Current
                ?? (JSEngine.Current as JSContext)?.synchronizationContext;

            return (JSValue)JSEngine.CreatePromiseFromDelegate((resolve, reject) =>
            {
                void Queue(Action action)
                {
                    if (continuationContext != null)
                        continuationContext.Post(_ => action(), null);
                    else
                        ThreadPool.QueueUserWorkItem(_ => action());
                }

                if (!isThenable)
                {
                    Queue(() =>
                    {
                        try
                        {
                            resolve(ToPromise(gen, r));
                        }
                        catch (Exception ex)
                        {
                            reject(JSException.JSErrorFrom(ex));
                        }
                    });
                    return;
                }

                r.InvokeMethod(in KeyStrings.then,
                    JSValue.CreateFunction((in Arguments a) =>
                    {
                        var resumeValue = a.Get1();
                        Queue(() =>
                        {
                            try
                            {
                                resolve(ToPromise(gen, resumeValue));
                            }
                            catch (Exception ex)
                            {
                                reject(JSException.JSErrorFrom(ex));
                            }
                        });
                        return JSUndefined.Value;
                    }),
                    JSValue.CreateFunction((in Arguments a) =>
                    {
                        var thrownValue = a.Get1();
                        Queue(() =>
                        {
                            try
                            {
                                var thrownResult = gen.Throw(thrownValue);
                                resolve(ToPromise(gen, thrownResult));
                            }
                            catch (Exception ex)
                            {
                                reject(JSException.JSErrorFrom(ex));
                            }
                        });
                        return JSUndefined.Value;
                    }));
            });
        }
        catch (Exception ex)
        {
            return JSEngine.CreateResolvedOrRejectedPromise(JSException.JSErrorFrom(ex), false);
        }
    }
}
