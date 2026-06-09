using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Extensions;
using Broiler.JavaScript.Extensions;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.Runtime;
using System;
using System.Collections.Concurrent;
using System.Threading;
using Broiler.JavaScript.BuiltIns.Function;
using Broiler.JavaScript.BuiltIns.Symbol;
using Broiler.JavaScript.LinqExpressions.LinqExpressions.GeneratorsV2;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Generator;

[JSClassGenerator("Generator")]
public partial class JSGenerator : JSObject, IJSGenerator
{
    private static readonly ConcurrentDictionary<string, JSObject> IteratorPrototypes = new(StringComparer.Ordinal);
    readonly IElementEnumerator en;
    private ClrGeneratorV2 cg;
    private readonly string name;

    internal JSValue value;
    internal bool done;
    private bool executing;

    public JSGenerator(in Arguments a) : base(JSEngine.NewTargetPrototype) => throw JSEngine.NewTypeError("Generator is not a constructor");

    public JSGenerator(IElementEnumerator en, string name) : this()
    {
        this.en = en;
        this.name = name;

        if (name.EndsWith("Iterator", StringComparison.Ordinal))
        {
            var iteratorPrototype = GetIteratorPrototype();
            BasePrototypeObject = iteratorPrototype == null
                ? IteratorPrototypes.GetOrAdd(name, CreateIteratorPrototype)
                : IteratorPrototypes.GetOrAdd($"{name}:{iteratorPrototype.UniqueID}", _ => CreateIteratorPrototype(name, iteratorPrototype));
        }
    }

    public JSGenerator(ClrGeneratorV2 g) : this()
    {
        cg = g;
        value = JSUndefined.Value;
    }

    public override string ToString() => $"[object {name}]";

    private static JSObject GetIteratorPrototype()
        => ((JSEngine.Current as JSObject)?[KeyStrings.GetOrCreate("Iterator")] as JSFunction)?.prototype;

    private static JSObject CreateIteratorPrototype(string name)
        => CreateIteratorPrototype(name, null);

    private static JSObject CreateIteratorPrototype(string name, JSObject prototypeBase)
    {
        var prototype = new JSObject
        {
            BasePrototypeObject = prototypeBase
        };
        prototype.FastAddValue(KeyStrings.next, JSValue.CreateFunction(IteratorNext, "next", null, 0, false), JSPropertyAttributes.ConfigurableValue);
        prototype.FastAddValue((IJSSymbol)JSSymbol.iterator, JSValue.CreateFunction(static (in Arguments a) => a.This, "[Symbol.iterator]", null, 0, false), JSPropertyAttributes.ConfigurableValue);
        prototype.FastAddValue((IJSSymbol)JSSymbol.toStringTag, JSValue.CreateString(name), JSPropertyAttributes.ConfigurableReadonlyValue);
        return prototype;
    }

    private static JSValue IteratorNext(in Arguments a)
    {
        if (a.This is not JSGenerator generator)
            throw JSEngine.NewTypeError("Iterator.prototype.next called on incompatible receiver");

        return generator.Next(in a);
    }

    [JSExport("toString")]
    public new JSValue ToString(in Arguments a) => JSValue.CreateString(ToString());


    public JSValue Return(JSValue value)
    {
        ThrowIfExecuting();
        if (cg != null && cg.HasDelegatedEnumerator)
        {
            try
            {
                var delegatedResult = cg.TryReturnDelegated(value, out var iteratorResult)
                    ? iteratorResult
                    : JSUndefined.Value;

                if (!delegatedResult.IsUndefined)
                {
                    var delegatedDone = delegatedResult[KeyStrings.done].BooleanValue;
                    var delegatedValue = delegatedResult[KeyStrings.value];
                    if (!delegatedDone)
                    {
                        done = false;
                        this.value = delegatedValue;
                        return ValueObject;
                    }

                    cg.EndDelegation(delegatedValue);
                    done = true;
                    this.value = JSUndefined.Value;
                    return NewWithProperties().AddProperty(KeyStrings.value, delegatedValue).AddProperty(KeyStrings.done, JSValue.BooleanTrue);
                }

                cg.EndDelegation(value);
                done = true;
                this.value = JSUndefined.Value;
                return NewWithProperties().AddProperty(KeyStrings.value, value).AddProperty(KeyStrings.done, JSValue.BooleanTrue);
            }
            catch (Exception ex)
            {
                cg.EndDelegation();
                cg.InjectException(JSException.From(ex));
                return Next();
            }
        }

        // A generator parked at a `yield` must be resumed with a "return" completion
        // so enclosing `finally` blocks run (e.g. IteratorClose for a destructuring or
        // for-of that was suspended mid-iteration). A `finally` may override the result
        // by completing abruptly itself (its own `return`/`throw`) or by yielding.
        if (!done && cg != null && cg.IsSuspendedAtYield)
        {
            cg.InjectException(new GeneratorReturnCompletion(value));
            try
            {
                return Next();
            }
            catch (GeneratorReturnCompletion ret)
            {
                done = true;
                this.value = JSUndefined.Value;
                return NewWithProperties().AddProperty(KeyStrings.value, ret.Value).AddProperty(KeyStrings.done, JSValue.BooleanTrue);
            }
        }

        // Suspended-start or already-finished: complete without running the body.
        done = true;
        this.value = JSUndefined.Value;

        return NewWithProperties().AddProperty(KeyStrings.value, value).AddProperty(KeyStrings.done, JSValue.BooleanTrue);
    }

