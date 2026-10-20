using System.Collections.Generic;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.ExpressionCompiler.Runtime;

namespace Broiler.JavaScript.Runtime;

/// <summary>All compilation-affecting host inputs that participate in a code-cache key.</summary>
public readonly record struct JSCompilationOptions(
    bool ScriptHostMode = false,
    int FeatureFlags = 0,
    ExpressionCompilationBackend Backend = ExpressionCompilationBackend.DynamicMethod,
    int SemanticVersion = 1,
    // Whether the source is parsed with the MODULE goal symbol rather than the script one. It
    // belongs here, and not only in an ambient scope, because it changes what the text MEANS:
    // `await` is a reserved word in module code, so the same characters are a valid script and an
    // invalid module. Two compiles that disagree about it must not share a cache entry.
    bool IsModule = false)
{
    /// <summary>
    /// Whether <c>await</c> may stand at the top level of the code compiled: an AwaitExpression outside
    /// every function, and a top-level <c>for await</c>. False by default; <c>EvalWithTopLevelAwaitAsync</c>
    /// sets it for the program it is given, and a module is compiled with it.
    /// </summary>
    /// <remarks>
    /// It belongs here, and so in the code cache's key, because it changes what the text means: in a
    /// script <c>await (21)</c> calls a function named <c>await</c>, and with the permission it awaits
    /// 21. Two compiles that disagree about it must not share a cache entry. Before milestone SJL-12.5
    /// it reached the parser through an ambient scope of <c>CoreScript</c> and was not in the key, so one
    /// context answered a compile from the other mode's entry (correction J2 in the changelog).
    /// </remarks>
    public bool AllowTopLevelAwait { get; init; }
}

public readonly struct JSCode(
    string location,
    in StringSpan code,
    IList<string> args,
    JSCodeCompiler compiler,
    JSCompilationOptions options = default)
{
    public readonly string Location = location;
    public readonly StringSpan Code = code;
    public readonly IList<string> Arguments = args;
    public readonly JSCodeCompiler Compiler = compiler;
    public readonly JSCompilationOptions Options = options;

    public JSCode Clone() => new(Location, Code, Arguments, Compiler, Options);

    public string Key
    {
        get
        {
            if (Arguments != null)
                return $"`OPTIONS:{Options};LOCATION:{Location};ARGS:{string.Join(",", Arguments)}\r\n{Code}";

            return $"`OPTIONS:{Options};LOCATION:{Location};ARGS:\r\n{Code}";
        }
    }
}
