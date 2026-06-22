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
        this BLambdaExpression exp, Type boundType = null, IMethodBuilder methodBuilder = null)
    {
        // create closure...

        boundType = boundType ?? typeof(Closures);

        // dynamic method expects this as first parameter !!


        var method = new DynamicMethod(exp.Name.FullName, exp.ReturnType, exp.ParameterTypesWithThis, boundType, true);

        var ilg = method.GetILGenerator();
        StringWriter sw = new();
        var expWriter = new StringWriter();
        // ILCodeGenerator.GenerateLogs = true;
        ILCodeGenerator icg = new(ilg, methodBuilder, sw, expWriter);
        icg.Emit(exp);

        string il = sw.ToString();

        return (method, il, expWriter.ToString());

    }

    public static T CompileWithNestedLambdas<T>(this BExpression<T> expression)
    {
        var repository = new MethodRepository();
        var outerLambda = BExpression.InstanceLambda<Func<T>>(expression.Name + "_outer", expression, BExpression.Parameter(typeof(Closures)), []) as BLambdaExpression;
        LambdaRewriter.Rewrite(outerLambda);
        var runtimeMethodBuilder = new RuntimeMethodBuilder(repository);

        var (outer, il, exp) = outerLambda.CompileToBoundDynamicMethod(typeof(Closures), runtimeMethodBuilder);

        repository.IL = il;
        repository.Exp = exp;
        var root = new Closures(repository, null, il, exp);

        var func = outer.CreateDelegate(typeof(Func<T>), root) as Func<T>;

        return func();
    }

}
