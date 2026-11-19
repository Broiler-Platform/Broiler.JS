using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Parser.Bridge;
using Broiler.JavaScript.Runtime.Unicode;
using System;
using System.Text;

namespace Broiler.JavaScript.Parser;

/// <summary>
/// The scanner's facade over the bridge's token chain (window W2 of milestone SJL-14; decision D50, <c>facade-obsolete</c>
/// in bundle <c>sjl-14-001</c>'s <c>dispositions.md</c>): it keeps every public member, and its tokens are the shared syntax
/// library's, as the bridge makes them into this engine's <see cref="FastToken"/>s (<see cref="SharedSyntaxBridge.TokenChain"/>).
/// </summary>
/// <remarks>
/// <para>
/// Until W2 this class was the old scanner: its cursor, its readers and its classifiers, the speculative
/// regular-expression scan whose refusal fell back to division among them (row S9). W2 deletes that code (SJL-14.5's
/// deletion list, with the division fallback, correction K5): where a <c>/</c> begins a literal is the library's to decide
/// as it parses, and a literal Broiler.Regex refuses is refused, never read as a division.
/// </para>
/// <para>
/// The tokens are made the first time one is read, by a parse of the whole text under the options' goal, so a text the
/// library refuses throws the refusal, a <see cref="FastParseException"/>, at that read rather than at construction.
/// <see cref="YieldIsKeyword"/> is kept and read by nothing: the library knows the generator context it parses in.
/// </para>
/// </remarks>
[System.Obsolete(ObsoleteMarks.Parser, DiagnosticId = ObsoleteMarks.DiagnosticId)]
public class FastScanner
{
    public readonly StringSpan Text;

    /// <summary>Kept as a facade member; the library decides a <c>/</c> after <c>yield</c> from the parse's own context.</summary>
    public bool YieldIsKeyword;

    private FastToken token;
    private BridgeTokens positions;

    /// <summary>The options the scanner reads under: their goal and their provider.</summary>
    public FastParserOptions Options { get; }

    /// <summary>
    /// A scanner over <paramref name="text"/> with <see cref="FastParserOptions.Script"/>: a script
    /// without top-level <c>await</c>, over the engine's own Unicode tables.
    /// </summary>
    public FastScanner(FastPool pool, in StringSpan text, FastKeywordMap keywords = null)
        : this(pool, text, keywords, FastParserOptions.Script)
    {
    }

    /// <summary>A scanner over <paramref name="text"/> with <paramref name="options"/>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is null.</exception>
    public FastScanner(FastPool pool, in StringSpan text, FastKeywordMap keywords, FastParserOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        Text = text;
        Options = options;
    }

    /// <summary>The current token.</summary>
    /// <exception cref="FastParseException">The library refused the text.</exception>
    public FastToken Token => token ??= SharedSyntaxBridge.TokenChain(Text, FastParser.RequestFor(Options));

    /// <summary>Where the current token begins.</summary>
    public SpanLocation Location => Token.Start;

    /// <summary>Moves to the token after the current one; the end of file stays.</summary>
    public void ConsumeToken()
    {
        var current = Token;
        if (current.Type != TokenTypes.EOF)
            token = current.Next ?? current;
    }

    /// <summary>The refusal of the current token, as the old scanner worded it.</summary>
    public Exception Unexpected()
    {
        var current = Token;
        return new FastParseException(current, $"Unexpected token {current.Type}: {current.Span} at {current.Start}");
    }

    /// <summary>A state at the current token's start, which commits a token from there to where the scanner then is.</summary>
    public State Push() => new(this, Offset - Text.Offset);

    // The offset in Text.Source where the current token begins.
    private int Offset => Token.Type == TokenTypes.EOF ? Text.Offset + Text.Length : Token.Span.Offset;

    private SpanLocation At(int offset) => (positions ??= new BridgeTokens(Text)).Location(offset);

    private void ResetTo(int offset)
    {
        var current = Token;
        while (current.Previous != null && current.Type != TokenTypes.EOF && current.Span.Offset > offset)
            current = current.Previous;
        while (current.Previous != null && current.Type == TokenTypes.EOF && current.Previous.Span.Offset >= offset)
            current = current.Previous;
        token = current;
    }

    /// <summary>
    /// A state of the scanner: the offset, from the start of <see cref="Text"/>, it was pushed at. A commit makes a token
    /// of the text from there to where the scanner is.
    /// </summary>
    [System.Obsolete(ObsoleteMarks.Parser, DiagnosticId = ObsoleteMarks.DiagnosticId)]
    public struct State(FastScanner scanner, int start)
    {
        public FastToken Commit(TokenTypes type, string cooked, string flags) => Make(type, cooked, flags, 0, false);

        public FastToken Commit(TokenTypes type, double number) => Make(type, null, null, number, false);

        /// <summary>The source text from where this state was pushed to where the scanner is.</summary>
        public readonly ReadOnlySpan<char> Scanned()
        {
            var from = scanner.Text.Offset + start;
            return scanner.Text.Source.AsSpan(from, Math.Max(0, scanner.Offset - from));
        }

        public FastToken Commit(TokenTypes type, StringBuilder builder = null, bool cookedInvalid = false)
            => Make(type, builder?.ToString(), null, 0, cookedInvalid);

        /// <summary>Moves the state to where the scanner is.</summary>
        public void Reset() => start = scanner.Offset - scanner.Text.Offset;

        /// <summary>Returns the scanner to the state's offset.</summary>
        public void Dispose() => scanner.ResetTo(scanner.Text.Offset + start);

        private readonly FastToken Make(TokenTypes type, string cooked, string flags, double number, bool cookedInvalid)
        {
            var from = scanner.Text.Offset + start;
            var to = Math.Max(from, scanner.Offset);
            return new FastToken(type, scanner.Text.Source, cooked, flags, from, to - from, scanner.At(from), scanner.At(to),
                number: number, cookedInvalid: cookedInvalid);
        }
    }

    /// <summary>
    /// Whether a regular-expression literal's body and flags are a RegularExpressionLiteral the specification admits
    /// (ECMA-262 13.2.7.2, IsValidRegularExpressionLiteral): Broiler.Regex's validate entry, under the nesting limit
    /// this engine compiles under (Broiler.VM's JSD-0081, decisions D7 and D22). The bridge's regular-expression provider
    /// answers the library with it (decision D51; kept here at W2 by the session's decision B10 of 2026-11-19).
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 13.2.7.2 and JSD-0081 decision D22; IP=Low; Security=Medium; Resources=2; Fingerprint=TBF
    // Broiler-Falsified-If: the literal /(?<=a)?/ or /[(]/v is admitted, or /\u{1F600}/u is refused
    // Broiler-Human:        PENDING
    internal static bool IsValidRegularExpressionLiteral(string pattern, string flags)
        => Broiler.Regex.BroilerRegex.Validate(pattern, flags, JSUnicode.Tables, Broiler.Regex.BroilerRegex.MaximumNestingLimit,
            out _, out _) == Broiler.Regex.RegexStatus.Success;
}
