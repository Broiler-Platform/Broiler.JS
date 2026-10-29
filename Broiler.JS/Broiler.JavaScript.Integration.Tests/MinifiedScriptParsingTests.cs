using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Parse-level regressions found by running FastParser over the scripts real sites
// actually serve — minifier output that the parser rejected outright, so nothing on the
// page ran. The parser tests assert that each shape parses; these assert that it also
// *means* what it should, because two of the five fixes change how a token is read
// (`/` as a regex delimiter rather than division, `in` as an operator rather than a
// for-in marker) and "it parses" is not evidence that it parses correctly.
//
// Reported as: FastParseException "Unexpected token Negate: ! at 1, 1" — the report a
// failed statement produces once the parser has rewound to its first token, which for a
// `!function(){…}()` bundle is character 1. FastTokenStream.UnexpectedStatement now also
// names where the parse actually stopped.
public class MinifiedScriptParsingTests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // ---- A `/` after the `)` of a statement head opens a regex, not a division ----

    // marked.js (bundled into monaco-editor and into several news sites' page bundles)
    // walks a markdown table's alignment row with the regex as the loop body.
    [Fact]
    public void RegularExpression_AsStatementBodyOfFor_Matches()
    {
        Assert.Equal("right,center,left", Eval(@"
            var align = ['---:', ':--:', ':---'], out = [];
            for (var i = 0; i < align.length; i++)
                /^ *-+: *$/.test(align[i]) ? out.push('right')
                    : /^ *:-+: *$/.test(align[i]) ? out.push('center')
                    : out.push('left');
            out.join(',');"));
    }

    [Fact]
    public void RegularExpression_AsStatementBodyOfIf_Matches()
    {
        Assert.Equal("true", Eval("var r = false; if (1) /ab/.test('xaby') && (r = true); r;"));
    }

    [Fact]
    public void RegularExpression_AsStatementBodyOfWhile_Matches()
    {
        Assert.Equal("3", Eval("var n = 0; while (n < 3) /x/.test('x') && n++; n;"));
    }

    // The other half of the same decision: a `)` that closed a call or a grouping is still
    // followed by division. Getting this wrong would silently turn arithmetic into a regex.
    [Fact]
    public void SlashAfterCallOrGrouping_StillDivides()
    {
        Assert.Equal("5", Eval("var f = function (x) { return x; }; (4 + 6) / 2;"));
        Assert.Equal("3", Eval("var f = function (x) { return x; }; f(12) / 4;"));
        Assert.Equal("2", Eval("var a = 8, b = 2, c = 2; (a) / b / c;"));
    }

    // ---- `in` is an operator again inside anything nested in a `for` head ----

    // core-js and its re-bundles install properties from a `for` head whose initialiser is
    // a function expression that tests `e in C`. The `[~In]` of the head leaked into the
    // function body and rejected the script at that `in`.
    [Fact]
    public void In_InsideAFunctionExpressionOfAForHead_IsAnOperator()
    {
        Assert.Equal("true,false", Eval(@"
            var out = [], C = { a: 1 };
            for (var i = 0, has = function (e) { return e in C; }; i < 1; i++)
                out.push(has('a'), has('b'));
            out.join(',');"));
    }

    [Fact]
    public void In_InsideCallArgumentsOfAForHead_IsAnOperator()
    {
        Assert.Equal("true", Eval("var C = { a: 1 }, id = function (v) { return v; }; for (var r = id('a' in C); 0;); r;"));
    }

    [Fact]
    public void In_InsideATemplateSubstitutionOfAForHead_IsAnOperator()
    {
        Assert.Equal("true", Eval("var C = { a: 1 }; for (var s = `${'a' in C}`; 0;); s;"));
    }

    // And the head's own `[~In]` is untouched: a for-in loop must still be a for-in loop.
    [Fact]
    public void ForInHead_StillIteratesProperties()
    {
        Assert.Equal("a,b", Eval("var out = []; for (var k in { a: 1, b: 2 }) out.push(k); out.join(',');"));
    }

    // ---- A template substitution takes a full Expression ----

    // swiper-bundle.min.js folds a whole initialisation sequence into one interpolation.
    [Fact]
    public void TemplateSubstitution_CommaSequence_EvaluatesToItsLastElement()
    {
        Assert.Equal("id-16", Eval("var n; `id-${n = 16, void 0 === n && (n = 1), n}`;"));
    }

    [Fact]
    public void TemplateSubstitution_CommaSequence_EvaluatesEveryElement()
    {
        Assert.Equal("2:done", Eval("var calls = 0, bump = function () { calls++; }; var s = `${bump(), bump(), 'done'}`; calls + ':' + s;"));
    }

    // ---- A binding pattern's PropertyName may be a reserved word ----

    // TypeScript's own bundle destructures groupBy's result by its boolean keys.
    [Fact]
    public void ObjectBindingPattern_ReservedWordPropertyNames_BindTheRightValues()
    {
        Assert.Equal("d,m", Eval("var { false: decorators, true: metadata } = { false: 'd', true: 'm' }; decorators + ',' + metadata;"));
        Assert.Equal("n", Eval("var { null: x } = { null: 'n' }; x;"));
        Assert.Equal("i", Eval("function f({ in: v }) { return v; } f({ in: 'i' });"));
    }

    // ---- Annex B identity escapes are valid inside a character class ----

    // Adobe Launch's bundle sanitises input with this exact pattern; rejecting `\_` made
    // the scanner re-read the `/` as division and fail the file.
    [Fact]
    public void RegularExpressionClass_IdentityEscape_MatchesTheLiteralCharacter()
    {
        Assert.Equal("a-b_c", Eval(@"'a-b_c!?'.replace(/[^0-9a-zA-Z\-\_]/g, '');"));
        Assert.Equal("__", Eval(@"'_x_'.replace(/[^\_]/g, '');"));
        // An escaped `-` between two atoms stays a literal `-`, not a range.
        Assert.Equal("a-z", Eval(@"'a-zq'.replace(/[^a\-z]/g, '');"));
    }
}
