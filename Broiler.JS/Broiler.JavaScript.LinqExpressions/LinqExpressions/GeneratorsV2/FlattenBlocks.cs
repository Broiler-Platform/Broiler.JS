using System;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Exp = Broiler.JavaScript.ExpressionCompiler.Expressions.BExpression;
using Expression = Broiler.JavaScript.ExpressionCompiler.Expressions.BExpression;
using ParameterExpression = Broiler.JavaScript.ExpressionCompiler.Expressions.BParameterExpression;

namespace Broiler.JavaScript.LinqExpressions.LinqExpressions.GeneratorsV2;

public class FlattenBlocks : BExpressionMapVisitor
{
    protected override Exp VisitLambda(BLambdaExpression yLambdaExpression) => yLambdaExpression;

    protected override Expression VisitBinary(BBinaryExpression node)
        => HoistValueOperand(node.Right, last => node.Update(node.Left, node.Operator, last));

    protected override Exp VisitAssign(BAssignExpression node)
        => HoistValueOperand(node.Right, last => new BAssignExpression(node.Left, last, null));

    protected override Exp VisitReturn(BReturnExpression node)
    {
        if (node.Default == null)
            return base.VisitReturn(node);

        return HoistValueOperand(node.Default, x => node.Update(node.Target, x));
    }

    // Flatten a value operand consumed by `p` (an assignment RHS, a binary right operand, a
    // return value, …). A raw block operand is flattened directly, as before. But an operand
    // that is NOT itself a block may still HOIST into one when visited — e.g. an array
    // destructuring assignment in value position lowers, via VisitCall, into a block carrying
    // the iterator-close try/catch/finally. Left in this value position the embedded try faults
    // (the CLR forbids entering a try region with a non-empty evaluation stack), so visit the
    // operand and flatten the result too, hoisting its statements out as siblings.
    private Expression HoistValueOperand(Expression rawExp, Func<Expression, Expression> p)
    {
        if (Flatten(rawExp, p, out var result))
            return result;

        var visited = Visit(rawExp);
        if (visited.NodeType == BExpressionType.Block && Flatten(visited, p, out var hoisted))
            return hoisted;

        return p(visited);
    }

    protected override Expression VisitNew(BNewExpression node)
    {
        var vars = new Sequence<ParameterExpression>();
        var args = new Sequence<Expression>();
        var list = new Sequence<Expression>();
        var argEn = node.args.GetFastEnumerator();

        while (argEn.MoveNext(out var a))
        {
            var e = Visit(a);

            if (e.NodeType == BExpressionType.Block && e is BBlockExpression block)
            {
                vars.AddRange(block.Variables);

                var p = Expression.Parameter(e.Type);
                vars.Add(p);
                args.Add(p);

                var length = block.Expressions.Count;
                var last = length - 1;
                var en = block.Expressions.GetFastEnumerator();

                while (en.MoveNext(out var exp, out var i))
                {
                    var be = Visit(exp);

                    if (i == last)
                    {
                        list.Add(Expression.Assign(p, be));
                        continue;
                    }

                    list.Add(be);
                }

                continue;
            }

            args.Add(e);
        }

        if (vars.Count == 0)
            return Expression.New(node.constructor, args);

        list.Add(Expression.New(node.constructor, args));
        return Expression.Block(vars, list);
    }

    protected override Expression VisitCall(BCallExpression node)
    {
        // A method-call operand (instance target or argument) may itself be a block — e.g. an
        // array destructuring assignment used in value position (`y = ([a] = [1])`) lowers to a
        // block that contains an iterator-close try/catch/finally and returns the source value.
        // The CLR requires an empty evaluation stack when entering a try region, but a block used
        // directly as a call operand is evaluated while the target / preceding arguments are already
        // on the stack, so the embedded try faults as an invalid program. Hoist each block operand's
        // statements out as siblings and spill its tail value into a temp, then call with the temps —
        // exactly as VisitNew already does for constructor arguments. (Object destructuring uses no
        // iterator and so produced no try, which is why only the array form crashed.)
        var vars = new Sequence<ParameterExpression>();
        var list = new Sequence<Expression>();

        Expression HoistOperand(Expression operand)
        {
            var e = Visit(operand);

            if (e.NodeType != BExpressionType.Block || e is not BBlockExpression block)
                return e;

            vars.AddRange(block.Variables);
            var p = Expression.Parameter(e.Type);
            vars.Add(p);

            var length = block.Expressions.Count;
            var last = length - 1;
            var en = block.Expressions.GetFastEnumerator();
            while (en.MoveNext(out var exp, out var i))
            {
                if (i == last)
                {
                    list.Add(Expression.Assign(p, exp));
                    continue;
                }

                list.Add(exp);
            }

            return p;
        }

        var target = node.Target == null ? null : HoistOperand(node.Target);
        var args = new Sequence<Expression>(node.Arguments.Count);
        var argEn = node.Arguments.GetFastEnumerator();
        while (argEn.MoveNext(out var a))
            args.Add(HoistOperand(a));

        if (vars.Count == 0)
            return new BCallExpression(target, node.Method, args);

        list.Add(new BCallExpression(target, node.Method, args));
        return Expression.Block(vars, list);
    }

