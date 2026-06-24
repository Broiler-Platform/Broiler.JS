using System;
using System.Collections.Generic;
using System.Linq;
using Broiler.JavaScript.Ast.Patterns;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Statements;
using Broiler.JavaScript.LinqExpressions.LambdaGen;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Compiler;

partial class FastCompiler
{
    private BExpression Scoped(FastFunctionScope scope, IFastEnumerable<BExpression> body)
    {
        var list = new Sequence<BExpression>();
        list.AddRange(scope.InitList);
        list.AddRange(body);

        if (scope.VariableParameters.Any() && !list.Any())
            throw new InvalidOperationException();

        if (!list.Any())
            return BExpression.Empty;

        var r = BExpression.Block(scope.VariableParameters.AsSequence(), list);

        if (scope.HasDisposable)
        {
            list =
            [
                // create new disposable via factory delegate ...
                BExpression.Assign(scope.Disposable,
                    NewLambdaExpression.StaticCallExpression<IJSDisposableStack>(() => () => IJSDisposableStack.New()))
            ];

            var d = scope.Disposable;
            var dispose = d.CallExpression<IJSDisposableStack, JSValue>(() => (j) => j.Dispose());
            if ((scope.Function?.Async ?? false) && scope.HasAsyncDisposable)
            {
                // An `await using` resource: await the (possibly async) disposal. Only done
                // when the scope actually has an async-disposed resource — a sync-only
                // `using` scope disposes synchronously (no await), which is spec-correct and
                // avoids a Yield inside a try/finally nested in a loop (not yet lowerable by
                // the async state-machine rewrite).
                list.Add(BExpression.TryFinally(r, BExpression.Yield(dispose)));
            }
            else
            {
                // DisposeResources runs with the block's completion: if the body throws,
                // seed that error as the pending completion before disposing, so a disposer
                // error wraps it as a SuppressedError (and a clean disposal re-throws it
                // unchanged). The body error is caught and recorded — the finally then
                // disposes and throws the resulting (possibly wrapping) error.
                var pe = scope.CreateException("#usingBodyError");
                var seed = d.CallExpression<IJSDisposableStack, Exception, JSValue>(
                    () => (j, e) => j.SeedPendingError(e), pe.Expression);
                var guardedBody = BExpression.TryCatch(r, BExpression.Catch(pe.Variable, seed));
                list.Add(BExpression.TryFinally(guardedBody, dispose));

                return BExpression.Block(new Sequence<BParameterExpression> { scope.Disposable, pe.Variable }, list);
            }

            return BExpression.Block(new Sequence<BParameterExpression> { scope.Disposable }, list);
        }

        return r;
    }


