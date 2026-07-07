using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Statements;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Compiler;


partial class FastCompiler
{
    protected override BExpression VisitForInStatement(AstForInStatement forInStatement, string? label = null)
    {
        FastFunctionScope? tdzScope = null;
        if (forInStatement.HeadTdzNames != null)
        {
            tdzScope = scope.Push(new FastFunctionScope(scope.Top));
            var tdzNames = forInStatement.HeadTdzNames.GetFastEnumerator();
            while (tdzNames.MoveNext(out var name))
            {
                var variable = tdzScope.CreateVariable(name, null, true, initialize: false);
                variable.IsLexical = true;
            }
        }

        var breakTarget = BExpression.Label();
        var continueTarget = BExpression.Label();
        var completionVar = BExpression.Variable(typeof(JSValue), "#cv");
        var outerCompletionVars = GetCompletionVariables();
        // this will create a variable if needed...
        // desugar takes care of let so do not worry

        // The for-in head may bind to a simple identifier, a destructuring
        // pattern (with or without a var declaration), or an arbitrary
        // assignment target such as a member expression. Mirror the for-of
        // handling: when the target is not a simple identifier, enumerate each
        // key into a temporary and run the assignment per-iteration.
        var perIterationInits = new Sequence<BExpression>();
        BParameterExpression? iterationValueVariable = null;
        BExpression? identifier = forInStatement.Init.Type switch
        {
            // A bare-identifier head (whether it resolves to a known binding or is a
            // free/undeclared identifier) is assigned through the per-iteration
            // assignment path (`x = key`), NOT used directly as the enumerator
            // out-parameter. Passing the loop-target binding's value expression
            // (a boxed `JSVariable.Value`) as the MoveNext out-argument did not
            // write back to the binding, so `for (x of …)` / `for (x in …)` over an
            // existing `var`/`let` `x` left it unchanged after the loop. Routing it
            // through CreateForOfDestructuringAssignment assigns the real binding
            // (and, for a free identifier in sloppy mode, creates/assigns the global)
            // exactly like the member-expression and destructuring heads already do.
            FastNodeType.Identifier =>
                CreateForOfDestructuringAssignment(
                    (AstExpression)forInStatement.Init,
                    perIterationInits,
                    out iterationValueVariable,
                    forceDynamicAssignment: false),
            FastNodeType.VariableDeclaration when TryCreateForOfDestructuringAssignment(
                (AstVariableDeclaration)forInStatement.Init,
                perIterationInits,
                out iterationValueVariable)
                => iterationValueVariable,
            FastNodeType.VariableDeclaration => Visit(forInStatement.Init),
            _ when forInStatement.Init is AstExpression expression =>
                CreateForOfDestructuringAssignment(
                    expression,
                    perIterationInits,
                    out iterationValueVariable,
                    forceDynamicAssignment: false),
            _ => throw new FastParseException(forInStatement.Start, $"Unexpcted"),
        };

        // AnnexB B.3.7: `for (var a = <init> in obj)` — a `var` binding carrying an
        // initializer, valid only in sloppy mode — evaluates the initializer and assigns it
        // to the binding ONCE, before the RHS Expression is evaluated. (let/const initializers
        // and for-of initializers are rejected by the parser; the per-iteration path above
        // only assigns each enumerated key.)
        BExpression? headInitAssignment = null;
        if (forInStatement.Init is AstVariableDeclaration { Kind: FastVariableKind.Var } headDeclaration)
        {
            var declEnumerator = headDeclaration.Declarators.GetFastEnumerator();
            if (declEnumerator.MoveNext(out var headDeclarator)
                && headDeclarator.Identifier.Type == FastNodeType.Identifier
                && headDeclarator.Init != null)
            {
                headInitAssignment = CreateAssignment(
                    headDeclarator.Identifier,
                    VisitExpression(headDeclarator.Init),
                    createVariable: true);
            }
        }

        using var completion = completionScopes.Push(completionVar);
        using var s = scope.Top.Loop.Push(new LoopScope(breakTarget, continueTarget, false, label) { CompletionVariable = completionVar });
        var en = BExpression.Variable(typeof(IElementEnumerator));
        var pList = new Sequence<BParameterExpression> { en, completionVar };
        if (iterationValueVariable != null)
            pList.Add(iterationValueVariable);
        var body = TrackCompletion(VisitStatement(forInStatement.Body));
        var bodyListItems = new Sequence<BExpression>
        {
            BExpression.IfThen(BExpression.Not(IElementEnumeratorBuilder.MoveNext(en, identifier)), BExpression.Goto(s.Break)),
        };
        bodyListItems.AddRange(perIterationInits);
        bodyListItems.Add(body);
        var bodyList = BExpression.Block(bodyListItems);
        var right = VisitExpression(forInStatement.Target);
        var loop = BExpression.Loop(bodyList, s.Break, s.Continue);

        var resultItems = new Sequence<BExpression> { BExpression.Assign(completionVar, JSUndefinedBuilder.Value) };
        if (headInitAssignment != null)
            resultItems.Add(headInitAssignment);
        resultItems.Add(BExpression.Assign(en, JSValueBuilder.GetAllKeys(right)));
        resultItems.Add(BExpression.TailCallTransparentTryFinally(loop, PropagateCompletion(completionVar, outerCompletionVars)));
        resultItems.Add(completionVar);
        var result = BExpression.Block(pList, resultItems);
        if (tdzScope == null)
            return result;

        var scoped = Scoped(tdzScope, new Sequence<BExpression> { result });
        tdzScope.Dispose();
        return scoped;
    }

