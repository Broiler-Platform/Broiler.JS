using System.Collections.Generic;
using System.Linq;
using Broiler.ECMAScript.Runners;
using Broiler.Unicode.Algorithms;
using Xunit;

namespace Broiler.ECMAScript.Syntax.Tests;

/// <summary>Tokenizers over a text, with the classes of the repository's pinned Unicode tables.</summary>
internal static class Lex
{
    internal static EcmaScriptLexical Lexical { get; } = new(new UnicodeProperties(UnicodeDataHome.Tables));

    internal static Tokenizer Over(string text, bool html = true, LexerExtensions extensions = LexerExtensions.None, bool hashbang = true, bool lineStart = true, SourceText? source = null) =>
        new(source ?? new SourceText(text), Lexical, html, extensions, hashbang, lineStart);

    /// <summary>Every token of <paramref name="text"/> under one goal, up to the end of the input or a refusal, which is kept.</summary>
    internal static List<Token> All(string text, LexicalGoal goal = LexicalGoal.RegExp, bool html = true, LexerExtensions extensions = LexerExtensions.None)
    {
        var tokenizer = Over(text, html, extensions);
        var tokens = new List<Token>();
        while (true)
        {
            var token = tokenizer.Next(goal);
            if (token.Kind == TokenKind.EndOfInput)
                return tokens;
            tokens.Add(token);
            if (token.Kind == TokenKind.Invalid)
                return tokens;
        }
    }

    internal static TokenKind[] Kinds(string text, LexicalGoal goal = LexicalGoal.RegExp, bool html = true) => All(text, goal, html).Select(static t => t.Kind).ToArray();

    internal static string Names(string text, LexicalGoal goal = LexicalGoal.RegExp) => string.Join(' ', Kinds(text, goal));

    internal static Token One(string text, LexicalGoal goal = LexicalGoal.RegExp, LexerExtensions extensions = LexerExtensions.None)
    {
        var tokens = All(text, goal, extensions: extensions);
        Assert.Single(tokens);
        return tokens[0];
    }

    /// <summary>The diagnostic a text is refused with, under one goal.</summary>
    internal static SyntaxDiagnostic Refusal(string text, LexicalGoal goal = LexicalGoal.RegExp, LexerExtensions extensions = LexerExtensions.None)
    {
        var tokenizer = Over(text, extensions: extensions);
        while (true)
        {
            var token = tokenizer.Next(goal);
            Assert.NotEqual(TokenKind.EndOfInput, token.Kind);
            if (token.Kind == TokenKind.Invalid)
            {
                Assert.NotNull(tokenizer.Diagnostic);
                Assert.Equal(DiagnosticPhase.Tokenizer, tokenizer.Diagnostic!.Phase);
                return tokenizer.Diagnostic;
            }
        }
    }
}

/// <summary>
/// White space, line terminators, comments, the hashbang and Annex B's HTML-like comments (ECMA-262 clauses 12.2 to 12.5
/// and B.1.1; the correspondence's rows T1 to T5, T30).
/// </summary>
public sealed class TriviaTests
{
    [Theory]
    [InlineData("\t\v\f \u00A0\uFEFF\u1680\u2000\u200A\u202F\u205F\u3000a")]
    public void White_space_is_TAB_VT_FF_SP_NBSP_ZWNBSP_and_Zs(string text)
    {
        var token = Lex.One(text);
        Assert.Equal(TokenKind.IdentifierName, token.Kind);
        Assert.False(token.NewLineBefore);
    }

    [Theory]
    [InlineData("\u0085")]
    [InlineData("\u180E")]
    [InlineData("\u200B")]
    public void A_code_point_that_is_no_white_space_is_an_unexpected_character(string text)
    {
        var refusal = Lex.Refusal("a" + text);
        Assert.Equal("JSE9101", refusal.Code);
        Assert.Equal("syntax.character.unexpected", refusal.MessageId);
        Assert.Equal(new SourceSpan(1, 1, 1, 2), refusal.Span);
    }

    [Theory]
    [InlineData("a\nb", 2, 1)]
    [InlineData("a\rb", 2, 1)]
    [InlineData("a\r\nb", 2, 1)]
    [InlineData("a\u2028b", 2, 1)]
    [InlineData("a\u2029b", 2, 1)]
    [InlineData("a\n\r\n\rb", 4, 1)]
    [InlineData("a /*\n*/ b", 2, 4)]
    [InlineData("a // c\n  b", 2, 3)]
    public void Each_line_terminator_starts_a_line_CR_LF_is_one_and_the_token_after_one_is_marked(string text, int line, int column)
    {
        var tokens = Lex.All(text);
        Assert.Equal(2, tokens.Count);
        Assert.False(tokens[0].NewLineBefore);
        Assert.True(tokens[1].NewLineBefore);
        Assert.Equal((line, column), (tokens[1].Line, tokens[1].Column));
    }

