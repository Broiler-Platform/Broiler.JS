using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Engine.Core;

/// <summary>
/// Provides static access to the current JavaScript execution context and
/// shared infrastructure (error factories, CLR interop, built-in registry).
/// When <c>JSContext</c> lived inside Core every type could reach these
/// members directly.  Now that <c>JSContext</c> has moved to the Engine
/// assembly, this static class keeps the same functionality available to
/// Core without introducing a circular reference.
/// </summary>
public static class JSEngine
{
    // ── Current context ─────────────────────────────────────────────

    [ThreadStatic]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static IJSContext Current;

    /// <summary>
    /// The JavaScript function currently executing on this thread. It is set by
    /// <c>JSFunction.InvokeFunction</c> around the invocation of a function body
    /// and is used to populate the non-strict mapped arguments object's
    /// <c>callee</c> property with the function being invoked.
    /// </summary>
    [ThreadStatic]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static JSValue ExecutingFunction;

    private static readonly AsyncLocal<IJSContext> _current =
        new((e) => { Current = e.CurrentValue ?? e.PreviousValue; });

    public static IJSContext CurrentContext
    {
        get => Current;
        set
        {
            _current.Value = value;
            Current = value;
        }
    }

    internal static CurrentContextScope EnterContext(IJSContext context) => new(context);

    internal readonly struct CurrentContextScope : IDisposable
    {
        private readonly IJSContext previous;
        private readonly bool changed;

        public CurrentContextScope(IJSContext context)
        {
            previous = Current;
            changed = context != null && !ReferenceEquals(context, previous);
            if (changed)
                CurrentContext = context;
        }

        public void Dispose()
        {
            if (changed)
                CurrentContext = previous;
        }
    }

    /// <summary>
    /// Clears the async-local context reference. Called by
    /// <c>JSContext.Dispose()</c> in the Engine assembly.
    /// </summary>
    internal static void ClearAsyncLocal()
    {
        _current.Value = null;
    }

    // ── Built-in registry ───────────────────────────────────────────

    /// <summary>
    /// Gets or sets the built-in object registry used to populate new contexts.
    /// Set by the BuiltIns assembly's module initializer.
    /// </summary>
    public static IBuiltInRegistry BuiltInRegistry { get; set; }

    /// <summary>
    /// True when a host selected the default registry through the explicit bootstrap API.
    /// Explicit hosts never enter the legacy magic-name assembly probing adapter.
    /// </summary>
    internal static bool HasExplicitBuiltInRegistry { get; set; }

    /// <summary>
    /// Delegate for registering Core's source-generated built-in classes.
    /// Wired by Core's module initializer.
    /// </summary>
    internal static Action<JSObject> CoreClassRegistrations { get; set; }

    // ── CLR interop ─────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets the CLR interop provider used to marshal between .NET
    /// objects and JavaScript values.
    /// </summary>
    public static IClrInterop ClrInterop { get; set; } = FallbackClrInterop.Instance;

    /// <summary>
    /// Factory delegate that provides the default CLR module object.
    /// Set by the Clr assembly during initialization.
    /// </summary>
    public static Func<JSObject> ClrModuleProvider { get; set; }

    // ── Error factory delegates (wired by BuiltInsAssemblyInitializer) ──

