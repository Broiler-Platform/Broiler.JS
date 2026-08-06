using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Broiler.JavaScript.Ast.Misc;

namespace Broiler.JavaScript.Storage;

public enum ElementKind : byte
{
    Empty,
    Packed,
    Holey,
    Dictionary
}

/// <summary>
/// Indexed-property storage with explicit packed, holey, and sparse dictionary modes.
/// Dense modes keep properties contiguous; dictionary mode uses the radix map for lookup
/// plus a sorted live-key vector for allocation-free ECMAScript-order enumeration.
/// </summary>
/// <remarks>
/// Anything that is not a plain writable/enumerable/configurable data property moves the whole
/// array to dictionary mode, so the dense store never has to describe a descriptor: it holds
/// bare values, and the attributes, the absent accessors and the key are all implied by the
/// mode and the slot. A dense slot is therefore one reference rather than a 32-byte
/// <see cref="JSProperty"/>, and descriptors are rebuilt on the way out.
/// </remarks>
public struct ElementArray
{
    private const int InitialDenseCapacity = 4;
    private const uint MaximumDenseIndex = 1_048_575;
    private const int SparseDensityDivisor = 4;

    private IPropertyValue[] dense;
    private Dictionary<uint, JSProperty> dictionary;
    private SortedSet<uint> orderedDictionaryKeys;
    private ElementKind kind;
    private int liveCount;
    private bool hasCustomDescriptors;

    public uint Length { get; private set; }
    public readonly ElementKind Kind => kind;
    public readonly int Count => liveCount;
    public readonly int Capacity => kind == ElementKind.Dictionary ? dictionary?.Count ?? 0 : dense?.Length ?? 0;
    public readonly bool IsDense => kind is ElementKind.Packed or ElementKind.Holey;
    public readonly bool HasDefaultDescriptors => !hasCustomDescriptors;
    public readonly bool IsNull => kind == ElementKind.Empty;

    public void Put(
        uint index,
        IPropertyAccessor getter,
        IPropertyAccessor setter,
        JSPropertyAttributes attributes = JSPropertyAttributes.EnumerableConfigurableProperty)
        => Set(index, JSProperty.Property(getter, setter, attributes));

    public void Put(
        uint index,
        IPropertyValue value,
        JSPropertyAttributes attributes = JSPropertyAttributes.EnumerableConfigurableValue)
        => Set(index, JSProperty.Property(value, attributes));

    /// <summary>
    /// Compatibility ref path. Since the assigned descriptor is not observable until after
    /// return, it conservatively selects dictionary mode. Known data-property writes should
    /// use <see cref="Put(uint,IPropertyValue,JSPropertyAttributes)"/> or <see cref="Set"/>.
    /// </summary>
    public ref JSProperty Put(uint index)
    {
        TransitionToDictionary();
        hasCustomDescriptors = true;
        return ref PrepareDictionarySlot(index);
    }

    public void Set(uint index, in JSProperty property)
    {
        var custom = property.Attributes != JSPropertyAttributes.EnumerableConfigurableValue;

        if (!custom
            && kind != ElementKind.Dictionary
            && IsDenseRepresentable(in property)
            && !ShouldUseDictionary(index))
        {
            EnsureDenseCapacity(checked((int)index + 1));
            if (dense[index] == null)
                liveCount++;
            if (index >= Length)
                Length = index + 1;
            kind = liveCount == Length ? ElementKind.Packed : ElementKind.Holey;
            dense[index] = property.value;
            if (PropertyStorageMetrics.Enabled)
                PropertyStorageMetrics.RecordDenseWrite(property.value);
            return;
        }

        TransitionToDictionary();
        PrepareDictionarySlot(index) = property;
        hasCustomDescriptors |= custom;
    }

