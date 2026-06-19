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

    // ---- Problems 57/58: %TypedArray%.prototype.fill coerces the value once ----
    //
    // §23.2.3.10 step 4 converts the fill value (ToNumber, or ToBigInt for bigint element
    // types) exactly once, before reading start/end, and then writes that single result to
    // every position. The engine assigned the raw argument inside the loop, so the element
    // setter re-ran ToNumber/ToBigInt once per filled index.

    [Fact]
    public void TypedArrayFillCoercesNumberValueExactlyOnce()
        => Assert.Equal("2|1,1,1", Eval(
            "(function () {" +
            "  var n = 1;" +
            "  var sample = new Float64Array(3);" +
            "  sample.fill({ valueOf() { return n++; } });" +
            "  return n + '|' + sample.join(',');" +
            "})()"));

    [Fact]
    public void TypedArrayFillCoercesBigIntValueExactlyOnce()
        => Assert.Equal("2|1,1,1", Eval(
            "(function () {" +
            "  var n = 1n;" +
            "  var sample = new BigInt64Array(3);" +
            "  sample.fill({ valueOf() { return n++; } });" +
            "  return n + '|' + sample.join(',');" +
            "})()"));

    [Fact]
    public void TypedArrayFillCoercesValueBeforeStartAndEnd()
        => Assert.Equal("v,s,e", Eval(
            "(function () {" +
            "  var log = [];" +
            "  new Int8Array(3).fill(" +
            "    { valueOf() { log.push('v'); return 0; } }," +
            "    { valueOf() { log.push('s'); return 0; } }," +
            "    { valueOf() { log.push('e'); return 3; } });" +
            "  return log.join(',');" +
            "})()"));

    [Theory]
    [InlineData("new Int8Array([1,2,3,4]).fill(9,1,3).join(',')", "1,9,9,4")]
    [InlineData("new Int8Array([1,2,3,4]).fill(9,-3,-1).join(',')", "1,9,9,4")]
    [InlineData("new Float64Array(2).fill(7).join(',')", "7,7")]
    public void TypedArrayFillStillFillsTheRequestedRange(string expr, string expected)
        => Assert.Equal(expected, Eval(expr));

    // ---- Problems 77/78: %TypedArray%.prototype.sort comparefn this value ----
    //
    // SortCompare (§23.2.3.29) calls the comparefn with undefined as the this value, so a
    // sloppy-mode comparefn observes the global object and a strict one sees undefined. The
    // engine passed the typed array itself as the comparefn's this.

    [Fact]
    public void TypedArraySortComparefnSloppyThisIsGlobal()
        => Assert.Equal("true", Eval(
            "(function () {" +
            "  var seen;" +
            "  new Float64Array([3, 1, 2]).sort(function (a, b) { seen = this; return a - b; });" +
            "  return seen === (function () { return this; })();" +
            "})()"));

    [Fact]
    public void TypedArraySortComparefnStrictThisIsUndefined()
        => Assert.Equal("true", Eval(
            "(function () {" +
            "  'use strict';" +
            "  var seen = 'unset';" +
            "  new Float64Array([3, 1, 2]).sort(function (a, b) { seen = this; return a - b; });" +
            "  return seen === undefined;" +
            "})()"));

    [Fact]
    public void BigIntTypedArraySortComparefnSloppyThisIsGlobal()
        => Assert.Equal("true", Eval(
            "(function () {" +
            "  var seen;" +
            "  new BigInt64Array([3n, 1n, 2n]).sort(function (a, b) { seen = this; return a > b ? 1 : -1; });" +
            "  return seen === (function () { return this; })();" +
            "})()"));

    [Fact]
    public void TypedArrayToSortedComparefnThisIsUndefinedBased()
        => Assert.Equal("true", Eval(
            "(function () {" +
            "  var seen;" +
            "  new Float64Array([3, 1, 2]).toSorted(function (a, b) { seen = this; return a - b; });" +
            "  return seen === (function () { return this; })();" +
            "})()"));

    [Theory]
    [InlineData("new Float64Array([3,1,2,5,4]).sort(function(a,b){return a-b;}).join(',')", "1,2,3,4,5")]
    [InlineData("new Int8Array([3,1,2]).sort().join(',')", "1,2,3")]
    public void TypedArraySortStillOrdersElements(string expr, string expected)
        => Assert.Equal(expected, Eval(expr));

    // ---- Problems 59/67: Temporal creation rejects out-of-range ISO results ----
    //
    // CreateTemporalDateTime / CreateTemporalMonthDay run ISODateTimeWithinLimits /
    // ISODateWithinLimits. PlainDate.prototype.toPlainDateTime built the PlainDateTime through an
    // internal constructor that skipped the check (so combining the minimum date with midnight,
    // which is below the representable PlainDateTime range, did not throw), and the PlainMonthDay
    // constructor validated only month/day for the year, not the representable range of the
    // referenceISOYear-month-day date.

    private static string ThrowsRangeError(string code)
        => Eval("(function () { try { " + code + "; return false; } catch (e) { return e instanceof RangeError; } })()");

    [Fact]
    public void ToPlainDateTimeAtMinimumDateMidnightThrows()
        => Assert.Equal("true", ThrowsRangeError("Temporal.PlainDate.from('-271821-04-19').toPlainDateTime()"));

    [Fact]
    public void ToPlainDateTimeAtMinimumDateWithInRangeTimeSucceeds()
        => Assert.Equal("-271821-04-19T12:00:00",
            Eval("Temporal.PlainDate.from('-271821-04-19').toPlainDateTime('12:00').toString()"));

    [Fact]
    public void ToPlainDateTimeInRangeStillWorks()
        => Assert.Equal("2000-01-01T00:00:00",
            Eval("Temporal.PlainDate.from('2000-01-01').toPlainDateTime().toString()"));

    [Theory]
    [InlineData("new Temporal.PlainMonthDay(9, 14, 'iso8601', 275760)")]   // after the maximum ISO date
    [InlineData("new Temporal.PlainMonthDay(4, 18, 'iso8601', -271821)")]  // before the minimum ISO date
    public void PlainMonthDayReferenceYearOutOfRangeThrows(string code)
        => Assert.Equal("true", ThrowsRangeError(code));

    [Theory]
    [InlineData("new Temporal.PlainMonthDay(9, 13, 'iso8601', 275760).monthCode", "M09")]
    [InlineData("new Temporal.PlainMonthDay(2, 29).toString()", "02-29")]
    [InlineData("Temporal.PlainMonthDay.from('06-15').toString()", "06-15")]
    public void PlainMonthDayInRangeStillWorks(string expr, string expected)
        => Assert.Equal(expected, Eval(expr));

    // ---- Problem 50: Array.prototype.toSpliced reads indices beyond 2^32 ----
    //
    // The array-like's length is ToLength-clamped to 2^53-1, so the source-copy loops walk
    // long indices that the spec addresses by their canonical numeric property key. The
    // engine cast each long index to uint when reading the source, truncating values such as
    // 9007199254740989 (2^53-3) to 4294967293 — so the result was filled with the wrong
    // slots (effectively undefined). GetIndexedValue's long overload now routes indices ≥
    // 2^32 through the numeric-string key, matching how the source was populated.

    [Fact]
    public void ToSplicedReadsLargeIndicesPastUint32()
        => Assert.Equal("2|9007199254740989,9007199254740990", Eval(
            "(function () {" +
            "  var arrayLike = {" +
            "    '9007199254740989': 2 ** 53 - 3," +
            "    '9007199254740990': 2 ** 53 - 2," +
            "    '9007199254740991': 2 ** 53 - 1," +
            "    '9007199254740992': 2 ** 53," +
            "    '9007199254740994': 2 ** 53 + 2," +
            "    length: 2 ** 53 + 20" +
            "  };" +
            "  var r = Array.prototype.toSpliced.call(arrayLike, 0, 2 ** 53 - 3);" +
            "  return r.length + '|' + r[0] + ',' + r[1];" +
            "})()"));

    [Theory]
    [InlineData("[1,2,3,4,5].toSpliced(1,2).join(',')", "1,4,5")]
    [InlineData("[1,2,3,4].toSpliced(1,1,'a','b').join(',')", "1,a,b,3,4")]
    [InlineData("[1,2,3,4].toSpliced(-2,1).join(',')", "1,2,4")]
    [InlineData("[1,2,3].toSpliced().join(',')", "1,2,3")]
    public void ToSplicedOrdinaryArraysStillWork(string expr, string expected)
        => Assert.Equal(expected, Eval(expr));

    [Fact]
    public void ToSplicedNewLengthBeyond2Pow53Throws()
        => Assert.Equal("true", ThrowsRangeError(
            "try { Array.prototype.toSpliced.call({ length: 2 ** 53 - 1 }, 0, 0, 'x'); throw new Error('no throw'); }" +
            " catch (e) { if (e instanceof TypeError) throw new RangeError(); else throw e; }"));

    // ---- Problem 62: Array.of propagates an abrupt CreateDataProperty ----
    //
    // Array.of is generic: when called with a constructor (Array.of.call(C, ...)) it builds the
    // result with CreateDataPropertyOrThrow, which must throw a TypeError if defining the index
    // fails — e.g. the constructor made itself non-extensible, or pre-defined the index as
    // non-configurable. The engine populated via the non-throwing CreateDataProperty (a plain
    // FastAddValue that ignores extensibility/configurability), so no exception surfaced.

    private static string ThrowsTypeError(string code)
        => Eval("(function () { try { " + code + "; return false; } catch (e) { return e instanceof TypeError; } })()");

    [Fact]
    public void ArrayOfThrowsWhenResultIsNonExtensible()
        => Assert.Equal("true", ThrowsTypeError(
            "function T1() { Object.preventExtensions(this); } Array.of.call(T1, 'Bob')"));

    [Fact]
    public void ArrayOfThrowsWhenIndexIsNonConfigurable()
        => Assert.Equal("true", ThrowsTypeError(
            "function T2() { Object.defineProperty(this, 0, { configurable: false, writable: true, enumerable: true }); }" +
            " Array.of.call(T2, 'Bob')"));

    [Theory]
    [InlineData("Array.of(1,2,3).join(',')", "1,2,3")]
    [InlineData("Array.of().length", "0")]
    [InlineData("(function(){ function C(n){ this.lenArg = n; } var r = Array.of.call(C, 'a', 'b'); return r[0]+r[1]+r.length+r.lenArg; })()", "ab22")]
    public void ArrayOfStillBuildsResults(string expr, string expected)
        => Assert.Equal(expected, Eval(expr));

    // ---- Problem 63: Array.prototype.reverse with a length beyond the 32-bit range ----
    //
    // LengthOfArrayLike (ToLength) clamps the array-like length to 2^53-1, so reverse's high
    // index can exceed the 32-bit array-index range. reverse tracked the upper index in a uint
    // taken from a uint-clamped length, so a length of 2^53+2 truncated to ~2^32 and reverse
    // walked the wrong indices (never reaching the real high index). It now walks long indices
    // via the long-keyed accessors. The per-pair access order is also aligned with the spec
    // (HasProperty/Get the lower index fully before the upper one).

    private const string ReverseProxySetup =
        "var arrayLike = { 0: 'a', get 4() { throw new RangeError('StopReverse'); }," +
        "  9007199254740990: 'hi', length: 2 ** 53 + 2 };" +
        "var traps = [];" +
        "var proxy = new Proxy(arrayLike, {" +
        "  has(t, pk) { traps.push('Has:' + String(pk)); return Reflect.has(t, pk); }," +
        "  get(t, pk, r) { traps.push('Get:' + String(pk)); return Reflect.get(t, pk, r); }," +
        "  set(t, pk, v, r) { traps.push('Set:' + String(pk)); return Reflect.set(t, pk, v, r); }," +
        "  getOwnPropertyDescriptor(t, pk) { return Reflect.getOwnPropertyDescriptor(t, pk); }," +
        "  defineProperty(t, pk, d) { return Reflect.defineProperty(t, pk, d); }," +
        "  deleteProperty(t, pk) { return Reflect.deleteProperty(t, pk); }" +
        "});";

    [Fact]
    public void ReverseReachesHighIndexBeyondUint32()
        => Assert.Equal("StopReverse", Eval(
            "(function () {" + ReverseProxySetup +
            "  try { Array.prototype.reverse.call(proxy); return 'no throw'; }" +
            "  catch (e) { return e.message; }" +
            "})()"));

    [Fact]
    public void ReverseAccessesLowerThenUpperPerPair()
        => Assert.Equal("Get:length|Has:0|Get:0|Has:9007199254740990|Get:9007199254740990", Eval(
            "(function () {" + ReverseProxySetup +
            "  try { Array.prototype.reverse.call(proxy); } catch (e) {}" +
            "  return traps.slice(0, 5).join('|');" +
            "})()"));

    [Theory]
    [InlineData("[1,2,3,4,5].reverse().join(',')", "5,4,3,2,1")]
    [InlineData("(function(){ var a=[1,,3]; a.reverse(); return a[0]+','+(1 in a)+','+a[2]; })()", "3,false,1")]
    public void ReverseOrdinaryArraysStillWork(string expr, string expected)
        => Assert.Equal(expected, Eval(expr));
}
