using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.Storage;

namespace Broiler.JavaScript.Runtime;


public partial class JSObject
{
    [JSExport(IsConstructor = true)]
    public static JSValue Constructor(in Arguments a)
    {
        // ES2024 §20.1.1.1 Object ( [ value ] ): when a value argument is
        // supplied it is returned via ToObject(value) — an object (including a
        // function) is returned unchanged, a primitive is boxed — regardless of
        // whether Object was invoked with `new`. The freshly-constructed `this`
        // is only used for the no-argument (and subclass) case so that the
        // correct prototype is preserved.
        var first = a.Get1();

        if (first.IsObject)
            return first;

        if (first.IsNullOrUndefined)
        {
            if (a.This != null && !a.This.IsUndefined)
                return a.This;

            return new JSObject();
        }

        return CreatePrimitiveObject(first);
    }

    [JSPrototypeMethod][JSExport("propertyIsEnumerable")]
    public static JSValue PropertyIsEnumerable(in Arguments a)
    {
        // §20.1.3.4 step 1 is `Let P be ? ToPropertyKey(V)`, performed BEFORE
        // `Let O be ? ToObject(this value)` (step 2). A throwing key coercion must
        // therefore surface before the receiver is rejected — e.g.
        // propertyIsEnumerable.call(null, { toString() { throw } }) throws the
        // key's error, not a TypeError for the null receiver.
        var key = a.Get1().ToKey(false);

        // §20.1.3.4 step 2: O = ToObject(this value). A primitive receiver is boxed
        // (so `"s".propertyIsEnumerable(0)` sees the String exotic's index property);
        // null/undefined throw a TypeError — after the key coercion above.
        var @object = ToObjectOrThrow(a.This);

        if (key.IsUInt)
        {
            ref var elements = ref @object.GetElements();
            ref var property = ref elements.Get(key.Index);
            if (!property.IsEmpty)
                return property.IsEnumerable ? BooleanTrue : BooleanFalse;

            return EnumerableFromDescriptor(@object, a.Get1());
        }

        if (key.IsSymbol)
        {
            ref var symbols = ref @object.GetSymbols();
            ref var property = ref symbols.GetRefOrDefault(key.Symbol.Key, ref JSProperty.Empty);
            if (!property.IsEmpty)
                return property.IsEnumerable ? BooleanTrue : BooleanFalse;

            return EnumerableFromDescriptor(@object, a.Get1());
        }

        ref var ownProperties = ref @object.GetOwnProperties(false);
        ref var ownProperty = ref ownProperties.GetValue(key.KeyString.Key);
        if (!ownProperty.IsEmpty)
            return !IsPrivateName(in key.KeyString) && ownProperty.IsEnumerable
                ? BooleanTrue : BooleanFalse;

        return EnumerableFromDescriptor(@object, a.Get1());
    }

    // §20.1.3.4: when an object has no directly stored property slot, the
    // enumerability of an own property is still defined by [[GetOwnProperty]] —
    // this surfaces synthesized exotic properties (String index characters,
    // typed-array elements) and routes Proxy receivers through their trap.
    private static JSValue EnumerableFromDescriptor(JSObject @object, JSValue key)
    {
        if (@object.GetOwnPropertyDescriptor(key) is JSObject descriptor)
            return descriptor[KeyStrings.enumerable].BooleanValue ? BooleanTrue : BooleanFalse;

        return BooleanFalse;
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
            return CreateString("[object Null]");

        if (a.This.IsUndefined)
            return CreateString("[object Undefined]");

        // The builtin tag (§20.1.3.6 steps 4-21) is computed first; a string-valued
        // @@toStringTag (step 22-23) overrides it. Error objects have an [[ErrorData]]
        // internal slot and therefore tag as "Error", not "Object".
        string builtinTag;
        if (a.This.IsArray)
            builtinTag = "Array";
        else if (a.This is IJSArguments)
            builtinTag = "Arguments";
        else if (a.This is IJSError)
            builtinTag = "Error";
        else if (a.This?.TypeOf() == JSConstants.Function)
            builtinTag = "Function";
        else
            // Boolean/Number/String (boxed primitives and their prototype objects,
            // which carry the matching [[XxxData]] slot) are resolved in the
            // BuiltIns layer via this hook; everything else tags as "Object".
            builtinTag = GetBuiltinToStringTag?.Invoke(a.This) ?? "Object";

        var toStringTag = GetGlobalSymbolFactory?.Invoke("toStringTag");
        if (toStringTag != null)
        {
            // §20.1.3.6 does ToObject(this) (step 3) before reading @@toStringTag, so a
            // primitive receiver reads the tag through its wrapper's prototype chain
            // (e.g. Boolean.prototype[@@toStringTag], or Symbol.prototype's default
            // "Symbol"). Box a non-object receiver to perform that lookup.
            var tagHolder = a.This as JSObject ?? CreatePrimitiveObject?.Invoke(a.This) as JSObject;
            if (tagHolder != null)
            {
                var tag = tagHolder[toStringTag];
                if (tag.IsString)
                    return CreateString($"[object {tag}]");
            }
        }

        return CreateString($"[object {builtinTag}]");
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
            return BooleanFalse;

        var first = a.Get1();
        var key = first.ToKey(false);
        if (key.IsUInt)
        {
            ref var elements = ref @object.GetElements();
            ref var property = ref elements.Get(key.Index);
            if (!property.IsEmpty)
                return BooleanTrue;

            return BooleanFalse;
        }

        if (key.IsSymbol)
        {
            ref var symbols = ref @object.GetSymbols();
            ref var property = ref symbols.GetRefOrDefault(key.Symbol.Key, ref JSProperty.Empty);
            if (!property.IsEmpty)
                return BooleanTrue;
            return BooleanFalse;
        }

        ref var op = ref @object.GetOwnProperties(false);
        ref var ownProperty = ref op.GetValue(key.KeyString.Key);
        if (!ownProperty.IsEmpty)
        {
            if (IsPrivateName(in key.KeyString))
                return BooleanFalse;
            return BooleanTrue;
        }

        return BooleanFalse;
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
        descriptor[KeyStrings.enumerable] = BooleanTrue;
        descriptor[KeyStrings.configurable] = BooleanTrue;
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
        if (a.Get1() is not JSObject candidate)
            return BooleanFalse;

        if (!a.This.TryAsObjectThrowIfNullOrUndefined(out var @this))
            return BooleanFalse;

        for (var current = candidate.GetPrototypeOf(); current is JSObject currentObject; current = currentObject.GetPrototypeOf())
        {
            if (ReferenceEquals(@this, currentObject))
                return BooleanTrue;
        }

        return BooleanFalse;
    }
}
