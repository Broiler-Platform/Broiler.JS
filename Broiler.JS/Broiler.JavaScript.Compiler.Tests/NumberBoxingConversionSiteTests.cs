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

        var root = At(boxing, NumberBoxingConversionSite.GuardedTreeRoot);
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

    [Fact]
    public void Counting_is_off_by_default()
    {
        // The counter sits on an allocation path. A run that leaves it on is a run whose wall
        // clock the census's own documentation says is distorted.
        Assert.False(NumberBoxingDiagnostics.Enabled);
    }
}
