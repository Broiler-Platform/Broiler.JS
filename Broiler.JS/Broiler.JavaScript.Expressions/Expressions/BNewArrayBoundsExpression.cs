using System.Diagnostics.CodeAnalysis;
using System;
using System.CodeDom.Compiler;

namespace Broiler.JavaScript.ExpressionCompiler.Expressions;

public class BNewArrayBoundsExpression : BExpression
{
    [RequiresDynamicCode("Array element types must have Native AOT instantiations available.")]
    public BNewArrayBoundsExpression(Type type, BExpression size) : this(type, type.MakeArrayType(), size) { }

    internal BNewArrayBoundsExpression(Type type, Type arrayType, BExpression size) : base(BExpressionType.NewArrayBounds, arrayType)
    {
        ElementType = type;
        Size = size;
    }
    public readonly Type ElementType;
    public readonly BExpression Size;

    public override void Print(IndentedTextWriter writer)
    {
        writer.Write($"new {ElementType.GetFriendlyName()} [");
        Size.Print(writer);
        writer.Write("]");
    }
}