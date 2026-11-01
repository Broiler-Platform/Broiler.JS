// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;

namespace Broiler.ECMAScript.Syntax;

/// <summary>Modules (ECMA-262 clause 16.2): ModuleItems, import and export declarations with their WithClauses, and the import phases under their flags.</summary>
internal sealed partial class Parser
{
    /// <summary>A ModuleItem (clause 16.2): an ImportDeclaration, an ExportDeclaration or a StatementListItem.</summary>
    private Statement ParseModuleItem()
    {
        var token = Ex;
        if (IsWord(token, Word.Import))
        {
            var next = PeekAfter(LexicalGoal.RegExp, LexicalGoal.Div);
            if (next.Kind is TokenKind.LeftParen or TokenKind.Dot)
                return ParseStatementListItem();
            return WithNesting(token, ParseImportDeclaration);
        }

        if (IsWord(token, Word.Export))
            return WithNesting(token, () => ParseExportDeclaration(System.Array.Empty<Decorator>()));

        if (token.Kind == TokenKind.At && (proposals & ProposalFlags.Decorators) != 0)
        {
            var decorators = ParseDecorators();
            var after = Ex;
            if (IsWord(after, Word.Export))
                return WithNesting(token, () => ParseExportDeclaration(decorators));
            if (!IsWord(after, Word.Class))
                throw Missing(after, "class");
            return WithNesting(token, () => ParseClassDeclaration(decorators, isDefault: false));
        }

        return ParseStatementListItem();
    }

    private Statement WithNesting(Token at, System.Func<Statement> parse)
    {
        EnterNesting(at);
        try
        {
            return parse();
        }
        finally
        {
            ExitNesting();
        }
    }

    /// <summary>
    /// An ImportDeclaration (clause 16.2.2): a module specifier alone, or an ImportClause and a FromClause, and a WithClause;
    /// <c>import source x from</c> and <c>import defer * as ns from</c> under their flags (decision D17).
    /// </summary>
    private Statement ParseImportDeclaration()
    {
        var start = StartOf(Advance());
        var token = Op;
        if (token.Kind == TokenKind.StringLiteral)
        {
            var bare = ParseModuleSpecifier();
            var (bareAttributes, bareTrailing) = ParseWithClause();
            ConsumeSemicolon();
            return new ImportDeclaration(SpanFrom(start), ImportPhase.Evaluation, null, null, null, false, bare, bareAttributes, bareTrailing);
        }

        var phase = ImportPhase.Evaluation;
        if (IsWord(token, Word.Source) && (proposals & ProposalFlags.ImportSourceDeclarations) != 0 && IsSourcePhase())
            phase = ImportPhase.Source;
        else if (IsWord(token, Word.Defer) && (proposals & ProposalFlags.ImportDeferDeclarations) != 0 && PeekAfter(LexicalGoal.Div, LexicalGoal.Div).Kind == TokenKind.Star)
            phase = ImportPhase.Defer;

        BindingIdentifier? defaultBinding = null;
        BindingIdentifier? namespaceBinding = null;
        IReadOnlyList<ImportSpecifier>? named = null;
        var namedTrailing = false;
        if (phase == ImportPhase.Source)
        {
            Advance();
            defaultBinding = ParseBindingIdentifier();
        }
        else if (phase == ImportPhase.Defer)
        {
            Advance();
            Expect(TokenKind.Star);
            ExpectWord(Word.As, "as");
            namespaceBinding = ParseBindingIdentifier();
        }
        else
        {
            if (token.Kind == TokenKind.IdentifierName)
            {
                defaultBinding = ParseBindingIdentifier();
                if (!Eat(TokenKind.Comma))
                {
                    ExpectWord(Word.From, "from");
                    var defaultSource = ParseModuleSpecifier();
                    var (defaultAttributes, defaultTrailing) = ParseWithClause();
                    ConsumeSemicolon();
                    return new ImportDeclaration(SpanFrom(start), phase, defaultBinding, null, null, false, defaultSource, defaultAttributes, defaultTrailing);
                }

                if (Op.Kind is not (TokenKind.Star or TokenKind.LeftBrace))
                    throw Unexpected(cur);
            }

            if (Op.Kind == TokenKind.Star)
            {
                Advance();
                ExpectWord(Word.As, "as");
                namespaceBinding = ParseBindingIdentifier();
            }
            else if (Op.Kind == TokenKind.LeftBrace)
            {
                (named, namedTrailing) = ParseNamedImports();
            }
            else if (defaultBinding is null)
            {
                throw Unexpected(cur);
            }
        }

        ExpectWord(Word.From, "from");
        var source = ParseModuleSpecifier();
        var (attributes, attributesTrailing) = ParseWithClause();
        ConsumeSemicolon();
        return new ImportDeclaration(SpanFrom(start), phase, defaultBinding, namespaceBinding, named, namedTrailing, source, attributes, attributesTrailing);
    }

