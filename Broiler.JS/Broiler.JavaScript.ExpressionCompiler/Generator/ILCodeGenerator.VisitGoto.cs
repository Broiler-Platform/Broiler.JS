using Broiler.JavaScript.ExpressionCompiler.Expressions;

namespace Broiler.JavaScript.ExpressionCompiler.Generator;

public partial class ILCodeGenerator
{

    protected override CodeInfo VisitGoto(BGoToExpression yGoToExpression)
    {
        il.Branch(labels[yGoToExpression.Target]);
        return true;
    }

}
