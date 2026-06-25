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

    private BExpression CreateFunction(AstFunctionExpression functionDeclaration, BExpression super = null, bool createClass = false, string className = null,
        IFastEnumerable<AstClassProperty> memberInits = null, bool forceStrictMode = false, bool hoistStatementDeclaration = true, string inferredFunctionName = null,
        bool createPrototype = true, string[] directEvalPrivateNames = null, IReadOnlyDictionary<AstClassProperty, BExpression> computedMemberNames = null,
        bool thisIsUninitialized = false, BExpression superConstructor = null, IReadOnlyList<PrivateInstanceElement> privateInstanceElements = null)
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
            // super() in a derived constructor (or an arrow nested in it) targets the
            // superclass constructor, which differs from the home-object prototype.
            cs.SuperConstructor = superConstructor ?? (functionDeclaration.IsArrowFunction ? previousScope.SuperConstructor : null);
            cs.PrivateInstanceElements = privateInstanceElements;
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
            foreach (var parameterName in parameterNames)
                cs.CreateVariable(parameterName, null, true, initialize: false);

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

            sList.Add(BExpression.Assign(stackItem, CallStackItemBuilder.New(cs.Context, scriptInfo, nameOffset, nameLength, point.Line, point.Column)));

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
                sList.Add(EvalShadowBuilder.Register(stackItem, evalShadow.Variable));

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
                Broiler.JavaScript.ExpressionCompiler.LambdaRewriter.RewriteRootOnly(lambda);

                jsf = JSAsyncFunctionBuilder.Create(JSGeneratorFunctionBuilderV2.New(lambda, fxName, code, functionLength));
            }
            else
            {
                lambda = BExpression.Lambda(typeof(JSFunctionDelegate), block, in scriptFunctionName, [cs.Arguments]);
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
            // A public field is CreateDataPropertyOrThrow — observable through a
            // Proxy receiver's defineProperty trap (a `return`-override base may
            // hand back a Proxy as `this`). A private field is an internal slot
            // added directly via PrivateFieldAdd (never consults proxy traps), which
            // also enforces the TypeError on a non-extensible target or a re-added
            // private name.
            var init = member.IsPrivate
                ? JSObjectBuilder.PrivateFieldAdd(name, value)
                : JSObjectBuilder.CreateDataProperty(name, value);

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
}
