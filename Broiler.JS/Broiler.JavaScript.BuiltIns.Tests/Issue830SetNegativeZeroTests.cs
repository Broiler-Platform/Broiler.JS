using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Issue #830 (problems 46, 47, 48): Set.prototype.add canonicalizes -0 to +0 before
// storing, so iteration and the set-combination methods (intersection/union/
// symmetricDifference) yield +0, never -0. Mirrors test262
// Set/prototype/{intersection,union,symmetricDifference}/converts-negative-zero.
public class Issue830SetNegativeZeroTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    [Theory]
    // Plain add canonicalizes the stored value.
    [InlineData("Object.is([...new Set([-0])][0], 0)", "true")]
    [InlineData("Object.is([...new Set([-0])][0], -0)", "false")]
    // The combination methods copy the canonicalized entries.
    [InlineData("Object.is([...new Set([-0]).intersection(new Set([0]))][0], 0)", "true")]
    [InlineData("Object.is([...new Set([-0]).union(new Set([1]))][0], 0)", "true")]
    [InlineData("Object.is([...new Set([1]).symmetricDifference(new Set([-0]))][1], 0)", "true")]
    // -0 supplied through a set-like keys() iterator is also canonicalized in the result.
    [InlineData("""
        Object.is([...new Set([1]).union({ size: 1, has() { return false; },
          keys() { let d = false; return { next() { if (d) return { done: true }; d = true; return { value: -0, done: false }; } }; } })][1], 0)
        """, "true")]
    public void NegativeZeroCanonicalized(string expr, string expected)
        => Assert.Equal(expected, Eval($"String({expr})"));
}
