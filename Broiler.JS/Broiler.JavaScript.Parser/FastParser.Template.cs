using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.ExpressionCompiler.Core;

namespace Broiler.JavaScript.Parser;

partial class FastParser
{
    AstTemplateExpression Template()
    {
        var begin = stream.Current;
        stream.Consume();
        var nodes = new Sequence<AstExpression> { new AstLiteral(TokenTypes.TemplatePart, begin) };

        while (!stream.CheckAndConsume(TokenTypes.EOF))
        {
            if (stream.CheckAndConsume(TokenTypes.TemplateEnd, out var end))
            {
                nodes.Add(new AstLiteral(TokenTypes.TemplatePart, end));
                break;
            }

            if (stream.CheckAndConsume(TokenTypes.TemplatePart, out var token))
            {
                nodes.Add(new AstLiteral(TokenTypes.TemplatePart, token));
                continue;
            }

            if (Substitution(out var exp))
            {
                nodes.Add(exp);
                continue;
            }

            throw stream.Unexpected();
        }

        return new AstTemplateExpression(begin, PreviousToken, nodes);

        // TemplateSubstitution : ${ Expression[+In] }. That is a full Expression, so a
        // comma sequence is legal and evaluates to its last element — the shape a
        // minifier produces when it folds statements into an interpolation
        // (`` `swiper-wrapper-${n=16,void 0===n&&(n=16),rnd(n)}` ``). Parsing only an
        // AssignmentExpression left the `,` unconsumed and rejected the script.
        // Being an [+In] context also means `in` is an operator here even when the
        // substitution sits inside a `for` head.
        bool Substitution(out AstExpression node)
        {
            var start = stream.Current;
            var savedIn = considerInOfAsOperators;
            considerInOfAsOperators = true;

            try
            {
                if (!Expression(out node))
                    return false;

                if (stream.Current.Type != TokenTypes.Comma)
                    return true;

                var parts = new Sequence<AstExpression> { node };

                while (stream.CheckAndConsume(TokenTypes.Comma))
                {
                    if (!Expression(out var part))
                        throw stream.Unexpected();

                    parts.Add(part);
                }

                node = new AstSequenceExpression(start, PreviousToken, parts);
                return true;
            }
            finally
            {
                considerInOfAsOperators = savedIn;
            }
        }
    }
}
