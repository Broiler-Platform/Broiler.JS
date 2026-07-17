using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.ExpressionCompiler.Core;

namespace Broiler.JavaScript.ExpressionCompiler.Generator;

public class ActionList : IDisposable
{
    Sequence<Action> actions = [];

    public void Add(Action action) => actions.Add(action);

    public void Dispose()
    {
        var en = actions.GetFastEnumerator();
        while(en.MoveNext(out var a))
        {
            a();
        }
    }
}

public partial class ILCodeGenerator
{



    private MethodInfo StringEqualsMethod =
        typeof(string)
        .GetMethod(nameof(string.Equals), BindingFlags.Public | BindingFlags.Static, null, [ 
            typeof(string),
            typeof(string)
        ], null);

    private MethodInfo HashMatch =
        typeof(StringHashExtensions)
        .GetMethod(nameof(StringHashExtensions.HashMatch));

    private MethodInfo UnsafeGetHashCode =
        typeof(StringHashExtensions)
        .GetMethod(nameof(StringHashExtensions.UnsafeGetHashCode));

    protected override CodeInfo VisitSwitch(BSwitchExpression node)
    {

        using var tmp = tempVariables.Push();

        if (TryEmitDenseIntegerSwitch(node) || TryEmitStringHashSwitch(node))
            return true;

        bool isString = node.Target.Type == typeof(string);

        LocalVariableInfo hash = null;

        // save local if it is not parameter...
        Action loadTarget = LoadTargetMethod();

        var jumpMethod = LoadCompareMethod();

        var @break = il.DefineLabel("break", il.Top);

        using (var caseBodies = new ActionList())
        {
            foreach (var @case in node.Cases)
            {
                var jump = il.DefineLabel("caseBody", il.Top);
                foreach (var test in @case.TestValues)
                {
                    loadTarget();
                    jumpMethod(test, jump);

                }
                caseBodies.Add(() => {
                    il.MarkLabel(jump);
                    Visit(@case.Body);
                    il.Emit(OpCodes.Br, @break);
                });

            }

            if (node.Default != null)
            {
                Visit(node.Default);
            }
            il.Emit(OpCodes.Br, @break);
        }

        il.MarkLabel(@break);

        return true;

        Action LoadTargetMethod()
        {
            var t = node.Target;
            var isParameter = t.NodeType == BExpressionType.Parameter;
            if (isParameter && !isString)
            {
                return () => Visit(t);
            }
            var tmp = tempVariables[t.Type];
            Visit(t);
            if(isString)
            {
                hash = tempVariables[typeof(int)];
                if (!isParameter)
                {
                    il.Emit(OpCodes.Dup);
                }
                il.EmitConstant(0);
                il.EmitConstant(0);
                il.EmitCall(UnsafeGetHashCode);
                il.EmitSaveLocal(hash.LocalIndex);
                if (!isParameter)
                {
                    il.EmitSaveLocal(tmp.LocalIndex);
                }
                return () => {
                    if (isParameter) {
                        Visit(t);
                    }
                    else
                    {
                        il.EmitLoadLocal(tmp.LocalIndex);
                    }
                    il.EmitLoadLocal(hash.LocalIndex);
                };
            }
            il.EmitSaveLocal(tmp.LocalIndex);
            return () => il.EmitLoadLocal(tmp.LocalIndex);
        }

        Action<BExpression, ILWriterLabel> LoadCompareMethod()
        {
            void CompareInteger(BExpression test, ILWriterLabel target)
            {
                Visit(test);
                il.Emit(OpCodes.Beq, target);
            }

            if(node.Target.Type == typeof(int)) {
                return CompareInteger;
            }

            // A numeric (double) switch has no CompareMethod; compare with Beq,
            // which is an ordered comparison (NaN never matches), matching ===.
            if (node.Target.Type == typeof(double))
            {
                return CompareInteger;
            }

            var cm = node.CompareMethod;

            if (isString)
            {
                cm = StringEqualsMethod;
                void CompareString(BExpression test, ILWriterLabel target)
                {
                    var hash = (test as BStringConstantExpression).Value.ToString().UnsafeGetHashCode();
                    Visit(test);
                    il.EmitConstant(hash);
                    il.EmitCall(HashMatch);
                    il.Emit(OpCodes.Brtrue, target);
                }
                return CompareString;
            }

            void Compare(BExpression test, ILWriterLabel target)
            {
                Visit(test);
                il.EmitCall(cm);
                il.Emit(OpCodes.Brtrue, target);
            }

            return Compare;
        }

    }

