using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Parser.Bridge;
using Broiler.JavaScript.Runtime;
using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Broiler.JavaScript.Parser;

/// <summary>
/// The token stream's facade over the bridge's token chain (window W2 of milestone SJL-14; decision D50,
/// <c>facade-obsolete</c> in bundle <c>sjl-14-001</c>'s <c>dispositions.md</c>): it keeps every public member, and its tokens
/// are the shared syntax library's, as the bridge makes them into this engine's <see cref="FastToken"/>s, linked in source
/// order with a line-terminator token where a line terminator stands between two (<see cref="SharedSyntaxBridge.TokenChain"/>).
/// </summary>
/// <remarks>
/// Until W2 it drove the old scanner and kept the high-water mark of a parse for the old parser's reports; W2
/// deletes both (SJL-14.5's deletion list). The tokens are made the first time one is read, by a parse of the whole text
/// under the options' goal: <c>FastParser</c>'s facade parses the text itself and reads no token, and a text the library
/// refuses throws the refusal, a <see cref="FastParseException"/>, at the first read.
/// </remarks>
[System.Obsolete(ObsoleteMarks.Parser, DiagnosticId = ObsoleteMarks.DiagnosticId)]
public class FastTokenStream
{
    private readonly StringSpan text;
    public readonly FastPool Pool;

    /// <summary>The text the stream reads, which <see cref="FastParser"/>'s facade parses through the bridge.</summary>
    internal StringSpan Text => text;

    public override string ToString() => $"{Current} {Next}";

    public readonly FastKeywordMap Keywords;
    FastToken current;

    /// <summary>
    /// The options the stream was made with, which a parser over the stream parses with.
    /// </summary>
    public FastParserOptions Options { get; }

    /// <summary>
    /// A stream over <paramref name="text"/> with a pool of its own and
    /// <see cref="FastParserOptions.Script"/>: a script without top-level <c>await</c>, over the
    /// engine's own Unicode tables.
    /// </summary>
    public FastTokenStream(in StringSpan text, FastKeywordMap keywords = null)
        : this(new FastPool(), text, keywords, FastParserOptions.Script)
    {
    }

    /// <summary>A stream over <paramref name="text"/> with a pool of its own and <paramref name="options"/>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is null.</exception>
    public FastTokenStream(in StringSpan text, FastKeywordMap keywords, FastParserOptions options)
        : this(new FastPool(), text, keywords, options)
    {
    }

    /// <summary>
    /// A stream over <paramref name="text"/> drawing on <paramref name="pool"/>, with
    /// <see cref="FastParserOptions.Script"/>: a script without top-level <c>await</c>, over the
    /// engine's own Unicode tables.
    /// </summary>
    public FastTokenStream(FastPool pool, in StringSpan text, FastKeywordMap keywords = null)
        : this(pool, text, keywords, FastParserOptions.Script)
    {
    }

    /// <summary>
    /// A stream over <paramref name="text"/> drawing on <paramref name="pool"/>, with
    /// <paramref name="options"/>, whose goal the bridge parses the text under for its tokens.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is null.</exception>
    public FastTokenStream(FastPool pool, in StringSpan text, FastKeywordMap keywords, FastParserOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        Pool = pool;
        Keywords = keywords ?? FastKeywordMap.Instance;
        Options = options;
        this.text = text;
    }

    /// <summary>
    /// Kept as a facade member: the library decides whether a <c>/</c> after <c>yield</c> begins a regular-expression
    /// literal from the parse's own context, so the value is read by nothing.
    /// </summary>
    public bool YieldIsKeyword { set { } }

    /// <summary>The current token.</summary>
    /// <exception cref="FastParseException">The library refused the text.</exception>
    public FastToken Current => current ??= SharedSyntaxBridge.TokenChain(text, FastParser.RequestFor(Options));

    public FastToken Next => Current.Next;

