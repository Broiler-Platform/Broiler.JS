using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/812
//
// Fixed here (Problem 37 — `async of` at the head of a C-style for loop):
//
//   `for (async of => {}; …; …)` begins with the async arrow function
//   `async of => {}` (a single parameter named `of`), so it is the *init*
//   clause of a C-style for, not a for-of head. The grammar only forbids the
//   bare `async of <iterable>` sequence as the left-hand side of a *sync*
//   for-of (it is permitted in `for await`, where `async` is the loop target).
//   The parser intercepted every `async of` and reported "'async' is not
//   allowed as the left-hand side of a for-of loop", rejecting the valid arrow
//   init. It now peeks past `of` for the `=>` and leaves the arrow alone.
public class Issue812Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code)?.ToString();
    }

    private static string ErrorName(string body) => Eval(
        "let t='NONE'; try { " + body + " } catch (e) { t = e.constructor.name; } t");

    [Fact]
    public void AsyncOfArrowIsForInitNotForOf()
        => Assert.Equal("3", Eval(
            "let n = 0; for (async of => {}; n < 3; ) { n++; } '' + n"));

    [Fact]
    public void AsyncOfArrowInitRunsOnce()
        => Assert.Equal("ok", Eval(
            "let r = 'no'; for (let f = (async of => {}); ; ) { r = 'ok'; break; } r"));

    [Fact]
    public void BareAsyncOfForOfIsSyntaxError()
        => Assert.Equal("SyntaxError", ErrorName("eval('for (async of [1, 2, 3]) { }')"));

    // Problem 59 — the braced Unicode code-point escape `\u{H..H}` (valid only in
    // u/v mode). A `/…/u` regex *literal* has it rewritten to a fixed-width `\uHHHH`
    // escape by the source scanner, but a `new RegExp(string, "u")` pattern reached
    // the translator with the brace form intact, which .NET rejected with
    // "Invalid pattern '[\u{0}]' … Insufficient or invalid hexadecimal digits."

    [Fact]
    public void RegExpStringBracedEscape_BmpInClass_Matches()
        => Assert.Equal("true", Eval("'' + new RegExp('[\\\\u{0}]', 'u').test('\\u0000')"));

    [Fact]
    public void RegExpStringBracedEscape_BmpInClass_DoesNotMatchOther()
        => Assert.Equal("false", Eval("'' + new RegExp('[\\\\u{0}]', 'u').test('a')"));

    [Fact]
    public void RegExpStringBracedEscape_AstralInClass_MatchesWholeCodePoint()
        => Assert.Equal("true", Eval("'' + new RegExp('[\\\\u{1F600}]', 'u').test('\\u{1F600}')"));

    [Fact]
    public void RegExpStringBracedEscape_Atom_Matches()
        => Assert.Equal("true", Eval("'' + new RegExp('\\\\u{41}', 'u').test('A')"));

    [Fact]
    public void RegExpStringBracedEscape_MetacharacterStaysLiteral()
        => Assert.Equal("true", Eval("'' + new RegExp('\\\\u{3f}', 'u').test('?')"));
}
