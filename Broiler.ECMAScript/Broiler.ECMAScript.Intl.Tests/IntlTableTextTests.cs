using System;
using System.IO;
using System.Text;
using Xunit;

namespace Broiler.ECMAScript.Intl.Tests;

/// <summary>The text tables' readers, ported from Broiler.VM's <c>JsIntlTables</c> (<c>Unescape</c>, <c>Lines</c>, <c>Pairs</c>).</summary>
public sealed class IntlTableTextTests
{
    [Fact]
    public void Unescape_resolves_each_escape_to_the_code_unit_it_names()
    {
        Assert.Equal("a\u00A0b", IntlTableText.Unescape("a\\u00A0b"));
        Assert.Equal("\u00E9\u00E9", IntlTableText.Unescape("\\u00e9\\u00E9"));
        Assert.Equal("\uD83A\uDD50", IntlTableText.Unescape("\\uD83A\\uDD50"));
        Assert.Equal("{0}\u202F{1}", IntlTableText.Unescape("{0}\\u202F{1}"));
    }

    [Fact]
    public void Unescape_leaves_a_value_without_an_escape_as_it_is()
    {
        const string plain = "#,##0.###";
        Assert.Same(plain, IntlTableText.Unescape(plain));
        Assert.Equal(string.Empty, IntlTableText.Unescape(string.Empty));
    }

    [Fact]
    public void Unescape_keeps_a_backslash_that_starts_no_full_escape()
    {
        // Broiler.VM's reader resolves an escape only when its four digits fit before the end, and only after 'u'.
        Assert.Equal("a\\u12", IntlTableText.Unescape("a\\u12"));
        Assert.Equal("\\x0041", IntlTableText.Unescape("\\x0041"));
        Assert.Equal("\\u004", IntlTableText.Unescape("\\u004"));
    }

    [Fact]
    public void Unescape_refuses_an_escape_that_is_not_hexadecimal()
    {
        Assert.Throws<FormatException>(() => IntlTableText.Unescape("\\uZZZZ"));
    }

    [Fact]
    public void Lines_splits_at_LF_and_leaves_out_the_empty_lines()
    {
        Assert.Equal(["a|b", "c|d"], IntlTableText.Lines(Encoding.ASCII.GetBytes("a|b\n\nc|d\n")));
        Assert.Empty(IntlTableText.Lines(default));
    }

    [Fact]
    public void Pairs_maps_the_text_before_the_first_bar_to_the_rest_and_a_later_line_wins()
    {
        var pairs = IntlTableText.Pairs(Encoding.ASCII.GetBytes("en|en-Latn-US\nund|en-Latn-US|x\nen|later"), IntlTable.LikelySubtags);
        Assert.Equal("later", pairs["en"]);
        Assert.Equal("en-Latn-US|x", pairs["und"]);
    }

    [Fact]
    public void A_line_short_of_its_table_s_fields_is_refused()
    {
        Assert.Throws<InvalidDataException>(() => IntlTableText.Pairs(Encoding.ASCII.GetBytes("en"), IntlTable.LikelySubtags));
        Assert.Throws<InvalidDataException>(() => IntlTableText.Fields("a|b", 3, IntlTable.Aliases));
        Assert.Equal(["a", "b", "c", "d"], IntlTableText.Fields("a|b|c|d", 3, IntlTable.Aliases));
    }
}
