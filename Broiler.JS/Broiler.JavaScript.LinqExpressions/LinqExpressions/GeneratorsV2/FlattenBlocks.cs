using System;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Exp = Broiler.JavaScript.ExpressionCompiler.Expressions.YExpression;
using Expression = Broiler.JavaScript.ExpressionCompiler.Expressions.YExpression;
using ParameterExpression = Broiler.JavaScript.ExpressionCompiler.Expressions.YParameterExpression;

namespace Broiler.JavaScript.LinqExpressions.LinqExpressions.GeneratorsV2;

public class FlattenBlocks : YExpressionMapVisitor
{
    protected override Exp VisitLambda(YLambdaExpression yLambdaExpression) => yLambdaExpression;

    protected override Expression VisitBinary(YBinaryExpression node)
    {
        if (Flatten(node.Right, last => node.Update(node.Left, node.Operator, last), out var result))
            return result;

        return base.VisitBinary(node);
    }

    protected override Exp VisitAssign(YAssignExpression node)
    {
        if (Flatten(node.Right, last => new YAssignExpression(node.Left, last, null), out var result))
            return result;

        return base.VisitAssign(node);
    }

    protected override Exp VisitReturn(YReturnExpression node)
    {
        if (Flatten(node.Default, x => node.Update(node.Target, x), out var block))
            return block;

        return base.VisitReturn(node);
    }

    protected override Expression VisitNew(YNewExpression node)
    {
        var vars = new Sequence<ParameterExpression>();
        var args = new Sequence<Expression>();
        var list = new Sequence<Expression>();
        var argEn = node.args.GetFastEnumerator();

        while (argEn.MoveNext(out var a))
        {
            var e = Visit(a);

            if (e.NodeType == YExpressionType.Block && e is YBlockExpression block)
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

    protected override Expression VisitBlock(YBlockExpression node)
    {
        var vars = new Sequence<ParameterExpression>(node.Variables);
        var list = new Sequence<Expression>(node.Expressions.Count);
        var en = node.Expressions.GetFastEnumerator();

        while (en.MoveNext(out var e))
        {
            var visited = Visit(e);
            if (visited.NodeType == YExpressionType.Block && visited is YBlockExpression block)
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
        if (exp is YTypeAsExpression typeAs && typeAs.Target.NodeType == YExpressionType.Block)
        {
            var targetType = typeAs.Type;
            return Flatten(typeAs.Target,
                last => p(last.Type == targetType ? last : YExpression.Convert(last, targetType)),
                out result);
        }

        if (exp.NodeType != YExpressionType.Block)
        {
            result = null;
            return false;
        }

        var block = exp as YBlockExpression;

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
                if (Flatten(visited, p, out var nested) && nested is YBlockExpression nestedBlock)
                {
                    vars.AddRange(nestedBlock.Variables);
                    list.AddRange(nestedBlock.Expressions);
                }
                else if (visited.NodeType == YExpressionType.TryCatchFinally && visited.Type != typeof(void))
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
