using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Compiler.Tests;

// An arithmetic tree whose operands the compiler cannot prove numeric is computed on raw doubles
// behind one run-time type test, boxing only its root (docs/performance-roadmap.md item 3-1).
//
// The census this was built from says the guard holds on 73 817 515 of 73 818 646 invocations —
// so the failing arm is what almost never runs, and is therefore exactly what a test suite has to
// exercise. Every case below is asserted on BOTH settings of BROILER_JS_NUMERIC_SPECULATION, which
// makes each one a statement about JavaScript semantics rather than a description of the fast path:
// if the two arms ever disagree, the speculation is wrong and the test says so on the arm that
// changed.
//
// The cases fall into three groups. Values — the arithmetic itself must be identical, including
// NaN, the infinities, -0, and the ToInt32 wrapping the bitwise operators do. Types — a leaf that
// is not a Number must take the generic arm and answer what it always answered, which for `+`
// means string concatenation and for an object means running valueOf. And ORDER — the argument that
// makes hoisting sound is about when a coercion runs relative to a leaf evaluation, so the fixtures
// that matter most are the ones with an observable valueOf and an observable getter in the same
// expression.
[Collection(Phase3DiagnosticsCollection.Name)]
public sealed class NumericSpeculationTests
{
    private static string Eval(string body, bool speculate)
    {
        var previous = NumericSpeculation.Enabled;
        NumericSpeculation.Enabled = speculate;
        try
        {
            using var context = new JSContext();
            return context.Eval("(function(){ " + body + " })()").ToString();
        }
        finally
        {
            NumericSpeculation.Enabled = previous;
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TheOrdinaryArithmeticAnswersTheSame(bool speculate)
    {
        Assert.Equal("7", Eval("var a = [3, 4]; return a[0] + a[1];", speculate));
        Assert.Equal("12", Eval("var a = [3, 4]; return a[0] * a[1];", speculate));
        Assert.Equal("-1", Eval("var a = [3, 4]; return a[0] - a[1];", speculate));
        Assert.Equal("0.75", Eval("var a = [3, 4]; return a[0] / a[1];", speculate));
        Assert.Equal("1", Eval("var a = [3, 4]; return a[1] % a[0];", speculate));
        Assert.Equal("81", Eval("var a = [3, 4]; return a[0] ** a[1];", speculate));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ANestedTreeAnswersTheSame(bool speculate)
    {
        // The shape the item is written around, and the one the measurement prices at three boxes
        // of which two are intermediates. It parses right-leaning, so every leaf precedes the
        // first coercion and the tree is eligible.
        Assert.Equal("8.5", Eval("var s = 4, a = [3]; return s + a[0] * 1.5;", speculate));
        Assert.Equal("25.5", Eval("var a = [3, 4]; return a[0] * a[1] + a[0] * 4.5;", speculate));
        Assert.Equal("5", Eval("var a = [1, 2, 3]; return a[0] + a[1] + a[1];", speculate));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void NaNAndTheInfinitiesAndNegativeZeroAnswerTheSame(bool speculate)
    {
        Assert.Equal("NaN", Eval("var a = [NaN, 1]; return a[0] + a[1];", speculate));
        Assert.Equal("NaN", Eval("var a = [Infinity, Infinity]; return a[0] - a[1];", speculate));
        Assert.Equal("Infinity", Eval("var a = [1, 0]; return a[0] / a[1];", speculate));
        Assert.Equal("-Infinity", Eval("var a = [-1, 0]; return a[0] / a[1];", speculate));
        Assert.Equal("NaN", Eval("var a = [0, 0]; return a[0] / a[1];", speculate));
        // -0 is only observable through 1/x, which is the standard way to see it.
        Assert.Equal("-Infinity", Eval("var a = [0, -1]; return 1 / (a[0] * a[1]);", speculate));
        Assert.Equal("NaN", Eval("var a = [Infinity, 0]; return a[0] % a[1];", speculate));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TheBitwiseOperatorsKeepTheirToInt32Semantics(bool speculate)
    {
        // A CLR cast is undefined on overflow where ToInt32 wraps, which is why these live in
        // JSNumericOperators; a speculative tree reaches them through the same helper.
        Assert.Equal("1", Eval("var a = [5, 3]; return a[0] & a[1];", speculate));
        Assert.Equal("7", Eval("var a = [5, 3]; return a[0] | a[1];", speculate));
        Assert.Equal("6", Eval("var a = [5, 3]; return a[0] ^ a[1];", speculate));
        Assert.Equal("40", Eval("var a = [5, 3]; return a[0] << a[1];", speculate));
        Assert.Equal("-1", Eval("var a = [-1, 0]; return a[0] >> a[1];", speculate));
        Assert.Equal("4294967295", Eval("var a = [-1, 0]; return a[0] >>> a[1];", speculate));
        Assert.Equal("0", Eval("var a = [NaN, 1]; return a[0] & a[1] | 0;", speculate));
        Assert.Equal("1", Eval("var a = [4294967297, 1]; return a[0] & a[1];", speculate));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AStringLeafStillConcatenates(bool speculate)
    {
        // The guard fails and the generic arm has to be the ordinary `+`, not numeric addition.
        Assert.Equal("1x", Eval("var a = [1], b = ['x']; return a[0] + b[0];", speculate));
        Assert.Equal("x1", Eval("var a = [1], b = ['x']; return b[0] + a[0];", speculate));
        // ... while the other operators coerce the string to a number, as they always did.
        Assert.Equal("6", Eval("var a = [2], b = ['3']; return a[0] * b[0];", speculate));
        Assert.Equal("NaN", Eval("var a = [2], b = ['x']; return a[0] * b[0];", speculate));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AnObjectLeafStillRunsValueOf(bool speculate)
    {
        Assert.Equal("7", Eval(
            "var o = { valueOf: function () { return 3; } }; var a = [o, 4]; return a[0] + a[1];",
            speculate));
        Assert.Equal("12", Eval(
            "var o = { valueOf: function () { return 3; } }; var a = [o, 4]; return a[0] * a[1];",
            speculate));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ValueOfRunsExactlyOncePerOperand(bool speculate)
    {
        // Both arms read saved values, so a leaf is evaluated once — but the COERCION is the thing
        // that must also happen once, and it happens in the generic arm only.
        Assert.Equal("3,1", Eval("""
            var calls = 0;
            var o = { valueOf: function () { calls++; return 3; } };
            var a = [o];
            var r = a[0] + 0;
            return r + ',' + calls;
            """, speculate));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AGetterLeafIsReadExactlyOnceAndInSourceOrder(bool speculate)
    {
        // The leaves are hoisted into temporaries, so "once" and "left to right" are both
        // properties that could break and neither is visible in a value assertion alone.
        Assert.Equal("30,x,y", Eval("""
            var log = [];
            var o = {
              get x() { log.push('x'); return 10; },
              get y() { log.push('y'); return 20; }
            };
            var r = o.x + o.y;
            return r + ',' + log.join(',');
            """, speculate));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ACoercionThatMutatesALaterLeafKeepsItsOrder(bool speculate)
    {
        // THE order fixture. `(o * 2) + p.v` is left-leaning, so the reference emission coerces o
        // — running valueOf — BEFORE it reads p.v. Hoisting p.v ahead of that coercion would read
        // the old value, so the eligibility rule refuses the tree. If that rule is ever loosened
        // without this being re-derived, this is what fails.
        Assert.Equal("28", Eval("""
            var p = { v: 1 };
            var o = { valueOf: function () { p.v = 20; return 4; } };
            var a = [o];
            return (a[0] * 2) + p.v;
            """, speculate));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AGetterThatMutatesALaterLeafKeepsItsOrder(bool speculate)
    {
        // The same hazard from the evaluation side rather than the coercion side: reading o.x
        // changes o.y, so left-to-right is observable in the answer.
        Assert.Equal("11", Eval("""
            var o = {
              _y: 1,
              get x() { this._y = 10; return 1; },
              get y() { return this._y; }
            };
            return o.x + o.y;
            """, speculate));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AThrowingLeafStillThrowsBeforeALaterLeafIsRead(bool speculate)
    {
        // A throw is an order too. The second getter must not have run.
        Assert.Equal("caught,", Eval("""
            var log = [];
            var o = {
              get x() { throw new Error('boom'); },
              get y() { log.push('y'); return 1; }
            };
            try { var r = o.x + o.y; } catch (e) { return 'caught,' + log.join(','); }
            return 'no throw';
            """, speculate));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ABigIntLeafStillThrowsOnMixing(bool speculate)
    {
        // IsNumber is false for a BigInt, so the generic arm must produce the TypeError it always
        // did rather than the speculation silently coercing it.
        Assert.Equal("TypeError", Eval("""
            var a = [1n, 2];
            try { return a[0] + a[1], 'no throw'; } catch (e) { return e.constructor.name; }
            """, speculate));
        Assert.Equal("3", Eval("var a = [1n, 2n]; return String(a[0] + a[1]);", speculate));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void NullUndefinedAndBooleansCoerceAsBefore(bool speculate)
    {
        Assert.Equal("1", Eval("var a = [null, 1]; return a[0] + a[1];", speculate));
        Assert.Equal("NaN", Eval("var a = [undefined, 1]; return a[0] + a[1];", speculate));
        Assert.Equal("2", Eval("var a = [true, 1]; return a[0] + a[1];", speculate));
        Assert.Equal("0", Eval("var a = [false, 1]; return a[0] * a[1];", speculate));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AMixOfProvenAndUnprovenOperandsAnswersTheSame(bool speculate)
    {
        // A numeric local is already a raw double and needs no guard; an element read needs one.
        // The tree has to be able to hold both without boxing the proven side to reach the other.
        Assert.Equal("11.5", Eval("""
            var x = 2.5;
            var a = [3, 3];
            var s = 0;
            for (var i = 0; i < 1; i++) s = x + a[0] * 3;
            return s;
            """, speculate));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ALoopOverElementsAccumulatesTheSame(bool speculate)
    {
        // The NavierStokes-shaped kernel: read, arithmetic, store back, many times. Anything that
        // goes wrong once goes wrong a thousand times here.
        Assert.Equal("332833500", Eval("""
            var a = [];
            for (var i = 0; i < 1000; i++) a[i] = i;
            var s = 0;
            for (var i = 0; i < 1000; i++) s = s + a[i] * a[i];
            return s;
            """, speculate));
    }

    [Fact]
    public void TheSwitchDefaultsOn()
    {
        Assert.True(NumericSpeculation.Enabled);
    }

    [Fact]
    public void TheSpecializationActuallyFires()
    {
        // Every case above passes on both arms, which is what it is for and is also exactly what a
        // specialization that never engages would do (§3.5: "an emitter that cannot be fed is not
        // an optimization, and it will pass every test you write for it"). So the compile-time
        // counter is asserted directly, and asserted to stay still with the switch off.
        static long Trees(string source, bool speculate)
        {
            var previous = NumericSpeculation.Enabled;
            NumericSpeculation.Enabled = speculate;
            using var context = new JSContext();
            CompilerSpecializationDiagnostics.Reset();
            try
            {
                context.Eval(source);
                return CompilerSpecializationDiagnostics.Snapshot().SpeculativeNumericTrees;
            }
            finally
            {
                NumericSpeculation.Enabled = previous;
            }
        }

        const string source = "var a = [1, 2]; var s = 0; s = s + a[0] * 1.5; s;";

        Assert.True(Trees(source, true) > 0, "the tree the item is written around must specialize");
        Assert.Equal(0, Trees(source, false));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ATreeWhoseOrderTheHOISTINGFormCannotPreserveIsRefusedByIt(bool ordered)
    {
        // The hoisting form's eligibility rule, asserted as a count rather than only as an answer.
        // In `(a[0] * 2 * 3) + p.v` the multiply's coercion runs before `p.v` is read, so hoisting
        // p.v ahead of the test would move it in front of that coercion and the WHOLE tree has to
        // be refused. The assertion has to distinguish that from the tree specializing anyway,
        // because the inner multiply is separately eligible on its own and does specialize when
        // the outer one is turned down. The guard count is what separates them: refusing the root
        // leaves one tree with ONE guarded leaf, `a[0]`.
        //
        // This fixture asserted that refusal unconditionally, and so it FAILED the moment item
        // 3-1's order-preserving half landed — which is the fixture working rather than a cost, on
        // the same terms as `AnUpdateOnAPropertyCostsTwoBoxesNotOne` when the ToNumeric reuse
        // landed under it. What it pins now is the invariant rather than the total: the answer is
        // 25 either way, and only WHICH form computes it moves. The ordered arm's own counts are
        // owned by NumericTreeOrderTests.
        var previousOrdering = NumericTreeOrdering.Enabled;
        var previous = NumericSpeculation.Enabled;
        NumericTreeOrdering.Enabled = ordered;
        NumericSpeculation.Enabled = true;
        using var context = new JSContext();
        CompilerSpecializationDiagnostics.Reset();
        try
        {
            // Two operators inside the parentheses, so the inner tree clears the "at least one
            // intermediate" bar on its own and the counts below distinguish "root refused" from
            // "nothing was eligible".
            var answer = context.Eval("var p = { v: 1 }; var a = [4]; var r = (a[0] * 2 * 3) + p.v; r;");
            var snapshot = CompilerSpecializationDiagnostics.Snapshot();

            Assert.Equal("25", answer.ToString());
            Assert.Equal(1, snapshot.SpeculativeNumericTrees);
            Assert.Equal(ordered ? 2 : 1, snapshot.SpeculativeNumericGuards);
        }
        finally
        {
            NumericSpeculation.Enabled = previous;
            NumericTreeOrdering.Enabled = previousOrdering;
        }
    }
}
