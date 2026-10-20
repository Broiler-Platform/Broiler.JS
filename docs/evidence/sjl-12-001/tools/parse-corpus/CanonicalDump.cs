using System.Collections;
using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using Broiler.JavaScript.Ast;
using Broiler.JavaScript.Ast.Misc;

namespace ParseCorpus;

/// <summary>
/// The canonical dump of a parsed tree (decision rule part D): each node's type, span, names, flags
/// and children, in tree order, as text whose SHA-256 is what a run reports.
/// </summary>
/// <remarks>
/// <para>
/// The dump is read from the tree by reflection, so it names no node member at compile time and
/// needs no change when the after commit adds or removes one (item 5 adds <c>ContainsContinue</c>
/// to the loop statements, for one). Everything a node holds is in it: every instance field, public
/// or not, and every public readable property, of each type from <c>AstNode</c> down to the node's
/// own, each type's fields and then its properties in declaration (metadata) order. A
/// compiler-generated backing field whose property is already written is left out.
/// <c>AstNode.Type</c>, <c>Start</c> and <c>End</c> make the line's head; <c>AstNode.Code</c>, the
/// source text the span covers, is left out because the span already says it.
/// </para>
/// <para>
/// Names are written as they are, never renumbered: the parser's temporaries at the corpus commit
/// (a process-wide counter) and the pass's <c>d:n</c> temporaries at the after commit (JSD-0080
/// decision D22) both appear verbatim, which is what lets run 2 (the reverse order) show the
/// counter's dependence on parse order at the base. Lists are written in the order they enumerate
/// in, so a list ordered by a hash (control K2) shows.
/// </para>
/// <para>
/// The text grows linearly with the tree: each line starts with its depth as a number, never with
/// indentation, and the text is streamed into the hash (<see cref="DumpWriter"/>) rather than held.
/// The tree is walked with an explicit stack, so its depth costs no thread stack.
/// </para>
/// <para>
/// Nothing in the text depends on the process: no object identity, hash code, address, culture or
/// clock. Numbers use the invariant culture ("R" for doubles), enums their names, type names carry
/// no assembly version, and every character of a value outside printable ASCII is escaped as
/// <c>\uXXXX</c> (UTF-16 code units, so a lone surrogate is kept). The format is set out in
/// README.md beside this file.
/// </para>
/// </remarks>
internal static class CanonicalDump
{
    private static readonly ConcurrentDictionary<Type, Member[]> Members = new();
    private static readonly Type AstNodeType = typeof(AstNode);

    /// <summary>Writes the dump of the tree under <paramref name="root"/>.</summary>
    internal static void Write(DumpWriter writer, AstNode root)
    {
        var ancestors = new HashSet<object>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<Step>();
        pending.Push(new Step(StepKind.Nested, 0, "program", root));

        while (pending.TryPop(out var step))
        {
            switch (step.Kind)
            {
                case StepKind.Leave:
                    ancestors.Remove(step.Value!);
                    break;
                case StepKind.Inline:
                    writer.Head(step.Depth, step.Label).Append(' ');
                    WriteInline(writer, step.Value);
                    writer.Append('\n');
                    break;
                default:
                    Visit(writer, step, ancestors, pending);
                    break;
            }
        }
    }

    /// <summary>What is dumped in place of the prepared tree where the pass throws.</summary>
    internal static void WriteException(DumpWriter writer, Exception exception)
        => writer.Append("throws ").Append(exception.GetType().FullName ?? "?").Append(' ').Append(Text.Quote(exception.Message)).Append('\n');

    private enum StepKind
    {
        Nested,
        Inline,
        Leave,
    }

    private readonly record struct Step(StepKind Kind, int Depth, string Label, object? Value);

