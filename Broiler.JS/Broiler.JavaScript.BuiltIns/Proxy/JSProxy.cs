using System;
using System.Collections.Generic;
using Broiler.JavaScript.BuiltIns.Array;
using Broiler.JavaScript.BuiltIns.Symbol;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.BuiltIns.Boolean;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.BuiltIns.Function;
using Broiler.JavaScript.Engine.Extensions;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.Storage;

namespace Broiler.JavaScript.BuiltIns.Proxy;

[JSBaseClass("Object")]
[JSFunctionGenerator("Proxy")]
public partial class JSProxy : JSObject
{
    private static readonly KeyString ConstructTrapKey = KeyStrings.GetOrCreate("construct");
    private static readonly KeyString HasTrapKey = KeyStrings.GetOrCreate("has");
    private static readonly KeyString IsExtensibleTrapKey = KeyStrings.GetOrCreate("isExtensible");
    private static readonly KeyString PreventExtensionsTrapKey = KeyStrings.GetOrCreate("preventExtensions");
    private static readonly KeyString GetOwnPropertyDescriptorTrapKey = KeyStrings.GetOrCreate("getOwnPropertyDescriptor");
    readonly JSObject target;
    private readonly JSObject handler;
    private readonly bool callable;
    private readonly bool constructable;
    private bool revoked;

    protected JSProxy((JSObject target, JSObject handler) p) : base((JSEngine.Current as IJSExecutionContext)?.ObjectPrototype)
    {
        var (target, handler) = p;
        if (target == null || handler == null)
            throw JSEngine.NewTypeError("Cannot create proxy with a non-object as target or handler");

        this.target = target;
        this.handler = handler;
        callable = IsCallableTarget(target);
        constructable = IsConstructableTarget(target);
    }

    public override bool BooleanValue => target.BooleanValue;
    public override bool IsArray => RequireTarget().IsArray;
    public override bool IsFunction => callable;

    // A Proxy has its own identity: `===` (SameValueNonNumeric) and `==` compare it by
    // reference, never by its target — `new Proxy(t, {}) === t` is false and a proxy equals
    // only itself. Inherit JSObject's Equals/StrictEquals (reference identity, plus trap-aware
    // primitive coercion for `==`); delegating to the target broke proxy identity comparison.

    internal JSObject RequireTarget()
    {
        if (revoked)
            throw JSEngine.NewTypeError("Cannot perform operation on a revoked Proxy");

        return target;
    }

    internal void Revoke() => revoked = true;

    private static bool IsCallableTarget(JSObject target) => target switch
    {
        JSFunction => true,
        JSProxy proxy => proxy.callable,
        _ => false
    };

    internal bool IsConstructable => constructable;

    private static bool IsConstructableTarget(JSObject target) => target switch
    {
        JSFunction function when function.BoundTargetFunction is JSObject boundTarget && !function.BoundTargetFunction.IsUndefined
            => IsConstructableTarget(boundTarget),
        JSFunction function => function.prototype != null,
        JSProxy proxy => proxy.constructable,
        _ => false
    };

    private JSValue GetTrap(KeyString trapKey)
    {
        var trap = handler[trapKey];
        if (trap.IsNullOrUndefined)
            return JSUndefined.Value;

        if (!trap.IsFunction)
            throw JSEngine.NewTypeError($"Proxy trap '{trapKey}' is not callable (received {trap.TypeOf()})");

        return trap;
    }

    private static JSValue NormalizeTrapPropertyKey(JSValue key)
    {
        var propertyKey = key.ToKey(false);
        return propertyKey.Type switch
        {
            KeyType.UInt => JSValue.CreateString(propertyKey.Index.ToString()),
            KeyType.String => JSValue.CreateString(propertyKey.KeyString.ToString()),
            KeyType.Symbol => (JSValue)(JSSymbol)propertyKey.Symbol,
            _ => key
        };
    }

    internal bool HasTrap(KeyString trapKey) => !GetTrap(trapKey).IsUndefined;

    internal JSObject Target => RequireTarget();

