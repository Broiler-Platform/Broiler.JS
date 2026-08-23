# JavaScript concurrency — compile-ahead, context isolation, and Worker agents

Own the JavaScript-local parts of modernization phases MOD-M5–MOD-M7: compiler/cache concurrency,
optimizer-state ownership, context entry, and ECMAScript agent semantics. The objective is
bounded, deterministic concurrency with measured product value—not parallelism as a feature
claim by itself.

> The evidence half is [`Concurrency.status.md`](Concurrency.status.md). The cross-track
> order and program gates remain in [`Modernization.md`](Modernization.md); measurement
> claims close only under [`Measurement.md`](Measurement.md).
>
> The aggregate `docs/architecture/multithreading.md` owns cross-component integration,
> host implementation history, and its measurements. Its compile-ahead and Worker slices
> are inputs recorded in the status file; “built” there does not mean MOD-M5, MOD-M6, or MOD-M7 is
> accepted here.

## Current state

Existing aggregate-repository implementations cover a narrow classic-script compile-ahead
path and an initial dedicated Worker host. They are **implemented subsets**. MOD-M5–MOD-M7 remain
open because the shared-state census, optimizer lifetime work, host-wide budgets, full
lifecycle/conformance gates, and resource plateaus have not been demonstrated. See
[`Concurrency.status.md`](Concurrency.status.md).

## Invariants

1. A `JSContext` has one owner thread while JavaScript executes. Fetch, parse, semantic
   analysis, and compilation may overlap only at a boundary proved realm-neutral; install
   and execution return to the owner at a defined quiescent point.
2. Execution order, error type/message/location/reporting order, cancellation, and cache
   semantics are identical with concurrency disabled and enabled.
3. Mutable shapes, inline caches, type feedback, quickening state, delegates, jobs, and host
   captures have an explicit semantic owner and reclaimable lifetime. A lock does not make
   cross-realm feedback correct.
4. Every scheduler shares one host-wide worker/context budget. Compiler workers, Workers,
   embedding pools, and large-stack helpers may not multiply independently.
5. A cached or shared program artifact is immutable and realm-neutral. Adaptive state is a
   realm/function-local side table or copy-on-write overlay, never an in-place mutation of a
   shared bytecode blob.
6. One Worker is one agent with its own context, event loop, job queue, owner thread, error
   channel, and shutdown policy. No realm object crosses an agent boundary directly.
7. Cross-agent `SharedArrayBuffer` and Atomics remain unavailable until the separate MOD-M7B
   memory-model gate passes.

## MOD-M5 — bounded compile-ahead without concurrent context execution

The existing host slice compiles immutable top-level classic-script sources into an already
created context's settable `CodeCache`. That corrects MOD-M5-2's original premise for this
specific shape: no new cache option is automatically required. It does not remove the need
to prove cache keys, realm neutrality, single-flight behavior, and bounded scheduling.

| ID | Next action owned here | Exit evidence |
|---|---|---|
| **MOD-M5-1** | Measure fetch, parse, semantic analysis, code generation, install, ordered execution, first script, and first paint on representative multi-script product workloads. Predeclare the primary metric, practical decision threshold, guardrails, and lane-specific A/A validity rule. | MOD-M1 run bundle with background-off control |
| **MOD-M5-2** | Audit the existing public settable `JSContext.CodeCache`, its construction time, lifetime, key, invalidation, and supported host modes. Add an option/factory only if a supported pre-context or externally owned-cache mode requires it. | cache ownership/key decision and tests |
| **MOD-M5-3** | Census every compiler-touched static/global, registry, ambient context read, site allocator, feedback switch, generated delegate capture, and realm-derived input. Classify shareable source/parse/semantic IR separately from realm-bound installation. | completed classification in the status file |
| **MOD-M5-4** | Bring the existing scheduler under a host-wide cap shared with all other worker sources. Derive the default from measured stack, RSS, and active-worker high-water; keep a synchronous zero/background-off path. | scheduler budget and saturation tests |
| **MOD-M5-5** | Keep the first supported input to immutable external classic scripts with final bytes, URL, mode, backend, and options. Install and execute in source/document order on the realm owner. Publish every exclusion. | ordered integration and error tests |
| **MOD-M5-6** | Make same-key work single-flight per live cache-entry generation. Specify failure, cancellation, invalidation, eviction, and retry; unrelated keys must progress. | deterministic stress suite |
| **MOD-M5-7** | Record queue/compile/install time, overlap, active and parked workers, stack mappings, duplicate waits, cancellation, cache events, allocations, RSS, GC, and p50/p95/p99. | schema-valid MOD-M1 metrics |
| **MOD-M5-8** | Compare background-off, 1, 2, 4, and automatic budgets in cold and warm paired runs. Include the existing aggregate implementation as a candidate arm, not as the baseline. | paired decision report |

### MOD-M5 exit gate

- Disabled and enabled arms have identical results, ordering, diagnostics, applicable
  conformance outcomes, and one-context-at-a-time entry.
- Same-key compilation is bounded and single-flight per generation; cancellation and retry
  follow the documented policy.
- A user-visible critical-path metric improves beyond the predeclared decision rule while
  RSS, virtual/committed memory, stack reservations, GC, and p95/p99 stay within host budget.
- The supported source/mode/backend set and every exclusion are explicit. The feature stays
  opt-in until all of the above pass.

