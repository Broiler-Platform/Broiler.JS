#nullable enable
using System;
using System.Globalization;
using System.Threading;

namespace Broiler.JavaScript.ExpressionCompiler.Expressions;

public class BLabelTarget
{
    public readonly string Name;
    public readonly Type LabelType;

    private static int id = 0;

    public BLabelTarget(string? name, Type type)
    {
        name ??= string.Create(CultureInfo.InvariantCulture, $"LABEL_{Interlocked.Increment(ref id)}");
        Name = name;
        LabelType = type;
    }
}