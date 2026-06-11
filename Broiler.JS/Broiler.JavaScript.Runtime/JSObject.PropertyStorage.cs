using Broiler.JavaScript.Storage;
using Broiler.JavaScript.Ast.Misc;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
namespace Broiler.JavaScript.Runtime;

public partial class JSObject
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref PropertySequence GetOwnProperties(bool create = true) => ref ownProperties;

    /// <summary>
    /// Internal marker character prefixed to a private name's property key so it
    /// occupies a key space disjoint from ordinary string properties. A class's
    /// private <c>#x</c> and a public <c>"#x"</c> string property must be distinct
    /// bindings (sec-privatefieldget); the compiler emits this marker for private
    /// member references, and reflection/enumeration hides keys carrying it.
    /// </summary>
    public const char PrivateNameMarker = '\u0001';

    // Separates a private name's text from a per-evaluation uniquifier in a minted
    // private key (see MintPrivateName). Distinct from PrivateNameMarker.
    private const char PrivateNameEvalSeparator = '';

    private static int privateNameCounter;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsPrivateName(in KeyString key)
    {
        var value = key.Value.Value;
        return !string.IsNullOrEmpty(value) && value[0] == PrivateNameMarker;
    }

    /// <summary>
    /// Mints a fresh private-name key for one class evaluation. Each call returns a
    /// distinct key, so a private element installed by one evaluation of a class is
    /// not visible on instances produced by another evaluation — the key itself is
    /// the per-evaluation PrivateBrand (brand-check-multiple-evaluations). The
    /// compiler stores the result in a class-evaluation-scope variable that every
    /// member reference closes over. <paramref name="name"/> already carries the
    /// leading '#'.
    /// </summary>
    public static KeyString MintPrivateName(string name)
        => KeyStrings.GetOrCreate(
            PrivateNameMarker + name + PrivateNameEvalSeparator + Interlocked.Increment(ref privateNameCounter));

    // Ergonomic brand check `#name in rval` (RelationalExpression : PrivateIdentifier
    // in ShiftExpression). Returns true when rval carries the private name, false
    // otherwise; a non-object rval is a TypeError. Uses the same internal lookup as a
    // private member access, so `#x in obj` is true exactly when `obj.#x` would not
    // throw a brand-check TypeError.
    public static JSValue PrivateNameIn(KeyString key, JSValue rval)
    {
        if (rval is not JSObject obj)
            throw NewTypeError("Cannot use 'in' operator to check for a private name in a non-object");

        return obj.GetInternalProperty(key).IsEmpty ? JSValue.BooleanFalse : JSValue.BooleanTrue;
    }

    // Brand check for a private member access (`obj.#x`). A private name must be
    // present as an OWN element of the receiver (PrivateBrandCheck inspects
    // O.[[PrivateBrands]] / [[PrivateElements]] directly — it never walks the
    // prototype chain). Instance private fields/methods are installed own on each
    // instance, and static private elements own on the class constructor, so an
    // own-only lookup matches them all. Crucially, a subclass constructor inherits
    // its super-class through the constructor prototype chain but does NOT carry the
    // super-class's static private brand: `class C { static #g(){} static f(){
    // return this.#g(); } } class D extends C {}; D.f()` must throw (test262
    // static-private-method-subclass-receiver). The check observes neither
    // getters/setters nor Proxy traps. Field *initialization* never reaches here.
    private void ThrowIfMissingPrivateMember(in KeyString key, bool reading)
    {
        if (!GetInternalProperty(key, inherited: false).IsEmpty)
            return;

        ThrowMissingPrivateMember(in key, reading);
    }

    // Raises the brand-check TypeError for a private member access whose receiver
    // does not carry the private name. Also used by the primitive path: a raw
    // primitive (the boxed wrapper ToObject would create) can never hold a private
    // field, so `(15).#x` / `"s".#x` is always a TypeError.
    internal static void ThrowMissingPrivateMember(in KeyString key, bool reading)
    {
        var display = PrivateDisplayName(in key);
        throw NewTypeError(reading
            ? $"Cannot read private member {display} from an object whose class did not declare it"
            : $"Cannot write private member {display} to an object whose class did not declare it");
    }

    // Recovers a private name's human-readable text (e.g. "#x") from a minted key
    // for diagnostics, dropping the internal marker and per-evaluation uniquifier.
    private static string PrivateDisplayName(in KeyString key)
    {
        var s = key.Value.Value;
        if (string.IsNullOrEmpty(s) || s[0] != PrivateNameMarker)
            return "#<unknown>";

        var end = s.IndexOf(PrivateNameEvalSeparator, 1);
        return end < 0 ? s[1..] : s[1..end];
    }

    // Shared guard for PrivateFieldAdd / PrivateMethodOrAccessorAdd: adding a
    // private element to a non-extensible object is a TypeError (the
    // nonextensible-applies-to-private refinement), and so is re-adding a private
    // name the object already carries — observable when a derived constructor's
    // return-override hands the same object to two installations.
    private void PrivateElementAddGuard(in KeyString key)
    {
        if (!IsExtensible())
            throw NewTypeError($"Cannot add private member {PrivateDisplayName(in key)} to a non-extensible object");

        if (!ownProperties.GetValue(key.Key).IsEmpty)
            throw NewTypeError($"Cannot add private member {PrivateDisplayName(in key)}: it is already present on the object");
    }

    /// <summary>
    /// PrivateFieldAdd (ECMA-262 § 7.3.28): installs a private field on this object
    /// during instance-field initialization. The field is stored directly as an
    /// internal slot, bypassing Proxy traps.
    /// </summary>
    public void PrivateFieldAdd(KeyString key, JSValue value)
    {
        PrivateElementAddGuard(in key);
        FastAddValue(key, value, JSPropertyAttributes.ConfigurableValue);
    }

    /// <summary>
    /// PrivateMethodOrAccessorAdd for an instance private method: installs the
    /// shared method function as a read-only per-instance internal slot. Installing
    /// it per instance (rather than once on the prototype) is what gives a
    /// <c>return</c>-override object the brand and makes a second installation throw.
    /// </summary>
    public void PrivateMethodAdd(KeyString key, JSValue method)
    {
        PrivateElementAddGuard(in key);
        FastAddValue(key, method, JSPropertyAttributes.ConfigurableReadonlyValue);
    }

    /// <summary>
    /// PrivateMethodOrAccessorAdd for an instance private accessor: installs the
    /// shared getter and/or setter (either may be null) merged into one element.
    /// </summary>
    public void PrivateAccessorAdd(KeyString key, JSValue getter, JSValue setter)
    {
        PrivateElementAddGuard(in key);
        ref var pr = ref GetOwnProperties();
        pr.Put(key.Key) = new JSProperty(key, getter, setter, JSPropertyAttributes.ConfigurableProperty);
    }

    public override JSValue GetOwnPropertyDescriptor(JSValue name)
    {
        var key = name.ToKey(false);

        switch (key.Type)
        {
            case KeyType.String:
                if (IsPrivateName(in key.KeyString))
                    return JSValue.UndefinedValue;

                if (ownProperties.TryGetValue(key.KeyString.Key, out var p))
                    return JSObjectCoreExtensions.PropertyToJSValue(in p);
                return JSValue.UndefinedValue;

            case KeyType.UInt:
                if (elements.TryGetValue(key.Index, out var p1))
                    return JSObjectCoreExtensions.PropertyToJSValue(in p1);
                return JSValue.UndefinedValue;

            case KeyType.Symbol:
                if (symbols.TryGetValue(key.Symbol.Key, out var p3))
                    return JSObjectCoreExtensions.PropertyToJSValue(in p3);
                return JSValue.UndefinedValue;
        }

        return JSValue.UndefinedValue;
    }

    public override JSValue GetOwnProperty(in KeyString name)
    {
        ref var p = ref ownProperties.GetValue(name.Key);
        return this.GetValue(p);
    }

    public override JSValue GetOwnProperty(IJSSymbol name)
    {
        ref var p = ref symbols.GetRefOrDefault(name.Key, ref JSProperty.Empty);
        return this.GetValue(p);
    }

    public override JSValue GetOwnProperty(uint name)
    {
        ref var p = ref elements.Get(name);
        return this.GetValue(p);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref ElementArray GetElements(bool create = true) => ref elements;
    public ref SAUint32Map<JSProperty> GetSymbols() => ref symbols;

    internal void AllocateElements(uint size)
    {
        size = size > 1024 ? 1024 : size;
        elements.Resize(size);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ref ElementArray CreateElements(uint size = 4) => ref elements;
    [EditorBrowsable(EditorBrowsableState.Never)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void FastAddValue(uint index, JSValue value, JSPropertyAttributes attributes) => elements.Put(index, value, attributes);

    [EditorBrowsable(EditorBrowsableState.Never)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void FastAddProperty(uint index, JSValue getter, JSValue setter, JSPropertyAttributes attributes) => elements.Put(index) = new JSProperty(index, getter, setter, getter, attributes);

    [EditorBrowsable(EditorBrowsableState.Never)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void FastAddValue(KeyString key, JSValue value, JSPropertyAttributes attributes)
    {
        ref var pr = ref GetOwnProperties(true);
        pr.Put(key.Key) = new JSProperty(key.Key, value, attributes);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void FastAddProperty(KeyString key, JSValue getter, JSValue setter, JSPropertyAttributes attributes)
    {
        ref var pr = ref GetOwnProperties(true);
        pr.Put(key.Key) = new JSProperty(key, getter, setter, attributes);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void FastAddValue(IJSSymbol key, JSValue value, JSPropertyAttributes attributes)
    {
        ref var pr = ref GetSymbols();
        pr.Put(key.Key) = new JSProperty(key.Key, value, attributes);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void FastAddProperty(IJSSymbol key, JSValue getter, JSValue setter, JSPropertyAttributes attributes)
    {
        ref var pr = ref GetSymbols();
        pr.Put(key.Key) = new JSProperty(key.Key, getter, setter, getter, attributes);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void FastAddValue(JSValue key, JSValue value, JSPropertyAttributes attributes)
    {
        var k = key.ToKey(true);
        switch (k.Type)
        {
            case KeyType.String:
                FastAddValue(k.KeyString, value, attributes);
                return;

            case KeyType.UInt:
                FastAddValue(k.Index, value, attributes);
                return;

            default:
                FastAddValue(k.Symbol, value, attributes);
                return;
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void FastAddProperty(JSValue key, JSValue getter, JSValue setter, JSPropertyAttributes attributes)
    {
        var k = key.ToKey(true);
        switch (k.Type)
        {
            case KeyType.String:
                FastAddProperty(k.KeyString, getter, setter, attributes);
                return;

            case KeyType.UInt:
                FastAddProperty(k.Index, getter, setter, attributes);
                return;

            default:
                FastAddProperty(k.Symbol, getter, setter, attributes);
                return;
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    // Object spread (`{ ...source }`) and Object.assign perform CopyDataProperties:
    // copy the source's own *enumerable* properties. Ordinary objects use the fast
    // path below (direct slot copy). Exotic objects whose own-key enumeration and
    // property reads must be observable — Proxies above all — override this to true
    // so the copy goes through [[OwnPropertyKeys]] / [[GetOwnProperty]] / [[Get]].
    private protected virtual bool UseObservableSpreadCopy => false;

    public void FastAddRange(JSValue value)
    {
        if (value is not JSObject target)
        {
            // §7.3.25 CopyDataProperties: undefined/null sources contribute nothing.
            // Any other primitive is boxed via ToObject — a String wrapper exposes its
            // characters as own enumerable index properties, so `{ ...'ab' }` and
            // `let { ...rest } = 'ab'` copy { 0:'a', 1:'b' } (other primitive wrappers
            // have no own enumerable properties and copy nothing).
            if (value.IsNullOrUndefined)
                return;

            if (CreatePrimitiveObject(value) is not JSObject boxed)
                return;

            target = boxed;
        }

        if (target.UseObservableSpreadCopy)
        {
            // §7.3.25 CopyDataProperties: iterate [[OwnPropertyKeys]] in order; for
            // each key read its descriptor (firing the getOwnPropertyDescriptor
            // trap) and copy only enumerable properties, reading the value via
            // [[Get]] (firing the get trap).
            var keys = target.GetAllKeys(showEnumerableOnly: false, inherited: false);
            while (keys.MoveNext(out var hasKey, out var key, out _))
            {
                if (!hasKey)
                    continue;

                if (target.GetOwnPropertyDescriptor(key) is not JSObject descriptor
                    || !descriptor[KeyStrings.enumerable].BooleanValue)
                    continue;

                CreateDataProperty(key, target[key]);
            }

            return;
        }

        var en = target.elements.Length;
        for (uint i = 0; i < en; i++)
        {
            if (target.elements.TryGetValue(i, out var p) && !p.IsEmpty)
                elements.Put(i) = p.IsValue
                    ? JSProperty.Property(i, p.value)
                    : JSProperty.Property(i, (IPropertyValue)target.GetValue(p));
        }

        var pe = target.ownProperties.GetEnumerator();
        while (pe.MoveNext(out var key, out var val) && !val.IsEmpty)
            ownProperties.Put(key.Key) = val.IsValue
                ? JSProperty.Property(key, val.value)
                : JSProperty.Property(key, (IPropertyValue)target.GetValue(val));

        foreach (var symbol in target.symbols.All)
        {
            var key = symbol.Key;
            var sv = symbol.Value;

            if (sv.IsEmpty)
                continue;

            symbols.Put(key) = sv.IsValue
                ? JSProperty.Property(key, sv.value)
                : JSProperty.Property(key, (IPropertyValue)target.GetValue(sv));
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public JSObject Merge(JSValue value)
    {
        if (value is not JSObject target)
            return this;

        var pe = new PropertyEnumerator(target, true, false);
        while (pe.MoveNext(out var key, out var val))
            this[key] = val;

        var en = new ElementEnumerator(target);
        while (en.MoveNext(out var hasValue, out var val, out var index))
        {
            if (hasValue)
                this[index] = val;
        }

        return this;
    }
    public override JSValue this[KeyString name]
    {
        get => GetValue(name, this);
        set => SetValue(name, value, null, IsStrictModeEnabled?.Invoke() == true);
    }

    internal protected override bool SetValue(KeyString name, JSValue value, JSValue receiver, bool throwError = true)
    {
        // A private member assignment (`obj.#x = v`) requires the brand: writing a
        // private name to an object whose class did not declare it is a TypeError.
        // Field initialization adds the field directly via FastAddValue and never
        // reaches SetValue, so it is unaffected.
        if (IsPrivateName(in name))
            ThrowIfMissingPrivateMember(in name, reading: false);

        if (name.Key == KeyStrings.__proto__.Key
            && GetInternalProperty(name, false).IsEmpty
            && !GetInternalProperty(name).IsEmpty)
        {
            if (!value.IsObject && !value.IsNull)
                return true;

            (receiver as JSObject ?? this).SetPrototypeOf(value);
            return true;
        }

        var p = GetInternalProperty(name, false);
        if (p.IsProperty)
        {
            if (p.set is IJSFunction setter)
            {
                setter.InvokeFunction(new Arguments(receiver ?? this, value));
                return true;
            }

            if (throwError)
                throw NewTypeError($"Cannot modify property {name} of {this} which has only a getter");

            return false;
        }

        if (p.IsReadOnly)
        {
            if (throwError)
            {
                // Only in Strict Mode ..
                throw NewTypeError($"Cannot modify property {name} of {this}");
            }

            return false;
        }

        if (!p.IsEmpty)
            return SetKeyStringOnReceiver(name, value, receiver, p.Attributes, throwError);

        if (GetPrototypeOf() is JSObject prototypeObject)
            return prototypeObject.SetValue(name, value, receiver ?? this, throwError);

        return SetKeyStringOnReceiver(name, value, receiver, JSPropertyAttributes.EnumerableConfigurableValue, throwError);
    }

    public override JSValue this[uint name]
    {
        get => GetValue(name, this);
        set => SetValue(name, value, this, IsStrictModeEnabled?.Invoke() == true);
    }

    public override bool SetValue(uint name, JSValue value, JSValue receiver, bool throwError = true)
    {
        var p = GetInternalProperty(name, false);
        if (p.IsProperty)
        {
            if (p.set is IJSFunction setter)
            {
                setter.InvokeFunction(new Arguments(receiver ?? this, value));
                return true;
            }

            if (throwError)
                throw NewTypeError($"Cannot modify property {name} of {this} which has only a getter");

            return false;
        }

        if (p.IsReadOnly)
        {
            if (throwError)
                throw NewTypeError($"Cannot modify property {name} of {this}");

            return false;
        }

        if (!p.IsEmpty)
            return SetIndexOnReceiver(name, value, receiver, p.Attributes, throwError);

        if (GetPrototypeOf() is JSObject prototypeObject)
            return prototypeObject.SetValue(name, value, receiver ?? this, throwError);

        return SetIndexOnReceiver(name, value, receiver, JSPropertyAttributes.EnumerableConfigurableValue, throwError);
    }

    public override JSValue this[IJSSymbol name]
    {
        get => GetValue(name, this);
        set => SetValue(name, value, null, IsStrictModeEnabled?.Invoke() == true);
    }

    public void SetPropertyOrThrow(JSValue key, JSValue value)
    {
        var propertyKey = key.ToKey(false);
        switch (propertyKey.Type)
        {
            case KeyType.UInt:
                SetValue(propertyKey.Index, value, this, true);
                return;
            case KeyType.String:
                SetValue(propertyKey.KeyString, value, this, true);
                return;
            case KeyType.Symbol:
                SetValue(propertyKey.Symbol, value, this, true);
                return;
            default:
                throw NewTypeError($"Cannot set property {key}");
        }
    }

    internal protected override bool SetValue(IJSSymbol name, JSValue value, JSValue receiver, bool throwError = true)
    {
        var p = GetInternalProperty(name, false);
        if (p.IsProperty)
        {
            if (p.set is IJSFunction setter)
            {
                setter.InvokeFunction(new Arguments(receiver ?? this, value));
                return true;
            }

            if (throwError)
                throw NewTypeError($"Cannot modify property {name} of {this} which has only a getter");

            return false;
        }

        if (p.IsReadOnly)
        {
            if (throwError)
                throw NewTypeError($"Cannot modify property {name} of {this}");

            return false;
        }

        if (!p.IsEmpty)
            return SetSymbolOnReceiver(name, value, receiver, p.Attributes, throwError);

        if (GetPrototypeOf() is JSObject prototypeObject)
            return prototypeObject.SetValue(name, value, receiver ?? this, throwError);

        return SetSymbolOnReceiver(name, value, receiver, JSPropertyAttributes.EnumerableConfigurableValue, throwError);
    }

    protected bool SetKeyStringOnReceiver(KeyString name, JSValue value, JSValue receiver, JSPropertyAttributes defaultAttributes, bool throwError)
    {
        if (receiver != null && receiver is not JSObject)
        {
            if (throwError)
                throw NewTypeError($"Cannot add property {name} to {receiver}");

            return false;
        }

        var target = receiver as JSObject ?? this;
        if (!ReferenceEquals(target, this))
        {
            var descriptor = target.GetOwnPropertyDescriptor(name.ToJSValue()) as JSObject;
            if (descriptor != null)
            {
                if (TrySetReceiverAccessorProperty(target, descriptor, receiver, value, name, throwError, out var accessorResult))
                    return accessorResult;

                if (IsReceiverReadOnly(descriptor))
                {
                    if (throwError)
                        throw NewTypeError($"Cannot modify property {name} of {target}");

                    return false;
                }

                return DefineReceiverDataProperty(target, name, value, GetReceiverAttributes(descriptor, defaultAttributes), throwError);
            }

            if (!target.IsExtensible())
            {
                if (throwError)
                    throw NewTypeError($"Cannot add property {name} to {target}");

                return false;
            }

            return DefineReceiverDataProperty(target, name, value, defaultAttributes, throwError);
        }

        var p = target.GetInternalProperty(name, false);
        if (p.IsProperty)
        {
            if (p.set is IJSFunction setter)
            {
                setter.InvokeFunction(new Arguments(receiver ?? target, value));
                return true;
            }

            if (throwError)
                throw NewTypeError($"Cannot modify property {name} of {target} which has only a getter");

            return false;
        }

        if (p.IsReadOnly)
        {
            if (throwError)
                throw NewTypeError($"Cannot modify property {name} of {target}");

            return false;
        }

        if (target.IsFrozen())
        {
            if (throwError)
                throw NewTypeError($"Cannot modify property {name} of {target}");

            return false;
        }

        if (p.IsEmpty && !target.IsExtensible())
        {
            if (throwError)
                throw NewTypeError($"Cannot add property {name} to {target}");

            return false;
        }

        return DefineReceiverDataProperty(target, name, value, !p.IsEmpty ? p.Attributes : defaultAttributes, throwError);
    }

    private protected bool SetIndexOnReceiver(uint name, JSValue value, JSValue receiver, JSPropertyAttributes defaultAttributes, bool throwError)
    {
        if (receiver != null && receiver is not JSObject)
        {
            if (throwError)
                throw NewTypeError($"Cannot add property {name} to {receiver}");

            return false;
        }

        var target = receiver as JSObject ?? this;
        if (!ReferenceEquals(target, this))
        {
            var descriptor = target.GetOwnPropertyDescriptor(JSValue.CreateNumber(name)) as JSObject;
            if (descriptor != null)
            {
                if (TrySetReceiverAccessorProperty(target, descriptor, receiver, value, name, throwError, out var accessorResult))
                    return accessorResult;

                if (IsReceiverReadOnly(descriptor))
                {
                    if (throwError)
                        throw NewTypeError($"Cannot modify property {name} of {target}");

                    return false;
                }

                return DefineReceiverDataProperty(target, name, value, GetReceiverAttributes(descriptor, defaultAttributes), throwError);
            }

            if (!target.IsExtensible())
            {
                if (throwError)
                    throw NewTypeError($"Cannot add property {name} to {target}");

                return false;
            }

            return DefineReceiverDataProperty(target, name, value, defaultAttributes, throwError);
        }

        var p = target.GetInternalProperty(name, false);
        if (p.IsProperty)
        {
            if (p.set is IJSFunction setter)
            {
                setter.InvokeFunction(new Arguments(receiver ?? target, value));
                return true;
            }

            if (throwError)
                throw NewTypeError($"Cannot modify property {name} of {target} which has only a getter");

            return false;
        }

        if (p.IsReadOnly)
        {
            if (throwError)
                throw NewTypeError($"Cannot modify property {name} of {target}");

            return false;
        }

        if (target.IsFrozen())
        {
            if (throwError)
                throw NewTypeError($"Cannot modify property {name} of {target}");

            return false;
        }

        if (p.IsEmpty && !target.IsExtensible())
        {
            if (throwError)
                throw NewTypeError($"Cannot add property {name} to {target}");

            return false;
        }

        return DefineReceiverDataProperty(target, name, value, !p.IsEmpty ? p.Attributes : defaultAttributes, throwError);
    }

    private bool SetSymbolOnReceiver(IJSSymbol name, JSValue value, JSValue receiver, JSPropertyAttributes defaultAttributes, bool throwError)
    {
        if (receiver != null && receiver is not JSObject)
        {
            if (throwError)
                throw NewTypeError($"Cannot add property {name} to {receiver}");

            return false;
        }

        var target = receiver as JSObject ?? this;
        if (name.Key == JSValue.SymbolIterator.Key)
            target.HasIterator = true;
        else if (JSValue.SymbolAsyncIterator != null && name.Key == JSValue.SymbolAsyncIterator.Key)
            target.HasAsyncIterator = true;

        if (!ReferenceEquals(target, this))
        {
            var symbolValue = (JSValue)(JSValue.GetSymbolByKeyFactory?.Invoke(name.Key)
                ?? throw new InvalidOperationException($"Unknown symbol key {name.Key}"));
            var descriptor = target.GetOwnPropertyDescriptor(symbolValue) as JSObject;
            if (descriptor != null)
            {
                if (TrySetReceiverAccessorProperty(target, descriptor, receiver, value, name, throwError, out var accessorResult))
                    return accessorResult;

                if (IsReceiverReadOnly(descriptor))
                {
                    if (throwError)
                        throw NewTypeError($"Cannot modify property {name} of {target}");

                    return false;
                }

                return DefineReceiverDataProperty(target, name, value, GetReceiverAttributes(descriptor, defaultAttributes), throwError);
            }

            if (!target.IsExtensible())
            {
                if (throwError)
                    throw NewTypeError($"Cannot add property {name} to {target}");

                return false;
            }

            return DefineReceiverDataProperty(target, name, value, defaultAttributes, throwError);
        }

        var p = target.GetInternalProperty(name, false);
        if (p.IsProperty)
        {
            if (p.set is IJSFunction setter)
            {
                setter.InvokeFunction(new Arguments(receiver ?? target, value));
                return true;
            }

            if (throwError)
                throw NewTypeError($"Cannot modify property {name} of {target} which has only a getter");

            return false;
        }

        if (p.IsReadOnly)
        {
            if (throwError)
                throw NewTypeError($"Cannot modify property {name} of {target}");

            return false;
        }

        if (target.IsFrozen())
        {
            if (throwError)
                throw NewTypeError($"Cannot modify property {name} of {target}");

            return false;
        }

        if (p.IsEmpty && !target.IsExtensible())
        {
            if (throwError)
                throw NewTypeError($"Cannot add property {name} to {target}");

            return false;
        }

        return DefineReceiverDataProperty(target, name, value, !p.IsEmpty ? p.Attributes : defaultAttributes, throwError);
    }

    private static bool TrySetReceiverAccessorProperty(JSObject target, JSObject descriptor, JSValue receiver, JSValue value, object name, bool throwError, out bool result)
    {
        var hasGet = !descriptor.GetInternalProperty(KeyStrings.get, false).IsEmpty;
        var hasSet = !descriptor.GetInternalProperty(KeyStrings.set, false).IsEmpty;
        if (!hasGet && !hasSet)
        {
            result = false;
            return false;
        }

        // This branch runs only when the base (the object whose prototype chain was
        // walked) resolved the property to a DATA descriptor — i.e. we are applying the
        // write to a DISTINCT receiver (super.x = / Reflect.set with a 4th argument).
        // OrdinarySetWithOwnDescriptor step: if the receiver's own property is an
        // accessor, return false. The receiver's setter is NOT invoked here — a setter
        // is only honoured when the accessor is found while walking the base's prototype
        // chain (handled by the IsProperty branches before reaching this receiver path).
        if (throwError)
            throw NewTypeError($"Cannot assign to property {name} of {target} whose receiver has an accessor");

        result = false;
        return true;
    }

    private static bool IsReceiverReadOnly(JSObject descriptor)
        => !descriptor.GetInternalProperty(KeyStrings.writable, false).IsEmpty
            && !descriptor[KeyStrings.writable].BooleanValue;

    private static JSPropertyAttributes GetReceiverAttributes(JSObject descriptor, JSPropertyAttributes defaultAttributes)
    {
        var attributes = JSPropertyAttributes.Value;
        if (IsReceiverReadOnly(descriptor))
            attributes |= JSPropertyAttributes.Readonly;

        if (!descriptor.GetInternalProperty(KeyStrings.enumerable, false).IsEmpty
            ? descriptor[KeyStrings.enumerable].BooleanValue
            : defaultAttributes.HasFlag(JSPropertyAttributes.Enumerable))
        {
            attributes |= JSPropertyAttributes.Enumerable;
        }

        if (!descriptor.GetInternalProperty(KeyStrings.configurable, false).IsEmpty
            ? descriptor[KeyStrings.configurable].BooleanValue
            : defaultAttributes.HasFlag(JSPropertyAttributes.Configurable))
        {
            attributes |= JSPropertyAttributes.Configurable;
        }

        return attributes;
    }

    private bool DefineReceiverDataProperty(JSObject target, KeyString name, JSValue value, JSPropertyAttributes attributes, bool throwError)
    {
        if (ReferenceEquals(target, this))
        {
            ref var own = ref target.GetOwnProperties();
            own.Put(name, value, attributes);
            target.PropertyChanged?.Invoke(target, (name.Key, uint.MaxValue, null));
            return true;
        }

        var descriptor = CreateDataDescriptor(value, attributes);
        var result = target.DefineProperty(name, descriptor);
        if (!result.IsBoolean || result.BooleanValue)
            return true;

        if (throwError)
            throw NewTypeError($"Cannot modify property {name} of {target}");

        return false;
    }

    private bool DefineReceiverDataProperty(JSObject target, uint name, JSValue value, JSPropertyAttributes attributes, bool throwError)
    {
        if (ReferenceEquals(target, this))
        {
            ref var elements = ref target.CreateElements();
            elements.Put(name, value, attributes);
            target.PropertyChanged?.Invoke(target, (uint.MaxValue, name, null));
            return true;
        }

        var descriptor = CreateDataDescriptor(value, attributes);
        var result = target.DefineProperty(name, descriptor);
        if (!result.IsBoolean || result.BooleanValue)
            return true;

        if (throwError)
            throw NewTypeError($"Cannot modify property {name} of {target}");

        return false;
    }

    private bool DefineReceiverDataProperty(JSObject target, IJSSymbol name, JSValue value, JSPropertyAttributes attributes, bool throwError)
    {
        if (ReferenceEquals(target, this))
        {
            target.symbols.Put(name.Key) = new JSProperty(name.Key, value, attributes);
            target.PropertyChanged?.Invoke(target, (uint.MaxValue, uint.MaxValue, name));
            return true;
        }

        var descriptor = CreateDataDescriptor(value, attributes);
        var result = target.DefineProperty(name, descriptor);
        if (!result.IsBoolean || result.BooleanValue)
            return true;

        if (throwError)
            throw NewTypeError($"Cannot modify property {name} of {target}");

        return false;
    }

    // CreateDataPropertyOrThrow(this, key, value) for a public class field
    // initializer. An ordinary object stores the own data property directly; an
    // exotic object (e.g. a Proxy handed back by a `return`-override base
    // constructor) overrides these to route through [[DefineOwnProperty]], so its
    // defineProperty trap observes the field initialization.
    public virtual void CreateDataProperty(KeyString key, JSValue value)
        => FastAddValue(key, value, JSPropertyAttributes.EnumerableConfigurableValue);

    public virtual void CreateDataProperty(uint index, JSValue value)
        => FastAddValue(index, value, JSPropertyAttributes.EnumerableConfigurableValue);

    public virtual void CreateDataProperty(JSValue key, JSValue value)
        => FastAddValue(key, value, JSPropertyAttributes.EnumerableConfigurableValue);

    internal static JSObject CreateDataDescriptor(JSValue value, JSPropertyAttributes attributes)
    {
        var descriptor = new JSObject();
        descriptor.FastAddValue(KeyStrings.value, value, JSPropertyAttributes.EnumerableConfigurableValue);
        descriptor.FastAddValue(KeyStrings.writable, attributes.HasFlag(JSPropertyAttributes.Readonly) ? JSValue.BooleanFalse : JSValue.BooleanTrue, JSPropertyAttributes.EnumerableConfigurableValue);
        descriptor.FastAddValue(KeyStrings.enumerable, attributes.HasFlag(JSPropertyAttributes.Enumerable) ? JSValue.BooleanTrue : JSValue.BooleanFalse, JSPropertyAttributes.EnumerableConfigurableValue);
        descriptor.FastAddValue(KeyStrings.configurable, attributes.HasFlag(JSPropertyAttributes.Configurable) ? JSValue.BooleanTrue : JSValue.BooleanFalse, JSPropertyAttributes.EnumerableConfigurableValue);
        return descriptor;
    }

    internal protected override JSValue GetValue(IJSSymbol key, JSValue receiver, bool throwError = true)
    {
        ref var p = ref symbols.GetRefOrDefault(key.Key, ref JSProperty.Empty);
        if (!p.IsEmpty)
            return (receiver ?? this).GetValue(p);

        return base.GetValue(key, receiver, throwError);
    }

    internal protected override JSValue GetValue(KeyString key, JSValue receiver, bool throwError = true)
    {
        // A private member read on an object whose class did not declare the private
        // name is a TypeError (brand check), not an `undefined` result. Throwing here
        // — before the ordinary own/prototype lookup — also covers private method
        // gets (InvokeMethod resolves the method through GetValue).
        if (IsPrivateName(in key))
            ThrowIfMissingPrivateMember(in key, reading: true);

        ref var p = ref ownProperties.GetValue(key.Key);
        if (!p.IsEmpty)
        {
            // A private accessor declared with only a setter has no [[Get]]: reading
            // it is a TypeError (PrivateGet, sec-privateget), not the `undefined` an
            // ordinary getterless accessor yields. Public accessors keep the undefined
            // result; this stricter behaviour is gated on the private-name marker.
            if (IsPrivateName(in key) && p.IsProperty && p.get is not IJSFunction)
                throw NewTypeError($"Cannot read private member {PrivateDisplayName(in key)}: it was defined without a getter");

            return (receiver ?? this).GetValue(p);
        }

        // A canonical array-index string key (e.g. "1") names the same property as
        // the integer index, which is stored in the element table. Canonicalize
        // directly from the key's text: routing through KeyStringToJSValue().ToKey()
        // would short-circuit, because that JSString carries a preset KeyString and
        // ToKey() returns it without ever testing for an array index.
        if (NumberParser.TryGetArrayIndex(key.Value, out var index))
            return GetValue(index, receiver, throwError);

        return base.GetValue(key, receiver, throwError);
    }

    public override JSValue GetValue(uint key, JSValue receiver, bool throwError = true)
    {
        ref var p = ref elements.Get(key);
        if (!p.IsEmpty)
        {
            if (p.IsValue)
                return (JSValue)p.value;

            if (p.get is IJSFunction getter)
                return getter.InvokeFunction(new Arguments(receiver ?? this));

            return JSValue.UndefinedValue;
        }

        return base.GetValue(key, receiver, throwError);
    }

    public virtual JSValue DefineProperty(JSValue key, JSObject propertyDescription)
    {
        var k = key.ToKey();
        return k.Type switch
        {
            KeyType.Empty => JSValue.BooleanFalse,
            KeyType.UInt => DefineProperty(k.Index, propertyDescription),
            KeyType.String => DefineProperty(k.KeyString, propertyDescription),
            KeyType.Symbol => DefineProperty(k.Symbol, propertyDescription),
            _ => JSValue.BooleanFalse,
        };
    }

    public virtual JSValue DefineProperty(IJSSymbol name, JSObject pd)
    {
        var key = name.Key;
        var old = symbols[key];
        if (old.IsEmpty && !IsExtensible())
            return JSValue.BooleanFalse;
        if (!old.IsEmpty)
        {
            CompletePropertyDescriptor(pd, in old);
            if (!IsCompatiblePropertyRedefinition(in old, pd))
                return JSValue.BooleanFalse;
        }

        symbols.Put(key) = pd.ToProperty(key);
        PropertyChanged?.Invoke(this, (uint.MaxValue, uint.MaxValue, name));
        return JSValue.UndefinedValue;
    }

    public virtual JSValue DefineProperty(uint key, JSObject pd)
    {
        ref var elements = ref GetElements(true);
        var old = elements[key];
        if (old.IsEmpty && !IsExtensible())
            return JSValue.BooleanFalse;
        if (!old.IsEmpty)
        {
            CompletePropertyDescriptor(pd, in old);
            if (!IsCompatiblePropertyRedefinition(in old, pd))
                return JSValue.BooleanFalse;
        }

        elements.Put(key) = pd.ToProperty(key);
        this.UpdateArrayLengthIfNeeded(key);

        PropertyChanged?.Invoke(this, (uint.MaxValue, key, null));
        return JSValue.UndefinedValue;
    }

    public virtual JSValue DefineProperty(in KeyString name, JSObject pd)
    {
        var key = name.Key;
        ref var ownProperties = ref GetOwnProperties();
        ref var old = ref ownProperties.GetValue(name.Key);
        if (old.IsEmpty && !IsExtensible())
            return JSValue.BooleanFalse;

        if (!old.IsEmpty)
        {
            if (name.Key == KeyStrings.length.Key
                && old.IsValue
                && pd.GetInternalProperty(KeyStrings.value, false).IsEmpty
                && pd.GetInternalProperty(KeyStrings.get, false).IsEmpty
                && pd.GetInternalProperty(KeyStrings.set, false).IsEmpty)
            {
                var currentLength = Length;
                if (currentLength >= 0)
                    pd.FastAddValue(KeyStrings.value, JSValue.CreateNumber(currentLength), JSPropertyAttributes.EnumerableConfigurableValue);
            }

            CompletePropertyDescriptor(pd, in old);
            if (!IsCompatiblePropertyRedefinition(in old, pd))
                return JSValue.BooleanFalse;
        }
        // p.key = name;
        ownProperties.Put(key) = pd.ToProperty(key);
        PropertyChanged?.Invoke(this, (name.Key, uint.MaxValue, null));
        return JSValue.UndefinedValue;
    }

    private static void CompletePropertyDescriptor(JSObject descriptor, in JSProperty current)
    {
        var hasConfigurable = !descriptor.GetInternalProperty(KeyStrings.configurable, false).IsEmpty;
        var hasEnumerable = !descriptor.GetInternalProperty(KeyStrings.enumerable, false).IsEmpty;
        var hasGet = !descriptor.GetInternalProperty(KeyStrings.get, false).IsEmpty;
        var hasSet = !descriptor.GetInternalProperty(KeyStrings.set, false).IsEmpty;
        var hasValue = !descriptor.GetInternalProperty(KeyStrings.value, false).IsEmpty;
        var hasWritable = !descriptor.GetInternalProperty(KeyStrings.writable, false).IsEmpty;
        var descriptorIsAccessor = hasGet || hasSet;
        var descriptorIsData = hasValue || hasWritable;

        if (!hasConfigurable)
            descriptor.FastAddValue(KeyStrings.configurable, current.IsConfigurable ? JSValue.BooleanTrue : JSValue.BooleanFalse, JSPropertyAttributes.EnumerableConfigurableValue);

        if (!hasEnumerable)
            descriptor.FastAddValue(KeyStrings.enumerable, current.IsEnumerable ? JSValue.BooleanTrue : JSValue.BooleanFalse, JSPropertyAttributes.EnumerableConfigurableValue);

        if (current.IsProperty)
        {
            if (!descriptorIsData && !hasGet)
                descriptor[KeyStrings.get] = current.get as JSValue ?? JSValue.UndefinedValue;

            if (!descriptorIsData && !hasSet)
                descriptor[KeyStrings.set] = current.set as JSValue ?? JSValue.UndefinedValue;

            return;
        }

        if (!descriptorIsAccessor && !hasValue)
            descriptor.FastAddValue(KeyStrings.value, current.value as JSValue ?? JSValue.UndefinedValue, JSPropertyAttributes.EnumerableConfigurableValue);

        if (!descriptorIsAccessor && !hasWritable)
            descriptor.FastAddValue(KeyStrings.writable, current.IsReadOnly ? JSValue.BooleanFalse : JSValue.BooleanTrue, JSPropertyAttributes.EnumerableConfigurableValue);
    }

    private static bool IsCompatiblePropertyRedefinition(in JSProperty current, JSObject descriptor)
    {
        if (current.IsConfigurable)
            return true;

        if (descriptor[KeyStrings.configurable].BooleanValue)
            return false;

        if (descriptor[KeyStrings.enumerable].BooleanValue != current.IsEnumerable)
            return false;

        var descriptorHasGet = !descriptor.GetInternalProperty(KeyStrings.get, false).IsEmpty;
        var descriptorHasSet = !descriptor.GetInternalProperty(KeyStrings.set, false).IsEmpty;
        var descriptorIsAccessor = descriptorHasGet || descriptorHasSet;
        if (descriptorIsAccessor != current.IsProperty)
            return false;

        if (current.IsProperty)
        {
            if (!descriptor[KeyStrings.get].StrictEquals(current.get as JSValue ?? JSUndefined.Value))
                return false;

            if (!descriptor[KeyStrings.set].StrictEquals(current.set as JSValue ?? JSUndefined.Value))
                return false;

            return true;
        }

        var descriptorWritable = descriptor[KeyStrings.writable].BooleanValue;
        if (current.IsReadOnly && descriptorWritable)
            return false;

        if (current.IsReadOnly
            && !descriptor[KeyStrings.value].Is(current.value as JSValue ?? JSUndefined.Value).BooleanValue)
        {
            return false;
        }

        return true;
    }

    public override IElementEnumerator GetAllKeys(bool showEnumerableOnly = true, bool inherited = true) => new KeyEnumerator(this, showEnumerableOnly, inherited);//var elements = this.elements;//if (elements != null)//{//    foreach (var (Key, Value) in elements.AllValues)//    {//        if (showEnumerableOnly)//        {//            if (!Value.IsEnumerable)//                continue;//        }//        yield return new JSNumber(Key);//    }//}//var ownProperties = this.ownProperties;//if (ownProperties != null)//{//    var en = new PropertySequence.Enumerator(ownProperties);//    while(en.MoveNext())//    {//        var p = en.Current;//        if (showEnumerableOnly)//        {//            if (!p.IsEnumerable)//                continue;//        }//        yield return p.ToJSValue();//    }//}//if (inherited)//{//    var @base = this.prototypeChain;//    if (@base != this && @base != null)//    {//        foreach (var i in @base.GetAllKeys(showEnumerableOnly))//            yield return i;//    }//}

    /// <summary>
    /// Implements ToPropertyDescriptor (ECMA-262 § 6.2.6.5): reads the well-known
    /// descriptor fields from <paramref name="userDescriptor"/> using [[HasProperty]]
    /// and [[Get]] — both of which consult the prototype chain — producing a fresh
    /// own-data-property record. Descriptor fields may therefore be inherited from
    /// the descriptor object's prototype or be supplied via accessors on it.
    /// </summary>
    internal static JSObject NormalizeDescriptor(JSObject userDescriptor)
    {
        var record = new JSObject();
        CopyDescriptorField(userDescriptor, record, KeyStrings.enumerable);
        CopyDescriptorField(userDescriptor, record, KeyStrings.configurable);
        CopyDescriptorField(userDescriptor, record, KeyStrings.value);
        CopyDescriptorField(userDescriptor, record, KeyStrings.writable);
        CopyDescriptorField(userDescriptor, record, KeyStrings.get);
        CopyDescriptorField(userDescriptor, record, KeyStrings.set);
        return record;
    }

    private static void CopyDescriptorField(JSObject source, JSObject record, in KeyString field)
    {
        // [[HasProperty]] (prototype chain) decides presence; [[Get]] reads the value.
        if (source.GetInternalProperty(field).IsEmpty)
            return;

        record.FastAddValue(field, source[field], JSPropertyAttributes.EnumerableConfigurableValue);
    }

    internal JSProperty ToProperty(uint key)
    {
        // Accessor-ness is decided by the *presence* of get/set fields, not their
        // values: { get: undefined } / { set: undefined } describe an accessor
        // property (with the respective accessor absent), not a data property.
        var hasGet = !GetInternalProperty(KeyStrings.get, false).IsEmpty;
        var hasSet = !GetInternalProperty(KeyStrings.set, false).IsEmpty;
        var hasValue = !GetInternalProperty(KeyStrings.value, false).IsEmpty;
        var hasWritable = !GetInternalProperty(KeyStrings.writable, false).IsEmpty;
        var isAccessor = hasGet || hasSet;

        if (isAccessor && (hasValue || hasWritable))
            throw NewTypeError("Invalid property.  Cannot both specify accessors and a value or writable attribute");

        var pt = JSPropertyAttributes.Empty;

        if (this[KeyStrings.configurable].BooleanValue)
            pt |= JSPropertyAttributes.Configurable;

        if (this[KeyStrings.enumerable].BooleanValue)
            pt |= JSPropertyAttributes.Enumerable;

        if (isAccessor)
        {
            JSValue pget = null;
            JSValue pset = null;

            if (hasGet)
            {
                var get = this[KeyStrings.get];
                if (!get.IsUndefined)
                {
                    if (get is not IJSFunction)
                        throw NewTypeError("Getter must be a function");

                    pget = get;
                }
            }

            if (hasSet)
            {
                var set = this[KeyStrings.set];
                if (!set.IsUndefined)
                {
                    if (set is not IJSFunction)
                        throw NewTypeError("Setter must be a function");

                    pset = set;
                }
            }

            pt |= JSPropertyAttributes.Property;
            return new JSProperty(key, pget, pset, null, pt);
        }

        if (!this[KeyStrings.writable].BooleanValue)
            pt |= JSPropertyAttributes.Readonly;

        pt |= JSPropertyAttributes.Value;
        return new JSProperty(key, null, null, this[KeyStrings.value], pt);
    }

    public override JSValue Delete(in KeyString key)
    {
        var property = ownProperties.GetValue(key.Key);
        if (!property.IsEmpty && !property.IsConfigurable)
            return JSValue.BooleanFalse;

        if (ownProperties.RemoveAt(key.Key))
        {
            PropertyChanged?.Invoke(this, (key.Key, uint.MaxValue, null));
            return JSValue.BooleanTrue;
        }

        return JSValue.BooleanTrue;
    }

    public override JSValue Delete(uint key)
    {
        if (elements.TryGetValue(key, out var property) && !property.IsConfigurable)
            return JSValue.BooleanFalse;

        ref var element = ref elements.Get(key);

        if (elements.RemoveAt(key))
        {
            PropertyChanged?.Invoke(this, (uint.MaxValue, key, null));
            return JSValue.BooleanTrue;
        }

        return JSValue.BooleanTrue;
    }

    public override JSValue Delete(IJSSymbol symbol)
    {
        if (symbols.TryGetValue(symbol.Key, out var property) && !property.IsConfigurable)
            return JSValue.BooleanFalse;

        if (symbols.RemoveAt(symbol.Key))
        {
            PropertyChanged?.Invoke(this, (uint.MaxValue, uint.MaxValue, symbol));
            return JSValue.BooleanTrue;
        }

        return JSValue.BooleanTrue;
    }
    internal override bool TryGetValue(uint i, out JSProperty value) => elements.TryGetValue(i, out value);

    internal override bool TryGetElement(uint i, out JSValue value)
    {
        if (elements.TryGetValue(i, out var p))
        {
            value = this.GetValue(p);
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>
    /// Moves elements from `start` to `to`.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="count"></param>
    /// <param name="to"></param>
    internal override void MoveElements(int start, int to)
    {
        ref var elements = ref CreateElements();

        var end = Length - 1;
        var diff = to - start;
        if (start > to)
        {

            for (uint i = (uint)start, j = (uint)to; i <= end; i++, j++)
            {
                if (TryRemove(i, out var p))
                    elements.Put(j) = p;
            }

            Length += diff;
            return;
        }
        else
        {
            for (int i = end, j = Length + diff - 1; i >= start; i--, j--)
            {
                if (TryRemove((uint)i, out var p))
                    elements.Put((uint)j) = p;
            }

            Length += diff;
        }

        PropertyChanged?.Invoke(this, (uint.MaxValue, uint.MaxValue, null));
    }

    /// <summary>
    /// Used in pop
    /// </summary>
    /// <param name="i"></param>
    /// <param name="p"></param>
    /// <returns></returns>
    internal override bool TryRemove(uint i, out JSProperty p)
    {
        if (elements.TryRemove(i, out p))
        {
            PropertyChanged?.Invoke(this, (uint.MaxValue, i, null));
            return true;
        }

        if (prototypeChain != null)
            return ((IJSPrototype)prototypeChain).TryRemove(i, out p);

        return false;
    }
    public override IElementEnumerator GetElementEnumerator()
    {
        if (HasIterator)
        {
            var v = this.GetValue(symbols[JSValue.SymbolIterator.Key]);
            if (!v.IsFunction)
                throw NewTypeError("@@iterator is not a function");

            var iterator = v.InvokeFunction(new Arguments(this));
            if (!iterator.IsObject)
                throw NewTypeError("@@iterator result is not an object");

            return new JSIterator(iterator);
        }

        return new ElementEnumerator(this);
    }

    // Enumerates the object's own integer-indexed elements for key enumeration
    // (Object.keys / for-in / etc.), which must never invoke the iterator protocol.
    // When the object carries its *own* @@iterator the iterator-aware
    // GetElementEnumerator would (correctly, for for-of) honour it — and throw if it
    // is non-callable (e.g. `o[Symbol.iterator] = 'x'`) — so bypass it with the raw
    // element walk. Otherwise delegate to GetElementEnumerator so exotic objects
    // (arrays, typed arrays) keep their specialised, hole-aware element enumeration.
    internal IElementEnumerator GetOwnIndexedElementEnumerator(bool enumerableOnly = false)
    {
        // Objects whose indexed data lives in the ordinary `elements` map (object
        // literals, class prototypes, …) are walked directly so a non-enumerable
        // indexed property (e.g. a computed-number class method `[1]() {}`) is
        // skipped during key enumeration (Object.keys / for-in). A subclass that
        // specialises element iteration (array, typed array, string, …) overrides
        // GetElementEnumerator; honour that specialised, hole-aware walk for those
        // (their indexed elements are always enumerable). An object carrying its
        // own @@iterator takes the raw slot walk so the user iterator is not run.
        if (!HasIterator)
        {
            var specialized = GetElementEnumerator();
            if (specialized is not ElementEnumerator)
                return specialized;
        }

        return new ElementEnumerator(this, enumerableOnly);
    }

    public override IElementEnumerator GetIterableEnumerator()
    {
        var iterator = this[JSValue.SymbolIterator];
        if (iterator.IsNullOrUndefined)
            throw NewTypeError(NotIterable(this));

        if (!iterator.IsFunction)
            throw NewTypeError("@@iterator is not a function");

        var iteratorResult = iterator.InvokeFunction(new Arguments(this));
        if (!iteratorResult.IsObject)
            throw NewTypeError("@@iterator result is not an object");

        return new JSIterator(iteratorResult);
    }

    public override IElementEnumerator GetAsyncElementEnumerator()
    {
        if (JSValue.SymbolAsyncIterator != null
            && (HasAsyncIterator || symbols.TryGetValue(JSValue.SymbolAsyncIterator.Key, out _)))
        {
            var v = this.GetValue(symbols[JSValue.SymbolAsyncIterator.Key]);
            if (!v.IsFunction)
                throw NewTypeError("@@asyncIterator is not a function");

            var iterator = v.InvokeFunction(new Arguments(this));
            if (!iterator.IsObject)
                throw NewTypeError("@@asyncIterator result is not an object");

            return new JSIterator(iterator, awaitResult: true);
        }

        return GetElementEnumerator();
    }

    public override IElementEnumerator GetAsyncIterableEnumerator()
    {
        if (JSValue.SymbolAsyncIterator != null)
        {
            var asyncIterator = this[JSValue.SymbolAsyncIterator];
            if (!asyncIterator.IsNullOrUndefined)
            {
                if (!asyncIterator.IsFunction)
                    throw NewTypeError("@@asyncIterator is not a function");

                var iterator = asyncIterator.InvokeFunction(new Arguments(this));
                if (!iterator.IsObject)
                    throw NewTypeError("@@asyncIterator result is not an object");

                return new JSIterator(iterator, awaitResult: true);
            }
        }

        return GetIterableEnumerator();
    }

    private readonly struct ElementEnumerator(JSObject @object, bool enumerableOnly = false) : IElementEnumerator
    {
        readonly IEnumerator<(uint Key, JSProperty Value)> en = @object.elements.AllValues().GetEnumerator();
        readonly bool enumerableOnly = enumerableOnly;

        // Advance to the next stored element, skipping non-enumerable ones when key
        // enumeration requested enumerable-only (Object.keys / for-in).
        private bool MoveNextSlot(out uint key, out JSProperty prop)
        {
            while (en?.MoveNext() ?? false)
            {
                (key, prop) = en.Current;
                if (!enumerableOnly || prop.IsEnumerable)
                    return true;
            }

            key = 0;
            prop = default;
            return false;
        }

        public bool MoveNext(out bool hasValue, out JSValue value, out uint index)
        {
            if (MoveNextSlot(out var key, out var prop))
            {
                value = @object.GetValue(prop);
                index = key;
                hasValue = true;
                return true;
            }

            hasValue = false;
            value = JSValue.UndefinedValue;
            index = 0;
            return false;
        }

        public bool MoveNext(out JSValue value)
        {
            if (MoveNextSlot(out var _, out var prop))
            {
                value = @object.GetValue(prop);
                return true;
            }

            value = JSValue.UndefinedValue;
            return false;
        }

        public bool MoveNextOrDefault(out JSValue value, JSValue @default)
        {
            if (MoveNextSlot(out var _, out var prop))
            {
                value = @object.GetValue(prop);
                return true;
            }

            value = @default;
            return false;
        }

        public JSValue NextOrDefault(JSValue @default)
        {
            if (MoveNextSlot(out var _, out var prop))
                return @object.GetValue(prop);

            return @default;
        }
    }
}
