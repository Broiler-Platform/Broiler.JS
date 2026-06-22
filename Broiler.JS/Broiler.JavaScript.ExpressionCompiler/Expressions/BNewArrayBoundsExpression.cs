using System;
using System.CodeDom.Compiler;

namespace Broiler.JavaScript.ExpressionCompiler.Expressions;

public class BNewArrayBoundsExpression(Type type, BExpression size) : BExpression(BExpressionType.NewArrayBounds, type.MakeArrayType())
{
    public readonly Type ElementType = type;
    public readonly BExpression Size = size;

    public override void Print(IndentedTextWriter writer)
    {
        writer.Write($"new {ElementType.GetFriendlyName()} [");
        Size.Print(writer);
        writer.Write("]");
    }
}