using Broiler.JavaScript.Storage;
using System.Runtime.CompilerServices;
namespace Broiler.JavaScript.Runtime;

public partial class JSObject
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsSealed()
    {
        if ((status & ObjectStatus.Sealed) > 0)
            return true;

        if (IsExtensible())
            return false;

        ref var ownProperties = ref GetOwnProperties(false);
        var ownEnumerator = ownProperties.GetEnumerator();
        while (ownEnumerator.MoveNext(out var property))
        {
            if (property.IsConfigurable)
                return false;
        }

        var elements = GetElements(false);
        foreach (var (_, property) in elements.AllValues())
        {
            if (property.IsConfigurable)
                return false;
        }

        foreach (var entry in GetSymbols().All)
        {
            if (entry.Value.IsConfigurable)
                return false;
        }

        return true;
    }

    public bool IsSealedOrFrozen() => (status & ObjectStatus.SealedOrFrozen) > 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual bool IsExtensible() => !((status & ObjectStatus.NonExtensible) > 0);


    public bool IsSealedOrFrozenOrNonExtensible() => (status & ObjectStatus.SealedFrozenNonExtensible) > 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsFrozen()
    {
        if ((status & ObjectStatus.Frozen) > 0)
            return true;

        if (IsExtensible())
            return false;

        ref var ownProperties = ref GetOwnProperties(false);
        var ownEnumerator = ownProperties.GetEnumerator();
        while (ownEnumerator.MoveNext(out var property))
        {
            if (property.IsConfigurable || (property.IsValue && !property.IsReadOnly))
                return false;
        }

        var elements = GetElements(false);
        foreach (var (_, property) in elements.AllValues())
        {
            if (property.IsConfigurable || (property.IsValue && !property.IsReadOnly))
                return false;
        }

        foreach (var entry in GetSymbols().All)
        {
            if (entry.Value.IsConfigurable || (entry.Value.IsValue && !entry.Value.IsReadOnly))
                return false;
        }

        return true;
    }

    public virtual bool PreventExtensions()
    {
        status |= ObjectStatus.NonExtensible;
        return true;
    }

    internal override PropertyKey ToKey(bool create = true)
    {
        return CreateString(StringValue).ToKey(create);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal JSProperty GetInternalProperty(in KeyString key, bool inherited = true)
    {
        var r = ownProperties.GetValue(key.Key);
        if (!r.IsEmpty)
            return r;

        if (inherited && prototypeChain != null)
            r = prototypeChain.GetInternalProperty(key);

        return r;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal JSProperty GetInternalProperty(uint key, bool inherited = true)
    {
        if (elements.TryGetValue(key, out var r))
            return r;

        if (inherited && prototypeChain != null)
            return prototypeChain.GetInternalProperty(key);

        return new JSProperty();
    }

    internal JSProperty GetInternalProperty(IJSSymbol key, bool inherited = true)
    {
        if (symbols.TryGetValue(key.Key, out var r))
            return r;

        if (inherited && prototypeChain != null)
            return prototypeChain.GetInternalProperty(key);

        return new JSProperty();
    }
    internal override JSFunctionDelegate GetMethod(in KeyString key)
    {
        if (!ownProperties.IsEmpty)
        {
            ref var p = ref ownProperties.GetValue(key.Key);
            if (!p.IsEmpty)
            {
                var value = GetValue(p);
                if (value.IsUndefined || value.IsNull)
                    return null;

                if (value is IJSFunction g)
                    return g.Delegate;

                throw NewTypeError($"{key} is not a function");
            }
        }

        return prototypeChain?.GetMethod(key);
    }
}
