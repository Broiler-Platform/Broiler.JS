using System.Collections;
using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Broiler.JavaScript.Ast;
using Broiler.JavaScript.Ast.Misc;

namespace ParseCorpus;

/// <summary>
/// The canonical dump of a parsed tree (decision rule part D): each node's type, span, names, flags
/// and children, in tree order, as ASCII text whose SHA-256 is what a run reports.
/// </summary>
/// <remarks>
/// <para>
/// The dump is read from the tree by reflection, so it names no node member at compile time and
/// needs no change when the after commit adds or removes one (item 5 adds <c>ContainsContinue</c>
/// to the loop statements, for one). Everything a node holds publicly is in it: the public
/// instance fields and the public readable properties of each type from <c>AstNode</c> down to the
/// node's own, each type's members in declaration (metadata) order. <c>AstNode.Type</c>,
/// <c>Start</c> and <c>End</c> make the line's head; <c>AstNode.Code</c>, the source text the span
/// covers, is left out because the span already says it.
/// </para>
/// <para>
/// Names are written as they are, never renumbered: the parser's temporaries at the corpus commit
/// (a process-wide counter) and the pass's <c>d:n</c> temporaries at the after commit (JSD-0080
/// decision D22) both appear verbatim, which is what lets run 2 (the reverse order) show the
/// counter's dependence on parse order at the base. Lists are written in the order they enumerate
/// in, so a list ordered by a hash (control K2) shows.
/// </para>
/// <para>
/// Nothing in the text depends on the process: no object identity, hash code, address, culture or
/// clock. Numbers use the invariant culture ("R" for doubles), enums their names, and every
/// character outside printable ASCII is escaped as <c>\uXXXX</c> (UTF-16 code units, so a lone
/// surrogate is kept). The format is set out in README.md beside this file.
/// </para>
/// </remarks>
internal static class CanonicalDump
{
    private static readonly ConcurrentDictionary<Type, Member[]> Members = new();
    private static readonly Type AstNodeType = typeof(AstNode);

    internal static string Hash(string text)
    {
        var bytes = Encoding.ASCII.GetBytes(text);
        return Convert.ToHexStringLower(SHA256.HashData(bytes));
    }

    internal static string Of(AstNode root)
    {
        var writer = new StringBuilder();
        var ancestors = new HashSet<object>(ReferenceEqualityComparer.Instance);
        WriteNested(writer, 0, "program", root, ancestors);
        return writer.ToString();
    }

    internal static string OfException(Exception exception)
        => "throws " + exception.GetType().FullName + " " + Text.Quote(exception.Message) + "\n";

    private static void WriteNested(StringBuilder writer, int depth, string label, object value, HashSet<object> ancestors)
    {
        Indent(writer, depth);
        writer.Append(label).Append(':');

        if (value is AstNode node)
        {
            if (!ancestors.Add(node))
            {
                writer.Append(" cycle ").Append(node.GetType().FullName).Append('\n');
                return;
            }

            writer.Append(' ').Append(node.GetType().FullName)
                .Append(' ').Append(node.Type.ToString())
                .Append(" @").Append(Span(node.Start, node.End));
            WriteMembers(writer, depth, MembersOf(node.GetType()), node, ancestors);
            ancestors.Remove(node);
            return;
        }

        if (value is IEnumerable sequence && value is not string)
        {
            var items = new List<object?>();
            foreach (var item in sequence)
                items.Add(item);

            writer.Append(" list ").Append(items.Count.ToString(CultureInfo.InvariantCulture)).Append('\n');
            for (var i = 0; i < items.Count; i++)
            {
                var itemLabel = "[" + i.ToString(CultureInfo.InvariantCulture) + "]";
                if (IsInline(items[i]))
                {
                    Indent(writer, depth + 1);
                    writer.Append(itemLabel).Append(": ");
                    WriteInline(writer, items[i]);
                    writer.Append('\n');
                }
                else
                {
                    WriteNested(writer, depth + 1, itemLabel, items[i]!, ancestors);
                }
            }

            return;
        }

        // A value type that holds a node somewhere: VariableDeclarator, ObjectProperty, Case, a
        // tuple of a name and a literal.
        writer.Append(' ').Append(TypeName(value.GetType()));
        WriteMembers(writer, depth, MembersOf(value.GetType()), value, ancestors);
    }

    private static void WriteMembers(StringBuilder writer, int depth, Member[] members, object owner, HashSet<object> ancestors)
    {
        var values = new object?[members.Length];
        for (var i = 0; i < members.Length; i++)
            values[i] = members[i].Read(owner);

        for (var i = 0; i < members.Length; i++)
        {
            if (!IsInline(values[i]))
                continue;

            writer.Append(' ').Append(members[i].Label).Append('=');
            WriteInline(writer, values[i]);
        }

        writer.Append('\n');

        for (var i = 0; i < members.Length; i++)
        {
            if (IsInline(values[i]))
                continue;

            WriteNested(writer, depth + 1, members[i].Label, values[i]!, ancestors);
        }
    }

    // Inline: everything that is not, and does not hold, a node.
    private static bool IsInline(object? value)
    {
        switch (value)
        {
            case null:
            case string:
            case StringSpan:
            case FastToken:
            case Enum:
            case MemberFailure:
                return true;
            case AstNode:
                return false;
        }

        var type = value.GetType();
        if (type.IsPrimitive || value is decimal)
            return true;

        if (value is IEnumerable sequence)
        {
            foreach (var item in sequence)
            {
                if (!IsInline(item))
                    return false;
            }

            return true;
        }

        if (type.IsValueType)
        {
            foreach (var member in MembersOf(type))
            {
                if (!IsInline(member.Read(value)))
                    return false;
            }
        }

        // Any other class is written by its type name alone (see WriteInline).
        return true;
    }

