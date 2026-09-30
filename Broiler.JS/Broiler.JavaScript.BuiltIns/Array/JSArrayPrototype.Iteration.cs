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

// Broiler-AI:           Origin=AI; IP=Medium; Security=Medium; Resources=8; Fingerprint=TBF
// Broiler-Falsified-If: flat(Infinity) on an array that contains itself ends the process with a stack overflow instead of raising a catchable error
// Broiler-Human:        PENDING
public partial class JSArray
{
    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=0; Fingerprint=TBF
    // Broiler-Falsified-If: the constant differs from 2^53-1, so a push that would take an array-like past 9007199254740991 is not rejected with a TypeError
    // Broiler-Human:        PENDING
    private const double MaxArrayLikeLength = 9007199254740991d;

    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=1; Fingerprint=TBF
    // Broiler-Falsified-If: a null or undefined receiver is returned or boxed instead of raising a TypeError
    // Broiler-Human:        PENDING
    private static JSObject ToArrayLikeObject(JSValue value)
    {
        if (value is JSObject @object)
            return @object;

        if (value.IsNullOrUndefined)
            throw JSEngine.NewTypeError(JSException.Cannot_convert_undefined_or_null_to_object);

        return (JSObject)CreatePrimitiveObject(value);
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=1; Fingerprint=TBF
    // Broiler-Falsified-If: an @@toPrimitive method is called with a hint other than number, or its object result is accepted instead of raising a TypeError
    // Broiler-Human:        PENDING
    private static JSValue ToNumberPrimitive(JSValue value)
    {
        if (value is not JSObject @object)
            return value;

        var toPrimitive = @object[(IJSSymbol)JSSymbol.toPrimitive];
        if (!toPrimitive.IsUndefined && !toPrimitive.IsNull)
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

    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=0; Fingerprint=TBF
    // Broiler-Falsified-If: a Symbol value is returned for string conversion instead of raising a TypeError
    // Broiler-Human:        PENDING
    private static JSValue ThrowIfSymbolToString(JSValue value)
    {
        if (value.IsSymbol)
            throw JSEngine.NewTypeError("Cannot convert a Symbol value to a string");

        return value;
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=1; Fingerprint=TBF
    // Broiler-Falsified-If: for an object without @@toPrimitive, valueOf is called before toString
    // Broiler-Human:        PENDING
    private static JSValue ToStringPrimitive(JSValue value)
    {
        if (!value.IsObject)
            return ThrowIfSymbolToString(value);

        var @object = (JSObject)value;
        var toPrimitive = @object[(IJSSymbol)JSSymbol.toPrimitive];
        if (!toPrimitive.IsUndefined && !toPrimitive.IsNull)
        {
            var primitive = toPrimitive.InvokeFunction(new Arguments(@object, JSConstants.String));
            if (primitive.IsObject)
                throw JSEngine.NewTypeError("Cannot convert object to primitive value");

            return ThrowIfSymbolToString(primitive);
        }

        if (@object[KeyStrings.toString] is IJSFunction toString)
        {
            var primitive = toString.InvokeFunction(new Arguments(@object));
            if (!primitive.IsObject)
                return ThrowIfSymbolToString(primitive);
        }

        if (@object[KeyStrings.valueOf] is IJSFunction valueOf)
        {
            var primitive = valueOf.InvokeFunction(new Arguments(@object));
            if (!primitive.IsObject)
                return ThrowIfSymbolToString(primitive);
        }

        throw JSEngine.NewTypeError("Cannot convert object to primitive value");
    }

    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=1; Fingerprint=TBF
    // Broiler-Falsified-If: a Symbol or BigInt argument converts to a number instead of raising a TypeError
    // Broiler-Human:        PENDING
    private static double ToNumber(JSValue value) => ToNumberPrimitive(value).DoubleValue;

    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=1; Fingerprint=TBF
    // Broiler-Falsified-If: a length of 2^60 or +Infinity is returned above 9007199254740991 instead of being clamped to it
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=1; Fingerprint=TBF
    // Broiler-Falsified-If: a finite argument beyond the long range such as 1e300 comes back negative or wrapped instead of saturating at long.MaxValue
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=1; Fingerprint=TBF
    // Broiler-Falsified-If: a length property of 2^32 or more is returned as a wrapped small count instead of uint.MaxValue
    // Broiler-Human:        PENDING
    private static uint GetArrayLikeLength(JSObject @object)
    {
        var length = ToLength(@object[KeyStrings.length]);
        return length >= uint.MaxValue
            ? uint.MaxValue
            : (uint)length;
    }

    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=1; Fingerprint=TBF
    // Broiler-Falsified-If: a length property above 2^53-1 is returned larger than 9007199254740991
    // Broiler-Human:        PENDING
    private static long GetArrayLikeLengthLong(JSObject @object) => (long)ToLength(@object[KeyStrings.length]);

    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=1; Fingerprint=TBF
    // Broiler-Falsified-If: a species constructor that returns a primitive has that primitive used as the result instead of raising a TypeError
    // Broiler-Human:        PENDING
    private static JSObject CreateArraySpecies(JSObject source, long length)
    {
        if (!IsArrayValue(source))
            return ArrayCreateChecked(length);

        var constructor = source[KeyStrings.constructor];
        if (constructor.IsUndefined)
            return ArrayCreateChecked(length);

        if (!constructor.IsObject)
            throw JSEngine.NewTypeError("Array constructor is not an object");

        var species = constructor[(IJSSymbol)JSSymbol.species];
        if (species.IsNull || species.IsUndefined)
            return ArrayCreateChecked(length);

        if (!species.IsFunction)
            throw JSEngine.NewTypeError("Array species constructor is not a constructor");

        // ArraySpeciesCreate step 8: Construct(C, « 𝔽(length) »). The length is passed to the
        // custom constructor as a Number (up to 2^53-1) — it is NOT clamped to the 2^32 array-
        // index limit here; only the default ArrayCreate path (above) enforces that limit.
        var created = species.CreateInstance(new Arguments(JSUndefined.Value, CreateNumber((double)length)));
        if (created is not JSObject createdObject)
            throw JSEngine.NewTypeError("Array species constructor did not return an object");

        return createdObject;
    }

    // ArrayCreate(length): the default array constructor, which rejects a length above the
    // 2^32-1 array-index limit with a RangeError.
    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=2; Fingerprint=TBF
    // Broiler-Falsified-If: a length of exactly 2^32 is accepted and wraps to an empty array instead of raising a RangeError
    // Broiler-Human:        PENDING
    private static JSArray ArrayCreateChecked(long length)
    {
        if (length < 0 || length > uint.MaxValue)
            throw JSEngine.NewRangeError("Invalid array length");

        return new JSArray((uint)length);
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=1; Fingerprint=TBF
    // Broiler-Falsified-If: a Proxy defineProperty trap that returns false leaves the call returning normally instead of raising a TypeError
    // Broiler-Human:        PENDING
    private static void CreateDataPropertyOrThrow(JSObject target, uint index, JSValue value)
    {
        // CreateDataProperty(O, P, V) is O.[[DefineOwnProperty]](P, {value, writable,
        // enumerable, configurable: all true}); the extensibility / non-configurable
        // checks are performed inside [[DefineOwnProperty]] (OrdinaryDefineOwnProperty for
        // an ordinary target, the defineProperty trap for a Proxy). Probing
        // GetOwnPropertyDescriptor and IsExtensible up front would fire extra Proxy traps
        // the spec never invokes (test262 Array splice/reverse length-exceeding-with-proxy).
        var descriptor = new JSObject();
        descriptor.FastAddValue(KeyStrings.value, value, JSPropertyAttributes.EnumerableConfigurableValue);
        descriptor.FastAddValue(KeyStrings.writable, JSBoolean.True, JSPropertyAttributes.EnumerableConfigurableValue);
        descriptor.FastAddValue(KeyStrings.enumerable, JSBoolean.True, JSPropertyAttributes.EnumerableConfigurableValue);
        descriptor.FastAddValue(KeyStrings.configurable, JSBoolean.True, JSPropertyAttributes.EnumerableConfigurableValue);
        var result = target.DefineProperty(index, descriptor);
        if (result.IsBoolean && !result.BooleanValue)
            throw JSEngine.NewTypeError($"Cannot define property {index}");
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=1; Fingerprint=TBF
    // Broiler-Falsified-If: an index present only on the prototype chain is reported absent
    // Broiler-Human:        PENDING
    private static bool TryGetArrayLikeElement(JSObject @object, uint index, out JSValue value)
    {
        if (!HasIndexedProperty(@object, index))
        {
            value = JSUndefined.Value;
            return false;
        }

        value = GetIndexedValue(@object, index);
        return true;
    }

    // 64-bit-index overload for array-likes whose length exceeds the 32-bit array
    // index range (up to 2^53-1). Indices past uint.MaxValue are addressed by their
    // canonical numeric property key via the long Has/Get overloads.
    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=1; Fingerprint=TBF
    // Broiler-Falsified-If: an index of 2^32 or above stored under its numeric property key is reported absent
    // Broiler-Human:        PENDING
    private static bool TryGetArrayLikeElement(JSObject @object, long index, out JSValue value)
    {
        if (!HasIndexedProperty(@object, index))
        {
            value = JSUndefined.Value;
            return false;
        }

        value = GetIndexedValue(@object, index);
        return true;
    }

    // Length-bound value iterator (CreateArrayIterator, "value" kind) for generic
    // array-likes such as a mapped arguments object: the length is re-read on every
    // step from the receiver's "length" property, so shrinking it (arguments.length = 2)
    // ends iteration early and growing it keeps later elements reachable. Real arrays /
    // typed arrays use their own (hole-aware) element enumerator instead.
    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=1; Fingerprint=TBF
    // Broiler-Falsified-If: iterating an array-like whose length is 2^32-1 or more never ends, because the int step counter wraps and restarts at index 0
    // Broiler-Human:        PENDING
    private struct ArrayLikeValueEnumerator(JSObject @object) : IElementEnumerator
    {
        private int index = -1;

        // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=1; Fingerprint=TBF
        // Broiler-Falsified-If: shrinking the receiver length to 2 mid-iteration still yields the element stored at index 2
        // Broiler-Human:        PENDING
        public bool MoveNext(out JSValue value)
        {
            if (++index < GetArrayLikeLength(@object))
            {
                value = @object[(uint)index];
                return true;
            }

            value = UndefinedValue;
            return false;
        }

        // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=1; Fingerprint=TBF
        // Broiler-Falsified-If: a successful step reports an index different from the position whose value it returned
        // Broiler-Human:        PENDING
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

        // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=1; Fingerprint=TBF
        // Broiler-Falsified-If: an exhausted iterator leaves something other than the supplied default in the out value
        // Broiler-Human:        PENDING
        public bool MoveNextOrDefault(out JSValue value, JSValue @default)
        {
            if (MoveNext(out value))
                return true;

            value = @default;
            return false;
        }

        // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=1; Fingerprint=TBF
        // Broiler-Falsified-If: an exhausted iterator returns something other than the supplied default
        // Broiler-Human:        PENDING
        public JSValue NextOrDefault(JSValue @default) => MoveNext(out var value) ? value : @default;
    }

    // Length-bound key iterator (CreateArrayIterator, "key" kind). Like the value and
    // entry variants the length is re-read on every step, so an index appended after
    // the iterator is created (but before it is exhausted) remains reachable and an
    // index removed by shrinking "length" ends iteration early.
    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=1; Fingerprint=TBF
    // Broiler-Falsified-If: after 2^31 steps the int counter wraps and a negative key such as -2147483648 is yielded instead of 2147483648
    // Broiler-Human:        PENDING
    private struct ArrayLikeKeyEnumerator(JSObject @object) : IElementEnumerator
    {
        private int index = -1;

        // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=1; Fingerprint=TBF
        // Broiler-Falsified-If: after 2^31 steps the int counter wraps and a negative key such as -2147483648 is yielded instead of 2147483648
        // Broiler-Human:        PENDING
        public bool MoveNext(out JSValue value)
        {
            if (++index < GetArrayLikeLength(@object))
            {
                value = CreateNumber(index);
                return true;
            }

            value = UndefinedValue;
            return false;
        }

        // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=1; Fingerprint=TBF
        // Broiler-Falsified-If: a successful step reports an index different from the position whose value it returned
        // Broiler-Human:        PENDING
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

        // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=1; Fingerprint=TBF
        // Broiler-Falsified-If: an exhausted iterator leaves something other than the supplied default in the out value
        // Broiler-Human:        PENDING
        public bool MoveNextOrDefault(out JSValue value, JSValue @default)
        {
            if (MoveNext(out value))
                return true;

            value = @default;
            return false;
        }

        // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=1; Fingerprint=TBF
        // Broiler-Falsified-If: an exhausted iterator returns something other than the supplied default
        // Broiler-Human:        PENDING
        public JSValue NextOrDefault(JSValue @default) => MoveNext(out var value) ? value : @default;
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=1; Fingerprint=TBF
    // Broiler-Falsified-If: an entry carries a key that is not the index its value was read from, as happens once the int counter passes 2^31 and turns negative
    // Broiler-Human:        PENDING
    private struct ArrayLikeEntryEnumerator(JSObject @object) : IElementEnumerator
    {
        private int index = -1;

        // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=1; Fingerprint=TBF
        // Broiler-Falsified-If: an entry carries a key that is not the index its value was read from, as happens once the int counter passes 2^31 and turns negative
        // Broiler-Human:        PENDING
        public bool MoveNext(out JSValue value)
        {
            // Array iterators are live: per CreateArrayIterator the length is
            // re-read on every step, so elements appended after the iterator is
            // created (but before it is exhausted) remain reachable.
            if (++index < GetArrayLikeLength(@object))
            {
                var entry = CreateArray();
                entry.AddArrayItem(CreateNumber(index));
                entry.AddArrayItem(@object[(uint)index]);
                value = entry;
                return true;
            }

            value = UndefinedValue;
            return false;
        }

        // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=1; Fingerprint=TBF
        // Broiler-Falsified-If: a successful step reports an index different from the position whose value it returned
        // Broiler-Human:        PENDING
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

        // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=1; Fingerprint=TBF
        // Broiler-Falsified-If: an exhausted iterator leaves something other than the supplied default in the out value
        // Broiler-Human:        PENDING
        public bool MoveNextOrDefault(out JSValue value, JSValue @default)
        {
            if (MoveNext(out value))
                return true;

            value = @default;
            return false;
        }

        // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=1; Fingerprint=TBF
        // Broiler-Falsified-If: an exhausted iterator returns something other than the supplied default
        // Broiler-Human:        PENDING
        public JSValue NextOrDefault(JSValue @default) => MoveNext(out var value) ? value : @default;
    }

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=7; Fingerprint=TBF
    // Broiler-Falsified-If: a callable Proxy wrapping a function is rejected with a TypeError instead of being called for each element
    // Broiler-Human:        PENDING
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
            // Per spec every uses HasProperty(O, k) then Get(O, k), so an index
            // inherited from the prototype chain (e.g. a boxed primitive whose
            // wrapper prototype defines indexed properties) must be visited too —
            // not just own elements.
            if (!TryGetArrayLikeElement(array, index, out var item))
                continue;

            var itemArgs = new Arguments(thisArg, item, new JSNumber(index), array);
            if (!fn.InvokeCallback(itemArgs).BooleanValue)
                return JSBoolean.False;
        }

        return JSBoolean.True;
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=1; Fingerprint=TBF
    // Broiler-Falsified-If: a typed-array receiver gets the generic length-bound iterator instead of the typed-array iterator that throws once the view is out of bounds
    // Broiler-Human:        PENDING
    [JSPrototypeMethod]
    [JSExport("entries")]
    public new static JSValue Entries(in Arguments a)
    {
        if (a.This is Typed.JSTypedArray taE)
            return taE.GetArrayIterator(Typed.JSTypedArray.ArrayIteratorKind.Entry);
        var array = ToArrayLikeObject(a.This);
        return new JSGenerator(new ArrayLikeEntryEnumerator(array), "Array Iterator");
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=7; Fingerprint=TBF
    // Broiler-Falsified-If: an element kept by the callback is written at a result index other than the count of elements kept before it
    // Broiler-Human:        PENDING
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
            // Per spec filter uses HasProperty(O, k) then Get(O, k), so an index
            // inherited from the prototype chain (e.g. a boxed primitive whose
            // wrapper prototype defines indexed properties) must be visited too.
            if (!TryGetArrayLikeElement(@this, index, out var item))
                continue;

            var itemParams = new Arguments(thisArg, item, new JSNumber(index), @this);

            if (fn.InvokeCallback(itemParams).BooleanValue)
                CreateDataPropertyOrThrow(r, resultIndex++, item);
        }
        return r;
    }

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=7; Fingerprint=TBF
    // Broiler-Falsified-If: a hole in the array is skipped instead of being passed to the predicate as undefined
    // Broiler-Human:        PENDING
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
            // find visits every index (holes read as undefined); it does not skip holes.
            var item = @this[index];

            var itemParams = new Arguments(thisArg, item, new JSNumber(index), @this);
            if (fn.InvokeCallback(itemParams).BooleanValue)
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
    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=8; Fingerprint=TBF
    // Broiler-Falsified-If: an explicit undefined depth flattens a different number of levels than an omitted depth
    // Broiler-Human:        PENDING
    [JSPrototypeMethod]
    [JSExport("flat", Length = 0)]
    public static JSValue Flat(in Arguments a)
    {
        var @this = ToArrayLikeObject(a.This);
        // Spec order: LengthOfArrayLike (read "length") first, then coerce the depth
        // argument, then ArraySpeciesCreate (read "constructor") — test262
        // flat/proxy-access-count.
        var length = GetArrayLikeLength(@this);
        // FlattenIntoArray: depth defaults to 1 and is coerced (ToIntegerOrInfinity) only
        // when the argument is supplied — an explicit `undefined` keeps the default, like an
        // omitted argument (test262 flat/non-numeric-depth-should-not-throw). A negative or
        // other non-positive depth means "do not flatten", handled by FlattenTo's depth > 0 gate.
        var depthArg = a[0];
        int depth = depthArg == null || depthArg.IsUndefined ? 1 : depthArg.IntegerValue;
        var result = CreateArraySpecies(@this, 0);
        uint resultIndex = 0;
        FlattenTo(result, @this, null, null, depth, ref resultIndex, length);
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
    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=7; Fingerprint=TBF
    // Broiler-Falsified-If: an array returned by the mapper is mapped again or flattened by more than one level
    // Broiler-Human:        PENDING
    [JSPrototypeMethod]
    [JSExport("flatMap", Length = 1)]
    public static JSValue FlatMap(in Arguments a)
    {
        var @this = ToArrayLikeObject(a.This);
        var length = GetArrayLikeLength(@this);
        var (callback, thisArg) = a.Get2();
        if (callback is not JSFunction fn)
            throw JSEngine.NewTypeError($"{callback} is not a function in Array.prototype.flatMap");

        var result = CreateArraySpecies(@this, 0);
        uint resultIndex = 0;
        FlattenTo(result, @this, fn, thisArg, 1, ref resultIndex, length);
        return result;
    }

    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=8; Fingerprint=TBF
    // Broiler-Falsified-If: a nested array's length is not read from that nested array, so a nested Proxy's length trap never fires
    // Broiler-Human:        PENDING
    private static void FlattenTo(JSObject result, JSObject @this, JSValue callback, JSValue thisArg, int depth, ref uint resultIndex)
        => FlattenTo(result, @this, callback, thisArg, depth, ref resultIndex, GetArrayLikeLength(@this));

    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=8; Fingerprint=TBF
    // Broiler-Falsified-If: a self-containing array flattened with depth Infinity recurses until the process dies with a stack overflow instead of raising a catchable RangeError
    // Broiler-Human:        PENDING
    private static void FlattenTo(JSObject result, JSObject @this, JSValue callback, JSValue thisArg, int depth, ref uint resultIndex, uint length)
    {
        for (uint i = 0; i < length; i++)
        {
            // FlattenIntoArray: HasProperty then Get, so holes are skipped and exotic
            // sources (a Proxy / array-like) are observed through their has/get traps
            // rather than only their dense own elements (test262 flat/proxy-access-count).
            if (!@this.HasProperty(CreateString(i.ToString())).BooleanValue)
                continue;

            var elementValue = @this[i];

            // Transform the value using the mapping function.
            if (callback != null)
                elementValue = callback.InvokeFunction(new Arguments(thisArg, elementValue, new JSNumber(i), @this));

            // If the element is an array, flatten it. FlattenIntoArray uses the
            // IsArray abstract operation, so a Proxy whose target is an array is
            // flattened too and is observed through its length/index get traps
            // (test262 flat/flatMap proxy-access-count). The mapper (flatMap) is
            // applied only to the source's own elements — FlattenIntoArray recurses
            // with the mapperFunction absent — so a mapped element that is itself an
            // array is flattened, not re-mapped.
            if (depth > 0 && elementValue is JSObject childArray && IsArrayValue(childArray))
                FlattenTo(result, childArray, null, null, depth - 1, ref resultIndex);
            else
                CreateDataPropertyOrThrow(result, resultIndex++, elementValue);
        }
    }

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=7; Fingerprint=TBF
    // Broiler-Falsified-If: a hole in the array is skipped instead of being passed to the predicate as undefined
    // Broiler-Human:        PENDING
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
            // findIndex visits every index (holes read as undefined); it does not skip holes.
            var item = @this[n];

            var index = new JSNumber(n);
            var itemParams = new Arguments(thisArg, item, index, @this);

            if (fn.InvokeCallback(itemParams).BooleanValue)
                return index;
        }

        return JSNumber.MinusOne;
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=7; Fingerprint=TBF
    // Broiler-Falsified-If: on an array-like of length 2^32 the value stored at index 4294967295 reaches the predicate as undefined
    // Broiler-Human:        PENDING
    [JSPrototypeMethod]
    [JSExport("findLast", Length = 1)]
    public static JSValue FindLast(in Arguments a)
    {
        var @this = ToArrayLikeObject(a.This);
        var length = GetArrayLikeLengthLong(@this);
        var (callback, thisArg) = a.Get2();

        if (callback is not JSFunction fn)
            throw JSEngine.NewTypeError($"{callback} is not a function in Array.prototype.findLast");

        for (var n = length - 1; n >= 0; n--)
        {
            // findLast visits every index (holes read as undefined); it does not skip holes.
            // Indices beyond the 32-bit array-index range (length may be up to 2^53-1)
            // are read through their canonical numeric-string property key.
            var item = n <= uint.MaxValue ? @this[(uint)n] : @this[new JSNumber(n)];

            var index = new JSNumber(n);
            var itemParams = new Arguments(thisArg, item, index, @this);
            if (fn.InvokeCallback(itemParams).BooleanValue)
                return item;
        }

        return JSUndefined.Value;
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=7; Fingerprint=TBF
    // Broiler-Falsified-If: on an array-like of length 2^32 the value stored at index 4294967295 reaches the predicate as undefined
    // Broiler-Human:        PENDING
    [JSPrototypeMethod]
    [JSExport("findLastIndex", Length = 1)]
    public static JSValue FindLastIndex(in Arguments a)
    {
        var @this = ToArrayLikeObject(a.This);
        var length = GetArrayLikeLengthLong(@this);
        var (callback, thisArg) = a.Get2();

        if (callback is not JSFunction fn)
            throw JSEngine.NewTypeError($"{callback} is not a function in Array.prototype.findLastIndex");

        for (var n = length - 1; n >= 0; n--)
        {
            // findLastIndex visits every index (holes read as undefined); it does not skip holes.
            // Indices beyond the 32-bit array-index range are read through their canonical
            // numeric-string property key.
            var item = n <= uint.MaxValue ? @this[(uint)n] : @this[new JSNumber(n)];

            var index = new JSNumber(n);
            var itemParams = new Arguments(thisArg, item, index, @this);
            if (fn.InvokeCallback(itemParams).BooleanValue)
                return index;
        }

        return JSNumber.MinusOne;
    }

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=7; Fingerprint=TBF
    // Broiler-Falsified-If: a hole in the array is passed to the callback instead of being skipped
    // Broiler-Human:        PENDING
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
            if (!TryGetArrayLikeElement(@this, index, out var item))
                continue;

            var n = new JSNumber(index);
            var itemParams = new Arguments(thisArg, item, n, @this);

            fn.InvokeCallback(itemParams);
        }

        return JSUndefined.Value;
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=1; Fingerprint=TBF
    // Broiler-Falsified-If: a typed-array receiver gets the generic length-bound key iterator instead of the typed-array iterator
    // Broiler-Human:        PENDING
    [JSPrototypeMethod]
    [JSExport("keys")]
    public new static JSValue Keys(in Arguments a)
    {
        if (a.This is Typed.JSTypedArray taK)
            return taK.GetArrayIterator(Typed.JSTypedArray.ArrayIteratorKind.Key);
        var @this = ToArrayLikeObject(a.This);
        return new JSGenerator(new ArrayLikeKeyEnumerator(@this), "Array Iterator");
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=7; Fingerprint=TBF
    // Broiler-Falsified-If: a hole in the source becomes a defined element in the result instead of staying a hole
    // Broiler-Human:        PENDING
    [JSPrototypeMethod]
    [JSExport("map", Length = 1)]
    public static JSValue Map(in Arguments a)
    {
        var @this = ToArrayLikeObject(a.This);
        var length = GetArrayLikeLengthLong(@this);
        var (callback, thisArg) = a.Get2();
        if (callback is not JSFunction fn)
            throw JSEngine.NewTypeError($"{callback} is not a function in Array.prototype.find");

        var r = CreateArraySpecies(@this, length);

        var arrayLikeLength = (uint)length;
        for (uint index = 0; index < arrayLikeLength; index++)
        {
            // Per spec map uses HasProperty(O, k) then Get(O, k), so an index
            // inherited from the prototype chain (e.g. a boxed primitive whose
            // wrapper prototype defines indexed properties) must be visited too.
            if (!TryGetArrayLikeElement(@this, index, out var item))
                continue;

            var itemArgs = new Arguments(thisArg, item, new JSNumber(index), @this);
            CreateDataPropertyOrThrow(r, index, fn.InvokeCallback(itemArgs));
        }

        return r;
    }

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=7; Fingerprint=TBF
    // Broiler-Falsified-If: with no initial value and only holes, the call returns undefined instead of raising a TypeError
    // Broiler-Human:        PENDING
    [JSPrototypeMethod]
    [JSExport("reduce", Length = 1)]
    public static JSValue Reduce(in Arguments a)
    {
        var @this = ToArrayLikeObject(a.This);
        var length = GetArrayLikeLength(@this);
        var (callback, initialValue) = a.Get2();

        if (callback is not JSFunction fn)
            throw JSEngine.NewTypeError($"{callback} is not a function in Array.prototype.reduce");
        uint index = 0;

        if (a.Length == 1)
        {
            while (index < length && !TryGetArrayLikeElement(@this, index, out initialValue))
                index++;

            if (index >= length)
                throw JSEngine.NewTypeError($"No initial value provided and array is empty");

            index++;
        }

        for (; index < length; index++)
        {
            if (!TryGetArrayLikeElement(@this, index, out var item))
                continue;

            var itemArgs = new Arguments(JSUndefined.Value, initialValue, item, new JSNumber(index), @this);
            initialValue = fn.InvokeFunction(itemArgs);
        }

        return initialValue;
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=7; Fingerprint=TBF
    // Broiler-Falsified-If: with no initial value, the last present element is used as the accumulator and also passed to the callback as an item
    // Broiler-Human:        PENDING
    [JSPrototypeMethod]
    [JSExport("reduceRight", Length = 1)]
    public static JSValue ReduceRight(in Arguments a)
    {
        var @this = ToArrayLikeObject(a.This);
        // §23.1.3.24 step 2 reads the length with LengthOfArrayLike (ToLength, up to 2^53-1). Clamping
        // it into the 32-bit array-index range started the descending walk four billion indices below
        // the real end, so an array-like holding its elements near 2^53 iterated over four billion
        // absent indices instead of the two present ones (test262
        // Array/prototype/reduceRight/length-near-integer-limit, staging/sm/Array/to-length).
        var length = GetArrayLikeLengthLong(@this);
        var (callback, initialValue) = a.Get2();

        if (callback is not JSFunction fn)
            throw JSEngine.NewTypeError($"{callback} is not a function in Array.prototype.reduce");

        long start = length - 1;

        if (a.Length == 1)
        {
            while (start >= 0 && !TryGetArrayLikeElement(@this, start, out initialValue))
                start--;

            if (start < 0)
                throw JSEngine.NewTypeError($"No initial value provided and array is empty");

            start--;
        }

        for (long i = start; i >= 0; i--)
        {
            if (!TryGetArrayLikeElement(@this, i, out var item))
                continue;

            initialValue = fn.InvokeCallback(new Arguments(JSUndefined.Value, initialValue, item, new JSNumber(i), @this));
        }

        return initialValue;
    }

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=7; Fingerprint=TBF
    // Broiler-Falsified-If: a hole in the array is passed to the callback instead of being skipped
    // Broiler-Human:        PENDING
    [JSPrototypeMethod]
    [JSExport("some", Length = 1)]
    public static JSValue Some(in Arguments a)
    {
        var array = ToArrayLikeObject(a.This);
        var length = GetArrayLikeLength(array);
        var (first, thisArg) = a.Get2();

        if (first is not JSFunction fn)
            throw JSEngine.NewTypeError($"First argument is not function");

        for (uint index = 0; index < length; index++)
        {
            if (!TryGetArrayLikeElement(array, index, out var item))
                continue;

            var itemArgs = new Arguments(thisArg, item, new JSNumber(index), array);

            if (fn.InvokeCallback(itemArgs).BooleanValue)
                return JSBoolean.True;
        }

        return JSBoolean.False;
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=1; Fingerprint=TBF
    // Broiler-Falsified-If: shrinking a mapped arguments object's length during iteration does not end the iteration at the new length
    // Broiler-Human:        PENDING
    [JSPrototypeMethod]
    [JSExport("values", Length = 0)]
    [Symbol("@@iterator")]
    public new static JSValue Values(in Arguments a)
    {
        var receiver = a.This;
        // A typed-array receiver uses the CreateArrayIterator typed-array path: each step re-validates
        // the view and throws a TypeError once a resize leaves it out of bounds (so the generic
        // Array.prototype.values applied to a resizable-backed typed array matches %TypedArray%.values).
        if (receiver is Typed.JSTypedArray taV)
            return taV.GetArrayIterator(Typed.JSTypedArray.ArrayIteratorKind.Value);
        // Real arrays carry their own live, hole-aware element enumerators. A generic array-like (e.g. a
        // mapped arguments object) needs the length-bound CreateArrayIterator behaviour that re-reads
        // "length" each step, so shrinking it (arguments.length = 2) ends iteration before the stored
        // elements are exhausted.
        if (receiver is JSArray || receiver is IJSIntegerIndexedObject)
            return new JSGenerator(receiver.GetElementEnumerator(), "Array Iterator");

        return new JSGenerator(new ArrayLikeValueEnumerator(ToArrayLikeObject(receiver)), "Array Iterator");
    }

}
