using Broiler.JavaScript.Compiler;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Compiler.Tests;

// Every write to a local the numeric analysis admits has to be visible to that analysis, or
// the local is held in a raw CLR double while something stores a string into it
// (docs/performance-roadmap.md P2-2 item 3, corrected under 3-3).
//
// Two writes were invisible, both since a746f82d, and neither is exotic:
//
//   1. A `var` RE-declaration deeper than the function body's own statement list. It names the
//      same function-scoped binding, so `var s = 0; { var s = 'x'; }` really does store a
//      string -- but only the top-level declarations were recorded, so the analysis went on
//      believing s held a number.
//   2. A destructuring pattern's names. AstReduce treats ObjectProperty as a leaf, and the
//      name collector behind RejectEveryNameIn -- unlike its two siblings in
//      FastCompiler.CreateFunction -- never overrode it, so every name bound through an
//      OBJECT pattern was invisible to every rejection path at once.
//
// The failure modes differ and both are here, because "it returns NaN" is the mild one:
//
//   * silent NaN, a wrong answer with nothing thrown -- the destructuring assignments and
//     every nested re-declaration below;
//   * an UNHANDLED System.NotImplementedException ("Assignment target Call (BCallExpression)
//     is not supported") out of ILCodeGenerator.VisitAssign, which aborts compilation of the
//     whole script and cannot be caught from JavaScript -- `var { a: s } = o` and the for-of
//     head below. That is the failure the numeric local's own remarks predict: its readable
//     Expression is a BOXING READ, so writing through it is an assignment to a method call.
//
// So the assertions here are ordinary JavaScript answers. Every one of them is a value the
// engine got wrong, or refused to compile, on the build before the fix.
[Collection(Phase3DiagnosticsCollection.Name)]
public class NumericLocalWriteVisibilityTests
{
    private static string Eval(string body)
    {
        using var context = new JSContext();
        return context.Eval("(function () { " + body + " })()").ToString();
    }

    /// <summary>How many locals the compiler kept in a raw double for this body.</summary>
    private static long NumericLocalsOf(string body)
    {
        using var context = new JSContext();
        CompilerSpecializationDiagnostics.Reset();
        context.Eval("(function () { " + body + " })");
        return CompilerSpecializationDiagnostics.Snapshot().NumericLocals;
    }

    // ── a var re-declared below the function body's own statement list ────────────────

    [Theory]
    // Every nesting a `var` can hide in and still name the same binding. All six returned NaN.
    [InlineData("var s = 0; { var s = 'x'; } return s;", "x")]
    [InlineData("var s = 0; if (true) { var s = 'y'; } return s;", "y")]
    [InlineData("var s = 0; for (var i = 0; i < 1; i++) { var s = 'z'; } return s;", "z")]
    [InlineData("var s = 0; while (s === 0) { var s = 'w'; } return s;", "w")]
    [InlineData("var s = 0; try { var s = 't'; } catch (e) { } return s;", "t")]
    [InlineData("var s = 0; switch (1) { case 1: var s = 'c'; } return s;", "c")]
    [InlineData("var s = 0; do { var s = 'd'; } while (false); return s;", "d")]
    [InlineData("var s = 0; { { var s = 'deep'; } } return s;", "deep")]
    // The re-declaration still being numeric must NOT cost the specialization.
    [InlineData("var s = 0; { var s = 5; } return s;", "5")]
    // A second `var` with no initializer stores nothing, so it must not reject either.
    [InlineData("var s = 1; { var s; } return s;", "1")]
    public void AVarReDeclaredInANestedStatementIsAWrite(string body, string expected)
        => Assert.Equal(expected, Eval(body));

    [Fact]
    public void ANumericReDeclarationKeepsTheLocalSpecialized()
    {
        // The fix records a store; it must not record a REJECTION, or every loop with an
        // inner `var i` would fall off the fast path. This is the guard against over-fixing.
        Assert.True(
            NumericLocalsOf("var s = 0; { var s = 5; } return s;") >= 1,
            "a numeric re-declaration should still leave the local in a raw double");
    }

    // ── names bound through a destructuring pattern ───────────────────────────────────

