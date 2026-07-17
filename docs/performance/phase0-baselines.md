# Phase 0 performance baselines

Phase 0 is implemented as repeatable evidence collection rather than a checked-in
machine-specific score. The configuration is
[`eng/performance/phase0.json`](../../eng/performance/phase0.json), collected results
conform to
[`phase0-result.schema.json`](../../eng/performance/schemas/phase0-result.schema.json),
and CI retains raw BenchmarkDotNet, EventPipe, MSBuild binary-log, package, IL, and
publish artifacts.

## Commands

Run the two-pass developer smoke profile:

```powershell
python scripts/performance/collect_phase0.py --profile smoke
```

Run a release-quality local baseline, including every Phase 0 evidence type:

```powershell
dotnet tool install --tool-path artifacts/tools dotnet-trace
$env:PATH = "$(Resolve-Path artifacts/tools)$([IO.Path]::PathSeparator)$env:PATH"
python scripts/performance/collect_phase0.py `
  --profile baseline `
  --include-eventpipe `
  --include-build-baselines `
  --include-publish `
  --rid win-x64
```

Run IL/JIT disassembly for the selected primitive paths:

```powershell
python scripts/performance/collect_phase0.py --profile disassembly
```

Raw evidence is written below `artifacts/performance/` and is intentionally ignored by
Git. `phase0-result.json` records the commit/dirty state, exact commands, .NET/OS/RID,
processor identity, GC/tiering overrides, artifact locations, lifecycle samples,
normalized benchmark means and allocations, package graph, assembly file/metadata/IL
sizes, and optional publish results. BenchmarkDotNet's full JSON retains its detected
instruction-set and detailed GC/job data.

## Lifecycle and diagnostic boundaries

The benchmark executable has three non-BenchmarkDotNet modes used by the collector:

- `--lifecycle-child all` runs in a new process and separately records process-to-main,
  first context, first parse/compile/evaluate, cache-hit evaluate, and subsequent
  context measurements;
- `--profile <context|functions|properties|arrays|parsing|mapset>` supplies bounded,
  deterministic EventPipe workloads;
- `--assembly-metrics <assembly>` reads managed PE metadata and reports file bytes,
  metadata bytes, method counts, and aggregate IL bytes.

The EventPipe provider captures runtime allocation/GC, contention, exception, loader,
and JIT events. Hardware counters remain optional because hosted/virtualized runners do
not expose them consistently. Record separate evidence when a supported physical host
is available.

## Repeatability gate

The smoke profile documents a 20% band and is only a wiring check. The baseline profile
uses a 7.5% cross-run band. Both launch BenchmarkDotNet twice; the baseline also takes
ten fresh-process lifecycle samples per repetition. `repeatability.json` compares the
two benchmark means and lifecycle medians.

For an acceptance run:

1. use the same commit, idle physical machine, power plan, RID, CPU feature overrides,
   GC mode, and publish properties;
2. do not mix cold lifecycle data with warmed BenchmarkDotNet measurements;
3. require two runs inside the band; rerun a metric outside the band before treating it
   as a regression;
4. retain time, allocation, working set, file count, and total byte results together;
5. never gate on unavailable virtualized hardware counters or one noisy sample.

Windows x64, Linux x64, and Linux Arm64 are the required release matrix. SIMD changes
also require x64 AVX2 enabled/disabled and an AdvSimd-capable Arm64 host. The checked-in
workflow supplies Windows/Linux x64 smoke evidence; architecture-specific release
runners must supply the remaining matrix.

## Semantic ownership

[`eng/performance/ownership.json`](../../eng/performance/ownership.json) maps every
roadmap priority item to a benchmark and owning semantic test project. It also selects
focused test262 manifests for properties/Proxy, arrays, Map/Set, parser, binary data,
strict mode, and realm isolation. Run a focused manifest with the pinned suite revision:

```powershell
python scripts/compliance/run_test262.py `
  --suite-ref <sha> `
  --path-file scripts/compliance/test262-properties-proxy.txt
```

Files requiring `$262` remain visible in audit/coverage evidence but are reported as
host-harness exclusions by the raw script-host runner.
