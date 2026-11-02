// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   128
// Annotated:        128/128
// Exempt:           143
// Human-reviewed:   0/128
// IP risk:          Low
// Security risk:    Low
// Criteria:         0/0
// Resource impact:  1/10 max
// Unverified:       128
//
// GENERATED - DO NOT EDIT MANUALLY

using System.Collections.Generic;

namespace Broiler.ECMAScript.Syntax;

/// <summary>The base of every expression (ECMA-262 clause 13).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=6C0FDA
// Broiler-Human:        PENDING
public abstract class Expression : SyntaxNode
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=186638
    // Broiler-Human:        PENDING
    private protected Expression(SyntaxKind kind, SourceSpan span)
        : base(kind, span)
    {
    }

}

/// <summary>An IdentifierReference (ECMA-262 clause 13.1): a name read or written.</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=3267F3
// Broiler-Human:        PENDING
public sealed class Identifier : Expression
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=3D4421
    // Broiler-Human:        PENDING
    internal Identifier(SourceSpan span, string name, bool isEscaped)
        : base(SyntaxKind.Identifier, span)
    {
        Name = name;
        IsEscaped = isEscaped;
    }

    /// <summary>Its StringValue (clause 13.1.3): the name its escapes spell.</summary>
    public string Name { get; }

    /// <summary>Whether it is written with a <c>\u</c> escape (clause 12.7), a raw fact.</summary>
    public bool IsEscaped { get; }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=CA8EA6
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into)
    {
    }
}

/// <summary>An IdentifierName where no binding or reference is meant (ECMA-262 clause 12.7): a property name, a label, a meta-property's part, a module export name.</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=C60116
// Broiler-Human:        PENDING
public sealed class IdentifierName : SyntaxNode
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=04864C
    // Broiler-Human:        PENDING
    internal IdentifierName(SourceSpan span, string name, bool isEscaped)
        : base(SyntaxKind.IdentifierName, span)
    {
        Name = name;
        IsEscaped = isEscaped;
    }

    /// <summary>Its StringValue (clause 12.7.1.2).</summary>
    public string Name { get; }

    /// <summary>Whether it is written with a <c>\u</c> escape (clause 12.7), a raw fact.</summary>
    public bool IsEscaped { get; }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=CA8EA6
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into)
    {
    }
}

/// <summary>A PrivateIdentifier (ECMA-262 clause 12.7): <c>#</c> and an IdentifierName.</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=7CA461
// Broiler-Human:        PENDING
public sealed class PrivateIdentifier : SyntaxNode
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=D5FF45
    // Broiler-Human:        PENDING
    internal PrivateIdentifier(SourceSpan span, string name, bool isEscaped)
        : base(SyntaxKind.PrivateIdentifier, span)
    {
        Name = name;
        IsEscaped = isEscaped;
    }

    /// <summary>Its StringValue (clause 12.7.1.2): <c>#</c> followed by the name its escapes spell.</summary>
    public string Name { get; }

    /// <summary>Whether its name is written with a <c>\u</c> escape (clause 12.7), a raw fact.</summary>
    public bool IsEscaped { get; }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=CA8EA6
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into)
    {
    }
}

/// <summary><c>this</c> (ECMA-262 clause 13.2.1).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=07B445
// Broiler-Human:        PENDING
public sealed class ThisExpression : Expression
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=CE2226
    // Broiler-Human:        PENDING
    internal ThisExpression(SourceSpan span)
        : base(SyntaxKind.ThisExpression, span)
    {
    }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=CA8EA6
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into)
    {
    }
}

/// <summary><c>super</c>, as a SuperProperty's object or a SuperCall's callee (ECMA-262 clause 13.3.7); it stands nowhere else.</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=C2E54B
// Broiler-Human:        PENDING
public sealed class SuperExpression : Expression
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=E6130D
    // Broiler-Human:        PENDING
    internal SuperExpression(SourceSpan span)
        : base(SyntaxKind.SuperExpression, span)
    {
    }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=CA8EA6
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into)
    {
    }
}

/// <summary><c>null</c> (ECMA-262 clause 13.2.3).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=14FA08
// Broiler-Human:        PENDING
public sealed class NullLiteral : Expression
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=867D92
    // Broiler-Human:        PENDING
    internal NullLiteral(SourceSpan span)
        : base(SyntaxKind.NullLiteral, span)
    {
    }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=CA8EA6
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into)
    {
    }
}

/// <summary><c>true</c> or <c>false</c> (ECMA-262 clause 13.2.3).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=1469CE
// Broiler-Human:        PENDING
public sealed class BooleanLiteral : Expression
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=651661
    // Broiler-Human:        PENDING
    internal BooleanLiteral(SourceSpan span, bool value)
        : base(SyntaxKind.BooleanLiteral, span)
    {
        Value = value;
    }

    /// <summary>The literal's value.</summary>
    public bool Value { get; }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=CA8EA6
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into)
    {
    }
}

