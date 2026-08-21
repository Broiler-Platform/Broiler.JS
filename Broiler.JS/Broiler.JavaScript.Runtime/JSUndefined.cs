using Broiler.JavaScript.Storage;
using System;
using System.Globalization;

namespace Broiler.JavaScript.Runtime;

public sealed class JSUndefined : JSValue
{
    private JSUndefined() : base(null) { }

    public static JSValue Value = new JSUndefined();

    internal override PropertyKey ToKey(bool create = true) => KeyStrings.undefined;

    public override JSValue TypeOf() => JSConstants.Undefined;

    public override bool BooleanValue => false;

    public override double DoubleValue => double.NaN;

    public override uint UIntValue => 0;

    public override int IntegerValue => 0;

    public override int IntValue => 0;

    public override string ToString() => "undefined";

    public override JSValue this[KeyString name]
    {
        get
        {
#if DEBUG
            if (JSException.LogThrows)
            {
                var st = new System.Diagnostics.StackTrace(true);
                Console.Error.WriteLine($"[JSUndefined] Cannot get property {name} of undefined");
                Console.Error.WriteLine(st.ToString());
            }
#endif
            // NullishAccess.Evaluating names the expression the access was written as — the half
            // "Cannot get property enabled of undefined" never said, and the half that identifies
            // WHICH value was undefined. Empty for an access the compiler emitted no description
            // for; see NullishAccess for what carries one.
            throw NewTypeError($"Cannot get property {name} of undefined{NullishAccess.Evaluating(in name)}");
        }
        set => throw NewTypeError($"Cannot set property {name} of undefined{NullishAccess.Evaluating(in name)}");
    }

    public override JSValue this[uint key]
    {
        get => throw NewTypeError($"Cannot get property {key} of undefined");
        set => throw NewTypeError($"Cannot set property {key} of undefined");
    }

    internal override JSFunctionDelegate GetMethod(in KeyString key) => throw NewTypeError($"Cannot get property {key} of undefined");

    public override JSValue Delete(in KeyString key) => throw NewTypeError(JSObject.Cannot_convert_undefined_or_null_to_object);

    public override JSValue Delete(uint key) => throw NewTypeError(JSObject.Cannot_convert_undefined_or_null_to_object);

    public override bool Equals(JSValue value) => value.IsNullOrUndefined;//if (value.IsUndefined)//    return true;//return false;

    public override bool StrictEquals(JSValue value) => ReferenceEquals(this, value);

    // "X is not a constructor" is what every browser reports for `new undefined()`, and what this
    // engine already reports at each of its other construct sites (JSFunction, JSSymbol, JSGenerator,
    // JSReflect). "cannot create instance of undefined" was the odd one out, and it is the message a
    // reader meets in a feature-probe trace — `new (window.RTCPeerConnection || ...)()` against an
    // engine with no WebRTC — where a non-standard wording reads as an engine fault rather than as the
    // expected answer.
    public override JSValue CreateInstance(in Arguments a) => throw NewTypeError("undefined is not a constructor");

    public override JSValue InvokeFunction(in Arguments a) => throw NewTypeError("undefined is not a function");

    public override IElementEnumerator GetElementEnumerator() => throw NewTypeError("undefined is not iterable");

    // Spread and for-of enter through GetIterableEnumerator, not GetElementEnumerator, so
    // `[...undefined]` reported the base's "Value is not iterable" — a sentence that names
    // neither the value nor its type, and the one not-iterable message in the engine that did
    // not. `undefined` renders without running any JavaScript, so it can simply be said.
    public override IElementEnumerator GetIterableEnumerator() => throw NewTypeError("undefined is not iterable");

    public override bool ConvertTo(Type type, out object value)
    {
        if (type.IsAssignableFrom(typeof(JSUndefined)))
        {
            value = this;
            return true;
        }

        value = null;
        return !type.IsValueType;
    }

    public override string ToLocaleString(string format, CultureInfo culture) => "";
}
