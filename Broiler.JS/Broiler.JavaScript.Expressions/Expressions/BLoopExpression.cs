using System.CodeDom.Compiler;

namespace Broiler.JavaScript.ExpressionCompiler.Expressions;

public class BLoopExpression(BExpression body, BLabelTarget @break, BLabelTarget @continue) : BExpression(BExpressionType.Loop, @break.LabelType)
{
    public readonly BExpression Body = body;
    public readonly new BLabelTarget Break = @break;
    public readonly new BLabelTarget Continue = @continue;

    public override void Print(IndentedTextWriter writer)
    {
        writer.WriteLine("while(true) {");
        writer.Indent++;
        Body.Print(writer);
        writer.Indent--;
        writer.WriteLine("}");
        writer.WriteLine($"{Break.Name}:");
    }
}