using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Storage;
using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using Expression = Broiler.JavaScript.ExpressionCompiler.Expressions.YExpression;

namespace Broiler.JavaScript.Runtime;

public class JSVariable
{
    // BROILER-PATCH: Support read-only variables for function expression names (ES3 §13)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JSValue PrepareAnonymousFunctionNameForDestructuring(JSValue value, string name, bool assignName)
    {
        if (value is not JSObject functionObject || value is not IJSFunction)
            return value;

        if (functionObject[KeyStrings.name].ToString() != "native")
            return value;

        functionObject.FastAddValue(KeyStrings.name, JSValue.CreateString(assignName ? name : string.Empty), JSPropertyAttributes.ConfigurableReadonlyValue);
        return value;
    }

    private static JSValue PrepareAnonymousFunctionName(JSValue value, string name)
    {
        if (value is not JSObject functionObject || value is not IJSFunction)
            return value;

        if (functionObject[KeyStrings.name].ToString() != "native")
            return value;

        functionObject.FastAddValue(KeyStrings.name, JSValue.CreateString(name), JSPropertyAttributes.ConfigurableReadonlyValue);
        return value;
    }

    public static JSValue PrepareAnonymousFunctionNameForProperty(JSValue value, uint name)
        => PrepareAnonymousFunctionName(value, name.ToString(CultureInfo.InvariantCulture));

    public static JSValue PrepareAnonymousFunctionNameForProperty(JSValue value, KeyString name)
        => PrepareAnonymousFunctionName(value, name.ToString());

    public static JSValue PrepareAnonymousFunctionNameForProperty(JSValue value, JSValue name)
    {
        if (name is IJSSymbol symbol)
            return PrepareAnonymousFunctionName(value, symbol.ToString().Length == 0 ? string.Empty : $"[{symbol}]");

        var propertyKey = name.ToKey(false);
        return propertyKey.Type switch
        {
            KeyType.UInt => PrepareAnonymousFunctionName(value, propertyKey.Index.ToString(CultureInfo.InvariantCulture)),
            KeyType.String => PrepareAnonymousFunctionName(value, propertyKey.KeyString.ToString()),
            KeyType.Symbol => PrepareAnonymousFunctionName(value,
                propertyKey.Symbol?.ToString() is { Length: > 0 } description ? $"[{description}]" : string.Empty),
            _ => value,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private JSValue InferAnonymousFunctionName(JSValue value)
    {
        if (Name.IsEmpty || value is not JSObject functionObject || value is not IJSFunction)
            return value;

        if (functionObject[KeyStrings.name].ToString() != "native")
            return value;

        functionObject.FastAddValue(KeyStrings.name, JSValue.CreateString(Name.Value), JSPropertyAttributes.ConfigurableReadonlyValue);
        return value;
    }

    private JSValue _value;
    private bool _isInitialized = true;

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
    internal bool IsReadOnly;
    internal bool ThrowOnReadOnlyWrite;

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
