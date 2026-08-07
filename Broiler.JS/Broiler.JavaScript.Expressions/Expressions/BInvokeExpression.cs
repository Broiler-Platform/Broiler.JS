#nullable enable
using Broiler.JavaScript.ExpressionCompiler.Core;
using System;
using System.CodeDom.Compiler;

namespace Broiler.JavaScript.ExpressionCompiler.Expressions;

public class BInvokeExpression(BExpression target, IFastEnumerable<BExpression> args, Type type) : BExpression(BExpressionType.Invoke, type)
{
    public readonly BExpression Target = target;
    public readonly IFastEnumerable<BExpression> Arguments = args;

    public override void Print(IndentedTextWriter writer)
    {
        Target.Print(writer);
        writer.Write(".Invoke(");
        writer.PrintCSV(Arguments);
        writer.Write(")");
    }
}