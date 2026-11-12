#nullable enable
using System;
using System.CodeDom.Compiler;
using System.Globalization;
using System.Threading;

namespace Broiler.JavaScript.ExpressionCompiler.Expressions;

public class BParameterExpression: BExpression
{
    public readonly string Name;

    private static int id = 0;

    public BParameterExpression(Type type, string? name)
        :base(BExpressionType.Parameter, type)
    {
        name ??= string.Create(CultureInfo.InvariantCulture, $"{type.Name}_{Interlocked.Increment(ref id)}");
        Name = name;
    }

    public override void Print(IndentedTextWriter writer) => writer.Write(Name);
}