    [Fact]
    public void A_comment_without_a_line_terminator_is_white_space_for_automatic_semicolon_insertion()
    {
        var tokens = Lex.All("a /* c */ b // d");
        Assert.Equal(2, tokens.Count);
        Assert.False(tokens[1].NewLineBefore);
        Assert.Equal(10, tokens[1].Start);
    }

    [Fact]
    public void An_unterminated_comment_is_refused_where_it_begins()
    {
        var refusal = Lex.Refusal("a\n  /* b\n c");
        Assert.Equal("JSE9102", refusal.Code);
        Assert.Equal(new SourceSpan(4, 7, 2, 3), refusal.Span);
    }

    [Fact]
    public void A_hashbang_is_a_comment_at_the_text_s_first_code_unit_alone()
    {
        var tokens = Lex.All("#!/usr/bin/env node\nx");
        Assert.Single(tokens);
        Assert.Equal((2, 1, true), (tokens[0].Line, tokens[0].Column, tokens[0].NewLineBefore));

        Assert.Equal("JSE9101", Lex.Refusal(" #!x").Code);
        Assert.Equal("JSE9101", Lex.Refusal("x\n#!x").Code);
        var noHashbang = Lex.Over("#!x", hashbang: false);
        Assert.Equal(TokenKind.Invalid, noHashbang.Next(LexicalGoal.RegExp).Kind);
        Assert.Equal("JSE9101", noHashbang.Diagnostic!.Code);
    }

    [Theory]
    [InlineData("x <!-- y\nz", "IdentifierName IdentifierName")]
    [InlineData("--> y\nz", "IdentifierName")]
    [InlineData("x\n  --> y\nz", "IdentifierName IdentifierName")]
    [InlineData("x\n/* a */ /* b */ --> y\nz", "IdentifierName IdentifierName")]
    [InlineData("x /*\n*/ --> y\nz", "IdentifierName IdentifierName")]
    [InlineData("x --> y", "IdentifierName MinusMinus GreaterThan IdentifierName")]
    [InlineData("x /* a */ --> y", "IdentifierName MinusMinus GreaterThan IdentifierName")]
    public void Html_like_comments_open_anywhere_and_close_at_a_line_s_start(string text, string kinds)
    {
        Assert.Equal(kinds, Lex.Names(text));
    }

    [Fact]
    public void Without_the_switch_html_like_comments_are_punctuators()
    {
        Assert.Equal(
            [TokenKind.IdentifierName, TokenKind.LessThan, TokenKind.Bang, TokenKind.MinusMinus, TokenKind.IdentifierName],
            Lex.Kinds("x <!-- y", html: false));
        Assert.Equal([TokenKind.MinusMinus, TokenKind.GreaterThan, TokenKind.IdentifierName], Lex.Kinds("--> y", html: false));
    }

    [Fact]
    public void A_dynamic_function_s_parameters_do_not_start_a_line()
    {
        var tokenizer = Lex.Over("--> a", hashbang: false, lineStart: false);
        Assert.Equal(TokenKind.MinusMinus, tokenizer.Next(LexicalGoal.RegExp).Kind);
    }
}

/// <summary>Names: IdentifierName, PrivateIdentifier and their escapes, with clause 12.7.1.1's early errors (rows T10, T11, T12, T13, T21, T23, T31, T32).</summary>
public sealed class NameTests
{
    [Theory]
    [InlineData("abc", "abc")]
    [InlineData("$_a1", "$_a1")]
    [InlineData("\u2118x", "\u2118x")]
    [InlineData("\u2163", "\u2163")]
    [InlineData("a\u200Cb\u200D", "a\u200Cb\u200D")]
    [InlineData("\uD835\uDC00b", "\uD835\uDC00b")]
    [InlineData("\\u0061bc", "abc")]
    [InlineData("a\\u{62}c", "abc")]
    [InlineData("\\u{0000000061}", "a")]
    [InlineData("\\u{16D40}", "\uD81B\uDD40")]
    public void An_identifier_name_s_value_is_the_name_its_escapes_spell(string text, string name)
    {
        var token = Lex.One(text);
        Assert.Equal(TokenKind.IdentifierName, token.Kind);
        Assert.Equal(name, token.Value);
        Assert.Equal(text.Contains('\\'), token.IsEscaped);
        Assert.Equal((0, text.Length), (token.Start, token.End));
    }

