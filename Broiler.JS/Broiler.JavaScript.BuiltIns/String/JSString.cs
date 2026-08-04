using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.Ast.Misc;
using System;
using System.Globalization;
using System.Threading;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.BuiltIns.Function;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.String;

[JSBaseClass("Object")]
[JSFunctionGenerator("String")]
public partial class JSString : JSPrimitive
{
    internal static JSString Empty = new(string.Empty);

    /// <summary>
    /// Minimum combined length before a concatenation is deferred into a rope rather than
    /// copied. A rope node plus its JSString costs on the order of a hundred bytes, so
    /// deferring a short join would lose; an accumulating string passes this within a few
    /// iterations and is O(1) per append from then on.
    /// </summary>
    private const int RopeThreshold = 64;

    // Exactly one of these is set. `flat` is the materialized value; `rope` is a pending
    // concatenation that has not been copied out yet. Flatten() moves a string from the
    // second state to the first, permanently.
    private string flat;
    private Rope rope;

    /// <summary>One deferred concatenation: <c>Left + Right</c>, of total <c>Length</c>.</summary>
    /// <remarks>
    /// Immutable, and only ever left-leaning — <c>Right</c> is always an already-flat string.
    /// That is the shape `s = s + x` produces, which is the case worth optimizing; a join
    /// whose right side is itself a rope flattens that side rather than nesting, keeping the
    /// walk in <see cref="Flatten"/> a simple spine descent.
    /// </remarks>
    private sealed class Rope(JSString left, string right, int length)
    {
        internal readonly JSString Left = left;
        internal readonly string Right = right;
        internal readonly int Length = length;
    }

    /// <summary>
    /// The string's characters, materializing a pending concatenation on first demand.
    /// </summary>
    internal string value => flat ?? Flatten();

    /// <summary>
    /// Copies a pending rope out into a real string, iteratively.
    /// </summary>
    /// <remarks>
    /// Walks the left spine writing each segment into the buffer back-to-front, so a rope
    /// built by n appends costs one O(total) pass instead of the n copies that eager
    /// concatenation performed. Iterative rather than recursive because the spine is exactly
    /// as deep as the number of appends — a recursive flatten would overflow the stack on the
    /// very workload this exists for.
    /// </remarks>
    private string Flatten()
    {
        var pending = rope;
        if (pending == null)
            return flat;

        var materialized = string.Create(pending.Length, this, static (span, start) =>
        {
            var position = span.Length;
            var node = start;
            while (true)
            {
                // Read once: a concurrent Flatten on this node may clear it, but the node we
                // captured stays valid because a Rope is immutable.
                var segment = node.rope;
                if (segment == null)
                {
                    var tail = Volatile.Read(ref node.flat);
                    position -= tail.Length;
                    tail.AsSpan().CopyTo(span[position..]);
                    return;
                }

                position -= segment.Right.Length;
                segment.Right.AsSpan().CopyTo(span[position..]);
                node = segment.Left;
            }
        });

        // Publish the flat value before dropping the rope, and drop it with a volatile write
        // so a reader that observes a null rope is guaranteed to see the string. Releasing
        // the rope is what lets the whole chain of intermediate nodes become garbage.
        Volatile.Write(ref flat, materialized);
        Volatile.Write(ref rope, null);
        return materialized;
    }

    /// <summary>Builds the deferred join, or copies when it is too small to be worth deferring.</summary>
    private JSString Concat(string right)
    {
        var length = Length;
        if (right.Length == 0)
            return this;

        if (length == 0)
            return new JSString(right);

        var total = length + right.Length;
        if (total < RopeThreshold)
            return new JSString(string.Concat(value, right));

        return new JSString(new Rope(this, right, total));
    }

    private JSString(Rope pending) : base() => rope = pending;

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

    public override bool BooleanValue => Length > 0;
    public override long BigIntValue => long.TryParse(ToString(), out var n) ? n : 0;
    public override bool IsString => true;

    // The three concatenation entry points all defer through Concat, so `s = s + x` in a
    // loop builds a rope instead of copying the accumulated string every iteration. The
    // emptiness tests use Length rather than the characters, so they no longer force a
    // pending concatenation to materialize just to find out whether it is empty.
    public override JSValue AddValue(double value)
    {
        if (Runtime.ArithmeticOperandDiagnostics.Enabled)
            Runtime.ArithmeticOperandDiagnostics.RecordRawDoubleOperand(false);

        return Concat(NumberToECMAString(value));
    }

    public override JSValue AddValue(string value) => Concat(value);

    public override JSValue AddValue(JSValue value)
    {
        // Counted here as well as on the base, because a String receiver never reaches it
        // (item 3-1). A String operand can never be both-Numbers, so leaving this out would
        // shrink the census's denominator and overstate the share a native form could reach.
        if (Runtime.ArithmeticOperandDiagnostics.Enabled)
            Runtime.ArithmeticOperandDiagnostics.RecordGeneric(false, value.IsNumber);

        if (value is JSString vString)
        {
            if (Length == 0)
                return vString;

            if (vString.Length == 0)
                return this;

            return Concat(vString.value);
        }

        // `string + obj` coerces the object with ToPrimitive (default hint), not a
        // string-forcing ValueOf() — so an overridden valueOf / @@toPrimitive that
        // yields a number (e.g. a boxed Symbol) is honoured before stringification.
        if (value is JSObject valueObject)
            value = valueObject.ToDefaultPrimitive();

        return Concat(value.StringValue);
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

    public JSString(string value) : base() => flat = value;
    public JSString(JSObject prototype, string value) : base(prototype) => flat = value;

    public JSString(in StringSpan value) : base() => flat = value.Value;


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
            return CreateNumber(value.Length);

        return base.GetValue(key, receiver, throwError);
    }

    internal protected override bool SetValue(KeyString key, JSValue value, JSValue receiver, bool throwError = true)
    {
        // "length" is a String exotic object's own { [[Writable]]: false } data property,
        // so a write must fail — the primitive indexer setter then applies the sloppy
        // no-op / strict TypeError (test262 sm/strict/15.5.5.1). Without this the inherited
        // [[Set]] reports success and the strict write is silently swallowed.
        if (key.Key == KeyStrings.length.Key)
            return false;

        return base.SetValue(key, value, receiver, throwError);
    }

    public override bool SetValue(uint key, JSValue value, JSValue receiver, bool throwError = true)
    {
        // An in-range indexed character is likewise a non-writable own property; the write
        // fails so the caller applies the sloppy no-op / strict TypeError.
        if (key < value.Length)
            return false;

        return base.SetValue(key, value, receiver, throwError);
    }

    // Property-key enumeration (for-in, Object.keys, spread) must yield the index
    // properties as String keys ("0", "1", …), not Numbers — otherwise destructuring
    // a for-in key (`for (var {length:x} in "foo")`) reads `.length` off a number.
    public override IElementEnumerator GetAllKeys(bool showEnumerableOnly = true, bool inherited = true) => new StringIndexKeyEnumerator(Length);

    // A rope knows its length without being copied out, so `s.length` — and every internal
    // range check — stays free while a concatenation is still pending.
    [JSExport]
    public override int Length
    {
        get
        {
            var pending = rope;
            return pending != null ? pending.Length : value.Length;
        }
    }

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

        // String == BigInt compares via StringToBigInt; let the BigInt side perform it
        // (it handles empty/whitespace -> 0n and non-integer strings -> not equal).
        if (value.IsBigInt)
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
            return BooleanTrue;

        return BooleanFalse;
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
        var symbolIterator = SymbolIterator;
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
                value = new JSString(new string([first, span[pos + 1]]));
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
