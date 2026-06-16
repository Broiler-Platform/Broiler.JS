using System;
using System.Collections.Generic;
using System.Threading;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;
using Broiler.JavaScript.LinqExpressions.LinqExpressions.GeneratorsV2;

namespace Broiler.JavaScript.Compiler;


// One non-static private method or accessor of a class, captured at class
// evaluation time. Its shared function object(s) live in class-scope variables;
// every instance installs the element under the minted private Key via
// PrivateMethodAdd / PrivateAccessorAdd, which establishes the per-instance brand
// (so a `return`-override object carries it, and a second installation throws).
// Getter/Setter are the two halves of one accessor element (either may be null);
// Method is set instead for an ordinary private method.
public sealed class PrivateInstanceElement
{
    public YExpression Key;
    public YExpression Method;
    public YExpression Getter;
    public YExpression Setter;
}


public class SharedParserStringMap<T>
{
    private static ConcurrentNameMap parserStringCache = new();
    private SAUint32Map<uint> indexes;
    private (StringSpan Key, T Value)[] storage;

    private uint length;

    public T this[in StringSpan name]
    {
        get
        {
            if (parserStringCache.TryGetValue(in name, out var id))
            {
                if (indexes.TryGetValue(id.Key, out var index))
                    return storage[index].Value;
            }

            return default;
        }
        set
        {
            var a = parserStringCache.Get(in name);
            if (!indexes.TryGetValue(a.Key, out var id))
            {
                id = length++;
                indexes.Put(a.Key) = id;
            }

            Save(id, in name, in value);
        }
    }

    private void Save(uint index, in StringSpan key, in T value)
    {
        storage = storage ?? (new (StringSpan, T)[8]);
        if (index >= storage.Length)
            Array.Resize(ref storage, (((int)index >> 2) + 1) << 2);

        storage[index] = (key, value);
    }

