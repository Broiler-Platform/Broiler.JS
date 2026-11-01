// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;

namespace Broiler.ECMAScript.Syntax;

/// <summary>
/// One function's parameters and body (ECMA-262 clauses 15.1 to 15.9), shared by its declaration, expression, arrow and
/// method forms, as Broiler.VM's tree shares it.
/// </summary>
/// <remarks>
/// Its span is the source text the function's declaration, expression, arrow or MethodDefinition matches (the text
/// <c>Function.prototype.toString</c> answers, clause 20.2.3.5): for a method it begins at the method's first token after
/// <c>static</c>, its name included, so it overlaps the name its MethodDefinition or PropertyDefinition holds.
/// </remarks>
public sealed class FunctionNode : SyntaxNode
{
    internal FunctionNode(
        SourceSpan span,
        BindingIdentifier? name,
        IReadOnlyList<Pattern> parameters,
        bool parametersTrailingComma,
        IReadOnlyList<Statement> body,
        Expression? expressionBody,
        bool isAsync,
        bool isGenerator,
        bool isArrow)
        : base(SyntaxKind.Function, span)
    {
        Name = name;
        Parameters = parameters;
        ParametersTrailingComma = parametersTrailingComma;
        Body = body;
        ExpressionBody = expressionBody;
        IsAsync = isAsync;
        IsGenerator = isGenerator;
        IsArrow = isArrow;
    }

    /// <summary>The name a declaration or a named expression binds; null for an anonymous function, an arrow or a method.</summary>
    public BindingIdentifier? Name { get; }

    /// <summary>The FormalParameters (clause 15.1): each a <see cref="BindingIdentifier"/>, a pattern or an <see cref="AssignmentPattern"/>, a <see cref="RestElement"/> last where one is written.</summary>
    public IReadOnlyList<Pattern> Parameters { get; }

    /// <summary>Whether a comma closes the parameter list, a raw fact.</summary>
    public bool ParametersTrailingComma { get; }

    /// <summary>The FunctionBody's statements, its directive prologue first (clause 11.2.1); empty for an arrow with an expression body.</summary>
    public IReadOnlyList<Statement> Body { get; }

    /// <summary>An arrow function's ExpressionBody (clause 15.3), or null.</summary>
    public Expression? ExpressionBody { get; }

    /// <summary>Whether it is an async function (clauses 15.8 and 15.9).</summary>
    public bool IsAsync { get; }

    /// <summary>Whether it is a generator (clauses 15.5 and 15.6).</summary>
    public bool IsGenerator { get; }

    /// <summary>Whether it is an arrow function (clauses 15.3 and 15.9).</summary>
    public bool IsArrow { get; }

    internal override void AddChildren(List<SyntaxNode> into)
    {
        Add(into, Name);
        AddAll(into, Parameters);
        AddAll(into, Body);
        Add(into, ExpressionBody);
    }
}

/// <summary>One class's heritage and body (ECMA-262 clause 15.7), shared by its declaration and expression forms.</summary>
public sealed class ClassNode : SyntaxNode
{
    internal ClassNode(SourceSpan span, IReadOnlyList<Decorator> decorators, BindingIdentifier? name, Expression? heritage, IReadOnlyList<SyntaxNode> elements)
        : base(SyntaxKind.Class, span)
    {
        Decorators = decorators;
        Name = name;
        Heritage = heritage;
        Elements = elements;
    }

    /// <summary>The decorators before the class, under the decorators proposal's flag; empty otherwise.</summary>
    public IReadOnlyList<Decorator> Decorators { get; }

    /// <summary>The class's BindingIdentifier, or null where none is written.</summary>
    public BindingIdentifier? Name { get; }

    /// <summary>The ClassHeritage's expression, or null where no <c>extends</c> clause is written.</summary>
    public Expression? Heritage { get; }

    /// <summary>The ClassElements in source order: each a <see cref="MethodDefinition"/>, a <see cref="FieldDefinition"/> or a <see cref="StaticBlock"/>; an empty element (<c>;</c>) has no node.</summary>
    public IReadOnlyList<SyntaxNode> Elements { get; }

