// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   75
// Annotated:        75/75
// Exempt:           49
// Human-reviewed:   0/75
// IP risk:          Low
// Security risk:    Low
// Criteria:         0/0
// Resource impact:  1/10 max
// Unverified:       75
//
// GENERATED - DO NOT EDIT MANUALLY

using System.Collections.Generic;

namespace Broiler.ECMAScript.Syntax;

/// <summary>The base of every statement, declaration and module item (ECMA-262 clauses 14, 15 and 16.2).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=739BE0
// Broiler-Human:        PENDING
public abstract class Statement : SyntaxNode
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=5C8034
    // Broiler-Human:        PENDING
    private protected Statement(SyntaxKind kind, SourceSpan span)
        : base(kind, span)
    {
    }
}

/// <summary>A Block (ECMA-262 clause 14.2).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=B49C3C
// Broiler-Human:        PENDING
public sealed class BlockStatement : Statement
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=D46BCA
    // Broiler-Human:        PENDING
    internal BlockStatement(SourceSpan span, IReadOnlyList<Statement> body)
        : base(SyntaxKind.BlockStatement, span)
    {
        Body = body;
    }

    /// <summary>The StatementList.</summary>
    public IReadOnlyList<Statement> Body { get; }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=6ECCEA
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into) => AddAll(into, Body);
}

/// <summary>What a declaration declares (ECMA-262 clauses 14.3.1 and 14.3.2, and explicit resource management).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=B2352A
// Broiler-Human:        PENDING
public enum DeclarationKind
{
    /// <summary><c>var</c>.</summary>
    Var = 0,

    /// <summary><c>let</c>.</summary>
    Let,

    /// <summary><c>const</c>.</summary>
    Const,

    /// <summary><c>using</c>.</summary>
    Using,

    /// <summary><c>await using</c>.</summary>
    AwaitUsing,
}

/// <summary>A VariableStatement, a LexicalDeclaration, or a for head's declaration (ECMA-262 clauses 14.3 and 14.7).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=8D905C
// Broiler-Human:        PENDING
public sealed class VariableDeclaration : Statement
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=DD21DA
    // Broiler-Human:        PENDING
    internal VariableDeclaration(SourceSpan span, DeclarationKind declarationKind, IReadOnlyList<VariableDeclarator> declarations)
        : base(SyntaxKind.VariableDeclaration, span)
    {
        DeclarationKind = declarationKind;
        Declarations = declarations;
    }

    /// <summary>What it declares.</summary>
    public DeclarationKind DeclarationKind { get; }

    /// <summary>The bindings, in source order.</summary>
    public IReadOnlyList<VariableDeclarator> Declarations { get; }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=04EB10
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into) => AddAll(into, Declarations);
}

/// <summary>One VariableDeclaration or LexicalBinding (ECMA-262 clauses 14.3.1 and 14.3.2).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=245788
// Broiler-Human:        PENDING
public sealed class VariableDeclarator : SyntaxNode
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=EA6BC2
    // Broiler-Human:        PENDING
    internal VariableDeclarator(SourceSpan span, Pattern target, Expression? initializer)
        : base(SyntaxKind.VariableDeclarator, span)
    {
        Target = target;
        Initializer = initializer;
    }

    /// <summary>The <see cref="BindingIdentifier"/> or binding pattern.</summary>
    public Pattern Target { get; }

    /// <summary>The Initializer's expression, or null. A missing one is a static-semantic matter (stage 4d).</summary>
    public Expression? Initializer { get; }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=F125E6
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into)
    {
        into.Add(Target);
        Add(into, Initializer);
    }
}

/// <summary>An EmptyStatement (ECMA-262 clause 14.4).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=947F65
// Broiler-Human:        PENDING
public sealed class EmptyStatement : Statement
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=7E43E3
    // Broiler-Human:        PENDING
    internal EmptyStatement(SourceSpan span)
        : base(SyntaxKind.EmptyStatement, span)
    {
    }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=CA8EA6
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into)
    {
    }
}

/// <summary>An ExpressionStatement (ECMA-262 clause 14.5), a directive of a prologue among them (clause 11.2.1).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=9B3B4D
// Broiler-Human:        PENDING
public sealed class ExpressionStatement : Statement
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=E8981F
    // Broiler-Human:        PENDING
    internal ExpressionStatement(SourceSpan span, Expression expression, string? directive)
        : base(SyntaxKind.ExpressionStatement, span)
    {
        Expression = expression;
        Directive = directive;
    }

    /// <summary>The expression.</summary>
    public Expression Expression { get; }

    /// <summary>
    /// For a statement of a directive prologue, the directive's raw text: the code units between the string literal's quotes,
    /// as written (clause 11.2.1, a Use Strict Directive being exactly <c>use strict</c> with no escape); null otherwise.
    /// </summary>
    public string? Directive { get; }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=9961D5
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into) => into.Add(Expression);
}

