// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   4
// Annotated:        4/4
// Exempt:           0
// Human-reviewed:   0/4
// IP risk:          Low
// Security risk:    High
// Criteria:         3/3
// Resource impact:  2/10 max
// Unverified:       4
//
// GENERATED - DO NOT EDIT MANUALLY

using System.Collections.Generic;
using Broiler.Unicode.Algorithms;

namespace Broiler.ECMAScript.Syntax;

/// <summary>
/// The parse entry's answer at stage SJL-13.2b, before the parser exists: the request's text tokenized as far as the
/// lexical grammar alone decides it, so that a text no reading of it can tokenize is refused, and every other text is not
/// yet judged.
/// </summary>
/// <remarks>
/// <para>
/// <b>A refusal here is the specification's.</b> The scan asks the tokenizer for each token under a goal it can name
/// without the parser, and stops, answering <see cref="ParseOutcome.NotImplemented"/>, at the first token whose goal only
/// the parser can name. A <c>}</c> continues a template when the innermost brace open at it is a substitution's, which
/// holds in every text the syntactic grammar admits, since braces nest there; a <c>/</c> begins a regular-expression literal
/// where no division can stand (at the start, after a punctuator after which only an expression or a statement begins,
/// and after a template head or middle), and is division where no literal can stand (after a numeric, string, template or
/// regular-expression literal, but for one place in a module: a string literal followed by a line terminator may be an
/// import or export declaration's <c>ModuleSpecifier</c>, after which automatic semicolon insertion lets the next item
/// begin with a regular-expression literal), and anywhere else the scan stops: after a name, a keyword, <c>)</c>,
/// <c>]</c>, <c>}</c>, <c>++</c> or <c>--</c>, and at that place in a module. Every other code unit begins the same input element under every goal. So where the
/// scan meets a lexical refusal, either the parse reads the text as the scan did and meets the same refusal, or the text
/// was refused by the syntactic grammar before it: the outcome is a refusal either way. Only the diagnostic may differ
/// from the one the parser will report first (stage 4c onwards), which is why the result is recorded and not judged.
/// </para>
/// <para>
/// <b>The goal is the request's</b>: a Script, a Module or eval code may begin with a <c>HashbangComment</c>; a module has
/// no HTML-like comments (the request refuses the switch for it); a dynamic function's parameters do not start a line for
/// <c>--&gt;</c>, and its body does, since CreateDynamicFunction parses it after a line feed (clause 20.2.1.1.1).
/// </para>
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s12 goal symbols, JSD-0083 SJL-13.2b; IP=Low; Security=Medium; Resources=1; Fingerprint=051437
// Broiler-Human:        PENDING
internal static class LexicalScan
{
    /// <summary>Tokenizes <paramref name="request"/>'s text as far as the lexical grammar decides it alone.</summary>
    /// <returns>A refusal with the tokenizer's diagnostic, or <see cref="ParseOutcome.NotImplemented"/>.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s12 goal symbols, JSD-0083 SJL-13.2b lexical answer; IP=Low; Security=High; Resources=2; Fingerprint=E408D5
    // Broiler-Falsified-If: a source the specification accepts under the request is answered Refused, or a goal is named from the scan's reading where the parser could name another one that tokenizes the text
    // Broiler-Human:        PENDING
    internal static ParseResult Run(ParseRequest request)
    {
        var lexical = new EcmaScriptLexical(new UnicodeProperties(request.UnicodeTables));
        var goal = request.Goal;
        var tokenizer = new Tokenizer(
            request.Source,
            lexical,
            request.Switches.HtmlLikeComments,
            request.Extensions,
            hashbang: goal is ParseGoal.Script or ParseGoal.Module or ParseGoal.Eval,
            startsAtLineStart: goal != ParseGoal.FormalParameters);

        // Each open brace: true for a template's substitution, false for a `{`.
        var braces = new List<bool>();
        var previous = TokenKind.EndOfInput;
        var first = true;

        while (true)
        {
            var next = tokenizer.PeekTokenStart();
            if (next == -2)
                return ParseResult.Refused(tokenizer.Diagnostic!);

            if (next == -1)
            {
                // The end of the input: refused only where it ends a template's substitution.
                return tokenizer.Next(LexicalGoal.Div).Kind == TokenKind.Invalid
                    ? ParseResult.Refused(tokenizer.Diagnostic!)
                    : ParseResult.NotImplemented();
            }

            LexicalGoal lexicalGoal;
            if (next == '/')
            {
                if (first || OnlyAnExpressionFollows(previous))
                    lexicalGoal = LexicalGoal.RegExp;
                else if (OnlyAnOperatorFollows(previous, goal, tokenizer.NewLinePending))
                    lexicalGoal = LexicalGoal.Div;
                else
                    return ParseResult.NotImplemented();
            }
            else if (next == '}')
            {
                lexicalGoal = braces.Count > 0 && braces[^1] ? LexicalGoal.TemplateTail : LexicalGoal.Div;
            }
            else
            {
                lexicalGoal = LexicalGoal.Div;
            }

            var token = tokenizer.Next(lexicalGoal);
            if (token.Kind == TokenKind.Invalid)
                return ParseResult.Refused(tokenizer.Diagnostic!);

            switch (token.Kind)
            {
                case TokenKind.LeftBrace:
                    braces.Add(false);
                    break;
                case TokenKind.TemplateHead:
                    braces.Add(true);
                    break;
                case TokenKind.RightBrace or TokenKind.TemplateTail when braces.Count > 0:
                    braces.RemoveAt(braces.Count - 1);
                    break;
            }

            previous = token.Kind;
            first = false;
        }
    }

