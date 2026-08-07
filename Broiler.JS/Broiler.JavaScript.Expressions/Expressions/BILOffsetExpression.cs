using System.CodeDom.Compiler;

namespace Broiler.JavaScript.ExpressionCompiler.Expressions;

public class BILOffsetExpression : BExpression
{

    public BILOffsetExpression():
        base (BExpressionType.ILOffset, typeof(int))
    {

    }

    public override void Print(IndentedTextWriter writer) => writer.WriteLine("// IL Offset");
}
