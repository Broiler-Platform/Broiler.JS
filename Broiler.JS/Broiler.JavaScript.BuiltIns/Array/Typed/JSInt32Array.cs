using Broiler.JavaScript.ExpressionCompiler;
using System;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.BuiltIns.Array.Typed;

// Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=8; Fingerprint=TBF
// Broiler-Falsified-If: an element read or write at an index below length reaches bytes before byteOffset or at or past byteOffset + length * 4 in the backing buffer
// Broiler-Human:        PENDING
[JSClassGenerator("Int32Array"), JSBaseClass("TypedArray")]
public partial class JSInt32Array : JSTypedArray
{
    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=0; Fingerprint=TBF
    // Broiler-Falsified-If: the value differs from the 4-byte stride GetValue and SetValue use, so an index just below length addresses bytes past the end of the view
    // Broiler-Human:        PENDING
    [JSExport("BYTES_PER_ELEMENT")]
    internal static readonly int BYTES_PER_ELENENT = 4;

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=8; Fingerprint=TBF
    // Broiler-Falsified-If: new Int32Array(0x40000001) returns a zero-length view instead of throwing a RangeError, because the element count times 4 wraps in int arithmetic
    // Broiler-Human:        PENDING
    [JSExport(Length = 3)]
    public JSInt32Array(in Arguments a) : base(new TypedArrayParameters(a, BYTES_PER_ELENENT)) { }

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=8; Fingerprint=TBF
    // Broiler-Falsified-If: a TypedArrayParameters whose bytesPerElement is not 4 builds a view whose length disagrees with the 4-byte stride GetValue and SetValue use
    // Broiler-Human:        PENDING
    private JSInt32Array(TypedArrayParameters a) : base(a) { }

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=1; Fingerprint=TBF
    // Broiler-Falsified-If: after a resizable buffer shrinks so that byteOffset + (index + 1) * 4 exceeds its byte length, reading that index throws instead of returning undefined
    // Broiler-Human:        PENDING
    public override JSValue GetValue(uint index, JSValue receiver, bool throwError = true)
    {
        if (index < 0 || index >= length)
            return JSUndefined.Value;
        return new JSNumber(BitConverter.ToInt32(buffer.buffer.AsSpan(byteOffset + (int)index * 4, 4)));
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=3; Fingerprint=TBF
    // Broiler-Falsified-If: an indexed write into a Int32Array whose buffer came from ArrayBuffer.prototype.transferToImmutable changes the stored bytes
    // Broiler-Human:        PENDING
    public override bool SetValue(uint index, JSValue value, JSValue receiver, bool throwError = true)
    {
        if (TrySetForeignReceiver(index, value, receiver, throwError, out var foreign))
            return foreign;

        var intValue = (value ?? JSUndefined.Value).IntValue;
        if (index >= length)
            return true; // out-of-bounds element write is a successful no-op (spec [[Set]] returns true)
        BitConverter.TryWriteBytes(buffer.buffer.AsSpan(byteOffset + (int)index * 4, 4), intValue);
        return true;
    }


}
