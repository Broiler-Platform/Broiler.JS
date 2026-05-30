using System.CodeDom.Compiler;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.ExpressionCompiler.Expressions;

namespace Broiler.JavaScript.Compiler.Tests;

public class ExpressionLoggingTests
{
    [Fact]
    public void MemberInit_Print_Handles_ElementBindings_And_Null_Bindings()
    {
        var constructor = typeof(List<int>).GetConstructor(Type.EmptyTypes)!;
        var add = typeof(List<int>).GetMethod(nameof(List<int>.Add), [typeof(int)])!;
        var expression = YExpression.MemberInit(
            YExpression.New(constructor, Sequence<YExpression>.Empty),
            new Sequence<YBinding>
            {
                new YElementInit(add, YExpression.Constant(1)),
                null!
            });

        using var output = new StringWriter();
        using var writer = new IndentedTextWriter(output, "    ");

        expression.Print(writer);

        var printed = output.ToString();
        Assert.Contains("Add(1)", printed);
        Assert.Contains("<null binding>", printed);
    }
}
