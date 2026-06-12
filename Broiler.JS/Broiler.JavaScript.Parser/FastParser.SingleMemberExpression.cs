using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.ExpressionCompiler.Core;

namespace Broiler.JavaScript.Parser;


partial class FastParser
{

    /// <summary>
    /// SingleExpression
    /// SingleExpression[]
    /// SingleExpression.SingleExpression[]
    /// SingleExpression(.... )
    /// SingleExpression.SingleExpression(....) 
    /// SingleExpression.SingleExpression[].SingleExpression
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    bool SingleMemberExpression(out AstExpression node, bool asNew = false, bool asyncFunction = false)
    {
        node = null;
        var current = stream.Current;

        if (current.Keyword == FastKeywords.@new)
        {
            // next must be .target...
            if (stream.Next.Type != TokenTypes.Dot)
                throw stream.Unexpected();

            stream.Consume();
            stream.Consume();

            if (!stream.CheckAndConsume(TokenTypes.Identifier, out var id))
                throw stream.Unexpected();

            if (id.CookedText != null || !id.Span.Equals("target"))
                throw stream.Unexpected();

            node = new AstMeta(new AstIdentifier(current.AsString()), new AstIdentifier(id));
        }
        else if (current.Keyword == FastKeywords.@import && stream.Next.Type == TokenTypes.Dot)
        {
            // import.meta — only valid when the syntactic goal symbol is Module.
            // Parse it as a meta property so the compiler can reject it with a
            // SyntaxError in Script / FunctionBody / FormalParameters contexts.
            stream.Consume();
            stream.Consume();

            if (!stream.CheckAndConsume(TokenTypes.Identifier, out var id))
                throw stream.Unexpected();

            if (id.CookedText != null || !id.Span.Equals("meta"))
                throw stream.Unexpected();

            node = new AstMeta(new AstIdentifier(current.AsString()), new AstIdentifier(id));
        }
        else if (!SingleExpression(out node, asyncFunction: asyncFunction))
        {
            return false;
        }

        FastToken begin;
        FastToken token;

        // Once an optional link (`?.`, `?.[`, `?.(`) appears, the rest of this chain
        // short-circuits with it: `a?.b.c`, `a?.b()`, `a?.b[c]` must all yield undefined
        // when `a` is nullish (and never read/call through the undefined). The chain is
        // delimited by this SingleMemberExpression invocation — a parenthesized
        // sub-expression parses in its own invocation, so `(a?.b).c` correctly resets.
        var inOptional = false;

        while (true)
        {
            var m = stream.SkipNewLines();

            begin = stream.Current;
            token = begin;

            switch (token.Type)
            {
                case TokenTypes.TemplateBegin:
                    var template = Template();
                    node = new AstTaggedTemplateExpression(node, template.Parts);
                    continue;

                case TokenTypes.TemplateEnd:
                    if (token.Span.Length > 0 && token.Span.Source[token.Span.Offset] == '}')
                    {
                        m.Undo();
                        break;
                    }

                    stream.Consume();
                    node = new AstTaggedTemplateExpression(node, new Sequence<AstExpression>(1)
                    {
                        new AstLiteral(token.Type, token)
                    });
                    continue;

                case TokenTypes.OptionalIndex:
                case TokenTypes.SquareBracketStart:
                    inOptional |= token.Type == TokenTypes.OptionalIndex;
                    stream.Consume();
                    if (!ExpressionSequence(out var index, TokenTypes.SquareBracketEnd))
                        throw stream.Unexpected();
                    node = node.Member(index, true, inOptional);
                    continue;

                case TokenTypes.BracketStart:
                case TokenTypes.OptionalCall:
                    inOptional |= token.Type == TokenTypes.OptionalCall;
                    stream.Consume();
                    if (!ArrayExpression(out var arguments))
                        throw stream.Unexpected();
                    if (asNew)
                    {
                        node = new AstNewExpression(token, node, arguments);
                        asNew = false;
                    }
                    else
                        node = new AstCallExpression(node, arguments, inOptional);
                    continue;

                case TokenTypes.QuestionDot:
                case TokenTypes.Dot:
                    inOptional |= token.Type == TokenTypes.QuestionDot;
                    stream.Consume();
                    stream.SkipNewLines();

                    if ((token.Type == TokenTypes.Dot || token.Type == TokenTypes.QuestionDot)
                        && stream.CheckAndConsume(TokenTypes.Hash, out var hashToken))
                    {
                        if (!Identitifer(out var privateIdentifier))
                            throw stream.Unexpected();

                        node = node.Member(
                            new AstIdentifier(hashToken, $"#{privateIdentifier.Name.Value}"),
                            false,
                            inOptional);
                        continue;
                    }

                    var next = stream.Current;

                    // The scanner only fuses the *adjacent* forms `?.(` and `?.[` into
                    // OptionalCall / OptionalIndex. When whitespace or a newline separates
                    // `?.` from its brackets (`a ?. (b)`, `a ?.\n[b]`), it emits a bare
                    // QuestionDot, so handle the optional call / computed access here.
                    if (token.Type == TokenTypes.QuestionDot)
                    {
                        if (next.Type == TokenTypes.BracketStart)
                        {
                            stream.Consume();
                            if (!ArrayExpression(out var optionalArguments))
                                throw stream.Unexpected();
                            node = new AstCallExpression(node, optionalArguments, inOptional);
                            continue;
                        }

                        if (next.Type == TokenTypes.SquareBracketStart)
                        {
                            stream.Consume();
                            if (!ExpressionSequence(out var optionalIndex, TokenTypes.SquareBracketEnd))
                                throw stream.Unexpected();
                            node = node.Member(optionalIndex, true, inOptional);
                            continue;
                        }
                    }

                    switch (next.Type)
                    {
                        case TokenTypes.Identifier:
                        case TokenTypes.In:
                        case TokenTypes.InstanceOf:
                        case TokenTypes.Null:
                        case TokenTypes.True:
                        case TokenTypes.False:
                            stream.Consume();
                            node = node.Member(
                                new AstIdentifier(next.AsString()),
                                false,
                                inOptional);
                            break;
                        default:
                            throw stream.Unexpected();
                    }
                    continue;

                default:
                    if (token.Type == TokenTypes.Number
                        && token.Span.Length > 0
                        && token.Span.Source[token.Span.Offset] == '.'
                        && node?.End.End.Line == token.Start.Line)
                    {
                        throw stream.Unexpected();
                    }

                    m.Undo();
                    break;
            }

            break;
        }

        if (asNew)
            node = new AstNewExpression(token, node, Sequence<AstExpression>.Empty);

        return true;

    }

}
