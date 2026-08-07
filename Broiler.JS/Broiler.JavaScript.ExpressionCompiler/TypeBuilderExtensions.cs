#nullable enable
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Broiler.JavaScript.ExpressionCompiler.Expressions;

namespace Broiler.JavaScript.ExpressionCompiler;

// The Reflection.Emit half of what used to be TypeExtensions.cs. The namespace is unchanged,
// so callers are unaffected; a separate class rather than a partial one because the two halves
// now ship from different assemblies.
internal static class TypeBuilderExtensions
{
    public static MethodBuilder CreateMethod(
        this TypeBuilder type,
        BLambdaExpression exp,
        string name, bool hasThis)
    {
        var dt = exp.Type;// this is delegate type...
        MethodInfo invoke = dt.GetMethod("Invoke");
        var pa = invoke.GetParameters();
        var pat = pa.Select(x => x.ParameterType).ToArray();
        var m = type.DefineMethod(
            name,
            MethodAttributes.Public,
            hasThis ? CallingConventions.HasThis : CallingConventions.Standard,
            invoke.ReturnType,
            pat);

        for (int i = 0; i < pa.Length; i++)
        {
            var p = pa[i];
            var pd = m.DefineParameter(i + 1, ParameterAttributes.None, p.Name);
        }


        return m;
    }
}
