using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Parser;


partial class FastParser
{
    /// <summary>
    /// Single expression is,
    ///     Identifier
    ///     ( Expression )
    ///     Literal
    ///     Array
    ///     Object
    ///     Function
    ///     Class
    ///     `fdfsd${singleExpression}dfsd`
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    bool SingleExpression(out AstExpression node, bool afterDot = false, bool asyncFunction = false)
    {
        var begin = stream.Current;
        var token = begin;

        if (token.Type == TokenTypes.At)
        {
            // A DecoratorList preceding a class expression (`x = @dec class {}`).
            // Decorators are parsed and discarded; the class expression follows.
            Decorators();
            if (stream.Current.Keyword != FastKeywords.@class)
                throw stream.Unexpected();
            return ClassExpression(out node);
        }

        if (afterDot)
        {
            // after .
            // even keywords are accepted as a member name
            switch (token.Type)
            {
                case TokenTypes.Identifier:
                case TokenTypes.In:
                case TokenTypes.InstanceOf:
                case TokenTypes.Null:
                case TokenTypes.True:
                case TokenTypes.False:
                    node = new AstIdentifier(token.AsString());
                    stream.Consume();
                    return true;
            }
        }

        if (Literal(out node))
            return true;

        switch (token.Keyword)
        {
            case FastKeywords.async:
                // Whether `async` introduces an async function/arrow is decided by
                // SinglePrefixPostfixExpression (LooksLikeAsyncFunctionOrArrow), which
                // consumes it in that case. Reaching here means it is a plain
                // identifier (`async`, `async.x`, `async = 1`, `async(args)`, or the
                // parameter of a non-async arrow); fall through to the identifier
                // parser below.
                break;

            case FastKeywords.function:
                // When a leading `async` was already consumed by the caller
                // (SinglePrefixPostfixExpression), it threads asyncFunction here
                // so the function body is parsed with inAsyncFunctionBody = true.
                return FunctionExpression(out node, asyncFunction);

            case FastKeywords.@class:
                return ClassExpression(out node);

            // NOTE: `yield` is intentionally NOT handled here. A YieldExpression is at
            // the AssignmentExpression level, so it is intercepted in Expression();
            // handling it at the primary level would wrongly accept it as an operand of
            // a higher-precedence operator (e.g. `3 + yield 4`).

            case FastKeywords.await:
                if (ShouldParseAwaitAsExpression())
                {
                    // An async function's FormalParameters must not contain an
                    // AwaitExpression (early Syntax error).
                    if (inFormalParameters && inAsyncFunctionBody)
                        throw stream.Unexpected();
                    return AwaitExpression(out node);
                }
                break;

            case FastKeywords.super:
                stream.Consume();
                node = new AstSuper(token);
                return true;
        }

        if (Identitifer(out var id))
        {
            if (id.Start.IsEscapedReservedWord)
                throw new FastParseException(id.Start, "Keyword must not contain escaped characters");

            // Inside a generator body `yield` is a reserved word and cannot be an
            // IdentifierReference (a bare `yield` is a YieldExpression, intercepted in
            // Expression()). Reaching here means `yield` appeared in an operand position
            // such as the right side of a binary operator (`3 + yield 4`), which is a
            // SyntaxError.
            if (inGeneratorBody && id.Start.Keyword == FastKeywords.yield)
                throw stream.Unexpected();

            // `enum` and `extends` are always-reserved words that nonetheless lex as
            // identifier-typed keyword tokens and reach this IdentifierReference path
            // (class/const/export/import are intercepted as statement/expression starters;
            // `super`, `this`, `null`, `true`, `false` are valid primaries handled above).
            // A bare `enum` / `extends` IdentifierReference (`enum = 1`) is a SyntaxError.
            if (id.Start.Keyword is FastKeywords.@enum or FastKeywords.@extends)
                throw new FastParseException(id.Start, $"'{id.Name}' is a reserved word and cannot be used here");

            // `await` is reserved throughout an async function/arrow, so it cannot be
            // an IdentifierReference there. A unicode-escaped form such as
            // `await` is classified by the scanner as a plain identifier (the
            // escape strips its keyword identity, and `await` is allowed escaped as an
            // ordinary identifier outside async code), so it bypasses the `await`
            // keyword handling above; reject it here by name. The unescaped form is
            // already an AwaitExpression / early error via that keyword path.
            if (inAsyncFunctionBody && id.Name.Equals("await"))
                throw stream.Unexpected();

            node = id;
            return true;
        }

        switch (token.Type)
        {
            // A for-head suppresses `in` as a binary operator (to detect `for (x in
            // …)`), but that `[~In]` only applies at the top level of the head: inside
            // a parenthesised, array, or object sub-expression `in` is an ordinary
            // operator again (e.g. the destructuring default in
            // `for ({ p = 'a' in obj } of …)`). Restore it while parsing the grouping.
            case TokenTypes.BracketStart:
            {
                var savedIn = considerInOfAsOperators;
                considerInOfAsOperators = true;
                var ok = BracketExpression(out node);
                considerInOfAsOperators = savedIn;
                return ok;
            }

            case TokenTypes.SquareBracketStart:
            {
                var savedIn = considerInOfAsOperators;
                considerInOfAsOperators = true;
                var ok = ArrayExpression(out node);
                considerInOfAsOperators = savedIn;
                return ok;
            }

            case TokenTypes.CurlyBracketStart:
            {
                var savedIn = considerInOfAsOperators;
                considerInOfAsOperators = true;
                var ok = ObjectLiteral(out node);
                considerInOfAsOperators = savedIn;
                return ok;
            }

            case TokenTypes.Hash:
                // A leading `#name` is a PrivateIdentifier, only valid as the left
                // operand of `in` (the ergonomic brand check `#x in obj`). Parse it
                // into an identifier in the private-name namespace; the compiler
                // resolves the brand check (VisitBinaryExpression) and rejects any
                // other position.
                stream.Consume();
                if (!Identitifer(out var privateName))
                    throw stream.Unexpected();
                node = new AstIdentifier(token, "#" + privateName.Name.Value);
                return true;

            case TokenTypes.TemplateBegin:
                node = Template();
                return true;

            case TokenTypes.TemplateEnd:
                stream.Consume();
                node = new AstTemplateExpression(token, token, new Sequence<AstExpression>(1) { new AstLiteral(token.Type, token) });
                return true;

            case TokenTypes.EOF:
            case TokenTypes.Comma:
            case TokenTypes.BracketEnd:
            case TokenTypes.SquareBracketEnd:
            case TokenTypes.CurlyBracketEnd:
            case TokenTypes.LineTerminator:
            case TokenTypes.SemiColon:
                return false;

            default:
                throw stream.Unexpected();
        }

        bool BracketExpression(out AstExpression node)
        {
            node = default;

            if (ExpressionList(out var nodes, out var start, out var end, TokenTypes.BracketEnd))
            {
                if (nodes.Count == 0)
                {
                    node = new AstEmptyExpression(PreviousToken);
                }
                else if (nodes.Count == 1)
                {
                    node = nodes.First();
                }
                else
                {
                    node = new AstSequenceExpression(start, end, nodes);
                }

                node.WasParenthesized = true;
                return true;
            }

            return false;
        }

        bool ArrayExpression(out AstExpression node)
        {
            node = default;

            if (ExpressionList(out var nodes, out var start, out var end, TokenTypes.SquareBracketEnd, true))
            {
                node = new AstArrayExpression(start, end, nodes);
                return true;
            }

            return false;
        }

        bool ExpressionList(out IFastEnumerable<AstExpression> node, out FastToken start, out FastToken end, TokenTypes endType, bool allowEmpty = false)
        {
            var begin = stream.Current;
            start = begin;
            stream.Consume();

            var nodes = new Sequence<AstExpression>();

            while (!stream.CheckAndConsume(endType))
            {
                if (stream.CheckAndConsume(TokenTypes.Comma))
                {
                    if (allowEmpty)
                    {
                        nodes.Add(null);
                        continue;
                    }

                    throw stream.Unexpected();
                }

                var spread = stream.CheckAndConsume(TokenTypes.TripleDots, out var token);

                if (!Expression(out var n))
                    throw stream.Unexpected();

                if (spread)
                    n = new AstSpreadElement(token, n.End, n);

                nodes.Add(n);

                if (stream.CheckAndConsume(TokenTypes.Comma))
                    continue;

                if (stream.CheckAndConsume(endType))
                    break;

                throw stream.Unexpected();
            }

            node = nodes;
            end = PreviousToken;

            return true;
        }

        bool ShouldParseAwaitAsExpression()
        {
            if (classStaticBlockDepth != 0 && !inAsyncFunctionBody)
                return false;

            if (inAsyncFunctionBody)
                return true;

            if (functionDepth != 0)
                return false;

            // At the top level, `await` is only an AwaitExpression when top-level await is
            // enabled (a module, or an eval that opted into it). In an ordinary script
            // `await` is a plain IdentifierReference, so `await + x` / `await(x)` must not
            // be mis-parsed as `await (+x)` / `await (x)` (which then fails the
            // top-level-await check). Module / TLA contexts keep the lookahead heuristic.
            if (!CoreScript.AllowTopLevelAwait)
                return false;

            var next = stream.Next;
            return next.Type is not (TokenTypes.SemiColon
                or TokenTypes.LineTerminator
                or TokenTypes.EOF
                or TokenTypes.Comma
                or TokenTypes.BracketEnd
                or TokenTypes.SquareBracketEnd
                or TokenTypes.CurlyBracketEnd
                // `instanceof`/`in` are binary operators that require a left operand
                // and cannot begin `await`'s UnaryExpression operand. `await
                // instanceof X` / `await in X` therefore use `await` as an
                // IdentifierReference (valid at the top level of a script, where
                // top-level await does not apply), not an AwaitExpression.
                or TokenTypes.InstanceOf
                or TokenTypes.In)
                && (next.Type <= TokenTypes.BeginAssignTokens || next.Type >= TokenTypes.EndAssignTokens);
        }

        bool AwaitExpression(out AstExpression statement)
        {
            var begin = stream.Current;
            stream.Consume();

            // `await`'s operand is a UnaryExpression, not a full Expression: `await a
            // * b` is `(await a) * b`, and the await expression participates in the
            // enclosing operator-precedence chain handled by the caller. Parsing a
            // full Expression here mis-associated higher-precedence operators (so
            // `await a * b` became `await (a * b)`) and the trailing EndOfStatement()
            // wrongly consumed the statement terminator — breaking ASI for the
            // enclosing statement (e.g. `let y = await x\n stmt`).
            if (!SinglePrefixPostfixExpression(out var target, out _, out _))
                throw stream.Unexpected();

            // `await x ** y` is a SyntaxError: an await UnaryExpression cannot be the
            // left operand of `**`. Parenthesised `(await x) ** y` is fine — there the
            // `**` is applied outside this production and never reaches here.
            if (stream.Current.Type == TokenTypes.Power)
                throw new FastParseException(begin,
                    "Unary operator used immediately before exponentiation expression; parentheses must be used to disambiguate operator precedence");

            if (functionDepth == 0)
                isAsync = true;
            statement = new AstAwaitExpression(begin, PreviousToken, target);

            return true;
        }
    }
}
