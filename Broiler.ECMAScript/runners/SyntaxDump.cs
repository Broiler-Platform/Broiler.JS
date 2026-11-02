using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Broiler.ECMAScript.Semantics;
using Broiler.ECMAScript.Syntax;

namespace Broiler.ECMAScript.Runners;

/// <summary>
/// The canonical dump of a parse (stage SJL-13.2e; the home rule's part C, "a canonical dump of the tree (each node's kind,
/// span, raw facts and children in tree order), the side tables in their own order, and the ordered diagnostics"): one
/// line per item, every number in invariant digits, every string escaped to ASCII, nothing read from a hash table's order.
/// The token stream the parse read is dumped too. Two parses are the same parse when their dumps are the same bytes.
/// </summary>
/// <remarks>
/// The walk over the tree keeps its own stack, so that a tree as deep as the library's tree-depth constant is dumped on
/// any stack the parse ran on. The tree's children and the token stream are internal to the library, which grants its
/// tests and the Native AOT probe its internals for this dump alone.
/// </remarks>
internal static class SyntaxDump
{
    /// <summary>The canonical dump of <paramref name="result"/>, as UTF-8 text with LF line ends.</summary>
    internal static byte[] Dump(ParseResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        var text = new StringBuilder();
        text.Append("outcome ").Append(Number((int)result.Outcome)).Append('\n');
        foreach (var diagnostic in result.Diagnostics)
        {
            text.Append("diagnostic ").Append(diagnostic.Code).Append(' ').Append(diagnostic.MessageId).Append(' ')
                .Append(Number((int)diagnostic.Phase)).Append(' ');
            Span(text, diagnostic.Span);
            text.Append(' ');
            Escaped(text, diagnostic.SourceName);
            foreach (var argument in diagnostic.Arguments)
            {
                text.Append(' ');
                Escaped(text, argument);
            }

            text.Append('\n');
        }

        foreach (var token in result.Tokens)
        {
            text.Append("token ").Append(Number((int)token.Kind)).Append(' ').Append(Number(token.Start)).Append(' ')
                .Append(Number(token.End)).Append(' ').Append(Number(token.Line)).Append(' ').Append(Number(token.Column)).Append(' ')
                .Append(Number((int)token.Flags)).Append(' ');
            Escaped(text, token.Value);
            text.Append(' ');
            Escaped(text, token.Raw);
            text.Append(' ').Append(Bits(token.Number)).Append(' ').Append(Number(token.Split)).Append(' ').Append(Number((int)token.Word)).Append('\n');
        }

        var order = new Dictionary<SyntaxNode, int>(ReferenceEqualityComparer.Instance);
        if (result.Tree is { } tree)
        {
            var pending = new Stack<(SyntaxNode Node, int Depth)>();
            var children = new List<SyntaxNode>();
            pending.Push((tree, 0));
            while (pending.Count > 0)
            {
                var (node, depth) = pending.Pop();
                order[node] = order.Count;
                text.Append("node ").Append(Number(depth)).Append(' ').Append(Number((int)node.Kind)).Append(' ');
                Span(text, node.Span);
                Facts(text, node);
                text.Append('\n');
                children.Clear();
                node.AddChildren(children);
                for (var i = children.Count - 1; i >= 0; i--)
                    pending.Push((children[i], depth + 1));
            }
        }

        if (result.Declarations is { } tables)
        {
            foreach (var scope in tables.Scopes)
            {
                text.Append("scope ").Append(Number((int)scope.Kind)).Append(' ').Append(Reference(order, scope.Node)).Append('\n');
                foreach (var declaration in scope.Declarations)
                {
                    text.Append("declaration ");
                    Escaped(text, declaration.Name);
                    text.Append(' ').Append(Number((int)declaration.Kind)).Append(' ').Append(Reference(order, declaration.Node)).Append(' ')
                        .Append(Flag(declaration.IsLexical)).Append(Flag(declaration.IsBlockFunction)).Append(Flag(declaration.IsIfClauseFunction)).Append('\n');
                }
            }

            foreach (var function in tables.Functions)
            {
                text.Append("function ").Append(Reference(order, function.Node)).Append(' ').Append(Flag(function.IsStrict));
                foreach (var name in function.ParameterNames)
                {
                    text.Append(' ');
                    Escaped(text, name);
                }

                text.Append('\n');
            }
        }

        return Encoding.UTF8.GetBytes(text.ToString());
    }

    /// <summary>The SHA-256 of the canonical dump, in lowercase hexadecimal.</summary>
    internal static string Digest(ParseResult result) => Convert.ToHexStringLower(SHA256.HashData(Dump(result)));

