#nullable enable
using System.CodeDom.Compiler;

namespace Broiler.JavaScript.ExpressionCompiler.Expressions;

public class BLabelExpression(BLabelTarget target, BExpression? defaultValue) : BExpression(BExpressionType.Label, target.LabelType)
{
    public readonly BLabelTarget Target = target;
    public readonly BExpression? Default = defaultValue;

    public override void Print(IndentedTextWriter writer)
    {
        if(Default != null)
        {
            writer.Write($"{Target.Name}: (");
            Default.Print(writer);
            writer.Write(")");
            return;
        }
        writer.WriteLine($"{Target.Name}:");
    }
}

public class BGoToExpression(BLabelTarget target, BExpression? defaultValue) : BExpression(BExpressionType.GoTo, target.LabelType)
{
    public readonly BLabelTarget Target = target;

    public readonly BExpression? Default = defaultValue;

    public override void Print(IndentedTextWriter writer)
    {
        if(Default!=null){
            writer.Write($"Goto {Target.Name} with (");
            Default.Print(writer);
            writer.Write(")");
            return;
        }

        writer.Write($"Goto {Target.Name}");
    }
}
public class BReturnExpression(BLabelTarget target, BExpression? defaultValue) : BExpression(BExpressionType.Return, defaultValue?.Type ?? typeof(void))
{
    public readonly BLabelTarget Target = target;
    public readonly BExpression? Default = defaultValue;

    public override void Print(IndentedTextWriter writer)
    {
        if (Default != null)
        {
            writer.Write("RETURN (");
            Default.Print(writer);
            writer.Write($") at {Target.Name}");
            return;
        }

        writer.Write($"RETURN {Target.Name}");
    }

    public BExpression Update(BLabelTarget target, BExpression x) => new BReturnExpression(target, x);
}