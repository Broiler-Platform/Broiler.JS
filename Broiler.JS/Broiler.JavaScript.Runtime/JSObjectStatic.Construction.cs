using System.Collections.Generic;
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

    private static void DefineArrayProperty(JSObject target, uint index, JSObject descriptor)
    {
        var lengthProperty = GetArrayLengthProperty(target);
        if (index >= target.Length && lengthProperty.IsReadOnly)
            throw NewTypeError("Cannot redefine property");

        target.DefineProperty(index, descriptor);
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

        var newLengthNumber = descriptor[KeyStrings.value].DoubleValue;
        if (double.IsNaN(newLengthNumber)
            || newLengthNumber < 0
            || newLengthNumber > uint.MaxValue
            || newLengthNumber != Math.Truncate(newLengthNumber))
        {
            throw NewTypeError("Invalid length");
        }

        var newLength = (uint)newLengthNumber;
        if (newLength >= currentLength)
        {
            target[KeyStrings.length] = JSValue.CreateNumber(newLength);
            SetArrayLengthWritable(target, newWritable);
            return;
        }

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

        target.DefineProperty(index, descriptor);
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

        target.DefineProperty(key, descriptor);
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

        static void SetSymbolValue(JSObject target, uint symbolKey, JSValue value)
        {
            ref var symbols = ref target.GetSymbols();
            ref var existing = ref symbols.GetRefOrDefault(symbolKey, ref JSProperty.Empty);

            if (!existing.IsEmpty)
            {
                if (existing.IsProperty)
                {
                    if (existing.set is IJSFunction setter)
                    {
                        setter.Delegate(new Arguments(target, value));
                        return;
                    }

                    throw NewTypeError($"Cannot modify property {symbolKey} of {target} which has only a getter");
                }

                if (existing.IsReadOnly)
                    throw NewTypeError($"Cannot modify property {symbolKey} of {target}");
            }

            if (target.IsFrozen())
                throw NewTypeError($"Cannot modify property {symbolKey} of {target}");

            if (existing.IsEmpty && !target.IsExtensible())
                throw NewTypeError($"Cannot add property {symbolKey} to {target}");

            symbols.Put(symbolKey) = new JSProperty(
                symbolKey,
                value,
                !existing.IsEmpty ? existing.Attributes : JSPropertyAttributes.EnumerableConfigurableValue);
            target.PropertyChanged?.Invoke(target, (uint.MaxValue, uint.MaxValue, null));
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
                    SetSymbolValue(target, symbol.Key, source[symbol]);
                    copiedSymbols ??= [];
                    copiedSymbols.Add(symbol.Key);
                    continue;
                }

                var key = propertyKey.ToKey(false);
                if (key.Type == KeyType.UInt)
                    target[key.Index] = source[key.Index];
                else
                    target[key.KeyString] = source[key.KeyString];
            }

            foreach (var (key, property) in source.GetSymbols().AllValues())
            {
                if (!property.IsEmpty && property.IsEnumerable && (copiedSymbols == null || !copiedSymbols.Contains(key)))
                    SetSymbolValue(target, key, source.GetValue(property));
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

        if (!target.IsExtensible())
            throw NewTypeError("Object is not extensible");

        var ownElements = pds is JSObject
            ? pdObject.GetElementEnumerator()
            : pds.GetElementEnumerator();
        while (ownElements.MoveNext(out var hasValue, out var item, out var index))
        {
            if (!hasValue)
                continue;

            if (item is not JSObject itemObject)
                throw NewTypeError("Property Description must be an object");

            DefineOwnProperty(target, index, itemObject);
        }

        var properties = pdObject.GetOwnProperties(false).GetEnumerator();
        while (properties.MoveNext(out var keyString, out var property))
        {
            var item = pdObject.GetValue(property);
            if (item is not JSObject itemObject)
                throw NewTypeError("Property Description must be an object");

            DefineOwnProperty(target, keyString, itemObject);
        }

        return target;
    }

    [JSExport("defineProperty")]
    internal static JSValue DefineProperty(in Arguments a)
    {
        var (target, key, desc) = a.Get3();

        if (target is not JSObject targetObject)
            throw NewTypeError("Object.defineProperty called on non-object");

        if (desc is not JSObject pd)
            throw NewTypeError("Property Description must be an object");

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
                    targetObject.DefineProperty(propertyKey.Symbol, pd);
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

        static JSProperty FreezeProperty(uint key, in JSProperty property)
        {
            var attributes = property.Attributes & (~JSPropertyAttributes.Configurable);
            if (property.IsValue)
                attributes |= JSPropertyAttributes.Readonly;

            return new JSProperty(key, property.get, property.set, property.value, attributes);
        }

        ref var ownProperties = ref @object.GetOwnProperties();
        ownProperties.Update((uint key, ref JSProperty property) => property = FreezeProperty(key, property));

        ref var elements = ref @object.GetElements();
        foreach (var (key, property) in elements.AllValues())
            elements.Put(key) = FreezeProperty(key, property);

        ref var symbols = ref @object.GetSymbols();
        foreach (var entry in symbols.All)
            symbols.Put(entry.Key) = FreezeProperty(entry.Key, entry.Value);

        if (!@object.PreventExtensions())
            throw NewTypeError("Cannot freeze object");

        @object.status |= ObjectStatus.Frozen;
        return @object;
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
                    returnable.Return(JSUndefined.Value);

                throw NewTypeError(NotEntry(item));
            }

            r.FastAddValue(entry[0], entry[1], JSPropertyAttributes.EnumerableConfigurableValue);
        }

        return r;
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

        if (!@object.PreventExtensions())
            throw NewTypeError("Cannot seal object");

        @object.status |= ObjectStatus.Sealed;
        @object.GetOwnProperties().Update((uint x, ref JSProperty v) => v = new JSProperty(x, v.get, v.set, v.value, v.Attributes & (~JSPropertyAttributes.Configurable)));
        ref var elements = ref @object.GetElements();
        foreach (var (key, property) in elements.AllValues())
            elements.Put(key) = new JSProperty(key, property.get, property.set, property.value, property.Attributes & (~JSPropertyAttributes.Configurable));

        ref var symbols = ref @object.GetSymbols();
        foreach (var entry in symbols.All)
            symbols.Put(entry.Key) = new JSProperty(entry.Key, entry.Value.get, entry.Value.set, entry.Value.value, entry.Value.Attributes & (~JSPropertyAttributes.Configurable));

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

        var result = new JSObject();
        var en = items.GetIterableEnumerator();
        int index = 0;

        while (en.MoveNext(out var hasValue, out var item, out var _))
        {
            if (!hasValue)
                continue;

            var key = JSObjectCoreExtensions.CallWith(callbackfn, JSValue.UndefinedValue, item, JSValue.CreateNumber(index));
            var keyStr = key.ToString();
            var group = result[keyStr];

            if (group.IsNullOrUndefined)
            {
                group = JSValue.CreateArray();
                result.FastAddValue(keyStr, group, JSPropertyAttributes.EnumerableConfigurableValue);
            }

            group.AddArrayItem(item);
            index++;
        }

        return result;
    }
}
