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
    private static MethodInfo _createConversion;
    private static MethodInfo _createConversionAtSite;
    private static MethodInfo _createConversionIntoRefusedLocal;
    private static MethodInfo _createSpeculativeRead;

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
        _createConversion = type.GetMethod("CreateConversion", [typeof(double)])
            ?? throw new NotSupportedException();
        _createConversionAtSite = type.GetMethod("CreateConversion", [typeof(double), typeof(int)])
            ?? throw new InvalidOperationException("JSNumber.CreateConversion(double, int) not found");
        _createConversionIntoRefusedLocal = type.GetMethod("CreateConversionIntoRefusedLocal", [typeof(double), typeof(int)])
            ?? throw new InvalidOperationException("JSNumber.CreateConversionIntoRefusedLocal(double, int) not found");
        _createLiteral = type.GetMethod("CreateLiteral", [typeof(double)])
            ?? throw new InvalidOperationException("JSNumber.CreateLiteral(double) not found");
        _createSpeculativeRead = type.GetMethod("CreateSpeculativeRead", [typeof(double)])
            ?? throw new InvalidOperationException("JSNumber.CreateSpeculativeRead(double) not found");

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

        return Expression.Call(null, _createConversion, exp);
    }

    /// <summary>
    /// Emits the creation of a number for a boxing conversion, naming the emission site so the
    /// census can attribute it (docs/performance-roadmap.md item 3-1, §4.2a).
    /// </summary>
    /// <remarks>
    /// The site is an ordinary constant argument on the one code path the engine ships, rather
    /// than a factory entry per site or an argument gated behind the counter flag. Nine entries
    /// would be nine near-identical methods, and a gated argument would leave the arm that is
    /// measured and the arm that ships running different code — which is the defect §3.5 keeps
    /// finding in this campaign's own instruments.
    /// </remarks>
    public static Expression New(Expression exp, NumberBoxingConversionSite site)
    {
        if (exp.Type != typeof(double))
            exp = Expression.Convert(exp, typeof(double));

        return Expression.Call(null, _createConversionAtSite, exp, Expression.Constant((int)site));
    }

    /// <summary>
    /// Emits the creation of a number for a compile-time LITERAL. Same factory, separate entry,
    /// so a run can be asked how much of its boxing is literals (docs/performance-roadmap.md
    /// item 3-1).
    /// </summary>
    public static Expression NewLiteral(double value)
        => Expression.Call(null, _createLiteral, Expression.Constant(value));

    /// <summary>
    /// Emits the creation of a number for a READ of item 3-8a's speculative local. Same factory,
    /// separate entry, so a run can be asked how much of its boxing the dual representation is
    /// still paying (docs/performance-roadmap.md item 3-8a).
    /// </summary>
    public static Expression NewSpeculativeRead(Expression exp)
        => Expression.Call(null, _createSpeculativeRead, exp);

    /// <summary>
    /// Emits a guarded tree root's box whose consuming LOCAL the numeric tier refused, naming the
    /// refusal (docs/performance-roadmap.md item 3-1, `0106`).
    /// </summary>
    public static Expression NewIntoRefusedLocal(Expression exp, NumericLocalMiss miss)
    {
        if (exp.Type != typeof(double))
            exp = Expression.Convert(exp, typeof(double));

        return Expression.Call(null, _createConversionIntoRefusedLocal, exp, Expression.Constant((int)miss));
    }
}
