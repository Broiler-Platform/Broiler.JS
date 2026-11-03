#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Runtime.Unicode;

namespace Broiler.JavaScript.Parser.Bridge;

/// <summary>
/// The bridge's token chain (milestone SJL-14.2, decision D44): the <see cref="FastToken"/>s the old tree's nodes
/// name as their first and last tokens, made from the shared tree's spans over the parse's source, each with its
/// source, offset, length and start and end locations, and linked into one chain in source order.
/// </summary>
/// <remarks>
/// <para>
/// <b>Where the facts come from.</b> A literal's or a name's token takes its facts from the shared tree's node (its
/// cooked value, number, flags, regular-expression body and flags, template value). A node's first and last tokens
/// take their extent from the node's span: the token that begins where the span begins, and the one that ends where
/// it ends. Decision D44 names the shared token stream as their source; the library keeps that stream internal at
/// SJL-13.2f (<c>ParseResult.Tokens</c>, "fixed when the first consumer reads it", its design's section 9.3), so the
/// bridge reads a boundary token's extent from the source text at the span's edge instead, which needs no scanning
/// of anything the library has not already accepted. When the library publishes its stream, this class reads it in
/// place of <see cref="LexemeAt"/> and <see cref="LexemeEndingAt"/>; nothing else changes.
/// </para>
/// <para>
/// <b>The chain is sparse</b>: it links the tokens the tree names, the parentheses of each parenthesised
/// expression among them, and not every token of the source. Its one reader, <c>SyntaxValidation</c>'s
/// parenthesis check (<c>Start.Previous</c> and <c>End.Next</c>), sees a parenthesis exactly where one encloses
/// the node, as it does on the old parser's chain.
/// </para>
/// <para>
/// <b>Positions</b> are those of <c>FastScanner</c>: offsets in the source string the parse's text is a span of,
/// lines and columns from 1 at the start of that span, every ECMAScript line terminator a new line and CR LF one.
/// </para>
/// </remarks>
internal sealed class BridgeTokens
{
    private readonly string source;
    private readonly int baseOffset;
    private readonly int length;
    private readonly List<int> lineStarts = new() { 0 };
    private readonly Dictionary<int, FastToken> byStart = new();
    private readonly Dictionary<int, FastToken> byEnd = new();
    private readonly List<FastToken> all = new();
    private FastToken? eof;

    internal BridgeTokens(in StringSpan code)
    {
        source = code.Source ?? string.Empty;
        baseOffset = code.Offset;
        length = code.Length;
        for (var i = 0; i < length; i++)
        {
            var c = source[baseOffset + i];
            if (c == '\r')
            {
                if (i + 1 < length && source[baseOffset + i + 1] == '\n')
                    i++;
                lineStarts.Add(i + 1);
            }
            else if (c is '\n' or '\u2028' or '\u2029')
            {
                lineStarts.Add(i + 1);
            }
        }
    }

    /// <summary>The source string every token's span is a part of.</summary>
    internal string Source => source;

    /// <summary>The offset in <see cref="Source"/> of the parse's first code unit.</summary>
    internal int BaseOffset => baseOffset;

    /// <summary>The offset in <see cref="Source"/> just past the parse's last code unit.</summary>
    internal int EndOffset => baseOffset + length;

    /// <summary>The location of <paramref name="offset"/>, an offset in <see cref="Source"/>.</summary>
    internal SpanLocation Location(int offset)
    {
        var relative = Math.Clamp(offset - baseOffset, 0, length);
        var index = lineStarts.BinarySearch(relative);
        if (index < 0)
            index = ~index - 1;
        return new SpanLocation(index + 1, relative - lineStarts[index] + 1);
    }

    /// <summary>The end-of-file token, at the end of the parse's text.</summary>
    internal FastToken Eof
    {
        get
        {
            if (eof != null)
                return eof;
            var location = Location(EndOffset);
            eof = new FastToken(TokenTypes.EOF, source, null, null, EndOffset, 0, location, location);
            return eof;
        }
    }

