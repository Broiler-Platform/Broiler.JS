using Broiler.JavaScript.ExpressionCompiler.Expressions;

namespace Broiler.JavaScript.ExpressionCompiler.Generator;


public partial class ILCodeGenerator
{

    protected override CodeInfo VisitConstant(BConstantExpression yConstantExpression)
    {
        il.EmitConstant(yConstantExpression.Value, yConstantExpression.Type);
        return true;
    }

}
