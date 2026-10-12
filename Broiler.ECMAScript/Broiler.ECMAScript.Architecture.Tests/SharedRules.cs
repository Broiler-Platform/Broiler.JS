using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Xml.Linq;

namespace Broiler.ECMAScript.Architecture.Tests;

/// <summary>
/// The rules of group SL (governance section 3.2), each a function from what it reads to the
/// violations it finds. An empty answer is a pass; the tests also assert that each rule read a
/// real subject, because a rule that passes by finding nothing passes most easily over nothing.
/// </summary>
internal static class SharedRules
{
    // ---------------------------------------------------------------------------------------------
    // SL1: references, in the layering direction only.

    /// <summary>
    /// The layering of architecture section 1, for the libraries that exist: each library and the
    /// shared libraries and component packages it may reference. A library that is not here has
    /// no place in the layering yet, which is a violation rather than a pass.
    /// </summary>
    internal static readonly FrozenDictionary<string, string[]> Layering = new Dictionary<string, string[]>
    {
        // Runtime-side, data-free, references the base class library only.
        ["Broiler.ECMAScript.Algorithms"] = [],

        // Test-only; reads the corpus its caller hands it.
        ["Broiler.ECMAScript.Conformance"] = [],
    }.ToFrozenDictionary(StringComparer.Ordinal);

    /// <summary>The component packages a shared library may reference at all, where the layering lets it.</summary>
    internal static readonly string[] ComponentPackagePrefixes = ["Broiler.Unicode.", "Broiler.Regex"];

    /// <summary>Packages the SDK adds to every trimmable project on its own; they flow to no consumer.</summary>
    internal static readonly FrozenSet<string> ImplicitPackages = FrozenSet.Create(StringComparer.Ordinal, "Microsoft.NET.ILLink.Tasks");

    /// <summary>SL1 over a project file: every project and package reference against the layering.</summary>
    internal static IEnumerable<string> SL1(string projectName, string projectDirectory, XDocument project)
    {
        if (!Layering.TryGetValue(projectName, out var allowed))
        {
            yield return projectName + " has no place in the layering; add it to SharedRules.Layering with the edges architecture section 1 gives it";
            yield break;
        }

        foreach (var include in project.Descendants("ProjectReference").Select(static e => (string?)e.Attribute("Include")).OfType<string>())
        {
            var target = Path.GetFullPath(Path.Combine(projectDirectory, include.Replace('\\', Path.DirectorySeparatorChar)));
            var name = Path.GetFileNameWithoutExtension(target);
            var inFolder = target.StartsWith(SharedTree.Folder + Path.DirectorySeparatorChar, StringComparison.Ordinal);
            if (!inFolder)
                yield return projectName + " references the project " + include + ", outside the shared folder: an engine, Broiler.VM or anything else";
            else if (!allowed.Contains(name, StringComparer.Ordinal))
                yield return projectName + " references " + name + ", against the layering";
        }

        foreach (var include in project.Descendants("PackageReference").Select(static e => (string?)e.Attribute("Include")).OfType<string>())
        {
            if (!IsAllowedPackage(include, allowed))
                yield return projectName + " references the package " + include + ", outside the shared allow-list or against the layering";
        }
    }

    /// <summary>SL1 over a restore: every package the assets file resolved, direct or transitive.</summary>
    internal static IEnumerable<string> SL1Restore(string projectName, JsonDocument assets)
    {
        var allowed = Layering.TryGetValue(projectName, out var edges) ? edges : [];
        foreach (var library in assets.RootElement.GetProperty("libraries").EnumerateObject())
        {
            if (library.Value.GetProperty("type").GetString() != "package")
                continue;
            var id = library.Name[..library.Name.IndexOf('/', StringComparison.Ordinal)];
            if (!ImplicitPackages.Contains(id) && !IsAllowedPackage(id, allowed))
                yield return projectName + " restores the package " + library.Name + ", outside the shared allow-list or against the layering";
        }
    }

