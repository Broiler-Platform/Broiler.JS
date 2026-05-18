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

    private StringSpan source;

    internal JSFunction constructor;
    internal JSFunction BoundTargetFunction;

    public readonly StringSpan name;

    internal JSFunctionDelegate f;
    public bool CoerceThisOnInvoke { get; set; }
    public bool IsStrictMode { get; set; }

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
        ownProperties.Put(KeyStrings.prototype.Key) = JSProperty.Property(KeyStrings.prototype, (IPropertyValue)prototype);

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
        this.source = source.IsEmpty ? $"function {this.name}() {{ [native] }}" : source;

        prototype = _prototype;
        prototype.GetOwnProperties(true).Put(KeyStrings.constructor, this);

        ownProperties.Put(KeyStrings.prototype, prototype);
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
        this.source = source.IsEmpty ? $"function {this.name}() {{ [native] }}" : source;

        if (createPrototype)
        {
            prototype = new JSObject();
            // prototype[KeyStrings.constructor] = this;
            prototype.FastAddValue(KeyStrings.constructor, this, JSPropertyAttributes.ConfigurableValue);
            // ref var opp = ref prototype.GetOwnProperties(true);
            // opp[KeyStrings.constructor.Key] = JSProperty.Property(this, JSPropertyAttributes.ConfigurableReadonlyValue);
            ownProperties.Put(KeyStrings.prototype, prototype, JSPropertyAttributes.ConfigurableValue);
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
        this.source = source.IsEmpty ? $"function {this.name}() {{ [native] }}" : source;

        if (createPrototype)
        {
            prototype = new JSObject();
            // prototype[KeyStrings.constructor] = this;
            prototype.FastAddValue(KeyStrings.constructor, this, JSPropertyAttributes.ConfigurableValue);
            // ref var opp = ref prototype.GetOwnProperties(true);
            // opp[KeyStrings.constructor.Key] = JSProperty.Property(this, JSPropertyAttributes.ConfigurableReadonlyValue);
            ownProperties.Put(KeyStrings.prototype, prototype, JSPropertyAttributes.ConfigurableValue);
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
        ownProperties.Put(KeyStrings.name, JSValue.CreateString(name), JSPropertyAttributes.ReadonlyValue);
        throwTypeError.f = (in Arguments a) => throw JSEngine.NewTypeError(message);
        throwTypeError.PreventExtensions();
        return throwTypeError;
    }

    public override JSValue this[KeyString name]
    {
        get => base[name];
        set
        {
            if (name.Key == KeyStrings.prototype.Key)
                prototype = value as JSObject;

            base[name] = value;
        }
    }

    internal protected override JSValue GetValue(KeyString key, JSValue receiver, bool throwError = true)
    {
        if (prototypeChain == null
            && (JSEngine.Current as IJSExecutionContext)?.FunctionPrototype is JSObject functionPrototype)
        {
            var property = functionPrototype.GetInternalProperty(key, false);
            if (!property.IsEmpty)
                return (receiver ?? this).GetValue(property);
        }

        return base.GetValue(key, receiver, throwError);
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
            if (newTargetValue is IJSFunction newTargetFunction)
                return newTargetFunction.Prototype as JSObject ?? prototype;

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
        var instancePrototype = previousNewTarget != null
            ? ResolveInstancePrototype(previousNewTarget)
            : prototype;

        JSValue obj = new JSObject { BasePrototypeObject = instancePrototype };
        var a1 = a.OverrideThis(obj);
        if (ec != null)
            ec.CurrentNewTarget = previousNewTarget ?? this;

        JSValue r;
        try
        {
            r = f(a1);
        }
        finally
        {
            if (ec != null)
                ec.CurrentNewTarget = previousNewTarget;
        }

        if (r.IsObject)
        {
            r.BasePrototypeObject = instancePrototype;
            return r;
        }

        return obj;
    }

    public JSValue InvokeSuper(in Arguments a)
    {
        var r = f(in a);
        if (r.IsObject)
            return r;

        return a.This;
    }

    public override JSValue InvokeFunction(in Arguments a)
    {
        using var _ = JSEngine.EnterStrictMode(IsStrictMode);
        return f(CoerceThisOnInvoke ? a.OverrideThis(CoerceNonStrictThis(a.This)) : a);
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
        if (a.This is not JSFunction fOriginal)
            throw JSEngine.NewTypeError($"{a.This} is not a function");

        var targetName = fOriginal[KeyStrings.name];
        var boundName = targetName.IsString ? $"bound {targetName.StringValue}" : "bound";
        var copy = a;
        var fx = new JSFunction((in Arguments a2) => { return fOriginal.InvokeFunction(copy.CopyForBind(a2)); })
        {
            // need to set prototypeChain...
            prototypeChain = fOriginal.prototypeChain,
            prototype = fOriginal.prototype,
            constructor = fOriginal.constructor,
            BoundTargetFunction = fOriginal.BoundTargetFunction ?? fOriginal
        };
        fx.FastAddValue(KeyStrings.name, JSValue.CreateString(boundName), JSPropertyAttributes.ConfigurableReadonlyValue);

        return fx;
    }

    [JSPrototypeMethod]
    [JSExport("toString", Length = 0)]
    public new static JSValue ToString(in Arguments a)
    {
        if (a.This is not JSFunction fx)
            throw JSEngine.NewTypeError($"Function.prototype.toString cannot be called with non function");
        
        var source = fx.source;
        if (source.IsEmpty)
            return JSValue.CreateString(string.Empty);

        if (source.Source.Length != source.Length || source.Offset != 0)
            source = source.Value;

        return JSValue.CreateString(source.Source);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static JSValue InvokeSuperConstructor(JSValue newTarget, JSValue super, in Arguments a)
    {
        var target = newTarget;

        var @this = a.This;
        var r = (super as JSFunction).CreateInstance(a.OverrideThis(a.This));
        return r.IsObject ? r : @this;
    }

    [JSExport(IsConstructor = true, Length = 1)]
    internal new static JSValue Constructor(in Arguments args)
    {
        var len = args.Length;
        if (len == 0)
            return new JSFunction(empty, "anonymous", "function anonymous() {\n\n}");

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

        _ = CoreScript.Compile($"function anonymous({parameterText}\n) {{\n{bodyText}\n}}", "internal", codeCache: context?.CodeCache);

        var fx = new JSFunction(empty, "internal", bodyText)
        {
            CoerceThisOnInvoke = true
        };

        // parse and create method...
        var fx1 = CoreScript.Compile(bodyText, "internal", sargs, codeCache: context?.CodeCache);
        fx.f = (in Arguments a) => fx1(a.OverrideThis(CoerceNonStrictThis(a.This)));
        return fx;
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
