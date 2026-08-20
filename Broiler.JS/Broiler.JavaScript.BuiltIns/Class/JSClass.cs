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
    /// <summary>
    /// A class constructor tracks its named properties by shape, so its statics reach an inline
    /// cache. See <see cref="JSFunction.SupportsShapeTracking"/> — earned the same way, by this
    /// class installing <c>prototype</c> and <c>length</c> through <c>FastAddValue</c> instead of
    /// a mutable ref to the property store, which abandoned the layout on the spot.
    /// </summary>
    internal override bool SupportsShapeTracking => GetType() == typeof(JSClass);

    internal readonly JSValue super;

    // True for a body-less class with no own constructor (no explicit constructor and
    // no field/private-method-synthesised one). Its [[Construct]] is the default
    // derived constructor `constructor(...args){ super(...args) }`, whose super target
    // is GetSuperConstructor() = the class's CURRENT [[Prototype]] — so it must be
    // resolved dynamically at construction (observing Object.setPrototypeOf(C, X)),
    // not bound to the superclass delegate captured at definition.
    internal bool IsBodylessDefaultConstructor;

    // Whether the class was written with a ClassHeritage — which is what makes its default
    // constructor the DERIVED one that runs `super(...args)`. A base class carries the Object
    // constructor as `super` for prototype resolution alone (see the constructor), so `super`
    // cannot answer this, and only a derived class's [[Construct]] looks at the class's current
    // [[Prototype]] at all.
    internal readonly bool hasHeritage;

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

    public JSClass(JSFunctionDelegate fx, JSValue super, bool hasHeritage, string name = null, string code = null)
        : base(fx ?? (super as JSFunction)?.Delegate ?? empty, name, code)
    {
        this.super = super;
        this.hasHeritage = hasHeritage;
        IsBodylessDefaultConstructor = fx == null;

        // Class bodies are always strict (MakeClassConstructor / ClassDefinitionEvaluation),
        // so the constructor must run under strict mode when invoked via [[Construct]]
        // (CreateInstance enters EnterStrictMode(IsStrictMode)). The per-member function
        // objects carry their own strict flag, but the JSClass constructor object —
        // whose delegate AddConstructor copies — needs it set here so strict property
        // [[Set]] semantics (e.g. `super.x =` to a non-writable inherited property)
        // throw rather than silently failing.
        IsStrictMode = true;

        // A derived class's constructor [[Prototype]] is its superclass
        // (ClassDefinitionEvaluation constructorParent). A base class (no
        // ClassHeritage) keeps %Function.prototype%, which the JSFunction base
        // constructor already installed. The compiler passes the Object constructor
        // as `super` for a base class only so the prototype's [[Prototype]] resolves to
        // %Object.prototype% below — it must NOT also become the constructor's
        // [[Prototype]], which would make `Object.getPrototypeOf(class C {})` the Object
        // constructor instead of Function.prototype.
        if (hasHeritage && super is JSObject superObject)
            BasePrototypeObject = superObject;

        prototype.BasePrototypeObject = ResolveSuperclassPrototype(super);

        // Unlike an ordinary function (whose "prototype" is writable), a class's
        // "prototype" is a non-writable, non-enumerable, non-configurable data
        // property (ECMA-262 ClassDefinitionEvaluation / MakeClassConstructor). The
        // base JSFunction constructor installed it as writable, so tighten it here.
        FastAddValue(KeyStrings.prototype, prototype, JSPropertyAttributes.ReadonlyValue);
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
        FastAddValue(KeyStrings.length, fx[KeyStrings.length], JSPropertyAttributes.ConfigurableReadonlyValue);

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
        using var realmScope = EnterRealm();
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
        var ambientNewTarget = (JSEngine.Current as IJSExecutionContext)?.CurrentNewTarget;
        using var realmScope = EnterRealm();

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
        var restoreNewTarget = ec?.CurrentNewTarget;
        var previousNewTarget = ambientNewTarget ?? restoreNewTarget;
        var instancePrototype = previousNewTarget != null
            ? ResolveInstancePrototype(previousNewTarget)
            : prototype;

        // Installed by the constructor, not by an initializer overwriting it — see the same
        // change in JSFunction's OrdinaryCreateFromConstructor for why the second write cost
        // a process-wide prototype-cache invalidation per `new`. The null branch is kept
        // verbatim: assigning null through the setter clears the chain, whereas passing null
        // to the constructor would substitute %Object.prototype%.
        var @object = instancePrototype != null
            ? new JSObject(instancePrototype)
            : new JSObject() { BasePrototypeObject = instancePrototype };
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

        // And whatever GetSuperConstructor resolves to has to BE a constructor, because
        // `super(...args)` [[Construct]]s it. `Object.setPrototypeOf(C, Math.sin)` leaves a
        // callable that is not one, and the delegate fast path below would then run its [[Call]]
        // as though it were, constructing an instance out of a class whose super() cannot
        // succeed. A class that writes `constructor(){ super(); }` already reports this from
        // JSFunction's own [[Construct]]; the class that writes no constructor at all — which is
        // what a minifier leaves behind when it drops the trivial one — must report it too.
        if (IsBodylessDefaultConstructor && hasHeritage)
        {
            var dynamicSuperConstructor = GetPrototypeOf();
            if (dynamicSuperConstructor == null
                || !JSConstructorOperations.IsConstructor(dynamicSuperConstructor))
            {
                var superName = dynamicSuperConstructor is JSFunction superAsFunction
                    ? superAsFunction.name.Value
                    : "null";
                throw JSEngine.NewTypeError(
                    $"Super constructor {superName} of derived class is not a constructor");
            }
        }

        // A body-less default derived constructor runs `super(...args)`. The delegate
        // fast-path below calls the super constructor's delegate directly, which is only
        // equivalent to [[Construct]] for a plain JSFunction super (its [[Call]] with
        // `this` = @object sets up the instance). A super constructor that is NOT a
        // JSFunction — a Proxy (no construct trap forwards to its target's [[Construct]])
        // or a bound function — has no such delegate, so it must be invoked through its
        // real [[Construct]] via CreateInstance, threading the active new target so the
        // base allocates the most-derived prototype (test262 sm/class/superCallBaseInvoked,
        // proxy default-constructor case). A bound function IS a JSFunction, so it has to be
        // named here rather than fall out of the type test: its own delegate is the [[Call]]
        // that never reaches the target's [[Construct]], and taking it left the instance
        // uninitialised ("Must call super constructor before accessing 'this'").
        if (IsBodylessDefaultConstructor
            && GetPrototypeOf() is { } superCtor
            && !superCtor.IsNull
            && (superCtor is not JSFunction superFunction || superFunction.BoundConstructTarget != null))
        {
            JSValue constructed;
            try
            {
                if (ec != null)
                    ec.CurrentNewTarget = previousNewTarget ?? this;
                using (JSEngine.EnterStrictMode(IsStrictMode))
                    constructed = superCtor.CreateInstance(a);
            }
            finally
            {
                if (ec != null)
                    ec.CurrentNewTarget = restoreNewTarget;
            }

            if (constructed is { IsObject: true })
            {
                constructed.BasePrototypeObject = instancePrototype;
                return constructed;
            }

            return @object;
        }

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
                ec.CurrentNewTarget = restoreNewTarget;
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
