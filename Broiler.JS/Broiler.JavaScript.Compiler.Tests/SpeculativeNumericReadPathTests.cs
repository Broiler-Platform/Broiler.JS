using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Compiler.Tests;

// Item 3-8a's two CONSUMING read paths: the guarded arithmetic tree's leaf and the element index
// (docs/performance-roadmap.md item 3-8a).
//
// The storage half — a raw double, a JSValue slot and a flag saying which is live — was measured on
// its own and came out a 2.1% REGRESSION, because every read still went through the variable's
// readable expression, which boxes the raw half back up. These are the two paths that can consume
// the raw double directly, and they are the half that makes the item pay.
//
// They also introduce the item's one genuinely new hazard, and it is the reason this file exists.
// While the flag is live the slot is DELIBERATELY STALE: `i++` on the fast arm is a native double
// add that writes nothing back. So every consumer has to answer from the flag, and any path that
// reaches for the slot without testing it reads a value that is merely old rather than wrong-looking
// — no exception, no NaN, just a number from several iterations ago.
//
// Asserted on BOTH settings of BROILER_JS_SPECULATIVE_NUMERIC_LOCALS. The control arm has one
// representation and cannot have the bug; a disagreement between the arms IS the bug.
[Collection(Phase3DiagnosticsCollection.Name)]
public sealed class SpeculativeNumericReadPathTests
{
    private static string Eval(string source, bool speculate)
    {
        var previous = SpeculativeNumericLocals2.Enabled;
        var speculation = NumericSpeculation.Enabled;
        SpeculativeNumericLocals2.Enabled = speculate;
        NumericSpeculation.Enabled = true;
        try
        {
            using var context = new JSContext();
            return context.Eval(source).ToString();
        }
        finally
        {
            SpeculativeNumericLocals2.Enabled = previous;
            NumericSpeculation.Enabled = speculation;
        }
    }

    private static long Emitted(string source)
    {
        var previous = SpeculativeNumericLocals2.Enabled;
        var speculation = NumericSpeculation.Enabled;
        SpeculativeNumericLocals2.Enabled = true;
        NumericSpeculation.Enabled = true;
        using var context = new JSContext();
        CompilerSpecializationDiagnostics.Reset();
        try
        {
            context.Eval(source);
            return CompilerSpecializationDiagnostics.Snapshot().SpeculativeNumericLocalsEmitted;
        }
        finally
        {
            SpeculativeNumericLocals2.Enabled = previous;
            NumericSpeculation.Enabled = speculation;
        }
    }

    // The shape every fixture below is built on: a local whose initializer reads a name from
    // outside the function, which is the entire population item 3-8a's count found (26 names on the
    // Octane corpus, 15 of them NavierStokes'). The real analysis drops it for that read; the
    // speculative one keeps it and tests the assumption at run time.
    private const string StaleSlot = """
        gg = 0;
        function f(tail) {
          var x = gg;
          x++; x++; x++;
          return x + tail;
        }
        f("!");
        """;

