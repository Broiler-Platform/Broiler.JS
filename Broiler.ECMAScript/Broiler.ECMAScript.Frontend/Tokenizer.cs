// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// HAIRS Code Assurance
// --------------------
// Relevant units:   57
// Annotated:        57/57
// Exempt:           20
// Human-reviewed:   0/57
// IP risk:          Low
// Security risk:    High
// Criteria:         15/15
// Resource impact:  2/10 max
// Unverified:       57
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Text;
using Broiler.ECMAScript.Numbers;
using Broiler.Unicode.Algorithms;

namespace Broiler.ECMAScript.Syntax;

/// <summary>
/// The lexical grammar of ECMA-262 clause 12 over one request's text: a pull tokenizer that reads one input element at a
/// time under the goal its caller names (Broiler.VM's JSD-0083, sub-area T; the plan's stage 4b).
/// </summary>
/// <remarks>
/// <para>
/// <b>The caller chooses the goal; the tokenizer never chooses it from the tokens it has read.</b> Whether a <c>/</c>
/// begins a regular-expression literal, and whether a <c>}</c> continues a template, is the syntactic grammar's question
/// (clause 12's goal symbols), which only the parser can answer. The tokenizer keeps no bracket stack and no previous
/// token; what it carries from one token to the next is its position, the line it is on, whether only trivia stands
/// between the last line terminator and it (for Annex B's <c>--&gt;</c>), whether a line terminator stands before the next
/// token, and the templates whose substitutions are open, for where a template's refusal is anchored.
/// </para>
/// <para>
/// <b>It refuses rather than throws.</b> A text no input element of the goal admits answers a token of kind
/// <see cref="TokenKind.Invalid"/> and a <see cref="Diagnostic"/> with the catalogue's shared code for the lexical rule, at
/// the construct's start, as Broiler.VM's wide front end anchors it (decision D15); every later call answers the same
/// token. The facts the parser rules on later (a legacy octal literal in strict code, a template's
/// <c>NotEscapeSequence</c> when it is not tagged, an escaped reserved word) are recorded on the token, never refused here.
/// </para>
/// <para>
/// <b>Its work is linear in the text</b>: each code unit is read a bounded number of times, numeric literals are valued by
/// <c>NumericLiterals.Evaluate</c> (decision D9) and a BigInt literal's value is not built (an engine builds it under its
/// own ceiling). The parse charges the request's meter for the whole text before the tokenizer reads it
/// (<see cref="ParseRequest.Meter"/>), so the tokenizer charges nothing of its own. It reads no culture: the identifier and
/// white-space classes beyond ASCII are the request's Unicode provider's (decision D8).
/// </para>
/// <para>
/// One tokenizer serves one parse on one thread; <see cref="Save"/> and <see cref="Restore"/> let its parser look ahead.
/// </para>
/// </remarks>
// Hairs-AI:           Origin=AI; Spec=ECMA-262 s12 lexical grammar, JSD-0083 sub-area T; IP=Low; Security=Medium; Resources=2; Fingerprint=81DA3B
// Hairs-Human:        PENDING
internal sealed class Tokenizer
{
    /// <summary>The widest BigInt <c>NumericLiterals.Evaluate</c> is asked to build: none, so that only the literal's form is checked.</summary>
    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D9; IP=Low; Security=Low; Resources=1; Fingerprint=B4141B
    // Hairs-Human:        PENDING
    private const long NoBigIntWidth = 0;

    private readonly SourceText source;
    private readonly string text;
    private readonly EcmaScriptLexical lexical;
    private readonly bool htmlLikeComments;
    private readonly bool decimalSuffix;
    private readonly bool hashbang;

    private int position;
    private int line = 1;
    private int lineStart;
    private bool onlyTriviaOnLine;
    private bool newLine;
    private TemplateFrame? templates;
    private SyntaxDiagnostic? diagnostic;
    private Token refusal;

    /// <summary>Creates a tokenizer over <paramref name="source"/>.</summary>
    /// <param name="source">The text and where it stands.</param>
    /// <param name="lexical">The identifier and white-space classes, over the request's Unicode provider.</param>
    /// <param name="htmlLikeComments">Whether Annex B.1.1's <c>&lt;!--</c> and <c>--&gt;</c> are comments (the request's switch).</param>
    /// <param name="extensions">The request's lexer extensions: the <c>m</c> suffix alone.</param>
    /// <param name="hashbang">Whether the text's first code unit is where a <c>HashbangComment</c> may stand (the start of a Script, a Module or eval code).</param>
    /// <param name="startsAtLineStart">Whether the text's start counts as the start of a line for <c>--&gt;</c> (every goal but a dynamic function's parameters).</param>
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s12, JSD-0083 D8 D17; IP=Low; Security=Medium; Resources=1; Fingerprint=8BAFCA
    // Hairs-Human:        PENDING
    internal Tokenizer(SourceText source, EcmaScriptLexical lexical, bool htmlLikeComments, LexerExtensions extensions, bool hashbang, bool startsAtLineStart)
    {
        this.source = source;
        text = source.Text;
        this.lexical = lexical;
        this.htmlLikeComments = htmlLikeComments;
        decimalSuffix = (extensions & LexerExtensions.DecimalSuffix) != 0;
        this.hashbang = hashbang;
        onlyTriviaOnLine = startsAtLineStart;
    }

    /// <summary>The refusal that ended the token stream, or null while there is none.</summary>
    internal SyntaxDiagnostic? Diagnostic => diagnostic;

    /// <summary>The offset the next token's trivia starts at.</summary>
    internal int Position => position;

    /// <summary>
    /// Whether a line terminator, or a comment holding one, stands between the last token read and the offset
    /// <see cref="PeekTokenStart"/> stopped at: the next token's <see cref="Token.NewLineBefore"/>, known before it is read.
    /// </summary>
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s12.10 automatic semicolon insertion line terminator; IP=Low; Security=High; Resources=1; Fingerprint=941A34
    // Hairs-Falsified-If: it answers otherwise than the next token's NewLineBefore after PeekTokenStart
    // Hairs-Human:        PENDING
    internal bool NewLinePending => newLine;

    /// <summary>The tokenizer's state, for <see cref="Restore"/>.</summary>
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s12 lexical goal lookahead; IP=Low; Security=Medium; Resources=1; Fingerprint=16808E
    // Hairs-Human:        PENDING
    internal TokenizerState Save() => new(position, line, lineStart, onlyTriviaOnLine, newLine, templates, diagnostic, refusal);