    protected override Expression VisitBlock(BBlockExpression node)
    {
        var vars = new Sequence<ParameterExpression>(node.Variables);
        var list = new Sequence<Expression>(node.Expressions.Count);
        var en = node.Expressions.GetFastEnumerator();

        while (en.MoveNext(out var e))
        {
            var visited = Visit(e);
            if (visited.NodeType == BExpressionType.Block && visited is BBlockExpression block)
            {
                vars.AddRange(block.Variables);
                list.AddRange(block.Expressions);
                continue;
            }

            list.Add(visited);
        }

        return Expression.Block(vars, list);
    }

    private bool Flatten(Expression exp, Func<Expression, Expression> p, out Expression result)
    {
        // Look through a type-conversion wrapper (e.g. `{ … yield … } as JSValue`, which
        // arises when a try/catch's completion value is lifted into a generator box). The
        // wrapped block can carry a yield suspension — `return state; <jump label>; value` —
        // and leaving it buried inside `p`'s operand (a field store that pre-loads its
        // target) means the resume `goto` lands mid-store with the target reference missing,
        // faulting at runtime. Recurse into the operand and re-apply the conversion to the
        // tail value only, so the suspension's leading statements are hoisted out as siblings.
        if (exp is BTypeAsExpression typeAs && typeAs.Target.NodeType == BExpressionType.Block)
        {
            var targetType = typeAs.Type;
            return Flatten(typeAs.Target,
                last => p(last.Type == targetType ? last : BExpression.Convert(last, targetType)),
                out result);
        }

        if (exp.NodeType != BExpressionType.Block)
        {
            result = null;
            return false;
        }

        var block = exp as BBlockExpression;

        var vars = new Sequence<ParameterExpression>(block.Variables);
        var length = block.Expressions.Count;
        var list = new Sequence<Expression>(length);
        var last = length - 1;
        var en = block.Expressions.GetFastEnumerator();

        while (en.MoveNext(out var e, out var i))
        {
            if (last == i)
            {
                var visited = Visit(e);

                // The tail expression may itself be a block (e.g. a `yield`
                // rewritten to `Block(Return, Label, nextValue)`, nested an extra
                // level by braces such as `{ yield x; }`). Recurse so `p` is
                // applied to the innermost tail value and any leading statements —
                // notably the yield's Return branch — are hoisted out as siblings
                // instead of being left buried inside `p`'s result, which would
                // emit a mid-expression branch and produce invalid IL inside loops.
                if (Flatten(visited, p, out var nested) && nested is BBlockExpression nestedBlock)
                {
                    vars.AddRange(nestedBlock.Variables);
                    list.AddRange(nestedBlock.Expressions);
                }
                else if (visited.NodeType == BExpressionType.TryCatchFinally && visited.Type != typeof(void))
                {
                    // A value-producing try/catch/finally cannot be consumed in place by `p`
                    // when `p` pre-loads a target onto the evaluation stack — e.g. a store to
                    // a field, `boxField = <try>` (which arises when a completion value `#cv`
                    // is lifted into a generator box because it lives across a yield). CLR
                    // requires the field's target reference before the value, but the try's
                    // finally clears the stack, faulting as an invalid program. Spill the
                    // value into a temp first, then apply `p` to the temp.
                    var temp = Expression.Parameter(visited.Type);
                    vars.Add(temp);
                    list.Add(Expression.Assign(temp, visited));
                    list.Add(p(temp));
                }
                else
                {
                    list.Add(p(visited));
                }

                continue;
            }

            list.Add(Visit(e));
        }

        result = Expression.Block(vars, list);
        return true;
    }
}