    /// <summary>
    /// Whether <see cref="Rebuild"/> can reproduce this property exactly from its value alone.
    /// A derived accessor is always either the value itself or null, so those two cases are the
    /// whole reconstructible set; a real setter or a missing value is not representable.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsDenseRepresentable(in JSProperty property)
        => property.value != null
            && property.set == null
            && (property.get == null || ReferenceEquals(property.get, property.value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static JSProperty Rebuild(uint index, IPropertyValue value)
        => JSProperty.Property(index, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly JSProperty Get(uint index) => TryGetValue(index, out var value) ? value : JSProperty.Empty;

    public JSProperty this[uint index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Get(index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool TryGetValue(uint index, out JSProperty value)
    {
        if (kind == ElementKind.Dictionary)
        {
            if (dictionary != null && dictionary.TryGetValue(index, out value))
                return true;
            value = JSProperty.Empty;
            return false;
        }
        if (dense != null && index < dense.Length && index < Length)
        {
            var stored = dense[index];
            if (stored != null)
            {
                if (PropertyStorageMetrics.Enabled)
                    PropertyStorageMetrics.RecordDenseRead(stored);
                value = Rebuild(index, stored);
                return true;
            }
        }
        value = JSProperty.Empty;
        return false;
    }

    public bool TryRemove(uint index, out JSProperty value)
    {
        if (!TryGetValue(index, out value))
            return false;
        return RemoveAt(index);
    }

    public bool RemoveAt(uint index)
    {
        if (kind == ElementKind.Dictionary)
        {
            if (dictionary == null || !dictionary.Remove(index))
                return false;
            RemoveOrderedKey(index);
            liveCount--;
            return true;
        }

        if (dense == null || index >= dense.Length || index >= Length || dense[index] == null)
            return false;

        dense[index] = null;
        liveCount--;
        kind = ElementKind.Holey;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool HasKey(uint index)
    {
        if (kind == ElementKind.Dictionary)
            return dictionary != null && dictionary.ContainsKey(index);
        return dense != null && index < dense.Length && index < Length && dense[index] != null;
    }

    public readonly ValueEnumerable AllValues() => new(this);
    public readonly ValueEnumerable StoredValues() => new(this);

    public void Resize(uint size)
    {
        if (kind == ElementKind.Dictionary || size == 0 || size > MaximumDenseIndex + 1)
            return;

        EnsureDenseCapacity(checked((int)size));
        if (kind == ElementKind.Empty)
            kind = ElementKind.Holey;
    }

    public bool TryCopyWithin(uint target, uint start, uint count, uint logicalLength)
    {
        if (!CanBulkMutate(logicalLength)
            || target > logicalLength || start > logicalLength
            || count > logicalLength - target || count > logicalLength - start)
            return false;

        System.Array.Copy(dense, start, dense, target, count);
        Length = Math.Max(Length, logicalLength);
        RecountDense(logicalLength);
        return true;
    }

    public bool TryFill(uint start, uint end, IPropertyValue value, uint logicalLength)
    {
        // A null would punch holes rather than fill them, so leave it to the generic path.
        if (value == null || !CanBulkMutate(logicalLength) || start > end || end > logicalLength)
            return false;

        System.Array.Fill(dense, value, checked((int)start), checked((int)(end - start)));
        Length = Math.Max(Length, logicalLength);
        RecountDense(logicalLength);
        return true;
    }

    public bool TryReverse(uint logicalLength)
    {
        if (!CanBulkMutate(logicalLength))
            return false;

        System.Array.Reverse(dense, 0, checked((int)logicalLength));
        Length = Math.Max(Length, logicalLength);
        return true;
    }

    private bool CanBulkMutate(uint logicalLength)
    {
        if (!IsDense || hasCustomDescriptors || logicalLength > MaximumDenseIndex + 1)
            return false;
        EnsureDenseCapacity(checked((int)logicalLength));
        return true;
    }

    private readonly bool ShouldUseDictionary(uint index)
    {
        if (index > MaximumDenseIndex)
            return true;
        if (index < 1024)
            return false;
        return (long)(liveCount + 1) * SparseDensityDivisor < (long)index + 1;
    }

    private void TransitionToDictionary()
    {
        if (kind == ElementKind.Dictionary)
            return;

        var keys = new SortedSet<uint>();
        if (dense != null)
        {
            var limit = Math.Min((uint)dense.Length, Length);
            for (uint i = 0; i < limit; i++)
            {
                var value = dense[i];
                if (value == null)
                    continue;
                dictionary ??= new Dictionary<uint, JSProperty>(liveCount);
                dictionary[i] = Rebuild(i, value);
                keys.Add(i);
            }
        }

        dense = null;
        dictionary ??= new Dictionary<uint, JSProperty>();
        orderedDictionaryKeys = keys;
        kind = ElementKind.Dictionary;
    }

    private ref JSProperty PrepareDictionarySlot(uint index)
    {
        dictionary ??= new Dictionary<uint, JSProperty>();
        ref var slot = ref CollectionsMarshal.GetValueRefOrAddDefault(dictionary, index, out var exists);
        if (!exists)
        {
            InsertOrderedKey(index);
            liveCount++;
        }
        if (index >= Length)
            Length = index + 1;
        return ref slot;
    }

    private void InsertOrderedKey(uint index)
    {
        (orderedDictionaryKeys ??= []).Add(index);
    }

    private void RemoveOrderedKey(uint index)
    {
        if (orderedDictionaryKeys == null)
            return;
        orderedDictionaryKeys.Remove(index);
    }

    private void EnsureDenseCapacity(int required)
    {
        if (required <= 0)
            return;
        if (dense == null)
        {
            var capacity = InitialDenseCapacity;
            while (capacity < required)
                capacity = checked(capacity * 2);
            dense = new IPropertyValue[capacity];
            return;
        }
        if (required <= dense.Length)
            return;

        var next = dense.Length;
        while (next < required)
            next = checked(next * 2);
        System.Array.Resize(ref dense, next);
    }

    private void RecountDense(uint logicalLength)
    {
        liveCount = 0;
        for (uint i = 0; i < logicalLength; i++)
        {
            if (dense[i] != null)
                liveCount++;
        }
        kind = liveCount == logicalLength ? ElementKind.Packed : ElementKind.Holey;
    }

    public void QuickSort(Comparison<IPropertyValue> comparer, uint start, uint end)
    {
        if (end - start < 30)
        {
            InsertionSort(comparer, start, end);
            return;
        }

        uint pivotIndex = start + (uint)(Random.Shared.NextDouble() * (end - start));
        var pivotValue = this[pivotIndex];
        Swap(pivotIndex, end);
        uint newPivotIndex = start;
        for (uint i = start; i < end; i++)
        {
            if (comparer(this[i].value, pivotValue.value) <= 0)
            {
                Swap(i, newPivotIndex);
                newPivotIndex++;
            }
        }
        Swap(end, newPivotIndex);
        if (newPivotIndex > start)
            QuickSort(comparer, start, newPivotIndex - 1);
        if (newPivotIndex < end)
            QuickSort(comparer, newPivotIndex + 1, end);
    }

    private void InsertionSort(Comparison<IPropertyValue> comparer, uint start, uint end)
    {
        for (uint i = start + 1; i <= end; i++)
        {
            var value = this[i];
            uint j;
            for (j = i - 1; j > start && comparer(this[j].value, value.value) > 0; j--)
                Set(j + 1, this[j]);
            if (j == start && comparer(this[j].value, value.value) > 0)
            {
                Set(j + 1, this[j]);
                j--;
            }
            Set(j + 1, value);
        }
    }

    private void Swap(uint left, uint right)
    {
        var value = this[left];
        Set(left, this[right]);
        Set(right, value);
    }

    public readonly struct ValueEnumerable : IEnumerable<(uint Key, JSProperty Value)>
    {
        private readonly ElementArray source;
        internal ValueEnumerable(ElementArray source) => this.source = source;
        public ValueEnumerator GetEnumerator() => new(source);
        IEnumerator<(uint Key, JSProperty Value)> IEnumerable<(uint Key, JSProperty Value)>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public struct ValueEnumerator : IEnumerator<(uint Key, JSProperty Value)>
    {
        private readonly ElementKind kind;
        private readonly IPropertyValue[] dense;
        private readonly Dictionary<uint, JSProperty> dictionary;
        private readonly SortedSet<uint> orderedKeys;
        private SortedSet<uint>.Enumerator orderedEnumerator;
        private readonly uint denseLimit;
        private int position;
        private (uint Key, JSProperty Value) current;

        internal ValueEnumerator(ElementArray source)
        {
            kind = source.kind;
            dense = source.dense;
            dictionary = source.dictionary;
            orderedKeys = source.orderedDictionaryKeys;
            orderedEnumerator = orderedKeys?.GetEnumerator() ?? default;
            denseLimit = source.dense == null ? 0 : Math.Min(source.Length, (uint)source.dense.Length);
            position = -1;
            current = default;
        }

        public readonly (uint Key, JSProperty Value) Current => current;
        readonly object IEnumerator.Current => current;

        public bool MoveNext()
        {
            if (kind == ElementKind.Dictionary)
            {
                while (orderedEnumerator.MoveNext())
                {
                    var key = orderedEnumerator.Current;
                    if (dictionary.TryGetValue(key, out var value))
                    {
                        current = (key, value);
                        return true;
                    }
                }
            }
            else
            {
                while (++position < denseLimit)
                {
                    var value = dense[position];
                    if (value != null)
                    {
                        current = ((uint)position, Rebuild((uint)position, value));
                        return true;
                    }
                }
            }

            current = default;
            return false;
        }

        public readonly void Dispose() { }
        public void Reset()
        {
            position = -1;
            orderedEnumerator = orderedKeys?.GetEnumerator() ?? default;
            current = default;
        }
    }
}
