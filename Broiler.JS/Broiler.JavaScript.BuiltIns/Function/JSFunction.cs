using System;
using System.Collections.Generic;
using System.ComponentModel;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.BuiltIns.Proxy;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Function;


[JSBaseClass("Object")]
[JSFunctionGenerator("Function", Register = false)]
public partial class JSFunction : JSObject, IPropertyAccessor, IJSFunction
{
    internal static JSFunctionDelegate empty = (in Arguments a) => a.This;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public JSObject prototype;

    /// <summary>
    /// True once this function has been observed to carry a constructor's
    /// <c>prototype</c> object. A function's [[Construct]] capability is fixed at
    /// creation, so assigning a non-object value to the observable
    /// <c>prototype</c> property (which nulls the <see cref="prototype"/> field)
    /// must not demote it from being a constructor. <see cref="JSConstructorOperations"/>
    /// consults this so <c>Reflect.construct(target, args, newTarget)</c> keeps
    /// accepting a <c>newTarget</c> whose <c>prototype</c> was set to a non-object.
    /// </summary>
    internal bool IsConstructable;

    /// <summary>
    /// Updates the cached <see cref="prototype"/> field for a write to the
    /// observable <c>prototype</c> property. If the function currently has a
    /// prototype object it is, by construction, a constructor; record that before
    /// the field is potentially cleared so constructorness survives the write.
    /// </summary>
    private void AssignPrototypeField(JSValue value)
    {
        if (prototype != null)
            IsConstructable = true;

        prototype = value as JSObject;
    }

    private StringSpan source;

    internal JSFunction constructor;
    internal JSValue BoundTargetFunction;

    /// <summary>
    /// For a bound function (created via <c>Function.prototype.bind</c>), the
    /// immediate target whose [[Construct]] is invoked when the bound function is
    /// used with <c>new</c>, together with the bind-time arguments used to build
    /// the bound-arguments prefix. Per the ECMAScript BoundFunction [[Construct]]
    /// semantics, the target is constructed directly (bound <c>this</c> ignored)
    /// rather than going through the bound function's [[Call]] delegate.
    /// </summary>
    internal JSValue BoundConstructTarget;
    internal Arguments BoundConstructArguments;

    public readonly StringSpan name;

    internal JSFunctionDelegate f;
    public bool CoerceThisOnInvoke { get; set; }
    public bool IsStrictMode { get; set; }

    /// <summary>
    /// True for ordinary functions compiled from JavaScript source. When such a
    /// function is used as a constructor and explicitly returns a distinct
    /// object, that object is returned as-is with its own prototype preserved,
    /// per the ECMAScript [[Construct]] semantics. Native built-in constructors
    /// keep this <c>false</c> so the engine applies the newTarget-derived
    /// prototype to the instance they allocate (required for subclassing).
    /// </summary>
    public bool IsOrdinaryUserFunction { get; set; }

    /// <summary>
    /// Whether a tail call targeting this function may be dispatched by the InvokeFunction
    /// fast loop (re-entering the delegate directly). Ordinary functions can; subclasses
    /// whose InvokeFunction override enforces an invariant (e.g. a class constructor's
    /// "cannot be invoked without 'new'" guard) must return false so a tail-positioned
    /// call is routed through the virtual InvokeFunction and the guard still runs.
    /// </summary>
    protected virtual bool SupportsTailCallLoop => true;

    public JSObject[] CapturedWithObjects { get; set; }

    // The `with`-fallback lexical overlays captured when this function was created inside a `with`
    // block, re-established on invocation so a name the with-object lacks (or @@unscopables blocks)
    // still falls through to the enclosing lexical binding (test262 sm/.../unscopables-closures).
    public (JSVariable[] Variables, JSVariable[] Shadowed)[] CapturedWithFallbackScopes { get; set; }

    /// <summary>
    /// True for ordinary non-strict, non-arrow functions that expose the legacy
    /// <c>caller</c>/<c>arguments</c> own data properties. Such functions get
    /// their <c>caller</c> property updated to the currently executing caller
    /// while they are running (web reality behaviour).
    /// </summary>
    internal bool HasLegacyCallerArguments;

    private static readonly KeyString LegacyCallerKey = KeyStrings.GetOrCreate("caller");

    public static JSValue CaptureWithScopes(JSValue functionValue)
    {
        if (functionValue is JSFunction function)
        {
            var context = JSEngine.Current as JSContext;
            function.CapturedWithObjects = context?.CaptureWithScopes();
            function.CapturedWithFallbackScopes = context?.CaptureWithFallbackScopes();
        }

        return functionValue;
    }

    /// <summary>
    /// Adds the legacy, non-strict <c>caller</c> and <c>arguments</c> own data
    /// properties (value <c>null</c>, non-writable, non-enumerable,
    /// non-configurable) that web reality engines define on ordinary
    /// non-strict functions. They shadow the poison-pill accessors inherited
    /// from <c>Function.prototype</c>, so accessing them does not throw.
    /// </summary>
    public JSValue AddLegacyCallerAndArguments()
    {
        FastAddValue(LegacyCallerKey, JSValue.NullValue, JSPropertyAttributes.ReadonlyValue);
        FastAddValue(KeyStrings.arguments, JSValue.NullValue, JSPropertyAttributes.ReadonlyValue);
        HasLegacyCallerArguments = true;
        return this;
    }

