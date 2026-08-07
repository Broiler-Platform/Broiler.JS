using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.ExpressionCompiler.Expressions;

namespace Broiler.JavaScript.ExpressionCompiler;

public interface IMethodBuilder
{
    BExpression Relay(BExpression @this, IFastEnumerable<BExpression> closures, BLambdaExpression innerLambda);
}