    protected override BExpression VisitProgram(AstProgram program)
    {
        var blockList = new Sequence<BExpression>(program.Statements.Count);
        ref var hoistingScope = ref program.HoistingScope;
        var scope = this.scope.Push(new FastFunctionScope(this.scope.Top));
        var lexicalBindings = CollectTopLevelLexicalBindings(program.Statements);
        var functionDeclarations = CollectTopLevelFunctionDeclarations(program.Statements);
        // A top-level *script* publishes its let/const/class bindings into the global lexical
        // environment so later code that runs in the global environment (notably an indirect eval)
        // can resolve them; an eval program keeps its lexical bindings scoped to the eval.
        var globalLexicalScopes = !isDirectEvalCompilation && lexicalBindings.Count > 0
            ? new List<FastFunctionScope.VariableScope>(lexicalBindings.Count)
            : null;
        foreach (var lexicalBinding in lexicalBindings)
        {
            var lexicalVariable = scope.CreateVariable(new StringSpan(lexicalBinding), null, true, initialize: false);
            globalLexicalScopes?.Add(lexicalVariable);
        }

        if (hoistingScope != null)
        {
            var en = hoistingScope.GetFastEnumerator();
            var top = this.scope.Top;
            var isDirectEvalProgramScope = isDirectEvalCompilation && top.Function == null;
        
            while (en.MoveNext(out var v))
            {
                if (lexicalBindings.Contains(v.Value))
                    continue;

                if (isDirectEvalProgramScope && (IsStrictMode || usesDirectEvalLocalVarEnvironment))
                {
                    // Strict eval, and non-strict eval inside a function var
                    // environment, keep vars local to the eval scope.
                    var localVariable = scope.CreateVariable(v, null, true);
                    localVariable.IsLexical = false;
                    localVariable.IsDeletable = !IsStrictMode && isDirectEvalProgramScope;
                    if (usesDirectEvalLocalVarEnvironment && !IsStrictMode)
                    {
                        // EvalDeclarationInstantiation initializes a NEWLY created var binding to
                        // `undefined` (CreateMutableBinding + InitializeBinding(undefined)). When the
                        // name instead reuses an existing var-env binding (a parameter or a pre-existing
                        // var, e.g. `(function(f){ eval("init=f;…") })`), GetOrCreate returns that binding
                        // and ignores this seed — so the seed must be `undefined`, never an eager read of
                        // the current value. Reading the current value here wrongly seeded a fresh local
                        // that shadows an enclosing/global `var X` with the outer value instead of
                        // undefined (test262 sm/misc/line-paragraph-separator: `eval('var hidden …')`
                        // must leave `hidden` undefined even though an outer `var hidden = 17` exists).
                        localVariable.SetInit(JSContextBuilder.GetOrCreateDirectEvalLocalBinding(KeyOfName(v), JSUndefinedBuilder.Value));
                    }
                    continue;
                }

                var isDirectEvalLexicalBinding = directEvalLexicalBindingNames?.Contains(v.Value) ?? false;
                var isFunctionDeclaration = functionDeclarations.Contains(v.Value);
                // A direct-eval global `var` seeds its binding with `undefined`, never an
                // eager read of the existing global property: CreateGlobalVarBinding is a
                // no-op when the property already exists (it must not be re-read or rewritten),
                // and reads of the var go through the lazy ResolveGlobalVarRead/Index paths
                // below. Eagerly reading here would observe an existing accessor's getter
                // (test262 staging/sm/global/bug-320887).
                var g = isDirectEvalProgramScope
                    ? JSUndefinedBuilder.Value
                    : JSValueBuilder.Index(top.Context, KeyOfName(v));
                var vs = scope.CreateVariable(v, null, true);
                vs.IsLexical = false;
                vs.IsDeletable = isDirectEvalProgramScope;
                if (isDirectEvalProgramScope && isDirectEvalLexicalBinding)
                    vs.SkipRegistration = true;
                scope.Parent?.AddExternalVariable(v, vs);

                if (isDirectEvalProgramScope)
                {
                    if (!isDirectEvalLexicalBinding)
                    {
                        vs.Expression = isFunctionDeclaration
                            ? JSVariable.ValueExpression(vs.Variable)
                            : JSContextBuilder.Index(KeyOfName(v));

                        // An eval-introduced global `var` is configurable and deletable: once
                        // `delete` removes it, a later read (e.g. from a closure created in the
                        // eval) must throw a ReferenceError rather than observe the now-absent
                        // global-object property as `undefined`. Reads therefore go through the
                        // throwing global resolution, while writes keep the assignable property
                        // index above (test262 staging/sm/eval/exhaustive-global-*).
                        if (!isFunctionDeclaration)
                            vs.ReadExpression = JSContextBuilder.ResolveGlobalVarRead(KeyOfName(v));
                    }
                }
                else
                    vs.Expression = JSVariableBuilder.Property(vs.Variable);
                vs.SetInit(JSVariableBuilder.New(g, v.Value));
            }
        }

        // Annex B 3.3.2 (sloppy mode): a block/if-clause-nested FunctionDeclaration
        // also creates a var-scoped binding in the global var environment,
        // initialized to `undefined` at GlobalDeclarationInstantiation time (so a
        // read before the declaration resolves to `undefined`). The declaration
        // site copies the function value out via AppendAnnexBOuterBindingAssignments.
        // Direct/strict eval program scopes handle their own var environment, so
        // skip them here.
        if (!IsStrictMode && !isDirectEvalCompilation && program.AnnexBFunctionNames != null)
        {
            var top = this.scope.Top;
            var annexBEn = program.AnnexBFunctionNames.GetFastEnumerator();
            while (annexBEn.MoveNext(out var v))
            {
                if (lexicalBindings.Contains(v.Value) || scope.TryGetOwnVariable(v, out _))
                    continue;

                var g = JSValueBuilder.Index(top.Context, KeyOfName(v));
                var vs = scope.CreateVariable(v, null, true);
                vs.IsLexical = false;
                scope.Parent?.AddExternalVariable(v, vs);
                vs.Expression = JSVariableBuilder.Property(vs.Variable);
                vs.SetInit(JSVariableBuilder.New(g, v.Value));
            }
        }

        // Expose this eval body's genuine top-level lexical names so a B.3.4
        // `if`-clause FunctionDeclaration whose name collides with one of them has
        // its Annex B var hoisting suppressed instead of clobbering the lexical
        // binding (see VisitRuntimeFunctionDeclaration). Restored afterwards so a
        // nested function/program body does not inherit it.
        var previousDirectEvalProgramLexicalNames = directEvalProgramLexicalNames;
        if (isDirectEvalCompilation && this.scope.Top.Function == null)
            directEvalProgramLexicalNames = lexicalBindings;

        // The program (script/eval) result is the script's completion value, which
        // per spec ignores statements that complete with an empty value
        // (declarations, empty statements/blocks, …). Establish a completion var so
        // the existing TrackCompletion (value-bearing expression statements) and
        // PropagateCompletion (nested blocks/loops/labeled statements) plumbing
        // flows into it; statements that complete empty leave it untouched. This is
        // what makes `eval('function f(){}{x:42};')` evaluate to 42 rather than the
        // trailing empty statement's undefined.
        var completionVar = BExpression.Variable(typeof(JSValue), "#programCompletion");
        blockList.Add(BExpression.Assign(completionVar, JSUndefinedBuilder.Value));

        // Publish the script's top-level lexical bindings (created above, still in their TDZ) into
        // the global lexical environment before any statement runs, so an indirect eval invoked
        // anywhere in the script resolves them — yet they never become global-object properties.
        if (globalLexicalScopes != null)
        {
            foreach (var lexicalScope in globalLexicalScopes)
                blockList.Add(JSContextBuilder.DeclareGlobalLexical(lexicalScope.Variable));
        }

        using (completionScopes.Push(completionVar))
        {
            try
            {
                var se = program.Statements.GetFastEnumerator();
                while (se.MoveNext(out var stmt))
                {
                    var exp = Visit(stmt);
                    if (exp == null)
                        continue;

                    // A VariableStatement and a LexicalDeclaration both complete with an
                    // empty value (ECMA-262 — VariableStatement/Declaration Evaluation
                    // return NormalCompletion(empty)), so they must NOT update the
                    // script/eval completion value: `eval('var x = 1')` is undefined and
                    // `eval('1; var x = 1')` is 1. Leaving the declaration unwrapped lets
                    // the completion var retain the previous value-bearing statement's
                    // result (UpdateEmpty). Declarations synthesized inside desugared
                    // for-in/for-of bodies are likewise untouched.

                    blockList.Add(CallStackItemBuilder.Step(scope.StackItem, stmt.Start.Start.Line, stmt.Start.Start.Column));
                    blockList.Add(exp);
                }
            }
            finally
            {
                directEvalProgramLexicalNames = previousDirectEvalProgramLexicalNames;
            }
        }

        blockList.Add(completionVar);
        var r = BExpression.Block(
            new Sequence<BParameterExpression> { completionVar },
            Scoped(scope, blockList));

        scope.Dispose();
        return r;
    }

