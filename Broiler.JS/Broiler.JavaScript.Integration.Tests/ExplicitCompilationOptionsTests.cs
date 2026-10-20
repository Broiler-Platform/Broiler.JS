using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.FastParser.Compiler;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.ExpressionCompiler.Runtime;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Integration.Tests;

// The compiler is told the options it compiles under (milestone SJL-12.5 item 1; Broiler.VM's JSD-0080,
// decision D19): IJSCompiler.Compile(in StringSpan, in JSCompilationOptions, ...), whose default forwards a
// script without top-level await to a host's compiler and refuses anything else, naming the compiler.
public class ExplicitCompilationOptionsTests
{
    // A host's compiler that implements only the member IJSCompiler had before SJL-12.5.
    private sealed class ScriptOnlyCompiler : IJSCompiler
    {
        public int Calls { get; private set; }

        public BExpression<JSFunctionDelegate> Compile(in StringSpan code, string location = null, IList<string> argsList = null, ICodeCache codeCache = null)
        {
            Calls++;
            return null;
        }
    }

    [Fact]
    public void A_host_compiler_is_given_a_script_without_top_level_await()
    {
        var host = new ScriptOnlyCompiler();
        IJSCompiler compiler = host;
        compiler.Compile(new StringSpan("a;"), new JSCompilationOptions(ScriptHostMode: true, FeatureFlags: 3));
        Assert.Equal(1, host.Calls);
    }

    [Fact]
    public void A_host_compiler_is_not_given_a_module()
    {
        var host = new ScriptOnlyCompiler();
        IJSCompiler compiler = host;
        var refusal = Assert.Throws<NotSupportedException>(() => compiler.Compile(new StringSpan("a;"), new JSCompilationOptions(IsModule: true)));
        Assert.Contains(typeof(ScriptOnlyCompiler).FullName!, refusal.Message, StringComparison.Ordinal);
        Assert.Contains("a module", refusal.Message, StringComparison.Ordinal);
        Assert.Equal(0, host.Calls);
    }

    [Fact]
    public void A_host_compiler_is_not_given_top_level_await()
    {
        var host = new ScriptOnlyCompiler();
        IJSCompiler compiler = host;
        var refusal = Assert.Throws<NotSupportedException>(() => compiler.Compile(new StringSpan("a;"), new JSCompilationOptions { AllowTopLevelAwait = true }));
        Assert.Contains(typeof(ScriptOnlyCompiler).FullName!, refusal.Message, StringComparison.Ordinal);
        Assert.Contains("top-level await", refusal.Message, StringComparison.Ordinal);
        Assert.Equal(0, host.Calls);
    }

    [Fact]
    public void The_default_compiler_compiles_under_the_options_it_is_given()
    {
        using var context = new JSContext();
        IJSCompiler compiler = new DefaultJSCompiler();

        Assert.NotNull(compiler.Compile(new StringSpan("await 1;"), new JSCompilationOptions { AllowTopLevelAwait = true }));
        Assert.Throws<FastParseException>(() => compiler.Compile(new StringSpan("await 1;"), default(JSCompilationOptions)));
        Assert.Throws<FastParseException>(() => compiler.Compile(new StringSpan("await 1;")));

        Assert.NotNull(compiler.Compile(new StringSpan("export const a = 1;"), new JSCompilationOptions(IsModule: true) { AllowTopLevelAwait = true }));
        Assert.Throws<FastParseException>(() => compiler.Compile(new StringSpan("export const a = 1;"), default(JSCompilationOptions)));
    }

    [Fact]
    public void The_top_level_await_permission_reaches_the_compile_through_the_options()
    {
        using var context = new JSContext();
        var options = context.CompilationOptions with { AllowTopLevelAwait = true };
        Assert.NotNull(CoreScript.Compile(new StringSpan("await 1;"), "options.js", codeCache: new NoCache(), compilationOptions: options));
    }

    [Fact]
    public void The_permission_is_in_the_code_caches_key()
    {
        var script = new JSCompilationOptions();
        var permitted = script with { AllowTopLevelAwait = true };
        Assert.NotEqual(script, permitted);
        Assert.Contains("AllowTopLevelAwait = False", new JSCode("a.js", new StringSpan("a"), null, null, script).Key, StringComparison.Ordinal);
        Assert.Contains("AllowTopLevelAwait = True", new JSCode("a.js", new StringSpan("a"), null, null, permitted).Key, StringComparison.Ordinal);
    }

    // Compiles what it is given and keeps nothing.
    private sealed class NoCache : ICodeCache
    {
        public JSFunctionDelegate GetOrCreate(in JSCode code) => code.Compiler().CompileWithNestedLambdas();
    }
}
