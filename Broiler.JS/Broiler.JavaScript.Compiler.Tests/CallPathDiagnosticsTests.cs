using System;
using Broiler.JavaScript.BuiltIns;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Compiler.Tests;

// The counter item 4-4's premise rests on (docs/performance-roadmap.md item 4-4).
//
// 4-4's conclusion — that inlining's ceiling on the Octane corpus is 1.89% — is arithmetic over the
// numbers this counter produces: how many invocations there are, how many have a callee with a
// JavaScript body, and how many of those are made from a function the tiering controller has
// promoted. A measurement is only evidence if the thing producing it is checked, and these cases
// earned that twice over: an earlier draft counted a native builtin as inlinable and merged the two
// call entries, which between them overstated the surface by 72%. Each claim the emitter makes has
// a case here:
//
//   * it counts at the call rather than at an instrumented site, which is why it does not reuse
//     item 4-1's figure;
//   * a call to a NATIVE builtin is not part of the surface — it has an emitted call site and no
//     body to inline, so counting it would put every `Math.floor` into 4-4's ceiling;
//   * a builtin running a JavaScript callback takes a different, much shorter entry
//     (`InvokeCallback`), so the two are counted apart rather than averaged;
//   * "from a promoted function" means the CALLER, not the callee;
//   * it is off by default, because it is an interlocked increment on the hottest path.
[Collection(Phase3DiagnosticsCollection.Name)]
public sealed class CallPathDiagnosticsTests
{
    private static JSContext Untiered()
        => JavaScriptBootstrap.CreateContextBuilder()
            .UseBuiltInRegistry(DefaultBuiltInRegistry.Instance)
            .Build();

    private static JSContext Tiered(int threshold = 2)
        => JavaScriptBootstrap.CreateContextBuilder()
            .UseBuiltInRegistry(DefaultBuiltInRegistry.Instance)
            .UseFunctionTiering(new FunctionTieringOptions
            {
                Enabled = true,
                InvocationThreshold = threshold,
                MaxRecompilations = 64,
                MaxRetainedCodeBytes = 8L * 1024 * 1024,
            })
            .Build();

    /// <summary>Runs <paramref name="source"/> with counting on and returns the totals.</summary>
    private static (string Answer, CallPathSnapshot Calls) Count(Func<JSContext> factory, string source)
    {
        var previously = CallPathDiagnostics.Enabled;
        try
        {
            CallPathDiagnostics.Reset();
            CallPathDiagnostics.Enabled = true;
            using var context = factory();
            var answer = context.Eval(source).ToString();
            return (answer, CallPathDiagnostics.Snapshot());
        }
        finally
        {
            CallPathDiagnostics.Enabled = previously;
            CallPathDiagnostics.Reset();
        }
    }

    [Fact]
    public void NothingIsCountedWhileDisabled()
    {
        CallPathDiagnostics.Reset();
        Assert.False(CallPathDiagnostics.Enabled);

        using var context = Untiered();
        Assert.Equal("10", context.Eval("function f(x) { return x; } var s = 0; for (var i = 0; i < 10; i++) s = f(i) + 1; s;").ToString());
        Assert.Equal(0, CallPathDiagnostics.Snapshot().Calls);
    }

    // A known number of calls, counted exactly. Deliberately not "at least": the emitter divides by
    // this, so an over-count would deflate every share it reports.
    [Fact]
    public void EveryCallIsCountedExactlyOnce()
    {
        var (answer, calls) = Count(Untiered, """
            function f(x) { return x + 1; }
            var s = 0;
            for (var i = 0; i < 10; i++) s = s + f(i);
            s;
            """);

        Assert.Equal("55", answer);
        Assert.Equal(10, calls.Calls);
    }

