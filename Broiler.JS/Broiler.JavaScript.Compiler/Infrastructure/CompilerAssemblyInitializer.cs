using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine.FastParser.Compiler;

namespace Broiler.JavaScript.Compiler;

/// <summary>
/// Module initializer for the <c>Broiler.JavaScript.Compiler</c> assembly.
/// The <see cref="Initialize"/> method is invoked automatically by the runtime
/// when this assembly is loaded.  Because .NET loads assemblies lazily,
/// <see cref="DefaultJSCompiler"/> proactively loads this assembly in its
/// static constructor to guarantee that <see cref="Initialize"/> runs before
/// the first compilation attempt.
/// </summary>
internal static class CompilerAssemblyInitializer
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        // Register the FastCompiler-based compilation pipeline, which compiles under the options
        // CoreScript.Compile keys the compilation under: its goal and its top-level-await permission.
        DefaultJSCompiler.Register(
            (code, options, location, argsList, codeCache) =>
            {
                var compiler = new FastCompiler(code, options, location, argsList, codeCache);
                return compiler.Method;
            });
    }
}
