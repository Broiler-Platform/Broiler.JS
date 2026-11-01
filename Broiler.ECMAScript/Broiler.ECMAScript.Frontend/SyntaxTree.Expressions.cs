// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;

namespace Broiler.ECMAScript.Syntax;

/// <summary>The base of every expression (ECMA-262 clause 13).</summary>
public abstract class Expression : SyntaxNode
{
    private protected Expression(SyntaxKind kind, SourceSpan span)
        : base(kind, span)
    {
    }

    /// <summary>
    /// The tree depth the expression's left spine is charged (decision D14): one for each node a binary chain or a member or
    /// call chain adds, none for a parenthesis, a template or a substitution. A chain that begins with this expression is
    /// charged from it, which closes the left-operand gap.
    /// </summary>
    internal int Spine { get; set; }
}

/// <summary>An IdentifierReference (ECMA-262 clause 13.1): a name read or written.</summary>
public sealed class Identifier : Expression
{
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

    internal override void AddChildren(List<SyntaxNode> into)
    {
    }
}

/// <summary>An IdentifierName where no binding or reference is meant (ECMA-262 clause 12.7): a property name, a label, a meta-property's part, a module export name.</summary>
public sealed class IdentifierName : SyntaxNode
{
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

    internal override void AddChildren(List<SyntaxNode> into)
    {
    }
}

/// <summary>A PrivateIdentifier (ECMA-262 clause 12.7): <c>#</c> and an IdentifierName.</summary>
public sealed class PrivateIdentifier : SyntaxNode
{
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

    internal override void AddChildren(List<SyntaxNode> into)
    {
    }
}

/// <summary><c>this</c> (ECMA-262 clause 13.2.1).</summary>
public sealed class ThisExpression : Expression
{
    internal ThisExpression(SourceSpan span)
        : base(SyntaxKind.ThisExpression, span)
    {
    }

    internal override void AddChildren(List<SyntaxNode> into)
    {
    }
}

/// <summary><c>super</c>, as a SuperProperty's object or a SuperCall's callee (ECMA-262 clause 13.3.7); it stands nowhere else.</summary>
public sealed class SuperExpression : Expression
{
    internal SuperExpression(SourceSpan span)
        : base(SyntaxKind.SuperExpression, span)
    {
    }

    internal override void AddChildren(List<SyntaxNode> into)
    {
    }
}

/// <summary><c>null</c> (ECMA-262 clause 13.2.3).</summary>
public sealed class NullLiteral : Expression
{
    internal NullLiteral(SourceSpan span)
        : base(SyntaxKind.NullLiteral, span)
    {
    }

    internal override void AddChildren(List<SyntaxNode> into)
    {
    }
}

/// <summary><c>true</c> or <c>false</c> (ECMA-262 clause 13.2.3).</summary>
public sealed class BooleanLiteral : Expression
{
    internal BooleanLiteral(SourceSpan span, bool value)
        : base(SyntaxKind.BooleanLiteral, span)
    {
        Value = value;
    }

    /// <summary>The literal's value.</summary>
    public bool Value { get; }

    internal override void AddChildren(List<SyntaxNode> into)
    {
    }
}

/// <summary>What a numeric literal's spelling records (ECMA-262 clause 12.9.3 and Annex B.1.1), raw facts the parse keeps.</summary>
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
public sealed class NumericLiteral : Expression
{
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

    internal override void AddChildren(List<SyntaxNode> into)
    {
    }
}

/// <summary>What a string literal's escapes record (ECMA-262 clause 12.9.4 and Annex B.1.2), raw facts the parse keeps.</summary>
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
public sealed class StringLiteral : Expression
{
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

    internal override void AddChildren(List<SyntaxNode> into)
    {
    }
}

/// <summary>A RegularExpressionLiteral (ECMA-262 clause 12.9.5), delimited only: its validity is the request's provider's (decision D7).</summary>
public sealed class RegularExpressionLiteral : Expression
{
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

    internal override void AddChildren(List<SyntaxNode> into)
    {
    }
}

/// <summary>One piece of a template's text (ECMA-262 clause 12.9.6): the text between the backquotes, a substitution's braces or both.</summary>
public sealed class TemplateElement : SyntaxNode
{
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

    internal override void AddChildren(List<SyntaxNode> into)
    {
    }
}

/// <summary>A TemplateLiteral (ECMA-262 clause 13.2.8), read in place from the tokenizer's pieces.</summary>
public sealed class TemplateLiteral : Expression
{
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
public sealed class TaggedTemplateExpression : Expression
{
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

    internal override void AddChildren(List<SyntaxNode> into)
    {
        into.Add(Tag);
        into.Add(Quasi);
    }
}

/// <summary>An ArrayLiteral (ECMA-262 clause 13.2.4).</summary>
public sealed class ArrayLiteral : Expression
{
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

    internal override void AddChildren(List<SyntaxNode> into) => AddAll(into, Elements);
}

/// <summary>What one entry of an object literal defines (ECMA-262 clause 13.2.5).</summary>
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
public sealed class PropertyDefinition : SyntaxNode
{
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

