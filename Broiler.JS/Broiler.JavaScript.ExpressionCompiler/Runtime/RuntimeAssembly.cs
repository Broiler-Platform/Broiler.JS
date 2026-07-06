using System;
using System.IO;
using System.Reflection.Emit;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.ExpressionCompiler.Generator;

namespace Broiler.JavaScript.ExpressionCompiler.Runtime;

public static class RuntimeAssembly
{

    public static object Compile(this BLambdaExpression exp)
    {
        LambdaRewriter.Rewrite(exp);
        exp = exp.WithThis(typeof(Closures));

        var method = new DynamicMethod(exp.Name.FullName, exp.ReturnType, exp.ParameterTypesWithThis, typeof(Closures), true);

        var ilg = method.GetILGenerator();

        ILCodeGenerator icg = new(ilg, null);
        icg.Emit(exp);

        var c = new Closures(null, null, null, null);

        return method.CreateDelegate(exp.Type, c);
    }

    public static T Compile<T>(this BExpression<T> exp)
    {
        LambdaRewriter.Rewrite(exp);
        exp = exp.WithThis<T>(typeof(Closures));

        // var f = new FlattenVisitor();

        var method = new DynamicMethod(exp.Name.FullName, exp.ReturnType, exp.ParameterTypesWithThis, typeof(Closures), true);

        var ilg = method.GetILGenerator();

        var sw = new StringWriter();
        var expWriter = new StringWriter();
        ILCodeGenerator icg = new(ilg, null, sw, expWriter);
        icg.Emit(exp);

        string il = sw.ToString();

        var c = new Closures(null, null, il, expWriter.ToString());
        return (T)(object)method.CreateDelegate(typeof(T), c);
    }


    internal static (DynamicMethod, string il, string exp) CompileToBoundDynamicMethod(
        this BLambdaExpression exp,
        Type boundType = null,
        IMethodBuilder methodBuilder = null,
        bool captureDiagnostics = false)
    {
        // create closure...

        boundType = boundType ?? typeof(Closures);

        // dynamic method expects this as first parameter !!


        var method = new DynamicMethod(exp.Name.FullName, exp.ReturnType, exp.ParameterTypesWithThis, boundType, true);

        var ilg = method.GetILGenerator();
        StringWriter sw = captureDiagnostics ? new StringWriter() : null;
        StringWriter expWriter = captureDiagnostics ? new StringWriter() : null;
        // ILCodeGenerator.GenerateLogs = true;
        ILCodeGenerator icg = new(ilg, methodBuilder, sw, expWriter, captureDiagnostics);
        icg.Emit(exp);

        string il = sw?.ToString() ?? string.Empty;

        return (method, il, expWriter?.ToString() ?? string.Empty);

    }

    public static T CompileWithNestedLambdas<T>(this BExpression<T> expression)
        => expression.CompileWithNestedLambdas(ExpressionCompilationOptions.Default).Value;

    public static ExpressionCompilationResult<T> CompileWithNestedLambdas<T>(
        this BExpression<T> expression,
        ExpressionCompilationOptions options)
    {
        options ??= ExpressionCompilationOptions.Default;
        return ExpressionCompilationBackends.Get(options.Backend).Compile(expression, options);
    }

    public static T CompileWithNestedLambdas<T>(
        this BExpression<T> expression,
        ExpressionCompilationBackend backend)
        => expression.CompileWithNestedLambdas(new ExpressionCompilationOptions { Backend = backend }).Value;

}
