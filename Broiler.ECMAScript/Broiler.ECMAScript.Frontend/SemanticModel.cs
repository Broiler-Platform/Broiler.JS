// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// HAIRS Code Assurance
// --------------------
// Relevant units:   36
// Annotated:        36/36
// Exempt:           110
// Human-reviewed:   0/36
// IP risk:          Low
// Security risk:    Medium
// Criteria:         0/0
// Resource impact:  1/10 max
// Unverified:       36
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Generic;
using Broiler.ECMAScript.Syntax;

namespace Broiler.ECMAScript.Semantics;

/// <summary>
/// The kind of a program, a function, a class static block or a field initialiser (Broiler.VM's JSD-0080, "The semantic
/// model's design": "the function's kind"; the area design's "arrow, async, generator, method, constructor, derived, field
/// initialiser, static block"), as its <see cref="FunctionFacts.Kind"/>. Whether it is async and whether it is a generator
/// are <see cref="FunctionFacts.IsAsync"/> and <see cref="FunctionFacts.IsGenerator"/>, beside it.
/// </summary>
/// <remarks>The numbers are stable; a later member takes a new number.</remarks>
// Hairs-AI:           Origin=AI; Spec=ECMA-262 s10.2 ECMAScript function objects, s15 functions and classes, JSD-0080 semantic model function kind; IP=Low; Security=Low; Resources=0; Fingerprint=309E57
// Hairs-Human:        PENDING
public enum FunctionKind
{
    /// <summary>A function declaration or expression, of any of its four forms: plain, generator, async or async generator (ECMA-262 clauses 15.2, 15.5, 15.6 and 15.8).</summary>
    Normal = 0,

    /// <summary>An arrow function or an async arrow function, whose [[ThisMode]] is lexical (ECMA-262 clauses 15.3 and 15.9).</summary>
    Arrow = 1,

    /// <summary>
    /// A method of an object literal or a class, plain, generator, async or async generator (ECMA-262 clause 15.4
    /// MethodDefinition, and clauses 15.5, 15.6 and 15.8), but a class's constructor, which is
    /// <see cref="ClassConstructor"/> or <see cref="DerivedConstructor"/>.
    /// </summary>
    Method = 2,

    /// <summary>A getter, <c>get</c> ClassElementName (ECMA-262 clause 15.4), of an object literal or a class.</summary>
    Getter = 3,

    /// <summary>A setter, <c>set</c> ClassElementName (ECMA-262 clause 15.4), of an object literal or a class.</summary>
    Setter = 4,

    /// <summary>
    /// The constructor a class without a heritage writes: its ClassBody's ConstructorMethod (ECMA-262 clause 15.7.3), whose
    /// [[ConstructorKind]] is base (ClassDefinitionEvaluation, clause 15.7.14).
    /// </summary>
    ClassConstructor = 5,

    /// <summary>
    /// The constructor a class with a heritage (<c>extends</c>) writes, whose [[ConstructorKind]] is derived
    /// (ClassDefinitionEvaluation, ECMA-262 clause 15.7.14).
    /// </summary>
    DerivedConstructor = 6,

    /// <summary>
    /// A field's initialiser, evaluated as a method of the class (ClassFieldDefinitionEvaluation, ECMA-262 clause
    /// 15.7.10); its <see cref="FunctionFacts.Node"/> is the <see cref="FieldDefinition"/>, an auto-accessor's among them.
    /// The semantic model's alone.
    /// </summary>
    FieldInitializer = 7,

    /// <summary>
    /// A class static block's body, evaluated as a method of the class (ClassStaticBlockDefinitionEvaluation, ECMA-262
    /// clause 15.7.11); its <see cref="FunctionFacts.Node"/> is the <see cref="Syntax.StaticBlock"/>. The semantic model's alone.
    /// </summary>
    StaticBlock = 8,

    /// <summary>A Script (ECMA-262 clause 16.1), the program of <see cref="ParseGoal.Script"/>.</summary>
    Script = 9,

    /// <summary>A Module (ECMA-262 clause 16.2), the program of <see cref="ParseGoal.Module"/>.</summary>
    Module = 10,

    /// <summary>
    /// Eval code (PerformEval, ECMA-262 clause 19.2.1.1), the program of <see cref="ParseGoal.Eval"/>, whose scope is a
    /// <see cref="ScopeKind.Script"/> scope: eval code is a kind of facts, not of scope.
    /// </summary>
    EvalCode = 11,

    /// <summary>
    /// A dynamic function's parameters or body (CreateDynamicFunction, ECMA-262 clause 20.2.1.1.1), the program of
    /// <see cref="ParseGoal.FormalParameters"/> and <see cref="ParseGoal.FunctionBody"/>; async and generator as the
    /// request's <see cref="ParseRequest.FunctionKind"/> says.
    /// </summary>
    DynamicFunction = 12,
}

/// <summary>
/// The semantic model of one compilation unit (Broiler.VM's JSD-0080, "The semantic model's design"; milestone SJL-15.2):
/// built per compilation unit, after the parse, by the declarations walk itself, which "does not start a second one", and
/// only when the request sets <see cref="ParseRequest.BuildSemanticModel"/>. At SJL-15.2 stage 1 it holds the scopes with
/// their parents, the declarations with their binding identifiers, and the per-function facts; from stage 3 it also holds
/// each <c>break</c>'s and <c>continue</c>'s target (<see cref="TargetOf(BreakStatement)"/>), and from stage 4 each
/// class's private environment and the binding each private name names (<see cref="PrivateEnvironmentOf"/>,
/// <see cref="Resolve(PrivateIdentifier)"/>), and from stage 5a each reference's and each binding identifier's
/// resolution (<see cref="Resolve(Identifier)"/>, <see cref="Resolve(BindingIdentifier)"/>) and the environment facts of
/// the program, every function, every static block and every initialised field (<see cref="EnvironmentOf"/>), and from
/// stage 5b each block function's Annex B alias (<see cref="AnnexBAlias"/>, Annex B.3.2, which row AB1 names by an
/// earlier edition's number, B.3.3), and from stage 5c the calls that may be direct evals and the caller's records a direct
/// <c>eval</c> at each receives (<see cref="DirectEvalCalls"/>, <see cref="CallerScopesOf"/>), and from stage 6 a Module's
/// record (<see cref="Module"/>), and from stage 7 the name each anonymous function or class definition's named evaluation
/// passes it and each function's free names and captures (<see cref="NamedEvaluationName"/>,
/// <see cref="NamedEvaluationKey"/>, <see cref="FreeNames"/>, <see cref="Captured"/>); the early
/// errors that are not purely grammatical are later stages'. From stage 2 the walk that builds it also reports its
/// early errors of the function context (the super rows, ContainsArguments, eval code's from the caller's context, and
/// three of expressions') among the parse's diagnostics, so that a source they refuse has no model, from stage 3 those
/// of jump targets (a <c>break</c> or a <c>continue</c> with no target, and an undefined break or continue target), and
/// from stage 4 those of private names (a private name no enclosing class declares, in a Script, in a Module, and in
/// eval code against the private names its caller sees), and from stage 6 those of the module record (ExportedNames'
/// duplicates and ExportedBindings not declared, catalogue rows EE0170 and EE0171).
/// <i>(Revised 2026-11-25, at SJL-15.2 stage 2; where it names the early errors, it read "and the early errors that are
/// not purely grammatical are later stages'.")</i> <i>(Revised 2026-11-25, at SJL-15.2 stage 3, when jump targets
/// joined the model; it read "resolution, jump targets, private names, the module record, named evaluation, captures
/// and the early errors that are not purely grammatical are later stages'" and "the early errors of jump targets,
/// private names and the module record are later stages'.")</i> <i>(Revised 2026-11-25, at SJL-15.2 stage 4, when
/// private names joined the model; it read "each <c>break</c>'s and <c>continue</c>'s target
/// (<see cref="TargetOf(BreakStatement)"/>); resolution, private names, the module record, named evaluation, captures
/// and the early errors that are not purely grammatical are later stages'", "and from stage 3 those of jump targets"
/// and "the early errors of private names and the module record are later stages'.")</i> <i>(Revised 2026-11-26, at
/// SJL-15.2 stage 5a, when resolution and the environment facts joined the model; it read "(<see
/// cref="PrivateEnvironmentOf"/>, <see cref="Resolve(PrivateIdentifier)"/>); resolution, the module record, named
/// evaluation, captures and the early errors that are not purely grammatical are later stages'.")</i>
/// <i>(Revised 2026-11-26, at SJL-15.2 stage 5b, when the Annex B aliases joined the model; it read "(<see cref="EnvironmentOf"/>);
/// Annex B.3.3's aliases, the caller's names of eval code, the module record, named evaluation, captures and the early
/// errors that are not purely grammatical are later stages'.")</i>
/// <i>(Revised 2026-11-26, at SJL-15.2 stage 5c, when the caller's records joined the model; it read "earlier edition's
/// number, B.3.3); the caller's names of eval code, the module record, named evaluation, captures and the early errors that
/// are not purely grammatical are later stages'.")</i> From stage 5c the walk also reports eval code's <c>var</c>s against
/// the caller's records the request gives (EvalDeclarationInstantiation step 3.d, catalogue rows EA0007 and EA0008).
/// <i>(Added 2026-11-26, at SJL-15.2 stage 5c.)</i>
/// <i>(Revised 2026-11-27, at SJL-15.2 stage 6, when the module record joined the model; it read "(<see
/// cref="DirectEvalCalls"/>, <see cref="CallerScopesOf"/>); the module record, named evaluation, captures and the early
/// errors that are not purely grammatical are later stages'" and "eval code against the private names its caller sees);
/// the early errors of the module record are later stages'.")</i>
/// <i>(Revised 2026-11-27, at SJL-15.2 stage 7, when named evaluation and captures joined the model; it read "record (<see
/// cref="Module"/>); named evaluation, captures and the early errors that are not purely grammatical are later
/// stages'.")</i>
/// </summary>
/// <remarks>
/// <para>
/// <b>It extends the tables and shares their instances</b> (the session's decision D-js-instances): every
/// <see cref="Scope"/>, <see cref="Declaration"/> and <see cref="FunctionFacts"/> of <see cref="ParseResult.Declarations"/>
/// is the same object here, and the model adds the scopes of the kinds <see cref="ScopeKind.With"/>,
/// <see cref="ScopeKind.ClassBody"/>, <see cref="ScopeKind.FunctionName"/> and <see cref="ScopeKind.Parameters"/>, their
/// declarations, and the facts of static blocks and field initialisers. The tables are the same with the model on or off.
/// </para>
/// <para>
/// <b>In source order, never a hash table's</b> (JSD-0080: "every collection in source order"): its scopes and its facts are
/// ordered by the start of their node and, for one start, by the order the walk made them in, which puts a scope before
/// every scope inside it. Every fact is keyed by node identity.
/// </para>
/// <para>
/// <b>The module record</b> (<see cref="Module"/>) keeps the same rule: its requests in the order of their first
/// statement and its entries in source order, each request's attributes in its first statement's source order, which
/// <see cref="ModuleRequest.Attributes"/> says how to sort as the specification does; it takes no node, so that no
/// <see cref="ArgumentException"/> is added. <i>(Added 2026-11-27, at SJL-15.2 stage 6.)</i>
/// </para>
/// <para>
/// <b>A node of another tree is a caller's error</b> (convention 3): <see cref="ScopeOf"/>, <see cref="DeclarationOf"/>,
/// <see cref="FactsOf"/>, both <see cref="TargetOf(BreakStatement)"/> overloads, <see cref="PrivateEnvironmentOf"/> and
/// <see cref="Resolve(PrivateIdentifier)"/> throw <see cref="ArgumentException"/>
/// for a node that is not in the model's tree, and <see cref="ParentOf"/> for a scope that is not one of the model's.
/// <i>(Revised 2026-11-25, at SJL-15.2 stage 3, when <c>TargetOf</c> joined them; it read "<see cref="ScopeOf"/>,
/// <see cref="DeclarationOf"/> and <see cref="FactsOf"/> throw <see cref="ArgumentException"/> for a node that is not in
/// the model's tree".)</i> <i>(Revised 2026-11-25, at SJL-15.2 stage 4, when <c>PrivateEnvironmentOf</c> and
/// <c>Resolve</c> joined them; it read "<see cref="FactsOf"/> and both <see cref="TargetOf(BreakStatement)"/> overloads
/// throw".)</i>
/// So do the <see cref="Resolve(Identifier)"/> and <see cref="Resolve(BindingIdentifier)"/> overloads and
/// <see cref="EnvironmentOf"/>. <i>(Added 2026-11-26, at SJL-15.2 stage 5a.)</i>
/// So does <see cref="AnnexBAlias"/>, for a declaration whose <see cref="Declaration.Node"/> is not in the model's tree.
/// <i>(Added 2026-11-26, at SJL-15.2 stage 5b.)</i>
/// So does <see cref="CallerScopesOf"/>, for a call that is not in the model's tree or is not a direct-eval candidate.
/// <i>(Added 2026-11-26, at SJL-15.2 stage 5c.)</i>
/// So do <see cref="NamedEvaluationName"/> and <see cref="NamedEvaluationKey"/>, for a node that is not in the model's
/// tree, and <see cref="FreeNames"/> and <see cref="Captured"/>, for such a node and, as <see cref="FactsOf"/> does, for a
/// node of it that has no facts. <i>(Added 2026-11-27, at SJL-15.2 stage 7.)</i>
/// </para>
/// <para><b>Immutable, and safe to call concurrently</b> (convention 10). The <see cref="CallerScope"/> instances
/// <see cref="CallerScopesOf"/> answers are init-only and copy their names, so that an instance shared by several answers,
/// or taken from the request, stays as it was. <i>(Added 2026-11-26, at SJL-15.2 stage 5c.)</i></para>
/// <para>The lists <see cref="FreeNames"/> and <see cref="Captured"/> answer are built with the model and read-only.
/// <i>(Added 2026-11-27, at SJL-15.2 stage 7.)</i></para>
/// </remarks>
// Hairs-AI:           Origin=AI; Spec=JSD-0080 semantic model design, ECMA-262 s9.1 environment records, s8.2 scope analysis; IP=Low; Security=Medium; Resources=1; Fingerprint=ADA8F1
// Hairs-Human:        PENDING
public sealed class SemanticModel
{
    private readonly Dictionary<SyntaxNode, Scope> scopeOf;
    private readonly Dictionary<Scope, Scope?> parentOf;
    private readonly Dictionary<SyntaxNode, Declaration> declarationOf;
    private readonly Dictionary<SyntaxNode, FunctionFacts> factsOf;
    private readonly Dictionary<SyntaxNode, LabelTarget> targetOf;
    private readonly Dictionary<SyntaxNode, PrivateNameBinding> privateOf;
    private readonly Dictionary<ClassNode, PrivateEnvironment> privateEnvironmentOf;
    private readonly Dictionary<SyntaxNode, Resolution> resolutionOf;
    private readonly Dictionary<SyntaxNode, EnvironmentFacts> environmentOf;
    private readonly Dictionary<Declaration, Declaration> annexBAliasOf;
    private readonly Dictionary<CallExpression, IReadOnlyList<CallerScope>?> callerScopesOf;
    private readonly Dictionary<SyntaxNode, string> namedEvaluationNameOf;
    private readonly Dictionary<SyntaxNode, ComputedPropertyName> namedEvaluationKeyOf;
    private readonly Dictionary<SyntaxNode, IReadOnlyList<string>> freeNamesOf;
    private readonly Dictionary<SyntaxNode, IReadOnlyList<Declaration>> capturedOf;

    // Hairs-AI:           Origin=AI; Spec=JSD-0080 semantic model design; IP=Low; Security=Low; Resources=1; Fingerprint=A28072
    // Hairs-Human:        PENDING
    internal SemanticModel(
        IReadOnlyList<Scope> scopes,
        IReadOnlyList<FunctionFacts> functions,
        Dictionary<SyntaxNode, Scope> scopeOf,
        Dictionary<Scope, Scope?> parentOf,
        Dictionary<SyntaxNode, Declaration> declarationOf,
        Dictionary<SyntaxNode, FunctionFacts> factsOf,
        Dictionary<SyntaxNode, LabelTarget> targetOf,
        Dictionary<SyntaxNode, PrivateNameBinding> privateOf,
        Dictionary<ClassNode, PrivateEnvironment> privateEnvironmentOf,
        Dictionary<SyntaxNode, Resolution> resolutionOf,
        Dictionary<SyntaxNode, EnvironmentFacts> environmentOf,
        Dictionary<Declaration, Declaration> annexBAliasOf,
        IReadOnlyList<CallExpression> directEvalCalls,
        Dictionary<CallExpression, IReadOnlyList<CallerScope>?> callerScopesOf,
        ModuleRecord? module,
        Dictionary<SyntaxNode, string> namedEvaluationNameOf,
        Dictionary<SyntaxNode, ComputedPropertyName> namedEvaluationKeyOf,
        Dictionary<SyntaxNode, IReadOnlyList<string>> freeNamesOf,
        Dictionary<SyntaxNode, IReadOnlyList<Declaration>> capturedOf)
    {
        Scopes = scopes;
        Functions = functions;
        this.scopeOf = scopeOf;
        this.parentOf = parentOf;
        this.declarationOf = declarationOf;
        this.factsOf = factsOf;
        this.targetOf = targetOf;
        this.privateOf = privateOf;
        this.privateEnvironmentOf = privateEnvironmentOf;
        this.resolutionOf = resolutionOf;
        this.environmentOf = environmentOf;
        this.annexBAliasOf = annexBAliasOf;
        DirectEvalCalls = directEvalCalls;
        this.callerScopesOf = callerScopesOf;
        Module = module;
        this.namedEvaluationNameOf = namedEvaluationNameOf;
        this.namedEvaluationKeyOf = namedEvaluationKeyOf;
        this.freeNamesOf = freeNamesOf;
        this.capturedOf = capturedOf;
    }

