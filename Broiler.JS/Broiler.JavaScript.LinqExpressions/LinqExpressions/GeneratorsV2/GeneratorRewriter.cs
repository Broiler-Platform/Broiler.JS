using System;
using System.Collections.Generic;
using System.Linq;
using Exp = Broiler.JavaScript.ExpressionCompiler.Expressions.BExpression;
using Expression = Broiler.JavaScript.ExpressionCompiler.Expressions.BExpression;
using ParameterExpression = Broiler.JavaScript.ExpressionCompiler.Expressions.BParameterExpression;
using LambdaExpression = Broiler.JavaScript.ExpressionCompiler.Expressions.BLambdaExpression;
using LabelTarget = Broiler.JavaScript.ExpressionCompiler.Expressions.BLabelTarget;
using GotoExpression = Broiler.JavaScript.ExpressionCompiler.Expressions.BGoToExpression;
using TryExpression = Broiler.JavaScript.ExpressionCompiler.Expressions.BTryCatchFinallyExpression;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.ExpressionCompiler.ClosureSeparator;
using Broiler.JavaScript.Runtime;
using CoreReferenceEqualityComparer = Broiler.JavaScript.ExpressionCompiler.Core.ReferenceEqualityComparer;

namespace Broiler.JavaScript.LinqExpressions.LinqExpressions.GeneratorsV2;



public class GeneratorRewriter(ParameterExpression pe, LabelTarget @return, ParameterExpression replaceArguments, ParameterExpression replaceStackItem, ParameterExpression replaceContext, ParameterExpression replaceScriptInfo) : BExpressionMapVisitor
{
    private readonly ParameterExpression args = Expression.Parameter(typeof(Arguments).MakeByRefType(), "args");
    private readonly ParameterExpression nextJump = Expression.Parameter(typeof(int), "nextJump");
    private readonly ParameterExpression nextValue = Expression.Parameter(typeof(JSValue), "nextValue");
    private readonly ParameterExpression exception = Expression.Parameter(typeof(Exception), "ex");
    private readonly BFieldExpression Context = Expression.Field(pe, "Context");
    private readonly ParameterExpression _replaceScriptInfo = replaceScriptInfo;
    private readonly ParameterExpression _scriptInfoBox = Expression.Parameter(typeof(Box<ScriptInfo>), "scriptInfo");
    private readonly BFieldExpression StackItem = Expression.Field(pe, "StackItem");
    private readonly LabelTarget generatorReturn = Expression.Label(typeof(GeneratorState), "RETURN");
    private readonly Sequence<(ParameterExpression original, ParameterExpression box, int index, Expression boxField)> lifted = [];

    // Tracks which originals have already been lifted into a box. A temp variable produced by the
    // compiler can be reused across sibling scopes (e.g. the two `[yield …]` computed property
    // names of a class declared inside a generator share one temp), so the same ParameterExpression
    // can appear in more than one yield-containing block's variable list. Lifting it twice would
    // add a duplicate entry to `lifted` and make the original→box ToDictionary throw on the
    // colliding key — so each original is boxed exactly once and later references reuse that box.
    private readonly HashSet<ParameterExpression> liftedOriginals = new(CoreReferenceEqualityComparer.Instance);

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

        BBlockExpression newBody;

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
        liftedOriginals.Add(_replaceScriptInfo);
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
            {
                // Seed the ScriptInfo box from the incoming `scriptInfo` local — but ONLY on the
                // first entry (`nextJump == 0`). This box-load prologue runs on *every* (re)entry,
                // including each await/yield resume, and the seed value `_replaceScriptInfo` is a
                // body-local whose only writes were redirected into `_scriptInfoBox.Value`, so as a
                // bare local it is always its default (null) at prologue time. Seeding unconditionally
                // therefore clobbers, on every resume, the ScriptInfo the box persisted from the first
                // run — including its `Indices` key table — back to null; any post-resume identifier /
                // member access that resolves a name through `scriptInfo.Value.Indices[…]` then
                // dereferences null (constant receivers and bare globals resolve via constant
                // KeyStrings and survive, which made the fault look receiver-shaped). Guarding on
                // `nextJump == 0` keeps the first-entry seed that nested async/generator functions rely
                // on, while preserving the persisted value across every resume.
                boxes.Add(Expression.IfThen(
                    Expression.Equal(nextJump, Expression.Constant(0)),
                    Expression.Block(
                        Expression.Assign(Expression.Field(_scriptInfoBox, "Value"), _replaceScriptInfo),
                        Expression.Empty)));
            }
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

