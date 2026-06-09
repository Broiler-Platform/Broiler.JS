using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Broiler.JavaScript.BuiltIns.Boolean;
using Broiler.JavaScript.BuiltIns.Generator;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.BuiltIns.Symbol;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine.Extensions;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Array.Typed;


[JSClassGenerator("TypedArray")]
public partial class JSTypedArray: JSObject, IJSIntegerIndexedObject
{
    internal static int ToIntegerOrInfinity(JSValue value, int defaultValue = 0)
    {
        if (value == null || value.IsUndefined)
            return defaultValue;

        var number = value.DoubleValue;
        if (double.IsNaN(number) || number == 0)
            return 0;

        if (double.IsPositiveInfinity(number) || number > int.MaxValue)
            return int.MaxValue;

        if (double.IsNegativeInfinity(number) || number < int.MinValue)
            return int.MinValue;

        return (int)Math.Truncate(number);
    }

    // %TypedArray%.from and %TypedArray%.of are defined once on the %TypedArray%
    // intrinsic and inherited by every concrete TypedArray constructor, so e.g.
    // Int8Array.from === Float64Array.from. They use `this` (a.This) as the target
    // constructor, so they behave correctly for each subclass.
    [JSExport(Length = 1)]
    private static JSValue From(in Arguments a) => FromShared(in a);

    [JSExport]
    private static JSValue Of(in Arguments a)
    {
        var result = CreateTypedArrayFromConstructor(a.This, a.Length);
        for (int i = 0; i < a.Length; i++)
            result[(uint)i] = a[i];

        return result;
    }

    [JSExport]
    internal readonly JSArrayBuffer buffer;
    [JSExport]
    public readonly int byteOffset;

    // NOTE: BYTES_PER_ELEMENT is intentionally NOT exported here. Per spec it is a data
    // property defined on each concrete TypedArray constructor and its prototype (wired up in
    // PatchTypedArrayBuiltIns), not an accessor on %TypedArray%.prototype.
    internal readonly int bytesPerElement;

    [JSExport]
    internal readonly int length;

    [JSExport]
    internal int ByteLength => buffer.buffer.Length;
    
    public override int Length { get => length; set => throw new NotSupportedException(); }
    public bool HasIntegerIndexedElements => length > 0;

    public JSTypedArray(in Arguments a) : this(JSEngine.NewTargetPrototype) => throw JSEngine.NewTypeError("TypedArray is not a constructor");

