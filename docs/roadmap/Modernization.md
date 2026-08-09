# Broiler.JS performance, architecture, and concurrency modernization roadmap

Turn the roadmap audit into an executable program: first make the current state and
measurement system trustworthy, then establish enforceable assembly and AOT boundaries,
then add bounded concurrency and profile-led engine work, and only then decide whether a
second execution engine is justified.

> This is a **cross-track orchestration overlay**. It supplements, but does not replace,
> [`Roadmap.md`](Roadmap.md), [`Assemblies.md`](Assemblies.md),
> [`Component.md`](Component.md), or phases 0–9. It owns ordering, dependencies, decision
> points, and program-level exit gates. Mutable results remain in the owning
> `*.status.md` record and in reproducible machine-generated evidence.

---

## 1. Mandate

This roadmap has six outcomes:

1. contributors can identify the engine's real current state from one generated index;
2. performance claims are reproducible against a same-machine, known-good control;
3. the target assembly graph is buildable, acyclic, baselinable, and compatible;
4. the IL and Native AOT profiles are enforced by builds rather than prose labels;
5. independent JavaScript work can scale without executing one `JSContext` concurrently;
6. optimization, Workers, shared memory, and a bytecode VM proceed only after their entry
   measurements justify them.

The roadmap deliberately combines performance, decomposition, and concurrency. In this
engine they share the same prerequisites: a backend-neutral semantic front end, explicit
state ownership, reproducible package and memory baselines, and architecture tests that
prevent dynamic-code dependencies from leaking back into portable profiles.

### Relationship to the existing plans

| Existing source | Continues to own |
|---|---|
| [`Measurement.md`](Measurement.md) | the acceptance protocol and the conditions under which a performance result may be claimed |
| [`Roadmap.md`](Roadmap.md) and [`Roadmap.status.md`](Roadmap.status.md) | the optimization catalogue, campaign sequencing, and cross-phase performance evidence |
| [`Phase-0.md`](Phase-0.md) through [`Phase-5.md`](Phase-5.md) | detailed work on the current IL execution path |
| [`Assemblies.md`](Assemblies.md), [`AssemblySplit.md`](AssemblySplit.md), and their status records | assembly moves, backend isolation, and the evidence for each graph change |
| [`Phase-6.md`](Phase-6.md) through [`Phase-9.md`](Phase-9.md) | the detailed bytecode and adaptive-VM design, if the VM decision is `go` |
| [`Component.md`](Component.md) | conformance, host modes, public API, and package readiness; M0-7 links the future JavaScript concurrency plan into it |
| Aggregate `docs/architecture/multithreading.md` | integration across HTML, loading, scheduling, and other components; after M0-7, JavaScript implementation details move to the component-owned concurrency pair |

The `M-*` identifiers below do not renumber any existing item. Each work package must name
its owning plan/status record before implementation starts. If none exists, the first
change creates or nominates that evidence record; this overlay is never used as a delivery
journal.

### Evidence routing

M0-1 defines one machine-readable state source at
`eng/performance/roadmap-items.json`. A state transition and its owning status evidence land
in the same change; the human-readable cross-track index is generated from that file and is
never edited as a second ledger. Required evidence routes are:

| Modernization work | Exact owning record |
|---|---|
| M0 split validation and compatibility | [`AssemblySplit.status.md`](AssemblySplit.status.md) |
| M0 graph and M2–M4 architecture/package work | [`Assemblies.status.md`](Assemblies.status.md), or a new dedicated split plan/status pair named before that split starts |
| M0 phase reconciliation and M8 optimization | the matching [`Phase-1.status.md`](Phase-1.status.md), [`Phase-3.status.md`](Phase-3.status.md), [`Phase-4.status.md`](Phase-4.status.md), or [`Phase-5.status.md`](Phase-5.status.md) |
| M1 baseline and M10 governance | [`Roadmap.status.md`](Roadmap.status.md) plus the immutable raw evidence bundle |
| M5–M7 concurrency and Workers | `Concurrency.md` and `Concurrency.status.md`, created and indexed by M0-7 before any of those phases starts |
| M9 VM decision and any approved implementation | [`Phase-6.status.md`](Phase-6.status.md), then the matching phase 7–9 status record |

## 2. Program rules

### State model

Use the same state vocabulary everywhere:

`proposed → implemented → validated → accepted`

- **Proposed** means the hypothesis, owner, next action, and gate exist.
- **Implemented** means the code or document change landed; its exit gate may still be open.
- **Validated** means the semantic, architecture, compatibility, and measurement evidence
  required by the item exists and is reproducible.
- **Accepted** means the owning item's objective exit gate passed. A performance or resource
  claim additionally passes [`Measurement.md`](Measurement.md) on the declared profiles.
- **Blocked** is non-terminal: the index names the unmet dependency and the event that makes
  the item actionable again.
- **Deferred** and **cancelled** are explicit terminal decisions. They are preferable to an
  indefinitely open speculative item.

An implemented item is not automatically validated, and a validated microbenchmark result
is not automatically an accepted engine-level claim.

### Required fields for every work package

Before work starts, record:

- one accountable maintainer and the owning assemblies or tooling area;
- the architectural requirement or measurable hypothesis;
- current evidence and the exact next action;
- the control arm and feature-disable or rollback path;
- the semantic, compatibility, and resource guardrails;
- the corpus, RID, CPU-feature, GC, and publish profiles that apply;
- the evidence destination and objective exit gate; and
- the decision that follows a pass, a failure, or a result below measurement resolution.

The tables below name an **owner area**. Scheduling an item requires replacing that area
with an accountable maintainer in the generated item index.

### Non-negotiable engineering invariants

1. **At most one executing thread per context.** Parallelism is across independent contexts
   or compile units, never simultaneous execution inside one `JSContext`. A general context
   may migrate only at a quiescent boundary; a Worker agent keeps its fixed owner thread.
2. **Semantics precede speed.** Any unexplained conformance, exception-order, observable
   initialization, or API difference is a regression regardless of throughput.
3. **The semantic front end is shared.** Parsing, early errors, binding, hoisting, scope,
   free-name analysis, and backend-neutral lowering are not duplicated between IL and
   bytecode.
4. **Dynamic code is an explicit profile boundary.** Only the IL profile may transitively
   reach `System.Reflection.Emit`; portable profiles cannot discover a backend by assembly
   name.
5. **Sequential execution remains available.** Concurrency and tiering have a deterministic
   single-threaded control and a fast disable path.
6. **Resource use is part of performance.** Time is reported with allocation, GC, working
   set, committed/virtual memory, threads, code/package size, and tail latency as applicable.
7. **Package moves preserve consumers deliberately.** Namespace preservation alone is not
   binary compatibility; assembly identity, type forwarding, package contents, and pristine
   consumers are gated.
8. **Plans do not copy volatile facts.** Plans name the command or evidence record that
   reads a count, graph, commit, test result, or benchmark score.

## 3. Dependency map

