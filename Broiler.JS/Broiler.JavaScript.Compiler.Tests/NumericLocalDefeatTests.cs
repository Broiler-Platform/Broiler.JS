using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Compiler.Tests;

// WHY a local that holds nothing but numbers is not a raw double — one shape per conjunct
// (docs/performance-roadmap.md item 3-8, scoped).
//
// Item 3-1's update-target census found that 98.1% of the corpus's `++`/`--` steps are on a local
// the numeric analysis did not prove numeric, and that 6.76 M of the 7.05 M real boxes behind that
// are NavierStokes' `++currentRow`. Scoping the fix needs the next thing down: not "which names
// were dropped", which the waterfall counts, but WHICH RULE defeats the shape the traffic is in —
// because the rules want different fixes, and two of them can never be widened at all.
//
// The oracle is the update-target census itself, which is exactly the discrimination it was built
// for: a numeric local compiles `c++` to a native add and contributes NO row, while a local that
// stayed a JSValue contributes LocalSlot and a captured one contributes LocalCell. So each fixture
// below is a shape plus the row it lands in plus the conjunct that put it there, and together they
// say which fix reaches NavierStokes and which do not.
[Collection(Phase3DiagnosticsCollection.Name)]
public sealed class NumericLocalDefeatTests
{
    private readonly record struct Defeat(
        long Steps,
        long[] Targets,
        long NumericLocals,
        long[] Rejections,
        long[] DropCauses);

    private static Defeat Analyse(string source, bool speculate = false)
    {
        var previousSpeculation = SpeculativeNumericLocals2.Enabled;
        SpeculativeNumericLocals2.Enabled = speculate;

        // Item 3-1's widening is a SECOND speculation path into the same representation, so the
        // `speculate: false` arm has to pin it too or it is not a control. Left off here because
        // these cases are about the enclosing-name conjunct, which is item 3-8a's population and
        // not the element-read one — a run with the widening's environment variable set would
        // otherwise turn every control arm into a second treatment arm.
        var previousWidening = ElementReadNumericLocals.Enabled;
        ElementReadNumericLocals.Enabled = false;
        var previous = ArithmeticOperandDiagnostics.Enabled;
        using var context = new JSContext();
        ArithmeticOperandDiagnostics.Reset();
        CompilerSpecializationDiagnostics.Reset();
        ArithmeticOperandDiagnostics.Enabled = true;
        try
        {
            context.Eval(source);
            var compiler = CompilerSpecializationDiagnostics.Snapshot();
            return new Defeat(
                ArithmeticOperandDiagnostics.UnaryUpdate,
                ArithmeticOperandDiagnostics.UpdateTargets,
                compiler.NumericLocals,
                compiler.NumericRejections,
                compiler.NumericDropCauses);
        }
        finally
        {
            ArithmeticOperandDiagnostics.Enabled = previous;
            SpeculativeNumericLocals2.Enabled = previousSpeculation;
            ElementReadNumericLocals.Enabled = previousWidening;
        }
    }

    private static long Target(Defeat d, ArithmeticOperandDiagnostics.UpdateTarget t) => d.Targets[(int)t];

    private static long Rejected(Defeat d, NumericLocalRejection r) => d.Rejections[(int)r];

    private static long Dropped(Defeat d, NumericDropCause c) => d.DropCauses[(int)c];

    [Fact]
    public void ALiteralInitializedLocalIsNumericAndCostsNothing()
    {
        // The control every row below is read against.
        var d = Analyse("function f(){ var c = 10; c++; return c; } f();");

        Assert.Equal(0, d.Steps);
        Assert.Equal(1, d.NumericLocals);
    }

    [Fact]
    public void ANestedFunctionDeclarationDoesNotDefeatTheEnclosingLocal()
    {
        // Ruled out first, because it is the obvious suspect and it is innocent:
        // `CanScalarReplaceLocals` tolerates a nested function declaration, so the presence of one
        // is not why NavierStokes' locals are untyped. Every function in NavierStokes' FluidField
        // is full of them.
        var d = Analyse("function f(){ function g(){ return 1; } var c = 10; c++; return c + g(); } f();");

        Assert.Equal(0, d.Steps);
        Assert.Equal(1, d.NumericLocals);
    }

