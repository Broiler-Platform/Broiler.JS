using System.ComponentModel;
using System.Runtime.CompilerServices;
using Broiler.JavaScript.BuiltIns.Function;
using Broiler.JavaScript.BuiltIns.Proxy;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Storage;

namespace Broiler.JavaScript.BuiltIns.Class;

public class JSClass : JSFunction
{
    internal readonly JSValue super;

    // True for a body-less class with no own constructor (no explicit constructor and
    // no field/private-method-synthesised one). Its [[Construct]] is the default
    // derived constructor `constructor(...args){ super(...args) }`, whose super target
    // is GetSuperConstructor() = the class's CURRENT [[Prototype]] — so it must be
    // resolved dynamically at construction (observing Object.setPrototypeOf(C, X)),
    // not bound to the superclass delegate captured at definition.
    internal bool IsBodylessDefaultConstructor;

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
        IsBodylessDefaultConstructor = fx == null;

        // Class bodies are always strict (MakeClassConstructor / ClassDefinitionEvaluation),
        // so the constructor must run under strict mode when invoked via [[Construct]]
        // (CreateInstance enters EnterStrictMode(IsStrictMode)). The per-member function
        // objects carry their own strict flag, but the JSClass constructor object —
        // whose delegate AddConstructor copies — needs it set here so strict property
        // [[Set]] semantics (e.g. `super.x =` to a non-writable inherited property)
        // throw rather than silently failing.
        IsStrictMode = true;

        if (super is JSObject superObject)
            BasePrototypeObject = superObject;

        prototype.BasePrototypeObject = ResolveSuperclassPrototype(super);

        // Unlike an ordinary function (whose "prototype" is writable), a class's
        // "prototype" is a non-writable, non-enumerable, non-configurable data
        // property (ECMA-262 ClassDefinitionEvaluation / MakeClassConstructor). The
        // base JSFunction constructor installed it as writable, so tighten it here.
        GetOwnProperties().Put(KeyStrings.prototype, prototype, JSPropertyAttributes.ReadonlyValue);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void AddConstructor(JSFunction fx)
    {
        f = fx.f;
        // SetFunctionLength: a class constructor's "length" is the formal-parameter count of
        // the class's own constructor. AddConstructor previously copied only the delegate,
        // leaving the placeholder length (0) the base JSFunction ctor installed, so e.g.
        // `class C { constructor(a, b) {} }` reported `C.length === 0`. Copy the constructor
        // function's own "length" (configurable, non-writable, non-enumerable).
        GetOwnProperties().Put(KeyStrings.length, fx[KeyStrings.length], JSPropertyAttributes.ConfigurableReadonlyValue);

        // The class now has its own constructor body, so it is no longer the default
        // derived constructor; its super references are already compiled dynamically.
        IsBodylessDefaultConstructor = false;

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

    // A tail-positioned call to this class constructor must still hit the
    // "is not a function" guard above, not be looped through the delegate by the
    // JSFunction tail-call fast path (which would skip the new.target check).
    protected override bool SupportsTailCallLoop => false;

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

        // For a body-less default derived constructor, super() targets the class's
        // CURRENT [[Prototype]] (GetSuperConstructor), resolved dynamically here rather
        // than the superclass delegate captured at definition.
        //
        // `class extends null {}` IS a derived class whose synthetic constructor runs
        // `super(...args)`; GetSuperConstructor is %Function.prototype% (not a
        // constructor) so super() throws a TypeError. The null heritage uniquely marks
        // this case: a base class (no heritage) carries the Object sentinel here, never
        // JS null, so base classes are unaffected.
        if (IsBodylessDefaultConstructor && super != null && super.IsNull)
            throw JSEngine.NewTypeError("Super constructor null of derived class is not a constructor");

        var constructorDelegate = IsBodylessDefaultConstructor && GetPrototypeOf() is JSFunction superConstructor
            ? superConstructor.Delegate
            : f;

        JSValue @this;
        try
        {
            if (ec != null)
                ec.CurrentNewTarget = previousNewTarget ?? this;

            // [[Construct]] must run the constructor body under its own strict-mode
            // setting, exactly as [[Call]] does in InvokeFunction. Class constructor
            // bodies are always strict, so a property [[Set]] that fails (e.g. adding
            // a property to a non-extensible object, or `super.x =` onto a
            // non-writable inherited property) must throw a TypeError rather than
            // silently failing. The runtime strict flag is read by JSValue's set
            // accessors via IsStrictModeEnabled, so it must be entered here.
            using (JSEngine.EnterStrictMode(IsStrictMode))
                @this = constructorDelegate(ao);
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