    internal override void AddChildren(List<SyntaxNode> into)
    {
        if (PropertyKind != PropertyKind.Shorthand)
            into.Add(Key);
        into.Add(Value);
    }
}

/// <summary>A ComputedPropertyName (ECMA-262 clause 13.2.5): <c>[</c> an expression <c>]</c>, the span covering the brackets.</summary>
public sealed class ComputedPropertyName : SyntaxNode
{
    internal ComputedPropertyName(SourceSpan span, Expression expression)
        : base(SyntaxKind.ComputedPropertyName, span)
    {
        Expression = expression;
    }

    /// <summary>The key's expression.</summary>
    public Expression Expression { get; }

    internal override void AddChildren(List<SyntaxNode> into) => into.Add(Expression);
}

/// <summary>An ObjectLiteral (ECMA-262 clause 13.2.5).</summary>
public sealed class ObjectLiteral : Expression
{
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

    internal override void AddChildren(List<SyntaxNode> into) => AddAll(into, Properties);
}

/// <summary><c>...</c> and an expression, in an array literal, an object literal or an argument list (ECMA-262 clauses 13.2.4, 13.2.5 and 13.3.8).</summary>
public sealed class SpreadElement : Expression
{
    internal SpreadElement(SourceSpan span, Expression argument)
        : base(SyntaxKind.SpreadElement, span)
    {
        Argument = argument;
    }

    /// <summary>What is spread.</summary>
    public Expression Argument { get; }

    internal override void AddChildren(List<SyntaxNode> into) => into.Add(Argument);
}

/// <summary>
/// A ParenthesizedExpression (ECMA-262 clause 13.2.10), the span covering the parentheses: parenthesisation is a raw fact the
/// tree keeps as a node, and charges no tree depth (decision D14).
/// </summary>
public sealed class ParenthesizedExpression : Expression
{
    internal ParenthesizedExpression(SourceSpan span, Expression expression)
        : base(SyntaxKind.ParenthesizedExpression, span)
    {
        Expression = expression;
    }

    /// <summary>The expression inside.</summary>
    public Expression Expression { get; }

    internal override void AddChildren(List<SyntaxNode> into) => into.Add(Expression);
}

/// <summary>A function expression (ECMA-262 clauses 15.2, 15.5, 15.6 and 15.8), or an object literal's method's function.</summary>
public sealed class FunctionExpression : Expression
{
    internal FunctionExpression(SourceSpan span, FunctionNode function)
        : base(SyntaxKind.FunctionExpression, span)
    {
        Function = function;
    }

    /// <summary>The function.</summary>
    public FunctionNode Function { get; }

    internal override void AddChildren(List<SyntaxNode> into) => into.Add(Function);
}

/// <summary>An ArrowFunction or AsyncArrowFunction (ECMA-262 clauses 15.3 and 15.9), its parameters converted from the cover the parse read.</summary>
public sealed class ArrowFunctionExpression : Expression
{
    internal ArrowFunctionExpression(SourceSpan span, FunctionNode function)
        : base(SyntaxKind.ArrowFunctionExpression, span)
    {
        Function = function;
    }

    /// <summary>The function, with <see cref="FunctionNode.IsArrow"/> set.</summary>
    public FunctionNode Function { get; }

    internal override void AddChildren(List<SyntaxNode> into) => into.Add(Function);
}

/// <summary>A ClassExpression (ECMA-262 clause 15.7).</summary>
public sealed class ClassExpression : Expression
{
    internal ClassExpression(SourceSpan span, ClassNode @class)
        : base(SyntaxKind.ClassExpression, span)
    {
        Class = @class;
    }

    /// <summary>The class.</summary>
    public ClassNode Class { get; }

    internal override void AddChildren(List<SyntaxNode> into) => into.Add(Class);
}

/// <summary>
/// A property access (ECMA-262 clause 13.3.2): <c>o.p</c>, <c>o[e]</c>, <c>o.#p</c>, a SuperProperty, or a link of an optional
/// chain.
/// </summary>
public sealed class MemberExpression : Expression
{
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

    internal override void AddChildren(List<SyntaxNode> into)
    {
        into.Add(Object);
        into.Add(Property);
    }
}

/// <summary>A call (ECMA-262 clause 13.3.6): a CallExpression, a SuperCall, a CoverCallExpressionAndAsyncArrowHead read as a call, or a link of an optional chain.</summary>
public sealed class CallExpression : Expression
{
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

    internal override void AddChildren(List<SyntaxNode> into)
    {
        into.Add(Callee);
        AddAll(into, Arguments);
    }
}

/// <summary>An OptionalExpression (ECMA-262 clause 13.3.9): the whole chain whose links begin at a <c>?.</c>, which short-circuits as one.</summary>
public sealed class ChainExpression : Expression
{
    internal ChainExpression(SourceSpan span, Expression expression)
        : base(SyntaxKind.ChainExpression, span)
    {
        Expression = expression;
    }

    /// <summary>The chain's last link.</summary>
    public Expression Expression { get; }