    [Fact]
    public void ALocalNamedByAHoistedFunctionBecomesACellNotASlot()
    {
        // Item 3-7's correctness conjunct: a hoisted `function g(){ return c; }` can read `c`
        // before `var c = 10` runs. It cannot be widened, and it is NOT what defeats NavierStokes'
        // hot names — which is exactly what the LocalCell/LocalSlot split proves, since
        // NavierStokes reports 9 461 760 LocalSlot steps against SIX LocalCell.
        var d = Analyse("function f(){ function g(){ return c; } var c = 10; c++; return c + g(); } f();");

        Assert.Equal(1, Target(d, ArithmeticOperandDiagnostics.UpdateTarget.LocalCell));
        Assert.Equal(0, Target(d, ArithmeticOperandDiagnostics.UpdateTarget.LocalSlot));
        Assert.True(Rejected(d, NumericLocalRejection.CapturedByHoistedFunction) > 0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ALocalInitializedFromAnEnclosingScopeNameIsDroppedAsOtherName(bool speculate)
    {
        // THE shape. `var currentRow = j * rowSize` inside a function nested in the one that
        // declares `rowSize`. The local lands in LocalSlot and the cause is OtherName — the
        // analysis is per-function and will not type a name from outside it.
        var d = Analyse(speculate: speculate, source: """
            function o(){
              var rowSize = 10;
              function f(){ var c = 2 * rowSize; c++; return c; }
              return f();
            }
            o();
            """);

        Assert.Equal(speculate ? 0 : 1, Target(d, ArithmeticOperandDiagnostics.UpdateTarget.LocalSlot));
        Assert.Equal(1, Dropped(d, NumericDropCause.OtherName));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ItIsStillDroppedWhenTheEnclosingNameIsWrittenFromASiblingClosure(bool speculate)
    {
        // NavierStokes exactly: `rowSize` is a FluidField-scope var assigned inside `reset()`.
        // Same answer, and the enclosing name is additionally held by the hoisting rule, which is
        // what makes the cascade permanent — see TheEnclosingNameIsHeldByACorrectnessRule below.
        var d = Analyse(speculate: speculate, source: """
            function o(){
              var rowSize;
              function reset(){ rowSize = 12; }
              function f(){ var c = 2 * rowSize; c++; return c; }
              reset();
              return f();
            }
            o();
            """);

        Assert.Equal(speculate ? 0 : 1, Target(d, ArithmeticOperandDiagnostics.UpdateTarget.LocalSlot));
        Assert.Equal(1, Dropped(d, NumericDropCause.OtherName));
    }

    [Fact]
    public void TheEnclosingNameIsHeldByACorrectnessRuleWhenItsReadersAreDeclarations()
    {
        // Why no static widening can reach NavierStokes. The functions that read `rowSize` are
        // hoisted DECLARATIONS, so `rowSize` itself is rejected by item 3-7's conjunct — the one
        // that is correctness rather than policy. The root of the cascade is therefore permanently
        // untypable, and only a run-time test can reach the names that read it.
        var d = Analyse("""
            function o(){
              var rowSize = 10;
              function f(){ var c = 2 * rowSize; c++; return c; }
              return f();
            }
            o();
            """);

        Assert.True(Rejected(d, NumericLocalRejection.CapturedByHoistedFunction) > 0);
        Assert.Equal(0, Rejected(d, NumericLocalRejection.Accepted));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AProvenNumericEnclosingNameIsStillNotImportedAcrossTheClosure(bool speculate)
    {
        // The gap that is NOT a correctness rule, and the one worth its own item. Here `rowSize`
        // is captured only by a function EXPRESSION, so item 3-7's mechanism types it — the
        // analysis proves it numeric, NumericLocals is 1. The inner local is dropped as OtherName
        // anyway, because the analysis is per-function and never imports the enclosing scope's own
        // conclusion.
        //
        // So a name can be proven numeric in one scope and unusable as a numeric input one level
        // down. That is pure analysis reach, no soundness argument attached to it.
        var d = Analyse(speculate: speculate, source: """
            function o(){
              var rowSize = 10;
              var f = function(){ var c = 2 * rowSize; c++; return c; };
              return f();
            }
            o();
            """);

        Assert.Equal(1, d.NumericLocals);
        Assert.True(Rejected(d, NumericLocalRejection.Accepted) > 0);
        Assert.Equal(speculate ? 0 : 1, Target(d, ArithmeticOperandDiagnostics.UpdateTarget.LocalSlot));
        Assert.Equal(1, Dropped(d, NumericDropCause.OtherName));
    }

    [Fact]
    public void PassingTheValueInAsAnArgumentTradesOneDefeatForAnother()
    {
        // The rewrite a static fix would have to turn NavierStokes into, and it does not help: a
        // parameter is item 3-3's one acknowledged gap, so the drop cause moves from OtherName to
        // Parameter and the local is still a slot. Recorded because "just pass it in" is the first
        // thing anyone proposes.
        var d = Analyse("""
            function o(){
              var rowSize = 10;
              function f(r){ var c = 2 * r; c++; return c; }
              return f(rowSize);
            }
            o();
            """);

        Assert.Equal(1, Target(d, ArithmeticOperandDiagnostics.UpdateTarget.LocalSlot));
        Assert.Equal(1, Dropped(d, NumericDropCause.Parameter));
        Assert.Equal(0, Dropped(d, NumericDropCause.OtherName));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheEnclosingNameIsTheBindingConstraintAndOneCharacterProvesIt(bool speculate)
    {
        // The A/B item 3-8a's population rests on, reduced to a single difference. Two programs
        // whose inner function is identical except that the initializer reads a name from the
        // enclosing scope in one and a literal in the other:
        //
        //     var c = 2 * rowSize;   ->  LocalSlot, and every c++ boxes
        //     var c = 2 * 10;        ->  numeric, and c++ costs nothing
        //
        // Everything else about the two is the same — same nesting, same enclosing declaration,
        // same body, same update. So the enclosing-scope read is not one defeat among several on
        // this shape, it is THE defeat, and it is what 3-8a proposes to test at run time.
        //
        // Kept as a pair rather than as one assertion because the interesting half is the control:
        // a fixture that only showed the slot would be consistent with the local being refused for
        // any of the six other conjuncts, which is exactly the ambiguity the rest of this file
        // exists to remove.
        var viaEnclosingName = Analyse(speculate: speculate, source: """
            function o(){
              var rowSize = 10;
              function f(){ var c = 2 * rowSize; c++; return c; }
              return f();
            }
            o();
            """);

        Assert.Equal(speculate ? 0 : 1, Target(viaEnclosingName, ArithmeticOperandDiagnostics.UpdateTarget.LocalSlot));

        var viaLiteral = Analyse(speculate: speculate, source: """
            function o(){
              function f(){ var c = 2 * 10; c++; return c; }
              return f();
            }
            o();
            """);

        Assert.Equal(0, viaLiteral.Steps);
    }
}
