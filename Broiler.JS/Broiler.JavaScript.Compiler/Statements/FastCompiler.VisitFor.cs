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

        return YExpression.Block(pList, YExpression.Assign(completionVar, JSUndefinedBuilder.Value), YExpression.Assign(en, JSValueBuilder.GetAllKeys(right)), YExpression.TryFinally(loop, PropagateCompletion(completionVar, outerCompletionVars)), completionVar);
    }

    protected override YExpression VisitForOfStatement(AstForOfStatement forOfStatement, string? label = null)
    {
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
        var closeIterator = YExpression.Block(
            YExpression.IfThen(
                YExpression.Not(iterDoneVar),
                YExpression.IfThen(
                    YExpression.NotEqual(YExpression.Convert(returnableVar, typeof(object)), YExpression.Null),
                    YExpression.Block(
                        YExpression.Call(returnableVar, ReturnableEnumeratorReturnMethod),
                        YExpression.Empty))),
            YExpression.Empty);

        var loop = YExpression.Loop(bodyList, s.Break, s.Continue);
        var tryFinally = YExpression.TryFinally(loop, closeIterator);

        var r = YExpression.Block(pList,
            YExpression.Assign(completionVar, JSUndefinedBuilder.Value),
            YExpression.Assign(en, enumerator),
            YExpression.Assign(returnableVar, YExpression.TypeAs(en, typeof(IReturnableEnumerator))),
            YExpression.Assign(iterDoneVar, YExpression.Constant(false)),
            YExpression.TryFinally(tryFinally, PropagateCompletion(completionVar, outerCompletionVars)),
            completionVar);

        return r;
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
