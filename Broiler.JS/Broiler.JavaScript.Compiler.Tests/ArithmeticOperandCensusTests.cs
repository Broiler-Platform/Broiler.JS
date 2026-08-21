using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Compiler.Tests;

// The counters behind item 3-1's shared half: what the generic arithmetic operators are handed at
// run time (docs/performance-roadmap.md item 3-1).
//
// The census says 73 824 712 of 73 824 732 generic invocations across the Octane corpus — every one
// but twenty — arrive with both operands already Numbers. A number that clean is the kind that is
// usually an instrument reporting a constant, and the first version of these counters WAS one: it
// read zero on all seven suites, because the enable was inserted next to the wrong one of two
// identical lines and the driver never turned it on. So what these tests pin is not the corpus
// figure, it is that the counter discriminates: it must move on arithmetic, stay still on
// arithmetic that never reaches the generic path, and separate the both-Numbers case from the case
// where an operand is a string or an object.
[Collection(Phase3DiagnosticsCollection.Name)]
public sealed class ArithmeticOperandCensusTests
{
    private readonly record struct Census(
        long Generic,
        long BothNumbers,
        long RawDouble,
        long UnaryNegate,
        long UnaryUpdate,
        long UnaryToNumeric,
        long UnaryToNumericReused);

    private static Census Count(string source, bool speculate = false, bool reuse = true)
    {
        // Pinned rather than inherited, for every fixture in the file: these counters are
        // process-wide statics and so is the switch, so a fixture that left the switch flipped
        // would otherwise show up as a census that moved on its own.
        var previousReuse = NumericUpdateReuse.Enabled;
        NumericUpdateReuse.Enabled = reuse;
        // Speculation OFF by default here, and that is the point of the parameter rather than an
        // oversight: this census counts what reaches the GENERIC path, and item 3-1's guarded tree
        // exists to take shapes off it. Measuring the population with the thing that consumes the
        // population switched on would report the remainder, not the population.
        var previousSpeculation = NumericSpeculation.Enabled;
        NumericSpeculation.Enabled = speculate;
        var previous = ArithmeticOperandDiagnostics.Enabled;
        using var context = new JSContext();
        // Built after the reset would still be counted, so the context is created first: creating
        // one evaluates built-in JavaScript of its own.
        ArithmeticOperandDiagnostics.Reset();
        ArithmeticOperandDiagnostics.Enabled = true;
        try
        {
            context.Eval(source);
            return new Census(
                ArithmeticOperandDiagnostics.Generic,
                ArithmeticOperandDiagnostics.BothNumbers,
                ArithmeticOperandDiagnostics.RawDoubleOperand,
                ArithmeticOperandDiagnostics.UnaryNegate,
                ArithmeticOperandDiagnostics.UnaryUpdate,
                ArithmeticOperandDiagnostics.UnaryToNumeric,
                ArithmeticOperandDiagnostics.UnaryToNumericReused);
        }
        finally
        {
            ArithmeticOperandDiagnostics.Enabled = previous;
            NumericSpeculation.Enabled = previousSpeculation;
            NumericUpdateReuse.Enabled = previousReuse;
        }
    }

    [Fact(Timeout = 600000)]
    public void ArithmeticOnValuesOutOfAnArrayIsCountedAndIsBothNumbers()
    {
        // The shape the whole item is about: the operands come out of an array, so the compiler
        // can prove nothing about them, and at run time they are Numbers every time.
        var census = Count("""
            var a = [1.5, 2.5];
            var s = 0;
            for (var i = 0; i < 100; i++) s = s + a[0] * a[1];
            s;
            """);

        Assert.True(census.Generic >= 200, $"expected at least 200 generic operations, got {census.Generic}");
        Assert.Equal(census.Generic, census.BothNumbers);

        // And the same shape with item 3-1's guarded tree ON reaches the generic path not at all.
        // This is the census and the specialization checking each other: the first measured a
        // population of 73.8 M invocations whose operands are always Numbers, the second was built
        // to consume it, and one assertion says the second reaches what the first counted.
        var specialized = Count("""
            var a = [1.5, 2.5];
            var s = 0;
            for (var i = 0; i < 100; i++) s = s + a[0] * a[1];
            s;
            """, speculate: true);

        Assert.Equal(0, specialized.Generic);
    }

    [Fact(Timeout = 600000)]
    public void AStringOperandIsCountedAndIsNotBothNumbers()
    {
        // The discriminating case. Without it "100% are both Numbers" could be a counter that
        // increments the same field twice.
        // Both operands come out of arrays, so neither is a compile-time literal — a literal IS
        // already a native double here, and `+` is the one operator with a raw-double overload, so
        // `a[0] + 1` never reaches the generic path at all. That asymmetry is itself a finding and
        // is why this fixture reads the way it does.
        var census = Count("""
            var a = ['x'];
            var b = [1];
            var s = 0;
            for (var i = 0; i < 10; i++) s = a[0] + b[0];
            s;
            """);

        Assert.True(census.Generic >= 10, $"expected at least 10 generic operations, got {census.Generic}");
        Assert.True(
            census.BothNumbers < census.Generic,
            $"a string operand must not count as both-Numbers ({census.BothNumbers} of {census.Generic})");
    }

