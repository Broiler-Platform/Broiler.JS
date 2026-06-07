using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/699
//
// Fixed here:
//
// Problem 8 — Object.prototype.toString did not recognise the [[ErrorData]]
//   internal slot, so `Object.prototype.toString.call(new TypeError)` returned
//   "[object Object]" instead of "[object Error]". The builtin tag is now computed
//   up front (Array / Error / Function / Object) and only a string-valued
//   @@toStringTag overrides it.
//
// Problem 9 — several operations dropped the sign of a zero result:
//   * Math.cbrt/asinh/atanh hand-rolled their formulas via Math.Pow/Math.Log, which
//     turn -0 into +0; they now use the sign-of-zero-correct .NET intrinsics.
//   * Math.expm1 returned +0 for an input of -0 (Math.Exp(-0)-1); ±0 (and NaN) now
//     pass through unchanged.
//   * Math.sumPrecise accumulated from +0 instead of the spec's -0, so an empty
//     list (or a list of only -0 values) produced +0.
//   * Map.prototype.getOrInsert / getOrInsertComputed did not apply
//     CanonicalizeKeyedCollectionKey, so a -0 key was surfaced to the callback and
//     stored as -0 instead of +0.
public class Issue699Tests
{
    private static JSValue Eval(string code)
    {
        using var ctx = new JSContext(experimentalFeatures: JavaScriptFeatureFlags.AllExperimentalEs2026);
        return ctx.Eval(code);
    }

    // ---- Problem 8: Error objects tag as "[object Error]" ----

    [Theory]
    [InlineData("Error")]
    [InlineData("TypeError")]
    [InlineData("RangeError")]
    [InlineData("ReferenceError")]
    [InlineData("SyntaxError")]
    [InlineData("EvalError")]
    [InlineData("URIError")]
    public void ErrorObjects_Tag_As_Error(string ctor)
    {
        Assert.Equal("[object Error]",
            Eval($"Object.prototype.toString.call(new {ctor})").ToString());
    }

    [Fact]
    public void Plain_Object_And_Array_Still_Tag_Correctly()
    {
        Assert.Equal("[object Object]", Eval("Object.prototype.toString.call({})").ToString());
        Assert.Equal("[object Array]", Eval("Object.prototype.toString.call([])").ToString());
        Assert.Equal("[object Function]", Eval("Object.prototype.toString.call(function(){})").ToString());
    }

    [Fact]
    public void ToStringTag_Still_Overrides_Builtin_Tag()
    {
        // A string @@toStringTag wins even over the Error builtin tag.
        Assert.Equal("[object Custom]",
            Eval("var e = new Error; e[Symbol.toStringTag] = 'Custom'; Object.prototype.toString.call(e)").ToString());
    }

    // ---- Problem 9: sign-of-zero preservation ----

    [Theory]
    [InlineData("Math.cbrt(-0)")]
    [InlineData("Math.asinh(-0)")]
    [InlineData("Math.atanh(-0)")]
    [InlineData("Math.expm1(-0)")]
    public void Math_Preserves_Negative_Zero(string expr)
    {
        Assert.Equal("true", Eval($"Object.is({expr}, -0)").ToString());
    }

    [Theory]
    [InlineData("Math.cbrt(+0)")]
    [InlineData("Math.asinh(+0)")]
    [InlineData("Math.atanh(+0)")]
    [InlineData("Math.expm1(+0)")]
    public void Math_Preserves_Positive_Zero(string expr)
    {
        Assert.Equal("true", Eval($"Object.is({expr}, +0)").ToString());
    }

    [Theory]
    [InlineData("Math.cbrt(-8)", "-2")]
    [InlineData("Math.cbrt(27)", "3")]
    [InlineData("Math.atanh(-1)", "-Infinity")]
    [InlineData("Math.atanh(1)", "Infinity")]
    [InlineData("Math.atanh(2)", "NaN")]
    [InlineData("Math.expm1(-Infinity)", "-1")]
    [InlineData("Math.cbrt(-Infinity)", "-Infinity")]
    public void Math_Exact_Landmarks(string expr, string expected)
    {
        Assert.Equal(expected, Eval($"String({expr})").ToString());
    }

    [Theory]
    [InlineData("Math.sumPrecise([])", "true")]        // -0
    [InlineData("Math.sumPrecise([-0])", "true")]      // -0
    [InlineData("Math.sumPrecise([-0, -0])", "true")]  // -0
    public void SumPrecise_All_Negative_Zero_Is_Negative_Zero(string expr, string expected)
    {
        Assert.Equal(expected, Eval($"Object.is({expr}, -0)").ToString());
    }

    [Fact]
    public void SumPrecise_Mixed_Zero_Is_Positive_Zero()
    {
        Assert.Equal("true", Eval("Object.is(Math.sumPrecise([-0, 0]), 0)").ToString());
        // A genuine cancellation of non-zero values is +0, not -0.
        Assert.Equal("true", Eval("Object.is(Math.sumPrecise([1, -1]), 0)").ToString());
    }

    [Fact]
    public void SumPrecise_Still_Sums()
    {
        Assert.Equal("6", Eval("String(Math.sumPrecise([1, 2, 3]))").ToString());
    }

    // ---- Problem 9: Map canonicalizes -0 key to +0 ----

    [Fact]
    public void GetOrInsertComputed_Passes_Canonical_Key_To_Callback()
    {
        Assert.Equal("true", Eval(
            "var k; new Map().getOrInsertComputed(-0, function(a){ k = a; }); Object.is(k, 0)").ToString());
    }

    [Fact]
    public void GetOrInsert_Stores_Canonical_Key()
    {
        Assert.Equal("true", Eval(
            "var m = new Map(); m.getOrInsert(-0, 1); Object.is([...m.keys()][0], 0)").ToString());
    }
}
