// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   11
// Annotated:        11/11
// Exempt:           92
// Human-reviewed:   0/11
// IP risk:          Low
// Security risk:    Low
// Criteria:         0/0
// Resource impact:  1/10 max
// Unverified:       11
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Broiler.ECMAScript.Syntax;

/// <summary>
/// What a node of the tree is (ECMA-262 clauses 13 to 16, one kind per production the tree keeps), so that a consumer can
/// switch on it without a type test.
/// </summary>
/// <remarks>The numbers are stable: a kind is never renumbered and a number is never reused.</remarks>
// Broiler-AI:           Origin=AI; Spec=JSD-0080 output tree, JSD-0083 D15 full spans; IP=Low; Security=Low; Resources=0; Fingerprint=89DBEB
// Broiler-Human:        PENDING
public enum SyntaxKind
{
    /// <summary>A Script or a Module, eval code, or a dynamic function's parameters or body (<see cref="ProgramNode"/>).</summary>
    Program = 0,

    /// <summary><see cref="Syntax.Identifier"/>: an IdentifierReference (clause 13.1).</summary>
    Identifier,

    /// <summary><see cref="Syntax.BindingIdentifier"/>: a BindingIdentifier (clause 13.1).</summary>
    BindingIdentifier,

    /// <summary><see cref="Syntax.IdentifierName"/>: a property, label, meta-property or module export name (clause 12.7).</summary>
    IdentifierName,

    /// <summary><see cref="Syntax.PrivateIdentifier"/> (clause 12.7).</summary>
    PrivateIdentifier,

    /// <summary><see cref="Syntax.ThisExpression"/> (clause 13.2.1).</summary>
    ThisExpression,

    /// <summary><see cref="Syntax.SuperExpression"/>: <c>super</c> as a SuperProperty's object or a SuperCall's callee (clause 13.3.7).</summary>
    SuperExpression,

    /// <summary><see cref="Syntax.NullLiteral"/> (clause 13.2.3).</summary>
    NullLiteral,

    /// <summary><see cref="Syntax.BooleanLiteral"/> (clause 13.2.3).</summary>
    BooleanLiteral,

    /// <summary><see cref="Syntax.NumericLiteral"/> (clause 12.9.3).</summary>
    NumericLiteral,

    /// <summary><see cref="Syntax.StringLiteral"/> (clause 12.9.4).</summary>
    StringLiteral,

    /// <summary><see cref="Syntax.RegularExpressionLiteral"/> (clause 12.9.5).</summary>
    RegularExpressionLiteral,

    /// <summary><see cref="Syntax.TemplateLiteral"/> (clause 13.2.8).</summary>
    TemplateLiteral,

    /// <summary><see cref="Syntax.TemplateElement"/>: one piece of a template's text (clause 12.9.6).</summary>
    TemplateElement,

    /// <summary><see cref="Syntax.TaggedTemplateExpression"/> (clause 13.3.11).</summary>
    TaggedTemplateExpression,

    /// <summary><see cref="Syntax.ArrayLiteral"/> (clause 13.2.4).</summary>
    ArrayLiteral,

    /// <summary><see cref="Syntax.ObjectLiteral"/> (clause 13.2.5).</summary>
    ObjectLiteral,

    /// <summary><see cref="Syntax.PropertyDefinition"/> (clause 13.2.5).</summary>
    PropertyDefinition,

    /// <summary><see cref="Syntax.ComputedPropertyName"/> (clause 13.2.5).</summary>
    ComputedPropertyName,

    /// <summary><see cref="Syntax.SpreadElement"/> (clauses 13.2.4, 13.2.5 and 13.3.8).</summary>
    SpreadElement,

    /// <summary><see cref="Syntax.ParenthesizedExpression"/> (clause 13.2.10).</summary>
    ParenthesizedExpression,

    /// <summary><see cref="Syntax.FunctionExpression"/> (clauses 15.2 to 15.6).</summary>
    FunctionExpression,