```mermaid
flowchart LR
    M0["M0 · Reconcile truth"]
    M1["M1 · Reproducible baseline"]
    M2["M2 · Achievable target graph"]
    M3["M3 · IL and AOT isolation"]
    M4["M4 · Package decomposition"]
    M5["M5 · Bounded compile-ahead"]
    M6["M6 · Optimizer-state isolation"]
    M7["M7 · Workers without shared memory"]
    M7B["M7B · Shared memory and Atomics"]
    M8["M8 · Profile-led optimization"]
    M9["M9 · Bytecode VM decision"]
    M10["M10 · Continuous governance"]

    M0 --> M1
    M0 --> M2
    M1 --> M3
    M2 --> M3
    M1 --> M4
    M2 --> M4
    M3 --> M4
    M1 --> M5
    M2 --> M5
    M5 -->|"artifact and shared-state classification"| M6
    M6 --> M7
    M2 --> M7
    M7 --> M7B
    M1 --> M8
    M2 --> M8
    M3 --> M9
    M4 --> M9
    M5 --> M9
    M8 -->|"finite VM decision bundle"| M9
    M0 --> M10
```

M1 and M2 are the first parallel work streams. M4 discovery spikes may overlap M3, but a
production move that changes the IL/AOT closure waits for the applicable M3 gate. M6 depends
on M5's artifact/shared-state classification, not on background compilation being a
performance success. M8 is continuous, so M9 waits only for a predeclared finite evidence
bundle covering front-end/startup, compile-ahead, package/AOT, and current-backend results;
each item in that bundle must be accepted, deferred, cancelled, or below resolution. M10
begins with M0 and remains active throughout.

### Portfolio overview

| Phase | Outcome | Relative size | Primary owner area |
|---|---|---:|---|
| **M0** | documentation, state, and landed-split truth agree | M | roadmap maintainers, Compiler, packaging |
| **M1** | stable, comparative, durable performance baselines | L | performance harness and CI |
| **M2** | an acyclic backend-neutral target graph | L | architecture, Parser, Compiler, Runtime |
| **M3** | enforceable IL/reflection/AOT boundaries | L–XL | Compiler backends, packaging, AOT |
| **M4** | smaller packages with measured deployment value | L–XL, divisible | Hosting, BuiltIns, packaging |
| **M5** | a measured decision on bounded background compilation | M–L | Compiler, Engine, embedding |
| **M6** | safe independent-context scaling and reclaimable feedback | L | Runtime, Engine, code cache |
| **M7/M7B** | Workers first; correct shared memory only as a later capability | XL, staged | Engine, Runtime, host integration |
| **M8** | current IL engine optimized from profiles, not catalogue labels | continuous S–L items | owning phase/assembly |
| **M9** | bytecode VM funded, narrowed, or cancelled explicitly | S–M decision; XL if approved | FrontEnd, backends, product owner |
| **M10** | drift detected automatically | S initially, continuous | docs, CI, release engineering |

---

## M0 — Establish one trustworthy current state

**Objective.** Reconcile the landed code, validation state, documented graph, open-item
inventory, and reproduction commands before using any plan as an implementation brief.

**Entry check.** Read [`AssemblySplit.status.md`](AssemblySplit.status.md), the generated
project graph, and the consumer/API baseline. M0 responds to whatever validation work those
sources show; their mutable state is not copied into this plan.

| ID | Work package and next action | Owner area | Size | Evidence target |
|---|---|---|---:|---|
| **M0-1** | Adopt the state model and create `eng/performance/roadmap-items.json` as the single machine-readable source for owner, state, blocker, next action, plan, and evidence links. Generate the compact index from it. | roadmap tooling | S | [`Roadmap.status.md`](Roadmap.status.md) plus generated data |
| **M0-2** | Regenerate the current project graph and assembly census from `.csproj` and source inputs; replace every stale “today” description with the generated view or a link to it. | architecture/tooling | S | [`Assemblies.status.md`](Assemblies.status.md) |
| **M0-3** | Run the split's S-7 conformance comparison manifest by manifest and classify every delta. The split remains `implemented, validation pending` until the comparison has a terminal result. | Compiler/conformance | M | [`AssemblySplit.status.md`](AssemblySplit.status.md) |
| **M0-4** | Add packaged source-consumer, previously compiled binary-consumer, public-API diff, package-content, and assembly-identity checks for the split. Decide explicitly whether type forwarding or a major-version break is required. | packaging/API | M | [`AssemblySplit.status.md`](AssemblySplit.status.md), [public API](../public-api.md) |
| **M0-5** | Reconcile Phase 1, Phase 3, Phase 4, assembly, and VM next actions with their status records; repair drifting item IDs and distinguish actionable, queued, gated, deferred, and accepted work. Correct stale polymorphic-cache and SIMD catalogue labels here. | roadmap maintainers | S | [`Phase-1.status.md`](Phase-1.status.md), [`Phase-3.status.md`](Phase-3.status.md), [`Phase-4.status.md`](Phase-4.status.md), and the owning records |
| **M0-6** | Fix reproduction paths, case-sensitive links, anchors, duplicated acceptance text, and ownership entries; add link, duplicate-ID, and stale-state checks to CI. | docs/CI | S–M | [`Roadmap.status.md`](Roadmap.status.md) |
| **M0-7** | Create and index `Concurrency.md` / `Concurrency.status.md`; move JavaScript-local parallel compilation and Worker implementation ownership there, and retain only cross-component integration dependencies at repository root. | JS and aggregate roadmap owners | S | new concurrency pair, [`Component.md`](Component.md), aggregate multithreading plan |
| **M0-8** | Immediately investigate the suspected `TypedArray.prototype.set` overlap/offset wrong-answer case: add the focused regression first and fix correctness if reproduced. This does not wait for M1; M8-5 owns only the optional bulk-copy performance follow-up. | BuiltIns/conformance | S | [`Component.md`](Component.md) and focused regression |

### M0 exit gate

- The generated project graph and the architecture/reference documents agree.
- S-7 has exact manifest-by-manifest equivalence with the pre-split baseline. Any intentional
  semantic fix is peeled into a separate change with its own gate; it is not absorbed into
  the assembly move's baseline.
- Public API, package contents, and representative source and binary consumers have a
  reproducible baseline.
- No document calls a landed project split “not started,” and no plan prescribes a next
  action already completed in its status record.
- Link, anchor, filename-case, duplicate-ID, and item-state checks pass on a case-sensitive
  CI filesystem.
- One index answers what is proposed, implemented, validated, accepted, blocked, deferred,
  or cancelled without reading the entire campaign history.

**Stop rule.** Do not start structural moves while the current graph or validation state is
disputed. An unexplained conformance or consumer delta blocks acceptance; it does not erase
the fact that the implementation landed.

---

## M1 — Make the performance baseline decision-grade

**Objective.** Turn the existing measurement protocol into a stable-hardware, same-machine
regression system that can reject real regressions and declare small effects unresolved.

**Depends on:** M0.

