using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Broiler.JavaScript.ExpressionCompiler.ClosureSeparator;
using Broiler.JavaScript.ExpressionCompiler.Expressions;

namespace Broiler.JavaScript.ExpressionCompiler.Runtime;

/// <summary>
/// A nested lambda whose IL has not been generated yet, and the thunk that generates it on
/// first invocation.
/// </summary>
/// <remarks>
/// <para>
/// This is roadmap item 1-1 (lazy function compilation), placed one layer lower than the item
/// describes it. The item proposes deferring a function's <em>parse and compile</em> and names
/// four spec-visible risks for doing so — early errors inside a never-called function are still
/// syntax errors, a deferred body must compile against the scope as it was at closure creation,
/// a direct <c>eval</c> can introduce bindings into enclosing scopes, and generator/async bodies
/// are rewritten into state machines. Every one of those is a property of the front end, and
/// every one of them is <em>already settled</em> by the time a lambda reaches here: the source
/// is parsed, early errors have thrown, <see cref="LambdaRewriter"/> has decided which variables
/// are captured and boxed them, and <c>GeneratorRewriter</c> has run. What is left is generating
/// machine code for a tree that is already correct — and on function-dense source that is
/// <b>89%</b> of front-end cost (parse 0.5%, expression-tree construction 11%).
/// </para>
/// <para>
/// So the deferral is of IL generation alone. It is bounded by construction: the deferred work
/// cannot observe anything, cannot fail differently for any input the eager path accepts, and
/// produces byte-identical IL, because it is the same call on the same tree — only later.
/// </para>
/// <para>
/// The unit is the <em>syntactic site</em>, not the closure instance. <c>Relay</c> runs once per
/// nested lambda while the enclosing lambda is emitted, and <c>Create</c> runs once per closure
/// instantiation; generation is memoized on the shared site while each instance still binds its
/// own boxes. A function defined once and instantiated a million times generates once, and a
/// function instantiated once and never called generates never.
/// </para>
/// </remarks>
internal sealed class DeferredMethod
{
    private readonly bool enableJavaScriptTailCalls;
    private readonly object gate = new();

    /// <summary>
    /// The delegate type this site produces. Held separately from <see cref="lambda"/> because
    /// the tree is released once the code exists and this outlives it.
    /// </summary>
    private readonly Type delegateType;

    // Cleared by Force. A site is registered under a GCHandle that is never freed, so anything
    // still referenced here is retained for the life of the process: eagerly that is a
    // DynamicMethod, and while deferred it is the whole expression tree, which is far larger.
    // Keeping them past generation would turn every compiled script into a permanent copy of
    // its own tree — measurably, on a probe that compiles six corpora in one process, the
    // corpora measured last paid for the ones before them.
    private BLambdaExpression lambda;
    private IMethodBuilder methodBuilder;

    private DynamicMethod method;

    public DeferredMethod(
        BLambdaExpression lambda,
        IMethodBuilder methodBuilder,
        bool enableJavaScriptTailCalls)
    {
        this.lambda = lambda;
        this.delegateType = lambda.Type;
        this.methodBuilder = methodBuilder;
        this.enableJavaScriptTailCalls = enableJavaScriptTailCalls;
    }

