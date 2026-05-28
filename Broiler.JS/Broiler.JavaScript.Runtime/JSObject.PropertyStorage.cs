using Broiler.JavaScript.Storage;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
namespace Broiler.JavaScript.Runtime;

public partial class JSObject
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref PropertySequence GetOwnProperties(bool create = true) => ref ownProperties;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsPrivateName(in KeyString key)
    {
        var value = key.Value.Value;
        return !string.IsNullOrEmpty(value) && value[0] == '#';
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
    public void FastAddRange(JSValue value)
    {
        if (value is not JSObject target)
            return;

        var pe = target.ownProperties.GetEnumerator();
        while (pe.MoveNext(out var key, out var val) && !val.IsEmpty)
            ownProperties.Put(key.Key) = val.IsValue ? JSProperty.Property(val.value) : JSProperty.Property(target.GetValue(val));

        var en = target.elements.Length;
        for (uint i = 0; i < en; i++)
        {
            if (target.elements.TryGetValue(i, out var p) && !p.IsEmpty)
                elements.Put(i) = p.IsValue ? JSProperty.Property(p.value) : JSProperty.Property(target.GetValue(p));
        }

        foreach (var symbol in target.symbols.All)
        {
            var key = symbol.Key;
            var sv = symbol.Value;

            if (sv.IsEmpty)
                continue;

            symbols.Put(key) = sv.IsValue ? JSProperty.Property(sv.value) : JSProperty.Property(target.GetValue(sv));
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

    private bool SetKeyStringOnReceiver(KeyString name, JSValue value, JSValue receiver, JSPropertyAttributes defaultAttributes, bool throwError)
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

    private bool SetIndexOnReceiver(uint name, JSValue value, JSValue receiver, JSPropertyAttributes defaultAttributes, bool throwError)
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

        if (hasSet && descriptor[KeyStrings.set] is IJSFunction setter)
        {
            setter.InvokeFunction(new Arguments(receiver ?? target, value));
            result = true;
            return true;
        }

        if (throwError)
            throw NewTypeError($"Cannot modify property {name} of {target} which has only a getter");

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

    private static JSObject CreateDataDescriptor(JSValue value, JSPropertyAttributes attributes)
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
        ref var p = ref ownProperties.GetValue(key.Key);
        if (!p.IsEmpty)
            return (receiver ?? this).GetValue(p);

        var propertyKey = JSObjectCoreExtensions.KeyStringToJSValue(key).ToKey(false);
        if (propertyKey.Type == KeyType.UInt)
            return GetValue(propertyKey.Index, receiver, throwError);

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
                throw NewTypeError("Cannot redefine property");
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
                throw NewTypeError("Cannot redefine property");
        }

        elements.Put(key) = pd.ToProperty(key);
        this.UpdateArrayLengthIfNeeded(key);

        PropertyChanged?.Invoke(this, (uint.MaxValue, key, null));
        return JSValue.UndefinedValue;
    }

    public virtual JSValue DefineProperty(in KeyString name, JSObject pd)
    {
        if (name.Key == KeyStrings.length.Key && pd.GetInternalProperty(KeyStrings.value, false).IsEmpty)
        {
            var currentLength = Length;
            if (currentLength >= 0)
                pd.FastAddValue(KeyStrings.value, JSValue.CreateNumber(currentLength), JSPropertyAttributes.EnumerableConfigurableValue);
        }

        var key = name.Key;
        ref var ownProperties = ref GetOwnProperties();
        ref var old = ref ownProperties.GetValue(name.Key);
        if (old.IsEmpty && !IsExtensible())
            return JSValue.BooleanFalse;

        if (!old.IsEmpty)
        {
            CompletePropertyDescriptor(pd, in old);
            if (!IsCompatiblePropertyRedefinition(in old, pd))
                throw NewTypeError("Cannot redefine property");
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
            && !descriptor[KeyStrings.value].StrictEquals(current.value as JSValue ?? JSUndefined.Value))
        {
            return false;
        }

        return true;
    }

    public override IElementEnumerator GetAllKeys(bool showEnumerableOnly = true, bool inherited = true) => new KeyEnumerator(this, showEnumerableOnly, inherited);//var elements = this.elements;//if (elements != null)//{//    foreach (var (Key, Value) in elements.AllValues)//    {//        if (showEnumerableOnly)//        {//            if (!Value.IsEnumerable)//                continue;//        }//        yield return new JSNumber(Key);//    }//}//var ownProperties = this.ownProperties;//if (ownProperties != null)//{//    var en = new PropertySequence.Enumerator(ownProperties);//    while(en.MoveNext())//    {//        var p = en.Current;//        if (showEnumerableOnly)//        {//            if (!p.IsEnumerable)//                continue;//        }//        yield return p.ToJSValue();//    }//}//if (inherited)//{//    var @base = this.prototypeChain;//    if (@base != this && @base != null)//    {//        foreach (var i in @base.GetAllKeys(showEnumerableOnly))//            yield return i;//    }//}

    internal JSProperty ToProperty(uint key)
    {
        JSValue pget = null;
        JSValue pset = null;
        JSValue pvalue = null;
        var value = this[KeyStrings.value];
        var get = this[KeyStrings.get];
        var set = this[KeyStrings.set];
        var hasValue = !GetInternalProperty(KeyStrings.value, false).IsEmpty;
        var hasWritable = !GetInternalProperty(KeyStrings.writable, false).IsEmpty;
        var pt = JSPropertyAttributes.Empty;

        if (this[KeyStrings.configurable].BooleanValue)
            pt |= JSPropertyAttributes.Configurable;

        if (this[KeyStrings.enumerable].BooleanValue)
            pt |= JSPropertyAttributes.Enumerable;

        if (!this[KeyStrings.writable].BooleanValue)
            pt |= JSPropertyAttributes.Readonly;

        if (!get.IsUndefined)
        {
            if (get is not IJSFunction)
                throw NewTypeError("Getter must be a function");

            pt |= JSPropertyAttributes.Property;
            pget = get;
        }

        if (!set.IsUndefined)
        {
            if (set is not IJSFunction)
                throw NewTypeError("Setter must be a function");

            pt |= JSPropertyAttributes.Property;
            pset = set;
        }

        if ((pget != null || pset != null) && (hasValue || hasWritable))
            throw NewTypeError("Invalid property.  Cannot both specify accessors and a value or writable attribute");

        if (pget == null && pset == null)
        {
            pt |= JSPropertyAttributes.Value;
            pvalue = value;
        }

        var pAttributes = pt;
        return new JSProperty(key, pget, pset, pvalue, pAttributes);
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
            return new JSIterator(v.InvokeFunction(new Arguments(this)));
        }

        return new ElementEnumerator(this);
    }

    public override IElementEnumerator GetIterableEnumerator()
    {
        var iterator = this[JSValue.SymbolIterator];
        if (iterator.IsUndefined)
            throw NewTypeError(NotIterable(this));

        return new JSIterator(iterator.InvokeFunction(new Arguments(this)));
    }

    public override IElementEnumerator GetAsyncElementEnumerator()
    {
        if (JSValue.SymbolAsyncIterator != null
            && (HasAsyncIterator || symbols.TryGetValue(JSValue.SymbolAsyncIterator.Key, out _)))
        {
            var v = this.GetValue(symbols[JSValue.SymbolAsyncIterator.Key]);
            return new JSIterator(v.InvokeFunction(new Arguments(this)), awaitResult: true);
        }

        return GetElementEnumerator();
    }

    public override IElementEnumerator GetAsyncIterableEnumerator()
    {
        if (JSValue.SymbolAsyncIterator != null)
        {
            var asyncIterator = this[JSValue.SymbolAsyncIterator];
            if (!asyncIterator.IsUndefined)
                return new JSIterator(asyncIterator.InvokeFunction(new Arguments(this)), awaitResult: true);
        }

        return GetIterableEnumerator();
    }

    private readonly struct ElementEnumerator(JSObject @object) : IElementEnumerator
    {
        readonly IEnumerator<(uint Key, JSProperty Value)> en = @object.elements.AllValues().GetEnumerator();

        public bool MoveNext(out bool hasValue, out JSValue value, out uint index)
        {
            if (en?.MoveNext() ?? false)
            {
                var (Key, Value) = en.Current;
                value = @object.GetValue(Value);
                index = Key;
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
            if (en?.MoveNext() ?? false)
            {
                var (Key, Value) = en.Current;
                value = @object.GetValue(Value);
                return true;
            }

            value = JSValue.UndefinedValue;
            return false;
        }

        public bool MoveNextOrDefault(out JSValue value, JSValue @default)
        {
            if (en?.MoveNext() ?? false)
            {
                var (_, Value) = en.Current;
                value = @object.GetValue(Value);
                return true;
            }

            value = @default;
            return false;
        }

        public JSValue NextOrDefault(JSValue @default)
        {
            if (en?.MoveNext() ?? false)
            {
                var (Key, Value) = en.Current;
                return @object.GetValue(Value);
            }

            return @default;
        }
    }
}