    /// <summary><see cref="Syntax.ArrowFunctionExpression"/> (clauses 15.3 and 15.9).</summary>
    ArrowFunctionExpression,

    /// <summary><see cref="Syntax.ClassExpression"/> (clause 15.7).</summary>
    ClassExpression,

    /// <summary><see cref="Syntax.MemberExpression"/> (clause 13.3.2).</summary>
    MemberExpression,

    /// <summary><see cref="Syntax.CallExpression"/> (clause 13.3.6).</summary>
    CallExpression,

    /// <summary><see cref="Syntax.ChainExpression"/>: an OptionalExpression (clause 13.3.9).</summary>
    ChainExpression,

    /// <summary><see cref="Syntax.NewExpression"/> (clause 13.3.5).</summary>
    NewExpression,

    /// <summary><see cref="Syntax.MetaProperty"/>: <c>new.target</c> or <c>import.meta</c> (clause 13.3.12).</summary>
    MetaProperty,

    /// <summary><see cref="Syntax.ImportCall"/> (clause 13.3.10).</summary>
    ImportCall,

    /// <summary><see cref="Syntax.UpdateExpression"/> (clause 13.4).</summary>
    UpdateExpression,

    /// <summary><see cref="Syntax.UnaryExpression"/> (clause 13.5).</summary>
    UnaryExpression,

    /// <summary><see cref="Syntax.AwaitExpression"/> (clause 15.8).</summary>
    AwaitExpression,

    /// <summary><see cref="Syntax.BinaryExpression"/> (clauses 13.6 to 13.12).</summary>
    BinaryExpression,

    /// <summary><see cref="Syntax.LogicalExpression"/> (clause 13.13).</summary>
    LogicalExpression,

    /// <summary><see cref="Syntax.PrivateInExpression"/>: <c>#x in o</c> (clause 13.10).</summary>
    PrivateInExpression,

    /// <summary><see cref="Syntax.ConditionalExpression"/> (clause 13.14).</summary>
    ConditionalExpression,

    /// <summary><see cref="Syntax.AssignmentExpression"/> (clause 13.15).</summary>
    AssignmentExpression,

    /// <summary><see cref="Syntax.SequenceExpression"/>: the comma operator (clause 13.16).</summary>
    SequenceExpression,

    /// <summary><see cref="Syntax.YieldExpression"/> (clause 15.5).</summary>
    YieldExpression,

    /// <summary><see cref="Syntax.Decorator"/>, under the decorators proposal's flag (decision D17).</summary>
    Decorator,

    /// <summary><see cref="Syntax.ArrayPattern"/> (clauses 13.15.5 and 14.3.3).</summary>
    ArrayPattern,

    /// <summary><see cref="Syntax.ObjectPattern"/> (clauses 13.15.5 and 14.3.3).</summary>
    ObjectPattern,

    /// <summary><see cref="Syntax.PatternProperty"/> (clauses 13.15.5 and 14.3.3).</summary>
    PatternProperty,

    /// <summary><see cref="Syntax.AssignmentPattern"/>: a target with its initializer (clauses 13.15.5 and 14.3.3).</summary>
    AssignmentPattern,

    /// <summary><see cref="Syntax.RestElement"/> (clauses 13.15.5, 14.3.3 and 15.1).</summary>
    RestElement,

    /// <summary><see cref="Syntax.TargetPattern"/>: an assignment pattern's DestructuringAssignmentTarget that is an expression (clause 13.15.5).</summary>
    TargetPattern,

    /// <summary><see cref="Syntax.FunctionNode"/>: a function's parameters and body (clause 15.1 to 15.9).</summary>
    Function,

    /// <summary><see cref="Syntax.ClassNode"/>: a class's heritage and body (clause 15.7).</summary>
    Class,

    /// <summary><see cref="Syntax.MethodDefinition"/> (clauses 15.4 and 15.7).</summary>
    MethodDefinition,

