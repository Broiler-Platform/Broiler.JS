using System;
using System.Numerics;
using Broiler.JavaScript.BuiltIns.BigInt;
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
        if (index >= length)
            return false;

        var longValue = value is JSBigInt bigint
            ? (long)bigint.value
            : value.BigIntValue;

        System.Array.Copy(BitConverter.GetBytes(longValue), 0, buffer.buffer, byteOffset + index * 8, 8);
        return true;
    }

    [JSExport(Length = 1)]
    public static JSValue From(in Arguments a) => new JSBigInt64Array(TypedArrayParameters.From(in a, BYTES_PER_ELENENT));

    [JSExport]
    public static JSValue Of(in Arguments a)
    {
        var result = new JSBigInt64Array(TypedArrayParameters.Of(in a, BYTES_PER_ELENENT));
        for (int i = 0; i < a.Length; i++)
            result[(uint)i] = a[i];

        return result;
    }
}
