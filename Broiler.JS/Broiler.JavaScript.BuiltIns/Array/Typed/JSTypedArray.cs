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

    [JSExport(Length = 1)]
    private static JSValue From(in Arguments a) => a.This.InvokeMethod(Names.from, a);

    [JSExport]
    private static JSValue Of(in Arguments a) => a.This.InvokeMethod(Names.of, a);

    [JSExport]
    internal readonly JSArrayBuffer buffer;
    [JSExport]
    public readonly int byteOffset;

    [JSExport("BYTES_PER_ELEMENT")]
    internal readonly int bytesPerElement;

    [JSExport]
    internal readonly int length;

    [JSExport]
    internal int ByteLength => buffer.buffer.Length;
    
    public override int Length { get => length; set => throw new NotSupportedException(); }
    public bool HasIntegerIndexedElements => length > 0;

    public JSTypedArray(in Arguments a) : this(JSEngine.NewTargetPrototype) => throw new NotSupportedException();

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
        if (length == -1
            && source is JSObject arrayLike
            && IsNonIterableArrayLike(source))
        {
            length = arrayLike.Length;
            copyByIndex = length >= 0;
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

        return typedArray;
    }

    internal static JSValue GetSpeciesConstructor(JSTypedArray source)
    {
        var constructor = source[KeyStrings.constructor];
        if (!constructor.IsObject)
            return constructor;

        var species = constructor[(IJSSymbol)JSSymbol.species];
        if (species.IsNullOrUndefined)
            return constructor;

        if (species is not IJSFunction)
            throw JSEngine.NewTypeError("TypedArray species constructor is not a constructor");

        return species;
    }

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
        var hasValue = !pd.GetInternalProperty(KeyStrings.value, false).IsEmpty;
        if (hasValue)
            ValidateElementValue(pd[KeyStrings.value]);

        if (key >= length)
            return JSBoolean.False;

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
            var hasValue = !pd.GetInternalProperty(KeyStrings.value, false).IsEmpty;
            if (hasValue)
                ValidateElementValue(pd[KeyStrings.value]);

            return IsValidIntegerIndex(numericIndex) ? DefineProperty((uint)numericIndex, pd) : JSBoolean.False;
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

    public override IElementEnumerator GetAllKeys(bool showEnumerableOnly = true, bool inherited = true) => new IntKeyEnumerator(length);

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
