// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// HAIRS Code Assurance
// --------------------
// Relevant units:   8
// Annotated:        8/8
// Exempt:           74
// Human-reviewed:   0/8
// IP risk:          Low
// Security risk:    Medium
// Criteria:         0/0
// Resource impact:  1/10 max
// Unverified:       8
//
// GENERATED - DO NOT EDIT MANUALLY

namespace Broiler.ECMAScript.Syntax;

/// <summary>
/// One input element (ECMA-262 clause 12) and its raw facts: where it is, what it is, its value where the lexical grammar
/// defines one, and the facts the parser reads for strict mode, templates and automatic semicolon insertion (Broiler.VM's
/// JSD-0080, the output's "raw facts"; JSD-0083, decisions D15 and D21).
/// </summary>
/// <remarks>
/// <para>
/// <b>Positions</b> (decision D15): <see cref="Start"/> and <see cref="End"/> are 0-based offsets into the request's text,
/// in UTF-16 code units, the source's base offset not added; <see cref="Line"/> and <see cref="Column"/> are 1-based, with
/// the source's base line and column applied, every line terminator of clause 12.3 starting a line and CR LF one.
/// </para>
/// <para>
/// <b>The raw spelling is the text between <see cref="Start"/> and <see cref="End"/></b>, for every kind: the token keeps
/// the source's spelling, and its cooked value beside it, and takes neither engine's convention (decision D21).
/// </para>
/// <para>
/// <b>Public from 2026-11-04</b>, by the owner's decision of that day ("Expose + preview.10"), so that Broiler.VM's
/// tokenizers-agreement row (SJL-13.3's step 1, part F; decision D21) reads the stream a parse produced:
/// <see cref="ParseResult.Tokens"/> holds it when the request sets <see cref="ParseRequest.RetainTokens"/>. A token is
/// immutable and its constructor internal: the library makes every token a result holds, and a default
/// <see cref="Token"/> (kind <see cref="TokenKind.EndOfInput"/>, offsets 0, no value, a <see cref="Number"/> of 0) is no
/// token of any stream. An identifier name's <see cref="Word"/> is the reserved or contextual word its cooked name
/// spells, which the kind of section 9.3 of the design includes, so that a consumer's kind map reads the kind, the word
/// and the escaped flag. A regular-expression literal's body and flags are its raw spelling's two parts: the flags are the
/// identifier parts after its last <c>/</c>, and the body the text between the first <c>/</c> and that one.
/// <i>(Revised 2026-11-04; the type was internal, and its stream "is kept beside the tree inside the library and is not
/// public".)</i>
/// </para>
/// </remarks>
// Hairs-AI:           Origin=AI; Spec=JSD-0080 output raw facts, JSD-0083 D15 D21; IP=Low; Security=Low; Resources=0; Fingerprint=C18C8A
// Hairs-Human:        PENDING
public readonly struct Token
{
    /// <summary>Creates a token.</summary>
    internal Token(TokenKind kind, int start, int end, int line, int column, TokenFlags flags, string? value = null, string? raw = null, double number = double.NaN, int split = -1, Word word = Word.None)
    {
        Kind = kind;
        Start = start;
        End = end;
        Line = line;
        Column = column;
        Flags = flags;
        Value = value;
        Raw = raw;
        Number = number;
        Split = split;
        Word = word;
    }

    /// <summary>What the token is.</summary>
    public TokenKind Kind { get; }

    /// <summary>
    /// The 0-based offset of its first code unit in the request's text, without <see cref="SourceText.BaseOffset"/>, unlike
    /// a node's <see cref="SourceSpan"/>, which adds it.
    /// </summary>
    public int Start { get; }

    /// <summary>
    /// The offset just past its last code unit in the request's text, without <see cref="SourceText.BaseOffset"/>, unlike a
    /// node's <see cref="SourceSpan"/>.
    /// </summary>
    public int End { get; }

    /// <summary>The 1-based line it starts on, with <see cref="SourceText.BaseLine"/> applied.</summary>
    public int Line { get; }

    /// <summary>
    /// The 1-based column, in UTF-16 code units, it starts at, with <see cref="SourceText.BaseColumn"/> applied on the
    /// text's first line.
    /// </summary>
    public int Column { get; }

    /// <summary>Its facts beside its kind.</summary>
    public TokenFlags Flags { get; }

    /// <summary>
    /// Its cooked value: an identifier's or a private name's StringValue (the name its escapes spell), a string literal's
    /// SV, a template piece's TV (null where it holds a <c>NotEscapeSequence</c>); null for every other kind.
    /// </summary>
    public string? Value { get; }

    /// <summary>A template piece's TRV, its raw text with every CR and CR LF read as LF (clause 12.9.6.2); null for every other kind.</summary>
    public string? Raw { get; }

    /// <summary>
    /// A numeric literal's Number value, from <c>NumericLiterals.Evaluate</c> (decision D9); NaN for a BigInt, whose value
    /// an engine computes under its own ceiling, for a literal with the <c>m</c> suffix, and for every other kind.
    /// </summary>
    public double Number { get; }

    /// <summary>
    /// A regular-expression literal's closing slash, an offset into the text: its body is the text after
    /// <see cref="Start"/> up to it, and its flags the text after it up to <see cref="End"/>; for a template piece with a
    /// <c>NotEscapeSequence</c>, the offset of the first one, for its diagnostic; otherwise -1.
    /// </summary>
    internal int Split { get; }

    /// <summary>
    /// The reserved or contextual word an identifier name's cooked name spells, escaped or not (an escaped <c>if</c> spells
    /// <see cref="Syntax.Word.If"/> with <see cref="IsEscaped"/> true); <see cref="Syntax.Word.None"/> for any other name
    /// and every other kind. <i>(Revised 2026-11-04, when the member became public, for the kind map of Broiler.VM's
    /// tokenizers-agreement row; it was internal.)</i>
    /// </summary>
    public Word Word { get; }

    /// <summary>Whether a line terminator stands between the previous token and this one.</summary>
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s12.10; IP=Low; Security=Low; Resources=1; Fingerprint=E3DFD4
    // Hairs-Human:        PENDING
    public bool NewLineBefore => (Flags & TokenFlags.NewLineBefore) != 0;

    /// <summary>Whether this is an identifier name or private name written with an escape.</summary>
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s12.7.1; IP=Low; Security=Low; Resources=1; Fingerprint=FF945F
    // Hairs-Human:        PENDING
    public bool IsEscaped => (Flags & TokenFlags.Escaped) != 0;

    /// <summary>Its length in code units.</summary>
    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D15; IP=Low; Security=Low; Resources=1; Fingerprint=F4209D
    // Hairs-Human:        PENDING
    public int Length => End - Start;

    /// <summary>The token with the newline fact set as the trivia before it says.</summary>
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s12.10 automatic semicolon insertion; IP=Low; Security=Medium; Resources=1; Fingerprint=C70164
    // Hairs-Human:        PENDING
    internal Token WithNewLineBefore(bool newLine) =>
        new(Kind, Start, End, Line, Column, newLine ? Flags | TokenFlags.NewLineBefore : Flags & ~TokenFlags.NewLineBefore, Value, Raw, Number, Split, Word);
}