    private static JSProperty GetOwnTargetProperty(JSObject target, in PropertyKey key)
    {
        var property = key.IsSymbol
            ? target.GetInternalProperty(key.Symbol, false)
            : key.IsUInt
                ? target.GetInternalProperty(key.Index, false)
                : target.GetInternalProperty(in key.KeyString, false);

        if (!property.IsEmpty || key.IsSymbol)
            return property;

        // The invariant checks read target.[[GetOwnProperty]] per spec. An exotic
        // object can expose an own property that isn't backed by ordinary storage —
        // most notably an Array's `length` — which GetInternalProperty misses. Fall
        // back to [[GetOwnProperty]] so e.g. `Reflect.set(arr, "length", v, proxy)`
        // (from pop/shift via a Proxy) sees `length` as a real (non-configurable)
        // property rather than reporting it as absent.
        var name = key.IsUInt ? JSValue.CreateNumber(key.Index) : key.KeyString.ToJSValue();
        if (target.GetOwnPropertyDescriptor(name) is JSObject descriptor && IsDataDescriptor(descriptor))
        {
            bool Flag(KeyString field) => HasDescriptorField(descriptor, field) && descriptor[field].BooleanValue;
            var attributes = JSPropertyAttributes.Value
                | (Flag(KeyStrings.configurable) ? JSPropertyAttributes.Configurable : JSPropertyAttributes.Empty)
                | (Flag(KeyStrings.enumerable) ? JSPropertyAttributes.Enumerable : JSPropertyAttributes.Empty)
                | (Flag(KeyStrings.writable) ? JSPropertyAttributes.Empty : JSPropertyAttributes.Readonly);
            var ks = key.IsUInt ? KeyString.Empty : key.KeyString;
            return new JSProperty(in ks, descriptor[KeyStrings.value], attributes);
        }

        return property;
    }

    private static string CreateKeyIdentity(in PropertyKey key)
    {
        if (key.IsSymbol)
            return $"y:{key.Symbol.Key}";

        if (key.IsUInt)
            return $"u:{key.Index}";

        return $"s:{key.KeyString.Key}";
    }

    private static string CreateSymbolKeyIdentity(uint key) => $"y:{key}";

    private static void ValidateGetInvariant(JSObject target, in PropertyKey key, JSValue trapResult)
    {
        var property = GetOwnTargetProperty(target, in key);
        if (property.IsEmpty || property.IsConfigurable)
            return;

        if (!property.IsProperty)
        {
            if (property.IsReadOnly)
            {
                var targetValue = target.GetValue(property);
                if (!trapResult.StrictEquals(targetValue))
                    throw JSEngine.NewTypeError("Proxy get trap violated an invariant for a non-configurable, non-writable property");
            }

            return;
        }

        if (property.get == null && !trapResult.IsUndefined)
            throw JSEngine.NewTypeError("Proxy get trap violated an invariant for a non-configurable accessor without a getter");
    }

    private static void ValidateSetInvariant(JSObject target, in PropertyKey key, JSValue value)
    {
        var property = GetOwnTargetProperty(target, in key);
        if (property.IsEmpty || property.IsConfigurable)
            return;

        if (!property.IsProperty)
        {
            if (property.IsReadOnly)
            {
                var targetValue = target.GetValue(property);
                if (!value.Is(targetValue).BooleanValue)
                    throw JSEngine.NewTypeError("Proxy set trap violated an invariant for a non-configurable, non-writable property");
            }

            return;
        }

        if (property.set == null)
            throw JSEngine.NewTypeError("Proxy set trap violated an invariant for a non-configurable accessor without a setter");
    }

    private static bool HasDescriptorField(JSObject descriptor, KeyString key)
        => !descriptor.GetInternalProperty(key, false).IsEmpty;

    private static bool IsAccessorDescriptor(JSObject descriptor)
        => HasDescriptorField(descriptor, KeyStrings.get) || HasDescriptorField(descriptor, KeyStrings.set);

    private static bool IsDataDescriptor(JSObject descriptor)
        => HasDescriptorField(descriptor, KeyStrings.value) || HasDescriptorField(descriptor, KeyStrings.writable);

