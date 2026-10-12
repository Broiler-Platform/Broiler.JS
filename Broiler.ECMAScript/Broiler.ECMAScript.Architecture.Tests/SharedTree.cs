using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Broiler.ECMAScript.Architecture.Tests;

/// <summary>What a project below Broiler.ECMAScript/ is, for the rules that hold it.</summary>
internal enum SharedProjectKind
{
    /// <summary>A shared library: a library output that is not a test project. Every rule holds it.</summary>
    Library,

    /// <summary>The Native AOT probe: an executable. SL5 and SL10 hold it; SL3's I/O clause does not.</summary>
    Probe,

    /// <summary>A test project. SL10 holds it.</summary>
    Test,
}

/// <summary>One project below Broiler.ECMAScript/, read from its project file.</summary>
internal sealed record SharedProject(string Name, string Path, SharedProjectKind Kind, XDocument Document)
{
    internal string Directory => System.IO.Path.GetDirectoryName(Path)!;

    internal IEnumerable<string> Items(string itemType) => Document.Descendants(itemType)
        .Select(static element => (string?)element.Attribute("Include"))
        .Where(static include => include is not null)
        .Select(static include => include!);
}

/// <summary>
/// The repository as the rules read it: the shared folder, its projects, and the build output the
/// test project's references put beside this assembly.
/// </summary>
internal static class SharedTree
{
    internal const string FolderName = "Broiler.ECMAScript";

    /// <summary>The repository root: the first directory above the test assembly that holds the shared solution filter.</summary>
    internal static string Root { get; } = FindRoot();

    internal static string Folder => Path.Combine(Root, FolderName);

    internal static string TestProjectDirectory => Path.Combine(Folder, "Broiler.ECMAScript.Architecture.Tests");

    internal static string Witnesses => Path.Combine(TestProjectDirectory, "witnesses");

    /// <summary>Every project below the shared folder, in ordinal order of name.</summary>
    internal static IReadOnlyList<SharedProject> Projects { get; } = ReadProjects();

    internal static IReadOnlyList<SharedProject> Libraries { get; } =
        Projects.Where(static p => p.Kind == SharedProjectKind.Library).ToList();

    internal static SharedProject Project(string name) => Projects.Single(p => p.Name == name);

    /// <summary>
    /// The files of the shared folder that a rule about sources reads: everything outside build
    /// output and outside the witnesses, which exist to break the rules.
    /// </summary>
    internal static IEnumerable<string> SourceFiles(string pattern) =>
        Directory.EnumerateFiles(Folder, pattern, SearchOption.AllDirectories)
            .Where(static path => !IsExcluded(path))
            .Order(StringComparer.Ordinal);

    internal static string Relative(string path) =>
        Path.GetRelativePath(Root, path).Replace(Path.DirectorySeparatorChar, '/');

    /// <summary>The built assembly of a shared project, from this test's output directory.</summary>
    internal static string AssemblyPath(string assemblyName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, assemblyName + ".dll");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                assemblyName + ".dll is not in the test output. Every shared library is referenced by "
                + "this project so that the rules read the build that just happened.", path);
        }

        return path;
    }

    private static bool IsExcluded(string path)
    {
        var relative = Path.GetRelativePath(Folder, path).Replace(Path.DirectorySeparatorChar, '/');
        var segments = relative.Split('/');
        return segments.Any(static s => s is "bin" or "obj")
            || relative.StartsWith("Broiler.ECMAScript.Architecture.Tests/witnesses/", StringComparison.Ordinal);
    }

    private static string FindRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, FolderName + ".slnf")))
                return directory.FullName;
        }

        throw new DirectoryNotFoundException("no " + FolderName + ".slnf above " + AppContext.BaseDirectory);
    }

    private static List<SharedProject> ReadProjects()
    {
        var projects = new List<SharedProject>();
        foreach (var path in SourceFiles("*.csproj"))
        {
            var document = XDocument.Load(path);
            var name = Path.GetFileNameWithoutExtension(path);
            var isTest = document.Descendants("IsTestProject").Any(static e => e.Value.Trim() == "true");
            var isExe = document.Descendants("OutputType").Any(static e => e.Value.Trim() == "Exe");
            var kind = isTest ? SharedProjectKind.Test : isExe ? SharedProjectKind.Probe : SharedProjectKind.Library;
            projects.Add(new SharedProject(name, path, kind, document));
        }

        projects.Sort(static (a, b) => string.CompareOrdinal(a.Name, b.Name));
        return projects;
    }
}