    /// <summary>
    /// The generated method, generating it if this is the first caller.
    /// </summary>
    /// <remarks>
    /// Locked rather than <see cref="Lazy{T}"/> so the generation of a nested site that this
    /// one's own emission triggers cannot deadlock against it: the lock is per site and
    /// generation of a site never re-enters that same site.
    /// </remarks>
    public DynamicMethod Force()
    {
        var generated = Volatile.Read(ref method);
        if (generated != null)
            return generated;

        lock (gate)
        {
            generated = method;
            if (generated != null)
                return generated;

            // Generated on the calling thread, with no stack handoff. The emitter recurses over
            // the tree and eagerly ran inside the enclosing compilation's single
            // CompilationStack crossing, so hopping to a sized worker here looks like the
            // matching thing to do — and it is the wrong thing, for the reason item 1-2 already
            // recorded about short sources: the handoff is a fixed ~180 us, which is nothing
            // against one whole-script compilation and unaffordable once per function. Doing it
            // took the repository suite from 3.5 minutes to over 20, at 12% CPU — blocked, not
            // working.
            //
            // It is also unnecessary. ILCodeGenerator derives from StackGuard, which measures
            // consumed stack and segments onto a fresh one only when a tree is actually deep
            // enough to need it. That is the mechanism 1-2 built for exactly this, and it costs
            // nothing on the shallow trees that are almost all of them.
            var (emitted, _, _) = lambda.CompileToBoundDynamicMethod(
                methodBuilder: methodBuilder,
                captureDiagnostics: false,
                enableJavaScriptTailCalls: enableJavaScriptTailCalls);

            Volatile.Write(ref method, emitted);
            // Released here so a forced site retains exactly what an eagerly generated one
            // does, and no more.
            lambda = null;
            methodBuilder = null;
            return emitted;
        }
    }

    /// <summary>
    /// One closure instance of a deferred site: the boxes captured when the closure was
    /// created, plus the real delegate once it exists.
    /// </summary>
    /// <remarks>
    /// The instance is what the thunk delegate is bound to, so <see cref="Resolve"/> is on the
    /// call path of every invocation of a deferred function — a field read and a null test once
    /// warm. It is deliberately not virtual and not locked: two threads racing to resolve the
    /// same instance both produce a delegate over the same generated method and the same boxes,
    /// so the loser's copy is equivalent and discarded.
    /// </remarks>
    public sealed class Instance(DeferredMethod site, IMethodRepository repository, Box[] boxes)
    {
        internal static readonly MethodInfo ResolveMethod =
            typeof(Instance).GetMethod(nameof(Resolve), BindingFlags.Public | BindingFlags.Instance);

        internal static readonly FieldInfo ResolvedField =
            typeof(Instance).GetField(nameof(resolved), BindingFlags.NonPublic | BindingFlags.Instance);

        private Delegate resolved;

        public Delegate Resolve()
        {
            var current = Volatile.Read(ref resolved);
            if (current != null)
                return current;

            var created = site.Force().CreateDelegate(
                site.delegateType,
                new Closures(repository, boxes, string.Empty, string.Empty));
            Volatile.Write(ref resolved, created);
            return created;
        }
    }

    public Delegate CreateThunk(IMethodRepository repository, Box[] boxes)
        => ThunkFactory.For(delegateType).CreateDelegate(delegateType, new Instance(this, repository, boxes));

    /// <summary>
    /// Builds, once per delegate type in the process, a stub method with that delegate's exact
    /// signature which resolves its instance and forwards to the real delegate.
    /// </summary>
    /// <remarks>
    /// A stub has to be generated rather than written in C# because the signature is not known
    /// here — this assembly is a leaf and cannot name <c>JSFunctionDelegate</c>, and the
    /// parameter it takes is a by-ref struct, so no <c>Func&lt;&gt;</c> shape covers it. Keying
    /// the cache on the delegate type rather than the site is what keeps this from being the
    /// cost it is avoiding: jQuery's 532 deferred functions share one stub, and the per-instance
    /// price is the same single <c>CreateDelegate</c> the eager path already paid.
    /// </remarks>
    private static class ThunkFactory
    {
        private static readonly Dictionary<Type, DynamicMethod> cache = [];

        public static DynamicMethod For(Type delegateType)
        {
            lock (cache)
            {
                if (cache.TryGetValue(delegateType, out var existing))
                    return existing;

                var built = Build(delegateType);
                cache[delegateType] = built;
                return built;
            }
        }

        /// <summary>
        /// Whether a delegate type can be forwarded by a generated stub. Everything a stub has
        /// to do is load its arguments in order and call <c>Invoke</c>, which works for any
        /// signature <see cref="DynamicMethod"/> accepts — including the by-ref <c>in</c>
        /// parameter every JavaScript function takes.
        /// </summary>
        public static bool CanForward(Type delegateType)
            => delegateType != null
                && typeof(Delegate).IsAssignableFrom(delegateType)
                && delegateType.GetMethod("Invoke") != null;

