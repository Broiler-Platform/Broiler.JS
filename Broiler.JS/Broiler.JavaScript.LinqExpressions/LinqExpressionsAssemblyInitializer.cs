using System;
using System.Runtime.CompilerServices;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.ExpressionCompiler.Runtime;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;
using Broiler.JavaScript.Runtime;
using Expression = Broiler.JavaScript.ExpressionCompiler.Expressions.BExpression;
using ParameterExpression = Broiler.JavaScript.ExpressionCompiler.Expressions.BParameterExpression;

namespace Broiler.JavaScript.LinqExpressions;

public static class LinqExpressionsAssemblyInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        // Wire up JSValueToClrConverter expression delegates
        JSValueToClrConverter.GetAtExpression = ArgumentsBuilder.GetAt;
        JSValueToClrConverter.LengthExpression = ArgumentsBuilder.Length;

        EnsureCompilationBackendsRegistered();
    }

    /// <summary>
    /// Loads <c>Broiler.JavaScript.ExpressionCompiler</c> so that ITS module initializer runs and
    /// registers the DynamicMethod and CollectibleAssembly compilation back ends before anything
    /// asks <c>ExpressionCompilationBackends.Get</c> for one.
    /// </summary>
    /// <remarks>
    /// Same hazard, and same remedy, as <c>CompilerAssemblyInitializer</c>: .NET loads assemblies
    /// lazily, so a <c>[ModuleInitializer]</c> that registers something runs only once the host has
    /// happened to touch a type in its assembly. Registration therefore depended on incidental load
    /// order rather than on configuration, and a host that never touched the emitter directly got
    /// "No compilation back end is registered for 'DynamicMethod'" while the assembly implementing
    /// it sat unloaded in its own output directory — which is what made every test in
    /// <c>Broiler.JavaScript.Modules.Tests</c> that compiles script fail.
    /// <para>
    /// This is the right assembly to do it from: it is the one that genuinely needs the emitter
    /// (see the ProjectReference comment in its .csproj — it compiles the trees it builds), and it
    /// is loaded while a compilation's expression tree is being built, which is necessarily before
    /// a back end is requested to emit that tree. The registry itself cannot do this: it lives in
    /// Broiler.JavaScript.Expressions, which the emitter depends on — so the reference would be a
    /// cycle — and that assembly is deliberately kept trim- and AOT-clean, which a load-by-name
    /// would spoil.
    /// </para>
    /// </remarks>
    private static void EnsureCompilationBackendsRegistered()
        => RuntimeHelpers.RunClassConstructor(
            typeof(global::Broiler.JavaScript.ExpressionCompiler.ExpressionCompiler).TypeHandle);

    internal static object CreateClrDelegate(Type type, IJSFunction function)
    {
        var method = type.GetMethod("Invoke");
        var rt = method.ReturnType;
        var rtt = rt == typeof(void) ? typeof(object) : rt;
        var pa = method.GetParameters();
        var veList = new Sequence<ParameterExpression>(pa.Length + 1);
        var peList = new Sequence<ParameterExpression>(pa.Length);
        var stmts = new Sequence<Expression>();

        foreach (var p in method.GetParameters())
        {
            var inP = Expression.Parameter(p.ParameterType, p.Name);
            peList.Add(inP);

            var jsV = Expression.Parameter(typeof(JSValue), "js" + p.Name);
            veList.Add(jsV);

            stmts.Add(Expression.Assign(jsV, ClrProxyBuilder.Marshal(inP)));
        }

        var @delegate = function.Delegate;
        var d = Expression.Constant(@delegate);
        var @this = Expression.Constant((JSValue)function);
        var nargs = ArgumentsBuilder.New(@this, veList.AsSequence<Expression>());

        if (rt == typeof(void) || rt == typeof(object))
        {
            stmts.Add(Expression.Invoke(d, nargs));
        }
        else
        {
            stmts.Add(JSValueToClrConverter.Get(Expression.Invoke(d, nargs), rt, ""));
        }

        return Expression.Lambda(type, Expression.Block(veList, stmts), type.Name, peList.ToArray()).Compile();
    }
}
