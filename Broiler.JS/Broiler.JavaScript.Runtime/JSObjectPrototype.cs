using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.Storage;

namespace Broiler.JavaScript.Runtime;


public partial class JSObject
{
    [JSExport(IsConstructor = true)]
    public static JSValue Constructor(in Arguments a) 
    {
        if (a.This != null && !a.This.IsUndefined)
            return a.This;

        var first = a.Get1();
        if (first.IsObject)
            return first;

        if (first.IsNullOrUndefined)
            return new JSObject();

        return CreatePrimitiveObject(first);
    }

    [JSPrototypeMethod][JSExport("propertyIsEnumerable")]
    public static JSValue PropertyIsEnumerable(in Arguments a)
    {
        if(!a.This.TryAsObjectThrowIfNullOrUndefined(out var @object))
            return JSValue.BooleanFalse;

        if (a.Length > 0)
        {
            var text = a.Get1().ToString();
            var px = @object.GetInternalProperty(text, false);
            if (!px.IsEmpty && px.IsEnumerable)
                return JSValue.BooleanTrue;
        }

        return JSValue.BooleanFalse;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="a"></param>
    /// <returns></returns>
    [JSPrototypeMethod][JSExport("toString")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "JavaScript Method Signature is Standard")]
    public static JSValue ToString(in Arguments a)
    {
        if (a.This.IsNull)
            return JSValue.CreateString("[object Null]");

        if (a.This.IsUndefined)
            return JSValue.CreateString("[object Undefined]");

        if (a.This.IsArray)
            return JSValue.CreateString("[object Array]");

        var toStringTag = GetGlobalSymbolFactory?.Invoke("toStringTag");
        if (toStringTag != null && a.This is JSObject @object)
        {
            var tag = @object[toStringTag];
            if (tag.IsString)
                return JSValue.CreateString($"[object {tag}]");
        }

        return JSValue.CreateString(a.This?.TypeOf() == JSConstants.Function ? "[object Function]" : "[object Object]");
    }

    [JSPrototypeMethod][JSExport("toLocaleString")]
    public static JSValue ToLocaleString(in Arguments a)
    {
        if (a.This.IsNullOrUndefined)
            throw NewTypeError(Cannot_convert_undefined_or_null_to_object);

        return ToString(in a);
    }

    [JSExport("__proto__")]
    internal JSValue ObjectPrototype
    {
        get => GetPrototypeOf();
        set
        {
            if (!value.IsObject && !value.IsNull)
                return;

            var @object = this;
            @object.SetPrototypeOf(value);
        }
    }

    [JSPrototypeMethod][JSExport("hasOwnProperty")]
    internal static JSValue HasOwnProperty(in Arguments a)
    {
        if (!a.This.TryAsObjectThrowIfNullOrUndefined(out var @object))
            return JSValue.BooleanFalse;

        var first = a.Get1();
        var key = first.ToKey(false);
        if (key.IsUInt)
        {
            ref var elements = ref @object.GetElements();
            ref var property = ref elements.Get(key.Index);
            if (!property.IsEmpty)
                return JSValue.BooleanTrue;

            return JSValue.BooleanFalse;
        }

        if (key.IsSymbol)
        {
            ref var symbols = ref @object.GetSymbols();
            ref var property = ref symbols.GetRefOrDefault(key.Symbol.Key, ref JSProperty.Empty);
            if (!property.IsEmpty)
                return JSValue.BooleanTrue;
            return JSValue.BooleanFalse;
        }

        ref var op = ref @object.GetOwnProperties(false);
        ref var ownProperty = ref op.GetValue(key.KeyString.Key);
        if (!ownProperty.IsEmpty)
            return JSValue.BooleanTrue;

        return JSValue.BooleanFalse;
    }

    [JSPrototypeMethod][JSExport("__defineGetter__", Length = 2)]
    internal static JSValue DefineGetter(in Arguments a)
    {
        if (!a.This.TryAsObjectThrowIfNullOrUndefined(out var @object))
            throw NewTypeError(Cannot_convert_undefined_or_null_to_object);

        var (propertyName, getter) = a.Get2();
        if (getter is not IJSFunction)
            throw NewTypeError("Getter must be a function");

        var descriptor = new JSObject();
        descriptor[KeyStrings.get] = getter;
        descriptor[KeyStrings.enumerable] = JSValue.BooleanTrue;
        descriptor[KeyStrings.configurable] = JSValue.BooleanTrue;
        @object.DefineProperty(propertyName, descriptor);
        return JSUndefined.Value;
    }

    [JSPrototypeMethod]
    [JSExport("valueOf")]
    public static JSValue ValueOf(in Arguments a)
    {
        if (a.This.IsNullOrUndefined)
            throw NewTypeError(Cannot_convert_undefined_or_null_to_object);

        return a.This is JSObject ? a.This : CreatePrimitiveObject(a.This);
    }

    [JSPrototypeMethod][JSExport("isPrototypeOf")]
    internal static JSValue IsPrototypeOf(in Arguments a)
    {
        var first = a.Get1();
        if (!first.IsObject)
            return JSValue.BooleanFalse;

        if (!a.This.TryAsObjectThrowIfNullOrUndefined(out var @this))
            return JSValue.BooleanFalse;

        while (true)
        {
            if (@this == (first.prototypeChain as IJSPrototype)?.Object)
                return JSValue.BooleanTrue;

            if ((first.prototypeChain as IJSPrototype)?.Object == first || (first.prototypeChain as IJSPrototype)?.Object == null)
                break;

            first = (first.prototypeChain as IJSPrototype)?.Object;
        }

        return JSValue.BooleanFalse;
    }
}
