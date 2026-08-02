using System;
using System.Runtime.CompilerServices;
using Broiler.JavaScript.Storage;

namespace Broiler.JavaScript.Runtime;

/// <summary>
/// Roadmap item 2-9: while an object's named properties are tracked by an
/// <see cref="ObjectShape"/>, they live in the shape alone and the radix trie holds nothing.
/// </summary>
/// <remarks>
/// A <c>PropertySequence</c> is a radix trie, and it charges 2.5–4.0 <c>JSObjectProperty</c>
/// nodes at 56 bytes each to store what is, for a shape-tracked object, an 8-byte reference —
/// 67–94% of a tracked object's per-property bytes. Everything the trie holds is already
/// somewhere else: key-to-slot is the shape, enumeration order is slot order, and the value is
/// <c>shapeSlots</c>. The one genuinely per-object remainder is the attribute set, which is a
/// <see cref="JSPropertyAttributes"/> — a byte. So a parallel byte array replaces the trie at
/// 24 + n bytes against ~150 per property.
/// <para>
/// The design is a one-way materialization boundary. An object starts <em>shape-only</em>: the
/// trie is empty and the shape is the truth. Anything that needs a real descriptor — an
/// accessor, a deferred cell, a delete, a mutable <see cref="GetOwnProperties"/> ref handed to
/// another assembly — calls <see cref="MaterializeNamedProperties"/>, which rebuilds the trie
/// from the shape and sets <see cref="namedPropertiesMaterialized"/> for good. After that the
/// object behaves exactly as it did before this item existed. **Worst case is therefore the old
/// behaviour**, which is what makes the change safe to land incrementally.
/// </para>
/// <para>
/// The invariant that makes shape-only mode answerable without the trie: <em>while an object is
/// shape-only, every key in its shape has a non-null <c>shapeSlots</c> entry and a plain data
/// attribute set.</em> Accessors, private names, non-data descriptors and null-slot keys (the
/// Annex B deferred cells 2-8 admits) all materialize before they are recorded, so none of them
/// can be reached through a shape-only read.
/// </para>
/// </remarks>
public partial class JSObject
{
    /// <summary>
    /// Attributes per shape slot, parallel to <c>shapeSlots</c>. The per-object half of what
    /// the trie used to hold; see the type remarks for why it cannot come from the shape.
    /// </summary>
    /// <remarks>
    /// A shape is <em>shared</em> by every object that reaches it, while
    /// <c>IsShapeTrackableData</c> admits any plain data property — writable, enumerable and
    /// configurable in any combination — so two objects at the same shape can disagree about
    /// every flag. That widening was deliberate (without it no prototype object could keep a
    /// shape), and it is exactly why attributes are per-object data a shape cannot carry.
    /// </remarks>
    private JSPropertyAttributes[] shapeAttributes = Array.Empty<JSPropertyAttributes>();

