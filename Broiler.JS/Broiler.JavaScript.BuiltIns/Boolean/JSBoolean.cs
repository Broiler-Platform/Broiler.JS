using Broiler.JavaScript.BuiltIns.Function;
using Broiler.JavaScript.ExpressionCompiler;
using System;
using System.Runtime.CompilerServices;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.BuiltIns.Boolean;

[JSBaseClass("Object")]
[JSFunctionGenerator("Boolean")]
public partial class JSBoolean : JSPrimitive
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static JSBoolean ToBoolean(JSValue target, [CallerMemberName] string name = null)
    {
        if (target is JSBoolean boolean)
            return boolean;

        if (target is JSPrimitiveObject { value: JSBoolean primitiveBoolean })
            return primitiveBoolean;

        throw JSEngine.NewTypeError($"Boolean.prototype.{name} requires that 'this' be a Boolean");
    }

    public static JSBoolean True = new(true);
    public static JSBoolean False = new(false);

    internal readonly bool _value;

    private JSBoolean(bool _value) : base() => this._value = _value;

    [JSExport(IsConstructor = true)]
    public static JSValue Constructor(in Arguments a)
    {
        var value = (a[0]?.BooleanValue ?? false) ? True : False;
        return (JSEngine.Current as IJSExecutionContext)?.CurrentNewTarget == null
            ? value
            : new JSPrimitiveObject(value);
    }

    protected override JSValue GetPrototype() => ((JSEngine.Current as JSObject)?[Names.Boolean] as JSFunction)?.prototype;

    public override double DoubleValue => _value ? 1 : 0;

    public override bool BooleanValue => _value;

    public override bool IsBoolean => true;

    public override JSValue TypeOf() => JSConstants.Boolean;

    public override JSValue Negate() => _value ? JSNumber.MinusOne : JSNumber.NegativeZero;

    [JSPrototypeMethod]
    [JSExport("toString")]
    public static JSValue ToString(in Arguments a) => JSValue.CreateString(ToBoolean(a.This).ToString());

    [JSPrototypeMethod]
    [JSExport("valueOf")]
    public static JSValue ValueOf(in Arguments a) => ToBoolean(a.This);

    public override bool ConvertTo(Type type, out object value)
    {
        if (type == typeof(bool))
        {
            value = _value;
            return true;
        }

        if (type.IsAssignableFrom(typeof(JSBoolean)))
        {
            value = this;
            return true;
        }

        if (type == typeof(object))
        {
            value = _value;
            return true;
        }

        value = null;
        return false;
    }

    public override string ToString() => _value ? "true" : "false";

    public override int GetHashCode() => _value ? 1 : 0;

    public override bool Equals(object obj)
    {
        if (obj is JSBoolean b)
            return _value == b._value;

        return base.Equals(obj);
    }

    public override bool Equals(JSValue value)
    {
        if (ReferenceEquals(this, value))
            return true;

        if (value.IsObject)
            return value.Equals(this);

        if (!_value)
        {
            if (value.IsNullOrUndefined)
                return false;
        }

        if (_value)
        {
            if (value.DoubleValue == 1)
                return true;
        }
        else
        {
            if (value.DoubleValue == 0)
                return true;
        }

        return false;
    }

    public override bool EqualsLiteral(double value) => _value ? value == 1 : value == 0;

    public override bool EqualsLiteral(string value) => _value ? value == "1" : value == "0";

    public override bool StrictEquals(JSValue value) => ReferenceEquals(this, value);

    public override JSValue InvokeFunction(in Arguments a) => throw JSEngine.NewTypeError("boolean is not a function");

    internal override PropertyKey ToKey(bool create = false) => _value ? KeyStrings.@true : KeyStrings.@false;
}
