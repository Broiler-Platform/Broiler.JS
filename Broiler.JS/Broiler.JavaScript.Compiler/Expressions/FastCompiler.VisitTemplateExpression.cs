using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;

namespace Broiler.JavaScript.Compiler;

partial class FastCompiler
{
    protected override YExpression VisitTemplateExpression(AstTemplateExpression templateExpression)
    {
        var items = new Sequence<YExpression>(templateExpression.Parts.Count);
        var e = templateExpression.Parts.GetFastEnumerator();
        int size = 0;

        while (e.MoveNext(out var item))
        {
            if (item.Type == FastNodeType.Literal)
            {
                var l = item as AstLiteral;

                // An invalid escape sequence is allowed only in a tagged template
                // (where its cooked value is undefined). In a normal template
                // literal it is an early SyntaxError.
                if (l.Start.CookedInvalid)
                    throw new FastParseException(l.Start, "Invalid escape sequence in template literal");

                var txt = l.TokenType == TokenTypes.TemplatePart ? l.Start.CookedText : l.StringValue;

                size += txt.Length;
                items.Add(YExpression.Constant(txt));
            }
            else
            {
                items.Add(VisitExpression(item));
            }
        }

        return JSTemplateStringBuilder.New(items, size);
    }
}
