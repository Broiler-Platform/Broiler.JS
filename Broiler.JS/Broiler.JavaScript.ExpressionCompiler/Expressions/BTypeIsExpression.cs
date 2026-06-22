using System;
using System.CodeDom.Compiler;

namespace Broiler.JavaScript.ExpressionCompiler.Expressions;

public class BTypeIsExpression(BExpression target, Type type) : BExpression(BExpressionType.TypeIs, typeof(bool))
{
    public readonly BExpression Target = target;
    public readonly Type TypeOperand = type;

    public override void Print(IndentedTextWriter writer)
    {
        Target.Print(writer);
        writer.Write(" is ");
        writer.Write(TypeOperand.GetFriendlyName());
    }
}