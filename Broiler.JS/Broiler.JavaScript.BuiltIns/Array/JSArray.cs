using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.BuiltIns.Error;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.BuiltIns.Symbol;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.BuiltIns.Function;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Array;

[JSBaseClass("Object")]
[JSFunctionGenerator("Array")]

public partial class JSArray : JSObject
{
    internal uint _length;

    private bool IsLengthReadOnly()
    {
        ref var ownProperties = ref GetOwnProperties(false);
        if (ownProperties.IsEmpty)
            return false;

        ref var lengthProperty = ref ownProperties.GetValue(KeyStrings.length.Key);
        return !lengthProperty.IsEmpty && lengthProperty.IsReadOnly;
    }

    public JSArray() : base((JSObject)null) { }

    public JSArray(params JSValue[] items) : this((IEnumerable<JSValue>)items) { }

    public JSArray(IElementEnumerator en) : this()
    {
        ref var elements = ref GetElements(true);
        while (en.MoveNextOrDefault(out var v, JSUndefined.Value))
            elements.Put(_length++, v);
    }

    public JSArray(IEnumerable<JSValue> items) : this()
    {
        ref var elements = ref GetElements(true);
        foreach (var item in items)
            elements.Put(_length++, item);
    }

    internal IElementEnumerator GetEntries() => new EntryEnumerator(this);

    public JSArray(uint count) : this()
    {
        AllocateElements(count);
        CreateElements(count);
        _length = count;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        for (uint i = 0; i < _length; i++)
        {
            if (i > 0)
                sb.Append(',');
            var item = this[i];
            if (item != null && !item.IsNullOrUndefined)
                sb.Append(item);
        }
        return sb.ToString();
    }

    public override string ToDetailString() => $"[{ToString()}]";

    public override bool IsArray => true;

    internal override void UpdateArrayLengthIfNeeded(uint key)
    {
        // Array indices run 0..2^32-2; uint.MaxValue (2^32-1) is not a valid index, so it never
        // extends the length — and "key + 1" would overflow to 0, corrupting an existing length.
        if (key != uint.MaxValue && _length <= key)
            _length = key + 1;
    }

    public override void AddArrayItem(JSValue item) => Add(item);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerable<(uint index, JSValue value)> GetArrayElements(bool withHoles = true)
    {
        var elements = GetElements();
        uint l = _length;
        for (uint i = 0; i < l; i++)
        {
            if (elements.TryGetValue(i, out var p))
            {
                yield return (i, (JSValue)p.value);
                continue;
            }
            if (withHoles)
                yield return (i, JSUndefined.Value);
        }
    }

    [JSExport("length")]
    public double ArrayLength
    {
        get => _length;
        set => SetLengthValue(JSValue.CreateNumber(value), true);
    }

    public override int Length
    {
        get => (int)_length;
        set => ArrayLength = value;
    }

    // True once "length" has been materialized into the ordinary property store
    // (which happens only when its writability is changed via SetLengthWritable).
    // While synthetic it is absent from the base key stream and must be injected.
    private bool IsLengthStored()
    {
        ref var ownProperties = ref GetOwnProperties(false);
        return !ownProperties.IsEmpty && !ownProperties.GetValue(KeyStrings.length.Key).IsEmpty;
    }

    // Array exotic objects expose "length" as a non-enumerable own property that
    // sits right after the integer indices in [[OwnPropertyKeys]]. Because it is
    // synthetic (not stored unless its writability changes), the base key stream
    // omits it. Inject it for non-enumerable own-key reflection
    // (Object.getOwnPropertyNames / Reflect.ownKeys / Object.getOwnPropertyDescriptors)
    // while leaving enumerable-only walks (Object.keys, for-in, JSON, spread)
    // untouched — "length" is non-enumerable. When it is already stored, the base
    // stream yields it and no injection is needed.
    public override IElementEnumerator GetAllKeys(bool showEnumerableOnly = true, bool inherited = true)
    {
        var baseKeys = base.GetAllKeys(showEnumerableOnly, inherited);
        if (showEnumerableOnly || IsLengthStored())
            return baseKeys;

        return new ArrayLengthKeyEnumerator(baseKeys);
    }

