// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   42
// Annotated:        42/42
// Exempt:           35
// Human-reviewed:   0/42
// IP risk:          Low
// Security risk:    High
// Criteria:         6/6
// Resource impact:  2/10 max
// Unverified:       42
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Generic;
using System.Globalization;
using Broiler.Unicode.Algorithms;

namespace Broiler.ECMAScript.Syntax;

/// <summary>
/// The syntactic grammar of ECMA-262 (clauses 13 to 16), a recursive-descent parse of one request (Broiler.VM's JSD-0083,
/// sub-area P; the plan's stage 4c): the tree with full spans and raw facts, every goal the request names, cover grammars
/// by conversion, arrow functions without rewinding, automatic semicolon insertion, and the contract's two limits checked
/// before the stack is.
/// </summary>
/// <remarks>
/// <para>
/// <b>The parser names every token's lexical goal</b> (clause 12): it asks the tokenizer for the next input element under
/// the goal the syntactic grammar stands at, a regular-expression literal where an expression may begin and division where
/// an operator may follow, and a template's continuation where a substitution ends. A token read under one goal and
/// wanted under another is read again from where it began; nothing the parser has built is undone.
/// </para>
/// <para>
/// <b>Cover grammars are converted, never parsed again</b>: a parenthesised list, an async call's arguments and an array
/// or object literal are read once as the cover, and an arrow's <c>=&gt;</c> or an assignment's <c>=</c> converts what was
/// read into parameters or a pattern (clauses 13.2.10, 13.3.6, 13.15.5 and 15.3), refusing what the cover does not cover.
/// </para>
/// <para>
/// <b>The grammar is the parser's</b>, with the early errors it checks as it reads (a cover's conversion, an operand that
/// is no assignment target, a proposal's form its flag does not admit); the declarations walk
/// (<see cref="Semantics.DeclarationsWalk"/>) reports the other static-semantic early errors over the tree the parser
/// admits. A parser's refusal is a <see cref="SyntaxDiagnostic"/> of phase <see cref="DiagnosticPhase.Parser"/> with the
/// code of the catalogue row its site serves: <see cref="UnexpectedToken"/> (EX0008) and <see cref="MissingToken"/>
/// (EX0009) where no other row explains it; the contract's two limits carry their catalogue rows' codes (EX0011 and
/// EX0012), and a lexical refusal the tokenizer's. <i>(Revised 2026-11-02, at stage SJL-13.2d; it read "Only the grammar is
/// the parser's: the static-semantic early errors are stage 4d's, so a source the grammar admits is accepted here even
/// where an early error refuses it. A refusal is a SyntaxDiagnostic of phase Parser with one of two provisional codes,
/// UnexpectedToken and MissingToken, which stage 4d replaces by the catalogue row each site serves; the contract's two
/// limits carry their catalogue rows' codes (EX0011 and EX0012), and a lexical refusal the tokenizer's.")</i>
/// </para>
/// <para>
/// <b>Limits before the stack</b> (decision D14; JSD-0080's decision D27): the nesting depth is charged once for each
/// statement, assignment expression, binding pattern, recursing prefix operator and nested <c>new</c>, as Broiler.VM's
/// <c>JsParser</c> charges it, and for each right operand of <c>**</c>, the one right-recursive binary operator, and for
/// each class heritage, through which a class expression recurses, both of which Broiler.VM's parser leaves uncharged;
/// the tree depth is charged once for each node a binary chain or a member or call chain adds, each charged its whole
/// height, the most chain nodes on any path from it to a leaf, so that a tree deeper than
/// <see cref="ParseLimits.MaximumTreeDepth"/> is refused wherever it is built, and no tree within it is. <i>(Revised
/// 2026-11-02, at stage SJL-13.2f, by the owner's decision of that day on decision D14's residual left-operand shapes;
/// it read "the tree depth is charged once for each node a binary chain or a member or call chain adds, from the spine of
/// the chain's first operand, which closes the left-operand gap in all four of its shapes.")</i>
/// </para>
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s12 lexical goals, JSD-0083 SJL-13.2c parser; IP=Low; Security=Medium; Resources=2; Fingerprint=130766
// Broiler-Human:        PENDING
internal sealed partial class Parser
{
    /// <summary>The row of a token no production admits where it stands, where no other row explains the site (catalogue row EX0008; stage 4c's provisional code, which stage 4d kept for these sites).</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s12 lexical goals, JSD-0083 SJL-13.2c parser; IP=Low; Security=Low; Resources=0; Fingerprint=DE2D19
    // Broiler-Human:        PENDING
    internal static readonly LexicalRefusal UnexpectedToken = Rows.EX0008;

