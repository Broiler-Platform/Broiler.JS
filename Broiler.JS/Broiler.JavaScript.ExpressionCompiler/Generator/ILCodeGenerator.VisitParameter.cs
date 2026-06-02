using System;
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
    private static readonly Type JsValueType = Type.GetType("Broiler.JavaScript.Runtime.JSValue, Broiler.JavaScript.Runtime", true)!;
    private static readonly Type ScriptInfoType = Type.GetType("Broiler.JavaScript.Runtime.ScriptInfo, Broiler.JavaScript.Runtime", true)!;
    private static readonly Type JsEngineType = Type.GetType("Broiler.JavaScript.Engine.Core.JSEngine, Broiler.JavaScript.Engine", true)!;
    private static readonly Type ScriptInfoBoxType = typeof(Broiler.JavaScript.ExpressionCompiler.ClosureSeparator.Box<>).MakeGenericType(ScriptInfoType);
    private static readonly System.Reflection.ConstructorInfo ScriptInfoBoxCtor = ScriptInfoBoxType.GetConstructor(Type.EmptyTypes)!;
    private static readonly MethodInfo ResolveIdentifierMethod = JsContextType
        .GetMethod("ResolveIdentifier", [KeyStringType.MakeByRefType()])
        ?? throw new InvalidOperationException("JSContext.ResolveIdentifier(KeyString) not found");
    private static readonly MethodInfo KeyStringsGetOrCreateMethod = KeyStringsType
        .GetMethod("GetOrCreate", [StringSpanType.MakeByRefType()])
        ?? throw new InvalidOperationException("KeyStrings.GetOrCreate(StringSpan) not found");
    private static readonly FieldInfo JsEngineCurrentField = JsEngineType
        .GetField("Current", BindingFlags.Public | BindingFlags.Static)
        ?? throw new InvalidOperationException("JSEngine.Current field not found");
    private static readonly ConstructorInfo StringSpanStringCtor = StringSpanType
        .GetConstructor([typeof(string)])
        ?? throw new InvalidOperationException("StringSpan(string) constructor not found");

    protected override CodeInfo VisitParameter(YParameterExpression yParameterExpression)
    {
        // check if it is marked as a closure...

        if (closureRepository.TryGet(yParameterExpression, out var ve))
        {
            InitializeClosure(yParameterExpression);
            return Visit(ve);
        }

        if (!TryResolveVariable(yParameterExpression, out var v))
        {
            if (TryResolveBoxByType(yParameterExpression.Type, out v))
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
            else if (TryEmitRuntimeIdentifierResolution(yParameterExpression))
            {
                return true;
            }
            else
            {
                throw new InvalidOperationException($"Unable to resolve parameter '{yParameterExpression.Name}'.");
            }
        }

        il.Comment($"Load {v.Name}");
        if (v.IsArgument)
        {
            // irrespective of RequiresAddress
            // retype always load ref...
            il.EmitLoadArg(v.Index);
            return true;
        }

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
            il.EmitLoadLocal(v.LocalBuilder.LocalIndex);
            return true;
        }

        il.EmitLoadLocal(v.LocalBuilder.LocalIndex);
        if (v.IsReference)
        {
            throw new NotSupportedException();
        }

        return true;
    }

    private bool TryEmitRuntimeIdentifierResolution(YParameterExpression yParameterExpression)
    {
        if (string.IsNullOrEmpty(yParameterExpression.Name)
            || yParameterExpression.Type != JsValueType)
        {
            return false;
        }

        using var spanLocal = il.NewTemp(StringSpanType);
        using var keyLocal = il.NewTemp(KeyStringType);

        il.EmitConstant(yParameterExpression.Name);
        il.EmitNew(StringSpanStringCtor);
        il.EmitSaveLocal(spanLocal.LocalIndex);

        il.EmitLoadLocalAddress(spanLocal.LocalIndex);
        il.EmitCall(KeyStringsGetOrCreateMethod);
        il.EmitSaveLocal(keyLocal.LocalIndex);

        il.Emit(OpCodes.Ldsfld, JsEngineCurrentField);
        il.Emit(OpCodes.Castclass, JsContextType);
        il.EmitLoadLocalAddress(keyLocal.LocalIndex);
        il.EmitCall(ResolveIdentifierMethod);
        return true;
    }

    private bool TryResolveVariable(YParameterExpression yParameterExpression, out Variable variable)
    {
        if (variables.TryGetValue(yParameterExpression, out variable))
            return true;

        return TryResolveVariableByName(yParameterExpression.Name, out variable);
    }

    private bool TryResolveVariableByName(string name, out Variable variable)
    {
        variable = null;
        if (string.IsNullOrEmpty(name))
            return false;

        var resolvedName = name;
        if (resolvedName.EndsWith('`'))
            resolvedName = resolvedName[..^1];
        var underscore = name.LastIndexOf('_');
        if (underscore > 0 && int.TryParse(name[(underscore + 1)..], out _))
            resolvedName = name[..underscore];

        if (variables.TryFindByName(resolvedName, out variable))
            return true;

        return variables.TryFindByName(name, out variable);
    }

    private bool TryResolveBoxByType(Type type, out Variable variable)
    {
        foreach (var candidate in variables.Values)
        {
            if (candidate.IsArgument || candidate.LocalBuilder == null)
                continue;

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

        foreach (var closure in closureRepository.Closures.Values)
        {
            if (string.Equals(closure.local.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                exp = closure.value;
                return true;
            }
        }

        exp = default!;
        return false;
    }
}
