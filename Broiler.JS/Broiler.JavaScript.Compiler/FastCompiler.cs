using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.Ast.Statements;
using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;
using Broiler.JavaScript.LinqExpressions.LinqExpressions.GeneratorsV2;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.LinqExpressions.Utils;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.Compiler;

public partial class FastCompiler : AstMapVisitor<YExpression>
{
    private static readonly MethodInfo EnterStrictModeDisposableMethod = typeof(JSEngine)
        .InternalMethod("EnterStrictModeDisposable", typeof(bool))
        ?? throw new InvalidOperationException("JSEngine.EnterStrictModeDisposable(bool) not found");
    private static readonly MethodInfo DisposeMethod = typeof(IDisposable)
        .GetMethod(nameof(IDisposable.Dispose))
        ?? throw new InvalidOperationException("IDisposable.Dispose() not found");
    private readonly FastPool pool;

    readonly LinkedStack<FastFunctionScope> scope = new();
    readonly ScopedStack<YParameterExpression> completionScopes = new();
    private readonly Stack<FastFunctionScope> withBoundaries = new();

    // The scope of the nearest enclosing sloppy function whose parameter list
    // contains a direct eval. While set, identifier references that resolve OUTSIDE
    // this function are routed through an EvalShadowVariable created in this scope,
    // so an eval-introduced parameter `var` can shadow the outer binding for the
    // function body and the closures it creates (FunctionDeclarationInstantiation
    // step 20). Null when not inside such a function. Inherited by nested functions.
    private FastFunctionScope evalShadowBoundary;
    private readonly string location;
    private readonly bool isDirectEvalCompilation;
    private readonly bool usesDirectEvalLocalVarEnvironment;
    private readonly string[] directEvalBindingNames;
    private readonly string[] directEvalLexicalBindingNames;

    // Annex B 3.3: names handed from CreateFunction to the next VisitBlock (the
    // function body) to create function-scope var bindings for block-nested
    // function declarations. Consumed and cleared by the first VisitBlock so
    // inner blocks do not pick them up.
    private IFastEnumerable<StringSpan> pendingAnnexBFunctionNames;

    // The genuine top-level lexical (let/const/class) binding names of the direct
    // eval body currently being compiled. Used to recognise when a B.3.4 `if`-clause
    // FunctionDeclaration's name collides with such a binding so its Annex B var
    // hoisting must be suppressed (and the outer lexical binding left untouched)
    // rather than overwriting it. Only meaningful at the eval program's own top
    // level (scope.Top.Function == null).
    private HashSet<string> directEvalProgramLexicalNames;

    // True while compiling a class field (member) initializer. A SuperCall is a
    // Syntax Error inside a field initializer even though super property access
    // is permitted, so a direct eval appearing here must reject `super()`.
    private bool inMemberInitializer;

    public LoopScope LoopScope => scope.Top.Loop.Top;

    private StringArray _keyStrings = new();
    private YParameterExpression scriptInfo;

    public YExpression<JSFunctionDelegate> Method { get; }

