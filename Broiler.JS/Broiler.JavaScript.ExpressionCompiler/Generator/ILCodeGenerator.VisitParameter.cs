using System;
using System.Collections.Generic;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace Broiler.JavaScript.ExpressionCompiler.Generator;

public partial class ILCodeGenerator
{
    private static readonly Type JsContextType = Type.GetType("Broiler.JavaScript.Engine.JSContext, Broiler.JavaScript.Engine", true)!;
    private static readonly Type KeyStringsType = Type.GetType("Broiler.JavaScript.Storage.KeyStrings, Broiler.JavaScript.Storage", true)!;
    private static readonly Type StringSpanType = Type.GetType("Broiler.JavaScript.Ast.Misc.StringSpan, Broiler.JavaScript.Ast", true)!;
    private static readonly Type KeyStringType = Type.GetType("Broiler.JavaScript.Storage.KeyString, Broiler.JavaScript.Storage", true)!;
    private static readonly Type ScriptInfoType = Type.GetType("Broiler.JavaScript.Runtime.ScriptInfo, Broiler.JavaScript.Runtime", true)!;
    private static readonly Type ScriptInfoBoxType = typeof(Broiler.JavaScript.ExpressionCompiler.ClosureSeparator.Box<>).MakeGenericType(ScriptInfoType);
    private static readonly System.Reflection.ConstructorInfo ScriptInfoBoxCtor = ScriptInfoBoxType.GetConstructor(Type.EmptyTypes)!;
    private static readonly MethodInfo ResolveIdentifierMethod = JsContextType
        .GetMethod("ResolveIdentifier", [KeyStringType.MakeByRefType()])
        ?? throw new InvalidOperationException("JSContext.ResolveIdentifier(KeyString) not found");
    private static readonly MethodInfo KeyStringsGetOrCreateMethod = KeyStringsType
        .GetMethod("GetOrCreate", [StringSpanType.MakeByRefType()])
        ?? throw new InvalidOperationException("KeyStrings.GetOrCreate(StringSpan) not found");

    protected override CodeInfo VisitParameter(YParameterExpression yParameterExpression)
    {
        // check if it is marked as a closure...

        if (closureRepository.TryGet(yParameterExpression, out var ve))
        {
            InitializeClosure(yParameterExpression);
            return Visit(ve);
        }

        if (!variables.TryGetValue(yParameterExpression, out var v))
        {
            if (TryResolveVariableByName(yParameterExpression.Name, out v)
                || TryResolveBoxByType(yParameterExpression.Type, out v))
            {
                // resolved through the current scope
            }
            else if (TryResolveClosureByName(yParameterExpression.Name, out var closure))
            {
                return Visit(closure);
            }
            else if (yParameterExpression.Type == ScriptInfoType)
            {
                il.EmitNew(ScriptInfoBoxCtor);
                return true;
            }
            else
            {
                v = variables[yParameterExpression];
            }
        }

        il.Comment($"Load {v.Name}");
        var localType = v.LocalBuilder.LocalType;
        if (localType.IsGenericType
            && localType.GetGenericTypeDefinition() == typeof(Broiler.JavaScript.ExpressionCompiler.ClosureSeparator.Box<>)
            && localType.GetGenericArguments()[0] == yParameterExpression.Type)
        {
            il.EmitLoadLocal(v.LocalBuilder.LocalIndex);
            il.Emit(OpCodes.Ldfld, localType.GetField("Value"));
            return true;
        }

        var isValueType = yParameterExpression.Type.IsValueType;
        if (isValueType)
        {
            if (v.IsArgument)
            {
                il.EmitLoadArg(v.Index);
                return true;
            }

            il.EmitLoadLocal(v.LocalBuilder.LocalIndex);
            return true;
        }
        if (v.IsArgument)
        {
            // irrespective of RequiresAddress
            // retype always load ref...
            il.EmitLoadArg(v.Index);
            return true;
        }

        il.EmitLoadLocal(v.LocalBuilder.LocalIndex);
        if (v.IsReference)
        {
            throw new NotSupportedException();
        }

        return true;
    }

    private bool TryResolveVariableByName(string name, out Variable variable)
    {
        variable = null;
        if (string.IsNullOrEmpty(name))
            return false;

        if (variables.TryFindByName(name, out variable))
            return true;

        var resolvedName = NormalizeGeneratedName(name);
        foreach (var candidate in variables.Values)
        {
            if (string.Equals(NormalizeGeneratedName(candidate.Name), resolvedName, StringComparison.OrdinalIgnoreCase))
            {
                variable = candidate;
                return true;
            }
        }

        return false;
    }

    private bool TryResolveBoxByType(Type type, out Variable variable)
    {
        foreach (var candidate in variables.Values)
        {
            var localType = candidate.LocalBuilder.LocalType;
            if (localType.IsGenericType
                && localType.GetGenericTypeDefinition() == typeof(Broiler.JavaScript.ExpressionCompiler.ClosureSeparator.Box<>)
                && localType.GetGenericArguments()[0] == type)
            {
                variable = candidate;
                return true;
            }
        }

        variable = null;
        return false;
    }

    private bool TryResolveClosureByName(string name, out YExpression exp)
    {
        if (string.IsNullOrEmpty(name))
        {
            exp = default!;
            return false;
        }

        var resolvedName = NormalizeGeneratedName(name);
        foreach (var closure in closureRepository.Closures.Values)
        {
            if (string.Equals(NormalizeGeneratedName(closure.local.Name), resolvedName, StringComparison.OrdinalIgnoreCase))
            {
                exp = closure.value;
                return true;
            }
        }

        exp = default!;
        return false;
    }

    private static string NormalizeGeneratedName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        var resolvedName = name;
        if (resolvedName.EndsWith('`'))
            resolvedName = resolvedName[..^1];

        var underscore = resolvedName.LastIndexOf('_');
        if (underscore > 0 && int.TryParse(resolvedName[(underscore + 1)..], out _))
            return resolvedName[..underscore];

        var end = resolvedName.Length;
        while (end > 0 && char.IsDigit(resolvedName[end - 1]))
            end--;

        if (end > 0 && end < resolvedName.Length)
            return resolvedName[..end];

        return resolvedName;
    }
}
