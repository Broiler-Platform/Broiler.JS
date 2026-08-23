# JavaScript concurrency — status and evidence

This record maps existing aggregate-repository work to modernization MOD-M5–MOD-M7 without
retroactively treating narrower implementation gates as acceptance. The plan to act from is
[`Concurrency.md`](Concurrency.md).

## State

| Phase | Implemented evidence | Acceptance state | Next decisive action |
|---|---|---|---|
| **MOD-M5 · compile-ahead** | Aggregate item #16 compiles immutable document classic-script sources into the owning context's cache while preserving the ordered evaluation loop | **Implemented subset; MOD-M5 open** | Complete MOD-M5-3 shared-state/artifact census, then run representative paired critical-path and resource gates under one host-wide budget |
| **MOD-M6 · independent contexts** | Four-thread isolation tests and a 1/2/4-context scaling probe exercise named globals, shapes, identical-source compilation, shared/per-context caches, and registry initialization | **Scoped evidence; MOD-M6 open** | Inventory and relocate/isolate mutable IC and type-feedback state, cover every host entry, then run optimizer-on/off soak and reclamation gates |
| **MOD-M7A · Worker agents** | Aggregate Worker first slice has a dedicated thread/context, two-stage cross-realm clone, messages, errors, termination, timers, `importScripts`, and copy-then-detach transfers | **Implemented subset/experimental; MOD-M7A open** | Specify/map the agent lifecycle, enforce a host-wide cap, add failed-transfer atomicity and explicit shared-memory rejection, then run WPT/resource plateaus |
| **MOD-M7B · shared memory** | Existing single-agent/simulated SharedArrayBuffer/Atomics behavior is not cross-agent memory-model evidence | **Not started** | Product cost/risk decision before any implementation |

No phase above is accepted under the modernization gate. “Done”, “built”, or “gate passed”
in aggregate `docs/architecture/multithreading.md` refers to that document's scoped host item
and historical phase, not to the newer MOD-M5–MOD-M7 definitions.

## MOD-M5 evidence map

### What exists

Aggregate item #16, recorded in `docs/architecture/multithreading.md` §8–§9 and its status
tables, implements `ScriptCompileAhead` for a document's classic-script sources. It:

- starts after the `JSContext` and source set exist;
- writes into the context's existing public settable `CodeCache`;
- leaves ordered evaluation on the owning path; and
- has host tests across worker budgets plus published compile-stage and whole-capture
  measurements in `tests/render-stages/results/script-compile-ahead.md`.

### Corrected premise

For this existing host shape, MOD-M5-2's proposed “add an `ICodeCache` or cache factory through
context options” is not a prerequisite: `JSContext.CodeCache` already supplies the store
after context creation. That does **not** prove every desired supported mode has correct
ownership. The remaining decision is to validate lifetime and cache-key completeness and to
add an options/factory seam only if a supported pre-context or externally owned-cache mode
actually requires one.

The current implementation also intentionally excludes direct `eval`: the compiler reads
ambient direct-eval state that is not represented in the cache key, while the supported
top-level scripts agree on “none”. This is a useful explicit narrowing, not evidence that
all compiler artifacts are realm-neutral.

### Why MOD-M5 remains open

- The aggregate corpus demonstrates a host slice, not MOD-M5-1's representative product
  first-script/first-paint opportunity measurement under the current MOD-M1 decision rule.
- No complete compiler-touched mutable-state, registry, feedback, site-allocation, ambient
  context, or generated-delegate capture census is recorded.
- Evidence has not yet established same-key generation/retry semantics for failure,
  cancellation, invalidation, and eviction while unrelated keys progress.
- The scheduler has not been shown to share one cap with Workers, embedding pools, and
  large-stack compiler helpers.
- The owning evidence does not yet include the required RSS, virtual/committed memory, stack
  mappings, GC, queueing, cancellation, and p95/p99 bundle.

## MOD-M6 evidence map

### What exists

Aggregate `docs/architecture/multithreading.md` §10 records five
`JSContextIsolationTests` cases, each using four overlapping owner threads, against globals,
interned keys/shape transitions, identical-source compilation, process-shared code cache,
and built-in registry initialization. The same section records a scaling probe for one
context per thread with per-context and process-shared caches. This is meaningful evidence
that the named configuration is neither trivially leaking nor globally serialized.