    internal override void AddChildren(List<SyntaxNode> into)
    {
        AddAll(into, Decorators);
        Add(into, Name);
        Add(into, Heritage);
        AddAll(into, Elements);
    }
}

/// <summary>What a class element's method defines (ECMA-262 clauses 15.4 and 15.7).</summary>
public enum MethodKind
{
    /// <summary>A method: <c>m() {}</c>, a generator method or an async method.</summary>
    Method = 0,

    /// <summary>A getter.</summary>
    Get,

    /// <summary>A setter.</summary>
    Set,
}

/// <summary>A class element's MethodDefinition (ECMA-262 clauses 15.4 and 15.7), with <c>static</c> where written.</summary>
public sealed class MethodDefinition : SyntaxNode
{
    internal MethodDefinition(SourceSpan span, IReadOnlyList<Decorator> decorators, bool isStatic, MethodKind methodKind, SyntaxNode key, FunctionNode function)
        : base(SyntaxKind.MethodDefinition, span)
    {
        Decorators = decorators;
        IsStatic = isStatic;
        MethodKind = methodKind;
        Key = key;
        Function = function;
    }

    /// <summary>The decorators before the element, under the decorators proposal's flag; empty otherwise.</summary>
    public IReadOnlyList<Decorator> Decorators { get; }

    /// <summary>Whether it is written <c>static</c>.</summary>
    public bool IsStatic { get; }

    /// <summary>What it defines.</summary>
    public MethodKind MethodKind { get; }

    /// <summary>The ClassElementName: an <see cref="Syntax.IdentifierName"/>, a <see cref="StringLiteral"/>, a <see cref="NumericLiteral"/>, a <see cref="ComputedPropertyName"/> or a <see cref="PrivateIdentifier"/>.</summary>
    public SyntaxNode Key { get; }

    /// <summary>The method's function.</summary>
    public FunctionNode Function { get; }

    internal override void AddChildren(List<SyntaxNode> into)
    {
        AddAll(into, Decorators);
        into.Add(Key);
        into.Add(Function);
    }
}

/// <summary>A FieldDefinition (ECMA-262 clause 15.7), or an auto-accessor under the decorators proposal's <c>accessor</c> flag (decision D17).</summary>
public sealed class FieldDefinition : SyntaxNode
{
    internal FieldDefinition(SourceSpan span, IReadOnlyList<Decorator> decorators, bool isStatic, bool isAccessor, SyntaxNode key, Expression? value)
        : base(SyntaxKind.FieldDefinition, span)
    {
        Decorators = decorators;
        IsStatic = isStatic;
        IsAccessor = isAccessor;
        Key = key;
        Value = value;
    }

    /// <summary>The decorators before the element, under the decorators proposal's flag; empty otherwise.</summary>
    public IReadOnlyList<Decorator> Decorators { get; }

    /// <summary>Whether it is written <c>static</c>.</summary>
    public bool IsStatic { get; }

    /// <summary>Whether it is written <c>accessor</c>, admitted only under its proposal flag.</summary>
    public bool IsAccessor { get; }

    /// <summary>The ClassElementName, as <see cref="MethodDefinition.Key"/>.</summary>
    public SyntaxNode Key { get; }

    /// <summary>The Initializer's expression, or null where none is written.</summary>
    public Expression? Value { get; }

    internal override void AddChildren(List<SyntaxNode> into)
    {
        AddAll(into, Decorators);
        into.Add(Key);
        Add(into, Value);
    }
}

/// <summary>A ClassStaticBlock (ECMA-262 clause 15.7): <c>static {</c> statements <c>}</c>.</summary>
public sealed class StaticBlock : SyntaxNode
{
    internal StaticBlock(SourceSpan span, IReadOnlyList<Statement> body)
        : base(SyntaxKind.StaticBlock, span)
    {
        Body = body;
    }

    /// <summary>The ClassStaticBlockStatementList.</summary>
    public IReadOnlyList<Statement> Body { get; }

    internal override void AddChildren(List<SyntaxNode> into) => AddAll(into, Body);
}
