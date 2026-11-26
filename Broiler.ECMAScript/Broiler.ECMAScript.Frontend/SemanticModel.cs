// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// HAIRS Code Assurance
// --------------------
// Relevant units:   24
// Annotated:        24/24
// Exempt:           63
// Human-reviewed:   0/24
// IP risk:          Low
// Security risk:    Medium
// Criteria:         0/0
// Resource impact:  1/10 max
// Unverified:       24
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
/// the program, every function, every static block and every initialised field (<see cref="EnvironmentOf"/>); Annex
/// B.3.3's aliases, the caller's names of eval code, the module record, named evaluation, captures and the early
/// errors that are not purely grammatical are later stages'. From stage 2 the walk that builds it also reports its
/// early errors of the function context (the super rows, ContainsArguments, eval code's from the caller's context, and
/// three of expressions') among the parse's diagnostics, so that a source they refuse has no model, from stage 3 those
/// of jump targets (a <c>break</c> or a <c>continue</c> with no target, and an undefined break or continue target), and
/// from stage 4 those of private names (a private name no enclosing class declares, in a Script, in a Module, and in
/// eval code against the private names its caller sees); the early errors of the module record are later stages'.
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
/// <b>A node of another tree is a caller's error</b> (convention 3): <see cref="ScopeOf"/>, <see cref="DeclarationOf"/>,
/// <see cref="FactsOf"/>, both <see cref="TargetOf(BreakStatement)"/> overloads, <see cref="PrivateEnvironmentOf"/> and
/// <see cref="Resolve(PrivateIdentifier)"/> throw <see cref="ArgumentException"/>
/// for a node that is not in the model's tree, and <see cref="ParentOf"/> for a scope that is not one of the model's.
/// So do the <see cref="Resolve(Identifier)"/> and <see cref="Resolve(BindingIdentifier)"/> overloads and
/// <see cref="EnvironmentOf"/>. <i>(Added 2026-11-26, at SJL-15.2 stage 5a.)</i>
/// <i>(Revised 2026-11-25, at SJL-15.2 stage 3, when <c>TargetOf</c> joined them; it read "<see cref="ScopeOf"/>,
/// <see cref="DeclarationOf"/> and <see cref="FactsOf"/> throw <see cref="ArgumentException"/> for a node that is not in
/// the model's tree".)</i> <i>(Revised 2026-11-25, at SJL-15.2 stage 4, when <c>PrivateEnvironmentOf</c> and
/// <c>Resolve</c> joined them; it read "<see cref="FactsOf"/> and both <see cref="TargetOf(BreakStatement)"/> overloads
/// throw".)</i>
/// </para>
/// <para><b>Immutable, and safe to call concurrently</b> (convention 10).</para>
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

    // Hairs-AI:           Origin=AI; Spec=JSD-0080 semantic model design; IP=Low; Security=Low; Resources=1; Fingerprint=D347C4
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
        Dictionary<SyntaxNode, EnvironmentFacts> environmentOf)
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
    }

    /// <summary>
    /// Every scope, in the source order of its node's start, a scope before the scopes inside it (JSD-0080: "the scopes in
    /// source order"): those of <see cref="DeclarationTables.Scopes"/>, the same instances in the same relative order, and
    /// the model's own. The first is the program's: its <see cref="ScopeKind.Script"/> or <see cref="ScopeKind.Module"/>
    /// scope, or for a dynamic function's goals its <see cref="ScopeKind.Parameters"/> scope.
    /// </summary>
    public IReadOnlyList<Scope> Scopes { get; }

    /// <summary>
    /// The program's, every function's, every class static block's and every initialised field's facts, in the source
    /// order of their node's start: those of <see cref="DeclarationTables.Functions"/>, the same instances in the same
    /// relative order, and the model's own, of kinds <see cref="FunctionKind.StaticBlock"/> and
    /// <see cref="FunctionKind.FieldInitializer"/>. A field without an initialiser has none.
    /// </summary>
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
    /// (<see cref="ParseSwitches.AnnexB"/>), a name that only a block function's Annex B.3.3 alias binds is answered as the
    /// scopes alone answer it until stage 5b: <see cref="ResolutionKind.Global"/>, or a <see cref="ResolutionKind.Static"/>
    /// or <see cref="ResolutionKind.Dynamic"/> answer naming an outer binding, which the alias shadows at run time. Beside
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
    public Statement Statement { get; }

    /// <summary>
    /// The statement's own label set (LabelledEvaluation's labelSet): the labels of the chain of
    /// <see cref="LabelledStatement"/>s directly over it, outermost first, so that in <c>A: B: C: do ... while (c)</c> it is
    /// [A, B, C]; empty when it has none.
    /// </summary>
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
    Field = 0,

    /// <summary>A private method: a <see cref="MethodDefinition"/> of <see cref="MethodKind.Method"/>, plain, generator, async or async generator.</summary>
    Method = 1,

    /// <summary>
    /// A private accessor: a getter, a setter, a getter and setter pair of one placement, or an auto-accessor
    /// (<c>accessor #x</c>, the auto-accessors proposal, <see cref="ProposalFlags.AutoAccessors"/>).
    /// </summary>
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
    public PrivateEnvironment Environment { get; }

    /// <summary>Whether it is a field, a method or an accessor.</summary>
    public PrivateNameKind Kind { get; }

    /// <summary>Whether its elements are <c>static</c>. A static name used through an instance, or the other way round, is the brand check's run-time error, not an early one.</summary>
    public bool IsStatic { get; }

    /// <summary>
    /// The class elements that declare it, each a <see cref="MethodDefinition"/> or a <see cref="FieldDefinition"/>, in source
    /// order: one, or a getter and a setter.
    /// </summary>
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
    Static = 0,

    /// <summary>
    /// To a binding one of <see cref="Resolution.DynamicScopes"/> may hold at run time, a <c>with</c> statement's object or
    /// a sloppy direct <c>eval</c>'s <c>var</c>, and otherwise to <see cref="Resolution.Fallback"/>.
    /// </summary>
    Dynamic = 1,

    /// <summary>
    /// To no binding the unit declares or the parse knows: GetIdentifierReference ends at the global environment, so that a read throws a ReferenceError unless the global object holds the name, <c>typeof</c> gives
    /// <c>"undefined"</c>, and a sloppy write creates a global. Only Script, Module and the dynamic function's goals answer
    /// it.
    /// </summary>
    Global = 2,

    /// <summary>
    /// To the environments eval code inherits, which the request does not describe (<see cref="InheritedContext"/>): the
    /// caller's chain for a direct <c>eval</c>, the global environment for an indirect one, which the request does not tell
    /// apart. Only eval code answers it: a name eval code does not declare, and a var-scoped declaration of sloppy eval
    /// code, whose binding PerformEval creates in the caller's VariableEnvironment (step 16.b).
    /// </summary>
    Inherited = 3,
}

