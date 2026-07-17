using Broiler.JavaScript.ExpressionCompiler;
using System;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.BuiltIns.Array.Typed;


[JSClassGenerator("Float64Array"), JSBaseClass("TypedArray")]
public partial class JSFloat64Array : JSTypedArray
{
    [JSExport("BYTES_PER_ELEMENT")]
    internal static readonly int BYTES_PER_ELENENT = 8;

    [JSExport(Length = 3)]
    public JSFloat64Array(in Arguments a) : base(new TypedArrayParameters(a, BYTES_PER_ELENENT)) { }

    private JSFloat64Array(TypedArrayParameters a) : base(a) { }

    public override JSValue GetValue(uint index, JSValue receiver, bool throwError = true)
    {
        if (index < 0 || index >= length)
            return JSUndefined.Value;
        return new JSNumber(BitConverter.ToDouble(buffer.buffer.AsSpan(byteOffset + (int)index * 8, 8)));
    }

    public override bool SetValue(uint index, JSValue value, JSValue receiver, bool throwError = true)
    {
        if (TrySetForeignReceiver(index, value, receiver, throwError, out var foreign))
            return foreign;

        var number = (value ?? JSUndefined.Value).DoubleValue;
        if (index >= length)
            return true; // out-of-bounds element write is a successful no-op (spec [[Set]] returns true)
        BitConverter.TryWriteBytes(buffer.buffer.AsSpan(byteOffset + (int)index * 8, 8), number);
        return true;
    }


}
