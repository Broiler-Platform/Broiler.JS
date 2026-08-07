using System.CodeDom.Compiler;

namespace Broiler.JavaScript.ExpressionCompiler.Expressions;

public class BArrayIndexExpression(BExpression target, BExpression index) : BExpression(BExpressionType.ArrayIndex, target.Type.GetElementType())
{
    public readonly BExpression Target = target;
    public new readonly BExpression Index = index;

    public override void Print(IndentedTextWriter writer)
    {
        Target.Print(writer);
        writer.Write("[");
        Index.Print(writer);
        writer.Write("]");
    }
}