    /// <summary>SL1 over a built assembly: every assembly it references is the framework's or a shared library it may reference.</summary>
    internal static IEnumerable<string> SL1Assembly(AssemblyFacts assembly)
    {
        var allowed = Layering.TryGetValue(assembly.Name, out var edges) ? edges : [];
        return assembly.AssemblyReferences
            .Where(reference => !IsFramework(reference) && !allowed.Contains(reference, StringComparer.Ordinal)
                && !IsAllowedPackage(reference, allowed))
            .Select(reference => assembly.Name + " references the assembly " + reference);
    }

    private static bool IsAllowedPackage(string id, string[] allowed) =>
        allowed.Contains(id, StringComparer.Ordinal)
        && ComponentPackagePrefixes.Any(prefix => id.StartsWith(prefix, StringComparison.Ordinal) || id == prefix.TrimEnd('.'));

    private static bool IsFramework(string assemblyName) =>
        assemblyName is "System" or "netstandard" or "mscorlib" || assemblyName.StartsWith("System.", StringComparison.Ordinal);

    // ---------------------------------------------------------------------------------------------
    // SL2: no mutable static state.

    /// <summary>Types whose instances can be changed after construction, so that a static readonly field of one is still mutable state.</summary>
    private static readonly string[] MutableTypes =
    [
        "System.Collections.Generic.List`1", "System.Collections.Generic.Dictionary`2", "System.Collections.Generic.HashSet`1",
        "System.Collections.Generic.SortedSet`1", "System.Collections.Generic.SortedDictionary`2", "System.Collections.Generic.SortedList`2",
        "System.Collections.Generic.Queue`1", "System.Collections.Generic.Stack`1", "System.Collections.Generic.LinkedList`1",
        "System.Collections.Generic.PriorityQueue`2", "System.Collections.Concurrent.ConcurrentDictionary`2",
        "System.Collections.Concurrent.ConcurrentBag`1", "System.Collections.Concurrent.ConcurrentQueue`1",
        "System.Collections.Concurrent.ConcurrentStack`1", "System.Collections.ArrayList", "System.Collections.Hashtable",
        "System.Text.StringBuilder", "System.Random",
    ];

    internal static IEnumerable<string> SL2(AssemblyFacts assembly)
    {
        foreach (var field in assembly.StaticFields)
        {
            var where = assembly.Name + ": " + field.Type + "." + field.Name;
            if (field.IsLiteral)
                continue;
            if (!field.IsReadOnly)
                yield return where + " is a mutable static field";
            else if (field.FieldType.EndsWith(']'))
                yield return where + " is a static array, whose elements can be changed: use a constant span or an immutable collection";
            else if (MutableTypes.Any(t => field.FieldType == t || field.FieldType.StartsWith(t + "<", StringComparison.Ordinal)))
                yield return where + " is a static " + field.FieldType + ", which can be changed after construction";
        }

        foreach (var property in assembly.SettableStaticProperties)
            yield return assembly.Name + ": " + property + " is a settable static property";

        if (assembly.CustomAttributeTypes.Contains("System.ThreadStaticAttribute"))
            yield return assembly.Name + " declares a [ThreadStatic] field";

        foreach (var type in assembly.TypeReferences.Where(static t => t is "System.Threading.ThreadLocal`1" or "System.Threading.AsyncLocal`1"))
            yield return assembly.Name + " names " + type;
    }

    // ---------------------------------------------------------------------------------------------
    // SL3: no dynamic code, no hidden machinery, no I/O, no environment.

