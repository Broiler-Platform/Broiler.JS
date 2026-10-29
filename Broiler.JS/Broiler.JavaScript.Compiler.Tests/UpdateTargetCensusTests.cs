using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Compiler.Tests;

// Where the operand of each `++`/`--` step lives (docs/performance-roadmap.md item 3-1).
//
// The step is the largest single source of boxing left on the corpus after the guarded tree and
// the ToNumeric reuse — 33.2% of what remains — and item 3-1's re-specification named this count
// as the thing to take before a typed backing store is built: if the operand is an element or a
// field the step shares that mechanism, and if it is a local the analysis merely failed to type,
// it is a much smaller change aimed somewhere else entirely.
//
// So what these fixtures pin is that the census DISCRIMINATES, on the same terms as
// ArithmeticOperandCensusTests: each target kind must be reachable and must land in its own row,
// the rows must sum to UnaryUpdate (which is recorded by a different method, so a call site the
// emitter forgot shows up as a shortfall rather than vanishing), and — the one that matters most —
// a numeric local must contribute NOTHING, because `i++` on a raw double compiles to a native add
// and never reaches Increment at all. That last one is what makes "98.1% are locals the analysis
// did not type" a statement about coverage rather than about the operator.
//
// The first version of this census reported 98.1% in `Other`, because an identifier that resolves
// statically but is neither a numeric local nor a JSVariable cell falls through to the shared
// member-update tail unlabelled. A residue that size is a claim about the census and not about the
// engine (§3.5), which is why `Other` is asserted at zero below rather than merely reported.
[Collection(Phase3DiagnosticsCollection.Name)]
public sealed class UpdateTargetCensusTests
{
    private static (long[] Targets, long Total) Count(string source)
    {
        // Pinned rather than inherited: these counters and both switches are process-wide statics.
        var previousReuse = NumericUpdateReuse.Enabled;
        var previousSpeculation = NumericSpeculation.Enabled;
        var previous = ArithmeticOperandDiagnostics.Enabled;
        NumericUpdateReuse.Enabled = true;
        NumericSpeculation.Enabled = true;
        using var context = new JSContext();
        // Created before the reset: building a context evaluates built-in JavaScript of its own.
        ArithmeticOperandDiagnostics.Reset();
        ArithmeticOperandDiagnostics.Enabled = true;
        try
        {
            context.Eval(source);
            return (ArithmeticOperandDiagnostics.UpdateTargets, ArithmeticOperandDiagnostics.UnaryUpdate);
        }
        finally
        {
            ArithmeticOperandDiagnostics.Enabled = previous;
            NumericSpeculation.Enabled = previousSpeculation;
            NumericUpdateReuse.Enabled = previousReuse;
        }
    }

    private static long Row(long[] targets, ArithmeticOperandDiagnostics.UpdateTarget target)
        => targets[(int)target];

    private static void AssertRowsSumToTotal(long[] targets, long total)
    {
        long sum = 0;
        foreach (var row in targets)
            sum += row;

        Assert.Equal(total, sum);
    }

    [Fact]
    public void AnElementUpdateIsCountedAsAnElement()
    {
        // Deliberately loop-free, for the reason 0085's fixtures record: written with the
        // `for (var i = 0; ...; i++)` the rest of these files use, the loop counter's own update
        // lands in the rows being asserted on.
        var (targets, total) = Count("var a = [1, 2]; a[0]++; ++a[1]; a[0];");

        Assert.Equal(2, Row(targets, ArithmeticOperandDiagnostics.UpdateTarget.Element));
        Assert.Equal(0, Row(targets, ArithmeticOperandDiagnostics.UpdateTarget.Property));
        Assert.Equal(0, Row(targets, ArithmeticOperandDiagnostics.UpdateTarget.Other));
        AssertRowsSumToTotal(targets, total);
    }

