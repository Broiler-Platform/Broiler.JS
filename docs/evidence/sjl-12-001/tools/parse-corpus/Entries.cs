using System.Reflection;
using Broiler.JavaScript.Ast;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.ExpressionCompiler.Runtime;
using Broiler.JavaScript.Parser;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Storage;

namespace ParseCorpus;

/// <summary>What one entry point answered for one input and variant (decision rule part C).</summary>
internal readonly record struct Outcome(string Kind, string? Type, string? Message)
{
    internal const string Accepted = "accepted";
    internal const string Refused = "refused";
    internal const string Crashed = "crashed";
    internal const string NotApplicable = "not-applicable";

    internal static Outcome Accept() => new(Accepted, null, null);

    // A crash keeps its message for the reader; the comparison compares a crash's type only.
    internal static Outcome Crash(Exception exception) => new(Crashed, exception.GetType().FullName, exception.Message);
}

/// <summary>
/// The two entry points of part C and the for-loop pass of part D. They name only members that
/// exist at the corpus commit and at the after commit: the <c>FastParser(FastTokenStream)</c>
/// constructor over <c>FastTokenStream(in StringSpan)</c> and <c>ParseProgram</c>,
/// <c>FastParser.ParseModule</c>, and <c>CoreScript.Compile</c>.
/// </summary>
internal static class Entries
{
    internal const string NoEntryPoint = "the goal json has no entry point in part C (P parses the script and module goals, K compiles a script or a module)";

    /// <summary>
    /// P, the parser alone: a <c>FastParser</c> over a <c>FastTokenStream</c> and
    /// <c>ParseProgram</c> for the script goal, <c>FastParser.ParseModule</c> for the module goal.
    /// Refused is a <c>FastParseException</c>; any other exception is a crash.
    /// </summary>
    /// <remarks>
    /// The keyword map is passed explicitly (null, the stream's default) so that the call stays the
    /// same overload if the after commit adds a constructor with other optional parameters
    /// (JSD-0080 decision D19 adds options overloads to these types).
    /// </remarks>
    internal static Outcome Parse(string text, bool module, out AstProgram? program)
    {
        program = null;
        try
        {
            program = module
                ? FastParser.ParseModule(new StringSpan(text))
                : new FastParser(new FastTokenStream(new StringSpan(text), (FastKeywordMap?)null)).ParseProgram();
            return Outcome.Accept();
        }
        catch (FastParseException exception)
        {
            return new Outcome(Outcome.Refused, exception.GetType().FullName, exception.Message);
        }
        catch (Exception exception)
        {
            return Outcome.Crash(exception);
        }
    }

    /// <summary>
    /// K, the whole compile: <c>CoreScript.Compile</c> with the module flag and the pass-through
    /// cache, under the source name the manifest fixes. Refused is a <c>JSException</c> whose
    /// thrown value is a JavaScript <c>SyntaxError</c> (its <c>name</c>, read as the shell reads
    /// it); any other exception, a <c>JSException</c> of another error included, is a crash. A
    /// <c>JSException</c>'s type is written with the thrown value's name after a colon.
    /// </summary>
    internal static Outcome Compile(string text, bool module, string sourceName)
    {
        try
        {
            CoreScript.Compile(
                new StringSpan(text),
                location: sourceName,
                args: null,
                codeCache: PassThroughCodeCache.Instance,
                compilationOptions: null,
                isModule: module);
            return Outcome.Accept();
        }
        catch (JSException exception)
        {
            var name = ErrorName(exception);
            var type = exception.GetType().FullName + ":" + name;
            return name == "SyntaxError"
                ? new Outcome(Outcome.Refused, type, exception.Message)
                : new Outcome(Outcome.Crashed, type, exception.Message);
        }
        catch (Exception exception)
        {
            return Outcome.Crash(exception);
        }
    }

