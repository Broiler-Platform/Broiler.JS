using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Broiler.JavaScript.Architecture.Tests;

/// <summary>
/// A parsed tree read by reflection from the judged build: its dump, and every token it holds.
/// </summary>
/// <remarks>
/// The dump writes each node's type, kind and span, then every instance field of each type from
/// <c>AstNode</c> down to the node's own, in metadata order, so it needs no change when a later item
/// adds a field. Tokens are written by kind, span and position and never followed through their
/// <c>Next</c> and <c>Previous</c> links. Nothing in the text depends on the process: no identity,
/// hash code or culture.
/// </remarks>
internal sealed class TreeText(SubjectParser subject)
{
    private const BindingFlags InstanceFields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    private static readonly string[] NodeHead = ["Type", "Start", "End"];

    private readonly ConcurrentDictionary<Type, FieldInfo[]> fields = new();

    /// <summary>The dump of the tree under <paramref name="root"/>.</summary>
    internal string Dump(object root)
    {
        var text = new StringBuilder();
        Walk(root, text, static _ => { });
        return text.ToString();
    }

    /// <summary>Calls <paramref name="token"/> for every token the tree under <paramref name="root"/> holds.</summary>
    internal void Tokens(object root, Action<object> token) => Walk(root, null, token);

    private void Walk(object root, StringBuilder? text, Action<object> token)
    {
        var ancestors = new HashSet<object>(ReferenceEqualityComparer.Instance);
        Visit(0, "program", root, text, token, ancestors);
    }

    private void Visit(int depth, string label, object? value, StringBuilder? text, Action<object> token, HashSet<object> ancestors)
    {
        text?.Append(depth.ToString(CultureInfo.InvariantCulture)).Append(' ').Append(label).Append(' ');

        switch (value)
        {
            case null:
                text?.Append("null\n");
                return;
            case string s:
                text?.Append(Quote(s)).Append('\n');
                return;
            case Enum e:
                text?.Append(e.ToString()).Append('\n');
                return;
            case bool b:
                text?.Append(b ? "true\n" : "false\n");
                return;
            case char c:
                text?.Append(Quote(c.ToString())).Append('\n');
                return;
            case IFormattable formattable when value.GetType().IsPrimitive:
                text?.Append(formattable.ToString(value is double or float ? "R" : null, CultureInfo.InvariantCulture)).Append('\n');
                return;
        }

        var type = value.GetType();
        if (subject.Token.IsAssignableFrom(type))
        {
            token(value);
            text?.Append("token ").Append(Token(value)).Append('\n');
            return;
        }

        if (type == subject.StringSpan)
        {
            text?.Append("span ").Append(Quote(subject.Read(value, "Value") as string)).Append('\n');
            return;
        }

        if (subject.AstNode.IsAssignableFrom(type))
        {
            if (!ancestors.Add(value))
            {
                text?.Append("cycle ").Append(type.Name).Append('\n');
                return;
            }

            var start = subject.Read(value, "Start");
            var end = subject.Read(value, "End");
            if (start is not null)
                token(start);
            if (end is not null)
                token(end);
            text?.Append("node ").Append(type.Name).Append(' ').Append(subject.Read(value, "Type"))
                .Append(" from ").Append(start is null ? "-" : Token(start))
                .Append(" to ").Append(end is null ? "-" : Token(end)).Append('\n');
            Members(depth, value, skipNodeHead: true, text, token, ancestors);
            ancestors.Remove(value);
            return;
        }

        if (value is IEnumerable sequence)
        {
            var items = sequence.Cast<object?>().ToList();
            text?.Append("list ").Append(items.Count.ToString(CultureInfo.InvariantCulture)).Append('\n');
            for (var i = 0; i < items.Count; i++)
                Visit(depth + 1, "[" + i.ToString(CultureInfo.InvariantCulture) + "]", items[i], text, token, ancestors);
            return;
        }

        if (!ancestors.Add(value))
        {
            text?.Append("cycle ").Append(type.Name).Append('\n');
            return;
        }

        // A value holding nodes somewhere: VariableDeclarator, ObjectProperty, Case, a tuple.
        text?.Append(type.Name).Append('\n');
        Members(depth, value, skipNodeHead: false, text, token, ancestors);
        ancestors.Remove(value);
    }

    private void Members(int depth, object owner, bool skipNodeHead, StringBuilder? text, Action<object> token, HashSet<object> ancestors)
    {
        foreach (var field in FieldsOf(owner.GetType()))
        {
            if (skipNodeHead && field.DeclaringType == subject.AstNode && NodeHead.Contains(field.Name, StringComparer.Ordinal))
                continue;
            Visit(depth + 1, field.Name, field.GetValue(owner), text, token, ancestors);
        }
    }

    private FieldInfo[] FieldsOf(Type type) => fields.GetOrAdd(type, static t =>
    {
        var chain = new List<Type>();
        for (var current = t; current is not null && current != typeof(object) && current != typeof(ValueType); current = current.BaseType)
            chain.Add(current);
        chain.Reverse();
        return [.. chain.SelectMany(static c => c.GetFields(InstanceFields).OrderBy(static f => f.MetadataToken))];
    });

    private string Token(object token)
    {
        var span = subject.Read(token, "Span")!;
        var start = subject.Read(token, "Start")!;
        return subject.Read(token, "Type") + " " + Quote(subject.Read(span, "Value") as string)
            + " @" + Number(subject.Read(span, "Offset")) + "+" + Number(subject.Read(span, "Length"))
            + " " + Number(subject.Read(start, "Line")) + ":" + Number(subject.Read(start, "Column"));
    }

    private static string Number(object? value) => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "-";

    private static string Quote(string? value)
    {
        if (value is null)
            return "null";
        var quoted = new StringBuilder("\"");
        foreach (var c in value)
        {
            if (c is '"' or '\\')
                quoted.Append('\\').Append(c);
            else if (c is >= ' ' and <= '~')
                quoted.Append(c);
            else
                quoted.Append("\\u").Append(((int)c).ToString("X4", CultureInfo.InvariantCulture));
        }

        return quoted.Append('"').ToString();
    }
}
