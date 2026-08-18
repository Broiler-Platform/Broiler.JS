using System;
using System.Globalization;
using System.Text;
using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;

namespace Broiler.JavaScript.Compiler;

partial class FastCompiler
{
    /// <summary>
    /// Element count at which an all-constant ArrayLiteral is emitted as a template rather than
    /// element by element.
    /// </summary>
    /// <remarks>
    /// Below it the inline form is the faster one: it builds the elements with no decode step at
    /// all, and its IL is small enough that nothing downstream notices. The cost the template
    /// avoids is superlinear in the method it is emitted into, so the threshold only has to be
    /// low enough to catch a literal used as a data table — where the counts are in the
    /// thousands, not near any number that could be argued about.
    /// </remarks>
    private const int ConstantArrayTemplateThreshold = 256;

    protected override BExpression VisitArrayExpression(AstArrayExpression arrayExpression)
    {
        if (TryEncodeConstantArrayLiteral(arrayExpression, out var template))
            return JSArrayBuilder.FromTemplate(template);

        var e = arrayExpression.Elements.GetFastEnumerator();
        var list = new Sequence<BElementInit>();

        while (e.MoveNext(out var item))
        {
            if (item == null)
            {
                list.Add(BExpression.ElementInit(JSArrayBuilder._Add, [BExpression.Null]));
                continue;
            }

            if (item.Type == FastNodeType.SpreadElement)
            {
                var i = (item as AstSpreadElement).Argument;
                list.Add(BExpression.ElementInit(JSArrayBuilder._AddRange, [Visit(i)]));
                continue;
            }

            list.Add(BExpression.ElementInit(JSArrayBuilder._Add, [Visit(item)]));
        }

        if (list.Count > 0)
            return BExpression.ListInit(BExpression.New(JSArrayBuilder._New), list);

        return BExpression.New(JSArrayBuilder._New);
    }

    /// <summary>
    /// Encodes an ArrayLiteral whose every element is a constant — a string, number, boolean or
    /// null literal, an elision, or a nested such array — into the compact description
    /// <c>JSArray.FromTemplate</c> reads back, when the literal is large enough to be worth it.
    /// </summary>
    /// <remarks>
    /// The grammar is self-delimiting so nothing needs escaping: <c>a</c>COUNT<c>:</c> followed
    /// by that many elements is an array, <c>s</c>LENGTH<c>:</c> followed by that many chars is a
    /// string, <c>d</c> followed by 16 hex digits is a number's IEEE-754 bits, and <c>t</c>,
    /// <c>f</c>, <c>n</c>, <c>h</c> are true, false, null and a hole.
    /// <para>
    /// Deliberately narrow. A RegularExpressionLiteral is an object with per-evaluation identity
    /// and mutable <c>lastIndex</c>, so it is not a constant; a BigInt or a Decimal only misses
    /// because nothing needs them yet; an ObjectLiteral has __proto__, accessors, computed keys
    /// and duplicate-key ordering to answer for. Anything not listed here simply takes the
    /// inline path, which is what it did before.
    /// </para>
    /// </remarks>
    private static bool TryEncodeConstantArrayLiteral(AstArrayExpression arrayExpression, out string encoded)
    {
        encoded = null;
        var builder = new StringBuilder();
        var elementCount = 0;
        if (!TryEncodeConstantArray(arrayExpression, builder, ref elementCount)
            || elementCount < ConstantArrayTemplateThreshold)
        {
            return false;
        }

        encoded = builder.ToString();
        return true;
    }

    private static bool TryEncodeConstantArray(AstArrayExpression arrayExpression, StringBuilder builder, ref int elementCount)
    {
        builder.Append('a');
        builder.Append(arrayExpression.Elements.Count.ToString(CultureInfo.InvariantCulture));
        builder.Append(':');

        var enumerator = arrayExpression.Elements.GetFastEnumerator();
        while (enumerator.MoveNext(out var element))
        {
            elementCount++;
            if (!TryEncodeConstantElement(element, builder, ref elementCount))
                return false;
        }

        return true;
    }

    private static bool TryEncodeConstantElement(AstExpression element, StringBuilder builder, ref int elementCount)
    {
        // An elision: the length advances, no element is created.
        if (element == null)
        {
            builder.Append('h');
            return true;
        }

        if (element is AstArrayExpression nested)
            return TryEncodeConstantArray(nested, builder, ref elementCount);

        if (element is not AstLiteral literal)
            return false;

        switch (literal.TokenType)
        {
            case TokenTypes.String:
                var text = literal.StringValue;
                builder.Append('s');
                builder.Append(text.Length.ToString(CultureInfo.InvariantCulture));
                builder.Append(':');
                builder.Append(text);
                return true;

            case TokenTypes.Number:
                builder.Append('d');
                builder.Append(BitConverter.DoubleToInt64Bits(literal.NumericValue).ToString("X16", CultureInfo.InvariantCulture));
                return true;

            case TokenTypes.True:
                builder.Append('t');
                return true;

            case TokenTypes.False:
                builder.Append('f');
                return true;

            case TokenTypes.Null:
                builder.Append('n');
                return true;

            default:
                return false;
        }
    }
}
