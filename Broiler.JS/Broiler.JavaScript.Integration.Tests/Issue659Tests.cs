using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/659
//
// Problems 1 & 2 — Unicode "properties of strings" (UTS #51 emoji string
// properties: \p{RGI_Emoji}, \p{Basic_Emoji}, \p{Emoji_Keycap_Sequence},
// \p{RGI_Emoji_*_Sequence}). With the `v` (unicodeSets) flag these expand to an
// alternation of the literal emoji sequences drawn from the bundled
// Broiler.Unicode emoji data (UnicodeEmoji.StringProperties). They are only valid
// with `v`: bare in `u` mode, or negated, they remain a SyntaxError.
//
// Problems 3 (sm "structurally equal" harness), 4/5 (Intl.DateTimeFormat
// formatRange / Date.toISOString extended range) and 6-10 (assorted language /
// built-in failures) are triaged in the issue and out of scope for this change.
public class Issue659Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // ---- Emoji_Keycap_Sequence ----

    [Fact]
    public void KeycapDigitMatches()
        => Assert.Equal("true", Eval(@"String(/^\p{Emoji_Keycap_Sequence}$/v.test('1️⃣'))"));

    [Fact]
    public void KeycapHashAndStarMatch()
        => Assert.Equal("true", Eval(
            @"String(/^\p{Emoji_Keycap_Sequence}$/v.test('#️⃣') && /^\p{Emoji_Keycap_Sequence}$/v.test('*️⃣'))"));

    [Fact]
    public void KeycapDoesNotMatchBarePlainDigit()
        => Assert.Equal("false", Eval(@"String(/^\p{Emoji_Keycap_Sequence}$/v.test('1'))"));

    // ---- Basic_Emoji ----

    [Fact]
    public void BasicEmojiMatchesSingleCodePoint()
        => Assert.Equal("true", Eval(@"String(/^\p{Basic_Emoji}$/v.test('\u{1F600}'))"));

    [Fact]
    public void BasicEmojiMatchesTextPresentationWithVariationSelector()
        => Assert.Equal("true", Eval(@"String(/^\p{Basic_Emoji}$/v.test('❤️'))"));

    // ---- RGI_Emoji (the union) ----

    [Fact]
    public void RgiEmojiMatchesFlagSequence()
        => Assert.Equal("true", Eval(@"String(/^\p{RGI_Emoji}$/v.test('\u{1F1E9}\u{1F1EA}'))"));

    [Fact]
    public void RgiEmojiMatchesZwjFamilySequence()
        => Assert.Equal("true", Eval(
            @"String(/^\p{RGI_Emoji}$/v.test('\u{1F468}‍\u{1F469}‍\u{1F467}‍\u{1F466}'))"));

    [Fact]
    public void RgiEmojiMatchesSingleEmoji()
        => Assert.Equal("true", Eval(@"String(/^\p{RGI_Emoji}$/v.test('\u{1F600}'))"));

    [Fact]
    public void RgiEmojiDoesNotMatchLetter()
        => Assert.Equal("false", Eval(@"String(/^\p{RGI_Emoji}$/v.test('a'))"));

    // Leftmost-longest: an embedded match must consume the whole ZWJ sequence,
    // not just its leading code point.
    [Fact]
    public void RgiEmojiEmbeddedMatchIsLeftmostLongest()
        => Assert.Equal("true", Eval(
            @"var s='x\u{1F468}‍\u{1F469}‍\u{1F467}‍\u{1F466}y';"
            + @"var m=s.match(/\p{RGI_Emoji}/v);"
            + @"String(m[0]==='\u{1F468}‍\u{1F469}‍\u{1F467}‍\u{1F466}')"));

    // ---- Validity rules ----

    // Properties of strings require the `v` flag; bare in `u` mode it is a SyntaxError.
    [Fact]
    public void RgiEmojiInUnicodeModeThrowsSyntaxError()
        => Assert.Equal("SyntaxError", Eval(
            @"var n='ok'; try { eval('/\\p{RGI_Emoji}/u'); } catch(e){ n=e.constructor.name; } n"));

    // Negating a property of strings (\P{...}) is a SyntaxError even in `v` mode.
    [Fact]
    public void NegatedRgiEmojiThrowsSyntaxError()
        => Assert.Equal("SyntaxError", Eval(
            @"var n='ok'; try { eval('/\\P{RGI_Emoji}/v'); } catch(e){ n=e.constructor.name; } n"));
}