| ID | Work package and next action | Owner area | Size | Evidence target |
|---|---|---|---:|---|
| **M1-1** | Separate PR smoke from acceptance. Smoke proves wiring only; stable controlled hardware is the sole source of accepted performance claims. | CI/performance | S | [`Measurement.md`](Measurement.md) |
| **M1-2** | Provision declared acceptance lanes for Windows x64, Linux x64, and Linux Arm64. Record CPU, microcode, power/governor state, thermals, memory, OS, SDK, runtime, and publish settings. | CI/release | L | performance run manifest |
| **M1-3** | Execute applicable CPU-feature and GC arms: x64 feature on/off, AdvSimd-capable Arm64, workstation GC, and server GC. A claim is scoped only to arms actually run. | performance harness | M | raw run bundle |
| **M1-4** | Add a retained known-good control and run candidate/control in an interleaved order on the same host. Reject missing rows and deliberately seeded time or allocation regressions. | harness/CI | M | comparison report |
| **M1-5** | Enforce the configured noise rule. If same-commit controls exceed it, fail acceptance rather than widening the band after seeing a candidate. Recalibration is a separate control-only baseline change with its own evidence. | harness/CI | S | comparison report |
| **M1-6** | Run a JetStream 3 applicability spike and check in a compatible-test/exclusion manifest separating shell-compatible, browser-host, and Wasm-dependent cases. Add the supported subset, script-heavy cold startup/first-script/first-paint fixtures, independent-context scaling, and focused engine probes. Keep Octane as historical continuity, not the sole priority signal. | benchmarks/embedding | L | [`Roadmap.status.md`](Roadmap.status.md) and raw artifacts |
| **M1-7** | Capture process-wide allocation, RSS/working set, committed/virtual memory, GC pauses/collections, thread count, queue depth, and p50/p95/p99 where background work is involved. Retain thread-local allocation probes for single-thread microbenchmarks only. | diagnostics | M | EventPipe/runtime-counter bundle |
| **M1-8** | Version assembly and package baselines: direct/transitive edges, file and IL/metadata bytes, public types, package contents, publish bytes, loaded assemblies, cold context time, and working set. | packaging/performance | M | generated assembly metrics |
| **M1-9** | Store schema-versioned, checksummed, immutable summaries and raw BenchmarkDotNet, EventPipe, build, publish, and conformance artifacts for every supported release and, prospectively, at least the two previous accepted baselines. Bootstrap by rerunning two nominated compatible historical revisions on the controlled lanes; if one cannot build, record the incompatibility and retain continuously from the first accepted baseline. Short-lived CI artifacts are a cache, not the evidence store. | release engineering | M | durable artifact index |
| **M1-10** | Define the candidate decision before a run: primary metric and direction, minimum relevant effect or equivalence budget, paired analysis method, guardrail precedence, missing-row failure, and confirmation rerun. Treat the noise band as lane stability, not the regression threshold. | performance owners | S–M | [`Measurement.md`](Measurement.md), comparison schema |

### Required measurement arms

| Question | Minimum control arms | Primary measures | Guardrails |
|---|---|---|---|
| single-thread optimization | same build, feature off/on | wall time, throughput, allocation | conformance, GC, code/package size |
| cold startup or packaging | clean process, identical publish inputs | first-context/first-script, publish bytes, loaded files | working set, missing globals/features |
| background compile | background off, then 1, 2, 4, automatic worker budgets | critical-path time, queue/compile overlap, p50/p95/p99 | RSS, virtual memory, workers, GC, error order |
| independent contexts | 1, 2, 4 contexts and a serialized control | throughput and tail latency | wrong answers, cross-realm state, memory plateau |
| SIMD/intrinsics | x64 feature on/off and supported Arm64 | hot-loop throughput | fallback semantics, code size, startup |
| package split | monolith and each satellite composition | package/publish size, cold load, working set | public API, bootstrap surface, conformance |

Whenever a gate requires a **memory plateau**, its protocol is fixed before the candidate
run: warm-up point, number and size of rounds, idle/post-GC sampling procedure, allowed live
cache, and control-arm slope band. Passing means retained-live growth after warm-up stays
inside that band with no unexplained positive trend.

### M1 exit gate

- Two same-commit acceptance runs on every claimed lane fall within the configured baseline
  band, with cold and warm results reported separately.
- The pipeline detects an intentionally seeded timing/allocation regression and fails on
  absent or incomparable results.
- The predeclared candidate decision rule produces the expected accept/reject/below-resolution
  result and requires a confirmation run for an accepted change.
- Every result identifies the candidate and control revisions, machine, RID, CPU features,
  GC, profile, corpus, repetitions, and publish properties.
- Checksummed raw evidence covers every supported release and the available nominated
  historical baselines; the policy retains the two previous accepted baselines once they
  exist, and every published summary can be regenerated from raw evidence.
- Phase 0's outstanding baseline work can close without weakening
  [`Measurement.md`](Measurement.md).

**Stop rule.** If the control is noisier than the proposed effect, make no performance
claim. Stabilize the lane, increase samples, narrow the workload, or record the result as
below resolution.

---

## M2 — Prove an achievable assembly and semantic-front-end graph

**Objective.** Replace an aspirational assembly count with an acyclic graph and prove, with
project shells and a minimal backend test sink, that a shared semantic-front-end boundary is
feasible. Production bytecode work remains behind M9's go/no-go decision.

**Depends on:** M0. May run alongside M1.

The `.csproj` graph is authoritative, as described by the
[dependency rules](../architecture/dependencies.md). In particular, do not merge
Storage with the expression foundation while Storage reaches Ast, and do not merge Runtime
with Engine while Parser and Engine create a return edge. A lower assembly count is not a
success if it introduces cycles or hides useful ownership boundaries.

| ID | Work package and next action | Owner area | Size | Evidence target |
|---|---|---|---:|---|
| **M2-1** | Generate current and proposed `ProjectReference` graphs, including transitive closure, target profile, public types, package identity, and forbidden edges. | architecture/tooling | S–M | [`Assemblies.status.md`](Assemblies.status.md) |
| **M2-2** | Re-open A-0 as a real build spike: create target project shells, move no production code, and prove the graph restores and compiles before approving names or merges. | architecture | M | [`Assemblies.status.md`](Assemblies.status.md) |
| **M2-3** | Resolve the foundation cycle by preserving Expressions as a bottom model assembly or extracting a smaller Primitives/Model contract. Remove Storage's Ast dependency before considering a fold. | Storage/Ast/Expressions | M | [`Assemblies.status.md`](Assemblies.status.md) and graph ADR |
| **M2-4** | Inventory Parser→Runtime type use. Remove or invert the edge if feasible; otherwise preserve the boundary and revise the proposed tiers. Keep Runtime and Engine separate unless an independently measured deployment or startup result justifies a later merge. | Parser/Runtime/Engine | M | [`Assemblies.status.md`](Assemblies.status.md) and graph ADR |
| **M2-5** | Specify a backend-neutral `FrontEnd`/`Semantics` boundary for early errors, binding, scopes, hoisting, free-name and numeric-local analysis, and shared lowering. Prove the shape with project shells and a minimal fake/test backend; produce a priced extraction plan without migrating production bytecode. | Parser/Compiler | M–L | build spike, semantic fixtures |
| **M2-6** | Prove the LinqExpressions registration/compilation cut with a project-shell or file/type dependency spike. M3-3 owns the production move. | LinqExpressions/IL | S–M | graph and build evidence |
| **M2-7** | Design the backend contract against IL and the minimal test sink without exposing `DynamicMethod`, reflection-emitter, or realm-captured assumptions. Specify cache-key inputs; production bytecode adoption remains conditional on M9. | Compiler backends/Runtime | M | contract tests |
| **M2-8** | Create a project/assembly/package/namespace identity matrix, including intentional spelling repairs and type-forwarding decisions. | packaging/API | M | compatibility baseline |
| **M2-9** | Add architecture tests for acyclicity, tier direction, optional satellites, bytecode→IL prohibition, the one allowed Emit owner, public API, and assembly budgets. | architecture/CI | M | CI architecture suite |

