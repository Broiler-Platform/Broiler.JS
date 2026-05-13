using System;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.BuiltIns.Null;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.BuiltIns.Function;
using Broiler.JavaScript.Engine.Extensions;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Array;

public partial class JSArray
{
    [JSPrototypeMethod]
    [JSExport("copyWithin", Length = 2)]
    public static JSValue CopyWithin(in Arguments a)
    {
        var (t, s) = a.Get2();
        var @this = ToArrayLikeObject(a.This);
        var length = (int)GetArrayLikeLength(@this);
        var target = t.IntValue;
        var start = s.IntValue;
        var end = a.TryGetAt(2, out var e) ? e.IntValue : int.MaxValue;

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
            if (@this.TryGetElement((uint)start, out var elementValue))
            {
                @this.SetValue((uint)target, elementValue, @this);
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

        var len = (int)GetArrayLikeLength(@this);
        var relativeStart = start.AsInt32OrDefault();
        var relativeEnd = end.AsInt32OrDefault(len);

        relativeStart = relativeStart < 0 ? Math.Max(len + relativeStart, 0) : Math.Min(relativeStart, len);
        relativeEnd = relativeEnd < 0 ? Math.Max(len + relativeEnd, 0) : Math.Min(relativeEnd, len);

        for (; relativeStart < relativeEnd; relativeStart++)
            @this[(uint)relativeStart] = value;

        return @this;
    }

    [JSPrototypeMethod]
    [JSExport("push", Length = 1)]
    public static JSValue Push(in Arguments a)
    {
        var @this = ToArrayLikeObject(a.This);
        long length = GetArrayLikeLengthLong(@this);

        if (length + a.Length > MaxArrayLikeLength)
            throw JSEngine.NewTypeError("Invalid array length");

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
                array[KeyStrings.length] = new JSNumber(length);
            else
                array.Length = (int)length;

            return new JSNumber(length);
        }

        for (var index = 0; index < a.Length; index++, length++)
        {
            var item = a.GetAt(index);
            if (length <= uint.MaxValue)
            {
                @this[(uint)length] = item;
            }
            else
            {
                @this[KeyStrings.GetOrCreate(length.ToString())] = item;
            }
        }

        var newLength = new JSNumber(length);
        @this[KeyStrings.length] = newLength;
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
            @this[KeyStrings.length] = JSNumber.Zero;
            return JSUndefined.Value;
        }

        var index = length - 1;
        var element = @this[index];

        if (!@this.Delete(index).BooleanValue)
            throw JSEngine.NewTypeError($"Cannot delete property {index}");

