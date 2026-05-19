using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Broiler.JavaScript.ExpressionCompiler;
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
        if (_length <= key)
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
        set
        {
            if (IsLengthReadOnly())
                throw JSEngine.NewTypeError("Cannot modify property length");

            if (IsSealedOrFrozen())
                throw JSEngine.NewTypeError("Cannot modify property length");
            var prev = _length;
            ref var elements = ref GetElements();
            double n = value;
            if (n < 0 || n > uint.MaxValue || double.IsNaN(n))
                throw JSEngine.NewRangeError("Invalid length");
            _length = (uint)n;
            if (prev > _length)
            {
                // remove.. 
                for (uint i = _length; i < prev; i++)
                {
                    elements.RemoveAt(i);
                }
            }
            else
            {
                elements.Resize(_length);
            }
        }
    }

    public override int Length
    {
        get => (int)_length;
        set => ArrayLength = value;
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
        if (name.Key == KeyStrings.length.Key)
        {
            ArrayLength = value.DoubleValue;
            return true;
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
        if (HasIterator)
        {
            var v = this.GetValue(GetSymbols()[JSSymbol.iterator.Key]);
            return v.InvokeFunction(Arguments.Empty).GetElementEnumerator();
        }
        return new ElementEnumerator(this);
    }

    private struct ElementEnumerator(JSArray array) : IElementEnumerator
    {
        uint length = array._length;
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
                if (elements.TryGetValue(index, out var property))
                {
                    value = property.IsEmpty
                        ? null
                        : (property.IsValue
                        ? (JSValue)property.value
                        : ((JSFunction)property.get).InvokeFunction(new Arguments(array)));
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
                        : ((JSFunction)property.set).InvokeFunction(new Arguments(array)));
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
                        : ((JSFunction)property.set).InvokeFunction(new Arguments(array)));
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
            if (_length <= name && !GetInternalProperty(name, false).IsEmpty)
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
        if (index >= _length && IsLengthReadOnly())
            throw JSEngine.NewTypeError("Cannot modify property length");

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

        if ((hasConfigurable && propertyDescription[KeyStrings.configurable].BooleanValue)
            || (hasEnumerable && propertyDescription[KeyStrings.enumerable].BooleanValue))
        {
            throw JSEngine.NewTypeError("Cannot redefine array length");
        }

        if (!hasValue)
        {
            if (hasWritable)
                SetLengthWritable(propertyDescription[KeyStrings.writable].BooleanValue);

            return JSUndefined.Value;
        }

        var newLengthNumber = propertyDescription[KeyStrings.value].DoubleValue;
        if (double.IsNaN(newLengthNumber) || newLengthNumber < 0 || newLengthNumber > uint.MaxValue || newLengthNumber != System.Math.Truncate(newLengthNumber))
            throw JSEngine.NewRangeError("Invalid length");

        var newLength = (uint)newLengthNumber;
        var oldLength = _length;
        var newWritable = !hasWritable || propertyDescription[KeyStrings.writable].BooleanValue;

        if (newLength >= oldLength)
        {
            if (newLength != oldLength && IsLengthReadOnly())
                throw JSEngine.NewTypeError("Cannot modify property length");

            _length = newLength;
            SetLengthWritable(newWritable);
            return JSUndefined.Value;
        }

        if (IsLengthReadOnly())
            throw JSEngine.NewTypeError("Cannot modify property length");

        ref var elements = ref GetElements();
        for (uint index = oldLength; index > newLength; index--)
        {
            var actualIndex = index - 1;
            if (elements.TryGetValue(actualIndex, out var property) && !property.IsConfigurable)
            {
                _length = actualIndex + 1;
                SetLengthWritable(newWritable);
                throw JSEngine.NewTypeError("Cannot redefine array length");
            }

            elements.RemoveAt(actualIndex);
        }

        _length = newLength;
        SetLengthWritable(newWritable);
        return JSUndefined.Value;
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