    /// <summary>Whether the current <c>source</c> begins a source-phase import: a binding follows that is not <c>from</c>, or <c>from</c> twice (<c>import source from from "m"</c>).</summary>
    private bool IsSourcePhase()
    {
        Peek(LexicalGoal.Div);
        var state = tokenizer.Save();
        var first = tokenizer.Next(LexicalGoal.Div);
        var second = tokenizer.Next(LexicalGoal.Div);
        tokenizer.Restore(state);
        if (!CanBind(first))
            return false;
        return !IsWord(first, Word.From) || IsWord(second, Word.From);
    }

    /// <summary>NamedImports (clause 16.2.2): ImportSpecifiers, each a binding or a ModuleExportName <c>as</c> a binding.</summary>
    private (IReadOnlyList<ImportSpecifier>, bool) ParseNamedImports()
    {
        Expect(TokenKind.LeftBrace);
        var list = new List<ImportSpecifier>();
        var trailing = false;
        while (Op.Kind != TokenKind.RightBrace)
        {
            var token = cur;
            var start = StartOf(token);
            SyntaxNode? imported = null;
            if (token.Kind == TokenKind.StringLiteral)
            {
                Advance();
                imported = StringLiteralOf(token);
                ExpectWord(Word.As, "as");
            }
            else if (token.Kind == TokenKind.IdentifierName && IsWord(PeekAfter(LexicalGoal.Div, LexicalGoal.Div), Word.As))
            {
                Advance();
                imported = new IdentifierName(SpanOf(token), token.Value!, token.IsEscaped);
                Advance();
            }

            // An ImportedBinding: a reserved word is no binding (row D40).
            var local = ParseBindingIdentifier();
            list.Add(new ImportSpecifier(SpanFrom(start), imported, local));
            if (Op.Kind == TokenKind.RightBrace)
                break;
            Expect(TokenKind.Comma);
            if (Op.Kind == TokenKind.RightBrace)
                trailing = true;
        }

        Advance();
        return (Lists.Freeze(list), trailing);
    }

    /// <summary>A ModuleSpecifier (clause 16.2.2): a StringLiteral.</summary>
    private StringLiteral ParseModuleSpecifier()
    {
        var token = Op;
        if (token.Kind != TokenKind.StringLiteral)
            throw Missing(token, "string");
        Advance();
        return StringLiteralOf(token);
    }

    /// <summary>A WithClause (clause 16.2.2), or none: AttributeKeys, each an IdentifierName or a StringLiteral, with string values.</summary>
    private (IReadOnlyList<ImportAttribute>?, bool) ParseWithClause()
    {
        if (!IsWord(Op, Word.With))
            return (null, false);
        Advance();
        Expect(TokenKind.LeftBrace);
        var list = new List<ImportAttribute>();
        var trailing = false;
        while (Op.Kind != TokenKind.RightBrace)
        {
            var token = cur;
            var start = StartOf(token);
            SyntaxNode key;
            if (token.Kind == TokenKind.IdentifierName)
                key = new IdentifierName(SpanOf(token), token.Value!, token.IsEscaped);
            else if (token.Kind == TokenKind.StringLiteral)
                key = StringLiteralOf(token);
            else
                throw Unexpected(token);
            Advance();
            Expect(TokenKind.Colon);
            var value = ParseModuleSpecifier();
            list.Add(new ImportAttribute(SpanFrom(start), key, value));
            if (Op.Kind == TokenKind.RightBrace)
                break;
            Expect(TokenKind.Comma);
            if (Op.Kind == TokenKind.RightBrace)
                trailing = true;
        }

        Advance();
        return (Lists.Freeze(list), trailing);
    }

    /// <summary>A ModuleExportName (clause 16.2.3): an IdentifierName, any word among them, or a StringLiteral.</summary>
    private SyntaxNode ParseModuleExportName()
    {
        var token = Op;
        if (token.Kind == TokenKind.IdentifierName)
        {
            Advance();
            return new IdentifierName(SpanOf(token), token.Value!, token.IsEscaped);
        }

        if (token.Kind == TokenKind.StringLiteral)
        {
            Advance();
            return StringLiteralOf(token);
        }

        throw Unexpected(token);
    }

