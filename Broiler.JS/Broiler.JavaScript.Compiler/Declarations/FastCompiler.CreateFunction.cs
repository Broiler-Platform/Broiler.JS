using System;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using System.Collections.Generic;
using System.Reflection;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.Ast;
using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Statements;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;
using Broiler.JavaScript.LinqExpressions.LinqExpressions.GeneratorsV2;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Compiler;

partial class FastCompiler
{
    private int parameterInitializerDepth;

    /// <summary>
    /// Item 1-1's remaining half: retains the enclosing scope for each function compiled, so its
    /// body can be re-compiled after this compilation has finished.
    /// </summary>
    /// <remarks>
    /// A wrapper rather than an edit inside <see cref="CreateFunctionCore"/>, which has several
    /// return points — the retention has to see the value each of them produced, and threading a
    /// capture through all of them is how one gets missed. Off by default; with the switch off
    /// this is one boolean test per compiled function.
    /// </remarks>
    private BExpression CreateFunction(AstFunctionExpression functionDeclaration, BExpression super = null, bool createClass = false, string className = null,
        IFastEnumerable<AstClassProperty> memberInits = null, bool forceStrictMode = false, bool hoistStatementDeclaration = true, string inferredFunctionName = null,
        bool createPrototype = true, string[] directEvalPrivateNames = null, IReadOnlyDictionary<AstClassProperty, BExpression> computedMemberNames = null,
        bool thisIsUninitialized = false, BExpression superConstructor = null, IReadOnlyList<PrivateInstanceElement> privateInstanceElements = null,
        IReadOnlyDictionary<AstClassProperty, BExpression> autoAccessorBackingKeys = null)
    {
        if (!DeferredTreeCompilation.Retaining)
        {
            return CreateFunctionCore(functionDeclaration, super, createClass, className, memberInits, forceStrictMode,
                hoistStatementDeclaration, inferredFunctionName, createPrototype, directEvalPrivateNames,
                computedMemberNames, thisIsUninitialized, superConstructor, privateInstanceElements,
                autoAccessorBackingKeys);
        }

        // Taken BEFORE the core runs, because the core pushes this function's own scope and the
        // context has to name the scope the body was written in, not the one it creates.
        var enclosing = scope.Top;
        var eager = CreateFunctionCore(functionDeclaration, super, createClass, className, memberInits, forceStrictMode,
            hoistStatementDeclaration, inferredFunctionName, createPrototype, directEvalPrivateNames,
            computedMemberNames, thisIsUninitialized, superConstructor, privateInstanceElements,
            autoAccessorBackingKeys);

        // Only the plain shape is retained. Everything else — a class member, a derived
        // constructor, a direct-eval compilation, a body with private names or computed member
        // keys — carries context through CreateFunction's own parameters, which a re-entry from a
        // scope alone cannot reproduce. Refusing them is the honest half of "keep the scope
        // alive": the scope is what is kept, so only what the scope determines may be replayed.
        if (super == null
            && !createClass
            && className == null
            && memberInits == null
            && !forceStrictMode
            && hoistStatementDeclaration
            && inferredFunctionName == null
            && createPrototype
            && directEvalPrivateNames == null
            && computedMemberNames == null
            && !thisIsUninitialized
            && superConstructor == null
            && privateInstanceElements == null
            && autoAccessorBackingKeys == null)
        {
            RetainDeferredBody(functionDeclaration, enclosing, eager);
        }

        return eager;
    }