    /// <summary>Broiler.VM's B5 list, by exact member name, and convention 8's additions.</summary>
    private static readonly string[] DynamicMembers =
    [
        "System.Reflection.Assembly.Load", "System.Reflection.Assembly.LoadFrom", "System.Reflection.Assembly.LoadFile",
        "System.Reflection.Assembly.UnsafeLoadFrom", "System.Reflection.Assembly.GetTypes", "System.Reflection.Assembly.GetType",
        "System.Type.GetType", "System.Activator.CreateInstance", "System.Activator.CreateInstanceFrom",
        "System.Reflection.MethodBase.Invoke", "System.Linq.Expressions.LambdaExpression.Compile",
        "System.Runtime.InteropServices.NativeLibrary.Load", "System.Runtime.InteropServices.NativeLibrary.TryLoad",
        "System.Runtime.InteropServices.NativeLibrary.GetExport", "System.Runtime.InteropServices.NativeLibrary.TryGetExport",
        "System.Runtime.InteropServices.Marshal.GetDelegateForFunctionPointer",
        "System.Runtime.InteropServices.Marshal.GetFunctionPointerForDelegate",
        "System.Runtime.CompilerServices.RuntimeHelpers.PrepareMethod",

        // Convention 8: generic instantiation over a run-time Type, and the Type-taking enum readers.
        "System.Type.MakeGenericType", "System.Type.MakeArrayType", "System.Reflection.MethodInfo.MakeGenericMethod",
        "System.Enum.GetValues", "System.Enum.GetNames", "System.Enum.GetValuesAsUnderlyingType",
    ];

    /// <summary>Namespaces and types where naming the thing at all is the violation.</summary>
    private static readonly string[] HiddenMachineryTypePrefixes =
    [
        "System.Reflection.Emit.", "System.Runtime.Loader.", "Microsoft.CSharp.RuntimeBinder.",
        "System.Runtime.CompilerServices.CallSite", "System.Dynamic.",

        // Reflection-based serialization.
        "System.Text.Json.JsonSerializer", "System.Xml.Serialization.", "System.Runtime.Serialization.",

        // File, network, process and environment.
        "System.IO.File", "System.IO.Directory", "System.IO.FileInfo", "System.IO.DirectoryInfo", "System.IO.FileSystemInfo",
        "System.IO.FileStream", "System.IO.DriveInfo", "System.IO.StreamReader", "System.IO.StreamWriter",
        "System.IO.FileSystemWatcher", "System.IO.Path", "System.IO.Pipes.", "System.IO.MemoryMappedFiles.",
        "System.IO.IsolatedStorage.", "System.Net.", "System.Console", "System.Diagnostics.Process",
        "System.AppContext", "Microsoft.Win32.",
    ];

    /// <summary>
    /// The one member of <c>System.Environment</c> that reads nothing of the environment: the
    /// compiler's iterator and async machinery asks it which thread it is on. Every other member
    /// of the type is an environment read and is refused by member, not by type, for that reason.
    /// </summary>
    private const string CompilerThreadIdentity = "System.Environment.get_CurrentManagedThreadId()";

    /// <summary>
    /// SL3 over a built assembly. <paramref name="allowIo"/> is the probe's: an executable reads
    /// the corpus from disk, and every other clause still holds it.
    /// </summary>
    internal static IEnumerable<string> SL3(AssemblyFacts assembly, bool allowIo = false)
    {
        foreach (var member in assembly.MemberReferences)
        {
            var name = member.Split('(')[0];
            if (DynamicMembers.Contains(name, StringComparer.Ordinal))
            {
                // Enum.GetValues<T>() and its siblings are the generic, AOT-safe forms; only the Type-taking overloads are refused.
                if (name.StartsWith("System.Enum.", StringComparison.Ordinal) && !member.Contains("System.Type", StringComparison.Ordinal))
                    continue;
                yield return assembly.Name + " calls " + member;
            }
            else if (name.StartsWith("System.Environment.", StringComparison.Ordinal) && member != CompilerThreadIdentity && !allowIo)
            {
                yield return assembly.Name + " calls " + member + ": an environment read";
            }
        }

        foreach (var type in assembly.TypeReferences)
        {
            if (allowIo && IsIoType(type))
                continue;
            if (HiddenMachineryTypePrefixes.Any(prefix => type == prefix || type.StartsWith(prefix, StringComparison.Ordinal) && !IsException(type)))
                yield return assembly.Name + " names " + type;
            else if (type.StartsWith("System.Reflection.", StringComparison.Ordinal) && !type.EndsWith("Attribute", StringComparison.Ordinal))
                yield return assembly.Name + " names " + type + ": reflection";
        }

        if (assembly.AssemblyReferences.Contains("Microsoft.CSharp"))
            yield return assembly.Name + " references Microsoft.CSharp: dynamic";

        if (assembly.CustomAttributeTypes.Contains("System.Runtime.CompilerServices.ModuleInitializerAttribute"))
            yield return assembly.Name + " applies a module initializer";

        foreach (var invoke in assembly.PlatformInvokes)
            yield return assembly.Name + " declares the platform invoke " + invoke;
    }

