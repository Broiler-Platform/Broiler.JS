using System.Diagnostics;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/Broiler-Platform/Broiler.JS/issues/966 — the
// test262 `RegExp/property-escapes/generated` timeouts.
//
// A `\p{…}` under `/u` is lowered to a .NET pattern, and its supplementary-plane members
// have to be written as UTF-16 surrogate pairs. Each of the property's code-point ranges
// used to become its own alternative, so `\p{General_Category=Other_Letter}` translated to
// 266 alternatives and every input character that was not an Other_Letter had to fail all
// of them. The alternatives are now grouped by leading surrogate — 43 for that property —
// and `\P{…}` is built from the complemented ranges instead of wrapping the whole positive
// alternation in a negative lookahead.
//
// The re-encoding must not change which code points match, which is what the bulk of this
// file checks: every alternative in the emitted alternation is disjoint from every other,
// so grouping and reordering them can only change how fast a match is found.
public class Issue966PropertyEscapeShapeTests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    [Theory]
    // Astral members of a property still match as whole code points.
    [InlineData(@"/^\p{General_Category=Other_Letter}$/u.test('\u{20000}')", "true")]  // CJK ext. B
    [InlineData(@"/^\p{General_Category=Other_Letter}$/u.test('\u{10600}')", "true")]  // Linear A
    [InlineData(@"/^\p{General_Category=Other_Letter}$/u.test('\u{1D400}')", "false")] // math bold A is Lu
    [InlineData(@"/^\p{Lo}$/u.test('\u{20000}')", "true")]
    [InlineData(@"/^\p{Script=Han}$/u.test('\u{20000}')", "true")]
    [InlineData(@"/^\p{Script=Han}$/u.test('\u{1D400}')", "false")]
    // A lead surrogate alone is not the astral code point it starts.
    [InlineData(@"/^\p{General_Category=Other_Letter}$/u.test('\uD840')", "false")]
    // The last code point of a lead's trail range, and the first of the next lead's.
    [InlineData(@"/^\p{Any}$/u.test('\u{10FFFF}')", "true")]
    [InlineData(@"/^\p{Any}$/u.test('\u{10000}')", "true")]
    public void AstralMembersStillMatchAsCodePoints(string code, string expected)
        => Assert.Equal(expected, Eval(code));

    [Theory]
    // `\P{…}` now comes from the complemented ranges rather than a negative lookahead;
    // it still consumes a whole astral code point, and still matches a lone surrogate.
    [InlineData(@"/^\P{General_Category=Other_Letter}$/u.test('\u{1D400}')", "true")]
    [InlineData(@"/^\P{General_Category=Other_Letter}$/u.test('\u{20000}')", "false")]
    [InlineData(@"/^\P{General_Category=Other_Letter}$/u.test('a')", "true")]
    [InlineData(@"/^\P{Lo}$/u.test('\uD800')", "true")]           // lone lead surrogate
    [InlineData(@"/^\P{Lo}$/u.test('\uDC00')", "true")]           // lone trail surrogate
    [InlineData(@"/^\P{Lo}$/u.test('\u{20000}')", "false")]       // a whole pair, not two units
    [InlineData(@"/^\P{Lo}\P{Lo}$/u.test('\uD800\uD800')", "true")]   // two unpaired leads
    [InlineData(@"/^\P{Lo}$/u.test('𐀀')", "false")]    // one pair, and it is Lo
    // A property that itself contains the surrogates: `\P{Cs}` must take the pair.
    [InlineData(@"/^\P{Cs}$/u.test('\u{20000}')", "true")]
    [InlineData(@"/^\P{Cs}$/u.test('\uD800')", "false")]
    public void NegatedPropertyKeepsCodePointSemantics(string code, string expected)
        => Assert.Equal(expected, Eval(code));

    [Theory]
    // Dropping the negative lookahead also fixed what it was hiding. The old form was
    // `(?!positive)(?:pair|[\s\S])`, and an unanchored scan reaching the TRAILING half of a
    // surrogate pair found that `positive` failed there — a lead-plus-trail alternative
    // cannot match starting at the trail — so the lookahead succeeded and `[\s\S]` consumed
    // that half on its own. `"a\u{20000}b".match(/\P{Lo}/gu)` came back as
    // ["a", "\uDC00", "b"]: a match that is half a code point, at an index inside one.
    // The complement matcher cannot do that; its lone-surrogate alternative is guarded by
    // `(?<![\uD800-\uDBFF])`.
    [InlineData(@"JSON.stringify('a\u{20000}b'.match(/\P{Lo}/gu))", @"[""a"",""b""]")]
    [InlineData(@"JSON.stringify([...'a\u{20000}b'.matchAll(/\P{Lo}/gu)].map(m => m.index))", "[0,3]")]
    [InlineData(@"JSON.stringify('\u{20000}\u{20001}'.match(/\P{Lo}/gu))", "null")]
    [InlineData(@"JSON.stringify('7\u{1D400}-'.match(/\P{L}/gu))", @"[""7"",""-""]")]
    [InlineData(@"JSON.stringify([...'7\u{1D400}-'.matchAll(/\P{L}/gu)].map(m => m.index))", "[0,3]")]
    // A lone surrogate really is a `\P{Lo}` match, and keeps its own index.
    [InlineData(@"JSON.stringify([...'\uD800\u{20000}'.matchAll(/\P{Lo}/gu)].map(m => m.index))", "[0]")]
    [InlineData(@"JSON.stringify('a\u{20000}b'.replace(/\P{Lo}/gu, '#'))", @"""#𠀀#""")]
    public void NegatedPropertyNeverMatchesHalfOfASurrogatePair(string code, string expected)
        => Assert.Equal(expected, Eval(code));

    [Theory]
    // `\P{Any}` complements to the empty set. Building a matcher from no ranges produced
    // the empty group `(?:)`, which matches the empty string at every position — so the
    // escape matched everything instead of nothing.
    [InlineData(@"/\P{Any}/u.test('a')", "false")]
    [InlineData(@"/\P{Any}/u.test('')", "false")]
    [InlineData(@"/\P{Any}/iu.test('a')", "false")]
    [InlineData(@"/\P{Any}/iu.test('')", "false")]
    [InlineData(@"'a'.replace(/\P{Any}/gu, 'X')", "a")]
    [InlineData(@"'a'.replace(/\P{Any}/giu, 'X')", "a")]
    [InlineData(@"/\p{Any}/u.test('a')", "true")]
    public void NegatedAnyMatchesNothing(string code, string expected)
        => Assert.Equal(expected, Eval(code));

    [Theory]
    // BMP-only properties are unaffected by the astral grouping, negated or not.
    [InlineData(@"/^\p{Script=Thai}$/u.test('ก')", "true")]
    [InlineData(@"/^\p{Script=Thai}$/u.test('a')", "false")]
    [InlineData(@"/^\P{Script=Thai}$/u.test('a')", "true")]
    [InlineData(@"/^\P{Script=Thai}$/u.test('ก')", "false")]
    [InlineData(@"/^\p{ASCII}$/u.test('a')", "true")]
    [InlineData(@"/^\P{ASCII}$/u.test('é')", "true")]
    [InlineData(@"/^\p{L}$/u.test('a')", "true")]
    [InlineData(@"/^\P{L}$/u.test('5')", "true")]
    public void BmpOnlyPropertiesUnaffected(string code, string expected)
        => Assert.Equal(expected, Eval(code));

    [Fact]
    public void NegatedPropertyIsExactlyTheComplementAcrossTheSupplementaryPlanes()
    {
        // The re-encoding is only allowed to change the shape of the alternation, so `\P{X}`
        // must still accept exactly the code points `\p{X}` rejects. Swept rather than
        // sampled, because the failure this guards against — a lead surrogate grouped with
        // the wrong trail class — shows up at the edge of one range and nowhere else.
        //
        // Planes 1 and 2 (leads U+D800..U+D87F) carry both shapes the grouping has to get
        // right: plane 1's Lo ranges are short and scattered, so its leads each end up with
        // their own many-span trail class, while plane 2 is almost entirely CJK Lo, so its
        // leads share the full [\uDC00-\uDFFF] class and are merged into one alternative.
        using var ctx = new JSContext();
        var mismatches = ctx.Eval(
            """
            (function () {
              // Hoisted: a regex literal is re-translated every time it is evaluated, and a
              // property escape is an expensive translation.
              const positive = /^\p{Lo}$/u;
              const negative = /^\P{Lo}$/u;
              let bad = 0;
              for (let cp = 0x10000; cp <= 0x2FFFF; cp++) {
                const s = String.fromCodePoint(cp);
                if (positive.test(s) === negative.test(s)) bad++;
              }
              return bad;
            })()
            """).ToString();

        Assert.Equal("0", mismatches);
    }

    [Fact]
    public void EveryLoneSurrogateIsOutsideAnAstralProperty()
    {
        // A surrogate code unit standing on its own is a code point in its own right, and it
        // is never a member of Lo — but the grouped astral alternatives are written in terms
        // of surrogate units, so a mis-grouped lead could start matching one.
        using var ctx = new JSContext();
        var mismatches = ctx.Eval(
            """
            (function () {
              const positive = /^\p{Lo}$/u;
              const negative = /^\P{Lo}$/u;
              let bad = 0;
              for (let unit = 0xD800; unit <= 0xDFFF; unit++) {
                const s = String.fromCharCode(unit);
                if (positive.test(s)) bad++;
                if (!negative.test(s)) bad++;
              }
              return bad;
            })()
            """).ToString();

        Assert.Equal("0", mismatches);
    }

    [Fact]
    public void TheTimingOutTestShapeCompletesQuickly()
    {
        // The shape of test262's property-escapes/generated files: one string holding every
        // code point of the property matched against `^\p{…}+$`, then the complement against
        // `^\P{…}+$`. Over the whole code-point space that ran for ~37 s against a 30 s
        // per-test budget; this covers planes 1-2, where the astral alternatives live.
        //
        // The bound is deliberately loose — a CI runner is not a benchmark host. It is here
        // to fail if the alternation goes back to one alternative per range, which costs an
        // order of magnitude and nothing less.
        using var ctx = new JSContext();
        var stopwatch = Stopwatch.StartNew();
        var result = ctx.Eval(
            """
            (function () {
              const positive = /^\p{Lo}$/u;
              const inSet = [], outOfSet = [];
              for (let cp = 0x10000; cp <= 0x2FFFF; cp++) {
                const s = String.fromCodePoint(cp);
                (positive.test(s) ? inSet : outOfSet).push(s);
              }
              return /^\p{Lo}+$/u.test(inSet.join('')) && /^\P{Lo}+$/u.test(outOfSet.join(''));
            })()
            """).ToString();
        stopwatch.Stop();

        Assert.Equal("true", result);
        Assert.True(
            stopwatch.Elapsed.TotalSeconds < 30,
            $"the property-escape shape took {stopwatch.Elapsed.TotalSeconds:F1}s");
    }
}
