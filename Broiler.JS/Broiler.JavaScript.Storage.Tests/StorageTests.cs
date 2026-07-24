using Broiler.JavaScript.Ast;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Storage;

namespace Broiler.JavaScript.Storage.Tests;

public class StorageTests
{
    private sealed class TestValue(string name) : IPropertyValue
    {
        public override string ToString() => name;
    }

    [Fact]
    public void KeyStrings_GetOrCreate_ReturnsSameKeyForSameString()
    {
        var span1 = new StringSpan("testProp");
        var span2 = new StringSpan("testProp");
        var key1 = KeyStrings.GetOrCreate(span1);
        var key2 = KeyStrings.GetOrCreate(span2);
        Assert.Equal(key1.Key, key2.Key);
    }

    [Fact]
    public void KeyStrings_GetOrCreate_ReturnsDifferentKeysForDifferentStrings()
    {
        var key1 = KeyStrings.GetOrCreate(new StringSpan("alpha"));
        var key2 = KeyStrings.GetOrCreate(new StringSpan("beta"));
        Assert.NotEqual(key1.Key, key2.Key);
    }

    [Fact]
    public void VirtualMemory_Allocate_ReturnsNonEmptyArray()
    {
        var vm = new VirtualMemory<int>();
        var arr = vm.Allocate(5);
        Assert.False(arr.IsEmpty);
        Assert.Equal(5, arr.Length);
    }

    [Fact]
    public void VirtualMemory_Count_GrowsWithAllocations()
    {
        var vm = new VirtualMemory<int>();
        Assert.Equal(0, vm.Count);
        vm.Allocate(3);
        Assert.Equal(3, vm.Count);
        Assert.True(vm.Capacity >= vm.Count);
        var countAfterFirst = vm.Count;
        vm.Allocate(2);
        Assert.Equal(countAfterFirst + 2, vm.Count);
    }

    [Fact]
    public void SAUint32Map_EnumeratesOnlyLiveNodesAfterLargeReserve()
    {
        var map = new SAUint32Map<string>();
        map.Resize(1_000_000);
        map.Save(1, "one");
        map.Save(900_000, "large");
        Assert.True(map.RemoveAt(1));

        Assert.Equal(1, map.Count);
        Assert.True(map.Capacity >= 1_000_000);

        var enumerator = map.AllValues().GetEnumerator();
        Assert.True(enumerator.MoveNext());
        Assert.Equal((900_000u, "large"), enumerator.Current);
        Assert.False(enumerator.MoveNext());
    }

    [Fact]
    public void JSPropertyAttributes_FlagCombinations()
    {
        var attrs = JSPropertyAttributes.Value | JSPropertyAttributes.Enumerable | JSPropertyAttributes.Configurable;
        Assert.True(attrs.HasFlag(JSPropertyAttributes.Value));
        Assert.True(attrs.HasFlag(JSPropertyAttributes.Enumerable));
        Assert.True(attrs.HasFlag(JSPropertyAttributes.Configurable));
        Assert.False(attrs.HasFlag(JSPropertyAttributes.Readonly));
    }

    [Fact]
    public void KeyStrings_PublishesImmutableIndexAndPrivateMetadata()
    {
        var index = KeyStrings.GetOrCreate("4294967294");
        Assert.True(index.Metadata.IsArrayIndex);
        Assert.Equal(4_294_967_294u, index.Metadata.ArrayIndex);
        Assert.True(index.Metadata.IsCanonicalNumericIndex);

        Assert.False(KeyStrings.GetOrCreate("4294967295").Metadata.IsArrayIndex);
        Assert.False(KeyStrings.GetOrCreate("01").Metadata.IsArrayIndex);
        Assert.True(KeyStrings.GetOrCreate("\u0001#secret").Metadata.IsPrivateName);
        Assert.Equal(index.Metadata.StableOrdinalHash, KeyStrings.GetOrCreate("4294967294").Metadata.StableOrdinalHash);
    }

