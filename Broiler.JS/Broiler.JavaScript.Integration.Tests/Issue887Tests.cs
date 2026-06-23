using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/887 — three of the
// most tractable test262 failure clusters:
//
//   * Cluster A (issue Problems 127–133): `v`-flag (unicodeSets) nested character classes.
//     A nested class such as `[d[0-9]]` is a class-set union; .NET regex has no nested-class
//     syntax, so these must be evaluated to a concrete set instead of being passed through.
//   * Cluster B (issue Problem 107): Temporal June-2024 method removals
//     (test/staging/Temporal/removed-methods.js).
//   * Cluster C (issue Problems 134–137): `\p{…}` properties of strings backed by the
//     Unicode 17.0 emoji data (Broiler.Unicode emoji tables bumped 16.0 → 17.0).
public class Issue887Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code)?.ToString();
    }

    private static bool Test(string code) => Eval(code) == "true";

    // ── Cluster A — v-flag nested character-class union ───────────────────────────

    [Theory]
    // character-class-escape ∪ character-class, both orders
    [InlineData(@"/[d[0-9]]/v")]
    [InlineData(@"/[[0-9]d]/v")]
    // character-property-escape ∪ character-class, both orders
    [InlineData(@"/[\p{ASCII_Hex_Digit}[0-9]]/v")]
    [InlineData(@"/[[0-9]\p{ASCII_Hex_Digit}]/v")]
    // character-class ∪ character-class, and ∪ a bare character, both orders
    [InlineData(@"/[[0-9][0-9]]/v")]
    [InlineData(@"/[[0-9]_]/v")]
    [InlineData(@"/[_[0-9]]/v")]
    public void VFlagNestedClassUnionMatchesZero(string regex)
        => Assert.True(Test(regex + @".test('0')"), regex);

    [Fact]
    public void VFlagNestedClassUnionStillExcludesNonMembers()
    {
        Assert.True(Test(@"/[d[0-9]]/v.test('d')"));
        Assert.False(Test(@"/[d[0-9]]/v.test('a')"));
    }

    [Fact]
    public void VFlagNegatedNestedClassComplementsTheUnion()
    {
        Assert.True(Test(@"/[^[0-9]]/v.test('a')"));
        Assert.False(Test(@"/[^[0-9]]/v.test('5')"));
    }

    [Fact]
    public void VFlagSetOperatorsStillWorkAlongsideNesting()
    {
        Assert.True(Test(@"/[[0-9]--[5]]/v.test('3')"));
        Assert.False(Test(@"/[[0-9]--[5]]/v.test('5')"));
        Assert.True(Test(@"/[[0-9]&&[3-7]]/v.test('5')"));
        Assert.False(Test(@"/[[0-9]&&[3-7]]/v.test('1')"));
    }

    // ── Cluster B — Temporal June-2024 removed methods ────────────────────────────

    [Theory]
    [InlineData("Temporal.Instant.prototype", "toZonedDateTime")]
    [InlineData("Temporal.PlainDateTime.prototype", "toPlainMonthDay")]
    [InlineData("Temporal.PlainDateTime.prototype", "toPlainYearMonth")]
    [InlineData("Temporal.PlainTime.prototype", "toPlainDateTime")]
    [InlineData("Temporal.PlainTime.prototype", "toZonedDateTime")]
    public void TemporalRemovedMethodIsAbsent(string proto, string method)
        => Assert.Equal("false", Eval($"'{method}' in {proto}"));

    [Theory]
    // Methods that were NOT removed must still be present.
    [InlineData("Temporal.Instant.prototype", "toZonedDateTimeISO")]
    [InlineData("Temporal.PlainDateTime.prototype", "toZonedDateTime")]
    [InlineData("Temporal.PlainDate.prototype", "toPlainMonthDay")]
    [InlineData("Temporal.PlainDate.prototype", "toPlainYearMonth")]
    public void TemporalRetainedMethodStillPresent(string proto, string method)
        => Assert.Equal("true", Eval($"'{method}' in {proto}"));

    // ── Cluster C — Unicode 17.0 emoji properties of strings ──────────────────────

    [Fact]
    public void BasicEmojiMatchesUnicode17Addition()
        // U+1F6D8 LANDSLIDE is a Basic_Emoji introduced in Emoji 17.0.
        => Assert.True(Test(@"/\p{Basic_Emoji}/v.test('\u{1F6D8}')"));

    [Fact]
    public void RgiEmojiModifierSequenceMatches()
        // 👋🏻 = waving hand + light skin tone.
        => Assert.True(Test(@"/\p{RGI_Emoji_Modifier_Sequence}/v.test('\u{1F44B}\u{1F3FB}')"));

    [Fact]
    public void RgiEmojiZwjSequenceMatches()
        // 👨‍👩‍👧 = family: man, woman, girl (ZWJ sequence).
        => Assert.True(Test(@"/\p{RGI_Emoji_ZWJ_Sequence}/v.test('\u{1F468}‍\u{1F469}‍\u{1F467}')"));
}
