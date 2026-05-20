using System;
using System.Threading;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Extensions;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.Storage;

namespace Broiler.JavaScript.BuiltIns.Function;

public class JSAsyncFunction
{
    private static JSObject CreateAsyncFunctionPrototype()
    {
        var prototype = new JSObject();
        if ((JSEngine.Current as IJSExecutionContext)?.FunctionPrototype is JSObject functionPrototype)
            prototype.BasePrototypeObject = functionPrototype;

        var constructor = (JSFunction)JSValue.CreateFunction((in Arguments a) =>
        {
            var created = JSFunction.CreateDynamicFunction(in a, "async function");
            if (created is JSFunction function)
                function.prototype = null;

            return created;
        }, "AsyncFunction", "function AsyncFunction() { [native code] }", 1, createPrototype: false);
        constructor.FastAddValue(KeyStrings.prototype, prototype, JSPropertyAttributes.ConfigurableValue);
        prototype.FastAddValue(KeyStrings.constructor, constructor, JSPropertyAttributes.ConfigurableValue);

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
            asyncObject.BasePrototypeObject = CreateAsyncFunctionPrototype();

        return asyncFunction;
    }

    private static JSValue ToPromise(IJSGenerator gen, JSValue lastResult)
    {
        try
        {
            if (!gen.MoveNext(lastResult, out var r))
                return JSEngine.CreateResolvedOrRejectedPromise(r, true);

            var then = r[KeyStrings.then];
            if (then.IsUndefined)
                return JSEngine.CreateResolvedOrRejectedPromise(r, true);

            var continuationContext = (JSEngine.Current as JSContext)?.synchronizationContext;

            return (JSValue)JSEngine.CreatePromiseFromDelegate((resolve, reject) =>
            {
                void Queue(Action action)
                {
                    if (continuationContext != null)
                        continuationContext.Post(_ => action(), null);
                    else
                        ThreadPool.QueueUserWorkItem(_ => action());
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
