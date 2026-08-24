using Broiler.JavaScript.Ast.Misc;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Broiler.JavaScript.Runtime;


/// <summary>
/// Provides the top-level API for compiling and evaluating JavaScript source
/// code within the current execution context.
/// </summary>
/// <remarks>
/// This class lives in the Runtime assembly and uses factory delegates
/// (wired by Core's <c>[ModuleInitializer]</c>) to access Core-only types
/// such as <c>JSContext</c>, <c>DefaultJSCompiler</c>, and <c>AsyncPump</c>.
/// </remarks>
public class CoreScript
{
    // ── Factory infrastructure ──
    // Initialized by Core's ModuleInitializer so that CoreScript can operate
    // without a direct dependency on Core types.

    /// <summary>Creates the default <see cref="IJSCompiler"/> instance.</summary>
    internal static Func<IJSCompiler> CreateDefaultCompiler;

    /// <summary>Returns the default <see cref="ICodeCache"/> instance.</summary>
    internal static Func<ICodeCache> GetDefaultCodeCache;

    /// <summary>
    /// Returns the current execution context as a <see cref="JSValue"/>
    /// (for use in <see cref="Arguments"/> construction) together with
    /// its <see cref="ICodeCache"/>.
    /// </summary>
    internal static Func<(JSValue value, ICodeCache codeCache, JSCompilationOptions compilationOptions)> GetCurrentContext;

    /// <summary>
    /// Returns the <see cref="Task"/> representing pending async work
    /// for the current execution context, or <c>null</c> if none.
    /// </summary>
    internal static Func<Task> GetCurrentWaitTask;

    /// <summary>
    /// Creates a <see cref="Exception"/> representing a JavaScript
    /// <c>SyntaxError</c>.
    /// </summary>
    internal static Func<string, string, string, int, Exception> CreateSyntaxError;

    /// <summary>
    /// Runs a <see cref="Func{Task}"/> on a synchronous async pump,
    /// processing microtasks until completion.
    /// </summary>
    internal static Action<Func<Task>> RunAsyncPump;

    private static readonly AsyncLocal<int> TopLevelAwaitScopeDepth = new();

    internal static bool AllowTopLevelAwait => TopLevelAwaitScopeDepth.Value > 0;

    internal static IDisposable AllowTopLevelAwaitScope()
    {
        TopLevelAwaitScopeDepth.Value++;
        return new TopLevelAwaitScopeToken();
    }

