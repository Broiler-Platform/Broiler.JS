using System;
using System.CodeDom.Compiler;

namespace Broiler.JavaScript.ExpressionCompiler.Expressions;

public class BTypeAsExpression(BExpression target, Type type) : BExpression(BExpressionType.TypeAs, type)
{
    public readonly BExpression Target = target;

    public override void Print(IndentedTextWriter writer)
    {
        Target.Print(writer);
        writer.Write(" as ");
        writer.Write(Type.GetFriendlyName());
    }
}