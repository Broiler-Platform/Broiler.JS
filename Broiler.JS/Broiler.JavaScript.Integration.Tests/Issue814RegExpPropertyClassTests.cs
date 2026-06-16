using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/814 — Problem 64
// (test/built-ins/RegExp/property-escapes/character-class.js).
//
// A character class whose only member is a Unicode property escape — `[\p{X}]`,
// `[\P{X}]`, `[^\p{X}]`, `[^\P{X}]` — is equivalent to the standalone (possibly
// doubly-negated) property. The in-class lowering could not represent a negated
// fragment or a supplementary-plane range inside a `[...]` set, so a negated binary
// property (`[\P{Hex}]`) or an astral-bearing property (`[\p{Emoji}]`) was rejected with
// "Unicode property escape … is not supported yet". Such single-property classes are now
// translated as the standalone (code-point, surrogate-aware) property.
public class Issue814RegExpPropertyClassTests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    [Theory]
    // negated binary property in a class
    [InlineData(@"/[\P{Hex}]/u.test('g')", "true")]   // 'g' is not a hex digit
    [InlineData(@"/[\P{Hex}]/u.test('f')", "false")]  // 'f' is a hex digit
    // double negation: [^\P{X}] == \p{X}
    [InlineData(@"/[^\P{Hex}]/u.test('f')", "true")]
    [InlineData(@"/[^\P{Hex}]/u.test('g')", "false")]
    // [^\p{X}] == \P{X}
    [InlineData(@"/[^\p{Hex}]/u.test('g')", "true")]
    [InlineData(@"/[^\p{Hex}]/u.test('f')", "false")]
    public void NegatedPropertyInsideCharacterClass(string code, string expected)
        => Assert.Equal(expected, Eval(code));

    [Theory]
    // astral-bearing property in a class (supplementary code point)
    [InlineData(@"/[\p{Emoji}]/u.test('\u{1F600}')", "true")]
    [InlineData(@"/[\p{Emoji}]/u.test('a')", "false")]
    [InlineData(@"/[\p{Alphabetic}]/u.test('\u{10000}')", "true")]  // Linear B syllable
    public void AstralPropertyInsideCharacterClass(string code, string expected)
        => Assert.Equal(expected, Eval(code));

    [Theory]
    // existing single/multi property class behaviour must be unaffected
    [InlineData(@"/[\p{L}]/u.test('a')", "true")]
    [InlineData(@"/[\p{L}]/u.test('5')", "false")]
    [InlineData(@"/[\p{Hex}]/u.test('f')", "true")]
    [InlineData(@"/[\p{Hex}]/u.test('g')", "false")]
    [InlineData(@"/[\p{L}0-9]/u.test('5')", "true")]   // mixed class
    [InlineData(@"/[\p{L}\p{N}]/u.test('7')", "true")] // two properties
    [InlineData(@"/[abc]/u.test('b')", "true")]        // plain class
    [InlineData(@"/[^abc]/u.test('z')", "true")]       // plain negated class
    [InlineData(@"/\P{Hex}/u.test('g')", "true")]      // standalone property
    public void ExistingPropertyClassBehaviourUnaffected(string code, string expected)
        => Assert.Equal(expected, Eval(code));
}
