using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Compiler.Tests;

// `u[i]` on a null or undefined base must throw, whatever `i` is.
//
// It did not. An index the compiler could hold unboxed — a numeric local, a loop counter, a
// constant-folded expression — reached JSValue.GetElementByNumber, whose fast arm converts an
// integral in-range double to a uint and calls GetValue(uint, ...) directly. JSUndefined and
// JSNull override the this[uint] INDEXER to throw but not that virtual, whose base implementation
// answers `undefined` for a value with no prototype chain. So the read produced `undefined`
// instead of a TypeError — and the script carried on with a wrong value rather than stopping.
//
// Which shapes did and did not throw is the whole of the defect, so the cases are enumerated:
// a literal index (uint key, indexer) threw, a string key threw, a non-integral or out-of-range
// index (boxed key) threw, and only the unboxed-integral arm did not.
public class IndexedNullishReadTests
{
    private static string Run(string source)
    {
        using var context = new JSContext();
        return context.Eval(
            $"String((function(){{ try {{ {source} }} catch (e) {{ return 'threw: ' + e.message; }} return 'no throw'; }})())",
            "t.js").ToString();
    }

    // The arm that regressed: an integral, in-range index held in a variable.
    [Theory]
    [InlineData("var u; var k = 1; return u[k];", "undefined", "1")]
    [InlineData("var u; var k = 0; return u[k];", "undefined", "0")]
    [InlineData("var u; var k = 360; return u[k];", "undefined", "360")]
    [InlineData("var u; var k = 1 + 0; return u[k];", "undefined", "1")]
    [InlineData("var u; var k = 1.0; return u[k];", "undefined", "1")]
    [InlineData("var u; var k = 4294967294; return u[k];", "undefined", "4294967294")]
    [InlineData("var n = null; var k = 1; return n[k];", "null", "1")]
    [InlineData("var n = null; var k = 360; return n[k];", "null", "360")]
    public void AnUnboxedIntegralIndex_Throws(string source, string baseValue, string key)
    {
        Assert.Equal($"threw: Cannot read properties of {baseValue} (reading '{key}')", Run(source));
    }

    // A member expression is the shape minified code actually writes — `o.K[q]`, where the base is
    // itself a read that came back undefined. It is the same arm; it is called out because it is
    // the one that hid the defect in the wild.
    [Fact(Timeout = 600000)]
    public void AnUndefinedMemberBase_Throws()
    {
        Assert.Equal(
            "threw: Cannot read properties of undefined (reading '360')",
            Run("var o = {}; var q = 360; return o.K[q];"));
    }

    // A loop counter is unboxed for the same reason, and reads one element per iteration — the
    // shape most likely to turn one silent `undefined` into a whole loop of them.
    [Fact(Timeout = 600000)]
    public void ALoopCounterIndex_Throws()
    {
        Assert.Equal(
            "threw: Cannot read properties of undefined (reading '0')",
            Run("var u; var r; for (var i = 0; i < 4; i++) { r = u[i]; } return r;"));
    }

    // The shapes that already threw, kept so a future fast path cannot quietly drop one of them.
    [Theory]
    [InlineData("var u; return u[1];", "undefined", "1")]
    [InlineData("var u; return u[0];", "undefined", "0")]
    [InlineData("var u; var k = -1; return u[k];", "undefined", "-1")]
    [InlineData("var u; var k = 1.5; return u[k];", "undefined", "1.5")]
    [InlineData("var u; var k = 4294967295; return u[k];", "undefined", "4294967295")]
    [InlineData("var u; var k = '3'; return u[k];", "undefined", "3")]
    [InlineData("var n = null; var k = -1; return n[k];", "null", "-1")]
    public void TheOtherIndexShapes_StillThrow(string source, string baseValue, string key)
    {
        var message = Run(source);

        // A constant index lowers to the this[uint] indexer, which has its own (older) wording.
        // Either is a throw naming the same base and key; the point of this test is that neither
        // shape silently answers `undefined`.
        Assert.True(
            message == $"threw: Cannot read properties of {baseValue} (reading '{key}')" ||
            message == $"threw: Cannot get property {key} of {baseValue}",
            $"expected a TypeError naming {baseValue}/{key}, got: {message}");
    }

    // The write twin already threw (SetElementByNumber checks IsNullOrUndefined in
    // ThrowOnFailedElementAssignment); asserted so the read fix is not read as having moved it.
    // It names the key too, as a browser does — see UndefinedPropertyReadMessageTests.
    [Fact(Timeout = 600000)]
    public void AnIndexedWriteToANullishBase_StillThrows()
    {
        Assert.Equal("threw: Cannot set properties of undefined (setting '360')", Run("var u; var k = 360; u[k] = 1; return 'ok';"));
    }

    // Optional chaining is the one place the read must NOT throw, and it short-circuits before
    // GetElementByNumber is reached. The fix must not have made `u?.[k]` throw.
    [Fact(Timeout = 600000)]
    public void OptionalChaining_StillShortCircuits()
    {
        Assert.Equal("undefined", Run("var u; var k = 360; return String(u?.[k]);"));
        Assert.Equal("undefined", Run("var n = null; var k = 0; return String(n?.[k]);"));
    }

    // A real base still reads its element, through the same arm.
    [Fact(Timeout = 600000)]
    public void AnIndexedReadOnARealBase_IsUnchanged()
    {
        Assert.Equal("30", Run("var a = [10, 20, 30]; var k = 2; return String(a[k]);"));
        Assert.Equal("undefined", Run("var a = [10]; var k = 5; return String(a[k]);"));
        Assert.Equal("1", Run("var o = { '0': 1 }; var k = 0; return String(o[k]);"));
    }
}
