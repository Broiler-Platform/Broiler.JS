using Broiler.JavaScript.BuiltIns.Function;
using Broiler.JavaScript.ExpressionCompiler;
using System;
using System.Runtime.CompilerServices;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.BuiltIns.Boolean;

// Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=2; Fingerprint=TBF
// Broiler-Falsified-If: a boolean compared loosely with a string literal such as the empty string or "1.0" gives a different answer from the same comparison against a string variable
// Broiler-Human:        PENDING
[JSBaseClass("Object")]
[JSFunctionGenerator("Boolean")]
public partial class JSBoolean : JSPrimitive
{
    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=0; Fingerprint=TBF
    // Broiler-Falsified-If: Boolean.prototype.valueOf or toString called on a plain object or a Number wrapper returns a boolean instead of throwing a TypeError
    // Broiler-Human:        PENDING
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static JSBoolean ToBoolean(JSValue target, [CallerMemberName] string name = null)
    {
        if (target is JSBoolean boolean)
            return boolean;

        if (target is JSPrimitiveObject { value: JSBoolean primitiveBoolean })
            return primitiveBoolean;

        // Boolean.prototype itself has a [[BooleanData]] internal slot whose value
        // is false (§20.3.3), so valueOf/toString invoked on it must succeed.
        if (target is JSObject @object
            && ReferenceEquals(@object, Intrinsics.Prototype(Names.Boolean)))
            return False;

        throw JSEngine.NewTypeError($"Boolean.prototype.{name} requires that 'this' be a Boolean");
    }

    public static JSBoolean True = new(true);
    public static JSBoolean False = new(false);

    internal readonly bool _value;

    private JSBoolean(bool _value) : base() => this._value = _value;

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s20.3.1.1; IP=Low; Security=Medium; Resources=1; Fingerprint=TBF
    // Broiler-Falsified-If: Boolean(x) called without new returns a wrapper object, or new Boolean(x) returns a primitive
    // Broiler-Human:        PENDING
    [JSExport(IsConstructor = true)]
    public static JSValue Constructor(in Arguments a)
    {
        var value = (a[0]?.BooleanValue ?? false) ? True : False;
        return (JSEngine.Current as IJSExecutionContext)?.CurrentNewTarget == null
            ? value
            : new JSPrimitiveObject(value);
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=0; Fingerprint=TBF
    // Broiler-Falsified-If: a property read on a primitive boolean resolves against a prototype other than the current realm's Boolean.prototype
    // Broiler-Human:        PENDING
    protected override JSValue GetPrototype() => Intrinsics.Prototype(Names.Boolean);

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Low; Resources=0; Fingerprint=TBF
    // Broiler-Falsified-If: ToNumber of true yields a value other than 1, or of false a value other than +0
    // Broiler-Human:        PENDING
    public override double DoubleValue => _value ? 1 : 0;

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Low; Resources=0; Fingerprint=TBF
    // Broiler-Human:        PENDING
    public override bool BooleanValue => _value;

    public override bool IsBoolean => true;

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Low; Resources=0; Fingerprint=TBF
    // Broiler-Falsified-If: typeof applied to a primitive boolean yields a string other than "boolean"
    // Broiler-Human:        PENDING
    public override JSValue TypeOf() => JSConstants.Boolean;

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Low; Resources=0; Fingerprint=TBF
    // Broiler-Falsified-If: unary minus applied to false yields +0 instead of -0
    // Broiler-Human:        PENDING
    public override JSValue Negate() => _value ? JSNumber.MinusOne : JSNumber.NegativeZero;

    [JSPrototypeMethod]
    [JSExport("toString")]
    public static JSValue ToString(in Arguments a) => CreateString(ToBoolean(a.This).ToString());

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s20.3.3.3; IP=Low; Security=Medium; Resources=0; Fingerprint=TBF
    // Broiler-Falsified-If: Boolean.prototype.valueOf called on new Boolean(true) returns the wrapper object instead of the primitive true
    // Broiler-Human:        PENDING
    [JSPrototypeMethod]
    [JSExport("valueOf")]
    public static JSValue ValueOf(in Arguments a) => ToBoolean(a.This);

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=0; Fingerprint=TBF
    // Broiler-Falsified-If: a conversion request for a CLR type that is not bool, object or a base of JSBoolean (for example int or string) reports success
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Low; Resources=0; Fingerprint=TBF
    // Broiler-Human:        PENDING
    public override string ToString() => _value ? "true" : "false";

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Low; Resources=0; Fingerprint=TBF
    // Broiler-Falsified-If: two JSBoolean values that Equals treats as equal return different hash codes
    // Broiler-Human:        PENDING
    public override int GetHashCode() => _value ? 1 : 0;

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Low; Resources=0; Fingerprint=TBF
    // Broiler-Falsified-If: two distinct JSBoolean instances holding the same value compare unequal through object equality
    // Broiler-Human:        PENDING
    public override bool Equals(object obj)
    {
        if (obj is JSBoolean b)
            return _value == b._value;

        return base.Equals(obj);
    }

    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 s7.2.14; IP=Medium; Security=Medium; Resources=2; Fingerprint=TBF
    // Broiler-Falsified-If: true compared loosely with a Symbol throws a TypeError instead of evaluating to false
    // Broiler-Human:        PENDING
    public override bool Equals(JSValue value)
    {
        if (ReferenceEquals(this, value))
            return true;

        if (value.IsObject)
            return value.Equals(this);

        // Boolean == BigInt becomes ToNumber(Boolean) == BigInt; let the BigInt
        // side perform the mathematical-value comparison rather than reading
        // BigInt.DoubleValue, which throws.
        if (value.IsBigInt)
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

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=0; Fingerprint=TBF
    // Broiler-Falsified-If: true compared loosely with the numeric literal 1, or false with the literal -0, evaluates to false
    // Broiler-Human:        PENDING
    public override bool EqualsLiteral(double value) => _value ? value == 1 : value == 0;

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=0; Fingerprint=TBF
    // Broiler-Falsified-If: false compared loosely with the empty string literal, or true with the literal "1.0", evaluates to false although ToNumber of those strings gives 0 and 1
    // Broiler-Human:        PENDING
    public override bool EqualsLiteral(string value) => _value ? value == "1" : value == "0";

    public override bool StrictEquals(JSValue value) => ReferenceEquals(this, value);

    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=0; Fingerprint=TBF
    // Broiler-Falsified-If: calling a primitive boolean as a function returns a value instead of throwing a TypeError
    // Broiler-Human:        PENDING
    public override JSValue InvokeFunction(in Arguments a) => throw JSEngine.NewTypeError("boolean is not a function");

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Low; Resources=0; Fingerprint=TBF
    // Broiler-Falsified-If: a boolean used as a property key addresses a key other than "true" or "false"
    // Broiler-Human:        PENDING
    internal override PropertyKey ToKey(bool create = false) => _value ? KeyStrings.@true : KeyStrings.@false;
}
