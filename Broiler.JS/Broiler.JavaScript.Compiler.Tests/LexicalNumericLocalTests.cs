using Broiler.JavaScript.Compiler;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Compiler.Tests;

// A function-body-top-level `let` or `const` the compiler can prove only ever holds a number
// is kept in a CLR double, exactly as an eligible `var` already was
// (docs/performance-roadmap.md item 3-3).
//
// A lexical binding carries two things a `var` does not, and both live in the JSVariable cell
// that this optimization removes: a temporal dead zone, and (for `const`) read-only-ness. So
// the tests come in three parts. The first is that the gate ADMITS these declarations at all —
// without it every other test here passes vacuously against an unchanged compiler. The second
// is ordinary arithmetic correctness. The third, and the one that matters, is everything the
// gate has to REFUSE: a name whose TDZ is observable, a const that is written, a binding a
// closure can reach, and — the shape a previous attempt at this item got wrong — a `let` in a
// NESTED block, which is a distinct binding per entry and must never take the function's slot.
//
// Shares Phase 3's collection because CompilerSpecializationDiagnostics is a process-wide
// counter set.
[Collection(Phase3DiagnosticsCollection.Name)]
public class LexicalNumericLocalTests
{
    private static string Eval(string source)
    {
        using var context = new JSContext();
        return context.Eval(source).ToString();
    }

    private static (string Result, long NumericLocals) Compile(string body)
    {
        using var context = new JSContext();
        CompilerSpecializationDiagnostics.Reset();
        var result = context.Eval("(function(){ " + body + " })()").ToString();
        return (result, CompilerSpecializationDiagnostics.Snapshot().NumericLocals);
    }

    private static string Fn(string body) => Eval("(function(){ " + body + " })()");

    // ── the gate admits them, and to the same depth as `var` ──────────────────────────

    // The measurement item 3-3 rests on: a `let`/`const` declaration reaches the tier a `var`
    // already did, and it brings the locals downstream of it along — the accumulator in
    // `s = s + v * 2` is only provably numeric while `v` is, which is the 1 -> 3 the item
    // records. Asserting the COUNT rather than only the answer is what makes this a test of
    // the optimization instead of a test of arithmetic.
    [Theory]
    [InlineData("var")]
    [InlineData("let")]
    [InlineData("const")]
    public void AnEligibleDeclarationKeepsItsLocalsInDoubles(string keyword)
    {
        var (result, numericLocals) = Compile(
            keyword + " v = 3.5; var s = 0; for (var i = 0; i < 10; i++) { s = s + v * 2; } return s;");

        Assert.Equal("70", result);
        Assert.Equal(3, numericLocals);
    }

    // ── ordinary correctness over the values doubles make awkward ─────────────────────

    [Theory]
    [InlineData("let v = 0.1; const w = 0.2; return v + w;", "0.30000000000000004")]
    [InlineData("let v = 1; v = v / 0; return v;", "Infinity")]
    [InlineData("let v = -1; v = v / 0; return v;", "-Infinity")]
    [InlineData("let v = 0; v = v / 0; return String(v);", "NaN")]
    [InlineData("const v = -0; return 1 / v;", "-Infinity")]
    [InlineData("let v = 2; v = v ** 53; return v;", "9007199254740992")]
    [InlineData("let v = 5; let w = 2; return v % w;", "1")]
    [InlineData("const v = 7; return v & 3;", "3")]
    [InlineData("let v = 1; for (var i = 0; i < 5; i++) { v = v * 2; } return v;", "32")]
    [InlineData("const v = 3; let w = v + 1; return v * w;", "12")]
    public void TheValuesStayRight(string body, string expected)
        => Assert.Equal(expected, Fn(body));

    // ── what the gate must refuse ─────────────────────────────────────────────────────

