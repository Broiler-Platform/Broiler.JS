using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/889 — the three
// most tractable test262 failure clusters picked from the 130 listed problems:
//
//   * Cluster A (issue Problem 44): try / catch completion value. When `try` produced
//     a value before throwing and `catch` produced none, the script/eval completion
//     wrongly returned the try value instead of the empty completion (UpdateEmpty
//     should substitute undefined per ECMA-262 §14.15.5). Same fix covers the catch-
//     with-break / catch-with-continue variants in test262
//     `staging/sm/statements/try-completion.js`.
//   * Cluster B (issue Problems 121–124): `\p{…}` properties-of-strings backed by
//     the Unicode 17.0 emoji data — Broiler.Unicode submodule bumped 16.0 → 17.0
//     (e.g. U+1F6D8 LANDSLIDE is a Basic_Emoji introduced in Emoji 17.0).
//   * Cluster C (issue Problem 110): Intl.ListFormat.formatToParts misattributed
//     separator characters that also occurred inside a list element. The previous
//     implementation formatted the list to a single string and recovered the
//     separators with IndexOf, so e.g. `formatToParts("foo")` — iterated to
//     ['f','o','o'] — split the "or " connector around the second element. Now
//     the parts are built directly from the CLDR pattern templates.
public class Issue889Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code)?.ToString();
    }

    // ── Cluster A — try / catch completion value ───────────────────────────────

    [Theory]
    [InlineData("try { 'try'; throw 'e'; } catch (e) { }", "undefined")]
    [InlineData("try { 'try'; throw 'e'; } catch (e) { 'catch'; }", "catch")]
    [InlineData("try { 'try'; throw 'e'; } catch (e) { } finally { 'finally'; }", "undefined")]
    [InlineData("while (true) { try { 'try'; throw 'e'; } catch (e) { break; } }", "undefined")]
    [InlineData("do { try { 'try'; throw 'e'; } catch (e) { continue; } } while (false);", "undefined")]
    [InlineData("while (true) { try { 'try'; throw 'e'; } catch (e) { break; } finally { 'finally'; } }", "undefined")]
    [InlineData("do { try { 'try'; throw 'e'; } catch (e) { continue; } finally { 'finally'; } } while (false);", "undefined")]
    public void TryThrowEmptyCatchGivesUndefined(string body, string expected)
        => Assert.Equal(expected, Eval($"String(eval({Quote(body)}))"));

    [Theory]
    // Variants that must keep working — the try / catch with values, and the existing
    // finally-overrides-completion behaviour covered by Issue887Tests.
    [InlineData("try { 'try'; } finally { 'finally'; }", "try")]
    [InlineData("try { 'try'; throw 'e'; } catch (e) { 'catch'; } finally { 'finally'; }", "catch")]
    [InlineData("while (true) { try { 'try'; } finally { 'finally'; break; } }", "finally")]
    [InlineData("do { try { 'try'; continue; } finally { 'finally'; continue; } } while (false);", "finally")]
    public void TryFinallyExistingCompletionShapesUnchanged(string body, string expected)
        => Assert.Equal(expected, Eval($"String(eval({Quote(body)}))"));

    // Destructuring and optional-binding catch must reset the completion too.
    [Fact]
    public void DestructuringCatchEmptyBodyGivesUndefined()
        => Assert.Equal("undefined", Eval(
            "String(eval(\"try { 'try'; throw { a: 1 }; } catch ({ a }) { }\"))"));

    [Fact]
    public void OptionalCatchBindingEmptyBodyGivesUndefined()
        => Assert.Equal("undefined", Eval(
            "String(eval(\"try { 'try'; throw 'e'; } catch { }\"))"));

    // ── Cluster B — Unicode 17.0 emoji properties of strings ──────────────────

    [Fact]
    public void Unicode17BasicEmojiLandslideMatches()
        // U+1F6D8 LANDSLIDE is a Basic_Emoji introduced in Emoji 17.0.
        => Assert.Equal("true", Eval(@"String(/\p{Basic_Emoji}/v.test('\u{1F6D8}'))"));

    [Fact]
    public void Unicode17RgiEmojiAlsoCoversLandslide()
        => Assert.Equal("true", Eval(@"String(/\p{RGI_Emoji}/v.test('\u{1F6D8}'))"));

    [Fact]
    public void ModifierAndZwjSequencesStillMatch()
    {
        // 👋🏻 = waving hand + light skin tone.
        Assert.Equal("true", Eval(@"String(/\p{RGI_Emoji_Modifier_Sequence}/v.test('\u{1F44B}\u{1F3FB}'))"));
        // 👨‍👩‍👧 = family: man, woman, girl (ZWJ sequence).
        Assert.Equal("true", Eval(@"String(/\p{RGI_Emoji_ZWJ_Sequence}/v.test('\u{1F468}\u{200D}\u{1F469}\u{200D}\u{1F467}'))"));
    }

    [Fact]
    public void NonEmojiCodePointStillRejectedByBasicEmoji()
        => Assert.Equal("false", Eval(@"String(/^\p{Basic_Emoji}$/v.test('A'))"));

    // ── Cluster C — Intl.ListFormat.formatToParts pattern walking ─────────────

    [Fact]
    public void FormatToPartsForStringIterableSplitsOrConnectorCorrectly()
    {
        // "foo" is iterated as ['f','o','o']; the second element 'o' must not be
        // matched against the 'o' inside the ", or " connector.
        const string code = @"
            var lf = new Intl.ListFormat('en-US', { type: 'disjunction' });
            JSON.stringify(lf.formatToParts('foo'))";
        Assert.Equal(
            "[{\"type\":\"element\",\"value\":\"f\"},{\"type\":\"literal\",\"value\":\", \"},"
            + "{\"type\":\"element\",\"value\":\"o\"},{\"type\":\"literal\",\"value\":\", or \"},"
            + "{\"type\":\"element\",\"value\":\"o\"}]",
            Eval(code));
    }

    [Fact]
    public void FormatToPartsExistingThreeAndFourElementShapesUnchanged()
    {
        const string codeThree = @"
            var lf = new Intl.ListFormat('en-US', { type: 'disjunction' });
            JSON.stringify(lf.formatToParts(['foo', 'bar', 'baz']))";
        Assert.Equal(
            "[{\"type\":\"element\",\"value\":\"foo\"},{\"type\":\"literal\",\"value\":\", \"},"
            + "{\"type\":\"element\",\"value\":\"bar\"},{\"type\":\"literal\",\"value\":\", or \"},"
            + "{\"type\":\"element\",\"value\":\"baz\"}]",
            Eval(codeThree));

        const string codeFour = @"
            var lf = new Intl.ListFormat('en-US', { type: 'disjunction' });
            JSON.stringify(lf.formatToParts(['foo', 'bar', 'baz', 'quux']))";
        Assert.Equal(
            "[{\"type\":\"element\",\"value\":\"foo\"},{\"type\":\"literal\",\"value\":\", \"},"
            + "{\"type\":\"element\",\"value\":\"bar\"},{\"type\":\"literal\",\"value\":\", \"},"
            + "{\"type\":\"element\",\"value\":\"baz\"},{\"type\":\"literal\",\"value\":\", or \"},"
            + "{\"type\":\"element\",\"value\":\"quux\"}]",
            Eval(codeFour));
    }

    [Fact]
    public void FormatToPartsEmptyAndSingletonAndPairShapesUnchanged()
    {
        const string preamble = "var lf = new Intl.ListFormat('en-US', { type: 'disjunction' }); ";
        Assert.Equal("[]", Eval(preamble + "JSON.stringify(lf.formatToParts([]))"));
        Assert.Equal(
            "[{\"type\":\"element\",\"value\":\"foo\"}]",
            Eval(preamble + "JSON.stringify(lf.formatToParts(['foo']))"));
        Assert.Equal(
            "[{\"type\":\"element\",\"value\":\"foo\"},{\"type\":\"literal\",\"value\":\" or \"},"
            + "{\"type\":\"element\",\"value\":\"bar\"}]",
            Eval(preamble + "JSON.stringify(lf.formatToParts(['foo', 'bar']))"));
    }

    private static string Quote(string s)
        => "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
}