    /// <summary>
    /// Updates the legacy <c>caller</c> own property to reflect the function
    /// that invoked this one. Non-strict ordinary callers are exposed as a data
    /// value; strict-mode, native, or absent callers expose <c>null</c>.
    /// </summary>
    /// <remarks>
    /// Per the Annex B.2 forbidden extension, the value observed through
    /// <c>[[Get]]</c>/<c>[[GetOwnProperty]]</c> must never be a strict function.
    /// A strict caller is therefore reported as <c>null</c> rather than via a
    /// throwing %ThrowTypeError% accessor: test262's
    /// <c>forbidden-ext/b2/*-indirect-access-prop-caller</c> tests read the
    /// property directly and require that the access does not throw.
    /// </remarks>
    private void SetLegacyCaller(JSValue callerFunction)
    {
        var value = callerFunction is JSFunction ordinary
            && ordinary.IsOrdinaryUserFunction
            && !ordinary.IsStrictMode
            ? callerFunction
            : JSValue.NullValue;

        FastAddValue(LegacyCallerKey, value, JSPropertyAttributes.ReadonlyValue);
    }

    /// <summary>
    /// Updates the legacy <c>arguments</c> own property to the arguments object of
    /// the in-progress invocation (Annex B web-reality behaviour: a non-strict
    /// function's <c>f.arguments</c> is non-null while <c>f</c> is on the stack).
    /// Reset to <c>null</c> when the invocation completes.
    /// </summary>
    private void SetLegacyArguments(JSValue argumentsObject)
        => FastAddValue(KeyStrings.arguments, argumentsObject, JSPropertyAttributes.ReadonlyValue);

    private static JSObject CreateLegacyArgumentsObject(in Arguments a)
    {
        // A plain (unmapped) arguments-like object: indexed data properties plus a
        // writable, non-enumerable, configurable `length`. Sufficient for the
        // legacy `f.arguments` accessor, which exposes positional arguments.
        var argumentsObject = new JSObject();
        var length = a.Length;
        for (uint i = 0; i < length; i++)
            argumentsObject.FastAddValue(i, a.GetAt((int)i), JSPropertyAttributes.EnumerableConfigurableValue);
        argumentsObject.FastAddValue(KeyStrings.length, JSValue.CreateNumber(length), JSPropertyAttributes.ConfigurableValue);
        return argumentsObject;
    }

    /// <summary>
    /// Gets or sets the underlying <see cref="JSFunctionDelegate"/> that implements
    /// this function's invocation logic. Used by CLR interop to wire constructor
    /// delegates.
    /// </summary>
    public JSFunctionDelegate Delegate
    {
        get => f;
        set => f = value;
    }

    /// <inheritdoc />
    JSValue IJSFunction.Prototype => prototype;

    public override bool IsFunction => true;

    /// <inheritdoc />
    public bool IsAnonymousNamePending { get; set; }

    public override JSValue TypeOf() => JSConstants.Function;


    /// <summary>
    /// Used as specific type constructor.
    /// Accepts any <see cref="JSFunction"/> as the type wrapper
    /// (typically a <c>ClrType</c>) so that the Function subsystem
    /// does not depend on the concrete Clr type.
    /// </summary>
    /// <param name="clrDelegate">The delegate implementing the constructor logic.</param>
    /// <param name="type">
    /// A <see cref="JSFunction"/> whose <see cref="name"/> and
    /// <see cref="prototype"/> are used to configure this function.
    /// </param>
    public JSFunction(JSFunctionDelegate clrDelegate, JSFunction type) : this()
    {
        ref var ownProperties = ref GetOwnProperties();

        f = clrDelegate;
        name = "clr-native";
        source = source.IsEmpty ? $"function {type.name}() {{ [clr-native] }}" : source;
        prototype = type.prototype;

        prototype.FastAddValue(KeyStrings.constructor, type, JSPropertyAttributes.EnumerableConfigurableValue);
        ownProperties.Put(KeyStrings.prototype.Key) = JSProperty.Property(KeyStrings.prototype, (IPropertyValue)prototype, JSPropertyAttributes.Value);

        FastAddValue(KeyStrings.name, name.IsEmpty ? JSValue.CreateString("native") : JSValue.CreateString(name.Value), JSPropertyAttributes.ConfigurableReadonlyValue);
        FastAddValue(KeyStrings.length, JSValue.CreateNumber(0), JSPropertyAttributes.ConfigurableReadonlyValue);

        constructor = this;
    }

    internal void Seal()
    {
        ref var ownProperties = ref GetOwnProperties();
        ownProperties.Update((uint key, ref JSProperty p) =>
        {
            if (p.IsValue)
                p = new JSProperty(key, p.get, p.set, p.value, JSPropertyAttributes.ReadonlyValue);
        });
    }

    protected JSFunction(StringSpan name, StringSpan source, JSObject _prototype) : this()
    {
        ref var ownProperties = ref GetOwnProperties();
        f = empty;
        this.name = name.IsEmpty ? "native" : name;
        this.source = source.IsEmpty ? $"function {this.name}() {{ [native code] }}" : source;

        prototype = _prototype;
        prototype.GetOwnProperties(true).Put(KeyStrings.constructor, this);

            ownProperties.Put(KeyStrings.prototype, prototype, JSPropertyAttributes.Value);
        ownProperties.Put(KeyStrings.length, JSValue.NumberZero, JSPropertyAttributes.ConfigurableReadonlyValue);
        ownProperties.Put(KeyStrings.name, name.IsEmpty ? JSValue.CreateString("native") : JSValue.CreateString(name.Value), JSPropertyAttributes.ConfigurableReadonlyValue);

        constructor = this;
    }

    public JSFunction(JSFunctionDelegate f) : this(f, StringSpan.Empty, StringSpan.Empty) { }

    public JSFunction(Func<JSFunctionDelegate> fx, in StringSpan name) :
        this(empty, in name, StringSpan.Empty) => f = (in Arguments a) => { f = fx(); return f(in a); };