    private bool TryEmitDenseIntegerSwitch(BSwitchExpression node)
    {
        if (node.Type != typeof(void) || node.Target.Type != typeof(int))
            return false;

        var tests = new List<(int Value, int Case)>();
        for (var caseIndex = 0; caseIndex < node.Cases.Length; caseIndex++)
        {
            foreach (var test in node.Cases[caseIndex].TestValues)
            {
                if (test is not BInt32ConstantExpression integer)
                    return false;
                tests.Add((integer.Value, caseIndex));
            }
        }

        if (tests.Count < 4)
            return false;

        var firstCases = new Dictionary<int, int>();
        var min = int.MaxValue;
        var max = int.MinValue;
        foreach (var test in tests)
        {
            firstCases.TryAdd(test.Value, test.Case);
            min = Math.Min(min, test.Value);
            max = Math.Max(max, test.Value);
        }

        var range64 = (long)max - min + 1;
        // Explicit code-size budget: at most 256 table entries and at least 50%
        // occupancy. Small/sparse switches retain the generic comparison emitter.
        if (range64 > 256 || range64 > firstCases.Count * 2L)
            return false;

        var range = (int)range64;
        var @break = il.DefineLabel("denseSwitchBreak", il.Top);
        var @default = il.DefineLabel("denseSwitchDefault", il.Top);
        var caseLabels = new ILWriterLabel[node.Cases.Length];
        for (var i = 0; i < caseLabels.Length; i++)
            caseLabels[i] = il.DefineLabel("denseSwitchCase", il.Top);

        var table = new Label[range];
        Array.Fill(table, @default.Value);
        foreach (var item in firstCases)
            table[item.Key - min] = caseLabels[item.Value].Value;

        Visit(node.Target);
        if (min != 0)
        {
            il.EmitConstant(min);
            il.Emit(OpCodes.Sub);
        }
        il.Emit(OpCodes.Switch, table);
        il.Emit(OpCodes.Br, @default);

        for (var i = 0; i < node.Cases.Length; i++)
        {
            il.MarkLabel(caseLabels[i]);
            Visit(node.Cases[i].Body);
            il.Emit(OpCodes.Br, @break);
        }

        il.MarkLabel(@default);
        if (node.Default != null)
            Visit(node.Default);
        il.Emit(OpCodes.Br, @break);
        il.MarkLabel(@break);
        ILSpecializationDiagnostics.RecordDenseIntegerSwitch(range);
        return true;
    }

    private bool TryEmitStringHashSwitch(BSwitchExpression node)
    {
        if (node.Type != typeof(void) || node.Target.Type != typeof(string))
            return false;

        var tests = new List<(string Value, int Case, int Hash)>();
        for (var caseIndex = 0; caseIndex < node.Cases.Length; caseIndex++)
        {
            foreach (var test in node.Cases[caseIndex].TestValues)
            {
                if (test is not BStringConstantExpression text)
                    return false;
                tests.Add((text.Value, caseIndex, text.Value.UnsafeGetHashCode()));
            }
        }

        if (tests.Count < 4 || tests.Count > 256)
            return false;

        var bucketCount = 4;
        while (bucketCount < tests.Count && bucketCount < 256)
            bucketCount <<= 1;
        var mask = bucketCount - 1;
        var buckets = new List<(string Value, int Case)>[bucketCount];
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var test in tests)
        {
            if (!seen.Add(test.Value))
                continue;
            var bucket = test.Hash & mask;
            (buckets[bucket] ??= new List<(string, int)>()).Add((test.Value, test.Case));
        }

        var @break = il.DefineLabel("stringSwitchBreak", il.Top);
        var @default = il.DefineLabel("stringSwitchDefault", il.Top);
        var caseLabels = new ILWriterLabel[node.Cases.Length];
        for (var i = 0; i < caseLabels.Length; i++)
            caseLabels[i] = il.DefineLabel("stringSwitchCase", il.Top);
        var bucketLabels = new ILWriterLabel[bucketCount];
        var table = new Label[bucketCount];
        for (var i = 0; i < bucketCount; i++)
        {
            bucketLabels[i] = il.DefineLabel("stringSwitchBucket", il.Top);
            table[i] = bucketLabels[i].Value;
        }

        using var target = il.NewTemp(typeof(string));
        using var hash = il.NewTemp(typeof(int));
        Visit(node.Target);
        il.EmitSaveLocal(target.LocalIndex);
        il.EmitLoadLocal(target.LocalIndex);
        il.EmitConstant(0);
        il.EmitConstant(0);
        il.EmitCall(UnsafeGetHashCode);
        il.EmitSaveLocal(hash.LocalIndex);
        il.EmitLoadLocal(hash.LocalIndex);
        il.EmitConstant(mask);
        il.Emit(OpCodes.And);
        il.Emit(OpCodes.Switch, table);
        il.Emit(OpCodes.Br, @default);

        for (var i = 0; i < bucketCount; i++)
        {
            il.MarkLabel(bucketLabels[i]);
            if (buckets[i] != null)
            {
                foreach (var test in buckets[i])
                {
                    il.EmitLoadLocal(target.LocalIndex);
                    il.EmitConstant(test.Value);
                    il.EmitCall(StringEqualsMethod);
                    il.Emit(OpCodes.Brtrue, caseLabels[test.Case]);
                }
            }
            il.Emit(OpCodes.Br, @default);
        }

        for (var i = 0; i < node.Cases.Length; i++)
        {
            il.MarkLabel(caseLabels[i]);
            Visit(node.Cases[i].Body);
            il.Emit(OpCodes.Br, @break);
        }

        il.MarkLabel(@default);
        if (node.Default != null)
            Visit(node.Default);
        il.Emit(OpCodes.Br, @break);
        il.MarkLabel(@break);
        ILSpecializationDiagnostics.RecordStringHashSwitch(bucketCount);
        return true;
    }
}
