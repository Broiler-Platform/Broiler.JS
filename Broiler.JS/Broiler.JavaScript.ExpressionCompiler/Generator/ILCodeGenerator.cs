#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.ExpressionCompiler.Core;

namespace Broiler.JavaScript.ExpressionCompiler.Generator;


public partial class ILCodeGenerator
{
    private readonly ILWriter il;

    public static bool GenerateLogs = false;
    private ClosureRepository closureRepository;
    private BParameterExpression? This;
    private readonly VariableInfo variables;
    private readonly LabelInfo labels;
    private readonly TempVariables tempVariables;
    private readonly IMethodBuilder methodBuilder;
    private readonly TextWriter? expressionWriter;
    private int tailCallTryDepth;
    private int tailCallBlockedDepth;

    private readonly Dictionary<BParameterExpression,(Type type, int localIndex)> uninitialized
        = new(Core.ReferenceEqualityComparer.Instance);

    public Sequence<ILDebugInfo> SequencePoints { get; }
        = [];

    /// <summary>
    /// IL code must load the address
    /// </summary>
    // public bool RequiresAddress => addressScope.RequiresAddress;

    public override string ToString() => il.ToString();

    public ILCodeGenerator(
        ILGenerator il,
        IMethodBuilder methodBuilder,
        TextWriter? writer = null,
        TextWriter? expressionWriter = null,
        bool captureDiagnostics = false)
    {
        if(!GenerateLogs && !captureDiagnostics)
        {
            writer = null;
            expressionWriter = null;
        }
        this.il = new ILWriter(il, writer);
        variables = new VariableInfo(il);
        labels = new LabelInfo(this.il);
        tempVariables = new TempVariables(this.il);
        this.methodBuilder = methodBuilder;
        this.expressionWriter = expressionWriter;
    }

    private void InitializeClosure(BParameterExpression pe)
    {
        if (uninitialized.TryGetValue(pe, out var x))
        {
            uninitialized.Remove(pe);
            il.EmitNew(x.type.GetConstructor());
            il.EmitSaveLocal(x.localIndex);
        }
    }

    internal void Emit(BLambdaExpression exp)
    {
        var body = exp.Body;

        short i = 0;
        if(exp.This != null)
        {
            variables.Create(exp.This, true, i++);
        }
        foreach(var p in exp.Parameters)
        {
            variables.Create(p, true, i++);
        }

        closureRepository = exp.GetClosureRepository();
        @This = exp.This;
        var closures = closureRepository.Closures;
        if (closures.Any())
        {
            bool isThisLoaded = false;
            // add temporary replacements
            // load this...

            // Outer Closures
            foreach (var kvp in closures.Where(x => x.Value.index != -1))
            {
                var (local, _, index, isArg) = kvp.Value;

                variables.Create(local, false, i);


                if (index != -1)
                {
                    if (!isThisLoaded)
                    {
                        isThisLoaded = true;
                        il.EmitLoadArg(0);
                        il.Emit(OpCodes.Ldfld, Closures.boxesField);
                    }
                    il.Emit(OpCodes.Dup);
                    il.EmitConstant(index);
                    il.Emit(OpCodes.Ldelem, local.Type);
                    // save it in field...
                    il.EmitSaveLocal(i);
                }

                i++;
            }
            if (isThisLoaded)
            {
                il.Emit(OpCodes.Pop);
            }

            // Self Closures (Needs initialization by parameters)
            foreach (var kvp in closures.Where(x => x.Value.index == -1))
            {
                var (local, original, index, argIndex) = kvp.Value;

                variables.Create(local, false, i);

                if (argIndex != -1)
                {
                    il.EmitLoadArg(argIndex + 1);
                    if (local.Type != original.Type)
                    {
                        var cnstr = local.Type.GetConstructor(original.Type);
                        il.EmitNew(cnstr);
                    }
                    il.EmitSaveLocal(i);
                }
                else
                {
                    // this is a problem in loop
                    // so box should be created when first assigned
                    // or read...
                    uninitialized[kvp.Key] = (local.Type, i);
                }

                i++;
            }

        }

        using (tempVariables.Push())
        {
            body = ReWriteTryCatch(body);
            RegisterBlockVariables(body);
            if(expressionWriter != null)
            {
                var writer = new System.CodeDom.Compiler.IndentedTextWriter(expressionWriter, "\t");
                body.Print(writer);
            }

            if(body.NodeType == BExpressionType.Call)
            {
                if(exp.ReturnType.IsAssignableFrom(body.Type) && body.Type != typeof(void))
                {
                    if (VisitTailCall((body as BCallExpression)!))
                        return;
                }
            }

            Visit(body);

            il.Emit(OpCodes.Ret);
        }
        il.Verify();

        return;

    }

    private void RegisterBlockVariables(BExpression expression)
    {
        switch (expression)
        {
            case BBlockExpression block:
                foreach (var p in block.FlattenVariables)
                {
                    if (!variables.TryGetValue(p, out _))
                        variables.Create(p);
                }
                foreach (var (child, _) in block.FlattenExpressions)
                    RegisterBlockVariables(child);
                break;

            case BConvertExpression convert:
                RegisterBlockVariables(convert.Target);
                break;

            case BReturnExpression @return when @return.Default != null:
                RegisterBlockVariables(@return.Default);
                break;

            case BTryCatchFinallyExpression tryCatchFinally:
                RegisterBlockVariables(tryCatchFinally.Try);
                if (tryCatchFinally.Catch != null)
                    RegisterBlockVariables(tryCatchFinally.Catch.Body);
                if (tryCatchFinally.Finally != null)
                    RegisterBlockVariables(tryCatchFinally.Finally);
                break;
        }
    }

    private static BExpression ReWriteTryCatch(BExpression body)
    {
        if (body.NodeType != BExpressionType.TryCatchFinally)
        {
            return body;
        }

        BTryCatchFinallyExpression exp = (body as BTryCatchFinallyExpression)!;

        var returnLabel = BExpression.Label("ReturnLabel", exp.Try.Type);

        // replace catchbody...
        var @catch = exp.Catch;
        if(@catch != null)
        {
            @catch = BExpression.Catch(@catch.Parameter!, BExpression.Return(returnLabel, @catch.Body));
        }

        exp = new BTryCatchFinallyExpression(
            BExpression.Return(returnLabel, exp.Try), @catch, exp.Finally);

        return BExpression.Block(exp, BExpression.Label(returnLabel));
        
    }
}
