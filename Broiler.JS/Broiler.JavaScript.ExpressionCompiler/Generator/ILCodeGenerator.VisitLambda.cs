using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.ExpressionCompiler.Expressions;

namespace Broiler.JavaScript.ExpressionCompiler.Generator;

public partial class ILCodeGenerator
{
    protected override CodeInfo VisitLambda(BLambdaExpression yLambdaExpression)
    {

        var closureRepository = yLambdaExpression.GetClosureRepository();
        var captures = closureRepository.Inputs.AsSequence<BExpression>();
        yLambdaExpression.SetupAsClosure();

        return Visit(methodBuilder.Relay(This, captures, yLambdaExpression));
    }
}
