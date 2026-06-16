using System;
using System.Collections.Generic;
using System.Linq;
using Exp = Broiler.JavaScript.ExpressionCompiler.Expressions.YExpression;
using Expression = Broiler.JavaScript.ExpressionCompiler.Expressions.YExpression;
using ParameterExpression = Broiler.JavaScript.ExpressionCompiler.Expressions.YParameterExpression;
using LambdaExpression = Broiler.JavaScript.ExpressionCompiler.Expressions.YLambdaExpression;
using LabelTarget = Broiler.JavaScript.ExpressionCompiler.Expressions.YLabelTarget;
using GotoExpression = Broiler.JavaScript.ExpressionCompiler.Expressions.YGoToExpression;
using TryExpression = Broiler.JavaScript.ExpressionCompiler.Expressions.YTryCatchFinallyExpression;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.ExpressionCompiler.ClosureSeparator;
using Broiler.JavaScript.Runtime;
using CoreReferenceEqualityComparer = Broiler.JavaScript.ExpressionCompiler.Core.ReferenceEqualityComparer;

namespace Broiler.JavaScript.LinqExpressions.LinqExpressions.GeneratorsV2;



public class GeneratorRewriter(ParameterExpression pe, LabelTarget @return, ParameterExpression replaceArguments, ParameterExpression replaceStackItem, ParameterExpression replaceContext, ParameterExpression replaceScriptInfo) : YExpressionMapVisitor
{
    private readonly ParameterExpression args = Expression.Parameter(typeof(Arguments).MakeByRefType(), "args");
    private readonly ParameterExpression nextJump = Expression.Parameter(typeof(int), "nextJump");
    private readonly ParameterExpression nextValue = Expression.Parameter(typeof(JSValue), "nextValue");
    private readonly ParameterExpression exception = Expression.Parameter(typeof(Exception), "ex");
    private readonly YFieldExpression Context = Expression.Field(pe, "Context");
    private readonly ParameterExpression _replaceScriptInfo = replaceScriptInfo;
    private readonly ParameterExpression _scriptInfoBox = Expression.Parameter(typeof(Box<ScriptInfo>), "scriptInfo");
    private readonly YFieldExpression StackItem = Expression.Field(pe, "StackItem");
    private readonly LabelTarget generatorReturn = Expression.Label(typeof(GeneratorState), "RETURN");
    private readonly Sequence<(ParameterExpression original, ParameterExpression box, int index, Expression boxField)> lifted = [];

    // private readonly ParameterExpression replaceScriptInfo;
    private Sequence<(LabelTarget label, int id)> jumps = [];

    public static LambdaExpression Rewrite(in FunctionName name, Expression body, LabelTarget r, ParameterExpression generator, ParameterExpression replaceArgs,
       ParameterExpression replaceStackItem, ParameterExpression replaceContext, ParameterExpression replaceScriptInfo)
    {
       var gw = new GeneratorRewriter(generator, r, replaceArgs, replaceStackItem, replaceContext, replaceScriptInfo);
       gw.AddScriptInfoCapture();

       body = MethodRewriter.Rewrite(body);

        var flatten = new FlattenBlocks();
        var innerBody = flatten.Visit(gw.Visit(body));

        // setup jump table...

        var @break = Expression.Label("generatorEnd");
        var jumpExp = gw.GenerateJumps(@break);
        var (boxes, inits) = gw.LoadBoxes();

        YBlockExpression newBody;

        if (boxes == null)
        {
            newBody = Expression.Block(jumpExp, innerBody, Expression.Label(gw.generatorReturn, GeneratorStateBuilder.New(0)));
        }
        else
        {
            newBody = Expression.Block(boxes, inits, jumpExp, Expression.Label(@break), innerBody, Expression.Label(gw.generatorReturn, GeneratorStateBuilder.New(0)));
        }

        return Expression.Lambda<JSGeneratorDelegateV2>(in name, newBody, generator, gw.args, gw.nextJump, gw.nextValue, gw.exception);
    }

    private void AddScriptInfoCapture()
    {
        if (_replaceScriptInfo == null)
            return;

        lifted.Add((_replaceScriptInfo, _scriptInfoBox, lifted.Count, Expression.Field(_scriptInfoBox, "Value")));
    }

    private (Sequence<ParameterExpression> boxes, Expression init) LoadBoxes()
    {
        var boxes = new Sequence<Expression>(lifted.Count) { ClrGeneratorV2Builder.InitVariables(pe, lifted.Count) };
        var vlist = new Sequence<ParameterExpression>(lifted.Count);

        foreach (var (original, box, index, _) in lifted)
        {
            vlist.Add(box);
            boxes.Add(Expression.Assign(box, ClrGeneratorV2Builder.GetVariable(pe, index, original.Type)));
            if (original == _replaceScriptInfo)
                boxes.Add(Expression.Assign(Expression.Field(_scriptInfoBox, "Value"), _replaceScriptInfo));
        }

        if (vlist.Count == 0)
            return (null, null);

        return (vlist, Expression.Block(boxes));
    }

