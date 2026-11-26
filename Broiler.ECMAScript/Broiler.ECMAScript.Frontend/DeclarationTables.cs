// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// HAIRS Code Assurance
// --------------------
// Relevant units:   8
// Annotated:        8/8
// Exempt:           51
// Human-reviewed:   0/8
// IP risk:          Low
// Security risk:    Low
// Criteria:         0/0
// Resource impact:  1/10 max
// Unverified:       8
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
    /// <summary>
    /// A Script's top level, or eval code's. A Script's declarations are its own, its lexical ones in the global
    /// environment's declarative record and the others in its object record (ECMA-262
    /// GlobalDeclarationInstantiation); sloppy eval code's scope holds its own LexicalEnvironment's declarations, its
    /// var-scoped ones going to the caller's VariableEnvironment (PerformEval, clause 19.2.1.1, steps 16.a and 16.b), and
    /// strict eval code's is one environment, its VariableEnvironment its LexicalEnvironment (step 18). <i>(Revised
    /// 2026-11-26, at SJL-15.2 stage 5a, when resolution joined the semantic model; it read "A Script's top level, or eval
    /// code's.")</i>
    /// </summary>
    Script = 0,

    /// <summary>A Module's top level.</summary>
    Module = 1,

    /// <summary>
    /// A function's body, an arrow function's among them; the dynamic function's goals too (ECMA-262 clause 10.2.11
    /// FunctionDeclarationInstantiation). The tables give a function this one scope, holding its body's declarations; the
    /// semantic model puts its parameters in a <see cref="Parameters"/> scope, this one's parent, and leaves this scope as
    /// the tables have it. <i>(Revised 2026-11-24, at SJL-15.2 stage 1; it read "A function's parameters and body, an arrow
    /// function's among them; the dynamic function's goals too.")</i> A sloppy function's one scope stands for its
    /// LexicalEnvironment over its VariableEnvironment (FunctionDeclarationInstantiation step 32.b): its
    /// <see cref="Declaration.IsLexical"/> declarations are the LexicalEnvironment's and the others the
    /// VariableEnvironment's, and where <see cref="FunctionFacts.HasSeparateVariableEnvironment"/> is false the
    /// VariableEnvironment is also its <see cref="Parameters"/> scope's environment (step 29.d); a strict function's scope is
    /// one environment (step 31.a). For resolution the split cannot be observed: a reference meets the LexicalEnvironment
    /// first, and a direct <c>eval</c>'s <c>var</c> beside one of its names is EvalDeclarationInstantiation's SyntaxError.
    /// <i>(Added 2026-11-26, at SJL-15.2 stage 5a.)</i>
    /// For resolution, a sloppy function's scope also binds the name of each Annex B alias of its block functions
    /// (<see cref="SemanticModel.AnnexBAlias"/>, a <see cref="BindingKind.BlockFunctionAlias"/> declaration), which no
    /// scope's <see cref="Scope.Declarations"/> holds. <i>(Added 2026-11-26, at SJL-15.2 stage 5b.)</i>
    /// The split is what <see cref="SemanticModel.CallerScopesOf"/> reads for a direct <c>eval</c> in a sloppy function's
    /// body: the record between the call and the VariableEnvironment holds the <see cref="Declaration.IsLexical"/>
    /// declarations alone. <i>(Added 2026-11-26, at SJL-15.2 stage 5c.)</i>
    /// </summary>
    Function = 2,

    /// <summary>A Block statement.</summary>
    Block = 3,

    /// <summary>A <c>switch</c> statement's CaseBlock.</summary>
    Switch = 4,

    /// <summary>
    /// A <c>catch</c> clause's parameter. A <c>catch</c> without a parameter keeps the tables' Catch scope too, with no
    /// declaration, where ECMA-262 clause 14.15.2 (<c>Catch : catch Block</c>) creates no environment; the semantic model
    /// keeps it as the tables have it, and its <see cref="SemanticModel.ParentOf"/> is then the parent of the clause's
    /// block. <i>(Added 2026-11-24, at SJL-15.2 stage 1.)</i>
    /// A Catch scope with a parameter stands for the record EvalDeclarationInstantiation calls "the Environment Record for a
    /// |Catch| clause", whatever the parameter's form, which <see cref="SemanticModel.CallerScopesOf"/> marks
    /// <see cref="CallerScope.IsCatch"/>. <i>(Added 2026-11-26, at SJL-15.2 stage 5c.)</i>
    /// </summary>
    Catch = 5,

    /// <summary>The head of a <c>for</c>, <c>for</c>-<c>in</c> or <c>for</c>-<c>of</c> statement that declares lexically.</summary>
    For = 6,

    /// <summary>
    /// A class static block, whose body FunctionDeclarationInstantiation instantiates with no parameters (ECMA-262 clause
    /// 15.7.11): in the semantic model it also binds the block's <c>arguments</c>, its
    /// <see cref="EnvironmentFacts.Arguments"/>, which no scope's <see cref="Scope.Declarations"/> holds. <i>(Revised
    /// 2026-11-26, at SJL-15.2 stage 5a; it read "A class static block.")</i>
    /// </summary>
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
    /// Script, a Module and eval code have none. The semantic model's alone. It also binds the function's
    /// <c>arguments</c> where FunctionDeclarationInstantiation creates it (step 22), the function's
    /// <see cref="EnvironmentFacts.Arguments"/>, which no scope's <see cref="Scope.Declarations"/> holds. <i>(Revised
    /// 2026-11-26, at SJL-15.2 stage 5a; it read "and is the parent of the function's Function scope. A Script, a Module and
    /// eval code have none. The semantic model's alone.")</i>
    /// </summary>
    Parameters = 11,
}