    // Wraps the base own-key enumerator and emits "length" at the boundary between
    // the integer-index keys and the remaining string keys (or at the end when there
    // are no further string keys), reproducing the array's [[OwnPropertyKeys]] order.
    private sealed class ArrayLengthKeyEnumerator(IElementEnumerator inner) : IElementEnumerator
    {
        private static readonly JSValue LengthKey = JSValue.CreateString("length");

        private bool lengthPending = true;
        private bool hasBuffered;
        private bool bufHasValue;
        private JSValue bufValue;
        private uint bufIndex;

        private static bool IsIndexKey(JSValue key) => key.ToKey(false).Type == KeyType.UInt;

        private bool Advance(out bool hasValue, out JSValue value, out uint index)
        {
            if (hasBuffered)
            {
                hasBuffered = false;
                hasValue = bufHasValue;
                value = bufValue;
                index = bufIndex;
                return true;
            }

            if (inner.MoveNext(out hasValue, out value, out index))
            {
                // The first present non-index key marks the end of the index run;
                // surface "length" before it (buffering the key we just pulled).
                if (lengthPending && hasValue && !IsIndexKey(value))
                {
                    lengthPending = false;
                    bufHasValue = hasValue;
                    bufValue = value;
                    bufIndex = index;
                    hasBuffered = true;

                    hasValue = true;
                    value = LengthKey;
                    index = 0;
                }

                return true;
            }

            if (lengthPending)
            {
                lengthPending = false;
                hasValue = true;
                value = LengthKey;
                index = 0;
                return true;
            }

            hasValue = false;
            value = null;
            index = 0;
            return false;
        }

        public bool MoveNext(out bool hasValue, out JSValue value, out uint index)
            => Advance(out hasValue, out value, out index);

        public bool MoveNext(out JSValue value)
        {
            while (Advance(out var hasValue, out value, out _))
            {
                if (hasValue)
                    return true;
            }

            value = JSValue.UndefinedValue;
            return false;
        }

        public bool MoveNextOrDefault(out JSValue value, JSValue @default)
        {
            while (Advance(out var hasValue, out value, out _))
            {
                if (hasValue)
                    return true;
            }

            value = @default;
            return false;
        }

        public JSValue NextOrDefault(JSValue @default)
        {
            while (Advance(out var hasValue, out var value, out _))
            {
                if (hasValue)
                    return value;
            }

            return @default;
        }
    }

    public override JSValue GetOwnPropertyDescriptor(JSValue name)
    {
        var key = name.ToKey(false);
        if (key.Type == KeyType.String && key.KeyString.Key == KeyStrings.length.Key)
        {
            var descriptor = new JSObject();
            descriptor.FastAddValue(KeyStrings.value, JSValue.CreateNumber(_length), JSPropertyAttributes.ConfigurableValue);
            descriptor.FastAddValue(KeyStrings.writable, IsLengthReadOnly() ? JSValue.BooleanFalse : JSValue.BooleanTrue, JSPropertyAttributes.ConfigurableValue);
            descriptor.FastAddValue(KeyStrings.enumerable, JSValue.BooleanFalse, JSPropertyAttributes.ConfigurableValue);
            descriptor.FastAddValue(KeyStrings.configurable, JSValue.BooleanFalse, JSPropertyAttributes.ConfigurableValue);
            return descriptor;
        }

        return base.GetOwnPropertyDescriptor(name);
    }

    internal protected override JSValue GetValue(KeyString key, JSValue receiver, bool throwError = true)
    {
        if (key.Key == KeyStrings.length.Key)
            return JSValue.CreateNumber(_length);

        return base.GetValue(key, receiver, throwError);
    }

    internal protected override bool SetValue(KeyString name, JSValue value, JSValue receiver, bool throwError = true)
    {
        // Array exotic objects do not override [[Set]] — it is OrdinarySet. The
        // `length` fast path applies only when writing to THIS array directly; when
        // the receiver is a different object (e.g. `Reflect.set(arr, "length", v,
        // proxy)` from a Proxy set trap, or pop/shift via a proxy), OrdinarySet must
        // redirect to the receiver's [[GetOwnProperty]] + [[DefineOwnProperty]]
        // instead of mutating this array's own length.
        if (name.Key == KeyStrings.length.Key)
        {
            if (receiver == null || ReferenceEquals(receiver, this))
                return SetLengthValue(value, throwError);

            return SetKeyStringOnReceiver(name, value, receiver, JSPropertyAttributes.EnumerableConfigurableValue, throwError);
        }

        return base.SetValue(name, value, receiver, throwError);
    }

