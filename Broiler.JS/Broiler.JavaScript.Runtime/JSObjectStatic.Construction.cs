using System.Collections.Generic;
using System.Collections.Concurrent;
using Broiler.JavaScript.Storage;
using System;

namespace Broiler.JavaScript.Runtime;

public partial class JSObject
{
    private static JSProperty GetArrayLengthProperty(JSObject target)
    {
        ref var ownProperties = ref target.GetOwnProperties(false);
        if (!ownProperties.IsEmpty)
        {
            ref var existing = ref ownProperties.GetValue(KeyStrings.length.Key);
            if (!existing.IsEmpty)
                return existing;
        }

        return new JSProperty(KeyStrings.length, JSValue.CreateNumber(target.Length), JSPropertyAttributes.Value);
    }

    private static void SetArrayLengthWritable(JSObject target, bool writable)
    {
        ref var ownProperties = ref target.GetOwnProperties();
        ownProperties.Put(KeyStrings.length.Key) = new JSProperty(
            KeyStrings.length,
            JSValue.CreateNumber(target.Length),
            writable ? JSPropertyAttributes.Value : JSPropertyAttributes.ReadonlyValue);
        target.PropertyChanged?.Invoke(target, (KeyStrings.length.Key, uint.MaxValue, null));
    }

    private static uint ToUint32(double number)
    {
        if (double.IsNaN(number) || double.IsInfinity(number))
            return 0;

        const double twoPow32 = 4294967296d;
        var truncated = Math.Truncate(number) % twoPow32;
        if (truncated < 0)
            truncated += twoPow32;

        return (uint)truncated;
    }

    private static void DefineArrayProperty(JSObject target, uint index, JSObject descriptor)
    {
        var lengthProperty = GetArrayLengthProperty(target);
        if (index >= target.Length && lengthProperty.IsReadOnly)
            throw NewTypeError("Cannot redefine property");

        var result = target.DefineProperty(index, descriptor);
        if (result.IsBoolean && !result.BooleanValue)
            throw NewTypeError("Cannot define property");
    }

    private static void DefineArrayLength(JSObject target, JSObject descriptor)
    {
        var currentLength = (uint)Math.Max(target.Length, 0);
        var currentLengthProperty = GetArrayLengthProperty(target);
        var currentWritable = !currentLengthProperty.IsReadOnly;

        var hasValue = !descriptor.GetInternalProperty(KeyStrings.value, false).IsEmpty;
        var hasWritable = !descriptor.GetInternalProperty(KeyStrings.writable, false).IsEmpty;
        var hasEnumerable = !descriptor.GetInternalProperty(KeyStrings.enumerable, false).IsEmpty;
        var hasConfigurable = !descriptor.GetInternalProperty(KeyStrings.configurable, false).IsEmpty;

        // ArraySetLength steps 3-5: when a length value is supplied, ToUint32 then ToNumber are *both*
        // performed (each coerces the value, so a custom valueOf is invoked twice) and the results must
        // agree under SameValueZero or it is an invalid length. This RangeError is produced BEFORE the
        // ordinary descriptor invariants below (which throw TypeError) — so e.g. {value: -1,
        // configurable: true} is a RangeError for the invalid value, not a TypeError for the
        // configurable redefinition.
        uint newLength = 0;
        if (hasValue)
        {
            var rawValue = descriptor[KeyStrings.value];
            newLength = ToUint32(rawValue.DoubleValue);
            var numberLen = rawValue.DoubleValue;
            if (newLength != numberLen)
                throw NewRangeError("Invalid length");
        }

        if (!descriptor[KeyStrings.get].IsUndefined
            || !descriptor[KeyStrings.set].IsUndefined
            || (hasEnumerable && descriptor[KeyStrings.enumerable].BooleanValue)
            || (hasConfigurable && descriptor[KeyStrings.configurable].BooleanValue))
        {
            throw NewTypeError("Cannot redefine property");
        }

        var newWritable = hasWritable ? descriptor[KeyStrings.writable].BooleanValue : currentWritable;
        if (!currentWritable && newWritable)
            throw NewTypeError("Cannot redefine property");

        if (!hasValue)
        {
            SetArrayLengthWritable(target, newWritable);
            return;
        }

        // Step 9: when the new length is not smaller than the current one, defer to
        // ordinary [[DefineOwnProperty]] validation — redefining to the *same* value
        // is a no-op even when length is non-writable, but growing it is rejected.
        if (newLength >= currentLength)
        {
            if (!currentWritable && newLength != currentLength)
                throw NewTypeError("Cannot redefine property");

            target[KeyStrings.length] = JSValue.CreateNumber(newLength);
            SetArrayLengthWritable(target, newWritable);
            return;
        }

        // Step 10: shrinking a non-writable length always fails.
        if (!currentWritable)
            throw NewTypeError("Cannot redefine property");

        for (uint i = currentLength; i > newLength; i--)
        {
            var index = i - 1;
            if (!target.Delete(index).BooleanValue)
            {
                target[KeyStrings.length] = JSValue.CreateNumber(index + 1);
                SetArrayLengthWritable(target, newWritable);
                throw NewTypeError("Cannot redefine property");
            }
        }

        target[KeyStrings.length] = JSValue.CreateNumber(newLength);
        SetArrayLengthWritable(target, newWritable);
    }

