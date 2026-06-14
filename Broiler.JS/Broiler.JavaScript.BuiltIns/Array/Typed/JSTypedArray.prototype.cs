using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Broiler.JavaScript.BuiltIns.BigInt;
using Broiler.JavaScript.BuiltIns.Boolean;
using Broiler.JavaScript.BuiltIns.Generator;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.BuiltIns.Function;
using Broiler.JavaScript.Engine.Extensions;
using Broiler.JavaScript.Engine.Core;
using System.Linq;

namespace Broiler.JavaScript.BuiltIns.Array.Typed;

partial class JSTypedArray
{
    // Get(O, k) for the iteration methods: the element value, or undefined when k is past the view's
    // *live* length. The %TypedArray%.prototype iterators capture the length once (TypedArrayLength at
    // the start) and then index 0…len-1 with an ordinary [[Get]], so when the backing resizable buffer is
    // grown mid-iteration the extra elements are never visited, and when it is shrunk the now-out-of-bounds
    // indices read as undefined rather than truncating the iteration.
    private JSValue ReadElement(int k) => k < length ? this[(uint)k] : JSUndefined.Value;

    [JSExport("toString")]
    private new JSValue ToString(in Arguments a) => new JSString(ToString());

    /// <summary>
    /// Copies the sequence of array elements within the array to the position starting at
    /// target. The copy is taken from the index positions of the second and third arguments
    /// start and end. The end argument is optional and defaults to the length of the array.
    /// This method has the same algorithm as Array.prototype.copyWithin.
    /// </summary>
    /// <param name="target"> Target start index position where to copy the elements to. </param>
    /// <param name="start"> Source start index position where to start copying elements from. </param>
    /// <param name="end"> Optional. Source end index position where to end copying elements from. </param>
    /// <returns> The array that is being operated on. </returns>
    [JSExport("copyWithin", Length = 2)]
    public JSValue CopyWithin(in Arguments a)
    {
        ValidateTypedArray("copyWithin");
        var (t, s) = a.Get2();
        var target = t.IntValue;
        var start = s.IntValue;
        var end = a.TryGetAt(2, out var e) ? e.IntValue : int.MaxValue;
        // Negative values represent offsets from the end of the array.
        target = target < 0 ? Math.Max(Length + target, 0) : Math.Min(target, Length);
        start = start < 0 ? Math.Max(Length + start, 0) : Math.Min(start, Length);
        end = end < 0 ? Math.Max(Length + end, 0) : Math.Min(end, Length);

        // Calculate the number of values to copy.
        int count = Math.Min(end - start, Length - target);

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
            // Get the value of the array element.
            var elementValue = this[(uint)start];

            // Copy the value to the new position.
            this[(uint)target] = elementValue;

            // Progress to the next element.
            start += direction;
            target += direction;
            count--;
        }