### Recommended target boundaries

These are hypotheses to prove in M2, not pre-approved names:

| Boundary | Responsibility | Reason to keep or introduce it |
|---|---|---|
| **Primitives/Expressions model** | expression nodes and the smallest shared contracts | true bottom layer for Ast, Storage, Parser, Runtime, and both backends |
| **Ast** | syntax tree and syntax-only helpers | avoids pulling storage or runtime into parsing |
| **Parser** | text-to-Ast and syntax diagnostics | separately baselinable front-end cost |
| **Storage** | property-name and storage mechanics above the shared model | remains separate until its Ast edge is removed or isolated |
| **FrontEnd/Semantics** | binding, scope, hoisting, early errors, shared analyses and lowered IR | gives an approved VM one semantics source without beginning VM work in M2 |
| **Runtime** | values, objects, shapes, caches, job/runtime contracts | clearer state ownership and independent-context baseline |
| **Engine** | contexts, realms, bootstrap, execution coordination | keeps embedding/lifecycle separate from the object model |
| **BuiltIns and satellites** | core ECMAScript built-ins plus independently optional Temporal, Intl, and RegExp candidates | makes optional deployment cost measurable without changing core semantics |
| **IL** | IL lowering/emission, IL adapter, assembly code cache, and ILPack | one enforceable dynamic-code boundary |
| **Bytecode and Bytecode.Compiler** | interpreter/runtime and, only after M9, compiler lowering | mutually optional with IL and forbidden from depending on it |
| **Hosting abstractions** | context/bootstrap interfaces and backend-neutral composition hooks | usable without CLI, Roslyn, NuGet, or hard IL references |
| **CLI/composition** | command line, CSX/NuGet tooling, default backend selection | intentionally feature-rich and not an AOT foundation |
| **Composition profiles** | full IL, bytecode/AOT, and optional-feature meta-packages | make the supported transitive closures explicit and build-tested |

### M2 exit gate

- Proposed project shells compile as an acyclic graph and match the checked-in generated
  target graph.
- An IL adapter and minimal backend test sink compile against the proposed neutral contract;
  project-shell architecture tests prove that a future bytecode compiler need not reference
  IL. No production portable compiler migration is required before M9.
- Architecture tests enforce every allowed and forbidden edge.
- Every project, assembly, package, namespace, and public-type move has a compatibility
  disposition.
- An ADR records which folds were accepted, rejected, or deferred and why.

**Stop rule.** Reject a merge that creates a cycle or lacks a deployment, AOT, ownership,
or measurable baselining benefit. Prefer a folder/namespace boundary when a new assembly
would add packaging complexity without enforcing a useful contract.

---

## M3 — Isolate dynamic code and make Native AOT a publish-and-run property

**Objective.** Define the complete IL/reflection boundary, eliminate magic-name backend
discovery from portable profiles, and prove supported Native AOT capability by executing a
published application.

**Depends on:** M1 and M2. Crosswalk: A-3, A-4, A-6, and A-8. M3-5 is preliminary
evidence for A-7 and cannot close A-7's representative-script capability gate.

| ID | Work package and next action | Owner area | Size | Evidence target |
|---|---|---|---:|---|
| **M3-0** | Before publishing, declare the preliminary AOT graph-gate RID matrix from supported Native AOT targets and product needs. A later result may narrow support explicitly, but the candidate cannot choose its RIDs after the run. | product/AOT/CI | S | [`Assemblies.status.md`](Assemblies.status.md) |
| **M3-1** | Generate a full census of `Reflection.Emit`, `RequiresDynamicCode`, `RequiresUnreferencedCode`, `Assembly.Load`, assembly-qualified `Type.GetType`, generated/module registration, and equivalent discovery. Classify every site by target profile. | AOT/tooling | M | [`Assemblies.status.md`](Assemblies.status.md) |
| **M3-2** | Replace backend and module discovery by string with explicit or generated registration. Keep intentional CLR reflection in an explicitly excluded satellite with reviewed annotations. | Engine/Expressions/Modules | M–L | architecture tests |
| **M3-3** | Form the real IL boundary: emitter-specific Compiler code, Linq adapter, `AssemblyCodeCache`, and ILPack all belong below the IL composition. Correct any claim that the front-end split already isolated every Emit use. | IL/CLI packaging | L | [`Assemblies.status.md`](Assemblies.status.md) |
| **M3-4** | Assign and resolve every trim/AOT warning. Portable publish output must have zero trim/AOT warnings; any analyzer suppression is separately inventoried with owner, rationale, and reachability proof. | AOT owners | M | analyzer and publish logs |
| **M3-5** | Publish and run the current portable numeric sample on every M3-0 AOT RID with warnings treated as errors. Label this an **AOT graph gate**, not full JavaScript Native AOT support. | AOT/CI | M | publish-and-run bundle |
| **M3-6** | Add closure tests that fail if portable profiles transitively reach IL, Roslyn, NuGet scripting, ILPack, unapproved reflection, or name-based backend loading. | architecture/CI | S–M | CI architecture suite |

### M3 exit gate

- The dynamic-code/reflection inventory is generated, profile-classified, and has no
  unowned sites.
- Only shipped/runtime assemblies in the IL boundary reference `System.Reflection.Emit`;
  explicitly allowlisted test/build tools are reported separately.
- Portable profiles contain no magic-name backend/module discovery.
- The current supported portable sample publishes **and runs** on every M3-0 RID with
  zero trim/AOT warnings, and every analyzer suppression is inventoried and justified.
- CI verifies the transitive closure, analyzer results, publish result, and runtime result.
- Documentation describes the exact current subset; analyzer cleanliness is never called a
  full JavaScript engine capability.

A `narrow-go` or `full-go` in M9 extends this preliminary graph gate to a representative
script and host surface for the approved scope; a full-go also satisfies A-7's general-engine
intent. That post-decision work is owned by M9/Phase 6, not by M3's exit gate.

**Stop rule.** If a supported semantic feature requires unavoidable reflection, narrow and
document the AOT profile or move the feature to an excluded satellite. Do not silence a
warning or add an AOT checkmark in lieu of a working published sample.

---

## M4 — Decompose packages where the boundary has measurable value

**Objective.** Improve clarity and manageability while reducing optional deployment cost,
without turning assembly count into the goal.

**Depends on:** discovery spikes may start after M1 and M2; production moves that affect
the IL/AOT closure wait for M3's applicable boundary gate.

Use the [extraction pattern](../architecture/extraction-pattern.md) for every move.
Perform one split at a time so its API, conformance, startup, working-set, and package
effects remain attributable.

