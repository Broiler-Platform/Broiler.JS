using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/857 — test262 script-host
// failures fixed across two passes:
//
//  • Problem 1: a `with`-fallback overlay published a program-level `let` as a global-object
//    property, desyncing reads/writes inside `with (globalThis)` from the live binding.
//  • Problem 2: PlainMonthDay/PlainYearMonth toLocaleString collapsed an iso8601 calendar to
//    gregory for the value-vs-formatter calendar check, wrongly throwing a RangeError.
//  • Problem 3: out-of-range rounding boundaries (since/until huge increment), out-of-range
//    Duration.compare endpoints, overspecified PlainMonthDay year fields, and invalid hebrew
//    leap month codes must all be RangeErrors.
//  • Problem 4: a class declared inside a generator with two `[yield …]` computed property
//    names made the generator rewriter lift the same reused compiler temp twice, so the
//    original→box ToDictionary threw "An item with the same key has already been added".
//  • Problems 5/8/9/10: multiple non-critical calendar annotations
//    ("[u-ca=iso8601][u-ca=discord]") were rejected; they are valid (first wins) and only a
//    critical ("!") flag among several makes them a RangeError. (Covered in Issue781Tests.)
//  • Problem 6: the completion value of a break/continue in a desugared eval loop, and `++` on
//    an eval-introduced global var, threw NotImplementedException at IL generation.
//  • Problem 7: a `using` LexicalDeclaration in a C-style for head (`for (using x = …; …; …)`)
//    was rejected as a SyntaxError even though it is valid Explicit Resource Management syntax.
public class Issue857Tests
{
    private static JSValue Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code);
    }

    // Problem 4: two `[yield …]` computed property names of a class inside a generator share one
    // reused compiler temp; the rewriter must box it once instead of faulting on a duplicate key.
    [Fact]
    public void ClassWithMultipleYieldComputedNamesInsideGenerator()
    {
        var code =
            "(function () {"
            + "  function* g() {"
            + "    let C = class {"
            + "      [yield 9]() { return 'a'; }"
            + "      static [yield 9]() { return 'b'; }"
            + "    };"
            + "    let c = new C();"
            + "    return c[yield 9]() + C[yield 9]();"
            + "  }"
            + "  var it = g();"
            + "  while (!it.next().done) ;"
            + "  return 'ok';"
            + "})()";
        Assert.Equal("ok", Eval(code).ToString());
    }

    // The doubled object-literal computed-name variant exercises the same shared-temp path.
    [Fact]
    public void ObjectLiteralWithMultipleYieldComputedNamesInsideGenerator()
    {
        var code =
            "(function () {"
            + "  function* g() { return { [yield 1]: 'x', [yield 2]: 'y' }; }"
            + "  var it = g();"
            + "  it.next(); it.next('a'); var r = it.next('b');"
            + "  return r.value.a + r.value.b;"
            + "})()";
        Assert.Equal("xy", Eval(code).ToString());
    }

    // Problem 7: a sync `using` declaration is a valid C-style for-head LexicalDeclaration.
    [Fact]
    public void SyncUsingInCStyleForHeadIsDisposed()
    {
        var code =
            "(function () {"
            + "  var log = [];"
            + "  for (using x = { [Symbol.dispose]() { log.push('disposed'); } }; false;) {}"
            + "  return log.join(',');"
            + "})()";
        Assert.Equal("disposed", Eval(code).ToString());
    }

    private static string ErrorName(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(
            "(function(){ try { " + code + "; return 'no throw'; } catch (e) { return e.constructor.name; } })()")
            .ToString();
    }

    // ───────────── Problem 1: @@unscopables `with` corrupts a global `let` ─────────────

    // A program-level `let` is a lexical binding, never a global-object property. A `with`-fallback
    // overlay used to publish it as one, so a `count++` inside `with (globalThis)` read/wrote the
    // leaked property while code outside the `with` used the real binding — desyncing them. The two
    // `with` blocks (with a plain `count++` between them) must observe the same, live `count`.
    [Fact]
    public void WithOverGlobalThisDoesNotDesyncGlobalLet()
    {
        var code =
            "let count = 0;"
            + "with (globalThis) { count++; }"
            + "count++;"
            + "with (globalThis) { count++; }"
            + "'' + count + '|' + globalThis.hasOwnProperty('count');";
        Assert.Equal("3|false", Eval(code).ToString());
    }

    // ───────────── Problem 2: iso8601 Temporal value formats with an iso8601 formatter ─────────────

    // PlainMonthDay / PlainYearMonth require the Temporal value's calendar to equal the formatter's
    // resolved calendar IDENTIFIER. iso8601 must not be collapsed to gregory for that check, so an
    // iso8601 value with an explicit { calendar: "iso8601" } (or -u-ca-iso8601 locale) formats.
    [Fact]
    public void PlainMonthDayToLocaleStringIso8601IsAString()
        => Assert.Equal("string", Eval(
            "typeof new Temporal.PlainMonthDay(1, 1).toLocaleString(undefined, { calendar: 'iso8601' })").ToString());

    [Fact]
    public void PlainYearMonthToLocaleStringIso8601IsAString()
        => Assert.Equal("string", Eval(
            "typeof new Temporal.PlainYearMonth(2024, 1).toLocaleString('en-u-ca-iso8601')").ToString());

    // ───────────── Problem 3: rounding / arithmetic out-of-range RangeErrors ─────────────

    // since/until with a rounding increment large enough to push the rounding boundary past the valid
    // ISO range is a RangeError — but the default (smallestUnit month, increment 1) still differences
    // exactly at the limit (issue #794), building no boundary.
    [Fact]
    public void PlainYearMonthSinceHugeIncrementThrows()
        => Assert.Equal("RangeError", ErrorName(
            "new Temporal.PlainYearMonth(1970, 1).since(new Temporal.PlainYearMonth(1971, 1), { roundingIncrement: 100000000 })"));

    [Fact]
    public void PlainYearMonthSinceAtIsoLimitDoesNotThrow()
        => Assert.Equal("ok", Eval(
            "new Temporal.PlainYearMonth(1970, 1).since('+275760-09'); 'ok'").ToString());

    // Duration.compare adds each duration to relativeTo; an endpoint outside the representable
    // PlainDateTime range is a RangeError (a duration carrying ~2^53 seconds reaches far past it).
    [Fact]
    public void DurationCompareOutOfRangeEndpointThrows()
        => Assert.Equal("RangeError", ErrorName(
            "Temporal.Duration.compare("
            + "Temporal.Duration.from({ years: 1, seconds: 9007199254740991 }),"
            + "Temporal.Duration.from({ years: 2 }),"
            + "{ relativeTo: new Temporal.PlainDate(2000, 1, 1) })"));

    // PlainMonthDay.from with overspecified, disagreeing year fields (era/eraYear vs year) is a
    // RangeError; agreeing ones are accepted.
    [Fact]
    public void PlainMonthDayFromConflictingEraAndYearThrows()
        => Assert.Equal("RangeError", ErrorName(
            "Temporal.PlainMonthDay.from({ calendar: 'gregory', era: 'ce', eraYear: 2024, year: 2023, monthCode: 'M01', day: 1 })"));

    [Fact]
    public void PlainMonthDayFromAgreeingEraAndYearIsAccepted()
        => Assert.Equal("string", Eval(
            "typeof Temporal.PlainMonthDay.from({ calendar: 'gregory', era: 'ce', eraYear: 2024, year: 2024, monthCode: 'M01', day: 1 }).toString()").ToString());

    // A leap month code naming a leap month a fixed-leap-month calendar never has (hebrew has only
    // "M05L", Adar I) is a RangeError; the variable-leap-month chinese calendar keeps M05L valid.
    [Theory]
    [InlineData("M01L")]
    [InlineData("M04L")]
    [InlineData("M13")]
    public void PlainMonthDayFromInvalidHebrewLeapCodeThrows(string monthCode)
        => Assert.Equal("RangeError", ErrorName(
            $"Temporal.PlainMonthDay.from({{ calendar: 'hebrew', monthCode: '{monthCode}', day: 1 }})"));

    [Fact]
    public void PlainMonthDayFromValidHebrewLeapCodeIsAccepted()
        => Assert.Equal("M05L", Eval(
            "Temporal.PlainMonthDay.from({ calendar: 'hebrew', monthCode: 'M05L', day: 1 }).monthCode").ToString());

    // ───────────── Problem 6: completion value of break/continue in a desugared eval loop ─────────────

    // The completion value of a loop whose body's last statement is empty (UpdateEmpty) must be
    // undefined. An eval-introduced global `var` updated by `++` inside the loop previously emitted an
    // assignment to the binding's (non-assignable) throwing read expression — NotImplementedException
    // at IL generation. The write must target the assignable global-object property instead.
    [Fact]
    public void EvalForLoopWithBreakInCatchCompletionIsUndefined()
        => Assert.Equal("undefined", Eval(
            "typeof eval(\"for (var i = 0; i < 2; ++i) { if (i) { try { throw null; } catch (e) { break; } } 'x'; }\")").ToString());

    [Fact]
    public void EvalGlobalVarIncrementInLoop()
        => Assert.Equal("undefined", Eval(
            "typeof eval(\"for (var count = 0;;) { if (count === 5) break; else count++; }\")").ToString());
}
