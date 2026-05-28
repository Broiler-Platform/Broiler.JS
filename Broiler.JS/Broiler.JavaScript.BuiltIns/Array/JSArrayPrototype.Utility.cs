using System;
using System.Text;
using System.Globalization;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine.Extensions;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Array;

public partial class JSArray
{
    [JSPrototypeMethod]
    [JSExport("concat", Length = 1)]
    public static JSValue Concat(in Arguments a)
    {
        var @this = ToArrayLikeObject(a.This);
        var result = CreateArraySpecies(@this, 0);
        uint resultIndex = 0;

        void Append(JSValue item)
        {
            if (IsArrayValue(item) && item is JSObject spreadable)
            {
                var length = GetArrayLikeLength(spreadable);
                for (uint sourceIndex = 0; sourceIndex < length; sourceIndex++)
                {
                    if (TryGetArrayLikeElement(spreadable, sourceIndex, out var value))
                        CreateDataPropertyOrThrow(result, resultIndex, value);

                    resultIndex++;
                }

                return;
            }

            CreateDataPropertyOrThrow(result, resultIndex++, item);
        }

        Append(a.This);
        for (int i = 0; i < a.Length; i++)
            Append(a.GetAt(i));

        result.SetPropertyOrThrow(KeyStrings.length.ToJSValue(), JSValue.CreateNumber(resultIndex));
        return result;
    }

    [JSPrototypeMethod]
    [JSExport("join", Length = 1)]
    public static JSValue Join(in Arguments a)
    {
        var @this = a.This as JSObject;
        var first = a.Get1();
        var length = (uint)@this.Length;
        var sep = first.IsUndefined ? "," : first.ToString();
        var sb = new StringBuilder();

        for (uint i = 0; i < length; i++)
        {
            var item = @this[i];
            if (i != 0)
                sb.Append(sep);

            if (item.IsNullOrUndefined)
                continue;

            sb.Append(ToStringPrimitive(item).ToString());
        }

        return new JSString(sb.ToString());
    }

    [JSPrototypeMethod]
    [JSExport("slice", Length = 2)]
    public static JSValue Slice(in Arguments a)
    {
        var @this = ToArrayLikeObject(a.This);
        var length = GetArrayLikeLengthLong(@this);
        var relativeStart = a.TryGetAt(0, out var start) ? ToIntegerOrInfinity(start) : 0;
        var relativeEnd = a.TryGetAt(1, out var end)
            ? (end.IsUndefined ? length : ToIntegerOrInfinity(end, length))
            : length;

        var actualStart = relativeStart < 0
            ? Math.Max(length + relativeStart, 0)
            : Math.Min(relativeStart, length);
        var actualEnd = relativeEnd < 0
            ? Math.Max(length + relativeEnd, 0)
            : Math.Min(relativeEnd, length);
        var count = Math.Max(actualEnd - actualStart, 0);
        var resultLength = (uint)Math.Min(count, uint.MaxValue);

        var result = CreateArraySpecies(@this, resultLength);
        uint resultIndex = 0;

        for (long sourceIndex = actualStart; sourceIndex < actualEnd; sourceIndex++)
        {
            if (!TryGetArrayLikeElement(@this, (uint)sourceIndex, out var value))
            {
                resultIndex++;
                continue;
            }

            CreateDataPropertyOrThrow(result, resultIndex++, value);
        }

        return result;
    }

    [JSPrototypeMethod]
    [JSExport("toLocaleString", Length = 0)]
    internal static JSValue ToLocaleString(in Arguments a)
    {
        var @this = ToArrayLikeObject(a.This);
        var (locale, format) = a.Get2();
        StringBuilder sb = new();

        var def = "N0";

        string strFormat = format.IsNullOrUndefined ? def : (format.IsString ? format.ToString() :
            throw JSEngine.NewTypeError("Options not supported, use .Net String Formats")
            );

        CultureInfo culture = locale.IsNullOrUndefined ? CultureInfo.CurrentCulture : CultureInfo.GetCultureInfo(locale.ToString());

        // Group separator based on Culture Info.
        var separator = culture.TextInfo.ListSeparator;

        bool first = true;
        var en = @this.GetElementEnumerator();

        while (en.MoveNext(out var n))
        {
            if (!first)
            {
                //sb.Append(',');
                sb.Append(separator);
            }

            first = false;
            sb.Append(n.ToLocaleString(strFormat, culture));
        }

        return new JSString(sb.ToString());
    }