    [Fact]
    public void AFieldUpdateIsCountedAsAProperty()
    {
        var (targets, total) = Count("var o = { x: 1, y: 2 }; o.x++; --o.y; o.x;");

        Assert.Equal(2, Row(targets, ArithmeticOperandDiagnostics.UpdateTarget.Property));
        Assert.Equal(0, Row(targets, ArithmeticOperandDiagnostics.UpdateTarget.Element));
        Assert.Equal(0, Row(targets, ArithmeticOperandDiagnostics.UpdateTarget.Other));
        AssertRowsSumToTotal(targets, total);
    }

    [Fact]
    public void AComputedKeyThatIsAStringStillCountsAsAnElement()
    {
        // The census splits on syntax — computed against named — because the key is not known
        // before it is evaluated. `a["x"]++` is a named property reached through a computed key,
        // and it lands in Element. Recorded as a fixture rather than a caveat so the limit of the
        // reading is pinned rather than described.
        var (targets, total) = Count("var o = { x: 1 }; o['x']++; o.x;");

        Assert.Equal(1, Row(targets, ArithmeticOperandDiagnostics.UpdateTarget.Element));
        Assert.Equal(0, Row(targets, ArithmeticOperandDiagnostics.UpdateTarget.Property));
        AssertRowsSumToTotal(targets, total);
    }

    [Fact]
    public void ALocalTheAnalysisCannotTypeIsCountedAsASlot()
    {
        // A local assigned from a call's return value: statically resolved, so it never reaches
        // the dynamic path, and not provable numeric, so it stays a JSValue slot. This is 98.1% of
        // the corpus's step and the row the whole census exists to separate.
        //
        // Inside a function, and that is not cosmetic — see ATopLevelVarIsAJSVariableCellRatherThanAGlobalOrALocal below.
        var (targets, total) = Count("""
            function f() { return 1; }
            function hot() { var s = f(); s++; ++s; return s; }
            hot();
            """);

        Assert.Equal(2, Row(targets, ArithmeticOperandDiagnostics.UpdateTarget.LocalSlot));
        Assert.Equal(0, Row(targets, ArithmeticOperandDiagnostics.UpdateTarget.Other));
        AssertRowsSumToTotal(targets, total);
    }

    [Fact]
    public void ATopLevelVarIsAJSVariableCellRatherThanAGlobalOrALocal()
    {
        // Pinned because it is what three of these fixtures got wrong on the first attempt, and
        // getting it wrong twice is what makes it worth a test rather than a comment. The first
        // guess was that a top-level `var` is a local; the second, that it is a global-object
        // property. It is neither: it is a script-scope binding in a JSVariable cell, so it lands
        // in LocalCell, while only an UNDECLARED name takes the dynamic global path
        // (AGlobalUpdateIsCountedAsGlobalOrWith below).
        //
        // It also reads the corpus for us: LocalCell is 359 steps in total there, so a top-level
        // `var` counter is essentially absent from real programs, while EarleyBoyer's 285 009
        // GlobalOrWith steps are genuinely undeclared names.
        var (targets, total) = Count("var s = 0; s++; ++s; s;");

        Assert.Equal(2, Row(targets, ArithmeticOperandDiagnostics.UpdateTarget.LocalCell));
        Assert.Equal(0, Row(targets, ArithmeticOperandDiagnostics.UpdateTarget.GlobalOrWith));
        Assert.Equal(0, Row(targets, ArithmeticOperandDiagnostics.UpdateTarget.LocalSlot));
        AssertRowsSumToTotal(targets, total);
    }

    [Fact]
    public void AGlobalUpdateIsCountedAsGlobalOrWith()
    {
        // No `var`, so the name resolves dynamically through the global object rather than to a
        // static binding.
        var (targets, total) = Count("g = 1; g++; ++g; g;");

        Assert.Equal(2, Row(targets, ArithmeticOperandDiagnostics.UpdateTarget.GlobalOrWith));
        Assert.Equal(0, Row(targets, ArithmeticOperandDiagnostics.UpdateTarget.LocalSlot));
        AssertRowsSumToTotal(targets, total);
    }

