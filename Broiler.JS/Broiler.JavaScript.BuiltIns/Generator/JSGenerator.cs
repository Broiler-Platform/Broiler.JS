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

    // An async generator's queued next/return/throw requests (AsyncGeneratorQueue), and whether
    // one of them is running — see DriveAsync.
    private System.Collections.Generic.Queue<(Action<Promise.JSPromise> Start, Promise.JSPromise Promise)> asyncRequests;
    private bool asyncStepRunning;

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

    // CLR-side display only. %GeneratorPrototype% deliberately exposes NO own JS
    // `toString`: per §27.5 a generator inherits %Object.prototype.toString%, which
    // reports "[object Generator]" from the @@toStringTag on %GeneratorPrototype%.
    public override string ToString() => $"[object {name}]";

    private static JSObject GetIteratorPrototype()
        => Intrinsics.Prototype(KeyStrings.GetOrCreate("Iterator"));

    private static JSObject CreateIteratorPrototype(string name)
        => CreateIteratorPrototype(name, null);

    private static JSObject CreateIteratorPrototype(string name, JSObject prototypeBase)
    {
        var prototype = new JSObject
        {
            BasePrototypeObject = prototypeBase
        };
        prototype.FastAddValue(KeyStrings.next, CreateFunction(IteratorNext, "next", null, 0, false), JSPropertyAttributes.ConfigurableValue);
        prototype.FastAddValue((IJSSymbol)JSSymbol.iterator, CreateFunction(static (in Arguments a) => a.This, "[Symbol.iterator]", null, 0, false), JSPropertyAttributes.ConfigurableValue);
        prototype.FastAddValue((IJSSymbol)JSSymbol.toStringTag, CreateString(name), JSPropertyAttributes.ConfigurableReadonlyValue);
        return prototype;
    }

    private static JSValue IteratorNext(in Arguments a)
    {
        if (a.This is not JSGenerator generator)
            throw JSEngine.NewTypeError("Iterator.prototype.next called on incompatible receiver");

        return generator.Next(in a);
    }

    public JSValue Return(JSValue value)
    {
        ThrowIfExecuting();
        // An async `yield*` hands the return to its delegate's return() (see ClrGeneratorV2).
        if (cg != null && cg.HasAsyncDelegate && !done)
        {
            cg.ReceiveAsyncDelegateReturn(value);
            return Next();
        }

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
                    if (!delegatedDone)
                    {
                        // Sync `yield*` performs GeneratorYield(innerReturnResult) when the
                        // inner `return` result is not done: the result object is surfaced
                        // unchanged and IteratorValue is NOT performed, so its `value` getter
                        // must not run while delegation continues.
                        done = false;
                        return delegatedResult;
                    }

                    var delegatedValue = delegatedResult[KeyStrings.value];
                    cg.EndDelegation(delegatedValue);

                    // The inner iterator's `return` completed (done): yield* completes with
                    // a return completion of `delegatedValue`. Resume the generator body
                    // with that return completion so enclosing `finally` blocks execute
                    // before the generator finishes (a `finally` may override the result).
                    if (!done && cg.IsSuspendedAtYield)
                    {
                        cg.InjectException(new GeneratorReturnCompletion(delegatedValue));
                        try
                        {
                            return Next();
                        }
                        catch (GeneratorReturnCompletion ret)
                        {
                            done = true;
                            this.value = JSUndefined.Value;
                            return NewWithProperties().AddProperty(KeyStrings.value, ret.Value).AddProperty(KeyStrings.done, BooleanTrue);
                        }
                    }

                    done = true;
                    this.value = JSUndefined.Value;
                    return NewWithProperties().AddProperty(KeyStrings.value, delegatedValue).AddProperty(KeyStrings.done, BooleanTrue);
                }

                cg.EndDelegation(value);
                done = true;
                this.value = JSUndefined.Value;
                return NewWithProperties().AddProperty(KeyStrings.value, value).AddProperty(KeyStrings.done, BooleanTrue);
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
                return NewWithProperties().AddProperty(KeyStrings.value, ret.Value).AddProperty(KeyStrings.done, BooleanTrue);
            }
        }

        // Suspended-start or already-finished: complete without running the body.
        done = true;
        this.value = JSUndefined.Value;

        return NewWithProperties().AddProperty(KeyStrings.value, value).AddProperty(KeyStrings.done, BooleanTrue);
    }

    public JSValue Throw(JSValue value)
    {
        ThrowIfExecuting();
        if (cg != null && cg.HasAsyncDelegate && !done)
        {
            // The awaited result of an async `yield*` delegate rejected: the `yield*` throws.
            // Otherwise the throw goes to the delegate's throw().
            if (cg.IsAwaitingAsyncDelegate)
                cg.FailAsyncDelegate(JSException.FromValue(value));
            else
                cg.ReceiveAsyncDelegateThrow(value);

            return Next();
        }

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
                if (!delegatedDone)
                {
                    // GeneratorYield(innerResult) of the inner `throw` result: surface it
                    // unchanged without performing IteratorValue (no `value` read) while
                    // delegation continues.
                    done = false;
                    return delegatedResult;
                }

                var delegatedValue = delegatedResult[KeyStrings.value];
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

        // Only a generator suspended at a `yield` resumes its body with the thrown
        // value. A suspended-start or already-completed generator (GeneratorResumeAbrupt
        // with state "suspendedStart"/"completed") does NOT run the body: it becomes /
        // stays completed and the throw completion is honored by re-throwing the value.
        if (cg != null && !done && cg.IsSuspendedAtYield)
        {
            cg.InjectException(JSException.FromValue(value));
            return Next();
        }

        done = true;
        this.value = JSUndefined.Value;
        throw JSException.FromValue(value);
    }

    public JSValue ValueObject => NewWithProperties().AddProperty(KeyStrings.value, value).AddProperty(KeyStrings.done, done ? BooleanTrue : BooleanFalse);

    public bool MoveNext(JSValue replaceOld, out JSValue item)
    {
        var c = JSEngine.Current as IJSExecutionContext;
        // Depth rather than a frame reference: restoring a depth can only unwind, so it cannot
        // resurrect a frame the body has already left (see CallFrameStack.RestoreDepth).
        var savedDepth = c?.Frames.Depth ?? 0;
        // new.target is always undefined inside a generator body. Clear any ambient
        // new target (e.g. the constructor being run while this generator is iterated
        // as a `new TypedArray(gen)` source) so a `new` inside the body resolves its
        // instance prototype from its own constructor, not the outer new.target.
        var savedNewTarget = c?.CurrentNewTarget;
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

            if (c != null) c.CurrentNewTarget = null;
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
            if (c != null) { c.Frames.RestoreDepth(savedDepth); c.CurrentNewTarget = savedNewTarget; }
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
        // Depth rather than a frame reference: restoring a depth can only unwind, so it cannot
        // resurrect a frame the body has already left (see CallFrameStack.RestoreDepth).
        var savedDepth = c?.Frames.Depth ?? 0;
        // new.target is always undefined inside a generator body; clear any ambient
        // new target so a `new` inside the body is unaffected by an outer construction
        // that happens to be iterating this generator.
        var savedNewTarget = c?.CurrentNewTarget;

        try
        {
            executing = true;
            if (c != null) c.CurrentNewTarget = null;
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
            if (c != null) { c.Frames.RestoreDepth(savedDepth); c.CurrentNewTarget = savedNewTarget; }
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

    // A generator object exposes its values through GetElementEnumerator (for for-of /
    // spread), but those values are NOT own indexed properties. Key enumeration
    // (Object.keys / getOwnPropertyNames / for-in) must walk only the generator's own
    // element slots — otherwise running the generator would leak its yielded values as
    // integer keys (e.g. getOwnPropertyNames of a fresh generator returning ["0", ...]).
    internal override IElementEnumerator GetOwnIndexedElementEnumerator(bool enumerableOnly = false)
        => GetOwnElementSlotEnumerator(enumerableOnly);

    public override IElementEnumerator GetAsyncIterableEnumerator()
        => IsAsyncGenerator
            ? new ElementEnumerator(this)
            : base.GetAsyncIterableEnumerator();

    internal bool IsAsyncGenerator => cg?.IsAsyncGenerator == true;

    // Drives an async generator step (next/return/throw) and settles the promise that step
    // returned only at a user `yield` or completion. Internal awaits — an explicit `await`, and
    // the per-iteration await of `for await` — are awaited here and the generator is resumed
    // without surfacing them to the consumer. The suspension kind is carried by
    // ClrGeneratorV2.LastYieldWasAwait, set from GeneratorState.IsAwait (await vs yield are
    // otherwise both lowered to Yield). A user yield also awaits its operand
    // (AsyncGeneratorYield(? Await(value))) before surfacing, so `yield <promise>` surfaces the
    // settled value.
    //
    // Every Await is the spec's: PromiseResolve(%Promise%, value) plus a reaction that resumes the
    // body in the reaction job itself — one job for a native promise or a plain value, and a
    // thenable's `then` called in a job of its own. It used to invoke `then` on the awaited value
    // (observably, and synchronously for a thenable) and queue the resumption as a second job, to
    // resume at once after a non-thenable, and to settle each step's promise by resolving it
    // with the next step's promise, which adopted it two jobs late. The step's promise is now one
    // promise, settled with the iterator result when the body reaches its `yield` or completion.
    //
    // Requests are queued (AsyncGeneratorEnqueue): a next/return/throw made while a step is still
    // running — suspended at an internal await, or from inside the body — waits for that step to
    // settle and then runs, in order. Without the queue a second `next()` resumed the body at the
    // first step's pending await with its own argument.
    private JSValue DriveAsync(Func<JSValue> advance)
        => EnqueueAsyncRequest(promise => DriveAsync(advance, promise));

    private JSValue EnqueueAsyncRequest(Action<Promise.JSPromise> start)
    {
        var promise = new Promise.JSPromise(static (_, _) => { });
        (asyncRequests ??= new()).Enqueue((start, promise));
        if (!asyncStepRunning)
            RunNextAsyncRequest();

        return promise;
    }

    // An async generator's return(value) request. At a yield — a user yield or a value of a
    // `yield*` delegate — the value is awaited before the body resumes with the return completion,
    // and a rejection resumes it with a throw instead (AsyncGeneratorUnwrapYieldResumption). A
    // generator that has not started or has completed awaits the value and completes the request
    // with it, or rejects it with the rejection (AsyncGeneratorAwaitReturn); the body does not run.
    private void StartAsyncReturn(JSValue returnValue, Promise.JSPromise promise)
    {
        if (!done && cg.IsSuspendedAtYield)
        {
            Await(returnValue,
                settled => DriveAsync(() => Return(settled), promise),
                error => DriveAsync(() => Throw(error), promise));
            return;
        }

        done = true;
        value = JSUndefined.Value;
        Await(returnValue,
            settled => CompleteAsyncStep(promise,
                NewWithProperties().AddProperty(KeyStrings.value, settled).AddProperty(KeyStrings.done, BooleanTrue)),
            error => CompleteAsyncStep(promise, error, rejected: true));
    }

    private void RunNextAsyncRequest()
    {
        if (asyncRequests.Count == 0)
        {
            asyncStepRunning = false;
            return;
        }

        asyncStepRunning = true;
        var (start, promise) = asyncRequests.Dequeue();
        start(promise);
    }

    // Settles the running request's promise and starts the next queued request.
    private void CompleteAsyncStep(Promise.JSPromise promise, JSValue result, bool rejected = false)
    {
        if (rejected)
            promise.Reject(result);
        else
            promise.Resolve(result);

        RunNextAsyncRequest();
    }

    private void DriveAsync(Func<JSValue> advance, Promise.JSPromise promise)
    {
        JSValue result;
        try
        {
            result = advance();
        }
        catch (Exception ex)
        {
            CompleteAsyncStep(promise, JSException.ErrorFrom(ex), rejected: true);
            return;
        }

        // A completion settles the step with the { value, done } iterator-result object.
        if (done || cg == null)
        {
            CompleteAsyncStep(promise, result);
            return;
        }

        var operand = value ?? JSUndefined.Value;

        // A value of an async `yield*` delegate is surfaced as it is: AsyncGeneratorYield of
        // IteratorValue(innerResult), with no Await of its own.
        if (cg.LastYieldWasAsyncDelegateValue)
        {
            CompleteAsyncStep(promise,
                NewWithProperties().AddProperty(KeyStrings.value, operand).AddProperty(KeyStrings.done, BooleanFalse));
            return;
        }

        if (cg.LastYieldWasAwait)
        {
            // Internal await: resume the generator with the settled value; a rejection is thrown
            // back in at the await point.
            Await(operand,
                settled => DriveAsync(() => Next(settled), promise),
                error => DriveAsync(() => Throw(error), promise));
            return;
        }

        // `yield*` surfaces the delegate's own result object while its value is not a thenable
        // (see Next), and awaits a thenable value as a yield does.
        if (cg.DelegatedRawResult != null
            && (operand.IsNullOrUndefined || !operand[KeyStrings.then].IsFunction))
        {
            CompleteAsyncStep(promise, result);
            return;
        }

        // User yield (AsyncGeneratorYield): await the yielded value, then settle the step with
        // { value: settled, done: false }; a rejected operand is thrown back into the generator
        // at the yield point.
        Await(operand,
            settled => CompleteAsyncStep(promise,
                NewWithProperties().AddProperty(KeyStrings.value, settled).AddProperty(KeyStrings.done, BooleanFalse)),
            error => DriveAsync(() => Throw(error), promise));
    }

    // Await(value) for the async-generator driver: `onFulfilled` / `onRejected` run in the promise
    // reaction job (see JSAsyncFunction.Activation). An abrupt PromiseResolve — a throwing
    // `constructor` getter on a native promise — is the Await's own throw, delivered now.
    //
    // Resuming an async generator runs the rest of its body, which is user JavaScript, so where the
    // resumption is dispatched is the same decision a promise reaction makes, and JSContext.PostJob
    // owns it. The pump is captured HERE, at the await, and handed to the reaction: the awaited
    // promise may have been created, and may settle, elsewhere.
    private static void Await(JSValue value, Action<JSValue> onFulfilled, Action<JSValue> onRejected)
    {
        Promise.JSPromise promise;
        try
        {
            promise = Promise.JSPromise.PromiseResolveIntrinsic(value);
        }
        catch (Exception ex)
        {
            onRejected(JSException.ErrorFrom(ex));
            return;
        }

        var continuationContext = SynchronizationContext.Current
            ?? (JSEngine.Current as JSContext)?.synchronizationContext;
        promise.AwaitReaction(onFulfilled, onRejected, continuationContext);
    }

    private struct ElementEnumerator(JSGenerator generator) : IElementEnumerator, IAsyncDelegateIterator
    {
        int index = -1;

        // A native async generator as the delegate of `yield*` in an async generator: its own
        // next/throw/return, each a queued request settling a promise the delegating generator
        // awaits. Stepping it synchronously, as MoveNext does, surfaced each `await` in its body
        // as a delegated value and lost its return value.
        public readonly bool IsAsyncIterator => generator.IsAsyncGenerator;

        public readonly JSValue DelegateNext(JSValue value)
        {
            var asyncGenerator = generator;
            var sent = value ?? JSUndefined.Value;
            return asyncGenerator.DriveAsync(() => asyncGenerator.Next(sent));
        }

        public readonly bool TryDelegateThrow(JSValue value, out JSValue result)
        {
            var asyncGenerator = generator;
            var sent = value ?? JSUndefined.Value;
            result = asyncGenerator.DriveAsync(() => asyncGenerator.Throw(sent));
            return true;
        }

        public readonly bool TryDelegateReturn(JSValue value, out JSValue result)
        {
            var asyncGenerator = generator;
            var sent = value ?? JSUndefined.Value;
            result = asyncGenerator.EnqueueAsyncRequest(promise => asyncGenerator.StartAsyncReturn(sent, promise));
            return true;
        }

        // One `for await` step over an async generator: its own next(), which runs the body to its
        // next `yield` or completion and awaits every `await` on the way, returning the promise of
        // the step's iterator result for the loop to await. Stepping it synchronously through
        // MoveNext, as the sync-iterable fallback does, surfaced each `await` in the body — among
        // them an `await using` disposal — to the loop as an iteration value.
        public JSValue AsyncNextRaw()
        {
            if (generator.IsAsyncGenerator)
            {
                var asyncGenerator = generator;
                return asyncGenerator.DriveAsync(() => asyncGenerator.Next(JSUndefined.Value));
            }

            return MoveNext(out JSValue value)
                ? AsyncIterationStep.ValueResult(value)
                : AsyncIterationStep.DoneResult();
        }

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
            ? EnqueueAsyncRequest(promise => StartAsyncReturn(returnValue, promise))
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
