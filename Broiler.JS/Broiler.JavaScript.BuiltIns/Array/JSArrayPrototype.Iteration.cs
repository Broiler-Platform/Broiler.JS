using System;
using Broiler.JavaScript.BuiltIns.Boolean;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.BuiltIns.Generator;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.BuiltIns.Symbol;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.BuiltIns.Function;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Array;

public partial class JSArray
{
    private const double MaxArrayLikeLength = 9007199254740991d;

    private static JSObject ToArrayLikeObject(JSValue value)
    {
        if (value is JSObject @object)
            return @object;

        if (value.IsNullOrUndefined)
            throw JSEngine.NewTypeError(JSException.Cannot_convert_undefined_or_null_to_object);

        return (JSObject)JSObject.CreatePrimitiveObject(value);
    }

    private static JSValue ToNumberPrimitive(JSValue value)
    {
        if (value is not JSObject @object)
            return value;

        var toPrimitive = @object[(IJSSymbol)JSSymbol.toPrimitive];
        if (!toPrimitive.IsUndefined)
        {
            var primitive = toPrimitive.InvokeFunction(new Arguments(@object, JSConstants.Number));
            if (primitive.IsObject)
                throw JSEngine.NewTypeError("Cannot convert object to primitive value");

            return primitive;
        }

        if (@object[KeyStrings.valueOf] is IJSFunction valueOf)
        {
            var primitive = valueOf.InvokeFunction(new Arguments(@object));
            if (!primitive.IsObject)
                return primitive;
        }

        if (@object[KeyStrings.toString] is IJSFunction toString)
        {
            var primitive = toString.InvokeFunction(new Arguments(@object));
            if (!primitive.IsObject)
                return primitive;
        }

        throw JSEngine.NewTypeError("Cannot convert object to primitive value");
    }

    private static double ToNumber(JSValue value) => ToNumberPrimitive(value).DoubleValue;

    private static double ToLength(JSValue value)
    {
        if (value == null || value.IsUndefined)
            return 0;

        var length = ToNumber(value);
        if (double.IsNaN(length) || length <= 0)
            return 0;

        if (double.IsPositiveInfinity(length) || length >= MaxArrayLikeLength)
            return MaxArrayLikeLength;

        return Math.Floor(length);
    }

    private static long ToIntegerOrInfinity(JSValue value, long defaultValue = 0)
    {
        if (value == null || value.IsUndefined)
            return defaultValue;

        var number = ToNumber(value);
        if (double.IsNaN(number) || number == 0)
            return 0;

        if (double.IsPositiveInfinity(number))
            return long.MaxValue;

        if (double.IsNegativeInfinity(number))
            return long.MinValue;

        return (long)number;
    }

    private static uint GetArrayLikeLength(JSObject @object)
    {
        var length = ToLength(@object[KeyStrings.length]);
        return length >= uint.MaxValue
            ? uint.MaxValue
            : (uint)length;
    }

    private static long GetArrayLikeLengthLong(JSObject @object) => (long)ToLength(@object[KeyStrings.length]);

    private static JSObject CreateArraySpecies(JSObject source, uint length)
    {
        if (!IsArrayValue(source))
            return new JSArray(length);

        var constructor = source[KeyStrings.constructor];
        if (constructor.IsObject)
        {
            var species = constructor[(IJSSymbol)JSSymbol.species];
            if (species.IsNull)
                return new JSArray(length);

            if (!species.IsUndefined)
            {
                var created = species.CreateInstance(new Arguments(JSUndefined.Value, new JSNumber(length)));
                if (created is not JSObject createdObject)
                    throw JSEngine.NewTypeError("Array species constructor did not return an object");

                return createdObject;
            }
        }

        return new JSArray(length);
    }

    private static void CreateDataPropertyOrThrow(JSObject target, uint index, JSValue value)
    {
        var current = target.GetOwnPropertyDescriptor(JSValue.CreateNumber(index));
        if (current.IsUndefined)
        {
            if (!target.IsExtensible())
                throw JSEngine.NewTypeError($"Cannot add property {index} to {target}");
        }
        else if (!current[KeyStrings.configurable].BooleanValue)
        {
            throw JSEngine.NewTypeError("Cannot redefine property");
        }

        var descriptor = new JSObject();
        descriptor.FastAddValue(KeyStrings.value, value, JSPropertyAttributes.EnumerableConfigurableValue);
        descriptor.FastAddValue(KeyStrings.writable, JSBoolean.True, JSPropertyAttributes.EnumerableConfigurableValue);
        descriptor.FastAddValue(KeyStrings.enumerable, JSBoolean.True, JSPropertyAttributes.EnumerableConfigurableValue);
        descriptor.FastAddValue(KeyStrings.configurable, JSBoolean.True, JSPropertyAttributes.EnumerableConfigurableValue);
        target.DefineProperty(index, descriptor);
    }

