using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// A quantifier bound above Int32.MaxValue (valid in ECMAScript up to 2^53-1, but rejected by .NET)
// is clamped during lexer validation as well as at runtime, so a literal like /a{2147483648}/ parses
// as a regex instead of being mis-classified as division. Issue #810 problem 41.
public class Issue810RegExpLargeQuantifierTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    [Theory]
    [InlineData(@"/a{2147483648}/.test('a')", "false")]                 // just over Int32.MaxValue
    [InlineData(@"/a{2147483647,2147483648}/.test('aaa')", "false")]    // huge upper bound
    [InlineData(@"/a{2147483648}/u.test('a')", "false")]               // u-mode
    [InlineData(@"/a{99999999999999999999}/.test('a')", "false")]      // overflows Int64 too
    public void OversizedQuantifier_ParsesAndRuns(string expr, string expected)
        => Assert.Equal(expected, Eval($"String({expr});"));

    [Theory]
    [InlineData(@"/a{2,3}/.test('aa')", "true")]
    [InlineData(@"/a{2,3}/.test('a')", "false")]
    [InlineData(@"/a{0}/.test('')", "true")]
    public void NormalQuantifiers_Unaffected(string expr, string expected)
        => Assert.Equal(expected, Eval($"String({expr});"));
}