    private static void Visit(DumpWriter writer, Step step, HashSet<object> ancestors, Stack<Step> pending)
    {
        var value = step.Value!;
        writer.Head(step.Depth, step.Label);

        if (value is AstNode node)
        {
            if (!ancestors.Add(node))
            {
                writer.Append(" cycle ").Append(TypeName(node.GetType())).Append('\n');
                return;
            }

            writer.Append(' ').Append(TypeName(node.GetType()))
                .Append(' ').Append(node.Type.ToString())
                .Append(" @").Append(Span(node.Start, node.End));

            // Popped after every nested member of the node has been written.
            pending.Push(new Step(StepKind.Leave, step.Depth, step.Label, node));
            WriteMembers(writer, step.Depth, MembersOf(node.GetType()), node, pending);
            return;
        }

        if (value is IEnumerable sequence && value is not string)
        {
            var items = new List<object?>();
            foreach (var item in sequence)
                items.Add(item);

            writer.Append(" list ").Append(items.Count).Append('\n');
            for (var i = items.Count - 1; i >= 0; i--)
            {
                var kind = IsInline(items[i]) ? StepKind.Inline : StepKind.Nested;
                pending.Push(new Step(kind, step.Depth + 1, "[" + i.ToString(CultureInfo.InvariantCulture) + "]", items[i]));
            }

            return;
        }

        // A value type that holds a node somewhere: VariableDeclarator, ObjectProperty, Case, a
        // tuple of a name and a literal.
        writer.Append(' ').Append(TypeName(value.GetType()));
        WriteMembers(writer, step.Depth, MembersOf(value.GetType()), value, pending);
    }

    // The inline members on the owner's line, then the nested ones, each on its own lines one
    // level deeper, in member order (pushed in reverse, so the first is popped first).
    private static void WriteMembers(DumpWriter writer, int depth, Member[] members, object owner, Stack<Step> pending)
    {
        var values = new object?[members.Length];
        var inline = new bool[members.Length];
        for (var i = 0; i < members.Length; i++)
            values[i] = members[i].Read(owner);

        for (var i = 0; i < members.Length; i++)
        {
            inline[i] = IsInline(values[i]);
            if (!inline[i])
                continue;

            writer.Append(' ').Append(members[i].Label).Append('=');
            WriteInline(writer, values[i]);
        }

        writer.Append('\n');

        for (var i = members.Length - 1; i >= 0; i--)
        {
            if (!inline[i])
                pending.Push(new Step(StepKind.Nested, depth + 1, members[i].Label, values[i]));
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

    private static void WriteInline(DumpWriter writer, object? value)
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
        writer.Append('<').Append(TypeName(type)).Append('>');
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

    // A type's full name without any assembly name or version: generic arguments are written
    // by the same rule, inside <...>.
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

    private static Member[] MembersOf(Type type) => Members.GetOrAdd(type, Collect);

    private static Member[] Collect(Type type)
    {
        var chain = new List<Type>();
        for (var current = type; current != null && current != typeof(object) && current != typeof(ValueType); current = current.BaseType)
            chain.Add(current);
        chain.Reverse();

        var members = new List<Member>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        const BindingFlags publicMembers = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        const BindingFlags allFields = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        foreach (var declaring in chain)
        {
            var properties = declaring.GetProperties(publicMembers)
                .Where(property => property.GetMethod is { IsPublic: true } && property.GetIndexParameters().Length == 0)
                .OrderBy(property => property.MetadataToken)
                .ToList();
            var written = properties.Select(property => property.Name).ToHashSet(StringComparer.Ordinal);

            // The product's own types give up their non-public fields too, so a flag or name the
            // after commit keeps in an internal or private member is still in the dump. A type of
            // the runtime's (a tuple, say) gives its public members alone.
            var fields = declaring.GetFields(IsProductType(declaring) ? allFields : publicMembers)
                .Where(field => BackingFieldOf(field) is not { } property || !written.Contains(property))
                .OrderBy(field => field.MetadataToken);

            var declared = new List<(string Name, MemberInfo Member)>();
            declared.AddRange(fields.Select(field => (BackingFieldOf(field) ?? field.Name, (MemberInfo)field)));
            declared.AddRange(properties.Select(property => (property.Name, (MemberInfo)property)));

            foreach (var (name, member) in declared)
            {
                if (declaring == AstNodeType && name is "Type" or "Start" or "End" or "Code")
                    continue;

                // A member that hides an inherited one of the same name (AstFunctionExpression's
                // IsStatement) is written with its declaring type, so the two stay apart.
                var label = seen.Add(name) ? name : declaring.Name + "." + name;
                members.Add(new Member(label, member));
            }
        }

        return members.ToArray();
    }

    private static bool IsProductType(Type type)
        => type.Assembly.GetName().Name?.StartsWith("Broiler.", StringComparison.Ordinal) == true;

    // The property an auto-property's compiler-generated backing field belongs to ("<Name>k__BackingField").
    private static string? BackingFieldOf(FieldInfo field)
    {
        const string suffix = ">k__BackingField";
        return field.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false)
            && field.Name.StartsWith('<')
            && field.Name.EndsWith(suffix, StringComparison.Ordinal)
                ? field.Name[1..^suffix.Length]
                : null;
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