    public FastToken Previous => Current.Previous ?? Current;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FastToken Expect(TokenTypes type)
    {
        SkipNewLines();
        
        var c = Current;
        if (c.Type != type)
            throw new FastParseException(c, string.Create(CultureInfo.InvariantCulture, $"Expecting {type} at {c.Start.Line}, {c.Start.Column}"));

        Consume();
        return c;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FastToken Expect(FastKeywords type)
    {
        SkipNewLines();
        
        var c = Current;
        if (c.Keyword != type)
            throw new FastParseException(c, string.Create(CultureInfo.InvariantCulture, $"Expecting keyword {type} at {c.Start.Line}, {c.Start.Column}"));
        
        Consume();
        return c;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FastToken ExpectContextualKeyword(FastKeywords type)
    {
        SkipNewLines();
        
        var c = Current;
        if (c.ContextualKeyword != type)
            throw new FastParseException(c, string.Create(CultureInfo.InvariantCulture, $"Expecting keyword {type} at {c.Start.Line}, {c.Start.Column}"));
        
        Consume();
        return c;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool CheckAndConsumeContextualKeyword(FastKeywords keyword)
    {
        var m = SkipNewLines();
        var c = Current;
        
        if (c.ContextualKeyword == keyword)
        {
            Consume();
            return true;
        }
        
        m.Undo();
        return false;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool CheckAndConsume(FastKeywords keyword)
    {
        var m = SkipNewLines();
        var c = Current;
        
        if (c.Keyword == keyword)
        {
            Consume();
            return true;
        }
        
        m.Undo();
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool CheckAndConsumeNoLineTerminator(TokenTypes type)
    {
        var c = Current;

        if (c.Type == type)
        {
            Consume();
            return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool CheckAndConsume(TokenTypes type)
    {
        var m = SkipNewLines();
        var c = Current;
        
        if (c.Type == type)
        {
            Consume();
            return true;
        }
        
        m.Undo();
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool CheckAndConsumeAny(TokenTypes type1, TokenTypes type2)
    {
        var m = SkipNewLines();
        var c = Current.Type;
        
        if (c == type1 || c == type2)
        {
            Consume();
            return true;
        }
        
        m.Undo();
        return false;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool CheckAndConsumeWithLineTerminator(TokenTypes type)
    {
        var m = SkipNewLines();
        var c = Current.Type;

        if (c == type)
        {
            Consume();
            return true;
        }

        if (m.LinesSkipped)
            return true;

        m.Undo();
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool CheckAndConsumeWithLineTerminator(TokenTypes type1, TokenTypes type2)
    {
        var m = SkipNewLines();
        var c = Current.Type;

        if (c == type1 || c == type2)
        {
            Consume();
            return true;
        }

        if (m.LinesSkipped)
            return true;

        m.Undo();
        return false;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool CheckAndConsumeAny(TokenTypes type1, TokenTypes type2, TokenTypes type3)
    {
        var m = SkipNewLines();
        var c = Current.Type;

        if (c == type1 || c == type2 || c == type3)
        {
            Consume();
            return true;
        }

        m.Undo();
        return false;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool CheckAndConsumeAny(TokenTypes type1, TokenTypes type2, TokenTypes type3, TokenTypes type4)
    {
        var m = SkipNewLines();
        var ct = Current.Type;

        if (ct == type1 || ct == type2 || ct == type3 || ct == type4)
        {
            Consume();
            return true;
        }

        m.Undo();
        return false;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool CheckAndConsume(TokenTypes type, out FastToken token)
    {
        var m = SkipNewLines();
        var c = Current;

        if (c.Type == type)
        {
            token = c;
            Consume();
            return true;
        }

        m.Undo();
        token = null;
        return false;
    }

    public readonly struct Marker(FastTokenStream stream, FastToken index, bool linesSkipped)
    {
        public readonly bool LinesSkipped = linesSkipped;

        public void Undo() => stream.current = index;
    }

    public Marker SkipNewLines()
    {
        var index = Current;
        var c = index.Type;
        bool skipped = false;

        while (c == TokenTypes.LineTerminator)
        {
            c = Consume().Type;
            skipped = true;
        }

        return new Marker(this, index, skipped);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool CheckAndConsume(TokenTypes type1, TokenTypes type2, out FastToken token1, out FastToken token2)
    {
        var marker = SkipNewLines();
        var c = Current;

        if (c.Type == type1)
        {
            token1 = c;
            SkipNewLines();
            c = Next;
        
            if (c.Type == type2)
            {
                Consume();
                Consume();
                token2 = c;
                return true;
            }
        }

        marker.Undo();
        token1 = null;
        token2 = null;
        return false;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FastToken Consume()
    {
        var c = Current;
        if (c.Type != TokenTypes.EOF && c.Next != null)
            current = c.Next;
        return current;
    }

    /// <summary>
    /// A mark of the current position, which returns the stream to it when disposed unless cancelled or
    /// committed first.
    /// </summary>
    /// <remarks>
    /// Obsolete since milestone SJL-12.5, under decision SJL-D-25 (b): the parser does not call it, and
    /// <see cref="Current"/> with <see cref="Reset(FastToken)"/> does what it does without an allocation.
    /// It is kept, unchanged, until a recorded check of its consumers allows its removal (SJL-D-25 (c)).
    /// </remarks>
    [Obsolete("Read Current to mark the position and call Reset(FastToken) to return to it. UndoMark is kept until a recorded check of its consumers allows its removal (decision SJL-D-25 (c)).")]
    public CancellableDisposableAction UndoMark()
    {
        var i = current;
        return new CancellableDisposableAction(() => current = i);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Reset(FastToken position)
    {
        current = position;
        return false;
    }
}