    [Theory]
    [InlineData("if", "If", false)]
    [InlineData("\\u0069f", "If", true)]
    [InlineData("await", "Await", false)]
    [InlineData("\\u006ff", "Of", true)]
    [InlineData("implements", "Implements", false)]
    [InlineData("ifs", "None", false)]
    public void A_reserved_word_is_an_identifier_name_with_its_word_a_fact_for_the_parser(string text, string word, bool escaped)
    {
        var token = Lex.One(text);
        Assert.Equal(TokenKind.IdentifierName, token.Kind);
        Assert.Equal(word, token.Word.ToString());
        Assert.Equal(escaped, token.IsEscaped);
    }

    [Theory]
    [InlineData("\\u0030a", "JSE1201")]
    [InlineData("\\u002Da", "JSE1201")]
    [InlineData("\\uD835\\uDC00", "JSE1201")]
    [InlineData("a\\u002D", "JSE1202")]
    [InlineData("a\\u{1F600}", "JSE1202")]
    [InlineData("\\u00", "JSE9111")]
    [InlineData("\\u{}", "JSE9111")]
    [InlineData("\\u{110000}", "JSE9111")]
    [InlineData("\\u{61", "JSE9111")]
    [InlineData("\\u 061", "JSE9111")]
    [InlineData("\\u{0_61}", "JSE9111")]
    [InlineData("\\x61", "JSE9101")]
    [InlineData("a\\", "JSE9101")]
    public void A_malformed_or_out_of_class_escape_is_refused_at_the_name_s_start(string text, string code)
    {
        var refusal = Lex.Refusal("  " + text);
        Assert.Equal(code, refusal.Code);
        Assert.Equal((2, 1, 3), (refusal.Span.Offset, refusal.Span.Line, refusal.Span.Column));
    }

    [Theory]
    [InlineData("#x", "#x", false)]
    [InlineData("#\\u0061b", "#ab", true)]
    [InlineData("#$", "#$", false)]
    public void A_private_name_is_one_token_with_its_hash(string text, string name, bool escaped)
    {
        var token = Lex.One(text);
        Assert.Equal(TokenKind.PrivateIdentifier, token.Kind);
        Assert.Equal(name, token.Value);
        Assert.Equal(escaped, token.IsEscaped);
        Assert.Equal(Word.None, token.Word);
    }

    [Theory]
    [InlineData("# x")]
    [InlineData("#1")]
    [InlineData("#")]
    public void A_hash_not_followed_by_an_identifier_start_is_refused(string text)
    {
        Assert.Equal("JSE9101", Lex.Refusal(text).Code);
    }
}

/// <summary>Numeric literals: clause 12.9.3, valued by <c>NumericLiterals.Evaluate</c> (rows T16, T17, T18, T19; decision D9).</summary>
public sealed class NumericLiteralTests
{
    [Theory]
    [InlineData("0", 0d, (int)(TokenFlags.None))]
    [InlineData("123", 123d, (int)(TokenFlags.None))]
    [InlineData("1_000_000", 1_000_000d, (int)(TokenFlags.None))]
    [InlineData(".5", 0.5d, (int)(TokenFlags.None))]
    [InlineData("5.", 5d, (int)(TokenFlags.None))]
    [InlineData("1.5e3", 1500d, (int)(TokenFlags.None))]
    [InlineData("1E-3", 0.001d, (int)(TokenFlags.None))]
    [InlineData("1e+0_1", 10d, (int)(TokenFlags.None))]
    [InlineData("0x1F", 31d, (int)(TokenFlags.None))]
    [InlineData("0X1_f", 31d, (int)(TokenFlags.None))]
    [InlineData("0o17", 15d, (int)(TokenFlags.None))]
    [InlineData("0b101", 5d, (int)(TokenFlags.None))]
    [InlineData("017", 15d, (int)(TokenFlags.LegacyOctal))]
    [InlineData("00", 0d, (int)(TokenFlags.LegacyOctal))]
    [InlineData("08", 8d, (int)(TokenFlags.NonOctalDecimal))]
    [InlineData("08.5", 8.5d, (int)(TokenFlags.NonOctalDecimal))]
    [InlineData("09e1", 90d, (int)(TokenFlags.NonOctalDecimal))]
    [InlineData("0.5", 0.5d, (int)(TokenFlags.None))]
    public void A_number_is_valued_by_the_shared_algorithm_and_its_legacy_form_recorded(string text, double value, int flags)
    {
        var token = Lex.One(text);
        Assert.Equal(TokenKind.NumericLiteral, token.Kind);
        Assert.Equal(value, token.Number);
        Assert.Equal((TokenFlags)flags, token.Flags);
        Assert.Equal(text.Length, token.End);
    }