/// <summary>An IfStatement (ECMA-262 clause 14.6), or Annex B.3.3's form with a FunctionDeclaration as a clause in sloppy code.</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=664E1B
// Broiler-Human:        PENDING
public sealed class IfStatement : Statement
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=B6AB6B
    // Broiler-Human:        PENDING
    internal IfStatement(SourceSpan span, Expression test, Statement consequent, Statement? alternate)
        : base(SyntaxKind.IfStatement, span)
    {
        Test = test;
        Consequent = consequent;
        Alternate = alternate;
    }

    /// <summary>The condition.</summary>
    public Expression Test { get; }

    /// <summary>The statement when it holds, as written: a function declaration there is kept raw, not wrapped in a block.</summary>
    public Statement Consequent { get; }

    /// <summary>The <c>else</c> clause, or null.</summary>
    public Statement? Alternate { get; }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=7F687E
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into)
    {
        into.Add(Test);
        into.Add(Consequent);
        Add(into, Alternate);
    }
}

/// <summary>A DoWhileStatement (ECMA-262 clause 14.7.2).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=0D9845
// Broiler-Human:        PENDING
public sealed class DoWhileStatement : Statement
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=F8C51A
    // Broiler-Human:        PENDING
    internal DoWhileStatement(SourceSpan span, Statement body, Expression test)
        : base(SyntaxKind.DoWhileStatement, span)
    {
        Body = body;
        Test = test;
    }

    /// <summary>The body.</summary>
    public Statement Body { get; }

    /// <summary>The condition.</summary>
    public Expression Test { get; }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=53ED90
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into)
    {
        into.Add(Body);
        into.Add(Test);
    }
}

/// <summary>A WhileStatement (ECMA-262 clause 14.7.3).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=3D86A7
// Broiler-Human:        PENDING
public sealed class WhileStatement : Statement
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=3EC2D6
    // Broiler-Human:        PENDING
    internal WhileStatement(SourceSpan span, Expression test, Statement body)
        : base(SyntaxKind.WhileStatement, span)
    {
        Test = test;
        Body = body;
    }

    /// <summary>The condition.</summary>
    public Expression Test { get; }

    /// <summary>The body.</summary>
    public Statement Body { get; }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=C3EF53
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into)
    {
        into.Add(Test);
        into.Add(Body);
    }
}

/// <summary>A ForStatement (ECMA-262 clause 14.7.4), as written: the tree does not desugar a lexical head.</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=A3C52B
// Broiler-Human:        PENDING
public sealed class ForStatement : Statement
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=28955B
    // Broiler-Human:        PENDING
    internal ForStatement(SourceSpan span, SyntaxNode? init, Expression? test, Expression? update, Statement body)
        : base(SyntaxKind.ForStatement, span)
    {
        Init = init;
        Test = test;
        Update = update;
        Body = body;
    }

    /// <summary>The head's first part: a <see cref="VariableDeclaration"/>, an <see cref="Expression"/>, or null.</summary>
    public SyntaxNode? Init { get; }

    /// <summary>The condition, or null.</summary>
    public Expression? Test { get; }

    /// <summary>The increment, or null.</summary>
    public Expression? Update { get; }

    /// <summary>The body.</summary>
    public Statement Body { get; }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=9DF0BD
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into)
    {
        Add(into, Init);
        Add(into, Test);
        Add(into, Update);
        into.Add(Body);
    }
}

/// <summary>A <c>for</c>-<c>in</c> statement (ECMA-262 clause 14.7.5), Annex B.3.5's initializer form among them.</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=263710
// Broiler-Human:        PENDING
public sealed class ForInStatement : Statement
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=29A73E
    // Broiler-Human:        PENDING
    internal ForInStatement(SourceSpan span, SyntaxNode left, Expression right, Statement body)
        : base(SyntaxKind.ForInStatement, span)
    {
        Left = left;
        Right = right;
        Body = body;
    }

    /// <summary>The head's target: a <see cref="VariableDeclaration"/> of one binding, a converted <see cref="Pattern"/>, or a LeftHandSideExpression.</summary>
    public SyntaxNode Left { get; }

    /// <summary>The object whose keys are enumerated.</summary>
    public Expression Right { get; }

    /// <summary>The body.</summary>
    public Statement Body { get; }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=49D089
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into)
    {
        into.Add(Left);
        into.Add(Right);
        into.Add(Body);
    }
}