    private static bool IsException(string type) => type.EndsWith("Exception", StringComparison.Ordinal);

    private static bool IsIoType(string type) =>
        type.StartsWith("System.IO.", StringComparison.Ordinal) || type is "System.Console" or "System.Environment" or "System.AppContext";

    // ---------------------------------------------------------------------------------------------
    // SL4: no platform globalization, no regular expressions, no culture.

    /// <summary>Types whose answers come from the platform's culture, time zone or Unicode data, or from a clock.</summary>
    private static readonly string[] PlatformTypePrefixes =
    [
        "System.Text.RegularExpressions.", "System.TimeZoneInfo", "System.Text.Rune", "System.Diagnostics.Stopwatch",
        "System.Globalization.StringNormalizationExtensions",
    ];

    /// <summary>Members that read a culture, the platform's Unicode tables or a clock.</summary>
    private static readonly string[] PlatformMembers =
    [
        "System.String.Normalize", "System.String.IsNormalized",
        "System.String.ToUpper", "System.String.ToLower", "System.String.ToUpperInvariant", "System.String.ToLowerInvariant",
        "System.Char.ToUpper", "System.Char.ToLower", "System.Char.ToUpperInvariant", "System.Char.ToLowerInvariant",
        "System.Char.GetUnicodeCategory", "System.Char.GetNumericValue",
        "System.Char.IsLetter", "System.Char.IsDigit", "System.Char.IsLetterOrDigit", "System.Char.IsNumber", "System.Char.IsUpper",
        "System.Char.IsLower", "System.Char.IsWhiteSpace", "System.Char.IsPunctuation", "System.Char.IsSymbol", "System.Char.IsSeparator",
        "System.Char.IsControl",
        "System.DateTime.get_Now", "System.DateTime.get_UtcNow", "System.DateTime.get_Today",
        "System.DateTimeOffset.get_Now", "System.DateTimeOffset.get_UtcNow",
        "System.String.Compare", "System.String.CompareTo",
    ];

    /// <summary>String members whose overload without a <c>StringComparison</c> compares by the current culture.</summary>
    private static readonly string[] CultureDefaultStringMembers =
    [
        "System.String.StartsWith(System.String", "System.String.EndsWith(System.String", "System.String.IndexOf(System.String",
        "System.String.LastIndexOf(System.String",
    ];

    private static readonly string[] NumericTypes =
    [
        "System.Byte", "System.SByte", "System.Int16", "System.UInt16", "System.Int32", "System.UInt32", "System.Int64", "System.UInt64",
        "System.Int128", "System.UInt128", "System.IntPtr", "System.UIntPtr", "System.Single", "System.Double", "System.Half",
        "System.Decimal", "System.Numerics.BigInteger",
    ];

