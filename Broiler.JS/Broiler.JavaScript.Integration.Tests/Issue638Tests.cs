using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/638
// Covers the cleanly-reproducible subset:
//   Problem 7      - %TypedArray%.prototype.sort on BigInt element arrays.
//   Problem 9/10   - Reflect.construct with a newTarget whose `prototype` is not
//                    an object (constructorness is fixed at creation).
public class Issue638Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // ---- Problem 9/10: newTarget with a non-object `prototype` ----

    // A function remains a constructor after its `prototype` property is set to a
    // non-object; Reflect.construct must accept it as newTarget and fall back to
    // the target's default prototype for the new instance.
    [Theory]
    [InlineData("var nt=function(){}; nt.prototype=null; var o=Reflect.construct(ArrayBuffer,[8],nt); '' + (Object.getPrototypeOf(o)===ArrayBuffer.prototype)", "true")]
    [InlineData("var nt=function(){}; nt.prototype=1; var ab=new ArrayBuffer(8); var o=Reflect.construct(DataView,[ab],nt); '' + (Object.getPrototypeOf(o)===DataView.prototype)", "true")]
    [InlineData("var nt=function(){}; nt.prototype=null; var o=Reflect.construct(Float64Array,[4],nt); '' + (Object.getPrototypeOf(o)===Float64Array.prototype)", "true")]
    [InlineData("var nt=function(){}; nt.prototype='x'; var o=Reflect.construct(Date,[0],nt); '' + (Object.getPrototypeOf(o)===Date.prototype)", "true")]
    public void ReflectConstructNewTargetWithNonObjectPrototype(string code, string expected)
        => Assert.Equal(expected, Eval(code));

    // A genuine non-constructor (arrow function) is still rejected as newTarget.
    [Fact]
    public void ReflectConstructRejectsArrowNewTarget()
        => Assert.Equal("TypeError", Eval("(function(){ try { Reflect.construct(ArrayBuffer,[8],()=>{}); return 'no throw'; } catch (e) { return e.constructor.name; } })()"));

    // ---- Problem 7: %TypedArray%.prototype.sort on BigInt arrays ----

    // The default comparator orders BigInt elements as BigInt (they have no Number
    // value), rather than throwing while coercing to a double.
    [Fact]
    public void BigIntTypedArraySortDefault()
        => Assert.Equal("1,2,3", Eval("var ta=new BigInt64Array([3n,1n,2n]); ta.sort(); ta.join(',')"));

    // sort mutates in place and returns the same instance.
    [Fact]
    public void TypedArraySortReturnsSameInstance()
        => Assert.Equal("true", Eval("var ta=new BigInt64Array([3n,1n,2n]); '' + (ta.sort()===ta)"));

    // A user comparefn is honoured (consulted by sign) for BigInt arrays.
    [Fact]
    public void BigIntTypedArraySortWithComparefn()
        => Assert.Equal("3,2,1", Eval("var ta=new BigInt64Array([3n,1n,2n]); ta.sort((a,b)=>a<b?1:a>b?-1:0); ta.join(',')"));

    // Numeric sort still applies the spec NaN-last / -0-before-+0 ordering.
    [Fact]
    public void NumericTypedArraySortOrdering()
        => Assert.Equal("0,1,2,3,NaN", Eval("var ta=new Float64Array([3,1,2,NaN,-0]); ta.sort(); ta.join(',')"));

    // toSorted returns a new same-type typed array and leaves the receiver intact.
    [Fact]
    public void BigIntTypedArrayToSorted()
        => Assert.Equal("1,2,3|3,1,2|true", Eval("var ta=new BigInt64Array([3n,1n,2n]); var s=ta.toSorted(); s.join(',')+'|'+ta.join(',')+'|'+(s instanceof BigInt64Array)"));
}