    private BExpression CreateFunctionCore(AstFunctionExpression functionDeclaration, BExpression super = null, bool createClass = false, string className = null,
        IFastEnumerable<AstClassProperty> memberInits = null, bool forceStrictMode = false, bool hoistStatementDeclaration = true, string inferredFunctionName = null,
        bool createPrototype = true, string[] directEvalPrivateNames = null, IReadOnlyDictionary<AstClassProperty, BExpression> computedMemberNames = null,
        bool thisIsUninitialized = false, BExpression superConstructor = null, IReadOnlyList<PrivateInstanceElement> privateInstanceElements = null,
        IReadOnlyDictionary<AstClassProperty, BExpression> autoAccessorBackingKeys = null)
    {
        var node = functionDeclaration;
        var functionLength = GetExpectedArgumentCount(functionDeclaration.Params);

        // get text...

        var previousScope = scope.Top;

        // if this is an arrowFunction then override previous thisExperssion

        var previousThis = scope.Top.ThisExpression;
        if (!functionDeclaration.IsArrowFunction)
            previousThis = null;

        // An arrow function has no new.target of its own; it inherits the enclosing
        // function's captured cell. Ordinary functions capture their own (below).
        var previousNewTarget = functionDeclaration.IsArrowFunction ? scope.Top.NewTargetExpression : null;

        var functionName = functionDeclaration.Id?.Name.Value;

        // var parentScriptInfo = this.scope.Top.ScriptInfo;

        var nodeCode = node.Code;

        var code = StringSpanBuilder.New(ScriptInfoBuilder.Code(scriptInfo), nodeCode.Offset, nodeCode.Length);
        var sList = new Sequence<BExpression>();
        var bodyInits = new Sequence<BExpression>();
        var vList = new Sequence<BParameterExpression>();

        var current = scope.Top.RootScope;
        var cs = scope.Push(new FastFunctionScope(
            pool,
            functionDeclaration,
            previousThis,
            super,
            memberInits: memberInits,
            previous: functionDeclaration.IsArrowFunction ? current : null,
            directEvalPrivateNames: directEvalPrivateNames ?? previousScope.DirectEvalPrivateNames,
        computedMemberNames: computedMemberNames,
        thisIsUninitialized: thisIsUninitialized,
        previousNewTarget: previousNewTarget));
        {
            // The first property-read inline-cache site this function's body may allocate. Read
            // again just before the tiering gate below, the pair bounds the sites this body
            // emitted — which is how a tier-2 recompile re-emits the SAME site indices instead of
            // fresh cold ones, and so can ask item 4-1's feedback what each of them saw
            // (item 4-2b). A bound rather than an inventory: the counter is process-wide, so a
            // concurrent compilation can interleave into the range, and the emitted guard's key
            // compare is what makes that cost a poisoned speculation instead of an answer.
            var firstReadSite = PropertyInlineCacheSite.NextReadSite;
            cs.CanScalarReplaceLocals = TryPlanScalarReplacement(functionDeclaration, out var capturedNames, out var hoistedCapturedNames, out var hasNestedFunction, out var mentionsArguments);
            cs.CapturedByNestedFunctions = capturedNames;
            cs.CapturedByHoistedFunctions = hoistedCapturedNames;
            cs.HasNestedFunctions = hasNestedFunction;
            cs.MentionsArguments = mentionsArguments;
            // Only worth asking when the locals are scalar-replaceable at all: the analysis
            // assumes no closure can capture the binding and no eval/with can rename it.
            if (cs.CanScalarReplaceLocals)
            {
                cs.NumericLocals = NumericLocalAnalysis.Analyze(functionDeclaration, out var numericMisses);
                cs.NumericLocalMisses = numericMisses;
            }

            // Item 3-8a's population, counted rather than estimated. Gated because it costs a
            // second analysis pass per compiled function, which is compile time nothing else
            // needs to pay — and it is a COMPILE-time counter, so whoever turns it on has to do
            // so before the corpus is compiled rather than beside the run-time censuses.
            // Item 3-8a: the names held in two representations. Same gate as the numeric tier —
            // the analysis assumes no closure can capture the binding and no eval/with can rename
            // it — and the switch is read here so the OFF arm allocates nothing extra at all.
            if (cs.CanScalarReplaceLocals && (SpeculativeNumericLocals2.Enabled || ElementReadNumericLocals.Enabled))
                cs.SpeculativeNumericLocals = NumericLocalAnalysis.AnalyzeSpeculative(functionDeclaration);

            if (SpeculativeNumericLocals.Counting && cs.CanScalarReplaceLocals)
                CompilerSpecializationDiagnostics.RecordSpeculativeNumericCandidates(
                    NumericLocalAnalysis.AnalyzeSpeculative(functionDeclaration).Count);

            // Item 3-9's population. The probe answers for the ENCLOSING scope chain and asks
            // about the binding rather than the spelling: `previousScope.GetVariable` resolves the
            // name the way a reference to it would, and `NumericStorage != null` is the question
            // "is this already a raw double" asked of what the compiler actually built — not of
            // what the enclosing analysis merely proved. The two differ, and the difference is the
            // item's own prediction: item 3-7 leaves a name captured by a hoisted function
            // DECLARATION in a JSVariable cell however numeric its analysis found it, which is why
            // 3-9 is not expected to reach NavierStokes.
            // Item 1-1's remaining half: how many function sites could have their body tree built
            // on first call with NO capture mechanism. Asked of the ENCLOSING scope — previousScope
            // rather than cs — because the question is what this body reaches outside itself, and
            // asked before anything of this function is emitted so the answer describes the site
            // rather than the compilation. Compile-time counter, off by default.
            if (DeferrableFunctions.Counting)
                RecordDeferrability(functionDeclaration, previousScope);

            if (ImportedOuterNumerics.Counting && cs.CanScalarReplaceLocals)
                CompilerSpecializationDiagnostics.RecordImportedOuterNumericCandidates(
                    NumericLocalAnalysis.AnalyzeImportedOuter(
                        functionDeclaration,
                        name =>
                        {
                            if (previousScope?.GetVariable(name, true)?.NumericStorage == null)
                                return false;

                            CompilerSpecializationDiagnostics.RecordImportedOuterNumericOffer();
                            return true;
                        }).Count);
            // super() in a derived constructor (or an arrow nested in it) targets the
            // superclass constructor, which differs from the home-object prototype.
            cs.SuperConstructor = superConstructor ?? (functionDeclaration.IsArrowFunction ? previousScope.SuperConstructor : null);
            cs.PrivateInstanceElements = privateInstanceElements;
            cs.AutoAccessorBackingKeys = autoAccessorBackingKeys;
            cs.InParameterInitializer = previousScope.InParameterInitializer;
            var lexicalScopeVar = cs.Context;

            vList.Add(cs.Context);
            vList.Add(cs.StackItem);
            sList.Add(BExpression.Assign(cs.Context, JSContextBuilder.Current));

            FastFunctionScope.VariableScope jsFVarScope = null;

            // BROILER-PATCH: For function declarations, look up name in parent scope
            // to bind the function. For function expressions, the name is local to
            // the function body and must not leak to the parent scope (ES3 §13).
            BParameterExpression fexprNameParam = null;
            if (functionName != null && functionDeclaration.IsStatement && hoistStatementDeclaration)
            {
                jsFVarScope = previousScope.GetVariable(functionName);
                if (isDirectEvalCompilation && !usesDirectEvalLocalVarEnvironment && jsFVarScope != null)
                    jsFVarScope.IsDeletable = true;
            }
            else if (functionName != null && !functionDeclaration.IsStatement)
            {
                // BROILER-PATCH: For named function *expressions* only, create a
                // closure variable in the parent scope that the function body
                // captures, holding the function reference and marked read-only
                // (the name binding of a named function expression is immutable).
                //
                // A function *declaration* compiled at runtime (hoistStatementDeclaration
                // == false, e.g. a block- or switch-scoped declaration inside direct
                // eval) must NOT use this read-only binding: its name is a mutable
                // binding, so the body resolves its own name to the enclosing
                // (mutable) binding instead. Otherwise self-assignment such as
                // `function f(){ f = 1; }` is wrongly ignored.
                fexprNameParam = BExpression.Parameter(typeof(JSVariable), functionName);
                var fexprVarScope = new FastFunctionScope.VariableScope
                {
                    Name = functionName,
                    Expression = JSVariable.ValueExpression(fexprNameParam),
                    // Capture this (immutable) self-name binding for a direct eval or `with`
                    // body so `(function g(){ eval("g") })` resolves `g` instead of throwing
                    // "g is not defined", and a strict `eval("g = 1")` reports the read-only
                    // assignment as a TypeError. It is not a local Variable of this scope (it
                    // is captured read-only), so it is exposed via EvalCaptureExpression only.
                    EvalCaptureExpression = fexprNameParam,
                    Create = false
                };

                cs.AddExternalVariable(functionName, fexprVarScope);
            }

            var s = cs;
            // use this to create variables...
            // var t = s.ThisExpression;
            var args = s.ArgumentsExpression;
            var stackItem = cs.StackItem;
            var r = s.ReturnLabel;

            var inheritedStrictMode = IsStrictMode || forceStrictMode || createClass;
            var isStrictFunction = inheritedStrictMode || HasUseStrictDirective(functionDeclaration.Body);
            ValidateFunctionEarlyErrors(functionDeclaration, isStrictFunction);

            var previousStrictMode = IsStrictMode;
            IsStrictMode = isStrictFunction;

            // The "inside a class field initializer" context is arrow-transparent
            // but function-opaque: an ordinary function nested in a field
            // initializer establishes its own `arguments`/super bindings, so the
            // field-initializer early-error rules must not apply within it.
            var previousInMemberInitializer = inMemberInitializer;
            if (!functionDeclaration.IsArrowFunction)
                inMemberInitializer = false;

            var parameterNames = new List<StringSpan>();
            CollectParameterNames(functionDeclaration.Params, parameterNames);
            var scalarParameterNames = CollectScalarParameterNames(functionDeclaration, cs, parameterNames);
            foreach (var parameterName in parameterNames)
            {
                // A parameter that nothing can reach through a cell is a plain JSValue local,
                // exactly as a scalar-replaced var is. It starts as `undefined` rather than a
                // CLR null because the binding is declared before the argument is bound.
                if (scalarParameterNames.Contains(parameterName.Value))
                    cs.CreateVariable(parameterName, null, true, typeof(JSValue), initialize: true);
                else
                    cs.CreateVariable(parameterName, null, true, initialize: false);
            }

            var directEvalParameterBindings = CollectDirectEvalParameterBindings(functionDeclaration, parameterNames);

            BExpression fxName;
            BExpression localFxName;
            int nameOffset;
            int nameLength;

            if (functionName != null)
            {
                var id = functionDeclaration.Id;

                fxName = StringSpanBuilder.New(ScriptInfoBuilder.Code(scriptInfo), id.Name.Offset, id.Name.Length);
                localFxName = StringSpanBuilder.New(ScriptInfoBuilder.Code(scriptInfo), id.Name.Offset, id.Name.Length);

                nameOffset = id.Name.Offset;
                nameLength = id.Name.Length;
            }
            else if (inferredFunctionName != null)
            {
                fxName = StringSpanBuilder.New(new StringSpan(inferredFunctionName));
                localFxName = StringSpanBuilder.New(new StringSpan(inferredFunctionName));

                nameOffset = 0;
                nameLength = 0;
            }
            else
            {
                fxName = StringSpanBuilder.Empty;
                localFxName = StringSpanBuilder.Empty;

                nameOffset = 0;
                nameLength = 0;
            }

            var point = node.Start.Start;

            // A generator or async body is rewritten into a state machine whose frame is
            // pushed once, at priming, and then held across every suspension — so it keeps
            // its caller's frame pinned out of the recycling pool (see CallStackItem.PinSuspendable).
            sList.Add(BExpression.Assign(stackItem, CallStackItemBuilder.New(cs.Context, scriptInfo, nameOffset, nameLength, point.Line, point.Column,
                suspendable: functionDeclaration.Generator || functionDeclaration.Async)));

            var argumentElements = args;

            // A sloppy function whose parameter list contains a direct eval gets a
            // separate parameter environment (FunctionDeclarationInstantiation step
            // 20): an eval-introduced `var` must shadow same-named outer bindings for
            // the parameter expressions, the body and the closures created in them.
            // While this boundary is active, identifier references that resolve
            // outside this function are routed through EvalShadowVariable bindings.
            var previousEvalShadowBoundary = evalShadowBoundary;
            if (!isStrictFunction && (ParametersContainDirectEval(functionDeclaration) || BodyContainsDirectEval(functionDeclaration)))
            {
                evalShadowBoundary = cs;
                cs.IsEvalShadowBoundary = true;
            }

            var pe = functionDeclaration.Params.GetFastEnumerator();
            while (pe.MoveNext(out var v, out var i))
            {
                if (v.Identifier.IsSpreadElement(out var spe))
                {
                    CreateAssignment(bodyInits, spe.Argument, ArgumentsBuilder.RestFrom(argumentElements, (uint)i), false, true,
                        suppressAnonymousFunctionNameInference: true);
                    continue;
                }

                BExpression parameterInitializer = null;
                if (v.Init != null)
                {
                    var previousDirectEvalParameterBindings = cs.CurrentDirectEvalParameterBindings;
                    var previousInParameterInitializer = cs.InParameterInitializer;
                    cs.CurrentDirectEvalParameterBindings = directEvalParameterBindings;
                    cs.InParameterInitializer = true;
                    parameterInitializerDepth++;
                    try
                    {
                        parameterInitializer = VisitExpression(v.Init);
                    }
                    finally
                    {
                        parameterInitializerDepth--;
                        cs.InParameterInitializer = previousInParameterInitializer;
                        cs.CurrentDirectEvalParameterBindings = previousDirectEvalParameterBindings;
                    }

                    // SingleNameBinding NamedEvaluation (§8.6.3 / IteratorBindingInitialization):
                    // when a simple parameter's default is an anonymous function definition and
                    // that default is actually used, the function adopts the parameter name, e.g.
                    // `(function(p = function(){}) { return p; })().name === "p"`. The default is
                    // evaluated lazily by FromArgumentOptional, so a supplied argument is never
                    // renamed; destructuring targets are named within their own pattern handling.
                    if (v.Identifier is AstIdentifier)
                        parameterInitializer = PrepareDestructuringInitializer(v.Identifier, v.Init, parameterInitializer);
                }

                CreateAssignment(bodyInits, v.Identifier, JSVariableBuilder.FromArgumentOptional(argumentElements, i, parameterInitializer), false, true,
                    suppressAnonymousFunctionNameInference: true);
            }

            // Annex B 3.3 (sloppy mode): hand block-nested function declaration names
            // to the body block so it creates the function-scope var bindings at
            // entry (initialized to undefined). Reads before the declaration then
            // resolve, and the declaration site assigns them via
            // AppendAnnexBOuterBindingAssignments. Set right before visiting the body
            // so parameter initializers cannot consume it first.
            pendingAnnexBFunctionNames = isStrictFunction
                ? null
                : (functionDeclaration.Body as AstBlock)?.AnnexBFunctionNames;

            // A function whose body (or parameter list) contains a direct eval must
            // materialize its `arguments` binding eagerly — the eval body resolves an
            // unqualified `arguments` through the runtime scope chain to this function's
            // activation, and if the function never directly referenced `arguments` itself
            // there would be no binding for the eval to find. (Arrow functions inherit
            // `arguments` from the enclosing function and have none of their own, so they
            // are excluded.)
            var containsEval = !functionDeclaration.IsArrowFunction
                && (ParametersContainDirectEval(functionDeclaration) || BodyContainsDirectEval(functionDeclaration));
            if (containsEval)
                MaterializeArgumentsBinding();

            BExpression lambdaBody;
            using (completionScopes.Push(null))
                lambdaBody = VisitStatement(functionDeclaration.Body);

            evalShadowBoundary = previousEvalShadowBoundary;

            // Emit only the binding-construction half here. The hoisted FunctionDeclaration
            // value assignments (PostInit) are deferred until after the formal parameters are
            // bound (bodyInits), so a body `function x(){}` whose name matches a parameter
            // overrides that parameter rather than being clobbered by it
            // (FunctionDeclarationInstantiation; test262 language/function-code/S10.2.1_A4_T1).
            sList.AddRange(s.InitOnlyList);

            // Register the parameter-environment shadow bindings into this function's
            // CallStackItem AFTER they are constructed (InitList) but BEFORE the
            // parameter initializers run (bodyInits), so a direct eval in the
            // parameter list resolves its introduced `var` to the shared shadow
            // binding (via TryResolveDirectEvalBinding in JSContext.Register).
            foreach (var evalShadow in cs.EvalShadows)
                sList.Add(EvalShadowBuilder.Register(cs.Context, stackItem, evalShadow.Variable));

            // Base class (§10.2.2 [[Construct]]): InitializeInstanceElements runs after
            // OrdinaryCallBindThis but BEFORE OrdinaryCallEvaluateBody, so instance fields
            // are initialized before the parameter initializers (bodyInits) evaluate. A
            // parameter default such as `constructor(o = this.#x)` can therefore observe an
            // already-initialized field. (A derived class initializes its fields only after
            // super() — handled by the thisIsUninitialized path below.)
            if (s.MemberInits != null && !thisIsUninitialized)
                InitMembers(sList, s);

            sList.AddRange(bodyInits);

            // Hoisted FunctionDeclaration assignments run after parameter binding (see
            // InitOnlyList above), so they take precedence over a same-named parameter.
            sList.AddRange(s.PostInitList);

            if (functionDeclaration.Generator)
                sList.Add(BExpression.Yield(JSUndefinedBuilder.Value));

            sList.Add(lambdaBody);

            if (thisIsUninitialized && s.MemberInits != null)
                InitMembers(sList, s);

            // Collect the lambda's locals AFTER InitMembers: a member (field)
            // initializer can allocate fresh temporaries while it is compiled (e.g.
            // the by-address KeyString temp a private-method call needs). Snapshotting
            // VariableParameters before InitMembers would omit those temps, leaving the
            // IL generator unable to resolve them (KeyNotFoundException at emit time).
            vList.AddRange(s.VariableParameters);

            sList.Add(BExpression.Label(r, JSUndefinedBuilder.Value));

            // A class constructor body produces a value via the return label
            // (explicit `return x`, or `undefined` on fall-through). Apply the
            // [[Construct]] return-value semantics: object returns pass through,
            // a base constructor otherwise yields `this`, and a derived
            // constructor throws (TypeError for a non-undefined non-object,
            // ReferenceError when `this` is still uninitialized).
            BExpression bodyExpression = BExpression.Block(sList);
            if (createClass)
                bodyExpression = JSFunctionBuilder.NormalizeConstructorReturn(
                    bodyExpression, s.ThisExpression, thisIsUninitialized);

            var block = BExpression.Block(
                vList,
                BExpression.TryFinally(
                    bodyExpression,
                    JSContextStackBuilder.Pop(stackItem, cs.Context)));

            // adding lexical scope pending...

            functionName = functionName ?? inferredFunctionName ?? "inline";

            static BExpression ToDelegate(BLambdaExpression e1) => e1;

            var scriptFunctionName = new FunctionName(functionName, location, point.Line, point.Column);

            BLambdaExpression lambda;
            BExpression jsf;

            if (functionDeclaration.Generator)
            {
                lambda = GeneratorRewriter.Rewrite(in scriptFunctionName, block, cs.ReturnLabel, cs.Generator, replaceArgs: cs.Arguments, replaceStackItem: cs.StackItem,
                    replaceContext: cs.Context, replaceScriptInfo: scriptInfo);

                jsf = JSGeneratorFunctionBuilderV2.New(lambda, fxName, code, functionLength, functionDeclaration.Async, primeOnInvoke: true, coerceThis: !isStrictFunction);
            }
            else if (functionDeclaration.Async)
            {

                lambda = GeneratorRewriter.Rewrite(in scriptFunctionName, block, cs.ReturnLabel, cs.Generator, replaceArgs: cs.Arguments, replaceStackItem: cs.StackItem,
                    replaceContext: cs.Context, replaceScriptInfo: scriptInfo);
                // Pre-rewrite only the async function's own body. Nested lambdas keep
                // their raw outer-variable references so the later full top-down
                // rewrite can thread (and box) those captures with the complete scope
                // chain — descending here would strand them (KeyNotFound / type
                // mismatch at IL-gen). See LambdaRewriter.rewriteNestedLambdas.
                ExpressionCompiler.LambdaRewriter.RewriteRootOnly(lambda);

                jsf = JSAsyncFunctionBuilder.Create(JSGeneratorFunctionBuilderV2.New(lambda, fxName, code, functionLength));
            }
            else
            {
                lambda = BExpression.Lambda(typeof(JSFunctionDelegate), block, in scriptFunctionName, [cs.Arguments]);
                // Item 1-1's remaining half. Record what this body captures, derived from source
                // alone the way a DEFERRED body would have to derive it — before the rewrite that
                // decides the real Box[] layout from the tree has run. RuntimeMethodBuilder.Relay
                // compares the two.
                if (ExpressionCompiler.DeferredCaptureLayout.Checking)
                {
                    // A body that can reach bindings its text never names has no layout to be
                    // right about, and the mechanism refuses to defer it — so it is EXCLUDED
                    // rather than given an empty prediction, which would report every one of its
                    // captures as a miss.
                    if (FreeNameScan.Of(functionDeclaration).Dynamic)
                        ExpressionCompiler.DeferredCaptureLayout.Undeferrable(lambda);
                    else
                        ExpressionCompiler.DeferredCaptureLayout.Predict(
                            lambda,
                            PredictCaptures(functionDeclaration, previousScope, cs));
                }
                jsf = JSFunctionBuilder.New(ToDelegate(lambda), fxName, code, functionLength, createPrototype: createPrototype && !functionDeclaration.IsArrowFunction);
                if (!isStrictFunction)
                    jsf = JSFunctionBuilder.EnableNonStrictThis(jsf);
                else
                    jsf = JSFunctionBuilder.EnableStrictMode(jsf);

                // Only ordinary FunctionDeclaration/FunctionExpression objects expose
                // the legacy `caller`/`arguments` own data properties (Annex B). Concise
                // methods, getters and setters do not — they are compiled with
                // createPrototype:false, which distinguishes them here from real functions.
                if (!isStrictFunction && !functionDeclaration.IsArrowFunction && createPrototype)
                    jsf = JSFunctionBuilder.EnableLegacyCallerAndArguments(jsf);

                // A function created inside a `with` block must capture the current
                // with-scope chain so it stays resolvable when the function is later
                // invoked with the dynamic with-scopes suspended (script-host / PTC
                // mode). This holds even when the `with` is part of directly-eval'd
                // code — e.g. `eval("with (o) { (function () { ... })(); }")` — so the
                // capture must not be skipped for direct-eval compilation.
                if (withBoundaries.Count > 0)
                    jsf = JSFunctionBuilder.CaptureWithScopes(jsf);

                // RECOMPILE CONTRACT (item 4-2a): a promoted function is recompiled from its
                // source text into a SECOND function object, so a body that can observe its own
                // function object observes the copy. Asked here, at the decision point, rather
                // than left to the conjunction below — see TieringRecompileContract.
                if (createPrototype
                    && !createClass
                    && !functionDeclaration.IsArrowFunction
                    && !isDirectEvalCompilation
                    && cs.CanScalarReplaceLocals
                    && !cs.HasNestedFunctions
                    && !cs.HasOuterFunctionCaptures
                    && withBoundaries.Count == 0
                    && TieringRecompileContract.Admits(functionDeclaration, mentionsArguments))
                {
                    jsf = JSFunctionBuilder.EnableTiering(
                        jsf,
                        NumericLoopPlanner.TryCreate(functionDeclaration),
                        location,
                        firstReadSite,
                        PropertyInlineCacheSite.NextReadSite);
                }
            }

            IsStrictMode = previousStrictMode;
            inMemberInitializer = previousInMemberInitializer;

            cs.Dispose();

            if (jsFVarScope != null)
            {
                // Only a function declaration at the TOP LEVEL of the program/eval
                // body creates a global binding (GlobalDeclarationInstantiation /
                // CreateGlobalFunctionBinding). A declaration nested inside a block
                // is block-scoped: in sloppy mode its extra var-scoped binding is
                // created via Annex B (AppendAnnexBOuterBindingAssignments); in
                // strict mode it must not escape the block at all. The program body
                // sits one level below RootScope, so "top level" is parent == root.
                var isProgramTopLevel = previousScope.Parent == previousScope.RootScope;
                // A strict-mode eval (direct or indirect) has its own variable
                // environment, so a top-level function declaration must NOT be
                // instantiated on the global object (CreateGlobalFunctionBinding is
                // only reached when varEnv is the global environment). It binds in
                // the eval's own scope instead and is gone once the eval returns;
                // otherwise `(0,eval)("'use strict'; function f(){}")` leaked `f`
                // to the global object.
                var isStrictEvalProgram = isDirectEvalCompilation && IsStrictMode;
                // A direct eval whose variable environment is a function activation (sloppy
                // eval inside a function) must NOT instantiate a top-level function
                // declaration on the global object: EvalDeclarationInstantiation creates the
                // binding in that function's variable environment, where it lives only for the
                // life of the activation. CreateGlobalFunctionBinding is reached only when
                // varEnv is the global environment (a global-scoped or strict-eval program).
                // Otherwise `function h(){ var f; eval("function f(){}"); }` leaked `f` to the
                // global object even though it should merely update h's own `f`.
                var isLocalVarEnvEval = isDirectEvalCompilation && usesDirectEvalLocalVarEnvironment;
                if (previousScope.Function == null && isProgramTopLevel && !isStrictEvalProgram && !isLocalVarEnvEval)
                    jsFVarScope.SetPostInit(JSContextBuilder.DeclareGlobalFunction(KeyOfName(functionName), jsf));
                else
                    jsFVarScope.SetPostInit(jsf);

                // A top-level FunctionDeclaration in a function-local direct eval is
                // instantiated once at eval entry: the program hoisting seeds the
                // binding (GetOrCreateDirectEvalLocalBinding, registered on the calling
                // frame) and the PostInit above assigns its function value before any
                // statement runs. The textual declaration statement is therefore a
                // runtime no-op and must NOT re-read the binding (jsFVarScope.Expression)
                // — the binding is deletable, so a `delete f` executed before the textual
                // site would make that read throw "f is not defined". The matching
                // Annex B var copy-out in VisitExpressionStatement is likewise skipped.
                // (test262 language/eval-code/direct/var-env-func-init-local-new-delete)
                if (IsTopLevelLocalVarEnvEvalScope(previousScope))
                    return JSUndefinedBuilder.Value;

                return jsFVarScope.Expression;
            }

            // BROILER-PATCH: For function expressions with a name, wrap the result
            // in a block that creates a read-only closure variable holding the
            // function reference. The function body captures this variable.
            if (fexprNameParam != null)
            {
                var fexprVars = new Sequence<BParameterExpression> { fexprNameParam };

                // Mark the name binding read-only. In a strict-mode function an
                // assignment to it must throw a TypeError; the default read-only
                // write only throws when the engine's runtime strict-mode flag is
                // active, but a generator / async body resumes as a state machine
                // outside that scope, so the throw would be lost. Baking the
                // strictness in (throwOnWrite) makes the TypeError independent of
                // the runtime flag.
                return BExpression.Block(fexprVars,
                    BExpression.Assign(fexprNameParam, JSVariableBuilder.New(jsf, functionName)),
                    JSVariableBuilder.SetReadOnly(fexprNameParam, throwOnWrite: isStrictFunction),
                    JSVariable.ValueExpression(fexprNameParam));
            }

            return jsf;
        }
    }