    /// <summary>Returns to a state <see cref="Save"/> gave, so that the parser can look ahead and read again under another goal.</summary>
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s12 lexical goal lookahead; IP=Low; Security=Medium; Resources=1; Fingerprint=C10992
    // Hairs-Human:        PENDING
    internal void Restore(TokenizerState state)
    {
        position = state.Position;
        line = state.Line;
        lineStart = state.LineStart;
        onlyTriviaOnLine = state.OnlyTriviaOnLine;
        newLine = state.NewLine;
        templates = state.Templates;
        diagnostic = state.Diagnostic;
        refusal = state.Refusal;
    }

    /// <summary>
    /// Skips the trivia before the next token (white space, line terminators, comments, Annex B's HTML-like comments and,
    /// at the text's start, a hashbang) and answers the code unit the next token starts with: -1 at the end of the input,
    /// and -2 once the trivia, or an earlier token, was refused.
    /// </summary>
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s12 trivia; IP=Low; Security=Medium; Resources=1; Fingerprint=77F678
    // Hairs-Human:        PENDING
    internal int PeekTokenStart()
    {
        if (diagnostic is not null)
            return -2;

        if (!SkipTrivia())
            return -2;

        return position < text.Length ? text[position] : -1;
    }

    /// <summary>Reads the next input element under <paramref name="goal"/> (ECMA-262 clause 12).</summary>
    /// <param name="goal">The lexical goal the parser stands at.</param>
    /// <returns>The token; <see cref="TokenKind.Invalid"/> once the text is refused, with <see cref="Diagnostic"/> saying why.</returns>
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s12 lexical grammar goal symbols; IP=Low; Security=High; Resources=2; Fingerprint=994D5A
    // Hairs-Falsified-If: a token is read under another goal than the caller names, or a token is answered after a refusal
    // Hairs-Human:        PENDING
    internal Token Next(LexicalGoal goal)
    {
        if (diagnostic is not null || !SkipTrivia())
            return refusal;

        var token = ReadToken(goal);
        if (diagnostic is not null)
            return refusal;

        token = token.WithNewLineBefore(newLine);
        newLine = false;
        onlyTriviaOnLine = false;
        return token;
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Trivia: clauses 12.2 (white space), 12.3 (line terminators), 12.4 (comments, with the HashbangComment of 12.5),
    // and Annex B.1.1 (HTML-like comments).

    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s12.2-s12.5 trivia, B.1.1 HTML-like comments; IP=Low; Security=High; Resources=2; Fingerprint=AA07AE
    // Hairs-Falsified-If: a comment or white space the goal admits ends the token stream, a hashbang is skipped anywhere but the first code unit of a text that admits one, or --> is a comment after a token on its line
    // Hairs-Human:        PENDING
    private bool SkipTrivia()
    {
        if (position == 0 && hashbang && text.Length >= 2 && text[0] == '#' && text[1] == '!')
        {
            position = 2;
            while (position < text.Length && !IsLineTerminator(text[position]))
                position++;
        }

        while (position < text.Length)
        {
            var c = text[position];
            if (IsLineTerminator(c))
            {
                newLine = true;
                onlyTriviaOnLine = true;
                AdvanceLine(c);
                continue;
            }

            if (IsWhiteSpace(c))
            {
                position++;
                continue;
            }

            if (c == '/' && position + 1 < text.Length && text[position + 1] == '/')
            {
                SkipToLineEnd(position + 2);
                continue;
            }

            if (c == '/' && position + 1 < text.Length && text[position + 1] == '*')
            {
                if (!SkipMultiLineComment())
                    return false;
                continue;
            }

            if (htmlLikeComments && c == '<' && At(position, "<!--"))
            {
                SkipToLineEnd(position + 4);
                continue;
            }

            if (htmlLikeComments && onlyTriviaOnLine && c == '-' && At(position, "-->"))
            {
                SkipToLineEnd(position + 3);
                continue;
            }

            break;
        }

        return true;
    }

    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s12.4 SingleLineComment; IP=Low; Security=Medium; Resources=1; Fingerprint=83817F
    // Hairs-Human:        PENDING
    private void SkipToLineEnd(int from)
    {
        position = from;
        while (position < text.Length && !IsLineTerminator(text[position]))
            position++;
    }

    /// <summary>A <c>MultiLineComment</c>: one holding a line terminator counts as one (clause 12.4), and is the start of a line for <c>--&gt;</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s12.4 MultiLineComment; IP=Low; Security=High; Resources=2; Fingerprint=BFA651
    // Hairs-Falsified-If: an unterminated comment is admitted, or a comment holding a line terminator is not counted as one
    // Hairs-Human:        PENDING
    private bool SkipMultiLineComment()
    {
        var start = position;
        var startLine = line;
        var startLineStart = lineStart;
        position += 2;
        while (position < text.Length)
        {
            var c = text[position];
            if (c == '*' && position + 1 < text.Length && text[position + 1] == '/')
            {
                position += 2;
                return true;
            }

            if (IsLineTerminator(c))
            {
                newLine = true;
                onlyTriviaOnLine = true;
                AdvanceLine(c);
                continue;
            }

            position++;
        }

        return Refuse(Catalogue.CommentUnterminated, start, startLine, startLineStart);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Input elements.

    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s12 InputElement goal symbols; IP=Low; Security=High; Resources=2; Fingerprint=F693F8
    // Hairs-Falsified-If: a slash under a division goal begins a literal, or a right brace under a template goal is read as a punctuator
    // Hairs-Human:        PENDING
    private Token ReadToken(LexicalGoal goal)
    {
        var start = position;
        if (start >= text.Length)
        {
            // The end of the input inside a template's substitution: the template has no closing backtick before the end of
            // the source (catalogue row EX0007), refused where the template begins, as Broiler.VM's front end anchors it.
            if (templates is { } open)
            {
                Refuse(Catalogue.TemplateUnterminated, open.Start, open.Line, open.LineStart);
                return refusal;
            }

            return new Token(TokenKind.EndOfInput, start, start, AbsoluteLine(line), AbsoluteColumn(start, line, lineStart), TokenFlags.None);
        }

        var c = text[start];
        if (c == '\\' || IsIdentifierStartAt(start, out _))
            return ReadIdentifierName(start, TokenKind.IdentifierName);

        if (c == '#')
        {
            if (start + 1 < text.Length && (text[start + 1] == '\\' || IsIdentifierStartAt(start + 1, out _)))
                return ReadIdentifierName(start, TokenKind.PrivateIdentifier);

            return UnexpectedCharacter(start);
        }

        if (IsAsciiDigit(c) || (c == '.' && start + 1 < text.Length && IsAsciiDigit(text[start + 1])))
            return ReadNumericLiteral(start);

        if (c is '"' or '\'')
            return ReadStringLiteral(start);

        if (c == '`')
            return ReadTemplate(start, opening: true);

        if (c == '}' && goal is LexicalGoal.TemplateTail or LexicalGoal.RegExpOrTemplateTail)
            return ReadTemplate(start, opening: false);

        if (c == '/' && goal is LexicalGoal.RegExp or LexicalGoal.RegExpOrTemplateTail or LexicalGoal.HashbangOrRegExp)
            return ReadRegularExpression(start);

        return ReadPunctuator(start, c);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Names: clause 12.7, IdentifierName and PrivateIdentifier, with the early errors of 12.7.1.1.

    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s12.7 IdentifierName, s12.7.1.1 early errors; IP=Low; Security=High; Resources=2; Fingerprint=05B5D9
    // Hairs-Falsified-If: an escaped code point outside ID_Start or ID_Continue is admitted, or an escaped name and its plain spelling cook to different names
    // Hairs-Human:        PENDING
    private Token ReadIdentifierName(int start, TokenKind kind)
    {
        var startLine = line;
        var startLineStart = lineStart;
        StringBuilder? cooked = null;
        var nameStart = kind == TokenKind.PrivateIdentifier ? start + 1 : start;
        position = nameStart;
        var first = true;

        while (position < text.Length)
        {
            if (text[position] == '\\')
            {
                cooked ??= new StringBuilder().Append(text, start, position - start);
                if (position + 1 >= text.Length || text[position + 1] != 'u')
                {
                    position++;
                    Refuse(Catalogue.UnexpectedCharacter, start, startLine, startLineStart);
                    return refusal;
                }

                position += 2;
                if (!ReadUnicodeEscape(out var codePoint))
                {
                    Refuse(Catalogue.IdentifierEscapeInvalid, start, startLine, startLineStart);
                    return refusal;
                }

                if (first ? !lexical.IsIdentifierStart(codePoint) : !lexical.IsIdentifierPart(codePoint))
                {
                    Refuse(first ? Catalogue.IdentifierEscapeStart : Catalogue.IdentifierEscapePart, start, startLine, startLineStart);
                    return refusal;
                }

                AppendCodePoint(cooked, codePoint);
                first = false;
                continue;
            }

            var cp = CodePointAt(position, out var width);
            if (first ? !lexical.IsIdentifierStart(cp) : !lexical.IsIdentifierPart(cp))
                break;

            cooked?.Append(text, position, width);
            position += width;
            first = false;
        }

        var name = cooked?.ToString() ?? text.Substring(start, position - start);
        var flags = cooked is null ? TokenFlags.None : TokenFlags.Escaped;
        var word = kind == TokenKind.IdentifierName ? Words.Of(name) : Word.None;
        return new Token(kind, start, position, AbsoluteLine(startLine), AbsoluteColumn(start, startLine, startLineStart), flags, name, word: word);
    }

    /// <summary>
    /// After <c>\u</c>: <c>Hex4Digits</c> or <c>{ CodePoint }</c> (clause 12.9.4), the code point any number of leading
    /// zeros may spell up to U+10FFFF. The position is past the escape when it is well formed.
    /// </summary>
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s12.9.4 UnicodeEscapeSequence; IP=Low; Security=High; Resources=2; Fingerprint=AC4AD7
    // Hairs-Falsified-If: an escape with white space, a separator or a non-ASCII digit, or a code point above U+10FFFF, is admitted
    // Hairs-Human:        PENDING
    private bool ReadUnicodeEscape(out int codePoint)
    {
        codePoint = 0;
        if (position < text.Length && text[position] == '{')
        {
            var at = position + 1;
            var digits = 0;
            while (at < text.Length && HexValue(text[at]) is var digit and >= 0)
            {
                codePoint = codePoint > 0x10FFFF ? codePoint : (codePoint * 16) + digit;
                digits++;
                at++;
            }

            if (digits == 0 || at >= text.Length || text[at] != '}' || codePoint > 0x10FFFF)
                return false;

            position = at + 1;
            return true;
        }

        if (position + 4 > text.Length)
            return false;

        for (var i = 0; i < 4; i++)
        {
            var digit = HexValue(text[position + i]);
            if (digit < 0)
                return false;
            codePoint = (codePoint * 16) + digit;
        }

        position += 4;
        return true;
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Numeric literals: clause 12.9.3, scanned by its grammar and valued by NumericLiterals.Evaluate (decision D9).

    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s12.9.3 NumericLiteral, JSD-0083 D9 D17; IP=Low; Security=High; Resources=2; Fingerprint=7FFC6B
    // Hairs-Falsified-If: a text that is no NumericLiteral is admitted, an IdentifierStart or digit touching a literal is admitted, or the m suffix is admitted without the lexer extension
    // Hairs-Human:        PENDING
    private Token ReadNumericLiteral(int start)
    {
        var startLine = line;
        var startLineStart = lineStart;
        var flags = TokenFlags.None;
        var mayTakeSuffix = false;
        var c = text[start];
        position = start;

        if (c == '.')
        {
            position++;
            ScanDigits(10, separators: true);
            ScanExponent();
            mayTakeSuffix = !HasExponent(start);
        }
        else if (c == '0' && start + 1 < text.Length && Radix(text[start + 1]) is var radix and not 0)
        {
            position = start + 2;
            if (!ScanDigits(radix, separators: true))
                return RefuseNumeric(start, startLine, startLineStart);

            if (position < text.Length && text[position] == 'n')
            {
                position++;
                flags |= TokenFlags.BigInt;
            }
        }
        else if (c == '0' && start + 1 < text.Length && IsAsciiDigit(text[start + 1]))
        {
            // A zero followed by digits: a LegacyOctalIntegerLiteral when every digit is an octal digit, and a
            // NonOctalDecimalIntegerLiteral, which takes a fraction and an exponent, when one is 8 or 9. Neither takes a
            // separator or the BigInt suffix.
            position = start + 1;
            var octal = true;
            while (position < text.Length && IsAsciiDigit(text[position]))
            {
                octal &= text[position] <= '7';
                position++;
            }

            if (octal)
            {
                flags |= TokenFlags.LegacyOctal;

                // A legacy octal integer directly followed by a decimal fraction (07.5) is two literals the grammar never
                // admits side by side, which both engines' lexers refuse at the literal (decision D15's anchor).
                if (position + 1 < text.Length && text[position] == '.' && IsAsciiDigit(text[position + 1]))
                    return RefuseNumeric(start, startLine, startLineStart);
            }
            else
            {
                flags |= TokenFlags.NonOctalDecimal;
                ScanFraction();
                ScanExponent();
            }

            mayTakeSuffix = (flags & TokenFlags.LegacyOctal) != 0 || !HasExponent(start);
        }
        else
        {
            if (c == '0')
                position = start + 1;
            else
                ScanDigits(10, separators: true);

            var integer = position;
            ScanFraction();
            ScanExponent();
            if (position == integer && position < text.Length && text[position] == 'n')
            {
                position++;
                flags |= TokenFlags.BigInt;
            }

            mayTakeSuffix = (flags & TokenFlags.BigInt) == 0 && !HasExponent(start);
        }

        var valueEnd = position;
        if (decimalSuffix && mayTakeSuffix && position < text.Length && text[position] == 'm')
        {
            position++;
            flags |= TokenFlags.DecimalSuffix;
        }

        // The SourceCharacter immediately following a NumericLiteral must not be an IdentifierStart or a DecimalDigit
        // (clause 12.9.3). An IdentifierStart spelled as an escape (`1\u0061`) is left to the parser, which refuses the
        // name that follows the literal, as both engines' front ends do, so that the refusal keeps their code and place
        // (decision D15); the outcome is a refusal either way, since no production puts a name directly after a literal.
        if (position < text.Length && (IsAsciiDigit(text[position]) || IsIdentifierStartAt(position, out _)))
            return RefuseNumeric(start, startLine, startLineStart);

        var status = NumericLiterals.Evaluate(text.AsSpan(start, valueEnd - start), strict: false, new LengthLimit(NoBigIntWidth), out var literal, out _);
        var number = double.NaN;
        if ((flags & TokenFlags.BigInt) != 0)
        {
            if (status == ResultStatus.Malformed)
                return RefuseNumeric(start, startLine, startLineStart);
        }
        else
        {
            if (status != ResultStatus.Completed)
                return RefuseNumeric(start, startLine, startLineStart);

            if ((flags & TokenFlags.DecimalSuffix) == 0)
                number = literal.Number;
        }

        return new Token(TokenKind.NumericLiteral, start, position, AbsoluteLine(startLine), AbsoluteColumn(start, startLine, startLineStart), flags, number: number);
    }

    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s12.9.3 ExponentPart; IP=Low; Security=Medium; Resources=1; Fingerprint=DF48D8
    // Hairs-Human:        PENDING
    private bool HasExponent(int start)
    {
        for (var at = start; at < position; at++)
        {
            if (text[at] is 'e' or 'E')
                return true;
        }

        return false;
    }

    /// <summary><c>. DecimalDigits[+Sep]opt</c> after a decimal integer.</summary>
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s12.9.3 DecimalLiteral; IP=Low; Security=Medium; Resources=1; Fingerprint=255E4B
    // Hairs-Human:        PENDING
    private void ScanFraction()
    {
        if (position < text.Length && text[position] == '.')
        {
            position++;
            ScanDigits(10, separators: true);
        }
    }

    /// <summary><c>ExponentPart[+Sep]</c>: taken only when digits follow the indicator and its sign, so that <c>1e</c> leaves the <c>e</c> to refuse.</summary>
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s12.9.3 ExponentPart; IP=Low; Security=Medium; Resources=1; Fingerprint=FD27AA
    // Hairs-Human:        PENDING
    private void ScanExponent()
    {
        if (position >= text.Length || text[position] is not ('e' or 'E'))
            return;

        var at = position + 1;
        if (at < text.Length && text[at] is '+' or '-')
            at++;

        if (at < text.Length && DigitValue(text[at], 10) >= 0)
        {
            position = at;
            ScanDigits(10, separators: true);
        }
    }

    /// <summary>
    /// <c>Digits[+Sep]</c> of a radix from the position: a digit, then digits each after at most one separator that stands
    /// between two digits. Answers whether there was a first digit; a separator not followed by a digit is left unread.
    /// </summary>
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s12.9.3 NumericLiteralSeparator; IP=Low; Security=High; Resources=2; Fingerprint=9AD849
    // Hairs-Falsified-If: a separator not between two digits of the radix is taken into the literal
    // Hairs-Human:        PENDING
    private bool ScanDigits(int radix, bool separators)
    {
        if (position >= text.Length || DigitValue(text[position], radix) < 0)
            return false;

        position++;
        while (position < text.Length)
        {
            if (DigitValue(text[position], radix) >= 0)
                position++;
            else if (separators && text[position] == '_' && position + 1 < text.Length && DigitValue(text[position + 1], radix) >= 0)
                position += 2;
            else
                break;
        }

        return true;
    }

    // Hairs-AI:           Origin=AI; Spec=catalogue EX0003; IP=Low; Security=Medium; Resources=1; Fingerprint=FBF5CF
    // Hairs-Human:        PENDING
    private Token RefuseNumeric(int start, int startLine, int startLineStart)
    {
        Refuse(Catalogue.NumericLiteralInvalid, start, startLine, startLineStart);
        return refusal;
    }

    // ---------------------------------------------------------------------------------------------------------------
    // String literals: clause 12.9.4, with the legacy escapes whose strict-mode refusal (12.9.4.1) is the parser's.

    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s12.9.4 StringLiteral; IP=Low; Security=High; Resources=2; Fingerprint=47058A
    // Hairs-Falsified-If: a raw LF or CR is admitted in a string, an undefined escape is admitted, or a legacy escape is not recorded
    // Hairs-Human:        PENDING
    private Token ReadStringLiteral(int start)
    {
        var startLine = line;
        var startLineStart = lineStart;
        var quote = text[start];
        var value = new StringBuilder();
        var flags = TokenFlags.None;
        position = start + 1;

        while (true)
        {
            if (position >= text.Length || text[position] is '\n' or '\r')
            {
                Refuse(Catalogue.StringUnterminated, start, startLine, startLineStart);
                return refusal;
            }

            var c = text[position];
            if (c == quote)
            {
                position++;
                break;
            }

            if (c is '\u2028' or '\u2029')
            {
                // Since ES2019 a raw LS or PS is a string character, and still a line terminator for positions.
                value.Append(c);
                AdvanceLine(c);
                continue;
            }

            if (c != '\\')
            {
                value.Append(c);
                position++;
                continue;
            }

            flags |= TokenFlags.StringHasEscape;
            position++;
            if (position >= text.Length)
            {
                Refuse(Catalogue.StringUnterminated, start, startLine, startLineStart);
                return refusal;
            }

            var escape = text[position];
            if (IsLineTerminator(escape))
            {
                AdvanceLine(escape);
                continue;
            }

            position++;
            switch (escape)
            {
                case 'b': value.Append('\b'); break;
                case 't': value.Append('\t'); break;
                case 'n': value.Append('\n'); break;
                case 'v': value.Append('\v'); break;
                case 'f': value.Append('\f'); break;
                case 'r': value.Append('\r'); break;
                case >= '0' and <= '7':
                    if (escape == '0' && (position >= text.Length || !IsAsciiDigit(text[position])))
                    {
                        value.Append('\0');
                        break;
                    }

                    // LegacyOctalEscapeSequence: up to three octal digits when the first is 0 to 3, two when it is 4 to 7,
                    // and a lone 0 before an 8 or a 9.
                    flags |= TokenFlags.LegacyOctalEscape;
                    var octal = escape - '0';
                    for (var more = escape <= '3' ? 2 : 1; more > 0 && position < text.Length && text[position] is >= '0' and <= '7'; more--)
                    {
                        octal = (octal * 8) + (text[position] - '0');
                        position++;
                    }

                    value.Append((char)octal);
                    break;
                case '8':
                case '9':
                    flags |= TokenFlags.NonOctalDecimalEscape;
                    value.Append(escape);
                    break;
                case 'x':
                    if (position + 2 > text.Length || HexValue(text[position]) < 0 || HexValue(text[position + 1]) < 0)
                    {
                        Refuse(Catalogue.StringEscapeInvalid, start, startLine, startLineStart);
                        return refusal;
                    }

                    value.Append((char)((HexValue(text[position]) * 16) + HexValue(text[position + 1])));
                    position += 2;
                    break;
                case 'u':
                    if (!ReadUnicodeEscape(out var codePoint))
                    {
                        Refuse(Catalogue.StringEscapeInvalid, start, startLine, startLineStart);
                        return refusal;
                    }

                    AppendCodePoint(value, codePoint);
                    break;
                default:
                    // A NonEscapeCharacter stands for itself.
                    value.Append(escape);
                    break;
            }
        }

        return new Token(TokenKind.StringLiteral, start, position, AbsoluteLine(startLine), AbsoluteColumn(start, startLine, startLineStart), flags, value.ToString());
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Templates: clause 12.9.6, read in place one piece at a time, each with its TV and its TRV.

    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s12.9.6 Template, TV and TRV; IP=Low; Security=High; Resources=2; Fingerprint=22B4D9
    // Hairs-Falsified-If: a template piece ends at a brace or backtick its text escapes, a NotEscapeSequence leaves a cooked value, or CR and CR LF are not LF in both values
    // Hairs-Human:        PENDING
    private Token ReadTemplate(int start, bool opening)
    {
        var startLine = line;
        var startLineStart = lineStart;
        var cooked = new StringBuilder();
        var raw = new StringBuilder();
        var invalidAt = -1;
        position = start + 1;

        while (true)
        {
            if (position >= text.Length)
            {
                var frame = opening ? new TemplateFrame(start, startLine, startLineStart, null) : templates;
                Refuse(Catalogue.TemplateUnterminated, frame?.Start ?? start, frame?.Line ?? startLine, frame?.LineStart ?? startLineStart);
                return refusal;
            }

            var c = text[position];
            if (c == '`')
            {
                position++;
                var kind = opening ? TokenKind.NoSubstitutionTemplate : TokenKind.TemplateTail;
                if (!opening)
                    templates = templates?.Outer;
                return TemplatePiece(kind, start, startLine, startLineStart, cooked, raw, invalidAt);
            }

            if (c == '$' && position + 1 < text.Length && text[position + 1] == '{')
            {
                position += 2;
                if (opening)
                    templates = new TemplateFrame(start, startLine, startLineStart, templates);
                return TemplatePiece(opening ? TokenKind.TemplateHead : TokenKind.TemplateMiddle, start, startLine, startLineStart, cooked, raw, invalidAt);
            }

            if (IsLineTerminator(c))
            {
                // A LineTerminatorSequence: CR and CR LF are LF in both the TV and the TRV (clauses 12.9.6.1 and 12.9.6.2).
                var normalised = c == '\r' ? '\n' : c;
                cooked.Append(normalised);
                raw.Append(normalised);
                AdvanceLine(c);
                continue;
            }

            if (c != '\\')
            {
                cooked.Append(c);
                raw.Append(c);
                position++;
                continue;
            }

            var escapeStart = position;
            raw.Append('\\');
            position++;
            if (position >= text.Length)
                continue;

            var escape = text[position];
            if (IsLineTerminator(escape))
            {
                raw.Append(escape == '\r' ? '\n' : escape);
                AdvanceLine(escape);
                continue;
            }

            raw.Append(escape);
            position++;
            switch (escape)
            {
                case 'b': cooked.Append('\b'); break;
                case 't': cooked.Append('\t'); break;
                case 'n': cooked.Append('\n'); break;
                case 'v': cooked.Append('\v'); break;
                case 'f': cooked.Append('\f'); break;
                case 'r': cooked.Append('\r'); break;
                case '0' when position >= text.Length || !IsAsciiDigit(text[position]):
                    cooked.Append('\0');
                    break;
                case >= '0' and <= '9':
                    // A NotEscapeSequence: `\0` before a digit, and `\1` to `\9`. The digit after a `\0` is read again as
                    // a template character.
                    invalidAt = invalidAt < 0 ? escapeStart : invalidAt;
                    break;
                case 'x':
                    if (!TemplateHex(raw, 2, out var latin))
                        invalidAt = invalidAt < 0 ? escapeStart : invalidAt;
                    else
                        cooked.Append((char)latin);
                    break;
                case 'u':
                    if (!TemplateUnicode(raw, out var codePoint))
                        invalidAt = invalidAt < 0 ? escapeStart : invalidAt;
                    else
                        AppendCodePoint(cooked, codePoint);
                    break;
                default:
                    cooked.Append(escape);
                    break;
            }
        }
    }

    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s12.9.6 TV and TRV; IP=Low; Security=Medium; Resources=1; Fingerprint=44F559
    // Hairs-Human:        PENDING
    private Token TemplatePiece(TokenKind kind, int start, int startLine, int startLineStart, StringBuilder cooked, StringBuilder raw, int invalidAt)
    {
        var flags = invalidAt >= 0 ? TokenFlags.NotEscapeSequence : TokenFlags.None;
        return new Token(kind, start, position, AbsoluteLine(startLine), AbsoluteColumn(start, startLine, startLineStart), flags, invalidAt >= 0 ? null : cooked.ToString(), raw.ToString(), split: invalidAt);
    }

    /// <summary>
    /// A template's <c>\x</c>: two hex digits make a <c>HexEscapeSequence</c>; fewer make a <c>NotEscapeSequence</c>,
    /// which takes the one hex digit there is and leaves the next code unit to the template.
    /// </summary>
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s12.9.6 NotEscapeSequence; IP=Low; Security=High; Resources=2; Fingerprint=1B518C
    // Hairs-Falsified-If: a NotEscapeSequence takes a code unit after its hex digits, so the template reads the next one wrongly
    // Hairs-Human:        PENDING
    private bool TemplateHex(StringBuilder raw, int count, out int value)
    {
        value = 0;
        for (var i = 0; i < count; i++)
        {
            var digit = position < text.Length ? HexValue(text[position]) : -1;
            if (digit < 0)
                return false;
            raw.Append(text[position]);
            value = (value * 16) + digit;
            position++;
        }

        return true;
    }

    /// <summary>
    /// A template's <c>\u</c>: a <c>UnicodeEscapeSequence</c>, or the <c>NotEscapeSequence</c> its longest prefix is, which
    /// takes its hex digits and its <c>{</c> and leaves the next code unit to the template.
    /// </summary>
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s12.9.6 NotEscapeSequence; IP=Low; Security=High; Resources=2; Fingerprint=CDBA42
    // Hairs-Falsified-If: a NotEscapeSequence takes the code unit after its digits, or a code point above U+10FFFF is cooked
    // Hairs-Human:        PENDING
    private bool TemplateUnicode(StringBuilder raw, out int codePoint)
    {
        codePoint = 0;
        if (position < text.Length && text[position] == '{')
        {
            raw.Append('{');
            position++;
            var digits = 0;
            while (position < text.Length && HexValue(text[position]) is var digit and >= 0)
            {
                codePoint = codePoint > 0x10FFFF ? codePoint : (codePoint * 16) + digit;
                raw.Append(text[position]);
                digits++;
                position++;
            }

            if (digits == 0 || codePoint > 0x10FFFF || position >= text.Length || text[position] != '}')
                return false;

            raw.Append('}');
            position++;
            return true;
        }

        return TemplateHex(raw, 4, out codePoint);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Regular-expression literals: clause 12.9.5, delimited only; the pattern's validity is the provider's (decision D7).

    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s12.9.5 RegularExpressionLiteral; IP=Low; Security=High; Resources=2; Fingerprint=7EA2F0
    // Hairs-Falsified-If: a slash inside a class ends the literal, a line terminator is admitted in its body, or an escape is taken into its flags
    // Hairs-Human:        PENDING
    private Token ReadRegularExpression(int start)
    {
        var startLine = line;
        var startLineStart = lineStart;
        position = start + 1;
        var inClass = false;

        while (true)
        {
            if (position >= text.Length || IsLineTerminator(text[position]))
            {
                Refuse(Catalogue.RegExpUnterminated, start, startLine, startLineStart);
                return refusal;
            }

            var c = text[position];
            if (c == '\\')
            {
                if (position + 1 >= text.Length || IsLineTerminator(text[position + 1]))
                {
                    position++;
                    Refuse(Catalogue.RegExpUnterminated, start, startLine, startLineStart);
                    return refusal;
                }

                position += 2;
                continue;
            }

            if (c == '[')
                inClass = true;
            else if (c == ']')
                inClass = false;
            else if (c == '/' && !inClass)
                break;

            position++;
        }

        var split = position;
        position++;

        // RegularExpressionFlags: IdentifierPartChar code points, never an escape.
        while (position < text.Length && lexical.IsIdentifierPart(CodePointAt(position, out var width)))
            position += width;

        return new Token(TokenKind.RegularExpressionLiteral, start, position, AbsoluteLine(startLine), AbsoluteColumn(start, startLine, startLineStart), TokenFlags.None, split: split);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Punctuators: clause 12.8, longest match first.

    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s12.8 Punctuators longest match; IP=Low; Security=Medium; Resources=1; Fingerprint=D876D7
    // Hairs-Human:        PENDING
    private Token ReadPunctuator(int start, char c)
    {
        var kind = c switch
        {
            '{' => Take(1, TokenKind.LeftBrace),
            '}' => Take(1, TokenKind.RightBrace),
            '(' => Take(1, TokenKind.LeftParen),
            ')' => Take(1, TokenKind.RightParen),
            '[' => Take(1, TokenKind.LeftBracket),
            ']' => Take(1, TokenKind.RightBracket),
            ';' => Take(1, TokenKind.Semicolon),
            ',' => Take(1, TokenKind.Comma),
            '~' => Take(1, TokenKind.Tilde),
            ':' => Take(1, TokenKind.Colon),
            '@' => Take(1, TokenKind.At),
            '.' => At(start, "...") ? Take(3, TokenKind.Ellipsis) : Take(1, TokenKind.Dot),
            '<' => At(start, "<<=") ? Take(3, TokenKind.LeftShiftEquals)
                : At(start, "<<") ? Take(2, TokenKind.LeftShift)
                : At(start, "<=") ? Take(2, TokenKind.LessThanEquals)
                : Take(1, TokenKind.LessThan),
            '>' => At(start, ">>>=") ? Take(4, TokenKind.UnsignedRightShiftEquals)
                : At(start, ">>>") ? Take(3, TokenKind.UnsignedRightShift)
                : At(start, ">>=") ? Take(3, TokenKind.RightShiftEquals)
                : At(start, ">>") ? Take(2, TokenKind.RightShift)
                : At(start, ">=") ? Take(2, TokenKind.GreaterThanEquals)
                : Take(1, TokenKind.GreaterThan),
            '=' => At(start, "===") ? Take(3, TokenKind.EqualsEqualsEquals)
                : At(start, "==") ? Take(2, TokenKind.EqualsEquals)
                : At(start, "=>") ? Take(2, TokenKind.Arrow)
                : Take(1, TokenKind.Equals),
            '!' => At(start, "!==") ? Take(3, TokenKind.NotEqualsEquals)
                : At(start, "!=") ? Take(2, TokenKind.NotEquals)
                : Take(1, TokenKind.Bang),
            '+' => At(start, "++") ? Take(2, TokenKind.PlusPlus)
                : At(start, "+=") ? Take(2, TokenKind.PlusEquals)
                : Take(1, TokenKind.Plus),
            '-' => At(start, "--") ? Take(2, TokenKind.MinusMinus)
                : At(start, "-=") ? Take(2, TokenKind.MinusEquals)
                : Take(1, TokenKind.Minus),
            '*' => At(start, "**=") ? Take(3, TokenKind.StarStarEquals)
                : At(start, "**") ? Take(2, TokenKind.StarStar)
                : At(start, "*=") ? Take(2, TokenKind.StarEquals)
                : Take(1, TokenKind.Star),
            '/' => At(start, "/=") ? Take(2, TokenKind.SlashEquals) : Take(1, TokenKind.Slash),
            '%' => At(start, "%=") ? Take(2, TokenKind.PercentEquals) : Take(1, TokenKind.Percent),
            '&' => At(start, "&&=") ? Take(3, TokenKind.AmpersandAmpersandEquals)
                : At(start, "&&") ? Take(2, TokenKind.AmpersandAmpersand)
                : At(start, "&=") ? Take(2, TokenKind.AmpersandEquals)
                : Take(1, TokenKind.Ampersand),
            '|' => At(start, "||=") ? Take(3, TokenKind.BarBarEquals)
                : At(start, "||") ? Take(2, TokenKind.BarBar)
                : At(start, "|=") ? Take(2, TokenKind.BarEquals)
                : Take(1, TokenKind.Bar),
            '^' => At(start, "^=") ? Take(2, TokenKind.CaretEquals) : Take(1, TokenKind.Caret),
            '?' => At(start, "??=") ? Take(3, TokenKind.QuestionQuestionEquals)
                : At(start, "??") ? Take(2, TokenKind.QuestionQuestion)
                : At(start, "?.") && !(start + 2 < text.Length && IsAsciiDigit(text[start + 2])) ? Take(2, TokenKind.QuestionDot)
                : Take(1, TokenKind.Question),
            _ => TokenKind.Invalid,
        };

        if (kind == TokenKind.Invalid)
            return UnexpectedCharacter(start);

        return new Token(kind, start, position, AbsoluteLine(line), AbsoluteColumn(start, line, lineStart), TokenFlags.None);
    }

    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s12.8 Punctuators; IP=Low; Security=Medium; Resources=1; Fingerprint=7C4E9E
    // Hairs-Human:        PENDING
    private TokenKind Take(int length, TokenKind kind)
    {
        position += length;
        return kind;
    }

    /// <summary>A code point no input element begins with (catalogue row EX0001), at the code point.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue EX0001; IP=Low; Security=Medium; Resources=1; Fingerprint=314920
    // Hairs-Human:        PENDING
    private Token UnexpectedCharacter(int start)
    {
        CodePointAt(start, out var width);
        position = start + width;
        Refuse(Catalogue.UnexpectedCharacter, start, line, lineStart);
        return refusal;
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Positions, classes and refusals.

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D11 D15 diagnostic anchors; IP=Low; Security=Medium; Resources=1; Fingerprint=31F25A
    // Hairs-Human:        PENDING
    private bool Refuse(LexicalRefusal rule, int start, int startLine, int startLineStart)
    {
        var end = Math.Max(position, start);
        var span = new SourceSpan(source.BaseOffset + start, end - start, AbsoluteLine(startLine), AbsoluteColumn(start, startLine, startLineStart));
        diagnostic = new SyntaxDiagnostic(rule.Code, rule.MessageId, Array.Empty<string>(), DiagnosticPhase.Tokenizer, span, source.Name);
        refusal = new Token(TokenKind.Invalid, start, end, span.Line, span.Column, TokenFlags.None);
        return false;
    }

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D15 positions; IP=Low; Security=Medium; Resources=1; Fingerprint=ABEB25
    // Hairs-Human:        PENDING
    private int AbsoluteLine(int relativeLine) => source.BaseLine + relativeLine - 1;

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D15 positions; IP=Low; Security=Medium; Resources=1; Fingerprint=E65194
    // Hairs-Human:        PENDING
    private int AbsoluteColumn(int offset, int relativeLine, int relativeLineStart) =>
        relativeLine == 1 ? source.BaseColumn + offset : offset - relativeLineStart + 1;

    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s12.3 LineTerminatorSequence; IP=Low; Security=Medium; Resources=1; Fingerprint=4C9A4C
    // Hairs-Human:        PENDING
    private void AdvanceLine(char c)
    {
        // CR LF is one line terminator (clause 12.3), so it moves one line.
        position += c == '\r' && position + 1 < text.Length && text[position + 1] == '\n' ? 2 : 1;
        line++;
        lineStart = position;
    }

    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s12 lexical grammar; IP=Low; Security=Medium; Resources=1; Fingerprint=0C2BC4
    // Hairs-Human:        PENDING
    private bool At(int offset, string what) =>
        offset + what.Length <= text.Length && string.CompareOrdinal(text, offset, what, 0, what.Length) == 0;

    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s12.7 IdentifierStartChar via IUnicodeTables; IP=Low; Security=Medium; Resources=1; Fingerprint=E1A838
    // Hairs-Human:        PENDING
    private bool IsIdentifierStartAt(int offset, out int width) => lexical.IsIdentifierStart(CodePointAt(offset, out width));

    /// <summary>The code point at <paramref name="offset"/>: a surrogate pair read whole, anything else, a lone surrogate included, the code unit it is.</summary>
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s11.1.4 CodePointAt; IP=Low; Security=High; Resources=2; Fingerprint=1FC901
    // Hairs-Falsified-If: a surrogate pair is read as two code points or a lone surrogate as a pair
    // Hairs-Human:        PENDING
    private int CodePointAt(int offset, out int width)
    {
        var unit = text[offset];
        if (unit is >= '\uD800' and <= '\uDBFF' && offset + 1 < text.Length && text[offset + 1] is >= '\uDC00' and <= '\uDFFF')
        {
            width = 2;
            return 0x10000 + ((unit - 0xD800) << 10) + (text[offset + 1] - 0xDC00);
        }

        width = 1;
        return unit;
    }

    /// <summary>WhiteSpace (clause 12.2): TAB, VT, FF, ZWNBSP and the Space_Separator code points, all in the BMP, through the provider beyond ASCII.</summary>
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s12.2 WhiteSpace via IUnicodeTables; IP=Low; Security=Medium; Resources=1; Fingerprint=BDAB22
    // Hairs-Human:        PENDING
    private bool IsWhiteSpace(char c) => c is '\t' or '\v' or '\f' or ' ' ? true : c >= 0x80 && lexical.IsWhiteSpace(c);

    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s12.3 LineTerminator; IP=Low; Security=Medium; Resources=1; Fingerprint=FBEFF9
    // Hairs-Human:        PENDING
    private static bool IsLineTerminator(char c) => c is '\n' or '\r' or '\u2028' or '\u2029';

    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s12.9.3 DecimalDigit; IP=Low; Security=Medium; Resources=1; Fingerprint=3F788E
    // Hairs-Human:        PENDING
    private static bool IsAsciiDigit(char c) => c is >= '0' and <= '9';

    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s12.9.3 NonDecimalIntegerLiteral; IP=Low; Security=Medium; Resources=1; Fingerprint=FEFA4D
    // Hairs-Human:        PENDING
    private static int Radix(char c) => c switch
    {
        'x' or 'X' => 16,
        'o' or 'O' => 8,
        'b' or 'B' => 2,
        _ => 0,
    };

    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s12.9.4 HexDigit; IP=Low; Security=Medium; Resources=1; Fingerprint=2D7172
    // Hairs-Human:        PENDING
    private static int HexValue(char c) => c switch
    {
        >= '0' and <= '9' => c - '0',
        >= 'a' and <= 'f' => c - 'a' + 10,
        >= 'A' and <= 'F' => c - 'A' + 10,
        _ => -1,
    };

    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s12.9.3 digits of a radix; IP=Low; Security=Medium; Resources=1; Fingerprint=629D02
    // Hairs-Human:        PENDING
    private static int DigitValue(char c, int radix)
    {
        var value = HexValue(c);
        return value < radix ? value : -1;
    }

    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s11.1.1 UTF16EncodeCodePoint; IP=Low; Security=Medium; Resources=1; Fingerprint=A2F483
    // Hairs-Human:        PENDING
    private static void AppendCodePoint(StringBuilder into, int codePoint)
    {
        if (codePoint <= 0xFFFF)
        {
            into.Append((char)codePoint);
            return;
        }

        codePoint -= 0x10000;
        into.Append((char)(0xD800 + (codePoint >> 10))).Append((char)(0xDC00 + (codePoint & 0x3FF)));
    }

    /// <summary>A template whose substitution is open: where its backtick is, for where its refusal is anchored.</summary>
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s12.9.6, JSD-0083 D15; IP=Low; Security=Low; Resources=0; Fingerprint=0CFD5E
    // Hairs-Human:        PENDING
    internal sealed class TemplateFrame(int start, int line, int lineStart, TemplateFrame? outer)
    {
        /// <summary>The offset of the template's opening backtick.</summary>
        internal int Start { get; } = start;

        /// <summary>The relative line it stands on.</summary>
        internal int Line { get; } = line;

        /// <summary>The offset that line starts at.</summary>
        internal int LineStart { get; } = lineStart;

        /// <summary>The template whose substitution holds this one, or null.</summary>
        internal TemplateFrame? Outer { get; } = outer;
    }
}

/// <summary>A tokenizer's state, which <see cref="Tokenizer.Save"/> takes and <see cref="Tokenizer.Restore"/> returns to.</summary>
// Hairs-AI:           Origin=AI; Spec=ECMA-262 s12 lexical goal lookahead; IP=Low; Security=Low; Resources=0; Fingerprint=AC6498
// Hairs-Human:        PENDING
internal readonly record struct TokenizerState(
    int Position,
    int Line,
    int LineStart,
    bool OnlyTriviaOnLine,
    bool NewLine,
    Tokenizer.TemplateFrame? Templates,
    SyntaxDiagnostic? Diagnostic,
    Token Refusal);

/// <summary>A lexical rule's shared code and message identifier, from the early-error catalogue (<c>early-errors.tsv</c>).</summary>
// Hairs-AI:           Origin=AI; Spec=catalogue shared codes; IP=Low; Security=Low; Resources=0; Fingerprint=DEEEF7
// Hairs-Human:        PENDING
internal readonly record struct LexicalRefusal(string Code, string MessageId);

/// <summary>The catalogue rows the tokenizer refuses by (decision D11's shared codes; the catalogue's message identifiers).</summary>
// Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers; IP=Low; Security=Low; Resources=0; Fingerprint=7358A6
// Hairs-Human:        PENDING
internal static class Catalogue
{
    /// <summary>EX0001: a code point that begins no input element of the goal's lexical grammar.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue row EX0001; IP=Low; Security=Low; Resources=1; Fingerprint=E7BC20
    // Hairs-Human:        PENDING
    internal static LexicalRefusal UnexpectedCharacter => new("JSE9101", "syntax.character.unexpected");

    /// <summary>EX0002: a <c>MultiLineComment</c> with no closing <c>*/</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue row EX0002; IP=Low; Security=Low; Resources=1; Fingerprint=3837AF
    // Hairs-Human:        PENDING
    internal static LexicalRefusal CommentUnterminated => new("JSE9102", "syntax.comment.unterminated");

    /// <summary>EX0003: a numeric literal no <c>NumericLiteral</c> production matches, or one an IdentifierStart or a digit touches.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue row EX0003; IP=Low; Security=Low; Resources=1; Fingerprint=A4B1D3
    // Hairs-Human:        PENDING
    internal static LexicalRefusal NumericLiteralInvalid => new("JSE9103", "syntax.numeric-literal.invalid");

    /// <summary>EX0004: a <c>StringLiteral</c> with no closing quote before a line terminator or the end.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue row EX0004; IP=Low; Security=Low; Resources=1; Fingerprint=7BDAB5
    // Hairs-Human:        PENDING
    internal static LexicalRefusal StringUnterminated => new("JSE9104", "syntax.string.unterminated");

    /// <summary>EX0005: a string literal's escape sequence the lexical grammar does not define.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue row EX0005; IP=Low; Security=Low; Resources=1; Fingerprint=044085
    // Hairs-Human:        PENDING
    internal static LexicalRefusal StringEscapeInvalid => new("JSE9105", "syntax.string.escape.invalid");

    /// <summary>EX0006: a <c>RegularExpressionLiteral</c> with no closing <c>/</c> before a line terminator or the end.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue row EX0006; IP=Low; Security=Low; Resources=1; Fingerprint=175544
    // Hairs-Human:        PENDING
    internal static LexicalRefusal RegExpUnterminated => new("JSE9106", "syntax.regexp.unterminated");

    /// <summary>EX0007: a template literal with no closing backtick before the end.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue row EX0007; IP=Low; Security=Low; Resources=1; Fingerprint=DA7B62
    // Hairs-Human:        PENDING
    internal static LexicalRefusal TemplateUnterminated => new("JSE9107", "syntax.template.unterminated");

    /// <summary>EX0025: an identifier's unicode escape the lexical grammar does not define.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue row EX0025; IP=Low; Security=Low; Resources=1; Fingerprint=1CA9F0
    // Hairs-Human:        PENDING
    internal static LexicalRefusal IdentifierEscapeInvalid => new("JSE9111", "syntax.identifier.escape.invalid");

    /// <summary>EE0001: an escaped code point no <c>IdentifierStartChar</c> matches, at a name's start (clause 12.7.1.1).</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue row EE0001; IP=Low; Security=Low; Resources=1; Fingerprint=CEE78A
    // Hairs-Human:        PENDING
    internal static LexicalRefusal IdentifierEscapeStart => new("JSE1201", "identifier.escape.invalid-start");

    /// <summary>EE0002: an escaped code point no <c>IdentifierPartChar</c> matches, after a name's start (clause 12.7.1.1).</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue row EE0002; IP=Low; Security=Low; Resources=1; Fingerprint=70D3EB
    // Hairs-Human:        PENDING
    internal static LexicalRefusal IdentifierEscapePart => new("JSE1202", "identifier.escape.invalid-part");
}
