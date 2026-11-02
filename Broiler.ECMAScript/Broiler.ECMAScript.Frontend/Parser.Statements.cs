// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   37
// Annotated:        37/37
// Exempt:           1
// Human-reviewed:   0/37
// IP risk:          Low
// Security risk:    High
// Criteria:         1/1
// Resource impact:  2/10 max
// Unverified:       37
//
// GENERATED - DO NOT EDIT MANUALLY

using System.Collections.Generic;

namespace Broiler.ECMAScript.Syntax;

/// <summary>The goals' roots, statements and declarations (ECMA-262 clauses 14, 16.1 and 16.2).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 s16.1 statements grammar; IP=Low; Security=Medium; Resources=2; Fingerprint=130766
// Broiler-Human:        PENDING
internal sealed partial class Parser
{
    // The offset of the string literal that may begin a directive of the prologue being read, or -1.
    private int prologueStart = -1;

    /// <summary>The goal's root (clauses 16.1 ParseScript, 16.2 ParseModule, 19.2.1.1 PerformEval and 20.2.1.1.1 CreateDynamicFunction).</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 s16.1 statements grammar; IP=Low; Security=Medium; Resources=2; Fingerprint=0CB78B
    // Broiler-Human:        PENDING
    private ProgramNode ParseProgram()
    {
        var goal = request.Goal;
        var kind = request.FunctionKind;
        var parameters = new List<Pattern>();
        var parametersTrailingComma = false;
        List<Statement> body;

        switch (goal)
        {
            case ParseGoal.Script:
            case ParseGoal.Eval:
                // ScriptBody[~Yield, ~Await, ~Return]; the request's top-level await reads it with [Await] (decision D10).
                inAwait = request.Switches.TopLevelAwait;
                body = ParseStatementListUntilEnd(directives: true, module: false, top: true);
                break;
            case ParseGoal.Module:
                inAwait = request.Switches.TopLevelAwait;
                body = ParseStatementListUntilEnd(directives: false, module: true, top: true);
                break;
            case ParseGoal.FunctionBody:
                inYield = kind is DynamicFunctionKind.Generator or DynamicFunctionKind.AsyncGenerator;
                inAwait = kind is DynamicFunctionKind.Async or DynamicFunctionKind.AsyncGenerator;
                inReturn = true;
                functionDepth = 1;
                body = ParseStatementListUntilEnd(directives: true, module: false, top: true);
                break;
            default:
                // FormalParameters[?Yield, ?Await], read to the end of the text.
                inYield = kind is DynamicFunctionKind.Generator or DynamicFunctionKind.AsyncGenerator;
                inAwait = kind is DynamicFunctionKind.Async or DynamicFunctionKind.AsyncGenerator;
                functionDepth = 1;
                body = new List<Statement>();
                parametersTrailingComma = ParseFormalParameterList(parameters, TokenKind.EndOfInput);
                var end = Ex;
                if (end.Kind != TokenKind.EndOfInput)
                    throw Unexpected(end);
                break;
        }

        var span = new SourceSpan(source.BaseOffset, text.Length, source.BaseLine, source.BaseColumn);
        return new ProgramNode(span, goal, Lists.Freeze(body), Lists.Freeze(parameters), parametersTrailingComma, hasTopLevelAwait);
    }

    /// <summary>A StatementList, or a ModuleItemList, to the end of the text, its directive prologue first where the goal has one.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 s16.1 statements grammar; IP=Low; Security=Medium; Resources=2; Fingerprint=F080B0
    // Broiler-Human:        PENDING
    private List<Statement> ParseStatementListUntilEnd(bool directives, bool module, bool top)
    {
        var body = new List<Statement>();
        if (top)
            topLevel = body;
        if (directives)
            ParsePrologue(body, TokenKind.EndOfInput);

        while (true)
        {
            var token = Ex;
            if (token.Kind == TokenKind.EndOfInput)
                return body;
            body.Add(module ? ParseModuleItem() : ParseStatementListItem());
        }
    }

    /// <summary>
    /// A directive prologue (clause 11.2.1): the longest run of expression statements at the start of a body that each
    /// consist wholly of a string literal. A Use Strict Directive, exactly <c>use strict</c> with no escape or line
    /// continuation, makes the rest of the code strict.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 s16.1 statements grammar; IP=Low; Security=Medium; Resources=2; Fingerprint=FAEAE7
    // Broiler-Human:        PENDING
    private void ParsePrologue(List<Statement> body, TokenKind end)
    {
        while (true)
        {
            var token = Ex;
            if (token.Kind != TokenKind.StringLiteral || token.Kind == end)
                return;

            prologueStart = token.Start;
            var statement = ParseStatementListItem();
            body.Add(statement);
            if (statement is not ExpressionStatement { Directive: { } directive })
                return;

            if (directive == "use strict")
                strict = true;
        }
    }

