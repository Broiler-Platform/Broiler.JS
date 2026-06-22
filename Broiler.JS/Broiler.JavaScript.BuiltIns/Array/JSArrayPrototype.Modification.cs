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

    // 64-bit-index overloads for array-like objects whose length exceeds the
    // 32-bit array-index range (up to 2^53-1). Valid array indices (< 2^32-1)
    // keep the fast uint path; larger integer indices are addressed by their
    // canonical numeric property key.
    private static bool HasIndexedProperty(JSObject @object, long index)
        => index < uint.MaxValue
            ? HasIndexedProperty(@object, (uint)index)
            : @object.HasProperty(JSValue.CreateNumber(index)).BooleanValue;

    private static JSValue GetIndexedValue(JSObject @object, long index)
        => index < uint.MaxValue
            ? GetIndexedValue(@object, (uint)index)
            : @object[JSValue.CreateNumber(index)];

    private static void SetIndexedValue(JSObject @object, long index, JSValue value)
    {
        if (index < uint.MaxValue)
            SetIndexedValue(@object, (uint)index, value);
        else
            @object.SetValue(JSValue.CreateNumber(index), value, @object, true);
    }

    private static void DeleteIndexedValueOrThrow(JSObject @object, long index)
    {
        if (index < uint.MaxValue)
            DeleteIndexedValueOrThrow(@object, (uint)index);
        else if (!@object.Delete(JSValue.CreateNumber(index)).BooleanValue)
            throw JSEngine.NewTypeError($"Cannot delete property {index}");
    }

    private static void SetArrayLikeLength(JSObject @object, long length)
        => @object.SetValue(KeyStrings.length, JSValue.CreateNumber(length), @object, true);

    [JSPrototypeMethod]
    [JSExport("copyWithin", Length = 2)]
    public static JSValue CopyWithin(in Arguments a)
    {
        var (t, s) = a.Get2();
        var @this = ToArrayLikeObject(a.This);
        // Indices are computed in long space: ToIntegerOrInfinity maps ±∞ to ±long.MaxValue, which an
        // int cast would truncate to -1 (so e.g. copyWithin(Infinity, 0) wrongly copied a value). An
        // undefined `end` (whether passed explicitly or omitted) defaults to the length.
        var length = GetArrayLikeLengthLong(@this);
        var relTarget = ToIntegerOrInfinity(t);
        var relStart = ToIntegerOrInfinity(s);
        var relEnd = ToIntegerOrInfinity(a.GetAt(2), length);

        long target = relTarget < 0 ? Math.Max(length + relTarget, 0) : Math.Min(relTarget, length);
        long start = relStart < 0 ? Math.Max(length + relStart, 0) : Math.Min(relStart, length);
        long end = relEnd < 0 ? Math.Max(length + relEnd, 0) : Math.Min(relEnd, length);

        // Calculate the number of values to copy.
        long count = Math.Min(end - start, length - target);

        // Check if we need to copy in reverse due to an overlap.
        long direction = 1;
        if (start < target && target < start + count)
        {
            direction = -1;
            start += count - 1;
            target += count - 1;
        }

        while (count > 0)
        {
            var fromKey = JSValue.CreateNumber((double)start);
            if (@this.HasProperty(fromKey).BooleanValue)
            {
                SetIndexedValue(@this, target, @this.GetValue(fromKey, @this));
            }
            else if (!@this.Delete(JSValue.CreateNumber((double)target)).BooleanValue)
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
            SetIndexedValue(@this, index, value);

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

        // 2^32-1 (uint.MaxValue) is NOT a valid array index — array indices are < 2^32-1 —
        // so an element pushed there must be stored as an ordinary property and the
        // subsequent length update to 2^32 must throw RangeError AFTER the element is set.
        // The uint fast path would address it as element 2^32-1 and drop it, so route a
        // length already at 2^32-1 through the slow path (test262: push/S15.4.4.7_A3).
        if (@this is JSArray array && length < uint.MaxValue)
        {
            var mustSetLengthThroughProperty = false;
            for (var index = 0; index < a.Length; index++, length++)
            {
                var arrayIndex = (uint)length;
                array.SetValue(arrayIndex, a.GetAt(index), array, true);
                if (array.GetOwnPropertyDescriptor(JSValue.CreateNumber(arrayIndex)).IsUndefined)
                    mustSetLengthThroughProperty = true;
            }

            // The fast `Length` setter takes an int, so a length above int.MaxValue
            // (e.g. pushing nothing onto an array whose length is already 2^32 - 1)
            // must go through the property to avoid wrapping to a negative value.
            if (mustSetLengthThroughProperty || length > int.MaxValue)
                array.SetPropertyOrThrow(KeyStrings.length.ToJSValue(), new JSNumber(length));
            else
                array.Length = (int)length;

            return new JSNumber(length);
        }

        for (var index = 0; index < a.Length; index++, length++)
        {
            // The long overload routes indices < 2^32-1 through the fast uint path and
            // addresses 2^32-1 and above by their canonical numeric key. 2^32-1 is NOT a
            // valid array index, so it must not take the uint fast path (which would drop
            // the element); `length <= uint.MaxValue` would wrongly include it.
            SetIndexedValue(@this, length, a.GetAt(index));
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
        // A generic array-like object's length is ToLength (up to 2^53-1), not a
        // 32-bit array index, so the last element of e.g. a `{ length: 2**32 }`
        // object is at index 2**32-1 — clamping to uint would read the wrong slot.
        var length = GetArrayLikeLengthLong(@this);

        if (length == 0)
        {
            @this.SetPropertyOrThrow(KeyStrings.length.ToJSValue(), JSNumber.Zero);
            return JSUndefined.Value;
        }

        var index = length - 1;
        var element = GetIndexedValue(@this, index);

        DeleteIndexedValueOrThrow(@this, index);

        @this.SetPropertyOrThrow(KeyStrings.length.ToJSValue(), JSValue.CreateNumber(index));
        return element;
    }

    [JSPrototypeMethod]
    [JSExport("reverse")]
    public static JSValue Reverse(in Arguments a)
    {
        var @this = ToArrayLikeObject(a.This);
        var lower = 0L;
        // LengthOfArrayLike (ToLength) clamps to 2^53-1, so the high index can exceed the
        // 32-bit array-index range. Walk with long indices and the long-keyed accessors,
        // which address indices >= 2^32 by their canonical numeric property key.
        var upper = GetArrayLikeLengthLong(@this);
        if (upper == 0)
            return @this;

        upper--;
        while (lower < upper)
        {
            // Spec §23.1.3.26 order: test then read the lower index fully before the upper one
            // (HasProperty(lower), Get(lower), HasProperty(upper), Get(upper)).
            var lowerExists = HasIndexedProperty(@this, lower);
            var lowerValue = lowerExists ? GetIndexedValue(@this, lower) : JSUndefined.Value;
            var upperExists = HasIndexedProperty(@this, upper);
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

        // §23.1.3.30 step 1: a non-callable comparefn is rejected before anything else.
        if (!fx.IsUndefined && !fx.IsFunction)
            throw JSEngine.NewTypeError($"Argument is not a function");

        // Step 2: let obj be ? ToObject(this value). Primitive receivers are boxed
        // into their wrapper objects; null/undefined throw a TypeError.
        var @this = ToArrayLikeObject(a.This);

        // Step 3: len = ToLength(? Get(obj, "length")) — read the (possibly inherited)
        // `length` property, not the CLR element-store count, so sort works on a
        // generic array-like object.
        var length = GetArrayLikeLengthLong(@this);
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

        var values = new System.Collections.Generic.List<JSValue>(length > int.MaxValue ? 0 : (int)length);
        for (long index = 0; index < length; index++)
        {
            if (HasIndexedProperty(@this, index))
                values.Add(GetIndexedValue(@this, index));
        }

        // §23.1.3.30 step 6: Array.prototype.sort must be stable. List<T>.Sort uses
        // an unstable introsort; carry the original index and break comparator ties
        // on it so equal-keyed elements retain their input order.
        var indexed = new (JSValue Value, int Index)[values.Count];
        for (var i = 0; i < values.Count; i++)
            indexed[i] = (values[i], i);
        System.Array.Sort(indexed, (a, b) =>
        {
            var r = cx(a.Value, b.Value);
            return r != 0 ? r : a.Index.CompareTo(b.Index);
        });

        long writeIndex = 0;
        foreach (var (item, _) in indexed)
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

        var itemsLength = a.Length > 1 ? a.Length - 2 : 0;

        // Array-like objects can carry a length beyond Int32 (up to 2^53-1).
        // Such indices overflow the 32-bit fast path below, so dispatch them to
        // a 64-bit implementation that addresses elements by numeric key.
        if (arrayLength > int.MaxValue)
            return SpliceLarge(in a, @this, arrayLength, start, deleteCount, itemsLength);

        // Get the deleted items.
        var deletedItems = CreateArraySpecies(@this, deleteCount);

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

        // Step 12: Set(A, "length", actualDeleteCount, true). A no-op for a plain array
        // (its length already matches), but a real, observable [[Set]] on a @@species
        // result — e.g. a Proxy records the set/getOwnPropertyDescriptor/defineProperty
        // traps (test262 splice/property-traps-order-with-species).
        deletedItems.SetPropertyOrThrow(KeyStrings.length.ToJSValue(), JSValue.CreateNumber(deleteCount));

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

        // Insert the new elements, THEN set the new length — Array.prototype.splice inserts
        // the items (step 16) before the final Set(O, "length", …) (step 17). When a species
        // constructor has made "length" non-writable, the items are still written before that
        // assignment throws (test262 sm/Array/splice-species-changes-length).
        for (int i = 0; i < itemsLength; i++)
            SetIndexedValue(@this, (uint)(start + i), a[i + 2]);

        // Step 17: Set(O, "length", len, true) — a real [[Set]] that throws when
        // "length" is non-writable (e.g. an array-like whose "length" is an
        // accessor with no setter), rather than the fast JSObject.Length setter,
        // which would silently overwrite the accessor with a data property.
        SetArrayLikeLength(@this, newLength);

        // Return the deleted items.
        return deletedItems;
    }

    // 64-bit splice for array-like objects whose length exceeds Int32 (up to
    // 2^53-1). Mirrors Array.prototype.splice steps 7-19, addressing elements by
    // numeric property key so indices beyond the 32-bit range are honoured
    // rather than rejected with a (spec-incorrect) "array is too long" error.
    private static JSValue SpliceLarge(in Arguments a, JSObject @this, long len, long start, long deleteCount, int itemsLength)
    {
        // Step 7: the resulting length must not exceed 2^53-1.
        if (len + itemsLength - deleteCount > MaxArrayLikeLength)
            throw JSEngine.NewTypeError("Invalid array length");

        var deletedItems = CreateArraySpecies(@this, deleteCount);

        for (long k = 0; k < deleteCount; k++)
        {
            var fromIndex = start + k;
            if (HasIndexedProperty(@this, fromIndex))
                CreateDataPropertyOrThrow(deletedItems, (uint)k, GetIndexedValue(@this, fromIndex));
        }

        // Step 12: Set(A, "length", actualDeleteCount, true) — observable on a @@species
        // proxy result, mirroring the 32-bit splice path above.
        deletedItems.SetPropertyOrThrow(KeyStrings.length.ToJSValue(), JSValue.CreateNumber(deleteCount));

        var newLength = len - deleteCount + itemsLength;

        if (deleteCount > itemsLength)
        {
            for (long k = start; k < len - deleteCount; k++)
            {
                var fromIndex = k + deleteCount;
                var toIndex = k + itemsLength;
                if (HasIndexedProperty(@this, fromIndex))
                    SetIndexedValue(@this, toIndex, GetIndexedValue(@this, fromIndex));
                else
                    DeleteIndexedValueOrThrow(@this, toIndex);
            }

            for (long k = len; k > newLength; k--)
                DeleteIndexedValueOrThrow(@this, k - 1);
        }
        else if (deleteCount < itemsLength)
        {
            for (long k = len - deleteCount; k > start; k--)
            {
                var fromIndex = k + deleteCount - 1;
                var toIndex = k + itemsLength - 1;
                if (HasIndexedProperty(@this, fromIndex))
                    SetIndexedValue(@this, toIndex, GetIndexedValue(@this, fromIndex));
                else
                    DeleteIndexedValueOrThrow(@this, toIndex);
            }
        }

        for (int i = 0; i < itemsLength; i++)
            SetIndexedValue(@this, start + i, a[i + 2]);

        SetArrayLikeLength(@this, newLength);

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

            // Source presence is tested with HasProperty and read with [[Get]] — both
            // traverse the prototype chain — so a hole shadowing an inherited indexed
            // property (e.g. Array.prototype[0]) is copied, not deleted.
            if (HasIndexedProperty(@this, fromIndex))
            {
                SetIndexedValue(@this, toIndex, GetIndexedValue(@this, fromIndex));
            }
            else
            {
                DeleteIndexedValueOrThrow(@this, toIndex);
            }
        }

        for (uint index = 0; index < argCount; index++)
            @this.SetValue(index, a.GetAt((int)index), @this);

        var newLength = new JSNumber(length + argCount);
        @this.SetPropertyOrThrow(KeyStrings.length.ToJSValue(), newLength);
        return newLength;
    }

}
