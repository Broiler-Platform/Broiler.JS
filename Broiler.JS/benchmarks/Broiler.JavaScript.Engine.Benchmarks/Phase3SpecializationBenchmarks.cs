using System.Text;
using BenchmarkDotNet.Attributes;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.BuiltIns.String;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Engine.Benchmarks;

/// <summary>
/// Dispatch scaling and code-size breakpoints for Phase 3. The 4/16/64/256
/// matrix deliberately includes the largest table the compiler will emit.
/// </summary>
public class SwitchDispatchBenchmarks
{
    private JSContext context;
    private JSValue integerSwitch;
    private JSValue stringSwitch;
    private Arguments integerHit;
    private Arguments integerMiss;
    private Arguments stringHit;
    private Arguments stringMiss;

    [Params(4, 16, 64, 256)]
    public int Cases { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        context = BenchmarkContext.Create(new NoCodeCache());
        integerSwitch = context.Eval(BuildSwitch(Cases, strings: false), $"phase3-int-switch-{Cases}.js");
        stringSwitch = context.Eval(BuildSwitch(Cases, strings: true), $"phase3-string-switch-{Cases}.js");
        integerHit = new Arguments(JSUndefined.Value, new JSNumber(Cases - 1));
        integerMiss = new Arguments(JSUndefined.Value, new JSNumber(Cases + 1));
        stringHit = new Arguments(JSUndefined.Value, new JSString("key" + (Cases - 1)));
        stringMiss = new Arguments(JSUndefined.Value, new JSString("missing"));
    }

    [GlobalCleanup]
    public void Cleanup() => context?.Dispose();

    [Benchmark(Baseline = true)]
    public JSValue IntegerHit() => integerSwitch.InvokeFunction(in integerHit);

    [Benchmark]
    public JSValue IntegerMiss() => integerSwitch.InvokeFunction(in integerMiss);

    [Benchmark]
    public JSValue StringHit() => stringSwitch.InvokeFunction(in stringHit);

    [Benchmark]
    public JSValue StringMiss() => stringSwitch.InvokeFunction(in stringMiss);

    private static string BuildSwitch(int count, bool strings)
    {
        var source = new StringBuilder("(function (value) { switch (value) {");
        for (var i = 0; i < count; i++)
        {
            var label = strings ? $"'key{i}'" : i.ToString(System.Globalization.CultureInfo.InvariantCulture);
            source.Append("case ").Append(label).Append(": return ").Append(i).Append(';');
        }
        return source.Append("default: return -1; } })").ToString();
    }
}

/// <summary>Hot-path comparison for raw locals and mono/poly/generic property reads.</summary>
public class ScalarAndPropertySpecializationBenchmarks
{
    private JSContext context;
    private JSValue scalarLocals;
    private JSValue monomorphicProperty;
    private JSValue polymorphicProperty;
    private JSValue proxyProperty;
    private Arguments noArguments;
    private Arguments scalarArguments;

    [GlobalSetup]
    public void Setup()
    {
        context = BenchmarkContext.Create(new NoCodeCache());
        scalarLocals = context.Eval("""
            (function (n) {
              var a = 1, b = 2, c = 3, d = 4;
              for (var i = 0; i < n; i++) { a += b; b += c; c += d; d += 1; }
              return a + b + c + d;
            })
            """, "phase3-scalar-locals.js");
        monomorphicProperty = context.Eval("""
            (function () {
              var o = { x: 1 };
              return function () { var total = 0; for (var i = 0; i < 128; i++) total += o.x; return total; };
            })()
            """, "phase3-monomorphic-property.js");
        polymorphicProperty = context.Eval("""
            (function () {
              var objects = [{x:1}, {a:0,x:2}, {a:0,b:0,x:3}, {a:0,b:0,c:0,x:4}];
              return function () { var total = 0; for (var i = 0; i < 128; i++) total += objects[i & 3].x; return total; };
            })()
            """, "phase3-polymorphic-property.js");
        proxyProperty = context.Eval("""
            (function () {
              var o = new Proxy({x:1}, {get:function(t,k){return Reflect.get(t,k);}});
              return function () { var total = 0; for (var i = 0; i < 128; i++) total += o.x; return total; };
            })()
            """, "phase3-proxy-property.js");
        noArguments = new Arguments(JSUndefined.Value);
        scalarArguments = new Arguments(JSUndefined.Value, new JSNumber(128));
    }

    [GlobalCleanup]
    public void Cleanup() => context?.Dispose();

    [Benchmark(Baseline = true)]
    public JSValue ScalarLocals() => scalarLocals.InvokeFunction(in scalarArguments);

    [Benchmark]
    public JSValue MonomorphicProperty() => monomorphicProperty.InvokeFunction(in noArguments);

    [Benchmark]
    public JSValue PolymorphicProperty() => polymorphicProperty.InvokeFunction(in noArguments);

    [Benchmark]
    public JSValue ProxyGenericProperty() => proxyProperty.InvokeFunction(in noArguments);
}