    [Theory]
    [InlineData("0n")]
    [InlineData("1n")]
    [InlineData("1_0n")]
    [InlineData("0x1Fn")]
    [InlineData("0b1n")]
    [InlineData("123456789012345678901234567890n")]
    public void A_bigint_is_checked_for_its_form_and_its_value_left_to_the_engine(string text)
    {
        var token = Lex.One(text);
        Assert.Equal(TokenKind.NumericLiteral, token.Kind);
        Assert.Equal(TokenFlags.BigInt, token.Flags);
        Assert.True(double.IsNaN(token.Number));
    }

    [Theory]
    [InlineData("1_")]
    [InlineData("1__0")]
    [InlineData("0_1")]
    [InlineData("08_1")]
    [InlineData("0x")]
    [InlineData("0x_1")]
    [InlineData("0xg")]
    [InlineData("1e")]
    [InlineData("1e+")]
    [InlineData("1._5")]
    [InlineData("07.5")]
    [InlineData("01n")]
    [InlineData("08n")]
    [InlineData("1.5n")]
    [InlineData("1e3n")]
    [InlineData("3in")]
    [InlineData("3$")]
    [InlineData("1m")]
    [InlineData("0b12")]
    [InlineData("0o8")]
    public void A_literal_that_is_no_numeric_literal_or_touches_an_identifier_start_or_digit_is_refused(string text)
    {
        var refusal = Lex.Refusal("x = " + text);
        Assert.Equal("JSE9103", refusal.Code);
        Assert.Equal("syntax.numeric-literal.invalid", refusal.MessageId);
        Assert.Equal((4, 1, 5), (refusal.Span.Offset, refusal.Span.Line, refusal.Span.Column));
    }

    [Theory]
    [InlineData("07.toString", "NumericLiteral Dot IdentifierName")]
    [InlineData("1..toString", "NumericLiteral Dot IdentifierName")]
    [InlineData("1.5.5", "NumericLiteral NumericLiteral")]
    [InlineData("a?.5:b", "IdentifierName Question NumericLiteral Colon IdentifierName")]
    [InlineData("a.\u0663", "IdentifierName Dot Invalid")]
    [InlineData("3\\u0061", "NumericLiteral IdentifierName")]
    public void A_literal_ends_where_its_production_does(string text, string kinds)
    {
        Assert.Equal(kinds, Lex.Names(text));
    }

    [Theory]
    [InlineData("1m")]
    [InlineData("1.5m")]
    [InlineData(".5m")]
    [InlineData("08m")]
    [InlineData("017m")]
    public void The_m_suffix_is_admitted_by_the_lexer_extension_alone_and_given_no_value(string text)
    {
        var token = Lex.One(text, extensions: LexerExtensions.DecimalSuffix);
        Assert.Equal(TokenKind.NumericLiteral, token.Kind);
        Assert.True((token.Flags & TokenFlags.DecimalSuffix) != 0);
        Assert.True(double.IsNaN(token.Number));
        Assert.Equal("JSE9103", Lex.Refusal(text).Code);
    }

    [Theory]
    [InlineData("1e3m")]
    [InlineData("0x1m")]
    [InlineData("1nm")]
    [InlineData("1mm")]
    public void The_m_suffix_follows_no_exponent_radix_or_bigint(string text)
    {
        Assert.Equal("JSE9103", Lex.Refusal(text, extensions: LexerExtensions.DecimalSuffix).Code);
    }
}

