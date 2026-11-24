// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// HAIRS Code Assurance
// --------------------
// Relevant units:   7
// Annotated:        7/7
// Exempt:           49
// Human-reviewed:   0/7
// IP risk:          Low
// Security risk:    Low
// Criteria:         0/0
// Resource impact:  1/10 max
// Unverified:       7
//
// GENERATED - DO NOT EDIT MANUALLY

using System.Collections.Generic;
using Broiler.ECMAScript.Syntax;

namespace Broiler.ECMAScript.Semantics;

/// <summary>
/// The kind of a scope the declarations walk records (Broiler.VM's JSD-0083, decision D10), and from SJL-15.2 the semantic
/// model (<see cref="SemanticModel"/>; JSD-0080, "The semantic model's design": "the scopes in source order ... program,
/// function, block, module, <c>with</c>, eval, class body, and the separate parameter scope of a function"). The members
/// up to <see cref="StaticBlock"/> are the tables' kinds; <see cref="With"/>, <see cref="ClassBody"/>,
/// <see cref="FunctionName"/> and <see cref="Parameters"/> are the model's alone. Eval code is not a kind of its own: its
/// program's scope is a <see cref="Script"/> scope and its facts' kind is <see cref="FunctionKind.EvalCode"/>.
/// </summary>
// Hairs-AI:           Origin=AI; Spec=JSD-0083 D10 side tables of the declarations walk, JSD-0080 semantic model scopes; IP=Low; Security=Low; Resources=0; Fingerprint=B01ED5
// Hairs-Human:        PENDING
public enum ScopeKind
{
    /// <summary>A Script's top level, or eval code's.</summary>
    Script = 0,

    /// <summary>A Module's top level.</summary>
    Module = 1,

    /// <summary>
    /// A function's body, an arrow function's among them; the dynamic function's goals too (ECMA-262 clause 10.2.11
    /// FunctionDeclarationInstantiation). The tables give a function this one scope, holding its body's declarations; the
    /// semantic model puts its parameters in a <see cref="Parameters"/> scope, this one's parent, and leaves this scope as
    /// the tables have it. <i>(Revised 2026-11-24, at SJL-15.2 stage 1; it read "A function's parameters and body, an arrow
    /// function's among them; the dynamic function's goals too.")</i>
    /// </summary>
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

    /// <summary>
    /// A <c>with</c> statement's object environment (ECMA-262 clause 14.11.2, the statement's evaluation:
    /// NewObjectEnvironment), keyed by the <see cref="WithStatement"/>: its body is inside it and its object outside it. It
    /// declares nothing. The semantic model's alone.
    /// </summary>
    With = 8,

    /// <summary>
    /// A class's classEnv (ECMA-262 clause 15.7.14 ClassDefinitionEvaluation, its NewDeclarativeEnvironment), keyed by its
    /// <see cref="ClassNode"/>, every class's, an anonymous class's among them: it holds the class's inner name where one is
    /// written (<see cref="BindingKind.ClassName"/>), and its heritage, its elements, their keys and their initialisers
    /// are inside it. The class's private environment is a later stage's. The semantic model's alone.
    /// </summary>
    ClassBody = 9,

    /// <summary>
    /// A named function expression's funcEnv (ECMA-262 clause 15.2.5 InstantiateOrdinaryFunctionExpression, and its generator,
    /// async generator and async forms in clauses 15.5, 15.6 and 15.8), keyed by its <see cref="FunctionNode"/>,
    /// holding the name (<see cref="BindingKind.FunctionName"/>). A function declaration, an anonymous function expression,
    /// an arrow function and a method have none. The semantic model's alone.
    /// </summary>
    FunctionName = 10,

    /// <summary>
    /// A function's parameters, the environment FunctionDeclarationInstantiation binds them in (ECMA-262 clause 10.2.11),
    /// keyed by every <see cref="FunctionNode"/>, an arrow function's and a function's without parameters
    /// among them, and by the <see cref="ProgramNode"/> for <see cref="ParseGoal.FormalParameters"/> and
    /// <see cref="ParseGoal.FunctionBody"/> (CreateDynamicFunction, clause 20.2.1.1.1): it holds the parameters'
    /// <see cref="BindingKind.Parameter"/> declarations and is the parent of the function's <see cref="Function"/> scope. A
    /// Script, a Module and eval code have none. The semantic model's alone.
    /// </summary>
    Parameters = 11,
}

