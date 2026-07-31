using Broiler.JavaScript.Ast.Misc;

namespace Broiler.JavaScript.Runtime;

/// <summary>
/// A data property whose value is computed on every read rather than stored.
/// </summary>
/// <remarks>
/// <para>
/// Unlike an accessor property, a property backed by one of these is still an ordinary
/// <em>data</em> property: <c>[[Get]]</c>, <c>[[GetOwnProperty]]</c> and
/// <c>Object.getOwnPropertyDescriptor</c> all report <c>value</c>/<c>writable</c> exactly as
/// they would for a stored value, because <see cref="JSValue.ResolvePropertyValue"/> resolves
/// the cell before any of them observe it. That distinction matters where the spec (or a web-
/// reality extension) mandates a data property but the value is a function of live engine
/// state — the Annex B <c>f.caller</c> / <c>f.arguments</c> pair being the motivating case.
/// </para>
/// <para>
/// Distinct from <see cref="LazyDataPropertyCell"/>, which realizes <em>once</em> and replaces
/// itself. A deferred value is re-read every time and must stay cheap.
/// </para>
/// </remarks>
internal interface IDeferredPropertyValue : IPropertyValue
{
    /// <summary>Computes the property's current value. Never returns <c>null</c>.</summary>
    JSValue Resolve();
}
