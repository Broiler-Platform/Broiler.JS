using System.Diagnostics;
using System.Text;

namespace Broiler.JavaScript.Vectors.Tests.Runners;

/// <summary>
/// Broiler.JS's engine-level runner. Each vector is evaluated by <see cref="BroilerJsRealm"/> in a
/// child process of this assembly, one script per line over a pipe, so that a vector the engine
/// never finishes is ended by ending the process.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why a child process.</b> The engine has no interrupt, and a conversion that loops in
/// BigInteger arithmetic (parseFloat of a significand beyond the double range, which steps one
/// unit in the last place at a time) never reaches a point where a thread can be aborted:
/// <c>ControlledExecution</c> was tried and its canceller spun for as long as the run was left.
/// Ending the process is the one way to end such an evaluation, and it costs nothing for the
/// vectors that complete, which share one process.
/// </para>
/// <para>
/// A vector that is still running at <see cref="Limit"/> is rendered <c>abort: </c>; the process
/// is ended and the next vector starts a new one. Every vector still gets a fresh realm of its own,
/// in whichever process evaluates it.
/// </para>
/// </remarks>
public sealed class BroilerJsEngine : IVectorEngine, IDisposable
{
    /// <summary>The argument that makes this assembly's entry point a vector evaluator.</summary>
    public const string ChildSwitch = "--evaluate-vectors";

    /// <summary>
    /// How long one vector may run. The corpus's slowest vectors complete in well under a second, so
    /// only an evaluation that does not terminate reaches it.
    /// </summary>
    public static readonly TimeSpan Limit = TimeSpan.FromSeconds(5);

    private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false);

    private readonly object gate = new();
    private Process? child;
    private string? childPrelude;
    private string? preludeFile;

    public string Name => "broiler-js";

    public string Evaluate(string prelude, string script)
    {
        if (script.Contains('\n') || script.Contains('\r'))
            return BroilerJsRealm.Fault("a script must be one line");

        lock (gate)
        {
            var process = Child(prelude);
            process.StandardInput.WriteLine(script);
            process.StandardInput.Flush();
            var answer = process.StandardOutput.ReadLineAsync();
            if (!answer.Wait(Limit))
            {
                Stop();
                return $"abort: no completion within {Limit.TotalSeconds:0} s";
            }

            if (answer.Result is { } rendered)
                return rendered;

            process.WaitForExit();
            var code = process.ExitCode;
            Stop();
            return $"abort: the evaluating process ended with exit code {code}";
        }
    }

    public void Dispose()
    {
        lock (gate)
            Stop();
    }

    private Process Child(string prelude)
    {
        if (child is { HasExited: false } && childPrelude == prelude)
            return child;

        Stop();
        preludeFile = Path.GetTempFileName();
        File.WriteAllText(preludeFile, prelude, Utf8);

        var assembly = typeof(BroilerJsEngine).Assembly.Location;
        var host = Environment.ProcessPath is { } path && Path.GetFileNameWithoutExtension(path) == "dotnet" ? path : "dotnet";
        var start = new ProcessStartInfo(host)
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = false,
            UseShellExecute = false,
            StandardInputEncoding = Utf8,
            StandardOutputEncoding = Utf8,
        };
        start.ArgumentList.Add("exec");
        start.ArgumentList.Add(assembly);
        start.ArgumentList.Add(ChildSwitch);
        start.ArgumentList.Add(preludeFile);

        child = Process.Start(start) ?? throw new InvalidOperationException("the evaluating process did not start");
        childPrelude = prelude;
        return child;
    }

    private void Stop()
    {
        if (child is not null)
        {
            try
            {
                if (!child.HasExited)
                    child.Kill(entireProcessTree: true);
                child.WaitForExit();
            }
            catch (InvalidOperationException)
            {
                // It ended between the check and the kill.
            }

            child.Dispose();
            child = null;
            childPrelude = null;
        }

        if (preludeFile is not null)
        {
            File.Delete(preludeFile);
            preludeFile = null;
        }
    }

    /// <summary>
    /// The child's loop: the prelude from the file named on the command line, then one script per
    /// line of standard input, each answered by one rendered outcome per line of standard output.
    /// </summary>
    public static int Serve(string preludePath)
    {
        var prelude = File.ReadAllText(preludePath, Utf8);
        // Standard output is the answer channel, so nothing else may write to it.
        Console.SetOut(TextWriter.Null);
        using var input = new StreamReader(Console.OpenStandardInput(), Utf8);
        using var output = new StreamWriter(Console.OpenStandardOutput(), Utf8) { AutoFlush = true, NewLine = "\n" };
        while (input.ReadLine() is { } script)
            output.WriteLine(BroilerJsRealm.Evaluate(prelude, script));
        return 0;
    }
}