    [JSPrototypeMethod]
    [JSExport("toString")]
    internal new static JSValue ToString(in Arguments args)
    {
        if (args.This.IsArray)
            return Join(in args);

        return args.This.InvokeMethod(KeyStrings.join, in args);
    }

    [JSPrototypeMethod]
    [JSExport("toReversed", Length = 0)]
    internal static JSValue ToReversed(in Arguments a)
    {
        var source = ToArrayLikeObject(a.This);
        var length = GetArrayLikeLength(source);
        var result = new JSArray(length);
        for (uint i = 0; i < length; i++)
            result[i] = source[length - i - 1];
        return result;
    }

    [JSPrototypeMethod]
    [JSExport("toSorted", Length = 1)]
    internal static JSValue ToSorted(in Arguments a)
    {
        var source = ToArrayLikeObject(a.This);
        var length = GetArrayLikeLengthLong(source);
        if (length > uint.MaxValue)
            throw JSEngine.NewRangeError("Invalid array length");

        var copy = Slice(new Arguments(a.This, JSValue.NumberZero, JSValue.CreateNumber(length)));
        return copy.InvokeMethod(KeyStrings.GetOrCreate("sort"), a.Get1());
    }

    [JSPrototypeMethod]
    [JSExport("toSpliced", Length = 2)]
    internal static JSValue ToSpliced(in Arguments a)
    {
        var source = ToArrayLikeObject(a.This);
        var length = GetArrayLikeLengthLong(source);
        if (length > uint.MaxValue)
            throw JSEngine.NewRangeError("Invalid array length");

        var len = (uint)length;

        long relativeStart = a.TryGetAt(0, out var startArg)
            ? ToIntegerOrInfinity(startArg)
            : 0;
        uint actualStart = relativeStart < 0
            ? (uint)Math.Max(len + relativeStart, 0)
            : (uint)Math.Min(relativeStart, len);

        uint insertCount = a.Length > 2 ? (uint)(a.Length - 2) : 0;

        long actualSkipCount;
        if (a.Length == 0)
            actualSkipCount = 0;
        else if (a.Length == 1)
            actualSkipCount = len - actualStart;
        else
        {
            var dc = ToIntegerOrInfinity(a[1]);
            actualSkipCount = Math.Min(Math.Max(dc, 0), len - actualStart);
        }

        var newLen = len - (uint)actualSkipCount + insertCount;
        var result = new JSArray(newLen);

        uint i = 0;
        for (; i < actualStart; i++)
            result[i] = source[i];
        for (uint j = 0; j < insertCount; j++)
            result[i++] = a[(int)(j + 2)];
        for (uint k = actualStart + (uint)actualSkipCount; k < len; k++)
            result[i++] = source[k];

        return result;
    }

    [JSPrototypeMethod]
    [JSExport("with", Length = 2)]
    internal static JSValue With(in Arguments a)
    {
        var source = ToArrayLikeObject(a.This);
        var length = GetArrayLikeLengthLong(source);
        if (length > uint.MaxValue)
            throw JSEngine.NewRangeError("Invalid array length");

        var len = (uint)length;
        var (indexArg, value) = a.Get2();
        var relativeIndex = ToIntegerOrInfinity(indexArg);
        long actualIndex = relativeIndex >= 0 ? (long)relativeIndex : len + relativeIndex;

        if (actualIndex < 0 || actualIndex >= len)
            throw JSEngine.NewRangeError("Invalid index");

        var result = new JSArray(len);
        for (uint i = 0; i < len; i++)
            result[i] = i == (uint)actualIndex ? value : source[i];

        return result;
    }

}
