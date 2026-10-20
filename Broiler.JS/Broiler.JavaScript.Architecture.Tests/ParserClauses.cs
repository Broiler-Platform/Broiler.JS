using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using Broiler.ECMAScript.Architecture.Tests;

namespace Broiler.JavaScript.Architecture.Tests;

/// <summary>What the metadata clauses found in one assembly.</summary>
/// <param name="Assembly">The assembly's name.</param>
/// <param name="Violations">Every violation, in ordinal order.</param>
/// <param name="AcceptedReadOnlyStatics">Every InitOnly static field the clauses read and accepted (non-vacuity).</param>
/// <param name="StaticFieldsRead">How many static fields outside compiler-generated types the clauses read.</param>
internal sealed record ClauseReading(string Assembly, IReadOnlyList<string> Violations, IReadOnlyList<string> AcceptedReadOnlyStatics, int StaticFieldsRead);

/// <summary>
/// The clauses of part B of bundle sjl-12-001's rule that read an assembly's metadata. Each is a
/// function from the image to the violations it finds; nothing here loads what it judges.
/// </summary>
/// <remarks>
/// The clauses, as the rule states them: no AssemblyRef to Runtime, Engine, Compiler or BuiltIns; no
/// TypeRef to <c>AsyncLocal`1</c> or <c>ThreadLocal`1</c>, and no <c>ThreadStaticAttribute</c>;
/// outside compiler-generated types, no static field that is neither InitOnly nor Literal, no static
/// field of an array type or of a mutable collection type (rule SL2's list), no settable static
/// property, and no InitOnly static field whose type declares a mutable instance field, except
/// primitives, enums, string, readonly structs and <c>FastKeywordMap</c>; and no TypeRef to the
/// three desugaring walkers. A static field here includes a property's compiler-generated backing
/// field, which <see cref="AssemblyFacts"/> leaves out, because a get-only static property is
/// exactly an InitOnly static field (<c>FastKeywordMap.Instance</c> is one, which is why the rule
/// excepts its type).
/// <para>
/// The last clause reads a field's declared type, as the rule words it, and not the object the field
/// holds. So an InitOnly static field declared as <c>object</c>, as an interface, or as a type that
/// with its base types declares no mutable instance field is accepted whatever it holds:
/// <c>static readonly IList&lt;int&gt; X = new List&lt;int&gt;()</c> passes every clause, because rule
/// SL2's list is matched against the declared type's name too. The test does not guard such a field;
/// widening the clause would change part B's evidence, so the gap is stated here instead.
/// </para>
/// </remarks>
internal static class ParserClauses
{
    /// <summary>The engine assemblies the parser may not reference.</summary>
    internal static readonly string[] ForbiddenAssemblies =
    [
        "Broiler.JavaScript.Runtime", "Broiler.JavaScript.Engine", "Broiler.JavaScript.Compiler", "Broiler.JavaScript.BuiltIns",
    ];

    /// <summary>The ambient-state types the parser may not name.</summary>
    internal static readonly string[] AmbientTypes = ["System.Threading.AsyncLocal`1", "System.Threading.ThreadLocal`1"];

    internal const string ThreadStaticAttribute = "System.ThreadStaticAttribute";

    /// <summary>The walkers the parser's for-loop desugaring uses, by simple name, in whatever namespace.</summary>
    internal static readonly string[] DesugaringWalkers = ["AstIdentifierReplacer", "IdentifierNameCollector", "ClosureDetector"];

    /// <summary>
    /// Rule SL2's list of types whose instances can be changed after construction
    /// (<c>Broiler.ECMAScript.Architecture.Tests/SharedRules.cs</c>, <c>MutableTypes</c>), copied
    /// because that list is private; <see cref="ParserArchitectureTests"/> checks that the copy is
    /// the list.
    /// </summary>
    internal static readonly string[] MutableTypes =
    [
        "System.Collections.Generic.List`1", "System.Collections.Generic.Dictionary`2", "System.Collections.Generic.HashSet`1",
        "System.Collections.Generic.SortedSet`1", "System.Collections.Generic.SortedDictionary`2", "System.Collections.Generic.SortedList`2",
        "System.Collections.Generic.Queue`1", "System.Collections.Generic.Stack`1", "System.Collections.Generic.LinkedList`1",
        "System.Collections.Generic.PriorityQueue`2", "System.Collections.Concurrent.ConcurrentDictionary`2",
        "System.Collections.Concurrent.ConcurrentBag`1", "System.Collections.Concurrent.ConcurrentQueue`1",
        "System.Collections.Concurrent.ConcurrentStack`1", "System.Collections.ArrayList", "System.Collections.Hashtable",
        "System.Text.StringBuilder", "System.Random",
    ];

