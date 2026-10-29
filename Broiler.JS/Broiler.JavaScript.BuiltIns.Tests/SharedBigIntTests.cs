using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// The corrections milestone SJL-4.5 makes where BigInt moved to the shared library
// Broiler.ECMAScript.Algorithms - the BigInt kernel, BigIntMath and BigIntText, under decision
// SJL-D-15 (a): no width but the representation's, no meter, and a result past the representation a
// RangeError - and the built-ins kept their coercions, errors and value wrapping. The kernel itself is
// judged by the shared corpus (conformance/vectors/numerics/bigint-*.vectors), which this engine's
// engine-level run reads; the cases here are each correction on the paths the engine has for it: at the
// top level, where every operand is a boxed value, and compiled inside a function, where a Number
// operand is a native double and `==` against it is the compiled EqualsLiteral(double).
public class SharedBigIntTests
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

    // The declarations and the expression at the top level, and again returned from a function.
    private static void AssertOnBothPaths(string expected, string declarations, string expression)
    {
        Assert.Equal(expected, Eval(declarations + " String(" + expression + ")"));
        Assert.Equal(expected, Eval("String((function () { " + declarations + " return " + expression + "; })())"));
    }

    private static void AssertThrowsOnBothPaths(string expected, string declarations, string expression)
    {
        Assert.Equal(expected, Eval("try { " + declarations + " " + expression + "; 'no error' } catch (e) { e.constructor.name }"));
        Assert.Equal(expected, Eval("(function () { try { " + declarations + " " + expression + "; return 'no error'; } catch (e) { return e.constructor.name; } })()"));
    }

    // BigInt::toString is culture-invariant: the radix-10 text was BigInteger.ToString() in the
    // current culture, so a culture whose negative sign is U+2212 wrote String(-1n) as "\u22121".
    [Theory]
    [InlineData("String(-1n)", "-1")]
    [InlineData("'' + -1n", "-1")]
    [InlineData("-1n + ''", "-1")]
    [InlineData("-1n + 'x'", "-1x")]
    [InlineData("`${-1n}`", "-1")]
    [InlineData("(-1n).toString()", "-1")]
    [InlineData("(-1n).toString(10)", "-1")]
    [InlineData("(-255n).toString(16)", "-ff")]
    [InlineData("String(-(2n ** 70n))", "-1180591620717411303424")]
    [InlineData("String(Object(-1n))", "-1")]
    [InlineData("[-1n, -2n].join()", "-1,-2")]
    [InlineData("Object.keys({ [-1n]: 0 })[0]", "-1")]
    public void A_BigInt_s_text_ignores_the_culture_s_negative_sign(string expression, string expected)
    {
        var saved = CultureInfo.CurrentCulture;
        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        culture.NumberFormat.NegativeSign = "\u2212";
        CultureInfo.CurrentCulture = culture;
        try
        {
            // The culture does reach the base class library's text, which the engine no longer reads.
            Assert.Equal("\u22121", BigInteger.MinusOne.ToString());
            AssertOnBothPaths(expected, "", expression);
        }
        finally
        {
            CultureInfo.CurrentCulture = saved;
        }
    }

    // BigInt::exponentiate takes an exponent of any size: 0n, 1n and -1n have exact powers, where the
    // exponent's 32-bit conversion raised a .NET OverflowException from 2^31 on.
    [Theory]
    [InlineData("1n ** 2n ** 31n", "1")]
    [InlineData("0n ** 2n ** 31n", "0")]
    [InlineData("(-1n) ** (2n ** 31n)", "1")]
    [InlineData("(-1n) ** (2n ** 64n + 1n)", "-1")]
    [InlineData("1n ** 0x10000000000000000n", "1")]
    [InlineData("0n ** 0n", "1")]
    [InlineData("2n ** 100n", "1267650600228229401496703205376")]
    [InlineData("(-3n) ** 3n", "-27")]
    public void Exponentiation_takes_an_exponent_of_any_size(string expression, string expected)
        => AssertOnBothPaths(expected, "", expression);

    // A result past the representation is the kernel's over-width status, a RangeError, never a .NET
    // exception: a left shift by 2^31 - 1 raised one, and BigInt.asUintN of a negative value at a width
    // of 2^31 or more computed its modulus as 1 << (int)bits, answering 0n, 7n or dividing by zero. The
    // representation is 2^31 - 128 bits, below .NET's 2^31 - 64: the last three, between the two, raised
    // an OverflowException while the library's limit was 2^31 - 1.
    [Theory]
    [InlineData("1n << 2147483647n")]
    [InlineData("1n << 2147483600n")]
    [InlineData("(-1n) << 2147483646n")]
    [InlineData("BigInt.asUintN(2147483600, -1n)")]
    [InlineData("1n << (2n ** 64n)")]
    [InlineData("1n >> -(2n ** 64n)")]
    [InlineData("2n ** (2n ** 64n)")]
    [InlineData("2n ** 2147483648n")]
    [InlineData("BigInt.asUintN(2 ** 32, -1n)")]
    [InlineData("BigInt.asUintN(2 ** 31, -1n)")]
    [InlineData("BigInt.asUintN(2 ** 32 + 3, -1n)")]
    [InlineData("BigInt.asUintN(2 ** 53 - 1, -5n)")]
    public void A_result_past_the_representation_is_a_RangeError(string expression)
        => AssertThrowsOnBothPaths("RangeError", "", expression);

    [Theory]
    [InlineData("5n >> -1n", "10")]
    [InlineData("-5n >> 1n", "-3")]
    [InlineData("123n >> 9999999999999n", "0")]
    [InlineData("-123n >> 9999999999999n", "-1")]
    [InlineData("1n << -(2n ** 64n)", "0")]
    [InlineData("-1n << -(2n ** 64n)", "-1")]
    [InlineData("0n << (2n ** 64n)", "0")]
    [InlineData("1n << 64n", "18446744073709551616")]
    public void Shifts_take_a_count_of_any_size(string expression, string expected)
        => AssertOnBothPaths(expected, "", expression);

    [Theory]
    [InlineData("BigInt.asUintN(64, -1n)", "18446744073709551615")]
    [InlineData("BigInt.asUintN(3, -1n)", "7")]
    [InlineData("BigInt.asUintN(2 ** 32, 5n)", "5")]
    [InlineData("BigInt.asUintN(2 ** 31, 2n ** 70n)", "1180591620717411303424")]
    [InlineData("BigInt.asUintN(2 ** 53 - 1, 5n)", "5")]
    [InlineData("BigInt.asUintN(0, -5n)", "0")]
    [InlineData("BigInt.asIntN(2 ** 32, -1n)", "-1")]
    [InlineData("BigInt.asIntN(2 ** 53 - 1, -(2n ** 64n))", "-18446744073709551616")]
    [InlineData("BigInt.asIntN(64, 2n ** 63n)", "-9223372036854775808")]
    [InlineData("BigInt.asIntN(3, 25n)", "1")]
    [InlineData("BigInt.asIntN(3, 4n)", "-4")]
    [InlineData("BigInt.asIntN(0, 5n)", "0")]
    [InlineData("BigInt.asIntN(undefined, 5n)", "0")]
    [InlineData("BigInt.asIntN(2.9, 3n)", "-1")]
    [InlineData("BigInt.asIntN(NaN, 3n)", "0")]
    public void AsIntN_and_asUintN_wrap_at_any_width(string expression, string expected)
        => AssertOnBothPaths(expected, "", expression);

    // ToIndex of the width comes first, so its RangeError precedes ToBigInt of the value.
    [Theory]
    [InlineData("BigInt.asUintN(2 ** 53, -1n)", "RangeError")]
    [InlineData("BigInt.asIntN(-1, 1n)", "RangeError")]
    [InlineData("BigInt.asIntN(-1, { valueOf() { throw new TypeError(); } })", "RangeError")]
    [InlineData("BigInt.asIntN(1n, 1n)", "TypeError")]
    public void AsIntN_and_asUintN_convert_the_width_with_ToIndex_first(string expression, string expected)
        => Assert.Equal(expected, Throws(expression));

    // ToBigInt (7.1.13) is ToPrimitive with the number hint, where it took the default hint, and a Number
    // is a TypeError; BigInt(value) takes the number hint too, and converts a Number with NumberToBigInt.
    [Theory]
    [InlineData("BigInt.asUintN(8, { [Symbol.toPrimitive](hint) { return hint === 'number' ? 1n : 2n; } })", "1")]
    [InlineData("BigInt.asIntN(8, { [Symbol.toPrimitive](hint) { return hint === 'number' ? 1n : 2n; } })", "1")]
    [InlineData("BigInt({ [Symbol.toPrimitive](hint) { return hint === 'number' ? 1n : 2n; } })", "1")]
    [InlineData("BigInt({ [Symbol.toPrimitive](hint) { return hint === 'number' ? 3 : 2n; } })", "3")]
    [InlineData("BigInt.asUintN(8, Object(5n))", "5")]
    [InlineData("BigInt.asUintN(8, { valueOf() { return '0x10'; } })", "16")]
    [InlineData("BigInt.asUintN(8, true)", "1")]
    public void ToBigInt_converts_with_the_number_hint(string expression, string expected)
        => Assert.Equal(expected, Eval("String(" + expression + ")"));

    [Theory]
    [InlineData("BigInt.asUintN(8, 1)", "TypeError")]
    [InlineData("BigInt.asUintN(8, { valueOf() { return 1; } })", "TypeError")]
    [InlineData("BigInt.asIntN(8, Object(1))", "TypeError")]
    [InlineData("BigInt.asIntN(8, undefined)", "TypeError")]
    [InlineData("BigInt.asIntN(8, null)", "TypeError")]
    [InlineData("BigInt.asIntN(8, Symbol())", "TypeError")]
    [InlineData("BigInt.asIntN(8, '1.5')", "SyntaxError")]
    public void ToBigInt_refuses_a_Number_and_a_text_that_is_not_an_integer(string expression, string expected)
        => Assert.Equal(expected, Throws(expression));

    // A BigInt64Array or BigUint64Array element, a DataView's BigInt setter and Atomics on a BigInt array
    // convert their value with ToBigInt too, so a Number is a TypeError and the hint is the number hint;
    // they called BigInt(value), which converts a Number.
    [Theory]
    [InlineData("new BigInt64Array([1])", "TypeError")]
    [InlineData("new BigUint64Array(1).fill(1)", "TypeError")]
    [InlineData("var a = new BigInt64Array(1); a[0] = 1", "TypeError")]
    [InlineData("BigInt64Array.from([1])", "TypeError")]
    [InlineData("new DataView(new ArrayBuffer(8)).setBigInt64(0, 1)", "TypeError")]
    [InlineData("new DataView(new ArrayBuffer(8)).setBigUint64(0, { valueOf() { return 1; } })", "TypeError")]
    [InlineData("Atomics.add(new BigInt64Array(1), 0, 1)", "TypeError")]
    [InlineData("Atomics.store(new BigUint64Array(1), 0, 1.5)", "TypeError")]
    public void A_BigInt_element_refuses_a_Number(string expression, string expected)
        => Assert.Equal(expected, Throws(expression));

    [Theory]
    [InlineData("String(new BigInt64Array([{ [Symbol.toPrimitive](hint) { return hint === 'number' ? 1n : 2n; } }]))", "1")]
    [InlineData("String(new BigUint64Array(['0x10', true]))", "16,1")]
    public void A_BigInt_element_converts_with_the_number_hint(string expression, string expected)
        => Assert.Equal(expected, Eval(expression));

    [Theory]
    [InlineData("BigInt(2 ** 64)", "18446744073709551616")]
    [InlineData("BigInt(-0)", "0")]
    [InlineData("BigInt(-(2 ** 53))", "-9007199254740992")]
    [InlineData("String(BigInt(Number.MAX_VALUE)).length", "309")]
    [InlineData("BigInt(true)", "1")]
    [InlineData("BigInt(Object(7n))", "7")]
    public void The_BigInt_constructor_converts_an_integral_Number_exactly(string expression, string expected)
        => Assert.Equal(expected, Eval("String(" + expression + ")"));

    [Theory]
    [InlineData("BigInt(1.5)", "RangeError")]
    [InlineData("BigInt(NaN)", "RangeError")]
    [InlineData("BigInt(-Infinity)", "RangeError")]
    [InlineData("BigInt('1.5')", "SyntaxError")]
    [InlineData("BigInt('-0x10')", "SyntaxError")]
    [InlineData("BigInt(undefined)", "TypeError")]
    [InlineData("BigInt(null)", "TypeError")]
    [InlineData("BigInt(Symbol())", "TypeError")]
    [InlineData("new (1n)()", "TypeError")]
    public void The_BigInt_constructor_raises_the_specification_s_errors(string expression, string expected)
        => Assert.Equal(expected, Throws(expression));

    // `==` of a BigInt and a Number is exact on every path: the compiled comparison against a native
    // double converted the BigInt to a double, which truncates, so these were true there.
    [Theory]
    [InlineData("var b = 2n ** 53n + 1n;", "b == 2 ** 53 + 1", "false")]
    [InlineData("var b = 2n ** 53n + 1n;", "b == 9007199254740993", "false")]
    [InlineData("var b = 2n ** 53n + 1n;", "9007199254740993 == b", "false")]
    [InlineData("var b = 2n ** 53n + 1n;", "b != 9007199254740993", "true")]
    [InlineData("var b = 2n ** 1024n;", "b == 1e400", "false")]
    [InlineData("var b = 2n ** 1024n;", "b == Infinity", "false")]
    [InlineData("var b = 2n ** 1024n;", "1e400 == b", "false")]
    [InlineData("var b = 2n ** 53n;", "b == 9007199254740992", "true")]
    [InlineData("var b = 2n ** 53n;", "b == 2 ** 53", "true")]
    [InlineData("var b = 1n;", "b == 1.5", "false")]
    [InlineData("var b = 0n;", "b == -0", "true")]
    [InlineData("var b = 1n;", "b == NaN", "false")]
    [InlineData("var b = 1n, n = 2 ** 53 + 1;", "2n ** 53n + 1n == n", "false")]
    [InlineData("", "2n ** 53n + 1n == 2 ** 53 + 1", "false")]
    [InlineData("", "2n ** 1024n == 1e400", "false")]
    public void A_BigInt_equals_a_Number_exactly(string declarations, string expression, string expected)
        => AssertOnBothPaths(expected, declarations, expression);

    [Theory]
    [InlineData("1n < 1.5", "true")]
    [InlineData("-1n < -0.5", "true")]
    [InlineData("1n < NaN", "false")]
    [InlineData("1n >= NaN", "false")]
    [InlineData("NaN <= 1n", "false")]
    [InlineData("2n ** 1100n < Infinity", "true")]
    [InlineData("-(2n ** 1100n) > -Infinity", "true")]
    [InlineData("2n ** 53n + 1n > 2 ** 53", "true")]
    [InlineData("1.5 < 2n", "true")]
    [InlineData("1n < '1.5'", "false")]
    [InlineData("1n >= '1.5'", "false")]
    [InlineData("1n < '2'", "true")]
    [InlineData("'0x10' > 15n", "true")]
    public void A_BigInt_compares_with_a_Number_and_a_String_exactly(string expression, string expected)
        => AssertOnBothPaths(expected, "", expression);

    // A BigInt property key is its decimal text: an array index only where that text is one, so
    // 4294967295n keys as "4294967295" does, and a key past 2^32 - 1 or below 0 is a string key, where
    // the (uint) conversion raised a .NET OverflowException.
    [Theory]
    [InlineData("var o = {}; o[2n ** 40n] = 1;", "o['1099511627776']", "1")]
    [InlineData("var o = {}; o[-1n] = 2;", "o['-1']", "2")]
    [InlineData("var o = {}; o[4294967295n] = 1;", "o['4294967295'] === 1", "true")]
    [InlineData("var o = {}; o['4294967295'] = 3;", "o[4294967295n]", "3")]
    [InlineData("var o = {}; o[2n ** 64n + 1n] = 4;", "o['18446744073709551617']", "4")]
    [InlineData("", "({})[2n ** 40n]", "undefined")]
    [InlineData("", "({})[-1n]", "undefined")]
    [InlineData("var o = {}; o[4294967295n] = 1; o[2n ** 64n] = 2; o[-1n] = 3; o[7n] = 4;", "Object.keys(o).join()", "7,4294967295,18446744073709551616,-1")]
    [InlineData("var a = []; a[1n] = 5;", "a.length", "2")]
    [InlineData("var a = []; a[4294967295n] = 5;", "a.length + ':' + a['4294967295']", "0:5")]
    [InlineData("var o = { 1099511627776n: 1, [-1n]: 2 };", "o['1099511627776'] + o['-1']", "3")]
    [InlineData("var o = { 4294967295n: 1 };", "o['4294967295']", "1")]
    [InlineData("var o = { 1099511627776n() {} };", "o['1099511627776'].name", "1099511627776")]
    [InlineData("var o = { 0x10n: 1 };", "o[16]", "1")]
    public void A_BigInt_keys_as_its_decimal_text(string declarations, string expression, string expected)
        => AssertOnBothPaths(expected, declarations, expression);

    // StringToBigInt of a prefixed text is linear: TryParsePrefixedDigits multiplied the whole value by
    // the radix for every digit, so a hexadecimal text of 262,144 digits did not complete within the
    // vector runner's five seconds.
    [Fact]
    public void A_long_hexadecimal_text_parses_in_linear_time()
    {
        Assert.Equal("1", Eval("String(1n)"));
        var watch = Stopwatch.StartNew();
        Assert.Equal("18446744073709551615", Eval("String(BigInt('0x' + 'f'.repeat(262144)) >> 1048512n)"));
        Assert.Equal("18446744073709551616", Eval("String(BigInt('0x1' + '0'.repeat(262144)) >> 1048512n)"));
        Assert.Equal("1", Eval("String(BigInt('0b1' + '0'.repeat(1048576)) >> 1048576n)"));
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(5), $"the parse took {watch.Elapsed}");
    }

    [Theory]
    [InlineData("(255n).toString(16)", "ff")]
    [InlineData("(-255n).toString(2)", "-11111111")]
    [InlineData("(35n).toString(36.9)", "z")]
    [InlineData("(255n).toString('16')", "ff")]
    [InlineData("(255n).toString(undefined)", "255")]
    [InlineData("(2n ** 64n).toString(36)", "3w5e11264sgsg")]
    [InlineData("(-(2n ** 64n)).toString(7)", "-45012021522523134134602")]
    public void ToString_reads_its_radix_with_ToIntegerOrInfinity(string expression, string expected)
        => Assert.Equal(expected, Eval("String(" + expression + ")"));

    [Theory]
    [InlineData("(1n).toString(0)")]
    [InlineData("(1n).toString(-0)")]
    [InlineData("(1n).toString(1)")]
    [InlineData("(1n).toString(37)")]
    [InlineData("(1n).toString(NaN)")]
    [InlineData("(1n).toString(null)")]
    [InlineData("(1n).toString(Infinity)")]
    public void ToString_refuses_a_radix_outside_2_to_36(string expression)
        => Assert.Equal("RangeError", Throws(expression));

    [Theory]
    [InlineData("7n / -2n", "-3")]
    [InlineData("-7n % 2n", "-1")]
    [InlineData("5n & -2n", "4")]
    [InlineData("5n | -8n", "-3")]
    [InlineData("5n ^ -1n", "-6")]
    [InlineData("~5n", "-6")]
    [InlineData("-(2n ** 64n)", "-18446744073709551616")]
    [InlineData("(2n ** 64n) * (2n ** 64n) - 1n", "340282366920938463463374607431768211455")]
    [InlineData("Object(2n) + 3n", "5")]
    public void The_operators_are_the_kernel_s(string expression, string expected)
        => AssertOnBothPaths(expected, "", expression);

    [Theory]
    [InlineData("var x = 2n ** 64n; x++;", "x", "18446744073709551617")]
    [InlineData("var x = -(2n ** 64n); x--;", "x", "-18446744073709551617")]
    [InlineData("var x = 0n; var y = x--;", "y + ':' + x", "0:-1")]
    public void Increment_and_decrement_are_add_and_subtract_with_one(string declarations, string expression, string expected)
        => AssertOnBothPaths(expected, declarations, expression);

    [Theory]
    [InlineData("1n / 0n", "RangeError")]
    [InlineData("1n % 0n", "RangeError")]
    [InlineData("1n ** -1n", "RangeError")]
    [InlineData("1n + 1", "TypeError")]
    [InlineData("1n * 1", "TypeError")]
    [InlineData("1n >>> 0n", "TypeError")]
    public void The_specification_s_errors_are_raised_before_the_kernel(string expression, string expected)
        => AssertThrowsOnBothPaths(expected, "", expression);

    // Correspondence section 4's cases with no difference between the engines, which still agree.
    [Theory]
    [InlineData("-0n", "0")]
    [InlineData("2n ** 64n", "18446744073709551616")]
    [InlineData("BigInt('  12 \uFEFF')", "12")]
    [InlineData("BigInt(' 0x10 ')", "16")]
    [InlineData("BigInt('')", "0")]
    [InlineData("BigInt('-0012')", "-12")]
    [InlineData("Number(2n ** 1024n)", "Infinity")]
    [InlineData("Number(2n ** 53n + 1n)", "9007199254740992")]
    [InlineData("Number(2n ** 1024n - 2n ** 970n)", "Infinity")]
    [InlineData("Number(2n ** 1024n - 2n ** 970n - 1n)", "1.7976931348623157e+308")]
    [InlineData("Number(-(2n ** 53n + 3n))", "-9007199254740996")]
    [InlineData("0n == ''", "true")]
    [InlineData("1n == '\uFEFF1'", "true")]
    [InlineData("0n == '0.'", "false")]
    public void The_cases_with_no_difference_still_agree(string expression, string expected)
        => AssertOnBothPaths(expected, "", expression);
}
