# Phase 1 low-risk hot-path cleanup

Phase 1 implements the ten delivery items in the performance and IL roadmap. The
changes preserve the ordinary JavaScript semantics behind explicit exotic/proxy slow
paths and add executable architecture, package-consumer, and generator checks.

## Implementation matrix

| Roadmap item | Implementation and evidence |
|---|---|
| Explicit host options and cache keys | `JSContextOptions` captures host mode, backend, feature flags, and cache selection. `JSCompilationOptions` flows through the runtime and IL backend and participates in structural cache keys. The CLI resolves `BROILER_SCRIPT_HOST` once; runtime call paths no longer query process state. |
| Allocation-free binary scalar access | Typed arrays use span `BitConverter` overloads and `TryWriteBytes`; DataView uses `BinaryPrimitives` and direct buffer spans for both endian modes. No scalar write creates a temporary byte array. |
| Descriptor-free `HasProperty` | Ordinary own string/index/symbol tests inspect property storage directly. Arrays, primitive strings, and typed arrays override exotic membership, while prototype traversal still invokes Proxy traps. |
| VirtualMemory occupancy | Capacity, live count, used-node high-water mark, and enumeration bounds are distinct. `SAUint32Map` and element storage expose struct enumerators and scan only used nodes. |
| Parser span paths | Keyword recognition switches on `ReadOnlySpan<char>`, unescaped identifiers avoid a builder, numeric coercion parses spans and rents separator scratch storage only when necessary, and `FastList` scans only live entries. |
| Bounded code cache | Each context receives an isolated bounded cache by default; process sharing is explicit. Entry/source/code limits, per-key concurrent miss coalescing, LRU eviction, compilation duration, hit/miss/wait/eviction metrics, and `Clear` are available. |
| Disabled experimental built-ins | Generated exports carry their feature flag and registration checks it before constructing the `JSFunction`. The former create-then-delete registry pass is removed. |
| Cold error paths | DataView success paths branch to non-inlined detached, out-of-bounds, immutable-buffer, and invalid-offset throw helpers. The Phase 0 disassembly profile keeps this layout inspectable. |
| Runtime packages | Regex, DateTime, Unicode properties, and module-extension runtime dependencies are packable and declared. `Broiler.JavaScript.All` is explicitly a dependency-only meta-package; Network and Node polyfills stay optional. |
| Incremental generation | Attribute discovery uses `ForAttributeWithMetadataName`; each type emits independently and aggregate registration collects compact equatable metadata. Workspaces/analyzer dependencies and source-tree output targets are removed. Opt-in output is isolated under `obj/generated/<Configuration>/<TFM>`. |

## Verification

The local Phase 1 validation on Windows x64, .NET SDK 10.0.302/runtime 10.0.10,
completed with:

- aggregate Release build: succeeded;
- architecture/configuration tests: 10/10 passed;
- host/cache/tail-call integration tests: 33/33 passed;
- expanded host/cache/tail-call and DateTime-qualification integration filter: 85/85 passed;
- DataView, typed-array, property-membership, and `clz32` tests: 66/66 passed;
- experimental feature registration tests: 42/42 passed;
- parser tests: 71/71 passed;
- storage tests: 6/6 passed;
- generator determinism: 62 persisted files matched across two rebuilds;
- pristine consumer: restored 21 locally produced packages into an empty package cache
  and evaluated a representative script to `42`.
- BenchmarkDotNet developer smoke: ordinary own `HasProperty` reported 19.6 ns and no
  managed allocation; this three-iteration smoke is wiring evidence, not a release
  baseline.

Run the package and generator gates directly with:

```powershell
python scripts/packaging/test_pristine_consumer.py
python scripts/performance/check_generator_determinism.py `
  --output artifacts/performance/generator-determinism.json
```

The scheduled performance workflow runs both gates on Linux in addition to the Phase 0
evidence collector. Before a release, run the two-repeat `baseline` profile and the
pinned focused test262 manifests on every required architecture. Those release-level
noise-band and full conformance results remain machine/commit-specific artifacts rather
than claims embedded in this implementation document.