    private static void DefineOwnProperty(JSObject target, uint index, JSObject descriptor)
    {
        if (target.IsArray)
        {
            DefineArrayProperty(target, index, descriptor);
            return;
        }

        if (target.GetType() != typeof(JSObject))
        {
            var result = target.DefineProperty(JSValue.CreateNumber(index), descriptor);
            if (result.IsBoolean && !result.BooleanValue)
                throw NewTypeError("Cannot define property");
            return;
        }

        var r = target.DefineProperty(index, descriptor);
        if (r.IsBoolean && !r.BooleanValue)
            throw NewTypeError("Cannot define property");
    }

    private static void DefineOwnProperty(JSObject target, KeyString key, JSObject descriptor)
    {
        if (target.IsArray && key.Key == KeyStrings.length.Key)
        {
            DefineArrayLength(target, descriptor);
            return;
        }

        if (target.GetType() != typeof(JSObject))
        {
            var result = target.DefineProperty(key.ToJSValue(), descriptor);
            if (result.IsBoolean && !result.BooleanValue)
                throw NewTypeError("Cannot define property");
            return;
        }

        var r = target.DefineProperty(key, descriptor);
        if (r.IsBoolean && !r.BooleanValue)
            throw NewTypeError("Cannot define property");
    }

    private static void DefineOwnProperty(JSObject target, IJSSymbol key, JSObject descriptor)
    {
        if (target.GetType() != typeof(JSObject))
        {
            var result = target.DefineProperty((JSValue)key, descriptor);
            if (result.IsBoolean && !result.BooleanValue)
                throw NewTypeError("Cannot define property");
            return;
        }

        var r = target.DefineProperty(key, descriptor);
        if (r.IsBoolean && !r.BooleanValue)
            throw NewTypeError("Cannot define property");
    }

    private static void DefineOwnProperty(JSObject target, JSValue key, JSObject descriptor)
    {
        var propertyKey = key.ToKey(false);
        switch (propertyKey.Type)
        {
            case KeyType.UInt:
                DefineOwnProperty(target, propertyKey.Index, descriptor);
                return;
            case KeyType.String:
                DefineOwnProperty(target, propertyKey.KeyString, descriptor);
                return;
            case KeyType.Symbol:
                DefineOwnProperty(target, propertyKey.Symbol, descriptor);
                return;
            default:
                throw NewTypeError($"Cannot define property {key}");
        }
    }

    private static JSObject CreateIntegrityDescriptor(bool writable)
    {
        var descriptor = new JSObject();
        descriptor.SetPropertyOrThrow(KeyStrings.configurable.ToJSValue(), JSValue.BooleanFalse);
        if (writable)
            descriptor.SetPropertyOrThrow(KeyStrings.writable.ToJSValue(), JSValue.BooleanFalse);

        return descriptor;
    }

