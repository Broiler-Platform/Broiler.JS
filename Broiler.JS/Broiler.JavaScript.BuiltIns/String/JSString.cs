using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.Ast.Misc;
using System;
using System.Globalization;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.BuiltIns.Function;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.String;

[JSBaseClass("Object")]
[JSFunctionGenerator("String")]
public partial class JSString : JSPrimitive
{
    internal static JSString Empty = new(string.Empty);

    internal readonly string value;

    /// <summary>
    /// Gets the underlying string value of this JSString instance.
    /// </summary>
    public new string StringValue => value;

    KeyString _keyString;

    private double NumberValue = 0;
    private bool NumberParsed = false;

    public override double DoubleValue
    {
        get
        {
            if (NumberParsed)
                return NumberValue;

            NumberValue = NumberParser.CoerceToNumber(value);
            NumberParsed = true;

            return NumberValue;
        }
    }

    public override bool BooleanValue => value.Length > 0;
    public override long BigIntValue => long.TryParse(ToString(), out var n) ? n : 0;
    public override bool IsString => true;

    public override JSValue AddValue(double value)
    {
        var numStr = JSValue.NumberToECMAString(value);

        if (this.value.IsEmpty())
            return new JSString(numStr);

        return new JSString(string.Concat(this.value, numStr));
    }

    public override JSValue AddValue(string value)
    {
        if (this.value.IsEmpty())
            return new JSString(value);

        return new JSString(string.Concat(this.value, value));
    }

    public override JSValue AddValue(JSValue value)
    {
        if (value is JSString vString)
        {
            if (this.value.IsEmpty())
                return vString;

            if (vString.value.IsEmpty())
                return this;

            return new JSString(string.Concat(this.value, vString.value));
        }

        // `string + obj` coerces the object with ToPrimitive (default hint), not a
        // string-forcing ValueOf() — so an overridden valueOf / @@toPrimitive that
        // yields a number (e.g. a boxed Symbol) is honoured before stringification.
        if (value is JSObject valueObject)
            value = valueObject.ToDefaultPrimitive();

        if (this.value.IsEmpty())
            return new JSString(value.StringValue);

        var v = value.StringValue;
        if (v.Length == 0)
            return this;

        return new JSString(string.Concat(this.value, v));
    }

    public override bool ConvertTo(Type type, out object value)
    {
        if (type == typeof(string))
        {
            value = this.value;
            return true;
        }

        if (type == typeof(object))
        {
            value = this.value;
            return true;
        }

        if (type == typeof(char))
        {
            value = this.value[0];
            return true;
        }

        if (type.IsAssignableFrom(typeof(JSString)))
        {
            value = this;
            return true;
        }

        value = null;
        return false;
    }

    internal override PropertyKey ToKey(bool create = true)
    {
        if (_keyString.HasValue)
            return _keyString;

        if (NumberParser.TryGetArrayIndex(value, out var index))
            return index;

        if (!create)
        {
            if (!KeyStrings.TryGet(value, out _keyString))
                _keyString = KeyStrings.GetOrCreate(value);

            return _keyString;
        }

        return _keyString.Value != null ? _keyString : (_keyString = KeyStrings.GetOrCreate(value));
    }

    protected override JSValue GetPrototype() => ((JSEngine.Current as JSObject)?[Names.String] as JSFunction).prototype;

    public JSString(string value) : base() => this.value = value;
    public JSString(JSObject prototype, string value) : base(prototype) => this.value = value;

    public JSString(in StringSpan value) : base() => this.value = value.Value;


    public JSString(char ch) : this(new string(ch, 1)) { }


    public JSString(in StringSpan value, KeyString keyString) : this(value) => _keyString = keyString;

    public static implicit operator KeyString(JSString value) => value.ToString();

    public override JSValue TypeOf() => JSConstants.String;

    public override string ToString() => value;

    public byte[] Encode(System.Text.Encoding encoding) => encoding.GetBytes(value);

    public override string ToDetailString() => value;

    public override string ToLocaleString(string format, CultureInfo culture) => value;

    public override JSValue Delete(in KeyString key)
    {
        if (key.Key == KeyStrings.length.Key)
            return BooleanFalse;

        return base.Delete(key);
    }

    public override JSValue Delete(uint key)
    {
        if (key < value.Length)
            return BooleanFalse;

        return base.Delete(key);
    }

    public override JSValue GetValue(uint key, JSValue receiver, bool throwError = true)
    {
        if (key >= value.Length)
            return JSUndefined.Value;

        return new JSString(new string(value[(int)key], 1));
    }

