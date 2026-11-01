using System.Linq;
using Xunit;

namespace Broiler.ECMAScript.Syntax.Tests;

/// <summary>
/// The correspondence's rows T (Broiler.VM's bundle <c>sjl-13-001</c>, <c>correspondence.md</c>, section T) and the
/// owner's decision D-t24 of 2026-10-27 (JSD-0083), as the tokenizer answers them: each row's case with the specification's
/// answer, the goal named as the parser will name it (stage 4c).
/// </summary>
public sealed class CorrespondenceTests
{
    /// <summary>Row T6: a substitution's <c>${</c> is no text's start, so <c>#!</c> there is no hashbang.</summary>
    [Fact]
    public void T6_a_substitution_is_not_the_start_of_a_text()
    {
        var tokenizer = Lex.Over("`${#!\n1}`");
        Assert.Equal(TokenKind.TemplateHead, tokenizer.Next(LexicalGoal.RegExp).Kind);
        Assert.Equal(TokenKind.Invalid, tokenizer.Next(LexicalGoal.RegExp).Kind);
        Assert.Equal("JSE9101", tokenizer.Diagnostic!.Code);
    }

    /// <summary>Row T7: an HTML-like comment in a substitution is a comment, so a <c>}</c> inside it closes nothing.</summary>
    [Fact]
    public void T7_an_html_like_comment_in_a_substitution_hides_its_brace()
    {
        var tokenizer = Lex.Over("`${1 <!-- }\n}`");
        Assert.Equal(TokenKind.TemplateHead, tokenizer.Next(LexicalGoal.RegExp).Kind);
        Assert.Equal(TokenKind.NumericLiteral, tokenizer.Next(LexicalGoal.RegExp).Kind);
        var tail = tokenizer.Next(LexicalGoal.TemplateTail);
        Assert.Equal((TokenKind.TemplateTail, 12, string.Empty), (tail.Kind, tail.Start, tail.Value));
        Assert.True(tail.NewLineBefore);
    }

    /// <summary>Row T8: the regular expression after <c>typeof</c> in a substitution is the parser's goal, not a guess.</summary>
    [Fact]
    public void T8_a_literal_in_a_substitution_is_delimited_by_the_goal()
    {
        var tokenizer = Lex.Over("`${typeof /}/}`");
        Assert.Equal(TokenKind.TemplateHead, tokenizer.Next(LexicalGoal.RegExp).Kind);
        Assert.Equal(Word.Typeof, tokenizer.Next(LexicalGoal.RegExp).Word);
        var literal = tokenizer.Next(LexicalGoal.RegExp);
        Assert.Equal((TokenKind.RegularExpressionLiteral, 10, 13), (literal.Kind, literal.Start, literal.End));
        Assert.Equal(TokenKind.TemplateTail, tokenizer.Next(LexicalGoal.TemplateTail).Kind);
    }

    /// <summary>Rows T12 and T13: an escape's hex digits are ASCII hex digits, with no separator and no white space.</summary>
    [Theory]
    [InlineData("'\\u{0_41}'")]
    [InlineData("'\\x_1'")]
    [InlineData("'\\x 1'")]
    [InlineData("'\\x1 '")]
    [InlineData("'\\u 041'")]
    [InlineData("'\\x\n1'")]
    public void T12_T13_an_escape_holds_hex_digits_alone(string text)
    {
        Assert.StartsWith("JSE910", Lex.Refusal(text).Code);
    }

    /// <summary>Rows T15 and T16: legacy octal literals and escapes are recorded, for the parser to refuse in strict code, object keys and class element names included.</summary>
    [Theory]
    [InlineData("({ 010: 1 })", 2, (int)TokenFlags.LegacyOctal)]
    [InlineData("class C { 010() {} }", 3, (int)TokenFlags.LegacyOctal)]
    [InlineData("08.5", 0, (int)TokenFlags.NonOctalDecimal)]
    [InlineData("'\\01'", 0, (int)(TokenFlags.LegacyOctalEscape | TokenFlags.StringHasEscape))]
    public void T15_T16_legacy_forms_are_facts_for_the_parser(string text, int index, int flags)
    {
        var token = Lex.All(text)[index];
        Assert.Equal((TokenFlags)flags, token.Flags & ~TokenFlags.NewLineBefore);
    }

    /// <summary>
    /// Row T19: the digit lookahead after <c>?.</c> and <c>.</c> is ASCII's, so a raw U+0663 (ARABIC-INDIC DIGIT THREE)
    /// after <c>?.</c> leaves the <c>?.</c> whole, and begins no input element (EX0001). Its escaped spelling is another
    /// case: an escaped code point outside ID_Start at a name's start (EE0001).
    /// </summary>
    [Fact]
    public void T19_only_an_ascii_digit_makes_a_fraction()
    {
        Assert.Equal("IdentifierName QuestionDot Invalid", Lex.Names("a?.\u0663"));
        var raw = Lex.Refusal("a?.\u0663");
        Assert.Equal(("JSE9101", 3), (raw.Code, raw.Span.Offset));
        Assert.Equal("IdentifierName Question NumericLiteral", Lex.Names("a?.3"));

        Assert.Equal("IdentifierName QuestionDot Invalid", Lex.Names("a?.\\u0663"));
        var escaped = Lex.Refusal("a?.\\u0663");
        Assert.Equal(("JSE1201", 3), (escaped.Code, escaped.Span.Offset));
    }

