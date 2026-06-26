using Broiler.JavaScript.Ast.Misc;
using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Broiler.JavaScript.Parser;


/// <summary>
/// Scanner Features.
/// 
/// 1.  Scanner ignores whitespace and comments
///     but a token is marked as LineTerminated if it is
///     followed by a line terminator.
///     
///     This is useful in the case when expression needs
///     a line terminator as a expression end marker.
///     
///     Ignoring line terminator and whitespace makes
///     parsing rules simple as everything else are pure
///     tokens.
///     
/// 2.  Scanner parses first token and keeps next token 
///     ready. Only when you consume current token, next 
///     token is read. This is to avoid in case of failure.
///     
/// 3.  Never read beyond EOF, because once you encounter
///     EOF, scanner will endlessly send you EOF. It is 
///     responsibility of the Parser to detect end of program.
/// 
/// </summary>
public class FastScanner
{
    private readonly FastPool pool;
    public readonly StringSpan Text;
    private readonly FastKeywordMap keywords;
    private int position = 0;

    private int line = 1;
    private int column = 1;
    private int templateParts = 0;
    // Within an open template substitution (`${ ... }`) we must balance plain
    // `{ }` braces (object literals, function bodies, blocks) so that an inner
    // `}` is not mistaken for the substitution terminator. templateBraceDepth is
    // the count of currently-open plain braces in the innermost substitution;
    // the stack saves the enclosing substitutions' depths across nested
    // template literals.
    private int templateBraceDepth = 0;
    private readonly System.Collections.Generic.Stack<int> templateBraceDepthStack = new();

    // Set while scanning a single template part when it contains an invalid escape
    // sequence (deferred — a tagged template tolerates it with an undefined cooked
    // value, an untagged one is an early SyntaxError). Reset per part.
    private bool templateCookedInvalid = false;

    public SpanLocation Location => new(line, column);

    public Exception Unexpected()
    {
        var c = Token;
        return new FastParseException(c, $"Unexpected token {c.Type}: {c.Span} at {Location}");
    }

    public FastScanner(FastPool pool, in StringSpan text, FastKeywordMap keywords = null)
    {
        this.pool = pool;
        Text = text;
        this.keywords = keywords ?? FastKeywordMap.Instance;

        Token = ReadToken();
        nextToken = ReadToken();
        Token.Next = nextToken;
        nextToken.Previous = Token;
    }

    private static readonly FastToken EmptyToken = new(TokenTypes.Empty, string.Empty);
    private static readonly FastToken EOF = new(TokenTypes.EOF, string.Empty);
    private FastToken nextToken = EOF;

    private FastToken lastToken = EmptyToken;

    /// <summary>
    /// Whether <c>yield</c> is currently a keyword (set by the parser when it enters
    /// a generator body). This governs the regex-vs-division decision for a <c>/</c>
    /// that follows a <c>yield</c> token: after the keyword form it begins a
    /// regular-expression literal, after the identifier form it is the division
    /// operator.
    /// </summary>
    public bool YieldIsKeyword;