    /// <summary>
    /// Classifies one function site for item 1-1's remaining half: could its body's expression
    /// tree be built on first invocation instead of now, with no capture mechanism?
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The whole content of this is the resolution step</b>, and it is why the existing scanners
    /// cannot answer the question. <see cref="FreeNameScan"/> gives the names the body references
    /// and does not bind; whether each of them <em>costs</em> anything depends on where it
    /// resolves, and only the enclosing compiler scope knows that. A name that resolves to nothing
    /// this compilation holds is looked up dynamically at run time, from a body compiled whenever
    /// — so <c>function () { return jQuery; }</c> at program top level is free, and the identical
    /// text inside an IIFE that declares <c>jQuery</c> needs a box whose index only
    /// <c>LambdaRewriter</c> knows.
    /// </para>
    /// <para>
    /// <b>Over-counting the refusals is the safe direction and is what this does.</b> A name is
    /// charged as a capture the moment the chain holds a binding for it, including a program-level
    /// one — which may or may not turn out to need a box — so <see cref="DeferralRefusal.CaptureFree"/>
    /// is a lower bound on the population and never an over-claim. The
    /// <c>functionOwned</c> count is carried separately precisely so the two can be told apart
    /// afterwards rather than argued about now.
    /// </para>
    /// </remarks>
    private static void RecordDeferrability(AstFunctionExpression function, FastFunctionScope enclosing)
    {
        var scan = FreeNameScan.Of(function);
        if (scan.Dynamic)
        {
            CompilerSpecializationDiagnostics.RecordDeferralDecision(DeferralRefusal.Dynamic, scan.Free.Count, 0, 0, 0);
            return;
        }

        var bound = 0;
        var functionOwned = 0;
        var cellBacked = 0;
        foreach (var name in scan.Free)
        {
            if (enclosing == null || !enclosing.TryResolveBinding(name, out var binding))
                continue;

            bound++;
            if (binding.OwnerFunction != null)
                functionOwned++;

            // The count the item actually turns on. A binding with no CLR local of its own is not
            // held in an enclosing lambda's frame, so no Box[] entry can refer to it and the body
            // resolves it the same way whenever it is compiled — a script's top-level `var` and
            // function declarations are properties of the global object, which is what makes
            // Mandreel's 1 364 top-level declarations a different case from jQuery's IIFE.
            if (binding.Variable != null)
                cellBacked++;
        }

        CompilerSpecializationDiagnostics.RecordDeferralDecision(
            cellBacked == 0 ? DeferralRefusal.CaptureFree : DeferralRefusal.CapturesEnclosingBinding,
            scan.Free.Count,
            bound,
            functionOwned,
            cellBacked);
    }

