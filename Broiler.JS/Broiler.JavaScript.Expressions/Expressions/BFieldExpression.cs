using System.CodeDom.Compiler;
using System.Reflection;

namespace Broiler.JavaScript.ExpressionCompiler.Expressions;

public class BFieldExpression(BExpression target, FieldInfo field) : BExpression(BExpressionType.Field, field.FieldType)
{
    public readonly BExpression Target = target;
    public readonly FieldInfo FieldInfo = field;

    public override void Print(IndentedTextWriter writer)
    {
        if(Target==null)
        {
            writer.Write($"{FieldInfo.DeclaringType.GetFriendlyName()}.{FieldInfo.Name}");
            return;
        }
        Target.Print(writer);
        writer.Write('.');
        writer.Write(FieldInfo.Name);
    }
}