    /// <summary>A StatementListItem (clause 14): a declaration or a statement, charged one level of nesting.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 s16.1 statements grammar; IP=Low; Security=Medium; Resources=2; Fingerprint=676DCE
    // Broiler-Human:        PENDING
    private Statement ParseStatementListItem()
    {
        var token = Ex;
        EnterNesting(token);
        try
        {
            if (token.Kind == TokenKind.IdentifierName && !token.IsEscaped)
            {
                switch (token.Word)
                {
                    case Word.Function:
                        return ParseFunctionDeclaration(isAsync: false, isDefault: false);
                    case Word.Async when IsAsyncFunction():
                        return ParseFunctionDeclaration(isAsync: true, isDefault: false);
                    case Word.Class:
                        return ParseClassDeclaration(new List<Decorator>(), isDefault: false);
                    case Word.Const:
                        return ParseLexicalDeclaration(DeclarationKind.Const);
                    case Word.Let when LetStartsDeclaration():
                        return ParseLexicalDeclaration(DeclarationKind.Let);
                    case Word.Using when UsingStartsDeclaration():
                        return ParseLexicalDeclaration(DeclarationKind.Using);
                    case Word.Await when inAwait && AwaitUsingStartsDeclaration():
                        return ParseLexicalDeclaration(DeclarationKind.AwaitUsing);
                }
            }
            else if (token.Kind == TokenKind.At && (proposals & ProposalFlags.Decorators) != 0)
            {
                var decorators = ParseDecorators();
                if (!IsWord(Ex, Word.Class))
                    throw Missing(Ex, "class");
                return ParseClassDeclaration(decorators, isDefault: false);
            }

            return ParseStatementCore(token);
        }
        finally
        {
            ExitNesting();
        }
    }

    /// <summary>A Statement (clause 14), charged one level of nesting.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 s16.1 statements grammar; IP=Low; Security=Medium; Resources=2; Fingerprint=494C02
    // Broiler-Human:        PENDING
    private Statement ParseStatement()
    {
        var token = Ex;
        EnterNesting(token);
        try
        {
            return ParseStatementCore(token);
        }
        finally
        {
            ExitNesting();
        }
    }

    /// <summary>Whether the current <c>async</c> begins an async function: <c>function</c> follows with no line terminator between (clause 15.8).</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 s16.1 statements grammar; IP=Low; Security=Medium; Resources=2; Fingerprint=212863
    // Broiler-Human:        PENDING
    private bool IsAsyncFunction()
    {
        var next = PeekAfter(LexicalGoal.RegExp, LexicalGoal.Div);
        return IsWord(next, Word.Function) && !next.NewLineBefore;
    }

    /// <summary>Whether a token can be a BindingIdentifier (clause 13.1): an escaped name, any name but a reserved word, or <c>yield</c> and <c>await</c>, whose refusals are early errors.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 s16.1 statements grammar; IP=Low; Security=Medium; Resources=2; Fingerprint=BFE52B
    // Broiler-Human:        PENDING
    private static bool CanBind(Token token) =>
        token.Kind == TokenKind.IdentifierName && (token.IsEscaped || !IsReserved(token.Word) || token.Word is Word.Yield or Word.Await);

    /// <summary>Whether the current <c>let</c> begins a LexicalDeclaration (clause 14.3.1): a binding name, <c>[</c> or <c>{</c> follows.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 s16.1 statements grammar; IP=Low; Security=Medium; Resources=2; Fingerprint=DDB0F9
    // Broiler-Human:        PENDING
    private bool LetStartsDeclaration()
    {
        var next = PeekAfter(LexicalGoal.RegExp, LexicalGoal.Div);
        return next.Kind is TokenKind.LeftBracket or TokenKind.LeftBrace || CanBind(next);
    }

    /// <summary>Whether the current <c>using</c> begins a UsingDeclaration: a binding name follows on the same line.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 s16.1 statements grammar; IP=Low; Security=Medium; Resources=2; Fingerprint=7DD4DD
    // Broiler-Human:        PENDING
    private bool UsingStartsDeclaration()
    {
        var next = PeekAfter(LexicalGoal.RegExp, LexicalGoal.Div);
        return CanBind(next) && !next.NewLineBefore;
    }

