using Broiler.JavaScript.BuiltIns;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Compiler.Tests;

// Item 3-5: `<` and `>` with one operand already an unboxed double and the other a JSValue now
// unbox the value side behind a type test, instead of boxing the double side to meet the JSValue
// operator. `for (var i = 0; i < n; i++)` is the shape, and it was costing a box per iteration.
//
// This changes a CORE OPERATOR, so the tests are about semantics first and speed nowhere. The
// fast path is only taken when the value side is already a primitive number, and the whole claim
// that this is sound rests on one line of the spec: relational comparison runs ToPrimitive on both
// operands, and ToPrimitive of a Number is that Number — no valueOf, no toString, no observable
// effect. Everything else must reach the operator it reached before, unchanged.
//
// Every case is written so it runs the mixed path: one side is a numeric local (item 3-3 gives it
// a raw double) and the other is a parameter, which cannot be one.
[Collection(Phase3DiagnosticsCollection.Name)]
public sealed class MixedNumericComparisonTests
{
    private static string Run(string source)
    {
        using var context = JavaScriptBootstrap.CreateContextBuilder()
            .UseBuiltInRegistry(DefaultBuiltInRegistry.Instance)
            .Build();
        try
        {
            return context.Eval(source).ToString();
        }
        catch (System.Exception e)
        {
            return e.GetType().Name + ": " + e.Message;
        }
    }

    /// <summary>
    /// Evaluates <c>i &lt; v</c> and <c>i &gt; v</c> with <c>i</c> a numeric local and <c>v</c> a
    /// parameter, plus the mirrored <c>v &lt; i</c> and <c>v &gt; i</c> so the native side is
    /// tested on both sides of the operator.
    /// </summary>
    private static string Compare(string valueExpression, string localLiteral = "1")
        => Run($$"""
            function probe(v) {
              var i = {{localLiteral}};
              return [i < v, i > v, v < i, v > i].join(',');
            }
            probe({{valueExpression}});
            """);

    // ── the fast path: the value side really is a number ─────────────────────────────

    [Theory]
    [InlineData("2", "1", "true,false,false,true")]
    [InlineData("0", "1", "false,true,true,false")]
    [InlineData("1", "1", "false,false,false,false")]
    // NaN makes every relational comparison false, in both directions. The CLR's unordered
    // compare agrees, which is why only `<` and `>` take this path — `<=`/`>=` compile to an
    // ORDERED compare that would answer true here.
    [InlineData("0/0", "1", "false,false,false,false")]
    [InlineData("1/0", "1", "true,false,false,true")]
    [InlineData("-1/0", "1", "false,true,true,false")]
    // -0 and 0 compare equal, so every strict relation is false.
    [InlineData("-0", "0", "false,false,false,false")]
    [InlineData("1e308", "1", "true,false,false,true")]
    public void NumbersCompareTheSameOnBothSides(string value, string local, string expected)
        => Assert.Equal(expected, Compare(value, local));

    // A numeric local holding NaN, so the NATIVE side is the NaN one.
    [Fact(Timeout = 600000)]
    public void ANaNNativeSideIsFalseEveryWay()
        => Assert.Equal("false,false,false,false", Compare("1", "0/0"));

    // ── the fallback: everything the guard must decline ──────────────────────────────

    // A string operand is converted to a number for a number-vs-string relation.
    [Theory]
    [InlineData("'2'", "true,false,false,true")]
    [InlineData("'0'", "false,true,true,false")]
    [InlineData("'abc'", "false,false,false,false")]
    [InlineData("''", "false,true,true,false")]
    public void AStringOperandStillConverts(string value, string expected)
        => Assert.Equal(expected, Compare(value));

    [Theory]
    // null converts to 0, so 1 < null is false and 1 > null is true.
    [InlineData("null", "false,true,true,false")]
    // undefined converts to NaN, so everything is false.
    [InlineData("undefined", "false,false,false,false")]
    [InlineData("true", "false,false,false,false")]
    [InlineData("false", "false,true,true,false")]
    [InlineData("[]", "false,true,true,false")]
    [InlineData("[2]", "true,false,false,true")]
    [InlineData("{}", "false,false,false,false")]
    public void NonNumberOperandsTakeTheOrdinaryOperator(string value, string expected)
        => Assert.Equal(expected, Compare(value));

    // A boxed Number object is NOT a primitive number, so it must take the fallback — and the
    // fallback unwraps it to the same answer. Getting this wrong would read the wrapper's
    // DoubleValue and answer by accident rather than by rule.
    [Fact(Timeout = 600000)]
    public void ANumberWrapperObjectTakesTheFallbackAndStillAnswers()
        => Assert.Equal("true,false,false,true", Compare("new Number(2)"));

