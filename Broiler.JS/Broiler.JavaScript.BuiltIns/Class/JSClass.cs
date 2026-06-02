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
    public void AddConstructor(JSFunction fx) => f = fx.f;

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
            @this.BasePrototypeObject = instancePrototype;
            return @this;
        }

        return @object;
    }
}
