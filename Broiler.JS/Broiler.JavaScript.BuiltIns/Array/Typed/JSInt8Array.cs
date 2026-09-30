using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.BuiltIns.Array.Typed;

// Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=8; Fingerprint=TBF
// Broiler-Falsified-If: an element read or write at an index below length reaches bytes before byteOffset or at or past byteOffset + length in the backing buffer
// Broiler-Human:        PENDING
[JSClassGenerator("Int8Array"), JSBaseClass("TypedArray")]
public partial class JSInt8Array : JSTypedArray
{
    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=0; Fingerprint=TBF
    // Broiler-Falsified-If: the value is not 1 while GetValue and SetValue address one byte per index, so length counts a different number of elements than the view spans
    // Broiler-Human:        PENDING
    [JSExport("BYTES_PER_ELEMENT")]
    internal static readonly int BYTES_PER_ELENENT = 1;

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=8; Fingerprint=TBF
    // Broiler-Falsified-If: new Int8Array(0x7FFFFFFF) fails with a plain Error carrying a .NET OutOfMemoryException message instead of a RangeError
    // Broiler-Human:        PENDING
    [JSExport(Length = 3)]
    public JSInt8Array(in Arguments a) : base(new TypedArrayParameters(a, BYTES_PER_ELENENT)) { }

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=8; Fingerprint=TBF
    // Broiler-Falsified-If: a TypedArrayParameters whose bytesPerElement is not 1 builds a view whose length disagrees with the one-byte stride GetValue and SetValue use
    // Broiler-Human:        PENDING
    private JSInt8Array(TypedArrayParameters a) : base(a) { }

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=1; Fingerprint=TBF
    // Broiler-Falsified-If: after a resizable buffer shrinks so that byteOffset + index + 1 exceeds its byte length, reading that index throws instead of returning undefined
    // Broiler-Human:        PENDING
    public override JSValue GetValue(uint index, JSValue receiver, bool throwError = true)
    {
        if (index < 0 || index >= length)
            return JSUndefined.Value;
        return new JSNumber((sbyte)buffer.buffer[byteOffset + index]);
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=3; Fingerprint=TBF
    // Broiler-Falsified-If: an indexed write into a Int8Array whose buffer came from ArrayBuffer.prototype.transferToImmutable changes the stored bytes
    // Broiler-Human:        PENDING
    public override bool SetValue(uint index, JSValue value, JSValue receiver, bool throwError = true)
    {
        if (TrySetForeignReceiver(index, value, receiver, throwError, out var foreign))
            return foreign;

        var intValue = (value ?? JSUndefined.Value).IntValue;
        if (index >= length)
            return true; // out-of-bounds element write is a successful no-op (spec [[Set]] returns true)
        buffer.buffer[byteOffset + index] = (byte)intValue;
        return true;
    }


}
