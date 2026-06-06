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
    public JSObject[] CapturedWithObjects { get; set; }

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
            function.CapturedWithObjects = (JSEngine.Current as JSContext)?.CaptureWithScopes();

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
        this.name = name.IsEmpty ? "native" : name;
        this.source = source.IsEmpty ? $"function {this.name}() {{ [native code] }}" : source;

        if (createPrototype)
        {
            prototype = new JSObject();
            // prototype[KeyStrings.constructor] = this;
            prototype.FastAddValue(KeyStrings.constructor, this, JSPropertyAttributes.ConfigurableValue);
            // ref var opp = ref prototype.GetOwnProperties(true);
            // opp[KeyStrings.constructor.Key] = JSProperty.Property(this, JSPropertyAttributes.ConfigurableReadonlyValue);
            ownProperties.Put(KeyStrings.prototype, prototype, JSPropertyAttributes.Value);
        }

        ownProperties.Put(KeyStrings.length, JSValue.CreateNumber(length), JSPropertyAttributes.ConfigurableReadonlyValue);
        ownProperties.Put(KeyStrings.name, name.IsEmpty ? JSValue.CreateString("native") : JSValue.CreateString(name.Value), JSPropertyAttributes.ConfigurableReadonlyValue);

        constructor = this;
    }

    public JSFunction(JSFunctionDelegate f, in StringSpan name, in StringSpan source, int length = 0, bool createPrototype = true) : base((JSEngine.Current as IJSExecutionContext)?.FunctionPrototype)
    {
        ref var ownProperties = ref GetOwnProperties();
        this.f = f;
        this.name = name.IsEmpty ? "native" : name;
        this.source = source.IsEmpty ? $"function {this.name}() {{ [native code] }}" : source;

        if (createPrototype)
        {
            prototype = new JSObject();
            // prototype[KeyStrings.constructor] = this;
            prototype.FastAddValue(KeyStrings.constructor, this, JSPropertyAttributes.ConfigurableValue);
            // ref var opp = ref prototype.GetOwnProperties(true);
            // opp[KeyStrings.constructor.Key] = JSProperty.Property(this, JSPropertyAttributes.ConfigurableReadonlyValue);
            ownProperties.Put(KeyStrings.prototype, prototype, JSPropertyAttributes.Value);
        }

        ownProperties.Put(KeyStrings.length, JSValue.CreateNumber(length), JSPropertyAttributes.ConfigurableReadonlyValue);
        ownProperties.Put(KeyStrings.name, name.IsEmpty ? JSValue.CreateString("native") : JSValue.CreateString(name.Value), JSPropertyAttributes.ConfigurableReadonlyValue);

        constructor = this;
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

        if (prototype == null)
            throw JSEngine.NewTypeError($"{name} is not a constructor");

        var ec = JSEngine.Current as IJSExecutionContext;
        var previousNewTarget = ec?.CurrentNewTarget;
        var deferInstancePrototypeResolution = name.Value is "Promise"
            or "Function"
            or "AsyncFunction"
            or "GeneratorFunction"
            or "AsyncGeneratorFunction";
        var instancePrototype = !deferInstancePrototypeResolution && previousNewTarget != null
            ? ResolveInstancePrototype(previousNewTarget)
            : prototype;

        JSValue obj = new JSObject { BasePrototypeObject = instancePrototype };
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
        using var withScope = context?.PushWithScopes(CapturedWithObjects);
        try
        {
            r = f(a1) ?? JSUndefined.Value;
        }
        finally
        {
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
            if (!IsOrdinaryUserFunction || ReferenceEquals(r, obj))
            {
                if (deferInstancePrototypeResolution && previousNewTarget != null)
                    instancePrototype = ResolveInstancePrototype(previousNewTarget);

                r.BasePrototypeObject = instancePrototype;
            }

            return r;
        }

        return obj;
    }

    public JSValue InvokeSuper(in Arguments a)
    {
        var context = JSEngine.Current as JSContext;
        var scriptHostMode = string.Equals(
            Environment.GetEnvironmentVariable("BROILER_SCRIPT_HOST"),
            "1",
            StringComparison.Ordinal);
        using var suspendedWithScope = scriptHostMode ? context?.SuspendWithScopes() : null;
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
                    current.SetLegacyCaller(previousExecutingFunction);

                using (JSEngine.EnterStrictMode(current.IsStrictMode))
                {
                    var context = JSEngine.Current as JSContext;
                    var scriptHostMode = string.Equals(
                        Environment.GetEnvironmentVariable("BROILER_SCRIPT_HOST"),
                        "1",
                        StringComparison.Ordinal);
                    using var suspendedWithScope = scriptHostMode ? context?.SuspendWithScopes() : null;
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
                            current.SetLegacyCaller(JSValue.NullValue);
                    }
                }

                if (result is not JSTailCall tailCall)
                    return result;

                if (tailCall.Target is not JSFunction jsFunction)
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

    [JSPrototypeMethod]
    [JSExport("valueOf", Length = 1)]
    public new static JSValue ValueOf(in Arguments a) => a.This;

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
        var targetLength = target[KeyStrings.length].DoubleValue;
        var boundArgsLength = Math.Max(a.Length - 1, 0);
        var boundLength = double.IsNaN(targetLength) || targetLength <= 0
            ? 0
            : Math.Max(Math.Floor(targetLength) - boundArgsLength, 0);
        var copy = a;
        var fx = new JSFunction((in Arguments a2) => { return target.InvokeFunction(copy.CopyForBind(a2)); }, StringSpan.Empty, StringSpan.Empty, 0, createPrototype: false)
        {
            prototype = originalFunction?.prototype,
            constructor = originalFunction?.constructor,
            BoundTargetFunction = boundTargetFunction
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
        var r = super.CreateInstance(a.OverrideThis(a.This));
        return r?.IsObject == true ? r : @this ?? new JSObject();
    }

    [JSExport(IsConstructor = true, Length = 1)]
    internal new static JSValue Constructor(in Arguments args)
        => CreateDynamicFunction(in args, "function");

    internal static JSValue CreateDynamicFunction(in Arguments args, string functionKind)
    {
        var len = args.Length;
        if (len == 0)
            return CoreScript.Evaluate($"({functionKind} anonymous() {{\n\n}})", "internal");

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
        var source = $"({functionKind} anonymous({parameterText}\n) {{\n{bodyText}\n}})";
        _ = CoreScript.Compile(source, location ?? "internal", codeCache: null);
        return CoreScript.Evaluate(source, location ?? "internal", context?.CodeCache);
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