/// <summary>
/// How a name is declared (ECMA-262's BoundNames of the declaring production). The members up to
/// <see cref="ExportDefault"/> are the tables' kinds; <see cref="Parameter"/>, <see cref="FunctionName"/> and
/// <see cref="ClassName"/> are the semantic model's alone, in its own scopes (JSD-0080, "The semantic model's design":
/// "each binding identifier's declaration, kind (var, let, const, class, function, parameter, catch, import) and
/// mutability").
/// </summary>
// Hairs-AI:           Origin=AI; Spec=JSD-0083 D10 side tables of the declarations walk, JSD-0080 semantic model declarations; IP=Low; Security=Low; Resources=0; Fingerprint=C8AC8D
// Hairs-Human:        PENDING
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

    /// <summary>
    /// A parameter: one of the BoundNames of a function's FormalParameters (ECMA-262 clause 10.2.11
    /// FunctionDeclarationInstantiation), in its <see cref="ScopeKind.Parameters"/> scope, a name bound twice
    /// declared twice. Its declaration's <see cref="Declaration.IsLexical"/>, <see cref="Declaration.IsBlockFunction"/> and
    /// <see cref="Declaration.IsIfClauseFunction"/> are false: a parameter is in neither LexicallyDeclaredNames nor
    /// VarDeclaredNames, and the kind says which binding it is.
    /// </summary>
    Parameter = 10,

    /// <summary>
    /// A named function expression's own name, which its funcEnv binds immutably (ECMA-262 clause 15.2.5
    /// InstantiateOrdinaryFunctionExpression, and its forms in clauses 15.5, 15.6 and 15.8), in its
    /// <see cref="ScopeKind.FunctionName"/> scope. Its declaration's three flags are false, as a parameter's are.
    /// </summary>
    FunctionName = 11,

    /// <summary>
    /// A class's inner name, which its classEnv binds immutably (ECMA-262 clause 15.7.14
    /// ClassDefinitionEvaluation), in its <see cref="ScopeKind.ClassBody"/> scope; a class declaration's outer binding stays
    /// <see cref="Class"/>, in the scope that declares it. Its declaration's three flags are false, as a parameter's are.
    /// </summary>
    ClassName = 12,
}

/// <summary>One declared name, as the declarations walk finds it (decision D10).</summary>
// Hairs-AI:           Origin=AI; Spec=JSD-0083 D10 side tables of the declarations walk, JSD-0080 semantic model mutability; IP=Low; Security=Low; Resources=1; Fingerprint=A1AF75
// Hairs-Human:        PENDING
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

    /// <summary>
    /// Whether the scope declares it lexically (LexicallyDeclaredNames), rather than var-scoped (VarDeclaredNames); false
    /// also for a <see cref="BindingKind.Parameter"/>, <see cref="BindingKind.FunctionName"/> or
    /// <see cref="BindingKind.ClassName"/> declaration, which is in neither. <i>(Revised 2026-11-24, at SJL-15.2 stage 1,
    /// when the semantic model's kinds were added; it read "Whether the scope declares it lexically
    /// (LexicallyDeclaredNames), rather than var-scoped (VarDeclaredNames).")</i>
    /// </summary>
    public bool IsLexical { get; }

    /// <summary>Whether it is a function declaration directly in a block, a <c>switch</c>'s CaseBlock or an <c>if</c> clause (Annex B.3.2's block-level function).</summary>
    public bool IsBlockFunction { get; }

    /// <summary>Whether it is a function declaration that is the whole of an <c>if</c> clause (Annex B.3.3), which the scope of an implicit block declares.</summary>
    public bool IsIfClauseFunction { get; }

    /// <summary>
    /// Whether its binding is mutable, by the operation that creates it: false for a binding created by
    /// CreateImmutableBinding or CreateImportBinding (ECMA-262 clauses 9.1.1.1.3 and 9.1.1.5.5) - a <c>const</c>, a
    /// <c>using</c> or an <c>await using</c> declaration (IsConstantDeclaration, clause 8.2.3; BlockDeclarationInstantiation,
    /// clause 14.2.3), an import's local name (a source text module record's InitializeEnvironment), a named function
    /// expression's own name (clause 15.2.5) and a class's inner name (clause 15.7.14); true for
    /// every other kind, which CreateMutableBinding creates. It follows from <see cref="Kind"/> alone.
    /// </summary>
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s9.1.1.1.3 CreateImmutableBinding, s8.2.3 IsConstantDeclaration, JSD-0080 semantic model mutability; IP=Low; Security=Low; Resources=1; Fingerprint=43F5A5
    // Hairs-Human:        PENDING
    public bool IsMutable => Kind is not (BindingKind.Const or BindingKind.Using or BindingKind.AwaitUsing or BindingKind.Import or BindingKind.FunctionName or BindingKind.ClassName);
}

