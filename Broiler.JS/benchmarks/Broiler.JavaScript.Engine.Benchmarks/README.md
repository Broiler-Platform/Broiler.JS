# Broiler.JavaScript.Engine.Benchmarks

BenchmarkDotNet baseline for the Broiler.JS engine performance roadmap.

Run a focused smoke check:

```bash
dotnet run -c Release --project Broiler.JS/benchmarks/Broiler.JavaScript.Engine.Benchmarks -- --filter *ScriptEvaluationBenchmarks.EvalCacheHit*
```

Run all short baselines:

```bash
dotnet run -c Release --project Broiler.JS/benchmarks/Broiler.JavaScript.Engine.Benchmarks
```

Useful filters:

- `*ContextStartupBenchmarks*` for `JSContext` creation.
- `*ScriptEvaluationBenchmarks*` for cache-hit and no-cache evaluation.
- `*ObjectAndArrayBenchmarks*` for property, enumeration, spread/rest, sparse, and callback paths.
- `*PromiseBenchmarks*` for promise callback dispatch through `Execute`.
- `*BuiltInHeavyBenchmarks*` for RegExp, Intl, Temporal, and Date loops.
- `*JIntSmokeBenchmarks*` for the repo-local JInt/Dromaeo smoke scripts.
