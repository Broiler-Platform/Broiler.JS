using Broiler.JavaScript.ExpressionCompiler;
using System;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.Runtime;
using Broiler.ECMAScript.Numbers;

namespace Broiler.JavaScript.BuiltIns.Array.Typed;

[JSClassGenerator("Float16Array"), JSBaseClass("TypedArray")]
public partial class JSFloat16Array : JSTypedArray
{
    [JSExport("BYTES_PER_ELEMENT")]
    internal static readonly int BYTES_PER_ELENENT = 2;

    [JSExport(Length = 3)]
    public JSFloat16Array(in Arguments a) : base(new TypedArrayParameters(a, BYTES_PER_ELENENT)) { }

    private JSFloat16Array(TypedArrayParameters a) : base(a) { }

    public override JSValue GetValue(uint index, JSValue receiver, bool throwError = true)
    {
        if (index < 0 || index >= length)
            return JSUndefined.Value;
        // The decoding is the shared library's (SJL-4.5).
        return new JSNumber(Float16.Decode(BitConverter.ToUInt16(buffer.buffer.AsSpan(byteOffset + (int)index * 2, 2))));
    }

    public override bool SetValue(uint index, JSValue value, JSValue receiver, bool throwError = true)
    {
        if (TrySetForeignReceiver(index, value, receiver, throwError, out var foreign))
            return foreign;

        // The encoding is the shared library's; a NaN keeps its sign and high payload bits, this
        // engine's choice of the implementation-defined NaN encoding (SJL-4.5).
        var bits = Float16.Encode((value ?? JSUndefined.Value).DoubleValue, Float16NanPolicy.PreservePayload);
        if (index >= length)
            return true; // out-of-bounds element write is a successful no-op (spec [[Set]] returns true)
        BitConverter.TryWriteBytes(buffer.buffer.AsSpan(byteOffset + (int)index * 2, 2), bits);
        return true;
    }


}