/// <summary>String literals: clause 12.9.4, with the legacy escapes the parser refuses in strict code (rows T12, T13, T14, T15).</summary>
public sealed class StringLiteralTests
{
    [Theory]
    [InlineData("'a'", "a", (int)(TokenFlags.None))]
    [InlineData("\"it's\"", "it's", (int)(TokenFlags.None))]
    [InlineData("'\\b\\t\\n\\v\\f\\r\\\"\\'\\\\'", "\b\t\n\v\f\r\"'\\", (int)(TokenFlags.StringHasEscape))]
    [InlineData("'\\0'", "\0", (int)(TokenFlags.StringHasEscape))]
    [InlineData("'\\x41\\u0042\\u{43}'", "ABC", (int)(TokenFlags.StringHasEscape))]
    [InlineData("'\\u{1F600}'", "\uD83D\uDE00", (int)(TokenFlags.StringHasEscape))]
    [InlineData("'\\d\\%'", "d%", (int)(TokenFlags.StringHasEscape))]
    [InlineData("'a\\\nb'", "ab", (int)(TokenFlags.StringHasEscape))]
    [InlineData("'a\\\r\nb'", "ab", (int)(TokenFlags.StringHasEscape))]
    [InlineData("'a\u2028b'", "a\u2028b", (int)(TokenFlags.None))]
    [InlineData("'\\101'", "A", (int)(TokenFlags.StringHasEscape | TokenFlags.LegacyOctalEscape))]
    [InlineData("'\\400'", " 0", (int)(TokenFlags.StringHasEscape | TokenFlags.LegacyOctalEscape))]
    [InlineData("'\\08'", "\08", (int)(TokenFlags.StringHasEscape | TokenFlags.LegacyOctalEscape))]
    [InlineData("'\\7'", "\a", (int)(TokenFlags.StringHasEscape | TokenFlags.LegacyOctalEscape))]
    [InlineData("'\\8\\9'", "89", (int)(TokenFlags.StringHasEscape | TokenFlags.NonOctalDecimalEscape))]
    public void A_string_s_value_is_its_SV_with_its_escapes_recorded(string text, string value, int flags)
    {
        var token = Lex.One(text);
        Assert.Equal(TokenKind.StringLiteral, token.Kind);
        Assert.Equal(value, token.Value);
        Assert.Equal((TokenFlags)flags, token.Flags);
    }

    [Fact]
    public void An_escaped_lone_surrogate_is_one_code_unit_of_the_value()
    {
        var token = Lex.One("'\\uD800\\uDC00\\uDFFF'");
        Assert.Equal("\uD800\uDC00\uDFFF", token.Value);
        Assert.Equal(3, token.Value!.Length);
    }

    [Fact]
    public void A_line_continuation_and_a_raw_LS_count_as_lines_for_what_follows()
    {
        var tokens = Lex.All("'a\\\r\nb\u2029c' d");
        Assert.Equal(2, tokens.Count);
        Assert.Equal((3, 4), (tokens[1].Line, tokens[1].Column));
        Assert.False(tokens[1].NewLineBefore);
    }

    [Theory]
    [InlineData("'a", "JSE9104")]
    [InlineData("'a\nb'", "JSE9104")]
    [InlineData("'a\rb'", "JSE9104")]
    [InlineData("'a\\", "JSE9104")]
    [InlineData("'\\x4'", "JSE9105")]
    [InlineData("'\\x 1'", "JSE9105")]
    [InlineData("'\\x_1'", "JSE9105")]
    [InlineData("'\\u12'", "JSE9105")]
    [InlineData("'\\u 041'", "JSE9105")]
    [InlineData("'\\u{110000}'", "JSE9105")]
    [InlineData("'\\u{0_41}'", "JSE9105")]
    [InlineData("'\\u{}'", "JSE9105")]
    public void An_unterminated_string_or_an_undefined_escape_is_refused_at_the_string_s_start(string text, string code)
    {
        var refusal = Lex.Refusal("x\n " + text);
        Assert.Equal(code, refusal.Code);
        Assert.Equal((3, 2, 2), (refusal.Span.Offset, refusal.Span.Line, refusal.Span.Column));
    }
}

/// <summary>Templates: clause 12.9.6, read in place one piece at a time with their TV and TRV (rows T6, T7, T8, T27, T28, T29).</summary>
public sealed class TemplateTests
{
    [Fact]
    public void A_template_without_substitutions_is_one_token_with_its_cooked_and_raw_text()
    {
        var token = Lex.One("`a\\n\\u0041\\${x\\``");
        Assert.Equal(TokenKind.NoSubstitutionTemplate, token.Kind);
        Assert.Equal("a\nA${x`", token.Value);
        Assert.Equal("a\\n\\u0041\\${x\\`", token.Raw);
    }

    [Fact]
    public void Each_substitution_is_read_in_place_under_the_parser_s_goal()
    {
        var tokenizer = Lex.Over("`a${ {b} }c${d}e`");
        var head = tokenizer.Next(LexicalGoal.RegExp);
        Assert.Equal((TokenKind.TemplateHead, "a", 0, 4), (head.Kind, head.Value, head.Start, head.End));
        Assert.Equal(TokenKind.LeftBrace, tokenizer.Next(LexicalGoal.RegExp).Kind);
        Assert.Equal(TokenKind.IdentifierName, tokenizer.Next(LexicalGoal.RegExp).Kind);
        Assert.Equal(TokenKind.RightBrace, tokenizer.Next(LexicalGoal.Div).Kind);
        var middle = tokenizer.Next(LexicalGoal.TemplateTail);
        Assert.Equal((TokenKind.TemplateMiddle, "c", 9, 13), (middle.Kind, middle.Value, middle.Start, middle.End));
        Assert.Equal(TokenKind.IdentifierName, tokenizer.Next(LexicalGoal.RegExp).Kind);
        var tail = tokenizer.Next(LexicalGoal.RegExpOrTemplateTail);
        Assert.Equal((TokenKind.TemplateTail, "e", 14, 17), (tail.Kind, tail.Value, tail.Start, tail.End));
        Assert.Equal(TokenKind.EndOfInput, tokenizer.Next(LexicalGoal.Div).Kind);
    }

