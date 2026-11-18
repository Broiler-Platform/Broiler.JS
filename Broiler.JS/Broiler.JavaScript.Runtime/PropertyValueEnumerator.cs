using Broiler.JavaScript.Storage;

namespace Broiler.JavaScript.Runtime;

/// <summary>
/// Enumerator for iterating over property values in a <see cref="PropertySequence"/>.
/// This type was extracted from <see cref="PropertySequence"/> because it depends on
/// <see cref="JSObject"/> and <see cref="JSValue"/> (Core runtime types), while
/// <see cref="PropertySequence"/> itself lives in the Storage assembly.
/// </summary>
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

    // The target's live property map, read afresh on every step rather than copied once.
    // SAUint32Map is a struct over a growable node array, so a by-value copy goes stale as
    // soon as the object gains a property that grows that array: the copy keeps reading the
    // old array, in which a node relocated by the insertion (a smaller key displacing a
    // larger one on its trie path) is missing, and the walk stopped at the first such key.
    // A for-in body adding a property hit exactly that, so for-in dropped the properties
    // still to come — depending on the process-global KeyString ids, which made
    // ForIn_Does_Not_Visit_Properties_Added_During_Enumeration_Match_Test262 order-dependent.
    // Walking the live map keeps the insertion-order chain consistent; properties added
    // mid-walk are filtered by the for-in caller (KeyEnumerator), not here.
    private readonly ref SAUint32Map<JSObjectProperty> Map
        => ref target.GetOwnProperties(false).GetMap();

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

            value = target.GetValue(in p);
            start = objP.Next;
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
