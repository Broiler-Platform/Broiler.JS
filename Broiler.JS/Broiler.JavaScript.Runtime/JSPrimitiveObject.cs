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

    // A boxed String exposes its characters as synthesized own index properties that
    // are not held in the backing element store, so CopyDataProperties (object spread
    // and object rest) must enumerate them through [[OwnPropertyKeys]]/[[Get]] rather
    // than the fast direct-slot copy, which would observe an empty wrapper.
    private protected override bool UseObservableSpreadCopy => value.IsString;

    internal protected override bool HasOwnProperty(in PropertyKey key)
    {
        if (value.IsString
            && ((key.Type == KeyType.UInt && key.Index < value.Length)
                || (key.Type == KeyType.String && key.KeyString.Key == KeyStrings.length.Key)))
        {
            return true;
        }

        return base.HasOwnProperty(in key);
    }

    public override JSValue GetOwnPropertyDescriptor(JSValue name)
    {
        var key = name.ToKey(false);
        if (value.IsString)
        {
            if (key.IsUInt && key.Index < value.Length)
                return JSObjectCoreExtensions.PropertyToJSValue(new JSProperty(key.Index, value[key.Index], JSPropertyAttributes.EnumerableReadonlyValue));

            if (key.Type == KeyType.String && key.KeyString.Key == KeyStrings.length.Key)
                return JSObjectCoreExtensions.PropertyToJSValue(new JSProperty(KeyStrings.length.Key, CreateNumber(value.Length), JSPropertyAttributes.ReadonlyValue));
        }

        return base.GetOwnPropertyDescriptor(name);
    }

    protected internal override JSValue GetValue(KeyString key, JSValue receiver, bool throwError = true)
    {
        if (key.Key == KeyStrings.length.Key)
        {
            if (value.IsString)
                return CreateNumber(value.Length);
        }

        return base.GetValue(key, receiver, throwError);
    }

    public override JSValue this[uint name]
    {
        get
        {
            ref var elements = ref GetElements();

            if (elements.TryGetValue(name, out var p))
                return GetValue(p);

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
                && !propertyDescription[KeyStrings.value].Is(CreateNumber(value.Length)).BooleanValue)
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

        // String exotic [[OwnPropertyKeys]] (ES 10.4.3.3): every integer-index key
        // in ascending numeric order first, then the remaining String keys in
        // creation order, then Symbols. So the string's own character indices and
        // any extra array-index own properties (e.g. `str[5] = "de"`) are gathered
        // and sorted together ahead of "length" and the other string/symbol keys —
        // emitting "length" between them is wrong (test262 15.2.3.4-4-44).
        var keys = new List<JSValue>();
        var stringKeys = new IntKeyEnumerator(value.Length);
        while (stringKeys.MoveNext(out var hasValue, out _, out var index))
        {
            // String exotic objects expose their index properties under String
            // property keys ("0", "1", ...), matching the base element enumerator
            // which stringifies indices. Emitting raw numbers here would make
            // Object.keys/entries/getOwnPropertyNames return numeric keys.
            if (hasValue)
                keys.Add(CreateString(index.ToString()));
        }

        // Partition the remaining own keys: array-index keys (which belong with the
        // ascending index run, after the character indices) versus the rest (string
        // non-index keys then symbols), preserving the base creation order of the rest.
        List<uint> extraIndexKeys = null;
        var otherKeys = new List<JSValue>();
        var ownKeys = base.GetAllKeys(showEnumerableOnly, inherited);
        while (ownKeys.MoveNext(out var hasValue, out var key, out _))
        {
            if (!hasValue)
                continue;

            var keyAsKey = key.ToKey(false);
            if (keyAsKey.Type == KeyType.UInt && keyAsKey.Index >= value.Length)
            {
                extraIndexKeys ??= [];
                extraIndexKeys.Add(keyAsKey.Index);
            }
            else
            {
                otherKeys.Add(key);
            }
        }

        if (extraIndexKeys != null)
        {
            extraIndexKeys.Sort();
            foreach (var index in extraIndexKeys)
                keys.Add(CreateString(index.ToString()));
        }

        // String exotic objects also have a non-writable, non-enumerable,
        // non-configurable own "length" property. It is synthesized by the
        // GetValue/GetOwnPropertyDescriptor overrides rather than stored, so it
        // must be surfaced here for Object.getOwnPropertyNames/Descriptors. It
        // is a String non-index key created before any user-added property, so it
        // precedes the other string keys. It is omitted when only enumerable keys
        // are requested (Object.keys etc.).
        if (!showEnumerableOnly)
            keys.Add(KeyStrings.length.ToJSValue());

        keys.AddRange(otherKeys);

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
        // OrdinaryToPrimitive: try the two methods in hint order (valueOf-then-toString for
        // number/default, toString-then-valueOf for string), each via a full prototype-chain
        // Get. When neither is callable / both return objects this throws a TypeError, exactly
        // like the spec — the wrapper does NOT silently fall back to its primitive value
        // (test262 BigInt/wrapper-object-ordinary-toprimitive: Number(Object(1n)) with valueOf
        // and toString hooked to non-callables must throw).
        var firstKey = preferString ? KeyStrings.toString : KeyStrings.valueOf;
        var secondKey = preferString ? KeyStrings.valueOf : KeyStrings.toString;

        var first = TryInvokePrimitiveMethod(in firstKey);
        if (first != null)
            return first;

        var second = TryInvokePrimitiveMethod(in secondKey);
        if (second != null)
            return second;

        throw NewTypeError("Cannot convert object to primitive value");
    }

    private JSValue TryInvokePrimitiveMethod(in KeyString key)
    {
        // OrdinaryToPrimitive (ToPrimitive) resolves valueOf/toString via Get(O, name) —
        // a full prototype-chain lookup — so an overridden valueOf/toString on the boxed
        // primitive's PROTOTYPE (e.g. a user-replaced Number.prototype.valueOf or
        // BigInt.prototype.valueOf) is observed, not just an own property. The default
        // inherited method unwraps the wrapper and returns its primitive value, so the
        // common (unhooked) case still yields `value`
        // (test262 BigInt/wrapper-object-ordinary-toprimitive).
        var method = this[key];
        if (!method.IsFunction)
            return null;

        var primitive = method.InvokeFunction(new Arguments(this));
        return primitive.IsObject ? null : primitive;
    }
}