/// <summary>
/// The reserved words and the contextual words of ECMA-262 (clause 12.7.2, with the words that clauses 13 to 16 and the
/// proposals the request may admit read in place of an identifier), as an identifier name's cooked name spells them.
/// </summary>
/// <remarks>
/// <para>
/// <b>The word is a fact, and never a decision</b>: an escaped <c>if</c> spells <see cref="If"/> with the token's escaped
/// flag set, and whether a word is a keyword, a reserved word or a name where it stands is the parser's question.
/// </para>
/// <para>
/// Public from 2026-11-04, with <see cref="Token.Word"/>, by the owner's decision of that day ("Expose + preview.10"), so
/// that Broiler.VM's tokenizers-agreement row (decision D21) maps a token's kind as the library read it. The numbers are
/// stable. <i>(Revised 2026-11-04; the enumeration was internal.)</i>
/// </para>
/// </remarks>
// Hairs-AI:           Origin=AI; Spec=ECMA-262 s12.7.2 reserved words; IP=Low; Security=Low; Resources=0; Fingerprint=B56471
// Hairs-Human:        PENDING
public enum Word
{
    /// <summary>No word of this list.</summary>
    None = 0,

    /// <summary><c>await</c>.</summary>
    Await,

    /// <summary><c>break</c>.</summary>
    Break,

    /// <summary><c>case</c>.</summary>
    Case,

    /// <summary><c>catch</c>.</summary>
    Catch,

    /// <summary><c>class</c>.</summary>
    Class,

    /// <summary><c>const</c>.</summary>
    Const,

    /// <summary><c>continue</c>.</summary>
    Continue,

    /// <summary><c>debugger</c>.</summary>
    Debugger,

    /// <summary><c>default</c>.</summary>
    Default,

    /// <summary><c>delete</c>.</summary>
    Delete,

    /// <summary><c>do</c>.</summary>
    Do,

    /// <summary><c>else</c>.</summary>
    Else,

    /// <summary><c>enum</c>.</summary>
    Enum,

    /// <summary><c>export</c>.</summary>
    Export,

    /// <summary><c>extends</c>.</summary>
    Extends,

    /// <summary><c>false</c>.</summary>
    False,

    /// <summary><c>finally</c>.</summary>
    Finally,

    /// <summary><c>for</c>.</summary>
    For,

    /// <summary><c>function</c>.</summary>
    Function,

    /// <summary><c>if</c>.</summary>
    If,

    /// <summary><c>import</c>.</summary>
    Import,

    /// <summary><c>in</c>.</summary>
    In,

    /// <summary><c>instanceof</c>.</summary>
    Instanceof,

    /// <summary><c>new</c>.</summary>
    New,

    /// <summary><c>null</c>.</summary>
    Null,

    /// <summary><c>return</c>.</summary>
    Return,

    /// <summary><c>super</c>.</summary>
    Super,

    /// <summary><c>switch</c>.</summary>
    Switch,

    /// <summary><c>this</c>.</summary>
    This,

    /// <summary><c>throw</c>.</summary>
    Throw,

