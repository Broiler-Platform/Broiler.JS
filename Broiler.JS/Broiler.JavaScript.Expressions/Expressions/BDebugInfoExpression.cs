using System.CodeDom.Compiler;

namespace Broiler.JavaScript.ExpressionCompiler.Expressions;

public class BDebugInfoExpression(Position start, Position end) : BExpression(BExpressionType.DebugInfo, typeof(void))
{
    public readonly Position Start = start;
    public readonly Position End = end;

    public override void Print(IndentedTextWriter writer) => writer.WriteLine($"Sequence Point {Start} {End}");
}
