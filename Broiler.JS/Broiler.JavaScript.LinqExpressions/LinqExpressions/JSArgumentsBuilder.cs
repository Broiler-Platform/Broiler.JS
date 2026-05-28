using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Broiler.JavaScript.Runtime;
using Expression = Broiler.JavaScript.ExpressionCompiler.Expressions.YExpression;

namespace Broiler.JavaScript.LinqExpressions.LinqExpressions;

public static class JSArgumentsBuilder
{
    private static Type _type;
    private static ConstructorInfo _New;
    private static ConstructorInfo _NewMapped;

    /// <summary>
    /// Initializes the builder with the concrete JSArguments type.
    /// Called by the Modules assembly via <c>[ModuleInitializer]</c>.
    /// </summary>
    internal static void Initialize(Type argumentsType)
    {
        _type = argumentsType;
        _New = argumentsType.GetConstructor([typeof(Arguments).MakeByRefType()]);
        _NewMapped = argumentsType.GetConstructor([typeof(Arguments).MakeByRefType(), typeof(JSVariable[])]);
    }

    private static void EnsureInitialized()
    {
        if (_New != null && _NewMapped != null)
            return;

        try
        {
            var assembly = Assembly.Load("Broiler.JavaScript.Modules");
            RuntimeHelpers.RunModuleConstructor(assembly.ManifestModule.ModuleHandle);
        }
        catch (Exception ex) when (
            ex is System.IO.FileNotFoundException
            or System.IO.FileLoadException
            or BadImageFormatException)
        {
        }

        if (_New == null || _NewMapped == null)
            throw new InvalidOperationException("JSArgumentsBuilder is not initialized. Ensure the Broiler.JavaScript.Modules assembly is available.");
    }

    public static Expression New(Expression args)
    {
        EnsureInitialized();
        return Expression.New(_New, args);
    }

    public static Expression NewMapped(Expression args, Expression mappedParameters)
    {
        EnsureInitialized();
        return Expression.New(_NewMapped, args, mappedParameters);
    }
}