    /// <summary>Row T21: a private name is one token, and a <c>#</c> before white space begins none.</summary>
    [Fact]
    public void T21_a_private_name_is_one_token()
    {
        Assert.Equal("PrivateIdentifier", Lex.Names("#x"));
        Assert.Equal("JSE9101", Lex.Refusal("# x").Code);
    }

    /// <summary>Rows T23, T31 and T32: an escaped word is an identifier name whose word, escape and spelling are each kept.</summary>
    [Fact]
    public void T23_T31_T32_an_escaped_word_keeps_its_word_its_flag_and_its_spelling()
    {
        var token = Lex.One("impl\\u0065ments");
        Assert.Equal((TokenKind.IdentifierName, "implements", Word.Implements, true, 0, 15), (token.Kind, token.Value, token.Word, token.IsEscaped, token.Start, token.End));
        var of = Lex.One("\\u006ff");
        Assert.Equal((Word.Of, true), (of.Word, of.IsEscaped));
    }

    /// <summary>
    /// Rows T24a to T24i and the owner's decision D-t24: whether a <c>/</c> divides or begins a literal is the goal the
    /// parser names, never the tokenizer's reading of what came before; where the specification divides (after a line
    /// terminator that follows an operand, after a <c>}</c> that closes an expression, after <c>await</c> used as a name),
    /// the parser names <see cref="LexicalGoal.Div"/>, and the tokens are the division the specification reads.
    /// </summary>
    [Theory]
    [InlineData("a\n/b/g", 1)]
    [InlineData("var x = {} /b/g; x", 5)]
    [InlineData("var x = function () {} /b/g; x", 8)]
    [InlineData("var await = 6, b = 2, g = 3; await /b/g", 14)]
    public void T24_and_D_t24_a_slash_the_parser_reads_as_division_divides(string text, int slashIndex)
    {
        var tokenizer = Lex.Over(text);
        var kinds = new System.Collections.Generic.List<TokenKind>();
        while (true)
        {
            var token = tokenizer.Next(kinds.Count == slashIndex || kinds.Count == slashIndex + 2 ? LexicalGoal.Div : LexicalGoal.RegExp);
            if (token.Kind == TokenKind.EndOfInput)
                break;
            kinds.Add(token.Kind);
        }

        Assert.Equal(TokenKind.Slash, kinds[slashIndex]);
        Assert.Equal(TokenKind.Slash, kinds[slashIndex + 2]);
        Assert.DoesNotContain(TokenKind.RegularExpressionLiteral, kinds);
    }

    /// <summary>Rows T24d to T24f: where the specification begins a literal (after a <c>for await</c> head, a prefix <c>++</c>, <c>await</c> as a name), the parser names the regular-expression goal.</summary>
    [Theory]
    [InlineData("++/x/.lastIndex", 1)]
    [InlineData("for await (x of y) /z/.test(x)", 7)]
    public void T24_a_slash_the_parser_reads_as_a_literal_begins_one(string text, int index)
    {
        var tokenizer = Lex.Over(text);
        for (var i = 0; i < index; i++)
            tokenizer.Next(LexicalGoal.Div);
        Assert.Equal(TokenKind.RegularExpressionLiteral, tokenizer.Next(LexicalGoal.RegExp).Kind);
    }

    /// <summary>Row T26: a literal's body is its source text, braced escapes kept as written.</summary>
    [Fact]
    public void T26_a_literal_s_body_is_its_spelling()
    {
        var text = "/\\u{1F600}/u";
        var token = Lex.One(text);
        Assert.Equal("\\u{1F600}", text[(token.Start + 1)..token.Split]);
        Assert.Equal("u", text[(token.Split + 1)..token.End]);
    }

    /// <summary>Row T28: a template piece's raw text keeps every brace but the one that closes the substitution.</summary>
    [Fact]
    public void T28_raw_text_keeps_its_braces()
    {
        var tokenizer = Lex.Over("`${1}}}x${2}}y`");
        Assert.Equal(TokenKind.TemplateHead, tokenizer.Next(LexicalGoal.RegExp).Kind);
        tokenizer.Next(LexicalGoal.RegExp);
        var middle = tokenizer.Next(LexicalGoal.TemplateTail);
        Assert.Equal((TokenKind.TemplateMiddle, "}}x", "}}x"), (middle.Kind, middle.Raw, middle.Value));
        tokenizer.Next(LexicalGoal.RegExp);
        var tail = tokenizer.Next(LexicalGoal.TemplateTail);
        Assert.Equal((TokenKind.TemplateTail, "}y"), (tail.Kind, tail.Raw));
    }

    /// <summary>The lexical vectors' sources the tokenizer refuses agree with what the vectors expect (the parse entry's answer, over the corpus's own sources).</summary>
    [Theory]
    [InlineData("var \\u0030a;")]
    [InlineData("1_;")]
    [InlineData("\"\\u{110000}\";")]
    [InlineData("#!/x\n#!/y")]
    public void A_lexical_vector_s_refused_source_is_refused(string text)
    {
        var result = SyntaxParser.Parse(new ParseRequest(new SourceText(text), ParseGoal.Script, Runners.UnicodeDataHome.Tables, NoValidator.Instance));
        Assert.Equal(ParseOutcome.Refused, result.Outcome);
        Assert.Equal(DiagnosticPhase.Tokenizer, result.Diagnostics.Single().Phase);
    }
}
