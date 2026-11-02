// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   6
// Annotated:        6/6
// Exempt:           36
// Human-reviewed:   0/6
// IP risk:          Low
// Security risk:    Low
// Criteria:         0/0
// Resource impact:  1/10 max
// Unverified:       6
//
// GENERATED - DO NOT EDIT MANUALLY

using System.Collections.Generic;
using Broiler.ECMAScript.Syntax;

namespace Broiler.ECMAScript.Semantics;

/// <summary>The kind of a scope the declarations walk records (Broiler.VM's JSD-0083, decision D10).</summary>
// Broiler-AI:           Origin=AI; Spec=JSD-0083 D10 side tables of the declarations walk; IP=Low; Security=Low; Resources=0; Fingerprint=77FAF4
// Broiler-Human:        PENDING
public enum ScopeKind
{
    /// <summary>A Script's top level, or eval code's.</summary>
    Script = 0,

    /// <summary>A Module's top level.</summary>
    Module = 1,

    /// <summary>A function's parameters and body, an arrow function's among them; the dynamic function's goals too.</summary>
    Function = 2,

    /// <summary>A Block statement.</summary>
    Block = 3,

    /// <summary>A <c>switch</c> statement's CaseBlock.</summary>
    Switch = 4,

    /// <summary>A <c>catch</c> clause's parameter.</summary>
    Catch = 5,

    /// <summary>The head of a <c>for</c>, <c>for</c>-<c>in</c> or <c>for</c>-<c>of</c> statement that declares lexically.</summary>
    For = 6,

    /// <summary>A class static block.</summary>
    StaticBlock = 7,
}

/// <summary>How a name is declared (ECMA-262's BoundNames of the declaring production).</summary>
// Broiler-AI:           Origin=AI; Spec=JSD-0083 D10 side tables of the declarations walk; IP=Low; Security=Low; Resources=0; Fingerprint=675A8D
// Broiler-Human:        PENDING
public enum BindingKind
{
    /// <summary>A <c>var</c> declaration, its for heads' among them.</summary>
    Var = 0,

    /// <summary>A <c>let</c> declaration.</summary>
    Let = 1,

    /// <summary>A <c>const</c> declaration.</summary>
    Const = 2,

    /// <summary>A <c>using</c> declaration.</summary>
    Using = 3,

    /// <summary>An <c>await using</c> declaration.</summary>
    AwaitUsing = 4,

    /// <summary>A function declaration of any kind: plain, generator, async or async generator.</summary>
    Function = 5,

    /// <summary>A class declaration.</summary>
    Class = 6,

    /// <summary>An import's local name.</summary>
    Import = 7,

    /// <summary>A <c>catch</c> clause's parameter.</summary>
    CatchParameter = 8,

    /// <summary>The name <c>*default*</c> an <c>export default</c> of an expression or of an anonymous declaration binds.</summary>
    ExportDefault = 9,
}

/// <summary>One declared name, as the declarations walk finds it (decision D10).</summary>
// Broiler-AI:           Origin=AI; Spec=JSD-0083 D10 side tables of the declarations walk; IP=Low; Security=Low; Resources=1; Fingerprint=A1AF75
// Broiler-Human:        PENDING
public sealed class Declaration
{
    internal Declaration(string name, BindingKind kind, SyntaxNode node, bool isLexical, bool isBlockFunction, bool isIfClauseFunction)
    {
        Name = name;
        Kind = kind;
        Node = node;
        IsLexical = isLexical;
        IsBlockFunction = isBlockFunction;
        IsIfClauseFunction = isIfClauseFunction;
    }

    /// <summary>The name's StringValue.</summary>
    public string Name { get; }

    /// <summary>How it is declared.</summary>
    public BindingKind Kind { get; }

    /// <summary>The node that binds it: a <see cref="BindingIdentifier"/>, or the declaration of a name no identifier spells (<c>*default*</c>).</summary>
    public SyntaxNode Node { get; }