    protected override BExpression VisitForOfStatement(AstForOfStatement forOfStatement, string? label = null)
    {
        FastFunctionScope? tdzScope = null;
        if (forOfStatement.HeadTdzNames != null)
        {
            tdzScope = scope.Push(new FastFunctionScope(scope.Top));
            var tdzNames = forOfStatement.HeadTdzNames.GetFastEnumerator();
            while (tdzNames.MoveNext(out var name))
            {
                var variable = tdzScope.CreateVariable(name, null, true, initialize: false);
                variable.IsLexical = true;
            }
        }

        var breakTarget = BExpression.Label();
        var continueTarget = BExpression.Label();
        var completionVar = BExpression.Variable(typeof(JSValue), "#cv");
        var outerCompletionVars = GetCompletionVariables();
        // this will create a variable if needed...
        // desugar takes care of let so do not worry

        var perIterationInits = new Sequence<BExpression>();
        BParameterExpression? iterationValueVariable = null;
        BExpression? identifier = forOfStatement.Init.Type switch
        {
            // See VisitForInStatement: a bare-identifier head (declared or free) is
            // assigned through the per-iteration path (`x = value`). Using the
            // binding's boxed value expression directly as the MoveNext out-argument
            // failed to persist the write, so `for (x of o)` over an existing
            // `var`/`let` left it unchanged after the loop.
            FastNodeType.Identifier =>
                CreateForOfDestructuringAssignment(
                    (AstExpression)forOfStatement.Init,
                    perIterationInits,
                    out iterationValueVariable,
                    forceDynamicAssignment: false),
            FastNodeType.VariableDeclaration when TryCreateForOfDestructuringAssignment(
                (AstVariableDeclaration)forOfStatement.Init,
                perIterationInits,
                out iterationValueVariable)
                => iterationValueVariable,
            FastNodeType.VariableDeclaration => Visit(forOfStatement.Init),
            // A for-await head must resolve declared (lexical) destructuring targets
            // statically, exactly like sync for-of: the static JSVariable reference
            // survives the async state-machine rewrite (a plain `x = …` in the body
            // does). Forcing a dynamic assignment here (the old `IsAwait` flag) wrote
            // to the global object instead of a `let`/`const` binding's cell, leaving
            // the bound variable undefined. Undeclared free identifiers still fall
            // back to a dynamic assignment via the TryGetStaticIdentifierVariable
            // check inside CreateAssignment.
            _ when forOfStatement.Init is AstExpression expression =>
                CreateForOfDestructuringAssignment(
                    expression,
                    perIterationInits,
                    out iterationValueVariable,
                    forceDynamicAssignment: false),
            _ => throw new FastParseException(forOfStatement.Start, $"Unexpcted"),
        };

        using var completion = completionScopes.Push(completionVar);
        using var s = scope.Top.Loop.Push(new LoopScope(breakTarget, continueTarget, false, label) { CompletionVariable = completionVar });
        var en = BExpression.Variable(typeof(IElementEnumerator));
        var body = TrackCompletion(VisitStatement(forOfStatement.Body));
        var right = VisitExpression(forOfStatement.Target);
        var enumerator = forOfStatement.IsAwait ? IElementEnumeratorBuilder.GetAsync(right) : IElementEnumeratorBuilder.Get(right);

        // Wrap loop in try-finally to call iterator.return() on abrupt
        // completion (break/return/throw) per ECMAScript IteratorClose.
        var returnableVar = BExpression.Variable(typeof(IReturnableEnumerator));
        var iterDoneVar = BExpression.Variable(typeof(bool));

        var pList = new Sequence<BParameterExpression> { en, returnableVar, iterDoneVar, completionVar };
        if (iterationValueVariable != null)
            pList.Add(iterationValueVariable);

        var bodyListItems = new Sequence<BExpression>
        {
            // IteratorStep / IteratorValue (performed by MoveNext, which reads the result's
            // `value`) abrupt completions must NOT close the iterator — only the binding and
            // the body do (test262 for-of/iterator-next-result-value-attr-error). Mark the
            // iterator "done" while stepping and reading the value so a throw from MoveNext
            // skips the finally/catch close, then clear it once the value is in hand.
            BExpression.Assign(iterDoneVar, BExpression.Constant(true)),
            // When MoveNext returns false the iterator finished normally;
            // leave it marked done so finally does NOT call return().
            BExpression.IfThen(
                BExpression.Not(IElementEnumeratorBuilder.MoveNext(en, identifier)),
                BExpression.Goto(s.Break)),
            BExpression.Assign(iterDoneVar, BExpression.Constant(false))
        };

        if (forOfStatement.IsAwait)
            bodyListItems.Add(BExpression.Assign(identifier, BExpression.Await(identifier)));

        bodyListItems.AddRange(perIterationInits);
        bodyListItems.Add(body);
        var bodyList = BExpression.Block(bodyListItems);

        // Build a void finally body – must not leave values on the stack.
        // IteratorClose preserves an active throw completion even if return()
        // itself throws; return() errors are only observable for non-throw
        // abrupt completions such as break/return.
        var caughtException = scope.Top.CreateException("#forOfIteratorClose");
        var closeIterator = BExpression.Block(
            BExpression.IfThen(
                BExpression.Not(iterDoneVar),
                BExpression.Block(
                    BExpression.Call(null, CloseIteratorMethod, returnableVar),
                    BExpression.Empty)),
            BExpression.Empty);
        var closeIteratorAfterThrow = BExpression.Block(
            BExpression.IfThen(
                BExpression.Not(iterDoneVar),
                BExpression.Block(
                    BExpression.Call(null, CloseIteratorIgnoringErrorsMethod, returnableVar),
                    BExpression.Assign(iterDoneVar, BExpression.Constant(true)),
                    BExpression.Empty)),
            BExpression.Throw(caughtException.Expression));

        var loop = BExpression.Loop(bodyList, s.Break, s.Continue);
        var tryFinally = BExpression.TryCatchFinally(
            loop,
            closeIterator,
            BExpression.Catch(caughtException.Variable, closeIteratorAfterThrow));

        var r = BExpression.Block(pList,
            BExpression.Assign(completionVar, JSUndefinedBuilder.Value),
            BExpression.Assign(en, enumerator),
            BExpression.Assign(returnableVar, BExpression.TypeAs(en, typeof(IReturnableEnumerator))),
            BExpression.Assign(iterDoneVar, BExpression.Constant(false)),
            BExpression.TailCallTransparentTryFinally(tryFinally, PropagateCompletion(completionVar, outerCompletionVars)),
            completionVar);

        if (tdzScope == null)
            return r;

        var scoped = Scoped(tdzScope, new Sequence<BExpression> { r });
        tdzScope.Dispose();
        return scoped;
    }

