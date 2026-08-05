using System.Threading.Tasks;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Integration.Tests;

// One thread runs JavaScript in a context at a time. ECMAScript's agent is single-threaded and this
// engine is written throughout on that assumption, so a second thread inside a context is not a
// slow program, it is a heap modified from two threads at once.
//
// **These assert the property, not a value.** `MicrotaskSchedulingTests` asserts answers, which
// catches an overlap only when the overlap happens to change one — that is how the original defect
// survived a whole phase of full-suite runs at a 0.6% hit rate. `JSContext.ExecutionConcurrency`
// counts threads inside JavaScript directly, so a violation is caught whenever it happens rather
// than when it happens to matter.
//
// The shapes below are the ones that reach the case the job queue alone could not: a job posted
// while NOTHING is executing goes to a host context or the thread pool rather than the queue, and
// getting there needs a JavaScript entry point that is not `Eval`. A host invoking a `JSValue`
// directly is exactly that — and it is also the embedding case the contract has to name.
[Collection(nameof(ExecutionExclusionTests))]
public sealed class ExecutionExclusionTests
{
    private const string Arm = """
        globalThis.sink = 0;
        globalThis.armed = new Promise(function (resolve) { globalThis.fire = resolve; });
        globalThis.armed.then(function () { for (var i = 0; i < 200000; i++) { globalThis.sink += i; } });
        """;

    private const string Busy =
        "(function () { var t = 0; for (var j = 0; j < 20000; j++) { t += j; } return t; })()";

    [Fact]
    public void AJobDispatchedWithNothingRunningDoesNotOverlapALaterEvaluation()
    {
        // The residual the job queue could not close, and the reason the execution LOCK exists
        // beside it. The queue is taken only while an execution is already in progress — which is
        // what makes stranding a job impossible — so settling a promise from a host thread with
        // nothing running dispatches the reaction to the pool instead. Before the lock this
        // measured peak 2 and 172 overlaps in 200 rounds.
        JSContext.ResetExecutionConcurrency();

        for (var round = 0; round < 30; round++)
        {
            using var context = new JSContext();
            context.Eval(Arm);

            var fire = context["fire"];
            var settled = Task.Run(() => fire.InvokeFunction(new Arguments(JSUndefined.Value, JSUndefined.Value)));

            for (var i = 0; i < 20; i++)
                context.Eval(Busy);

            settled.Wait();
            context.Eval("1");
        }

        var (peak, overlaps) = JSContext.ExecutionConcurrency;
        Assert.Equal(0, overlaps);
        Assert.Equal(1, peak);
    }

    [Fact]
    public void TwoThreadsEvaluatingOnOneContextTakeTurns()
    {
        // The plainest violation the lock has to prevent, and one no dispatch rule addresses: two
        // host threads calling Eval on the same context. It is not a supported pattern and never
        // was, but "unsupported" used to mean silent heap corruption and now means a bounded wait.
        JSContext.ResetExecutionConcurrency();

        using var context = new JSContext();
        context.Eval("globalThis.n = 0;");

        Parallel.For(0, 4, _ =>
        {
            for (var i = 0; i < 40; i++)
                context.Eval("globalThis.n = globalThis.n + 1;" + Busy);
        });

        var (peak, overlaps) = JSContext.ExecutionConcurrency;
        Assert.Equal(0, overlaps);
        Assert.Equal(1, peak);

        // And the count is exact, which a torn read-modify-write would not be.
        Assert.Equal("160", context.Eval("String(globalThis.n)").ToString());
    }

    [Fact]
    public void AHostCallIntoJavaScriptJoinsTheExclusionThroughEnterExecution()
    {
        // The contract itself. The engine cannot guard every route into JavaScript — invoking a
        // JSValue is an ordinary call on an ordinary object, and locking it would put a mutex on
        // the hottest path in the engine — so the rule is stated and given an API instead. This
        // fixture is what says the API actually joins the same exclusion rather than merely
        // existing.
        JSContext.ResetExecutionConcurrency();

        using var context = new JSContext();
        context.Eval("globalThis.total = 0; globalThis.bump = function (n) { globalThis.total += n; return globalThis.total; };");

        var bump = context["bump"];
        var one = context.Eval("1");
        var work = Task.Run(() =>
        {
            for (var i = 0; i < 60; i++)
            {
                using (context.EnterExecution())
                    bump.InvokeFunction(new Arguments(JSUndefined.Value, one));
            }
        });

        for (var i = 0; i < 60; i++)
        {
            using (context.EnterExecution())
                bump.InvokeFunction(new Arguments(JSUndefined.Value, one));
        }

        work.Wait();

        var (peak, overlaps) = JSContext.ExecutionConcurrency;
        Assert.Equal(0, overlaps);
        Assert.Equal(1, peak);
        Assert.Equal("120", context.Eval("String(globalThis.total)").ToString());
    }

    [Fact]
    public void TheScopeIsReentrantSoNestingIsNotADeadlock()
    {
        // Required rather than convenient: a host that wraps an entry point which turns out to be
        // nested — a callback the engine already invoked from inside an evaluation — must not
        // deadlock against itself, and a conservative embedder will wrap everything.
        using var context = new JSContext();
        using (context.EnterExecution())
        {
            using (context.EnterExecution())
                Assert.Equal("3", context.Eval("1 + 2").ToString());

            Assert.Equal("7", context.Eval("3 + 4").ToString());
        }
    }

    [Fact]
    public void JobsQueuedUnderAHostScopeRunWhenTheOutermostOneIsReleased()
    {
        // The scope is an execution, so the queue is live inside it and drains on the way out —
        // which is what makes wrapping a host call equivalent to an evaluation rather than merely
        // exclusive with one.
        using var context = new JSContext();
        context.Eval("globalThis.log = ''; globalThis.go = function () { Promise.resolve().then(function () { globalThis.log += 'job'; }); };");

        var go = context["go"];
        using (context.EnterExecution())
        {
            go.InvokeFunction(new Arguments(JSUndefined.Value));

            // Still inside the scope: the job belongs to it and has not run.
            Assert.Equal("", context.Eval("globalThis.log").ToString());
        }

        Assert.Equal("job", context.Eval("globalThis.log").ToString());
    }
}
