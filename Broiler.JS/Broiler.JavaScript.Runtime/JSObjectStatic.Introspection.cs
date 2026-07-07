using System;
using System.Collections.Generic;
using Broiler.JavaScript.Storage;

namespace Broiler.JavaScript.Runtime;

public partial class JSObject
{
    private static bool ShouldIncludeOwnPropertyKey(JSValue value, bool includeSymbols) =>
        includeSymbols ? value is IJSSymbol : value is not IJSSymbol;

    /// <summary>
    /// Implements the abstract operation ToObject for the reflective Object.*
    /// statics. Primitives (notably string primitives, which become String
    /// exotic objects with index/length own properties) are boxed so their own
    /// properties become observable; null/undefined throw a TypeError.
    /// </summary>
    private static JSObject ToObjectOrThrow(JSValue value)
    {
        if (value is JSObject @object)
            return @object;

        if (value.IsNullOrUndefined)
            throw NewTypeError(Cannot_convert_undefined_or_null_to_object);

        return CreatePrimitiveObject(value) as JSObject
            ?? throw new InvalidOperationException("CreatePrimitiveObject returned a non-object value.");
    }

    private static List<JSValue> GetOwnPropertyKeysInListOrder(JSObject @object)
    {
        var keys = new List<JSValue>();
        List<(uint Key, JSValue Value)> symbolKeys = null;

        if (!@object.IsArray)
        {
            foreach (var (key, property) in @object.GetElements(false).AllValues())
            {
                if (!property.IsEmpty)
                    keys.Add(CreateString(key.ToString()));
            }
        }

        var en = @object.GetAllKeys(false, false);
        while (en.MoveNext(out var hasValue, out var value, out var _))
        {
            if (!hasValue)
                continue;

            keys.Add(value);
        }

        foreach (var (key, property) in @object.GetSymbols().AllValues())
        {
            if (property.IsEmpty)
                continue;

            var symbol = GetSymbolByKeyFactory?.Invoke(key)
                ?? throw new InvalidOperationException($"Unknown symbol key {key}");
            symbolKeys ??= [];
            symbolKeys.Add((key, (JSValue)symbol));
        }

        if (symbolKeys != null)
        {
            symbolKeys.Sort(static (left, right) => left.Key.CompareTo(right.Key));
            for (var i = 0; i < symbolKeys.Count; i++)
                keys.Add(symbolKeys[i].Value);
        }

        return keys;
    }

    // Snapshot O.[[OwnPropertyKeys]]() restricted to String keys, in list order.
    // For a Proxy this triggers the ownKeys trap exactly once (showEnumerableOnly:
    // false skips per-key descriptor lookups); the caller does its own per-key
    // [[GetOwnProperty]] so the enumerable filter and [[Get]] stay interleaved.
    private static List<JSValue> SnapshotOwnStringKeys(JSObject target)
    {
        var keys = new List<JSValue>();
        var en = target.GetAllKeys(showEnumerableOnly: false, inherited: false);
        while (en.MoveNext(out var hasValue, out var key, out var _))
        {
            if (hasValue && key is not IJSSymbol)
                keys.Add(key);
        }
        return keys;
    }

    private static bool HasDescriptorField(JSObject descriptor, KeyString key) =>
        !descriptor.GetInternalProperty(key, false).IsEmpty;

    private static bool IsDataDescriptor(JSObject descriptor) =>
        HasDescriptorField(descriptor, KeyStrings.value) || HasDescriptorField(descriptor, KeyStrings.writable);

    private static bool TestIntegrityLevel(JSObject @object, bool frozen)
    {
        if (@object.IsExtensible())
            return false;

        foreach (var key in GetOwnPropertyKeysInListOrder(@object))
        {
            if (@object.GetOwnPropertyDescriptor(key) is not JSObject descriptor)
                continue;

            if (descriptor[KeyStrings.configurable].BooleanValue)
                return false;

            if (frozen && IsDataDescriptor(descriptor) && descriptor[KeyStrings.writable].BooleanValue)
                return false;
        }

        return true;
    }

    [JSExport("entries")]
    internal static JSValue StaticEntries(in Arguments a)
    {
        var target = ToObjectOrThrow(a.Get1());

        var r = CreateArray();

        // EnumerableOwnProperties(O, key+value): snapshot the own String-keyed
        // property names ([[OwnPropertyKeys]]) BEFORE reading any value, then for
        // EACH key in order call [[GetOwnProperty]] and, if enumerable, [[Get]] —
        // the descriptor lookup and value read must interleave per key (observable
        // for a Proxy). Snapshotting the key list first also stops a getter that
        // mutates the object from injecting a freshly-added key into the result.
        foreach (var key in SnapshotOwnStringKeys(target))
        {
            if (target.GetOwnPropertyDescriptor(key) is not JSObject descriptor)
                continue;
            if (!descriptor[KeyStrings.enumerable].BooleanValue)
                continue;

            var entry = CreateArray();
            entry.AddArrayItem(key);
            entry.AddArrayItem(target[key]);
            r.AddArrayItem(entry);
        }

        return r;
    }

    [JSExport("is")]
    internal static JSValue Is(in Arguments a)
    {
        var (first, second) = a.Get2();
        return first.Is(second);
    }

    [JSExport("isExtensible")]
    internal static JSValue IsExtensible(in Arguments a)
    {
        if (a.Get1() is JSObject @object && @object.IsExtensible())
            return BooleanTrue;

        return BooleanFalse;
    }

