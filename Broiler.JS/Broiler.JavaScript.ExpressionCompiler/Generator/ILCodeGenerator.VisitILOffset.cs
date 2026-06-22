using Broiler.JavaScript.ExpressionCompiler.Expressions;

namespace Broiler.JavaScript.ExpressionCompiler.Generator;

public partial class ILCodeGenerator
{
    protected override CodeInfo VisitILOffset(BILOffsetExpression node)
    {
        il.EmitConstant(il.ILOffset);
        return true;
    }
}