    // The temporal dead zone is DISCHARGED rather than removed: a name with any reference
    // before its declaration is rejected, so the throw stays reachable on exactly the names
    // that would otherwise observe it.
    [Fact(Timeout = 600000)]
    public void ReadingALetBeforeItsDeclarationStillThrows()
    {
        using var context = new JSContext();
        var ex = Assert.Throws<JSException>(
            () => context.Eval("(function(){ var r = v; let v = 3.5; return r; })()"));
        Assert.Contains("v", ex.Message);
    }

    [Fact(Timeout = 600000)]
    public void ReadingAConstBeforeItsDeclarationStillThrows()
    {
        using var context = new JSContext();
        Assert.Throws<JSException>(
            () => context.Eval("(function(){ var r = v; const v = 3.5; return r; })()"));
    }

    // A write to a const is a TypeError raised by the binding's cell, and a raw double has no
    // cell to raise it — so a const written anywhere is rejected outright rather than
    // specialized into a store that would silently succeed.
    [Fact(Timeout = 600000)]
    public void AssigningToAConstStillThrows()
    {
        using var context = new JSContext();
        Assert.Throws<JSException>(
            () => context.Eval("(function(){ const v = 3.5; v = 4.5; return v; })()"));
    }

    [Theory]
    [InlineData("const v = 3.5; v += 1; return v;")]
    [InlineData("const v = 3.5; v++; return v;")]
    [InlineData("const v = 3.5; ++v; return v;")]
    [InlineData("const v = 3.5; v--; return v;")]
    public void EveryFormOfWritingAConstStillThrows(string body)
    {
        using var context = new JSContext();
        Assert.Throws<JSException>(() => context.Eval("(function(){ " + body + " })()"));
    }

    // The written const keeps its cell — it is rejected, not specialized. Asserting the count
    // is what distinguishes "the TypeError still fires" from "the TypeError still fires
    // because nothing was optimized in the first place". The control differs only in that the
    // write is absent, so the difference between the two counts is exactly `v`: the control
    // specializes both bindings and the written arm specializes only `s`.
    [Fact(Timeout = 600000)]
    public void AWrittenConstIsRejectedRatherThanSpecialized()
    {
        // `s` deliberately does not read `v`, so rejecting `v` cannot drag `s` down with it
        // and the two counts differ by exactly the one binding under test.
        var control = Compile("const v = 3.5; var s = 0; s = s + 1; return v;");
        Assert.Equal("3.5", control.Result);
        Assert.Equal(2, control.NumericLocals);

        using var context = new JSContext();
        CompilerSpecializationDiagnostics.Reset();
        Assert.Throws<JSException>(
            () => context.Eval("(function(){ const v = 3.5; var s = 0; s = s + 1; v = 1; return s; })()"));
        Assert.Equal(1, CompilerSpecializationDiagnostics.Snapshot().NumericLocals);
    }

    // A name that later holds something other than a number is not numeric, whichever keyword
    // declared it.
    [Theory]
    [InlineData("let v = 3.5; v = 'x'; return typeof v;", "string")]
    [InlineData("let v = 3.5; v = {}; return typeof v;", "object")]
    [InlineData("let v = 3.5; v = null; return String(v);", "null")]
    [InlineData("let v = 3.5; v = undefined; return typeof v;", "undefined")]
    [InlineData("let v = 3.5; v = 1n; return typeof v;", "bigint")]
    public void ALetThatLaterHoldsANonNumberIsRefused(string body, string expected)
        => Assert.Equal(expected, Fn(body));

    // A closure captures through a cell, so naming a lexical binding in a nested function has
    // to keep it — and the capture must observe the LIVE value, not a copy.
    [Theory]
    [InlineData("let v = 1; var g = function () { return v; }; v = 2; return g();", "2")]
    [InlineData("let v = 1; var g = function () { v = 42; }; g(); return v;", "42")]
    [InlineData("let v = 1; var g = () => v * 2; v = 3; return g();", "6")]
    [InlineData("const v = 5; var g = function () { return v; }; return g();", "5")]
    public void ACapturedLexicalBindingStillObservesTheLiveValue(string body, string expected)
        => Assert.Equal(expected, Fn(body));

