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
//
// Problem 10 — the multi-argument Date setters (setHours, setMinutes, setSeconds,
//   setMonth and their UTC counterparts) coerced their first argument twice. The
//   IsValid helper performs ToNumber on the first argument (reading [[DateValue]]
//   before coercion, per spec), but the setters then ignored that coerced value and
//   re-read .IntValue on the same argument — invoking its valueOf a second time. The
//   built-ins/Date/prototype/set*/date-value-read-before-tonumber-when-date-is-valid
//   tests assert valueOf is called exactly once. The setters now reuse the already
//   coerced value for the first slot.
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

    // ---- Problem 10: Date multi-arg setters coerce the first argument once ----

    // On a valid date, the first argument's valueOf must run exactly once, and the
    // setter must still apply the coerced value (here 5) to the relevant component.
    [Theory]
    [InlineData("setHours", "getHours")]
    [InlineData("setMinutes", "getMinutes")]
    [InlineData("setSeconds", "getSeconds")]
    [InlineData("setMonth", "getMonth")]
    [InlineData("setUTCMinutes", "getUTCMinutes")]
    [InlineData("setUTCSeconds", "getUTCSeconds")]
    [InlineData("setUTCMonth", "getUTCMonth")]
    public void MultiArgSetterCoercesFirstArgumentOnce(string setter, string getter)
    {
        var code =
            "var calls = 0;" +
            "var arg = { valueOf: function () { calls++; return 5; } };" +
            "var d = new Date(2020, 0, 15, 10, 20, 30, 40);" +
            "d." + setter + "(arg);" +
            "calls + '|' + d." + getter + "();";
        Assert.Equal("1|5", Eval(code).ToString());
    }

    // The single-argument setters were already correct; guard against a regression.
    [Theory]
    [InlineData("setDate", "getDate")]
    [InlineData("setMilliseconds", "getMilliseconds")]
    [InlineData("setUTCDate", "getUTCDate")]
    public void SingleArgSetterCoercesArgumentOnce(string setter, string getter)
    {
        var code =
            "var calls = 0;" +
            "var arg = { valueOf: function () { calls++; return 7; } };" +
            "var d = new Date(2020, 0, 15, 10, 20, 30, 40);" +
            "d." + setter + "(arg);" +
            "calls + '|' + d." + getter + "();";
        Assert.Equal("1|7", Eval(code).ToString());
    }
}
