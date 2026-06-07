using System.ComponentModel;
using System.Runtime.CompilerServices;
using Broiler.JavaScript.BuiltIns.Function;
using Broiler.JavaScript.BuiltIns.Proxy;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.BuiltIns.Class;

public class JSClass : JSFunction
{
    internal readonly JSValue super;

    internal static JSObject ResolveSuperclassPrototype(JSValue super)
    {
        if (super.IsNull)
            return null;

        if (!IsConstructableSuperclass(super))
            throw JSEngine.NewTypeError("Class extends value is not a constructor or null");

        var superPrototype = super[KeyStrings.prototype];
        if (superPrototype.IsNull)
            return null;

        if (superPrototype is JSObject superPrototypeObject)
            return superPrototypeObject;

        throw JSEngine.NewTypeError("Class extends value does not have a valid prototype property");
    }

    private static bool IsConstructableSuperclass(JSValue value) => JSConstructorOperations.IsConstructor(value);

    public JSClass(JSFunctionDelegate fx, JSValue super, string name = null, string code = null)
        : base(fx ?? (super as JSFunction)?.Delegate ?? empty, name, code)
    {
        this.super = super;
        if (super is JSObject superObject)
            BasePrototypeObject = superObject;

        prototype.BasePrototypeObject = ResolveSuperclassPrototype(super);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void AddConstructor(JSFunction fx)
    {
        f = fx.f;

        // A class with its own (user-written or field-synthesised) constructor is
        // an ordinary user function: when its body explicitly returns a distinct
        // object, that object is yielded as-is with its own [[Prototype]]. A
        // body-less default-derived class instead delegates straight to its
        // superclass [[Construct]] (f stays the super delegate) and keeps this
        // false, so CreateInstance still applies the newTarget-derived prototype
        // to whatever that native/derived machinery produced.
        IsOrdinaryUserFunction = fx.IsOrdinaryUserFunction;
    }

    public override JSValue InvokeFunction(in Arguments a)
    {
        if (JSEngine.NewTarget == null && (JSEngine.Current as IJSExecutionContext)?.CurrentNewTarget == null)
            throw JSEngine.NewTypeError($"{this} is not a function");

        return f(a);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override JSValue CreateInstance(in Arguments a)
    {
        static void ValidateProxyNewTarget(JSProxy proxy) => _ = proxy.RequireTarget();

        JSObject ResolveInstancePrototype(JSValue newTargetValue)
        {
            var newTargetPrototype = newTargetValue[KeyStrings.prototype];
            if (newTargetPrototype is JSObject newTargetPrototypeObject)
                return newTargetPrototypeObject;

            if (newTargetValue is JSProxy proxy)
                ValidateProxyNewTarget(proxy);

            return prototype;
        }

        var ec = JSEngine.Current as IJSExecutionContext;
        var previousNewTarget = ec?.CurrentNewTarget;
        var instancePrototype = previousNewTarget != null
            ? ResolveInstancePrototype(previousNewTarget)
            : prototype;

        var @object = new JSObject() { BasePrototypeObject = instancePrototype };
        var ao = a.OverrideThis(@object);

        JSValue @this;
        try
        {
            if (ec != null)
                ec.CurrentNewTarget = previousNewTarget ?? this;

            @this = f(ao);
        }
        finally
        {
            if (ec != null)
                ec.CurrentNewTarget = previousNewTarget;
        }

        if (@this == null || @this.IsUndefined)
            return @object;

        if (@this.IsObject)
        {
            // An ordinary user class whose constructor explicitly returns a
            // distinct object yields that object as-is, preserving its own
            // [[Prototype]] (ECMAScript [[Construct]] step 13: "If
            // Type(result.[[Value]]) is Object, return result.[[Value]]"). Only
            // the engine-allocated `this` — the object OrdinaryCreateFromConstructor
            // produced, or, for a derived class, the one super() bound — receives
            // the newTarget-derived prototype. A body-less default-derived class
            // (IsOrdinaryUserFunction == false) keeps the older behaviour of
            // forcing the prototype, which the native/derived delegate it inherits
            // does not always set correctly on its own.
            if (!IsOrdinaryUserFunction || ReferenceEquals(@this, @object))
                @this.BasePrototypeObject = instancePrototype;

            return @this;
        }

        return @object;
    }
}
