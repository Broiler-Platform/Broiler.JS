using Broiler.JavaScript.BuiltIns;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Compiler.Tests;

// The instrument that decides item 3-1's re-opened storage half
// (docs/performance-roadmap.md item 3-1, §4.2a).
//
// §4.2a re-opened the typed backing store on a conversion count that had tripled — 24.6 M to
// 69.3 M once the census stopped running 7 of 15 suites — and whose dominant producer had changed
// identity: Gameboy alone mints 26.9 M at 51.0% of its own requests, on a `Uint8Array` memory
// image, which is the shape the item was originally written for. But the counter that produced
// that finding sits in the boxing factory. It can say a raw double crossed into a JSValue; it
// cannot say which of the compiler's emission sites did it — and a typed backing store only
// reaches the ones an element read or an element write mints.
//
// So each emission site now declares itself and the census reports the split. Item 3-9 was closed
// at a population of zero using an instrument that had first been proven to discriminate on nine
// constructed shapes, and the reason that mattered is that a counter reading zero and a counter
// that is not wired up are indistinguishable from the number alone. The same applies here in the
// opposite direction: a site reading high has to be shown to read high FOR THE RIGHT SHAPE. Each
// case below is a shape whose boxing the compiler emits from one known site.
//
// The partition test is the one that cannot be faked: the eight sites must sum to the factory's
// own independent conversion total, so a site that double-counts or one that is never reached
// shows up as a broken sum rather than as a plausible-looking row.
[Collection(Phase3DiagnosticsCollection.Name)]
public sealed class NumberBoxingConversionSiteTests
{
    private static JSContext Context()
        => JavaScriptBootstrap.CreateContextBuilder()
            .UseBuiltInRegistry(DefaultBuiltInRegistry.Instance)
            .Build();

    /// <summary>Runs <paramref name="source"/> with boxing counted and returns the totals.</summary>
    private static (string Answer, NumberBoxingSnapshot Boxing) Count(string source)
    {
        var previously = NumberBoxingDiagnostics.Enabled;
        try
        {
            NumberBoxingDiagnostics.Reset();
            NumberBoxingDiagnostics.Enabled = true;
            using var context = Context();
            var answer = context.Eval(source).ToString();
            return (answer, NumberBoxingDiagnostics.Snapshot());
        }
        finally
        {
            NumberBoxingDiagnostics.Enabled = previously;
            NumberBoxingDiagnostics.Reset();
        }
    }

    private static long At(NumberBoxingSnapshot boxing, NumberBoxingConversionSite site)
        => boxing.ConversionsAt(site);

    /// <summary>Every guarded-tree root, whatever consumes it.</summary>
    private static long Roots(NumberBoxingSnapshot boxing)
        => At(boxing, NumberBoxingConversionSite.GuardedTreeRoot)
            + At(boxing, NumberBoxingConversionSite.GuardedTreeRootIntoElement)
            + At(boxing, NumberBoxingConversionSite.GuardedTreeRootIntoProperty)
            + At(boxing, NumberBoxingConversionSite.GuardedTreeRootIntoLocal)
            + At(boxing, NumberBoxingConversionSite.GuardedTreeRootIntoReturn)
            + At(boxing, NumberBoxingConversionSite.GuardedTreeRootIntoArgument);

    [Fact]
    public void The_eight_sites_partition_the_conversion_total()
    {
        // Deliberately mixed: an arithmetic tree, a numeric local read into a call argument, an
        // update step and a unary negation, so several sites are non-zero at once. A partition
        // that only holds when one site is live proves nothing.
        var (answer, boxing) = Count(
            """
            function f(a, b) { var s = 0; for (var i = 0; i < 200; i++) { s = s + (a * b + i); s = -s; s++; } return s; }
            f(2, 3);
            """);

        Assert.NotEqual("0", answer);

        var summed = 0L;
        foreach (NumberBoxingConversionSite site in Enum.GetValues<NumberBoxingConversionSite>())
            summed += At(boxing, site);

        Assert.Equal(boxing.ConversionRequests, summed);
        Assert.True(boxing.ConversionRequests > 0, "the shape must box at all for the partition to mean anything");
    }

