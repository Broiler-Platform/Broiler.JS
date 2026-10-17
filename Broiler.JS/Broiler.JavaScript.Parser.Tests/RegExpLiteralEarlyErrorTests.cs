using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Parser;

namespace Broiler.JavaScript.Parser.Tests;

// A regular-expression literal's early errors (ECMA-262 22.2.1.1) that the parser did not check, though the
// RegExp constructor refused most of them: its group names, a negated class that may contain strings under v,
// and a class escape at a range's end under u. Broiler.VM's bundle sjl-7-001 and this repository's record them
// as rows of test262 that failed before and after milestone SJL-7's cut-over.
public class RegExpLiteralEarlyErrorTests
{
    private static FastParseException ParseError(string source)
        => Assert.Throws<FastParseException>(
            () => new FastParser(new FastTokenStream(new StringSpan(source))).ParseProgram());

    private static Ast.AstProgram Parse(string source)
        => new FastParser(new FastTokenStream(new StringSpan(source))).ParseProgram();

    // A group name is a RegExpIdentifierName: its first code point ID_Start, `$` or `_`, the rest ID_Continue,
    // `$`, ZWNJ or ZWJ, by Broiler.Unicode's EcmaScriptLexical as an identifier's (test262
    // language/literals/regexp/named-groups/invalid-*).
    [Theory]
    [InlineData("/(?<42a>a)/")]
    [InlineData("/(?<42a>a)/u")]
    [InlineData("/(?<:a>a)/")]
    [InlineData("/(?<a:>a)/u")]
    [InlineData("/(?<❤>a)/")]                // HEAVY BLACK HEART, So
    [InlineData("/(?<\U000104A4>a)/u")]           // OSMANYA DIGIT FOUR, ID_Continue but not ID_Start
    [InlineData("/(?<$❞>a)/")]               // a continuation that is not ID_Continue
    [InlineData("/(?<a\\uD83D\\uDF12>.)/")]       // U+1F712, So
    [InlineData("/(?<a\\uD801>.)/u")]             // a lone lead surrogate
    [InlineData("/(?<a\\uDCA4>.)/")]              // a lone trail surrogate
    [InlineData("/(?<a\\u{1F08B}>.)/")]
    [InlineData("/(?<a\\u{10FFFF}>.)/")]
    [InlineData("/(?<a\\>.)/")]
    [InlineData("/(?<\\>.)/")]
    [InlineData("/(?<>a)/")]
    public void A_group_name_that_is_no_identifier_name_is_an_early_error(string source) => ParseError(source);

    // Two groups of one alternative may not share a name; separate alternatives may (ES2025).
    [Theory]
    [InlineData("/(?<a>a)(?<a>a)/")]
    [InlineData("/(?<a>a)(?<b>b)(?<a>a)/u")]
    [InlineData("/(?<a>a)((?<a>b))/")]
    public void A_name_declared_twice_in_one_alternative_is_an_early_error(string source) => ParseError(source);

    // With a group name, or under u, `\k` is a complete `\k<GroupName>`, inside a class too.
    [Theory]
    [InlineData("/(?<a>.)\\k/")]
    [InlineData("/\\k(?<a>.)/")]
    [InlineData("/(?<a>.)\\k<a/")]
    [InlineData("/(?<a>.)\\k<1>/")]
    [InlineData("/(?<a>.)[\\k]/")]
    public void An_incomplete_named_reference_is_an_early_error(string source) => ParseError(source);

    [Theory]
    [InlineData("/(?<a>a)|(?<a>b)/")]
    [InlineData("/(?:(?<a>a)|(?<a>b))\\k<a>/")]
    [InlineData("/(?<$>a)(?<_‌‍>b)/")]
    [InlineData("/(?<\\u{1d4d1}>a)/u")]           // MATHEMATICAL BOLD SCRIPT SMALL B, ID_Start
    [InlineData("/(?<\U0001D4D1>a)/")]
    [InlineData("/\\k/")]                         // Annex B: no group name and no u, so `\k` is the letter
    [InlineData("/\\k<a>/")]
    [InlineData("/(?<=a)\\k/")]                   // a lookbehind declares no name
    [InlineData("/[(?<a>.)]\\k/")]                // nor does a class
    public void Valid_group_names_and_references_are_read(string source) => Parse(source);

    // Under v a negated class may not contain strings (MayContainStrings): a union may when an operand may,
    // an intersection only when every operand may, a subtraction when its first operand may (test262
    // built-ins/RegExp/property-escapes/generated/strings/*-negative-CharacterClass.js).
    [Theory]
    [InlineData(@"/[^\p{RGI_Emoji}]/v")]
    [InlineData(@"/[^\p{Basic_Emoji}]/v")]
    [InlineData(@"/[^\p{Emoji_Keycap_Sequence}]/v")]
    [InlineData(@"/[^a\p{RGI_Emoji_Flag_Sequence}]/v")]
    [InlineData(@"/[^\q{ab}]/v")]
    [InlineData(@"/[^\q{}]/v")]
    [InlineData(@"/[^\q{a|bc}]/v")]
    [InlineData(@"/[^[\p{RGI_Emoji}]]/v")]
    [InlineData(@"/[^\p{RGI_Emoji}--a]/v")]
    [InlineData(@"/[^\p{RGI_Emoji}&&\q{ab}]/v")]
    [InlineData(@"/[a[^\p{RGI_Emoji}]]/v")]
    public void A_negated_class_that_may_contain_strings_is_an_early_error(string source) => ParseError(source);

    [Theory]
    [InlineData(@"/[^\p{RGI_Emoji}&&a]/v")]
    [InlineData(@"/[^a&&\p{RGI_Emoji}]/v")]
    [InlineData(@"/[^a--\p{RGI_Emoji}]/v")]
    [InlineData(@"/[^\q{a|b}]/v")]
    [InlineData(@"/[^\q{a|😀}]/v")]
    [InlineData(@"/[^[^\p{RGI_Emoji}&&a]]/v")]
    [InlineData(@"/[^[\p{RGI_Emoji}&&a]]/v")]
    [InlineData(@"/[^\p{Lu}a-z]/v")]
    [InlineData(@"/[\p{RGI_Emoji}]/v")]
    [InlineData(@"/[^\p{Script=Latin}]/v")]
    public void A_negated_class_that_contains_no_string_is_read(string source) => Parse(source);

    // Under u neither end of a class range is a class escape, a property escape included (test262
    // built-ins/RegExp/property-escapes/character-class-range-*.js).
    [Theory]
    [InlineData(@"/[--\p{Hex}]/u")]
    [InlineData(@"/[\p{Hex}-￿]/u")]
    [InlineData(@"/[￿-\p{Hex}]/u")]
    [InlineData(@"/[\p{Hex}--]/u")]
    [InlineData(@"/[a-\d]/u")]
    [InlineData(@"/[\w-a]/u")]
    public void A_class_escape_at_a_range_s_end_is_an_early_error(string source) => ParseError(source);

    [Theory]
    [InlineData(@"/[\p{Hex}-]/u")]
    [InlineData(@"/[-\p{Hex}]/u")]
    [InlineData(@"/[\w-]/u")]
    [InlineData(@"/[a\-\d]/u")]
    [InlineData(@"/[\--\-]/u")]
    [InlineData(@"/[\p{Hex}\-a]/u")]
    [InlineData(@"/[a-z\d]/u")]
    [InlineData(@"/[\p{Hex}-￿]/")]           // Annex B: without u the range is not refused
    public void A_class_without_such_a_range_is_read(string source) => Parse(source);
}
