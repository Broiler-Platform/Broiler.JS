using System;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.BuiltIns.Null;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.BuiltIns.Function;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Array;

public partial class JSArray
{
    private static bool HasIndexedProperty(JSObject @object, uint index)
        => @object.HasProperty(JSValue.CreateNumber(index)).BooleanValue;

    private static JSValue GetIndexedValue(JSObject @object, uint index)
        => @object[index];

    private static void SetIndexedValue(JSObject @object, uint index, JSValue value)
        => @object.SetValue(index, value, @object, true);

    private static void DeleteIndexedValueOrThrow(JSObject @object, uint index)
    {
        if (!@object.Delete(index).BooleanValue)
            throw JSEngine.NewTypeError($"Cannot delete property {index}");
    }

    [JSPrototypeMethod]
    [JSExport("copyWithin", Length = 2)]
    public static JSValue CopyWithin(in Arguments a)
    {
        var (t, s) = a.Get2();
        var @this = ToArrayLikeObject(a.This);
        var length = (int)GetArrayLikeLength(@this);
        var target = (int)ToIntegerOrInfinity(t);
        var start = (int)ToIntegerOrInfinity(s);
        var end = a.TryGetAt(2, out var e) ? (int)ToIntegerOrInfinity(e) : int.MaxValue;

        target = target < 0 ? Math.Max(length + target, 0) : Math.Min(target, length);
        start = start < 0 ? Math.Max(length + start, 0) : Math.Min(start, length);
        end = end < 0 ? Math.Max(length + end, 0) : Math.Min(end, length);

        // Calculate the number of values to copy.
        int count = Math.Min(end - start, length - target);

        // Check if we need to copy in reverse due to an overlap.
        int direction = 1;
        if (start < target && target < start + count)
        {
            direction = -1;
            start += count - 1;
            target += count - 1;
        }

        while (count > 0)
        {
            var fromKey = JSValue.CreateNumber(start);
            if (@this.HasProperty(fromKey).BooleanValue)
            {
                @this.SetValue((uint)target, @this.GetValue(fromKey, @this), @this);
            }
            else if (!@this.Delete((uint)target).BooleanValue)
            {
                throw JSEngine.NewTypeError($"Cannot delete property {target}");
            }

            // Progress to the next element.
            start += direction;
            target += direction;
            count--;
        }

        return @this;
    }

    /// <summary>
    /// Fills all the elements of a typed array from a start index to an end index with a
    /// static value.
    /// </summary>
    /// <param name="value"> The value to fill the typed array with. </param>
    /// <param name="start"> Optional. Start index. Defaults to 0. </param>
    /// <param name="end"> Optional. End index (exclusive). Defaults to the length of the array. </param>
    /// <returns> The array that is being operated on. </returns>
    [JSPrototypeMethod]
    [JSExport("fill", Length = 1)]
    public static JSValue Fill(in Arguments a)
    {
        var @this = ToArrayLikeObject(a.This);
        var (value, start, end) = a.Get3();

        var len = GetArrayLikeLengthLong(@this);
        var relativeStart = ToIntegerOrInfinity(start);
        var relativeEnd = ToIntegerOrInfinity(end, len);

        var startIndex = relativeStart < 0 ? Math.Max(len + relativeStart, 0) : Math.Min(relativeStart, len);
        var endIndex = relativeEnd < 0 ? Math.Max(len + relativeEnd, 0) : Math.Min(relativeEnd, len);

        for (var index = startIndex; index < endIndex; index++)
            SetIndexedValue(@this, (uint)index, value);

        return @this;
    }

    [JSPrototypeMethod]
    [JSExport("push", Length = 1)]
    public static JSValue Push(in Arguments a)
    {
        var @this = ToArrayLikeObject(a.This);
        long length = GetArrayLikeLengthLong(@this);

        if (length + a.Length > MaxArrayLikeLength)
            throw @this is JSArray
                ? JSEngine.NewRangeError("Invalid array length")
                : JSEngine.NewTypeError("Invalid array length");

        if (@this is JSArray array && length <= uint.MaxValue)
        {
            var mustSetLengthThroughProperty = false;
            for (var index = 0; index < a.Length; index++, length++)
            {
                var arrayIndex = (uint)length;
                array.SetValue(arrayIndex, a.GetAt(index), array, true);
                if (array.GetOwnPropertyDescriptor(JSValue.CreateNumber(arrayIndex)).IsUndefined)
                    mustSetLengthThroughProperty = true;
            }

            if (mustSetLengthThroughProperty)
                array.SetPropertyOrThrow(KeyStrings.length.ToJSValue(), new JSNumber(length));
            else
                array.Length = (int)length;

            return new JSNumber(length);
        }

        for (var index = 0; index < a.Length; index++, length++)
        {
            var item = a.GetAt(index);
            if (length <= uint.MaxValue)
            {
                SetIndexedValue(@this, (uint)length, item);
            }
            else
            {
                @this.SetPropertyOrThrow(KeyStrings.GetOrCreate(length.ToString()).ToJSValue(), item);
            }
        }

        var newLength = new JSNumber(length);
        @this.SetPropertyOrThrow(KeyStrings.length.ToJSValue(), newLength);
        return newLength;
    }

