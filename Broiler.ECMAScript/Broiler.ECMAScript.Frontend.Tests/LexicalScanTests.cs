using Broiler.ECMAScript.Runners;
using Xunit;

namespace Broiler.ECMAScript.Syntax.Tests;

/// <summary>
/// The parse entry's answer at stage SJL-13.2b: a source the lexical grammar refuses is refused, with the tokenizer's
/// diagnostic, wherever no reading of it the parser could choose would tokenize it; every other source is not yet judged.
/// </summary>
public sealed class LexicalScanTests
{
    private static ParseResult Parse(string text, ParseGoal goal = ParseGoal.Script) =>
        SyntaxParser.Parse(new ParseRequest(new SourceText(text, "t.js"), goal, UnicodeDataHome.Tables, NoValidator.Instance));

    [Theory]
    [InlineData("var \\u0030a;", "JSE1201", 4)]
    [InlineData("var a\\u002D;", "JSE1202", 4)]
    [InlineData("x = '\\x4';", "JSE9105", 4)]
    [InlineData("x = 'a\nb';", "JSE9104", 4)]
    [InlineData("x = 1_;", "JSE9103", 4)]
    [InlineData("x = 07.5;", "JSE9103", 4)]
    [InlineData("x = `a", "JSE9107", 4)]
    [InlineData("x = `a${1", "JSE9107", 4)]
    [InlineData("/* a", "JSE9102", 0)]
    [InlineData("x = /a\n/;", "JSE9106", 4)]
    [InlineData("a \u0085 b", "JSE9101", 2)]
    [InlineData("#!x\n#!y", "JSE9101", 4)]
    [InlineData("1 / 'a", "JSE9104", 4)]
    [InlineData("`${ {} }` + '\\u{110000}'", "JSE9105", 12)]
    [InlineData("`${ `${1}` }` + '\\u12'", "JSE9105", 16)]
    [InlineData("(/[/]/, '\\x')", "JSE9105", 8)]
    public void A_source_no_reading_can_tokenize_is_refused_with_the_tokenizer_s_diagnostic(string text, string code, int offset)
    {
        var result = Parse(text);
        Assert.Equal(ParseOutcome.Refused, result.Outcome);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal((code, offset, DiagnosticPhase.Tokenizer, "t.js"), (diagnostic.Code, diagnostic.Span.Offset, diagnostic.Phase, diagnostic.SourceName));
    }

    /// <summary>
    /// Where only the parser can say whether a <c>/</c> divides, the scan stops: a refusal after it is not reached, because
    /// the other reading might tokenize. The same refusal before such a <c>/</c> is reached.
    /// </summary>
    [Theory]
    [InlineData("a / '\\x'")]
    [InlineData("f() /'\\x'/g")]
    [InlineData("x = a[0] / 2; '\\x'")]
    [InlineData("{} / '\\x'")]
    [InlineData("i++ / '\\x'")]
    [InlineData("return / '\\x'")]
    public void A_refusal_after_a_slash_only_the_parser_can_read_is_not_judged(string text)
    {
        var result = Parse(text);
        Assert.Equal(ParseOutcome.NotImplemented, result.Outcome);
        Assert.Empty(result.Diagnostics);
    }

    /// <summary>
    /// In a module, a string literal followed by a line terminator may end an import or export declaration, after which
    /// automatic semicolon insertion lets the next item begin with a regular-expression literal: the scan stops at the
    /// <c>/</c>, since reading it as division would refuse a module the specification accepts. In a script, and in a module
    /// without the line terminator, the <c>/</c> after a string is division, and the refusal after it is reached.
    /// </summary>
    [Theory]
    [InlineData("import \"m\"\n/\"/;")]
    [InlineData("import \"m\"\n/`/.test(\"\");")]
    [InlineData("export * from \"m\"\n/\"/;")]
    [InlineData("import x from \"m\" /*\n*/ /\"/;")]
    [InlineData("x = \"a\"\n/ '\\x'")]
    public void A_slash_after_a_module_specifier_s_line_is_not_judged(string text)
    {
        var result = Parse(text, ParseGoal.Module);
        Assert.Equal(ParseOutcome.NotImplemented, result.Outcome);
        Assert.Empty(result.Diagnostics);
    }

    [Theory]
    [InlineData("import \"m\" /\"/;", ParseGoal.Module, "JSE9104", 12)]
    [InlineData("x = \"a\" / '\\x'", ParseGoal.Module, "JSE9105", 10)]
    [InlineData("'a'\n/ '\\x'", ParseGoal.Script, "JSE9105", 6)]
    [InlineData("import \"m\"\n/\"/;", ParseGoal.Script, "JSE9104", 12)]
    public void A_slash_after_a_string_is_division_where_no_module_item_can_follow(string text, ParseGoal goal, string code, int offset)
    {
        var result = Parse(text, goal);
        Assert.Equal(ParseOutcome.Refused, result.Outcome);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal((code, offset), (diagnostic.Code, diagnostic.Span.Offset));
    }

    [Theory]
    [InlineData("var a = 1;")]
    [InlineData("x = /a/g.test('b');")]
    [InlineData("`a${b}c${ {d} }e`")]
    [InlineData("`\\01`")]
    [InlineData("'use strict'; 010")]
    [InlineData("var \\u0069f;")]
    [InlineData("@dec class C {}")]
    [InlineData("07.toString()")]
    [InlineData("")]
    public void A_source_the_lexical_grammar_admits_is_not_judged_before_the_parser(string text)
    {
        var result = Parse(text);
        Assert.Equal(ParseOutcome.NotImplemented, result.Outcome);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Each_goal_takes_its_own_lexical_start()
    {
        Assert.Equal(ParseOutcome.NotImplemented, Parse("#!x\n1", ParseGoal.Eval).Outcome);
        Assert.Equal(ParseOutcome.NotImplemented, Parse("#!x\n1", ParseGoal.Module).Outcome);
        Assert.Equal("JSE9101", Assert.Single(Parse("#!x\n1", ParseGoal.FunctionBody).Diagnostics).Code);
        Assert.Equal(ParseOutcome.NotImplemented, Parse("--> x\n1", ParseGoal.FunctionBody).Outcome);
        Assert.Equal(ParseOutcome.NotImplemented, Parse("x <!-- '\\x'", ParseGoal.Script).Outcome);
        Assert.Equal("JSE9105", Assert.Single(Parse("x <!-- '\\x'", ParseGoal.Module).Diagnostics).Code);
    }

    [Fact]
    public void The_lexer_extension_admits_the_m_suffix()
    {
        var request = new ParseRequest(new SourceText("x = 1.5m;"), ParseGoal.Script, UnicodeDataHome.Tables, NoValidator.Instance) { Extensions = LexerExtensions.DecimalSuffix };
        Assert.Equal(ParseOutcome.NotImplemented, SyntaxParser.Parse(request).Outcome);
        Assert.Equal("JSE9103", Assert.Single(Parse("x = 1.5m;").Diagnostics).Code);
    }

    [Fact]
    public void A_diagnostic_s_span_takes_the_source_s_base()
    {
        var request = new ParseRequest(new SourceText("x\n  'a", "s.js", 40, 3, 7), ParseGoal.Script, UnicodeDataHome.Tables, NoValidator.Instance);
        var diagnostic = Assert.Single(SyntaxParser.Parse(request).Diagnostics);
        Assert.Equal(new SourceSpan(44, 2, 4, 3), diagnostic.Span);
        Assert.Equal("syntax.string.unterminated", diagnostic.MessageId);
        Assert.Empty(diagnostic.Arguments);
    }
}
