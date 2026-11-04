using Broiler.JavaScript.Storage;

namespace Broiler.JavaScript.Runtime;

/// <summary>
/// Enumerator for iterating over property values in a <see cref="PropertySequence"/>.
/// This type was extracted from <see cref="PropertySequence"/> because it depends on
/// <see cref="JSObject"/> and <see cref="JSValue"/> (Core runtime types), while
/// <see cref="PropertySequence"/> itself lives in the Storage assembly.
/// </summary>
/// <remarks>
/// The walk reads the target's property map <em>live</em> on every step rather than through a
/// copy taken when the enumerator was made. <see cref="SAUint32Map{T}"/> is a struct over a
/// growable backing array, and adding a property can both grow that array (the copy keeps the
/// old one) and relocate an existing node to make room for a smaller key: the old slot is
/// overwritten with the new key <em>before</em> the displaced node is written to the grown
/// array. A stale copy then no longer finds the displaced key, reads its <c>Next</c> as 0 and
/// ends the walk early — which node is displaced depends on the order keys were interned, so a
/// for-in body that adds a property stopped after a varying number of keys.
/// </remarks>
public struct PropertyValueEnumerator
{
    public JSObject target;
    private uint start;
    readonly bool showEnumerableOnly;

    public PropertyValueEnumerator(JSObject target, bool showEnumerableOnly)
    {
        this.showEnumerableOnly = showEnumerableOnly;
        this.target = target;
        start = target.GetOwnProperties().Head;
    }

    // The object's current property map. GetOwnProperties() in the constructor materialized
    // the trie, which is one-way, and the field is never replaced, so this is always the map
    // the object is writing to.
    private readonly ref SAUint32Map<JSObjectProperty> Map => ref target.OwnProperties().GetMap();

    public bool MoveNext(out KeyString key)
    {
        while (start > 0)
        {
            ref var objP = ref Map.GetRefOrDefault(start, ref JSObjectProperty.Empty);
            ref var p = ref objP.Property;

            if (p.IsEmpty)
            {
                start = objP.Next;
                continue;
            }

            if (showEnumerableOnly && !p.IsEnumerable)
            {
                start = objP.Next;
                continue;
            }

            key = KeyStrings.GetName(start);
            if (JSObject.IsPrivateName(in key))
            {
                start = objP.Next;
                continue;
            }
            start = objP.Next;
            return true;
        }

        key = KeyString.Empty;
        return false;
    }

    public bool MoveNext(out JSValue value, out KeyString key)
    {
        while (start > 0)
        {
            ref var objP = ref Map.GetRefOrDefault(start, ref JSObjectProperty.Empty);
            ref var p = ref objP.Property;

            if (p.IsEmpty)
            {
                start = objP.Next;
                continue;
            }

            if (showEnumerableOnly && !p.IsEnumerable)
            {
                start = objP.Next;
                continue;
            }

            key = KeyStrings.GetName(start);
            if (JSObject.IsPrivateName(in key))
            {
                start = objP.Next;
                continue;
            }

            // Advance before reading the value: GetValue can run a getter that adds a
            // property, which may move the map's backing array out from under objP and p.
            start = objP.Next;
            var property = p;
            value = target.GetValue(in property);
            return true;
        }

        value = null;
        key = KeyString.Empty;
        return false;
    }

    public bool MoveNextProperty(out JSProperty value, out KeyString key)
    {
        while (start > 0)
        {
            ref var objP = ref Map.GetRefOrDefault(start, ref JSObjectProperty.Empty);
            ref var p = ref objP.Property;

            if (p.IsEmpty)
            {
                start = objP.Next;
                continue;
            }

            if (showEnumerableOnly && !p.IsEnumerable)
            {
                start = objP.Next;
                continue;
            }

            key = KeyStrings.GetName(start);
            if (JSObject.IsPrivateName(in key))
            {
                start = objP.Next;
                continue;
            }

            value = p;
            start = objP.Next;
            return true;
        }

        value = JSProperty.Empty;
        key = KeyString.Empty;
        return false;
    }
}