/// <summary>One scope and its declarations, in source order (decision D10).</summary>
// Hairs-AI:           Origin=AI; Spec=JSD-0083 D10 side tables of the declarations walk; IP=Low; Security=Low; Resources=1; Fingerprint=21DD7E
// Hairs-Human:        PENDING
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
    /// a <see cref="SwitchStatement"/>, a <see cref="CatchClause"/>, a for statement, a <see cref="StaticBlock"/>, or the
    /// <see cref="FunctionDeclaration"/> that is the whole of an <c>if</c> clause (Annex B.3.3's implicit block); and in the
    /// semantic model's own scopes, a <see cref="WithStatement"/>, a <see cref="ClassNode"/>, the <see cref="FunctionNode"/>
    /// of a <see cref="ScopeKind.FunctionName"/> or <see cref="ScopeKind.Parameters"/> scope, or the
    /// <see cref="ProgramNode"/> of a dynamic function's <see cref="ScopeKind.Parameters"/> scope. <i>(Revised 2026-11-24,
    /// at SJL-15.2 stage 1, when the semantic model's kinds were added; it read "The node it belongs to: the ProgramNode, a
    /// FunctionNode, a BlockStatement, a SwitchStatement, a CatchClause, a for statement or a StaticBlock.")</i>
    /// </summary>
    public SyntaxNode Node { get; }

    /// <summary>
    /// The names it declares, in source order: its lexical declarations, and for a script, module, function or static block
    /// also its var-scoped ones, from every statement down to the next function boundary.
    /// </summary>
    public IReadOnlyList<Declaration> Declarations { get; }
}

/// <summary>
/// A program's or a function's facts the walk computes (decision D10; Broiler.VM's JSD-0080, "The semantic model's design",
/// the per-function facts): its kind, its strictness, whether it is async or a generator, its parameters' names, and
/// whether its parameter list is simple and whether it holds expressions. The tables hold the program's and every
/// function's; the semantic model's <see cref="SemanticModel.Functions"/> also holds one for every class static block and
/// for every field with an initialiser, which the tables do not. <i>(Revised 2026-11-24, at SJL-15.2 stage 1, when the
/// kind and the parameter facts were added; it read "A program's or a function's facts the walk computes (decision D10):
/// its strictness and its parameters' names.")</i>
/// </summary>
// Hairs-AI:           Origin=AI; Spec=JSD-0083 D10 side tables of the declarations walk, JSD-0080 semantic model per-function facts; IP=Low; Security=Low; Resources=1; Fingerprint=6CA3D3
// Hairs-Human:        PENDING
public sealed class FunctionFacts
{
    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D10 side tables of the declarations walk, JSD-0080 semantic model per-function facts; IP=Low; Security=Low; Resources=1; Fingerprint=E4B47F
    // Hairs-Human:        PENDING
    internal FunctionFacts(SyntaxNode node, bool isStrict, IReadOnlyList<string> parameterNames, FunctionKind kind, bool isAsync, bool isGenerator, bool hasSimpleParameterList, bool hasParameterExpressions)
    {
        Node = node;
        IsStrict = isStrict;
        ParameterNames = parameterNames;
        Kind = kind;
        IsAsync = isAsync;
        IsGenerator = isGenerator;
        HasSimpleParameterList = hasSimpleParameterList;
        HasParameterExpressions = hasParameterExpressions;
    }

