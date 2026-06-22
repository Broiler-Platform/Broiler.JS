#nullable enable
using System;
using System.CodeDom.Compiler;

namespace Broiler.JavaScript.ExpressionCompiler.Expressions;

public class BConditionalExpression(
    BExpression test,
    BExpression @true,
    BExpression? @false,
    Type? type = null) : BExpression(BExpressionType.Conditional, type ?? @true.Type)
{
    public readonly BExpression test = test;
    public readonly BExpression @true = @true;
    public readonly BExpression? @false = @false;

    public override void Print(IndentedTextWriter writer)
    {
        writer.Write("if(");
        test.Print(writer);
        writer.Write(')');
        writer.WriteLine(" {");
        writer.Indent++;
        @true.Print(writer);
        writer.Indent--;

        if(@false != null)
        {
            writer.WriteLine("} else {");
            writer.Indent++;
            @false.Print(writer);
            writer.Indent--;
        }

        writer.WriteLine('}');
    }
}