    internal static Func<string, string, string, int, JSException> CreateTypeError;
    internal static Func<string, string, string, int, JSException> CreateSyntaxError;
    internal static Func<string, string, string, int, JSException> CreateURIError;
    internal static Func<string, string, string, int, JSException> CreateRangeError;
    internal static Func<string, string, string, int, JSException> CreateReferenceError;
    internal static Func<string, string, string, int, JSException> CreateError;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JSException NewTypeError(string message,
        [CallerMemberName] string function = null,
        [CallerFilePath] string filePath = null,
        [CallerLineNumber] int line = 0) =>
        (CreateTypeError ?? throw new InvalidOperationException("JSEngine.CreateTypeError delegate is not initialized. Ensure the BuiltIns assembly module initializer has run."))
            (message, function, filePath, line);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JSException NewSyntaxError(string message,
        [CallerMemberName] string function = null,
        [CallerFilePath] string filePath = null,
        [CallerLineNumber] int line = 0) =>
        (CreateSyntaxError ?? throw new InvalidOperationException("JSEngine.CreateSyntaxError delegate is not initialized. Ensure the BuiltIns assembly module initializer has run."))
            (message, function, filePath, line);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JSException NewURIError(string message,
        [CallerMemberName] string function = null,
        [CallerFilePath] string filePath = null,
        [CallerLineNumber] int line = 0) =>
        (CreateURIError ?? throw new InvalidOperationException("JSEngine.CreateURIError delegate is not initialized. Ensure the BuiltIns assembly module initializer has run."))
            (message, function, filePath, line);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JSException NewRangeError(string message,
        [CallerMemberName] string function = null,
        [CallerFilePath] string filePath = null,
        [CallerLineNumber] int line = 0) =>
        (CreateRangeError ?? throw new InvalidOperationException("JSEngine.CreateRangeError delegate is not initialized. Ensure the BuiltIns assembly module initializer has run."))
            (message, function, filePath, line);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JSException NewReferenceError(string message,
        [CallerMemberName] string function = null,
        [CallerFilePath] string filePath = null,
        [CallerLineNumber] int line = 0) =>
        (CreateReferenceError ?? throw new InvalidOperationException("JSEngine.CreateReferenceError delegate is not initialized. Ensure the BuiltIns assembly module initializer has run."))
            (message, function, filePath, line);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JSException NewError(string message,
        [CallerMemberName] string function = null,
        [CallerFilePath] string filePath = null,
        [CallerLineNumber] int line = 0) =>
        (CreateError ?? throw new InvalidOperationException("JSEngine.CreateError delegate is not initialized. Ensure the BuiltIns assembly module initializer has run."))
            (message, function, filePath, line);

    // ── Promise factory delegates ───────────────────────────────────

    internal static Func<JSValue, bool, JSValue> CreateResolvedOrRejectedPromise;
    internal static Func<JSPromiseDelegate, IJSPromise> CreatePromiseFromDelegate;

    // ── Function class factory ──────────────────────────────────────

    /// <summary>
    /// Factory delegate for creating the Function class.
    /// Wired by the BuiltIns assembly via <c>[ModuleInitializer]</c>.
    /// </summary>
    internal static Func<JSObject, bool, JSValue> CreateFunctionClass;

    /// <summary>
    /// Factory delegate for creating the Object class.
    /// Wired by Core's module initializer from the source-generated code.
    /// </summary>
    internal static Func<JSObject, bool, JSValue> CreateObjectClass;

    // ── new.target helpers ──────────────────────────────────────────

    /// <summary>
    /// Delegate that retrieves the current <c>new.target</c> value from
    /// the execution context. Wired by the Engine assembly's module initializer.
    /// </summary>
    internal static Func<IJSContext, JSValue> GetNewTargetFromTop;

    /// <summary>
    /// Delegate that retrieves the current <c>new.target</c>'s prototype from
    /// the execution context. Wired by the Engine assembly's module initializer.
    /// </summary>
    internal static Func<IJSContext, JSObject> GetNewTargetPrototypeFromTop;

    public static JSValue NewTarget => GetNewTargetFromTop?.Invoke(Current);

    public static JSObject NewTargetPrototype => GetNewTargetPrototypeFromTop?.Invoke(Current);

    // Strictness of the code currently executing on this flow.
    //
    // This must remain an AsyncLocal rather than a [ThreadStatic]: an async function body
    // suspends at `await` and resumes on whatever thread the microtask queue pumps it from,
    // and any property [[Set]] after that await still has to observe the awaiting function's
    // strictness. ExecutionContext capture is what carries it across the suspension.
    //
    // An AsyncLocal SET is expensive though — it boxes, copies the value map, and allocates a
    // fresh ExecutionContext — and InvokeFunction enters a scope around every single call, so
    // a naive save/restore paid for two of those per JS call. Reads are cheap, so the scope
    // below only writes on an actual strict/sloppy TRANSITION, which real code performs rarely
    // (calls within one strictness level are the norm). See docs/performance-roadmap.md P0-2.
    private static readonly AsyncLocal<bool> _strictMode = new();

