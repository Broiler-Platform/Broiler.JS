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

    // ---- Problems 84/86/87: parsing ISO expanded / out-of-range years ----
    //
    // toDateString/toString/toUTCString negative-year tests start from
    // new Date('-000001-07-01T00:00Z'). The .NET-backed DateParser cannot represent ISO
    // expanded (signed six-digit) years, the astronomical year 0, or years outside 1–9999, so
    // the engine fell back to Broiler.DateTime's strict parser — which required a full
    // "T HH:mm:ss" time component and therefore rejected the date-only and "HH:mm"-without-
    // seconds forms (and year 0). new Date('-000001-07-01T00:00Z') was an Invalid Date, so
    // toDateString().split(' ')[3] was undefined. The fallback now parses the full ECMAScript
    // Date Time String Format directly.

    [Fact]
    public void ParsesExpandedNegativeYearWithMinutePrecisionZone()
        => Assert.Equal("-0001", Eval(
            "new Date('-000001-07-01T00:00Z').toDateString().split(' ')[3]"));

    [Theory]
    [InlineData("'-000001-07-01T00:00Z'", "-0001")]
    [InlineData("'-000012-07-01T00:00Z'", "-0012")]
    [InlineData("'-000123-07-01T00:00Z'", "-0123")]
    [InlineData("'-001234-07-01T00:00Z'", "-1234")]
    [InlineData("'-012345-07-01T00:00Z'", "-12345")]
    public void ToDateStringSerializesParsedNegativeYears(string input, string expected)
        => Assert.Equal(expected, Eval($"new Date({input}).toDateString().split(' ')[3]"));

    [Fact]
    public void ToStringIncludesParsedNegativeYear()
        => Assert.Equal("-0001", Eval(
            "new Date('-000001-07-01T00:00Z').toString().split(' ')[3]"));

    [Fact]
    public void ToUTCStringIncludesParsedNegativeYear()
        // "Day, DD Mon -0001 …" → the year is the third space-separated token.
        => Assert.Equal("-0001", Eval(
            "new Date('-000001-07-01T00:00Z').toUTCString().split(' ')[3]"));

    [Theory]
    [InlineData("'+275760-09-13T00:00:00Z'", "8640000000000000")]  // maximum time value
    [InlineData("'-271821-04-20T00:00:00Z'", "-8640000000000000")] // minimum time value
    [InlineData("'0000-01-01'", "-62167219200000")]                // astronomical year 0
    [InlineData("'+000000-01-01T00:00:00Z'", "-62167219200000")]
    public void ParsesExpandedAndYearZeroBoundaries(string input, string expected)
        => Assert.Equal(expected, Eval($"Date.parse({input})"));

    [Theory]
    [InlineData("'+275760-09-14T00:00:00Z'")] // one day past the maximum time value
    [InlineData("'-000000-01-01'")]            // negative-zero year is invalid
    [InlineData("'2021-13-01'")]               // month out of range
    [InlineData("'2021-02-30'")]               // day out of range for February
    [InlineData("'2021-02-29'")]               // not a leap year
    [InlineData("'2021-01-01T24:00:01Z'")]     // hour 24 only valid when the rest is zero
    public void RejectsOutOfRangeExtendedDates(string input)
        => Assert.Equal("NaN", Eval($"String(Date.parse({input}))"));

    [Fact]
    public void DateOnlyExpandedFormParsesAsUtc()
        => Assert.Equal("-62183116800000", Eval("Date.parse('-000001-07-01')"));

    // ---- Problems 49/85: Date.prototype.toLocale* match Intl.DateTimeFormat ----
    //
    // §21.4.4.39/41/42 specify that these methods construct an Intl.DateTimeFormat with
    // ToDateTimeOptions(options, required, defaults) — "any"/"all", "date"/"date", "time"/"time"
    // respectively — and format through it. They must therefore produce the same output (Problem
    // 49) and throw the same exceptions (Problem 85) as Intl.DateTimeFormat. The engine's .NET
    // fast path returned the .NET "F"/"D"/"T" rendering for the no-options case and routed
    // toLocaleDateString/toLocaleTimeString options through a path that skipped the constructor's
    // option validation. All three now route through Intl.DateTimeFormat.

    [Theory]
    // toLocaleString → ToDateTimeOptions(_, "any", "all"): full date + time.
    [InlineData("d.toLocaleString('en-US')",
        "new Intl.DateTimeFormat('en-US',{year:'numeric',month:'numeric',day:'numeric',hour:'numeric',minute:'numeric',second:'numeric'}).format(d)")]
    [InlineData("d.toLocaleString()",
        "new Intl.DateTimeFormat(undefined,{year:'numeric',month:'numeric',day:'numeric',hour:'numeric',minute:'numeric',second:'numeric'}).format(d)")]
    [InlineData("d.toLocaleString('en-US',{hour12:false})",
        "new Intl.DateTimeFormat('en-US',{year:'numeric',month:'numeric',day:'numeric',hour:'numeric',minute:'numeric',second:'numeric',hour12:false}).format(d)")]
    // toLocaleDateString → ToDateTimeOptions(_, "date", "date").
    [InlineData("d.toLocaleDateString('en-US')",
        "new Intl.DateTimeFormat('en-US',{year:'numeric',month:'numeric',day:'numeric'}).format(d)")]
    [InlineData("d.toLocaleDateString('en-US',{month:'long',day:'numeric'})",
        "new Intl.DateTimeFormat('en-US',{month:'long',day:'numeric'}).format(d)")]
    // toLocaleTimeString → ToDateTimeOptions(_, "time", "time").
    [InlineData("d.toLocaleTimeString('en-US')",
        "new Intl.DateTimeFormat('en-US',{hour:'numeric',minute:'numeric',second:'numeric'}).format(d)")]
    public void ToLocaleMethodsMatchDateTimeFormat(string method, string reference)
        => Assert.Equal("true", Eval(
            "(function () {" +
            "  var d = new Date(Date.UTC(1989, 10, 9, 17, 57, 3));" +
            $"  return ({method}) === ({reference});" +
            "})()"));

    [Theory]
    [InlineData("toLocaleString")]
    [InlineData("toLocaleDateString")]
    [InlineData("toLocaleTimeString")]
    public void ToLocaleMethodsThrowRangeErrorForInvalidOption(string method)
        => Assert.Equal("true", Eval(
            "(function () {" +
            $"  try {{ new Date(0).{method}('en', {{ localeMatcher: null }}); return false; }}" +
            "  catch (e) { return e instanceof RangeError; }" +
            "})()"));

    [Theory]
    [InlineData("toLocaleString")]
    [InlineData("toLocaleDateString")]
    [InlineData("toLocaleTimeString")]
    public void ToLocaleMethodsThrowTypeErrorForNullLocale(string method)
        => Assert.Equal("true", Eval(
            "(function () {" +
            $"  try {{ new Date(0).{method}(null); return false; }}" +
            "  catch (e) { return e instanceof TypeError; }" +
            "})()"));

    [Theory]
    [InlineData("toLocaleString")]
    [InlineData("toLocaleDateString")]
    [InlineData("toLocaleTimeString")]
    public void ToLocaleMethodsReturnInvalidDateForNaN(string method)
        => Assert.Equal("Invalid Date", Eval($"new Date(NaN).{method}()"));
}
