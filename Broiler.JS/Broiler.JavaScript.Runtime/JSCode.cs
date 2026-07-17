using System.Collections.Generic;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.ExpressionCompiler.Runtime;

namespace Broiler.JavaScript.Runtime;

/// <summary>All compilation-affecting host inputs that participate in a code-cache key.</summary>
public readonly record struct JSCompilationOptions(
    bool ScriptHostMode = false,
    int FeatureFlags = 0,
    ExpressionCompilationBackend Backend = ExpressionCompilationBackend.DynamicMethod,
    int SemanticVersion = 1);

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