        private static DynamicMethod Build(Type delegateType)
        {
            var invoke = delegateType.GetMethod("Invoke");
            var parameters = invoke.GetParameters();
            var parameterTypes = new Type[parameters.Length + 1];
            // Bound to an Instance, so the stub takes it as its leading (closed-over) argument.
            parameterTypes[0] = typeof(Instance);
            for (var i = 0; i < parameters.Length; i++)
                parameterTypes[i + 1] = parameters[i].ParameterType;

            var stub = new DynamicMethod(
                "<deferred-thunk>" + delegateType.Name,
                invoke.ReturnType,
                parameterTypes,
                typeof(Instance),
                true);

            var il = stub.GetILGenerator();

            // The warm path is written out here rather than left to a call, because this stub is
            // on every invocation of every deferred function forever and a DynamicMethod does
            // not inline what it calls. Reading the field inline and branching to the slow path
            // only when it is null took the steady-state cost of the whole mechanism from ~2.5%
            // to what is measured in the roadmap; calling Resolve() unconditionally is the same
            // logic and a real regression on call-heavy code.
            //
            // The field is private and the stub is emitted with skipVisibility, which is what
            // makes reading it directly legal. A plain (non-volatile) read is correct: the
            // publishing write is volatile, and the only way to observe a stale null is to take
            // the slow path, which returns the same delegate.
            var slow = il.DefineLabel();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, Instance.ResolvedField);
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Brfalse_S, slow);

            EmitForward(il, delegateType, invoke, parameters.Length);

            il.MarkLabel(slow);
            il.Emit(OpCodes.Pop);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, Instance.ResolveMethod);
            EmitForward(il, delegateType, invoke, parameters.Length);

            return stub;
        }

        private static void EmitForward(ILGenerator il, Type delegateType, MethodInfo invoke, int parameterCount)
        {
            il.Emit(OpCodes.Castclass, delegateType);
            for (var i = 0; i < parameterCount; i++)
                il.Emit(OpCodes.Ldarg, i + 1);
            il.Emit(OpCodes.Callvirt, invoke);
            il.Emit(OpCodes.Ret);
        }
    }

    /// <summary>
    /// Whether <paramref name="lambda"/> may have its IL generation deferred.
    /// </summary>
    public static bool CanDefer(BLambdaExpression lambda, bool captureDiagnostics)
        // Two separate diagnostic switches, and both have to turn deferral off. CaptureDiagnostics
        // collects the IL and expression text during generation and hands it to the Closures the
        // delegate is bound to, so a deferred site would have none to give at the moment it is
        // asked. ILCodeGenerator.GenerateLogs — what BROILER_GENERATE_IL_LOGS sets — is a
        // different field on a different object, and deferral would reorder and delay the log it
        // writes, which is the opposite of what someone reading that log wants.
        => !captureDiagnostics
            && !Generator.ILCodeGenerator.GenerateLogs
            && DeferredMethodCompilation.Enabled
            && ThunkFactory.CanForward(lambda?.Type);
}

/// <summary>
/// Process-wide switch for item 1-1's deferred IL generation.
/// </summary>
/// <remarks>
/// Present for the same reason <c>BROILER_JS_COMPILE_STACK_BYTES</c> and
/// <c>BROILER_JS_REWRITER_INDEX_THRESHOLD</c> are: the change has a losing side — a function
/// that <em>is</em> called pays one extra delegate hop on every invocation forever — so it has
/// to be measurable against a build that differs in nothing else.
/// </remarks>
public static class DeferredMethodCompilation
{
    public const string EnvironmentVariable = "BROILER_JS_DEFER_IL";

    private static int enabled = ReadConfigured();

    /// <summary>Whether nested-lambda IL generation is deferred to first invocation.</summary>
    public static bool Enabled
    {
        get => Volatile.Read(ref enabled) != 0;
        set => Volatile.Write(ref enabled, value ? 1 : 0);
    }

    private static int ReadConfigured()
        => string.Equals(
            Environment.GetEnvironmentVariable(EnvironmentVariable),
            "0",
            StringComparison.Ordinal)
            ? 0
            : 1;
}