    /// <summary>
    /// Every scope, in the source order of its node's start, a scope before the scopes inside it (JSD-0080: "the scopes in
    /// source order"): those of <see cref="DeclarationTables.Scopes"/>, the same instances in the same relative order, and
    /// the model's own. The first is the program's: its <see cref="ScopeKind.Script"/> or <see cref="ScopeKind.Module"/>
    /// scope, or for a dynamic function's goals its <see cref="ScopeKind.Parameters"/> scope.
    /// </summary>
    /// <remarks>ECMA-262 at the pinned edition, clauses "Environment Records" and "Static Semantics: LexicallyScopedDeclarations". <i>(Added 2026-11-27, at SJL-15.2 stage 8.)</i></remarks>
    public IReadOnlyList<Scope> Scopes { get; }

    /// <summary>
    /// The program's, every function's, every class static block's and every initialised field's facts, in the source
    /// order of their node's start: those of <see cref="DeclarationTables.Functions"/>, the same instances in the same
    /// relative order, and the model's own, of kinds <see cref="FunctionKind.StaticBlock"/> and
    /// <see cref="FunctionKind.FieldInitializer"/>. A field without an initialiser has none.
    /// </summary>
    /// <remarks>ECMA-262 at the pinned edition, clauses "ECMAScript Function Objects" and "FunctionDeclarationInstantiation". <i>(Added 2026-11-27, at SJL-15.2 stage 8.)</i></remarks>
    public IReadOnlyList<FunctionFacts> Functions { get; }

    /// <summary>
    /// The scope whose environment is the running execution context's LexicalEnvironment where <paramref name="node"/> is
    /// evaluated or bound (ECMA-262 clause 9.1, and each construct's evaluation in clauses 14 to 16). A node that owns a
    /// scope is in the scope around it, and its children in its own, but where the specification evaluates a child
    /// outside it: a <c>switch</c>'s discriminant, a <c>with</c> statement's object and a class's decorators are outside;
    /// a class declaration's name is in the scope that binds it, and a class expression's name, its heritage and its
    /// elements, their keys and their initialisers in its <see cref="ScopeKind.ClassBody"/> scope; a named function
    /// expression's name is in its <see cref="ScopeKind.FunctionName"/> scope and a function declaration's in the scope
    /// that binds it; a function's parameters, their initialisers and computed keys are in its
    /// <see cref="ScopeKind.Parameters"/> scope and its body in its <see cref="ScopeKind.Function"/> scope; a <c>for</c>
    /// statement's lexical head, test, update and body, and a <c>for</c>-<c>in</c> or <c>for</c>-<c>of</c> statement's
    /// lexical head, expression and body, are in its <see cref="ScopeKind.For"/> scope (whose bound names the TDZ
    /// environment of ForIn/OfHeadEvaluation, clause 14.7.5.6, holds as well); the <see cref="FunctionNode"/> of a function
    /// declaration that is the whole of an <c>if</c> clause is in its implicit block's scope; a label is in the scope of
    /// the statement holding it; the program is in the model's first scope.
    /// </summary>
    /// <param name="node">A node of the model's tree.</param>
    /// <returns>The scope.</returns>
    /// <exception cref="ArgumentNullException">The node is null.</exception>
    /// <exception cref="ArgumentException">The node is not in the model's tree.</exception>
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s9.1 environment records, s9.4 execution contexts LexicalEnvironment, JSD-0080 ScopeOf; IP=Low; Security=Medium; Resources=1; Fingerprint=F2F8A3
    // Hairs-Human:        PENDING
    public Scope ScopeOf(SyntaxNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (!scopeOf.TryGetValue(node, out var scope))
            throw new ArgumentException("The node is not in the semantic model's tree.", nameof(node));
        return scope;
    }

