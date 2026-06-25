using Broiler.Regex;

namespace Broiler.Regex.Tests;

/// <summary>Baseline coverage of the common ECMAScript regex grammar.</summary>
public class CoreTests
{
    [Fact]
    public void Literal_FindsFirstOccurrence()
    {
        var m = new BroilerRegex("cat").Match("a cat sat");
        Assert.True(m.Success);
        Assert.Equal(2, m.Index);
        Assert.Equal("cat", m.Value);
    }

    [Fact]
    public void Alternation()
    {
        Assert.Equal("b", new BroilerRegex("a|b").Match("zb").Value);
    }

    [Fact]
    public void CharacterClass_RangeAndNegation()
    {
        Assert.Equal("abc", new BroilerRegex("[a-c]+").Match("xabcy").Value);
        Assert.Equal("x", new BroilerRegex("[^a-c]").Match("abcx").Value);
    }

    [Fact]
    public void Quantifiers_Greedy_Lazy_Bounds()
    {
        Assert.Equal("123", new BroilerRegex("\\d{2,3}").Match("12345").Value);
        Assert.Equal("aaa", new BroilerRegex("a+").Match("aaab").Value);
        Assert.Equal("a", new BroilerRegex("a+?").Match("aaab").Value);
        Assert.Equal("color", new BroilerRegex("colou?r").Match("color").Value);
    }

    [Fact]
    public void Anchors()
    {
        Assert.True(new BroilerRegex("^abc$").Match("abc").Success);
        Assert.False(new BroilerRegex("^abc$").Match("xabc").Success);
    }

    [Fact]
    public void Multiline_AnchorsMatchAtLineBreaks()
    {
        var m = new BroilerRegex("^b", "m").Match("a\nb");
        Assert.True(m.Success);
        Assert.Equal(2, m.Index);
    }

    [Fact]
    public void DotAll_DotMatchesNewline()
    {
        Assert.True(new BroilerRegex("a.b", "s").Match("a\nb").Success);
        Assert.False(new BroilerRegex("a.b").Match("a\nb").Success);
    }

    [Fact]
    public void WordBoundary()
    {
        var m = new BroilerRegex("\\bword\\b").Match("a word here");
        Assert.True(m.Success);
        Assert.Equal(2, m.Index);
        Assert.False(new BroilerRegex("\\bword\\b").Match("password").Success);
    }

    [Fact]
    public void NamedGroups_ExposedByName()
    {
        var re = new BroilerRegex("(?<year>\\d{4})-(?<month>\\d{2})");
        var m = re.Match("2026-06");
        Assert.True(m.Success);
        Assert.Equal("2026", m.NamedGroups["year"].Value);
        Assert.Equal("06", m.NamedGroups["month"].Value);
        Assert.Equal(2, re.CaptureCount);
    }

    [Fact]
    public void Lookahead_PositiveAndNegative()
    {
        Assert.True(new BroilerRegex("a(?=b)").Match("ab").Success);
        Assert.False(new BroilerRegex("a(?=b)").Match("ac").Success);
        Assert.True(new BroilerRegex("a(?!b)").Match("ac").Success);
    }

    [Fact]
    public void Sticky_AnchorsAtStart()
    {
        Assert.False(new BroilerRegex("a", "y").Match("ba").Success);
        Assert.True(new BroilerRegex("a", "y").Match("ab").Success);
    }

    [Fact]
    public void IgnoreCase_Ascii()
    {
        Assert.True(new BroilerRegex("abc", "i").Match("ABC").Success);
    }

    [Fact]
    public void Global_EnumeratesAllMatches()
    {
        var matches = new List<string>();
        foreach (var m in new BroilerRegex("a").Matches("aba"))
            matches.Add(m.Value);
        Assert.Equal(new[] { "a", "a" }, matches);
    }

    [Fact]
    public void EmptyPattern_MatchesEmpty()
    {
        var m = new BroilerRegex("").Match("abc");
        Assert.True(m.Success);
        Assert.Equal(0, m.Length);
    }

    [Fact]
    public void InlineModifier_IgnoreCaseRegion()
    {
        // (?i:b) is case-insensitive; the surrounding 'a' is not.
        var re = new BroilerRegex("a(?i:b)c");
        Assert.True(re.Match("aBc").Success);
        Assert.False(re.Match("Abc").Success);
    }
}
