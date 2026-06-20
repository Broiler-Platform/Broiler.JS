using Broiler.JavaScript.BuiltIns.Function;
using Broiler.JavaScript.BuiltIns.Array;
using Broiler.JavaScript.BuiltIns.Boolean;
using Broiler.JavaScript.BuiltIns.Iterator;
using Broiler.JavaScript.BuiltIns.Generator;
using Broiler.JavaScript.ExpressionCompiler;
using System.Collections.Generic;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Extensions;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Map;

[JSClassGenerator("Map")]
public partial class JSMap : JSObject
{
    // Entries are appended to an ordered list and never physically removed while live
    // iterators may be in flight; deletion/clear mark the slot as a tombstone instead.
    // Iteration walks the list by index so that entries added during iteration are
    // observed and deleted entries are skipped, matching the spec's [[MapData]] semantics
    // (and avoiding "Collection was modified" exceptions from foreach over a LinkedList).
    private struct Entry
    {
        public JSValue Key;
        public JSValue Value;
        public bool Deleted;
    }

    private readonly List<Entry> store = new();
    private StringMap<int> index = new();
    private int liveCount;

    [JSExport]
    public int Size => liveCount;

    public JSMap(in Arguments a) : base(JSEngine.NewTargetPrototype)
    {
        var iterable = a.Get1();
        if (iterable.IsNullOrUndefined)
            return;

        var adderTarget =
            (((JSEngine.Current as IJSExecutionContext)?.CurrentNewTarget as IJSFunction)?.Prototype as JSValue)
            ?? JSEngine.NewTargetPrototype
            ?? this;
        if (adderTarget[KeyStrings.set] is not IJSFunction adder)
            throw JSEngine.NewTypeError("Map instance 'set' property is not callable");

        var en = iterable.GetIterableEnumerator();
        while (en.MoveNext(out var item))
        {
            try
            {
                if (item is not JSObject entry)
                    throw JSEngine.NewTypeError(JSObject.NotEntry(item));

                adder.InvokeFunction(new Arguments(this, entry[0], entry[1]));
            }
            catch
            {
                JSIteratorObject.CloseIteratorIfPossible(en);
                throw;
            }
        }
    }

    [JSExport("groupBy")]
    internal static new JSValue GroupBy(in Arguments a)
    {
        var (items, callbackfn) = a.Get2();
        if (items.IsNullOrUndefined)
            throw JSEngine.NewTypeError(JSException.Cannot_convert_undefined_or_null_to_object);

        if (!callbackfn.IsFunction)
            throw JSEngine.NewTypeError("CallbackFn must be a function");

        var result = new JSMap(Arguments.Empty);
        var en = items.GetIterableEnumerator();
        int index = 0;

        while (en.MoveNext(out var hasValue, out var item, out var _))
        {
            if (!hasValue)
                continue;

            var key = callbackfn.Call(JSUndefined.Value, item, new JSNumber(index));
            var existing = result.Get(key);

            if (existing.IsNullOrUndefined)
            {
                var arr = new JSArray();
                arr.Add(item);
                result.Set(key, arr);
            }
            else
            {
                (existing as JSArray)?.Add(item);
            }

            index++;
        }

        return result;
    }

    [JSExport("set")]
    public JSValue Set(JSValue key, JSValue value)
    {
        HashedString uk = key.ToUniqueID();

        if (index.TryGetValue(in uk, out var pos))
        {
            var e = store[pos];
            e.Value = value;
            store[pos] = e;
        }
        else
        {
            index.Put(in uk) = store.Count;
            store.Add(new Entry { Key = key, Value = value });
            liveCount++;
        }

        return value;
    }

    [JSExport("clear")]
    public JSValue Set(in Arguments a)
    {
        // Preserve the backing list (live iterators keep their position) but tombstone
        // every slot so they observe the entries as removed.
        for (var i = 0; i < store.Count; i++)
        {
            var e = store[i];
            e.Deleted = true;
            e.Key = JSUndefined.Value;
            e.Value = JSUndefined.Value;
            store[i] = e;
        }

        index = new();
        liveCount = 0;

        return JSUndefined.Value;
    }

    [JSExport("delete")]
    public JSValue Delete(in Arguments a)
    {
        var f = a[0];
        HashedString uk = f.ToUniqueID();

        if (index.TryGetValue(in uk, out var pos))
        {
            var e = store[pos];
            e.Deleted = true;
            e.Key = JSUndefined.Value;
            e.Value = JSUndefined.Value;
            store[pos] = e;
            index.TryRemove(uk.Value, out _);
            liveCount--;
            return JSBoolean.True;
        }

        return JSBoolean.False;
    }

    [JSExport("entries")]
    [Symbol("@@iterator")]
    public JSValue GetEntries()
        => new JSGenerator(new ClrEnumerableElementEnumerator(EnumerateEntries()), "Map Iterator");