    private static string ErrorName(JSException exception)
    {
        try
        {
            var error = exception.Error;
            if (error == null)
                return "(none)";

            var name = error[KeyStrings.GetOrCreate("name")];
            return name.IsUndefined ? "(undefined)" : name.ToString();
        }
        catch (Exception reading)
        {
            return "(unreadable: " + reading.GetType().FullName + ")";
        }
    }

    /// <summary>
    /// The context K compiles in, made on the thread that uses it: the shell's experimental
    /// features, as test262 runs (<c>Broiler.JavaScript/Program.cs</c>). A compile without one
    /// cannot build the <c>SyntaxError</c> it refuses with.
    /// </summary>
    internal static JSContext NewContext() => new(experimentalFeatures: JavaScriptFeatureFlags.AllExperimentalEs2026);

    /// <summary>
    /// The code cache K passes: it compiles the code it is given and stores nothing, since a null
    /// cache selects the context's or the default one (rule part C).
    /// </summary>
    private sealed class PassThroughCodeCache : ICodeCache
    {
        internal static readonly PassThroughCodeCache Instance = new();

        public JSFunctionDelegate GetOrCreate(in JSCode code) => code.Compiler().CompileWithNestedLambdas();
    }
}

/// <summary>
/// The compiler's for-loop pass (decision rule part D), which only the after commit declares: in
/// the assembly <c>Broiler.JavaScript.Compiler</c>, the class
/// <c>Broiler.JavaScript.Compiler.ForLoopPreparation</c> and its static method
/// <c>AstProgram Prepare(AstProgram program, int directEvalDepth)</c>, public or not. It is looked
/// up by that name and signature when the tool runs, so the tool needs no change when item 5 adds
/// it; where there is none, the run dumps the parser's tree alone and its report says so.
/// </summary>
internal sealed class ForLoopPass
{
    internal const string AssemblyName = "Broiler.JavaScript.Compiler";
    internal const string TypeName = "Broiler.JavaScript.Compiler.ForLoopPreparation";
    internal const string MethodName = "Prepare";
    internal const string Signature = TypeName + "." + MethodName + "(Broiler.JavaScript.Ast.AstProgram, System.Int32) : Broiler.JavaScript.Ast.AstProgram";

    private readonly Func<AstProgram, int, AstProgram>? prepare;

    private ForLoopPass(Func<AstProgram, int, AstProgram>? prepare, string status)
    {
        this.prepare = prepare;
        Status = status;
    }

    /// <summary><c>found</c>, or <c>absent</c> and why.</summary>
    internal string Status { get; }

    internal bool Found => prepare != null;

    internal static ForLoopPass Find()
    {
        Assembly assembly;
        try
        {
            assembly = Assembly.Load(AssemblyName);
        }
        catch (Exception exception) when (exception is FileNotFoundException or FileLoadException or BadImageFormatException)
        {
            return new ForLoopPass(null, $"absent: the assembly {AssemblyName} did not load ({exception.GetType().Name})");
        }

        var type = assembly.GetType(TypeName, throwOnError: false);
        if (type == null)
            return new ForLoopPass(null, $"absent: {AssemblyName} declares no type {TypeName}");

        var method = type.GetMethod(
            MethodName,
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
            binder: null,
            types: [typeof(AstProgram), typeof(int)],
            modifiers: null);
        if (method == null)
            return new ForLoopPass(null, $"absent: {TypeName} declares no static {MethodName}(AstProgram, int)");
        if (method.ReturnType != typeof(AstProgram))
            return new ForLoopPass(null, $"absent: {TypeName}.{MethodName}(AstProgram, int) returns {method.ReturnType.FullName}, not AstProgram");

        return new ForLoopPass(method.CreateDelegate<Func<AstProgram, int, AstProgram>>(), "found: " + Signature);
    }

    /// <summary>The pass over <paramref name="program"/> with a direct-eval depth of 0.</summary>
    internal AstProgram Prepare(AstProgram program) => prepare!(program, 0);
}