        return this;
        //throw new NotImplementedException();
    }


    [JSExport("entries")]
    public new JSValue Entries(in Arguments a) { ValidateTypedArray("entries"); return GetArrayIterator(ArrayIteratorKind.Entry); }

    [JSExport("every", Length = 1)]
    public JSValue Every(in Arguments a)
    {
        ValidateTypedArray("every");
        var (first, thisArg) = a.Get2();
        if (first is not JSFunction fn)
            throw JSEngine.NewTypeError($"First argument is not function");
        var len = Length;
        for (int k = 0; k < len; k++)
        {
            var itemArgs = new Arguments(thisArg, ReadElement(k), new JSNumber(k), this);
            if (!fn.InvokeCallback(itemArgs).BooleanValue)
                return JSBoolean.False;
        }
        return JSBoolean.True;
    }

    [JSExport("fill", Length = 1)]
    public JSValue Fill(in Arguments a)
    {
        ValidateTypedArray("fill");
        var (value, start, end) = a.Get3();
        // JSArray r = new JSArray();
        var len = Length;
        var relativeStart = start.AsInt32OrDefault();
        var relativeEnd = end.AsInt32OrDefault(len);
        // Negative values represent offsets from the end of the array.
        relativeStart = relativeStart < 0 ? Math.Max(len + relativeStart, 0) : Math.Min(relativeStart, len);
        relativeEnd = relativeEnd < 0 ? Math.Max(len + relativeEnd, 0) : Math.Min(relativeEnd, len);
        for (; relativeStart < relativeEnd; relativeStart++)
        {
            this[(uint)relativeStart] = value;
        }
        return this;
    }

    [JSExport("filter", Length = 1)]
    public JSValue Filter(in Arguments a)
    {
        ValidateTypedArray("filter");
        var (callback, thisArg) = a.Get2();

        if (callback is not JSFunction fn)
            throw JSEngine.NewTypeError($"{callback} is not a function in Array.prototype.filter");
        var values = new List<JSValue>();
        var len = Length;
        for (int k = 0; k < len; k++)
        {
            var item = ReadElement(k);
            var itemParams = new Arguments(thisArg, item, new JSNumber(k), this);
            if (fn.InvokeCallback(itemParams).BooleanValue)
                values.Add(item);
        }

        var result = CreateTypedArrayFromConstructor(GetSpeciesConstructor(this), values.Count);
        for (uint i = 0; i < values.Count; i++)
            result[i] = values[(int)i];

        return result;
    }

    [JSExport("toSorted", Length = 1)]
    public JSValue ToSorted(in Arguments a)
    {
        ValidateTypedArray("toSorted");
        var fx = a.Get1();
        if (!fx.IsUndefined && !fx.IsFunction)
            throw JSEngine.NewTypeError($"Argument is not a function");

        var cx = BuildSortComparison(fx);

        var len = Length;
        var values = new List<JSValue>(len);
        for (int i = 0; i < len; i++)
            values.Add(this[(uint)i]);

        values.Sort(cx);

        // toSorted returns a new typed array of the same type (TypedArrayCreateSameType,
        // never @@species), leaving this intact.
        var result = CreateSameTypeTypedArray(len);
        for (int i = 0; i < len; i++)
            result[(uint)i] = values[i];
        return result;
    }

    [JSExport("toReversed", Length = 0)]
    public JSValue ToReversed(in Arguments a)
    {
        ValidateTypedArray("toReversed");
        var len = Length;
        // toReversed creates a same-type array (TypedArrayCreateSameType), not @@species.
        var result = CreateSameTypeTypedArray(len);
        for (int i = 0; i < len; i++)
            result[(uint)i] = this[(uint)(len - i - 1)];
        return result;
    }

    [JSExport("with", Length = 2)]
    public JSValue With(in Arguments a)
    {
        ValidateTypedArray("with");
        var len = Length;
        var (indexArg, value) = a.Get2();
        var relativeIndex = (long)indexArg.DoubleValue;
        long actualIndex = relativeIndex >= 0 ? relativeIndex : len + relativeIndex;

        if (actualIndex < 0 || actualIndex >= len)
            throw JSEngine.NewRangeError("Invalid index");

        // with creates a same-type array (TypedArrayCreateSameType), not @@species.
        var result = CreateSameTypeTypedArray(len);
        for (int i = 0; i < len; i++)
            result[(uint)i] = i == (int)actualIndex ? value : this[(uint)i];
        return result;
    }


    [JSExport("at", Length = 1)]
    public JSValue At(in Arguments a)
    {
        ValidateTypedArray("at");
        var len = Length;
        var relativeIndex = ToIntegerOrInfinity(a.Get1());
        long index = relativeIndex >= 0 ? relativeIndex : (long)len + relativeIndex;

        if (index < 0 || index >= len)
            return JSUndefined.Value;

        return this[(uint)index];
    }

    [JSExport("find", Length = 1)]
    public JSValue Find(in Arguments a)
    {
        ValidateTypedArray("find");
        var (callback, thisArg) = a.Get2();

        if (callback is not JSFunction fn)
            throw JSEngine.NewTypeError($"{callback} is not a function in Array.prototype.filter");

        var len = Length;
        for (int k = 0; k < len; k++)
        {
            var item = ReadElement(k);
            var itemParams = new Arguments(thisArg, item, new JSNumber(k), this);
            if (fn.InvokeCallback(itemParams).BooleanValue)
                return item;
        }
        return JSUndefined.Value;
    }

    [JSExport("findIndex", Length = 1)]
    public JSValue FindIndex(in Arguments a)
    {
        ValidateTypedArray("findIndex");
        var (callback, thisArg) = a.Get2();
        if (callback is not JSFunction fn)
            throw JSEngine.NewTypeError($"{callback} is not a function in Array.prototype.find");
        var len = Length;
        for (int k = 0; k < len; k++)
        {
            var index = new JSNumber(k);
            var itemParams = new Arguments(thisArg, ReadElement(k), index, this);
            if (fn.InvokeCallback(itemParams).BooleanValue)
                return index;
        }
        return JSNumber.MinusOne;
    }

    [JSExport("findLast", Length = 1)]
    public JSValue FindLast(in Arguments a)
    {
        ValidateTypedArray("findLast");
        var (callback, thisArg) = a.Get2();
        if (callback is not JSFunction fn)
            throw JSEngine.NewTypeError($"{callback} is not a function in %TypedArray%.prototype.findLast");
        for (int i = Length - 1; i >= 0; i--)
        {
            var item = this[(uint)i];
            if (fn.InvokeCallback(new Arguments(thisArg, item, new JSNumber(i), this)).BooleanValue)
                return item;
        }
        return JSUndefined.Value;
    }

    [JSExport("findLastIndex", Length = 1)]
    public JSValue FindLastIndex(in Arguments a)
    {
        ValidateTypedArray("findLastIndex");
        var (callback, thisArg) = a.Get2();
        if (callback is not JSFunction fn)
            throw JSEngine.NewTypeError($"{callback} is not a function in %TypedArray%.prototype.findLastIndex");
        for (int i = Length - 1; i >= 0; i--)
        {
            var item = this[(uint)i];
            if (fn.InvokeCallback(new Arguments(thisArg, item, new JSNumber(i), this)).BooleanValue)
                return new JSNumber(i);
        }
        return JSNumber.MinusOne;
    }

    [JSExport("forEach", Length = 1)]
    public JSValue ForEach(in Arguments a)
    {
        ValidateTypedArray("forEach");
        var (callback, thisArg) = a.Get2();
        if (callback is not JSFunction fn)
            throw JSEngine.NewTypeError($"{callback} is not a function in Array.prototype.find");
        var len = Length;
        for (int k = 0; k < len; k++)
        {
            var itemParams = new Arguments(thisArg, ReadElement(k), new JSNumber(k), this);
            fn.InvokeCallback(itemParams);
        }
        return JSUndefined.Value;
    }

    [JSExport("includes", Length = 1)]
    public JSValue Includes(in Arguments a)
    {
        ValidateTypedArray("includes");
        var (searchElement, fromIndex) = a.Get2();

        // The length is captured before fromIndex coercion (spec step 4: a zero-length array returns
        // false before ToIntegerOrInfinity runs). A fromIndex valueOf that resizes the buffer is then
        // observed only by the element reads below — indices left out of bounds read as undefined.
        var n = Length;
        if (n == 0)
            return JSBoolean.False;

        // ToIntegerOrInfinity (not ToInt32): a +Infinity fromIndex is past the end → false, and
        // -Infinity clamps to the start.
        var startIndex = ToIntegerOrInfinity(fromIndex);
        if (startIndex >= n)
            return JSBoolean.False;
        if (startIndex < 0)
        {
            // A negative fromIndex counts back from the end; below the start it clamps to 0.
            startIndex = n + startIndex;
            if (startIndex < 0)
                startIndex = 0;
        }

        for (var k = startIndex; k < n; k++)
        {
            // An index that became out of bounds (a resize during coercion) reads as undefined.
            var item = TryGetElement((uint)k, out var v) ? v : JSUndefined.Value;
            // includes uses SameValueZero, so NaN matches NaN (and +0/-0 are equal).
            if (item.SameValueZero(searchElement))
                return JSBoolean.True;
        }
        return JSBoolean.False;
    }

    [JSExport("indexOf", Length = 1)]
    public JSValue IndexOf(in Arguments a)
    {
        ValidateTypedArray("indexOf");
        var (searchElement, fromIndex) = a.Get2();
        var n = Length;
        if (n == 0)
        {
            return JSNumber.MinusOne;
        }
        var startIndex = fromIndex.AsInt32OrDefault();
        if (startIndex >= n)
        {
            return JSNumber.MinusOne;
        }
        if (startIndex < 0)
        {
            startIndex = n + startIndex;
            if (startIndex < 0)
            {
                startIndex = 0;
            }
        }
        var en = GetElementEnumerator(startIndex);
        while (en.MoveNext(out var hasValue, out var item, out var index))
        {
            if (!hasValue)
                continue;
            if (searchElement.StrictEquals(item))
                return new JSNumber(index);
        }
        return JSNumber.MinusOne;
    }

    [JSExport("join", Length = 1)]
    public JSValue Join(in Arguments a)
    {
        ValidateTypedArray("join");
        var first = a.Get1();
        var sep = first.IsUndefined ? "," : first.StringValue;
        var sb = new StringBuilder();
        bool isFirst = true;
        var en = GetElementEnumerator();
        while (en.MoveNext(out var item))
        {
            if (!isFirst)
            {
                sb.Append(sep);
            }
            else
            {
                isFirst = false;
            }
            if (item.IsUndefined)
                continue;
            sb.Append(item.ToString());
        }
        return new JSString(sb.ToString());
    }

    [JSExport("keys", Length = 0)]
    public new JSValue Keys(in Arguments a) { ValidateTypedArray("keys"); return GetArrayIterator(ArrayIteratorKind.Key); }

    [JSExport("lastIndexOf", Length = 1)]
    public JSValue LastIndexOf(in Arguments a)
    {
        ValidateTypedArray("lastIndexOf");
        var (element, fromIndex) = a.Get2();
        var n = Length;
        if (n == 0)
        {
            return JSNumber.MinusOne;
        }

        var startIndex = a.Length == 2 ? fromIndex.IntValue : int.MaxValue;
        if (startIndex >= n)
        {
            startIndex = n - 1;
        }
        if (startIndex < 0)
        {
            startIndex = n + startIndex;
        }

        var i = (uint)startIndex;

        while (i >= 0)
        {
            var item = this[i];
            if (item.StrictEquals(element))
                return new JSNumber(i);
            if (i == 0)
                break;
            i--;
        }
        return JSNumber.MinusOne;
    }

    [JSExport("map", Length = 1)]
    public JSValue Map(in Arguments a)
    {
        ValidateTypedArray("map");
        var (callback, thisArg) = a.Get2();
        if (callback is not JSFunction fn)
            throw JSEngine.NewTypeError($"{callback} is not a function in Array.prototype.find");
        var values = new List<JSValue>();
        var len = Length;
        for (int k = 0; k < len; k++)
        {
            var itemArgs = new Arguments(thisArg, ReadElement(k), new JSNumber(k), this);
            values.Add(fn.InvokeCallback(itemArgs));
        }

        var result = CreateTypedArrayFromConstructor(GetSpeciesConstructor(this), values.Count);
        for (uint i = 0; i < values.Count; i++)
            result[i] = values[(int)i];

        return result;
    }

    [JSExport("reduce", Length = 1)]
    public JSValue Reduce(in Arguments a)
    {
        ValidateTypedArray("reduce");
        var (callback, initialValue) = a.Get2();
        if (callback is not JSFunction fn)
            throw JSEngine.NewTypeError($"{callback} is not a function in Array.prototype.reduce");
        var len = Length;
        int k = 0;
        if (a.Length == 1)
        {
            if (len == 0)
                throw JSEngine.NewTypeError($"No initial value provided and array is empty");
            initialValue = ReadElement(k++);
        }
        for (; k < len; k++)
        {
            var itemArgs = new Arguments(JSUndefined.Value, initialValue, ReadElement(k), new JSNumber(k), this);
            initialValue = fn.InvokeCallback(itemArgs);
        }
        return initialValue;
    }

    [JSExport("reduceRight", Length = 1)]
    public JSValue ReduceRight(in Arguments a)
    {
        ValidateTypedArray("reduceRight");
        var r = new JSArray();

        var (callback, initialValue) = a.Get2();
        if (callback is not JSFunction fn)
            throw JSEngine.NewTypeError($"{callback} is not a function in Array.prototype.reduce");
        var start = Length - 1;
        if (a.Length == 1)
        {
            if (Length == 0)
                throw JSEngine.NewTypeError($"No initial value provided and array is empty");
            initialValue = this[(uint)start];
            start--;
        }
        for (int i = start; i >= 0; i--)
        {
            var item = this[(uint)i];
            var itemArgs = new Arguments(JSUndefined.Value, initialValue, item, new JSNumber(i), this);
            initialValue = fn.InvokeCallback(itemArgs);
        }
        return initialValue;
    }

    [JSExport("reverse", Length = 0)]
    public JSValue Reverse(in Arguments a)
    {
        ValidateTypedArray("reverse");
        var src = buffer.buffer;
        var temp = new byte[src.Length];
        System.Array.Copy(src, temp, src.Length);
        int bytesPerElement = this.bytesPerElement;
        int length = Length;
        for (int i = 0; i < length; i++)
        {
            var y = length - i - 1;
            System.Array.Copy(temp, byteOffset + (i * bytesPerElement),
                src,
                byteOffset + (y * bytesPerElement),
                bytesPerElement);
        }
        // Array.Copy(temp, src,src.Length);
        return this;
    }

    [JSExport("set", Length = 1)]
    public JSValue Set(in Arguments a)
    {
        ValidateTypedArray("set");
        var (source, offset) = a.Get2();
        var relativeStart = ToIntegerOrInfinity(offset);
        // The target length is captured before the source is measured: a source `length` getter (or a
        // length-tracking source) may resize the target's backing buffer, but the bounds check below
        // still uses the length the view had on entry.
        int length = Length;

        if (relativeStart < 0)
            throw JSEngine.NewRangeError("Offset is out of bounds");

        if (source is JSTypedArray typedArray)
        {
            // SetTypedArrayFromTypedArray: a source view left detached or out of bounds by a resize is a
            // TypeError (its element count can no longer be trusted).
            if (typedArray.buffer == null || typedArray.buffer.isDetached || typedArray.IsOutOfBounds)
                throw JSEngine.NewTypeError("TypedArray.prototype.set: the source TypedArray is detached or out of bounds");

            if ((long)typedArray.Length + relativeStart > length)
                throw JSEngine.NewRangeError("Offset is out of bounds");

            var src = typedArray.buffer.buffer;
            var target = buffer.buffer;
            int sourceBytesPerElement = typedArray.bytesPerElement;
            int targetBytesPerElement = bytesPerElement;
            // var maxLength = source.Length - (relativeStart * sourceBytesPerElement);
            if (src == target && (relativeStart * targetBytesPerElement) >= typedArray.byteOffset)
            {
                for (int i = source.Length - 1; i >= 0; i--)
                {
                    var y = relativeStart + i;
                    System.Array.Copy(src, typedArray.byteOffset + (i * sourceBytesPerElement),
                        target,
                        byteOffset + (y * targetBytesPerElement),
                        sourceBytesPerElement);
                }
            }
            else
            {
                for (int i = 0; i < source.Length; i++)
                {
                    var y = relativeStart + i;
                    System.Array.Copy(src, typedArray.byteOffset + (i * sourceBytesPerElement),
                        target,
                        byteOffset + (y * targetBytesPerElement),
                        sourceBytesPerElement);
                }
            }

            return JSValue.UndefinedValue;
        }

        // SetTypedArrayFromArrayLike: read source.length (a getter may resize the target's buffer),
        // bounds-check against the captured target length, then copy element by element via [[Get]]
        // (so a source Proxy / accessor source is observed correctly).
        var srcLength = (long)ToIntegerOrInfinity(source[KeyStrings.length]);
        if (srcLength + relativeStart > length)
            throw JSEngine.NewRangeError("Offset is out of bounds");

        var rs = (uint)relativeStart;
        for (long i = 0; i < srcLength; i++)
            this[rs + (uint)i] = source[JSValue.CreateNumber(i)];

        return JSValue.UndefinedValue;
    }

    [JSExport("slice", Length = 2)]
    public JSValue Slice(in Arguments a)
    {
        ValidateTypedArray("slice");
        // The source length is captured once, before coercion. ToIntegerOrInfinity coerces start/end
        // (valueOf, strings, booleans, NaN→0); an undefined end (explicit or absent) defaults to len.
        var srcLen = Length;
        var begin = ToIntegerOrInfinity(a.GetAt(0), 0);
        var end = ToIntegerOrInfinity(a.GetAt(1), srcLen);

        begin = begin < 0 ? Math.Max(srcLen + begin, 0) : Math.Min(begin, srcLen);
        end = end < 0 ? Math.Max(srcLen + end, 0) : Math.Min(end, srcLen);
        var count = (int)Math.Max(end - begin, 0);
        var startIndex = (int)begin;

        var r = CreateTypedArrayFromConstructor(GetSpeciesConstructor(this), count);

        if (count > 0)
        {
            // Coercing start/end (and creating the species result) may have detached or resized the
            // backing buffer: re-validate (an out-of-bounds view is a TypeError) and re-read the live
            // length, copying only the elements that are still in bounds (the rest stay zero).
            if (buffer == null || buffer.isDetached || IsOutOfBounds)
                throw JSEngine.NewTypeError("TypedArray.prototype.slice called on an out-of-bounds TypedArray");

            var n = Math.Min(count, Math.Max(Length - startIndex, 0));
            for (int i = 0; i < n; i++)
                r[(uint)i] = this[(uint)(startIndex + i)];
        }

        return r;
    }

    [JSExport("some", Length = 1)]
    public JSValue Some(in Arguments a)
    {
        ValidateTypedArray("some");
        var (callback, thisArg) = a.Get2();
        if (callback is not JSFunction fn)
            throw JSEngine.NewTypeError($"First argument is not function");
        var len = Length;
        for (int k = 0; k < len; k++)
        {
            var itemArgs = new Arguments(thisArg, ReadElement(k), new JSNumber(k), this);
            if (fn.InvokeCallback(itemArgs).BooleanValue)
                return JSBoolean.True;
        }
        return JSBoolean.False;
    }

    // SortCompare for %TypedArray%.prototype.sort with no comparefn: BigInt
    // element arrays compare as BigInt (their elements have no Number value), all
    // other arrays compare as Number with the spec's NaN-last / -0-before-+0 order.
    private static int DefaultSortCompare(JSValue l, JSValue r)
    {
        if (l.IsBigInt || r.IsBigInt)
            return l.AsBigIntegerOnly().CompareTo(r.AsBigIntegerOnly());

        var x = l.DoubleValue;
        var y = r.DoubleValue;
        if (x < y)
            return -1;
        if (x > y)
            return 1;
        if (double.IsNaN(x))
            return double.IsNaN(y) ? 0 : 1;
        if (double.IsNaN(y))
            return -1;
        if (JSNumber.IsNegativeZero(x) && JSNumber.IsPositiveZero(y))
            return -1;
        if (JSNumber.IsPositiveZero(x) && JSNumber.IsNegativeZero(y))
            return 1;
        return 0;
    }

    // Builds the comparison used by sort/toSorted. A user comparefn is consulted
    // through its sign only (ToNumber of the result, NaN treated as +0) so a
    // fractional or BigInt-array result still orders correctly.
    private Comparison<JSValue> BuildSortComparison(JSValue fx)
    {
        if (fx.IsUndefined)
            return DefaultSortCompare;

        return (l, r) =>
        {
            var v = fx.InvokeFunction(new Arguments(this, l, r)).DoubleValue;
            return double.IsNaN(v) ? 0 : Math.Sign(v);
        };
    }

    [JSExport("sort", Length = 1)]
    public JSValue Sort(in Arguments a)
    {
        var fx = a.Get1();
        if (!fx.IsUndefined && !fx.IsFunction)
            throw JSEngine.NewTypeError($"Argument is not a function");
        ValidateTypedArray("sort");

        var cx = BuildSortComparison(fx);

        var len = Length;
        var list = new List<JSValue>(len);
        for (int i = 0; i < len; i++)
            list.Add(this[(uint)i]);

        list.Sort(cx);

        // %TypedArray%.prototype.sort sorts in place and returns the same instance.
        for (int i = 0; i < len; i++)
            this[(uint)i] = list[i];

        return this;
    }

    [JSExport("subarray", Length = 2)]
    public JSValue SubArray(in Arguments a)
    {
        // subarray does NOT call ValidateTypedArray (it can be taken of an out-of-bounds view).
        // srcLength is captured *before* coercing start/end (spec MakeTypedArrayWithBufferWitnessRecord):
        // an out-of-bounds view has length 0, and a start/end valueOf that resizes the buffer is
        // observed only when the new view is created — so a shrink that leaves the requested extent
        // past the buffer end surfaces as a RangeError from the species constructor (not silently
        // clamped to the live length).
        var srcLength = IsOutOfBounds ? 0 : Length;

        // ToIntegerOrInfinity coerces start/end (valueOf, strings, booleans, NaN→0); an undefined end
        // (explicit or absent) defaults to srcLength, not 0.
        var begin = ToIntegerOrInfinity(a.GetAt(0), 0);
        var end = ToIntegerOrInfinity(a.GetAt(1), srcLength);

        begin = begin < 0 ? Math.Max(srcLength + begin, 0) : Math.Min(begin, srcLength);
        end = end < 0 ? Math.Max(srcLength + end, 0) : Math.Min(end, srcLength);
        var newLength = Math.Max(end - begin, 0);

        return GetSpeciesConstructor(this).CreateInstance(buffer, new JSNumber(byteOffset + begin * bytesPerElement), new JSNumber(newLength));
    }

    [JSExport("values", Length = 0)]
    [Symbol("@@iterator")]
    public new JSValue Values(in Arguments a) { ValidateTypedArray("values"); return GetArrayIterator(ArrayIteratorKind.Value); }

    [JSExport("toLocaleString", Length = 0)]
    internal JSValue ToLocaleString(in Arguments a)
    {
        ValidateTypedArray("toLocaleString");
        var (locale, format) = a.Get2();
        StringBuilder sb = new();

        // §23.2.3.32: the element list separator is the same implementation-defined
        // value Array.prototype.toLocaleString uses (a comma), so the two stay
        // consistent. Each element is formatted via its own toLocaleString(locale,
        // format) below, which handles a string .NET format or an Intl options bag
        // (so the typed array must not reject an options object up front).
        var separator = ",";

        bool first = true;
        var en = GetElementEnumerator();
        while (en.MoveNext(out var n))
        {
            if (!first)
            {
                //sb.Append(',');
                sb.Append(separator);
            }
            first = false;
            sb.Append(n.InvokeMethod(KeyStrings.GetOrCreate("toLocaleString"), locale, format).StringValue);
        }

        return new JSString(sb.ToString());
    }
}