    /// <summary>Whether the scope declares it lexically (LexicallyDeclaredNames), rather than var-scoped (VarDeclaredNames).</summary>
    public bool IsLexical { get; }

    /// <summary>Whether it is a function declaration directly in a block, a <c>switch</c>'s CaseBlock or an <c>if</c> clause (Annex B.3.2's block-level function).</summary>
    public bool IsBlockFunction { get; }

    /// <summary>Whether it is a function declaration that is the whole of an <c>if</c> clause (Annex B.3.3), which the scope of an implicit block declares.</summary>
    public bool IsIfClauseFunction { get; }
}

/// <summary>One scope and its declarations, in source order (decision D10).</summary>
// Broiler-AI:           Origin=AI; Spec=JSD-0083 D10 side tables of the declarations walk; IP=Low; Security=Low; Resources=1; Fingerprint=21DD7E
// Broiler-Human:        PENDING
public sealed class Scope
{
    internal Scope(ScopeKind kind, SyntaxNode node, IReadOnlyList<Declaration> declarations)
    {
        Kind = kind;
        Node = node;
        Declarations = declarations;
    }

    /// <summary>Its kind.</summary>
    public ScopeKind Kind { get; }

    /// <summary>
    /// The node it belongs to: the <see cref="ProgramNode"/>, a <see cref="FunctionNode"/>, a <see cref="BlockStatement"/>,
    /// a <see cref="SwitchStatement"/>, a <see cref="CatchClause"/>, a for statement or a <see cref="StaticBlock"/>.
    /// </summary>
    public SyntaxNode Node { get; }

    /// <summary>
    /// The names it declares, in source order: its lexical declarations, and for a script, module, function or static block
    /// also its var-scoped ones, from every statement down to the next function boundary.
    /// </summary>
    public IReadOnlyList<Declaration> Declarations { get; }
}

/// <summary>A program's or a function's facts the walk computes (decision D10): its strictness and its parameters' names.</summary>
// Broiler-AI:           Origin=AI; Spec=JSD-0083 D10 side tables of the declarations walk; IP=Low; Security=Low; Resources=1; Fingerprint=6CA3D3
// Broiler-Human:        PENDING
public sealed class FunctionFacts
{
    internal FunctionFacts(SyntaxNode node, bool isStrict, IReadOnlyList<string> parameterNames)
    {
        Node = node;
        IsStrict = isStrict;
        ParameterNames = parameterNames;
    }

    /// <summary>The <see cref="ProgramNode"/> or the <see cref="FunctionNode"/>.</summary>
    public SyntaxNode Node { get; }

    /// <summary>Whether its code is strict mode code (ECMA-262 clause 11.2.2).</summary>
    public bool IsStrict { get; }

    /// <summary>The BoundNames of its parameters, in source order, a name bound twice listed twice; empty for a program but the dynamic function's parameters goal.</summary>
    public IReadOnlyList<string> ParameterNames { get; }
}

/// <summary>
/// The side tables of the syntax half's declarations walk (Broiler.VM's JSD-0083, decision D10), each ordered by source
/// position, so that neither engine computes them again: every scope with its kind and declarations, and the program's and
/// every function's strictness and parameter names.
/// </summary>
// Broiler-AI:           Origin=AI; Spec=JSD-0083 D10 side tables of the declarations walk; IP=Low; Security=Low; Resources=1; Fingerprint=6CE5F8
// Broiler-Human:        PENDING
public sealed class DeclarationTables
{
    internal DeclarationTables(IReadOnlyList<Scope> scopes, IReadOnlyList<FunctionFacts> functions)
    {
        Scopes = scopes;
        Functions = functions;
    }

    /// <summary>Every scope, in the source order of its start, the program's first.</summary>
    public IReadOnlyList<Scope> Scopes { get; }

    /// <summary>The program and every function, in the source order of its start, the program's first.</summary>
    public IReadOnlyList<FunctionFacts> Functions { get; }
}
