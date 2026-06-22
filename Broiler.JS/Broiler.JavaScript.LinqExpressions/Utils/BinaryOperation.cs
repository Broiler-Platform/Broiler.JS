using System;
using System.Linq;
using Expression = Broiler.JavaScript.ExpressionCompiler.Expressions.BExpression;
using ParameterExpression = Broiler.JavaScript.ExpressionCompiler.Expressions.BParameterExpression;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.LinqExpressions.LambdaGen;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;


namespace Broiler.JavaScript.LinqExpressions.Utils;

public delegate Expression CaseExpression(ParameterExpression pe);

public class SwitchExpression
{
    protected static TypeCheckCase Case<T>(in CaseExpression e) => new()
    {
        Type = typeof(T),
        TrueCase = e
    };

    protected static TypeCheckCase Case(Type type, CaseExpression e) => new()
    {
        Type = type,
        TrueCase = e
    };

    protected static TypeCheckCase Default(Expression e) => new()
    {
        Type = null,
        TrueCase = (a) => e
    };

    public class TypeCheckCase
    {
        public Type Type { get; set; }

        public CaseExpression TrueCase { get; set; }

    }

    protected static Expression Switch(Expression right, params TypeCheckCase[] cases)
    {
        var defaultCase = cases.First(x => x.Type == null);
        var allCases = cases.Where(x => x.Type != null);

        Expression condition = defaultCase.TrueCase(null);
        foreach (var @case in allCases)
        {
            var bp = Expression.Parameter(@case.Type);

            if (@case.Type.IsValueType)
            {
                condition = Expression.Condition(Expression.TypeIs(right, @case.Type), Expression.Block(bp.AsSequence(), @case.TrueCase(bp)), condition, typeof(JSValue));
                continue;
            }

            var nbt = Expression.Constant(null, @case.Type);
            condition = Expression.Block(bp.AsSequence(), Expression.Assign(bp, Expression.TypeAs(right, @case.Type)), Expression.Condition(Expression.NotEqual(nbt, bp),
                    @case.TrueCase(bp), condition, typeof(JSValue)));
        }

        return condition;
    }
}

public class BinaryOperation : SwitchExpression
{
    public static Expression Assign(Expression left, Expression right, TokenTypes assignmentOperator)
    {
        var oneF = Expression.Constant(0x1F);

        switch (assignmentOperator)
        {
            case TokenTypes.Assign:
                return Assign(left, right);

            case TokenTypes.AssignAdd:
                return Assign(left, JSValueBuilder.Add(left, right));
        }

        var leftDouble = JSValueBuilder.DoubleValue(left);
        var rightDouble = JSValueBuilder.DoubleValue(right);

        var leftInt = JSValueBuilder.IntValue(left);
        var rightInt = JSValueBuilder.IntValue(right);

        var leftUInt = Expression.Convert(leftDouble, typeof(uint));
        var rightUInt = Expression.Convert(rightDouble, typeof(uint));

        // convert to double...
        return assignmentOperator switch
        {
            TokenTypes.AssignSubtract => Assign(left, JSValueBuilder.Subtract(left, right)),
            TokenTypes.AssignMultiply => Assign(left, JSValueBuilder.Multiply(left, right)),
            TokenTypes.AssignDivide => Assign(left, JSValueBuilder.Divide(left, right)),
            TokenTypes.AssignMod => Assign(left, JSValueBuilder.Modulo(left, right)),
            TokenTypes.AssignBitwideAnd => Assign(left, JSValueBuilder.BitwiseAnd(left, right)),
            TokenTypes.AssignBitwideOr => Assign(left, JSValueBuilder.BitwiseOr(left, right)),
            TokenTypes.AssignXor => Assign(left, JSValueBuilder.BitwiseXor(left, right)),
            TokenTypes.AssignLeftShift => Assign(left, JSValueBuilder.LeftShift(left, right)),
            TokenTypes.AssignRightShift => Assign(left, JSValueBuilder.RightShift(left, right)),
            TokenTypes.AssignUnsignedRightShift => Assign(left, JSValueBuilder.UnsignedRightShift(left, right)),
            TokenTypes.AssignPower => Assign(left, JSValueBuilder.Power(left, right)),
            TokenTypes.AssignCoalesce => AssignCoalesce(left, right),
            TokenTypes.AssignBooleanAnd => AssignBooleanAnd(left, right),
            TokenTypes.AssignBooleanOr => AssignBooleanOr(left, right),
            _ => throw new NotSupportedException(),
        };
    }