    /// <summary>
    /// The scope around <paramref name="scope"/>: its environment's [[OuterEnv]] as the walk reads it from the source
    /// (ECMA-262 clause 9.1.1); null for the model's first scope. For one function the chain runs
    /// <see cref="ScopeKind.FunctionName"/> (a named function expression's alone), <see cref="ScopeKind.Parameters"/>,
    /// <see cref="ScopeKind.Function"/>, each the parent of the next. A <c>catch</c> without a parameter keeps the tables'
    /// empty <see cref="ScopeKind.Catch"/> scope, the parent of its block's, where the specification has no environment
    /// (clause 14.15.2).
    /// </summary>
    /// <param name="scope">One of the model's scopes.</param>
    /// <returns>The parent scope, or null.</returns>
    /// <exception cref="ArgumentNullException">The scope is null.</exception>
    /// <exception cref="ArgumentException">The scope is not one of the model's.</exception>
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s9.1.1 environment record OuterEnv, JSD-0080 scope parents; IP=Low; Security=Medium; Resources=1; Fingerprint=D9C86C
    // Hairs-Human:        PENDING
    public Scope? ParentOf(Scope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);
        if (!parentOf.TryGetValue(scope, out var parent))
            throw new ArgumentException("The scope is not one of the semantic model's.", nameof(scope));
        return parent;
    }

    /// <summary>
    /// The declaration a node makes (ECMA-262 clause 8.2.1 BoundNames; JSD-0080: "each binding identifier's declaration"):
    /// for each <see cref="BindingIdentifier"/> that declares a binding - a variable's, a parameter's, a function's or a
    /// class's name, a <c>catch</c> parameter, an import's local name - and for the <see cref="ExportDefaultDeclaration"/>
    /// that binds <c>*default*</c>; null for every other node of the tree. It is one declaration: for a class
    /// declaration's name, the outer <see cref="BindingKind.Class"/> declaration the tables hold, the class's inner
    /// <see cref="BindingKind.ClassName"/> declaration, with the same <see cref="Declaration.Node"/>, being in its
    /// <see cref="ScopeKind.ClassBody"/> scope's <see cref="Scope.Declarations"/>; for a class expression's name, the
    /// <see cref="BindingKind.ClassName"/> one, which is its only binding.
    /// </summary>
    /// <param name="node">A node of the model's tree.</param>
    /// <returns>The declaration, or null.</returns>
    /// <exception cref="ArgumentNullException">The node is null.</exception>
    /// <exception cref="ArgumentException">The node is not in the model's tree.</exception>
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s8.2.1 BoundNames, JSD-0080 DeclarationOf; IP=Low; Security=Medium; Resources=1; Fingerprint=6E8D5A
    // Hairs-Human:        PENDING
    public Declaration? DeclarationOf(SyntaxNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (!scopeOf.ContainsKey(node))
            throw new ArgumentException("The node is not in the semantic model's tree.", nameof(node));
        return declarationOf.TryGetValue(node, out var declaration) ? declaration : null;
    }

    /// <summary>
    /// The facts of the program, a <see cref="FunctionNode"/>, a <see cref="StaticBlock"/> or a <see cref="FieldDefinition"/>
    /// with an initialiser (JSD-0080: "per-function facts"), the instance <see cref="Functions"/> holds.
    /// </summary>
    /// <param name="node">The <see cref="ProgramNode"/>, a <see cref="FunctionNode"/>, a <see cref="StaticBlock"/> or an initialised <see cref="FieldDefinition"/> of the model's tree.</param>
    /// <returns>Its facts.</returns>
    /// <exception cref="ArgumentNullException">The node is null.</exception>
    /// <exception cref="ArgumentException">The node is not in the model's tree, or is a node of it that has no facts: none of those four.</exception>
    /// <remarks>ECMA-262 at the pinned edition, clauses "ECMAScript Function Objects" and "FunctionDeclarationInstantiation". <i>(Added 2026-11-27, at SJL-15.2 stage 8.)</i></remarks>
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s10.2 function objects, s15.7 class elements, JSD-0080 FactsOf; IP=Low; Security=Medium; Resources=1; Fingerprint=6D55B3
    // Hairs-Human:        PENDING
    public FunctionFacts FactsOf(SyntaxNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (!scopeOf.ContainsKey(node))
            throw new ArgumentException("The node is not in the semantic model's tree.", nameof(node));
        if (!factsOf.TryGetValue(node, out var facts))
            throw new ArgumentException("The node is not a program, a function, a static block or an initialised field.", nameof(node));
        return facts;
    }

    /// <summary>
    /// The target of a <c>break</c> (JSD-0080: "jump targets"; ECMA-262 clause 14.9.1's early errors, ContainsUndefinedBreakTarget
    /// and LabelledEvaluation): for <c>break;</c> the innermost iteration statement or
    /// <c>switch</c> that encloses it within its function, static block or program; for <c>break L;</c> the statement
    /// <c>L</c> labels, looked at through every further label on it, which may be any statement, the jump itself among them
    /// (<c>L: break L;</c>), and never a <see cref="LabelledStatement"/>. Every jump that targets one statement answers the
    /// same instance. Never null: the model exists only for a source the early errors accept, and in one every jump has a
    /// target. <i>(Added 2026-11-25, at SJL-15.2 stage 3.)</i>
    /// </summary>
    /// <param name="jump">A <see cref="BreakStatement"/> of the model's tree.</param>
    /// <returns>Its target, with the target statement's own label set.</returns>
    /// <exception cref="ArgumentNullException">The jump is null.</exception>
    /// <exception cref="ArgumentException">The jump is not in the model's tree.</exception>
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s14.9.1 BreakStatement early errors, ContainsUndefinedBreakTarget, LabelledEvaluation, JSD-0080 jump targets; IP=Low; Security=Medium; Resources=1; Fingerprint=AAC5E3
    // Hairs-Human:        PENDING
    public LabelTarget TargetOf(BreakStatement jump)
    {
        ArgumentNullException.ThrowIfNull(jump);
        if (!scopeOf.ContainsKey(jump))
            throw new ArgumentException("The node is not in the semantic model's tree.", nameof(jump));
        return targetOf[jump];
    }

    /// <summary>
    /// The target of a <c>continue</c> (JSD-0080: "jump targets"; ECMA-262 clause 14.8.1's early errors,
    /// ContainsUndefinedContinueTarget and LabelledEvaluation): for <c>continue;</c> the
    /// innermost iteration statement that encloses it within its function, static block or program; for
    /// <c>continue L;</c> the innermost such iteration statement whose own label set holds <c>L</c>, wherever <c>L</c>
    /// stands in that set, so that in <c>A: B: for (;;) continue A;</c> it is the <c>for</c> (Broiler.VM's hand-off H20,
    /// read by the specification). Every jump that targets one statement answers the same instance. Never null, as for
    /// <see cref="TargetOf(BreakStatement)"/>. <i>(Added 2026-11-25, at SJL-15.2 stage 3.)</i>
    /// </summary>
    /// <param name="jump">A <see cref="ContinueStatement"/> of the model's tree.</param>
    /// <returns>Its target, an iteration statement with its own label set.</returns>
    /// <exception cref="ArgumentNullException">The jump is null.</exception>
    /// <exception cref="ArgumentException">The jump is not in the model's tree.</exception>
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s14.8.1 ContinueStatement early errors, ContainsUndefinedContinueTarget, LabelledEvaluation, JSD-0080 jump targets; IP=Low; Security=Medium; Resources=1; Fingerprint=4CCEB2
    // Hairs-Human:        PENDING
    public LabelTarget TargetOf(ContinueStatement jump)
    {
        ArgumentNullException.ThrowIfNull(jump);
        if (!scopeOf.ContainsKey(jump))
            throw new ArgumentException("The node is not in the semantic model's tree.", nameof(jump));
        return targetOf[jump];
    }

    /// <summary>
    /// The private environment a class's body declares (JSD-0080: "private-name environments"; ECMA-262 PrivateEnvironment
    /// Records, made by ClassDefinitionEvaluation, clause 15.7.14): the class, the environment the class is
    /// evaluated in as its outer one, and its PrivateBoundIdentifiers as bindings in source order, once each. Every class of
    /// the tree has one, empty when it declares no private name. <i>(Added 2026-11-25, at SJL-15.2 stage 4.)</i>
    /// </summary>
    /// <param name="class">A <see cref="ClassNode"/> of the model's tree.</param>
    /// <returns>Its private environment.</returns>
    /// <exception cref="ArgumentNullException">The class is null.</exception>
    /// <exception cref="ArgumentException">The class is not in the model's tree.</exception>
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 PrivateEnvironment Records, s15.7.14 ClassDefinitionEvaluation, PrivateBoundIdentifiers, JSD-0080 private names; IP=Low; Security=Medium; Resources=1; Fingerprint=F1416F
    // Hairs-Human:        PENDING
    public PrivateEnvironment PrivateEnvironmentOf(ClassNode @class)
    {
        ArgumentNullException.ThrowIfNull(@class);
        if (!scopeOf.ContainsKey(@class))
            throw new ArgumentException("The node is not in the semantic model's tree.", nameof(@class));
        return privateEnvironmentOf[@class];
    }

    /// <summary>
    /// The binding a private name names (JSD-0080: "private names"; ECMA-262 ResolvePrivateIdentifier, read
    /// statically, and AllPrivateIdentifiersValid): for a class element's key, the binding it declares in its class's
    /// environment; for a reference, a member's <c>.#x</c> (an optional link's among them) or <c>#x in o</c>, the binding of
    /// the innermost class whose environment holds the reference and declares the name, whether the reference and the
    /// declaration are static or not. A class's heritage is resolved in the environment around the class, its keys,
    /// initialisers, static blocks and methods in its own. Every key and every reference that names one binding answer the
    /// same instance. Null where no class of the unit declares the name: in eval code, for a name its caller sees; under the
    /// dynamic function's goals; and for an object literal's private key, which declares nothing (catalogue row EE0016
    /// refuses every such key, so that no model holds one). Under the Script and Module
    /// goals no reference is null, since the model exists only for an accepted source. This is the first of the area
    /// design's two <c>Resolve</c> overloads; a caller passing a null literal names the overload by a cast.
    /// <i>(Added 2026-11-25, at SJL-15.2 stage 4.)</i>
    /// </summary>
    /// <param name="name">A <see cref="PrivateIdentifier"/> of the model's tree.</param>
    /// <returns>Its binding, or null.</returns>
    /// <exception cref="ArgumentNullException">The name is null.</exception>
    /// <exception cref="ArgumentException">The name is not in the model's tree.</exception>
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 ResolvePrivateIdentifier, AllPrivateIdentifiersValid, JSD-0080 private names; IP=Low; Security=Medium; Resources=1; Fingerprint=326814
    // Hairs-Human:        PENDING
    public PrivateNameBinding? Resolve(PrivateIdentifier name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (!scopeOf.ContainsKey(name))
            throw new ArgumentException("The node is not in the semantic model's tree.", nameof(name));
        return privateOf.TryGetValue(name, out var binding) ? binding : null;
    }

    /// <summary>
    /// The resolution of a reference (JSD-0080: "reference resolution: static, to a scope and declaration; dynamic, with its
    /// reason, where <c>with</c> or sloppy direct <c>eval</c> makes it so; or global"; ECMA-262 ResolveBinding and
    /// GetIdentifierReference, read statically): the binding the name resolves to where it stands, with
    /// the scopes that may bind it at run time before that one, as <see cref="Resolution"/> describes. Every
    /// <see cref="Identifier"/> of the tree has one, since an <see cref="Identifier"/> is exactly an IdentifierReference: a
    /// property name, a label, a meta-property's part and an export name are <see cref="IdentifierName"/>s. With Annex B on
    /// (<see cref="ParseSwitches.AnnexB"/>), a name that only a block function's Annex B alias binds (<see cref="AnnexBAlias"/>)
    /// is answered by that alias: in function code <see cref="ResolutionKind.Static"/>, to the function's
    /// <see cref="ScopeKind.Function"/> scope and the alias; in sloppy eval code <see cref="ResolutionKind.Inherited"/>,
    /// carrying the alias; in a Script <see cref="ResolutionKind.Global"/>, as before; and an <c>arguments</c> binding a block
    /// function's evaluation creates makes the variable environment's scope a dynamic entry for that name alone, with reason
    /// <see cref="DynamicReason.BlockFunction"/>. Eval code's alias is decided when the request carries the caller's records
    /// (<see cref="InheritedContext.CallerScopes"/>), a Catch clause's record exempt. Without them the request does not
    /// describe the caller, and the eval body alone decides, as <c>CallerScopes</c>' contract says. <i>(Revised 2026-11-26,
    /// at SJL-15.2 stage 5c, when the caller's records joined the request, closing the interim; it read "What remains: the
    /// interim, eval code's alias conditional on its caller's environments until stage 5c describes them; and" before the
    /// run-time conditions, which stay as written.)</i> What remains: the run-time conditions, which no stage closes: a
    /// Script's alias
    /// depends on GlobalDeclarationInstantiation's checks, and eval code with a global variable environment on
    /// GlobalDeclarationInstantiation-like checks. <i>(Revised 2026-11-26, at SJL-15.2
    /// stage 5b, when the aliases joined the model; it read "With Annex B on (<see cref="ParseSwitches.AnnexB"/>), a name that
    /// only a block function's Annex B.3.3 alias binds is answered as the scopes alone answer it until stage 5b:
    /// <see cref="ResolutionKind.Global"/>, or a <see cref="ResolutionKind.Static"/> or <see cref="ResolutionKind.Dynamic"/>
    /// answer naming an outer binding, which the alias shadows at run time.")</i> Beside
    /// the <see cref="Resolve(BindingIdentifier)"/> and <see cref="Resolve(PrivateIdentifier)"/> overloads, a caller passing a
    /// null literal names the overload by a cast. <i>(Added 2026-11-26, at SJL-15.2 stage 5a.)</i>
    /// </summary>
    /// <param name="reference">An <see cref="Identifier"/> of the model's tree.</param>
    /// <returns>Its resolution, never null.</returns>
    /// <exception cref="ArgumentNullException">The reference is null.</exception>
    /// <exception cref="ArgumentException">The reference is not in the model's tree.</exception>
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 ResolveBinding GetIdentifierReference, s14.11.2 with statement, s19.2.1.1 PerformEval, JSD-0080 reference resolution; IP=Low; Security=Medium; Resources=1; Fingerprint=95618E
    // Hairs-Human:        PENDING
    public Resolution Resolve(Identifier reference)
    {
        ArgumentNullException.ThrowIfNull(reference);
        if (!scopeOf.ContainsKey(reference))
            throw new ArgumentException("The node is not in the semantic model's tree.", nameof(reference));
        return resolutionOf[reference];
    }

    /// <summary>
    /// The resolution of a binding identifier (the session's decision D-js-resolution). A binding the language resolves
    /// when it runs is looked up as a reference standing there would be: every BoundName of a <c>var</c> declarator with an
    /// initialiser (VariableDeclaration evaluation, whose ResolveBinding a pattern's PutValue with
    /// an undefined environment repeats) and of a <c>for</c>-<c>in</c> or <c>for</c>-<c>of</c> statement's <c>var</c>
    /// head, so that <c>with (o) { var x = 2 }</c> writes <c>o.x</c> and <c>catch (e) { var e = 1 }</c> writes the
    /// <c>catch</c> parameter (Annex B.3.4). Every other binding answers as a reference would where the lookup reaches the
    /// scope that holds its <see cref="DeclarationOf"/>: <see cref="ResolutionKind.Static"/> with that scope and the name's
    /// first declaration in it, or, for a var-scoped declaration of sloppy eval code, <see cref="ResolutionKind.Inherited"/>
    /// with that first declaration. Duplicate sloppy parameters, which FunctionDeclarationInstantiation also initialises
    /// through ResolveBinding (its step 25.a), answer that way too: a simple sloppy list has no expression, <c>with</c> or
    /// direct <c>eval</c> between a parameter and its environment, so that the lookup ends there on the name's first
    /// declaration. So the two overloads agree: in <c>var v; var v = 1; v</c> both bindings and the reference answer the
    /// first <c>var</c>. <i>(Added 2026-11-26, at SJL-15.2 stage 5a.)</i>
    /// </summary>
    /// <param name="binding">A <see cref="BindingIdentifier"/> of the model's tree.</param>
    /// <returns>Its resolution, never null.</returns>
    /// <exception cref="ArgumentNullException">The binding is null.</exception>
    /// <exception cref="ArgumentException">The binding is not in the model's tree.</exception>
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 ResolveBinding, VariableDeclaration evaluation, ForIn/OfBodyEvaluation, Annex B.3.4, JSD-0080 reference resolution; IP=Low; Security=Medium; Resources=1; Fingerprint=9BAE93
    // Hairs-Human:        PENDING
    public Resolution Resolve(BindingIdentifier binding)
    {
        ArgumentNullException.ThrowIfNull(binding);
        if (!scopeOf.ContainsKey(binding))
            throw new ArgumentException("The node is not in the semantic model's tree.", nameof(binding));
        return resolutionOf[binding];
    }

    /// <summary>
    /// The environment facts of the program, a <see cref="FunctionNode"/>, a <see cref="StaticBlock"/> or a
    /// <see cref="FieldDefinition"/> with an initialiser (JSD-0080: "per-function facts"; rows FF3 and FF5), the nodes
    /// <see cref="FactsOf"/> accepts: its <c>arguments</c> binding, whether it is needed and mapped, and direct <c>eval</c>
    /// and <c>with</c> in its own code, as <see cref="EnvironmentFacts"/> describes. The separate variable environment, row
    /// FF4, is <see cref="FunctionFacts.HasSeparateVariableEnvironment"/>. <i>(Added 2026-11-26, at SJL-15.2 stage 5a.)</i>
    /// </summary>
    /// <param name="node">The <see cref="ProgramNode"/>, a <see cref="FunctionNode"/>, a <see cref="StaticBlock"/> or an initialised <see cref="FieldDefinition"/> of the model's tree.</param>
    /// <returns>Its environment facts.</returns>
    /// <exception cref="ArgumentNullException">The node is null.</exception>
    /// <exception cref="ArgumentException">The node is not in the model's tree, or is a node of it that has no facts: none of those four.</exception>
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s10.2.11 FunctionDeclarationInstantiation steps 15-22, s19.2.1.1 PerformEval, s14.11.2 with statement, JSD-0080 per-function facts; IP=Low; Security=Medium; Resources=1; Fingerprint=863083
    // Hairs-Human:        PENDING
    public EnvironmentFacts EnvironmentOf(SyntaxNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (!scopeOf.ContainsKey(node))
            throw new ArgumentException("The node is not in the semantic model's tree.", nameof(node));
        if (!environmentOf.TryGetValue(node, out var facts))
            throw new ArgumentException("The node is not a program, a function, a static block or an initialised field.", nameof(node));
        return facts;
    }

    /// <summary>
    /// The var-scoped binding a block function's evaluation also sets (JSD-0080's "Annex B's block-function aliases";
    /// ECMA-262 Annex B.3.2, the web legacy compatibility semantics of block-level function declarations, whose steps stand
    /// in the main text: FunctionDeclarationInstantiation step 32.a, GlobalDeclarationInstantiation step 12,
    /// EvalDeclarationInstantiation step 13, and BlockDeclarationInstantiation steps 3.a.ii.1 and 3.b.iii; correspondence
    /// row AB1, whose name, "Annex B.3.3 block-function aliases", is an earlier edition's number for the same feature):
    /// for a block function the pinned edition gives a var-scoped binding, the declaration that names the binding its
    /// evaluation sets (FunctionDeclarationInstantiation step 32.a.i.2.c, GlobalDeclarationInstantiation step 12.b.ii.2.a.ii.3,
    /// EvalDeclarationInstantiation step 13.b.ii.7.b), which is one of: the first <c>var</c> or top-level function
    /// declaration of the name in the variable scope, which the block function writes and which it creates no binding
    /// beside; for a block function named <c>arguments</c> in function code, the function's
    /// <see cref="EnvironmentOf"/>'s <see cref="EnvironmentFacts.Arguments"/> where its VariableEnvironment holds it, whose
    /// binding FunctionDeclarationInstantiation step 32.a.i.2.b creates no second of while step 32.a.i.2.c still writes it;
    /// or a declaration of <see cref="BindingKind.BlockFunctionAlias"/>, one per variable scope and name, shared by every
    /// admitted block function of that name there, in no scope's <see cref="Scope.Declarations"/>. The variable scope is the
    /// first <see cref="ScopeKind.Function"/>, <see cref="ScopeKind.Script"/>, <see cref="ScopeKind.StaticBlock"/> or
    /// <see cref="ScopeKind.Module"/> scope on <see cref="ParentOf"/>'s chain from the block function's scope.
    /// <i>(Added 2026-11-26, at SJL-15.2 stage 5b.)</i>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Null</b> for every declaration that is not a block function, and for the model's own
    /// <see cref="BindingKind.Arguments"/> and <see cref="BindingKind.BlockFunctionAlias"/> declarations. A block function
    /// (<see cref="Declaration.IsBlockFunction"/>) is admitted unless one of these holds, each an inference from the pinned
    /// text, since the only early errors a replacing <c>var</c> of the name can add are the lexical-against-var conflicts and
    /// FunctionDeclarationInstantiation tests the parameters itself: it is a generator, an async function or an async
    /// generator, which the clauses' "each FunctionDeclaration" leaves out; Annex B is off (<see cref="ParseSwitches.AnnexB"/>);
    /// its code is strict (FunctionDeclarationInstantiation steps 31 and 32, GlobalDeclarationInstantiation step 12.a,
    /// EvalDeclarationInstantiation step 13), a Module's, a class's and a static block's included; in function code, a
    /// parameter binds the name (FunctionDeclarationInstantiation step 32.a.i.2), an arrow's among them; a
    /// <see cref="ScopeKind.Block"/>, <see cref="ScopeKind.Switch"/> or <see cref="ScopeKind.For"/> scope between it and the
    /// variable scope declares the name, before or after it in the source, an enclosing block's own block function of the
    /// name among them; a <see cref="ScopeKind.Catch"/> scope between them declares the name and its parameter is not a lone
    /// BindingIdentifier, since a simple catch parameter does not block (Annex B.3.4), and then still shadows the binding
    /// inside the clause; the variable scope's own lexical declarations hold the name. A <c>with</c> scope adds no name.
    /// </para>
    /// <para>
    /// <b>The readings</b> (the session's decision D-js-annexb-aliases). Two plain block functions of one name in one block or
    /// CaseBlock are both admitted, sharing one alias, as the syntax vector <c>syntax/annex-b/eg0004/0001</c> records for both
    /// engines, although replacing either with a <c>var</c> would conflict with the other read literally; the block binding's
    /// value is the last one's (BlockDeclarationInstantiation step 3.b.iii). A labelled block function is admitted as the
    /// walk declares it, although read literally a LabelledStatement does not directly contain it.
    /// </para>
    /// <para>
    /// <b>When the binding is set.</b> The evaluation of each admitted declaration sets the binding to the block binding's
    /// value, so that the last one evaluated wins. Before that, a <see cref="BindingKind.BlockFunctionAlias"/> binding exists
    /// from instantiation, initialised to undefined (FunctionDeclarationInstantiation step 32.a.i.2.b,
    /// GlobalDeclarationInstantiation's CreateGlobalVarBinding, EvalDeclarationInstantiation step 13.b.ii.7.a); an existing
    /// <c>var</c> holds what it holds, undefined from instantiation unless something writes it; an existing top-level function
    /// starts as its function object; an <see cref="BindingKind.Arguments"/> answer starts as the arguments object. A block
    /// function named <c>arguments</c> in function code whose VariableEnvironment holds no <c>arguments</c> binding, an
    /// arrow's or a function's with parameter expressions, answers an alias whose binding does not exist until its
    /// evaluation creates it (a declarative record's SetMutableBinding creates a missing binding when S is false; an
    /// inference from the text), which <see cref="Resolve(Identifier)"/> answers as dynamic, with reason
    /// <see cref="DynamicReason.BlockFunction"/>.
    /// </para>
    /// <para>
    /// <b>Where the answer is conditional.</b> A Script's answer is what the source alone decides. A
    /// Script's alias also depends on GlobalDeclarationInstantiation's run-time checks (step 12.b.ii.2.a:
    /// HasLexicalDeclaration, which an earlier script's lexical declaration of the name fails, and CanDeclareGlobalVar), and
    /// no binding is made where either fails. Eval code's alias is decided when the request carries the caller's records
    /// (<see cref="InheritedContext.CallerScopes"/>), a Catch clause's record exempt: a caller's record that binds the name
    /// between the eval and its variable environment, and is not a Catch clause's, withholds it
    /// (EvalDeclarationInstantiation step 13.b.ii.4), and an object record never does; and with a global variable
    /// environment, GlobalDeclarationInstantiation-like checks can withhold it at run time (step 13.b.ii.5). Without the
    /// caller's records the request does not describe the caller, and the eval body alone decides, as
    /// <c>CallerScopes</c>' contract says. <i>(Revised 2026-11-26, at SJL-15.2 stage 5c, when the caller's records joined
    /// the request, closing the interim; it read "A Script's and eval code's answers are what the source alone decides.",
    /// "Eval code's alias depends on its caller's environments, which the request does not describe: a caller's lexical
    /// binding of the name between the eval and its variable environment, a Catch clause's excepted, withholds it
    /// (EvalDeclarationInstantiation step 13.b.ii.4); and" and "What remains: the interim, eval code's alias conditional on
    /// its caller's environments until stage 5c describes them; and", the sentence on step 13.b.ii.5 and the run-time
    /// conditions staying as written.)</i> What remains: the run-time
    /// conditions, which no stage closes: a Script's alias depends on GlobalDeclarationInstantiation's checks, and eval code
    /// with a global variable environment on GlobalDeclarationInstantiation-like checks. In eval
    /// code the binding it writes can be the caller's parameter or <c>arguments</c> binding (step 13.b.ii.7.a), which the
    /// model does not see, and the answer is the alias. Under <see cref="ParseGoal.FunctionBody"/> the body is answered with
    /// the parameters empty, as <see cref="Resolution"/> says, so an alias a real parameter would withhold is answered as
    /// admitted.
    /// </para>
    /// <para>
    /// The answer names the binding, not its value, as <see cref="Resolution.Declaration"/> does. Every answer is computed
    /// when the model is built and only looked up.
    /// </para>
    /// </remarks>
    /// <param name="declaration">A declaration of the model: one of its scopes' <see cref="Scope.Declarations"/>, or a declaration it answers.</param>
    /// <returns>The declaration that names the binding the block function's evaluation sets, or null.</returns>
    /// <exception cref="ArgumentNullException">The declaration is null.</exception>
    /// <exception cref="ArgumentException">The declaration's <see cref="Declaration.Node"/> is not in the model's tree, so that a declaration of another tree is refused: a node belongs to one tree, and a declaration's constructor is internal.</exception>
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 Annex B.3.2 block-level function declarations web legacy compatibility, s10.2.11 FunctionDeclarationInstantiation step 32.a, GlobalDeclarationInstantiation step 12, EvalDeclarationInstantiation step 13, JSD-0080 Annex B aliases; IP=Low; Security=Medium; Resources=1; Fingerprint=ABC107
    // Hairs-Human:        PENDING
    public Declaration? AnnexBAlias(Declaration declaration)
    {
        ArgumentNullException.ThrowIfNull(declaration);
        if (!scopeOf.ContainsKey(declaration.Node))
            throw new ArgumentException("The declaration is not of the semantic model's tree.", nameof(declaration));
        return annexBAliasOf.TryGetValue(declaration, out var alias) ? alias : null;
    }

    /// <summary>
    /// The calls that may be direct evals (JSD-0080's inherited context; the session's decision D-js-caller-scopes): every
    /// direct-<c>eval</c> candidate of the tree, as <see cref="EnvironmentFacts"/> defines one, of every strictness, in the
    /// source order of the call's start. Whether each is direct is SameValue with %eval% when it runs.
    /// <i>(Added 2026-11-26, at SJL-15.2 stage 5c.)</i>
    /// </summary>
    /// <remarks>ECMA-262 at the pinned edition, clauses "Function Calls" (the direct eval test of its evaluation) and "PerformEval". <i>(Added 2026-11-27, at SJL-15.2 stage 8.)</i></remarks>
    public IReadOnlyList<CallExpression> DirectEvalCalls { get; }

    /// <summary>
    /// The caller's records a direct <c>eval</c> at <paramref name="call"/> would receive, as
    /// <see cref="InheritedContext.CallerScopes"/> describes them (PerformEval steps 16.a and 16.b,
    /// FunctionDeclarationInstantiation steps 19 to 32, EvalDeclarationInstantiation step 3.d; the session's decision
    /// D-js-caller-scopes): from <see cref="ScopeOf"/> of the call outward by <see cref="ParentOf"/>, one entry per declarative
    /// record up to and not including the variable environment of the code that holds the call, an arrow's included,
    /// innermost first. <i>(Added 2026-11-26, at SJL-15.2 stage 5c.)</i>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The chain, by scope.</b> A <see cref="ScopeKind.With"/> scope adds no entry, being an object record. A
    /// <see cref="ScopeKind.Block"/>, <see cref="ScopeKind.Switch"/> or <see cref="ScopeKind.For"/> scope adds every name it
    /// declares, its block functions and class declarations among them, an empty one an empty entry. A
    /// <see cref="ScopeKind.ClassBody"/> scope inside the code that holds the call adds its inner name, or none. A
    /// <see cref="ScopeKind.Catch"/> scope with a parameter adds the parameter's names with <see cref="CallerScope.IsCatch"/>
    /// true, whatever its form; one without a parameter has no environment and adds nothing. Then the chain stops at the
    /// variable environment: a sloppy function's <see cref="ScopeKind.Function"/> scope adds its
    /// <see cref="Declaration.IsLexical"/> declarations (FunctionDeclarationInstantiation step 32.b) and a strict one's
    /// nothing (step 31.a); for a call in a function's parameters, a sloppy function's <see cref="ScopeKind.Parameters"/>
    /// scope adds every parameter, those after the call included, and <c>arguments</c> where
    /// <see cref="EnvironmentFacts.Arguments"/> is non-null (steps 20 and 22.d.i), an arrow having none, and a strict one's
    /// nothing (step 19); a <see cref="ScopeKind.StaticBlock"/> or <see cref="ScopeKind.Module"/> scope, a Script's top level
    /// (its variable environment the global record) and strict eval code's program add nothing. A field initialiser has no
    /// scope of its own: the class around it is the first scope outside it, where the chain stops with no entry, so that
    /// <c>class C { x = eval("") }</c> answers an empty list, while a class inside the initialiser is listed first. A named
    /// function expression's <see cref="ScopeKind.FunctionName"/> scope lies outside calleeEnv and is never reached. A
    /// <c>switch</c>'s discriminant stands outside its <see cref="ScopeKind.Switch"/> scope, and a <c>for</c>-<c>in</c> or
    /// <c>for</c>-<c>of</c> head's expression inside its <see cref="ScopeKind.For"/> scope, as <see cref="ScopeOf"/> says.
    /// </para>
    /// <para>
    /// <b>Eval code.</b> For a call in sloppy eval code the chain reaches the program's scope, whose
    /// <see cref="Declaration.IsLexical"/> declarations are eval code's own LexicalEnvironment, and continues into the
    /// request's own <see cref="InheritedContext.CallerScopes"/>, appended as given; where the request gives none the answer
    /// is null, since the caller is not described.
    /// </para>
    /// <para>
    /// The records are answered as they are, whatever the call's strictness; the library reads none of them for strict eval
    /// code. Each model scope has one <see cref="CallerScope"/> instance, shared by every answer that holds it, each name once
    /// in declaration order. Every answer is computed when the model is built and only looked up.
    /// </para>
    /// </remarks>
    /// <param name="call">One of <see cref="DirectEvalCalls"/>.</param>
    /// <returns>The records, innermost first, or null where the call is in sloppy eval code whose request gives no caller's records.</returns>
    /// <exception cref="ArgumentNullException">The call is null.</exception>
    /// <exception cref="ArgumentException">The call is not in the model's tree, or is a call of it that is not a direct-eval candidate.</exception>
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s19.2.1.1 PerformEval steps 16-18, s10.2.11 FunctionDeclarationInstantiation steps 19-32, s19.2.1.3 EvalDeclarationInstantiation step 3.d, JSD-0080 inherited context; IP=Low; Security=Medium; Resources=1; Fingerprint=80DE53
    // Hairs-Human:        PENDING
    public IReadOnlyList<CallerScope>? CallerScopesOf(CallExpression call)
    {
        ArgumentNullException.ThrowIfNull(call);
        if (!scopeOf.ContainsKey(call))
            throw new ArgumentException("The node is not in the semantic model's tree.", nameof(call));
        if (!callerScopesOf.TryGetValue(call, out var scopes))
            throw new ArgumentException("The call is not a direct-eval candidate.", nameof(call));
        return scopes;
    }

    /// <summary>
    /// A Module's record (JSD-0080: "the module record: requests with attributes, top-level <c>await</c>, import and export
    /// entries, and their errors"; the session's decision D-js-module-record; ECMA-262 ParseModule and the Source Text Module
    /// Record's fields): the requested modules, the import entries, the local, indirect and star export entries, and whether
    /// the module awaits at its top level, one instance, built with the model; null for every goal but
    /// <see cref="ParseGoal.Module"/>, as the sketch's <c>ModuleRecord?</c> allows. A Module with no import or export has a
    /// record with empty lists. Its early errors, ExportedNames' duplicates (EE0170) and ExportedBindings not declared
    /// (EE0171), are among the parse's diagnostics, so that a Module they refuse has no model.
    /// <i>(Added 2026-11-27, at SJL-15.2 stage 6.)</i>
    /// </summary>
    public ModuleRecord? Module { get; }

    /// <summary>
    /// The name an anonymous function or class definition's named evaluation passes it, where the source fixes that name
    /// (JSD-0080: "named-evaluation names"; correspondence row NE1; ECMA-262 NamedEvaluation, IsAnonymousFunctionDefinition
    /// and SetFunctionName; the session's decision D-js-named-evaluation): for the <see cref="FunctionNode"/> of an
    /// anonymous function expression, generator, async function, async generator or arrow, or the <see cref="ClassNode"/>
    /// of an anonymous class expression, that is the value of a named-evaluation site, through any number of
    /// <see cref="ParenthesizedExpression"/>s: the StringValue of a declarator's or a single-name binding's
    /// <see cref="BindingIdentifier"/>, of an assignment's, a logical assignment's or a destructuring assignment
    /// element's <see cref="Identifier"/> target, or of an assignment pattern shorthand's name; an object literal's or a
    /// class field's literal key's property name (an <see cref="IdentifierName"/>'s name, a <see cref="StringLiteral"/>'s
    /// value, a Number key's <c>Number::toString</c>, a BigInt key's digits in decimal); a private field's name with its
    /// <c>#</c>; and <c>"default"</c> for <c>export default</c> of an expression. The anonymous default declarations,
    /// <c>export default function () {}</c> (a generator, an async function and an async generator among them) and
    /// <c>export default class {}</c>, answer <c>"default"</c> too, which the specification gives them by SetFunctionName
    /// and not by NamedEvaluation; their binding is <c>*default*</c>, the record's local name, not the function's name.
    /// <i>(Added 2026-11-27, at SJL-15.2 stage 7.)</i>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Where named evaluation applies</b> this member or <see cref="NamedEvaluationKey"/> answers, never both; every other
    /// node answers neither. A compound assignment (<c>x += f</c>), a parenthesised or member target (<c>(a) = f</c>,
    /// <c>a.b = f</c>, <c>[(a) = f] = []</c>), a target that is a pattern, a comma expression (<c>(0, f)</c>) and an
    /// assignment as the value (<c>a = b = f</c> names <c>f</c> by <c>b</c> alone) give none, and so does an object
    /// literal's non-computed <c>__proto__</c> key, which sets the prototype; a class field's <c>__proto__</c> key names. A
    /// method, a getter and a setter, of an object literal or a class, answer null: DefineMethod names them by their key and
    /// prefix, which is not a named evaluation. So do a function or class with its own name, the
    /// <see cref="FunctionExpression"/>, <see cref="ArrowFunctionExpression"/> and <see cref="ClassExpression"/> wrappers,
    /// and every other node.
    /// </para>
    /// <para>
    /// <b>The answer is the name passed, not the final <c>name</c> property.</b> A class's name is set before its static
    /// elements are defined (ClassDefinitionEvaluation), so a static member named <c>name</c> overrides it. Where no name is
    /// passed the function's <c>name</c> is the empty string at run time, not a missing property; the member answers null.
    /// </para>
    /// <para>
    /// <b>Beyond the pinned edition</b>, as readings of the library's: a <c>using</c> or <c>await using</c> declarator names
    /// as a <c>let</c> or <c>const</c> one does, which test262's <c>statements/using/fn-name-fn.js</c> expects, while the
    /// pinned edition has no such declaration; and an auto-accessor field (<c>accessor x = f</c>, under the decorators
    /// proposal's flag) names as a field does, the tree's reading, whether the proposal names it being unknown. Annex B's
    /// <c>for (var x = f in o)</c> names as a <c>var</c> declarator does.
    /// </para>
    /// </remarks>
    /// <param name="node">A node of the model's tree.</param>
    /// <returns>The name, or null.</returns>
    /// <exception cref="ArgumentNullException">The node is null.</exception>
    /// <exception cref="ArgumentException">The node is not in the model's tree.</exception>
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 NamedEvaluation IsAnonymousFunctionDefinition SetFunctionName, JSD-0080 named-evaluation names; IP=Low; Security=Medium; Resources=1; Fingerprint=F233B1
    // Hairs-Human:        PENDING
    public string? NamedEvaluationName(SyntaxNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (!scopeOf.ContainsKey(node))
            throw new ArgumentException("The node is not in the semantic model's tree.", nameof(node));
        return namedEvaluationNameOf.TryGetValue(node, out var name) ? name : null;
    }

    /// <summary>
    /// The computed key whose value names an anonymous function or class definition at run time (JSD-0080:
    /// "named-evaluation names"; correspondence row NE1; ECMA-262 NamedEvaluation and SetFunctionName; the session's
    /// decision D-js-named-evaluation): for the node <see cref="NamedEvaluationName"/> takes, the
    /// <see cref="ComputedPropertyName"/> of the object literal entry <c>[k]: f</c> or the class field <c>[k] = f</c>, a
    /// static one among them, whose value it is. The name is SetFunctionName's of the key's ToPropertyKey: a String as it
    /// is, a Symbol as <c>"["</c>, its description and <c>"]"</c>, or the empty string for a Symbol without one; so
    /// <c>{ ["__proto__"]: f }</c> names <c>f</c> "__proto__". Where named evaluation applies this member or
    /// <see cref="NamedEvaluationName"/> answers, never both; every other node answers null. A key whose value the source
    /// writes, <c>['x']</c>, is not folded: the specification evaluates it at run time. <i>(Added 2026-11-27, at SJL-15.2
    /// stage 7.)</i>
    /// </summary>
    /// <param name="node">A node of the model's tree.</param>
    /// <returns>The key, or null.</returns>
    /// <exception cref="ArgumentNullException">The node is null.</exception>
    /// <exception cref="ArgumentException">The node is not in the model's tree.</exception>
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 NamedEvaluation SetFunctionName PropertyDefinitionEvaluation ClassFieldDefinitionEvaluation, JSD-0080 named-evaluation names; IP=Low; Security=Medium; Resources=1; Fingerprint=FE39C8
    // Hairs-Human:        PENDING
    public ComputedPropertyName? NamedEvaluationKey(SyntaxNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (!scopeOf.ContainsKey(node))
            throw new ArgumentException("The node is not in the semantic model's tree.", nameof(node));
        return namedEvaluationKeyOf.TryGetValue(node, out var key) ? key : null;
    }

    /// <summary>
    /// The free names of the program, a <see cref="FunctionNode"/>, a <see cref="StaticBlock"/> or an initialised
    /// <see cref="FieldDefinition"/>, the nodes <see cref="FactsOf"/> accepts (JSD-0080: "free names and captures, per
    /// function"; correspondence row FC1; the session's decision D-js-captures): each name an <see cref="Identifier"/> in
    /// its code, nested code included, resolves to a binding that no scope of its own, nor of a facts node nested in it,
    /// holds, once each by ordinal comparison, in the source order of its first such reference. <i>(Added 2026-11-27, at
    /// SJL-15.2 stage 7.)</i>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The model's definition.</b> The specification defines neither free names nor captures: a closure reaches an outer
    /// binding by resolving its name through its <c>[[Environment]]</c> chain. Each model scope belongs to one facts node:
    /// a scope made for a facts node to that node (a function's <see cref="ScopeKind.FunctionName"/>,
    /// <see cref="ScopeKind.Parameters"/> and <see cref="ScopeKind.Function"/> scopes, a static block's, and the program's,
    /// the dynamic function's parameters' among them), a class's <see cref="ScopeKind.ClassBody"/> scope to the facts node
    /// whose code holds the class, and every other scope to the facts node whose code makes it. A reference's binding is
    /// <see cref="Resolve(Identifier)"/>'s answer, or a dynamic answer's <see cref="Resolution.Fallback"/>: a
    /// <see cref="ResolutionKind.Static"/> one stands in its scope, and a <see cref="ResolutionKind.Global"/> or
    /// <see cref="ResolutionKind.Inherited"/> one outside every facts node. A dynamic answer's <c>with</c> objects and
    /// scopes that may gain bindings change nothing here, so for them the list may name more than run time reaches.
    /// </para>
    /// <para>
    /// <b>What a direct <c>eval</c>'s text names is not listed.</b> That text is no part of the tree, so a name only it
    /// uses is no reference here: in <c>function f() { var x; (() => eval("x"))(); }</c> the arrow reaches f's <c>x</c> at
    /// run time, and its list holds only "eval". <see cref="EnvironmentFacts.DirectEvalOrWithWithin"/> says where code may
    /// so reach bindings by names the list leaves out.
    /// </para>
    /// <para>
    /// <b>What counts.</b> Only references, not bindings: a <c>var</c> declarator's binding resolved when it runs is no
    /// use, so eval code's <c>var x = 1;</c> gives the program no free name and <c>var x = 1; x</c> gives "x". <c>this</c>,
    /// <c>super</c> and <c>new.target</c> are no names; <c>arguments</c> is an <see cref="Identifier"/> like any other, so
    /// an arrow's is free in the arrow and resolves to the enclosing function's <see cref="EnvironmentFacts.Arguments"/>. A
    /// named function expression's own name is in its <see cref="ScopeKind.FunctionName"/> scope, its own, so it is never
    /// its free name, though it is a free name of a function nested in it that uses it:
    /// <see cref="EnvironmentFacts.FunctionNameReferenced"/> says whether its code uses it. A class's inner
    /// name is free in its methods, field initialisers and static blocks that use it, and not in the code that holds the
    /// class; its heritage and computed keys stand in that code.
    /// </para>
    /// <para>
    /// The list is built with the model and read-only. The order is the source's; the specification gives none.
    /// </para>
    /// </remarks>
    /// <param name="function">The <see cref="ProgramNode"/>, a <see cref="FunctionNode"/>, a <see cref="StaticBlock"/> or an initialised <see cref="FieldDefinition"/> of the model's tree.</param>
    /// <returns>Its free names, empty where it has none.</returns>
    /// <exception cref="ArgumentNullException">The node is null.</exception>
    /// <exception cref="ArgumentException">The node is not in the model's tree, or is a node of it that has no facts: none of those four.</exception>
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s9.1 environment records OuterEnv, OrdinaryFunctionCreate Environment, ResolveBinding, JSD-0080 free names and captures; IP=Low; Security=Medium; Resources=1; Fingerprint=DB7149
    // Hairs-Human:        PENDING
    public IReadOnlyList<string> FreeNames(SyntaxNode function)
    {
        ArgumentNullException.ThrowIfNull(function);
        if (!scopeOf.ContainsKey(function))
            throw new ArgumentException("The node is not in the semantic model's tree.", nameof(function));
        if (!freeNamesOf.TryGetValue(function, out var names))
            throw new ArgumentException("The node is not a program, a function, a static block or an initialised field.", nameof(function));
        return names;
    }

    /// <summary>
    /// The declarations the free names of the program, a <see cref="FunctionNode"/>, a <see cref="StaticBlock"/> or an
    /// initialised <see cref="FieldDefinition"/> resolve to (JSD-0080: "free names and captures, per function";
    /// correspondence row FC1; the area design's <c>Captured</c>; the session's decision D-js-captures): for each name of
    /// <see cref="FreeNames"/> whose binding is <see cref="ResolutionKind.Static"/>, directly or as a dynamic answer's
    /// <see cref="Resolution.Fallback"/>, its <see cref="Resolution.Declaration"/>, the model's own instance, once each by
    /// reference and in <see cref="FreeNames"/>' order. A name resolving <see cref="ResolutionKind.Global"/> or
    /// <see cref="ResolutionKind.Inherited"/> is free and not captured: eval code's inherited binding is created in its
    /// caller's environment, not in any of this unit's, even where the answer carries the unit's own declaration.
    /// <i>(Added 2026-11-27, at SJL-15.2 stage 7.)</i>
    /// </summary>
    /// <remarks>
    /// Past the node's outermost scope every free reference of one name resolves to one binding, so a name gives one
    /// declaration. A captured declaration may be one of the model's own: an outer function's
    /// <see cref="EnvironmentFacts.Arguments"/>, which an arrow's <c>arguments</c> reaches; an Annex B alias
    /// (<see cref="BindingKind.BlockFunctionAlias"/>); or an outer function's <see cref="BindingKind.FunctionName"/> or an
    /// outer class's <see cref="BindingKind.ClassName"/>. A <c>for</c> statement's per-iteration copies of one binding are
    /// one declaration. The list is built with the model and read-only; the facts are the specification's, and where
    /// Broiler.JS's <c>FreeNameScan</c> answers otherwise the session's records name the difference.
    /// </remarks>
    /// <param name="function">The <see cref="ProgramNode"/>, a <see cref="FunctionNode"/>, a <see cref="StaticBlock"/> or an initialised <see cref="FieldDefinition"/> of the model's tree.</param>
    /// <returns>Its captured declarations, empty where it has none.</returns>
    /// <exception cref="ArgumentNullException">The node is null.</exception>
    /// <exception cref="ArgumentException">The node is not in the model's tree, or is a node of it that has no facts: none of those four.</exception>
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s9.1 environment records OuterEnv, OrdinaryFunctionCreate Environment, ResolveBinding, JSD-0080 free names and captures; IP=Low; Security=Medium; Resources=1; Fingerprint=67C779
    // Hairs-Human:        PENDING
    public IReadOnlyList<Declaration> Captured(SyntaxNode function)
    {
        ArgumentNullException.ThrowIfNull(function);
        if (!scopeOf.ContainsKey(function))
            throw new ArgumentException("The node is not in the semantic model's tree.", nameof(function));
        if (!capturedOf.TryGetValue(function, out var declarations))
            throw new ArgumentException("The node is not a program, a function, a static block or an initialised field.", nameof(function));
        return declarations;
    }
}

