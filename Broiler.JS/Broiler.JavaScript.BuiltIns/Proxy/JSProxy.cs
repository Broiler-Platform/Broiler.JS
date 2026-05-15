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
    private bool revoked;

    protected JSProxy((JSObject target, JSObject handler) p) : base((JSEngine.Current as IJSExecutionContext)?.ObjectPrototype)
    {
        var (target, handler) = p;
        if (target == null || handler == null)
            throw JSEngine.NewTypeError("Cannot create proxy with a non-object as target or handler");

        this.target = target;
        this.handler = handler;
        callable = IsCallableTarget(target);
    }

    public override bool BooleanValue => target.BooleanValue;
    public override bool IsArray => RequireTarget().IsArray;

    public override bool Equals(JSValue value) => target.Equals(value);

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

    private JSValue GetTrap(KeyString trapKey)
    {
        var trap = handler[trapKey];
        if (trap.IsNullOrUndefined)
            return JSUndefined.Value;

        if (!trap.IsFunction)
            throw JSEngine.NewTypeError($"Proxy trap '{trapKey}' is not callable (received {trap.TypeOf()})");

        return trap;
    }

    private static JSProperty GetOwnTargetProperty(JSObject target, in PropertyKey key)
    {
        if (key.IsSymbol)
            return target.GetInternalProperty(key.Symbol, false);

        if (key.IsUInt)
            return target.GetInternalProperty(key.Index, false);

        return target.GetInternalProperty(in key.KeyString, false);
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
                if (!value.StrictEquals(targetValue))
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
                    && !descriptor[KeyStrings.get].StrictEquals(property.get as JSValue ?? JSUndefined.Value))
                {
                    return false;
                }

                if (HasDescriptorField(descriptor, KeyStrings.set)
                    && !descriptor[KeyStrings.set].StrictEquals(property.set as JSValue ?? JSUndefined.Value))
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
                if (!descriptor[KeyStrings.value].StrictEquals(targetValue))
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
            return fx.InvokeFunction(new Arguments(this, target, a.This, args));
        }

        return target.InvokeFunction(a);
    }

    public override JSValue CreateInstance(in Arguments a)
    {
        var target = RequireTarget();
        var ec = JSEngine.Current as IJSExecutionContext;
        var newTarget = ec?.CurrentNewTarget ?? this;
        var constructTrap = GetTrap(ConstructTrapKey);
        if (!constructTrap.IsUndefined)
        {
            var args = new JSArray(a.ToArray());
            var result = constructTrap.InvokeFunction(new Arguments(this, target, args, newTarget));
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
            var result = fx.InvokeFunction(new Arguments(target, target, key, propertyDescription));
            if (!result.BooleanValue)
                return JSBoolean.False;

            ValidateDefinePropertyInvariant(target, key.ToKey(false), propertyDescription);
            return JSBoolean.True;
        }

        return target.DefineProperty(key, propertyDescription);
    }

    public override JSValue Delete(JSValue index)
    {
        var target = RequireTarget();
        var fx = GetTrap(KeyStrings.deleteProperty);
        if (!fx.IsUndefined)
        {
            var result = fx.InvokeFunction(new Arguments(target, target, index));
            if (!result.BooleanValue)
                return JSBoolean.False;

            ValidateDeleteInvariant(target, index.ToKey(false));
            return JSBoolean.True;
        }

        return target.Delete(index);
    }

    public override JSValue GetOwnPropertyDescriptor(JSValue name)
    {
        var target = RequireTarget();
        var fx = GetTrap(GetOwnPropertyDescriptorTrapKey);
        if (!fx.IsUndefined)
        {
            var result = fx.InvokeFunction(new Arguments(target, target, name));
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
            var result = fx.InvokeFunction(new Arguments(target, target, (JSValue)(JSSymbol)key, receiver));
            ValidateGetInvariant(target, PropertyKey.FromSymbol(key), result);
            return result;
        }

        return target.GetValue(key, receiver, throwError);
    }

    internal protected override JSValue GetValue(KeyString key, JSValue receiver, bool throwError = true)
    {
        var target = RequireTarget();
        var fx = GetTrap(KeyStrings.get);
        if (!fx.IsUndefined)
        {
            var result = fx.InvokeFunction(new Arguments(target, target, key.ToJSValue(), receiver));
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
            var result = fx.InvokeFunction(new Arguments(target, target, new JSNumber(key), receiver));
            ValidateGetInvariant(target, key, result);
            return result;
        }

        return target.GetValue(key, receiver, throwError);
    }

    internal protected override bool SetValue(IJSSymbol name, JSValue value, JSValue receiver, bool throwError = true)
    {
        var target = RequireTarget();
        var fx = GetTrap(KeyStrings.set);
        if (!fx.IsUndefined)
        {
            var setResult = fx.InvokeFunction(new Arguments(target, target, (JSValue)(JSSymbol)name, value, receiver));
            if (!setResult.BooleanValue)
                return false;

            ValidateSetInvariant(target, PropertyKey.FromSymbol(name), value);
            return true;
        }

        return target.SetValue(name, value, receiver, false);
    }

    internal protected override bool SetValue(KeyString name, JSValue value, JSValue receiver, bool throwError = true)
    {
        var target = RequireTarget();
        var fx = GetTrap(KeyStrings.set);
        if (!fx.IsUndefined)
        {
            var setResult = fx.InvokeFunction(new Arguments(target, target, name.ToJSValue(), value, receiver));
            if (!setResult.BooleanValue)
                return false;

            ValidateSetInvariant(target, name, value);
            return true;
        }

        return target.SetValue(name, value, receiver, false);
    }

    public override bool SetValue(uint name, JSValue value, JSValue receiver, bool throwError = true)
    {
        var target = RequireTarget();
        var fx = GetTrap(KeyStrings.set);
        if (!fx.IsUndefined)
        {
            var setResult = fx.InvokeFunction(new Arguments(target, target, new JSNumber(name), value, receiver));
            if (!setResult.BooleanValue)
                return false;

            ValidateSetInvariant(target, name, value);
            return true;
        }

        return target.SetValue(name, value, receiver, false);
    }

    public override JSValue GetPrototypeOf()
    {
        var target = RequireTarget();
        var fx = GetTrap(KeyStrings.getPrototypeOf);
        if (!fx.IsUndefined)
        {
            var result = fx.InvokeFunction(new Arguments(target));
            if (!result.IsObject && !result.IsNull)
                throw JSEngine.NewTypeError("Proxy getPrototypeOf trap must return an object or null");

            if (!target.IsExtensible() && !ReferenceEquals(target.GetPrototypeOf(), result))
                throw JSEngine.NewTypeError("Proxy getPrototypeOf trap returned an inconsistent prototype");

            return result;
        }

        return target.GetPrototypeOf();
    }

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
            var result = fx.InvokeFunction(new Arguments(target));
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
            var result = fx.InvokeFunction(new Arguments(target));
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
        var target = RequireTarget();
        var fx = GetTrap(KeyStrings.setPrototypeOf);
        if (!fx.IsUndefined)
        {
            var result = fx.InvokeFunction(new Arguments(target, proto));
            if (!result.BooleanValue)
                throw JSEngine.NewTypeError("Proxy setPrototypeOf trap returned false");

            if (!target.IsExtensible() && !ReferenceEquals(target.GetPrototypeOf(), proto))
                throw JSEngine.NewTypeError("Proxy setPrototypeOf trap returned true for an invalid prototype change");

            return;
        }

        target.SetPrototypeOf(proto);
    }

    public override IElementEnumerator GetAllKeys(bool showEnumerableOnly = true, bool inherited = true)
    {
        var target = RequireTarget();
        var fx = GetTrap(KeyStrings.ownKeys);
        if (!fx.IsUndefined)
        {
            var result = fx.InvokeFunction(new Arguments(target));
            var array = new JSArray();
            var seenKeys = new HashSet<string>();
            var en = result.GetElementEnumerator();
            while (en.MoveNext(out var hasValue, out var value, out var _))
            {
                if (!hasValue)
                    continue;

                if (!value.IsString && !value.IsSymbol)
                    throw JSEngine.NewTypeError("Proxy ownKeys trap must return only string and symbol keys");

                var key = value.ToKey(false);
                var identity = CreateKeyIdentity(key);
                if (!seenKeys.Add(identity))
                    throw JSEngine.NewTypeError("Proxy ownKeys trap cannot report duplicate keys");

                array.Add(value);
            }

            ValidateOwnKeysInvariant(target, seenKeys);
            return array.GetElementEnumerator();
        }

        return target.GetAllKeys(showEnumerableOnly, inherited);
    }

    public override bool StrictEquals(JSValue value) => RequireTarget().StrictEquals(value);

    public override JSValue TypeOf() => callable ? JSConstants.Function : JSConstants.Object;

    internal override PropertyKey ToKey(bool create = false) => RequireTarget().ToKey();

    [JSExport(IsConstructor = true)]
    public new static JSValue Constructor(in Arguments a)
    {
        var (f, s) = a.Get2();
        return new JSProxy((f as JSObject, s as JSObject));
    }

    [JSExport("revocable", Length = 2)]
    public static JSValue Revocable(in Arguments a)
    {
        var (target, handler) = a.Get2();
        var proxy = new JSProxy((target as JSObject, handler as JSObject));
        var result = new JSObject();

        result.FastAddValue("proxy", proxy, JSPropertyAttributes.ConfigurableValue);
        result.FastAddValue(
            "revoke",
            JSValue.CreateFunction((in Arguments _) =>
            {
                proxy.Revoke();
                return JSUndefined.Value;
            }, "revoke", length: 0, createPrototype: false),
            JSPropertyAttributes.ConfigurableValue);

        return result;
    }
}
