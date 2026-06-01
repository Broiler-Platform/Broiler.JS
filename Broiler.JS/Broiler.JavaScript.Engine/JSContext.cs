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
    public JSValue Object { get; private set; }
    public JSValue IntrinsicEval { get; private set; } = JSUndefined.Value;
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
    private readonly List<DirectEvalScope> activeDirectEvalScopes = [];
    private readonly List<CallStackItem> directEvalActivationOwners = [];
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

    private sealed class DirectEvalScope : IDisposable
    {
        private readonly JSContext context;
        private readonly List<Entry> entries = [];

        private sealed class Entry
        {
            public KeyString Name;
            public JSVariable OverlayVariable;
            public JSVariable PreviousVariable;
            public bool HadPreviousVariable;
            public bool HadOwnProperty;
            public JSValue PreviousValue;
        }

        public DirectEvalScope(JSContext context, JSVariable[] variables)
        {
            this.context = context;
            if (variables == null || variables.Length == 0)
            {
                context.directEvalDepth++;
                return;
            }

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
                    entry.PreviousValue = context[key];

                entries.Add(entry);
                context.Register(variable);
                context.globalVars.Put(key.Key) = variable;
            }

            context.activeDirectEvalScopes.Add(this);
            context.directEvalBindingNameScopes.Add(ExtractBindingNames(variables));
            context.directEvalDepth++;
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
                        context[entry.Name] = entry.PreviousValue;
                    else
                        context.Delete(entry.Name);
                }
                else
                {
                    context.globalVars.RemoveAt(entry.Name.Key);
                    if (entry.HadOwnProperty)
                        context[entry.Name] = entry.PreviousValue;
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
                    entry.PreviousValue = context[entry.Name];
                entry.HadPreviousVariable = context.globalVars.TryGetValue(entry.Name.Key, out var pv);
                entry.PreviousVariable = pv;

                context.Register(entry.OverlayVariable);
                context.globalVars.Put(entry.Name.Key) = entry.OverlayVariable;
            }
        }

        public void Dispose()
        {
            context.activeDirectEvalScopes.Remove(this);

            foreach (var entry in entries)
            {
                if (entry.HadPreviousVariable)
                {
                    if (!ReferenceEquals(entry.PreviousVariable, entry.OverlayVariable))
                        entry.PreviousVariable.Value = entry.OverlayVariable.Value;

                    context.globalVars.Put(entry.Name.Key) = entry.PreviousVariable;

                    if (!ReferenceEquals(entry.PreviousVariable, entry.OverlayVariable))
                    {
                        if (entry.HadOwnProperty)
                            context[entry.Name] = entry.PreviousVariable.Value;
                        else
                            context.Delete(entry.Name);
                    }

                    continue;
                }

                context.globalVars.RemoveAt(entry.Name.Key);
                if (entry.HadOwnProperty)
                    context[entry.Name] = entry.PreviousValue;
                else
                    context.Delete(entry.Name);
            }

            if (context.directEvalBindingNameScopes.Count > 0)
                context.directEvalBindingNameScopes.RemoveAt(context.directEvalBindingNameScopes.Count - 1);
            context.directEvalDepth--;
        }
    }

    public IDisposable PushDirectEvalScope(JSVariable[] variables) => new DirectEvalScope(this, variables);

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

    internal bool TryResolveDirectEvalBinding(in KeyString name, out JSVariable variable)
    {
        for (var current = Top; current != null; current = current.Parent)
        {
            if (current.TryGetDirectEvalBinding(name, out variable))
                return true;
        }

        variable = null;
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

    public JSValue Register(JSVariable variable)
    {
        KeyString name = variable.Name;
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

        var oldV = this[name];
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
        else if (oldV != v)
        {
            this[name] = v;
        }

        if ((directEvalDepth <= 0 || hadExistingVariable)
            && (!hadExistingVariable || ReferenceEquals(existingVariable, variable)))
            globalVars.Put(name.Key) = variable;
        return v;
    }

    internal JSVariable RegisterDirectEvalVariable(JSVariable variable)
    {
        Register(variable);
        return variable;
    }

    public override JSValue this[KeyString name]
    {
        get
        {
            if (TryResolveDirectEvalBinding(name, out var directEvalBinding))
                return directEvalBinding.Value;

            return base[name];
        }
        set
        {
            if (TryResolveDirectEvalBinding(name, out var directEvalBinding))
            {
                directEvalBinding.Value = value;
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
            return directEvalBinding.Value;

        if (globalVars.TryGetValue(name.Key, out var variable))
            return variable.Value;

        if (!GetInternalProperty(name).IsEmpty)
            return this[name];

        throw JSEngine.NewReferenceError($"{name} is not defined");
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
            directEvalBinding.Value = value;
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
        this[functionKey] = func;
        FunctionPrototype = ((IJSFunction)func).Prototype as JSObject;
        if (FunctionPrototype.GetInternalProperty(KeyStrings.length, false).IsEmpty)
            FunctionPrototype.FastAddValue(KeyStrings.length, JSValue.NumberZero, JSPropertyAttributes.ConfigurableReadonlyValue);
        Object = JSEngine.CreateObjectClass(this, false);
        this[objectKey] = Object;
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
        this[KeyStrings.globalThis] = this;
        this[KeyStrings.debug] = JSValue.CreateFunction(Debug);
    }

    private void ReattachFunctionPrototypeMethods()
    {
        var en = FunctionPrototype.GetOwnProperties(false).GetEnumerator();
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
    public JSValue Eval(string code, string codeFilePath = null, JSValue @this = null)
    {
        @this ??= this;
        if (Debugger == null)
        {
            var fx = CoreScript.Compile(code, codeFilePath, codeCache: CodeCache);
            return fx(new Arguments(@this));
        }

        try
        {
            var f = CoreScript.Compile(code, codeFilePath, codeCache: CodeCache);
            Debugger.ScriptParsed(ID, code, codeFilePath);
            return f(new Arguments(@this));
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
            result = fx(new Arguments(@this));
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
    public async Task<JSValue> ExecuteAsync(string code, string codeFilePath = null)
    {
        var r = CoreScript.Evaluate(code, codeFilePath, codeCache: CodeCache);
        var wt = WaitTask;
        if (wt != null)
            await wt;

        return await AwaitThenableResultAsync(r);
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
    public JSValue Execute(string code, string codeFilePath = null) => AsyncPump.Run(() => ExecuteAsync(code, codeFilePath));
}
