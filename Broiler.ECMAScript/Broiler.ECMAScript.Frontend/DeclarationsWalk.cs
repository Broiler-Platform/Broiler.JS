// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// HAIRS Code Assurance
// --------------------
// Relevant units:   119
// Annotated:        119/119
// Exempt:           73
// Human-reviewed:   0/119
// IP risk:          Low
// Security risk:    Medium
// Criteria:         0/0
// Resource impact:  1/10 max
// Unverified:       119
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using Broiler.ECMAScript.Syntax;

namespace Broiler.ECMAScript.Semantics;

/// <summary>
/// The syntax half's declarations walk (stage SJL-13.2d; Broiler.VM's JSD-0083, decisions D6, D7 and D10): over a tree the
/// grammar admitted, it reports every static-semantic early error of a catalogue row the stage table assigns
/// <c>syntax</c>, asks the request's regular-expression provider about every regular-expression literal, and builds the side
/// tables (<see cref="DeclarationTables"/>).
/// </summary>
/// <remarks>
/// <para>
/// <b>The library's order</b> (decision D11): a refusal of a construct is placed at the construct, and a scope's
/// declared-name conflicts at the scope's close, in the source order of the second of the conflicting declarations; the walk
/// sorts what it finds by that place, so that the first is the one the parse reaches first.
/// </para>
/// <para>
/// <b>No recursion over the tree</b>: the walk keeps its own stack, so that a tree as deep as the tree-depth constant is
/// walked on any stack the parse ran on. The static-semantic helpers recurse over statements only, whose nesting the
/// request's nesting bound limits.
/// </para>
/// <para>
/// <b>The semantic model is built by this walk</b> (milestone SJL-15.2: "it does not start a second one"), when the request
/// sets <see cref="ParseRequest.BuildSemanticModel"/>: the same visit, in the same order, also records each node's scope,
/// each model scope's parent, the model's own scopes and its own facts, and, from stage 3, each jump's target
/// <i>(added 2026-11-25, at SJL-15.2 stage 3)</i>, numbering them by a counter of its own, and, from stage 4, each class's
/// private environment and each private name's binding, which it numbers by nothing <i>(added 2026-11-25, at SJL-15.2
/// stage 4)</i>, so that the
/// walk's sequence, which orders the diagnostics and the tables, is the same on or off on every parse the member's on path
/// accepts. Only <see cref="Run"/> builds it; a partial walk before a grammar refusal never does, but with the member on it
/// reports the model's rows (below). <i>(Revised 2026-11-25, at SJL-15.2 stage 2, when the model's rows joined the
/// diagnostics; it read "so that the walk's sequence, which orders the diagnostics and the tables, is the same on or off.
/// Only <see cref="Run"/> builds it; a partial walk before a grammar refusal never does.")</i>
/// </para>
/// <para>
/// <b>No <c>request</c> or <c>engine</c> row is reported, and a <c>semantics</c> row only with the member on</b>: with it
/// off every refusal is one of <see cref="Rows"/>; with it on the walk also reports the rows of <see cref="SemanticRows"/>,
/// each a construct's refusal at its construct, through the same report and the same sequence, so that decision D11's order
/// merges them with the <c>syntax</c> rows (the session's decisions D-js-channel and D-js-semantic-rows). The walk's
/// <c>semantics</c> flag, set from the member for <see cref="Run"/> and for the partial walk alike, guards every statement
/// that reports one. The top-level-<c>await</c> switch (EX0023, the one <c>request</c> row) reaches the walk only as the
/// [Await] parameter of the top level, as it reaches the parser (decision D10). <i>(Revised 2026-11-25, at SJL-15.2 stage
/// 2; it read "<b>No <c>request</c>, <c>engine</c> or <c>semantics</c> row is reported</b>: every refusal is one of
/// <see cref="Rows"/>.")</i>
/// </para>
/// </remarks>
// Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=B38FCA
// Hairs-Human:        PENDING
internal sealed class DeclarationsWalk
{
    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=774738
    // Hairs-Human:        PENDING
    private static readonly FrozenSet<string> StrictReserved = new HashSet<string>(StringComparer.Ordinal)
    {
        "implements", "interface", "let", "package", "private", "protected", "public", "static", "yield",
    }.ToFrozenSet(StringComparer.Ordinal);

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=0B9019
    // Hairs-Human:        PENDING
    private static readonly FrozenSet<string> Reserved = new HashSet<string>(StringComparer.Ordinal)
    {
        "await", "break", "case", "catch", "class", "const", "continue", "debugger", "default", "delete", "do", "else", "enum",
        "export", "extends", "false", "finally", "for", "function", "if", "import", "in", "instanceof", "new", "null", "return",
        "super", "switch", "this", "throw", "true", "try", "typeof", "var", "void", "while", "with", "yield",
    }.ToFrozenSet(StringComparer.Ordinal);

    private readonly ParseRequest request;
    private readonly bool annexB;
    private readonly bool isModule;
    private readonly List<(int Key, int SubKey, int Sequence, SyntaxDiagnostic Diagnostic)> found = new();
    private readonly List<(int Start, int Sequence, Scope Scope)> scopes = new();
    private readonly List<(int Start, int Sequence, FunctionFacts Facts)> functions = new();
    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=4AC307
    // Hairs-Human:        PENDING
    private readonly Dictionary<FunctionNode, Shape> shapes = new(ReferenceEqualityComparer.Instance);
    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=E373F4
    // Hairs-Human:        PENDING
    private readonly HashSet<SyntaxNode> declaredFunctions = new(ReferenceEqualityComparer.Instance);
    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=E6D526
    // Hairs-Human:        PENDING
    private readonly HashSet<SyntaxNode> taggedQuasis = new(ReferenceEqualityComparer.Instance);
    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=408B2B
    // Hairs-Human:        PENDING
    private readonly HashSet<SyntaxNode> ifClauseFunctions = new(ReferenceEqualityComparer.Instance);
    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=016F67
    // Hairs-Human:        PENDING
    private readonly HashSet<SyntaxNode> skippedNames = new(ReferenceEqualityComparer.Instance);
    private readonly Stack<(SyntaxNode Node, Context Context)> stack = new();
    private readonly List<SyntaxNode> children = new();
    private int sequence;

    // The semantic model's state, made only when the request asks for the model (SJL-15.2): its scopes and facts with the
    // model's own sequence, the scope of every node, every model scope's parent, and the facts by node; from stage 3, each
    // jump's target. (Added 2026-11-25, at SJL-15.2 stage 3.) From stage 4, each private name's binding and each class's
    // private environment. (Added 2026-11-25, at SJL-15.2 stage 4.)
    private readonly bool model;
    private readonly List<(int Start, int Sequence, Scope Scope)>? modelScopes;
    private readonly List<(int Start, int Sequence, FunctionFacts Facts)>? modelFunctions;
    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, JSD-0080 semantic model; IP=Low; Security=Medium; Resources=1; Fingerprint=7631C9
    // Hairs-Human:        PENDING
    private readonly Dictionary<SyntaxNode, Scope>? scopeOf;
    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, JSD-0080 semantic model; IP=Low; Security=Medium; Resources=1; Fingerprint=B50831
    // Hairs-Human:        PENDING
    private readonly Dictionary<Scope, Scope?>? parentOf;
    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, JSD-0080 semantic model; IP=Low; Security=Medium; Resources=1; Fingerprint=2E3C8C
    // Hairs-Human:        PENDING
    private readonly Dictionary<SyntaxNode, FunctionFacts>? factsOf;
    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, JSD-0080 semantic model; IP=Low; Security=Medium; Resources=1; Fingerprint=04701C
    // Hairs-Human:        PENDING
    private readonly Dictionary<SyntaxNode, Scope>? ifClauseScopes;
    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, JSD-0080 semantic model; IP=Low; Security=Medium; Resources=1; Fingerprint=8237C1
    // Hairs-Human:        PENDING
    private readonly HashSet<SyntaxNode>? declaredClasses;
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 ContainsUndefinedBreakTarget ContainsUndefinedContinueTarget LabelledEvaluation, JSD-0080 jump targets; IP=Low; Security=Medium; Resources=1; Fingerprint=10963D
    // Hairs-Human:        PENDING
    private readonly Dictionary<SyntaxNode, LabelTarget>? targetOf;
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 ResolvePrivateIdentifier AllPrivateIdentifiersValid, JSD-0080 private names; IP=Low; Security=Medium; Resources=1; Fingerprint=C73638
    // Hairs-Human:        PENDING
    private readonly Dictionary<SyntaxNode, PrivateNameBinding>? privateOf;
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 PrivateEnvironment Records, s15.7.14 ClassDefinitionEvaluation, JSD-0080 private names; IP=Low; Security=Medium; Resources=1; Fingerprint=F74F37
    // Hairs-Human:        PENDING
    private readonly Dictionary<ClassNode, PrivateEnvironment>? privateEnvironmentOf;
    private int modelSequence;

    // Each class constructor's kind, on every request (the functions' facts are the same with the model on or off).
    private Dictionary<FunctionNode, FunctionKind>? constructorKinds;

    // The model's rows' state, made only when the request asks for the model (SJL-15.2 stage 2): whether the walk reports
    // them, each method's home, and the members that have reported ContainsArguments once; from stage 4, the row an
    // undeclared private name breaks under the request's goal. (Added 2026-11-25, at SJL-15.2 stage 4.)
    private readonly bool semantics;
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s15.4 MethodDefinition HasDirectSuper, s15.7.1 ClassBody early errors, SJL-15.2 semantics rows; IP=Low; Security=Medium; Resources=1; Fingerprint=42B997
    // Hairs-Human:        PENDING
    private readonly Dictionary<FunctionNode, MethodHome>? methodHomes;
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s15.7.1 ContainsArguments, SJL-15.2 semantics rows; IP=Low; Security=Medium; Resources=1; Fingerprint=87C76A
    // Hairs-Human:        PENDING
    private readonly HashSet<SyntaxNode>? argumentsReported;
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s16.1.1 s16.2.1.1 AllPrivateIdentifiersValid, s19.2.1.3 EvalDeclarationInstantiation step 7, SJL-15.2 semantics rows; IP=Low; Security=Medium; Resources=1; Fingerprint=B03F19
    // Hairs-Human:        PENDING
    private readonly LexicalRefusal? undeclaredPrivateRow;

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=01B720
    // Hairs-Human:        PENDING
    private DeclarationsWalk(ParseRequest request, bool buildModel, bool semantics)
    {
        this.request = request;
        annexB = request.Switches.AnnexB;
        isModule = request.Goal == ParseGoal.Module;
        model = buildModel;
        if (buildModel)
        {
            modelScopes = new();
            modelFunctions = new();
            scopeOf = new(ReferenceEqualityComparer.Instance);
            parentOf = new(ReferenceEqualityComparer.Instance);
            factsOf = new(ReferenceEqualityComparer.Instance);
            ifClauseScopes = new(ReferenceEqualityComparer.Instance);
            declaredClasses = new(ReferenceEqualityComparer.Instance);
            targetOf = new(ReferenceEqualityComparer.Instance);
            privateOf = new(ReferenceEqualityComparer.Instance);
            privateEnvironmentOf = new(ReferenceEqualityComparer.Instance);
        }

        this.semantics = semantics;
        if (semantics)
        {
            methodHomes = new(ReferenceEqualityComparer.Instance);
            argumentsReported = new(ReferenceEqualityComparer.Instance);

            // AllPrivateIdentifiersValid with no names, of a Script (EE0167) and of a Module (EE0177), and of eval code
            // against the names its caller sees (EA0009), only where the request gives them: a null list leaves the check to
            // the engine. The dynamic function's goals check none (the catalogue's EX0031 is an engine row).
            undeclaredPrivateRow = request.Goal switch
            {
                ParseGoal.Script => SemanticRows.EE0167,
                ParseGoal.Module => SemanticRows.EE0177,
                ParseGoal.Eval => request.Context!.VisiblePrivateNames is null ? null : SemanticRows.EA0009,
                _ => null,
            };
        }
    }

    /// <summary>
    /// Where a method stands, which decides the row its HasDirectSuper is reported by (clauses 13.2.5.1 and 15.7.1): an
    /// object literal's, a class's, a class's static one, the class's constructor (ConstructorMethod of its ClassBody, the
    /// first element whose PropName is <c>"constructor"</c>) with no heritage or with one, or a later element of that name.
    /// </summary>
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s15.4 MethodDefinition HasDirectSuper, s15.7.1 ClassBody early errors, SJL-15.2 semantics rows; IP=Low; Security=Medium; Resources=1; Fingerprint=DA6343
    // Hairs-Human:        PENDING
    private enum MethodHome
    {
        ObjectLiteral,
        Class,
        StaticClass,
        BaseConstructor,
        DerivedConstructor,
        DuplicateConstructor,
    }

    /// <summary>The shape of a function, which decides the catalogue row each of its rules is reported by.</summary>
    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=A9BD3F
    // Hairs-Human:        PENDING
    private enum Shape
    {
        Function,
        Generator,
        AsyncFunction,
        AsyncGenerator,
        Method,
        GeneratorMethod,
        AsyncMethod,
        AsyncGeneratorMethod,
        Getter,
        Setter,
        Arrow,
        AsyncArrow,
    }

    /// <summary>The kind of the statement list whose labels a label set holds (decision D10's ContainsDuplicateLabels rows).</summary>
    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=ADA519
    // Hairs-Human:        PENDING
    private enum LabelOwner
    {
        Script,
        Module,
        Function,
        StaticBlock,
    }

    /// <summary>The walk's result: the diagnostics in the library's order, and the side tables.</summary>
    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=F734C4
    // Hairs-Human:        PENDING
    internal sealed class Result(List<SyntaxDiagnostic> diagnostics, DeclarationTables tables)
    {
        internal List<SyntaxDiagnostic> Diagnostics { get; } = diagnostics;

        internal DeclarationTables Tables { get; } = tables;
    }

    /// <summary>Walks a tree the grammar admitted.</summary>
    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=16FD67
    // Hairs-Human:        PENDING
    internal static Result Run(ParseRequest request, ProgramNode program)
    {
        var walk = new DeclarationsWalk(request, request.BuildSemanticModel, request.BuildSemanticModel);
        walk.WalkProgram(program, program.Body, closed: true);
        return new Result(walk.Ordered(int.MaxValue), walk.Tables());
    }

    /// <summary>
    /// Walks the top-level statements a parse completed before the grammar refused at <paramref name="refusedAt"/>: their
    /// early errors come before the grammar's refusal (decision D11). The top level's own scope never closed, so its conflicts
    /// are not checked. It builds no model; with the request's <see cref="ParseRequest.BuildSemanticModel"/> on it reports
    /// the model's rows, each of which its construct decides whole (SJL-15.2 stage 2).
    /// </summary>
    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=49D611
    // Hairs-Human:        PENDING
    internal static List<SyntaxDiagnostic> RunPartial(ParseRequest request, List<Statement> completed, int refusedAt)
    {
        var walk = new DeclarationsWalk(request, buildModel: false, semantics: request.BuildSemanticModel);
        var span = new SourceSpan(request.Source.BaseOffset, 0, request.Source.BaseLine, request.Source.BaseColumn);
        var program = new ProgramNode(span, request.Goal, completed.ToArray(), Array.Empty<Pattern>(), false, false);
        walk.WalkProgram(program, completed, closed: false);
        return walk.Ordered(refusedAt);
    }

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=7D56CF
    // Hairs-Human:        PENDING
    private List<SyntaxDiagnostic> Ordered(int before)
    {
        // By place, then (for the conflicts a scope's close reports) by the second of the conflicting declarations, then by
        // the order the walk found them in.
        found.Sort(static (a, b) => a.Key != b.Key ? a.Key.CompareTo(b.Key) : a.SubKey != b.SubKey ? a.SubKey.CompareTo(b.SubKey) : a.Sequence.CompareTo(b.Sequence));
        var list = new List<SyntaxDiagnostic>(found.Count);
        foreach (var entry in found)
        {
            if (entry.Key < before)
                list.Add(entry.Diagnostic);
        }

        return list;
    }

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=8C679D
    // Hairs-Human:        PENDING
    private DeclarationTables Tables()
    {
        scopes.Sort(static (a, b) => a.Start != b.Start ? a.Start.CompareTo(b.Start) : a.Sequence.CompareTo(b.Sequence));
        functions.Sort(static (a, b) => a.Start != b.Start ? a.Start.CompareTo(b.Start) : a.Sequence.CompareTo(b.Sequence));
        var scopeList = new Scope[scopes.Count];
        for (var i = 0; i < scopeList.Length; i++)
            scopeList[i] = scopes[i].Scope;
        var functionList = new FunctionFacts[functions.Count];
        for (var i = 0; i < functionList.Length; i++)
            functionList[i] = functions[i].Facts;
        return new DeclarationTables(Array.AsReadOnly(scopeList), Array.AsReadOnly(functionList), model ? Model(scopeList) : null);
    }

