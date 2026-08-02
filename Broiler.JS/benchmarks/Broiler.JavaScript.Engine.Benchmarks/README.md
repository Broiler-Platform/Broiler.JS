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

## The execution-roadmap probe corpus

`HotPathProbeBenchmarks` is the permanent home of the Appendix A probes from
`docs/performance-roadmap.md` — the scenarios every P0/P1/P2/P3 figure in that document
was measured on. They lived in an ad-hoc harness outside the repository, which is why
none of those numbers is acceptance evidence; run these to reproduce or re-check them.

```bash
$env:BROILER_BENCHMARK_PROFILE = "baseline"
dotnet run -c Release --project Broiler.JS/benchmarks/Broiler.JavaScript.Engine.Benchmarks -- --filter *HotPathProbeBenchmarks*
```

Iteration counts are Appendix A's and are load-bearing: a probe run at a different count
is not comparable to the figure it is being checked against. `LoopEmpty` is the floor the
other per-iteration numbers are quoted against, so it is the benchmark baseline.

Phase P1 is measured by cache **hit rate** rather than wall clock, so it has its own
emitter rather than a benchmark. It reports every site in the roadmap's phase C table as
JSON, each run cold in a fresh context:

```bash
dotnet run -c Release --project Broiler.JS/benchmarks/Broiler.JavaScript.Engine.Benchmarks -- --cache-metrics
```

A monomorphic site should report one miss per 200 000 reads. A site reporting 200 000
misses is the pre-P1 defect, and dictionary fallbacks above single digits is the pre-P0-3
one.

Useful filters:

- `*HotPathProbeBenchmarks*` for the Appendix A corpus above: loop floor, arithmetic,
  own/class property read and write, sloppy/strict/closure/prototype/built-in calls,
  array read-write, object allocation, push, and string concatenation.
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
construction-time and bytes-per-entry comparison as JSON, and `--cache-metrics` for the
property inline-cache hit rates described above.

The other standing emitters report a quantity no wall-clock benchmark does, each in the
same JSON shape — allocated bytes measured after a forced gen2 collection, warmed first,
and reported net of a control that carries the same loop without the thing under test:

| Emitter | Reports | Sized which roadmap item |
|---|---|---|
| `--object-alloc` | bytes per object, by shape | 2-3, 2-7, 2-9 |
| `--element-alloc` | bytes per array element, write and read separately | 3-0, 3-1 |
| `--local-alloc` | bytes per iteration for each place a value can live — a top-level `var`, a parameter, a `let`, a `const`, a block `var` — plus the compiler's own count of how many bindings it kept scalar | 3-3 |
| `--regex-profile` | nanoseconds and bytes per subject character for nine regex shapes, plus real Octane patterns through `System.Text.RegularExpressions` with and without `RegexOptions.Compiled` | phase 5 |
| `--property-map-distribution <octane-dir>` | the final node-group count of every property map over an Octane run | 2-7, 2-9 |

`--local-alloc`'s two columns answer different questions and both are needed: the counter
is exact and says whether a shape is *eligible*, the bytes say what that eligibility is
*worth*. A site reporting zero scalar bindings and the same bytes as an eligible one is an
item with no prize; the pair is the only way to tell that from a probe that never reached
the change.
