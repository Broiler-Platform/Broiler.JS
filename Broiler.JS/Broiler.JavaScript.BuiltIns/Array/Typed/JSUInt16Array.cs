using Broiler.JavaScript.ExpressionCompiler;
using System;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.Runtime;
using Broiler.ECMAScript.Numbers;

namespace Broiler.JavaScript.BuiltIns.Array.Typed;

[JSClassGenerator("Uint16Array"), JSBaseClass("TypedArray")]
public partial class JSUInt16Array : JSTypedArray
{
    [JSExport("BYTES_PER_ELEMENT")]
    internal static readonly int BYTES_PER_ELENENT = 2;

    [JSExport(Length = 3)]
    public JSUInt16Array(in Arguments a) : base(new TypedArrayParameters(a, BYTES_PER_ELENENT)) { }

    private JSUInt16Array(TypedArrayParameters a) : base(a) { }

    public override JSValue GetValue(uint index, JSValue receiver, bool throwError = true)
    {
        if (index < 0 || index >= length)
            return JSUndefined.Value;
        return new JSNumber(BitConverter.ToUInt16(buffer.buffer.AsSpan(byteOffset + (int)index * 2, 2)));
    }

    public override bool SetValue(uint index, JSValue value, JSValue receiver, bool throwError = true)
    {
        if (TrySetForeignReceiver(index, value, receiver, throwError, out var foreign))
            return foreign;

        var number = (value ?? JSUndefined.Value).DoubleValue;
        if (index >= length)
            return true; // out-of-bounds element write is a successful no-op (spec [[Set]] returns true)
        // ToUint16 is the shared library's (SJL-4.5).
        BitConverter.TryWriteBytes(buffer.buffer.AsSpan(byteOffset + (int)index * 2, 2), IntegerConversions.ToUint16(number));
        return true;
    }


}
