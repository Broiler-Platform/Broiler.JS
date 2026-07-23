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
    private static readonly Type ScriptInfoType = Type.GetType("Broiler.JavaScript.Runtime.ScriptInfo, Broiler.JavaScript.Runtime", true)!;
    private static readonly Type ScriptInfoBoxType = typeof(Broiler.JavaScript.ExpressionCompiler.ClosureSeparator.Box<>).MakeGenericType(ScriptInfoType);
    private static readonly System.Reflection.ConstructorInfo ScriptInfoBoxCtor = ScriptInfoBoxType.GetConstructor(Type.EmptyTypes)!;
    private static readonly MethodInfo ResolveIdentifierMethod = JsContextType
        .GetMethod("ResolveIdentifier", [KeyStringType.MakeByRefType()])
        ?? throw new InvalidOperationException("JSContext.ResolveIdentifier(KeyString) not found");
    private static readonly MethodInfo KeyStringsGetOrCreateMethod = KeyStringsType
        .GetMethod("GetOrCreate", [StringSpanType.MakeByRefType()])
        ?? throw new InvalidOperationException("KeyStrings.GetOrCreate(StringSpan) not found");

    protected override CodeInfo VisitParameter(BParameterExpression yParameterExpression)
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
            else if (IsCompilerTemp(yParameterExpression.Name))
            {
                // A pooled compiler scratch temp (e.g. "#TempJSValue123") can reach
                // here undeclared: the block that lists it in its Variables is visited
                // by the IL generator only when Visit descends into it, so a reference
                // that is emitted before that block runs — or one whose declaring block
                // was dropped by an out-of-order VariableParameters snapshot — finds no
                // local yet. Temps are keyed by reference, so declaring the local now
                // yields the very same local every later read/write of this temp
                // resolves to (VisitBlock skips re-creating an already-present key),
                // keeping value semantics intact regardless of visit order. Previously
                // this fell through to the indexer below and threw KeyNotFoundException,
                // aborting the entire script's compilation — the WPT
                // "#TempJSValue… was not present in the dictionary" crash cluster. This
                // branch only runs on the path that already failed, so it cannot change
                // a compilation that currently succeeds.
                v = variables.Create(yParameterExpression);
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

    // Compiler-synthesized scratch temporaries are named "#Temp<Type><id>" by
    // FastFunctionScope.GetTempVariable. The "#Temp" prefix is unreachable for a
    // real JS binding (a source identifier cannot start with '#', and private
    // names are "#name" without the "Temp" segment), so it precisely identifies a
    // pooled temp whose local may be declared on demand without shadowing a user
    // variable resolution failure — which must still surface.
    private static bool IsCompilerTemp(string name)
        => name is not null && name.StartsWith("#Temp", StringComparison.Ordinal);

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

    private bool TryResolveClosureByName(string name, out BExpression exp)
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
