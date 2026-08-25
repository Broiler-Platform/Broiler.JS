# Phase 8 — Broiler.VM JavaScript profile 3.0: measured optimization and persistence

Optimize the accepted Phase 7 JavaScript interpreter only from its own measured execution
profile.
Type feedback, quickening, superinstructions, and dispatch layout are gated by item 8-0.
The bytecode cache is the deliberate exception: its premise is cold startup and repeated
compilation, so it is gated by MOD-M1 startup evidence and Phase 6's accepted format/verifier,
not by an opcode execution histogram.

The measured product boundary follows the selected Broiler.VM JavaScript composition. An
`execution-only` optimizes and caches verified precompiled JavaScript execution without
inventing a runtime compiler. A `narrow-runtime-compiler` or `general-runtime-compiler` may additionally optimize
and cache the approved
source-to-result runtime-compiler path.

This phase owns JavaScript-profile persistence and JavaScript-specific adaptive state. The
common catalog, profile-selection, execution-session lifecycle, and cross-profile resource
policy remain owned by `Broiler.VM/docs/roadmap.md`. A cache or optimized JavaScript result
must integrate with those contracts, but this phase does not define them for WebAssembly or
future built-in profiles. The selected JavaScript deployment/compiler composition is also
distinct from the Broiler.VM `JavaScript` language-profile identity and from the realm's
`JavaScriptBootstrapProfile`; all applicable identities belong in measurements and cache keys.