    private Expression GenerateJumps(LabelTarget @break)
    {
        if (jumps.Count == 0)
            return Expression.Empty;

        var cases = new Sequence<LabelTarget>();
        var offset = 1;

        jumps = [.. jumps.OrderBy(x => x.id)];

        var en = jumps.GetFastEnumerator();

        while (en.MoveNext(out var jump, out var i))
        {
            var (label, id) = jump;
            var index = id + offset;

            // this will fill the gap in between jumps, if any
            while (index > cases.Count)
                cases.Add(@break);

            cases.Add(label);
        }

        return Expression.JumpSwitch(nextJump + offset, cases);
    }

    protected override Expression VisitBlock(YBlockExpression node)
    {
        if (!node.HasYield())
            return base.VisitBlock(node);

        var list = new Sequence<Expression>(node.Variables.Count + node.Expressions.Count);
        var retainedVariables = new Sequence<ParameterExpression>();
        var nonYieldingCatchParameters = NonYieldingCatchParameterFinder.Find(node);
        var ve = node.Variables.GetFastEnumerator();

        while (ve.MoveNext(out var v))
        {
            if (v == _replaceScriptInfo)
            {
                retainedVariables.Add(v);
                continue;
            }

            if (nonYieldingCatchParameters.Contains(v))
            {
                retainedVariables.Add(v);
                continue;
            }

            int index = lifted.Count;
            var box = Expression.Parameter(typeof(Box<>).MakeGenericType(v.Type));
            lifted.Add((v, box, index, Expression.Field(box, "Value")));
        }

        var vne = node.Expressions.GetFastEnumerator();
        while (vne.MoveNext(out var s))
            list.Add(Visit(s));
        if (node.Type == typeof(void) && (list.Count == 0 || list[^1].Type != typeof(void)))
            list.Add(Expression.Empty);

        return retainedVariables.Count == 0
            ? Expression.Block(list)
            : Expression.Block(retainedVariables, list);
    }


    private sealed class NonYieldingCatchParameterFinder : YExpressionMapVisitor
    {
        private readonly HashSet<ParameterExpression> parameters = new(CoreReferenceEqualityComparer.Instance);

        public static HashSet<ParameterExpression> Find(YBlockExpression block)
        {
            var finder = new NonYieldingCatchParameterFinder();
            var en = block.Expressions.GetFastEnumerator();
            while (en.MoveNext(out var expression))
                finder.Visit(expression);

            return finder.parameters;
        }

        protected override Expression VisitTryCatchFinally(TryExpression node)
        {
            if (node.Catch?.Parameter != null && !node.HasYield())
                parameters.Add(node.Catch.Parameter);

            return base.VisitTryCatchFinally(node);
        }

        protected override Expression VisitLambda(LambdaExpression yLambdaExpression) => yLambdaExpression;
    }

    protected override Exp VisitReturn(YReturnExpression node)
    {
        if (node.Default == null || node.Default.NodeType != YExpressionType.Yield)
            return Expression.Return(generatorReturn, GeneratorStateBuilder.New(Visit(node.Default), -1));

        // return yield case... need to expand..
        // Preserve the suspension kind: `return yield* X` must delegate, and
        // `return await X` (async) must be treated as an await — otherwise the
        // flags are lost and the operand surfaces as a plain yield value.
        var yield = node.Default as YYieldExpression;
        var arg = Visit(yield.Argument);
        var (label, id) = GetNextYieldJumpTarget();

        return Expression.Block(Expression.Return(generatorReturn, GeneratorStateBuilder.New(arg, id, yield.DelegateYield, yield.IsAwait)), Expression.Label(label),
            Expression.Return(generatorReturn, GeneratorStateBuilder.New(nextValue, -1)));
    }

    protected override Expression VisitGoto(GotoExpression node) => base.VisitGoto(node);

    protected override Expression VisitParameter(ParameterExpression node)
    {
        if (node == replaceArguments)
            return args;

        if (node == replaceContext)
            return Context;

        if (node == replaceStackItem)
            return StackItem;

        if (node == _replaceScriptInfo)
            return Expression.Field(_scriptInfoBox, "Value");

        foreach (var (original, _, _, boxField) in lifted)
        {
            if (original == node)
                return boxField;
        }

        return base.VisitParameter(node);
    }

    private (LabelTarget label, int id) GetNextYieldJumpTarget()
    {
        int id = jumps.Count + 1;
        var label = Expression.Label(typeof(void), "next" + id);
        var r = (label, id);
        jumps.Add(r);
        return r;
    }

    protected override Exp VisitYield(YYieldExpression node)
    {
        var arg = Visit(node.Argument);
        var (label, id) = GetNextYieldJumpTarget();

        return Expression.Block(Expression.Return(generatorReturn, GeneratorStateBuilder.New(arg, id, node.DelegateYield, node.IsAwait)), Expression.Label(label), nextValue);
    }