    private sealed class TopLevelAwaitScopeToken : IDisposable
    {
        private bool disposed;

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            TopLevelAwaitScopeDepth.Value = Math.Max(0, TopLevelAwaitScopeDepth.Value - 1);
        }
    }

    // Whether the compile now running is parsing with the MODULE goal symbol. Ambient for the
    // same reason AllowTopLevelAwait is: the parser is several assemblies away from the host and
    // IJSCompiler.Compile does not carry compilation options. It is set from the resolved
    // JSCompilationOptions inside Compile's factory, so the flag the parser reads and the flag in
    // the code-cache key are the same value by construction and cannot drift apart.
    private static readonly AsyncLocal<int> ModuleGoalDepth = new();

    internal static bool IsModuleGoal => ModuleGoalDepth.Value > 0;

    internal static IDisposable ModuleGoalScope()
    {
        ModuleGoalDepth.Value++;
        return new ModuleGoalScopeToken();
    }

    private sealed class ModuleGoalScopeToken : IDisposable
    {
        private bool disposed;

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            ModuleGoalDepth.Value = Math.Max(0, ModuleGoalDepth.Value - 1);
        }
    }

    private static IJSCompiler _compiler;

    /// <summary>
    /// Gets or sets the compiler used by <see cref="Compile"/>.
    /// Defaults to the compiler provided by Core's module initializer.
    /// </summary>
    public static IJSCompiler Compiler
    {
        get => _compiler ??= CreateDefaultCompiler();
        set => _compiler = value ?? throw new ArgumentNullException(nameof(value));
    }

    public static JSFunctionDelegate Compile(
        in StringSpan code,
        string location = null,
        IList<string> args = null,
        ICodeCache codeCache = null,
        JSCompilationOptions? compilationOptions = null,
        bool isModule = false)
    {
        try
        {
            var current = GetCurrentContext?.Invoke() ?? default;
            codeCache ??= current.codeCache ?? GetDefaultCodeCache();
            var script = code;
            var compiler = Compiler;
            // Wrapped here rather than only in each ICodeCache: the factory is the one point
            // every implementation reaches, including a host's own, and it is where the front
            // end starts recursing over the source. A cache *hit* never crosses the boundary.
            // Resolved once: the options carried in the cache key and the goal the factory
            // parses under both come from this single value.
            var options = compilationOptions ?? current.compilationOptions;
            if (isModule)
                options = options with { IsModule = true };

            var moduleGoal = options.IsModule;
            var jsc = new JSCode(
                location,
                code,
                args,
                () => ExpressionCompiler.CompilationStack.Run(
                    () =>
                    {
                        // Entered around the FACTORY rather than around Compile, so it is
                        // established exactly when the front end runs: a cache hit never reaches
                        // here, and never needed to.
                        using var goal = moduleGoal ? ModuleGoalScope() : null;
                        return compiler.Compile(script, location, args, codeCache);
                    },
                    script.Length),
                options);
            return codeCache.GetOrCreate(in jsc);
        }
        catch (FastParseException ex)
        {
            throw CreateSyntaxError(ex.Message, "Compile", location, ex.Token.Start.Line);
        }
    }

    /// <summary>
    /// Evaluates JavaScript code synchronously, pumping an async message loop
    /// so that microtasks (e.g., resolved promises) are processed before
    /// returning the result.
    /// </summary>
    /// <param name="code">The JavaScript source code to evaluate.</param>
    /// <param name="location">Optional source location for diagnostics.</param>
    /// <returns>The result of evaluating <paramref name="code"/>.</returns>
    public static JSValue EvaluateWithTasks(string code, string location = null)
    {
        var result = JSValue.UndefinedValue;
        var ctx = GetCurrentContext();
        var fx = Compile(code, location, codeCache: ctx.codeCache);
        
        RunAsyncPump(() =>
        {
            result = fx(new Arguments(ctx.value));
            return Task.CompletedTask;
        });
        
        return result;
    }

    /// <summary>
    /// Evaluates JavaScript code synchronously in the current context.
    /// </summary>
    /// <param name="code">The JavaScript source code to evaluate.</param>
    /// <param name="location">Optional source location for diagnostics.</param>
    /// <param name="codeCache">Optional code cache for compiled script reuse.</param>
    /// <returns>The result of evaluating <paramref name="code"/>.</returns>
    public static JSValue Evaluate(string code, string location = null, ICodeCache codeCache = null)
    {
        var ctx = GetCurrentContext();
        var fx = Compile(code, location, null, codeCache ?? ctx.codeCache);
        // The script/eval completion value must be a materialized value, never an
        // unforced JSTailCall sentinel: a trailing call in the body is a tail call
        // only within the script, and this boundary must trampoline it.
        return JSTailCall.Resolve(fx(new Arguments(ctx.value)));
    }

    /// <summary>
    /// Evaluates JavaScript code asynchronously, awaiting any pending
    /// async operations before returning.
    /// </summary>
    /// <param name="code">The JavaScript source code to evaluate.</param>
    /// <param name="location">Optional source location for diagnostics.</param>
    /// <param name="codeCache">Optional code cache for compiled script reuse.</param>
    /// <returns>The result of evaluating <paramref name="code"/>.</returns>
    public static async Task<JSValue> EvaluateAsync(string code, string location = null, ICodeCache codeCache = null)
    {
        var ctx = GetCurrentContext();
        var fx = Compile(code, location, null, codeCache ?? ctx.codeCache);
        
        var result = fx(new Arguments(ctx.value));
        
        var waitTask = GetCurrentWaitTask();
        if (waitTask != null)
            await waitTask;
        
        return result;
    }
}
