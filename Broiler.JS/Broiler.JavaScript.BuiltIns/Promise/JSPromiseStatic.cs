using Broiler.JavaScript.Runtime;
using System;
using System.Threading.Tasks;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.BuiltIns.Function;
using Broiler.JavaScript.Engine.Extensions;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Promise;


public partial class JSPromise
{
    public static Task Await(JSValue value)
    {
        if (value.IsNullOrUndefined)
            return System.Threading.Tasks.Task.CompletedTask;

        if (value is JSPromise p)
            return p.Task;


        var then = value["then"];
        if (then.IsNullOrUndefined)
            return System.Threading.Tasks.Task.CompletedTask;

        return new JSPromise((resolve, reject) => then.Call(value, ToFunction(resolve), ToFunction(reject))).Task;

        static JSFunction ToFunction(Action<JSValue> action)
        {
            return new JSFunction((in Arguments a) =>
            {
                action(a[0]);
                return JSUndefined.Value;
            });
        }
    }

    [JSExport("try")]
    public static JSValue Try(in Arguments a)
    {
        var receiver = a.This;
        if (!receiver.IsObject)
            throw JSEngine.NewTypeError("Promise.try receiver must be an object");

        if (receiver is not JSFunction constructor || constructor.prototype == null)
            throw JSEngine.NewTypeError("Promise.try receiver must be a constructor");

        var callbackfn = a.Get1();
        if (!callbackfn.IsFunction)
            throw JSEngine.NewTypeError("Promise.try requires a callable argument");

        var extraArgs = new JSValue[a.Length > 1 ? a.Length - 1 : 0];
        for (int i = 1; i < a.Length; i++)
            extraArgs[i - 1] = a.GetAt(i);

        var executor = JSValue.CreateFunction((in Arguments executorArgs) =>
        {
            var resolve = executorArgs.Get1();
            var reject = executorArgs.GetAt(1);

            try
            {
                var result = callbackfn.InvokeFunction(new Arguments(JSUndefined.Value, extraArgs));
                resolve.InvokeFunction(new Arguments(JSUndefined.Value, result));
            }
            catch (JSException ex)
            {
                reject.InvokeFunction(new Arguments(JSUndefined.Value, ex.Error ?? JSException.JSErrorFrom(ex)));
            }
            catch (Exception ex)
            {
                reject.InvokeFunction(new Arguments(JSUndefined.Value, JSException.JSErrorFrom(ex)));
            }

            return JSUndefined.Value;
        }, "executor", length: 2, createPrototype: false);

        return constructor.CreateInstance(new Arguments(JSUndefined.Value, executor));
    }

    [JSExport("resolve")]
    public static JSValue Resolve(in Arguments a) => new JSPromise(a.Get1(), PromiseState.Resolved);

    [JSExport("reject")]
    public static JSValue Reject(in Arguments a)
    {
        var reason = a.Get1();
        if (reason.IsNullOrUndefined)
            throw JSEngine.NewTypeError($"Failure reason must be provided for rejected promise");

        return new JSPromise(reason, PromiseState.Rejected);
    }


    [JSExport("all")]
    public static JSValue All(in Arguments a)
    {
        var f = a.Get1();
        var result = JSValue.CreateArray();
        uint i = 0;

        return new JSPromise((resolve, reject) =>
        {
            var sc = (JSEngine.Current as JSContext)?.synchronizationContext ?? System.Threading.SynchronizationContext.Current
                ?? throw JSEngine.NewTypeError($"Cannot use promise without Synchronization Context");
            uint total = 0;

            bool empty = true;

            var length = (uint)Math.Max(f.Length, 0);
            for (uint index = 0; index < length; index++)
            {
                var item = f[index];
                if (item.IsUndefined)
                    continue;

                empty = false;
                var ni = i++;
                total = i;
                var resolveElement = new JSFunction((in Arguments args) =>
                {
                    var r1 = args.Get1();
                    sc.Post((r) =>
                    {
                        result[ni] = r as JSValue;
                        total--;

                        if (total <= 0)
                            resolve(result);
                    }, r1);
                    return JSUndefined.Value;
                }, "", "function () { [native] }", length: 1, createPrototype: false);
                var rejectElement = new JSFunction((in Arguments args) =>
                {
                    var v = args.Get1();
                    sc.Post((o) => reject(o as JSValue), v);
                    return JSUndefined.Value;
                }, "", "function () { [native] }", length: 1, createPrototype: false);

                if (item is JSPromise p)
                {
                    p.Then(resolveElement.Delegate, rejectElement.Delegate);
                    continue;
                }

                var then = item[KeyStrings.then];
                if (then.IsFunction)
                {
                    then.InvokeFunction(new Arguments(item, resolveElement, rejectElement));
                    continue;
                }

                resolveElement.InvokeFunction(new Arguments(JSUndefined.Value, item));
            }

            if (empty)
                sc.Post((o) => resolve(JSValue.CreateArray()), null);
        });
    }

