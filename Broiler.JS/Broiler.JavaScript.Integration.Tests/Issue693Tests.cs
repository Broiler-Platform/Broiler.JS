using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/693
//
// Fixed here (Problem 10 — Intl `*.supportedLocalesOf` returned every requested
// locale instead of only the supported ones):
//
//   SupportedLocales / LookupSupportedLocales (ECMA-402) must narrow the
//   canonicalized request list to the locales the runtime can actually serve,
//   using BestAvailableLocale (strip extension sequences, then trim trailing
//   subtags). The implementation skipped this step and echoed the whole list
//   back, so the test262 `supportedLocalesOf/basic.js` family — which passes
//   `[defaultLocale, "zxx"]` ("zxx" = "no linguistic content") and asserts the
//   result length is 1 — saw length 2. AvailableLocales is sourced from the host
//   .NET globalization (ICU/CLDR) culture list.
//
// Out of scope (architectural / CLDR / deep parser, matching the triage carried
// in #683 / #685 / #687 / #689 / #691): the private-* brand-check and
// double-initialisation families, super-*-reference-null, the proxy
// default-handler TypeError tests, AnnexB eval binding re-init / skip-early-err,
// scope-param-elem-var, derived-class-return-override,
// computed-property-abrupt-completion, NumberFormat signDisplay "negative"
// currency CLDR formatting, and the staging/sm negative SyntaxError grab-bag.
public class Issue693Tests
{
    private static JSValue Eval(string code)
    {
        using var ctx = new JSContext(experimentalFeatures: JavaScriptFeatureFlags.AllExperimentalEs2026);
        return ctx.Eval(code);
    }

    // The exact shape of intl402/<Service>/supportedLocalesOf/basic.js: a request
    // mixing the default (supported) locale with the unsupported "zxx" tag must
    // resolve to a one-element list containing only the default locale.
    [Theory]
    [InlineData("Collator")]
    [InlineData("DateTimeFormat")]
    [InlineData("NumberFormat")]
    [InlineData("PluralRules")]
    [InlineData("RelativeTimeFormat")]
    [InlineData("ListFormat")]
    [InlineData("Segmenter")]
    public void UnsupportedLocaleIsDropped(string ctor)
    {
        var code =
            "var C = Intl." + ctor + ";" +
            "var def = new C().resolvedOptions().locale;" +
            "var s = C.supportedLocalesOf([def, 'zxx']);" +
            "s.length + '|' + (s[0] === def);";
        Assert.Equal("1|true", Eval(code).ToString());
    }

    // "und" (undetermined) and other tags with no available match are dropped.
    [Fact]
    public void UndeterminedLocaleIsDropped()
        => Assert.Equal("0", Eval("Intl.NumberFormat.supportedLocalesOf(['und']).length;").ToString());

    // A genuinely supported locale survives, and a script subtag still matches via
    // the BestAvailableLocale language fallback.
    [Fact]
    public void SupportedLocalesAreKept()
        => Assert.Equal("3", Eval("Intl.NumberFormat.supportedLocalesOf(['de-DE', 'fr', 'en-Latn-US']).length;").ToString());

    // A Unicode extension sequence is stripped before matching but preserved on
    // the locale that is returned.
    [Fact]
    public void ExtensionSequenceIsPreservedOnSupportedLocale()
        => Assert.Equal("de-DE-u-co-phonebk", Eval(
            "Intl.NumberFormat.supportedLocalesOf(['de-DE-u-co-phonebk'])[0];").ToString());

    // An empty request stays an empty array (not undefined / not throwing).
    [Fact]
    public void EmptyRequestReturnsEmptyArray()
        => Assert.Equal("true|0", Eval(
            "var s = Intl.NumberFormat.supportedLocalesOf([]); Array.isArray(s) + '|' + s.length;").ToString());
}
