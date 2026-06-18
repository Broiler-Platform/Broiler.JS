using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/840
//
//   Problem 90 (Object.getOwnPropertyDescriptor(RegExp, "prototype") should be a data
//   descriptor with every attribute false) — per §22.2.5.1 the RegExp constructor's
//   "prototype" is { [[Writable]]: false, [[Enumerable]]: false, [[Configurable]]: false }.
//   The original generated RegExp constructor installed it correctly (ReadonlyValue), but
//   PatchRegExpPrototype replaces the constructor with a wrapper (for the §22.2.4.1 "return
//   the existing RegExp unchanged" call-form optimization) and re-added the "prototype"
//   property as a ConfigurableValue, leaving it writable AND configurable. test262's
//   built-ins/Object/getOwnPropertyDescriptor/15.2.3.3-4-211.js therefore saw
//   desc.writable === true. The wrapper now carries the same non-writable/non-configurable
//   data property as the original constructor.
public class Issue840Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    [Fact]
    public void RegExpPrototypeIsNonWritable()
        => Assert.Equal("false", Eval("Object.getOwnPropertyDescriptor(RegExp, 'prototype').writable"));

    [Fact]
    public void RegExpPrototypeIsNonEnumerable()
        => Assert.Equal("false", Eval("Object.getOwnPropertyDescriptor(RegExp, 'prototype').enumerable"));

    [Fact]
    public void RegExpPrototypeIsNonConfigurable()
        => Assert.Equal("false", Eval("Object.getOwnPropertyDescriptor(RegExp, 'prototype').configurable"));

    [Fact]
    public void RegExpPrototypeIsDataDescriptorWithoutAccessors()
        => Assert.Equal("false", Eval(
            "(function () {" +
            "  var d = Object.getOwnPropertyDescriptor(RegExp, 'prototype');" +
            "  return d.hasOwnProperty('get') || d.hasOwnProperty('set');" +
            "})()"));

    [Fact]
    public void AssigningRegExpPrototypeIsRejectedInStrictMode()
        => Assert.Equal("true", Eval(
            "(function () {" +
            "  'use strict';" +
            "  try { RegExp.prototype = {}; return false; }" +
            "  catch (e) { return e instanceof TypeError; }" +
            "})()"));

    [Fact]
    public void RegExpStillConstructsAndMatches()
        => Assert.Equal("true", Eval("/a(b)c/.test('abc')"));

    // ---- Problems 95/96/97: Intl.DurationFormat out-of-range fields ----
    //
    // ToDurationRecord runs IsValidDuration: years/months/weeks must each be below 2^32 in
    // magnitude, and the combined time portion (days..nanoseconds) must total under 2^53
    // seconds. The validator previously only checked integrality and sign consistency, so a
    // RangeError was never thrown for these out-of-range fields.

    private static string ThrowsRange(string durationLiteral)
        => Eval(
            "(function () {" +
            "  var df = new Intl.DurationFormat();" +
            "  try { df.format(" + durationLiteral + "); return false; }" +
            "  catch (e) { return e instanceof RangeError; }" +
            "})()");

    private static string FormatsWithoutThrowing(string durationLiteral)
        => Eval(
            "(function () {" +
            "  var df = new Intl.DurationFormat();" +
            "  return typeof df.format(" + durationLiteral + ") === 'string';" +
            "})()");

    [Theory]
    [InlineData("{ years: 4294967296 }")]      // 2^32
    [InlineData("{ years: -4294967296 }")]
    [InlineData("{ months: 4294967297 }")]     // 2^32 + 1
    [InlineData("{ weeks: Number.MAX_SAFE_INTEGER }")]
    [InlineData("{ years: Number.MAX_VALUE }")]
    public void YearsMonthsWeeksAboveTwoPow32Throw(string durationLiteral)
        => Assert.Equal("true", ThrowsRange(durationLiteral));

    [Theory]
    [InlineData("{ years: 4294967295 }")]      // 2^32 - 1
    [InlineData("{ months: -4294967295 }")]
    [InlineData("{ weeks: 4294967295 }")]
    public void YearsMonthsWeeksAtTwoPow32MinusOneAreValid(string durationLiteral)
        => Assert.Equal("true", FormatsWithoutThrowing(durationLiteral));

    [Theory]
    [InlineData("{ days: 104249991375 }")]     // ceil((2^53)/86400)
    [InlineData("{ seconds: Number.MAX_SAFE_INTEGER + 1 }")]
    public void TimeTotalAtOrAboveTwoPow53SecondsThrows(string durationLiteral)
        => Assert.Equal("true", ThrowsRange(durationLiteral));

    [Theory]
    [InlineData("{ days: 104249991374 }")]     // floor(MAX_SAFE/86400)
    [InlineData("{ seconds: Number.MAX_SAFE_INTEGER }")]
    public void TimeTotalBelowTwoPow53SecondsIsValid(string durationLiteral)
        => Assert.Equal("true", FormatsWithoutThrowing(durationLiteral));

    [Fact]
    public void CombinedTimeFieldsExceedingTheLimitThrow()
        => Assert.Equal("true", ThrowsRange(
            "{ days: 104249991374, hours: 7, minutes: 36, seconds: 31, " +
            "milliseconds: 999, microseconds: 999, nanoseconds: 1000 }"));

    [Fact]
    public void OrdinaryDurationStillFormats()
        => Assert.Equal("true", FormatsWithoutThrowing("{ hours: 1, minutes: 30 }"));
}
