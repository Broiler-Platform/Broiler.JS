using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;

namespace Broiler.JavaScript.Parser;


partial class FastParser
{
    /// <summary>
    /// delete SingleComputedExpression
    /// void SingleComputedExpression
    /// typeof SingleComputedExpression
    /// +SingleComputedExpression
    /// -SingleComputedExpression
    /// ~SingleComputedExpression
    /// !SingleComputedExpression
    /// ++SingleComputedExpression
    /// --SingleComputedExpression
    /// SingleComputedExpression++
    /// SingleComputedExpression--
    /// </summary>
    /// <param name="node"></param>
    /// <param name="hasAsync"></param>
    /// <param name="hasGenerator"></param>
    /// <returns></returns>
    bool SinglePrefixPostfixExpression(out AstExpression node, out bool hasAsync, out bool hasGenerator, UnaryOperator previous = UnaryOperator.None, FastToken previousToken = null)
    {
        var begin = BeginUndo();
        if (HasUnaryOperator(out var prefix, out var token))
        {
            if (!SinglePrefixPostfixExpression(out node, out hasAsync, out hasGenerator, prefix, token))
                return begin.Reset();

            if (previous != UnaryOperator.None && previous != UnaryOperator.@new)
                node = new AstUnaryExpression(previousToken, node, previous);

            return true;
        }

        hasAsync = false;
        hasGenerator = false;

        // `async` is the async function/arrow keyword only when it actually
        // introduces one (`async function`, `async (...) =>`, `async id =>`).
        // Otherwise it is a plain identifier — `async`, `async.x`, `async = 1`,
        // `async(args)` (a call), or the parameter of a non-async arrow
        // (`async => x`) — and must be left for the member/identifier parser.
        if (stream.Current.Keyword == FastKeywords.async && LooksLikeAsyncFunctionOrArrow())
        {
            pendingAsyncStart = stream.Current;
            stream.Consume();
            hasAsync = true;
        }

        // A leading `*` never begins an expression in ECMAScript. Generator syntax
        // only appears after `function` or in a method definition, both handled by
        // other productions; there is no generator-arrow. Previously a stray `*` was
        // silently consumed (and ignored unless an arrow followed), so `x = * 1`,
        // `({a: * 1})` and `({*a: 1})`'s value were wrongly accepted. Fail here so the
        // `*` is reported as unexpected by the enclosing production.
        if (stream.Current.Type == TokenTypes.Multiply)
        {
            node = null;
            return begin.Reset();
        }

        var previousInAsyncFunctionBody = inAsyncFunctionBody;
        if (hasAsync)
            inAsyncFunctionBody = true;

        try
        {
            if (!SingleMemberExpression(out node, previous == UnaryOperator.@new, hasAsync))
                return begin.Reset();
        }
        finally
        {
            inAsyncFunctionBody = previousInAsyncFunctionBody;
        }

        if (previous != UnaryOperator.None)
        {
            if (previous != UnaryOperator.@new)
                node = new AstUnaryExpression(previousToken, node, previous);

            return true;
        }

        while (true)
        {
            if (HasUnaryOperator(out var postfix, out var postFixToken, false))
                node = new AstUnaryExpression(postFixToken, node, postfix, false);
            else
                break;
        }

        if (node.Type == FastNodeType.FunctionExpression)
        {
            var fx = node as AstFunctionExpression;

            if (hasAsync)
                fx.Async = hasAsync;

            if (hasGenerator)
                fx.Generator = hasGenerator;
        }

        return true;

        bool HasUnaryOperator(out UnaryOperator unaryOperator, out FastToken token, bool prefix = true)
        {
            var m = stream.SkipNewLines();
            unaryOperator = UnaryOperator.None;
            token = stream.Current;

            switch (token.Type)
            {
                case TokenTypes.Plus:
                    if (prefix)
                    {
                        stream.Consume();
                        unaryOperator = UnaryOperator.Plus;

                        return true;
                    }
                    return false;

                case TokenTypes.Minus:
                    if (prefix)
                    {
                        stream.Consume();
                        unaryOperator = UnaryOperator.Minus;
                        return true;
                    }
                    m.Undo();
                    return false;

                case TokenTypes.Increment:
                    if (m.LinesSkipped)
                    {
                        m.Undo();
                        return false;
                    }
                    stream.Consume();
                    unaryOperator = UnaryOperator.Increment;
                    return true;

                case TokenTypes.Decrement:
                    if (m.LinesSkipped)
                    {
                        m.Undo();
                        return false;
                    }
                    stream.Consume();
                    unaryOperator = UnaryOperator.Decrement;
                    return true;

                case TokenTypes.Negate:
                    if (prefix)
                    {
                        stream.Consume();
                        unaryOperator = UnaryOperator.Negate;
                        return true;
                    }
                    break;

                case TokenTypes.BitwiseNot:
                    if (prefix)
                    {
                        stream.Consume();
                        unaryOperator = UnaryOperator.BitwiseNot;
                        return true;
                    }
                    break;
            }

            if (!prefix)
            {
                m.Undo();
                return false;
            }

            switch (token.Keyword)
            {
                case FastKeywords.@new:
                    if (stream.Next.Type == TokenTypes.Dot)
                    {
                        m.Undo();
                        return false;
                    }
                    stream.Consume();
                    unaryOperator = UnaryOperator.@new;
                    return true;

                case FastKeywords.@typeof:
                    stream.Consume();
                    unaryOperator = UnaryOperator.@typeof;
                    return true;

                case FastKeywords.delete:
                    stream.Consume();
                    unaryOperator = UnaryOperator.delete;
                    return true;

                case FastKeywords.@void:
                    stream.Consume();
                    unaryOperator = UnaryOperator.@void;
                    return true;

                default:
                    m.Undo();
                    return false;
            }
        }
    }

