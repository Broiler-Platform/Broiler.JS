using Broiler.JavaScript.BuiltIns.Array;
using Broiler.JavaScript.BuiltIns.Boolean;
using Broiler.JavaScript.BuiltIns.Iterator;
using Broiler.JavaScript.ExpressionCompiler;
using System;
using System.Collections.Generic;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Extensions;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Set;


[JSClassGenerator("Set")]
public partial class JSSet : JSObject
{
    private readonly record struct SetLikeRecord(int Size, Func<JSValue, bool> Has, Func<IElementEnumerator> GetKeys);

    // Entries are appended to an ordered list and never physically removed while live
    // iterators may be in flight; deletion/clear set the slot to null (a tombstone)
    // instead. Iteration walks the list by index so that entries added during iteration
    // are observed and removed entries are skipped, matching the spec's [[SetData]]
    // semantics (and avoiding "Collection was modified" exceptions from foreach).
    private List<JSValue> store = new();
    private StringMap<int> index;
    private int liveCount;

    [JSExport]
    public int Size => liveCount;

    public JSSet(in Arguments a) : base(JSEngine.NewTargetPrototype)
    {
        var iterable = a.Get1();
        if (iterable.IsNullOrUndefined)
            return;

        var adderTarget =
            (((JSEngine.Current as IJSExecutionContext)?.CurrentNewTarget as IJSFunction)?.Prototype as JSValue)
            ?? JSEngine.NewTargetPrototype
            ?? this;
        if (adderTarget[KeyStrings.GetOrCreate("add")] is not IJSFunction adder)
            throw JSEngine.NewTypeError("Set instance 'add' property is not callable");

        var en = iterable.GetIterableEnumerator();
        while (en.MoveNext(out var item))
        {
            try
            {
                adder.InvokeFunction(new Arguments(this, item));
            }
            catch
            {
                JSIteratorObject.CloseIteratorIfPossible(en);
                throw;
            }
        }
    }

    [JSExport("add")]
    public JSValue Add(JSValue key)
    {
        HashedString uk = key.ToUniqueID();

        if (!index.TryGetValue(in uk, out _))
        {
            index.Put(in uk) = store.Count;
            store.Add(key);
            liveCount++;
        }

        // Set.prototype.add returns the Set itself (not the value) so calls can
        // be chained: `set.add(1).add(2)` (test262 Set/prototype/add/*).
        return this;
    }

    [JSExport("clear")]
    public JSValue Set(in Arguments a)
    {
        // Preserve the backing list (live iterators keep their position) but tombstone
        // every slot so they observe the entries as removed.
        for (var i = 0; i < store.Count; i++)
            store[i] = null;

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
            store[pos] = null;
            index.TryRemove(uk.Value, out _);
            liveCount--;
            return JSBoolean.True;
        }

