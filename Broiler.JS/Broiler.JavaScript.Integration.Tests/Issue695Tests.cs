using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/695
//
// Fixed here:
//
// Problem 3 — Intl.NumberFormat rejected the "negative" signDisplay option with a
//   RangeError ("Invalid signDisplay option"). The "negative" value was added by
//   the Intl.NumberFormat v3 proposal and is part of the ECMA-402 GetOption list
//   « "auto", "never", "always", "exceptZero", "negative" », but the runtime's
//   SignDisplayValues table omitted it. So the whole
//   intl402/NumberFormat/prototype/format/signDisplay-negative-* family threw
//   during construction (GetOption) before any formatting assertion ran. The value
//   is now accepted and round-trips through resolvedOptions().
public class Issue695Tests
{
    private static JSValue Eval(string code)
    {
        using var ctx = new JSContext(experimentalFeatures: JavaScriptFeatureFlags.AllExperimentalEs2026);
        return ctx.Eval(code);
    }

    // ---- Problem 3: signDisplay accepts the "negative" value ----

    // Constructing with signDisplay:"negative" no longer throws "Invalid signDisplay
    // option" — every spec-listed signDisplay value is accepted.
    [Theory]
    [InlineData("auto")]
    [InlineData("never")]
    [InlineData("always")]
    [InlineData("exceptZero")]
    [InlineData("negative")]
    public void SignDisplayValueIsAccepted(string value)
    {
        var code =
            "var t; try { new Intl.NumberFormat('en', { signDisplay: '" + value + "' });" +
            " t = 'ok'; } catch (e) { t = e.constructor.name + ':' + e.message; } t;";
        Assert.Equal("ok", Eval(code).ToString());
    }

    // The resolved value is reflected back by resolvedOptions().
    [Fact]
    public void SignDisplayNegativeRoundTripsThroughResolvedOptions()
        => Assert.Equal("negative", Eval(
            "new Intl.NumberFormat('en', { signDisplay: 'negative' }).resolvedOptions().signDisplay;").ToString());

    // An unknown signDisplay value is still a RangeError.
    [Fact]
    public void UnknownSignDisplayValueStillThrows()
        => Assert.Equal("RangeError", Eval(
            "var t; try { new Intl.NumberFormat('en', { signDisplay: 'sometimes' }); t = 'no throw'; }" +
            " catch (e) { t = e.constructor.name; } t;").ToString());
}
