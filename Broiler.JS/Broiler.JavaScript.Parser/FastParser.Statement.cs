using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Statements;
using System.Runtime.CompilerServices;

namespace Broiler.JavaScript.Parser;


partial class FastParser
{
    FastToken lastStatementPosition;
    bool Statement(out AstStatement node)
    {
        SkipNewLines();
        PreventStackoverFlow(ref lastStatementPosition);

        var begin = BeginUndo();
        var token = begin.Token;

        switch (token.Type)
        {
            case TokenTypes.CurlyBracketStart:
                stream.Consume();
                if (Block(out var block))
                {
                    node = block;
                    return true;
                }
                break;

            case TokenTypes.SemiColon:
                stream.Consume();
                node = new AstExpressionStatement(new AstEmptyExpression(token));
                return true;
        }

        if (SingleStatement(begin, out node))
        {
            stream.CheckAndConsumeAny(TokenTypes.SemiColon, TokenTypes.LineTerminator);
            return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    bool SingleStatement(in StreamLocation begin, out AstStatement node)
    {
        var token = begin.Token;
        if (token.IsKeyword)
        {
            switch (token.Keyword)
            {
                case FastKeywords.var:
                    return VariableDeclaration(out node);

                case FastKeywords.let:
                    return VariableDeclaration(out node, FastVariableKind.Let);

                case FastKeywords.@const:
                    return VariableDeclaration(out node, FastVariableKind.Const);

                case FastKeywords.@if:
                    return IfStatement(out node);

                case FastKeywords.@while:
                    return WhileStatement(out node);

                case FastKeywords.@do:
                    return DoWhileStatement(out node);

                case FastKeywords.@for:
                    return ForStatement(out node);

                case FastKeywords.@continue:
                    return Continue(out node);

                case FastKeywords.@break:
                    return Break(out node);

                case FastKeywords.@return:
                    return Return(out node);

                case FastKeywords.@using:
                    return Using(out node);

                case FastKeywords.await:
                    if (Using(out node, true))
                        return true;

                    break;

                case FastKeywords.with:
                    return WithStatement(out node);

                case FastKeywords.@else:
                    throw stream.Unexpected();

                case FastKeywords.@switch:
                    return Switch(out node);

                case FastKeywords.@throw:
                    return Throw(out node);

                case FastKeywords.@try:
                    return Try(out node);

                case FastKeywords.debugger:
                    return Debugger(out node);

                case FastKeywords.@class:
                    return Class(out node);

                case FastKeywords.export:
                    return Export(token, out node);

                case FastKeywords.import:
                    return Import(token, out node);

                case FastKeywords.async:
                    stream.Consume();
                    if (stream.Current.Keyword != FastKeywords.function)
                        throw stream.Unexpected();
                    return Function(out node, true);

                case FastKeywords.function:
                    return Function(out node);
            }
        }

        // goto....
        if (LabeledLoop(out node))
            return true;

        if (ExpressionSequence(out var expression, TokenTypes.SemiColon))
        {
            node = new AstExpressionStatement(token, PreviousToken, expression);
            return true;
        }

        return begin.Reset();

        bool LabeledLoop(out AstStatement statement)
        {
            if (stream.CheckAndConsume(TokenTypes.Identifier, TokenTypes.Colon, out var id, out var _))
            {
                SkipNewLines();

                // has to be do/while/for...
                var current = stream.Current;

                // Lexical declarations, class declarations, and generator
                // declarations are forbidden as the body of a labeled statement.
                if (current.IsKeyword)
                {
                    switch (current.Keyword)
                    {
                        case FastKeywords.let:
                        case FastKeywords.@const:
                        case FastKeywords.@class:
                            throw new FastParseException(current, "Lexical declaration cannot appear in a single-statement context");
                    }
                }

                switch (current.Keyword)
                {
                    case FastKeywords.@do:
                        if (!DoWhileStatement(out statement))
                            throw stream.Unexpected();
                        break;

                    case FastKeywords.@for:
                        if (!ForStatement(out statement))
                            throw stream.Unexpected();
                        break;

                    case FastKeywords.@while:
                        if (!WhileStatement(out statement))
                            throw stream.Unexpected();
                        break;

                    default:
                        if (Statement(out statement))
                        {
                            // Reject generator declarations: label: function* g() {}
                            if (statement is AstExpressionStatement { Expression: AstFunctionExpression { IsStatement: true, Generator: true } gen })
                                throw new FastParseException(gen.Start, "Generator declarations cannot appear in a single-statement context");

                            statement = new AstLabeledStatement(id, statement);
                            return true;
                        }

                        break;
                }

                statement = new AstLabeledStatement(id, statement);
                return true;
            }

            statement = null;
            return false;
        }

        bool Debugger(out AstStatement statement)
        {
            var begin = stream.Current;
            stream.Consume();
            statement = new AstDebuggerStatement(begin);
            EndOfStatement();

            return true;
        }

        bool Try(out AstStatement statement)
        {
            var begin = stream.Current;
            stream.Consume();

            if (!Statement(out var body))
                throw stream.Unexpected();

            // we may not have catch...
            if (stream.CheckAndConsume(FastKeywords.@catch))
            {
                AstExpression catchParam = null;
                FastScopeItem catchScope = null;

                if (stream.CheckAndConsume(TokenTypes.BracketStart))
                {
                    if (Identitifer(out var id))
                    {
                        catchParam = id;
                    }
                    else if (stream.Current.Type == TokenTypes.SquareBracketStart || stream.Current.Type == TokenTypes.CurlyBracketStart)
                    {
                        // Push a scope for destructured catch parameters so that
                        // bound names do not leak into the enclosing scope's
                        // HoistingScope.  This prevents Annex B hoisting of a
                        // block-scoped function declaration whose name collides
                        // with a destructured CatchParameter (B.3.5).
                        catchScope = variableScope.Push(stream.Current, FastNodeType.Block);
                        if (!AssignmentLeftPattern(out catchParam, FastVariableKind.Let))
                            throw stream.Unexpected();
                    }
                    else
                        throw stream.Unexpected();

                    stream.Expect(TokenTypes.BracketEnd);
                }

                if (!Statement(out var @catch))
                    throw stream.Unexpected();

                catchScope?.Dispose();

                Finally(out var @finally);
                statement = new AstTryStatement(begin, PreviousToken, body, catchParam, @catch, @finally);

                return true;
            }
            else if (Finally(out var @finally))
            {
                statement = new AstTryStatement(begin, PreviousToken, body, null, null, @finally);
                return true;
            }
            else
                throw stream.Unexpected();
        }

        bool Finally(out AstStatement statement)
        {
            statement = null;

            if (!stream.CheckAndConsume(FastKeywords.@finally))
                return false;

            if (!Statement(out statement))
                throw stream.Unexpected();

            return true;
        }

        bool Throw(out AstStatement statement)
        {
            var begin = stream.Current;
            stream.Consume();

            if (stream.Current.Type == TokenTypes.LineTerminator)
                throw stream.Unexpected();

            if (!Expression(out var target))
                throw stream.Unexpected();

            statement = new AstThrowStatement(begin, PreviousToken, target);
            return true;
        }

        bool Continue(out AstStatement statement)
        {
            var begin = stream.Current;
            stream.Consume();

            AstIdentifier id = null;

            if (!EndOfLine())
                Identitifer(out id);

            statement = new AstContinueStatement(begin, PreviousToken, id);
            return true;
        }

        bool Break(out AstStatement statement)
        {
            var begin = stream.Current;
            stream.Consume();

            AstIdentifier id = null;
            if (!EndOfLine())
                Identitifer(out id);

            statement = new AstBreakStatement(begin, PreviousToken, id);
            return true;
        }

        bool Return(out AstStatement statement)
        {
            var begin = stream.Current;
            stream.Consume();

            var current = stream.Current;
            if (current.Type == TokenTypes.SemiColon || current.Type == TokenTypes.LineTerminator)
            {
                statement = new AstReturnStatement(begin, current);
                return true;
            }

            if (ExpressionSequence(out var target, TokenTypes.SemiColon))
            {
                statement = new AstReturnStatement(begin, PreviousToken, target);
                EndOfStatement();

                return true;
            }

            throw stream.Unexpected();
        }

        bool Using(out AstStatement statement, bool isAsync = false)
        {
            var start = stream.Current;
            statement = default;

            if (isAsync)
            {
                if (stream.Next.Keyword != FastKeywords.@using)
                    return false;

                stream.Consume();
                stream.Consume();
            }
            else
            {
                stream.Consume();
            }

            if (stream.Current.Type != TokenTypes.Identifier)
                return false;

            if (!Parameters(out var declarators, TokenTypes.SemiColon, false, FastVariableKind.Const))
                throw stream.Unexpected();

            statement = new AstVariableDeclaration(start, PreviousToken, declarators, FastVariableKind.Const, true, await: isAsync);
            return true;
        }
    }
}