### Claim boundary

Those tests do not enumerate all mutable engine state and do not cover the bridge, timers,
promises/jobs, modules, async/generator resumes, every callback entry, lifetime reclamation,
or every optimizer mode. In particular, the modernization audit identifies process-shared
inline-cache/site and type-feedback state whose semantic ownership must be fixed, not merely
locked. Therefore the supported claim is:

> The named default paths passed a four-thread isolation probe and scaled in its synthetic
> workload. General concurrent-context safety, shared adaptive artifacts, and background
> tier-up are not yet accepted.

### Why MOD-M6 remains open

- MOD-M6-1's generated static/global ownership census is absent.
- IC entries/site ownership and type-feedback publication/invalidation/eviction have not
  been demonstrated context/function-local.
- Optimizer-on/off, shared/unshared-cache randomized stress and long soaks are absent.
- Weak-reference reclamation and repeated create/evict/dispose memory plateau are absent.
- There is no architecture test enumerating every asynchronous and host entry into the
  context owner/exclusion policy.

Until these close, bytecode programs intended for cache sharing remain immutable and any
quickening/feedback data stays in context/function-local side state.

## MOD-M7A evidence map

### What exists

Aggregate `docs/architecture/multithreading.md` §11–§12 records:

- existing `MessageChannel`/`MessagePort` and structured-clone machinery;
- cross-context clone tests proving receiving-realm prototypes and no shared identity;
- a Worker-owned thread and `JSContext` with sender-side isolation clone followed by a
  receiving-realm clone;
- page-loop reply queuing, errors, close/terminate/disposal behavior;
- real-deadline Worker timers and synchronous ordered `importScripts`; and
- `ArrayBuffer` transfer semantics implemented as copy then detach, with `MessagePort`
  transfer refused.

The recorded first-slice exclusions are module workers, `SharedWorker`, nested workers,
Worker `requestAnimationFrame`, `MessagePort` transfer, and network-fetched Worker scripts.
Those are legitimate published scope limits.

### Why MOD-M7A remains open

- The existing implementation has not been mapped to a reviewed agent contract covering
  all queue/task-source FIFO guarantees, permitted interleavings, cancellation, error,
  shutdown, and queued-work drop/drain policies.
- No owning evidence yet proves failed-transfer atomicity: validating a transfer list must
  not detach only a prefix before a later entry fails.
- No host-wide worker/context cap and combined compiler/Worker budget is evidenced.
- Repeated creation/termination has no process-wide RSS, committed/virtual memory, stack,
  GC, p95, and stable-plateau acceptance bundle.
- Applicable Worker and structured-clone WPT scope/results are not recorded as the MOD-M7 gate;
  unchanged unrelated WPT classification is regression evidence, not Worker conformance.
- Cross-agent `SharedArrayBuffer` and Atomics rejection is not explicit in the documented
  refused surface and lacks a capability test.

The Worker should therefore be described as an implemented first slice or experimental
profile, not as MOD-M7-complete.

## MOD-M7B status

Not started and deliberately independent. The current single-agent storage and simulated
Atomics behavior does not establish shared backing-store lifetime, no-tear access, ECMAScript
ordering, atomic RMW, waiter lists, `AgentCanSuspend`, growth/termination cleanup, or x64 and
Arm64 stress. No cross-agent shared-memory claim is permitted until the separate product
decision and complete MOD-M7B gate.

## Evidence still required to change state

1. Complete MOD-M5-3/MOD-M6-1 as one generated, reviewable shared-state and artifact census.
2. Move/isolate adaptive state and prove reclamation before advertising general parallel
   contexts or sharing compiled/bytecode artifacts.
3. Put compile-ahead and Workers under one measured host-wide cap.
4. Run MOD-M1-compliant paired critical-path, scaling, tail, and process-memory bundles.
5. Publish explicit supported/excluded source, host, Worker, transfer, shared-memory, and WPT
   modes.
6. Update this status with commands, commit identity, environment, artifacts, and failures;
   do not copy a volatile result into [`Concurrency.md`](Concurrency.md).