| ID | Work package and next action | Owner area | Size | Evidence target |
|---|---|---|---:|---|
| **M4-1** | Split backend-neutral hosting/context/bootstrap contracts from the executable CLI and its Roslyn, NuGet, CSX, default-backend, and command-line composition. | Hosting/CLI | M–L | [`Assemblies.status.md`](Assemblies.status.md), [public API](../public-api.md) |
| **M4-2** | Validate and baseline the LinqExpressions neutral/IL boundary produced by M3-3 as a packaging candidate. M3-3 owns the file move; M4 owns its package and lifecycle decision. | LinqExpressions/IL | S–M | [`Assemblies.status.md`](Assemblies.status.md) |
| **M4-3** | Create a separate dependency and generated-code spike for each BuiltIns candidate: Temporal, Intl, and RegExp. Inventory registries, internals, resources, generators, and public types before moving files; create a dedicated split plan/status pair only for an approved move. | BuiltIns/packaging | M | [`Assemblies.status.md`](Assemblies.status.md) |
| **M4-4** | Split satellites one by one. Preserve `Full`, `FullEager`, and `Minimal` observable bootstrap contracts and make absence/lazy-load behavior explicit. | BuiltIns/Engine | L each | conformance and packaging evidence |
| **M4-5** | Keep Runtime and Engine separately baselinable. Consider a fold only after the graph is clean and a measured outcome outweighs lost ownership clarity. | Runtime/Engine | S decision | ADR and M1 metrics |
| **M4-6** | Evaluate `Runtime.Interop`/CLR binding as an optional reflective boundary if the M3 census shows it materially simplifies the portable closure. | Runtime/CLR | M spike | graph/AOT evidence |
| **M4-7** | Run pristine source consumers, previously compiled consumers, API diff, package graph/content, type-forwarding, namespace, and host-composition tests for each accepted split. | packaging/API | M per split | package compatibility bundle |

### Per-split scorecard

| Dimension | Required comparison |
|---|---|
| correctness | focused regression, affected pinned test262 shard, repository suite, bootstrap/global snapshot |
| compatibility | source consumer, binary consumer or approved major-version decision, public API diff, package identity |
| deployment | package and publish bytes, file/assembly count, transitive dependency closure |
| lifecycle | cold startup, first context, first use of the satellite, unloaded/absent behavior |
| memory | peak and steady working set, loaded metadata, repeated context create/dispose plateau |
| manageability | explicit owner, architecture rule, coherent API surface, independent test target |

### M4 exit gate

- Every published package restores, builds, and runs from a pristine consumer.
- Full composition retains its documented global surface and conformance; reduced profiles
  state every omission.
- Public/binary compatibility is preserved or an explicit versioned migration is approved.
- Each new assembly enforces a useful dependency, deployment, AOT, ownership, or testing
  boundary and has an M1 baseline.
- Renames occur last, after the graph stabilizes, or are cancelled if their compatibility
  cost has no product value.

**Stop rule.** Do not split a satellite merely because it is large. If the lifecycle,
package, memory, testing, or ownership benefit is indistinguishable from noise or outweighed
by registration and consumer complexity, keep a logical boundary inside the existing
assembly.

---

## M5 — Prototype bounded compile-ahead without concurrent context execution

**Objective.** Decide whether independent-script background compilation improves a real
startup or first-paint critical path within explicit CPU, stack, memory, and determinism
budgets.

**Depends on:** M1 and M2. Cross-component scheduling integrates through the aggregate
multithreading plan; cache, compiler, and `JSContext` behavior are owned by the
`Concurrency.md` / `Concurrency.status.md` pair created in M0-7.

| ID | Work package and next action | Owner area | Size | Evidence target |
|---|---|---|---:|---|
| **M5-1** | Measure the opportunity first: separate fetch, parse, semantic analysis, IL emission, installation, execution, first-script, and first-paint critical paths on representative multi-script pages. | embedding/performance | M | [`Roadmap.status.md`](Roadmap.status.md) |
| **M5-2** | Make page/host cache ownership explicit by allowing an `ICodeCache` or cache factory through context options. Prove cache keys include every semantic input. | Engine/Runtime | M | `Concurrency.status.md` created by M0-7 |
| **M5-3** | Before scheduling concurrent work, classify compiler-touched mutable static/global state, registries, feedback switches, site allocation, and generated-delegate captures. Prove which artifacts are realm-neutral; if code captures a realm, share only source/parse/semantic IR and finalize installation at a quiescent context boundary. | Compiler/Runtime | M–L | `Concurrency.status.md` created by M0-7 |
| **M5-4** | After every M5-3 blocker is resolved or the artifact boundary is narrowed, implement one bounded scheduler for compilation. Derive its cap from core count and measured per-worker stack/memory high-water; do not layer unbounded `Task.Run` work over the existing large-stack compiler workers. | Compiler/host scheduler | M | scheduler tests and metrics |
| **M5-5** | Start with immutable external classic scripts whose bytes, location, mode, backend, and options are final. Preserve document/source execution order on the owning realm thread. | loader/Compiler | M | integration tests |
| **M5-6** | Make concurrent same-key requests single-flight for the lifetime and generation of a live cache entry; allow unrelated keys to progress. Define retry/generation behavior so failed, cancelled, invalidated, or evicted entries may compile again. Preserve syntax-error type, message, location, and observable reporting order. | code cache/Compiler | M | deterministic stress suite |
| **M5-7** | Instrument queue time, compile time, critical-path overlap, active/parked workers, stack mappings, duplicate waits, cancellations, cache hits/evictions, process allocation, RSS, GC, and p50/p95/p99. | diagnostics | M | M1 run bundle |
| **M5-8** | Compare background-off, 1, 2, 4, and automatic worker budgets in cold and warm runs. Background-off is the permanent exact control; one worker separately measures queue and handoff overhead. | performance harness | S–M | paired comparison report |

### M5 exit gate

- Background-off and parallel arms have identical program results, execution order, error
  type/message/location/reporting order, and applicable test262 results.
- A per-context entry detector reports a peak of one for every context; only independent
  compile units overlap.
- Concurrent same-key requests perform one bounded compilation per live entry generation,
  unrelated keys progress, and failed/cancelled/invalidated/evicted generations retry only
  under the documented policy.
- The user-visible primary metric improves beyond the predeclared noise/resolution rule on
  a representative workload; compile-stage timing alone is insufficient.
- Peak RSS, committed/virtual memory, worker retention, GC, and p95/p99 remain within the
  host's declared budgets on all enabled RIDs.
- The feature is opt-in until the full gate passes and always has a synchronous disable path.

**Stop rule.** Stop after M5-1 if compilation is not material to the critical path. Narrow
the experiment to parse/semantic IR if code generation is realm-bound. Disable or reduce
parallelism if memory or tail latency exceeds budget. Never multiply embedding workers by
compiler workers without one host-wide cap.

If M5 is a performance no-go, record that decision and continue M6 from M5-3's artifact and
shared-state classification; independent-context safety does not depend on compile-ahead
shipping.

---

## M6 — Make optimizer state safe for independent-context scaling

**Objective.** Remove process-shared mutable feedback as a correctness, contention, and
retention hazard before sharing compiled artifacts or advertising parallel contexts.

**Depends on:** M1 and M5-3's artifact/shared-state classification only, not M5's
performance outcome.

