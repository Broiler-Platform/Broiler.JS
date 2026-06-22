#nullable enable
using System;
using System.CodeDom.Compiler;

namespace Broiler.JavaScript.ExpressionCompiler.Expressions;

public class BAssignExpression(BExpression left, BExpression right, Type? type) : BExpression(BExpressionType.Assign, type ?? left.Type)
{
    public readonly BExpression Left = left;
    public readonly BExpression Right = right;

    public override void Print(IndentedTextWriter writer)
    {
        Left.Print(writer);
        writer.Write(" = ");
        Right.Print(writer);
    }
}