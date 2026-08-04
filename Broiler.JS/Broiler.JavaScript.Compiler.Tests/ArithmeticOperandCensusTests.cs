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
    private readonly record struct Census(long Generic, long BothNumbers, long RawDouble);

    private static Census Count(string source, bool speculate = false)
    {
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
                ArithmeticOperandDiagnostics.RawDoubleOperand);
        }
        finally
        {
            ArithmeticOperandDiagnostics.Enabled = previous;
            NumericSpeculation.Enabled = previousSpeculation;
        }
    }

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
    public void TheCounterIsOffByDefault()
    {
        // It sits on the arithmetic path, so "off by default" is a property worth asserting
        // rather than assuming.
        Assert.False(ArithmeticOperandDiagnostics.Enabled);
    }
}