    public JSTypedArray(in TypedArrayParameters p): this(p.prototype) 
    {
        buffer = p.buffer;
        length = p.length;
        byteOffset = p.byteOffset;
        bytesPerElement = p.bytesPerElement;

        if (p.copyFrom == null)
        {
            if (buffer == null)
            {
                buffer = new JSArrayBuffer(length * bytesPerElement);
            } 
            else 
            {
                var byteLength = length;
                if (byteLength == -1)
                {
                    byteLength = buffer.buffer.Length - byteOffset;
                    if (byteLength % bytesPerElement != 0)
                        throw JSEngine.NewRangeError($"byte length of TypedArray should be multiple of {bytesPerElement}");

                    length = byteLength / bytesPerElement;
                }
                else
                {
                    var requestedByteLength = (long)byteLength * bytesPerElement;
                    if (requestedByteLength > int.MaxValue)
                        throw JSEngine.NewRangeError($"Start offset {byteOffset} is outside the bounds of the buffer");

                    byteLength = (int)requestedByteLength;
                }

                if (byteOffset < 0 || (byteOffset % bytesPerElement) != 0)
                    throw JSEngine.NewRangeError($"Start offset {byteOffset} is outside the bounds of the buffer");

                if (byteLength < 0 || ((long)byteOffset + byteLength) > buffer.buffer.Length)
                    throw JSEngine.NewRangeError($"Start offset {byteOffset} is outside the bounds of the buffer");

            }
            return;
        }

        if(p.copyFrom == null)
        {
            return;
        }

        var source = p.copyFrom;

        // copy..
        length = -1;
        switch (source)
        {
            case JSArray array:
                length = array.Length;
                break;
            case JSString @string:
                length = @string.Length;
                break;
            case JSTypedArray typed:
                length = typed.Length;
                break;
        }
        var copyByIndex = false;
        if (length == -1 && IsNonIterableArrayLike(source))
        {
            // No @@iterator method: %TypedArray%.from treats the source as an
            // array-like (ToObject, then ToLength(Get(source, "length"))). A
            // missing/undefined "length" yields 0 — a primitive (number, symbol,
            // boolean) or a plain object such as {} becomes a zero-length result.
            // This must never fall through to the iterator path below, which
            // throws "<source> is not iterable".
            length = source is JSObject arrayLike && arrayLike.Length >= 0
                ? arrayLike.Length
                : 0;
            copyByIndex = true;
        }

        IElementEnumerator en2;
        /*
         * If length is unknown, create a List and get its count
         * 
         */
        if (length == -1)
        {
            var en = source.GetIterableEnumerator();
            var elements = new List<JSValue>();
            while (en.MoveNext(out var hasValue, out var item, out var index))
            {
                if (hasValue)
                    elements.Add(item);
            }
            length = elements.Count;
            en2 = new ListElementEnumerator(elements.GetEnumerator());
        }
        else
        {
            en2 = source.GetElementEnumerator();
        }

        buffer = new JSArrayBuffer(length * bytesPerElement);

        if (copyByIndex)
        {
            for (uint i = 0; i < length; i++)
            {
                var item = source[i];
                if (p.map == null || p.map.IsUndefined)
                {
                    this[i] = item;
                }
                else
                {
                    this[i] = p.map.Call(p.thisArg, item, new JSNumber(i));
                }
            }
        }
        else if (p.map == null || p.map.IsUndefined)
        {
            uint i = 0;
            while (en2.MoveNext(out var item))
            {
                this[i++] = item;
            }
        }
        else
        {
            uint i = 0;
            while (en2.MoveNext(out var item))
            {
                this[i] = p.map.Call(p.thisArg, item, new JSNumber(i));
                i++;
            }
        }
    }

    internal static JSTypedArray CreateTypedArrayFromConstructor(JSValue constructor, int length)
    {
        var created = constructor.CreateInstance(new JSNumber(length));
        if (created is not JSTypedArray typedArray)
            throw JSEngine.NewTypeError("TypedArray constructor did not return a TypedArray");

        if (typedArray.Length < length)
            throw JSEngine.NewTypeError("TypedArray constructor returned a too-small TypedArray");

        // TypedArrayCreateFromConstructor is invoked with the `write` access intent
        // by %TypedArray%.from/of and the prototype methods that copy into the
        // result, so the produced typed array must be backed by a mutable
        // (non-immutable) buffer. Validated here, before any element is written.
        if (typedArray.buffer.isImmutable)
            throw JSEngine.NewTypeError("TypedArray constructor returned a typed array backed by an immutable ArrayBuffer");

        return typedArray;
    }