    /// <summary><see cref="Syntax.FieldDefinition"/> (clause 15.7).</summary>
    FieldDefinition,

    /// <summary><see cref="Syntax.StaticBlock"/> (clause 15.7).</summary>
    StaticBlock,

    /// <summary><see cref="Syntax.BlockStatement"/> (clause 14.2).</summary>
    BlockStatement,

    /// <summary><see cref="Syntax.VariableDeclaration"/> (clauses 14.3.1, 14.3.2 and explicit resource management).</summary>
    VariableDeclaration,

    /// <summary><see cref="Syntax.VariableDeclarator"/> (clauses 14.3.1 and 14.3.2).</summary>
    VariableDeclarator,

    /// <summary><see cref="Syntax.EmptyStatement"/> (clause 14.4).</summary>
    EmptyStatement,

    /// <summary><see cref="Syntax.ExpressionStatement"/> (clause 14.5), a directive among them (clause 11.2.1).</summary>
    ExpressionStatement,

    /// <summary><see cref="Syntax.IfStatement"/> (clause 14.6).</summary>
    IfStatement,

    /// <summary><see cref="Syntax.DoWhileStatement"/> (clause 14.7.2).</summary>
    DoWhileStatement,

    /// <summary><see cref="Syntax.WhileStatement"/> (clause 14.7.3).</summary>
    WhileStatement,

    /// <summary><see cref="Syntax.ForStatement"/> (clause 14.7.4).</summary>
    ForStatement,

    /// <summary><see cref="Syntax.ForInStatement"/> (clause 14.7.5).</summary>
    ForInStatement,

    /// <summary><see cref="Syntax.ForOfStatement"/> (clause 14.7.5).</summary>
    ForOfStatement,

    /// <summary><see cref="Syntax.ContinueStatement"/> (clause 14.8).</summary>
    ContinueStatement,

    /// <summary><see cref="Syntax.BreakStatement"/> (clause 14.9).</summary>
    BreakStatement,

    /// <summary><see cref="Syntax.ReturnStatement"/> (clause 14.10).</summary>
    ReturnStatement,

    /// <summary><see cref="Syntax.WithStatement"/> (clause 14.11).</summary>
    WithStatement,

    /// <summary><see cref="Syntax.SwitchStatement"/> (clause 14.12).</summary>
    SwitchStatement,

    /// <summary><see cref="Syntax.SwitchCase"/> (clause 14.12).</summary>
    SwitchCase,

    /// <summary><see cref="Syntax.LabelledStatement"/> (clause 14.13).</summary>
    LabelledStatement,

    /// <summary><see cref="Syntax.ThrowStatement"/> (clause 14.14).</summary>
    ThrowStatement,

    /// <summary><see cref="Syntax.TryStatement"/> (clause 14.15).</summary>
    TryStatement,

    /// <summary><see cref="Syntax.CatchClause"/> (clause 14.15).</summary>
    CatchClause,

    /// <summary><see cref="Syntax.DebuggerStatement"/> (clause 14.16).</summary>
    DebuggerStatement,

    /// <summary><see cref="Syntax.FunctionDeclaration"/> (clauses 15.2 to 15.6).</summary>
    FunctionDeclaration,

    /// <summary><see cref="Syntax.ClassDeclaration"/> (clause 15.7).</summary>
    ClassDeclaration,

    /// <summary><see cref="Syntax.ImportDeclaration"/> (clause 16.2.2).</summary>
    ImportDeclaration,

    /// <summary><see cref="Syntax.ImportSpecifier"/> (clause 16.2.2).</summary>
    ImportSpecifier,

    /// <summary><see cref="Syntax.ImportAttribute"/> (clause 16.2.2).</summary>
    ImportAttribute,

    /// <summary><see cref="Syntax.ExportNamedDeclaration"/> (clause 16.2.3).</summary>
    ExportNamedDeclaration,

    /// <summary><see cref="Syntax.ExportDefaultDeclaration"/> (clause 16.2.3).</summary>
    ExportDefaultDeclaration,