    [Fact(Timeout = 600000)]
    public void AnObjectOperandIsCountedAndIsNotBothNumbers()
    {
        // An object with valueOf is a Number only AFTER ToPrimitive, and the census reads the
        // operands before any coercion — which is the right place, because that is where a guard
        // on a native form would have to read them.
        var census = Count("""
            var o = { valueOf: function () { return 2; } };
            var a = [o];
            var s = 0;
            for (var i = 0; i < 10; i++) s = a[0] * 3;
            s;
            """);

        Assert.True(census.Generic >= 10, $"expected at least 10 generic operations, got {census.Generic}");
        Assert.True(
            census.BothNumbers < census.Generic,
            $"an un-coerced object must not count as both-Numbers ({census.BothNumbers} of {census.Generic})");
    }

    [Fact(Timeout = 600000)]
    public void ArithmeticOnProvenNumericLocalsNeverReachesTheGenericPath()
    {
        // The control, and the item's whole argument in one assertion: the same arithmetic on
        // locals the compiler has proved numeric is computed on raw doubles and is counted
        // nowhere. The difference between this and the first test is exactly what item 3-1 is
        // about — not the operator, but where its operands live.
        var census = Count("""
            (function () {
              var x = 1.5, y = 2.5, s = 0;
              for (var i = 0; i < 100; i++) s = s + x * y;
              return s;
            })();
            """);

        Assert.Equal(0, census.Generic);
    }

    [Fact(Timeout = 600000)]
    public void ExclusiveOrIsCounted()
    {
        // This one is here because it was MISSING. The census's first version hooked every generic
        // binary operator except `^`, so Crypto's exclusive-ors were minting boxes the census
        // reported as coming from nowhere. Nothing failed — an unhooked operator is silent, which
        // is exactly why the residue has to be checked against the counters rather than trusted.
        var census = Count("""
            var a = [3], b = [5];
            var s = 0;
            for (var i = 0; i < 10; i++) s = a[0] ^ b[0];
            s;
            """);

        Assert.True(census.Generic >= 10, $"expected at least 10 generic operations, got {census.Generic}");
        Assert.Equal(census.Generic, census.BothNumbers);
    }

    // The three unary fixtures below are deliberately loop-free, and the reason is the finding in
    // miniature: written with the `for (var i = 0; i < 10; i++)` the rest of this file uses, all
    // three failed, because the loop counter's OWN `i++` lands in the columns they assert on — a
    // top-level `++` costs one ToNumeric box and one step box, ten iterations of it being exactly
    // the 10 and 20 that showed up. Exact counts on a straight-line body say what the counter
    // attributes without an incidental second source of the same operation.

