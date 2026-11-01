// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   3
// Annotated:        3/3
// Exempt:           85
// Human-reviewed:   0/3
// IP risk:          Low
// Security risk:    Low
// Criteria:         0/0
// Resource impact:  0/10 max
// Unverified:       3
//
// GENERATED - DO NOT EDIT MANUALLY

using System;

namespace Broiler.ECMAScript.Syntax;

/// <summary>
/// The kinds of input element the tokenizer gives the parser (ECMA-262 clause 12, "ECMAScript Language: Lexical
/// Grammar"): the end of the input, names, literals, the four template pieces, and every punctuator of clause 12.8, with
/// <c>@</c>, which only the decorators proposal reads.
/// </summary>
/// <remarks>
/// <para>
/// <b>A reserved word is an <see cref="IdentifierName"/></b>, as the lexical grammar has it: which word it spells is the
/// token's <see cref="Token.Word"/>, and whether that word is a keyword where it stands is the parser's question, since
/// only the parser knows the position, the strictness and the goal's parameters.
/// </para>
/// <para>Internal to the library: the parser reads these, and the token stream reaches an engine through the result.</para>
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s12 input elements; IP=Low; Security=Low; Resources=0; Fingerprint=9F1F7D
// Broiler-Human:        PENDING
internal enum TokenKind
{
    /// <summary>The end of the input: no input element is left.</summary>
    EndOfInput = 0,

    /// <summary>A refusal: the tokenizer met text no input element of the goal admits, and says why (<see cref="Tokenizer.Diagnostic"/>).</summary>
    Invalid,

    /// <summary><c>IdentifierName</c> (clause 12.7), reserved words included; its cooked name is <see cref="Token.Value"/>.</summary>
    IdentifierName,

    /// <summary><c>PrivateIdentifier</c> (clause 12.7): <c>#</c> and an <c>IdentifierName</c>; <see cref="Token.Value"/> is its StringValue, with the <c>#</c>.</summary>
    PrivateIdentifier,

    /// <summary><c>NumericLiteral</c> (clause 12.9.3), with the request's <c>m</c> suffix where its lexer extension admits one.</summary>
    NumericLiteral,

    /// <summary><c>StringLiteral</c> (clause 12.9.4); its SV is <see cref="Token.Value"/>.</summary>
    StringLiteral,

    /// <summary><c>NoSubstitutionTemplate</c> (clause 12.9.6): a template with no substitution, backtick to backtick.</summary>
    NoSubstitutionTemplate,

    /// <summary><c>TemplateHead</c> (clause 12.9.6): from a template's backtick to its first <c>${</c>.</summary>
    TemplateHead,

    /// <summary><c>TemplateMiddle</c> (clause 12.9.6): from a substitution's closing <c>}</c> to the next <c>${</c>.</summary>
    TemplateMiddle,

    /// <summary><c>TemplateTail</c> (clause 12.9.6): from a substitution's closing <c>}</c> to the template's closing backtick.</summary>
    TemplateTail,

    /// <summary><c>RegularExpressionLiteral</c> (clause 12.9.5): its body and flags, delimited and not interpreted.</summary>
    RegularExpressionLiteral,

    /// <summary><c>{</c>.</summary>
    LeftBrace,

    /// <summary><c>}</c>, where the goal reads a <c>RightBracePunctuator</c> rather than a template's continuation.</summary>
    RightBrace,

    /// <summary><c>(</c>.</summary>
    LeftParen,

    /// <summary><c>)</c>.</summary>
    RightParen,

    /// <summary><c>[</c>.</summary>
    LeftBracket,

    /// <summary><c>]</c>.</summary>
    RightBracket,

    /// <summary><c>.</c>.</summary>
    Dot,

    /// <summary><c>...</c>.</summary>
    Ellipsis,

    /// <summary><c>;</c>.</summary>
    Semicolon,

    /// <summary><c>,</c>.</summary>
    Comma,

    /// <summary><c>&lt;</c>.</summary>
    LessThan,

    /// <summary><c>&gt;</c>.</summary>
    GreaterThan,

