using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Broiler.ECMAScript.Runners;
using Xunit;

namespace Broiler.ECMAScript.Syntax.Tests;

/// <summary>
/// The public token stream (the owner's decision of 2026-11-04, "Expose + preview.10"; Broiler.VM's JSD-0083, decision
/// D21): <see cref="ParseResult.Tokens"/> with <see cref="ParseRequest.RetainTokens"/>, and every fact Broiler.VM's
/// tokenizers-agreement row (SJL-13.3's step 1, part F) compares - kind, raw text, numeric and string value, line, column,
/// offset and end, the line-terminator, legacy-octal and escaped flags, a <c>/</c> read as division or as a literal's start
/// under the goal the parser named, and a template's run of pieces with their cooked and raw chunks - with the request's
/// switch off by default and the parse otherwise unchanged by it.
/// </summary>
public sealed class TokenStreamTests
{
    private static ParseRequest Request(string text, ParseGoal goal = ParseGoal.Script, bool retain = true, SourceText? source = null) =>
        new(source ?? new SourceText(text), goal, UnicodeDataHome.Tables, NoValidator.Instance) { RetainTokens = retain };

    private static IReadOnlyList<Token> Tokens(string text, ParseGoal goal = ParseGoal.Script)
    {
        var result = SyntaxParser.Parse(Request(text, goal));
        Assert.True(result.Outcome == ParseOutcome.Accepted, text + " was refused: " + string.Join(", ", result.Diagnostics.Select(static d => d.Code)));
        return result.Tokens;
    }

    private static string RawOf(string text, Token token) => text[token.Start..token.End];

    private static Token Only(string text, TokenKind kind, ParseGoal goal = ParseGoal.Script) => Assert.Single(Tokens(text, goal), t => t.Kind == kind);

    // ---------------------------------------------------------------------------------------------------------------
    // The switch.

    [Fact]
    public void The_switch_is_off_by_default_and_a_result_then_publishes_no_token()
    {
        var request = new ParseRequest(new SourceText("var x = 1;"), ParseGoal.Script, UnicodeDataHome.Tables, NoValidator.Instance);
        Assert.False(request.RetainTokens);
        var result = SyntaxParser.Parse(request);
        Assert.Equal(ParseOutcome.Accepted, result.Outcome);
        Assert.Empty(result.Tokens);

        // Not vacuous: the parse read its five tokens, which the canonical dump reads either way.
        Assert.Equal(5, result.TokenStream.Count);
    }

    [Fact]
    public void With_the_switch_on_the_result_publishes_the_stream_the_parse_read()
    {
        var result = SyntaxParser.Parse(Request("var x = 1;"));
        Assert.Equal([TokenKind.IdentifierName, TokenKind.IdentifierName, TokenKind.Equals, TokenKind.NumericLiteral, TokenKind.Semicolon], result.Tokens.Select(static t => t.Kind).ToArray());
        Assert.Equal(result.TokenStream, result.Tokens);
    }

    [Fact]
    public void A_result_holds_one_list_of_tokens_and_a_flag_as_its_layout_was_before_the_switch()
    {
        // 0.1.0-preview.9's result held four references and its outcome; the switch adds a flag beside the outcome, where
        // the layout has room, and no reference, so that a result is the same size switch off or on.
        var fields = typeof(ParseResult).GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        Assert.Equal(4, fields.Count(static f => !f.FieldType.IsValueType));
        Assert.Equal([typeof(bool), typeof(ParseOutcome)], fields.Where(static f => f.FieldType.IsValueType).Select(static f => f.FieldType).OrderBy(static t => t.Name, StringComparer.Ordinal).ToArray());
    }

    [Theory]
    [InlineData("var x = ")]
    [InlineData("x = 'abc")]
    [InlineData("let a; let a;")]
    public void A_refused_source_publishes_no_token_with_the_switch_on(string text)
    {
        var result = SyntaxParser.Parse(Request(text));
        Assert.Equal(ParseOutcome.Refused, result.Outcome);
        Assert.Empty(result.Tokens);
    }

