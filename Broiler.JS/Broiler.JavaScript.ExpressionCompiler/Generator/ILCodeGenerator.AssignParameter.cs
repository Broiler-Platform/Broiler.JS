using System;
using System.Reflection.Emit;
using Broiler.JavaScript.ExpressionCompiler.Expressions;

namespace Broiler.JavaScript.ExpressionCompiler.Generator;

public partial class ILCodeGenerator
{
    private CodeInfo AssignParameter(DataSource exp, BParameterExpression yParameterExpression, int savedIndex)
    {
        if (closureRepository.TryGet(yParameterExpression, out var ve))
        {
            InitializeClosure(yParameterExpression);
            return Assign(ve, exp, savedIndex);
        }

        VisitSave(exp, savedIndex);


        var pType = yParameterExpression.Type;
        if (!variables.TryGetValue(yParameterExpression, out var varInfo))
        {
            if (TryResolveClosureByName(yParameterExpression.Name, out var closure))
                return Assign(closure, exp, savedIndex);

            if (IsCompilerTemp(yParameterExpression.Name))
            {
                // Store-path counterpart to the read-path fallback in VisitParameter: a
                // pooled compiler scratch temp (#Temp<Type><id>) can reach the assignment
                // undeclared when the block that lists it in its Variables was not visited
                // by the IL generator before this write (an out-of-order VariableParameters
                // snapshot). Declare the local on demand — temps are keyed by reference, so
                // the on-demand local is the identical one every later read/write of this
                // temp resolves to (VisitParameter's TryGetValue and VisitBlock both find
                // the already-present key), keeping value semantics intact regardless of
                // visit order. Without this, a temp whose *first* emitted reference is a
                // write still fell through to the throwing indexer below and aborted the
                // whole script's compilation with KeyNotFoundException — the asymmetry that
                // the read-only VisitParameter fix left open. This branch only runs on the
                // path that already threw, so it cannot change a compilation that currently
                // succeeds; genuine user-variable resolution failures still surface via the
                // original throwing indexer (guarded to the unreachable "#Temp" prefix).
                varInfo = variables.Create(yParameterExpression);
            }
            else
            {
                varInfo = variables[yParameterExpression];
            }
        }

        il.Comment($"save {varInfo.Name}");

        if (pType.IsByRef)
        {
            AssignRefParameter(varInfo, pType);
            return true;
        }

        if (varInfo.IsArgument)
        {
            il.EmitSaveArg(varInfo.Index);
            return true;
        }
        var i = varInfo.LocalBuilder.LocalIndex;
        il.EmitSaveLocal(i);
        return true;
    }

    private void AssignRefParameter(Variable varInfo, Type pType)
    {
        if (!varInfo.IsArgument)
            throw new NotSupportedException();
        il.EmitLoadArg(varInfo.Index);
        var code = Type.GetTypeCode(pType);
        switch (code)
        {
            case TypeCode.Boolean:
                il.Emit(OpCodes.Stind_I1);
                return;
            case TypeCode.Byte:
                il.Emit(OpCodes.Stind_I1);
                return;
            case TypeCode.Char:
                il.Emit(OpCodes.Stind_I2);
                return;
            case TypeCode.DateTime:
                il.Emit(OpCodes.Stobj);
                return;
            case TypeCode.DBNull:
                il.Emit(OpCodes.Stobj);
                return;
            case TypeCode.Decimal:
                il.Emit(OpCodes.Stobj);
                return;
            case TypeCode.Double:
                il.Emit(OpCodes.Stind_R8);
                return;
            case TypeCode.Empty:
                break;
            case TypeCode.Int16:
                il.Emit(OpCodes.Stind_I2);
                return;
            case TypeCode.Int32:
                il.Emit(OpCodes.Stind_I4);
                return;
            case TypeCode.Int64:
                il.Emit(OpCodes.Stind_I8);
                return;
            case TypeCode.Object:
                il.Emit(OpCodes.Stind_Ref);
                return;
            case TypeCode.SByte:
                il.Emit(OpCodes.Stind_I1);
                return;
            case TypeCode.Single:
                il.Emit(OpCodes.Stind_R4);
                return;
            case TypeCode.String:
                il.Emit(OpCodes.Stind_Ref);
                return;
            case TypeCode.UInt16:
                il.Emit(OpCodes.Stind_I2);
                return;
            case TypeCode.UInt32:
                il.Emit(OpCodes.Stind_I4);
                return;
            case TypeCode.UInt64:
                il.Emit(OpCodes.Stind_I8);
                return;
        }
        throw new NotImplementedException();
    }

}
