#nullable enable
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Statements;
using Broiler.JavaScript.ExpressionCompiler.Core;

namespace Broiler.JavaScript.Parser;

partial class FastParser
{
    bool Import(FastToken token, out AstStatement statement)
    {
        stream.Consume();

        AstIdentifier id;

        if (stream.CheckAndConsume(TokenTypes.Multiply))
        {
            stream.ExpectContextualKeyword(FastKeywords.@as);

            if (!Identitifer(out id))
                throw stream.Unexpected();

            RejectReservedImportedBinding(id);
            stream.ExpectContextualKeyword(FastKeywords.from);

            var literal = ExpectStringLiteral();
            var attrs = ImportAttributes();

            isAsync = true;
            statement = new AstImportStatement(token, null, id, null, literal, attrs);

            return true;

        }

        AstIdentifier? all = null;
        IFastEnumerable<(StringSpan, StringSpan)>? names = null;

        if (Identitifer(out id))
        {
            RejectReservedImportedBinding(id);

            if (stream.CheckAndConsume(TokenTypes.Comma))
            {
                if (stream.CheckAndConsume(TokenTypes.Multiply))
                {
                    stream.ExpectContextualKeyword(FastKeywords.@as);

                    if (!Identitifer(out all))
                        throw stream.Unexpected();

                    RejectReservedImportedBinding(all);
                }
                else if (ImportNames(out var n))
                {
                    names = n;
                }
                else throw stream.Unexpected();
            }

            stream.ExpectContextualKeyword(FastKeywords.from);

            var literal = ExpectStringLiteral();
            var attrs = ImportAttributes();

            isAsync = true;
            statement = new AstImportStatement(token, id, all, names, literal, attrs);

            return true;
        }

        if (ImportNames(out names))
        {
            if (stream.CheckAndConsume(TokenTypes.Comma))
            {
                if (!Identitifer(out id))
                    throw stream.Unexpected();

                RejectReservedImportedBinding(id);
            }

            stream.ExpectContextualKeyword(FastKeywords.from);

            var literal = ExpectStringLiteral();
            var attrs = ImportAttributes();

            isAsync = true;
            statement = new AstImportStatement(token, id, all, names, literal, attrs);

            return true;
        }

        throw stream.Unexpected();

        bool ImportNames(out IFastEnumerable<(StringSpan, StringSpan)>? names)
        {
            if (!stream.CheckAndConsume(TokenTypes.CurlyBracketStart))
            {
                names = null;
                return false;
            }

            var list = new Sequence<(StringSpan, StringSpan)>();

            while (!stream.CheckAndConsume(TokenTypes.CurlyBracketEnd))
            {
                if (!Identitifer(out var id))
                    throw stream.Unexpected();

                if (stream.CheckAndConsumeContextualKeyword(FastKeywords.@as))
                {
                    if (!Identitifer(out var asName))
                        throw stream.Unexpected();

                    RejectReservedImportedBinding(asName);
                    list.Add((id.Name, asName.Name));
                }
                else
                {
                    RejectReservedImportedBinding(id);
                    list.Add((id.Name, id.Name));
                }

                if (stream.CheckAndConsume(TokenTypes.Comma))
                    continue;

                if (stream.CheckAndConsume(TokenTypes.CurlyBracketEnd))
                    break;

                throw stream.Unexpected();
            }

            names = list;
            return true;
        }
    }

    /// <summary>
    /// An ImportedBinding is a BindingIdentifier, so <c>await</c> cannot be one: an
    /// ImportDeclaration only appears in module code, where <c>await</c> is reserved.
    /// </summary>
    private void RejectReservedImportedBinding(AstIdentifier binding)
    {
        if (binding.Start.IsKeyword && binding.Start.Keyword == FastKeywords.await && isModuleGoal)
            throw new FastParseException(binding.Start, "'await' is reserved in module code");
    }

    /// <summary>
    /// Parse optional import attributes: <c>with { key: "value", ... }</c>
    /// (ES2025 §2.3 Import Attributes).
    /// </summary>
    IFastEnumerable<(StringSpan, AstLiteral)>? ImportAttributes()
    {
        // The `with` keyword is a reserved keyword, so use CheckAndConsume(FastKeywords)
        if (!stream.CheckAndConsume(FastKeywords.@with))
            return null;

        if (!stream.CheckAndConsume(TokenTypes.CurlyBracketStart))
            throw stream.Unexpected();

        var list = new Sequence<(StringSpan, AstLiteral)>();

        while (!stream.CheckAndConsume(TokenTypes.CurlyBracketEnd))
        {
            // AttributeKey : IdentifierName | StringLiteral. The comment already said so; only the
            // identifier half was implemented, so `with { "type": "json" }` — the quoted form the
            // proposal's own examples use — was rejected as an unexpected token. An IdentifierName
            // also admits reserved words, both shapes of them (see FastParser.Export's
            // ModuleExportName for why there are two).
            StringSpan key;
            var keyToken = stream.Current;
            if (keyToken.Type == TokenTypes.String)
            {
                key = ExpectStringLiteral().StringValue;
            }
            else if (keyToken.IsKeyword || IsKeywordPropertyName(keyToken.Type))
            {
                stream.Consume();
                key = keyToken.Span;
            }
            else if (Identitifer(out var keyId))
            {
                key = keyId.Name;
            }
            else
            {
                throw stream.Unexpected();
            }

            // Expect colon separator
            if (!stream.CheckAndConsume(TokenTypes.Colon))
                throw stream.Unexpected();

            // Attribute value must be a string literal
            var value = ExpectStringLiteral();
            list.Add((key, value));

            if (stream.CheckAndConsume(TokenTypes.Comma))
                continue;

            if (stream.CheckAndConsume(TokenTypes.CurlyBracketEnd))
                break;

            throw stream.Unexpected();
        }

        return list;
    }
}