    /// <summary>
    /// The semantic model: its scopes and facts in the order of their node's start and, for one start, of the model's
    /// sequence; each declaration by its node, the tables' first, so that a class declaration's name answers its outer
    /// declaration (the session's decision D-js-declaration-of).
    /// </summary>
    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, JSD-0080 semantic model; IP=Low; Security=Medium; Resources=1; Fingerprint=3AD55B
    // Hairs-Human:        PENDING
    private SemanticModel Model(Scope[] tableScopes)
    {
        modelScopes!.Sort(static (a, b) => a.Start != b.Start ? a.Start.CompareTo(b.Start) : a.Sequence.CompareTo(b.Sequence));
        modelFunctions!.Sort(static (a, b) => a.Start != b.Start ? a.Start.CompareTo(b.Start) : a.Sequence.CompareTo(b.Sequence));
        var scopeList = new Scope[modelScopes.Count];
        for (var i = 0; i < scopeList.Length; i++)
            scopeList[i] = modelScopes[i].Scope;
        var functionList = new FunctionFacts[modelFunctions.Count];
        for (var i = 0; i < functionList.Length; i++)
            functionList[i] = modelFunctions[i].Facts;

        var declarationOf = new Dictionary<SyntaxNode, Declaration>(ReferenceEqualityComparer.Instance);
        foreach (var scope in tableScopes)
        {
            foreach (var declaration in scope.Declarations)
                declarationOf.TryAdd(declaration.Node, declaration);
        }

        foreach (var scope in scopeList)
        {
            foreach (var declaration in scope.Declarations)
                declarationOf.TryAdd(declaration.Node, declaration);
        }

        return new SemanticModel(Array.AsReadOnly(scopeList), Array.AsReadOnly(functionList), scopeOf!, parentOf!, declarationOf, factsOf!, targetOf!, privateOf!, privateEnvironmentOf!);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // The tables' and the model's entries.

    /// <summary>A scope of the tables, numbered by the walk's sequence; with the model on, also one of the model's, its parent given.</summary>
    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, JSD-0080 semantic model; IP=Low; Security=Medium; Resources=1; Fingerprint=1BEA9B
    // Hairs-Human:        PENDING
    private Scope AddScope(int start, Scope scope, Scope? parent)
    {
        scopes.Add((start, sequence++, scope));
        if (model)
            AddModelScope(start, scope, parent);
        return scope;
    }

    /// <summary>A scope of the model's, numbered by the model's sequence alone.</summary>
    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, JSD-0080 semantic model; IP=Low; Security=Medium; Resources=1; Fingerprint=C4ED77
    // Hairs-Human:        PENDING
    private Scope AddModelScope(int start, Scope scope, Scope? parent)
    {
        modelScopes!.Add((start, modelSequence++, scope));
        parentOf![scope] = parent;
        return scope;
    }

    /// <summary>A program's or a function's facts in the tables, numbered by the walk's sequence; with the model on, also in the model's.</summary>
    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, JSD-0080 semantic model; IP=Low; Security=Medium; Resources=1; Fingerprint=6B32E8
    // Hairs-Human:        PENDING
    private void AddFunction(int start, FunctionFacts facts)
    {
        functions.Add((start, sequence++, facts));
        if (model)
            AddModelFunction(start, facts);
    }

    /// <summary>Facts of the model's, numbered by the model's sequence alone.</summary>
    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, JSD-0080 semantic model; IP=Low; Security=Medium; Resources=1; Fingerprint=E1C975
    // Hairs-Human:        PENDING
    private void AddModelFunction(int start, FunctionFacts facts)
    {
        modelFunctions!.Add((start, modelSequence++, facts));
        factsOf![facts.Node] = facts;
    }

    /// <summary>The model's scope of a node the walk handles without visiting it.</summary>
    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, JSD-0080 semantic model; IP=Low; Security=Medium; Resources=1; Fingerprint=070F1C
    // Hairs-Human:        PENDING
    private void Record(SyntaxNode node, Scope scope) => scopeOf![node] = scope;

    /// <summary>The model's scope of a node the walk visits nothing of, and of everything below it; iterative.</summary>
    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, JSD-0080 semantic model; IP=Low; Security=Medium; Resources=1; Fingerprint=1BEEC9
    // Hairs-Human:        PENDING
    private void RecordSubtree(SyntaxNode node, Scope scope)
    {
        var pending = new Stack<SyntaxNode>();
        var below = new List<SyntaxNode>();
        pending.Push(node);
        while (pending.Count > 0)
        {
            var next = pending.Pop();
            scopeOf![next] = scope;
            below.Clear();
            next.AddChildren(below);
            foreach (var child in below)
                pending.Push(child);
        }
    }

    /// <summary>The model's scope of a node's children, which the walk does not visit, and of everything below them.</summary>
    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, JSD-0080 semantic model; IP=Low; Security=Medium; Resources=1; Fingerprint=F59619
    // Hairs-Human:        PENDING
    private void RecordChildren(SyntaxNode node, Scope scope)
    {
        var below = new List<SyntaxNode>();
        node.AddChildren(below);
        foreach (var child in below)
            RecordSubtree(child, scope);
    }

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, JSD-0080 semantic model parameters; IP=Low; Security=Medium; Resources=1; Fingerprint=1CA422
    // Hairs-Human:        PENDING
    private static IReadOnlyList<Declaration> ParameterDeclarations(List<BindingIdentifier> nodes)
    {
        var declarations = new Declaration[nodes.Count];
        for (var i = 0; i < declarations.Length; i++)
            declarations[i] = new Declaration(nodes[i].Name, BindingKind.Parameter, nodes[i], false, false, false);
        return Array.AsReadOnly(declarations);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Reporting.

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=34AB6C
    // Hairs-Human:        PENDING
    private void Report(LexicalRefusal row, SourceSpan span, int key, int subKey, string argument = "")
    {
        var diagnostic = new SyntaxDiagnostic(row.Code, row.MessageId, argument.Length == 0 ? Array.Empty<string>() : new[] { argument },
            DiagnosticPhase.Semantics, span, request.Source.Name);
        found.Add((key, subKey, sequence++, diagnostic));
    }

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=67E8B3
    // Hairs-Human:        PENDING
    private void Report(LexicalRefusal row, SyntaxNode at, string argument = "") => Report(row, at.Span, at.Span.Offset, at.Span.Offset, argument);

    /// <summary>
    /// A declared-name conflict, reported at the second of the conflicting declarations and placed at the close of the scope
    /// that holds them, after the conflicts of the same scope whose second declaration comes earlier (decision D11).
    /// </summary>
    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=9326BB
    // Hairs-Human:        PENDING
    private void ReportConflict(LexicalRefusal row, SyntaxNode second, int scopeEnd, string name) =>
        Report(row, second.Span, scopeEnd, second.Span.Offset, name);

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=FF2B43
    // Hairs-Human:        PENDING
    private static int End(SyntaxNode node) => node.Span.Offset + node.Span.Length;

    // ---------------------------------------------------------------------------------------------------------------
    // The program.

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=87BEB8
    // Hairs-Human:        PENDING
    private void WalkProgram(ProgramNode program, IReadOnlyList<Statement> body, bool closed)
    {
        var goal = request.Goal;
        var strict = isModule || request.Switches.ForcedStrict || HasUseStrict(body);
        var kind = request.FunctionKind;
        var isDynamic = goal is ParseGoal.FormalParameters or ParseGoal.FunctionBody;
        var inFunction = isDynamic || (goal == ParseGoal.Eval && request.Context is { InFunction: true });
        var context = new Context
        {
            Strict = strict,
            Yield = isDynamic && kind is DynamicFunctionKind.Generator or DynamicFunctionKind.AsyncGenerator,
            Await = isDynamic ? kind is DynamicFunctionKind.Async or DynamicFunctionKind.AsyncGenerator : request.Switches.TopLevelAwait,
            InFunction = inFunction,
            LabelOwner = isModule ? LabelOwner.Module : isDynamic ? LabelOwner.Function : LabelOwner.Script,
            Semantic = semantics ? ProgramSemantic(isDynamic ? FunctionRows(DynamicShape(kind), inBody: true) : null) : null,
        };

        var parameterNames = new List<string>();
        foreach (var parameter in program.Parameters)
            BoundNames(parameter, parameterNames, null);
        var programKind = goal switch
        {
            ParseGoal.Script => FunctionKind.Script,
            ParseGoal.Module => FunctionKind.Module,
            ParseGoal.Eval => FunctionKind.EvalCode,
            _ => FunctionKind.DynamicFunction,
        };
        var simpleProgram = IsSimpleParameterList(program.Parameters);
        AddFunction(program.Span.Offset, new FunctionFacts(program, strict, parameterNames.AsReadOnly(), programKind,
            isDynamic && kind is DynamicFunctionKind.Async or DynamicFunctionKind.AsyncGenerator,
            isDynamic && kind is DynamicFunctionKind.Generator or DynamicFunctionKind.AsyncGenerator,
            simpleProgram, !simpleProgram && ContainsExpression(program.Parameters)));

        // The model's: a dynamic function's parameters have a scope of their own, the model's first, before the program's.
        Scope? programParameters = null;
        if (model && isDynamic)
        {
            var nodes = new List<BindingIdentifier>();
            foreach (var parameter in program.Parameters)
                BoundNames(parameter, null, nodes);
            programParameters = AddModelScope(program.Span.Offset, new Scope(ScopeKind.Parameters, program, ParameterDeclarations(nodes)), null);
        }

        // The top level's scope: a function's for the dynamic function's goals, whose declarations are its body's.
        var top = !isModule;
        var lexical = new List<Declaration>();
        var vars = new List<Declaration>();
        CollectList(body, top, isModule, lexical, vars);
        var scopeKind = isModule ? ScopeKind.Module : isDynamic ? ScopeKind.Function : ScopeKind.Script;
        var programScope = AddScope(program.Span.Offset, new Scope(scopeKind, program, Merge(lexical, vars)), programParameters);
        if (model)
        {
            Record(program, programParameters ?? programScope);
            context = context with { ModelScope = programScope };
        }

        if (closed)
        {
            var scopeEnd = End(program);
            if (isModule)
                CheckConflicts(lexical, vars, scopeEnd, Rows.EE0168, Rows.EE0169);
            else if (isDynamic)
                CheckConflicts(lexical, vars, scopeEnd, Rows.EE0078, Rows.EE0079);
            else
                CheckConflicts(lexical, vars, scopeEnd, Rows.EE0160, Rows.EE0161);
        }

        // ContainsUsing of a ScriptBody (EP0008): a using declaration directly in its statements.
        if (goal is ParseGoal.Script or ParseGoal.Eval)
            CheckNoUsing(body, Rows.EP0008);

        // The dynamic function's parameters bind no name twice where FormalParameters forbids it (EE0069, EE0070 and its shapes).
        if (goal == ParseGoal.FormalParameters)
        {
            var nodes = new List<BindingIdentifier>();
            foreach (var parameter in program.Parameters)
                BoundNames(parameter, null, nodes);
            if (DuplicateParameterRow(DynamicShape(kind), IsSimpleParameterList(program.Parameters), strict) is { } duplicateRow)
            {
                // Every name bound more than once, at its second binding (decision D11: all of them under all errors).
                var seen = new HashSet<string>(StringComparer.Ordinal);
                var reported = new HashSet<string>(StringComparer.Ordinal);
                foreach (var node in nodes)
                {
                    if (!seen.Add(node.Name) && reported.Add(node.Name))
                        Report(duplicateRow, node, node.Name);
                }
            }
        }

        var parameterContext = context with
        {
            ParamsOf = isDynamic ? DynamicShape(kind) : null,
            ModelScope = programParameters ?? context.ModelScope,
            Semantic = semantics ? isDynamic ? FunctionRows(DynamicShape(kind), inBody: false) : context.Semantic : null,
        };
        Run(program.Parameters, parameterContext);
        Run(body, context);
    }

    /// <summary>
    /// The rows of the program's own code (SJL-15.2 stage 2): a Script's <c>super</c> (EE0162) and a Module's (EE0172);
    /// eval code's from the caller's context, PerformEval's steps 11.f to 11.h (EA0003 to EA0005), for every eval request,
    /// direct or indirect, which the request does not tell apart; a dynamic function's those of the function its source is
    /// parsed as (<paramref name="dynamic"/>). From stage 4 eval code's private names start at the names its caller sees,
    /// where the request gives them; every other program's at none. <i>(Added 2026-11-25, at SJL-15.2 stage 4.)</i>
    /// </summary>
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s16.1.1 s16.2.1.1 Contains super, s19.2.1.1 PerformEval steps 11.f-11.h, s20.2.1.1.1 CreateDynamicFunction; IP=Low; Security=Medium; Resources=1; Fingerprint=6D20C2
    // Hairs-Human:        PENDING
    private SemanticContext ProgramSemantic(SemanticContext? dynamic)
    {
        switch (request.Goal)
        {
            case ParseGoal.Module:
                return new SemanticContext(SemanticRows.EE0172, SemanticRows.EE0172, null);
            case ParseGoal.Eval:
            {
                var caller = request.Context!;
                return new SemanticContext(
                    caller.InMethod ? null : SemanticRows.EA0003,
                    caller.InDerivedConstructor ? null : SemanticRows.EA0004,
                    caller.InClassFieldInitializer ? new ArgumentsAnchors(SemanticRows.EA0005, null, null) : null)
                {
                    // The caller's visible private names, the root of eval code's (EvalDeclarationInstantiation steps 4 to
                    // 7; SJL-15.2 stage 4), where the request gives them.
                    Privates = caller.VisiblePrivateNames is { } visible ? new PrivateScope(visible, null, null) : null,
                };
            }

            case ParseGoal.FormalParameters or ParseGoal.FunctionBody:
                return dynamic!;
            default:
                return new SemanticContext(SemanticRows.EE0162, SemanticRows.EE0162, null);
        }
    }

    /// <summary>
    /// The rows a SuperProperty and a SuperCall break in a non-arrow function's parameters or body (SJL-15.2 stage 2): a
    /// method's call row by its shape and home and no property row (every method admits one); any other function's by its
    /// shape and side (clauses 15.2.1, 15.5.1, 15.6.1 and 15.8.1). A function's parameters and body reset ContainsArguments'
    /// anchors.
    /// </summary>
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s15.2.1 s15.5.1 s15.6.1 s15.8.1 Contains SuperProperty SuperCall, s15.4 HasDirectSuper; IP=Low; Security=Medium; Resources=1; Fingerprint=6466CC
    // Hairs-Human:        PENDING
    private SemanticContext FunctionSemantic(FunctionNode function, Shape shape, bool inBody) =>
        methodHomes!.TryGetValue(function, out var home)
            ? new SemanticContext(null, MethodCallRow(shape, home), null)
            : FunctionRows(shape, inBody);

    /// <summary>A function's rows by its shape and side; a shape no such row names (a method's, an arrow's) admits both.</summary>
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s15.2.1 s15.5.1 s15.6.1 s15.8.1 Contains SuperProperty SuperCall; IP=Low; Security=Medium; Resources=1; Fingerprint=13E42B
    // Hairs-Human:        PENDING
    private static SemanticContext FunctionRows(Shape shape, bool inBody) => shape switch
    {
        Shape.Function => inBody ? new(SemanticRows.EE0075, SemanticRows.EE0077, null) : new(SemanticRows.EE0074, SemanticRows.EE0076, null),
        Shape.Generator => inBody ? new(SemanticRows.EE0103, SemanticRows.EE0105, null) : new(SemanticRows.EE0102, SemanticRows.EE0104, null),
        Shape.AsyncGenerator => inBody ? new(SemanticRows.EE0118, SemanticRows.EE0120, null) : new(SemanticRows.EE0117, SemanticRows.EE0119, null),
        Shape.AsyncFunction => inBody ? new(SemanticRows.EE0151, SemanticRows.EE0153, null) : new(SemanticRows.EE0150, SemanticRows.EE0152, null),
        _ => new(null, null, null),
    };

    /// <summary>
    /// The row a method's SuperCall breaks (HasDirectSuper; the session's proposal of one row per construct): its shape's
    /// first (EE0093, EE0142, EE0106), then its home's (EE0015, EE0124, EE0126, EE0121); a derived class's constructor, and
    /// every later element named <c>"constructor"</c>, which EE0122 or EE0125 already refuses, admit it.
    /// </summary>
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s13.2.5.1 s15.5.1 s15.6.1 s15.7.1 s15.8.1 HasDirectSuper early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=2AD253
    // Hairs-Human:        PENDING
    private static LexicalRefusal? MethodCallRow(Shape shape, MethodHome home) => shape switch
    {
        Shape.GeneratorMethod => SemanticRows.EE0093,
        Shape.AsyncMethod => SemanticRows.EE0142,
        Shape.AsyncGeneratorMethod => SemanticRows.EE0106,
        _ => home switch
        {
            MethodHome.ObjectLiteral => SemanticRows.EE0015,
            MethodHome.Class => SemanticRows.EE0124,
            MethodHome.StaticClass => SemanticRows.EE0126,
            MethodHome.BaseConstructor => SemanticRows.EE0121,
            _ => null,
        },
    };

    /// <summary>Whether a template's TemplateStrings reach EE0021's bound, 2^32 elements (clause 13.2.8.1); no .NET source reaches it.</summary>
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s13.2.8.1 TemplateLiteral early errors; IP=Low; Security=Low; Resources=0; Fingerprint=A0C330
    // Hairs-Human:        PENDING
    internal static bool TooManyTemplateStrings(long count) => count >= 1L << 32;

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=DCC45B
    // Hairs-Human:        PENDING
    private static Shape DynamicShape(DynamicFunctionKind kind) => kind switch
    {
        DynamicFunctionKind.Generator => Shape.Generator,
        DynamicFunctionKind.Async => Shape.AsyncFunction,
        DynamicFunctionKind.AsyncGenerator => Shape.AsyncGenerator,
        _ => Shape.Function,
    };

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=84DDF8
    // Hairs-Human:        PENDING
    private void Run<T>(IReadOnlyList<T> nodes, Context context)
        where T : SyntaxNode?
    {
        for (var i = nodes.Count - 1; i >= 0; i--)
        {
            if (nodes[i] is { } node)
                stack.Push((node, context));
        }

        while (stack.Count > 0)
        {
            var (node, nodeContext) = stack.Pop();
            if (model)
                Record(node, nodeContext.ModelScope!);
            Visit(node, nodeContext);
        }
    }

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=5CC567
    // Hairs-Human:        PENDING
    private void Push(SyntaxNode? node, Context context)
    {
        if (node is not null)
            stack.Push((node, context));
    }

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=6215BA
    // Hairs-Human:        PENDING
    private void PushAll<T>(IReadOnlyList<T> nodes, Context context)
        where T : SyntaxNode?
    {
        for (var i = nodes.Count - 1; i >= 0; i--)
        {
            if (nodes[i] is { } node)
                stack.Push((node, context));
        }
    }

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=D08166
    // Hairs-Human:        PENDING
    private void PushChildren(SyntaxNode node, Context context)
    {
        children.Clear();
        node.AddChildren(children);
        for (var i = children.Count - 1; i >= 0; i--)
            stack.Push((children[i], context));
    }

    // ---------------------------------------------------------------------------------------------------------------
    // The visit: each node's own rules, then its children under their context, pushed in reverse so that they are
    // visited in source order.

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=48A9F3
    // Hairs-Human:        PENDING
    private void Visit(SyntaxNode node, Context context)
    {
        switch (node)
        {
            case Identifier identifier:
                CheckName(identifier.Name, identifier.IsEscaped, identifier, NameUse.Reference, context);

                // The model's rows: ContainsArguments of each member whose initialiser or block holds the reference, once
                // per member (EE0130, EE0138), and of eval code a field initialiser evaluates, at the reference (EA0005).
                if (semantics && identifier.Name == "arguments")
                    ReportArguments(identifier, context);
                return;
            case BindingIdentifier binding:
                if (!skippedNames.Contains(binding))
                    CheckName(binding.Name, binding.IsEscaped, binding, NameUse.Binding, context);
                return;
            case NumericLiteral numeric:
                if (context.Strict && (numeric.Flags & (NumericLiteralFlags.LegacyOctal | NumericLiteralFlags.NonOctalDecimal)) != 0)
                    Report(Rows.EE0003, numeric, numeric.Text);
                return;
            case StringLiteral text:
                if (context.Strict && (text.Flags & (StringLiteralFlags.LegacyOctalEscape | StringLiteralFlags.NonOctalDecimalEscape)) != 0)
                    Report(Rows.EE0004, text);
                return;
            case RegularExpressionLiteral literal:
                ValidateRegularExpression(literal);
                return;
            case TemplateLiteral template:
                // The model's row: TemplateStrings of 2^32 elements or more (EE0021), which no source reaches.
                if (semantics && TooManyTemplateStrings(template.Quasis.Count))
                    Report(SemanticRows.EE0021, template);
                if (!taggedQuasis.Contains(template))
                    CheckTemplateEscapes(template);
                PushChildren(node, context);
                return;
            case TaggedTemplateExpression tagged:
                taggedQuasis.Add(tagged.Quasi);
                PushChildren(node, context);
                return;
            case FunctionDeclaration declaration:
                declaredFunctions.Add(declaration.Function);

                // The model's: an if clause's function is in its implicit block (Annex B.3.3).
                Push(declaration.Function, model && ifClauseScopes!.TryGetValue(declaration, out var clauseScope) ? context with { ModelScope = clauseScope } : context);
                return;
            case ClassDeclaration classDeclaration:
                if (model)
                    declaredClasses!.Add(classDeclaration.Class);
                PushChildren(node, context);
                return;
            case FunctionNode function:
                VisitFunction(function, context);
                return;
            case ClassNode @class:
                VisitClass(@class, context);
                return;
            case PropertyDefinition property:
                VisitProperty(property, context);
                return;
            case MetaProperty meta:
                CheckMetaProperty(meta, context);
                if (model)
                    RecordChildren(meta, context.ModelScope!);
                return;
            case YieldExpression yield:
                if (context.ParamsOf is { } yieldShape && YieldInParameters(yieldShape) is { } yieldRow)
                    Report(yieldRow, yield);
                PushChildren(node, context);
                return;
            case AwaitExpression await:
                if (context.InStaticBlock)
                    Report(Rows.EE0140, await, "await");
                else if (context.ParamsOf is { } awaitShape && AwaitInParameters(awaitShape) is { } awaitRow)
                    Report(awaitRow, await);
                PushChildren(node, context);
                return;
            case AssignmentExpression assignment:
                CheckAssignment(assignment, context);
                PushChildren(node, context);
                return;
            case UpdateExpression update:
                if (TargetTypeOf(update.Operand, context) == TargetType.Invalid)
                    Report(update.IsPrefix ? Rows.EE0029 : Rows.EE0028, update.Operand);
                PushChildren(node, context);
                return;
            case UnaryExpression { Operator: UnaryOperator.Delete } unary:
                CheckDelete(unary, context);
                PushChildren(node, context);
                return;
            case TargetPattern target:
                if (TargetTypeOf(target.Target, context) != TargetType.Simple)
                    Report(Rows.EE0039, target.Target);
                PushChildren(node, context);
                return;
            case PatternProperty { IsShorthand: true } shorthand:
                VisitShorthand(shorthand, context);
                return;
            case BlockStatement block:
                var blockScope = VisitStatementListScope(block, block.Body, ScopeKind.Block, Rows.EE0040, Rows.EE0041, context.Strict, context.ModelScope);
                PushChildren(node, model ? context with { ModelScope = blockScope } : context);
                return;
            case SwitchStatement @switch:
                VisitSwitch(@switch, context);
                return;
            case VariableDeclaration declaration:
                CheckDeclaration(declaration, inForInOfHead: false);
                PushChildren(node, context);
                return;
            case ForStatement @for:
                VisitFor(@for, context);
                return;
            case ForInStatement forIn:
                VisitForInOf(forIn, forIn.Left, forIn.Body, context);
                return;
            case ForOfStatement forOf:
                VisitForInOf(forOf, forOf.Left, forOf.Body, context);
                return;
            case IfStatement @if:
                VisitIf(@if, context);
                return;
            case WhileStatement @while:
                CheckLabelledFunctionBody(@while.Body, Rows.EE0049);
                PushChildren(node, semantics ? Inside(context, BreakableKind.Iteration, @while) : context);
                return;
            case DoWhileStatement doWhile:
                CheckLabelledFunctionBody(doWhile.Body, Rows.EE0048);
                PushChildren(node, semantics ? Inside(context, BreakableKind.Iteration, doWhile) : context);
                return;
            case WithStatement with:
                if (context.Strict)
                    Report(Rows.EE0060, with, "with");
                CheckLabelledFunctionBody(with.Body, Rows.EE0061);
                if (model)
                {
                    // The model's: the body in the object environment, the object outside it (clause 14.11.2).
                    var withScope = AddModelScope(with.Span.Offset, new Scope(ScopeKind.With, with, Array.Empty<Declaration>()), context.ModelScope);
                    Push(with.Body, context with { ModelScope = withScope });
                    Push(with.Object, context);
                }
                else
                {
                    PushChildren(node, context);
                }

                return;
            case LabelledStatement labelled:
                VisitLabelled(labelled, context);
                return;
            case BreakStatement { Label: { } breakLabel } labelledBreak:
                CheckName(breakLabel.Name, breakLabel.IsEscaped, breakLabel, NameUse.Label, context);
                if (model)
                    RecordSubtree(breakLabel, context.ModelScope!);

                // The model's row and fact (SJL-15.2 stage 3): the statement the label names, or the undefined target's row.
                if (semantics)
                    CheckJump(labelledBreak, breakLabel.Name, continues: false, context);
                return;
            case ContinueStatement { Label: { } continueLabel } labelledContinue:
                CheckName(continueLabel.Name, continueLabel.IsEscaped, continueLabel, NameUse.Label, context);
                if (model)
                    RecordSubtree(continueLabel, context.ModelScope!);

                // The model's row and fact (SJL-15.2 stage 3): the iteration statement whose label set holds the label, or
                // the undefined target's row.
                if (semantics)
                    CheckJump(labelledContinue, continueLabel.Name, continues: true, context);
                return;
            case CatchClause @catch:
                VisitCatch(@catch, context);
                return;
            case ImportDeclaration import:
                CheckModuleExportNames(import);
                CheckAttributes(import.Attributes);
                PushChildren(node, context);
                return;
            case ExportNamedDeclaration export:
                CheckExport(export);
                CheckAttributes(export.Attributes);
                PushChildren(node, context);
                return;
            case ExportAllDeclaration exportAll:
                if (exportAll.Exported is StringLiteral exported)
                    CheckExportName(exported);
                CheckAttributes(exportAll.Attributes);
                PushChildren(node, context);
                return;
            case ImportSpecifier specifier:
                if (specifier.Imported is StringLiteral imported)
                    CheckExportName(imported);
                PushChildren(node, context);
                return;

            // The model's rows (SJL-15.2 stage 2), at the construct Broiler.VM's binder names: each SuperProperty and each
            // SuperCall where the enclosing function, class element, program or caller does not admit it, and an object
            // literal's duplicate __proto__ entries. With the member off these fall to the default, as before.
            case MemberExpression { Object: SuperExpression } superProperty when semantics:
                if (context.Semantic!.SuperPropertyRow is { } propertyRow)
                    Report(propertyRow, superProperty);
                PushChildren(node, context);
                return;
            case CallExpression { Callee: SuperExpression } superCall when semantics:
                if (context.Semantic!.SuperCallRow is { } callRow)
                    Report(callRow, superCall);
                PushChildren(node, context);
                return;
            case ObjectLiteral literal when semantics:
                CheckProtoEntries(literal);
                PushChildren(node, context);
                return;

            // The model's rows and fact (SJL-15.2 stage 3), at the jump Broiler.VM's binder names: an unlabelled break's
            // innermost iteration statement or switch (EE0059 where none encloses it), an unlabelled continue's innermost
            // iteration statement (EE0058). The labelled jumps are the cases above. With the member off these fall to the
            // default, as before.
            case BreakStatement unlabelledBreak when semantics:
                CheckJump(unlabelledBreak, null, continues: false, context);
                PushChildren(node, context);
                return;
            case ContinueStatement unlabelledContinue when semantics:
                CheckJump(unlabelledContinue, null, continues: true, context);
                PushChildren(node, context);
                return;

            // The model's rows and fact (SJL-15.2 stage 4), at the member or `in` expression Broiler.VM's binder names: a
            // private name's binding in the innermost class that declares it, or the undeclared name's row (EE0167, EE0177,
            // EA0009). With the member off these fall to the default, as before.
            case MemberExpression { Property: PrivateIdentifier privateName } privateMember when semantics:
                CheckPrivateReference(privateName, privateMember, context);
                PushChildren(node, context);
                return;
            case PrivateInExpression privateIn when semantics:
                CheckPrivateReference(privateIn.Name, privateIn, context);
                PushChildren(node, context);
                return;
            default:
                PushChildren(node, context);
                return;
        }
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Names (ECMA-262 clause 13.1.1; catalogue rows EE0005 to EE0014, EE0140).

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=BE3932
    // Hairs-Human:        PENDING
    private enum NameUse
    {
        Reference,
        Binding,
        Label,
    }

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=5AF585
    // Hairs-Human:        PENDING
    private void CheckName(string name, bool escaped, SyntaxNode at, NameUse use, Context context)
    {
        // A static block reads its statements under [+Await]: an `await` BindingIdentifier there is EE0009's in either goal,
        // an escaped name EE0011's or EE0013's (below), and EE0140 ("Contains `await`", which looks into no function and
        // finds no `await` terminal in an escaped name) the unescaped reference's or label's alone. (Revised
        // 2026-11-02, at stage SJL-13.2f, by part A's triage: every `await` name in a static block was refused EE0140, so that
        // `function await() {}` there, which EE0140 does not reach, was refused by it.)
        if (context.InStaticBlock && name == "await" && !escaped && use != NameUse.Binding)
        {
            Report(Rows.EE0140, at, name);
            return;
        }

        if (context.InStaticBlock && name == "await" && !escaped)
        {
            Report(Rows.EE0009, at, name);
            return;
        }

        if (escaped && name is not ("yield" or "await") && Reserved.Contains(name))
        {
            Report(Rows.EE0014, at, name);
            return;
        }

        if (name == "yield")
        {
            if (context.Yield && use != NameUse.Label)
                Report(escaped ? Rows.EE0010 : use == NameUse.Binding ? Rows.EE0008 : Rows.EE0010, at, name);
            else if (context.Yield)
                Report(Rows.EE0010, at, name);
            else if (context.Strict)
                Report(escaped ? Rows.EE0012 : Rows.EE0006, at, name);
            return;
        }

        if (name == "await")
        {
            if (isModule)
                Report(escaped ? Rows.EE0013 : Rows.EE0007, at, name);
            else if (context.Await)
                Report(!escaped && use == NameUse.Binding ? Rows.EE0009 : Rows.EE0011, at, name);
            return;
        }

        if (context.Strict && StrictReserved.Contains(name))
        {
            Report(Rows.EE0012, at, name);
            return;
        }

        if (use == NameUse.Binding && context.Strict && name is "eval" or "arguments")
            Report(Rows.EE0005, at, name);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Literals.

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=144201
    // Hairs-Human:        PENDING
    private void ValidateRegularExpression(RegularExpressionLiteral literal)
    {
        // The provider decides IsValidRegularExpressionLiteral (decision D7), once per literal, in source order; the library
        // reports a refusal at the literal's span, by the row whose message identifier the verdict names, or EE0019's.
        var verdict = request.RegularExpressions.Validate(literal.Pattern, literal.Flags);
        if (verdict.Status == RegularExpressionStatus.Valid)
            return;
        var row = verdict.Status == RegularExpressionStatus.Invalid && verdict.MessageId is { } id ? RegularExpressionRow(id) : null;
        Report(row ?? Rows.EE0019, literal, row is null ? verdict.MessageId ?? string.Empty : verdict.Argument ?? string.Empty);
    }

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=1D75B6
    // Hairs-Human:        PENDING
    private static LexicalRefusal? RegularExpressionRow(string messageId)
    {
        foreach (var row in RegularExpressionRows)
        {
            if (row.MessageId == messageId)
                return row;
        }

        return null;
    }

    /// <summary>The rows a regular-expression literal's provider decides (the stage table's basis <c>provider</c>), in catalogue order.</summary>
    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=DF2A2A
    // Hairs-Human:        PENDING
    internal static readonly System.Collections.Immutable.ImmutableArray<LexicalRefusal> RegularExpressionRows =
    [
        Rows.EE0019, Rows.EE0183, Rows.EE0184, Rows.EE0185, Rows.EE0186, Rows.EE0187, Rows.EE0188, Rows.EE0189, Rows.EE0190,
        Rows.EE0191, Rows.EE0192, Rows.EE0193, Rows.EE0194, Rows.EE0195, Rows.EE0196, Rows.EE0197, Rows.EE0198, Rows.EE0199,
        Rows.EE0200, Rows.EE0201, Rows.EE0202, Rows.EE0203, Rows.EE0204, Rows.EE0205, Rows.EE0206, Rows.EE0207, Rows.EE0208,
        Rows.EE0209, Rows.EE0210, Rows.EE0211, Rows.EE0212, Rows.EE0213, Rows.EG0002,
    ];

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=EB66B0
    // Hairs-Human:        PENDING
    private void CheckTemplateEscapes(TemplateLiteral template)
    {
        // An untagged template's NotEscapeSequence (EE0020, EE0022 to EE0024): its piece has no cooked value.
        var count = template.Quasis.Count;
        for (var i = 0; i < count; i++)
        {
            if (template.Quasis[i].Cooked is not null)
                continue;
            var row = count == 1 ? Rows.EE0020 : i == 0 ? Rows.EE0022 : i == count - 1 ? Rows.EE0023 : Rows.EE0024;
            Report(row, template.Quasis[i]);
        }
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Functions (clauses 15.1 to 15.9).

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=D84F48
    // Hairs-Human:        PENDING
    private void VisitFunction(FunctionNode function, Context outer)
    {
        var shape = ShapeOf(function);
        var body = function.Body;
        var useStrict = !function.IsArrow || function.ExpressionBody is null ? HasUseStrict(body) : false;
        var strict = outer.Strict || useStrict;
        var simple = IsSimpleParameterList(function.Parameters);

        var parameterNames = new List<string>();
        var parameterNodes = new List<BindingIdentifier>();
        foreach (var parameter in function.Parameters)
            BoundNames(parameter, parameterNames, parameterNodes);
        AddFunction(function.Span.Offset, new FunctionFacts(function, strict, parameterNames.AsReadOnly(), KindOf(function, shape), function.IsAsync,
            function.IsGenerator, simple, !simple && ContainsExpression(function.Parameters)));

        // The model's: a named function expression's funcEnv, then the parameters' scope, both before the body's; the name
        // in the funcEnv, or for a declaration in the scope that binds it (clauses 15.2.5, 10.2.11).
        Scope? parameterScope = null;
        if (model)
        {
            Scope? nameScope = null;
            if (function.Name is { } ownName && !declaredFunctions.Contains(function))
            {
                var own = new Declaration(ownName.Name, BindingKind.FunctionName, ownName, false, false, false);
                nameScope = AddModelScope(function.Span.Offset, new Scope(ScopeKind.FunctionName, function, Array.AsReadOnly(new[] { own })), outer.ModelScope);
            }

            if (function.Name is { } recorded)
                Record(recorded, nameScope ?? outer.ModelScope!);
            parameterScope = AddModelScope(function.Span.Offset, new Scope(ScopeKind.Parameters, function, ParameterDeclarations(parameterNodes)), nameScope ?? outer.ModelScope);
        }

        // The name: a declaration's binds in the enclosing context, an expression's in its own (clauses 15.2 to 15.8).
        if (function.Name is { } name)
        {
            skippedNames.Add(name);
            var nameContext = declaredFunctions.Contains(function)
                ? outer with { Strict = strict }
                : outer with { Strict = strict, Yield = function.IsGenerator, Await = function.IsAsync, InStaticBlock = false };
            if (strict && name.Name is "eval" or "arguments" && StrictName(shape) is { } nameRow)
                Report(nameRow, name, name.Name);
            else
                CheckName(name.Name, name.IsEscaped, name, NameUse.Binding, nameContext);
        }

        // "use strict" in a function whose parameters are not simple (EE0072 and its shapes).
        if (useStrict && !simple && UseStrictRow(shape) is { } useStrictRow)
            Report(useStrictRow, FirstDirective(body)!, "use strict");

        // Duplicate parameters (EE0068 to EE0070, EE0090, EE0097, EE0111, EE0147).
        if (DuplicateParameterRow(shape, simple, strict) is { } duplicateRow)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var reported = new HashSet<string>(StringComparer.Ordinal);
            foreach (var parameter in parameterNodes)
            {
                if (!seen.Add(parameter.Name) && reported.Add(parameter.Name))
                    Report(duplicateRow, parameter, parameter.Name);
            }
        }

        // The body's scope, closed at the function's end: its own conflicts, and a parameter also lexically declared.
        var lexical = new List<Declaration>();
        var vars = new List<Declaration>();
        CollectList(body, top: true, module: false, lexical, vars);
        var bodyScope = AddScope(function.Span.Offset, new Scope(ScopeKind.Function, function, Merge(lexical, vars)), parameterScope);
        var end = End(function);
        CheckConflicts(lexical, vars, end, Rows.EE0078, Rows.EE0079);
        if (ParameterCollisionRow(shape, function) is { } collisionRow)
        {
            // Each such name once, at its first lexical declaration, which follows the parameter.
            var parameterSet = new HashSet<string>(parameterNames, StringComparer.Ordinal);
            foreach (var declaration in lexical)
            {
                if (parameterSet.Remove(declaration.Name))
                    ReportConflict(collisionRow, declaration.Node, end, declaration.Name);
            }
        }

        // An arrow's parameters keep the enclosing [Yield] (ArrowParameters[?Yield], and `async x =>`'s
        // AsyncArrowBindingIdentifier[?Yield]), except an async arrow's parenthesised head, whose ArrowFormalParameters are
        // [~Yield] (clauses 15.3 and 15.9).
        var isArrow = function.IsArrow;
        var parameterContext = new Context
        {
            Strict = strict,
            Yield = isArrow ? outer.Yield && (!function.IsAsync || !IsParenthesizedAsyncHead(function)) : function.IsGenerator,
            Await = isArrow ? function.IsAsync || outer.Await : function.IsAsync,
            InFunction = isArrow ? outer.InFunction : true,
            ParamsOf = shape,
            Labels = null,
            LabelOwner = LabelOwner.Function,
            ModelScope = parameterScope,

            // The model's rows: an arrow's parameters and body are its enclosing code's for Contains and ContainsArguments,
            // but, as every function's, a boundary for jumps (added 2026-11-25, at SJL-15.2 stage 3); every function's carry
            // the private names around it (added 2026-11-25, at SJL-15.2 stage 4).
            Semantic = isArrow ? (semantics ? outer.Semantic! with { Breakables = null, CurrentLabels = null } : null) : semantics ? FunctionSemantic(function, shape, inBody: false) with { Privates = outer.Semantic!.Privates } : null,
        };
        var bodyContext = parameterContext with
        {
            Yield = !isArrow && function.IsGenerator,
            Await = function.IsAsync,
            ParamsOf = null,
            ModelScope = model ? bodyScope : null,
            Semantic = isArrow ? (semantics ? outer.Semantic! with { Breakables = null, CurrentLabels = null } : null) : semantics ? FunctionSemantic(function, shape, inBody: true) with { Privates = outer.Semantic!.Privates } : null,
        };

        Push(function.ExpressionBody, bodyContext);
        PushAll(body, bodyContext);
        PushAll(function.Parameters, parameterContext);
    }

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=08E36D
    // Hairs-Human:        PENDING
    private Shape ShapeOf(FunctionNode function)
    {
        if (shapes.TryGetValue(function, out var shape))
            return shape;
        if (function.IsArrow)
            return function.IsAsync ? Shape.AsyncArrow : Shape.Arrow;
        return (function.IsAsync, function.IsGenerator) switch
        {
            (false, false) => Shape.Function,
            (false, true) => Shape.Generator,
            (true, false) => Shape.AsyncFunction,
            _ => Shape.AsyncGenerator,
        };
    }

    /// <summary>A function's kind, from its shape and, for a class's constructor, the class visit (the session's decision D-js-function-kind).</summary>
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s10.2 function objects ThisMode ConstructorKind, s15.7.14 ClassDefinitionEvaluation, JSD-0080 function kind; IP=Low; Security=Medium; Resources=1; Fingerprint=56CC21
    // Hairs-Human:        PENDING
    private FunctionKind KindOf(FunctionNode function, Shape shape)
    {
        if (constructorKinds is not null && constructorKinds.TryGetValue(function, out var constructor))
            return constructor;
        return shape switch
        {
            Shape.Arrow or Shape.AsyncArrow => FunctionKind.Arrow,
            Shape.Method or Shape.GeneratorMethod or Shape.AsyncMethod or Shape.AsyncGeneratorMethod => FunctionKind.Method,
            Shape.Getter => FunctionKind.Getter,
            Shape.Setter => FunctionKind.Setter,
            _ => FunctionKind.Normal,
        };
    }

    /// <summary>
    /// ContainsExpression of FormalParameters (clause 15.1.2): an initialiser, or a computed property name in a binding
    /// pattern, anywhere in them; iterative, since a pattern's nesting is the request's to bound.
    /// </summary>
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s15.1.2 ContainsExpression; IP=Low; Security=Medium; Resources=1; Fingerprint=4FCDDE
    // Hairs-Human:        PENDING
    private static bool ContainsExpression(IReadOnlyList<Pattern> parameters)
    {
        var pending = new Stack<SyntaxNode>();
        foreach (var parameter in parameters)
            pending.Push(parameter);
        while (pending.Count > 0)
        {
            switch (pending.Pop())
            {
                case AssignmentPattern:
                    return true;
                case RestElement rest:
                    pending.Push(rest.Argument);
                    break;
                case ArrayPattern array:
                    foreach (var element in array.Elements)
                    {
                        if (element is not null)
                            pending.Push(element);
                    }

                    break;
                case ObjectPattern @object:
                    foreach (var property in @object.Properties)
                    {
                        if (property is PatternProperty patternProperty)
                        {
                            if (patternProperty.Key is ComputedPropertyName)
                                return true;
                            pending.Push(patternProperty.Value);
                        }
                        else
                        {
                            pending.Push(property);
                        }
                    }

                    break;
            }
        }

        return false;
    }

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=8476A8
    // Hairs-Human:        PENDING
    private static Shape MethodShape(MethodKind kind, FunctionNode function) => kind switch
    {
        MethodKind.Get => Shape.Getter,
        MethodKind.Set => Shape.Setter,
        _ => (function.IsAsync, function.IsGenerator) switch
        {
            (false, false) => Shape.Method,
            (false, true) => Shape.GeneratorMethod,
            (true, false) => Shape.AsyncMethod,
            _ => Shape.AsyncGeneratorMethod,
        },
    };

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=F87D22
    // Hairs-Human:        PENDING
    private static LexicalRefusal? StrictName(Shape shape) => shape switch
    {
        Shape.Function => Rows.EE0071,
        Shape.Generator => Rows.EE0098,
        Shape.AsyncGenerator => Rows.EE0112,
        Shape.AsyncFunction => Rows.EE0148,
        _ => null,
    };

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=9D5983
    // Hairs-Human:        PENDING
    private static LexicalRefusal? UseStrictRow(Shape shape) => shape switch
    {
        Shape.Function => Rows.EE0072,
        Shape.Generator => Rows.EE0099,
        Shape.AsyncFunction => Rows.EE0145,
        Shape.AsyncGenerator => Rows.EE0113,
        Shape.Method => Rows.EE0088,
        Shape.GeneratorMethod => Rows.EE0095,
        Shape.AsyncMethod => Rows.EE0141,
        Shape.AsyncGeneratorMethod => Rows.EE0109,
        Shape.Setter => Rows.EE0091,
        Shape.Arrow => Rows.EE0085,
        Shape.AsyncArrow => Rows.EE0159,
        _ => null,
    };

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=DF1DA7
    // Hairs-Human:        PENDING
    private static LexicalRefusal? DuplicateParameterRow(Shape shape, bool simple, bool strict) => shape switch
    {
        Shape.Function or Shape.Generator or Shape.AsyncFunction or Shape.AsyncGenerator when !simple => Rows.EE0069,
        Shape.Function when strict => Rows.EE0070,
        Shape.Generator when strict => Rows.EE0097,
        Shape.AsyncGenerator when strict => Rows.EE0111,
        Shape.AsyncFunction when strict => Rows.EE0147,
        Shape.Function or Shape.Generator or Shape.AsyncFunction or Shape.AsyncGenerator => null,
        Shape.Setter => Rows.EE0090,
        Shape.Getter => null,
        _ => Rows.EE0068,
    };

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=826D9A
    // Hairs-Human:        PENDING
    private LexicalRefusal? ParameterCollisionRow(Shape shape, FunctionNode function) => shape switch
    {
        Shape.Function => Rows.EE0073,
        Shape.Generator => Rows.EE0100,
        Shape.AsyncFunction => Rows.EE0149,
        Shape.AsyncGenerator => Rows.EE0114,
        Shape.Method => Rows.EE0089,
        Shape.GeneratorMethod => Rows.EE0096,
        Shape.AsyncMethod => Rows.EE0144,
        Shape.AsyncGeneratorMethod => Rows.EE0110,
        Shape.Setter => Rows.EE0092,
        Shape.Arrow => Rows.EE0086,
        Shape.AsyncArrow => IsParenthesizedAsyncHead(function) ? Rows.EE0158 : Rows.EE0154,
        _ => null,
    };

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=39E703
    // Hairs-Human:        PENDING
    private bool IsParenthesizedAsyncHead(FunctionNode function)
    {
        // `async x => ...` binds an AsyncArrowBindingIdentifier (EE0154); `async (...) => ...` a cover (EE0158).
        if (function.Parameters.Count != 1 || function.Parameters[0] is not BindingIdentifier parameter)
            return true;
        var text = request.Source.Text;
        var from = function.Span.Offset - request.Source.BaseOffset;
        var to = parameter.Span.Offset - request.Source.BaseOffset;
        for (var i = Math.Max(0, from); i < to && i < text.Length; i++)
        {
            if (text[i] == '(')
                return true;
        }

        return false;
    }

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=613732
    // Hairs-Human:        PENDING
    private static LexicalRefusal? YieldInParameters(Shape shape) => shape switch
    {
        Shape.Generator => Rows.EE0101,
        Shape.GeneratorMethod => Rows.EE0094,
        Shape.AsyncGenerator => Rows.EE0115,
        Shape.AsyncGeneratorMethod => Rows.EE0107,
        Shape.Arrow => Rows.EE0083,
        Shape.AsyncArrow => Rows.EE0156,
        _ => null,
    };

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=C2B62D
    // Hairs-Human:        PENDING
    private static LexicalRefusal? AwaitInParameters(Shape shape) => shape switch
    {
        Shape.AsyncFunction => Rows.EE0146,
        Shape.AsyncMethod => Rows.EE0143,
        Shape.AsyncGenerator => Rows.EE0116,
        Shape.AsyncGeneratorMethod => Rows.EE0108,
        Shape.Arrow => Rows.EE0084,
        Shape.AsyncArrow => Rows.EE0157,
        _ => null,
    };

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=19B0D5
    // Hairs-Human:        PENDING
    private static bool IsSimpleParameterList(IReadOnlyList<Pattern> parameters)
    {
        foreach (var parameter in parameters)
        {
            if (parameter is not BindingIdentifier)
                return false;
        }

        return true;
    }

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=081B31
    // Hairs-Human:        PENDING
    private static bool HasUseStrict(IReadOnlyList<Statement> body) => FirstUseStrict(body) is not null;

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=12913B
    // Hairs-Human:        PENDING
    private static ExpressionStatement? FirstUseStrict(IReadOnlyList<Statement> body)
    {
        foreach (var statement in body)
        {
            if (statement is not ExpressionStatement { Directive: { } directive } expression)
                return null;
            if (directive == "use strict")
                return expression;
        }

        return null;
    }

    private static ExpressionStatement? FirstDirective(IReadOnlyList<Statement> body) => FirstUseStrict(body);

    // ---------------------------------------------------------------------------------------------------------------
    // Classes (clause 15.7) and object literals' methods (clause 13.2.5).

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=7E8ABA
    // Hairs-Human:        PENDING
    private void VisitClass(ClassNode @class, Context outer)
    {
        // The model's: every class's classEnv, holding its inner name, made at the class (clause 15.7.14).
        Scope? classScope = null;
        if (model)
        {
            var inner = @class.Name is { } innerName
                ? Array.AsReadOnly(new[] { new Declaration(innerName.Name, BindingKind.ClassName, innerName, false, false, false) })
                : (IReadOnlyList<Declaration>)Array.Empty<Declaration>();
            classScope = AddModelScope(@class.Span.Offset, new Scope(ScopeKind.ClassBody, @class, inner), outer.ModelScope);
        }

        var context = model ? outer with { Strict = true, ModelScope = classScope } : outer with { Strict = true };
        var constructors = 0;
        var constructorMethodSeen = false;
        var privateNames = new Dictionary<string, (bool IsStatic, MethodKind? Kind)>(StringComparer.Ordinal);
        foreach (var element in @class.Elements)
        {
            switch (element)
            {
                case MethodDefinition method:
                {
                    shapes[method.Function] = MethodShape(method.MethodKind, method.Function);
                    var name = PropName(method.Key);
                    var special = method.MethodKind != MethodKind.Method || method.Function.IsAsync || method.Function.IsGenerator;

                    // The model's: the method's home, the class's ConstructorMethod being its first element named
                    // "constructor", special or not (clause 15.7.1, ClassTail; ClassBody's ConstructorMethod).
                    if (semantics)
                    {
                        var home = method.IsStatic ? MethodHome.StaticClass
                            : name != "constructor" ? MethodHome.Class
                            : constructorMethodSeen ? MethodHome.DuplicateConstructor
                            : @class.Heritage is null ? MethodHome.BaseConstructor : MethodHome.DerivedConstructor;
                        constructorMethodSeen |= home is MethodHome.BaseConstructor or MethodHome.DerivedConstructor;
                        methodHomes![method.Function] = home;
                    }

                    if (!method.IsStatic && name == "constructor")
                    {
                        if (special)
                            Report(Rows.EE0125, method.Key, name);
                        else if (++constructors > 1)
                            Report(Rows.EE0122, method.Key, name);

                        // Its ConstructorMethod: base, or derived where the class has a heritage (clause 15.7.14).
                        if (!special)
                            (constructorKinds ??= new(ReferenceEqualityComparer.Instance))[method.Function] = @class.Heritage is null ? FunctionKind.ClassConstructor : FunctionKind.DerivedConstructor;
                    }

                    if (method.IsStatic && name == "prototype")
                        Report(Rows.EE0127, method.Key, name);
                    CheckPrivateName(method.Key, method.IsStatic, method.MethodKind, privateNames);
                    break;
                }

                case FieldDefinition field:
                {
                    var name = PropName(field.Key);
                    if (!field.IsStatic && name == "constructor")
                        Report(Rows.EE0128, field.Key, name);
                    if (field.IsStatic && name is "prototype" or "constructor")
                        Report(Rows.EE0129, field.Key, name);
                    CheckPrivateName(field.Key, field.IsStatic, null, privateNames);

                    // The model's: an initialiser's facts, as a method's of the class (clause 15.7.10).
                    if (model && field.Value is not null)
                        AddModelFunction(field.Span.Offset, new FunctionFacts(field, true, Array.Empty<string>(), FunctionKind.FieldInitializer, false, false, true, false));
                    break;
                }

                case StaticBlock block when model:
                    // The model's: a static block's facts, as a method's of the class (clause 15.7.11).
                    AddModelFunction(block.Span.Offset, new FunctionFacts(block, true, Array.Empty<string>(), FunctionKind.StaticBlock, false, false, true, false));
                    break;
            }
        }

        // The model's rows and fact (SJL-15.2 stage 4): the class's private names, in source order, once each, read by its
        // elements' references, which the elements' context carries; the heritage, the name and the class's decorators keep
        // the names around the class (AllPrivateIdentifiersValid; ClassDefinitionEvaluation evaluates the heritage in the
        // outer private environment). With the model on, the class's environment and its bindings.
        List<string>? names = null;
        PrivateEnvironment? environment = null;
        if (semantics)
        {
            names = new List<string>();
            environment = CollectPrivateNames(@class, outer, names);
        }

        var elementContext = semantics ? context with { InStaticBlock = false, Semantic = context.Semantic! with { Privates = new PrivateScope(names!, environment, context.Semantic!.Privates) } } : context with { InStaticBlock = false };
        for (var i = @class.Elements.Count - 1; i >= 0; i--)
        {
            switch (@class.Elements[i])
            {
                case FieldDefinition field:
                    if (model)
                    {
                        // The model's: the field and a key the walk does not visit are in the class's scope.
                        Record(field, classScope!);
                        if (field.Key is not ComputedPropertyName)
                            RecordSubtree(field.Key, classScope!);
                    }

                    // A field's initialiser is read under [~Yield, ~Await] (design section 9.3), inside a function boundary.
                    Push(field.Value, elementContext with
                    {
                        Yield = false,
                        Await = false,
                        InFunction = true,
                        ParamsOf = null,
                        Labels = null,
                        LabelOwner = LabelOwner.Function,

                        // The model's rows: an initialiser admits a SuperProperty, not a SuperCall (EE0131), and anchors
                        // ContainsArguments (EE0130) at the field, inside every member that encloses it.
                        Semantic = semantics ? new SemanticContext(null, SemanticRows.EE0131, new ArgumentsAnchors(SemanticRows.EE0130, field, elementContext.Semantic!.Arguments)) { Privates = elementContext.Semantic!.Privates } : null,
                    });
                    if (field.Key is ComputedPropertyName)
                        Push(field.Key, elementContext);
                    PushAll(field.Decorators, elementContext);
                    break;
                case StaticBlock block:
                    VisitStaticBlock(block, elementContext);
                    break;
                default:
                    Push(@class.Elements[i], elementContext);
                    break;
            }
        }

        Push(@class.Heritage, context);
        if (@class.Name is { } className)
        {
            // The model's: a declaration's name is in the scope that binds it, an expression's in the class's (clause 15.7.14).
            Push(className, model && declaredClasses!.Contains(@class) ? context with { ModelScope = outer.ModelScope } : context);
        }

        PushAll(@class.Decorators, outer);
    }

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=233DB0
    // Hairs-Human:        PENDING
    private void VisitStaticBlock(StaticBlock block, Context outer)
    {
        var lexical = new List<Declaration>();
        var vars = new List<Declaration>();
        CollectList(block.Body, top: true, module: false, lexical, vars);
        var blockScope = AddScope(block.Span.Offset, new Scope(ScopeKind.StaticBlock, block, Merge(lexical, vars)), outer.ModelScope);
        if (model)
            Record(block, outer.ModelScope!);
        CheckConflicts(lexical, vars, End(block), Rows.EE0133, Rows.EE0134);
        var context = outer with
        {
            Yield = false,
            Await = true,
            InFunction = true,
            InStaticBlock = true,
            ParamsOf = null,
            Labels = null,
            LabelOwner = LabelOwner.StaticBlock,
            ModelScope = model ? blockScope : null,

            // The model's rows: a static block admits a SuperProperty, not a SuperCall (EE0139), and anchors
            // ContainsArguments (EE0138) at the block, inside every member that encloses it.
            Semantic = semantics ? new SemanticContext(null, SemanticRows.EE0139, new ArgumentsAnchors(SemanticRows.EE0138, block, outer.Semantic!.Arguments)) { Privates = outer.Semantic!.Privates } : null,
        };
        PushAll(block.Body, context);
    }

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=3111FE
    // Hairs-Human:        PENDING
    private void CheckPrivateName(SyntaxNode key, bool isStatic, MethodKind? kind, Dictionary<string, (bool IsStatic, MethodKind? Kind)> seen)
    {
        if (key is not PrivateIdentifier privateName)
            return;
        if (privateName.Name == "#constructor" || privateName.Name == "constructor")
        {
            Report(Rows.EE0132, key, privateName.Name);
            return;
        }

        if (!seen.TryGetValue(privateName.Name, out var earlier))
        {
            seen[privateName.Name] = (isStatic, kind);
            return;
        }

        // A getter and a setter of one name, of one placement, may share it (EE0123's exception).
        var pair = earlier.IsStatic == isStatic && earlier.Kind is MethodKind.Get or MethodKind.Set && kind is MethodKind.Get or MethodKind.Set && earlier.Kind != kind;
        if (pair)
            seen[privateName.Name] = (isStatic, null);
        else
            Report(Rows.EE0123, key, privateName.Name);
    }

    /// <summary>
    /// A class's private names (SJL-15.2 stage 4; PrivateBoundIdentifiers of its ClassBody): the key of every method, getter,
    /// setter, field and auto-accessor that is a <see cref="PrivateIdentifier"/>, in source order, once each,
    /// <c>#constructor</c> among them, added to <paramref name="names"/>; a pass of its own, which leaves EE0123's and
    /// EE0132's dictionary as it is. With the model on, the class's environment, whose outer one is the innermost class
    /// environment around the class, and its bindings: a getter and a setter of one name and one placement one binding, the
    /// first occurrence of any other duplicate (which EE0123 refuses) the binding; each declaring key mapped to its binding.
    /// It numbers nothing in the walk's sequence.
    /// </summary>
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 PrivateBoundIdentifiers, s15.7.14 ClassDefinitionEvaluation, PrivateEnvironment Records, SJL-15.2 semantics rows, JSD-0080 private names; IP=Low; Security=Medium; Resources=1; Fingerprint=631988
    // Hairs-Human:        PENDING
    private PrivateEnvironment? CollectPrivateNames(ClassNode @class, Context outer, List<string> names)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        PrivateEnvironment? environment = null;
        List<(string Name, PrivateNameKind Kind, bool IsStatic, List<SyntaxNode> Elements)>? declared = null;
        Dictionary<string, (int Index, MethodKind? Pairable)>? byName = null;
        List<(PrivateIdentifier Key, int Index)>? keys = null;
        if (model)
        {
            PrivateEnvironment? around = null;
            for (var scope = outer.Semantic!.Privates; scope is not null && around is null; scope = scope.Outer)
                around = scope.Environment;
            environment = new PrivateEnvironment(@class, around);
            declared = new();
            byName = new(StringComparer.Ordinal);
            keys = new();
        }

        foreach (var element in @class.Elements)
        {
            var (key, isStatic, kind, method) = element switch
            {
                MethodDefinition m => (m.Key, m.IsStatic, m.MethodKind == MethodKind.Method ? PrivateNameKind.Method : PrivateNameKind.Accessor, (MethodKind?)m.MethodKind),
                FieldDefinition f => (f.Key, f.IsStatic, f.IsAccessor ? PrivateNameKind.Accessor : PrivateNameKind.Field, (MethodKind?)null),
                _ => ((SyntaxNode?)null, false, PrivateNameKind.Field, (MethodKind?)null),
            };
            if (key is not PrivateIdentifier privateKey)
                continue;
            if (seen.Add(privateKey.Name))
                names.Add(privateKey.Name);
            if (!model)
                continue;

            if (!byName!.TryGetValue(privateKey.Name, out var earlier))
            {
                byName[privateKey.Name] = (declared!.Count, method is MethodKind.Get or MethodKind.Set ? method : null);
                keys!.Add((privateKey, declared.Count));
                declared.Add((privateKey.Name, kind, isStatic, new List<SyntaxNode> { element }));
                continue;
            }

            // A getter and a setter of one placement share one binding (EE0123's exception); a third element does not.
            var first = declared![earlier.Index];
            if (earlier.Pairable is { } pairable && method is MethodKind.Get or MethodKind.Set && method != pairable && first.IsStatic == isStatic)
            {
                first.Elements.Add(element);
                byName[privateKey.Name] = (earlier.Index, null);
            }

            keys!.Add((privateKey, earlier.Index));
        }

        if (model)
        {
            var bindings = new PrivateNameBinding[declared!.Count];
            for (var i = 0; i < bindings.Length; i++)
                bindings[i] = new PrivateNameBinding(declared[i].Name, environment!, declared[i].Kind, declared[i].IsStatic, Array.AsReadOnly(declared[i].Elements.ToArray()));
            environment!.Seal(bindings);
            foreach (var (key, index) in keys!)
                privateOf![key] = bindings[index];
            privateEnvironmentOf![@class] = environment;
        }

        return environment;
    }

    /// <summary>
    /// A private name's reference (SJL-15.2 stage 4; AllPrivateIdentifiersValid): its binding in the innermost class whose
    /// names hold it, recorded with the model on; at eval code's root, a name its caller sees, which has no binding in the
    /// unit; otherwise the request's goal's row (EE0167, EE0177, EA0009), at the member or <c>in</c> expression, with the
    /// name and its <c>#</c> as its argument, once per reference. Whether the reference or the declaration is static plays
    /// no part.
    /// </summary>
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 AllPrivateIdentifiersValid, ResolvePrivateIdentifier, s16.1.1 s16.2.1.1 s19.2.1.3 early errors, SJL-15.2 semantics rows; IP=Low; Security=Medium; Resources=1; Fingerprint=CE4FB6
    // Hairs-Human:        PENDING
    private void CheckPrivateReference(PrivateIdentifier name, SyntaxNode at, Context context)
    {
        for (var scope = context.Semantic!.Privates; scope is not null; scope = scope.Outer)
        {
            if (!scope.Holds(name.Name))
                continue;
            if (model && scope.Environment is { } environment)
                privateOf![name] = environment.Find(name.Name)!;
            return;
        }

        if (undeclaredPrivateRow is { } row)
            Report(row, at, name.Name);
    }

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=BC2B3E
    // Hairs-Human:        PENDING
    private static string? PropName(SyntaxNode key) => key switch
    {
        IdentifierName identifier => identifier.Name,
        StringLiteral text => text.Value,
        _ => null,
    };

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=D4357B
    // Hairs-Human:        PENDING
    private void VisitProperty(PropertyDefinition property, Context context)
    {
        if (property.PropertyKind is PropertyKind.Method or PropertyKind.Get or PropertyKind.Set && property.Value is FunctionExpression { Function: { } method })
        {
            var kind = property.PropertyKind switch
            {
                PropertyKind.Get => MethodKind.Get,
                PropertyKind.Set => MethodKind.Set,
                _ => MethodKind.Method,
            };
            shapes[method] = MethodShape(kind, method);
            if (semantics)
                methodHomes![method] = MethodHome.ObjectLiteral;

            // An object literal's method may not have a private name (EE0016).
            if (property.Key is PrivateIdentifier privateKey)
                Report(Rows.EE0016, privateKey, privateKey.Name);
        }

        PushChildren(property, context);
    }

    /// <summary>
    /// An object literal's duplicate <c>__proto__</c> entries (EE0018, clause 13.2.5.1): every <c>PropertyName : value</c>
    /// entry whose non-computed PropName is <c>"__proto__"</c>, after the first, at the entry, as Broiler.VM's binder places
    /// it. Shorthand, method and computed entries do not count; a literal the grammar converted to a pattern is none.
    /// </summary>
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s13.2.5.1 ObjectLiteral early errors PropertyNameList __proto__; IP=Low; Security=Medium; Resources=1; Fingerprint=9D23B3
    // Hairs-Human:        PENDING
    private void CheckProtoEntries(ObjectLiteral literal)
    {
        var seen = false;
        foreach (var entry in literal.Properties)
        {
            if (entry is not PropertyDefinition { PropertyKind: PropertyKind.Init } definition || definition.Key is ComputedPropertyName || PropName(definition.Key) != "__proto__")
                continue;
            if (seen)
                Report(SemanticRows.EE0018, definition, "__proto__");
            seen = true;
        }
    }

    /// <summary>
    /// ContainsArguments where an <c>arguments</c> reference stands (SJL-15.2 stage 2): for each anchor that encloses it,
    /// innermost first, the field's or static block's row once per member, at the member (EE0130, EE0138, decision D11's
    /// place, as Broiler.VM's binder reports it), and eval code's in a field initialiser at the reference (EA0005).
    /// </summary>
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s15.7.1 ContainsArguments early errors, s19.2.1.1 PerformEval step 11.h; IP=Low; Security=Medium; Resources=1; Fingerprint=D40D9B
    // Hairs-Human:        PENDING
    private void ReportArguments(Identifier identifier, Context context)
    {
        for (var anchor = context.Semantic!.Arguments; anchor is not null; anchor = anchor.Outer)
        {
            if (anchor.At is null)
                Report(anchor.Row, identifier, "arguments");
            else if (argumentsReported!.Add(anchor.At))
                Report(anchor.Row, anchor.At, "arguments");
        }
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Expressions.

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=89F333
    // Hairs-Human:        PENDING
    private void CheckMetaProperty(MetaProperty meta, Context context)
    {
        if (meta.Meta.Name == "new" && !context.InFunction)
        {
            // new.target outside every non-arrow function (EE0163, EE0173; EA0002 in eval code outside a function).
            var row = isModule ? Rows.EE0173 : request.Goal == ParseGoal.Eval ? Rows.EA0002 : Rows.EE0163;
            Report(row, meta, "new.target");
        }
        else if (meta.Meta.Name == "import" && !isModule)
        {
            Report(Rows.EE0027, meta, "import.meta");
        }
    }

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=BBFEE4
    // Hairs-Human:        PENDING
    private enum TargetType
    {
        Simple,
        WebCompat,
        Invalid,
    }

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=FDFAF2
    // Hairs-Human:        PENDING
    private TargetType TargetTypeOf(Expression expression, Context context)
    {
        while (expression is ParenthesizedExpression parenthesized)
        {
            expression = parenthesized.Expression;
            if (expression is ObjectLiteral or ArrayLiteral)
                return TargetType.Invalid;
        }

        return expression switch
        {
            Identifier identifier => context.Strict && identifier.Name is "eval" or "arguments" ? TargetType.Invalid : TargetType.Simple,
            MemberExpression { IsOptional: false } => TargetType.Simple,

            // Annex B.3.9: a call as an assignment target in non-strict code is refused when it is evaluated, not early.
            CallExpression { IsOptional: false } when annexB && !context.Strict => TargetType.WebCompat,
            _ => TargetType.Invalid,
        };
    }

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=359FCA
    // Hairs-Human:        PENDING
    private void CheckAssignment(AssignmentExpression assignment, Context context)
    {
        if (assignment.Target is not Expression target)
            return;
        var type = TargetTypeOf(target, context);
        switch (assignment.Operator)
        {
            case AssignmentOperator.Assign:
                if (type == TargetType.Invalid)
                    Report(Rows.EE0033, target);
                break;
            case AssignmentOperator.AndAssign or AssignmentOperator.OrAssign or AssignmentOperator.CoalesceAssign:
                if (type != TargetType.Simple)
                    Report(Rows.EE0035, target);
                break;
            default:
                if (type == TargetType.Invalid)
                    Report(Rows.EE0034, target);
                break;
        }
    }

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=E07BA1
    // Hairs-Human:        PENDING
    private void CheckDelete(UnaryExpression unary, Context context)
    {
        // delete of a bare name in strict code, or of a private reference (EE0030), and either parenthesised (EE0031).
        var operand = unary.Operand;
        var parenthesized = false;
        while (operand is ParenthesizedExpression inner)
        {
            parenthesized = true;
            operand = inner.Expression;
        }

        if (operand is ChainExpression chain)
            operand = chain.Expression;
        var refused = (operand is Identifier && context.Strict) || operand is MemberExpression { Property: PrivateIdentifier };
        if (refused)
            Report(parenthesized ? Rows.EE0031 : Rows.EE0030, unary, "delete");
    }

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=90D27B
    // Hairs-Human:        PENDING
    private void VisitShorthand(PatternProperty shorthand, Context context)
    {
        // An AssignmentProperty's IdentifierReference must be a simple target (EE0036): eval and arguments in strict code are not.
        var value = shorthand.Value is AssignmentPattern withDefault ? withDefault.Target : shorthand.Value;
        if (value is TargetPattern { Target: Identifier identifier })
        {
            if (model)
            {
                // The model's: the patterns the walk reads through without visiting them.
                Record(shorthand.Value, context.ModelScope!);
                Record(value, context.ModelScope!);
            }

            if (TargetTypeOf(identifier, context) != TargetType.Simple)
                Report(Rows.EE0036, identifier, identifier.Name);
            Push(identifier, context);
            if (shorthand.Value is AssignmentPattern initialised)
                Push(initialised.Initializer, context);
            return;
        }

        PushChildren(shorthand, context);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Statements.

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=D8746B
    // Hairs-Human:        PENDING
    private Scope VisitStatementListScope(SyntaxNode node, IReadOnlyList<Statement> body, ScopeKind kind, LexicalRefusal duplicateRow, LexicalRefusal collisionRow, bool strict, Scope? parent)
    {
        var lexical = new List<Declaration>();
        foreach (var statement in body)
            CollectLexical(statement, lexical, inBlock: true);
        var scope = AddScope(node.Span.Offset, new Scope(kind, node, lexical.AsReadOnly()), parent);
        if (lexical.Count == 0)
            return scope;

        var vars = new List<Declaration>();
        foreach (var statement in body)
            CollectVars(statement, vars);
        CheckConflicts(lexical, vars, End(node), duplicateRow, collisionRow, sloppy: !strict);
        return scope;
    }

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=F44922
    // Hairs-Human:        PENDING
    private void VisitSwitch(SwitchStatement @switch, Context context)
    {
        var all = new List<Statement>();
        foreach (var @case in @switch.Cases)
        {
            all.AddRange(@case.Consequent);
            CheckNoUsing(@case.Consequent, @case.Test is null ? Rows.EP0007 : Rows.EP0006);
        }

        var switchScope = VisitStatementListScope(@switch, all, ScopeKind.Switch, Rows.EE0062, Rows.EE0063, context.Strict, context.ModelScope);
        if (model)
        {
            // The model's: the cases in the CaseBlock's scope, the discriminant outside it (clause 14.12.4).
            var caseContext = context with { ModelScope = switchScope };
            if (semantics)
                caseContext = Inside(caseContext, BreakableKind.Switch, @switch);
            PushAll(@switch.Cases, caseContext);
            Push(@switch.Discriminant, context);
        }
        else
        {
            PushChildren(@switch, semantics ? Inside(context, BreakableKind.Switch, @switch) : context);
        }
    }

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=A16920
    // Hairs-Human:        PENDING
    private void CheckNoUsing(IReadOnlyList<Statement> statements, LexicalRefusal row)
    {
        foreach (var statement in statements)
        {
            if (statement is VariableDeclaration { DeclarationKind: DeclarationKind.Using or DeclarationKind.AwaitUsing } declaration)
                Report(row, declaration, declaration.DeclarationKind == DeclarationKind.Using ? "using" : "await using");
        }
    }

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=C6F0D1
    // Hairs-Human:        PENDING
    private void CheckDeclaration(VariableDeclaration declaration, bool inForInOfHead)
    {
        var kind = declaration.DeclarationKind;
        if (kind == DeclarationKind.Var)
            return;

        var names = new List<string>();
        var nodes = new List<BindingIdentifier>();
        foreach (var declarator in declaration.Declarations)
            BoundNames(declarator.Target, names, nodes);

        // `let` as a lexically bound name (EE0042, EE0055, EP0001, EP0004).
        var letRow = inForInOfHead ? Rows.EE0055 : kind switch
        {
            DeclarationKind.Using => Rows.EP0001,
            DeclarationKind.AwaitUsing => Rows.EP0004,
            _ => Rows.EE0042,
        };
        foreach (var node in nodes)
        {
            if (node.Name == "let")
                Report(letRow, node, "let");
        }

        // A name bound twice (EE0043, EE0057, EP0002, EP0005).
        var duplicateRow = inForInOfHead ? Rows.EE0057 : kind switch
        {
            DeclarationKind.Using => Rows.EP0002,
            DeclarationKind.AwaitUsing => Rows.EP0005,
            _ => Rows.EE0043,
        };
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in nodes)
        {
            if (!seen.Add(node.Name))
                Report(duplicateRow, node, node.Name);
        }

        // A constant declaration (const, using, await using) without an initialiser outside a for-in or for-of head (EE0044).
        if (kind is DeclarationKind.Const or DeclarationKind.Using or DeclarationKind.AwaitUsing && !inForInOfHead)
        {
            foreach (var declarator in declaration.Declarations)
            {
                if (declarator.Initializer is null)
                    Report(Rows.EE0044, declarator, kind == DeclarationKind.Const ? "const" : kind == DeclarationKind.Using ? "using" : "await using");
            }
        }
    }

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=FF94BD
    // Hairs-Human:        PENDING
    private void VisitFor(ForStatement @for, Context context)
    {
        Scope? forScope = null;
        if (@for.Init is VariableDeclaration { DeclarationKind: not DeclarationKind.Var } head)
        {
            var lexical = new List<Declaration>();
            CollectLexical(head, lexical, inBlock: true);
            forScope = AddScope(@for.Span.Offset, new Scope(ScopeKind.For, @for, lexical.AsReadOnly()), context.ModelScope);
            var vars = new List<Declaration>();
            CollectVars(@for.Body, vars);
            CheckCollisions(lexical, vars, End(@for), Rows.EE0051);
        }

        CheckLabelledFunctionBody(@for.Body, Rows.EE0050);
        var forContext = model && forScope is not null ? context with { ModelScope = forScope } : context;
        if (semantics)
            forContext = Inside(forContext, BreakableKind.Iteration, @for);
        PushChildren(@for, forContext);
    }

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=629473
    // Hairs-Human:        PENDING
    private void VisitForInOf(Statement statement, SyntaxNode left, Statement body, Context context)
    {
        // A call as a for-in or for-of target: Annex B.3.9 makes it a run-time refusal in non-strict code only (EG0008).
        if (left is CallExpression { IsOptional: false } call && (context.Strict || !annexB))
            Report(Rows.EG0008, call);

        // The model's row: a head target whose AssignmentTargetType is invalid (EE0054); EG0008 in its place for a
        // non-optional call, which Annex B makes web-compatible in sloppy code; a converted pattern is no expression.
        if (semantics && left is Expression target && left is not CallExpression { IsOptional: false } && TargetTypeOf(target, context) == TargetType.Invalid)
            Report(SemanticRows.EE0054, target);

        if (left is VariableDeclaration { DeclarationKind: not DeclarationKind.Var } head)
        {
            CheckDeclaration(head, inForInOfHead: true);
            var lexical = new List<Declaration>();
            CollectLexical(head, lexical, inBlock: true);
            var forScope = AddScope(statement.Span.Offset, new Scope(ScopeKind.For, statement, lexical.AsReadOnly()), context.ModelScope);
            var vars = new List<Declaration>();
            CollectVars(body, vars);
            CheckCollisions(lexical, vars, End(statement), Rows.EE0056);

            // The model's: the head, the expression and the body in the head's scope (clause 14.7.5.6's TDZ environment
            // holds the same bound names), the head itself recorded, since the walk visits its declarators alone.
            var inner = context;
            if (model)
            {
                inner = context with { ModelScope = forScope };
                Record(head, forScope);
            }

            if (semantics)
                inner = Inside(inner, BreakableKind.Iteration, statement);

            children.Clear();
            statement.AddChildren(children);
            for (var i = children.Count - 1; i >= 0; i--)
            {
                if (ReferenceEquals(children[i], head))
                    PushAll(head.Declarations, inner);
                else
                    stack.Push((children[i], inner));
            }
        }
        else
        {
            PushChildren(statement, semantics ? Inside(context, BreakableKind.Iteration, statement) : context);
        }

        CheckLabelledFunctionBody(body, Rows.EE0052);
    }

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=9881A8
    // Hairs-Human:        PENDING
    private void VisitIf(IfStatement @if, Context context)
    {
        if (@if.Alternate is null)
        {
            CheckLabelledFunctionBody(@if.Consequent, Rows.EE0047);
        }
        else
        {
            CheckLabelledFunctionBody(@if.Consequent, Rows.EE0045);
            CheckLabelledFunctionBody(@if.Alternate, Rows.EE0046);
        }

        // Annex B.3.3: a FunctionDeclaration as an `if` clause is read as if a block held it.
        foreach (var clause in new[] { @if.Consequent, @if.Alternate })
        {
            if (clause is FunctionDeclaration function)
            {
                ifClauseFunctions.Add(function);
                var lexical = new List<Declaration>();
                CollectLexical(function, lexical, inBlock: true);
                var clauseScope = AddScope(function.Span.Offset, new Scope(ScopeKind.Block, function, lexical.AsReadOnly()), context.ModelScope);
                if (model)
                    ifClauseScopes![function] = clauseScope;
            }
        }

        PushChildren(@if, context);
    }

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=004437
    // Hairs-Human:        PENDING
    private void CheckLabelledFunctionBody(Statement body, LexicalRefusal row)
    {
        // IsLabelledFunction: a labelled function, through any number of labels, as a statement's body.
        var statement = body;
        while (statement is LabelledStatement labelled)
            statement = labelled.Body;
        if (body is LabelledStatement && statement is FunctionDeclaration function)
            Report(row, function, "function");
    }

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=AAE59A
    // Hairs-Human:        PENDING
    private void VisitLabelled(LabelledStatement labelled, Context context)
    {
        var name = labelled.Label.Name;
        CheckName(name, labelled.Label.IsEscaped, labelled.Label, NameUse.Label, context);
        if (model)
            RecordSubtree(labelled.Label, context.ModelScope!);
        for (var set = context.Labels; set is not null; set = set.Next)
        {
            if (set.Name == name)
            {
                var row = context.LabelOwner switch
                {
                    LabelOwner.Module => Rows.EE0174,
                    LabelOwner.StaticBlock => Rows.EE0135,
                    LabelOwner.Function => Rows.EE0080,
                    _ => Rows.EE0164,
                };
                Report(row, labelled.Label, name);
                break;
            }
        }

        // A labelled function declaration in strict code, or where Annex B.3.1 does not apply (EE0064).
        if (labelled.Body is FunctionDeclaration function && (context.Strict || !annexB))
            Report(Rows.EE0064, function, "function");

        // The model's (SJL-15.2 stage 3): the label joins the label set of the item it labels, which a further label, a
        // loop or a switch takes over, and which any other item carries as a statement a break may leave.
        var bodyContext = context with { Labels = new LabelSet(name, context.Labels) };
        if (semantics)
            bodyContext = LabelledItem(bodyContext, labelled.Body, new LabelSet(name, context.Semantic!.CurrentLabels));
        Push(labelled.Body, bodyContext);
    }

    /// <summary>
    /// The context of a labelled statement's item (SJL-15.2 stage 3; LabelledEvaluation's labelSet): a further label, an
    /// iteration statement or a <c>switch</c> takes <paramref name="labels"/> as the label set it is labelled by; any other
    /// item is a statement a <c>break</c> naming one of them leaves, with that label set.
    /// </summary>
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 LabelledEvaluation labelSet, ContainsUndefinedBreakTarget, SJL-15.2 semantics rows; IP=Low; Security=Medium; Resources=1; Fingerprint=29F8F7
    // Hairs-Human:        PENDING
    private Context LabelledItem(Context context, Statement item, LabelSet labels)
    {
        var semantic = context.Semantic!;
        if (item is LabelledStatement or WhileStatement or DoWhileStatement or ForStatement or ForInStatement or ForOfStatement or SwitchStatement)
            return context with { Semantic = semantic with { CurrentLabels = labels } };
        return context with
        {
            Semantic = semantic with
            {
                Breakables = new Breakable(BreakableKind.Labelled, item, labels, TargetFor(item, labels), semantic.Breakables),
                CurrentLabels = null,
            },
        };
    }

    /// <summary>
    /// The context of an iteration statement's or a <c>switch</c>'s children (SJL-15.2 stage 3): the statement joins the
    /// statements a jump there may leave or continue, with the labels directly over it, which it takes from the context.
    /// </summary>
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s14.8.1 s14.9.1 ContainsUndefinedBreakTarget ContainsUndefinedContinueTarget LabelledEvaluation, SJL-15.2 semantics rows; IP=Low; Security=Medium; Resources=1; Fingerprint=2A82C5
    // Hairs-Human:        PENDING
    private Context Inside(Context context, BreakableKind kind, Statement statement)
    {
        var semantic = context.Semantic!;
        var labels = semantic.CurrentLabels;
        return context with
        {
            Semantic = semantic with
            {
                Breakables = new Breakable(kind, statement, labels, TargetFor(statement, labels), semantic.Breakables),
                CurrentLabels = null,
            },
        };
    }

    /// <summary>A statement's target, with its own label set outermost first; the model's alone, null without it.</summary>
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 LabelledEvaluation labelSet, JSD-0080 jump targets; IP=Low; Security=Low; Resources=1; Fingerprint=BC2F75
    // Hairs-Human:        PENDING
    private LabelTarget? TargetFor(Statement statement, LabelSet? labels)
    {
        if (!model)
            return null;
        var count = 0;
        for (var set = labels; set is not null; set = set.Next)
            count++;
        var names = new string[count];
        for (var set = labels; set is not null; set = set.Next)
            names[--count] = set.Name;
        return new LabelTarget(statement, Array.AsReadOnly(names));
    }

    /// <summary>
    /// A jump's target, or its row (SJL-15.2 stage 3): the innermost of the statements it may leave or continue - for an
    /// unlabelled <c>break</c> an iteration statement or a <c>switch</c>, for an unlabelled <c>continue</c> an iteration
    /// statement, for <c>break L</c> any statement whose label set holds <c>L</c>, for <c>continue L</c> an iteration
    /// statement whose label set holds it, wherever it stands in the set - recorded as the jump's target with the model on;
    /// with none, EE0059 or EE0058 for an unlabelled jump, and for a labelled one the undefined target's row of the label
    /// set's owner, with the label as its argument, at the jump. A labelled <c>continue</c> that no iteration statement
    /// encloses reports its owner's row alone (the session's decision D-js-jump-rows: one row per construct).
    /// </summary>
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s14.8.1 s14.9.1 s15.2.1 s15.7.1 s16.1.1 s16.2.1.1 ContainsUndefinedBreakTarget ContainsUndefinedContinueTarget, SJL-15.2 semantics rows; IP=Low; Security=Medium; Resources=1; Fingerprint=5DFB7C
    // Hairs-Human:        PENDING
    private void CheckJump(Statement jump, string? label, bool continues, Context context)
    {
        for (var entry = context.Semantic!.Breakables; entry is not null; entry = entry.Outer)
        {
            var matches = label is null
                ? entry.Kind == BreakableKind.Iteration || (!continues && entry.Kind == BreakableKind.Switch)
                : (!continues || entry.Kind == BreakableKind.Iteration) && Holds(entry.Labels, label);
            if (!matches)
                continue;
            if (model)
                targetOf![jump] = entry.Target!;
            return;
        }

        if (label is null)
        {
            Report(continues ? SemanticRows.EE0058 : SemanticRows.EE0059, jump);
            return;
        }

        var row = context.LabelOwner switch
        {
            LabelOwner.Module => continues ? SemanticRows.EE0176 : SemanticRows.EE0175,
            LabelOwner.StaticBlock => continues ? SemanticRows.EE0137 : SemanticRows.EE0136,
            LabelOwner.Function => continues ? SemanticRows.EE0082 : SemanticRows.EE0081,
            _ => continues ? SemanticRows.EE0166 : SemanticRows.EE0165,
        };
        Report(row, jump, label);
    }

    /// <summary>Whether a label set holds a label.</summary>
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 LabelledEvaluation labelSet; IP=Low; Security=Low; Resources=0; Fingerprint=A93A4E
    // Hairs-Human:        PENDING
    private static bool Holds(LabelSet? labels, string label)
    {
        for (var set = labels; set is not null; set = set.Next)
        {
            if (set.Name == label)
                return true;
        }

        return false;
    }

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=1C89D0
    // Hairs-Human:        PENDING
    private void VisitCatch(CatchClause @catch, Context context)
    {
        var names = new List<string>();
        var nodes = new List<BindingIdentifier>();
        if (@catch.Parameter is { } parameter)
            BoundNames(parameter, names, nodes);
        var declarations = new List<Declaration>();
        foreach (var node in nodes)
            declarations.Add(new Declaration(node.Name, BindingKind.CatchParameter, node, true, false, false));
        var catchScope = AddScope(@catch.Span.Offset, new Scope(ScopeKind.Catch, @catch, declarations.AsReadOnly()), context.ModelScope);

        // A name the parameter binds twice (EE0065).
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var reported = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in nodes)
        {
            if (!seen.Add(node.Name) && reported.Add(node.Name))
                Report(Rows.EE0065, node, node.Name);
        }

        if (nodes.Count > 0)
        {
            var end = End(@catch);
            // Each name once per rule, at the declaration that follows the parameter (decision D11).
            var parameterNames = new HashSet<string>(names, StringComparer.Ordinal);
            var lexical = new List<Declaration>();
            foreach (var statement in @catch.Body.Body)
                CollectLexical(statement, lexical, inBlock: true);
            var lexicalReported = new HashSet<string>(StringComparer.Ordinal);
            foreach (var declaration in lexical)
            {
                if (parameterNames.Contains(declaration.Name) && lexicalReported.Add(declaration.Name))
                    ReportConflict(Rows.EE0066, declaration.Node, end, declaration.Name);
            }

            // A var of the parameter's name (EE0067), which Annex B.3.4 admits for a lone BindingIdentifier (EG0006), a for-of
            // head's among them as test262 reads it.
            var vars = new List<Declaration>();
            foreach (var statement in @catch.Body.Body)
                CollectVars(statement, vars);
            var lone = @catch.Parameter is BindingIdentifier;
            var varReported = new HashSet<string>(StringComparer.Ordinal);
            foreach (var declaration in vars)
            {
                if (parameterNames.Contains(declaration.Name) && (!lone || !annexB) && varReported.Add(declaration.Name))
                    ReportConflict(Rows.EE0067, declaration.Node, end, declaration.Name);
            }
        }

        PushChildren(@catch, model ? context with { ModelScope = catchScope } : context);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Modules (clause 16.2).

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=7EEC92
    // Hairs-Human:        PENDING
    private void CheckExportName(StringLiteral name)
    {
        // A ModuleExportName's string must be well-formed Unicode (EE0178).
        var value = name.Value;
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (char.IsHighSurrogate(c) && i + 1 < value.Length && char.IsLowSurrogate(value[i + 1]))
            {
                i++;
                continue;
            }

            if (char.IsSurrogate(c))
            {
                Report(Rows.EE0178, name);
                return;
            }
        }
    }

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=D8573C
    // Hairs-Human:        PENDING
    private void CheckModuleExportNames(ImportDeclaration import)
    {
        if (import.NamedImports is null)
            return;
        foreach (var specifier in import.NamedImports)
        {
            if (specifier.Imported is StringLiteral text)
                CheckExportName(text);
        }
    }

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=A77227
    // Hairs-Human:        PENDING
    private void CheckExport(ExportNamedDeclaration export)
    {
        foreach (var specifier in export.Specifiers)
        {
            if (specifier.Local is StringLiteral local)
            {
                CheckExportName(local);

                // Without `from`, a local name that is a string refers to no binding (EE0181).
                if (export.Source is null)
                    Report(Rows.EE0181, local);
            }
            else if (specifier.Local is IdentifierName identifier && export.Source is null
                && (Reserved.Contains(identifier.Name) || (StrictReserved.Contains(identifier.Name) && identifier.Name != "yield")))
            {
                // Without `from`, a local name that is a reserved word refers to no binding (EE0182).
                Report(Rows.EE0182, identifier, identifier.Name);
            }

            if (specifier.Exported is StringLiteral exported)
                CheckExportName(exported);
        }
    }

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=3F41E6
    // Hairs-Human:        PENDING
    private void CheckAttributes(IReadOnlyList<ImportAttribute>? attributes)
    {
        // An import attribute's key given twice (EE0180).
        if (attributes is null)
            return;
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var attribute in attributes)
        {
            var key = attribute.Key switch
            {
                IdentifierName identifier => identifier.Name,
                StringLiteral text => text.Value,
                _ => string.Empty,
            };
            if (!keys.Add(key))
                Report(Rows.EE0180, attribute.Key, key);
        }
    }

    // ---------------------------------------------------------------------------------------------------------------
    // The static semantics of declarations: BoundNames, LexicallyDeclaredNames and VarDeclaredNames (clauses 8.2 and 14).

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=9059BA
    // Hairs-Human:        PENDING
    private void CheckConflicts(List<Declaration> lexical, List<Declaration> vars, int scopeEnd, LexicalRefusal duplicateRow, LexicalRefusal collisionRow, bool sloppy = false)
    {
        if (lexical.Count == 0)
            return;

        // A name lexically declared twice: Annex B.3.2.4 admits two plain function declarations of a block in sloppy code,
        // which CollectLexical marks by their kind and the walk by strictness (EG0004's exception).
        // Every such name is reported once, at its first declaration that conflicts with an earlier one; every conflict of
        // the scope is found, whatever else conflicts there, and the order sorts them (decision D11).
        var seen = new Dictionary<string, Declaration>(StringComparer.Ordinal);
        var reported = new HashSet<string>(StringComparer.Ordinal);
        foreach (var declaration in lexical)
        {
            if (seen.TryGetValue(declaration.Name, out var earlier))
            {
                if (!(annexB && sloppy && earlier.IsBlockFunction && declaration.IsBlockFunction && IsSloppyPlainFunction(earlier) && IsSloppyPlainFunction(declaration))
                    && reported.Add(declaration.Name))
                {
                    ReportConflict(duplicateRow, declaration.Node, scopeEnd, declaration.Name);
                }
            }
            else
            {
                seen[declaration.Name] = declaration;
            }
        }

        CheckCollisions(lexical, vars, scopeEnd, collisionRow);
    }

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=B4C9B1
    // Hairs-Human:        PENDING
    private readonly HashSet<SyntaxNode> sloppyPlainFunctions = new(ReferenceEqualityComparer.Instance);

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=C45402
    // Hairs-Human:        PENDING
    private bool IsSloppyPlainFunction(Declaration declaration) => sloppyPlainFunctions.Contains(declaration.Node);

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=2FF186
    // Hairs-Human:        PENDING
    private void CheckCollisions(List<Declaration> lexical, List<Declaration> vars, int scopeEnd, LexicalRefusal collisionRow)
    {
        if (lexical.Count == 0 || vars.Count == 0)
            return;
        // Each name both lexically and var-declared, once: the pair of its first lexical and its first var declaration is the
        // first that conflicts, and the later of the two is reported (decision D11).
        var firstLexical = new Dictionary<string, Declaration>(StringComparer.Ordinal);
        foreach (var declaration in lexical)
        {
            if (!firstLexical.TryGetValue(declaration.Name, out var known) || declaration.Node.Span.Offset < known.Node.Span.Offset)
                firstLexical[declaration.Name] = declaration;
        }

        var firstVar = new Dictionary<string, Declaration>(StringComparer.Ordinal);
        foreach (var declaration in vars)
        {
            if (firstLexical.ContainsKey(declaration.Name)
                && (!firstVar.TryGetValue(declaration.Name, out var known) || declaration.Node.Span.Offset < known.Node.Span.Offset))
            {
                firstVar[declaration.Name] = declaration;
            }
        }

        foreach (var declaration in vars)
        {
            if (!firstVar.TryGetValue(declaration.Name, out var first) || !ReferenceEquals(first, declaration))
                continue;
            var lexicalDeclaration = firstLexical[declaration.Name];
            var second = declaration.Node.Span.Offset > lexicalDeclaration.Node.Span.Offset ? declaration : lexicalDeclaration;
            ReportConflict(collisionRow, second.Node, scopeEnd, declaration.Name);
        }
    }

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=36C73B
    // Hairs-Human:        PENDING
    private void CollectList(IReadOnlyList<Statement> body, bool top, bool module, List<Declaration> lexical, List<Declaration> vars)
    {
        foreach (var statement in body)
        {
            if (top)
                CollectTopLevel(statement, lexical, vars);
            else
                CollectModuleItem(statement, lexical, vars, module);
        }
    }

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=73EC67
    // Hairs-Human:        PENDING
    private void CollectTopLevel(Statement statement, List<Declaration> lexical, List<Declaration> vars)
    {
        // TopLevelLexicallyDeclaredNames and TopLevelVarDeclaredNames: a function's or a script's own function declarations
        // are var-scoped.
        var inner = statement;
        while (inner is LabelledStatement labelled)
            inner = labelled.Body;
        if (inner is FunctionDeclaration function)
        {
            if (function.Function.Name is { } name)
                vars.Add(new Declaration(name.Name, BindingKind.Function, name, false, false, false));
            return;
        }

        if (statement is LabelledStatement)
        {
            CollectVars(statement, vars);
            return;
        }

        if (!CollectLexical(statement, lexical, inBlock: false))
            CollectVars(statement, vars);
    }

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=2E5F8D
    // Hairs-Human:        PENDING
    private void CollectModuleItem(Statement statement, List<Declaration> lexical, List<Declaration> vars, bool module)
    {
        switch (statement)
        {
            case ImportDeclaration import:
                foreach (var binding in ImportBindings(import))
                    lexical.Add(new Declaration(binding.Name, BindingKind.Import, binding, true, false, false));
                return;
            case ExportNamedDeclaration { Declaration: { } declaration }:
                CollectModuleItem(declaration, lexical, vars, module);
                return;
            case ExportDefaultDeclaration exportDefault:
                switch (exportDefault.Declaration)
                {
                    case FunctionDeclaration { Function.Name: { } functionName }:
                        lexical.Add(new Declaration(functionName.Name, BindingKind.Function, functionName, true, false, false));
                        return;
                    case ClassDeclaration { Class.Name: { } className }:
                        lexical.Add(new Declaration(className.Name, BindingKind.Class, className, true, false, false));
                        return;
                    default:
                        lexical.Add(new Declaration("*default*", BindingKind.ExportDefault, exportDefault, true, false, false));
                        return;
                }

            default:
                if (!CollectLexical(statement, lexical, inBlock: !module))
                    CollectVars(statement, vars);
                return;
        }
    }

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=B79A4D
    // Hairs-Human:        PENDING
    private static IEnumerable<BindingIdentifier> ImportBindings(ImportDeclaration import)
    {
        if (import.DefaultBinding is { } defaultBinding)
            yield return defaultBinding;
        if (import.NamespaceBinding is { } namespaceBinding)
            yield return namespaceBinding;
        if (import.NamedImports is { } named)
        {
            foreach (var specifier in named)
                yield return specifier.Local;
        }
    }

    /// <summary>Adds a statement's LexicallyDeclaredNames, and answers whether it declares lexically at all.</summary>
    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=3A79C2
    // Hairs-Human:        PENDING
    private bool CollectLexical(Statement statement, List<Declaration> lexical, bool inBlock)
    {
        switch (statement)
        {
            case VariableDeclaration { DeclarationKind: not DeclarationKind.Var } declaration:
            {
                var kind = declaration.DeclarationKind switch
                {
                    DeclarationKind.Let => BindingKind.Let,
                    DeclarationKind.Const => BindingKind.Const,
                    DeclarationKind.Using => BindingKind.Using,
                    _ => BindingKind.AwaitUsing,
                };
                var nodes = new List<BindingIdentifier>();
                foreach (var declarator in declaration.Declarations)
                    BoundNames(declarator.Target, null, nodes);
                foreach (var node in nodes)
                    lexical.Add(new Declaration(node.Name, kind, node, true, false, false));
                return true;
            }

            case ClassDeclaration { Class.Name: { } className }:
                lexical.Add(new Declaration(className.Name, BindingKind.Class, className, true, false, false));
                return true;
            case FunctionDeclaration { Function.Name: { } name } function:
            {
                var plain = !function.Function.IsAsync && !function.Function.IsGenerator;
                if (plain)
                    sloppyPlainFunctions.Add(name);
                lexical.Add(new Declaration(name.Name, BindingKind.Function, name, true, inBlock, ifClauseFunctions.Contains(function)));
                return true;
            }

            case LabelledStatement labelled:
            {
                var inner = labelled.Body;
                while (inner is LabelledStatement next)
                    inner = next.Body;
                return inner is FunctionDeclaration && CollectLexical(inner, lexical, inBlock);
            }

            default:
                return false;
        }
    }

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=0C2D53
    // Hairs-Human:        PENDING
    private static void CollectVars(Statement statement, List<Declaration> vars, HashSet<SyntaxNode>? forOf = null)
    {
        switch (statement)
        {
            case VariableDeclaration { DeclarationKind: DeclarationKind.Var } declaration:
                AddVars(declaration, vars, null);
                return;
            case BlockStatement block:
                foreach (var inner in block.Body)
                    CollectVars(inner, vars, forOf);
                return;
            case IfStatement @if:
                CollectVars(@if.Consequent, vars, forOf);
                if (@if.Alternate is not null)
                    CollectVars(@if.Alternate, vars, forOf);
                return;
            case ForStatement @for:
                if (@for.Init is VariableDeclaration { DeclarationKind: DeclarationKind.Var } init)
                    AddVars(init, vars, null);
                CollectVars(@for.Body, vars, forOf);
                return;
            case ForInStatement forIn:
                if (forIn.Left is VariableDeclaration { DeclarationKind: DeclarationKind.Var } inHead)
                    AddVars(inHead, vars, null);
                CollectVars(forIn.Body, vars, forOf);
                return;
            case ForOfStatement forOfStatement:
                if (forOfStatement.Left is VariableDeclaration { DeclarationKind: DeclarationKind.Var } ofHead)
                    AddVars(ofHead, vars, forOf);
                CollectVars(forOfStatement.Body, vars, forOf);
                return;
            case WhileStatement @while:
                CollectVars(@while.Body, vars, forOf);
                return;
            case DoWhileStatement doWhile:
                CollectVars(doWhile.Body, vars, forOf);
                return;
            case WithStatement with:
                CollectVars(with.Body, vars, forOf);
                return;
            case LabelledStatement labelled:
                CollectVars(labelled.Body, vars, forOf);
                return;
            case SwitchStatement @switch:
                foreach (var @case in @switch.Cases)
                {
                    foreach (var inner in @case.Consequent)
                        CollectVars(inner, vars, forOf);
                }

                return;
            case TryStatement @try:
                CollectVars(@try.Block, vars, forOf);
                if (@try.Handler is not null)
                    CollectVars(@try.Handler.Body, vars, forOf);
                if (@try.Finalizer is not null)
                    CollectVars(@try.Finalizer, vars, forOf);
                return;
            case ExportNamedDeclaration { Declaration: { } declaration }:
                CollectVars(declaration, vars, forOf);
                return;
            default:
                return;
        }
    }

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=118E73
    // Hairs-Human:        PENDING
    private static void AddVars(VariableDeclaration declaration, List<Declaration> vars, HashSet<SyntaxNode>? forOf)
    {
        var nodes = new List<BindingIdentifier>();
        foreach (var declarator in declaration.Declarations)
            BoundNames(declarator.Target, null, nodes);
        foreach (var node in nodes)
        {
            vars.Add(new Declaration(node.Name, BindingKind.Var, node, false, false, false));
            forOf?.Add(node);
        }
    }

    /// <summary>BoundNames of a binding pattern, in source order; iterative, since a pattern's nesting is the request's to bound.</summary>
    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=F6DB6B
    // Hairs-Human:        PENDING
    private static void BoundNames(Pattern? pattern, List<string>? names, List<BindingIdentifier>? nodes)
    {
        if (pattern is null)
            return;
        var pending = new Stack<SyntaxNode>();
        pending.Push(pattern);
        while (pending.Count > 0)
        {
            switch (pending.Pop())
            {
                case BindingIdentifier identifier:
                    names?.Add(identifier.Name);
                    nodes?.Add(identifier);
                    break;
                case AssignmentPattern assignment:
                    pending.Push(assignment.Target);
                    break;
                case RestElement rest:
                    pending.Push(rest.Argument);
                    break;
                case ArrayPattern array:
                    for (var i = array.Elements.Count - 1; i >= 0; i--)
                    {
                        if (array.Elements[i] is { } element)
                            pending.Push(element);
                    }

                    break;
                case ObjectPattern @object:
                    for (var i = @object.Properties.Count - 1; i >= 0; i--)
                    {
                        var property = @object.Properties[i];
                        pending.Push(property is PatternProperty patternProperty ? patternProperty.Value : property);
                    }

                    break;
            }
        }
    }

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=DD74DF
    // Hairs-Human:        PENDING
    private static IReadOnlyList<Declaration> Merge(List<Declaration> lexical, List<Declaration> vars)
    {
        var all = new List<Declaration>(lexical.Count + vars.Count);
        all.AddRange(lexical);
        all.AddRange(vars);
        all.Sort(static (a, b) => a.Node.Span.Offset.CompareTo(b.Node.Span.Offset));
        return all.AsReadOnly();
    }

    // ---------------------------------------------------------------------------------------------------------------
    // The context the walk carries down the tree.

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=923D84
    // Hairs-Human:        PENDING
    private sealed record LabelSet(string Name, LabelSet? Next);

    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=751BF7
    // Hairs-Human:        PENDING
    private sealed record Context
    {
        /// <summary>Whether the code is strict mode code.</summary>
        public bool Strict { get; init; }

        /// <summary>The grammar's [Yield] parameter where the node stands.</summary>
        public bool Yield { get; init; }

        /// <summary>The grammar's [Await] parameter where the node stands.</summary>
        public bool Await { get; init; }

        /// <summary>Whether a non-arrow function (or a field initialiser, a static block, eval code in one) encloses the node.</summary>
        public bool InFunction { get; init; }

        /// <summary>Whether the node is in a class static block's statements, inside no function nested in it.</summary>
        public bool InStaticBlock { get; init; }

        /// <summary>The shape of the function whose parameters hold the node, inside no function nested in them; null elsewhere.</summary>
        public Shape? ParamsOf { get; init; }

        /// <summary>The labels of the statements that enclose the node in its statement list's owner.</summary>
        public LabelSet? Labels { get; init; }

        /// <summary>The owner of those labels' statement list.</summary>
        public LabelOwner LabelOwner { get; init; }

        /// <summary>The semantic model's scope where the node stands; null unless the request asks for the model.</summary>
        public Scope? ModelScope { get; init; }

        /// <summary>What the model's rows read where the node stands; null unless the request asks for the model.</summary>
        public SemanticContext? Semantic { get; init; }
    }

    /// <summary>
    /// What the model's rows read where a node stands (SJL-15.2 stage 2): the row a SuperProperty breaks there, or null
    /// where one is admitted; the same for a SuperCall; and the ContainsArguments anchors that enclose the place, innermost
    /// first, or null where none does. From stage 3, also the statements a jump there may leave or continue and the labels
    /// directly over the statement being visited, members that are not positional, so that every place that makes a fresh
    /// one (the program, the dynamic goals, every function but an arrow, a field initialiser, a static block) resets them,
    /// and an arrow resets them itself. <i>(Added 2026-11-25, at SJL-15.2 stage 3.)</i> From stage 4, also the private names
    /// visible there, a member that is not positional either, which the program sets for eval code and every place that
    /// makes a fresh one carries, not resets. <i>(Added 2026-11-25, at SJL-15.2 stage 4.)</i>
    /// </summary>
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 Contains SuperProperty SuperCall, s15.7.1 ContainsArguments, SJL-15.2 semantics rows; IP=Low; Security=Medium; Resources=1; Fingerprint=EEAC7A
    // Hairs-Human:        PENDING
    private sealed record SemanticContext(LexicalRefusal? SuperPropertyRow, LexicalRefusal? SuperCallRow, ArgumentsAnchors? Arguments)
    {
        /// <summary>The statements a jump here may leave or continue, innermost first, up to the boundary; null at a boundary.</summary>
        public Breakable? Breakables { get; init; }

        /// <summary>The labels directly over the statement being visited (LabelledEvaluation's labelSet), innermost first; null elsewhere.</summary>
        public LabelSet? CurrentLabels { get; init; }

        /// <summary>
        /// The private names visible here, innermost class first, carried through every function, parameter list, field
        /// initialiser and static block; null where none is (SJL-15.2 stage 4). <i>(Added 2026-11-25, at SJL-15.2 stage 4.)</i>
        /// </summary>
        public PrivateScope? Privates { get; init; }
    }

    /// <summary>
    /// The private names one class declares, or at eval code's root the names its caller sees (SJL-15.2 stage 4): the names
    /// in source order, or the caller's order; the class's environment with the model on (null without it, and always at
    /// the root); and the names around it. Its lookup is a set, from which no order is read.
    /// </summary>
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 AllPrivateIdentifiersValid, PrivateEnvironment Records, SJL-15.2 semantics rows; IP=Low; Security=Low; Resources=1; Fingerprint=1950F1
    // Hairs-Human:        PENDING
    private sealed record PrivateScope(IReadOnlyList<string> Names, PrivateEnvironment? Environment, PrivateScope? Outer)
    {
        // Hairs-AI:           Origin=AI; Spec=ECMA-262 AllPrivateIdentifiersValid, SJL-15.2 semantics rows; IP=Low; Security=Low; Resources=1; Fingerprint=7ECC5F
        // Hairs-Human:        PENDING
        private readonly HashSet<string> set = new(Names, StringComparer.Ordinal);

        /// <summary>Whether the scope holds a name.</summary>
        // Hairs-AI:           Origin=AI; Spec=ECMA-262 AllPrivateIdentifiersValid, SJL-15.2 semantics rows; IP=Low; Security=Low; Resources=1; Fingerprint=18339B
        // Hairs-Human:        PENDING
        public bool Holds(string name) => set.Contains(name);
    }

    /// <summary>What a statement a jump may leave or continue is (SJL-15.2 stage 3).</summary>
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s14.8.1 s14.9.1 ContainsUndefinedBreakTarget ContainsUndefinedContinueTarget, SJL-15.2 semantics rows; IP=Low; Security=Low; Resources=0; Fingerprint=C9EF45
    // Hairs-Human:        PENDING
    private enum BreakableKind
    {
        Iteration,
        Switch,
        Labelled,
    }

    /// <summary>
    /// A statement a jump may leave or continue (SJL-15.2 stage 3): its kind, the statement, its own label set innermost
    /// first (null when it has none), its target with the model on (null without it), and the one enclosing it.
    /// </summary>
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 ContainsUndefinedBreakTarget ContainsUndefinedContinueTarget LabelledEvaluation, SJL-15.2 semantics rows; IP=Low; Security=Low; Resources=0; Fingerprint=4FD15B
    // Hairs-Human:        PENDING
    private sealed record Breakable(BreakableKind Kind, Statement Statement, LabelSet? Labels, LabelTarget? Target, Breakable? Outer);

    /// <summary>
    /// A ContainsArguments anchor: its row, the <c>FieldDefinition</c> or <c>StaticBlock</c> it is reported at, or null for
    /// eval code's in a field initialiser (EA0005), reported at each reference; and the anchor enclosing it.
    /// </summary>
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s15.7.1 ContainsArguments, s19.2.1.1 PerformEval step 11.h, SJL-15.2 semantics rows; IP=Low; Security=Medium; Resources=1; Fingerprint=0EE3A9
    // Hairs-Human:        PENDING
    private sealed record ArgumentsAnchors(LexicalRefusal Row, SyntaxNode? At, ArgumentsAnchors? Outer);
}