    /// <summary>A token of <paramref name="type"/> over <c>[start, start + count)</c>, registered in the chain.</summary>
    internal FastToken Make(TokenTypes type, int start, int count, string? cooked = null, string? flags = null, double number = 0,
        bool isKeyword = false, FastKeywords keyword = FastKeywords.none, FastKeywords contextualKeyword = FastKeywords.none,
        bool isEscapedReservedWord = false, bool cookedInvalid = false)
    {
        var token = new FastToken(type, source, cooked, flags, start, count, Location(start), Location(start + count), number,
            isKeyword, keyword, contextualKeyword, isEscapedReservedWord, cookedInvalid);
        Register(token, start, count);
        return token;
    }

    private void Register(FastToken token, int start, int count)
    {
        all.Add(token);
        byStart.TryAdd(start, token);
        byEnd[start + count] = token;
    }

    /// <summary>
    /// An identifier name's token: its keyword facts as <c>FastScanner.CommitIdentifier</c> gives them, the cooked
    /// name where it is written with an escape.
    /// </summary>
    internal FastToken Name(int start, int count, string name, bool escaped)
    {
        var (type, isKeyword, keyword, contextual, escapedReserved) = NameFacts(name, escaped);
        return Make(type, start, count, escaped ? name : null, isKeyword: isKeyword, keyword: keyword,
            contextualKeyword: contextual, isEscapedReservedWord: escapedReserved);
    }

    /// <summary><c>FastToken.AsString</c>: the same span as a string token, outside the chain, as the old parser made it.</summary>
    internal static FastToken AsString(FastToken token) => token.AsString();

    /// <summary>The token the source text has at <paramref name="start"/>, made once and shared.</summary>
    internal FastToken At(int start)
    {
        if (start >= EndOffset)
            return Eof;
        if (byStart.TryGetValue(start, out var known))
            return known;
        return LexemeAt(start);
    }

    /// <summary>The token the source text has ending at <paramref name="end"/>, made once and shared.</summary>
    internal FastToken EndingAt(int end)
    {
        if (byEnd.TryGetValue(end, out var known))
            return known;
        return LexemeEndingAt(end);
    }

    /// <summary>The first token at or after <paramref name="offset"/>, past white space and comments, or end of file.</summary>
    internal FastToken After(int offset) => At(SkipTrivia(offset));

    /// <summary>
    /// The program's first token, at the parse's first code unit: the source's first token where nothing comes before
    /// it, and otherwise a token over the white space and comments before it, outside the chain, as the old scanner
    /// began a source with a leading comment at that comment.
    /// </summary>
    internal FastToken Leading()
    {
        var first = SkipTrivia(baseOffset);
        if (first == baseOffset)
            return At(baseOffset);
        return new FastToken(TokenTypes.LineTerminator, source, null, null, baseOffset, first - baseOffset, Location(baseOffset), Location(first));
    }

    /// <summary>Links every token made into one chain in source order.</summary>
    internal void Link()
    {
        all.Sort(static (a, b) => a.Span.Offset != b.Span.Offset ? a.Span.Offset.CompareTo(b.Span.Offset) : a.Span.Length.CompareTo(b.Span.Length));
        FastToken? previous = null;
        foreach (var token in all)
        {
            if (previous != null && ReferenceEquals(previous, token))
                continue;
            token.Previous = previous;
            if (previous != null)
                previous.Next = token;
            previous = token;
        }

        if (previous != null && eof != null)
        {
            previous.Next = eof;
            eof.Previous = previous;
        }
    }

