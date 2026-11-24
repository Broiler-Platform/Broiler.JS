// Bundle sjl-14-001, part J, a reading aid of the after records (2026-11-24); not the rule's session and no run the rule
// names. It compiles one file through CoreScript.Compile exactly as the session's Compile does
// (docs/evidence/sjl-14-001/tools/fuzz/Program.cs: a JSContext with every ES2026 experimental feature made on the
// compiling thread, a thread of 16 MiB, PassThrough's code cache, location "mutant.js", no arguments, no compilation
// options, isModule by the goal), in a process of its own and with nothing else compiling, and reports the outcome as
// the session reads it (accepted; refused with a SyntaxError; any other exception a finding), the time the compile took
// and whether it ended within the session's bound. Unlike the session, it waits past the bound, up to a cap, so that
// the compile's own time is known; past the cap it reports the compile as still running.
//
//     single-case <file> <script|module> <bound ms> <cap ms>
//
// One line on standard output: file, goal, outcome, type, message, elapsed ms, within-bound (yes/no/still-running).

using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.ExpressionCompiler.Runtime;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Storage;

var file = args[0];
var module = args[1] == "module";
var bound = int.Parse(args[2], CultureInfo.InvariantCulture);
var cap = int.Parse(args[3], CultureInfo.InvariantCulture);
var bytes = File.ReadAllBytes(file);
var text = new UTF8Encoding(false, true).GetString(bytes);
(string, string, string) result = ("running", "", "");
var watch = new Stopwatch();
var worker = new Thread(() =>
{
    try
    {
        using var context = new JSContext(experimentalFeatures: JavaScriptFeatureFlags.AllExperimentalEs2026);
        watch.Start();
        CoreScript.Compile(new StringSpan(text), location: "mutant.js", args: null, codeCache: PassThrough.Instance,
            compilationOptions: null, isModule: module);
        watch.Stop();
        result = ("accepted", "", "");
    }
    catch (JSException exception) when (ErrorName(exception) == "SyntaxError")
    {
        watch.Stop();
        result = ("refused", "SyntaxError", exception.Message);
    }
    catch (Exception exception)
    {
        watch.Stop();
        result = ("finding", exception.GetType().FullName ?? "?", exception.Message);
    }
}, 16 * 1024 * 1024) { IsBackground = true };
var wall = Stopwatch.StartNew();
worker.Start();
var ended = worker.Join(cap);
var elapsed = ended ? watch.Elapsed.TotalMilliseconds : wall.Elapsed.TotalMilliseconds;
var within = !ended ? "still-running" : elapsed <= bound ? "yes" : "no";
var (outcome, type, message) = result;
Console.WriteLine(string.Join('\t', Path.GetFileName(file), Convert.ToHexStringLower(SHA256.HashData(bytes)),
    args[1], outcome, type, Flatten(message), elapsed.ToString("F0", CultureInfo.InvariantCulture), within));
return 0;

static string Flatten(string value) => value.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');

static string ErrorName(JSException exception)
{
    try
    {
        var name = exception.Error?[KeyStrings.GetOrCreate("name")];
        return name == null || name.IsUndefined ? "" : name.ToString();
    }
    catch (Exception)
    {
        return "";
    }
}

internal sealed class PassThrough : ICodeCache
{
    internal static readonly PassThrough Instance = new();

    public JSFunctionDelegate GetOrCreate(in JSCode code) => code.Compiler().CompileWithNestedLambdas();
}
