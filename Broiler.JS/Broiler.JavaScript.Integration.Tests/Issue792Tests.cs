using Broiler.JavaScript.Engine;
using Xunit;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/792
//
// Fixed here:
//   * Problem 3 — `let` followed by `{` (or an identifier) across a line terminator in a
//     single-statement context (the body of if/while/do/for/with) is an IdentifierReference
//     expression statement (ASI), not a LexicalDeclaration: `if (false) let\n{}` parses as
//     `if (false) let;` followed by an empty block. A lexical declaration that genuinely begins
//     in such a body (`let [`, or same-line `let {` / `let <id>`) is a SyntaxError.
//   * Problems 14/15 — Temporal.Duration (from/with/add/subtract) and the date/time add/subtract
//     methods read the duration-like fields in alphabetical order (days, hours, microseconds,
//     milliseconds, minutes, months, nanoseconds, seconds, weeks, years); PlainDate/PlainDateTime/
//     PlainYearMonth add/subtract read the duration argument before the `overflow` option.
//   * Problem 9 — Temporal.Instant.from / compare / since / until / equals accept a ZonedDateTime,
//     converting it via its epoch nanoseconds (ToTemporalInstant).
public class Issue792Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // Reports the error constructor name from compiling `source` (a JS string literal),
    // so SyntaxErrors raised while parsing are observable.
    private static string CompileError(string source)
    {
        using var ctx = new JSContext();
        return ctx.Eval($"var c='NONE'; try {{ eval({source}); }} catch (e) {{ c = e.constructor.name; }} c").ToString();
    }

    // ── Problem 3: `let \n {` in a single-statement body is `let;` + block ─────────

    // The body of these statements is a single Statement, so `let` followed by `{` across a
    // line terminator is an IdentifierReference + ASI, not a LexicalDeclaration. The bodies are
    // chosen so they never execute (the unbound `let` reference is never evaluated).
    [Theory]
    [InlineData("if (false) let\n{}\n'ok'", "ok")]
    [InlineData("while (false) let\n{}\n'ok'", "ok")]
    [InlineData("for (;false;) let\n{}\n'ok'", "ok")]
    [InlineData("for (let x in {}) let\n{}\n'ok'", "ok")]
    [InlineData("for (let x of []) let\n{}\n'ok'", "ok")]
    // a `with` always executes its body, so make the `let` reference resolve via the with-object
    [InlineData("with ({let: 0}) let\n{}\n'ok'", "ok")]
    public void LetBlockWithNewline_ParsesAsExpressionAndBlock(string code, string expected)
        => Assert.Equal(expected, Eval(code));

    // a genuine lexical declaration in a single-statement body is still a SyntaxError
    [Theory]
    [InlineData("'if (false) let x = 1;'")]
    [InlineData("'if (false) let [a] = [];'")]
    [InlineData("'while (false) let {a} = {};'")]
    public void LexicalDeclaration_InSingleStatementBody_Throws(string source)
        => Assert.Equal("SyntaxError", CompileError(source));

    // a block body still allows lexical declarations
    [Fact]
    public void LexicalDeclaration_InBlockBody_Allowed()
        => Assert.Equal("1", Eval("let r; if (true) { let x = 1; r = x; } r"));

    // ── Problems 14/15: duration fields are read in alphabetical order ────────────

    private const string Recorder = @"
        var order = [];
        function bag(extra) {
          var o = extra || {};
          ['years','months','weeks','days','hours','minutes','seconds','milliseconds','microseconds','nanoseconds'].forEach(function(k){
            Object.defineProperty(o, k, { get: function(){ order.push(k); return 0; }, enumerable: true });
          });
          return o;
        }
    ";

    private const string AlphaOrder =
        "days,hours,microseconds,milliseconds,minutes,months,nanoseconds,seconds,weeks,years";

    [Theory]
    [InlineData("Temporal.Duration.from(bag())")]
    [InlineData("new Temporal.Duration().with(bag())")]
    [InlineData("new Temporal.Duration().add(bag())")]
    [InlineData("new Temporal.Duration().subtract(bag())")]
    [InlineData("new Temporal.PlainTime(0).add(bag())")]
    [InlineData("new Temporal.PlainTime(0).subtract(bag())")]
    public void Duration_FieldsReadAlphabetically(string expr)
        => Assert.Equal(AlphaOrder, Eval(Recorder + expr + "; order.join(',')"));

    // PlainDate/PlainDateTime/PlainYearMonth read the duration before the overflow option
    [Theory]
    [InlineData("new Temporal.PlainDate(2000,1,1).add(bag())")]
    [InlineData("new Temporal.PlainDateTime(2000,1,1).add(bag())")]
    public void DateAdd_ReadsDurationBeforeOverflow(string expr)
    {
        const string code = @"
            var seenOverflow = -1;
            var opts = { get overflow(){ seenOverflow = order.length; return 'constrain'; } };
        ";
        // overflow is read after all 10 duration fields
        Assert.Equal("10", Eval(Recorder + code + expr.Replace("bag()", "bag(), opts") + "; String(seenOverflow)"));
    }

    // ── Problem 5: hebrew leap-month constraining (M05L → M06 in a common year) ───

    private const string HebrewLeapDate =
        "Temporal.PlainDate.from({year:5784,monthCode:'M05L',day:1,calendar:'hebrew'})";

    [Fact]
    public void Hebrew_LeapToLeapYear_KeepsLeapMonthCode()
        => Assert.Equal("M05L", Eval($"{HebrewLeapDate}.with({{year:5782}}).monthCode"));

    [Fact]
    public void Hebrew_LeapToCommonYear_ConstrainsToAdar()
        => Assert.Equal("M06", Eval($"{HebrewLeapDate}.with({{year:5783}}).monthCode"));

    [Fact]
    public void Hebrew_LeapToCommonYear_RejectThrows()
        => Assert.Equal("RangeError",
            ErrorNameRuntime($"{HebrewLeapDate}.with({{year:5783}}, {{overflow:'reject'}})"));

    // catches a runtime JS error thrown by evaluating an otherwise-valid program
    private static string ErrorNameRuntime(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval($"var c='NONE'; try {{ {code}; }} catch (e) {{ c = e.constructor.name; }} c").ToString();
    }

    // ── Problem 9: Temporal.Instant accepts a ZonedDateTime ──────────────────────

    [Fact]
    public void InstantFrom_AcceptsZonedDateTime()
        => Assert.Equal(
            "1970-01-01T00:00:00Z",
            Eval("Temporal.Instant.from(new Temporal.ZonedDateTime(0n, 'UTC')).toString()"));

    [Fact]
    public void InstantCompare_AcceptsZonedDateTime()
        => Assert.Equal(
            "0",
            Eval("String(Temporal.Instant.compare(new Temporal.ZonedDateTime(0n,'UTC'), new Temporal.Instant(0n)))"));
}