    private static bool IsCompatibleDescriptor(JSObject descriptor, JSObject target, in JSProperty property)
    {
        if (!property.IsConfigurable)
        {
            if (HasDescriptorField(descriptor, KeyStrings.configurable) && descriptor[KeyStrings.configurable].BooleanValue)
                return false;

            if (HasDescriptorField(descriptor, KeyStrings.enumerable)
                && descriptor[KeyStrings.enumerable].BooleanValue != property.IsEnumerable)
            {
                return false;
            }
        }

        var descriptorIsAccessor = IsAccessorDescriptor(descriptor);
        var descriptorIsData = IsDataDescriptor(descriptor);

        if (property.IsProperty)
        {
            if (descriptorIsData)
                return false;

            if (!property.IsConfigurable)
            {
                if (HasDescriptorField(descriptor, KeyStrings.get)
                    && !descriptor[KeyStrings.get].Is(property.get as JSValue ?? JSUndefined.Value).BooleanValue)
                {
                    return false;
                }

                if (HasDescriptorField(descriptor, KeyStrings.set)
                    && !descriptor[KeyStrings.set].Is(property.set as JSValue ?? JSUndefined.Value).BooleanValue)
                {
                    return false;
                }
            }

            return true;
        }

        if (descriptorIsAccessor)
            return false;

        if (!property.IsConfigurable)
        {
            if (HasDescriptorField(descriptor, KeyStrings.writable))
            {
                var writable = descriptor[KeyStrings.writable].BooleanValue;
                if (property.IsReadOnly && writable)
                    return false;
            }

            if (property.IsReadOnly && HasDescriptorField(descriptor, KeyStrings.value))
            {
                var targetValue = target.GetValue(property);
                if (!descriptor[KeyStrings.value].Is(targetValue).BooleanValue)
                    return false;
            }
        }

        return true;
    }

    private static void ValidateDefinePropertyInvariant(JSObject target, in PropertyKey key, JSObject descriptor)
    {
        var property = GetOwnTargetProperty(target, in key);
        var extensibleTarget = target.IsExtensible();
        var settingConfigFalse = HasDescriptorField(descriptor, KeyStrings.configurable)
            && !descriptor[KeyStrings.configurable].BooleanValue;

        if (property.IsEmpty)
        {
            if (!extensibleTarget || settingConfigFalse)
                throw JSEngine.NewTypeError("Proxy defineProperty trap violated target invariants");

            return;
        }

        if (!IsCompatibleDescriptor(descriptor, target, in property))
            throw JSEngine.NewTypeError("Proxy defineProperty trap returned an incompatible descriptor");

        if (settingConfigFalse && property.IsConfigurable)
            throw JSEngine.NewTypeError("Proxy defineProperty trap cannot report a configurable target property as non-configurable");

        if (!property.IsConfigurable
            && !property.IsProperty
            && !property.IsReadOnly
            && HasDescriptorField(descriptor, KeyStrings.writable)
            && !descriptor[KeyStrings.writable].BooleanValue)
        {
            throw JSEngine.NewTypeError("Proxy defineProperty trap cannot make a non-configurable writable property non-writable");
        }
    }

    private static void ValidateDeleteInvariant(JSObject target, in PropertyKey key)
    {
        var property = GetOwnTargetProperty(target, in key);
        if (property.IsEmpty)
            return;

        if (!property.IsConfigurable || !target.IsExtensible())
            throw JSEngine.NewTypeError("Proxy deleteProperty trap violated target invariants");
    }

    private static void ValidateHasInvariant(JSObject target, in PropertyKey key)
    {
        var property = GetOwnTargetProperty(target, in key);
        if (property.IsEmpty)
            return;

        if (!property.IsConfigurable || !target.IsExtensible())
            throw JSEngine.NewTypeError("Proxy has trap violated target invariants");
    }

    private static void ValidateGetOwnPropertyDescriptorInvariant(JSObject target, in PropertyKey key, JSValue trapResult)
    {
        var property = GetOwnTargetProperty(target, in key);
        var extensibleTarget = target.IsExtensible();

        if (trapResult.IsUndefined)
        {
            if (!property.IsEmpty && (!property.IsConfigurable || !extensibleTarget))
                throw JSEngine.NewTypeError("Proxy getOwnPropertyDescriptor trap cannot hide an existing property");

            return;
        }

        if (trapResult is not JSObject descriptor)
            throw JSEngine.NewTypeError("Proxy getOwnPropertyDescriptor trap must return an object or undefined");

        var settingConfigFalse = HasDescriptorField(descriptor, KeyStrings.configurable)
            && !descriptor[KeyStrings.configurable].BooleanValue;

        if (property.IsEmpty)
        {
            if (!extensibleTarget || settingConfigFalse)
                throw JSEngine.NewTypeError("Proxy getOwnPropertyDescriptor trap returned an incompatible descriptor");

            return;
        }

        if (!IsCompatibleDescriptor(descriptor, target, in property))
            throw JSEngine.NewTypeError("Proxy getOwnPropertyDescriptor trap returned an incompatible descriptor");

        if (settingConfigFalse && property.IsConfigurable)
            throw JSEngine.NewTypeError("Proxy getOwnPropertyDescriptor trap cannot report a configurable target property as non-configurable");

        if (settingConfigFalse
            && !property.IsProperty
            && HasDescriptorField(descriptor, KeyStrings.writable)
            && !descriptor[KeyStrings.writable].BooleanValue
            && !property.IsReadOnly)
        {
            throw JSEngine.NewTypeError("Proxy getOwnPropertyDescriptor trap cannot report a writable target property as non-writable");
        }
    }

