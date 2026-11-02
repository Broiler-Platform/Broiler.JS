// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   23
// Annotated:        23/23
// Exempt:           13
// Human-reviewed:   0/23
// IP risk:          Low
// Security risk:    Low
// Criteria:         0/0
// Resource impact:  1/10 max
// Unverified:       23
//
// GENERATED - DO NOT EDIT MANUALLY

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
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s13.15.5 s14.3.3 patterns tree; IP=Low; Security=Low; Resources=0; Fingerprint=2EFFA8
// Broiler-Human:        PENDING
public abstract class Pattern : SyntaxNode
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13.15.5 s14.3.3 patterns tree; IP=Low; Security=Low; Resources=1; Fingerprint=E9E79F
    // Broiler-Human:        PENDING
    private protected Pattern(SyntaxKind kind, SourceSpan span)
        : base(kind, span)
    {
    }
}

/// <summary>A BindingIdentifier (ECMA-262 clause 13.1): a name a declaration, a parameter or a binding pattern binds.</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s13.15.5 s14.3.3 patterns tree; IP=Low; Security=Low; Resources=0; Fingerprint=D18FE1
// Broiler-Human:        PENDING
public sealed class BindingIdentifier : Pattern
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13.15.5 s14.3.3 patterns tree; IP=Low; Security=Low; Resources=1; Fingerprint=E0A240
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13.15.5 s14.3.3 patterns tree; IP=Low; Security=Low; Resources=1; Fingerprint=CA8EA6
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into)
    {
    }
}

/// <summary>An ArrayBindingPattern or ArrayAssignmentPattern (ECMA-262 clauses 14.3.3 and 13.15.5).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s13.15.5 s14.3.3 patterns tree; IP=Low; Security=Low; Resources=0; Fingerprint=A5B017
// Broiler-Human:        PENDING
public sealed class ArrayPattern : Pattern
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13.15.5 s14.3.3 patterns tree; IP=Low; Security=Low; Resources=1; Fingerprint=E721C4
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13.15.5 s14.3.3 patterns tree; IP=Low; Security=Low; Resources=1; Fingerprint=470A6A
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into) => AddAll(into, Elements);
}

/// <summary>A BindingProperty or AssignmentProperty (ECMA-262 clauses 14.3.3 and 13.15.5).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s13.15.5 s14.3.3 patterns tree; IP=Low; Security=Low; Resources=0; Fingerprint=1F7D48
// Broiler-Human:        PENDING
public sealed class PatternProperty : SyntaxNode
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13.15.5 s14.3.3 patterns tree; IP=Low; Security=Low; Resources=1; Fingerprint=549578
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13.15.5 s14.3.3 patterns tree; IP=Low; Security=Low; Resources=1; Fingerprint=EBBC47
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into)
    {
        if (!IsShorthand)
            into.Add(Key);
        into.Add(Value);
    }
}

/// <summary>An ObjectBindingPattern or ObjectAssignmentPattern (ECMA-262 clauses 14.3.3 and 13.15.5).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s13.15.5 s14.3.3 patterns tree; IP=Low; Security=Low; Resources=0; Fingerprint=6DF749
// Broiler-Human:        PENDING
public sealed class ObjectPattern : Pattern
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13.15.5 s14.3.3 patterns tree; IP=Low; Security=Low; Resources=1; Fingerprint=5B76E1
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13.15.5 s14.3.3 patterns tree; IP=Low; Security=Low; Resources=1; Fingerprint=448E81
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into) => AddAll(into, Properties);
}

/// <summary>A target with its Initializer (ECMA-262 clauses 13.15.5 and 14.3.3): a SingleNameBinding or BindingElement with one, or an AssignmentElement.</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s13.15.5 s14.3.3 patterns tree; IP=Low; Security=Low; Resources=0; Fingerprint=E890B8
// Broiler-Human:        PENDING
public sealed class AssignmentPattern : Pattern
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13.15.5 s14.3.3 patterns tree; IP=Low; Security=Low; Resources=1; Fingerprint=4880EE
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13.15.5 s14.3.3 patterns tree; IP=Low; Security=Low; Resources=1; Fingerprint=23D6E2
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into)
    {
        into.Add(Target);
        into.Add(Initializer);
    }
}

/// <summary><c>...</c> and a target (ECMA-262 clauses 13.15.5, 14.3.3 and 15.1): a rest element, property or parameter.</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s13.15.5 s14.3.3 patterns tree; IP=Low; Security=Low; Resources=0; Fingerprint=8E7E9C
// Broiler-Human:        PENDING
public sealed class RestElement : Pattern
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13.15.5 s14.3.3 patterns tree; IP=Low; Security=Low; Resources=1; Fingerprint=B9BF76
    // Broiler-Human:        PENDING
    internal RestElement(SourceSpan span, Pattern argument)
        : base(SyntaxKind.RestElement, span)
    {
        Argument = argument;
    }

    /// <summary>The target.</summary>
    public Pattern Argument { get; }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13.15.5 s14.3.3 patterns tree; IP=Low; Security=Low; Resources=1; Fingerprint=DCA11B
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into) => into.Add(Argument);
}

/// <summary>
/// An assignment pattern's DestructuringAssignmentTarget that is not itself a pattern (ECMA-262 clause 13.15.5): a
/// LeftHandSideExpression, as Broiler.VM's tree keeps it. Whether it is a valid target is a static-semantic rule (stage 4d).
/// </summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s13.15.5 s14.3.3 patterns tree; IP=Low; Security=Low; Resources=0; Fingerprint=B7EA98
// Broiler-Human:        PENDING
public sealed class TargetPattern : Pattern
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13.15.5 s14.3.3 patterns tree; IP=Low; Security=Low; Resources=1; Fingerprint=E6CDC5
    // Broiler-Human:        PENDING
    internal TargetPattern(SourceSpan span, Expression target)
        : base(SyntaxKind.TargetPattern, span)
    {
        Target = target;
    }

    /// <summary>The target expression.</summary>
    public Expression Target { get; }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13.15.5 s14.3.3 patterns tree; IP=Low; Security=Low; Resources=1; Fingerprint=99F89A
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into) => into.Add(Target);
}