    private static HashSet<string> CollectTopLevelFunctionDeclarations(IFastEnumerable<AstStatement> statements)
    {
        var functionDeclarations = new HashSet<string>(StringComparer.Ordinal);
        var enumerator = statements.GetFastEnumerator();

        while (enumerator.MoveNext(out var statement))
        {
            if (statement is not AstExpressionStatement { Expression: AstFunctionExpression { IsStatement: true, Id: { } id } })
                continue;

            functionDeclarations.Add(id.Name.Value);
        }

        return functionDeclarations;
    }

    private FastFunctionScope.VariableScope GetOrCreateDirectEvalRootVariable(in StringSpan name, bool functionBinding = false)
    {
        var top = scope.Top;
        while (top.Parent != null && top.Parent.Function == top.Function)
            top = top.Parent;

        var isLexicalDirectEvalBinding = directEvalLexicalBindingNames?.Contains(name.Value) == true;
        var existing = isLexicalDirectEvalBinding
            ? top.TryGetOwnVariable(name, out var ownVariable) ? ownVariable : null
            : top.GetVariable(name);
        if (existing != null)
        {
            existing.IsDeletable = true;
            return existing;
        }

        var globalValue = JSContextBuilder.Index(KeyOfName(name));
        var variable = top.CreateVariable(name, null, true);
        variable.IsLexical = false;
        variable.IsDeletable = true;
        if (isLexicalDirectEvalBinding)
            variable.SkipRegistration = true;
        top.Parent?.AddExternalVariable(name, variable);
        variable.Expression = isLexicalDirectEvalBinding
            ? JSVariable.ValueExpression(variable.Variable)
            : functionBinding
                ? JSVariable.ValueExpression(variable.Variable)
                : directEvalBindingNames?.Contains(name.Value) == true
                ? JSContextBuilder.Index(KeyOfName(name))
                : JSValueBuilder.Index(top.RootScope.Context, KeyOfName(name));
        // Initialise the binding from the current global value rather than
        // `undefined`: per B.3.3.3 / CreateGlobalVarBinding an Annex B function
        // var binding that already exists on the global object is left in place,
        // so a read before the FunctionDeclaration executes must observe the
        // existing value (e.g. `var f='x'; eval('… if (true) function f(){} …')`).
        // When no such global exists the read yields `undefined`, matching the
        // previous behaviour.
        variable.SetInit(JSVariableBuilder.New(globalValue, name.Value));
        return variable;
    }