    internal override void AddChildren(List<SyntaxNode> into) => into.Add(Expression);
}

/// <summary>A NewExpression or <c>new</c> MemberExpression Arguments (ECMA-262 clause 13.3.5).</summary>
public sealed class NewExpression : Expression
{
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

    internal override void AddChildren(List<SyntaxNode> into)
    {
        into.Add(Callee);
        AddAll(into, Arguments);
    }
}

/// <summary>A MetaProperty (ECMA-262 clause 13.3.12): <c>new.target</c> or <c>import.meta</c>.</summary>
public sealed class MetaProperty : Expression
{
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

    internal override void AddChildren(List<SyntaxNode> into)
    {
        into.Add(Meta);
        into.Add(Property);
    }
}

/// <summary>The phase an import is written with (ECMA-262 clause 16.2.2, and the import-phase proposals behind their flags, decision D17).</summary>
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
public sealed class ImportCall : Expression
{
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

    internal override void AddChildren(List<SyntaxNode> into)
    {
        into.Add(Source);
        Add(into, Options);
    }
}

/// <summary>An update operator (ECMA-262 clause 13.4).</summary>
public enum UpdateOperator
{
    /// <summary><c>++</c>.</summary>
    Increment = 0,

    /// <summary><c>--</c>.</summary>
    Decrement,
}

/// <summary>An UpdateExpression (ECMA-262 clause 13.4), prefix or postfix.</summary>
public sealed class UpdateExpression : Expression
{
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

    internal override void AddChildren(List<SyntaxNode> into) => into.Add(Operand);
}

/// <summary>A unary operator (ECMA-262 clause 13.5).</summary>
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
public sealed class UnaryExpression : Expression
{
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

    internal override void AddChildren(List<SyntaxNode> into) => into.Add(Operand);
}

/// <summary>An AwaitExpression (ECMA-262 clause 15.8).</summary>
public sealed class AwaitExpression : Expression
{
    internal AwaitExpression(SourceSpan span, Expression argument)
        : base(SyntaxKind.AwaitExpression, span)
    {
        Argument = argument;
    }

    /// <summary>The awaited operand.</summary>
    public Expression Argument { get; }

    internal override void AddChildren(List<SyntaxNode> into) => into.Add(Argument);
}

/// <summary>A binary operator (ECMA-262 clauses 13.6 to 13.12), the logical ones apart (<see cref="LogicalOperator"/>).</summary>
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
public sealed class BinaryExpression : Expression
{
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

    internal override void AddChildren(List<SyntaxNode> into)
    {
        into.Add(Left);
        into.Add(Right);
    }
}

/// <summary>A logical operator (ECMA-262 clause 13.13).</summary>
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
public sealed class LogicalExpression : Expression
{
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

    internal override void AddChildren(List<SyntaxNode> into)
    {
        into.Add(Left);
        into.Add(Right);
    }
}

/// <summary><c>#x in o</c> (ECMA-262 clause 13.10, RelationalExpression : PrivateIdentifier <c>in</c> ShiftExpression).</summary>
public sealed class PrivateInExpression : Expression
{
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

    internal override void AddChildren(List<SyntaxNode> into)
    {
        into.Add(Name);
        into.Add(Right);
    }
}

/// <summary>A ConditionalExpression (ECMA-262 clause 13.14).</summary>
public sealed class ConditionalExpression : Expression
{
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

    internal override void AddChildren(List<SyntaxNode> into)
    {
        into.Add(Test);
        into.Add(Consequent);
        into.Add(Alternate);
    }
}

/// <summary>An assignment operator (ECMA-262 clause 13.15).</summary>
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
public sealed class AssignmentExpression : Expression
{
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

    internal override void AddChildren(List<SyntaxNode> into)
    {
        into.Add(Target);
        into.Add(Value);
    }
}

/// <summary>The comma operator (ECMA-262 clause 13.16).</summary>
public sealed class SequenceExpression : Expression
{
    internal SequenceExpression(SourceSpan span, IReadOnlyList<Expression> expressions)
        : base(SyntaxKind.SequenceExpression, span)
    {
        Expressions = expressions;
    }

    /// <summary>The operands, two or more, in source order.</summary>
    public IReadOnlyList<Expression> Expressions { get; }

    internal override void AddChildren(List<SyntaxNode> into) => AddAll(into, Expressions);
}

/// <summary>A YieldExpression (ECMA-262 clause 15.5).</summary>
public sealed class YieldExpression : Expression
{
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

    internal override void AddChildren(List<SyntaxNode> into) => Add(into, Argument);
}

/// <summary>A decorator, <c>@</c> and its expression, admitted only under the decorators proposal's flag (decision D17).</summary>
public sealed class Decorator : SyntaxNode
{
    internal Decorator(SourceSpan span, Expression expression)
        : base(SyntaxKind.Decorator, span)
    {
        Expression = expression;
    }

    /// <summary>The decorator's expression: a member chain, a call of one, or a parenthesised expression.</summary>
    public Expression Expression { get; }

    internal override void AddChildren(List<SyntaxNode> into) => into.Add(Expression);
}