    /// <summary><see cref="Syntax.ExportAllDeclaration"/> (clause 16.2.3).</summary>
    ExportAllDeclaration,

    /// <summary><see cref="Syntax.ExportSpecifier"/> (clause 16.2.3).</summary>
    ExportSpecifier,
}

/// <summary>
/// The base of every node of the tree (Broiler.VM's JSD-0080, the output's "Tree": immutable nodes with full spans and raw
/// facts, no desugaring; JSD-0083, decision D15).
/// </summary>
/// <remarks>
/// <para>
/// <b>Full spans</b>: <see cref="Span"/> runs from the first code unit of the node's first token to just past its last
/// token's, in UTF-16 code units, with the source's base offset added to its offset and its base line and column applied
/// to its line and column (decision D15), as the diagnostics' spans are.
/// </para>
/// <para>
/// <b>Immutable</b>: a node is built once by the parse and never changed; its lists are read-only. The library alone makes
/// nodes. A node is its own identity: side facts sit in identity-keyed tables beside the tree (stage 4d), never on it.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=JSD-0080 output tree, JSD-0083 D15 full spans; IP=Low; Security=Low; Resources=0; Fingerprint=9FC754
// Broiler-Human:        PENDING
public abstract class SyntaxNode
{
    private protected SyntaxNode(SyntaxKind kind, SourceSpan span)
    {
        Kind = kind;
        Span = span;
    }

    /// <summary>What the node is.</summary>
    public SyntaxKind Kind { get; }

    /// <summary>Where the node stands in the source: its full span (decision D15).</summary>
    public SourceSpan Span { get; }

    /// <summary>
    /// The node's height in tree depth (decision D14, as the owner decided on 2026-11-02): the most nodes of binary chains
    /// and of member or call chains on any path from this node down to a leaf, through every child it has; a parenthesis, a
    /// template, a substitution and every other node add none. The parser sets it for each chain node it charges, and
    /// computes it once for any other node when a chain is charged with it; -1 until then. <i>(Revised 2026-11-02, at
    /// stage SJL-13.2f; it was <c>Expression.Spine</c>, "The tree depth the expression's left spine is charged (decision
    /// D14): one for each node a binary chain or a member or call chain adds, none for a parenthesis, a template or a
    /// substitution. A chain that begins with this expression is charged from it, which closes the left-operand gap.")</i>
    /// </summary>
    internal int Height { get; set; } = -1;

    /// <summary>The node's children in source order, for a walk that needs no type test.</summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0080 output tree, JSD-0083 D15 full spans; IP=Low; Security=Low; Resources=1; Fingerprint=9EBE30
    // Broiler-Human:        PENDING
    internal abstract void AddChildren(List<SyntaxNode> into);

    // Broiler-AI:           Origin=AI; Spec=JSD-0080 output tree, JSD-0083 D15 full spans; IP=Low; Security=Low; Resources=1; Fingerprint=AF0CDC
    // Broiler-Human:        PENDING
    private protected static void Add(List<SyntaxNode> into, SyntaxNode? node)
    {
        if (node is not null)
            into.Add(node);
    }

    // Broiler-AI:           Origin=AI; Spec=JSD-0080 output tree, JSD-0083 D15 full spans; IP=Low; Security=Low; Resources=1; Fingerprint=86046F
    // Broiler-Human:        PENDING
    private protected static void AddAll<T>(List<SyntaxNode> into, IReadOnlyList<T> nodes)
        where T : SyntaxNode?
    {
        foreach (var node in nodes)
        {
            if (node is not null)
                into.Add(node);
        }
    }
}