    private static void ValidateOwnKeysInvariant(JSObject target, HashSet<string> seenKeys)
    {
        void ValidateOwnKey(string identity, in JSProperty property)
        {
            if (!property.IsConfigurable)
            {
                if (!seenKeys.Remove(identity))
                    throw JSEngine.NewTypeError("Proxy ownKeys trap must include all non-configurable target keys");

                return;
            }

            if (target.IsExtensible())
                return;

            if (!seenKeys.Remove(identity))
                throw JSEngine.NewTypeError("Proxy ownKeys trap must include all keys of a non-extensible target");
        }

        foreach (var (key, property) in target.GetElements().AllValues())
            ValidateOwnKey(CreateKeyIdentity(key), property);

        var properties = target.GetOwnProperties(false).GetEnumerator(false);
        while (properties.MoveNext(out KeyString key, out JSProperty property))
            ValidateOwnKey(CreateKeyIdentity(key), property);

        foreach (var (key, property) in target.GetSymbols().AllValues())
            ValidateOwnKey(CreateSymbolKeyIdentity(key), property);

        if (!target.IsExtensible() && seenKeys.Count > 0)
            throw JSEngine.NewTypeError("Proxy ownKeys trap cannot report extra keys for a non-extensible target");
    }

    public override JSValue InvokeFunction(in Arguments a)
    {
        var target = RequireTarget();
        var fx = GetTrap(KeyStrings.apply);
        if (!fx.IsUndefined)
        {
            var args = new JSArray(a.ToArray());
            return fx.InvokeFunction(new Arguments(handler, target, a.This, args));
        }

        return target.InvokeFunction(a);
    }

    public override JSValue CreateInstance(in Arguments a)
    {
        var target = RequireTarget();
        if (!constructable)
            throw JSEngine.NewTypeError("Proxy target is not a constructor");

        var ec = JSEngine.Current as IJSExecutionContext;
        var newTarget = ec?.CurrentNewTarget ?? this;
        var constructTrap = GetTrap(ConstructTrapKey);
        if (!constructTrap.IsUndefined)
        {
            var args = new JSArray(a.ToArray());
            var result = constructTrap.InvokeFunction(new Arguments(handler, target, args, newTarget));
            if (!result.IsObject)
                throw JSEngine.NewTypeError("Proxy construct trap must return an object");

            return result;
        }

        var previousNewTarget = ec?.CurrentNewTarget;

        if (ec != null && previousNewTarget == null)
            ec.CurrentNewTarget = this;

        try
        {
            return target.CreateInstance(a);
        }
        finally
        {
            if (ec != null)
                ec.CurrentNewTarget = previousNewTarget;
        }
    }

    public override JSValue DefineProperty(JSValue key, JSObject propertyDescription)
    {
        var target = RequireTarget();
        var fx = GetTrap(KeyStrings.defineProperty);
        if (!fx.IsUndefined)
        {
            // §10.5.6 step 9: the trap receives FromPropertyDescriptor(Desc), whose
            // fields appear in the canonical §6.2.5.4 order — not the order the
            // caller happened to write them in.
            var result = fx.InvokeFunction(new Arguments(handler, target, NormalizeTrapPropertyKey(key), CanonicalizeTrapDescriptor(propertyDescription)));
            if (!result.BooleanValue)
                return JSBoolean.False;

            ValidateDefinePropertyInvariant(target, key.ToKey(false), propertyDescription);
            return JSBoolean.True;
        }

        return target.DefineProperty(key, propertyDescription);
    }

    // FromPropertyDescriptor (§6.2.5.4): emit only the present fields, in the order
    // value, writable, get, set, enumerable, configurable.
    private static JSObject CanonicalizeTrapDescriptor(JSObject descriptor)
    {
        var result = new JSObject();
        CopyTrapField(descriptor, result, KeyStrings.value);
        CopyTrapField(descriptor, result, KeyStrings.writable);
        CopyTrapField(descriptor, result, KeyStrings.get);
        CopyTrapField(descriptor, result, KeyStrings.set);
        CopyTrapField(descriptor, result, KeyStrings.enumerable);
        CopyTrapField(descriptor, result, KeyStrings.configurable);
        return result;
    }