    /// <summary>The row of a token a production requires and does not find (catalogue row EX0009; stage 4c's provisional code, which stage 4d kept for these sites).</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s12 lexical goals, JSD-0083 SJL-13.2c parser; IP=Low; Security=Low; Resources=0; Fingerprint=21A3FD
    // Broiler-Human:        PENDING
    internal static readonly LexicalRefusal MissingToken = Rows.EX0009;

    /// <summary>The contract's nesting diagnostic (catalogue row EX0011).</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s12 lexical goals, JSD-0083 SJL-13.2c parser; IP=Low; Security=Low; Resources=0; Fingerprint=1EC0E4
    // Broiler-Human:        PENDING
    internal static readonly LexicalRefusal NestingTooDeep = Rows.EX0011;

    /// <summary>The contract's tree-depth diagnostic (catalogue row EX0012).</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s12 lexical goals, JSD-0083 SJL-13.2c parser; IP=Low; Security=Low; Resources=0; Fingerprint=991416
    // Broiler-Human:        PENDING
    internal static readonly LexicalRefusal TreeTooDeep = Rows.EX0012;

    private readonly ParseRequest request;
    private readonly SourceText source;
    private readonly string text;
    private readonly Tokenizer tokenizer;
    private readonly int maximumNesting;
    private readonly bool annexB;
    private readonly ProposalFlags proposals;
    private readonly bool isModule;
    private readonly List<Token> tokens = new();

    // The current token: read under curGoal from the tokenizer state curState, or not read yet.
    private Token cur;
    private bool hasCur;
    private LexicalGoal curGoal;
    private TokenizerState curState;

    // The last token consumed.
    private Token previous;
    private bool hasPrevious;

    // The grammar's parameters where the parse stands.
    private bool inYield;
    private bool inAwait;
    private bool inReturn;
    private bool strict;
    private int functionDepth;
    private bool hasTopLevelAwait;

    // The limits' counters.
    private int nesting;

    // The row a conversion of a cover refuses by when what was read covers nothing the conversion admits (stage 4d: EE0087 or
    // EE0155 at an arrow's head, EE0032 or EE0053 at an assignment's or a for head's pattern, EE0038 inside one), or null
    // where no cover is being converted and the refusal is the grammar's generic one (EX0008).
    private LexicalRefusal? coverRule;

    // Whether the parse stands in a class static block's statements and inside no function nested in it (EX0033's place).
    private bool inStaticBlock;

    // The top level's statements, read so far: when the grammar refuses, the early errors of those completed before the
    // refusal come first (decision D11, the library's order).
    private List<Statement>? topLevel;

    // The literals whose refusal waits for the cover around them: an object literal holding a CoverInitializedName, and an
    // assignment's array or object literal target that covers no AssignmentPattern, read where an arrow's head may yet
    // convert it (deferredPatterns), each until a conversion takes it, in the order read. (Revised 2026-11-02, at stage
    // SJL-13.2f, by part A's triage; it read "The object literals holding a CoverInitializedName that no conversion has
    // taken yet, in source order.", and an assignment target's refusal was made where the assignment was read.)
    private readonly List<Expression> coverInitialized = new();

    // The refusal of each deferred assignment target in coverInitialized: EE0032's, or a nested literal's row, made when
    // no conversion takes the literal: an arrow's head refuses it by its own row, and an enclosing literal converted to an
    // assignment pattern takes it as a DestructuringAssignmentTarget, EE0038's (stage SJL-13.2f; revised 2026-11-02, after
    // review, from "made when no arrow's head takes the literal").
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13.15.1 EE0032, s15.3.1 ArrowParameters cover early errors; IP=Low; Security=Low; Resources=0; Fingerprint=4E23F0
    // Broiler-Human:        PENDING
    private readonly Dictionary<Expression, ParseAbort> deferredPatterns = new(ReferenceEqualityComparer.Instance);

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s12 lexical goals, JSD-0083 SJL-13.2c parser; IP=Low; Security=Low; Resources=0; Fingerprint=ABA2F0
    // Broiler-Human:        PENDING
    private readonly Dictionary<Expression, (Token At, string Name)> coverInitializedAt = new(ReferenceEqualityComparer.Instance);

