#nullable enable
using System;
using System.Linq;
using System.Reflection;
using System.Text;
using Broiler.JavaScript.ExpressionCompiler.Expressions;

namespace Broiler.JavaScript.ExpressionCompiler;

// Public rather than internal because the emitter assembly's ILWriter uses Quoted() and
// GetFriendlyName() for its diagnostic output. The alternative — InternalsVisibleTo — is the
// trapdoor AssemblySplit.md's S-0 warns about: it would compile while preserving the coupling.
public static class TypeExtensions
{

    public static string Quoted(this string text)
    {
        StringBuilder sb = new();
        foreach(var che in text)
        {
            switch(che)
            {
                case '\n':
                    sb.Append("\\n");
                    break;
                case '\r':
                    sb.Append("\\r");
                    break;
                case '\t':
                    sb.Append("\\t");
                    break;
                default:
                    sb.Append(che);
                    break;
            }
        }
        return $"\"{sb.ToString()}\"";
    }

    public static ConstructorInfo GetConstructor(this Type type, params Type[] args)
        => type.GetConstructor(args);


    public static Type? GetUnderlyingTypeIfRef(this Type? type)
    {
        if (type == null)
        {
            return type;
        }
        if (type.IsByRef)
        {
            return type.GetElementType();
        }
        return type;
    }
    public static string GetFriendlyName(this MethodInfo method)
    {
        if (method.IsGenericMethod)
        {
            return method.Name + "<" + string.Join(",", method.GetGenericArguments().Select(x => x.GetFriendlyName())) + ">";
        }
        return method.Name;
    }

        public static string GetFriendlyName(this Type? type)
    {
        if (type == null)
            return "";
        if(type.IsArray)
        {
            return type.GetElementType().GetFriendlyName() + "[]";
        }
        if(type.IsConstructedGenericType)
        {
            var a = string.Join(", ", type.GetGenericArguments().Select(x => x.GetFriendlyName()));
            return $"{type.Name}<{a}>";
        }
        if(type.IsGenericTypeDefinition)
        {
            return $"{type.Name}<>";
        }
        return type.Name;
    }
}