    /// <summary>
    /// Shared implementation of %TypedArray%.from ( source [ , mapfn [ , thisArg ] ] )
    /// following the spec's validation and error ordering (ES2024 23.2.2.1).
    /// The receiver C must be a constructor; mapfn must be undefined or callable,
    /// and that is verified BEFORE the source is observed (no @@iterator lookup,
    /// constructor call, length read, or element read happens first). The source
    /// is iterated through its @@iterator method when present, otherwise treated
    /// as an array-like, and the result is created by calling C with the resolved
    /// length so element coercion matches the receiver's element type.
    /// </summary>
    internal static JSValue FromShared(in Arguments a)
    {
        var constructor = a.This;
        if (constructor is not IJSFunction)
            throw JSEngine.NewTypeError("%TypedArray%.from must be called with a constructor receiver");

        var (source, mapfn, thisArg) = a.Get3();

        // mapfn must be undefined or callable. Checked before the source is
        // touched in any way (the test asserts no side effects occur first).
        var mapping = false;
        if (!mapfn.IsUndefined)
        {
            if (!mapfn.IsFunction)
                throw JSEngine.NewTypeError("%TypedArray%.from: mapping function is not callable");
            mapping = true;
        }

        // GetMethod(source, @@iterator): a null/undefined source throws here (its
        // ToObject step), a non-callable non-nullish @@iterator throws, and an
        // absent/nullish @@iterator selects the array-like path below.
        var usingIterator = GetIteratorMethod(source);

        if (!usingIterator.IsUndefined)
        {
            var iterator = usingIterator.InvokeFunction(new Arguments(source));
            if (!iterator.IsObject)
                throw JSEngine.NewTypeError("Result of the Symbol.iterator method is not an object");

            var values = new List<JSValue>();
            var step = new JSIterator(iterator);
            while (step.MoveNext(out var item))
                values.Add(item);

            var target = CreateTypedArrayFromConstructor(constructor, values.Count);
            for (var k = 0; k < values.Count; k++)
            {
                var value = mapping ? mapfn.Call(thisArg, values[k], new JSNumber(k)) : values[k];
                target[(uint)k] = value;
            }

            return target;
        }

        // Array-like path: ToLength(Get(source, "length")); a missing length is 0.
        var rawLength = ToIntegerOrInfinity(source[KeyStrings.length]);
        var length = rawLength < 0 ? 0 : rawLength;

        var arrayLikeTarget = CreateTypedArrayFromConstructor(constructor, length);
        for (var k = 0; k < length; k++)
        {
            var kValue = source[(uint)k];
            var value = mapping ? mapfn.Call(thisArg, kValue, new JSNumber(k)) : kValue;
            arrayLikeTarget[(uint)k] = value;
        }

        return arrayLikeTarget;
    }

    private static JSValue GetIteratorMethod(JSValue source)
    {
        // GetMethod(source, @@iterator). GetV performs ToObject(source) first, so a
        // null/undefined source throws here (covers from(), from(undefined), from(null)).
        if (source.IsNullOrUndefined)
            throw JSEngine.NewTypeError("Cannot convert undefined or null to object");

        var method = source[JSValue.SymbolIterator];
        if (method.IsNullOrUndefined)
            return JSValue.UndefinedValue;

        if (!method.IsFunction)
            throw JSEngine.NewTypeError("Symbol(Symbol.iterator) value is not callable");

        return method;
    }

    internal static JSValue GetSpeciesConstructor(JSTypedArray source)
    {
        var defaultConstructor = source.GetDefaultConstructor();

        var constructor = source[KeyStrings.constructor];
        if (constructor.IsUndefined)
            return defaultConstructor;

        if (!constructor.IsObject)
            throw JSEngine.NewTypeError("TypedArray constructor property is not an object");

        var species = constructor[(IJSSymbol)JSSymbol.species];
        if (species.IsNullOrUndefined)
            return defaultConstructor;

        if (species is not IJSFunction)
            throw JSEngine.NewTypeError("TypedArray species constructor is not a constructor");

        return species;
    }

    /// <summary>
    /// Resolves the intrinsic constructor for this typed array kind (e.g.
    /// %Float64Array%), used as the default constructor by the species
    /// protocol when <c>constructor</c> or <c>@@species</c> is absent.
    /// </summary>
    /// <remarks>
    /// The default constructor must be obtained non-observably: it is the realm
    /// intrinsic for this typed array's element kind, not whatever the
    /// <c>constructor</c> property currently resolves to. Reading the prototype's
    /// <c>constructor</c> would be hijacked by tests that install a getter on
    /// <c>TA.prototype.constructor</c> (e.g. speciesctor-get-ctor-inherited),
    /// turning the default into <c>undefined</c> and breaking species creation.
    /// </remarks>
    private JSValue GetDefaultConstructor()
    {
        var kind = GetIntrinsicConstructorName();
        if (kind.HasValue && JSEngine.Current is JSObject global)
        {
            var intrinsic = global[kind];
            if (intrinsic is IJSFunction)
                return intrinsic;
        }

        // Fallback: the constructor reachable from the (intrinsic) prototype.
        if (GetCurrentPrototype() is JSObject intrinsicPrototype)
        {
            var constructor = intrinsicPrototype[KeyStrings.constructor];
            if (!constructor.IsNullOrUndefined)
                return constructor;
        }

        return this[KeyStrings.constructor];
    }

