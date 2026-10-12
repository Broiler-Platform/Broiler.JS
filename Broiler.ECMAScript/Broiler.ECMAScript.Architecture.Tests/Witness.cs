using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Broiler.ECMAScript.Architecture.Tests;

/// <summary>
/// The witnesses: inputs each rule must refuse. A source witness is compiled here, in memory,
/// against the platform's own assemblies, and the rule reads the image the way it reads a shared
/// library's, so a rule that stops recognising a violation stops refusing its witness.
/// </summary>
internal static class Witness
{
    internal static string PathOf(string file) => Path.Combine(SharedTree.Witnesses, file);

    internal static string Text(string file) => File.ReadAllText(PathOf(file));

    /// <summary>Compiles a witness source and reads the assembly it makes.</summary>
    internal static AssemblyFacts Compile(string file, params string[] referencedAssemblies)
    {
        var image = Emit(file, referencedAssemblies);
        return AssemblyFacts.Read(image);
    }

    internal static byte[] Emit(string file, params string[] referencedAssemblies)
    {
        var source = Text(file);
        var name = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(file)).Replace('-', '_');
        var tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest), path: file);
        var references = PlatformReferences()
            .Concat(referencedAssemblies.Select(static a => MetadataReference.CreateFromFile(SharedTree.AssemblyPath(a))));
        var compilation = CSharpCompilation.Create(
            name,
            [tree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable, allowUnsafe: true));
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        if (!result.Success)
        {
            var errors = result.Diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error);
            throw new InvalidOperationException("the witness " + file + " does not compile:\n" + string.Join("\n", errors));
        }

        return stream.ToArray();
    }

    private static IEnumerable<MetadataReference> PlatformReferences()
    {
        var trusted = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
        return trusted.Split(Path.PathSeparator)
            .Where(static path => Path.GetFileName(path).StartsWith("System.", StringComparison.Ordinal)
                || Path.GetFileName(path) is "netstandard.dll" or "mscorlib.dll" or "Microsoft.CSharp.dll")
            .Select(static path => MetadataReference.CreateFromFile(path));
    }
}
