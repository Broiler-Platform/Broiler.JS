using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Compiler.Tests;

/// <summary>
/// Fixtures for roadmap item 1-1: a nested function's IL is generated on first invocation
/// rather than while its enclosing lambda is emitted.
/// </summary>
/// <remarks>
/// <para>
/// The item names four spec-visible risks for deferring a function — early errors, scope
/// capture, direct <c>eval</c>, and generator/async bodies. Deferring <em>IL generation</em>
/// rather than parse-and-compile is supposed to make all four vacuous, because each is settled
/// by the front end and the front end still runs eagerly. "Supposed to" is the reason these
/// exist: the claim is that a deferred function behaves identically, and a claim about
/// behaviour is a test.
/// </para>
/// <para>
/// None of these toggles <c>DeferredMethodCompilation.Enabled</c>. It is a process-wide static
/// and xUnit runs classes in parallel, so a test that flipped it would change what every other
/// test in the run was measuring — the same reason item 1-2's guard-alone row is a manual
/// result. They assert behaviour under the shipped default instead.
/// </para>
/// </remarks>
public class DeferredCompilationTests
{
    [Fact(Timeout = 600000)]
    public void SyntaxErrorInNeverCalledFunction_StillThrowsAtCompileTime()
    {
        // The item's first risk. Nothing here is ever called, so if the error surfaced at
        // invocation instead of at compile time this would return normally.
        using var context = new JSContext();
        Assert.ThrowsAny<System.Exception>(
            () => context.Eval("function never() { var 1invalid = 2; } 'reached';"));
    }

    [Fact(Timeout = 600000)]
    public void DeferredFunctionCapturesItsOwnInstanceOfALoopBinding()
    {
        // Generation is memoized per syntactic site and the boxes are captured per closure
        // instance. Sharing the wrong one is the failure this pins: every closure would report
        // the last iteration's value.
        const string source = """
            var fns = [];
            for (let i = 0; i < 5; i++) fns.push(function () { return i * 10; });
            fns.map(function (f) { return f(); }).join(',');
            """;

        using var context = new JSContext();
        Assert.Equal("0,10,20,30,40", context.Eval(source).ToString());
    }

    [Fact(Timeout = 600000)]
    public void DeferredFunctionSeesLaterWritesToACapturedBinding()
    {
        // A capture is by cell, not by value, and the cell is bound at closure creation while
        // the code that reads it is generated later. Reading the value at generation time
        // instead would freeze it at whatever it was on first call.
        const string source = """
            var x = 1;
            function read() { return x; }
            var before = read();
            x = 2;
            var after = read();
            before + '/' + after;
            """;

        using var context = new JSContext();
        Assert.Equal("1/2", context.Eval(source).ToString());
    }

    [Fact(Timeout = 600000)]
    public void RecursionThroughADeferredFunctionTerminates()
    {
        // The first call generates the method and only then runs the body, which calls the same
        // function again — re-entering the thunk while the outer resolve is still on the stack.
        using var context = new JSContext();
        Assert.Equal(
            120,
            context.Eval("function fact(n) { return n <= 1 ? 1 : n * fact(n - 1); } fact(5);")
                .DoubleValue);
    }

    [Fact(Timeout = 600000)]
    public void MutuallyRecursiveDeferredFunctionsTerminate()
    {
        // Two sites, each first-called from inside the other's first call. Generation is under
        // a per-site lock, so a shared or reentrant lock would deadlock here rather than fail.
        const string source = """
            function isEven(n) { return n === 0 ? true : isOdd(n - 1); }
            function isOdd(n) { return n === 0 ? false : isEven(n - 1); }
            isEven(10) + '/' + isOdd(10);
            """;

        using var context = new JSContext();
        Assert.Equal("true/false", context.Eval(source).ToString());
    }

    [Fact(Timeout = 600000)]
    public void DirectEvalInsideADeferredFunctionSeesItsScope()
    {
        // The item's third risk. A direct eval resolves names against the enclosing activation
        // at run time, which is established by the front end and by the closure rewrite — both
        // of which still happen eagerly.
        const string source = """
            function outer(a) { var b = a + 1; return eval('a + b'); }
            outer(4);
            """;

        using var context = new JSContext();
        Assert.Equal(9, context.Eval(source).DoubleValue);
    }

    [Fact(Timeout = 600000)]
    public void DeferredGeneratorAndAsyncBodiesRun()
    {
        // The item's fourth risk. A generator body is rewritten into a state machine before it
        // ever reaches the emitter, so deferring the emitter cannot see the difference — but
        // the rewritten lambda is a different shape, which is exactly why it is worth running.
        const string source = """
            function* count() { yield 1; yield 2; yield 3; }
            var total = 0;
            for (var v of count()) total += v;
            total;
            """;

        using var context = new JSContext();
        Assert.Equal(6, context.Eval(source).DoubleValue);
    }

    [Fact(Timeout = 600000)]
    public void ManyInstancesOfOneSiteKeepDistinctCaptures()
    {
        // One site, many closure instances: generation happens once and is shared, the boxes do
        // not. A thunk that memoized the resolved delegate on the SITE rather than on the
        // instance would give every counter the first one's state and this would read all 1s.
        const string source = """
            function makeCounter(start) { return function () { return start++; }; }
            var counters = [];
            for (var i = 0; i < 100; i++) counters.push(makeCounter(i));
            var sum = 0;
            for (var i = 0; i < 100; i++) sum += counters[i]();
            sum;
            """;

        using var context = new JSContext();
        Assert.Equal(4950, context.Eval(source).DoubleValue);
    }

    [Fact(Timeout = 600000)]
    public void ConcurrentFirstCallsOfOneSiteAgree()
    {
        // Generation is guarded and resolution deliberately is not — two threads racing to
        // resolve the same instance both produce a delegate over the same generated method and
        // the same boxes, so the loser's copy is equivalent. This is what says "equivalent" is
        // true rather than assumed. Each context is its own engine, so the shared thing under
        // test is the process-wide generation path, not the contexts.
        const string source = """
            function work(n) { var s = 0; for (var i = 0; i < n; i++) s += i; return s; }
            work(1000);
            """;

        var results = new double[8];
        Parallel.For(0, results.Length, i =>
        {
            using var context = new JSContext();
            results[i] = context.Eval(source).DoubleValue;
        });

        Assert.All(results, r => Assert.Equal(499500, r));
    }

    [Fact(Timeout = 600000)]
    public void ADeferredFunctionThatIsNeverCalledDoesNotAffectTheRest()
    {
        // The shape the whole item is for: thousands of definitions, almost none invoked. It
        // asserts the answer rather than a time, because the timing claim belongs in the
        // roadmap where it can carry its control; what a fixture can pin is that skipping
        // generation for the other 999 changed nothing about the one that runs.
        var source = new StringBuilder(1_000 * 40 + 64);
        for (var i = 0; i < 1_000; i++)
            source.Append("function unused").Append(i).Append("(a){return a*").Append(i).Append(";}\n");
        source.Append("function used(a){return a+1;} used(41);");

        using var context = new JSContext();
        Assert.Equal(42, context.Eval(source.ToString()).DoubleValue);
    }
}