    [JSExport("create")]
    internal static JSValue StaticCreate(in Arguments a)
    {
        static JSObject CreateObject(JSValue prototype)
        {
            if (prototype.IsNull)
            {
                var result = new JSObject();
                result.BasePrototypeObject = null;
                return result;
            }

            if (prototype is not JSObject proto)
                throw NewTypeError("Object prototype may only be an Object or null");

            return new JSObject(proto);
        }

        var (prototype, properties) = a.Get2();
        var created = CreateObject(prototype);

        if (!properties.IsUndefined)
            DefineProperties(new Arguments(a.This, created, properties));

        return created;
    }

    [JSExport("assign")]
    internal static JSValue Assign(in Arguments a)
    {
        static JSObject ToObject(JSValue value)
        {
            if (value is JSObject @object)
                return @object;

            if (value.IsNullOrUndefined)
                throw NewTypeError(Cannot_convert_undefined_or_null_to_object);

            return CreatePrimitiveObject(value) as JSObject
                ?? throw new InvalidOperationException("CreatePrimitiveObject returned a non-object value.");
        }

        var target = ToObject(a.Get1());

        for (var i = 1; i < a.Length; i++)
        {
            var ai = a.GetAt(i);
            if (ai.IsNullOrUndefined)
                continue;

            var source = ToObject(ai);
            HashSet<uint> copiedSymbols = null;
            var keys = source.GetAllKeys(showEnumerableOnly: false, inherited: false);
            while (keys.MoveNext(out var hasValue, out var propertyKey, out var _))
            {
                if (!hasValue)
                    continue;

                var descriptor = source.GetOwnPropertyDescriptor(propertyKey);
                if (descriptor.IsUndefined || !descriptor[KeyStrings.enumerable].BooleanValue)
                    continue;

                if (propertyKey.IsSymbol)
                {
                    var symbol = (IJSSymbol)propertyKey;
                    target.SetPropertyOrThrow((JSValue)symbol, source[symbol]);
                    copiedSymbols ??= [];
                    copiedSymbols.Add(symbol.Key);
                    continue;
                }

                var key = propertyKey.ToKey(false);
                if (key.Type == KeyType.UInt)
                    target.SetPropertyOrThrow(propertyKey, source[key.Index]);
                else
                    target.SetPropertyOrThrow(propertyKey, source[key.KeyString]);
            }

            foreach (var (key, property) in source.GetSymbols().AllValues())
            {
                if (!property.IsEmpty && property.IsEnumerable && (copiedSymbols == null || !copiedSymbols.Contains(key)))
                    target.SetPropertyOrThrow((JSValue)(JSValue.GetSymbolByKeyFactory?.Invoke(key)
                        ?? throw new InvalidOperationException($"Unknown symbol key {key}")), source.GetValue(property));
            }
        }

        return target;
    }

    [JSExport("defineProperties")]
    internal static JSValue DefineProperties(in Arguments a)
    {
        var (a0, a1) = a.Get2();
        if (a0 is not JSObject target)
            throw NewTypeError("Object.defineProperty called on non-object");

        var pds = a1;
        if (pds.IsNullOrUndefined)
            throw NewTypeError(Cannot_convert_undefined_or_null_to_object);

        var pdObject = pds as JSObject ?? (JSObject)CreatePrimitiveObject(pds);

        var descriptors = new List<(JSValue Key, JSObject Descriptor)>();
        foreach (var key in GetOwnPropertyKeysInListOrder(pdObject))
        {
            if (pdObject.GetOwnPropertyDescriptor(key) is not JSObject propertyDescriptor
                || !propertyDescriptor[KeyStrings.enumerable].BooleanValue)
            {
                continue;
            }

            if (pdObject[key] is not JSObject itemObject)
                throw NewTypeError("Property Description must be an object");

            // ToPropertyDescriptor for each descriptor (resolves inherited / accessor
            // fields), performed during the enumeration pass like the spec.
            descriptors.Add((key, JSObject.NormalizeDescriptor(itemObject)));
        }

        foreach (var (key, descriptor) in descriptors)
            DefineOwnProperty(target, key, descriptor);

        return target;
    }

