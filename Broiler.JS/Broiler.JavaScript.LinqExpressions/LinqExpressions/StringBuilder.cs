using Expression = Broiler.JavaScript.ExpressionCompiler.Expressions.BExpression;
using Broiler.JavaScript.LinqExpressions.LambdaGen;
using System.Reflection;

namespace Broiler.JavaScript.LinqExpressions.LinqExpressions;

public class ClrStringBuilder
{
    // Two CLR strings compare ordinally, by string's equality operator (milestone SJL-9's clause 8, on the owner's ruling
    // of 2026-11-11, "Fix all + enforce", which names the comparison); until then the call was `x.Equals("")`, the same
    // ordinal comparison through an overload that names none.
    private static readonly MethodInfo OrdinalEquality = typeof(string).GetMethod("op_Equality", [typeof(string), typeof(string)]);

    public static Expression Equal(Expression left, Expression right) => Expression.Call(null, OrdinalEquality, left, right);
    public static Expression NotEqual(Expression left, Expression right) => Expression.Not(Equal(left, right));
    public static Expression Concat(Expression left, Expression right) => NewLambdaExpression.StaticCallExpression(() => () => string.Concat("", ""), left, right);
}