    private static void CopyTrapField(JSObject source, JSObject result, in KeyString field)
    {
        var property = source.GetInternalProperty(field, false);
        if (property.IsEmpty)
            return;

        result.FastAddValue(field, source[field], JSPropertyAttributes.EnumerableConfigurableValue);
    }

    public override JSValue DefineProperty(in KeyString name, JSObject pd) => DefineProperty(name.ToJSValue(), pd);

    public override JSValue DefineProperty(uint key, JSObject pd) => DefineProperty(JSValue.CreateString(key.ToString()), pd);

    public override JSValue DefineProperty(IJSSymbol name, JSObject pd) => DefineProperty((JSValue)(JSSymbol)name, pd);

    // A public class field initializer is CreateDataPropertyOrThrow, an observable
    // [[DefineOwnProperty]] — so on a Proxy receiver (via a `return`-override base)
    // it must fire the defineProperty trap and throw if the define is rejected.
    public override void CreateDataProperty(KeyString key, JSValue value) => CreateDataPropertyOrThrow(key.ToJSValue(), value);

    public override void CreateDataProperty(uint index, JSValue value) => CreateDataPropertyOrThrow(JSValue.CreateString(index.ToString()), value);

    public override void CreateDataProperty(JSValue key, JSValue value) => CreateDataPropertyOrThrow(key, value);

    private void CreateDataPropertyOrThrow(JSValue key, JSValue value)
    {
        var descriptor = CreateDataDescriptor(value, JSPropertyAttributes.EnumerableConfigurableValue);
        var result = DefineProperty(key, descriptor);
        if (result.IsBoolean && !result.BooleanValue)
            throw JSEngine.NewTypeError($"Cannot define property {key} on proxy");
    }

    public override JSValue Delete(JSValue index)
    {
        var target = RequireTarget();
        var fx = GetTrap(KeyStrings.deleteProperty);
        if (!fx.IsUndefined)
        {
            var result = fx.InvokeFunction(new Arguments(handler, target, NormalizeTrapPropertyKey(index)));
            if (!result.BooleanValue)
                return JSBoolean.False;

            ValidateDeleteInvariant(target, index.ToKey(false));
            return JSBoolean.True;
        }

        return target.Delete(index);
    }

    public override JSValue Delete(in KeyString key) => Delete(key.ToJSValue());

    public override JSValue Delete(uint key) => Delete(JSValue.CreateString(key.ToString()));

    public override JSValue Delete(IJSSymbol symbol) => Delete((JSValue)(JSSymbol)symbol);

    public override JSValue GetOwnPropertyDescriptor(JSValue name)
    {
        var target = RequireTarget();
        var fx = GetTrap(GetOwnPropertyDescriptorTrapKey);
        if (!fx.IsUndefined)
        {
            var result = fx.InvokeFunction(new Arguments(handler, target, NormalizeTrapPropertyKey(name)));
            ValidateGetOwnPropertyDescriptorInvariant(target, name.ToKey(false), result);
            return result;
        }

        return target.GetOwnPropertyDescriptor(name);
    }

    internal protected override JSValue GetValue(IJSSymbol key, JSValue receiver, bool throwError = true)
    {
        var target = RequireTarget();
        var fx = GetTrap(KeyStrings.get);
        if (!fx.IsUndefined)
        {
            var result = fx.InvokeFunction(new Arguments(handler, target, (JSValue)(JSSymbol)key, receiver));
            ValidateGetInvariant(target, PropertyKey.FromSymbol(key), result);
            return result;
        }

        return target.GetValue(key, receiver, throwError);
    }

    internal protected override JSValue GetValue(KeyString key, JSValue receiver, bool throwError = true)
    {
        // A private member is not a property lookup: it operates on this object's
        // own private elements and never consults the target or a Proxy trap. A
        // proxy that does not itself carry the private name (the common case) fails
        // the brand check with a TypeError; a proxy handed a private field via a
        // constructor return-override holds it as its own slot.
        if (IsPrivateName(in key))
            return base.GetValue(key, receiver, throwError);

        var target = RequireTarget();
        var fx = GetTrap(KeyStrings.get);
        if (!fx.IsUndefined)
        {
            var result = fx.InvokeFunction(new Arguments(handler, target, key.ToJSValue(), receiver));
            ValidateGetInvariant(target, key, result);
            return result;
        }

        return target.GetValue(key, receiver, throwError);
    }