/// <summary>What a numeric literal's spelling records (ECMA-262 clause 12.9.3 and Annex B.1.1), raw facts the parse keeps.</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=F928D2
// Broiler-Human:        PENDING
[System.Flags]
public enum NumericLiteralFlags
{
    /// <summary>None of the facts below.</summary>
    None = 0,

    /// <summary>A LegacyOctalIntegerLiteral (Annex B.1.1), such as <c>017</c>.</summary>
    LegacyOctal = 1,

    /// <summary>A NonOctalDecimalIntegerLiteral (Annex B.1.1), such as <c>08</c>.</summary>
    NonOctalDecimal = 2,

    /// <summary>A BigInt literal: the <c>n</c> suffix (clause 12.9.3).</summary>
    BigInt = 4,

    /// <summary>The <c>m</c> suffix, admitted only through the request's lexer extension (decision D17).</summary>
    DecimalSuffix = 8,
}

/// <summary>A NumericLiteral (ECMA-262 clause 12.9.3), with its raw facts.</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=E28D7E
// Broiler-Human:        PENDING
public sealed class NumericLiteral : Expression
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=EBB212
    // Broiler-Human:        PENDING
    internal NumericLiteral(SourceSpan span, double value, string text, NumericLiteralFlags flags)
        : base(SyntaxKind.NumericLiteral, span)
    {
        Value = value;
        Text = text;
        Flags = flags;
    }

    /// <summary>
    /// Its Number value (clause 12.9.3.3), from <c>NumericLiterals.Evaluate</c> (decision D9); NaN for a BigInt literal and
    /// for one with the <c>m</c> suffix, whose values an engine computes from <see cref="Text"/> under its own ceilings.
    /// </summary>
    public double Value { get; }

    /// <summary>The literal as written: the BigInt literal's text, among others, which the tree keeps raw.</summary>
    public string Text { get; }

    /// <summary>The facts its spelling records.</summary>
    public NumericLiteralFlags Flags { get; }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=CA8EA6
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into)
    {
    }
}

/// <summary>What a string literal's escapes record (ECMA-262 clause 12.9.4 and Annex B.1.2), raw facts the parse keeps.</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=0E4A07
// Broiler-Human:        PENDING
[System.Flags]
public enum StringLiteralFlags
{
    /// <summary>None of the facts below.</summary>
    None = 0,

    /// <summary>A LegacyOctalEscapeSequence (Annex B.1.2), such as <c>\1</c>.</summary>
    LegacyOctalEscape = 1,

    /// <summary>A NonOctalDecimalEscapeSequence (Annex B.1.2): <c>\8</c> or <c>\9</c>.</summary>
    NonOctalDecimalEscape = 2,

    /// <summary>An escape sequence or a line continuation, which keeps it from being a <c>use strict</c> directive (clause 11.2.1).</summary>
    HasEscape = 4,
}

/// <summary>A StringLiteral (ECMA-262 clause 12.9.4), with its raw facts.</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=EF0ADF
// Broiler-Human:        PENDING
public sealed class StringLiteral : Expression
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=679788
    // Broiler-Human:        PENDING
    internal StringLiteral(SourceSpan span, string value, StringLiteralFlags flags)
        : base(SyntaxKind.StringLiteral, span)
    {
        Value = value;
        Flags = flags;
    }

    /// <summary>Its SV (clause 12.9.4.1), which may hold lone surrogates.</summary>
    public string Value { get; }

    /// <summary>The facts its escapes record.</summary>
    public StringLiteralFlags Flags { get; }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=CA8EA6
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into)
    {
    }
}

/// <summary>A RegularExpressionLiteral (ECMA-262 clause 12.9.5), delimited only: its validity is the request's provider's (decision D7).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=968BC5
// Broiler-Human:        PENDING
public sealed class RegularExpressionLiteral : Expression
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=D0689B
    // Broiler-Human:        PENDING
    internal RegularExpressionLiteral(SourceSpan span, string pattern, string flags)
        : base(SyntaxKind.RegularExpressionLiteral, span)
    {
        Pattern = pattern;
        Flags = flags;
    }

    /// <summary>Its BodyText (clause 12.9.5.2), as written.</summary>
    public string Pattern { get; }

    /// <summary>Its FlagText (clause 12.9.5.3), as written.</summary>
    public string Flags { get; }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=CA8EA6
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into)
    {
    }
}

/// <summary>One piece of a template's text (ECMA-262 clause 12.9.6): the text between the backquotes, a substitution's braces or both.</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=6A77A3
// Broiler-Human:        PENDING
public sealed class TemplateElement : SyntaxNode
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=57ED5B
    // Broiler-Human:        PENDING
    internal TemplateElement(SourceSpan span, string? cooked, string raw)
        : base(SyntaxKind.TemplateElement, span)
    {
        Cooked = cooked;
        Raw = raw;
    }

    /// <summary>Its TV (clause 12.9.6.1); null where it holds a <c>NotEscapeSequence</c>, which only a tagged template admits.</summary>
    public string? Cooked { get; }

    /// <summary>Its TRV (clause 12.9.6.2), CR and CR LF read as LF.</summary>
    public string Raw { get; }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=CA8EA6
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into)
    {
    }
}

