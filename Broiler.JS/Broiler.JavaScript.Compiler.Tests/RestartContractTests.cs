using Broiler.JavaScript.BuiltIns;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Compiler.Tests;

// The restart contract the tiering pilot runs under (docs/performance-roadmap.md item 4-3a).
//
// The pilot's bailout is RESTART, not resume: a failed guard re-enters the unoptimized function
// from the top with the original arguments. There is nothing to resume into — a JavaScript local
// is a CLR local of the compiled method — so restart is the only transfer this engine can
// express today, and it is sound only under three conditions:
//
//   1. every guard fires before any observable effect, because restart re-runs the function;
//   2. the bailout leaves no CallFrameStack slot behind;
//   3. the body is not suspendable, because a generator or async body may already have yielded.
//
// All three held before this file existed. The point of 4-3a is that none of them was STATED or
// CHECKED — condition 3 in particular held only because the EnableTiering call sits inside
// FastCompiler.CreateFunction's ordinary-function `else` branch, so hoisting that call out of
// the branch would have silently started tiering generators. These tests make each condition
// fail loudly instead of quietly.
[Collection(Phase3DiagnosticsCollection.Name)]
public sealed class RestartContractTests
{
    private static JSContext Tiered(int threshold = 2)
        => JavaScriptBootstrap.CreateContextBuilder()
            .UseBuiltInRegistry(DefaultBuiltInRegistry.Instance)
            .UseFunctionTiering(new FunctionTieringOptions
            {
                Enabled = true,
                InvocationThreshold = threshold,
                MaxRecompilations = 8,
                MaxRetainedCodeBytes = 1024 * 1024,
            })
            .Build();

    private const string Reduction =
        "function sum(n) { var s = 0; for (var i = 0; i < n; i++) { s += i; } return s; }";

    // ── condition 3: no suspendable bodies ───────────────────────────────────────────

    // Each of these is a LEGAL suspendable function whose body matches the planner's counted-
    // reduction shape exactly and contains no yield or await — which is what makes it a real
    // hazard rather than a hypothetical one. Tiering such a function would replace a delegate
    // that returns a generator object (or a promise) with one that returns a number.
    [Theory]
    [InlineData("function* sum(n) { var s = 0; for (var i = 0; i < n; i++) { s += i; } return s; }")]
    [InlineData("async function sum(n) { var s = 0; for (var i = 0; i < n; i++) { s += i; } return s; }")]
    [InlineData("async function* sum(n) { var s = 0; for (var i = 0; i < n; i++) { s += i; } return s; }")]
    public void ASuspendableBodyIsNeverATieringCandidate(string declaration)
    {
        using var context = Tiered();
        var result = context.Eval(declaration + """

            var kinds = [];
            for (var k = 0; k < 8; k++) { kinds.push(typeof sum(10)); }
            kinds.join(',');
            """);

        // Every call still returns the generator object / promise, never the reduction's number.
        Assert.Equal("object,object,object,object,object,object,object,object", result.ToString());

        var snapshot = context.FunctionTiering.Snapshot();
        Assert.Equal(0, snapshot.Recompilations);
        Assert.Equal(0, snapshot.DelegateReplacements);
    }

    // The control, and it is what makes the three above mean something: the SAME body as an
    // ordinary function is tiered, so the refusal is about suspendability and not about the
    // shape failing to match.
    [Fact(Timeout = 600000)]
    public void TheSameBodyAsAnOrdinaryFunctionIsTiered()
    {
        using var context = Tiered();
        var result = context.Eval(Reduction + """

            var kinds = [];
            for (var k = 0; k < 8; k++) { kinds.push(typeof sum(10)); }
            kinds.join(',') + '|' + sum(10);
            """);

        Assert.Equal("number,number,number,number,number,number,number,number|45", result.ToString());

        var snapshot = context.FunctionTiering.Snapshot();
        Assert.Equal(1, snapshot.Recompilations);
        Assert.Equal(1, snapshot.DelegateReplacements);
    }

    // A generator that actually yields is the same refusal for a second reason, and is kept
    // separate so a change that only fixed the no-yield case would still fail one of the two.
    [Fact(Timeout = 600000)]
    public void AYieldingGeneratorStillIterates()
    {
        using var context = Tiered();
        var result = context.Eval("""
            function* gen(n) { var s = 0; for (var i = 0; i < n; i++) { s += i; yield s; } return s; }
            var out = [];
            for (var k = 0; k < 8; k++) { var it = gen(4); out.push(it.next().value + ':' + it.next().value); }
            out[0] + '|' + out[7];
            """);

        Assert.Equal("0:1|0:1", result.ToString());
        Assert.Equal(0, context.FunctionTiering.Snapshot().DelegateReplacements);
    }

    // ── condition 1: guards fire before any observable effect ────────────────────────