/// <summary>A <c>for</c>-<c>of</c> or <c>for await</c>-<c>of</c> statement (ECMA-262 clause 14.7.5).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=7FE3BE
// Broiler-Human:        PENDING
public sealed class ForOfStatement : Statement
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=427EBC
    // Broiler-Human:        PENDING
    internal ForOfStatement(SourceSpan span, SyntaxNode left, Expression right, Statement body, bool isAwait)
        : base(SyntaxKind.ForOfStatement, span)
    {
        Left = left;
        Right = right;
        Body = body;
        IsAwait = isAwait;
    }

    /// <summary>The head's target, as <see cref="ForInStatement.Left"/>.</summary>
    public SyntaxNode Left { get; }

    /// <summary>The iterable.</summary>
    public Expression Right { get; }

    /// <summary>The body.</summary>
    public Statement Body { get; }

    /// <summary>Whether it is written <c>for await</c>.</summary>
    public bool IsAwait { get; }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=49D089
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into)
    {
        into.Add(Left);
        into.Add(Right);
        into.Add(Body);
    }
}

/// <summary>A ContinueStatement (ECMA-262 clause 14.8).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=102F35
// Broiler-Human:        PENDING
public sealed class ContinueStatement : Statement
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=4F7B23
    // Broiler-Human:        PENDING
    internal ContinueStatement(SourceSpan span, IdentifierName? label)
        : base(SyntaxKind.ContinueStatement, span)
    {
        Label = label;
    }

    /// <summary>The LabelIdentifier, or null.</summary>
    public IdentifierName? Label { get; }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=97FFB2
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into) => Add(into, Label);
}

/// <summary>A BreakStatement (ECMA-262 clause 14.9).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=98497C
// Broiler-Human:        PENDING
public sealed class BreakStatement : Statement
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=9FC891
    // Broiler-Human:        PENDING
    internal BreakStatement(SourceSpan span, IdentifierName? label)
        : base(SyntaxKind.BreakStatement, span)
    {
        Label = label;
    }

    /// <summary>The LabelIdentifier, or null.</summary>
    public IdentifierName? Label { get; }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=97FFB2
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into) => Add(into, Label);
}

/// <summary>A ReturnStatement (ECMA-262 clause 14.10).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=25D80B
// Broiler-Human:        PENDING
public sealed class ReturnStatement : Statement
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=DAB5CD
    // Broiler-Human:        PENDING
    internal ReturnStatement(SourceSpan span, Expression? argument)
        : base(SyntaxKind.ReturnStatement, span)
    {
        Argument = argument;
    }

    /// <summary>The returned expression, or null.</summary>
    public Expression? Argument { get; }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=5D8557
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into) => Add(into, Argument);
}

/// <summary>A WithStatement (ECMA-262 clause 14.11); its refusal in strict code is a static-semantic rule (stage 4d).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=0B6082
// Broiler-Human:        PENDING
public sealed class WithStatement : Statement
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=06BEEB
    // Broiler-Human:        PENDING
    internal WithStatement(SourceSpan span, Expression @object, Statement body)
        : base(SyntaxKind.WithStatement, span)
    {
        Object = @object;
        Body = body;
    }

    /// <summary>The object.</summary>
    public Expression Object { get; }

    /// <summary>The body.</summary>
    public Statement Body { get; }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=D930A0
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into)
    {
        into.Add(Object);
        into.Add(Body);
    }
}

/// <summary>A SwitchStatement (ECMA-262 clause 14.12).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=397856
// Broiler-Human:        PENDING
public sealed class SwitchStatement : Statement
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=5F4167
    // Broiler-Human:        PENDING
    internal SwitchStatement(SourceSpan span, Expression discriminant, IReadOnlyList<SwitchCase> cases)
        : base(SyntaxKind.SwitchStatement, span)
    {
        Discriminant = discriminant;
        Cases = cases;
    }

    /// <summary>The value switched on.</summary>
    public Expression Discriminant { get; }

    /// <summary>The CaseClauses and the DefaultClause, in source order.</summary>
    public IReadOnlyList<SwitchCase> Cases { get; }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=4095B0
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into)
    {
        into.Add(Discriminant);
        AddAll(into, Cases);
    }
}

/// <summary>A CaseClause or the DefaultClause (ECMA-262 clause 14.12).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=B1510A
// Broiler-Human:        PENDING
public sealed class SwitchCase : SyntaxNode
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=5A78D1
    // Broiler-Human:        PENDING
    internal SwitchCase(SourceSpan span, Expression? test, IReadOnlyList<Statement> consequent)
        : base(SyntaxKind.SwitchCase, span)
    {
        Test = test;
        Consequent = consequent;
    }

    /// <summary>The case's expression; null for <c>default</c>.</summary>
    public Expression? Test { get; }

    /// <summary>The StatementList.</summary>
    public IReadOnlyList<Statement> Consequent { get; }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=4D1CB2
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into)
    {
        Add(into, Test);
        AddAll(into, Consequent);
    }
}