    /// <summary>Whether the current <c>await</c> begins an AwaitUsingDeclaration: <c>using</c> and a binding name follow, each on the same line.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 s16.1 statements grammar; IP=Low; Security=Medium; Resources=2; Fingerprint=F1E13F
    // Broiler-Human:        PENDING
    private bool AwaitUsingStartsDeclaration()
    {
        Peek(LexicalGoal.RegExp);
        var state = tokenizer.Save();
        var first = tokenizer.Next(LexicalGoal.Div);
        var second = tokenizer.Next(LexicalGoal.Div);
        tokenizer.Restore(state);
        return IsWord(first, Word.Using) && !first.NewLineBefore && CanBind(second) && !second.NewLineBefore;
    }

    /// <summary>The Statement production's alternatives, the lookahead restrictions of ExpressionStatement among them (clause 14.5).</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 s16.1 statements grammar; IP=Low; Security=Medium; Resources=2; Fingerprint=8795FC
    // Broiler-Human:        PENDING
    private Statement ParseStatementCore(Token token)
    {
        switch (token.Kind)
        {
            case TokenKind.LeftBrace:
                return ParseBlock();
            case TokenKind.Semicolon:
                Advance();
                return new EmptyStatement(SpanOf(token));
            case TokenKind.IdentifierName when !token.IsEscaped:
                switch (token.Word)
                {
                    case Word.Var:
                        return ParseVariableStatement();
                    case Word.If:
                        return ParseIf();
                    case Word.For:
                        return ParseFor();
                    case Word.While:
                        return ParseWhile();
                    case Word.Do:
                        return ParseDoWhile();
                    case Word.Continue:
                    case Word.Break:
                        return ParseJump(token.Word == Word.Break);
                    case Word.Return:
                        return ParseReturn();
                    case Word.With:
                        return ParseWith();
                    case Word.Switch:
                        return ParseSwitch();
                    case Word.Throw:
                        return ParseThrow();
                    case Word.Try:
                        return ParseTry();
                    case Word.Debugger:
                        Advance();
                        ConsumeSemicolon();
                        return new DebuggerStatement(SpanFrom(StartOf(token)));
                    case Word.Function:
                    case Word.Class:
                        // ExpressionStatement's lookahead excludes both; a declaration is no Statement.
                        throw Unexpected(token);
                    case Word.Import:
                        return ParseImportInStatement(token);
                    case Word.Export:
                        throw ModuleItemOutsideModuleItemList(token);
                    case Word.Let:
                        if (PeekAfter(LexicalGoal.RegExp, LexicalGoal.Div).Kind == TokenKind.LeftBracket)
                            throw Unexpected(token);
                        break;
                    case Word.Async:
                        if (IsAsyncFunction())
                            throw Unexpected(token);
                        break;
                }

                break;
        }

        if (token.Kind == TokenKind.IdentifierName && IsLabelIdentifier(token) && PeekAfter(LexicalGoal.RegExp, LexicalGoal.Div).Kind == TokenKind.Colon)
            return ParseLabelled(token);

        return ParseExpressionStatement(token);
    }

    /// <summary>Whether a token can be a LabelIdentifier (clause 13.1): an escaped name, a name that is no reserved word, <c>yield</c> outside [Yield] and <c>await</c> outside [Await].</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 s16.1 statements grammar; IP=Low; Security=Medium; Resources=2; Fingerprint=6C12C5
    // Broiler-Human:        PENDING
    private bool IsLabelIdentifier(Token token)
    {
        if (token.Kind != TokenKind.IdentifierName)
            return false;
        if (token.IsEscaped)
            return true;
        return token.Word switch
        {
            Word.Yield => !inYield,
            Word.Await => !inAwait,
            _ => !IsReserved(token.Word),
        };
    }

    /// <summary>
    /// An <c>import</c> where a statement stands: an ImportCall or <c>import.meta</c> begins an expression statement; an
    /// ImportDeclaration is a ModuleItem, so here, inside a block or under the Script goal, it is refused (catalogue row
    /// EX0010 at stage 4d).
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 s16.1 statements grammar; IP=Low; Security=Medium; Resources=2; Fingerprint=15C0F1
    // Broiler-Human:        PENDING
    private Statement ParseImportInStatement(Token token)
    {
        var next = PeekAfter(LexicalGoal.RegExp, LexicalGoal.Div);
        if (next.Kind is TokenKind.LeftParen or TokenKind.Dot)
            return ParseExpressionStatement(token);
        throw ModuleItemOutsideModuleItemList(token);
    }

