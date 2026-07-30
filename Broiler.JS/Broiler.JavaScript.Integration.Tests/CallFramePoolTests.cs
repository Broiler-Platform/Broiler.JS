using System;
using System.Threading.Tasks;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Ast.Misc;

namespace Broiler.JavaScript.Integration.Tests;

// docs/performance-roadmap.md §7 (P3): a call's activation record (CallStackItem) is rented
// from a free list instead of being allocated, which removed the entire fixed 80-byte cost of
// an argument-less call.
//
// Recycling is only sound while a frame's lifetime is a synchronous span, and in this engine
// it often is not: a generator or async body strands itself — and everything it had called —
// off the stack at every suspension without running their finallys, and JSGenerator.MoveNext
// restores a `Top` it captured on entry, which the body it just ran may have popped. Each of
// those produced a real defect, and every one showed up as a corrupted or looping Parent chain
// rather than as a wrong answer — two of them only as an intermittent hang.
//
// The tests come in two layers, because the first layer alone is not enough. The JavaScript
// ones below exercise the machinery end to end and would catch a gross regression, but neither
// rule's removal makes them fail: the corruptions need a job-queue interleaving the test host
// does not reproduce (the script host fails outright on the same code). So the two rules are
// also asserted directly against the frame API, and those assertions do fail when their rule
// is removed.
public class CallFramePoolTests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // ── the chain is still the real chain after frames have been recycled ──

    [Fact]
    public void StackTraceNamesEveryFrameAfterManyRecycles()
        => Assert.Equal("true", Eval(@"
            function inner() { return new Error('x').stack; }
            function middle() { return inner(); }
            function outer() { return middle(); }
            for (var i = 0; i < 5000; i++) outer();
            var s = outer();
            String(s.indexOf('inner') >= 0 && s.indexOf('middle') >= 0 && s.indexOf('outer') >= 0);"));

    [Fact]
    public void StackTraceDepthMatchesRecursionDepthAfterRecycles()
        => Assert.Equal("true", Eval(@"
            function d(n) { return n === 0 ? new Error('x').stack : d(n - 1); }
            for (var i = 0; i < 200; i++) d(20);
            var frames = d(20).split('\n').length;
            var again  = d(20).split('\n').length;
            String(frames === again && frames > 20);"));

    // ── a suspended body strands frames; the chain must not loop or grow ──
    //
    // Before the "a released frame is never anybody's parent" rule, a frame that was still the
    // top after being popped could be reissued and link itself under itself, and every walker
    // spun forever. A hang is what this asserts against; xUnit's own timeout is the backstop.

    [Fact]
    public void StackTraceInsideResumedGeneratorTerminates()
        => Assert.Equal("true", Eval(@"
            function* g() { for (var i = 0; i < 3; i++) yield new Error('x').stack; }
            var ok = true;
            for (var r = 0; r < 500; r++) {
                var it = g();
                var a = it.next().value, b = it.next().value, c = it.next().value;
                if (!a || !b || !c) { ok = false; break; }
                filler();
            }
            function filler() { function q(){ return 1; } for (var i = 0; i < 20; i++) q(); }
            String(ok);"));

    [Fact]
    public void GeneratorInterleavedWithOrdinaryCallsKeepsStackFinite()
        => Assert.Equal("true", Eval(@"
            function* g() { yield 1; yield 2; yield 3; }
            function work() { function q(){ return new Error('e').stack.split('\n').length; } return q(); }
            var first = 0, ok = true;
            for (var r = 0; r < 500; r++) {
                var it = g();
                it.next();
                var n = work();
                if (first === 0) first = n;
                // A stale parent link would splice this call into the suspended generator's
                // chain and the depth would drift upward run over run.
                if (n !== first) { ok = false; break; }
                it.next(); it.next();
            }
            String(ok);"));

    [Fact]
    public async Task StackTraceInsideResumedAsyncBodyTerminates()
    {
        using var ctx = new JSContext();
        var result = await ctx.EvalWithTopLevelAwaitAsync(@"
            async function inner() { return new Error('x').stack; }
            async function outer() {
                var acc = 0;
                for (var i = 0; i < 200; i++) { var s = await inner(); acc += s.length > 0 ? 1 : 0; }
                return acc;
            }
            await outer();");
        Assert.Equal(200d, result.DoubleValue);
    }

    // The shape that actually caught the "a released frame is never anybody's parent" rule, cut
    // down from test262 built-ins/AsyncGeneratorPrototype/throw/this-val-not-async-generator.
    // Rejecting an async-generator method on a bad receiver settles a promise from inside the
    // job queue, and Promise.all's iteration then builds a native iterator while the engine is
    // sitting on a frame the generator machinery had already popped — reissue that frame and the
    // next call links itself under itself. Everything downstream that walks the chain then spins.
    //
    // Without the rule this fails every run; the loop is what makes it deterministic, and the
    // timeout is what turns the hang into a failure rather than a stuck suite.
    [Fact(Timeout = 120000)]
    public void AsyncGeneratorRejectionsUnderPromiseAllDoNotCorruptTheFrameChain()
    {
        using var ctx = new JSContext();
        ctx.Execute(@"
            async function* g() {}
            var AGP = Object.getPrototypeOf(g).prototype;
            function* syncGenerator() {}
            var syncIterator = syncGenerator();
            var rounds = 0;

            function expectTypeError(p) {
                return p.then(
                    function () { throw new Error('expected rejection'); },
                    function (e) { if (!(e instanceof TypeError)) throw new Error('expected TypeError, got ' + e); });
            }

            function round() {
                return Promise.all([
                    expectTypeError(AGP.throw.call({})),
                    expectTypeError(AGP.throw.call(function () {})),
                    expectTypeError(AGP.throw.call(g)),
                    expectTypeError(AGP.throw.call(g.prototype)),
                    expectTypeError(AGP.throw.call(syncIterator))
                ]).then(function () {}).then(function () { rounds++; });
            }

            var chain = Promise.resolve();
            for (var i = 0; i < 40; i++) chain = chain.then(round);
            chain.then(function () { globalThis.completedRounds = rounds; },
                       function (e) { globalThis.completedRounds = 'rejected: ' + e; });");

        Assert.Equal("40", ctx.Eval("String(globalThis.completedRounds)").ToString());
    }

    // ── the two invariants, asserted directly ──
    //
    // The end-to-end shapes above exercise the machinery but do not reliably reproduce either
    // corruption: both depend on an interleaving of the job queue that the test host does not
    // reproduce (removing either rule leaves every JavaScript-level test here green, while the
    // script host fails outright). So the rules are pinned at the level they are stated, by
    // driving the frame API into the two states the engine can actually reach.

    // JSGenerator.MoveNext restores the top it captured on entry, which the body it just ran
    // may have popped — so `Top` can legitimately be a frame that has already returned. If that
    // corpse is reissued while it is still the top, the frame taking it links itself under
    // itself, and every walker spins on the one-element cycle.
    [Fact]
    public void ARecycledFrameIsNeverItsOwnCaller()
    {
        using var ctx = new JSContext();
        var ec = (IJSExecutionContext)ctx;
        var saved = ec.Top;
        try
        {
            var dead = CallStackItem.EnterScope(ctx, "a.js", new StringSpan("a"), 1, 1);
            dead.Pop(ctx);          // released, and back on the free list
            ec.Top = dead;          // ...but still the top, as the generator restore can leave it

            var next = CallStackItem.EnterScope(ctx, "b.js", new StringSpan("b"), 2, 2);
            Assert.Same(dead, next);                 // precondition: the corpse was reissued
            Assert.NotSame(next, next.Caller);       // ...and did not become its own caller
            Assert.Null(next.Caller);

            var depth = 0;
            for (var frame = next; frame != null && depth <= 64; frame = frame.Caller)
                depth++;
            Assert.True(depth <= 64, "walking Caller did not terminate");
            next.Pop(ctx);
        }
        finally
        {
            ec.Top = saved;
        }
    }

    // A generator that suspends strands frames whose parent later returns and is reissued. The
    // stale link must read as "no caller" rather than resolving to whatever owns that object
    // now — otherwise a trace walks out of the suspended body into an unrelated live chain.
    [Fact]
    public void AStrandedFrameStopsNamingItsParentOnceThatFrameIsReissued()
    {
        using var ctx = new JSContext();
        var ec = (IJSExecutionContext)ctx;
        var saved = ec.Top;
        try
        {
            var caller = CallStackItem.EnterScope(ctx, "a.js", new StringSpan("a"), 1, 1);
            var stranded = CallStackItem.EnterScope(ctx, "b.js", new StringSpan("b"), 2, 2);
            Assert.Same(caller, stranded.Caller);    // live link

            // The caller returns while `stranded` is still off-stack holding a link to it —
            // what a suspension leaves behind. `stranded` is deliberately never popped.
            caller.Pop(ctx);
            var reissued = CallStackItem.EnterScope(ctx, "c.js", new StringSpan("c"), 3, 3);
            Assert.Same(caller, reissued);           // precondition: the same object came back

            Assert.Null(stranded.Caller);            // the stale link is no longer followed
            Assert.Same(caller, stranded.Parent);    // ...though the raw field still points at it
            reissued.Pop(ctx);
        }
        finally
        {
            ec.Top = saved;
        }
    }

    // ── new.target must not survive into a recycled frame ──

    [Fact]
    public void NewTargetIsNotInheritedByARecycledFrame()
        => Assert.Equal("true", Eval(@"
            function C() { this.nt = new.target !== undefined; }
            function plain() { return new.target !== undefined; }
            var ok = true;
            for (var i = 0; i < 5000; i++) {
                // Alternating construct and plain call reuses the same frame objects; a frame
                // that kept the previous call's new.target would report true here.
                if (!(new C()).nt) { ok = false; break; }
                if (plain()) { ok = false; break; }
            }
            String(ok);"));

    [Fact]
    public void NewTargetStaysUndefinedInCallsMadeFromAConstructor()
        => Assert.Equal("true", Eval(@"
            function helper() { return new.target === undefined; }
            function C() { this.ok = helper(); }
            var ok = true;
            for (var i = 0; i < 5000; i++) if (!(new C()).ok) { ok = false; break; }
            String(ok);"));

    // ── direct eval bindings are per-frame and must not leak between reuses ──

    [Fact]
    public void DirectEvalBindingsDoNotLeakIntoARecycledFrame()
        => Assert.Equal("true", Eval(@"
            function withEval(n) { eval('var leaked = ' + n + ';'); return leaked; }
            function withoutEval() { return typeof leaked; }
            var ok = true;
            for (var i = 0; i < 2000; i++) {
                if (withEval(i) !== i) { ok = false; break; }
                // The next call reuses the frame the eval registered its binding on.
                if (withoutEval() !== 'undefined') { ok = false; break; }
            }
            String(ok);"));

    // ── the pool must not change what a program computes ──

    [Fact]
    public void DeepRecursionUnwindsAndRecursesAgainToTheSameResult()
        => Assert.Equal("true", Eval(@"
            function sum(n) { return n === 0 ? 0 : n + sum(n - 1); }
            var first = sum(400);
            for (var i = 0; i < 200; i++) sum(400);
            String(first === 80200 && sum(400) === first);"));

    [Fact]
    public void ThrowingUnwindReleasesFramesAndLeavesTheStackUsable()
        => Assert.Equal("true", Eval(@"
            function boom(n) { if (n === 0) throw new Error('boom'); return boom(n - 1); }
            var caught = 0;
            for (var i = 0; i < 2000; i++) { try { boom(10); } catch (e) { caught++; } }
            function after() { return new Error('x').stack.split('\n').length; }
            String(caught === 2000 && after() > 1);"));
}
