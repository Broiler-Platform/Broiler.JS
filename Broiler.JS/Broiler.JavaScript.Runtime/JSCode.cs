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
    // belongs here, in the key the compiler is also given, because it changes what the text MEANS:
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

    /// <summary>
    /// The test-only switch of milestone SJL-14.2 (Broiler.VM's JSD-0083, decision D43): when set, the compiler parses
    /// through the bridge to the shared syntax library (<c>Broiler.JavaScript.Parser.Bridge.SharedSyntaxBridge</c>)
    /// instead of <c>FastParser</c>. Off by default; tests set it through the internals grant, the shell through its
    /// hidden <c>--parse-through-bridge</c> flag, and a build with the MSBuild property
    /// <c>BroilerParseThroughBridge=true</c> turns it on for every compile, which is how the test projects run with it
    /// on. Window W2 turns it on for good and removes it.
    /// </summary>
    /// <remarks>
    /// Internal, so it is in no public listing and in no <see cref="ToString"/>. <see cref="JSCode.Key"/>, the key of the
    /// in-memory code caches (<c>DictionaryCodeCache</c>), names it apart, so that those caches never answer one parse's
    /// compile with the other's. The shell's persistent <c>AssemblyCodeCache</c> keys on <see cref="ToString"/> and does
    /// not tell them apart; no host uses it with the switch on (the shell's own use of it is commented out).
    /// </remarks>
    internal bool ParseThroughBridge { get; init; }

    /// <summary>Whether this compile parses through the bridge: the switch, or a build that turns it on for every compile.</summary>
    internal bool ParsesThroughBridge => ParseThroughBridge || BridgeByDefault;

#if BROILER_PARSE_THROUGH_BRIDGE
    internal const bool BridgeByDefault = true;
#else
    internal const bool BridgeByDefault = false;
#endif
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
            // The bridge's switch (milestone SJL-14.2) is no member ToString writes, so it is named here, and only
            // when it is on: with it off every key is the one it was.
            var bridge = Options.ParsesThroughBridge ? ";BRIDGE" : string.Empty;
            if (Arguments != null)
                return $"`OPTIONS:{Options}{bridge};LOCATION:{Location};ARGS:{string.Join(",", Arguments)}\r\n{Code}";

            return $"`OPTIONS:{Options}{bridge};LOCATION:{Location};ARGS:\r\n{Code}";
        }
    }
}
