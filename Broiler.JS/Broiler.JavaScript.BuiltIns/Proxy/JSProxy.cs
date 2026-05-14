using System.Collections.Generic;
using Broiler.JavaScript.BuiltIns.Array;
using Broiler.JavaScript.BuiltIns.Symbol;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.BuiltIns.Function;
using Broiler.JavaScript.Engine.Extensions;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Proxy;

[JSBaseClass("Object")]
[JSFunctionGenerator("Proxy")]
public partial class JSProxy : JSObject
{
    private static readonly KeyString ConstructTrapKey = KeyStrings.GetOrCreate("construct");
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

    private static void ValidateOwnKeysInvariant(JSObject target, HashSet<string> seenKeys)
    {
        foreach (var (key, property) in target.GetElements().AllValues())
        {
            if (!property.IsConfigurable && !seenKeys.Contains(CreateKeyIdentity(key)))
                throw JSEngine.NewTypeError("Proxy ownKeys trap must include all non-configurable target keys");
        }

        var properties = target.GetOwnProperties(false).GetEnumerator(false);
        while (properties.MoveNext(out KeyString key, out JSProperty property))
        {
            if (!property.IsConfigurable && !seenKeys.Contains(CreateKeyIdentity(key)))
                throw JSEngine.NewTypeError("Proxy ownKeys trap must include all non-configurable target keys");
        }

        foreach (var (key, property) in target.GetSymbols().AllValues())
        {
            if (!property.IsConfigurable && !seenKeys.Contains(CreateSymbolKeyIdentity(key)))
                throw JSEngine.NewTypeError("Proxy ownKeys trap must include all non-configurable target keys");
        }
    }

    public override JSValue InvokeFunction(in Arguments a)
    {
        var target = RequireTarget();
        var fx = handler[KeyStrings.apply];
        if (fx is JSFunction fxFunction)
        {
            var args = new JSArray(a.ToArray());
            return fxFunction.Call(this, target, a.This, args);
        }

        return target.InvokeFunction(a);
    }

    public override JSValue CreateInstance(in Arguments a)
    {
        var target = RequireTarget();
        var ec = JSEngine.Current as IJSExecutionContext;
        var newTarget = ec?.CurrentNewTarget ?? this;
        var constructTrap = handler[ConstructTrapKey];
        if (constructTrap is JSFunction fxFunction)
        {
            var args = new JSArray(a.ToArray());
            return fxFunction.Call(this, target, args, newTarget);
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
        var fx = handler[KeyStrings.defineProperty];
        if (fx is JSFunction fxFunction)
            return fxFunction.InvokeFunction(new Arguments(target, target, key, propertyDescription));

        return target.DefineProperty(key, propertyDescription);
    }

    public override JSValue Delete(JSValue index)
    {
        var target = RequireTarget();
        var fx = handler[KeyStrings.deleteProperty];
        if (fx is JSFunction fxFunction)
            return fxFunction.InvokeFunction(new Arguments(target, target, index));

        return target.Delete(index);
    }

    internal protected override JSValue GetValue(IJSSymbol key, JSValue receiver, bool throwError = true)
    {
        var target = RequireTarget();
        var fx = handler[KeyStrings.get];
        if (fx is JSFunction fxFunction)
        {
            var result = fxFunction.InvokeFunction(new Arguments(target, target, (JSValue)(JSSymbol)key, receiver));
            ValidateGetInvariant(target, PropertyKey.FromSymbol(key), result);
            return result;
        }

        return target.GetValue(key, receiver, throwError);
    }

    internal protected override JSValue GetValue(KeyString key, JSValue receiver, bool throwError = true)
    {
        var target = RequireTarget();
        var fx = handler[KeyStrings.get];
        if (fx is JSFunction fxFunction)
        {
            var result = fxFunction.InvokeFunction(new Arguments(target, target, key.ToJSValue(), receiver));
            ValidateGetInvariant(target, key, result);
            return result;
        }

        return target.GetValue(key, receiver, throwError);
    }

    public override JSValue GetValue(uint key, JSValue receiver, bool throwError = true)
    {
        var target = RequireTarget();
        var fx = handler[KeyStrings.get];
        if (fx is JSFunction fxFunction)
        {
            var result = fxFunction.InvokeFunction(new Arguments(target, target, new JSNumber(key), receiver));
            ValidateGetInvariant(target, key, result);
            return result;
        }

        return target.GetValue(key, receiver, throwError);
    }

    internal protected override bool SetValue(IJSSymbol name, JSValue value, JSValue receiver, bool throwError = true)
    {
        var target = RequireTarget();
        var fx = handler[KeyStrings.set];
        if (fx is JSFunction fxFunction)
        {
            var setResult = fxFunction.InvokeFunction(new Arguments(target, target, (JSValue)(JSSymbol)name, value, receiver));
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
        var fx = handler[KeyStrings.set];
        if (fx is JSFunction fxFunction)
        {
            var setResult = fxFunction.InvokeFunction(new Arguments(target, target, name.ToJSValue(), value, receiver));
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
        var fx = handler[KeyStrings.set];
        if (fx is JSFunction fxFunction)
        {
            var setResult = fxFunction.InvokeFunction(new Arguments(target, target, new JSNumber(name), value, receiver));
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
        var fx = handler[KeyStrings.getPrototypeOf];
        if (fx is JSFunction fxFunction)
            return fxFunction.InvokeFunction(new Arguments(target));

        return target.GetPrototypeOf();
    }

    public override void SetPrototypeOf(JSValue proto)
    {
        var target = RequireTarget();
        var fx = handler[KeyStrings.setPrototypeOf];
        if (fx is JSFunction fxFunction)
        {
            fxFunction.InvokeFunction(new Arguments(target, proto));
            return;
        }

        target.SetPrototypeOf(proto);
    }

    public override IElementEnumerator GetAllKeys(bool showEnumerableOnly = true, bool inherited = true)
    {
        var target = RequireTarget();
        var fx = handler[KeyStrings.ownKeys];
        if (fx is JSFunction fxFunction)
        {
            var result = fxFunction.InvokeFunction(new Arguments(target));
            var array = new JSArray();
            var seenKeys = new HashSet<string>();
            var en = result.GetElementEnumerator();
            while (en.MoveNext(out var hasValue, out var value, out var _))
            {
                if (!hasValue)
                    continue;

                var key = value.ToKey();
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