    /// <summary>
    /// An ImportDeclaration or ExportDeclaration where only a statement may stand: under the Script goal, catalogue row EX0010;
    /// inside a module's block or function, the grammar's generic refusal (EX0008).
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 early errors at the parse, catalogue rows, JSD-0083 D11 D12; IP=Low; Security=Medium; Resources=1; Fingerprint=2D035E
    // Broiler-Human:        PENDING
    private ParseAbort ModuleItemOutsideModuleItemList(Token token) =>
        isModule ? Unexpected(token) : Refuse(Rows.EX0010, SpanOf(token), Spelling(token));

    /// <summary>An ExpressionStatement (clause 14.5), a directive where it opens a prologue and holds only its string literal (clause 11.2.1).</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 s16.1 statements grammar; IP=Low; Security=Medium; Resources=2; Fingerprint=42DBC3
    // Broiler-Human:        PENDING
    private Statement ParseExpressionStatement(Token token)
    {
        var prologue = prologueStart;
        prologueStart = -1;
        var start = StartOf(token);
        var expression = ParseExpression(noIn: false);
        string? directive = null;
        if (prologue == token.Start && expression is StringLiteral && expression.Span.Length == token.Length)
            directive = text.Substring(token.Start + 1, token.Length - 2);
        ConsumeSemicolon();
        return new ExpressionStatement(SpanFrom(start), expression, directive);
    }

    /// <summary>A Block (clause 14.2).</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 s16.1 statements grammar; IP=Low; Security=Medium; Resources=2; Fingerprint=B90444
    // Broiler-Human:        PENDING
    private BlockStatement ParseBlock()
    {
        var open = Expect(TokenKind.LeftBrace, LexicalGoal.RegExp);
        var body = new List<Statement>();
        while (Ex.Kind != TokenKind.RightBrace)
        {
            if (cur.Kind == TokenKind.EndOfInput)
                throw Missing(cur, "}");
            body.Add(ParseStatementListItem());
        }

        Advance();
        return new BlockStatement(SpanFrom(StartOf(open)), Lists.Freeze(body));
    }

    /// <summary>A VariableStatement (clause 14.3.2).</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 s16.1 statements grammar; IP=Low; Security=Medium; Resources=2; Fingerprint=5FF59A
    // Broiler-Human:        PENDING
    private Statement ParseVariableStatement()
    {
        var start = StartOf(Advance());
        var declarations = ParseDeclarators(DeclarationKind.Var, noIn: false, inForHead: false);
        ConsumeSemicolon();
        return new VariableDeclaration(SpanFrom(start), DeclarationKind.Var, declarations);
    }

    /// <summary>A LexicalDeclaration (clause 14.3.1), or a UsingDeclaration or AwaitUsingDeclaration.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 s16.1 statements grammar; IP=Low; Security=Medium; Resources=2; Fingerprint=630563
    // Broiler-Human:        PENDING
    private Statement ParseLexicalDeclaration(DeclarationKind kind)
    {
        var start = StartOf(Ex);
        Advance();
        if (kind == DeclarationKind.AwaitUsing)
        {
            NoteAwait();
            Advance();
        }

        var declarations = ParseDeclarators(kind, noIn: false, inForHead: false);
        ConsumeSemicolon();
        return new VariableDeclaration(SpanFrom(start), kind, declarations);
    }

    /// <summary>
    /// A VariableDeclarationList or BindingList (clauses 14.3.1 and 14.3.2): a binding pattern needs its Initializer but in a
    /// for head, where the head decides; a <c>using</c> binds names only.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 s16.1 statements grammar; IP=Low; Security=Medium; Resources=2; Fingerprint=63B5F9
    // Broiler-Human:        PENDING
    private IReadOnlyList<VariableDeclarator> ParseDeclarators(DeclarationKind kind, bool noIn, bool inForHead)
    {
        var list = new List<VariableDeclarator>();
        do
        {
            var token = Op;
            var start = StartOf(token);
            Pattern target;
            if (kind is DeclarationKind.Using or DeclarationKind.AwaitUsing)
                target = ParseBindingIdentifier();
            else
                target = ParseBindingTarget();

            Expression? initializer = null;
            if (Eat(TokenKind.Equals))
                initializer = ParseAssignment(noIn);
            else if (target is not BindingIdentifier && !inForHead)
                throw Missing(Op, "=");

            list.Add(new VariableDeclarator(SpanFrom(start), target, initializer));
        }
        while (Eat(TokenKind.Comma));

        return Lists.Freeze(list);
    }

