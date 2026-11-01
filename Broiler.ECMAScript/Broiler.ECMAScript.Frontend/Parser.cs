// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0

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
/// <b>Only the grammar is the parser's</b>: the static-semantic early errors are stage 4d's, so a source the grammar admits
/// is accepted here even where an early error refuses it. A refusal is a <see cref="SyntaxDiagnostic"/> of phase
/// <see cref="DiagnosticPhase.Parser"/> with one of two <b>provisional</b> codes, <see cref="UnexpectedToken"/> and
/// <see cref="MissingToken"/>, which stage 4d replaces by the catalogue row each site serves; the contract's two limits
/// carry their catalogue rows' codes (EX0011 and EX0012), and a lexical refusal the tokenizer's.
/// </para>
/// <para>
/// <b>Limits before the stack</b> (decision D14; JSD-0080's decision D27): the nesting depth is charged once for each
/// statement, assignment expression, binding pattern, recursing prefix operator and nested <c>new</c>, as Broiler.VM's
/// <c>JsParser</c> charges it, and for each right operand of <c>**</c>, the one right-recursive binary operator, which
/// Broiler.VM's parser leaves uncharged; the tree depth is charged once for each node a binary chain or a member or call
/// chain adds, from the spine of the chain's first operand, which closes the left-operand gap in all four of its shapes.
/// </para>
/// </remarks>
internal sealed partial class Parser
{
    /// <summary>The provisional code of a token no production admits where it stands (catalogue row EX0008's code, until stage 4d assigns each site its row).</summary>
    internal static readonly LexicalRefusal UnexpectedToken = new("JSE9108", "syntax.token.unexpected");

    /// <summary>The provisional code of a token a production requires and does not find (catalogue row EX0009's code, until stage 4d assigns each site its row).</summary>
    internal static readonly LexicalRefusal MissingToken = new("JSE9109", "syntax.token.missing");

    /// <summary>The contract's nesting diagnostic (catalogue row EX0011).</summary>
    internal static readonly LexicalRefusal NestingTooDeep = new("JSE9201", "limit.nesting.too-deep");

    /// <summary>The contract's tree-depth diagnostic (catalogue row EX0012).</summary>
    internal static readonly LexicalRefusal TreeTooDeep = new("JSE9202", "limit.tree.too-deep");

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
    private int treeDepth;

    // The object literals holding a CoverInitializedName that no conversion has taken yet, in source order.
    private readonly List<ObjectLiteral> coverInitialized = new();
    private readonly Dictionary<ObjectLiteral, Token> coverInitializedAt = new(ReferenceEqualityComparer.Instance);

    // The cover the primary expression just read, which an arrow's => may convert: a parenthesised list, or an async call.
    private Expression? arrowCover;