    public JSFunction(JSFunctionDelegate f, in StringSpan name, int length = 0) : this(f, name, StringSpan.Empty, length) { }

    public JSFunction(JSObject basePrototype, JSFunctionDelegate f, in StringSpan name, in StringSpan source, int length = 0, bool createPrototype = true) : base(basePrototype)
    {
        ref var ownProperties = ref GetOwnProperties();
        this.f = f;
        // A user-compiled function carries its source span; per ES2026 §10.2.9 SetFunctionName
        // an anonymous user function's `name` defaults to "" (and the contextual NamedEvaluation
        // rebinds it for `const x = function() {}` etc.). Native functions (no source — a
        // builtin or wrapped CLR delegate) keep the legacy "native" placeholder for the
        // diagnostic "function native() { [native code] }" rendering.
        var publicName = name.IsEmpty
            ? (source.IsEmpty ? "native" : "")
            : name.Value;
        this.name = name.IsEmpty && source.IsEmpty ? "native" : (name.IsEmpty ? StringSpan.Empty : name);
        this.source = source.IsEmpty ? $"function {(this.name.IsEmpty ? "anonymous" : this.name)}() {{ [native code] }}" : source;

        // A user-compiled anonymous function (has a source span, no name) reports
        // name "" yet remains eligible for NamedEvaluation; mark it so the binding /
        // property machinery may still infer its name (HasPlaceholderName).
        IsAnonymousNamePending = name.IsEmpty && !source.IsEmpty;

        // Own-key order per spec: a function's "length" and "name" are installed
        // before its "prototype" (SetFunctionName/SetFunctionLength then
        // MakeConstructor), so getOwnPropertyNames yields [length, name, prototype].
        ownProperties.Put(KeyStrings.length, JSValue.CreateNumber(length), JSPropertyAttributes.ConfigurableReadonlyValue);
        ownProperties.Put(KeyStrings.name, JSValue.CreateString(publicName), JSPropertyAttributes.ConfigurableReadonlyValue);

        if (createPrototype)
        {
            prototype = new JSObject();
            prototype.FastAddValue(KeyStrings.constructor, this, JSPropertyAttributes.ConfigurableValue);
            ownProperties.Put(KeyStrings.prototype, prototype, JSPropertyAttributes.Value);
        }

        constructor = this;
    }

    public JSFunction(JSFunctionDelegate f, in StringSpan name, in StringSpan source, int length = 0, bool createPrototype = true) : base((JSEngine.Current as IJSExecutionContext)?.FunctionPrototype)
    {
        ref var ownProperties = ref GetOwnProperties();
        this.f = f;
        // See the other constructor above: anonymous user functions report name "" per
        // SetFunctionName, while native functions keep the "native" placeholder so
        // Function.prototype.toString renders as "function native() { [native code] }".
        var publicName = name.IsEmpty
            ? (source.IsEmpty ? "native" : "")
            : name.Value;
        this.name = name.IsEmpty && source.IsEmpty ? "native" : (name.IsEmpty ? StringSpan.Empty : name);
        this.source = source.IsEmpty ? $"function {(this.name.IsEmpty ? "anonymous" : this.name)}() {{ [native code] }}" : source;

        // See the other constructor: a user-compiled anonymous function stays eligible
        // for NamedEvaluation even though it reports name "".
        IsAnonymousNamePending = name.IsEmpty && !source.IsEmpty;

        // Own-key order per spec: [length, name, prototype] (see above).
        ownProperties.Put(KeyStrings.length, JSValue.CreateNumber(length), JSPropertyAttributes.ConfigurableReadonlyValue);
        ownProperties.Put(KeyStrings.name, JSValue.CreateString(publicName), JSPropertyAttributes.ConfigurableReadonlyValue);

        if (createPrototype)
        {
            prototype = new JSObject();
            prototype.FastAddValue(KeyStrings.constructor, this, JSPropertyAttributes.ConfigurableValue);
            ownProperties.Put(KeyStrings.prototype, prototype, JSPropertyAttributes.Value);
        }

        constructor = this;
    }

    // Returns the per-realm %ThrowTypeError% intrinsic, creating and caching it on
    // first use. A single shared object is required so that the get and set of an
    // unmapped arguments object's "callee" accessor — and the accessors across every
    // such arguments object in the realm — are the same function under SameValue
    // (test262 ThrowTypeError/unique-per-realm-*).
    public static JSFunction GetOrCreateThrowTypeError()
    {
        if (JSEngine.Current is IJSExecutionContext context)
        {
            if (context.ThrowTypeError is JSFunction cached)
                return cached;

            var created = CreateFrozenThrowTypeErrorFunction("ThrowTypeError",
                "'caller', 'callee', and 'arguments' properties may not be accessed on strict mode functions or the arguments objects for calls to them");
            context.ThrowTypeError = created;
            return created;
        }

        return CreateFrozenThrowTypeErrorFunction("ThrowTypeError",
                "'caller', 'callee', and 'arguments' properties may not be accessed on strict mode functions or the arguments objects for calls to them");
    }

    public static JSFunction CreateFrozenThrowTypeErrorFunction(string name, string message)
    {
        var throwTypeError = new JSFunction(
            empty,
            name,
            $"function {name}() {{ [native code] }}",
            length: 0,
            createPrototype: false);

        ref var ownProperties = ref throwTypeError.GetOwnProperties();
        ownProperties.Put(KeyStrings.length, JSValue.NumberZero, JSPropertyAttributes.ReadonlyValue);
        ownProperties.Put(KeyStrings.name, JSValue.CreateString(string.Empty), JSPropertyAttributes.ReadonlyValue);
        throwTypeError.f = (in Arguments a) => throw JSEngine.NewTypeError(message);
        throwTypeError.PreventExtensions();
        return throwTypeError;
    }