/// <summary>A TemplateLiteral (ECMA-262 clause 13.2.8), read in place from the tokenizer's pieces.</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=713F7C
// Broiler-Human:        PENDING
public sealed class TemplateLiteral : Expression
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=24E9C8
    // Broiler-Human:        PENDING
    internal TemplateLiteral(SourceSpan span, IReadOnlyList<TemplateElement> quasis, IReadOnlyList<Expression> expressions)
        : base(SyntaxKind.TemplateLiteral, span)
    {
        Quasis = quasis;
        Expressions = expressions;
    }

    /// <summary>The pieces of text, one more than <see cref="Expressions"/>, each piece's span its token's.</summary>
    public IReadOnlyList<TemplateElement> Quasis { get; }

    /// <summary>The substitutions, in source order.</summary>
    public IReadOnlyList<Expression> Expressions { get; }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=D3662F
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into)
    {
        for (var i = 0; i < Quasis.Count; i++)
        {
            into.Add(Quasis[i]);
            if (i < Expressions.Count)
                into.Add(Expressions[i]);
        }
    }
}

/// <summary>A tagged template (ECMA-262 clause 13.3.11): a MemberExpression or CallExpression followed by a TemplateLiteral.</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=72AAD7
// Broiler-Human:        PENDING
public sealed class TaggedTemplateExpression : Expression
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=658CDC
    // Broiler-Human:        PENDING
    internal TaggedTemplateExpression(SourceSpan span, Expression tag, TemplateLiteral quasi)
        : base(SyntaxKind.TaggedTemplateExpression, span)
    {
        Tag = tag;
        Quasi = quasi;
    }

    /// <summary>The tag.</summary>
    public Expression Tag { get; }

    /// <summary>The template, parsed with the <c>[Tagged]</c> parameter.</summary>
    public TemplateLiteral Quasi { get; }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=F48E1F
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into)
    {
        into.Add(Tag);
        into.Add(Quasi);
    }
}

/// <summary>An ArrayLiteral (ECMA-262 clause 13.2.4).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=3231B7
// Broiler-Human:        PENDING
public sealed class ArrayLiteral : Expression
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=EAFB5F
    // Broiler-Human:        PENDING
    internal ArrayLiteral(SourceSpan span, IReadOnlyList<Expression?> elements, bool trailingComma)
        : base(SyntaxKind.ArrayLiteral, span)
    {
        Elements = elements;
        TrailingComma = trailingComma;
    }

    /// <summary>The elements, a <see cref="SpreadElement"/> where one is spread and null for an elision.</summary>
    public IReadOnlyList<Expression?> Elements { get; }

    /// <summary>Whether a comma closes the list after its last element, a raw fact: <c>[a,]</c> has one element and a trailing comma.</summary>
    public bool TrailingComma { get; }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=470A6A
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into) => AddAll(into, Elements);
}

/// <summary>What one entry of an object literal defines (ECMA-262 clause 13.2.5).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=92D76B
// Broiler-Human:        PENDING
public enum PropertyKind
{
    /// <summary><c>key: value</c>.</summary>
    Init = 0,

    /// <summary><c>{ x }</c>, a shorthand reference.</summary>
    Shorthand,

    /// <summary>A method: <c>m() {}</c>, a generator method or an async method.</summary>
    Method,

    /// <summary>A getter.</summary>
    Get,

    /// <summary>A setter.</summary>
    Set,
}

/// <summary>A PropertyDefinition of an object literal (ECMA-262 clause 13.2.5), other than a spread.</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=D433FC
// Broiler-Human:        PENDING
public sealed class PropertyDefinition : SyntaxNode
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=7CAB30
    // Broiler-Human:        PENDING
    internal PropertyDefinition(SourceSpan span, PropertyKind kind, SyntaxNode key, Expression value)
        : base(SyntaxKind.PropertyDefinition, span)
    {
        PropertyKind = kind;
        Key = key;
        Value = value;
    }

    /// <summary>What the entry defines.</summary>
    public PropertyKind PropertyKind { get; }

    /// <summary>
    /// The PropertyName: an <see cref="Syntax.IdentifierName"/>, a <see cref="StringLiteral"/>, a <see cref="NumericLiteral"/>
    /// or a <see cref="ComputedPropertyName"/>; for a method, a getter or a setter, whose name is a ClassElementName, also a
    /// <see cref="Syntax.PrivateIdentifier"/>, which a static-semantic rule refuses there (stage 4d).
    /// </summary>
    public SyntaxNode Key { get; }

    /// <summary>The value: the expression, the shorthand's <see cref="Identifier"/>, or the method's <see cref="FunctionExpression"/>.</summary>
    public Expression Value { get; }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=FCED38
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into)
    {
        if (PropertyKind != PropertyKind.Shorthand)
            into.Add(Key);
        into.Add(Value);
    }
}

