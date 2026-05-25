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
            Token = nextToken;
            nextToken = ReadToken();
        }

        Token.Next = nextToken;
        nextToken.Previous = Token;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        while (char.IsWhiteSpace(first))
        {
            if (first == '\n')
                lineTerminator = true;

            first = Consume();
            skipped = true;
        }

        if (lineTerminator)
            return state.Commit(TokenTypes.LineTerminator);

        if (skipped)
            state = Push();

        if (first == '\\' || first.IsIdentifierStart())
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
                return ReadSymbol(state, TokenTypes.CurlyBracketStart);

            case '}':
                if (templateParts > 0)
                {
                    templateParts--;
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
                        Consume();
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
                        Consume();
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

        return EOF;
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

    private bool ScanEscaped(char next, StringBuilder t)
    {
        if (next != '\\')
            return false;

        next = Consume();

        switch (next)
        {
            /**
             * This is special case, slash followed by a single line terminator is
             * only used to break the string starting at next line
             */
            case '\n':
                return true;

            case 'u':
                if (CanConsumeNext('{'))
                {
                    t.Append(ScanUnicodeCodePointEscape());
                    return true;
                }

                if (ScanHexEscape(next, out var n))
                {
                    t.Append(n);
                    return true;
                }
                throw Unexpected();

            case 'x':
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
                // \0 followed by an octal digit (0-7) is a legacy octal escape
                var nextCh = Next();
                if (nextCh >= '0' && nextCh <= '7')
                {
                    // Legacy octal escape \0N: consume the octal digit
                    var octal = Consume();
                    t.Append((char)(octal - '0'));
                    return true;
                }
                next = '\0';
                break;

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

        // At least, one hex digit is required.
        if (ch == '}')
            throw Unexpected();

        Span<char> text = stackalloc char[10];
        int i = 0;

        text.Fill('0');
        text[0] = '\\';
        text[1] = 'U';

        while (ch != char.MaxValue)
        {
            if (!ch.IsDigitPart(true, false))
                break;

            for (int j = i; j > 0; j--)
                text[9 - j] = text[10 - j];

            text[9] = ch;
            ch = Consume();
            i++;
        }

        if (ch != '}')
            throw Unexpected();

        return new string(text.ToArray());
    }

    private FastToken ReadTemplateString(State state, TokenTypes part = TokenTypes.TemplateBegin)
    {
        var sb = pool.AllocateStringBuilder();
        var t = sb.Builder;

        try
        {
            do
            {
                char ch = Consume();
                switch (ch)
                {
                    case '$':
                        ch = Consume();
                        if (ch == '{')
                        {
                            Consume();
                            // template part begin...
                            templateParts++;
                            return state.Commit(part, t);
                        }

                        t.Append('$');
                        t.Append(ch);
                        continue;

                    case '`':
                        Consume();
                        return state.Commit(TokenTypes.TemplateEnd, t);

                    case char.MaxValue:
                        break;
                }

                if (ch == char.MaxValue)
                    throw Unexpected();

                if (ScanEscaped(ch, t))
                    continue;

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
                    FastKeywords.instanceof or FastKeywords.@in or FastKeywords.@typeof or FastKeywords.@return or FastKeywords.yield or FastKeywords.await => true,
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

                        case '\n':
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

                            if (first == 'u')
                            {
                                first = Consume();
                                if (CanConsume('{'))
                                {
                                    t.Append(ScanUnicodeCodePointEscape());
                                    break;
                                }
                                t.Append('\\');
                                t.Append('u');
                                t.Append(first);
                                break;
                            }

                            if (CanConsume('\n', '\r'))
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
        bool hasLineTerminator = ch == '\n' || ch == '\r';
        do
        {
            ch = Consume();
            switch (ch)
            {
                case '\r':
                case '\n':
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
            return ReadSymbol(state, TokenTypes.LineTerminator);

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
        while (ch != '\n' && ch != char.MaxValue)
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
                    return state.Commit(TokenTypes.String, sb.Builder);

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

        if (Peek() == '0')
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
        public FastToken Commit(TokenTypes type, StringBuilder builder = null)
        {
            var cp = scanner.position;
            var start = scanner.Text.Offset + position;
            var location = scanner.Location;
            var token = new FastToken(type, scanner.Text.Source, builder?.ToString(), null, start, cp - start, this.start, location);
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