/// <summary>
/// A jump's target, as <see cref="SemanticModel.TargetOf(BreakStatement)"/> and
/// <see cref="SemanticModel.TargetOf(ContinueStatement)"/> answer it (JSD-0080: "jump targets"; milestone SJL-15.2, stage
/// 3; ECMA-262 ContainsUndefinedBreakTarget and ContainsUndefinedContinueTarget, and
/// LabelledEvaluation): the statement the jump leaves or continues, and that statement's own label set.
/// One instance per target statement, shared by every jump that targets it. <i>(Added 2026-11-25, at SJL-15.2 stage 3.)</i>
/// </summary>
/// <remarks>
/// The statement is the labelled item, never the <see cref="LabelledStatement"/> that carries a label, so that a loop's
/// labels and a block's are read one way (the session's decision D-js-jump-targets). Immutable, and safe to read
/// concurrently (convention 10).
/// </remarks>
// Hairs-AI:           Origin=AI; Spec=ECMA-262 ContainsUndefinedBreakTarget ContainsUndefinedContinueTarget, LabelledEvaluation labelSet, JSD-0080 jump targets; IP=Low; Security=Low; Resources=1; Fingerprint=6D5CFC
// Hairs-Human:        PENDING
public sealed class LabelTarget
{
    // Hairs-AI:           Origin=AI; Spec=JSD-0080 jump targets; IP=Low; Security=Low; Resources=1; Fingerprint=DD87FC
    // Hairs-Human:        PENDING
    internal LabelTarget(Statement statement, IReadOnlyList<string> labelSet)
    {
        Statement = statement;
        LabelSet = labelSet;
    }

