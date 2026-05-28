using Broiler.JavaScript.ExpressionCompiler;
using System;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.BuiltIns.Array.Typed;

[JSClassGenerator("Int32Array"), JSBaseClass("TypedArray")]
public partial class JSInt32Array : JSTypedArray
{
    [JSExport("BYTES_PER_ELEMENT")]
    internal static readonly int BYTES_PER_ELENENT = 4;

    [JSExport(Length = 3)]
    public JSInt32Array(in Arguments a) : base(new TypedArrayParameters(a, BYTES_PER_ELENENT)) { }

    private JSInt32Array(TypedArrayParameters a) : base(a) { }

    public override JSValue GetValue(uint index, JSValue receiver, bool throwError = true)
    {
        if (index < 0 || index >= length)
            return JSUndefined.Value;
        return new JSNumber(BitConverter.ToInt32(buffer.buffer, byteOffset + (int)index * 4));
    }

    public override bool SetValue(uint index, JSValue value, JSValue receiver, bool throwError = true)
    {
        var intValue = (value ?? JSUndefined.Value).IntValue;
        if (index >= length)
            return false;
        System.Array.Copy(BitConverter.GetBytes(intValue), 0, buffer.buffer, byteOffset + index * 4, 4);
        return true;
    }

    [JSExport(Length = 1)]
    public static JSValue From(in Arguments a)
    {
        var temp = new JSInt32Array(TypedArrayParameters.From(in a, BYTES_PER_ELENENT));
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