    public override JSValue GetValue(uint key, JSValue receiver, bool throwError = true)
    {
        var target = RequireTarget();
        var fx = GetTrap(KeyStrings.get);
        if (!fx.IsUndefined)
        {
            var result = fx.InvokeFunction(new Arguments(handler, target, JSValue.CreateString(key.ToString()), receiver));
            ValidateGetInvariant(target, key, result);
            return result;
        }

        return target.GetValue(key, receiver, throwError);
    }

    internal protected override bool SetValue(KeyString name, JSValue value, JSValue receiver, bool throwError = true)
    {
        // Private member writes bypass the target and the set trap (see GetValue).
        if (IsPrivateName(in name))
            return base.SetValue(name, value, receiver, throwError);

        if (name.Key == KeyStrings.__proto__.Key)
        {
            if (!value.IsObject && !value.IsNull)
                return true;

            SetPrototypeOf(value);
            return true;
        }

        var target = RequireTarget();
        var fx = GetTrap(KeyStrings.set);
        if (!fx.IsUndefined)
        {
            var setResult = fx.InvokeFunction(new Arguments(handler, target, name.ToJSValue(), value, receiver));
            if (!setResult.BooleanValue)
                return false;

            ValidateSetInvariant(target, name, value);
            return true;
        }

        if (ReferenceEquals(receiver as JSObject ?? this, this)
            && TrySetReceiverOwnProperty(name.ToJSValue(), value, receiver, throwError, out var receiverResult))
        {
            return receiverResult;
        }

        return target.SetValue(name, value, receiver, throwError);
    }

    public override bool SetValue(uint name, JSValue value, JSValue receiver, bool throwError = true)
    {
        var target = RequireTarget();
        var fx = GetTrap(KeyStrings.set);
        if (!fx.IsUndefined)
        {
            var setResult = fx.InvokeFunction(new Arguments(handler, target, JSValue.CreateString(name.ToString()), value, receiver));
            if (!setResult.BooleanValue)
                return false;

            ValidateSetInvariant(target, name, value);
            return true;
        }

        if (ReferenceEquals(receiver as JSObject ?? this, this)
            && TrySetReceiverOwnProperty(JSValue.CreateString(name.ToString()), value, receiver, throwError, out var receiverResult))
        {
            return receiverResult;
        }

        return target.SetValue(name, value, receiver, throwError);
    }

    internal protected override bool SetValue(IJSSymbol name, JSValue value, JSValue receiver, bool throwError = true)
    {
        var target = RequireTarget();
        var fx = GetTrap(KeyStrings.set);
        if (!fx.IsUndefined)
        {
            var setResult = fx.InvokeFunction(new Arguments(handler, target, (JSValue)(JSSymbol)name, value, receiver));
            if (!setResult.BooleanValue)
                return false;

            ValidateSetInvariant(target, PropertyKey.FromSymbol(name), value);
            return true;
        }

        if (ReferenceEquals(receiver as JSObject ?? this, this)
            && TrySetReceiverOwnProperty((JSValue)(JSSymbol)name, value, receiver, throwError, out var receiverResult))
        {
            return receiverResult;
        }

        return target.SetValue(name, value, receiver, throwError);
    }

    private bool TrySetReceiverOwnProperty(JSValue propertyKey, JSValue value, JSValue receiver, bool throwError, out bool result)
    {
        var descriptorValue = GetOwnPropertyDescriptor(propertyKey);
        RequireTarget();

        if (descriptorValue is not JSObject descriptor)
        {
            result = false;
            return false;
        }

        var hasGetter = !descriptor.GetInternalProperty(KeyStrings.get, false).IsEmpty;
        var hasSetter = !descriptor.GetInternalProperty(KeyStrings.set, false).IsEmpty;
        if (hasGetter || hasSetter)
        {
            if (hasSetter && descriptor[KeyStrings.set] is IJSFunction setter)
            {
                setter.InvokeFunction(new Arguments(receiver ?? this, value));
                result = true;
                return true;
            }

            if (throwError)
                throw JSEngine.NewTypeError($"Cannot modify property {propertyKey} of {this} which has only a getter");

            result = false;
            return true;
        }

        if (!descriptor.GetInternalProperty(KeyStrings.writable, false).IsEmpty
            && !descriptor[KeyStrings.writable].BooleanValue)
        {
            if (throwError)
                throw JSEngine.NewTypeError($"Cannot modify property {propertyKey} of {this}");

            result = false;
            return true;
        }

        result = false;
        return false;
    }

