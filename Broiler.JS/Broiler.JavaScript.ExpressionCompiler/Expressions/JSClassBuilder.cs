using Expression = Broiler.JavaScript.ExpressionCompiler.Expressions.YExpression;
using System;
using System.Reflection;

namespace Broiler.JavaScript.ExpressionCompiler.Expressions;

public static class JSClassBuilder
{
    private static Type _type;
    private static MethodInfo _addConstructor;
    private static ConstructorInfo _ctor;
    private static MethodInfo _resolveSuperclassPrototype;

    /// <summary>
    /// The concrete JSClass type, set during assembly initialization.
    /// Used by the Compiler to allocate temp variables without a direct type reference.
    /// </summary>
    public static Type Type => _type;

    /// <summary>
    /// Initializes the builder with the concrete JSClass type.
    /// Called by the BuiltIns assembly via <c>[ModuleInitializer]</c>.
    /// </summary>
    internal static void Initialize(Type classType, Type functionType, Type delegateType)
    {
        var jsValueType = Type.GetType("Broiler.JavaScript.Runtime.JSValue, Broiler.JavaScript.Runtime")
            ?? throw new InvalidOperationException("JSValue type not found in BuiltIns assembly");
        _type = classType;
        _addConstructor = classType.GetMethod(nameof(AddConstructorName), [functionType])
            ?? classType.GetMethod(AddConstructorName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        _ctor = classType.GetConstructor([delegateType, jsValueType, typeof(string), typeof(string)]);
        _resolveSuperclassPrototype = classType.GetMethod(
            ResolveSuperclassPrototypeName,
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public,
            binder: null,
            types: [jsValueType],
            modifiers: null);
    }

    private const string AddConstructorName = "AddConstructor";
    private const string ResolveSuperclassPrototypeName = "ResolveSuperclassPrototype";

    public static YElementInit AddConstructor(Expression exp) => Expression.ElementInit(_addConstructor, exp);

    public static Expression ResolveSuperclassPrototype(Expression exp) => Expression.Call(null, _resolveSuperclassPrototype, exp);

    public static YNewExpression New(Expression constructor, Expression super, string name, string code = "") =>
        Expression.New(_ctor,
            constructor ?? Expression.Null, super ?? Expression.Null, Expression.Constant(name), Expression.Constant(code));
}
