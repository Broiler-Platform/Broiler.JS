using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/721
//
// Fixed here:
//
//   Problem 1 (subset) — a `function` declaration nested directly inside the body
//   of a `for (let …)` / `for (let … of …)` / `for (let … in …)` loop that closes
//   over the loop variable threw a NullReferenceException (surfaced as
//   "Object reference not set to an instance of an object." at JSFunction
//   InvokeFunction) the moment the closure was instantiated.
//
//   The `for` desugarer rewrites a lexically-scoped loop into a synthetic
//   per-iteration block so each iteration gets a fresh copy of the binding. That
//   synthetic block was rebuilt from the original body's *statements* only, so the
//   body block's own hoisted bindings (the nested FunctionDeclaration) were dropped
//   from its HoistingScope. The declaration was therefore never hoisted into the
//   per-iteration block scope, and its closure over the loop variable captured an
//   uninitialised slot. The desugarer now merges the body block's HoistingScope
//   (and Annex-B function names) into the synthetic block.
//
//   sm/regress/regress-560998-1.js is the canonical reproduction.
//
//   Problem 3 (subset) — assigning to the immutable name binding of a named
//   generator or async function expression in strict mode silently did nothing
//   instead of throwing a TypeError. The read-only write only threw while the
//   engine's runtime strict-mode flag was active, but a generator / async body
//   resumes as a state machine outside that scope, so the throw was lost. The
//   name binding now bakes the function's strictness in at compile time.
//   (language/expressions/generators/named-strict-error-reassign-fn-name-in-body.js
//   and the function/async siblings.)
//
//   Problem 10 — CR (U+000D), LINE SEPARATOR (U+2028) and PARAGRAPH SEPARATOR
//   (U+2029) were not treated as LineTerminators by the scanner (only LF was), so
//   automatic semicolon insertion did not fire across them and `eval("var x =
//   asdf<CR>ghjk")` raised a SyntaxError instead of the expected ReferenceError
//   from the second statement. The scanner now recognises all four
//   LineTerminators in whitespace, line comments and block comments.
//   (language/types/string/S8.4_A7.2.js, A7.3, A7.4.)
public class Issue721Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext(experimentalFeatures: JavaScriptFeatureFlags.AllExperimentalEs2026);
        return ctx.Eval(code).ToString();
    }

    // Run `source` and drain the microtask queue, returning the settled value of
    // the final expression (used to observe async Promise settlement).
    private static string Execute(string code)
    {
        using var ctx = new JSContext(experimentalFeatures: JavaScriptFeatureFlags.AllExperimentalEs2026);
        return ctx.Execute(code).ToString();
    }

    // Run `source`, reporting the thrown error's constructor name or "ok".
    private static string Catch(string source)
        => Eval("var r; try { " + source + " r = 'ok'; } catch (e) { r = e.constructor.name; } r;");

    // ---- Problem 1: hoisted FunctionDeclaration capturing a for-let binding ----

    // The exact shape from sm/regress/regress-560998-1.js: no longer throws.
    [Fact]
    public void ForLetNestedFunctionDeclarationDoesNotThrow()
        => Assert.Equal("done", Eval("for (let j = 0; j < 4; ++j) { function g() { j; } g(); } 'done';"));

    // The closure observes the *current* iteration's value when called eagerly.
    [Fact]
    public void ForLetNestedFunctionDeclarationSeesCurrentIteration()
        => Assert.Equal("3", Eval("var s = 0; for (let j = 0; j < 3; ++j) { function g() { return j; } s += g(); } s;"));

    // Per-iteration binding semantics: each captured closure keeps its own copy.
    [Fact]
    public void ForLetNestedFunctionDeclarationCapturesPerIteration()
        => Assert.Equal(
            "0,1,2",
            Eval("var fns = []; for (let j = 0; j < 3; ++j) { function g() { return j; } fns.push(g); } fns[0]() + ',' + fns[1]() + ',' + fns[2]();"));

    // The declaration is still hoisted to the top of the per-iteration block.
    [Fact]
    public void ForLetNestedFunctionDeclarationIsHoisted()
        => Assert.Equal("function", Eval("var out = ''; for (let j = 0; j < 1; ++j) { out += typeof g; function g() { return j; } } out;"));

    // for-of with a body FunctionDeclaration closing over the iteration variable.
    [Fact]
    public void ForOfLetNestedFunctionDeclaration()
        => Assert.Equal("ab", Eval("var s = ''; for (let j of ['a', 'b']) { function g() { return j; } s += g(); } s;"));

    // for-in with a body FunctionDeclaration closing over the iteration variable.
    [Fact]
    public void ForInLetNestedFunctionDeclaration()
        => Assert.Equal("xy", Eval("var s = ''; for (let k in { x: 1, y: 1 }) { function g() { return k; } s += g(); } s;"));

    // Multiple loop bindings captured together.
    [Fact]
    public void ForLetMultipleBindingsCaptured()
        => Assert.Equal("22", Eval("var s = 0; for (let a = 0, b = 10; a < 2; ++a, ++b) { function g() { return a + b; } s += g(); } s;"));

    // A nested-within-nested function still resolves the loop binding.
    [Fact]
    public void ForLetDoublyNestedFunctionDeclaration()
        => Assert.Equal("1", Eval("var s = 0; for (let j = 0; j < 2; ++j) { function g() { function h() { return j; } return h(); } s += g(); } s;"));

    // Regression guard: a non-capturing nested declaration keeps working.
    [Fact]
    public void ForLetNonCapturingNestedFunctionDeclaration()
        => Assert.Equal("3", Eval("var s = 0; for (let j = 0; j < 3; ++j) { function g() { return 1; } s += g(); } s;"));

    // Regression guard: a classic `for (var …)` loop is unaffected.
    [Fact]
    public void ForVarNestedFunctionDeclaration()
        => Assert.Equal("3", Eval("var s = 0; for (var j = 0; j < 3; ++j) { function g() { return j; } s += g(); } s;"));

    // A static async private method invoked through a public wrapper settles
    // correctly (covers the async/private-name machinery exercised by the
    // class-elements samples in Problem 1).
    [Fact]
    public void StaticAsyncPrivateMethodSettles()
        => Assert.Equal(
            "1",
            Execute("class C { static async #m(v) { return await v; } static async run(v) { return await this.#m(v); } } C.run(1);"));

    // ---- Problem 3: strict-mode reassignment of a function-expression name ----

    // A named generator function expression: assigning to its own name throws.
    [Fact]
    public void StrictGeneratorNameReassignThrows()
        => Assert.Equal(
            "TypeError",
            Eval("'use strict'; var ref = function* BindingIdentifier() { BindingIdentifier = 1; yield; }; var r; try { ref().next(); r = 'no throw'; } catch (e) { r = e.constructor.name; } r;"));

    // A named async function expression behaves the same once awaited.
    [Fact]
    public void StrictAsyncNameReassignThrows()
        => Assert.Equal(
            "TypeError",
            Execute("'use strict'; var f = async function g() { g = 1; }; f().then(function () { return 'no throw'; }, function (e) { return e.constructor.name; });"));

    // Regression guard: the ordinary named function expression case still throws.
    [Fact]
    public void StrictFunctionNameReassignThrows()
        => Assert.Equal(
            "TypeError",
            Eval("'use strict'; var f = function g() { g = 1; }; var r; try { f(); r = 'no throw'; } catch (e) { r = e.constructor.name; } r;"));

    // In sloppy mode the assignment is silently ignored (no throw, no effect).
    [Fact]
    public void SloppyGeneratorNameReassignIsSilent()
        => Assert.Equal("function", Eval("var ref = function* g() { g = 1; yield typeof g; }; ref().next().value;"));

    [Fact]
    public void SloppyFunctionNameReassignIsSilent()
        => Assert.Equal("function", Eval("var f = function g() { g = 1; return typeof g; }; f();"));

    // ---- Problem 10: CR / LS / PS are LineTerminators (ASI) ----

    // CR (U+000D) between two statements triggers ASI, so the second statement
    // runs and resolving the undeclared `asdf` is a ReferenceError (not a parse
    // SyntaxError). Mirrors S8.4_A7.2.js.
    [Fact]
    public void CarriageReturnIsLineTerminatorForAsi()
        => Assert.Equal("ReferenceError", Catch("eval('var x = asdf\\u000Dghjk');"));

    // LINE SEPARATOR (U+2028). Mirrors S8.4_A7.3/A7.4.
    [Fact]
    public void LineSeparatorIsLineTerminatorForAsi()
        => Assert.Equal("ReferenceError", Catch("eval('var x = asdf\\u2028ghjk');"));

    // PARAGRAPH SEPARATOR (U+2029).
    [Fact]
    public void ParagraphSeparatorIsLineTerminatorForAsi()
        => Assert.Equal("ReferenceError", Catch("eval('var x = asdf\\u2029ghjk');"));

    // A CR-terminated single-line comment ends at the CR; the following code runs.
    [Fact]
    public void CarriageReturnEndsLineComment()
        => Assert.Equal("1", Eval("eval('// comment\\u000Dvar y = 1; y');"));

    // LF still works (regression guard).
    [Fact]
    public void LineFeedIsLineTerminatorForAsi()
        => Assert.Equal("ReferenceError", Catch("eval('var x = asdf\\u000Aghjk');"));
}
