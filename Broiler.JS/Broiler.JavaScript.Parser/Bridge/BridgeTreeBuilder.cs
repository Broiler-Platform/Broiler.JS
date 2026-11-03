#nullable enable

using System;
using System.Collections.Generic;
using Broiler.ECMAScript.Semantics;
using Broiler.ECMAScript.Syntax;
using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Statements;
using Broiler.JavaScript.ExpressionCompiler.Core;
using AstDeclarator = Broiler.JavaScript.Ast.VariableDeclarator;

namespace Broiler.JavaScript.Parser.Bridge;

/// <summary>
/// Builds this engine's <see cref="AstProgram"/> from the shared library's tree (milestone SJL-14.2, decision D44):
/// one node of the old parser's shape for each construct, with the facts the old parser recorded as it parsed and the
/// shared tree keeps raw or in its side tables.
/// </summary>
/// <remarks>
/// <para>
/// <b>The shapes</b> are the old parser's, node for node: a function declaration is an expression statement over a
/// function expression marked as a statement; an object literal's entries and a class's elements are
/// <see cref="AstClassProperty"/>s; a destructuring assignment's target and an arrow's parameters are the cover's
/// conversion (<c>ExpressionPatternExtensions.ToPattern</c>'s shapes), and a declaration's and a function's binding
/// patterns the binding parser's; a parenthesised expression is the expression marked <see cref="AstNode.WasParenthesized"/>,
/// a parenthesised sequence one spanning its parentheses; an optional chain is wrapped in <see cref="AstOptionalChain"/>
/// with its links marked as the old parser marked them; empty statements and empty blocks leave a block's statement
/// list as the old parser dropped them, the directive prologue's terminator kept where a directive follows it.
/// </para>
/// <para>
/// <b>The facts</b> (decision D44): the linked token chain (<see cref="BridgeTokens"/>); <see cref="AstBlock.HoistingScope"/>
/// from the declarations side table, in source order, each name once; <see cref="AstBlock.AnnexBFunctionNames"/> by the
/// old parser's rule over the same declarations, walked in source order (<see cref="BridgeScopes"/>);
/// <see cref="AstBlock.IsSyntheticFunctionStatementBlock"/> and the Annex B.3.4 block, synthesised here from the shared
/// tree, which keeps the <c>if</c> clause raw (JSD-0080's decision D23); <c>ContainsContinue</c> on each loop;
/// <see cref="AstNode.WasParenthesized"/>; <see cref="TokenTypes.Decimal"/> for the <c>m</c> suffix. Not
/// <c>IsLoopBookkeeping</c>, which the compiler's for-loop pass sets. And the bridge refuses an import attribute this
/// engine cannot honour (catalogue row EX0021, an engine row the library never reports), as its parser does.
/// </para>
/// <para>
/// The walk recurses: the tree it walks is within the library's tree-depth constant, on the parse thread's stack
/// (<see cref="SharedSyntaxBridge"/>).
/// </para>
/// </remarks>
internal sealed partial class BridgeTreeBuilder
{
    private readonly string text;
    private readonly int baseOffset;
    private readonly BridgeTokens tokens;
    private readonly BridgeScopes scopes;
    private readonly BridgeRequest request;

    // The old parser's ParsedContinueCount: a loop contains a continue when one was built while its body was.
    private int continueCount;

    internal BridgeTreeBuilder(in StringSpan code, BridgeTokens tokens, DeclarationTables declarations, BridgeRequest request)
    {
        text = code.Source ?? string.Empty;
        baseOffset = code.Offset;
        this.tokens = tokens;
        this.request = request;
        scopes = new BridgeScopes(declarations, tokens);
    }