    internal static IEnumerable<string> SL4(AssemblyFacts assembly)
    {
        foreach (var type in assembly.TypeReferences)
        {
            if (PlatformTypePrefixes.Any(prefix => type.StartsWith(prefix, StringComparison.Ordinal)))
                yield return assembly.Name + " names " + type;
            else if (type.StartsWith("System.Globalization.", StringComparison.Ordinal)
                && type is not "System.Globalization.NumberFormatInfo" and not "System.Globalization.NumberStyles")
                yield return assembly.Name + " names " + type + ": platform globalization";
        }

        foreach (var member in assembly.MemberReferences)
        {
            var name = member.Split('(')[0];
            var parameters = member[(member.IndexOf('(', StringComparison.Ordinal) + 1)..^1];
            var hasProvider = parameters.Contains("System.IFormatProvider", StringComparison.Ordinal);

            if (PlatformMembers.Contains(name, StringComparer.Ordinal))
            {
                // char.Is*(string, int) and the Comparison-taking Compare overloads are as culture-free as the char ones are not.
                if (name is "System.String.Compare" && parameters.Contains("System.StringComparison", StringComparison.Ordinal))
                    continue;
                yield return assembly.Name + " calls " + member;
            }
            else if (CultureDefaultStringMembers.Any(prefix => member.StartsWith(prefix, StringComparison.Ordinal))
                && !parameters.Contains("System.StringComparison", StringComparison.Ordinal))
            {
                yield return assembly.Name + " calls " + member + ", which compares by the current culture";
            }
            else if (name.StartsWith("System.Globalization.NumberFormatInfo.", StringComparison.Ordinal)
                && name != "System.Globalization.NumberFormatInfo.get_InvariantInfo")
            {
                yield return assembly.Name + " calls " + member + ": only NumberFormatInfo.InvariantInfo is culture-free";
            }
            else if (NumericTypes.Any(t => name.StartsWith(t + ".", StringComparison.Ordinal))
                && name.EndsWith("Parse", StringComparison.Ordinal) && !hasProvider)
            {
                yield return assembly.Name + " calls " + member + ", which parses by the current culture";
            }
            else if (NumericTypes.Any(t => name == t + ".ToString" || name == t + ".TryFormat") && parameters.Length > 0 && !hasProvider)
            {
                yield return assembly.Name + " calls " + member + ", which formats by the current culture";
            }
            else if (name.StartsWith("System.Convert.", StringComparison.Ordinal) && !name.Contains("Base64", StringComparison.Ordinal)
                && !name.Contains("Hex", StringComparison.Ordinal)
                && (name == "System.Convert.ToString" || parameters.Contains("System.String", StringComparison.Ordinal)) && !hasProvider)
            {
                yield return assembly.Name + " calls " + member + ", which converts by the current culture";
            }
            else if (name == "System.String.Format" && !parameters.StartsWith("System.IFormatProvider", StringComparison.Ordinal))
            {
                yield return assembly.Name + " calls " + member + ", which formats by the current culture";
            }
            else if (name == "System.Text.StringBuilder.Append" && NumericTypes.Contains(parameters, StringComparer.Ordinal))
            {
                yield return assembly.Name + " calls " + member + ", which formats by the current culture";
            }
            else if (member is "System.Runtime.CompilerServices.DefaultInterpolatedStringHandler..ctor(System.Int32,System.Int32)"
                || member == "System.Text.StringBuilder+AppendInterpolatedStringHandler..ctor(System.Int32,System.Int32,System.Text.StringBuilder)")
            {
                yield return assembly.Name + " interpolates a string without a format provider, which formats its values by the current culture";
            }
        }
    }

    // ---------------------------------------------------------------------------------------------
    // SL5: Native AOT and trimming, nothing suppressed.

    /// <summary>The evaluated properties every shared library must have, and the values.</summary>
    internal static readonly (string Property, string Value)[] LibraryProperties =
    [
        ("IsAotCompatible", "true"), ("IsTrimmable", "true"), ("EnableTrimAnalyzer", "true"), ("EnableAotAnalyzer", "true"),
        ("EnableSingleFileAnalyzer", "true"), ("TreatWarningsAsErrors", "true"), ("TargetFramework", "net10.0"),
        ("Nullable", "enable"), ("ImplicitUsings", "disable"), ("Deterministic", "true"), ("AllowUnsafeBlocks", "false"),
    ];

