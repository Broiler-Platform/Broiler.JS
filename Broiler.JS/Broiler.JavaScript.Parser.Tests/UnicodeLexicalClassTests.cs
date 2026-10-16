using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Parser;

namespace Broiler.JavaScript.Parser.Tests;

// The scanner's lexical classes are Broiler.Unicode's over this engine's pinned Unicode 17.0.0 tables
// (milestone SJL-7.5; Broiler.VM's bundle sjl-7-001, correspondence rows I1, I2 and W1). IdentifierStartChar
// is ID_Start, which subtracts Pattern_Syntax (UAX #31), where the scanner read the runtime's General_Category
// with hand lists and Unicode 17 patches; WhiteSpace is the specification's, where it was .NET's
// char.IsWhiteSpace with U+FEFF added, which held U+0085.
public class UnicodeLexicalClassTests
{
    private static FastParseException ParseError(string source)
        => Assert.Throws<FastParseException>(
            () => new FastParser(new FastTokenStream(new StringSpan(source))).ParseProgram());

    private static Ast.AstProgram Parse(string source)
        => new FastParser(new FastTokenStream(new StringSpan(source))).ParseProgram();

    // I1: U+2E2F VERTICAL TILDE is Lm and Pattern_Syntax, so it is neither ID_Start nor ID_Continue.
    [Theory]
    [InlineData("var \u2E2F = 1;")]
    [InlineData("var \\u2E2F = 1;")]
    [InlineData("var \\u{2E2F} = 1;")]
    [InlineData("var a\u2E2F = 1;")]
    [InlineData("var a\\u2E2F = 1;")]
    public void Pattern_syntax_is_no_identifier_character(string source) => ParseError(source);

    // W1: U+0085 is Cc, in neither WhiteSpace nor LineTerminator.
    [Theory]
    [InlineData("1\u0085+1")]
    [InlineData("var\u0085a = 1;")]
    public void U0085_is_not_white_space(string source) => ParseError(source);

    // U+FEFF and every Space_Separator are WhiteSpace; the line terminators still end a line.
    [Theory]
    [InlineData("var\uFEFFa = 1;")]
    [InlineData("var\u3000a = 1;")]
    [InlineData("var\u202Fa = 1;")]
    [InlineData("var\u000Ba\u000C= 1;")]
    [InlineData("var a = 1\u2028var b = 2")]
    [InlineData("var a = 1\u2029var b = 2")]
    public void The_specification_s_white_space_and_line_terminators_are_read(string source) => Parse(source);

    // I2: identifiers of Unicode 14, 16 and 17, and the Other_ID_Start and Other_ID_Continue code points,
    // from the pinned data rather than the runtime's.
    [Theory]
    [InlineData("var \u0870 = 1;")]               // ARABIC LETTER ALEF WITH ATTACHED FATHA (14.0)
    [InlineData("var \U00016D40 = 1;")]          // KIRAT RAI SIGN ANUSVARA (16.0, Lm)
    [InlineData("var a\U00011B60 = 1;")]         // SHARADA VOWEL SIGN OE (17.0, ID_Continue)
    [InlineData("var \U00016EA0 = 1;")]          // BERIA ERFE CAPITAL LETTER ARKAB (17.0)
    [InlineData("var \u2118\u212E\u309B = 1;")]  // Other_ID_Start
    [InlineData("var a\u00B7\u0387\u1369\u19DA\u200C\u200D = 1;")] // Other_ID_Continue, ZWNJ and ZWJ
    [InlineData("var \\u{16D40} = 1;")]
    [InlineData("var \u1885\u1886 = 1;")]
    public void Unicode_17_identifiers_are_read(string source) => Parse(source);

    // An identifier start right after a numeric literal is still refused.
    [Theory]
    [InlineData("3in []")]
    [InlineData("3\u0870")]
    public void An_identifier_start_after_a_number_is_refused(string source) => ParseError(source);

    // P1 to P3 and S2, at parse time: a regular-expression literal under u or v whose property escape
    // names what ECMA-262's tables do not hold, exactly as written, is an early SyntaxError (22.2.1.1),
    // checked by Broiler.Unicode's lookup, where the lexer replaced every name with a placeholder.
    [Theory]
    [InlineData(@"/\p{letter}/u")]
    [InlineData(@"/\p{lu}/u")]
    [InlineData(@"/\p{GC=Lu}/u")]
    [InlineData(@"/\p{Script_Extensions=greek}/u")]
    [InlineData(@"/\p{IsGreek}/u")]
    [InlineData(@"/\p{RGI_Emoji}/u")]
    [InlineData(@"/\p{rgiemoji}/v")]
    [InlineData(@"/\P{RGI_Emoji}/v")]
    [InlineData(@"/[\p{lowercase_letter}]/v")]
    public void A_property_name_ECMA_262_does_not_hold_is_an_early_error(string source) => ParseError(source);

    [Theory]
    [InlineData(@"/\p{Lu}/u")]
    [InlineData(@"/\p{General_Category=Uppercase_Letter}/u")]
    [InlineData(@"/\p{Script=Katakana_Or_Hiragana}/u")]
    [InlineData(@"/\P{scx=Grek}/u")]
    [InlineData(@"/\p{RGI_Emoji}/v")]
    [InlineData(@"/[\p{Basic_Emoji}--\q{x}]/v")]
    [InlineData(@"/\p{letter}/")]
    [InlineData(@"/\\p/u")]
    public void A_property_name_ECMA_262_holds_is_read(string source) => Parse(source);
}
