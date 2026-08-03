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
        // Stays eager, and must: it decides which variables this lambda captures and boxes
        // them, and the boxes are referenced by the creation-site expression built below. Only
        // what comes after it — generating the machine code — is deferrable (item 1-1).
        LambdaRewriter.Rewrite(innerLambda);

        var repository = BExpression.Field(@this, Closures.repositoryField);
        var id = methods is MethodRepository runtimeMethods
            && DeferredMethod.CanDefer(innerLambda, captureDiagnostics)
            ? runtimeMethods.RegisterDeferred(
                new DeferredMethod(innerLambda, this, enableJavaScriptTailCalls),
                innerLambda.Type)
            : RegisterGenerated(innerLambda);

        return BExpression.Call(repository, create, closures == null ? BExpression.Null : BExpression.NewArray(typeof(Box), closures), BExpression.Constant(id));
    }

    private ulong RegisterGenerated(BLambdaExpression innerLambda)
    {
        var (method, il, exp) = innerLambda.CompileToBoundDynamicMethod(
            methodBuilder: this,
            captureDiagnostics: captureDiagnostics,
            enableJavaScriptTailCalls: enableJavaScriptTailCalls);
        return methods.RegisterNew(method, il, exp, innerLambda.Type);
    }
}
