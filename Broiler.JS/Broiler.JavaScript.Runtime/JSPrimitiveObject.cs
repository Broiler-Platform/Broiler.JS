using Broiler.JavaScript.Storage;
using System;
using System.Collections.Generic;

namespace Broiler.JavaScript.Runtime;

public class JSPrimitiveObject : JSObject
{
    internal readonly JSValue value;

    public JSPrimitiveObject(JSPrimitive value) : base(GetCurrentObjectPrototype?.Invoke())
    {
        this.value = value;
        value.ResolvePrototype();
        prototypeChain = value.prototypeChain;
    }

    public override string ToString() => CoerceOwnOverrides(preferString: true).ToString();

    public override JSValue ValueOf() => CoerceOwnOverrides(preferString: false);

    public override double DoubleValue => value.DoubleValue;

    public override long BigIntValue => value.BigIntValue;

    public override bool BooleanValue => true;

    public override bool ConvertTo(Type type, out object value) => this.value.ConvertTo(type, out value);

    public override JSValue CreateInstance(in Arguments a) => throw NewTypeError($"Cannot create instance of {this}");

    public override JSValue AddValue(JSValue value) => CoerceOwnOverrides(preferString: false).AddValue(value);

    public override JSValue AddValue(double value) => CoerceOwnOverrides(preferString: false).AddValue(value);

    public override JSValue AddValue(string value) => CoerceOwnOverrides(preferString: false).AddValue(value);

    public override JSValue GetOwnPropertyDescriptor(JSValue name)
    {
        var key = name.ToKey(false);
        if (value.IsString)
        {
            if (key.IsUInt && key.Index < value.Length)
                return JSObjectCoreExtensions.PropertyToJSValue(new JSProperty(key.Index, value[key.Index], JSPropertyAttributes.EnumerableReadonlyValue));

            if (key.Type == KeyType.String && key.KeyString.Key == KeyStrings.length.Key)
                return JSObjectCoreExtensions.PropertyToJSValue(new JSProperty(KeyStrings.length.Key, JSValue.CreateNumber(value.Length), JSPropertyAttributes.ReadonlyValue));
        }

        return base.GetOwnPropertyDescriptor(name);
    }

    protected internal override JSValue GetValue(KeyString key, JSValue receiver, bool throwError = true)
    {
        if (key.Key == KeyStrings.length.Key)
        {
            if (value.IsString)
                return JSValue.CreateNumber(value.Length);
        }

        return base.GetValue(key, receiver, throwError);
    }

    public override JSValue this[uint name]
    {
        get
        {
            ref var elements = ref GetElements();

            if (elements.TryGetValue(name, out var p))
                return this.GetValue(p);

            return value[name];
        }
        set
        {
            if (value.IsString)
            {
                if (name < value.Length)
                    return;
            }

            base[name] = value;
        }
    }

    public override JSValue GetValue(uint name, JSValue receiver, bool throwError = true)
    {
        // String exotic objects expose their characters as own index properties.
        // The this[uint] indexer handles direct C# access, but generic value
        // reads (e.g. Object.entries/values via the JSValue indexer) route
        // through GetValue(uint, ...), so mirror the character lookup here.
        if (value.IsString && name < value.Length)
            return value[name];

        return base.GetValue(name, receiver, throwError);
    }

    public override bool SetValue(uint name, JSValue value, JSValue receiver, bool throwError = true)
    {
        if (this.value.IsString && name < this.value.Length)
        {
            if (throwError)
                throw NewTypeError($"Cannot modify property {name} of {this}");

            return false;
        }

        return base.SetValue(name, value, receiver, throwError);
    }

    internal protected override bool SetValue(KeyString name, JSValue value, JSValue receiver, bool throwError = true)
    {
        if (this.value.IsString && name.Key == KeyStrings.length.Key)
        {
            if (throwError)
                throw NewTypeError($"Cannot modify property length of {this}");

            return false;
        }

        return base.SetValue(name, value, receiver, throwError);
    }

