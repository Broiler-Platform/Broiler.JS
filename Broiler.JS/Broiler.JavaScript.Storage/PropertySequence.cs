using System;
using System.Runtime.CompilerServices;
using Broiler.JavaScript.Ast.Misc;

namespace Broiler.JavaScript.Storage;

public struct JSObjectProperty
{
    public JSProperty Property;
    public uint Next;

    public static JSObjectProperty Empty;
}

public delegate void Updater<TKey, TValue>(TKey key, ref TValue value);

public struct PropertySequence
{
    public readonly PropertyEnumerator GetEnumerator(bool showEnumerableOnly = true) => new(this, showEnumerableOnly);

    public struct PropertyEnumerator(PropertySequence sequence, bool showEnumerableOnly)
    {
        private SAUint32Map<JSObjectProperty> map = sequence.map;
        private readonly uint tail = sequence.tail;
        private uint start = sequence.head;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext(out JSProperty property)
        {
            while (start > 0)
            {
                ref var objP = ref map.GetRefOrDefault(start, ref JSObjectProperty.Empty);
                ref var p = ref objP.Property;

                if (p.IsEmpty)
                {
                    start = objP.Next;
                    continue;
                }

                if (showEnumerableOnly)
                {
                    if (!p.IsEnumerable)
                    {
                        start = objP.Next;
                        continue;
                    }
                }

                property = p;
                start = objP.Next;
                return true;
            }

            property = JSProperty.Empty;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext(out KeyString key, out JSProperty property)
        {
            while (start > 0)
            {
                ref var objP = ref map.GetRefOrDefault(start, ref JSObjectProperty.Empty);
                ref var p = ref objP.Property;
                if (p.IsEmpty)
                {
                    start = objP.Next;
                    continue;
                }
                if (showEnumerableOnly)
                {
                    if (!p.IsEnumerable)
                    {
                        start = objP.Next;
                        continue;
                    }
                }
                property = p;
                key = KeyStrings.GetName(start);
                start = objP.Next;
                return true;
            }

            property = JSProperty.Empty;
            key = KeyString.Empty;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNextKey(out KeyString key)
        {
            while (start > 0)
            {
                ref var objP = ref map.GetRefOrDefault(start, ref JSObjectProperty.Empty);
                ref var p = ref objP.Property;
                if (p.IsEmpty)
                {
                    start = objP.Next;
                    continue;
                }
                if (showEnumerableOnly)
                {
                    if (!p.IsEnumerable)
                    {
                        start = objP.Next;
                        continue;
                    }
                }
                key = KeyStrings.GetName(start);
                start = objP.Next;
                return true;
            }
            key = KeyString.Empty;
            return false;
        }
    }

    /// <summary>
    /// Static delegate factory for creating type errors when property deletion
    /// is attempted on read-only or non-configurable properties. Set by the
    /// Core assembly during initialization to produce the correct JavaScript
    /// TypeError exception. If not set, falls back to InvalidOperationException.
    /// </summary>
    public static Func<string, Exception>? TypeErrorFactory { get; set; }

    private SAUint32Map<JSObjectProperty> map;
    private uint head;
    private uint tail;

    public readonly bool IsEmpty => head == 0;

    /// <summary>
    /// Returns a reference to the internal map for use by enumerators
    /// that need direct access to the property storage.
    /// </summary>
    public readonly ref SAUint32Map<JSObjectProperty> GetMap() => ref Unsafe.AsRef(in map);

    /// <summary>
    /// Returns the head index for use by enumerators.
    /// </summary>
    public readonly uint Head => head;

    public void Update(Updater<uint, JSProperty> func)
    {
        var start = head;

        while (start > 0)
        {
            ref var objP = ref map.GetRefOrDefault(start, ref JSObjectProperty.Empty);
            ref var p = ref objP.Property;

            if (p.IsEmpty)
            {
                start = objP.Next;
                continue;
            }

            func(start, ref p);
            start = objP.Next;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasKey(uint key) => map.HasKey(key);

    public bool RemoveAt(uint key)
    {
        ref var objectProperty = ref map.GetRefOrDefault(key, ref JSObjectProperty.Empty);
        ref var property = ref objectProperty.Property;

        if (property.IsEmpty)
            return false;

        if (!property.IsConfigurable)
        {
            var factory = TypeErrorFactory;
            if (factory != null)
                throw factory($"Cannot delete property {KeyStrings.GetNameString(key)} of {this}");
            throw new InvalidOperationException($"Cannot delete property {KeyStrings.GetNameString(key)} of {this}");
        }

        // Unlink the node from the insertion-order chain. A property that is deleted
        // and later recreated must be treated as a NEW property — placed at the END of
        // the enumeration order (OrdinaryOwnPropertyKeys), not revived in its original
        // position; Put detects that via the now-empty Property and re-appends it at the
        // tail. The node's own Next is deliberately left pointing at its old successor so
        // an enumerator currently parked on this node (a for-in body that deletes the
        // property it is about to visit) can still advance past it instead of stranding
        // at a Next of 0 (test262 for-in/S12.6.4_A7_T1).
        var next = objectProperty.Next;
        if (head == key)
        {
            head = next;
            if (tail == key)
                tail = 0; // removed the only / last remaining linked property
        }
        else
        {
            var prev = head;
            while (prev != 0)
            {
                ref var prevNode = ref map.GetRefOrDefault(prev, ref JSObjectProperty.Empty);
                if (prevNode.Next == key)
                {
                    prevNode.Next = next;
                    if (tail == key)
                        tail = prev;
                    break;
                }

                prev = prevNode.Next;
            }
        }

        property = JSProperty.Empty;

        return true;
    }

    public ref JSProperty GetValue(uint key)
    {
        ref var objectProperty = ref map.GetRefOrDefault(key, ref JSObjectProperty.Empty);
        ref var property = ref objectProperty.Property;

        if (property.IsEmpty)
            return ref JSProperty.Empty;

        return ref property;
    }

    public bool TryGetValue(uint key, out JSProperty obj)
    {
        ref var objectProperty = ref map.GetRefOrDefault(key, ref JSObjectProperty.Empty);
        ref var property = ref objectProperty.Property;

        if (property.IsEmpty)
        {
            obj = JSProperty.Empty;
            return false;
        }

        obj = property;
        return true;
    }

    public void Put(uint key, IPropertyValue value, JSPropertyAttributes attributes = JSPropertyAttributes.EnumerableConfigurableValue) => Put(key) = JSProperty.Property(key, value, attributes);

    public void Put(in KeyString key, IPropertyValue value, JSPropertyAttributes attributes = JSPropertyAttributes.EnumerableConfigurableValue) => Put(key.Key) = JSProperty.Property(key, value, attributes);

    public void Put(in KeyString key, IPropertyAccessor getter, IPropertyAccessor setter, JSPropertyAttributes attributes = JSPropertyAttributes.EnumerableConfigurableProperty) => Put(key.Key) = JSProperty.Property(key, getter, setter, attributes);

    public ref JSProperty Put(uint key)
    {
        if (head == 0)
        {
            tail = head = key;
            ref var objP = ref map.Put(key);
            return ref objP.Property;
        }

        ref var @new = ref map.Put(key);

        // Append at the tail when this is a brand-new binding or a deleted one being
        // recreated — both have an empty Property here (the caller assigns it on the
        // returned ref). A live property being updated keeps its position. `tail != key`
        // guards against re-adding the current tail (which would create a self-loop).
        // RemoveAt leaves a deleted node's stale Next pointing at its old successor, so
        // reset it to 0 here now that it becomes the new tail.
        if (@new.Property.IsEmpty && tail != key)
        {
            @new.Next = 0;
            ref var last = ref map.GetRefOrDefault(tail, ref JSObjectProperty.Empty);
            last.Next = key;
            tail = key;
        }

        return ref @new.Property;
    }
}
