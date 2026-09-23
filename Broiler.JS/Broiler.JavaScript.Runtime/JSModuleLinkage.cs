using System;

namespace Broiler.JavaScript.Runtime;

/// <summary>
/// The environment of one ECMAScript module, as its compiled body sees it.
/// </summary>
/// <remarks>
/// A module body is compiled once and instantiated by the host's module linker. The compiled
/// prologue asks this environment for each import binding the linker created, publishes each
/// local binding another module may import, and reads <c>import.meta</c> from it. The linker
/// (Broiler.JavaScript.Modules) implements it; the compiler only reaches it through
/// <see cref="JSModuleLinkage"/>.
/// </remarks>
public interface IJSModuleEnvironment
{
    /// <summary>The binding the linker created for the import bound to <paramref name="localName"/>:
    /// an indirect binding to the exporter's binding, or an immutable binding holding a namespace
    /// object.</summary>
    JSVariable GetImportBinding(string localName);

    /// <summary>Records the module's own binding <paramref name="localName"/>, so that an importer's
    /// indirect binding and the namespace object read it live.</summary>
    void PublishBinding(string localName, JSVariable binding);

    /// <summary>The module's <c>import.meta</c> object.</summary>
    JSValue ImportMeta { get; }
}

/// <summary>
/// The calls a compiled module body makes into its <see cref="IJSModuleEnvironment"/>.
/// </summary>
internal static class JSModuleLinkage
{
    private static IJSModuleEnvironment Environment(JSValue environment)
        => environment as IJSModuleEnvironment
            ?? throw new InvalidOperationException("The module body was instantiated without a module environment.");

    public static JSVariable Import(JSValue environment, string localName)
        => Environment(environment).GetImportBinding(localName);

    public static JSValue Publish(JSValue environment, string localName, JSVariable binding)
    {
        Environment(environment).PublishBinding(localName, binding);
        return JSUndefined.Value;
    }

    public static JSValue Meta(JSValue environment)
        => Environment(environment).ImportMeta;
}

/// <summary>
/// An import binding: the immutable, indirect binding ECMAScript creates for an imported name
/// (CreateImportBinding). It holds no value of its own. Every read goes to the exporting module's
/// binding, so the importer sees each later assignment the exporter makes and that binding's
/// temporal dead zone; every write throws a TypeError, as assigning an immutable binding in strict
/// code does.
/// </summary>
/// <remarks>
/// The binding is never initialized, so <see cref="JSVariable.Value"/> routes every read through
/// <see cref="ReadUnavailable"/>. The target is resolved on first read rather than when the binding
/// is created, because in a cycle the importer's environment is created before the exporter's.
/// No module body runs until every module of the graph has been instantiated, so the target always
/// exists by the time anything can read it.
/// </remarks>
public sealed class JSModuleImportBinding : JSVariable
{
    private readonly Func<JSVariable> resolveTarget;
    private JSVariable target;

    public JSModuleImportBinding(string localName, Func<JSVariable> resolveTarget)
        : base(JSUndefined.Value, localName, initialized: false)
    {
        this.resolveTarget = resolveTarget ?? throw new ArgumentNullException(nameof(resolveTarget));
        IsReadOnly = true;
        ThrowOnReadOnlyWrite = true;
    }

    private JSVariable Target => target ??= resolveTarget()
        ?? throw (NewReferenceErrorFactory ?? throw new InvalidOperationException("JSVariable.NewReferenceErrorFactory delegate is not initialized."))
            ($"Cannot access '{Name.Value}' before initialization");

    internal override JSValue ReadUnavailable() => Target.Value;

    // CreateImportBinding records the binding as initialized, so an assignment is never a
    // ReferenceError: it is SetMutableBinding on an immutable binding in strict code.
    internal override JSValue AssignUnavailable(JSValue value)
    {
        Value = value;
        return value;
    }

    public override JSValue GetValue() => Target.Value;

    public override JSValue SetValue(JSValue value)
    {
        Value = value;
        return value;
    }

    public override JSValue GetCaptured(bool reference) => GetValue();

    public override JSValue SetCaptured(bool reference, JSValue value) => SetValue(value);
}