Stop after MOD-M5-1 if compilation is immaterial to the product critical path. Narrow sharing to
parse/semantic IR if generated code is realm-bound. Never multiply compiler workers by
embedding or Worker pools without the single host cap.

## MOD-M6 — independent-context and optimizer-state safety

The aggregate isolation/scaling probe is useful evidence for the named default paths. It is
not a complete safety proof while mutable adaptive state or unenumerated host entries can
cross contexts.

| ID | Next action owned here | Exit evidence |
|---|---|---|
| **MOD-M6-1** | Extend MOD-M5-3 across execution. Classify shapes, inline-cache site tables, type feedback, registries, queues, diagnostics, compiled artifacts, and host captures as immutable process, synchronized process, artifact, realm, function, or diagnostic state. | generated census with an owner/lifetime for every entry |
| **MOD-M6-2** | Move inline-cache entries and site ownership to a realm, compiled function, or equivalent lifetime that cannot mix semantic feedback across contexts. | focused cross-context IC tests |
| **MOD-M6-3** | Replace or isolate static non-thread-safe type-feedback state; define publication, snapshot, invalidation, and eviction. Keep quickening/adaptive data outside immutable cached bytecode. | feedback/quickening tests and metrics |
| **MOD-M6-4** | Prove disposed contexts and evicted functions release feedback, delegates, source, host captures, and cache state. | weak-reference tests and repeated memory plateau |
| **MOD-M6-5** | Stress serialized versus 1/2/4 independent contexts, on fixed owner threads, with shared and unshared caches and optimizer modes both enabled and disabled. | randomized and long-soak equality runs |
| **MOD-M6-6** | Measure throughput, p95/p99, contention, cache hits, allocation, RSS, GC, and plateau at each context count. | MOD-M1 scaling bundle |
| **MOD-M6-7** | Permit background tier-up only after the other MOD-M6 gates, from a quiescent snapshot with owner-thread installation and preserved fallback. | tier-up decision and focused tests |
| **MOD-M6-8** | Inventory every host, promise/job, async/generator, timer, module, callback, and error entry into JavaScript; assert every route uses the context exclusion/dispatch policy. | architecture/coverage test |

### MOD-M6 exit gate

Parallel contexts must equal serialized execution under stress, every mutable optimization
structure must have semantic ownership and reclamation, every host entry must use the owner
policy, and repeated creation/eviction/disposal must reach a stable memory plateau. Report
scaling and tails at 1/2/4 contexts. Until then, describe existing results as scoped
isolation evidence—not general concurrent-context safety—and do not share adaptive VM state.

## MOD-M7A — Workers as isolated agents, without shared memory

The aggregate Worker is the implementation seed. Bring it under an explicit agent contract
and close the missing lifecycle, budget, resource, and conformance gates rather than treating
feature presence as acceptance.

| ID | Next action owned here | Exit evidence |
|---|---|---|
| **MOD-M7-1** | Specify the agent: context/realms, owner thread, job and task queues, error channel, event-loop scheduling, host budget, shutdown, cancellation, and queued-work drop/drain policy. Map the existing Worker to it. | reviewed agent contract and architecture tests |
| **MOD-M7-2** | Verify structured clone for the supported cyclic value graph and errors; explicitly reject host objects without clone semantics. | clone conformance suite |
| **MOD-M7-3** | Verify transfer-list validation and failed-transfer atomicity before detachment. Preserve copy-then-detach as an explicit performance limitation until zero-copy ownership exists. | transfer atomicity/lifetime tests |
| **MOD-M7-4** | Complete create/options, FIFO guarantees per port/task source, allowed interleavings, error propagation, close, cancellation, termination, and nested/module/network policies. | stress tests and declared WPT scope |
| **MOD-M7-5** | Apply the shared host-wide cap and measure startup, throughput, p95, worker high-water, RSS, committed/virtual memory, stack reservation, and repeated create/terminate plateau. | MOD-M1 resource bundle |
| **MOD-M7-6** | Make cross-agent `SharedArrayBuffer` and Atomics unavailable in the initial profile and test the rejection. Document single-agent/simulated behavior separately. | capability tests and public scope |

### MOD-M7A exit gate

The specified queue/FIFO, lifecycle, error, cancellation, transfer, and termination policies
hold under stress; applicable Worker/clone WPT and repository tests pass at multiple worker
counts; the host cap is respected; repeated creation/termination reaches a stable memory
plateau; and no shared-memory feature crosses agents. Keep the feature experimental if any
part remains open.

## MOD-M7B — shared memory and Atomics

MOD-M7B is a separate, high-risk product decision and is not implied by MOD-M7A. It requires a shared
backing store, ECMAScript no-tear and ordering rules, real atomic RMW operations, waiter
lifecycle, `AgentCanSuspend`, termination/growth handling, and repeated x64/Arm64 litmus and
test262 stress. A lock around the existing simulated operations is not an implementation of
the memory model.

## Evidence routing

- Record item state, implementation mapping, scoped claims, and next actions in
  [`Concurrency.status.md`](Concurrency.status.md).
- Record controlled performance/resource bundles under [`Measurement.md`](Measurement.md)
  and link them from the status file.
- Keep cross-component host integration narrative and historical measurements in aggregate
  `docs/architecture/multithreading.md`; link back here whenever it labels a JavaScript slice
  built or complete.
- Record bytecode quickening, feedback, persistence, and tiering evidence in the applicable
  VM phase status as well as MOD-M6 when concurrency ownership is affected.