    // A builtin does NOT run its callback through the emitted-call entry — it uses
    // JSFunction.InvokeCallback, which takes one `using` scope where the other takes five and
    // skips the executing-function and legacy-caller bookkeeping entirely. The two are counted
    // apart because they are not the same operation, and this is what says so: four callback
    // invocations, none of them on the emitted-call path, plus the two native calls (`forEach`
    // and `join`) that reach it from the script.
    [Fact]
    public void ABuiltinRunsItsCallbackOnTheOtherEntry()
    {
        var (answer, calls) = Count(Untiered, """
            var a = [1, 2, 3, 4];
            var out = [];
            a.forEach(function (x) { out.push(x + 1); });
            out.join(',');
            """);

        Assert.Equal("2,3,4,5", answer);
        Assert.Equal(4, calls.CallbackCalls);
        // `push` is native and reached from the callback, so it lands on the emitted-call entry.
        Assert.Equal(calls.Calls - calls.CallbackCalls, calls.Calls - 4);
        // None of it is inlinable: a native callee has no body, and a callback has no call site.
        Assert.Equal(0, calls.UserCalls);
    }

    // A call to a native builtin has an emitted call site and no body to inline, so it must not be
    // counted into item 4-4's surface. Without this the surface would include every `Math.floor`.
    [Fact]
    public void ACallToANativeBuiltinIsNotAUserCall()
    {
        var (answer, calls) = Count(Untiered, """
            var s = 0;
            for (var i = 0; i < 5; i++) s = s + Math.floor(i + 0.5);
            s;
            """);

        Assert.Equal("10", answer);
        Assert.Equal(5, calls.Calls);
        Assert.Equal(0, calls.UserCalls);
    }

    [Fact]
    public void WithoutTieringNoCallIsAttributedToAPromotedCaller()
    {
        var (_, calls) = Count(Untiered, """
            function callee(x) { return x + 1; }
            function caller(n) { var t = 0; for (var i = 0; i < n; i++) t = t + callee(i); return t; }
            caller(4); caller(4); caller(4); caller(4);
            """);

        Assert.Equal(20, calls.UserCalls);
        Assert.Equal(0, calls.UserCallsFromPromoted);
    }

    // The attribution is to the CALLER. `caller` is promoted after two invocations, so the calls it
    // makes to `callee` from then on are attributable and the calls to `caller` itself — made from
    // top-level script, which is not a promoted function — are not.
    [Fact]
    public void CallsAreAttributedToThePromotedCallerNotThePromotedCallee()
    {
        var (answer, calls) = Count(() => Tiered(), """
            function callee(x) { return x + 1; }
            function caller(n) { var t = 0; for (var i = 0; i < n; i++) t = t + callee(i); return t; }
            [caller(4), caller(4), caller(4), caller(4)].join('|');
            """);

        Assert.Equal("10|10|10|10", answer);
        Assert.True(calls.UserCallsFromPromoted > 0,
            "expected the promoted caller's calls to be attributed");
        // Four calls to `caller` plus four to `callee` per invocation: 20 JavaScript calls, and the
        // four top-level calls to `caller` can never be attributed, because top-level script is not
        // a function the controller promotes.
        Assert.Equal(20, calls.UserCalls);
        Assert.True(calls.UserCallsFromPromoted <= 16,
            $"top-level calls must not be attributed: {calls.UserCallsFromPromoted} of {calls.UserCalls}");
    }

    // A function that is a candidate but never gets hot attributes nothing, which is what keeps
    // "from a promoted function" from quietly meaning "from any function".
    [Fact]
    public void ACallerBelowTheThresholdAttributesNothing()
    {
        var (_, calls) = Count(() => Tiered(threshold: 1000), """
            function callee(x) { return x + 1; }
            function caller(n) { var t = 0; for (var i = 0; i < n; i++) t = t + callee(i); return t; }
            [caller(4), caller(4)].join('|');
            """);

        Assert.True(calls.UserCalls > 0);
        Assert.Equal(0, calls.UserCallsFromPromoted);
    }
}