    [Theory]
    [InlineData("`a\r\nb\rc\u2028d`", "a\nb\nc\u2028d", "a\nb\nc\u2028d")]
    [InlineData("`a\\\r\nb`", "ab", "a\\\nb")]
    [InlineData("`$`", "$", "$")]
    [InlineData("`$$\\${`", "$$${", "$$\\${")]
    public void Line_terminators_are_LF_in_both_values_and_a_continuation_is_raw_alone(string text, string cooked, string raw)
    {
        var token = Lex.One(text);
        Assert.Equal((cooked, raw), (token.Value, token.Raw));
    }

    [Theory]
    [InlineData("`\\01`")]
    [InlineData("`\\1`")]
    [InlineData("`\\8`")]
    [InlineData("`\\x`")]
    [InlineData("`\\x1`")]
    [InlineData("`\\xg1`")]
    [InlineData("`\\u12`")]
    [InlineData("`\\u{`")]
    [InlineData("`\\u{110000}`")]
    [InlineData("`\\u{12`")]
    [InlineData("`\\u {41}`")]
    public void A_not_escape_sequence_leaves_the_piece_without_a_cooked_value_and_its_raw_text_whole(string text)
    {
        var token = Lex.One(text);
        Assert.Equal(TokenKind.NoSubstitutionTemplate, token.Kind);
        Assert.Null(token.Value);
        Assert.Equal(TokenFlags.NotEscapeSequence, token.Flags);
        Assert.Equal(text[1..^1], token.Raw);
        Assert.Equal(1, token.Split);
    }

    [Fact]
    public void A_not_escape_sequence_s_next_code_unit_is_read_as_the_template_s()
    {
        var tokenizer = Lex.Over("`\\u{${x}`");
        var head = tokenizer.Next(LexicalGoal.RegExp);
        Assert.Equal((TokenKind.TemplateHead, (string?)null, "\\u{"), (head.Kind, head.Value, head.Raw));
        Assert.Equal(TokenKind.IdentifierName, tokenizer.Next(LexicalGoal.RegExp).Kind);
        Assert.Equal(TokenKind.TemplateTail, tokenizer.Next(LexicalGoal.TemplateTail).Kind);
    }

    [Fact]
    public void An_unterminated_template_is_refused_at_its_backtick_its_tail_s_included()
    {
        var refusal = Lex.Refusal("x\n `abc");
        Assert.Equal("JSE9107", refusal.Code);
        Assert.Equal((3, 2, 2), (refusal.Span.Offset, refusal.Span.Line, refusal.Span.Column));

        var tokenizer = Lex.Over("x\n `a${b}c");
        Assert.Equal(TokenKind.IdentifierName, tokenizer.Next(LexicalGoal.RegExp).Kind);
        Assert.Equal(TokenKind.TemplateHead, tokenizer.Next(LexicalGoal.RegExp).Kind);
        Assert.Equal(TokenKind.IdentifierName, tokenizer.Next(LexicalGoal.RegExp).Kind);
        Assert.Equal(TokenKind.Invalid, tokenizer.Next(LexicalGoal.TemplateTail).Kind);
        Assert.Equal(("JSE9107", 3, 2, 2), (tokenizer.Diagnostic!.Code, tokenizer.Diagnostic.Span.Offset, tokenizer.Diagnostic.Span.Line, tokenizer.Diagnostic.Span.Column));
    }

    [Fact]
    public void The_end_of_the_input_inside_a_substitution_is_an_unterminated_template_at_its_backtick()
    {
        var tokenizer = Lex.Over("x = `a${1");
        Assert.Equal(TokenKind.IdentifierName, tokenizer.Next(LexicalGoal.RegExp).Kind);
        Assert.Equal(TokenKind.Equals, tokenizer.Next(LexicalGoal.Div).Kind);
        Assert.Equal(TokenKind.TemplateHead, tokenizer.Next(LexicalGoal.RegExp).Kind);
        Assert.Equal(TokenKind.NumericLiteral, tokenizer.Next(LexicalGoal.RegExp).Kind);
        Assert.Equal(TokenKind.Invalid, tokenizer.Next(LexicalGoal.Div).Kind);
        Assert.Equal(("JSE9107", 4), (tokenizer.Diagnostic!.Code, tokenizer.Diagnostic.Span.Offset));
    }

