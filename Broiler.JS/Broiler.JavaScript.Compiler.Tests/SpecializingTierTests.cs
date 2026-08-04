using System;
using Broiler.JavaScript.BuiltIns;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Compiler.Tests;

// The specializing tier-2 compile (docs/performance-roadmap.md item 4-2b).
//
// A promoted function's property reads are re-emitted at their ORIGINAL inline-cache site
// indices, so item 4-1's per-site feedback becomes addressable from the recompile. A site whose
// whole history is one shape, resolving one key to one own slot, is emitted as a shape guard plus
// a direct slot load — through item 4-3b's in-method fallback, whose first JavaScript-level
// consumer this is — with the ordinary cached get as the fallback arm.
//
// Every test here runs the same source through a specializing and an untiered context and
// requires the two to agree. The untiered answer is the specification; the speculation snapshot
// is what says the specialized path was actually emitted, without which a passing test would only
// prove that nothing happened.
[Collection(Phase3DiagnosticsCollection.Name)]
public sealed class SpecializingTierTests
{
    private static JSContext Specializing(int threshold = 2)
        => JavaScriptBootstrap.CreateContextBuilder()
            .UseBuiltInRegistry(DefaultBuiltInRegistry.Instance)
            .UseFunctionTiering(new FunctionTieringOptions
            {
                Enabled = true,
                InvocationThreshold = threshold,
                MaxRecompilations = 64,
                MaxRetainedCodeBytes = 8L * 1024 * 1024,
                SpecializeFromTypeFeedback = true,
            })
            .Build();

    private static JSContext Untiered()
        => JavaScriptBootstrap.CreateContextBuilder()
            .UseBuiltInRegistry(DefaultBuiltInRegistry.Instance)
            .Build();

    private static string Run(JSContext context, string source)
    {
        try
        {
            return context.Eval(source).ToString();
        }
        catch (Exception e)
        {
            return e.GetType().Name;
        }
    }

    private readonly record struct Outcome(string Answer, int SpeculationSites, long GuardsMissed, long Recompilations);

    /// <summary>
    /// Runs <paramref name="source"/> with and without the specializing tier and asserts the
    /// answers agree, returning what the specializing run did.
    /// </summary>
    private static Outcome Compare(string source, string expected, int threshold = 2)
    {
        using var untiered = Untiered();
        var control = Run(untiered, source);
        Assert.Equal(expected, control);

        var previouslyEnabled = TypeFeedback.Enabled;
        try
        {
            TypeFeedback.Reset();
            Speculation.Reset();
            using var specializing = Specializing(threshold);
            var answer = Run(specializing, source);
            Assert.Equal(control, answer);

            var speculation = Speculation.Snapshot();
            return new Outcome(
                answer,
                speculation.Sites,
                speculation.GuardsMissed,
                specializing.FunctionTiering.Snapshot().Recompilations);
        }
        finally
        {
            TypeFeedback.Enabled = previouslyEnabled;
            TypeFeedback.Reset();
            Speculation.Reset();
        }
    }

    private const string Monomorphic = """
        function total(p) { var t = 0; for (var i = 0; i < 4; i++) t += p.x; return t; }
        var a = { x: 1, y: 2 };
        [total(a), total(a), total(a), total(a)].join('|');
        """;

    // The base case, and the one that says the machinery runs at all: four reads of one own data
    // property on one shape. The site is monomorphic, so the promoted body reads the slot behind
    // a shape guard — and never misses, because the receiver never changes shape.
    [Fact]
    public void AMonomorphicOwnPropertyReadIsSpecializedAndCorrect()
    {
        var outcome = Compare(Monomorphic, "4|4|4|4");

        Assert.Equal(1, outcome.Recompilations);
        Assert.True(outcome.SpeculationSites >= 1,
            $"expected a specialized read, got {outcome.SpeculationSites} speculation sites");
        Assert.Equal(0, outcome.GuardsMissed);
    }

    // The control for the control: with feedback off nothing is specialized, and the same source
    // still answers the same. Without this, "speculation sites >= 1" above could be measuring
    // some other part of the engine.
    [Fact]
    public void WithoutFeedbackNothingIsSpecialized()
    {
        using var untiered = Untiered();
        Assert.Equal("4|4|4|4", Run(untiered, Monomorphic));

        var previouslyEnabled = TypeFeedback.Enabled;
        try
        {
            TypeFeedback.Enabled = false;
            TypeFeedback.Reset();
            Speculation.Reset();
            using var tiered = JavaScriptBootstrap.CreateContextBuilder()
                .UseBuiltInRegistry(DefaultBuiltInRegistry.Instance)
                .UseFunctionTiering(new FunctionTieringOptions
                {
                    Enabled = true,
                    InvocationThreshold = 2,
                    MaxRecompilations = 64,
                    MaxRetainedCodeBytes = 8L * 1024 * 1024,
                    SpecializeFromTypeFeedback = false,
                })
                .Build();

            Assert.Equal("4|4|4|4", Run(tiered, Monomorphic));
            Assert.Equal(1, tiered.FunctionTiering.Snapshot().Recompilations);
            Assert.Equal(0, Speculation.Snapshot().Sites);
        }
        finally
        {
            TypeFeedback.Enabled = previouslyEnabled;
            TypeFeedback.Reset();
            Speculation.Reset();
        }
    }

