# Broiler.JS roadmap

Every open plan for this engine, and the rules that decide when one of them may close.

## The convention: `X.md` is the plan, `X.status.md` is the evidence

Every evidence-owning delivery plan in this directory is split in two, and the split is
the point. Cross-track orchestration and reference documents are listed separately below;
they own no implementation state:

- **`X.md` — the plan.** What each item *is*, why it is worth doing, what to do next, how
  big it is, which assemblies own it, and the gate it closes against. A state is one line
  with a link. **This is the document to act from.**
- **`X.status.md` — the record.** What was built, what it cost, what was measured, what was
  refuted, and every number. **This is the document to check a claim against.**

The campaign has re-specified items on their own measurements often enough that mixing the
two made the plan unreadable: an item's next action was buried three screens inside the
narrative of why its previous three next actions were wrong. Both halves are still here in
full — nothing was dropped — but you can now read the plan without reading the campaign.

## The documents

| Subject | Plan | Evidence |
|---|---|---|
| **The performance campaign** | [`Roadmap.md`](Roadmap.md) — scope, metrics, historical campaign crosswalk, optimization catalogue, non-goals, traceability | [`Roadmap.status.md`](Roadmap.status.md) — dated evidence snapshots and cross-phase status; modernization sequencing is owned below |
| **Phase 0** — establish the baseline | [`Phase-0.md`](Phase-0.md) | [`Phase-0.status.md`](Phase-0.status.md) |
| **Phase 1** — the front end | [`Phase-1.md`](Phase-1.md) | [`Phase-1.status.md`](Phase-1.status.md) |
| **Phase 2** — the call and property paths | [`Phase-2.md`](Phase-2.md) | [`Phase-2.status.md`](Phase-2.status.md) |
| **Phase 3** — value representation | [`Phase-3.md`](Phase-3.md) | [`Phase-3.status.md`](Phase-3.status.md) |
| **Phase 4** — speculation | [`Phase-4.md`](Phase-4.md) | [`Phase-4.status.md`](Phase-4.status.md) |
| **Phase 5** — RegExp | [`Phase-5.md`](Phase-5.md) | [`Phase-5.status.md`](Phase-5.status.md) |

Those are **track one** — the IL execution path, where the campaign's measured history is.
**Track two** is now the JavaScript built-in profile for Broiler.VM. The generic profile host,
static built-in catalog, common lifecycle/resource contracts, WebAssembly built-in, and future
profile-extension gate are owned by `Broiler.VM/docs/roadmap.md` in the aggregate repository.
The numeric portable runtime/compiler seed and Native AOT execution-only sample already exist,
but **none of the production Phase 6–9 JavaScript-profile items has started**. Modernization
MOD-M9 selects the JavaScript deployment/compiler composition—`execution-only`,
`narrow-runtime-compiler`, or `general-runtime-compiler`—and cannot cancel Broiler.VM or its
WebAssembly profile.

| Subject | Plan | Evidence |
|---|---|---|
| **Phase 6** — JavaScript profile 1.0: correctness | [`Phase-6.md`](Phase-6.md) | [`Phase-6.status.md`](Phase-6.status.md) — seed census only; zero production items/measurements |
| **Phase 7** — JavaScript profile 2.0: make the approved interpreter shippable | [`Phase-7.md`](Phase-7.md) | [`Phase-7.status.md`](Phase-7.status.md) — no accepted baseline |
| **Phase 8** — JavaScript profile 3.0: measured optimization and persistence | [`Phase-8.md`](Phase-8.md) | [`Phase-8.status.md`](Phase-8.status.md) — no accepted adaptive/persistence item |
| **Phase 9** — JavaScript profile 4.0: optional IL/bytecode adaptivity | [`Phase-9.md`](Phase-9.md) | [`Phase-9.status.md`](Phase-9.status.md) — no tier/deopt/OSR feasibility decision |