    internal void SetNameProperty(string name, JSPropertyAttributes attributes = JSPropertyAttributes.ConfigurableReadonlyValue)
        => GetOwnProperties().Put(KeyStrings.name, JSValue.CreateString(name), attributes);

    // Override the source text reported by Function.prototype.toString. Used by the
    // dynamic Function constructor, which builds the function from an *anonymous*
    // function expression (so no self-name binding is created in the body) but must
    // still toString as `function anonymous(...) { ... }` per CreateDynamicFunction.
    internal void OverrideSource(in StringSpan source) => this.source = source;

    // The raw source span backing Function.prototype.toString. Exposed so a wrapper
    // function (e.g. an async function built around a generator) can adopt the
    // underlying function's source text instead of reporting "[native code]".
    internal StringSpan SourceSpan => source;

    public override JSValue this[KeyString name]
    {
        get => base[name];
        set
        {
            if (name.Key == KeyStrings.prototype.Key)
                AssignPrototypeField(value);

            base[name] = value;
        }
    }

    internal protected override JSValue GetValue(KeyString key, JSValue receiver, bool throwError = true)
    {
        if (prototypeChain == null
            && (JSEngine.Current as IJSExecutionContext)?.FunctionPrototype is JSObject functionPrototype)
        {
            // Check own properties first so they aren't shadowed by
            // Function.prototype's own properties (e.g. length = 0).
            var ownProp = GetInternalProperty(key, false);
            if (!ownProp.IsEmpty)
                return (receiver ?? this).GetValue(ownProp);

            var property = functionPrototype.GetInternalProperty(key, false);
            if (!property.IsEmpty)
                return (receiver ?? this).GetValue(property);
        }

        return base.GetValue(key, receiver, throwError);
    }

    internal protected override bool SetValue(KeyString name, JSValue value, JSValue receiver, bool throwError = true)
    {
        if (prototypeChain == null
            && GetInternalProperty(name, false).IsEmpty
            && !ReferenceEquals(receiver as JSObject ?? this, (JSEngine.Current as IJSExecutionContext)?.FunctionPrototype)
            && (JSEngine.Current as IJSExecutionContext)?.FunctionPrototype is JSObject functionPrototype)
        {
            var property = functionPrototype.GetInternalProperty(name, false);
            if (!property.IsEmpty)
            {
                var inheritedResult = functionPrototype.SetValue(name, value, receiver ?? this, throwError);
                if (inheritedResult && name.Key == KeyStrings.prototype.Key && ReferenceEquals(receiver as JSObject ?? this, this))
                    AssignPrototypeField(value);

                return inheritedResult;
            }
        }

        var result = base.SetValue(name, value, receiver, throwError);
        if (result && name.Key == KeyStrings.prototype.Key && ReferenceEquals(receiver as JSObject ?? this, this))
            AssignPrototypeField(value);

        return result;
    }

    public override JSValue DefineProperty(in KeyString name, JSObject pd)
    {
        var result = base.DefineProperty(name, pd);
        if (result.BooleanValue
            && name.Key == KeyStrings.prototype.Key
            && pd.HasProperty(KeyStrings.value.ToJSValue()).BooleanValue)
        {
            AssignPrototypeField(pd[KeyStrings.value]);
        }
        else if (HasLegacyCallerArguments
            && (name.Key == LegacyCallerKey.Key || name.Key == KeyStrings.arguments.Key)
            // [[DefineOwnProperty]] reports success as undefined (the abstract-operation
            // value) and failure as the boolean false, so a successful redefine is "not
            // the false boolean" rather than a truthy result.
            && !(result.IsBoolean && !result.BooleanValue))
        {
            // Once script explicitly redefines the legacy "caller"/"arguments" own
            // property, stop the engine's live per-invocation updates of it. The
            // [[GetOwnProperty]] invariant requires a non-writable, non-configurable
            // data property's value to stay constant, so a frozen "caller"/"arguments"
            // must not change to the running caller/arguments object while the function
            // is on the stack (test262 Object/internals/DefineOwnProperty/
            // consistent-value-function-caller and -arguments). Functions whose property
            // is left untouched keep the web-reality dynamic behaviour.
            HasLegacyCallerArguments = false;
        }

        return result;
    }

    internal override JSFunctionDelegate GetMethod(in KeyString key)
    {
        var method = base.GetMethod(in key);
        if (method != null || prototypeChain != null)
            return method;

        if ((JSEngine.Current as IJSExecutionContext)?.FunctionPrototype is JSObject functionPrototype)
            return functionPrototype.GetMethod(in key);

        return null;
    }

