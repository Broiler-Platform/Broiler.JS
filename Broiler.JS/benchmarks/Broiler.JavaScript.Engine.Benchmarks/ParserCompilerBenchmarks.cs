using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Broiler.JavaScript.Ast;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Parser;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Engine.Benchmarks;

public class ParserCompilerBenchmarks
{
    private static readonly IReadOnlyDictionary<string, string> Sources = new Dictionary<string, string>
    {
        ["Identifiers"] = "function calculate(alpha, beta, gamma) { let accumulatedValue = alpha; for (let currentIndex = 0; currentIndex < beta; currentIndex++) accumulatedValue += gamma + currentIndex; return accumulatedValue; } calculate(1, 100, 3);",
        ["Numbers"] = "var values = [0, 1, 1.5, 0xff, 0b101010, 0o755, 1_000_000, 6.022e23, .125, 9007199254740991]; values.reduce(function (a, b) { return a + b; }, 0);",
        ["StringsComments"] = "/* leading comment */ var message = 'alpha' + \"beta\"; // line comment\nvar template = `value:${message}`; template;",
        ["Switch"] = "function choose(value) { switch (value) { case 1: return 'one'; case 2: return 'two'; case 3: return 'three'; default: return 'other'; } } choose(3);",
        ["Locals"] = "function locals(a) { let b = a + 1, c = b + 1, d = c + 1, e = d + 1, f = e + 1, g = f + 1, h = g + 1; return h; } locals(1);",
    };

    private JSContext context;
    private string source;
    private JSFunctionDelegate precompiled;
    private Arguments executeArguments;

    [Params("Identifiers", "Numbers", "StringsComments", "Switch", "Locals")]
    public string Scenario { get; set; } = "Identifiers";

    [GlobalSetup]
    public void Setup()
    {
        context = BenchmarkContext.Create(new NoCodeCache());
        source = Sources[Scenario];
        precompiled = CoreScript.Compile(source, $"benchmark-{Scenario}.js", codeCache: new NoCodeCache());
        executeArguments = new Arguments(context);
    }

    [GlobalCleanup]
    public void Cleanup() => context?.Dispose();

    [Benchmark]
    public AstProgram ParseOnly()
    {
        var span = new StringSpan(source);
        return new Broiler.JavaScript.Parser.FastParser(new FastTokenStream(in span)).ParseProgram();
    }

    [Benchmark]
    public JSFunctionDelegate CompileOnly()
        => CoreScript.Compile(source, $"benchmark-{Scenario}.js", codeCache: new NoCodeCache());

    [Benchmark]
    public JSValue ExecutePrecompiled()
        => precompiled(in executeArguments);
}