    public override JSValue Delete(in KeyString key)
    {
        if (key.Key == KeyStrings.length.Key)
            return JSValue.BooleanFalse;

        return base.Delete(key);
    }

    public void Add(JSValue item)
    {
        if (item == null)
        {
            _length++;
        }
        else
        {
            ref var elements = ref CreateElements();
            elements.Put(_length++, item);
        }
    }

    public override IElementEnumerator GetElementEnumerator()
    {
        return new ElementEnumerator(this);
    }

    // Key enumeration (Object.keys / for-in / spread of own keys) must skip
    // non-enumerable indices. Array elements are normally enumerable, but
    // Object.defineProperty can store an indexed element with enumerable:false,
    // so the raw index walk has to honour the enumerable filter here. (The base
    // override assumes specialised element walks are always enumerable, which is
    // not true for arrays.) GetElementEnumerator stays unfiltered because the
    // iterator protocol (for-of / spread) visits every index regardless.
    internal override IElementEnumerator GetOwnIndexedElementEnumerator(bool enumerableOnly = false)
    {
        return new ElementEnumerator(this, enumerableOnly);
    }

    private struct ElementEnumerator(JSArray array, bool enumerableOnly = false) : IElementEnumerator
    {
        readonly bool enumerableOnly = enumerableOnly;

        // Array iterators are live (CreateArrayIterator re-reads the length each
        // step): entries pushed during for-of / spread traversal must be visited,
        // and a shrink must end iteration early. Read the length dynamically
        // rather than snapshotting it at construction.
        readonly uint length => array._length;
        uint index = uint.MaxValue;

        public bool MoveNext(out JSValue value)
        {
            if ((index = (index == uint.MaxValue) ? 0 : (index + 1)) < length)
            {
                value = array[index];
                return true;
            }
            value = JSUndefined.Value;
            return false;
        }

        public bool MoveNext(out bool hasValue, out JSValue value, out uint index)
        {
            ref var elements = ref array.GetElements();
            if ((this.index = (this.index == uint.MaxValue) ? 0 : (this.index + 1)) < length)
            {
                index = this.index;
                if (elements.TryGetValue(index, out var property)
                    && (!enumerableOnly || property.IsEnumerable))
                {
                    value = property.IsEmpty
                        ? null
                        : (property.IsValue
                        ? (JSValue)property.value
                        : (property.get is IJSFunction getter
                            ? getter.InvokeFunction(new Arguments(array))
                            : JSUndefined.Value));
                    hasValue = true;
                }
                else
                {
                    hasValue = false;
                    value = JSUndefined.Value;
                }
                return true;
            }
            index = 0;
            value = JSUndefined.Value;
            hasValue = false;
            return false;
        }

        public bool MoveNextOrDefault(out JSValue value, JSValue @default)
        {
            ref var elements = ref array.GetElements();
            if ((index = (index == uint.MaxValue) ? 0 : (index + 1)) < length)
            {
                if (elements.TryGetValue(index, out var property))
                {
                    value = property.IsEmpty
                        ? null
                        : (property.IsValue
                        ? (JSValue)property.value
                        : (property.get is IJSFunction getter
                            ? getter.InvokeFunction(new Arguments(array))
                            : JSUndefined.Value));
                }
                else
                {
                    value = @default;
                }
                return true;
            }
            value = @default;
            return false;
        }

        public JSValue NextOrDefault(JSValue @default)
        {
            ref var elements = ref array.GetElements();
            if ((index = (index == uint.MaxValue) ? 0 : (index + 1)) < length)
            {
                if (elements.TryGetValue(index, out var property))
                {
                    return property.IsEmpty
                        ? null
                        : (property.IsValue
                        ? (JSValue)property.value
                        : (property.get is IJSFunction getter
                            ? getter.InvokeFunction(new Arguments(array))
                            : JSUndefined.Value));
                }
                return @default;
            }
            return @default;
        }


    }

