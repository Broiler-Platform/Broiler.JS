using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Storage;
using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using Expression = Broiler.JavaScript.ExpressionCompiler.Expressions.BExpression;

namespace Broiler.JavaScript.Runtime;

public class JSVariable
{
    // A user-compiled anonymous function reports name "" yet stays eligible for
    // NamedEvaluation, tracked by IJSFunction.IsAnonymousNamePending; name-less native
    // functions instead carry the legacy "native" placeholder. NamedEvaluation later
    // overwrites the name (with the binding/property name) or clears it to "" and drops
    // the pending flag. We must only ever act on a still-eligible function, and detect
    // it WITHOUT triggering a user-defined `name` getter: reading the public [[Get]]
    // would invoke an accessor the script installed via Object.defineProperty (e.g.
    // `var t = Object.defineProperty(function(){}, 'name', { get(){throw} })`), which is
    // observable and per spec must not happen here. Check the flag / own data property
    // directly instead.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool HasPlaceholderName(JSObject functionObject)
    {
        // Inspect the own `name` data property directly (never the public [[Get]]) so a
        // user-installed `name` getter is not observably invoked. A redefined accessor
        // (or absent property) is not a value — never rename in that case.
        var nameProperty = functionObject.GetInternalProperty(KeyStrings.name, false);
        if (!nameProperty.IsValue)
            return false;

        var nameString = (nameProperty.value as JSValue)?.ToString();

        // Name-less native functions keep the legacy "native" placeholder.
        if (nameString == "native")
            return true;

        // A user-compiled anonymous function reports "" and stays eligible for
        // NamedEvaluation until a name is inferred, tracked by an explicit flag. Confirm
        // the property still holds that default empty name — if the script (or the
        // dynamic Function constructor's "anonymous") gave it another value, leave it.
        return string.IsNullOrEmpty(nameString)
            && functionObject is IJSFunction { IsAnonymousNamePending: true };
    }

    // BROILER-PATCH: Support read-only variables for function expression names (ES3 §13)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JSValue PrepareAnonymousFunctionNameForDestructuring(JSValue value, string name, bool assignName)
    {
        if (value is not JSObject functionObject || value is not IJSFunction fn)
            return value;

        if (!HasPlaceholderName(functionObject))
            return value;

        functionObject.FastAddValue(KeyStrings.name, JSValue.CreateString(assignName ? name : string.Empty), JSPropertyAttributes.ConfigurableReadonlyValue);
        fn.IsAnonymousNamePending = false;
        return value;
    }

    private static JSValue PrepareAnonymousFunctionName(JSValue value, string name)
    {
        if (value is not JSObject functionObject || value is not IJSFunction fn)
            return value;

        if (!HasPlaceholderName(functionObject))
            return value;

        functionObject.FastAddValue(KeyStrings.name, JSValue.CreateString(name), JSPropertyAttributes.ConfigurableReadonlyValue);
        fn.IsAnonymousNamePending = false;
        return value;
    }

    // NamedEvaluation for a non-computed class field initializer (ClassFieldDefinitionEvaluation):
    // a public field names the function with the field name, a private field with the
    // PrivateName string (e.g. "#field"). The name is known at compile time.
    public static JSValue PrepareAnonymousFunctionNameForField(JSValue value, string name)
        => PrepareAnonymousFunctionName(value, name);

    public static JSValue PrepareAnonymousFunctionNameForProperty(JSValue value, uint name)
        => PrepareAnonymousFunctionName(value, name.ToString(CultureInfo.InvariantCulture));

    public static JSValue PrepareAnonymousFunctionNameForProperty(JSValue value, KeyString name)
        => PrepareAnonymousFunctionName(value, name.ToString());

    public static JSValue PrepareAnonymousFunctionNameForProperty(JSValue value, JSValue name)
        => PrepareAnonymousFunctionName(value, PropertyKeyToFunctionName(name));

    // NamedEvaluation for a computed-key accessor (`get [expr]() {}` / `set [expr]() {}`):
    // SetFunctionName receives the "get"/"set" prefix, so the name is "get [desc]" /
    // "set foo" etc. The base name follows the same rules as PrepareAnonymousFunctionName
    // ForProperty (symbols → "[desc]"/"" , numeric → string, string keys verbatim).
    public static JSValue PrepareAnonymousFunctionNameForGetter(JSValue value, JSValue name)
        => PrepareAnonymousFunctionName(value, "get " + PropertyKeyToFunctionName(name));