    [Fact]
    public void AWithBoundNameIsCountedAsGlobalOrWith()
    {
        var (targets, total) = Count("var o = { n: 1 }; with (o) { n++; } o.n;");

        Assert.Equal(1, Row(targets, ArithmeticOperandDiagnostics.UpdateTarget.GlobalOrWith));
        Assert.Equal(0, Row(targets, ArithmeticOperandDiagnostics.UpdateTarget.Property));
        AssertRowsSumToTotal(targets, total);
    }

    [Fact]
    public void ANumericLocalContributesNothingAtAll()
    {
        // THE discriminating fixture. `i++` on a raw double local is a native add — it never calls
        // Increment, so it appears in no row and in no total. Without this, "98.1% of the corpus's
        // step is on locals the analysis did not type" would be consistent with the census simply
        // counting every local, typed or not, and the finding would say nothing about coverage.
        var (targets, total) = Count("""
            function hot() { var s = 0; for (var i = 0; i < 10; i++) { s = s + i; } return s; }
            hot();
            """);

        Assert.Equal(0, total);
        foreach (var row in targets)
            Assert.Equal(0, row);
    }

    [Fact]
    public void TheRowsSeparateTwoKindsInOneProgram()
    {
        // Every kind at once, so a census that collapsed two of them into one row would still pass
        // each fixture above and fail here.
        var (targets, total) = Count("""
            gg = 1;
            function f() { return 1; }
            function hot() {
              var a = [1];
              var o = { x: 1 };
              var s = f();
              a[0]++;
              o.x++;
              s++;
              gg++;
              return s;
            }
            hot();
            """);

        Assert.Equal(1, Row(targets, ArithmeticOperandDiagnostics.UpdateTarget.Element));
        Assert.Equal(1, Row(targets, ArithmeticOperandDiagnostics.UpdateTarget.Property));
        Assert.Equal(1, Row(targets, ArithmeticOperandDiagnostics.UpdateTarget.LocalSlot));
        Assert.Equal(1, Row(targets, ArithmeticOperandDiagnostics.UpdateTarget.GlobalOrWith));
        Assert.Equal(0, Row(targets, ArithmeticOperandDiagnostics.UpdateTarget.Other));
        Assert.Equal(4, total);
        AssertRowsSumToTotal(targets, total);
    }

    [Fact]
    public void TheCensusIsOffByDefaultAndCountsNothingWhenDisabled()
    {
        var previous = ArithmeticOperandDiagnostics.Enabled;
        using var context = new JSContext();
        ArithmeticOperandDiagnostics.Reset();
        ArithmeticOperandDiagnostics.Enabled = false;
        try
        {
            context.Eval("var a = [1]; a[0]++; a[0];");
            foreach (var row in ArithmeticOperandDiagnostics.UpdateTargets)
                Assert.Equal(0, row);
            Assert.Equal(0, ArithmeticOperandDiagnostics.UnaryUpdate);
        }
        finally
        {
            ArithmeticOperandDiagnostics.Enabled = previous;
        }
    }

    [Fact]
    public void TheStepStillAnswersTheSameWithTheCensusOn()
    {
        // The census carries a compile-time constant into a run-time call, so it changes the
        // emitted code for every update in the program. That the answers are unchanged is the
        // thing a counter must not be trusted about without asserting it.
        var previous = ArithmeticOperandDiagnostics.Enabled;
        ArithmeticOperandDiagnostics.Enabled = true;
        try
        {
            using var context = new JSContext();
            Assert.Equal("1,2", context.Eval("var a = [1]; var r = a[0]++; r + ',' + a[0];").ToString());
            Assert.Equal("2,2", context.Eval("var o = { x: 1 }; var r = ++o.x; r + ',' + o.x;").ToString());
            Assert.Equal("1", context.Eval("var s = '1'; s++; String(s - 1);").ToString());
            Assert.Equal("NaN", context.Eval("var s = 'x'; s++; String(s);").ToString());
        }
        finally
        {
            ArithmeticOperandDiagnostics.Enabled = previous;
        }
    }
}