    public override JSValue GetPrototypeOf()
    {
        var target = RequireTarget();
        var fx = GetTrap(KeyStrings.getPrototypeOf);
        if (!fx.IsUndefined)
        {
            var result = fx.InvokeFunction(new Arguments(handler, target));
            if (!result.IsObject && !result.IsNull)
                throw JSEngine.NewTypeError("Proxy getPrototypeOf trap must return an object or null");

            if (!target.IsExtensible() && !ReferenceEquals(target.GetPrototypeOf(), result))
                throw JSEngine.NewTypeError("Proxy getPrototypeOf trap returned an inconsistent prototype");

            return result;
        }

        return target.GetPrototypeOf();
    }

    // Object spread / Object.assign must observe the proxy's ownKeys,
    // getOwnPropertyDescriptor and get traps in spec order rather than copying
    // internal slots directly.
    private protected override bool UseObservableSpreadCopy => true;

    public override JSValue HasProperty(JSValue propertyKey)
    {
        var target = RequireTarget();
        var fx = GetTrap(HasTrapKey);
        if (!fx.IsUndefined)
        {
            var result = fx.InvokeFunction(new Arguments(handler, target, propertyKey));
            if (result.BooleanValue)
                return JSBoolean.True;

            ValidateHasInvariant(target, propertyKey.ToKey(false));
            return JSBoolean.False;
        }

        return target.HasProperty(propertyKey);
    }

    public override bool IsExtensible()
    {
        var target = RequireTarget();
        var fx = GetTrap(IsExtensibleTrapKey);
        if (!fx.IsUndefined)
        {
            var result = fx.InvokeFunction(new Arguments(handler, target));
            var isExtensible = result.BooleanValue;
            if (isExtensible != target.IsExtensible())
                throw JSEngine.NewTypeError("Proxy isExtensible trap returned an inconsistent result");

            return isExtensible;
        }

        return target.IsExtensible();
    }

    public override bool PreventExtensions()
    {
        var target = RequireTarget();
        var fx = GetTrap(PreventExtensionsTrapKey);
        if (!fx.IsUndefined)
        {
            var result = fx.InvokeFunction(new Arguments(handler, target));
            if (!result.BooleanValue)
                return false;

            if (target.IsExtensible())
                throw JSEngine.NewTypeError("Proxy preventExtensions trap returned true but target is still extensible");

            status |= ObjectStatus.NonExtensible;
            return true;
        }

        if (!target.PreventExtensions())
            return false;

        status |= ObjectStatus.NonExtensible;
        return true;
    }

    public override void SetPrototypeOf(JSValue proto)
    {
        if (!TrySetPrototypeOf(proto, out var error))
            throw JSEngine.NewTypeError(error ?? "Proxy setPrototypeOf trap returned false");
    }

    public override bool TrySetPrototypeOf(JSValue proto, out string error)
    {
        error = null;
        var target = RequireTarget();
        var fx = GetTrap(KeyStrings.setPrototypeOf);
        if (!fx.IsUndefined)
        {
            var result = fx.InvokeFunction(new Arguments(handler, target, proto));
            // §10.5.2 [[SetPrototypeOf]]: a falsy trap result makes the internal
            // method return false (it does not throw); only Object.setPrototypeOf
            // / __proto__ turn that into a TypeError.
            if (!result.BooleanValue)
                return false;

            if (!target.IsExtensible() && !ReferenceEquals(target.GetPrototypeOf(), proto))
                throw JSEngine.NewTypeError("Proxy setPrototypeOf trap returned true for an invalid prototype change");

            return true;
        }

        return target.TrySetPrototypeOf(proto, out error);
    }

    // ToLength(Get(arrayLike, "length")): ToNumber, then clamp to [0, 2^53-1].
    private static long ToArrayLikeLength(JSValue value)
    {
        var number = value.DoubleValue;
        if (double.IsNaN(number) || number <= 0)
            return 0;

        const double maxSafeInteger = 9007199254740991.0; // 2^53 - 1
        return number >= maxSafeInteger ? (long)maxSafeInteger : (long)System.Math.Floor(number);
    }

