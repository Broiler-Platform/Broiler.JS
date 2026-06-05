using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/669
//
// Fixed here:
//   * Problem 7  — Intl constructors (NumberFormat / DateTimeFormat /
//                  RelativeTimeFormat / Locale) coerce a defined non-object
//                  `options` argument with ToObject instead of rejecting it.
//                  Primitives box into wrapper objects; only null/undefined are
//                  special (undefined → defaults, null → TypeError).
//   * Problem 8  — a computed member CALL whose key is a non-string/number literal
//                  (e.g. `obj[null]()`) threw NotImplementedException in the
//                  compiler. The read path already evaluated such a literal and
//                  coerced it to a property key; the call path now does too. This
//                  is what `class C { [null]() {} }; new C()[null]()` exercises.
//   * Problem 9  — three switch fixes: (a) a `case` whose test is a
//                  non-string/number/boolean literal (e.g. `case null:`) threw
//                  NotImplementedException; it now compiles and compares via
//                  strict-equals like any expression case. (b) the mixed/object
//                  case path used loose `==` instead of `===`. (c) a `default:`
//                  clause that was NOT the last clause was dropped by the parser
//                  (its statements merged into the following case) and the
//                  compiler always treated default as last; the parser now flushes
//                  a pending default and the switch is lowered to a dispatch +
//                  textual-order bodies so default-in-the-middle falls through
//                  correctly.
//   * Problem 10 — a template-literal substitution containing plain `{ }` braces
//                  (object literal, function body, block) mis-parsed: the scanner's
//                  flat `templateParts` counter treated the first inner `}` as the
//                  substitution terminator (`` `${function(){ ... }()}` `` →
//                  "Unexpected token )"). Brace depth is now tracked per
//                  substitution so only the matching `}` closes it.
//
// Problems 1 (sm deepEqual harness), 2 (dstr array-rest IteratorClose + Locale
// canonicalize), 3 (Intl.DateTimeFormat formatRange — needs CLDR), 4 (abrupt
// completion in `finally` overriding a pending throw — architectural IL layer),
// 5 (compound-assignment PutValue ordering with a direct-eval var binding) and 6
// (several unrelated root causes grouped by message) are triaged in the issue and
// remain out of scope.
public class Issue669Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // ---- Problem 10: template substitution with inner braces ----

    [Fact]
    public void TemplateSubstitutionWithFunctionBodyBraces()
        => Assert.Equal("caught", Eval(
            "(function(){ try { `${function(){ throw 1; }()}`; return 'no-throw'; }"
            + " catch(e){ return 'caught'; } })()"));

    [Fact]
    public void TemplateSubstitutionWithObjectLiteralBraces()
        => Assert.Equal("1", Eval("`${ {a:1}.a }`"));

    [Fact]
    public void TemplateSubstitutionWithCallAfterBraces()
        => Assert.Equal("a5b", Eval("var f = function(){ return 5; }; `a${ f() }b`"));

    [Fact]
    public void NestedTemplateLiteralsWithBraces()
        => Assert.Equal("xy5zw", Eval("`x${ `y${ {n:2}.n + 3 }z` }w`"));

    // ---- Problem 8: computed member CALL with a null literal key ----

    [Fact]
    public void ClassComputedNullKeyMethodCall()
        => Assert.Equal("11", Eval(
            "class C { [null]() { return 11; } static [null]() { return 22; } }"
            + " new C()[null]()"));

    [Fact]
    public void ClassComputedNullKeyStaticMethodCall()
        => Assert.Equal("22", Eval(
            "class C { static [null]() { return 22; } } C[null]()"));

    [Fact]
    public void MemberCallWithNullLiteralMatchesStringNullKey()
        => Assert.Equal("ok", Eval(
            "var o = { 'null': function(){ return 'ok'; } }; o[null]()"));

    // ---- Problem 9: switch case with a null literal ----

    [Fact]
    public void SwitchCaseNullMatches()
        => Assert.Equal("n", Eval(
            "function f(v){ switch(v){ case null: return 'n'; case 1: return 'one';"
            + " default: return 'd'; } } f(null)"));

    [Fact]
    public void SwitchCaseNullDoesNotMatchUndefined()
        => Assert.Equal("d", Eval(
            "function f(v){ switch(v){ case null: return 'n'; default: return 'd'; } }"
            + " f(undefined)"));

    [Fact]
    public void SwitchCaseNullFallThrough()
        // case null matches, then falls through into the (last) default: 64 + 32.
        => Assert.Equal("96", Eval(
            "function f(v){ var r=0; switch(v){ case 0: r+=2; break; case null: r+=64;"
            + " default: r+=32; } return r; } f(null)"));

    // ---- Problem 9: a `default:` clause that is not the last clause ----

    [Fact]
    public void SwitchDefaultInMiddleNoMatchFallsThrough()
        // no case matches -> run default (32) then fall through to the case after it (4).
        => Assert.Equal("36", Eval(
            "function f(v){ var r=0; switch(v){ case 0: r+=2; default: r+=32; case 1: r+=4; }"
            + " return r; } f(9)"));

    [Fact]
    public void SwitchDefaultInMiddleMatchBeforeDefaultFallsThrough()
        // case 0 matches -> 2, fall through default -> 32, fall through case 1 -> 4.
        => Assert.Equal("38", Eval(
            "function f(v){ var r=0; switch(v){ case 0: r+=2; default: r+=32; case 1: r+=4; }"
            + " return r; } f(0)"));

    [Fact]
    public void SwitchDefaultInMiddleMatchAfterDefaultDoesNotRunDefault()
        // case 1 matches -> only 4, default is not entered.
        => Assert.Equal("4", Eval(
            "function f(v){ var r=0; switch(v){ case 0: r+=2; default: r+=32; case 1: r+=4; }"
            + " return r; } f(1)"));

    [Fact]
    public void SwitchDefaultFirstNoMatchFallsThrough()
        => Assert.Equal("38", Eval(
            "function f(v){ var r=0; switch(v){ default: r+=32; case 0: r+=2; case 1: r+=4; }"
            + " return r; } f(9)"));

    [Fact]
    public void SwitchDefaultInMiddleStringDiscriminant()
        // string discriminant exercises the non-numeric dispatch path.
        => Assert.Equal("def-b", Eval(
            "function f(v){ var r=''; switch(v){ case 'a': r+='a'; break;"
            + " default: r+='def-'; case 'b': r+='b'; break; } return r; } f('z')"));

    // ---- Problem 7: Intl options coerced via ToObject ----

    [Fact]
    public void NumberFormatPrimitiveOptionsCoercedToObject()
        => Assert.Equal(
            Eval("new Intl.NumberFormat(['en-US'], new Number(42)).resolvedOptions().style"),
            Eval("new Intl.NumberFormat(['en-US'], 42).resolvedOptions().style"));

    [Fact]
    public void NumberFormatStringOptionsDoesNotThrow()
        => Assert.Equal("decimal", Eval(
            "new Intl.NumberFormat(['en-US'], 'foo').resolvedOptions().style"));

    [Fact]
    public void NumberFormatBooleanOptionsDoesNotThrow()
        => Assert.Equal("decimal", Eval(
            "new Intl.NumberFormat(['en-US'], true).resolvedOptions().style"));

    [Fact]
    public void NumberFormatNullOptionsThrowsTypeError()
        => Assert.Equal("TypeError", Eval(
            "(function(){ try { new Intl.NumberFormat(['en-US'], null); return 'no-throw'; }"
            + " catch(e){ return e.constructor.name; } })()"));

    [Fact]
    public void NumberFormatUndefinedOptionsUsesDefaults()
        => Assert.Equal("decimal", Eval(
            "new Intl.NumberFormat(['en-US'], undefined).resolvedOptions().style"));
}
