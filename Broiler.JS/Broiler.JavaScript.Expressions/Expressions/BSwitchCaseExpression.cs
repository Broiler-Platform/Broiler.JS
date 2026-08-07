namespace Broiler.JavaScript.ExpressionCompiler.Expressions;

public class BSwitchCaseExpression(BExpression body, BExpression[] testValues)
{
    public readonly BExpression Body = body;
    public readonly BExpression[] TestValues = testValues;
}