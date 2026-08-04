using Broiler.JavaScript.Compiler;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Compiler.Tests;

// A `var` declared inside a block can be kept in a CLR double, on the same dominance argument
// the function body already gets, scoped one level down (docs/performance-roadmap.md item 3-3).
//
// This is the half of 3-3 that needed a definite-assignment argument, and the reason is
// hoisting: the binding exists from function entry but its initializer sits inside a block, so
// between the two it is observably `undefined` — which a raw double hoisted to 0 cannot
// represent. A wrong answer here is `0` where the program can see `undefined`, and it is silent.
//
// Two admissions, and they are different arguments:
//
//   * TRANSPARENT — an unlabelled `{ … }` that is a direct statement of the function body (or
//     of another transparent block) is entered whenever control reaches it, and the only exits
//     are `return`/`throw`, which leave the function. So it does not weaken the body's
//     dominance at all and its `var`s carry no extra condition.
//   * CONFINED — a `var` that is a direct statement of any other block, where every reference
//     is inside that block and after it. Entering the block is then what proves the
//     initializer ran, which is the loop-body temporary this item is really about.
//
// Everything else is refused, and the refusals are the important half of this file: a block
// that may not run with a reference after it, a declaration with no block of its own, a
// label a `break` can jump out of, and a `catch` reading what its `try` declared.
[Collection(Phase3DiagnosticsCollection.Name)]
public class BlockScopedVarNumericLocalTests
{
    private static (string Result, long NumericLocals) Compile(string body)
    {
        using var context = new JSContext();
        CompilerSpecializationDiagnostics.Reset();
        var result = context.Eval("(function(){ " + body + " })()").ToString();
        return (result, CompilerSpecializationDiagnostics.Snapshot().NumericLocals);
    }

    private static string Fn(string body)
    {
        using var context = new JSContext();
        return context.Eval("(function(){ " + body + " })()").ToString();
    }

    // ── admitted, and to the same depth as a body-level var ──────────────────────────

    // The floor: three numeric locals for the accumulator, the counter and the value under
    // test. Asserting the COUNT is what makes this a test of the optimization; the control is
    // the same shape with the declaration at body level, which was already eligible.
    [Theory]
    // the item's own probe shape: an unconditional block, read after it
    [InlineData("var s = 0; { var v = 3.5; } for (var i = 0; i < 10; i++) { s = s + v * 2; } return s;", "70")]
    // the control it is measured against
    [InlineData("var s = 0; var v = 3.5; for (var i = 0; i < 10; i++) { s = s + v * 2; } return s;", "70")]
    // transitively through two transparent blocks
    [InlineData("var s = 0; { { var v = 3.5; } } for (var i = 0; i < 10; i++) { s = s + v * 2; } return s;", "70")]
    public void ATransparentBlockDeclarationReachesTheNumericTier(string body, string expected)
    {
        var (result, numericLocals) = Compile(body);

        Assert.Equal(expected, result);
        Assert.Equal(3, numericLocals);
    }

    // The case the item is really about: a temporary declared and consumed inside a loop body.
    // Entering the block is what proves the initializer ran, so the reference has to stay
    // inside it.
    [Fact]
    public void ALoopBodyTemporaryReachesTheNumericTier()
    {
        var (result, numericLocals) = Compile(
            "var s = 0; for (var i = 0; i < 10; i++) { var t = i * 2; s = s + t; } return s;");

        Assert.Equal("90", result);
        Assert.Equal(3, numericLocals);
    }

    [Theory]
    [InlineData("var s = 0; { var v = 2.5; } return s + v;", "2.5")]
    [InlineData("var s = 0; for (var i = 0; i < 4; i++) { var t = i; s = s + t; } return s;", "6")]
    [InlineData("var s = 0; while (s < 3) { var t = 1; s = s + t; } return s;", "3")]
    [InlineData("var s = 0; do { var t = 2; s = s + t; } while (s < 4); return s;", "4")]
    [InlineData("{ var a = 1; var b = 2; } return a + b;", "3")]
    [InlineData("var s = 0; { var v = 0.1; } { var w = 0.2; } return v + w;", "0.30000000000000004")]
    public void TheValuesStayRight(string body, string expected)
        => Assert.Equal(expected, Fn(body));

    // ── refused, and each refusal is a value the program can observe ─────────────────

