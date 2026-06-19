using System;
using System.Collections.Generic;
using Broiler.JavaScript.Storage;

namespace Broiler.JavaScript.Runtime;

public class PropertyEnumerator
{
    readonly JSObject target;
    readonly bool showEnumerableOnly;
    readonly bool inherited;
    private PropertyEnumerator parent;
    PropertyValueEnumerator properties;

    public PropertyEnumerator(JSObject jSObject, bool showEnumerableOnly, bool inherited)
    {
        target = jSObject;
        ref var op = ref jSObject.GetOwnProperties(false);
        properties = !op.IsEmpty ? new PropertyValueEnumerator(jSObject, showEnumerableOnly) : new PropertyValueEnumerator();
        this.showEnumerableOnly = showEnumerableOnly;
        this.inherited = inherited;
        parent = null;
    }

    public bool MoveNextProperty(out KeyString key, out JSProperty value)
    {
        if (properties.target != null)
        {
            if (properties.MoveNextProperty(out value, out key))
                return true;

            properties.target = null;

            if (inherited)
            {
                var @base = (target.prototypeChain as IJSPrototype)?.Object as JSObject;
                if (@base != null && @base != target)
                    parent = new PropertyEnumerator(@base, showEnumerableOnly, inherited);
            }
        }

        if (parent != null)
        {
            if (parent.MoveNextProperty(out key, out value))
                return true;

            parent = null;
        }

        key = KeyString.Empty;
        value = default;
        return false;
    }

    public bool MoveNext(out KeyString key, out JSValue value)
    {
        if (properties.target != null)
        {
            if (properties.MoveNext(out value, out key))
                return true;

            properties.target = null;
            
            if (inherited)
            {
                var @base = (target.prototypeChain as IJSPrototype)?.Object as JSObject;
                if (@base != null && @base != target)
                    parent = new PropertyEnumerator(@base, showEnumerableOnly, inherited);
            }
        }

        if (parent != null)
        {
            if (parent.MoveNext(out key, out value))
                return true;

            parent = null;
        }

        key = KeyString.Empty;
        value = null;
        return false;
    }
}

public class KeyEnumerator : IElementEnumerator
{
    private readonly JSObject jSObject;
    private readonly bool showEnumerableOnly;
    private readonly bool inherited;
    private KeyEnumerator parent = null;
    // Own indexed elements only — never the user @@iterator (key enumeration must
    // not invoke the iterator protocol; see GetOwnIndexedElementEnumerator).
    IElementEnumerator elements;
    PropertyValueEnumerator properties;

    // The set of property names already produced while walking the prototype chain.
    // for-in (EnumerateObjectProperties) must visit each name at most once, so a key
    // shadowed by a nearer object is skipped on the ancestors. Shared with the parent
    // enumerators; null when not walking the chain (inherited == false), where no
    // de-duplication is needed.
    private readonly HashSet<string> seen;

    // The own string-property names present when this object's enumeration began. Only
    // captured for a prototype-chain walk (for-in): EnumerateObjectProperties must not
    // visit properties added to the object *during* enumeration, but the property store
    // is walked live (so a deleted property is correctly skipped). A name produced by
    // the live walk that is absent here was added mid-enumeration and is dropped. Null
    // for a non-chain walk (Object.keys etc.), which never observes interleaved mutation.
    private readonly HashSet<string> ownPropertyNamesAtStart;

    // As ownPropertyNamesAtStart, but for the own indexed elements. The indexed-element
    // enumerator is walked live, so without this an index added mid-enumeration (e.g. by an
    // unshift/splice inside the loop body) would be visited; for-in must not visit it
    // (test262 sm/Array/unshift-with-enumeration). Null for a non-chain walk.
    private readonly HashSet<string> ownIndexNamesAtStart;

    public KeyEnumerator(JSObject jSObject, bool showEnumerableOnly, bool inherited)
        : this(jSObject, showEnumerableOnly, inherited,
               inherited ? new HashSet<string>(StringComparer.Ordinal) : null)
    {
    }

