using Broiler.JavaScript.Vectors.Tests.Corpus;

namespace Broiler.JavaScript.Vectors.Tests;

public class TypedValueTests
{
    [Theory]
    [InlineData("f64:0x3FF0000000000000")]
    [InlineData("f64:0x8000000000000000")]
    [InlineData("f64:0x7FF8000000000000")]
    [InlineData("str:\"\"")]
    [InlineData("str:\"a | b\"")]
    [InlineData("str:\"\\\"\\\\\"")]
    [InlineData("bigint:0")]
    [InlineData("bigint:-123")]
    [InlineData("int:36")]
    [InlineData("bool:true")]
    [InlineData("undefined")]
    [InlineData("null")]
    public void A_canonical_value_is_accepted(string text)
    {
        Assert.True(TypedValue.TryParse(text, out var value, out _, out var message), message);
        Assert.Equal(text, value.Text);
    }

    [Theory]
    [InlineData("f64:0x3ff0000000000000", "bad-double")]
    [InlineData("f64:0x3FF000000000000", "bad-double")]
    [InlineData("f64:0x7FF8000000000001", "bad-double")]
    [InlineData("f64:0xFFF8000000000000", "bad-double")]
    [InlineData("str:\"a", "bad-string")]
    [InlineData("str:\"\\x41\"", "bad-string")]
    [InlineData("bigint:-0", "bad-value")]
    [InlineData("bigint:007", "bad-value")]
    [InlineData("int:+1", "bad-value")]
    [InlineData("bool:1", "bad-value")]
    [InlineData("1.5", "bad-value")]
    public void A_malformed_or_non_canonical_value_is_refused(string text, string code)
    {
        Assert.False(TypedValue.TryParse(text, out _, out var actual, out _));
        Assert.Equal(code, actual);
    }

    [Fact]
    public void A_string_has_one_written_form()
    {
        // The escape of a printable character is refused: it has a shorter form, the character itself.
        var escapedOne = "str:\"" + '\\' + "u0031\"";
        Assert.False(TypedValue.TryParse(escapedOne, out _, out var code, out _));
        Assert.Equal("bad-string", code);

        // Lowercase hexadecimal in an escape is refused too.
        Assert.False(TypedValue.TryParse("str:\"" + '\\' + "u00a0\"", out _, out _, out _));

        // A lone surrogate is a code unit like any other.
        var lone = "str:\"" + '\\' + "uD800\"";
        Assert.True(TypedValue.TryParse(lone, out var value, out _, out var message), message);
        Assert.Equal("\uD800", value.Chars);
        Assert.Equal(lone, TypedValue.RenderString(value.Chars));
    }

    [Fact]
    public void Rendering_writes_every_NaN_and_both_zeros_by_their_canonical_bits()
    {
        Assert.Equal("f64:0x7FF8000000000000", TypedValue.RenderNumber(BitConverter.UInt64BitsToDouble(0xFFF0000000000001)));
        Assert.Equal("f64:0x8000000000000000", TypedValue.RenderNumber(-0.0));
        Assert.Equal("f64:0x0000000000000000", TypedValue.RenderNumber(0.0));
        Assert.Equal("str:\"" + '\\' + "u000A" + '\\' + "\"\"", TypedValue.RenderString("\n\""));
    }

    [Fact]
    public void Alternatives_are_read_value_by_value_so_a_string_may_hold_the_separator()
    {
        Assert.True(Outcome.TryParseAlternatives("value: str:\"a | b\" | value: str:\"c\"", out var outcomes, out _, out var message), message);
        Assert.Equal(["value: str:\"a | b\"", "value: str:\"c\""], outcomes.Select(o => o.Text));

        Assert.True(Outcome.TryParseAlternatives("error: SyntaxError", out outcomes, out _, out message), message);
        Assert.Equal("SyntaxError", Assert.Single(outcomes).Name);

        Assert.False(Outcome.TryParseAlternatives("error: Oops", out _, out _, out _));
        Assert.False(Outcome.TryParseAlternatives("value: int:1", out _, out _, out _));
        Assert.False(Outcome.TryParseAlternatives("value: str:\"1\" | value: str:\"1\"", out _, out _, out _));
        Assert.False(Outcome.TryParseAlternatives("value: str:\"1\" |value: str:\"2\"", out _, out _, out _));
    }
}