    private BExpression CreateForOfDestructuringAssignment(
        AstExpression expression,
        Sequence<BExpression> perIterationInits,
        out BParameterExpression? iterationValueVariable,
        bool forceDynamicAssignment)
    {
        iterationValueVariable = BExpression.Variable(typeof(JSValue), "#forOfValue");

        // A function call (or other invalid reference) used as the loop target is
        // a runtime ReferenceError (AnnexB web-compat). Evaluate it for its side
        // effects on each iteration, then throw, without coercing the iterated
        // value. Mirrors VisitAssignmentExpression's handling of `f() = x`.
        if (expression.Type == FastNodeType.CallExpression)
        {
            perIterationInits.Add(Visit(expression));
            perIterationInits.Add(BExpression.Call(null, ThrowInvalidAssignmentReferenceMethod));
            return iterationValueVariable;
        }

        CreateAssignment(
            perIterationInits,
            expression.ToPattern(),
            iterationValueVariable,
            createVariable: false,
            newScope: false,
            suppressAnonymousFunctionNameInference: true,
            forceDynamicAssignment: forceDynamicAssignment);
        return iterationValueVariable;
    }

    private bool TryCreateForOfDestructuringAssignment(
        AstVariableDeclaration declaration,
        Sequence<BExpression> perIterationInits,
        out BParameterExpression? iterationValueVariable)
    {
        iterationValueVariable = null;

        if (declaration.Declarators.Count != 1)
            return false;

        var declarator = declaration.Declarators[0];
        var isPattern = declarator.Identifier.Type is FastNodeType.ArrayPattern or FastNodeType.ObjectPattern;

        // A simple `var` identifier head is also routed through the per-iteration
        // assignment path. Inside a (direct or indirect) eval a `var` loop variable
        // is a dynamic binding on the surrounding variable environment; using its
        // lvalue directly as the MoveNext out-argument did not persist the write, so
        // the loop variable read back as undefined on every iteration. Going through
        // CreateAssignment writes the dynamic/global binding correctly (the same path
        // a free bare-identifier head already uses). `let`/`const` simple identifiers
        // are block-scoped statics and keep their existing direct-binding path.
        var isVarIdentifier = declarator.Identifier.Type == FastNodeType.Identifier
            && declaration.Kind == FastVariableKind.Var;

        if (!isPattern && !isVarIdentifier)
            return false;

        iterationValueVariable = BExpression.Variable(typeof(JSValue), "#forOfValue");
        var newScope = declaration.Kind is FastVariableKind.Const or FastVariableKind.Let;
        var readOnlyAfterAssign = declaration.Kind == FastVariableKind.Const;
        CreateAssignment(
            perIterationInits,
            declarator.Identifier,
            iterationValueVariable,
            createVariable: true,
            newScope: newScope,
            suppressAnonymousFunctionNameInference: true,
            readOnlyAfterAssign: readOnlyAfterAssign);
        return true;
    }

