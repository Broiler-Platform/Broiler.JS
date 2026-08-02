using System;
using Broiler.JavaScript.Compiler;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Compiler.Tests;

// A parameter no closure can reach and no `arguments` object can map is held in a plain
// JSValue local instead of a per-call JSVariable cell (docs/performance-roadmap.md item 3-3).
//
// Every parameter allocated a cell before this, unconditionally, while a `var` in the same
// function had been scalar-replaced since P2-2. A cell exists so something ELSE can reach the
// binding, so the tests are weighted to the four things that can: a closure, a mapped
// `arguments` object, a direct eval, and a `with`. A miss there is a miscompilation and not a
// lost optimization — the binding still holds the right value, but two views of it stop being
// the same binding, which is the failure mode that shows up as a stale read rather than a
// crash.
//
// The counter assertions are exact and are the ones that pin the gate: ScalarLocals is 0 for
// every parameter on the build before this item, so a test asserting 2 can only pass on the
// build under test. The behavioural half would pass either way, which is the point of it.
//
// Shares Phase 3's collection because CompilerSpecializationDiagnostics is a process-wide
// counter set.
[Collection(Phase3DiagnosticsCollection.Name)]
public class ScalarParameterTests
{
    private static string Eval(string source)
    {
        using var context = new JSContext();
        return context.Eval(source).ToString();
    }

    /// <summary>
    /// Compiles one function expression and reports how many of its bindings were scalar.
    /// </summary>
    /// <remarks>
    /// The source is a function EXPRESSION and is never invoked, so the count describes that
    /// function's own bindings and nothing the body would create at run time. The enclosing
    /// program declares nothing.
    /// </remarks>
    private static long ScalarLocalsOf(string functionSource)
    {
        using var context = new JSContext();
        CompilerSpecializationDiagnostics.Reset();
        context.Eval(functionSource);
        return CompilerSpecializationDiagnostics.Snapshot().ScalarLocals;
    }

    // ── the gate fires, and exactly where it should ───────────────────────────────────

    [Fact]
    public void SimpleParametersAreHeldWithoutACell()
        => Assert.Equal(2, ScalarLocalsOf("(function (a, b) { return a + b; })"));

    [Fact]
    public void AnArrowFunctionsParametersAreHeldWithoutACell()
        => Assert.Equal(2, ScalarLocalsOf("((a, b) => a + b)"));

    [Fact]
    public void AParameterANestedFunctionNamesKeepsItsCell()
    {
        // `b` is named by the closure and keeps its cell; `a` is not and does not. Capturing a
        // binding requires naming it, which is the whole rule.
        Assert.Equal(1, ScalarLocalsOf("(function (a, b) { return function () { return b; }; })"));
    }

    [Theory]
    // A mapped arguments object aliases the parameters through their cells.
    [InlineData("(function (a, b) { return arguments[0] + a + b; })")]
    // An arrow has no arguments of its own, so this materializes the OUTER function's.
    [InlineData("(function (a, b) { return (() => arguments[0])() + a + b; })")]
    [InlineData("(function (a, b) { var arguments; return a + b; })")]
    // A non-simple parameter list puts expressions in the parameter environment, and a closure
    // in one of them captures a parameter without the body ever naming it.
    [InlineData("(function (a, b = 1) { return a + b; })")]
    [InlineData("(function (a, ...rest) { return a + rest.length; })")]
    [InlineData("(function ({ a }, b) { return a + b; })")]
    // Reaches a binding it never names.
    [InlineData("(function (a, b) { return eval('a + b'); })")]
    [InlineData("(function (a, b) { with ({}) { return a + b; } } )")]
    [InlineData("(function (a, b) { debugger; return a + b; })")]
    // Suspends mid-frame; the binding outlives the activation in a way this has not analysed.
    [InlineData("(function* (a, b) { yield a + b; })")]
    [InlineData("(async function (a, b) { return a + b; })")]
    public void AReachableParameterKeepsItsCell(string functionSource)
        => Assert.Equal(0, ScalarLocalsOf(functionSource));