        return JSBoolean.False;
    }

    private bool Contains(JSValue key)
    {
        HashedString uk = key.ToUniqueID();
        return index.TryGetValue(in uk, out _);
    }

    private bool Remove(JSValue key)
    {
        HashedString uk = key.ToUniqueID();
        if (!index.TryGetValue(in uk, out var pos))
            return false;

        store[pos] = null;
        index.TryRemove(uk.Value, out _);
        liveCount--;
        return true;
    }

    private static SetLikeRecord GetSetLikeRecord(JSValue other, string methodName)
    {
        if (!other.IsObject)
            throw JSEngine.NewTypeError($"Set.prototype.{methodName} requires a Set or set-like object argument");

        if (other is JSSet otherSet)
        {
            return new SetLikeRecord(
                otherSet.Size,
                otherSet.Contains,
                () => new StoreEnumerator(otherSet.store));
        }

        // GetSetRecord: the "size" property is read and coerced to a number
        // (observably invoking valueOf/toString) BEFORE "has" and "keys" are
        // read. Reading those methods first would surface the size coercion in
        // the wrong order (test262 staging/sm/Set property-access-order checks).
        var sizeValue = other["size"];
        var numSize = sizeValue.DoubleValue;
        if (double.IsNaN(numSize))
            throw JSEngine.NewTypeError($"Set.prototype.{methodName} requires a Set or set-like object argument");
        if (numSize < 0)
            throw JSEngine.NewRangeError($"Set.prototype.{methodName} requires the set-like object's size to be non-negative");

        var hasMethod = other["has"];
        if (!hasMethod.IsFunction)
            throw JSEngine.NewTypeError($"Set.prototype.{methodName} requires a Set or set-like object argument");

        var keysMethod = other["keys"];
        if (!keysMethod.IsFunction)
            throw JSEngine.NewTypeError($"Set.prototype.{methodName} requires a Set or set-like object argument");

        return new SetLikeRecord(
            double.IsPositiveInfinity(numSize) ? int.MaxValue : (int)numSize,
            value => hasMethod.Call(other, value).BooleanValue,
            () =>
            {
                var iterator = keysMethod.Call(other);
                if (iterator is not JSObject iteratorObject)
                    throw JSEngine.NewTypeError($"Set.prototype.{methodName} requires keys() to return an object");

                return new JSIterator(iteratorObject);
            });
    }

    private sealed class StoreEnumerator(List<JSValue> source) : IElementEnumerator
    {
        private int position = -1;
        private uint index;

        private bool MoveNextNode(out JSValue value, out uint currentIndex)
        {
            // Walk by index and skip tombstones so the enumerator tolerates mutation of
            // the underlying set during iteration.
            while (true)
            {
                position++;
                if (position >= source.Count)
                {
                    value = JSUndefined.Value;
                    currentIndex = 0;
                    return false;
                }

                var entry = source[position];
                if (entry is null)
                    continue;

                value = entry;
                currentIndex = index++;
                return true;
            }
        }

        public bool MoveNext(out bool hasValue, out JSValue value, out uint index)
        {
            hasValue = MoveNextNode(out value, out index);
            return hasValue;
        }

        public bool MoveNext(out JSValue value)
            => MoveNextNode(out value, out _);

        public bool MoveNextOrDefault(out JSValue value, JSValue @default)
        {
            if (MoveNextNode(out value, out _))
                return true;

            value = @default;
            return false;
        }

        public JSValue NextOrDefault(JSValue @default)
            => MoveNextNode(out var value, out _) ? value : @default;
    }

    [JSExport("entries")]
    public IEnumerable<JSValue> GetEntries()
    {
        for (var i = 0; i < store.Count; i++)
        {
            var entry = store[i];
            if (entry is null)
                continue;

            yield return new JSArray(entry, entry);
        }
    }

    [JSExport("forEach")]
    public JSValue ForEach(in Arguments a)
    {
        var fx = a.Get1();
        if (!fx.IsFunction)
            throw JSEngine.NewTypeError($"Function parameter expected");

        var @this = a.This ?? this;

        for (var i = 0; i < store.Count; i++)
        {
            var e = store[i];
            if (e is null)
                continue;

            fx.Call(@this, e, e, this);
        }

        return JSUndefined.Value;
    }

    [JSExport("has")]
    public JSValue Has(in Arguments a)
    {
        var f = a.Get1();
        HashedString uk = f.ToUniqueID();

        if (index.TryGetValue(in uk, out var i))
            return JSBoolean.True;

        return JSBoolean.False;
    }

    [JSExport("keys")]
    public IEnumerable<JSValue> Keys()
    {
        for (var i = 0; i < store.Count; i++)
        {
            var entry = store[i];
            if (entry is null)
                continue;

            yield return entry;
        }
    }


    [JSExport("values")]
    public IEnumerable<JSValue> Values()
    {
        for (var i = 0; i < store.Count; i++)
        {
            var entry = store[i];
            if (entry is null)
                continue;

            yield return entry;
        }
    }

    [JSExport("union")]
    public JSValue Union(in Arguments a)
    {
        var other = GetSetLikeRecord(a.Get1(), "union");

        var result = new JSSet(Arguments.Empty);
        for (var i = 0; i < store.Count; i++)
        {
            var item = store[i];
            if (item is not null)
                result.Add(item);
        }

        var keys = other.GetKeys();
        while (keys.MoveNext(out var item))
        {
            result.Add(item);
        }

        return result;
    }

    [JSExport("intersection")]
    public JSValue Intersection(in Arguments a)
    {
        var other = GetSetLikeRecord(a.Get1(), "intersection");

        var result = new JSSet(Arguments.Empty);
        // Spec selects the smaller collection to drive iteration: when this set
        // is no larger than the argument, probe the argument's `has`; otherwise
        // iterate the argument's keys() and probe this set. The argument's `has`
        // must not be called in the keys() branch (test262 staging/sm/Set order
        // checks use a `has` that throws). Result order follows the driver.
        if (Size <= other.Size)
        {
            for (var i = 0; i < store.Count; i++)
            {
                var item = store[i];
                if (item is not null && other.Has(item) && Contains(item))
                    result.Add(item);
            }
        }
        else
        {
            var keys = other.GetKeys();
            while (keys.MoveNext(out var item))
            {
                if (Contains(item))
                    result.Add(item);
            }
        }

        return result;
    }

    [JSExport("difference")]
    public JSValue Difference(in Arguments a)
    {
        var other = GetSetLikeRecord(a.Get1(), "difference");

        // Start from a copy of this set (preserving insertion order), then remove
        // the argument's members. When this set is no larger than the argument,
        // probe the argument's `has`; otherwise iterate the argument's keys() and
        // remove each — the argument's `has` must not be called in that branch.
        // The has-branch iterates the snapshot taken here, not the live store: a
        // user `has` may mutate this set mid-operation (test262 clears it and adds
        // new keys), and the spec operates over the copy of the original data.
        var snapshot = new List<JSValue>(liveCount);
        for (var i = 0; i < store.Count; i++)
        {
            var item = store[i];
            if (item is not null)
                snapshot.Add(item);
        }

        var result = new JSSet(Arguments.Empty);
        foreach (var item in snapshot)
            result.Add(item);

        if (Size <= other.Size)
        {
            foreach (var item in snapshot)
            {
                if (other.Has(item))
                    result.Remove(item);
            }
        }
        else
        {
            var keys = other.GetKeys();
            while (keys.MoveNext(out var item))
                result.Remove(item);
        }

        return result;
    }

    [JSExport("symmetricDifference")]
    public JSValue SymmetricDifference(in Arguments a)
    {
        var other = GetSetLikeRecord(a.Get1(), "symmetricDifference");

        var result = new JSSet(Arguments.Empty);
        for (var i = 0; i < store.Count; i++)
        {
            var item = store[i];
            if (item is not null)
                result.Add(item);
        }

        var keys = other.GetKeys();
        while (keys.MoveNext(out var item))
        {
            if (!result.Remove(item))
                result.Add(item);
        }

        return result;
    }

    [JSExport("isSubsetOf")]
    public JSValue IsSubsetOf(in Arguments a)
    {
        var other = GetSetLikeRecord(a.Get1(), "isSubsetOf");

        // A set cannot be a subset of a smaller one; bail before probing `has`
        // (the argument's `has` must not be called in that case).
        if (Size > other.Size)
            return JSBoolean.False;

        for (var i = 0; i < store.Count; i++)
        {
            var item = store[i];
            if (item is not null && !other.Has(item))
                return JSBoolean.False;
        }

        return JSBoolean.True;
    }

    [JSExport("isSupersetOf")]
    public JSValue IsSupersetOf(in Arguments a)
    {
        var other = GetSetLikeRecord(a.Get1(), "isSupersetOf");

        if (other.Size > Size)
            return JSBoolean.False;

        var keys = other.GetKeys();
        while (keys.MoveNext(out var item))
        {
            if (!Contains(item))
            {
                JSIteratorObject.CloseIteratorIfPossible(keys);
                return JSBoolean.False;
            }
        }

        return JSBoolean.True;
    }

    [JSExport("isDisjointFrom")]
    public JSValue IsDisjointFrom(in Arguments a)
    {
        var other = GetSetLikeRecord(a.Get1(), "isDisjointFrom");

        if (store == null)
            return JSBoolean.True;

        // Per spec: when this set is no larger than the argument, iterate this
        // set's elements and probe other.[[Has]] — the argument's keys iterator
        // must NOT be touched in that case. Otherwise iterate the argument's keys.
        if (Size <= other.Size)
        {
            for (var i = 0; i < store.Count; i++)
            {
                var item = store[i];
                if (item is not null && other.Has(item))
                    return JSBoolean.False;
            }

            return JSBoolean.True;
        }

        var keys = other.GetKeys();
        while (keys.MoveNext(out var item))
        {
            if (Contains(item))
            {
                JSIteratorObject.CloseIteratorIfPossible(keys);
                return JSBoolean.False;
            }
        }

        return JSBoolean.True;
    }
}
