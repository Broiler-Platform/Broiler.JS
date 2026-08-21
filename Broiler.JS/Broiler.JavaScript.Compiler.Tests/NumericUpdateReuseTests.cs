using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Compiler.Tests;

// `ToNumeric` coerces the operand of `++`/`--` and hands back the coerced old value. It minted
// unconditionally, so an operand that was already a JSNumber was copied into a second, equal
// JSNumber — 15.4% of everything the Octane corpus boxes, for a value the engine is already
// holding (docs/performance-roadmap.md item 3-1).
//
// Reusing it is sound because a JavaScript Number has no observable identity: it compares by value,
// it cannot carry properties, and `Object.is` on two Numbers is a value comparison. That is the
// same argument the small-integer cache has rested on since P2-2, which already hands the same
// instance to unrelated call sites.
//
// Every case is asserted on BOTH settings of BROILER_JS_NUMERIC_UPDATE_REUSE. That is what makes
// each one a statement about JavaScript semantics rather than a description of the fast path — if
// the arms ever disagree, the reuse is wrong and the test says so on the arm that changed. The
// cases that matter most are the ones where the OLD value is observable separately from the new:
// a postfix result, a coercion from a non-Number, -0, and a valueOf that must run exactly once.
[Collection(Phase3DiagnosticsCollection.Name)]
public sealed class NumericUpdateReuseTests
{
    private static string Eval(string body, bool reuse)
    {
        var previous = NumericUpdateReuse.Enabled;
        NumericUpdateReuse.Enabled = reuse;
        try
        {
            using var context = new JSContext();
            return context.Eval("(function(){ " + body + " })()").ToString();
        }
        finally
        {
            NumericUpdateReuse.Enabled = previous;
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ThePostfixResultIsTheOldValueAndThePrefixResultIsTheNew(bool reuse)
    {
        // The reused instance IS the old value, so if reuse ever handed back the wrong object
        // this is the assertion that sees it.
        Assert.Equal("1,2", Eval("var o = { x: 1 }; var y = o.x++; return y + ',' + o.x;", reuse));
        Assert.Equal("2,2", Eval("var o = { x: 1 }; var y = ++o.x; return y + ',' + o.x;", reuse));
        Assert.Equal("1,0", Eval("var o = { x: 1 }; var y = o.x--; return y + ',' + o.x;", reuse));
        Assert.Equal("0,0", Eval("var o = { x: 1 }; var y = --o.x; return y + ',' + o.x;", reuse));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AnOperandThatIsNotANumberIsStillCoerced(bool reuse)
    {
        // The branch reuse must NOT take. `"1"++` yields the Number 1, not the String "1" — the
        // postfix result is the *coerced* old value, which is the whole reason ToNumeric exists.
        Assert.Equal("1,number", Eval("var o = { x: '1' }; var y = o.x++; return y + ',' + typeof y;", reuse));
        Assert.Equal("0,number", Eval("var o = { x: null }; var y = o.x++; return y + ',' + typeof y;", reuse));
        Assert.Equal("1,number", Eval("var o = { x: true }; var y = o.x++; return y + ',' + typeof y;", reuse));
        Assert.Equal("NaN,number", Eval("var o = { x: undefined }; var y = o.x++; return y + ',' + typeof y;", reuse));
        Assert.Equal("NaN,number", Eval("var o = { x: 'abc' }; var y = o.x++; return y + ',' + typeof y;", reuse));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void NaNAndTheInfinitiesAndNegativeZeroAnswerTheSame(bool reuse)
    {
        Assert.Equal("NaN,NaN", Eval("var o = { x: NaN }; var y = o.x++; return y + ',' + o.x;", reuse));
        Assert.Equal("Infinity,Infinity", Eval("var o = { x: Infinity }; var y = o.x++; return y + ',' + o.x;", reuse));
        Assert.Equal("-Infinity,-Infinity", Eval("var o = { x: -Infinity }; var y = o.x--; return y + ',' + o.x;", reuse));
        // -0 is the value most at risk from a reuse that round-tripped through a double, and it is
        // only observable through 1/x and Object.is. It cannot survive the increment itself —
        // -0 + 1 is 1 — so the half that matters is the OLD value, asserted both ways.
        Assert.Equal(
            "-Infinity,true,1",
            Eval("var o = { x: -0 }; var y = o.x++; return (1 / y) + ',' + Object.is(y, -0) + ',' + o.x;", reuse));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ValueOfRunsExactlyOnce(bool reuse)
    {
        // The operand must be coerced once, and reuse must not add or remove a coercion. An
        // object is never a Number, so this exercises the branch reuse does not take while
        // counting the thing the branch could have disturbed.
        Assert.Equal(
            "1,1,2",
            Eval(
                """
                var calls = 0;
                var o = { x: { valueOf: function () { calls++; return 1; } } };
                var y = o.x++;
                return calls + ',' + y + ',' + o.x;
                """,
                reuse));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AGetterIsReadOnceAndTheSetterSeesTheIncrement(bool reuse)
    {
        Assert.Equal(
            "1,1,5,2",
            Eval(
                """
                var reads = 0, writes = 0, stored = 5;
                var o = {};
                Object.defineProperty(o, 'x', {
                  get: function () { reads++; return 1; },
                  set: function (v) { writes++; stored = v; }
                });
                var y = o.x++;
                return reads + ',' + writes + ',' + (stored === 2 ? 5 : stored) + ',' + (y + 1);
                """,
                reuse));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ABigIntStillUpdatesAsABigInt(bool reuse)
    {
        // The BigInt branch returns the primitive unchanged and always did; reuse is inserted
        // after it, so this is the case that says the insertion did not move it.
        Assert.Equal("1,2,bigint", Eval("var o = { x: 1n }; var y = o.x++; return y + ',' + o.x + ',' + typeof y;", reuse));
        Assert.Equal("1,0,bigint", Eval("var o = { x: 1n }; var y = o.x--; return y + ',' + o.x + ',' + typeof y;", reuse));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ASymbolStillThrows(bool reuse)
    {
        Assert.Equal(
            "TypeError",
            Eval(
                """
                var o = { x: Symbol('s') };
                try { o.x++; return 'no throw'; } catch (e) { return e.constructor.name; }
                """,
                reuse));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TheOldValueHasNoObservableIdentity(bool reuse)
    {
        // The soundness argument itself, asserted rather than assumed: every way JavaScript can
        // compare two Numbers is a value comparison, and a Number cannot carry a property. If any
        // of these could distinguish a reused instance from a fresh one, the change would be
        // observable and this test would fail on exactly one arm.
        Assert.Equal(
            "true,true,true,undefined",
            Eval(
                """
                var o = { x: 5 };
                var a = o.x++;
                var b = 5;
                a.tag = 'x';
                return (a === b) + ',' + (a == b) + ',' + Object.is(a, b) + ',' + a.tag;
                """,
                reuse));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AnElementUpdatesTheSame(bool reuse)
    {
        // The array shape, which is where NavierStokes' increments live.
        Assert.Equal("1,2", Eval("var a = [1]; var y = a[0]++; return y + ',' + a[0];", reuse));
        Assert.Equal("3,2", Eval("var a = [1, 3]; var y = a[1]--; return y + ',' + a[1];", reuse));
    }

    [Fact(Timeout = 600000)]
    public void TheCoercionCountIsTheSameOnBothArmsAndOnlyTheSplitMoves()
    {
        // The counter invariant, and the reason the reuse column exists: the two arms must run the
        // same number of coercions, and differ only in how many of them minted. Without this,
        // "the box count went down" could equally mean the coercion stopped happening.
        var minted = Count(reuse: false);
        var reused = Count(reuse: true);

        Assert.Equal(0, minted.Reused);
        Assert.Equal(0, reused.Minted);
        Assert.True(minted.Minted > 0, "the un-reused arm must mint");
        Assert.Equal(minted.Minted + minted.Reused, reused.Minted + reused.Reused);
    }

    private readonly record struct Coercions(long Minted, long Reused);

    private static Coercions Count(bool reuse)
    {
        var previousReuse = NumericUpdateReuse.Enabled;
        NumericUpdateReuse.Enabled = reuse;
        var previous = ArithmeticOperandDiagnostics.Enabled;
        using var context = new JSContext();
        // Created before the reset: constructing a context evaluates built-in JavaScript of its own.
        ArithmeticOperandDiagnostics.Reset();
        ArithmeticOperandDiagnostics.Enabled = true;
        try
        {
            context.Eval("var o = { x: 1 }; o.x++; o.x++; o.x++; o.x;");
            return new Coercions(
                ArithmeticOperandDiagnostics.UnaryToNumeric,
                ArithmeticOperandDiagnostics.UnaryToNumericReused);
        }
        finally
        {
            ArithmeticOperandDiagnostics.Enabled = previous;
            NumericUpdateReuse.Enabled = previousReuse;
        }
    }

    [Fact(Timeout = 600000)]
    public void TheSwitchIsOnByDefault()
    {
        Assert.True(NumericUpdateReuse.Enabled);
    }
}