    public override string ToDetailString() => source.Value;
    public override JSValue CreateInstance(in Arguments a)
    {
        // BoundFunction [[Construct]]: construct the (immediate) target directly
        // with boundArgs ++ args, ignoring the bound `this`.
        if (BoundConstructTarget != null)
        {
            if (!JSConstructorOperations.IsConstructor(this))
                throw JSEngine.NewTypeError($"{name} is not a constructor");

            // Spec step: "If SameValue(F, newTarget), set newTarget to target." So
            // `new BF()` (whose default newTarget is the bound function itself) and
            // Reflect.construct(BF, args, BF) build the target with newTarget = the
            // bound target — while an explicit, different newTarget is preserved.
            // Done here (not only at the `new` site) so Reflect.construct gets it too.
            var boundEc = JSEngine.Current as IJSExecutionContext;
            var savedNewTarget = boundEc?.CurrentNewTarget;
            if (boundEc != null && (savedNewTarget == null || ReferenceEquals(savedNewTarget, this)))
                boundEc.CurrentNewTarget = BoundConstructTarget;
            try
            {
                return BoundConstructTarget.CreateInstance(BoundConstructArguments.CopyForBind(a));
            }
            finally
            {
                if (boundEc != null)
                    boundEc.CurrentNewTarget = savedNewTarget;
            }
        }

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

        if (!JSConstructorOperations.IsConstructor(this))
            throw JSEngine.NewTypeError($"{name} is not a constructor");

        var ec = JSEngine.Current as IJSExecutionContext;
        var previousNewTarget = ec?.CurrentNewTarget;
        var deferInstancePrototypeResolution = name.Value is "Promise"
            or "Function"
            or "AsyncFunction"
            or "GeneratorFunction"
            or "AsyncGeneratorFunction"
            // These validate their arguments (byteLength vs maxByteLength / byteOffset vs buffer
            // length) and throw a RangeError BEFORE the object is created, so NewTarget.prototype —
            // whose getter is observable — must not be read until after a successful construction.
            or "ArrayBuffer"
            or "SharedArrayBuffer"
            or "DataView"
            // The TypedArray constructors likewise coerce a non-object first argument with
            // ToIndex (§23.2.5.1 step 6.c.ii) before AllocateTypedArray reads NewTarget.prototype,
            // so e.g. `Reflect.construct(Int8Array, [Symbol()], nt)` must throw the ToIndex
            // TypeError without ever evaluating nt's prototype getter.
            or "Int8Array"
            or "Uint8Array"
            or "Uint8ClampedArray"
            or "Int16Array"
            or "Uint16Array"
            or "Int32Array"
            or "Uint32Array"
            or "Float16Array"
            or "Float32Array"
            or "Float64Array"
            or "BigInt64Array"
            or "BigUint64Array"
            // RegExp captures pattern.[[OriginalSource]] / [[OriginalFlags]] (§22.2.4
            // step 5) BEFORE RegExpAlloc reads NewTarget.prototype (step 8). Resolving
            // the prototype here fires the user prototype getter first, letting an
            // ill-behaving subclass observe — or, via re.compile, mutate — the source
            // before it is captured (test262 sm/RegExp/constructor-ordering). Deferring
            // makes the JSRegExp ctor responsible for reading NewTarget.prototype after
            // its source capture, which is the spec-compliant order.
            or "RegExp";
        var instancePrototype = !deferInstancePrototypeResolution && previousNewTarget != null
            ? ResolveInstancePrototype(previousNewTarget)
            : prototype;

        // OrdinaryCreateFromConstructor: when the resolved prototype is not an
        // object (the function's `prototype` property was overwritten with a
        // primitive), the new instance falls back to %Object.prototype% — which a
        // default `new JSObject()` already adopts.
        JSValue obj = instancePrototype != null
            ? new JSObject { BasePrototypeObject = instancePrototype }
            : new JSObject();
        var a1 = a.OverrideThis(obj);
        if (ec != null)
            ec.CurrentNewTarget = previousNewTarget ?? this;

        JSValue r;
        var context = JSEngine.Current as JSContext;
        var scriptHostMode = string.Equals(
            Environment.GetEnvironmentVariable("BROILER_SCRIPT_HOST"),
            "1",
            StringComparison.Ordinal);
        using var suspendedWithScope = scriptHostMode ? context?.SuspendWithScopes() : null;
        using var withFallback = context?.PushWithFallbackScopes(CapturedWithFallbackScopes);
        using var withScope = context?.PushWithScopes(CapturedWithObjects);

        // Track the executing function across [[Construct]] just as [[Call]] does,
        // so a function invoked from within this constructor's body observes this
        // function as its (legacy) caller. Without this, the strict-mode poison of
        // `.caller` cannot be reached for a callee invoked from a constructor body.
        var previousExecutingFunction = JSEngine.ExecutingFunction;
        JSEngine.ExecutingFunction = this;
        var trackLegacyCaller = HasLegacyCallerArguments;
        if (trackLegacyCaller)
        {
            SetLegacyCaller(previousExecutingFunction);
            SetLegacyArguments(CreateLegacyArgumentsObject(a1));
        }
        try
        {
            // [[Construct]] must run the body under its own strict-mode setting,
            // mirroring [[Call]] in InvokeFunction: a strict function constructed via
            // `new` performs strict property [[Set]] semantics (the runtime strict
            // flag is read by JSValue's set accessors through IsStrictModeEnabled).
            using (JSEngine.EnterStrictMode(IsStrictMode))
                r = f(a1) ?? JSUndefined.Value;
        }
        finally
        {
            if (trackLegacyCaller)
            {
                SetLegacyCaller(JSValue.NullValue);
                SetLegacyArguments(JSValue.NullValue);
            }
            JSEngine.ExecutingFunction = previousExecutingFunction;
            if (ec != null)
                ec.CurrentNewTarget = previousNewTarget;
        }

        if (r.IsObject)
        {
            // An ordinary (user-defined) function that explicitly returns a
            // distinct object yields that object as-is, preserving its own
            // prototype. Native built-in constructors instead allocate their
            // exotic instance here and need the newTarget-derived prototype
            // applied to support subclassing.
            //
            // The one exception is a native ctor that, when invoked directly
            // (not subclassed), returns a *boxed primitive* — e.g. Object(value)
            // performing ToObject(value) into a Number/String/Boolean/Symbol
            // wrapper. That wrapper already carries the correct, type-specific
            // prototype (%Number.prototype% etc.); overwriting it with this
            // function's own .prototype (%Object.prototype%) would make
            // `new Object(1).constructor` resolve to Object instead of Number.
            var subclassing = previousNewTarget != null && !ReferenceEquals(previousNewTarget, this);
            // When not subclassing, a native constructor that returns an object which already carries its
            // own correct prototype must keep it rather than have this function's .prototype
            // (%Object.prototype%) forced onto it. This covers a boxed primitive (Number/String/Boolean
            // wrapper or a Symbol object) and `Object(value)` performing ToObject, which returns the
            // *existing* argument object unchanged — clobbering its prototype would corrupt it (e.g.
            // resetting a passed-in Date's prototype, or making `new Object(1).constructor` → Object).
            // A freshly-allocated exotic instance (typed arrays, etc.) is returned distinct from the
            // argument list and still receives the newTarget-derived prototype below.
            var keepsOwnPrototype = !subclassing
                && (r is JSPrimitiveObject || r is Broiler.JavaScript.BuiltIns.Symbol.JSSymbolObject
                    || ReturnedAnInputArgument(r, a));
            if ((!IsOrdinaryUserFunction && !keepsOwnPrototype) || ReferenceEquals(r, obj))
            {
                if (deferInstancePrototypeResolution && previousNewTarget != null)
                    instancePrototype = ResolveInstancePrototype(previousNewTarget);

                r.BasePrototypeObject = instancePrototype;
            }

            return r;
        }

        return obj;
    }

