using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Runs one explicit test of this assembly in a child process under invariant globalization (milestone SJL-9.5): .NET reads
// DOTNET_SYSTEM_GLOBALIZATION_INVARIANT once, when a process starts, so a test of the providers in that mode must run in a
// process started with it. The child is this test assembly, run by its own xUnit runner with the one method selected and
// explicit tests admitted; the child's test reads IsChild to know it runs there, and asserts that the mode is on.
internal static class InvariantGlobalizationChild
{
    private const string Switch = "BROILER_BUILTINS_TESTS_INVARIANT_CHILD";

    // Whether this process is such a child.
    internal static bool IsChild => Environment.GetEnvironmentVariable(Switch) == "1";

    // Runs the explicit test `method` (fully qualified) in a child under invariant globalization; fails unless the child
    // ran exactly that one test and it passed.
    internal static void Run(string method)
    {
        var assembly = typeof(InvariantGlobalizationChild).Assembly.Location;
        var host = Environment.ProcessPath is { } path && Path.GetFileNameWithoutExtension(path) == "dotnet" ? path : "dotnet";
        var start = new ProcessStartInfo(host)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        start.ArgumentList.Add("exec");
        start.ArgumentList.Add(assembly);
        start.ArgumentList.Add("-method");
        start.ArgumentList.Add(method);
        start.ArgumentList.Add("-explicit");
        start.ArgumentList.Add("on");
        start.ArgumentList.Add("-noLogo");
        start.ArgumentList.Add("-noColor");
        start.Environment["DOTNET_SYSTEM_GLOBALIZATION_INVARIANT"] = "1";
        start.Environment[Switch] = "1";

        using var process = Process.Start(start) ?? throw new InvalidOperationException("the child test process did not start");
        var error = process.StandardError.ReadToEndAsync();
        var output = process.StandardOutput.ReadToEnd();
        if (!process.WaitForExit(TimeSpan.FromMinutes(5)))
        {
            process.Kill(entireProcessTree: true);
            Assert.Fail("the child test process did not finish within five minutes:\n" + output);
        }

        var text = output + error.Result;
        Assert.True(process.ExitCode == 0, $"the child test process exited with {process.ExitCode}:\n{text}");
        Assert.Contains("Total: 1, Errors: 0, Failed: 0, Skipped: 0", text);
    }
}
