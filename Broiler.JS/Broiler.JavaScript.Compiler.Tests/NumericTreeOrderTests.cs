using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Compiler.Tests;

// Item 3-1's order-preserving guard placement: the type test is emitted where the coercion it
// stands in for would have run, instead of ahead of every leaf
// (docs/performance-roadmap.md item 3-1).
//
// The hoisting form this replaces is bounded by evaluation order rather than by what the guard can
// predict — it moves later leaves in front of a coercion, so it has to refuse any tree with an
// impure leaf after the first internal node. The refusal census priced that at 1 762 of 5 396
// candidate arithmetic nodes on the Octane corpus, with 2 718 more refused for having no saving to
// make, most of which are the bottom nodes of chains the first rule had already turned down.
//
// So the whole risk of this change is ORDER, and every fixture below is asserted on BOTH settings
// of BROILER_JS_NUMERIC_TREE_ORDER. That is what makes each one a statement about JavaScript
// rather than a description of the fast path: the hoisting arm reaches these answers by refusing
// to specialize, the ordered arm by specializing correctly, and a disagreement between the two is
// the bug this file exists to catch.
//
// The cases that matter most are the ones where something observable sits BETWEEN two leaf
// evaluations — a valueOf that mutates a later leaf, a getter that does, and a coercion that
// throws before a later leaf is even read. Those are exactly the trees the old rule refused, and
// they are now compiled rather than declined.
[Collection(Phase3DiagnosticsCollection.Name)]
public sealed class NumericTreeOrderTests
{
    private static string Eval(string body, bool ordered)
    {
        var previous = NumericTreeOrdering.Enabled;
        var speculation = NumericSpeculation.Enabled;
        NumericTreeOrdering.Enabled = ordered;
        NumericSpeculation.Enabled = true;
        try
        {
            using var context = new JSContext();
            return context.Eval("(function(){ " + body + " })()").ToString();
        }
        finally
        {
            NumericTreeOrdering.Enabled = previous;
            NumericSpeculation.Enabled = speculation;
        }
    }

