using System.CodeDom.Compiler;

namespace Broiler.JavaScript.ExpressionCompiler.Expressions;

public class BCoalesceExpression(BExpression left, BExpression right) : BExpression(BExpressionType.Coalesce, left.Type)
{
    public readonly BExpression Left = left;
    public readonly BExpression Right = right;

    public override void Print(IndentedTextWriter writer)
    {
        writer.Write("(");
        Left.Print(writer);
        writer.Write(" ?? ");
        Right.Print(writer);
        writer.Write(")");
    }
}