    // ── everything a cell exists for still works ──────────────────────────────────────

    [Theory]
    // A mapped arguments object aliases both ways: through the object, and back through it.
    [InlineData("(function (a) { return arguments[0] + ':' + a; })(3)", "3:3")]
    [InlineData("(function (a) { arguments[0] = 9; return a; })(3)", "9")]
    [InlineData("(function (a) { a = 11; return arguments[0]; })(3)", "11")]
    [InlineData("(function (a, b) { b = 2; return arguments.length + ':' + arguments[1]; })(1)", "1:undefined")]
    // Strict mode gets an UNMAPPED copy, so the two stop tracking each other.
    [InlineData("(function (a) { 'use strict'; a = 11; return arguments[0]; })(3)", "3")]
    // A closure over a parameter is one binding, not a copy of one.
    [InlineData("(function (a) { function g() { return a; } a = 2; return g(); })(1)", "2")]
    [InlineData("(function (a) { function g() { a = 5; } g(); return a; })(1)", "5")]
    [InlineData("(function (a) { return (() => { a++; return a; })() + ',' + a; })(1)", "2,2")]
    // A direct eval resolves names that appear nowhere in the compiled text.
    [InlineData("(function (a) { eval('a = 7'); return a; })(1)", "7")]
    [InlineData("(function (a) { return eval('a'); })(4)", "4")]
    // `with` can shadow a parameter, and must stop shadowing it at the closing brace.
    [InlineData("(function (a) { with ({ a: 9 }) { var inner = a; } return inner + ',' + a; })(1)", "9,1")]
    public void ABindingSomethingElseCanReachStillBehaves(string source, string expected)
        => Assert.Equal(expected, Eval(source));

    [Theory]
    // The last of two same-named parameters wins, and both name one binding.
    [InlineData("(function (a, a) { return a; })(1, 2)", "2")]
    [InlineData("(function (a, a) { a = 5; return arguments[0] + ',' + arguments[1]; })(1, 2)", "1,5")]
    // A var redeclaring a parameter keeps the argument rather than resetting to undefined.
    [InlineData("(function (a) { var a; return a; })(7)", "7")]
    [InlineData("(function (a) { var a = 3; return a; })(7)", "3")]
    // A body function declaration overrides a same-named parameter.
    [InlineData("(function (a) { return typeof a; function a() {} })(7)", "function")]
    // Arity mismatches in both directions.
    [InlineData("(function (a, b) { return a + ',' + b; })(1)", "1,undefined")]
    [InlineData("(function (a) { return a; })(1, 2, 3)", "1")]
    [InlineData("(function () { return 'none'; })(1)", "none")]
    // Writes of every shape, since the binding is now assigned rather than stored through.
    [InlineData("(function (a) { a = 5; return a; })(1)", "5")]
    [InlineData("(function (a) { return a++ + ',' + a; })(1)", "1,2")]
    [InlineData("(function (a) { return ++a + ',' + a; })(1)", "2,2")]
    [InlineData("(function (a) { a += 'x'; return a; })(1)", "1x")]
    [InlineData("(function (a) { a *= 3; return a; })(2)", "6")]
    [InlineData("(function (a) { a ??= 5; return a; })(undefined)", "5")]
    // The binding is a binding, not a property: typeof sees it and delete refuses it.
    [InlineData("(function (a) { return typeof a; })(1)", "number")]
    [InlineData("(function (a) { return typeof b; })(1)", "undefined")]
    [InlineData("(function (a) { return delete a; })(1)", "false")]
    // A per-call binding, so recursion must not share one.
    [InlineData("(function f(n) { return n <= 0 ? '' : n + f(n - 1); })(4)", "4321")]
    // Every value kind, since a scalar local is typed JSValue and not double.
    [InlineData("(function (a) { return typeof a; })('s')", "string")]
    [InlineData("(function (a) { return a.x; })({ x: 1 })", "1")]
    [InlineData("(function (a) { return typeof a; })(null)", "object")]
    [InlineData("(function (a) { return a; })(9007199254740993n)", "9007199254740993")]
    // Names that are bindings of their own elsewhere.
    [InlineData("(function (eval) { return eval; })(1)", "1")]
    public void AParameterWithNoCellStillBehavesLikeABinding(string source, string expected)
        => Assert.Equal(expected, Eval(source));