    // True when the constructor returned one of its own (positional) arguments — i.e. an object it did
    // not allocate, such as `Object(existingObject)` returning that object via ToObject. Such a
    // passthrough already has its own prototype and must not have the constructor's .prototype forced
    // onto it.
    private static bool ReturnedAnInputArgument(JSValue result, in Arguments a)
    {
        for (var i = 0; i < a.Length; i++)
            if (ReferenceEquals(a.GetAt(i), result))
                return true;

        return false;
    }

    public JSValue InvokeSuper(in Arguments a)
    {
        var context = JSEngine.Current as JSContext;
        var scriptHostMode = string.Equals(
            Environment.GetEnvironmentVariable("BROILER_SCRIPT_HOST"),
            "1",
            StringComparison.Ordinal);
        using var suspendedWithScope = scriptHostMode ? context?.SuspendWithScopes() : null;
        using var withFallback = context?.PushWithFallbackScopes(CapturedWithFallbackScopes);
        using var withScope = context?.PushWithScopes(CapturedWithObjects);
        JSValue r;
        try
        {
            r = f(in a) ?? JSUndefined.Value;
        }
        catch (NullReferenceException ex)
        {
            throw JSEngine.NewReferenceError(ex.Message);
        }
        if (r.IsObject)
            return r;

        return a.This;
    }

    public override JSValue InvokeFunction(in Arguments a)
    {
        var previousExecutingFunction = JSEngine.ExecutingFunction;
        var current = this;
        var currentArguments = a;
        try
        {
            while (true)
            {
                JSValue result;
                JSEngine.ExecutingFunction = current;

                var trackLegacyCaller = current.HasLegacyCallerArguments;
                if (trackLegacyCaller)
                {
                    current.SetLegacyCaller(previousExecutingFunction);
                    current.SetLegacyArguments(CreateLegacyArgumentsObject(currentArguments));
                }

                using (JSEngine.EnterStrictMode(current.IsStrictMode))
                {
                    var context = JSEngine.Current as JSContext;
                    var scriptHostMode = string.Equals(
                        Environment.GetEnvironmentVariable("BROILER_SCRIPT_HOST"),
                        "1",
                        StringComparison.Ordinal);
                    using var suspendedWithScope = scriptHostMode ? context?.SuspendWithScopes() : null;
                    using var withFallback = context?.PushWithFallbackScopes(current.CapturedWithFallbackScopes);
                    using var withScope = context?.PushWithScopes(current.CapturedWithObjects);
                    try
                    {
                        result = current.f(current.CoerceThisOnInvoke ? currentArguments.OverrideThis(CoerceNonStrictThis(currentArguments.This)) : currentArguments) ?? JSUndefined.Value;
                    }
                    catch (NullReferenceException ex)
                    {
                        throw JSEngine.NewReferenceError(ex.Message);
                    }
                    finally
                    {
                        if (trackLegacyCaller)
                        {
                            current.SetLegacyCaller(JSValue.NullValue);
                            current.SetLegacyArguments(JSValue.NullValue);
                        }
                    }
                }

                if (result is not JSTailCall tailCall)
                    return result;

                // The fast loop dispatches a tail call by re-entering the target's
                // delegate (current.f) directly, bypassing its InvokeFunction override.
                // Subclasses that override InvokeFunction to enforce an invariant — a
                // class constructor's "cannot be invoked without 'new'" guard — must not
                // be shortcut this way, or a tail-positioned call (`() => SomeClass()`)
                // would skip the guard. Route those through the virtual InvokeFunction.
                if (tailCall.Target is not JSFunction jsFunction || !jsFunction.SupportsTailCallLoop)
                    return tailCall.Target.InvokeFunction(tailCall.Arguments);

                // The function that produced this tail call becomes the caller
                // of the tail-called function, mirroring the stack-frame based
                // legacy caller semantics of reference engines.
                previousExecutingFunction = current;
                current = jsFunction;
                currentArguments = tailCall.Arguments;
            }
        }
        finally
        {
            JSEngine.ExecutingFunction = previousExecutingFunction;
        }
    }

