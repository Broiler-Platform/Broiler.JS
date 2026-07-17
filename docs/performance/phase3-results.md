# Phase 3 compiler specialization

Phase 3 adds guarded compiler/runtime specialization while retaining the existing
ECMAScript lookup, descriptor, Proxy, and comparison paths as correctness fallbacks.
Every specialization exposes a diagnostic counter so tests and benchmark runs can
confirm which path was selected without relying on timing alone.

## Implementation matrix

| Delivery item | Implementation |
| --- | --- |
| Noncaptured local scalar replacement | Eligible function-scoped `var` bindings use direct `JSValue` IL locals instead of `JSVariable` cells. Async/generator functions and functions containing direct eval, nested closures, `with`, or `debugger` retain cells. Lexical/TDZ bindings and `arguments`/`eval` also remain on their semantic paths. |
| Integer switch dispatch | Four or more integer cases use `OpCodes.Switch` when the range is at most 256 entries and at least 50% occupied. Sparse, small, mixed-type, fractional, and out-of-range inputs retain strict comparison dispatch. |
| String switch dispatch | Four through 256 constant string cases use the stable ordinal hash, a power-of-two bucket table, and ordinal collision checks. Duplicate cases preserve first-match behavior; small and nonconstant switches retain the comparison emitter. |
| Ordinary-object shapes and slots | Exact ordinary `JSObject` instances follow immutable shared transitions for default public data properties and mirror values into contiguous slots. Deletes, accessors, private names, unusual descriptors, exposed mutable storage, and non-ordinary subclasses switch permanently to dictionary mode. |
| Property inline caches | Constant named reads receive an integer cache-site ID. A site caches one shape, promotes once to a maximum of four shapes, then becomes permanently megamorphic. Shape mismatch, inherited properties, dictionary objects, Proxy/exotic receivers, private names, and writes use the generic index path. |
| Prototype invalidation | Named/indexed prototype mutation and prototype-chain replacement advance a process-wide version and invalidation counter. The current own-data cache does not assume prototype state, but the version is available for later inherited-property and array guards. |
| Persistent metadata/PDB backend | The optional assembly cache uses incremental SHA-256 keys, a schema-3 manifest commit marker, atomic PE/PDB/manifest renames, SHA-256 integrity validation, corrupt-entry quarantine, a valid portable PDB document, collectible `AssemblyLoadContext` loading, and `CreateDelegate` instead of reflection invocation. Raw source is no longer persisted as a sidecar. |

## Explicit budgets and fallback rules

| Area | Budget | Fallback |
| --- | ---: | --- |
| Dense integer table | 256 slots; minimum 50% occupancy | Linear strict comparison emitter |
| String hash table | 4–256 buckets, power of two | Linear hash/equality emitter |
| Property cache | 4 receiver shapes per site | Permanent generic/megamorphic lookup |
| Property cache site table | 65,536 emitted sites per process | Generic lookup (`site == -1`) |
| Scalar local | `JSValue` only in Phase 3 | `JSVariable` cell |

The switch benchmarks cover 4, 16, 64, and 256 cases for integer/string hits and
misses. The property benchmark compares monomorphic, four-shape polymorphic, and Proxy
generic reads; the local benchmark exercises a multi-local loop. Run the smoke matrix
with:

```powershell
$env:BROILER_BENCHMARK_PROFILE = "smoke"
dotnet run --project Broiler.JS/benchmarks/Broiler.JavaScript.Engine.Benchmarks `
  -c Release -- --filter "*SwitchDispatchBenchmarks*" "*ScalarAndPropertySpecializationBenchmarks*"
```

The local Windows x64/.NET 10 smoke run completed all 20 jobs. Three-iteration smoke
figures validate scaling and wiring; they are not a release comparison:

| Cases | Integer hit | Integer miss | String hit | String miss |
| ---: | ---: | ---: | ---: | ---: |
| 4 | 436 ns | 416 ns | 376 ns | 399 ns |
| 16 | 389 ns | 376 ns | 422 ns | 435 ns |
| 64 | 400 ns | 387 ns | 426 ns | 397 ns |
| 256 | 446 ns | 465 ns | 655 ns | 695 ns |

The same run measured the 128-iteration scalar loop at 10.7 us, monomorphic property
loop at 7.7 us, four-shape loop at 13.5 us, and Proxy generic loop at 89.2 us. The Proxy
result intentionally includes observable trap and `Reflect.get` work.

## Verification

- Focused Phase 3 semantics, invalidation, dispatch-budget, write-boundary,
  direct-eval/closure-guard, and persistent-cache tests: 8/8 passed.
- The persistent-cache test verifies a cold cache hit from a second collectible load
  context, parses the emitted portable PDB, corrupts the PE, and verifies quarantine
  plus successful recompilation.
- Phase 0–3 architecture checks: 20/20 passed.
- Compiler tests: 245/245 passed.
- Runtime and storage tests: 9/9 and 11/11 passed.
- Built-ins tests: 1,899/1,900 passed. The sole failure is the independently
  reproducible CLDR alias expectation `ru-Armn-AM` versus current data result
  `ru-Armn-RU`, unchanged from Phase 2.
- Integration tests: 4,475/4,483 passed. The eight existing environment/data/path
  failures are timezone/locale expectations and fixed-path/documentation fixtures;
  the 51-test Phase 3 regression cluster passes.
- Benchmark project Release build: zero warnings and zero errors; all 20 Phase 3 smoke
  jobs completed.
- Architecture checks pin the scalar guards, switch budgets, shape/PIC bound, prototype
  versioning, and manifest/atomic/PDB backend so later refactors cannot silently remove
  the fallback invariants.

The controlled two-run BenchmarkDotNet baseline and the pinned full supported test262
matrix remain release evidence rather than claims in this implementation note. The
scheduled workflow runs the focused Phase 3 gate on Linux; full test262 continues
through the repository's dedicated sharded compliance workflow.