        @this[KeyStrings.length] = new JSNumber(index);
        return element;
    }

    [JSPrototypeMethod]
    [JSExport("reverse")]
    public static JSValue Reverse(in Arguments a)
    {
        var @this = a.This as JSObject;
        var i = 0;
        var j = @this.Length - 1;
        ref var elements = ref @this.GetElements();

        while (i < j)
        {
            var swap = elements[(uint)i];
            elements.Put((uint)i++) = elements[(uint)j];
            elements.Put((uint)j--) = swap;
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
            @object[KeyStrings.length] = JSNumber.Zero;
            return first;
        }

        ref var oe = ref @object.GetElements();
        first = @this[0];
        var last = n - 1;

        if (!oe.IsNull)
        {
            for (uint i = 1; i < n; i++)
                oe.Put(i - 1) = oe[i];

            oe.RemoveAt(last);
        }

        @object[KeyStrings.length] = new JSNumber(last);

        return first;
    }

    [JSPrototypeMethod]
    [JSExport("sort", Length = 1)]
    public static JSValue Sort(in Arguments a)
    {
        // To be modified by Akash
        var fx = a.Get1();
        var @this = a.This as JSObject;

        if (@this == null)
            throw JSEngine.NewTypeError($"Sort can only be called with an Array or an Object");

        var length = @this.Length;
        if (length <= 1)
            return @this;

        Comparison<JSValue> cx = null;
        if (fx is JSFunction fn)
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

                var arg = new Arguments(JSUndefined.Value, left, right);
                var r = fn.f(arg).DoubleValue;

                if (double.IsNaN(r))
                    return 0;

                return Math.Sign(r);
            };
        }
        else
        {
            if (!fx.IsUndefined)
                throw JSEngine.NewTypeError($"Argument is not a function");

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

        ref var elements = ref @this.GetElements();
        elements.QuickSort((a, b) => cx((JSValue)a, (JSValue)b), 0, (uint)(length - 1));

        return @this;
    }

    [JSPrototypeMethod]
    [JSExport("splice", Length = 2)]
    public static JSValue Splice(in Arguments a)
    {
        var r = new JSArray();

        var start = a.TryGetAt(0, out var startP)
            ? startP.IntegerValue
            : 0;
        var deleteCount = a.TryGetAt(1, out var deleteCountP)
            ? deleteCountP.IntegerValue
            : (a.Length == 0 ? 0 : int.MaxValue);

        var @this = a.This as JSObject;

        if (@this == null)
            return r;

        if (@this.IsSealedOrFrozen())
            throw JSEngine.NewTypeError("Cannot modify property length");

        // Get the length of the array.
        int arrayLength = @this.Length;

        // This method only supports arrays of length up to 2^31 - 1.
        if (@this.Length > int.MaxValue)
            throw JSEngine.NewRangeError("The array is too long");

        // Fix the arguments so they are positive and within the bounds of the array.
        if (start < 0)
            start = Math.Max(arrayLength + start, 0);
        else
            start = Math.Min(start, arrayLength);

        deleteCount = Math.Min(Math.Max(deleteCount, 0), arrayLength - start);

        ref var elements = ref @this.GetElements();

        // Get the deleted items.
        var deletedItems = CreateArraySpecies(@this, (uint)deleteCount);

        for (uint i = 0; i < deleteCount; i++)
        {
            ref var property = ref elements.Get((uint)(start + i));

            if (property.IsEmpty)
                continue;

            CreateDataPropertyOrThrow(deletedItems, i, property.IsProperty
                ? @this.GetValue(in property)
                : (JSValue)property.value);
        }

        var itemsLength = a.Length > 1 ? a.Length - 2 : 0;

        // Move the trailing elements.
        int offset = itemsLength - deleteCount;
        int newLength = arrayLength + offset;

        if (deleteCount > itemsLength)
        {
            for (int i = start + itemsLength; i < newLength; i++)
                elements.Put((uint)i) = elements.Get((uint)(i - offset));

            // Delete the trailing elements.
            for (int i = newLength; i < arrayLength; i++)
                elements.RemoveAt((uint)i);
        }
        else
        {
            for (int i = newLength - 1; i >= start + itemsLength; i--)
                elements.Put((uint)i) = elements.Get((uint)(i - offset));
        }

        @this.Length = newLength;

        // Insert the new elements.
        for (int i = 0; i < itemsLength; i++)
        {
            elements.Put((uint)(start + i), a[i + 2]);
        }

        // Return the deleted items.
        return deletedItems;
    }

    [JSPrototypeMethod]
    [JSExport("unshift", Length = 1)]
    public static JSValue Unshift(in Arguments a)
    {
        var @this = a.This as JSObject;
        if (@this == null)
            return JSUndefined.Value;

        if (@this.IsSealedOrFrozen())
            throw JSEngine.NewTypeError("Cannot modify property length");

        var l = a.This.Length;
        if (l > 0)
        {
            // move existing elements to the right; MoveElements updates Length
            @this.MoveElements(0, a.Length);
        }
        else
        {
            // No existing elements — set length explicitly since MoveElements
            // is not called.  Length can be 0 or -1 (missing length property).
            @this.Length = a.Length;
        }

        // insert the new elements at the front
        ref var elements = ref @this.GetElements();
        for (uint i = 0; i < a.Length; i++)
        {
            elements.Put(i, a.GetAt((int)i));
        }

        return new JSNumber(a.This.Length);
    }

}
