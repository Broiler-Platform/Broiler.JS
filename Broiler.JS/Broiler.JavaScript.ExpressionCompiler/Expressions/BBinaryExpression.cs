using System;
using System.CodeDom.Compiler;

namespace Broiler.JavaScript.ExpressionCompiler.Expressions;

public class BBinaryExpression(BExpression left, BOperator @operator, BExpression right) : BExpression(BExpressionType.Binary, GetType(@operator, left.Type, right.Type))
{
    public readonly BExpression Left = left;
    public readonly BOperator Operator = @operator;
    public readonly BExpression Right = right;

    private static Type GetType(BOperator @operator, Type leftType, Type rightType)
    {
        switch (@operator)
        {
            case BOperator.Less:
            case BOperator.LessOrEqual:
            case BOperator.Greater:
            case BOperator.GreaterOrEqual:
            case BOperator.Equal:
            case BOperator.NotEqual:
                if(!leftType.IsAssignableFrom(rightType))
                {
                    throw new NotSupportedException($"{@operator} cannot be applied {leftType} between {rightType}");
                }
                return typeof(bool);
        }
        return leftType;
    }

    public BExpression Update(BExpression left, BOperator @operator, BExpression right) => new BBinaryExpression(left, @operator, right);

    public override void Print(IndentedTextWriter writer)
    {
        writer.Write("(");
        Left.Print(writer);
        writer.Write($" {ToString(Operator)} ");
        Right.Print(writer);
        writer.Write(")");
    }

    private string ToString(BOperator @operator)
    {
        switch (@operator)
        {
            case BOperator.Add:
                return "+";
            case BOperator.Subtract:
                return "-";
            case BOperator.Multipley:
                return "*";
            case BOperator.Divide:
                return "/";
            case BOperator.Mod:
                return "%";
            case BOperator.Power:
                return "**";
            case BOperator.Xor:
                return "^";
            case BOperator.BitwiseAnd:
                return "&";
            case BOperator.BitwiseOr:
                return "|";
            case BOperator.BooleanAnd:
                return "&&";
            case BOperator.BooleanOr:
                return "||";
            case BOperator.Less:
                return "<";
            case BOperator.LessOrEqual:
                return "<=";
            case BOperator.Greater:
                return ">";
            case BOperator.GreaterOrEqual:
                return ">=";
            case BOperator.Equal:
                return "==";
            case BOperator.NotEqual:
                return "!=";
            case BOperator.LeftShift:
                return "<<";
            case BOperator.RightShift:
                return ">>";
            case BOperator.UnsignedRightShift:
                return ">>>";
        }

        throw new NotImplementedException();
    }
}