    public override IElementEnumerator GetAllKeys(bool showEnumerableOnly = true, bool inherited = true)
    {
        var target = RequireTarget();
        var fx = GetTrap(KeyStrings.ownKeys);
        if (!fx.IsUndefined)
        {
            var result = fx.InvokeFunction(new Arguments(handler, target));
            var array = new JSArray();
            var seenKeys = new HashSet<string>();

            // §10.5.11 step 7 uses CreateListFromArrayLike on the trap result, which
            // reads the array-like's "length" via [[Get]] (observing a length accessor)
            // and then each index 0..length-1 via [[Get]] — a fast element walk would
            // skip the observable "length" read.
            if (result is not JSObject resultObject)
                throw JSEngine.NewTypeError("Proxy ownKeys trap must return an array-like object");

            var length = ToArrayLikeLength(resultObject[KeyStrings.length]);
            for (long i = 0; i < length; i++)
            {
                var value = resultObject[(uint)i];

                if (!value.IsString && !value.IsSymbol)
                    throw JSEngine.NewTypeError("Proxy ownKeys trap must return only string and symbol keys");

                var key = value.ToKey(false);
                var identity = CreateKeyIdentity(key);
                if (!seenKeys.Add(identity))
                    throw JSEngine.NewTypeError("Proxy ownKeys trap cannot report duplicate keys");

                array.Add(value);
            }

            ValidateOwnKeysInvariant(target, seenKeys);

            // The ownKeys trap itself never skips non-enumerable keys (the
            // invariant above is validated over the full key set). Callers that
            // want EnumerableOwnPropertyNames (Object.keys/values/entries) pass
            // showEnumerableOnly: they must additionally filter each key by its
            // [[GetOwnProperty]] descriptor's [[Enumerable]] attribute.
            if (!showEnumerableOnly)
                return array.GetElementEnumerator();

            var enumerableKeys = new JSArray();
            var keyEnumerator = array.GetElementEnumerator();
            while (keyEnumerator.MoveNext(out var keyHasValue, out var key, out var _))
            {
                if (!keyHasValue)
                    continue;

                // EnumerableOwnPropertyNames / for-in enumerate STRING keys only;
                // GetAllKeys never surfaces symbols in its enumerable form (the
                // ordinary KeyEnumerator does the same). Symbol keys for a proxy are
                // obtained via Reflect.ownKeys / getOwnPropertySymbols (showEnumerableOnly: false).
                if (key.IsSymbol)
                    continue;

                var descriptor = GetOwnPropertyDescriptor(key);
                if (descriptor.IsUndefined || !descriptor[KeyStrings.enumerable].BooleanValue)
                    continue;

                enumerableKeys.Add(key);
            }

            return enumerableKeys.GetElementEnumerator();
        }

        var keys = new JSArray();
        var fallbackKeys = target.GetAllKeys(showEnumerableOnly, inherited);
        while (fallbackKeys.MoveNext(out var hasValue, out var value, out var _))
        {
            if (hasValue)
                keys.Add(value);
        }

        // Symbol keys are part of [[OwnPropertyKeys]] (Reflect.ownKeys /
        // getOwnPropertySymbols, showEnumerableOnly: false) but never of the
        // string-key enumeration used by Object.keys / for-in.
        if (!showEnumerableOnly)
        {
            foreach (var (key, property) in target.GetSymbols().AllValues())
            {
                if (property.IsEmpty)
                    continue;

                var symbol = JSValue.GetSymbolByKeyFactory?.Invoke(key)
                    ?? throw new InvalidOperationException($"Unknown symbol key {key}");
                keys.Add((JSValue)symbol);
            }
        }

        return keys.GetElementEnumerator();
    }

    public override JSValue TypeOf() => callable ? JSConstants.Function : JSConstants.Object;

    internal override PropertyKey ToKey(bool create = false) => RequireTarget().ToKey();

    [JSExport(IsConstructor = true)]
    public new static JSValue Constructor(in Arguments a)
    {
        if (JSEngine.NewTarget == null && (JSEngine.Current as IJSExecutionContext)?.CurrentNewTarget == null)
            throw JSEngine.NewTypeError("Proxy constructor requires 'new'");

        var (f, s) = a.Get2();
        return new JSProxy((f as JSObject, s as JSObject));
    }

    [JSExport("revocable", Length = 2)]
    public static JSValue Revocable(in Arguments a)
    {
        var (target, handler) = a.Get2();
        var proxy = new JSProxy((target as JSObject, handler as JSObject));
        var result = new JSObject();
        var revoke = JSValue.CreateFunction((in Arguments _) =>
        {
            proxy.Revoke();
            return JSUndefined.Value;
        }, "revoke", length: 0, createPrototype: false);
        ((JSFunction)revoke).SetNameProperty(string.Empty);

        result.FastAddValue("proxy", proxy, JSPropertyAttributes.ConfigurableValue);
        result.FastAddValue("revoke", revoke, JSPropertyAttributes.ConfigurableValue);

        return result;
    }
}