    // The lexeme at a node's start: a name (with escapes), a punctuator, or, rarely, a literal's extent.
    private FastToken LexemeAt(int start)
    {
        var c = source[start];
        if (IsNameStart(start) || c == '\\')
        {
            var end = ScanName(start);
            var raw = source.Substring(start, end - start);
            var escaped = raw.IndexOf('\\') >= 0;
            return Name(start, end - start, escaped ? Unescape(raw) : raw, escaped);
        }

        if (c is '"' or '\'')
            return Make(TokenTypes.String, start, ScanQuoted(start) - start);

        if (char.IsAsciiDigit(c) || (c == '.' && start + 1 < EndOffset && char.IsAsciiDigit(source[start + 1])))
            return Make(TokenTypes.Number, start, ScanNumber(start) - start);

        for (var count = Math.Min(4, EndOffset - start); count > 0; count--)
        {
            var type = Punctuator(source.AsSpan(start, count));
            if (type != TokenTypes.None)
                return Make(type, start, count);
        }

        return Make(TokenTypes.None, start, 1);
    }

    // The lexeme that ends where a node ends: a closing punctuator, a name, `++`/`--`, or another punctuator.
    private FastToken LexemeEndingAt(int end)
    {
        if (end <= baseOffset)
            return At(baseOffset);
        if (NamePartEndingAt(end) > 0)
        {
            var start = end - NamePartEndingAt(end);
            while (start > baseOffset && NamePartEndingAt(start) > 0)
                start -= NamePartEndingAt(start);
            if (start > baseOffset && source[start - 1] == '\\')
                start--;
            if (char.IsAsciiDigit(source[start]) || (start > baseOffset && source[start - 1] == '.' && char.IsAsciiDigit(source[start])))
                return At(start);
            return At(start);
        }

        for (var count = Math.Min(4, end - baseOffset); count > 0; count--)
        {
            var start = end - count;
            var type = Punctuator(source.AsSpan(start, count));
            if (type == TokenTypes.None)
                continue;
            if (byStart.TryGetValue(start, out var known) && known.Span.Length == count)
                return known;
            return Make(type, start, count);
        }

        return Make(TokenTypes.None, end - 1, 1);
    }

    /// <summary>The offset of the first code unit at or after <paramref name="offset"/> that is not white space, a line terminator or a comment.</summary>
    internal int SkipTrivia(int offset)
    {
        var i = offset;
        var end = EndOffset;
        while (i < end)
        {
            var c = source[i];
            if (JSUnicode.IsWhiteSpace(c) || c is '\n' or '\r' or '\u2028' or '\u2029')
            {
                i++;
                continue;
            }

            if (c == '/' && i + 1 < end && source[i + 1] == '/')
            {
                i += 2;
                while (i < end && source[i] is not ('\n' or '\r' or '\u2028' or '\u2029'))
                    i++;
                continue;
            }

            if (c == '/' && i + 1 < end && source[i + 1] == '*')
            {
                var close = source.IndexOf("*/", i + 2, end - i - 2, StringComparison.Ordinal);
                i = close < 0 ? end : close + 2;
                continue;
            }

            break;
        }

        return i;
    }

    private int ScanName(int start)
    {
        var i = start;
        while (i < EndOffset)
        {
            var c = source[i];
            if (c == '\\')
            {
                i++;
                if (i < EndOffset && source[i] == 'u')
                {
                    i++;
                    if (i < EndOffset && source[i] == '{')
                    {
                        while (i < EndOffset && source[i] != '}')
                            i++;
                        i++;
                    }
                    else
                    {
                        i += 4;
                    }
                }

                continue;
            }

            var width = NamePartAt(i);
            if (width == 0)
                break;
            i += width;
        }

        return Math.Min(i, EndOffset);
    }

    private int ScanQuoted(int start)
    {
        var quote = source[start];
        var i = start + 1;
        while (i < EndOffset && source[i] != quote)
            i += source[i] == '\\' ? 2 : 1;
        return Math.Min(i + 1, EndOffset);
    }

    private int ScanNumber(int start)
    {
        var i = start;
        while (i < EndOffset && (char.IsAsciiLetterOrDigit(source[i]) || source[i] is '.' or '_'))
            i++;
        return i;
    }