    public void AddRange(JSValue iterator)
    {
        ref var et = ref CreateElements();
        // var et = this.elements;
        var el = _length;
        if (iterator is JSArray ary)
        {
            var l = ary._length;
            ref var e = ref ary.GetElements();
            for (uint i = 0; i < l; i++)
            {
                et.Put(el++, ary[i]);
            }
            _length = el;
            return;
        }

        var en = iterator.GetIterableEnumerator();
        while (en.MoveNext(out var hasValue, out var item, out var index))
        {
            if (hasValue)
            {
                et.Put(el++, item);
            }
            else
            {
                el++;
            }
        }
        _length = el;
    }

    public override bool SetValue(uint name, JSValue value, JSValue receiver, bool throwError = true)
    {
        if (_length <= name && IsLengthReadOnly())
        {
            if (throwError)
                throw JSEngine.NewTypeError("Cannot modify property length");

            return false;
        }

        if (base.SetValue(name, value, receiver, throwError))
        {
            // uint.MaxValue (2^32-1) is not a valid array index, so it never extends the length;
            // "name + 1" would otherwise overflow to 0 and corrupt an existing length.
            if (name != uint.MaxValue && _length <= name && !GetInternalProperty(name, false).IsEmpty)
            {
                _length = name + 1;
            }
            return true;
        }
        return false;
    }

    public override JSValue DefineProperty(JSValue key, JSObject propertyDescription)
    {
        var propertyKey = key.ToKey();
        if (propertyKey.Type == KeyType.String && propertyKey.KeyString.Key == KeyStrings.length.Key)
            return DefineLengthProperty(propertyDescription);

        if (propertyKey.Type == KeyType.UInt)
            return DefineIndexProperty(propertyKey.Index, propertyDescription);

        return base.DefineProperty(key, propertyDescription);
    }

    private JSValue DefineIndexProperty(uint index, JSObject propertyDescription)
    {
        // Adding an index at or beyond a non-writable length fails the
        // [[DefineOwnProperty]] predicate — return false (Object.defineProperty's
        // OrThrow caller converts it to a TypeError; Reflect.defineProperty returns it).
        if (index >= _length && IsLengthReadOnly())
            return JSValue.BooleanFalse;

        var result = base.DefineProperty(index, propertyDescription);
        if (_length <= index)
            _length = index + 1;

        return result;
    }

    private JSValue DefineLengthProperty(JSObject propertyDescription)
    {
        var hasValue = !propertyDescription.GetInternalProperty(KeyStrings.value, false).IsEmpty;
        var hasWritable = !propertyDescription.GetInternalProperty(KeyStrings.writable, false).IsEmpty;
        var hasConfigurable = !propertyDescription.GetInternalProperty(KeyStrings.configurable, false).IsEmpty;
        var hasEnumerable = !propertyDescription.GetInternalProperty(KeyStrings.enumerable, false).IsEmpty;

        // ArraySetLength validates a supplied length *value* (ToUint32 must equal ToNumber) and throws
        // RangeError for an invalid one BEFORE the ordinary descriptor invariants are checked — so
        // e.g. {value: -1, configurable: true} is a RangeError (invalid value), not the TypeError the
        // configurable-redefinition invariant would otherwise produce.
        uint newLength = 0;
        if (hasValue)
        {
            var newLengthNumber = propertyDescription[KeyStrings.value].DoubleValue;
            if (double.IsNaN(newLengthNumber) || newLengthNumber < 0 || newLengthNumber > uint.MaxValue || newLengthNumber != System.Math.Truncate(newLengthNumber))
                throw JSEngine.NewRangeError("Invalid length");

            newLength = (uint)newLengthNumber;
        }

        // [[DefineOwnProperty]] is a predicate: an invariant violation returns false
        // (the OrThrow caller, e.g. Object.defineProperty, turns that into a throw;
        // Reflect.defineProperty surfaces it as `false`).
        if ((hasConfigurable && propertyDescription[KeyStrings.configurable].BooleanValue)
            || (hasEnumerable && propertyDescription[KeyStrings.enumerable].BooleanValue))
        {
            return JSValue.BooleanFalse;
        }

        if (!hasValue)
        {
            if (hasWritable)
                SetLengthWritable(propertyDescription[KeyStrings.writable].BooleanValue);

            return JSUndefined.Value;
        }

        var oldLength = _length;
        var newWritable = !hasWritable || propertyDescription[KeyStrings.writable].BooleanValue;

        if (newLength >= oldLength)
        {
            if (newLength != oldLength && IsLengthReadOnly())
                return JSValue.BooleanFalse;

            _length = newLength;
            SetLengthWritable(newWritable);
            return JSUndefined.Value;
        }

        if (IsLengthReadOnly())
            return JSValue.BooleanFalse;

        ref var elements = ref GetElements();

        // Only the indices that are actually stored need to be deleted; absent
        // indices delete trivially. Walking the whole [newLength, oldLength) range
        // would loop billions of times when shrinking from a huge sparse length
        // (e.g. length = 2**32 - 1 back to 2). Collect the stored indices in range,
        // then delete them high→low so the first non-configurable element halts the
        // shrink at the spec-mandated point (ArraySetLength deletes from the top).
        var doomed = new List<uint>();
        foreach (var (key, _) in elements.StoredValues())
        {
            if (key >= newLength && key < oldLength)
                doomed.Add(key);
        }

        doomed.Sort();
        for (var i = doomed.Count - 1; i >= 0; i--)
        {
            var actualIndex = doomed[i];
            if (elements.TryGetValue(actualIndex, out var property) && !property.IsConfigurable)
            {
                _length = actualIndex + 1;
                SetLengthWritable(newWritable);
                return JSValue.BooleanFalse;
            }

            elements.RemoveAt(actualIndex);
        }

        _length = newLength;
        SetLengthWritable(newWritable);
        return JSUndefined.Value;
    }

