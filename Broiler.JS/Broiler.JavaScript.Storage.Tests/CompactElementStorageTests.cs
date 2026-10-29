using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Storage;

namespace Broiler.JavaScript.Storage.Tests;

// The dense half of ElementArray stores bare values rather than 32-byte JSProperty
// descriptors (docs/performance-roadmap.md P2-3). That only works because anything which is
// not a plain writable/enumerable/configurable data property moves the whole array to
// dictionary mode first, so a dense slot never has to describe anything: the attributes,
// the absent accessors and the key are all implied and the descriptor is rebuilt on the way
// out. Everything below pins that invariant and the round-trip.
public class CompactElementStorageTests
{
    private sealed class TestValue(string name) : IPropertyValue
    {
        public override string ToString() => name;
    }

    private sealed class TestAccessor(string name) : IPropertyAccessor
    {
        public override string ToString() => name;
    }

    // ── the invariant the compact store depends on ───────────────────────────────────

    [Theory]
    [InlineData(JSPropertyAttributes.EnumerableReadonlyValue)]
    [InlineData(JSPropertyAttributes.ConfigurableValue)]
    [InlineData(JSPropertyAttributes.Value)]
    [InlineData(JSPropertyAttributes.EnumerableConfigurableProperty)]
    public void AnyNonDefaultDescriptorLeavesDenseMode(JSPropertyAttributes attributes)
    {
        var elements = new ElementArray();
        elements.Put(0, new TestValue("plain"));
        Assert.Equal(ElementKind.Packed, elements.Kind);

        elements.Put(1, new TestValue("custom"), attributes);

        Assert.Equal(ElementKind.Dictionary, elements.Kind);
        Assert.False(elements.HasDefaultDescriptors);
    }

    [Fact]
    public void AnAccessorPairLeavesDenseMode()
    {
        var elements = new ElementArray();
        elements.Put(0, new TestValue("plain"));
        elements.Put(1, new TestAccessor("get"), new TestAccessor("set"));

        Assert.Equal(ElementKind.Dictionary, elements.Kind);
        Assert.False(elements.HasDefaultDescriptors);
    }

    [Fact]
    public void PlainValuesInDictionaryModeStillCountAsDefaultDescriptors()
    {
        // Going sparse is a storage decision, not a descriptor one: `HasDefaultDescriptors`
        // must keep reporting true so the bulk-mutation fast paths stay eligible if the
        // array ever comes back.
        var elements = new ElementArray();
        elements.Put(0, new TestValue("zero"));
        elements.Put(1_000_000, new TestValue("sparse"));

        Assert.Equal(ElementKind.Dictionary, elements.Kind);
        Assert.True(elements.HasDefaultDescriptors);
    }

    // ── the descriptor survives the round-trip ───────────────────────────────────────

    [Fact]
    public void ADenseSlotRebuildsTheFullDefaultDescriptor()
    {
        var value = new TestValue("v");
        var elements = new ElementArray();
        elements.Put(3, value);

        var property = elements.Get(3);

        Assert.False(property.IsEmpty);
        Assert.Same(value, property.value);
        Assert.Equal(JSPropertyAttributes.EnumerableConfigurableValue, property.Attributes);
        Assert.True(property.IsValue);
        Assert.True(property.IsEnumerable);
        Assert.True(property.IsConfigurable);
        Assert.False(property.IsReadOnly);
        Assert.Null(property.set);
    }

    [Fact]
    public void ADenseSlotHoldingAnAccessorRebuildsItsGetter()
    {
        // A value that happens to also be an accessor keeps the derived `get` the descriptor
        // constructors would have given it, so a rebuilt slot is indistinguishable.
        var value = new TestAccessor("callable");
        var elements = new ElementArray();
        elements.Set(0, JSProperty.Property(value));

        var property = elements.Get(0);

        Assert.Same(value, property.value);
        Assert.Same(value, property.get);
        Assert.Null(property.set);
    }

    [Fact]
    public void ADenseSlotHoldingANonAccessorHasNoGetter()
    {
        var elements = new ElementArray();
        elements.Put(0, new TestValue("plain"));

        Assert.Null(elements.Get(0).get);
    }

    [Fact]
    public void TheRebuiltDescriptorCarriesItsOwnIndexAsTheKey()
    {
        var elements = new ElementArray();
        elements.Put(7, new TestValue("seven"));

        Assert.Equal(7u, elements.Get(7).key);
    }

