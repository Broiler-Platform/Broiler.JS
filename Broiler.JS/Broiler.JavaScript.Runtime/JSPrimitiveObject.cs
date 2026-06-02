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
        while (stringKeys.MoveNext(out var hasValue, out var key, out _))
        {
            if (hasValue)
                keys.Add(key);
        }

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