    public static JSValue PrepareAnonymousFunctionNameForSetter(JSValue value, JSValue name)
        => PrepareAnonymousFunctionName(value, "set " + PropertyKeyToFunctionName(name));

    public static JSValue PrepareAnonymousFunctionNameForGetter(JSValue value, uint name)
        => PrepareAnonymousFunctionName(value, "get " + name.ToString(CultureInfo.InvariantCulture));

    public static JSValue PrepareAnonymousFunctionNameForSetter(JSValue value, uint name)
        => PrepareAnonymousFunctionName(value, "set " + name.ToString(CultureInfo.InvariantCulture));

    private static string PropertyKeyToFunctionName(JSValue name)
    {
        if (name is IJSSymbol symbol)
            return SymbolFunctionNamePrefix(symbol);

        var propertyKey = name.ToKey(false);
        return propertyKey.Type switch
        {
            KeyType.UInt => propertyKey.Index.ToString(CultureInfo.InvariantCulture),
            KeyType.String => propertyKey.KeyString.ToString(),
            KeyType.Symbol => SymbolFunctionNamePrefix(propertyKey.Symbol),
            _ => string.Empty,
        };
    }

    // SetFunctionName with a symbol key: an undefined description (`Symbol()`) gives the
    // empty name, while any other (including the empty string, `Symbol("")`) gives
    // "[" + description + "]" — so `Symbol("")` yields "[]", not "".
    private static string SymbolFunctionNamePrefix(IJSSymbol symbol)
        => symbol == null || symbol.DescriptionIsUndefined ? string.Empty : $"[{symbol}]";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private JSValue InferAnonymousFunctionName(JSValue value)
    {
        if (Name.IsEmpty || value is not JSObject functionObject || value is not IJSFunction fn)
            return value;

        if (!HasPlaceholderName(functionObject))
            return value;

        functionObject.FastAddValue(KeyStrings.name, JSValue.CreateString(Name.Value), JSPropertyAttributes.ConfigurableReadonlyValue);
        fn.IsAnonymousNamePending = false;
        return value;
    }

    private JSValue _value;
    private bool _isInitialized = true;

    /// <summary>
    /// Whether this binding has been initialized. A binding in its temporal dead
    /// zone (uninitialized) throws when its <see cref="Value"/> is read; callers
    /// that need to inspect a binding without triggering that error can check this.
    /// </summary>
    internal bool IsInitialized => _isInitialized;

    private string ReferenceErrorMessage
        => Name.IsEmpty ? "Cannot access variable before initialization" : $"Cannot access '{Name.Value}' before initialization";

    public JSValue Value
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (!_isInitialized)
                throw (NewReferenceErrorFactory ?? throw new InvalidOperationException("JSVariable.NewReferenceErrorFactory delegate is not initialized. Ensure the Engine assembly module initializer has run."))
                    (ReferenceErrorMessage);

            return _value;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            if (!IsReadOnly)
            {
                _value = InferAnonymousFunctionName(value);
                _isInitialized = true;
                return;
            }

            if (ThrowOnReadOnlyWrite || IsStrictMode?.Invoke() == true)
                throw (JSException.NewTypeErrorFactory
                    ?? throw new InvalidOperationException("JSException.NewTypeErrorFactory delegate is not initialized. Ensure the Core assembly module initializer has run."))
                    ("Cannot assign to read only variable");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public JSValue Assign(JSValue value)
    {
        if (!_isInitialized)
            throw (NewReferenceErrorFactory ?? throw new InvalidOperationException("JSVariable.NewReferenceErrorFactory delegate is not initialized. Ensure the Engine assembly module initializer has run."))
                (ReferenceErrorMessage);

        Value = value;
        return _value;
    }

    /// <summary>
    /// Reads this binding's value. For an ordinary binding this is just
    /// <see cref="Value"/>; <see cref="EvalShadowVariable"/> overrides it to forward
    /// to its outer binding until a direct eval introduces the binding. Kept as a
    /// separate virtual method so the hot <see cref="Value"/> property stays
    /// non-virtual and inlinable for the common case.
    /// </summary>
    public virtual JSValue GetValue() => Value;

