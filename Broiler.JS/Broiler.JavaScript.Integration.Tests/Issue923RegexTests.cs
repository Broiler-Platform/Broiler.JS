using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Issue #923: JS/.NET regex gaps now handled by the Broiler.Regex engine.
public class Issue923RegexTests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // Problem 3 — look-behind with mutual-recursive captures/back-references.
    [Theory]
    [InlineData("/(?<=a(.\\2)b(\\1)).{4}/.exec(\"aabcacbc\")", "[\"cacb\",\"a\",\"\"]")]
    [InlineData("/(?<=a(\\2)b(..\\1))b/.exec(\"aacbacb\")", "[\"b\",\"ac\",\"ac\"]")]
    [InlineData("/(?<=(?:\\1b)(aa))./.exec(\"aabaax\")", "[\"x\",\"aa\"]")]
    [InlineData("/(?<=(?:\\1|b)(aa))./.exec(\"aaaax\")", "[\"x\",\"aa\"]")]
    public void LookbehindMutualRecursive(string expr, string expected)
        => Assert.Equal(expected, Eval($"JSON.stringify({expr})"));

    // Problem 4 — back-references to captures inside a look-behind.
    [Theory]
    [InlineData("\"abcCd\".match(/(?<=\\1(\\w))d/i)", "[\"d\",\"C\"]")]
    [InlineData("\"abxxd\".match(/(?<=\\1([abx]))d/)", "[\"d\",\"x\"]")]
    [InlineData("\"ababc\".match(/(?<=\\1(\\w+))c/)", "[\"c\",\"ab\"]")]
    [InlineData("\"ababbc\".match(/(?<=\\1(\\w+))c/)", "[\"c\",\"b\"]")]
    [InlineData("\"ababdc\".match(/(?<=\\1(\\w+))c/)", "null")]
    [InlineData("\"ababc\".match(/(?<=(\\w+)\\1)c/)", "[\"c\",\"abab\"]")]
    public void LookbehindBackReferences(string expr, string expected)
        => Assert.Equal(expected, Eval($"JSON.stringify({expr})"));

    // Problem 8 — nullable quantifier matches the whole string.
    [Fact]
    public void NullableQuantifier()
        => Assert.Equal("ab", Eval("/(a?b??)*/.exec(\"ab\")[0]"));

    // Problem 6 — code-point back-references under /u.
    [Theory]
    [InlineData("/foo(.+)bar\\1/u.exec(\"fooAbarA\")", "[\"fooAbarA\",\"A\"]")]
    public void UnicodeBackReference(string expr, string expected)
        => Assert.Equal(expected, Eval($"JSON.stringify({expr})"));

    // Problem 6 (surrogate edge) — a /u back-reference matches whole code points: it
    // must not match a lead surrogate that pairs with the next code unit
    // (sm/RegExp/unicode-back-reference assertions #5 and #13 — both expect null).
    [Theory]
    [InlineData("/foo(.+)bar\\1/u.exec(\"foo\\uD834bar\\uD834\\uDC00\") === null", "true")]
    [InlineData("/^(.+)\\1$/u.exec(\"\\uDC00foobar\\uD834\\uDC00foobar\\uD834\") === null", "true")]
    // A lone lead surrogate NOT followed by a trail surrogate still matches.
    [InlineData("(function(){var r=/foo(.+)bar\\1/u.exec(\"foo\\uD834bar\\uD834\\uD834\");"
        + "return !!r && r[1].length===1 && r[1].charCodeAt(0)===0xD834;})()", "true")]
    public void UnicodeBackReferenceSurrogates(string expr, string expected)
        => Assert.Equal(expected, Eval(expr));

    // Problem 7 — braced unicode escape as a single astral atom.
    [Theory]
    [InlineData("/^\\u{1F438}$/u.test(\"\\u{1F438}\")", "true")]
    [InlineData("/^\\u{1F438}{2}$/u.test(\"\\u{1F438}\\u{1F438}\")", "true")]
    [InlineData("/^\\u{1F438}{2}$/u.test(\"\\u{1F438}\")", "false")]
    public void BracedUnicodeAstralAtom(string expr, string expected)
        => Assert.Equal(expected, Eval(expr));

    // Problem 7 (lone surrogate) — a lone surrogate atom under /u must not match a
    // code unit that forms a surrogate pair (sm/RegExp/unicode-braced).
    [Theory]
    [InlineData("/\\u{D83D}/u.exec(\"\\uD83D\\uDBFF\") !== null", "true")]
    [InlineData("/\\u{D83D}/u.exec(\"\\uD83D\\uDC00\") === null", "true")]
    [InlineData("/\\u{D83D}/u.exec(\"\\uD83D\\uE000\") !== null", "true")]
    [InlineData("/\\u{1F438}+/u.exec(\"\") === null", "true")]
    public void LoneSurrogateAtom(string expr, string expected)
        => Assert.Equal(expected, Eval(expr));

    // Regression guard: ordinary patterns still flow through the .NET engine.
    [Theory]
    [InlineData("JSON.stringify(\"abc\".match(/b(c)/))", "[\"bc\",\"c\"]")]
    [InlineData("String(\"a1b2\".match(/(\\d)/g).length)", "2")]
    [InlineData("/(?<year>\\d{4})/.exec(\"2024\").groups.year", "2024")]
    public void OrdinaryPatternsUnaffected(string expr, string expected)
        => Assert.Equal(expected, Eval(expr));
}
