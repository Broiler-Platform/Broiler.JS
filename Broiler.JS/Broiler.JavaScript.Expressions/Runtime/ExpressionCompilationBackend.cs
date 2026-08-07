#nullable enable
using System;
using System.Collections.Generic;
using Broiler.JavaScript.ExpressionCompiler.Expressions;

namespace Broiler.JavaScript.ExpressionCompiler.Runtime;

/// <summary>
/// Which back end compiled, or should compile, an expression tree.
/// </summary>
/// <remarks>
/// This participates in the runtime's code-cache key (see <c>JSCompilationOptions</c>), so it is
/// deliberately an enum with room to grow rather than a boolean: a bytecode back end is a third
/// value, not a special case.
/// </remarks>
public enum ExpressionCompilationBackend
{
    DynamicMethod,
    CollectibleAssembly
}

public sealed class ExpressionCompilationOptions
{
    public static ExpressionCompilationOptions Default { get; } = new();

    public ExpressionCompilationBackend Backend { get; init; } = ExpressionCompilationBackend.DynamicMethod;

    public bool CaptureDiagnostics { get; init; }

    public bool EnableJavaScriptTailCalls { get; init; }
}

public sealed class ExpressionCompilationResult<T>
{
    public ExpressionCompilationResult(
        T value,
        ExpressionCompilationBackend backend,
        string? il = null,
        string? expression = null)
    {
        Value = value;
        Backend = backend;
        IL = il ?? string.Empty;
        Expression = expression ?? string.Empty;
    }

    public T Value { get; }

    public ExpressionCompilationBackend Backend { get; }

    public string IL { get; }

    public string Expression { get; }

    public bool HasDiagnostics => IL.Length != 0 || Expression.Length != 0;
}

public interface IExpressionCompilationBackend
{
    ExpressionCompilationBackend Backend { get; }

    ExpressionCompilationResult<T> Compile<T>(BExpression<T> expression, ExpressionCompilationOptions options);
}

/// <summary>
/// The back ends known to this process, keyed by <see cref="ExpressionCompilationBackend"/>.
/// </summary>
/// <remarks>
/// This used to be a hard <c>switch</c> over the two IL implementations. Those implementations
/// now live in the emitter assembly, which this one must not know about, so they
/// <see cref="Register"/> themselves instead — explicitly, from a module initializer in the
/// assembly that owns them. Deliberately not reflection or assembly-name probing: that is what
/// defeats trimming and Native AOT, and removing the probing already in the engine is a separate
/// roadmap item. A back end that is not registered is one whose assembly was not linked in, and
/// saying so is more useful than silently falling back.
/// </remarks>
public static class ExpressionCompilationBackends
{
    private static readonly Dictionary<ExpressionCompilationBackend, IExpressionCompilationBackend> Registered = new();

    public static void Register(IExpressionCompilationBackend backend)
    {
        ArgumentNullException.ThrowIfNull(backend);

        lock (Registered)
        {
            Registered[backend.Backend] = backend;
        }
    }

    public static IExpressionCompilationBackend Get(ExpressionCompilationBackend backend)
    {
        lock (Registered)
        {
            if (Registered.TryGetValue(backend, out var registered))
            {
                return registered;
            }
        }

        if (!Enum.IsDefined(backend))
        {
            throw new ArgumentOutOfRangeException(nameof(backend), backend, null);
        }

        throw new InvalidOperationException(
            $"No compilation back end is registered for '{backend}'. The assembly implementing it " +
            "is not part of this configuration.");
    }
}