    internal protected override JSValue GetValue(KeyString key, JSValue receiver, bool throwError = true)
    {
        // A primitive string is a String exotic object with its own non-configurable
        // "length". Resolve it here so a dynamic `s["length"]` read returns the string's
        // own length rather than falling through to String.prototype, which is itself a
        // String exotic whose [[StringData]] is "" (length 0). The static `s.length`
        // path already resolved locally; this keeps the dynamic-key path consistent.
        if (key.Key == KeyStrings.length.Key)
            return JSValue.CreateNumber(value.Length);

        return base.GetValue(key, receiver, throwError);
    }

    // Property-key enumeration (for-in, Object.keys, spread) must yield the index
    // properties as String keys ("0", "1", …), not Numbers — otherwise destructuring
    // a for-in key (`for (var {length:x} in "foo")`) reads `.length` off a number.
    public override IElementEnumerator GetAllKeys(bool showEnumerableOnly = true, bool inherited = true) => new StringIndexKeyEnumerator(Length);

    [JSExport]
    public override int Length => value.Length;

    public override int GetHashCode() => value.GetHashCode();

    public override bool Equals(object obj)
    {
        if (obj is JSString v)
            return value == v.value;

        return base.Equals(obj);
    }

    public override bool Equals(JSValue value)
    {
        if (ReferenceEquals(this, value))
            return true;

        if (value.IsObject)
            return value.Equals(this);

        switch (value)
        {
            case JSString strValue:
                if (this.value == strValue.value)
                    return true;
                return false;

            case JSValue number
                when number.IsNumber
                    && (DoubleValue == number.DoubleValue
                        || this.value.CompareTo(number.DoubleValue.ToString()) == 0):
                return true;

            case JSValue boolVal when boolVal.IsBoolean && DoubleValue == (boolVal.BooleanValue ? 1D : 0D):
                return true;
        }

        return false;
    }

    public override bool EqualsLiteral(double value) => DoubleValue == value || this.value.CompareTo(value.ToString()) == 0;

    public override bool EqualsLiteral(string value) => this.value.Equals(value);

    public override bool StrictEqualsLiteral(string value) => this.value.Equals(value);

    // Abstract Relational Comparison (ECMA-262): the other operand is first coerced to a
    // primitive with the Number hint (valueOf-then-toString), not the String hint. Only when
    // BOTH operands end up Strings is a code-point string comparison performed; otherwise the
    // operands are compared numerically. Coercing an object via ToString here would wrongly
    // pick toString and force a string comparison (e.g. "-1" < {valueOf:()=>-2}).
    public override bool Less(JSValue value)
    {
        var py = value is JSObject o ? o.ToNumberPrimitive() : value;

        if (py.IsUndefined)
            return false;

        if (py.IsBigInt)
            return py.Greater(this);

        if (py.IsString)
            return this.value.Less(py.StringValue);

        return DoubleValue < py.DoubleValue;
    }

    public override bool LessOrEqual(JSValue value)
    {
        var py = value is JSObject o ? o.ToNumberPrimitive() : value;

        if (py.IsUndefined)
            return false;

        if (py.IsBigInt)
            return py.GreaterOrEqual(this);

        if (py.IsString)
            return this.value.LessOrEqual(py.StringValue);

        return DoubleValue <= py.DoubleValue;
    }

    public override bool Greater(JSValue value)
    {
        var py = value is JSObject o ? o.ToNumberPrimitive() : value;

        if (py.IsUndefined)
            return false;

        if (py.IsBigInt)
            return py.Less(this);

        if (py.IsString)
            return this.value.Greater(py.StringValue);

        return DoubleValue > py.DoubleValue;
    }

    public override bool GreaterOrEqual(JSValue value)
    {
        var py = value is JSObject o ? o.ToNumberPrimitive() : value;

        if (py.IsUndefined)
            return false;

        if (py.IsBigInt)
            return py.LessOrEqual(this);

        if (py.IsString)
            return this.value.GreaterOrEqual(py.StringValue);

        return DoubleValue >= py.DoubleValue;
    }

    public override bool StrictEquals(JSValue value)
    {
        if (ReferenceEquals(this, value))
            return true;

        if (value is JSString s)
            if (s.value.Equals(this.value))
                return true;

        return false;
    }

    public override JSValue InvokeFunction(in Arguments a) => throw JSEngine.NewTypeError($"\"{value}\" is not a function");

    internal override JSValue Is(JSValue value)
    {
        if (value is JSString @string && this.value == @string.value)
            return JSValue.BooleanTrue;

        return JSValue.BooleanFalse;
    }

    // Array-like / property-index access enumerates by UTF-16 code unit (used by
    // for-in, Object.keys, etc.).
    public override IElementEnumerator GetElementEnumerator() => new ElementEnumerator(value);