**Why the JavaScript profile exists at all:** Broiler.JS has **no general JavaScript execution
path on a platform that forbids `System.Reflection.Emit`**. The compiler back end is an IL
writer; the current portable path is a deliberately limited numeric subset. That is a
*capability* gap, not a performance one. Broiler.VM being able to execute WebAssembly does not
close it. A future Phase 6 may create a reconstructable JavaScript frame ABI, but
deoptimization still requires explicit state, invalidation, materialization, reconstruction,
and correctness gates in the JavaScript-only Phase 9. The historical argument is
[`Roadmap.md` § Track two](Roadmap.md#track-two--the-vm-tier-phases-69).

**Capability and performance are separate decisions.** Phase 6 makes no speed claim.
Startup, throughput, memory, package size, and any IL comparison close independently under
MOD-M1; do not call a bytecode result a win merely because it runs where IL also runs.

**The assembly plan gates the JavaScript integration**, while JavaScript concurrency has its own delivery
pair for modernization MOD-M5–MOD-M7:

| Subject | Plan | Evidence |
|---|---|---|
| **The assembly restructure** — backend-neutral foundations / `.IL` / `.Bytecode` / optional packages | [`Assemblies.md`](Assemblies.md) | [`Assemblies.status.md`](Assemblies.status.md) — initial graph work implemented; target-graph and validation work remains |
| **The `ExpressionCompiler` split** — the restructure's first executable piece | [`AssemblySplit.md`](AssemblySplit.md) | [`AssemblySplit.status.md`](AssemblySplit.status.md) — implementation landed; final validation remains open |
| **JavaScript concurrency** — bounded compile-ahead, independent-context safety, and Worker agents | [`Concurrency.md`](Concurrency.md) | [`Concurrency.status.md`](Concurrency.status.md) — existing aggregate-repository slices mapped; MOD-M5–MOD-M7 acceptance remains open |

**Why the assembly work exists:** the model/emitter split removed the front-end consumers' direct dependency
on the IL emitter. The remaining restructure must prove an acyclic backend-neutral semantic
front end, isolate every runtime Emit dependency in the IL backend boundary, preserve consumers, and
make a bytecode-only Native AOT composition a publish-and-run property.

Cross-cutting orchestration and reference documents sit above or beside the two tracks;
delivery ownership still uses a plan/status pair. `Modernization.md` is the orchestration
authority rather than an independent evidence ledger, while `Concurrency.md` is an owning
delivery plan. Changing evidence belongs in the matching status record:

| | |
|---|---|
| [`Modernization.md`](Modernization.md) | **The cross-track execution roadmap.** It orders the audit cleanup, trustworthy baselines, an achievable assembly graph, IL/AOT isolation, package decomposition, bounded compile-ahead, independent-context safety, Workers, profile-led optimization, and the JavaScript VM composition decision. It owns dependencies and program gates; changing evidence remains in the linked `*.status.md` records. |
| [`ModernizationDelivery.md`](ModernizationDelivery.md) | **The separate phased delivery view.** It maps executable increments and handoffs to the authoritative `MOD-M*`, phase, assembly, and concurrency gates without creating another state ledger. |
| [`Measurement.md`](Measurement.md) | **The gate, and §3 of the campaign.** What may be *claimed* — evidence classes, immutable candidate/control identity, A/A lane validity, practical decision thresholds, exact-row comparison, resource/conformance guardrails, the RID matrix and bootstrap profiles — plus the standing lessons (§3.5) and every probe's command line (Appendix A). **It governs everything above.** It is not split because it is all rules: it has no status. |
| [`Concurrency.md`](Concurrency.md) | **The JavaScript-local owner for MOD-M5–MOD-M7.** It owns compiler/cache/context safety, optimizer-state lifetime, and Worker-agent acceptance. The aggregate `docs/architecture/multithreading.md` retains integration history and host-level measurements; a host feature recorded there as built is not thereby accepted against MOD-M5–MOD-M7. |
| [`Component.md`](Component.md) | The engine's non-performance roadmap: closing the supported test262 failure set, expanding host-mode coverage, finishing RegExp backend adoption, and API/package/preview readiness. |
| [`Archive.md`](Archive.md) | **Both** superseded plans — the engine campaign (P0–P3, phases A–F) and the Octane roadmap (phases 0–5) — merged into `Roadmap.md` on 2026-08-01 and **not back-ported**. Kept for measurements and defect narratives. Where sources disagree, the owning current phase plan/status pair and `Modernization.md` take precedence over `Roadmap.md`, which takes precedence over the archive. |

## Where to start

| If you are… | Read |
|---|---|
| **planning work that crosses performance, assemblies, AOT, or concurrency** | [`Modernization.md`](Modernization.md) for authority and stop/go gates, [`ModernizationDelivery.md`](ModernizationDelivery.md) for delivery waves, then the linked owning plan and its status record. |
| **working on compile-ahead, context isolation, or Workers** | [`Concurrency.md`](Concurrency.md), then [`Concurrency.status.md`](Concurrency.status.md). Use the aggregate multithreading document only for integration history and its host-side measurements. |
| **picking up performance work** | [`Modernization.md`](Modernization.md) for cross-track order, [`Roadmap.md`](Roadmap.md) for the campaign crosswalk, then the owning phase plan/status pair and [`Measurement.md` §3.5](Measurement.md#35-standing-measurement-lessons) *before* designing a probe. |
| **deciding whether a number may be published** | [`Measurement.md`](Measurement.md), and the answer is usually *not yet*. |
| **checking a claim** | the matching `*.status.md`. Every figure is attached to the run that produced it. |
| **reproducing a measurement** | [`Measurement.md` Appendix A](Measurement.md#appendix-a--reproducing-the-measurements) — every probe's command line, and the traps each has already cost somebody. |
| **doing conformance, host-mode or packaging work** | [`Component.md`](Component.md). |
| **wondering why a technique is not on the plan** | [the optimization catalogue](Roadmap.md#the-optimization-catalogue--the-design-space-this-plan-was-chosen-out-of), which marks twelve as never scoped and nine as inapplicable to an engine that compiles to IL rather than bytecode. |

## Where open work is recorded

- **Start cross-track audit follow-up at
  [`Modernization.md` MOD-M0](Modernization.md).**
  It reconciles the current graph and item state before structural or concurrency work.
- **For the existing IL performance campaign**, read the relevant Phase 0–5 plan and then
  its linked status record. MOD-M0 owns the known plan/status drift; until it closes, the status
  record is the evidence and the plan's stale next action is not an instruction to repeat
  completed work.
- **For assembly and packaging work**, read [`Assemblies.md`](Assemblies.md),
  [`AssemblySplit.md`](AssemblySplit.md), and their status records. Treat the split as
  implemented with validation remaining, not as unstarted.
- **Track two — phases 6–9 — is the JavaScript built-in beyond the numeric seed.**
  Modernization MOD-M9 is item 6-0 and must select exactly one `execution-only`,
  `narrow-runtime-compiler`, or `general-runtime-compiler` JavaScript composition before
  production JavaScript-profile work starts. Broiler.VM core and WebAssembly use their own plan.
- **For JavaScript-local concurrency and Worker acceptance**, use
  [`Concurrency.md`](Concurrency.md) and [`Concurrency.status.md`](Concurrency.status.md).
  Existing compile-ahead and Worker code in the aggregate repository is mapped there as
  implemented subsets, not as proof that modernization phases MOD-M5–MOD-M7 have closed.
- **For conformance and other host capability**, use [`Component.md`](Component.md).

## Conventions

- **Paths are relative to the `Broiler.JS` root.** Paths into the aggregate repository —
  for example `tests/octane/`, `scripts/`, and `.github/workflows/` — cannot be linked from
  a submodule and are written as bare code spans. Historical `patches/` references describe
  a retired handoff ledger; no current aggregate `patches/` directory exists.
- **Section numbering is stable across the split.** §0 and §4 are in `Roadmap.status.md`;
  §1 and §2 are in `Roadmap.md`; §3, §3.5 and Appendix A are in `Measurement.md`;
  Appendix B is in `Roadmap.md`. An existing reference to §3.5 or §4.2a still resolves.
- **A roadmap item stays only while its outcome is open**, and closes only against the
  objective gate in its owning plan. A performance or resource claim additionally passes
  [`Measurement.md`](Measurement.md). Every item has an owner, current evidence, a next
  action, and an objective exit criterion; a checked historical task is not release or
  conformance evidence.
- **Do not duplicate a changing count in a plan document** — a test262 total, a benchmark
  score, a submodule pointer. Name the command that reads it. `Roadmap.md`'s provenance
  bullet is what happens when this rule is not followed: nine consecutive readings found its
  pointer sentence stale.

## History

Consolidated 2026-08-07 in three passes.

**Pass 1 — gather.** Six locations, four of them in the aggregate repository:

| Was | Became |
|---|---|
| `docs/performance-roadmap.md` (aggregate) | `Roadmap.md` |
| `docs/performance/` — 16 files (aggregate) | its parts |
| `docs/manual performance additions.md` (aggregate) | the optimization catalogue, translated from German |
| `tests/octane/roadmap.md` (aggregate) | [`Archive.md`](Archive.md), part two |
| `Broiler.JS/docs/roadmap.md` | [`Component.md`](Component.md) |
| `Broiler.JS/docs/performance.md` | [`Measurement.md`](Measurement.md) |
| `Broiler.JS/docs/performance-roadmap.md` | [`Archive.md`](Archive.md), part one |

**Pass 2 — merge what was the same kind of thing**, 22 files to 11: `status.md`,
`scope-and-metrics.md`, `engine-state.md`, `catalogue.md` and `appendix-b-traceability.md`
into the roadmap; `acceptance.md`, `protocol.md`, `lessons.md` and
`appendix-a-reproducing.md` into `Measurement.md`; the three `item-*.md` files back into
their phases; the two archives into one.

**Pass 3 — split plan from evidence**, which is the layout above. The phase documents
became `Phase-N.md` + `Phase-N.status.md`, and the roadmap's §0 and §4 — both of which are
status rather than plan — moved to `Roadmap.status.md`. **The plan documents are new
writing**; the status documents are the previous prose, moved.

**Why the plan is in the submodule at all.** It used to live one repository above the engine
it directs, on the argument that it spans both — the harness is main-repo, the engine is a
submodule, and a document inside `Broiler.JS` cannot link outward to the harness. That is
true, and it cost nine links. Against it: every item in the plan changes `Broiler.JS`
source, and a plan that cannot be revised in the same commit as the change it describes goes
stale by default. [`Roadmap.md`](Roadmap.md) argues the reversal at the point where it used
to argue the opposite.

`Broiler.JS/docs/{architecture,compliance,agents}/` and `public-api.md` are reference
material, not roadmap, and stay where they are. So do `tests/octane/README.md` and
`tests/octane/benchmarks.md`, which describe the harness and belong beside it.