    // An arrow function the primary expression read whole (async x => ...), which only an AssignmentExpression may be.
    private Expression? primaryArrow;

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
    internal static ParseResult Run(ParseRequest request)
    {
        var parser = new Parser(request);
        try
        {
            var program = parser.ParseProgram();
            return ParseResult.Accepted(program, parser.tokens);
        }
        catch (ParseAbort abort)
        {
            return ParseResult.Refused(abort.Diagnostic);
        }
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Tokens.

    /// <summary>The current token, read under <paramref name="goal"/>: read again from its start when it was read under a goal that reads its first code unit otherwise.</summary>
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

    private static bool IsRegExpGoal(LexicalGoal goal) => goal is LexicalGoal.RegExp or LexicalGoal.RegExpOrTemplateTail or LexicalGoal.HashbangOrRegExp;

    private static bool IsTemplateGoal(LexicalGoal goal) => goal is LexicalGoal.TemplateTail or LexicalGoal.RegExpOrTemplateTail;

    /// <summary>The current token where an operator may follow (InputElementDiv).</summary>
    private Token Op => Peek(LexicalGoal.Div);

    /// <summary>The current token where an expression or a statement may begin (InputElementRegExp).</summary>
    private Token Ex => Peek(LexicalGoal.RegExp);

    /// <summary>The token after the current one, read under <paramref name="goal"/> and then forgotten: a lexical lookahead, which builds nothing.</summary>
    private Token PeekAfter(LexicalGoal currentGoal, LexicalGoal goal)
    {
        Peek(currentGoal);
        var state = tokenizer.Save();
        var next = tokenizer.Next(goal);
        tokenizer.Restore(state);
        return next;
    }

    /// <summary>Consumes the current token, which the caller has read; refuses it when the tokenizer refused the text there.</summary>
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
    private static bool IsWord(Token token, Word word) => token.Kind == TokenKind.IdentifierName && token.Word == word && !token.IsEscaped;

    /// <summary>Consumes the punctuator <paramref name="kind"/>, read under <paramref name="goal"/>, or refuses its absence.</summary>
    private Token Expect(TokenKind kind, LexicalGoal goal = LexicalGoal.Div)
    {
        var token = Peek(goal);
        if (token.Kind != kind)
            throw Missing(token, Spelling(kind));
        return Advance();
    }

    /// <summary>Consumes the keyword <paramref name="word"/>, or refuses its absence.</summary>
    private Token ExpectWord(Word word, string spelling, LexicalGoal goal = LexicalGoal.Div)
    {
        var token = Peek(goal);
        if (!IsWord(token, word))
            throw Missing(token, spelling);
        return Advance();
    }

    /// <summary>Consumes the punctuator <paramref name="kind"/> when it is the current token.</summary>
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
    private bool AtStatementEnd(Token token) =>
        token.Kind is TokenKind.Semicolon or TokenKind.RightBrace or TokenKind.EndOfInput || token.NewLineBefore;

    // ---------------------------------------------------------------------------------------------------------------
    // Spans.

    /// <summary>Where a token begins, as a node's span begins (decision D15).</summary>
    private SourceSpan StartOf(Token token) => new(source.BaseOffset + token.Start, 0, token.Line, token.Column);

    /// <summary>A span from <paramref name="start"/> to the end of the last token consumed: a node's full span.</summary>
    private SourceSpan SpanFrom(SourceSpan start)
    {
        var end = hasPrevious ? source.BaseOffset + previous.End : start.Offset;
        return new SourceSpan(start.Offset, Math.Max(0, end - start.Offset), start.Line, start.Column);
    }

    /// <summary>A token's own span.</summary>
    private SourceSpan SpanOf(Token token) => new(source.BaseOffset + token.Start, token.Length, token.Line, token.Column);

    // ---------------------------------------------------------------------------------------------------------------
    // Refusals.

    /// <summary>A refusal of <paramref name="token"/>, which no production admits where it stands (provisional code).</summary>
    private ParseAbort Unexpected(Token token)
    {
        if (token.Kind == TokenKind.Invalid)
            return new ParseAbort(tokenizer.Diagnostic!);
        return Refuse(UnexpectedToken, SpanOf(token), Spelling(token));
    }

    /// <summary>A refusal where <paramref name="expected"/> is required and <paramref name="found"/> stands (provisional code).</summary>
    private ParseAbort Missing(Token found, string expected)
    {
        if (found.Kind == TokenKind.Invalid)
            return new ParseAbort(tokenizer.Diagnostic!);
        return Refuse(MissingToken, SpanOf(found), expected);
    }

    /// <summary>A refusal of a node the grammar does not admit where it stands, at its span (provisional code).</summary>
    private ParseAbort UnexpectedAt(SourceSpan span) => Refuse(UnexpectedToken, span, string.Empty);

    private ParseAbort Refuse(LexicalRefusal rule, SourceSpan span, string argument) =>
        new(new SyntaxDiagnostic(rule.Code, rule.MessageId, argument.Length == 0 ? Array.Empty<string>() : new[] { argument }, DiagnosticPhase.Parser, span, source.Name));

    /// <summary>A token's spelling, for a provisional diagnostic's argument: its text, empty at the end of the input.</summary>
    private string Spelling(Token token) =>
        token.Kind == TokenKind.EndOfInput ? string.Empty : text.Substring(token.Start, Math.Min(token.Length, 64));

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
    private void EnterNesting(Token at)
    {
        nesting++;
        if (nesting > maximumNesting)
            throw Refuse(NestingTooDeep, SpanOf(at), maximumNesting.ToString(NumberFormatInfo.InvariantInfo));
    }

    private void ExitNesting() => nesting--;

    /// <summary>Charges one level of tree depth for a chain node, refusing the source past the library's constant (catalogue row EX0012).</summary>
    private void Deepen(SourceSpan at)
    {
        treeDepth++;
        if (treeDepth > ParseLimits.MaximumTreeDepth)
            throw Refuse(TreeTooDeep, at, ParseLimits.MaximumTreeDepth.ToString(NumberFormatInfo.InvariantInfo));
    }

    /// <summary>A refusal the parse unwinds with: the first in the library's order (decision D11).</summary>
    private sealed class ParseAbort(SyntaxDiagnostic diagnostic) : Exception
    {
        internal SyntaxDiagnostic Diagnostic { get; } = diagnostic;
    }
}