    [Fact]
    public void A_substitution_s_hashbang_and_html_close_are_not_a_text_s_start()
    {
        // Rows T6 and T7: `${#!` and `${-->` are not the start of a text or a line.
        var tokenizer = Lex.Over("`${#!\n1}`");
        Assert.Equal(TokenKind.TemplateHead, tokenizer.Next(LexicalGoal.RegExp).Kind);
        Assert.Equal(TokenKind.Invalid, tokenizer.Next(LexicalGoal.RegExp).Kind);

        var close = Lex.Over("`${-->\n1}`");
        Assert.Equal(TokenKind.TemplateHead, close.Next(LexicalGoal.RegExp).Kind);
        Assert.Equal(TokenKind.MinusMinus, close.Next(LexicalGoal.RegExp).Kind);
    }
}

/// <summary>Regular-expression literals: clause 12.9.5, delimited by the goal the caller names (rows T24, T25).</summary>
public sealed class RegularExpressionTests
{
    [Theory]
    [InlineData("/a/g", 2)]
    [InlineData("/[/]/", 4)]
    [InlineData("/\\//", 3)]
    [InlineData("/a/\u0300", 2)]
    [InlineData("/a/", 2)]
    public void A_literal_is_delimited_and_its_body_and_flags_split(string text, int split)
    {
        var token = Lex.One(text);
        Assert.Equal(TokenKind.RegularExpressionLiteral, token.Kind);
        Assert.Equal((0, text.Length, split), (token.Start, token.End, token.Split));
    }

    [Fact]
    public void Flags_take_no_escape()
    {
        Assert.Equal([TokenKind.RegularExpressionLiteral, TokenKind.IdentifierName], Lex.Kinds("/a/\\u0067"));
    }

    [Theory]
    [InlineData("/a")]
    [InlineData("/a\n/")]
    [InlineData("/a\\\n/")]
    [InlineData("/[/\n]/")]
    public void A_literal_a_line_terminator_or_the_end_interrupts_is_refused_at_its_slash(string text)
    {
        var refusal = Lex.Refusal("x=" + text);
        Assert.Equal("JSE9106", refusal.Code);
        Assert.Equal(2, refusal.Span.Offset);
    }

    [Fact]
    public void The_goal_decides_and_the_tokenizer_reads_again_under_another()
    {
        var tokenizer = Lex.Over("a /b/g");
        Assert.Equal(TokenKind.IdentifierName, tokenizer.Next(LexicalGoal.RegExp).Kind);
        var state = tokenizer.Save();
        Assert.Equal(TokenKind.RegularExpressionLiteral, tokenizer.Next(LexicalGoal.RegExp).Kind);
        tokenizer.Restore(state);
        Assert.Equal([TokenKind.Slash, TokenKind.IdentifierName, TokenKind.Slash, TokenKind.IdentifierName],
            new[] { tokenizer.Next(LexicalGoal.Div), tokenizer.Next(LexicalGoal.Div), tokenizer.Next(LexicalGoal.Div), tokenizer.Next(LexicalGoal.Div) }.Select(static t => t.Kind));
        Assert.Equal(TokenKind.SlashEquals, Lex.Over("/=").Next(LexicalGoal.TemplateTail).Kind);
    }
}

