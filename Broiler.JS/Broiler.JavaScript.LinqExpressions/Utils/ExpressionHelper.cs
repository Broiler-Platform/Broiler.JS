#nullable enable
using System.Collections.Generic;
using Expression = Broiler.JavaScript.ExpressionCompiler.Expressions.BExpression;
using ParameterExpression = Broiler.JavaScript.ExpressionCompiler.Expressions.BParameterExpression;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;

namespace Broiler.JavaScript.LinqExpressions.Utils;

public static class ExpressionHelper
{
    public static void AddExpanded(this IList<Expression> list, IList<ParameterExpression> peList, Expression exp)
    {
        if (exp.NodeType == BExpressionType.Block)
        {
            var block = (exp as BBlockExpression)!;

            foreach (var p in block.Variables)
                peList.Add(p);

            foreach (var s in block.Expressions)
                list.Add(s);

            return;
        }

        list.Add(exp);
    }

    public static Expression? ToJSValue(this Expression exp)
    {
        if (exp == null)
            return exp;

        if (typeof(JSVariable) == exp.Type)
            return JSVariable.ValueExpression(exp);

        if (typeof(JSValue) == exp.Type)
            return exp;

        if (!typeof(JSValue).IsAssignableFrom(exp.Type))
            return Expression.Block(exp, JSUndefinedBuilder.Value);

        return Expression.TypeAs(exp, typeof(JSValue));
    }
}
