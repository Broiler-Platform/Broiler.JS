using System;
using System.Numerics;
using Broiler.JavaScript.BuiltIns.BigInt;
using Broiler.JavaScript.BuiltIns.Boolean;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.Runtime;

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

        return new JSBigInt(new BigInteger(BitConverter.ToUInt64(buffer.buffer, byteOffset + (int)index * 8)));
    }

    public override bool SetValue(uint index, JSValue value, JSValue receiver, bool throwError = true)
    {
        if (TrySetForeignReceiver(index, value, receiver, throwError, out var foreign))
            return foreign;

        var ulongValue = (ulong)ToBigIntValue(value ?? JSUndefined.Value).value;
        if (index >= length)
            return true; // out-of-bounds element write is a successful no-op (spec [[Set]] returns true)

        System.Array.Copy(BitConverter.GetBytes(ulongValue), 0, buffer.buffer, byteOffset + index * 8, 8);
        return true;
    }


    internal override void ValidateElementValue(JSValue value) => _ = ToBigIntValue(value);

    private static JSBigInt ToBigIntValue(JSValue value)
    {
        if (value is JSBigInt bigint)
            return bigint;

        if (value is JSBoolean boolean)
            return new JSBigInt(boolean.BooleanValue ? BigInteger.One : BigInteger.Zero);

        if (value.IsNullOrUndefined || value.IsNumber || value.IsSymbol)
            throw JSEngine.NewTypeError("Cannot convert value to BigInt");

        return (JSBigInt)JSBigInt.Constructor(new Arguments(JSUndefined.Value, value));
    }
}