    /// <summary>An IfStatement (clause 14.6), each clause a Statement or, in sloppy code under Annex B, a FunctionDeclaration (Annex B.3.3).</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 s16.1 statements grammar; IP=Low; Security=Medium; Resources=2; Fingerprint=E561FB
    // Broiler-Human:        PENDING
    private Statement ParseIf()
    {
        var start = StartOf(Advance());
        Expect(TokenKind.LeftParen);
        var test = ParseExpression(noIn: false);
        Expect(TokenKind.RightParen);
        var consequent = ParseIfClause();
        Statement? alternate = null;
        if (IsWord(Ex, Word.Else))
        {
            Advance();
            alternate = ParseIfClause();
        }

        return new IfStatement(SpanFrom(start), test, consequent, alternate);
    }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 s16.1 statements grammar; IP=Low; Security=Medium; Resources=2; Fingerprint=AE4EF1
    // Broiler-Human:        PENDING
    private Statement ParseIfClause()
    {
        var token = Ex;

        // Annex B.3.3's FunctionDeclaration as an `if` clause applies only to non-strict code (catalogue row EG0005).
        if (IsWord(token, Word.Function) && annexB && strict)
            throw Refuse(Rows.EG0005, SpanOf(token), "function");
        if (IsWord(token, Word.Function) && annexB && !strict)
        {
            EnterNesting(token);
            try
            {
                return ParseFunctionDeclaration(isAsync: false, isDefault: false, plainOnly: true);
            }
            finally
            {
                ExitNesting();
            }
        }

        return ParseStatement();
    }

    /// <summary>A WhileStatement (clause 14.7.3).</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 s16.1 statements grammar; IP=Low; Security=Medium; Resources=2; Fingerprint=C062C0
    // Broiler-Human:        PENDING
    private Statement ParseWhile()
    {
        var start = StartOf(Advance());
        Expect(TokenKind.LeftParen);
        var test = ParseExpression(noIn: false);
        Expect(TokenKind.RightParen);
        var body = ParseStatement();
        return new WhileStatement(SpanFrom(start), test, body);
    }

    /// <summary>A DoWhileStatement (clause 14.7.2): a semicolon is inserted after its <c>)</c> whatever follows (clause 12.10).</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 s16.1 statements grammar; IP=Low; Security=Medium; Resources=2; Fingerprint=6C6EA0
    // Broiler-Human:        PENDING
    private Statement ParseDoWhile()
    {
        var start = StartOf(Advance());
        var body = ParseStatement();
        ExpectWord(Word.While, "while", LexicalGoal.RegExp);
        Expect(TokenKind.LeftParen);
        var test = ParseExpression(noIn: false);
        Expect(TokenKind.RightParen);
        Eat(TokenKind.Semicolon, LexicalGoal.RegExp);
        return new DoWhileStatement(SpanFrom(start), body, test);
    }