    /// <summary>
    /// The statement the jump leaves or continues: an iteration statement, a <c>switch</c>, or the statement a labelled
    /// <c>break</c> names, looked at through every label on it; never a <see cref="LabelledStatement"/>.
    /// </summary>
    /// <remarks>ECMA-262 at the pinned edition, clauses "Runtime Semantics: LabelledEvaluation" and "Static Semantics: ContainsUndefinedBreakTarget". <i>(Added 2026-11-27, at SJL-15.2 stage 8.)</i></remarks>
    public Statement Statement { get; }

    /// <summary>
    /// The statement's own label set (LabelledEvaluation's labelSet): the labels of the chain of
    /// <see cref="LabelledStatement"/>s directly over it, outermost first, so that in <c>A: B: C: do ... while (c)</c> it is
    /// [A, B, C]; empty when it has none.
    /// </summary>
    /// <remarks>ECMA-262 at the pinned edition, clause "Runtime Semantics: LabelledEvaluation" (its labelSet). <i>(Added 2026-11-27, at SJL-15.2 stage 8.)</i></remarks>
    public IReadOnlyList<string> LabelSet { get; }
}

/// <summary>
/// The kind of a private name (ECMA-262 PrivateElement's [[Kind]]): a field, a method, or an accessor, as
/// <see cref="PrivateNameBinding.Kind"/>. <i>(Added 2026-11-25, at SJL-15.2 stage 4.)</i>
/// </summary>
/// <remarks>The numbers are stable; a later member takes a new number.</remarks>
// Hairs-AI:           Origin=AI; Spec=ECMA-262 PrivateElement Kind, JSD-0080 private names; IP=Low; Security=Low; Resources=0; Fingerprint=1E0C8E
// Hairs-Human:        PENDING
public enum PrivateNameKind
{
    /// <summary>A private field: a <see cref="FieldDefinition"/> that is not an auto-accessor.</summary>
    /// <remarks>ECMA-262 at the pinned edition, clause "PrivateElements" ([[Kind]] field). <i>(Added 2026-11-27, at SJL-15.2 stage 8.)</i></remarks>
    Field = 0,

    /// <summary>A private method: a <see cref="MethodDefinition"/> of <see cref="MethodKind.Method"/>, plain, generator, async or async generator.</summary>
    /// <remarks>ECMA-262 at the pinned edition, clause "PrivateElements" ([[Kind]] method). <i>(Added 2026-11-27, at SJL-15.2 stage 8.)</i></remarks>
    Method = 1,

    /// <summary>
    /// A private accessor: a getter, a setter, a getter and setter pair of one placement, or an auto-accessor
    /// (<c>accessor #x</c>, the auto-accessors proposal, <see cref="ProposalFlags.AutoAccessors"/>).
    /// </summary>
    /// <remarks>ECMA-262 at the pinned edition, clause "PrivateElements" ([[Kind]] accessor). <i>(Added 2026-11-27, at SJL-15.2 stage 8.)</i></remarks>
    Accessor = 2,
}

/// <summary>
/// A class's private environment, as <see cref="SemanticModel.PrivateEnvironmentOf"/> answers it (JSD-0080: "private-name
/// environments"; milestone SJL-15.2, stage 4; ECMA-262 PrivateEnvironment Records, and
/// ClassDefinitionEvaluation, clause 15.7.14): the class whose body declares it, the environment around it, and its private
/// names. <i>(Added 2026-11-25, at SJL-15.2 stage 4.)</i>
/// </summary>
/// <remarks>
/// Its outer environment is the one the class is evaluated in, so that a class in another class's heritage skips that
/// class, as AllPrivateIdentifiersValid and ClassDefinitionEvaluation read the heritage; it differs from the scope
/// <see cref="SemanticModel.ScopeOf"/> gives the heritage, which is the class's own (the session's decision
/// D-js-private-environments). Immutable, and safe to read concurrently (convention 10).
/// </remarks>
// Hairs-AI:           Origin=AI; Spec=ECMA-262 PrivateEnvironment Records OuterPrivateEnvironment Names, s15.7.14 ClassDefinitionEvaluation, JSD-0080 private names; IP=Low; Security=Low; Resources=1; Fingerprint=5528D6
// Hairs-Human:        PENDING
public sealed class PrivateEnvironment
{
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 PrivateEnvironment Records Names, JSD-0080 private names; IP=Low; Security=Low; Resources=0; Fingerprint=4214C5
    // Hairs-Human:        PENDING
    private IReadOnlyList<PrivateNameBinding> names = Array.Empty<PrivateNameBinding>();
    private Dictionary<string, PrivateNameBinding>? byName;

    // Hairs-AI:           Origin=AI; Spec=JSD-0080 private names; IP=Low; Security=Low; Resources=1; Fingerprint=0BB576
    // Hairs-Human:        PENDING
    internal PrivateEnvironment(ClassNode @class, PrivateEnvironment? outer)
    {
        Class = @class;
        Outer = outer;
    }

    /// <summary>The class whose ClassBody declares the environment.</summary>
    /// <remarks>ECMA-262 at the pinned edition, clauses "PrivateEnvironment Records" and "Runtime Semantics: ClassDefinitionEvaluation". <i>(Added 2026-11-27, at SJL-15.2 stage 8.)</i></remarks>
    public ClassNode Class { get; }

    /// <summary>
    /// Its [[OuterPrivateEnvironment]] as the source gives it: the private environment the class is evaluated in, so that a
    /// class in another class's heritage skips that class; null for the unit's outermost classes, eval code's included.
    /// </summary>
    public PrivateEnvironment? Outer { get; }

    /// <summary>
    /// Its [[Names]]: the class's PrivateBoundIdentifiers in source order, once each, <c>#constructor</c> among them where the
    /// class writes it; empty when it declares none.
    /// </summary>
    public IReadOnlyList<PrivateNameBinding> Names => names;

    /// <summary>The environment's names, set once by the walk that builds the model, before the model is published.</summary>
    // Hairs-AI:           Origin=AI; Spec=JSD-0080 private names; IP=Low; Security=Low; Resources=1; Fingerprint=4AFDF8
    // Hairs-Human:        PENDING
    internal void Seal(PrivateNameBinding[] bindings)
    {
        names = Array.AsReadOnly(bindings);
        byName = new Dictionary<string, PrivateNameBinding>(bindings.Length, StringComparer.Ordinal);
        foreach (var binding in bindings)
            byName.TryAdd(binding.Name, binding);
    }

    /// <summary>The binding of a name the environment declares, or null; a lookup, never an order.</summary>
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 ResolvePrivateIdentifier, JSD-0080 private names; IP=Low; Security=Low; Resources=1; Fingerprint=419EDD
    // Hairs-Human:        PENDING
    internal PrivateNameBinding? Find(string name) => byName is not null && byName.TryGetValue(name, out var binding) ? binding : null;
}

/// <summary>
/// A private name's binding in its class's environment, as <see cref="SemanticModel.Resolve(PrivateIdentifier)"/> answers
/// it (JSD-0080: "private names"; milestone SJL-15.2, stage 4; the area design's <c>PrivateNameBinding</c>): its name, its
/// environment, its kind, its placement, and the elements that declare it. One instance per binding, shared by its keys
/// and by every reference to it. <i>(Added 2026-11-25, at SJL-15.2 stage 4.)</i>
/// </summary>
/// <remarks>
/// A getter and a setter of one name and one placement are one binding with two elements. The class's [[PrivateMethods]]
/// brands are read from its environment's names: a binding not of <see cref="PrivateNameKind.Field"/>, instance or static
/// (the session's decision D-js-private-environments). Immutable, and safe to read concurrently (convention 10).
/// </remarks>
// Hairs-AI:           Origin=AI; Spec=ECMA-262 PrivateElement, PrivateEnvironment Records Names, PrivateBoundIdentifiers, JSD-0080 private names; IP=Low; Security=Low; Resources=1; Fingerprint=6F405B
// Hairs-Human:        PENDING
public sealed class PrivateNameBinding
{
    // Hairs-AI:           Origin=AI; Spec=JSD-0080 private names; IP=Low; Security=Low; Resources=1; Fingerprint=158CA8
    // Hairs-Human:        PENDING
    internal PrivateNameBinding(string name, PrivateEnvironment environment, PrivateNameKind kind, bool isStatic, IReadOnlyList<SyntaxNode> elements)
    {
        Name = name;
        Environment = environment;
        Kind = kind;
        IsStatic = isStatic;
        Elements = elements;
    }

    /// <summary>The name's StringValue, with its <c>#</c> (clause 12.7.1.2), as <see cref="PrivateIdentifier.Name"/> holds it.</summary>
    public string Name { get; }

    /// <summary>The private environment that holds it, the one <see cref="SemanticModel.PrivateEnvironmentOf"/> answers for its class.</summary>
    /// <remarks>ECMA-262 at the pinned edition, clauses "PrivateEnvironment Records" and "ResolvePrivateIdentifier". <i>(Added 2026-11-27, at SJL-15.2 stage 8.)</i></remarks>
    public PrivateEnvironment Environment { get; }

    /// <summary>Whether it is a field, a method or an accessor.</summary>
    /// <remarks>ECMA-262 at the pinned edition, clause "PrivateElements" ([[Kind]]). <i>(Added 2026-11-27, at SJL-15.2 stage 8.)</i></remarks>
    public PrivateNameKind Kind { get; }

    /// <summary>Whether its elements are <c>static</c>. A static name used through an instance, or the other way round, is the brand check's run-time error, not an early one.</summary>
    /// <remarks>ECMA-262 at the pinned edition, clause "Runtime Semantics: ClassDefinitionEvaluation" (its static and instance private methods). <i>(Added 2026-11-27, at SJL-15.2 stage 8.)</i></remarks>
    public bool IsStatic { get; }

    /// <summary>
    /// The class elements that declare it, each a <see cref="MethodDefinition"/> or a <see cref="FieldDefinition"/>, in source
    /// order: one, or a getter and a setter.
    /// </summary>
    /// <remarks>ECMA-262 at the pinned edition, clauses "Static Semantics: PrivateBoundIdentifiers" and "Runtime Semantics: ClassElementEvaluation". <i>(Added 2026-11-27, at SJL-15.2 stage 8.)</i></remarks>
    public IReadOnlyList<SyntaxNode> Elements { get; }
}

/// <summary>
/// The kind of a <see cref="Resolution"/> (the session's decision D-js-resolution). <i>(Added 2026-11-26, at SJL-15.2
/// stage 5a.)</i>
/// </summary>
/// <remarks>The numbers are stable; a later member takes a new number.</remarks>
// Hairs-AI:           Origin=AI; Spec=ECMA-262 ResolveBinding GetIdentifierReference, JSD-0080 reference resolution; IP=Low; Security=Low; Resources=0; Fingerprint=6798E4
// Hairs-Human:        PENDING
public enum ResolutionKind
{
    /// <summary>To the binding of <see cref="Resolution.Scope"/>'s environment named by <see cref="Resolution.Declaration"/>.</summary>
    /// <remarks>ECMA-262 at the pinned edition, clauses "ResolveBinding" and "GetIdentifierReference". <i>(Added 2026-11-27, at SJL-15.2 stage 8.)</i></remarks>
    Static = 0,

    /// <summary>
    /// To a binding one of <see cref="Resolution.DynamicScopes"/> may hold at run time, a <c>with</c> statement's object, a
    /// sloppy direct <c>eval</c>'s <c>var</c>, or the <c>arguments</c> binding a block function's evaluation creates, and
    /// otherwise to <see cref="Resolution.Fallback"/>. <i>(Revised 2026-11-26, at SJL-15.2 stage 5b; it read "To a binding
    /// one of <see cref="Resolution.DynamicScopes"/> may hold at run time, a <c>with</c> statement's object or a sloppy
    /// direct <c>eval</c>'s <c>var</c>, and otherwise to <see cref="Resolution.Fallback"/>.")</i>
    /// </summary>
    /// <remarks>ECMA-262 at the pinned edition, clauses "GetIdentifierReference", "Object Environment Records" and "PerformEval". <i>(Added 2026-11-27, at SJL-15.2 stage 8.)</i></remarks>
    Dynamic = 1,

    /// <summary>
    /// To no binding the unit declares or the parse knows: GetIdentifierReference ends at the global environment, so that a read throws a ReferenceError unless the global object holds the name, <c>typeof</c> gives
    /// <c>"undefined"</c>, and a sloppy write creates a global. Only Script, Module and the dynamic function's goals answer
    /// it.
    /// </summary>
    /// <remarks>ECMA-262 at the pinned edition, clauses "GetIdentifierReference" and "Global Environment Records". <i>(Added 2026-11-27, at SJL-15.2 stage 8.)</i></remarks>
    Global = 2,

    /// <summary>
    /// To the environments eval code inherits, which the request does not describe (<see cref="InheritedContext"/>): the
    /// caller's chain for a direct <c>eval</c>, the global environment for an indirect one, which the request does not tell
    /// apart. Only eval code answers it: a name eval code does not declare, and a var-scoped declaration of sloppy eval
    /// code, whose binding PerformEval creates in the caller's VariableEnvironment (step 16.b).
    /// From stage 5c the request may describe the caller's records (<see cref="InheritedContext.CallerScopes"/>); they narrow
    /// only the carried declaration and eval code's aliases, and the answer stays inherited, since the records stop at the
    /// variable environment, leave out <c>with</c> objects and are no scopes of this model. <i>(Added 2026-11-26, at
    /// SJL-15.2 stage 5c.)</i>
    /// </summary>
    Inherited = 3,
}

/// <summary>
/// Why a <see cref="ResolutionKind.Dynamic"/> resolution is dynamic: the reason of its innermost
/// <see cref="Resolution.DynamicScopes"/> entry. <i>(Added 2026-11-26, at SJL-15.2 stage 5a.)</i>
/// An entry's own reason is <see cref="With"/> for a <see cref="ScopeKind.With"/> scope, <see cref="SloppyDirectEval"/> for a
/// scope a sloppy direct <c>eval</c> may add to, and <see cref="BlockFunction"/> for one that may gain only
/// <c>arguments</c>, by a block function's evaluation; a scope that may gain both ways has <see cref="SloppyDirectEval"/>,
/// the wider gain. <i>(Added 2026-11-26, at SJL-15.2 stage 5b.)</i>
/// </summary>
/// <remarks>The numbers are stable; a later member takes a new number.</remarks>
// Hairs-AI:           Origin=AI; Spec=ECMA-262 s14.11.2 with statement NewObjectEnvironment, s19.2.1.1 PerformEval step 16.b, JSD-0080 dynamic markers; IP=Low; Security=Low; Resources=0; Fingerprint=2AD202
// Hairs-Human:        PENDING
public enum DynamicReason
{
    /// <summary>A <c>with</c> statement's object environment (ECMA-262 clause 14.11.2, NewObjectEnvironment), which may bind any name.</summary>
    With = 0,

