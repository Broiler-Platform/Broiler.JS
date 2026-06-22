using Broiler.JavaScript.ExpressionCompiler.Core;
using System.CodeDom.Compiler;

namespace Broiler.JavaScript.ExpressionCompiler.Expressions;

public class BJumpSwitchExpression(BExpression target, IFastEnumerable<BLabelTarget> cases) : BExpression(BExpressionType.JumpSwitch, typeof(void))
{
    public readonly BExpression Target = target;
    public readonly IFastEnumerable<BLabelTarget> Cases = cases;

    public override void Print(IndentedTextWriter writer)
    {
        writer.Write("switch (");
        Target.Print(writer);
        writer.WriteLine(") {");
        writer.Indent++;
        int i = 0;
        var en = Cases.GetFastEnumerator();
        while(en.MoveNext(out var label))
        {
            writer.Write("case ");
            writer.Write(i++);
            writer.Write(": goto ");
            writer.Write(label.Name);
            writer.WriteLine(";");
        }
        writer.Indent--;
        writer.WriteLine("}");
    }
}
