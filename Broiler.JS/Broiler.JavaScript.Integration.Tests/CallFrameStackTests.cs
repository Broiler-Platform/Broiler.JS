using System;
using System.Threading.Tasks;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Ast.Misc;

namespace Broiler.JavaScript.Integration.Tests;

// docs/performance-roadmap.md §7 (P3): a call's activation record lives in the context's
// CallFrameStack — a growable array of structs addressed by a FrameToken the body holds as an
// ordinary CLR local — rather than as one heap object per call.
//
// What makes any of this delicate is that a frame's lifetime is not always a contiguous span
// of the stack: a generator or async body is entered once, when primed, then leaves the stack
// at every suspension without unwinding — stranding itself and everything it had called — and
// is resumed later under a different caller entirely. Getting that wrong does not produce a
// wrong answer; it produces a corrupted stack, which surfaces as a bad trace or a hang.
//
// The tests come in two layers, because the first is not enough on its own. The JavaScript
// ones exercise the machinery end to end and would catch a gross regression, but the states
// that break the stack's invariants need a job-queue interleaving the test host does not
// reproduce. So the invariants are also asserted directly against the frame API.
public class CallFrameStackTests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // ── the trace is still the real trace after slots have been reused ──

    [Fact]
    public void StackTraceNamesEveryFrameAfterManySlotReuses()
        => Assert.Equal("true", Eval(@"
            function inner() { return new Error('x').stack; }
            function middle() { return inner(); }
            function outer() { return middle(); }
            for (var i = 0; i < 5000; i++) outer();
            var s = outer();
            String(s.indexOf('inner') >= 0 && s.indexOf('middle') >= 0 && s.indexOf('outer') >= 0);"));

    [Fact]
    public void StackTraceDepthMatchesRecursionDepthAfterSlotReuse()
        => Assert.Equal("true", Eval(@"
            function d(n) { return n === 0 ? new Error('x').stack : d(n - 1); }
            for (var i = 0; i < 200; i++) d(20);
            var frames = d(20).split('\n').length;
            var again  = d(20).split('\n').length;
            String(frames === again && frames > 20);"));

    // ── a suspended body strands frames; the trace must not loop or grow ──

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
                // A frame left behind by the suspended generator would splice this call in
                // below it, and the depth would drift upward run over run.
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

    // Cut down from test262 built-ins/AsyncGeneratorPrototype/throw/this-val-not-async-generator,
    // which is the densest exercise of the awkward case in the tree: async-generator rejections
    // settle from inside the job queue, and Promise.all's iteration then builds a native iterator
    // while the engine is positioned on a frame the generator machinery has already left. The
    // timeout turns a corrupted stack into a failure rather than a stuck suite.
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

    // ── the stack's own invariants, asserted directly ──
    //
    // The JavaScript shapes above exercise the machinery but do not pin these: the states that
    // break them are reachable only through an interleaving of the job queue that the test host
    // does not reproduce. So they are driven through the frame API instead.

    // A suspendable body loses its slot at every suspension and takes a fresh one when it is
    // resumed, under whatever caller resumed it. Its identity — and therefore its trace entry
    // and its new.target — has to survive that, which is the whole reason it holds a heap frame
    // rather than living in the array like everything else.
    [Fact]
    public void ASuspendableFrameSurvivesLosingItsSlotAndRetakesOne()
    {
        using var ctx = new JSContext();
        var frames = ((IJSExecutionContext)ctx).Frames;
        var baseDepth = frames.Depth;

        var body = CallFrames.EnterSuspendableScope(ctx, "gen.js", new StringSpan("g"), 7, 3);
        CallFrames.Step(ctx, body, 11, 5);
        frames.Describe(frames.Depth - 1, out var name, out _, out var line, out _);
        Assert.Equal("g", name.Value);
        Assert.Equal(11, line);

        // The suspension: everything the body occupied goes away without it being popped.
        frames.RestoreDepth(baseDepth);
        Assert.Equal(baseDepth, frames.Depth);

        // Resumed under a different caller than the one it was primed under.
        var resumer = CallFrames.EnterScope(ctx, "other.js", new StringSpan("resumer"), 1, 1);
        CallFrames.Step(ctx, body, 12, 9);

        frames.Describe(frames.Depth - 1, out name, out _, out line, out _);
        Assert.Equal("g", name.Value);
        Assert.Equal(12, line);

        CallFrames.Pop(ctx, body);
        CallFrames.Pop(ctx, resumer);
        Assert.Equal(baseDepth, frames.Depth);
    }

    // Unwinding to a remembered depth is how a generator's caller gets its stack back, and it
    // must only ever shrink. Growing back into slots that were abandoned would resurrect frames
    // whose contents are gone — the array-shaped version of handing a dead frame to a new owner.
    [Fact]
    public void RestoringADepthOnlyEverUnwinds()
    {
        using var ctx = new JSContext();
        var frames = ((IJSExecutionContext)ctx).Frames;

        var outer = CallFrames.EnterScope(ctx, "a.js", new StringSpan("a"), 1, 1);
        var deep = frames.Depth;
        var inner = CallFrames.EnterScope(ctx, "b.js", new StringSpan("b"), 2, 2);
        Assert.Equal(deep + 1, frames.Depth);

        frames.RestoreDepth(deep);
        Assert.Equal(deep, frames.Depth);

        frames.RestoreDepth(deep + 5);          // stale, deeper: must not take effect
        Assert.Equal(deep, frames.Depth);

        frames.Describe(deep - 1, out var name, out _, out _, out _);
        Assert.Equal("a", name.Value);          // and the surviving frame is still itself

        CallFrames.Pop(ctx, outer);
    }

    // Popping names the frame to unwind to, not "one frame", so a call whose callees were left
    // behind — which is what a suspension inside it does — still restores its caller's depth
    // exactly rather than leaking the abandoned slots.
    [Fact]
    public void PoppingUnwindsToTheNamedFrameEvenWithStrandedCallees()
    {
        using var ctx = new JSContext();
        var frames = ((IJSExecutionContext)ctx).Frames;
        var baseDepth = frames.Depth;

        var outer = CallFrames.EnterScope(ctx, "a.js", new StringSpan("a"), 1, 1);
        CallFrames.EnterScope(ctx, "b.js", new StringSpan("b"), 2, 2);
        CallFrames.EnterScope(ctx, "c.js", new StringSpan("c"), 3, 3);
        Assert.Equal(baseDepth + 3, frames.Depth);

        CallFrames.Pop(ctx, outer);             // b and c were never popped
        Assert.Equal(baseDepth, frames.Depth);
    }

    // ── new.target must not survive into a reused frame slot ──

    [Fact]
    public void NewTargetIsNotInheritedByAReusedFrameSlot()
        => Assert.Equal("true", Eval(@"
            function C() { this.nt = new.target !== undefined; }
            function plain() { return new.target !== undefined; }
            var ok = true;
            for (var i = 0; i < 5000; i++) {
                // Alternating construct and plain call reuses the same slot; a slot that kept
                // the previous call's new.target would report true here.
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