    [Theory]
    [InlineData("a = /b/g; `t${c}u`; x / y;", ParseGoal.Script)]
    [InlineData("import x from 'm'; export default /re/;", ParseGoal.Module)]
    [InlineData("var s = 017 + '\\01'; f(a\n++b)", ParseGoal.Script)]
    [InlineData("let a; let a;", ParseGoal.Script)]
    [InlineData("a, b = 1", ParseGoal.FormalParameters)]
    [InlineData("return /x/.test(a)", ParseGoal.FunctionBody)]
    [InlineData("eval(1); 'use strict'; x", ParseGoal.Eval)]
    public void The_switch_changes_nothing_but_the_published_tokens(string text, ParseGoal goal)
    {
        var off = SyntaxParser.Parse(Request(text, goal, retain: false));
        var on = SyntaxParser.Parse(Request(text, goal, retain: true));

        // The canonical dump (tree, side tables, diagnostics and the stream the parse read) is the same bytes.
        Assert.Equal(SyntaxDump.Dump(off), SyntaxDump.Dump(on));
        Assert.Equal(off.Outcome, on.Outcome);
        Assert.Equal(off.Diagnostics.Count, on.Diagnostics.Count);
        Assert.Empty(off.Tokens);
        Assert.Equal(on.TokenStream, on.Tokens);
        Assert.Equal(off.TokenStream, on.Tokens);
    }