    public JSValue Throw(JSValue value)
    {
        ThrowIfExecuting();
        if (cg != null && cg.HasDelegatedEnumerator)
        {
            try
            {
                if (!cg.TryThrowDelegated(value, out var delegatedResult))
                {
                    if (cg.DelegatedEnumerator is IReturnableEnumerator returnable)
                        returnable.Return();

                    cg.EndDelegation();
                    throw JSEngine.NewTypeError("Iterator does not provide a throw method");
                }

                var delegatedDone = delegatedResult[KeyStrings.done].BooleanValue;
                var delegatedValue = delegatedResult[KeyStrings.value];
                if (!delegatedDone)
                {
                    done = false;
                    this.value = delegatedValue;
                    return ValueObject;
                }

                cg.EndDelegation(delegatedValue);
                return Next(delegatedValue);
            }
            catch (Exception ex)
            {
                cg.EndDelegation();
                cg.InjectException(JSException.From(ex));
                return Next();
            }
        }

        if (cg != null)
        {
            cg.InjectException(JSException.FromValue(value));
            return Next();
        }

        throw JSException.FromValue(value);
    }

    public JSValue ValueObject => NewWithProperties().AddProperty(KeyStrings.value, value).AddProperty(KeyStrings.done, done ? JSValue.BooleanTrue : JSValue.BooleanFalse);

    public bool MoveNext(JSValue replaceOld, out JSValue item)
    {
        var c = JSEngine.Current as IJSExecutionContext;
        var top = c?.Top;
        ThrowIfExecuting();

        if (done)
        {
            item = JSUndefined.Value;
            value = item;
            return false;
        }

        try
        {
            executing = true;
            // c.Top = cg.StackItem;
            cg.Next(replaceOld, out item, out done);
            value = item;

            if (!done)
                return true;

            value = item;
            done = true;

            return false;
        }
        catch
        {
            done = true;
            value = JSUndefined.Value;
            throw;
        }
        finally
        {
            executing = false;
            if (c != null) c.Top = top;
        }
    }

    public JSValue Next(JSValue replaceOld = null)
    {
        ThrowIfExecuting();

        if (done)
        {
            value = JSUndefined.Value;
            return ValueObject;
        }

        if (en != null)
        {
            if (en.MoveNext(out JSValue item))
            {
                value = item;
                return ValueObject;
            }

            done = true;
            value = JSUndefined.Value;

            return ValueObject;
        }

        var c = JSEngine.Current as IJSExecutionContext;
        var top = c?.Top;
        
        try
        {
            executing = true;
            cg.Next(replaceOld, out value, out done);

            // `yield*` re-yields the delegated iterator's result object unchanged
            // (GeneratorYield(innerResult)); surface it instead of re-boxing.
            if (!done && cg.DelegatedRawResult is JSValue rawResult)
                return rawResult;

            return ValueObject;
        }
        catch
        {
            done = true;
            value = JSUndefined.Value;
            throw;
        }
        finally
        {
            executing = false;
            if (c != null) c.Top = top;
        }
    }

    private void ThrowIfExecuting()
    {
        if (!executing)
            return;

        done = true;
        value = JSUndefined.Value;
        throw JSEngine.NewTypeError("Generator is already running");
    }

    public override IElementEnumerator GetElementEnumerator() => new ElementEnumerator(this);

    public override IElementEnumerator GetAsyncIterableEnumerator()
        => IsAsyncGenerator
            ? new ElementEnumerator(this)
            : base.GetAsyncIterableEnumerator();

    internal bool IsAsyncGenerator => cg?.IsAsyncGenerator == true;

