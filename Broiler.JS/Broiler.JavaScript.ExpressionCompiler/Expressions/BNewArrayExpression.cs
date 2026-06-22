#nullable enable
using Broiler.JavaScript.ExpressionCompiler.Core;
using System;
using System.CodeDom.Compiler;

namespace Broiler.JavaScript.ExpressionCompiler.Expressions;

public class BNewArrayExpression(Type type, IFastEnumerable<BExpression> elements) : BExpression( BExpressionType.NewArray, type.MakeArrayType())
{
    public readonly IFastEnumerable<BExpression>? Elements = elements;
    public readonly Type ElementType = type;

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