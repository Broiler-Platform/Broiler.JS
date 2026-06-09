using Broiler.JavaScript.Runtime;
using System;
using System.Threading.Tasks;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.BuiltIns.Function;
using Broiler.JavaScript.Engine.Extensions;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.Storage;

namespace Broiler.JavaScript.BuiltIns.Promise;


public partial class JSPromise
{
    private static bool IsDefaultPromiseConstructor(JSValue constructor)
        => ReferenceEquals(constructor, (JSEngine.Current as JSObject)?[KeyStrings.Promise]);

    private static bool IsConstructor(JSValue value)
        => JSConstructorOperations.IsConstructor(value);

    // GetPromiseResolve(C) — §27.2.4.1.1. Reads the constructor's "resolve"
    // method and verifies it is callable. Performed up front by every Promise
    // combinator (before iterating), so that an abrupt completion from the
    // "resolve" getter rejects the returned promise without ever creating —
    // and therefore without closing — the iterator.
    private static JSValue GetPromiseResolve(JSValue constructor)
    {
        var promiseResolve = constructor[KeyStrings.GetOrCreate("resolve")];
        if (!promiseResolve.IsFunction)
            throw JSEngine.NewTypeError("Promise resolve is not a function");

        return promiseResolve;
    }

    private static JSValue CreatePromiseFromConstructor(JSValue constructor, Action<JSValue, JSValue> executor)
    {
        if (!IsConstructor(constructor))
            throw JSEngine.NewTypeError("Promise receiver must be a constructor");

        JSValue resolve = JSUndefined.Value;
        JSValue reject = JSUndefined.Value;
        var executorFunction = new JSFunction((in Arguments executorArgs) =>
        {
            var nextResolve = executorArgs.Get1();
            var nextReject = executorArgs.GetAt(1);

            if (!nextResolve.IsUndefined && !resolve.IsUndefined)
                throw JSEngine.NewTypeError("Promise capability executor already called");

            if (!nextReject.IsUndefined && !reject.IsUndefined)
                throw JSEngine.NewTypeError("Promise capability executor already called");

            if (!nextResolve.IsUndefined)
                resolve = nextResolve;

            if (!nextReject.IsUndefined)
                reject = nextReject;

            return JSUndefined.Value;
        }, "executor", "function executor() { [native code] }", length: 2, createPrototype: false);
        executorFunction.SetNameProperty(string.Empty);

        var promise = constructor.CreateInstance(new Arguments(JSUndefined.Value, executorFunction));
        if (!resolve.IsFunction || !reject.IsFunction)
            throw JSEngine.NewTypeError("Promise capability executor did not provide callable resolve and reject functions");

        executor(resolve, reject);
        return promise;
    }

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

        if (!IsConstructor(receiver))
            throw JSEngine.NewTypeError("Promise.try receiver must be a constructor");

        var constructor = (JSFunction)receiver;

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
        ((JSFunction)executor).SetNameProperty(string.Empty);