    public override JSValue DefineProperty(JSValue key, JSObject propertyDescription)
    {
        var propertyKey = key.ToKey();

        // String exotic objects have a synthesized own "length" property that is
        // non-writable, non-enumerable and non-configurable. It is not stored, so
        // redefining it (e.g. by Object.freeze/seal) must be validated here rather
        // than falling through to the ordinary store, which would reject it on a
        // non-extensible object with "Cannot define property".
        if (value.IsString && propertyKey.Type == KeyType.String && propertyKey.KeyString.Key == KeyStrings.length.Key)
        {
            if (!propertyDescription.GetInternalProperty(KeyStrings.configurable, false).IsEmpty
                && propertyDescription[KeyStrings.configurable].BooleanValue)
            {
                return BooleanFalse;
            }

            if (!propertyDescription.GetInternalProperty(KeyStrings.enumerable, false).IsEmpty
                && propertyDescription[KeyStrings.enumerable].BooleanValue)
            {
                return BooleanFalse;
            }

            if (!propertyDescription.GetInternalProperty(KeyStrings.writable, false).IsEmpty
                && propertyDescription[KeyStrings.writable].BooleanValue)
            {
                return BooleanFalse;
            }

            if (!propertyDescription.GetInternalProperty(KeyStrings.get, false).IsEmpty
                || !propertyDescription.GetInternalProperty(KeyStrings.set, false).IsEmpty)
            {
                return BooleanFalse;
            }

            if (!propertyDescription.GetInternalProperty(KeyStrings.value, false).IsEmpty
                && !propertyDescription[KeyStrings.value].Is(JSValue.CreateNumber(value.Length)).BooleanValue)
            {
                return BooleanFalse;
            }

            return JSUndefined.Value;
        }

        if (value.IsString && propertyKey.IsUInt && propertyKey.Index < value.Length)
        {
            if (!propertyDescription.GetInternalProperty(KeyStrings.configurable, false).IsEmpty
                && propertyDescription[KeyStrings.configurable].BooleanValue)
            {
                return BooleanFalse;
            }

            if (!propertyDescription.GetInternalProperty(KeyStrings.enumerable, false).IsEmpty
                && !propertyDescription[KeyStrings.enumerable].BooleanValue)
            {
                return BooleanFalse;
            }

            if (!propertyDescription.GetInternalProperty(KeyStrings.writable, false).IsEmpty
                && propertyDescription[KeyStrings.writable].BooleanValue)
            {
                return BooleanFalse;
            }

            if (!propertyDescription.GetInternalProperty(KeyStrings.get, false).IsEmpty
                || !propertyDescription.GetInternalProperty(KeyStrings.set, false).IsEmpty)
            {
                return BooleanFalse;
            }

            if (!propertyDescription.GetInternalProperty(KeyStrings.value, false).IsEmpty
                && !propertyDescription[KeyStrings.value].Is(value[propertyKey.Index]).BooleanValue)
            {
                return BooleanFalse;
            }

            return JSUndefined.Value;
        }

        return base.DefineProperty(key, propertyDescription);
    }

    public override IElementEnumerator GetAllKeys(bool showEnumerableOnly = true, bool inherited = true)
    {
        ((JSPrimitive)value).ResolvePrototype();

        prototypeChain = value.prototypeChain;

        if (!value.IsString)
            return base.GetAllKeys(showEnumerableOnly, inherited);

        var keys = new List<JSValue>();
        var stringKeys = new IntKeyEnumerator(value.Length);
        while (stringKeys.MoveNext(out var hasValue, out _, out var index))
        {
            // String exotic objects expose their index properties under String
            // property keys ("0", "1", ...), matching the base element enumerator
            // which stringifies indices. Emitting raw numbers here would make
            // Object.keys/entries/getOwnPropertyNames return numeric keys.
            if (hasValue)
                keys.Add(JSValue.CreateString(index.ToString()));
        }

        // String exotic objects also have a non-writable, non-enumerable,
        // non-configurable own "length" property. It is synthesized by the
        // GetValue/GetOwnPropertyDescriptor overrides rather than stored, so it
        // must be surfaced here for Object.getOwnPropertyNames/Descriptors. It
        // is omitted when only enumerable keys are requested (Object.keys etc.).
        if (!showEnumerableOnly)
            keys.Add(KeyStrings.length.ToJSValue());

        var ownKeys = base.GetAllKeys(showEnumerableOnly, inherited);
        while (ownKeys.MoveNext(out var hasValue, out var key, out _))
        {
            if (hasValue)
                keys.Add(key);
        }

        return new ListElementEnumerator(keys.GetEnumerator());
    }

    public override JSValue Delete(in KeyString key)
    {
        if (value.IsString && key.Key == KeyStrings.length.Key)
            return BooleanFalse;

        return base.Delete(key);
    }

    public override JSValue Delete(uint key)
    {
        if (value.IsString && key < value.Length)
            return BooleanFalse;

        return base.Delete(key);
    }

    /// <summary> Added for below TCs in ExpressionTests.cs
    /// Assert.AreEqual(false, Evaluate("var x = new Number(10); x == new Number(10)"));
    // Assert.AreEqual(true, Evaluate("var x = new Number(10); x == x"));
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>

    public override bool Equals(JSValue value)
    {
        if (ReferenceEquals(this, value))
            return true;

        if (value is JSPrimitiveObject)
            return false;

        return CoerceOwnOverrides(preferString: false).Equals(value);
    }

    public override bool EqualsLiteral(double value) => CoerceOwnOverrides(preferString: false).EqualsLiteral(value);

    public override bool EqualsLiteral(string value) => CoerceOwnOverrides(preferString: false).EqualsLiteral(value);

    private JSValue CoerceOwnOverrides(bool preferString)
    {
        var methodKey = preferString ? KeyStrings.toString : KeyStrings.valueOf;
        var overridden = TryInvokeOwnPrimitiveMethod(in methodKey);
        if (overridden != null)
            return overridden;

        return value;
    }

    private JSValue TryInvokeOwnPrimitiveMethod(in KeyString key)
    {
        var descriptor = GetOwnPropertyDescriptor(JSValue.CreateString(key.Value.Value));
        if (descriptor.IsUndefined)
            return null;

        var method = descriptor[KeyStrings.value];
        if (!method.IsFunction)
            return null;

        var primitive = method.InvokeFunction(new Arguments(this));
        return primitive.IsObject ? null : primitive;
    }
}