> The plan half of [`Phase-8.status.md`](Phase-8.status.md). No JavaScript-profile measurement
> exists and no item is scheduled.
> Part of the [performance and benchmark roadmap](Roadmap.md); the four JavaScript-profile
> VM phases are
> [track two](Roadmap.md#track-two--the-vm-tier-phases-69).

---

## Why this phase is a gate rather than a catalogue

Adaptive techniques have permanent format, correctness, memory, and maintenance cost. A
star rating or another engine's opcode table is not evidence about Broiler.JS. Phase 8 first
counts the operations and types the accepted JavaScript interpreter actually executes, then prices candidates
with an absolute rate and attainable end-to-end ceiling.

Historical IL-path evidence remains useful as a warning: a large allocation share may buy a
small time result, and a monomorphic share may cover too little total time to justify a new
tier. It cannot replace the JavaScript profile's own modern/product workload evidence.

**Owner boundaries:** the Broiler.VM JavaScript executor, the Broiler.JS compiler/runtime, and
accepted Broiler.JS Runtime/Storage contracts. Any
mutable feedback, quickening, or IC state is owned by a bytecode function/program or realm,
not by a process-global site number, and is reclaimed with that owner under MOD-M6.

## Items

| # | Item | Size | Entry evidence | State |
|---|---|---|---|---|
| **8-0** | **Instrument and attribute the accepted JavaScript interpreter** | M | Phase 7 uninstrumented baseline | ❌ **not started; blocks 8-1 through 8-5** |
| **8-1** | Realm/function-owned type feedback | M–L | polymorphism/type population and MOD-M6 ownership | ❌ |
| **8-2** | Adaptive/quickened opcodes | L | generic opcode/type distribution and mutation/lifetime design | ❌ |
| **8-3** | Superinstructions | M | measured opcode n-grams and bytecode-size budget | ❌ |
| **8-4** | Dispatch layout/encoding experiment | S–M | dispatch/decode cost on each claimed JIT/AOT RID | ❌ |
| **8-5** | PGO the Native AOT JavaScript-profile image | S–M | representative target workload and MOD-M1 AOT lanes | ❌ |
| **8-6** | **Versioned verified bytecode persistence/cache** | M–L | MOD-M1 cold-startup opportunity and accepted 6-3 format | ❌ **independent of 8-0** |

### 8-0 · Instrument and attribute the JavaScript interpreter — **the optimization gate**

Phase 7 supplies the uninstrumented end-to-end baseline. Item 8-0 adds separate diagnostic
builds/runs that collect:

- executed opcode counts and bytes fetched, per family and workload;
- operand-type distributions per candidate specialization site;
- branch/slow-path, IC, element, call, allocation, and completion counts;
- operand-stack/local traffic and frame high-water marks; and
- opcode bigram/trigram histograms for superinstruction candidates.

Counters give exact **counts**, not unbiased nanoseconds. Do not time every opcode in the hot
loop and call the sum an attribution. For every diagnostic run:

1. retain an uninstrumented same-revision control;
2. report diagnostic overhead and whether instruction/type distributions changed;
3. use focused differential microbenchmarks or supported runtime/hardware-counter evidence to
   estimate a cost per candidate operation; and
4. reconcile candidate ceilings against the uninstrumented end-to-end total.

Run the MOD-M1 workload manifest: product scenarios, supported JetStream 3 shell cases, focused
probes, and relevant conformance fixtures. Octane may remain a historical continuity arm,
but “all fifteen suites” is not a modern acceptance gate and does not replace unsupported or
missing product/JetStream rows.

Inspect the generated dispatch on each claimed CoreCLR and Native AOT RID. A dense managed
`switch` often lowers efficiently, but the plan does not assume a jump table, branch cost, or
opcode ordering before inspecting/measuring that target.

### 8-1 · Type feedback with semantic ownership

Do not consume the current static `Runtime/TypeFeedback` tables unchanged. Define feedback
on a bytecode function/program or realm-owned side table with:

- program-relative site identity;
- snapshot/publication rules for an optimizing consumer;
- invalidation when code, realm, shape assumptions, or feature/profile inputs change;
- weak/stable identities for callees so observation does not retain arbitrary functions;
- eviction/disposal reclamation and memory-plateau tests; and
- serialized bytecode that contains no warmed feedback or process-local object/shape ids.

This item depends on MOD-M6. It is accepted only if 8-0 identifies a population and a consumer
with a predeclared end-to-end ceiling; recording feedback without a funded consumer is not a
shipping feature.

### 8-2 · Adaptive/quickened opcodes

Specialize a generic opcode only where 8-0 shows a stable type population and where the
accepted 6-2 slot ABI makes the guard/materialization cheaper than the generic path. Every
quickened form retains an exact generic fallback and a canonical bytecode position for
exceptions, debugging, serialization, deoptimization, and OSR metadata.

Quickening is mutable execution state, never persisted as authoritative format state. Define
who may mutate it, how a second context obtains its own state, how it resets, and how
instrumentation observes generic versus quickened executions. Test guard failure before and
after realm/shape/prototype changes.

### 8-3 · Superinstructions

Generate candidates from 8-0's measured n-grams for this compiler and workload manifest.
For each candidate report executed population, dispatches removed, bytecode/code-size change,
verifier/debugger/source-map impact, and end-to-end result. Cap the set; every fused opcode is
a permanent compiler, verifier, interpreter, debugger, and conformance case.

Do not copy a pair list from another VM or infer value from frequency alone. A frequent pair
whose handlers dominate their own cost may save no measurable time.

### 8-4 · Dispatch layout and operand encoding

Run only if 8-0 plus generated-code inspection show dispatch or decode is a material cost.
Compare the plain switch with any supported alternative on each claimed JIT/AOT RID, with
feature and code-size controls. Include branch misses/instruction-cache evidence where the
platform exposes it, but accept or reject on the predeclared end-to-end metric and resource
guardrails.

Operand compactness is principally a cache/startup/package property. Measure verified bytes,
decode cost, and total launch/execution rather than assuming the smallest encoding wins.

### 8-5 · PGO the Native AOT image

Train and publish the actual accepted Native AOT composition with representative JavaScript
product workloads. Keep training and evaluation corpora separate, record toolchain/profile
inputs, and compare no-PGO/PGO images on every claimed target lane for startup, steady-state,
image size, memory, and conformance.

This item changes no JavaScript semantics, but it is still a release-configuration
performance claim and follows all MOD-M1 controls.

### 8-6 · A versioned verified bytecode cache

Persistence is driven by repeated parse/semantic/lowering cost and cold product milestones,
not by 8-0's execution histogram. It starts only after the Phase 6 format/verifier is accepted
for persistence and the MOD-M1 paired interval resolves and clears the predeclared practical
startup threshold.

#### Persisted identity and invalidation

Every artifact has a bounded, checksummed header/manifest containing at least:

- Broiler.VM language-profile id, JavaScript bytecode schema version, engine semantic/cache
  version, JavaScript capability-manifest id, and deployment/compiler composition;
- `JavaScriptBootstrapProfile` plus the bootstrap/built-in manifest versions where they affect
  semantics;
- source/content hash and every semantic compiler option/cache-key input;
- host-contract/built-in manifest version and approved optional-satellite composition;
- section sizes/counts, endianness/encoding rules, and integrity checksum; and
- producer identity needed for compatibility policy, without treating a branch name as a
  semantic key.

Property names are serialized as text and re-interned when loaded. Never persist raw
`KeyString`, shape, IC, type-feedback, function-object, realm, or host-object identities.
Loaded code receives cold owner-local IC/feedback/quickening state.

#### Safety and lifecycle

- Verify the complete artifact before installation or execution, using 6-3's CFG, stack,
  handler, suspension, metadata, and resource rules.
- Fuzz malformed/truncated/oversized artifacts and verify deterministic rejection.
- Write atomically, recover from partial writes, and define concurrent-reader/writer and
  eviction behavior.
- On incompatibility or corruption, fall back to source compilation only for a
  `narrow-runtime-compiler` or `general-runtime-compiler` product whose selected capability
  manifest contains that source; an
  `execution-only` product reports a defined load failure.
- Bind retained strings, code, side tables, and host captures to the cache entry and prove a
  repeated load/run/evict plateau.

#### Measurement arms

Report the arms applicable to the selected JavaScript composition without inventing a runtime compiler:

1. for `narrow-runtime-compiler` and `general-runtime-compiler`, cold source compile + execute and warm source
   compile + execute with no persisted hit;
2. for all compositions, verified artifact/cache hit + execute;
3. incompatible/corrupt cache fallback for a runtime-compiler composition or the defined
   `execution-only` load failure;
4. persistence disabled—an uncached source compile for a runtime-compiler composition, or a
   fresh verification/load of the supplied precompiled artifact for `execution-only`;
   and
5. offline precompilation time and output size as a separately labelled build/deployment
   result when they are relevant to the execution-only product.

Keep compile-throughput benchmarks cache-disabled unless their explicit purpose is cache
lookup/deserialization. The run manifest must identify the arm; a cache hit must never be
silently reported as compiler throughput.

## Order

```text
Phase 7 JavaScript-profile uninstrumented baseline
  ├→ 8-0 diagnostic counts + calibrated cost/ceiling
  │    ├→ 8-1 owned feedback → 8-2 quickening
  │    ├→ 8-3 superinstructions
  │    └→ 8-4 dispatch/encoding, 8-5 AOT PGO when their populations exist
  └→ 8-6 persistence independently
       (requires accepted 6-3 format/verifier + MOD-M1 startup opportunity, not 8-0)
```

## Exit gate

1. Each of 8-1 through 8-5 cites 8-0's executed population, calibrated cost, attainable
   ceiling, and predeclared decision rule. A candidate without that evidence is not built.
2. Item 8-6 instead cites MOD-M1 cold-startup/compile-or-precompiled-load evidence and passes the complete
   compatibility, verifier, corrupt-input, fallback, atomic-write, and lifecycle gates.
3. The independent expected-result manifest and IL/JavaScript-profile differential checks
   remain green on
   source, cache-round-trip, quickened/unquickened, and claimed Native AOT arms.
4. Instrumented results report their overhead beside an uninstrumented same-revision control;
   counts are not presented as direct time attribution.
5. Paired MOD-M1 evidence includes time with allocation, GC, RSS/working set, bytecode/code/image
   size, and applicable tail latency on modern/product workloads. Octane is labelled
   historical continuity.
6. Every item is accepted, remains explicitly experimental, is deferred, or is removed;
   failed switches and unused opcode forms do not accumulate indefinitely.

## Dependencies

- Depends on Phase 6 JavaScript correctness/format evidence for its selected composition
  (`execution-only`, `narrow-runtime-compiler`, or `general-runtime-compiler`), Phase 7's uninstrumented
  baseline, MOD-M1, and the accepted Broiler.VM catalog/session integration contract. A
  runtime-compiler no-go selects `execution-only`; it does not affect WebAssembly or any
  other Broiler.VM profile.
- Items with mutable feedback, quickening, shared code, or cross-context execution depend on
  the applicable MOD-M6 ownership/lifetime gates.
- 8-5 depends on declared Native AOT target lanes. 8-6 depends on an accepted persisted
  format version but not on 8-0.
- Phase 9 consumes canonical, immutable bytecode positions and frame metadata. Quickening and
  peephole work must preserve that identity; a mutable opcode address is not a deopt/OSR key.
