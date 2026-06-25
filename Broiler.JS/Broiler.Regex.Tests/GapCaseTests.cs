using Broiler.Regex;

namespace Broiler.Regex.Tests;

/// <summary>
/// Tests for the ECMAScript / .NET semantic gaps from issue #923 — the cases the
/// source-to-source translator into System.Text.RegularExpressions cannot fix.
/// Expected values are the JavaScript-correct results.
/// </summary>
public class GapCaseTests
{
    // ----- #8 Nullable quantifier (RepeatMatcher empty-iteration guard) -------

    [Fact]
    public void NullableQuantifier_Terminates_AndMatchesGreedily()
    {
        // Without the empty-match guard this either loops forever or matches short.
        var re = new BroilerRegex("(a*)*");
        var m = re.Match("aaa");
        Assert.True(m.Success);
        Assert.Equal("aaa", m.Value);
    }

    [Fact]
    public void NullableQuantifier_EmptyAlternative_Terminates()
    {
        var re = new BroilerRegex("(a|)*");
        var m = re.Match("aa");
        Assert.True(m.Success);
        Assert.Equal("aa", m.Value);
    }

    [Fact]
    public void NullableQuantifier_OptionalGroup_DoesNotInfiniteLoop()
    {
        var re = new BroilerRegex("(?:a?)*b");
        var m = re.Match("aaab");
        Assert.True(m.Success);
        Assert.Equal("aaab", m.Value);
    }

    // ----- #3 / #4 Look-behind with captures and back-references --------------

    [Fact]
    public void Lookbehind_PreservesSourceCaptureOrder()
    {
        // Matched right-to-left, but group 1 = "a", group 2 = "b" (source order).
        var re = new BroilerRegex("(?<=(a)(b))");
        var m = re.Match("ab");
        Assert.True(m.Success);
        Assert.Equal(2, m.Index);
        Assert.Equal("a", m.Groups[1].Value);
        Assert.Equal("b", m.Groups[2].Value);
    }

    [Fact]
    public void Lookbehind_BackreferenceToCapture_MatchesJsSemantics()
    {
        // Inside a look-behind terms run right-to-left, so \1 (still unset) matches
        // empty and (ab) captures "ab" — the JS result, not .NET's reversed one.
        var re = new BroilerRegex("(?<=(ab)\\1)c");
        var m = re.Match("ababc");
        Assert.True(m.Success);
        Assert.Equal(4, m.Index);
        Assert.Equal("c", m.Value);
        Assert.Equal("ab", m.Groups[1].Value);
    }

    [Fact]
    public void Lookbehind_Negative()
    {
        var re = new BroilerRegex("(?<!a)b");
        Assert.False(re.Match("ab").Success);
        Assert.True(re.Match("cb").Success);
    }

    // ----- #6 Unicode (code-point) back-references ----------------------------

    [Fact]
    public void Backreference_AstralCodePoint()
    {
        var re = new BroilerRegex("(\\u{1F438})\\1", "u");
        var m = re.Match("\U0001F438\U0001F438");
        Assert.True(m.Success);
        Assert.Equal("\U0001F438", m.Groups[1].Value);
    }

    [Fact]
    public void Backreference_CaseFolded()
    {
        var re = new BroilerRegex("(?<x>a)\\k<x>", "i");
        var m = re.Match("aA");
        Assert.True(m.Success);
        Assert.Equal("aA", m.Value);
    }

    // ----- #7 Braced Unicode escape as a single astral atom -------------------

    [Fact]
    public void BracedUnicodeEscape_MatchesAstralCharacter()
    {
        var re = new BroilerRegex("^\\u{1F438}$", "u");
        var m = re.Match("\U0001F438");
        Assert.True(m.Success);
        Assert.Equal("\U0001F438", m.Value);
    }

    [Fact]
    public void AstralAtom_WithQuantifier_IsOneCodePoint()
    {
        // The pair is a single atom, so {2} requires two frogs, not four units.
        var re = new BroilerRegex("^\\u{1F438}{2}$", "u");
        Assert.True(re.Match("\U0001F438\U0001F438").Success);
        Assert.False(re.Match("\U0001F438").Success);
    }
}
