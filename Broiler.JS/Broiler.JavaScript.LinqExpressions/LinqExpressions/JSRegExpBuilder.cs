using System;
using System.Reflection;
using Expression = Broiler.JavaScript.ExpressionCompiler.Expressions.BExpression;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.LinqExpressions.LinqExpressions;

public class JSRegExpBuilder
{
    private static Type type;
    private static ConstructorInfo _ctor;

    /// <summary>
    /// Initializes the builder with the concrete JSRegExp type.
    /// Called by the BuiltIns assembly via <c>[ModuleInitializer]</c>.
    /// </summary>
    internal static void Initialize(Type regExpType)
    {
        type = regExpType;
        _ctor = type.GetConstructor([typeof(string), typeof(string)])
            ?? throw new InvalidOperationException($"JSRegExp type {regExpType.FullName} does not have a (string, string) constructor.");

        var pattern = System.Linq.Expressions.Expression.Parameter(typeof(string), "pattern");
        var flags = System.Linq.Expressions.Expression.Parameter(typeof(string), "flags");
        Create = System.Linq.Expressions.Expression.Lambda<Func<string, string, JSValue>>(
            System.Linq.Expressions.Expression.TypeAs(
                System.Linq.Expressions.Expression.New(_ctor, pattern, flags),
                typeof(JSValue)),
            pattern,
            flags).Compile();
    }

    public static Expression New(Expression exp, Expression exp2) =>
        Expression.TypeAs(Expression.New(_ctor, exp, exp2), typeof(JSValue));

    /// <summary>
    /// The value of a RegularExpressionLiteral, produced through the same constructor
    /// <see cref="New"/> emits a call to, but without generating a method. Lets a caller that
    /// already knows the pattern and flags — the literal-program fast path in
    /// <c>DirectEvalSupport</c> — skip compilation entirely. Null until the BuiltIns assembly
    /// has registered its RegExp type.
    /// </summary>
    public static Func<string, string, JSValue> Create { get; private set; }
}
