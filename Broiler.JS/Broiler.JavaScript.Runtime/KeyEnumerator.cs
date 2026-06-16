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
        properties = new PropertyValueEnumerator(jSObject, showEnumerableOnly);
    }

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
            while (properties.MoveNext(out var key))
            {
                if (!Accept(key.ToString()))
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
                if (!Accept(name))
                    continue;

                value = JSValue.CreateString(name);
                return true;
            }

            elements = null;
        }

        if (properties.target != null)
        {
            while (properties.MoveNext(out var key))
            {
                if (!Accept(key.ToString()))
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