    private static Expression Assign(Expression left, Expression right) => JSValueExtensionsBuilder.Assign(left, right);

    private static Expression AssignCoalesce(Expression left, Expression right)
    {
        var current = Expression.Parameter(typeof(JSValue));
        return Expression.Block(
            current.AsSequence(),
            Expression.Assign(current, left),
            Expression.Condition(JSValueBuilder.IsNullOrUndefined(current), Assign(left, right), current, typeof(JSValue)));
    }

    private static Expression AssignBooleanAnd(Expression left, Expression right)
    {
        var current = Expression.Parameter(typeof(JSValue));
        return Expression.Block(
            current.AsSequence(),
            Expression.Assign(current, left),
            Expression.Condition(JSValueBuilder.BooleanValue(current), Assign(left, right), current, typeof(JSValue)));
    }

    private static Expression AssignBooleanOr(Expression left, Expression right)
    {
        var current = Expression.Parameter(typeof(JSValue));
        return Expression.Block(
            current.AsSequence(),
            Expression.Assign(current, left),
            Expression.Condition(JSValueBuilder.BooleanValue(current), current, Assign(left, right), typeof(JSValue)));
    }

    public static Expression Operation(Expression left, Expression right, TokenTypes op)
    {
        return op switch
        {
            TokenTypes.Equal => JSValueBuilder.Equals(left, right),
            TokenTypes.NotEqual => JSValueBuilder.NotEquals(left, right),
            TokenTypes.StrictlyEqual => JSValueBuilder.StrictEquals(left, right),
            TokenTypes.StrictlyNotEqual => JSValueBuilder.NotStrictEquals(left, right),
            TokenTypes.InstanceOf => JSValueExtensionsBuilder.InstanceOf(left, right),
            TokenTypes.In => JSValueExtensionsBuilder.IsIn(left, right),
            TokenTypes.Plus => JSValueBuilder.Add(left, right),
            TokenTypes.Minus => left.CallExpression<JSValue, JSValue, JSValue>(() => (a, b) => a.Subtract(b), right),
            TokenTypes.Multiply => left.CallExpression<JSValue, JSValue, JSValue>(() => (a, b) => a.Multiply(b), right),
            TokenTypes.Divide => left.CallExpression<JSValue, JSValue, JSValue>(() => (a, b) => a.Divide(b), right),
            TokenTypes.Mod => left.CallExpression<JSValue, JSValue, JSValue>(() => (a, b) => a.Modulo(b), right),
            TokenTypes.Greater => JSValueBuilder.Greater(left, right),
            TokenTypes.GreaterOrEqual => JSValueBuilder.GreaterOrEqual(left, right),
            TokenTypes.Less => JSValueBuilder.Less(left, right),
            TokenTypes.LessOrEqual => JSValueBuilder.LessOrEqual(left, right),
            TokenTypes.BitwiseAnd => left.CallExpression<JSValue, JSValue, JSValue>(() => (a, b) => a.BitwiseAnd(b), right),
            TokenTypes.BitwiseOr => left.CallExpression<JSValue, JSValue, JSValue>(() => (a, b) => a.BitwiseOr(b), right),
            TokenTypes.Xor => left.CallExpression<JSValue, JSValue, JSValue>(() => (a, b) => a.BitwiseXor(b), right),
            TokenTypes.LeftShift => left.CallExpression<JSValue, JSValue, JSValue>(() => (a, b) => a.LeftShift(b), right),
            TokenTypes.RightShift => left.CallExpression<JSValue, JSValue, JSValue>(() => (a, b) => a.RightShift(b), right),
            TokenTypes.UnsignedRightShift => left.CallExpression<JSValue, JSValue, JSValue>(() => (a, b) => a.UnsignedRightShift(b), right),
            TokenTypes.BooleanAnd => JSValueBuilder.LogicalAnd(left, right),
            TokenTypes.BooleanOr => JSValueBuilder.LogicalOr(left, right),
            TokenTypes.Power => JSValueBuilder.Power(left, right),
            TokenTypes.Coalesce => JSValueExtensionsBuilder.Coalesce(left, right),
            _ => null,
        };
    }
}