    [Fact]
    public void No_conversion_is_left_unclassified()
    {
        // Every JSNumberBuilder.New call site in the compiler names a site. If one is added
        // without naming one, it lands in Unclassified and this fails — which is the only way a
        // future site can be stopped from silently diluting the split the item is decided on.
        var (_, boxing) = Count(
            """
            function f(a) { var s = 0; for (var i = 0; i < 200; i++) { s = s + a * 1.5; s = -s; s++; } return s; }
            f(3);
            """);

        Assert.Equal(0, At(boxing, NumberBoxingConversionSite.Unclassified));
    }

    [Fact]
    public void An_arithmetic_tree_boxes_at_its_root_and_not_at_every_node()
    {
        // Item 3-1's whole claim is that a guarded tree mints ONE box for the root instead of one
        // per operator. A left-leaning four-operand sum evaluates three operators; if the root
        // count tracked the operator count rather than the evaluation count, this is where it
        // would show.
        var (_, boxing) = Count(
            """
            function f(a, b, c, d) { var s = 0; for (var i = 0; i < 100; i++) { s = a + b + c + d; } return s; }
            f(1, 2, 3, 4);
            """);

        // Summed over consumers: since `0105` a root is attributed to whatever consumes it, and
        // this shape's root is consumed by the local `s`.
        var root = Roots(boxing);
        Assert.True(root > 0, "a guarded tree must box its root");

        // Three operators over 100 iterations would be ~300 if the tree boxed per node.
        Assert.True(root <= 200, $"root boxing tracked the operator count rather than the tree count ({root})");
    }

    [Fact]
    public void Reading_a_numeric_local_is_attributed_to_the_local_and_not_to_the_operator()
    {
        // The site that separates "the operators mint the boxes" from "the representation does".
        // `n` is a proven-numeric local living in a raw double; handing it to a call argument
        // boxes it AT THE READ, with no operator anywhere in the expression.
        var (_, boxing) = Count(
            """
            function sink(x) { return x; }
            function f() { var n = 7.5; var s = 0; for (var i = 0; i < 100; i++) { s = sink(n); } return s; }
            f();
            """);

        Assert.True(
            At(boxing, NumberBoxingConversionSite.NumericLocalRead) > 0,
            "reading a raw-double local into a JSValue consumer must be attributed to the read");
    }

    [Fact]
    public void The_update_step_is_attributed_to_the_update_and_not_to_the_binary_operator()
    {
        // `++` was measured at 30.9% of the corpus's boxing (item 3-8's re-opening) and is a
        // different mechanism from `s = s + 1` — it belongs to the numeric local, which is why the
        // two must not share a row.
        var (_, boxing) = Count(
            """
            function sink(x) { return x; }
            function f() { var n = 0; var s = 0; for (var i = 0; i < 100; i++) { s = sink(n++); } return s; }
            f();
            """);

        Assert.True(
            At(boxing, NumberBoxingConversionSite.UpdateStep) > 0,
            "the ++ step must be attributed to the update site");
    }

    [Fact]
    public void Resetting_clears_every_counter_including_the_speculative_read()
    {
        // Item 3-8a's counter was added to the snapshot and not to Reset(), so a host that resets
        // between suites carried it forward and every suite but the first read high. The census
        // reports per suite, which is exactly the shape that defect corrupts.
        Count("function f(a) { var s = 0; for (var i = 0; i < 100; i++) { s = s + a; } return s; } f(2);");

        NumberBoxingDiagnostics.Reset();
        var cleared = NumberBoxingDiagnostics.Snapshot();

        Assert.Equal(0, cleared.Requests);
        Assert.Equal(0, cleared.ConversionRequests);
        Assert.Equal(0, cleared.LiteralRequests);
        Assert.Equal(0, cleared.SpeculativeReadRequests);

        foreach (NumberBoxingConversionSite site in Enum.GetValues<NumberBoxingConversionSite>())
            Assert.Equal(0, At(cleared, site));
    }

    // ── the root box's consumer (item 3-1, `0105`) ────────────────────────────────────────
    //
    // `0103` found 61.79% of the corpus's conversions are the guarded tree's root, and the root is
    // boxed only because its consumer takes a JSValue. Splitting it by consumer is a compile-time
    // attribution, and a compile-time attribution that leaks reads as a finding about the corpus.
    // The leak case below is therefore the load-bearing one: it is the shape that would make the
    // instrument agree with whatever it was pointed at.

    [Fact]
    public void A_root_stored_into_an_element_is_attributed_to_the_element()
    {
        var (_, boxing) = Count(
            """
            function f(a, b, c) { for (var i = 0; i < 100; i++) { a[0] = b * c + i; } return a[0]; }
            f([0], 2, 3);
            """);

        Assert.True(At(boxing, NumberBoxingConversionSite.GuardedTreeRootIntoElement) > 0);
        Assert.Equal(0, At(boxing, NumberBoxingConversionSite.GuardedTreeRootIntoProperty));
    }

