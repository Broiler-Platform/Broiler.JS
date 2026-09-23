using System;
using System.Threading;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.BuiltIns.Promise;
using Broiler.JavaScript.BuiltIns.Symbol;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Extensions;
using Broiler.JavaScript.Engine.Core;

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

    /// <summary>
    /// Runs <paramref name="generator"/> as the body of an async function whose start has already
    /// happened: it resumes the body now, runs it synchronously up to its first <c>await</c>, and
    /// returns the promise that settles with the body's completion — AsyncBlockStart.
    /// </summary>
    /// <remarks>
    /// The module linker uses it to execute a module with top-level <c>await</c>, whose body is
    /// compiled as a generator it has already advanced past the module's instantiation.
    /// </remarks>
    public static JSValue ResumeAsyncBody(IJSGenerator generator)
        => ToPromise(generator ?? throw new ArgumentNullException(nameof(generator)), JSUndefined.Value);

    private static JSValue ToPromise(IJSGenerator gen, JSValue lastResult)
    {
        var activation = new Activation(gen);
        activation.Step(lastResult, isThrow: false);
        return activation.Promise;
    }

    /// <summary>
    /// One running async body and its promise: AsyncBlockStart and the Await steps (§27.7.5.3).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The body is a generator that suspends at each <c>await</c> with the awaited value. Await is
    /// PromiseResolve(%Promise%, value) plus PerformPromiseThen on the result with the resumption
    /// as its reactions, so the body resumes IN the reaction job: one job after a native promise
    /// or a plain value settles, and a thenable's <c>then</c> is called in a job of its own
    /// (NewPromiseResolveThenableJob) rather than at the await.
    /// </para>
    /// <para>
    /// This replaces a driver that invoked <c>then</c> on the awaited value — observably, through
    /// the current <c>Promise.prototype.then</c>, and synchronously for a thenable — and queued
    /// the resumption as a job of its own from inside the reaction: two jobs per await where the
    /// spec takes one. It also made a new promise at every await step and resolved the previous
    /// step's promise with it, so the function's promise adopted its completion two jobs per step
    /// late. There is now one promise, resolved with the body's completion value (adopting a
    /// thenable, as the promise's resolve function does) or rejected with what it threw.
    /// </para>
    /// </remarks>
    private sealed class Activation
    {
        private readonly IJSGenerator generator;
        private readonly Action<JSValue> onFulfilled;
        private readonly Action<JSValue> onRejected;

        internal readonly JSPromise Promise;

        internal Activation(IJSGenerator generator)
        {
            this.generator = generator;
            Promise = new JSPromise(static (_, _) => { });
            onFulfilled = value => Step(value, isThrow: false);
            onRejected = reason => Step(reason, isThrow: true);
        }

        /// <summary>Resumes the body with a normal or a throw completion and runs it to its next
        /// <c>await</c> or its end.</summary>
        internal void Step(JSValue value, bool isThrow)
        {
            bool more;
            JSValue r;
            try
            {
                if (isThrow)
                {
                    // IJSGenerator.Throw advances the body itself and hands back its next step as
                    // an iterator result; that step is what is settled or awaited here. (Resuming
                    // WITH that result, as an earlier driver did, advanced the body twice.)
                    var step = generator.Throw(value);
                    more = !step[KeyStrings.done].BooleanValue;
                    r = step[KeyStrings.value];
                }
                else
                {
                    more = generator.MoveNext(value, out r);
                }
            }
            catch (Exception ex)
            {
                Promise.Reject(JSException.JSErrorFrom(ex));
                return;
            }

            if (!more)
            {
                // Call(promiseCapability.[[Resolve]], undefined, « result »): adopts a thenable
                // (its `then` read now, called in a job), fulfils with anything else.
                Promise.Resolve(r);
                return;
            }

            Await(r);
        }

        private void Await(JSValue value)
        {
            JSPromise promise;
            try
            {
                // PromiseResolve reads a native promise's `constructor`, which can throw: that is
                // an abrupt completion of the Await itself, thrown into the body at the await.
                promise = JSPromise.PromiseResolveIntrinsic(value);
            }
            catch (Exception ex)
            {
                Step(JSException.JSErrorFrom(ex), isThrow: true);
                return;
            }

            // The pump being run on this thread, if any — see JSContext.PostJob case 2. Captured
            // at the await, because the awaited promise may have been created, and may settle,
            // elsewhere.
            var continuationContext = SynchronizationContext.Current
                ?? (JSEngine.Current as JSContext)?.synchronizationContext;
            promise.AwaitReaction(onFulfilled, onRejected, continuationContext);
        }
    }
}