    /// <summary>
    /// A for statement (clauses 14.7.4 and 14.7.5): a C-style head, <c>in</c>, <c>of</c> or <c>for await</c>, with the
    /// lookahead restrictions on <c>let</c> and <c>async of</c>, Annex B.3.5's initialiser in a sloppy <c>for</c>-<c>in</c>,
    /// and no semicolon inserted in the head (row P16b).
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14.7.4 s14.7.5 for heads, row P16b; IP=Low; Security=High; Resources=2; Fingerprint=EDFCD9
    // Broiler-Falsified-If: a semicolon is inserted in a for head, or a head the lookahead restrictions exclude is admitted
    // Broiler-Human:        PENDING
    private Statement ParseFor()
    {
        var start = StartOf(Advance());
        var isAwait = false;
        if (inAwait && IsWord(Op, Word.Await))
        {
            Advance();
            NoteAwait();
            isAwait = true;
        }

        Expect(TokenKind.LeftParen);
        var token = Ex;
        SyntaxNode? init = null;

        if (token.Kind == TokenKind.Semicolon)
        {
            if (isAwait)
                throw Unexpected(token);
            return ParseForRest(start, null);
        }

        DeclarationKind? declaration = null;
        if (IsWord(token, Word.Var))
            declaration = DeclarationKind.Var;
        else if (IsWord(token, Word.Const))
            declaration = DeclarationKind.Const;
        else if (IsWord(token, Word.Let) && LetStartsDeclaration())
            declaration = DeclarationKind.Let;
        else if (IsWord(token, Word.Using) && UsingStartsDeclaration() && (!IsWord(PeekAfter(LexicalGoal.RegExp, LexicalGoal.Div), Word.Of) || UsingOfDeclares()))
            declaration = DeclarationKind.Using;
        else if (inAwait && IsWord(token, Word.Await) && AwaitUsingStartsDeclaration())
            declaration = DeclarationKind.AwaitUsing;

        if (declaration is { } kind)
        {
            var declStart = StartOf(token);
            Advance();
            if (kind == DeclarationKind.AwaitUsing)
            {
                NoteAwait();
                Advance();
            }

            var declarators = ParseDeclarators(kind, noIn: true, inForHead: true);
            var head = new VariableDeclaration(SpanFrom(declStart), kind, declarators);
            var next = Op;
            var isIn = IsWord(next, Word.In);
            var isOf = IsWord(next, Word.Of);
            if ((isIn && !isAwait && kind is not (DeclarationKind.Using or DeclarationKind.AwaitUsing)) || isOf)
            {
                if (declarators.Count != 1)
                    throw Unexpected(next);
                var only = declarators[0];
                if (only.Initializer is not null)
                {
                    // Annex B.3.5: for (var BindingIdentifier Initializer in Expression), in sloppy code only.
                    // Annex B.3.5 applies only to non-strict code (catalogue row EG0007).
                    if (isIn && kind == DeclarationKind.Var && only.Target is BindingIdentifier && annexB && strict)
                        throw Refuse(Rows.EG0007, SpanOf(next), "in");
                    if (!(isIn && kind == DeclarationKind.Var && only.Target is BindingIdentifier && annexB && !strict))
                        throw Unexpected(next);
                }

                Advance();
                return ParseForInOfRest(start, head, isOf, isAwait);
            }

            if (isAwait)
                throw Unexpected(next);
            foreach (var declarator in declarators)
            {
                if (declarator.Target is not BindingIdentifier && declarator.Initializer is null)
                    throw Missing(next, "=");
            }

            return ParseForRest(start, head);
        }

        // An expression, or the LeftHandSideExpression of a for-in or for-of head.
        var startsWithLet = IsWord(token, Word.Let);
        var startsWithAsync = IsWord(token, Word.Async);
        var mark = coverInitialized.Count;
        var expression = ParseExpressionCover(noIn: true);
        var after = Op;
        if (IsWord(after, Word.Of) || (IsWord(after, Word.In) && !isAwait))
        {
            var isOf = IsWord(after, Word.Of);
            if (isOf && (startsWithLet || (startsWithAsync && expression is Identifier { IsEscaped: false } && !isAwait)))
                throw Unexpected(after);
            init = Converting(Rows.EE0053, () => ToAssignmentTarget(expression, forHead: true));
            CheckCoverInitialized(mark);
            Advance();
            return ParseForInOfRest(start, init, isOf, isAwait);
        }

        CheckCoverInitialized(mark);
        if (isAwait)
            throw Unexpected(after);
        return ParseForRest(start, expression);
    }

    /// <summary>
    /// Whether <c>using of</c> in a for head begins a LexicalDeclaration of the name <c>of</c>, in a C-style head
    /// (<c>for (using of = null;;)</c>): ForDeclaration's lookahead excludes <c>using of</c>, so only <c>=</c>, <c>,</c> or
    /// <c>;</c> after it makes it one.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 s16.1 statements grammar; IP=Low; Security=Medium; Resources=2; Fingerprint=2999AB
    // Broiler-Human:        PENDING
    private bool UsingOfDeclares()
    {
        var state = tokenizer.Save();
        tokenizer.Next(LexicalGoal.Div);
        var after = tokenizer.Next(LexicalGoal.Div);
        tokenizer.Restore(state);
        return after.Kind is TokenKind.Equals or TokenKind.Comma or TokenKind.Semicolon;
    }

    /// <summary>The rest of a C-style head, from its first <c>;</c>: both semicolons are required (clause 12.10, row P16b).</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 s16.1 statements grammar; IP=Low; Security=Medium; Resources=2; Fingerprint=F53922
    // Broiler-Human:        PENDING
    private Statement ParseForRest(SourceSpan start, SyntaxNode? init)
    {
        Expect(TokenKind.Semicolon);
        Expression? test = null;
        if (Ex.Kind != TokenKind.Semicolon)
            test = ParseExpression(noIn: false);
        Expect(TokenKind.Semicolon);
        Expression? update = null;
        if (Ex.Kind != TokenKind.RightParen)
            update = ParseExpression(noIn: false);
        Expect(TokenKind.RightParen);
        var body = ParseStatement();
        return new ForStatement(SpanFrom(start), init, test, update, body);
    }

