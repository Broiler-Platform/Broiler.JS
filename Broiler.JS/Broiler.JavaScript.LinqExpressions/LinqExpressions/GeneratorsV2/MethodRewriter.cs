using System.Collections.Generic;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Exp = Broiler.JavaScript.ExpressionCompiler.Expressions.BExpression;
using Expression = Broiler.JavaScript.ExpressionCompiler.Expressions.BExpression;

namespace Broiler.JavaScript.LinqExpressions.LinqExpressions.GeneratorsV2;


public class MethodRewriter : BExpressionMapVisitor
{
    public static Expression Rewrite(Expression exp)
    {
        var rw = new MethodRewriter();
        return rw.Visit(exp);
    }

    protected override Exp VisitLambda(BLambdaExpression yLambdaExpression) => yLambdaExpression;

    protected override Exp VisitAssign(BAssignExpression yAssignExpression)
    {
        var left = yAssignExpression.Left;
        var right = yAssignExpression.Right;

        // A yield inside the assignment target (e.g. `obj[yield] = v`) must be
        // hoisted into temporaries so the rewritten left stays a valid
        // assignable reference instead of collapsing into a block.
        if (left.NodeType != BExpressionType.Parameter && left.HasYield())
        {
            var bb = new BBlockBuilder();
            var newLeft = RewriteAssignTarget(left, bb);
            var newRight = bb.ConvertToVariable(Visit(right));
            bb.AddExpression(Expression.Assign(newLeft, newRight));
            return bb.Build();
        }

        // nested assign should be converted to block if it contains yield...
        if (right.NodeType == BExpressionType.Assign && right.HasYield())
        {
            // we need to break the right..
            right = BreakAssign(right as BAssignExpression);

            var bb = new BBlockBuilder();
            right = bb.ConvertToVariable(right);

            bb.AddExpression(Expression.Assign(Visit(yAssignExpression.Left), right));
            return bb.Build();
        }

        return base.VisitAssign(yAssignExpression);
    }

    // Rebuilds an assignment target that contains a yield expression, hoisting
    // the target object and any index arguments into temporaries (preserving
    // evaluation order) so the returned reference is still assignable.
    private Exp RewriteAssignTarget(Exp left, BBlockBuilder bb)
    {
        switch (left.NodeType)
        {
            case BExpressionType.Index:
                var index = (BIndexExpression)left;
                var indexTarget = bb.ConvertToVariable(Visit(index.Target));
                var args = new Sequence<Exp>(index.Arguments.Count);
                var ae = index.Arguments.GetFastEnumerator();
                while (ae.MoveNext(out var arg))
                    args.Add(bb.ConvertToVariable(Visit(arg)));
                return Expression.Index(indexTarget, index.Property, args);

            case BExpressionType.Property:
                var property = (BPropertyExpression)left;
                var propertyTarget = bb.ConvertToVariable(Visit(property.Target));
                return Expression.Property(propertyTarget, property.PropertyInfo);

            case BExpressionType.Field:
                var field = (BFieldExpression)left;
                var fieldTarget = bb.ConvertToVariable(Visit(field.Target));
                return Expression.Field(fieldTarget, field.FieldInfo);

            case BExpressionType.ArrayIndex:
                var arrayIndex = (BArrayIndexExpression)left;
                var arrayTarget = bb.ConvertToVariable(Visit(arrayIndex.Target));
                var arrayIdx = bb.ConvertToVariable(Visit(arrayIndex.Index));
                return Expression.ArrayIndex(arrayTarget, arrayIdx);
        }

        return Visit(left);
    }

    private Exp BreakAssign(BAssignExpression assign)
    {
        var bb = new BBlockBuilder();
        var right = assign.Right;

        if (right.NodeType == BExpressionType.Assign && right.HasYield())
        {
            right = BreakAssign(right as BAssignExpression);
            right = bb.ConvertToVariable(Visit(right));

            bb.AddExpression(Expression.Assign(Visit(assign.Left), right));
            return bb.Build();
        }

        right = bb.ConvertToVariable(Visit(right));
        bb.AddExpression(Expression.Assign(Visit(assign.Left), right));
        return bb.Build();
    }

    protected override Expression VisitNew(BNewExpression node)
    {
        if (!node.HasYield())
            return base.VisitNew(node);

        var bb = new BBlockBuilder();
        var args = new Sequence<Expression>(node.args.Count);
        var ae = node.args.GetFastEnumerator();

        while (ae.MoveNext(out var item))
        {
            var a = Visit(item);
            args.Add(bb.ConvertToVariable(a));
        }

        bb.AddExpression(Expression.New(node.constructor, args));
        return bb.Build();
    }

    protected override Exp VisitMemberInit(BMemberInitExpression node)
    {
        if (!node.HasYield())
            return base.VisitMemberInit(node);

        var bb = new BBlockBuilder();
        var newExpression = node.Target;
        if (newExpression.HasYield())
            newExpression = newExpression.Update(newExpression.constructor, bb.ConvertToVariables(newExpression.args, this));

        var args = new Sequence<BBinding>(node.Bindings.Count);
        var en = node.Bindings.GetFastEnumerator();

        while (en.MoveNext(out var member))
        {
            switch (member.BindingType)
            {
                case BindingType.ElementInit:
                    var ei = member as BElementInit;
                    ei = new BElementInit(ei.AddMethod, bb.ConvertToVariables(ei.Arguments, this));
                    args.Add(ei);
                    break;

                case BindingType.MemberAssignment:
                    var ma = member as BMemberAssignment;
                    ma = new BMemberAssignment(ma.Member, bb.ConvertToVariable(Visit(ma.Value)));
                    args.Add(ma);
                    break;

                case BindingType.MemberListInit:
                    var ml = member as BMemberElementInit;
                    var el = new List<BElementInit>();
                    foreach (var item in ml.Elements)
                        el.Add(new BElementInit(item.AddMethod, bb.ConvertToVariables(item.Arguments, this)));

                    ml = new BMemberElementInit(ml.Member, el.ToArray());
                    args.Add(ml);
                    break;
            }
        }

        bb.AddExpression(new BMemberInitExpression(newExpression, args));
        return bb.Build();
    }

