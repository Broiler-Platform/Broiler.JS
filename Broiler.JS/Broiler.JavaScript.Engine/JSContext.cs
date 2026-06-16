using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Broiler.JavaScript.Storage;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.Engine;

public delegate void ConsoleEvent(JSContext context, string type, in Arguments a);
public delegate void LogEventHandler(JSContext context, JSValue value);
public delegate void ErrorEventHandler(JSContext context, Exception error);

public class EvalEventArgs : EventArgs
{
    public JSContext Context { get; set; }
    public string Script { get; set; }
    public string Location { get; set; }
}

public class JSContext : JSObject, IJSExecutionContext, IDisposable
{
    private static long contextId = 1;
    private static readonly KeyString UnscopablesKey = KeyStrings.GetOrCreate("unscopables");

    public long ID { get; set; } = Interlocked.Increment(ref contextId);

    /// <summary>
    /// Gets or sets the debugger attached to this context.
    /// </summary>
    public IDebugger Debugger;

    /// <summary>
    /// Available only when Enable Clr Integration is true in JSModuleContext
    /// </summary>
    public ClrMemberNamingConvention ClrMemberNamingConvention { get; set; } = ClrMemberNamingConvention.CamelCase;

    private TaskCompletionSource<int> _waitTask;
    public Task WaitTask => _waitTask?.Task;

    public CallStackItem Top { get; set; }

    public JSValue CurrentNewTarget { get; set; }

    public event EventHandler<EvalEventArgs> EvalEvent;

    public void DispatchEvalEvent(ref string script, ref string location)
    {
        var ee = EvalEvent;
        if (ee == null)
            return;

        var e = new EvalEventArgs { Context = this, Script = script, Location = location };
        EvalEvent.Invoke(this, e);
        script = e.Script;
        location = e.Location;
    }

    public void Dispose() => JSEngine.ClearAsyncLocal();

    public JSObject FunctionPrototype { get; private set; }
    public new JSObject ObjectPrototype { get; private set; }

    // The per-realm %AsyncFunction.prototype% intrinsic. All async functions
    // (declarations, expressions and arrows) share this single object as their
    // [[Prototype]], so it is created lazily once and cached here rather than
    // rebuilt per function. Populated by JSAsyncFunction via the interface.
    public JSObject AsyncFunctionPrototype { get; set; }

    // The per-realm %ThrowTypeError% intrinsic. Created lazily once and shared by
    // every unmapped arguments object's "callee" poison accessor so they compare
    // equal under SameValue. Populated by JSFunction via the interface.
    public JSObject ThrowTypeError { get; set; }
    public JSValue Object { get; private set; }
    public JSValue IntrinsicEval { get; private set; } = JSUndefined.Value;
    // %Array.prototype.values% captured at realm init — the mapped/unmapped arguments
    // object's @@iterator must be this exact intrinsic (=== [][Symbol.iterator]).
    public JSValue IntrinsicArrayValues { get; private set; } = JSUndefined.Value;
    public event LogEventHandler Log;
    public event ErrorEventHandler Error;
    public event ConsoleEvent ConsoleEvent;
    public JavaScriptFeatureFlags ExperimentalFeatures { get; }

    SAUint32Map<JSVariable> globalVars = new();
    private int directEvalDepth;
    private int directEvalCompilationDepth;
    private int directEvalLocalVarEnvironmentDepth;
    private readonly List<string[]> directEvalPrivateNameScopes = [];
    private readonly List<string[]> directEvalBindingNameScopes = [];
    private readonly List<string[]> directEvalLexicalBindingNameScopes = [];
    private readonly List<DirectEvalScope> activeDirectEvalScopes = [];
    private readonly List<CallStackItem> directEvalActivationOwners = [];
    private readonly List<JSValue> directEvalSuperValues = [];
    private WithScope withScope;

    private sealed class WithScope : IDisposable
    {
        private readonly JSContext context;

        public WithScope(JSContext context, JSObject @object)
        {
            this.context = context;
            Previous = context.withScope;
            Object = @object;
            context.withScope = this;
        }

        public JSObject Object { get; }
        public WithScope Previous { get; }

        public void Dispose() => context.withScope = Previous;
    }

    private sealed class SuspendedWithScope : IDisposable
    {
        private readonly JSContext context;
        private readonly WithScope previous;

        public SuspendedWithScope(JSContext context)
        {
            this.context = context;
            previous = context.withScope;
            context.withScope = null;
        }

        public void Dispose() => context.withScope = previous;
    }

    private sealed class DirectEvalScope : IDisposable
    {
        private readonly JSContext context;
        private readonly List<Entry> entries = [];

        // True for the lexical-environment fallback overlay of a `with` statement
        // (PushWithFallbackScope). Every binding it captures is a declarative /
        // global outer binding (let/const/var/function/captured local) — none are
        // deletable, so `delete` of a name held here returns false.
        public readonly bool IsWithFallback;

        private sealed class Entry
        {
            public KeyString Name;
            public JSVariable OverlayVariable;
            public JSVariable PreviousVariable;
            public bool HadPreviousVariable;
            public bool HadOwnProperty;
            public JSValue PreviousValue;

            // True for a `with`-fallback overlay of a function-owned binding that
            // merely shadows a same-named outer binding. Such overlays must never
            // publish to, propagate back to, or otherwise mutate the shadowed
            // binding or its global-object property (writes stay on the inner
            // variable). A program-level global `var` is never shadowed: it is
            // kept in sync with its property by the normal dual-binding path.
            public bool Shadowed;
        }