    /// <summary>
    /// A parameter named <c>undefined</c> does not shadow the global, and that is
    /// <em>pre-existing</em> — it reproduces identically with this item reverted.
    /// </summary>
    /// <remarks>
    /// Pinned rather than fixed so the deviation is recorded where the next person to widen
    /// parameter handling will meet it, and so that a fix flips a failing assertion instead of
    /// passing unnoticed. The expectation below is what the engine does, not what the spec
    /// says: §8.6.2 binds the parameter like any other, so both should answer <c>1</c> and
    /// <c>number</c>.
    /// </remarks>
    [Theory]
    [InlineData("(function (undefined) { return undefined; })(1)", "undefined")]
    [InlineData("(function (undefined) { return typeof undefined; })(1)", "undefined")]
    public void KnownGap_AParameterNamedUndefinedDoesNotShadowTheGlobal(string source, string engineAnswer)
        => Assert.Equal(engineAnswer, Eval(source));

    [Theory]
    // Class bodies are strict and their parameters take the same path.
    [InlineData("(function () { class C { constructor(v) { this.v = v; } m(x) { return this.v + x; } } return new C(1).m(2); })()", "3")]
    [InlineData("(function () { class C { set v(x) { this._v = x * 2; } get v() { return this._v; } } var c = new C(); c.v = 3; return c.v; })()", "6")]
    [InlineData("(function () { var o = { m(x) { return x + 1; } }; return o.m(1); })()", "2")]
    // A catch parameter is a different binding form and must be unaffected.
    [InlineData("(function (a) { try { throw 2; } catch (a) { return a; } })(1)", "2")]
    [InlineData("(function (a) { try { throw 2; } catch (e) { } return a; })(1)", "1")]
    // Closures created per iteration over a parameter each see the one shared binding.
    [InlineData("(function (a) { var f = []; for (var i = 0; i < 3; i++) f.push(function () { return a; }); a = 9; return f.map(function (g) { return g(); }).join(','); })(1)", "9,9,9")]
    public void OtherBindingFormsAreUnaffected(string source, string expected)
        => Assert.Equal(expected, Eval(source));

    // ── the item's own acceptance criterion ───────────────────────────────────────────

    [Fact]
    public void AParameterCostsNoAllocationPerCall()
    {
        // The point of the item, asserted as bytes rather than as a counter. Three parameters
        // cost three cells a call before this landed — measured at 56 bytes each — so 20 000
        // calls allocated ~3.4 MB of binding that nothing could observe.
        //
        // The bound sits between the two measured builds rather than near either: this loop
        // runs at 230.2 B/call with the item reverted and 62.2 B/call with it applied, both
        // taken from this very test, so 100 B/call fails by a factor of 2.3 before and passes
        // with 1.6x of headroom after. It is pinning the cells and not the calling convention
        // around them.
        using var context = new JSContext();
        var call = context.Eval(
            """
            (function (n) {
                function h(a, b, c) { return a + b + c; }
                var s = 0;
                for (var i = 0; i < n; i++) { s = s + h(1, 2, 3); }
                return s;
            })
            """);

        const int Calls = 20_000;
        var arguments = new Runtime.Arguments(Runtime.JSUndefined.Value, new BuiltIns.Number.JSNumber(Calls));

        // Warmed first: the compile, the inline-cache entries and the first-call work are
        // one-time and would otherwise land in the delta.
        call.InvokeFunction(in arguments);

        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);

        var before = GC.GetAllocatedBytesForCurrentThread();
        var result = call.InvokeFunction(in arguments);
        var bytes = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(120_000, result.IntValue);
        Assert.True(
            bytes < 100 * Calls,
            $"expected under {100 * Calls} bytes for {Calls} three-parameter calls, got {bytes} "
                + $"({(double)bytes / Calls:F1} B/call); three cells a call would be ~168 B/call");
    }
}