    [JSExport("isFrozen")]
    internal static JSValue IsFrozen(in Arguments a)
    {
        var value = a.Get1();
        if (value is not JSObject @object)
            return BooleanTrue;

        if (@object is IJSIntegerIndexedObject { HasIntegerIndexedElements: true })
            return BooleanFalse;

        return TestIntegrityLevel(@object, frozen: true) ? BooleanTrue : BooleanFalse;
    }

    [JSExport("isSealed")]
    internal static JSValue IsSealed(in Arguments a)
    {
        var value = a.Get1();
        if (value is not JSObject @object)
            return BooleanTrue;

        if (@object is IJSIntegerIndexedObject { HasIntegerIndexedElements: true })
            return BooleanFalse;

        return TestIntegrityLevel(@object, frozen: false) ? BooleanTrue : BooleanFalse;
    }

    [JSExport("keys")]
    internal static JSValue Keys(in Arguments a)
    {
        var target = ToObjectOrThrow(a.Get1());

        var r = CreateArray();

        // EnumerableOwnProperties(O, key) (§7.3.23): snapshot the own String-keyed names
        // ([[OwnPropertyKeys]]) first, then for EACH key call [[GetOwnProperty]] and keep it
        // only when the descriptor is enumerable. The per-key descriptor lookup is observable
        // on a Proxy (its getOwnPropertyDescriptor trap), which the generic key walk skipped —
        // so Object.keys(proxy) only triggered the ownKeys trap. Mirrors Object.values/entries.
        foreach (var key in SnapshotOwnStringKeys(target))
        {
            if (target.GetOwnPropertyDescriptor(key) is not JSObject descriptor)
                continue;
            if (!descriptor[KeyStrings.enumerable].BooleanValue)
                continue;

            r.AddArrayItem(key);
        }

        return r;
    }

    [JSExport("values")]
    internal static JSValue Values(in Arguments a)
    {
        var target = ToObjectOrThrow(a.Get1());

        var r = CreateArray();

        // EnumerableOwnProperties(O, value): mirror Object.entries but keep only the
        // values; the per-key [[GetOwnProperty]] / [[Get]] interleaving still holds.
        foreach (var key in SnapshotOwnStringKeys(target))
        {
            if (target.GetOwnPropertyDescriptor(key) is not JSObject descriptor)
                continue;
            if (!descriptor[KeyStrings.enumerable].BooleanValue)
                continue;

            r.AddArrayItem(target[key]);
        }

        return r;
    }

    [JSExport("getOwnPropertyDescriptor")]
    internal static JSValue GetOwnPropertyDescriptor(in Arguments a)
    {
        var (first, name) = a.Get2();

        var jobj = ToObjectOrThrow(first);

        return jobj.GetOwnPropertyDescriptor(name);
    }

    [JSExport("getOwnPropertyDescriptors")]
    internal static JSValue GetOwnPropertyDescriptors(in Arguments a)
    {
        var jobj = ToObjectOrThrow(a.Get1());

        var r = new JSObject();
        foreach (var key in GetOwnPropertyKeysInListOrder(jobj))
        {
            if (key.ToKey(false) is { Type: KeyType.String, KeyString: var keyString } && IsPrivateName(in keyString))
                continue;

            var descriptor = jobj.GetOwnPropertyDescriptor(key);
            if (!descriptor.IsUndefined)
                r.SetPropertyOrThrow(key, descriptor);
        }

        return r;
    }

    /// <summary>
    /// The Object.getOwnPropertyNames() method returns an array of all properties 
    /// (including non-enumerable properties except for those which use Symbol) 
    /// found directly in a given object.
    /// </summary>
    /// <param name="a"></param>
    /// <returns></returns>
    [JSExport("getOwnPropertyNames")]
    internal static JSValue GetOwnPropertyNames(in Arguments a)
    {
        var jobj = ToObjectOrThrow(a.Get1());

        var en = jobj.GetAllKeys(false, false);
        var r = CreateArray();
        while (en.MoveNext(out var hasValue, out var value, out var index))
        {
            if (hasValue && ShouldIncludeOwnPropertyKey(value, includeSymbols: false))
                r.AddArrayItem(value);
        }
        return r;
    }

    [JSExport("getOwnPropertySymbols")]
    internal static JSValue GetOwnPropertySymbols(in Arguments a)
    {
        var jobj = ToObjectOrThrow(a.Get1());

        var r = CreateArray();
        HashSet<uint> emittedSymbols = null;

        foreach (var value in GetOwnPropertyKeysInListOrder(jobj))
        {
            if (!ShouldIncludeOwnPropertyKey(value, includeSymbols: true))
                continue;

            r.AddArrayItem(value);
            if (value is IJSSymbol symbol)
            {
                emittedSymbols ??= [];
                emittedSymbols.Add(symbol.Key);
            }
        }

        foreach (var (key, property) in jobj.GetSymbols().AllValues())
        {
            if (property.IsEmpty || (emittedSymbols != null && emittedSymbols.Contains(key)))
                continue;

            var symbol = GetSymbolByKeyFactory?.Invoke(key)
                ?? throw new InvalidOperationException($"Unknown symbol key {key}");
            r.AddArrayItem((JSValue)symbol);
        }

        return r;
    }

    [JSExport("getPrototypeOf")]
    internal static JSValue GetPrototypeOf(in Arguments a)
    {
        var value = a.Get1();
        if (value.IsNullOrUndefined)
            throw NewTypeError(Cannot_convert_undefined_or_null_to_object);

        return value.GetPrototypeOf();
    }
}