    // Drives an async generator step (next/return/throw) and resolves the
    // consumer's promise only at a user `yield` or completion. Internal awaits —
    // an explicit `await`, and the per-iteration await of `for await` — are awaited
    // here and the generator is resumed without surfacing them to the consumer. The
    // suspension kind is carried by ClrGeneratorV2.LastYieldWasAwait, set from
    // GeneratorState.IsAwait (await vs yield are otherwise both lowered to Yield).
    // A user yield also awaits its operand (AsyncGeneratorYield) before surfacing,
    // so `yield <promise>` surfaces the settled value.
    private JSValue DriveAsync(Func<JSValue> advance)
    {
        JSValue result;
        try
        {
            result = advance();
        }
        catch (Exception ex)
        {
            return JSEngine.CreateResolvedOrRejectedPromise(JSException.ErrorFrom(ex), false);
        }

        while (true)
        {
            // A completion resolves the consumer promise with the { value, done }
            // iterator-result object directly.
            if (done || cg == null)
                return JSEngine.CreateResolvedOrRejectedPromise(result, true);

            var operand = value ?? JSUndefined.Value;
            var isThenable = !operand.IsNullOrUndefined && operand[KeyStrings.then].IsFunction;

            if (cg.LastYieldWasAwait)
            {
                // Internal await: await `operand`, then resume the generator with the
                // settled value (a rejection is thrown back in at the await point).
                if (!isThenable)
                {
                    try { result = Next(operand); continue; }
                    catch (Exception ex) { return JSEngine.CreateResolvedOrRejectedPromise(JSException.ErrorFrom(ex), false); }
                }

                return AwaitThenable(operand,
                    settled => DriveAsync(() => Next(settled)),
                    error => DriveAsync(() => Throw(error)));
            }

            // User yield (AsyncGeneratorYield): await the yielded value, then surface
            // the settled value as { value, done:false }; a rejected operand is thrown
            // back into the generator at the yield point.
            if (!isThenable)
                return JSEngine.CreateResolvedOrRejectedPromise(result, true);

            return AwaitThenable(operand,
                settled => JSEngine.CreateResolvedOrRejectedPromise(
                    NewWithProperties().AddProperty(KeyStrings.value, settled).AddProperty(KeyStrings.done, JSValue.BooleanFalse),
                    true),
                error => DriveAsync(() => Throw(error)));
        }
    }

    // Awaits a thenable on the pumped synchronization context (see
    // JSAsyncFunction.ToPromise for the rationale), invoking onFulfilled/onRejected
    // with the settled value/reason; the returned promise resolves to their result.
    private static JSValue AwaitThenable(JSValue thenable, Func<JSValue, JSValue> onFulfilled, Func<JSValue, JSValue> onRejected)
    {
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

            thenable.InvokeMethod(in KeyStrings.then,
                JSValue.CreateFunction((in Arguments a) =>
                {
                    var settled = a.Get1();
                    Queue(() =>
                    {
                        try { resolve(onFulfilled(settled)); }
                        catch (Exception ex) { reject(JSException.JSErrorFrom(ex)); }
                    });
                    return JSUndefined.Value;
                }),
                JSValue.CreateFunction((in Arguments a) =>
                {
                    var error = a.Get1();
                    Queue(() =>
                    {
                        try { resolve(onRejected(error)); }
                        catch (Exception ex) { reject(JSException.JSErrorFrom(ex)); }
                    });
                    return JSUndefined.Value;
                }));
        });
    }

    private struct ElementEnumerator(JSGenerator generator) : IElementEnumerator
    {
        int index = -1;

        public bool MoveNext(out JSValue value)
        {
            generator.Next();
            if (!generator.done)
            {
                index++;
                value = generator.value;
                return true;
            }

            value = JSUndefined.Value;
            return false;
        }

        public bool MoveNext(out bool hasValue, out JSValue value, out uint index)
        {
            generator.Next();

            if (!generator.done)
            {
                this.index++;
                index = (uint)this.index;
                hasValue = true;
                value = generator.value;
                return true;
            }

            index = 0;
            value = JSUndefined.Value;
            hasValue = false;

            return false;
        }

        public bool MoveNextOrDefault(out JSValue value, JSValue @default)
        {
            generator.Next();

            if (!generator.done)
            {
                index++;
                value = generator.value;
                return true;
            }

            value = @default;
            return false;
        }

        public JSValue NextOrDefault(JSValue @default)
        {
            generator.Next();

            if (!generator.done)
            {
                index++;
                return generator.value;
            }

            return @default;
        }
    }

    [JSExport("next", Length = 1)]
    public JSValue Next(in Arguments a)
    {
        if (IsAsyncGenerator)
        {
            // Per spec, next() with no argument resumes the suspended yield with
            // `undefined`. Pass it explicitly (not null) so cg.Next does not fall
            // back to the stale LastValue — which, in an async generator, holds the
            // value of the most recent internal await (e.g. the for-await iteration
            // value) and would otherwise leak into the user yield's result.
            var asyncNextValue = a.Length == 0 ? JSUndefined.Value : a.Get1();
            return DriveAsync(() => Next(asyncNextValue));
        }

        var nextValue = a.Length == 0 ? null : a.Get1();
        return Next(nextValue);
    }

    [JSExport("return", Length = 1)]
    public JSValue Return(in Arguments a)
    {
        var returnValue = a.Get1();
        return IsAsyncGenerator
            ? DriveAsync(() => Return(returnValue))
            : Return(returnValue);
    }

    [JSExport("throw", Length = 1)]
    public JSValue Throw(in Arguments a)
    {
        var throwValue = a.Get1();
        return IsAsyncGenerator
            ? DriveAsync(() => Throw(throwValue))
            : Throw(throwValue);
    }
}