    // MethodInfo of the built-in String iterator (JSStringPrototype `Iterator`,
    // installed as String.prototype[@@iterator]). Used to recognise — and keep
    // the fast path for — the default iterator when it has not been replaced.
    private static readonly System.Reflection.MethodInfo DefaultIteratorMethod =
        typeof(JSString).GetMethod(nameof(Iterator),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

    // The iteration protocol (for-of, spread, destructuring, Array.from, Map/Set,
    // ...) enumerates a String by Unicode code point, per the built-in String
    // Iterator. If user code has replaced String.prototype[@@iterator], that
    // override must be honoured instead (e.g. test262 sm/Array/from_string and a
    // spread/`for-of` over a string with a custom String.prototype iterator).
    public override IElementEnumerator GetIterableEnumerator()
    {
        var symbolIterator = JSValue.SymbolIterator;
        if (symbolIterator != null)
        {
            var iterator = this[symbolIterator];

            // Fall back to the @@iterator protocol only when the method is not the
            // built-in code-point iterator (the common, hot path stays direct).
            if (!IsDefaultStringIterator(iterator))
            {
                if (iterator.IsNullOrUndefined)
                    throw NewTypeError(JSException.NotIterable(this));
                if (!iterator.IsFunction)
                    throw NewTypeError("@@iterator is not a function");

                var iteratorResult = iterator.InvokeFunction(new Arguments(this));
                if (!iteratorResult.IsObject)
                    throw NewTypeError("@@iterator result is not an object");

                return new JSIterator(iteratorResult);
            }
        }

        return new CodePointEnumerator(value);
    }

    // The raw built-in String iterator (Unicode code points). Used by the default
    // String.prototype[@@iterator] so it never re-enters the override-aware
    // GetIterableEnumerator (which would recurse back into this method).
    internal IElementEnumerator GetCodePointEnumerator() => new CodePointEnumerator(value);

    private static bool IsDefaultStringIterator(JSValue iterator)
        => iterator is JSFunction f && DefaultIteratorMethod != null && f.Delegate?.Method == DefaultIteratorMethod;

    private struct CodePointEnumerator(in StringSpan value) : IElementEnumerator
    {
        private readonly StringSpan span = value;
        private int pos = 0;
        private int index = -1;

        private bool TryNext(out JSValue value)
        {
            if (pos >= span.Length)
            {
                value = JSUndefined.Value;
                return false;
            }

            var first = span[pos];
            if (char.IsHighSurrogate(first) && pos + 1 < span.Length && char.IsLowSurrogate(span[pos + 1]))
            {
                value = new JSString(new string(new[] { first, span[pos + 1] }));
                pos += 2;
            }
            else
            {
                value = new JSString(new string(first, 1));
                pos += 1;
            }

            index++;
            return true;
        }

        public bool MoveNext(out bool hasValue, out JSValue value, out uint i)
        {
            if (TryNext(out value))
            {
                hasValue = true;
                i = (uint)index;
                return true;
            }

            hasValue = false;
            i = 0;
            return false;
        }

        public bool MoveNext(out JSValue value) => TryNext(out value);

        public bool MoveNextOrDefault(out JSValue value, JSValue @default)
        {
            if (TryNext(out value))
                return true;

            value = @default;
            return false;
        }

        public JSValue NextOrDefault(JSValue @default) => TryNext(out var v) ? v : @default;
    }

    private struct ElementEnumerator(in StringSpan value) : IElementEnumerator
    {
        private StringSpan.CharEnumerator en = value.GetEnumerator();
        int index = -1;

        public bool MoveNext(out bool hasValue, out JSValue value, out uint i)
        {
            if (en.MoveNext(out var ch))
            {
                index++;
                i = (uint)index;
                hasValue = true;
                value = new JSString(new string(ch, 1));
                return true;
            }

            i = 0;
            value = JSUndefined.Value;
            hasValue = false;
            
            return false;
        }

        public bool MoveNext(out JSValue value)
        {
            if (en.MoveNext(out var ch))
            {
                index++;
                value = new JSString(new string(ch, 1));
                return true;
            }

            value = JSUndefined.Value;
            return false;
        }

        public bool MoveNextOrDefault(out JSValue value, JSValue @default)
        {
            if (en.MoveNext(out var ch))
            {
                index++;
                value = new JSString(new string(ch, 1));
                return true;
            }

            value = @default;
            return false;
        }

        public JSValue NextOrDefault(JSValue @default)
        {
            if (en.MoveNext(out var ch))
            {
                index++;
                return new JSString(new string(ch, 1));
            }

            return @default;
        }
    }
}
