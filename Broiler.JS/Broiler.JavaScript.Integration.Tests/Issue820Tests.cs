using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/820
//
// Problems 12-19 — Unicode 17.0.0 identifier support. The lexer classifies identifier
// start/continue characters via the runtime's char.GetUnicodeCategory, whose Unicode data
// predates 17.0.0, so code points assigned in 17.0.0 came back unassigned and were
// rejected ("Unexpected token ..."). CharExtensions now recognises the 17.0.0 ID_Start /
// ID_Continue additions explicitly. (test262: language/identifiers/{start,part}-
// unicode-17.0.0{,-escaped,-class,-class-escaped}.js)
//
// Problem 1 — Intl.DateTimeFormat.prototype.formatRange / formatRangeToParts must run
// ToDateTimeFormattable (i.e. ToNumber, observably calling valueOf) on BOTH endpoints,
// in argument order, before deciding the arguments have different kinds. Broiler used to
// short-circuit to the Temporal path whenever either argument was a Temporal object, so a
// non-Temporal argument's valueOf was never invoked before the TypeError for the
// mismatched pair. (test262:
//  intl402/DateTimeFormat/prototype/formatRange/to-datetime-formattable-with-different-arg-kinds.js
//  and the formatRangeToParts counterpart)
//
// Problem 10 — ECMAScript per-repetition capture reset. A capturing group nested
// inside a quantified group must be cleared to undefined at the start of every
// repetition, so a capture that only participated in an earlier iteration does not
// leak into the final result. .NET's regex engine instead retains the last capture
// across repetitions, so /(z)((a+)?(b+)?(c))* /.exec("zaacbbbcac") used to report
// group 4 (the (b+)? group) as "bbb" rather than undefined.
//
// (test262: built-ins/RegExp/S15.10.2.5_A1_T4.js and
//  built-ins/RegExp/prototype/exec/S15.10.6.2_A1_T6.js)
public class Issue820Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // Joins the captures of a match, mapping a non-participating (undefined) group to
    // the literal token "<undef>" so it is observable in the comparison.
    private static string Captures(string regex, string input)
        => Eval($@"
            var m = {regex}.exec({input});
            m.map(function (g) {{ return g === undefined ? '<undef>' : g; }}).join('|');");

    // The canonical test262 case: the inner (b+)? matched only in the middle iteration,
    // so it must reset to undefined for the final "ac" iteration.
    [Fact]
    public void NestedOptionalGroupResetsToUndefined()
        => Assert.Equal("zaacbbbcac|z|ac|a|<undef>|c",
            Captures(@"/(z)((a+)?(b+)?(c))*/", "\"zaacbbbcac\""));

    // Same semantics with named groups (exercises the bjsg rename + reset path).
    [Fact]
    public void NestedOptionalNamedGroupResetsToUndefined()
        => Assert.Equal("zaacbbbcac|z|ac|a|<undef>|c",
            Captures(@"/(?<z>z)((?<a>a+)?(?<b>b+)?(?<c>c))*/", "\"zaacbbbcac\""));

    // A group that DID participate in the final iteration keeps its value.
    [Fact]
    public void GroupParticipatingInFinalIterationIsRetained()
        => Assert.Equal("abab|b",
            Captures(@"/(?:a(b))*/", "\"abab\""));

    // A capture in the last iteration overwrites the earlier one (no stale value).
    [Fact]
    public void RepeatedSingleCaptureKeepsLastIteration()
        => Assert.Equal("xyz|z",
            Captures(@"/(?:(.))*/", "\"xyz\""));

    // Patterns without a capture nested in a quantifier are unaffected.
    [Fact]
    public void NonRepeatedCaptureIsUnaffected()
        => Assert.Equal("abc|b",
            Captures(@"/a(b)c/", "\"abc\""));

    // ---- Problem 1: formatRange ToDateTimeFormattable ordering ----

    // Harness: counts valueOf calls on a non-Temporal endpoint, expects a TypeError from the
    // different-kind pair, and reports "<callCount>,<errorName>".
    private static string RangeOrdering(string method, string start, string end)
        => Eval(@"
            var calls = 0;
            var bad = { valueOf: function () { calls++; return NaN; } };
            var dtf = new Intl.DateTimeFormat();
            var name = 'none';
            try { dtf." + method + "(" + start + ", " + end + @"); }
            catch (e) { name = e.constructor.name; }
            calls + ',' + name;");

    // valueOf must run on the non-Temporal start argument before the TypeError is thrown.
    [Fact]
    public void FormatRangeCoercesNonTemporalStartBeforeKindCheck()
        => Assert.Equal("1,TypeError",
            RangeOrdering("formatRange", "bad", "new Temporal.PlainDate(1970, 1, 1)"));

    // ...and on a non-Temporal end argument too.
    [Fact]
    public void FormatRangeCoercesNonTemporalEndBeforeKindCheck()
        => Assert.Equal("1,TypeError",
            RangeOrdering("formatRange", "new Temporal.PlainDate(1970, 1, 1)", "bad"));

    // formatRangeToParts shares the same coercion path.
    [Fact]
    public void FormatRangeToPartsCoercesNonTemporalBeforeKindCheck()
        => Assert.Equal("1,TypeError",
            RangeOrdering("formatRangeToParts", "bad", "new Temporal.Instant(0n)"));

    // Two Temporal objects of different kinds are still a TypeError (no coercion needed).
    [Fact]
    public void FormatRangeDifferentTemporalKindsThrowTypeError()
        => Assert.Equal("0,TypeError",
            RangeOrdering("formatRange",
                "new Temporal.PlainDate(1970, 1, 1)", "new Temporal.PlainTime()"));

    // ---- Problems 12-19: Unicode 17.0.0 identifiers ----

    // A few representative 17.0.0 ID_Start code points are accepted as identifier names:
    // U+0C5C (Telugu, BMP), U+10940 (Garay), U+11DB0 (Tolong Siki), U+2B73A (CJK Ext-I).
    [Theory]
    [InlineData("౜")]
    [InlineData("\U00010940")]
    [InlineData("\U00011DB0")]
    [InlineData("\U0002B73A")]
    public void Unicode17IdStartIsAccepted(string ch)
        => Assert.Equal("42", Eval($"var {ch} = 42; {ch};"));

    // A 17.0.0 ID_Continue-only code point (combining mark U+1ACF) is valid after a start.
    [Fact]
    public void Unicode17IdContinueIsAccepted()
        => Assert.Equal("7", Eval("var a᫏ = 7; a᫏;"));

    // The same character works through a \u{...} escape in the identifier.
    [Fact]
    public void Unicode17IdStartViaUnicodeEscape()
        => Assert.Equal("5", Eval("var \\u{10940} = 5; \\u{10940};"));

    // ...and as a class private name (#name).
    [Fact]
    public void Unicode17IdStartInPrivateName()
        => Assert.Equal("9", Eval(
            "class C { #౜ = 9; get() { return this.#౜; } } new C().get();"));
}
