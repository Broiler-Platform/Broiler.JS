using System;
using System.Reflection;
using Expression = Broiler.JavaScript.ExpressionCompiler.Expressions.BExpression;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.LinqExpressions.LinqExpressions;

public class JSNumberBuilder
{
    private static Type type;
    private static MethodInfo _create;
    private static MethodInfo _createLiteral;

    public static Expression NaN;
    public static Expression Zero;
    public static Expression One;
    public static Expression MinusOne;
    public static Expression Two;

    /// <summary>
    /// Initializes the builder with the concrete JSNumber type.
    /// Called by the BuiltIns assembly via <c>[ModuleInitializer]</c>.
    /// </summary>
    internal static void Initialize(Type numberType)
    {
        type = numberType;
        _create = type.GetMethod("Create", [typeof(double)])
            ?? throw new InvalidOperationException("JSNumber.Create(double) not found");
        _createLiteral = type.GetMethod("CreateLiteral", [typeof(double)])
            ?? throw new InvalidOperationException("JSNumber.CreateLiteral(double) not found");

        NaN = Expression.Field(null, type.GetField("NaN"));
        Zero = Expression.Field(null, type.GetField("Zero"));
        One = Expression.Field(null, type.GetField("One"));
        MinusOne = Expression.Field(null, type.GetField("MinusOne"));
        Two = Expression.Field(null, type.GetField("Two"));
    }

    /// <summary>
    /// Emits the creation of a number. Routed through the factory rather than the
    /// constructor so a small integer reuses a cached instance
    /// (docs/performance-roadmap.md P2-2).
    /// </summary>
    public static Expression New(Expression exp)
    {
        if (exp.Type != typeof(double))
            exp = Expression.Convert(exp, typeof(double));

        return Expression.Call(null, _create, exp);
    }

    /// <summary>
    /// Emits the creation of a number for a compile-time LITERAL. Same factory, separate entry,
    /// so a run can be asked how much of its boxing is literals (docs/performance-roadmap.md
    /// item 3-1).
    /// </summary>
    public static Expression NewLiteral(double value)
        => Expression.Call(null, _createLiteral, Expression.Constant(value));
}
