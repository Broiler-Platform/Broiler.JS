using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Broiler.JavaScript.Engine;
using BroilerJS;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.ExpressionCompiler.Generator;
using Broiler.JavaScript.Runtime;
using BroilerJS.Utils;
using BroilerJS.REPL;

namespace BroilerJS
{
    public class Program
    {
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
                Environment.SetEnvironmentVariable("BROILER_SCRIPT_HOST", "1");
                using var context = new JSContext(experimentalFeatures: JavaScriptFeatureFlags.AllExperimentalEs2026);
                var code = await File.ReadAllTextAsync(file.FullName);
                // Pass the global context explicitly so top-level `this` resolves to
                // the same host object that owns the evaluated script. Script-host
                // runs standalone test262 fixtures too, so allow top-level await
                // and drain the resulting promise before exiting.
                await context.EvalWithTopLevelAwaitAsync(code, file.FullName, context);
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
    }

}
