using System;
using System.Threading;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Extensions;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.Storage;
using Broiler.JavaScript.BuiltIns.Promise;

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
        constructor.FastAddValue(KeyStrings.prototype, prototype, JSPropertyAttributes.ReadonlyValue);
        constructor.prototype = prototype;
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
                return r ?? JSUndefined.Value;

            var then = r[KeyStrings.then];
            if (then.IsUndefined)
                return r ?? JSUndefined.Value;

            var continuationContext = (JSEngine.Current as JSContext)?.synchronizationContext;
            var executionContext = JSEngine.Current as IJSExecutionContext;
            var continuationTop = executionContext?.Top;

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
                            var previousTop = executionContext?.Top;
                            try
                            {
                                if (executionContext != null)
                                    executionContext.Top = continuationTop;
                                var awaited = ToPromise(gen, resumeValue);
                                if (awaited is JSPromise awaitedPromise)
                                {
                                    awaitedPromise.Then(
                                        (in Arguments a1) =>
                                        {
                                            resolve(a1.Get1());
                                            return JSUndefined.Value;
                                        },
                                        (in Arguments a1) =>
                                        {
                                            reject(a1.Get1());
                                            return JSUndefined.Value;
                                        });
                                }
                                else
                                {
                                    resolve(awaited);
                                }
                            }
                            catch (Exception ex)
                            {
                                reject(JSException.JSErrorFrom(ex));
                            }
                            finally
                            {
                                if (executionContext != null)
                                    executionContext.Top = previousTop;
                            }
                        });
                        return JSUndefined.Value;
                    }),
                    JSValue.CreateFunction((in Arguments a) =>
                    {
                        var thrownValue = a.Get1();
                        Queue(() =>
                        {
                            var previousTop = executionContext?.Top;
                            try
                            {
                                if (executionContext != null)
                                    executionContext.Top = continuationTop;
                                var thrownResult = gen.Throw(thrownValue);
                                var awaited = ToPromise(gen, thrownResult);
                                if (awaited is JSPromise awaitedPromise)
                                {
                                    awaitedPromise.Then(
                                        (in Arguments a1) =>
                                        {
                                            resolve(a1.Get1());
                                            return JSUndefined.Value;
                                        },
                                        (in Arguments a1) =>
                                        {
                                            reject(a1.Get1());
                                            return JSUndefined.Value;
                                        });
                                }
                                else
                                {
                                    resolve(awaited);
                                }
                            }
                            catch (Exception ex)
                            {
                                reject(JSException.JSErrorFrom(ex));
                            }
                            finally
                            {
                                if (executionContext != null)
                                    executionContext.Top = previousTop;
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
