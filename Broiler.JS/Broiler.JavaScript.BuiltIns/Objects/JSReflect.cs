using Broiler.JavaScript.BuiltIns.Array;
using Broiler.JavaScript.BuiltIns.Boolean;
using Broiler.JavaScript.BuiltIns.Null;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.Extensions;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.BuiltIns.Function;
using Broiler.JavaScript.BuiltIns.Proxy;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.Engine.Extensions;

namespace Broiler.JavaScript.BuiltIns.Objects;

[JSClassGenerator("Reflect"), JSInternalObject]
public partial class JSReflect : JSObject
{
    private static bool IsCallable(JSValue value)
        => value is JSObject && value.TypeOf() == JSConstants.Function;

    [JSExport(Length = 3)]
    public static JSValue Apply(in Arguments a)
    {
        var (target, thisArgument, arguments) = a.Get3();
        if (!IsCallable(target) || target is not JSObject targetObject)
            throw JSEngine.NewTypeError("target is not a function");

        return targetObject.InvokeFunction(Arguments.ForApply(thisArgument, arguments));
    }

    [JSExport(Length = 2)]
    public static JSValue Construct(in Arguments a)
    {
        var (target, arguments, newTarget) = a.Get3();
        newTarget = newTarget.IsUndefined ? target : newTarget;

        if (!IsCallable(target) || target is not JSObject targetObject)
            throw JSEngine.NewTypeError("target is not a constructor");

        if (!IsCallable(newTarget)
            || newTarget is not JSObject newTargetObject
            || newTargetObject[KeyStrings.prototype] is not JSObject)
        {
            throw JSEngine.NewTypeError("newTarget is not a constructor");
        }

        var ec = JSEngine.Current as IJSExecutionContext;
        var previousNewTarget = ec?.CurrentNewTarget;

        if (ec != null)
            ec.CurrentNewTarget = newTargetObject;

        try
        {
            return targetObject.CreateInstance(Arguments.ForApply(new JSObject(), arguments));
        }
        finally
        {
            if (ec != null)
                ec.CurrentNewTarget = previousNewTarget;
        }
    }

    [JSExport(Length = 3)]
    public new static JSValue DefineProperty(in Arguments a)
    {
        var (target, propertyKey, attributes) = a.Get3();
        if (target is not JSObject targetObject)
            return JSBoolean.False;

        if (targetObject is not JSProxy && !targetObject.IsExtensible())
            return JSBoolean.False;

        if (attributes is not JSObject pd)
            return JSBoolean.False;

        var result = targetObject.DefineProperty(propertyKey, pd);
        return result.IsBoolean ? result : JSBoolean.True;
    }

    [JSExport(Length = 2)]
    public static JSValue DeleteProperty(in Arguments a)
    {
        var (target, propertyKey) = a.Get2();
        if (target is not JSObject @object)
            return JSBoolean.False;

        var result = @object.Delete(propertyKey);
        return result.IsBoolean ? result : JSBoolean.True;
    }

    [JSExport(Length = 2)]
    public static JSValue Get(in Arguments a)
    {
        var (target, propertyKey, receiver) = a.Get3();
        if (target is not JSObject @object)
            throw JSEngine.NewTypeError($"Not an object");

        receiver = receiver.IsUndefined ? target : receiver;
        return target.GetValue(propertyKey, receiver, false);
    }

    [JSExport(Length = 2)]
    public new static JSValue GetOwnPropertyDescriptor(in Arguments a)
    {
        var (target, propertyKey) = a.Get2();
        if (target is not JSObject @object)
            throw JSEngine.NewTypeError($"Not an object");

        return @object.GetOwnPropertyDescriptor(propertyKey);
    }

    [Static("getPrototypeOf")]
    public new static JSValue GetPrototypeOf(in Arguments a)
    {
        var target = a.Get1();
        if (target is not JSObject @object)
            throw JSEngine.NewTypeError($"Not an object");

        return @object.GetPrototypeOf();
    }

    [JSExport(Length = 2)]
    public static JSValue Has(in Arguments a)
    {
        var (target, propertyKey, _) = a.Get3();
        if (target is not JSObject)
            throw JSEngine.NewTypeError($"Not an object");

        return target.HasProperty(propertyKey);
    }

    [JSExport(Length = 1)]
    public new static JSValue IsExtensible(in Arguments a)
    {
        var target = a.Get1();
        if (target is not JSObject @object)
            throw JSEngine.NewTypeError($"Not an object");
        
        return @object.IsExtensible() ? JSBoolean.True : JSBoolean.False;
    }

    [JSExport(Length = 1)]
    public static JSValue OwnKeys(in Arguments a)
    {
        var target = a.Get1();
        if (target is not JSObject @object)
            throw JSEngine.NewTypeError($"Not an object");
        
        var r = new JSArray();
        var en = @object.GetAllKeys(false, false);
        while (en.MoveNext(out var hasValue, out var value, out var _))
        {
            if (hasValue)
                r.Add(value);
        }

        return r;
    }

    [JSExport(Length = 1)]
    public new static JSValue PreventExtensions(in Arguments a)
    {
        var target = a.Get1();
        if (target is not JSObject @object)
            throw JSEngine.NewTypeError($"Not an object");

        return @object.PreventExtensions() ? JSBoolean.True : JSBoolean.False;
    }

    [JSExport(Length = 2)]
    public static JSValue Set(in Arguments a)
    {
        var (target, propertyKey, value, receiver) = a.Get4();
        if (target is not JSObject @object)
            throw JSEngine.NewTypeError($"Not an object");

        receiver = receiver.IsUndefined ? target : receiver;
        return @object.SetValue(propertyKey, value, receiver, false)
            ? JSBoolean.True
            : JSBoolean.False;
    }

    [JSExport]
    public new static JSValue SetPrototypeOf(in Arguments a)
    {
        var (target, p) = a.Get2();
        if (target is not JSObject @object)
            throw JSEngine.NewTypeError($"Not an object");

        if (!p.IsObject && !p.IsNull)
            throw JSEngine.NewTypeError($"Not an object");

        @object.SetPrototypeOf(p);
        return JSBoolean.True;
    }
}