    /// <summary>
    /// Invokes this function's compiled body for a native callback site (e.g.
    /// Array.prototype.map/filter/forEach) and resolves any proper-tail-call
    /// sentinel it returns. A raw <c>f(a)</c> call does not trampoline tail
    /// calls, so under BROILER_SCRIPT_HOST a callback ending in <c>return g()</c>
    /// would otherwise leak a <see cref="JSTailCall"/> object to the native
    /// caller instead of g()'s actual result.
    /// </summary>
    internal JSValue InvokeCallback(in Arguments a) =>
        JSTailCall.Resolve((CoerceThisOnInvoke ? f(a.OverrideThis(CoerceNonStrictThis(a.This))) : f(in a)) ?? JSUndefined.Value);

    // Function.prototype has no own `valueOf`: per the spec it simply inherits
    // %Object.prototype.valueOf%, so `Function.prototype.hasOwnProperty("valueOf")`
    // must be false (test262: built-ins/Function/prototype/S15.3.4_A4).

    [JSPrototypeMethod]
    [JSExport("call", Length = 1)]
    public static JSValue Call(in Arguments a)
    {
        var a1 = a.CopyForCall();
        return a.This.InvokeFunction(a1);
    }

    [JSPrototypeMethod]
    [JSExport("apply", Length = 2)]
    public static JSValue Apply(in Arguments a)
    {
        var ar = a.CopyForApply();
        return a.This.InvokeFunction(ar);
    }

    [JSPrototypeMethod]
    [JSExport("bind", Length = 1)]
    public static JSValue Bind(in Arguments a)
    {
        if (!a.This.IsFunction)
            throw JSEngine.NewTypeError("Bind target is not a function");

        var target = a.This;
        var originalFunction = target as JSFunction;
        var boundTargetFunction = originalFunction?.BoundTargetFunction != null && !originalFunction.BoundTargetFunction.IsUndefined
            ? originalFunction.BoundTargetFunction
            : target;
        var targetName = target[KeyStrings.name];
        var boundName = $"bound {(targetName.IsString ? targetName.StringValue : string.Empty)}";
        // SetFunctionLength: the bound length is derived from the target's "length" only when it is an
        // OWN property (HasOwnProperty) whose value is a Number; a missing-own or non-Number (Symbol,
        // string, …) length is ignored and the bound length defaults to 0. Reading it must not attempt a
        // ToNumber coercion that would throw (e.g. on a Symbol), and an inherited "length" must not leak.
        var boundArgsLength = Math.Max(a.Length - 1, 0);
        double boundLength = 0;
        if (target is JSObject targetObject
            && !targetObject.GetOwnPropertyDescriptor(KeyStrings.length.ToJSValue()).IsUndefined)
        {
            var targetLengthValue = target[KeyStrings.length];
            if (targetLengthValue.IsNumber)
            {
                var targetLength = targetLengthValue.DoubleValue;
                boundLength = double.IsNaN(targetLength) || targetLength <= 0
                    ? 0
                    : Math.Max(Math.Floor(targetLength) - boundArgsLength, 0);
            }
        }
        var copy = a;
        var fx = new JSFunction((in Arguments a2) => { return target.InvokeFunction(copy.CopyForBind(a2)); }, StringSpan.Empty, StringSpan.Empty, 0, createPrototype: false)
        {
            prototype = originalFunction?.prototype,
            constructor = originalFunction?.constructor,
            BoundTargetFunction = boundTargetFunction,
            BoundConstructTarget = target,
            BoundConstructArguments = copy
        };
        if (originalFunction != null)
            fx.prototypeChain = originalFunction.prototypeChain;
        ref var ownProperties = ref fx.GetOwnProperties();
        ownProperties.Put(KeyStrings.name, JSValue.CreateString(boundName), JSPropertyAttributes.ConfigurableReadonlyValue);
        ownProperties.Put(KeyStrings.length, JSValue.CreateNumber(boundLength), JSPropertyAttributes.ConfigurableReadonlyValue);

        return fx;
    }

    [JSPrototypeMethod]
    [JSExport("toString", Length = 0)]
    public new static JSValue ToString(in Arguments a)
    {
        if (a.This is not JSFunction fx)
        {
            // Per spec (Function.prototype.toString), any callable object (e.g. a Proxy whose
            // target is callable) returns an implementation-defined NativeFunction representation.
            if (a.This is JSObject { IsFunction: true })
                return JSValue.CreateString("function () { [native code] }");

            throw JSEngine.NewTypeError($"Function.prototype.toString cannot be called with non function");
        }
        
        var source = fx.source;
        if (source.IsEmpty)
            return JSValue.CreateString(string.Empty);

        if (source.Source.Length != source.Length || source.Offset != 0)
            source = source.Value;

        // Function.prototype.toString must NOT normalise line terminator sequences:
        // a function whose source was written with CR or CRLF line endings toStrings
        // with those exact characters preserved (test262
        // Function/prototype/toString/line-terminator-normalisation-*). Return the
        // verbatim source text.
        return JSValue.CreateString(source.Source);
    }

