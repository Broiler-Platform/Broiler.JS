#nullable enable
using System.CodeDom.Compiler;
using System.Linq;
using System.Reflection;

namespace Broiler.JavaScript.ExpressionCompiler.Expressions;

public class BSwitchExpression(BExpression target,
    MethodInfo? method,
    BExpression? defaultBody, BSwitchCaseExpression[] cases) : BExpression(
        BExpressionType.Switch,
        cases.Length > 0 ? cases[^1].Body.Type : defaultBody?.Type ?? typeof(void))
{
    public readonly BExpression Target = target;
    public readonly MethodInfo? CompareMethod = method;
    public readonly BExpression? Default = defaultBody;
    public readonly BSwitchCaseExpression[] Cases = cases;

    public override void Print(IndentedTextWriter writer)
    {
        writer.Write("switch(");
        Target.Print(writer);
        writer.WriteLine(") {");
        writer.Indent++;

        foreach(var @case in Cases)
        {
            foreach(var tv in @case.TestValues)
            {
                writer.Write("case ");
                tv.Print(writer);
                writer.WriteLine(":");
            }
            writer.Indent++;

            @case.Body.Print(writer);
            writer.WriteLine();
            writer.WriteLine("break;");
            writer.Indent--;
        }

        if(Default != null)
        {
            writer.WriteLine("default:");
            writer.Indent++;
            Default.Print(writer);
            writer.WriteLine("break;");
            writer.Indent--;
        }

        writer.Indent--;
        writer.WriteLine("}");
    }
}