/// <summary>
/// Why a <see cref="ResolutionKind.Dynamic"/> resolution is dynamic: the reason of its innermost
/// <see cref="Resolution.DynamicScopes"/> entry. <i>(Added 2026-11-26, at SJL-15.2 stage 5a.)</i>
/// </summary>
/// <remarks>The numbers are stable; a later member takes a new number.</remarks>
// Hairs-AI:           Origin=AI; Spec=ECMA-262 s14.11.2 with statement NewObjectEnvironment, s19.2.1.1 PerformEval step 16.b, JSD-0080 dynamic markers; IP=Low; Security=Low; Resources=0; Fingerprint=27A2DE
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
/// </para>
/// <para>
/// <b>A scope binds</b> its <see cref="Scope.Declarations"/> by name; a <see cref="ScopeKind.Parameters"/> scope also binds
/// its function's <c>arguments</c> where <see cref="EnvironmentFacts.Arguments"/> is non-null
/// (FunctionDeclarationInstantiation step 22), as a <see cref="ScopeKind.StaticBlock"/> scope binds its block's. A Script's
/// declarations are <see cref="ResolutionKind.Static"/> to the Script scope, its lexical ones in the global declarative
/// record and the others in the object record (GlobalDeclarationInstantiation); storage stays each engine's.
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
/// <c>EnvironmentOf(entry.Node).DirectEvalInParameters</c>. Each entry's own reason follows from its kind.
/// </para>
/// <para>
/// <b>Eval code.</b> The request carries no caller's names and does not tell a direct <c>eval</c> from an indirect one, so
/// a name eval code does not declare is <see cref="ResolutionKind.Inherited"/>, with no declaration; a var-scoped
/// declaration of sloppy eval code (a top-level <c>var</c> or function declaration) is
/// <see cref="ResolutionKind.Inherited"/> with that declaration and no scope, since its binding is created in the caller's
/// VariableEnvironment (PerformEval step 16.b), before which a caller's <c>with</c> object or <c>catch</c> parameter of the
/// name can stand; strict eval code's declarations, whose VariableEnvironment is its own (step 18), and sloppy eval code's
/// lexical ones are <see cref="ResolutionKind.Static"/>. The caller's names are a later stage's.
/// </para>
/// <para>
/// <b>The dynamic function's goals</b> (CreateDynamicFunction, whose function's scope is the realm's global environment):
/// each goal is answered with the other piece empty, so that under <see cref="ParseGoal.FunctionBody"/> <c>return a</c> in
/// the body of <c>new Function("a", "return a")</c> answers <see cref="ResolutionKind.Global"/>, although the real
/// function's parameter binds <c>a</c>: <see cref="ResolutionKind.Global"/> there means "bound neither by this piece nor
/// known to the parse".
/// </para>
/// <para>
/// <b>Annex B.3.3's aliases</b> are stage 5b's: until then, with Annex B on, a name only a block function's alias binds
/// is answered as the scopes alone answer it, which may be a <see cref="ResolutionKind.Static"/> answer naming an outer
/// binding that the alias shadows at run time, as in <c>var g; function f() { { function g() {} } return g; }</c>.
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
    public ResolutionKind Kind { get; }

    /// <summary>For <see cref="ResolutionKind.Static"/>, the scope whose environment holds the binding; null otherwise.</summary>
    public Scope? Scope { get; }

    /// <summary>
    /// For <see cref="ResolutionKind.Static"/>, the binding's first declaration in <see cref="Scope"/>, or its
    /// <see cref="EnvironmentFacts.Arguments"/>; for <see cref="ResolutionKind.Inherited"/>, sloppy eval code's own
    /// var-scoped declaration of the name, the first, or null for a name eval code does not declare; null otherwise.
    /// </summary>
    public Declaration? Declaration { get; }

    /// <summary>
    /// For <see cref="ResolutionKind.Dynamic"/>, the scopes that may bind the name at run time, innermost first: every
    /// <see cref="ScopeKind.With"/> scope and every scope a sloppy direct <c>eval</c> may add to between the node and
    /// <see cref="Fallback"/>'s scope, or the end of the chain; empty otherwise. Never empty for a dynamic resolution.
    /// </summary>
    public IReadOnlyList<Scope> DynamicScopes { get; }

    /// <summary>For <see cref="ResolutionKind.Dynamic"/>, the reason of the first <see cref="DynamicScopes"/> entry; null otherwise.</summary>
    public DynamicReason? Reason { get; }

    /// <summary>For <see cref="ResolutionKind.Dynamic"/>, the resolution when none of <see cref="DynamicScopes"/> binds the name, never itself dynamic; null otherwise.</summary>
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
/// later stage's, with captures.
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
    // Hairs-AI:           Origin=AI; Spec=JSD-0080 per-function facts; IP=Low; Security=Low; Resources=1; Fingerprint=EC3BFD
    // Hairs-Human:        PENDING
    internal EnvironmentFacts(SyntaxNode node, Declaration? arguments, bool argumentsNeeded, bool argumentsMapped, bool directEvalInParameters, bool directEvalInBody, bool containsWith)
    {
        Node = node;
        Arguments = arguments;
        ArgumentsNeeded = argumentsNeeded;
        ArgumentsMapped = argumentsMapped;
        DirectEvalInParameters = directEvalInParameters;
        DirectEvalInBody = directEvalInBody;
        ContainsWith = containsWith;
    }

    /// <summary>The node, as <see cref="FunctionFacts.Node"/>: the <see cref="ProgramNode"/>, a <see cref="FunctionNode"/>, a <see cref="StaticBlock"/> or a <see cref="FieldDefinition"/>.</summary>
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
    public bool DirectEvalInParameters { get; }

    /// <summary>Whether a direct-<c>eval</c> candidate made in non-strict code stands in this node's own body, outside its parameters (row FF5).</summary>
    public bool DirectEvalInBody { get; }

    /// <summary>Whether a <c>with</c> statement stands in this node's own code, arrows excluded, as Contains crosses no function boundary; strict code refuses one.</summary>
    public bool ContainsWith { get; }
}
