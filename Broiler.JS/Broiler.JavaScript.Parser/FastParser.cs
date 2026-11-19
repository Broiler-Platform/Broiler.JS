using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Statements;
using Broiler.JavaScript.Parser.Bridge;
using System;
using System.Runtime.CompilerServices;

namespace Broiler.JavaScript.Parser;

/// <summary>
/// The parser's facade over the bridge (window W2 of milestone SJL-14; decision D50, <c>facade-obsolete</c> in bundle
/// <c>sjl-14-001</c>'s <c>dispositions.md</c>): it keeps every public member, and parses the text its stream reads through
/// the bridge to the shared syntax library, which builds this engine's tree; a refusal throws today's exception type,
/// <see cref="FastParseException"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>The parse takes the bridge's hop</b> (decision D47 (a); the session's decision B7 of 2026-11-19, recorded in the
/// bundle's README): <see cref="ParseProgram"/> and <see cref="ParseModule(in StringSpan, FastParserOptions)"/> run the
/// library's parse and the tree's construction on a thread with the stack the library documents, as every compile does,
/// so the time from source text to the tree the compiler reads, which a caller of <see cref="ParseProgram"/> measures,
/// is the after quantity of the rule's part I.
/// </para>
/// <para>
/// Until W2 this class was the recursive-descent parser, in some thirty partial files; W2 deletes them (SJL-14.5's
/// deletion list). The members that parsed a piece of the program at the stream's position, <see cref="Export"/> and
/// <see cref="NonDeclarativeStatement"/>, parse the rest of the text from that position through the bridge and take its
/// first statement; <see cref="Combine"/> builds the node its operator names, as it did, and no longer parses: its
/// conditional form, which parsed the false branch, is not supported by the facade.
/// </para>
/// </remarks>
/// <param name="stream">The tokens to parse; the parser takes the stream's <see cref="FastTokenStream.Options"/>.</param>
[System.Obsolete(ObsoleteMarks.Parser, DiagnosticId = ObsoleteMarks.DiagnosticId)]
public class FastParser(FastTokenStream stream) : IParser
{
    private readonly FastTokenStream stream = stream;

    /// <summary>
    /// A parser over <paramref name="stream"/> that parses with <paramref name="options"/>, which must be
    /// the options the stream was made with.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> or <paramref name="options"/> is null.</exception>
    /// <exception cref="ArgumentException">The stream was made with other options.</exception>
    public FastParser(FastTokenStream stream, FastParserOptions options)
        : this(MadeWith(stream, options))
    {
    }

    private static FastTokenStream MadeWith(FastTokenStream stream, FastParserOptions options)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(options);
        if (!options.Equals(stream.Options))
            throw new ArgumentException(
                "The token stream was made with other options than the parser's: the stream's are " + stream.Options
                + " and the parser's " + options + ". Make the stream with the parser's options.",
                nameof(options));

