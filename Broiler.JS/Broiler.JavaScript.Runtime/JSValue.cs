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

    internal static JSErrorFactory NewTypeError;
    internal static Func<bool> IsStrictModeEnabled;
    internal static Func<object, JSValue> MarshalObject;
    internal static Func<JSObject, int, bool> IsFeatureEnabledFactory;
    internal static Func<JSValue, object, bool, object> ForceConvertHelper;
    internal static Func<Expression, JSValue, DynamicMetaObject> CreateDynamicMetaObject;
    internal static Func<double, string> NumberToECMAString;
    internal static Func<JSValue, IJSPrototype> CreatePrototypeObject;
    internal static Func<IPropertyAccessor, JSValue, JSValue> InvokePropertyGetter;

    /// <summary>Used by generated registration code to avoid constructing disabled members.</summary>
    public static bool IsFeatureEnabled(JSObject context, int feature)
        => IsFeatureEnabledFactory?.Invoke(context, feature) == true;

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
        set
        {
            // Assigning a null prototype cannot invalidate anything, so it must not pay for
            // the global mutation notice. Every primitive takes this path — JSPrimitive's
            // constructor chains to JSValue(null) and the default GetCurrentPrototype()
            // returns null — so a bare `new JSNumber(x)` would otherwise perform a delegate
            // invoke plus three interlocked increments on process-wide statics, making a
            // three-instruction arithmetic loop bump the prototype version once per
            // intermediate value.
            //
            // Soundness: the two published versions both answer "may something have been
            // ADDED to a prototype chain". IndexedPrototypeVersion gates
            // JSArray.CanUseDenseElementFastPath, which caches
            // !HasIndexedPropertiesOnPrototypeChain(); clearing a prototype can only shorten
            // a chain, so a stale cached answer stays conservative (an array keeps taking the
            // slow path) and never wrongly reports the fast path as safe. MarkUsedAsPrototype
            // and CreatePrototypeObject are both no-ops on null.
            if (value is null)
            {
                prototypeChain = null;
                return;
            }

            (value as JSObject)?.MarkUsedAsPrototype();
            prototypeChain = CreatePrototypeObject?.Invoke(value);
            JSObject.NotifyPrototypeChainMutation();
        }
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
            // ToNumeric (§7.1.4) is ToPrimitive(value, NUMBER) then ToNumber/ToBigInt, so a
            // user @@toPrimitive receives the "number" hint — not "default" — for an
            // arithmetic/bitwise operand (e.g. `obj * 2`, `-obj`).
            JSObject @object => @object.ToNumberPrimitive(),
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
        // Counted on the minting branch only: the other one delegates to the coerced primitive,
        // which counts there (JSNumber.Negate, or this method re-entered).
        if (ReferenceEquals(self, this) && ArithmeticOperandDiagnostics.Enabled)
            ArithmeticOperandDiagnostics.RecordUnaryNegate();
        return !ReferenceEquals(self, this) ? self.Negate() : CreateNumber(-DoubleValue);
    }

    public virtual JSValue Increment()
    {
        if (ArithmeticOperandDiagnostics.Enabled)
            ArithmeticOperandDiagnostics.RecordUnaryUpdate();
        return CreateNumber(DoubleValue + 1);
    }

    public virtual JSValue Decrement()
    {
        if (ArithmeticOperandDiagnostics.Enabled)
            ArithmeticOperandDiagnostics.RecordUnaryUpdate();
        return CreateNumber(DoubleValue - 1);
    }

    /// <summary>
    /// <see cref="Increment"/>, told by the compiler where the operand it is stepping lives
    /// (docs/performance-roadmap.md item 3-1).
    /// </summary>
    /// <remarks>
    /// Deliberately a non-virtual overload rather than a parameter on the virtual method: the
    /// override that exists (<c>JSBigInt</c>) is untouched, and the total the census already
    /// reports keeps being recorded by <see cref="Increment"/> itself — so the target rows sum to
    /// <c>UnaryUpdate</c> by construction rather than by inspection, which is what makes a missing
    /// call site visible instead of silent.
    /// </remarks>
    public JSValue Increment(ArithmeticOperandDiagnostics.UpdateTarget target)
    {
        if (ArithmeticOperandDiagnostics.Enabled)
            ArithmeticOperandDiagnostics.RecordUpdateTarget(target);
        return Increment();
    }

    /// <summary><see cref="Decrement"/>, told where the operand it is stepping lives.</summary>
    public JSValue Decrement(ArithmeticOperandDiagnostics.UpdateTarget target)
    {
        if (ArithmeticOperandDiagnostics.Enabled)
            ArithmeticOperandDiagnostics.RecordUpdateTarget(target);
        return Decrement();
    }

    // ToNumeric (ECMA-262 § 7.1.4 / § 7.1.3): coerce to a Number or BigInt primitive.
    // Used by the update operators (`++`/`--`), whose result is the coerced numeric
    // old value — `var y = "1"++` yields the Number 1, not the String "1" — and whose
    // operand must be coerced exactly once.
    public JSValue ToNumeric()
    {
        var primitive = ToNumericPrimitive(this);
        if (primitive.IsBigInt)
            return primitive;

        // Already the answer. ToNumeric minted unconditionally, so `n++` on a Number copied the
        // Number into a second, equal JSNumber to hand back as the old value -- and a JavaScript
        // Number has no observable identity (it compares by value and cannot carry properties),
        // which is the argument the small-integer cache already rests on. Item 3-1's boxing-source
        // census priced the copy at 15.4% of everything the corpus boxes
        // (docs/performance-roadmap.md item 3-1).
        if (primitive.IsNumber && NumericUpdateReuse.Enabled)
        {
            if (ArithmeticOperandDiagnostics.Enabled)
                ArithmeticOperandDiagnostics.RecordUnaryToNumericReused();
            return primitive;
        }

        if (ArithmeticOperandDiagnostics.Enabled)
            ArithmeticOperandDiagnostics.RecordUnaryToNumeric();
        return CreateNumber(primitive.DoubleValue);
    }

    public virtual JSValue Subtract(JSValue value)
    {
        if (ArithmeticOperandDiagnostics.Enabled)
            ArithmeticOperandDiagnostics.RecordGeneric(IsNumber, value.IsNumber);
        var self = ToNumericPrimitive(this);
        value = ToNumericPrimitive(value);
        return !ReferenceEquals(self, this) ? self.Subtract(value) : CreateNumber(DoubleValue - value.DoubleValue);
    }

    public virtual JSValue Multiply(JSValue value)
    {
        if (ArithmeticOperandDiagnostics.Enabled)
            ArithmeticOperandDiagnostics.RecordGeneric(IsNumber, value.IsNumber);
        var self = ToNumericPrimitive(this);
        value = ToNumericPrimitive(value);
        return !ReferenceEquals(self, this) ? self.Multiply(value) : CreateNumber(DoubleValue * value.DoubleValue);
    }

    /// <summary>
    public virtual JSValue Divide(JSValue value)
    {
        if (ArithmeticOperandDiagnostics.Enabled)
            ArithmeticOperandDiagnostics.RecordGeneric(IsNumber, value.IsNumber);
        var self = ToNumericPrimitive(this);
        value = ToNumericPrimitive(value);
        return !ReferenceEquals(self, this) ? self.Divide(value) : CreateNumber(DoubleValue / value.DoubleValue);
    }

    public virtual JSValue BitwiseAnd(JSValue value)
    {
        if (ArithmeticOperandDiagnostics.Enabled)
            ArithmeticOperandDiagnostics.RecordGeneric(IsNumber, value.IsNumber);
        var self = ToNumericPrimitive(this);
        value = ToNumericPrimitive(value);
        return !ReferenceEquals(self, this) ? self.BitwiseAnd(value) : CreateNumber(IntValue & value.IntValue);
    }

    public virtual JSValue BitwiseOr(JSValue value)
    {
        if (ArithmeticOperandDiagnostics.Enabled)
            ArithmeticOperandDiagnostics.RecordGeneric(IsNumber, value.IsNumber);
        var self = ToNumericPrimitive(this);
        value = ToNumericPrimitive(value);
        return !ReferenceEquals(self, this) ? self.BitwiseOr(value) : CreateNumber(IntValue | value.IntValue);
    }

    public virtual JSValue BitwiseXor(JSValue value)
    {
        if (ArithmeticOperandDiagnostics.Enabled)
            ArithmeticOperandDiagnostics.RecordGeneric(IsNumber, value.IsNumber);
        var self = ToNumericPrimitive(this);
        value = ToNumericPrimitive(value);
        return !ReferenceEquals(self, this) ? self.BitwiseXor(value) : CreateNumber(IntValue ^ value.IntValue);
    }

    public virtual JSValue BitwiseNot()
    {
        var self = ToNumericPrimitive(this);
        if (ReferenceEquals(self, this) && ArithmeticOperandDiagnostics.Enabled)
            ArithmeticOperandDiagnostics.RecordUnaryNegate();
        return !ReferenceEquals(self, this) ? self.BitwiseNot() : CreateNumber(~IntValue);
    }

    public virtual JSValue LeftShift(JSValue value)
    {
        if (ArithmeticOperandDiagnostics.Enabled)
            ArithmeticOperandDiagnostics.RecordGeneric(IsNumber, value.IsNumber);
        var self = ToNumericPrimitive(this);
        value = ToNumericPrimitive(value);
        return !ReferenceEquals(self, this) ? self.LeftShift(value) : CreateNumber(IntValue << value.IntValue);
    }

    public virtual JSValue RightShift(JSValue value)
    {
        if (ArithmeticOperandDiagnostics.Enabled)
            ArithmeticOperandDiagnostics.RecordGeneric(IsNumber, value.IsNumber);
        var self = ToNumericPrimitive(this);
        value = ToNumericPrimitive(value);
        return !ReferenceEquals(self, this) ? self.RightShift(value) : CreateNumber(IntValue >> (value.IntValue & 0x1F));
    }

    public virtual JSValue UnsignedRightShift(JSValue value)
    {
        if (ArithmeticOperandDiagnostics.Enabled)
            ArithmeticOperandDiagnostics.RecordGeneric(IsNumber, value.IsNumber);
        var self = ToNumericPrimitive(this);
        value = ToNumericPrimitive(value);
        return !ReferenceEquals(self, this) ? self.UnsignedRightShift(value) : CreateNumber(UIntValue >> value.IntValue);
    }

    public virtual JSValue Modulo(JSValue value)
    {
        if (ArithmeticOperandDiagnostics.Enabled)
            ArithmeticOperandDiagnostics.RecordGeneric(IsNumber, value.IsNumber);
        var self = ToNumericPrimitive(this);
        value = ToNumericPrimitive(value);
        return !ReferenceEquals(self, this) ? self.Modulo(value) : CreateNumber(DoubleValue % value.DoubleValue);
    }

    /// <summary>
    /// Whether <paramref name="value"/> takes the numeric branch of <c>+</c>.
    /// </summary>
    /// <remarks>
    /// §13.15 ApplyStringOrNumericBinaryOperator concatenates only when one side is a String
    /// after ToPrimitive; every other primitive adds via ToNumeric. <c>undefined</c> is one of
    /// those — ToNumber(undefined) is NaN — but <see cref="CanBeNumber"/> excludes it (the
    /// relational operators rely on that), so <c>+</c> widens the test here instead. Without
    /// this, <c>undefined + undefined</c> concatenated to <c>"undefinedundefined"</c>: Octane's
    /// PdfJS computes <c>this.end = (start + length) || this.bytes.length</c> with both
    /// arguments omitted, and that truthy string made every stream report a NaN length.
    /// Symbol and BigInt stay out: they must reach the TypeError paths below.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool AddsNumerically(JSValue value) => value.CanBeNumber || value.IsUndefined;

    /// <summary>
    /// Speed improvements for string contact operations
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public virtual JSValue AddValue(JSValue value)
    {
        if (ArithmeticOperandDiagnostics.Enabled)
            ArithmeticOperandDiagnostics.RecordGeneric(IsNumber, value.IsNumber);
        var self = this is JSObject selfObject ? selfObject.ToDefaultPrimitive() : ValueOf();
        value = value is JSObject valueObject ? valueObject.ToDefaultPrimitive() : value;

        if (!ReferenceEquals(self, this))
            return self.AddValue(value);

        if (AddsNumerically(self) && AddsNumerically(value))
            return CreateNumber(self.DoubleValue + value.DoubleValue);

        // §13.15 ApplyStringOrNumericBinaryOperator for `+`: once neither operand (after
        // ToPrimitive) is a String the operands add numerically via ToNumeric, and mixing a
        // BigInt with a non-BigInt numeric value (Number/Boolean/null/undefined) is a
        // TypeError rather than a string concatenation — e.g. `true + 1n`.
        if (!self.IsString && !value.IsString && (self.IsBigInt ^ value.IsBigInt))
            throw NewTypeError("Cannot mix BigInt and other types, use explicit conversions");

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
        if (ArithmeticOperandDiagnostics.Enabled)
            ArithmeticOperandDiagnostics.RecordRawDoubleOperand(IsNumber);
        // §13.15 ApplyStringOrNumericBinaryOperator: the left operand is first
        // coerced with ToPrimitive (default hint). Going through ToDefaultPrimitive
        // — rather than the raw CLR ValueOf() — lets a wrapper observe an overridden
        // valueOf / @@toPrimitive (e.g. a boxed Symbol whose valueOf was replaced).
        var self = this is JSObject selfObject ? selfObject.ToDefaultPrimitive() : ValueOf();
        if (!ReferenceEquals(self, this))
            return self.AddValue(value);

        if (AddsNumerically(self))
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

        // §10.4.7.1 Immutable prototype exotic objects (e.g. %Object.prototype%):
        // the change only succeeds when the new value equals the current prototype.
        if (this is JSObject { } immutableProtoObject && (immutableProtoObject.status & ObjectStatus.ImmutablePrototype) != 0)
        {
            var currentProto = prototypeChain?.Object;
            var unchanged = currentProto == null ? target == NullValue : ReferenceEquals(currentProto, target);
            if (unchanged)
                return true;

            error = "Immutable prototype object cannot have its prototype changed";
            return false;
        }

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

        var key = propertyKey.ToKey(false);

        // §10.1.7 OrdinaryHasProperty only needs an existence check. Avoid building
        // the observable descriptor object used by Object.getOwnPropertyDescriptor.
        if (target.HasOwnProperty(in key))
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

        return !p.IsProperty ? ResolvePropertyValue(p.value) : InvokePropertyGetter(p.get, this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static JSValue ResolvePropertyValue(IPropertyValue value) => value switch
    {
        JSValue jsValue => jsValue,
        LazyDataPropertyCell lazy => lazy.Resolve(),
        IDeferredPropertyValue deferred => deferred.Resolve(),
        null => UndefinedValue,
        _ => throw new InvalidOperationException($"Unsupported property value storage '{value.GetType().FullName}'.")
    };

    public virtual JSValue GetOwnProperty(in KeyString name)
    {
        var pc = prototypeChain;

        if (pc != null)
            return GetValue(pc.GetInternalProperty(name));

        return UndefinedValue;
    }

    public virtual JSValue GetOwnProperty(uint name)
    {
        var pc = prototypeChain;

        if (pc != null)
            return GetValue(pc.GetInternalProperty(name));

        return UndefinedValue;
    }

    public virtual JSValue GetOwnProperty(IJSSymbol name)
    {
        var pc = prototypeChain;

        if (pc != null)
            return GetValue(pc.GetInternalProperty(name));

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

    /// <summary>
    /// Reads an indexed property whose key is a raw <see cref="double"/> — <c>a[i]</c> where the
    /// compiler holds <c>i</c> unboxed — without materializing a <see cref="JSNumber"/> for the
    /// key (roadmap item 3-0).
    /// </summary>
    /// <remarks>
    /// A numeric local's readable expression boxes its storage so that every consumer expecting
    /// a <see cref="JSValue"/> keeps working, and an index expression was one of those consumers.
    /// Measured, that box is **~32 bytes on every indexed read** and, since the element itself is
    /// already a heap object, it is the *entire* allocation of one — a constant-index read
    /// (<c>a[0]</c>, which lowers to a <c>uint</c> key) allocates nothing at all.
    /// <para>
    /// The guard is what makes it legal rather than merely fast. Only a non-negative integral
    /// double below 2^32-1 names an array index; everything else is an ordinary string-keyed
    /// property, and <c>a[1.5]</c>, <c>a[-1]</c> and <c>a[1e30]</c> all have to keep resolving
    /// the key through <c>ToPropertyKey</c>. Each rejection falls back to exactly the boxed path
    /// that ran before, so a guard that is too strict costs a box and never a wrong answer.
    /// </para>
    /// <para>
    /// <c>-0</c> is deliberately admitted: it passes <c>index >= 0</c>, converts to <c>0</c>, and
    /// <c>ToString(-0)</c> is <c>"0"</c> — so slot 0 is the key the spec asks for. NaN fails the
    /// first comparison and every infinity fails the second.
    /// </para>
    /// <para>
    /// A null or undefined base is sent to the boxed arm before the guard is even consulted; see
    /// the comment on that test for why the fast arm cannot carry it.
    /// </para>
    /// </remarks>
    public JSValue GetElementByNumber(double index)
    {
        // ToObject(base) precedes ToPropertyKey(key) (6.2.5.5), so a null or undefined base has to
        // throw before the index is looked at at all. The fast arm below cannot express that: it
        // calls GetValue(uint, ...) directly, and that virtual's base implementation answers
        // `undefined` for any value with no prototype chain — which is exactly what null and
        // undefined are. JSUndefined/JSNull override the this[uint] INDEXER to throw, so a
        // *constant* index (`u[0]`, which lowers to a uint key) always threw; a *variable* one
        // reaches this method instead, and so `var k = 3; u[k]` silently evaluated to undefined
        // rather than raising a TypeError — feeding a wrong value onward instead of stopping
        // the script, which is how it surfaced: minified code carrying on for thousands of
        // instructions past its first broken load.
        //
        // The boxed arm already throws, because its JSValue-keyed GetValue tests IsNullOrUndefined
        // (and names the key, per DescribeKeyForDiagnostic). Routing a nullish base there rather
        // than repeating the throw here is what keeps this read's message identical to the one a
        // variable index reported before the unboxed fast path existed — and identical to what a
        // browser reports for the same expression. The two reference comparisons cost nothing on
        // a base that is about to throw anyway.
        //
        // The write twin needs no such test: SetElementByNumber routes its failures through
        // ThrowOnFailedElementAssignment, which checks IsNullOrUndefined itself.
        if (IsNullOrUndefined)
            return GetValue(CreateNumber(index), this);

        // 2^32-1 is NOT an array index (it is the one canonical numeric string above the range),
        // so the bound is 2^32-2 and the comparison is inclusive.
        if (index >= 0 && index <= 4294967294d)
        {
            var element = (uint)index;
            if (element == index)
                return GetValue(element, this);
        }

        return GetValue(CreateNumber(index), this);
    }

    /// <summary>
    /// Writes an indexed property whose key is a raw <see cref="double"/> — <c>a[i] = v</c> where
    /// the compiler holds <c>i</c> unboxed — without materializing a <see cref="JSNumber"/> for
    /// the key (roadmap item 3-0). Returns the assigned value, which is what the assignment
    /// expression evaluates to.
    /// </summary>
    /// <remarks>
    /// The write twin of <see cref="GetElementByNumber"/> and guarded on exactly the same terms;
    /// see there for why the bound is 2^32-2 and why <c>-0</c> is admitted.
    /// <para>
    /// Both arms go through <see cref="SetValue(uint, JSValue, JSValue, bool)"/> and its JSValue
    /// twin rather than through the indexers, and the failure handling below is the
    /// <c>this[JSValue]</c> setter's, copied deliberately. That is the setter a variable index
    /// used before this item, so keeping it keeps the messages: a null or undefined receiver
    /// reports "Cannot set properties of … (setting '0')" rather than the <c>this[uint]</c>
    /// override's "Cannot set property 0 of …", which is what a *constant* index has always
    /// reported. Both name the key; reconciling the two WORDINGS is still a separate item.
    /// </para>
    /// </remarks>
    public JSValue SetElementByNumber(double index, JSValue value)
    {
        var strict = IsStrictModeEnabled?.Invoke() == true;

        if (index >= 0 && index <= 4294967294d)
        {
            var element = (uint)index;
            if (element == index)
            {
                if (!SetValue(element, value, this, strict))
                    ThrowOnFailedElementAssignment(element);

                return value;
            }
        }

        var key = CreateNumber(index);
        if (!SetValue(key, value, this, strict))
            ThrowOnFailedElementAssignment(key);

        return value;
    }

    private void ThrowOnFailedElementAssignment(object key)
    {
        if (IsNullOrUndefined)
            throw NewTypeError?.Invoke($"Cannot set properties of {this}{DescribeElementKeyForDiagnostic(key)}")
                ?? new InvalidOperationException("JSValue.NewTypeError delegate is not initialized. Ensure the BuiltIns assembly module initializer has run.");

        ThrowOnStrictPrimitiveAssignment(key);
    }

    /// <summary>
    /// Names the element in this method's "Cannot set properties of undefined" message, the way
    /// <see cref="DescribeKeyForDiagnostic"/> does for a read.
    /// </summary>
    /// <remarks>
    /// The key arrives boxed as <see cref="object"/> because the two call sites hold it as a
    /// <see cref="uint"/> and as a <see cref="JSValue"/> respectively — but in both cases it is the
    /// index this method was handed, so it is always a number and always safe to render. The
    /// JSValue arm still goes through the shared describer, so its object-key guard applies by
    /// construction rather than by this method being trusted not to need it.
    /// </remarks>
    private static string DescribeElementKeyForDiagnostic(object key)
        => key is JSValue value ? DescribeKeyForDiagnostic(value, setting: true) : $" (setting '{key}')";

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
                throw NewTypeError?.Invoke($"Cannot set properties of {this}{DescribeKeyForDiagnostic(key, setting: true)}")
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

    /// <summary>
    /// Names the property in a "Cannot read properties of undefined" message, as a browser does
    /// ("... (reading 'foo')"). Without it the message says only that *something* was read off
    /// nothing, which on minified code is not enough to find the line, let alone the cause.
    /// </summary>
    /// <remarks>
    /// Only a key that is already a primitive is named. Converting one is what the caller must
    /// NOT do here — GetValue throws before ToPropertyKey precisely because ToObject(base)
    /// comes first (6.2.5.5), and an object key's toString/@@toPrimitive is user code that would
    /// then run in an order the spec forbids. So an object key is left undescribed rather than
    /// coerced for a diagnostic.
    /// <para>
    /// EVERY primitive is named, which is the whole of the rule: a boolean, <c>null</c>,
    /// <c>undefined</c> and a BigInt all render from their own value with no user code involved,
    /// exactly as a string or a number does. Listing only some of them was an allowlist with no
    /// principle behind it — <c>u[k]</c> with <c>k</c> false reported a bare "Cannot read
    /// properties of undefined" while <c>k</c> <c>0</c> named the key, so the message got worse
    /// precisely where a reader could least guess what <c>k</c> held.
    /// </para>
    /// <para>
    /// Testing <see cref="IsObject"/> is what enforces the rule, and it is exact rather than
    /// approximate: <c>JSObject</c> is the only branch of the hierarchy whose <c>ToString</c> can
    /// reach user code (<c>JSPrimitiveObject</c> coerces through an overridable
    /// <c>toString</c>/<c>valueOf</c>, and every function, array and proxy is a JSObject too).
    /// Everything else deriving from <see cref="JSValue"/> — <c>JSPrimitive</c> and its
    /// subclasses, <c>JSNull</c>, <c>JSUndefined</c> — renders from its own state. The try/catch
    /// remains for the base <c>ToString</c>, which throws for a type that overrides neither.
    /// </para>
    /// </remarks>
    private static string DescribeKeyForDiagnostic(JSValue key, bool setting = false)
    {
        if (key == null || key.IsObject)
            return string.Empty;

        try
        {
            return $" ({(setting ? "setting" : "reading")} '{RenderKeyForDiagnostic(key)}')";
        }
        catch (Exception)
        {
            // A description must never replace the error it is describing.
            return string.Empty;
        }
    }

    /// <summary>
    /// The text a primitive key contributes to <see cref="DescribeKeyForDiagnostic"/>.
    /// </summary>
    /// <remarks>
    /// Everything but a Symbol renders as itself. A Symbol has to be spelled the way
    /// <c>Symbol.prototype.toString</c> spells it — "Symbol(s)", not the bare description —
    /// because the bare form is ambiguous with the string key <c>"s"</c>, and those are different
    /// properties. JSSymbol.ToDescriptiveString does this, but it lives in BuiltIns and this
    /// assembly cannot see it; <see cref="IJSSymbol.DescriptionIsUndefined"/> is on the interface
    /// for the same reason and distinguishes the two cases <c>ToString()</c> renders identically
    /// as "" — <c>Symbol()</c> and <c>Symbol("")</c>.
    /// </remarks>
    private static string RenderKeyForDiagnostic(JSValue key)
        => key is IJSSymbol symbol
            ? (symbol.DescriptionIsUndefined ? "Symbol()" : $"Symbol({key})")
            : key.ToString();

    internal JSValue GetValue(JSValue key, JSValue receiver, bool throwError = true)
    {
        // Per spec (6.2.5.5 GetValue), ToObject(base) must precede ToPropertyKey(key).
        // For null/undefined, ToObject throws TypeError before the key is converted.
        if (IsNullOrUndefined)
            throw NewTypeError?.Invoke($"Cannot read properties of {this}{DescribeKeyForDiagnostic(key)}")
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

    // OrdinarySet on a primitive base value (number/string/boolean/symbol/bigint):
    // §6.2.5.6 PutValue boxes the base with ToObject, so [[Set]] must walk the wrapper
    // prototype chain object-by-object with the primitive as the receiver — NOT flatten
    // it into a single descriptor lookup. Walking it reaches an inherited accessor's
    // setter AND any exotic [[Set]] in the chain, e.g. a Proxy (test262:
    // language/types/reference/put-value-prop-base-primitive). A data property — or no
    // property — cannot be created on a primitive, so SetKeyStringOnReceiver fails as a
    // no-op (non-strict) / throws (strict) when the walk reaches the terminal create.
    public virtual bool SetValue(uint key, JSValue value, JSValue receiver, bool throwError = true)
        => prototypeChain?.Object is JSObject proto && proto.SetValue(key, value, receiver ?? this, throwError);

    internal protected virtual bool SetValue(KeyString key, JSValue value, JSValue receiver, bool throwError = true)
        => prototypeChain?.Object is JSObject proto && proto.SetValue(key, value, receiver ?? this, throwError);

    internal protected virtual bool SetValue(IJSSymbol key, JSValue value, JSValue receiver, bool throwError = true)
        => prototypeChain?.Object is JSObject proto && proto.SetValue(key, value, receiver ?? this, throwError);

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

        // `delete o[p]` performs ToPropertyKey(p) exactly once (when the [[Delete]]
        // that produced `result` coerced the key). Interpolating an object-valued
        // `key` into the message would call its `toString` a *second* time — observable
        // re-coercion the spec forbids (test262 sm/strict/8.12.7-2). Only embed the key
        // when it is already a primitive (string/number/symbol), which carries no
        // user-visible coercion.
        var keyText = key.IsObject ? "property" : $"property {key}";
        throw NewTypeError?.Invoke($"Cannot delete {keyText} of {target}")
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
        using var counted = ArithmeticOperandDiagnostics.Enabled
            ? ArithmeticOperandDiagnostics.Relate(IsNumber, value.IsNumber)
            : default;
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
        using var counted = ArithmeticOperandDiagnostics.Enabled
            ? ArithmeticOperandDiagnostics.Relate(IsNumber, value.IsNumber)
            : default;
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
        using var counted = ArithmeticOperandDiagnostics.Enabled
            ? ArithmeticOperandDiagnostics.Relate(IsNumber, value.IsNumber)
            : default;
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
        using var counted = ArithmeticOperandDiagnostics.Enabled
            ? ArithmeticOperandDiagnostics.Relate(IsNumber, value.IsNumber)
            : default;
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
        if (ArithmeticOperandDiagnostics.Enabled)
            ArithmeticOperandDiagnostics.RecordGeneric(IsNumber, a.IsNumber);
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
    // "Value is not iterable" names nothing — not the value, not even its type — and it was the
    // message `[...undefined]` produced, because undefined and null were the only two kinds of
    // value that did not override this. Every other one does and every other one names itself:
    // JSObject and JSPrimitive resolve @@iterator and fall back to NotIterable(this), JSString
    // and Intl.Segments enumerate. The two singletons now override it too
    // (JSUndefined.GetIterableEnumerator), so nothing in the engine reaches this line; it stays
    // as the answer for a subtype that adds neither.
    public virtual IElementEnumerator GetIterableEnumerator() => throw NewTypeError("Value is not iterable");
    public virtual IElementEnumerator GetAsyncIterableEnumerator() => GetIterableEnumerator();

    // GetIterator using an @@iterator method already fetched by the caller (GetMethod).
    // Callers such as Array.from perform their own GetMethod(items, @@iterator) — re-reading
    // the property here would be an extra observable [[Get]] on a Proxy/getter
    // (test262 sm/Array/from_proxy). The receiver for the Call(method, items) step is this
    // value (a primitive string is passed through unwrapped, matching GetV semantics).
    public IElementEnumerator GetIterableEnumerator(JSValue iteratorMethod)
    {
        if (iteratorMethod.IsNullOrUndefined)
            throw NewTypeError(JSException.NotIterable(this));

        if (!iteratorMethod.IsFunction)
            throw NewTypeError("@@iterator is not a function");

        var iteratorResult = iteratorMethod.InvokeFunction(new Arguments(this));
        if (!iteratorResult.IsObject)
            throw NewTypeError("@@iterator result is not an object");

        return new JSIterator(iteratorResult);
    }

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
