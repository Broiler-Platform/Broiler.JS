using Expression = Broiler.JavaScript.ExpressionCompiler.Expressions.BExpression;
using Broiler.JavaScript.LinqExpressions.LambdaGen;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.LinqExpressions.LinqExpressions;

public class JSModuleExportsBuilder
{
    /// <summary>
    /// Emits the copy half of <c>export * from '…'</c>. The set of names is whatever the source
    /// module turned out to export, so it is resolved at run time rather than emitted specifier by
    /// specifier the way <c>export { a } from '…'</c> is.
    /// </summary>
    public static Expression CopyStarExports(Expression source, Expression target)
        => NewLambdaExpression.StaticCallExpression(
            () => () => JSModuleExports.CopyStarExports(null, null), source, target);
}