    /// <summary>
    /// The <see cref="ProgramNode"/> or the <see cref="FunctionNode"/>; in the semantic model's own facts, the
    /// <see cref="StaticBlock"/> or the <see cref="FieldDefinition"/>. <i>(Revised 2026-11-24, at SJL-15.2 stage 1, when
    /// the model's own facts were added; it read "The ProgramNode or the FunctionNode.")</i>
    /// </summary>
    public SyntaxNode Node { get; }

    /// <summary>Whether its code is strict mode code (ECMA-262 clause 11.2.2).</summary>
    public bool IsStrict { get; }

    /// <summary>The BoundNames of its parameters, in source order, a name bound twice listed twice; empty for a program but the dynamic function's parameters goal.</summary>
    public IReadOnlyList<string> ParameterNames { get; }

    /// <summary>
    /// Its kind (ECMA-262: the function's [[ThisMode]], [[ConstructorKind]], [[HomeObject]] and the production it is made
    /// from, clauses 10.2 and 15; for a program, its goal, clauses 16.1, 16.2, 19.2.1.1 and 20.2.1.1.1). The same on every
    /// request, with the semantic model on or off.
    /// </summary>
    public FunctionKind Kind { get; }

    /// <summary>Whether it is an async function, an async arrow function, an async method or the dynamic function of an async kind (ECMA-262 clauses 15.8, 15.9, 15.6 and 20.2.1.1.1).</summary>
    public bool IsAsync { get; }

    /// <summary>Whether it is a generator, a generator method or the dynamic function of a generator kind (ECMA-262 clauses 15.5, 15.6 and 20.2.1.1.1).</summary>
    public bool IsGenerator { get; }

    /// <summary>
    /// IsSimpleParameterList of its FormalParameters (ECMA-262 clause 15.1.3): every parameter a bare BindingIdentifier,
    /// with no initialiser, no pattern and no rest element; true for a program, a static block and a field initialiser,
    /// which have no parameters, and for a dynamic function's body.
    /// </summary>
    public bool HasSimpleParameterList { get; }

    /// <summary>
    /// ContainsExpression of its FormalParameters (ECMA-262 clause 15.1.2): an initialiser anywhere in them, or a computed
    /// property name anywhere in a binding pattern of theirs (FunctionDeclarationInstantiation's hasParameterExpressions,
    /// clause 10.2.11); false where there are no parameters.
    /// </summary>
    public bool HasParameterExpressions { get; }
}

/// <summary>
/// The side tables of the syntax half's declarations walk (Broiler.VM's JSD-0083, decision D10), each ordered by source
/// position, so that neither engine computes them again: every scope with its kind and declarations, and the program's and
/// every function's strictness and parameter names.
/// </summary>
// Hairs-AI:           Origin=AI; Spec=JSD-0083 D10 side tables of the declarations walk, JSD-0080 semantic model entry; IP=Low; Security=Low; Resources=1; Fingerprint=6CE5F8
// Hairs-Human:        PENDING
public sealed class DeclarationTables
{
    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D10 side tables of the declarations walk, JSD-0080 semantic model entry; IP=Low; Security=Low; Resources=1; Fingerprint=181EA1
    // Hairs-Human:        PENDING
    internal DeclarationTables(IReadOnlyList<Scope> scopes, IReadOnlyList<FunctionFacts> functions, SemanticModel? model)
    {
        Scopes = scopes;
        Functions = functions;
        Model = model;
    }

    /// <summary>Every scope, in the source order of its start, the program's first.</summary>
    public IReadOnlyList<Scope> Scopes { get; }

    /// <summary>The program and every function, in the source order of its start, the program's first.</summary>
    public IReadOnlyList<FunctionFacts> Functions { get; }

    /// <summary>
    /// The semantic model the same walk built, when the request set <see cref="ParseRequest.BuildSemanticModel"/>; null
    /// otherwise. Published as <see cref="ParseResult.Model"/>, so that the result keeps its layout.
    /// </summary>
    internal SemanticModel? Model { get; }
}