    // The whole hazard in one line: the block may not run, and the reference after it would
    // then read `undefined`. A raw double would answer 0.
    [Theory]
    [InlineData("var s = 0; if (s) { var t = 1; } return String(t);", "undefined")]
    [InlineData("var s = 0; while (s) { var t = 1; } return String(t);", "undefined")]
    [InlineData("var s = 0; for (var i = 0; i < s; i++) { var t = 1; } return String(t);", "undefined")]
    [InlineData("var s = 0; if (s) { var t = 1; } else { s = 1; } return String(t);", "undefined")]
    public void ABlockThatMayNotRunKeepsItsCellWhenReadAfterwards(string body, string expected)
        => Assert.Equal(expected, Fn(body));

    // No block of its own, so the enclosing block does not dominate the declaration. This is
    // the shape that would have been wrong had the rule keyed on "innermost enclosing block"
    // rather than on being a direct statement of it.
    [Theory]
    [InlineData("var s = 0; if (s) var t = 1; return String(t);", "undefined")]
    [InlineData("var s = 0; while (s) var t = 1; return String(t);", "undefined")]
    [InlineData("var s = 0; if (s) var t = 1; else s = 1; return String(t);", "undefined")]
    public void ADeclarationWithNoBlockOfItsOwnKeepsItsCell(string body, string expected)
        => Assert.Equal(expected, Fn(body));

    // A label is not transparent: `break` can leave the block before the declaration runs, so
    // a reference after it can still observe `undefined`.
    [Theory]
    [InlineData("var s = 0; lbl: { if (!s) break lbl; var v = 1; } return String(v);", "undefined")]
    [InlineData("var s = 1; lbl: { if (!s) break lbl; var v = 1; } return String(v);", "1")]
    public void ALabelledBlockIsNotTransparent(string body, string expected)
        => Assert.Equal(expected, Fn(body));

    // The `catch` is a sibling of the `try`'s block, not inside it — so a `var` the try
    // declared may not have been reached when the catch reads it.
    [Theory]
    [InlineData("var s = 0; try { s = q(); var t = 1; } catch (e) { s = String(t); } return s;", "undefined")]
    [InlineData("var s = 0; try { var t = 1; } finally { s = String(t); } return s;", "1")]
    public void ACatchDoesNotInheritItsTryBlocksDominance(string body, string expected)
        => Assert.Equal(expected, Fn(body));

    // Inside the confining block but textually before the declaration: on the first entry the
    // read precedes the initializer. The accumulator keeps every iteration's reading, because
    // only the FIRST one distinguishes `undefined` from a raw double's 0 — taking the last
    // value would answer "1" either way and prove nothing.
    [Theory]
    [InlineData("var s = ''; { s = s + String(t) + ','; var t = 1; } return s;", "undefined,")]
    [InlineData("var s = ''; for (var i = 0; i < 2; i++) { s = s + String(t) + ','; var t = 1; } return s;", "undefined,1,")]
    [InlineData("var s = ''; var n = 0; while (n < 2) { s = s + String(t) + ','; var t = 1; n = n + 1; } return s;", "undefined,1,")]
    public void AReferenceBeforeTheDeclarationKeepsItsCell(string body, string expected)
        => Assert.Equal(expected, Fn(body));

    // The subtlest refusal, and one an earlier cut of this rule got wrong: the name IS
    // definitely assigned — by the transparent block at the end — but the read sits BEFORE
    // that declaration and after a non-dominating one. Marking the name readable at the
    // non-dominating declaration masks exactly this read, and a raw double answers 0 where the
    // program can see `undefined`. So a name becomes readable at its dominating declaration
    // only.
    [Theory]
    [InlineData("var s = 0; if (s) { var t = 1; } var r = String(t); { var t = 2; } return r;", "undefined")]
    [InlineData("var s = 0; if (s) var t = 1; var r = String(t); { var t = 2; } return r;", "undefined")]
    [InlineData("var s = 0; for (var i = 0; i < s; i++) { var t = 1; } var r = String(t); { var t = 2; } return r;", "undefined")]
    public void ANonDominatingDeclarationDoesNotMakeTheNameReadable(string body, string expected)
        => Assert.Equal(expected, Fn(body));

    // The same shapes with the dominating declaration FIRST have no such read, and stay
    // eligible — the point of keying on the dominating declaration rather than rejecting
    // outright.
    [Theory]
    [InlineData("var s = 0; { var t = 2; } if (s) { var t = 1; } return String(t);", "2")]
    [InlineData("var s = 0; var t = 2; if (s) { var t = 1; } return String(t);", "2")]
    public void ADominatingDeclarationBeforeANonDominatingOneStaysEligible(string body, string expected)
    {
        var (result, numericLocals) = Compile(body);

        Assert.Equal(expected, result);
        Assert.Equal(2, numericLocals);
    }

