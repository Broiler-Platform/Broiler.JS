using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// The corrections milestone SJL-4.5 makes where the Math kernels moved to the shared library
// Broiler.ECMAScript.Algorithms: Number::exponentiate is MathKernels.Power on every path the
// compiler takes for ** and Math.pow, and Math.round, hypot, cbrt, expm1, log1p, f16round and
// sumPrecise are adapters that keep their coercions, errors and iteration. The kernels themselves
// are judged by the shared corpus (conformance/vectors/numerics), which this engine's engine-level
// run reads; the cases here are the paths, the argument handling and the iteration the corpus does
// not hold.
public class SharedMathTests
{
    // $f64 builds a double from its two 32-bit halves, as the corpus's prelude does, and $bits reads
    // one back as sixteen upper-case hexadecimal digits.
    private const string Prelude = """
        function $f64(hi, lo) { var v = new DataView(new ArrayBuffer(8)); v.setUint32(0, hi); v.setUint32(4, lo); return v.getFloat64(0); }
        function $hex(n) { return n.toString(16).toUpperCase().padStart(8, '0'); }
        function $bits(x) { var v = new DataView(new ArrayBuffer(8)); v.setFloat64(0, x); return $hex(v.getUint32(0)) + $hex(v.getUint32(4)); }

        """;

    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source, JavaScriptFeatureFlags features = JavaScriptFeatureFlags.None)
    {
        Load();
        using var ctx = new JSContext(experimentalFeatures: features);
        return ctx.Eval(Prelude + source).ToString();
    }

    private static string Throws(string expression, JavaScriptFeatureFlags features = JavaScriptFeatureFlags.None)
        => Eval("try { " + expression + "; 'no error' } catch (e) { e.constructor.name }", features);

    // NaN and the infinities spelled without a name, so that a local initialised with one is a
    // numeric local and the operator over two of them is the compiler's native emission.
    private static string Numeric(string value) => value switch
    {
        "NaN" => "(0 / 0)",
        "Infinity" => "(1 / 0)",
        "-Infinity" => "(-1 / 0)",
        _ => "(" + value + ")",
    };

    // Each spelling reaches ** by a different route: literals and the global names (the generic
    // operator, or the native one when both sides are numeric already), numeric literals only (the
    // native emission), numeric locals of a compiled function, a tree over element reads (the
    // speculative guard, native when both are Numbers), parameters, property reads and objects with
    // valueOf (the generic operator), **= on a numeric local, on a property and on a global, and
    // Math.pow.
    private static string EveryPath(string b, string e)
    {
        var nb = Numeric(b);
        var ne = Numeric(e);
        return $$"""
            var r = [];
            r.push('names:' + (({{b}}) ** ({{e}})));
            r.push('numeric:' + ({{nb}} ** {{ne}}));
            r.push('locals:' + (function () { var x = {{nb}}, y = {{ne}}; return x ** y; })());
            r.push('speculated:' + (function (a) { return a[0] ** a[1] * 1; })([{{b}}, {{e}}]));
            r.push('parameters:' + (function (x, y) { return x ** y; })({{b}}, {{e}}));
            r.push('properties:' + (function (o) { return o.x ** o.y; })({ x: {{b}}, y: {{e}} }));
            r.push('valueOf:' + ({ valueOf() { return {{b}}; } } ** { valueOf() { return {{e}}; } }));
            r.push('assign-local:' + (function () { var x = {{nb}}; x **= {{ne}}; return x; })());
            r.push('assign-property:' + (function (o) { o.x **= {{e}}; return o.x; })({ x: {{b}} }));
            var g = {{b}}; g **= {{e}};
            r.push('assign-global:' + g);
            r.push('Math.pow:' + Math.pow({{b}}, {{e}}));
            r.join(' ')
            """;
    }

    private static string Expected(string result)
        => string.Join(' ', new[] { "names", "numeric", "locals", "speculated", "parameters", "properties", "valueOf", "assign-local", "assign-property", "assign-global", "Math.pow" }
            .Select(path => path + ":" + result));

    // ECMA-262 6.1.6.1.3: a NaN exponent is NaN, and 1 or -1 to an infinity is NaN, where IEEE 754's
    // pow answers 1.
    [Theory]
    [InlineData("1", "NaN")]
    [InlineData("1", "Infinity")]
    [InlineData("1", "-Infinity")]
    [InlineData("-1", "Infinity")]
    [InlineData("-1", "-Infinity")]
    [InlineData("-1", "NaN")]
    public void Number_exponentiate_of_1_or_minus_1_is_NaN_on_every_path(string b, string e)
        => Assert.Equal(Expected("NaN"), Eval(EveryPath(b, e)));

    // The cases IEEE 754's pow already answered as the clause does stay as they were.
    [Theory]
    [InlineData("NaN", "0", "1")]
    [InlineData("NaN", "-0", "1")]
    [InlineData("1", "0", "1")]
    [InlineData("2", "10", "1024")]
    [InlineData("-8", "1 / 3", "NaN")]
    [InlineData("-0", "-3", "-Infinity")]
    [InlineData("0", "-1", "Infinity")]
    [InlineData("0.5", "Infinity", "0")]
    [InlineData("2", "-1074", "5e-324")]
    public void Number_exponentiate_keeps_the_cases_pow_answers_as_specified(string b, string e, string expected)
        => Assert.Equal(Expected(expected), Eval(EveryPath(b, e)));

    // Math.pow converts its base, then its exponent.
    [Fact]
    public void Math_pow_converts_its_arguments_in_order()
        => Assert.Equal("be:8", Eval("""
            var log = '';
            var r = Math.pow({ valueOf() { log += 'b'; return 2; } }, { valueOf() { log += 'e'; return 3; } });
            log + ':' + r
            """));

    // §21.3.2.9: correctly rounded, so an exact cube root is exact at every magnitude.
    [Theory]
    [InlineData("Math.cbrt(0.125)", "0.5")]
    [InlineData("Math.cbrt(27)", "3")]
    [InlineData("Math.cbrt(-8)", "-2")]
    [InlineData("Math.cbrt(1e30)", "10000000000")]
    [InlineData("$bits(Math.cbrt($f64(0x00000000, 0x00000008)))", "29A0000000000000")]
    [InlineData("$bits(Math.cbrt($f64(0x80000000, 0x00000008)))", "A9A0000000000000")]
    [InlineData("$bits(Math.cbrt(Number.MIN_VALUE))", "2990000000000000")]
    [InlineData("$bits(Math.cbrt(Number.MAX_VALUE))", "554428A2F98D728B")]
    [InlineData("1 / Math.cbrt(-0)", "-Infinity")]
    [InlineData("Math.cbrt(-Infinity)", "-Infinity")]
    public void Math_cbrt_is_correctly_rounded(string expression, string expected)
        => Assert.Equal(expected, Eval("String(" + expression + ")"));

    // §21.3.2.18: within one unit in the last place, and exact where the hypotenuse is a double.
    [Theory]
    [InlineData("Math.hypot(2, 10, 11)", "15")]
    [InlineData("$bits(Math.hypot($f64(0x7E800000, 0), $f64(0x7EA40000, 0), $f64(0x7EA60000, 0)))", "7EAE000000000000")]
    [InlineData("$bits(Math.hypot($f64(0x01800000, 0), $f64(0x01A40000, 0), $f64(0x01A60000, 0)))", "01AE000000000000")]
    [InlineData("Math.hypot(3, 4)", "5")]
    [InlineData("Math.hypot(-3)", "3")]
    [InlineData("Math.hypot(NaN, -Infinity)", "Infinity")]
    [InlineData("Math.hypot(1, NaN)", "NaN")]
    [InlineData("1 / Math.hypot()", "Infinity")]
    [InlineData("1 / Math.hypot(-0, -0)", "Infinity")]
    [InlineData("Math.hypot(1e308, 1e308)", "1.4142135623730951e+308")]
    [InlineData("Math.hypot(5e-324, 5e-324)", "5e-324")]
    public void Math_hypot_is_within_a_unit_in_the_last_place(string expression, string expected)
        => Assert.Equal(expected, Eval("String(" + expression + ")"));

    // §21.3.2.18 steps 2-3: every argument is converted, in order, before any value is looked at, so
    // an infinity among the first does not spare the rest their valueOf.
    [Fact]
    public void Math_hypot_converts_every_argument_in_order_before_it_computes()
        => Assert.Equal("abc:Infinity", Eval("""
            var log = '';
            function v(name, value) { return { valueOf() { log += name; return value; } }; }
            var r = Math.hypot(v('a', Infinity), v('b', NaN), v('c', 1));
            log + ':' + r
            """));

    [Fact]
    public void Math_hypot_propagates_a_later_argument_s_conversion_error()
        => Assert.Equal("RangeError", Throws("Math.hypot(Infinity, { valueOf() { throw new RangeError('coerced'); } })"));

    // §21.3.2.15 and §21.3.2.21: accurate near zero and within one unit in the last place elsewhere.
    [Theory]
    [InlineData("Math.expm1(1e-300)", "1e-300")]
    [InlineData("Math.expm1(-1e-300)", "-1e-300")]
    [InlineData("1 / Math.expm1(-0)", "-Infinity")]
    [InlineData("Math.expm1(-745) >= -1", "true")]
    [InlineData("Math.expm1(-Infinity)", "-1")]
    [InlineData("isFinite(Math.expm1(709.78))", "true")]
    [InlineData("Math.log1p(1e-300)", "1e-300")]
    [InlineData("1 / Math.log1p(-0)", "-Infinity")]
    [InlineData("Math.log1p(-1)", "-Infinity")]
    [InlineData("Math.log1p(-2)", "NaN")]
    public void Math_expm1_and_log1p_are_accurate_near_zero(string expression, string expected)
        => Assert.Equal(expected, Eval("String(" + expression + ")"));

    // Either double bracketing the exact value: the corpus's vectors, and log1p just above 0.01,
    // where a series stopped and log(1 + x) was tens of units in the last place out.
    [Theory]
    [InlineData("Math.expm1(-745)", "BFF0000000000000", "BFEFFFFFFFFFFFFF")]
    [InlineData("Math.expm1(709.78)", "7FEFE9CE5C4C52B4", "7FEFE9CE5C4C52B5")]
    [InlineData("Math.expm1($f64(0xBCB00000, 0))", "BCAFFFFFFFFFFFFF", "BCB0000000000000")]
    [InlineData("Math.expm1($f64(0x3EBFCB59, 0xC5586F30))", "3EBFCB5BBEC926C2", "3EBFCB5BBEC926C1")]
    [InlineData("Math.log1p(0.012566334936294443)", "3F899353876FD155", "3F899353876FD154")]
    [InlineData("Math.log1p($f64(0xBD6F923E, 0x9A58B2E3))", "BD6F923E9A58C276", "BD6F923E9A58C275")]
    public void Math_expm1_and_log1p_are_within_a_unit_in_the_last_place(string expression, string nearest, string other)
        => Assert.Contains(Eval("$bits(" + expression + ")"), new[] { nearest, other });

    // §21.3.2.28: a tie rounds toward +Infinity, and -0 from -0.5 up to -0.
    [Theory]
    [InlineData("Math.round(2.5)", "3")]
    [InlineData("Math.round(-2.5)", "-2")]
    [InlineData("Math.round(0.49999999999999994)", "0")]
    [InlineData("1 / Math.round(-0.5)", "-Infinity")]
    [InlineData("1 / Math.round(-0)", "-Infinity")]
    [InlineData("Math.round(4503599627370495.5)", "4503599627370496")]
    [InlineData("Math.round(-4503599627370495.5)", "-4503599627370495")]
    [InlineData("Math.round(undefined)", "NaN")]
    [InlineData("Math.round(null)", "0")]
    [InlineData("Math.round({ valueOf() { return 1.5; } })", "2")]
    [InlineData("typeof Math.round(2.5m)", "decimal")]
    public void Math_round_is_the_specification_s(string expression, string expected)
        => Assert.Equal(expected, Eval("String(" + expression + ")"));

    // §21.3.2.17: rounded once to the nearest binary16, ties to even.
    [Theory]
    [InlineData("Math.f16round(5.5)", "5.5")]
    [InlineData("Math.f16round(1.337)", "1.3369140625")]
    [InlineData("Math.f16round(65519.99)", "65504")]
    [InlineData("Math.f16round(65520)", "Infinity")]
    [InlineData("Math.f16round(2 ** -25)", "0")]
    [InlineData("Math.f16round(2 ** -25 + 2 ** -60)", "5.960464477539063e-8")]
    [InlineData("1 / Math.f16round(-0)", "-Infinity")]
    [InlineData("Math.f16round(NaN)", "NaN")]
    public void Math_f16round_rounds_once_to_binary16(string expression, string expected)
        => Assert.Equal(expected, Eval("String(" + expression + ")"));

    // The lengths the generated registration infers or is told.
    [Fact]
    public void The_adapters_keep_their_lengths()
        => Assert.Equal("1,2,2,1,1,1,1", Eval("[Math.round, Math.pow, Math.hypot, Math.cbrt, Math.expm1, Math.log1p, Math.f16round].map(f => f.length).join()"));

    // §21.3.2.34: the iteration is the engine's - GetIterator(items, sync), each step, and a value
    // that is not a Number refused with a TypeError that closes the iterator - and the sum is the
    // shared PreciseSum's. Math.sumPrecise is behind its feature flag, which is this engine's policy.
    private const JavaScriptFeatureFlags SumPrecise = JavaScriptFeatureFlags.MathSumPrecise;

    [Fact]
    public void Math_sumPrecise_is_absent_unless_its_feature_flag_is_set()
    {
        Assert.Equal("undefined", Eval("typeof Math.sumPrecise"));
        Assert.Equal("function,1", Eval("typeof Math.sumPrecise + ',' + Math.sumPrecise.length", SumPrecise));
    }

    [Theory]
    [InlineData("Math.sumPrecise({ length: 1, 0: 5 })")]
    [InlineData("Math.sumPrecise(5)")]
    [InlineData("Math.sumPrecise()")]
    [InlineData("Math.sumPrecise(null)")]
    [InlineData("Math.sumPrecise([1, '2'])")]
    [InlineData("Math.sumPrecise([1, , 2])")]
    [InlineData("Math.sumPrecise([1, 2n])")]
    [InlineData("Math.sumPrecise([new Number(1)])")]
    [InlineData("Math.sumPrecise('12')")]
    public void Math_sumPrecise_refuses_what_is_not_an_iterable_of_Numbers(string expression)
        => Assert.Equal("TypeError", Throws(expression, SumPrecise));

    [Fact]
    public void Math_sumPrecise_closes_the_iterator_on_a_value_that_is_not_a_Number()
        => Assert.Equal("TypeError:1:1", Eval("""
            var closed = 0, steps = 0;
            var items = { [Symbol.iterator]() { return { next() { steps++; return { value: steps === 1 ? 1 : 'x', done: false }; }, return() { closed++; return {}; } }; } };
            var name;
            try { Math.sumPrecise(items); } catch (e) { name = e.constructor.name; }
            name + ':' + closed + ':' + (steps - 1)
            """, SumPrecise));

    // IteratorClose with a throw completion: the TypeError is what propagates, whatever return() does.
    [Theory]
    [InlineData("throw new RangeError('closing')")]
    [InlineData("return 5")]
    public void Math_sumPrecise_throws_its_TypeError_whatever_return_does(string returnBody)
        => Assert.Equal("TypeError", Throws(
            "Math.sumPrecise({ [Symbol.iterator]() { return { next() { return { value: 'x', done: false }; }, return() { " + returnBody + "; } }; } })",
            SumPrecise));

    [Fact]
    public void Math_sumPrecise_does_not_close_an_iterator_it_exhausted()
        => Assert.Equal("3:0", Eval("""
            var closed = 0, i = 0;
            var items = { [Symbol.iterator]() { return { next() { i++; return i <= 2 ? { value: i, done: false } : { done: true }; }, return() { closed++; return {}; } }; } };
            Math.sumPrecise(items) + ':' + closed
            """, SumPrecise));

    [Theory]
    [InlineData("Object.is(Math.sumPrecise([]), -0)", "true")]
    [InlineData("Object.is(Math.sumPrecise(''), -0)", "true")]
    [InlineData("Object.is(Math.sumPrecise([-0, -0]), -0)", "true")]
    [InlineData("Object.is(Math.sumPrecise([-0, 0]), 0)", "true")]
    [InlineData("Math.sumPrecise([1e308, -1e308, 5e-324])", "5e-324")]
    [InlineData("Math.sumPrecise([0.1, 0.2])", "0.30000000000000004")]
    [InlineData("Math.sumPrecise([1e308, 1e308, -1e308])", "1e+308")]
    [InlineData("Math.sumPrecise([Infinity, -Infinity])", "NaN")]
    [InlineData("Math.sumPrecise([Infinity, 1, Infinity])", "Infinity")]
    [InlineData("Math.sumPrecise(new Set([1, 2, 3]))", "6")]
    [InlineData("Math.sumPrecise((function* () { yield 0.5; yield 0.25; })())", "0.75")]
    public void Math_sumPrecise_sums_exactly(string expression, string expected)
        => Assert.Equal(expected, Eval("String(" + expression + ")", SumPrecise));
}
