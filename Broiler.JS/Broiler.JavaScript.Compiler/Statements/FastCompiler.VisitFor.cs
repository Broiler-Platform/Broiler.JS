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
        YExpression? identifier = forInStatement.Init.Type switch
        {
            FastNodeType.Identifier or FastNodeType.VariableDeclaration => Visit(forInStatement.Init),
            _ => throw new FastParseException(forInStatement.Start, $"Unexpcted"),
        };

        using var completion = completionScopes.Push(completionVar);
        using var s = scope.Top.Loop.Push(new LoopScope(breakTarget, continueTarget, false, label) { CompletionVariable = completionVar });
        var en = YExpression.Variable(typeof(IElementEnumerator));
        var pList = new Sequence<YParameterExpression> { en, completionVar };
        var body = TrackCompletion(VisitStatement(forInStatement.Body));
        var bodyList = YExpression.Block(YExpression.IfThen(YExpression.Not(IElementEnumeratorBuilder.MoveNext(en, identifier)), YExpression.Goto(s.Break)), body);
        var right = VisitExpression(forInStatement.Target);
        var loop = YExpression.Loop(bodyList, s.Break, s.Continue);

        var result = YExpression.Block(pList, YExpression.Assign(completionVar, JSUndefinedBuilder.Value), YExpression.Assign(en, JSValueBuilder.GetAllKeys(right)), YExpression.TryFinally(loop, PropagateCompletion(completionVar, outerCompletionVars)), completionVar);
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

        YExpression? identifier = forOfStatement.Init.Type switch
        {
            FastNodeType.Identifier or FastNodeType.VariableDeclaration => Visit(forOfStatement.Init),
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
            bodyListItems.Add(YExpression.Assign(identifier, YExpression.Yield(identifier)));

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
            YExpression.TryFinally(tryFinally, PropagateCompletion(completionVar, outerCompletionVars)),
            completionVar);

        if (tdzScope == null)
            return r;

        var scoped = Scoped(tdzScope, new Sequence<YExpression> { r });
        tdzScope.Dispose();
        return scoped;
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
                YExpression.TryFinally(loop, PropagateCompletion(completionVar, outerCompletionVars)),
                completionVar);
            return r1;
        }

        var bodyLoop = YExpression.Loop(YExpression.Block(innerBody), breakTarget);
        var r = YExpression.Block(
            new Sequence<YParameterExpression> { completionVar },
            YExpression.Assign(completionVar, JSUndefinedBuilder.Value),
            init,
            YExpression.TryFinally(bodyLoop, PropagateCompletion(completionVar, outerCompletionVars)),
            completionVar);
        return r;
    }
}