/// <summary>Punctuators: clause 12.8, longest match (row T20), with <c>@</c> for the decorators proposal.</summary>
public sealed class PunctuatorTests
{
    [Theory]
    [InlineData("{ } ( ) [ ] . ... ; , < > <= >= == != === !== + - * ** % ++ -- << >> >>> & | ^ ! ~ && || ?? ? ?. : = += -= *= %= **= <<= >>= >>>= &= |= ^= &&= ||= ??= => @")]
    public void Every_punctuator_is_read_by_its_longest_match(string text)
    {
        var expected = new[]
        {
            TokenKind.LeftBrace, TokenKind.RightBrace, TokenKind.LeftParen, TokenKind.RightParen, TokenKind.LeftBracket,
            TokenKind.RightBracket, TokenKind.Dot, TokenKind.Ellipsis, TokenKind.Semicolon, TokenKind.Comma, TokenKind.LessThan,
            TokenKind.GreaterThan, TokenKind.LessThanEquals, TokenKind.GreaterThanEquals, TokenKind.EqualsEquals, TokenKind.NotEquals,
            TokenKind.EqualsEqualsEquals, TokenKind.NotEqualsEquals, TokenKind.Plus, TokenKind.Minus, TokenKind.Star, TokenKind.StarStar,
            TokenKind.Percent, TokenKind.PlusPlus, TokenKind.MinusMinus, TokenKind.LeftShift, TokenKind.RightShift,
            TokenKind.UnsignedRightShift, TokenKind.Ampersand, TokenKind.Bar, TokenKind.Caret, TokenKind.Bang, TokenKind.Tilde,
            TokenKind.AmpersandAmpersand, TokenKind.BarBar, TokenKind.QuestionQuestion, TokenKind.Question, TokenKind.QuestionDot,
            TokenKind.Colon, TokenKind.Equals, TokenKind.PlusEquals, TokenKind.MinusEquals, TokenKind.StarEquals, TokenKind.PercentEquals,
            TokenKind.StarStarEquals, TokenKind.LeftShiftEquals, TokenKind.RightShiftEquals, TokenKind.UnsignedRightShiftEquals,
            TokenKind.AmpersandEquals, TokenKind.BarEquals, TokenKind.CaretEquals, TokenKind.AmpersandAmpersandEquals,
            TokenKind.BarBarEquals, TokenKind.QuestionQuestionEquals, TokenKind.Arrow, TokenKind.At,
        };
        Assert.Equal(expected, Lex.Kinds(text, LexicalGoal.Div));
        Assert.Equal([TokenKind.UnsignedRightShiftEquals, TokenKind.Equals], Lex.Kinds(">>>==", LexicalGoal.Div));
        Assert.Equal([TokenKind.Dot, TokenKind.Dot, TokenKind.IdentifierName], Lex.Kinds("..a", LexicalGoal.Div));
        Assert.Equal([TokenKind.MinusMinus, TokenKind.Minus], Lex.Kinds("---", LexicalGoal.Div));
    }
}

/// <summary>Positions: 0-based offsets and 1-based lines and columns, with the source's base applied (rows N1 to N8; decision D15).</summary>
public sealed class PositionTests
{
    [Fact]
    public void Lines_and_columns_take_the_source_s_base_and_offsets_stay_the_text_s()
    {
        var source = new SourceText("a\n  b", "s.js", 100, 7, 9);
        var tokenizer = Lex.Over(source.Text, source: source);
        var a = tokenizer.Next(LexicalGoal.RegExp);
        var b = tokenizer.Next(LexicalGoal.RegExp);
        Assert.Equal((0, 7, 9), (a.Start, a.Line, a.Column));
        Assert.Equal((4, 8, 3), (b.Start, b.Line, b.Column));

        var refused = Lex.Over("x\n 'y", source: new SourceText("x\n 'y", "s.js", 100, 7, 9));
        refused.Next(LexicalGoal.RegExp);
        Assert.Equal(TokenKind.Invalid, refused.Next(LexicalGoal.RegExp).Kind);
        Assert.Equal(new SourceSpan(103, 2, 8, 2), refused.Diagnostic!.Span);
        Assert.Equal("s.js", refused.Diagnostic.SourceName);
    }

    [Fact]
    public void A_refusal_ends_the_stream_and_every_later_call_answers_it()
    {
        var tokenizer = Lex.Over("a \u0085 b");
        Assert.Equal(TokenKind.IdentifierName, tokenizer.Next(LexicalGoal.RegExp).Kind);
        var refusal = tokenizer.Next(LexicalGoal.RegExp);
        Assert.Equal(TokenKind.Invalid, refusal.Kind);
        Assert.Equal(-2, tokenizer.PeekTokenStart());
        Assert.Equal(refusal.Start, tokenizer.Next(LexicalGoal.Div).Start);
        Assert.Equal(TokenKind.Invalid, tokenizer.Next(LexicalGoal.Div).Kind);
    }

    [Fact]
    public void The_end_of_the_input_is_a_token_at_the_text_s_end()
    {
        var tokenizer = Lex.Over("a\n");
        tokenizer.Next(LexicalGoal.RegExp);
        var end = tokenizer.Next(LexicalGoal.RegExp);
        Assert.Equal((TokenKind.EndOfInput, 2, 2, 2, 1, true), (end.Kind, end.Start, end.End, end.Line, end.Column, end.NewLineBefore));
        Assert.Equal(-1, tokenizer.PeekTokenStart());
    }
}
