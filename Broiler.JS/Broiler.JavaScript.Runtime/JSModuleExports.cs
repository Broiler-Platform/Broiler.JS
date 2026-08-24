namespace Broiler.JavaScript.Runtime;

/// <summary>
/// Runtime support for export forms whose work is not known until the source module has been
/// evaluated.
/// </summary>
public static class JSModuleExports
{
    /// <summary>
    /// Implements the copy half of <c>export * from '…'</c>: republishes every NAMED export of
    /// <paramref name="source"/> on <paramref name="target"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>default</c> is deliberately not among them. A star re-export forwards a module's named
    /// exports only (ES2024 16.2.3.7, ExportEntries for <c>ExportDeclaration : export
    /// ExportFromClause FromClause</c>, whose star entry carries an [[ImportName]] of
    /// <c>all-but-default</c>), which is why a barrel file re-exporting several modules does not
    /// end up with their defaults fighting over one name.
    /// </para>
    /// <para>
    /// The keys are the source's OWN enumerable keys: a namespace object's inherited properties
    /// are not its exports, and a non-enumerable one is not an export either. Unlike a static
    /// <c>export { a } from</c>, the set is not known at compile time — it is whatever the source
    /// module turned out to export — which is the whole reason this runs at run time rather than
    /// being emitted specifier by specifier.
    /// </para>
    /// <para>
    /// A name the module already exports is left alone. A local export (<c>export const x</c>) or a
    /// named re-export (<c>export { x }</c>, <c>export { x } from</c>) writes the name straight onto
    /// <paramref name="target"/>, and ResolveExport (ES2024 16.2.1.5.3) consults a module's own local
    /// and indirect export entries before its star entries — so a name that resolves locally is never
    /// taken from an <c>export *</c>; the explicit export shadows the star. Skipping a key the target
    /// already owns implements that precedence independent of source order, and it also stops the copy
    /// from assigning over a <c>const</c> export's read-only property, which raised "Cannot assign to
    /// read only variable" whenever the star followed the local export in the text.
    /// </para>
    /// </remarks>
    public static JSValue CopyStarExports(JSValue source, JSValue target)
    {
        if (source is not JSObject || target is not JSObject targetObject)
            return JSUndefined.Value;

        var keys = source.GetAllKeys(showEnumerableOnly: true, inherited: false);
        while (keys.MoveNext(out var key))
        {
            if (key.ToString() == "default")
                continue;

            if (targetObject.HasOwnProperty(key.ToKey(false)))
                continue;

            target[key] = source[key];
        }

        return JSUndefined.Value;
    }
}
