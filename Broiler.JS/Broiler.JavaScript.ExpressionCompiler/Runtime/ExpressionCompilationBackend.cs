#nullable enable
using System;
using System.Runtime.CompilerServices;
using Broiler.JavaScript.ExpressionCompiler.Expressions;

namespace Broiler.JavaScript.ExpressionCompiler.Runtime;

// The IL half of what used to be one file. The contract, the options and the result stayed on
// the model side; everything here emits, and everything here is internal, so splitting the file
// is not an API change. The two implementations register themselves rather than being reached
// by a switch in the model — see ExpressionCompilationBackends.
internal static class ILExpressionCompilationBackends
{
    [ModuleInitializer]
    internal static void Register()
    {
        ExpressionCompilationBackends.Register(DynamicMethodExpressionCompilationBackend.Instance);
        ExpressionCompilationBackends.Register(CollectibleAssemblyExpressionCompilationBackend.Instance);
    }
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
        var runtimeMethodBuilder = new RuntimeMethodBuilder(
            repository,
            options.CaptureDiagnostics,
            options.EnableJavaScriptTailCalls);

        var (outer, il, exp) = outerLambda.CompileToBoundDynamicMethod(
            typeof(Closures),
            runtimeMethodBuilder,
            options.CaptureDiagnostics,
            options.EnableJavaScriptTailCalls);

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