        return constructor.CreateInstance(new Arguments(JSUndefined.Value, executor));
    }

    // Promise.withResolvers ( ) — §27.2.4.10. Creates a new promise via the
    // receiver constructor's NewPromiseCapability and returns a plain object
    // exposing the promise together with its resolve/reject functions.
    [JSExport("withResolvers")]
    public static JSValue WithResolvers(in Arguments a)
    {
        var constructor = a.This;

        JSValue capturedResolve = JSUndefined.Value;
        JSValue capturedReject = JSUndefined.Value;
        var promise = CreatePromiseFromConstructor(constructor, (resolve, reject) =>
        {
            capturedResolve = resolve;
            capturedReject = reject;
        });

        var result = new JSObject();
        result[KeyStrings.GetOrCreate("promise")] = promise;
        result[KeyStrings.GetOrCreate("resolve")] = capturedResolve;
        result[KeyStrings.GetOrCreate("reject")] = capturedReject;
        return result;
    }

    [JSExport("resolve")]
    public static JSValue Resolve(in Arguments a)
    {
        var value = a.Get1();
        var constructor = a.This;
        if (!constructor.IsObject)
            throw JSEngine.NewTypeError("Promise.resolve must be called with an object receiver");

        // PromiseResolve(C, x) — §27.2.4.7.1: if x is already a promise whose
        // own "constructor" is C, return it unchanged so an overridden `then`
        // (or other observable behaviour) on the existing promise is preserved.
        if (value is JSPromise && ReferenceEquals(value[KeyStrings.constructor], constructor))
            return value;

        if (IsDefaultPromiseConstructor(constructor))
            return new JSPromise(value, PromiseState.Resolved);

        return CreatePromiseFromConstructor(constructor, (resolve, _) =>
        {
            resolve.InvokeFunction(new Arguments(JSUndefined.Value, value));
        });
    }

    [JSExport("reject")]
    public static JSValue Reject(in Arguments a)
    {
        var reason = a.Get1();
        if (IsDefaultPromiseConstructor(a.This))
            return new JSPromise(reason, PromiseState.Rejected);

        return CreatePromiseFromConstructor(a.This, (_, reject) =>
        {
            reject.InvokeFunction(new Arguments(JSUndefined.Value, reason));
        });
    }


    [JSExport("all")]
    public static JSValue All(in Arguments a)
    {
        var iterable = a.Get1();
        var constructor = a.This;
        var result = JSValue.CreateArray();
        uint index = 0;

        return CreatePromiseFromConstructor(constructor, (resolve, reject) =>
        {
            // remainingElementsCount (§27.2.4.1.2) starts at 1 and is decremented
            // once per settled element plus once after iteration completes. The
            // capability resolves only when it reaches 0, so synchronous thenables
            // that settle during the loop cannot resolve the result prematurely.
            // The resolve element runs synchronously (a native promise already
            // dispatches its reaction on a microtask via Then).
            int remaining = 1;

            void Settle()
            {
                if (--remaining == 0)
                    resolve.InvokeFunction(new Arguments(JSUndefined.Value, result));
            }

            // Per §27.2.4.1, an abrupt completion while obtaining the constructor's
            // "resolve" method, or while obtaining or stepping the iterable, must
            // reject the returned promise rather than throwing synchronously.
            try
            {
                GetPromiseResolve(constructor);
                var en = iterable.GetIterableEnumerator();
                while (en.MoveNext(out var hasValue, out var item, out var _))
                {
                    if (!hasValue)
                        continue;

                    var currentIndex = index++;
                    result[currentIndex] = JSUndefined.Value;
                    remaining++;

                    var alreadyCalled = false;
                    var resolveElement = new JSFunction((in Arguments args) =>
                    {
                        if (alreadyCalled)
                            return JSUndefined.Value;
                        alreadyCalled = true;
                        result[currentIndex] = args.Get1();
                        Settle();
                        return JSUndefined.Value;
                    }, "", "function () { [native code] }", length: 1, createPrototype: false);
                    var rejectElement = new JSFunction((in Arguments args) =>
                    {
                        reject.InvokeFunction(new Arguments(JSUndefined.Value, args.Get1()));
                        return JSUndefined.Value;
                    }, "", "function () { [native code] }", length: 1, createPrototype: false);

                    if (item is JSPromise promise)
                    {
                        promise.Then(resolveElement.Delegate, rejectElement.Delegate);
                        continue;
                    }

                    // null/undefined (and other primitives) are not thenable; reading
                    // ".then" off null/undefined would throw, so only probe objects.
                    if (!item.IsNullOrUndefined)
                    {
                        var then = item[KeyStrings.then];
                        if (then.IsFunction)
                        {
                            then.InvokeFunction(new Arguments(item, resolveElement, rejectElement));
                            continue;
                        }
                    }

                    resolveElement.InvokeFunction(new Arguments(JSUndefined.Value, item));
                }

                Settle();
            }
            catch (JSException ex)
            {
                reject.InvokeFunction(new Arguments(JSUndefined.Value, ex.Error ?? JSException.JSErrorFrom(ex)));
            }
        });
    }

    [JSExport("allKeyed", Length = 1)]
    public static JSValue AllKeyed(in Arguments a)
    {
        var input = a.Get1();
        if (input is not JSObject obj)
            return CreatePromiseFromConstructor(a.This, (resolve, _) =>
            {
                resolve.InvokeFunction(new Arguments(JSUndefined.Value, new JSObject()));
            });

        var result = new JSObject();
        var keys = new System.Collections.Generic.List<KeyString>();
        var en = obj.GetOwnProperties(false).GetEnumerator();
        while (en.MoveNext(out var key, out var _))
            keys.Add(key);

        if (keys.Count == 0)
            return CreatePromiseFromConstructor(a.This, (resolve, _) =>
            {
                resolve.InvokeFunction(new Arguments(JSUndefined.Value, new JSObject()));
            });

        return CreatePromiseFromConstructor(a.This, (resolve, reject) =>
        {
            var sc = (JSEngine.Current as JSContext)?.synchronizationContext ?? System.Threading.SynchronizationContext.Current
                ?? throw JSEngine.NewTypeError("Cannot use promise without Synchronization Context");
            int remaining = keys.Count;

            foreach (var key in keys)
            {
                var value = obj[key];
                var capturedKey = key;

                var resolveElement = new JSFunction((in Arguments args) =>
                {
                    var r = args.Get1();
                    sc.Post((_) =>
                    {
                        result[capturedKey] = r;
                        remaining--;
                        if (remaining <= 0)
                            resolve.InvokeFunction(new Arguments(JSUndefined.Value, result));
                    }, null);
                    return JSUndefined.Value;
                }, "", "function () { [native code] }", length: 1, createPrototype: false);

                var rejectElement = new JSFunction((in Arguments args) =>
                {
                    var v = args.Get1();
                    sc.Post((o) => reject.InvokeFunction(new Arguments(JSUndefined.Value, o as JSValue)), v);
                    return JSUndefined.Value;
                }, "", "function () { [native code] }", length: 1, createPrototype: false);

                if (value is JSPromise p)
                {
                    p.Then(resolveElement.Delegate, rejectElement.Delegate);
                    continue;
                }

                var then = value[KeyStrings.then];
                if (then.IsFunction)
                {
                    then.InvokeFunction(new Arguments(value, resolveElement, rejectElement));
                    continue;
                }

                resolveElement.InvokeFunction(new Arguments(JSUndefined.Value, value));
            }
        });
    }

    [JSExport("race", Length = 1)]
    public static JSValue Race(in Arguments a)
    {
        var iterable = a.Get1();
        var constructor = a.This;
        return CreatePromiseFromConstructor(constructor, (resolve, reject) =>
        {
            var sc = (JSEngine.Current as JSContext)?.synchronizationContext ?? System.Threading.SynchronizationContext.Current
                ?? throw JSEngine.NewTypeError("Cannot use promise without Synchronization Context");

            // Per §27.2.4.5, an abrupt completion while obtaining the constructor's
            // "resolve" method, or while obtaining or stepping the iterable (e.g. a
            // throwing @@iterator, next, or result-property access), must reject the
            // returned promise rather than throwing synchronously.
            try
            {
                // PerformPromiseRace (§27.2.4.5.1) routes every value through
                // Call(promiseResolve, constructor, « nextValue »); the resulting
                // promise's `then` schedules the race reaction. This is observable
                // for Promise subclasses whose `resolve` is overridden, so it must
                // be called per element rather than special-casing native promises.
                var promiseResolve = GetPromiseResolve(constructor);
                var en = iterable.GetIterableEnumerator();
                while (en.MoveNext(out var hasValue, out var item, out var _))
                {
                    if (!hasValue)
                        continue;

                    var resolveElement = new JSFunction((in Arguments args) =>
                    {
                        var value = args.Get1();
                        sc.Post(o => resolve.InvokeFunction(new Arguments(JSUndefined.Value, o as JSValue)), value);
                        return JSUndefined.Value;
                    }, "", "function () { [native code] }", length: 1, createPrototype: false);
                    var rejectElement = new JSFunction((in Arguments args) =>
                    {
                        var value = args.Get1();
                        sc.Post(o => reject.InvokeFunction(new Arguments(JSUndefined.Value, o as JSValue)), value);
                        return JSUndefined.Value;
                    }, "", "function () { [native code] }", length: 1, createPrototype: false);

                    var nextPromise = promiseResolve.InvokeFunction(new Arguments(constructor, item));
                    if (nextPromise is JSPromise promise)
                    {
                        promise.Then(resolveElement.Delegate, rejectElement.Delegate);
                        continue;
                    }

                    var then = nextPromise[KeyStrings.then];
                    if (!then.IsFunction)
                        throw JSEngine.NewTypeError("Promise resolve did not return a thenable");

                    then.InvokeFunction(new Arguments(nextPromise, resolveElement, rejectElement));
                }
            }
            catch (JSException ex)
            {
                reject.InvokeFunction(new Arguments(JSUndefined.Value, ex.Error ?? JSException.JSErrorFrom(ex)));
            }
        });
    }

    [JSExport("allSettled", Length = 1)]
    public static JSValue AllSettled(in Arguments a)
    {
        var iterable = a.Get1();
        var constructor = a.This;
        var result = JSValue.CreateArray();
        uint index = 0;

        return CreatePromiseFromConstructor(constructor, (resolve, reject) =>
        {
            // remainingElementsCount (§27.2.4.7) starts at 1 and is decremented per
            // settled element plus once after iteration; the capability resolves
            // only at 0. Each element settles synchronously (a native promise
            // already dispatches its reaction on a microtask via Then).
            int remaining = 1;

            void Settle()
            {
                if (--remaining == 0)
                    resolve.InvokeFunction(new Arguments(JSUndefined.Value, result));
            }

            // Per §27.2.4.7, an abrupt completion while obtaining the constructor's
            // "resolve" method, or while obtaining or stepping the iterable, must
            // reject the returned promise rather than throwing synchronously.
            try
            {
                GetPromiseResolve(constructor);
                var en = iterable.GetIterableEnumerator();
                while (en.MoveNext(out var hasValue, out var item, out var _))
                {
                    if (!hasValue)
                        continue;

                    var currentIndex = index++;
                    result[currentIndex] = JSUndefined.Value;
                    remaining++;

                    var alreadyCalled = false;
                    var resolveElement = new JSFunction((in Arguments args) =>
                    {
                        if (alreadyCalled)
                            return JSUndefined.Value;
                        alreadyCalled = true;
                        var entry = new JSObject();
                        entry[KeyStrings.GetOrCreate("status")] = JSValue.CreateString("fulfilled");
                        entry[KeyStrings.GetOrCreate("value")] = args.Get1();
                        result[currentIndex] = entry;
                        Settle();
                        return JSUndefined.Value;
                    }, "", "function () { [native code] }", length: 1, createPrototype: false);
                    var rejectElement = new JSFunction((in Arguments args) =>
                    {
                        if (alreadyCalled)
                            return JSUndefined.Value;
                        alreadyCalled = true;
                        var entry = new JSObject();
                        entry[KeyStrings.GetOrCreate("status")] = JSValue.CreateString("rejected");
                        entry[KeyStrings.GetOrCreate("reason")] = args.Get1();
                        result[currentIndex] = entry;
                        Settle();
                        return JSUndefined.Value;
                    }, "", "function () { [native code] }", length: 1, createPrototype: false);

                    if (item is JSPromise promise)
                    {
                        promise.Then(resolveElement.Delegate, rejectElement.Delegate);
                        continue;
                    }

                    if (!item.IsNullOrUndefined)
                    {
                        var then = item[KeyStrings.then];
                        if (then.IsFunction)
                        {
                            then.InvokeFunction(new Arguments(item, resolveElement, rejectElement));
                            continue;
                        }
                    }

                    resolveElement.InvokeFunction(new Arguments(JSUndefined.Value, item));
                }

                Settle();
            }
            catch (JSException ex)
            {
                reject.InvokeFunction(new Arguments(JSUndefined.Value, ex.Error ?? JSException.JSErrorFrom(ex)));
            }
        });
    }

    [JSExport("allSettledKeyed", Length = 1)]
    public static JSValue AllSettledKeyed(in Arguments a)
    {
        var input = a.Get1();
        if (input is not JSObject obj)
            return CreatePromiseFromConstructor(a.This, (resolve, _) =>
            {
                resolve.InvokeFunction(new Arguments(JSUndefined.Value, new JSObject()));
            });

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

        return CreatePromiseFromConstructor(a.This, (resolve, _) =>
        {
            resolve.InvokeFunction(new Arguments(JSUndefined.Value, result));
        });
    }

    [JSExport("any", Length = 1)]
    public static JSValue Any(in Arguments a)
    {
        var iterable = a.Get1();
        var constructor = a.This;
        var errors = JSValue.CreateArray();
        uint errorIndex = 0;

        return CreatePromiseFromConstructor(constructor, (resolve, reject) =>
        {
            // remainingElementsCount (§27.2.4.3) starts at 1 and is decremented per
            // rejection plus once after iteration; the promise rejects with an
            // AggregateError only when every element has rejected. A fulfillment
            // resolves the result. Elements settle synchronously (a native promise
            // already dispatches its reaction on a microtask via Then).
            int remaining = 1;

            void RejectIfDone()
            {
                if (--remaining == 0)
                    reject.InvokeFunction(new Arguments(JSUndefined.Value, NewAggregateError(errors)));
            }

            // Per §27.2.4.3, an abrupt completion while obtaining the constructor's
            // "resolve" method, or while obtaining or stepping the iterable, must
            // reject the returned promise rather than throwing synchronously.
            try
            {
                GetPromiseResolve(constructor);
                var en = iterable.GetIterableEnumerator();
                while (en.MoveNext(out var hasValue, out var item, out var _))
                {
                    if (!hasValue)
                        continue;

                    var currentIndex = errorIndex++;
                    errors[currentIndex] = JSUndefined.Value;
                    remaining++;

                    var alreadyCalled = false;
                    var resolveElement = new JSFunction((in Arguments args) =>
                    {
                        if (alreadyCalled)
                            return JSUndefined.Value;
                        alreadyCalled = true;
                        resolve.InvokeFunction(new Arguments(JSUndefined.Value, args.Get1()));
                        return JSUndefined.Value;
                    }, "", "function () { [native code] }", length: 1, createPrototype: false);
                    var rejectElement = new JSFunction((in Arguments args) =>
                    {
                        if (alreadyCalled)
                            return JSUndefined.Value;
                        alreadyCalled = true;
                        errors[currentIndex] = args.Get1();
                        RejectIfDone();
                        return JSUndefined.Value;
                    }, "", "function () { [native code] }", length: 1, createPrototype: false);

                    if (item is JSPromise promise)
                    {
                        promise.Then(resolveElement.Delegate, rejectElement.Delegate);
                        continue;
                    }

                    if (!item.IsNullOrUndefined)
                    {
                        var then = item[KeyStrings.then];
                        if (then.IsFunction)
                        {
                            then.InvokeFunction(new Arguments(item, resolveElement, rejectElement));
                            continue;
                        }
                    }

                    resolveElement.InvokeFunction(new Arguments(JSUndefined.Value, item));
                }

                RejectIfDone();
            }
            catch (JSException ex)
            {
                reject.InvokeFunction(new Arguments(JSUndefined.Value, ex.Error ?? JSException.JSErrorFrom(ex)));
            }
        });
    }

    // Builds an AggregateError whose "errors" property holds the collected
    // rejection reasons, using the realm's AggregateError constructor so that
    // the result is `instanceof AggregateError` with the correct prototype.
    private static JSValue NewAggregateError(JSValue errors)
    {
        var constructor = (JSEngine.CurrentContext as JSObject)?[KeyStrings.GetOrCreate("AggregateError")];
        if (constructor != null && constructor.IsFunction)
            return constructor.CreateInstance(new Arguments(JSUndefined.Value, errors));

        return errors;
    }
}
