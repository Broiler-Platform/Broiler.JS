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
    protected override YExpression VisitForInStatement(AstForInStatement forInStatement, string? label = null)
    {
        FastFunctionScope? tdzScope = null;
        if (forInStatement.HeadTdzNames != null)
        {
            tdzScope = this.scope.Push(new FastFunctionScope(this.scope.Top));
            var tdzNames = forInStatement.HeadTdzNames.GetFastEnumerator();
            while (tdzNames.MoveNext(out var name))
            {
                var variable = tdzScope.CreateVariable(name, null, true, initialize: false);
                variable.IsLexical = true;
            }
        }

        var breakTarget = YExpression.Label();
        var continueTarget = YExpression.Label();
        var completionVar = YExpression.Variable(typeof(JSValue), "#cv");
        var outerCompletionVars = GetCompletionVariables();
        // this will create a variable if needed...
        // desugar takes care of let so do not worry

        // The for-in head may bind to a simple identifier, a destructuring
        // pattern (with or without a var declaration), or an arbitrary
        // assignment target such as a member expression. Mirror the for-of
        // handling: when the target is not a simple identifier, enumerate each
        // key into a temporary and run the assignment per-iteration.
        var perIterationInits = new Sequence<YExpression>();
        YParameterExpression? iterationValueVariable = null;
        YExpression? identifier = forInStatement.Init.Type switch
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
        YExpression? headInitAssignment = null;
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
        var en = YExpression.Variable(typeof(IElementEnumerator));
        var pList = new Sequence<YParameterExpression> { en, completionVar };
        if (iterationValueVariable != null)
            pList.Add(iterationValueVariable);
        var body = TrackCompletion(VisitStatement(forInStatement.Body));
        var bodyListItems = new Sequence<YExpression>
        {
            YExpression.IfThen(YExpression.Not(IElementEnumeratorBuilder.MoveNext(en, identifier)), YExpression.Goto(s.Break)),
        };
        bodyListItems.AddRange(perIterationInits);
        bodyListItems.Add(body);
        var bodyList = YExpression.Block(bodyListItems);
        var right = VisitExpression(forInStatement.Target);
        var loop = YExpression.Loop(bodyList, s.Break, s.Continue);

        var resultItems = new Sequence<YExpression> { YExpression.Assign(completionVar, JSUndefinedBuilder.Value) };
        if (headInitAssignment != null)
            resultItems.Add(headInitAssignment);
        resultItems.Add(YExpression.Assign(en, JSValueBuilder.GetAllKeys(right)));
        resultItems.Add(YExpression.TailCallTransparentTryFinally(loop, PropagateCompletion(completionVar, outerCompletionVars)));
        resultItems.Add(completionVar);
        var result = YExpression.Block(pList, resultItems);
        if (tdzScope == null)
            return result;

        var scoped = Scoped(tdzScope, new Sequence<YExpression> { result });
        tdzScope.Dispose();
        return scoped;
    }

    protected override YExpression VisitForOfStatement(AstForOfStatement forOfStatement, string? label = null)
    {
        FastFunctionScope? tdzScope = null;
        if (forOfStatement.HeadTdzNames != null)
        {
            tdzScope = this.scope.Push(new FastFunctionScope(this.scope.Top));
            var tdzNames = forOfStatement.HeadTdzNames.GetFastEnumerator();
            while (tdzNames.MoveNext(out var name))
            {
                var variable = tdzScope.CreateVariable(name, null, true, initialize: false);
                variable.IsLexical = true;
            }
        }

        var breakTarget = YExpression.Label();
        var continueTarget = YExpression.Label();
        var completionVar = YExpression.Variable(typeof(JSValue), "#cv");
        var outerCompletionVars = GetCompletionVariables();
        // this will create a variable if needed...
        // desugar takes care of let so do not worry

        var perIterationInits = new Sequence<YExpression>();
        YParameterExpression? iterationValueVariable = null;
        YExpression? identifier = forOfStatement.Init.Type switch
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
        var en = YExpression.Variable(typeof(IElementEnumerator));
        var body = TrackCompletion(VisitStatement(forOfStatement.Body));
        var right = VisitExpression(forOfStatement.Target);
        var enumerator = forOfStatement.IsAwait ? IElementEnumeratorBuilder.GetAsync(right) : IElementEnumeratorBuilder.Get(right);

        // Wrap loop in try-finally to call iterator.return() on abrupt
        // completion (break/return/throw) per ECMAScript IteratorClose.
        var returnableVar = YExpression.Variable(typeof(IReturnableEnumerator));
        var iterDoneVar = YExpression.Variable(typeof(bool));

        var pList = new Sequence<YParameterExpression> { en, returnableVar, iterDoneVar, completionVar };
        if (iterationValueVariable != null)
            pList.Add(iterationValueVariable);

        var bodyListItems = new Sequence<YExpression>
        {
            // When MoveNext returns false the iterator finished normally;
            // mark it done so finally does NOT call return().
            YExpression.IfThen(
                YExpression.Not(IElementEnumeratorBuilder.MoveNext(en, identifier)),
                YExpression.Block(
                    YExpression.Assign(iterDoneVar, YExpression.Constant(true)),
                    YExpression.Goto(s.Break)))
        };

        if (forOfStatement.IsAwait)
            bodyListItems.Add(YExpression.Assign(identifier, YExpression.Await(identifier)));

        bodyListItems.AddRange(perIterationInits);
        bodyListItems.Add(body);
        var bodyList = YExpression.Block(bodyListItems);

        // Build a void finally body – must not leave values on the stack.
        // IteratorClose preserves an active throw completion even if return()
        // itself throws; return() errors are only observable for non-throw
        // abrupt completions such as break/return.
        var caughtException = scope.Top.CreateException("#forOfIteratorClose");
        var closeIterator = YExpression.Block(
            YExpression.IfThen(
                YExpression.Not(iterDoneVar),
                YExpression.Block(
                    YExpression.Call(null, CloseIteratorMethod, returnableVar),
                    YExpression.Empty)),
            YExpression.Empty);
        var closeIteratorAfterThrow = YExpression.Block(
            YExpression.IfThen(
                YExpression.Not(iterDoneVar),
                YExpression.Block(
                    YExpression.Call(null, CloseIteratorIgnoringErrorsMethod, returnableVar),
                    YExpression.Assign(iterDoneVar, YExpression.Constant(true)),
                    YExpression.Empty)),
            YExpression.Throw(caughtException.Expression));

        var loop = YExpression.Loop(bodyList, s.Break, s.Continue);
        var tryFinally = YExpression.TryCatchFinally(
            loop,
            closeIterator,
            YExpression.Catch(caughtException.Variable, closeIteratorAfterThrow));

        var r = YExpression.Block(pList,
            YExpression.Assign(completionVar, JSUndefinedBuilder.Value),
            YExpression.Assign(en, enumerator),
            YExpression.Assign(returnableVar, YExpression.TypeAs(en, typeof(IReturnableEnumerator))),
            YExpression.Assign(iterDoneVar, YExpression.Constant(false)),
            YExpression.TailCallTransparentTryFinally(tryFinally, PropagateCompletion(completionVar, outerCompletionVars)),
            completionVar);

        if (tdzScope == null)
            return r;

        var scoped = Scoped(tdzScope, new Sequence<YExpression> { r });
        tdzScope.Dispose();
        return scoped;
    }

    private YExpression CreateForOfDestructuringAssignment(
        AstExpression expression,
        Sequence<YExpression> perIterationInits,
        out YParameterExpression? iterationValueVariable,
        bool forceDynamicAssignment)
    {
        iterationValueVariable = YExpression.Variable(typeof(JSValue), "#forOfValue");

        // A function call (or other invalid reference) used as the loop target is
        // a runtime ReferenceError (AnnexB web-compat). Evaluate it for its side
        // effects on each iteration, then throw, without coercing the iterated
        // value. Mirrors VisitAssignmentExpression's handling of `f() = x`.
        if (expression.Type == FastNodeType.CallExpression)
        {
            perIterationInits.Add(Visit(expression));
            perIterationInits.Add(YExpression.Call(null, ThrowInvalidAssignmentReferenceMethod));
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
        Sequence<YExpression> perIterationInits,
        out YParameterExpression? iterationValueVariable)
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

        iterationValueVariable = YExpression.Variable(typeof(JSValue), "#forOfValue");
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

    protected override YExpression VisitForStatement(AstForStatement forStatement, string? label = null)
    {
        var breakTarget = YExpression.Label();
        var continueTarget = YExpression.Label();
        var completionVar = YExpression.Variable(typeof(JSValue), "#cv");
        var outerCompletionVars = GetCompletionVariables();
        // this will create a variable if needed...
        // desugar takes care of let so do not worry
        YExpression init = Visit(forStatement.Init);
        var innerBody = new Sequence<YExpression>();

        var update = Visit(forStatement.Update);
        var test = Visit(forStatement.Test);

        if (test != null)
        {
            test = YExpression.IfThen(YExpression.Not(JSValueBuilder.BooleanValue(test)), YExpression.Goto(breakTarget));
            innerBody.Add(test);
        }

        using var completion = completionScopes.Push(completionVar);
        using var s = scope.Top.Loop.Push(new LoopScope(breakTarget, continueTarget, false, label) { CompletionVariable = completionVar });
        var body = TrackCompletion(VisitStatement(forStatement.Body));

        innerBody.Add(body);
        innerBody.Add(YExpression.Label(continueTarget));

        if (update != null)
            innerBody.Add(update);

        if (init == null)
        {
            var loop = YExpression.Loop(YExpression.Block(innerBody), breakTarget);
            var r1 = YExpression.Block(
                new Sequence<YParameterExpression> { completionVar },
                YExpression.Assign(completionVar, JSUndefinedBuilder.Value),
                YExpression.TailCallTransparentTryFinally(loop, PropagateCompletion(completionVar, outerCompletionVars)),
                completionVar);
            return r1;
        }

        var bodyLoop = YExpression.Loop(YExpression.Block(innerBody), breakTarget);
        var r = YExpression.Block(
            new Sequence<YParameterExpression> { completionVar },
            YExpression.Assign(completionVar, JSUndefinedBuilder.Value),
            init,
            YExpression.TailCallTransparentTryFinally(bodyLoop, PropagateCompletion(completionVar, outerCompletionVars)),
            completionVar);
        return r;
    }
}
