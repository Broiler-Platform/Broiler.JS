# Phase 9 — VM 4.0: optional adaptive IL/bytecode execution — status

**No Phase 9 measurement or feasibility result exists. Nothing in Phase 9 has been built,
measured, or attempted.**

> The evidence half of [`Phase-9.md`](Phase-9.md). A plan statement is not a result.
> Historical IL-path figures retained below remain attributed to their Phase 1 and Phase 5
> status records; they are priors for a future decision, not VM or adaptive-tier results.
> [`Measurement.md`](Measurement.md) governs any future performance claim.

---

## State

| | |
|---|---|
| Items started | **0** |
| Items landed | **0** |
| Measurements taken | **0** |
| Feasibility spikes run | **0** |
| Outcome gate | `execution-only-go` alone does **not** authorize runtime IL tiering |
| Blocked on | MOD-M9 `narrow-runtime-go` or `full-go` with a dynamic-code-capable adaptive host profile; accepted Phase 6/7 evidence; MOD-M1 and applicable MOD-M6 gates |
| First performance action after entry gates | item **9-0 · VM-first promotion decision curve** |
| First deopt action after entry gates | item **9-3 · explicit state-materialization feasibility spike** |

**This phase is not scheduled.** A runtime-capable MOD-M9 outcome is necessary but not
sufficient: its capability manifest must name a host composition where bytecode and emitted
IL both exist and adaptivity is a product requirement. An `execution-only-go` remains a
valid, correct precompiled-execution product and must not be expanded into a runtime compiler
or IL tier merely to enter this phase.

---

## Entry performance evidence — item 9-0

Use MOD-M1's accepted modern/product manifest, stable-host protocol, and predeclared decision
rule. The future immutable evidence bundle must compare:

1. VM-only execution;
2. VM-first plus promotion across a **predeclared threshold curve**, including promotion
   disabled and failure/backoff controls;
3. current IL eager compilation where that composition remains supported;
4. the accepted current-IL lazy/deferred-function path; and
5. persisted and non-persisted bytecode arms where relevant, labelled explicitly.

Record first-context, first-script, first-paint/product milestone, and steady-state time with
allocation, GC, peak/steady working set, retained source/IR/bytecode/IL, code/package size,
compile queue/latency, promotion attempts/successes/failures, and applicable p50/p95/p99.
Separate foreground and any approved background compilation. Native-AOT-only targets are
VM-only product lanes; do not average them into a dynamic-code tier-up result.

Before running the curve, record the primary metric, minimum relevant effect or equivalence
budget, resource-guardrail precedence, retained-code/metadata ceiling, missing-row policy,
and confirmation rule. If the accepted IL lazy path captures the opportunity or every
threshold remains below measurement resolution or violates a guardrail, the valid result is
to cancel 9-1/9-2.

The independent expected-result manifest is the conformance oracle for every configuration.
VM/IL agreement alone is a differential check, not proof of correctness.

---

## Semantic and ownership evidence — items 9-1 and 9-2

No existing counter, feedback table, source-text tiering hook, or delegate replacement is
recorded as completion. Before either item can land, its evidence must show:

- hotness and transition state owned by a bytecode function/program or realm, with MOD-M6
  snapshot, invalidation, cancellation, publication, eviction, and memory-plateau behavior;
- duplicate-promotion suppression and installation only at a proved quiescent boundary;
- promotion from a backend-neutral compiled-function descriptor retaining source span,
  strict/async/generator flags, lexical/private environment shape, realm, home object,
  module/eval/arguments requirements, and stable safepoint mapping;
- preserved function object, environment, `this`, `new.target`, home-object, and stack/source
  identity; and
- bounded retained VM fallback plus source, cached-bytecode, promoted, failed-promotion, and
  disabled conformance arms.

Reparsing function text as a fresh top-level program does not satisfy this gate, even when it
can produce an executable delegate.

---

## Deoptimization feasibility evidence — item 9-3

