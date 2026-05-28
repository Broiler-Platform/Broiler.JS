using Broiler.JavaScript.ExpressionCompiler;
using System;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.Runtime;

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
        var half = BitConverter.ToHalf(buffer.buffer, byteOffset + (int)index * 2);
        return new JSNumber((double)half);
    }

    public override bool SetValue(uint index, JSValue value, JSValue receiver, bool throwError = true)
    {
        var half = (Half)(value ?? JSUndefined.Value).DoubleValue;
        if (index >= length)
            return false;
        var bytes = BitConverter.GetBytes(half);
        System.Array.Copy(bytes, 0, buffer.buffer, byteOffset + index * 2, 2);
        return true;
    }

    [JSExport(Length = 1)]
    public static JSValue From(in Arguments a)
    {
        var temp = new JSFloat16Array(TypedArrayParameters.From(in a, BYTES_PER_ELENENT));
        var result = CreateTypedArrayFromConstructor(a.This, temp.Length);
        for (uint i = 0; i < temp.Length; i++)
            result[i] = temp[i];

        return result;
    }

    [JSExport]
    public static JSValue Of(in Arguments a)
    {
        var r = CreateTypedArrayFromConstructor(a.This, a.Length);
        for (int i = 0; i < a.Length; i++)
        {
            r[(uint)i] = a[i];
        }
        return r;
    }
}