    [Fact]
    public void EveryReadPathAgreesOnADenseSlot()
    {
        var value = new TestValue("v");
        var elements = new ElementArray();
        elements.Put(2, value);

        Assert.True(elements.TryGetValue(2, out var viaTry));
        Assert.True(elements.HasKey(2));
        Assert.Same(value, viaTry.value);
        Assert.Same(value, elements.Get(2).value);
        Assert.Same(value, elements[2].value);

        var enumerated = new List<(uint Key, JSProperty Value)>();
        foreach (var entry in elements.AllValues())
            enumerated.Add(entry);
        Assert.Same(value, Assert.Single(enumerated).Value.value);
    }

    // ── holes ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AHoleIsEmptyOnEveryReadPath()
    {
        var elements = new ElementArray();
        elements.Put(0, new TestValue("zero"));
        elements.Put(1, new TestValue("one"));
        Assert.True(elements.RemoveAt(0));

        Assert.True(elements.Get(0).IsEmpty);
        Assert.True(elements[0].IsEmpty);
        Assert.False(elements.TryGetValue(0, out _));
        Assert.False(elements.HasKey(0));
        Assert.Equal(1, elements.Count);
        Assert.Equal(ElementKind.Holey, elements.Kind);
    }

    [Fact]
    public void RefillingAHoleRepacks()
    {
        var elements = new ElementArray();
        elements.Put(0, new TestValue("zero"));
        elements.Put(1, new TestValue("one"));
        elements.RemoveAt(0);
        elements.Put(0, new TestValue("back"));

        Assert.Equal(ElementKind.Packed, elements.Kind);
        Assert.Equal(2, elements.Count);
        Assert.Equal("back", elements.Get(0).value.ToString());
    }

    [Fact]
    public void EnumerationSkipsHoles()
    {
        var elements = new ElementArray();
        for (uint i = 0; i < 5; i++)
            elements.Put(i, new TestValue(i.ToString()));
        elements.RemoveAt(1);
        elements.RemoveAt(3);

        var keys = new List<uint>();
        foreach (var (key, _) in elements.AllValues())
            keys.Add(key);

        Assert.Equal([0u, 2u, 4u], keys);
    }

    [Fact]
    public void WritingPastTheEndLeavesHolesNotStaleValues()
    {
        var elements = new ElementArray();
        elements.Put(0, new TestValue("zero"));
        elements.Put(4, new TestValue("four"));

        Assert.Equal(5u, elements.Length);
        Assert.Equal(2, elements.Count);
        Assert.Equal(ElementKind.Holey, elements.Kind);
        for (uint i = 1; i < 4; i++)
            Assert.False(elements.HasKey(i));
    }

    // ── the transition out of the compact store ──────────────────────────────────────

    [Fact]
    public void PromotionToDictionaryPreservesEveryDenseValue()
    {
        var elements = new ElementArray();
        for (uint i = 0; i < 8; i++)
            elements.Put(i, new TestValue(i.ToString()));
        elements.RemoveAt(3);

        // A readonly descriptor forces the whole array over.
        elements.Put(8, new TestValue("custom"), JSPropertyAttributes.EnumerableReadonlyValue);
        Assert.Equal(ElementKind.Dictionary, elements.Kind);

        for (uint i = 0; i < 8; i++)
        {
            if (i == 3)
            {
                Assert.False(elements.HasKey(i));
                continue;
            }

            var property = elements.Get(i);
            Assert.Equal(i.ToString(), property.value.ToString());
            Assert.Equal(JSPropertyAttributes.EnumerableConfigurableValue, property.Attributes);
        }

        Assert.Equal(8, elements.Count);
        Assert.True(elements.Get(8).IsReadOnly);
    }

    [Fact]
    public void PromotionKeepsAscendingKeyOrder()
    {
        var elements = new ElementArray();
        for (uint i = 0; i < 4; i++)
            elements.Put(i, new TestValue(i.ToString()));
        elements.Put(2_000_000, new TestValue("sparse"));

        var keys = new List<uint>();
        foreach (var (key, _) in elements.AllValues())
            keys.Add(key);

        Assert.Equal([0u, 1u, 2u, 3u, 2_000_000u], keys);
    }

    [Fact]
    public void OverwritingADenseSlotWithACustomDescriptorReplacesIt()
    {
        var elements = new ElementArray();
        elements.Put(0, new TestValue("first"));
        elements.Put(0, new TestValue("second"), JSPropertyAttributes.EnumerableReadonlyValue);

        Assert.Equal(1, elements.Count);
        var property = elements.Get(0);
        Assert.Equal("second", property.value.ToString());
        Assert.True(property.IsReadOnly);
    }

