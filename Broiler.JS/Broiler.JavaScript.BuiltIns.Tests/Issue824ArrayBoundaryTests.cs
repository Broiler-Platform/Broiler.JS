using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Issue #824: Array methods with out-of-bounds / large indices.
//  - 91/92/100: copyWithin clamps Infinity/out-of-bounds target/start/end and defaults an
//    undefined end to the length.
//  - 77: Array.prototype.includes finds values at indices up to 2^53-1 (long index/length).
//  - 80/81: indexOf / lastIndexOf find sparse / >2^32 indexed properties.
public class Issue824ArrayBoundaryTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    [Theory]
    // 91 — out-of-bounds / Infinity target is a no-op.
    [InlineData("[0,1,2,3,4,5].copyWithin(6,0)", "0,1,2,3,4,5")]
    [InlineData("[0,1,2,3,4,5].copyWithin(7,0)", "0,1,2,3,4,5")]
    [InlineData("[0,1,2,3,4,5].copyWithin(Infinity,0)", "0,1,2,3,4,5")]
    // 92 — undefined / omitted end defaults to the length.
    [InlineData("[0,1,2,3].copyWithin(0,1,undefined)", "1,2,3,3")]
    [InlineData("[0,1,2,3].copyWithin(0,1)", "1,2,3,3")]
    // 100 — out-of-bounds / Infinity end clamps to the length.
    [InlineData("[0,1,2,3].copyWithin(0,1,6)", "1,2,3,3")]
    [InlineData("[0,1,2,3].copyWithin(0,1,Infinity)", "1,2,3,3")]
    // negative start/end still work.
    [InlineData("[0,1,2,3,4].copyWithin(0,-2)", "3,4,2,3,4")]
    public void CopyWithin(string expr, string expected)
        => Assert.Equal(expected, Eval($"{expr}.join(',')"));

    [Theory]
    // 77 — includes finds values at indices up to 2^53-1, ignores indexes ≥ 2^53-1.
    [InlineData("(function(){var o={'0':'a','9007199254740990':'c','9007199254740991':'d'};o.length=9007199254740991;return [].includes.call(o,'c',9007199254740990);})()", "true")]
    [InlineData("(function(){var o={'9007199254740991':'d'};o.length=9007199254740991;return [].includes.call(o,'d',9007199254740990);})()", "false")]
    [InlineData("(function(){var o={'0':'a'};o.length=-0;return [].includes.call(o,'a');})()", "false")]
    public void IncludesLengthBoundaries(string expr, string expected)
        => Assert.Equal(expected, Eval(expr));

    [Theory]
    // 80 — indexOf finds the 2^32-2 indexed property; non-array-index props (≥ 2^32-1) are excluded.
    // (fromIndex near the end keeps the search bounded, as in the test262 vector.)
    [InlineData("a.indexOf(2,4294967290)", "4294967294")]
    [InlineData("a.indexOf(3,4294967290)", "-1")]
    [InlineData("a.indexOf(4,4294967290)", "-1")]
    [InlineData("a.indexOf(5,4294967290)", "-1")]
    public void IndexOfLargeIndex(string expr, string expected)
        => Assert.Equal(expected, Eval(
            $"var a=new Array(0,1);a[4294967294]=2;a[4294967295]=3;a[4294967296]=4;a[4294967297]=5;{expr}"));

    [Fact]
    // 81 — lastIndexOf finds the 2^32-2 indexed property (matches on the first probed index).
    public void LastIndexOfLargeIndex()
        => Assert.Equal("4294967294", Eval(
            "var b=new Array(0,1);b[4294967294]=2;b[4294967295]=3;b.lastIndexOf(2)"));

    [Theory]
    // Ordinary dense-array behaviour is unchanged.
    [InlineData("[1,2,3,2,1].indexOf(2)", "1")]
    [InlineData("[1,2,3,2,1].lastIndexOf(2)", "3")]
    [InlineData("[1,2,3].includes(2)", "true")]
    [InlineData("[1,2,3].indexOf(9)", "-1")]
    [InlineData("[1,,3].indexOf(undefined)", "-1")]
    [InlineData("[1,,3].includes(undefined)", "true")]
    public void DenseArrayUnchanged(string expr, string expected)
        => Assert.Equal(expected, Eval($"String({expr})"));
}