    public FastCompiler(in StringSpan code, string location = null, IList<string> argsList = null, ICodeCache codeCache = null)
    {
        pool = new FastPool();

        location = location ?? "vm.js";
        this.location = location;
        var context = JSEngine.Current as JSContext;
        isDirectEvalCompilation = context?.IsCompilingDirectEval ?? false;
        usesDirectEvalLocalVarEnvironment = context?.UsesDirectEvalLocalVarEnvironment ?? false;
        directEvalBindingNames = isDirectEvalCompilation ? context?.DirectEvalBindingNamesInScope : null;
        directEvalLexicalBindingNames = isDirectEvalCompilation ? context?.DirectEvalLexicalBindingNamesInScope : null;
        var directEvalPrivateNames = isDirectEvalCompilation ? context?.DirectEvalPrivateNamesInScope : null;

        // add top level...

        var parserPool = new FastPool();
        var parser = new FastParser(new FastTokenStream(parserPool, code));
        var jScript = parser.ParseProgram();
        parserPool.Dispose();
        SyntaxValidation.ValidateProgram(jScript, code.Value, directEvalPrivateNames: directEvalPrivateNames);
        var isStrictProgram = HasUseStrictDirective(jScript);

        using var fx = scope.Push(new FastFunctionScope(pool, null, isAsync: jScript.IsAsync));

        // Direct eval inside a method/initializer that has a [[HomeObject]] super:
        // expose that super to the eval body so super.x resolves. Capture the
        // pushed super value into a local at eval entry (like `this`) so nested
        // arrows close over a stable value instead of re-reading the context.
        if (isDirectEvalCompilation && context?.HasDirectEvalSuper == true)
        {
            var evalSuper = fx.CreateVariable("#evalSuper", JSContextBuilder.DirectEvalSuper);
            evalSuper.SkipRegistration = true;
            fx.Super = evalSuper.Expression;
        }

        // A direct eval inside a derived constructor (before super()) shares that
        // constructor's superclass constructor and `this` binding: a `super(...)` in the
        // eval body must run the superclass [[Construct]] and bind the SAME `this` the
        // constructor sees afterwards. #evalThis holds the shared JSVariable directly
        // (CreateVariable assigns the JSVariable rather than wrapping a value), so `this`
        // reads resolve through it and the eval's super() binds it via its target.
        if (isDirectEvalCompilation && context?.HasDirectEvalSuperCall == true)
        {
            var evalSuperConstructor = fx.CreateVariable("#evalSuperConstructor", JSContextBuilder.DirectEvalSuperConstructor);
            evalSuperConstructor.SkipRegistration = true;
            fx.SuperConstructor = evalSuperConstructor.Expression;

            var evalThis = fx.CreateVariable("#evalThis", JSContextBuilder.DirectEvalThisBinding);
            evalThis.SkipRegistration = true;
            fx.ThisExpression = evalThis.Expression;
        }

        var lScope = fx.Context;

        if (argsList != null && jScript.HoistingScope != null)
        {
            var list = new Sequence<StringSpan>(jScript.HoistingScope.Count);
            var e = jScript.HoistingScope.GetFastEnumerator();

            while (e.MoveNext(out var a))
            {
                if (argsList.Contains(a.Value))
                    continue;

                list.Add(a);
            }

            jScript.HoistingScope = list;
        }

        scriptInfo = YExpression.Parameter(typeof(ScriptInfo));

        var args = fx.ArgumentsExpression;
        var te = ArgumentsBuilder.This(args);
        var stackItem = fx.StackItem;
        var vList = new Sequence<YParameterExpression>() { scriptInfo, lScope, stackItem };

        if (argsList != null)
        {
            int i = 0;
            foreach (var arg in argsList)
            {
                // global arguments are set here for FunctionConstructor
                fx.CreateVariable(arg, JSVariableBuilder.FromArgument(fx.ArgumentsExpression, i++, arg));
            }
        }

        var l = fx.ReturnLabel;
        var previousStrictMode = IsStrictMode;
        IsStrictMode = isStrictProgram;
        var script = Visit(jScript);
        IsStrictMode = previousStrictMode;
        if (isStrictProgram)
            script = WrapInStrictMode(script);
        var sList = new Sequence<YExpression>()
        {
            YExpression.Assign(scriptInfo, ScriptInfoBuilder.New(location,code.Value)),
            YExpression.Assign(lScope, JSContextBuilder.Current)
        };

        JSContextStackBuilder.Push(sList, lScope, stackItem, YExpression.Constant(location), StringSpanBuilder.Empty, 0, 0);
        sList.Add(ScriptInfoBuilder.Build(scriptInfo, _keyStrings));

        // GlobalDeclarationInstantiation step 8: every top-level FunctionDeclaration of a
        // Script (or an eval whose VariableEnvironment is the global environment) must
        // satisfy CanDeclareGlobalFunction BEFORE any binding is instantiated. The
        // per-declaration DeclareGlobalFunction calls run as part of fx.InitList below, so a
        // later non-declarable name (e.g. `function NaN(){}`, NaN being a non-configurable
        // global) would otherwise throw only after an earlier function
        // (`function shouldNotBeDefined(){}`) had already been defined on the global object.
        // Emit all the checks first so the instantiation is rejected atomically and no
        // earlier function leaks. A Function-constructor body (argsList != null), a strict
        // eval, and a non-strict eval nested in a function bind their functions in their own
        // variable environment, not the global object, so they are excluded.
        if (argsList == null && !usesDirectEvalLocalVarEnvironment && !(isDirectEvalCompilation && isStrictProgram))
        {
            var seenGlobalFunctionNames = new HashSet<string>(StringComparer.Ordinal);
            var topLevelStatements = jScript.Statements.GetFastEnumerator();
            while (topLevelStatements.MoveNext(out var topLevelStatement))
            {
                if (topLevelStatement is AstExpressionStatement { Expression: AstFunctionExpression { IsStatement: true, Id: { } globalFunctionId } }
                    && seenGlobalFunctionNames.Add(globalFunctionId.Name.Value))
                {
                    sList.Add(JSContextBuilder.EnsureCanDeclareGlobalFunction(KeyOfName(globalFunctionId.Name)));
                }
            }
        }

        vList.AddRange(fx.VariableParameters);
        sList.AddRange(fx.InitList);

        // register globals..
        foreach (var v in fx.Variables)
        {
            if (v.Variable != null && v.Variable.Type == typeof(JSVariable))
            {
                if (v.SkipRegistration)
                    continue;

                if (argsList?.Contains(v.Name) ?? false)
                    continue;

                if (v.Name == "this" || v.Name == FastFunctionScope.NewTargetBindingName)
                    continue;

                if (isDirectEvalCompilation && isStrictProgram)
                    continue;

                sList.Add(JSContextBuilder.Register(lScope, v.Variable));
            }
        }

        sList.Add(YExpression.Return(l, script.ToJSValue()));
        sList.Add(YExpression.Label(l, JSUndefinedBuilder.Value));

        script = YExpression.Block(vList, YExpression.TryFinally(YExpression.Block(sList), JSContextStackBuilder.Pop(stackItem, lScope)));

        if (jScript.IsAsync)
        {
            var g = GeneratorRewriter.Rewrite("vm", script, fx.ReturnLabel, fx.Generator, replaceArgs: fx.Arguments, replaceStackItem: fx.StackItem,
                replaceContext: fx.Context, replaceScriptInfo: scriptInfo);
            // Pre-rewrite the top-level-await body only; nested lambdas are threaded
            // by the later full rewrite. See LambdaRewriter.rewriteNestedLambdas.
            Broiler.JavaScript.ExpressionCompiler.LambdaRewriter.RewriteRootOnly(g);

            var jsf = JSAsyncFunctionBuilder.Create(JSGeneratorFunctionBuilderV2.New(g, StringSpanBuilder.New("vm"), StringSpanBuilder.New(code.Value)));
            var np = YExpression.Parameter(ArgumentsBuilder.refType, "a");

            jsf = JSFunctionBuilder.InvokeFunction(jsf, np);

            Method = YExpression.Lambda<JSFunctionDelegate>("vm", jsf, [np]);
            return;
        }

        var lambda = YExpression.Lambda<JSFunctionDelegate>("body", script, fx.Arguments);
        Method = lambda;
    }

