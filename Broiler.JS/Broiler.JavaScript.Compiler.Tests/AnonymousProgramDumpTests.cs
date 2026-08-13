using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Compiler.Tests;

// Dumping the source of programs that have no script of their own.
//
// Numbering them made a trace attributable — frames say vm16.js rather than every one saying
// vm.js — but a name is only half of it. A frame naming vm16.js still does not say what vm16.js
// is, and a payload a loader evaluated exists nowhere on disk to go and look at. These cover the
// half that closes: the program reaches a file named exactly what the frames call it.
public class AnonymousProgramDumpTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "broiler-jsdump-" + Guid.NewGuid().ToString("n"));
    private readonly string previous = AnonymousProgramDump.Directory;

    public void Dispose()
    {
        AnonymousProgramDump.Directory = previous;
        try
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
        catch (IOException)
        {
            // A leftover temp directory is not worth failing a test over.
        }
    }

    // The reported shape: one program defines a wrapper, another calls it, and the failing
    // identifier is inside the wrapper. Both programs have to land on disk under the names their
    // frames use, because that correspondence is the whole point.
    [Fact]
    public void EvaluatedPrograms_AreWrittenUnderTheNameTheirFramesUse()
    {
        AnonymousProgramDump.Directory = root;
        using var context = new JSContext();

        var stack = context.Eval(
            "(function(){ try {" +
            " globalThis.NS = {};" +
            " (0,eval)('globalThis.NS.mod = (function(_){ return function(){ return nosuchvalue; }; });');" +
            " (0,eval)('(function(){ return globalThis.NS.mod(1)(); })()');" +
            " } catch (e) { return e.stack; } })()",
            "outer.js").ToString();

        // Read the expectation off the trace rather than off the directory: the dump switch is
        // process-global, so a test class running in parallel drops its own programs in here too.
        // What matters is that every program THIS trace names has a file holding its source.
        var named = System.Text.RegularExpressions.Regex.Matches(stack, @"vm\d+\.js")
            .Select(m => m.Value)
            .Distinct()
            .ToArray();

        Assert.True(named.Length >= 2, $"expected the trace to name at least two programs: {stack}");

        var sources = new List<string>();
        foreach (var name in named)
        {
            var path = Path.Combine(root, name);
            Assert.True(File.Exists(path), $"{name} is named by the trace but was not written");
            var source = File.ReadAllText(path);
            Assert.NotEmpty(source);
            sources.Add(source);
        }

        // The wrapper's own source is recoverable, which is what makes an unresolved identifier
        // inside it readable at all.
        Assert.Contains(sources, s => s.Contains("nosuchvalue"));
    }

    // Off unless asked for: page script is page content, and writing it on every render is not
    // something a diagnostic should decide on a page's behalf.
    [Fact]
    public void NothingIsWritten_WhenNoDirectoryIsConfigured()
    {
        AnonymousProgramDump.Directory = string.Empty;
        Assert.False(AnonymousProgramDump.Enabled);

        using var context = new JSContext();
        context.Eval("(0,eval)('1 + 1');", "outer.js");

        Assert.False(Directory.Exists(root));
    }

    // A script that has a name of its own is not an anonymous program and is not dumped.
    [Fact]
    public void ANamedScript_IsNotDumped()
    {
        AnonymousProgramDump.Directory = root;
        using var context = new JSContext();

        context.Eval("var dumpProbe = 1;", "inline-3");

        var written = Directory.Exists(root) ? Directory.GetFiles(root).Select(Path.GetFileName).ToArray() : [];
        Assert.DoesNotContain("inline-3", written);
    }

    // Writing must never be able to break the execution it observes.
    [Fact]
    public void AnUnwritableDirectory_DoesNotDisturbExecution()
    {
        AnonymousProgramDump.Directory = "\0:/nowhere/that/can/exist";
        using var context = new JSContext();

        Assert.Equal("2", context.Eval("String((0,eval)('1 + 1'))", "outer.js").ToString());
    }
}
