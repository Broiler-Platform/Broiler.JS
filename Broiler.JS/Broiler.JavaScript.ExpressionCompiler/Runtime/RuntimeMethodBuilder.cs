using System;
using System.Reflection;
using Broiler.JavaScript.ExpressionCompiler.ClosureSeparator;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.ExpressionCompiler.Expressions;

namespace Broiler.JavaScript.ExpressionCompiler.Runtime;


public class RuntimeMethodBuilder(
    IMethodRepository methods,
    bool captureDiagnostics = false,
    bool enableJavaScriptTailCalls = false) : IMethodBuilder
{
    private static Type type = typeof(IMethodRepository);

    private static MethodInfo create = type.GetMethod(nameof(IMethodRepository.Create));


    public BExpression Relay(BExpression @this, IFastEnumerable<BExpression> closures, BLambdaExpression innerLambda)
    {
        LambdaRewriter.Rewrite(innerLambda);
        var (method, il, exp) = innerLambda.CompileToBoundDynamicMethod(
            methodBuilder: this,
            captureDiagnostics: captureDiagnostics,
            enableJavaScriptTailCalls: enableJavaScriptTailCalls);
        var repository = BExpression.Field(@this, Closures.repositoryField);
        var id = methods.RegisterNew(method, il, exp, innerLambda.Type);
        return BExpression.Call(repository, create, closures == null ? BExpression.Null : BExpression.NewArray(typeof(Box), closures), BExpression.Constant(id));
    }
}