    /// <summary><c>true</c>.</summary>
    True,

    /// <summary><c>try</c>.</summary>
    Try,

    /// <summary><c>typeof</c>.</summary>
    Typeof,

    /// <summary><c>var</c>.</summary>
    Var,

    /// <summary><c>void</c>.</summary>
    Void,

    /// <summary><c>while</c>.</summary>
    While,

    /// <summary><c>with</c>.</summary>
    With,

    /// <summary><c>yield</c>.</summary>
    Yield,

    /// <summary><c>implements</c>, reserved in strict mode code.</summary>
    Implements,

    /// <summary><c>interface</c>, reserved in strict mode code.</summary>
    Interface,

    /// <summary><c>let</c>, reserved in strict mode code.</summary>
    Let,

    /// <summary><c>package</c>, reserved in strict mode code.</summary>
    Package,

    /// <summary><c>private</c>, reserved in strict mode code.</summary>
    Private,

    /// <summary><c>protected</c>, reserved in strict mode code.</summary>
    Protected,

    /// <summary><c>public</c>, reserved in strict mode code.</summary>
    Public,

    /// <summary><c>static</c>, reserved in strict mode code.</summary>
    Static,

    /// <summary><c>as</c>, contextual.</summary>
    As,

    /// <summary><c>async</c>, contextual.</summary>
    Async,

    /// <summary><c>from</c>, contextual.</summary>
    From,

    /// <summary><c>get</c>, contextual.</summary>
    Get,

    /// <summary><c>meta</c>, contextual (<c>import.meta</c>).</summary>
    Meta,

    /// <summary><c>of</c>, contextual.</summary>
    Of,

    /// <summary><c>set</c>, contextual.</summary>
    Set,

    /// <summary><c>target</c>, contextual (<c>new.target</c>).</summary>
    Target,

    /// <summary><c>using</c>, contextual (explicit resource management).</summary>
    Using,

    /// <summary><c>accessor</c>, contextual (the decorators proposal's auto-accessors).</summary>
    Accessor,

    /// <summary><c>source</c>, contextual (the source-phase import proposal).</summary>
    Source,

    /// <summary><c>defer</c>, contextual (the deferred-import proposal).</summary>
    Defer,

    /// <summary><c>constructor</c>, which class bodies read.</summary>
    Constructor,

    /// <summary><c>arguments</c>, which strict mode and class field initialisers read.</summary>
    Arguments,

    /// <summary><c>eval</c>, which strict mode reads.</summary>
    Eval,
}

/// <summary>The words of <see cref="Syntax.Word"/>, looked up by spelling.</summary>
// Hairs-AI:           Origin=AI; Spec=ECMA-262 s12.7.2 reserved words; IP=Low; Security=Low; Resources=0; Fingerprint=2B2B48
// Hairs-Human:        PENDING
internal static class Words
{
    /// <summary>The word <paramref name="name"/> spells, or <see cref="Word.None"/>.</summary>
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s12.7.2 reserved words; IP=Low; Security=Medium; Resources=1; Fingerprint=66912A
    // Hairs-Human:        PENDING
    internal static Word Of(string name) => name switch
    {
        "await" => Word.Await,
        "break" => Word.Break,
        "case" => Word.Case,
        "catch" => Word.Catch,
        "class" => Word.Class,
        "const" => Word.Const,
        "continue" => Word.Continue,
        "debugger" => Word.Debugger,
        "default" => Word.Default,
        "delete" => Word.Delete,
        "do" => Word.Do,
        "else" => Word.Else,
        "enum" => Word.Enum,
        "export" => Word.Export,
        "extends" => Word.Extends,
        "false" => Word.False,
        "finally" => Word.Finally,
        "for" => Word.For,
        "function" => Word.Function,
        "if" => Word.If,
        "import" => Word.Import,
        "in" => Word.In,
        "instanceof" => Word.Instanceof,
        "new" => Word.New,
        "null" => Word.Null,
        "return" => Word.Return,
        "super" => Word.Super,
        "switch" => Word.Switch,
        "this" => Word.This,
        "throw" => Word.Throw,
        "true" => Word.True,
        "try" => Word.Try,
        "typeof" => Word.Typeof,
        "var" => Word.Var,
        "void" => Word.Void,
        "while" => Word.While,
        "with" => Word.With,
        "yield" => Word.Yield,
        "implements" => Word.Implements,
        "interface" => Word.Interface,
        "let" => Word.Let,
        "package" => Word.Package,
        "private" => Word.Private,
        "protected" => Word.Protected,
        "public" => Word.Public,
        "static" => Word.Static,
        "as" => Word.As,
        "async" => Word.Async,
        "from" => Word.From,
        "get" => Word.Get,
        "meta" => Word.Meta,
        "of" => Word.Of,
        "set" => Word.Set,
        "target" => Word.Target,
        "using" => Word.Using,
        "accessor" => Word.Accessor,
        "source" => Word.Source,
        "defer" => Word.Defer,
        "constructor" => Word.Constructor,
        "arguments" => Word.Arguments,
        "eval" => Word.Eval,
        _ => Word.None,
    };
}