/// <summary>A ComputedPropertyName (ECMA-262 clause 13.2.5): <c>[</c> an expression <c>]</c>, the span covering the brackets.</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=DF0B7E
// Broiler-Human:        PENDING
public sealed class ComputedPropertyName : SyntaxNode
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=69F8EF
    // Broiler-Human:        PENDING
    internal ComputedPropertyName(SourceSpan span, Expression expression)
        : base(SyntaxKind.ComputedPropertyName, span)
    {
        Expression = expression;
    }

    /// <summary>The key's expression.</summary>
    public Expression Expression { get; }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=9961D5
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into) => into.Add(Expression);
}

/// <summary>An ObjectLiteral (ECMA-262 clause 13.2.5).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=FE3BC4
// Broiler-Human:        PENDING
public sealed class ObjectLiteral : Expression
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=B95AE1
    // Broiler-Human:        PENDING
    internal ObjectLiteral(SourceSpan span, IReadOnlyList<SyntaxNode> properties, bool trailingComma)
        : base(SyntaxKind.ObjectLiteral, span)
    {
        Properties = properties;
        TrailingComma = trailingComma;
    }

    /// <summary>The entries in source order: each a <see cref="PropertyDefinition"/> or a <see cref="SpreadElement"/>.</summary>
    public IReadOnlyList<SyntaxNode> Properties { get; }

    /// <summary>Whether a comma closes the list after its last entry, a raw fact.</summary>
    public bool TrailingComma { get; }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=448E81
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into) => AddAll(into, Properties);
}

/// <summary><c>...</c> and an expression, in an array literal, an object literal or an argument list (ECMA-262 clauses 13.2.4, 13.2.5 and 13.3.8).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=D11D63
// Broiler-Human:        PENDING
public sealed class SpreadElement : Expression
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=648C2C
    // Broiler-Human:        PENDING
    internal SpreadElement(SourceSpan span, Expression argument)
        : base(SyntaxKind.SpreadElement, span)
    {
        Argument = argument;
    }

    /// <summary>What is spread.</summary>
    public Expression Argument { get; }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=DCA11B
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into) => into.Add(Argument);
}

/// <summary>
/// A ParenthesizedExpression (ECMA-262 clause 13.2.10), the span covering the parentheses: parenthesisation is a raw fact the
/// tree keeps as a node, and charges no tree depth (decision D14).
/// </summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=54610A
// Broiler-Human:        PENDING
public sealed class ParenthesizedExpression : Expression
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=3ADC99
    // Broiler-Human:        PENDING
    internal ParenthesizedExpression(SourceSpan span, Expression expression)
        : base(SyntaxKind.ParenthesizedExpression, span)
    {
        Expression = expression;
    }

    /// <summary>The expression inside.</summary>
    public Expression Expression { get; }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=9961D5
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into) => into.Add(Expression);
}

/// <summary>A function expression (ECMA-262 clauses 15.2, 15.5, 15.6 and 15.8), or an object literal's method's function.</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=49BF6D
// Broiler-Human:        PENDING
public sealed class FunctionExpression : Expression
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=8F86B2
    // Broiler-Human:        PENDING
    internal FunctionExpression(SourceSpan span, FunctionNode function)
        : base(SyntaxKind.FunctionExpression, span)
    {
        Function = function;
    }

    /// <summary>The function.</summary>
    public FunctionNode Function { get; }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=67BC92
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into) => into.Add(Function);
}

/// <summary>An ArrowFunction or AsyncArrowFunction (ECMA-262 clauses 15.3 and 15.9), its parameters converted from the cover the parse read.</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=1CE712
// Broiler-Human:        PENDING
public sealed class ArrowFunctionExpression : Expression
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=A1EE9C
    // Broiler-Human:        PENDING
    internal ArrowFunctionExpression(SourceSpan span, FunctionNode function)
        : base(SyntaxKind.ArrowFunctionExpression, span)
    {
        Function = function;
    }

    /// <summary>The function, with <see cref="FunctionNode.IsArrow"/> set.</summary>
    public FunctionNode Function { get; }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=67BC92
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into) => into.Add(Function);
}

/// <summary>A ClassExpression (ECMA-262 clause 15.7).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=E88297
// Broiler-Human:        PENDING
public sealed class ClassExpression : Expression
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=8E5743
    // Broiler-Human:        PENDING
    internal ClassExpression(SourceSpan span, ClassNode @class)
        : base(SyntaxKind.ClassExpression, span)
    {
        Class = @class;
    }

    /// <summary>The class.</summary>
    public ClassNode Class { get; }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=A5271E
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into) => into.Add(Class);
}