    // Restart re-runs the function from the top, so an effect the specialized path performed
    // before its guard would happen TWICE. Counting entries by editing the body is not an
    // option — the planner needs the body's exact shape, so any added statement stops it being
    // tiered and the test would pass vacuously. The argument carries the effect instead: a
    // `valueOf` that counts its own calls is observable, and the guard tests `IsNumber` without
    // coercing, so the deoptimizing call must produce EXACTLY the effects the untiered engine
    // produces — not one more.
    [Fact(Timeout = 600000)]
    public void ADeoptimizingCallProducesNoEffectTheUntieredEngineDoesNot()
    {
        const string Probe = """

            var calls = 0;
            var counting = { valueOf: function () { calls++; return 10; } };
            var answer = sum(counting);
            answer + '|' + calls;
            """;

        using var tiered = Tiered();
        var warmed = tiered.Eval(Reduction + """

            for (var k = 0; k < 6; k++) { sum(10); }
            """ + Probe);

        using var plain = new JSContext();
        var control = plain.Eval(Reduction + Probe);

        // Same answer AND the same number of observable effects. A specialized path that
        // coerced before its guard would show a higher count on the left.
        Assert.Equal(control.ToString(), warmed.ToString());
        Assert.True(tiered.FunctionTiering.Snapshot().Deoptimizations >= 1,
            "the probe call must actually deoptimize, or this asserts nothing");
    }

    // Each guard the plan carries, driven independently: too few arguments, a non-numeric
    // argument, and values the closed form must not be used for. Every one must answer exactly
    // what the untiered engine answers.
    [Theory]
    [InlineData("sum(10)", "45")]
    [InlineData("sum('10')", "45")]        // argument-type guard, then baseline coercion
    [InlineData("sum()", "0")]             // argument-count guard: n is undefined, loop never runs
    [InlineData("sum(3.5)", "6")]          // fractional limit
    [InlineData("sum(-5)", "0")]           // negative limit, loop never runs
    [InlineData("String(sum(NaN))", "0")]  // NaN limit
    [InlineData("Object.is(sum(-0), 0)", "true")]
    public void EveryGuardAnswersWhatTheUntieredEngineAnswers(string expression, string expected)
    {
        using var tiered = Tiered();
        var warmed = tiered.Eval(Reduction + """

            for (var k = 0; k < 6; k++) { sum(10); }
            """ + expression + ";");

        using var plain = new JSContext();
        var control = plain.Eval(Reduction + "\n" + expression + ";");

        Assert.Equal(expected, control.ToString());
        Assert.Equal(control.ToString(), warmed.ToString());
    }

    // ── condition 2: the bailout leaves no frame behind ──────────────────────────────

    // The specialized delegate pushes no CallFrameStack slot, so on the bailout path the
    // baseline pushes exactly one — as it would have without tiering. If a slot were stranded,
    // a deoptimizing call in a deep recursion would either lose frames or refuse to grow back.
    [Fact(Timeout = 600000)]
    public void DeoptimizingInsideDeepRecursionStillUnwinds()
    {
        using var context = Tiered();
        var result = context.Eval(Reduction + """

            function deep(d) { return d === 0 ? sum('10') : deep(d - 1); }
            for (var k = 0; k < 6; k++) { sum(10); }
            var first = deep(200);
            var second = deep(200);
            first + '|' + second + '|' + sum(10);
            """);

        Assert.Equal("45|45|45", result.ToString());
    }

    // A throw raised on the bailout path has to unwind through the frame the baseline pushed,
    // and be catchable — which it would not be if the restart had left the stack inconsistent.
    [Fact(Timeout = 600000)]
    public void AThrowOnTheBailoutPathIsCatchable()
    {
        using var context = Tiered();
        var result = context.Eval("""
            function sum(n) { var s = 0; for (var i = 0; i < n; i++) { s += i; } return s; }
            for (var k = 0; k < 6; k++) { sum(10); }
            var caught = 'none';
            try { sum({ valueOf: function () { throw new Error('boom'); } }); }
            catch (e) { caught = e.message; }
            caught + '|' + sum(10);
            """);

        Assert.Equal("boom|45", result.ToString());
    }

    // And the engine keeps working afterwards: a deoptimized function is re-entered many more
    // times without the state machine wedging or re-promoting into a wrong answer.
    [Fact(Timeout = 600000)]
    public void TheFunctionKeepsAnsweringAfterDeoptimization()
    {
        using var context = Tiered();
        var result = context.Eval(Reduction + """

            for (var k = 0; k < 6; k++) { sum(10); }
            sum('10');
            var after = [];
            for (var k = 0; k < 20; k++) { after.push(sum(10)); }
            after.join(',') === new Array(20).fill(45).join(',');
            """);

        Assert.Equal("true", result.ToString());
    }
}