    // ── the guard has to be able to fail, and failing has to be right ────────────────

    // A second shape arrives only AFTER promotion, so the feedback still says monomorphic and the
    // guard is emitted — then misses. The answer must not change.
    [Fact]
    public void AShapeTheFeedbackNeverSawFallsBackToTheCachedGet()
    {
        var outcome = Compare("""
            function read(p) { var t = p.x; return t; }
            var a = { x: 1 };
            var out = [read(a), read(a), read(a)];
            var b = { w: 0, x: 9 };
            out.push(read(b), read(b));
            out.join('|');
            """, "1|1|1|9|9");

        Assert.True(outcome.GuardsMissed >= 1,
            $"expected the guard to miss on the second shape, got {outcome.GuardsMissed}");
    }

    // Adding a property to the very receiver the feedback described moves it to a new shape id,
    // so the guard fails on an object that is otherwise the same one.
    [Fact]
    public void GrowingTheReceiverInvalidatesTheGuard()
        => Compare("""
            function read(p) { var t = p.x; return t; }
            var a = { x: 1 };
            var out = [read(a), read(a), read(a)];
            a.z = 5;
            out.push(read(a), read(a));
            out.join('|');
            """, "1|1|1|1|1");

    // Deleting a property drops the object out of shape mode entirely.
    [Fact]
    public void DeletingAPropertyDropsToTheGenericPath()
        => Compare("""
            function read(p) { var t = p.x; return t; }
            var a = { x: 1, y: 2 };
            var out = [read(a), read(a), read(a)];
            delete a.y;
            out.push(read(a), read(a));
            out.join('|');
            """, "1|1|1|1|1");

    // Redefining the property as an accessor must be observed: a slot load would answer the old
    // value, and there is no old value to answer.
    [Fact]
    public void RedefiningAsAnAccessorIsObserved()
        => Compare("""
            function read(p) { var t = p.x; return t; }
            var a = { x: 1 };
            var out = [read(a), read(a), read(a)];
            Object.defineProperty(a, 'x', { get: function () { return 42; }, configurable: true });
            out.push(read(a), read(a));
            out.join('|');
            """, "1|1|1|42|42");

    // Writing through the property between reads: the slot is the same, the value is not.
    [Fact]
    public void AWrittenSlotReadsTheNewValue()
        => Compare("""
            function read(p) { var t = p.x; return t; }
            var a = { x: 1 };
            var out = [read(a), read(a), read(a)];
            a.x = 7;
            out.push(read(a), read(a));
            out.join('|');
            """, "1|1|1|7|7");

    // ── what the specialization must decline ─────────────────────────────────────────

    // A method lives on the PROTOTYPE, so no own slot describes it and the site is never
    // specialized — the read stays on the cached get, which has the prototype guards. Kept
    // because getting this wrong would read some unrelated own slot of the receiver.
    [Fact]
    public void APrototypeResolvedReadIsNotSpecialized()
    {
        var outcome = Compare("""
            function Point(x) { this.x = x; }
            Point.prototype.scale = 3;
            function read(p) { var t = p.scale; return t; }
            var a = new Point(1);
            [read(a), read(a), read(a), read(a)].join('|');
            """, "3|3|3|3");

        Assert.Equal(0, outcome.SpeculationSites);
    }

    // An element read is not a shape read at all — the key is an array index, which no shape
    // tracks, and the feedback declines to describe it for the same reason the cache declines to
    // cache it.
    [Fact]
    public void AnIndexedReadIsNotSpecialized()
        => Compare("""
            function read(p) { var t = p[0]; return t; }
            var a = [5, 6];
            [read(a), read(a), read(a), read(a)].join('|');
            """, "5|5|5|5");

