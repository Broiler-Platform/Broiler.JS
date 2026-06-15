using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// TypedArray.prototype.set with a typed-array source of a different element type converts values
// instead of copying raw bytes (issue #798 problem 30).
public class TypedArraySetCrossTypeTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string expr)
    {
        using var ctx = new JSContext();
        return ctx.Eval(expr).ToString();
    }

    [Fact]
    public void Set_LargerSourceType_ConvertsValues_NoOverflow()
    {
        Load();
        // Float64 source (8 bytes/elt) into Int8 target (1 byte/elt): values are converted, and the
        // copy must not overflow the destination (previously threw a .NET ArgumentException).
        var result = Eval("""
            var target = new Int8Array(4);
            var source = new Float64Array([1.9, -2.9, 300, 4]);
            target.set(source);
            Array.from(target).join(',');
        """);
        // ToInt8: 1.9→1, -2.9→-2, 300→(300 mod 256)=44, 4→4.
        Assert.Equal("1,-2,44,4", result);
    }

    [Fact]
    public void Set_SmallerSourceType_ConvertsValues()
    {
        Load();
        var result = Eval("""
            var target = new Float64Array(3);
            var source = new Uint8Array([10, 20, 30]);
            target.set(source, 0);
            Array.from(target).join(',');
        """);
        Assert.Equal("10,20,30", result);
    }

    [Fact]
    public void Set_DifferentTypeSharedBuffer_NoCorruption()
    {
        Load();
        // Source and target share one buffer but have different types and overlap.
        var result = Eval("""
            var buf = new ArrayBuffer(16);
            var u32 = new Uint32Array(buf);     // 4 elements
            u32.set([1, 2, 3, 4]);
            var u16 = new Uint16Array(buf);     // 8 elements, same buffer
            u16.set(u32, 0);                    // convert 1,2,3,4 into the first 4 u16 slots
            Array.from(u16).join(',');
        """);
        // Source values are read before writing, so no corruption: slots 0-3 become 1,2,3,4 and
        // slots 4-7 still reflect the original bytes of u32[2]=3 and u32[3]=4 (low/high halves).
        Assert.Equal("1,2,3,4,3,0,4,0", result);
    }

    [Fact]
    public void Set_MixBigIntAndNumber_Throws()
    {
        Load();
        var result = Eval("""
            var threw = false;
            try { new Int32Array(2).set(new BigInt64Array([1n, 2n])); }
            catch (e) { threw = e instanceof TypeError; }
            threw;
        """);
        Assert.Equal("true", result);
    }

    [Fact]
    public void Set_SameType_StillWorks()
    {
        Load();
        var result = Eval("""
            var target = new Int16Array(4);
            target.set(new Int16Array([5, 6]), 1);
            Array.from(target).join(',');
        """);
        Assert.Equal("0,5,6,0", result);
    }
}