/// <summary>
/// A property access (ECMA-262 clause 13.3.2): <c>o.p</c>, <c>o[e]</c>, <c>o.#p</c>, a SuperProperty, or a link of an optional
/// chain.
/// </summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=31F751
// Broiler-Human:        PENDING
public sealed class MemberExpression : Expression
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=57370B
    // Broiler-Human:        PENDING
    internal MemberExpression(SourceSpan span, Expression @object, SyntaxNode property, bool isComputed, bool isOptional)
        : base(SyntaxKind.MemberExpression, span)
    {
        Object = @object;
        Property = property;
        IsComputed = isComputed;
        IsOptional = isOptional;
    }

    /// <summary>The object, a <see cref="SuperExpression"/> for a SuperProperty.</summary>
    public Expression Object { get; }

    /// <summary>An <see cref="Syntax.IdentifierName"/>, a <see cref="Syntax.PrivateIdentifier"/>, or for a computed access the expression.</summary>
    public SyntaxNode Property { get; }

    /// <summary>Whether it is written with brackets.</summary>
    public bool IsComputed { get; }

    /// <summary>Whether it is written with <c>?.</c> (clause 13.3.9).</summary>
    public bool IsOptional { get; }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=8EBF65
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into)
    {
        into.Add(Object);
        into.Add(Property);
    }
}

/// <summary>A call (ECMA-262 clause 13.3.6): a CallExpression, a SuperCall, a CoverCallExpressionAndAsyncArrowHead read as a call, or a link of an optional chain.</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=01B052
// Broiler-Human:        PENDING
public sealed class CallExpression : Expression
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=30DF8D
    // Broiler-Human:        PENDING
    internal CallExpression(SourceSpan span, Expression callee, IReadOnlyList<Expression> arguments, bool trailingComma, bool isOptional)
        : base(SyntaxKind.CallExpression, span)
    {
        Callee = callee;
        Arguments = arguments;
        TrailingComma = trailingComma;
        IsOptional = isOptional;
    }

    /// <summary>The callee, a <see cref="SuperExpression"/> for a SuperCall.</summary>
    public Expression Callee { get; }

    /// <summary>The arguments, a <see cref="SpreadElement"/> where one is spread.</summary>
    public IReadOnlyList<Expression> Arguments { get; }

    /// <summary>Whether a comma closes the argument list, a raw fact.</summary>
    public bool TrailingComma { get; }

    /// <summary>Whether it is written with <c>?.</c> (clause 13.3.9).</summary>
    public bool IsOptional { get; }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=76A735
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into)
    {
        into.Add(Callee);
        AddAll(into, Arguments);
    }
}

/// <summary>An OptionalExpression (ECMA-262 clause 13.3.9): the whole chain whose links begin at a <c>?.</c>, which short-circuits as one.</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=9B6682
// Broiler-Human:        PENDING
public sealed class ChainExpression : Expression
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=7147A0
    // Broiler-Human:        PENDING
    internal ChainExpression(SourceSpan span, Expression expression)
        : base(SyntaxKind.ChainExpression, span)
    {
        Expression = expression;
    }

    /// <summary>The chain's last link.</summary>
    public Expression Expression { get; }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=9961D5
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into) => into.Add(Expression);
}

/// <summary>A NewExpression or <c>new</c> MemberExpression Arguments (ECMA-262 clause 13.3.5).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=A95DA0
// Broiler-Human:        PENDING
public sealed class NewExpression : Expression
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=25B69E
    // Broiler-Human:        PENDING
    internal NewExpression(SourceSpan span, Expression callee, IReadOnlyList<Expression> arguments, bool hasArgumentList, bool trailingComma)
        : base(SyntaxKind.NewExpression, span)
    {
        Callee = callee;
        Arguments = arguments;
        HasArgumentList = hasArgumentList;
        TrailingComma = trailingComma;
    }

    /// <summary>The constructor.</summary>
    public Expression Callee { get; }

    /// <summary>The arguments; empty when none are written.</summary>
    public IReadOnlyList<Expression> Arguments { get; }

    /// <summary>Whether an argument list is written: <c>new C()</c> rather than <c>new C</c>.</summary>
    public bool HasArgumentList { get; }

    /// <summary>Whether a comma closes the argument list, a raw fact.</summary>
    public bool TrailingComma { get; }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=76A735
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into)
    {
        into.Add(Callee);
        AddAll(into, Arguments);
    }
}

/// <summary>A MetaProperty (ECMA-262 clause 13.3.12): <c>new.target</c> or <c>import.meta</c>.</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=60224F
// Broiler-Human:        PENDING
public sealed class MetaProperty : Expression
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=917608
    // Broiler-Human:        PENDING
    internal MetaProperty(SourceSpan span, IdentifierName meta, IdentifierName property)
        : base(SyntaxKind.MetaProperty, span)
    {
        Meta = meta;
        Property = property;
    }

    /// <summary><c>new</c> or <c>import</c>.</summary>
    public IdentifierName Meta { get; }

    /// <summary><c>target</c> or <c>meta</c>.</summary>
    public IdentifierName Property { get; }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=9EEA03
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into)
    {
        into.Add(Meta);
        into.Add(Property);
    }
}

