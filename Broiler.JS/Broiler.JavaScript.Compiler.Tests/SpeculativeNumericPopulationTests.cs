using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Compiler.Tests;

// The population instrument for item 3-8a: names a function's fixed point keeps ONLY when a name
// from outside the function is assumed to hold a number
// (docs/performance-roadmap.md item 3-8a).
//
// These tests exist because the FIRST version of this instrument read zero on all seven Octane
// suites and that zero was nearly reported — before anyone had shown the thing could read anything
// else. It could not, and it was reverted; §3.5 gained the rule that a counter never shown to read
// non-zero is a claim about the counter. So the order in this file is the point of it: **the
// instrument is made to discriminate on constructed shapes here, and only then pointed at the
// corpus.**
//
// Every program below declares exactly ONE function, because the counters are process-wide and the
// analysis walks into nested functions — reading an aggregate as though it described one function
// is the specific mistake that sent item 3-8a's scoping down the wrong path in the first place.
[Collection(Phase3DiagnosticsCollection.Name)]
public sealed class SpeculativeNumericPopulationTests
{
    private readonly record struct Population(long Speculative, long Numeric, long Offered, long Dropped);

    private static Population Count(string source)
    {
        var previous = SpeculativeNumericLocals.Counting;
        SpeculativeNumericLocals.Counting = true;
        using var context = new JSContext();
        CompilerSpecializationDiagnostics.Reset();
        try
        {
            context.Eval(source);
            var c = CompilerSpecializationDiagnostics.Snapshot();
            return new Population(
                c.SpeculativeNumericCandidates,
                c.NumericLocals,
                c.NumericCandidatesOffered,
                c.NumericCandidatesDropped);
        }
        finally
        {
            SpeculativeNumericLocals.Counting = previous;
        }
    }

    [Fact]
    public void TheSwitchIsOffByDefault()
    {
        // It costs a second analysis pass per compiled function, which is compile time nothing
        // else needs to pay.
        Assert.False(SpeculativeNumericLocals.Counting);
    }

    [Fact]
    public void AnInitializerReadingAnOuterNameIsInThePopulation()
    {
        // The shape item 3-8a exists for, and the assertion the first instrument failed silently.
        var p = Count("gg = 3; function f(){ var c = 2 * gg; c++; return c; } f();");

        Assert.Equal(1, p.Speculative);
        Assert.Equal(0, p.Numeric);
        Assert.Equal(1, p.Dropped);
    }

    [Fact]
    public void TheCascadeResolvesRatherThanBeingCountedAsOneRootCause()
    {
        // `r` is dropped for OtherName and `c` for DroppedCandidate. The optimistic pass is a
        // fixed point like the real one, so relaxing the first resolves the second for free and
        // BOTH names come out — which is what makes this a population rather than a count of root
        // causes, and it is the half a hand-written rule would most easily get wrong.
        var p = Count("gg = 3; function f(){ var r = gg; var c = 2 * r; c++; return c + r; } f();");

        Assert.Equal(2, p.Speculative);
        Assert.Equal(2, p.Dropped);
    }

    [Fact]
    public void AProvenNumericLocalIsNotInThePopulation()
    {
        // The control: a name the real analysis already keeps must not appear in a set defined as
        // "kept only under the assumption", or the difference is not a difference.
        var p = Count("function f(){ var c = 2 * 10; c++; return c; } f();");

        Assert.Equal(1, p.Numeric);
        Assert.Equal(0, p.Speculative);
    }

    [Fact]
    public void AParameterInitializedLocalIsNotInThePopulation()
    {
        // THE discriminating negative. This name is dropped too, by a cause one slot away in the
        // same enum — but a parameter is not a name from outside the function, it is a value the
        // caller picks per call, and no test at an initializer decides it for the whole body. An
        // instrument that could not separate these two would report 3-8a's population as
        // everything item 3-3 already deferred to phase 4.
        var p = Count("function f(n){ var c = 2 * n; c++; return c; } f(5);");

        Assert.Equal(0, p.Numeric);
        Assert.Equal(1, p.Dropped);
        Assert.Equal(0, p.Speculative);
    }

    [Fact]
    public void ALocalDefeatedByAPropertyReadIsNotInThePopulation()
    {
        // The other large drop cause on the corpus, refused for the same reason: a property read
        // can hand back anything on every evaluation, so one test at the initializer decides
        // nothing about the rest of the body.
        var p = Count("function f(o){ var c = 2 * o.x; c++; return c; } f({ x: 1 });");

        Assert.Equal(0, p.Speculative);
        Assert.Equal(1, p.Dropped);
    }

    [Fact]
    public void ALocalThatNeverQualifiedIsNotMistakenForAnOuterName()
    {
        // The bug the `declaredNames` set exists to stop, and it is the one that would have
        // inflated the corpus figure rather than zeroed it. `a` is declared in this function and
        // was never offered as a candidate, so it is not in `candidates` — and an instrument that
        // asked only "is it a candidate?" would classify it as a name from OUTSIDE the function
        // and assume it numeric, reporting `c` as reachable when no test on an enclosing value
        // could reach it. The set has to tell "outside the function" from "inside and unqualified".
        var p = Count("function f(){ var a = []; var c = 2 * a; c++; return c; } f();");

        Assert.Equal(0, p.Speculative);
    }
}