    // The engine's own identifier classes (ID_Start and ID_Continue with `$`, `_`, ZWNJ and ZWJ, as FastScanner reads
    // them), over the code point at an offset or ending at one: its width in code units, or 0 where it is no part of a
    // name.
    private bool IsNameStart(int offset)
    {
        var codePoint = CodePointAt(offset, out _);
        return codePoint >= 0 && JSUnicode.IsIdentifierStart(codePoint);
    }

    private int NamePartAt(int offset)
    {
        var codePoint = CodePointAt(offset, out var width);
        return codePoint >= 0 && JSUnicode.IsIdentifierPart(codePoint) ? width : 0;
    }

    private int NamePartEndingAt(int end)
    {
        if (end <= baseOffset)
            return 0;
        var low = source[end - 1];
        var start = char.IsLowSurrogate(low) && end - 2 >= baseOffset && char.IsHighSurrogate(source[end - 2]) ? end - 2 : end - 1;
        return NamePartAt(start) > 0 && start + NamePartAt(start) == end ? end - start : 0;
    }

    private int CodePointAt(int offset, out int width)
    {
        width = 0;
        if (offset >= EndOffset)
            return -1;
        var c = source[offset];
        if (char.IsHighSurrogate(c) && offset + 1 < EndOffset && char.IsLowSurrogate(source[offset + 1]))
        {
            width = 2;
            return char.ConvertToUtf32(c, source[offset + 1]);
        }

        width = 1;
        return char.IsSurrogate(c) ? -1 : c;
    }