    private struct ArrayLikeEntryEnumerator(JSObject @object, uint length) : IElementEnumerator
    {
        private int index = -1;

        public bool MoveNext(out JSValue value)
        {
            if (++index < length)
            {
                var entry = JSValue.CreateArray();
                entry.AddArrayItem(JSValue.CreateNumber(index));
                entry.AddArrayItem(@object[(uint)index]);
                value = entry;
                return true;
            }

            value = JSValue.UndefinedValue;
            return false;
        }

        public bool MoveNext(out bool hasValue, out JSValue value, out uint index)
        {
            if (MoveNext(out value))
            {
                hasValue = true;
                index = (uint)this.index;
                return true;
            }

            hasValue = false;
            index = 0;
            return false;
        }

        public bool MoveNextOrDefault(out JSValue value, JSValue @default)
        {
            if (MoveNext(out value))
                return true;

            value = @default;
            return false;
        }

        public JSValue NextOrDefault(JSValue @default) => MoveNext(out var value) ? value : @default;
    }

    [JSPrototypeMethod]
    [JSExport("every", Length = 1)]
    public static JSValue Every(in Arguments a)
    {
        var array = ToArrayLikeObject(a.This);
        var length = GetArrayLikeLength(array);
        var (first, thisArg) = a.Get2();

        if (first is not JSFunction fn)
            throw JSEngine.NewTypeError($"First argument is not function");

        for (uint index = 0; index < length; index++)
        {
            if (!array.TryGetElement(index, out var item))
                continue;

            var itemArgs = new Arguments(thisArg, item, new JSNumber(index), array);
            if (!fn.f(itemArgs).BooleanValue)
                return JSBoolean.False;
        }

        return JSBoolean.True;
    }

    [JSPrototypeMethod]
    [JSExport("entries")]
    public new static JSValue Entries(in Arguments a)
    {
        var array = ToArrayLikeObject(a.This);
        var length = GetArrayLikeLength(array);
        return new JSGenerator(new ArrayLikeEntryEnumerator(array, length), "Array Iterator");
    }

    [JSPrototypeMethod]
    [JSExport("filter", Length = 1)]
    public static JSValue Filter(in Arguments a)
    {
        var @this = ToArrayLikeObject(a.This);
        var length = GetArrayLikeLength(@this);
        var (callback, thisArg) = a.Get2();

        if (callback is not JSFunction fn)
            throw JSEngine.NewTypeError($"{callback} is not a function in Array.prototype.filter");

        var r = CreateArraySpecies(@this, 0);
        uint resultIndex = 0;

        for (uint index = 0; index < length; index++)
        {
            if (!@this.TryGetElement(index, out var item))
                continue;

            var itemParams = new Arguments(thisArg, item, new JSNumber(index), @this);

            if (fn.f(itemParams).BooleanValue)
                CreateDataPropertyOrThrow(r, resultIndex++, item);
        }
        return r;
    }

    [JSPrototypeMethod]
    [JSExport("find", Length = 1)]
    public static JSValue Find(in Arguments a)
    {
        var @this = ToArrayLikeObject(a.This);
        var length = GetArrayLikeLength(@this);
        var (callback, thisArg) = a.Get2();

        if (callback is not JSFunction fn)
            throw JSEngine.NewTypeError($"{callback} is not a function in Array.prototype.find");

        for (uint index = 0; index < length; index++)
        {
            if (!@this.TryGetElement(index, out var item))
                continue;

            var itemParams = new Arguments(thisArg, item, new JSNumber(index), @this);
            if (fn.f(itemParams).BooleanValue)
                return item;
        }

        return JSUndefined.Value;
    }

    /// <summary>
    /// Creates a new array with all sub-array elements concatenated into it recursively up to
    /// the specified depth.
    /// </summary>
    /// <param name="thisObj"> The array that is being operated on. </param>
    /// <param name="depth"> The depth level specifying how deep a nested array structure
    /// should be flattened. Defaults to 1. </param>
    /// <returns> A new array with the sub-array elements concatenated into it. </returns>
    [JSPrototypeMethod]
    [JSExport("flat", Length = 0)]
    public static JSValue Flat(in Arguments a)
    {
        var @this = ToArrayLikeObject(a.This);
        var result = CreateArraySpecies(@this, 0);
        int depth = a[0]?.IntegerValue ?? 1;
        uint resultIndex = 0;
        FlattenTo(result, @this, null, null, depth, ref resultIndex);
        return result;
    }

