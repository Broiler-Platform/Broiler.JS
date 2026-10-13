using Broiler.JavaScript.ExpressionCompiler;
using System;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.Runtime;
using Broiler.ECMAScript.Numbers;

namespace Broiler.JavaScript.BuiltIns.Array.Typed;

// Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=7; Fingerprint=TBF
// Broiler-Falsified-If: an element read or write reaches a byte outside the view's live byteOffset plus length window after its ArrayBuffer is resized smaller or detached
// Broiler-Human:        PENDING
[JSClassGenerator("Uint8ClampedArray"), JSBaseClass("TypedArray")]
public partial class JSUint8ClampedArray : JSTypedArray
{
    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=0; Fingerprint=TBF
    // Broiler-Falsified-If: Uint8ClampedArray.BYTES_PER_ELEMENT reads as anything but 1, so the base constructor's element-size arithmetic no longer matches the one-byte-per-index access in GetValue and SetValue
    // Broiler-Human:        PENDING
    [JSExport("BYTES_PER_ELEMENT")]
    internal static readonly int BYTES_PER_ELENENT = 1;

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=7; Fingerprint=TBF
    // Broiler-Falsified-If: Uint8ClampedArray called as a plain function, without new, returns a view instead of throwing a TypeError
    // Broiler-Human:        PENDING
    [JSExport(Length = 3)]
    public JSUint8ClampedArray(in Arguments a) : base(new TypedArrayParameters(a, BYTES_PER_ELENENT)) { }

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=7; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private JSUint8ClampedArray(TypedArrayParameters a) : base(a) { }

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=1; Fingerprint=TBF
    // Broiler-Falsified-If: reading an index at or beyond the live length after the backing resizable ArrayBuffer shrinks throws a host IndexOutOfRangeException instead of returning undefined
    // Broiler-Human:        PENDING
    public override JSValue GetValue(uint index, JSValue receiver, bool throwError = true)
    {
        if (index < 0 || index >= length)
            return JSUndefined.Value;
        return new JSNumber(buffer.buffer[byteOffset + index]);
    }

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=1; Fingerprint=TBF
    // Broiler-Falsified-If: a value whose valueOf shrinks the backing resizable ArrayBuffer below the target index makes the store throw a host IndexOutOfRangeException instead of being a silent no-op
    // Broiler-Human:        PENDING
    public override bool SetValue(uint index, JSValue value, JSValue receiver, bool throwError = true)
    {
        if (TrySetForeignReceiver(index, value, receiver, throwError, out var foreign))
            return foreign;

        double number = (value ?? JSUndefined.Value).DoubleValue;
        if (index >= length)
            return true; // out-of-bounds element write is a successful no-op (spec [[Set]] returns true)
        // ToUint8Clamp is the shared library's (SJL-4.5).
        buffer.buffer[byteOffset + index] = IntegerConversions.ToUint8Clamp(number);
        return true;
    }


}
