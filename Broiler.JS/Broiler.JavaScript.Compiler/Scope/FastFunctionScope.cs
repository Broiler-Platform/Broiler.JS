using System;
using System.Collections.Generic;
using System.Threading;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine;
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
    public BExpression Key;
    public BExpression Method;
    public BExpression Getter;
    public BExpression Setter;
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
        public BParameterExpression Variable { get; internal set; }
        public BExpression Expression { get; internal set; }

        // A distinct expression for a *read* of this binding when it differs from the
        // assignable <see cref="Expression"/> used for writes. Used for an eval-introduced global
        // `var`, whose read must throw a ReferenceError once the binding is deleted (a method call
        // that cannot serve as an assignment lvalue). Null means reads use <see cref="Expression"/>.
        // Only consulted for a throwing read; `typeof` keeps the non-throwing Expression.
        public BExpression ReadExpression { get; internal set; }
        public string Name { get; internal set; }
        public bool Create { get; internal set; }

        // The JSVariable-typed expression that exposes this binding to a direct eval or
        // `with` body when the binding has no local <see cref="Variable"/> of its own.
        // A named function expression's self-name is captured read-only (no local
        // Variable, so it is otherwise omitted from the eval/with capture); it supplies
        // its underlying JSVariable here so `(function g(){ eval("g") })` can resolve `g`.
        public BExpression EvalCaptureExpression { get; internal set; }

        // The JSVariable-typed expression used to capture this binding for a direct eval
        // or `with` body: the binding's own Variable, or its EvalCaptureExpression.
        internal BExpression CaptureExpression => (BExpression)Variable ?? EvalCaptureExpression;
        public bool IsLexical { get; internal set; }
        public bool IsSimpleCatchBinding { get; internal set; }
        public bool IsDeletable { get; internal set; }
        public AstFunctionExpression OwnerFunction { get; internal set; }
        public BExpression Init { get; private set; }

        /// <summary>
        /// Create Variable first and then assign it, in next step.
        /// 
        /// This is required for recursive function as name/instance of function
        /// is null when it is being created and accessed at the same time
        /// </summary>
        public BExpression PostInit { get; private set; }
        public bool InUse { get; internal set; }
        public bool IsTemp { get; internal set; }
        public bool SkipRegistration { get; internal set; }

        // A parameter-environment shadow binding created for a name that resolves
        // outside a sloppy function whose parameter list contains a direct eval (see
        // EvalShadowVariable). Reads/writes of such a binding go through
        // GetValue/SetValue rather than the JSVariable.Value property.
        public bool IsEvalShadow { get; internal set; }

        /// <summary>
        /// The raw CLR <c>double</c> holding this binding, for a local the compiler proved
        /// only ever holds a number (docs/performance-roadmap.md P2-2 item 3). Null for every
        /// ordinary binding.
        /// </summary>
        /// <remarks>
        /// <see cref="Expression"/> stays a boxing READ of this local, so the hundreds of
        /// places that consume a binding as a <c>JSValue</c> keep working untouched. Only the
        /// writes, and the arithmetic that wants the number unboxed, reach for the storage
        /// directly — and a write through <see cref="Expression"/> would be an assignment to a
        /// method call, which the IL backend rejects loudly rather than miscompiling.
        /// </remarks>
        public BParameterExpression NumericStorage { get; internal set; }

        /// <summary>
        /// Item 3-8a: the raw <c>double</c> half of a speculative local, live exactly while
        /// <see cref="SpeculativeNumericFlag"/> holds.
        /// </summary>
        /// <remarks>
        /// <b>Deliberately NOT <see cref="NumericStorage"/>.</b> That field means "this binding IS a
        /// raw double", and five separate fast paths read it on exactly that understanding — the
        /// numeric element index, the mixed comparison, the raw store, the native update step and
        /// <c>ToNativeExpression</c>. A speculative local is a double only *sometimes*, so giving it
        /// that field would have each of those read a dead value and answer wrongly. It gets its own
        /// field instead, and a site opts in by naming it.
        /// </remarks>
        public BParameterExpression SpeculativeNumericStorage { get; internal set; }

        /// <summary>Whether <see cref="SpeculativeNumericStorage"/> currently holds the binding's value.</summary>
        public BParameterExpression SpeculativeNumericFlag { get; internal set; }

        /// <summary>
        /// The <c>JSValue</c> half, live while the flag does not hold. This is the binding's
        /// ordinary storage; <see cref="Expression"/> becomes a conditional over the two, so every
        /// existing READ site stays correct without being touched and a write through it is an
        /// assignment to a conditional, which the backend rejects rather than miscompiling.
        /// </summary>
        public BParameterExpression SpeculativeSlot { get; internal set; }

        public void Dispose() => InUse = false;

        public void SetPostInit(BExpression exp)
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
                    PostInit = BExpression.Assign(Variable, exp);
                    return;
                }
            }

            PostInit = BExpression.Assign(Expression, exp);
        }

        public void SetInit(BExpression exp, bool initialize = true)
        {
            if (Variable.Type == typeof(JSVariable))
            {
                if (exp != null)
                {
                    if (typeof(JSValue).IsAssignableFrom(exp.Type))
                    {
                        Init = BExpression.Assign(Variable, JSVariableBuilder.New(exp, Name));
                    }
                    else
                    {
                        Init = BExpression.Assign(Variable, exp);
                    }
                }
                else
                {
                    Init = BExpression.Assign(Variable, initialize ? JSVariableBuilder.New(Name) : JSVariableBuilder.NewUninitialized(Name));
                }
            }
            else
            {
                if (exp != null)
                {
                    Init = BExpression.Assign(Variable, exp);
                }
                else if (initialize && Variable.Type == typeof(JSValue))
                {
                    Init = BExpression.Assign(Variable, JSUndefinedBuilder.Value);
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
    public bool CanScalarReplaceLocals { get; internal set; }

    /// <summary>
    /// Names some nested function mentions, and which therefore may not live in a raw local:
    /// capturing a binding requires naming it. Empty when the function has no nested functions.
    /// </summary>
    public IReadOnlySet<string> CapturedByNestedFunctions { get; internal set; } = NoCapturedNames;

    /// <summary>
    /// The subset of <see cref="CapturedByNestedFunctions"/> named by a function declaration at
    /// this function's body top level — one whose function object exists from function entry.
    /// Such a name may not become a raw <c>double</c> even when the analysis proves it numeric
    /// (docs/performance-roadmap.md item 3-7).
    /// </summary>
    /// <remarks>
    /// Every other numeric-local condition rests on a textual argument: a name with a reference
    /// before its declaration is refused, so the initializer has always run by the time anything
    /// reads it, and a raw double hoisted to 0 is never observed where <c>undefined</c> belongs.
    /// A hoisted function declaration is the one construct that breaks the link between text
    /// order and execution order — its body is textually after the declaration and its value
    /// exists before it — so <c>var r = g(); var s = 0; function g() { return s; }</c> reads
    /// <c>s</c> before its initializer runs from a body position the walk accepts. Empty when the
    /// function has no such declaration.
    /// </remarks>
    public IReadOnlySet<string> CapturedByHoistedFunctions { get; internal set; } = NoCapturedNames;

    /// <summary>Whether the function contains any nested function at all.</summary>
    public bool HasNestedFunctions { get; internal set; }

    /// <summary>
    /// Whether <c>arguments</c> is named anywhere in this function or in a function nested in
    /// it. A mapped <c>arguments</c> object aliases the parameters through their
    /// <see cref="JSVariable"/> cells, so a function that may build one keeps them
    /// (docs/performance-roadmap.md item 3-3). Defaults to <c>true</c>: a scope nobody scanned
    /// must not be treated as one that was found clean.
    /// </summary>
    public bool MentionsArguments { get; internal set; } = true;

    private static readonly IReadOnlySet<string> NoCapturedNames = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>
    /// Names this function's analysis proved only ever hold a number, so they can live in a
    /// CLR <c>double</c> (docs/performance-roadmap.md P2-2 item 3). Never null.
    /// </summary>
    public System.Collections.Generic.IReadOnlySet<string> NumericLocals { get; internal set; }
        = System.Collections.Immutable.ImmutableHashSet<string>.Empty;

    /// <summary>
    /// Names item 3-8a speculates on: the analysis proves them numeric only if a value arriving
    /// from outside the function holds a number, so they are held in BOTH representations at once
    /// — a raw <c>double</c> and the <c>JSValue</c> slot — with a flag saying which is live
    /// (docs/performance-roadmap.md item 3-8a). Never null.
    /// </summary>
    public System.Collections.Generic.IReadOnlySet<string> SpeculativeNumericLocals { get; internal set; }
        = System.Collections.Immutable.ImmutableHashSet<string>.Empty;
    public bool HasOuterFunctionCaptures { get; private set; }

    // True when this function scope is an eval-shadow boundary: a sloppy function whose
    // parameter list or body contains a direct eval, so identifier references resolving
    // outside it are routed through EvalShadowVariable bindings (see TryResolveEvalShadow).
    // Marks the scope persistently so a NESTED eval-boundary (e.g. an eval-containing
    // closure inside an eval-containing function) can chain its shadows through this one.
    public bool IsEvalShadowBoundary { get; internal set; }

    private BExpression _thisExpression;
    public BExpression ThisExpression
    {
        get => _thisExpression ??= GetVariable("this", true).Expression;
        internal set => _thisExpression = value;
    }

    // new.target is lexically scoped exactly like `this`: an ordinary function
    // captures the running new.target at entry into a closure cell, and arrow
    // functions reuse the enclosing function's cell instead of creating their own
    // (an arrow has no new.target of its own). Null in the root/program scope,
    // where VisitMeta falls back to reading the live call-stack value.
    public BExpression NewTargetExpression { get; set; }

    internal const string NewTargetBindingName = "new.target";

    public bool HasDisposable => _dispoable != null;

    // True when this scope declares at least one `await using` (async-disposed) resource.
    // Only then must the scope's disposal be awaited; a scope with only synchronous
    // `using` resources disposes synchronously (DisposableStack.Dispose returns undefined),
    // so it must not introduce an await — which is both spec-correct and avoids a `Yield`
    // inside a try/finally nested in a loop, which the async state-machine rewrite cannot
    // currently lower.
    public bool HasAsyncDisposable { get; set; }

    private BParameterExpression _dispoable;
    public BParameterExpression Disposable => _dispoable ??= BExpression.Parameter(typeof(IJSDisposableStack));

    public BExpression ArgumentsExpression { get; }

    public BParameterExpression Arguments { get; }

    public string[] CurrentDirectEvalParameterBindings { get; set; }

    public bool InParameterInitializer { get; set; }

    public string[] DirectEvalPrivateNames { get; }

    public BParameterExpression Context { get; }

    public BParameterExpression StackItem { get; }

    public bool IsRoot => Function == null;

    public LinkedStack<LoopScope> Loop;

    /// <summary>
    /// The home object's prototype, used to resolve <c>super.x</c> property
    /// references. (For a derived constructor this is the superclass prototype,
    /// not the superclass constructor — those differ; see <see cref="SuperConstructor"/>.)
    /// </summary>
    public BExpression Super { get; set; }

    /// <summary>
    /// The superclass constructor, used by a <c>super(...)</c> call in a derived
    /// class constructor. Distinct from <see cref="Super"/> (the prototype).
    /// </summary>
    public BExpression SuperConstructor { get; set; }

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

    public IEnumerable<BParameterExpression> VariableParameters
    {
        get
        {
            var en = variableScopeList.AllValues;
            while (en.MoveNext(out var s))
            {
                if (s.Value.Variable != null)
                    yield return s.Value.Variable;

                // Item 3-8a: a speculative local needs its raw double and its flag declared as
                // method locals too. They are yielded here rather than tracked separately so a
                // scope cannot emit the binding without them.
                if (s.Value.SpeculativeNumericStorage != null)
                {
                    yield return s.Value.SpeculativeNumericStorage;
                    yield return s.Value.SpeculativeNumericFlag;
                }
            }
        }
    }

    public IEnumerable<BExpression> InitList
    {
        get
        {
            foreach (var init in InitOnlyList)
                yield return init;

            foreach (var postInit in PostInitList)
                yield return postInit;
        }
    }

    // The binding-construction half of InitList (variable allocation, direct-eval local
    // binding setup). These run before parameter binding.
    public IEnumerable<BExpression> InitOnlyList
    {
        get
        {
            var en = variableScopeList.AllValues;
            while (en.MoveNext(out var s))
            {
                if (s.Value.Init != null)
                    yield return s.Value.Init;
            }
        }
    }

    // The post-construction half of InitList: hoisted FunctionDeclaration value
    // assignments. Per FunctionDeclarationInstantiation these run AFTER the formal
    // parameters are bound, so a body `function x(){}` overrides a same-named parameter
    // rather than being clobbered by it.
    public IEnumerable<BExpression> PostInitList
    {
        get
        {
            var en = variableScopeList.AllValues;
            while (en.MoveNext(out var s))
            {
                if (s.Value.PostInit != null)
                    yield return s.Value.PostInit;
            }
        }
    }

    public BLabelTarget ReturnLabel { get; }

    public readonly FastFunctionScope TopScope;

    public BParameterExpression Generator { get; set; }

    public BParameterExpression Awaiter { get; set; }

    private static int scopeID = 0;

    public IFastEnumerable<AstClassProperty> MemberInits { get; set; }

    public IReadOnlyDictionary<AstClassProperty, BExpression> ComputedMemberNames { get; set; }

    // For each public instance auto-accessor (`accessor x = v`), the class-scope
    // variable holding its minted private backing-field key. The constructor's
    // InitMembers installs the backing field (not a public data property) under this
    // key, while the getter/setter pair sits on the prototype. Null when the class
    // has no public instance auto-accessors.
    public IReadOnlyDictionary<AstClassProperty, BExpression> AutoAccessorBackingKeys { get; set; }

    // Non-static private methods/accessors installed on each instance (before the
    // field initializers) by the constructor's InitMembers. Null/empty for classes
    // without instance private methods or accessors.
    public IReadOnlyList<PrivateInstanceElement> PrivateInstanceElements { get; set; }

    public readonly FastFunctionScope RootScope;

    public FastFunctionScope(FastPool pool, AstFunctionExpression fx, BExpression previousThis = null, BExpression super = null, bool isAsync = false,
        IFastEnumerable<AstClassProperty> memberInits = null, FastFunctionScope previous = null, string[] directEvalPrivateNames = null,
        IReadOnlyDictionary<AstClassProperty, BExpression> computedMemberNames = null, bool thisIsUninitialized = false,
        BExpression previousNewTarget = null)
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
            Generator = BExpression.Parameter(typeof(ClrGeneratorV2), "clrGenerator");
        }
        else
        {
            Generator = null;
        }

        if (fx?.Async ?? true)
            Generator = BExpression.Parameter(typeof(ClrGeneratorV2), "clrGenerator");

        if (isAsync && Generator == null)
            Generator = BExpression.Parameter(typeof(ClrGeneratorV2), "clrGenerator");

        Arguments = (fx?.Generator ?? false) ? BExpression.Parameter(typeof(Arguments), $"a-{sID}") : BExpression.Parameter(typeof(Arguments).MakeByRefType(), $"a-{sID}");
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

        Context = BExpression.Parameter(typeof(JSContext), $"{nameof(Context)}{sID}");
        StackItem = BExpression.Parameter(typeof(FrameToken), $"{nameof(StackItem)}{sID}");

        Loop = new LinkedStack<LoopScope>();
        TempVariables = [];
        ReturnLabel = BExpression.Label(typeof(JSValue));
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
        AutoAccessorBackingKeys = p.AutoAccessorBackingKeys;
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
        CanScalarReplaceLocals = p.CanScalarReplaceLocals;
        CapturedByNestedFunctions = p.CapturedByNestedFunctions;
        CapturedByHoistedFunctions = p.CapturedByHoistedFunctions;
        HasNestedFunctions = p.HasNestedFunctions;
        NumericLocals = p.NumericLocals;
        SpeculativeNumericLocals = p.SpeculativeNumericLocals;
    }

    public BExpression this[string name] => GetVariable(name).Expression;

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

    // The var-environment binding names of the *immediate* function enclosing this scope (its own
    // var / parameter / function-declaration bindings, walking only its own scopes — not enclosing
    // functions or the global scope). A sloppy direct eval declaring `var X` reuses the binding of
    // such a name in the calling function's var environment instead of creating a new one, so this
    // distinguishes the immediate function's locals (reuse) from a captured enclosing-function or
    // global binding of the same name (which the eval's `var` must shadow with a fresh local).
    public IEnumerable<string> GetImmediateVarEnvNames()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var current = this;
        while (current != null && current.Function == Function)
        {
            var variables = current.variableScopeList.AllValues;
            while (variables.MoveNext(out var entry))
            {
                var variable = entry.Value;
                if (variable.IsLexical
                    || variable.IsTemp
                    || variable.IsEvalShadow
                    || string.IsNullOrEmpty(variable.Name)
                    || variable.Name == "this"
                    || variable.Name == NewTargetBindingName
                    || !seen.Add(NormalizeVisibleName(variable.Name)))
                {
                    continue;
                }

                yield return variable.Name;
            }

            current = current.Parent;
        }
    }

    public VariableScope CreateException(string name)
    {
        var v = new VariableScope { Variable = BExpression.Parameter(typeof(Exception), name + "Exp") };
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

        var tp = BExpression.Variable(type, "#Temp" + type.Name + id++);
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

    public VariableScope CreateVariable(in StringSpan name, BExpression init = null, bool newScope = false, Type type = null, bool initialize = true)
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
        var variableType = type ?? typeof(JSVariable);
        var pe = BExpression.Parameter(variableType, name.Value);

        // Item 3-8a: a name the analysis proves numeric only under an assumption about a value
        // from outside the function is held in BOTH representations. The flag decides which is
        // live; Expression below reads through it, so nothing that consumes the binding as a
        // JSValue has to know, and a write through Expression fails loudly.
        BParameterExpression speculativeDouble = null;
        BParameterExpression speculativeFlag = null;
        if (variableType == typeof(JSValue)
            && SpeculativeNumericLocals.Contains(name.Value)
            && SpeculativeNumericLocals2.Enabled)
        {
            speculativeDouble = BExpression.Parameter(typeof(double), name.Value + "#num");
            speculativeFlag = BExpression.Parameter(typeof(bool), name.Value + "#isnum");
        }
        // A numeric local's readable Expression BOXES its storage, so every consumer that
        // expects a JSValue keeps working; writes go through NumericStorage instead.
        var ve = variableType == typeof(JSVariable)
            ? JSVariable.ValueExpression(pe)
            : variableType == typeof(double)
                ? JSNumberBuilder.New(pe)
                : speculativeFlag != null
                    ? BExpression.Condition(speculativeFlag, JSNumberBuilder.New(speculativeDouble), pe, typeof(JSValue))
                    : pe;
        
        v = new VariableScope
        {
            Name = name.Value,
            Expression = ve,
            Variable = pe,
            Create = true,
            IsLexical = newScope,
            OwnerFunction = Function
        };
        
        if (variableType == typeof(double))
            v.NumericStorage = pe;

        if (speculativeFlag != null)
        {
            v.SpeculativeNumericStorage = speculativeDouble;
            v.SpeculativeNumericFlag = speculativeFlag;
            v.SpeculativeSlot = pe;
            CompilerSpecializationDiagnostics.RecordSpeculativeNumericLocal();
        }
        v.SetInit(init, initialize);
        variableScopeList[name] = v;
        if (variableType == typeof(JSValue) || variableType == typeof(double))
            CompilerSpecializationDiagnostics.RecordScalarLocal();
        if (variableType == typeof(double))
            CompilerSpecializationDiagnostics.RecordNumericLocal();
        
        return v;
    }

    public VariableScope GetVariable(in StringSpan name, bool createClosure = true)
    {

        var start = this;
        while (start != null)
        {
            if (start.variableScopeList.TryGetValue(name, out var result))
            {
                if (result.OwnerFunction != null && !ReferenceEquals(result.OwnerFunction, Function))
                    RootScope.HasOuterFunctionCaptures = true;
                return result;
            }

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
    public VariableScope CreateEvalShadow(in StringSpan name, BParameterExpression outerVariable, bool outerIsGlobal)
    {
        var pe = BExpression.Parameter(typeof(JSVariable), name.Value + "`evalShadow");
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
