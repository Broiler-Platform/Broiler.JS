using System;
using System.Collections.Generic;
using Broiler.JavaScript.BuiltIns.Array;
using Broiler.JavaScript.BuiltIns.Symbol;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Storage;

namespace Broiler.JavaScript.Modules;

/// <summary>
/// A module namespace exotic object (ES2024 10.4.6): the object <c>import * as ns</c>,
/// <c>import()</c> and <c>export * as ns</c> expose for a module.
/// </summary>
/// <remarks>
/// <para>
/// Its string-keyed properties are exactly the module's exported names, in code-unit order, each
/// reading the exporting module's binding live: <c>{ writable: true, enumerable: true,
/// configurable: false }</c> as a descriptor, but refusing every [[Set]], [[Delete]] and
/// incompatible [[DefineOwnProperty]]. A read of a binding still in its temporal dead zone throws
/// the binding's ReferenceError. The object has a null prototype, is not extensible, and carries
/// one symbol-keyed property, <c>@@toStringTag</c>, whose value is <c>"Module"</c>.
/// </para>
/// <para>
/// The resolution of each exported name is computed once, when the namespace is created: after
/// linking, ResolveExport is side-effect free and returns the same answer every time (the spec's
/// note to [[Get]] invites caching it).
/// </para>
/// </remarks>
public sealed class JSModuleNamespace : JSObject
{
    private readonly string[] exports;
    private readonly Dictionary<string, JSModule.ResolvedBinding> bindings;

    internal JSModuleNamespace(IReadOnlyList<(string Name, JSModule.ResolvedBinding Binding)> resolvedExports)
    {
        BasePrototypeObject = null;

        var sorted = new List<(string Name, JSModule.ResolvedBinding Binding)>(resolvedExports);
        sorted.Sort(static (a, b) => string.CompareOrdinal(a.Name, b.Name));

        exports = new string[sorted.Count];
        bindings = new Dictionary<string, JSModule.ResolvedBinding>(sorted.Count, StringComparer.Ordinal);
        for (var i = 0; i < sorted.Count; i++)
        {
            exports[i] = sorted[i].Name;
            bindings[sorted[i].Name] = sorted[i].Binding;
        }

        // 28.3.1 @@toStringTag: { [[Writable]]: false, [[Enumerable]]: false, [[Configurable]]: false }.
        FastAddValue((IJSSymbol)JSSymbol.toStringTag, CreateString("Module"), JSPropertyAttributes.ReadonlyValue);
        base.PreventExtensions();
    }

    /// <summary>The exported names, in the order [[OwnPropertyKeys]] reports them.</summary>
    public IReadOnlyList<string> ExportNames => exports;

    private static string NameOf(uint index) => index.ToString(System.Globalization.CultureInfo.InvariantCulture);

    private bool TryGetExport(string name, out JSModule.ResolvedBinding binding)
        => bindings.TryGetValue(name, out binding);

    /// <summary>[[Get]] of an exported name: the live value of the binding it resolves to.</summary>
    private static JSValue Read(string name, JSModule.ResolvedBinding binding)
    {
        if (binding.IsNamespace)
            return binding.Module.GetNamespace();

        var cell = binding.Module.GetBindingCell(binding.BindingName)
            ?? throw JSEngine.NewReferenceError($"Cannot access '{name}' before its module is instantiated");
        return cell.Value;
    }

    private static JSObject Descriptor(JSValue value)
        => CreateDataDescriptor(value, JSPropertyAttributes.Value | JSPropertyAttributes.Enumerable);

    // [[GetPrototypeOf]] / [[SetPrototypeOf]] (SetImmutablePrototype) / [[IsExtensible]] /
    // [[PreventExtensions]].

    public override JSValue GetPrototypeOf() => NullValue;

    public override void SetPrototypeOf(JSValue proto)
    {
        if (!TrySetPrototypeOf(proto, out var error))
            throw JSEngine.NewTypeError(error);
    }

    public override bool TrySetPrototypeOf(JSValue proto, out string error)
    {
        if (proto.IsNull)
        {
            error = null;
            return true;
        }

        error = "Cannot set the prototype of a module namespace object";
        return false;
    }

    public override bool IsExtensible() => false;

    public override bool PreventExtensions() => true;

    // [[GetOwnProperty]]

    public override JSValue GetOwnPropertyDescriptor(JSValue name)
    {
        var key = name.ToKey(false);
        switch (key.Type)
        {
            case KeyType.String:
                return TryGetExport(key.KeyString.ToString(), out var binding)
                    ? Descriptor(Read(key.KeyString.ToString(), binding))
                    : UndefinedValue;

            case KeyType.UInt:
                var indexName = NameOf(key.Index);
                return TryGetExport(indexName, out var indexBinding)
                    ? Descriptor(Read(indexName, indexBinding))
                    : UndefinedValue;

            default:
                return base.GetOwnPropertyDescriptor(name);
        }
    }

    internal protected override bool HasOwnProperty(in PropertyKey key) => key.Type switch
    {
        KeyType.String => bindings.ContainsKey(key.KeyString.ToString()),
        KeyType.UInt => bindings.ContainsKey(NameOf(key.Index)),
        _ => base.HasOwnProperty(in key),
    };

    // [[DefineOwnProperty]]

    public override JSValue DefineProperty(JSValue key, JSObject propertyDescription)
    {
        var k = key.ToKey();
        return k.Type switch
        {
            KeyType.String => DefineExport(k.KeyString.ToString(), propertyDescription),
            KeyType.UInt => DefineExport(NameOf(k.Index), propertyDescription),
            KeyType.Symbol => base.DefineProperty(k.Symbol, propertyDescription),
            _ => BooleanFalse,
        };
    }