    [Theory]
    // Destructuring ASSIGNMENT. The object forms returned NaN; the array form did too.
    [InlineData("var s = 0; ({ a: s } = { a: 'x' }); return s;", "x")]
    [InlineData("var s = 0; [s] = ['y']; return s;", "y")]
    [InlineData("var s = 0; ({ a: s = 'd' } = {}); return s;", "d")]
    [InlineData("var s = 0; ({ a: { b: s } } = { a: { b: 'n' } }); return s;", "n")]
    [InlineData("var s = 0; ({ ...s } = { q: 1 }); return typeof s;", "object")]
    [InlineData("var s = 0; [...s] = ['a']; return s.join();", "a")]
    [InlineData("var s = 0; [, s] = ['skip', 'q']; return s;", "q")]
    // Destructuring DECLARATION. These three aborted the process outright.
    [InlineData("var s = 0; var { a: s } = { a: 'x' }; return s;", "x")]
    [InlineData("var s = 0; { var { a: s } = { a: 'y' }; } return s;", "y")]
    [InlineData("var s = 0; for (var { a: s } of [{ a: 'p' }]) { } return s;", "p")]
    [InlineData("var s = 0; var [s] = ['z']; return s;", "z")]
    [InlineData("var s = 0; var { a: { b: s } } = { a: { b: 'deep' } }; return s;", "deep")]
    public void ANameBoundThroughAPatternIsAWrite(string body, string expected)
        => Assert.Equal(expected, Eval(body));

    // ── what the fix must not take away ──────────────────────────────────────────────

    [Fact]
    public void RejectingAPatternDoesNotRejectAnIndexOrAMemberBase()
    {
        // The pattern rule refuses every name in a non-identifier assignment target, so it has
        // to exclude member expressions explicitly: `a[i] = v` is not a binding target, its
        // names are READS, and rejecting them would undo 3-0's unboxed numeric index. Asserted
        // as a count rather than only as an answer, because the wrong version still computes
        // the right values -- just slower and with an allocation per access.
        Assert.True(
            NumericLocalsOf("var a = [], i = 0; for (i = 0; i < 3; i++) { a[i] = i + 0.5; } return a;") >= 1,
            "a[i] = v must leave i in a raw double");

        Assert.True(
            NumericLocalsOf("var o = {}, i = 0; for (i = 0; i < 3; i++) { o.x = i; } return o;") >= 1,
            "o.x = i must leave i in a raw double");
    }

    [Theory]
    // The same shapes as answers, so a regression shows up as a wrong value and not only as a
    // lost optimization.
    [InlineData("var a = [], i = 0; for (i = 0; i < 3; i++) { a[i] = i + 0.5; } return a.join();", "0.5,1.5,2.5")]
    [InlineData("var a = [1, 2, 3], s = 0; for (var i = 0; i < 3; i++) { s = s + a[i]; } return s;", "6")]
    [InlineData("var o = {}, i = 0; for (i = 0; i < 3; i++) { o.x = i; } return o.x + '|' + i;", "2|3")]
    [InlineData("var a = [[0]], i = 0; a[i][i] = 7; return a[0][0];", "7")]
    public void IndexedAndMemberWritesStillComputeTheSameAnswer(string body, string expected)
        => Assert.Equal(expected, Eval(body));

    [Theory]
    // Binding forms that were already handled, kept so the fix cannot quietly change them.
    [InlineData("var s = 0; try { throw 'e'; } catch (s) { } return s;", "0")]
    [InlineData("var s = 0; try { throw 'e'; } catch (s) { return s; }", "e")]
    [InlineData("var s = 0; for (s of ['a']) { } return s;", "a")]
    [InlineData("var s = 0; for (s in { k: 1 }) { } return s;", "k")]
    [InlineData("var s = 0; for (var s of ['a']) { } return s;", "a")]
    [InlineData("{ var s = 'x'; } return s;", "x")]
    [InlineData("var s = 0; { s = 'q'; } return s;", "q")]
    public void TheBindingFormsThatAlreadyWorkedStillDo(string body, string expected)
        => Assert.Equal(expected, Eval(body));
}