    // Walk by index so entries added during iteration are observed and tombstoned
    // entries are skipped.
    internal IEnumerable<JSValue> EnumerateEntries()
    {
        for (var i = 0; i < store.Count; i++)
        {
            var e = store[i];
            if (e.Deleted)
                continue;

            yield return new JSArray(e.Key, e.Value);
        }
    }

    [JSExport("forEach", Length = 1)]
    public JSValue ForEach(in Arguments a)
    {
        // callbackfn is the first argument; thisArg (the callback's `this`) is the
        // SECOND argument, defaulting to undefined — NOT the forEach receiver (the
        // Map). InvokeCallback applies the non-strict this-coercion (undefined →
        // global object for a sloppy callback) like Array.prototype.forEach.
        var (callback, thisArg) = a.Get2();
        if (callback is not JSFunction fx)
            throw JSEngine.NewTypeError($"Function parameter expected");

        for (var i = 0; i < store.Count; i++)
        {
            var e = store[i];
            if (e.Deleted)
                continue;

            fx.InvokeCallback(new Arguments(thisArg, e.Value, e.Key, this));
        }

        return JSUndefined.Value;
    }

    [JSExport("has")]
    public JSValue Has(in Arguments a)
    {
        var f = a.Get1();
        HashedString uk = f.ToUniqueID();
        if (index.TryGetValue(in uk, out _))
            return JSBoolean.True;

        return JSBoolean.False;
    }


    [JSExport("get")]
    public JSValue Get(JSValue key)
    {
        HashedString uk = key.ToUniqueID();
        if (index.TryGetValue(in uk, out var pos))
            return store[pos].Value;

        return JSUndefined.Value;
    }

    [JSExport("keys")]
    public JSValue Keys()
        => new JSGenerator(new ClrEnumerableElementEnumerator(EnumerateKeys()), "Map Iterator");

    private IEnumerable<JSValue> EnumerateKeys()
    {
        for (var i = 0; i < store.Count; i++)
        {
            var e = store[i];
            if (e.Deleted)
                continue;

            yield return e.Key;
        }
    }


    [JSExport("values")]
    public JSValue Values()
        => new JSGenerator(new ClrEnumerableElementEnumerator(EnumerateValues()), "Map Iterator");

    private IEnumerable<JSValue> EnumerateValues()
    {
        for (var i = 0; i < store.Count; i++)
        {
            var e = store[i];
            if (e.Deleted)
                continue;

            yield return e.Value;
        }
    }

    /// <summary>
    /// ES2026 §4.9.1 — Map.prototype.getOrInsert(key, defaultValue)
    /// Returns the value for key if present, otherwise inserts defaultValue
    /// and returns it.
    /// </summary>
    /// <summary>
    /// CanonicalizeKeyedCollectionKey (§24.5.1): -0𝔽 is stored and surfaced as +0𝔽.
    /// </summary>
    private static JSValue CanonicalizeKey(JSValue key)
        => key is JSNumber n && n.value == 0.0 && double.IsNegative(n.value)
            ? JSValue.CreateNumber(0.0)
            : key;

    [JSExport("getOrInsert", Length = 2)]
    public JSValue GetOrInsert(in Arguments a)
    {
        var (key, defaultValue) = a.Get2();
        key = CanonicalizeKey(key);
        HashedString uk = key.ToUniqueID();

        if (index.TryGetValue(in uk, out var pos))
            return store[pos].Value;

        index.Put(in uk) = store.Count;
        store.Add(new Entry { Key = key, Value = defaultValue });
        liveCount++;

        return defaultValue;
    }

    /// <summary>
    /// ES2026 §4.9.2 — Map.prototype.getOrInsertComputed(key, callback)
    /// Returns the value for key if present, otherwise calls callback(key),
    /// inserts the result, and returns it.
    /// </summary>
    [JSExport("getOrInsertComputed")]
    public JSValue GetOrInsertComputed(in Arguments a)
    {
        var (key, callbackfn) = a.Get2();
        if (!callbackfn.IsFunction)
            throw JSEngine.NewTypeError("getOrInsertComputed requires a callback function");

        key = CanonicalizeKey(key);
        HashedString uk = key.ToUniqueID();
        if (index.TryGetValue(in uk, out var pos))
            return store[pos].Value;

        var value = callbackfn.Call(JSUndefined.Value, key);
        // Re-check: the callback may have inserted this key already.
        if (index.TryGetValue(in uk, out pos))
        {
            var existing = store[pos];
            existing.Value = value;
            store[pos] = existing;
            return value;
        }

        index.Put(in uk) = store.Count;
        store.Add(new Entry { Key = key, Value = value });
        liveCount++;

        return value;
    }
}