    /// <summary>Broiler.VM's root settings, which the probe publishes with (convention 12).</summary>
    internal static readonly (string Property, string Value)[] ProbeProperties =
    [
        ("PublishAot", "true"), ("TrimMode", "full"), ("InvariantGlobalization", "true"), ("UseSystemResourceKeys", "true"),
        ("TreatWarningsAsErrors", "true"), ("IsAotCompatible", "true"), ("TargetFramework", "net10.0"),
    ];

    /// <summary>The compiler's own informational warnings the SDK puts in NoWarn for every project; they are not trim or AOT warnings.</summary>
    /// <remarks>
    /// IL2121 is in the same position for a project the SDK trims: Microsoft.NET.ILLink.targets adds
    /// it to every such project. It reports an <c>UnconditionalSuppressMessage</c> that suppresses
    /// nothing, and SL5 refuses every such attribute, so it has nothing left to report.
    /// </remarks>
    internal static readonly FrozenSet<string> SdkNoWarn = FrozenSet.Create(StringComparer.Ordinal, "1701", "1702", "IL2121");

    internal static IEnumerable<string> SL5Properties(string project, IReadOnlyDictionary<string, string> evaluated, (string Property, string Value)[] required)
    {
        foreach (var (property, value) in required)
        {
            evaluated.TryGetValue(property, out var actual);
            if (!string.Equals(actual, value, StringComparison.OrdinalIgnoreCase))
                yield return project + ": " + property + " is '" + actual + "', not '" + value + "'";
        }

        foreach (var code in Codes(evaluated, "NoWarn").Where(static c => !SdkNoWarn.Contains(c)))
            yield return project + ": NoWarn suppresses " + code;
        foreach (var code in Codes(evaluated, "WarningsNotAsErrors"))
            yield return project + ": WarningsNotAsErrors exempts " + code;
        if (evaluated.TryGetValue("SuppressTrimAnalysisWarnings", out var suppress) && suppress.Equals("true", StringComparison.OrdinalIgnoreCase))
            yield return project + ": SuppressTrimAnalysisWarnings is true";
    }

    private static IEnumerable<string> Codes(IReadOnlyDictionary<string, string> evaluated, string property) =>
        evaluated.TryGetValue(property, out var value)
            ? value.Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : [];

    /// <summary>SL5 over a built assembly: a suppression the compiler keeps in metadata.</summary>
    internal static IEnumerable<string> SL5Assembly(AssemblyFacts assembly) =>
        assembly.CustomAttributeTypes
            .Where(static a => a is "System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessageAttribute")
            .Select(a => assembly.Name + " applies " + a);

    /// <summary>
    /// The inventory half, in the shape of Broiler.VM's N36: every suppression in a source or
    /// project file of the shared folder, by file. Text, so a suppression the compiler drops
    /// (<c>SuppressMessage</c> is conditional) is found too.
    /// </summary>
    internal static IEnumerable<string> Suppressions(string relativePath, string text)
    {
        var markers = relativePath.EndsWith(".cs", StringComparison.Ordinal)
            ? new[] { "#pragma warning disable", "SuppressMessage", "UnconditionalSuppressMessage" }
            : new[] { "<NoWarn>", "<WarningsNotAsErrors>", "<SuppressTrimAnalysisWarnings>" };
        foreach (var marker in markers)
        {
            for (var at = text.IndexOf(marker, StringComparison.Ordinal); at >= 0; at = text.IndexOf(marker, at + 1, StringComparison.Ordinal))
            {
                // An element that sets the property empty resets it, which is what the shared folder's props does.
                if (marker[0] == '<' && text.AsSpan(at + marker.Length).StartsWith("</", StringComparison.Ordinal))
                    continue;
                yield return relativePath + ": " + marker.Trim('<', '>');
            }
        }
    }