    // A name with \u escapes, decoded; the shared tree gives every name it holds, so this serves only a boundary
    // token that is a keyword written with an escape.
    private static string Unescape(string raw)
    {
        var builder = new System.Text.StringBuilder(raw.Length);
        for (var i = 0; i < raw.Length; i++)
        {
            if (raw[i] == '\\' && i + 1 < raw.Length && raw[i + 1] == 'u')
            {
                int value;
                if (i + 2 < raw.Length && raw[i + 2] == '{')
                {
                    var close = raw.IndexOf('}', i + 3);
                    if (close < 0 || !int.TryParse(raw.AsSpan(i + 3, close - i - 3), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value))
                        return raw;
                    i = close;
                }
                else
                {
                    if (i + 6 > raw.Length || !int.TryParse(raw.AsSpan(i + 2, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value))
                        return raw;
                    i += 5;
                }

                if (value <= 0xFFFF)
                    builder.Append((char)value);
                else if (value <= 0x10FFFF)
                    builder.Append(char.ConvertFromUtf32(value));
                else
                    return raw;
                continue;
            }

            builder.Append(raw[i]);
        }

        return builder.ToString();
    }

    /// <summary>The facts <c>FastScanner.CommitIdentifier</c> gives a name's token.</summary>
    internal static (TokenTypes Type, bool IsKeyword, FastKeywords Keyword, FastKeywords Contextual, bool EscapedReserved) NameFacts(string name, bool escaped)
    {
        var known = Enum.TryParse<FastKeywords>(name, ignoreCase: false, out var keyword)
            && keyword != FastKeywords.none
            && name.Length > 0
            && char.IsAsciiLetterLower(name[0]);
        if (escaped)
        {
            var escapedReserved = known && keyword is not (FastKeywords.get or FastKeywords.set or FastKeywords.of
                or FastKeywords.constructor or FastKeywords.from or FastKeywords.@as or FastKeywords.@accessor
                or FastKeywords.async or FastKeywords.let or FastKeywords.yield or FastKeywords.@static or FastKeywords.await
                or FastKeywords.implements or FastKeywords.@interface or FastKeywords.package or FastKeywords.@private
                or FastKeywords.@public or FastKeywords.@protected or FastKeywords.@using);
            return (TokenTypes.Identifier, false, FastKeywords.none, FastKeywords.none, escapedReserved);
        }

        if (!known)
            return (TokenTypes.Identifier, false, FastKeywords.none, FastKeywords.none, false);

        return keyword switch
        {
            FastKeywords.instanceof => (TokenTypes.InstanceOf, false, FastKeywords.none, FastKeywords.none, false),
            FastKeywords.@in => (TokenTypes.In, false, FastKeywords.none, FastKeywords.none, false),
            FastKeywords.@null => (TokenTypes.Null, false, FastKeywords.none, FastKeywords.none, false),
            FastKeywords.@true => (TokenTypes.True, false, FastKeywords.none, FastKeywords.none, false),
            FastKeywords.@false => (TokenTypes.False, false, FastKeywords.none, FastKeywords.none, false),
            FastKeywords.get or FastKeywords.set or FastKeywords.of or FastKeywords.constructor or FastKeywords.from
                or FastKeywords.@as or FastKeywords.accessor => (TokenTypes.Identifier, false, FastKeywords.none, keyword, false),
            _ => (TokenTypes.Identifier, true, keyword, FastKeywords.none, false),
        };
    }

    // FastScanner's punctuators, by their text; None for any other text.
    private static TokenTypes Punctuator(ReadOnlySpan<char> text) => text switch
    {
        ">>>=" => TokenTypes.AssignUnsignedRightShift,
        "..." => TokenTypes.TripleDots,
        "===" => TokenTypes.StrictlyEqual,
        "!==" => TokenTypes.StrictlyNotEqual,
        "**=" => TokenTypes.AssignPower,
        "<<=" => TokenTypes.AssignLeftShift,
        ">>=" => TokenTypes.AssignRightShift,
        ">>>" => TokenTypes.UnsignedRightShift,
        "&&=" => TokenTypes.AssignBooleanAnd,
        "||=" => TokenTypes.AssignBooleanOr,
        "??=" => TokenTypes.AssignCoalesce,
        "=>" => TokenTypes.Lambda,
        "==" => TokenTypes.Equal,
        "!=" => TokenTypes.NotEqual,
        "<=" => TokenTypes.LessOrEqual,
        ">=" => TokenTypes.GreaterOrEqual,
        "&&" => TokenTypes.BooleanAnd,
        "||" => TokenTypes.BooleanOr,
        "??" => TokenTypes.Coalesce,
        "?." => TokenTypes.QuestionDot,
        "++" => TokenTypes.Increment,
        "--" => TokenTypes.Decrement,
        "+=" => TokenTypes.AssignAdd,
        "-=" => TokenTypes.AssignSubtract,
        "*=" => TokenTypes.AssignMultiply,
        "/=" => TokenTypes.AssignDivide,
        "%=" => TokenTypes.AssignMod,
        "&=" => TokenTypes.AssignBitwideAnd,
        "|=" => TokenTypes.AssignBitwideOr,
        "^=" => TokenTypes.AssignXor,
        "**" => TokenTypes.Power,
        "<<" => TokenTypes.LeftShift,
        ">>" => TokenTypes.RightShift,
        "(" => TokenTypes.BracketStart,
        ")" => TokenTypes.BracketEnd,
        "[" => TokenTypes.SquareBracketStart,
        "]" => TokenTypes.SquareBracketEnd,
        "{" => TokenTypes.CurlyBracketStart,
        "}" => TokenTypes.CurlyBracketEnd,
        ";" => TokenTypes.SemiColon,
        "," => TokenTypes.Comma,
        "." => TokenTypes.Dot,
        "?" => TokenTypes.QuestionMark,
        ":" => TokenTypes.Colon,
        "#" => TokenTypes.Hash,
        "@" => TokenTypes.At,
        "!" => TokenTypes.Negate,
        "~" => TokenTypes.BitwiseNot,
        "+" => TokenTypes.Plus,
        "-" => TokenTypes.Minus,
        "*" => TokenTypes.Multiply,
        "/" => TokenTypes.Divide,
        "%" => TokenTypes.Mod,
        "<" => TokenTypes.Less,
        ">" => TokenTypes.Greater,
        "=" => TokenTypes.Assign,
        "&" => TokenTypes.BitwiseAnd,
        "^" => TokenTypes.Xor,
        "|" => TokenTypes.BitwiseOr,
        _ => TokenTypes.None,
    };
}