/// <summary>A LabelledStatement (ECMA-262 clause 14.13).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=22EB11
// Broiler-Human:        PENDING
public sealed class LabelledStatement : Statement
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=80A057
    // Broiler-Human:        PENDING
    internal LabelledStatement(SourceSpan span, IdentifierName label, Statement body)
        : base(SyntaxKind.LabelledStatement, span)
    {
        Label = label;
        Body = body;
    }

    /// <summary>The LabelIdentifier.</summary>
    public IdentifierName Label { get; }

    /// <summary>The LabelledItem: a statement, or a function declaration (whose admission is a static-semantic rule, stage 4d).</summary>
    public Statement Body { get; }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=3C0597
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into)
    {
        into.Add(Label);
        into.Add(Body);
    }
}

/// <summary>A ThrowStatement (ECMA-262 clause 14.14).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=38AEA4
// Broiler-Human:        PENDING
public sealed class ThrowStatement : Statement
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=1F4672
    // Broiler-Human:        PENDING
    internal ThrowStatement(SourceSpan span, Expression argument)
        : base(SyntaxKind.ThrowStatement, span)
    {
        Argument = argument;
    }

    /// <summary>The thrown expression.</summary>
    public Expression Argument { get; }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=DCA11B
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into) => into.Add(Argument);
}

/// <summary>A TryStatement (ECMA-262 clause 14.15).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=6B9068
// Broiler-Human:        PENDING
public sealed class TryStatement : Statement
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=B1B55B
    // Broiler-Human:        PENDING
    internal TryStatement(SourceSpan span, BlockStatement block, CatchClause? handler, BlockStatement? finalizer)
        : base(SyntaxKind.TryStatement, span)
    {
        Block = block;
        Handler = handler;
        Finalizer = finalizer;
    }

    /// <summary>The protected block.</summary>
    public BlockStatement Block { get; }

    /// <summary>The Catch, or null.</summary>
    public CatchClause? Handler { get; }

    /// <summary>The Finally block, or null.</summary>
    public BlockStatement? Finalizer { get; }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=F9C51D
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into)
    {
        into.Add(Block);
        Add(into, Handler);
        Add(into, Finalizer);
    }
}

/// <summary>A Catch (ECMA-262 clause 14.15).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=8D7A60
// Broiler-Human:        PENDING
public sealed class CatchClause : SyntaxNode
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=8A49F2
    // Broiler-Human:        PENDING
    internal CatchClause(SourceSpan span, Pattern? parameter, BlockStatement body)
        : base(SyntaxKind.CatchClause, span)
    {
        Parameter = parameter;
        Body = body;
    }

    /// <summary>The CatchParameter, or null where <c>catch</c> is written with no binding.</summary>
    public Pattern? Parameter { get; }

    /// <summary>The block.</summary>
    public BlockStatement Body { get; }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=0A622A
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into)
    {
        Add(into, Parameter);
        into.Add(Body);
    }
}

/// <summary>A DebuggerStatement (ECMA-262 clause 14.16).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=D67C59
// Broiler-Human:        PENDING
public sealed class DebuggerStatement : Statement
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=CA75B6
    // Broiler-Human:        PENDING
    internal DebuggerStatement(SourceSpan span)
        : base(SyntaxKind.DebuggerStatement, span)
    {
    }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=CA8EA6
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into)
    {
    }
}

/// <summary>A function declaration (ECMA-262 clauses 15.2, 15.5, 15.6 and 15.8), an export default's anonymous one among them.</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=3EC4F0
// Broiler-Human:        PENDING
public sealed class FunctionDeclaration : Statement
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=E4E89E
    // Broiler-Human:        PENDING
    internal FunctionDeclaration(SourceSpan span, FunctionNode function)
        : base(SyntaxKind.FunctionDeclaration, span)
    {
        Function = function;
    }

    /// <summary>The function.</summary>
    public FunctionNode Function { get; }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=67BC92
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into) => into.Add(Function);
}

/// <summary>A ClassDeclaration (ECMA-262 clause 15.7), an export default's anonymous one among them.</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=0; Fingerprint=A130D7
// Broiler-Human:        PENDING
public sealed class ClassDeclaration : Statement
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=D7181A
    // Broiler-Human:        PENDING
    internal ClassDeclaration(SourceSpan span, ClassNode @class)
        : base(SyntaxKind.ClassDeclaration, span)
    {
        Class = @class;
    }

    /// <summary>The class.</summary>
    public ClassNode Class { get; }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s14 statements tree, JSD-0080 raw facts; IP=Low; Security=Low; Resources=1; Fingerprint=A5271E
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into) => into.Add(Class);
}
