using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Storage;
using System;
using System.ComponentModel;
using System.Dynamic;
using System.Globalization;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Broiler.JavaScript.Runtime;

/// <summary>
/// Base class for all JavaScript values.  Every JS type (number, string,
/// boolean, object, function, symbol, null, undefined) derives from this
/// class and overrides the relevant virtual members.
/// </summary>
public abstract partial class JSValue : IDynamicMetaObjectProvider, IPropertyAccessor
{
    // ── Factory infrastructure ──
    // Initialized by Core's ModuleInitializer so that Runtime types can
    // create concrete JS values without a direct dependency on Core.
    // These statics prepare for a future move of JSValue to Runtime.
    internal static JSValue UndefinedValue;
    internal static JSValue NullValue;
    internal static JSValue BooleanTrue;
    internal static JSValue BooleanFalse;
    internal static JSValue NumberOne;
    internal static JSValue NumberNaN;
    internal static JSValue NumberZero;
    internal static JSValue NumberMinusOne;
    internal static JSValue NumberTwo;
    internal static JSValue NumberNegativeZero;
    internal static JSValue NumberPositiveInfinity;
    internal static JSValue NumberNegativeInfinity;
    internal static Func<double, JSValue> CreateNumber;
    internal static Func<double, bool> IsPositiveZeroCheck;
    internal static Func<double, bool> IsNegativeZeroCheck;
    internal static Func<string, JSValue> CreateString;

    /// <summary>
    /// Cached empty-string value.  Wired by the BuiltIns assembly.
    /// </summary>
    internal static JSValue EmptyString;

    /// <summary>
    /// Factory delegate for creating a <c>JSString</c> that already has
    /// a pre-computed <see cref="KeyString"/>.
    /// Wired by the BuiltIns assembly via <c>[ModuleInitializer]</c>.
    /// </summary>
    internal static Func<string, KeyString, JSValue> CreateStringWithKey;

    internal static Func<string, Exception> NewTypeError;
    internal static Func<bool> IsStrictModeEnabled;
    internal static Func<object, JSValue> MarshalObject;
    internal static Func<JSValue, object, bool, object> ForceConvertHelper;
    internal static Func<Expression, JSValue, DynamicMetaObject> CreateDynamicMetaObject;
    internal static Func<double, string> NumberToECMAString;
    internal static Func<JSValue, IJSPrototype> CreatePrototypeObject;
    internal static Func<IPropertyAccessor, JSValue, JSValue> InvokePropertyGetter;

    /// <summary>
    /// Factory delegate for creating a <c>JSDecimal</c> from a <c>decimal</c> value.
    /// Wired by the BuiltIns assembly via <c>[ModuleInitializer]</c>.
    /// </summary>
    internal static Func<decimal, JSValue> CreateDecimalFactory;

    /// <summary>
    /// Factory delegate for creating a <c>JSDecimal</c> from a <c>string</c> value.
    /// Wired by the BuiltIns assembly via <c>[ModuleInitializer]</c>.
    /// Used by the Compiler for decimal literal compilation.
    /// </summary>
    public static Func<string, JSValue> CreateDecimalFromStringFactory;

    /// <summary>
    /// Creates a <c>JSDecimal</c> from a <c>decimal</c> value via the registered factory delegate.
    /// </summary>
    public static JSValue CreateDecimal(decimal value) => CreateDecimalFactory(value);

    /// <summary>
    /// Creates a <c>JSDecimal</c> from a <c>string</c> value via the registered factory delegate.
    /// Used by the Compiler for decimal literal compilation.
    /// </summary>
    public static JSValue CreateDecimalFromString(string value) => CreateDecimalFromStringFactory(value);

    /// <summary>
    /// Factory delegate for creating a <c>JSBigInt</c> from a <c>string</c> value.
    /// Wired by the BuiltIns assembly via <c>[ModuleInitializer]</c>.
    /// Used by the Compiler for BigInt literal compilation.
    /// </summary>
    public static Func<string, JSValue> CreateBigIntFromStringFactory;

    /// <summary>
    /// Factory delegate for creating a <c>JSBigInt</c> from a <c>long</c> value.
    /// Wired by the BuiltIns assembly via <c>[ModuleInitializer]</c>.
    /// Used by JSGlobal for timer IDs.
    /// </summary>
    internal static Func<long, JSValue> CreateBigIntFactory;

    /// <summary>
    /// Creates a <c>JSBigInt</c> from a <c>string</c> value via the registered factory delegate.
    /// Used by the Compiler for BigInt literal compilation.
    /// </summary>
    public static JSValue CreateBigIntFromString(string value) => CreateBigIntFromStringFactory(value);

    /// <summary>
    /// Creates a <c>JSBigInt</c> from a <c>long</c> value via the registered factory delegate.
    /// Used by JSGlobal for timer IDs.
    /// </summary>
    public static JSValue CreateBigInt(long value) => CreateBigIntFactory(value);

    /// <summary>
    /// Factory delegate for creating a <c>JSDate</c> from a <c>DateTimeOffset</c>.
    /// Wired by the BuiltIns assembly via <c>[ModuleInitializer]</c>.
    /// Used by Core and Clr for DateTime/DateTimeOffset marshaling.
    /// </summary>
    internal static Func<DateTimeOffset, JSValue> CreateDateFactory;

    /// <summary>
    /// Creates a <c>JSDate</c> from a <c>DateTimeOffset</c> via the registered factory delegate.
    /// </summary>
    public static JSValue CreateDate(DateTimeOffset value) => CreateDateFactory(value);

    /// <summary>
    /// Factory delegate for creating a <c>JSPromise</c> from a <c>Task&lt;JSValue&gt;</c>.
    /// Wired by the BuiltIns assembly via <c>[ModuleInitializer]</c>.
    /// Used by Clr for Task marshaling without referencing the concrete JSPromise type.
    /// </summary>
    internal static Func<Task<JSValue>, JSValue> CreatePromiseFromTask;

    /// <summary>
    /// Factory delegate for creating a <c>JSPromise</c> from a <c>Task</c> (non-generic).
    /// Wired by the BuiltIns assembly via <c>[ModuleInitializer]</c>.
    /// Used by Clr for Task marshaling without referencing the concrete JSPromise type.
    /// </summary>
    internal static Func<Task, JSValue> CreatePromiseFromUntypedTask;

