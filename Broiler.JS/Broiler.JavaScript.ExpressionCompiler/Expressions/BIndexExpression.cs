#nullable enable
using Broiler.JavaScript.ExpressionCompiler.Core;
using System.CodeDom.Compiler;
using System.Reflection;

namespace Broiler.JavaScript.ExpressionCompiler.Expressions;

public class BIndexExpression(BExpression target, PropertyInfo propertyInfo, IFastEnumerable<BExpression> args) : BExpression(BExpressionType.Index, propertyInfo.PropertyType)
{
    public readonly BExpression Target = target;
    public new readonly PropertyInfo Property = propertyInfo;
    public readonly IFastEnumerable<BExpression> Arguments = args;
    public readonly MethodInfo? SetMethod = propertyInfo.SetMethod;
    public readonly MethodInfo? GetMethod = propertyInfo.GetMethod;

    public override void Print(IndentedTextWriter writer)
    {
        Target?.Print(writer);
        writer.Write('[');
        writer.PrintCSV(Arguments);
        writer.Write(']');
    }
}