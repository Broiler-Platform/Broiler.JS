using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// A /u (Unicode) regex literal containing astral (supplementary-plane) characters parses as a regex
// instead of being mis-classified as division, and matches by whole code point: an astral character
// class range matches every code point in the range, and a negated astral class matches lone
// surrogates. Mirrors test/language/literals/regexp/u-astral.js. Issue #810 problems 39-42.
public class Issue810RegExpUnicodeAstralTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static bool Test(string body) // body is JS that evaluates to a boolean
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval("String(" + body + ")").ToString() == "true";
    }

    // U+1D306 TETRAGRAM FOR CENTRE (𝌆), U+1F4A9 (💩) .. U+1F4AB (💫).
    private const string Centre = "\U0001D306";

    [Fact]
    public void AstralQuantifier()
        => Assert.True(Test($"/{Centre}{{2}}/u.test('{Centre}{Centre}')"));

    [Fact]
    public void AstralClass()
        => Assert.True(Test($"/^[{Centre}]$/u.test('{Centre}')"));

    [Theory]
    [InlineData("\U0001F4A8", false)] // below 💩
    [InlineData("\U0001F4A9", true)]  // lower boundary 💩
    [InlineData("\U0001F4AA", true)]  // within range 💪
    [InlineData("\U0001F4AB", true)]  // upper boundary 💫
    [InlineData("\U0001F4AC", false)] // above 💫
    public void AstralRange(string ch, bool expected)
        => Assert.Equal(expected, Test($"/[\U0001F4A9-\U0001F4AB]/u.test('{ch}')"));

    // Lone surrogates are passed via explicit \u escapes (xUnit cannot distinguish two
    // InlineData rows whose only difference is an unpaired surrogate).
    [Fact]
    public void NegatedAstralClass_MatchesLoneLeadSurrogate()
        => Assert.True(Test($"/[^{Centre}]/u.test('\\ud834')"));

    [Fact]
    public void NegatedAstralClass_MatchesLoneTrailSurrogate()
        => Assert.True(Test($"/[^{Centre}]/u.test('\\udf06')"));

    [Fact]
    public void NegatedAstralClass_MatchesBmpCharacter()
        => Assert.True(Test($"/[^{Centre}]/u.test('a')"));

    [Fact]
    public void NegatedAstralClass_DoesNotMatchTheExcludedAstralCharacter()
        => Assert.False(Test($"/[^{Centre}]/u.test('{Centre}')"));
}