    /// <summary>The class the rule accepts as the type of an InitOnly static field by name.</summary>
    internal const string KeywordMap = "Broiler.JavaScript.Parser.FastKeywordMap";

    /// <summary>
    /// Reads <paramref name="image"/>. The types its static fields name are read from the assemblies
    /// in <paramref name="directory"/> (the engine's, beside it) and from the platform's.
    /// </summary>
    internal static ClauseReading Read(byte[] image, string directory)
    {
        var facts = AssemblyFacts.Read(image);
        var name = facts.Name;
        var violations = new List<string>();

        foreach (var reference in facts.AssemblyReferences.Where(static r => ForbiddenAssemblies.Contains(r, StringComparer.Ordinal)))
            violations.Add(name + " references the assembly " + reference);

        foreach (var type in facts.TypeReferences)
        {
            if (AmbientTypes.Contains(type, StringComparer.Ordinal))
                violations.Add(name + " names " + type);
            if (DesugaringWalkers.Contains(SimpleName(type), StringComparer.Ordinal))
                violations.Add(name + " names the desugaring walker " + type);
        }

        foreach (var property in facts.SettableStaticProperties)
            violations.Add(name + ": " + property + " is a settable static property");

        using var universe = new TypeUniverse(image, directory);
        var self = universe.Self;
        var metadata = self.Metadata;
        var provider = new ShapeProvider(self);
        var accepted = new List<string>();
        var read = 0;
        var threadStatic = false;

        foreach (var typeHandle in metadata.TypeDefinitions)
        {
            var type = metadata.GetTypeDefinition(typeHandle);
            var typeName = SignatureNames.Qualified(metadata, typeHandle);
            var generated = AssemblyFacts.IsCompilerGenerated(typeName);

            foreach (var fieldHandle in type.GetFields())
            {
                var field = metadata.GetFieldDefinition(fieldHandle);
                if ((field.Attributes & FieldAttributes.Static) == 0)
                    continue;

                var where = name + ": " + typeName + "." + metadata.GetString(field.Name);

                // [ThreadStatic] is ambient state wherever it is, compiler-generated or not.
                if (field.GetCustomAttributes().Any(a => TypeUniverse.AttributeType(metadata, metadata.GetCustomAttribute(a)) == ThreadStaticAttribute))
                {
                    threadStatic = true;
                    violations.Add(where + " is [ThreadStatic]");
                }

                if (generated)
                    continue;

                read++;
                if ((field.Attributes & FieldAttributes.Literal) != 0)
                    continue;

                if ((field.Attributes & FieldAttributes.InitOnly) == 0)
                {
                    violations.Add(where + " is a mutable static field");
                    continue;
                }

                var shape = field.DecodeSignature(provider, null);
                if (Judge(universe, shape) is { } refusal)
                    violations.Add(where + " " + refusal);
                else
                    accepted.Add(typeName + "." + metadata.GetString(field.Name) + " (" + shape.Name + ")");
            }
        }

        if (!threadStatic && facts.CustomAttributeTypes.Contains(ThreadStaticAttribute))
            violations.Add(name + " applies " + ThreadStaticAttribute);

        violations.Sort(StringComparer.Ordinal);
        return new ClauseReading(name, violations, accepted, read);
    }

    /// <summary>Why an InitOnly static field of type <paramref name="shape"/> is refused, or null when it is accepted.</summary>
    private static string? Judge(TypeUniverse universe, TypeShape shape)
    {
        if (shape.Kind == ShapeKind.Array)
            return "is a static array, whose elements can be changed";

        if (MutableTypes.Any(t => shape.Name == t || shape.Name.StartsWith(t + "<", StringComparison.Ordinal)))
            return "is a static " + shape.Name + ", which can be changed after construction";

        if (shape.Kind is ShapeKind.Primitive or ShapeKind.String or ShapeKind.Object)
            return null;

        var definition = shape.Kind == ShapeKind.Generic ? shape.Definition! : shape;
        if (definition.Kind != ShapeKind.Named)
            return "is a static readonly " + shape.Name + ", whose type the test cannot read";

        if (definition.Name == KeywordMap)
            return null;

        var description = universe.Describe(definition.Module!, definition.Handle);
        if (description is null)
            return "is a static readonly " + shape.Name + ", whose type the test cannot read";
        if (description.IsEnum || (description.IsValueType && description.IsReadOnly))
            return null;
        if (description.MutableFields.Count == 0)
            return null;

        return "is a static readonly " + shape.Name + ", whose type declares the mutable instance field"
            + (description.MutableFields.Count == 1 ? " " : "s ") + string.Join(", ", description.MutableFields);
    }

    private static string SimpleName(string qualified)
    {
        var cut = Math.Max(qualified.LastIndexOf('.'), qualified.LastIndexOf('+'));
        return cut < 0 ? qualified : qualified[(cut + 1)..];
    }
}