    [Fact]
    public void A_root_stored_into_a_property_is_attributed_to_the_property()
    {
        var (_, boxing) = Count(
            """
            function f(o, b, c) { for (var i = 0; i < 100; i++) { o.x = b * c + i; } return o.x; }
            f({ x: 0 }, 2, 3);
            """);

        Assert.True(At(boxing, NumberBoxingConversionSite.GuardedTreeRootIntoProperty) > 0);
        Assert.Equal(0, At(boxing, NumberBoxingConversionSite.GuardedTreeRootIntoElement));
    }

    [Fact]
    public void A_root_returned_is_attributed_to_the_return()
    {
        var (_, boxing) = Count(
            """
            function g(b, c, i) { return b * c + i; }
            function f(b, c) { var s = 0; for (var i = 0; i < 100; i++) { s = g(b, c, i); } return s; }
            f(2, 3);
            """);

        Assert.True(At(boxing, NumberBoxingConversionSite.GuardedTreeRootIntoReturn) > 0);
    }

    [Fact]
    public void A_root_passed_as_an_argument_is_attributed_to_the_argument()
    {
        var (_, boxing) = Count(
            """
            function sink(x) { return x; }
            function f(b, c) { var s = 0; for (var i = 0; i < 100; i++) { s = sink(b * c + i); } return s; }
            f(2, 3);
            """);

        Assert.True(At(boxing, NumberBoxingConversionSite.GuardedTreeRootIntoArgument) > 0);
    }

    [Fact]
    public void A_consumer_does_not_leak_into_a_tree_nested_inside_its_own_right_hand_side()
    {
        // THE case this instrument can fail silently on. The element store's right-hand side
        // contains a SECOND tree, inside a call argument. If the pending consumer survived into
        // that nested visit, the inner tree would be counted as an element store and the element
        // row would be inflated by exactly the shapes the typed-store question is about. The inner
        // tree is an argument, and the outer one is the element.
        //
        // Both trees need two operators to specialize at all: with one operator and no provable
        // leaf the tree is refused for having no saving to make, and a refused outer tree would
        // make this pass for the wrong reason.
        var (_, boxing) = Count(
            """
            function sink(x) { return x; }
            function f(a, b, c, d) { for (var i = 0; i < 100; i++) { a[0] = b * c + sink(d * 2 + i); } return a[0]; }
            f([0], 2, 3, 4);
            """);

        var element = At(boxing, NumberBoxingConversionSite.GuardedTreeRootIntoElement);
        var argument = At(boxing, NumberBoxingConversionSite.GuardedTreeRootIntoArgument);

        Assert.True(argument > 0, "the nested tree is consumed by the call argument");
        Assert.True(element > 0, "the outer tree is consumed by the element store");

        // 100 iterations, one outer tree each. If the consumer leaked, the element row would carry
        // the inner tree's boxes too and roughly double.
        Assert.True(element <= 150, $"the element consumer leaked into the nested tree ({element})");
    }

    [Fact]
    public void A_compound_assignment_does_not_claim_its_right_hand_side()
    {
        // Under `a[0] += <tree>` the tree is an operand of `+=`, not the value stored, so the
        // element store is not its consumer. Attributing it to the element would overstate exactly
        // the row the typed-store question turns on.
        var (_, boxing) = Count(
            """
            function f(a, b, c) { for (var i = 0; i < 100; i++) { a[0] += b * c + i; } return a[0]; }
            f([0], 2, 3);
            """);

        Assert.Equal(0, At(boxing, NumberBoxingConversionSite.GuardedTreeRootIntoElement));
    }

    [Fact]
    public void The_root_consumers_still_partition_the_conversion_total()
    {
        // The partition has to survive the split, or one of the six root rows is double-counting.
        var (_, boxing) = Count(
            """
            function sink(x) { return x; }
            function g(b, c) { return b * c + 1; }
            function f(a, o, b, c) {
              var s = 0;
              for (var i = 0; i < 100; i++) {
                a[0] = b * c + i; o.x = b + c * i; s = b * i + c;
                s = sink(b * c + i) + g(b, c);
              }
              return s;
            }
            f([0], { x: 0 }, 2, 3);
            """);

        var summed = 0L;
        foreach (NumberBoxingConversionSite site in Enum.GetValues<NumberBoxingConversionSite>())
            summed += At(boxing, site);

        Assert.Equal(boxing.ConversionRequests, summed);
        Assert.Equal(0, At(boxing, NumberBoxingConversionSite.Unclassified));
    }