        public DirectEvalScope(JSContext context, JSVariable[] variables, JSVariable[] shadowedVariables = null, bool isWithFallback = false)
        {
            this.context = context;
            IsWithFallback = isWithFallback;
            HashSet<JSVariable> shadowed = shadowedVariables is { Length: > 0 }
                ? [.. shadowedVariables]
                : null;
            if (variables == null || variables.Length == 0)
            {
                context.directEvalDepth++;
                return;
            }

            // Enter eval depth BEFORE publishing captured bindings below. Register must
            // observe directEvalDepth > 0 so an overlaid outer-scope binding that has no
            // global property yet is published as a CONFIGURABLE one (a transient eval
            // overlay) — Dispose can then delete it. Otherwise a function-local `var`
            // captured for the eval would be published as a permanent, non-configurable
            // global property and leak after the surrounding function returns.
            context.directEvalDepth++;

            var seen = new HashSet<uint>();
            foreach (var variable in variables)
            {
                if (variable == null)
                    continue;

                var name = variable.Name;
                if (name.IsEmpty)
                    continue;

                var key = KeyStrings.GetOrCreate(name);
                if (!seen.Add(key.Key))
                    continue;

                var entry = new Entry
                {
                    Name = key,
                    OverlayVariable = variable,
                    HadPreviousVariable = context.globalVars.TryGetValue(key.Key, out var previousVariable),
                    PreviousVariable = previousVariable
                };

                var property = context.GetInternalProperty(key, false);
                entry.HadOwnProperty = !property.IsEmpty;
                if (entry.HadOwnProperty)
                    entry.PreviousValue = context.GetOwnPropertyValue(key);

                entry.Shadowed = shadowed != null && shadowed.Contains(variable);

                entries.Add(entry);
                // A captured binding can still be in its temporal dead zone when the
                // eval runs — e.g. a parameter whose own default initializer contains
                // the direct eval (`function f(_ = (eval('var x=1'), ...)) {}`).
                // Register publishes the binding's value as a global property, which
                // reads JSVariable.Value and would throw for such a binding. Skip that
                // value materialization; the binding stays in globalVars so it remains
                // resolvable and a genuine read still throws the proper ReferenceError.
                //
                // A shadowing overlay never publishes to the global object: the
                // shadowed binding's property must keep its own value while the
                // `with` body runs (e.g. `globalThis.v` stays the outer value even
                // though the unscopables-blocked `v` resolves to the inner var).
                // Do NOT republish when the name already has a live global-object
                // property: that property is the variable's true store and already holds
                // its current value. The captured binding's `Value` field can be a stale
                // snapshot (a global var written through its property does not update the
                // binding field), so Register would overwrite the live property with a
                // stale value. The eval resolves the name through the existing property
                // regardless. Transient function-local bindings (no prior property) are
                // still published so the eval can see them.
                if (variable.IsInitialized && !entry.Shadowed && !entry.HadOwnProperty)
                    context.Register(variable);
                context.globalVars.Put(key.Key) = variable;
            }

            context.activeDirectEvalScopes.Add(this);
            context.directEvalBindingNameScopes.Add(ExtractBindingNames(variables));
        }

        private static string[] ExtractBindingNames(JSVariable[] bindings)
        {
            if (bindings == null || bindings.Length == 0)
                return [];

            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var binding in bindings)
            {
                if (binding == null || binding.Name.IsEmpty)
                    continue;

                names.Add(binding.Name.Value);
            }

            return [.. names];
        }

        /// <summary>
        /// Temporarily removes overlay bindings from the global object so
        /// that indirect eval can see the true global environment.
        /// </summary>
        public void Suspend()
        {
            foreach (var entry in entries)
            {
                if (entry.HadPreviousVariable)
                {
                    context.globalVars.Put(entry.Name.Key) = entry.PreviousVariable;
                    if (entry.HadOwnProperty)
                        context.SetOwnPropertyValue(entry.Name, entry.PreviousValue);
                    else
                        context.Delete(entry.Name);
                }
                else
                {
                    context.globalVars.RemoveAt(entry.Name.Key);
                    if (entry.HadOwnProperty)
                        context.SetOwnPropertyValue(entry.Name, entry.PreviousValue);
                    else
                        context.Delete(entry.Name);
                }
            }
        }

        /// <summary>
        /// Re-applies overlay bindings after indirect eval completes.
        /// </summary>
        public void Resume()
        {
            foreach (var entry in entries)
            {
                // Re-read the current global value (indirect eval may have
                // modified the real global binding).
                var property = context.GetInternalProperty(entry.Name, false);
                entry.HadOwnProperty = !property.IsEmpty;
                if (entry.HadOwnProperty)
                    entry.PreviousValue = context.GetOwnPropertyValue(entry.Name);
                entry.HadPreviousVariable = context.globalVars.TryGetValue(entry.Name.Key, out var pv);
                entry.PreviousVariable = pv;

                // A shadowing overlay only re-establishes the globalVars binding;
                // it never republishes to the global-object property.
                if (entry.OverlayVariable.IsInitialized && !entry.Shadowed)
                    context.Register(entry.OverlayVariable);
                context.globalVars.Put(entry.Name.Key) = entry.OverlayVariable;
            }
        }

        /// <summary>
        /// If this scope overlays <paramref name="name"/>, reports whether that
        /// overlay is a shadowing (lexical-fallback) one. The return value says
        /// whether the name is overlaid at all.
        /// </summary>
        public bool ContainsName(in KeyString name)
        {
            foreach (var entry in entries)
            {
                if (entry.Name.Key == name.Key)
                    return true;
            }

            return false;
        }

        public bool TryGetOverlayShadowing(in KeyString name, out bool isShadowing)
        {
            foreach (var entry in entries)
            {
                if (entry.Name.Key == name.Key)
                {
                    isShadowing = entry.Shadowed;
                    return true;
                }
            }

            isShadowing = false;
            return false;
        }