    /// <summary>The program, with its hoisting facts and its top-level-<c>await</c> fact.</summary>
    internal AstProgram Build(ProgramNode program)
    {
        scopes.EnterBody();
        var statements = StatementList(program.Body);
        var hoisting = scopes.Hoisting(program);
        var annexB = scopes.LeaveBody();

        var start = tokens.After(tokens.BaseOffset);
        return new AstProgram(start, tokens.Eof, statements, program.HasTopLevelAwait)
        {
            HoistingScope = hoisting,
            AnnexBFunctionNames = annexB,
        };
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Tokens at a node's edges.

    private FastToken Start(SyntaxNode node) => tokens.At(node.Span.Offset);

    private FastToken End(SyntaxNode node) => tokens.EndingAt(node.Span.Offset + node.Span.Length);

    private static int EndOf(SyntaxNode node) => node.Span.Offset + node.Span.Length;

    // The first token inside a braced body, as the old parser's Block began: its first statement's, or the closing
    // brace's where it is empty.
    private FastToken BodyStart(IReadOnlyList<Statement> body, SyntaxNode owner)
        => body.Count > 0 ? Start(body[0]) : End(owner);

    // ---------------------------------------------------------------------------------------------------------------
    // Statement lists.

    /// <summary>
    /// A block's statements as the old parser's Block kept them: an empty statement or an empty block dropped, but the
    /// first of them held while the directive prologue is open, and kept where a literal expression statement follows it.
    /// </summary>
    private Sequence<AstStatement> StatementList(IReadOnlyList<Statement> body)
    {
        var list = new Sequence<AstStatement>();
        AstStatement? held = null;
        var prologuePossible = true;
        foreach (var item in body)
        {
            var statement = Statement(item);
            if (!prologuePossible)
            {
                if (!IsElidable(statement))
                    list.Add(statement);
                continue;
            }

            if (IsElidable(statement))
            {
                held ??= statement;
                continue;
            }

            if (statement is AstExpressionStatement { Expression: AstLiteral })
            {
                if (held != null)
                {
                    list.Add(held);
                    held = null;
                    prologuePossible = false;
                }

                list.Add(statement);
                continue;
            }

            held = null;
            prologuePossible = false;
            list.Add(statement);
        }

        return list;
    }

    private static bool IsElidable(AstStatement statement)
        => statement switch
        {
            AstBlock block => block.Statements.Count == 0,
            AstExpressionStatement { Expression: AstEmptyExpression } => true,
            _ => false,
        };

    // ---------------------------------------------------------------------------------------------------------------
    // Statements.

    private AstStatement Statement(Statement statement)
    {
        switch (statement)
        {
            case BlockStatement block:
                return Block(block);
            case EmptyStatement:
                return new AstExpressionStatement(new AstEmptyExpression(Start(statement)));
            case ExpressionStatement expression:
            {
                var value = Expression(expression.Expression);
                return new AstExpressionStatement(Start(statement), End(statement), value);
            }

            case VariableDeclaration declaration:
                return Declaration(declaration);
            case FunctionDeclaration function:
                return new AstExpressionStatement(FunctionStatement(function.Function, ifClause: false));
            case ClassDeclaration declaration:
                return new AstExpressionStatement(Class(declaration.Class, isDeclaration: true));
            case IfStatement @if:
                return If(@if);
            case WhileStatement @while:
            {
                var test = Expression(@while.Test);
                var body = Statement(@while.Body);
                return new AstWhileStatement(Start(statement), End(statement), test, body);
            }

            case DoWhileStatement doWhile:
            {
                var body = Statement(doWhile.Body);
                var test = Expression(doWhile.Test);
                return new AstDoWhileStatement(Start(statement), End(statement), test, body);
            }

            case ForStatement @for:
                return For(@for);
            case ForInStatement forIn:
                return ForIn(forIn);
            case ForOfStatement forOf:
                return ForOf(forOf);
            case ContinueStatement @continue:
                continueCount++;
                return new AstContinueStatement(Start(statement), End(statement), Label(@continue.Label));
            case BreakStatement @break:
                return new AstBreakStatement(Start(statement), End(statement), Label(@break.Label));
            case ReturnStatement @return:
            {
                var argument = @return.Argument is null ? null : Expression(@return.Argument);
                return new AstReturnStatement(Start(statement), End(statement), argument);
            }

            case ThrowStatement @throw:
            {
                var argument = Expression(@throw.Argument);
                return new AstThrowStatement(Start(statement), End(statement), argument);
            }

            case TryStatement @try:
                return Try(@try);
            case SwitchStatement @switch:
                return Switch(@switch);
            case LabelledStatement labelled:
            {
                var label = tokens.Name(labelled.Label.Span.Offset, labelled.Label.Span.Length, labelled.Label.Name, labelled.Label.IsEscaped);
                return new AstLabeledStatement(label, Statement(labelled.Body));
            }

            case WithStatement with:
            {
                var @object = Expression(with.Object);
                var body = Statement(with.Body);
                return new AstWithStatement(Start(statement), End(statement), @object, body);
            }

            case DebuggerStatement:
                return new AstDebuggerStatement(Start(statement));
            case ImportDeclaration import:
                return Import(import);
            case ExportNamedDeclaration export:
                return Export(export);
            case ExportDefaultDeclaration exportDefault:
                return ExportDefault(exportDefault);
            case ExportAllDeclaration exportAll:
                return ExportAll(exportAll);
            default:
                throw Unsupported(statement);
        }
    }

    private AstBlock Block(BlockStatement block)
    {
        scopes.EnterBlock();
        var statements = StatementList(block.Body);
        scopes.LeaveBlock();
        return new AstBlock(BodyStart(block.Body, block), End(block), statements)
        {
            HoistingScope = scopes.Hoisting(block),
            AnnexBFunctionNames = Sequence<StringSpan>.Empty,
        };
    }

    private AstIdentifier? Label(IdentifierName? label)
        => label is null ? null : new AstIdentifier(tokens.Name(label.Span.Offset, label.Span.Length, label.Name, label.IsEscaped));

    private AstVariableDeclaration Declaration(VariableDeclaration declaration)
    {
        var (kind, @using, @await) = declaration.DeclarationKind switch
        {
            DeclarationKind.Let => (FastVariableKind.Let, false, false),
            DeclarationKind.Const => (FastVariableKind.Const, false, false),
            DeclarationKind.Using => (FastVariableKind.Const, true, false),
            DeclarationKind.AwaitUsing => (FastVariableKind.Const, true, true),
            _ => (FastVariableKind.Var, false, false),
        };

        var declarators = new Sequence<AstDeclarator>(declaration.Declarations.Count);
        foreach (var declarator in declaration.Declarations)
        {
            var target = BindingPattern(declarator.Target, kind);
            var initializer = declarator.Initializer is null ? null : Expression(declarator.Initializer);
            declarators.Add(new AstDeclarator(target, initializer));
        }

        return new AstVariableDeclaration(Start(declaration), End(declaration), declarators, kind, @using, @await);
    }

    private AstStatement If(IfStatement @if)
    {
        var test = Expression(@if.Test);
        var consequent = Clause(@if.Consequent);
        var alternate = @if.Alternate is null ? null : Clause(@if.Alternate);
        return new AstIfStatement(Start(@if), End(@if), test, consequent, alternate);
    }

    // An `if` clause, as the old parser's NestedStatement read it: a function declaration there is Annex B.3.4's,
    // scoped to a block of its own, which the old parser made explicit (a synthetic block) where the function's name
    // is a parameter of the enclosing function, and otherwise registered for Annex B's hoisting alone.
    private AstStatement Clause(Statement clause)
    {
        if (clause is not FunctionDeclaration function)
            return Statement(clause);

        if (function.Function.Name is { } name && scopes.ShadowsParameter(RawName(name)))
        {
            scopes.EnterBlock();
            var inner = new AstExpressionStatement(FunctionStatement(function.Function, ifClause: false));
            scopes.LeaveBlock();
            return new AstBlock(Start(function), End(function), new Sequence<AstStatement> { inner })
            {
                HoistingScope = scopes.Hoisting(function),
                AnnexBFunctionNames = Sequence<StringSpan>.Empty,
                IsSyntheticFunctionStatementBlock = true,
            };
        }

        return new AstExpressionStatement(FunctionStatement(function.Function, ifClause: true));
    }

    // The name's text as written: the old parser compared the token's span, not its cooked name.
    private string RawName(SyntaxNode name) => text.Substring(name.Span.Offset, name.Span.Length);

    private AstStatement For(ForStatement @for)
    {
        scopes.EnterFor();
        try
        {
            AstNode? init = @for.Init switch
            {
                null => null,
                VariableDeclaration declaration => Declaration(declaration),
                Expression expression => Expression(expression),
                _ => throw Unsupported(@for.Init),
            };
            var test = @for.Test is null ? null : Expression(@for.Test);
            var update = @for.Update is null ? null : Expression(@for.Update);
            var before = continueCount;
            var body = Statement(@for.Body);
            return new AstForStatement(Start(@for), End(@for), init!, test!, update!, body)
            {
                ContainsContinue = continueCount != before,
            };
        }
        finally
        {
            scopes.LeaveFor();
        }
    }

    private AstStatement ForIn(ForInStatement forIn)
    {
        scopes.EnterFor();
        try
        {
            var left = ForHead(forIn.Left);
            var right = Expression(forIn.Right);
            var before = continueCount;
            var body = Statement(forIn.Body);
            return new AstForInStatement(Start(forIn), End(forIn), left, right, body)
            {
                ContainsContinue = continueCount != before,
            };
        }
        finally
        {
            scopes.LeaveFor();
        }
    }

    private AstStatement ForOf(ForOfStatement forOf)
    {
        scopes.EnterFor();
        try
        {
            var left = ForHead(forOf.Left);
            var right = Expression(forOf.Right);
            var before = continueCount;
            var body = Statement(forOf.Body);
            return new AstForOfStatement(Start(forOf), End(forOf), left, right, body, forOf.IsAwait)
            {
                ContainsContinue = continueCount != before,
            };
        }
        finally
        {
            scopes.LeaveFor();
        }
    }

    // A for-in or for-of head: a declaration, or a target the old parser reinterpreted from the expression it read.
    private AstNode ForHead(SyntaxNode left)
        => left switch
        {
            VariableDeclaration declaration => Declaration(declaration),
            Pattern pattern => CoverPattern(pattern),
            Expression expression => Expression(expression),
            _ => throw Unsupported(left),
        };

    private AstStatement Try(TryStatement @try)
    {
        var block = Block(@try.Block);
        AstExpression? parameter = null;
        AstStatement? handler = null;
        if (@try.Handler is { } clause)
        {
            var patterned = clause.Parameter is not null and not BindingIdentifier;
            if (patterned)
                scopes.EnterBlock();
            if (clause.Parameter is BindingIdentifier identifier)
                parameter = Name(identifier);
            else if (clause.Parameter is not null)
                parameter = BindingPattern(clause.Parameter, FastVariableKind.Let);
            handler = Block(clause.Body);
            if (patterned)
                scopes.LeaveBlock();
        }

        var finalizer = @try.Finalizer is null ? null : Block(@try.Finalizer);
        return new AstTryStatement(Start(@try), End(@try), block, parameter!, handler!, finalizer!);
    }

    private AstStatement Switch(SwitchStatement @switch)
    {
        var discriminant = Expression(@switch.Discriminant);
        scopes.EnterBlock();
        var cases = new Sequence<Case>(@switch.Cases.Count);
        foreach (var @case in @switch.Cases)
        {
            var test = @case.Test is null ? null : Expression(@case.Test);
            var statements = new Sequence<AstStatement>(@case.Consequent.Count);
            foreach (var item in @case.Consequent)
                statements.Add(Statement(item));
            cases.Add(new Case(test!, statements));
        }

        scopes.LeaveBlock();
        return new AstSwitchStatement(Start(@switch), End(@switch), discriminant, cases)
        {
            HoistingScope = scopes.Hoisting(@switch),
        };
    }

    private static NotSupportedException Unsupported(SyntaxNode? node)
        => new("The bridge builds no node of this engine's tree for the shared tree's " + (node?.Kind.ToString() ?? "absent node") + ".");
}
