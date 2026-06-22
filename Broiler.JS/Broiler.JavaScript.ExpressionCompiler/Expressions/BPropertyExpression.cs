#nullable enable
using System.CodeDom.Compiler;
using System.Reflection;

namespace Broiler.JavaScript.ExpressionCompiler.Expressions;

public class BPropertyExpression : BExpression
{
    public readonly BExpression Target;
    public readonly PropertyInfo PropertyInfo;
    public readonly MethodInfo? GetMethod;
    public readonly MethodInfo? SetMethod;
    public readonly bool IsStatic;

    public BPropertyExpression(BExpression target, PropertyInfo property)
        : base(BExpressionType.Property, property.PropertyType)
    {
        Target = target;
        PropertyInfo = property;

        if (property.CanRead)
        {
            GetMethod = property.GetMethod;
            IsStatic = GetMethod.IsStatic;
        }
        if(property.CanWrite)
        {
            SetMethod = property.SetMethod;
            IsStatic = SetMethod.IsStatic;
        }
    }

    public override void Print(IndentedTextWriter writer)
    {
        if (Target == null)
        {
            writer.Write($"{PropertyInfo.DeclaringType.GetFriendlyName()}.{PropertyInfo.Name}");
            return;
        }
        Target.Print(writer);
        writer.Write('.');
        writer.Write(PropertyInfo.Name);
    }
}