    private static bool HasUseStrictDirective(AstStatement body)
    {
        if (body is not AstBlock block)
            return false;

        var statements = block.Statements.GetFastEnumerator();
        while (statements.MoveNext(out var statement))
        {
            if (statement is not AstExpressionStatement { Expression: var expression })
                return false;

            if (!expression.IsStringLiteral(out var literal))
                return false;

            if (SyntaxValidation.IsUseStrictDirectiveLiteral((AstLiteral)expression))
                return true;
        }

        return false;
    }

    private static YExpression WrapInStrictMode(YExpression body)
    {
        var strictScope = YExpression.Parameter(typeof(IDisposable), "#strictScope");
        return YExpression.Block(
            new Sequence<YParameterExpression> { strictScope },
            YExpression.Assign(strictScope, YExpression.Call(null, EnterStrictModeDisposableMethod, YExpression.Constant(true))),
            YExpression.TryFinally(body, YExpression.Call(strictScope, DisposeMethod)));
    }

    private YExpression TrackCompletion(YExpression result)
    {
        if (result == null || !typeof(JSValue).IsAssignableFrom(result.Type))
            return result;

        var completionVars = GetCompletionVariables();
        if (completionVars == null || completionVars.Length == 0)
            return result;

        if (completionVars.Length == 1)
            return YExpression.Block(YExpression.Assign(completionVars[0], result), completionVars[0]);

        using var temp = scope.Top.GetTempVariable(typeof(JSValue));
        var statements = new Sequence<YExpression> { YExpression.Assign(temp.Variable, result) };
        foreach (var completionVar in completionVars)
            statements.Add(YExpression.Assign(completionVar, temp.Variable));
        statements.Add(temp.Variable);
        return YExpression.Block(new Sequence<YParameterExpression> { temp.Variable }, statements);
    }