    [Fact]
    public void The_stream_is_the_same_in_a_repeated_parse_and_on_another_thread()
    {
        const string text = "function* g(a = /x/g) { yield `h${a / 2}m${'\\x41'}t`; return 0x1F + 017 + 1n; }\nclass C { #p = 1; m() { return this.#p } }";
        var first = Tokens(text).ToArray();
        Assert.Equal(first, Tokens(text).ToArray());
        Token[]? other = null;
        var thread = new Thread(() => other = Tokens(text).ToArray(), ParseLimits.StackBytes);
        thread.Start();
        thread.Join();
        Assert.Equal(first, other);
        Assert.True(first.Length > 30);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Positions and raw text.

    [Fact]
    public void Each_token_s_raw_text_is_its_span_and_the_stream_is_in_source_order_with_no_comment_and_no_end()
    {
        const string text = "#!/usr/bin/env node\n// a comment\nvar /* c */ a = [1, 'b', `c${d}e`, /f/g];\n<!-- html\nx";
        var tokens = Tokens(text);
        Assert.Equal(
            ["var", "a", "=", "[", "1", ",", "'b'", ",", "`c${", "d", "}e`", ",", "/f/g", "]", ";", "x"],
            tokens.Select(t => RawOf(text, t)).ToArray());
        for (var i = 1; i < tokens.Count; i++)
            Assert.True(tokens[i - 1].End <= tokens[i].Start);
        Assert.All(tokens, static t => Assert.Equal(t.End - t.Start, t.Length));
        Assert.DoesNotContain(tokens, static t => t.Kind is TokenKind.EndOfInput or TokenKind.Invalid);
    }

    [Fact]
    public void Lines_and_columns_are_one_based_in_code_units_and_every_line_terminator_starts_a_line()
    {
        const string text = "a\r\nbb\rc\u2028d\u2029e\nf+\u00e9g";
        var tokens = Tokens(text);
        Assert.Equal(
            [(1, 1, 0, 1), (2, 1, 3, 5), (3, 1, 6, 7), (4, 1, 8, 9), (5, 1, 10, 11), (6, 1, 12, 13), (6, 2, 13, 14), (6, 3, 14, 16)],
            tokens.Select(static t => (t.Line, t.Column, t.Start, t.End)).ToArray());
    }

    [Fact]
    public void Offsets_are_the_request_s_text_s_and_lines_and_columns_take_the_source_s_base()
    {
        const string text = "a +\n  b";
        var result = SyntaxParser.Parse(Request(text, source: new SourceText(text, "s.js", baseOffset: 100, baseLine: 10, baseColumn: 5)));
        Assert.Equal(
            [(10, 5, 0, 1), (10, 7, 2, 3), (11, 3, 6, 7)],
            result.Tokens.Select(static t => (t.Line, t.Column, t.Start, t.End)).ToArray());
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Values and flags.

    [Theory]
    [InlineData("0x1F", 31.0)]
    [InlineData("1e3", 1000.0)]
    [InlineData(".5", 0.5)]
    [InlineData("1_000", 1000.0)]
    [InlineData("0b101", 5.0)]
    [InlineData("0o17", 15.0)]
    [InlineData("9007199254740993", 9007199254740992.0)]
    public void A_numeric_literal_carries_its_number_value(string literal, double value)
    {
        var token = Only("x = " + literal, TokenKind.NumericLiteral);
        Assert.Equal(value, token.Number);
        Assert.Equal(TokenFlags.None, token.Flags & (TokenFlags.LegacyOctal | TokenFlags.NonOctalDecimal | TokenFlags.BigInt));
        Assert.Null(token.Value);
    }

    [Fact]
    public void A_legacy_octal_literal_a_non_octal_decimal_one_and_a_big_int_carry_their_flags()
    {
        var octal = Only("x = 017", TokenKind.NumericLiteral);
        Assert.Equal((15.0, TokenFlags.LegacyOctal), (octal.Number, octal.Flags & TokenFlags.LegacyOctal));
        var nonOctal = Only("x = 089", TokenKind.NumericLiteral);
        Assert.Equal((89.0, TokenFlags.NonOctalDecimal), (nonOctal.Number, nonOctal.Flags & TokenFlags.NonOctalDecimal));
        Assert.Equal(TokenFlags.None, nonOctal.Flags & TokenFlags.LegacyOctal);

        // A BigInt's value is an engine's to build: NaN here, its digits in its raw text.
        const string big = "x = 123456789012345678901234567890n";
        var bigInt = Only(big, TokenKind.NumericLiteral);
        Assert.True(double.IsNaN(bigInt.Number));
        Assert.Equal(TokenFlags.BigInt, bigInt.Flags & TokenFlags.BigInt);
        Assert.Equal("123456789012345678901234567890n", RawOf(big, bigInt));
    }

    [Fact]
    public void A_string_literal_carries_its_value_and_its_escape_facts()
    {
        const string text = "x = ['\\x61\\u{62}\\u0063', 'plain', '\\01', '\\8', 'a\\\nb']";
        var strings = Tokens(text).Where(static t => t.Kind == TokenKind.StringLiteral).ToArray();
        Assert.Equal(["abc", "plain", "\u0001", "8", "ab"], strings.Select(static t => t.Value).ToArray());
        Assert.Equal(TokenFlags.StringHasEscape, strings[0].Flags);
        Assert.Equal(TokenFlags.None, strings[1].Flags);
        Assert.Equal(TokenFlags.LegacyOctalEscape, strings[2].Flags & TokenFlags.LegacyOctalEscape);
        Assert.Equal(TokenFlags.NonOctalDecimalEscape, strings[3].Flags & TokenFlags.NonOctalDecimalEscape);
        Assert.Equal(TokenFlags.StringHasEscape, strings[4].Flags & TokenFlags.StringHasEscape);
        Assert.Equal("'\\x61\\u{62}\\u0063'", RawOf(text, strings[0]));
    }

    [Fact]
    public void An_identifier_carries_its_cooked_name_and_whether_it_is_escaped()
    {
        const string text = "var \\u0061b = a\\u{62}c, plain, \\u{1D49C};";
        var names = Tokens(text).Where(static t => t.Kind == TokenKind.IdentifierName).ToArray();
        Assert.Equal(["var", "ab", "abc", "plain", "\U0001D49C"], names.Select(static t => t.Value).ToArray());
        Assert.Equal([false, true, true, false, true], names.Select(static t => t.IsEscaped).ToArray());
        Assert.Equal(names.Select(static t => t.IsEscaped), names.Select(static t => (t.Flags & TokenFlags.Escaped) != 0));
        Assert.Equal("a\\u{62}c", RawOf(text, names[2]));
    }

    [Fact]
    public void A_keyword_is_an_identifier_name_and_a_private_name_keeps_its_hash()
    {
        const string text = "class C { #p = 1; static m() { return this.#p in this; } }";
        var tokens = Tokens(text);
        Assert.Equal("class", tokens[0].Value);
        Assert.Equal(TokenKind.IdentifierName, tokens[0].Kind);
        var privates = tokens.Where(static t => t.Kind == TokenKind.PrivateIdentifier).ToArray();
        Assert.Equal(["#p", "#p"], privates.Select(static t => t.Value).ToArray());
    }

    [Fact]
    public void An_identifier_name_carries_the_word_its_cooked_name_spells_escaped_or_not()
    {
        // The kind of section 9.3 of the design: an identifier name with the reserved or contextual word its cooked name
        // spells, never decided a keyword, so that a kind map reads the kind, the word and the escaped flag.
        const string text = "if (a) a.i\\u0066 = of + async + foo + \\u0061wait + this;";
        var names = Tokens(text).Where(static t => t.Kind == TokenKind.IdentifierName).ToArray();
        Assert.Equal(
            [(Word.If, false), (Word.None, false), (Word.None, false), (Word.If, true), (Word.Of, false), (Word.Async, false), (Word.None, false), (Word.Await, true), (Word.This, false)],
            names.Select(static t => (t.Word, t.IsEscaped)).ToArray());
        Assert.All(Tokens(text).Where(static t => t.Kind != TokenKind.IdentifierName), static t => Assert.Equal(Word.None, t.Word));
    }

    [Fact]
    public void A_default_token_is_no_token_of_any_stream()
    {
        // The constructor is internal; a consumer can still make a default token, which carries no facts.
        var token = default(Token);
        Assert.Equal(TokenKind.EndOfInput, token.Kind);
        Assert.Equal(Word.None, token.Word);
        Assert.Equal((0, 0, 0, 0, TokenFlags.None), (token.Start, token.End, token.Line, token.Column, token.Flags));
        Assert.Null(token.Value);
        Assert.Null(token.Raw);
        Assert.Equal(0.0, token.Number);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Templates.

    [Fact]
    public void A_template_with_substitutions_is_its_run_of_pieces_with_each_substitution_s_tokens_between()
    {
        const string text = "x = `a${b}c\\n${ {d: `in${e}` }.d + 1 }e\\u0041`;";
        var tokens = Tokens(text);
        Assert.Equal(
            [
                (TokenKind.IdentifierName, "x"), (TokenKind.Equals, "="),
                (TokenKind.TemplateHead, "`a${"), (TokenKind.IdentifierName, "b"),
                (TokenKind.TemplateMiddle, "}c\\n${"),
                (TokenKind.LeftBrace, "{"), (TokenKind.IdentifierName, "d"), (TokenKind.Colon, ":"),
                (TokenKind.TemplateHead, "`in${"), (TokenKind.IdentifierName, "e"), (TokenKind.TemplateTail, "}`"),
                (TokenKind.RightBrace, "}"), (TokenKind.Dot, "."), (TokenKind.IdentifierName, "d"), (TokenKind.Plus, "+"),
                (TokenKind.NumericLiteral, "1"),
                (TokenKind.TemplateTail, "}e\\u0041`"), (TokenKind.Semicolon, ";"),
            ],
            tokens.Select(t => (t.Kind, RawOf(text, t))).ToArray());

        // Each piece's cooked chunk (TV) and raw chunk (TRV).
        var pieces = tokens.Where(static t => t.Kind is TokenKind.TemplateHead or TokenKind.TemplateMiddle or TokenKind.TemplateTail).ToArray();
        Assert.Equal(["a", "c\n", "in", "", "eA"], pieces.Select(static t => t.Value).ToArray());
        Assert.Equal(["a", "c\\n", "in", "", "e\\u0041"], pieces.Select(static t => t.Raw).ToArray());

        // The outer template's span is from its head's start to its tail's end.
        Assert.Equal("`a${b}c\\n${ {d: `in${e}` }.d + 1 }e\\u0041`", text[pieces[0].Start..pieces[^1].End]);
    }

    [Fact]
    public void A_template_without_substitutions_is_one_token_and_its_raw_chunk_reads_every_CR_as_LF()
    {
        const string text = "x = `a\r\nb\rc\\\r\nd`";
        var token = Only(text, TokenKind.NoSubstitutionTemplate);
        Assert.Equal("a\nb\nc\\\nd", token.Raw);
        Assert.Equal("a\nb\ncd", token.Value);
        Assert.Equal(text[4..], RawOf(text, token));
    }

    [Fact]
    public void A_tagged_template_s_not_escape_sequence_has_no_cooked_chunk()
    {
        var token = Only("tag`\\unicode`", TokenKind.NoSubstitutionTemplate);
        Assert.Null(token.Value);
        Assert.Equal("\\unicode", token.Raw);
        Assert.Equal(TokenFlags.NotEscapeSequence, token.Flags & TokenFlags.NotEscapeSequence);
        Assert.Equal(TokenFlags.None, Only("tag`\\n`", TokenKind.NoSubstitutionTemplate).Flags & TokenFlags.NotEscapeSequence);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // A slash, as the parser's goal read it.

    // R: a regular-expression literal; D: division (/ or /=).
    [Theory]
    [InlineData("a / b / c", "DD")]
    [InlineData("(a) / 2", "D")]
    [InlineData("[a] / 2", "D")]
    [InlineData("x = {} / 1", "D")]
    [InlineData("{} /re/", "R")]
    [InlineData("{ /re/ }", "R")]
    [InlineData("f(/re/)", "R")]
    [InlineData("f(a, /re/)", "R")]
    [InlineData("x = /re/", "R")]
    [InlineData("x += /re/", "R")]
    [InlineData("a ? /b/ : /c/", "RR")]
    [InlineData("a + /re/", "R")]
    [InlineData("!/re/.test(a)", "R")]
    [InlineData("a; /re/", "R")]
    [InlineData("x => /re/", "R")]
    [InlineData("typeof /re/", "R")]
    [InlineData("function f() { return /re/ }", "R")]
    [InlineData("a++ / 2", "D")]
    [InlineData("++/re/.lastIndex", "R")]
    [InlineData("1 / 2", "D")]
    [InlineData("'a' / 2", "D")]
    [InlineData("this / 2", "D")]
    [InlineData("`a` / 2", "D")]
    [InlineData("`${a}` / 2", "D")]
    [InlineData("`${/re/}`", "R")]
    [InlineData("a /= 2", "D")]
    [InlineData("x = /=/", "R")]
    [InlineData("a\n/re/g", "DD")]
    [InlineData("x = a\n/re/i", "DD")]
    [InlineData("if (a) /re/", "R")]
    [InlineData("x = function () {} / 1", "D")]
    [InlineData("x = class {} / 1", "D")]
    public void A_slash_is_a_literal_s_start_or_division_as_the_parser_s_goal_read_it(string text, string expected)
    {
        var slashes = Tokens(text).Where(static t => t.Kind is TokenKind.RegularExpressionLiteral or TokenKind.Slash or TokenKind.SlashEquals);
        Assert.Equal(expected, string.Concat(slashes.Select(static t => t.Kind == TokenKind.RegularExpressionLiteral ? 'R' : 'D')));
    }

    [Fact]
    public void A_regular_expression_literal_s_raw_text_holds_its_body_and_its_flags()
    {
        const string text = "x = /[/]\\/a/giu";
        var literal = Only(text, TokenKind.RegularExpressionLiteral);
        var raw = RawOf(text, literal);
        Assert.Equal("/[/]\\/a/giu", raw);
        var last = raw.LastIndexOf('/');
        Assert.Equal(("[/]\\/a", "giu"), (raw[1..last], raw[(last + 1)..]));
        Assert.Null(literal.Value);
        Assert.True(double.IsNaN(literal.Number));
        Assert.Equal(TokenKind.SlashEquals, Assert.Single(Tokens("a /= 2"), static t => t.Kind == TokenKind.SlashEquals).Kind);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Line terminators and automatic semicolon insertion.

    [Fact]
    public void A_token_after_a_line_terminator_or_a_comment_holding_one_is_marked()
    {
        const string text = "a\nb /* no */ + c /*\n*/ d // e\nf\u2028g\u2029h\r\ni";
        var tokens = Tokens(text);
        Assert.Equal(["a", "b", "+", "c", "d", "f", "g", "h", "i"], tokens.Select(t => RawOf(text, t)).ToArray());
        Assert.Equal([false, true, false, false, true, true, true, true, true], tokens.Select(static t => t.NewLineBefore).ToArray());
        Assert.Equal(tokens.Select(static t => t.NewLineBefore), tokens.Select(static t => (t.Flags & TokenFlags.NewLineBefore) != 0));
    }

    [Fact]
    public void The_restricted_productions_and_automatic_semicolon_insertion_read_the_marks_the_stream_carries()
    {
        // ++ after a line terminator is a prefix of the next statement's operand.
        const string update = "a\n++b";
        var tokens = Tokens(update);
        Assert.Equal([TokenKind.IdentifierName, TokenKind.PlusPlus, TokenKind.IdentifierName], tokens.Select(static t => t.Kind).ToArray());
        Assert.Equal([false, true, false], tokens.Select(static t => t.NewLineBefore).ToArray());

        // return, then a line terminator: the statement ends, and no semicolon token is made.
        const string ret = "function f() { return\nx }";
        var body = Tokens(ret);
        Assert.DoesNotContain(body, static t => t.Kind == TokenKind.Semicolon);
        Assert.True(body.Single(t => RawOf(ret, t) == "x").NewLineBefore);

        // The text's start is no line terminator, but one before the first token marks it, as the tokenizer marks it after
        // a hashbang.
        Assert.False(Tokens("  /* c */ a")[0].NewLineBefore);
        Assert.True(Tokens("\n\n  a")[0].NewLineBefore);
        Assert.True(Tokens("/*\n*/a")[0].NewLineBefore);
    }
}