    /// <summary>
    /// A sloppy direct <c>eval</c>, which declares its <c>var</c>s in the running VariableEnvironment (PerformEval, ECMA-262
    /// clause 19.2.1.1, step 16.b).
    /// </summary>
    SloppyDirectEval = 1,

    /// <summary>
    /// A block function named <c>arguments</c> whose evaluation sets the running VariableEnvironment's <c>arguments</c>
    /// binding where that environment holds none, an arrow's or a function's with parameter expressions, so that its
    /// SetMutableBinding creates the binding (ECMA-262 Annex B.3.2, FunctionDeclarationInstantiation step 32.a.i.2.c; a
    /// declarative record's SetMutableBinding creates a missing binding when S is false): the entry may gain the name
    /// <c>arguments</c> alone. <i>(Added 2026-11-26, at SJL-15.2 stage 5b.)</i>
    /// </summary>
    BlockFunction = 2,
}

/// <summary>
/// The resolution of a reference or a binding identifier, as <see cref="SemanticModel.Resolve(Identifier)"/> and
/// <see cref="SemanticModel.Resolve(BindingIdentifier)"/> answer it (JSD-0080: "reference resolution"; milestone SJL-15.2,
/// stage 5a; the area design's <c>Resolution</c>): <see cref="ResolutionKind.Static"/>, to a scope and a declaration;
/// <see cref="ResolutionKind.Dynamic"/>, with the scopes that may bind the name at run time, innermost first, the
/// innermost's reason, and the answer when none of them binds it; <see cref="ResolutionKind.Global"/>; or
/// <see cref="ResolutionKind.Inherited"/>, for eval code. <i>(Added 2026-11-26, at SJL-15.2 stage 5a.)</i>
/// </summary>
/// <remarks>
/// <para>
/// <b>The lookup.</b> From the scope where the node stands outward by <see cref="SemanticModel.ParentOf"/>: a
/// <see cref="ScopeKind.With"/> scope may bind any name and is a dynamic entry; a scope that binds the name ends the
/// lookup; a scope that does not, but whose environment a sloppy direct <c>eval</c> may add to, is a dynamic entry; the end
/// of the chain is <see cref="ResolutionKind.Inherited"/> for eval code and <see cref="ResolutionKind.Global"/> for every
/// other unit. Resolution is finished after the walk, over the references the walk recorded, since a later <c>eval</c>
/// makes an earlier reference dynamic (<c>function f() { x; eval("var x"); }</c>); it is a pass over a list, not a second
/// walk of the tree.
/// A scope whose environment a block function's evaluation may give an <c>arguments</c> binding (Annex B.3.2) is a dynamic
/// entry for the name <c>arguments</c> alone. <i>(Added 2026-11-26, at SJL-15.2 stage 5b.)</i>
/// </para>
/// <para>
/// <b>A scope binds</b> its <see cref="Scope.Declarations"/> by name; a <see cref="ScopeKind.Parameters"/> scope also binds
/// its function's <c>arguments</c> where <see cref="EnvironmentFacts.Arguments"/> is non-null
/// (FunctionDeclarationInstantiation step 22), as a <see cref="ScopeKind.StaticBlock"/> scope binds its block's. A Script's
/// declarations are <see cref="ResolutionKind.Static"/> to the Script scope, its lexical ones in the global declarative
/// record and the others in the object record (GlobalDeclarationInstantiation); storage stays each engine's.
/// A sloppy function's <see cref="ScopeKind.Function"/> scope also binds the name of each
/// <see cref="BindingKind.BlockFunctionAlias"/> declaration <see cref="SemanticModel.AnnexBAlias"/> answers for its block
/// functions, as sloppy eval code's <see cref="ScopeKind.Script"/> scope binds its own, whose answer is
/// <see cref="ResolutionKind.Inherited"/> carrying the alias; but not an <c>arguments</c> alias whose binding the
/// evaluation creates, which is a dynamic entry instead, and not a Script's alias, whose references stay
/// <see cref="ResolutionKind.Global"/> since GlobalDeclarationInstantiation's checks can leave it unmade. <i>(Added
/// 2026-11-26, at SJL-15.2 stage 5b.)</i>
/// </para>
/// <para>
/// <b><see cref="Declaration"/> names the binding, not its value.</b> Several declarations of one name in one scope make
/// one binding, and the answer names the first in source order. Where several function declarations share a name, the
/// binding's value is the last one's ("If there are multiple function declarations for the same name, the last
/// declaration is used", FunctionDeclarationInstantiation step 14.a.iii.2, as GlobalDeclarationInstantiation and
/// EvalDeclarationInstantiation note too); a <c>var</c> and a function declaration of one name are one binding, whose
/// value is the function. A top-level <c>var</c> or function declaration in a function's <see cref="ScopeKind.Function"/>
/// scope of a parameter's name, or of <c>arguments</c>, answers its own declaration: where
/// <see cref="FunctionFacts.HasSeparateVariableEnvironment"/> is false such a <c>var</c> creates no binding and is that
/// parameter's binding (FunctionDeclarationInstantiation steps 29.b and 29.c), and such a function declaration's object
/// replaces the parameter's value (step 37, in the VariableEnvironment, which is the parameters' environment); otherwise
/// each is a binding of its own, a <c>var</c> initialised from the parameter (steps 30.e.i.4 and 30.e.i.6) and a function
/// declaration to undefined (step 30.e.i.3) and then to its function object.
/// </para>
/// <para>
/// <b>A <see cref="DynamicScopes"/> entry</b> is a <see cref="ScopeKind.With"/> scope, the statement's object environment,
/// whose reason is <see cref="DynamicReason.With"/>; or a scope a sloppy direct <c>eval</c> may add to, whose reason is
/// <see cref="DynamicReason.SloppyDirectEval"/>: a <see cref="ScopeKind.Function"/> entry is the function's own
/// VariableEnvironment, where its body holds such a call and the environment is separate
/// (FunctionDeclarationInstantiation step 30); a <see cref="ScopeKind.Parameters"/> entry stands for one of two
/// environments, never both: where the function's <see cref="EnvironmentFacts.DirectEvalInParameters"/> is true, its
/// calleeEnv, just outside the parameters' environment (step 20 and its note), and otherwise the parameters' environment
/// itself, which is also the body's VariableEnvironment (step 29.d). A caller tells the two apart by
/// <c>EnvironmentOf(entry.Node).DirectEvalInParameters</c>. For the name <c>arguments</c> alone, a
/// <see cref="ScopeKind.Function"/> or <see cref="ScopeKind.Parameters"/> entry can also stand for a function's
/// VariableEnvironment that a block function's evaluation may give an <c>arguments</c> binding (Annex B.3.2,
/// FunctionDeclarationInstantiation step 32.a.i.2.c): the <see cref="ScopeKind.Function"/> scope where the environment is
/// separate, and otherwise the <see cref="ScopeKind.Parameters"/> scope, whose reason is
/// <see cref="DynamicReason.BlockFunction"/>; an entry that may gain both ways has <see cref="DynamicReason.SloppyDirectEval"/>.
/// Each entry's own reason follows from its kind and from how it may gain. <i>(Revised 2026-11-26, at SJL-15.2 stage 5b;
/// it read "Each entry's own reason follows from its kind.")</i>
/// </para>
/// <para>
/// <b>Eval code.</b> The request does not tell a direct <c>eval</c> from an indirect one, and its caller's records
/// (<see cref="InheritedContext.CallerScopes"/>) stop at the variable environment, leave out <c>with</c> objects and are no
/// scopes of this model, so a name eval code does not declare is <see cref="ResolutionKind.Inherited"/>, with no
/// declaration; a var-scoped
/// declaration of sloppy eval code (a top-level <c>var</c> or function declaration) is
/// <see cref="ResolutionKind.Inherited"/> with that declaration and no scope, since its binding is created in the caller's
/// VariableEnvironment (PerformEval step 16.b); strict eval code's declarations, whose VariableEnvironment is its own (step
/// 18), and sloppy eval code's lexical ones are <see cref="ResolutionKind.Static"/>. The caller's records decide whether a
/// caller's catch parameter stands before the binding: where one of them binds the name, an
/// <see cref="ResolutionKind.Inherited"/> answer for a reference, for a binding the language resolves when it runs, or as a
/// dynamic answer's <see cref="Fallback"/> carries no declaration, and every other binding keeps its own, since
/// EvalDeclarationInstantiation instantiates it in the variable environment without passing those records; without them
/// the carried declaration is the eval body's, as <c>CallerScopes</c>' contract says. A caller's <c>with</c> object of the
/// name can still stand before the binding, and a global variable environment's checks can still withhold an alias; both
/// are run-time conditions that no stage closes. <i>(Revised 2026-11-26, at SJL-15.2 stage 5c, when the caller's records
/// joined the request; it read "The request carries no caller's names and does not tell a direct <c>eval</c> from an
/// indirect one, so", "VariableEnvironment (PerformEval step 16.b), before which a caller's <c>with</c> object or
/// <c>catch</c> parameter of the name can stand; strict eval code's declarations, whose VariableEnvironment is its own (step
/// 18), and sloppy eval code's lexical ones are <see cref="ResolutionKind.Static"/>. The caller's names are a later
/// stage's.")</i>
/// A name only an Annex B alias of sloppy eval code binds is <see cref="ResolutionKind.Inherited"/> too, carrying that
/// <see cref="BindingKind.BlockFunctionAlias"/> declaration, whose binding is made in the caller's VariableEnvironment
/// (EvalDeclarationInstantiation step 13.b.ii.7.a) unless a caller's non-Catch lexical binding of the name between them
/// withholds it (step 13.b.ii.4), or, for a global variable environment, the global checks do (step 13.b.ii.5); a
/// caller's <c>catch</c> parameter or <c>with</c> object can still stand before it, as for eval code's <c>var</c>s. Either
/// way the binding lies outside the eval code. <i>(Added 2026-11-26, at SJL-15.2 stage 5b.)</i>
/// With the caller's records
/// given, a record that is not a Catch clause's and binds the name withholds the alias, so that the name answers
/// <see cref="ResolutionKind.Inherited"/> with no declaration, and under a Catch clause's record of the name the alias is
/// kept and the answer carries no declaration, the parameter standing first. <i>(Added 2026-11-26, at SJL-15.2 stage
/// 5c.)</i>
/// </para>
/// <para>
/// <b>The dynamic function's goals</b> (CreateDynamicFunction, whose function's scope is the realm's global environment):
/// each goal is answered with the other piece empty, so that under <see cref="ParseGoal.FunctionBody"/> <c>return a</c> in
/// the body of <c>new Function("a", "return a")</c> answers <see cref="ResolutionKind.Global"/>, although the real
/// function's parameter binds <c>a</c>: <see cref="ResolutionKind.Global"/> there means "bound neither by this piece nor
/// known to the parse".
/// So under <see cref="ParseGoal.FunctionBody"/> a <see cref="ResolutionKind.Static"/> answer naming an Annex B alias can
/// be wrong too, where the real parameters bind the name and withhold the alias. <i>(Added 2026-11-26, at SJL-15.2 stage
/// 5b.)</i>
/// </para>
/// <para>
/// <b>Annex B's block-function aliases</b> (Annex B.3.2; <see cref="SemanticModel.AnnexBAlias"/>): with Annex B on, a name
/// only a block function's alias binds is answered by the alias as the paragraphs above say, so that in
/// <c>var g; function f() { { function g() {} } return g; }</c> the reference answers <see cref="ResolutionKind.Static"/>
/// to f's <see cref="ScopeKind.Function"/> scope and its alias, not the Script's <c>var g</c>; a Script's references stay
/// <see cref="ResolutionKind.Global"/>. Eval code's alias is decided when the request carries the caller's records
/// (<see cref="InheritedContext.CallerScopes"/>), a Catch clause's record exempt. Without them the request does not
/// describe the caller, and the eval body alone decides, as <c>CallerScopes</c>' contract says. <i>(Revised 2026-11-26,
/// at SJL-15.2 stage 5c, when the caller's records joined the request, closing the interim; it read "What remains: the
/// interim, eval code's alias conditional on its caller's environments until stage 5c describes them; and" before the
/// run-time conditions, which stay as written.)</i> What remains: the run-time conditions, which no stage closes: a
/// Script's alias
/// depends on GlobalDeclarationInstantiation's checks, and eval code with a global variable environment on
/// GlobalDeclarationInstantiation-like checks. <i>(Revised 2026-11-26, at SJL-15.2 stage 5b, when the aliases joined the
/// model; it read "<b>Annex B.3.3's aliases</b> are stage 5b's: until then, with Annex B on, a name only a block
/// function's alias binds is answered as the scopes alone answer it, which may be a <see cref="ResolutionKind.Static"/>
/// answer naming an outer binding that the alias shadows at run time, as in
/// <c>var g; function f() { { function g() {} } return g; }</c>.")</i>
/// </para>
/// <para>
/// Whether a reference writes is not the model's: <see cref="Declaration.IsMutable"/> of the answer's declaration is the
/// mutability. Immutable, and safe to read concurrently (convention 10); no instance is shared by guarantee, and callers
/// compare by members.
/// </para>
/// </remarks>
// Hairs-AI:           Origin=AI; Spec=ECMA-262 ResolveBinding GetIdentifierReference, s10.2.11 FunctionDeclarationInstantiation, s19.2.1.1 PerformEval, s14.11.2 with statement, JSD-0080 reference resolution; IP=Low; Security=Low; Resources=1; Fingerprint=2CBC03
// Hairs-Human:        PENDING
public sealed class Resolution
{
    // Hairs-AI:           Origin=AI; Spec=JSD-0080 reference resolution; IP=Low; Security=Low; Resources=1; Fingerprint=07C870
    // Hairs-Human:        PENDING
    internal Resolution(ResolutionKind kind, Scope? scope, Declaration? declaration, IReadOnlyList<Scope> dynamicScopes, DynamicReason? reason, Resolution? fallback)
    {
        Kind = kind;
        Scope = scope;
        Declaration = declaration;
        DynamicScopes = dynamicScopes;
        Reason = reason;
        Fallback = fallback;
    }

    /// <summary>Its kind.</summary>
    /// <remarks>ECMA-262 at the pinned edition, clauses "ResolveBinding" and "GetIdentifierReference". <i>(Added 2026-11-27, at SJL-15.2 stage 8.)</i></remarks>
    public ResolutionKind Kind { get; }

    /// <summary>For <see cref="ResolutionKind.Static"/>, the scope whose environment holds the binding; null otherwise.</summary>
    /// <remarks>ECMA-262 at the pinned edition, clause "GetIdentifierReference" (the environment that holds the binding). <i>(Added 2026-11-27, at SJL-15.2 stage 8.)</i></remarks>
    public Scope? Scope { get; }

    /// <summary>
    /// For <see cref="ResolutionKind.Static"/>, the binding's first declaration in <see cref="Scope"/>, its
    /// <see cref="EnvironmentFacts.Arguments"/>, or the Annex B alias of a function's <see cref="ScopeKind.Function"/>
    /// scope; for <see cref="ResolutionKind.Inherited"/>, sloppy eval code's own var-scoped declaration of the name, the
    /// first, or, where it has none, its Annex B alias of the name, or null for a name eval code neither declares nor
    /// aliases, and null where a caller's record the request gives binds the name, for a reference, a binding the language
    /// resolves when it runs and a dynamic answer's fallback; null otherwise. The caller's records decide whether a caller's
    /// non-Catch lexical binding withholds an alias, and whether a caller's catch parameter stands before the binding;
    /// without them the carried declaration is the eval body's, as <see cref="InheritedContext.CallerScopes"/>' contract
    /// says. A caller's <c>with</c> object of the name can still stand before the binding, and a global variable
    /// environment's checks can still withhold an alias; both are run-time conditions that no stage closes. <i>(Revised
    /// 2026-11-26, at SJL-15.2 stage 5c, when the caller's records joined the request; it read "first, or, where it has
    /// none, its Annex B alias of the name, conditional on its caller's environments until stage 5c describes them, or null
    /// for a name eval code neither declares nor aliases; null otherwise.")</i> <i>(Revised 2026-11-26, at
    /// SJL-15.2 stage 5b; it read "For <see cref="ResolutionKind.Static"/>, the binding's first declaration in
    /// <see cref="Scope"/>, or its <see cref="EnvironmentFacts.Arguments"/>; for <see cref="ResolutionKind.Inherited"/>,
    /// sloppy eval code's own var-scoped declaration of the name, the first, or null for a name eval code does not declare;
    /// null otherwise.")</i>
    /// </summary>
    public Declaration? Declaration { get; }