    /// <summary>The rest of a for-in or for-of head, after <c>in</c> or <c>of</c>: an Expression, or for <c>of</c> an AssignmentExpression.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 s16.1 statements grammar; IP=Low; Security=Medium; Resources=2; Fingerprint=59B5A5
    // Broiler-Human:        PENDING
    private Statement ParseForInOfRest(SourceSpan start, SyntaxNode left, bool isOf, bool isAwait)
    {
        var right = isOf ? ParseAssignment(noIn: false) : ParseExpression(noIn: false);
        Expect(TokenKind.RightParen);
        var body = ParseStatement();
        return isOf
            ? new ForOfStatement(SpanFrom(start), left, right, body, isAwait)
            : new ForInStatement(SpanFrom(start), left, right, body);
    }

    /// <summary>A ContinueStatement or BreakStatement (clauses 14.8 and 14.9): a label on the same line, or none.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 s16.1 statements grammar; IP=Low; Security=Medium; Resources=2; Fingerprint=FE862B
    // Broiler-Human:        PENDING
    private Statement ParseJump(bool isBreak)
    {
        var start = StartOf(Advance());
        IdentifierName? label = null;
        var token = Op;
        if (token.Kind == TokenKind.IdentifierName && !token.NewLineBefore && IsLabelIdentifier(token))
        {
            Advance();
            label = new IdentifierName(SpanOf(token), token.Value!, token.IsEscaped);
        }

        ConsumeSemicolon();
        var span = SpanFrom(start);
        return isBreak ? new BreakStatement(span, label) : new ContinueStatement(span, label);
    }

    /// <summary>A ReturnStatement (clause 14.10): only a function body is parsed with [Return] (catalogue rows EX0032 and EX0033 at stage 4d).</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 s16.1 statements grammar; IP=Low; Security=Medium; Resources=2; Fingerprint=7A96D4
    // Broiler-Human:        PENDING
    private Statement ParseReturn()
    {
        var keyword = Ex;
        if (!inReturn)
            throw Refuse(inStaticBlock ? Rows.EX0033 : Rows.EX0032, SpanOf(keyword), "return");
        var start = StartOf(Advance());
        Expression? argument = null;
        if (!AtStatementEnd(Ex))
            argument = ParseExpression(noIn: false);
        ConsumeSemicolon();
        return new ReturnStatement(SpanFrom(start), argument);
    }

    /// <summary>A WithStatement (clause 14.11).</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 s16.1 statements grammar; IP=Low; Security=Medium; Resources=2; Fingerprint=315EF0
    // Broiler-Human:        PENDING
    private Statement ParseWith()
    {
        var start = StartOf(Advance());
        Expect(TokenKind.LeftParen);
        var @object = ParseExpression(noIn: false);
        Expect(TokenKind.RightParen);
        var body = ParseStatement();
        return new WithStatement(SpanFrom(start), @object, body);
    }

    /// <summary>A SwitchStatement (clause 14.12): at most one DefaultClause.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 s16.1 statements grammar; IP=Low; Security=Medium; Resources=2; Fingerprint=80B6A8
    // Broiler-Human:        PENDING
    private Statement ParseSwitch()
    {
        var start = StartOf(Advance());
        Expect(TokenKind.LeftParen);
        var discriminant = ParseExpression(noIn: false);
        Expect(TokenKind.RightParen);
        Expect(TokenKind.LeftBrace);
        var cases = new List<SwitchCase>();
        var sawDefault = false;
        while (true)
        {
            var token = Ex;
            if (token.Kind == TokenKind.RightBrace)
                break;
            var caseStart = StartOf(token);
            Expression? test = null;
            if (IsWord(token, Word.Case))
            {
                Advance();
                test = ParseExpression(noIn: false);
            }
            else if (IsWord(token, Word.Default) && !sawDefault)
            {
                sawDefault = true;
                Advance();
            }
            else
            {
                throw Unexpected(token);
            }

            Expect(TokenKind.Colon);
            var consequent = new List<Statement>();
            while (true)
            {
                var next = Ex;
                if (next.Kind is TokenKind.RightBrace or TokenKind.EndOfInput || IsWord(next, Word.Case) || IsWord(next, Word.Default))
                    break;
                consequent.Add(ParseStatementListItem());
            }

            cases.Add(new SwitchCase(SpanFrom(caseStart), test, Lists.Freeze(consequent)));
        }

        Advance();
        return new SwitchStatement(SpanFrom(start), discriminant, Lists.Freeze(cases));
    }

