# Performance measurement and execution modes

Performance changes are accepted only with repeatable measurements and the semantic
tests that own the optimized path. Machine-specific output belongs under the ignored
`artifacts/performance/` directory, not in Markdown result logs.

## Configuration

- Jobs: [`eng/performance/phase0.json`](../eng/performance/phase0.json)
- Result schema:
  [`eng/performance/schemas/phase0-result.schema.json`](../eng/performance/schemas/phase0-result.schema.json)
- Benchmark/test owners:
  [`eng/performance/ownership.json`](../eng/performance/ownership.json)

## Collect evidence

Developer wiring check:

```powershell
python scripts/performance/collect_phase0.py --profile smoke
```

Release-quality local baseline:

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

Selected IL/JIT disassembly:

```powershell
python scripts/performance/collect_phase0.py --profile disassembly
```

The collector records commit/dirty state, commands, runtime, OS/RID, processor, GC and
tiering settings, lifecycle samples, BenchmarkDotNet results, package graph, managed
assembly sizes, and optional publish results. Retain the raw BenchmarkDotNet, EventPipe,
binary-log, IL, and publish artifacts with release evidence.

## Measurement rules

The smoke profile uses a broad 20% repeatability band and only verifies wiring. The
baseline profile uses a 7.5% cross-run band and fresh-process lifecycle samples.

For an acceptance run:

1. use the same commit, idle physical machine, power plan, RID, CPU feature overrides,
   GC mode, and publish properties;
2. keep cold lifecycle results separate from warmed microbenchmarks;
3. require two runs inside the configured band;
4. report time, allocation, working set, file count, and publish bytes together; and
5. run the semantic owner and focused test262 manifests named in
   `eng/performance/ownership.json`.

The release matrix is Windows x64, Linux x64, and Linux Arm64. SIMD claims also require
x64 with the relevant feature enabled and disabled and an AdvSimd-capable Arm64 host.

## Bootstrap profiles

`JavaScriptBootstrap` and `JavaScriptContextBuilder` accept a
`JavaScriptBootstrapProfile`. Three standard profiles are provided:

- `Full`: the supported full surface with lazy Intl/Temporal realization;
- `FullEager`: the comparison/compatibility profile that realizes the full surface
  eagerly; and
- `Minimal`: a deliberately reduced, non-conformant host surface.

Hosts should select a profile explicitly. A smaller package or faster context is not a
conformance win if required globals are absent.

## Experimental execution modes

Function tiering is disabled unless the host supplies enabled tiering options. It is
bounded per realm and must retain the original delegate as the semantic fallback.

`Broiler.JavaScript.Portable` is a separate numeric bytecode/interpreter capability for
offline compilation and Native AOT. It supports numeric parameters/locals, arithmetic,
comparisons, assignment, blocks, `if`, `while`, counted `for`, and value returns. It does
not implement the JavaScript object model, strings, properties, arrays, calls, closures,
exceptions, modules, async/generators, host callbacks, `eval`, or runtime compilation.
Do not describe it as Native AOT support for the full engine.
