using System;
using System.Text;
using System.Globalization;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine.Extensions;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.BuiltIns.Symbol;

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

        // IsConcatSpreadable: @@isConcatSpreadable overrides the IsArray default.
        static bool IsConcatSpreadable(JSValue item, out JSObject obj)
        {
            obj = item as JSObject;
            if (obj == null)
                return false;

            var spreadable = obj[(IJSSymbol)JSSymbol.isConcatSpreadable];
            if (!spreadable.IsUndefined)
                return spreadable.BooleanValue;

            return IsArrayValue(item);
        }

        void Append(JSValue item)
        {
            if (IsConcatSpreadable(item, out var spreadable))
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
        var @this = ToArrayLikeObject(a.This);
        var first = a.Get1();
        var length = GetArrayLikeLength(@this);
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
            if (!TryGetArrayLikeElement(@this, sourceIndex, out var value))
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
        var (locales, options) = a.Get2();
        StringBuilder sb = new();

        var length = (uint)@this.Length;
        var toLocaleString = KeyStrings.GetOrCreate("toLocaleString");
        for (uint i = 0; i < length; i++)
        {
            if (i != 0)
                sb.Append(',');

            var item = @this[i];
            if (item.IsNullOrUndefined)
                continue;

            // §23.1.3.32: Invoke(element, "toLocaleString", « locales, options ») —
            // the locale and options arguments are forwarded to each element.
            var method = item[toLocaleString];
            sb.Append(method.InvokeFunction(new Arguments(item, locales, options)).StringValue);
        }

        return new JSString(sb.ToString());
    }

    [JSPrototypeMethod]
    [JSExport("toString")]
    internal new static JSValue ToString(in Arguments args)
    {
        // §23.1.3.36 Array.prototype.toString: ToObject(this), then Get "join"; if it is not callable,
        // fall back to %Object.prototype.toString%. This is generic (works on non-arrays and on a boxed
        // primitive receiver) and honours an overridden / non-callable "join" instead of throwing.
        var array = ToArrayLikeObject(args.This);
        var join = array[KeyStrings.join];
        if (join.IsFunction)
            return join.InvokeFunction(new Arguments(array));

        return JSObject.ToString(new Arguments(array));
    }

    [JSPrototypeMethod]
    [JSExport("toReversed", Length = 0)]
    internal static JSValue ToReversed(in Arguments a)
    {
        var source = ToArrayLikeObject(a.This);
        var length = GetArrayLikeLength(source);
        var result = new JSArray(length);
        for (uint i = 0; i < length; i++)
            CreateDataPropertyOrThrow(result, i, source[length - i - 1]);
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

        var compareFn = a.Get1();
        if (!compareFn.IsUndefined && !compareFn.IsFunction)
            throw JSEngine.NewTypeError("Argument is not a function");

        Comparison<JSValue> comparison;
        if (compareFn.IsUndefined)
            comparison = CompareArraySortValues;
        else
            comparison = (left, right) => CompareArraySortValues(left, right, compareFn);

        var len = (uint)length;
        var values = new System.Collections.Generic.List<JSValue>();
        for (uint index = 0; index < len; index++)
            values.Add(source[index]);

        values.Sort(comparison);

        var result = new JSArray(len);
        for (uint index = 0; index < len; index++)
            CreateDataPropertyOrThrow(result, index, values[(int)index]);

        return result;
    }

    private static int CompareArraySortValues(JSValue left, JSValue right)
    {
        if (left.IsUndefined)
        {
            if (right.IsUndefined)
                return 0;
            return 1;
        }

        if (right.IsUndefined)
            return -1;

        return string.CompareOrdinal(left.ToString(), right.ToString());
    }

    private static int CompareArraySortValues(JSValue left, JSValue right, JSValue compareFn)
    {
        if (left.IsUndefined)
        {
            if (right.IsUndefined)
                return 0;
            return 1;
        }

        if (right.IsUndefined)
            return -1;

        var result = compareFn.InvokeFunction(new Arguments(JSUndefined.Value, left, right)).DoubleValue;
        if (double.IsNaN(result))
            return 0;

        return Math.Sign(result);
    }

    [JSPrototypeMethod]
    [JSExport("toSpliced", Length = 2)]
    internal static JSValue ToSpliced(in Arguments a)
    {
        var source = ToArrayLikeObject(a.This);
        // len is LengthOfArrayLike (ToLength → clamped to [0, 2^53-1]). The
        // 2^32-1 limit is only enforced later against newLen (ArrayCreate), not
        // against len itself, so the spec error ordering (TypeError for newLen
        // beyond 2^53-1 vs RangeError for newLen beyond 2^32-1) is preserved.
        long len = GetArrayLikeLengthLong(source);

        long relativeStart = a.TryGetAt(0, out var startArg)
            ? ToIntegerOrInfinity(startArg)
            : 0;
        long actualStart = relativeStart < 0
            ? Math.Max(len + relativeStart, 0)
            : Math.Min(relativeStart, len);

        long insertCount = a.Length > 2 ? a.Length - 2 : 0;

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

        long newLen = len + insertCount - actualSkipCount;
        // ArrayCreate would throw RangeError beyond 2^32-1; newLen beyond
        // 2^53-1 is a TypeError per step 12 of Array.prototype.toSpliced.
        if (newLen > MaxArrayLikeLength)
            throw JSEngine.NewTypeError("Invalid array length");
        if (newLen > uint.MaxValue)
            throw JSEngine.NewRangeError("Invalid array length");

        var result = new JSArray((uint)newLen);

        uint i = 0;
        for (long s = 0; s < actualStart; s++)
            CreateDataPropertyOrThrow(result, i++, source[(uint)s]);
        for (int j = 0; j < insertCount; j++)
            CreateDataPropertyOrThrow(result, i++, a[j + 2]);
        for (long k = actualStart + actualSkipCount; k < len; k++)
            CreateDataPropertyOrThrow(result, i++, source[(uint)k]);

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
            CreateDataPropertyOrThrow(result, i, i == (uint)actualIndex ? value : source[i]);

        return result;
    }

}