    /// <summary>
    /// The bindings a deferred body would have to be handed boxes for, computed from source alone
    /// (docs/performance-roadmap.md item 1-1's remaining half).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The same two steps the deferral itself would run</b>, and no others:
    /// <see cref="FreeNameScan"/> for the names the body references and does not bind, then the
    /// enclosing scope for where each resolves. A name that resolves to nothing this compilation
    /// holds is looked up dynamically at run time and needs no box; a name whose binding has no CLR
    /// local of its own has no box to point at. What is left is exactly the set an addressable
    /// <c>Box[]</c> has to cover.
    /// </para>
    /// <para>
    /// <b>A <see cref="FreeNameScan.Dynamic"/> body predicts nothing and is not counted</b>, because
    /// the mechanism refuses to defer it — a direct <c>eval</c>, a <c>with</c> or a <c>debugger</c>
    /// can reach a binding the text never names, so there is no set to be right about.
    /// </para>
    /// </remarks>
    private static List<BParameterExpression> PredictCaptures(
        AstFunctionExpression function,
        FastFunctionScope enclosing,
        FastFunctionScope own)
    {
        // **Ordered, because the thing being predicted is an index.** `0104` predicted a set and
        // checked membership; a captured name's Box[] slot is the order the closure rewrite's walk
        // first encounters it, so a set cannot answer the item's actual obstacle. First-mention
        // order is the only order source alone offers, and whether it is the walk's order is what
        // the checker now asks rather than what this assumes.
        var captures = new List<BParameterExpression>();
        var seen = new HashSet<BParameterExpression>(ExpressionCompiler.Core.ReferenceEqualityComparer.Instance);
        if (enclosing == null)
            return captures;

        var scan = FreeNameScan.Of(function);
        if (scan.Dynamic)
            return captures;

        foreach (var name in scan.FreeInOrder)
        {
            if (!enclosing.TryResolveBinding(name, out var binding))
            {
                ExpressionCompiler.DeferredCaptureLayout.NoteUnresolved(name);
                continue;
            }

            // Variable OR EvalCaptureExpression, which is the same disjunction VariableScope's
            // own CaptureExpression makes. A binding can be captured without being a local of the
            // scope that holds it — a named function expression's self-name is the case that
            // matters, and testing Variable alone silently drops it.
            var cell = binding.Variable ?? binding.EvalCaptureExpression as BParameterExpression;
            if (cell == null)
            {
                ExpressionCompiler.DeferredCaptureLayout.NoteNoCell(name);
                continue;
            }

            if (seen.Add(cell))
                captures.Add(cell);
        }

        // **A named function EXPRESSION's own name, which is not free and is still handed in.**
        // The specification binds it inside the function, so FreeNameScan is right to leave it out
        // of the free set — and this engine gives it a JSVariable parameter in the ENCLOSING scope
        // that the body captures, so the layout has to carry it anyway. The binding is registered
        // on this function's own scope with a deliberately null Variable, exposed through
        // EvalCaptureExpression alone, which is what the first attempt at this missed.
        //
        // Gated on SelfNameReferenced rather than on "has a name", because adding it
        // unconditionally over-approximated 126 sites — a named function expression that never
        // mentions its own name is handed no cell for it. FreeNameScan gives the self-name its own
        // scope precisely so that a reference reaching it can be told from one reaching a
        // parameter of the same spelling.
        //
        // Item 0097's rule, deciding a mechanism rather than a measurement for the first time:
        // ask what the compiler BUILT, not what the analysis PROVED.
        if (own != null
            && scan.SelfNameReferenced
            && function.Id != null
            && own.TryGetOwnVariable(function.Id.Name.Value, out var self)
            && (self.Variable ?? self.EvalCaptureExpression as BParameterExpression) is { } selfCell)
        {
            if (seen.Add(selfCell))
                captures.Add(selfCell);
        }

        return captures;
    }