    public override JSValue DefineProperty(in KeyString name, JSObject pd) => DefineExport(name.ToString(), pd);

    public override JSValue DefineProperty(uint key, JSObject pd) => DefineExport(NameOf(key), pd);

    private JSValue DefineExport(string name, JSObject descriptor)
    {
        if (!TryGetExport(name, out var binding))
            return BooleanFalse;

        // Step 2: `current` is ? [[GetOwnProperty]], so a binding in its TDZ throws here.
        var current = Read(name, binding);

        bool Has(in KeyString field) => !descriptor.GetInternalProperty(field, false).IsEmpty;

        if (Has(KeyStrings.configurable) && descriptor[KeyStrings.configurable].BooleanValue)
            return BooleanFalse;
        if (Has(KeyStrings.enumerable) && !descriptor[KeyStrings.enumerable].BooleanValue)
            return BooleanFalse;
        if (Has(KeyStrings.get) || Has(KeyStrings.set))
            return BooleanFalse;
        if (Has(KeyStrings.writable) && !descriptor[KeyStrings.writable].BooleanValue)
            return BooleanFalse;
        if (Has(KeyStrings.value))
            return descriptor[KeyStrings.value].Is(current);

        return BooleanTrue;
    }

    public override void CreateDataProperty(KeyString key, JSValue value) => CreateDataPropertyOrThrow(key.ToJSValue(), value);

    public override void CreateDataProperty(uint index, JSValue value) => CreateDataPropertyOrThrow(CreateString(NameOf(index)), value);

    public override void CreateDataProperty(JSValue key, JSValue value) => CreateDataPropertyOrThrow(key, value);

    private void CreateDataPropertyOrThrow(JSValue key, JSValue value)
    {
        var result = DefineProperty(key, CreateDataDescriptor(value, JSPropertyAttributes.EnumerableConfigurableValue));
        if (result.IsBoolean && !result.BooleanValue)
            throw JSEngine.NewTypeError($"Cannot define property {key} on a module namespace object");
    }

    // [[HasProperty]]

    public override JSValue HasProperty(JSValue propertyKey)
    {
        var key = propertyKey.ToKey(false);
        return key.Type switch
        {
            KeyType.String => bindings.ContainsKey(key.KeyString.ToString()) ? BooleanTrue : BooleanFalse,
            KeyType.UInt => bindings.ContainsKey(NameOf(key.Index)) ? BooleanTrue : BooleanFalse,
            _ => base.HasOwnProperty(in key) ? BooleanTrue : BooleanFalse,
        };
    }

    // [[Get]]

    internal protected override JSValue GetValue(KeyString key, JSValue receiver, bool throwError = true)
    {
        var name = key.ToString();
        return TryGetExport(name, out var binding) ? Read(name, binding) : UndefinedValue;
    }

    public override JSValue GetValue(uint key, JSValue receiver, bool throwError = true)
    {
        var name = NameOf(key);
        return TryGetExport(name, out var binding) ? Read(name, binding) : UndefinedValue;
    }

    // [[Set]] always returns false.

    internal protected override bool SetValue(KeyString name, JSValue value, JSValue receiver, bool throwError = true)
        => RefuseSet(name.ToString(), throwError);

    public override bool SetValue(uint name, JSValue value, JSValue receiver, bool throwError = true)
        => RefuseSet(NameOf(name), throwError);

    internal protected override bool SetValue(IJSSymbol name, JSValue value, JSValue receiver, bool throwError = true)
        => RefuseSet(name.ToString(), throwError);

    private static bool RefuseSet(string name, bool throwError)
    {
        if (throwError)
            throw JSEngine.NewTypeError($"Cannot assign to '{name}' of a module namespace object");

        return false;
    }

    // [[Delete]]

    public override JSValue Delete(JSValue index)
    {
        var key = index.ToKey(false);
        return key.Type switch
        {
            KeyType.String => Delete(key.KeyString),
            KeyType.UInt => Delete(key.Index),
            KeyType.Symbol => Delete(key.Symbol),
            _ => BooleanTrue,
        };
    }

    public override JSValue Delete(in KeyString key) => bindings.ContainsKey(key.ToString()) ? BooleanFalse : BooleanTrue;

    public override JSValue Delete(uint key) => bindings.ContainsKey(NameOf(key)) ? BooleanFalse : BooleanTrue;

    public override JSValue Delete(IJSSymbol symbol) => base.Delete(symbol);

    // [[OwnPropertyKeys]]: the exports, then the symbol keys.

    public override IElementEnumerator GetAllKeys(bool showEnumerableOnly = true, bool inherited = true)
    {
        var keys = new JSArray();
        foreach (var name in exports)
        {
            // The enumerable-key walk (for-in, Object.keys and their kind) asks [[GetOwnProperty]]
            // of each key, which is a [[Get]]: a binding still in its temporal dead zone throws
            // its ReferenceError here, as the specification's EnumerableOwnProperties does.
            if (showEnumerableOnly)
                Read(name, bindings[name]);

            keys.Add(CreateString(name));
        }

        // The one symbol key, @@toStringTag, lives in the ordinary symbol storage: callers that
        // want [[OwnPropertyKeys]] append the object's own symbols after these string keys, as
        // they do for an ordinary object, so it is not listed here as well.
        return keys.GetElementEnumerator();
    }

    // Spread, Object.assign, JSON.stringify and a Proxy's ownKeys invariant read each property
    // through [[OwnPropertyKeys]], [[GetOwnProperty]] and [[Get]], never from the (empty)
    // property storage.
    private protected override bool UseObservableSpreadCopy => true;
}