    [JSPrototypeMethod]
    [JSExport("pop")]
    public static JSValue Pop(in Arguments a)
    {
        var @this = ToArrayLikeObject(a.This);
        var length = GetArrayLikeLength(@this);

        if (length == 0)
        {
            @this.SetPropertyOrThrow(KeyStrings.length.ToJSValue(), JSNumber.Zero);
            return JSUndefined.Value;
        }

        var index = length - 1;
        var element = @this[index];

        if (!@this.Delete(index).BooleanValue)
            throw JSEngine.NewTypeError($"Cannot delete property {index}");

        @this.SetPropertyOrThrow(KeyStrings.length.ToJSValue(), new JSNumber(index));
        return element;
    }

    [JSPrototypeMethod]
    [JSExport("reverse")]
    public static JSValue Reverse(in Arguments a)
    {
        var @this = ToArrayLikeObject(a.This);
        var lower = 0u;
        var upper = GetArrayLikeLength(@this);
        if (upper == 0)
            return @this;

        upper--;
        while (lower < upper)
        {
            var lowerExists = HasIndexedProperty(@this, lower);
            var upperExists = HasIndexedProperty(@this, upper);
            var lowerValue = lowerExists ? GetIndexedValue(@this, lower) : JSUndefined.Value;
            var upperValue = upperExists ? GetIndexedValue(@this, upper) : JSUndefined.Value;

            if (lowerExists && upperExists)
            {
                SetIndexedValue(@this, lower, upperValue);
                SetIndexedValue(@this, upper, lowerValue);
            }
            else if (!lowerExists && upperExists)
            {
                SetIndexedValue(@this, lower, upperValue);
                DeleteIndexedValueOrThrow(@this, upper);
            }
            else if (lowerExists && !upperExists)
            {
                DeleteIndexedValueOrThrow(@this, lower);
                SetIndexedValue(@this, upper, lowerValue);
            }

            lower++;
            upper--;
        }

        return @this;
    }

    [JSPrototypeMethod]
    [JSExport("shift", Length = 0)]
    public static JSValue Shift(in Arguments a)
    {
        var @this = ToArrayLikeObject(a.This);
        JSValue first = JSUndefined.Value;
        var @object = @this;

        if (@object.IsSealedOrFrozen())
            throw JSEngine.NewTypeError("Cannot modify property length");

        var n = GetArrayLikeLength(@object);
        if (n == 0)
        {
            @object.SetPropertyOrThrow(KeyStrings.length.ToJSValue(), JSNumber.Zero);
            return first;
        }

        first = @this[0];
        var last = n - 1;
        for (uint i = 1; i < n; i++)
        {
            if (HasIndexedProperty(@object, i))
                SetIndexedValue(@object, i - 1, GetIndexedValue(@object, i));
            else
                DeleteIndexedValueOrThrow(@object, i - 1);
        }

        DeleteIndexedValueOrThrow(@object, last);

        @object.SetPropertyOrThrow(KeyStrings.length.ToJSValue(), new JSNumber(last));

        return first;
    }

    [JSPrototypeMethod]
    [JSExport("sort", Length = 1)]
    public static JSValue Sort(in Arguments a)
    {
        var fx = a.Get1();
        var @this = a.This as JSObject;

        if (@this == null)
            throw JSEngine.NewTypeError($"Sort can only be called with an Array or an Object");

        if (!fx.IsUndefined && !fx.IsFunction)
            throw JSEngine.NewTypeError($"Argument is not a function");

        var length = @this.Length;
        if (length <= 1)
            return @this;

        Comparison<JSValue> cx = null;
        if (!fx.IsUndefined)
        {
            cx = (left, right) =>
            {
                left = left ?? JSNull.Value;
                right = right ?? JSNull.Value;

                if (left == JSNull.Value)
                {
                    if (right == JSNull.Value)
                        return 0;

                    return 1;
                }

                if (right == JSNull.Value)
                    return -1;

                if (left == JSUndefined.Value)
                {
                    if (right == JSUndefined.Value)
                        return 0;

                    return 1;
                }

                if (right == JSUndefined.Value)
                    return -1;

                var r = fx.InvokeFunction(new Arguments(JSUndefined.Value, left, right)).DoubleValue;

                if (double.IsNaN(r))
                    return 0;

                return Math.Sign(r);
            };
        }
        else
        {
            cx = (left, right) =>
            {
                left = left ?? JSNull.Value;
                right = right ?? JSNull.Value;

                if (left == JSNull.Value)
                {
                    if (right == JSNull.Value)
                        return 0;

                    return 1;
                }

                if (right == JSNull.Value)
                    return -1;

                if (left == JSUndefined.Value)
                {
                    if (right == JSUndefined.Value)
                        return 0;
                    return 1;
                }

                if (right == JSUndefined.Value)
                    return -1;

                return string.CompareOrdinal(
                    left.IsUndefined ? string.Empty : left.ToString(),
                    right.IsUndefined ? string.Empty : right.ToString());
            };
        }

        var values = new System.Collections.Generic.List<JSValue>(length);
        for (uint index = 0; index < length; index++)
        {
            if (HasIndexedProperty(@this, index))
                values.Add(GetIndexedValue(@this, index));
        }

        values.Sort(cx);

        uint writeIndex = 0;
        foreach (var item in values)
            SetIndexedValue(@this, writeIndex++, item);

        while (writeIndex < length)
            DeleteIndexedValueOrThrow(@this, writeIndex++);

        return @this;
    }

