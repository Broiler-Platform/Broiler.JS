using Broiler.JavaScript.LinqExpressions.LambdaGen;
using Expression = Broiler.JavaScript.ExpressionCompiler.Expressions.BExpression;

namespace Broiler.JavaScript.LinqExpressions.LinqExpressions.GeneratorsV2;

public class GeneratorStateBuilder
{
    public static Expression New(Expression value, int id, bool @delegate = false, bool isAwait = false) => NewLambdaExpression.NewExpression<GeneratorState>(() => () =>
    new GeneratorState(null, 0, false, false), value, Expression.Constant(id), Expression.Constant(@delegate), Expression.Constant(isAwait));

    public static Expression New(int id) => NewLambdaExpression.NewExpression<GeneratorState>(() => () =>
    new GeneratorState(null, 0, false, false), JSUndefinedBuilder.Value, Expression.Constant(id), Expression.Constant(false), Expression.Constant(false));
}
