// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   86
// Annotated:        86/86
// Exempt:           42
// Human-reviewed:   0/86
// IP risk:          Low
// Security risk:    Medium
// Criteria:         0/0
// Resource impact:  1/10 max
// Unverified:       86
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
/// <b>No <c>request</c>, <c>engine</c> or <c>semantics</c> row is reported</b>: every refusal is one of <see cref="Rows"/>.
/// The top-level-<c>await</c> switch (EX0023, the one <c>request</c> row) reaches the walk only as the [Await] parameter
/// of the top level, as it reaches the parser (decision D10).
/// </para>
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=B38FCA
// Broiler-Human:        PENDING
internal sealed class DeclarationsWalk
{
    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=774738
    // Broiler-Human:        PENDING
    private static readonly FrozenSet<string> StrictReserved = new HashSet<string>(StringComparer.Ordinal)
    {
        "implements", "interface", "let", "package", "private", "protected", "public", "static", "yield",
    }.ToFrozenSet(StringComparer.Ordinal);

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=0B9019
    // Broiler-Human:        PENDING
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
    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=4AC307
    // Broiler-Human:        PENDING
    private readonly Dictionary<FunctionNode, Shape> shapes = new(ReferenceEqualityComparer.Instance);
    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=E373F4
    // Broiler-Human:        PENDING
    private readonly HashSet<SyntaxNode> declaredFunctions = new(ReferenceEqualityComparer.Instance);
    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=E6D526
    // Broiler-Human:        PENDING
    private readonly HashSet<SyntaxNode> taggedQuasis = new(ReferenceEqualityComparer.Instance);
    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=408B2B
    // Broiler-Human:        PENDING
    private readonly HashSet<SyntaxNode> ifClauseFunctions = new(ReferenceEqualityComparer.Instance);
    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=016F67
    // Broiler-Human:        PENDING
    private readonly HashSet<SyntaxNode> skippedNames = new(ReferenceEqualityComparer.Instance);
    private readonly Stack<(SyntaxNode Node, Context Context)> stack = new();
    private readonly List<SyntaxNode> children = new();
    private int sequence;

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=BFF3D1
    // Broiler-Human:        PENDING
    private DeclarationsWalk(ParseRequest request)
    {
        this.request = request;
        annexB = request.Switches.AnnexB;
        isModule = request.Goal == ParseGoal.Module;
    }

    /// <summary>The shape of a function, which decides the catalogue row each of its rules is reported by.</summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=A9BD3F
    // Broiler-Human:        PENDING
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
    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=ADA519
    // Broiler-Human:        PENDING
    private enum LabelOwner
    {
        Script,
        Module,
        Function,
        StaticBlock,
    }

    /// <summary>The walk's result: the diagnostics in the library's order, and the side tables.</summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=F734C4
    // Broiler-Human:        PENDING
    internal sealed class Result(List<SyntaxDiagnostic> diagnostics, DeclarationTables tables)
    {
        internal List<SyntaxDiagnostic> Diagnostics { get; } = diagnostics;

        internal DeclarationTables Tables { get; } = tables;
    }

    /// <summary>Walks a tree the grammar admitted.</summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=CF7D4C
    // Broiler-Human:        PENDING
    internal static Result Run(ParseRequest request, ProgramNode program)
    {
        var walk = new DeclarationsWalk(request);
        walk.WalkProgram(program, program.Body, closed: true);
        return new Result(walk.Ordered(int.MaxValue), walk.Tables());
    }

    /// <summary>
    /// Walks the top-level statements a parse completed before the grammar refused at <paramref name="refusedAt"/>: their
    /// early errors come before the grammar's refusal (decision D11). The top level's own scope never closed, so its conflicts
    /// are not checked.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=B3D642
    // Broiler-Human:        PENDING
    internal static List<SyntaxDiagnostic> RunPartial(ParseRequest request, List<Statement> completed, int refusedAt)
    {
        var walk = new DeclarationsWalk(request);
        var span = new SourceSpan(request.Source.BaseOffset, 0, request.Source.BaseLine, request.Source.BaseColumn);
        var program = new ProgramNode(span, request.Goal, completed.ToArray(), Array.Empty<Pattern>(), false, false);
        walk.WalkProgram(program, completed, closed: false);
        return walk.Ordered(refusedAt);
    }

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=7D56CF
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=9E7968
    // Broiler-Human:        PENDING
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
        return new DeclarationTables(Array.AsReadOnly(scopeList), Array.AsReadOnly(functionList));
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Reporting.

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=34AB6C
    // Broiler-Human:        PENDING
    private void Report(LexicalRefusal row, SourceSpan span, int key, int subKey, string argument = "")
    {
        var diagnostic = new SyntaxDiagnostic(row.Code, row.MessageId, argument.Length == 0 ? Array.Empty<string>() : new[] { argument },
            DiagnosticPhase.Semantics, span, request.Source.Name);
        found.Add((key, subKey, sequence++, diagnostic));
    }

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=67E8B3
    // Broiler-Human:        PENDING
    private void Report(LexicalRefusal row, SyntaxNode at, string argument = "") => Report(row, at.Span, at.Span.Offset, at.Span.Offset, argument);

