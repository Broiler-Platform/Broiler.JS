using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/630
public class Issue630Tests
{
    private static JSValue Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code);
    }

    // Problems 4 & 5: a computed property name in an object binding pattern used to be
    // mis-parsed as a nested array pattern, so `[expr]` keys (the test262
    // `*-obj-ptrn-prop-eval-err` destructuring templates) failed to parse with
    // "Unexpected token BracketStart: (".
    [Theory]
    [InlineData("var k=()=>'a'; var { [k()]: x } = { a: 1 }; x;", "1")]            // call key
    [InlineData("var key='a'; var { [key]: x } = { a: 2 }; x;", "2")]             // identifier key
    [InlineData("var o={k:'a'}; var { [o.k]: x } = { a: 3 }; x;", "3")]           // member key
    [InlineData("var { ['a'+'b']: x } = { ab: 4 }; x;", "4")]                     // expression key
    [InlineData("var { ['a']: x = 7 } = {}; x;", "7")]                            // default value
    public void ComputedKeyInObjectBindingPatternBinds(string code, string expected)
    {
        Assert.Equal(expected, Eval(code).ToString());
    }

    [Fact]
    public void ComputedKeyInMethodParameterDestructures()
    {
        // Object-literal method (Problem 4) and generator method (Problem 5).
        Assert.Equal("99", Eval("var o={ method({ ['a']: x }) { return x; } }; o.method({a:99});").ToString());
        Assert.Equal("8", Eval("class C { *gen({ ['a']: x }) { yield x; } } [...new C().gen({a:8})][0];").ToString());
    }

    [Fact]
    public void ComputedKeyIsEvaluatedOnceAndBeforeTargetRead()
    {
        // The key expression must be evaluated exactly once, before the value is read,
        // so its side effects/abrupt completions are observed in spec order.
        Assert.Equal("1", Eval("var n=0; function k(){n++; return 'a';} var {[k()]:x}={a:1}; n;").ToString());
        Assert.Equal("key,get", Eval(
            "var log=[]; function k(){log.push('key'); return 'a';}" +
            "var o={get a(){log.push('get'); return 1;}};" +
            "var {[k()]:x}=o; log.join(',');").ToString());
    }

    [Fact]
    public void ComputedKeyEvaluationErrorPropagates()
    {
        var code = "function thrower(){throw new Error('boom');} var t;" +
                   "try { var {[thrower()]:x}={}; } catch(e){ t=e.message; } t;";
        Assert.Equal("boom", Eval(code).ToString());
    }

    // Problem 3: `new DataView(buffer, byteOffset[, byteLength])` rejected a byteOffset
    // equal to the buffer length (a valid zero-length view) with
    // "Start offset is outside the bounds of the buffer", and never validated an
    // explicit byteLength against the buffer.
    [Theory]
    [InlineData("new DataView(new ArrayBuffer(8))", "8|0")]
    [InlineData("new DataView(new ArrayBuffer(8), 4)", "4|4")]
    [InlineData("new DataView(new ArrayBuffer(8), 4, 2)", "2|4")]
    [InlineData("new DataView(new ArrayBuffer(8), 8)", "0|8")]                 // offset == length
    [InlineData("new DataView(new ArrayBuffer(8), 4, undefined)", "4|4")]      // explicit undefined length
    [InlineData("new DataView(new ArrayBuffer(8), 4, 4)", "4|4")]             // exact fit
    public void DataViewConstructorOffsetAndLength(string ctor, string expected)
    {
        Assert.Equal(expected, Eval($"var dv = {ctor}; dv.byteLength + '|' + dv.byteOffset;").ToString());
    }

    [Theory]
    [InlineData("new DataView(new ArrayBuffer(8), 9)")]        // offset past end
    [InlineData("new DataView(new ArrayBuffer(8), 4, 5)")]     // offset + length past end
    public void DataViewConstructorOutOfBoundsThrowsRangeError(string ctor)
    {
        var code = $"var t; try{{ {ctor}; t='no throw'; }}catch(e){{ t=e.constructor.name; }} t;";
        Assert.Equal("RangeError", Eval(code).ToString());
    }
}
