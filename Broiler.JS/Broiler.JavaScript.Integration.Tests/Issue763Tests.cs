using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/763 problem 6.
//
// A `yield` reachable through a branch inside a `for` loop with an update clause
// (e.g. `for (...; ...; i++) { if (cond) yield x; }`) crashed at runtime with a
// NullReferenceException surfaced as "Object reference not set to an instance of
// an object." The completion-tracking wrapper turned the conditional body into a
// value-producing expression (`#cv = if (cond) yield x`), so the generator state
// machine nested the yield's suspend/resume inside an assignment RHS. The resume
// `goto` then jumped into the middle of evaluating that RHS, skipping the
// assignment-target setup and corrupting the IL stack. GeneratorRewriter now
// spills a value-producing yield-bearing conditional into a temp and distributes
// the production into each branch, keeping every yield at statement level (where
// FlattenBlocks already hoists it safely).
public class Issue763Tests
{
    private static string Drive(string body)
    {
        using var ctx = new JSContext();
        ctx.Eval("globalThis.r = '<unset>';");
        ctx.Execute(body);
        return ctx.Eval("'' + globalThis.r").ToString();
    }

    [Fact]
    public void ConditionalYieldInForLoopDoesNotCrash()
        => Assert.Equal("0,1,2", Drive(
            "function* g(){ for (var i = 0; i < 3; i++) { if (i >= 0) yield i; } }"
            + " var s = []; for (var x of g()) s.push(x); globalThis.r = s.join(',');"));

    [Fact]
    public void ConditionalYieldWithElseInForLoop()
        => Assert.Equal("a,b,a", Drive(
            "function* g(){ for (var i = 0; i < 3; i++) { if (i === 1) yield 'b'; else yield 'a'; } }"
            + " var s = []; for (var x of g()) s.push(x); globalThis.r = s.join(',');"));

    [Fact]
    public void TernaryYieldInForLoop()
        => Assert.Equal("0,1,2", Drive(
            "function* g(){ for (var i = 0; i < 3; i++) { var t = (i > 0) ? yield i : yield i; } }"
            + " var s = []; var it = g(); var r = it.next();"
            + " while (!r.done) { s.push(r.value); r = it.next(); } globalThis.r = s.join(',');"));

    [Fact]
    public void SwitchYieldInForLoop()
        => Assert.Equal("0,one,2", Drive(
            "function* g(){ for (var i = 0; i < 3; i++) { switch (i) { case 1: yield 'one'; break; default: yield i; } } }"
            + " var s = []; for (var x of g()) s.push(x); globalThis.r = s.join(',');"));

    [Fact]
    public void InvalidControlEscapeRegExpFallsBackToLiteral()
        // annexB/built-ins/RegExp/RegExp-control-escape-russian-letter.js shape:
        // `\c` + invalid control letter must match the literal `\c`, driven from a
        // generator whose for-loop body yields conditionally.
        => Assert.Equal("ok", Drive(
            "function* invalid(){ for (var a = 0; a <= 0x7F; a++) { let l = String.fromCharCode(a);"
            + " if (!l.match(/[0-9A-Za-z_\\$(|)\\[\\]\\/\\\\^]/)) yield l; } yield ''; }"
            + " var bad = false;"
            + " for (let l of invalid()) { var src = '\\\\c' + l; var re = new RegExp(src);"
            + " if (re.exec(src) === null) bad = true; if (re.exec(src.substring(1)) !== null) bad = true; }"
            + " globalThis.r = bad ? 'fail' : 'ok';"));

    [Fact]
    public void AsyncGeneratorConditionalYieldInForLoop()
        => Assert.Equal("0,10,20", Drive(
            "async function* g(){ for (var i = 0; i < 3; i++) { if (i >= 0) yield i * 10; } }"
            + " (async () => { var s = []; for await (var x of g()) s.push(x);"
            + " globalThis.r = s.join(','); })();"));

    // Problem 12: a unicode-escaped `await` used as an async-arrow binding
    // identifier (`async await => 1`) must be a SyntaxError, exactly like the
    // unescaped `async await => 1`. The escape strips the keyword identity, so the
    // scanner produced a plain identifier that slipped past the `await` keyword
    // rejection. Escaped `async`, and escaped `await`/`async` used as ordinary
    // identifiers outside async code, remain valid.
    [Theory]
    [InlineData("async aw\\u0061it => 1;")]
    [InlineData("async await => 1;")]
    public void EscapedAwaitAsAsyncArrowParamIsSyntaxError(string code)
        => Assert.Equal("SyntaxError", Drive(
            "try { eval(" + System.Text.Json.JsonSerializer.Serialize(code)
            + "); globalThis.r = 'no-throw'; }"
            + " catch (e) { globalThis.r = e.constructor.name; }"));

    [Theory]
    [InlineData("var \\u0061sync = 7; globalThis.r = '' + \\u0061sync;", "7")]
    [InlineData("var o = { \\u0061sync() { return 3; } }; globalThis.r = '' + o.async();", "3")]
    [InlineData("async function f() { return 1; } globalThis.r = 'ok';", "ok")]
    public void EscapedContextualKeywordsStillValid(string code, string expected)
        => Assert.Equal(expected, Drive(code));
}
