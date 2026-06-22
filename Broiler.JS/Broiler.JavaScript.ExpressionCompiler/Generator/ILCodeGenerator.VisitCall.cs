#nullable enable
using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.ExpressionCompiler.Expressions;

namespace Broiler.JavaScript.ExpressionCompiler.Generator;

public partial class ILCodeGenerator
{
    private CodeInfo VisitTailCall(BCallExpression callExpression)
    {
        if (TryVisitJavaScriptTailCall(callExpression))
            return true;

        var parameters = callExpression.Method.GetParameters();
        if (parameters.Any(p => p.IsOut))
            return false;

        if (callExpression.Target != null)
            Visit(callExpression.Target);

        EmitParameters(callExpression.Method, callExpression.Arguments, callExpression.Type);
        // .net tail call works only in single threaded mode
        // we are unable to run unit tests with following
        // uncommented, still looking for the answer !!
        // il.Emit(OpCodes.Tailcall);
        il.Emit(!callExpression.Method.IsStatic
            ? OpCodes.Callvirt
            : OpCodes.Call, callExpression.Method);
        il.Emit(OpCodes.Ret);
        return true;
    }

    private bool TryVisitJavaScriptTailCall(BCallExpression callExpression)
    {
        if (!TryEmitJavaScriptTailCallValue(callExpression))
            return false;
        il.Emit(OpCodes.Ret);
        return true;
    }

    private bool TryEmitJavaScriptTailCallValue(BCallExpression callExpression)
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("BROILER_SCRIPT_HOST"), "1", StringComparison.Ordinal))
            return false;

        if (tailCallBlockedDepth != 0)
            return false;

        // Indirect-eval tail call: `return eval(...)` where eval is not %eval%
        // compiles to a static DirectEvalSupport.Execute call (not InvokeFunction).
        // Re-emit it with its trailing `tailCall` flag forced true so Execute returns
        // a JSTailCall sentinel for an indirect callee instead of recursing through
        // InvokeFunction. Only reached in a genuine tail position (script-host and
        // tailCallBlockedDepth == 0, checked above).
        if (callExpression.Method is { IsStatic: true, Name: "Execute" }
            && callExpression.Method.DeclaringType?.FullName == "Broiler.JavaScript.Compiler.DirectEvalSupport"
            && callExpression.Type.FullName == "Broiler.JavaScript.Runtime.JSValue")
        {
            var argCount = callExpression.Arguments.Count;
            var tailArgs = new Sequence<BExpression>(argCount + 1);
            for (int i = 0; i < argCount; i++)
                tailArgs.Add(callExpression.Arguments[i]);
            tailArgs.Add(BExpression.Constant(true));

            var save = EmitParameters(callExpression.Method, tailArgs, callExpression.Type);
            il.EmitCall(callExpression.Method);
            save();
            return true;
        }

        if (callExpression.Target == null
            || callExpression.Type.FullName != "Broiler.JavaScript.Runtime.JSValue"
            || callExpression.Method.Name != "InvokeFunction"
            || callExpression.Arguments.Count != 1)
            return false;

        var tailCallType = callExpression.Method.DeclaringType?.Assembly.GetType("Broiler.JavaScript.Runtime.JSTailCall");
        var tailCallConstructor = tailCallType?.GetConstructor([callExpression.Method.DeclaringType!, callExpression.Arguments[0].Type]);
        if (tailCallConstructor == null)
            return false;

        Visit(callExpression.Target);
        Visit(callExpression.Arguments[0]);
        il.Emit(OpCodes.Newobj, tailCallConstructor);
        return true;
    }


    protected override CodeInfo VisitCall(BCallExpression yCallExpression)
    {
        using (tempVariables.Push())
        {
            if(yCallExpression.Target != null)
            {
                Visit(yCallExpression.Target);
            }

            var a = EmitParameters(yCallExpression.Method, yCallExpression.Arguments, yCallExpression.Type);
            il.EmitCall(yCallExpression.Method);
            a();
        }
        return true;
    }
}