    // ---------------------------------------------------------------------------------------------
    // SL6: the public API baseline, both directions.

    internal static IEnumerable<string> SL6(string assembly, IReadOnlyList<string> listing, IReadOnlyList<string> baseline)
    {
        var current = listing.ToHashSet(StringComparer.Ordinal);
        var recorded = baseline.ToHashSet(StringComparer.Ordinal);
        foreach (var line in listing.Where(l => !recorded.Contains(l)))
            yield return assembly + " has public API the baseline does not record: " + line;
        foreach (var line in baseline.Where(l => !current.Contains(l)))
            yield return assembly + "'s baseline records public API the assembly does not have: " + line;
    }

    // ---------------------------------------------------------------------------------------------
    // SL7: generated data is what its generator writes.

    /// <summary>Compares a generated area as committed with what its generator writes, file by file and byte for byte.</summary>
    internal static IEnumerable<string> SL7(IReadOnlyDictionary<string, byte[]> committed, IReadOnlyDictionary<string, byte[]> generated)
    {
        foreach (var name in committed.Keys.Order(StringComparer.Ordinal))
        {
            if (!generated.TryGetValue(name, out var written))
                yield return name + " is committed in a generated area, and the generator does not write it";
            else if (!committed[name].AsSpan().SequenceEqual(written))
                yield return name + " differs from what the generator writes";
        }

        foreach (var name in generated.Keys.Order(StringComparer.Ordinal).Where(n => !committed.ContainsKey(n)))
            yield return name + " is written by the generator and not committed";
    }

    // ---------------------------------------------------------------------------------------------
    // SL8: assurance coverage.

    /// <summary>
    /// SL8's structural half: the shared folder's assurance configuration covers every shared
    /// library and forbids directives. That every relevant unit carries its block, and that the
    /// artefacts are current, is the assurance tool's <c>check</c>, which CI runs.
    /// </summary>
    internal static IEnumerable<string> SL8(JsonDocument config, IEnumerable<string> libraryProjects)
    {
        var root = config.RootElement;
        var projects = root.TryGetProperty("projects", out var list)
            ? list.EnumerateArray().Select(static p => p.GetString()!).ToHashSet(StringComparer.Ordinal)
            : [];
        foreach (var project in libraryProjects.Where(p => !projects.Contains(p)))
            yield return "assurance.config.json does not cover " + project;
        if (!root.TryGetProperty("forbidDirectives", out var forbid) || forbid.ValueKind != JsonValueKind.True)
            yield return "assurance.config.json does not forbid preprocessor directives";
        if (!root.TryGetProperty("exemptionPredicate", out var predicate) || predicate.GetString() != "strict")
            yield return "assurance.config.json does not use the strict exemption predicate";
    }

    // ---------------------------------------------------------------------------------------------
    // SL9: determinism.

    internal static IEnumerable<string> SL9(IReadOnlyList<string> runs)
    {
        for (var i = 1; i < runs.Count; i++)
        {
            if (!string.Equals(runs[0], runs[i], StringComparison.Ordinal))
                yield return "run " + (i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture) + " differs from run 1";
        }
    }

    // ---------------------------------------------------------------------------------------------
    // SL10: no preprocessor directives, LF line endings.

    internal static IEnumerable<string> SL10Directives(string relativePath, string text)
    {
        var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(text);
        foreach (var trivia in tree.GetRoot().DescendantTrivia(descendIntoTrivia: true).Where(static t => t.IsDirective))
        {
            var line = trivia.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            yield return relativePath + ":" + line.ToString(System.Globalization.CultureInfo.InvariantCulture) + ": " + trivia.ToString().Trim();
        }
    }

    internal static IEnumerable<string> SL10LineEndings(string relativePath, byte[] content)
    {
        var at = Array.IndexOf(content, (byte)'\r');
        if (at >= 0)
            yield return relativePath + " holds a CR at byte " + at.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
