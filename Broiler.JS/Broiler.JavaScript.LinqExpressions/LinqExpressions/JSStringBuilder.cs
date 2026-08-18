using System;
using System.Reflection;
using Expression = Broiler.JavaScript.ExpressionCompiler.Expressions.BExpression;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.LinqExpressions.LinqExpressions;


public class JSStringBuilder
{
    private static Type type;
    private static ConstructorInfo _ctor;

    /// <summary>
    /// Initializes the builder with the concrete JSString type.
    /// Called by the BuiltIns assembly via <c>[ModuleInitializer]</c>.
    /// </summary>
    internal static void Initialize(Type stringType)
    {
        type = stringType;
        _ctor = type.GetConstructor([typeof(string)])
            ?? throw new InvalidOperationException($"JSString type {stringType.FullName} does not have a (string) constructor.");

        var text = System.Linq.Expressions.Expression.Parameter(typeof(string), "text");
        Create = System.Linq.Expressions.Expression.Lambda<Func<string, JSValue>>(
            System.Linq.Expressions.Expression.TypeAs(
                System.Linq.Expressions.Expression.New(_ctor, text),
                typeof(JSValue)),
            text).Compile();
    }

    public static Expression New(Expression exp)
    {
        return Expression.TypeAs(Expression.New(_ctor, exp), typeof(JSValue));
    }

    /// <summary>
    /// The value of a StringLiteral, through the same constructor <see cref="New"/> emits a call
    /// to but without generating a method. Null until the BuiltIns assembly has registered its
    /// String type.
    /// </summary>
    public static Func<string, JSValue> Create { get; private set; }
}
