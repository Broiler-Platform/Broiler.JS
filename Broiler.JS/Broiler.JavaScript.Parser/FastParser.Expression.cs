
using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Statements;
using System.Runtime.CompilerServices;

namespace Broiler.JavaScript.Parser;


partial class FastParser
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void PreventStackoverFlow(ref FastToken id)
    {
        if (id == stream.Current)
            throw stream.Unexpected();

        id = stream.Current;
    }

    /// <summary>
    /// While parsing expression, it can never start from same
    /// position of token, any nested Expression must consume
    /// the current token.
    /// </summary>
    private FastToken lastExpressionIndex;

    // A literal/value token that can only begin a fresh PrimaryExpression. Such a
    // token directly following a complete expression (with no operator and no ASI
    // line break) is a SyntaxError rather than a token to silently consume.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsLiteralValueStart(TokenTypes type)
        => type is TokenTypes.Number
            or TokenTypes.BigInt
            or TokenTypes.Decimal
            or TokenTypes.String
            or TokenTypes.Null
            or TokenTypes.True
            or TokenTypes.False;

    // YieldExpression : yield | yield [no LineTerminator here] * AssignmentExpression
    //                 | yield AssignmentExpression
    bool YieldExpression(out AstExpression statement)
    {
        var begin = stream.Current;
        stream.Consume();

        // `yield *` requires the `*` on the same line (no LineTerminator here); a
        // newline turns `yield\n* x` into a yield with no argument followed by `* x`.
        var star = stream.CheckAndConsumeNoLineTerminator(TokenTypes.Multiply);

        if (!star)
        {
            switch (stream.Current.Type)
            {
                case TokenTypes.Comma:
                case TokenTypes.SemiColon:
                case TokenTypes.LineTerminator:
                case TokenTypes.EOF:
                case TokenTypes.CurlyBracketEnd:
                case TokenTypes.BracketEnd:
                case TokenTypes.SquareBracketEnd:
                case TokenTypes.Colon:
                // A bare `yield` ending a template substitution (`` `${ yield }` ``)
                // is followed by the template tail, which the scanner emits as a
                // TemplatePart/TemplateEnd (the closing `}` is re-scanned as template
                // text). Treat those as operand terminators, like the other closers.
                case TokenTypes.TemplatePart:
                case TokenTypes.TemplateEnd:
                    statement = new AstYieldExpression(begin, PreviousToken, null);
                    return true;
            }
        }

        if (Expression(out var target))
        {
            statement = new AstYieldExpression(begin, PreviousToken, target, star);
            EndOfStatement();
            return true;
        }

        throw stream.Unexpected();
    }

    bool Expression(out AstExpression node)
    {
        SkipNewLines();
        PreventStackoverFlow(ref lastExpressionIndex);

        var token = stream.Current;

        if (token.Type == TokenTypes.EOF)
            throw stream.Unexpected();

        // A YieldExpression is at the AssignmentExpression level. Intercept it here (the
        // assignment-expression entry) rather than at the primary level, so it cannot
        // appear as an operand of a higher-precedence operator (`3 + yield 4` is a
        // SyntaxError) while still being valid wherever an AssignmentExpression is —
        // assignment RHS, arguments, array elements, parentheses, ternary branches,
        // comma-sequences and the operand of another yield (all routed through here).
        if (inGeneratorBody && token.Keyword == FastKeywords.yield && !token.IsEscapedReservedWord)
        {
            // A generator's FormalParameters must not contain a YieldExpression.
            if (inFormalParameters)
                throw stream.Unexpected();
            return YieldExpression(out node);
        }

        if (!SinglePrefixPostfixExpression(out node, out var isAsync, out var isGenerator))
        {
            node = null;
            return false;
        }

        // Per spec: no LineTerminator allowed between ArrowParameters and =>
        // Use CheckAndConsumeNoLineTerminator to reject `(a) \n => {}`
        if (stream.CheckAndConsumeNoLineTerminator(TokenTypes.Lambda))
        {
            // ArrowParameters must not Contain an AwaitExpression / YieldExpression
            // (early Syntax error). The parameter list was parsed under the cover grammar
            // as an ordinary expression, so — unlike a normal FormalParameters list — the
            // await/yield parse sites did not reject it; check the refined parameters here.
            // await/yield expressions only exist inside an async/generator context, so the
            // scan is limited to those — either the enclosing context (still in effect before
            // the arrow's own context is established below) or the arrow's own async-ness:
            // an async arrow's own parameters parse `await x` as an AwaitExpression even at
            // the top level (`async (a = await x) => {}`), which is an early SyntaxError.
            if ((inAsyncFunctionBody || inGeneratorBody || isAsync || isGenerator) && AwaitYieldParameterDetector.Contains(node))
                throw stream.Unexpected();

            var scope = variableScope.Push(token, FastNodeType.FunctionExpression);
            scope.IsArrow = true;
            try
            {
                // create parameters now...
                var parameters = VariableDeclarator.From(node);
                functionDepth++;
                var previousInGeneratorBody = inGeneratorBody;
                var previousInAsyncFunctionBody = inAsyncFunctionBody;
                inGeneratorBody = isGenerator;
                inAsyncFunctionBody = isAsync;
                try
                {
                    if (stream.CheckAndConsume(TokenTypes.CurlyBracketStart))
                    {
                        if (!Block(out var block))
                            throw stream.Unexpected();

                        node = new AstFunctionExpression(token, PreviousToken, true, isAsync, isGenerator, null, VariableDeclarator.From(node), block);
                        return true;
                    }

                    if (!Expression(out var r))
                        throw stream.Unexpected();

                    node = new AstFunctionExpression(token, PreviousToken, true, isAsync, isGenerator, null, parameters, new AstReturnStatement(r.Start, r.End, r));
                    return true;
                }
                finally
                {
                    inGeneratorBody = previousInGeneratorBody;
                    inAsyncFunctionBody = previousInAsyncFunctionBody;
                    functionDepth--;
                }
            }
            finally
            {
                scope.Dispose();
            }
        }

        if (node.End.Type == TokenTypes.SemiColon)
            return true;

        if (stream.Previous.Type == TokenTypes.SemiColon)
            return true;

        var m = stream.SkipNewLines();
        var current = stream.Current;
        var currentType = current.Type;

        switch (currentType)
        {
            case TokenTypes.Colon:
            case TokenTypes.CurlyBracketEnd:
            case TokenTypes.BracketEnd:
            case TokenTypes.TemplatePart:
            case TokenTypes.TemplateEnd:
                return true;
        }

        if (!currentType.IsOperator() && !currentType.IsAssignmentOperator())
        {
            if (!considerInOfAsOperators && current.ContextualKeyword == FastKeywords.of)
                return true;

            if (m.LinesSkipped)
            {
                m.Undo();
                return true;
            }

            // A `@` can never follow a complete expression: it is only ever a
            // DecoratorList prefix (before a class declaration/expression or a class
            // element), which is handled at the start of SingleExpression/Statement.
            // A same-line `@` here (e.g. `5@`, `({a: 5@})`, `[f1@]`) is therefore a
            // SyntaxError rather than a token to silently drop. A newline-separated
            // `@dec class {}` is preserved by the LinesSkipped check above (ASI).
            if (currentType == TokenTypes.At)
                throw stream.Unexpected();

            // Two value-producing tokens cannot be adjacent without an operator (or,
            // via ASI, a line break — already handled above): `foo 1`, `1 "x"`, and
            // `x = await 1` (where `await` is a bare IdentifierReference) are all
            // SyntaxErrors. Without this, NextExpression's `CheckAndConsume(previousType)`
            // would consume the stray literal as if it were an operator and silently
            // drop it. A token of the same kind as this expression's first token is
            // likewise a stray adjacent primary.
            if (currentType == token.Type || IsLiteralValueStart(currentType))
                throw stream.Unexpected();
        }

        if (NextExpression(ref node, ref currentType, out var next, out var nextToken))
        {
            if (next == null)
                return true;

            node = Combine(node, currentType, next);
            return true;
        }

        return true;
    }
}