    // ── the nested-block shapes a previous attempt got wrong ──────────────────────────

    // NumericLocals is inherited by every nested block scope, and it names only body-top-level
    // declarations — so without a body-block test a nested `{ let v = ... }` of an eligible
    // NAME would take the function's slot. These are the exact reproductions item 3-3 records.
    [Theory]
    [InlineData("let v = 1; { let v = 2; return v; }", "2")]
    [InlineData("{ let v = 2; return v; }", "2")]
    [InlineData("let v = 1; { let w = 2; return w; }", "2")]
    [InlineData("let v = 1; { let v = 2; } return v;", "1")]
    [InlineData("const v = 1.5; { const v = 2.5; } return v;", "1.5")]
    [InlineData("let v = 1; if (true) { let v = 2; } return v;", "1")]
    [InlineData("let v = 1; { let v = 'x'; } return v;", "1")]
    [InlineData("let v = 1; { let v = v_outer_missing_guard(); } return v;", null)]
    public void ANestedBlockLexicalIsADistinctBinding(string body, string expected)
    {
        if (expected == null)
        {
            using var context = new JSContext();
            Assert.Throws<JSException>(() => context.Eval("(function(){ " + body + " })()"));
            return;
        }

        Assert.Equal(expected, Fn(body));
    }

    // A nested block's own dead zone is still a dead zone.
    [Fact(Timeout = 600000)]
    public void ANestedBlockLexicalKeepsItsOwnDeadZone()
    {
        using var context = new JSContext();
        Assert.Throws<JSException>(
            () => context.Eval("(function(){ let v = 1; { var r = v; let v = 2; return r; } })()"));
    }

    // A `for (let i ...)` head binds once per iteration, which a single raw double cannot
    // represent — the closures must see 0,1,2 rather than three copies of the last value.
    [Theory]
    [InlineData("var fs = []; for (let i = 0; i < 3; i++) { fs.push(function () { return i; }); } return fs[0]() + ',' + fs[1]() + ',' + fs[2]();", "0,1,2")]
    [InlineData("var fs = []; for (const x of [1, 2, 3]) { fs.push(function () { return x; }); } return fs[0]() + ',' + fs[2]();", "1,3")]
    [InlineData("var s = 0; for (let i = 0; i < 4; i++) { s += i; } return s;", "6")]
    public void ALoopHeadLexicalBindsPerIteration(string body, string expected)
        => Assert.Equal(expected, Fn(body));

    // The sharpest shadowing case, because both halves are numeric and so nothing about the
    // TYPE distinguishes them: an eligible body-top-level `let` and a `for`-head `let` of the
    // SAME name. The head's binding is per-iteration and lives in the loop's own scope; the
    // body's is the one that may take a raw double. Conflating them would return 3 (the loop's
    // final value) or 0 rather than 3.5.
    [Theory]
    [InlineData("let i = 3.5; var s = 0; for (let i = 0; i < 3; i++) { s = s + i; } return i;", "3.5")]
    [InlineData("let i = 3.5; var s = 0; for (let i = 0; i < 3; i++) { s = s + i; } return s;", "3")]
    [InlineData("const x = 1.5; var s = 0; for (const x of [1, 2, 3]) { s = s + x; } return x;", "1.5")]
    [InlineData("let v = 3.5; var s = 0; for (var i = 0; i < 3; i++) { let v = 2; s = s + v; } return v;", "3.5")]
    [InlineData("let v = 3.5; var s = 0; for (var i = 0; i < 3; i++) { let v = 2; s = s + v; } return s;", "6")]
    public void ALoopBindingShadowingAnEligibleNameStaysDistinct(string body, string expected)
        => Assert.Equal(expected, Fn(body));

    // ── the recorded cross-context reproduction ──────────────────────────────────────

