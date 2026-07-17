using BenchmarkDotNet.Attributes;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.BuiltIns.String;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Storage;

namespace Broiler.JavaScript.Engine.Benchmarks;

public class PropertyOperationBenchmarks
{
    private JSContext context;
    private JSObject ownObject;
    private JSObject prototypeObject;
    private JSObject inheritedObject;
    private JSValue proxyObject;
    private KeyString hitKey;
    private KeyString missKey;
    private JSValue hitValue;
    private JSValue missValue;
    private JSValue writeValue;

    [GlobalSetup]
    public void Setup()
    {
        context = BenchmarkContext.Create();
        hitKey = KeyStrings.GetOrCreate("hit");
        missKey = KeyStrings.GetOrCreate("miss");
        hitValue = new JSString("hit");
        missValue = new JSString("miss");
        writeValue = new JSNumber(1);

        ownObject = new JSObject();
        ownObject[hitKey] = writeValue;

        prototypeObject = new JSObject();
        prototypeObject[hitKey] = writeValue;
        inheritedObject = new JSObject { BasePrototypeObject = prototypeObject };

        proxyObject = context.Eval(
            "new Proxy({ hit: 1 }, { has: function (target, key) { return Reflect.has(target, key); } })",
            "benchmark-property-proxy.js");
    }

    [GlobalCleanup]
    public void Cleanup() => context?.Dispose();

    [Benchmark(Baseline = true)]
    public JSValue OwnGet()
        => ownObject[hitKey];

    [Benchmark]
    public JSValue OwnSet()
    {
        ownObject[hitKey] = writeValue;
        return ownObject;
    }

    [Benchmark]
    public JSValue OwnHas()
        => ownObject.HasProperty(hitValue);

    [Benchmark]
    public JSValue OwnMiss()
        => ownObject.HasProperty(missValue);

    [Benchmark]
    public JSValue PrototypeHas()
        => inheritedObject.HasProperty(hitValue);

    [Benchmark]
    public JSValue DescriptorRead()
        => ownObject.GetOwnPropertyDescriptor(hitValue);

    [Benchmark]
    public JSValue ProxyHas()
        => proxyObject.HasProperty(hitValue);
}