    public bool TryGetValue(in StringSpan name, out T value)
    {
        if (parserStringCache.TryGetValue(in name, out var id))
        {
            if (indexes.TryGetValue(id.Key, out var index))
            {
                value = storage[index].Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    public IFastEnumerator<(StringSpan Key, T Value)> AllValues => new Enumerator(this);

    private class Enumerator(SharedParserStringMap<T> map) : IFastEnumerator<(StringSpan Key, T Value)>
    {
        private int index = -1;

        public bool MoveNext(out (StringSpan Key, T Value) item)
        {
            while (true)
            {
                index++;
                if (index < map.length)
                {
                    item = map.storage[index];
                    return true;
                }
                else break;
            }
            item = (StringSpan.Empty, default);
            return false;
        }

        public bool MoveNext(out (StringSpan Key, T Value) item, out int index)
        {
            while (true)
            {
                this.index++;
                if (this.index < map.length)
                {
                    index = this.index;
                    item = map.storage[index];
                    return true;
                }
                else break;
            }

            item = (StringSpan.Empty, default);
            index = 0;
            return false;
        }
    }
}
public class FastFunctionScope : LinkedStackItem<FastFunctionScope>
{
    public class VariableScope : IDisposable
    {
        public YParameterExpression Variable { get; internal set; }
        public YExpression Expression { get; internal set; }
        public string Name { get; internal set; }
        public bool Create { get; internal set; }

        // The JSVariable-typed expression that exposes this binding to a direct eval or
        // `with` body when the binding has no local <see cref="Variable"/> of its own.
        // A named function expression's self-name is captured read-only (no local
        // Variable, so it is otherwise omitted from the eval/with capture); it supplies
        // its underlying JSVariable here so `(function g(){ eval("g") })` can resolve `g`.
        public YExpression EvalCaptureExpression { get; internal set; }

        // The JSVariable-typed expression used to capture this binding for a direct eval
        // or `with` body: the binding's own Variable, or its EvalCaptureExpression.
        internal YExpression CaptureExpression => (YExpression)Variable ?? EvalCaptureExpression;
        public bool IsLexical { get; internal set; }
        public bool IsSimpleCatchBinding { get; internal set; }
        public bool IsDeletable { get; internal set; }
        public AstFunctionExpression OwnerFunction { get; internal set; }
        public YExpression Init { get; private set; }

        /// <summary>
        /// Create Variable first and then assign it, in next step.
        /// 
        /// This is required for recursive function as name/instance of function
        /// is null when it is being created and accessed at the same time
        /// </summary>
        public YExpression PostInit { get; private set; }
        public bool InUse { get; internal set; }
        public bool IsTemp { get; internal set; }
        public bool SkipRegistration { get; internal set; }

        // A parameter-environment shadow binding created for a name that resolves
        // outside a sloppy function whose parameter list contains a direct eval (see
        // EvalShadowVariable). Reads/writes of such a binding go through
        // GetValue/SetValue rather than the JSVariable.Value property.
        public bool IsEvalShadow { get; internal set; }

        public void Dispose() => InUse = false;

        public void SetPostInit(YExpression exp)
        {
            if (exp == null)
            {
                PostInit = null;
                return;
            }

            if (Variable.Type == typeof(JSVariable))
            {
                if (exp.Type == typeof(JSVariable))
                {
                    PostInit = YExpression.Assign(Variable, exp);
                    return;
                }
            }

            PostInit = YExpression.Assign(Expression, exp);
        }

        public void SetInit(YExpression exp, bool initialize = true)
        {
            if (Variable.Type == typeof(JSVariable))
            {
                if (exp != null)
                {
                    if (typeof(JSValue).IsAssignableFrom(exp.Type))
                    {
                        Init = YExpression.Assign(Variable, JSVariableBuilder.New(exp, Name));
                    }
                    else
                    {
                        Init = YExpression.Assign(Variable, exp);
                    }
                }
                else
                {
                    Init = YExpression.Assign(Variable, initialize ? JSVariableBuilder.New(Name) : JSVariableBuilder.NewUninitialized(Name));
                }
            }
            else
            {
                if (exp != null)
                {
                    Init = YExpression.Assign(Variable, exp);
                }
            }
        }
    }

    private SharedParserStringMap<VariableScope> variableScopeList = new();

    // BROILER-PATCH: Register an externally-created variable in this scope.
    // Used for function expression names (ES3 §13) where the variable is
    // declared in the parent scope's block but referenced in the function body.
    internal void AddExternalVariable(in StringSpan name, VariableScope scope) => variableScopeList[name] = scope;

    public AstFunctionExpression Function { get; }

    private YExpression _thisExpression;
    public YExpression ThisExpression
    {
        get => _thisExpression ??= GetVariable("this", true).Expression;
        internal set => _thisExpression = value;
    }

    // new.target is lexically scoped exactly like `this`: an ordinary function
    // captures the running new.target at entry into a closure cell, and arrow
    // functions reuse the enclosing function's cell instead of creating their own
    // (an arrow has no new.target of its own). Null in the root/program scope,
    // where VisitMeta falls back to reading the live call-stack value.
    public YExpression NewTargetExpression { get; private set; }

    internal const string NewTargetBindingName = "new.target";

    public bool HasDisposable => _dispoable != null;

    // True when this scope declares at least one `await using` (async-disposed) resource.
    // Only then must the scope's disposal be awaited; a scope with only synchronous
    // `using` resources disposes synchronously (DisposableStack.Dispose returns undefined),
    // so it must not introduce an await — which is both spec-correct and avoids a `Yield`
    // inside a try/finally nested in a loop, which the async state-machine rewrite cannot
    // currently lower.
    public bool HasAsyncDisposable { get; set; }

    private YParameterExpression _dispoable;
    public YParameterExpression Disposable => _dispoable ??= YExpression.Parameter(typeof(IJSDisposableStack));

    public YExpression ArgumentsExpression { get; }

    public YParameterExpression Arguments { get; }

    public string[] CurrentDirectEvalParameterBindings { get; set; }

    public bool InParameterInitializer { get; set; }

    public string[] DirectEvalPrivateNames { get; }

    public YParameterExpression Context { get; }

    public YParameterExpression StackItem { get; }

    public bool IsRoot => Function == null;

    public LinkedStack<LoopScope> Loop;

    /// <summary>
    /// The home object's prototype, used to resolve <c>super.x</c> property
    /// references. (For a derived constructor this is the superclass prototype,
    /// not the superclass constructor — those differ; see <see cref="SuperConstructor"/>.)
    /// </summary>
    public YExpression Super { get; set; }

    /// <summary>
    /// The superclass constructor, used by a <c>super(...)</c> call in a derived
    /// class constructor. Distinct from <see cref="Super"/> (the prototype).
    /// </summary>
    public YExpression SuperConstructor { get; set; }

    public IEnumerable<VariableScope> Variables
    {
        get
        {
            var en = variableScopeList.AllValues;
            while (en.MoveNext(out var s))
            {
                if (s.Value.Variable != null)
                    yield return s.Value;
            }
        }
    }

    public IEnumerable<YParameterExpression> VariableParameters
    {
        get
        {
            var en = variableScopeList.AllValues;
            while (en.MoveNext(out var s))
            {
                if (s.Value.Variable != null)
                    yield return s.Value.Variable;
            }
        }
    }

    public IEnumerable<YExpression> InitList
    {
        get
        {
            var en = variableScopeList.AllValues;
            while (en.MoveNext(out var s))
            {
                if (s.Value.Init != null)
                    yield return s.Value.Init;
            }

            en = variableScopeList.AllValues;
            while (en.MoveNext(out var s))
            {
                if (s.Value.PostInit != null)
                    yield return s.Value.PostInit;
            }
        }
    }

    public YLabelTarget ReturnLabel { get; }

    public readonly FastFunctionScope TopScope;

    public YParameterExpression Generator { get; set; }

    public YParameterExpression Awaiter { get; set; }

    private static int scopeID = 0;

    public IFastEnumerable<AstClassProperty> MemberInits { get; set; }

    public IReadOnlyDictionary<AstClassProperty, YExpression> ComputedMemberNames { get; set; }

    // Non-static private methods/accessors installed on each instance (before the
    // field initializers) by the constructor's InitMembers. Null/empty for classes
    // without instance private methods or accessors.
    public IReadOnlyList<PrivateInstanceElement> PrivateInstanceElements { get; set; }

    public readonly FastFunctionScope RootScope;

    public FastFunctionScope(FastPool pool, AstFunctionExpression fx, YExpression previousThis = null, YExpression super = null, bool isAsync = false,
        IFastEnumerable<AstClassProperty> memberInits = null, FastFunctionScope previous = null, string[] directEvalPrivateNames = null,
        IReadOnlyDictionary<AstClassProperty, YExpression> computedMemberNames = null, bool thisIsUninitialized = false,
        YExpression previousNewTarget = null)
    {
        RootScope = previous ?? this;
        TopScope = this;
        var sID = Interlocked.Increment(ref scopeID);
        MemberInits = memberInits;
        ComputedMemberNames = computedMemberNames;
        DirectEvalPrivateNames = directEvalPrivateNames;
        InParameterInitializer = previous?.InParameterInitializer ?? false;
        Function = fx;
        Super = super;

        if (fx?.Generator ?? false)
        {
            Generator = YExpression.Parameter(typeof(ClrGeneratorV2), "clrGenerator");
        }
        else
        {
            Generator = null;
        }

        if (fx?.Async ?? true)
            Generator = YExpression.Parameter(typeof(ClrGeneratorV2), "clrGenerator");

        if (isAsync && Generator == null)
            Generator = YExpression.Parameter(typeof(ClrGeneratorV2), "clrGenerator");

        Arguments = (fx?.Generator ?? false) ? YExpression.Parameter(typeof(Arguments), $"a-{sID}") : YExpression.Parameter(typeof(Arguments).MakeByRefType(), $"a-{sID}");
        ArgumentsExpression = Arguments;

        if (previousThis == null)
        {
            // this is needed to fix closure over lambda
            // this can be improved
            var t = CreateVariable("this", thisIsUninitialized ? null : ArgumentsBuilder.This(Arguments), initialize: !thisIsUninitialized);
            ThisExpression = t.Expression;
        }
        else
        {
            // Arrow function: `this` is the enclosing scope's `this` expression
            // captured at creation. Bind it directly so a read resolves to exactly
            // that expression rather than falling back to a `GetVariable("this")`
            // lookup, which walks past a caller-installed override (e.g. a static
            // field initializer rebinding `this` to the class constructor) to the
            // nearest real `this` binding.
            ThisExpression = previousThis;
        }

        if (previousNewTarget != null)
        {
            // Arrow function: reuse the enclosing function's new.target cell.
            NewTargetExpression = previousNewTarget;
        }
        else if (fx != null)
        {
            // Ordinary function: capture the running new.target at entry. The init
            // runs in InitList, after the CallStackItem is pushed, so the live
            // value is observed.
            var nt = CreateVariable(NewTargetBindingName, JSContextBuilder.NewTarget());
            NewTargetExpression = nt.Expression;
        }

        Context = YExpression.Parameter(typeof(JSContext), $"{nameof(Context)}{sID}");
        StackItem = YExpression.Parameter(typeof(CallStackItem), $"{nameof(StackItem)}{sID}");

        Loop = new LinkedStack<LoopScope>();
        TempVariables = [];
        ReturnLabel = YExpression.Label(typeof(JSValue));
    }

    public FastFunctionScope(FastFunctionScope p)
    {
        Function = p.Function;
        // A nested block does not change `this`; inherit the enclosing scope's
        // binding. Copy the (possibly still-unresolved) backing field rather than
        // forcing resolution: when the parent's `this` was set explicitly to a
        // non-variable expression — e.g. a static field initializer rebinding it to
        // the class constructor box — the lazy GetVariable("this") fallback would
        // otherwise walk PAST that override to an outer real `this` binding.
        _thisExpression = p._thisExpression;
        TopScope = p.TopScope;
        RootScope = p.RootScope;
        MemberInits = p.MemberInits;
        ComputedMemberNames = p.ComputedMemberNames;
        PrivateInstanceElements = p.PrivateInstanceElements;
        DirectEvalPrivateNames = p.DirectEvalPrivateNames;
        InParameterInitializer = p.InParameterInitializer;
        ArgumentsExpression = p.ArgumentsExpression;
        Generator = p.Generator;
        Awaiter = p.Awaiter;
        TempVariables = p.TempVariables;
        Super = p.Super;
        SuperConstructor = p.SuperConstructor;
        Context = p.Context;
        StackItem = p.StackItem;
        Loop = p.Loop;
        ReturnLabel = p.ReturnLabel;
        NewTargetExpression = p.NewTargetExpression;
    }

    public YExpression this[string name] => GetVariable(name).Expression;

    public IEnumerable<VariableScope> GetVisibleVariables()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var current = this;

        while (current != null)
        {
            var variables = current.variableScopeList.AllValues;
            while (variables.MoveNext(out var entry))
            {
                var variable = entry.Value;
                var capture = variable.CaptureExpression;
                if (capture == null
                    || capture.Type != typeof(JSVariable)
                    || variable.IsTemp
                    || variable.IsEvalShadow
                    || variable.Name == "this"
                    || variable.Name == NewTargetBindingName
                    || !seen.Add(NormalizeVisibleName(variable.Name)))
                {
                    continue;
                }

                yield return variable;
            }

            current = current.Parent;
        }
    }

    /// <summary>
    /// The bindings a `with` statement must overlay so they remain resolvable
    /// inside the with body even though the object environment is consulted
    /// first. Only *function-owned* bindings are returned: a global/program-level
    /// `var` is already resolvable through the global environment and is kept in
    /// sync with its global-object property by the normal dual-binding path, so
    /// overlaying it (and isolating writes from that property) would be wrong.
    /// Function-local bindings, by contrast, genuinely shadow same-named globals
    /// and their writes must stay local.
    /// </summary>
    public IEnumerable<VariableScope> GetWithFallbackVariables()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var current = this;

        while (current != null)
        {
            if (current.Function != null)
            {
                var variables = current.variableScopeList.AllValues;
                while (variables.MoveNext(out var entry))
                {
                    var variable = entry.Value;
                    if (variable.Variable == null
                        || variable.Variable.Type != typeof(JSVariable)
                        || variable.IsTemp
                        || variable.Name == "this"
                        || variable.Name == NewTargetBindingName
                        || !seen.Add(NormalizeVisibleName(variable.Name)))
                    {
                        continue;
                    }

                    yield return variable;
                }
            }

            current = current.Parent;
        }
    }

    public IEnumerable<string> GetDirectEvalLexicalBindingNames(bool excludeSimpleCatchBindings = false)
    {
        var scopes = new List<FastFunctionScope>();
        var current = this;

        while (current != null && current.Function == Function)
        {
            scopes.Add(current);
            current = current.Parent;
        }

        var lastIncludedScope = Function == null
            ? scopes.Count - 2
            : scopes.Count - 1;

        if (lastIncludedScope <= 0)
            yield break;

        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < lastIncludedScope; i++)
        {
            var variables = scopes[i].variableScopeList.AllValues;
            while (variables.MoveNext(out var entry))
            {
                var variable = entry.Value;
                if (!variable.IsLexical
                    || variable.IsTemp
                    || string.IsNullOrEmpty(variable.Name)
                    // Per Annex B.3.4 a simple CatchParameter does not block a `var`
                    // of the same name in non-strict direct eval, so it is excluded
                    // from the conflict set there (strict mode still reports it).
                    || (excludeSimpleCatchBindings && variable.IsSimpleCatchBinding)
                    || !seen.Add(NormalizeVisibleName(variable.Name)))
                {
                    continue;
                }

                yield return variable.Name;
            }
        }
    }