    private static int GetExpectedArgumentCount(IFastEnumerable<VariableDeclarator> parameters)
    {
        var count = 0;
        var e = parameters.GetFastEnumerator();

        while (e.MoveNext(out var parameter))
        {
            if (parameter.Identifier.IsSpreadElement(out _)
                || parameter.Init != null)
                break;

            count++;
        }

        return count;
    }

    private void InitMembers(Sequence<BExpression> sList, FastFunctionScope s)
    {
        var @this = s.ThisExpression;

        // InitializeInstanceElements installs the class's private methods/accessors
        // (the shared function objects created once at class evaluation) onto the
        // instance BEFORE any field initializer runs, so a field initializer can
        // call them. Each install also establishes the per-instance private brand.
        if (s.PrivateInstanceElements != null)
        {
            foreach (var element in s.PrivateInstanceElements)
            {
                if (element.Method != null)
                {
                    sList.Add(JSObjectBuilder.PrivateMethodAdd(@this, element.Key, element.Method));
                }
                else
                {
                    sList.Add(JSObjectBuilder.PrivateAccessorAdd(@this, element.Key,
                        element.Getter ?? BExpression.Constant(null, typeof(JSValue)),
                        element.Setter ?? BExpression.Constant(null, typeof(JSValue))));
                }
            }
        }

        // `super()` nested in an arrow compiles its member-initialization against the arrow's
        // scope (`scope.Top`), whose MemberInits is null — the constructor's inits live on the
        // root scope. Guard the null so compiling such a `super()` does not NullReference; the
        // instance fields are still installed for the common (non-arrow) constructor path.
        if (s.MemberInits == null)
            return;

        var en = s.MemberInits.GetFastEnumerator();

        while (en.MoveNext(out var member))
        {
            var name = s.ComputedMemberNames != null && s.ComputedMemberNames.TryGetValue(member, out var computedName)
                ? computedName
                : GetClassElementName(member);
            BExpression value;
            if (member.Init == null)
            {
                value = JSUndefinedBuilder.Value;
            }
            else
            {
                var previousInMemberInitializer = inMemberInitializer;
                inMemberInitializer = true;
                try
                {
                    value = ApplyFieldFunctionName(member, name, Visit(member.Init));
                }
                finally
                {
                    inMemberInitializer = previousInMemberInitializer;
                }
            }
            // A public auto-accessor (`accessor x = v`) stores its value in a private
            // backing field; its getter/setter pair already sit on the prototype. The
            // backing field is installed via PrivateFieldAdd under the minted key, NOT
            // as a public data property.
            BElementInit init;
            if (s.AutoAccessorBackingKeys != null && s.AutoAccessorBackingKeys.TryGetValue(member, out var backingKey))
            {
                init = JSObjectBuilder.PrivateFieldAdd(backingKey, value);
            }
            else
            {
                // A public field is CreateDataPropertyOrThrow — observable through a
                // Proxy receiver's defineProperty trap (a `return`-override base may
                // hand back a Proxy as `this`). A private field is an internal slot
                // added directly via PrivateFieldAdd (never consults proxy traps), which
                // also enforces the TypeError on a non-extensible target or a re-added
                // private name.
                init = member.IsPrivate
                    ? JSObjectBuilder.PrivateFieldAdd(name, value)
                    : JSObjectBuilder.CreateDataProperty(name, value);
            }

            sList.Add(BExpression.Call(@this, init.Member as MethodInfo, init.Arguments));
        }
    }

