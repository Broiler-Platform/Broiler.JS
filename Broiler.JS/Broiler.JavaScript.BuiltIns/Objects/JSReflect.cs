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

    private static bool IsConstructor(JSValue value)
        => JSConstructorOperations.IsConstructor(value);

    [JSExport(Length = 3)]
    public static JSValue Apply(in Arguments a)
    {
        var (target, thisArgument, arguments) = a.Get3();
        if (!IsCallable(target) || target is not JSObject targetObject)
            throw JSEngine.NewTypeError("target is not a function");
        if (arguments is not JSObject)
            throw JSEngine.NewTypeError("argumentsList is not an object");

        return targetObject.InvokeFunction(Arguments.ForApply(thisArgument, arguments));
    }

    [JSExport(Length = 2)]
    public static JSValue Construct(in Arguments a)
    {
        var (target, arguments, newTarget) = a.Get3();
        newTarget = newTarget.IsUndefined ? target : newTarget;

        if (!IsConstructor(target) || target is not JSObject targetObject)
            throw JSEngine.NewTypeError("target is not a constructor");

        if (!IsConstructor(newTarget)
            || newTarget is not JSObject newTargetObject)
        {
            throw JSEngine.NewTypeError("newTarget is not a constructor");
        }
        if (arguments is not JSObject)
            throw JSEngine.NewTypeError("argumentsList is not an object");

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
            throw JSEngine.NewTypeError("Reflect.defineProperty called on non-object");

        var key = propertyKey.ToKey();

        if (attributes is not JSObject userDesc)
            throw JSEngine.NewTypeError("Property description must be an object");

        // ToPropertyDescriptor: resolve inherited / accessor descriptor fields into a
        // fresh own-only record before defining.
        var pd = JSObject.NormalizeDescriptor(userDesc);

        // Dispatch through the JSValue [[DefineOwnProperty]] overload, which exotic
        // objects override (e.g. JSArray routes `length` and integer indices to its
        // own length/index handling). Calling the typed KeyString/uint overloads
        // directly would bypass those overrides — so `Reflect.defineProperty(arr,
        // "length", {value:0})` would not shrink the array, unlike Object.defineProperty.
        var result = key.Type switch
        {
            KeyType.UInt => targetObject.DefineProperty(JSValue.CreateNumber(key.Index), pd),
            KeyType.String => targetObject.DefineProperty(key.KeyString.ToJSValue(), pd),
            KeyType.Symbol => targetObject.DefineProperty(key.Symbol, pd),
            _ => throw JSEngine.NewTypeError($"Cannot define property {propertyKey}")
        };
        return result.IsBoolean ? result : JSBoolean.True;
    }

    [JSExport(Length = 2)]
    public static JSValue DeleteProperty(in Arguments a)
    {
        var (target, propertyKey) = a.Get2();
        if (target is not JSObject @object)
            throw JSEngine.NewTypeError("Reflect.deleteProperty called on non-object");

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

    [JSExport("getPrototypeOf", Length = 1)]
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

        // GetAllKeys yields integer-index then string keys (and, for a Proxy, fires
        // the ownKeys trap which already returns its full string+symbol result).
        var en = @object.GetAllKeys(false, false);
        while (en.MoveNext(out var hasValue, out var value, out var _))
        {
            if (hasValue)
                r.Add(value);
        }

        // [[OwnPropertyKeys]] also lists symbol keys after the string keys. The
        // ordinary GetAllKeys omits them, so append the object's own symbols here.
        // A Proxy's GetAllKeys already included them via the trap, so skip it.
        if (@object is not JSProxy)
        {
            foreach (var (key, property) in @object.GetSymbols().AllValues())
            {
                if (property.IsEmpty)
                    continue;

                var symbol = JSValue.GetSymbolByKeyFactory?.Invoke(key);
                if (symbol != null)
                    r.Add((JSValue)symbol);
            }
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

    [JSExport(Length = 3)]
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

        // §28.1.13: return target.[[SetPrototypeOf]](proto) — the not-extensible
        // and cyclic cases yield false rather than throwing.
        return @object.TrySetPrototypeOf(p, out _) ? JSBoolean.True : JSBoolean.False;
    }
}
