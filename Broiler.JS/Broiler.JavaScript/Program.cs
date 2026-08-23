using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
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
                "--script-host",
                "--module-host"
            };

            var scriptHostMode = args.Contains("--script-host");
            var moduleHostMode = args.Contains("--module-host");
            // `--preload FILE` runs FILE as a script before the main file, in the same realm.
            // A module's own declarations are module-scoped, so a test262 module test cannot be
            // handed its harness by concatenation the way a script test is: `assert` and `$DONE`
            // have to be GLOBALS before the module body runs, and a preloaded script is what
            // makes them that — leaving the module file itself unmodified, so its imports still
            // resolve against its own directory and its line numbers are the file's own.
            string preloadPath = null;
            var positionalList = new List<string>();
            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                if (string.Equals(arg, "--preload", StringComparison.Ordinal))
                {
                    if (++i >= args.Length)
                        throw new ArgumentException("--preload requires a file path");

                    preloadPath = args[i];
                    continue;
                }

                if (recognizedOptions.Contains(arg))
                    continue;

                positionalList.Add(arg);
            }

            var positionalArgs = positionalList.ToArray();
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
                RunScriptHostOnOwnThread(file);
                return;
            }

            if (moduleHostMode)
            {
                RunModuleHostOnOwnThread(file, inbuilt, preloadPath);
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

        // The shell runs JavaScript on a thread whose stack size it chooses, rather than on
        // whatever the runtime handed `Main` — 1 MiB on Windows, 8 MiB on a typical Linux, and
        // not knowable from managed code. Owning the number is what makes a reserve possible:
        // MaxStackUsageBytes trips "Maximum call stack size exceeded" once JavaScript has
        // consumed the budget, leaving the rest for the `catch` that handles it (see
        // CallFrameStack.StackUsageLimit). The split gives roughly 10k frames of recursion
        // before the throw — the same order as a browser — and a quarter of the stack after it.
        private const int ScriptHostStackBytes = 16 * 1024 * 1024;
        private const int ScriptHostStackReserveBytes = 4 * 1024 * 1024;

        private static void RunScriptHostOnOwnThread(FileInfo file)
            => RunHostOnOwnThread(() => RunScriptHost(file));

        private static void RunHostOnOwnThread(Action body)
        {
            ExceptionDispatchInfo failure = null;
            var thread = new Thread(
                () =>
                {
                    try
                    {
                        body();
                    }
                    catch (JSException ex)
                    {
                        // An uncaught JavaScript error is reported by NAME, on stderr, before
                        // anything else prints. The runtime's own unhandled-exception printer
                        // shows the .NET exception type and the error's message, and a
                        // SyntaxError raised while COMPILING carries no JavaScript stack to
                        // name it — so a `negative: type: SyntaxError` test failed on the
                        // diagnostic rather than on the behaviour, having rejected the program
                        // exactly as it should.
                        Console.Error.WriteLine(DescribeUncaughtError(ex));
                        Console.Error.WriteLine(ex.StackTrace);
                        Environment.ExitCode = 1;
                    }
                    catch (Exception ex)
                    {
                        // Anything that is not a JavaScript error is an engine or host failure
                        // and keeps the runtime's unhandled-exception path, with its original
                        // stack trace: it is not a test result and must not read as one.
                        failure = ExceptionDispatchInfo.Capture(ex);
                    }
                },
                ScriptHostStackBytes);

            thread.Start();
            thread.Join();
            failure?.Throw();
        }

        // "<name>: <message>" for an uncaught JavaScript error, e.g. "SyntaxError: Unexpected
        // token". Reading the properties can run JavaScript (a getter, or a subclass whose
        // `name` is an accessor), so a failure to describe the error falls back to the text the
        // exception rendered when it was thrown rather than replacing one diagnostic with another.
        private static string DescribeUncaughtError(JSException exception)
        {
            try
            {
                var error = exception.Error;
                var name = error[Broiler.JavaScript.Storage.KeyStrings.GetOrCreate("name")];
                var message = error[Broiler.JavaScript.Storage.KeyStrings.GetOrCreate("message")];
                var describedName = name.IsUndefined ? "Error" : name.ToString();
                var describedMessage = message.IsUndefined ? exception.Message : message.ToString();
                return string.IsNullOrEmpty(describedMessage)
                    ? $"Uncaught {describedName}"
                    : $"Uncaught {describedName}: {describedMessage}";
            }
            catch (Exception)
            {
                return $"Uncaught {exception.Message}";
            }
        }

        // The module goal on the same terms as the script host: one thread whose stack size the
        // shell owns, and the shell's own host globals. test262's `flags: [module]` tests are
        // ordinary tests of the module goal — `import`/`export`, module scope, top-level await —
        // and the only thing they need that a script does not is a host that loads them AS
        // modules. Without this mode they were not executed at all and were reported as skipped.
        private static void RunModuleHostOnOwnThread(FileInfo file, string inbuiltModules, string preloadPath)
            => RunHostOnOwnThread(() => RunModuleHost(file, inbuiltModules, preloadPath));

        private static void RunModuleHost(FileInfo file, string inbuiltModules, string preloadPath)
        {
            using var context = new BroilerJSContext(file.DirectoryName);
            DefineScriptHostGlobals(context);
            if (preloadPath != null)
            {
                var preload = new FileInfo(preloadPath);
                if (!preload.Exists)
                    throw new FileNotFoundException(preload.FullName);

                context.Eval(File.ReadAllText(preload.FullName), preload.FullName, context);
            }
            // A module body's own errors surface as a faulted task; unwrap it so the process
            // exits on the JavaScript error rather than on an AggregateException wrapper, which
            // is what a negative test's expected error type is matched against.
            context.RunAsync(
                    file.DirectoryName,
                    "./" + file.Name,
                    new[]
                    {
                        file.DirectoryName,
                        file.DirectoryName + "/node_modules",
                        inbuiltModules
                    })
                .GetAwaiter()
                .GetResult();
        }

        private static void RunScriptHost(FileInfo file)
        {
            using var sc = CreateSynchronizationContext();
            using var context = new JSContext(
                SynchronizationContext.Current,
                experimentalFeatures: JavaScriptFeatureFlags.AllExperimentalEs2026,
                options: new JSContextOptions
                {
                    ScriptHostMode = true,
                    MaxStackUsageBytes = ScriptHostStackBytes - ScriptHostStackReserveBytes,
                });
            DefineScriptHostGlobals(context);
            // Read synchronously: an `await` here could resume the rest of this method on a
            // thread-pool thread, whose stack is neither this size nor under our control, and
            // the budget would then be measured against the wrong stack.
            var code = File.ReadAllText(file.FullName);
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
                // Blocking is safe here: CreateSynchronizationContext installs the default
                // SynchronizationContext, whose Post goes to the thread pool rather than back
                // to this thread, so the continuation never waits on the thread awaiting it.
                context.EvalWithTopLevelAwaitAsync(code, file.FullName, context).GetAwaiter().GetResult();
            }
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

            Define262HostObject(context);
        }

        // test262's host object (INTERPRETING.md §"Host-Defined Functions"). Only the hooks this
        // host can answer honestly are defined. `agent` (multi-agent Atomics), `IsHTMLDDA` (the
        // [[IsHTMLDDA]] exotic object) and `AbstractModuleSource` are deliberately absent: a
        // shape-only stub would turn tests the runner currently EXCLUDES by name into tests that
        // partially succeed, which is the opposite of what a conformance run is for. The runner
        // reads the absence back — see HOST_262_CAPABILITIES in run_test262.py — and excludes
        // exactly the tests that ask for one of them.
        private static void Define262HostObject(JSContext context)
        {
            var host = new JSObject();

            // `global`: the global object of the realm this $262 belongs to. A JSContext IS its
            // own global object here, so the realm and its global are the same value.
            host[Broiler.JavaScript.Storage.KeyStrings.GetOrCreate("global")] = context;

            // `createRealm()`: a new realm, returned as ITS $262. The new context gets the same
            // options and the same host globals, so a test can run code in it the way it runs
            // code here — including creating a further realm from the one it was handed.
            host[Broiler.JavaScript.Storage.KeyStrings.GetOrCreate("createRealm")] = JSValue.CreateFunction(
                (in Arguments a) =>
                {
                    // Constructing a JSContext makes it the CURRENT realm, and the caller is
                    // still running in this one. Restoring the current context is what makes
                    // createRealm a call that RETURNS a realm rather than one that moves the
                    // caller into it: without it every later `globalThis` and every later
                    // global lookup in the calling script resolves against the new realm.
                    var outer = Broiler.JavaScript.Engine.Core.JSEngine.CurrentContext;
                    try
                    {
                        var realm = new JSContext(
                            SynchronizationContext.Current,
                            experimentalFeatures: JavaScriptFeatureFlags.AllExperimentalEs2026,
                            options: new JSContextOptions
                            {
                                ScriptHostMode = true,
                                MaxStackUsageBytes = ScriptHostStackBytes - ScriptHostStackReserveBytes,
                            });
                        // Kept alive for the life of the process: the realm's objects outlive
                        // this call — the test holds them — and disposing it here would tear
                        // down the globals under values the caller is still using.
                        CreatedRealms.Add(realm);
                        DefineScriptHostGlobals(realm);
                        return realm[Broiler.JavaScript.Storage.KeyStrings.GetOrCreate("$262")];
                    }
                    finally
                    {
                        if (outer != null)
                            Broiler.JavaScript.Engine.Core.JSEngine.CurrentContext = outer;
                    }
                },
                "createRealm",
                length: 0);

            // `detachArrayBuffer(buffer)`: DetachArrayBuffer on the argument. `transfer()` is
            // the same operation with a witness — ArrayBufferCopyAndDetach detaches the source —
            // so the buffer the test then reads is detached exactly as the abstract operation
            // leaves it, and the copy it returns is dropped.
            host[Broiler.JavaScript.Storage.KeyStrings.GetOrCreate("detachArrayBuffer")] = JSValue.CreateFunction(
                (in Arguments a) =>
                {
                    var buffer = a.Get1();
                    var transfer = buffer[Broiler.JavaScript.Storage.KeyStrings.GetOrCreate("transfer")];
                    if (transfer.IsUndefined)
                        throw Broiler.JavaScript.Engine.Core.JSEngine.NewTypeError(
                            "$262.detachArrayBuffer requires an ArrayBuffer");

                    transfer.InvokeFunction(new Arguments(buffer));
                    return JSUndefined.Value;
                },
                "detachArrayBuffer",
                length: 1);

            // `evalScript(source)`: evaluate the source as a SCRIPT in this realm's global scope,
            // returning its completion value and letting its errors propagate to the caller.
            host[Broiler.JavaScript.Storage.KeyStrings.GetOrCreate("evalScript")] = JSValue.CreateFunction(
                (in Arguments a) =>
                {
                    var source = a.Get1();
                    return context.Eval(source.ToString(), "$262.evalScript", context);
                },
                "evalScript",
                length: 1);

            // `gc()`: a collection the test can rely on having happened, which is what the
            // WeakRef/FinalizationRegistry tests ask for. Blocking on finalizers between the two
            // collections is what makes an unreachable object's cleanup observable to the code
            // that runs after the call.
            host[Broiler.JavaScript.Storage.KeyStrings.GetOrCreate("gc")] = JSValue.CreateFunction(
                static (in Arguments a) =>
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                    return JSUndefined.Value;
                },
                "gc",
                length: 0);

            context[Broiler.JavaScript.Storage.KeyStrings.GetOrCreate("$262")] = host;
        }

        // Realms created by $262.createRealm, held so their globals outlive the call.
        private static readonly List<JSContext> CreatedRealms = new();
    }

}