    protected override BExpression VisitForStatement(AstForStatement forStatement, string? label = null)
    {
        var breakTarget = BExpression.Label();
        var continueTarget = BExpression.Label();
        var completionVar = BExpression.Variable(typeof(JSValue), "#cv");
        var outerCompletionVars = GetCompletionVariables();
        // C-style `for (let x = …; …; …)` introduces a fresh per-loop lexical
        // environment for the head bindings (PerIterationEnvironment). The parser's
        // desugar keeps the original name `x` as the head decl alongside a synthetic
        // carrier so closures inside test/update can capture it, but without a
        // compile-time scope push the head's `let x` shares the enclosing scope's
        // bindings — silently replacing an outer `let x` and leaving its value
        // visible after the loop (test262 language/statements/for/scope-head-lex-open).
        var forScope = forStatement.Init is AstVariableDeclaration { Kind: FastVariableKind.Let or FastVariableKind.Const }
            ? scope.Push(new FastFunctionScope(scope.Top))
            : null;
        try
        {
        // this will create a variable if needed...
        // desugar takes care of let so do not worry
        BExpression init = Visit(forStatement.Init);
        var innerBody = new Sequence<BExpression>();

        var update = Visit(forStatement.Update);
        var test = Visit(forStatement.Test);

        if (test != null)
        {
            test = BExpression.IfThen(BExpression.Not(JSValueBuilder.BooleanValue(test)), BExpression.Goto(breakTarget));
            innerBody.Add(test);
        }

        using var completion = completionScopes.Push(completionVar);
        using var s = scope.Top.Loop.Push(new LoopScope(breakTarget, continueTarget, false, label) { CompletionVariable = completionVar });
        var body = TrackCompletion(VisitStatement(forStatement.Body));

        innerBody.Add(body);
        innerBody.Add(BExpression.Label(continueTarget));

        if (update != null)
            innerBody.Add(update);

        if (init == null)
        {
            var loop = BExpression.Loop(BExpression.Block(innerBody), breakTarget);
            var r1 = BExpression.Block(
                new Sequence<BParameterExpression> { completionVar },
                BExpression.Assign(completionVar, JSUndefinedBuilder.Value),
                BExpression.TailCallTransparentTryFinally(loop, PropagateCompletion(completionVar, outerCompletionVars)),
                completionVar);
            return forScope != null ? Scoped(forScope, new Sequence<BExpression> { r1 }) : r1;
        }

        var bodyLoop = BExpression.Loop(BExpression.Block(innerBody), breakTarget);
        var r = BExpression.Block(
            new Sequence<BParameterExpression> { completionVar },
            BExpression.Assign(completionVar, JSUndefinedBuilder.Value),
            init,
            BExpression.TailCallTransparentTryFinally(bodyLoop, PropagateCompletion(completionVar, outerCompletionVars)),
            completionVar);
        return forScope != null ? Scoped(forScope, new Sequence<BExpression> { r }) : r;
        }
        finally
        {
            forScope?.Dispose();
        }
    }
}
