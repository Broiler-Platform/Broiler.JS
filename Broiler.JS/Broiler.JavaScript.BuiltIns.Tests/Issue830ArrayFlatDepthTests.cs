using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Issue #830 (problem 49): Array.prototype.flat's depth defaults to 1 and is coerced with
// ToIntegerOrInfinity only when supplied, so flat(undefined) flattens one level (like flat()),
// while other non-numeric depths coerce to 0 and do not throw. Mirrors test262
// Array/prototype/flat/non-numeric-depth-should-not-throw.
public class Issue830ArrayFlatDepthTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    [Theory]
    // An explicit undefined keeps the default depth of 1 (the reported failure).
    [InlineData("JSON.stringify([1, [2]].flat(undefined))", "[1,2]")]
    // Omitted argument also defaults to 1.
    [InlineData("JSON.stringify([1, [2]].flat())", "[1,2]")]
    // Other non-numeric depths coerce to 0 (ToIntegerOrInfinity) — no flattening, no throw.
    [InlineData("JSON.stringify([1, [2]].flat(null))", "[1,[2]]")]
    [InlineData("JSON.stringify([1, [2]].flat(NaN))", "[1,[2]]")]
    [InlineData("JSON.stringify([1, [2]].flat('x'))", "[1,[2]]")]
    [InlineData("JSON.stringify([1, [2]].flat({}))", "[1,[2]]")]
    // A numeric string / float still coerces normally.
    [InlineData("JSON.stringify([1, [2, [3]]].flat('2'))", "[1,2,3]")]
    [InlineData("JSON.stringify([1, [2, [3]]].flat(1.9))", "[1,2,[3]]")]
    // Negative depth behaves as 0.
    [InlineData("JSON.stringify([1, [2]].flat(-1))", "[1,[2]]")]
    // Infinity flattens fully.
    [InlineData("JSON.stringify([1, [2, [3, [4]]]].flat(Infinity))", "[1,2,3,4]")]
    public void FlatDepthCoercion(string expr, string expected)
        => Assert.Equal(expected, Eval(expr));
}
