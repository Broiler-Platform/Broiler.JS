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
    bool IsModule = false);

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
