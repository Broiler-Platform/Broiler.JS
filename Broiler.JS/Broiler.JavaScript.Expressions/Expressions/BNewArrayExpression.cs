using System.Diagnostics.CodeAnalysis;
#nullable enable
using Broiler.JavaScript.ExpressionCompiler.Core;
using System;
using System.CodeDom.Compiler;

namespace Broiler.JavaScript.ExpressionCompiler.Expressions;

public class BNewArrayExpression : BExpression
{
    [RequiresDynamicCode("Array element types must have Native AOT instantiations available.")]
    public BNewArrayExpression(Type type, IFastEnumerable<BExpression> elements) : this(type, type.MakeArrayType(), elements) { }

    internal BNewArrayExpression(Type type, Type arrayType, IFastEnumerable<BExpression> elements) : base(BExpressionType.NewArray, arrayType)
    {
        Elements = elements;
        ElementType = type;
    }
    public readonly IFastEnumerable<BExpression>? Elements;
    public readonly Type ElementType;

    public override void Print(IndentedTextWriter writer)
    {
        if (Elements == null || Elements.Count == 0){
            writer.WriteLine($"new {ElementType.GetFriendlyName()} [] {{}}");
            return;
        }

        writer.WriteLine($"new {ElementType.GetFriendlyName()} [] {{");
        writer.Indent++;
        var en = Elements.GetFastEnumerator();
        while(en.MoveNext(out var a))
        {
            a.Print(writer);
            writer.WriteLine(',');
        }
        writer.Indent--;
        writer.WriteLine("}");
    }
}