    public VariableScope CreateException(string name)
    {
        var v = new VariableScope { Variable = YExpression.Parameter(typeof(Exception), name + "Exp") };
        variableScopeList[name + DateTime.UtcNow.Ticks] = v;
        v.Expression = v.Variable;

        return v;
    }

    private readonly Sequence<VariableScope> TempVariables;
    private static int id;

    public VariableScope GetTempVariable(Type type = null)
    {
        type = type ?? typeof(JSValue);
        var fe = TopScope.variableScopeList.AllValues;

        while (fe.MoveNext(out var item))
        {
            var v = item.Value;
            if (v.IsTemp && v.Expression.Type == type && !v.InUse)
            {
                v.InUse = true;
                return v;
            }
        }

        var tp = YExpression.Variable(type, "#Temp" + type.Name + id++);
        var temp = new VariableScope
        {
            Create = true,
            Name = tp.Name,
            IsTemp = true,
            InUse = true,
            Expression = tp,
            Variable = tp
        };

        TopScope.variableScopeList[temp.Name] = temp;
        return temp;
    }

    public bool IsFunctionScope => Parent?.Function != Function;

    private static string NormalizeVisibleName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        if (name[^1] == '`')
            name = name[..^1];

        var underscore = name.LastIndexOf('_');
        if (underscore > 0 && int.TryParse(name[(underscore + 1)..], out _))
            name = name[..underscore];