| ID | Work package and next action | Owner area | Size | Evidence target |
|---|---|---|---:|---|
| **M6-1** | Extend and verify M5-3's census across independent-context execution. Classify every remaining mutable static/global as immutable process data, synchronized process data, compiled-artifact state, realm-local state, or diagnostics, including shapes, inline-cache site tables, feedback counters, registries, queues, and delegates. | Runtime/Engine audit | M | `Concurrency.status.md` created by M0-7 |
| **M6-2** | Move inline-cache entries and site ownership to the realm, compiled function, or another lifetime that cannot mix semantic feedback across contexts. Synchronization alone is insufficient if feedback remains cross-realm. | Runtime/Compiler | L | focused IC tests |
| **M6-3** | Replace or isolate static non-thread-safe type-feedback tables; define snapshot, publication, invalidation, and eviction behavior. | Runtime/Compiler | L | feedback tests and metrics |
| **M6-4** | Reclaim IC, feedback, delegate, source, and host-capture state with its context/function/cache entry. Add weak-reference and repeated create/evict/dispose checks. | Runtime/cache | M | lifetime/soak suite |
| **M6-5** | Stress 1, 2, and 4 independent contexts on separate owner threads with shared and unshared code-cache configurations; compare every result to serialized execution. | Engine/testing | M | randomized and long-soak runs |
| **M6-6** | Measure scaling, p95/p99, contention, cache hit rate, allocations, RSS, GC, and memory plateau. Attribute any loss before adding more locks or sharing. | performance/diagnostics | M | M1 run bundle |
| **M6-7** | Only after M6-1 through M6-6 and M6-8, prototype background tier-up from a quiescent-context snapshot with installation at a quiescent context boundary and a preserved original delegate fallback. | Compiler/Engine | M experiment | [`Phase-4.status.md`](Phase-4.status.md) and `Concurrency.status.md` |
| **M6-8** | Inventory every host, promise/job, generator/async, timer, module, and callback entry into JavaScript. Add an architecture/coverage test proving each route passes through the context's exclusion and dispatch policy. | Engine/host integrations | M | `Concurrency.status.md` created by M0-7 |

### M6 exit gate

- Parallel contexts produce the same results as serialized execution across repeated
  randomized stress and long-soak runs.
- Mutable optimization state has explicit semantic ownership and lifetime; it is not merely
  protected from torn writes.
- Evicted functions and disposed contexts release feedback, delegates, source, and cache
  state, and the soak reaches a stable memory plateau.
- Throughput and p95/p99 scaling are reported with contention and resource costs for 1, 2,
  and 4 contexts.
- No process-shared generated code captures realm-specific mutable objects unless the share
  is explicitly rejected and tested.
- Every asynchronous continuation and host entry is covered by the context exclusion and
  dispatch-policy test.

**Stop rule.** Do not advertise concurrent contexts, shared delegates, or background tier-up
while feedback can cross realms or retained memory grows with evicted contexts. If safe
sharing is not beneficial, keep compiled artifacts realm-local and parallelize only fully
isolated contexts.

---

## M7 — Add Workers as isolated agents; defer shared memory

**Objective.** Deliver useful Worker capability with one context and one event loop per
agent before attempting the ECMAScript shared-memory model.

**Depends on:** M2 and M6. M3 is required only for a Worker composition that itself claims
Native AOT. JavaScript owns agent/context semantics; HTML/loading owns URL, document, and
browser integration.

### M7A — Workers without shared memory

| ID | Work package and next action | Owner area | Size | Evidence target |
|---|---|---|---:|---|
| **M7-1** | Define the agent abstraction: one context, realm set, job queue, fixed owner thread, error channel, scheduler budget, and explicit shutdown/drop policy. | Engine/Runtime | L | `Concurrency.status.md` created by M0-7 |
| **M7-2** | Implement structured clone for the supported value graph, including cycles and errors, with explicit rejection of host objects that lack clone semantics. | Runtime/BuiltIns | L | clone conformance suite |
| **M7-3** | Add transferable `ArrayBuffer` only after ownership, detachment, and failed-transfer atomicity tests pass: a rejected clone cannot partially detach the transfer list. No realm object crosses an agent boundary directly. | Runtime/BuiltIns | M | transfer tests |
| **M7-4** | Implement Worker lifecycle and messaging: create, options, FIFO delivery where specified per port/task source, permitted interleavings across task sources, error propagation, close, cancellation, queued-work drop/drain policy, and termination. | Engine/host integration | XL | Worker tests and WPT scope |
| **M7-5** | Enforce a host-wide worker/context budget and measure startup, throughput, p95, peak worker count, RSS, committed/virtual memory, stack reservations, and repeated create/terminate memory plateau. | scheduler/performance | M | M1 run bundle |
| **M7-6** | Keep cross-agent `SharedArrayBuffer` and Atomics unavailable in the initial Worker profile. Test the rejection explicitly. | Runtime/API | S | capability tests |

### M7A exit gate

- Each Worker owns one context/event loop; specified FIFO relations hold, permitted
  cross-task-source interleavings are accepted, and the documented error, close,
  cancellation, drop/drain, and termination policies hold under stress.
- Structured clone handles cyclic graphs, a successful transfer detaches the sender exactly
  once, and a failed transfer makes no partial detachment visible.
- Applicable Worker and structured-clone WPT plus repository tests pass at multiple worker
  counts; test262 is used only for applicable ECMAScript realm/agent semantics.
- Repeated creation/termination reaches a stable memory plateau and respects the host cap.
- No shared-memory feature is exposed across agents.

**Stop rule.** If lifecycle cleanup, per-source FIFO, or event-loop ownership violates its
specified policy, keep the feature experimental. Workers are a capability and throughput
feature; do not claim that they accelerate one JavaScript program automatically.

### M7B — SharedArrayBuffer and Atomics as a separate high-risk phase

Start M7B only when a product requirement survives a written cost/risk decision. The current
single-agent storage and simulated Atomics behavior are not a foundation that may simply be
shared between contexts.

| ID | Work package and next action | Owner area | Size | Evidence target |
|---|---|---|---:|---|
| **M7B-1** | Design a shared backing store with the required no-tear element accesses, lifetime, growth synchronization, and agent ownership. | Runtime/memory model | XL | `Concurrency.status.md` created by M0-7 |
| **M7B-2** | Implement real atomic load/store and read-modify-write operations with ECMAScript ordering for the integer typed-array element types on which Atomics are valid; specify compliant non-atomic shared accesses separately. | Runtime/BuiltIns | XL | litmus and test262 suite |
| **M7B-3** | Implement waiter lists, timeouts, `wait`, `notify`, applicable async waiting, `AgentCanSuspend` and main-agent restrictions, termination cleanup, and growth races that preserve waiters at still-valid offsets. | Engine/Runtime | XL | waiter stress suite |
| **M7B-4** | Run message-passing, happens-before, no-tear, RMW, high-contention, timeout, termination, and growth correctness on every claimed OS/RID, with long stress on representative x64 and Arm64 machines. | testing/performance | L | durable stress bundle |

### M7B exit gate

- Applicable test262 plus repository memory-model litmus/stress tests pass repeatedly on
  every claimed OS/RID, with long x64 and Arm64 runs.
- Atomicity and ordering hold for every Atomics-valid integer typed-array element type;
  ordinary shared accesses meet their separately specified no-tear rules.