        public void Dispose()
        {
            context.activeDirectEvalScopes.Remove(this);

            foreach (var entry in entries)
            {
                if (entry.HadPreviousVariable)
                {
                    // A shadowing overlay leaves the shadowed binding (and its
                    // property) exactly as it was: writes inside the `with` body
                    // went only to the inner variable and must not leak outward.
                    if (entry.Shadowed)
                    {
                        context.globalVars.Put(entry.Name.Key) = entry.PreviousVariable;
                        continue;
                    }

                    context.globalVars.Put(entry.Name.Key) = entry.PreviousVariable;

                    if (entry.HadOwnProperty)
                    {
                        // The name already had a live global-object property — its true
                        // store, which the eval read and wrote DIRECTLY. Leave it exactly
                        // as the eval left it. The captured binding's `Value` field can be
                        // a stale snapshot (a global var's binding field is not kept in
                        // sync when writes flow through its property), so writing it back
                        // here would corrupt the live value.
                        continue;
                    }

                    if (!ReferenceEquals(entry.PreviousVariable, entry.OverlayVariable))
                    {
                        // A transient overlay (an eval-introduced binding with no prior
                        // global property): propagate its value to the outer binding so
                        // later closures observe it, then drop the transient property.
                        if (entry.OverlayVariable.IsInitialized)
                            entry.PreviousVariable.Value = entry.OverlayVariable.Value;
                        context.Delete(entry.Name);
                    }

                    continue;
                }

                context.globalVars.RemoveAt(entry.Name.Key);

                // A shadowing overlay never created or mutated the global-object
                // property, so leave it untouched on teardown.
                if (entry.Shadowed)
                    continue;

                if (entry.HadOwnProperty)
                    context.SetOwnPropertyValue(entry.Name, entry.PreviousValue);
                else
                    context.Delete(entry.Name);
            }

            if (context.directEvalBindingNameScopes.Count > 0)
                context.directEvalBindingNameScopes.RemoveAt(context.directEvalBindingNameScopes.Count - 1);
            context.directEvalDepth--;
        }
    }

    public IDisposable PushDirectEvalScope(JSVariable[] variables) => new DirectEvalScope(this, variables);

    /// <summary>
    /// Pushes the lexical-environment fallback overlay for a `with` statement.
    /// <paramref name="variables"/> are all the in-scope bindings made resolvable
    /// inside the body; <paramref name="shadowedVariables"/> is the function-owned
    /// subset whose writes must stay local (never leaking to a same-named global
    /// or its property). Program-level globals are resolvable through the normal
    /// path and are not shadow-isolated.
    /// </summary>
    public IDisposable PushWithFallbackScope(JSVariable[] variables, JSVariable[] shadowedVariables)
        => new DirectEvalScope(this, variables, shadowedVariables, isWithFallback: true);

    /// <summary>
    /// Whether <paramref name="name"/> currently resolves through an active
    /// `with`-fallback overlay (the captured outer lexical environment). Such a
    /// binding is a declarative/global binding that `delete` must not remove.
    /// </summary>
    private bool IsWithFallbackOverlayBinding(in KeyString name)
    {
        for (var i = activeDirectEvalScopes.Count - 1; i >= 0; i--)
        {
            var scope = activeDirectEvalScopes[i];
            if (scope.ContainsName(name))
                return scope.IsWithFallback;
        }

        return false;
    }

    private sealed class DirectEvalActivationScope(JSContext context, CallStackItem owner) : IDisposable
    {
        public void Dispose()
        {
            if (owner == null)
                return;

            for (var i = context.directEvalActivationOwners.Count - 1; i >= 0; i--)
            {
                if (!ReferenceEquals(context.directEvalActivationOwners[i], owner))
                    continue;

                context.directEvalActivationOwners.RemoveAt(i);
                break;
            }
        }
    }

    internal IDisposable PushDirectEvalActivation(CallStackItem owner)
    {
        if (owner == null)
            return null;

        directEvalActivationOwners.Add(owner);
        return new DirectEvalActivationScope(this, owner);
    }

    private bool TryGetCurrentDirectEvalActivationOwner(out CallStackItem owner)
    {
        if (directEvalActivationOwners.Count > 0)
        {
            owner = directEvalActivationOwners[^1];
            return true;
        }

        owner = null;
        return false;
    }

    private sealed class DirectEvalSuperScope(JSContext context) : IDisposable
    {
        public void Dispose() => context.directEvalSuperValues.RemoveAt(context.directEvalSuperValues.Count - 1);
    }

    /// <summary>
    /// Makes the lexical [[HomeObject]] super reference of the enclosing
    /// method/initializer available to a direct eval body so that
    /// <c>super.x</c> inside the eval resolves correctly.
    /// </summary>
    public IDisposable PushDirectEvalSuper(JSValue superValue)
    {
        if (superValue == null)
            return null;

        directEvalSuperValues.Add(superValue);
        return new DirectEvalSuperScope(this);
    }

    /// <summary>The super reference visible to the direct eval currently being compiled/executed, or undefined.</summary>
    public JSValue DirectEvalSuper => directEvalSuperValues.Count == 0 ? JSUndefined.Value : directEvalSuperValues[^1];

    /// <summary>Whether a super reference is available to the direct eval currently being compiled.</summary>
    public bool HasDirectEvalSuper => directEvalSuperValues.Count > 0;

    private readonly List<JSValue> directEvalSuperConstructorValues = [];
    private readonly List<JSVariable> directEvalThisBindingValues = [];

    private sealed class DirectEvalSuperCallScope(JSContext context) : IDisposable
    {
        public void Dispose()
        {
            context.directEvalSuperConstructorValues.RemoveAt(context.directEvalSuperConstructorValues.Count - 1);
            context.directEvalThisBindingValues.RemoveAt(context.directEvalThisBindingValues.Count - 1);
        }
    }

    /// <summary>
    /// Shares the enclosing derived constructor's superclass constructor and its
    /// <c>this</c> binding with a direct eval body, so a <c>super(...)</c> inside the
    /// eval runs the superclass [[Construct]] and initializes the SAME <c>this</c>
    /// binding the constructor observes after the eval returns. Sharing the binding
    /// (not its value) also lets the eval read <c>this</c> lazily, so a derived
    /// constructor's pre-<c>super()</c> eval no longer throws when it merely contains
    /// the <c>super()</c> call rather than reading <c>this</c>.
    /// </summary>
    public IDisposable PushDirectEvalSuperCall(JSValue superConstructor, JSVariable thisBinding)
    {
        if (thisBinding == null)
            return null;

        directEvalSuperConstructorValues.Add(superConstructor ?? JSUndefined.Value);
        directEvalThisBindingValues.Add(thisBinding);
        return new DirectEvalSuperCallScope(this);
    }

    /// <summary>The superclass constructor a <c>super(...)</c> in the direct eval being compiled/executed targets.</summary>
    public JSValue DirectEvalSuperConstructor => directEvalSuperConstructorValues.Count == 0 ? JSUndefined.Value : directEvalSuperConstructorValues[^1];

    /// <summary>The derived constructor <c>this</c> binding shared with the direct eval being compiled/executed, or null.</summary>
    public JSVariable DirectEvalThisBinding => directEvalThisBindingValues.Count == 0 ? null : directEvalThisBindingValues[^1];

    /// <summary>Whether a <c>super(...)</c> call is available to the direct eval currently being compiled.</summary>
    public bool HasDirectEvalSuperCall => directEvalThisBindingValues.Count > 0;

    internal bool TryResolveDirectEvalBinding(in KeyString name, out JSVariable variable, bool includeUninitializedShadows = false)
    {
        for (var current = Top; current != null; current = current.Parent)
        {
            if (current.TryGetDirectEvalBinding(name, out variable))
            {
                // An uninitialized EvalShadowVariable means a sloppy parameter-eval
                // shadow whose name the eval has not (yet) introduced; it forwards to
                // the outer binding, so resolution must look through it to the real
                // binding. The eval's own var-declaration path (Register) passes
                // includeUninitializedShadows so it can find and initialize the shadow.
                if (!includeUninitializedShadows && variable is EvalShadowVariable { IsInitialized: false })
                    continue;

                return true;
            }
        }

        variable = null;
        return false;
    }

    /// <summary>
    /// Whether the binding currently overlaying <paramref name="name"/> in
    /// <see cref="globalVars"/> is a shadowing (lexical-fallback) overlay
    /// established by a `with` statement. Such overlays must be written through
    /// the overlay variable alone, never the shadowed global-object property.
    /// </summary>
    private bool IsShadowingOverlay(in KeyString name)
    {
        for (var i = activeDirectEvalScopes.Count - 1; i >= 0; i--)
        {
            if (activeDirectEvalScopes[i].TryGetOverlayShadowing(name, out var isShadowing))
                return isShadowing;
        }

        return false;
    }

    /// <summary>
    /// Temporarily suspends all active direct-eval scope overlays AND resets
    /// the direct-eval compilation/depth counters.  The returned IDisposable
    /// re-applies everything when disposed.  Used by indirect eval so that
    /// the evaluated code sees the true global environment (§19.2.1.1).
    /// </summary>
    public IDisposable SuspendDirectEvalOverlays()
    {
        if (activeDirectEvalScopes.Count == 0
            && directEvalCompilationDepth == 0
            && directEvalDepth == 0)
            return null;

        return new DirectEvalSuspension(this);
    }

    private sealed class DirectEvalSuspension : IDisposable
    {
        private readonly JSContext context;
        private readonly int savedCompilationDepth;
        private readonly int savedLocalVarDepth;
        private readonly int savedEvalDepth;
        private readonly string[][] savedPrivateNameScopes;
        private readonly string[][] savedBindingNameScopes;
        private readonly DirectEvalScope[] suspendedScopes;

        public DirectEvalSuspension(JSContext context)
        {
            this.context = context;

            // Save and reset compilation flags
            savedCompilationDepth = context.directEvalCompilationDepth;
            savedLocalVarDepth = context.directEvalLocalVarEnvironmentDepth;
            savedEvalDepth = context.directEvalDepth;
            savedPrivateNameScopes = context.directEvalPrivateNameScopes.ToArray();
            savedBindingNameScopes = context.directEvalBindingNameScopes.ToArray();
            context.directEvalCompilationDepth = 0;
            context.directEvalLocalVarEnvironmentDepth = 0;
            context.directEvalDepth = 0;
            context.directEvalPrivateNameScopes.Clear();
            context.directEvalBindingNameScopes.Clear();

            // Suspend all overlay scopes (innermost first)
            suspendedScopes = context.activeDirectEvalScopes.ToArray();
            for (int i = suspendedScopes.Length - 1; i >= 0; i--)
                suspendedScopes[i].Suspend();
        }

        public void Dispose()
        {
            // Resume overlay scopes (outermost first)
            for (int i = 0; i < suspendedScopes.Length; i++)
                suspendedScopes[i].Resume();

            // Restore compilation flags
            context.directEvalCompilationDepth = savedCompilationDepth;
            context.directEvalLocalVarEnvironmentDepth = savedLocalVarDepth;
            context.directEvalDepth = savedEvalDepth;
            context.directEvalPrivateNameScopes.Clear();
            context.directEvalPrivateNameScopes.AddRange(savedPrivateNameScopes);
            context.directEvalBindingNameScopes.Clear();
            context.directEvalBindingNameScopes.AddRange(savedBindingNameScopes);
        }
    }

    private sealed class DirectEvalCompilationScope(JSContext context, bool usesLocalVarEnvironment) : IDisposable
    {
        public void Dispose()
        {
            context.directEvalCompilationDepth--;
            if (context.directEvalPrivateNameScopes.Count > 0)
                context.directEvalPrivateNameScopes.RemoveAt(context.directEvalPrivateNameScopes.Count - 1);
            if (usesLocalVarEnvironment)
                context.directEvalLocalVarEnvironmentDepth--;
        }
    }

    public IDisposable PushDirectEvalCompilation(bool usesLocalVarEnvironment = false, string[] privateNamesInScope = null)
    {
        directEvalCompilationDepth++;
        directEvalPrivateNameScopes.Add(privateNamesInScope);
        if (usesLocalVarEnvironment)
            directEvalLocalVarEnvironmentDepth++;

        return new DirectEvalCompilationScope(this, usesLocalVarEnvironment);
    }

    public bool IsCompilingDirectEval => directEvalCompilationDepth > 0;
    public bool UsesDirectEvalLocalVarEnvironment => directEvalLocalVarEnvironmentDepth > 0;
    public string[] DirectEvalPrivateNamesInScope => directEvalPrivateNameScopes.Count == 0 ? null : directEvalPrivateNameScopes[^1];
    public string[] DirectEvalBindingNamesInScope => directEvalBindingNameScopes.Count == 0 ? null : directEvalBindingNameScopes[^1];
    public string[] DirectEvalLexicalBindingNamesInScope => directEvalLexicalBindingNameScopes.Count == 0 ? null : directEvalLexicalBindingNameScopes[^1];

    private sealed class DirectEvalLexicalBindingScope(JSContext context, string[] names) : IDisposable
    {
        public void Dispose()
        {
            if (context.directEvalLexicalBindingNameScopes.Count > 0)
                context.directEvalLexicalBindingNameScopes.RemoveAt(context.directEvalLexicalBindingNameScopes.Count - 1);
        }
    }

    public IDisposable PushDirectEvalLexicalBindingNames(string[] names)
    {
        directEvalLexicalBindingNameScopes.Add(names);
        return new DirectEvalLexicalBindingScope(this, names);
    }

    public JSValue Register(JSVariable variable)
    {
        KeyString name = variable.Name;
        // Skip an uninitialized parameter-eval shadow: a captured-binding publish must
        // not initialize it. The eval's own var declaration reuses the shadow as its
        // local storage via GetOrCreateDirectEvalLocalBinding instead.
        if (TryResolveDirectEvalBinding(name, out var directEvalBinding))
        {
            directEvalBinding.Value = variable.Value;
            return JSUndefined.Value;
        }
        if (directEvalLocalVarEnvironmentDepth > 0
            && TryGetCurrentDirectEvalActivationOwner(out var activationOwner))
        {
            activationOwner.RegisterDirectEvalBinding(variable);
            return JSUndefined.Value;
        }

        var v = variable.Value;

        var hasOwnProperty = !GetInternalProperty(name, false).IsEmpty;
        var hadExistingVariable = globalVars.TryGetValue(name.Key, out var existingVariable);

        if (!hasOwnProperty)
        {
            if (!IsExtensible())
                throw JSEngine.NewTypeError($"Cannot define global variable {name} on a non-extensible global object");

            FastAddValue(
                name,
                v,
                (directEvalDepth > 0 || directEvalCompilationDepth > 0) && !hadExistingVariable
                    ? JSPropertyAttributes.EnumerableConfigurableValue
                    : JSPropertyAttributes.Value | JSPropertyAttributes.Enumerable);

            if (hadExistingVariable && !ReferenceEquals(existingVariable, variable))
                existingVariable.Value = v;
        }
        else
        {
            this[name] = v;
        }

        if ((directEvalDepth <= 0 || hadExistingVariable)
            && (!hadExistingVariable || ReferenceEquals(existingVariable, variable)))
            globalVars.Put(name.Key) = variable;
        return v;
    }

    public JSValue DeclareGlobalFunction(in KeyString name, JSValue value)
    {
        var property = GetInternalProperty(name, false);
        if (property.IsEmpty)
        {
            if (!IsExtensible())
                throw JSEngine.NewTypeError($"Cannot define global function {name}");

            FastAddValue(name, value, JSPropertyAttributes.EnumerableConfigurableValue);
            SyncGlobalVariable(name, value);
            return value;
        }

        if (property.IsConfigurable)
        {
            FastAddValue(name, value, JSPropertyAttributes.EnumerableConfigurableValue);
            SyncGlobalVariable(name, value);
            return value;
        }

        if (!property.IsProperty && !property.IsReadOnly && property.IsEnumerable)
        {
            this[name] = value;
            return value;
        }

        throw JSEngine.NewTypeError($"Cannot define global function {name}");
    }

    // Annex B block-level function hoisting at global scope (B.3.3.3) uses
    // CreateGlobalVarBinding followed by SetMutableBinding, NOT
    // CreateGlobalFunctionBinding. The difference matters when a property of this
    // name already exists on the global object: CreateGlobalVarBinding does not
    // redefine an existing property, so its descriptor (e.g. a non-enumerable
    // binding) must be preserved and only its value updated. A fresh enumerable,
    // configurable var binding is created only when no such property exists yet.
    public JSValue DeclareGlobalAnnexBFunction(in KeyString name, JSValue value)
    {
        var property = GetInternalProperty(name, false);
        if (property.IsEmpty)
        {
            if (!IsExtensible())
                throw JSEngine.NewTypeError($"Cannot define global function {name}");

            FastAddValue(name, value, JSPropertyAttributes.EnumerableConfigurableValue);
            SyncGlobalVariable(name, value);
            return value;
        }

        // Existing property: SetMutableBinding semantics (non-strict Set) updates
        // the value while leaving enumerable/configurable/writable untouched.
        this[name] = value;
        return value;
    }

    // Keep any registered global variable slot in sync with the global-object
    // property so identifier resolution (which consults globalVars first) does
    // not observe a stale binding when a global function declaration replaces an
    // earlier hoisted instance. Mirrors the indexer setter and
    // SetOwnPropertyValue behaviour.
    private void SyncGlobalVariable(in KeyString name, JSValue value)
    {
        if (globalVars.TryGetValue(name.Key, out var jsv))
            jsv.Value = value;
    }

    internal JSVariable RegisterDirectEvalVariable(JSVariable variable)
    {
        Register(variable);
        return variable;
    }

    /// <summary>
    /// The storage binding a direct eval should use for a <c>var</c> it declares
    /// while running inside a function's local var environment. When a
    /// parameter-environment shadow (or any other activation binding) for the name
    /// already exists, the eval reuses it so that assignments inside the eval body
    /// reach the same binding the surrounding closures capture; otherwise a fresh
    /// binding is created and registered into the activation. <paramref name="fallback"/>
    /// is the value to seed a freshly created binding with.
    /// </summary>
    public JSVariable GetOrCreateDirectEvalLocalBinding(in KeyString name, JSValue fallback)
    {
        if (TryResolveDirectEvalBinding(name, out var existing, includeUninitializedShadows: true))
            return existing;

        var variable = new JSVariable(fallback, name.Value);
        if (directEvalLocalVarEnvironmentDepth > 0 && TryGetCurrentDirectEvalActivationOwner(out var owner))
            owner.RegisterDirectEvalBinding(variable);

        return variable;
    }

    public override JSValue this[KeyString name]
    {
        get
        {
            if (TryResolveDirectEvalBinding(name, out var directEvalBinding))
                return directEvalBinding.GetValue();

            return base[name];
        }
        set
        {
            if (TryResolveDirectEvalBinding(name, out var directEvalBinding))
            {
                directEvalBinding.SetValue(value);
                return;
            }

            base[name] = value;
            if (globalVars.TryGetValue(name.Key, out var jsv))
                jsv.Value = value;
        }
    }

    internal JSValue GetOwnPropertyValue(in KeyString name) => base[name];

    internal void SetOwnPropertyValue(in KeyString name, JSValue value)
    {
        base[name] = value;
        if (globalVars.TryGetValue(name.Key, out var jsv))
            jsv.Value = value;
    }

    public IDisposable PushWithScope(JSValue value)
    {
        value.RequireObjectCoercible();
        var @object = value as JSObject
            ?? JSObject.CreatePrimitiveObject(value) as JSObject
            ?? throw new InvalidOperationException("CreatePrimitiveObject returned a non-object value.");
        return new WithScope(this, @object);
    }

    public IDisposable SuspendWithScopes() => withScope == null ? null : new SuspendedWithScope(this);

    public JSObject[] CaptureWithScopes()
    {
        if (withScope == null)
            return [];

        var scopes = new List<JSObject>();
        for (var current = withScope; current != null; current = current.Previous)
            scopes.Add(current.Object);

        scopes.Reverse();
        return [.. scopes];
    }

    public IDisposable PushWithScopes(JSObject[] values)
    {
        if (values == null || values.Length == 0)
            return null;

        return new CompositeWithScope(this, values);
    }

    private sealed class CompositeWithScope : IDisposable
    {
        private readonly IDisposable[] scopes;

        public CompositeWithScope(JSContext context, JSObject[] values)
        {
            scopes = new IDisposable[values.Length];
            for (var i = 0; i < values.Length; i++)
                scopes[i] = new WithScope(context, values[i]);
        }

        public void Dispose()
        {
            for (var i = scopes.Length - 1; i >= 0; i--)
                scopes[i]?.Dispose();
        }
    }

    private bool TryResolveWithObject(in KeyString name, out JSObject @object)
    {
        var propertyKey = name.ToJSValue();
        for (var current = withScope; current != null; current = current.Previous)
        {
            if (!current.Object.HasProperty(propertyKey).BooleanValue)
                continue;

            var unscopablesSymbol = this[KeyStrings.Symbol][UnscopablesKey];
            var unscopables = unscopablesSymbol.IsUndefined
                ? JSValue.UndefinedValue
                : current.Object[unscopablesSymbol];
            if (unscopables is JSObject unscopablesObject
                && unscopablesObject[propertyKey].BooleanValue)
            {
                continue;
            }

            // @@unscopables lookup can run user code that deletes the binding.
            // Re-check before resolving the object environment record.
            if (!current.Object.HasProperty(propertyKey).BooleanValue)
                continue;

            @object = current.Object;
            return true;
        }

        @object = null;
        return false;
    }

    public JSObject ResolveWithObject(in KeyString name)
    {
        return TryResolveWithObject(name, out var withObject) ? withObject : null;
    }

    public JSValue ResolveIdentifier(in KeyString name)
    {
        if (TryResolveWithObject(name, out var withObject))
        {
            if (!withObject.HasProperty(name.ToJSValue()).BooleanValue)
                throw JSEngine.NewReferenceError($"{name} is not defined");

            return withObject[name];
        }

        if (TryResolveDirectEvalBinding(name, out var directEvalBinding))
            return directEvalBinding.GetValue();

        if (globalVars.TryGetValue(name.Key, out var variable))
            return variable.Value;

        if (!GetInternalProperty(name).IsEmpty)
            return this[name];

        throw JSEngine.NewReferenceError($"{name} is not defined");
    }

    /// <summary>
    /// Resolves an identifier the way <see cref="ResolveIdentifier"/> does — with
    /// the same precedence (`with` object, direct-eval / `with`-fallback overlay,
    /// global var, global property) — but yields <c>undefined</c> instead of
    /// throwing when the name is unresolvable. Used by `typeof` so it consults a
    /// `with`-fallback overlay (e.g. a function-local that shadows a global via
    /// @@unscopables) rather than reading the bare global-object property.
    /// </summary>
    public JSValue ResolveIdentifierOrUndefined(in KeyString name)
    {
        if (TryResolveWithObject(name, out var withObject))
        {
            return withObject.HasProperty(name.ToJSValue()).BooleanValue
                ? withObject[name]
                : JSUndefined.Value;
        }

        if (TryResolveDirectEvalBinding(name, out var directEvalBinding))
            return directEvalBinding.GetValue();

        if (globalVars.TryGetValue(name.Key, out var variable))
            return variable.Value;

        if (!GetInternalProperty(name).IsEmpty)
            return this[name];

        return JSUndefined.Value;
    }

    public JSValue AssignIdentifier(in KeyString name, JSValue value) => AssignIdentifier(name, value, JSEngine.IsStrictMode);

    public JSValue AssignWithObjectIdentifier(JSObject withObject, in KeyString name, JSValue value, bool strictMode)
    {
        if (withObject == null)
            return AssignIdentifier(name, value, strictMode);

        if (!withObject.HasProperty(name.ToJSValue()).BooleanValue && strictMode)
            throw JSEngine.NewReferenceError($"{name} is not defined");

        withObject[name] = value;
        return value;
    }

    public JSValue AssignIdentifier(in KeyString name, JSValue value, bool strictMode)
    {
        if (TryResolveWithObject(name, out var withObject))
        {
            if (!withObject.HasProperty(name.ToJSValue()).BooleanValue && strictMode)
                throw JSEngine.NewReferenceError($"{name} is not defined");

            withObject[name] = value;
            return value;
        }

        var hasVariable = globalVars.TryGetValue(name.Key, out var variable);
        var hasProperty = !GetInternalProperty(name).IsEmpty;

        if (TryResolveDirectEvalBinding(name, out var directEvalBinding))
        {
            directEvalBinding.SetValue(value);
            return value;
        }

        // A `with` shadowing overlay must capture the write itself; the shadowed
        // global-object property of the same name belongs to an outer binding and
        // must stay untouched (matching the read path, which resolves to the
        // overlay before consulting the property).
        if (hasVariable && IsShadowingOverlay(name))
        {
            variable.Value = value;
            return value;
        }

        if (!hasVariable && !hasProperty)
        {
            if (strictMode)
                throw JSEngine.NewReferenceError($"{name} is not defined");

            FastAddValue(name, value, JSPropertyAttributes.EnumerableConfigurableValue);
            return value;
        }

        if (hasVariable)
            variable.Value = value;

        if (hasProperty)
            this[name] = value;

        return value;
    }

    public void EnsureCanDeclareGlobalFunction(in KeyString name)
    {
        var property = GetInternalProperty(name, false);
        if (property.IsEmpty)
        {
            if (!IsExtensible())
                throw JSEngine.NewTypeError($"Cannot define global function {name}");

            return;
        }

        if (property.IsConfigurable)
            return;

        if (!property.IsProperty && !property.IsReadOnly && property.IsEnumerable)
            return;

        throw JSEngine.NewTypeError($"Cannot define global function {name}");
    }

    public JSValue DeleteIdentifier(in KeyString name)
    {
        if (TryResolveWithObject(name, out var withObject))
            return withObject.Delete(name);

        // A name resolved through a `with`-fallback overlay (the captured outer
        // lexical environment made resolvable inside a `with` body, e.g. a top-level
        // `let`/`const` that the unscopables list blocked from the object) is a
        // declarative binding: `delete` of it returns false, never deleting. The
        // overlay transiently publishes such a binding as a configurable global
        // property so the body can read it, so without this guard the fall-through
        // below would delete that transient property and wrongly report success.
        if (IsWithFallbackOverlayBinding(name))
            return JSValue.BooleanFalse;

        if (TryResolveDirectEvalBinding(name, out var directEvalBinding))
        {
            for (var current = Top; current != null; current = current.Parent)
            {
                if (!current.TryGetDirectEvalBinding(name, out var existingBinding)
                    || !ReferenceEquals(existingBinding, directEvalBinding))
                {
                    continue;
                }

                current.DeleteDirectEvalBinding(name);
                return JSValue.BooleanTrue;
            }
        }

        var hasVariable = globalVars.TryGetValue(name.Key, out _);
        var property = GetInternalProperty(name, false);

        if (hasVariable)
        {
            if (property.IsEmpty)
                return JSValue.BooleanFalse;

            if (!property.IsConfigurable)
                return JSValue.BooleanFalse;

            var deleted = Delete(name);
            if (deleted.BooleanValue)
                globalVars.RemoveAt(name.Key);

            return deleted;
        }

        if (!property.IsEmpty)
            return property.IsConfigurable ? Delete(name) : JSValue.BooleanFalse;

        return JSValue.BooleanTrue;
    }

    internal void FillStackTrace(StringBuilder sb) { }

    public JSContext(
        SynchronizationContext synchronizationContext = null,
        JavaScriptFeatureFlags experimentalFeatures = JavaScriptFeatureFlags.None)
    {
        JSEngine.EnsureBuiltInsAssemblyLoaded();

        this.synchronizationContext = synchronizationContext ?? SynchronizationContext.Current;
        ExperimentalFeatures = experimentalFeatures;

        JSEngine.CurrentContext = this;

        ref var ownProperties = ref GetOwnProperties();

        KeyString functionKey = "Function";
        KeyString objectKey = "Object";

        var func = JSEngine.CreateFunctionClass(this, false);
        // Built-in global constructors are { writable, enumerable: false, configurable }.
        // The indexer would install them as enumerable (so they'd wrongly show up in a
        // global for-in / Object.keys), so add them with ConfigurableValue attributes.
        this.FastAddValue(functionKey, func, JSPropertyAttributes.ConfigurableValue);
        FunctionPrototype = ((IJSFunction)func).Prototype as JSObject;
        if (FunctionPrototype.GetInternalProperty(KeyStrings.length, false).IsEmpty)
            FunctionPrototype.FastAddValue(KeyStrings.length, JSValue.NumberZero, JSPropertyAttributes.ConfigurableReadonlyValue);
        Object = JSEngine.CreateObjectClass(this, false);
        this.FastAddValue(objectKey, Object, JSPropertyAttributes.ConfigurableValue);
        ObjectPrototype = ((IJSFunction)Object).Prototype as JSObject;
        ObjectPrototype.BasePrototypeObject = null;

        func.BasePrototypeObject = Object;
        FunctionPrototype.BasePrototypeObject = ObjectPrototype;
        ReattachFunctionPrototypeMethods();

        if (JSEngine.BuiltInRegistry != null)
        {
            JSEngine.BuiltInRegistry.Register(this);
        }
        else
        {
            JSEngine.CoreClassRegistrations?.Invoke(this);
        }

        IntrinsicEval = this[KeyStrings.eval];
        // Capture %Array.prototype.values% (=== Array.prototype[@@iterator]) so the
        // arguments object can expose the genuine intrinsic as its @@iterator.
        if (this[KeyStrings.GetOrCreate("Array")] is IJSFunction arrayCtor
            && arrayCtor.Prototype is JSObject arrayProto)
        {
            var values = arrayProto[KeyStrings.GetOrCreate("values")];
            if (values.IsFunction)
                IntrinsicArrayValues = values;
        }
        // globalThis is { writable, enumerable: false, configurable } per spec.
        this.FastAddValue(KeyStrings.globalThis, this, JSPropertyAttributes.ConfigurableValue);
        this[KeyStrings.debug] = JSValue.CreateFunction(Debug);
        InstallDynamicImport();

        // The global object inherits from Object.prototype (as in Node's
        // `globalThis`), so that `Object.getPrototypeOf(globalThis)` is
        // Object.prototype and inherited methods such as hasOwnProperty /
        // propertyIsEnumerable are callable on the top-level `this`.
        BasePrototypeObject = ObjectPrototype;
    }

    private void ReattachFunctionPrototypeMethods()
    {
        // Function.prototype's own methods (toString/call/apply/bind/valueOf) are
        // non-enumerable, so the enumerator must be told to include non-enumerable
        // properties — otherwise every method is skipped and keeps the null
        // [[Prototype]] it was constructed with before FunctionPrototype existed.
        var en = FunctionPrototype.GetOwnProperties(false).GetEnumerator(showEnumerableOnly: false);
        while (en.MoveNext(out var _, out var property))
        {
            if (property.IsValue && property.value is JSValue value && value.IsFunction)
                value.BasePrototypeObject = FunctionPrototype;
        }
    }

    public bool HasExperimentalFeature(JavaScriptFeatureFlags feature)
        => (ExperimentalFeatures & feature) == feature;

    internal void FireConsoleEvent(string type, in Arguments a) => ConsoleEvent?.Invoke(this, type, in a);

    private JSValue Debug(in Arguments a)
    {
        System.Diagnostics.Debug.WriteLine(a.Get1().ToString());
        return JSUndefined.Value;
    }

    internal readonly ConcurrentDictionary<long, Timer> timeouts = new();
    internal readonly ConcurrentDictionary<long, Timer> timers = new();

    internal void ClearTimeout(long n)
    {
        if (timeouts.TryRemove(n, out var timer))
        {
            try
            {
                timer.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Broiler.JavaScript] ClearTimeout dispose error: {ex.Message}");
            }
        }

        if (timers.IsEmpty && timeouts.IsEmpty)
            _waitTask.TrySetResult(1);
    }

    internal void ClearInterval(long n)
    {
        if (timers.TryRemove(n, out var timer))
        {
            try
            {
                timer.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Broiler.JavaScript] ClearInterval dispose error: {ex.Message}");
            }
        }

        if (timers.IsEmpty && timeouts.IsEmpty)
            _waitTask.TrySetResult(1);
    }

    static readonly ConcurrentUInt32Map<JSValue> cache = ConcurrentUInt32Map<JSValue>.Create();
    internal readonly SynchronizationContext synchronizationContext;

    private static long nextTimeout = 1;
    private static long nextInterval = 1;

    internal void ReportError(Exception ex)
    {
        Error?.Invoke(this, ex);
    }

    public void ReportLog(JSValue f) => Log?.Invoke(this, f);

    internal long PostTimeout(int delay, JSValue f, in Arguments a)
    {
        var ctx = synchronizationContext ?? throw JSEngine.NewTypeError($"Synchronization context must be present to set timeout");
        var key = Interlocked.Increment(ref nextTimeout);
        JSValue[] args = System.Array.Empty<JSValue>();

        if (a.Length > 2)
        {
            args = new JSValue[a.Length - 2];
            for (int i = 2; i < a.Length; i++)
                args[i - 2] = a.GetAt(i);
        }

        var timer = new Timer((_) =>
        {
            ctx.Post((x) =>
            {
                var f = x as JSValue;
                try
                {
                    f.InvokeFunction(new Arguments(JSUndefined.Value, args));
                }
                catch (Exception ex)
                {
                    ReportError(ex);
                }
                ClearTimeout(key);
            }, f);
        }, f, delay, Timeout.Infinite);

        timeouts.AddOrUpdate(key, timer, (a1, a2) => a2);
        lock (this)
        {
            _waitTask = _waitTask ?? new TaskCompletionSource<int>();
        }

        return key;
    }

    internal long SetInterval(int delay, JSValue f, in Arguments a)
    {
        var ctx = synchronizationContext ?? throw JSEngine.NewTypeError($"Synchronization context must be present to set timeout");
        var key = Interlocked.Increment(ref nextInterval);
        JSValue[] args = System.Array.Empty<JSValue>();

        if (a.Length > 2)
        {
            args = new JSValue[a.Length - 2];
            for (int i = 2; i < a.Length; i++)
                args[i - 2] = a.GetAt(i);
        }

        var timer = new Timer((_) =>
        {
            ctx.Post((x) =>
            {
                var f = x as JSValue;
                try
                {
                    f.InvokeFunction(new Arguments(JSUndefined.Value, args));
                }
                catch (Exception ex)
                {
                    ReportError(ex);
                }
                ClearInterval(key);
            }, f);
        }, f, delay, Timeout.Infinite);

        timers.AddOrUpdate(key, timer, (a1, a2) => a2);
        lock (this)
        {
            _waitTask = _waitTask ?? new TaskCompletionSource<int>();
        }
        return key;
    }

    public ICodeCache CodeCache { get; set; } = DictionaryCodeCache.Current;

    internal ConcurrentDictionary<long, JSValue> PendingPromises = new();

    /// <summary>
    /// Quickly evaluates the code, does not wait for promises and timeouts/intervals.
    /// </summary>
    /// <summary>
    /// Host hook invoked by a dynamic <c>import(specifier)</c> in a plain script. Given the
    /// ToString-coerced module specifier, it returns a task for the module namespace object. When
    /// left unset the engine ships no module loader for plain scripts, so dynamic import rejects
    /// with a TypeError. (Inside an ES module the compiler uses the module's own injected loader and
    /// never reaches this hook.)
    /// </summary>
    public Func<string, Task<JSValue>> HostImportModule { get; set; }

    // Installs the global `import` loader that the compiler's ImportCall falls back to in a plain
    // script. `import` is a reserved word, so this property is reachable only through that fallback
    // (and is non-enumerable to stay out of global property enumeration).
    private void InstallDynamicImport()
        => this.FastAddValue(
            KeyStrings.GetOrCreate("import"),
            JSValue.CreateFunction((in Arguments a) => DynamicImport(a.GetAt(0))),
            JSPropertyAttributes.ConfigurableValue);

    // HostImportModuleDynamically: always returns a promise. The specifier is ToString-coerced (an
    // abrupt completion rejecting the promise rather than throwing synchronously), then forwarded to
    // HostImportModule, or rejected with a TypeError when no loader is configured.
    private JSValue DynamicImport(JSValue specifier)
    {
        string moduleSpecifier;
        try
        {
            moduleSpecifier = specifier.StringValue; // ToString — may run user code and throw
        }
        catch (JSException ex)
        {
            return RejectedPromise(ex.Error);
        }

        var loader = HostImportModule;
        if (loader == null)
            return RejectedPromise(JSEngine.NewTypeError(
                $"Cannot import module \"{moduleSpecifier}\": dynamic import is not supported in this host").Error);

        try
        {
            return JSEngine.ClrInterop.Marshal(loader(moduleSpecifier));
        }
        catch (JSException ex)
        {
            return RejectedPromise(ex.Error);
        }
    }

    private static JSValue RejectedPromise(JSValue reason)
        => (JSValue)JSEngine.CreatePromiseFromDelegate((resolve, reject) => reject(reason));

    public JSValue Eval(string code, string codeFilePath = null, JSValue @this = null)
    {
        @this ??= this;
        if (Debugger == null)
        {
            var fx = CoreScript.Compile(code, codeFilePath, codeCache: CodeCache);
            return JSTailCall.Resolve(fx(new Arguments(@this)));
        }

        try
        {
            var f = CoreScript.Compile(code, codeFilePath, codeCache: CodeCache);
            Debugger.ScriptParsed(ID, code, codeFilePath);
            return JSTailCall.Resolve(f(new Arguments(@this)));
        }
        catch (Exception ex)
        {
            ReportError(ex);
            throw;
        }
    }

    /// <summary>
    /// Evaluates JavaScript code with top-level <c>await</c> enabled, then waits
    /// for the returned promise and any pending host tasks before returning.
    /// </summary>
    public async Task<JSValue> EvalWithTopLevelAwaitAsync(string code, string codeFilePath = null, JSValue @this = null)
    {
        @this ??= this;
        JSValue result;

        using (CoreScript.AllowTopLevelAwaitScope())
        {
            var fx = CoreScript.Compile(code, codeFilePath, codeCache: CodeCache);
            result = JSTailCall.Resolve(fx(new Arguments(@this)));
        }

        result = await AwaitThenableResultAsync(result);

        var wt = WaitTask;
        if (wt != null)
            await wt;

        return result;
    }

    /// <summary>
    /// Evaluates the given code, waits for the promise and returns task that
    /// completes till all timeouts/intervals are completed.
    /// </summary>
    /// <remarks>
    /// The JavaScript job loop (promise reactions and async-function continuations)
    /// is posted to a <see cref="SynchronizationContext"/>. If that work were posted
    /// to the <em>caller's</em> ambient context, completion would require the caller to
    /// keep pumping it while it awaits — which single-threaded or otherwise un-pumped
    /// hosts (e.g. some test runners) do not do, deadlocking the returned task. To stay
    /// robust regardless of how the caller awaits us, run the script under our own pumped
    /// loop (<see cref="AsyncPump"/>) on a worker thread, exactly like <see cref="Execute"/>.
    /// The async-local current context flows across the <see cref="Task.Run(System.Action)"/>
    /// boundary, so engine state resolves correctly on the worker thread.
    /// </remarks>
    public Task<JSValue> ExecuteAsync(string code, string codeFilePath = null)
        => Task.Run(() => AsyncPump.Run(() => ExecuteScriptAsync(code, codeFilePath)));

    private async Task<JSValue> ExecuteScriptAsync(string code, string codeFilePath = null)
    {
        var r = CoreScript.Evaluate(code, codeFilePath, codeCache: CodeCache);
        var wt = WaitTask;
        if (wt != null)
            await wt;

        return await AwaitThenableResultAsync(JSTailCall.Resolve(r));
    }

    private static async Task<JSValue> AwaitThenableResultAsync(JSValue r)
    {
        if (r is IJSPromise promise)
            return await promise.Task;

        if (r is not JSObject @object)
            return r;

        var then = @object[KeyStrings.then];
        if (!then.IsFunction)
            return r;

        var promiseObj = JSEngine.CreatePromiseFromDelegate((resolve, reject) =>
        {
            var resolveF = JSValue.CreateFunction((in Arguments a) =>
            {
                var a1 = a.Get1();
                resolve(a1);
                return a1;
            });

            var rejectF = JSValue.CreateFunction((in Arguments a) =>
            {
                var a1 = a.Get1();
                reject(a1);
                return a1;
            });

            var a = new Arguments(@object, resolveF, rejectF);
            then.InvokeFunction(a);
        });

        return await promiseObj.Task;
    }

    /// <summary>
    /// Evaluates the given code, waits for the promise and also
    /// waits synchronously (by running and AsyncPump) for timeouts/intervals to finish
    /// </summary>
    public JSValue Execute(string code, string codeFilePath = null) => AsyncPump.Run(() => ExecuteScriptAsync(code, codeFilePath));
}
