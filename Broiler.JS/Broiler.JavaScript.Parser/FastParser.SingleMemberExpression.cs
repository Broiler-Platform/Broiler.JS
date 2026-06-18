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
            // next must be .target... (line terminators may separate the tokens:
            // `new\n.\ntarget`).
            stream.Consume();
            stream.SkipNewLines();
            if (stream.Current.Type != TokenTypes.Dot)
                throw stream.Unexpected();

            stream.Consume();

            if (!stream.CheckAndConsume(TokenTypes.Identifier, out var id))
                throw stream.Unexpected();

            if (id.CookedText != null || !id.Span.Equals("target"))
                throw stream.Unexpected();

            node = new AstMeta(new AstIdentifier(current.AsString()), new AstIdentifier(id));
        }
        else if (current.Keyword == FastKeywords.@import && stream.Next.Type == TokenTypes.Dot)
        {
            // `import.meta` (a meta property, Module-only) or a phased dynamic import
            // `import.defer( … )` / `import.source( … )` (an ImportCall with a phase). The compiler
            // currently treats the phased forms like a plain `import(…)`.
            var importToken = current;
            stream.Consume(); // `import`
            stream.Consume(); // `.`

            if (!stream.CheckAndConsume(TokenTypes.Identifier, out var id) || id.CookedText != null)
                throw stream.Unexpected();

            if (id.Span.Equals("meta"))
            {
                node = new AstMeta(new AstIdentifier(importToken.AsString()), new AstIdentifier(id));
            }
            else if ((id.Span.Equals("defer") || id.Span.Equals("source"))
                     && stream.Current.Type == TokenTypes.BracketStart)
            {
                node = ParseImportCall(importToken);
            }
            else
            {
                throw stream.Unexpected();
            }
        }
        else if (current.Keyword == FastKeywords.@import && stream.Next.Type == TokenTypes.BracketStart)
        {
            // Dynamic import — `import( AssignmentExpression [, options] ,opt )` — a CallExpression
            // that evaluates to a Promise.
            var importToken = current;
            stream.Consume(); // `import`
            node = ParseImportCall(importToken);
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
                    var indexOptional = token.Type == TokenTypes.OptionalIndex;
                    inOptional |= indexOptional;
                    stream.Consume();
                    if (!ExpressionSequence(out var index, TokenTypes.SquareBracketEnd))
                        throw stream.Unexpected();
                    node = node.Member(index, true, indexOptional, inOptional);
                    continue;

                case TokenTypes.BracketStart:
                case TokenTypes.OptionalCall:
                    var isOptionalCall = token.Type == TokenTypes.OptionalCall;
                    inOptional |= isOptionalCall;
                    stream.Consume();
                    if (!ArrayExpression(out var arguments))
                        throw stream.Unexpected();
                    if (asNew)
                    {
                        node = new AstNewExpression(token, node, arguments);
                        asNew = false;
                    }
                    else
                        // Mark the call optional only for an EXPLICIT `?.()`; a plain
                        // `()` that merely sits in an optional chain (`a?.b()`) must NOT
                        // short-circuit on the method being nullish — the receiver
                        // short-circuit is already carried by the callee member's
                        // coalesce flag, and `a?.b()` with a non-nullish `a` and a
                        // non-callable `b` must throw a TypeError. It does, however,
                        // still propagate an earlier short-circuit (inOptional).
                        node = new AstCallExpression(node, arguments, isOptionalCall, inOptional);
                    continue;

                case TokenTypes.QuestionDot:
                case TokenTypes.Dot:
                    var dotOptional = token.Type == TokenTypes.QuestionDot;
                    inOptional |= dotOptional;
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
                            dotOptional,
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
                            // `a ?. (b)` — an explicit optional call.
                            node = new AstCallExpression(node, optionalArguments, true, inOptional);
                            continue;
                        }

                        if (next.Type == TokenTypes.SquareBracketStart)
                        {
                            stream.Consume();
                            if (!ExpressionSequence(out var optionalIndex, TokenTypes.SquareBracketEnd))
                                throw stream.Unexpected();
                            node = node.Member(optionalIndex, true, dotOptional, inOptional);
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
                                dotOptional,
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

        // An optional chain short-circuits via an internal sentinel that each link
        // propagates; wrap the chain root so the sentinel is converted back to
        // `undefined` exactly once, at the boundary of the chain. A parenthesized
        // sub-expression parses in its own SingleMemberExpression invocation, so
        // `(a?.b).c` wraps `a?.b` and resets the chain — matching the spec.
        else if (inOptional)
            node = new AstOptionalChain(node);

        return true;

    }

    // Parses the argument list of a dynamic ImportCall — `( AssignmentExpression [, options] ,opt )`
    // — assuming `import` (and any phase keyword) is already consumed and the current token is `(`.
    // Exactly one specifier is required; a second argument is the optional `options` object.
    private AstImportCall ParseImportCall(FastToken importToken)
    {
        stream.Consume(); // `(`

        // import( AssignmentExpression[+In] , AssignmentExpression[+In] ,opt ): the argument
        // list is an [+In] context, so `in` is a valid binary operator even inside a for-head
        // (where it is otherwise suppressed to disambiguate for-in), e.g.
        // `for (import(spec, 'k' in obj); …; …)`.
        var savedIn = considerInOfAsOperators;
        considerInOfAsOperators = true;
        var parsedArgs = ArrayExpression(out var importArgs);
        considerInOfAsOperators = savedIn;
        if (!parsedArgs)
            throw stream.Unexpected();

        var en = importArgs.GetFastEnumerator();
        AstExpression source = null, options = null;
        if (en.MoveNext(out var first)) source = first;
        if (en.MoveNext(out var second)) options = second;

        if (source == null || en.MoveNext(out _))
            throw stream.Unexpected(); // require exactly one specifier (plus optional options)

        return new AstImportCall(importToken, source, options, (options ?? source).End);
    }

}