- Growth preserves waiters at still-valid offsets; timeout, notification, agent termination,
  and backing-store retirement clean their waiters without leaks.
- `Atomics.wait` obeys `AgentCanSuspend` and main-agent behavior.
- The capability remains feature-gated until the entire gate passes.

**Stop rule.** A lock around existing plain read/compute/write operations is not proof of
ECMAScript Atomics semantics. If the backing store cannot guarantee the required ordering
and element-width behavior, redesign it or keep shared memory unavailable.

---

## M8 — Run profile-led optimization packages on the current engine

**Objective.** Finish the current IL engine's measurable opportunities before committing to
a second execution engine, using catalogue state reconciled by M0 rather than trusting a
stale implementation label.

**Depends on:** M1. Boundary-changing items also depend on M2; shared-state items depend on
M6.

Every package follows the same loop:

`opportunity census → correctness fixture → control switch → implementation → conformance → paired acceptance → accept/defer/remove`

| ID | Initial package and next action | Owner area | Size | Evidence target |
|---|---|---|---:|---|
| **M8-1** | Read the current Phase 1 evidence and finish its remaining lazy function-body deferral action; measure startup and retained-source/capture cost. | Parser/Compiler | L | [`Phase-1.status.md`](Phase-1.status.md) |
| **M8-2** | Resolve the fixed call-envelope soundness question before widening speculative eligibility. | Compiler/Runtime | M–L | [`Phase-4.status.md`](Phase-4.status.md) |
| **M8-3** | Reduce the RegExp per-call envelope while preserving observable last-match and RegExp semantics. | BuiltIns/Regex | M | [`Phase-5.status.md`](Phase-5.status.md) |
| **M8-4** | Continue storage redesign only if the current evidence plus the remaining live-memory case show an attainable benefit above measurement resolution. | Storage/Runtime | XL or cancel | [`Phase-3.status.md`](Phase-3.status.md) |
| **M8-5** | After M0-8's independent correctness result, benchmark an optional overlap-safe bulk byte-copy fast path. Restrict raw copying to identical element types on fixed, non-shared buffers unless M7B supplies compliant shared-memory access. | BuiltIns | S–M | focused regression and [`Roadmap.status.md`](Roadmap.status.md) |
| **M8-6** | With the catalogue state corrected in M0, measure bounded polymorphic-cache coverage, misses, contention, and retention. Consider a megamorphic cache only if profiles show a material population. | Runtime | M experiment | [`Phase-4.status.md`](Phase-4.status.md) |
| **M8-7** | With the SIMD label corrected in M0, prototype explicit intrinsics only when a contiguous bulk operation dominates a representative profile; include feature-off x64 and AdvSimd Arm64 controls. | BuiltIns/Runtime | M experiment | [`Roadmap.status.md`](Roadmap.status.md) |
| **M8-8** | Compare DynamicMethod and collectible-assembly modes for cold compile, warm throughput, tiering/PGO, unloadability, code size, and memory before changing the default backend. | IL/Compiler | M | [`Roadmap.status.md`](Roadmap.status.md) |
| **M8-9** | Review the catalogue after each accepted item: actual code state, measured population, attainable ceiling, owner, gate, and terminal decision. | performance roadmap | S continuous | [`Roadmap.md`](Roadmap.md) / [`Roadmap.status.md`](Roadmap.status.md) split |

### M8 exit gate for each package

- The entry census identifies a population, cost attribution, attainable ceiling, semantic
  owner, primary metric, and resource guardrails.
- A focused regression exists before changing a possibly incorrect fast path.
- Repeated paired candidate/control measurements show an effect beyond the configured
  resolution on the declared workload and RIDs.
- Applicable conformance, test262, API, allocation, memory, GC, code-size, and package
  guardrails pass.
- The implementation is accepted, remains explicitly opt-in, is deferred, or is removed;
  failed experimental switches do not accumulate.

**Stop rule.** Cancel before implementation when the measured upper bound is inside noise or
the targeted population is absent. Keep Octane for historical comparison, but prioritize
modern shell and product-level workloads. A microbenchmark win alone is not an engine-level
claim. This performance cancellation rule never suppresses a reproduced correctness fix;
ship the fix with its semantic gate even when its speed effect is below resolution.

---

## M9 — Make the bytecode VM a terminal go/no-go decision

**Objective.** Decide whether the product needs a general bytecode engine after the IL path,
assembly graph, AOT boundary, startup work, and compile-ahead evidence are known.

**Initial decision size:** S–M. **Implementation size if approved:** XL and multi-release.

**Depends on:** M2, M3, and a finite decision bundle: the M5 compile-ahead decision, M4/M3
package and AOT evidence, M8-1 front-end/startup outcome, and M8-8 current-backend comparison.
Each must be accepted, deferred, cancelled, or below resolution; M9 does not wait for the
continuous remainder of M8. Crosswalk: item 6-0 and [`Phase-6.md`](Phase-6.md) through
[`Phase-9.md`](Phase-9.md).

| ID | Decision work and next action | Owner area | Size | Evidence target |
|---|---|---|---:|---|
| **M9-1** | Before measuring, name the dynamic-code-prohibited platforms and product scenarios, capability must-haves, decision thresholds, staffing/maintenance ceiling, and precedence when capability, conformance, startup, package, and memory criteria disagree. | product/AOT | S | [`Phase-6.status.md`](Phase-6.status.md) |
| **M9-2** | Measure the representative surface and workload after accepted IL/startup work: language constructs, host APIs, startup, package/code memory, and execution constraints. | performance/conformance | M | [`Phase-6.status.md`](Phase-6.status.md) |
| **M9-3** | Evaluate supported alternatives such as the IL profile, ReadyToRun/persisted artifacts where applicable, or a deliberately narrow portable subset. | architecture/product | S–M | [`Phase-6.status.md`](Phase-6.status.md) and decision ADR |
| **M9-4** | Verify M2's contract/test-sink feasibility and price production extraction without implementing bytecode lowering before the decision. Include hard semantics first: exceptions/finally, generators, async, modules, direct eval, debugging, and host interop. | FrontEnd/backends | M | prototype and estimate |
| **M9-5** | Publish one ADR with a terminal `no-go`, `narrow-go`, or `full-go`, named ownership, conformance scope, resource thresholds, and maintenance budget. | product/architecture | S | [`Phase-6.status.md`](Phase-6.status.md) |

### Decision outcomes

- **No-go.** Use when AOT is not a supported product requirement, the IL path is available
  on target platforms, or the measured capability benefit does not justify maintaining a
  second engine. Mark phases 6–9 cancelled or deferred with a reopening condition.
- **Narrow-go.** Define a constrained language and host profile for a specific AOT product.
  Never describe it as general JavaScript support.
- **Full-go.** Fund the existing phases with named maintainers. Reorder their execution so a
  dual-arm conformance harness and the shared-front-end contract precede format/compiler
  expansion.

If approved, execute in this order:

1. dual-arm IL/bytecode conformance harness;
2. versioned bytecode format and verifier, including source/exception/suspension metadata;
3. production extraction of the shared semantic front end, then bytecode lowering from it;
4. correct interpreter loop and slow paths;
5. exception and `finally` semantics;
6. generators, async suspension, modules, eval, debugging, and host interop;
7. real-script Native AOT publish-and-run gate;
8. profiles and fast paths; and
9. only then quickening, persistence, threaded dispatch, OSR, deoptimization, or adaptive
   tiering.