    internal static bool IsStrictMode => _strictMode.Value;

    internal static StrictModeScope EnterStrictMode(bool enabled) => new(enabled);

    internal static IDisposable EnterStrictModeDisposable(bool enabled) => EnterStrictMode(enabled);

    internal readonly struct StrictModeScope : IDisposable
    {
        private readonly bool previous;
        private readonly bool changed;

        public StrictModeScope(bool enabled)
        {
            // Strictness reflects the CURRENTLY executing code, not "any strict frame on
            // the stack". A strict function (or class constructor) entered from sloppy
            // code becomes strict; a sloppy function entered from strict code must drop
            // back to non-strict so its property [[Set]] semantics stay lenient. Modelling
            // this as a save/restore of an absolute state (rather than a one-way counter
            // that a sloppy callee leaves untouched) keeps a sloppy callee invoked from a
            // strict caller from inheriting the caller's strict-mode set semantics.
            //
            // This was previously a depth counter read as `depth > 0`, with the scope
            // assigning `enabled ? previous + 1 : 0`. That is exactly a boolean assignment:
            // the observable value after entry is `enabled` for any prior depth, and after
            // Dispose it is the saved one. Storing the boolean directly is therefore
            // behaviour-preserving, and it is what makes the no-transition case a no-op —
            // with a counter, strict-calls-strict still moved 1 -> 2 and had to write.
            previous = _strictMode.Value;
            changed = previous != enabled;
            if (changed)
                _strictMode.Value = enabled;
        }

        public void Dispose()
        {
            if (changed)
                _strictMode.Value = previous;
        }
    }

    // ── Stack trace helpers ─────────────────────────────────────────

    /// <summary>
    /// Delegate that walks the current call stack and appends trace entries.
    /// Wired by the Engine assembly's module initializer.
    /// </summary>
    internal static Action<StringBuilder, List<(StringSpan target, string file, int line, int column)>> AppendStackTrace;

    // ── Assembly loading ────────────────────────────────────────────

    /// <summary>
    /// Attempts to load satellite assemblies and run their module
    /// constructors so that <c>[ModuleInitializer]</c> methods register
    /// factory delegates, CLR interop, module helpers, and additional
    /// built-in type registrations.
    /// </summary>
    private static readonly object CompatibilityAssemblyLoadLock = new();
    private static bool compatibilityAssembliesLoaded;

    internal static void EnsureBuiltInsAssemblyLoaded()
    {
        lock (CompatibilityAssemblyLoadLock)
        {
            if (compatibilityAssembliesLoaded)
                return;

            TryLoadAssembly("Broiler.JavaScript.Engine");
            TryLoadAssembly("Broiler.JavaScript.BuiltIns");
            TryLoadAssembly("Broiler.JavaScript.Clr");
            TryLoadAssembly("Broiler.JavaScript.Globals");
            TryLoadAssembly("Broiler.JavaScript.Extensions");
            TryLoadAssembly("Broiler.JavaScript.Modules");
            compatibilityAssembliesLoaded = true;
        }
    }

    private static void TryLoadAssembly(string name)
    {
        StartupOptimizationDiagnostics.RecordCompatibilityAssemblyProbe();
        try
        {
            var assembly = Assembly.Load(name);
            RuntimeHelpers.RunModuleConstructor(assembly.ManifestModule.ModuleHandle);
        }
        catch (Exception ex) when (
            ex is System.IO.FileNotFoundException
            or System.IO.FileLoadException
            or BadImageFormatException)
        {
            // Assembly is not available – silently skip.
        }
    }
}