Item 9-3 has a separate correctness/product gate from 9-0; a negative promotion-speed result
does not decide it. It is also not authorized merely because an interpreter frame exists.
The first deliverable is a bounded spike, not a general deoptimization implementation.

The spike must prove one end-to-end forced guard failure with:

1. a canonical bytecode PC/safepoint that survives peephole, quickening, and persistence;
2. a generated `DeoptState` containing every live local and operand value, lexical
   environment, receiver/arguments/`new.target`, completion value, and handler/`finally`
   state required at that point;
3. IL generated specifically to spill/pass those values before leaving the compiled frame;
4. verifier/source/debug metadata mapping materialized state to the shared semantic IR;
5. VM frame reconstruction at the exact continuation without replaying an observable
   operation; and
6. forced-failure conformance against uninterrupted VM and IL controls, with code-size,
   runtime, allocation, metadata, and maintenance costs.

The evidence must enumerate supported and excluded safepoints. It cannot rely on inspecting
arbitrary live CLR locals after the fact. Failure to materialize the required state inside
the predeclared ceilings records a terminal `no-go` for 9-3; it does not become an
unspecified later implementation task.

Item 9-5 is evaluated only after accepted 9-3 evidence. The current restart/in-method
fallback remains authoritative until deoptimization demonstrably subsumes its supported
cases.

---

## OSR evidence — item 9-4

OSR is not inferred from deoptimization and is not the reverse mapping. Before an
implementation starts, record:

- the measured population and duration of loops that remain hot after function-level
  promotion;
- eligible loop headers and excluded handler/suspension states;
- a compiled loop-entry stub and calling convention that accepts validated VM state;
- VM-slot/environment to IL-local mapping, immediate guard-failure behavior, and
  anti-thrashing policy;
- debugger/stack/source identity and code/metadata lifetime; and
- a MOD-M1 paired interval that resolves and clears the predeclared practical end-to-end
  threshold while staying within resource and maintenance ceilings.

An ordinary function delegate that enters only at function start is not OSR evidence.

---

## Future evidence ledger

| Item | Required result before it can be marked validated |
|---|---|
| 9-0 decision curve | immutable MOD-M1 bundle for VM-only, promotion curve, IL eager/lazy, persistence controls, resources, and predeclared terminal decision |
| 9-1 transition state | owner/lifetime ADR, atomic transition tests, cancellation/backoff, concurrent-context isolation, eviction plateau |
| 9-2 descriptor promotion | no source reparse, identity/environment fixtures, quiescent publication, bounded fallback, independent-oracle result |
| 9-3 deoptimization | explicit ABI, canonical-PC mapping, generated live-state spill, forced guard failures, unsupported-safepoint manifest, stop/go decision |
| 9-4 OSR | measured loop population, separate entry-stub ABI and feasibility result, transition/thrashing fixtures, end-to-end decision |
| 9-5 restart decision | case-by-case comparison showing which restart/in-method fallbacks are retired or retained |

---

## Historical evidence retained as priors

The previous roadmap cited the following **historical IL-path** results from their owning
Phase 1 and Phase 5 status records:

- depending on corpus, **84–99.7%** of declared functions were never invoked;
- the then-current lazy/deferral experiment reported jQuery **0.661x**, PdfJS **0.689x**,
  Box2D **0.636x**, and CodeLoad **1.099x** in its recorded setup; and
- the Phase 5 tiering race reported **1.010x on 3 of 6 interleaved pairs** and remained off
  by default because it retained a `DynamicMethod` per hot pattern without a worthwhile
  speed result.

Those values remain tied to their original revisions, corpora, controls, and IL
implementation. They justify testing unused-function opportunity, accepted IL laziness,
threshold curves, and retained-code cost. They do **not** establish a present VM population,
a promotion win, deoptimization feasibility, or OSR value.

When any Phase 9 work begins, append immutable MOD-M1/MOD-M6/conformance evidence here while
preserving this historical attribution and the current zero state until an item actually
starts.