    private static string[] CollectDirectEvalParameterBindings(AstFunctionExpression functionDeclaration, List<StringSpan> parameterNames)
    {
        var bindings = new HashSet<string>(StringComparer.Ordinal);
        foreach (var parameterName in parameterNames)
            bindings.Add(parameterName.Value);

        if (functionDeclaration.Body is not AstBlock body)
            return [.. bindings];

        if (!functionDeclaration.IsArrowFunction && body.HoistingScope != null)
        {
            var hoistedNames = body.HoistingScope.GetFastEnumerator();
            while (hoistedNames.MoveNext(out var hoistedName))
            {
                if (hoistedName.Equals("arguments") || hoistedName.Equals("eval"))
                    bindings.Add(hoistedName.Value);
            }
        }

        if (!functionDeclaration.IsArrowFunction)
        {
            foreach (var lexicalBinding in CollectTopLevelLexicalBindings(body.Statements))
            {
                if (lexicalBinding is "arguments" or "eval")
                    bindings.Add(lexicalBinding);
            }
        }

        return [.. bindings];
    }

    private static readonly IReadOnlySet<string> NoCapturedNames = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>
    /// The parameters of <paramref name="functionDeclaration"/> that can be held in a plain
    /// <c>JSValue</c> local instead of a per-call <see cref="JSVariable"/> cell
    /// (docs/performance-roadmap.md item 3-3).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every parameter allocated a cell, unconditionally, while a <c>var</c> in the same
    /// function has been scalar-replaced since P2-2. That is the whole gap: a cell exists so
    /// something else can reach the binding — a closure, a mapped <c>arguments</c> object, a
    /// direct <c>eval</c> — and a parameter no more always needs one than a <c>var</c> does.
    /// Measured at 56 bytes per parameter per call: a three-parameter helper called in a loop
    /// went from 230.2 to 62.2 bytes a call, so the cells were 168 of the 230.
    /// </para>
    /// <para>
    /// This is the SCALAR tier of the eligibility gate, not the numeric one, and the difference
    /// is not a matter of effort: a <c>var</c> can be proved to hold only numbers by reading the
    /// function, while a parameter's value is the caller's choice and no analysis of the callee
    /// can constrain it. Holding a parameter in a raw <c>double</c> needs an entry guard and a
    /// generic fallback, which is speculation — phase 4, not this item.
    /// </para>
    /// <para>
    /// Four conditions, and the simple-parameter-list one is doing more work than it looks.
    /// It rules out defaults, rest and destructuring, and with them every expression in the
    /// parameter list — so the only closures that can capture a parameter are in the body,
    /// which is exactly the set <see cref="FastFunctionScope.CapturedByNestedFunctions"/>
    /// already collects. Without it a closure in a default (<c>function f(a, b = () =&gt; a)</c>)
    /// would capture a scalarized parameter, because the hazard detector scans the body and
    /// never the parameter list. That is a bound, not a heuristic, and it is what lets this
    /// reuse the existing analysis rather than extend it.
    /// </para>
    /// </remarks>
    private static IReadOnlySet<string> CollectScalarParameterNames(
        AstFunctionExpression functionDeclaration,
        FastFunctionScope cs,
        List<StringSpan> parameterNames)
    {
        // The same hazards that stop a var being scalar-replaced stop a parameter: a direct
        // eval or a `with` can name a binding no scan can predict.
        if (!cs.CanScalarReplaceLocals)
            return NoCapturedNames;

        // A sloppy function with a simple parameter list gets a MAPPED arguments object, and
        // the mapping is built out of the parameters' JSVariable cells
        // (MaterializeArgumentsBinding). Any mention at all is refused rather than only the
        // mapped case, because `arguments` is materialized lazily on first reference — long
        // after the parameters have been created here.
        if (cs.MentionsArguments)
            return NoCapturedNames;

        if (!HasSimpleParameterList(functionDeclaration.Params))
            return NoCapturedNames;

        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var parameterName in parameterNames)
        {
            var name = parameterName.Value;

            // Capturing a binding requires naming it, so a name any nested function mentions
            // keeps its cell — the same rule, and the same set, that VisitBlock applies to vars.
            if (name is "arguments" or "eval" || cs.CapturedByNestedFunctions.Contains(name))
                continue;

            names.Add(name);
        }

