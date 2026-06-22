using System.CodeDom.Compiler;

namespace Broiler.JavaScript.ExpressionCompiler.Expressions;

public class BUnaryExpression(BExpression exp, BUnaryOperator @operator) : BExpression(BExpressionType.Unary, exp.Type)
{
    public readonly BExpression Target = exp;
    public readonly BUnaryOperator Operator = @operator;

    public override void Print(IndentedTextWriter writer)
    {
        switch (Operator)
        {
            case BUnaryOperator.Not:
                writer.Write("~(");
                Target.Print(writer);
                writer.Write(")");
                break;
            case BUnaryOperator.Negative:
                writer.Write("!(");
                Target.Print(writer);
                writer.Write(")");
                break;
        }
    }
}