    /// <summary>
    /// The realm-global name of this typed array's intrinsic constructor, keyed
    /// off the concrete element type (never a user-observable property read).
    /// </summary>
    private KeyString GetIntrinsicConstructorName() => this switch
    {
        JSInt8Array => KeyStrings.GetOrCreate("Int8Array"),
        JSUInt8Array => KeyStrings.GetOrCreate("Uint8Array"),
        JSUint8ClampedArray => KeyStrings.GetOrCreate("Uint8ClampedArray"),
        JSInt16Array => KeyStrings.GetOrCreate("Int16Array"),
        JSUInt16Array => KeyStrings.GetOrCreate("Uint16Array"),
        JSInt32Array => KeyStrings.GetOrCreate("Int32Array"),
        JSUInt32Array => KeyStrings.GetOrCreate("Uint32Array"),
        JSBigInt64Array => KeyStrings.GetOrCreate("BigInt64Array"),
        JSBigUint64Array => KeyStrings.GetOrCreate("BigUint64Array"),
        JSFloat16Array => KeyStrings.GetOrCreate("Float16Array"),
        JSFloat32Array => KeyStrings.GetOrCreate("Float32Array"),
        JSFloat64Array => KeyStrings.GetOrCreate("Float64Array"),
        _ => KeyString.Empty
    };