    /// <summary>
    /// Writes this binding's value. Mirrors <see cref="GetValue"/> for the
    /// shadow-binding forwarding case.
    /// </summary>
    public virtual JSValue SetValue(JSValue value)
    {
        Value = value;
        return _value;
    }

    // Reference-stable compound assignment (`x op= y`). The spec resolves the
    // target Reference once, before the right-hand side runs, and the write uses
    // that same Reference even if the RHS (via a direct eval) introduces a more
    // local binding. CaptureReference records which binding the read observed;
    // Get/SetCaptured then read/write that same binding. For an ordinary binding
    // the reference is always this binding, so the captured token is ignored.
    public virtual bool CaptureReference() => true;
    public virtual JSValue GetCaptured(bool reference) => GetValue();
    public virtual JSValue SetCaptured(bool reference, JSValue value) => SetValue(value);

    /// <summary>
    /// Implements BindThisValue for a derived class constructor's <c>this</c>
    /// binding: the binding may be initialized only once, so a second
    /// <c>super(...)</c> call (the binding is already initialized) is a
    /// ReferenceError. Unlike <see cref="Assign"/>, this binds an as-yet
    /// uninitialized binding rather than requiring it to already be initialized.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public JSValue BindThis(JSValue value)
    {
        if (_isInitialized)
            throw (NewReferenceErrorFactory ?? throw new InvalidOperationException("JSVariable.NewReferenceErrorFactory delegate is not initialized. Ensure the Engine assembly module initializer has run."))
                ("Super constructor may only be called once per derived class instance");

        Value = value;
        return _value;
    }
    internal bool IsReadOnly;
    internal bool ThrowOnReadOnlyWrite;

    // True for a top-level script let/const/class binding published into the global
    // lexical environment. A later eval that declares a global var of the same name is a
    // SyntaxError (EvalDeclarationInstantiation, var/global-lexical collision).
    internal bool IsGlobalLexical;

    static readonly PropertyInfo _ValueProperty = typeof(JSVariable).GetProperty("Value");
    internal readonly StringSpan Name;
    private KeyString key;

    /// <summary>
    /// Delegate that retrieves the current JavaScript execution context.
    /// Wired by Core's module initializer to point to JSEngine.Current.
    /// </summary>
    internal static Func<object> GetCurrentContext;
    internal static Func<bool> IsStrictMode;
    internal static Func<string, JSException> NewReferenceErrorFactory;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public JSVariable(JSValue v, string name)
    {
        _value = v;
        Name = name;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public JSVariable(JSValue v, string name, bool initialized)
    {
        _value = v;
        Name = name;
        _isInitialized = initialized;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public JSVariable(JSValue v, in StringSpan name)
    {
        _value = v;
        Name = name;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public JSVariable(in Arguments a, int i, string name)
    {
        _value = a.GetAt(i);
        Name = name;
    }

    public JSValue GlobalValue
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (!_isInitialized)
                throw (NewReferenceErrorFactory ?? throw new InvalidOperationException("JSVariable.NewReferenceErrorFactory delegate is not initialized. Ensure the Engine assembly module initializer has run."))
                    (ReferenceErrorMessage);

            if (key.Value == null)
                key = KeyStrings.GetOrCreate(Name);

            if (GetCurrentContext?.Invoke() is JSObject ctx)
                return _value = ctx[key];

            return _value;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            value = InferAnonymousFunctionName(value);
            _value = value;
            _isInitialized = true;
            if (key.Value == null)
                key = KeyStrings.GetOrCreate(Name);

            if (GetCurrentContext?.Invoke() is JSObject ctx)
            {
                var property = ctx.GetInternalProperty(key, false);
                if (property.IsEmpty)
                {
                    var register = ctx.GetType().GetMethod("Register", [typeof(JSVariable)]);
                    if (register != null)
                    {
                        register.Invoke(ctx, [this]);
                        return;
                    }

                    ctx.FastAddValue(key, value, JSPropertyAttributes.Value | JSPropertyAttributes.Enumerable);
                    return;
                }

                var old = ctx[key];
                if (old != value)
                    ctx[key] = value;
            }
        }
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public JSVariable(Exception e, string name) : this(e is JSException je ? je.Error : JSException.From(e).Error, name) { }

    public static Expression ValueExpression(Expression exp) => Expression.Property(exp, _ValueProperty);
}
