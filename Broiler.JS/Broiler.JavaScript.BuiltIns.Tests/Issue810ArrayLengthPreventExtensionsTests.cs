using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Assigning to the length of a non-extensible (but not frozen) array must succeed: preventExtensions
// keeps length writable, so ArraySetLength does not throw. A frozen array still rejects the write.
// Issue #810 problem 86.
public class Issue810ArrayLengthPreventExtensionsTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    [Fact]
    public void EmptyArray_NonStrict_AssignLength_NoThrow()
        => Assert.Equal("0", Eval("""
            var a = Object.preventExtensions([]);
            a.length = 0;
            String(a.length);
        """));

    [Fact]
    public void EmptyArray_Strict_AssignLength_NoThrow()
        => Assert.Equal("0", Eval("""
            "use strict";
            var a = Object.preventExtensions([]);
            a.length = 0;
            String(a.length);
        """));

    [Fact]
    public void NonEmptyArray_Strict_TruncateLength_NoThrow()
        => Assert.Equal("0", Eval("""
            "use strict";
            var a = Object.preventExtensions([1, 2, 3]);
            a.length = 0;
            String(a.length);
        """));

    [Fact]
    public void FrozenArray_Strict_AssignLength_StillThrows()
        => Assert.Equal("TypeError", Eval("""
            "use strict";
            var a = Object.freeze([1, 2, 3]);
            try { a.length = 0; "no throw"; } catch (e) { e.constructor.name; }
        """));
}