/// <summary>The phase an import is written with (ECMA-262 clause 16.2.2, and the import-phase proposals behind their flags, decision D17).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=8D3182
// Broiler-Human:        PENDING
public enum ImportPhase
{
    /// <summary>No phase: the ordinary import.</summary>
    Evaluation = 0,

    /// <summary><c>source</c>, under the source-phase proposal's flags.</summary>
    Source,

    /// <summary><c>defer</c>, under the deferred-import proposal's flags.</summary>
    Defer,
}

/// <summary>An ImportCall (ECMA-262 clause 13.3.10): <c>import(specifier)</c> or <c>import(specifier, options)</c>, or a phase's call form.</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=719733
// Broiler-Human:        PENDING
public sealed class ImportCall : Expression
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=20B9B5
    // Broiler-Human:        PENDING
    internal ImportCall(SourceSpan span, ImportPhase phase, Expression source, Expression? options, bool trailingComma)
        : base(SyntaxKind.ImportCall, span)
    {
        Phase = phase;
        Source = source;
        Options = options;
        TrailingComma = trailingComma;
    }

    /// <summary>The phase: <c>import.source(…)</c> and <c>import.defer(…)</c> under their flags.</summary>
    public ImportPhase Phase { get; }

    /// <summary>The specifier expression.</summary>
    public Expression Source { get; }

    /// <summary>The options expression, or null.</summary>
    public Expression? Options { get; }

    /// <summary>Whether a comma closes the argument list, a raw fact.</summary>
    public bool TrailingComma { get; }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=172E44
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into)
    {
        into.Add(Source);
        Add(into, Options);
    }
}

/// <summary>An update operator (ECMA-262 clause 13.4).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=FD8D2E
// Broiler-Human:        PENDING
public enum UpdateOperator
{
    /// <summary><c>++</c>.</summary>
    Increment = 0,

    /// <summary><c>--</c>.</summary>
    Decrement,
}

/// <summary>An UpdateExpression (ECMA-262 clause 13.4), prefix or postfix.</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=D62F7E
// Broiler-Human:        PENDING
public sealed class UpdateExpression : Expression
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=092017
    // Broiler-Human:        PENDING
    internal UpdateExpression(SourceSpan span, UpdateOperator @operator, bool isPrefix, Expression operand)
        : base(SyntaxKind.UpdateExpression, span)
    {
        Operator = @operator;
        IsPrefix = isPrefix;
        Operand = operand;
    }

    /// <summary>The operator.</summary>
    public UpdateOperator Operator { get; }

    /// <summary>Whether the operator stands before its operand.</summary>
    public bool IsPrefix { get; }

    /// <summary>The operand.</summary>
    public Expression Operand { get; }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=3AAD70
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into) => into.Add(Operand);
}

/// <summary>A unary operator (ECMA-262 clause 13.5).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=C53B71
// Broiler-Human:        PENDING
public enum UnaryOperator
{
    /// <summary><c>delete</c>.</summary>
    Delete = 0,

    /// <summary><c>void</c>.</summary>
    Void,

    /// <summary><c>typeof</c>.</summary>
    Typeof,

    /// <summary><c>+</c>.</summary>
    Plus,

    /// <summary><c>-</c>.</summary>
    Minus,

    /// <summary><c>~</c>.</summary>
    BitwiseNot,

    /// <summary><c>!</c>.</summary>
    LogicalNot,
}

/// <summary>A UnaryExpression (ECMA-262 clause 13.5).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=E75D58
// Broiler-Human:        PENDING
public sealed class UnaryExpression : Expression
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=ECD607
    // Broiler-Human:        PENDING
    internal UnaryExpression(SourceSpan span, UnaryOperator @operator, Expression operand)
        : base(SyntaxKind.UnaryExpression, span)
    {
        Operator = @operator;
        Operand = operand;
    }

    /// <summary>The operator.</summary>
    public UnaryOperator Operator { get; }

    /// <summary>The operand.</summary>
    public Expression Operand { get; }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=3AAD70
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into) => into.Add(Operand);
}

/// <summary>An AwaitExpression (ECMA-262 clause 15.8).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=6F0EB3
// Broiler-Human:        PENDING
public sealed class AwaitExpression : Expression
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=BC0A87
    // Broiler-Human:        PENDING
    internal AwaitExpression(SourceSpan span, Expression argument)
        : base(SyntaxKind.AwaitExpression, span)
    {
        Argument = argument;
    }

    /// <summary>The awaited operand.</summary>
    public Expression Argument { get; }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=DCA11B
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into) => into.Add(Argument);
}

/// <summary>A binary operator (ECMA-262 clauses 13.6 to 13.12), the logical ones apart (<see cref="LogicalOperator"/>).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=1E07C2
// Broiler-Human:        PENDING
public enum BinaryOperator
{
    /// <summary><c>**</c> (clause 13.6).</summary>
    Exponent = 0,

    /// <summary><c>*</c>.</summary>
    Multiply,

