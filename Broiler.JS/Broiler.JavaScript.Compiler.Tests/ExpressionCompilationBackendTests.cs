using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.ExpressionCompiler.Runtime;

namespace Broiler.JavaScript.Compiler.Tests;

public class ExpressionCompilationBackendTests
{
    [Theory]
    [InlineData(ExpressionCompilationBackend.DynamicMethod)]
    [InlineData(ExpressionCompilationBackend.CollectibleAssembly)]
    public void CompileWithNestedLambdas_Targets_Configured_Backend(ExpressionCompilationBackend backend)
    {
        var result = CreateAnswer().CompileWithNestedLambdas(new ExpressionCompilationOptions { Backend = backend });

        Assert.Equal(backend, result.Backend);
        Assert.Equal(42, result.Value());
        Assert.False(result.HasDiagnostics);
    }

    [Fact]
    public void DynamicMethodBackend_Returns_Diagnostics_When_Requested()
    {
        var result = CreateAnswer().CompileWithNestedLambdas(new ExpressionCompilationOptions
        {
            Backend = ExpressionCompilationBackend.DynamicMethod,
            CaptureDiagnostics = true
        });

        Assert.Equal(ExpressionCompilationBackend.DynamicMethod, result.Backend);
        Assert.Equal(42, result.Value());
        Assert.True(result.HasDiagnostics);
        Assert.Contains("ret", result.IL, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("42", result.Expression);
    }

    private static BExpression<Func<int>> CreateAnswer()
        => BExpression.Lambda<Func<int>>(new FunctionName("answer"), BExpression.Constant(42));
}
