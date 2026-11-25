// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// HAIRS Code Assurance
// --------------------
// Relevant units:   9
// Annotated:        9/9
// Exempt:           24
// Human-reviewed:   0/9
// IP risk:          Low
// Security risk:    Medium
// Criteria:         0/0
// Resource impact:  1/10 max
// Unverified:       9
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
/// each <c>break</c>'s and <c>continue</c>'s target (<see cref="TargetOf(BreakStatement)"/>); resolution, private names, the
/// module record, named evaluation, captures and the early errors that are not purely grammatical are later stages'. From
/// stage 2 the walk that builds it also reports its early errors of the function context (the super rows,
/// ContainsArguments, eval code's from the caller's context, and three of expressions') among the parse's diagnostics, so
/// that a source they refuse has no model, and from stage 3 those of jump targets (a <c>break</c> or a <c>continue</c> with
/// no target, and an undefined break or continue target); the early errors of private names and the module record are
/// later stages'. <i>(Revised 2026-11-25, at SJL-15.2 stage 2; where it names the early errors, it read "and the early
/// errors that are not purely grammatical are later stages'.")</i> <i>(Revised 2026-11-25, at SJL-15.2 stage 3, when jump
/// targets joined the model; it read "resolution, jump targets, private names, the module record, named evaluation,
/// captures and the early errors that are not purely grammatical are later stages'" and "the early errors of jump targets,
/// private names and the module record are later stages'.")</i>
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
/// <see cref="FactsOf"/> and both <see cref="TargetOf(BreakStatement)"/> overloads throw <see cref="ArgumentException"/>
/// for a node that is not in the model's tree, and <see cref="ParentOf"/> for a scope that is not one of the model's.
/// <i>(Revised 2026-11-25, at SJL-15.2 stage 3, when <c>TargetOf</c> joined them; it read "<see cref="ScopeOf"/>,
/// <see cref="DeclarationOf"/> and <see cref="FactsOf"/> throw <see cref="ArgumentException"/> for a node that is not in
/// the model's tree".)</i>
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

    // Hairs-AI:           Origin=AI; Spec=JSD-0080 semantic model design; IP=Low; Security=Low; Resources=1; Fingerprint=BD0D4A
    // Hairs-Human:        PENDING
    internal SemanticModel(
        IReadOnlyList<Scope> scopes,
        IReadOnlyList<FunctionFacts> functions,
        Dictionary<SyntaxNode, Scope> scopeOf,
        Dictionary<Scope, Scope?> parentOf,
        Dictionary<SyntaxNode, Declaration> declarationOf,
        Dictionary<SyntaxNode, FunctionFacts> factsOf,
        Dictionary<SyntaxNode, LabelTarget> targetOf)
    {
        Scopes = scopes;
        Functions = functions;
        this.scopeOf = scopeOf;
        this.parentOf = parentOf;
        this.declarationOf = declarationOf;
        this.factsOf = factsOf;
        this.targetOf = targetOf;
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
