using System;
using Broiler.JavaScript.BuiltIns.Function;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.Engine.Extensions;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.BuiltIns.Array.Typed;

public readonly struct TypedArrayParameters
{
    public readonly JSArrayBuffer buffer;
    public readonly int length;
    public readonly int bytesPerElement;
    public readonly int byteOffset;
    public readonly JSValue copyFrom;
    public readonly JSValue map;
    public readonly JSValue thisArg;
    public readonly JSObject prototype;

    public static TypedArrayParameters From(in Arguments a, int bytesPerElements)
    {
        var (f, map, mapThis) = a.Get3();
        return new TypedArrayParameters(f, map, mapThis, bytesPerElements, GetConstructorPrototype(a.This));
    }

    public static TypedArrayParameters Of(in Arguments a, int bytesPerElements) => new(a.Length, bytesPerElements, GetConstructorPrototype(a.This));

    private TypedArrayParameters(int length, int bytesPerElements, JSObject prototype)
    {
        buffer = null;
        this.length = length;
        bytesPerElement = bytesPerElements;
        byteOffset = 0;
        copyFrom = null;
        map = null;
        thisArg = null;
        this.prototype = prototype;
    }

    private TypedArrayParameters(JSValue source, JSValue map, JSValue thisArg, int bytesPerElements, JSObject prototype)
    {
        buffer = null;
        length = -1;
        bytesPerElement = bytesPerElements;
        byteOffset = 0;
        copyFrom = source;
        this.map = map;
        this.thisArg = thisArg;
        this.prototype = prototype;
    }

    public TypedArrayParameters(byte[] data, int bytesPerElements)
    {
        buffer = new JSArrayBuffer(data);
        length = data.Length / bytesPerElements;
        bytesPerElement = bytesPerElements;
        byteOffset = 0;
        copyFrom = null;
        map = null;
        thisArg = null;
        prototype = JSEngine.NewTargetPrototype;
    }

    public TypedArrayParameters(
        in Arguments a, int bytesPerElements)
    {
        buffer = null;
        length = -1;
        bytesPerElement = bytesPerElements;
        byteOffset = 0;
        copyFrom = null;
        map = null;
        thisArg = null;
        prototype = JSEngine.NewTargetPrototype;
        if (a.Length == 0)
        {
            buffer = null;
            byteOffset = 0;
            length = 0;
            return;
        }
        var (a1, a2, a3) = a.Get3();
        if (a1 is JSArrayBuffer arrayBuffer)
        {
            buffer = arrayBuffer;
            byteOffset = JSTypedArray.ToIntegerOrInfinity(a2);
            length = a3.IsUndefined ? -1 : ToTypedArrayLength(a3);
            return;
        }

        if (!a1.IsObject)
        {
            buffer = null;
            byteOffset = 0;
            length = ToTypedArrayLength(a1);
            return;
        }
        copyFrom = a1;
    }

    private static JSObject GetConstructorPrototype(JSValue constructor)
    {
        if (constructor is not IJSFunction)
            throw JSEngine.NewTypeError("TypedArray constructor is not a constructor");

        if (constructor[KeyStrings.prototype] is JSObject prototype)
            return prototype;

        throw JSEngine.NewTypeError("TypedArray constructor is not a constructor");
    }

    private static int ToTypedArrayLength(JSValue value)
    {
        var numberLength = value.DoubleValue;
        if (double.IsNaN(numberLength) || numberLength == 0)
            return 0;

        if (double.IsInfinity(numberLength) || numberLength < 0 || numberLength > int.MaxValue)
            throw JSEngine.NewRangeError("Invalid typed array length");

        return (int)Math.Floor(numberLength);
    }
}