    /// <summary>
    /// Throws the TypeError a derived class constructor produces when its body
    /// returns a value that is neither an object nor <c>undefined</c>
    /// (OrdinaryCallEvaluateBody / [[Construct]] step 13c). Declared to return
    /// <see cref="JSValue"/> so it can sit in a value-producing conditional arm.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static JSValue ThrowDerivedConstructorReturnTypeError()
        => throw JSEngine.NewTypeError("Derived constructors may only return object or undefined");

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static JSValue InvokeSuperConstructor(JSValue newTarget, JSValue super, in Arguments a)
    {
        var @this = a.This;

        // The active new target (the most-derived constructor of the `new`
        // expression) was moved onto the running call-stack item when this
        // derived constructor's body began executing, which cleared
        // JSContext.CurrentNewTarget (see CallStackItem). Restore it for the
        // superclass [[Construct]] so the instance super() allocates carries the
        // most-derived prototype and `new.target` observed inside the superclass
        // is the original new target — not the immediate superclass.
        //
        // When the caller threads in the lexically-captured new.target (non-null), use
        // it: a super() invoked from inside an arrow function runs on the arrow's own
        // call-stack item, which carries no new target, so JSEngine.NewTarget would be
        // undefined there and the superclass [[Construct]] would allocate with the
        // immediate superclass's prototype (so the instance is not `instanceof` the
        // derived class). The lexical new.target is inherited correctly across arrows.
        var ec = JSEngine.Current as IJSExecutionContext;
        var activeNewTarget = newTarget is { } nt && !nt.IsNullOrUndefined ? newTarget : JSEngine.NewTarget;
        var previousNewTarget = ec?.CurrentNewTarget;
        if (ec != null && activeNewTarget != null)
            ec.CurrentNewTarget = activeNewTarget;

        try
        {
            var r = super.CreateInstance(a.OverrideThis(a.This));
            return r?.IsObject == true ? r : @this ?? new JSObject();
        }
        finally
        {
            if (ec != null)
                ec.CurrentNewTarget = previousNewTarget;
        }
    }

    [JSExport(IsConstructor = true, Length = 1)]
    internal new static JSValue Constructor(in Arguments args)
        => CreateDynamicFunction(in args, "function");

    internal static JSValue CreateDynamicFunction(in Arguments args, string functionKind)
    {
        var len = args.Length;
        if (len == 0)
        {
            // Build from an anonymous function expression so the body gets no
            // `anonymous` self-binding (CreateDynamicFunction uses OrdinaryFunctionCreate,
            // not a named function expression), then stamp the spec name/source.
            var emptyFn = CoreScript.Evaluate($"({functionKind} () {{\n\n}})", "internal");
            return FinalizeDynamicFunction(emptyFn, $"{functionKind} anonymous(\n) {{\n\n}}");
        }

        JSValue body = null;
        var al = args.Length;
        var last = al - 1;
        var sargs = new List<string>();
        
        for (var ai = 0; ai < al; ai++)
        {
            var item = args.GetAt(ai);

            if (ai == last)
            {
                body = item;
            }
            else
            {
                sargs.Add(item.ToString());
            }
        }

        var bodyText = body.IsString ? body.StringValue : body.ToString();
        string location = null;
        var context = JSEngine.Current as IJSExecutionContext;
        context?.DispatchEvalEvent(ref bodyText, ref location);
        var parameterText = string.Join(",", sargs);

        // The function is built from an *anonymous* function expression: the spec's
        // CreateDynamicFunction uses OrdinaryFunctionCreate (no self-name binding),
        // so the body must not see an `anonymous` binding (`new Function("return
        // typeof anonymous")()` === "undefined"). The name "anonymous" and the
        // toString source are stamped on afterward.
        var source = $"({functionKind} ({parameterText}\n) {{\n{bodyText}\n}})";
        var specSource = $"{functionKind} anonymous({parameterText}\n) {{\n{bodyText}\n}}";

        // §20.2.1.1.1 CreateDynamicFunction parses the parameter text and the body
        // text separately (as FormalParameters and FunctionBody) before assembling
        // the whole function. Validating only the concatenated source lets a comment,
        // template or stray `)` in the parameters escape into the body (or vice
        // versa) and close the parameter list early — e.g. `new Function("/*", "*/){")`
        // would otherwise parse as a valid empty function. Compile each part on its
        // own (against the same assembled shape, with the other part empty) so such an
        // injection surfaces as a SyntaxError.
        var loc = location ?? "internal";
        _ = CoreScript.Compile($"({functionKind} ({parameterText}\n) {{\n\n}})", loc, codeCache: null);
        _ = CoreScript.Compile($"({functionKind} (\n) {{\n{bodyText}\n}})", loc, codeCache: null);

        _ = CoreScript.Compile(source, loc, codeCache: null);
        return FinalizeDynamicFunction(CoreScript.Evaluate(source, loc, context?.CodeCache), specSource);
    }

    // Stamp the spec name ("anonymous") and toString source onto a freshly built
    // dynamic function (see CreateDynamicFunction). The function itself is anonymous,
    // so SetFunctionName is observable as the "anonymous" name property.
    private static JSValue FinalizeDynamicFunction(JSValue function, string specSource)
    {
        if (function is JSFunction jsFunction)
        {
            jsFunction.SetNameProperty("anonymous");
            jsFunction.OverrideSource(specSource);
        }

        return function;
    }

    internal static JSValue CoerceNonStrictThis(JSValue value)
    {
        if (value == null || value.IsNullOrUndefined)
            return JSEngine.CurrentContext as JSValue ?? JSUndefined.Value;

        if (value.IsObject)
            return value;

        return JSObject.CreatePrimitiveObject(value);
    }

    public override bool ConvertTo(Type type, out object value)
    {
        if (typeof(Delegate).IsAssignableFrom(type))
        {
            // create delegate....
            value = CreateClrDelegate(type, this);
            return true;
        }

        if (type.IsAssignableFrom(typeof(JSFunction)))
        {
            value = this;
            return true;
        }

        if (type == typeof(object))
        {
            value = this;
            return true;
        }

        return base.ConvertTo(type, out value);
    }

    internal static Func<Type, IJSFunction, object> CreateClrDelegateFactory;

    static object CreateClrDelegate(Type type, JSFunction function)
    {
        if (CreateClrDelegateFactory == null)
            throw new InvalidOperationException("CreateClrDelegateFactory not initialized. The Broiler.JavaScript.LinqExpressions assembly must be loaded before calling CreateClrDelegate.");
        return CreateClrDelegateFactory(type, function);
    }
}
