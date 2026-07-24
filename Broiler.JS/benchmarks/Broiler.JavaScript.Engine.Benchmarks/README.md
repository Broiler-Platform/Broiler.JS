# Broiler.JavaScript.Engine.Benchmarks

BenchmarkDotNet baseline for the Broiler.JS engine performance roadmap. Shared jobs,
repeatability thresholds, EventPipe workloads, and the machine-readable result format
are documented in `docs/performance.md`.

Run a focused smoke check:

```bash
$env:BROILER_BENCHMARK_PROFILE = "smoke"
dotnet run -c Release --project Broiler.JS/benchmarks/Broiler.JavaScript.Engine.Benchmarks -- --filter *ScriptEvaluationBenchmarks.EvalProductionCacheHit*
```

Run the repeatable collector (two benchmark launches plus fresh-process lifecycle
samples):

```bash
python scripts/performance/collect_phase0.py --profile smoke
```

Useful filters:

- `*ContextStartupBenchmarks*` for `JSContext` creation.
- `*ScriptEvaluationBenchmarks*` and `*CodeCacheBenchmarks*` for production structural
  cache hits, legacy key materialization, misses, and no-cache evaluation.
- `*FunctionCallBenchmarks*` for direct native/script, arity, strict/sloppy,
  same/cross-realm, recursive, callback, and tail-call invocation.
- `*PropertyOperationBenchmarks*` for direct own/prototype/Proxy get/set/has/descriptor paths.
- `*KeyMetadataBenchmarks*` for lock-free metadata reads, intern hits, and contended misses.
- `*ArrayPrimitiveBenchmarks*`, `*MapSetBenchmarks*`, and `*BinaryDataBenchmarks*` for
  direct collection and binary storage primitives.
- `*SparseMapBenchmarks*` for radix, hash, inline, segmented, and ordered sparse storage
  at 0/1/4/16/100/10k entries.
- `*ParserCompilerBenchmarks*` for parse-only, compile-only, and precompiled execution.
- `*ObjectAndArrayBenchmarks*` for property, enumeration, spread/rest, sparse, and callback paths.
- `*PromiseBenchmarks*` for promise callback dispatch through `Execute`.
- `*BuiltInHeavyBenchmarks*` for RegExp, Intl, Temporal, and Date loops.
- `*JIntSmokeBenchmarks*` for the repo-local JInt/Dromaeo smoke scripts.
- `*Phase5TieringBenchmarks*` for baseline versus promoted numeric reductions with
  numeric and mixed-type inputs; `*Phase5TaggedValueFeasibilityBenchmarks*` compares
  reference-backed `JSValue` scalar reads with the isolated eight-byte prototype.

Run `--sparse-metrics` against the built benchmark DLL to emit the Phase 2
construction-time and bytes-per-entry comparison as JSON.