    /// <summary>
    /// For <see cref="ResolutionKind.Dynamic"/>, the scopes that may bind the name at run time, innermost first: every
    /// <see cref="ScopeKind.With"/> scope, every scope a sloppy direct <c>eval</c> may add to, and, for the name
    /// <c>arguments</c>, every scope a block function's evaluation may give that binding, between the node and
    /// <see cref="Fallback"/>'s scope, or the end of the chain; empty otherwise. Never empty for a dynamic resolution.
    /// <i>(Revised 2026-11-26, at SJL-15.2 stage 5b; it read "every <see cref="ScopeKind.With"/> scope and every scope a
    /// sloppy direct <c>eval</c> may add to between the node and <see cref="Fallback"/>'s scope, or the end of the
    /// chain".)</i>
    /// </summary>
    /// <remarks>ECMA-262 at the pinned edition, clauses "Object Environment Records" and "PerformEval". <i>(Added 2026-11-27, at SJL-15.2 stage 8.)</i></remarks>
    public IReadOnlyList<Scope> DynamicScopes { get; }

    /// <summary>For <see cref="ResolutionKind.Dynamic"/>, the reason of the first <see cref="DynamicScopes"/> entry; null otherwise.</summary>
    /// <remarks>ECMA-262 at the pinned edition, clauses "The with Statement" and "PerformEval". <i>(Added 2026-11-27, at SJL-15.2 stage 8.)</i></remarks>
    public DynamicReason? Reason { get; }

    /// <summary>For <see cref="ResolutionKind.Dynamic"/>, the resolution when none of <see cref="DynamicScopes"/> binds the name, never itself dynamic; null otherwise.</summary>
    /// <remarks>ECMA-262 at the pinned edition, clause "GetIdentifierReference" (its [[OuterEnv]] walk). <i>(Added 2026-11-27, at SJL-15.2 stage 8.)</i></remarks>
    public Resolution? Fallback { get; }
}

/// <summary>
/// The environment facts of a program, a function, a static block or a field initialiser, as
/// <see cref="SemanticModel.EnvironmentOf"/> answers them (JSD-0080: "per-function facts"; milestone SJL-15.2, stage 5a;
/// rows FF3 and FF5): the facts that depend on the semantic model. The separate variable environment, row FF4, is
/// <see cref="FunctionFacts.HasSeparateVariableEnvironment"/>, which does not. <i>(Added 2026-11-26, at SJL-15.2 stage
/// 5a.)</i>
/// </summary>
/// <remarks>
/// <para>
/// <b>A direct-<c>eval</c> candidate</b> is a <see cref="CallExpression"/> that is not optional and whose callee, through
/// any number of <see cref="ParenthesizedExpression"/>s, is an <see cref="Identifier"/> named <c>eval</c>: the call's
/// evaluation reads a Reference whose name is <c>"eval"</c>, and the grouping operator keeps a Reference, so that
/// <c>(eval)(s)</c> is one; <c>eval?.(s)</c>, <c>(0, eval)(s)</c>, <c>new eval()</c>, a tagged template and a member call
/// are not. Every candidate only may be direct: SameValue with %eval% decides at run time. Its strictness is the call's own.
/// </para>
/// <para>
/// <b>Own code</b> is outside every nested function, arrow, method, field initialiser and static block, so that an arrow's
/// call is the arrow's, which has an environment of its own. <see cref="DirectEvalInParameters"/> and
/// <see cref="DirectEvalInBody"/> say whether this node's own environments may gain bindings; they do not say whether a
/// nested direct <c>eval</c> can read this node's bindings by name: in <c>function f() { var x; (() => eval("x = 1"))(); }</c>
/// f's <see cref="DirectEvalInBody"/> is false, yet the arrow's call reaches f's <c>x</c> at run time. That reach is a
/// fact of its own, <see cref="DirectEvalOrWithWithin"/>, beside captures (<see cref="SemanticModel.Captured"/>).
/// <i>(Revised 2026-11-27, at SJL-15.2 stage 7, when the reach joined the facts; it read "That reach is a later stage's,
/// with captures.")</i>
/// </para>
/// <para>
/// <b>The dynamic function's goals</b> are answered with the other piece empty, as <see cref="Resolution"/> says:
/// <see cref="Arguments"/>, <see cref="ArgumentsMapped"/> and <see cref="FunctionFacts.HasSeparateVariableEnvironment"/>
/// are computed with the parameters of the <see cref="ParseGoal.FunctionBody"/> goal empty too. Immutable, and safe to read
/// concurrently (convention 10).
/// </para>
/// </remarks>
// Hairs-AI:           Origin=AI; Spec=ECMA-262 s10.2.11 FunctionDeclarationInstantiation steps 15-22, call evaluation direct eval, s19.2.1.1 PerformEval, s14.11.2 with statement, JSD-0080 per-function facts; IP=Low; Security=Low; Resources=1; Fingerprint=B9E0FC
// Hairs-Human:        PENDING
public sealed class EnvironmentFacts
{
    // Hairs-AI:           Origin=AI; Spec=JSD-0080 per-function facts; IP=Low; Security=Low; Resources=1; Fingerprint=ED2B6C
    // Hairs-Human:        PENDING
    internal EnvironmentFacts(SyntaxNode node, Declaration? arguments, bool argumentsNeeded, bool argumentsMapped, bool directEvalInParameters, bool directEvalInBody, bool containsWith,
        bool directEvalOrWithWithin, bool functionNameReferenced)
    {
        Node = node;
        Arguments = arguments;
        ArgumentsNeeded = argumentsNeeded;
        ArgumentsMapped = argumentsMapped;
        DirectEvalInParameters = directEvalInParameters;
        DirectEvalInBody = directEvalInBody;
        ContainsWith = containsWith;
        DirectEvalOrWithWithin = directEvalOrWithWithin;
        FunctionNameReferenced = functionNameReferenced;
    }

    /// <summary>The node, as <see cref="FunctionFacts.Node"/>: the <see cref="ProgramNode"/>, a <see cref="FunctionNode"/>, a <see cref="StaticBlock"/> or a <see cref="FieldDefinition"/>.</summary>
    /// <remarks>ECMA-262 at the pinned edition, clause "FunctionDeclarationInstantiation". <i>(Added 2026-11-27, at SJL-15.2 stage 8.)</i></remarks>
    public SyntaxNode Node { get; }

    /// <summary>
    /// The <c>arguments</c> binding FunctionDeclarationInstantiation creates (ECMA-262 clause 10.2.11, steps 15 to 18 and
    /// 22), a <see cref="BindingKind.Arguments"/> declaration whose node is this one, in no scope's
    /// <see cref="Scope.Declarations"/>: for a function that is not an arrow, a static block (whose body FunctionDeclarationInstantiation
    /// instantiates with no parameters) and the dynamic function's goals, where no parameter is named <c>arguments</c> and,
    /// without parameter expressions, no top-level function declaration and no lexical declaration of the body is. Null for an
    /// arrow, a Script, a Module, eval code and every field initialiser, whose evaluation calls no
    /// FunctionDeclarationInstantiation ("Even though field initializers constitute a function boundary, calling
    /// FunctionDeclarationInstantiation does not have any observable effect and so is omitted").
    /// </summary>
    public Declaration? Arguments { get; }

    /// <summary>
    /// Whether the arguments object is needed (row FF3), the observable need rather than FunctionDeclarationInstantiation's
    /// argumentsObjectNeeded: <see cref="Arguments"/> is non-null and a reference resolves to it, or to a top-level
    /// <c>var</c> named <c>arguments</c> in this node's own <see cref="ScopeKind.Function"/> scope, which is the binding
    /// itself or is initialised from it, either as <see cref="ResolutionKind.Static"/> or as a dynamic resolution's
    /// <see cref="Resolution.Fallback"/>; or a direct-<c>eval</c> candidate of any strictness stands in this node's own code,
    /// in an arrow's parameters or body within it, or in a nested class's heritage, computed keys or decorators, since eval
    /// code may name <c>arguments</c>. Only references count, not a binding's write. It is reference-based, not
    /// flow-sensitive: <c>var arguments = 1; return arguments;</c> gives true.
    /// </summary>
    public bool ArgumentsNeeded { get; }

    /// <summary>
    /// Whether the needed arguments object is mapped: <see cref="ArgumentsNeeded"/>, not strict and a simple parameter list
    /// (FunctionDeclarationInstantiation steps 22.a and 22.b).
    /// </summary>
    public bool ArgumentsMapped { get; }

    /// <summary>Whether a direct-<c>eval</c> candidate made in non-strict code stands in this node's own parameters (row FF5); it implies <see cref="FunctionFacts.HasParameterExpressions"/>.</summary>
    /// <remarks>ECMA-262 at the pinned edition, clauses "FunctionDeclarationInstantiation" and "PerformEval". <i>(Added 2026-11-27, at SJL-15.2 stage 8.)</i></remarks>
    public bool DirectEvalInParameters { get; }

    /// <summary>Whether a direct-<c>eval</c> candidate made in non-strict code stands in this node's own body, outside its parameters (row FF5).</summary>
    /// <remarks>ECMA-262 at the pinned edition, clauses "FunctionDeclarationInstantiation" and "PerformEval". <i>(Added 2026-11-27, at SJL-15.2 stage 8.)</i></remarks>
    public bool DirectEvalInBody { get; }

    /// <summary>Whether a <c>with</c> statement stands in this node's own code, arrows excluded, as Contains crosses no function boundary; strict code refuses one.</summary>
    /// <remarks>ECMA-262 at the pinned edition, clauses "The with Statement" and "Static Semantics: Contains". <i>(Added 2026-11-27, at SJL-15.2 stage 8.)</i></remarks>
    public bool ContainsWith { get; }

    /// <summary>
    /// Whether a direct-<c>eval</c> candidate of any strictness, or a <c>with</c> statement, stands in this node's own code
    /// or in the code of any function, arrow, method, field initialiser or static block nested in it (JSD-0080: "free names
    /// and captures"; correspondence row FC1; the session's decision D-js-captures): code that may reach this node's
    /// bindings, or its free names' bindings, by a name its text does not hold, eval code reaching every enclosing binding
    /// by name and a <c>with</c> object standing before them. Transitive, and whatever the strictness, unlike
    /// <see cref="DirectEvalInParameters"/> and <see cref="DirectEvalInBody"/>, which say whether this node's own
    /// environments may gain bindings; a candidate is the one those define, so <c>(eval)(s)</c> counts and
    /// <c>eval?.(s)</c> does not. A <c>debugger</c> statement does not count: it gives no binding access.
    /// <i>(Added 2026-11-27, at SJL-15.2 stage 7.)</i>
    /// </summary>
    /// <remarks>ECMA-262 at the pinned edition, clauses "PerformEval" and "The with Statement". <i>(Added 2026-11-27, at SJL-15.2 stage 8.)</i></remarks>
    public bool DirectEvalOrWithWithin { get; }

    /// <summary>
    /// Whether this node is a named function expression whose own name, its <see cref="ScopeKind.FunctionName"/>
    /// scope's <see cref="BindingKind.FunctionName"/> declaration, a reference in its code or in code nested in it resolves
    /// to, directly or as a dynamic answer's <see cref="Resolution.Fallback"/> (JSD-0080: "free names and captures";
    /// correspondence row FC1; the session's decision D-js-captures): so in <c>var f = function g() { return () => g; }</c>
    /// it is true for <c>g</c> and false for the arrow. False for every other node. The name is never among this node's
    /// own <see cref="SemanticModel.FreeNames"/>, its scope being the function's own, though it is among those of a
    /// function nested in it that uses it, as the arrow's are. A use only in a direct <c>eval</c>'s text is no reference
    /// of the tree and does not count: in <c>var f = function g() { return eval("g"); }</c> it is false, and
    /// <see cref="DirectEvalOrWithWithin"/> is true. <i>(Added 2026-11-27, at SJL-15.2 stage 7.)</i>
    /// </summary>
    /// <remarks>ECMA-262 at the pinned edition, clause "Runtime Semantics: InstantiateOrdinaryFunctionExpression" (its funcEnv). <i>(Added 2026-11-27, at SJL-15.2 stage 8.)</i></remarks>
    public bool FunctionNameReferenced { get; }
}

/// <summary>
/// A Module's record, as <see cref="SemanticModel.Module"/> answers it (JSD-0080: "the module record: requests with
/// attributes, top-level <c>await</c>, import and export entries, and their errors"; milestone SJL-15.2, stage 6; the
/// session's decision D-js-module-record): ParseModule's lists of the Source Text Module Record, [[RequestedModules]],
/// [[ImportEntries]], [[LocalExportEntries]], [[IndirectExportEntries]] and [[StarExportEntries]], and its
/// [[HasTLA]]. <i>(Added 2026-11-27, at SJL-15.2 stage 6.)</i>
/// </summary>
/// <remarks>
/// <para>
/// <b>In source order</b> (JSD-0080: "Every collection is in source order"): the requests by their first statement, the
/// entries by their node, so that ParseModule's step 10 partition keeps the order of the ExportEntries it reads and a
/// rewritten entry stands among the true indirect ones where its export does. Every list is built with the model and
/// read-only.
/// </para>
/// <para>
/// <b>Shared instances.</b> Every entry that names a request holds the element of <see cref="RequestedModules"/> itself, one
/// instance per distinct request, where the specification builds a value-equal record for each declaration; an import
/// entry's <see cref="ImportEntry.Declaration"/> is the instance <see cref="SemanticModel.DeclarationOf"/> answers for its
/// node, and a local export entry's the module scope's first declaration of its local name. Immutable, and safe to read
/// concurrently (convention 10).
/// </para>
/// </remarks>
// Hairs-AI:           Origin=AI; Spec=ECMA-262 ParseModule, Source Text Module Records, JSD-0080 module record; IP=Low; Security=Low; Resources=1; Fingerprint=6A4FC1
// Hairs-Human:        PENDING
public sealed class ModuleRecord
{
    // Hairs-AI:           Origin=AI; Spec=JSD-0080 module record; IP=Low; Security=Low; Resources=1; Fingerprint=C55CAA
    // Hairs-Human:        PENDING
    internal ModuleRecord(IReadOnlyList<ModuleRequest> requestedModules, IReadOnlyList<ImportEntry> importEntries, IReadOnlyList<ExportEntry> localExportEntries, IReadOnlyList<ExportEntry> indirectExportEntries, IReadOnlyList<ExportEntry> starExportEntries, bool hasTopLevelAwait)
    {
        RequestedModules = requestedModules;
        ImportEntries = importEntries;
        LocalExportEntries = localExportEntries;
        IndirectExportEntries = indirectExportEntries;
        StarExportEntries = starExportEntries;
        HasTopLevelAwait = hasTopLevelAwait;
    }

    /// <summary>
    /// [[RequestedModules]], ModuleRequests of the module's items (ParseModule step 3): a request for each distinct request,
    /// "in source text occurrence order of the imports", that is, in the order of its first statement, a later statement
    /// equal to an earlier one by ModuleRequestsEqual, with the phase added, making none of its own. Every
    /// <see cref="ImportDeclaration"/>, <c>import "m"</c> and <c>import {} from "m"</c> among them, and every
    /// <c>export … from</c>, <c>export {} from "m"</c> among them, makes a request; an <see cref="ImportCall"/> makes none.
    /// </summary>
    public IReadOnlyList<ModuleRequest> RequestedModules { get; }

    /// <summary>
    /// [[ImportEntries]], ImportEntries of the module's items (ParseModule step 4), in source order: per
    /// <see cref="ImportDeclaration"/>, its default binding's entry, then its namespace binding's or its named imports'.
    /// <c>import "m"</c> and <c>import {} from "m"</c> give none.
    /// </summary>
    public IReadOnlyList<ImportEntry> ImportEntries { get; }

    /// <summary>
    /// [[LocalExportEntries]] (ParseModule step 10.a.i.1): each export entry with no request whose local name no import entry
    /// binds, in source order; <c>export default a</c> with <c>a</c> imported among them, its local name being
    /// <c>*default*</c>.
    /// </summary>
    public IReadOnlyList<ExportEntry> LocalExportEntries { get; }

    /// <summary>
    /// [[IndirectExportEntries]], in source order: each <c>export … from</c> entry but <c>export *</c>'s (step 10.c.i), an
    /// <c>export * as n from</c> entry among them, and each export of an imported binding, rewritten to its import's request:
    /// with <see cref="ImportNameKind.All"/> for a namespace import (step 10.a.ii.3.b) and with the import's kind and name
    /// for any other (step 10.a.ii.4.b).
    /// </summary>
    public IReadOnlyList<ExportEntry> IndirectExportEntries { get; }

    /// <summary>[[StarExportEntries]] (step 10.b.ii): each <c>export * from</c> entry, in source order.</summary>
    public IReadOnlyList<ExportEntry> StarExportEntries { get; }

