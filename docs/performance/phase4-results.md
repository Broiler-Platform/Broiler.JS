# Phase 4 startup and packaging

Phase 4 replaces implicit startup work with generated feature metadata, per-realm lazy
data-property cells, and an explicit bootstrap API. The default full profile retains
the supported global surface while deferring Intl and Temporal construction. The named
minimal profile is intentionally non-conformant and omits those globals.

## Implementation matrix

| Delivery item | Implementation |
| --- | --- |
| Generated manifests/descriptors | The registration generator classifies Core, Intl, and Temporal registrations and emits immutable `BuiltInRegistrationDescriptor` entries plus a feature-filtered registrar. `DefaultBuiltInRegistry` publishes a semantic-versioned `BuiltInManifest`; composite registries merge explicit satellite manifests without discovery. |
| Spec-correct lazy data properties | `LazyDataPropertyCell` stores only a core-owned feature ID and the realm resolver. It realizes once, caches failures, rejects recursion deterministically, and can be canceled by delete/redefinition. Property reads, descriptor-value retrieval, copy/spread, indexed access, and partial descriptor updates all use the normal data-property path. |
| Lazy Intl/Temporal pilot | Full bootstrap installs the generated keys in their original order, then replaces the placeholders with lazy cells. Key enumeration does not realize them. Eager and lazy full profiles expose identical global key order and descriptors; mutable constructors/prototypes remain realm-local. |
| Explicit bootstrap/profile API | `JavaScriptBootstrap`, `JavaScriptContextBuilder`, `JavaScriptBootstrapProfile`, and per-context registry options make the selected feature graph visible. Explicit hosts perform zero compatibility assembly probes. The magic-name loader remains only as a delayed legacy adapter. |
| Feature-satellite prototype | `Broiler.JavaScript.Feature.Sample` references Runtime only and has no module initializer. The full host composes a core-owned registration descriptor with a deferred, non-inlined factory, so its lazy `sampleFeature` cell loads the satellite assembly only on realization. `Broiler.JavaScript.Minimal` is a dependency-only package that excludes CLR, Debugger, Modules, and the sample satellite. |
| Publish samples and reports | `Broiler.JavaScript.StartupHost` has conditional full/minimal graphs and emits process, context, allocation, working-set, loaded-assembly, and feature-resolution data. `collect_phase4.py` publishes framework-dependent minimal/full, full ReadyToRun, and trimmed self-contained variants. |

The source-generator project treats host deployment properties as local and resets
trimming, AOT, ReadyToRun, RID, and self-contained settings. This prevents publish
properties from being incorrectly applied to the compiler-hosted `netstandard2.0`
analyzer.

## Context benchmark

The local Windows x64/.NET 10 BenchmarkDotNet smoke job used three warmups and three
measurement iterations. It is directional implementation evidence, not a release
baseline:

| Scenario | Mean | Managed allocation | Relative to eager time | Relative allocation |
| --- | ---: | ---: | ---: | ---: |
| Full eager context | 535.3 us | 2.22 MB | 1.00 | 1.00 |
| Full lazy context | 445.4 us | 1.73 MB | 0.84 | 0.78 |
| Minimal context | 299.5 us | 1.26 MB | 0.56 | 0.57 |
| Full lazy plus Intl use | 2.985 ms | 1.96 MB | 5.61 | 0.88 |
| Full lazy plus Temporal use | 5.791 ms | 2.28 MB | 10.89 | 1.03 |

In this smoke run, lazy full context creation was about 16.8% faster and allocated
about 22% less than eager full construction. The first-use rows deliberately include
context creation, parsing/evaluation, and feature realization.

Run the same focused job with:

```powershell
$env:BROILER_BENCHMARK_PROFILE = "smoke"
dotnet run --project Broiler.JS/benchmarks/Broiler.JavaScript.Engine.Benchmarks `
  -c Release -- --filter "*Phase4StartupBenchmarks*" --join
```

## Package and sample-host report

The following single-run report was collected on Windows 11, `win-x64`, .NET SDK
10.0.302, and runtime 10.0.10. Host timings are intentionally excluded from comparison
because a one-process sample is sensitive to filesystem, antivirus, and JIT state.
Bytes and working set are retained as the Phase 4 packaging evidence.

| Variant | Files | Publish bytes | Loaded managed bytes | Working set | Context allocation | Compatibility probes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Minimal framework-dependent | 53 | 5,638,454 | 27,606,152 | 40,583,168 | 1,413,752 | 0 |
| Full framework-dependent | 68 | 5,897,244 | 28,832,392 | 49,668,096 | 1,930,464 | 0 |
| Full ReadyToRun | 68 | 10,832,924 | 33,169,248 | 48,029,696 | 2,252,936 | 0 |
| Full trimmed self-contained | 133 | 29,538,464 | 8,259,072 | 44,773,376 | 1,923,760 | 0 |

Against the framework-dependent full host, the selective minimal host reduced publish
bytes by 258,790 (4.4%), loaded managed bytes by 1,226,240 (4.3%), working set by
9,084,928 (18.3%), and context allocation by 516,712 (26.8%). It loaded two fewer
assemblies and reported `undefined|undefined` for Intl/Temporal, while retaining the
core script result.

ReadyToRun increased publish bytes by about 84% and loaded managed bytes by about 15%
in exchange for lower first-script latency in this one-shot run. That trade-off is a
host publish option, not a repository-wide default. The trimmed variant is
self-contained, so its directory bytes and file count include the runtime and are not
directly comparable with framework-dependent output. It executed the same core,
Intl, Temporal, and sample-satellite checks successfully.

The trimmed publish completes but reports genuine linker warnings in dynamic compiler,
CLR, debugger, JSON, and runtime reflection paths (`IL2026`, `IL2055`-`IL2080`). They
are recorded as follow-up work rather than hidden with broad rooting or warning
suppression. The startup host itself uses `Utf8JsonWriter`, so its report does not rely
on reflection-based serialization after trimming.

Regenerate the ignored raw JSON report and publish directories with:

```powershell
python scripts/performance/collect_phase4.py --output artifacts/phase4
```

## Semantic and regression verification

- Focused Phase 4 lazy/eager/minimal/manifest/satellite tests: 9/9 passed.
- Property checks cover enumeration without realization, data descriptors, single
  realization, delete/redefine cancellation, recursive failure, global key order, and
  per-realm identity.
- Intl/Temporal focused regression cluster: 14/14 passed.
- Phase 0-4 architecture/configuration checks: 25/25 passed.
- Compiler, Runtime, and Storage tests: 245/245, 9/9, and 11/11 passed.
- Built-ins tests: 1,908/1,909 passed. The sole failure is the unchanged CLDR alias
  expectation `ru-Armn-AM` versus current data result `ru-Armn-RU`.
- Integration tests: 4,475/4,483 passed. The same eight established locale/timezone,
  fixed-path, and documentation-fixture failures remain; no Phase 4 regression was
  added.
- Minimal, full, ReadyToRun, and trimmed publishes all completed; every sample host
  exited successfully and every explicit host reported zero compatibility probes.
- Every full publish reported the sample feature assembly absent immediately before
  `sampleFeature` access and present immediately afterward; minimal never loaded it.

The focused semantics and broad repository baseline therefore clear the Phase 4
implementation gate. A pinned full test262 run and statistically controlled two-run
cold-process baseline remain release evidence, as in the earlier phases.
