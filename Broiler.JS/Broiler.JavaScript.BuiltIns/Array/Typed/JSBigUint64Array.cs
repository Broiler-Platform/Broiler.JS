using System;
using System.Numerics;
using Broiler.JavaScript.BuiltIns.BigInt;
using Broiler.JavaScript.BuiltIns.Boolean;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.Runtime;
using Broiler.ECMAScript.Numbers;

namespace Broiler.JavaScript.BuiltIns.Array.Typed;

[JSClassGenerator("BigUint64Array"), JSBaseClass("TypedArray")]
public partial class JSBigUint64Array : JSTypedArray
{
    [JSExport("BYTES_PER_ELEMENT")]
    internal static readonly int BYTES_PER_ELENENT = 8;

    [JSExport(Length = 3)]
    public JSBigUint64Array(in Arguments a) : base(new TypedArrayParameters(a, BYTES_PER_ELENENT)) { }

    private JSBigUint64Array(TypedArrayParameters a) : base(a) { }

    public override JSValue GetValue(uint index, JSValue receiver, bool throwError = true)
    {
        if (index >= length)
            return JSUndefined.Value;

        return new JSBigInt(new BigInteger(BitConverter.ToUInt64(buffer.buffer.AsSpan(byteOffset + (int)index * 8, 8))));
    }

    public override bool SetValue(uint index, JSValue value, JSValue receiver, bool throwError = true)
    {
        if (TrySetForeignReceiver(index, value, receiver, throwError, out var foreign))
            return foreign;

        // ToBigInt, then ToBigUint64, the shared library's bit pattern (SJL-4.5).
        var bits = IntegerConversions.ToBigUint64(JSBigInt.Coerce(value ?? JSUndefined.Value).value);
        if (index >= length)
            return true; // out-of-bounds element write is a successful no-op (spec [[Set]] returns true)

        BitConverter.TryWriteBytes(buffer.buffer.AsSpan(byteOffset + (int)index * 8, 8), bits);
        return true;
    }


    internal override void ValidateElementValue(JSValue value) => _ = JSBigInt.Coerce(value);
}