    // A site that saw two shapes BEFORE promotion is polymorphic in the feedback, so nothing is
    // emitted for it — this is the half 4-1 exists to answer, and the only one the guard cannot
    // recover on its own without paying for a speculation that was never going to hold.
    //
    // The threshold has to be raised for this to be expressible at all, and that is worth saying:
    // at the default the site's whole history when the recompile reads it is the handful of calls
    // that got the function promoted, so "what the site saw" is a claim about very little. The
    // shipping threshold is 64; this uses 8 so the second shape is seen four times first.
    [Fact]
    public void APolymorphicSiteIsNotSpecialized()
    {
        var outcome = Compare("""
            function read(p) { var t = p.x; return t; }
            var a = { x: 1 };
            var b = { w: 0, x: 2 };
            var out = [];
            for (var i = 0; i < 5; i++) { out.push(read(a)); out.push(read(b)); }
            out.join('|');
            """, "1|2|1|2|1|2|1|2|1|2", threshold: 8);

        Assert.Equal(0, outcome.SpeculationSites);
    }

    // ── the guarantee the facility exists for ────────────────────────────────────────

    // The receiver is evaluated EXACTLY ONCE. Hand-rolled, the conditional would evaluate it in
    // the guard and again in whichever arm ran, so an effectful receiver would run twice — a
    // wrong answer visible only on receivers nobody tests by hand. This is item 4-3b's stated
    // guarantee, exercised through JavaScript for the first time.
    [Fact]
    public void AnEffectfulReceiverIsEvaluatedOnce()
        => Compare("""
            var calls = 0;
            var target = { x: 1 };
            function make() { calls++; return target; }
            function read() { var t = make().x; return t; }
            var out = [read(), read(), read(), read()];
            out.join('|') + '|' + calls;
            """, "1|1|1|1|4");

    // The same, on the miss path: the fallback arm must not re-evaluate either.
    [Fact]
    public void AnEffectfulReceiverIsEvaluatedOnceWhenTheGuardMisses()
        => Compare("""
            var calls = 0;
            var first = { x: 1 };
            var second = { w: 0, x: 2 };
            var current = first;
            function make() { calls++; return current; }
            function read() { var t = make().x; return t; }
            var out = [read(), read(), read()];
            current = second;
            out.push(read(), read());
            out.join('|') + '|' + calls;
            """, "1|1|1|2|2|5");

    // ── the site range, which is what makes the feedback addressable ─────────────────

    // Two functions with IDENTICAL source text and different site ranges. If the recompile reused
    // one range for both — a shared code cache would do exactly that — the second would read the
    // first's slots. They are given receivers whose shapes put `x` at different slots, so a
    // crossed mapping answers the wrong number rather than merely missing.
    [Fact]
    public void TwoIdenticalFunctionsDoNotShareASiteRange()
        => Compare("""
            function one(p) { var t = p.x; return t; }
            function two(p) { var t = p.x; return t; }
            var flat = { x: 11 };
            var deep = { a: 0, b: 0, x: 22 };
            var out = [];
            for (var i = 0; i < 4; i++) { out.push(one(flat)); out.push(two(deep)); }
            out.join('|');
            """, "11|22|11|22|11|22|11|22");

    // Several reads in one body, so the ordinal mapping has to line up for more than one site.
    // Each key sits at a different slot, so a mapping off by one answers the wrong property.
    [Fact]
    public void SeveralReadsInOneBodyKeepTheirOwnSites()
    {
        var outcome = Compare("""
            function read(p) { var t = p.a + ':' + p.b + ':' + p.c; return t; }
            var o = { a: 'A', b: 'B', c: 'C' };
            [read(o), read(o), read(o), read(o)].join('|');
            """, "A:B:C|A:B:C|A:B:C|A:B:C");

        Assert.True(outcome.SpeculationSites >= 3,
            $"expected all three reads specialized, got {outcome.SpeculationSites}");
        Assert.Equal(0, outcome.GuardsMissed);
    }

    // A read whose receiver is not an object at all. The guard's type test is what catches it,
    // and the fallback has to produce the same TypeError the untiered engine produces.
    [Fact]
    public void ANullReceiverStillThrowsTheSameWay()
        => Compare("""
            function read(p) { var t = p.x; return t; }
            var a = { x: 1 };
            var out = [read(a), read(a), read(a)];
            try { read(null); out.push('no throw'); } catch (e) { out.push(e.name); }
            out.join('|');
            """, "1|1|1|TypeError");

    // A receiver that is a primitive: `'s'.length` resolves through the String wrapper, never
    // through a shape slot on the receiver.
    [Fact]
    public void APrimitiveReceiverTakesTheGenericPath()
        => Compare("""
            function read(p) { var t = p.length; return t; }
            var a = { length: 1 };
            var out = [read(a), read(a), read(a)];
            out.push(read('abcd'));
            out.join('|');
            """, "1|1|1|4");
}