    private YParameterExpression[] GetCompletionVariables()
    {
        var items = new List<YParameterExpression>();
        var current = completionScopes.Top;
        while (current != null)
        {
            if (current.Item == null)
                break;

            items.Add(current.Item);
            current = current.Parent;
        }
        return [.. items];
    }

    private static YExpression PropagateCompletion(YParameterExpression completionVar, YParameterExpression[] outerCompletionVars)
    {
        if (outerCompletionVars == null || outerCompletionVars.Length == 0)
            return YExpression.Empty;

        var statements = new Sequence<YExpression>(outerCompletionVars.Length);
        foreach (var outerCompletionVar in outerCompletionVars)
            statements.Add(YExpression.Assign(outerCompletionVar, completionVar));
        return YExpression.Block(statements);
    }

    private YExpression VisitExpression(AstExpression exp) => Visit(exp);

    private YExpression VisitStatement(AstStatement exp) => Visit(exp);

    protected override YExpression VisitClassStatement(AstClassExpression classStatement) => CreateClass(classStatement.Identifier, classStatement.Base, classStatement);

    protected override YExpression VisitContinueStatement(AstContinueStatement continueStatement)
    {
        string name = continueStatement.Label?.Name.Value;
        if (name != null)
        {
            var target = LoopScope.Get(name);
            // `continue <label>` jumps to the labelled loop's CONTINUE target (next
            // iteration), not its Break target — otherwise it exits the loop entirely.
            return target == null ? throw JSEngine.NewSyntaxError($"No label found for {name}") : YExpression.Continue(target.Continue);
        }

        return YExpression.Continue(scope.Top.Loop.Top.Continue);
    }

    protected override YExpression VisitDebuggerStatement(AstDebuggerStatement debuggerStatement) => JSDebuggerBuilder.RaiseBreak();

    protected override YExpression VisitEmptyExpression(AstEmptyExpression emptyExpression) => YExpression.Empty;

    protected override YExpression VisitExpressionStatement(AstExpressionStatement expressionStatement)
    {
        // A FunctionDeclaration and a ClassDeclaration complete with an empty value
        // (per spec), so they must NOT update the script/eval completion value —
        // e.g. `eval('1; function f(){}')` is 1, not the function. Visit them
        // without TrackCompletion. (Empty statements already compile to a void
        // expression that TrackCompletion ignores.)
        var producesEmptyCompletion = expressionStatement.Expression
            is AstFunctionExpression { IsStatement: true }
            or AstClassExpression { IsDeclaration: true };

        if (isDirectEvalCompilation
            && !IsStrictMode
            && scope.Top.Function == null
            && scope.Top.Parent != scope.Top.RootScope
            && expressionStatement.Expression is AstFunctionExpression { IsStatement: true, Id: { } directEvalFunctionId } directEvalFunctionDeclaration)
        {
            // A block-scoped FunctionDeclaration in direct eval gets an isolated block-local
            // binding (so a self-reassignment in its body — `function f(){ f = 1; }` — stays
            // block-local) plus the Annex B var copy-out to the eval's var environment, exactly
            // like the B.3.4 if-clause implicit block. Without the isolated binding the function
            // shares the global var binding, so mutating it from the body clobbers the outer one.
            // Generator/async declarations are purely block-scoped (no Annex B copy) and route
            // through the same implicit-block path — VisitImplicitBlockFunctionDeclaration's
            // AppendAnnexB call is itself gated on the generator/async check, so the var-env
            // copy-out is omitted while the block binding stays in place. The previous fall
            // through to VisitRuntimeFunctionDeclaration silently created a root binding in the
            // eval's var environment (via GetOrCreateDirectEvalRootVariable), letting
            // `eval("{ function* g(){} }"); typeof g` see "function" instead of "undefined"
            // (test262 sm/lexical-environment/block-scoped-functions-annex-b-generators).
            return VisitImplicitBlockFunctionDeclaration(directEvalFunctionDeclaration, directEvalFunctionId.Name);
        }

        var visited = Visit(expressionStatement.Expression);
        var result = producesEmptyCompletion ? visited : TrackCompletion(visited);

        if (IsStrictMode
            || scope.Top == scope.Top.RootScope
            || expressionStatement.Expression is not AstFunctionExpression { IsStatement: true, Id: { } id })
        {
            return result;
        }

        var currentBinding = scope.Top.GetVariable(id.Name);
        if (isDirectEvalCompilation && !IsStrictMode && !usesDirectEvalLocalVarEnvironment)
            currentBinding ??= GetOrCreateDirectEvalRootVariable(id.Name);
        if (currentBinding == null)
            return result;

        if (isDirectEvalCompilation && !IsStrictMode)
            currentBinding.IsDeletable = true;

        using var temp = scope.Top.GetTempVariable(typeof(JSValue));
        var statements = new Sequence<YExpression>
        {
            YExpression.Assign(temp.Variable, result)
        };

        // Annex B 3.3 Web Legacy Compatibility (the function-scope var copy-out)
        // applies only to plain FunctionDeclarations; generator and async
        // declarations nested in a block stay purely block-scoped.
        if (expressionStatement.Expression is AstFunctionExpression { Generator: false, Async: false })
            AppendAnnexBOuterBindingAssignments(statements, currentBinding, id.Name, temp.Variable);
        statements.Add(temp.Variable);

        return YExpression.Block(new Sequence<YParameterExpression> { temp.Variable }, statements);
    }

