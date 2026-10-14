using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// The corrections milestone SJL-4.5 makes at the level of the built-ins, where the integer conversions
// and Float16 moved to the shared library Broiler.ECMAScript.Algorithms and the built-ins kept their
// coercions, RangeErrors and storage. The conversions themselves are judged by the shared corpus
// (conformance/vectors/numerics/integer-conversions.vectors and float16.vectors), which this engine's
// engine-level run reads; the cases here are the argument handling the corpus does not hold. Each is
// listed in Broiler.JS/CHANGELOG.md.
public class SharedIntegerConversionTests
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

    // AtomicReadModifyWrite works on the element's bytes: an operand of 2**53 or 2**63 wraps as
    // ToInt32 wraps it, rather than saturating to a 64-bit integer and summing through a double.
    [Theory]
    [InlineData("var a = new Int32Array([1]); Atomics.add(a, 0, 2 ** 53); a[0]", "1")]
    [InlineData("var a = new Int32Array([1]); Atomics.or(a, 0, 2 ** 53); a[0]", "1")]
    [InlineData("var a = new Int32Array([1]); Atomics.add(a, 0, 2 ** 32 + 2); a[0]", "3")]
    [InlineData("var a = new Int8Array([127]); Atomics.add(a, 0, 1); a[0]", "-128")]
    [InlineData("var a = new Uint16Array([0]); Atomics.sub(a, 0, 1); a[0]", "65535")]
    [InlineData("var a = new Int32Array([0]); Atomics.compareExchange(a, 0, 2 ** 63, 5); a[0]", "5")]
    [InlineData("var a = new Int32Array([0]); Atomics.compareExchange(a, 0, 2 ** 32, 7); a[0]", "7")]
    [InlineData("var a = new Uint8Array([255]); Atomics.compareExchange(a, 0, -1, 9); a[0]", "9")]
    [InlineData("var a = new BigInt64Array([1n]); Atomics.add(a, 0, 2n ** 64n + 2n); a[0]", "3")]
    [InlineData("var a = new BigUint64Array([0n]); Atomics.sub(a, 0, 1n); a[0]", "18446744073709551615")]
    [InlineData("var a = new BigInt64Array([-1n]); Atomics.compareExchange(a, 0, 2n ** 64n - 1n, 4n); a[0]", "4")]
    [InlineData("Object.is(Atomics.store(new Int32Array(1), 0, -0.5), 0)", "true")]
    public void Atomics_operate_on_the_element_s_bytes(string source, string expected)
        => Assert.Equal(expected, Eval("String((function () { " + Last(source) + " })())"));

    [Fact]
    public void Atomics_wait_reads_its_value_with_ToInt32()
        => Assert.Equal("timed-out", Eval("Atomics.wait(new Int32Array(new SharedArrayBuffer(4)), 0, 2 ** 63, 0)"));

    // ToIntegerOrInfinity, not ToInt32: an infinite end fills to the end.
    [Theory]
    [InlineData("String(new Uint8Array(3).fill(1, 0, Infinity))", "1,1,1")]
    [InlineData("String(new Uint8Array(3).fill(1, -Infinity))", "1,1,1")]
    [InlineData("String(new Uint8Array(3).fill(1, 2 ** 32))", "0,0,0")]
    public void TypedArray_fill_reads_its_range_with_ToIntegerOrInfinity(string expression, string expected)
        => Assert.Equal(expected, Eval(expression));

    // InitializeTypedArrayFromArrayBuffer: the offset's RangeError comes before ToIndex(length).
    [Fact]
    public void The_offset_s_RangeError_comes_before_the_length_is_converted()
        => Assert.Equal("RangeError", Throws("new Float64Array(new ArrayBuffer(16), 1, { valueOf() { throw new Error('length'); } })"));

    [Theory]
    [InlineData("new Uint8Array(new ArrayBuffer(8), -1)")]
    [InlineData("new Uint8Array(new ArrayBuffer(8), 2 ** 53)")]
    [InlineData("new Uint8Array(-1)")]
    public void A_typed_array_s_offset_and_length_are_ToIndex_s(string expression)
        => Assert.Equal("RangeError", Throws(expression));

    [Fact]
    public void A_fractional_negative_offset_truncates_to_zero()
        => Assert.Equal("8", Eval("String(new Uint8Array(new ArrayBuffer(8), -0.5).length)"));

    // %TypedArray%.from asks the constructor for ToLength of the source's length, 2**32 included.
    [Fact]
    public void TypedArray_from_passes_the_whole_length_to_its_constructor()
        => Assert.Equal("4294967296", Eval(
            "var seen; function C(n) { seen = n; throw new Error('stop'); } " +
            "try { Uint8Array.from.call(C, { length: 2 ** 32 }); } catch (e) { } String(seen)"));

    // A Date setter's arguments are ToIntegerOrInfinity's, not wrapped modulo 2**32.
    [Theory]
    [InlineData("new Date(Date.UTC(2020, 0, 1)).setUTCSeconds(2 ** 32 + 1) - Date.UTC(2020, 0, 1)", "4294967297000")]
    [InlineData("new Date(Date.UTC(2020, 0, 1)).setUTCMilliseconds(2 ** 32) - Date.UTC(2020, 0, 1)", "4294967296")]
    [InlineData("new Date(Date.UTC(2020, 0, 1)).setUTCMinutes(0, 0, -(2 ** 32)) - Date.UTC(2020, 0, 1)", "-4294967296")]
    public void A_Date_setter_s_component_is_not_wrapped(string expression, string expected)
        => Assert.Equal(expected, Eval("String(" + expression + ")"));

    // An undefined position is the clause's default, not +0.
    [Theory]
    [InlineData("'abc'.endsWith('c', undefined)", "true")]
    [InlineData("'abc'.endsWith('c', 2)", "false")]
    [InlineData("'abc'.endsWith('b', 2)", "true")]
    [InlineData("'abc'.endsWith('c', Infinity)", "true")]
    [InlineData("'abc'.startsWith('a', -Infinity)", "true")]
    [InlineData("'abc'.includes('c', Infinity)", "false")]
    [InlineData("'abc'.indexOf('c', -Infinity)", "2")]
    [InlineData("'abc'.slice(-Infinity, Infinity)", "abc")]
    [InlineData("'abc'.slice(-(2 ** 40))", "abc")]
    [InlineData("'abc'.substring(Infinity, -Infinity)", "abc")]
    [InlineData("'abc'.substr(-Infinity, 2)", "ab")]
    [InlineData("'abc'.substr(1, Infinity)", "bc")]
    [InlineData("'abc'.charAt(2 ** 32)", "")]
    [InlineData("'abc'.charCodeAt(-0.5)", "97")]
    [InlineData("'abc'.at(-Infinity)", "undefined")]
    [InlineData("'abc'.at(-1.5)", "c")]
    public void String_positions_are_ToIntegerOrInfinity_s(string expression, string expected)
        => Assert.Equal(expected, Eval("String(" + expression + ")"));

    // The specification's order: searchString's ToString before the position's ToNumber.
    [Theory]
    [InlineData("endsWith")]
    [InlineData("startsWith")]
    [InlineData("includes")]
    public void The_search_string_is_converted_before_the_position(string method)
        => Assert.Equal("search,position", Eval(
            "var order = []; 'abc'." + method + "({ toString() { order.push('search'); return 'a'; } }, " +
            "{ valueOf() { order.push('position'); return 0; } }); String(order)"));

    [Theory]
    [InlineData("String.fromCharCode(65 + 65536)", "A")]
    [InlineData("String.fromCharCode(-65535)", "\u0001")]
    [InlineData("String.fromCharCode(2 ** 32 + 66)", "B")]
    [InlineData("[1, [2, [3, [4]]]].flat(Infinity).join()", "1,2,3,4")]
    [InlineData("[1, [2]].flat(-Infinity).length", "2")]
    public void Other_integer_arguments(string expression, string expected)
        => Assert.Equal(expected, Eval("String(" + expression + ")"));

    // The narrowings and the clamp are the shared library's; the bytes are what they were.
    [Theory]
    [InlineData("String(new Int8Array([128, -129, 2 ** 53 + 2]))", "-128,127,2")]
    [InlineData("String(new Uint8Array([256, -1, Infinity]))", "0,255,0")]
    [InlineData("String(new Int16Array([32768, -32769]))", "-32768,32767")]
    [InlineData("String(new Uint16Array([65536, -1]))", "0,65535")]
    [InlineData("String(new Uint8ClampedArray([254.5, 253.5, 0.5, 1.5, -1, 300, NaN]))", "254,254,0,2,0,255,0")]
    [InlineData("String(new BigInt64Array([2n ** 63n]))", "-9223372036854775808")]
    [InlineData("String(new BigUint64Array([-1n]))", "18446744073709551615")]
    [InlineData("var v = new DataView(new ArrayBuffer(2)); v.setInt16(0, 65537); v.getInt16(0)", "1")]
    [InlineData("var v = new DataView(new ArrayBuffer(1)); v.setInt8(0, 255); v.getInt8(0)", "-1")]
    [InlineData("var v = new DataView(new ArrayBuffer(8)); v.setBigInt64(0, 2n ** 64n + 5n); v.getBigInt64(0)", "5")]
    public void Typed_array_elements_keep_their_bytes(string source, string expected)
        => Assert.Equal(expected, Eval("String((function () { " + Last(source) + " })())"));

    // Float16: rounding ties to even, and a NaN read back is a NaN; the NaN encoding (this engine's
    // choice: the sign and high payload bits kept) is the corpus's float16.vectors' to judge.
    [Theory]
    [InlineData("String(new Float16Array([65520, 65519.99, 2 ** -25]))", "Infinity,65504,0")]
    [InlineData("var v = new DataView(new ArrayBuffer(2)); v.setFloat16(0, 1); v.getUint16(0)", "15360")]
    [InlineData("var v = new DataView(new ArrayBuffer(2)); v.setUint16(0, 0x7C01); Number.isNaN(v.getFloat16(0))", "true")]
    public void Float16_is_the_shared_library_s(string source, string expected)
        => Assert.Equal(expected, Eval("String((function () { " + Last(source) + " })())"));

    // A copy between typed arrays of the same element type moves bytes (slice's step 14.g, copyWithin,
    // set, and the constructor's InitializeTypedArrayFromTypedArray), so a binary16 NaN keeps its sign
    // and payload, as a binary32 one does, where a NaN read as a value is the platform's. The cut-over
    // had made slice and copyWithin copy through the value, which test262's
    // TypedArray/prototype/slice/bit-precision.js found. slice's bytes go forwards, as the
    // specification's loop does, even when the result shares the source's buffer.
    [Theory]
    [InlineData("var u = new Uint16Array([0x7E01, 0xFE00]); String(new Uint16Array(new Float16Array(u.buffer).slice().buffer))", "32257,65024")]
    [InlineData("var u = new Uint16Array([0x7E01, 0xFE00, 0, 0]); new Float16Array(u.buffer).copyWithin(2, 0); String(u)", "32257,65024,32257,65024")]
    [InlineData("var u = new Uint16Array([0x7E01, 0xFE00]); var b = new Float16Array(2); b.set(new Float16Array(u.buffer)); String(new Uint16Array(b.buffer))", "32257,65024")]
    [InlineData("var u = new Uint16Array([0x7E01, 0xFE00]); String(new Uint16Array(new Float16Array(new Float16Array(u.buffer)).buffer))", "32257,65024")]
    [InlineData("var u = new Uint32Array([0x7FC00001]); String(new Uint32Array(new Float32Array(u.buffer).slice().buffer))", "2143289345")]
    [InlineData("String(new Uint8Array([1, 2, 3, 4, 5]).copyWithin(1, 0, 3))", "1,1,2,3,5")]
    [InlineData("String(new Uint8Array([1, 2, 3, 4, 5]).copyWithin(0, 2))", "3,4,5,4,5")]
    [InlineData("var a = new Int16Array(new ArrayBuffer(12), 2, 4); a.set([1, 2, 3, 4]); String(a.copyWithin(1, 0, 3))", "1,1,2,3")]
    [InlineData("var a = new Int16Array(new ArrayBuffer(12), 2, 4); a.set([1, -2, 3, -4]); String(a.slice(1, 3))", "-2,3")]
    [InlineData("String(new Int16Array(new Int16Array(new Int16Array([5, -6, 7, -8]).buffer, 2, 2)))", "-6,7")]
    [InlineData("var buf = new ArrayBuffer(8); var a = new Uint8Array(buf); a.set([1, 2, 3, 4, 5, 6, 7, 8]); a.constructor = { [Symbol.species]: function (n) { return new Uint8Array(buf, 1, n); } }; a.slice(0, 4); String(a)", "1,1,1,1,1,6,7,8")]
    [InlineData("var buf = new ArrayBuffer(8, { maxByteLength: 8 }); var a = new Uint16Array(buf); a.fill(1); String(a.slice({ valueOf() { return buf.resize(0), 2 } }))", "0,0")]
    public void A_copy_of_the_same_element_type_keeps_each_element_s_bytes(string source, string expected)
        => Assert.Equal(expected, Eval("String((function () { " + Last(source) + " })())"));

    // Turns a statement list's last expression into a return.
    private static string Last(string source)
    {
        var at = source.LastIndexOf("; ", System.StringComparison.Ordinal);
        return at < 0 ? "return " + source + ";" : source[..(at + 2)] + "return " + source[(at + 2)..] + ";";
    }
}