    // ── why the consuming local was refused (item 3-1, `0106`) ───────────────────────────
    //
    // `0105` put 44.36% of the corpus's root boxes into a local, and the seam hypothesis — that
    // they land in locals the tier had ALREADY accepted and are boxed then immediately unboxed —
    // was measured and refuted at 36 boxes of 18.6 M. So each is a local the tier refused, and
    // which refusal carries the weight is the question. Item 3-6 counted these causes per NAME;
    // these are per EXECUTION, and the two rank differently by construction.

    private static long Miss(NumberBoxingSnapshot boxing, NumericLocalMiss miss)
        => boxing.LocalMissAt(miss);

    [Fact]
    public void A_local_refused_for_an_element_read_is_attributed_to_the_element_read()
    {
        // `t` would be numeric but for `a[i]`, which the analysis will not type. The guarded tree
        // then computes it on raw doubles behind a run-time test and boxes the root anyway,
        // because the STATIC prover and the tree's RUN-TIME test do not share a conclusion.
        var (_, boxing) = Count(
            """
            function f(a, b) { var s = 0; for (var i = 0; i < 100; i++) { var t = a[0] * b + i; s = t; } return s; }
            f([2], 3);
            """);

        Assert.True(Miss(boxing, NumericLocalMiss.ElementRead) > 0);
    }

    [Fact]
    public void A_local_refused_for_a_property_read_is_attributed_to_the_property_read()
    {
        var (_, boxing) = Count(
            """
            function f(o, b) { var s = 0; for (var i = 0; i < 100; i++) { var t = o.x * b + i; s = t; } return s; }
            f({ x: 2 }, 3);
            """);

        Assert.True(Miss(boxing, NumericLocalMiss.PropertyRead) > 0);
    }

    [Fact]
    public void A_cascade_is_attributed_to_the_dropped_candidate_rather_than_to_its_root_cause()
    {
        // `t` is refused for the property read; `u` is refused only BECAUSE `t` was. The roadmap's
        // own note on this cause is that such a name "wants nothing at all — fixing its root fixes
        // it for free", so a census that folded cascades into their root causes would overstate
        // how many independent refusals there are to fix.
        var (_, boxing) = Count(
            """
            function f(o, b) {
              var s = 0;
              // Both right-hand sides need two operators, or the one-operator tree is refused for
              // having no saving to make and mints no root box for its refusal to be charged to.
              for (var i = 0; i < 100; i++) { var t = o.x * b + i; var u = t * b + i; s = u; }
              return s;
            }
            f({ x: 2 }, 3);
            """);

        Assert.True(Miss(boxing, NumericLocalMiss.DroppedCandidate) > 0, "the downstream name is a cascade");
        Assert.True(Miss(boxing, NumericLocalMiss.PropertyRead) > 0, "its root is still charged to the property read");
    }

    [Fact]
    public void The_refusals_partition_the_roots_consumed_by_a_refused_local()
    {
        // The load-bearing check: every box counted at the IntoLocal site carries exactly one
        // refusal, so a cause that double-counts or one never reached shows up as a broken sum
        // rather than as a plausible row.
        var (_, boxing) = Count(
            """
            function sink(x) { return x; }
            function f(a, o, b) {
              var s = 0;
              for (var i = 0; i < 100; i++) {
                var t = a[0] * b + i; var u = o.x * b + i; var v = sink(b) * b + i; var w = t * u + v;
                s = w;
              }
              return s;
            }
            f([2], { x: 3 }, 4);
            """);

        var summed = 0L;
        foreach (NumericLocalMiss miss in Enum.GetValues<NumericLocalMiss>())
            summed += Miss(boxing, miss);

        Assert.Equal(
            boxing.ConversionsAt(NumberBoxingConversionSite.GuardedTreeRootIntoLocal),
            summed);
        Assert.True(summed > 0, "the shape must reach the site for the partition to mean anything");
    }

    [Fact]
    public void Counting_is_off_by_default()
    {
        // The counter sits on an allocation path. A run that leaves it on is a run whose wall
        // clock the census's own documentation says is distorted.
        Assert.False(NumberBoxingDiagnostics.Enabled);
    }
}