    [Fact]
    public void TheFixturesActuallyReachTheMechanism()
    {
        // Every other assertion in this file is an answer that must hold on both arms, which a
        // fixture that never compiled a speculative local would satisfy trivially. §3.5's rule
        // about counters applies to tests too: pin that the thing under test was built at all,
        // or the file is a description of nothing.
        Assert.True(Emitted(StaleSlot) > 0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheStaleSlotIsNeverHandedToTheGenericArm(bool speculate)
    {
        // The hazard, at its shortest. Three `x++` on the fast arm leave raw at 3 and the slot at
        // 0; then `x + tail` meets a String and has to take the tree's GENERIC arm, which wants
        // both operands as JSValues. Reading x's slot there answers "0!".
        //
        // This is why a speculative leaf is NOT `IsLeaf: true`. That flag means "the saved operand
        // is the value whichever way the test went", which is true of an ordinary guarded leaf —
        // it saved the JSValue it was handed — and false of this one, whose saved slot is only
        // correct while the flag is DOWN.
        Assert.Equal("3!", Eval(StaleSlot, speculate));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AnElementReadFollowsTheRawIndexWhileTheFlagHolds(bool speculate)
    {
        // The element path's version of the same hazard: `i++` between two reads never touches the
        // slot, so an `a[i]` that indexes by the slot reads a[0] every time and answers 30.
        Assert.Equal("60", Eval("""
            gg = 0;
            function f() {
              var a = [10, 20, 30, 40];
              var i = gg;
              var s = a[i]; i++;
              s += a[i]; i++;
              s += a[i];
              return s;
            }
            f();
            """, speculate));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AnElementReadFallsBackToTheSlotWhenTheFlagIsDown(bool speculate)
    {
        // The losing arm, which has to keep working rather than merely not crash: the local is
        // compiled speculative — that is a static decision — and turns out to hold a String, so
        // the read has to go through the ordinary indexer and index by "1".
        Assert.Equal("20", Eval("""
            gg = "1";
            function f() {
              var a = [10, 20, 30];
              var i = gg;
              i = gg;
              return a[i];
            }
            f();
            """, speculate));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AnElementWriteFollowsTheRawIndexWhileTheFlagHolds(bool speculate)
    {
        // The write is here rather than with the storage half because the MEASUREMENT put it here:
        // with only the two reads guarded the item was still a regression, and NavierStokes' inner
        // loop is `x[currentRow] = (x0[currentRow] + …)` — the local read four ways and written
        // through once, with the one write boxing an index the reads had stopped boxing.
        Assert.Equal("11,22,33", Eval("""
            gg = 0;
            function f() {
              var a = [1, 2, 3];
              var i = gg;
              a[i] = 11; i++;
              a[i] = 22; i++;
              a[i] = 33;
              return a.join(",");
            }
            f();
            """, speculate));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AnElementWriteFallsBackToTheSlotWhenTheFlagIsDown(bool speculate)
    {
        // Its losing arm, and the assignment's own value with it — `a[i] = v` evaluates to v on
        // both arms, which the outer `+ ""` here would lose if the conditional returned the slot.
        Assert.Equal("1,99,3:99", Eval("""
            gg = "1";
            function f() {
              var a = [1, 2, 3];
              var i = gg;
              i = gg;
              var v = (a[i] = 99);
              return a.join(",") + ":" + v;
            }
            f();
            """, speculate));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AnElementWriteEvaluatesItsRightHandSideAfterItsReceiver(bool speculate)
    {
        // §13.15.2 order: the receiver first, the right-hand side after it. Both are bound to
        // temps here — the receiver because both arms need it, the value because `compileValue`
        // is a COMPILATION and calling it once per arm would emit the right-hand side twice —
        // so the order is the emitter's to get right rather than the tree's.
        Assert.Equal("base,rhs:7", Eval("""
            gg = 0;
            function f() {
              var log = [];
              var a = [1, 2, 3];
              var i = gg;
              i = gg;
              var o = { get p() { log.push("base"); return a; } };
              o.p[i] = (log.push("rhs"), 7);
              return log.join(",") + ":" + a[0];
            }
            f();
            """, speculate));
    }

    [Fact]
    public void AnIndexThatTheReceiverCouldDisturbIsNotSpeculativeAtAll()
    {
        // `a[i]` evaluates the receiver before it reads i, so a receiver that changed i's flag
        // would make that order observable — read the flag too early and the raw arm is taken on a
        // speculation the receiver has since taken down, indexing by the 0 the write path leaves
        // behind. This is the fixture that says the case does not exist: to write i from inside a
        // getter the getter must CLOSE OVER i, and a captured binding lives in a JSVariable cell,
        // which is not a candidate for either numeric tier. The two properties are mutually
        // exclusive by construction, not by an ordering rule in the emitter.
        //
        // Asserted as a pair because only the pair says it. The captured program alone emitting
        // nothing is equally consistent with the fixture being malformed — which is exactly how
        // the first version of this test passed against an emitter deliberately broken to read the
        // flag first.
        const string uncaptured = """
            gg = 1; hh = "2";
            function f() {
              var a = [10, 20, 30];
              var i = gg;
              i = gg;
              var o = { get p() { return a; } };
              return o.p[i];
            }
            f();
            """;
        const string captured = """
            gg = 1; hh = "2";
            function f() {
              var a = [10, 20, 30];
              var i = gg;
              i = gg;
              var o = { get p() { i = hh; return a; } };
              return o.p[i];
            }
            f();
            """;

        Assert.True(Emitted(uncaptured) > 0);
        Assert.Equal(0, Emitted(captured));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AnIndexedReadEvaluatesItsReceiverExactlyOnce(bool speculate)
    {
        // A pin rather than a discriminator, and worth having as one: a conditional runs one arm,
        // so duplicating the receiver into both would not change this count today. What it would
        // change is code size and inline-cache-site identity, neither of which a test can see — so
        // this stands guard over the property that IS visible if the emitter ever grows a shape
        // that evaluates both sides.
        Assert.Equal("20:1", Eval("""
            gg = 1;
            function f() {
              var n = 0;
              var a = [10, 20, 30];
              var i = gg;
              i = gg;
              var o = { get p() { n++; return a; } };
              var v = o.p[i];
              return v + ":" + n;
            }
            f();
            """, speculate));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ATreeLeafReadsTheLocalAtItsOwnPositionAndNotAtTheOperator(bool speculate)
    {
        // Why the leaf SNAPSHOTS the three halves instead of reading them where the operator needs
        // them. `c + p.v` reads c first and the getter second, and the getter writes c — so the
        // operator must see the value c had before it ran. Reading the storage at the node answers
        // 100 + 10.
        Assert.Equal("11", Eval("""
            gg = 1;
            function f() {
              var c = gg;
              c++; c--;
              var p = { get v() { c = 100; return 10; } };
              return c + p.v;
            }
            f();
            """, speculate));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheSnapshotCoversTheFlagAndNotOnlyTheValue(bool speculate)
    {
        // The half of the snapshot that is easiest to leave out, because the value alone looks
        // sufficient. Here the getter does not change what c is worth, it changes what c IS — a
        // Number becomes a String — so a leaf that copied the double but re-read the flag at the
        // operator would find the speculation down, take the generic arm, and concatenate: "x10".
        Assert.Equal("11", Eval("""
            gg = 1;
            function f() {
              var c = gg;
              c++; c--;
              var p = { get v() { c = "x"; return 10; } };
              return c + p.v;
            }
            f();
            """, speculate));
    }
}
