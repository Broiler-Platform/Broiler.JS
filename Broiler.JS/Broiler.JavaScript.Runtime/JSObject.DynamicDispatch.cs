using Broiler.JavaScript.Storage;
using System;
using System.Text;
namespace Broiler.JavaScript.Runtime;

public partial class JSObject
{
    public override JSValue TypeOf() => JSConstants.Object;
    public override JSValue ValueOf()
    {
        var p = GetInternalProperty(KeyStrings.valueOf, false);
        if (!p.IsEmpty)
            return GetValue(p).InvokeFunction(new Arguments(this));

        return this;
    }

    public bool HasValueOf(out JSValue value)
    {
        var p = GetInternalProperty(KeyStrings.valueOf, false);
        if (!p.IsEmpty)
        {
            value = GetValue(p).InvokeFunction(new Arguments(this));
            return true;
        }

        value = null;
        return false;

    }
    public override string ToString()
    {
        var px = GetMethod(KeyStrings.toString);
        if (px != null)
        {
            if (toStringCalled)
                return "Stack overflow";

            toStringCalled = true;

            var v = px(new Arguments(this));
            if (v != this)
            {
                toStringCalled = false;
                return v.ToString();
            }
        }

        return "[object Object]";
    }

    // prevent recursive...
    public override string ToDetailString()
    {
        var sb = new StringBuilder();
        bool first = true;
        sb.Append('{');

        foreach (var (Key, Value) in this.GetAllEntries(false))
        {
            if (Value == this)
                continue;
            if (!first)
                sb.Append(", ");
            first = false;
            sb.Append($"{Key}: {Value?.ToDetailString()}");
        }

        sb.Append('}');
        return sb.ToString();
    }
    public override bool Equals(JSValue value)
    {
        if (ReferenceEquals(this, value))
            return true;

        if (value.IsObject)
            return false;

        return ToPrimitiveDefault().Equals(value);
    }

    public override bool EqualsLiteral(double value) => Equals(CreateNumber(value));

    public override bool EqualsLiteral(string value) => Equals(CreateString(value));

    public override bool StrictEquals(JSValue value) => ReferenceEquals(this, value);

    public override JSValue InvokeFunction(in Arguments a) => throw NewTypeError("Object is not a function");

    // Relational operators coerce with the Number hint (not the "default" hint used by
    // ==): for a Date `<`/`>` compares timestamps, while `==` compares strings.
    public override bool Less(JSValue value)
        => ToNumberPrimitive().Less(value);

    public override bool LessOrEqual(JSValue value)
        => ToNumberPrimitive().LessOrEqual(value);

    public override bool Greater(JSValue value)
        => ToNumberPrimitive().Greater(value);

    public override bool GreaterOrEqual(JSValue value)
        => ToNumberPrimitive().GreaterOrEqual(value);
    public override bool ConvertTo(Type type, out object value)
    {
        if (TryGetClrEnumeratorFunc?.Invoke(this, type, out value) ?? false)
            return true;

        if (type == typeof(object))
        {
            value = this;
            return true;
        }

        if (type != typeof(Type))
        {
            // if type has default constructor...
            if (TryUnmarshalObject?.Invoke(this, type, out value) ?? false)
                return true;
        }

        return base.ConvertTo(type, out value);
    }
}