    /// <summary>
    /// A declared-name conflict, reported at the second of the conflicting declarations and placed at the close of the scope
    /// that holds them, after the conflicts of the same scope whose second declaration comes earlier (decision D11).
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=9326BB
    // Broiler-Human:        PENDING
    private void ReportConflict(LexicalRefusal row, SyntaxNode second, int scopeEnd, string name) =>
        Report(row, second.Span, scopeEnd, second.Span.Offset, name);

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=FF2B43
    // Broiler-Human:        PENDING
    private static int End(SyntaxNode node) => node.Span.Offset + node.Span.Length;

    // ---------------------------------------------------------------------------------------------------------------
    // The program.

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=075477
    // Broiler-Human:        PENDING
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
        };

        var parameterNames = new List<string>();
        foreach (var parameter in program.Parameters)
            BoundNames(parameter, parameterNames, null);
        functions.Add((program.Span.Offset, sequence++, new FunctionFacts(program, strict, parameterNames.AsReadOnly())));

        // The top level's scope: a function's for the dynamic function's goals, whose declarations are its body's.
        var top = !isModule;
        var lexical = new List<Declaration>();
        var vars = new List<Declaration>();
        CollectList(body, top, isModule, lexical, vars);
        var scopeKind = isModule ? ScopeKind.Module : isDynamic ? ScopeKind.Function : ScopeKind.Script;
        scopes.Add((program.Span.Offset, sequence++, new Scope(scopeKind, program, Merge(lexical, vars))));
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

        var parameterContext = context with { ParamsOf = isDynamic ? DynamicShape(kind) : null };
        Run(program.Parameters, parameterContext);
        Run(body, context);
    }

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=DCC45B
    // Broiler-Human:        PENDING
    private static Shape DynamicShape(DynamicFunctionKind kind) => kind switch
    {
        DynamicFunctionKind.Generator => Shape.Generator,
        DynamicFunctionKind.Async => Shape.AsyncFunction,
        DynamicFunctionKind.AsyncGenerator => Shape.AsyncGenerator,
        _ => Shape.Function,
    };

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=B88031
    // Broiler-Human:        PENDING
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
            Visit(node, nodeContext);
        }
    }

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=5CC567
    // Broiler-Human:        PENDING
    private void Push(SyntaxNode? node, Context context)
    {
        if (node is not null)
            stack.Push((node, context));
    }

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=6215BA
    // Broiler-Human:        PENDING
    private void PushAll<T>(IReadOnlyList<T> nodes, Context context)
        where T : SyntaxNode?
    {
        for (var i = nodes.Count - 1; i >= 0; i--)
        {
            if (nodes[i] is { } node)
                stack.Push((node, context));
        }
    }

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=D08166
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=8F9BE4
    // Broiler-Human:        PENDING
    private void Visit(SyntaxNode node, Context context)
    {
        switch (node)
        {
            case Identifier identifier:
                CheckName(identifier.Name, identifier.IsEscaped, identifier, NameUse.Reference, context);
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
                Push(declaration.Function, context);
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
                VisitStatementListScope(block, block.Body, ScopeKind.Block, Rows.EE0040, Rows.EE0041, context.Strict);
                PushChildren(node, context);
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
                PushChildren(node, context);
                return;
            case DoWhileStatement doWhile:
                CheckLabelledFunctionBody(doWhile.Body, Rows.EE0048);
                PushChildren(node, context);
                return;
            case WithStatement with:
                if (context.Strict)
                    Report(Rows.EE0060, with, "with");
                CheckLabelledFunctionBody(with.Body, Rows.EE0061);
                PushChildren(node, context);
                return;
            case LabelledStatement labelled:
                VisitLabelled(labelled, context);
                return;
            case BreakStatement { Label: { } breakLabel }:
                CheckName(breakLabel.Name, breakLabel.IsEscaped, breakLabel, NameUse.Label, context);
                return;
            case ContinueStatement { Label: { } continueLabel }:
                CheckName(continueLabel.Name, continueLabel.IsEscaped, continueLabel, NameUse.Label, context);
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
            default:
                PushChildren(node, context);
                return;
        }
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Names (ECMA-262 clause 13.1.1; catalogue rows EE0005 to EE0014, EE0140).

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=BE3932
    // Broiler-Human:        PENDING
    private enum NameUse
    {
        Reference,
        Binding,
        Label,
    }

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=5AF585
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=144201
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=1D75B6
    // Broiler-Human:        PENDING
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
    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=DF2A2A
    // Broiler-Human:        PENDING
    internal static readonly System.Collections.Immutable.ImmutableArray<LexicalRefusal> RegularExpressionRows =
    [
        Rows.EE0019, Rows.EE0183, Rows.EE0184, Rows.EE0185, Rows.EE0186, Rows.EE0187, Rows.EE0188, Rows.EE0189, Rows.EE0190,
        Rows.EE0191, Rows.EE0192, Rows.EE0193, Rows.EE0194, Rows.EE0195, Rows.EE0196, Rows.EE0197, Rows.EE0198, Rows.EE0199,
        Rows.EE0200, Rows.EE0201, Rows.EE0202, Rows.EE0203, Rows.EE0204, Rows.EE0205, Rows.EE0206, Rows.EE0207, Rows.EE0208,
        Rows.EE0209, Rows.EE0210, Rows.EE0211, Rows.EE0212, Rows.EE0213, Rows.EG0002,
    ];

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=EB66B0
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=0FD09D
    // Broiler-Human:        PENDING
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
        functions.Add((function.Span.Offset, sequence++, new FunctionFacts(function, strict, parameterNames.AsReadOnly())));

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
        scopes.Add((function.Span.Offset, sequence++, new Scope(ScopeKind.Function, function, Merge(lexical, vars))));
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
        };
        var bodyContext = parameterContext with
        {
            Yield = !isArrow && function.IsGenerator,
            Await = function.IsAsync,
            ParamsOf = null,
        };

        Push(function.ExpressionBody, bodyContext);
        PushAll(body, bodyContext);
        PushAll(function.Parameters, parameterContext);
    }

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=08E36D
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=8476A8
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=F87D22
    // Broiler-Human:        PENDING
    private static LexicalRefusal? StrictName(Shape shape) => shape switch
    {
        Shape.Function => Rows.EE0071,
        Shape.Generator => Rows.EE0098,
        Shape.AsyncGenerator => Rows.EE0112,
        Shape.AsyncFunction => Rows.EE0148,
        _ => null,
    };

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=9D5983
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=DF1DA7
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=826D9A
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=39E703
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=613732
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=C2B62D
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=19B0D5
    // Broiler-Human:        PENDING
    private static bool IsSimpleParameterList(IReadOnlyList<Pattern> parameters)
    {
        foreach (var parameter in parameters)
        {
            if (parameter is not BindingIdentifier)
                return false;
        }

        return true;
    }

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=081B31
    // Broiler-Human:        PENDING
    private static bool HasUseStrict(IReadOnlyList<Statement> body) => FirstUseStrict(body) is not null;

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=12913B
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=2550B2
    // Broiler-Human:        PENDING
    private void VisitClass(ClassNode @class, Context outer)
    {
        var context = outer with { Strict = true };
        var constructors = 0;
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
                    if (!method.IsStatic && name == "constructor")
                    {
                        if (special)
                            Report(Rows.EE0125, method.Key, name);
                        else if (++constructors > 1)
                            Report(Rows.EE0122, method.Key, name);
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
                    break;
                }
            }
        }

        var elementContext = context with { InStaticBlock = false };
        for (var i = @class.Elements.Count - 1; i >= 0; i--)
        {
            switch (@class.Elements[i])
            {
                case FieldDefinition field:
                    // A field's initialiser is read under [~Yield, ~Await] (design section 9.3), inside a function boundary.
                    Push(field.Value, elementContext with { Yield = false, Await = false, InFunction = true, ParamsOf = null, Labels = null, LabelOwner = LabelOwner.Function });
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
            Push(className, context);
        PushAll(@class.Decorators, outer);
    }

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=194B51
    // Broiler-Human:        PENDING
    private void VisitStaticBlock(StaticBlock block, Context outer)
    {
        var lexical = new List<Declaration>();
        var vars = new List<Declaration>();
        CollectList(block.Body, top: true, module: false, lexical, vars);
        scopes.Add((block.Span.Offset, sequence++, new Scope(ScopeKind.StaticBlock, block, Merge(lexical, vars))));
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
        };
        PushAll(block.Body, context);
    }

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=3111FE
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=BC2B3E
    // Broiler-Human:        PENDING
    private static string? PropName(SyntaxNode key) => key switch
    {
        IdentifierName identifier => identifier.Name,
        StringLiteral text => text.Value,
        _ => null,
    };

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=27AE15
    // Broiler-Human:        PENDING
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

            // An object literal's method may not have a private name (EE0016).
            if (property.Key is PrivateIdentifier privateKey)
                Report(Rows.EE0016, privateKey, privateKey.Name);
        }

        PushChildren(property, context);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Expressions.

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=89F333
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=BBFEE4
    // Broiler-Human:        PENDING
    private enum TargetType
    {
        Simple,
        WebCompat,
        Invalid,
    }

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=FDFAF2
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=359FCA
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=E07BA1
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=E8B08A
    // Broiler-Human:        PENDING
    private void VisitShorthand(PatternProperty shorthand, Context context)
    {
        // An AssignmentProperty's IdentifierReference must be a simple target (EE0036): eval and arguments in strict code are not.
        var value = shorthand.Value is AssignmentPattern withDefault ? withDefault.Target : shorthand.Value;
        if (value is TargetPattern { Target: Identifier identifier })
        {
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

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=85A264
    // Broiler-Human:        PENDING
    private void VisitStatementListScope(SyntaxNode node, IReadOnlyList<Statement> body, ScopeKind kind, LexicalRefusal duplicateRow, LexicalRefusal collisionRow, bool strict)
    {
        var lexical = new List<Declaration>();
        foreach (var statement in body)
            CollectLexical(statement, lexical, inBlock: true);
        scopes.Add((node.Span.Offset, sequence++, new Scope(kind, node, lexical.AsReadOnly())));
        if (lexical.Count == 0)
            return;

        var vars = new List<Declaration>();
        foreach (var statement in body)
            CollectVars(statement, vars);
        CheckConflicts(lexical, vars, End(node), duplicateRow, collisionRow, sloppy: !strict);
    }

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=435D91
    // Broiler-Human:        PENDING
    private void VisitSwitch(SwitchStatement @switch, Context context)
    {
        var all = new List<Statement>();
        foreach (var @case in @switch.Cases)
        {
            all.AddRange(@case.Consequent);
            CheckNoUsing(@case.Consequent, @case.Test is null ? Rows.EP0007 : Rows.EP0006);
        }

        VisitStatementListScope(@switch, all, ScopeKind.Switch, Rows.EE0062, Rows.EE0063, context.Strict);
        PushChildren(@switch, context);
    }

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=A16920
    // Broiler-Human:        PENDING
    private void CheckNoUsing(IReadOnlyList<Statement> statements, LexicalRefusal row)
    {
        foreach (var statement in statements)
        {
            if (statement is VariableDeclaration { DeclarationKind: DeclarationKind.Using or DeclarationKind.AwaitUsing } declaration)
                Report(row, declaration, declaration.DeclarationKind == DeclarationKind.Using ? "using" : "await using");
        }
    }

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=C6F0D1
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=4BD257
    // Broiler-Human:        PENDING
    private void VisitFor(ForStatement @for, Context context)
    {
        if (@for.Init is VariableDeclaration { DeclarationKind: not DeclarationKind.Var } head)
        {
            var lexical = new List<Declaration>();
            CollectLexical(head, lexical, inBlock: true);
            scopes.Add((@for.Span.Offset, sequence++, new Scope(ScopeKind.For, @for, lexical.AsReadOnly())));
            var vars = new List<Declaration>();
            CollectVars(@for.Body, vars);
            CheckCollisions(lexical, vars, End(@for), Rows.EE0051);
        }

        CheckLabelledFunctionBody(@for.Body, Rows.EE0050);
        PushChildren(@for, context);
    }

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=43FF7B
    // Broiler-Human:        PENDING
    private void VisitForInOf(Statement statement, SyntaxNode left, Statement body, Context context)
    {
        // A call as a for-in or for-of target: Annex B.3.9 makes it a run-time refusal in non-strict code only (EG0008).
        if (left is CallExpression { IsOptional: false } call && (context.Strict || !annexB))
            Report(Rows.EG0008, call);

        if (left is VariableDeclaration { DeclarationKind: not DeclarationKind.Var } head)
        {
            CheckDeclaration(head, inForInOfHead: true);
            var lexical = new List<Declaration>();
            CollectLexical(head, lexical, inBlock: true);
            scopes.Add((statement.Span.Offset, sequence++, new Scope(ScopeKind.For, statement, lexical.AsReadOnly())));
            var vars = new List<Declaration>();
            CollectVars(body, vars);
            CheckCollisions(lexical, vars, End(statement), Rows.EE0056);
            children.Clear();
            statement.AddChildren(children);
            for (var i = children.Count - 1; i >= 0; i--)
            {
                if (ReferenceEquals(children[i], head))
                    PushAll(head.Declarations, context);
                else
                    stack.Push((children[i], context));
            }
        }
        else
        {
            PushChildren(statement, context);
        }

        CheckLabelledFunctionBody(body, Rows.EE0052);
    }

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=4D1B27
    // Broiler-Human:        PENDING
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
                scopes.Add((function.Span.Offset, sequence++, new Scope(ScopeKind.Block, function, lexical.AsReadOnly())));
            }
        }

        PushChildren(@if, context);
    }

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=004437
    // Broiler-Human:        PENDING
    private void CheckLabelledFunctionBody(Statement body, LexicalRefusal row)
    {
        // IsLabelledFunction: a labelled function, through any number of labels, as a statement's body.
        var statement = body;
        while (statement is LabelledStatement labelled)
            statement = labelled.Body;
        if (body is LabelledStatement && statement is FunctionDeclaration function)
            Report(row, function, "function");
    }

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=0B7ABE
    // Broiler-Human:        PENDING
    private void VisitLabelled(LabelledStatement labelled, Context context)
    {
        var name = labelled.Label.Name;
        CheckName(name, labelled.Label.IsEscaped, labelled.Label, NameUse.Label, context);
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
        Push(labelled.Body, context with { Labels = new LabelSet(name, context.Labels) });
    }

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=8BC487
    // Broiler-Human:        PENDING
    private void VisitCatch(CatchClause @catch, Context context)
    {
        var names = new List<string>();
        var nodes = new List<BindingIdentifier>();
        if (@catch.Parameter is { } parameter)
            BoundNames(parameter, names, nodes);
        var declarations = new List<Declaration>();
        foreach (var node in nodes)
            declarations.Add(new Declaration(node.Name, BindingKind.CatchParameter, node, true, false, false));
        scopes.Add((@catch.Span.Offset, sequence++, new Scope(ScopeKind.Catch, @catch, declarations.AsReadOnly())));

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

        PushChildren(@catch, context);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Modules (clause 16.2).

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=7EEC92
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=D8573C
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=A77227
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=3F41E6
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=9059BA
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=B4C9B1
    // Broiler-Human:        PENDING
    private readonly HashSet<SyntaxNode> sloppyPlainFunctions = new(ReferenceEqualityComparer.Instance);

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=C45402
    // Broiler-Human:        PENDING
    private bool IsSloppyPlainFunction(Declaration declaration) => sloppyPlainFunctions.Contains(declaration.Node);

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=2FF186
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=36C73B
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=73EC67
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=2E5F8D
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=B79A4D
    // Broiler-Human:        PENDING
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
    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=3A79C2
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=0C2D53
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=118E73
    // Broiler-Human:        PENDING
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
    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=F6DB6B
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=DD74DF
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=923D84
    // Broiler-Human:        PENDING
    private sealed record LabelSet(string Name, LabelSet? Next);

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 D10 D11 declarations walk, ECMA-262 static semantics early errors; IP=Low; Security=Medium; Resources=1; Fingerprint=751BF7
    // Broiler-Human:        PENDING
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
    }
}