    /// <summary>
    /// Maps each element using a mapping function, then flattens the result into a new array.
    /// </summary>
    /// <param name="thisObj"> The array that is being operated on. </param>
    /// <param name="callback"> A function that produces an element of the new Array, taking
    /// three arguments: currentValue, index, array. </param>
    /// <param name="thisArg"> Value to use as this when executing callback. </param>
    /// <returns> A new array with each element being the result of the callback function and
    /// flattened to a depth of 1. </returns>
    [JSPrototypeMethod]
    [JSExport("flatMap", Length = 1)]
    public static JSValue FlatMap(in Arguments a)
    {
        var @this = ToArrayLikeObject(a.This);
        var result = CreateArraySpecies(@this, 0);
        int depth = 1;
        var (callback, thisArg) = a.Get2();
        uint resultIndex = 0;
        FlattenTo(result, @this, callback, thisArg, depth, ref resultIndex);
        return result;
    }

    private static void FlattenTo(JSObject result, JSObject @this, JSValue callback, JSValue thisArg, int depth, ref uint resultIndex)
    {
        var length = GetArrayLikeLength(@this);
        for (uint i = 0; i < length; i++)
        {
            // TryGetElement - to check for holes in array
            if (@this.TryGetElement(i, out var elementValue))
            {
                // Transform the value using the mapping function.
                if (callback != null)
                    elementValue = callback.InvokeFunction(new Arguments(thisArg, elementValue, new JSNumber(i), @this));

                // If the element is an array, flatten it.
                if (depth > 0 && elementValue is JSArray childArray)
                    FlattenTo(result, childArray, callback, thisArg, depth - 1, ref resultIndex);
                else
                    CreateDataPropertyOrThrow(result, resultIndex++, elementValue);
            }
        }
    }

    [JSPrototypeMethod]
    [JSExport("findIndex", Length = 1)]
    public static JSValue FindIndex(in Arguments a)
    {
        var @this = ToArrayLikeObject(a.This);
        var length = GetArrayLikeLength(@this);
        var (callback, thisArg) = a.Get2();

        if (callback is not JSFunction fn)
            throw JSEngine.NewTypeError($"{callback} is not a function in Array.prototype.find");

        for (uint n = 0; n < length; n++)
        {
            if (!@this.TryGetElement(n, out var item))
                continue;

            var index = new JSNumber(n);
            var itemParams = new Arguments(thisArg, item, index, @this);

            if (fn.f(itemParams).BooleanValue)
                return index;
        }

        return JSNumber.MinusOne;
    }

    [JSPrototypeMethod]
    [JSExport("findLast", Length = 1)]
    public static JSValue FindLast(in Arguments a)
    {
        var @this = ToArrayLikeObject(a.This);
        var length = GetArrayLikeLength(@this);
        var (callback, thisArg) = a.Get2();

        if (callback is not JSFunction fn)
            throw JSEngine.NewTypeError($"{callback} is not a function in Array.prototype.findLast");

        for (var n = (long)length - 1; n >= 0; n--)
        {
            if (!@this.TryGetElement((uint)n, out var item))
                continue;

            var index = new JSNumber(n);
            var itemParams = new Arguments(thisArg, item, index, @this);
            if (fn.f(itemParams).BooleanValue)
                return item;
        }

        return JSUndefined.Value;
    }

    [JSPrototypeMethod]
    [JSExport("findLastIndex", Length = 1)]
    public static JSValue FindLastIndex(in Arguments a)
    {
        var @this = ToArrayLikeObject(a.This);
        var length = GetArrayLikeLength(@this);
        var (callback, thisArg) = a.Get2();

        if (callback is not JSFunction fn)
            throw JSEngine.NewTypeError($"{callback} is not a function in Array.prototype.findLastIndex");

        for (var n = (long)length - 1; n >= 0; n--)
        {
            if (!@this.TryGetElement((uint)n, out var item))
                continue;

            var index = new JSNumber(n);
            var itemParams = new Arguments(thisArg, item, index, @this);
            if (fn.f(itemParams).BooleanValue)
                return index;
        }

        return JSNumber.MinusOne;
    }