    [JSExport("defineProperty")]
    internal static JSValue DefineProperty(in Arguments a)
    {
        var (target, key, desc) = a.Get3();

        if (target is not JSObject targetObject)
            throw NewTypeError("Object.defineProperty called on non-object");

        if (desc is not JSObject userDesc)
            throw NewTypeError("Property Description must be an object");

        // ToPropertyDescriptor: read the descriptor fields (which may be inherited or
        // accessor-backed) into a fresh own-only record before defining.
        var pd = JSObject.NormalizeDescriptor(userDesc);

        var propertyKey = key.ToKey();
        switch (propertyKey.Type)
        {
            case KeyType.UInt:
                DefineOwnProperty(targetObject, propertyKey.Index, pd);
                break;
            case KeyType.String:
                DefineOwnProperty(targetObject, propertyKey.KeyString, pd);
                break;
            case KeyType.Symbol:
                if (targetObject.GetType() == typeof(JSObject))
                {
                    var symbolResult = targetObject.DefineProperty(propertyKey.Symbol, pd);
                    if (symbolResult.IsBoolean && !symbolResult.BooleanValue)
                        throw NewTypeError("Cannot define property");
                }
                else
                {
                    var result = targetObject.DefineProperty(key, pd);
                    if (result.IsBoolean && !result.BooleanValue)
                        throw NewTypeError("Cannot define property");
                }
                break;
            default:
                throw NewTypeError($"Cannot define property {key}");
        }

        return targetObject;
    }

    [JSExport("entries")]
    internal static JSValue GetEntries(in Arguments a)
    {
        if (a[0] is not JSObject obj)
            throw NewTypeError(NotIterable("undefined"));

        var r = JSValue.CreateArray();

        var es = obj.GetElementEnumerator();
        while (es.MoveNext(out var hasValue, out var value, out var index))
        {
            if (hasValue)
            {
                var entry = JSValue.CreateArray();
                entry.AddArrayItem(JSValue.CreateNumber(index));
                entry.AddArrayItem(value);
                r.AddArrayItem(entry);
            }
        }

        var vp = new PropertyValueEnumerator(obj, false);
        while (vp.MoveNext(out var value, out var key))
        {
            var entry = JSValue.CreateArray();
            entry.AddArrayItem(JSObjectCoreExtensions.KeyStringToJSValue(key));
            entry.AddArrayItem(value);
            r.AddArrayItem(entry);
        }

        return r;
    }

    [JSExport("freeze")]
    internal static JSValue Freeze(in Arguments a)
    {
        var first = a.Get1();
        if (first is not JSObject @object)
            return first;

        if (@object is IJSIntegerIndexedObject { HasIntegerIndexedElements: true })
        {
            if (!@object.PreventExtensions())
                throw NewTypeError("Cannot freeze object");

            throw NewTypeError("Cannot freeze object");
        }

        if (!@object.PreventExtensions())
            throw NewTypeError("Cannot freeze object");

        foreach (var key in GetOwnPropertyKeysInListOrder(@object))
        {
            // Private elements are not own property keys (they live in [[PrivateElements]]),
            // so SetIntegrityLevel never touches them — a frozen object keeps writable fields.
            if (IsPrivateNameKey(key))
                continue;

            if (@object.GetOwnPropertyDescriptor(key) is not JSObject descriptor)
                continue;

            DefineOwnProperty(@object, key, CreateIntegrityDescriptor(IsDataDescriptor(descriptor)));
        }

        @object.status |= ObjectStatus.Frozen;
        return @object;
    }

    internal static JSObject FreezeObject(JSObject @object)
    {
        Freeze(new Arguments(JSUndefined.Value, @object));
        return @object;
    }

    private static readonly ConcurrentDictionary<int, JSValue> _templateObjectCache = new();

    internal static JSValue GetOrCreateTemplateObject(int cacheKey, JSObject templateArray)
    {
        return _templateObjectCache.GetOrAdd(cacheKey, _ => FreezeObject(templateArray));
    }

