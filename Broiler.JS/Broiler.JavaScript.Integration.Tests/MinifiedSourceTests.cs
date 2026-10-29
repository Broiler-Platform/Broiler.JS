using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for engine bugs that only a MINIFIED program reaches. Each was found by a
// test262 minifier variant — every case below passes as written and failed only after Terser
// or Closure rewrote it — but none of them is really about minification: they are ordinary
// engine defects that hand-written source happens not to walk into, and minified code on
// the web walks into constantly.
//
//   * U+FFFF was the scanner's end-of-input sentinel, so a literal U+FFFF in a string,
//     template, regular expression or comment ended the program there. A minifier with
//     `ascii_only` off writes the character itself rather than the `￿` escape the
//     source used.
//   * A `/` after a BigInt, string, template, regular-expression, `null`/`true`/`false` or
//     `++`/`--` token was scanned as the start of a regular expression. The speculative
//     scan fails and falls back to division in spaced-out source, but a minified line has
//     a later `/` for it to close on — often one inside a string — and it swallowed
//     everything in between.
//   * `.5.toFixed(1)`: a leading-dot numeric literal consumed the member-access dot as a
//     second decimal point. `(0.5).toFixed(1)` is how it is written; that is how it is
//     minified.
//   * `catch (e) {} finally { e }`: the catch parameter's scope was still open while the
//     finally block compiled, so a name shared with it bound to the catch handler's local
//     and IL generation failed outright.
//   * `#in`, `#instanceof`, `#null`, `#true`, `#false`: a PrivateIdentifier is `#`
//     IdentifierName, so every reserved word is a legal private name. A mangler names
//     private fields from the same short alphabet as everything else.
//   * `for (const x of xs) { const x = …; }`: the loop body's Block is its own scope
//     nested inside the per-iteration environment, so a body binding may shadow the head's.
//     Head and body are renamed independently by a mangler and collide.
//   * `const n = 3; for (let r = 1; r <= n; ++r) { const n = 1; }`: a `let` head evaluates its
//     test inside the per-iteration block, which the body shared, so the body's `n` captured
//     the test's. The two names come from one short alphabet once a mangler has been through.
//   * `throw a, b, c`: the parser took an AssignmentExpression where the grammar says
//     Expression, so the first operand was thrown and the rest never evaluated. Folding
//     statements into a comma sequence is the first thing a minifier does.
//   * `false || (yield i)` inside a loop: a suspension inside a short-circuit operator put the
//     resume label in the middle of the operator's own branch, which is not valid IL. A
//     compiler writes `f(x) || (yield x)` where the source wrote an `if`.
//   * `class D extends B {}` re-parented onto a non-constructor: the default derived
//     constructor called it anyway. Closure drops a `constructor(){ super(); }` that does
//     nothing else, which is what moved the test onto the synthetic constructor's path.
public class MinifiedSourceTests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // ---- U+FFFF is content, not end of input ----

    private const string FFFF = "￿";

    [Fact]
    public void NonCharacterInsideStringLiteralIsOrdinaryContent()
    {
        Assert.Equal("3 true", Eval(
            $"var s = \"A{FFFF}B\"; `${{s.length}} ${{s.charCodeAt(1) === 0xFFFF}}`"));
    }

    [Fact]
    public void NonCharacterInsideTemplateLiteralIsOrdinaryContent()
    {
        Assert.Equal("3 true", Eval(
            $"var s = `A{FFFF}B`; `${{s.length}} ${{s.charCodeAt(1) === 0xFFFF}}`"));
    }

    [Fact]
    public void NonCharacterInsideRegExpLiteralIsOrdinaryContent()
    {
        Assert.Equal("true 3", Eval(
            $"var r = /a{FFFF}b/; `${{r.test(\"a{FFFF}b\")}} ${{r.source.length}}`"));
    }

    [Fact]
    public void NonCharacterInsideCommentsDoesNotEndTheProgram()
    {
        Assert.Equal("ok", Eval($"// comment {FFFF} continues\n\"ok\""));
        Assert.Equal("ok", Eval($"/* block {FFFF} comment */ \"ok\""));
    }

    [Fact]
    public void StrayNonCharacterIsReportedRatherThanTruncatingTheProgram()
    {
        // Silently returning EOF here dropped every statement after it — the failure mode
        // a sentinel that collides with real text produces when it is NOT inside a literal.
        var error = Assert.ThrowsAny<Exception>(() => Eval($"var a = 1; {FFFF} var b = 2;"));
        Assert.Contains("U+FFFF", error.Message);
    }

    [Fact]
    public void UnterminatedStringIsStillAnError()
        => Assert.ThrowsAny<Exception>(() => Eval("var s = \"unterminated"));

    // ---- `/` after a token that ends an operand is division ----

    [Theory]
    [InlineData("var f = function () { 1n / 1 }, g = /x/; \"ok\"")]
    [InlineData("var f = function () { \"a\" / 1 }, g = /x/; \"ok\"")]
    [InlineData("var f = function () { `a` / 1 }, g = /x/; \"ok\"")]
    [InlineData("var f = function () { /a/g / 1 }, g = /x/; \"ok\"")]
    [InlineData("var f = function () { a++ / 1 }, g = /x/; \"ok\"")]
    [InlineData("var f = function () { null / 1 }, g = /x/; \"ok\"")]
    [InlineData("var f = function () { true / 1 }, g = /x/; \"ok\"")]
    [InlineData("var f = function () { 1 / 1 }, g = /x/; \"ok\"")]
    public void DivisionAfterAnOperandIsNotMistakenForARegExp(string code)
        // Each of these has a later `/` for a mis-scanned regular expression to close on,
        // which is what makes the minified form fail where the spaced-out source does not.
        => Assert.Equal("ok", Eval(code));

    [Theory]
    [InlineData("/a/.test(\"a\")", "true")]
    [InlineData("typeof /x/", "object")]
    [InlineData("(function () { if (1) return /a/.source; })()", "a")]
    [InlineData("4 / 2 / 1", "2")]
    [InlineData("var yield = 4; yield /2/ 1", "2")]
    public void RegExpLiteralsStillScanWhereAnExpressionMayBegin(string code, string expected)
        => Assert.Equal(expected, Eval(code));

    // ---- a leading-dot literal has already consumed its fraction ----

    [Theory]
    [InlineData(".5.toFixed(1)", "0.5")]
    [InlineData(".9999.toExponential(0)", "1e+0")]
    [InlineData("1.5.toFixed(2)", "1.50")]
    [InlineData("1..toFixed(1)", "1.0")]
    [InlineData(".5 + .5", "1")]
    [InlineData(".5e1", "5")]
    public void MemberAccessOnALeadingDotNumberParses(string code, string expected)
        => Assert.Equal(expected, Eval(code));

    // ---- the catch parameter's scope ends with the catch block ----

    [Fact]
    public void FinallyBlockSeesTheOuterBindingNotTheCatchParameter()
    {
        // `e` in the finally is the function's `var e`; the catch parameter shadows it only
        // inside the catch block. Compiling the finally with the catch scope still open made
        // this a hard IL-generation failure ("references the undeclared parameter 'e'").
        Assert.Equal("1", Eval(
            "(function () { var e = 1; try { throw 2; } catch (e) {} finally { return e; } })()"));
    }

    [Fact]
    public void CatchParameterStillShadowsInsideTheCatchBlock()
    {
        Assert.Equal("2 1", Eval(
            "(function () { var e = 1, seen; try { throw 2; } catch (e) { seen = e; } "
            + "finally { return `${seen} ${e}`; } })()"));
    }

    [Fact]
    public void DestructuredCatchBindingDoesNotLeakIntoTheFinallyBlock()
    {
        Assert.Equal("7 1", Eval(
            "(function () { var e = 1, seen; try { throw [7]; } catch ([e]) { seen = e; } "
            + "finally { return `${seen} ${e}`; } })()"));
    }

    // ---- a private name is `#` IdentifierName, reserved words included ----

    [Theory]
    [InlineData("class C { #in = 1; m() { return this.#in; } } new C().m()", "1")]
    [InlineData("class C { #instanceof = 2; m() { return this.#instanceof; } } new C().m()", "2")]
    [InlineData("class C { #null = 3; m() { return this.#null; } } new C().m()", "3")]
    [InlineData("class C { #true = 4; m() { return this.#true; } } new C().m()", "4")]
    [InlineData("class C { #false = 5; m() { return this.#false; } } new C().m()", "5")]
    [InlineData("class C { #in() { return 6; } m() { return this.#in(); } } new C().m()", "6")]
    [InlineData("class C { get #in() { return 7; } m() { return this.#in; } } new C().m()", "7")]
    [InlineData("class C { #class = 8; m() { return this.#class; } } new C().m()", "8")]
    public void ReservedWordsAreValidPrivateNames(string code, string expected)
        => Assert.Equal(expected, Eval(code));

    [Fact]
    public void BrandCheckWorksForAReservedWordPrivateName()
    {
        Assert.Equal("true false", Eval(
            "class C { #in = 1; static has(o) { return #in in o; } } "
            + "`${C.has(new C())} ${C.has({})}`"));
    }

    // ---- the loop body's block is its own scope ----

    [Fact]
    public void ForOfBodyMayShadowAConstHeadBinding()
    {
        // The body's initializer used to assign the head's const binding instead of
        // declaring its own, throwing "Cannot assign to read only variable".
        Assert.Equal("5,5", Eval(
            "var r = []; for (const x of [1, 2]) { const x = 5; r.push(x); } r.join(\",\")"));
    }

    [Fact]
    public void ForInBodyMayShadowAConstHeadBinding()
    {
        Assert.Equal("5", Eval(
            "var r = []; for (const k in { a: 1 }) { const k = 5; r.push(k); } r.join(\",\")"));
    }

    [Fact]
    public void ForOfDestructuringHeadMayBeShadowedByTheBody()
    {
        Assert.Equal("1:9,2:9", Eval(
            "var r = []; for (const [a, b = a] of [[1], [2]]) { const b = 9; r.push(`${a}:${b}`); } "
            + "r.join(\",\")"));
    }

    [Fact]
    public void CStyleLoopStillAdvancesWhenTheBodyShadowsTheLoopVariable()
    {
        // The body's shadowing binding was being copied back into the per-iteration carrier,
        // so the loop variable became 9 and the loop ran once instead of twice.
        Assert.Equal("9,9", Eval(
            "var r = []; for (let i = 0; i < 2; i++) { let i = 9; r.push(i); } r.join(\",\")"));
    }

    [Fact]
    public void PerIterationBindingsSurviveAShadowedBody()
        => Assert.Equal("1,2,3", Eval(
            "var f = []; for (const x of [1, 2, 3]) { const y = x; f.push(() => y); } "
            + "f.map(g => g()).join(\",\")"));

    [Fact]
    public void FunctionDeclarationsAreStillHoistedInsideAShadowedBody()
    {
        // Giving the body its own scope must not strand its hoisted names: a function
        // declared anywhere in the block is still initialized at the top of it, and Annex B
        // still lifts one out of a nested block.
        Assert.Equal("function:2", Eval(
            "var out; for (const g of [1]) { const g = 2; out = `${typeof h}:${g}`; "
            + "function h() {} } out"));
        Assert.Equal("function", Eval(
            "for (const q of [1]) { const q = 2; { function w() {} } } typeof w"));
        Assert.Equal("function:3", Eval(
            "var out; for (let i = 0; i < 1; i++) { let i = 3; out = `${typeof z}:${i}`; "
            + "function z() {} } out"));
    }

    [Fact]
    public void VarAndControlFlowAreUnaffectedByAShadowedBody()
    {
        // `var` still reaches the enclosing function, and `continue` — plain and labelled —
        // still targets the loop rather than the body's new block.
        Assert.Equal("10/5", Eval(
            "(function () { var s = 0, v; for (const x of [1, 2]) { const x = 5; var v = x; "
            + "s += v; } return `${s}/${v}`; })()"));
        Assert.Equal("3", Eval(
            "var n = 0; for (const x of [1, 2, 3]) { const x = 0; n++; if (n === 2) continue; } n"));
        Assert.Equal("2", Eval(
            "var n = 0; outer: for (const x of [1, 2]) { const x = 0; "
            + "for (const y of [1, 2]) { n++; continue outer; } } n"));
    }

    [Fact]
    public void HeadBindingIsStillInTheTemporalDeadZoneOfAShadowingBody()
        // The body's own `x` shadows the head's for the WHOLE block, so reading it before
        // the declaration is a ReferenceError rather than a peek at the head's value.
        => Assert.Throws<JSException>(() => Eval("for (const x of [1]) { x; const x = 1; }"));

    [Fact]
    public void LoopBodyWithoutAShadowingBindingIsUnchanged()
    {
        Assert.Equal("3/2", Eval(
            "(function () { var s = 0, v; for (const x of [1, 2]) { var v = x; s += v; } "
            + "return `${s}/${v}`; })()"));
        Assert.Equal("function", Eval(
            "var t; for (const g of [1]) { t = typeof g2; function g2() {} } t"));
    }

    // ---- a `let` head reads the enclosing scope, not the body's bindings ----

    [Fact]
    public void LoopTestReadsTheEnclosingBindingAShadowingBodyHides()
    {
        // A `let` head evaluates its test against each iteration's own binding, which the
        // lowering achieves by moving the test INTO the per-iteration block. The body's
        // statements share that block, so a body-level `const n` captured the test's `n`
        // too — and the first evaluation read it one iteration before it exists:
        // "Cannot access 'n' before initialization", thrown before the body had run once.
        Assert.Equal("3", Eval(
            "const n = 3; var c = 0; for (let r = 1; r <= n; ++r) { const n = 1; c++; } c"));
        Assert.Equal("3", Eval(
            "const n = 3; var c = 0; for (let r = 1; r <= n; ++r) { let n = 1; c++; } c"));
        Assert.Equal("3", Eval(
            "const n = 3; var c = 0; for (let r = 1; r <= n; ++r) { class n {} c++; } c"));
    }

    [Fact]
    public void LoopUpdateReadsTheEnclosingBindingAShadowingBodyHides()
        // The update is moved into the same block when it carries a closure, so it needs the
        // same separation: `step` here is the outer 1, never the body's 99.
        => Assert.Equal("3", Eval(
            "const step = 1; var c = 0, fs = []; "
            + "for (let r = 0; r < 3; r += step) { const step = 99; fs.push(() => r); c++; } c"));

    [Fact]
    public void ShadowedBodyKeepsPerIterationBindingsAndControlFlow()
    {
        // Giving the body its own scope must not disturb what the per-iteration lowering is
        // there for: a closure still captures that iteration's binding, `continue` still
        // reaches the update, and `break` still leaves.
        Assert.Equal("0,1,2", Eval(
            "const n = 3; var fs = []; for (let i = 0; i < n; i++) { const n = 9; fs.push(() => i); } "
            + "fs.map(f => f()).join(\",\")"));
        Assert.Equal("3", Eval(
            "const n = 4; var c = 0; for (let r = 0; r < n; ++r) { const n = 1; "
            + "if (r === 1) continue; c++; } c"));
        Assert.Equal("2", Eval(
            "const n = 4; var c = 0; for (let r = 0; r < n; ++r) { const n = 1; "
            + "if (r === 2) break; c++; } c"));
    }

    [Fact]
    public void ShadowedBodyDoesNotHideTheNameFromItself()
        // The body's own binding is still the one the body sees, for the whole block: reading
        // it before its declaration is its temporal dead zone, not a peek at the outer value.
        => Assert.Throws<JSException>(() => Eval(
            "const n = 3; for (let r = 1; r <= n; ++r) { n; const n = 1; }"));

    // ---- `throw` takes an Expression, comma operator included ----

    [Fact]
    public void ThrowThrowsTheLastOperandOfASequence()
    {
        // `ThrowStatement : throw [no LineTerminator here] Expression ;` — the parser read an
        // AssignmentExpression instead, so the FIRST operand was thrown and the rest never ran.
        // Statements a minifier folds into the throw are exactly this shape: `x = 1;
        // throw "ex1";` becomes `throw x = 1, "ex1";`, which threw 1.
        Assert.Equal("3", Eval("var e; try { throw 1, 2, 3; } catch (x) { e = x; } '' + e"));
        Assert.Equal("ex1", Eval(
            "var e, x = 0; try { throw x = 1, 'ex1'; } catch (c) { e = c; } '' + e"));
    }

    [Fact]
    public void ThrowEvaluatesEveryOperandOfASequence()
        // Every operand runs, in order, before the last one is thrown.
        => Assert.Equal("ab/y", Eval(
            "var s = '', e; try { throw (s += 'a', 'x'), (s += 'b', 'y'); } catch (c) { e = c; } "
            + "s + '/' + e"));

    [Fact]
    public void ASequenceOperandThatThrowsIsCaughtByTheEnclosingTry()
        // The operand is evaluated inside the try, so a conversion that throws on the way to
        // the thrown value is that try's exception rather than an escape.
        => Assert.Equal("error", Eval(
            "var e; try { throw 1, ~{ valueOf: function () { throw 'error'; } }, 'unreached'; } "
            + "catch (c) { e = c; } '' + e"));

    // ---- a suspension inside a short-circuit operator, inside a loop ----

    [Fact]
    public void YieldInsideAShortCircuitOperatorInALoop()
    {
        // `a || b`, `a && b` and `a ?? b` all lower to one Coalesce node, which emits
        // `left; dup; brtrue end; pop; right; end:`. A yield in either operand lowers to
        // `return state; <label>; value`, and the resume goto then landed in the middle of
        // that sequence — invalid IL, which faulted as a NullReferenceException on the first
        // `next()`. Outside a loop the same source was fine, so only a minified body — which
        // writes `f(x) || (yield x)` where the source wrote an `if` — met it.
        Assert.Equal("0,1", Eval(
            "function* g() { for (var i = 0; i < 2; i++) { false || (yield i); } } [...g()].join()"));
        Assert.Equal("0,1", Eval(
            "function* g() { for (var i = 0; i < 2; i++) { true && (yield i); } } [...g()].join()"));
        Assert.Equal("0,1", Eval(
            "function* g() { for (var i = 0; i < 2; i++) { null ?? (yield i); } } [...g()].join()"));
        Assert.Equal("0,1", Eval(
            "function* g() { var i = 0; while (i < 2) { false || (yield i); i++; } } [...g()].join()"));
        // …and with the suspension on the left of the operator.
        Assert.Equal("0,1", Eval(
            "function* g() { for (var i = 0; i < 2; i++) { (yield i) || false; } } [...g()].join()"));
    }

    [Fact]
    public void ShortCircuitingStillShortCircuitsAroundAYield()
    {
        // The rewrite must not turn a conditional evaluation into an unconditional one: the
        // right operand runs only when the left does not decide the value, and the value the
        // whole expression produces is still the operand that decided it.
        Assert.Equal("y0,y2", Eval(
            "function* g() { for (var i = 0; i < 3; i++) { (i % 2) || (yield 'y' + i); } } "
            + "[...g()].join()"));
        Assert.Equal("sent,fallback", Eval(
            "function* g() { var out = []; for (var i = 0; i < 2; i++) { "
            + "out.push((yield i) || 'fallback'); } return out.join(); } "
            + "var it = g(); it.next(); it.next('sent'); it.next(0).value"));
    }

    // ---- a class that writes no constructor still checks its super ----

    [Fact]
    public void DefaultDerivedConstructorRejectsANonConstructorSuper()
    {
        // The synthetic `constructor(...args) { super(...args) }` [[Construct]]s whatever
        // GetSuperConstructor resolves to, so a class re-parented onto a callable that is not
        // a constructor must throw — as it already did when the class wrote that constructor
        // out. A minifier drops the trivial constructor, which is how the two paths came to
        // disagree.
        Assert.Equal("TypeError", Eval(
            "class B { constructor() {} } class D extends B {} "
            + "Object.setPrototypeOf(D, Math.sin); "
            + "var n = 'no-throw'; try { new D(); } catch (e) { n = e.constructor.name; } n"));
        Assert.Equal("TypeError", Eval(
            "class B { constructor() {} } class D extends B {} Object.setPrototypeOf(D, null); "
            + "var n = 'no-throw'; try { new D(); } catch (e) { n = e.constructor.name; } n"));
    }

    [Fact]
    public void DefaultDerivedConstructorStillConstructsEveryRealSuper()
    {
        // The check is about IsConstructor, not about the shape of the super: an ordinary
        // class, a Proxy and a bound function all still construct. (The bound function was
        // taking the delegate fast path, which never reaches the target's [[Construct]] and
        // left the instance uninitialised.)
        Assert.Equal("1", Eval(
            "class B { constructor() { this.b = 1; } } class D extends B {} '' + new D().b"));
        Assert.Equal("1", Eval(
            "class B { constructor() { this.b = 1; } } class D extends B {} "
            + "Object.setPrototypeOf(D, new Proxy(B, {})); '' + new D().b"));
        Assert.Equal("1", Eval(
            "class B { constructor() { this.b = 1; } } class D extends B {} "
            + "Object.setPrototypeOf(D, B.bind(null)); '' + new D().b"));
        Assert.Equal("3", Eval("class D extends Array {} '' + new D(1, 2, 3).length"));
        // A base class has no super to call, so re-parenting it changes nothing.
        Assert.Equal("object", Eval(
            "class B {} Object.setPrototypeOf(B, Math.sin); typeof new B()"));
    }
}