    /// <summary><c>&lt;=</c>.</summary>
    LessThanEquals,

    /// <summary><c>&gt;=</c>.</summary>
    GreaterThanEquals,

    /// <summary><c>==</c>.</summary>
    EqualsEquals,

    /// <summary><c>!=</c>.</summary>
    NotEquals,

    /// <summary><c>===</c>.</summary>
    EqualsEqualsEquals,

    /// <summary><c>!==</c>.</summary>
    NotEqualsEquals,

    /// <summary><c>+</c>.</summary>
    Plus,

    /// <summary><c>-</c>.</summary>
    Minus,

    /// <summary><c>*</c>.</summary>
    Star,

    /// <summary><c>**</c>.</summary>
    StarStar,

    /// <summary><c>/</c>, under a goal that reads division.</summary>
    Slash,

    /// <summary><c>%</c>.</summary>
    Percent,

    /// <summary><c>++</c>.</summary>
    PlusPlus,

    /// <summary><c>--</c>.</summary>
    MinusMinus,

    /// <summary><c>&lt;&lt;</c>.</summary>
    LeftShift,

    /// <summary><c>&gt;&gt;</c>.</summary>
    RightShift,

    /// <summary><c>&gt;&gt;&gt;</c>.</summary>
    UnsignedRightShift,

    /// <summary><c>&amp;</c>.</summary>
    Ampersand,

    /// <summary><c>|</c>.</summary>
    Bar,

    /// <summary><c>^</c>.</summary>
    Caret,

    /// <summary><c>!</c>.</summary>
    Bang,

    /// <summary><c>~</c>.</summary>
    Tilde,

    /// <summary><c>&amp;&amp;</c>.</summary>
    AmpersandAmpersand,

    /// <summary><c>||</c>.</summary>
    BarBar,

    /// <summary><c>??</c>.</summary>
    QuestionQuestion,

    /// <summary><c>?</c>.</summary>
    Question,

    /// <summary><c>?.</c>, the <c>OptionalChainingPunctuator</c>, never before a decimal digit (clause 12.8).</summary>
    QuestionDot,

    /// <summary><c>:</c>.</summary>
    Colon,

    /// <summary><c>=</c>.</summary>
    Equals,

    /// <summary><c>+=</c>.</summary>
    PlusEquals,

    /// <summary><c>-=</c>.</summary>
    MinusEquals,

    /// <summary><c>*=</c>.</summary>
    StarEquals,

    /// <summary><c>/=</c>, under a goal that reads division.</summary>
    SlashEquals,

    /// <summary><c>%=</c>.</summary>
    PercentEquals,

    /// <summary><c>**=</c>.</summary>
    StarStarEquals,

    /// <summary><c>&lt;&lt;=</c>.</summary>
    LeftShiftEquals,

    /// <summary><c>&gt;&gt;=</c>.</summary>
    RightShiftEquals,

    /// <summary><c>&gt;&gt;&gt;=</c>.</summary>
    UnsignedRightShiftEquals,

    /// <summary><c>&amp;=</c>.</summary>
    AmpersandEquals,

    /// <summary><c>|=</c>.</summary>
    BarEquals,

    /// <summary><c>^=</c>.</summary>
    CaretEquals,

    /// <summary><c>&amp;&amp;=</c>.</summary>
    AmpersandAmpersandEquals,

    /// <summary><c>||=</c>.</summary>
    BarBarEquals,

    /// <summary><c>??=</c>.</summary>
    QuestionQuestionEquals,

    /// <summary><c>=&gt;</c>.</summary>
    Arrow,

    /// <summary>
    /// <c>@</c>, which no input element of ECMA-262 begins: the decorators proposal's (catalogue row EP0009). The tokenizer
    /// gives it so that the parser refuses a decorator where it reaches one, with the proposal's code, when the request's
    /// flag is off (Broiler.VM's JSD-0083, decision D17).
    /// </summary>
    At,
}