    [Fact]
    public void TheSwitchDefaultsOn()
    {
        Assert.True(NumericTreeOrdering.Enabled);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ALeftLeaningChainOfImpureLeavesAnswersTheSame(bool ordered)
    {
        // The shape the whole item is about, and the one the hoisting form cannot take: `+` is
        // left-associative, so `a[0]+a[1]+a[2]+a[3]` coerces the first sum before a[2] is read.
        Assert.Equal("10", Eval("var a = [1, 2, 3, 4]; return a[0] + a[1] + a[2] + a[3];", ordered));
        Assert.Equal("24", Eval("var a = [1, 2, 3, 4]; return a[0] * a[1] * a[2] * a[3];", ordered));
        Assert.Equal("-8", Eval("var a = [1, 2, 3, 4]; return a[0] - a[1] - a[2] - a[3];", ordered));

        // Field reads rather than elements: the census says 1 028 of the 1 762 order-unsafe
        // refusals were blocked by a property read, so this is the commonest blocked leaf and not
        // an afterthought.
        Assert.Equal("10", Eval("var o = { a: 1, b: 2, c: 3, d: 4 }; return o.a + o.b + o.c + o.d;", ordered));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ACoercionThatMutatesALaterLeafOfADeeperTreeKeepsItsOrder(bool ordered)
    {
        // The order argument, stated where the old rule could not follow it. `(a[0] * 2 * 3) + p.v`
        // is the tree NumericSpeculationTests pins as REFUSED by the hoisting form; here the
        // valueOf that runs when a[0] is coerced changes p.v, so reading p.v early is visible in
        // the answer. Reference: coerce a[0] (p.v := 20, yields 4), 4*2*3 = 24, then read p.v = 20.
        Assert.Equal("44", Eval("""
            var p = { v: 1 };
            var o = { valueOf: function () { p.v = 20; return 4; } };
            var a = [o];
            return (a[0] * 2 * 3) + p.v;
            """, ordered));

        // And the same hazard with the mutation two nodes below the leaf it moves.
        Assert.Equal("107", Eval("""
            var a = [1];
            var o = { valueOf: function () { a[0] = 99; return 2; } };
            var b = [o];
            return b[0] * 3 + 2 + a[0];
            """, ordered));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AThrowingCoercionBeatsALaterLeafThatWouldAlsoThrow(bool ordered)
    {
        // The sharpest order fixture in the file, because both arms throw and only the MESSAGE
        // says whether the order held. The reference coerces `bad` — running valueOf, which
        // throws — before it ever evaluates `n.q`, which would throw a TypeError. Hoisting n.q
        // ahead of that coercion would report the wrong error, and it is exactly what the old
        // purity rule existed to prevent.
        Assert.Equal("coerce first", Eval("""
            var bad = { valueOf: function () { throw new Error('coerce first'); } };
            var a = [bad];
            var n = null;
            try { return a[0] * 2 + n.q; } catch (e) { return e.message; }
            """, ordered));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void EveryLeafIsReadExactlyOnceAndLeftToRight(bool ordered)
    {
        // Four impure leaves spread across three internal nodes. A tree that evaluates one twice,
        // or out of order, cannot produce this log — and the sum catches a leaf read once but
        // combined wrongly.
        Assert.Equal("35,w,x,y,z", Eval("""
            var log = [];
            var o = {
              get w() { log.push('w'); return 1; },
              get x() { log.push('x'); return 2; },
              get y() { log.push('y'); return 3; },
              get z() { log.push('z'); return 4; }
            };
            var r = o.w + o.x * o.y + o.z * 7;
            return r + ',' + log.join(',');
            """, ordered));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AFailureHalfWayUpKeepsTheRestOfTheTreeGeneric(bool ordered)
    {
        // The losing side of the ordered form, which the hoisting form does not have: the guard
        // can hold for the bottom of a chain and fail above it, so the accumulated raw double has
        // to be boxed at the node that failed and the rest run generically. valueOf must run
        // exactly once, and the concatenation must see the numeric prefix.
        Assert.Equal("14,1", Eval("""
            var calls = 0;
            var o = { valueOf: function () { calls++; return 7; } };
            var a = [3, 4];
            var r = a[0] + a[1] + o;
            return r + ',' + calls;
            """, ordered));

        // The same, where what defeats the guard is a String rather than an object — so `+`
        // becomes concatenation from that node up while the prefix stayed numeric.
        Assert.Equal("7x2", Eval("var a = [3, 4, 'x', 2]; return a[0] + a[1] + a[2] + a[3];", ordered));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ANonNumberAtTheBottomStillCoercesAtEveryNodeAbove(bool ordered)
    {
        // The mirror of the case above: the guard fails at the FIRST node, so nothing below is
        // native and every node above has to keep coercing. `null` is 0, `undefined` is NaN, and a
        // boolean is 0 or 1 — all of them still, and at every level.
        Assert.Equal("6", Eval("var a = [null, 2, 4]; return a[0] + a[1] + a[2];", ordered));
        Assert.Equal("NaN", Eval("var a = [undefined, 2, 4]; return a[0] + a[1] + a[2];", ordered));
        Assert.Equal("7", Eval("var a = [true, 2, 4]; return a[0] + a[1] + a[2];", ordered));
        Assert.Equal("0", Eval("var a = [false, 2, 4]; return a[0] * a[1] * a[2];", ordered));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ABigIntInADeepTreeStillThrowsOnMixing(bool ordered)
    {
        // IsNumber is false for a BigInt at whichever node it reaches, so the generic arm must
        // produce the TypeError rather than the ordered form quietly coercing it — and it must do
        // so from the middle of a chain, not only as a whole-tree refusal.
        Assert.Equal("TypeError", Eval("""
            var a = [1, 2, 3n];
            try { var r = a[0] + a[1] + a[2]; return 'no throw'; } catch (e) { return e.constructor.name; }
            """, ordered));
        Assert.Equal("6", Eval("var a = [1n, 2n, 3n]; return String(a[0] + a[1] + a[2]);", ordered));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TheValuesAndTheBitwiseSemanticsSurviveTheDeeperTree(bool ordered)
    {
        // The value cases NumericSpeculationTests pins for a two-leaf tree, re-asserted where the
        // ordered form is doing the work: an intermediate now stays a raw double across several
        // nodes, so a NaN, an infinity or a -0 has more places to be lost.
        Assert.Equal("NaN", Eval("var a = [NaN, 1, 2]; return a[0] + a[1] + a[2];", ordered));
        Assert.Equal("Infinity", Eval("var a = [1, 0, 5]; return a[0] / a[1] + a[2];", ordered));
        Assert.Equal("-Infinity", Eval("var a = [0, -1, 1]; return 1 / (a[0] * a[1] * a[2]);", ordered));
        Assert.Equal("NaN", Eval("var a = [Infinity, Infinity, 1]; return a[0] - a[1] + a[2];", ordered));

        // ToInt32 wrapping through a chain: a CLR cast is undefined where ToInt32 wraps, and the
        // ordered form reaches the same JSNumericOperators helpers at every node.
        Assert.Equal("1", Eval("var a = [5, 3, 7]; return a[0] & a[1] & a[2];", ordered));
        Assert.Equal("7", Eval("var a = [5, 3, 1]; return a[0] | a[1] | a[2];", ordered));
        Assert.Equal("2", Eval("var a = [4294967296, 1, 1]; return (a[0] + a[1]) << a[2];", ordered));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ALoopOverElementsAccumulatesTheSame(bool ordered)
    {
        // The NavierStokes-shaped kernel with a chain deep enough that the ordered form is what
        // compiles it. Anything that goes wrong once goes wrong a thousand times here.
        Assert.Equal("333832500", Eval("""
            var a = [];
            for (var i = 0; i < 1000; i++) a[i] = i;
            var s = 0;
            for (var i = 0; i < 1000; i++) s = s + a[i] * a[i] + a[i] * 2;
            return s;
            """, ordered));
    }

    [Fact]
    public void TheOrderedFormSpecializesTheTreeTheHoistingFormRefuses()
    {
        // The counter assertion, because every fixture above also passes when nothing specializes.
        // `(a[0] * 2 * 3) + p.v` is the tree NumericSpeculationTests pins as refused: the hoisting
        // arm leaves the inner multiply specializing on its own, one tree with ONE guarded leaf,
        // while the ordered arm takes the whole thing — one tree with TWO. That is what separates
        // "the root was refused" from "nothing was eligible".
        const string Source = "var p = { v: 1 }; var a = [4]; var r = (a[0] * 2 * 3) + p.v; r;";

        var hoisting = Compile(Source, ordered: false);
        Assert.Equal(1, hoisting.SpeculativeNumericTrees);
        Assert.Equal(1, hoisting.SpeculativeNumericGuards);

        var ordered = Compile(Source, ordered: true);
        Assert.Equal(1, ordered.SpeculativeNumericTrees);
        Assert.Equal(2, ordered.SpeculativeNumericGuards);
    }

    [Fact]
    public void TheOrderUnsafeRefusalIsWhatTheOrderedFormRemoves()
    {
        // The refusal waterfall, asserted rather than only reported: a left-leaning chain of four
        // element reads is refused as OrderUnsafe by the hoisting form and specialized whole by
        // the ordered one. Asserting the REASON is what makes the corpus census readable as
        // "widen this conjunct and that many sites move" rather than as a tally.
        const string Source = "var a = [1, 2, 3, 4]; var s = a[0] + a[1] + a[2] + a[3]; s;";

        var hoisting = Compile(Source, ordered: false);
        Assert.True(
            hoisting.NumericTreeRefusals[(int)NumericTreeRefusal.OrderUnsafe] > 0,
            "the hoisting form must refuse this chain for its order, not for another reason");
        Assert.Equal(0, hoisting.NumericTreeRefusals[(int)NumericTreeRefusal.Specialized]);

        var ordered = Compile(Source, ordered: true);
        Assert.Equal(0, ordered.NumericTreeRefusals[(int)NumericTreeRefusal.OrderUnsafe]);
        Assert.Equal(1, ordered.NumericTreeRefusals[(int)NumericTreeRefusal.Specialized]);
        Assert.Equal(4, ordered.SpeculativeNumericGuards);
    }

    [Fact]
    public void TheOrderBlockerSubCensusNamesTheLeafKind()
    {
        // The sub-census reads against the OrderUnsafe row one for one, so it has to attribute the
        // FIRST blocking leaf and not merely fire. A property read and an element read are
        // separate rows because the corpus separates them: 1 028 against 34.
        var property = Compile("var o = { a: 1, b: 2, c: 3 }; var s = o.a + o.b + o.c; s;", ordered: false);
        Assert.True(property.NumericTreeOrderBlockers[(int)NumericTreeOrderBlocker.PropertyRead] > 0);
        Assert.Equal(0, property.NumericTreeOrderBlockers[(int)NumericTreeOrderBlocker.ElementRead]);

        var element = Compile("var a = [1, 2, 3]; var s = a[0] + a[1] + a[2]; s;", ordered: false);
        Assert.True(element.NumericTreeOrderBlockers[(int)NumericTreeOrderBlocker.ElementRead] > 0);
        Assert.Equal(0, element.NumericTreeOrderBlockers[(int)NumericTreeOrderBlocker.PropertyRead]);

        // And nothing is attributed at all once the conjunct that consults it is gone.
        var ordered = Compile("var a = [1, 2, 3]; var s = a[0] + a[1] + a[2]; s;", ordered: true);
        for (var i = 0; i < ordered.NumericTreeOrderBlockers.Length; i++)
            Assert.Equal(0, ordered.NumericTreeOrderBlockers[i]);
    }

    private static CompilerSpecializationSnapshot Compile(string source, bool ordered)
    {
        var previousOrdering = NumericTreeOrdering.Enabled;
        var previousSpeculation = NumericSpeculation.Enabled;
        NumericTreeOrdering.Enabled = ordered;
        NumericSpeculation.Enabled = true;
        using var context = new JSContext();
        CompilerSpecializationDiagnostics.Reset();
        try
        {
            context.Eval(source);
            return CompilerSpecializationDiagnostics.Snapshot();
        }
        finally
        {
            NumericTreeOrdering.Enabled = previousOrdering;
            NumericSpeculation.Enabled = previousSpeculation;
        }
    }
}
