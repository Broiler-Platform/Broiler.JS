using Broiler.JavaScript.Vectors.Tests.Runners;

namespace Broiler.JavaScript.Vectors.Tests;

/// <summary>
/// The test assembly's entry point. The test host never calls it; <see cref="BroilerJsEngine"/>
/// starts this assembly as a child process with <see cref="BroilerJsEngine.ChildSwitch"/> to
/// evaluate vectors.
/// </summary>
public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length == 2 && args[0] == BroilerJsEngine.ChildSwitch)
            return BroilerJsEngine.Serve(args[1]);

        Console.Error.WriteLine($"usage: {BroilerJsEngine.ChildSwitch} <prelude.js>, with one script per line on standard input");
        return 2;
    }
}