    // The cover the primary expression just read, which an arrow's => may convert: a parenthesised list, or an async call.
    private Expression? arrowCover;

    // An arrow function the primary expression read whole (async x => ...), which only an AssignmentExpression may be.
    private Expression? primaryArrow;

    // The unescaped `await` IdentifierReferences read outside [Await] in the function the parse stands in, in source order:
    // an async arrow's conversion refuses each its cover read that its parameters do not bind (clause 15.9).
    private readonly List<Identifier> awaitReferences = new();

    // While an async arrow's cover is converted, the `await` names its own parameters bind.
    private List<Identifier>? awaitBound;

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s12 lexical goals, JSD-0083 SJL-13.2c parser; IP=Low; Security=Medium; Resources=2; Fingerprint=4A46A2
    // Broiler-Human:        PENDING
    private Parser(ParseRequest request)
    {
        this.request = request;
        source = request.Source;
        text = source.Text;
        var goal = request.Goal;
        var lexical = new EcmaScriptLexical(new UnicodeProperties(request.UnicodeTables));
        tokenizer = new Tokenizer(
            source,
            lexical,
            request.Switches.HtmlLikeComments,
            request.Extensions,
            hashbang: goal is ParseGoal.Script or ParseGoal.Module or ParseGoal.Eval,
            startsAtLineStart: goal != ParseGoal.FormalParameters);
        maximumNesting = request.Limits.NestingDepth.Maximum;
        annexB = request.Switches.AnnexB;
        proposals = request.Proposals;
        isModule = goal == ParseGoal.Module;
        strict = isModule || request.Switches.ForcedStrict;
    }

    /// <summary>Parses <paramref name="request"/>'s source under its goal.</summary>
    /// <returns>The accepted tree with the token stream the parse read, or the first refusal (decision D11).</returns>
    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D11 first refusal; IP=Low; Security=High; Resources=2; Fingerprint=541F16
    // Broiler-Falsified-If: a refusal escapes as an exception other than the parse's own, or a refused parse answers a tree
    // Broiler-Human:        PENDING
    internal static ParseResult Run(ParseRequest request)
    {
        var parser = new Parser(request);
        ProgramNode program;
        try
        {
            program = parser.ParseProgram();
        }
        catch (ParseAbort abort)
        {
            // The early errors of the top-level statements the parse completed before the grammar's refusal come first, in
            // their order; the refusal follows them (decision D11).
            var earlier = Semantics.DeclarationsWalk.RunPartial(request, parser.topLevel ?? new List<Statement>(), abort.Diagnostic.Span.Offset);
            earlier.Add(abort.Diagnostic);
            return ParseResult.Refused(Limit(request, earlier));
        }

        var walk = Semantics.DeclarationsWalk.Run(request, program);
        if (walk.Diagnostics.Count > 0)
            return ParseResult.Refused(Limit(request, walk.Diagnostics));
        return ParseResult.Accepted(program, parser.tokens, request.RetainTokens, walk.Tables);
    }