    protected override Exp VisitConditional(YConditionalExpression node)
    {
        var conditional = base.VisitConditional(node);
        if (conditional is not YConditionalExpression rewritten)
            return conditional;

        static Exp ToVoid(Exp expression) => expression.Type == typeof(void)
            ? expression
            : Expression.Block(expression, Expression.Empty);

        if (node.Type == typeof(void))
        {
            return new YConditionalExpression(
                rewritten.test,
                ToVoid(rewritten.@true),
                rewritten.@false == null ? null : ToVoid(rewritten.@false),
                typeof(void));
        }

        // A value-producing conditional that contains a yield/return suspension is
        // unsafe: the suspension lowers to `return state; <jump label>; value`, and
        // when this conditional is the operand of an enclosing expression (e.g. the
        // RHS of the completion-tracking assignment `#cv = if (c) yield x`), the
        // resume `goto` lands in the middle of evaluating that operand. The setup of
        // the enclosing expression (the assignment target, prior call arguments,
        // etc.) is skipped, corrupting the IL stack and faulting at runtime.
        // FlattenBlocks already hoists suspensions out of `target = block(...)`, so
        // make the branches statements: spill the conditional's value into a temp and
        // distribute the production into each branch (`if (c) temp = A else temp = B;
        // temp`). Each yield is now at statement level inside a branch where
        // FlattenBlocks can hoist it cleanly.
        if (rewritten.@false == null || !node.HasYield())
            return conditional;

        var temp = Expression.Parameter(node.Type);
        return Expression.Block(
            new Sequence<ParameterExpression> { temp },
            new YConditionalExpression(
                rewritten.test,
                ToVoid(Expression.Assign(temp, rewritten.@true)),
                ToVoid(Expression.Assign(temp, rewritten.@false)),
                typeof(void)),
            temp);
    }

    protected override Exp VisitLambda(LambdaExpression yLambdaExpression)
    {
        // we need to rewrite nested lambda to replace `this` or closures
        // with boxes...

        var replaces = lifted.ToDictionary((x) => (Expression)x.original, x => x.boxField);
        var parameterReplacer = new ReplaceParameters(replaces);

        return parameterReplacer.Visit(yLambdaExpression);
    }

    protected override Exp VisitTryCatchFinally(TryExpression node)
    {
        if (!node.HasYield())
            return base.VisitTryCatchFinally(node);

        var hasFinally = node.Finally != null;
        var @catch = node.Catch;
        var hasCatch = @catch != null;

        // A value-producing try/catch/finally is flattened below into a goto-driven state
        // machine whose tail is `Pop` (void). The try/catch value (a try/catch/finally
        // evaluates to its protected block's — or catch block's — normal completion, the
        // finally's value being discarded) would therefore be dropped, leaving the
        // rewritten block typed `void` while the enclosing IL still expects the original
        // type — an unbalanced stack that faults as an invalid program (e.g. `await using`
        // followed by another statement inside a loop). Spill that value into a lifted temp
        // so it survives the yield(s) in the finally / resume, and produce it as the
        // rewritten block's result so the node's type is preserved.
        Expression resultStore = null;
        if (node.Type != typeof(void))
        {
            var index = lifted.Count;
            var original = Expression.Parameter(node.Type);
            var box = Expression.Parameter(typeof(Box<>).MakeGenericType(node.Type));
            resultStore = Expression.Field(box, "Value");
            lifted.Add((original, box, index, resultStore));
        }

        Expression Store(Expression value)
            => resultStore != null ? Expression.Assign(resultStore, value) : value;

        LabelTarget catchLabel = null;
        int catchId = 0;
        LabelTarget finallyLabel = null;
        int finallyId = 0;

        var tryList = new YBlockBuilder();
        if (hasCatch)
            (catchLabel, catchId) = GetNextYieldJumpTarget();

        if (hasFinally)
            (finallyLabel, finallyId) = GetNextYieldJumpTarget();

        var (endLabel, endId) = GetNextYieldJumpTarget();

        tryList.AddExpression(ClrGeneratorV2Builder.Push(pe, catchId, finallyId, endId));
        tryList.AddExpression(Store(Visit(node.Try)));
        tryList.AddExpression(Expression.Goto(hasFinally ? finallyLabel : endLabel));

        if (hasCatch)
        {
            tryList.AddExpression(Expression.Label(catchLabel));
            tryList.AddExpression(ClrGeneratorV2Builder.BeginCatch(pe));
            tryList.AddExpression(Expression.Assign(Visit(@catch.Parameter), exception));
            tryList.AddExpression(Store(Visit(@catch.Body)));
            tryList.AddExpression(Expression.Empty);
            tryList.AddExpression(Expression.Goto(hasFinally ? finallyLabel : endLabel));
        }

        if (hasFinally)
        {
            tryList.AddExpression(Expression.Label(finallyLabel));
            tryList.AddExpression(ClrGeneratorV2Builder.BeginFinally(pe));
            tryList.AddExpression(Visit(node.Finally));
            tryList.AddExpression(ClrGeneratorV2Builder.Throw(pe, endId));
        }

        tryList.AddExpression(Expression.Label(endLabel));
        tryList.AddExpression(ClrGeneratorV2Builder.Pop(pe));

        // Produce the preserved try/catch value as the rewritten block's result.
        if (resultStore != null)
            tryList.AddExpression(resultStore);

        var b = tryList.Build();
        return b;
    }
}