    private static bool TryGetCanonicalNumericIndex(in KeyString key, out double numericIndex)
    {
        var text = key.Value.Value;
        if (text == "-0")
        {
            numericIndex = -0.0;
            return true;
        }

        if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out numericIndex))
            return false;

        return new JSNumber(numericIndex).ToString() == text;
    }

    private bool IsValidIntegerIndex(double numericIndex)
    {
        if (double.IsNaN(numericIndex)
            || double.IsInfinity(numericIndex)
            || Math.Truncate(numericIndex) != numericIndex)
        {
            return false;
        }

        if (numericIndex == 0 && double.IsNegativeInfinity(1 / numericIndex))
            return false;

        return numericIndex >= 0 && numericIndex < length;
    }

    internal virtual void ValidateElementValue(JSValue value) => _ = (value ?? JSUndefined.Value).DoubleValue;

    public override JSValue HasProperty(JSValue propertyKey)
    {
        // [[HasProperty]] for an integer-indexed exotic object: a canonical numeric
        // index resolves to IsValidIntegerIndex and never consults the prototype
        // chain (so an out-of-range index is absent even when the prototype carries
        // a same-named element). Non-numeric keys fall back to OrdinaryHasProperty.
        var key = propertyKey.ToKey(false);
        switch (key.Type)
        {
            case KeyType.UInt:
                return IsValidIntegerIndex(key.Index) ? BooleanTrue : BooleanFalse;

            case KeyType.String:
                if (TryGetCanonicalNumericIndex(key.KeyString, out var numericIndex))
                    return IsValidIntegerIndex(numericIndex) ? BooleanTrue : BooleanFalse;
                break;
        }

        return base.HasProperty(propertyKey);
    }

    public override JSValue GetOwnPropertyDescriptor(JSValue name)
    {
        var key = name.ToKey(false);
        switch (key.Type)
        {
            case KeyType.String:
                if (key.KeyString.Key == KeyStrings.length.Key)
                {
                    var l = new JSObject();
                    l.FastAddValue(KeyStrings.value, new JSNumber(length), JSPropertyAttributes.ConfigurableValue);
                    l.FastAddValue(KeyStrings.writable, JSBoolean.False, JSPropertyAttributes.ConfigurableValue);
                    l.FastAddValue(KeyStrings.enumerable, JSBoolean.True, JSPropertyAttributes.ConfigurableValue);
                    return l;
                }
                break;
            case KeyType.UInt:
                if (key.Index < (uint)length)
                {
                    var l = new JSObject();
                    var v = GetValue(key.Index, this, false);
                    l.FastAddValue(KeyStrings.value, v, JSPropertyAttributes.ConfigurableValue);
                    l.FastAddValue(KeyStrings.writable, JSBoolean.True, JSPropertyAttributes.ConfigurableValue);
                    l.FastAddValue(KeyStrings.enumerable, JSBoolean.True, JSPropertyAttributes.ConfigurableValue);
                    l.FastAddValue(KeyStrings.configurable, JSBoolean.False, JSPropertyAttributes.ConfigurableValue);
                    return l;

                }
                return JSUndefined.Value;
        }
        return base.GetOwnPropertyDescriptor(name);
    }

    public override JSValue DefineProperty(uint key, JSObject pd)
    {
        // IntegerIndexedDefineOwnProperty checks IsValidIntegerIndex first and
        // returns false for an out-of-bounds index WITHOUT converting the value
        // (ToNumber happens only at the final element-set step).
        if (key >= length)
            return JSBoolean.False;

        var hasValue = !pd.GetInternalProperty(KeyStrings.value, false).IsEmpty;
        if (hasValue)
            ValidateElementValue(pd[KeyStrings.value]);

        if (!pd.GetInternalProperty(KeyStrings.get, false).IsEmpty
            || !pd.GetInternalProperty(KeyStrings.set, false).IsEmpty)
        {
            return JSBoolean.False;
        }

        if (!pd.GetInternalProperty(KeyStrings.configurable, false).IsEmpty && pd[KeyStrings.configurable].BooleanValue)
            return JSBoolean.False;

        if (!pd.GetInternalProperty(KeyStrings.enumerable, false).IsEmpty && !pd[KeyStrings.enumerable].BooleanValue)
            return JSBoolean.False;

        if (!pd.GetInternalProperty(KeyStrings.writable, false).IsEmpty && !pd[KeyStrings.writable].BooleanValue)
            return JSBoolean.False;

        if (hasValue)
            SetValue(key, pd[KeyStrings.value], this, true);

        return JSUndefined.Value;
    }

    public override JSValue DefineProperty(in KeyString name, JSObject pd)
    {
        if (TryGetCanonicalNumericIndex(name, out var numericIndex))
        {
            // Out-of-bounds (invalid) integer index: return false without ToNumber.
            // The valid-index path delegates to the uint overload, which validates
            // and writes the value.
            if (!IsValidIntegerIndex(numericIndex))
                return JSBoolean.False;

            return DefineProperty((uint)numericIndex, pd);
        }

        return base.DefineProperty(name, pd);
    }

    internal protected override bool SetValue(KeyString name, JSValue value, JSValue receiver, bool throwError = true)
    {
        if (TryGetCanonicalNumericIndex(name, out var numericIndex))
        {
            ValidateElementValue(value);
            if (IsValidIntegerIndex(numericIndex))
                return SetValue((uint)numericIndex, value, receiver, throwError);

            return true;
        }

        return base.SetValue(name, value, receiver, throwError);
    }
    public override bool BooleanValue => true;
    public override double DoubleValue => double.NaN;
    public override bool Equals(JSValue value) => ReferenceEquals(this, value);

    public override JSValue InvokeFunction(in Arguments a) => throw JSEngine.NewTypeError($"{this} is not a function");

    public override bool StrictEquals(JSValue value) => ReferenceEquals(this, value);

    public override JSValue Delete(in KeyString key)
    {
        if (TryGetCanonicalNumericIndex(key, out var numericIndex))
            return IsValidIntegerIndex(numericIndex) ? JSBoolean.False : JSBoolean.True;

        return base.Delete(key);
    }

    public override JSValue Delete(uint key) => key < length ? JSBoolean.False : JSBoolean.True;

    public override string ToString()
    {
        var sb = new StringBuilder();
        for (int i = 0; i < length; i++)
        {
            if (i != 0)
            {
                sb.Append(',');
            }

            sb.Append(this[(uint)i].ToString());
        }
        return sb.ToString();
    }

    public override string ToDetailString() => ToString();

    public override IElementEnumerator GetElementEnumerator() => new ElementEnumerator(this);

    internal IElementEnumerator GetElementEnumerator(int startIndex) => new ElementEnumerator(this, startIndex);

    internal IElementEnumerator GetEntries() => new EntryEnumerator(this);

    // [[OwnPropertyKeys]] for a typed array is the integer indices followed by the
    // ordinary string-keyed own properties (and then symbols, handled separately).
    // The base KeyEnumerator already yields the indices via the overridden
    // GetElementEnumerator, so deferring to it also surfaces extra own properties
    // (e.g. `ta.foo = 1`) to getOwnPropertyNames / for-in / Object.is{Sealed,Frozen}
    // instead of dropping them as an indices-only enumerator did.
    public override IElementEnumerator GetAllKeys(bool showEnumerableOnly = true, bool inherited = true) => base.GetAllKeys(showEnumerableOnly, inherited);

    internal JSGenerator GetKeys() => new(new IntKeyEnumerator(length), "Array Iterator");

    private static bool IsNonIterableArrayLike(JSValue source) =>
        JSValue.SymbolIterator == null || source.PropertyOrUndefined(JSValue.SymbolIterator).IsUndefined;

    struct ElementEnumerator(JSTypedArray typedArray, int startIndex = 0) : IElementEnumerator
    {
        private int index = startIndex - 1;

        public bool MoveNext(out bool hasValue, out JSValue value, out uint index)
        {
            if (++this.index < typedArray.length)
            {
                hasValue = true;
                index = (uint)this.index;
                value = typedArray[index];
                return true;
            }

            hasValue = false;
            index = 0;
            value = JSUndefined.Value;
            return false;
        }

        public bool MoveNext(out JSValue value)
        {
            if (++index < typedArray.length)
            {
                value = typedArray[(uint)index];
                return true;
            }

            value = JSUndefined.Value;
            return false;
        }

        public bool MoveNextOrDefault(out JSValue value, JSValue @default)
        {
            if (++index < typedArray.length)
            {
                value = typedArray[(uint)index];
                return true;
            }

            value = @default;
            return false;
        }

        public JSValue NextOrDefault(JSValue @default)
        {
            if (++index < typedArray.length)
            {
                return typedArray[(uint)index];
            }

            return @default;
        }
    }

    struct EntryEnumerator(JSTypedArray typedArray) : IElementEnumerator
    {
        private int index = -1;

        public bool MoveNext(out bool hasValue, out JSValue value, out uint index)
        {
            if (++this.index < typedArray.length)
            {
                hasValue = true;
                index = (uint)this.index;
                value = new JSArray(new JSNumber(index), typedArray[index]);
                return true;
            }

            hasValue = false;
            index = 0;
            value = JSUndefined.Value;
            return false;
        }

        public bool MoveNext(out JSValue value)
        {
            if (++index < typedArray.length)
            {
                value = new JSArray(new JSNumber(index), typedArray[(uint)index]);
                return true;
            }

            value = JSUndefined.Value;
            return false;
        }

        public bool MoveNextOrDefault(out JSValue value, JSValue @default)
        {
            if (++index < typedArray.length)
            {
                value = new JSArray(new JSNumber(index), typedArray[(uint)index]);
                return true;
            }

            value = @default;
            return false;
        }
        public JSValue NextOrDefault(JSValue @default)
        {
            if (++index < typedArray.length)
            {
                return new JSArray(new JSNumber(index), typedArray[(uint)index]);
            }

            return @default;
        }
    }
}
