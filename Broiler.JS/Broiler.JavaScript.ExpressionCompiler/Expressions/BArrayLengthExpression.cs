using System.CodeDom.Compiler;

namespace Broiler.JavaScript.ExpressionCompiler.Expressions;

public class BArrayLengthExpression(BExpression target) : BExpression(BExpressionType.ArrayLength, typeof(int))
{
    public readonly BExpression Target = target;

    public override void Print(IndentedTextWriter writer)
    {
        Target.Print(writer);
        writer.Write(".Length");
    }
}