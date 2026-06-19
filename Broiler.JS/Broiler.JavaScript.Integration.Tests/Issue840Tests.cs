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

    // ---- Problem 100: Intl.getCanonicalLocales validates the t-extension grammar ----
    //
    // The structural language-tag regex permits any 2–8 alphanum subtag inside a singleton
    // extension, so a malformed transform ("-t-") payload — e.g. "en-t-root" (tlang must be
    // alpha{2,3} or alpha{5,8}, not 4), or "en-t-d0" (tkey with no tvalue) — slipped through
    // structural validation. ValidateLanguageTag now also runs HasInvalidTransformedExtension,
    // which parses each "-t-" payload against UTS #35 §3.6 transformed_extensions: an optional
    // tlang (language, optional script, optional region, zero+ variants) followed by zero+
    // tfields (tkey = 2 chars alpha+digit; one or more tvalue chunks of 3–8 alphanum each).

    [Theory]
    [InlineData("'en-t-root'")]                        // tlang language must be alpha{2,3} or alpha{5,8}
    [InlineData("'en-t-abcdefghi'")]                   // and not 9 chars
    [InlineData("'en-t-ar-aao'")]                      // extlang is not allowed in a tlang
    [InlineData("'en-t-en-lat0'")]                     // unicode_script_subtag must be 4 alpha (not alphanum)
    [InlineData("'en-t-en-latn-latn'")]                // script can't repeat (and "latn" isn't a region/variant)
    [InlineData("'en-t-en-00'")]                       // region must be 2 alpha or 3 digit
    [InlineData("'en-t-en-x0'")]
    [InlineData("'en-t-en-latn-00'")]
    [InlineData("'en-t-en-latn-xyz'")]
    [InlineData("'en-t-en-latn-gb-ab'")]               // variant must be 5–8 alphanum or 4 char starting with digit
    [InlineData("'en-t-en-latn-gb-abc'")]
    [InlineData("'en-t-en-latn-gb-abcd'")]
    [InlineData("'en-t-d0'")]                          // tkey must be followed by at least one tvalue chunk
    [InlineData("'en-t-d0-m0'")]
    [InlineData("'en-t-d0-x-private'")]
    public void InvalidTExtensionThrowsRangeError(string tag)
        => Assert.Equal("true", Eval(
            "(function () {" +
            $"  try {{ Intl.getCanonicalLocales({tag}); return false; }}" +
            "  catch (e) { return e instanceof RangeError; }" +
            "})()"));

    [Theory]
    [InlineData("'en-t-en'")]
    [InlineData("'en-t-en-latn'")]
    [InlineData("'en-t-en-latn-us'")]
    [InlineData("'en-t-en-latn-us-fonipa'")]           // variant: 6 alphanum
    [InlineData("'en-t-en-latn-gb-1abc'")]              // variant: 4 char starting with digit
    [InlineData("'en-t-en-latn-gb-abcde'")]             // variant: 5 alphanum
    [InlineData("'en-t-d0-fwidth'")]                    // tkey "d0" + tvalue "fwidth"
    [InlineData("'en-US'")]                             // ordinary tags still pass
    [InlineData("'en-Latn-US'")]
    public void ValidTExtensionAndOrdinaryTagsStillCanonicalize(string tag)
        => Assert.Equal("string", Eval($"typeof Intl.getCanonicalLocales({tag})[0]"));

    // ---- Problem 51: ToPropertyDescriptor reads descriptor fields via Has/Get ----
    //
    // ToPropertyDescriptor (§6.2.5.5) probes the descriptor object with [[HasProperty]] then
    // [[Get]] for enumerable, configurable, value, writable, get, set (in that order). The
    // engine's NormalizeDescriptor read the fields straight out of internal storage, so a
    // scripted Proxy descriptor's has/get traps never fired (the test observed no operations).

    private const string LoggingProxyDescriptor =
        "function mk(){ var log = []; var p = new Proxy(" +
        "  { enumerable: true, configurable: true, value: 3, writable: true }," +
        "  { has(t, id){ log.push('has ' + id); return id in t; }," +
        "    get(t, id){ log.push('get ' + id); return t[id]; } });" +
        "  return { p: p, log: log }; }";

    [Theory]
    [InlineData("{}")]
    [InlineData("[]")]
    [InlineData("new Proxy({}, {})")]
    public void DefinePropertyReadsProxyDescriptorFieldsInSpecOrder(string target)
        => Assert.Equal(
            "has enumerable,get enumerable,has configurable,get configurable," +
            "has value,get value,has writable,get writable,has get,has set",
            Eval(
                "(function () {" + LoggingProxyDescriptor +
                $"  var m = mk(); Object.defineProperty({target}, 'x', m.p);" +
                "  return m.log.join(',');" +
                "})()"));

    [Fact]
    public void ReflectDefinePropertyReadsProxyDescriptorFieldsInSpecOrder()
        => Assert.Equal(
            "has enumerable,get enumerable,has configurable,get configurable," +
            "has value,get value,has writable,get writable,has get,has set",
            Eval(
                "(function () {" + LoggingProxyDescriptor +
                "  var m = mk(); Reflect.defineProperty({}, 'x', m.p);" +
                "  return m.log.join(',');" +
                "})()"));

    [Theory]
    [InlineData("(function(){ var o={}; Object.defineProperty(o,'x',{value:42,enumerable:true}); return o.x; })()", "42")]
    [InlineData("(function(){ var o={}; Object.defineProperty(o,'y',{get(){return 7;},configurable:true}); return o.y; })()", "7")]
    [InlineData("(function(){ var d=Object.create({enumerable:true,value:9}); var o={}; Object.defineProperty(o,'z',d); return o.z+','+Object.getOwnPropertyDescriptor(o,'z').enumerable; })()", "9,true")]
    public void DefinePropertyOrdinaryDescriptorsStillWork(string expr, string expected)
        => Assert.Equal(expected, Eval(expr));

    // ---- Problem 47: Object.keys runs [[GetOwnProperty]] per key (proxy traps) ----
    //
    // EnumerableOwnProperties(O, key) (§7.3.23) calls [[OwnPropertyKeys]] then, for each key,
    // [[GetOwnProperty]] to test enumerability. Object.keys used a generic enumerable-key walk
    // that, for a Proxy, fired only the ownKeys trap and never the per-key
    // getOwnPropertyDescriptor trap. It now mirrors Object.values/entries.

    [Fact]
    public void KeysOnProxiedArrayFiresOwnKeysThenGetOwnPropertyDescriptor()
        => Assert.Equal("ownKeys,getOwnPropertyDescriptor", Eval(
            "(function () {" +
            "  var log = [];" +
            "  Object.keys(new Proxy([], new Proxy({}, { get(t, pk, r) { log.push(pk); } })));" +
            "  return log.join(',');" +
            "})()"));

    [Fact]
    public void KeysFiltersNonEnumerableProxyKeys()
        => Assert.Equal("v", Eval(
            "(function () {" +
            "  var t = {};" +
            "  Object.defineProperty(t, 'hidden', { value: 1, enumerable: false });" +
            "  t.v = 2;" +
            "  return Object.keys(new Proxy(t, {})).join(',');" +
            "})()"));

    [Theory]
    [InlineData("Object.keys({a:1,b:2,c:3}).join(',')", "a,b,c")]
    [InlineData("Object.keys([10,20,30]).join(',')", "0,1,2")]
    [InlineData("typeof Object.keys([10,20])[0]", "string")]
    [InlineData("(function(){ var o={}; o[2]='a'; o.x='b'; o[1]='c'; return Object.keys(o).join(','); })()", "1,2,x")]
    [InlineData("(function(){ var o={}; Object.defineProperty(o,'x',{value:1,enumerable:false}); o.y=2; return Object.keys(o).join(','); })()", "y")]
    public void KeysOrdinaryObjectsStillWork(string expr, string expected)
        => Assert.Equal(expected, Eval(expr));

    // ---- Problems 1/2: AnnexB for-in var declaration with an initializer ----
    //
    // AnnexB B.3.7 permits a `var` binding with an initializer at the head of a for-in loop
    // (sloppy mode): the initializer is evaluated and assigned to the binding exactly once,
    // before the RHS is evaluated. The compiler dropped the initializer entirely, so the
    // binding was undefined (and any side effect in the initializer never ran).

    [Fact]
    public void ForInVarInitializerIsEvaluatedExactlyOnce()
        => Assert.Equal("1", Eval(
            "(function(){ var effects = 0; for (var a = ++effects in {}); return effects; })()"));

    [Fact]
    public void ForInVarInitializerIsAssignedBeforeRhs()
        => Assert.Equal("0", Eval(
            "(function(){ var stored; for (var a = 0 in stored = a, {}); return stored; })()"));

    [Fact]
    public void ForInVarInitializedValueSurvivesEmptyIteration()
        => Assert.Equal("0", Eval("(function(){ for (var a = 0 in {}); return a; })()"));

    [Fact]
    public void ForInVarInitializerFullSemantics()
        // stored sees the initialized -1; the initializer runs once; the body runs per key.
        => Assert.Equal("-1|1|3", Eval(
            "(function(){" +
            "  var effects = 0, iterations = 0, stored;" +
            "  for (var a = (++effects, -1) in stored = a, { a: 0, b: 1, c: 2 }) { ++iterations; }" +
            "  return stored + '|' + effects + '|' + iterations;" +
            "})()"));

    [Theory]
    [InlineData("(function(){ var keys=[]; for (var k = 99 in {x:1,y:2}) { keys.push(k); } return keys.join(','); })()", "x,y")]
    [InlineData("(function(){ var s=''; for (var k in {a:1,b:2}) { s += k; } return s; })()", "ab")]
    public void ForInIterationStillWorks(string expr, string expected)
        => Assert.Equal(expected, Eval(expr));

    // ---- Problem 23: splice passes a >2^32 deleteCount to the species constructor ----
    //
    // ArraySpeciesCreate(O, length) only clamps to the 2^32 array-index limit on the default
    // ArrayCreate path; a custom @@species constructor receives the full length (up to 2^53-1)
    // as its argument. CreateArraySpecies threw a RangeError up front for any length > 2^32-1,
    // so a splice whose (clamped) deleteCount exceeds that limit aborted before ever calling
    // the species, instead of constructing through it.

    [Fact]
    public void SpliceClampedDeleteCountReachesSpeciesAndPropagatesAbrupt()
        => Assert.Equal("StopSplice|9007199254740991", Eval(
            "(function () {" +
            "  function StopSplice() {}" +
            "  var targetLength;" +
            "  var array = ['no-hole', , 'stop'];" +
            "  var target = new Proxy([], { defineProperty(t, pk, d) {" +
            "    if (pk === '0' || pk === '1') return Reflect.defineProperty(t, pk, d);" +
            "    throw new StopSplice(); } });" +
            "  array.constructor = { [Symbol.species]: function (n) { targetLength = n; return target; } };" +
            "  var source = new Proxy(array, { get(t, pk, r) {" +
            "    if (pk === 'length') return 2 ** 53 + 2; return Reflect.get(t, pk, r); } });" +
            "  var thrown = 'none';" +
            "  try { Array.prototype.splice.call(source, 0, 2 ** 53 + 4); }" +
            "  catch (e) { thrown = e instanceof StopSplice ? 'StopSplice' : e.constructor.name; }" +
            "  return thrown + '|' + targetLength;" +
            "})()"));

    // ---- Problem 20: splice inserts items before the final length assignment ----
    //
    // Array.prototype.splice inserts the new items (step 16) before Set(O, "length", …) (step
    // 17). The engine set the new length first, so when a species constructor made "length"
    // non-writable mid-operation, the TypeError fired before the inserted item was written —
    // leaving the array in the wrong partial state.

    [Fact]
    public void SpliceWritesInsertedItemBeforeNonWritableLengthThrows()
        => Assert.Equal("TypeError|6|123,0,1,2,4,5", Eval(
            "(function () {" +
            "  var array = []; array.push(0, 1, 2);" +
            "  array.constructor = { [Symbol.species]: function (n) {" +
            "    array.push(3, 4, 5);" +
            "    Object.defineProperty(array, 'length', { writable: false });" +
            "    return new Array(n); } };" +
            "  var thrown = 'none';" +
            "  try { Array.prototype.splice.call(array, 0, 0, 123); }" +
            "  catch (e) { thrown = e instanceof TypeError ? 'TypeError' : e.constructor.name; }" +
            "  return thrown + '|' + array.length + '|' + array.join(',');" +
            "})()"));

    [Fact]
    public void SpliceRemovalWithNonWritableLengthLeavesExpectedState()
        => Assert.Equal("TypeError|6|1,2,,3,4,5", Eval(
            "(function () {" +
            "  var array = []; array.push(0, 1, 2);" +
            "  array.constructor = { [Symbol.species]: function (n) {" +
            "    array.push(3, 4, 5);" +
            "    Object.defineProperty(array, 'length', { writable: false });" +
            "    return new Array(n); } };" +
            "  var thrown = 'none';" +
            "  try { Array.prototype.splice.call(array, 0, 1); }" +
            "  catch (e) { thrown = e instanceof TypeError ? 'TypeError' : e.constructor.name; }" +
            "  return thrown + '|' + array.length + '|' + array.join(',');" +
            "})()"));

    [Theory]
    [InlineData("[1,2,3,4,5].splice(1,2).join(',')", "2,3")]
    [InlineData("(function(){ var a=[1,2,3,4,5]; a.splice(1,2,'x'); return a.join(','); })()", "1,x,4,5")]
    [InlineData("(function(){ var a=[1,2,3]; a.splice(1,0,'a','b'); return a.join(','); })()", "1,a,b,2,3")]
    [InlineData("(function(){ var a=[1,2,3]; a.constructor={[Symbol.species]:function(n){return new Array(n);}}; return a.splice(0,2).length; })()", "2")]
    public void SpliceOrdinaryArraysStillWork(string expr, string expected)
        => Assert.Equal(expected, Eval(expr));

    // ---- Problem 79: %Iterator.prototype%[@@toStringTag] getter name ----
    //
    // The getter was created with the name "get [Symbol.toStringTag]" but the factory already
    // prepends the "get " accessor prefix, so the name and Function.prototype.toString doubled
    // it ("function get get [Symbol.toStringTag]() …"), which is not valid NativeFunction syntax.

    private static string IteratorToStringTagGetter =>
        "Object.getOwnPropertyDescriptor(Iterator.prototype, Symbol.toStringTag).get";

    [Fact]
    public void IteratorToStringTagGetterHasSingleGetPrefixInName()
        => Assert.Equal("get [Symbol.toStringTag]", Eval($"{IteratorToStringTagGetter}.name"));

    [Fact]
    public void IteratorToStringTagGetterConformsToNativeFunctionSyntax()
        => Assert.Equal("true", Eval(
            "/^function get \\[Symbol\\.toStringTag\\]\\(\\) \\{ \\[native code\\] \\}$/.test(" +
            $"{IteratorToStringTagGetter}.toString())"));

    [Fact]
    public void IteratorToStringTagSetterHasSingleSetPrefixInName()
        => Assert.Equal("set [Symbol.toStringTag]", Eval(
            "Object.getOwnPropertyDescriptor(Iterator.prototype, Symbol.toStringTag).set.name"));

    [Fact]
    public void IteratorToStringTagGetterStillReportsIterator()
        => Assert.Equal("Iterator", Eval($"{IteratorToStringTagGetter}.call({{}})"));
}