    /// <summary>
    /// The diagnostics a request asks for, of those found in the library's order (decision D11): the first under
    /// <see cref="ErrorReporting.FirstError"/>; all, up to the request's cap, under <see cref="ErrorReporting.AllErrors"/>.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 early errors at the parse, catalogue rows, JSD-0083 D11 D12; IP=Low; Security=Medium; Resources=1; Fingerprint=F710F0
    // Broiler-Human:        PENDING
    private static List<SyntaxDiagnostic> Limit(ParseRequest request, List<SyntaxDiagnostic> found)
    {
        var count = request.Limits.Reporting == ErrorReporting.FirstError ? 1 : Math.Min(found.Count, request.Limits.DiagnosticCap);
        if (found.Count > count)
            found.RemoveRange(count, found.Count - count);
        return found;
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Tokens.

    /// <summary>The current token, read under <paramref name="goal"/>: read again from its start when it was read under a goal that reads its first code unit otherwise.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s12 goal symbols named by the parser; IP=Low; Security=High; Resources=2; Fingerprint=E85EA6
    // Broiler-Falsified-If: a token is answered under another goal than the one the parser names
    // Broiler-Human:        PENDING
    private Token Peek(LexicalGoal goal)
    {
        if (hasCur)
        {
            if (curGoal == goal || !GoalMatters(cur, curGoal, goal))
                return cur;
            tokenizer.Restore(curState);
        }

        curState = tokenizer.Save();
        cur = tokenizer.Next(goal);
        curGoal = goal;
        hasCur = true;
        return cur;
    }

    /// <summary>Whether the token's first code unit begins another input element under <paramref name="wanted"/> than under <paramref name="read"/>: a <c>/</c> or a <c>}</c>.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s12 lexical goals, JSD-0083 SJL-13.2c parser; IP=Low; Security=Medium; Resources=2; Fingerprint=CE1EC4
    // Broiler-Human:        PENDING
    private bool GoalMatters(Token token, LexicalGoal read, LexicalGoal wanted)
    {
        if (token.Start >= text.Length)
            return false;
        var c = text[token.Start];
        if (c == '/')
            return IsRegExpGoal(read) != IsRegExpGoal(wanted);
        if (c == '}')
            return IsTemplateGoal(read) != IsTemplateGoal(wanted);
        return false;
    }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s12 lexical goals, JSD-0083 SJL-13.2c parser; IP=Low; Security=Medium; Resources=2; Fingerprint=112B6E
    // Broiler-Human:        PENDING
    private static bool IsRegExpGoal(LexicalGoal goal) => goal is LexicalGoal.RegExp or LexicalGoal.RegExpOrTemplateTail or LexicalGoal.HashbangOrRegExp;

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s12 lexical goals, JSD-0083 SJL-13.2c parser; IP=Low; Security=Medium; Resources=2; Fingerprint=A10664
    // Broiler-Human:        PENDING
    private static bool IsTemplateGoal(LexicalGoal goal) => goal is LexicalGoal.TemplateTail or LexicalGoal.RegExpOrTemplateTail;

    /// <summary>The current token where an operator may follow (InputElementDiv).</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s12 lexical goals, JSD-0083 SJL-13.2c parser; IP=Low; Security=Medium; Resources=2; Fingerprint=A05EA0
    // Broiler-Human:        PENDING
    private Token Op => Peek(LexicalGoal.Div);

    /// <summary>The current token where an expression or a statement may begin (InputElementRegExp).</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s12 lexical goals, JSD-0083 SJL-13.2c parser; IP=Low; Security=Medium; Resources=2; Fingerprint=2C9F68
    // Broiler-Human:        PENDING
    private Token Ex => Peek(LexicalGoal.RegExp);

    /// <summary>The token after the current one, read under <paramref name="goal"/> and then forgotten: a lexical lookahead, which builds nothing.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s12 lexical goals, JSD-0083 SJL-13.2c parser; IP=Low; Security=Medium; Resources=2; Fingerprint=2FF458
    // Broiler-Human:        PENDING
    private Token PeekAfter(LexicalGoal currentGoal, LexicalGoal goal)
    {
        Peek(currentGoal);
        var state = tokenizer.Save();
        var next = tokenizer.Next(goal);
        tokenizer.Restore(state);
        return next;
    }

    /// <summary>Consumes the current token, which the caller has read; refuses it when the tokenizer refused the text there.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s12 lexical goals, JSD-0083 SJL-13.2c parser; IP=Low; Security=Medium; Resources=2; Fingerprint=FFAAC3
    // Broiler-Human:        PENDING
    private Token Advance()
    {
        if (!hasCur)
            Peek(LexicalGoal.Div);
        var token = cur;
        if (token.Kind == TokenKind.Invalid)
            throw new ParseAbort(tokenizer.Diagnostic!);
        tokens.Add(token);
        previous = token;
        hasPrevious = true;
        hasCur = false;
        return token;
    }

    /// <summary>Whether the token is the word <paramref name="word"/> written without an escape, the only spelling a keyword or a contextual keyword has (clause 12.7.2).</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s12 lexical goals, JSD-0083 SJL-13.2c parser; IP=Low; Security=Medium; Resources=2; Fingerprint=146D39
    // Broiler-Human:        PENDING
    private static bool IsWord(Token token, Word word) => token.Kind == TokenKind.IdentifierName && token.Word == word && !token.IsEscaped;

    /// <summary>Consumes the punctuator <paramref name="kind"/>, read under <paramref name="goal"/>, or refuses its absence.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s12 lexical goals, JSD-0083 SJL-13.2c parser; IP=Low; Security=Medium; Resources=2; Fingerprint=2DEEA9
    // Broiler-Human:        PENDING
    private Token Expect(TokenKind kind, LexicalGoal goal = LexicalGoal.Div)
    {
        var token = Peek(goal);
        if (token.Kind != kind)
            throw Missing(token, Spelling(kind));
        return Advance();
    }

    /// <summary>Consumes the keyword <paramref name="word"/>, or refuses its absence.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s12 lexical goals, JSD-0083 SJL-13.2c parser; IP=Low; Security=Medium; Resources=2; Fingerprint=CA8CD8
    // Broiler-Human:        PENDING
    private Token ExpectWord(Word word, string spelling, LexicalGoal goal = LexicalGoal.Div)
    {
        var token = Peek(goal);
        if (!IsWord(token, word))
            throw Missing(token, spelling);
        return Advance();
    }

    /// <summary>Consumes the punctuator <paramref name="kind"/> when it is the current token.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s12 lexical goals, JSD-0083 SJL-13.2c parser; IP=Low; Security=Medium; Resources=2; Fingerprint=CE1A83
    // Broiler-Human:        PENDING
    private bool Eat(TokenKind kind, LexicalGoal goal = LexicalGoal.Div)
    {
        if (Peek(goal).Kind != kind)
            return false;
        Advance();
        return true;
    }

    /// <summary>
    /// The end of a statement that ends with a semicolon (clause 12.10): the <c>;</c>, or one automatic semicolon insertion
    /// supplies before a <c>}</c>, at the end of the input, or before a token a line terminator precedes.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s12.10 automatic semicolon insertion; IP=Low; Security=High; Resources=2; Fingerprint=AD94B2
    // Broiler-Falsified-If: a semicolon is inserted where no rule of clause 12.10 inserts one, or refused where one does
    // Broiler-Human:        PENDING
    private void ConsumeSemicolon()
    {
        var token = Op;
        if (token.Kind == TokenKind.Semicolon)
        {
            Advance();
            return;
        }

        if (token.Kind is TokenKind.RightBrace or TokenKind.EndOfInput || token.NewLineBefore)
            return;

        if (token.Kind == TokenKind.Invalid)
            Advance();
        throw Missing(token, ";");
    }

    /// <summary>Whether automatic semicolon insertion could end a statement before the current token: a <c>;</c>, a <c>}</c>, the end, or a line terminator before it.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s12 lexical goals, JSD-0083 SJL-13.2c parser; IP=Low; Security=Medium; Resources=2; Fingerprint=99BE9B
    // Broiler-Human:        PENDING
    private bool AtStatementEnd(Token token) =>
        token.Kind is TokenKind.Semicolon or TokenKind.RightBrace or TokenKind.EndOfInput || token.NewLineBefore;

    // ---------------------------------------------------------------------------------------------------------------
    // Spans.

    /// <summary>Where a token begins, as a node's span begins (decision D15).</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s12 lexical goals, JSD-0083 SJL-13.2c parser; IP=Low; Security=Medium; Resources=2; Fingerprint=0B6202
    // Broiler-Human:        PENDING
    private SourceSpan StartOf(Token token) => new(source.BaseOffset + token.Start, 0, token.Line, token.Column);

    /// <summary>A span from <paramref name="start"/> to the end of the last token consumed: a node's full span.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s12 lexical goals, JSD-0083 SJL-13.2c parser; IP=Low; Security=Medium; Resources=2; Fingerprint=6ABA32
    // Broiler-Human:        PENDING
    private SourceSpan SpanFrom(SourceSpan start)
    {
        var end = hasPrevious ? source.BaseOffset + previous.End : start.Offset;
        return new SourceSpan(start.Offset, Math.Max(0, end - start.Offset), start.Line, start.Column);
    }

    /// <summary>A token's own span.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s12 lexical goals, JSD-0083 SJL-13.2c parser; IP=Low; Security=Medium; Resources=2; Fingerprint=FB148C
    // Broiler-Human:        PENDING
    private SourceSpan SpanOf(Token token) => new(source.BaseOffset + token.Start, token.Length, token.Line, token.Column);

    // ---------------------------------------------------------------------------------------------------------------
    // Refusals.

    /// <summary>A refusal of <paramref name="token"/>, which no production admits where it stands (EX0008, or the row an <c>@</c> or an HTML-like comment serves).</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s12 lexical goals, JSD-0083 SJL-13.2c parser; IP=Low; Security=Medium; Resources=2; Fingerprint=AFC5A1
    // Broiler-Human:        PENDING
    private ParseAbort Unexpected(Token token)
    {
        if (token.Kind == TokenKind.Invalid)
            return new ParseAbort(tokenizer.Diagnostic!);

        // An `@` no production admits where it stands begins a decorator, whose proposal the request does not admit there
        // (catalogue row EP0009, decision D17). With the proposal admitted, an `@` where no decorator may stand is the
        // grammar's plain refusal.
        if (token.Kind == TokenKind.At && (proposals & ProposalFlags.Decorators) == 0)
            return Refuse(Rows.EP0009, SpanOf(token), "@");

        // An HTML-like comment, which the Module goal does not admit (catalogue row EG0001): `<!--` anywhere, or `-->` where
        // a line begins.
        if (isModule && IsHtmlLikeComment(token))
            return Refuse(Rows.EG0001, SpanOf(token), Spelling(token));
        return Refuse(UnexpectedToken, SpanOf(token), Spelling(token));
    }

    /// <summary>A refusal where <paramref name="expected"/> is required and <paramref name="found"/> stands (EX0009).</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s12 lexical goals, JSD-0083 SJL-13.2c parser; IP=Low; Security=Medium; Resources=2; Fingerprint=88A297
    // Broiler-Human:        PENDING
    private ParseAbort Missing(Token found, string expected)
    {
        if (found.Kind == TokenKind.Invalid)
            return new ParseAbort(tokenizer.Diagnostic!);
        return Refuse(MissingToken, SpanOf(found), expected);
    }

    /// <summary>Whether the token begins an HTML-like comment: <c>&lt;!--</c> anywhere, or <c>--&gt;</c> where a line begins (clause B.1.1).</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s12 lexical goals, JSD-0083 SJL-13.2c parser; IP=Low; Security=Medium; Resources=2; Fingerprint=B7AC3E
    // Broiler-Human:        PENDING
    private bool IsHtmlLikeComment(Token token)
    {
        var at = token.Start;
        if (string.CompareOrdinal(text, at, "<!--", 0, 4) == 0)
            return true;
        for (var start = Math.Max(0, at - 2); start <= at; start++)
        {
            if (string.CompareOrdinal(text, start, "-->", 0, 3) != 0)
                continue;
            var i = start - 1;
            while (i >= 0 && text[i] is ' ' or '\t')
                i--;
            if (i < 0 || text[i] is '\n' or '\r' or '\u2028' or '\u2029')
                return true;
        }

        return false;
    }

    /// <summary>A refusal of a node the grammar does not admit where it stands, at its span: by the row of the cover being converted, or EX0008.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 early errors at the parse, catalogue rows, JSD-0083 D11 D12; IP=Low; Security=Medium; Resources=1; Fingerprint=CF64FB
    // Broiler-Human:        PENDING
    private ParseAbort UnexpectedAt(SourceSpan span) => Refuse(coverRule ?? UnexpectedToken, span, string.Empty);

    /// <summary>Runs a cover's conversion under the row its refusals are made by (stage 4d; Broiler.VM's JSD-0083, decision D12).</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 early errors at the parse, catalogue rows, JSD-0083 D11 D12; IP=Low; Security=Medium; Resources=1; Fingerprint=383E3E
    // Broiler-Human:        PENDING
    private T Converting<T>(LexicalRefusal rule, Func<T> convert)
    {
        var saved = coverRule;
        coverRule = rule;
        try
        {
            return convert();
        }
        finally
        {
            coverRule = saved;
        }
    }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s12 lexical goals, JSD-0083 SJL-13.2c parser; IP=Low; Security=Medium; Resources=2; Fingerprint=917DA8
    // Broiler-Human:        PENDING
    private ParseAbort Refuse(LexicalRefusal rule, SourceSpan span, string argument) =>
        new(new SyntaxDiagnostic(rule.Code, rule.MessageId, argument.Length == 0 ? Array.Empty<string>() : new[] { argument }, DiagnosticPhase.Parser, span, source.Name));

    /// <summary>A token's spelling, for a diagnostic's argument: its text, at most 64 code units, empty at the end of the input.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s12 lexical goals, JSD-0083 SJL-13.2c parser; IP=Low; Security=Medium; Resources=2; Fingerprint=CDF76F
    // Broiler-Human:        PENDING
    private string Spelling(Token token) =>
        token.Kind == TokenKind.EndOfInput ? string.Empty : text.Substring(token.Start, Math.Min(token.Length, 64));

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s12 lexical goals, JSD-0083 SJL-13.2c parser; IP=Low; Security=Medium; Resources=2; Fingerprint=F7523D
    // Broiler-Human:        PENDING
    private static string Spelling(TokenKind kind) => kind switch
    {
        TokenKind.LeftBrace => "{",
        TokenKind.RightBrace => "}",
        TokenKind.LeftParen => "(",
        TokenKind.RightParen => ")",
        TokenKind.LeftBracket => "[",
        TokenKind.RightBracket => "]",
        TokenKind.Semicolon => ";",
        TokenKind.Comma => ",",
        TokenKind.Colon => ":",
        TokenKind.Arrow => "=>",
        TokenKind.Equals => "=",
        TokenKind.Dot => ".",
        TokenKind.Star => "*",
        _ => "token",
    };

    // ---------------------------------------------------------------------------------------------------------------
    // The limits (decision D14), checked before the stack is.

    /// <summary>Charges one level of nesting, refusing the source past the request's bound (catalogue row EX0011).</summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D14 nesting bound, JSD-0080 D27; IP=Low; Security=High; Resources=2; Fingerprint=68FB67
    // Broiler-Falsified-If: a recursion the nesting bound charges reaches the stack past the request's bound, or a source within the bound is refused
    // Broiler-Human:        PENDING
    private void EnterNesting(Token at)
    {
        nesting++;
        if (nesting > maximumNesting)
            throw Refuse(NestingTooDeep, SpanOf(at), maximumNesting.ToString(NumberFormatInfo.InvariantInfo));
    }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s12 lexical goals, JSD-0083 SJL-13.2c parser; IP=Low; Security=Medium; Resources=2; Fingerprint=BF42F8
    // Broiler-Human:        PENDING
    private void ExitNesting() => nesting--;

    /// <summary>
    /// Charges a chain node: its height is one more than its highest child's (<see cref="HeightOf"/>); past the library's
    /// constant the source is refused (catalogue row EX0012) before the node is used.
    /// </summary>
    /// <remarks>
    /// Every child counts, not the left operand's spine alone (decision D14, as the owner decided on 2026-11-02): a right
    /// operand, a call's argument, a computed key, a template's substitution, and through a prefix operator, an array or
    /// object literal, a sequence, a conditional or a function the chains inside them, so that a tree deeper than the
    /// constant is refused wherever it is built. The check is the node's height alone: the parse builds a tree from its
    /// leaves, so every chain node of a tree deeper than the constant has a descendant, or is one, whose height passes it
    /// first, and a tree within the constant, however its chains stand beside or below one another, is accepted.
    /// <i>(Revised 2026-11-02, at stage SJL-13.2f; it was <c>Deepen</c>, "Charges one level of tree depth for a chain
    /// node, refusing the source past the library's constant (catalogue row EX0012).", which added one to the depth
    /// whatever the node's right operand or arguments held, and which charged a chain from the depth the parse had reached
    /// when the chain began, so that a right operand or an argument read inside a chain was charged that depth and its own
    /// together and a tree within the constant could be refused.)</i>
    /// </remarks>
    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D14 tree-depth constant, owner's decision 2026-11-02; IP=Low; Security=High; Resources=2; Fingerprint=3FA06F
    // Broiler-Falsified-If: a chain node deeper than ParseLimits.MaximumTreeDepth is built rather than refused, a node's height misses a child's, or a tree within the constant is refused
    // Broiler-Human:        PENDING
    private void Charge(SyntaxNode node, SourceSpan at)
    {
        var height = ChildrenHeight(node) + 1;
        node.Height = height;
        if (height > ParseLimits.MaximumTreeDepth)
            throw Refuse(TreeTooDeep, at, ParseLimits.MaximumTreeDepth.ToString(NumberFormatInfo.InvariantInfo));
    }

    // The scratch lists of the height computation, which keeps its own stack (no recursion per level of the tree).
    private readonly List<SyntaxNode> heightChildren = new();
    private readonly List<SyntaxNode> heightOrder = new();
    private readonly List<SyntaxNode> heightStack = new();
    private readonly List<SyntaxNode> chainChildren = new();

    /// <summary>The highest of a node's children's heights (<see cref="HeightOf"/>), 0 for a node with no child.</summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D14 tree-depth constant, owner's decision 2026-11-02; IP=Low; Security=Medium; Resources=1; Fingerprint=BD6764
    // Broiler-Human:        PENDING
    private int ChildrenHeight(SyntaxNode node)
    {
        chainChildren.Clear();
        node.AddChildren(chainChildren);
        var height = 0;
        for (var i = 0; i < chainChildren.Count; i++)
            height = Math.Max(height, HeightOf(chainChildren[i]));
        chainChildren.Clear();
        return height;
    }

    /// <summary>
    /// A node's height in tree depth (<see cref="SyntaxNode.Height"/>): set already for a chain node the parser charged;
    /// otherwise computed once, from its children's, with an explicit stack, and kept on the node, so that the whole parse
    /// computes each node's height at most once and adds no frame per level of the tree.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D14 tree-depth constant, owner's decision 2026-11-02; IP=Low; Security=High; Resources=2; Fingerprint=DD2A62
    // Broiler-Falsified-If: a node's height differs from the most chain nodes on a path below it, or its computation recurses per level of the tree
    // Broiler-Human:        PENDING
    private int HeightOf(SyntaxNode node)
    {
        if (node.Height >= 0)
            return node.Height;

        // The nodes whose height is not known, parents before their children; then each from its children's, in reverse.
        heightOrder.Clear();
        heightStack.Clear();
        heightStack.Add(node);
        while (heightStack.Count > 0)
        {
            var next = heightStack[^1];
            heightStack.RemoveAt(heightStack.Count - 1);
            heightOrder.Add(next);
            heightChildren.Clear();
            next.AddChildren(heightChildren);
            foreach (var child in heightChildren)
            {
                if (child.Height < 0)
                    heightStack.Add(child);
            }
        }

        for (var i = heightOrder.Count - 1; i >= 0; i--)
        {
            var current = heightOrder[i];
            heightChildren.Clear();
            current.AddChildren(heightChildren);
            var height = 0;
            foreach (var child in heightChildren)
                height = Math.Max(height, child.Height);
            current.Height = IsChainNode(current) ? height + 1 : height;
        }

        heightChildren.Clear();
        heightOrder.Clear();
        return node.Height;
    }

    /// <summary>Whether a node is one a chain adds, which the tree depth counts: a binary or logical operator's, a member's, a call's or a tagged template's.</summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D14 tree-depth constant; IP=Low; Security=Low; Resources=0; Fingerprint=83F3D4
    // Broiler-Human:        PENDING
    private static bool IsChainNode(SyntaxNode node) => node.Kind is SyntaxKind.BinaryExpression or SyntaxKind.LogicalExpression
        or SyntaxKind.MemberExpression or SyntaxKind.CallExpression or SyntaxKind.TaggedTemplateExpression;

    /// <summary>A refusal the parse unwinds with: the first in the library's order (decision D11).</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s12 lexical goals, JSD-0083 SJL-13.2c parser; IP=Low; Security=Medium; Resources=2; Fingerprint=E526A5
    // Broiler-Human:        PENDING
    private sealed class ParseAbort(SyntaxDiagnostic diagnostic) : Exception
    {
        internal SyntaxDiagnostic Diagnostic { get; } = diagnostic;
    }
}
