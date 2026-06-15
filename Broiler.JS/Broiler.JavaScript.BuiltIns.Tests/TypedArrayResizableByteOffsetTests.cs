using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// %TypedArray%.prototype.byteOffset reports +0 when a fixed-length or length-tracking view over a
// resizable ArrayBuffer is left out of bounds by a shrink (or its buffer is detached). Issue #805
// problems 34, 40, 41. (byteLength / length already returned 0 in that state.)
public class TypedArrayResizableByteOffsetTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);
    private static string E(string e) { Load(); using var c = new JSContext(); return c.Eval(e).ToString(); }

    [Fact]
    public void ByteOffset_InBounds_ReportsRealOffset()
        => Assert.Equal("8", E("""
            var ab = new ArrayBuffer(16, { maxByteLength: 16 });
            new Int8Array(ab, 8).byteOffset + '';
        """));

    [Fact]
    public void ByteOffset_FixedLength_OutOfBoundsAfterShrink_IsZero()
        => Assert.Equal("0,0,0", E("""
            var ab = new ArrayBuffer(16, { maxByteLength: 16 });
            var ta = new Int8Array(ab, 8, 4); // fixed-length view starting at offset 8
            ab.resize(4);                      // now the view's start (8) is past the end
            [ta.byteOffset, ta.byteLength, ta.length].join(',');
        """));

    [Fact]
    public void ByteOffset_LengthTracking_OutOfBoundsAfterShrink_IsZero()
        => Assert.Equal("0,0,0", E("""
            var ab = new ArrayBuffer(16, { maxByteLength: 16 });
            var ta = new Float64Array(ab, 8); // length-tracking, starts at offset 8
            ab.resize(4);
            [ta.byteOffset, ta.byteLength, ta.length].join(',');
        """));

    [Fact]
    public void ByteOffset_RecoversAfterGrowBackInBounds()
        => Assert.Equal("8", E("""
            var ab = new ArrayBuffer(16, { maxByteLength: 16 });
            var ta = new Int8Array(ab, 8, 4);
            ab.resize(4);   // out of bounds -> byteOffset 0
            ab.resize(16);  // back in bounds -> byteOffset 8 again
            ta.byteOffset + '';
        """));

    [Fact]
    public void ByteOffset_DetachedBuffer_IsZero()
        => Assert.Equal("0", E("""
            var ab = new ArrayBuffer(16, { maxByteLength: 16 });
            var ta = new Int8Array(ab, 8, 4);
            ab.transfer();  // detaches ab
            ta.byteOffset + '';
        """));

    [Fact]
    public void ByteOffset_BigInt64_OutOfBounds_IsZero()
        => Assert.Equal("0", E("""
            var ab = new ArrayBuffer(32, { maxByteLength: 32 });
            var ta = new BigInt64Array(ab, 8); // length-tracking
            ab.resize(4);
            ta.byteOffset + '';
        """));

    // values() iterator exhausted, then the buffer is resized so the view is out of bounds: the
    // length/byteOffset read 0 and a further next() returns {done:true} without throwing (issue #805
    // problem 7 — make-out-of-bounds-after-exhausted.js).
    [Fact]
    public void ValuesIterator_AfterExhaustedThenOutOfBounds()
        => Assert.Equal("11,22,true,undefined,0,0,true,undefined", E("""
            var rab = new ArrayBuffer(3, { maxByteLength: 5 });
            var ta = new Int8Array(rab, 1);
            ta[0] = 11; ta[1] = 22;
            var it = ta.values(), out = [];
            out.push(it.next().value);                       // 11
            out.push(it.next().value);                       // 22
            var r = it.next();                               // exhausted
            out.push(r.done, String(r.value));               // true, "undefined"
            rab.resize(0);                                    // ta now out of bounds
            out.push(ta.length, ta.byteOffset);              // 0, 0
            r = it.next();                                    // must not throw
            out.push(r.done, String(r.value));               // true, "undefined"
            out.join(',');
        """));
}
