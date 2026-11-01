// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;

namespace Broiler.ECMAScript.Syntax;

/// <summary>
/// The base of every binding and assignment target (ECMA-262 clauses 13.15.5, 14.3.3 and 15.1): a name, a destructuring
/// pattern, a target with its initializer, a rest element, or an assignment pattern's expression target.
/// </summary>
/// <remarks>
/// An assignment pattern and an arrow function's parameters are converted from the cover the parse read (an array or object
/// literal, a parenthesised list, an async call's arguments), never parsed a second time.
/// </remarks>
public abstract class Pattern : SyntaxNode
{
    private protected Pattern(SyntaxKind kind, SourceSpan span)
        : base(kind, span)
    {
    }
}

/// <summary>A BindingIdentifier (ECMA-262 clause 13.1): a name a declaration, a parameter or a binding pattern binds.</summary>
public sealed class BindingIdentifier : Pattern
{
    internal BindingIdentifier(SourceSpan span, string name, bool isEscaped)
        : base(SyntaxKind.BindingIdentifier, span)
    {
        Name = name;
        IsEscaped = isEscaped;
    }

    /// <summary>Its StringValue (clause 13.1.3).</summary>
    public string Name { get; }

    /// <summary>Whether it is written with a <c>\u</c> escape (clause 12.7), a raw fact.</summary>
    public bool IsEscaped { get; }

    internal override void AddChildren(List<SyntaxNode> into)
    {
    }
}

/// <summary>An ArrayBindingPattern or ArrayAssignmentPattern (ECMA-262 clauses 14.3.3 and 13.15.5).</summary>
public sealed class ArrayPattern : Pattern
{
    internal ArrayPattern(SourceSpan span, IReadOnlyList<Pattern?> elements, bool trailingComma)
        : base(SyntaxKind.ArrayPattern, span)
    {
        Elements = elements;
        TrailingComma = trailingComma;
    }

    /// <summary>The elements, null for an elision and a <see cref="RestElement"/> last where one is written.</summary>
    public IReadOnlyList<Pattern?> Elements { get; }

    /// <summary>Whether a comma closes the list after its last element, a raw fact.</summary>
    public bool TrailingComma { get; }

    internal override void AddChildren(List<SyntaxNode> into) => AddAll(into, Elements);
}

/// <summary>A BindingProperty or AssignmentProperty (ECMA-262 clauses 14.3.3 and 13.15.5).</summary>
public sealed class PatternProperty : SyntaxNode
{
    internal PatternProperty(SourceSpan span, SyntaxNode key, Pattern value, bool isShorthand)
        : base(SyntaxKind.PatternProperty, span)
    {
        Key = key;
        Value = value;
        IsShorthand = isShorthand;
    }

    /// <summary>The PropertyName: an <see cref="Syntax.IdentifierName"/>, a <see cref="StringLiteral"/>, a <see cref="NumericLiteral"/> or a <see cref="ComputedPropertyName"/>.</summary>
    public SyntaxNode Key { get; }

    /// <summary>The target, with its initializer where one is written (<see cref="AssignmentPattern"/>).</summary>
    public Pattern Value { get; }

    /// <summary>Whether it is written <c>{ x }</c> or <c>{ x = 1 }</c>, the name serving as key and target.</summary>
    public bool IsShorthand { get; }

    internal override void AddChildren(List<SyntaxNode> into)
    {
        if (!IsShorthand)
            into.Add(Key);
        into.Add(Value);
    }
}

/// <summary>An ObjectBindingPattern or ObjectAssignmentPattern (ECMA-262 clauses 14.3.3 and 13.15.5).</summary>
public sealed class ObjectPattern : Pattern
{
    internal ObjectPattern(SourceSpan span, IReadOnlyList<SyntaxNode> properties, bool trailingComma)
        : base(SyntaxKind.ObjectPattern, span)
    {
        Properties = properties;
        TrailingComma = trailingComma;
    }

    /// <summary>The properties, each a <see cref="PatternProperty"/>, a <see cref="RestElement"/> last where one is written.</summary>
    public IReadOnlyList<SyntaxNode> Properties { get; }

    /// <summary>Whether a comma closes the list after its last property, a raw fact.</summary>
    public bool TrailingComma { get; }

    internal override void AddChildren(List<SyntaxNode> into) => AddAll(into, Properties);
}

/// <summary>A target with its Initializer (ECMA-262 clauses 13.15.5 and 14.3.3): a SingleNameBinding or BindingElement with one, or an AssignmentElement.</summary>
public sealed class AssignmentPattern : Pattern
{
    internal AssignmentPattern(SourceSpan span, Pattern target, Expression initializer)
        : base(SyntaxKind.AssignmentPattern, span)
    {
        Target = target;
        Initializer = initializer;
    }

    /// <summary>The target.</summary>
    public Pattern Target { get; }

    /// <summary>The initializer, evaluated when the value is undefined.</summary>
    public Expression Initializer { get; }

    internal override void AddChildren(List<SyntaxNode> into)
    {
        into.Add(Target);
        into.Add(Initializer);
    }
}

/// <summary><c>...</c> and a target (ECMA-262 clauses 13.15.5, 14.3.3 and 15.1): a rest element, property or parameter.</summary>
public sealed class RestElement : Pattern
{
    internal RestElement(SourceSpan span, Pattern argument)
        : base(SyntaxKind.RestElement, span)
    {
        Argument = argument;
    }

    /// <summary>The target.</summary>
    public Pattern Argument { get; }

    internal override void AddChildren(List<SyntaxNode> into) => into.Add(Argument);
}

/// <summary>
/// An assignment pattern's DestructuringAssignmentTarget that is not itself a pattern (ECMA-262 clause 13.15.5): a
/// LeftHandSideExpression, as Broiler.VM's tree keeps it. Whether it is a valid target is a static-semantic rule (stage 4d).
/// </summary>
public sealed class TargetPattern : Pattern
{
    internal TargetPattern(SourceSpan span, Expression target)
        : base(SyntaxKind.TargetPattern, span)
    {
        Target = target;
    }

    /// <summary>The target expression.</summary>
    public Expression Target { get; }

    internal override void AddChildren(List<SyntaxNode> into) => into.Add(Target);
}