    /// <summary>
    /// Factory delegate for creating a <c>JSPromise</c> from a generic <c>Task&lt;T&gt;</c>.
    /// Wired by the BuiltIns assembly via <c>[ModuleInitializer]</c>.
    /// </summary>
    internal static Func<Task, JSValue> CreatePromiseFromGenericTask;

    /// <summary>
    /// Factory delegate for creating a <c>JSFunction</c> from a delegate.
    /// Wired by the BuiltIns assembly via <c>[ModuleInitializer]</c>.
    /// </summary>
    internal static Func<JSFunctionDelegate, JSValue> CreateFunctionFactory;

    /// <summary>
    /// Factory delegate for creating a <c>JSFunction</c> with full parameters.
    /// Wired by the BuiltIns assembly via <c>[ModuleInitializer]</c>.
    /// </summary>
    internal static Func<JSFunctionDelegate, string, string, int, bool, JSValue> CreateFunctionFullFactory;

    /// <summary>
    /// Creates a <c>JSFunction</c> from a delegate via the registered factory.
    /// </summary>
    public static JSValue CreateFunction(JSFunctionDelegate f) => CreateFunctionFactory(f);

    /// <summary>
    /// Creates a <c>JSFunction</c> with full parameters via the registered factory.
    /// </summary>
    public static JSValue CreateFunction(JSFunctionDelegate f, string name, string source = null, int length = 0, bool createPrototype = true)
        => CreateFunctionFullFactory(f, name, source, length, createPrototype);

    /// <summary>
    /// Factory delegate for creating an empty <c>JSArray</c>.
    /// Wired by the BuiltIns assembly via <c>[ModuleInitializer]</c>.
    /// Used by Core when it needs to create arrays without referencing the concrete type.
    /// </summary>
    internal static Func<JSValue> CreateArrayFactory;

    /// <summary>
    /// Factory delegate for creating a <c>JSArray</c> with a specified length.
    /// Wired by the BuiltIns assembly via <c>[ModuleInitializer]</c>.
    /// </summary>
    internal static Func<uint, JSValue> CreateArrayWithLengthFactory;

    /// <summary>
    /// Creates an empty <c>JSArray</c> via the registered factory delegate.
    /// </summary>
    public static JSValue CreateArray() => CreateArrayFactory();

    /// <summary>
    /// Creates a <c>JSArray</c> with the specified length via the registered factory delegate.
    /// </summary>
    public static JSValue CreateArray(uint length) => CreateArrayWithLengthFactory(length);

    // ── JSSymbol factory infrastructure ──
    // Wired by the BuiltIns assembly's ModuleInitializer so that Core and
    // other assemblies can work with symbols without depending on the
    // concrete JSSymbol type.

    /// <summary>
    /// Well-known <c>Symbol.iterator</c> singleton.
    /// Wired by the BuiltIns assembly via <c>[ModuleInitializer]</c>.
    /// </summary>
    internal static IJSSymbol SymbolIterator;
    internal static IJSSymbol SymbolAsyncIterator;

    /// <summary>
    /// Well-known <c>Symbol.dispose</c> singleton.
    /// Wired by the BuiltIns assembly via <c>[ModuleInitializer]</c>.
    /// </summary>
    internal static IJSSymbol SymbolDispose;

    /// <summary>
    /// Well-known <c>Symbol.asyncDispose</c> singleton.
    /// Wired by the BuiltIns assembly via <c>[ModuleInitializer]</c>.
    /// </summary>
    internal static IJSSymbol SymbolAsyncDispose;

    /// <summary>
    /// Factory delegate for creating a new <c>JSSymbol</c> from a name string.
    /// Wired by the BuiltIns assembly via <c>[ModuleInitializer]</c>.
    /// </summary>
    internal static Func<string, JSValue> CreateSymbolFactory;

    /// <summary>
    /// Factory delegate for registering the <c>Symbol</c> constructor on a
    /// <see cref="JSContext"/>.  Mirrors <c>JSSymbol.CreateClass</c>.
    /// Wired by the BuiltIns assembly via <c>[ModuleInitializer]</c>.
    /// </summary>
    internal static Func<IJSContext, bool, JSValue> CreateSymbolClassFactory;

    /// <summary>
    /// Factory delegate for looking up a well-known symbol by name.
    /// Mirrors <c>JSSymbol.GlobalSymbol</c>.
    /// Wired by the BuiltIns assembly via <c>[ModuleInitializer]</c>.
    /// </summary>
    internal static Func<string, IJSSymbol> GetGlobalSymbolFactory;

    /// <summary>
    /// Factory delegate for looking up an existing symbol instance by its internal key.
    /// Wired by the BuiltIns assembly via <c>[ModuleInitializer]</c>.
    /// </summary>
    internal static Func<uint, IJSSymbol?> GetSymbolByKeyFactory;

    /// <summary>
    /// Returns the Object.prototype.toString builtin tag ("Number", "Boolean",
    /// "String", "BigInt", "Symbol") for values whose primitive [[XxxData]]
    /// internal slot is modelled in the BuiltIns layer — boxed primitives and the
    /// Number/Boolean/String prototype objects — or null when none applies.
    /// Wired by the BuiltIns assembly.
    /// </summary>
    internal static Func<JSValue, string> GetBuiltinToStringTag;