    [JSExport("fromEntries")]
    internal static JSValue FromEntries(in Arguments a)
    {
        var v = a.Get1();
        if (v.IsNullOrUndefined)
            throw NewTypeError(NotIterable("undefined"));

        var r = new JSObject();
        var en = v.GetIterableEnumerator();
        while (en.MoveNext(out var item))
        {
            if (item is not JSObject entry)
            {
                if (en is IReturnableEnumerator returnable)
                    returnable.Return();

                throw NewTypeError(NotEntry(item));
            }

            r.FastAddValue(entry[0], entry[1], JSPropertyAttributes.EnumerableConfigurableValue);
        }

        return r;
    }

    // B.3.1 __proto__ Property Names in Object Initializers: the `__proto__: value` form sets the new
    // object's [[Prototype]] directly via [[SetPrototypeOf]], NOT via a "__proto__" property assignment
    // (which a same-named own data property defined elsewhere in the literal would shadow). Only an
    // Object or null value applies; any other value (including undefined) is silently ignored.
    public static void SetObjectLiteralPrototype(JSObject target, JSValue value)
    {
        if (value is JSObject || value.IsNull)
            target.SetPrototypeOf(value);
    }

    [JSExport("preventExtensions")]
    internal static JSValue PreventExtensions(in Arguments a)
    {
        var first = a.Get1();
        if (first is not JSObject @object)
            return first;

        if (!@object.PreventExtensions())
            throw NewTypeError("Cannot prevent extensions");

        return @object;
    }

    [JSExport("seal")]
    internal static JSValue Seal(in Arguments a)
    {
        var first = a.Get1();
        if (first is not JSObject @object)
            return first;

        if (@object is IJSIntegerIndexedObject { HasIntegerIndexedElements: true })
        {
            if (!@object.PreventExtensions())
                throw NewTypeError("Cannot seal object");

            throw NewTypeError("Cannot seal object");
        }

        if (!@object.PreventExtensions())
            throw NewTypeError("Cannot seal object");

        foreach (var key in GetOwnPropertyKeysInListOrder(@object))
        {
            // Private elements are not own property keys (they live in [[PrivateElements]]),
            // so SetIntegrityLevel never touches them.
            if (IsPrivateNameKey(key))
                continue;

            if (@object.GetOwnPropertyDescriptor(key).IsUndefined)
                continue;

            DefineOwnProperty(@object, key, CreateIntegrityDescriptor(writable: false));
        }

        @object.status |= ObjectStatus.Sealed;
        return first;
    }

    [JSExport("setPrototypeOf")]
    internal static JSValue SetPrototypeOf(in Arguments a)
    {
        var (first, second) = a.Get2();
        first.SetPrototypeOf(second);
        return first;
    }

    [JSExport("groupBy")]
    internal static JSValue GroupBy(in Arguments a)
    {
        var (items, callbackfn) = a.Get2();

        if (items.IsNullOrUndefined)
            throw NewTypeError(Cannot_convert_undefined_or_null_to_object);

        if (!callbackfn.IsFunction)
            throw NewTypeError("CallbackFn must be a function");

        // Per spec, Object.groupBy returns an object with a null prototype.
        var result = new JSObject();
        result.BasePrototypeObject = null;
        var en = items.GetIterableEnumerator();
        int index = 0;

        while (en.MoveNext(out var hasValue, out var item, out var _))
        {
            if (!hasValue)
                continue;

            var key = JSObjectCoreExtensions.CallWith(callbackfn, JSValue.UndefinedValue, item, JSValue.CreateNumber(index));
            // ToPropertyKey: a Symbol key stays a Symbol, everything else coerces to a String. Keep the
            // key as a JSValue (not a CLR string) so the property store canonicalises an array-index name
            // such as "4" to an integer index — otherwise the group is unreachable as result[4].
            var keyValue = key is IJSSymbol ? key : JSValue.CreateString(key.StringValue);
            var group = result[keyValue];

            if (group.IsNullOrUndefined)
            {
                group = JSValue.CreateArray();
                result.FastAddValue(keyValue, group, JSPropertyAttributes.EnumerableConfigurableValue);
            }

            group.AddArrayItem(item);
            index++;
        }

        return result;
    }
}
