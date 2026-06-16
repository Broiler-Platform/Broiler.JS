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

    // Problem 91 — the Decode abstract operation (§19.2.6.5) must reject a percent
    // sequence whose octets are not a valid UTF-8 encoding of a single Unicode code
    // point: an overlong form, a value above U+10FFFF, or a surrogate (e.g. %ED%BF%BF,
    // the CESU-8 encoding of U+DFFF). The default UTF-8 decoder substituted U+FFFD
    // instead of throwing, so no URIError was raised.

    [Fact]
    public void DecodeURIComponent_SurrogateEncoding_ThrowsURIError()
        => Assert.Equal("URIError", ErrorName("decodeURIComponent('%ED%BF%BF')"));

    [Fact]
    public void DecodeURIComponent_OverlongEncoding_ThrowsURIError()
        => Assert.Equal("URIError", ErrorName("decodeURIComponent('%C0%80')"));

    [Fact]
    public void DecodeURIComponent_AboveMaxCodePoint_ThrowsURIError()
        => Assert.Equal("URIError", ErrorName("decodeURIComponent('%F4%90%80%80')"));

    [Fact]
    public void DecodeURI_LeadingSurrogateEncoding_ThrowsURIError()
        => Assert.Equal("URIError", ErrorName("decodeURI('%ED%A0%80')"));

    [Fact]
    public void DecodeURIComponent_ValidMultiByte_StillDecodes()
        => Assert.Equal("€", Eval("decodeURIComponent('%E2%82%AC')"));

    [Fact]
    public void DecodeURIComponent_AstralCodePoint_StillDecodes()
        => Assert.Equal("true", Eval("'' + (decodeURIComponent('%F0%9F%98%80') === '\\u{1F600}')"));

    // Problems 88/99/100 — a chained `new` (`new new X(args)`) is `new (new X(args))`:
    // the inner NewExpression consumes the arguments and the outer argument-less `new`
    // is applied to its result. The parser dropped the outer `new`, so `new new X(args)`
    // evaluated to `new X(args)` and never raised the "not a constructor" TypeError for
    // the outer `new` on the (non-constructor) instance.

    [Fact]
    public void NewNewNumber_ThrowsTypeError()
        => Assert.Equal("TypeError", ErrorName("new new Number(1)"));

    [Fact]
    public void NewNewBoolean_ThrowsTypeError()
        => Assert.Equal("TypeError", ErrorName("new new Boolean(true)"));

    [Fact]
    public void NewNewString_NoParens_ThrowsTypeError()
        => Assert.Equal("TypeError", ErrorName("new new String"));

    [Fact]
    public void NewNewUserConstructor_ThrowsTypeError()
        => Assert.Equal("TypeError", ErrorName("function C(){}; new new C()"));

    [Fact]
    public void TripleNew_ThrowsTypeError()
        => Assert.Equal("TypeError", ErrorName("function C(){}; new new new C()"));

    [Fact]
    public void SingleNew_StillConstructs()
        => Assert.Equal("5", Eval("function C(v){this.v=v}; '' + new C(5).v"));
}
