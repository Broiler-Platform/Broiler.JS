using System.CodeDom.Compiler;

namespace Broiler.JavaScript.ExpressionCompiler.Expressions;

public class BBoxExpression(BExpression target) : BExpression(BExpressionType.Box, typeof(object))
{
    public readonly BExpression Target = target;

    public override void Print(IndentedTextWriter writer)
    {
        Target.Print(writer);
        writer.Write(" as object");
    }
}
