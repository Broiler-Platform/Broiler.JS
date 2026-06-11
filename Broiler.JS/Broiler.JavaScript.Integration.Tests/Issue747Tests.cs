using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/747
//
// Fixed here:
//
//   Problem 16 (JSON.stringify of negative zero) — a finite number is now serialized
//   with the spec Number::toString (ToECMAString), so JSON.stringify(-0) is "0" (not
//   "-0") and large magnitudes use ECMAScript scientific notation ("1e+21").
//
//   Problem 22 (Date / Date.UTC argument coercion order) — each component argument is
//   coerced to Number exactly once via Arguments.Get7Double; the NaN/Infinity scan and
//   the integer reduction both read the cached doubles, so a valueOf side effect is
//   observed a single time per argument (previously twice).
//
//   Problem 25 (Array.prototype.indexOf near the integer limit) — indexOf iterates with
//   a long counter over the full LengthOfArrayLike range (up to 2^53-1); indices beyond
//   the 32-bit array-index range are probed via HasProperty/Get on their numeric-string
//   key instead of being lost to a uint truncation.
//
//   Problem 33 (Array.prototype.findLast / findLastIndex maximum index) — both use the
//   long length and visit indices beyond 2^32-1 through their numeric-string key, so the
//   first index visited for a length of 2^53-1 is 2^53-2 (was a uint-clamped value).
//
//   Problem 41 (object rest / spread over a string primitive) — CopyDataProperties boxes
//   a primitive source via ToObject, so `let { ...rest } = 'ab'` and `{ ...'ab' }` copy
//   the String wrapper's own enumerable index properties { 0:'a', 1:'b' }.
//
//   Problem 28 (parseInt of a bare "0x") — the leading "0" of a stripped "0x"/"0X" prefix
//   no longer counts as a parsed digit, so parseInt("0x") and parseInt("0x", 16) are NaN.
//
//   Problem 31 (eval/script completion of a declaration) — a VariableStatement and a
//   LexicalDeclaration complete with an empty value, so they no longer overwrite the
//   script/eval completion: eval("var x = 1") is undefined and eval("1; var x = 1") is 1.

public class Issue747Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // ---- Problem 16: JSON.stringify of negative zero ----

    [Fact]
    public void JsonStringifyNegativeZeroIsZero()
        => Assert.Equal("0", Eval("JSON.stringify(-0)"));

    [Fact]
    public void JsonStringifyNegativeZeroInArray()
        => Assert.Equal("[0,0,1.5,1e+21]", Eval("JSON.stringify([-0, 0, 1.5, 1e21])"));

    // ---- Problem 22: Date / Date.UTC coerce each argument exactly once ----

    [Fact]
    public void DateConstructorCoercesEachArgumentOnce()
        => Assert.Equal("ymodhmisms", Eval(
            "var log='';function mk(t){return {valueOf:function(){log+=t;return 1;}};}" +
            "new Date(mk('y'),mk('mo'),mk('d'),mk('h'),mk('mi'),mk('s'),mk('ms'));log"));

    [Fact]
    public void DateUtcCoercesEachArgumentOnce()
        => Assert.Equal("ymodhmisms", Eval(
            "var log='';function mk(t){return {valueOf:function(){log+=t;return 1;}};}" +
            "Date.UTC(mk('y'),mk('mo'),mk('d'),mk('h'),mk('mi'),mk('s'),mk('ms'));log"));

    // ---- Problem 25: indexOf across the integer limit ----

    [Fact]
    public void IndexOfFindsPropertyNearIntegerLimit()
        => Assert.Equal("9007199254740990", Eval(
            "var o={length:9007199254740991};o[9007199254740990]='x';" +
            "''+Array.prototype.indexOf.call(o,'x',9007199254740990)"));

    // ---- Problem 33: findLast / findLastIndex maximum index ----

    [Fact]
    public void FindLastIndexVisitsMaximumIndexFirst()
        => Assert.Equal("9007199254740990", Eval(
            "var seen;Array.prototype.findLastIndex.call({length:9007199254740991}," +
            "function(v,i){seen=i;return true;});''+seen"));

    [Fact]
    public void FindLastVisitsMaximumIndexFirst()
        => Assert.Equal("4294967294", Eval(
            "var seen;Array.prototype.findLast.call({length:4294967295}," +
            "function(v,i){seen=i;return true;});''+seen"));

    // ---- Problem 41: object rest / spread over a string primitive ----

    [Fact]
    public void ObjectRestOverStringPrimitiveCopiesIndices()
        => Assert.Equal("f,o", Eval("var {...x}='fo';x[0]+','+x[1]"));

    [Fact]
    public void ObjectSpreadOverStringPrimitiveCopiesIndices()
        => Assert.Equal("a,b", Eval("var o={...'ab'};o[0]+','+o[1]"));

    [Fact]
    public void ObjectSpreadOverNumberPrimitiveCopiesNothing()
        => Assert.Equal("0", Eval("''+Object.keys({...42}).length"));

    // ---- Problem 28: parseInt of a bare "0x" ----

    [Fact]
    public void ParseIntBare0xIsNaN()
        => Assert.Equal("NaN", Eval("''+parseInt('0x')"));

    [Fact]
    public void ParseIntBare0xWithRadix16IsNaN()
        => Assert.Equal("NaN", Eval("''+parseInt('0x',16)"));

    [Fact]
    public void ParseIntHexStillParses()
        => Assert.Equal("31", Eval("''+parseInt('0x1f')"));

    // ---- Problem 31: declaration completes empty ----

    [Fact]
    public void EvalOfBareVarDeclarationIsUndefined()
        => Assert.Equal("undefined", Eval("''+eval('var x = 1;')"));

    [Fact]
    public void EvalVarAfterExpressionKeepsExpressionValue()
        => Assert.Equal("1", Eval("''+eval('1; var x = 5;')"));

    [Fact]
    public void EvalLetAfterExpressionKeepsExpressionValue()
        => Assert.Equal("1", Eval("''+eval('1; let y = 5;')"));

    [Fact]
    public void EvalBareLetDeclarationIsUndefined()
        => Assert.Equal("undefined", Eval("''+eval('let y = 1;')"));
}
