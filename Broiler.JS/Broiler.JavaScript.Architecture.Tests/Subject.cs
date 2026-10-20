using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.Loader;

namespace Broiler.JavaScript.Architecture.Tests;

/// <summary>
/// The parser assembly the test judges: the build beside this assembly, or the one the environment
/// variable <c>BROILER_ARCH_PARSER_ASSEMBLY</c> names (part B of bundle sjl-12-001's rule runs the
/// test over the rule commit's parser that way).
/// </summary>
internal static class Subject
{
    internal const string Variable = "BROILER_ARCH_PARSER_ASSEMBLY";

    internal const string AssemblyName = "Broiler.JavaScript.Parser";

    /// <summary>The path the environment variable names, or null when it is unset or empty.</summary>
    internal static string? Named => Environment.GetEnvironmentVariable(Variable) is { Length: > 0 } named ? named : null;

    /// <summary>The full path of the parser assembly to judge.</summary>
    internal static string Locate()
    {
        if (Named is { } named)
        {
            var full = Path.GetFullPath(named);
            if (!File.Exists(full))
                throw new FileNotFoundException(Variable + " names " + full + ", which is not a file. Name the parser assembly itself, "
                    + AssemblyName + ".dll, in the build output that holds its dependencies.", full);
            return full;
        }

        var beside = Path.Combine(AppContext.BaseDirectory, AssemblyName + ".dll");
        if (!File.Exists(beside))
            throw new FileNotFoundException(AssemblyName + ".dll is not beside the test assembly; the project references the parser so that it is.", beside);
        return beside;
    }
}

/// <summary>
/// The judged parser, loaded with its dependencies from its own directory into a collectible load
/// context, and driven by reflection, so that the behavioural fixtures run the build the metadata
/// clauses read, whichever build that is. Only <c>Broiler.*</c> assemblies come from that directory,
/// and one that is not there fails the load rather than coming from another build; the platform's
/// come from the default context.
/// </summary>
internal sealed class SubjectParser : IDisposable
{
    private readonly ConcurrentDictionary<(Type, string), Func<object, object?>> readers = new();

    private readonly LoadContext context;
    private readonly ConstructorInfo span;
    private readonly ConstructorInfo stream;
    private readonly ConstructorInfo parser;
    private readonly MethodInfo parseProgram;
    private readonly PropertyInfo current;

    internal SubjectParser(string path)
    {
        context = new LoadContext(path);
        var parserAssembly = context.LoadFromAssemblyPath(path);
        var ast = context.LoadFromAssemblyName(new AssemblyName("Broiler.JavaScript.Ast"));

        var stringSpan = Required(ast, "Broiler.JavaScript.Ast.Misc.StringSpan");
        AstNode = Required(ast, "Broiler.JavaScript.Ast.AstNode");
        Token = Required(ast, "Broiler.JavaScript.Ast.Misc.FastToken");
        StringSpan = stringSpan;

        var tokenStream = Required(parserAssembly, "Broiler.JavaScript.Parser.FastTokenStream");
        var keywords = Required(parserAssembly, "Broiler.JavaScript.Parser.FastKeywordMap");
        var fastParser = Required(parserAssembly, "Broiler.JavaScript.Parser.FastParser");

        span = stringSpan.GetConstructor([typeof(string)]) ?? throw Missing("StringSpan(string)");
        stream = tokenStream.GetConstructor([stringSpan.MakeByRefType(), keywords]) ?? throw Missing("FastTokenStream(in StringSpan, FastKeywordMap)");
        parser = fastParser.GetConstructor([tokenStream]) ?? throw Missing("FastParser(FastTokenStream)");
        parseProgram = fastParser.GetMethod("ParseProgram", BindingFlags.Public | BindingFlags.Instance, Type.EmptyTypes) ?? throw Missing("FastParser.ParseProgram()");
        current = tokenStream.GetProperty("Current", BindingFlags.Public | BindingFlags.Instance) ?? throw Missing("FastTokenStream.Current");
    }

    /// <summary><c>Broiler.JavaScript.Ast.AstNode</c> as the judged build declares it.</summary>
    internal Type AstNode { get; }

    /// <summary><c>Broiler.JavaScript.Ast.Misc.FastToken</c> as the judged build declares it.</summary>
    internal Type Token { get; }

    /// <summary><c>Broiler.JavaScript.Ast.Misc.StringSpan</c> as the judged build declares it.</summary>
    internal Type StringSpan { get; }

    /// <summary>
    /// Parses <paramref name="source"/> as a script, as <c>new FastParser(new FastTokenStream(source)).ParseProgram()</c>
    /// does, and returns the program with the token the stream stands on afterwards, which is the
    /// end of the input.
    /// </summary>
    internal (object Program, object EndOfInput) Parse(string source)
    {
        try
        {
            var text = span.Invoke([source]);
            var tokens = stream.Invoke([text, null]);
            var program = parseProgram.Invoke(parser.Invoke([tokens]), null)!;
            return (program, current.GetValue(tokens)!);
        }
        catch (TargetInvocationException invocation) when (invocation.InnerException is { } inner)
        {
            ExceptionDispatchInfo.Throw(inner);
            throw;
        }
    }

    /// <summary>The value of the instance field or property <paramref name="member"/>, declared on the object's type or a base.</summary>
    internal object? Read(object owner, string member) =>
        readers.GetOrAdd((owner.GetType(), member), static key => Reader(key.Item1, key.Item2))(owner);

    public void Dispose()
    {
        readers.Clear();
        context.Unload();
    }

    private static Func<object, object?> Reader(Type type, string member)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        for (var t = type; t is not null; t = t.BaseType)
        {
            if (t.GetField(member, flags | BindingFlags.DeclaredOnly) is { } field)
                return field.GetValue;
            if (t.GetProperty(member, flags | BindingFlags.DeclaredOnly) is { GetMethod: not null } property && property.GetIndexParameters().Length == 0)
                return property.GetValue;
        }

        throw new MissingMemberException(type.FullName, member);
    }

    private static Type Required(Assembly assembly, string name) =>
        assembly.GetType(name, throwOnError: false) ?? throw Missing(name + " in " + assembly.GetName().Name);

    private static MissingMemberException Missing(string what) =>
        new("the judged parser build has no " + what + ", which the behavioural fixtures drive");

    private sealed class LoadContext(string path) : AssemblyLoadContext("parser under architecture test", isCollectible: true)
    {
        private readonly string directory = Path.GetDirectoryName(path)!;

        protected override Assembly? Load(AssemblyName name)
        {
            if (name.Name is not { } simple || !simple.StartsWith("Broiler.", StringComparison.Ordinal))
                return null;
            var candidate = Path.Combine(directory, simple + ".dll");
            if (File.Exists(candidate))
                return LoadFromAssemblyPath(candidate);

            // A satellite assembly that is not there is a resource lookup falling back to the
            // neutral culture, as it would anywhere.
            if (!string.IsNullOrEmpty(name.CultureName))
                return null;

            // Falling back to the default context would run the judged parser against this test's
            // build of the dependency, another commit's when the variable names another build, and
            // nothing would say so.
            throw new FileNotFoundException(
                simple + ".dll, which the judged parser needs, is not in " + directory + ". Name the parser assembly in a build "
                + "output that holds all its dependencies, such as a test or application project's, not the parser project's own.",
                candidate);
        }
    }
}