    private YExpression VisitRuntimeFunctionDeclaration(AstFunctionExpression functionDeclaration, bool implicitBlockScoped = false)
    {
        var functionName = functionDeclaration.Id!.Name;

        // Annex B.3.4: an `if`-clause FunctionDeclaration is wrapped in an implicit
        // block. Outside of direct eval (which manages its own var environment via
        // the direct-eval root/program bindings), materialise that block so the
        // function's own binding is block-scoped and distinct from the Annex B
        // var-scoped binding it copies its value out to. Without this, a body that
        // reassigns its own name (`function f(){ f = 1; }`) would clobber the outer
        // var/global binding instead of mutating only the block-scoped one.
        if (implicitBlockScoped && !isDirectEvalCompilation)
            return VisitImplicitBlockFunctionDeclaration(functionDeclaration, functionName);

        // Direct eval, B.3.4 `if`-clause FunctionDeclaration whose name is also a
        // genuine top-level lexical (let/const/class) binding of the eval body:
        // Annex B var hoisting is suppressed (B.3.3.3 step ii), so the function is
        // purely block-scoped within its implicit block and must NOT be copied to
        // (and clobber) the lexical binding. Materialise the implicit block; its
        // copy-out is in turn suppressed by IsAnnexBHoistingBlocked. A name that is
        // not a genuine lexical (e.g. a sibling block-scoped function's Annex B var
        // binding) still flows through the normal path below so it updates the var
        // binding as required.
        if (implicitBlockScoped
            && isDirectEvalCompilation
            && scope.Top.Function == null
            && directEvalProgramLexicalNames?.Contains(functionName.Value) == true)
        {
            return VisitImplicitBlockFunctionDeclaration(functionDeclaration, functionName);
        }

        var rootFunction = scope.Top.RootScope.Function;
        FastFunctionScope.VariableScope currentBinding;
        if (rootFunction?.IsArrowFunction == true
            && !HasSimpleParameterList(rootFunction.Params)
            && (functionName.Equals("arguments") || functionName.Equals("eval"))
            && !scope.Top.TryGetOwnVariable(functionName, out currentBinding))
        {
            currentBinding = scope.Top.CreateVariable(functionName, null, true);
        }
        else
        {
            currentBinding = scope.Top.GetVariable(functionName);
        }

        if (currentBinding == null && isDirectEvalCompilation && !IsStrictMode)
            currentBinding = GetOrCreateDirectEvalRootVariable(functionName, true);
        else if (currentBinding != null && isDirectEvalCompilation && !IsStrictMode)
            currentBinding.IsDeletable = true;
        currentBinding ??= scope.Top.CreateVariable(functionName, null, true, initialize: false);
        var result = CreateFunction(functionDeclaration, hoistStatementDeclaration: false);

        using var temp = scope.Top.GetTempVariable(typeof(JSValue));
        var variables = new Sequence<YParameterExpression> { temp.Variable };
        var statements = new Sequence<YExpression>
        {
            YExpression.Assign(temp.Variable, result),
        };

        if (isDirectEvalCompilation && scope.Top.Function == null && !usesDirectEvalLocalVarEnvironment)
            statements.Add(JSContextBuilder.EnsureCanDeclareGlobalFunction(KeyOfName(functionName)));

        statements.AddRange(
        [
            YExpression.Assign(currentBinding.Expression, temp.Variable)
        ]);

        // Annex B 3.3 Web Legacy Compatibility (the function-scope var copy-out)
        // applies only to plain FunctionDeclarations; generator and async
        // declarations stay purely block-scoped.
        if (!functionDeclaration.Generator && !functionDeclaration.Async)
            AppendAnnexBOuterBindingAssignments(statements, currentBinding, functionName, temp.Variable);

        statements.Add(temp.Variable);
        return YExpression.Block(variables, statements);
    }

