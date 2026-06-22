#nullable enable
using System;
using System.CodeDom.Compiler;

namespace Broiler.JavaScript.ExpressionCompiler.Expressions;

public class BThrowExpression(BExpression exp, Type? type = null) : BExpression(BExpressionType.Throw, typeof(void))
{
    public readonly BExpression Expression = exp;

    public override void Print(IndentedTextWriter writer)
    {
        writer.Write("throw ");
        Expression.Print(writer);
    }
}