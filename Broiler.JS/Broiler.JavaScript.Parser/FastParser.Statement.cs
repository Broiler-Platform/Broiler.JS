using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Statements;
using Broiler.JavaScript.ExpressionCompiler.Core;
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

        if (token.IsEscapedReservedWord)
            throw new FastParseException(token, "Keyword must not contain escaped characters");

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

            case TokenTypes.At:
                // A DecoratorList in statement position introduces a (decorated) class
                // declaration. Decorators are parsed and discarded; the class is then
                // parsed as usual so it binds its name in the enclosing scope.
                Decorators();
                if (stream.Current.Keyword != FastKeywords.@class)
                    throw stream.Unexpected();
                return Class(out node);
        }

        if (SingleStatement(begin, out node))
        {
            stream.CheckAndConsumeAny(TokenTypes.SemiColon, TokenTypes.LineTerminator);
            return true;
        }

        return false;
    }

    // Annex B.3.4: a FunctionDeclaration appearing as the sole Statement of an
    // `if` clause behaves as if enclosed in a Block, so its lexical binding is
    // scoped to that implicit block rather than the enclosing function/program
    // body. This only changes observable behaviour when the declared name shadows
    // a formal parameter (Annex B.3.3.1: the parameter binding must win and the
    // var-hoisting is skipped), so we only synthesize the block in that case and
    // otherwise leave the existing body-level handling untouched.
    bool NestedStatement(out AstStatement node)
    {
        SkipNewLines();

        if (stream.Current.Keyword == FastKeywords.function
            && NestedFunctionShadowsParameter())
        {
            var begin = stream.Current;
            var scope = variableScope.Push(begin, FastNodeType.Block);
            try
            {
                if (!Statement(out var fn))
                    throw stream.Unexpected();

                node = new AstBlock(begin, PreviousToken, new Sequence<AstStatement> { fn })
                {
                    HoistingScope = scope.GetVariables(),
                    AnnexBFunctionNames = scope.GetAnnexBNames(),
                    IsSyntheticFunctionStatementBlock = true
                };
                return true;
            }
            finally
            {
                scope.Dispose();
            }
        }

        // Annex B.3.4: a FunctionDeclaration used as the sole statement of an
        // `if` clause behaves as if wrapped in its own block, so its name must
        // not form a lexical binding in (or conflict with one in) the enclosing
        // block/switch. The compiler handles it via VisitRuntimeFunctionDeclaration;
        // here we only suppress the enclosing-scope lexical registration.
        var wasNestedFunctionClause = nestedFunctionClause;
        nestedFunctionClause = stream.Current.Keyword == FastKeywords.function;
        try
        {
            return Statement(out node);
        }
        finally
        {
            nestedFunctionClause = wasNestedFunctionClause;
        }
    }

    // True when the upcoming `function` declaration's name matches a formal
    // parameter of the nearest enclosing function (where parameter names live).
    private bool NestedFunctionShadowsParameter()
    {
        var next = stream.Current.Next;
        if (next == null || next.Type != TokenTypes.Identifier)
            return false;

        var scope = variableScope.Top;
        while (scope != null && scope.NodeType != FastNodeType.FunctionExpression)
            scope = scope.Parent;

        return scope != null && scope.DeclaresVariable(next.Span);
    }

    // `let` is only the start of a LexicalDeclaration when the next token can begin a
    // BindingList: `[`, `{`, or a BindingIdentifier. Otherwise (sloppy mode) `let` is an
    // ordinary IdentifierReference — `let`, `let = 1`, `let.x`, `let in obj`, `let;`,
    // `let: …` — and the caller parses it as an expression/label instead. `let [` is the
    // restricted production and counts as a declaration even across a line terminator.
    // In strict mode StrictModeValidator later rejects such identifier/label uses of
    // `let` (IsRestrictedName). Consumes nothing — peeks past `let` and rewinds.
    bool LetBeginsLexicalDeclaration()
    {
        var letPosition = stream.Current;
        stream.Consume();
        stream.LineTerminator();
        var afterLet = stream.Current.Type;
        stream.Reset(letPosition);
        return afterLet is TokenTypes.SquareBracketStart
            or TokenTypes.Identifier
            or TokenTypes.CurlyBracketStart;
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
                    if (LetBeginsLexicalDeclaration())
                        return VariableDeclaration(out node, FastVariableKind.Let);
                    // `let` is an IdentifierReference here — fall through to the
                    // labeled-statement / expression-statement handling below.
                    break;

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
                    var asyncToken = stream.Current;
                    stream.Consume();
                    // `async [no LineTerminator here] function` is an
                    // AsyncFunctionDeclaration. Otherwise `async` is a plain
                    // IdentifierReference — `async`, `async()`, `async = 1`, an
                    // async arrow (`async x => …`), or `async\nfunction f(){}`
                    // which ASIs into `async; function f(){}`. Reset and fall
                    // through to the expression-statement path.
                    if (stream.Current.Keyword == FastKeywords.function)
                    {
                        pendingAsyncStart = asyncToken;
                        return Function(out node, true);
                    }
                    stream.Reset(asyncToken);
                    break;

                case FastKeywords.function:
                    return Function(out node);
            }
        }

        // goto....
        if (LabeledLoop(out node))
            return true;

        if (ExpressionSequence(out var expression, TokenTypes.SemiColon))
        {
            if (stream.Current.Type == TokenTypes.CurlyBracketStart
                && stream.Previous.Type != TokenTypes.SemiColon)
                throw stream.Unexpected();

            node = new AstExpressionStatement(token, PreviousToken, expression);
            return true;
        }

        return begin.Reset();

        bool LabeledLoop(out AstStatement statement)
        {
            if (stream.CheckAndConsume(TokenTypes.Identifier, TokenTypes.Colon, out var id, out var _))
            {
                // `yield` is a reserved word inside a generator body and cannot be used
                // as a LabelIdentifier (`function* g(){ yield: 1 }` is a SyntaxError).
                if (inGeneratorBody && id.Keyword == FastKeywords.yield)
                    throw stream.Unexpected();

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
                        {
                            // `let` is only a LexicalDeclaration (forbidden here) when
                            // it begins the restricted `let [` lookahead, or when a
                            // BindingIdentifier / `{` pattern follows on the SAME line
                            // (ASI cannot split it). Otherwise `let` is an
                            // IdentifierReference and the labelled item is the
                            // expression statement it leads — e.g. `L: let\n{}` is
                            // `let;` followed by the block `{}`, and `L: let\nx = 1` is
                            // `let;` followed by `x = 1`.
                            var letPosition = stream.Current;
                            stream.Consume();
                            var lineBreakAfterLet = stream.LineTerminator();
                            var afterLet = stream.Current.Type;
                            stream.Reset(letPosition);

                            var isLexicalDeclaration = afterLet == TokenTypes.SquareBracketStart
                                || (!lineBreakAfterLet
                                    && (afterLet == TokenTypes.Identifier
                                        || afterLet == TokenTypes.CurlyBracketStart));
                            if (isLexicalDeclaration)
                                throw new FastParseException(current, "Lexical declaration cannot appear in a single-statement context");

                            if (!ExpressionSequence(out var letExpression, TokenTypes.SemiColon))
                                throw stream.Unexpected();

                            statement = new AstLabeledStatement(id, new AstExpressionStatement(current, PreviousToken, letExpression));
                            return true;
                        }

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
            // A `return` statement is only valid inside a function body. At the top
            // level of a Script, Module, or eval code (functionDepth == 0) it is an
            // early SyntaxError — even when the eval is a direct eval invoked from
            // within a function, since eval code is not itself a function body.
            if (functionDepth == 0)
                throw stream.Unexpected();
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
