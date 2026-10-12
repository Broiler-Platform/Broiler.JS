using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// The corrections milestone SJL-3.5 makes at the level of the built-ins, where the conversions moved
// to the shared library Broiler.ECMAScript.Algorithms and the built-ins kept their coercions and
// RangeErrors. The conversions themselves are judged by the shared corpus
// (conformance/vectors/numbers), which this engine's engine-level run reads; the cases here are the
// argument handling the corpus does not hold. Each is listed in Broiler.JS/CHANGELOG.md.
public class SharedNumberConversionTests
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

    // §21.1.3.6: ToIntegerOrInfinity(radix) is 0 for 0, -0, null and NaN, and 0 is out of range.
    [Theory]
    [InlineData("(255).toString(0)")]
    [InlineData("(255).toString(-0)")]
    [InlineData("(255).toString(null)")]
    [InlineData("(255).toString(NaN)")]
    [InlineData("(255).toString(1)")]
    [InlineData("(255).toString(37)")]
    [InlineData("(255).toString(Infinity)")]
    public void A_radix_outside_2_to_36_is_a_RangeError(string expression)
        => Assert.Equal("RangeError", Throws(expression));

    [Theory]
    [InlineData("(255).toString()", "255")]
    [InlineData("(255).toString(undefined)", "255")]
    [InlineData("(255).toString(16.9)", "ff")]
    [InlineData("(255).toString('16')", "ff")]
    [InlineData("(0.1).toString(3)", "0.0022002200220022002200220022002201")]
    [InlineData("(2 ** 64).toString(16)", "10000000000000000")]
    public void The_radix_is_truncated_and_the_digits_are_the_shortest(string expression, string expected)
        => Assert.Equal(expected, Eval(expression));

    // §21.1.3.5: the precision is coerced before x is looked at, and its truncated value is checked.
    [Fact]
    public void ToPrecision_coerces_its_argument_before_looking_at_an_infinity()
        => Assert.Equal("Error", Throws("Infinity.toPrecision({ valueOf() { throw new Error('coerced'); } })"));

    [Theory]
    [InlineData("(1.5).toPrecision(100.5).length", "101")]
    [InlineData("(123.456).toPrecision(4.9)", "123.5")]
    [InlineData("Infinity.toPrecision(1000)", "Infinity")]
    [InlineData("NaN.toPrecision(0)", "NaN")]
    public void ToPrecision_reads_the_truncated_precision(string expression, string expected)
        => Assert.Equal(expected, Eval("String(" + expression + ")"));

    [Theory]
    [InlineData("(1).toPrecision(0.5)")]
    [InlineData("(1).toPrecision(101)")]
    public void ToPrecision_refuses_a_truncated_precision_outside_1_to_100(string expression)
        => Assert.Equal("RangeError", Throws(expression));

    // §19.2.5: ToString(string) first, so null is "null", whose digits read at radix 36.
    [Theory]
    [InlineData("parseInt(null, 36)", "1112745")]
    [InlineData("parseInt(undefined, 36)", "86464843759093")]
    [InlineData("parseInt(null)", "NaN")]
    [InlineData("parseInt('0x10', 16)", "16")]
    [InlineData("parseInt('0x10', 4294967312)", "16")]
    [InlineData("parseInt('123', 1)", "NaN")]
    public void ParseInt_converts_its_argument_and_its_radix(string expression, string expected)
        => Assert.Equal(expected, Eval("String(" + expression + ")"));

    [Fact]
    public void ParseInt_coerces_its_radix_once()
        => Assert.Equal("1", Eval("var n = 0; parseInt('10', { valueOf() { n++; return 16; } }); String(n)"));

    // Number.parseInt and Number.parseFloat are the global functions, the one pair left.
    [Fact]
    public void Number_parseInt_and_parseFloat_are_the_global_functions()
        => Assert.Equal("true,true", Eval("String([Number.parseInt === parseInt, Number.parseFloat === parseFloat])"));

    // The white-space set is the specification's: U+0085 is not in it, U+FEFF is.
    [Theory]
    [InlineData("parseInt('\\u0085 12')", "NaN")]
    [InlineData("parseInt('\\uFEFF12')", "12")]
    [InlineData("parseFloat('\\u0085 1.5')", "NaN")]
    [InlineData("'\\u0085x\\u0085'.trim().length", "3")]
    [InlineData("'\\uFEFF\\u3000x\\u2029'.trim()", "x")]
    public void The_white_space_set_is_the_specification_s(string expression, string expected)
        => Assert.Equal(expected, Eval("String(" + expression + ")"));

    // StringToBigInt trims the same set.
    [Theory]
    [InlineData("BigInt('\\uFEFF12\\uFEFF')", "12")]
    [InlineData("BigInt('\\u3000')", "0")]
    [InlineData("1n == '\\uFEFF1'", "true")]
    public void StringToBigInt_trims_the_specification_s_white_space(string expression, string expected)
        => Assert.Equal(expected, Eval("String(" + expression + ")"));

    [Fact]
    public void StringToBigInt_does_not_trim_next_line()
        => Assert.Equal("SyntaxError", Throws("BigInt('\\u008512')"));

    // Literal forms the grammar refuses, which the scanner's digit runs let through.
    [Theory]
    [InlineData("01n")]
    [InlineData("08n")]
    [InlineData("07.5")]
    [InlineData("1e")]
    [InlineData(".5n")]
    public void A_literal_the_grammar_refuses_is_a_SyntaxError(string literal)
        => Assert.Equal("SyntaxError", Throws("eval('" + literal + "')"));

    [Theory]
    [InlineData("0x20000000000001", "9007199254740992")]
    [InlineData("0x20000000000003", "9007199254740996")]
    [InlineData("Number('0x20000000000003')", "9007199254740996")]
    [InlineData("0b1_0", "2")]
    [InlineData("08.5", "8.5")]
    [InlineData("-0x10n", "-16")]
    [InlineData("({ 0x10n: 1 })[16]", "1")]
    public void Literal_values_are_correctly_rounded(string expression, string expected)
        => Assert.Equal(expected, Eval("String(" + expression + ")"));

    // A canonical numeric string is Number::toString's: "1e+21" is one, "1E+21" (.NET's "R") is not.
    [Theory]
    [InlineData("var t = new Float64Array(1); t['1e+21'] = 5; String(t['1e+21'])", "undefined")]
    [InlineData("var t = new Float64Array(1); t['1E+21'] = 5; String(t['1E+21'])", "5")]
    [InlineData("var t = new Float64Array(1); t['1e-7'] = 5; String(t['1e-7'])", "undefined")]
    [InlineData("var t = new Float64Array(1); t['1E-07'] = 5; String(t['1E-07'])", "5")]
    public void Canonical_numeric_keys_are_the_specification_s(string source, string expected)
        => Assert.Equal(expected, Eval(source));
}
