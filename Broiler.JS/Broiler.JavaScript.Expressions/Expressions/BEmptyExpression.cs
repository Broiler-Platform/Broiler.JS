using System.CodeDom.Compiler;

namespace Broiler.JavaScript.ExpressionCompiler.Expressions;

public class BEmptyExpression: BExpression
{
    public BEmptyExpression()
        : base( BExpressionType.Empty, typeof(void))
    {
    }

    public override void Print(IndentedTextWriter writer) => writer.Write("<void>");
}