namespace Broiler.JavaScript.BuiltIns.Tests;

// The RegExp constructor makes the same group-name and class-range checks as the parser's early check of a
// literal (Broiler.JavaScript.Parser.Tests' RegExpLiteralEarlyErrorTests), since both call one implementation.
// Two of them were not made here either: an incomplete `\k` where group names are parsed, and a property
// escape at a class range's end under u. And a class escape after an escaped `-`, which is no range, was refused.
public class RegExpConstructorEarlyErrorTests
{
    private static string ErrorName(string expression) => SharedUnicodeTests.ErrorName(expression);

    [Theory]
    [InlineData("new RegExp('(?<a>.)\\\\k')")]
    [InlineData("new RegExp('\\\\k(?<a>.)')")]
    [InlineData("new RegExp('(?<a>.)\\\\k<1>')")]
    [InlineData("new RegExp('(?<a>a)(?<a>b)')")]
    [InlineData("new RegExp('(?<42>a)')")]
    [InlineData("new RegExp('[--\\\\p{Hex}]', 'u')")]
    [InlineData("new RegExp('[\\\\p{Hex}-\\\\uFFFF]', 'u')")]
    [InlineData("new RegExp('[^\\\\p{RGI_Emoji}]', 'v')")]
    public void An_early_error_is_a_SyntaxError(string expression)
        => Assert.Equal("SyntaxError", ErrorName(expression));

    [Theory]
    [InlineData("new RegExp('\\\\k')")]
    [InlineData("new RegExp('\\\\k<a>')")]
    [InlineData("new RegExp('(?<a>a)|(?<a>b)')")]
    [InlineData("new RegExp('(?<a>.)\\\\k<a>')")]
    [InlineData("new RegExp('[a\\\\-\\\\d]', 'u')")]
    [InlineData("new RegExp('[\\\\p{Hex}-]', 'u')")]
    [InlineData("new RegExp('[^\\\\p{RGI_Emoji}&&a]', 'v')")]
    // Row S5's class case (Broiler.VM's JSD-0081, decision D-s5): without u, B.1.2 reads `\k` in a class beside a
    // named group as the letter, which this engine refused until milestone SJL-11.5, a correction of Broiler.JS.
    [InlineData("new RegExp('(?<a>.)[\\\\k]')")]
    public void A_valid_pattern_is_built(string expression)
        => Assert.Equal("no error", ErrorName(expression));

    [Fact]
    public void A_class_escape_beside_an_escaped_dash_matches_each()
        => Assert.Equal("true,true,true,false", SharedUnicodeTests.Eval(
            "var r = /^[a\\-\\d]$/u; [r.test('a'), r.test('-'), r.test('7'), r.test('b')].join()"));
}