    /// <summary><c>/</c>.</summary>
    Divide,

    /// <summary><c>%</c>.</summary>
    Remainder,

    /// <summary><c>+</c>.</summary>
    Add,

    /// <summary><c>-</c>.</summary>
    Subtract,

    /// <summary><c>&lt;&lt;</c>.</summary>
    LeftShift,

    /// <summary><c>&gt;&gt;</c>.</summary>
    SignedRightShift,

    /// <summary><c>&gt;&gt;&gt;</c>.</summary>
    UnsignedRightShift,

    /// <summary><c>&lt;</c>.</summary>
    LessThan,

    /// <summary><c>&gt;</c>.</summary>
    GreaterThan,

    /// <summary><c>&lt;=</c>.</summary>
    LessThanOrEqual,

    /// <summary><c>&gt;=</c>.</summary>
    GreaterThanOrEqual,

    /// <summary><c>instanceof</c>.</summary>
    Instanceof,

    /// <summary><c>in</c>.</summary>
    In,

    /// <summary><c>==</c>.</summary>
    Equal,

    /// <summary><c>!=</c>.</summary>
    NotEqual,

    /// <summary><c>===</c>.</summary>
    StrictEqual,

    /// <summary><c>!==</c>.</summary>
    StrictNotEqual,

    /// <summary><c>&amp;</c>.</summary>
    BitwiseAnd,

    /// <summary><c>^</c>.</summary>
    BitwiseXor,

    /// <summary><c>|</c>.</summary>
    BitwiseOr,
}

/// <summary>A binary expression (ECMA-262 clauses 13.6 to 13.12): one node of a chain, each charged one level of tree depth (decision D14).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=FD4A8B
// Broiler-Human:        PENDING
public sealed class BinaryExpression : Expression
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=BDC6B1
    // Broiler-Human:        PENDING
    internal BinaryExpression(SourceSpan span, BinaryOperator @operator, Expression left, Expression right)
        : base(SyntaxKind.BinaryExpression, span)
    {
        Operator = @operator;
        Left = left;
        Right = right;
    }

    /// <summary>The operator.</summary>
    public BinaryOperator Operator { get; }

    /// <summary>The left operand.</summary>
    public Expression Left { get; }

    /// <summary>The right operand.</summary>
    public Expression Right { get; }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=478B7F
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into)
    {
        into.Add(Left);
        into.Add(Right);
    }
}

/// <summary>A logical operator (ECMA-262 clause 13.13).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=86AA91
// Broiler-Human:        PENDING
public enum LogicalOperator
{
    /// <summary><c>&amp;&amp;</c>.</summary>
    And = 0,

    /// <summary><c>||</c>.</summary>
    Or,

    /// <summary><c>??</c>.</summary>
    Coalesce,
}

/// <summary>A ShortCircuitExpression's node (ECMA-262 clause 13.13): <c>&amp;&amp;</c>, <c>||</c> or <c>??</c>, kept apart from the other binary operators as Broiler.VM's tree keeps it.</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=861C02
// Broiler-Human:        PENDING
public sealed class LogicalExpression : Expression
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=175027
    // Broiler-Human:        PENDING
    internal LogicalExpression(SourceSpan span, LogicalOperator @operator, Expression left, Expression right)
        : base(SyntaxKind.LogicalExpression, span)
    {
        Operator = @operator;
        Left = left;
        Right = right;
    }

    /// <summary>The operator.</summary>
    public LogicalOperator Operator { get; }

    /// <summary>The left operand.</summary>
    public Expression Left { get; }

    /// <summary>The right operand.</summary>
    public Expression Right { get; }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=478B7F
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into)
    {
        into.Add(Left);
        into.Add(Right);
    }
}

/// <summary><c>#x in o</c> (ECMA-262 clause 13.10, RelationalExpression : PrivateIdentifier <c>in</c> ShiftExpression).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=CC46C3
// Broiler-Human:        PENDING
public sealed class PrivateInExpression : Expression
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=934239
    // Broiler-Human:        PENDING
    internal PrivateInExpression(SourceSpan span, PrivateIdentifier name, Expression right)
        : base(SyntaxKind.PrivateInExpression, span)
    {
        Name = name;
        Right = right;
    }

    /// <summary>The private name.</summary>
    public PrivateIdentifier Name { get; }

    /// <summary>The object.</summary>
    public Expression Right { get; }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=FC3889
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into)
    {
        into.Add(Name);
        into.Add(Right);
    }
}

/// <summary>A ConditionalExpression (ECMA-262 clause 13.14).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=DCA030
// Broiler-Human:        PENDING
public sealed class ConditionalExpression : Expression
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=FA841D
    // Broiler-Human:        PENDING
    internal ConditionalExpression(SourceSpan span, Expression test, Expression consequent, Expression alternate)
        : base(SyntaxKind.ConditionalExpression, span)
    {
        Test = test;
        Consequent = consequent;
        Alternate = alternate;
    }

    /// <summary>The condition.</summary>
    public Expression Test { get; }

    /// <summary>The value when it holds.</summary>
    public Expression Consequent { get; }

    /// <summary>The value when it does not.</summary>
    public Expression Alternate { get; }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=0A8278
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into)
    {
        into.Add(Test);
        into.Add(Consequent);
        into.Add(Alternate);
    }
}