/// <summary>
/// How a name is declared (ECMA-262's BoundNames of the declaring production). The members up to
/// <see cref="ExportDefault"/> are the tables' kinds; <see cref="Parameter"/>, <see cref="FunctionName"/> and
/// <see cref="ClassName"/> are the semantic model's alone, in its own scopes (JSD-0080, "The semantic model's design":
/// "each binding identifier's declaration, kind (var, let, const, class, function, parameter, catch, import) and
/// mutability"); <see cref="Arguments"/> is the semantic model's alone too, and in no scope's
/// <see cref="Scope.Declarations"/>. <i>(Revised 2026-11-26, at SJL-15.2 stage 5a, when <c>Arguments</c> was added; it
/// read "are the semantic model's alone, in its own scopes".)</i>
/// <see cref="BlockFunctionAlias"/> is the semantic model's alone too, and in no scope's <see cref="Scope.Declarations"/>.
/// <i>(Added 2026-11-26, at SJL-15.2 stage 5b.)</i>
/// </summary>
// Hairs-AI:           Origin=AI; Spec=JSD-0083 D10 side tables of the declarations walk, JSD-0080 semantic model declarations; IP=Low; Security=Low; Resources=0; Fingerprint=FBEE46
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

    /// <summary>
    /// The <c>arguments</c> binding FunctionDeclarationInstantiation creates where its argumentsObjectNeeded is true (ECMA-262
    /// clause 10.2.11, steps 15 to 18 and 22), in the environment of the function's <see cref="ScopeKind.Parameters"/> scope
    /// or the static block's <see cref="ScopeKind.StaticBlock"/> scope: the semantic model's alone, held as
    /// <see cref="EnvironmentFacts.Arguments"/> and in no scope's <see cref="Scope.Declarations"/>. Its declaration's
    /// <see cref="Declaration.Node"/> is the function's, the static block's or the dynamic function's program, and its three
    /// flags are false, as a parameter's are. <i>(Added 2026-11-26, at SJL-15.2 stage 5a.)</i>
    /// </summary>
    Arguments = 13,

    /// <summary>
    /// The var-scoped binding Annex B.3.2 gives a block function (ECMA-262 Annex B.3.2, the web legacy compatibility
    /// semantics of block-level function declarations: FunctionDeclarationInstantiation step 32.a,
    /// GlobalDeclarationInstantiation step 12 and EvalDeclarationInstantiation step 13), where no <c>var</c>, top-level
    /// function or <c>arguments</c> binding of the name already stands in the variable scope:
    /// the semantic model's alone, answered by <see cref="SemanticModel.AnnexBAlias"/>, one per variable scope and name, and
    /// in no scope's <see cref="Scope.Declarations"/>. Its declaration's <see cref="Declaration.Node"/> is the
    /// <see cref="BindingIdentifier"/> of the admitted block function of that name that starts first in the source, for
    /// which <see cref="SemanticModel.DeclarationOf"/> answers the block function's own declaration, and its three flags
    /// are false, as a parameter's are. Row AB1's name, "Annex B.3.3 block-function aliases", is an earlier edition's
    /// number for the same feature. <i>(Added 2026-11-26, at SJL-15.2 stage 5b.)</i>
    /// </summary>
    BlockFunctionAlias = 14,
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

    /// <summary>
    /// The node that binds it: a <see cref="BindingIdentifier"/>, or the declaration of a name no identifier spells
    /// (<c>*default*</c>); for a <see cref="BindingKind.Arguments"/> declaration, the <see cref="FunctionNode"/>, the
    /// <see cref="StaticBlock"/> or the dynamic function's <see cref="ProgramNode"/> whose <c>arguments</c> it is, for which
    /// <see cref="SemanticModel.DeclarationOf"/> answers null. <i>(Revised 2026-11-26, at SJL-15.2 stage 5a; it read "The
    /// node that binds it: a BindingIdentifier, or the declaration of a name no identifier spells (*default*).")</i>
    /// For a <see cref="BindingKind.BlockFunctionAlias"/> declaration, the <see cref="BindingIdentifier"/> of the admitted
    /// block function of its name and variable scope that starts first in the source, for which
    /// <see cref="SemanticModel.DeclarationOf"/> answers that block function's own declaration. <i>(Added 2026-11-26, at
    /// SJL-15.2 stage 5b.)</i>
    /// </summary>
    public SyntaxNode Node { get; }

    /// <summary>
    /// Whether the scope declares it lexically (LexicallyDeclaredNames), rather than var-scoped (VarDeclaredNames); false
    /// also for a <see cref="BindingKind.Parameter"/>, <see cref="BindingKind.FunctionName"/> or
    /// <see cref="BindingKind.ClassName"/> declaration, which is in neither. <i>(Revised 2026-11-24, at SJL-15.2 stage 1,
    /// when the semantic model's kinds were added; it read "Whether the scope declares it lexically
    /// (LexicallyDeclaredNames), rather than var-scoped (VarDeclaredNames).")</i> False also for a
    /// <see cref="BindingKind.Arguments"/> declaration, which is in neither. <i>(Added 2026-11-26, at SJL-15.2 stage
    /// 5a.)</i>
    /// False also for a <see cref="BindingKind.BlockFunctionAlias"/> declaration, whose binding is var-scoped but which
    /// neither list holds. <i>(Added 2026-11-26, at SJL-15.2 stage 5b.)</i>
    /// </summary>
    public bool IsLexical { get; }

    /// <summary>
    /// Whether it is a function declaration directly in a block, a <c>switch</c>'s CaseBlock or an <c>if</c> clause (Annex B.3.2's block-level function).
    /// It is true for a generator, an async function and an async generator there too; whether the language also gives
    /// one a var-scoped binding is answered by <see cref="SemanticModel.AnnexBAlias"/>. <i>(Added 2026-11-26, at SJL-15.2
    /// stage 5b.)</i>
    /// </summary>
    public bool IsBlockFunction { get; }

    /// <summary>
    /// Whether it is a function declaration that is the whole of an <c>if</c> clause (Annex B.3.3), which the scope of an implicit block declares.
    /// Whether the language also gives it a var-scoped binding is answered by <see cref="SemanticModel.AnnexBAlias"/>.
    /// <i>(Added 2026-11-26, at SJL-15.2 stage 5b.)</i>
    /// </summary>
    public bool IsIfClauseFunction { get; }

    /// <summary>
    /// Whether its binding is mutable, by the operation that creates it: false for a binding created by
    /// CreateImmutableBinding or CreateImportBinding (ECMA-262 clauses 9.1.1.1.3 and 9.1.1.5.5) - a <c>const</c>, a
    /// <c>using</c> or an <c>await using</c> declaration (IsConstantDeclaration, clause 8.2.3; BlockDeclarationInstantiation,
    /// clause 14.2.3), an import's local name (a source text module record's InitializeEnvironment), a named function
    /// expression's own name (clause 15.2.5) and a class's inner name (clause 15.7.14); true for
    /// every other kind, which CreateMutableBinding creates. It follows from <see cref="Kind"/> alone. A
    /// <see cref="BindingKind.Arguments"/> binding is true too: strict FunctionDeclarationInstantiation creates it immutable,
    /// but "In strict mode code early errors prevent attempting to assign to this binding, so its mutability is not
    /// observable" (clause 10.2.11, step 22.c.ii's note). <i>(Added 2026-11-26, at SJL-15.2 stage 5a.)</i>
    /// A <see cref="BindingKind.BlockFunctionAlias"/> binding is true too: FunctionDeclarationInstantiation's
    /// CreateMutableBinding, GlobalDeclarationInstantiation's CreateGlobalVarBinding and EvalDeclarationInstantiation's
    /// CreateMutableBinding each make a mutable binding. Whether it can be deleted differs by clause and is not a model
    /// fact. <i>(Added 2026-11-26, at SJL-15.2 stage 5b.)</i>
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
    /// The names it declares, in source order: in a scope of the tables' kinds, its lexical declarations, and for a script,
    /// module, function or static block also its var-scoped ones, from every statement down to the next function boundary;
    /// in the semantic model's own scopes, its own kind's: a <see cref="ScopeKind.Parameters"/> scope's are its parameters
    /// (<see cref="BindingKind.Parameter"/>), one per BoundNames entry in source order, a name bound twice listed twice; a
    /// <see cref="ScopeKind.FunctionName"/> or <see cref="ScopeKind.ClassBody"/> scope's is the inner name
    /// (<see cref="BindingKind.FunctionName"/>, <see cref="BindingKind.ClassName"/>), a ClassBody scope of an anonymous
    /// class having none; a <see cref="ScopeKind.With"/> scope has none. <i>(Revised 2026-11-24, at SJL-15.2 stage 1, when
    /// the semantic model's kinds were added; it read "The names it declares, in source order: its lexical declarations,
    /// and for a script, module, function or static block also its var-scoped ones, from every statement down to the next
    /// function boundary.")</i>
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
/// its strictness and its parameters' names.")</i> From SJL-15.2 stage 5a it also answers whether it has a separate
/// variable environment (<see cref="HasSeparateVariableEnvironment"/>, row FF4), computed from the facts above, so that
/// it is the same with the semantic model on or off; the facts that depend on the model are
/// <see cref="SemanticModel.EnvironmentOf"/>'s. <i>(Added 2026-11-26, at SJL-15.2 stage 5a.)</i>
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

    /// <summary>
    /// Whether its VariableEnvironment is separate from the environment its parameters are bound in (row FF4): for a
    /// function of any kind, the dynamic function's goals among them, <see cref="HasParameterExpressions"/>
    /// (FunctionDeclarationInstantiation, ECMA-262 clause 10.2.11, step 30, whose note reads that closures created by
    /// expressions in the formal parameter list do not see the body's declarations); for eval code,
    /// <see cref="IsStrict"/> (PerformEval, clause 19.2.1.1, step 18, "If strictEval is true, set varEnv to lexEnv", strict
    /// eval code's VariableEnvironment being its own); false for a Script, a Module, a static block and a field
    /// initialiser, whose <see cref="HasParameterExpressions"/> is false. Computed from <see cref="Kind"/>,
    /// <see cref="IsStrict"/> and <see cref="HasParameterExpressions"/>, so that it is the same with the semantic model on
    /// or off. A separate parameter environment (steps 19 and 20) is <c>!IsStrict &amp;&amp; HasParameterExpressions</c>,
    /// and a separate LexicalEnvironment is <c>!IsStrict</c> for a function (steps 31 and 32) and always present for eval
    /// code (PerformEval steps 16.a and 17.a); neither is a member. <i>(Added 2026-11-26, at SJL-15.2 stage 5a.)</i>
    /// </summary>
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s10.2.11 FunctionDeclarationInstantiation step 30, s19.2.1.1 PerformEval step 18, JSD-0080 per-function facts FF4; IP=Low; Security=Low; Resources=0; Fingerprint=861B81
    // Hairs-Human:        PENDING
    public bool HasSeparateVariableEnvironment => Kind == FunctionKind.EvalCode ? IsStrict : HasParameterExpressions;
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
