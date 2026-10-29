using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// The "change array by copy" methods (with, toSpliced, toReversed, toSorted) build their result with
// CreateDataPropertyOrThrow, so they define own data properties directly and never trigger an inherited
// accessor such as a setter installed on Array.prototype[0]. Issue #810 problems 90 & 91.
public class Issue810ArrayCopyMethodsBypassSettersTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    // Installs a throwing setter at Array.prototype[0] (restored afterwards), runs the expression, and
    // returns the result joined with commas — proving the inherited setter was not invoked.
    private static string WithPoisonedZero(string expr) => Eval($$"""
            Object.defineProperty(Array.prototype, 0, { configurable: true, set: function () { throw "bad"; } });
            var __r;
            try { __r = {{expr}}.join(","); }
            finally { delete Array.prototype[0]; }
            __r;
        """);

    [Fact]
    public void With_BypassesInheritedSetter()
        => Assert.Equal("3,2", WithPoisonedZero("[1, 2].with(0, 3)"));

    [Fact]
    public void ToSpliced_BypassesInheritedSetter()
        => Assert.Equal("9,1,2", WithPoisonedZero("[1, 2].toSpliced(0, 0, 9)"));

    [Fact]
    public void ToReversed_BypassesInheritedSetter()
        => Assert.Equal("2,1", WithPoisonedZero("[1, 2].toReversed()"));

    [Fact]
    public void ToSorted_BypassesInheritedSetter()
        => Assert.Equal("1,2", WithPoisonedZero("[2, 1].toSorted()"));
}