    /// <summary>
    /// Whether the property trie has been rebuilt and is now the truth. One-way: once set, the
    /// object is back to pre-2-9 behaviour and every write dual-writes as it always did.
    /// </summary>
    /// <remarks>
    /// A bit in the existing <see cref="status"/> word rather than a field of its own: a bool
    /// here measured at 8 bytes on every object once alignment is accounted for, which an
    /// object with no named properties would pay for nothing.
    /// </remarks>
    private bool namedPropertiesMaterialized
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (status & ObjectStatus.NamedPropertiesMaterialized) != 0;
    }

    /// <summary>
    /// Whether named properties currently live in the shape alone, so the trie may be neither
    /// read nor written for them.
    /// </summary>
    internal bool IsShapeOnlyStorage
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => !namedPropertiesMaterialized && SupportsShapeTracking && !objectShape.IsDictionary;
    }

    /// <summary>
    /// The property trie, guaranteed to hold every named property this object owns.
    /// </summary>
    /// <remarks>
    /// Every path that needs a descriptor rather than a value goes through this. It is the only
    /// way to reach <c>ownProperties</c> outside the shape-only fast paths in this file, which
    /// is what keeps the invariant checkable by reading one accessor's callers.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ref PropertySequence OwnProperties()
    {
        if (!namedPropertiesMaterialized)
            MaterializeNamedProperties();

        return ref ownProperties;
    }

    /// <summary>
    /// Rebuilds the property trie from the shape, after which this object stores its named
    /// properties the way it did before item 2-9.
    /// </summary>
    /// <remarks>
    /// Walks <see cref="ObjectShape.KeysInSlotOrder"/> so the trie is rebuilt in insertion
    /// order — <c>PropertySequence.Put</c> appends each new key at the tail, so replaying slot
    /// order reproduces the exact chain the eager path would have built, and
    /// OrdinaryOwnPropertyKeys reports the same order it always did.
    /// <para>
    /// The flag is set <em>first</em>. Rebuilding writes through <c>ownProperties</c> directly
    /// rather than through <see cref="OwnProperties"/>, but setting the flag up front also
    /// means a re-entrant call during the rebuild sees a materialized object rather than
    /// starting a second one.
    /// </para>
    /// </remarks>
    private void MaterializeNamedProperties()
    {
        status |= ObjectStatus.NamedPropertiesMaterialized;

        var shape = objectShape;
        if (!SupportsShapeTracking || shape.IsDictionary)
            return;

        var keys = shape.KeysInSlotOrder;
        for (var slot = 0; slot < keys.Length; slot++)
        {
            if ((uint)slot >= (uint)shapeSlots.Length)
                break;

            var value = shapeSlots[slot];
            if (value == null)
                continue;

            var key = KeyStrings.GetName(keys[slot]);
            ownProperties.Put(key.Key) = new JSProperty(key, value, shapeAttributes[slot]);
        }
    }

    /// <summary>
    /// Grows <c>shapeSlots</c> and <see cref="shapeAttributes"/> together so a slot index is
    /// always valid in both.
    /// </summary>
    private void EnsureShapeSlotCapacity(int slotCount)
    {
        if (shapeSlots.Length >= slotCount)
            return;

        var size = Math.Max(4, Math.Max(slotCount, shapeSlots.Length * 2));

        var replacementSlots = new JSValue[size];
        Array.Copy(shapeSlots, replacementSlots, shapeSlots.Length);
        shapeSlots = replacementSlots;

        var replacementAttributes = new JSPropertyAttributes[size];
        Array.Copy(shapeAttributes, replacementAttributes, shapeAttributes.Length);
        shapeAttributes = replacementAttributes;
    }

    /// <summary>
    /// The descriptor for a named key while this object is shape-only, without touching the
    /// trie. Returns false when the key is absent — which, in shape-only mode, the shape alone
    /// is proof of.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TryGetShapeOnlyProperty(uint key, out JSProperty property)
    {
        if (objectShape.TryGetSlot(key, out var slot)
            && (uint)slot < (uint)shapeSlots.Length)
        {
            var value = shapeSlots[slot];
            if (value != null)
            {
                property = new JSProperty(key, value, shapeAttributes[slot]);
                return true;
            }
        }

        property = JSProperty.Empty;
        return false;
    }

    /// <summary>
    /// Whether a shape-only object owns this named key.
    /// </summary>
    /// <remarks>
    /// A concrete shape is by itself proof of both presence and absence while an object is in
    /// shape mode, because its tracked keys ARE its complete set of own named properties. The
    /// null-slot case still has to be excluded here: a key recorded without a slot value is
    /// owned, but its descriptor lives in the trie, and such a key only exists on a
    /// materialized object.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool ShapeOnlyHasKey(uint key)
        => objectShape.TryGetSlot(key, out var slot)
            && (uint)slot < (uint)shapeSlots.Length
            && shapeSlots[slot] != null;

    /// <summary>
    /// Overwrites an existing writable data property on a shape-only object. Returns false when
    /// the key is absent or the write is not the kind a slot can take, leaving the caller to run
    /// the ordinary <c>[[Set]]</c>.
    /// </summary>
    /// <remarks>
    /// The value-only twin of <see cref="TryShapeOnlySetDataProperty"/>: an overwrite of an
    /// existing property moves the value and leaves the attributes exactly where they were,
    /// which is why nothing here writes <see cref="shapeAttributes"/>.
    /// </remarks>
    internal bool TryShapeOnlyOverwrite(in KeyString key, JSValue value)
    {
        if (value == null
            || !objectShape.TryGetSlot(key.Key, out var slot)
            || (uint)slot >= (uint)shapeSlots.Length
            || shapeSlots[slot] == null
            || !AllowsDirectShapeWrite(key.Key))
        {
            return false;
        }

        var attributes = shapeAttributes[slot];
        if ((attributes & JSPropertyAttributes.Value) == 0
            || (attributes & JSPropertyAttributes.Readonly) != 0)
        {
            return false;
        }

        shapeSlots[slot] = value;
        NotifyNamedPropertyMutation();
        PropertyChanged?.Invoke(this, (key.Key, uint.MaxValue, null));
        return true;
    }

    /// <summary>
    /// Records a plain data property on a shape-only object, keeping it out of the trie.
    /// Returns false when this object or this property cannot stay shape-only, leaving the
    /// caller to run the ordinary descriptor path.
    /// </summary>
    /// <remarks>
    /// This is the write that item 2-9 exists for: the whole reason a three-field object cost
    /// 840 bytes over an empty one is that each of these used to allocate trie nodes. The
    /// rejections are the invariant restated — a value that is not a <see cref="JSValue"/>, a
    /// non-data attribute set and a private name each need a real descriptor, so each falls
    /// through to a path that materializes first.
    /// </remarks>
    internal bool TryShapeOnlySetDataProperty(in KeyString key, JSValue value, JSPropertyAttributes attributes)
    {
        if (!IsShapeOnlyStorage
            || value == null
            || key.Metadata.IsPrivateName
            || !IsShapeTrackableData(attributes)
            || !AllowsDirectShapeWrite(key.Key))
        {
            return false;
        }

        if (!objectShape.TryGetSlot(key.Key, out var slot))
        {
            objectShape = objectShape.Add(key.Key);
            slot = objectShape.SlotCount - 1;
            EnsureShapeSlotCapacity(objectShape.SlotCount);
        }

        shapeSlots[slot] = value;
        shapeAttributes[slot] = attributes;
        NotifyNamedPropertyMutation();
        return true;
    }
}
