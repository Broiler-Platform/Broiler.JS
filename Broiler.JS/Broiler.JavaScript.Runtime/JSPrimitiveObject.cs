using Broiler.JavaScript.Storage;
using System;

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

    public override string ToString() => value.ToString();

    public override JSValue ValueOf() => value;

    public override double DoubleValue => value.DoubleValue;

    public override long BigIntValue => value.BigIntValue;

    public override bool BooleanValue => value.BooleanValue;

    public override bool ConvertTo(Type type, out object value) => this.value.ConvertTo(type, out value);

    public override JSValue CreateInstance(in Arguments a) => throw NewTypeError($"Cannot create instance of {this}");

    public override JSValue AddValue(JSValue value) => this.value.AddValue(value);

    public override JSValue AddValue(double value) => this.value.AddValue(value);

    public override JSValue AddValue(string value) => this.value.AddValue(value);

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
                && !propertyDescription[KeyStrings.value].StrictEquals(value[propertyKey.Index]))
            {
                return BooleanFalse;
            }

            return JSUndefined.Value;
        }

        return base.DefineProperty(key, propertyDescription);
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

        return base.Equals(value);
    }
}
