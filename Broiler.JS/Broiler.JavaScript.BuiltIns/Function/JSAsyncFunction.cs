using System;
using System.Threading;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Extensions;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Function;

public class JSAsyncFunction
{
    public static JSValue Create(JSValue gf)
    {
        JSValue ToAsync(in Arguments a)
        {
            var gen = gf.InvokeFunction(in a) as IJSGenerator;
            return ToPromise(gen!, JSUndefined.Value);
        }

        var fn = gf as JSFunction;
        return JSValue.CreateFunction(ToAsync, fn?.name.Value, null, gf.Length);
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