    // Materialise the implicit B.3.4 block of an `if`-clause FunctionDeclaration:
    // a fresh block-scoped binding (distinct from any outer var/global binding) is
    // created, the function body resolves its own name to it, and the function
    // value is then copied out to the Annex B var-scoped binding (unless blocked).
    private YExpression VisitImplicitBlockFunctionDeclaration(AstFunctionExpression functionDeclaration, in StringSpan functionName)
    {
        var blockScope = scope.Push(new FastFunctionScope(scope.Top));

        // newScope:true forces a binding local to this implicit block even when an
        // outer binding of the same name exists. IsLexical is cleared so the
        // function's own binding is not treated as a lexical blocker of its own
        // Annex B var-hoisting (see IsAnnexBHoistingBlocked).
        var blockBinding = blockScope.CreateVariable(functionName, null, newScope: true, initialize: true);
        blockBinding.IsLexical = false;

        var result = CreateFunction(functionDeclaration, hoistStatementDeclaration: false);

        using var temp = blockScope.GetTempVariable(typeof(JSValue));
        var statements = new Sequence<YExpression>
        {
            YExpression.Assign(temp.Variable, result),
            YExpression.Assign(blockBinding.Expression, temp.Variable),
        };

        // Annex B 3.3 var copy-out is for plain FunctionDeclarations only.
        if (!functionDeclaration.Generator && !functionDeclaration.Async)
            AppendAnnexBOuterBindingAssignments(statements, blockBinding, functionName, temp.Variable);
        statements.Add(temp.Variable);

        var inner = YExpression.Block(new Sequence<YParameterExpression> { temp.Variable }, statements);
        var scoped = Scoped(blockScope, new Sequence<YExpression> { inner });
        blockScope.Dispose();
        return scoped;
    }

    private void AppendAnnexBOuterBindingAssignments(Sequence<YExpression> statements, FastFunctionScope.VariableScope currentBinding, in StringSpan name, YExpression value)
    {
        if (IsStrictMode)
            return;

        var currentFunction = scope.Top.Function;
        if (currentFunction?.IsArrowFunction == true
            && !HasSimpleParameterList(currentFunction.Params)
            && (name.Equals("arguments") || name.Equals("eval")))
        {
            return;
        }

        // Per B.3.3.3 step ii: skip Annex B hoisting when replacing the
        // FunctionDeclaration with a VariableStatement would produce an
        // early error (e.g. name conflicts with a destructured CatchParameter
        // per B.3.5, or with an enclosing lexical binding).
        if (IsAnnexBHoistingBlocked(name))
            return;

        if (scope.Top != scope.Top.RootScope)
        {
            var outerBinding = GetAnnexBOuterBinding(name, currentBinding);
            if (outerBinding != null && outerBinding != currentBinding)
                statements.Add(YExpression.Assign(outerBinding.Expression, value));
        }

        if (scope.Top.Function == null)
        {
            if (usesDirectEvalLocalVarEnvironment)
            {
                var evalProgramBinding = GetOrCreateDirectEvalProgramBinding(name);
                if (evalProgramBinding != null && evalProgramBinding != currentBinding)
                    statements.Add(YExpression.Assign(evalProgramBinding.Expression, value));
            }
            else
            {
                statements.Add(JSContextBuilder.DeclareGlobalAnnexBFunction(KeyOfName(name), value));
            }
        }
    }