    // Two declarations in blocks neither of which dominates the other. The value is the same
    // whichever storage wins, so the numeric-local count is the assertion that discriminates:
    // `s` and `i` still specialize and `t` must not.
    [Theory]
    [InlineData("var s = 0; for (var i = 0; i < 2; i++) { s = s + 1; } if (s) { var t = 1; } else { var t = 2; } return s + ',' + t;", "2,1")]
    [InlineData("var s = 0; for (var i = 0; i < 2; i++) { s = s + 1; } if (s) { var t = 1; } if (!s) { var t = 2; } return s + ',' + t;", "2,1")]
    public void TwoDeclarationsInIncomparableBlocksKeepTheirCell(string body, string expected)
    {
        var (result, numericLocals) = Compile(body);

        Assert.Equal(expected, result);
        Assert.Equal(2, numericLocals);
    }

    // Two declarations that BOTH dominate — two transparent blocks, or a body-level `var` and
    // a transparent block — are not a hazard and must stay eligible. Each dominates everything
    // after itself, so a read after either has seen an initializer, and the type proof still
    // runs over both values. Rejecting these would undo the numeric re-declaration case
    // `NumericLocalWriteVisibilityTests` already pins.
    [Theory]
    [InlineData("var s = 0; for (var i = 0; i < 2; i++) { s = s + 1; } { var t = 1; } { var t = 2; } return s + ',' + t;", "2,2")]
    [InlineData("var s = 0; for (var i = 0; i < 2; i++) { s = s + 1; } var t = 1; { var t = 2; } return s + ',' + t;", "2,2")]
    [InlineData("var s = 0; for (var i = 0; i < 2; i++) { s = s + 1; } { var t = 1; } var t = 2; return s + ',' + t;", "2,2")]
    public void TwoDominatingDeclarationsStayEligible(string body, string expected)
    {
        var (result, numericLocals) = Compile(body);

        Assert.Equal(expected, result);
        Assert.Equal(3, numericLocals);
    }

    // ...but only while every value is numeric. The second declaration is still a store.
    [Theory]
    [InlineData("var s = 0; { var t = 1; } { var t = 'x'; } return typeof t;", "string")]
    [InlineData("var s = 0; { var t = 'x'; } { var t = 1; } return typeof t;", "number")]
    public void ASecondDominatingDeclarationIsStillTypeChecked(string body, string expected)
        => Assert.Equal(expected, Fn(body));

    // A confined name touched from outside its block, in the forms that do not reach
    // VisitIdentifier — a compound assignment and an increment both name their target without
    // reading it as an ordinary identifier. Same again: the count is what discriminates.
    [Theory]
    [InlineData("var s = 0; for (var i = 0; i < 2; i++) { var t = 1; } t += 1; return s + ',' + t;", "0,2")]
    [InlineData("var s = 0; for (var i = 0; i < 2; i++) { var t = 1; } t++; return s + ',' + t;", "0,2")]
    [InlineData("var s = 0; for (var i = 0; i < 2; i++) { var t = 1; } return s + ',' + typeof t;", "0,number")]
    public void AConfinedNameTouchedFromOutsideItsBlockKeepsItsCell(string body, string expected)
    {
        var (result, numericLocals) = Compile(body);

        Assert.Equal(expected, result);
        // `s` and `i` only; `t` is reached from outside its block and keeps its cell.
        Assert.Equal(2, numericLocals);
    }

    // The type proof still has to hold: a block-scoped var that later holds something else is
    // no more eligible than a body-level one.
    [Theory]
    [InlineData("var s = 0; { var v = 3.5; v = 'x'; } return typeof v;", "string")]
    [InlineData("var s = 0; for (var i = 0; i < 2; i++) { var t = i; t = {}; } return typeof t;", "object")]
    [InlineData("var s = 0; { var v = 3.5; } var g = function () { return v; }; return g();", "3.5")]
    public void TheTypeProofStillApplies(string body, string expected)
        => Assert.Equal(expected, Fn(body));

    // A rejected block-scoped var must not drag down the body-level locals that do not read it.
    [Fact]
    public void ARejectedBlockVarDoesNotCostTheRestTheirSpecialization()
    {
        var (result, numericLocals) = Compile(
            "var s = 0; for (var i = 0; i < 10; i++) { s = s + 1; } if (s > 100) { var t = 1; } return s + ',' + String(t);");

        Assert.Equal("10,undefined", result);
        // `s` and `i` still specialize; `t` must not.
        Assert.Equal(2, numericLocals);
    }
}