    private static HashSet<string> CollectTopLevelLexicalBindings(IFastEnumerable<AstStatement> statements)
    {
        var lexicalBindings = new HashSet<string>(StringComparer.Ordinal);
        var enumerator = statements.GetFastEnumerator();

        while (enumerator.MoveNext(out var statement))
        {
            switch (statement)
            {
                case AstVariableDeclaration { Kind: FastVariableKind.Let or FastVariableKind.Const } declaration:
                    var declarators = declaration.Declarators.GetFastEnumerator();
                    while (declarators.MoveNext(out var declarator))
                        CollectBindingNames(declarator.Identifier, lexicalBindings);
                    break;

                // Only a ClassDeclaration introduces a top-level lexical binding;
                // a (parenthesized) named ClassExpression statement does not.
                case AstExpressionStatement { Expression: AstClassExpression { Identifier: { } identifier, IsDeclaration: true } }:
                    lexicalBindings.Add(identifier.Name.Value);
                    break;
            }
        }

        return lexicalBindings;
    }

    private static void CollectBindingNames(AstExpression expression, HashSet<string> names)
    {
        switch (expression)
        {
            case AstIdentifier identifier:
                names.Add(identifier.Name.Value);
                break;

            case AstBinaryExpression assignment:
                CollectBindingNames(assignment.Left, names);
                break;

            case AstSpreadElement spread:
                CollectBindingNames(spread.Argument, names);
                break;

            case AstArrayPattern array:
                var elements = array.Elements.GetFastEnumerator();
                while (elements.MoveNext(out var element))
                {
                    if (element != null)
                        CollectBindingNames(element, names);
                }
                 break;

            case AstObjectPattern @object:
                var properties = @object.Properties.GetFastEnumerator();
                while (properties.MoveNext(out var property))
                    CollectBindingNames(property.Value, names);
                break;
        }
    }
}