    /// <summary>
    /// [[HasTLA]], "Whether this module is individually asynchronous" (ParseModule steps 11 and 12: the module's items
    /// Contains <c>await</c>): <see cref="ProgramNode.HasTopLevelAwait"/>, the tree's fact, read and not recomputed. It is
    /// set for an <c>await</c> expression, a <c>for await</c> and an <c>await using</c> declaration outside every function,
    /// arrow, method, field initialiser and static block, a class's heritage and computed keys and an object literal's
    /// computed keys included, as Contains looks into them; an <c>await</c> in a top-level async arrow does not set it. An
    /// <c>await using</c> declaration is ecma262 main's, beyond the pinned edition, and an <c>await</c> in a top-level
    /// decorator's expression, under <see cref="ProposalFlags.Decorators"/>, is the tree's reading, beyond the edition; whether
    /// the decorators proposal counts it is not settled.
    /// </summary>
    public bool HasTopLevelAwait { get; }
}

/// <summary>
/// A ModuleRequest Record, as <see cref="ModuleRecord.RequestedModules"/> holds it (ECMA-262 ModuleRequest Records:
/// [[Specifier]] and [[Attributes]]; milestone SJL-15.2, stage 6; the session's decision D-js-module-record): the specifier,
/// the attributes, the phase, and the statements that make it. <i>(Added 2026-11-27, at SJL-15.2 stage 6.)</i>
/// </summary>
/// <remarks>
/// Two statements make one request where ModuleRequestsEqual holds of them: the same specifier, compared ordinally, and the
/// same attributes as a set, by count and then by each key and value in any order; and, the library's addition, beyond the
/// pinned edition, whose ModuleRequest Record has no phase, the same <see cref="Phase"/>. Immutable, and safe to read
/// concurrently (convention 10).
/// </remarks>
// Hairs-AI:           Origin=AI; Spec=ECMA-262 ModuleRequest Records, ModuleRequests, ModuleRequestsEqual, WithClauseToAttributes, JSD-0080 module record; IP=Low; Security=Low; Resources=1; Fingerprint=7167BC
// Hairs-Human:        PENDING
public sealed class ModuleRequest
{
    // Hairs-AI:           Origin=AI; Spec=JSD-0080 module record; IP=Low; Security=Low; Resources=1; Fingerprint=60C30F
    // Hairs-Human:        PENDING
    internal ModuleRequest(string specifier, IReadOnlyList<ModuleRequestAttribute> attributes, ImportPhase phase, IReadOnlyList<Statement> statements)
    {
        Specifier = specifier;
        Attributes = attributes;
        Phase = phase;
        Statements = statements;
    }

    /// <summary>
    /// [[Specifier]], the SV of the ModuleSpecifier (<see cref="StringLiteral.Value"/>), as written, lone surrogates kept, so
    /// that <c>"m"</c>, <c>'m'</c> and <c>"\u006d"</c> are one specifier; an engine refuses what it cannot load.
    /// </summary>
    public string Specifier { get; }

    /// <summary>
    /// The WithClause's attributes of <see cref="Statements"/>' first statement, in that statement's source order (JSD-0080:
    /// "Every collection is in source order"), each key its AttributeKey's PropName, so that <c>type</c> and <c>"type"</c>
    /// are one key, and each value its SV; empty for <c>with {}</c> and for no <c>with</c>. The specification's
    /// [[Attributes]] is this list sorted by <see cref="ModuleRequestAttribute.Key"/> under
    /// <see cref="StringComparer.Ordinal"/>, UTF-16 code unit by code unit (WithClauseToAttributes), whose NOTE reads: "This
    /// sorting is observable only in that hosts are prohibited from changing behaviour based on the order in which
    /// attributes are enumerated". The keys of an accepted source are distinct (EE0180), so that sort is a total order. A
    /// later statement that makes the same request may write the same attributes in another order; its own order is its
    /// statement's <c>Attributes</c>, which <see cref="Statements"/> reaches.
    /// </summary>
    public IReadOnlyList<ModuleRequestAttribute> Attributes { get; }

    /// <summary>
    /// The phase the request is made in: an <see cref="ImportDeclaration"/>'s <see cref="ImportDeclaration.Phase"/>, and
    /// <see cref="ImportPhase.Evaluation"/> for every <c>export … from</c>. The pinned edition's ModuleRequest Record has no
    /// phase; the source-phase and deferred imports are proposals (<see cref="ProposalFlags.ImportSourceDeclarations"/>,
    /// <see cref="ProposalFlags.ImportDeferDeclarations"/>), and the field is the library's, beyond the edition.
    /// </summary>
    /// <remarks>ECMA-262 at the pinned edition, clauses "ModuleRequest Records" and "Static Semantics: ModuleRequests". <i>(Added 2026-11-27, at SJL-15.2 stage 8.)</i></remarks>
    public ImportPhase Phase { get; }

    /// <summary>
    /// Every <see cref="ImportDeclaration"/>, <see cref="ExportNamedDeclaration"/> with a FromClause and
    /// <see cref="ExportAllDeclaration"/> that makes this request, in source order, the first making it.
    /// </summary>
    /// <remarks>ECMA-262 at the pinned edition, clause "Static Semantics: ModuleRequests". <i>(Added 2026-11-27, at SJL-15.2 stage 8.)</i></remarks>
    public IReadOnlyList<Statement> Statements { get; }
}

/// <summary>
/// An ImportAttribute Record, as <see cref="ModuleRequest.Attributes"/> holds it (ECMA-262: [[Key]] and [[Value]], both
/// Strings): an attribute's key and value. <i>(Added 2026-11-27, at SJL-15.2 stage 6.)</i>
/// </summary>
/// <remarks>Immutable, and safe to read concurrently (convention 10).</remarks>
// Hairs-AI:           Origin=AI; Spec=ECMA-262 ImportAttribute Records, WithClauseToAttributes, JSD-0080 module record; IP=Low; Security=Low; Resources=0; Fingerprint=164001
// Hairs-Human:        PENDING
public sealed class ModuleRequestAttribute
{
    // Hairs-AI:           Origin=AI; Spec=JSD-0080 module record; IP=Low; Security=Low; Resources=0; Fingerprint=25140F
    // Hairs-Human:        PENDING
    internal ModuleRequestAttribute(string key, string value)
    {
        Key = key;
        Value = value;
    }

    /// <summary>[[Key]], the PropName of the AttributeKey: an <see cref="IdentifierName"/>'s name or a <see cref="StringLiteral"/>'s SV.</summary>
    public string Key { get; }

    /// <summary>[[Value]], the SV of the attribute's <see cref="StringLiteral"/>.</summary>
    public string Value { get; }
}

/// <summary>
/// What an import entry's [[ImportName]] or an export entry's [[ImportName]] is (ECMA-262 ImportEntry and ExportEntry
/// Records), in the specification's spellings, as <see cref="ImportEntry.ImportKind"/> and
/// <see cref="ExportEntry.ImportKind"/>. <i>(Added 2026-11-27, at SJL-15.2 stage 6.)</i>
/// </summary>
/// <remarks>The numbers are stable; a later member takes a new number.</remarks>
// Hairs-AI:           Origin=AI; Spec=ECMA-262 ImportEntry Records ExportEntry Records ImportName, JSD-0080 module record; IP=Low; Security=Low; Resources=0; Fingerprint=ECF5E7
// Hairs-Human:        PENDING
public enum ImportNameKind
{
    /// <summary>A null [[ImportName]]: a local export entry's.</summary>
    None = 0,

    /// <summary>A String [[ImportName]], the entry's <c>ImportName</c>: a named or default import, or a re-export of one.</summary>
    Name = 1,

    /// <summary>~namespace-object~: an import entry's namespace binding, <c>import * as ns</c>, a deferred one among them.</summary>
    /// <remarks>ECMA-262 at the pinned edition, clause "ImportEntry Records" (~namespace-object~). <i>(Added 2026-11-27, at SJL-15.2 stage 8.)</i></remarks>
    NamespaceObject = 2,

    /// <summary>~all~: an export entry's <c>export * as n from</c>, or the re-export of an imported namespace object.</summary>
    /// <remarks>ECMA-262 at the pinned edition, clause "ExportEntry Records" (~all~). <i>(Added 2026-11-27, at SJL-15.2 stage 8.)</i></remarks>
    All = 3,

    /// <summary>~all-but-default~: an export entry's <c>export * from</c>.</summary>
    /// <remarks>ECMA-262 at the pinned edition, clause "ExportEntry Records" (~all-but-default~). <i>(Added 2026-11-27, at SJL-15.2 stage 8.)</i></remarks>
    AllButDefault = 4,

    /// <summary>
    /// A source-phase binding, <c>import source x from "m"</c> under <see cref="ProposalFlags.ImportSourceDeclarations"/>,
    /// and an export of one: the library's kind, beyond the pinned edition, whose rule for an ImportedDefaultBinding would
    /// name the default export, which the binding is not. The entry's <c>ImportName</c> is null.
    /// </summary>
    Source = 5,
}

/// <summary>
/// An ImportEntry Record, as <see cref="ModuleRecord.ImportEntries"/> holds it (ECMA-262: [[ModuleRequest]],
/// [[ImportName]] and [[LocalName]]; milestone SJL-15.2, stage 6), with its binding and its declaration.
/// <i>(Added 2026-11-27, at SJL-15.2 stage 6.)</i>
/// </summary>
/// <remarks>
/// A default binding is <c>{request, Name, "default", local}</c>; a namespace binding <c>{request, NamespaceObject, null,
/// ns}</c>, a deferred one's request with <see cref="ImportPhase.Defer"/>; a named import <c>{request, Name, the StringValue
/// of the name before <c>as</c> or of the binding, local}</c>; and a source-phase binding <c>{request, Source, null,
/// local}</c>, never <c>"default"</c>. Immutable, and safe to read concurrently (convention 10).
/// </remarks>
// Hairs-AI:           Origin=AI; Spec=ECMA-262 ImportEntry Records, ImportEntriesForModule, JSD-0080 module record; IP=Low; Security=Low; Resources=1; Fingerprint=36244F
// Hairs-Human:        PENDING
public sealed class ImportEntry
{
    // Hairs-AI:           Origin=AI; Spec=JSD-0080 module record; IP=Low; Security=Low; Resources=1; Fingerprint=3B41E6
    // Hairs-Human:        PENDING
    internal ImportEntry(ModuleRequest moduleRequest, ImportNameKind importKind, string? importName, string localName, BindingIdentifier node, Declaration declaration)
    {
        ModuleRequest = moduleRequest;
        ImportKind = importKind;
        ImportName = importName;
        LocalName = localName;
        Node = node;
        Declaration = declaration;
    }

    /// <summary>[[ModuleRequest]], the element of <see cref="ModuleRecord.RequestedModules"/> itself.</summary>
    public ModuleRequest ModuleRequest { get; }

    /// <summary>What [[ImportName]] is: <see cref="ImportNameKind.Name"/>, <see cref="ImportNameKind.NamespaceObject"/> or <see cref="ImportNameKind.Source"/>.</summary>
    public ImportNameKind ImportKind { get; }

    /// <summary>[[ImportName]] where it is a String, non-null exactly for <see cref="ImportNameKind.Name"/>.</summary>
    public string? ImportName { get; }

    /// <summary>[[LocalName]], the StringValue of the ImportedBinding.</summary>
    public string LocalName { get; }

    /// <summary>The ImportedBinding.</summary>
    /// <remarks>ECMA-262 at the pinned edition, clauses "ImportEntry Records" and "Static Semantics: ImportEntriesForModule". <i>(Added 2026-11-27, at SJL-15.2 stage 8.)</i></remarks>
    public BindingIdentifier Node { get; }

    /// <summary>The <see cref="BindingKind.Import"/> declaration the binding makes, the instance <see cref="SemanticModel.DeclarationOf"/> answers for <see cref="Node"/>.</summary>
    /// <remarks>ECMA-262 at the pinned edition, clauses "ImportEntry Records" and "Static Semantics: BoundNames". <i>(Added 2026-11-27, at SJL-15.2 stage 8.)</i></remarks>
    public Declaration Declaration { get; }
}

/// <summary>
/// An ExportEntry Record, as the export lists of <see cref="ModuleRecord"/> hold it (ECMA-262: [[ExportName]],
/// [[ModuleRequest]], [[ImportName]] and [[LocalName]]; milestone SJL-15.2, stage 6), with its node and, for a local entry,
/// its declaration. <i>(Added 2026-11-27, at SJL-15.2 stage 6.)</i>
/// </summary>
/// <remarks>
/// <para>
/// Per form (ExportEntriesForModule): an exported declaration gives a local entry per BoundName, in order, each named as
/// it is bound (<c>export var a, a;</c> would give two, and EE0170 refuses it); <c>export default</c> gives <c>{"default", null, None, null,
/// local}</c>, the local name <c>*default*</c> for an anonymous function, generator, async function, async generator or
/// class and for an expression, and the declared name otherwise; a specifier without a FromClause <c>{exported or local,
/// null, None, null, local}</c>; one with a FromClause <c>{exported or local, request, Name, local, null}</c>; <c>export *
/// from</c> <c>{null, request, AllButDefault, null, null}</c>; and <c>export * as n from</c> <c>{n, request, All, null,
/// null}</c>. Every name is a StringValue: an <see cref="IdentifierName"/>'s name or a <see cref="StringLiteral"/>'s SV.
/// </para>
/// <para>
/// <b>The node</b> is the <see cref="ExportSpecifier"/> for a clause's entry, rewritten or not, the
/// <see cref="BindingIdentifier"/> of each bound name for an exported declaration, the
/// <see cref="ExportDefaultDeclaration"/>, or the <see cref="ExportAllDeclaration"/>. An export of an imported binding is
/// rewritten as ParseModule's step 10.a.ii rewrites it, its import's request and kind taken; an export of a source-phase
/// binding takes <see cref="ImportNameKind.Source"/>, which applies the edition's step to the proposal's value, the
/// proposal's own rule not being pinned. Immutable, and safe to read concurrently (convention 10).
/// </para>
/// </remarks>
// Hairs-AI:           Origin=AI; Spec=ECMA-262 ExportEntry Records, ExportEntriesForModule, ParseModule step 10, JSD-0080 module record; IP=Low; Security=Low; Resources=1; Fingerprint=B81701
// Hairs-Human:        PENDING
public sealed class ExportEntry
{
    // Hairs-AI:           Origin=AI; Spec=JSD-0080 module record; IP=Low; Security=Low; Resources=1; Fingerprint=D4B5FF
    // Hairs-Human:        PENDING
    internal ExportEntry(string? exportName, ModuleRequest? moduleRequest, ImportNameKind importKind, string? importName, string? localName, SyntaxNode node, Declaration? declaration)
    {
        ExportName = exportName;
        ModuleRequest = moduleRequest;
        ImportKind = importKind;
        ImportName = importName;
        LocalName = localName;
        Node = node;
        Declaration = declaration;
    }

    /// <summary>[[ExportName]], null only for <c>export * from</c>.</summary>
    public string? ExportName { get; }

    /// <summary>[[ModuleRequest]], the element of <see cref="ModuleRecord.RequestedModules"/> itself; null for a local entry.</summary>
    public ModuleRequest? ModuleRequest { get; }

    /// <summary>
    /// What [[ImportName]] is: <see cref="ImportNameKind.None"/> for a local entry, <see cref="ImportNameKind.Name"/>,
    /// <see cref="ImportNameKind.All"/> or <see cref="ImportNameKind.AllButDefault"/>, or <see cref="ImportNameKind.Source"/>
    /// for a rewritten export of a source-phase binding.
    /// </summary>
    public ImportNameKind ImportKind { get; }

    /// <summary>[[ImportName]] where it is a String, non-null exactly for <see cref="ImportNameKind.Name"/>.</summary>
    public string? ImportName { get; }

    /// <summary>[[LocalName]], non-null exactly for a local entry.</summary>
    public string? LocalName { get; }

    /// <summary>The entry's node: the <see cref="ExportSpecifier"/>, the bound name's <see cref="BindingIdentifier"/>, the <see cref="ExportDefaultDeclaration"/> or the <see cref="ExportAllDeclaration"/>.</summary>
    /// <remarks>ECMA-262 at the pinned edition, clauses "ExportEntry Records" and "Static Semantics: ExportEntriesForModule". <i>(Added 2026-11-27, at SJL-15.2 stage 8.)</i></remarks>
    public SyntaxNode Node { get; }

    /// <summary>
    /// A local entry's binding: the module scope's first declaration of <see cref="LocalName"/> in its
    /// <see cref="Scope.Declarations"/>' order, the first in source order, and for <c>*default*</c> the
    /// <see cref="BindingKind.ExportDefault"/> declaration of the <see cref="ExportDefaultDeclaration"/>; every local entry
    /// of an accepted Module has one, the rest being refused (EE0171). Null for an indirect or star entry.
    /// </summary>
    /// <remarks>ECMA-262 at the pinned edition, clauses "ExportEntry Records" and "Static Semantics: ExportedBindings". <i>(Added 2026-11-27, at SJL-15.2 stage 8.)</i></remarks>
    public Declaration? Declaration { get; }
}