    private bool IsAnnexBHoistingBlocked(in StringSpan name)
    {
        // Per B.3.3.3 step ii and B.3.5: Annex B var-hoisting is blocked when a
        // lexical binding with the same name exists in the current or an enclosing
        // scope (e.g. a destructured CatchParameter or a let/const/class binding).
        // The current scope is included so that, for an `if`-clause function whose
        // implicit block sits inside a switch/block CaseBlock, a sibling `let`/
        // `const` of the same name in that CaseBlock blocks the hoist. A function's
        // own (non-lexical) binding never blocks itself.
        var current = scope.Top;
        while (current != null && current.Function == scope.Top.Function)
        {
            if (current.TryGetOwnVariable(name, out var variable) && variable.IsLexical && !variable.IsSimpleCatchBinding)
                return true;
            current = current.Parent;
        }
        return false;
    }

    private FastFunctionScope.VariableScope GetAnnexBOuterBinding(in StringSpan name, FastFunctionScope.VariableScope currentBinding)
    {
        var parent = scope.Top.Parent;
        while (parent != null && parent.Function == scope.Top.Function)
        {
            // Skip a simple CatchParameter binding: per B.3.5 it does not block
            // Annex B hoisting, and the var-scoped binding targets the enclosing
            // function/program var environment, not the catch parameter. Returning
            // it here would assign the (discarded) catch binding and short-circuit
            // creation of the real outer binding, leaving the name undefined.
            if (parent.TryGetOwnVariable(name, out var variable) && variable != currentBinding && !variable.IsSimpleCatchBinding)
                return variable;

            parent = parent.Parent;
        }

        if (scope.Top.RootScope.TryGetOwnVariable(name, out var rootVariable))
            return rootVariable;

        if (scope.Top.Function == null)
        {
            var globalVariable = scope.Top.RootScope.CreateVariable(name, null, true);
            globalVariable.Expression = JSVariableBuilder.Property(globalVariable.Variable);
            globalVariable.SetInit(JSVariableBuilder.New(JSValueBuilder.Index(scope.Top.RootScope.Context, KeyOfName(name)), name.Value));
            return globalVariable;
        }

        return scope.Top.RootScope.CreateVariable(name, null, true);
    }

    private FastFunctionScope.VariableScope GetOrCreateDirectEvalProgramBinding(in StringSpan name)
    {
        var current = scope.Top;
        while (current.Parent != null && current.Parent != current.RootScope)
            current = current.Parent;

        if (current.TryGetOwnVariable(name, out var variable))
            return variable;

        variable = current.CreateVariable(name, null, true);
        variable.IsDeletable = true;
        // This is the eval var-environment's Annex B var binding, NOT a lexical
        // one. CreateVariable(newScope: true) defaults IsLexical to true; leaving
        // it set makes IsAnnexBHoistingBlocked treat the binding as a lexical
        // blocker, so a second block declaring the same function name would have
        // its Annex B copy-out suppressed (only the first block's value reached the
        // var binding). Clearing it lets every block update the shared var binding.
        variable.IsLexical = false;
        return variable;
    }

    protected override YExpression VisitFunctionExpression(AstFunctionExpression functionExpression)
        // Arrow functions have no super binding of their own; they inherit the
        // lexical [[HomeObject]] super of the enclosing method/initializer.
        => CreateFunction(functionExpression, functionExpression.IsArrowFunction ? scope.Top.Super : null);

    protected override YExpression VisitSuper(AstSuper super) => scope.Top.ThisExpression;

    protected override YExpression VisitSpreadElement(AstSpreadElement spreadElement) => throw new NotImplementedException();

    protected override YExpression VisitThrowStatement(AstThrowStatement throwStatement) => JSExceptionBuilder.Throw(VisitExpression(throwStatement.Argument));

    protected override YExpression VisitYieldExpression(AstYieldExpression yieldExpression)
    {
        var target = yieldExpression.Argument == null ? JSUndefinedBuilder.Value : VisitExpression(yieldExpression.Argument);
        return YExpression.Yield(target, yieldExpression.Delegate);
    }
}