    /// <summary>A ThrowStatement (clause 14.14): its expression on the same line.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 s16.1 statements grammar; IP=Low; Security=Medium; Resources=2; Fingerprint=ADEE9C
    // Broiler-Human:        PENDING
    private Statement ParseThrow()
    {
        var start = StartOf(Advance());
        var token = Ex;
        if (token.NewLineBefore)
            throw Unexpected(token);
        var argument = ParseExpression(noIn: false);
        ConsumeSemicolon();
        return new ThrowStatement(SpanFrom(start), argument);
    }

    /// <summary>A TryStatement (clause 14.15), a Catch with or without its parameter.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 s16.1 statements grammar; IP=Low; Security=Medium; Resources=2; Fingerprint=665176
    // Broiler-Human:        PENDING
    private Statement ParseTry()
    {
        var start = StartOf(Advance());
        var block = ParseBlock();
        CatchClause? handler = null;
        BlockStatement? finalizer = null;
        var token = Ex;
        if (IsWord(token, Word.Catch))
        {
            var catchStart = StartOf(Advance());
            Pattern? parameter = null;
            if (Eat(TokenKind.LeftParen))
            {
                parameter = ParseBindingTarget();
                Expect(TokenKind.RightParen);
            }

            var body = ParseBlock();
            handler = new CatchClause(SpanFrom(catchStart), parameter, body);
            token = Ex;
        }

        if (IsWord(token, Word.Finally))
        {
            Advance();
            finalizer = ParseBlock();
        }

        if (handler is null && finalizer is null)
            throw Missing(token, "catch");
        return new TryStatement(SpanFrom(start), block, handler, finalizer);
    }

    /// <summary>A LabelledStatement (clause 14.13): its item a Statement or a FunctionDeclaration, whose admission is a static-semantic rule (stage 4d).</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 s16.1 statements grammar; IP=Low; Security=Medium; Resources=2; Fingerprint=F630A2
    // Broiler-Human:        PENDING
    private Statement ParseLabelled(Token token)
    {
        var start = StartOf(token);
        Advance();
        var label = new IdentifierName(SpanOf(token), token.Value!, token.IsEscaped);
        Expect(TokenKind.Colon);
        var next = Ex;
        Statement body;
        if (IsWord(next, Word.Function))
        {
            EnterNesting(next);
            try
            {
                body = ParseFunctionDeclaration(isAsync: false, isDefault: false, plainOnly: true);
            }
            finally
            {
                ExitNesting();
            }
        }
        else
        {
            body = ParseStatement();
        }

        return new LabelledStatement(SpanFrom(start), label, body);
    }

    /// <summary>
    /// The reserved words of clause 12.7.2, which no unescaped name may be where an Identifier is meant (<c>yield</c> and
    /// <c>await</c> among them; whether each may stand as a name is decided by the [Yield] and [Await] parameters).
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 s16.1 statements grammar; IP=Low; Security=Medium; Resources=2; Fingerprint=B778CB
    // Broiler-Human:        PENDING
    private static bool IsReserved(Word word) => word is Word.Await or Word.Break or Word.Case or Word.Catch or Word.Class or Word.Const
        or Word.Continue or Word.Debugger or Word.Default or Word.Delete or Word.Do or Word.Else or Word.Enum or Word.Export
        or Word.Extends or Word.False or Word.Finally or Word.For or Word.Function or Word.If or Word.Import or Word.In
        or Word.Instanceof or Word.New or Word.Null or Word.Return or Word.Super or Word.Switch or Word.This or Word.Throw
        or Word.True or Word.Try or Word.Typeof or Word.Var or Word.Void or Word.While or Word.With or Word.Yield;

    /// <summary>Records an <c>await</c> of the top level, the fact [[HasTLA]] reads (clause 16.2.1.7).</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 s16.1 statements grammar; IP=Low; Security=Medium; Resources=2; Fingerprint=B2875B
    // Broiler-Human:        PENDING
    private void NoteAwait()
    {
        if (functionDepth == 0)
            hasTopLevelAwait = true;
    }
}