    // ── bulk mutation over the compact store ─────────────────────────────────────────

    [Fact]
    public void FillOverwritesTheRequestedRangeOnly()
    {
        var elements = new ElementArray();
        for (uint i = 0; i < 4; i++)
            elements.Put(i, new TestValue(i.ToString()));

        Assert.True(elements.TryFill(1, 3, new TestValue("x"), 4));

        Assert.Equal(["0", "x", "x", "3"], Read(elements, 4));
        Assert.Equal(4, elements.Count);
        Assert.Equal(ElementKind.Packed, elements.Kind);
    }

    [Fact]
    public void FillRebuildsFullDescriptors()
    {
        var elements = new ElementArray();
        for (uint i = 0; i < 2; i++)
            elements.Put(i, new TestValue(i.ToString()));
        Assert.True(elements.TryFill(0, 2, new TestValue("x"), 2));

        Assert.Equal(JSPropertyAttributes.EnumerableConfigurableValue, elements.Get(1).Attributes);
        Assert.True(elements.HasDefaultDescriptors);
    }

    [Fact]
    public void FillRefusesANullValueRatherThanPunchingHoles()
    {
        var elements = new ElementArray();
        elements.Put(0, new TestValue("zero"));

        Assert.False(elements.TryFill(0, 1, null, 1));
        Assert.Equal("zero", elements.Get(0).value.ToString());
    }

    [Fact]
    public void CopyWithinMovesValues()
    {
        var elements = new ElementArray();
        for (uint i = 0; i < 5; i++)
            elements.Put(i, new TestValue(i.ToString()));

        Assert.True(elements.TryCopyWithin(0, 3, 2, 5));

        Assert.Equal(["3", "4", "2", "3", "4"], Read(elements, 5));
    }

    [Fact]
    public void ReverseFlipsValues()
    {
        var elements = new ElementArray();
        for (uint i = 0; i < 4; i++)
            elements.Put(i, new TestValue(i.ToString()));

        Assert.True(elements.TryReverse(4));

        Assert.Equal(["3", "2", "1", "0"], Read(elements, 4));
    }

    [Fact]
    public void BulkMutationIsRefusedOnceDescriptorsAreCustom()
    {
        var elements = new ElementArray();
        elements.Put(0, new TestValue("zero"));
        elements.Put(1, new TestValue("one"), JSPropertyAttributes.EnumerableReadonlyValue);

        Assert.False(elements.TryReverse(2));
        Assert.False(elements.TryFill(0, 2, new TestValue("x"), 2));
        Assert.False(elements.TryCopyWithin(0, 1, 1, 2));
    }

    [Fact]
    public void SortOrdersValues()
    {
        var elements = new ElementArray();
        foreach (var (index, name) in new (uint, string)[] { (0, "d"), (1, "b"), (2, "c"), (3, "a") })
            elements.Put(index, new TestValue(name));

        elements.QuickSort(static (x, y) => string.CompareOrdinal(x.ToString(), y.ToString()), 0, 3);

        Assert.Equal(["a", "b", "c", "d"], Read(elements, 4));
    }

    // ── growth ───────────────────────────────────────────────────────────────────────

    [Fact]
    public void AResizedArrayIsEmptyButPreallocated()
    {
        var elements = new ElementArray();
        elements.Resize(1000);

        Assert.Equal(0, elements.Count);
        Assert.Equal(0u, elements.Length);
        Assert.True(elements.Capacity >= 1000);
        Assert.False(elements.HasKey(0));
        Assert.True(elements.Get(0).IsEmpty);
    }

    [Fact]
    public void GrowingBeyondCapacityKeepsEveryValue()
    {
        var elements = new ElementArray();
        for (uint i = 0; i < 300; i++)
            elements.Put(i, new TestValue(i.ToString()));

        Assert.Equal(300, elements.Count);
        Assert.Equal(ElementKind.Packed, elements.Kind);
        for (uint i = 0; i < 300; i++)
            Assert.Equal(i.ToString(), elements.Get(i).value.ToString());
    }

    private static List<string> Read(ElementArray elements, uint count)
    {
        var values = new List<string>();
        for (uint i = 0; i < count; i++)
            values.Add(elements.Get(i).value?.ToString());
        return values;
    }
}
