using Broiler.JavaScript.Ast.Misc;

namespace Broiler.JavaScript.Ast.Tests;

// StringSpan's trims are obsolete forwarders to the shared white-space set (milestone SJL-7.5, decision
// SJL-D-25 (b)): StrWhiteSpaceChar, the specification's WhiteSpace and LineTerminator, where they trimmed
// .NET's char.IsWhiteSpace, which holds U+0085 and not U+FEFF.
#pragma warning disable CS0618 // the members under test are obsolete
public class StringSpanTrimTests
{
    [Fact]
    public void Trim_removes_StrWhiteSpaceChar_from_both_ends_of_the_view()
    {
        var span = new StringSpan("[\uFEFF\u3000 a b\t\u2028]", 1, 8);

        var trimmed = span.Trim();

        Assert.Equal("a b", trimmed.Value);
        Assert.Equal(4, trimmed.Offset);
    }

    [Fact]
    public void TrimStart_and_TrimEnd_trim_one_end()
    {
        var span = new StringSpan("\uFEFF x \u00A0");

        Assert.Equal("x \u00A0", span.TrimStart().Value);
        Assert.Equal("\uFEFF x", span.TrimEnd().Value);
    }

    // U+0085 is in neither WhiteSpace nor LineTerminator (correspondence row W1).
    [Fact]
    public void U0085_is_not_trimmed()
        => Assert.Equal("\u0085x\u0085", new StringSpan(" \u0085x\u0085 ").Trim().Value);

    [Fact]
    public void A_span_of_white_space_trims_to_empty()
    {
        Assert.Equal(0, new StringSpan(" \t\n").Trim().Length);
        Assert.Equal(0, StringSpan.Empty.Trim().Length);
    }
}
#pragma warning restore CS0618