    /// <summary>
    /// Decides whether a leading <c>async</c> token introduces an async function or
    /// arrow (so it should be consumed as the async keyword) rather than being a
    /// plain identifier. True for <c>async function …</c>, <c>async id =&gt; …</c>,
    /// and <c>async ( … ) =&gt; …</c>; false for <c>async</c>, <c>async.x</c>,
    /// <c>async = 1</c>, a call <c>async(args)</c>, and <c>async =&gt; x</c> (where
    /// <c>async</c> is the arrow parameter). The lookahead is non-destructive: it
    /// consumes tokens to scan a balanced parameter list and then resets the stream.
    /// </summary>
    bool LooksLikeAsyncFunctionOrArrow()
    {
        var start = stream.Current;
        stream.Consume();

        // `async [no LineTerminator here] function/BindingIdentifier/(` — a line
        // break after `async` disqualifies it as an async function/arrow head, so
        // it is a plain IdentifierReference (`async\nfunction f(){}` is `async;
        // function f(){}`; `async\nx => {}` is `async; x => {}`).
        if (stream.SkipNewLines().LinesSkipped)
        {
            stream.Reset(start);
            return false;
        }

        var t = stream.Current;
        bool result;

        if (t.Keyword == FastKeywords.function || t.Type == TokenTypes.Identifier)
        {
            // `async function …` or `async BindingIdentifier =>` (a bare
            // `async id` with no `=>` is itself a syntax error, so committing here
            // does not mis-handle any valid identifier use).
            result = true;
        }
        else if (t.Type == TokenTypes.BracketStart)
        {
            // `async ( … )` — an async arrow only when `=>` follows the matching
            // `)`; otherwise it is a call `async(args)`.
            var depth = 0;
            result = false;
            while (true)
            {
                var ct = stream.Current;
                if (ct.Type == TokenTypes.EOF)
                    break;

                if (ct.Type == TokenTypes.BracketStart)
                    depth++;
                else if (ct.Type == TokenTypes.BracketEnd)
                {
                    depth--;
                    if (depth == 0)
                    {
                        stream.Consume();
                        stream.SkipNewLines();
                        result = stream.Current.Type == TokenTypes.Lambda;
                        break;
                    }
                }

                stream.Consume();
            }
        }
        else
        {
            result = false;
        }

        stream.Reset(start);
        return result;
    }
}
