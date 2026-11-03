#nullable enable

using System.Collections.Generic;
using Broiler.ECMAScript.Syntax;
using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Statements;
using Broiler.JavaScript.ExpressionCompiler.Core;

namespace Broiler.JavaScript.Parser.Bridge;

internal sealed partial class BridgeTreeBuilder
{
    private AstStatement Import(ImportDeclaration import)
    {
        var start = Start(import);
        var defaultBinding = import.DefaultBinding is null ? null : Name(import.DefaultBinding);
        var namespaceBinding = import.NamespaceBinding is null ? null : Name(import.NamespaceBinding);
        Sequence<(StringSpan, StringSpan)>? members = null;
        if (import.NamedImports is { } named)
        {
            members = new Sequence<(StringSpan, StringSpan)>(named.Count);
            foreach (var specifier in named)
                members.Add((specifier.Imported is null ? Local(specifier.Local) : ModuleExportName(specifier.Imported), Local(specifier.Local)));
        }

        var source = String(import.Source);
        return new AstImportStatement(start, defaultBinding, namespaceBinding, members, source, Attributes(import.Attributes));
    }

    private AstStatement Export(ExportNamedDeclaration export)
    {
        var start = Start(export);
        if (export.Declaration is { } declaration)
        {
            AstNode node = declaration switch
            {
                VariableDeclaration variables => Declaration(variables),
                FunctionDeclaration function => FunctionStatement(function.Function, ifClause: false),
                ClassDeclaration @class => Class(@class.Class, isDeclaration: true),
                _ => throw Unsupported(declaration),
            };
            return new AstExportStatement(start, node);
        }

        var members = new Sequence<(StringSpan, StringSpan)>(export.Specifiers.Count);
        foreach (var specifier in export.Specifiers)
            members.Add((ModuleExportName(specifier.Local), ModuleExportName(specifier.Exported ?? specifier.Local)));

        if (export.Source is { } from)
        {
            var source = String(from);
            return new AstExportStatement(start, members, source, source.End, Attributes(export.Attributes));
        }

        // The old parser ended a local export at the token after its closing brace.
        var close = text.LastIndexOf('}', EndOf(export) - 1, export.Span.Length);
        return new AstExportStatement(start, members, null, tokens.After(close + 1));
    }

    private AstStatement ExportDefault(ExportDefaultDeclaration export)
    {
        var start = Start(export);
        AstNode node = export.Declaration switch
        {
            FunctionDeclaration function => FunctionStatement(function.Function, ifClause: false),
            ClassDeclaration @class => Class(@class.Class, isDeclaration: true),
            Expression expression => Expression(expression),
            _ => throw Unsupported(export.Declaration),
        };
        return new AstExportStatement(start, node, IsDefault: true);
    }

    private AstStatement ExportAll(ExportAllDeclaration export)
    {
        var start = Start(export);
        AstNode? exported = export.Exported switch
        {
            null => null,
            StringLiteral @string => String(@string),
            IdentifierName name => new AstIdentifier(tokens.Name(name.Span.Offset, name.Span.Length, name.Name, name.IsEscaped)),
            _ => throw Unsupported(export.Exported),
        };
        var source = String(export.Source);
        return new AstExportStatement(start, exported, source, Attributes(export.Attributes));
    }

    // A ModuleExportName as the old parser held it: a string's value, a reserved word's or a keyword literal's span as
    // written, a name's cooked name.
    private StringSpan ModuleExportName(SyntaxNode name)
    {
        switch (name)
        {
            case StringLiteral @string:
                return new StringSpan(@string.Value);
            case IdentifierName identifier:
            {
                var token = tokens.Name(identifier.Span.Offset, identifier.Span.Length, identifier.Name, identifier.IsEscaped);
                return token.IsKeyword || token.Type != TokenTypes.Identifier ? token.Span : new AstIdentifier(token).Name;
            }

            case Identifier reference:
                return new AstIdentifier(tokens.Name(reference.Span.Offset, reference.Span.Length, reference.Name, reference.IsEscaped)).Name;
            default:
                throw Unsupported(name);
        }
    }

    private StringSpan Local(BindingIdentifier local) => Name(local).Name;

    /// <summary>
    /// A WithClause's attributes, as the old parser's ImportAttributes read them, and its refusal of a key this engine
    /// cannot honour (catalogue row EX0021, an engine row the library never reports): any key but <c>type</c>.
    /// </summary>
    private IFastEnumerable<(StringSpan, AstLiteral)>? Attributes(IReadOnlyList<ImportAttribute>? attributes)
    {
        if (attributes is null)
            return null;

        var list = new Sequence<(StringSpan, AstLiteral)>(attributes.Count);
        foreach (var attribute in attributes)
        {
            StringSpan key;
            FastToken keyToken;
            switch (attribute.Key)
            {
                case StringLiteral @string:
                    var literal = String(@string);
                    keyToken = literal.Start;
                    key = literal.StringValue;
                    break;
                case IdentifierName name:
                    keyToken = tokens.Name(name.Span.Offset, name.Span.Length, name.Name, name.IsEscaped);
                    key = keyToken.IsKeyword || keyToken.Type != TokenTypes.Identifier ? keyToken.Span : new AstIdentifier(keyToken).Name;
                    break;
                default:
                    throw Unsupported(attribute.Key);
            }

            if (!key.Equals("type"))
                throw new FastParseException(keyToken, $"Invalid attribute key \"{key}\".");
            list.Add((key, String(attribute.Value)));
        }

        return list;
    }
}
