#nullable enable
using System;
using Broiler.JavaScript.ExpressionCompiler.Expressions;

namespace Broiler.JavaScript.ExpressionCompiler.Runtime;

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

internal static class ExpressionCompilationBackends
{
    public static IExpressionCompilationBackend Get(ExpressionCompilationBackend backend) => backend switch
    {
        ExpressionCompilationBackend.DynamicMethod => DynamicMethodExpressionCompilationBackend.Instance,
        ExpressionCompilationBackend.CollectibleAssembly => CollectibleAssemblyExpressionCompilationBackend.Instance,
        _ => throw new ArgumentOutOfRangeException(nameof(backend), backend, null)
    };
}

internal sealed class DynamicMethodExpressionCompilationBackend : IExpressionCompilationBackend
{
    public static readonly DynamicMethodExpressionCompilationBackend Instance = new();

    private DynamicMethodExpressionCompilationBackend()
    {
    }

    public ExpressionCompilationBackend Backend => ExpressionCompilationBackend.DynamicMethod;

    public ExpressionCompilationResult<T> Compile<T>(BExpression<T> expression, ExpressionCompilationOptions options)
    {
        var repository = new MethodRepository();
        var outerLambda = BExpression.InstanceLambda<Func<T>>(
            expression.Name + "_outer",
            expression,
            BExpression.Parameter(typeof(Closures)),
            []) as BLambdaExpression;

        LambdaRewriter.Rewrite(outerLambda);
        var runtimeMethodBuilder = new RuntimeMethodBuilder(repository, options.CaptureDiagnostics);

        var (outer, il, exp) = outerLambda.CompileToBoundDynamicMethod(
            typeof(Closures),
            runtimeMethodBuilder,
            options.CaptureDiagnostics);

        repository.IL = il;
        repository.Exp = exp;

        var root = new Closures(repository, null, il, exp);
        var func = outer.CreateDelegate(typeof(Func<T>), root) as Func<T>;

        return new ExpressionCompilationResult<T>(func(), Backend, il, exp);
    }
}

internal sealed class CollectibleAssemblyExpressionCompilationBackend : IExpressionCompilationBackend
{
    public static readonly CollectibleAssemblyExpressionCompilationBackend Instance = new();

    private CollectibleAssemblyExpressionCompilationBackend()
    {
    }

    public ExpressionCompilationBackend Backend => ExpressionCompilationBackend.CollectibleAssembly;

    public ExpressionCompilationResult<T> Compile<T>(BExpression<T> expression, ExpressionCompilationOptions options)
    {
        var value = expression.CompileInAssembly();
        return new ExpressionCompilationResult<T>(value, Backend);
    }
}
