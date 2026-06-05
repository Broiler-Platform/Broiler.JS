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
//   * Problem 9  — a switch `case` whose test is a non-string/number/boolean
//                  literal (e.g. `case null:`) threw NotImplementedException. It
//                  now compiles and compares via strict-equals like any expression
//                  case.
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
//
// Note: Problem 9's S12.11_A1_T3/T4 fixtures additionally require a `default:`
// clause that is NOT the last clause to fall through correctly. That is a separate,
// pre-existing architectural limitation of the value-producing switch codegen
// (cross-arm gotos through a mid-position default unbalance the evaluation stack)
// and is left out of scope here; the NotImplementedException itself is fixed.
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
