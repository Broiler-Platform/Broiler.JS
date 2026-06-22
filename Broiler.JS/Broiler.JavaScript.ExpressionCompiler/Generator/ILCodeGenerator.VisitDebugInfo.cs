using Broiler.JavaScript.ExpressionCompiler.Expressions;

namespace Broiler.JavaScript.ExpressionCompiler.Generator;

public partial class ILCodeGenerator
{

    protected override CodeInfo VisitDebugInfo(BDebugInfoExpression node)
    {
        SequencePoints.Add(new (il.ILOffset, node.Start, node.End));
        return true;
    }

}
