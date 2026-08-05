using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Engine.Benchmarks;

internal static class BenchmarkContext
{
    /// <summary>
    /// The stack the shell gives JavaScript, and the reserve it keeps back — copied from
    /// <c>Broiler.JavaScript/Program.cs</c> rather than invented, because the point is to measure
    /// on the configuration that ships.
    /// </summary>
    /// <remarks>
    /// <b>Item 0-2 is a property of the shell, and every benchmark host here lacked it.</b> The
    /// shell runs JavaScript on a thread whose stack size it chooses and sets
    /// <c>MaxStackUsageBytes</c> so deep recursion raises a catchable <em>"Maximum call stack size
    /// exceeded"</em> instead of aborting the process. A census host running on whatever the
    /// runtime handed <c>Main</c> has neither, and Mandreel's <c>global_init</c> takes it down
    /// with an uncatchable .NET stack overflow — which is what kept Mandreel out of every census
    /// in this campaign, silently and without anyone writing down why.
    /// </remarks>
    public const int ScriptHostStackBytes = 16 * 1024 * 1024;

    public const int ScriptHostStackReserveBytes = 4 * 1024 * 1024;

    public static JSContext Create(ICodeCache codeCache = null, bool scriptHostStackBudget = false)
    {
        var context = scriptHostStackBudget
            ? new JSContext(
                experimentalFeatures: JavaScriptFeatureFlags.AllExperimentalEs2026,
                options: new JSContextOptions
                {
                    MaxStackUsageBytes = ScriptHostStackBytes - ScriptHostStackReserveBytes,
                })
            : new JSContext(experimentalFeatures: JavaScriptFeatureFlags.AllExperimentalEs2026);

        if (codeCache != null)
            context.CodeCache = codeCache;
        return context;
    }

    /// <summary>
    /// Runs <paramref name="body"/> on a thread with the shell's stack size, rethrowing on the
    /// caller's thread so nothing about the failure path changes.
    /// </summary>
    /// <remarks>
    /// Pair this with <c>scriptHostStackBudget: true</c>. The budget alone is not enough — it is
    /// measured against the stack the code is standing on, and a guard sized against a stack
    /// larger than the real one can never fire, which is the defect §3.5 records under
    /// <em>"a threshold larger than the resource it guards is not a guard"</em>.
    /// </remarks>
    public static void RunOnScriptHostStack(Action body)
    {
        ExceptionDispatchInfo failure = null;
        var thread = new Thread(
            () =>
            {
                try
                {
                    body();
                }
                catch (Exception ex)
                {
                    failure = ExceptionDispatchInfo.Capture(ex);
                }
            },
            ScriptHostStackBytes);

        thread.Start();
        thread.Join();
        failure?.Throw();
    }
}