    protected override Exp VisitListInit(BListInitExpression node)
    {
        if (!node.HasYield())
            return node;

        var bb = new BBlockBuilder();
        var newExpression = node.NewExpression;
        if (newExpression.HasYield())
            newExpression = newExpression.Update(newExpression.constructor, bb.ConvertToVariables(newExpression.args, this));

        // scope of improvement

        var args = new Sequence<BElementInit>(node.Members.Count);
        var en = node.Members.GetFastEnumerator();
        while (en.MoveNext(out var member))
            args.Add(new BElementInit(member.AddMethod, bb.ConvertToVariables(member.Arguments, this)));

        bb.AddExpression(new BListInitExpression(newExpression, args));
        return bb.Build();
    }

    protected override Exp VisitUnary(BUnaryExpression yUnaryExpression)
    {
        var target = yUnaryExpression.Target;

        if (!target.HasYield())
            return base.VisitUnary(yUnaryExpression);

        // break...
        var bb = new BBlockBuilder();
        target = bb.ConvertToVariable(Visit(target));

        bb.AddExpression(new BUnaryExpression(target, yUnaryExpression.Operator));
        return bb.Build();
    }

    protected override Exp VisitConditional(BConditionalExpression yConditionalExpression)
    {
        var test = yConditionalExpression.test;
        if (!test.HasYield())
            return base.VisitConditional(yConditionalExpression);

        var bb = new BBlockBuilder();
        test = bb.ConvertToVariable(Visit(test));

        bb.AddExpression(Expression.Condition(test, Visit(yConditionalExpression.@true), Visit(yConditionalExpression.@false)));
        return bb.Build();
    }

    protected override Exp VisitCoalesceCall(BCoalesceCallExpression node)
    {
        if (!node.HasYield())
            return base.VisitCoalesceCall(node);

        var bb = new BBlockBuilder();
        var target = bb.ConvertToVariable(Visit(node.Target));
        var testArgs = bb.ConvertToVariables(node.TestArguments, this);
        var trueArgs = bb.ConvertToVariables(node.TrueArguments, this);
        var falseArgs = bb.ConvertToVariables(node.FalseArguments, this);

        bb.AddExpression(Expression.CoalesceCall(target, node.Test, testArgs, node.True, trueArgs, node.False, falseArgs));
        return bb.Build();
    }

    protected override Exp VisitField(BFieldExpression yFieldExpression)
    {
        if (yFieldExpression.Target == null)
            return yFieldExpression;

        var target = Visit(yFieldExpression.Target);
        if (!target.HasYield())
            return yFieldExpression;

        var bb = new BBlockBuilder();
        target = bb.ConvertToVariable(target);

        bb.AddExpression(Expression.Field(target, yFieldExpression.FieldInfo));
        return bb.Build();
    }

    protected override Exp VisitProperty(BPropertyExpression yPropertyExpression)
    {
        var target = yPropertyExpression.Target;
        if (target == null)
            return yPropertyExpression;

        if (!target.HasYield())
            return yPropertyExpression;

        var bb = new BBlockBuilder();
        target = bb.ConvertToVariable(Visit(target));

        bb.AddExpression(Expression.Property(target, yPropertyExpression.PropertyInfo));
        return bb.Build();
    }

    protected override Exp VisitIndex(BIndexExpression yIndexExpression)
    {
        var hasYield = yIndexExpression.HasYield();
        if (!hasYield)
            return yIndexExpression;

        var bb = new BBlockBuilder();
        var target = Visit(yIndexExpression.Target);
        if (target.HasYield())
            target = bb.ConvertToVariable(target);

        var args = new Sequence<Expression>(yIndexExpression.Arguments.Count);
        var ae = yIndexExpression.Arguments.GetFastEnumerator();
        
        while (ae.MoveNext(out var item))
        {
            var e = Visit(item);
            args.Add(bb.ConvertToVariable(e));
        }

        bb.AddExpression(Expression.Index(target, yIndexExpression.Property, args));
        return bb.Build();
    }

    protected override Exp VisitArrayIndex(BArrayIndexExpression yArrayIndexExpression)
    {
        var target = Visit(yArrayIndexExpression.Target);
        var index = Visit(yArrayIndexExpression.Index);

        var targetHasYield = target.HasYield();
        var indexHasYield = index.HasYield();

        if (!targetHasYield && !indexHasYield)
            return yArrayIndexExpression;

        var bb = new BBlockBuilder();

        if (targetHasYield)
            target = bb.ConvertToVariable(target);

        if (indexHasYield)
            index = bb.ConvertToVariable(index);

        bb.AddExpression(Expression.ArrayIndex(target, index));
        return bb.Build();
    }

    protected override Expression VisitCall(BCallExpression node)
    {
        if (!node.HasYield())
            return node;

        // rewrite...
        var bb = new BBlockBuilder();

        var target = Visit(node.Target);
        if (target?.HasYield() ?? false)
            target = bb.ConvertToVariable(target);

        var args = new Sequence<Expression>(node.Arguments.Count);
        var ae = node.Arguments.GetFastEnumerator();
        while (ae.MoveNext(out var item))
        {
            var a = Visit(item);
            args.Add(bb.ConvertToVariable(a));
        }

        bb.AddExpression(Expression.Call(target, node.Method, args));
        return bb.Build();
    }
}
