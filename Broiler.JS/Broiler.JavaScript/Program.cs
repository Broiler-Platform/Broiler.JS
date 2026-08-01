using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Broiler.JavaScript.Engine;
using BroilerJS;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.ExpressionCompiler.Generator;
using Broiler.JavaScript.Runtime;
using BroilerJS.Utils;
using BroilerJS.REPL;

namespace BroilerJS
{
    public class Program
    {
        private static IDisposable CreateSynchronizationContext()
        {
            if (SynchronizationContext.Current == null)
            {
                SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
                return new DisposableAction(() => SynchronizationContext.SetSynchronizationContext(null));
            }

            return DisposableAction.Empty;
        }

        public static async Task Main(string[] args)
        {

            // DictionaryCodeCache.Current = new AssemblyCodeCache();

            ILCodeGenerator.GenerateLogs = string.Equals(
                Environment.GetEnvironmentVariable("BROILER_GENERATE_IL_LOGS"),
                "1",
                StringComparison.Ordinal);

            var recognizedOptions = new HashSet<string>(StringComparer.Ordinal)
            {
                "--script-host"
            };

            var scriptHostMode = args.Contains("--script-host");
            var positionalArgs = args.Where(arg => !recognizedOptions.Contains(arg)).ToArray();
            var scriptPath = positionalArgs.FirstOrDefault(arg => !arg.StartsWith("-"));

            if (scriptPath == null)
            {
                // no parameter....

                // start REPL
                var c = new BroilerJSRepl();
                c.Run();
                return;
            }

            var file = new FileInfo(scriptPath);
            if (!file.Exists)
                throw new FileNotFoundException(file.FullName);

            var filePath = new FileInfo(typeof(Program).Assembly.Location);
            var inbuilt = filePath.DirectoryName + "/modules";
            
            if (scriptHostMode)
            {
                using var sc = CreateSynchronizationContext();
                using var context = new JSContext(
                    SynchronizationContext.Current,
                    experimentalFeatures: JavaScriptFeatureFlags.AllExperimentalEs2026,
                    options: new JSContextOptions { ScriptHostMode = true });
                DefineScriptHostGlobals(context);
                var code = await File.ReadAllTextAsync(file.FullName);
                // Pass the global context explicitly so top-level `this` resolves to
                // the same host object that owns the evaluated script. Prefer the
                // synchronous evaluator for ordinary fixtures and fall back to the
                // top-level-await path only when the parser rejects the script for
                // using await at the top level.
                try
                {
                    context.Eval(code, file.FullName, context);
                }
                catch (Exception ex) when (ex.Message.Contains("Unexpected await", StringComparison.Ordinal))
                {
                    await context.EvalWithTopLevelAwaitAsync(code, file.FullName, context);
                }
                return;
            }

            var yc = new BroilerJSContext(file.DirectoryName);
            var r = await yc.RunAsync(
                file.DirectoryName, "./" + file.Name, 
                new string[] { 
                    file.DirectoryName,
                    file.DirectoryName + "/node_modules",
                    inbuilt
                });
            if (!r.IsUndefined)
                Console.WriteLine(r);
        }

        // Host functions provided by JavaScript shells (SpiderMonkey's `js`, V8's `d8`)
        // that are not part of ECMAScript but are relied upon by many test262 staging
        // tests — most notably the imported SpiderMonkey `sm/` suite. Defined only in
        // script-host mode so they do not leak into the embeddable engine surface.
        private static void DefineScriptHostGlobals(JSContext context)
        {
            // `print(...values)` writes the space-joined string form of its arguments,
            // followed by a newline, to standard output and returns undefined.
            context[Broiler.JavaScript.Storage.KeyStrings.GetOrCreate("print")] = JSValue.CreateFunction(
                static (in Arguments a) =>
                {
                    var parts = new string[a.Length];
                    for (var i = 0; i < a.Length; i++)
                        parts[i] = a.GetAt(i).ToString();

                    Console.WriteLine(string.Join(" ", parts));
                    return JSUndefined.Value;
                },
                "print",
                length: 1);

            // `read(path)` returns the file's contents as a string; `read(path, "binary")`
            // returns them as a Uint8Array. The companion to `print` in both d8 and
            // SpiderMonkey's `js`, and referenced unconditionally by Emscripten's shell
            // preamble — `Module.read = read` — which is how Octane's zlib benchmark loads
            // under a JS shell. Without the binding that bare reference was a ReferenceError
            // before the benchmark ran a single line, even though it never reads a file
            // (its corpus is embedded in zlib-data.js).
            context[Broiler.JavaScript.Storage.KeyStrings.GetOrCreate("read")] = JSValue.CreateFunction(
                (in Arguments a) =>
                {
                    var (pathValue, modeValue) = a.Get2();
                    if (pathValue.IsUndefined)
                        throw Broiler.JavaScript.Engine.Core.JSEngine.NewTypeError("read requires a file path");

                    var path = pathValue.ToString();
                    var binary = !modeValue.IsUndefined
                        && string.Equals(modeValue.ToString(), "binary", StringComparison.Ordinal);

                    try
                    {
                        if (!binary)
                            return new Broiler.JavaScript.BuiltIns.String.JSString(File.ReadAllText(path));

                        var buffer = new Broiler.JavaScript.BuiltIns.Array.Typed.JSArrayBuffer(File.ReadAllBytes(path));
                        var uint8Array = context[Broiler.JavaScript.Storage.KeyStrings.GetOrCreate("Uint8Array")];
                        return uint8Array.CreateInstance(new Arguments(JSUndefined.Value, buffer));
                    }
                    catch (IOException ex)
                    {
                        throw Broiler.JavaScript.Engine.Core.JSEngine.NewError($"Cannot read file {path}: {ex.Message}");
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        throw Broiler.JavaScript.Engine.Core.JSEngine.NewError($"Cannot read file {path}: {ex.Message}");
                    }
                },
                "read",
                length: 1);
        }
    }

}