    // A BigInt is comparable with a Number for relational operators but is not a Number, so it
    // must reach the ordinary operator.
    [Fact(Timeout = 600000)]
    public void ABigIntOperandStillCompares()
        => Assert.Equal("true,false,false,true", Compare("2n"));

    // ── effects: the part a guard can silently change ────────────────────────────────

    // valueOf runs exactly once per comparison, and only on the fallback path. Four comparisons,
    // four calls — not eight (the value side is read twice by the emitted code) and not zero.
    [Fact(Timeout = 600000)]
    public void ValueOfRunsExactlyOncePerComparison()
        => Assert.Equal("true,false,false,true|4", Run("""
            var calls = 0;
            var box = { valueOf: function () { calls++; return 2; } };
            function probe(v) {
              var i = 1;
              return [i < v, i > v, v < i, v > i].join(',');
            }
            probe(box) + '|' + calls;
            """));

    // The operands are evaluated left-to-right, exactly once each, whichever side is native.
    [Fact(Timeout = 600000)]
    public void OperandsAreEvaluatedInSourceOrderExactlyOnce()
        => Assert.Equal("true|L,R|true|R,L", Run("""
            var log = [];
            function L(x) { log.push('L'); return x; }
            function R(x) { log.push('R'); return x; }
            function nativeLeft(n) { var i = 1; return (i + L(0)) < R(n); }
            function nativeRight(n) { var i = 1; return R(n) > (i + L(0)); }
            var a = nativeLeft(2), first = log.join(',');
            log = [];
            var b = nativeRight(2), second = log.join(',');   // R is written first here
            [a, first, b, second].join('|');
            """));

    // A throwing valueOf must still throw, from the fallback path.
    [Fact(Timeout = 600000)]
    public void AThrowingValueOfStillThrows()
        => Assert.Equal("boom", Run("""
            var box = { valueOf: function () { throw 'boom'; } };
            function probe(v) { var i = 1; return i < v; }
            try { probe(box); 'no throw'; } catch (e) { e; }
            """));

    // A Symbol cannot be converted to a number, so the comparison must throw a TypeError rather
    // than answering false.
    [Fact(Timeout = 600000)]
    public void ASymbolOperandStillThrows()
        => Assert.Equal("threw", Run("""
            function probe(v) { var i = 1; return i < v; }
            try { probe(Symbol('s')); 'no throw'; } catch (e) { 'threw'; }
            """));

    // ── the loop the item is about ───────────────────────────────────────────────────

    [Fact(Timeout = 600000)]
    public void TheCountedLoopStillComputesTheSameSum()
        => Assert.Equal("4950|4950", Run("""
            function withParameter(n) { var s = 0; for (var i = 0; i < n; i++) s = s + i; return s; }
            function withLiteral() { var s = 0; for (var i = 0; i < 100; i++) s = s + i; return s; }
            [withParameter(100), withLiteral()].join('|');
            """));

    // A bound that is not a number must still terminate the loop the way it did before.
    [Theory]
    [InlineData("'5'", "10")]
    [InlineData("undefined", "0")]
    [InlineData("null", "0")]
    [InlineData("5.5", "15")]
    public void ALoopBoundThatIsNotANumberIsUnchanged(string bound, string expected)
        => Assert.Equal(expected, Run($$"""
            function sum(n) { var s = 0; for (var i = 0; i < n; i++) s = s + i; return s; }
            sum({{bound}});
            """));

    // A bound that CHANGES type mid-loop: the guard is per evaluation, not per site, so the loop
    // must follow it. Getting this wrong would need the fast path to be sticky, which it is not —
    // pinned so it stays that way.
    [Fact(Timeout = 600000)]
    public void ABoundThatChangesTypeMidLoopIsFollowed()
        => Assert.Equal("0,1,2,3", Run("""
            var limit = { n: 0, valueOf: function () { return this.n; } };
            function collect(bound) {
              var out = [];
              for (var i = 0; i < bound; i++) { out.push(i); if (out.length > 20) break; }
              return out.join(',');
            }
            limit.n = 4;
            collect(limit);
            """));

    // The operand that is a property read rather than a parameter — the case that made this fix
    // worth more than "give a parameter a numeric local", because a.length is boxed for the same
    // reason and is at least as common a loop bound.
    [Fact(Timeout = 600000)]
    public void APropertyReadBoundBehavesTheSame()
        => Assert.Equal("6|6", Run("""
            var a = [1, 2, 3];
            function viaLength() { var s = 0; for (var i = 0; i < a.length; i++) s = s + a[i]; return s; }
            function viaLiteral() { var s = 0; for (var i = 0; i < 3; i++) s = s + a[i]; return s; }
            [viaLength(), viaLiteral()].join('|');
            """));
}
