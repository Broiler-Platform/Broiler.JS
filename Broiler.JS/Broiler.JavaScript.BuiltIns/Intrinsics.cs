using Broiler.JavaScript.BuiltIns.Function;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.BuiltIns;

/// <summary>
/// The current realm's intrinsic built-in constructors and prototypes, by global name.
/// </summary>
/// <remarks>
/// Every built-in class registered on a realm's global records its constructor and prototype
/// there (<see cref="JSContext.RegisterIntrinsic"/>). Code that needs %String.prototype%,
/// %Iterator.prototype% or the %Promise% a SpeciesConstructor defaults to reads it here, not
/// from the global binding of the same name: guest code may have replaced, deleted or turned
/// that binding into an accessor. The global binding is read only for a name the current realm
/// never registered.
/// </remarks>
internal static class Intrinsics
{
    public static JSObject Prototype(in KeyString name)
        => JSContext.CurrentIntrinsicPrototype(name)
            ?? ((JSEngine.Current as JSObject)?[name] as JSFunction)?.prototype;

    public static JSValue Constructor(in KeyString name)
        => (JSValue)JSContext.CurrentIntrinsicConstructor(name)
            ?? (JSEngine.Current as JSObject)?[name];

    /// <summary>
    /// The current realm's intrinsic prototype for a class that lives on a namespace object and
    /// not on the global (%Temporal.PlainDate.prototype%, %Intl.NumberFormat.prototype%),
    /// recorded under its qualified name (<paramref name="qualifiedName"/>, e.g.
    /// <c>Temporal.PlainDate</c>) when the realm's namespace object was created. The namespace
    /// object guest code sees is never consulted — it may have been replaced, deleted or given
    /// an impostor member. A realm whose namespace has not been created yet (a lazy bootstrap
    /// profile) creates it through <paramref name="createNamespace"/>, which must return the
    /// realm's one namespace object and register its classes, so the prototype is the one the
    /// guest's own namespace carries once it materializes.
    /// </summary>
    public static JSObject NamespacedPrototype(in KeyString qualifiedName, BuiltInFeatureId feature, System.Action<JSContext> createNamespace)
    {
        if (JSEngine.Current is not JSContext realm)
            return null;

        var prototype = realm.GetIntrinsicPrototype(qualifiedName);
        if (prototype != null || !realm.Options.BootstrapProfile.Includes(feature))
            return prototype;

        createNamespace(realm);
        return realm.GetIntrinsicPrototype(qualifiedName);
    }

    /// <summary>%Temporal.X.prototype% for <paramref name="qualifiedName"/> <c>Temporal.X</c>.</summary>
    public static JSObject TemporalPrototype(in KeyString qualifiedName)
        => NamespacedPrototype(qualifiedName, BuiltInFeatureId.Temporal,
            static realm => BuiltInsAssemblyInitializer.CreateTemporalObject(realm));

    /// <summary>%Intl.X.prototype% for <paramref name="qualifiedName"/> <c>Intl.X</c>.</summary>
    public static JSObject IntlPrototype(in KeyString qualifiedName)
        => NamespacedPrototype(qualifiedName, BuiltInFeatureId.Intl,
            static realm => Intl.JSIntl.GetIntlObject(realm));
}
