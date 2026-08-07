using System;
using System.CodeDom.Compiler;

namespace Broiler.JavaScript.ExpressionCompiler.Expressions;

public class BUnboxExpression(BExpression target, Type type) : BExpression(BExpressionType.Unbox, type)
{
    public readonly BExpression Target = target;

    public override void Print(IndentedTextWriter writer)
    {
        writer.Write($"({Type.GetFriendlyName()})");
        Target.Print(writer);
    }
}