using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Compiler;

partial class FastCompiler
{
    private static readonly System.Reflection.PropertyInfo TemplateSubstitutionStringValueProperty =
        typeof(JSValue).GetProperty(nameof(JSValue.StringValue));

    protected override BExpression VisitTemplateExpression(AstTemplateExpression templateExpression)
    {
        var items = new Sequence<BExpression>(templateExpression.Parts.Count);
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
                items.Add(BExpression.Constant(txt));
            }
            else
            {
                // §13.2.8.6 TemplateLiteral evaluation coerces each substitution with
                // ToString — which throws a TypeError for a Symbol and converts an object
                // through its toString with the "string" hint. Read the substitution's
                // StringValue (the spec ToString) here rather than letting the lenient CLR
                // ToString of the runtime template builder stringify a Symbol (test262
                // sm/object/toPrimitive: `` `${Symbol()}` `` must throw).
                var substitution = VisitExpression(item);
                items.Add(JSStringBuilder.New(BExpression.Property(substitution, TemplateSubstitutionStringValueProperty)));
            }
        }

        return JSTemplateStringBuilder.New(items, size);
    }
}