    /// <summary>Gets whether this value is the <c>undefined</c> singleton.</summary>
    public bool IsUndefined
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this == UndefinedValue;
    }

    /// <summary>Gets whether this value is the <c>null</c> singleton.</summary>
    public bool IsNull
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this == NullValue;
    }

    public bool IsNullOrUndefined
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this == NullValue || this == UndefinedValue;
    }

    /// <summary>Gets whether this value is a JavaScript number.</summary>
    public virtual bool IsNumber => false;

    /// <summary>Gets whether this value is a JavaScript object (including arrays and functions).</summary>
    public virtual bool IsObject => false;

    /// <summary>Gets whether this value is a JavaScript <c>Symbol</c>.</summary>
    public virtual bool IsSymbol => false;

    /// <summary>Gets whether this value is a JavaScript <c>Array</c>.</summary>
    public virtual bool IsArray => false;

    /// <summary>
    /// Updates the internal array length when a numeric key is set.
    /// Overridden by <c>JSArray</c> in the BuiltIns assembly.
    /// </summary>
    internal virtual void UpdateArrayLengthIfNeeded(uint key) { }

    /// <summary>
    /// Appends an item to this array.
    /// Overridden by <c>JSArray</c> in the BuiltIns assembly.
    /// </summary>
    public virtual void AddArrayItem(JSValue item) { }

    /// <summary>Gets whether this value is a JavaScript string.</summary>
    public virtual bool IsString => false;

    /// <summary>Gets whether this value is a JavaScript boolean.</summary>
    public virtual bool IsBoolean => false;

    /// <summary>Gets whether this value is a JavaScript BigInt.</summary>
    public virtual bool IsBigInt => false;

    /// <summary>Gets whether this value is a JavaScript function.</summary>
    public virtual bool IsFunction => false;

    /// <summary>Gets whether this value is a JavaScript <c>Decimal</c> (ES2025 Decimal128).</summary>
    public virtual bool IsDecimal => false;

    /// <summary>Gets the underlying <c>decimal</c> value. Only valid when <see cref="IsDecimal"/> is <c>true</c>.</summary>
    public virtual decimal DecimalValue => throw new InvalidOperationException("Not a decimal value");

    internal virtual bool IsSpread => false;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public object Convert(Type type, object def)
    {
        if (type.IsAssignableFrom(typeof(JSValue)))
            return this;

        if (ConvertTo(type, out var v))
            return v;

        return def;
    }

    public object ForceConvert(Type type)
    {
        if (type.IsAssignableFrom(GetType()))
            return this;
        if (ConvertTo(type, out var value))
            return value;
        var result = ForceConvertHelper?.Invoke(this, type, false);
        if (result != null) return result;
        throw NewTypeError($"Cannot convert {this} to type {type.Name}");
    }

    internal bool TryConvertTo(Type type, out object value)
    {
        if (typeof(JSValue).IsAssignableFrom(type))
        {
            value = this;
            return true;
        }

        return ConvertTo(type, out value);
    }
    public virtual bool ConvertTo(Type type, out object value)
    {
        if (type == typeof(JSValue))
        {
            value = this;
            return true;
        }

        value = null;
        return false;
    }

    public bool CanBeNumber
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => IsNumber || IsBoolean || IsNull;
    }

    public virtual int Length
    {
        get => 0;
        set { }
    }

    public virtual double DoubleValue => double.NaN;

    public abstract bool BooleanValue { get; }

    public virtual string StringValue => ToString();

    public abstract JSValue TypeOf();

    public virtual int IntValue => unchecked((int)ToUint32(DoubleValue));

    // ToUint32 (§7.1.6) / ToInt32 (§7.1.5): NaN, ±0 and ±∞ map to 0; every other
    // value is truncated toward zero and reduced modulo 2^32. The fast path covers
    // finite values inside the signed 64-bit range, where (long) truncates exactly
    // and the low 32 bits are the result. Values outside that range (including ±∞,
    // which .NET converts to long.MaxValue/MinValue rather than wrapping) take the
    // floating-point modulo path.
    internal static uint ToUint32(double d)
    {
        const double TwoPow32 = 4294967296.0;
        if (d >= -9.2233720368547758E18 && d < 9.2233720368547758E18)
            return unchecked((uint)((long)d << 32 >> 32));

        if (!double.IsFinite(d))
            return 0;

        var num = Math.Truncate(d) % TwoPow32;
        if (num < 0)
            num += TwoPow32;

        return (uint)num;
    }

    /// <summary>
    /// Integer value restricts value within int.MaxValue and
    /// more than int.MaxValue is returned as int.MaxValue
    /// </summary>
    public virtual int IntegerValue
    {
        get
        {
            var v = DoubleValue;
            if (v > 2147483647.0)
                return 2147483647;
#pragma warning disable 1718
            if (v != v)
                return 0;
#pragma warning restore 1718
            return (int)v;
        }
    }

    public virtual long BigIntValue => (long)(ulong)DoubleValue;

    public virtual uint UIntValue => ToUint32(DoubleValue);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public IJSPrototype prototypeChain;

    public virtual JSValue BasePrototypeObject
    {
        set => prototypeChain = CreatePrototypeObject?.Invoke(value);
    }


    /// <summary>
    /// Unless overriden, it returns self
    /// </summary>
    /// <returns></returns>
    public virtual JSValue ValueOf() => this;

    protected static JSValue ToNumericPrimitive(JSValue value)
    {
        var primitive = value switch
        {
            JSPrimitiveObject primitiveObject => primitiveObject.ValueOf(),
            JSObject @object => @object.ToDefaultPrimitive(),
            _ => value.ValueOf()
        };

        // ToNumeric must yield a Number or BigInt; a Symbol can never be coerced.
        // Throw here (rather than later at DoubleValue access) so that, for a binary
        // operator, ToNumeric(lhs) fails before the rhs operand is coerced.
        if (primitive.IsSymbol)
            throw NewTypeError?.Invoke("Cannot convert a Symbol value to a number.")
                ?? new InvalidOperationException("JSValue.NewTypeError delegate is not initialized. Ensure the BuiltIns assembly module initializer has run.");

        return primitive;
    }

    public virtual JSValue Negate()
    {
        var self = ToNumericPrimitive(this);
        return !ReferenceEquals(self, this) ? self.Negate() : CreateNumber(-DoubleValue);
    }

    public virtual JSValue Increment() => CreateNumber(DoubleValue + 1);

    public virtual JSValue Decrement() => CreateNumber(DoubleValue - 1);

    // ToNumeric (ECMA-262 § 7.1.4 / § 7.1.3): coerce to a Number or BigInt primitive.
    // Used by the update operators (`++`/`--`), whose result is the coerced numeric
    // old value — `var y = "1"++` yields the Number 1, not the String "1" — and whose
    // operand must be coerced exactly once.
    public JSValue ToNumeric()
    {
        var primitive = ToNumericPrimitive(this);
        return primitive.IsBigInt ? primitive : CreateNumber(primitive.DoubleValue);
    }

    public virtual JSValue Subtract(JSValue value)
    {
        var self = ToNumericPrimitive(this);
        value = ToNumericPrimitive(value);
        return !ReferenceEquals(self, this) ? self.Subtract(value) : CreateNumber(DoubleValue - value.DoubleValue);
    }

    public virtual JSValue Multiply(JSValue value)
    {
        var self = ToNumericPrimitive(this);
        value = ToNumericPrimitive(value);
        return !ReferenceEquals(self, this) ? self.Multiply(value) : CreateNumber(DoubleValue * value.DoubleValue);
    }

    /// <summary>
    public virtual JSValue Divide(JSValue value)
    {
        var self = ToNumericPrimitive(this);
        value = ToNumericPrimitive(value);
        return !ReferenceEquals(self, this) ? self.Divide(value) : CreateNumber(DoubleValue / value.DoubleValue);
    }

    public virtual JSValue BitwiseAnd(JSValue value)
    {
        var self = ToNumericPrimitive(this);
        value = ToNumericPrimitive(value);
        return !ReferenceEquals(self, this) ? self.BitwiseAnd(value) : CreateNumber(IntValue & value.IntValue);
    }

    public virtual JSValue BitwiseOr(JSValue value)
    {
        var self = ToNumericPrimitive(this);
        value = ToNumericPrimitive(value);
        return !ReferenceEquals(self, this) ? self.BitwiseOr(value) : CreateNumber(IntValue | value.IntValue);
    }

    public virtual JSValue BitwiseXor(JSValue value)
    {
        var self = ToNumericPrimitive(this);
        value = ToNumericPrimitive(value);
        return !ReferenceEquals(self, this) ? self.BitwiseXor(value) : CreateNumber(IntValue ^ value.IntValue);
    }

    public virtual JSValue BitwiseNot()
    {
        var self = ToNumericPrimitive(this);
        return !ReferenceEquals(self, this) ? self.BitwiseNot() : CreateNumber(~IntValue);
    }

    public virtual JSValue LeftShift(JSValue value)
    {
        var self = ToNumericPrimitive(this);
        value = ToNumericPrimitive(value);
        return !ReferenceEquals(self, this) ? self.LeftShift(value) : CreateNumber(IntValue << value.IntValue);
    }

    public virtual JSValue RightShift(JSValue value)
    {
        var self = ToNumericPrimitive(this);
        value = ToNumericPrimitive(value);
        return !ReferenceEquals(self, this) ? self.RightShift(value) : CreateNumber(IntValue >> (value.IntValue & 0x1F));
    }

    public virtual JSValue UnsignedRightShift(JSValue value)
    {
        var self = ToNumericPrimitive(this);
        value = ToNumericPrimitive(value);
        return !ReferenceEquals(self, this) ? self.UnsignedRightShift(value) : CreateNumber(UIntValue >> value.IntValue);
    }

    public virtual JSValue Modulo(JSValue value)
    {
        var self = ToNumericPrimitive(this);
        value = ToNumericPrimitive(value);
        return !ReferenceEquals(self, this) ? self.Modulo(value) : CreateNumber(DoubleValue % value.DoubleValue);
    }

    /// Speed improvements for string contact operations
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public virtual JSValue AddValue(JSValue value)
    {
        var self = this is JSObject selfObject ? selfObject.ToDefaultPrimitive() : ValueOf();
        value = value is JSObject valueObject ? valueObject.ToDefaultPrimitive() : value;

        if (!ReferenceEquals(self, this))
            return self.AddValue(value);

        if (self.CanBeNumber && value.CanBeNumber)
            return CreateNumber(self.DoubleValue + value.DoubleValue);

        if (value.ToString().Length == 0)
            return self.IsString ? self : CreateString(self.StringValue);

        return CreateString(self.StringValue + value.StringValue);
    }
    /// <summary>
    /// Speed improvements for string contact operations
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public virtual JSValue AddValue(double value)
    {
        // §13.15 ApplyStringOrNumericBinaryOperator: the left operand is first
        // coerced with ToPrimitive (default hint). Going through ToDefaultPrimitive
        // — rather than the raw CLR ValueOf() — lets a wrapper observe an overridden
        // valueOf / @@toPrimitive (e.g. a boxed Symbol whose valueOf was replaced).
        var self = this is JSObject selfObject ? selfObject.ToDefaultPrimitive() : ValueOf();
        if (!ReferenceEquals(self, this))
            return self.AddValue(value);

        if (self.CanBeNumber)
            return CreateNumber(self.DoubleValue + value);

        return CreateString(self.StringValue + NumberToECMAString(value));
    }

    /// <summary>
    /// Speed improvements for string contact operations
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public virtual JSValue AddValue(string value)
    {
        var self = this is JSObject selfObject ? selfObject.ToDefaultPrimitive() : ValueOf();
        if (!ReferenceEquals(self, this))
            return self.AddValue(value);

        if (value.Length == 0)
            return self.IsString ? self : CreateString(self.StringValue);

        return CreateString(self.StringValue + value);
    }

    protected JSValue(JSValue prototype) => BasePrototypeObject = prototype ?? GetCurrentPrototype();

    protected virtual JSValue GetCurrentPrototype() => null;

    internal abstract PropertyKey ToKey(bool create = true);

    internal static JSValue NormalizePropertyKey(JSValue key)
    {
        var normalized = key.ToKey(false);
        return normalized.Type switch
        {
            KeyType.UInt => CreateNumber(normalized.Index),
            KeyType.String => CreateString(normalized.KeyString.ToString()),
            KeyType.Symbol => normalized.Symbol as JSValue ?? key,
            _ => key,
        };
    }

    /// <summary>
    /// ToPropertyKey (ECMA-262 §7.1.19) returning the key as a JSValue, performing any
    /// observable ToPrimitive/ToString of a computed PropertyName. The object-literal
    /// compiler calls this while evaluating a computed key so its side effects happen
    /// before the property value expression is evaluated (PropertyDefinitionEvaluation
    /// evaluates PropertyName — including ToPropertyKey — before the AssignmentExpression).
    /// The result re-keys idempotently (no further user code) when handed to FastAddValue.
    /// </summary>
    public static JSValue ToPropertyKeyValue(JSValue key) => NormalizePropertyKey(key);

    public virtual JSValue GetPrototypeOf() => prototypeChain?.Object ?? NullValue;

    public virtual void SetPrototypeOf(JSValue target)
    {
        if (!TrySetPrototypeOf(target, out var error))
            throw NewTypeError(error ?? "Could not set prototype");
    }

    /// <summary>
    /// Spec ordinary [[SetPrototypeOf]] (§10.1.2): performs the change and
    /// returns whether it succeeded. The not-extensible and cyclic cases return
    /// <c>false</c> (with <paramref name="error"/> set) rather than throwing, so
    /// callers like <c>Reflect.setPrototypeOf</c> can surface the boolean result
    /// while <c>Object.setPrototypeOf</c> / the <c>__proto__</c> setter throw.
    /// </summary>
    public virtual bool TrySetPrototypeOf(JSValue target, out string error)
    {
        error = null;

        if (target == NullValue)
        {
            if (this is JSObject { } nullTargetObject && !nullTargetObject.IsExtensible() && prototypeChain?.Object != null)
            {
                error = "Object is not extensible";
                return false;
            }

            BasePrototypeObject = null;
            return true;
        }

        if (!target.IsObject)
        {
            error = "Prototype must be an object or null";
            return false;
        }

        if (this is JSObject { } @object)
        {
            var current = prototypeChain?.Object;
            if (ReferenceEquals(current, target))
                return true;

            if (!@object.IsExtensible())
            {
                error = "Object is not extensible";
                return false;
            }
        }

        for (var prototype = target; prototype is JSObject prototypeObject; prototype = prototypeObject.GetPrototypeOf())
        {
            if (ReferenceEquals(prototype, this))
            {
                error = "Cyclic __proto__ value";
                return false;
            }

            if (prototypeObject.GetType() != typeof(JSObject))
                break;
        }

        BasePrototypeObject = target;
        return true;
    }

    public virtual JSValue GetOwnPropertyDescriptor(JSValue name) => throw new NotImplementedException();

    public virtual JSValue HasProperty(JSValue propertyKey)
    {
        if (this is not JSObject target)
            throw NewTypeError($"Cannot use 'in' operator to search for '{propertyKey}' in {this}");

        // §10.1.7 OrdinaryHasProperty: check own property first
        if (!target.GetOwnPropertyDescriptor(propertyKey).IsUndefined)
            return BooleanTrue;

        // Then delegate to the prototype's [[HasProperty]] (not GetOwnPropertyDescriptor)
        // so that Proxy objects in the prototype chain invoke their "has" trap.
        var proto = target.GetPrototypeOf();
        if (proto is JSObject protoObj)
            return protoObj.HasProperty(propertyKey);

        return BooleanFalse;
    }

    /// <summary>
    /// Resolves a <see cref="JSProperty"/> to its runtime value, invoking
    /// getters via the <see cref="InvokePropertyGetter"/> factory delegate.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public JSValue GetValue(in JSProperty p)
    {
        if (p.IsEmpty)
            return UndefinedValue;

        return !p.IsProperty ? (JSValue)p.value : InvokePropertyGetter(p.get, this);
    }

    public virtual JSValue GetOwnProperty(in KeyString name)
    {
        var pc = prototypeChain;

        if (pc != null)
            return this.GetValue(pc.GetInternalProperty(name));

        return UndefinedValue;
    }

    public virtual JSValue GetOwnProperty(uint name)
    {
        var pc = prototypeChain;

        if (pc != null)
            return this.GetValue(pc.GetInternalProperty(name));

        return UndefinedValue;
    }

    public virtual JSValue GetOwnProperty(IJSSymbol name)
    {
        var pc = prototypeChain;

        if (pc != null)
            return this.GetValue(pc.GetInternalProperty(name));

        return UndefinedValue;
    }

    public JSValue GetOwnProperty(JSValue name)
    {
        if (name is IJSSymbol symbol)
            return GetOwnProperty(symbol);

        var key = name.ToKey(false);

        if (key.IsUInt)
            return GetOwnProperty(key.Index);

        return GetOwnProperty(in key.KeyString);
    }

    public JSValue PropertyOrUndefined(in KeyString name)
    {
        if (this == NullValue || this == UndefinedValue)
            return UndefinedValue;

        return GetValue(name, this);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public JSValue PropertyOrUndefined(JSValue super, in KeyString name)
    {
        if (this == NullValue || this == UndefinedValue)
            return UndefinedValue;

        var pc = prototypeChain;

        if (pc == null)
            return UndefinedValue;

        return super.GetValue(name, this);
    }

    public JSValue PropertyOrUndefined(uint name)
    {
        if (this == NullValue || this == UndefinedValue)
            return UndefinedValue;

        return GetValue(name, this);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public JSValue PropertyOrUndefined(JSValue super, uint name)
    {
        if (this == NullValue || this == UndefinedValue)
            return UndefinedValue;

        var pc = prototypeChain;
        if (pc == null)
            return UndefinedValue;

        return super.GetValue(name, this);
    }

    public JSValue PropertyOrUndefined(IJSSymbol name)
    {
        if (this == NullValue || this == UndefinedValue)
            return UndefinedValue;

        return GetValue(name, this);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public JSValue PropertyOrUndefined(JSValue super, IJSSymbol name)
    {
        if (this == NullValue || this == UndefinedValue)
            return UndefinedValue;

        var pc = prototypeChain;
        if (pc == null)
            return UndefinedValue;

        return super.GetValue(name, this);
    }

    public JSValue PropertyOrUndefined(JSValue name)
    {
        if (this == NullValue || this == UndefinedValue)
            return UndefinedValue;

        if (name is IJSSymbol s)
            return PropertyOrUndefined(s);

        var k = name.ToKey(false);
        if (k.IsUInt)
            return PropertyOrUndefined(k.Index);

        return PropertyOrUndefined(k.KeyString);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public JSValue PropertyOrUndefined(JSValue super, JSValue name)
    {
        if (this == NullValue || this == UndefinedValue)
            return UndefinedValue;

        if (name is IJSSymbol s)
            return PropertyOrUndefined(super, s);

        var k = name.ToKey(false);
        if (k.IsUInt)
            return PropertyOrUndefined(k.Index);

        return PropertyOrUndefined(k.KeyString);
    }

    // ── Optional-chaining links (see JSOptionalChainSkip) ─────────────────────────
    //
    // OptionalLink implements a `?.` link: a nullish base (or an already-short-circuited
    // chain) yields the skip sentinel; otherwise the property is read normally.
    // ChainLink implements a trailing NON-optional link inside an optional chain: it
    // propagates an in-flight short-circuit but, for a genuinely-nullish base, performs an
    // ordinary access (which throws), so `a?.b.c` throws when `a.b` is undefined yet
    // short-circuits when `a` is nullish.

    private bool IsOptionalChainSkip => ReferenceEquals(this, JSOptionalChainSkip.Value);

    // Public surface for the expression builder (separate assembly): the chain
    // short-circuit sentinel and a test for it, used by the call lowering.
    public static JSValue OptionalChainSkipValue() => JSOptionalChainSkip.Value;

    public bool IsOptionalChainSkipSentinel => IsOptionalChainSkip;

    public JSValue OptionalLink(in KeyString name)
        => IsOptionalChainSkip || this == NullValue || this == UndefinedValue
            ? JSOptionalChainSkip.Value
            : GetValue(name, this);

    public JSValue OptionalLink(uint name)
        => IsOptionalChainSkip || this == NullValue || this == UndefinedValue
            ? JSOptionalChainSkip.Value
            : GetValue(name, this);

    public JSValue OptionalLink(IJSSymbol name)
        => IsOptionalChainSkip || this == NullValue || this == UndefinedValue
            ? JSOptionalChainSkip.Value
            : GetValue(name, this);

    public JSValue OptionalLink(JSValue name)
    {
        if (IsOptionalChainSkip || this == NullValue || this == UndefinedValue)
            return JSOptionalChainSkip.Value;

        if (name is IJSSymbol s)
            return GetValue(s, this);

        var k = name.ToKey(false);
        return k.IsUInt ? GetValue(k.Index, this) : GetValue(k.KeyString, this);
    }

    // The indexer (this[name]) throws on a genuinely-nullish receiver, which is exactly
    // the required behaviour for a trailing link whose base is a real undefined/null.
    public JSValue ChainLink(in KeyString name)
        => IsOptionalChainSkip ? JSOptionalChainSkip.Value : this[name];

    public JSValue ChainLink(uint name)
        => IsOptionalChainSkip ? JSOptionalChainSkip.Value : this[name];

    public JSValue ChainLink(IJSSymbol name)
        => IsOptionalChainSkip ? JSOptionalChainSkip.Value : this[name];

    public JSValue ChainLink(JSValue name)
    {
        if (IsOptionalChainSkip)
            return JSOptionalChainSkip.Value;

        if (name is IJSSymbol s)
            return this[s];

        var k = name.ToKey(false);
        return k.IsUInt ? this[k.Index] : this[k.KeyString];
    }

    // Chain root: convert the short-circuit sentinel back to the observable `undefined`.
    public JSValue UnwrapOptionalChain()
        => IsOptionalChainSkip ? UndefinedValue : this;

    public virtual JSValue this[KeyString name]
    {
        get => GetValue(name, this);
        // Route through SetValue so an inherited accessor's setter is invoked with
        // this primitive as the receiver (OrdinarySet). Only when no setter handles
        // the write does the primitive no-op (non-strict) / strict-throw apply,
        // mirroring the JSValue-keyed indexer below.
        set
        {
            if (!SetValue(name, value, this, IsStrictModeEnabled?.Invoke() == true))
                ThrowOnStrictPrimitiveAssignment(name);
        }
    }

    public virtual JSValue this[uint key]
    {
        get => GetValue(key, this);
        set
        {
            if (!SetValue(key, value, this, IsStrictModeEnabled?.Invoke() == true))
                ThrowOnStrictPrimitiveAssignment(key);
        }
    }

    public virtual JSValue this[IJSSymbol symbol]
    {
        get => GetValue(symbol, this);
        set
        {
            if (!SetValue(symbol, value, this, IsStrictModeEnabled?.Invoke() == true))
                ThrowOnStrictPrimitiveAssignment(symbol);
        }
    }

    public JSValue this[JSValue key]
    {
        get => GetValue(key, this);
        set
        {
            if (SetValue(key, value, this, IsStrictModeEnabled?.Invoke() == true))
                return;

            if (IsNullOrUndefined)
                throw NewTypeError?.Invoke($"Cannot set properties of {this}")
                    ?? new InvalidOperationException("JSValue.NewTypeError delegate is not initialized. Ensure the BuiltIns assembly module initializer has run.");

            ThrowOnStrictPrimitiveAssignment(key);
        }
    }

    internal virtual JSValue this[KeyString name, JSValue @this]
    {
        get
        {
            if (prototypeChain == null)
                return UndefinedValue;

            return GetValue(name, this);
        }
        set { }
    }

    public virtual JSValue GetValue(uint key, JSValue receiver, bool throwError = true)
    {
        if (prototypeChain != null)
            return prototypeChain.Object.GetValue(key, receiver ?? this, throwError);

        return UndefinedValue;
    }

    internal protected virtual JSValue GetValue(KeyString key, JSValue receiver, bool throwError = true)
    {
        if (prototypeChain != null)
            return prototypeChain.Object.GetValue(key, receiver ?? this, throwError);

        return UndefinedValue;
    }

    internal protected virtual JSValue GetValue(IJSSymbol key, JSValue receiver, bool throwError = true)
    {
        if (prototypeChain != null)
            return prototypeChain.Object.GetValue(key, receiver ?? this, throwError);

        return UndefinedValue;
    }

    internal JSValue GetValue(JSValue key, JSValue receiver, bool throwError = true)
    {
        // Per spec (6.2.5.5 GetValue), ToObject(base) must precede ToPropertyKey(key).
        // For null/undefined, ToObject throws TypeError before the key is converted.
        if (IsNullOrUndefined)
            throw NewTypeError?.Invoke($"Cannot read properties of {this}")
                ?? new InvalidOperationException("JSValue.NewTypeError delegate is not initialized. Ensure the BuiltIns assembly module initializer has run.");

        var k = key.ToKey(false);
        return k.Type switch
        {
            KeyType.UInt => GetValue(k.Index, receiver, throwError),
            KeyType.String => GetValue(k.KeyString, receiver, throwError),
            KeyType.Symbol => GetValue(k.Symbol, receiver, throwError),
            _ => UndefinedValue,
        };
    }

    public virtual bool SetValue(uint key, JSValue value, JSValue receiver, bool throwError = true)
        => prototypeChain != null && TryInvokeInheritedSetter(prototypeChain.GetInternalProperty(key), value, receiver);

    internal protected virtual bool SetValue(KeyString key, JSValue value, JSValue receiver, bool throwError = true)
        => prototypeChain != null && TryInvokeInheritedSetter(prototypeChain.GetInternalProperty(key), value, receiver);

    internal protected virtual bool SetValue(IJSSymbol key, JSValue value, JSValue receiver, bool throwError = true)
        => prototypeChain != null && TryInvokeInheritedSetter(prototypeChain.GetInternalProperty(key), value, receiver);

    // OrdinarySet on a primitive base value (number/string/boolean/symbol/bigint):
    // an inherited accessor property's setter is invoked with the primitive as the
    // receiver. A data property — or no property at all — cannot be created on a
    // primitive, so those cases are left to the caller's no-op (non-strict) /
    // ThrowOnStrictPrimitiveAssignment (strict) handling by returning false. The
    // resolved property comes from the prototype chain's flattened descriptor set,
    // mirroring the read path (GetValue), which already delegates to the chain.
    private bool TryInvokeInheritedSetter(in JSProperty property, JSValue value, JSValue receiver)
    {
        if (property.IsProperty && property.set is IJSFunction setter)
        {
            setter.InvokeFunction(new Arguments(receiver ?? this, value));
            return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool SetValue(JSValue key, JSValue value, JSValue receiver, bool throwError = true)
    {
        // Per spec (6.2.5.6 PutValue), ToObject(base) must precede ToPropertyKey(key).
        // For null/undefined, the caller (this[JSValue] setter) handles the TypeError,
        // but we must not call ToKey() here to avoid observable side effects.
        if (IsNullOrUndefined)
            return false;

        var k = key.ToKey();
        return k.Type switch
        {
            KeyType.Empty => false,
            KeyType.UInt => SetValue(k.Index, value, receiver, throwError),
            KeyType.String => SetValue(k.KeyString, value, receiver, throwError),
            KeyType.Symbol => SetValue(k.Symbol, value, receiver, throwError),
            _ => false,
        };
    }

    // MakeSuperPropertyReference (12.3.5.3) step 5: the super base — GetSuperBase,
    // i.e. the prototype of the method's [[HomeObject]] — must be object-coercible.
    // When the home object's prototype is null, accessing super.x throws a
    // TypeError rather than silently reading undefined.
    private static JSValue RequireSuperBase(JSValue super)
    {
        if (super == null || super.IsNullOrUndefined)
            throw NewTypeError?.Invoke("Cannot convert undefined or null to object")
                ?? new InvalidOperationException("JSValue.NewTypeError delegate is not initialized. Ensure the BuiltIns assembly module initializer has run.");

        return super;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public JSValue this[JSValue super, KeyString name]
    {
        get => RequireSuperBase(super).GetValue(name, this); set => RequireSuperBase(super).SetValue(name, value, this, IsStrictModeEnabled?.Invoke() == true);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public JSValue this[JSValue super, uint index]
    {
        get => RequireSuperBase(super).GetValue(index, this); set => RequireSuperBase(super).SetValue(index, value, this, IsStrictModeEnabled?.Invoke() == true);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public JSValue this[JSValue super, JSValue name]
    {
        get => RequireSuperBase(super).GetValue(name, this); set => RequireSuperBase(super).SetValue(name, value, this, IsStrictModeEnabled?.Invoke() == true);
    }


    public abstract bool Equals(JSValue value);

    public virtual bool EqualsLiteral(string value) => false;
    public virtual bool EqualsLiteral(double value) => false;

    public virtual bool StrictEqualsLiteral(string value) => false;
    public virtual bool StrictEqualsLiteral(double value) => false;


    [EditorBrowsable(EditorBrowsableState.Never)]
    public static bool StaticEquals(JSValue left, JSValue right) => left.Equals(right);

    // SwitchStatement compares the discriminant against each case with the Strict
    // Equality Comparison (===), not loose ==; this is the static entry the
    // compiler emits for the general (non-numeric/non-string) case path.
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static bool StaticStrictEquals(JSValue left, JSValue right) => left.StrictEquals(right);

    public abstract bool StrictEquals(JSValue value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void ThrowOnStrictPrimitiveAssignment(object key)
    {
        if (IsStrictModeEnabled?.Invoke() == true)
            throw NewTypeError?.Invoke($"Cannot create property {key} on {this}")
                ?? new InvalidOperationException("JSValue.NewTypeError delegate is not initialized. Ensure the BuiltIns assembly module initializer has run.");
    }

    internal static JSValue ThrowOnStrictDeleteFailure(JSValue target, in KeyString key, JSValue result)
    {
        if (result.BooleanValue || IsStrictModeEnabled?.Invoke() != true)
            return result;

        throw NewTypeError?.Invoke($"Cannot delete property {key} of {target}")
            ?? new InvalidOperationException("JSValue.NewTypeError delegate is not initialized. Ensure the BuiltIns assembly module initializer has run.");
    }

    internal static JSValue ThrowOnStrictDeleteFailure(JSValue target, uint key, JSValue result)
    {
        if (result.BooleanValue || IsStrictModeEnabled?.Invoke() != true)
            return result;

        throw NewTypeError?.Invoke($"Cannot delete property {key} of {target}")
            ?? new InvalidOperationException("JSValue.NewTypeError delegate is not initialized. Ensure the BuiltIns assembly module initializer has run.");
    }

    internal static JSValue ThrowOnStrictDeleteFailure(JSValue target, JSValue key, JSValue result)
    {
        if (result.BooleanValue || IsStrictModeEnabled?.Invoke() != true)
            return result;

        throw NewTypeError?.Invoke($"Cannot delete property {key} of {target}")
            ?? new InvalidOperationException("JSValue.NewTypeError delegate is not initialized. Ensure the BuiltIns assembly module initializer has run.");
    }

    /// <summary>
    /// 1. NaN is considered equal to NaN.
    /// 2. +0 and -0 are considered to be equal.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public virtual bool SameValueZero(JSValue value) => StrictEquals(value);

    private static void ThrowIfSymbolRelationalOperand(JSValue left, JSValue right)
    {
        if (!left.IsSymbol && !right.IsSymbol)
            return;

        throw NewTypeError?.Invoke("Cannot convert a Symbol value to a number.")
            ?? new InvalidOperationException("JSValue.NewTypeError delegate is not initialized. Ensure the BuiltIns assembly module initializer has run.");
    }

    public virtual bool Less(JSValue value)
    {
        ThrowIfSymbolRelationalOperand(this, value);

        if (IsUndefined || value.IsUndefined)
            return false;

        // A BigInt operand compares by mathematical value; let the BigInt side
        // (which coerces the other operand) drive the comparison instead of
        // forcing it through DoubleValue, which throws for BigInt.
        if (value.IsBigInt && !IsBigInt)
            return value.Greater(this);

        if (!CanBeNumber && !value.CanBeNumber)
        {
            if (StringValue.Less(value.StringValue))
                return true;
        }
        else
        {
            if (DoubleValue < value.DoubleValue)
                return true;
        }

        return false;
    }

    public virtual bool LessOrEqual(JSValue value)
    {
        ThrowIfSymbolRelationalOperand(this, value);

        if (IsUndefined || value.IsUndefined)
            return false;

        if (value.IsBigInt && !IsBigInt)
            return value.GreaterOrEqual(this);

        if (!CanBeNumber && !value.CanBeNumber)
        {
            if (StringValue.LessOrEqual(value.StringValue))
                return true;
        }
        else
        {
            if (DoubleValue <= value.DoubleValue)
                return true;
        }

        return false;
    }

    public virtual bool Greater(JSValue value)
    {
        ThrowIfSymbolRelationalOperand(this, value);

        if (IsUndefined || value.IsUndefined)
            return false;

        if (value.IsBigInt && !IsBigInt)
            return value.Less(this);

        if (!CanBeNumber && !value.CanBeNumber)
        {
            if (StringValue.Greater(value.StringValue))
                return true;
        }
        else
        {
            if (DoubleValue > value.DoubleValue)
                return true;
        }

        return false;
    }

    public virtual bool GreaterOrEqual(JSValue value)
    {
        ThrowIfSymbolRelationalOperand(this, value);

        if (IsUndefined || value.IsUndefined)
            return false;

        if (value.IsBigInt && !IsBigInt)
            return value.LessOrEqual(this);

        if (!CanBeNumber && !value.CanBeNumber)
        {
            if (StringValue.GreaterOrEqual(value.StringValue))
                return true;
        }
        else
        {
            if (DoubleValue >= value.DoubleValue)
                return true;
        }

        return false;
    }

    public virtual IElementEnumerator GetAllKeys(bool showEnumerableOnly = true, bool inherited = true) => new ElementEnumerator();

    internal virtual JSValue Is(JSValue value) => ReferenceEquals(this, value) ? BooleanTrue : BooleanFalse;


    public virtual JSValue CreateInstance(in Arguments a) => throw NewTypeError("Value is not a constructor");

    public abstract JSValue InvokeFunction(in Arguments a);

    internal virtual JSFunctionDelegate GetMethod(in KeyString key) => prototypeChain.GetMethod(key);

    /// <summary>
    /// Warning do not use in concatenation
    /// </summary>
    /// <returns></returns>
    public override string ToString() => throw new NotSupportedException($"Use inherited version ... {GetType().Name} ");


    /// <summary>
    /// Returns a string containing a locale-dependant version of the number.
    /// </summary>
    /// <returns> A string containing a locale-dependant version of the number. </returns>
    /// 
    public virtual string ToLocaleString(string format, CultureInfo culture) => throw new NotImplementedException();
    public virtual string ToDetailString() => ToString();

    public virtual JSValue Delete(in KeyString key) => BooleanTrue;
    public virtual JSValue Delete(uint key) => BooleanTrue;
    public virtual JSValue Delete(IJSSymbol symbol) => BooleanTrue;

    public virtual JSValue Delete(JSValue index)
    {
        var key = index.ToKey(false);
        return key.Type switch
        {
            KeyType.Empty => BooleanFalse,
            KeyType.UInt => Delete(key.Index),
            KeyType.String => Delete(key.KeyString),
            KeyType.Symbol => Delete(key.Symbol),
            _ => BooleanFalse,
        };
    }

    internal JSValue InternalInvoke(object name, in Arguments a)
    {
        JSValue fx = null;
        switch (name)
        {
            case JSValue v:
                fx = this[v];
                break;
            case KeyString ks:
                fx = this[ks];
                break;
            case string str:
                fx = this[str];
                break;
        }

        if (fx.IsUndefined)
            throw NewTypeError($"Cannot invoke {name} of object as it is undefined");

        return fx.InvokeFunction(a.OverrideThis(this));
    }

    DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) => CreateDynamicMetaObject(parameter, this);

    public virtual JSValue Power(JSValue a)
    {
        var self = ToNumericPrimitive(this);
        a = ToNumericPrimitive(a);
        if (!ReferenceEquals(self, this))
            return self.Power(a);

        var v = self.DoubleValue;
        var a1 = a.DoubleValue;

        if (a1 == 0)
            return NumberOne;

        if (a1 == double.PositiveInfinity || a1 == double.NegativeInfinity)
        {
            if (v == 1 || v == -1)
                return NumberNaN;
        }

        return CreateNumber(Math.Pow(v, a1));
    }

    internal virtual bool TryGetValue(uint i, out JSProperty value)
    {
        value = new JSProperty { };
        return false;
    }

    internal virtual bool TryGetElement(uint i, out JSValue value)
    {
        value = null;
        return false;
    }

    internal virtual void MoveElements(int start, int to) { }

    internal virtual bool TryRemove(uint i, out JSProperty p)
    {
        p = new JSProperty();
        return false;
    }

    public virtual IElementEnumerator GetElementEnumerator() => ElementEnumerator.Empty;
    public virtual IElementEnumerator GetAsyncElementEnumerator() => GetElementEnumerator();
    public virtual IElementEnumerator GetIterableEnumerator() => throw NewTypeError("Value is not iterable");
    public virtual IElementEnumerator GetAsyncIterableEnumerator() => GetIterableEnumerator();

    private readonly struct ElementEnumerator : IElementEnumerator
    {
        public static IElementEnumerator Empty = new ElementEnumerator();

        public bool MoveNext(out bool hasValue, out JSValue value, out uint index)
        {
            value = UndefinedValue;
            index = 0;
            hasValue = false;

            return false;
        }

        public bool MoveNext(out JSValue value)
        {
            value = UndefinedValue;
            return false;
        }

        public bool MoveNextOrDefault(out JSValue value, JSValue @default)
        {
            value = @default;
            return false;
        }
        public JSValue NextOrDefault(JSValue @default) => @default;
    }
}
