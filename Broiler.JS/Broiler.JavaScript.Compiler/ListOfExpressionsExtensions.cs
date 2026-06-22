using System;
using System.Collections.Generic;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;
using Broiler.JavaScript.Runtime;
using Expression = Broiler.JavaScript.ExpressionCompiler.Expressions.BExpression;

namespace Broiler.JavaScript.Compiler;

public static class ListOfExpressionsExtensions
{
    public static Sequence<Expression> ConvertToInteger(this IFastEnumerable<Expression> source, FastPool.Scope scope)
    {
        var result = new Sequence<Expression>(source.Count);
        var se = source.GetFastEnumerator();

        while (se.MoveNext(out var exp))
        {
            switch (exp.NodeType)
            {
                case BExpressionType.Int32Constant:
                case BExpressionType.UInt32Constant:
                case BExpressionType.Int64Constant:
                case BExpressionType.UInt64Constant:
                    result.Add(exp);
                    continue;

                case BExpressionType.DoubleConstant:
                    result.Add(Expression.Constant((int)(exp as BDoubleConstantExpression).Value));
                    continue;
            }
            if (!exp.IsConstant(out var ce))
                throw new NotSupportedException();

            if (ce.Type == typeof(int))
            {
                result.Add(exp);
                continue;
            }

            result.Add(Expression.Constant(Convert.ToInt32(ce.Value)));
        }

        return result;
    }

    public static Sequence<Expression> ConvertToInteger(this IFastEnumerable<Expression> source)
    {
        var result = new Sequence<Expression>(source.Count);
        var se = source.GetFastEnumerator();

        while (se.MoveNext(out var exp))
        {
            switch (exp.NodeType)
            {
                case BExpressionType.Int32Constant:
                case BExpressionType.UInt32Constant:
                case BExpressionType.Int64Constant:
                case BExpressionType.UInt64Constant:
                    result.Add(exp);
                    continue;
            }

            if (!exp.IsConstant(out var ce))
                throw new NotSupportedException();

            if (ce.Type == typeof(int))
            {
                result.Add(exp);
                continue;
            }

            result.Add(Expression.Constant(Convert.ToInt32(ce.Value)));
        }

        return result;
    }

    public static Sequence<Expression> ConvertToNumber(this IFastEnumerable<Expression> source, FastPool.Scope scope)
    {
        var result = new Sequence<Expression>(source.Count);
        var se = source.GetFastEnumerator();

        while (se.MoveNext(out var exp))
        {
            // Numeric switch-case tests are emitted as DoubleConstant (the literal
            // path always uses BExpression.Constant(double)); the typed constant
            // nodes are not generic Constant nodes, so IsConstant() does not match
            // them. Mirror ConvertToInteger and pass/convert them by NodeType.
            switch (exp.NodeType)
            {
                case BExpressionType.DoubleConstant:
                    result.Add(exp);
                    continue;

                case BExpressionType.Int32Constant:
                    result.Add(Expression.Constant((double)(exp as BInt32ConstantExpression).Value));
                    continue;

                case BExpressionType.UInt32Constant:
                    result.Add(Expression.Constant((double)(exp as BUInt32ConstantExpression).Value));
                    continue;

                case BExpressionType.Int64Constant:
                    result.Add(Expression.Constant((double)(exp as BInt64ConstantExpression).Value));
                    continue;

                case BExpressionType.UInt64Constant:
                    result.Add(Expression.Constant((double)(exp as BUInt64ConstantExpression).Value));
                    continue;
            }

            if (!exp.IsConstant(out var ce))
                throw new NotSupportedException();

            if (ce.Type == typeof(double))
            {
                result.Add(exp);
                continue;
            }

            result.Add(Expression.Constant(Convert.ToDouble(ce.Value)));
        }

        return result;
    }

    public static SparseList<Expression> ConvertToNumber(this IList<Expression> source)
    {
        var result = new SparseList<Expression>(source.Count);

        foreach (var exp in source)
        {
            if (!exp.IsConstant(out var ce))
                throw new NotSupportedException();

            if (ce.Type == typeof(double))
            {
                result.Add(exp);
                continue;
            }

            result.Add(Expression.Constant(Convert.ToDouble(ce.Value)));
        }

        return result;
    }

    public static Sequence<Expression> ConvertToString(this IFastEnumerable<Expression> source, FastPool.Scope scope)
    {
        var result = new Sequence<Expression>(source.Count);
        var se = source.GetFastEnumerator();

        while (se.MoveNext(out var exp))
        {
            if (exp.NodeType == BExpressionType.StringConstant)
            {
                result.Add(exp);
                continue;
            }

            if (!exp.IsConstant(out var ce))
                throw new NotSupportedException();

            if (ce.Type == typeof(string))
            {
                result.Add(exp);
                continue;
            }

            result.Add(Expression.Constant(ce.Value.ToString()));
        }

        return result;
    }

    public static Sequence<Expression> ConvertToJSValue(this IFastEnumerable<Expression> source, FastPool.Scope scope)
    {
        var result = new Sequence<Expression>(source.Count);
        var se = source.GetFastEnumerator();

        while (se.MoveNext(out var exp))
        {
            switch (exp.NodeType)
            {
                case BExpressionType.StringConstant:
                    result.Add(JSStringBuilder.New(exp as BStringConstantExpression));
                    continue;

                case BExpressionType.DoubleConstant:
                    result.Add(JSNumberBuilder.New(exp as BDoubleConstantExpression));
                    continue;
            }

            if (!exp.IsConstant(out var ce))
            {
                result.Add(exp);
                continue;
            }

            Expression item = ce.Value switch
            {
                string @string => JSStringBuilder.New(ce),
                double @double => JSNumberBuilder.New(ce),
                _ => throw new NotImplementedException(),
            };

            result.Add(item);
        }

        return result;
    }

    public static SparseList<Expression> ConvertToString(this IList<Expression> source)
    {
        var result = new SparseList<Expression>(source.Count);

        foreach (var exp in source)
        {
            if (!exp.IsConstant(out var ce))
                throw new NotSupportedException();

            if (ce.Type == typeof(string))
            {
                result.Add(exp);
                continue;
            }

            result.Add(Expression.Constant(ce.Value.ToString()));
        }

        return result;
    }

    public static SparseList<Expression> ConvertToJSValue(this IList<Expression> source)
    {
        SparseList<Expression> result = new(source.Count);

        foreach (var exp in source)
        {
            if (!exp.IsConstant(out var ce))
            {
                result.Add(exp);
                continue;
            }

            Expression item;

            switch (ce.Value)
            {
                case string @string:
                    item = JSStringBuilder.New(ce);
                    break;

                case double @double:
                    item = JSNumberBuilder.New(ce);
                    break;

                default:
                    throw new NotImplementedException();
            }

            result.Add(item);
        }

        return result;
    }
}
