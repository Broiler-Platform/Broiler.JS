using Broiler.Regex;

namespace Broiler.Regex.Tests;

public class SyntaxTests
{
    [Theory]
    [InlineData("(")]            // unterminated group
    [InlineData("[a")]           // unterminated class
    [InlineData("a{2,1}")]       // m < n
    [InlineData("\\k<x>")]       // reference to non-existent named group
    public void InvalidPatterns_Throw(string pattern)
    {
        Assert.Throws<RegexSyntaxException>(() => new BroilerRegex(pattern));
    }

    [Theory]
    [InlineData("gg")]           // duplicate flag
    [InlineData("uv")]           // u and v together
    [InlineData("q")]            // unknown flag
    public void InvalidFlags_Throw(string flags)
    {
        Assert.Throws<RegexSyntaxException>(() => new BroilerRegex("a", flags));
    }

    [Fact]
    public void ValidFlags_AreParsed()
    {
        var re = new BroilerRegex("a", "gimsuyd");
        Assert.True((re.Flags & RegexFlags.Global) != 0);
        Assert.True((re.Flags & RegexFlags.IgnoreCase) != 0);
        Assert.True((re.Flags & RegexFlags.Unicode) != 0);
    }
}