    private bool SetLengthValue(JSValue value, bool throwError)
    {
        var newLengthNumber = value.DoubleValue;
        if (double.IsNaN(newLengthNumber)
            || newLengthNumber < 0
            || newLengthNumber > uint.MaxValue
            || newLengthNumber != System.Math.Truncate(newLengthNumber))
        {
            throw JSEngine.NewRangeError("Invalid length");
        }

        // ArraySetLength only fails when "length" itself is non-writable; a merely
        // non-extensible array (Object.preventExtensions) keeps a writable length, so
        // assigning to it must succeed rather than throw. (A frozen array already has a
        // read-only length, so IsLengthReadOnly covers that case.)
        if (IsLengthReadOnly())
        {
            if (throwError)
                throw JSEngine.NewTypeError("Cannot modify property length");

            return false;
        }

        var propertyDescription = new JSObject();
        propertyDescription.FastAddValue(KeyStrings.value, value, JSPropertyAttributes.EnumerableConfigurableValue);

        try
        {
            var result = DefineLengthProperty(propertyDescription);
            if (result.IsBoolean && !result.BooleanValue)
            {
                if (throwError)
                    throw JSEngine.NewTypeError("Cannot redefine array length");

                return false;
            }

            return true;
        }
        catch (JSException ex) when (!throwError && ex.Error is JSTypeError)
        {
            return false;
        }
    }

    private void SetLengthWritable(bool writable)
    {
        var attributes = writable ? JSPropertyAttributes.Value : JSPropertyAttributes.ReadonlyValue;
        GetOwnProperties().Put(KeyStrings.length.Key) = new JSProperty(KeyStrings.length, JSValue.CreateNumber(_length), attributes);
    }
}


struct EntryEnumerator(JSArray typedArray) : IElementEnumerator
{
    private int index = -1;

    public bool MoveNext(out bool hasValue, out JSValue value, out uint index)
    {
        if (++this.index < typedArray.Length)
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
        if (++index < typedArray.Length)
        {
            value = new JSArray(new JSNumber(index), typedArray[(uint)index]);
            return true;
        }

        value = JSUndefined.Value;
        return false;
    }

    public bool MoveNextOrDefault(out JSValue value, JSValue @default)
    {
        if (++index < typedArray.Length)
        {
            value = new JSArray(new JSNumber(index), typedArray[(uint)index]);
            return true;
        }

        value = @default;
        return false;
    }

    public JSValue NextOrDefault(JSValue @default)
    {
        if (++index < typedArray.Length)
        {
            return new JSArray(new JSNumber(index), typedArray[(uint)index]);
        }
        return @default;
    }
}