        return stream;
    }

    /// <summary>The options this parser parses with: its stream's.</summary>
    public FastParserOptions Options { get; } = stream?.Options ?? FastParserOptions.Script;

    /// <summary>Kept as a facade member (decision D50); the bridge builds the tree's scopes, and nothing fills this one.</summary>
    public readonly FastScope variableScope = new FastScope();

    public StreamLocation BeginUndo() => new(this, stream.Current);

    public StreamLocation Location
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new StreamLocation(this, stream.Current);
    }

    public FastToken PreviousToken
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => stream.Previous;
    }

    [System.Obsolete(ObsoleteMarks.Parser, DiagnosticId = ObsoleteMarks.DiagnosticId)]
    public readonly struct StreamLocation(FastParser parser, FastToken token)
    {
        public readonly FastToken Token = token;

        public bool Reset()
        {
            parser.stream.Reset(Token);
            return false;
        }
    }

    /// <summary>Parses the stream's text, under the stream's options, through the bridge.</summary>
    /// <exception cref="FastParseException">The text is not a program of the options' goal.</exception>
    public AstProgram ParseProgram() => SharedSyntaxBridge.Parse(stream.Text, RequestFor(Options));

    /// <summary>
    /// Parses <paramref name="code"/> with the Module goal symbol. <see cref="AstProgram.IsAsync"/> reports whether the
    /// module contains a top-level <c>await</c> ([[HasTLA]]).
    /// </summary>
    /// <exception cref="FastParseException">The text is not a Module.</exception>
    public static AstProgram ParseModule(in StringSpan code) => ParseModule(code, FastParserOptions.Module);

    /// <summary>
    /// Parses <paramref name="code"/> with the Module goal symbol under <paramref name="options"/>, whose
    /// goal is <see cref="FastParserGoal.Module"/>: their top-level-<c>await</c> permission and their
    /// provider apply.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is null.</exception>
    /// <exception cref="ArgumentException">The options' goal is not the Module goal.</exception>
    /// <exception cref="FastParseException">The text is not a Module.</exception>
    public static AstProgram ParseModule(in StringSpan code, FastParserOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (!options.IsModule)
            throw new ArgumentException("ParseModule parses the Module goal, and the options' goal is " + options.Goal + ".", nameof(options));

        return SharedSyntaxBridge.Parse(code, RequestFor(options));
    }

    /// <summary>The bridge's request for a parse under <paramref name="options"/>: the goal, the top-level-await permission and the provider.</summary>
    internal static BridgeRequest RequestFor(FastParserOptions options) => new()
    {
        Goal = options.IsModule ? BridgeGoal.Module : BridgeGoal.Script,
        AllowTopLevelAwait = options.AllowTopLevelAwait,
        UnicodeTables = options.UnicodeTables,
    };

    /// <summary>Reads the string literal at the stream's position.</summary>
    /// <exception cref="FastParseException">The token there is not a string literal.</exception>
    public AstLiteral ExpectStringLiteral()
    {
        var token = stream.Expect(TokenTypes.String);
        return new AstLiteral(TokenTypes.String, token);
    }

    /// <summary>
    /// Parses the export declaration at the stream's position, whose <c>export</c> keyword is <paramref name="start"/>:
    /// the rest of the text from there, parsed as a module through the bridge, gives its first statement, and the stream
    /// moves past it.
    /// </summary>
    public bool Export(FastToken start, out AstStatement statement)
    {
        statement = FirstStatementFrom(start ?? stream.Current, BridgeGoal.Module);
        return statement is AstExportStatement;
    }

    /// <summary>
    /// Parses the statement at the stream's position: the rest of the text from there, parsed under the options' goal
    /// through the bridge, gives its first statement, and the stream moves past it. A declaration is not a
    /// non-declarative statement.
    /// </summary>
    public bool NonDeclarativeStatement(out AstStatement statement)
    {
        statement = FirstStatementFrom(stream.Current, Options.IsModule ? BridgeGoal.Module : BridgeGoal.Script);
        if (statement is AstVariableDeclaration { Kind: not FastVariableKind.Var }
            or AstExpressionStatement { Expression: AstFunctionExpression or AstClassExpression })
        {
            statement = null;
            return false;
        }

        return statement != null;
    }

    /// <summary>
    /// The node <paramref name="type"/> combines <paramref name="left"/> and <paramref name="right"/> into: the left
    /// operand alone when the right is absent or the type ends an expression, a member expression for <c>.</c> and
    /// <c>?.</c>, and a binary expression otherwise.
    /// </summary>
    /// <exception cref="NotSupportedException">The type is <c>?</c>, whose false branch the old parser parsed here.</exception>
    public AstExpression Combine(AstExpression left, TokenTypes type, AstExpression right, TokenTypes next = TokenTypes.SemiColon)
    {
        if (right == null)
            return left;

        switch (type)
        {
            case TokenTypes.SemiColon:
            case TokenTypes.EOF:
            case TokenTypes.BracketEnd:
            case TokenTypes.SquareBracketEnd:
            case TokenTypes.CurlyBracketEnd:
            case TokenTypes.LineTerminator:
                return left;

            case TokenTypes.QuestionMark:
                throw new NotSupportedException("The parser's facade does not parse a conditional expression's false branch; parse the program through ParseProgram.");

            case TokenTypes.Dot:
                return new AstMemberExpression(left, right);

            case TokenTypes.QuestionDot:
                return new AstMemberExpression(left, right, false, true);
        }

        return new AstBinaryExpression(left, type, right);
    }

    private AstStatement FirstStatementFrom(FastToken from, BridgeGoal goal)
    {
        var text = stream.Text;
        var source = text.Source ?? string.Empty;
        var offset = from?.Type == TokenTypes.EOF || from == null ? text.Offset + text.Length : from.Span.Offset;
        var rest = new StringSpan(source, offset, text.Offset + text.Length - offset);
        var program = SharedSyntaxBridge.Parse(rest, new BridgeRequest
        {
            Goal = goal,
            AllowTopLevelAwait = goal == BridgeGoal.Module || Options.AllowTopLevelAwait,
            UnicodeTables = Options.UnicodeTables,
        });
        if (program.Statements.Count == 0)
            return null;

        var statement = program.Statements[0];
        var end = statement.End?.Span is { Source: not null } span ? span.Offset + span.Length : offset;
        while (stream.Current.Type != TokenTypes.EOF && stream.Current.Span.Offset < end)
            stream.Consume();

        return statement;
    }
}