    private KeyEnumerator(JSObject jSObject, bool showEnumerableOnly, bool inherited, HashSet<string> seen)
    {
        this.jSObject = jSObject;
        this.showEnumerableOnly = showEnumerableOnly;
        this.inherited = inherited;
        this.seen = seen;
        elements = jSObject.GetOwnIndexedElementEnumerator(showEnumerableOnly);
        // Always walk every own property (including non-enumerable ones) so a
        // non-enumerable own property still registers in `seen` and shadows a
        // same-named enumerable property further up the prototype chain. The
        // enumerable-only filter is applied below, after recording the name.
        properties = new PropertyValueEnumerator(jSObject, false);

        if (inherited)
        {
            ownPropertyNamesAtStart = new HashSet<string>(StringComparer.Ordinal);
            var snapshot = new PropertyValueEnumerator(jSObject, false);
            while (snapshot.MoveNext(out var name))
                ownPropertyNamesAtStart.Add(name.ToString());

            ownIndexNamesAtStart = new HashSet<string>(StringComparer.Ordinal);
            var indexSnapshot = jSObject.GetOwnIndexedElementEnumerator(showEnumerableOnly);
            while (indexSnapshot.MoveNext(out var hasValueAtStart, out var _, out var ui))
                if (hasValueAtStart)
                    ownIndexNamesAtStart.Add(ui.ToString());
        }
    }

    // True when `name` is a string property that was added to the object after the
    // for-in enumeration of this object began (and so must not be visited).
    private bool AddedDuringEnumeration(string name)
        => ownPropertyNamesAtStart != null && !ownPropertyNamesAtStart.Contains(name);

    // As AddedDuringEnumeration, for an indexed element produced by the live element walk.
    private bool IndexAddedDuringEnumeration(string name)
        => ownIndexNamesAtStart != null && !ownIndexNamesAtStart.Contains(name);

    // Records a name as produced; returns false if it (or a nearer same-named property)
    // was already produced, so the caller skips it.
    private bool Accept(string name) => seen == null || seen.Add(name);

    private KeyEnumerator CreateParent()
    {
        if (!inherited)
            return null;
        var @base = (jSObject.prototypeChain as IJSPrototype)?.Object as JSObject;
        return @base != null && @base != jSObject
            ? new KeyEnumerator(@base, showEnumerableOnly, inherited, seen)
            : null;
    }

    public bool MoveNext(out bool hasValue, out JSValue value, out uint index)
    {
        if (elements != null)
        {
            while (elements.MoveNext(out var hasValueout, out var _, out var ui))
            {
                var name = ui.ToString();
                if (IndexAddedDuringEnumeration(name))
                    continue;
                if (!Accept(name))
                    continue;
                value = JSValue.CreateString(name);
                hasValue = hasValueout;
                index = ui;
                return true;
            }

            elements = null;
        }

        if (properties.target != null)
        {
            while (properties.MoveNextProperty(out var prop, out var key))
            {
                var name = key.ToString();
                // A property added during enumeration is not visited (for-in only).
                if (AddedDuringEnumeration(name))
                    continue;
                // Record the name first (shadowing the chain), then drop it if a
                // nearer property already claimed it or it is non-enumerable while
                // only enumerable keys were requested (for-in / Object.keys).
                if (!Accept(name))
                    continue;
                if (showEnumerableOnly && !prop.IsEnumerable)
                    continue;
                value = JSObjectCoreExtensions.KeyStringToJSValue(key);
                hasValue = true;
                index = 0;
                return true;
            }

            properties.target = null;
            parent = CreateParent();
        }

        if (parent != null)
        {
            if (parent.MoveNext(out hasValue, out value, out index))
                return true;

            parent = null;
        }

        hasValue = false;
        value = null;
        index = 0;
        return false;
    }

    public bool MoveNext(out JSValue value)
    {
        if (elements != null)
        {
            while (elements.MoveNext(out var hasValueout, out var _, out var ui))
            {
                if (!hasValueout)
                    continue;

                var name = ui.ToString();
                if (IndexAddedDuringEnumeration(name))
                    continue;
                if (!Accept(name))
                    continue;

                value = JSValue.CreateString(name);
                return true;
            }

            elements = null;
        }

        if (properties.target != null)
        {
            while (properties.MoveNextProperty(out var prop, out var key))
            {
                var name = key.ToString();
                if (AddedDuringEnumeration(name))
                    continue;
                if (!Accept(name))
                    continue;
                if (showEnumerableOnly && !prop.IsEnumerable)
                    continue;

                value = JSObjectCoreExtensions.KeyStringToJSValue(key);
                return true;
            }

            properties.target = null;
            parent = CreateParent();
        }

        if (parent != null)
        {
            if (parent.MoveNext(out value))
                return true;

            parent = null;
        }

        value = JSValue.UndefinedValue;
        return false;
    }

    public bool MoveNextOrDefault(out JSValue value, JSValue @default)
    {
        if (MoveNext(out value))
            return true;

        value = @default;
        return false;
    }

    public JSValue NextOrDefault(JSValue @default)
        => MoveNext(out var value) ? value : @default;
}
