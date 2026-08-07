using System.CodeDom.Compiler;

namespace Broiler.JavaScript.ExpressionCompiler.Expressions;

public class BAddressOfExpression(BExpression target) : BExpression(BExpressionType.AddressOf, target.Type.IsByRef ? target.Type : target.Type.MakeByRefType())
{
    public readonly BExpression Target = target;

    public override void Print(IndentedTextWriter writer)
    {
        writer.Write("ref ");
        Target.Print(writer);
    }
}