    [JSPrototypeMethod]
    [JSExport("splice", Length = 2)]
    public static JSValue Splice(in Arguments a)
    {
        var r = new JSArray();

        long start = a.TryGetAt(0, out var startP)
            ? ToIntegerOrInfinity(startP)
            : 0;
        var deleteCount = a.TryGetAt(1, out var deleteCountP)
            ? ToIntegerOrInfinity(deleteCountP)
            : (a.Length == 0 ? 0 : long.MaxValue);

        var @this = a.This as JSObject;

        if (@this == null)
            return r;

        if (@this.IsSealedOrFrozen())
            throw JSEngine.NewTypeError("Cannot modify property length");

        var arrayLength = GetArrayLikeLengthLong(@this);

        // Fix the arguments so they are positive and within the bounds of the array.
        if (start < 0)
            start = Math.Max(arrayLength + start, 0);
        else
            start = Math.Min(start, arrayLength);

        deleteCount = Math.Min(Math.Max(deleteCount, 0), arrayLength - start);

        // Get the deleted items.
        var deletedItems = CreateArraySpecies(@this, deleteCount);

        if (arrayLength > int.MaxValue)
            throw JSEngine.NewRangeError("The array is too long");

        var arrayLengthInt = (int)arrayLength;
        var startInt = (int)start;
        var deleteCountInt = (int)deleteCount;

        for (uint i = 0; i < deleteCountInt; i++)
        {
            var fromIndex = (uint)(start + i);
            if (!HasIndexedProperty(@this, fromIndex))
                continue;

            CreateDataPropertyOrThrow(deletedItems, i, GetIndexedValue(@this, fromIndex));
        }

        var itemsLength = a.Length > 1 ? a.Length - 2 : 0;

        // Move the trailing elements.
        int offset = itemsLength - deleteCountInt;
        int newLength = arrayLengthInt + offset;

        if (deleteCountInt > itemsLength)
        {
            for (int i = startInt; i < arrayLengthInt - deleteCountInt; i++)
            {
                var fromIndex = (uint)(i + deleteCountInt);
                var toIndex = (uint)(i + itemsLength);
                if (HasIndexedProperty(@this, fromIndex))
                    SetIndexedValue(@this, toIndex, GetIndexedValue(@this, fromIndex));
                else
                    DeleteIndexedValueOrThrow(@this, toIndex);
            }

            // Delete the trailing elements.
            for (int i = arrayLengthInt; i > newLength; i--)
                DeleteIndexedValueOrThrow(@this, (uint)(i - 1));
        }
        else
        {
            for (int i = arrayLengthInt - deleteCountInt; i > startInt; i--)
            {
                var fromIndex = (uint)(i + deleteCountInt - 1);
                var toIndex = (uint)(i + itemsLength - 1);
                if (HasIndexedProperty(@this, fromIndex))
                    SetIndexedValue(@this, toIndex, GetIndexedValue(@this, fromIndex));
                else
                    DeleteIndexedValueOrThrow(@this, toIndex);
            }
        }

        @this.Length = newLength;

        // Insert the new elements.
        for (int i = 0; i < itemsLength; i++)
            SetIndexedValue(@this, (uint)(start + i), a[i + 2]);

        // Return the deleted items.
        return deletedItems;
    }

    [JSPrototypeMethod]
    [JSExport("unshift", Length = 1)]
    public static JSValue Unshift(in Arguments a)
    {
        var @this = ToArrayLikeObject(a.This);
        var argCount = (uint)a.Length;
        var length = GetArrayLikeLength(@this);

        if (length + argCount > MaxArrayLikeLength)
            throw JSEngine.NewRangeError("Invalid array length");

        for (var index = length; index > 0; index--)
        {
            var fromIndex = index - 1;
            var toIndex = fromIndex + argCount;

            if (@this.TryGetElement(fromIndex, out var value))
            {
                @this.SetValue(toIndex, value, @this);
            }
            else if (!@this.Delete(toIndex).BooleanValue)
            {
                throw JSEngine.NewTypeError($"Cannot delete property {toIndex}");
            }
        }

        for (uint index = 0; index < argCount; index++)
            @this.SetValue(index, a.GetAt((int)index), @this);

        var newLength = new JSNumber(length + argCount);
        @this.SetPropertyOrThrow(KeyStrings.length.ToJSValue(), newLength);
        return newLength;
    }

}
