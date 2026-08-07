using System.Linq.Expressions;
using Broiler.JavaScript.ExpressionCompiler.Expressions;

namespace Broiler.JavaScript.ExpressionCompiler.Converters;


public partial class LinqConverter
{
    protected override BExpression VisitLambda(LambdaExpression node) => VisitLambdaSpecific(node);
    public BLambdaExpression VisitLambdaSpecific(LambdaExpression lambda)
    {
        var plist = Register(lambda.Parameters);
        return BExpression.Lambda(lambda.Type, lambda.Name ?? "unnamed", Visit(lambda.Body), [.. plist]);
    }
}