    [Fact]
    public void KeyStrings_ConcurrentHitsPublishOneKey()
    {
        var ids = new uint[256];
        Parallel.For(0, ids.Length, i => ids[i] = KeyStrings.GetOrCreate("contended-key").Key);
        Assert.All(ids, id => Assert.Equal(ids[0], id));
        Assert.Equal("contended-key", KeyStrings.GetNameString(ids[0]).Value);
    }

    [Fact]
    public void PropertySequence_DeleteAndReaddMovesKeyToTail()
    {
        var sequence = new PropertySequence();
        var a = KeyStrings.GetOrCreate("phase2-a");
        var b = KeyStrings.GetOrCreate("phase2-b");
        var c = KeyStrings.GetOrCreate("phase2-c");
        sequence.Put(a, new TestValue("a"));
        sequence.Put(b, new TestValue("b"));
        sequence.Put(c, new TestValue("c"));

        Assert.True(sequence.RemoveAt(b.Key));
        Assert.False(sequence.HasKey(b.Key));
        sequence.Put(b, new TestValue("b2"));

        var keys = new List<uint>();
        var enumerator = sequence.GetEnumerator(false);
        while (enumerator.MoveNext(out var key, out _))
            keys.Add(key.Key);
        Assert.Equal([a.Key, c.Key, b.Key], keys);
    }

    [Fact]
    public void ElementArray_TransitionsPackedHoleyAndDictionaryInOrder()
    {
        var elements = new ElementArray();
        elements.Put(0, new TestValue("zero"));
        elements.Put(1, new TestValue("one"));
        Assert.Equal(ElementKind.Packed, elements.Kind);

        Assert.True(elements.RemoveAt(0));
        Assert.Equal(ElementKind.Holey, elements.Kind);

        elements.Put(1_000_000, new TestValue("sparse"));
        Assert.Equal(ElementKind.Dictionary, elements.Kind);
        Assert.Equal(2, elements.Count);

        var keys = new List<uint>();
        foreach (var (key, _) in elements.AllValues())
            keys.Add(key);
        Assert.Equal([1u, 1_000_000u], keys);
    }

    [Fact]
    public void ElementArray_CustomDescriptorSelectsDictionaryMode()
    {
        var elements = new ElementArray();
        elements.Put(0, new TestValue("readonly"), JSPropertyAttributes.EnumerableReadonlyValue);
        Assert.Equal(ElementKind.Dictionary, elements.Kind);
        Assert.False(elements.HasDefaultDescriptors);
    }

    // Regression for issue #1428: StringMap's "not found" sentinel node was a
    // shared mutable static. A create-path overflow could hand that sentinel to
    // a caller that wrote a (large, cumulative) value into it; afterwards a fresh
    // map (storage==null) returned the sentinel and reported a false hit with the
    // stale value for ANY key, without ever matching it. In the engine that made
    // a new script's key resolve to a stale index while its key List stayed
    // empty, so ScriptInfo.Indices (sized to List.Count) was indexed out of
    // bounds at runtime. Fill one StringArray heavily to exercise the overflow,
    // then assert a fresh StringArray is fully independent.
    [Fact]
    public void StringArray_FreshInstance_IsNotPoisonedByHeavyPriorInstance()
    {
        var heavy = new StringArray();
        for (int i = 0; i < 200_000; i++)
            heavy.GetOrAdd(new StringSpan("heavy_key_" + i));

        var fresh = new StringArray();
        Assert.Equal(0u, fresh.GetOrAdd(new StringSpan("alpha")));
        Assert.Equal(1u, fresh.GetOrAdd(new StringSpan("beta")));
        Assert.Equal(0u, fresh.GetOrAdd(new StringSpan("alpha"))); // stable, no re-add
        Assert.Equal(2, fresh.List.Count);
    }
}