/// <summary>An assignment operator (ECMA-262 clause 13.15).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=0BBC31
// Broiler-Human:        PENDING
public enum AssignmentOperator
{
    /// <summary><c>=</c>.</summary>
    Assign = 0,

    /// <summary><c>*=</c>.</summary>
    MultiplyAssign,

    /// <summary><c>/=</c>.</summary>
    DivideAssign,

    /// <summary><c>%=</c>.</summary>
    RemainderAssign,

    /// <summary><c>+=</c>.</summary>
    AddAssign,

    /// <summary><c>-=</c>.</summary>
    SubtractAssign,

    /// <summary><c>&lt;&lt;=</c>.</summary>
    LeftShiftAssign,

    /// <summary><c>&gt;&gt;=</c>.</summary>
    SignedRightShiftAssign,

    /// <summary><c>&gt;&gt;&gt;=</c>.</summary>
    UnsignedRightShiftAssign,

    /// <summary><c>&amp;=</c>.</summary>
    BitwiseAndAssign,

    /// <summary><c>^=</c>.</summary>
    BitwiseXorAssign,

    /// <summary><c>|=</c>.</summary>
    BitwiseOrAssign,

    /// <summary><c>**=</c>.</summary>
    ExponentAssign,

    /// <summary><c>&amp;&amp;=</c>.</summary>
    AndAssign,

    /// <summary><c>||=</c>.</summary>
    OrAssign,

    /// <summary><c>??=</c>.</summary>
    CoalesceAssign,
}

/// <summary>An AssignmentExpression with an operator (ECMA-262 clause 13.15).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=B60597
// Broiler-Human:        PENDING
public sealed class AssignmentExpression : Expression
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=03878B
    // Broiler-Human:        PENDING
    internal AssignmentExpression(SourceSpan span, AssignmentOperator @operator, SyntaxNode target, Expression value)
        : base(SyntaxKind.AssignmentExpression, span)
    {
        Operator = @operator;
        Target = target;
        Value = value;
    }

    /// <summary>The operator.</summary>
    public AssignmentOperator Operator { get; }

    /// <summary>
    /// The target: an <see cref="ArrayPattern"/> or <see cref="ObjectPattern"/> converted from the literal the parse read
    /// (clause 13.15.5's AssignmentPattern, by conversion and with no second parse), or the LeftHandSideExpression as
    /// written. Whether an expression target is valid is a static-semantic rule (stage 4d).
    /// </summary>
    public SyntaxNode Target { get; }

    /// <summary>The value.</summary>
    public Expression Value { get; }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=8BC742
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into)
    {
        into.Add(Target);
        into.Add(Value);
    }
}

/// <summary>The comma operator (ECMA-262 clause 13.16).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=5E916C
// Broiler-Human:        PENDING
public sealed class SequenceExpression : Expression
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=0499B4
    // Broiler-Human:        PENDING
    internal SequenceExpression(SourceSpan span, IReadOnlyList<Expression> expressions)
        : base(SyntaxKind.SequenceExpression, span)
    {
        Expressions = expressions;
    }

    /// <summary>The operands, two or more, in source order.</summary>
    public IReadOnlyList<Expression> Expressions { get; }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=5FFFDD
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into) => AddAll(into, Expressions);
}

/// <summary>A YieldExpression (ECMA-262 clause 15.5).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=B3A8D6
// Broiler-Human:        PENDING
public sealed class YieldExpression : Expression
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=12E4A5
    // Broiler-Human:        PENDING
    internal YieldExpression(SourceSpan span, Expression? argument, bool isDelegate)
        : base(SyntaxKind.YieldExpression, span)
    {
        Argument = argument;
        IsDelegate = isDelegate;
    }

    /// <summary>What is yielded, or null where nothing is written.</summary>
    public Expression? Argument { get; }

    /// <summary>Whether it is written <c>yield*</c>.</summary>
    public bool IsDelegate { get; }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=5D8557
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into) => Add(into, Argument);
}

/// <summary>A decorator, <c>@</c> and its expression, admitted only under the decorators proposal's flag (decision D17).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=DDDD4D
// Broiler-Human:        PENDING
public sealed class Decorator : SyntaxNode
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=A98C6C
    // Broiler-Human:        PENDING
    internal Decorator(SourceSpan span, Expression expression)
        : base(SyntaxKind.Decorator, span)
    {
        Expression = expression;
    }

    /// <summary>The decorator's expression: a member chain, a call of one, or a parenthesised expression.</summary>
    public Expression Expression { get; }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13 expressions tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=9961D5
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into) => into.Add(Expression);
}