    public FastToken Token
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get; private set;
    } = EmptyToken;

    public void ConsumeToken()
    {
        // lets ignore consecutive line terminators
        Token = nextToken;
        nextToken = ReadToken();

        while (Token.Type == TokenTypes.LineTerminator && nextToken.Type == TokenTypes.LineTerminator)
        {
            // Collapsing consecutive line terminators into one token must preserve
            // the link back to the last significant token. ReadToken() does not set
            // Previous, so without carrying it over PreviousToken would resolve to
            // this line-terminator token itself — wrongly extending the preceding
            // node's source range across the intervening blank lines / comments
            // (e.g. Function.prototype.toString including a trailing `// comment`).
            var previous = Token.Previous;
            Token = nextToken;
            Token.Previous = previous;
            nextToken = ReadToken();
        }

        Token.Next = nextToken;
        nextToken.Previous = Token;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private (int position, int line, int column) SaveCursor() => (position, line, column);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RestoreCursor(in (int position, int line, int column) cursor)
    {
        position = cursor.position;
        line = cursor.line;
        column = cursor.column;
    }

    private char Peek()
    {
        if (position >= Text.Length)
            return char.MaxValue;

        return Text[position];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private char Next()
    {
        var next = position + 1;
        if (next >= Text.Length)
            return char.MaxValue;

        return Text[next];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private char Peek(int offset)
    {
        var index = position + offset;
        if (index < 0 || index >= Text.Length)
            return char.MaxValue;

        return Text[index];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool ConsumeAndNext(char ch)
    {
        var next = Consume();
        if (next == ch)
        {
            Consume();
            return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private char Consume()
    {
        if (position >= Text.Length)
            return char.MaxValue;

        char ch = Text[position];

        if (ch == '\n')
        {
            line++;
            column = 0;
        }
        else
        {
            column++;
        }

        position++;

        if (position >= Text.Length)
            return char.MaxValue;

        ch = Text[position];
        return ch;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool CanConsume(char ch)
    {
        if (ch == Peek())
        {
            Consume();
            return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool CanConsumeNext(char ch)
    {
        if (ch == Next())
        {
            Consume();
            return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool CanConsume(char ch1, char ch2)
    {
        var ch = Peek();
        if (ch == ch1 || ch == ch2)
        {
            Consume();
            return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private FastToken ReadToken()
    {
        lastToken = _ReadToken();
        return lastToken;
    }

    private FastToken _ReadToken()
    {
        var state = Push();
        char first = Peek();

        if (first == char.MaxValue)
            return EOF;

        // following logic will
        // skip consecutive line breaks
        // and send only one line terminator token
        bool lineTerminator = false;
        bool skipped = false;

        // U+FEFF ZERO WIDTH NO-BREAK SPACE (ZWNBSP/BOM) is ECMAScript WhiteSpace, but
        // .NET reclassified it out of White_Space so char.IsWhiteSpace returns false
        // for it; treat it as whitespace explicitly.
        while (char.IsWhiteSpace(first) || first == '﻿')
        {
            if (first.IsLineTerminator())
                lineTerminator = true;

            first = Consume();
            skipped = true;
        }

        if (lineTerminator)
            return state.Commit(TokenTypes.LineTerminator);

        // Trailing non-newline whitespace runs `first` to EOF inside the skip
        // loop above; without re-checking we would fall through the entire token
        // switch to `throw Unexpected()` (reporting the previous token). The
        // initial EOF guard only runs before whitespace skipping, so a source
        // that ends in spaces/tabs (no final newline) would spuriously fail.
        if (first == char.MaxValue)
            return EOF;

        if (skipped)
            state = Push();

        if (first == '\\' || first.IsIdentifierStart())
            return ReadIdentifier(state);

        if (char.IsHighSurrogate(first) && char.IsLowSurrogate(Next())
            && char.ConvertToUtf32(first, Next()).IsIdentifierStart())
            return ReadIdentifier(state);

        switch (first)
        {
            case '\'':
            case '"':
                return ReadString(state, first);

            case '`':
                return ReadTemplateString(state);

            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9':
                return ReadNumber(state, first);

            case '#':
                if (Next() == '!' && IsHashbangStart())
                    return SkipSingleLineComment(state, 2);
                return ReadSymbol(state, TokenTypes.Hash);

            case '/':
                // Read comments
                // Read Regex
                // Read /=
                return ReadCommentsOrRegExOrSymbol(state);

            case ',':
                return ReadSymbol(state, TokenTypes.Comma);

            case '(':
                return ReadSymbol(state, TokenTypes.BracketStart);

            case ')':
                return ReadSymbol(state, TokenTypes.BracketEnd);

            case '[':
                return ReadSymbol(state, TokenTypes.SquareBracketStart);

            case ']':
                return ReadSymbol(state, TokenTypes.SquareBracketEnd);

            case '{':
                if (templateParts > 0)
                    templateBraceDepth++;
                return ReadSymbol(state, TokenTypes.CurlyBracketStart);

            case '}':
                if (templateParts > 0)
                {
                    // A `}` only closes the substitution when no plain braces are
                    // open inside it; otherwise it closes one of those braces.
                    if (templateBraceDepth > 0)
                    {
                        templateBraceDepth--;
                        return ReadSymbol(state, TokenTypes.CurlyBracketEnd);
                    }

                    templateParts--;
                    templateBraceDepth = templateBraceDepthStack.Pop();
                    return ReadTemplateString(state, TokenTypes.TemplatePart);
                }
                return ReadSymbol(state, TokenTypes.CurlyBracketEnd);

            case '!':
                switch (Consume())
                {
                    case '=':
                        if (ConsumeAndNext('='))
                            return state.Commit(TokenTypes.StrictlyNotEqual);

                        return state.Commit(TokenTypes.NotEqual);
                }

                return state.Commit(TokenTypes.Negate);
            case '>':
                switch (Consume())
                {
                    case '>':
                        switch (Consume())
                        {
                            case '>':
                                if (ConsumeAndNext('='))
                                    return state.Commit(TokenTypes.AssignUnsignedRightShift);

                                return state.Commit(TokenTypes.UnsignedRightShift);

                            case '=':
                                Consume();
                                return state.Commit(TokenTypes.AssignRightShift);
                        }

                        return state.Commit(TokenTypes.RightShift);

                    case '=':
                        Consume();
                        return state.Commit(TokenTypes.GreaterOrEqual);
                }
                return state.Commit(TokenTypes.Greater);

            case '<':
                if (IsHtmlOpenCommentStart())
                    return SkipSingleLineComment(state, 4);
                switch (Consume())
                {
                    case '<':
                        if (ConsumeAndNext('='))
                            return state.Commit(TokenTypes.AssignLeftShift);

                        return state.Commit(TokenTypes.LeftShift);

                    case '=':
                        Consume();
                        return state.Commit(TokenTypes.LessOrEqual);
                }
                return state.Commit(TokenTypes.Less);

            case '*':
                switch (Consume())
                {
                    case '*':
                        if (ConsumeAndNext('='))
                            return state.Commit(TokenTypes.AssignPower);

                        return state.Commit(TokenTypes.Power);

                    case '=':
                        Consume();
                        return state.Commit(TokenTypes.AssignMultiply);
                }
                return state.Commit(TokenTypes.Multiply);

            case '&':
                switch (Consume())
                {
                    case '&':
                        if (ConsumeAndNext('='))
                            return state.Commit(TokenTypes.AssignBooleanAnd);

                        return state.Commit(TokenTypes.BooleanAnd);

                    case '=':
                        Consume();
                        return state.Commit(TokenTypes.AssignBitwideAnd);
                }
                return state.Commit(TokenTypes.BitwiseAnd);

            case '|':
                switch (Consume())
                {
                    case '|':
                        if (ConsumeAndNext('='))
                            return state.Commit(TokenTypes.AssignBooleanOr);

                        return state.Commit(TokenTypes.BooleanOr);

                    case '=':
                        Consume();
                        return state.Commit(TokenTypes.AssignBitwideOr);
                }
                return state.Commit(TokenTypes.BitwiseOr);

            case '+':
                switch (Consume())
                {
                    case '+':
                        Consume();
                        return state.Commit(TokenTypes.Increment);

                    case '=':
                        Consume();
                        return state.Commit(TokenTypes.AssignAdd);
                }
                return state.Commit(TokenTypes.Plus);

            case '-':
                if (IsHtmlCloseCommentStart())
                    return SkipSingleLineComment(state, 3);
                switch (Consume())
                {
                    case '-':
                        Consume();
                        return state.Commit(TokenTypes.Decrement);

                    case '=':
                        Consume();
                        return state.Commit(TokenTypes.AssignSubtract);
                }
                return state.Commit(TokenTypes.Minus);

            case '^':
                if (ConsumeAndNext('='))
                    return state.Commit(TokenTypes.AssignXor);

                return state.Commit(TokenTypes.Xor);

            case '?':
                switch (Consume())
                {
                    case '.':
                        // `?.` is an OptionalChainingPunctuator only when NOT followed
                        // by a DecimalDigit: `a ?.3 : b` is the conditional operator
                        // with a fractional literal (`?` then `.3`), not optional
                        // chaining. Emit `?` and leave `.3` to scan as a number.
                        if (char.IsDigit(Next()))
                            return state.Commit(TokenTypes.QuestionMark);

                        switch (Consume())
                        {
                            case '(':
                                Consume();
                                return state.Commit(TokenTypes.OptionalCall);
                            case '[':
                                Consume();
                                return state.Commit(TokenTypes.OptionalIndex);
                        }
                        return state.Commit(TokenTypes.QuestionDot);

                    case '?':
                        if (ConsumeAndNext('='))
                            return state.Commit(TokenTypes.AssignCoalesce);

                        return state.Commit(TokenTypes.Coalesce);
                }
                return state.Commit(TokenTypes.QuestionMark);

            case '.':
                var peek = Next();
                if (char.IsDigit(peek))
                {
                    Consume();
                    return ReadNumber(state, first);
                }

                switch (Consume())
                {
                    case '.':
                        if (ConsumeAndNext('.'))
                            return state.Commit(TokenTypes.TripleDots);

                        throw Unexpected();
                }
                return state.Commit(TokenTypes.Dot);

            case ':':
                return ReadSymbol(state, TokenTypes.Colon);

            case ';':
                return ReadSymbol(state, TokenTypes.SemiColon);

            case '@':
                return ReadSymbol(state, TokenTypes.At);

            case '~':
                return ReadSymbol(state, TokenTypes.BitwiseNot);

            case '%':
                if (ConsumeAndNext('='))
                    return state.Commit(TokenTypes.AssignMod);

                return state.Commit(TokenTypes.Mod);

            case '\n':
                return ReadSymbol(state, TokenTypes.LineTerminator);

            case '=':
                switch (Consume())
                {
                    case '=':
                        if (ConsumeAndNext('='))
                            return state.Commit(TokenTypes.StrictlyEqual);

                        return state.Commit(TokenTypes.Equal);

                    case '>':
                        Consume();
                        return state.Commit(TokenTypes.Lambda);
                }
                return state.Commit(TokenTypes.Assign);
        }

        throw Unexpected();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsHtmlOpenCommentStart()
    {
        return Peek(0) == '<' && Peek(1) == '!' && Peek(2) == '-' && Peek(3) == '-';
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsHtmlCloseCommentStart()
    {
        if (Peek(0) != '-' || Peek(1) != '-' || Peek(2) != '>')
            return false;

        return lastToken.Type == TokenTypes.Empty || lastToken.Type == TokenTypes.LineTerminator;
    }

    private bool ScanEscaped(char next, StringBuilder t, bool deferInvalid = false)
    {
        if (next != '\\')
            return false;

        next = Consume();

        // Template (strict) escape rules (ES2018 template literal revision): a
        // DecimalEscape (`\1`-`\9`), a LegacyOctalEscapeSequence, and `\0` followed
        // by a decimal digit are invalid escapes whose cooked value is undefined.
        // Defer (mark + skip) rather than throw so a tagged template tolerates them;
        // an untagged template literal is rejected later by the compiler.
        if (deferInvalid)
        {
            if (next >= '1' && next <= '9')
            {
                templateCookedInvalid = true;
                return true;
            }

            if (next == '0')
            {
                var d = Next();
                if (d >= '0' && d <= '9')
                {
                    Consume();
                    templateCookedInvalid = true;
                    return true;
                }
            }
        }

        switch (next)
        {
            /**
             * This is special case, slash followed by a single line terminator is
             * only used to break the string starting at next line
             */
            case '\n':
                return true;

            // §11.8.4 LineContinuation eats `\` followed by a LineTerminatorSequence
            // (LF, CR, LS, PS, or CRLF). Consume() here is "advance past current, return
            // new current", and CanConsumeNext already advanced past `\r` when the next
            // char was `\n`. The previous code then did an extra Consume(), which also
            // advanced past `\n` AND popped the next visible character ("b" in `"a\\\r\nb"`),
            // so the loop returned the closing quote and produced "a" instead of "ab".
            case '\r':
                if (Next() == '\n')
                    Consume();
                return true;

            case '\u2028':
            case '\u2029':
                return true;

            case 'u':
                if (CanConsumeNext('{'))
                {
                    if (deferInvalid)
                    {
                        // `\u{...}` may be malformed (no digits, non-hex, out of
                        // range). Reuse the throwing scanner but roll the position
                        // back on failure so the rest of the template part — and in
                        // particular its terminating backtick — is rescanned normally.
                        var save = SaveCursor();
                        try
                        {
                            t.Append(ScanUnicodeCodePointEscape());
                            return true;
                        }
                        catch (FastParseException)
                        {
                            RestoreCursor(save);
                            templateCookedInvalid = true;
                            return true;
                        }
                    }
                    t.Append(ScanUnicodeCodePointEscape());
                    return true;
                }

                if (deferInvalid)
                {
                    var save = SaveCursor();
                    if (ScanHexEscape(next, out var n2))
                    {
                        t.Append(n2);
                        return true;
                    }
                    RestoreCursor(save);
                    templateCookedInvalid = true;
                    return true;
                }

                if (ScanHexEscape(next, out var n))
                {
                    t.Append(n);
                    return true;
                }
                throw Unexpected();

            case 'x':
                if (deferInvalid)
                {
                    var save = SaveCursor();
                    if (ScanHexEscape(next, out var hex2))
                    {
                        t.Append(hex2);
                        return true;
                    }
                    RestoreCursor(save);
                    templateCookedInvalid = true;
                    return true;
                }

                if (ScanHexEscape(next, out var hex))
                {
                    t.Append(hex);
                    return true;
                }
                throw Unexpected();

            case 'n':
                next = '\n';
                break;

            case 'r':
                next = '\r';
                break;

            case 't':
                next = '\t';
                break;

            case 'b':
                next = '\b';
                break;

            case 'f':
                next = '\f';
                break;

            case 'v':
                next = '\v';
                break;

            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            {
                // Annex B.1.2 LegacyOctalEscapeSequence: in a non-strict string literal a
                // `\` followed by octal digits is the corresponding code unit (e.g. `\1`
                // is U+0001, `\101` is "A"). A leading digit 0-3 permits up to three octal
                // digits total, 4-7 up to two (the value never exceeds 0o377 = 255). `\0`
                // not followed by an octal digit is the NUL escape. Strict-mode and
                // template occurrences are rejected separately (SyntaxValidation /
                // the deferInvalid path above), so only the cooked value is produced here.
                int value = next - '0';
                int maxMore = next <= '3' ? 2 : 1;
                for (var k = 0; k < maxMore; k++)
                {
                    var d = Next();
                    if (d < '0' || d > '7')
                        break;

                    Consume();
                    value = value * 8 + (d - '0');
                }

                t.Append((char)value);
                return true;
            }

            default:
                t.Append(next);
                return true;
        }

        t.Append(next);
        return true;

        bool ScanHexEscape(char prefix, out char result)
        {
            var len = (prefix == 'u') ? 4 : 2;
            var code = 0;

            for (var i = 0; i < len; ++i)
            {
                char ch = Consume();
                if (ch != char.MaxValue)
                {
                    if (ch.IsDigitPart(true, false))
                    {
                        code = code * 16 + ch.HexValue();
                    }
                    else
                    {
                        result = char.MinValue;
                        return false;
                    }
                }

                else
                {
                    result = char.MinValue;
                    return false;
                }
            }

            result = (char)code;
            return true;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsHashbangStart()
    {
        return position == 0;
    }

    private string ScanUnicodeCodePointEscape()
    {
        var ch = Consume();

        // At least one hex digit is required.
        if (ch == '}')
            throw Unexpected();

        var codePoint = 0;
        while (ch != char.MaxValue)
        {
            if (!ch.IsDigitPart(true, false))
                break;

            // A code point above U+10FFFF is a SyntaxError. Bound-check during
            // accumulation rather than relying on `checked` overflow, which would
            // surface as a non-SyntaxError for very long digit runs (e.g.
            // `\u{100000000000000000000000000000}`); leading zeros stay in range.
            codePoint = codePoint * 16 + ch.HexValue();
            if (codePoint > 0x10FFFF)
                throw Unexpected();

            ch = Consume();
        }

        if (ch != '}')
            throw Unexpected();

        try
        {
            return codePoint.FromCodePoint();
        }
        catch (ArgumentOutOfRangeException)
        {
            throw Unexpected();
        }
    }

    private static char HexDigit(int nibble)
        => (char)(nibble < 10 ? '0' + nibble : 'a' + (nibble - 10));

    // Rewrites every `\u{H..H}` braced code-point escape in a u/v-mode regex body into
    // the fixed-width `\uHHHH` form (a surrogate-pair escape for an astral code point),
    // leaving all other text — including a `\\u{…}` where the `\u` is itself escaped —
    // untouched. The body has already been validated by the source scanner, so any
    // `\u{` here is a well-formed escape with at least one hex digit and a closing `}`.
    private string DecodeBracedUnicodeEscapes(string pattern)
    {
        if (pattern.IndexOf("\\u{", StringComparison.Ordinal) < 0)
            return pattern;

        var t = new StringBuilder(pattern.Length);
        int i = 0;
        while (i < pattern.Length)
        {
            char c = pattern[i];
            if (c == '\\' && i + 1 < pattern.Length)
            {
                if (pattern[i + 1] == 'u' && i + 2 < pattern.Length && pattern[i + 2] == '{')
                {
                    // In u/v mode `\u{ … }` must be a non-empty run of hexadecimal digits
                    // naming a code point ≤ U+10FFFF, closed by `}`. Anything else — a
                    // non-hex char (`\u{G}`, `\u{0.0}`, `\u{-1}`), an empty `\u{}`, an
                    // unterminated `\u{`, or a value above U+10FFFF — is a SyntaxError
                    // (there is no IdentityEscape fallback in Unicode mode).
                    static int HexDigitValue(char ch) =>
                        ch is >= '0' and <= '9' ? ch - '0'
                        : ch is >= 'a' and <= 'f' ? ch - 'a' + 10
                        : ch is >= 'A' and <= 'F' ? ch - 'A' + 10
                        : -1;

                    int j = i + 3;
                    int codePoint = 0;
                    int digitCount = 0;
                    while (j < pattern.Length && pattern[j] != '}')
                    {
                        int hexValue = HexDigitValue(pattern[j]);
                        if (hexValue < 0)
                            throw Unexpected();

                        codePoint = codePoint * 16 + hexValue;
                        if (codePoint > 0x10FFFF)
                            throw Unexpected();

                        digitCount++;
                        j++;
                    }

                    // Reject an empty `\u{}` and an unterminated `\u{…` (ran off the end
                    // without a closing `}`).
                    if (digitCount == 0 || j >= pattern.Length)
                        throw Unexpected();

                    // A braced escape naming a lone surrogate (`\u{D83D}`) is its OWN
                    // single code point and must never combine with an adjacent surrogate:
                    // `/\u{D83D}\u{DC38}/u` is two lone surrogates, NOT the astral pair
                    // `/\u{1F438}/u`. Decoding both to the bare `\uHHHH` form would make
                    // them indistinguishable (`🐸`), so the RegExp runtime would
                    // wrongly fold the two lone surrogates into one code point. Keep the
                    // brace form (normalized) for lone surrogates — the runtime's
                    // TransformBracedUnicodeEscapes guards each so it cannot pair — and
                    // decode every other code point to the fixed-width form as before.
                    if (codePoint is >= 0xD800 and <= 0xDFFF)
                    {
                        t.Append('\\');
                        t.Append('u');
                        t.Append('{');
                        t.Append(HexDigit((codePoint >> 12) & 0xF));
                        t.Append(HexDigit((codePoint >> 8) & 0xF));
                        t.Append(HexDigit((codePoint >> 4) & 0xF));
                        t.Append(HexDigit(codePoint & 0xF));
                        t.Append('}');
                    }
                    else
                    {
                        foreach (var cu in codePoint.FromCodePoint())
                        {
                            t.Append('\\');
                            t.Append('u');
                            t.Append(HexDigit(cu >> 12));
                            t.Append(HexDigit((cu >> 8) & 0xF));
                            t.Append(HexDigit((cu >> 4) & 0xF));
                            t.Append(HexDigit(cu & 0xF));
                        }
                    }

                    i = j + 1; // skip past the closing '}'
                    continue;
                }

                // Any other escape (including a literal `\\`) is copied with the char it
                // escapes so that char cannot be misread as starting a `\u{`.
                t.Append(c);
                t.Append(pattern[i + 1]);
                i += 2;
                continue;
            }

            t.Append(c);
            i++;
        }

        return t.ToString();
    }

    private FastToken ReadTemplateString(State state, TokenTypes part = TokenTypes.TemplateBegin)
    {
        var sb = pool.AllocateStringBuilder();
        var t = sb.Builder;
        templateCookedInvalid = false;

        try
        {
            do
            {
                char ch = Consume();
                switch (ch)
                {
                    case '$':
                        // Only `${` begins a substitution. Peek at the next char rather
                        // than consuming it: a lone `$` is literal content, and the
                        // following char must be re-processed by the loop (it may end the
                        // template — `` `$` `` — start its own `${`, or be an escape). The
                        // old code consumed and appended it unconditionally, swallowing a
                        // closing backtick or a subsequent `${`.
                        if (Next() == '{')
                        {
                            Consume(); // '$' -> '{'
                            Consume(); // '{' -> after
                            // template part begin...
                            templateParts++;
                            templateBraceDepthStack.Push(templateBraceDepth);
                            templateBraceDepth = 0;
                            return state.Commit(part, t, templateCookedInvalid);
                        }

                        t.Append('$');
                        continue;

                    case '`':
                        Consume();
                        return state.Commit(TokenTypes.TemplateEnd, t, templateCookedInvalid);

                    case char.MaxValue:
                        break;
                }

                if (ch == char.MaxValue)
                    throw Unexpected();

                if (ScanEscaped(ch, t, deferInvalid: true))
                    continue;

                if (ch == '\r')
                {
                    // ES LineTerminatorSequence normalization: a bare <CR> and a <CRLF> pair
                    // both contribute a single <LF> to the cooked (TV) template value.
                    // (<LS> U+2028 / <PS> U+2029 are NOT normalized and fall through below.)
                    t.Append('\n');
                    if (Next() == '\n')
                        Consume(); // step onto the LF so the CRLF is consumed as one sequence
                    continue;
                }

                t.Append(ch);
            } while (true);
        }
        finally
        {
            sb.Clear();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private FastToken ReadSymbol(State state, TokenTypes type)
    {
        Consume();
        return state.Commit(type);
    }

    private FastToken ReadCommentsOrRegExOrSymbol(State state)
    {
        var scanRegExp = true;
        var last = lastToken;

        switch (last.Type)
        {
            case TokenTypes.BracketEnd:
            case TokenTypes.SquareBracketEnd:
            case TokenTypes.Number:
                // probably not regexp...
                scanRegExp = false;
                break;

            case TokenTypes.Identifier:
                scanRegExp = last.Keyword switch
                {
                    // Keywords that introduce an expression or statement: a following
                    // `/` begins a regex, never division (`new /x/()`, `typeof /x/`,
                    // `delete /x/.source`, `case /x/:`, `return /x/`, `do /x/.test(s)`).
                    FastKeywords.instanceof or FastKeywords.@in or FastKeywords.@typeof
                        or FastKeywords.@return or FastKeywords.await or FastKeywords.@new
                        or FastKeywords.@delete or FastKeywords.@void or FastKeywords.@throw
                        or FastKeywords.@case or FastKeywords.@do or FastKeywords.@else => true,
                    // `yield` only introduces an expression (so a following `/` begins
                    // a regex) when it is a keyword — inside a generator body. In sloppy
                    // code outside a generator it is an ordinary identifier, after which
                    // `/` is division (e.g. `var yield = 1; yield /a/g`).
                    FastKeywords.yield => YieldIsKeyword,
                    _ => false,
                };
                break;
        }

        var divide = Push();
        var first = Consume();
        bool divideAndAssign = false;

        switch (first)
        {
            /**
             * '//'
             */
            case '/':
                return SkipSingleLineComment(state);
            /**
             * '/*'
             */
            case '*':
                return SkipMultilineComment(state);
            /**
             * '/='
             */
            case '=':
                // this case should first consider if it is part of Regex or not..
                divideAndAssign = true;
                break;
        }

        if (scanRegExp)
        {
            if (ScanRegEx(state, first, out var token))
                return token;
        }

        if (divideAndAssign)
        {
            state.Dispose();
            Consume();
            Consume();
            return divide.Commit(TokenTypes.AssignDivide);
        }

        state.Dispose();
        Consume();
        return divide.Commit(TokenTypes.Divide);

        bool ScanRegEx(State state, char first, out FastToken token)
        {
            /**
                * Regex will never be followed by 
                * `)`, `]` and `keyword or identifier`
                */
            switch (lastToken.Type)
            {

                case TokenTypes.Identifier:
                    if (!lastToken.IsKeyword)
                    {
                        token = null;
                        return false;
                    }
                    break;

                case TokenTypes.BracketEnd:
                case TokenTypes.SquareBracketEnd:
                    token = null;
                    return false;
            }

            var sb = pool.AllocateStringBuilder();
            var t = sb.Builder;
            var classMarker = false;
            var terminated = false;
            var sawBracedUnicodeEscape = false;

            token = null;

            string regExp = null;
            try
            {
                do
                {
                    switch (first)
                    {
                        case char.MaxValue:
                            return false;

                        // A LineTerminator may not appear unescaped in a regex literal.
                        case '\n':
                        case '\r':
                        case '\u2028':
                        case '\u2029':
                            return false;

                        case '/':
                            if (classMarker)
                            {
                                t.Append(first);
                                break;
                            }

                            terminated = true;
                            Consume();
                            break;

                        case '[':
                            classMarker = true;
                            t.Append(first);
                            break;

                        case ']':
                            classMarker = false;
                            t.Append(first);
                            break;

                        case '\\':
                            first = Consume();

                            if (first == '/')
                            {
                                t.Append('\\');
                                t.Append('/');
                                break;
                            }

                            // A `\u{...}` braced escape is appended verbatim and scanned as
                            // ordinary characters (the brace/hex run holds no `/`, so it
                            // cannot swallow the terminator). It is a code-point escape only
                            // in u/v mode; without those flags it is the legacy `u`
                            // IdentityEscape followed by a `{n}` quantifier (so `/\u{3}/` \u2261
                            // `u{3}`). The flags are only known after the body, so decoding
                            // to fixed-width `\uHHHH` (the form the Unicode validator and the
                            // RegExp runtime expect) is deferred to DecodeBracedUnicodeEscapes
                            // below, applied only when a u/v flag is present.
                            if (first == 'u' && Next() == '{')
                                sawBracedUnicodeEscape = true;

                            // A LineTerminator (LF, CR, U+2028, U+2029) immediately
                            // after a backslash is never valid in a regex literal.
                            if (first is '\n' or '\r' or '\u2028' or '\u2029')
                                return false;

                            t.Append('\\');
                            t.Append(first);
                            break;

                        default:
                            t.Append(first);
                            break;
                    }

                    if (terminated)
                        break;

                    first = Consume();
                } while (true);

                regExp = t.ToString();
            }
            finally
            {
                sb.Clear();
            }

            // BROILER-PATCH: Validate parentheses balance in regex pattern (ES3 §15.10.1)
            // Reject patterns with unmatched ')' outside character classes
            {
                int depth = 0;
                bool cls = false;

                for (int vi = 0; vi < regExp.Length; vi++)
                {
                    char vc = regExp[vi];

                    if (vc == '\\' && vi + 1 < regExp.Length) { vi++; continue; }
                    if (cls) { if (vc == ']') cls = false; continue; }
                    if (vc == '[')
                    {
                        // ES3: ] immediately after [ closes the class (empty class)
                        if (vi + 1 < regExp.Length && regExp[vi + 1] == ']')
                        {
                            vi++; // skip ']'
                            continue;
                        }
                        cls = true;
                        continue;
                    }

                    if (vc == '(') depth++;
                    if (vc == ')') { depth--; if (depth < 0) { token = null; return false; } }
                }

                if (depth != 0) { token = null; return false; }
            }

            var flags = ScanFlags();

            // In u/v mode a `\u{...}` braced escape is a code point: decode it to the
            // fixed-width `\uHHHH` form (a surrogate pair for astral code points) the
            // Unicode validator and the RegExp runtime expect, mirroring what was
            // previously done inline. Without a u/v flag the escape is left verbatim so
            // the legacy `u`-plus-`{n}`-quantifier reading survives (`/\u{3}/` ≡ `u{3}`).
            if (sawBracedUnicodeEscape && (flags.IndexOf('u') >= 0 || flags.IndexOf('v') >= 0))
                regExp = DecodeBracedUnicodeEscapes(regExp);

            // we should test if it is a valid JSRegEx
            if (!RegExpValidator.IsValid(regExp, flags))
                return false;

            token = state.Commit(TokenTypes.RegExLiteral, regExp, flags);
            return true;
        }

        string ScanFlags()
        {
            var sb = pool.AllocateStringBuilder();
            var t = sb.Builder;
            var d = false;
            var g = false;
            var i = false;
            var m = false;
            var s = false;
            var u = false;
            var v = false;
            var y = false;

            try
            {
                do
                {
                    var ch = Peek();
                    switch (ch)
                    {
                        case 'd':
                            if (d) throw Unexpected();
                            d = true;
                            t.Append(ch);
                            Consume();
                            continue;

                        case 'g':
                            if (g) throw Unexpected();
                            g = true;
                            t.Append(ch);
                            Consume();
                            continue;

                        case 'i':
                            if (i) throw Unexpected();
                            i = true;
                            t.Append(ch);
                            Consume();
                            continue;

                        case 'm':
                            if (m) throw Unexpected();
                            m = true;
                            t.Append(ch);
                            Consume();
                            continue;

                        case 's':
                            if (s) throw Unexpected();
                            s = true;
                            t.Append(ch);
                            Consume();
                            continue;

                        case 'u':
                            if (u || v) throw Unexpected();
                            u = true;
                            t.Append(ch);
                            Consume();
                            continue;

                        case 'v':
                            if (v || u) throw Unexpected();
                            v = true;
                            t.Append(ch);
                            Consume();
                            continue;

                        case 'y':
                            if (y) throw Unexpected();
                            y = true;
                            t.Append(ch);
                            Consume();
                            continue;
                    }
                    break;
                } while (true);

                return sb.ToString();
            }
            finally
            {
                sb.Clear();
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private FastToken SkipMultilineComment(State state)
    {
        char ch = Peek();
        bool hasLineTerminator = ch.IsLineTerminator();
        do
        {
            ch = Consume();
            switch (ch)
            {
                case '\r':
                case '\n':
                case '\u2028':
                case '\u2029':
                    hasLineTerminator = true;
                    continue;

                case char.MaxValue:
                    if (hasLineTerminator)
                    {
                        return ReadSymbol(state, TokenTypes.LineTerminator);
                    }
                    return ReadToken();

                case '*':
                    while ((ch = Consume()) == '*') ;
                    if (ch == '/')
                    {
                        Consume();
                        break;
                    }
                    if (ch == char.MaxValue)
                    {
                        break;
                    }
                    continue;

                default:
                    continue;
            }
            break;
        } while (true);

        if (hasLineTerminator)
            // The closing `*/` has already been consumed, so `position` points at
            // the first character after the comment. Commit the LineTerminator token
            // directly — going through ReadSymbol would Consume() that next character
            // (swallowing e.g. the `v` of a following `var`, or a closing `'`), which
            // broke ASI after a MultiLineComment containing a line terminator.
            return state.Commit(TokenTypes.LineTerminator);

        return ReadToken();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private FastToken SkipSingleLineComment(State state, int prefixLength = 1)
    {
        for (var i = 0; i < prefixLength; i++)
        {
            if (Peek() == char.MaxValue)
                break;

            Consume();
        }

        var ch = Peek();
        while (!ch.IsLineTerminator() && ch != char.MaxValue)
        {
            ch = Consume();
        }

        return ReadSymbol(state, TokenTypes.LineTerminator);
    }

    private FastToken ReadString(State state, char first)
    {
        var start = first;
        var sb = pool.AllocateStringBuilder();
        var t = sb.Builder;

        try
        {
            do
            {
                first = Consume();

                if (first == char.MaxValue)
                    throw Unexpected();

                if (first == start)
                {
                    var next = Consume();
                    if (next == first)
                    {
                        t.Append(first);
                        continue;
                    }
                    else
                    {
                        break;
                    }
                }

                if (ScanEscaped(first, t))
                    continue;

                t.Append(first);

                if (first == start)
                    break;
            } while (true);

            return state.Commit(TokenTypes.String, sb.Builder);
        }
        finally
        {
            sb.Clear();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private FastToken ReadIdentifier(State state)
    {
        var sb = pool.AllocateStringBuilder();
        var builder = sb.Builder;
        var escaped = false;
        var start = true;

        try
        {
            while (true)
            {
                var current = Peek();
                if (current == '\\')
                {
                    Consume();
                    builder.Append(ReadIdentifierEscape(start));
                    escaped = true;
                    start = false;
                    continue;
                }

                if (char.IsHighSurrogate(current))
                {
                    var low = Next();
                    if (char.IsLowSurrogate(low))
                    {
                        var codePoint = char.ConvertToUtf32(current, low);
                        if (!(start ? codePoint.IsIdentifierStart() : codePoint.IsIdentifierPart()))
                            break;

                        builder.Append(current);
                        builder.Append(low);
                        Consume();
                        Consume();
                        start = false;
                        continue;
                    }
                }

                if (!(start ? current.IsIdentifierStart() : current.IsIdentifierPart()))
                    break;

                builder.Append(current);
                Consume();
                start = false;
            }

            return state.CommitIdentifier(keywords, escaped ? builder.ToString() : null);
        }
        finally
        {
            sb.Clear();
        }
    }

    private string ReadIdentifierEscape(bool start)
    {
        if (Peek() != 'u')
            throw Unexpected();

        Consume();

        int codePoint;
        var current = Peek();
        if (current == '{')
        {
            Consume();
            current = Peek();

            if (current == '}')
                throw Unexpected();

            codePoint = 0;
            while (current != char.MaxValue && current != '}')
            {
                if (!current.IsDigitPart(true, false))
                    throw Unexpected();

                codePoint = codePoint * 16 + current.HexValue();
                if (codePoint > 0x10FFFF)
                    throw Unexpected();
                Consume();
                current = Peek();
            }

            if (current != '}')
                throw Unexpected();

            Consume();
        }
        else
        {
            codePoint = 0;
            for (var i = 0; i < 4; i++)
            {
                current = Peek();
                if (!current.IsDigitPart(true, false))
                    throw Unexpected();

                codePoint = codePoint * 16 + current.HexValue();
                Consume();
            }
        }

        if (!(start ? codePoint.IsIdentifierStart() : codePoint.IsIdentifierPart()))
            throw Unexpected();

        return codePoint.FromCodePoint();
    }

    private FastToken ReadNumber(State state, char first)
    {
        void ConsumeDigits(bool hex = false, bool binary = false, bool octal = false)
        {
            char peek = Peek();
            if (!peek.IsDigitPart(hex, binary, octal))
                return;
            if (peek == '_')
                throw Unexpected(); // leading numeric separator
            bool lastWasSeparator = false;
            do
            {
                if (peek == '_')
                {
                    if (lastWasSeparator)
                        throw Unexpected(); // consecutive numeric separators
                    lastWasSeparator = true;
                }
                else
                {
                    lastWasSeparator = false;
                }
                peek = Consume();
            } while (peek.IsDigitPart(hex, binary, octal));
            if (lastWasSeparator)
                throw Unexpected(); // trailing numeric separator
        }

        FastToken CommitNumberToken(State s, TokenTypes type = TokenTypes.Number)
        {
            var p = Peek();
            if (p != char.MaxValue && p.IsIdentifierStart() && p != '$' && p != '@')
                throw Unexpected(); // identifier start after numeric literal
            return type == TokenTypes.BigInt
                ? s.Commit(TokenTypes.BigInt)
                : s.Commit(TokenTypes.Number, true);
        }

        // The `0x`/`0b`/`0o` radix prefixes can only begin the integer part. For a
        // leading-dot literal (`first == '.'`) there is no integer part — position is
        // already at the first fractional digit — so skip this block and fall through
        // to fractional/exponent handling (otherwise the leading `0` of e.g. `.0_1` is
        // mis-consumed here and the numeric separator is left dangling).
        if (first != '.' && Peek() == '0')
        {
            switch (Consume())
            {
                case 'x':
                case 'X':
                    Consume();
                    if (!Peek().IsDigitPart(true, false))
                        throw Unexpected(); // 0x without hex digits
                    ConsumeDigits(hex: true);
                    if (CanConsume('n'))
                        return CommitNumberToken(state, TokenTypes.BigInt);
                    return CommitNumberToken(state);

                case 'b':
                case 'B':
                    Consume();
                    if (!Peek().IsDigitPart(false, true))
                        throw Unexpected(); // 0b without binary digits
                    ConsumeDigits(binary: true);
                    if (CanConsume('n'))
                        return CommitNumberToken(state, TokenTypes.BigInt);
                    return CommitNumberToken(state);

                case 'o':
                case 'O':
                    Consume();
                    if (!Peek().IsDigitPart(false, false, true))
                        throw Unexpected(); // 0o without octal digits
                    ConsumeDigits(octal: true);
                    if (CanConsume('n'))
                        return CommitNumberToken(state, TokenTypes.BigInt);
                    return CommitNumberToken(state);
            }
        }

        ConsumeDigits();
        if (CanConsume('n'))
            return CommitNumberToken(state, TokenTypes.BigInt);

        // this logic is perfect
        // cannot be replaced with switch
        if (CanConsume('.'))
            ConsumeDigits();

        if (CanConsume('m'))
            return state.Commit(TokenTypes.Decimal);

        if (CanConsume('e', 'E'))
        {
            if (CanConsume('+', '-'))
            {
                ConsumeDigits();
                return CommitNumberToken(state);
            }

            ConsumeDigits();
            return CommitNumberToken(state);
        }

        ConsumeDigits();
        return CommitNumberToken(state);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public State Push() => new(this, position);

    [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
    public struct State(FastScanner scanner, int position)
    {
        private SpanLocation start = scanner.Location;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FastToken Commit(TokenTypes type, string cooked, string flags)
        {
            var cp = scanner.position;
            var start = scanner.Text.Offset + position;
            var location = scanner.Location;
            var token = new FastToken(type, scanner.Text.Source, cooked, flags, start, cp - start, this.start, location);
            scanner = null;
            return token;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FastToken Commit(TokenTypes type, bool parseNumber)
        {
            var cp = scanner.position;
            var start = scanner.Text.Offset + position;
            var location = scanner.Location;
            double number = 0;
            if (parseNumber)
            {
                var span = new StringSpan(scanner.Text.Source, start, cp - start);
                number = NumberCoercion.CoerceToNumber(span);
            }
            var token = new FastToken(type, scanner.Text.Source, null, null, start, cp - start, this.start, location, number: number);
            scanner = null;
            return token;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FastToken Commit(TokenTypes type, StringBuilder builder = null, bool cookedInvalid = false)
        {
            var cp = scanner.position;
            var start = scanner.Text.Offset + position;
            var location = scanner.Location;
            var token = new FastToken(type, scanner.Text.Source, builder?.ToString(), null, start, cp - start, this.start, location, cookedInvalid: cookedInvalid);
            scanner = null;
            return token;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            position = scanner.position;
            start = scanner.Location;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            if (scanner == null)
                return;

            scanner.position = position;
            scanner.line = start.Line;
            scanner.column = start.Column;
            scanner = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal FastToken CommitIdentifier(FastKeywordMap keywords, string? cooked = null)
        {
            var cp = scanner.position;
            var start = scanner.Text.Offset + position;
            var location = scanner.Location;

            var span = cooked != null
                ? new StringSpan(cooked)
                : new StringSpan(scanner.Text.Source, start, cp - start);
            var k = FastKeywords.none;
            bool isKw = cooked == null && keywords.IsKeyword(span, out k);
            var tokenType = TokenTypes.Identifier;
            var keyword = k;
            var contextualKeyword = FastKeywords.none;

            // Detect unicode-escaped reserved keywords:
            // When cooked is non-null, the identifier used unicode escapes.
            // Check if the cooked text resolves to a reserved keyword.
            bool escapedReserved = false;
            if (cooked != null && keywords.IsKeyword(span, out var ek))
            {
                switch (ek)
                {
                    // Contextual keywords are OK as identifiers when escaped
                    case FastKeywords.get:
                    case FastKeywords.set:
                    case FastKeywords.of:
                    case FastKeywords.constructor:
                    case FastKeywords.from:
                    case FastKeywords.@as:
                    case FastKeywords.@accessor:
                    case FastKeywords.@async:
                    case FastKeywords.@let:
                    case FastKeywords.@yield:
                    case FastKeywords.@static:
                    case FastKeywords.@await:
                    case FastKeywords.@implements:
                    case FastKeywords.@interface:
                    case FastKeywords.@package:
                    case FastKeywords.@private:
                    case FastKeywords.@public:
                    case FastKeywords.@protected:
                    case FastKeywords.@using:
                        break;
                    default:
                        // Truly reserved words: mark for parser rejection in binding contexts
                        escapedReserved = true;
                        break;
                }
            }

            if (isKw)
            {
                switch (k)
                {
                    case FastKeywords.instanceof:
                        isKw = false;
                        keyword = FastKeywords.none;
                        tokenType = TokenTypes.InstanceOf;
                        break;
                    case FastKeywords.@in:
                        isKw = false;
                        keyword = FastKeywords.none;
                        tokenType = TokenTypes.In;
                        break;
                    case FastKeywords.@null:
                        isKw = false;
                        tokenType = TokenTypes.Null;
                        keyword = FastKeywords.none;
                        break;
                    case FastKeywords.@true:
                        isKw = false;
                        tokenType = TokenTypes.True;
                        keyword = FastKeywords.none;
                        break;
                    case FastKeywords.@false:
                        isKw = false;
                        tokenType = TokenTypes.False;
                        keyword = FastKeywords.none;
                        break;
                    case FastKeywords.get:
                    case FastKeywords.set:
                    case FastKeywords.of:
                    case FastKeywords.constructor:
                    case FastKeywords.from:
                    case FastKeywords.@as:
                    case FastKeywords.@accessor:
                        isKw = false;
                        tokenType = TokenTypes.Identifier;
                        contextualKeyword = k;
                        keyword = FastKeywords.none;
                        break;
                }
            }

            var token = new FastToken(tokenType, scanner.Text.Source, cooked, null, start, cp - start, this.start, location, isKeyword: isKw, keyword: keyword, contextualKeyword: contextualKeyword, isEscapedReservedWord: escapedReserved);
            scanner = null;
            return token;
        }
    }
}