    [JSPrototypeMethod]
    [JSExport("forEach", Length = 1)]
    public static JSValue ForEach(in Arguments a)
    {
        var @this = ToArrayLikeObject(a.This);
        var length = GetArrayLikeLength(@this);
        var (callback, thisArg) = a.Get2();

        if (callback is not JSFunction fn)
            throw JSEngine.NewTypeError($"{callback} is not a function in Array.prototype.find");

        for (uint index = 0; index < length; index++)
        {
            if (!@this.TryGetElement(index, out var item))
                continue;

            var n = new JSNumber(index);
            var itemParams = new Arguments(thisArg, item, n, @this);

            fn.f(itemParams);
        }

        return JSUndefined.Value;
    }

    [JSPrototypeMethod]
    [JSExport("keys")]
    public new static JSValue Keys(in Arguments a)
    {
        var @this = ToArrayLikeObject(a.This);
        var length = GetArrayLikeLength(@this);
        return new JSGenerator(new IntKeyEnumerator((int)length), "Array Iterator");
    }

    [JSPrototypeMethod]
    [JSExport("map", Length = 1)]
    public static JSValue Map(in Arguments a)
    {
        var @this = ToArrayLikeObject(a.This);
        var length = GetArrayLikeLength(@this);
        var (callback, thisArg) = a.Get2();
        if (callback is not JSFunction fn)
            throw JSEngine.NewTypeError($"{callback} is not a function in Array.prototype.find");

        var r = CreateArraySpecies(@this, length);

        for (uint index = 0; index < length; index++)
        {
            if (!@this.TryGetElement(index, out var item))
                continue;

            var itemArgs = new Arguments(thisArg, item, new JSNumber(index), @this);
            CreateDataPropertyOrThrow(r, index, fn.f(itemArgs));
        }

        return r;
    }

    [JSPrototypeMethod]
    [JSExport("reduce", Length = 1)]
    public static JSValue Reduce(in Arguments a)
    {
        var r = new JSArray();
        var @this = a.This;
        var (callback, initialValue) = a.Get2();

        if (callback is not JSFunction fn)
            throw JSEngine.NewTypeError($"{callback} is not a function in Array.prototype.reduce");

        var en = @this.GetElementEnumerator();
        uint index = 0;

        if (a.Length == 1)
        {
            if (!en.MoveNext(out initialValue))
                throw JSEngine.NewTypeError($"No initial value provided and array is empty");
        }

        while (en.MoveNext(out var hasValue, out var item, out index))
        {
            if (!hasValue)
                continue;

            var itemArgs = new Arguments(JSUndefined.Value, initialValue, item, new JSNumber(index), @this);
            initialValue = fn.f(itemArgs);
        }

        return initialValue;
    }

    [JSPrototypeMethod]
    [JSExport("reduceRight", Length = 1)]
    public static JSValue ReduceRight(in Arguments a)
    {
        var r = new JSArray();
        var @this = a.This;
        var (callback, initialValue) = a.Get2();

        if (callback is not JSFunction fn)
            throw JSEngine.NewTypeError($"{callback} is not a function in Array.prototype.reduce");

        var start = @this.Length - 1;

        if (a.Length == 1)
        {
            if (@this.Length == 0)
                throw JSEngine.NewTypeError($"No initial value provided and array is empty");

            initialValue = @this[(uint)start];
            start--;
        }

        for (int i = start; i >= 0; i--)
        {
            var item = @this[(uint)i];
            var itemArgs = new Arguments(JSUndefined.Value, initialValue, item, new JSNumber(i), @this);
            initialValue = fn.f(itemArgs);
        }

        return initialValue;
    }

    [JSPrototypeMethod]
    [JSExport("some", Length = 1)]
    public static JSValue Some(in Arguments a)
    {
        var array = a.This;
        var (first, thisArg) = a.Get2();

        if (first is not JSFunction fn)
            throw JSEngine.NewTypeError($"First argument is not function");

        var en = array.GetElementEnumerator();

        while (en.MoveNext(out var hasValue, out var item, out var index))
        {
            if (!hasValue)
                continue;

            var itemArgs = new Arguments(thisArg, item, new JSNumber(index), array);

            if (fn.f(itemArgs).BooleanValue)
                return JSBoolean.True;
        }

        return JSBoolean.False;
    }

    [JSPrototypeMethod]
    [JSExport("values", Length = 2)]
    [Symbol("@@iterator")]
    public new static JSValue Values(in Arguments a) => new JSGenerator(a.This.GetElementEnumerator(), "Array Iterator");

}