    /// <summary>
    /// An ExportDeclaration (clause 16.2.3): <c>export *</c>, NamedExports with or without a FromClause, a declaration, or
    /// <c>export default</c> with a declaration or an AssignmentExpression.
    /// </summary>
    private Statement ParseExportDeclaration(IReadOnlyList<Decorator> decorators)
    {
        var keyword = Ex;
        var start = decorators.Count > 0 ? decorators[0].Span : StartOf(keyword);
        Advance();
        var token = Ex;

        if (token.Kind == TokenKind.Star && decorators.Count == 0)
        {
            Advance();
            SyntaxNode? exported = null;
            if (IsWord(Op, Word.As))
            {
                Advance();
                exported = ParseModuleExportName();
            }

            ExpectWord(Word.From, "from");
            var source = ParseModuleSpecifier();
            var (attributes, trailing) = ParseWithClause();
            ConsumeSemicolon();
            return new ExportAllDeclaration(SpanFrom(start), exported, source, attributes, trailing);
        }

        if (token.Kind == TokenKind.LeftBrace && decorators.Count == 0)
        {
            Advance();
            var specifiers = new List<ExportSpecifier>();
            var trailing = false;
            while (Op.Kind != TokenKind.RightBrace)
            {
                var specifierStart = StartOf(cur);
                var local = ParseModuleExportName();
                SyntaxNode? exported = null;
                if (IsWord(Op, Word.As))
                {
                    Advance();
                    exported = ParseModuleExportName();
                }

                specifiers.Add(new ExportSpecifier(SpanFrom(specifierStart), local, exported));
                if (Op.Kind == TokenKind.RightBrace)
                    break;
                Expect(TokenKind.Comma);
                if (Op.Kind == TokenKind.RightBrace)
                    trailing = true;
            }

            Advance();
            StringLiteral? source = null;
            IReadOnlyList<ImportAttribute>? attributes = null;
            var attributesTrailing = false;
            if (IsWord(Op, Word.From))
            {
                Advance();
                source = ParseModuleSpecifier();
                (attributes, attributesTrailing) = ParseWithClause();
            }

            ConsumeSemicolon();
            return new ExportNamedDeclaration(SpanFrom(start), null, Lists.Freeze(specifiers), trailing, source, attributes, attributesTrailing);
        }

        if (IsWord(token, Word.Default))
        {
            Advance();
            var next = Ex;
            SyntaxNode declaration;
            if (IsWord(next, Word.Function) && decorators.Count == 0)
            {
                declaration = ParseFunctionDeclaration(isAsync: false, isDefault: true);
            }
            else if (IsWord(next, Word.Async) && IsAsyncFunction() && decorators.Count == 0)
            {
                declaration = ParseFunctionDeclaration(isAsync: true, isDefault: true);
            }
            else if (IsWord(next, Word.Class))
            {
                declaration = ParseClassDeclaration(decorators, isDefault: true);
            }
            else if (next.Kind == TokenKind.At && (proposals & ProposalFlags.Decorators) != 0 && decorators.Count == 0)
            {
                var own = ParseDecorators();
                if (!IsWord(Ex, Word.Class))
                    throw Missing(Ex, "class");
                declaration = ParseClassDeclaration(own, isDefault: true);
            }
            else if (decorators.Count == 0)
            {
                declaration = ParseAssignment(noIn: false);
                ConsumeSemicolon();
            }
            else
            {
                throw Missing(next, "class");
            }

            return new ExportDefaultDeclaration(SpanFrom(start), declaration);
        }

        Statement exportedDeclaration;
        if (decorators.Count > 0 || token.Kind == TokenKind.At)
        {
            var own = decorators.Count > 0 ? decorators : (proposals & ProposalFlags.Decorators) != 0 ? ParseDecorators() : throw Unexpected(token);
            if (!IsWord(Ex, Word.Class))
                throw Missing(Ex, "class");
            exportedDeclaration = ParseClassDeclaration(own, isDefault: false);
        }
        else if (IsWord(token, Word.Var))
        {
            exportedDeclaration = ParseVariableStatement();
        }
        else if (IsWord(token, Word.Const) || (IsWord(token, Word.Let) && LetStartsDeclaration()))
        {
            exportedDeclaration = ParseLexicalDeclaration(token.Word == Word.Const ? DeclarationKind.Const : DeclarationKind.Let);
        }
        else if (IsWord(token, Word.Function))
        {
            exportedDeclaration = ParseFunctionDeclaration(isAsync: false, isDefault: false);
        }
        else if (IsWord(token, Word.Async) && IsAsyncFunction())
        {
            exportedDeclaration = ParseFunctionDeclaration(isAsync: true, isDefault: false);
        }
        else if (IsWord(token, Word.Class))
        {
            exportedDeclaration = ParseClassDeclaration(System.Array.Empty<Decorator>(), isDefault: false);
        }
        else
        {
            throw Unexpected(token);
        }

        return new ExportNamedDeclaration(SpanFrom(start), exportedDeclaration, System.Array.Empty<ExportSpecifier>(), false, null, null, false);
    }
}