### M9 exit gate

The ADR names target platforms, product requirement, representative workloads, conformance
scope, frontend-reuse evidence, the thresholds and staffing ceiling predeclared in M9-1,
their observed results, and one terminal outcome under the predeclared precedence rules. The
VM track may not remain indefinitely “open but unscheduled.”

**Stop rule.** Forked semantic analysis is a no-go until the shared boundary is fixed. Do not
justify a full VM using the capability or size of the existing numeric-only portable subset,
and do not describe a bytecode interpreter as an IL-path speed-up.

---

## M10 — Make roadmap and architecture drift mechanically visible

**Objective.** Prevent the same graph, status, command, API, and benchmark contradictions
from returning.

**Starts after:** M0 and continues across every phase.

| ID | Work package and next action | Owner area | Size | Evidence target |
|---|---|---|---:|---|
| **M10-1** | Run the M0-1 generator in CI: render the read-only item index from `eng/performance/roadmap-items.json` alongside the current project graph, assembly/package metrics, public API baseline, and target-profile closure. | docs/architecture tooling | M | generated artifacts |
| **M10-2** | Run Markdown link/anchor/case checks, duplicate item-ID checks, forbidden-edge tests, API diff, package-content tests, and AOT publish/run gates on appropriate changes. | CI | M | required checks |
| **M10-3** | Require every roadmap state transition to include its evidence link, accountable maintainer, next decision, and rollback/disable path. | review policy | S | PR template/lint |
| **M10-4** | Re-certify baselines when hardware, OS, SDK/runtime, compiler backend, GC, CPU-feature policy, benchmark corpus, or publish settings change. Never compare incompatible eras silently. | performance/release | S continuous | baseline manifest |
| **M10-5** | Review benchmark relevance and unowned catalogue entries on a fixed cadence. Retain historical corpora for continuity but add/remove prioritization workloads deliberately. | performance owners | S continuous | [`Roadmap.status.md`](Roadmap.status.md) |
| **M10-6** | Produce a release evidence bundle containing conformance manifests, API/package diffs, graph, AOT closure/run, performance comparisons, and known unsupported profiles. | release engineering | M per release | durable release bundle |
| **M10-7** | Archive superseded delivery narrative after durable decisions are folded into current architecture/support docs; do not leave completed histories mixed with instructions. | docs owners | S continuous | Git history and current docs |

### M10 exit gate

A new contributor can answer from one index:

- what is proposed, implemented, validated, accepted, blocked, deferred, or cancelled;
- which evidence supports the state;
- which assembly or maintainer owns the next action;
- which profiles and consumers are supported; and
- which decision comes next.

CI fails when the documented graph, package/API surface, AOT closure, links, or item identity
drifts from the generated truth.

---

## 4. Program-level acceptance matrix

| Change class | Correctness | Architecture/compatibility | Performance/resource evidence |
|---|---|---|---|
| documentation/state | state-source consistency, link and ID lint | generated graph/API references agree | none; no result claims in plan prose |
| compiler optimization | focused regression, owning pinned test262 manifests, full affected shard | sequential fallback, cache-key compatibility | paired stable-host time plus allocation/GC/code-size guardrails |
| assembly/package move | repository suite and manifest-by-manifest conformance equivalence | graph, API diff, source/binary consumers, package contents | publish size, startup, loaded files, working set |
| portable/AOT change | representative program output and supported conformance scope | no forbidden transitive edge or magic-name discovery | analyzer plus actual publish-and-run on each claimed RID |
| background compilation | exact result, execution/error reporting order, cancellation, per-context peak one | bounded scheduler and realm-neutral artifact proof | background-off/1/2/4/auto critical path, p50/p95/p99, RSS/virtual memory/GC |
| independent contexts | randomized and soak equivalence to serialized control | realm-owned feedback and reclaimable cache state | scaling, contention, tail latency, memory plateau |
| Worker capability | clone/transfer/message/lifecycle tests and WPT scope | one context/event loop per agent, explicit unsupported surface | startup, throughput, worker/RSS cap, teardown plateau |
| shared memory | test262 plus no-tear/order/waiter litmus and stress | shared backing-store and agent-lifetime model | every claimed OS/RID, with long x64/Arm64 stress; throughput is secondary |

## 5. Milestones

| Milestone | Required phases | Meaning |
|---|---|---|
| **T — Trustworthy** | M0 | the documentation and implementation describe the same engine |
| **B — Baselinable** | M1 | regression decisions and claims are reproducible |
| **A — Architecturally enforceable** | M2 + M3 | graph, backend, reflection, and AOT rules are build-checked |
| **P — Package-manageable** | accepted M4 packages | decomposition has consumer and deployment evidence |
| **C — Concurrency-ready** | M5 + M6 | compile-ahead is decided and independent contexts are safe |
| **W — Worker-capable** | M7A | isolated agents work without pretending shared memory is complete |
| **S — Shared-memory capable** | optional M7B | the ECMAScript memory model is implemented and stress-validated |
| **O — Optimized current engine** | accepted M8 packages | the IL path receives profile-led work first |
| **V — VM decision** | M9 | phases 6–9 are funded, narrowed, or closed explicitly |
| **G — Governed** | M10 | drift and unsupported claims fail mechanically |

## 6. Program stop conditions

Stop and re-plan when any of these occurs:

- conformance or observable host behavior changes without an understood specification reason;
- a proposed assembly graph is cyclic or a bytecode profile references IL;
- a performance relationship cannot reproduce against its same-machine control;
- host-wide concurrency is unbounded or a context has more than one executing entrant at a
  time; quiescent migration remains allowed for non-Worker contexts;
- an AOT profile reaches Emit or undocumented dynamic loading;
- a second backend begins duplicating semantic analysis;
- mutable optimizer feedback crosses realms without semantic and lifetime proof;
- a new assembly has no deployment, AOT, ownership, testing, or measurable management value;
- a benchmark-specific improvement exceeds a predeclared representative-workload guardrail;
  an unresolved delta inside the control band is reported as below resolution, not a known
  regression; or
- SharedArrayBuffer becomes visible across agents before the memory-model gate passes.

## 7. External design and measurement references

These references inform the gates; they do not replace repository conformance or performance
evidence:

- [.NET Native AOT deployment](https://learn.microsoft.com/dotnet/core/deploying/native-aot/)
- [BenchmarkDotNet diagnosers](https://benchmarkdotnet.org/articles/configs/diagnosers.html)
- [V8 background compilation](https://v8.dev/blog/background-compilation)
- [V8 isolate API](https://v8.github.io/api/head/classv8_1_1Isolate.html)
- [WHATWG Workers](https://html.spec.whatwg.org/multipage/workers.html)
- [WHATWG structured data and structured clone](https://html.spec.whatwg.org/multipage/structured-data.html)
- [ECMAScript shared-memory model](https://tc39.es/ecma262/multipage/memory-model.html)
- [JetStream 3](https://browserbench.org/announcements/jetstream3/)
- [Why V8 retired Octane](https://v8.dev/blog/retiring-octane)