    // Item 3-3's withdrawn attempt failed only AFTER an earlier compilation in the same
    // process, so a single evaluation is not an instrument that can see it. Every arm here
    // runs in a fresh JSContext behind a first compilation that specializes a lexical binding.
    [Fact(Timeout = 600000)]
    public void ALexicalBindingIsUnaffectedByAnEarlierCompilationInTheSameProcess()
    {
        var first = Eval("(function () { let v = 3.5; v = v + 1; return v; })()");
        Assert.Equal("4.5", first);

        Assert.Equal("2", Eval("(function () { let v = 1; { let v = 2; return v; } })()"));
        Assert.Equal("2", Eval("(function () { { let v = 2; return v; } })()"));
        Assert.Equal("2", Eval("(function () { let v = 1; { let w = 2; return w; } })()"));
        Assert.Equal("4.5", Eval("(function () { let v = 3.5; v = v + 1; return v; })()"));
        Assert.Equal("1.5", Eval("(function () { const v = 1.5; { const v = 2.5; } return v; })()"));
    }

    // The same, driven the other way round: a first compilation that does NOT specialize
    // anything, then one that does.
    [Fact(Timeout = 600000)]
    public void AnEarlierUnspecializedCompilationDoesNotDisturbTheNextOne()
    {
        Assert.Equal("x", Eval("(function () { let v = 'x'; return v; })()"));

        var (result, numericLocals) = Compile("let v = 3.5; var s = 0; for (var i = 0; i < 10; i++) { s = s + v * 2; } return s;");
        Assert.Equal("70", result);
        Assert.Equal(3, numericLocals);
    }

    // ── function kinds whose locals are not ordinary CLR locals ───────────────────────

    // A generator or async body is rewritten into a state machine, so what was a local becomes
    // a field that has to survive a suspension. Widening WHICH names are offered does not
    // change that mechanism, but a lexical binding in one of these had never been exercised,
    // and a value that does not survive a `yield` is a miscompile rather than a lost
    // optimization.
    [Theory]
    [InlineData("function* g() { let v = 3.5; yield v; yield v * 2; } var it = g(); return it.next().value + ',' + it.next().value;", "3.5,7")]
    [InlineData("function* g() { const v = 2; let s = 0; for (var i = 0; i < 3; i++) { s = s + v; yield s; } } var it = g(); it.next(); it.next(); return it.next().value;", "6")]
    [InlineData("function* g() { let v = 1; { let v = 2; yield v; } yield v; } var it = g(); return it.next().value + ',' + it.next().value;", "2,1")]
    public void AGeneratorBodyKeepsItsLexicalValuesAcrossASuspension(string body, string expected)
        => Assert.Equal(expected, Fn(body));

    // An arrow body is an ordinary function body for this purpose; a concise one has no block
    // at all, so `scope.Function?.Body` is not an AstBlock and the body-block test must simply
    // not match rather than misfire.
    [Theory]
    [InlineData("var f = () => { let v = 3.5; return v + 1; }; return f();", "4.5")]
    [InlineData("var f = () => { const v = 2; let s = 0; for (var i = 0; i < 3; i++) { s = s + v; } return s; }; return f();", "6")]
    [InlineData("var f = (a) => a * 2; let v = 3.5; return f(v);", "7")]
    public void AnArrowBodyBehavesTheSame(string body, string expected)
        => Assert.Equal(expected, Fn(body));

    // ── dynamic scope still shuts the whole function down ─────────────────────────────

    [Theory]
    [InlineData("let v = 3.5; eval('1'); return v;", "3.5")]
    [InlineData("let v = 3.5; var o = {}; with (o) { } return v;", "3.5")]
    [InlineData("let v = 3.5; return typeof v;", "number")]
    public void ANameADynamicScopeCanReachKeepsItsCell(string body, string expected)
    {
        var (result, numericLocals) = Compile(body);
        Assert.Equal(expected, result);
        Assert.Equal(0, numericLocals);
    }
}
