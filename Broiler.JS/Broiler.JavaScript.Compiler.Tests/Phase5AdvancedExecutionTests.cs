using Broiler.JavaScript.BuiltIns;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Compiler.Tests;

[Collection(Phase3DiagnosticsCollection.Name)]
public sealed class Phase5AdvancedExecutionTests
{
    private static JSContext CreateTieredContext(
        int threshold = 2,
        int maxRecompilations = 8,
        long maxRetainedCodeBytes = 1024 * 1024)
        => JavaScriptBootstrap.CreateContextBuilder()
            .UseBuiltInRegistry(DefaultBuiltInRegistry.Instance)
            .UseFunctionTiering(new FunctionTieringOptions
            {
                Enabled = true,
                InvocationThreshold = threshold,
                MaxRecompilations = maxRecompilations,
                MaxRetainedCodeBytes = maxRetainedCodeBytes,
            })
            .Build();

    [Fact]
    public void CountedReductionPromotesAndMixedInputDeoptimizesToBaseline()
    {
        using var context = CreateTieredContext();
        var result = context.Eval("""
            function sum(n) {
              var total = 0;
              for (var i = 0; i < n; i++) total += i;
              return total;
            }
            [sum(10), sum(10), sum(10), sum(3.5), Object.is(sum(-0), 0), sum('10')].join('|');
            """);

        Assert.Equal("45|45|45|6|true|45", result.ToString());
        var snapshot = context.FunctionTiering.Snapshot();
        Assert.True(snapshot.Candidates >= 1);
        Assert.Equal(1, snapshot.Recompilations);
        Assert.Equal(1, snapshot.DelegateReplacements);
        Assert.Equal(1, snapshot.Deoptimizations);
        Assert.InRange(snapshot.RetainedCodeBytes, 1, 1024 * 1024);
    }

    [Fact]
    public void RecompilationCountAndRetainedCodeAreBoundedPerRealm()
    {
        using var context = CreateTieredContext(threshold: 1, maxRecompilations: 1, maxRetainedCodeBytes: 4096);
        var result = context.Eval("""
            function a(n) { return n + 1; }
            function b(n) { return n + 2; }
            function c(n) { return n + 3; }
            [a(1), b(1), c(1)].join('|');
            """);

        Assert.Equal("2|3|4", result.ToString());
        var snapshot = context.FunctionTiering.Snapshot();
        Assert.Equal(1, snapshot.RecompilationAttempts);
        Assert.Equal(1, snapshot.Recompilations);
        Assert.True(snapshot.BudgetRejections >= 2);
        Assert.InRange(snapshot.RetainedCodeBytes, 1, 4096);
    }

    [Fact]
    public void CapturingAndDynamicScopeFunctionsRemainOnBaselineTier()
    {
        using var context = CreateTieredContext(threshold: 1);
        var result = context.Eval("""
            function outer(x) {
              return function inner(n) { return n + x; };
            }
            function dynamic(n) { with ({}) { return n + 1; } }
            var inner = outer(4);
            [inner(3), inner(5), dynamic(1)].join('|');
            """);

        Assert.Equal("7|9|2", result.ToString());
        var snapshot = context.FunctionTiering.Snapshot();
        Assert.Equal(0, snapshot.Candidates);
        Assert.Equal(0, snapshot.Recompilations);
    }

    [Fact]
    public void EvaluationAttachesTieringToTheTargetRealmInsteadOfTheCurrentRealm()
    {
        using var baselineContext = JavaScriptBootstrap.CreateContextBuilder()
            .UseBuiltInRegistry(DefaultBuiltInRegistry.Instance)
            .Build();
        using var tieredContext = CreateTieredContext(threshold: 1);

        var baselineFunction = baselineContext.Eval("(function baseline(n) { return n + 1; })");
        var tieredFunction = tieredContext.Eval("(function promoted(n) { return n + 1; })");
        Assert.Equal(2, baselineFunction.InvokeFunction(new Arguments(baselineContext, JSNumber.One)).DoubleValue);
        Assert.Equal(2, tieredFunction.InvokeFunction(new Arguments(tieredContext, JSNumber.One)).DoubleValue);

        Assert.Equal(0, baselineContext.FunctionTiering.Snapshot().Candidates);
        Assert.Equal(1, tieredContext.FunctionTiering.Snapshot().Recompilations);
    }

    [Fact]
    public void ConstantAndAliasedBindingsKeepBaselineSemantics()
    {
        using var context = CreateTieredContext(threshold: 1);
        var result = context.Eval("""
            function invalid(n) {
              const total = 0;
              for (var i = 0; i < n; i++) total += i;
              return total;
            }
            function shadowed(n) {
              var n = 0;
              for (var i = 0; i < n; i++) n += i;
              return n;
            }
            var errorName;
            try { invalid(3); errorName = 'missing error'; }
            catch (error) { errorName = error.name; }
            [errorName, shadowed(3)].join('|');
            """);

        Assert.Equal("TypeError|0", result.ToString());
    }
}
