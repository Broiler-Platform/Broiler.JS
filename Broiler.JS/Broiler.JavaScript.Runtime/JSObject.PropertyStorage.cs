using Broiler.JavaScript.Storage;
using Broiler.JavaScript.Ast.Misc;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
namespace Broiler.JavaScript.Runtime;

public partial class JSObject
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal protected virtual bool HasOwnProperty(in PropertyKey key) => key.Type switch
    {
        KeyType.UInt => elements.HasKey(key.Index),
        KeyType.String => !IsPrivateName(in key.KeyString) && HasOwnNamedProperty(key.KeyString.Key),
        KeyType.Symbol => symbols.HasKey(key.Symbol.Key),
        _ => false
    };

    /// <summary>
    /// Whether this object owns <paramref name="key"/> as a named property, answered from
    /// whichever storage currently holds the truth (item 2-9).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool HasOwnNamedProperty(uint key)
        => IsShapeOnlyStorage ? ShapeOnlyHasKey(key) : ownProperties.HasKey(key);

    /// <summary>
    /// The descriptor for a named property, from whichever storage holds the truth (item 2-9).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryGetOwnNamedProperty(uint key, out JSProperty property)
        => IsShapeOnlyStorage
            ? TryGetShapeOnlyProperty(key, out property)
            : ownProperties.TryGetValue(key, out property);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TryGetOrdinaryOwnProperty(in PropertyKey key, out JSProperty property)
    {
        switch (key.Type)
        {
            case KeyType.UInt:
                return elements.TryGetValue(key.Index, out property);
            case KeyType.String when !IsPrivateName(in key.KeyString):
                return IsShapeOnlyStorage
                    ? TryGetShapeOnlyProperty(key.KeyString.Key, out property)
                    : ownProperties.TryGetValue(key.KeyString.Key, out property);
            case KeyType.Symbol:
                return symbols.TryGetValue(key.Symbol.Key, out property);
            default:
                property = JSProperty.Empty;
                return false;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref PropertySequence GetOwnProperties(bool create = true)
    {
        // Returning a mutable ref permits callers in other assemblies to bypass the
        // shape tracker. Conservatively abandon the fast layout for exact ordinary
        // objects whenever mutable access is requested; read-only enumeration passes
        // create:false and retains its shape.
        //
        // Either way the caller is about to read the trie, so this is also item 2-9's
        // materialization boundary: a shape-only object writes its named properties back
        // here, and from this point on behaves exactly as it did before that item.
        if (create)
            AbandonObjectShape();
        return ref OwnProperties();
    }

    /// <summary>
    /// Internal marker character prefixed to a private name's property key so it
    /// occupies a key space disjoint from ordinary string properties. A class's
    /// private <c>#x</c> and a public <c>"#x"</c> string property must be distinct
    /// bindings (sec-privatefieldget); the compiler emits this marker for private
    /// member references, and reflection/enumeration hides keys carrying it.
    /// </summary>
    public const char PrivateNameMarker = '\u0001';

    // Separates a private name's text from a per-evaluation uniquifier in a minted
    // private key (see MintPrivateName). Distinct from PrivateNameMarker.
    private const char PrivateNameEvalSeparator = '';

    private static int privateNameCounter;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsPrivateName(in KeyString key)
        => key.Metadata.IsPrivateName;

    // True when a property key (as surfaced by the own-key enumeration) is a private name.
    // Private names are never own property keys per spec, so key-walking operations that must
    // exclude them (e.g. SetIntegrityLevel) use this to skip them.
    internal static bool IsPrivateNameKey(JSValue key)
        => key.ToKey(false) is { Type: KeyType.String, KeyString: var keyString } && IsPrivateName(in keyString);

    /// <summary>
    /// Mints a fresh private-name key for one class evaluation. Each call returns a
    /// distinct key, so a private element installed by one evaluation of a class is
    /// not visible on instances produced by another evaluation — the key itself is
    /// the per-evaluation PrivateBrand (brand-check-multiple-evaluations). The
    /// compiler stores the result in a class-evaluation-scope variable that every
    /// member reference closes over. <paramref name="name"/> already carries the
    /// leading '#'.
    /// </summary>
    public static KeyString MintPrivateName(string name)
        => KeyStrings.GetOrCreate(
            PrivateNameMarker + name + PrivateNameEvalSeparator + Interlocked.Increment(ref privateNameCounter));

    // Ergonomic brand check `#name in rval` (RelationalExpression : PrivateIdentifier
    // in ShiftExpression). Returns true when rval carries the private name, false
    // otherwise; a non-object rval is a TypeError. Uses the same internal lookup as a
    // private member access, so `#x in obj` is true exactly when `obj.#x` would not
    // throw a brand-check TypeError.
    public static JSValue PrivateNameIn(KeyString key, JSValue rval)
    {
        if (rval is not JSObject obj)
            throw NewTypeError("Cannot use 'in' operator to check for a private name in a non-object");

        return obj.GetInternalProperty(key).IsEmpty ? BooleanFalse : BooleanTrue;
    }

    // Brand check for a private member access (`obj.#x`). A private name must be
    // present as an OWN element of the receiver (PrivateBrandCheck inspects
    // O.[[PrivateBrands]] / [[PrivateElements]] directly — it never walks the
    // prototype chain). Instance private fields/methods are installed own on each
    // instance, and static private elements own on the class constructor, so an
    // own-only lookup matches them all. Crucially, a subclass constructor inherits
    // its super-class through the constructor prototype chain but does NOT carry the
    // super-class's static private brand: `class C { static #g(){} static f(){
    // return this.#g(); } } class D extends C {}; D.f()` must throw (test262
    // static-private-method-subclass-receiver). The check observes neither
    // getters/setters nor Proxy traps. Field *initialization* never reaches here.
    private void ThrowIfMissingPrivateMember(in KeyString key, bool reading)
    {
        if (!GetInternalProperty(key, inherited: false).IsEmpty)
            return;

        ThrowMissingPrivateMember(in key, reading);
    }

    // PrivateSet (the brand already verified): a private element is stored own on the
    // receiver and is outside the ordinary property model. A private accessor routes to
    // its setter (a getter-only accessor is a TypeError); a private method cannot be
    // reassigned (a TypeError); a private field's [[Value]] is updated directly. None of
    // this consults the object's extensibility or Object.freeze/seal state — a frozen
    // object's private field is still writable (test262 PrivateName/modify-non-extensible).
    private bool SetPrivateMember(in KeyString name, JSValue value, JSValue receiver, bool throwError)
    {
        var p = GetInternalProperty(name, false);
        if (p.IsProperty)
        {
            if (p.set is IJSFunction setter)
            {
                setter.InvokeFunction(new Arguments(receiver ?? this, value));
                return true;
            }

            if (throwError)
                throw NewTypeError($"Cannot write to private accessor {PrivateDisplayName(in name)} which has only a getter");

            return false;
        }

        if (p.IsReadOnly)
        {
            if (throwError)
                throw NewTypeError($"Cannot write to private method {PrivateDisplayName(in name)}");

            return false;
        }

        FastAddValue(name, value, p.Attributes);
        return true;
    }

    // Raises the brand-check TypeError for a private member access whose receiver
    // does not carry the private name. Also used by the primitive path: a raw
    // primitive (the boxed wrapper ToObject would create) can never hold a private
    // field, so `(15).#x` / `"s".#x` is always a TypeError.
    internal static void ThrowMissingPrivateMember(in KeyString key, bool reading)
    {
        var display = PrivateDisplayName(in key);
        throw NewTypeError(reading
            ? $"Cannot read private member {display} from an object whose class did not declare it"
            : $"Cannot write private member {display} to an object whose class did not declare it");
    }

    // Recovers a private name's human-readable text (e.g. "#x") from a minted key
    // for diagnostics, dropping the internal marker and per-evaluation uniquifier.
    private static string PrivateDisplayName(in KeyString key)
    {
        var s = key.Value.Value;
        if (string.IsNullOrEmpty(s) || s[0] != PrivateNameMarker)
            return "#<unknown>";

        var end = s.IndexOf(PrivateNameEvalSeparator, 1);
        return end < 0 ? s[1..] : s[1..end];
    }

    // Shared guard for PrivateFieldAdd / PrivateMethodOrAccessorAdd: adding a
    // private element to a non-extensible object is a TypeError (the
    // nonextensible-applies-to-private refinement), and so is re-adding a private
    // name the object already carries — observable when a derived constructor's
    // return-override hands the same object to two installations.
    private void PrivateElementAddGuard(in KeyString key)
    {
        if (!IsExtensible())
            throw NewTypeError($"Cannot add private member {PrivateDisplayName(in key)} to a non-extensible object");

        if (!OwnProperties().GetValue(key.Key).IsEmpty)
            throw NewTypeError($"Cannot add private member {PrivateDisplayName(in key)}: it is already present on the object");
    }

    /// <summary>
    /// PrivateFieldAdd (ECMA-262 § 7.3.28): installs a private field on this object
    /// during instance-field initialization. The field is stored directly as an
    /// internal slot, bypassing Proxy traps.
    /// </summary>
    public void PrivateFieldAdd(KeyString key, JSValue value)
    {
        PrivateElementAddGuard(in key);
        FastAddValue(key, value, JSPropertyAttributes.ConfigurableValue);
    }

    /// <summary>
    /// PrivateMethodOrAccessorAdd for an instance private method: installs the
    /// shared method function as a read-only per-instance internal slot. Installing
    /// it per instance (rather than once on the prototype) is what gives a
    /// <c>return</c>-override object the brand and makes a second installation throw.
    /// </summary>
    public void PrivateMethodAdd(KeyString key, JSValue method)
    {
        PrivateElementAddGuard(in key);
        FastAddValue(key, method, JSPropertyAttributes.ConfigurableReadonlyValue);
    }

    /// <summary>
    /// PrivateMethodOrAccessorAdd for an instance private accessor: installs the
    /// shared getter and/or setter (either may be null) merged into one element.
    /// </summary>
    public void PrivateAccessorAdd(KeyString key, JSValue getter, JSValue setter)
    {
        PrivateElementAddGuard(in key);
        ref var pr = ref GetOwnProperties();
        pr.Put(key.Key) = new JSProperty(key, getter, setter, JSPropertyAttributes.ConfigurableProperty);
    }

    public override JSValue GetOwnPropertyDescriptor(JSValue name)
    {
        var key = name.ToKey(false);

        switch (key.Type)
        {
            case KeyType.String:
                if (IsPrivateName(in key.KeyString))
                    return UndefinedValue;

                if (IsShapeOnlyStorage
                    ? TryGetShapeOnlyProperty(key.KeyString.Key, out var p)
                    : ownProperties.TryGetValue(key.KeyString.Key, out p))
                {
                    return JSObjectCoreExtensions.PropertyToJSValue(in p);
                }
                return UndefinedValue;

            case KeyType.UInt:
                if (elements.TryGetValue(key.Index, out var p1))
                    return JSObjectCoreExtensions.PropertyToJSValue(in p1);
                return UndefinedValue;

            case KeyType.Symbol:
                if (symbols.TryGetValue(key.Symbol.Key, out var p3))
                    return JSObjectCoreExtensions.PropertyToJSValue(in p3);
                return UndefinedValue;
        }

        return UndefinedValue;
    }

    public override JSValue GetOwnProperty(in KeyString name)
    {
        if (IsShapeOnlyStorage)
        {
            return TryGetShapeOnlyProperty(name.Key, out var shapeOnly)
                ? GetValue(shapeOnly)
                : GetValue(JSProperty.Empty);
        }

        ref var p = ref ownProperties.GetValue(name.Key);
        return GetValue(p);
    }

    public override JSValue GetOwnProperty(IJSSymbol name)
    {
        ref var p = ref symbols.GetRefOrDefault(name.Key, ref JSProperty.Empty);
        return GetValue(p);
    }

    public override JSValue GetOwnProperty(uint name)
    {
        var p = elements.Get(name);
        return GetValue(p);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref ElementArray GetElements(bool create = true) => ref elements;
    public ref SAUint32Map<JSProperty> GetSymbols() => ref symbols;

    internal void AllocateElements(uint size)
    {
        size = size > 1024 ? 1024 : size;
        elements.Resize(size);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ref ElementArray CreateElements(uint size = 4) => ref elements;
    [EditorBrowsable(EditorBrowsableState.Never)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void FastAddValue(uint index, JSValue value, JSPropertyAttributes attributes)
    {
        elements.Put(index, value, attributes);
        NotifyIndexedPropertyMutation();
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void FastAddProperty(uint index, JSValue getter, JSValue setter, JSPropertyAttributes attributes)
    {
        elements.Set(index, new JSProperty(index, getter, setter, getter, attributes));
        NotifyIndexedPropertyMutation();
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void FastAddValue(KeyString key, JSValue value, JSPropertyAttributes attributes)
    {
        // Item 2-9's principal saving. This is the property-creating write every constructor
        // and object literal ends in, so keeping it out of the radix trie is what takes a
        // tracked property from ~150 bytes of node to a slot and a byte. A shape-only object
        // can hold no lazy cell, so there is nothing to cancel on this path either.
        if (TryShapeOnlySetDataProperty(in key, value, attributes))
            return;

        ref var own = ref OwnProperties();
        CancelLazyDataProperty(in own.GetValue(key.Key));
        own.Put(key.Key) = new JSProperty(key.Key, value, attributes);
        TrackShapeDataProperty(in key, value, attributes);
    }

    /// <summary>
    /// Adds an ordinary data property whose per-realm value is resolved on demand.
    /// Key order and attributes are installed immediately; enumeration does not realize it.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void FastAddLazyDataProperty(
        KeyString key,
        IJSFeatureResolver resolver,
        BuiltInFeatureId feature,
        JSPropertyAttributes attributes)
    {
        ref var lazyOwn = ref OwnProperties();
        CancelLazyDataProperty(in lazyOwn.GetValue(key.Key));
        lazyOwn.Put(key.Key) = new JSProperty(
            key,
            null,
            null,
            new LazyDataPropertyCell(resolver, feature),
            attributes);
        AbandonObjectShape();
    }

    /// <summary>
    /// Adds an ordinary data property whose value is recomputed on every read. The property
    /// is observably a data property (value/writable, not get/set) — see
    /// <see cref="IDeferredPropertyValue"/> — so this suits a spec- or web-reality-mandated
    /// data property whose value tracks live engine state.
    /// </summary>
    internal void FastAddDeferredValue(
        KeyString key,
        IDeferredPropertyValue deferred,
        JSPropertyAttributes attributes)
    {
        ref var deferredOwn = ref OwnProperties();
        CancelLazyDataProperty(in deferredOwn.GetValue(key.Key));
        deferredOwn.Put(key.Key) = new JSProperty(key, null, null, deferred, attributes);

        // Recorded in the shape with no slot value rather than abandoning it. A deferred cell
        // has to be realized by the generic path, and a null slot is exactly how the shape says
        // so while still admitting the key exists — see TrackShapeKeyWithoutSlotValue. Dropping
        // the whole layout instead meant an ordinary non-strict function, which gets two of
        // these at birth, could never be shape-tracked (roadmap item 2-8).
        TrackShapeKeyWithoutSlotValue(in key);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CancelLazyDataProperty(in JSProperty property)
    {
        if (property.value is LazyDataPropertyCell lazy)
            lazy.Cancel();
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void FastAddProperty(KeyString key, JSValue getter, JSValue setter, JSPropertyAttributes attributes)
    {
        OwnProperties().Put(key.Key) = new JSProperty(key, getter, setter, attributes);
        AbandonObjectShape();
        NotifyNamedPropertyMutation();
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void FastAddValue(IJSSymbol key, JSValue value, JSPropertyAttributes attributes)
    {
        ref var pr = ref GetSymbols();
        pr.Put(key.Key) = new JSProperty(key.Key, value, attributes);
        NotifyNamedPropertyMutation();
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void FastAddProperty(IJSSymbol key, JSValue getter, JSValue setter, JSPropertyAttributes attributes)
    {
        ref var pr = ref GetSymbols();
        pr.Put(key.Key) = new JSProperty(key.Key, getter, setter, getter, attributes);
        NotifyNamedPropertyMutation();
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void FastAddValue(JSValue key, JSValue value, JSPropertyAttributes attributes)
    {
        var k = key.ToKey(true);
        switch (k.Type)
        {
            case KeyType.String:
                FastAddValue(k.KeyString, value, attributes);
                return;

            case KeyType.UInt:
                FastAddValue(k.Index, value, attributes);
                return;

            default:
                FastAddValue(k.Symbol, value, attributes);
                return;
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void FastAddProperty(JSValue key, JSValue getter, JSValue setter, JSPropertyAttributes attributes)
    {
        var k = key.ToKey(true);
        switch (k.Type)
        {
            case KeyType.String:
                FastAddProperty(k.KeyString, getter, setter, attributes);
                return;

            case KeyType.UInt:
                FastAddProperty(k.Index, getter, setter, attributes);
                return;

            default:
                FastAddProperty(k.Symbol, getter, setter, attributes);
                return;
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    // Object spread (`{ ...source }`) and Object.assign perform CopyDataProperties:
    // copy the source's own *enumerable* properties. Ordinary objects use the fast
    // path below (direct slot copy). Exotic objects whose own-key enumeration and
    // property reads must be observable — Proxies above all — override this to true
    // so the copy goes through [[OwnPropertyKeys]] / [[GetOwnProperty]] / [[Get]].
    private protected virtual bool UseObservableSpreadCopy => false;

    private readonly struct OrdinaryOwnKey
    {
        public readonly KeyType Type;
        public readonly uint Index;
        public readonly KeyString Name;

        public OrdinaryOwnKey(KeyType type, uint index, in KeyString name)
        {
            Type = type;
            Index = index;
            Name = name;
        }
    }

    // CopyDataProperties obtains [[OwnPropertyKeys]] before it invokes a getter. Keep the
    // compact direct-slot path for ordinary objects, but snapshot the keys so a getter may
    // add/delete sparse elements without invalidating the storage enumerator or changing
    // the key list being copied.
    private static List<OrdinaryOwnKey> SnapshotOrdinaryOwnKeys(JSObject target)
    {
        var keys = new List<OrdinaryOwnKey>(target.elements.Count + target.symbols.Count);

        foreach (var (index, _) in target.elements.AllValues())
            keys.Add(new OrdinaryOwnKey(KeyType.UInt, index, KeyString.Empty));

        var properties = target.OwnProperties().GetEnumerator(showEnumerableOnly: false);
        while (properties.MoveNext(out var name, out _))
            keys.Add(new OrdinaryOwnKey(KeyType.String, 0, name));

        foreach (var (symbol, _) in target.symbols.AllValues())
            keys.Add(new OrdinaryOwnKey(KeyType.Symbol, symbol, KeyString.Empty));

        return keys;
    }

    private static List<JSValue> SnapshotObservableOwnKeys(JSObject target)
    {
        var snapshot = new List<JSValue>();
        var keys = target.GetAllKeys(showEnumerableOnly: false, inherited: false);
        while (keys.MoveNext(out var hasKey, out var key, out _))
            if (hasKey)
                snapshot.Add(key);
        return snapshot;
    }

    private void CopyObservableDataProperties(JSObject target, JSObject excludedKeys = null)
    {
        foreach (var key in SnapshotObservableOwnKeys(target))
        {
            // Exclusions are checked before [[GetOwnProperty]]/[[Get]], as required by
            // CopyDataProperties (and observable through Proxy traps).
            if (excludedKeys != null && excludedKeys.IsExcludedOwnKey(key.ToKey()))
                continue;

            if (target.GetOwnPropertyDescriptor(key) is not JSObject descriptor
                || !descriptor[KeyStrings.enumerable].BooleanValue)
                continue;

            CreateDataProperty(key, target[key]);
        }
    }

    private void CopyOrdinaryDataProperties(JSObject target, JSObject excludedKeys = null)
    {
        foreach (var key in SnapshotOrdinaryOwnKeys(target))
        {
            JSProperty property;
            switch (key.Type)
            {
                case KeyType.UInt:
                    if ((excludedKeys != null && excludedKeys.elements.HasKey(key.Index))
                        || !target.elements.TryGetValue(key.Index, out property))
                        continue;
                    break;
                case KeyType.String:
                    if ((excludedKeys != null && excludedKeys.HasOwnNamedProperty(key.Name.Key))
                        || !target.TryGetOwnNamedProperty(key.Name.Key, out property))
                        continue;
                    break;
                case KeyType.Symbol:
                    if ((excludedKeys != null && excludedKeys.symbols.HasKey(key.Index))
                        || !target.symbols.TryGetValue(key.Index, out property))
                        continue;
                    break;
                default:
                    continue;
            }

            // The descriptor is looked up after the key snapshot and before invoking its
            // getter. A key deleted by an earlier getter is therefore skipped, while a key
            // added by one is not copied.
            if (!property.IsEnumerable)
                continue;

            var value = (IPropertyValue)target.GetValue(property);

            switch (key.Type)
            {
                case KeyType.UInt:
                    elements.Set(key.Index, JSProperty.Property(key.Index, value));
                    break;
                case KeyType.String:
                    if (!TryShapeOnlySetDataProperty(in key.Name, value as JSValue, JSPropertyAttributes.EnumerableConfigurableValue))
                    {
                        OwnProperties().Put(key.Name.Key) = JSProperty.Property(key.Name, value);
                        TrackShapeDataProperty(in key.Name, value as JSValue, JSPropertyAttributes.EnumerableConfigurableValue);
                    }
                    break;
                case KeyType.Symbol:
                    symbols.Put(key.Index) = JSProperty.Property(key.Index, value);
                    break;
            }
        }
    }

    public void FastAddRange(JSValue value)
    {
        if (value is not JSObject target)
        {
            // §7.3.25 CopyDataProperties: undefined/null sources contribute nothing.
            // Any other primitive is boxed via ToObject — a String wrapper exposes its
            // characters as own enumerable index properties, so `{ ...'ab' }` and
            // `let { ...rest } = 'ab'` copy { 0:'a', 1:'b' } (other primitive wrappers
            // have no own enumerable properties and copy nothing).
            if (value.IsNullOrUndefined)
                return;

            if (CreatePrimitiveObject(value) is not JSObject boxed)
                return;

            target = boxed;
        }

        if (target.UseObservableSpreadCopy)
        {
            // §7.3.25 CopyDataProperties: iterate [[OwnPropertyKeys]] in order; for
            // each key read its descriptor (firing the getOwnPropertyDescriptor
            // trap) and copy only enumerable properties, reading the value via
            // [[Get]] (firing the get trap).
            CopyObservableDataProperties(target);
            return;
        }

        CopyOrdinaryDataProperties(target);
    }

    // CopyDataProperties with an exclusion set — object rest destructuring
    // (`let { a, ...rest } = source`). Per §7.3.25 an excluded key is skipped BEFORE its
    // descriptor or value is read, so a Proxy source's getOwnPropertyDescriptor/get traps
    // (and an ordinary accessor's getter) never fire for the excluded keys. The previous
    // lowering copied every property and then deleted the excluded keys, which observably
    // read them first. The excluded keys are supplied as the own keys of `excludedKeys`
    // (populated by the caller through the ordinary indexer, so all key forms are normalised
    // the same way the source's keys are).
    public void FastAddRange(JSValue value, JSObject excludedKeys)
    {
        if (value is not JSObject target)
        {
            if (value.IsNullOrUndefined)
                return;

            if (CreatePrimitiveObject(value) is not JSObject boxed)
                return;

            target = boxed;
        }

        if (target.UseObservableSpreadCopy)
        {
            CopyObservableDataProperties(target, excludedKeys);
            return;
        }

        CopyOrdinaryDataProperties(target, excludedKeys);
    }

    // True when `key` names one of this object's own properties — used as the exclusion test
    // for object-rest CopyDataProperties (the excluded keys live as own keys of a scratch
    // object, normalised through the ordinary indexer just like the source's keys).
    private bool IsExcludedOwnKey(in PropertyKey key) => key.Type switch
    {
        KeyType.UInt => elements.HasKey(key.Index),
        KeyType.String => HasOwnNamedProperty(key.KeyString.Key),
        KeyType.Symbol => symbols.HasKey(key.Symbol.Key),
        _ => false,
    };

    [EditorBrowsable(EditorBrowsableState.Never)]
    public JSObject Merge(JSValue value)
    {
        if (value is not JSObject target)
            return this;

        var pe = new PropertyEnumerator(target, true, false);
        while (pe.MoveNext(out var key, out var val))
            this[key] = val;

        var en = new ElementEnumerator(target);
        while (en.MoveNext(out var hasValue, out var val, out var index))
        {
            if (hasValue)
                this[index] = val;
        }

        return this;
    }
    public override JSValue this[KeyString name]
    {
        get => GetValue(name, this);
        // The receiver of an ordinary property write is the object being indexed itself.
        // For a plain object `receiver ?? this` makes null and `this` equivalent, but a Proxy
        // forwards the receiver straight to its `set` trap, so a null receiver would surface as
        // `undefined` there instead of the proxy (test262 sm/Iterator proxy-accesses). Match the
        // uint indexer and pass `this`.
        set => SetValue(name, value, this, IsStrictModeEnabled?.Invoke() == true);
    }

    internal protected override bool SetValue(KeyString name, JSValue value, JSValue receiver, bool throwError = true)
    {
        // A private member assignment (`obj.#x = v`) requires the brand: writing a
        // private name to an object whose class did not declare it is a TypeError.
        // Field initialization adds the field directly via FastAddValue and never
        // reaches SetValue, so it is unaffected. A private element lives outside the
        // ordinary property model, so the write is handled here rather than flowing
        // into the extensibility/integrity checks below.
        if (IsPrivateName(in name))
        {
            ThrowIfMissingPrivateMember(in name, reading: false);
            return SetPrivateMember(in name, value, receiver, throwError);
        }

        // Overwriting an existing, writable own data property on this very object — the
        // shape a hot loop's `o.x = v` takes. The general path below resolves that same own
        // property twice (here, then again inside SetKeyStringOnReceiver) before arriving at
        // exactly the Put performed here. Restricted to an exact JSObject so no exotic
        // [[DefineOwnProperty]] is skipped, and to a stored JSValue so a lazily-realized
        // cell still goes the long way round and gets cancelled properly.
        if (ReferenceEquals(receiver, this) && GetType() == typeof(JSObject))
        {
            // Shape-only (item 2-9): the same overwrite, with the attributes read from the
            // parallel array and the value written to the slot. No descriptor exists to
            // rewrite, and none needs to — an overwrite moves the value and nothing else.
            if (IsShapeOnlyStorage)
            {
                if (TryShapeOnlyOverwrite(in name, value))
                    return true;
            }
            else
            {
                ref var existingOwn = ref ownProperties.GetValue(name.Key);
                if (existingOwn.IsValue
                    && !existingOwn.IsReadOnly
                    && existingOwn.value is JSValue)
                {
                    var existingAttributes = existingOwn.Attributes;
                    // Written through the ref the lookup above already produced. Re-entering
                    // the map through Put walked it a second time to reach a node known to
                    // exist, and the property map is a radix trie, so that walk is
                    // proportional to the key.
                    existingOwn = new JSProperty(name, value, existingAttributes);
                    TrackShapeDataProperty(in name, value, existingAttributes);
                    PropertyChanged?.Invoke(this, (name.Key, uint.MaxValue, null));
                    return true;
                }
            }
        }

        // `obj.__proto__ = v` is an ordinary [[Set]] of the inherited %Object.prototype%
        // `__proto__` accessor: it must walk the prototype chain so that an exotic [[Set]]
        // on the way (e.g. a Proxy in the chain) intercepts it, and only the accessor's own
        // setter performs SetPrototypeOf. Short-circuiting to SetPrototypeOf here would
        // bypass such an interceptor (test262 Proxy/set/call-parameters-prototype-dunder-proto);
        // the natural walk below reaches the real accessor for an ordinary chain instead.

        var p = GetInternalProperty(name, false);
        if (p.IsProperty)
        {
            if (p.set is IJSFunction setter)
            {
                setter.InvokeFunction(new Arguments(receiver ?? this, value));
                return true;
            }

            if (throwError)
                throw NewTypeError($"Cannot modify property {name} of {this} which has only a getter");

            return false;
        }

        if (p.IsReadOnly)
        {
            if (throwError)
            {
                // Only in Strict Mode ..
                throw NewTypeError($"Cannot modify property {name} of {this}");
            }

            return false;
        }

        if (!p.IsEmpty)
            return SetKeyStringOnReceiver(name, value, receiver, p.Attributes, throwError);

        if (GetPrototypeOf() is JSObject prototypeObject)
            return prototypeObject.SetValue(name, value, receiver ?? this, throwError);

        return SetKeyStringOnReceiver(name, value, receiver, JSPropertyAttributes.EnumerableConfigurableValue, throwError);
    }

    public override JSValue this[uint name]
    {
        get => GetValue(name, this);
        set => SetValue(name, value, this, IsStrictModeEnabled?.Invoke() == true);
    }

    public override bool SetValue(uint name, JSValue value, JSValue receiver, bool throwError = true)
    {
        var p = GetInternalProperty(name, false);
        if (p.IsProperty)
        {
            if (p.set is IJSFunction setter)
            {
                setter.InvokeFunction(new Arguments(receiver ?? this, value));
                return true;
            }

            if (throwError)
                throw NewTypeError($"Cannot modify property {name} of {this} which has only a getter");

            return false;
        }

        if (p.IsReadOnly)
        {
            if (throwError)
                throw NewTypeError($"Cannot modify property {name} of {this}");

            return false;
        }

        if (!p.IsEmpty)
            return SetIndexOnReceiver(name, value, receiver, p.Attributes, throwError);

        if (GetPrototypeOf() is JSObject prototypeObject)
            return prototypeObject.SetValue(name, value, receiver ?? this, throwError);

        return SetIndexOnReceiver(name, value, receiver, JSPropertyAttributes.EnumerableConfigurableValue, throwError);
    }

    public override JSValue this[IJSSymbol name]
    {
        get => GetValue(name, this);
        // See the KeyString indexer: pass `this` so a Proxy `set` trap receives the proxy as the
        // receiver rather than `undefined`.
        set => SetValue(name, value, this, IsStrictModeEnabled?.Invoke() == true);
    }

    public void SetPropertyOrThrow(JSValue key, JSValue value)
    {
        var propertyKey = key.ToKey(false);
        switch (propertyKey.Type)
        {
            case KeyType.UInt:
                SetValue(propertyKey.Index, value, this, true);
                return;
            case KeyType.String:
                SetValue(propertyKey.KeyString, value, this, true);
                return;
            case KeyType.Symbol:
                SetValue(propertyKey.Symbol, value, this, true);
                return;
            default:
                throw NewTypeError($"Cannot set property {key}");
        }
    }

    internal protected override bool SetValue(IJSSymbol name, JSValue value, JSValue receiver, bool throwError = true)
    {
        var p = GetInternalProperty(name, false);
        if (p.IsProperty)
        {
            if (p.set is IJSFunction setter)
            {
                setter.InvokeFunction(new Arguments(receiver ?? this, value));
                return true;
            }

            if (throwError)
                throw NewTypeError($"Cannot modify property {name} of {this} which has only a getter");

            return false;
        }

        if (p.IsReadOnly)
        {
            if (throwError)
                throw NewTypeError($"Cannot modify property {name} of {this}");

            return false;
        }

        if (!p.IsEmpty)
            return SetSymbolOnReceiver(name, value, receiver, p.Attributes, throwError);

        if (GetPrototypeOf() is JSObject prototypeObject)
            return prototypeObject.SetValue(name, value, receiver ?? this, throwError);

        return SetSymbolOnReceiver(name, value, receiver, JSPropertyAttributes.EnumerableConfigurableValue, throwError);
    }

    /// <summary>
    /// The attributes CreateDataProperty(O, P, V) gives the property it creates: writable,
    /// enumerable and configurable (7.3.5, via OrdinaryDefineOwnProperty).
    /// </summary>
    /// <remarks>
    /// The last step of OrdinarySetWithOwnDescriptor — the RECEIVER has no own property —
    /// is CreateDataProperty, and CreateDataProperty does not consult the base object: the
    /// property it makes is all-true whatever the base's own property looked like. Passing
    /// the base's attributes down instead made
    /// <c>Reflect.set(base, key, value, receiver)</c> give the receiver's new property the
    /// base property's attributes, so a non-enumerable non-configurable base property
    /// produced a non-enumerable non-configurable one on an unrelated object.
    /// </remarks>
    private const JSPropertyAttributes CreateDataPropertyAttributes =
        JSPropertyAttributes.EnumerableConfigurableValue;

    protected bool SetKeyStringOnReceiver(KeyString name, JSValue value, JSValue receiver, JSPropertyAttributes defaultAttributes, bool throwError)
    {
        if (receiver != null && receiver is not JSObject)
        {
            if (throwError)
                throw NewTypeError($"Cannot add property {name} to {receiver}");

            return false;
        }

        var target = receiver as JSObject ?? this;
        if (!ReferenceEquals(target, this))
        {
            if (TrySetOrdinaryReceiverDataProperty(target, in name, value, defaultAttributes, throwError, out var ordinaryResult))
                return ordinaryResult;

            var descriptor = target.GetOwnPropertyDescriptor(name.ToJSValue()) as JSObject;
            if (descriptor != null)
            {
                if (TrySetReceiverAccessorProperty(target, descriptor, receiver, value, name, throwError, out var accessorResult))
                    return accessorResult;

                if (IsReceiverReadOnly(descriptor))
                {
                    if (throwError)
                        throw NewTypeError($"Cannot modify property {name} of {target}");

                    return false;
                }

                return DefineReceiverDataProperty(target, name, value, GetReceiverAttributes(descriptor, defaultAttributes), throwError);
            }

            // CreateDataProperty(receiver, P, V) is receiver.[[DefineOwnProperty]]; the
            // extensibility check belongs to OrdinaryDefineOwnProperty and is performed
            // inside DefineReceiverDataProperty for an ordinary target. Calling
            // IsExtensible() here would additionally fire a proxy receiver's isExtensible
            // trap, which the spec's OrdinarySetWithOwnDescriptor does NOT (test262 Array
            // reverse/splice length-exceeding-integer-limit-with-proxy).
            return DefineReceiverDataProperty(target, name, value, CreateDataPropertyAttributes, throwError);
        }

        var p = target.GetInternalProperty(name, false);
        if (p.IsProperty)
        {
            if (p.set is IJSFunction setter)
            {
                setter.InvokeFunction(new Arguments(receiver ?? target, value));
                return true;
            }

            if (throwError)
                throw NewTypeError($"Cannot modify property {name} of {target} which has only a getter");

            return false;
        }

        if (p.IsReadOnly)
        {
            if (throwError)
                throw NewTypeError($"Cannot modify property {name} of {target}");

            return false;
        }

        if (target.IsFrozen())
        {
            if (throwError)
                throw NewTypeError($"Cannot modify property {name} of {target}");

            return false;
        }

        if (p.IsEmpty && !target.IsExtensible())
        {
            if (throwError)
                throw NewTypeError($"Cannot add property {name} to {target}");

            return false;
        }

        return DefineReceiverDataProperty(target, name, value, !p.IsEmpty ? p.Attributes : defaultAttributes, throwError);
    }

    private protected bool SetIndexOnReceiver(uint name, JSValue value, JSValue receiver, JSPropertyAttributes defaultAttributes, bool throwError)
    {
        if (receiver != null && receiver is not JSObject)
        {
            if (throwError)
                throw NewTypeError($"Cannot add property {name} to {receiver}");

            return false;
        }

        var target = receiver as JSObject ?? this;
        if (!ReferenceEquals(target, this))
        {
            if (TrySetOrdinaryReceiverIndexedProperty(target, name, value, defaultAttributes, throwError, out var ordinaryResult))
                return ordinaryResult;

            var descriptor = target.GetOwnPropertyDescriptor(CreateNumber(name)) as JSObject;
            if (descriptor != null)
            {
                if (TrySetReceiverAccessorProperty(target, descriptor, receiver, value, name, throwError, out var accessorResult))
                    return accessorResult;

                if (IsReceiverReadOnly(descriptor))
                {
                    if (throwError)
                        throw NewTypeError($"Cannot modify property {name} of {target}");

                    return false;
                }

                return DefineReceiverDataProperty(target, name, value, GetReceiverAttributes(descriptor, defaultAttributes), throwError);
            }

            // CreateDataProperty(receiver, P, V) is receiver.[[DefineOwnProperty]]; the
            // extensibility check belongs to OrdinaryDefineOwnProperty and is performed
            // inside DefineReceiverDataProperty for an ordinary target. Calling
            // IsExtensible() here would additionally fire a proxy receiver's isExtensible
            // trap, which the spec's OrdinarySetWithOwnDescriptor does NOT (test262 Array
            // reverse/splice length-exceeding-integer-limit-with-proxy).
            return DefineReceiverDataProperty(target, name, value, CreateDataPropertyAttributes, throwError);
        }

        var p = target.GetInternalProperty(name, false);
        if (p.IsProperty)
        {
            if (p.set is IJSFunction setter)
            {
                setter.InvokeFunction(new Arguments(receiver ?? target, value));
                return true;
            }

            if (throwError)
                throw NewTypeError($"Cannot modify property {name} of {target} which has only a getter");

            return false;
        }

        if (p.IsReadOnly)
        {
            if (throwError)
                throw NewTypeError($"Cannot modify property {name} of {target}");

            return false;
        }

        if (target.IsFrozen())
        {
            if (throwError)
                throw NewTypeError($"Cannot modify property {name} of {target}");

            return false;
        }

        if (p.IsEmpty && !target.IsExtensible())
        {
            if (throwError)
                throw NewTypeError($"Cannot add property {name} to {target}");

            return false;
        }

        return DefineReceiverDataProperty(target, name, value, !p.IsEmpty ? p.Attributes : defaultAttributes, throwError);
    }

    private bool SetSymbolOnReceiver(IJSSymbol name, JSValue value, JSValue receiver, JSPropertyAttributes defaultAttributes, bool throwError)
    {
        if (receiver != null && receiver is not JSObject)
        {
            if (throwError)
                throw NewTypeError($"Cannot add property {name} to {receiver}");

            return false;
        }

        var target = receiver as JSObject ?? this;
        if (name.Key == SymbolIterator.Key)
            target.HasIterator = true;
        else if (SymbolAsyncIterator != null && name.Key == SymbolAsyncIterator.Key)
            target.HasAsyncIterator = true;

        if (!ReferenceEquals(target, this))
        {
            var symbolValue = (JSValue)(GetSymbolByKeyFactory?.Invoke(name.Key)
                ?? throw new InvalidOperationException($"Unknown symbol key {name.Key}"));
            var descriptor = target.GetOwnPropertyDescriptor(symbolValue) as JSObject;
            if (descriptor != null)
            {
                if (TrySetReceiverAccessorProperty(target, descriptor, receiver, value, name, throwError, out var accessorResult))
                    return accessorResult;

                if (IsReceiverReadOnly(descriptor))
                {
                    if (throwError)
                        throw NewTypeError($"Cannot modify property {name} of {target}");

                    return false;
                }

                return DefineReceiverDataProperty(target, name, value, GetReceiverAttributes(descriptor, defaultAttributes), throwError);
            }

            // CreateDataProperty(receiver, P, V) is receiver.[[DefineOwnProperty]]; the
            // extensibility check belongs to OrdinaryDefineOwnProperty and is performed
            // inside DefineReceiverDataProperty for an ordinary target. Calling
            // IsExtensible() here would additionally fire a proxy receiver's isExtensible
            // trap, which the spec's OrdinarySetWithOwnDescriptor does NOT (test262 Array
            // reverse/splice length-exceeding-integer-limit-with-proxy).
            return DefineReceiverDataProperty(target, name, value, CreateDataPropertyAttributes, throwError);
        }

        var p = target.GetInternalProperty(name, false);
        if (p.IsProperty)
        {
            if (p.set is IJSFunction setter)
            {
                setter.InvokeFunction(new Arguments(receiver ?? target, value));
                return true;
            }

            if (throwError)
                throw NewTypeError($"Cannot modify property {name} of {target} which has only a getter");

            return false;
        }

        if (p.IsReadOnly)
        {
            if (throwError)
                throw NewTypeError($"Cannot modify property {name} of {target}");

            return false;
        }

        if (target.IsFrozen())
        {
            if (throwError)
                throw NewTypeError($"Cannot modify property {name} of {target}");

            return false;
        }

        if (p.IsEmpty && !target.IsExtensible())
        {
            if (throwError)
                throw NewTypeError($"Cannot add property {name} to {target}");

            return false;
        }

        return DefineReceiverDataProperty(target, name, value, !p.IsEmpty ? p.Attributes : defaultAttributes, throwError);
    }

    /// <summary>
    /// Applies an ordinary [[Set]] whose receiver is a DIFFERENT plain object from the base
    /// whose prototype chain was walked, without materializing property descriptors.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the ordinary-object case of the generic receiver path below, which asks for a
    /// full descriptor object, reads four properties back out of it, builds a second
    /// descriptor, and hands that to [[DefineOwnProperty]]. Every plain <c>obj.x = 1</c> on an
    /// object that has a prototype arrives here — the chain walk in
    /// <c>SetValue</c> bottoms out at %Object.prototype% and comes back with the real object
    /// as the receiver — so that round-trip was the standing cost of the single most common
    /// write in JavaScript. See docs/performance-roadmap.md P1-1.
    /// </para>
    /// <para>
    /// Returns false (leaving <paramref name="result"/> unset) whenever anything about the
    /// target could make the generic path observably different, so the caller falls through:
    /// a non-exact <see cref="JSObject"/> (a Proxy, an array, a typed array, any exotic with
    /// its own [[DefineOwnProperty]]), a key that also names an array index and therefore
    /// lives in the element table, or a lazily-realized cell that the descriptor path cancels.
    /// </para>
    /// <para>
    /// The decisions below mirror the generic path exactly. For an existing data property
    /// <c>GetReceiverAttributes</c> reconstructs precisely that property's own attributes, so
    /// they are reused directly; an accessor own property makes the write fail per
    /// OrdinarySetWithOwnDescriptor without invoking the setter; and a missing property is the
    /// CreateDataProperty case, whose only failure is a non-extensible target.
    /// </para>
    /// </remarks>
    private static bool TrySetOrdinaryReceiverDataProperty(
        JSObject target,
        in KeyString name,
        JSValue value,
        JSPropertyAttributes defaultAttributes,
        bool throwError,
        out bool result)
    {
        result = false;

        if (target.GetType() != typeof(JSObject))
            return false;

        var metadata = name.Metadata;
        if (metadata.IsArrayIndex || metadata.IsCanonicalNumericIndex || metadata.IsPrivateName)
            return false;

        // Shape-only (item 2-9): the descriptor this path reads exists only as a slot plus an
        // attribute byte, and the Put it ends in is exactly the trie write the item removes.
        // Reconstructing the descriptor keeps the decision logic below identical rather than
        // duplicating it — a shape-only object can hold no accessor and no lazy cell, so the
        // two branches that test for them simply never fire.
        if (target.IsShapeOnlyStorage)
        {
            target.TryGetShapeOnlyProperty(name.Key, out var shapeOnly);
            if (shapeOnly.IsEmpty && !target.IsExtensible())
            {
                if (throwError)
                    throw NewTypeError($"Cannot modify property {name} of {target}");

                return true;
            }

            if (!shapeOnly.IsEmpty && shapeOnly.IsReadOnly)
            {
                if (throwError)
                    throw NewTypeError($"Cannot modify property {name} of {target}");

                return true;
            }

            var shapeOnlyAttributes = shapeOnly.IsEmpty ? CreateDataPropertyAttributes : shapeOnly.Attributes;
            if (target.TryShapeOnlySetDataProperty(in name, value, shapeOnlyAttributes))
            {
                target.PropertyChanged?.Invoke(target, (name.Key, uint.MaxValue, null));
                result = true;
                return true;
            }
        }

        ref var own = ref target.OwnProperties().GetValue(name.Key);
        JSPropertyAttributes attributes;

        if (own.IsEmpty)
        {
            if (!target.IsExtensible())
            {
                if (throwError)
                    throw NewTypeError($"Cannot modify property {name} of {target}");

                return true;
            }

            attributes = CreateDataPropertyAttributes;
        }
        else
        {
            // An accessor here is the receiver's own; OrdinarySetWithOwnDescriptor returns
            // false rather than calling its setter (a setter found while walking the BASE's
            // chain was already handled before this point).
            if (own.IsProperty)
            {
                if (throwError)
                    throw NewTypeError($"Cannot assign to property {name} of {target} whose receiver has an accessor");

                return true;
            }

            if (own.IsReadOnly)
            {
                if (throwError)
                    throw NewTypeError($"Cannot modify property {name} of {target}");

                return true;
            }

            // Cancelling a pending lazy cell is the descriptor path's job; leave it to it.
            if (own.value is LazyDataPropertyCell)
                return false;

            attributes = own.Attributes;
        }

        target.ownProperties.Put(name.Key) = new JSProperty(name, value, attributes);
        target.TrackShapeDataProperty(in name, value, attributes);
        target.PropertyChanged?.Invoke(target, (name.Key, uint.MaxValue, null));
        result = true;
        return true;
    }

    /// <summary>
    /// Whether an indexed [[Set]] onto this object as a FOREIGN receiver may write the
    /// element table directly instead of going through [[DefineOwnProperty]] with a
    /// descriptor. True only where the ordinary element semantics apply.
    /// </summary>
    /// <remarks>
    /// Overridden by <c>JSArray</c>, whose indexed define is the ordinary one (it overrides
    /// only the <c>JSValue</c>-keyed overload, to special-case the <c>length</c> string).
    /// Integer-indexed exotics (<c>JSTypedArray</c>), mapped <c>arguments</c>, and proxies all
    /// derive from <see cref="JSObject"/> rather than the array, so the exact-type test here
    /// excludes them and they keep the descriptor path.
    /// </remarks>
    internal virtual bool SupportsOrdinaryIndexedWrite => GetType() == typeof(JSObject);

    /// <summary>
    /// The indexed twin of <see cref="TrySetOrdinaryReceiverDataProperty"/>.
    /// </summary>
    /// <remarks>
    /// Storing into an element that is not already present walks the prototype chain (the
    /// slot is absent, so <c>SetValue</c> recurses), bottoms out at %Object.prototype%, and
    /// returns here with the real array as a foreign receiver — so filling a fresh array cost
    /// a `JSNumber` for the key plus a four-property descriptor object *per element*, around
    /// 1 350 bytes each, while overwriting an element already present cost nothing. That is
    /// why `new Array(1000)` plus a fill allocated 1.3 MB. Same defect as P1-1, on the
    /// indexed path; see docs/performance-roadmap.md.
    /// </remarks>
    private static bool TrySetOrdinaryReceiverIndexedProperty(
        JSObject target,
        uint name,
        JSValue value,
        JSPropertyAttributes defaultAttributes,
        bool throwError,
        out bool result)
    {
        result = false;

        if (!target.SupportsOrdinaryIndexedWrite)
            return false;

        JSPropertyAttributes attributes;
        if (!target.elements.TryGetValue(name, out var existing) || existing.IsEmpty)
        {
            // CreateDataProperty on the receiver; its only ordinary failure is extensibility.
            if (!target.IsExtensible())
            {
                if (throwError)
                    throw NewTypeError($"Cannot modify property {name} of {target}");

                return true;
            }

            attributes = CreateDataPropertyAttributes;
        }
        else
        {
            // OrdinarySetWithOwnDescriptor: the receiver's own accessor makes the write fail
            // rather than invoking its setter.
            if (existing.IsProperty)
            {
                if (throwError)
                    throw NewTypeError($"Cannot assign to property {name} of {target} whose receiver has an accessor");

                return true;
            }

            if (existing.IsReadOnly)
            {
                if (throwError)
                    throw NewTypeError($"Cannot modify property {name} of {target}");

                return true;
            }

            // Cancelling a pending lazy cell is the descriptor path's job; leave it to it.
            if (existing.value is LazyDataPropertyCell)
                return false;

            // GetReceiverAttributes reconstructs exactly these for a data property.
            attributes = existing.Attributes;
        }

        target.elements.Put(name, value, attributes);
        target.UpdateArrayLengthIfNeeded(name);
        target.NotifyIndexedPropertyMutation();
        target.PropertyChanged?.Invoke(target, (uint.MaxValue, name, null));
        result = true;
        return true;
    }

    private static bool TrySetReceiverAccessorProperty(JSObject target, JSObject descriptor, JSValue receiver, JSValue value, object name, bool throwError, out bool result)
    {
        var hasGet = !descriptor.GetInternalProperty(KeyStrings.get, false).IsEmpty;
        var hasSet = !descriptor.GetInternalProperty(KeyStrings.set, false).IsEmpty;
        if (!hasGet && !hasSet)
        {
            result = false;
            return false;
        }

        // This branch runs only when the base (the object whose prototype chain was
        // walked) resolved the property to a DATA descriptor — i.e. we are applying the
        // write to a DISTINCT receiver (super.x = / Reflect.set with a 4th argument).
        // OrdinarySetWithOwnDescriptor step: if the receiver's own property is an
        // accessor, return false. The receiver's setter is NOT invoked here — a setter
        // is only honoured when the accessor is found while walking the base's prototype
        // chain (handled by the IsProperty branches before reaching this receiver path).
        if (throwError)
            throw NewTypeError($"Cannot assign to property {name} of {target} whose receiver has an accessor");

        result = false;
        return true;
    }

    private static bool IsReceiverReadOnly(JSObject descriptor)
        => !descriptor.GetInternalProperty(KeyStrings.writable, false).IsEmpty
            && !descriptor[KeyStrings.writable].BooleanValue;

    private static JSPropertyAttributes GetReceiverAttributes(JSObject descriptor, JSPropertyAttributes defaultAttributes)
    {
        var attributes = JSPropertyAttributes.Value;
        if (IsReceiverReadOnly(descriptor))
            attributes |= JSPropertyAttributes.Readonly;

        if (!descriptor.GetInternalProperty(KeyStrings.enumerable, false).IsEmpty
            ? descriptor[KeyStrings.enumerable].BooleanValue
            : defaultAttributes.HasFlag(JSPropertyAttributes.Enumerable))
        {
            attributes |= JSPropertyAttributes.Enumerable;
        }

        if (!descriptor.GetInternalProperty(KeyStrings.configurable, false).IsEmpty
            ? descriptor[KeyStrings.configurable].BooleanValue
            : defaultAttributes.HasFlag(JSPropertyAttributes.Configurable))
        {
            attributes |= JSPropertyAttributes.Configurable;
        }

        return attributes;
    }

    private bool DefineReceiverDataProperty(JSObject target, KeyString name, JSValue value, JSPropertyAttributes attributes, bool throwError)
    {
        if (ReferenceEquals(target, this))
        {
            if (!target.TryShapeOnlySetDataProperty(in name, value, attributes))
            {
                target.OwnProperties().Put(name, value, attributes);
                target.TrackShapeDataProperty(in name, value, attributes);
            }
            target.PropertyChanged?.Invoke(target, (name.Key, uint.MaxValue, null));
            return true;
        }

        var descriptor = CreateDataDescriptor(value, attributes);
        var result = target.DefineProperty(name, descriptor);
        if (!result.IsBoolean || result.BooleanValue)
            return true;

        if (throwError)
            throw NewTypeError($"Cannot modify property {name} of {target}");

        return false;
    }

    private bool DefineReceiverDataProperty(JSObject target, uint name, JSValue value, JSPropertyAttributes attributes, bool throwError)
    {
        if (ReferenceEquals(target, this))
        {
            ref var elements = ref target.CreateElements();
            elements.Put(name, value, attributes);
            target.NotifyIndexedPropertyMutation();
            target.PropertyChanged?.Invoke(target, (uint.MaxValue, name, null));
            return true;
        }

        var descriptor = CreateDataDescriptor(value, attributes);
        var result = target.DefineProperty(name, descriptor);
        if (!result.IsBoolean || result.BooleanValue)
            return true;

        if (throwError)
            throw NewTypeError($"Cannot modify property {name} of {target}");

        return false;
    }

    private bool DefineReceiverDataProperty(JSObject target, IJSSymbol name, JSValue value, JSPropertyAttributes attributes, bool throwError)
    {
        if (ReferenceEquals(target, this))
        {
            target.symbols.Put(name.Key) = new JSProperty(name.Key, value, attributes);
            target.NotifyNamedPropertyMutation();
            target.PropertyChanged?.Invoke(target, (uint.MaxValue, uint.MaxValue, name));
            return true;
        }

        var descriptor = CreateDataDescriptor(value, attributes);
        var result = target.DefineProperty(name, descriptor);
        if (!result.IsBoolean || result.BooleanValue)
            return true;

        if (throwError)
            throw NewTypeError($"Cannot modify property {name} of {target}");

        return false;
    }

    // CreateDataPropertyOrThrow(this, key, value) for a public class field
    // initializer. An ordinary object stores the own data property directly; an
    // exotic object (e.g. a Proxy handed back by a `return`-override base
    // constructor) overrides these to route through [[DefineOwnProperty]], so its
    // defineProperty trap observes the field initialization.
    public virtual void CreateDataProperty(KeyString key, JSValue value)
    {
        if (!IsExtensible() && !HasOwnNamedProperty(key.Key))
            throw NewTypeError($"Cannot define property {key}, object is not extensible");
        FastAddValue(key, value, JSPropertyAttributes.EnumerableConfigurableValue);
    }

    public virtual void CreateDataProperty(uint index, JSValue value)
    {
        if (!IsExtensible() && elements.Get(index).IsEmpty)
            throw NewTypeError($"Cannot define property {index}, object is not extensible");
        FastAddValue(index, value, JSPropertyAttributes.EnumerableConfigurableValue);
    }

    public virtual void CreateDataProperty(JSValue key, JSValue value)
    {
        // CreateDataPropertyOrThrow on a non-extensible object fails when the
        // property is new (e.g. a public class field on a frozen instance handed
        // back by a return-override base constructor). Route through the typed
        // overloads so the same extensibility guard applies to every key kind.
        var propertyKey = key.ToKey(false);
        switch (propertyKey.Type)
        {
            case KeyType.UInt:
                CreateDataProperty(propertyKey.Index, value);
                return;
            case KeyType.String:
                CreateDataProperty(propertyKey.KeyString, value);
                return;
            case KeyType.Symbol:
                if (!IsExtensible() && symbols.GetRefOrDefault(propertyKey.Symbol.Key, ref JSProperty.Empty).IsEmpty)
                    throw NewTypeError($"Cannot define property {key}, object is not extensible");
                break;
        }
        FastAddValue(key, value, JSPropertyAttributes.EnumerableConfigurableValue);
    }

    internal static JSObject CreateDataDescriptor(JSValue value, JSPropertyAttributes attributes)
    {
        var descriptor = new JSObject();
        descriptor.FastAddValue(KeyStrings.value, value, JSPropertyAttributes.EnumerableConfigurableValue);
        descriptor.FastAddValue(KeyStrings.writable, attributes.HasFlag(JSPropertyAttributes.Readonly) ? BooleanFalse : BooleanTrue, JSPropertyAttributes.EnumerableConfigurableValue);
        descriptor.FastAddValue(KeyStrings.enumerable, attributes.HasFlag(JSPropertyAttributes.Enumerable) ? BooleanTrue : BooleanFalse, JSPropertyAttributes.EnumerableConfigurableValue);
        descriptor.FastAddValue(KeyStrings.configurable, attributes.HasFlag(JSPropertyAttributes.Configurable) ? BooleanTrue : BooleanFalse, JSPropertyAttributes.EnumerableConfigurableValue);
        return descriptor;
    }

    internal protected override JSValue GetValue(IJSSymbol key, JSValue receiver, bool throwError = true)
    {
        ref var p = ref symbols.GetRefOrDefault(key.Key, ref JSProperty.Empty);
        if (!p.IsEmpty)
            return (receiver ?? this).GetValue(p);

        return base.GetValue(key, receiver, throwError);
    }

    internal protected override JSValue GetValue(KeyString key, JSValue receiver, bool throwError = true)
    {
        // A private member read on an object whose class did not declare the private
        // name is a TypeError (brand check), not an `undefined` result. Throwing here
        // — before the ordinary own/prototype lookup — also covers private method
        // gets (InvokeMethod resolves the method through GetValue).
        if (IsPrivateName(in key))
            ThrowIfMissingPrivateMember(in key, reading: true);

        // Shape-only (item 2-9): the uncached read of an own named property. Materializing
        // here would hand the trie back to every object whose properties are ever read
        // without a warm inline cache, which is most of them, so this reconstructs the
        // descriptor from the slot instead. A shape-only object owns no private name and no
        // accessor, so the two tests below are answered `false` by construction.
        if (IsShapeOnlyStorage)
        {
            if (TryGetShapeOnlyProperty(key.Key, out var shapeOnly))
                return (receiver ?? this).GetValue(shapeOnly);
        }
        else
        {
            ref var p = ref ownProperties.GetValue(key.Key);
            if (!p.IsEmpty)
            {
                // A private accessor declared with only a setter has no [[Get]]: reading
                // it is a TypeError (PrivateGet, sec-privateget), not the `undefined` an
                // ordinary getterless accessor yields. Public accessors keep the undefined
                // result; this stricter behaviour is gated on the private-name marker.
                if (IsPrivateName(in key) && p.IsProperty && p.get is not IJSFunction)
                    throw NewTypeError($"Cannot read private member {PrivateDisplayName(in key)}: it was defined without a getter");

                return (receiver ?? this).GetValue(p);
            }
        }

        // A canonical array-index string key (e.g. "1") names the same property as
        // the integer index, which is stored in the element table. Canonicalize
        // directly from the key's text: routing through KeyStringToJSValue().ToKey()
        // would short-circuit, because that JSString carries a preset KeyString and
        // ToKey() returns it without ever testing for an array index.
        if (NumberParser.TryGetArrayIndex(key.Value, out var index))
            return GetValue(index, receiver, throwError);

        return base.GetValue(key, receiver, throwError);
    }

    public override JSValue GetValue(uint key, JSValue receiver, bool throwError = true)
    {
        var p = elements.Get(key);
        if (!p.IsEmpty)
        {
            if (p.IsValue)
                return ResolvePropertyValue(p.value);

            if (p.get is IJSFunction getter)
                return getter.InvokeFunction(new Arguments(receiver ?? this));

            return UndefinedValue;
        }

        return base.GetValue(key, receiver, throwError);
    }

    public virtual JSValue DefineProperty(JSValue key, JSObject propertyDescription)
    {
        var k = key.ToKey();
        return k.Type switch
        {
            KeyType.Empty => BooleanFalse,
            KeyType.UInt => DefineProperty(k.Index, propertyDescription),
            KeyType.String => DefineProperty(k.KeyString, propertyDescription),
            KeyType.Symbol => DefineProperty(k.Symbol, propertyDescription),
            _ => BooleanFalse,
        };
    }

    public virtual JSValue DefineProperty(IJSSymbol name, JSObject pd)
    {
        var key = name.Key;
        var old = symbols[key];
        var preserveCurrentValue = false;
        if (old.IsEmpty && !IsExtensible())
            return BooleanFalse;
        if (!old.IsEmpty)
        {
            preserveCurrentValue = CompletePropertyDescriptor(pd, in old);
            if (!IsCompatiblePropertyRedefinition(in old, pd, preserveCurrentValue))
                return BooleanFalse;
        }

        symbols.Put(key) = ToPropertyPreservingLazyValue(pd, key, in old, preserveCurrentValue);
        NotifyNamedPropertyMutation();
        PropertyChanged?.Invoke(this, (uint.MaxValue, uint.MaxValue, name));
        return UndefinedValue;
    }

    public virtual JSValue DefineProperty(uint key, JSObject pd)
    {
        ref var elements = ref GetElements(true);
        var old = elements[key];
        var preserveCurrentValue = false;
        if (old.IsEmpty && !IsExtensible())
            return BooleanFalse;
        if (!old.IsEmpty)
        {
            preserveCurrentValue = CompletePropertyDescriptor(pd, in old);
            if (!IsCompatiblePropertyRedefinition(in old, pd, preserveCurrentValue))
                return BooleanFalse;
        }

        elements.Set(key, ToPropertyPreservingLazyValue(pd, key, in old, preserveCurrentValue));
        UpdateArrayLengthIfNeeded(key);
        NotifyIndexedPropertyMutation();

        PropertyChanged?.Invoke(this, (uint.MaxValue, key, null));
        return UndefinedValue;
    }

    public virtual JSValue DefineProperty(in KeyString name, JSObject pd)
    {
        // Deliberately touches the `ownProperties` field rather than GetOwnProperties(),
        // which abandons the shape on every mutable access. That guard exists for callers
        // in other assemblies, who receive a mutable ref and could write behind the shape
        // tracker's back; this method owns the write and tracks it explicitly below.
        //
        // Abandoning here was what made shapes and the property inline cache unreachable
        // for most real JavaScript: an ordinary `obj.x = 1` on an object with a prototype
        // walks the chain, comes back through SetKeyStringOnReceiver's receiver-mismatch
        // branch, and lands here — so a single assignment, or one constructor storing one
        // field, permanently dropped the object into dictionary mode. See
        // docs/performance-roadmap.md P1-1.
        var key = name.Key;
        ref var old = ref OwnProperties().GetValue(name.Key);
        var preserveCurrentValue = false;
        if (old.IsEmpty && !IsExtensible())
            return BooleanFalse;

        if (!old.IsEmpty)
        {
            if (name.Key == KeyStrings.length.Key
                && old.IsValue
                && pd.GetInternalProperty(KeyStrings.value, false).IsEmpty
                && pd.GetInternalProperty(KeyStrings.get, false).IsEmpty
                && pd.GetInternalProperty(KeyStrings.set, false).IsEmpty)
            {
                var currentLength = Length;
                if (currentLength >= 0)
                    pd.FastAddValue(KeyStrings.value, CreateNumber(currentLength), JSPropertyAttributes.EnumerableConfigurableValue);
            }

            preserveCurrentValue = CompletePropertyDescriptor(pd, in old);
            if (!IsCompatiblePropertyRedefinition(in old, pd, preserveCurrentValue))
                return BooleanFalse;
        }
        // p.key = name;
        if (!preserveCurrentValue)
            CancelLazyDataProperty(in old);

        // Built before the Put: `old` is a ref into the property map, and Put may grow it.
        var replacement = ToPropertyPreservingLazyValue(pd, key, in old, preserveCurrentValue);
        ownProperties.Put(key) = replacement;

        // A plain data property keeps the fast layout. TrackShapeDataProperty is the single
        // decision point — it abandons on its own for a non-default attribute set, a private
        // name, or a receiver that is not an exact JSObject — so the only cases handled here
        // are the ones whose value is not a JSValue at all: an accessor (whose `value` holds
        // the getter) and a preserved LazyDataPropertyCell. Both must abandon, otherwise the
        // inline cache could hand back a slot the property no longer describes.
        if (replacement.IsValue && replacement.value is JSValue trackableValue)
        {
            TrackShapeDataProperty(in name, trackableValue, replacement.Attributes);
        }
        else
        {
            AbandonObjectShape();
            NotifyNamedPropertyMutation();
        }

        PropertyChanged?.Invoke(this, (name.Key, uint.MaxValue, null));
        return UndefinedValue;
    }

    private static bool CompletePropertyDescriptor(JSObject descriptor, in JSProperty current)
    {
        var hasConfigurable = !descriptor.GetInternalProperty(KeyStrings.configurable, false).IsEmpty;
        var hasEnumerable = !descriptor.GetInternalProperty(KeyStrings.enumerable, false).IsEmpty;
        var hasGet = !descriptor.GetInternalProperty(KeyStrings.get, false).IsEmpty;
        var hasSet = !descriptor.GetInternalProperty(KeyStrings.set, false).IsEmpty;
        var hasValue = !descriptor.GetInternalProperty(KeyStrings.value, false).IsEmpty;
        var hasWritable = !descriptor.GetInternalProperty(KeyStrings.writable, false).IsEmpty;
        var descriptorIsAccessor = hasGet || hasSet;
        var descriptorIsData = hasValue || hasWritable;

        if (!hasConfigurable)
            descriptor.FastAddValue(KeyStrings.configurable, current.IsConfigurable ? BooleanTrue : BooleanFalse, JSPropertyAttributes.EnumerableConfigurableValue);

        if (!hasEnumerable)
            descriptor.FastAddValue(KeyStrings.enumerable, current.IsEnumerable ? BooleanTrue : BooleanFalse, JSPropertyAttributes.EnumerableConfigurableValue);

        if (current.IsProperty)
        {
            if (!descriptorIsData && !hasGet)
                descriptor[KeyStrings.get] = current.get as JSValue ?? UndefinedValue;

            if (!descriptorIsData && !hasSet)
                descriptor[KeyStrings.set] = current.set as JSValue ?? UndefinedValue;

            return false;
        }

        var preserveCurrentValue = !descriptorIsAccessor
            && !hasValue
            && current.value is LazyDataPropertyCell;
        if (!descriptorIsAccessor && !hasValue)
            descriptor.FastAddValue(
                KeyStrings.value,
                preserveCurrentValue ? UndefinedValue : ResolvePropertyValue(current.value),
                JSPropertyAttributes.EnumerableConfigurableValue);

        if (!descriptorIsAccessor && !hasWritable)
            descriptor.FastAddValue(KeyStrings.writable, current.IsReadOnly ? BooleanFalse : BooleanTrue, JSPropertyAttributes.EnumerableConfigurableValue);

        return preserveCurrentValue;
    }

    private static JSProperty ToPropertyPreservingLazyValue(
        JSObject descriptor,
        uint key,
        in JSProperty current,
        bool preserveCurrentValue)
    {
        var replacement = descriptor.ToProperty(key);
        return preserveCurrentValue
            ? new JSProperty(key, replacement.get, replacement.set, current.value, replacement.Attributes)
            : replacement;
    }

    private static bool IsCompatiblePropertyRedefinition(
        in JSProperty current,
        JSObject descriptor,
        bool preserveCurrentValue)
    {
        if (current.IsConfigurable)
            return true;

        if (descriptor[KeyStrings.configurable].BooleanValue)
            return false;

        if (descriptor[KeyStrings.enumerable].BooleanValue != current.IsEnumerable)
            return false;

        var descriptorHasGet = !descriptor.GetInternalProperty(KeyStrings.get, false).IsEmpty;
        var descriptorHasSet = !descriptor.GetInternalProperty(KeyStrings.set, false).IsEmpty;
        var descriptorIsAccessor = descriptorHasGet || descriptorHasSet;
        if (descriptorIsAccessor != current.IsProperty)
            return false;

        if (current.IsProperty)
        {
            if (!descriptor[KeyStrings.get].StrictEquals(current.get as JSValue ?? JSUndefined.Value))
                return false;

            if (!descriptor[KeyStrings.set].StrictEquals(current.set as JSValue ?? JSUndefined.Value))
                return false;

            return true;
        }

        var descriptorWritable = descriptor[KeyStrings.writable].BooleanValue;
        if (current.IsReadOnly && descriptorWritable)
            return false;

        if (current.IsReadOnly
            && !preserveCurrentValue
            && !descriptor[KeyStrings.value].Is(ResolvePropertyValue(current.value)).BooleanValue)
        {
            return false;
        }

        return true;
    }

    public override IElementEnumerator GetAllKeys(bool showEnumerableOnly = true, bool inherited = true) => new KeyEnumerator(this, showEnumerableOnly, inherited);//var elements = this.elements;//if (elements != null)//{//    foreach (var (Key, Value) in elements.AllValues)//    {//        if (showEnumerableOnly)//        {//            if (!Value.IsEnumerable)//                continue;//        }//        yield return new JSNumber(Key);//    }//}//var ownProperties = this.ownProperties;//if (ownProperties != null)//{//    var en = new PropertySequence.Enumerator(ownProperties);//    while(en.MoveNext())//    {//        var p = en.Current;//        if (showEnumerableOnly)//        {//            if (!p.IsEnumerable)//                continue;//        }//        yield return p.ToJSValue();//    }//}//if (inherited)//{//    var @base = this.prototypeChain;//    if (@base != this && @base != null)//    {//        foreach (var i in @base.GetAllKeys(showEnumerableOnly))//            yield return i;//    }//}

    /// <summary>
    /// Implements ToPropertyDescriptor (ECMA-262 § 6.2.6.5): reads the well-known
    /// descriptor fields from <paramref name="userDescriptor"/> using [[HasProperty]]
    /// and [[Get]] — both of which consult the prototype chain — producing a fresh
    /// own-data-property record. Descriptor fields may therefore be inherited from
    /// the descriptor object's prototype or be supplied via accessors on it.
    /// </summary>
    internal static JSObject NormalizeDescriptor(JSObject userDescriptor)
    {
        var record = new JSObject();
        CopyDescriptorField(userDescriptor, record, KeyStrings.enumerable);
        CopyDescriptorField(userDescriptor, record, KeyStrings.configurable);
        CopyDescriptorField(userDescriptor, record, KeyStrings.value);
        CopyDescriptorField(userDescriptor, record, KeyStrings.writable);
        CopyDescriptorField(userDescriptor, record, KeyStrings.get);
        CopyDescriptorField(userDescriptor, record, KeyStrings.set);
        return record;
    }

    private static void CopyDescriptorField(JSObject source, JSObject record, in KeyString field)
    {
        // ToPropertyDescriptor (§6.2.5.5): presence is decided by [[HasProperty]] and the value is
        // read with [[Get]]. Both are observable on a scripted Proxy descriptor (its has/get traps),
        // so they must go through the object's own operations rather than the internal storage —
        // GetInternalProperty bypasses the traps, so a Proxy descriptor's accessors never fired.
        if (!source.HasProperty(field.ToJSValue()).BooleanValue)
            return;

        record.FastAddValue(field, source[field], JSPropertyAttributes.EnumerableConfigurableValue);
    }

    internal JSProperty ToProperty(uint key)
    {
        // Accessor-ness is decided by the *presence* of get/set fields, not their
        // values: { get: undefined } / { set: undefined } describe an accessor
        // property (with the respective accessor absent), not a data property.
        var hasGet = !GetInternalProperty(KeyStrings.get, false).IsEmpty;
        var hasSet = !GetInternalProperty(KeyStrings.set, false).IsEmpty;
        var hasValue = !GetInternalProperty(KeyStrings.value, false).IsEmpty;
        var hasWritable = !GetInternalProperty(KeyStrings.writable, false).IsEmpty;
        var isAccessor = hasGet || hasSet;

        if (isAccessor && (hasValue || hasWritable))
            throw NewTypeError("Invalid property.  Cannot both specify accessors and a value or writable attribute");

        var pt = JSPropertyAttributes.Empty;

        if (this[KeyStrings.configurable].BooleanValue)
            pt |= JSPropertyAttributes.Configurable;

        if (this[KeyStrings.enumerable].BooleanValue)
            pt |= JSPropertyAttributes.Enumerable;

        if (isAccessor)
        {
            JSValue pget = null;
            JSValue pset = null;

            if (hasGet)
            {
                var get = this[KeyStrings.get];
                if (!get.IsUndefined)
                {
                    if (get is not IJSFunction)
                        throw NewTypeError("Getter must be a function");

                    pget = get;
                }
            }

            if (hasSet)
            {
                var set = this[KeyStrings.set];
                if (!set.IsUndefined)
                {
                    if (set is not IJSFunction)
                        throw NewTypeError("Setter must be a function");

                    pset = set;
                }
            }

            pt |= JSPropertyAttributes.Property;
            return new JSProperty(key, pget, pset, null, pt);
        }

        if (!this[KeyStrings.writable].BooleanValue)
            pt |= JSPropertyAttributes.Readonly;

        pt |= JSPropertyAttributes.Value;
        return new JSProperty(key, null, null, this[KeyStrings.value], pt);
    }

    public override JSValue Delete(in KeyString key)
    {
        ref var deleteOwn = ref OwnProperties();
        var property = deleteOwn.GetValue(key.Key);
        if (!property.IsEmpty && !property.IsConfigurable)
            return BooleanFalse;

        if (deleteOwn.RemoveAt(key.Key))
        {
            CancelLazyDataProperty(in property);
            AbandonObjectShape();
            NotifyNamedPropertyMutation();
            PropertyChanged?.Invoke(this, (key.Key, uint.MaxValue, null));
            return BooleanTrue;
        }

        return BooleanTrue;
    }

    public override JSValue Delete(uint key)
    {
        if (elements.TryGetValue(key, out var property) && !property.IsConfigurable)
            return BooleanFalse;

        if (elements.RemoveAt(key))
        {
            NotifyIndexedPropertyMutation();
            PropertyChanged?.Invoke(this, (uint.MaxValue, key, null));
            return BooleanTrue;
        }

        return BooleanTrue;
    }

    public override JSValue Delete(IJSSymbol symbol)
    {
        if (symbols.TryGetValue(symbol.Key, out var property) && !property.IsConfigurable)
            return BooleanFalse;

        if (symbols.RemoveAt(symbol.Key))
        {
            NotifyNamedPropertyMutation();
            PropertyChanged?.Invoke(this, (uint.MaxValue, uint.MaxValue, symbol));
            return BooleanTrue;
        }

        return BooleanTrue;
    }
    internal override bool TryGetValue(uint i, out JSProperty value) => elements.TryGetValue(i, out value);

    internal override bool TryGetElement(uint i, out JSValue value)
    {
        if (elements.TryGetValue(i, out var p))
        {
            value = GetValue(p);
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>
    /// Moves elements from `start` to `to`.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="count"></param>
    /// <param name="to"></param>
    internal override void MoveElements(int start, int to)
    {
        ref var elements = ref CreateElements();

        var end = Length - 1;
        var diff = to - start;
        if (start > to)
        {

            for (uint i = (uint)start, j = (uint)to; i <= end; i++, j++)
            {
                if (TryRemove(i, out var p))
                    elements.Set(j, p);
            }

            Length += diff;
            return;
        }
        else
        {
            for (int i = end, j = Length + diff - 1; i >= start; i--, j--)
            {
                if (TryRemove((uint)i, out var p))
                    elements.Set((uint)j, p);
            }

            Length += diff;
        }

        PropertyChanged?.Invoke(this, (uint.MaxValue, uint.MaxValue, null));
    }

    /// <summary>
    /// Used in pop
    /// </summary>
    /// <param name="i"></param>
    /// <param name="p"></param>
    /// <returns></returns>
    internal override bool TryRemove(uint i, out JSProperty p)
    {
        if (elements.TryRemove(i, out p))
        {
            PropertyChanged?.Invoke(this, (uint.MaxValue, i, null));
            return true;
        }

        if (prototypeChain != null)
            return ((IJSPrototype)prototypeChain).TryRemove(i, out p);

        return false;
    }
    public override IElementEnumerator GetElementEnumerator()
    {
        if (HasIterator)
        {
            var v = GetValue(symbols[SymbolIterator.Key]);
            if (!v.IsFunction)
                throw NewTypeError("@@iterator is not a function");

            var iterator = v.InvokeFunction(new Arguments(this));
            if (!iterator.IsObject)
                throw NewTypeError("@@iterator result is not an object");

            return new JSIterator(iterator);
        }

        return new ElementEnumerator(this);
    }

    // Enumerates the object's own integer-indexed elements for key enumeration
    // (Object.keys / for-in / etc.), which must never invoke the iterator protocol.
    // When the object carries its *own* @@iterator the iterator-aware
    // GetElementEnumerator would (correctly, for for-of) honour it — and throw if it
    // is non-callable (e.g. `o[Symbol.iterator] = 'x'`) — so bypass it with the raw
    // element walk. Otherwise delegate to GetElementEnumerator so exotic objects
    // (arrays, typed arrays) keep their specialised, hole-aware element enumeration.
    internal virtual IElementEnumerator GetOwnIndexedElementEnumerator(bool enumerableOnly = false)
    {
        // Objects whose indexed data lives in the ordinary `elements` map (object
        // literals, class prototypes, …) are walked directly so a non-enumerable
        // indexed property (e.g. a computed-number class method `[1]() {}`) is
        // skipped during key enumeration (Object.keys / for-in). A subclass that
        // specialises element iteration (array, typed array, string, …) overrides
        // GetElementEnumerator; honour that specialised, hole-aware walk for those
        // (their indexed elements are always enumerable). An object carrying its
        // own @@iterator takes the raw slot walk so the user iterator is not run.
        if (!HasIterator)
        {
            var specialized = GetElementEnumerator();
            if (specialized is not ElementEnumerator)
                return specialized;
        }

        return new ElementEnumerator(this, enumerableOnly);
    }

    // Walks ONLY the object's own integer-indexed element slots (never an overridden
    // iterator walk). A subclass whose GetElementEnumerator runs an iterator protocol
    // (a generator / built-in iterator) overrides GetOwnIndexedElementEnumerator to use
    // this, so its yielded values are not mistaken for indexed own keys during key
    // enumeration (Object.keys / getOwnPropertyNames / for-in).
    internal IElementEnumerator GetOwnElementSlotEnumerator(bool enumerableOnly = false)
        => new ElementEnumerator(this, enumerableOnly);

    public override IElementEnumerator GetIterableEnumerator()
    {
        var iterator = this[SymbolIterator];
        if (iterator.IsNullOrUndefined)
            throw NewTypeError(NotIterable(this));

        if (!iterator.IsFunction)
            throw NewTypeError("@@iterator is not a function");

        var iteratorResult = iterator.InvokeFunction(new Arguments(this));
        if (!iteratorResult.IsObject)
            throw NewTypeError("@@iterator result is not an object");

        return new JSIterator(iteratorResult);
    }

    public override IElementEnumerator GetAsyncElementEnumerator()
    {
        if (SymbolAsyncIterator != null
            && (HasAsyncIterator || symbols.TryGetValue(SymbolAsyncIterator.Key, out _)))
        {
            var v = GetValue(symbols[SymbolAsyncIterator.Key]);
            if (!v.IsFunction)
                throw NewTypeError("@@asyncIterator is not a function");

            var iterator = v.InvokeFunction(new Arguments(this));
            if (!iterator.IsObject)
                throw NewTypeError("@@asyncIterator result is not an object");

            return new JSIterator(iterator, awaitResult: true);
        }

        return GetElementEnumerator();
    }

    public override IElementEnumerator GetAsyncIterableEnumerator()
    {
        if (SymbolAsyncIterator != null)
        {
            var asyncIterator = this[SymbolAsyncIterator];
            if (!asyncIterator.IsNullOrUndefined)
            {
                if (!asyncIterator.IsFunction)
                    throw NewTypeError("@@asyncIterator is not a function");

                var iterator = asyncIterator.InvokeFunction(new Arguments(this));
                if (!iterator.IsObject)
                    throw NewTypeError("@@asyncIterator result is not an object");

                return new JSIterator(iterator, awaitResult: true);
            }
        }

        return GetIterableEnumerator();
    }

    private struct ElementEnumerator(JSObject @object, bool enumerableOnly = false) : IElementEnumerator
    {
        ElementArray.ValueEnumerator en = @object.elements.StoredValues().GetEnumerator();
        readonly bool enumerableOnly = enumerableOnly;

        // Advance to the next stored element, skipping non-enumerable ones when key
        // enumeration requested enumerable-only (Object.keys / for-in).
        private bool MoveNextSlot(out uint key, out JSProperty prop)
        {
            while (en.MoveNext())
            {
                (key, prop) = en.Current;
                if (!enumerableOnly || prop.IsEnumerable)
                    return true;
            }

            key = 0;
            prop = default;
            return false;
        }

        public bool MoveNext(out bool hasValue, out JSValue value, out uint index)
        {
            if (MoveNextSlot(out var key, out var prop))
            {
                value = @object.GetValue(prop);
                index = key;
                hasValue = true;
                return true;
            }

            hasValue = false;
            value = UndefinedValue;
            index = 0;
            return false;
        }

        public bool MoveNext(out JSValue value)
        {
            if (MoveNextSlot(out var _, out var prop))
            {
                value = @object.GetValue(prop);
                return true;
            }

            value = UndefinedValue;
            return false;
        }

        public bool MoveNextOrDefault(out JSValue value, JSValue @default)
        {
            if (MoveNextSlot(out var _, out var prop))
            {
                value = @object.GetValue(prop);
                return true;
            }

            value = @default;
            return false;
        }

        public JSValue NextOrDefault(JSValue @default)
        {
            if (MoveNextSlot(out var _, out var prop))
                return @object.GetValue(prop);

            return @default;
        }
    }
}
