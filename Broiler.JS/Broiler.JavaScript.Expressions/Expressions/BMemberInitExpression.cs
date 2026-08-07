using Broiler.JavaScript.ExpressionCompiler.Core;
using System.CodeDom.Compiler;

namespace Broiler.JavaScript.ExpressionCompiler.Expressions;

public class BMemberInitExpression(BNewExpression exp, IFastEnumerable<BBinding> list) : BExpression(BExpressionType.MemberInit, exp.Type)
{
    public readonly BNewExpression Target = exp;
    public readonly IFastEnumerable<BBinding> Bindings = list;

    public override void Print(IndentedTextWriter writer)
    {
        Target.Print(writer);
        writer.WriteLine("{");
        writer.Indent++;
        var en = Bindings.GetFastEnumerator();
        while(en.MoveNext(out var b))
        {
            if (b == null)
            {
                writer.WriteLine("<null binding>,");
                continue;
            }

            writer.Write(b.Member?.Name ?? "<null>");
            writer.Write(" = ");

            switch (b.BindingType)
            {
                case BindingType.MemberAssignment:
                    (b as BMemberAssignment)?.Value?.Print(writer);
                    break;
                case BindingType.MemberListInit:
                    PrintElements(writer, (b as BMemberElementInit)?.Elements);
                    break;
                case BindingType.ElementInit:
                    PrintElement(writer, b as BElementInit);
                    break;
                default:
                    writer.Write($"<{b.BindingType}>");
                    break;
            }

            writer.WriteLine(",");
        }
        writer.Indent--;
        writer.Write("}");
    }

    private static void PrintElements(IndentedTextWriter writer, BElementInit[]? elements)
    {
        writer.Write("[");
        if (elements != null)
        {
            var isFirst = true;
            foreach (var element in elements)
            {
                if (!isFirst)
                    writer.Write(", ");

                isFirst = false;
                PrintElement(writer, element);
            }
        }
        writer.Write("]");
    }

    private static void PrintElement(IndentedTextWriter writer, BElementInit? element)
    {
        if (element == null)
        {
            writer.Write("<null element>");
            return;
        }

        writer.Write(element.AddMethod?.Name ?? "<null>");
        writer.Write("(");
        var en = element.Arguments.GetFastEnumerator();
        var isFirst = true;
        while (en.MoveNext(out var argument))
        {
            if (!isFirst)
                writer.Write(", ");

            isFirst = false;
            argument?.Print(writer);
        }
        writer.Write(")");
    }
}