    protected override Expression VisitBlock(BBlockExpression node)
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

            // A compiler temp may be declared by more than one yield-containing sibling block; box
            // it once and let later references resolve through the existing box (see liftedOriginals).
            if (!liftedOriginals.Add(v))
                continue;

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


    private sealed class NonYieldingCatchParameterFinder : BExpressionMapVisitor
    {
        private readonly HashSet<ParameterExpression> parameters = new(CoreReferenceEqualityComparer.Instance);

        public static HashSet<ParameterExpression> Find(BBlockExpression block)
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

    protected override Exp VisitReturn(BReturnExpression node)
    {
        if (node.Default == null || node.Default.NodeType != BExpressionType.Yield)
            return Expression.Return(generatorReturn, GeneratorStateBuilder.New(Visit(node.Default), -1));

        // return yield case... need to expand..
        // Preserve the suspension kind: `return yield* X` must delegate, and
        // `return await X` (async) must be treated as an await — otherwise the
        // flags are lost and the operand surfaces as a plain yield value.
        var yield = node.Default as BYieldExpression;
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

    protected override Exp VisitYield(BYieldExpression node)
    {
        var arg = Visit(node.Argument);
        var (label, id) = GetNextYieldJumpTarget();

        return Expression.Block(Expression.Return(generatorReturn, GeneratorStateBuilder.New(arg, id, node.DelegateYield, node.IsAwait)), Expression.Label(label), nextValue);
    }

    protected override Exp VisitConditional(BConditionalExpression node)
    {
        var conditional = base.VisitConditional(node);
        if (conditional is not BConditionalExpression rewritten)
            return conditional;

        static Exp ToVoid(Exp expression) => expression.Type == typeof(void)
            ? expression
            : Expression.Block(expression, Expression.Empty);

        if (node.Type == typeof(void))
        {
            return new BConditionalExpression(
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
            new BConditionalExpression(
                rewritten.test,
                ToVoid(Expression.Assign(temp, rewritten.@true)),
                ToVoid(Expression.Assign(temp, rewritten.@false)),
                typeof(void)),
            temp);
    }

    /// <summary>
    /// `a &amp;&amp; b`, `a || b` and `a ?? b` all reach the backend as this node — the JavaScript
    /// operators lower to <c>Coalesce(NullIfTrue/NullIfFalse/NullIfNullOrUndefined(a), b)</c> —
    /// and it emits `left; dup; brtrue end; pop; right; end:`.
    /// </summary>
    /// <remarks>
    /// A suspension inside either operand lowers to `return state; &lt;jump label&gt;; value`, so
    /// the resume `goto` lands in the middle of that sequence: past the `dup`/`pop` pair the
    /// emitter's stack bookkeeping recorded for the label, on a re-entry whose evaluation stack is
    /// empty. It is the hazard the value-producing conditional above describes, and it gets the
    /// same answer — spill into a temp and make the second operand a statement, so each suspension
    /// sits at statement level where FlattenBlocks can hoist it out cleanly. Only reached when the
    /// node really does contain one: an ordinary `a || b` keeps the short-circuit emit.
    /// </remarks>
    protected override Exp VisitCoalesce(BCoalesceExpression node)
    {
        var coalesce = base.VisitCoalesce(node);
        if (!node.HasYield() || coalesce is not BCoalesceExpression rewritten)
            return coalesce;

        var temp = Expression.Parameter(node.Type);
        return Expression.Block(
            new Sequence<ParameterExpression> { temp },
            Expression.Assign(temp, rewritten.Left),
            new BConditionalExpression(
                // Null first: BBinaryExpression types a comparison only when the left type is
                // assignable from the right, and `object` is assignable from `JSValue`.
                Expression.Equal(Expression.Null, temp),
                Expression.Block(Expression.Assign(temp, rewritten.Right), Expression.Empty),
                null,
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
            liftedOriginals.Add(original);
        }

        Expression Store(Expression value)
            => resultStore != null ? Expression.Assign(resultStore, value) : value;

        LabelTarget catchLabel = null;
        int catchId = 0;
        LabelTarget finallyLabel = null;
        int finallyId = 0;

        var tryList = new BBlockBuilder();
        if (hasCatch)
            (catchLabel, catchId) = GetNextYieldJumpTarget();

        if (hasFinally)
            (finallyLabel, finallyId) = GetNextYieldJumpTarget();

        var (endLabel, endId) = GetNextYieldJumpTarget();

        var tryBody = Store(Visit(node.Try));
        var catchParameter = hasCatch ? Visit(@catch.Parameter) : null;
        var catchBody = hasCatch ? Store(Visit(@catch.Body)) : null;
        var finallyBody = hasFinally ? Visit(node.Finally) : null;

        // A break/continue leaving the statement must leave its region the way a CLR `leave`
        // leaves a try: through the finally, if there is one.
        var exits = new RegionExitRewriter(pe, endId, finallyLabel, tryBody, catchBody, finallyBody);
        tryBody = exits.RewriteTryOrCatch(tryBody);
        if (hasCatch)
            catchBody = exits.RewriteTryOrCatch(catchBody);
        if (hasFinally)
            finallyBody = exits.RewriteFinally(finallyBody);

        tryList.AddExpression(ClrGeneratorV2Builder.Push(pe, catchId, finallyId, endId));
        tryList.AddExpression(tryBody);
        tryList.AddExpression(Expression.Goto(hasFinally ? finallyLabel : endLabel));

        if (hasCatch)
        {
            tryList.AddExpression(Expression.Label(catchLabel));
            tryList.AddExpression(ClrGeneratorV2Builder.BeginCatch(pe));
            tryList.AddExpression(Expression.Assign(catchParameter, exception));
            tryList.AddExpression(catchBody);
            tryList.AddExpression(Expression.Empty);
            tryList.AddExpression(Expression.Goto(hasFinally ? finallyLabel : endLabel));
        }

        if (hasFinally)
        {
            tryList.AddExpression(Expression.Label(finallyLabel));
            tryList.AddExpression(ClrGeneratorV2Builder.BeginFinally(pe));
            tryList.AddExpression(finallyBody);
            // The finally completed normally, so the completion that entered it resumes (see
            // ClrGeneratorV2.EndFinally): with none pending, execution continues after the try
            // statement; a pending exception is re-thrown; a pending return completion returns
            // from the body right here, as a `return` would, for GetNext to carry through any
            // enclosing finally; a pending jump continues to its target. That last goto leaves
            // any enclosing try statement too, whose rewrite routes it through its own finally.
            var pendingReturn = Expression.Label(typeof(void), "pendingReturn");
            var dispatch = new Sequence<LabelTarget> { endLabel, pendingReturn };
            var jumps = new Sequence<Expression>();
            foreach (var target in exits.JumpTargets)
            {
                var jump = Expression.Label(typeof(void), "pendingJump");
                dispatch.Add(jump);
                jumps.Add(Expression.Label(jump));
                jumps.Add(Expression.Goto(target));
            }

            tryList.AddExpression(Expression.JumpSwitch(ClrGeneratorV2Builder.EndFinally(pe, endId), dispatch));
            tryList.AddExpression(Expression.Label(pendingReturn));
            tryList.AddExpression(Expression.Return(generatorReturn, GeneratorStateBuilder.New(ClrGeneratorV2Builder.TakeReturnValue(pe), -1)));
            foreach (var jump in jumps)
                tryList.AddExpression(jump);
        }

        tryList.AddExpression(Expression.Label(endLabel));
        tryList.AddExpression(ClrGeneratorV2Builder.Pop(pe));

        // Produce the preserved try/catch value as the rewritten block's result.
        if (resultStore != null)
            tryList.AddExpression(resultStore);

        var b = tryList.Build();
        return b;
    }

    /// <summary>
    /// Routes the jumps that leave a lowered try statement: a break, continue or labelled break
    /// whose target is outside the statement.
    /// </summary>
    /// <remarks>
    /// A try statement that holds a yield is lowered to a try region, and an ordinary goto out of
    /// it skipped the finally and left the region on the stack, where it later ran that finally
    /// out of turn or caught an exception thrown outside it. A goto out of the try or catch block
    /// of a statement with a finally now records its target on the region and runs the finally,
    /// whose end resumes the jump (see <see cref="ClrGeneratorV2.EndFinally"/>). Any other goto out
    /// of the statement (from a try/catch without a finally, or from the finally block, whose
    /// jump overrides the pending completion) first leaves the region. Targets are told apart the
    /// way FinallyBranchScanner does: a label declared inside the statement is internal.
    /// </remarks>
    private sealed class RegionExitRewriter : BExpressionMapVisitor
    {
        private readonly ParameterExpression pe;
        private readonly int endId;
        private readonly LabelTarget finallyLabel;
        private readonly HashSet<LabelTarget> internalLabels = [];
        private bool inFinally;

        // The targets of jumps that wait for the finally, in dispatch order: the jump recorded
        // as n resumes at JumpTargets[n - 1].
        public readonly Sequence<LabelTarget> JumpTargets = [];

        public RegionExitRewriter(ParameterExpression pe, int endId, LabelTarget finallyLabel, params Expression[] bodies)
        {
            this.pe = pe;
            this.endId = endId;
            this.finallyLabel = finallyLabel;

            var labels = new LabelCollector(internalLabels);
            foreach (var body in bodies)
            {
                if (body != null)
                    labels.Visit(body);
            }
        }

        public Expression RewriteTryOrCatch(Expression body)
        {
            inFinally = false;
            return Visit(body);
        }

        public Expression RewriteFinally(Expression body)
        {
            inFinally = true;
            return Visit(body);
        }

        protected override Expression VisitGoto(GotoExpression node)
        {
            // A goto carrying a value is an expression-level exit, not a statement leaving the try.
            if (node.Default != null || internalLabels.Contains(node.Target))
                return base.VisitGoto(node);

            if (inFinally || finallyLabel == null)
                return Expression.Block(ClrGeneratorV2Builder.Leave(pe, endId), node);

            var jump = 0;
            var en = JumpTargets.GetFastEnumerator();
            while (en.MoveNext(out var target, out var i))
            {
                if (target == node.Target)
                {
                    jump = i + 1;
                    break;
                }
            }

            if (jump == 0)
            {
                JumpTargets.Add(node.Target);
                jump = JumpTargets.Count;
            }

            return Expression.Block(ClrGeneratorV2Builder.LeaveTry(pe, endId, jump), Expression.Goto(finallyLabel));
        }

        // A nested function has its own labels and jumps.
        protected override Expression VisitLambda(LambdaExpression node) => node;

        private sealed class LabelCollector(HashSet<LabelTarget> labels) : BExpressionMapVisitor
        {
            protected override Expression VisitLabel(BLabelExpression node)
            {
                labels.Add(node.Target);
                return base.VisitLabel(node);
            }

            protected override Expression VisitLoop(BLoopExpression node)
            {
                labels.Add(node.Break);
                labels.Add(node.Continue);
                return base.VisitLoop(node);
            }

            protected override Expression VisitLambda(LambdaExpression node) => node;
        }
    }
}