    private static void WriteInline(StringBuilder writer, object? value)
    {
        switch (value)
        {
            case null:
                writer.Append("null");
                return;
            case MemberFailure failure:
                writer.Append("!throws ").Append(failure.TypeName);
                return;
            case string text:
                writer.Append(Text.Quote(text));
                return;
            case StringSpan span:
                writer.Append(span.Source == null ? "null" : Text.Quote(span.Value ?? string.Empty));
                return;
            case FastToken token:
                writer.Append(Token(token));
                return;
            case bool flag:
                writer.Append(flag ? "true" : "false");
                return;
            case Enum named:
                writer.Append(named.ToString().Replace(", ", "|", StringComparison.Ordinal));
                return;
            case double number:
                writer.Append(Number(number));
                return;
            case float single:
                writer.Append(Number(single));
                return;
            case char character:
                writer.Append(Text.Quote(character.ToString()));
                return;
            case IFormattable formattable when value.GetType().IsPrimitive || value is decimal:
                writer.Append(formattable.ToString(null, CultureInfo.InvariantCulture));
                return;
        }

        if (value is IEnumerable sequence)
        {
            writer.Append('[');
            var first = true;
            foreach (var item in sequence)
            {
                if (!first)
                    writer.Append(',');
                first = false;
                WriteInline(writer, item);
            }

            writer.Append(']');
            return;
        }

        var type = value.GetType();
        if (type.IsValueType)
        {
            writer.Append(TypeName(type)).Append('(');
            var members = MembersOf(type);
            for (var i = 0; i < members.Length; i++)
            {
                if (i > 0)
                    writer.Append(',');
                writer.Append(members[i].Label).Append('=');
                WriteInline(writer, members[i].Read(value));
            }

            writer.Append(')');
            return;
        }

        // A class the dump does not know: its type, never its default ToString or identity.
        writer.Append('<').Append(type.FullName).Append('>');
    }

    private static string Span(FastToken? start, FastToken? end)
    {
        var from = start == null ? "?" : start.Type == TokenTypes.EOF ? "eof" : start.Span.Offset.ToString(CultureInfo.InvariantCulture);
        var to = end == null
            ? "?"
            : end.Type == TokenTypes.EOF
                ? "eof"
                : (end.Span.Offset + end.Span.Length).ToString(CultureInfo.InvariantCulture);
        return from + ".." + to;
    }

    // The end-of-file token is written by name alone: at the corpus commit it is one object shared
    // by every parse (FastScanner.EOF), and nothing of it is read.
    private static string Token(FastToken token)
        => token.Type == TokenTypes.EOF
            ? "EOF"
            : token.Type + "@" + Span(token, token) + ":" + Text.Quote(token.Span.Value ?? string.Empty);

    private static string Number(double value)
    {
        if (value == 0 && double.IsNegative(value))
            return "-0";

        return value.ToString("R", CultureInfo.InvariantCulture);
    }

    private static string TypeName(Type type)
    {
        if (!type.IsGenericType)
            return type.FullName ?? type.Name;

        var name = type.GetGenericTypeDefinition().FullName ?? type.Name;
        var tick = name.IndexOf('`', StringComparison.Ordinal);
        if (tick >= 0)
            name = name[..tick];

        return name + "<" + string.Join(",", type.GetGenericArguments().Select(TypeName)) + ">";
    }

    private static void Indent(StringBuilder writer, int depth) => writer.Append(' ', depth * 2);

    private static Member[] MembersOf(Type type) => Members.GetOrAdd(type, Collect);

    private static Member[] Collect(Type type)
    {
        var chain = new List<Type>();
        for (var current = type; current != null && current != typeof(object) && current != typeof(ValueType); current = current.BaseType)
            chain.Add(current);
        chain.Reverse();

        var members = new List<Member>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        foreach (var declaring in chain)
        {
            var declared = new List<MemberInfo>();
            declared.AddRange(declaring.GetFields(flags).OrderBy(field => field.MetadataToken));
            declared.AddRange(declaring.GetProperties(flags)
                .Where(property => property.GetMethod is { IsPublic: true } && property.GetIndexParameters().Length == 0)
                .OrderBy(property => property.MetadataToken));

            foreach (var member in declared)
            {
                if (declaring == AstNodeType && member.Name is "Type" or "Start" or "End" or "Code")
                    continue;

                // A member that hides an inherited one of the same name (AstFunctionExpression's
                // IsStatement) is written with its declaring type, so the two stay apart.
                var label = seen.Add(member.Name) ? member.Name : declaring.Name + "." + member.Name;
                members.Add(new Member(label, member));
            }
        }

        return members.ToArray();
    }

    private sealed class Member(string label, MemberInfo member)
    {
        internal readonly string Label = label;

        internal object? Read(object owner)
        {
            try
            {
                return member is FieldInfo field ? field.GetValue(owner) : ((PropertyInfo)member).GetValue(owner);
            }
            catch (TargetInvocationException exception) when (exception.InnerException != null)
            {
                return new MemberFailure(exception.InnerException.GetType().FullName ?? "?");
            }
        }
    }

    // A property whose getter threw: written as the exception's type, so the dump still completes
    // and the failure is in its hash.
    private sealed class MemberFailure(string typeName)
    {
        internal readonly string TypeName = typeName;
    }
}