    [Fact(Timeout = 600000)]
    public void UnaryNegationIsCountedExactlyOncePerOperation()
    {
        // `-x` mints a box, and the count must not double: JSValue.Negate coerces and then
        // delegates to the coerced primitive, which for a Number lands on JSNumber.Negate. Both
        // sites are hooked, so the base counts only the branch that mints.
        var census = Count("""
            var a = [1.5];
            var s = -a[0];
            s;
            """);

        Assert.Equal(1, census.UnaryNegate);
        Assert.Equal(0, census.UnaryUpdate);
        Assert.Equal(0, census.UnaryToNumeric);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AnUpdateOnAPropertyCostsTwoBoxesUntilOneOfThemIsRemoved(bool reuse)
    {
        // The finding the counter was added for, and then the removal it led to. `o.x++` boxed
        // twice — once in ToNumeric, which re-boxed an operand that is already a Number to hand
        // back the old value, and once in the step itself. Splitting the two was the difference
        // between "++ is expensive" and knowing which half of it to remove, and the half that came
        // out is the ToNumeric copy.
        //
        // This fixture is why it is asserted on both settings rather than simply updated: written
        // against the old behaviour it FAILED the moment the reuse landed, in all three suite runs,
        // which is exactly what a census fixture is for. What must not change is the number of
        // coercions — only which side of the split they fall on.
        const string Source = """
            var o = { x: 1 };
            o.x++;
            o.x;
            """;

        var census = Count(Source, reuse: reuse);

        Assert.Equal(1, census.UnaryUpdate);
        Assert.Equal(0, census.UnaryNegate);
        Assert.Equal(1, census.UnaryToNumeric + census.UnaryToNumericReused);

        if (reuse)
        {
            // One box, not two: the coercion happened and minted nothing.
            Assert.Equal(0, census.UnaryToNumeric);
            Assert.Equal(1, census.UnaryToNumericReused);
        }
        else
        {
            Assert.Equal(1, census.UnaryToNumeric);
            Assert.Equal(0, census.UnaryToNumericReused);
        }
    }

    [Fact(Timeout = 600000)]
    public void TheUnaryCountersStayStillOnBinaryArithmetic()
    {
        // The discriminating control for the pair above: binary arithmetic must not leak into the
        // unary columns, or the attribution the residue is computed from is circular.
        var census = Count("""
            var a = [1.5, 2.5];
            var s = a[0] * a[1];
            s;
            """);

        Assert.True(census.Generic >= 1, $"expected at least one generic operation, got {census.Generic}");
        Assert.Equal(0, census.UnaryNegate);
        Assert.Equal(0, census.UnaryUpdate);
        Assert.Equal(0, census.UnaryToNumeric);
    }

    [Fact(Timeout = 600000)]
    public void TheCounterIsOffByDefault()
    {
        // It sits on the arithmetic path, so "off by default" is a property worth asserting
        // rather than assuming.
        Assert.False(ArithmeticOperandDiagnostics.Enabled);
    }

    private readonly record struct Relational(long Total, long BothNumbers, long NeitherNumber);

    private static Relational CountRelational(string source)
    {
        var previousSpeculation = NumericSpeculation.Enabled;
        NumericSpeculation.Enabled = false;
        var previous = ArithmeticOperandDiagnostics.Enabled;
        using var context = new JSContext();
        ArithmeticOperandDiagnostics.Reset();
        ArithmeticOperandDiagnostics.Enabled = true;
        try
        {
            context.Eval(source);
            return new Relational(
                ArithmeticOperandDiagnostics.Relational,
                ArithmeticOperandDiagnostics.RelationalBothNumbers,
                ArithmeticOperandDiagnostics.RelationalNeitherNumber);
        }
        finally
        {
            ArithmeticOperandDiagnostics.Enabled = previous;
            ArithmeticOperandDiagnostics.Reset();
            NumericSpeculation.Enabled = previousSpeculation;
        }
    }

    [Fact(Timeout = 600000)]
    public void EachRelationalComparisonIsCountedExactlyOnce()
    {
        // **The load-bearing fixture for the relational census, and the reason it is a re-entrancy
        // guard rather than a case analysis.** The relational operators re-dispatch: JSObject
        // coerces and calls the primitive's, JSNumber unwraps and calls the base's, and the base
        // hands a BigInt comparison to the other operand's mirror. Hooking only "the ones that
        // answer directly" means enumerating which paths delegate — the same analysis that left
        // BitwiseXor unhooked and silent about it, which the 3-1 census records as how the gap was
        // eventually found.
        //
        // So the property asserted is exact rather than directional: N comparisons in the source
        // must be N in the counter. A double count is as wrong as a miss, and only an exact
        // assertion can see it.
        const int comparisons = 100;
        var census = CountRelational($$"""
            var a = 1.5, b = 2.5, n = 0;
            for (var i = 0; i < {{comparisons}}; i++) { if (a < b) { n = n + 1; } }
            n;
            """);

        // The loop's own `i < 100` is a comparison too, and it is the shape item 3-5 specialized —
        // one side an unboxed double — so it does not reach the generic operator. Asserting the
        // body's comparisons exactly, with the loop head's contribution named rather than absorbed
        // into a `>=`, is what makes this a count instead of a bound.
        Assert.Equal(comparisons, census.Total);
        Assert.Equal(comparisons, census.BothNumbers);
        Assert.Equal(0, census.NeitherNumber);
    }

    [Fact(Timeout = 600000)]
    public void AComparisonThroughAnObjectIsStillCountedOnce()
    {
        // The re-dispatching path, which is where a naive hook double counts: JSObject.Less
        // coerces its receiver and calls the primitive's Less, so two entries run for one
        // source-level comparison.
        const int comparisons = 50;
        var census = CountRelational($$"""
            var o = { valueOf: function () { return 1.5; } }, b = 2.5, n = 0;
            for (var i = 0; i < {{comparisons}}; i++) { if (o < b) { n = n + 1; } }
            n;
            """);

        Assert.Equal(comparisons, census.Total);

        // And it is charged as a MISS, because the operand the source wrote is an object — which
        // is the fact a guard would have to test and fail on, whatever the coercion produces.
        Assert.Equal(0, census.BothNumbers);
    }

    [Fact(Timeout = 600000)]
    public void AStringComparisonSeparatesFromANumericOne()
    {
        // The discriminating half. A census whose both-Numbers share is 100% on everything it is
        // pointed at is indistinguishable from one that cannot tell the two apart, and the corpus
        // reading rests on the difference: 100.00% on Box2D against 0.46% on Splay.
        var strings = CountRelational("""
            var a = "abc", b = "abd", n = 0;
            for (var i = 0; i < 40; i++) { if (a < b) { n = n + 1; } }
            n;
            """);

        Assert.Equal(40, strings.Total);
        Assert.Equal(0, strings.BothNumbers);
        Assert.Equal(40, strings.NeitherNumber);
    }

    [Fact(Timeout = 600000)]
    public void AllFourRelationalOperatorsAreHooked()
    {
        // BitwiseXor's lesson as a fixture rather than as a note: an unhooked operator reports a
        // smaller number and says nothing about it. Four operators, one iteration each, four
        // records — an operator missing its hook shows as three.
        var census = CountRelational("""
            var a = 1.5, b = 2.5;
            var r = (a < b) + (a <= b) + (a > b) + (a >= b);
            r;
            """);

        Assert.Equal(4, census.Total);
        Assert.Equal(4, census.BothNumbers);
    }
}