        return names;
    }

    /// <summary>
    /// Decides whether this function's <c>var</c>s may live in raw locals, and which of its
    /// names must not because a nested function could capture them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The first classification disqualified any function containing a nested function, on the
    /// grounds that a closure needs a cell to capture. That is true but far too broad: it is not
    /// "this function has a closure" that matters, it is "this closure names this binding". The
    /// difference is most of the ordinary code there is — a counted loop in a function that also
    /// defines a helper was paying ~97 bytes an iteration to box its counter, against 0.8 for
    /// the same loop with the helper deleted, and it made no difference whether the helper was a
    /// declaration, an expression or an arrow; whether it was ever referenced; or whether it was
    /// written before the loop or after it (docs/performance-roadmap.md §6.6).
    /// </para>
    /// <para>
    /// So a nested function is scanned rather than treated as a wall, and every name it mentions
    /// is excluded. That is still conservative — it cannot tell a capture of the outer
    /// <c>i</c> from its own parameter <c>i</c>, or from a property named <c>i</c> — but it is
    /// sound in the direction that matters, because capturing a binding requires naming it.
    /// A nested function that could reach a name it does not mention — one containing a direct
    /// eval, a <c>with</c>, or a <c>debugger</c> — disqualifies the whole function as before.
    /// </para>
    /// </remarks>
    private static bool TryPlanScalarReplacement(AstFunctionExpression functionDeclaration,
        out IReadOnlySet<string> capturedNames, out IReadOnlySet<string> hoistedCapturedNames,
        out bool hasNestedFunction, out bool mentionsArguments)
    {
        capturedNames = NoCapturedNames;
        hoistedCapturedNames = NoCapturedNames;
        hasNestedFunction = false;
        // Conservative default: the paths below that return without running the detector have
        // not looked, and "did not look" must read as "may mention it".
        mentionsArguments = true;

        if (functionDeclaration.Async
            || functionDeclaration.Generator
            || ParametersContainDirectEval(functionDeclaration)
            || BodyContainsDirectEval(functionDeclaration))
        {
            return false;
        }

        var detector = new ScalarReplacementHazardDetector();
        detector.Visit(functionDeclaration.Body);
        hasNestedFunction = detector.HasNestedFunction;
        if (detector.Found)
            return false;

        capturedNames = detector.Captured;
        hoistedCapturedNames = CollectHoistedFunctionCaptures(functionDeclaration.Body);
        // A nested function's mention lands in Captured rather than here, because the detector
        // scans nested functions instead of descending into them.
        mentionsArguments = detector.MentionsArguments || detector.Captured.Contains("arguments");
        return true;
    }

    /// <summary>
    /// The names mentioned by a function declaration at <paramref name="body"/>'s own top level,
    /// whose function object therefore exists from function entry
    /// (docs/performance-roadmap.md item 3-7).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Only the body's own statement list is examined, and that is the whole of the hazard rather
    /// than a first approximation. A declaration one level down — in a block, an <c>if</c>, a
    /// loop, a <c>try</c>, a <c>switch</c> — has its BINDING hoisted (Annex B B.3.3.1 in sloppy
    /// mode) but not its VALUE: the function object is created when the block's declaration is
    /// reached, so calling the name earlier is a TypeError on <c>undefined</c> and not a read of
    /// the captured binding. A declaration at body top level is the only one
    /// FunctionDeclarationInstantiation initializes before the body runs.
    /// </para>
    /// <para>
    /// A declaration textually BEFORE the numeric declaration is already refused by the
    /// analysis — its mention of the name precedes the name's own declaration, which
    /// <c>NumericLocalAnalysis</c> rejects outright — so collecting both directions costs
    /// nothing and needs no position comparison here.
    /// </para>
    /// <para>
    /// Labels are unwrapped for the same reason
    /// <see cref="CollectBlockFunctionDeclarationNames"/> unwraps them: <c>l: function g(){}</c>
    /// is an Annex B labelled function declaration and hoists exactly like an unlabelled one.
    /// </para>
    /// </remarks>
    private static IReadOnlySet<string> CollectHoistedFunctionCaptures(AstNode body)
    {
        if (body is not AstBlock block)
            return NoCapturedNames;

        HashSet<string> captured = null;
        var statements = block.Statements.GetFastEnumerator();
        while (statements.MoveNext(out var statement))
        {
            var current = statement;
            while (current is AstLabeledStatement labeled)
                current = labeled.Body;

            if (current is not AstExpressionStatement
                {
                    Expression: AstFunctionExpression { IsStatement: true } declaration
                })
            {
                continue;
            }

            captured ??= new HashSet<string>(StringComparer.Ordinal);
            new NestedFunctionScanner(captured).Visit(declaration);
        }

        return (IReadOnlySet<string>)captured ?? NoCapturedNames;
    }

    // Hazards that poison the whole function: a with-environment, debugger visibility, or a
    // nested function that can reach names it does not mention. Names a nested function merely
    // mentions are collected instead, and excluded one at a time.
    private sealed class ScalarReplacementHazardDetector : Broiler.JavaScript.Ast.AstReduce
    {
        public bool Found { get; private set; }

        public bool HasNestedFunction { get; private set; }

        /// <summary>
        /// Whether this function's own body names <c>arguments</c>. A parameter that an
        /// <c>arguments</c> object may map has to keep its cell — see
        /// <see cref="CollectScalarParameterNames"/>.
        /// </summary>
        public bool MentionsArguments { get; private set; }

        public readonly HashSet<string> Captured = new(StringComparer.Ordinal);

        protected override AstNode VisitIdentifier(AstIdentifier identifier)
        {
            if (identifier.Name.Equals("arguments"))
                MentionsArguments = true;
            return identifier;
        }

        // AstReduce deliberately treats these compact structs as leaves because most
        // rewriting visitors handle them explicitly. Scalar eligibility must inspect
        // their children, otherwise a closure hidden in a variable initializer,
        // destructuring default, or switch clause can capture a raw local.
        protected override VariableDeclarator VisitVariableDeclarator(VariableDeclarator declarator)
        {
            Visit(declarator.Identifier);
            if (declarator.Init != null)
                Visit(declarator.Init);
            return declarator;
        }

        protected override ObjectProperty VisitObjectProperty(ObjectProperty property)
        {
            if (property.Key != null)
                Visit(property.Key);
            if (property.Value != null)
                Visit(property.Value);
            if (property.Init != null)
                Visit(property.Init);
            return property;
        }

        protected override Case VisitCase(Case @case)
        {
            if (@case.Test != null)
                Visit(@case.Test);
            var statements = @case.Statements.GetFastEnumerator();
            while (statements.MoveNext(out var statement))
                Visit(statement);
            return @case;
        }

        protected override AstNode VisitFunctionExpression(AstFunctionExpression functionExpression)
        {
            HasNestedFunction = true;

            // Scanned, not refused. Everything it names is off limits; if it can reach a name
            // it does not name, the enclosing function is disqualified outright.
            var scanner = new NestedFunctionScanner(Captured);
            scanner.Visit(functionExpression);
            if (scanner.Dynamic)
                Found = true;

            return functionExpression;
        }

        protected override AstNode VisitWithStatement(AstWithStatement withStatement)
        {
            Found = true;
            return withStatement;
        }

        protected override AstNode VisitDebuggerStatement(AstDebuggerStatement debuggerStatement)
        {
            Found = true;
            return debuggerStatement;
        }
    }

    /// <summary>
    /// Collects every name mentioned inside a nested function — including inside functions
    /// nested within it, whose captures reach through — and reports whether it can resolve a
    /// name that does not appear in its text.
    /// </summary>
    private sealed class NestedFunctionScanner(HashSet<string> captured) : Broiler.JavaScript.Ast.AstReduce
    {
        /// <summary>
        /// The nested function can reach a binding it never names: a direct eval resolves
        /// arbitrary identifiers at run time, a <c>with</c> resolves them against an object, and
        /// <c>debugger</c> exposes the whole scope chain.
        /// </summary>
        public bool Dynamic { get; private set; }

        // Same containers AstReduce leaves to specialized rewriters. Missing one here is not a
        // missed optimization but a miscompile: a capture hidden in a variable initializer, a
        // destructuring default or a switch clause would leave the captured local scalarized.
        protected override VariableDeclarator VisitVariableDeclarator(VariableDeclarator declarator)
        {
            Visit(declarator.Identifier);
            if (declarator.Init != null)
                Visit(declarator.Init);
            return declarator;
        }

        protected override ObjectProperty VisitObjectProperty(ObjectProperty property)
        {
            if (property.Key != null)
                Visit(property.Key);
            if (property.Value != null)
                Visit(property.Value);
            if (property.Init != null)
                Visit(property.Init);
            return property;
        }

        protected override Case VisitCase(Case @case)
        {
            if (@case.Test != null)
                Visit(@case.Test);
            var statements = @case.Statements.GetFastEnumerator();
            while (statements.MoveNext(out var statement))
                Visit(statement);
            return @case;
        }

        protected override AstNode VisitIdentifier(AstIdentifier identifier)
        {
            captured.Add(identifier.Name.Value);
            return identifier;
        }

        protected override AstNode VisitCallExpression(AstCallExpression callExpression)
        {
            if (callExpression.Callee is AstIdentifier callee && callee.Name.Equals("eval"))
                Dynamic = true;

            return base.VisitCallExpression(callExpression);
        }

        protected override AstNode VisitWithStatement(AstWithStatement withStatement)
        {
            Dynamic = true;
            return base.VisitWithStatement(withStatement);
        }

        protected override AstNode VisitDebuggerStatement(AstDebuggerStatement debuggerStatement)
        {
            Dynamic = true;
            return debuggerStatement;
        }
    }
}