/// <summary>
/// The goal symbols of the lexical grammar (ECMA-262 clause 12): which input elements may begin where the parser stands.
/// The parser chooses one for each token it asks for; the tokenizer never chooses from the tokens it has read.
/// </summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s12 goal symbols; IP=Low; Security=Low; Resources=0; Fingerprint=FFF830
// Broiler-Human:        PENDING
internal enum LexicalGoal
{
    /// <summary><c>InputElementDiv</c>: a <c>/</c> is division, and a <c>}</c> a <c>RightBracePunctuator</c>.</summary>
    Div = 0,

    /// <summary><c>InputElementRegExp</c>: a <c>/</c> begins a <c>RegularExpressionLiteral</c>, and a <c>}</c> is a <c>RightBracePunctuator</c>.</summary>
    RegExp,

    /// <summary><c>InputElementRegExpOrTemplateTail</c>: a <c>/</c> begins a literal, and a <c>}</c> a template's continuation.</summary>
    RegExpOrTemplateTail,

    /// <summary><c>InputElementTemplateTail</c>: a <c>/</c> is division, and a <c>}</c> a template's continuation.</summary>
    TemplateTail,

    /// <summary>
    /// <c>InputElementHashbangOrRegExp</c>: the start of a <c>Script</c> or a <c>Module</c>, where a <c>HashbangComment</c>
    /// may stand at the text's first code unit and a <c>/</c> begins a literal.
    /// </summary>
    HashbangOrRegExp,
}

/// <summary>The facts a token carries beside its kind, span and value: what the parser needs for strict mode, templates and automatic semicolon insertion.</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s12.9 raw facts, JSD-0083 D15; IP=Low; Security=Low; Resources=0; Fingerprint=62EF37
// Broiler-Human:        PENDING
[Flags]
internal enum TokenFlags
{
    /// <summary>None.</summary>
    None = 0,

    /// <summary>
    /// A <c>LineTerminator</c>, or a <c>MultiLineComment</c> holding one, stands between the previous token and this one
    /// (clause 12.10, automatic semicolon insertion and the restricted productions), the start of the text not counting.
    /// </summary>
    NewLineBefore = 1,

    /// <summary>An <c>IdentifierName</c> or <c>PrivateIdentifier</c> written with at least one <c>\u</c> escape (clause 12.7.1, the reserved words' rule).</summary>
    Escaped = 2,

    /// <summary>A <c>LegacyOctalIntegerLiteral</c>, such as <c>017</c> (clause 12.9.3.1: refused in strict mode code).</summary>
    LegacyOctal = 4,

    /// <summary>A <c>NonOctalDecimalIntegerLiteral</c>, such as <c>08</c>, with or without a fraction or exponent (clause 12.9.3.1).</summary>
    NonOctalDecimal = 8,

    /// <summary>A string literal holding a <c>LegacyOctalEscapeSequence</c>, such as <c>\01</c> or <c>\7</c> (clause 12.9.4.1).</summary>
    LegacyOctalEscape = 16,

    /// <summary>A string literal holding a <c>NonOctalDecimalEscapeSequence</c>, <c>\8</c> or <c>\9</c> (clause 12.9.4.1).</summary>
    NonOctalDecimalEscape = 32,

    /// <summary>
    /// A template piece holding a <c>NotEscapeSequence</c> (clause 12.9.6): its cooked value is undefined, which a tagged
    /// template admits and an untagged one refuses (clause 13.2.8.1, catalogue rows EE0020 and EE0022 to EE0024).
    /// </summary>
    NotEscapeSequence = 64,

    /// <summary>A <c>NumericLiteral</c> with the <c>BigIntLiteralSuffix</c> <c>n</c>.</summary>
    BigInt = 128,

    /// <summary>A numeric literal with the <c>m</c> suffix, admitted by the request's lexer extension alone (Broiler.VM's JSD-0083, decision D17).</summary>
    DecimalSuffix = 256,

    /// <summary>A string literal holding an escape sequence or a line continuation, so that it is no directive spelling <c>use strict</c> exactly (clause 11.2.1).</summary>
    StringHasEscape = 512,
}