    [JSExport("allSettled", Length = 1)]
    public static JSValue AllSettled(in Arguments a)
    {
        var iterable = a.Get1();
        var en = iterable.GetElementEnumerator();
        var result = JSValue.CreateArray();
        uint index = 0;

        return new JSPromise((resolve, reject) =>
        {
            var sc = (JSEngine.Current as JSContext)?.synchronizationContext ?? System.Threading.SynchronizationContext.Current
                ?? throw JSEngine.NewTypeError("Cannot use promise without Synchronization Context");

            uint remaining = 0;
            bool empty = true;

            void FinishIfDone()
            {
                if (remaining == 0)
                    sc.Post(_ => resolve(result), null);
            }

            while (en.MoveNext(out var hasValue, out var item, out var _))
            {
                if (!hasValue)
                    continue;

                empty = false;
                var currentIndex = index++;
                remaining++;
                var promise = item as JSPromise ?? new JSPromise(item, JSPromise.PromiseState.Resolved);
                promise.Then((in Arguments args) =>
                {
                    var entry = new JSObject();
                    entry[KeyStrings.GetOrCreate("status")] = JSValue.CreateString("fulfilled");
                    var value = args.Get1();
                    entry[KeyStrings.GetOrCreate("value")] = value;
                    sc.Post(_ =>
                    {
                        result[currentIndex] = entry;
                        remaining--;
                        FinishIfDone();
                    }, null);
                    return JSUndefined.Value;
                }, (in Arguments args) =>
                {
                    var entry = new JSObject();
                    entry[KeyStrings.GetOrCreate("status")] = JSValue.CreateString("rejected");
                    var reason = args.Get1();
                    entry[KeyStrings.GetOrCreate("reason")] = reason;
                    sc.Post(_ =>
                    {
                        result[currentIndex] = entry;
                        remaining--;
                        FinishIfDone();
                    }, null);
                    return JSUndefined.Value;
                });
            }

            if (empty)
                sc.Post(_ => resolve(result), null);
        });
    }

    [JSExport("allSettledKeyed", Length = 1)]
    public static JSValue AllSettledKeyed(in Arguments a)
    {
        var input = a.Get1();
        if (input is not JSObject obj)
            return new JSPromise(new JSObject(), JSPromise.PromiseState.Resolved);

        var result = new JSObject();
        var en = obj.GetOwnProperties(false).GetEnumerator();
        while (en.MoveNext(out var key, out var property))
        {
            var value = obj.GetValue(property);
            var entry = new JSObject();
            if (value is JSPromise promise && promise.state == JSPromise.PromiseState.Rejected)
            {
                entry[KeyStrings.GetOrCreate("status")] = JSValue.CreateString("rejected");
                entry[KeyStrings.GetOrCreate("reason")] = promise.result;
            }
            else
            {
                entry[KeyStrings.GetOrCreate("status")] = JSValue.CreateString("fulfilled");
                entry[KeyStrings.GetOrCreate("value")] = value is JSPromise settled ? settled.result : value;
            }

            result[key] = entry;
        }

        return new JSPromise(result, JSPromise.PromiseState.Resolved);
    }

    [JSExport("any", Length = 1)]
    public static JSValue Any(in Arguments a)
    {
        var iterable = a.Get1();
        var en = iterable.GetElementEnumerator();
        var errors = JSValue.CreateArray();
        uint errorIndex = 0;

        return new JSPromise((resolve, reject) =>
        {
            var sc = (JSEngine.Current as JSContext)?.synchronizationContext ?? System.Threading.SynchronizationContext.Current
                ?? throw JSEngine.NewTypeError("Cannot use promise without Synchronization Context");

            uint remaining = 0;
            bool empty = true;

            while (en.MoveNext(out var hasValue, out var item, out var _))
            {
                if (!hasValue)
                    continue;

                empty = false;
                remaining++;
                var promise = item as JSPromise ?? new JSPromise(item, JSPromise.PromiseState.Resolved);
                promise.Then((in Arguments args) =>
                {
                    var value = args.Get1();
                    sc.Post(_ => resolve(value), null);
                    return JSUndefined.Value;
                }, (in Arguments args) =>
                {
                    var currentIndex = errorIndex++;
                    var reason = args.Get1();
                    sc.Post(_ =>
                    {
                        errors[currentIndex] = reason;
                        remaining--;
                        if (remaining == 0)
                            reject(errors);
                    }, null);
                    return JSUndefined.Value;
                });
            }

            if (empty)
                sc.Post(_ => reject(errors), null);
        });
    }
}
