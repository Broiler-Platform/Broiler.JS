using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// The numerics area's surviving copies in the engine, which milestone SJL-4.5 moves onto the shared
// library Broiler.ECMAScript.Algorithms: hand-written ToIntegerOrInfinity, ToLength, ToUint32,
// TimeClip, Temporal's ToIntegerWithTruncation, NumberToBigInt, a ratio's rounding and
// StrWhiteSpace's trim, each now the library's through an adapter. The operations themselves are
// judged by the shared corpus (conformance/vectors/numerics); the cases here are the call sites:
// the corrections where a copy disagreed with its clause, and a case of each site whose behaviour
// is unchanged.
public class SharedNumericsSurvivorsTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    private static string Throws(string expression)
        => Eval("try { " + expression + "; 'no error' } catch (e) { e.constructor.name }");

    // ── unchanged: the sites whose copy agreed with the library ─────────────────────────────────

    [Theory]
    [InlineData("'abc'.padStart(5.9, '-')", "--abc")]
    [InlineData("'abc'.padEnd(-1, '-')", "abc")]
    [InlineData("'abc'.padStart(NaN, '-')", "abc")]
    [InlineData("'a'.padStart(2 ** 40, '')", "a")]
    [InlineData("'ab'.repeat(2.9)", "abab")]
    [InlineData("'ab'.repeat(-0.5)", "")]
    [InlineData("'abcabc'.lastIndexOf('c', 4.9)", "2")]
    [InlineData("'abcabc'.lastIndexOf('a', -Infinity)", "0")]
    [InlineData("'abcabc'.lastIndexOf('', Infinity)", "6")]
    [InlineData("Iterator.from([1, 2, 3]).take(1.9).toArray().join()", "1")]
    [InlineData("Iterator.from([1, 2, 3]).drop(-0.5).toArray().join()", "1,2,3")]
    // The indent is what these cases read. The line terminator is not: the serializer writes the
    // platform's, CR LF on Windows, where the specification writes LF (a defect outside this area).
    [InlineData("JSON.stringify({ a: 1 }, null, 2.9).replace(/\\r\\n/g, '\\n')", "{\n  \"a\": 1\n}")]
    [InlineData("JSON.stringify({ a: 1 }, null, -Infinity)", "{\"a\":1}")]
    [InlineData("JSON.stringify({ a: 1 }, null, 1e9).replace(/\\r\\n/g, '\\n').length", "20")]
    [InlineData("String(new Uint8Array([1, 2, 3]).with(-0.5, 9))", "9,2,3")]
    [InlineData("String(new Uint8Array([1, 2, 3]).with(-1.5, 9))", "1,2,9")]
    [InlineData("String(new Uint8Array([1, 2, 3]).with(NaN, 9))", "9,2,3")]
    [InlineData("Array(-0).length", "0")]
    [InlineData("Array(2 ** 32 - 1).length", "4294967295")]
    public void Unchanged_integer_conversions(string expression, string expected)
        => Assert.Equal(expected, Eval("String(" + expression + ")"));

    [Theory]
    [InlineData("'ab'.repeat(Infinity)")]
    [InlineData("Iterator.from([1]).take(-1)")]
    [InlineData("new Uint8Array([1, 2, 3]).with(Infinity, 9)")]
    [InlineData("new Uint8Array([1, 2, 3]).with(-Infinity, 9)")]
    [InlineData("Array(2.5)")]
    [InlineData("Array(2 ** 32)")]
    [InlineData("Array(NaN)")]
    public void Unchanged_RangeErrors(string expression)
        => Assert.Equal("RangeError", Throws(expression));

    // A bound or wrapped function's length is max(ToIntegerOrInfinity(targetLen) - argCount, 0).
    [Theory]
    [InlineData("function g() {} Object.defineProperty(g, 'length', { value: 3.7 }); g.bind(null, 1).length", "2")]
    [InlineData("function g() {} Object.defineProperty(g, 'length', { value: -Infinity }); g.bind(null).length", "0")]
    [InlineData("function g() {} Object.defineProperty(g, 'length', { value: Infinity }); g.bind(null, 1).length", "Infinity")]
    [InlineData("function g() {} Object.defineProperty(g, 'length', { value: -0.5 }); Object.is(g.bind(null).length, 0)", "true")]
    public void A_bound_function_s_length(string source, string expected)
        => Assert.Equal(expected, Eval("String((function () { " + Last(source) + " })())"));

    [Theory]
    [InlineData("2.7", "2")]
    [InlineData("-0.5", "0")]
    [InlineData("Infinity", "Infinity")]
    [InlineData("-Infinity", "0")]
    public void A_wrapped_function_s_length(string targetLength, string expected)
        => Assert.Equal(expected, Eval(
            "var f = new ShadowRealm().evaluate('var g = function () {}; Object.defineProperty(g, \"length\", { value: " +
            targetLength + " }); g'); Object.is(f.length, -0) ? '-0' : String(f.length)"));

    // lastIndex is ToLength's, held within int, in exec and in the RegExp String Iterator alike.
    [Theory]
    [InlineData("var re = /a/g; re.lastIndex = -3.5; re.exec('aa'); re.lastIndex", "1")]
    [InlineData("var re = /a/g; re.lastIndex = 2 ** 40; String(re.exec('aa')) + re.lastIndex", "null0")]
    [InlineData("var re = /a/y; re.lastIndex = 1.9; re.test('aa') + ',' + re.lastIndex", "true,2")]
    [InlineData("[...'aXbX'.matchAll(/X/g)].map(m => m.index).join()", "1,3")]
    [InlineData("'a\\uD83D\\uDE00b'.match(/(?:)/gu).length", "4")]
    public void LastIndex_is_ToLength_s(string source, string expected)
        => Assert.Equal(expected, Eval("String((function () { " + Last(source) + " })())"));

    [Theory]
    [InlineData("function f() { arguments.length = 2.5; return [...arguments].join(); }; f(1, 2, 3)", "1,2")]
    [InlineData("function f() { arguments.length = -1; return [...arguments].length; }; f(1, 2, 3)", "0")]
    [InlineData("function f() { arguments.length = '2'; return [...arguments].join(); }; f(1, 2, 3)", "1,2")]
    public void An_arguments_object_iterates_to_its_ToLength(string source, string expected)
        => Assert.Equal(expected, Eval("String((function () { " + Last(source) + " })())"));

    [Theory]
    [InlineData("new Temporal.PlainDate(2020.9, 1.9, 2.9).toString()", "2020-01-02")]
    [InlineData("new Temporal.PlainTime(1.9, 2.9).toString()", "01:02:00")]
    [InlineData("new Temporal.PlainYearMonth(2020, 1.9).toString()", "2020-01")]
    [InlineData("Temporal.PlainDate.from({ year: 2020.5, month: 1.5, day: 2.5 }).toString()", "2020-01-02")]
    [InlineData("Temporal.PlainMonthDay.from({ month: 2.5, day: 29.9 }).toString()", "02-29")]
    [InlineData("Temporal.ZonedDateTime.from({ year: 2020, month: 1, day: 1, hour: 1.9, timeZone: 'UTC' }).hour", "1")]
    [InlineData("Temporal.PlainTime.from('12:34:56').round({ smallestUnit: 'minute', roundingIncrement: 2.9 }).toString()", "12:34:00")]
    [InlineData("Temporal.Duration.from({ hours: 5 }).round({ smallestUnit: 'hour', roundingIncrement: 2.9 }).toString()", "PT6H")]
    [InlineData("new Temporal.PlainDate(-0.5, 1, 1).year", "0")]
    public void Temporal_s_ToIntegerWithTruncation(string expression, string expected)
        => Assert.Equal(expected, Eval("String(" + expression + ")"));

    // ToIntegerWithTruncation's RangeError for NaN and the infinities, and each caller's own range.
    [Theory]
    [InlineData("new Temporal.PlainDate(2020, 1)")]
    [InlineData("new Temporal.PlainDateTime(2020, 1, 1, NaN)")]
    [InlineData("Temporal.PlainDate.from({ year: 2020, month: 1, day: Infinity })")]
    [InlineData("Temporal.PlainDate.from({ year: 2020, month: 1, day: -0.5 })")]
    [InlineData("Temporal.PlainTime.from('12:34:56').round({ smallestUnit: 'minute', roundingIncrement: NaN })")]
    [InlineData("Temporal.PlainTime.from('12:34:56').round({ smallestUnit: 'minute', roundingIncrement: 0.9 })")]
    [InlineData("Temporal.Duration.from({ hours: 5 }).round({ smallestUnit: 'hour', roundingIncrement: Infinity })")]
    [InlineData("Temporal.Duration.from({ hours: 5 }).round({ smallestUnit: 'hour', roundingIncrement: 0 })")]
    public void Temporal_s_ToIntegerWithTruncation_RangeErrors(string expression)
        => Assert.Equal("RangeError", Throws(expression));

    // hoursInDay is TotalTimeDuration's exact ratio rounded once; NumberToBigInt is the library's.
    [Theory]
    [InlineData("Temporal.ZonedDateTime.from('2020-03-08T12:00[America/New_York]').hoursInDay", "23")]
    [InlineData("Temporal.ZonedDateTime.from('2020-11-01T12:00[America/New_York]').hoursInDay", "25")]
    [InlineData("Temporal.ZonedDateTime.from('2020-01-08T12:00[UTC]').hoursInDay", "24")]
    [InlineData("Temporal.Instant.fromEpochMilliseconds(1234567).epochNanoseconds", "1234567000000")]
    [InlineData("Temporal.Instant.fromEpochMilliseconds(-8.64e15).epochNanoseconds", "-8640000000000000000000")]
    [InlineData("Temporal.Instant.fromEpochMilliseconds(-0).epochNanoseconds", "0")]
    [InlineData("new Date(-1).toTemporalInstant().epochNanoseconds", "-1000000")]
    [InlineData("new Date(8.64e15).toTemporalInstant().epochNanoseconds", "8640000000000000000000")]
    public void Temporal_s_ratio_and_NumberToBigInt(string expression, string expected)
        => Assert.Equal(expected, Eval("String(" + expression + ")"));

    [Theory]
    [InlineData("Temporal.Instant.fromEpochMilliseconds(1.5)")]
    [InlineData("Temporal.Instant.fromEpochMilliseconds(8.64e15 + 1)")]
    public void NumberToBigInt_s_RangeErrors(string expression)
        => Assert.Equal("RangeError", Throws(expression));

    [Theory]
    [InlineData("var a = 7; a -= 2; a *= 3; a /= 5; a %= 2; a", "1")]
    [InlineData("var a = 5; a &= 3; a |= 8; a ^= 1; a <<= 2; a >>= 1; a >>>= 1; a **= 2; a", "64")]
    [InlineData("var a = -1; a >>>= 0; a", "4294967295")]
    public void Compound_assignments(string source, string expected)
        => Assert.Equal(expected, Eval("String((function () { " + Last(source) + " })())"));

    // ── corrections ───────────────────────────────────────────────────────────────────────────────

    // StringPad: intMaxLength is ToLength's; a result past this engine's string length is a RangeError.
    [Fact]
    public void A_pad_longer_than_a_string_holds_is_a_RangeError()
        => Assert.Equal("RangeError", Throws("'a'.padStart(2 ** 32 + 3, '-')"));

    // GetSetRecord: intSize = ToIntegerOrInfinity(numSize) before the sign test, so -0.5 is 0.
    [Theory]
    [InlineData("new Set([1, 2]).union({ size: -0.5, has() { return false; }, keys() { return [3][Symbol.iterator](); } }).size", "3")]
    [InlineData("new Set([1, 2]).intersection({ size: -0.9, has() { return true; }, keys() { return [][Symbol.iterator](); } }).size", "0")]
    public void A_set_like_size_in_minus_one_to_zero_is_zero(string expression, string expected)
        => Assert.Equal(expected, Eval("String(" + expression + ")"));

    [Fact]
    public void A_set_like_size_of_minus_one_is_a_RangeError()
        => Assert.Equal("RangeError", Throws(
            "new Set([1]).union({ size: -1, has() { return false; }, keys() { return [][Symbol.iterator](); } })"));

    // SetTypedArrayFromArrayLike: srcLength is LengthOfArrayLike's, 0 for a negative length, so the
    // offset's bound applies to it.
    [Theory]
    [InlineData("var t = new Uint8Array(2); t.set({ length: -5 }, 3)")]
    [InlineData("var t = new Uint8Array(4); t.set({ length: Infinity }, 1)")]
    public void A_typed_array_set_s_source_length_is_ToLength_s(string source)
        => Assert.Equal("RangeError", Throws(source));

    [Theory]
    [InlineData("var t = new Uint8Array(2); t.set({ length: -1 }, 2); String(t)", "0,0")]
    [InlineData("var t = new Uint8Array(4); t.set({ length: 2.5, 0: 7, 1: 8, 2: 9 }, 1); String(t)", "0,7,8,0")]
    public void A_typed_array_set_s_source_length(string source, string expected)
        => Assert.Equal(expected, Eval("String((function () { " + Last(source) + " })())"));

    // RegExp.prototype[@@match] with an empty match: thisIndex = ToLength(lastIndex), then
    // AdvanceStringIndex, in double.
    [Theory]
    [InlineData("g", "-5", "abc", "1")]
    [InlineData("g", "2 ** 31", "abc", "2147483649")]
    [InlineData("g", "2 ** 32 + 1", "abc", "4294967298")]
    [InlineData("g", "1.5", "abc", "2")]
    [InlineData("gu", "-5", "abc", "1")]
    [InlineData("gu", "3", "abc", "4")]
    [InlineData("gu", "10", "abc", "11")]
    [InlineData("gu", "2.5", "ab\\uD83D\\uDE00", "4")]
    public void Match_advances_an_empty_match_from_ToLength_of_lastIndex(string flags, string lastIndex, string input, string expected)
        => Assert.Equal(expected, Eval(
            "var n = 0; var rx = { flags: '" + flags + "', lastIndex: 0, exec() { if (n++ > 0) return null; " +
            "this.lastIndex = " + lastIndex + "; return ['']; } }; " +
            "RegExp.prototype[Symbol.match].call(rx, '" + input + "'); String(rx.lastIndex)"));

    // CreateListFromArrayLike: LengthOfArrayLike, a [[Get]] of "length" and its ToLength; a length
    // past this engine's argument lists is a RangeError, not an empty list.
    [Theory]
    [InlineData("Function.prototype.apply.call(f, null, { length: 2 ** 32 })")]
    [InlineData("Function.prototype.apply.call(f, null, { length: 2 ** 31 })")]
    [InlineData("Reflect.apply(f, null, { length: 2 ** 32 })")]
    [InlineData("Reflect.construct(f, { length: 2 ** 32 })")]
    [InlineData("var a = []; a.length = 2 ** 31; f.apply(null, a)")]
    [InlineData("var a = []; a.length = 2 ** 32 - 1; f.apply(null, a)")]
    public void An_argument_list_too_long_is_a_RangeError(string expression)
        => Assert.Equal("RangeError", Throws("function f() { return arguments.length; } " + expression));

    [Theory]
    [InlineData("Function.prototype.apply.call(f, null, { length: -1 })", "")]
    [InlineData("Function.prototype.apply.call(f, null, { length: 2.5, 0: 'a', 1: 'b', 2: 'c' })", "a,b")]
    [InlineData("f.apply(null, { length: '2', 0: 'x', 1: 'y' })", "x,y")]
    [InlineData("f.apply(null, { get length() { return 2; }, 0: 'x', 1: 'y' })", "x,y")]
    [InlineData("Reflect.apply(f, null, Object.create({ length: 2, 0: 'a', 1: 'b' }))", "a,b")]
    [InlineData("Reflect.apply(f, null, new Proxy({ length: 2, 0: 'a', 1: 'b' }, {}))", "a,b")]
    [InlineData("f.apply(null, new String('ab'))", "a,b")]
    [InlineData("String(Reflect.construct(function () { this.n = arguments.length; }, Object.create({ length: 3 })).n)", "3")]
    [InlineData("Reflect.apply(f, null, new Proxy([1, 2, 3], {}))", "1,2,3")]
    [InlineData("Reflect.apply(f, null, new Proxy([1, 2, 3, 4, 5, 6], {}))", "1,2,3,4,5,6")]
    [InlineData("f.apply(null, new Proxy([1, 2], { get(t, k) { return k === 'length' ? 2 : 'x' + String(k); } }))", "x0,x1")]
    [InlineData("f.apply(null, [1, 2, 3, 4, 5, 6])", "1,2,3,4,5,6")]
    [InlineData("f.apply(null, new Uint8Array([1, 2, 3, 4, 5]))", "1,2,3,4,5")]
    [InlineData("String.fromCharCode.apply(null, [72, 105, 33, 33, 33])", "Hi!!!")]
    public void An_argument_list_is_LengthOfArrayLike_long(string expression, string expected)
        => Assert.Equal(expected, Eval("function f() { return [].join.call(arguments); } " + expression));

    // CreateListFromArrayLike step 4: each element is Get(obj, index), so a hole reads through the
    // prototype, whatever the array's length.
    [Theory]
    [InlineData("[0, , 2]", "0,p,2")]
    [InlineData("[0, , 2, 3, 4, 5]", "0,p,2,3,4,5")]
    public void An_argument_list_s_elements_are_Get_s(string array, string expected)
        => Assert.Equal(expected, Eval(
            "Array.prototype[1] = 'p'; (function () { return [].map.call(arguments, String).join(); }).apply(null, " + array + ")"));

    // String.raw: literalCount = LengthOfArrayLike(literals).
    [Theory]
    [InlineData("String.raw({ raw: 'abc' }, 1, 2)", "a1b2c")]
    [InlineData("String.raw({ raw: Object.create({ length: 2, 0: 'a', 1: 'b' }) }, '-')", "a-b")]
    [InlineData("String.raw({ raw: { length: 2.5, 0: 'a', 1: 'b', 2: 'c' } }, '-', '+')", "a-b")]
    [InlineData("String.raw({ raw: { length: -1, 0: 'a' } })", "")]
    [InlineData("String.raw`a${1}b`", "a1b")]
    public void String_raw_s_literal_count_is_LengthOfArrayLike(string expression, string expected)
        => Assert.Equal(expected, Eval(expression));

    [Theory]
    [InlineData("String.raw({ raw: { length: 2 ** 32 } })")]
    [InlineData("String.raw({ raw: { length: 2 ** 31 } })")]
    public void String_raw_with_too_many_literals_is_a_RangeError(string expression)
        => Assert.Equal("RangeError", Throws(expression));

    // TimeClip is 𝔽(ToIntegerOrInfinity(time)): -0 and a time in (-1, 0) are +0.
    [Theory]
    [InlineData("Object.is(new Date(0).setTime(-0.5), 0)")]
    [InlineData("Object.is(new Date(0).setTime(-0), 0)")]
    [InlineData("Object.is(new Date(-0).getTime(), 0)")]
    [InlineData("Object.is(new Date(-0.5).getTime(), 0)")]
    [InlineData("Object.is(Date.UTC(1970, 0, 1, 0, 0, 0, -0.5), 0)")]
    public void TimeClip_gives_positive_zero(string expression)
        => Assert.Equal("true", Eval("String(" + expression + ")"));

    // MakeTime and MakeDay take ToIntegerOrInfinity of each component, and setDate,
    // setMilliseconds and their UTC forms pass theirs through it, rather than adding a fraction.
    [Theory]
    [InlineData("new Date(1000).setUTCMilliseconds(-0.5)", "1000")]
    [InlineData("new Date(1000).setMilliseconds(-0.5) - new Date(1000).setMilliseconds(0)", "0")]
    [InlineData("new Date(Date.UTC(2020, 0, 1)).setUTCDate(1.5) - Date.UTC(2020, 0, 1)", "0")]
    [InlineData("new Date(Date.UTC(2020, 0, 3)).setUTCDate(-0.5) - Date.UTC(2020, 0, 3)", "-259200000")]
    [InlineData("new Date(2020, 0, 15, 12).setDate(1.5) - new Date(2020, 0, 15, 12).setDate(1)", "0")]
    [InlineData("new Date(Date.UTC(2020, 0, 1)).setUTCHours(1.5) - Date.UTC(2020, 0, 1)", "3600000")]
    [InlineData("new Date(Date.UTC(2020, 0, 1)).setUTCHours(0, 1.5) - Date.UTC(2020, 0, 1)", "60000")]
    [InlineData("new Date(Date.UTC(2020, 0, 1)).setUTCHours(0, 0, 1.5) - Date.UTC(2020, 0, 1)", "1000")]
    [InlineData("new Date(Date.UTC(2020, 0, 1)).setUTCHours(-0.5) - Date.UTC(2020, 0, 1)", "0")]
    [InlineData("new Date(Date.UTC(2020, 0, 1)).setUTCHours(-1.5) - Date.UTC(2020, 0, 1)", "-3600000")]
    public void A_Date_component_is_ToIntegerOrInfinity_s(string expression, string expected)
        => Assert.Equal(expected, Eval("String(" + expression + ")"));

    // The components the copies truncated already, through the library now.
    [Theory]
    [InlineData("Date.UTC(2020, -0.5) - Date.UTC(2020, 0)", "0")]
    [InlineData("Date.UTC(2020.9, 1.9, 2.9, 3.9, 4.9, 5.9, 6.9) - Date.UTC(2020, 1, 2, 3, 4, 5, 6)", "0")]
    [InlineData("Date.UTC(-0.5) - Date.UTC(1900)", "0")]
    [InlineData("Date.UTC(99.9) - Date.UTC(1999)", "0")]
    [InlineData("new Date(2020.9, 1.9, 2.9, 3.9, 4.9, 5.9, 6.9) - new Date(2020, 1, 2, 3, 4, 5, 6)", "0")]
    [InlineData("new Date(-0.5, 0) - new Date(1900, 0)", "0")]
    [InlineData("new Date(2020, -0.5) - new Date(2020, 0)", "0")]
    [InlineData("new Date(Date.UTC(2020, 5, 1)).setUTCFullYear(2021.7) - Date.UTC(2021, 5, 1)", "0")]
    [InlineData("new Date(Date.UTC(2020, 5, 1)).setUTCFullYear(2021, -0.5) - Date.UTC(2021, 0, 1)", "0")]
    [InlineData("new Date(Date.UTC(2020, 5, 1)).setFullYear(2021.7) - new Date(Date.UTC(2020, 5, 1)).setFullYear(2021)", "0")]
    [InlineData("new Date(NaN).setFullYear(2021.7, 1.5, 2.5) - new Date(NaN).setFullYear(2021, 1, 2)", "0")]
    [InlineData("new Date(Date.UTC(2020, 5, 1)).setUTCMonth(1.5) - Date.UTC(2020, 1, 1)", "0")]
    [InlineData("var d = new Date(2000, 5, 15); d.setYear(-0.5); d.getFullYear()", "1900")]
    [InlineData("var d = new Date(2000, 5, 15); d.setYear(99.9); d.getFullYear()", "1999")]
    [InlineData("var d = new Date(2000, 5, 15); d.setYear(2021.9); d.getFullYear()", "2021")]
    public void Unchanged_Date_components(string source, string expected)
        => Assert.Equal(expected, Eval("String((function () { " + Last(source) + " })())"));

    // Each argument's ToNumber happens once, in argument order, before the components are reduced.
    [Theory]
    [InlineData("new Date(0).setUTCHours(v('h'), v('m'), v('s'), v('ms'))", "h,m,s,ms")]
    [InlineData("new Date(NaN).setUTCHours(v('h'), v('m'))", "h,m")]
    [InlineData("Date.UTC(v('y'), v('m'), v('d'), v('h'), v('mi'), v('s'), v('ms'))", "y,m,d,h,mi,s,ms")]
    [InlineData("new Date(0).setUTCMilliseconds(v('a')); new Date(0).setMilliseconds(v('b')); new Date(0).setUTCDate(v('c')); new Date(0).setDate(v('d'))", "a,b,c,d")]
    public void A_Date_argument_is_converted_once_in_order(string calls, string expected)
        => Assert.Equal(expected, Eval(
            "var order = []; var v = n => ({ valueOf() { order.push(n); return 1.5; } }); " + calls + "; String(order)"));

    // Scientific and engineering notation format x × 10^-e of the Number's exact value x, rounded
    // once; e is the exact value's magnitude (the Number 9.999999999999999e22, which is also 1e23, is
    // below 10^23). The expected digits are the exact values' (1e-30 is 1.000000000000000083336…e-30).
    [Theory]
    [InlineData("scientific", "1e-30", "1.00000000000000008334E-30")]
    [InlineData("engineering", "1e-30", "1.00000000000000008334E-30")]
    [InlineData("scientific", "1.2345e-300", "1.23449999999999996222E-300")]
    [InlineData("engineering", "1.2345e-300", "1.23449999999999996222E-300")]
    [InlineData("scientific", "1e300", "1.0000000000000000525E300")]
    [InlineData("engineering", "1e300", "1.0000000000000000525E300")]
    [InlineData("scientific", "9.999999999999999e22", "9.99999999999999916114E22")]
    [InlineData("engineering", "9.999999999999999e22", "99.99999999999999161139E21")]
    [InlineData("scientific", "-1e-30", "-1.00000000000000008334E-30")]
    [InlineData("scientific", "5e-324", "4.94065645841246544177E-324")]
    [InlineData("scientific", "2.2250738585072014e-308", "2.22507385850720138309E-308")]
    [InlineData("engineering", "1.7976931348623157e308", "179.76931348623157081453E306")]
    [InlineData("scientific", "0.1", "1.00000000000000005551E-1")]
    [InlineData("scientific", "1e22", "1E22")]
    [InlineData("engineering", "1e22", "10E21")]
    public void Scientific_notation_formats_the_exact_value(string notation, string number, string expected)
        => Assert.Equal(expected, Eval(
            "new Intl.NumberFormat('en', { notation: '" + notation + "', maximumFractionDigits: 20 }).format(" + number + ")"));

    // At the default three fraction digits, the exact value rounds to what it always did.
    [Theory]
    [InlineData("scientific", "1e-30", "1E-30")]
    [InlineData("scientific", "1.2345e-300", "1.234E-300")]
    [InlineData("scientific", "1e300", "1E300")]
    [InlineData("scientific", "9.999999999999999e22", "1E23")]
    [InlineData("engineering", "9.999999999999999e22", "100E21")]
    [InlineData("engineering", "1e300", "1E300")]
    [InlineData("scientific", "123456", "1.235E5")]
    [InlineData("engineering", "123456", "123.456E3")]
    [InlineData("scientific", "0.000123", "1.23E-4")]
    [InlineData("scientific", "0", "0E0")]
    public void Scientific_notation_at_the_default_digits(string notation, string number, string expected)
        => Assert.Equal(expected, Eval(
            "new Intl.NumberFormat('en', { notation: '" + notation + "' }).format(" + number + ")"));

    // A subnormal Number's exact value is its significand × 2^-1074 (ExactRational scaled it by
    // 2^-1075), in standard notation too.
    [Theory]
    [InlineData("5e-324", "^0\\.0{323}494$")]
    [InlineData("1e-310", "^0\\.0{309}1$")]
    public void A_subnormal_Number_formats_its_exact_value(string number, string pattern)
        => Assert.Equal("true", Eval(
            "String(/" + pattern + "/.test(new Intl.NumberFormat('en', { maximumSignificantDigits: 3 }).format(" + number + ")))"));

    // StringIntlMV: the white space around a numeric string is StrWhiteSpace (U+FEFF is in it, U+0085
    // is not), and a 0x/0o/0b literal's value is StringToBigInt's.
    [Theory]
    [InlineData("'\\uFEFF0.1'", "0.1")]
    [InlineData("'0.1\\uFEFF'", "0.1")]
    [InlineData("' 0.1 '", "0.1")]
    [InlineData("'\\u00850.1'", "NaN")]
    [InlineData("'\\uFEFF'", "0")]
    [InlineData("'0x10'", "16")]
    [InlineData("'0X1f'", "31")]
    [InlineData("'0b101'", "5")]
    [InlineData("'0o17'", "15")]
    [InlineData("'0x1fffffffffffff1'", "144,115,188,075,855,857")]
    [InlineData("'\\uFEFF0x10\\uFEFF'", "16")]
    [InlineData("'0x1g'", "NaN")]
    [InlineData("'-0x10'", "NaN")]
    [InlineData("'12345678901234567890123'", "12,345,678,901,234,567,890,123")]
    public void A_numeric_string_is_StringIntlMV_s(string text, string expected)
        => Assert.Equal(expected, Eval("new Intl.NumberFormat('en', { maximumFractionDigits: 20 }).format(" + text + ")"));

    // Turns a statement list's last expression into a return.
    private static string Last(string source)
    {
        var at = source.LastIndexOf("; ", System.StringComparison.Ordinal);
        return at < 0 ? "return " + source + ";" : source[..(at + 2)] + "return " + source[(at + 2)..] + ";";
    }
}