    // The scalar facts of a node, its raw facts among them; its children are dumped as nodes of their own.
    private static void Facts(StringBuilder text, SyntaxNode node)
    {
        switch (node)
        {
            case ProgramNode program:
                Fact(text, (int)program.Goal, program.ParametersTrailingComma, program.HasTopLevelAwait);
                break;
            case Identifier identifier:
                Name(text, identifier.Name, identifier.IsEscaped);
                break;
            case IdentifierName name:
                Name(text, name.Name, name.IsEscaped);
                break;
            case PrivateIdentifier name:
                Name(text, name.Name, name.IsEscaped);
                break;
            case BindingIdentifier name:
                Name(text, name.Name, name.IsEscaped);
                break;
            case BooleanLiteral literal:
                text.Append(' ').Append(Flag(literal.Value));
                break;
            case NumericLiteral literal:
                text.Append(' ').Append(Bits(literal.Value)).Append(' ').Append(Number((int)literal.Flags)).Append(' ');
                Escaped(text, literal.Text);
                break;
            case StringLiteral literal:
                text.Append(' ').Append(Number((int)literal.Flags)).Append(' ');
                Escaped(text, literal.Value);
                break;
            case RegularExpressionLiteral literal:
                text.Append(' ');
                Escaped(text, literal.Pattern);
                text.Append(' ');
                Escaped(text, literal.Flags);
                break;
            case TemplateElement element:
                text.Append(' ');
                Escaped(text, element.Cooked);
                text.Append(' ');
                Escaped(text, element.Raw);
                break;
            case ArrayLiteral literal:
                Fact(text, 0, literal.TrailingComma, false);
                break;
            case ObjectLiteral literal:
                Fact(text, 0, literal.TrailingComma, false);
                break;
            case PropertyDefinition property:
                Fact(text, (int)property.PropertyKind, false, false);
                break;
            case MemberExpression member:
                Fact(text, 0, member.IsComputed, member.IsOptional);
                break;
            case CallExpression call:
                Fact(text, 0, call.TrailingComma, call.IsOptional);
                break;
            case NewExpression expression:
                Fact(text, 0, expression.HasArgumentList, expression.TrailingComma);
                break;
            case ImportCall call:
                Fact(text, (int)call.Phase, call.TrailingComma, false);
                break;
            case UpdateExpression update:
                Fact(text, (int)update.Operator, update.IsPrefix, false);
                break;
            case UnaryExpression unary:
                Fact(text, (int)unary.Operator, false, false);
                break;
            case BinaryExpression binary:
                Fact(text, (int)binary.Operator, false, false);
                break;
            case LogicalExpression logical:
                Fact(text, (int)logical.Operator, false, false);
                break;
            case AssignmentExpression assignment:
                Fact(text, (int)assignment.Operator, false, false);
                break;
            case YieldExpression yield:
                Fact(text, 0, yield.IsDelegate, false);
                break;
            case FunctionNode function:
                Fact(text, 0, function.IsAsync, function.IsGenerator);
                Fact(text, 0, function.IsArrow, function.ParametersTrailingComma);
                break;
            case MethodDefinition method:
                Fact(text, (int)method.MethodKind, method.IsStatic, false);
                break;
            case FieldDefinition field:
                Fact(text, 0, field.IsStatic, field.IsAccessor);
                break;
            case ImportDeclaration declaration:
                Fact(text, (int)declaration.Phase, declaration.NamedImportsTrailingComma, declaration.AttributesTrailingComma);
                break;
            case ExportNamedDeclaration declaration:
                Fact(text, 0, declaration.SpecifiersTrailingComma, declaration.AttributesTrailingComma);
                break;
            case ExportAllDeclaration declaration:
                Fact(text, 0, declaration.AttributesTrailingComma, false);
                break;
            case ArrayPattern pattern:
                Fact(text, 0, pattern.TrailingComma, false);
                break;
            case ObjectPattern pattern:
                Fact(text, 0, pattern.TrailingComma, false);
                break;
            case PatternProperty property:
                Fact(text, 0, property.IsShorthand, false);
                break;
            case VariableDeclaration declaration:
                Fact(text, (int)declaration.DeclarationKind, false, false);
                break;
            case ExpressionStatement statement:
                text.Append(' ');
                Escaped(text, statement.Directive);
                break;
            case ForOfStatement statement:
                Fact(text, 0, statement.IsAwait, false);
                break;
        }
    }

    private static void Fact(StringBuilder text, int value, bool first, bool second) =>
        text.Append(' ').Append(Number(value)).Append(' ').Append(Flag(first)).Append(Flag(second));

    private static void Name(StringBuilder text, string name, bool escaped)
    {
        text.Append(' ');
        Escaped(text, name);
        text.Append(' ').Append(Flag(escaped));
    }

    private static void Span(StringBuilder text, SourceSpan span) =>
        text.Append(Number(span.Offset)).Append(' ').Append(Number(span.Length)).Append(' ').Append(Number(span.Line)).Append(' ').Append(Number(span.Column));

    private static string Reference(Dictionary<SyntaxNode, int> order, SyntaxNode node) =>
        order.TryGetValue(node, out var index) ? Number(index) : "?";

    private static string Number(int value) => value.ToString(NumberFormatInfo.InvariantInfo);

    private static string Bits(double value) => BitConverter.DoubleToInt64Bits(value).ToString("X16", NumberFormatInfo.InvariantInfo);

    private static char Flag(bool value) => value ? '1' : '0';

    // A string as ASCII: printable ASCII but the backslash and the quote as written, every other code unit as \uXXXX; a
    // null string as a lone dash, so that null and empty differ.
    private static void Escaped(StringBuilder text, string? value)
    {
        if (value is null)
        {
            text.Append('-');
            return;
        }

        text.Append('"');
        foreach (var c in value)
        {
            if (c is >= ' ' and <= '~' and not '\\' and not '"')
                text.Append(c);
            else
                text.Append("\\u").Append(((int)c).ToString("X4", NumberFormatInfo.InvariantInfo));
        }

        text.Append('"');
    }
}
