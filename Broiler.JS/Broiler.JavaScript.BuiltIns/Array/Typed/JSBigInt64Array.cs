using System;
using System.Numerics;
using Broiler.JavaScript.BuiltIns.BigInt;
using Broiler.JavaScript.BuiltIns.Boolean;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.BuiltIns.Array.Typed;

[JSClassGenerator("BigInt64Array"), JSBaseClass("TypedArray")]
public partial class JSBigInt64Array : JSTypedArray
{
    [JSExport("BYTES_PER_ELEMENT")]
    internal static readonly int BYTES_PER_ELENENT = 8;

    [JSExport(Length = 3)]
    public JSBigInt64Array(in Arguments a) : base(new TypedArrayParameters(a, BYTES_PER_ELENENT)) { }

    private JSBigInt64Array(TypedArrayParameters a) : base(a) { }

    public override JSValue GetValue(uint index, JSValue receiver, bool throwError = true)
    {
        if (index >= length)
            return JSUndefined.Value;

        return new JSBigInt(new BigInteger(BitConverter.ToInt64(buffer.buffer, byteOffset + (int)index * 8)));
    }

    public override bool SetValue(uint index, JSValue value, JSValue receiver, bool throwError = true)
    {
        var longValue = (long)ToBigIntValue(value ?? JSUndefined.Value).value;
        if (index >= length)
            return false;

        System.Array.Copy(BitConverter.GetBytes(longValue), 0, buffer.buffer, byteOffset + index * 8, 8);
        return true;
    }

    [JSExport(Length = 1)]
    public static JSValue From(in Arguments a)
    {
        var temp = new JSBigInt64Array(TypedArrayParameters.From(in a, BYTES_PER_ELENENT));
        var result = CreateTypedArrayFromConstructor(a.This, temp.Length);
        for (uint i = 0; i < temp.Length; i++)
            result[i] = temp[i];

        return result;
    }

    [JSExport]
    public static JSValue Of(in Arguments a)
    {
        var result = CreateTypedArrayFromConstructor(a.This, a.Length);
        for (int i = 0; i < a.Length; i++)
            result[(uint)i] = a[i];

        return result;
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