    /// <summary>
    /// Whether no division can follow <paramref name="kind"/>: a punctuator after which the syntactic grammar admits only
    /// the start of an expression, a statement or nothing at all, or a template's head or middle.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s12 InputElementRegExp contexts; IP=Low; Security=High; Resources=2; Fingerprint=60BE3D
    // Broiler-Falsified-If: a token after which a division can stand in a text the syntactic grammar admits is answered true
    // Broiler-Human:        PENDING
    private static bool OnlyAnExpressionFollows(TokenKind kind) => kind switch
    {
        TokenKind.RightParen or TokenKind.RightBracket or TokenKind.RightBrace or TokenKind.PlusPlus or TokenKind.MinusMinus => false,
        TokenKind.IdentifierName or TokenKind.PrivateIdentifier => false,
        TokenKind.NumericLiteral or TokenKind.StringLiteral or TokenKind.NoSubstitutionTemplate or TokenKind.TemplateTail
            or TokenKind.RegularExpressionLiteral => false,
        TokenKind.EndOfInput or TokenKind.Invalid => false,
        _ => true,
    };

    /// <summary>
    /// Whether no regular-expression literal can follow <paramref name="kind"/> under <paramref name="goal"/>, with
    /// <paramref name="newLine"/> saying whether a line terminator stands before the next token: a literal, after which
    /// only an operator can. In a module, a string literal followed by a line terminator is not one: it may end an
    /// <c>ImportDeclaration</c> or an <c>ExportDeclaration</c> as its <c>ModuleSpecifier</c> (ECMA-262 clauses 16.2.2 and
    /// 16.2.3), where a <c>/</c> is no operator, so automatic semicolon insertion (clause 12.10) ends the declaration and
    /// the next <c>ModuleItem</c> may begin with a regular-expression literal. Without the line terminator no semicolon is
    /// inserted, and a <c>/</c> after a module specifier is refused by the syntactic grammar under either reading.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s12 InputElementDiv contexts, s12.10 ASI, s16.2 ModuleSpecifier; IP=Low; Security=High; Resources=2; Fingerprint=87CAFF
    // Broiler-Falsified-If: a token after which a regular-expression literal can stand in a text the syntactic grammar admits under the goal is answered true
    // Broiler-Human:        PENDING
    private static bool OnlyAnOperatorFollows(TokenKind kind, ParseGoal goal, bool newLine) => kind switch
    {
        TokenKind.StringLiteral => goal != ParseGoal.Module || !newLine,
        TokenKind.NumericLiteral or TokenKind.NoSubstitutionTemplate or TokenKind.TemplateTail or TokenKind.RegularExpressionLiteral => true,
        _ => false,
    };
}