        return name;
    }

    public VariableScope CreateVariable(in StringSpan name, YExpression init = null, bool newScope = false, Type type = null, bool initialize = true)
    {
        var v = variableScopeList[name];
        if (v != null)
            return v;

        // search parent if it is in same function scope...
        if (!newScope)
        {
            var p = Parent;
            while (p != null && p.Function == Function)
            {
                v = p.variableScopeList[name];
                if (v != null)
                    return v;

                p = p.Parent;
            }
        }

        // we need to move variable in top scope...
        var pe = YExpression.Parameter(type ?? typeof(JSVariable), name.Value);
        var ve = JSVariable.ValueExpression(pe);
        
        v = new VariableScope
        {
            Name = name.Value,
            Expression = ve,
            Variable = pe,
            Create = true,
            IsLexical = newScope,
            OwnerFunction = Function
        };
        
        v.SetInit(init, initialize);
        variableScopeList[name] = v;
        
        return v;
    }

    public VariableScope GetVariable(in StringSpan name, bool createClosure = true)
    {

        var start = this;
        while (start != null)
        {
            if (start.variableScopeList.TryGetValue(name, out var result))
                return result;

            start = start.Parent;
        }

        if (!createClosure)
            throw new ArgumentOutOfRangeException($"{name} not found in current variable scope");

        return null;
    }

    public bool TryGetOwnVariable(in StringSpan name, out VariableScope variable)
        => variableScopeList.TryGetValue(name, out variable);

    // Parameter-environment shadow bindings created in this function scope (see
    // EvalShadowVariable). Registered into the function's CallStackItem at entry so
    // a direct eval in the parameter list writes the eval-introduced var into the
    // shared binding the closures capture.
    public readonly List<VariableScope> EvalShadows = [];

    /// <summary>
    /// Creates a shadow binding for <paramref name="name"/> — a name that resolves
    /// to <paramref name="outerVariable"/> outside this (the boundary) function whose
    /// parameter list contains a sloppy direct eval. The binding forwards to the
    /// outer one until the eval introduces the var; reads use <c>GetValue</c>.
    /// </summary>
    public VariableScope CreateEvalShadow(in StringSpan name, YParameterExpression outerVariable, bool outerIsGlobal)
    {
        var pe = YExpression.Parameter(typeof(JSVariable), name.Value + "`evalShadow");
        var v = new VariableScope
        {
            Name = name.Value,
            Variable = pe,
            Expression = EvalShadowBuilder.GetValue(pe),
            Create = true,
            IsEvalShadow = true,
            OwnerFunction = Function,
        };
        v.SetInit(EvalShadowBuilder.New(name.Value, outerVariable, outerIsGlobal), initialize: false);
        variableScopeList[name] = v;
        EvalShadows.Add(v);
        return v;
    }
}