/// <summary>Read-only lists for the tree.</summary>
// Broiler-AI:           Origin=AI; Spec=JSD-0080 output tree, JSD-0083 D15 full spans; IP=Low; Security=Low; Resources=0; Fingerprint=65D27B
// Broiler-Human:        PENDING
internal static class Lists
{
    // Broiler-AI:           Origin=AI; Spec=JSD-0080 output tree, JSD-0083 D15 full spans; IP=Low; Security=Low; Resources=1; Fingerprint=422F06
    // Broiler-Human:        PENDING
    internal static IReadOnlyList<T> Freeze<T>(List<T>? items) =>
        items is null || items.Count == 0 ? Array.Empty<T>() : new ReadOnlyCollection<T>(items.ToArray());

    // Broiler-AI:           Origin=AI; Spec=JSD-0080 output tree, JSD-0083 D15 full spans; IP=Low; Security=Low; Resources=1; Fingerprint=0FD37C
    // Broiler-Human:        PENDING
    internal static IReadOnlyList<T> One<T>(T item) => new ReadOnlyCollection<T>(new[] { item });
}

/// <summary>
/// The root: a Script (clause 16.1), a Module (clause 16.2), eval code (clause 19.2.1.1), or a dynamic function's
/// FormalParameters or body (clause 20.2.1.1.1), as the request's goal says.
/// </summary>
/// <remarks>
/// The directive prologue (clause 11.2.1) is the leading run of <see cref="Body"/>'s expression statements whose
/// <see cref="ExpressionStatement.Directive"/> is set, its raw text kept as written. A dynamic function's parameters are in
/// <see cref="Parameters"/> and its <see cref="Body"/> is empty; every other goal's are in <see cref="Body"/>.
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=JSD-0080 output tree, JSD-0083 D15 full spans; IP=Low; Security=Low; Resources=0; Fingerprint=5441C5
// Broiler-Human:        PENDING
public sealed class ProgramNode : SyntaxNode
{
    // Broiler-AI:           Origin=AI; Spec=JSD-0080 output tree, JSD-0083 D15 full spans; IP=Low; Security=Low; Resources=1; Fingerprint=85E41B
    // Broiler-Human:        PENDING
    internal ProgramNode(SourceSpan span, ParseGoal goal, IReadOnlyList<Statement> body, IReadOnlyList<Pattern> parameters, bool parametersTrailingComma, bool hasTopLevelAwait)
        : base(SyntaxKind.Program, span)
    {
        Goal = goal;
        Body = body;
        Parameters = parameters;
        ParametersTrailingComma = parametersTrailingComma;
        HasTopLevelAwait = hasTopLevelAwait;
    }

    /// <summary>The goal the source was parsed under (ECMA-262 clause 5.1.4).</summary>
    public ParseGoal Goal { get; }

    /// <summary>The statement list: ScriptBody, ModuleItemList, eval code's ScriptBody or a dynamic function's body.</summary>
    public IReadOnlyList<Statement> Body { get; }

    /// <summary>For <see cref="ParseGoal.FormalParameters"/>, the FormalParameters (clause 15.1), a <see cref="RestElement"/> last where one is written; empty otherwise.</summary>
    public IReadOnlyList<Pattern> Parameters { get; }

    /// <summary>For <see cref="ParseGoal.FormalParameters"/>, whether a comma closes the list (clause 15.1, <c>FormalParameterList ,</c>).</summary>
    public bool ParametersTrailingComma { get; }

    /// <summary>
    /// Whether the top level awaits, as ParseModule's [[HasTLA]] reads it (clause 16.2.1.7): an <c>await</c> expression, a
    /// <c>for await</c> or an <c>await using</c> declaration outside every function, method, arrow function, class field
    /// initializer and static block. Only a request with top-level <c>await</c> on can set it (decision D10).
    /// </summary>
    public bool HasTopLevelAwait { get; }

    // Broiler-AI:           Origin=AI; Spec=JSD-0080 output tree, JSD-0083 D15 full spans; IP=Low; Security=Low; Resources=1; Fingerprint=3F48E1
    // Broiler-Human:        PENDING
    internal override void AddChildren(List<SyntaxNode> into)
    {
        AddAll(into, Parameters);
        AddAll(into, Body);
    }
}
