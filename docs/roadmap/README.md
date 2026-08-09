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
| **The performance campaign** | [`Roadmap.md`](Roadmap.md) — scope, metrics, sequencing, the optimization catalogue, non-goals, traceability | [`Roadmap.status.md`](Roadmap.status.md) — §0 status in full, §4 where the engine stands |
| **Phase 0** — establish the baseline | [`Phase-0.md`](Phase-0.md) | [`Phase-0.status.md`](Phase-0.status.md) |
| **Phase 1** — the front end | [`Phase-1.md`](Phase-1.md) | [`Phase-1.status.md`](Phase-1.status.md) |
| **Phase 2** — the call and property paths | [`Phase-2.md`](Phase-2.md) | [`Phase-2.status.md`](Phase-2.status.md) |
| **Phase 3** — value representation | [`Phase-3.md`](Phase-3.md) | [`Phase-3.status.md`](Phase-3.status.md) |
| **Phase 4** — speculation | [`Phase-4.md`](Phase-4.md) | [`Phase-4.status.md`](Phase-4.status.md) |
| **Phase 5** — RegExp | [`Phase-5.md`](Phase-5.md) | [`Phase-5.status.md`](Phase-5.status.md) |

Those are **track one** — the IL execution path, where all the evidence is. **Track two**
is the VM tier: the optimization catalogue's VM 1.0 → 4.0 staging, written as a plan.
**None of it is started or scheduled**, and each phase is gated on an entry measurement
that may cancel it:

| Subject | Plan | Evidence |
|---|---|---|
| **Phase 6** — VM 1.0: a correct bytecode interpreter | [`Phase-6.md`](Phase-6.md) | [`Phase-6.status.md`](Phase-6.status.md) — nothing measured |
| **Phase 7** — VM 2.0: respectable performance | [`Phase-7.md`](Phase-7.md) | [`Phase-7.status.md`](Phase-7.status.md) — nothing measured |
| **Phase 8** — VM 3.0: a highly optimized interpreter | [`Phase-8.md`](Phase-8.md) | [`Phase-8.status.md`](Phase-8.status.md) — nothing measured |
| **Phase 9** — VM 4.0: an adaptive two-tier engine | [`Phase-9.md`](Phase-9.md) | [`Phase-9.status.md`](Phase-9.status.md) — nothing measured |

**Why track two exists at all:** Broiler.JS has **no general JavaScript execution path on a
platform that forbids `System.Reflection.Emit`**. The compiler back end is an IL writer;
the current portable path is a deliberately limited numeric subset. That is a *capability*
gap, not a performance one — and separately, phase 6 would create the interpreter frame
that phase 4's item 4-3 could not find and had to design around. The full argument is
[`Roadmap.md` § Track two](Roadmap.md#track-two--the-vm-tier-phases-69).

**It is not a speed-up.** Wherever `Reflection.Emit` works, an interpreter is slower than
IL + RyuJIT. Never put a VM number beside an IL number and call it a win.

**One more plan gates track two**, and it serves track one's packaging work at the same
time:

| Subject | Plan | Evidence |
|---|---|---|
| **The assembly restructure** — backend-neutral foundations / `.IL` / `.Bytecode` / optional packages | [`Assemblies.md`](Assemblies.md) | [`Assemblies.status.md`](Assemblies.status.md) — initial graph work implemented; target-graph and validation work remains |
| **The `ExpressionCompiler` split** — the restructure's first executable piece | [`AssemblySplit.md`](AssemblySplit.md) | [`AssemblySplit.status.md`](AssemblySplit.status.md) — implementation landed; final validation remains open |

**Why it exists:** the model/emitter split removed the front-end consumers' direct dependency
on the IL emitter. The remaining restructure must prove an acyclic backend-neutral semantic
front end, isolate every runtime Emit dependency in the IL profile, preserve consumers, and
make a bytecode-only Native AOT profile a publish-and-run property.

Cross-cutting and reference documents sit above or beside the two tracks and are not split.
`Modernization.md` is an orchestration overlay rather than an independent evidence ledger;
its work records evidence in the owning plan/status pair:

| | |
|---|---|
| [`Modernization.md`](Modernization.md) | **The cross-track execution roadmap.** It orders the audit cleanup, trustworthy baselines, an achievable assembly graph, IL/AOT isolation, package decomposition, bounded compile-ahead, independent-context safety, Workers, profile-led optimization, and the bytecode-VM decision. It owns dependencies and program gates; changing evidence remains in the linked `*.status.md` records. |
| [`Measurement.md`](Measurement.md) | **The gate, and §3 of the campaign.** What may be *claimed* — repeatability bands, the RID matrix, bootstrap profiles, the boundary around the experimental execution modes — plus the protocol, the conformance gates, the standing measurement lessons (§3.5) and every probe's command line (Appendix A). **It governs everything above.** It is not split because it is *all* rules: it has no status. |
| [`Component.md`](Component.md) | The engine's non-performance roadmap: closing the supported test262 failure set, expanding host-mode coverage, finishing RegExp backend adoption, and API/package/preview readiness. |
| [`Archive.md`](Archive.md) | **Both** superseded plans — the engine campaign (P0–P3, phases A–F) and the Octane roadmap (phases 0–5) — merged into `Roadmap.md` on 2026-08-01 and **not back-ported**. Kept for their measurements, their defect narratives, and the scope exclusions later overturned. Where an archive and `Roadmap.md` disagree, `Roadmap.md` is current. |

## Where to start

| If you are… | Read |
|---|---|
| **planning work that crosses performance, assemblies, AOT, or concurrency** | [`Modernization.md`](Modernization.md) for ordering and stop/go gates, then the linked owning plan and its status record. |
| **picking up performance work** | [`Roadmap.md`](Roadmap.md) for the sequencing, then the phase's plan document, then [`Measurement.md` §3.5](Measurement.md#35-standing-measurement-lessons) *before* designing a probe. Those lessons exist because the campaign has repeatedly measured the wrong thing in an instructive way. |
| **deciding whether a number may be published** | [`Measurement.md`](Measurement.md), and the answer is usually *not yet*. |
| **checking a claim** | the matching `*.status.md`. Every figure is attached to the run that produced it. |
| **reproducing a measurement** | [`Measurement.md` Appendix A](Measurement.md#appendix-a--reproducing-the-measurements) — every probe's command line, and the traps each has already cost somebody. |
| **doing conformance, host-mode or packaging work** | [`Component.md`](Component.md). |
| **wondering why a technique is not on the plan** | [the optimization catalogue](Roadmap.md#the-optimization-catalogue--the-design-space-this-plan-was-chosen-out-of), which marks twelve as never scoped and nine as inapplicable to an engine that compiles to IL rather than bytecode. |

## Where open work is recorded

- **Start cross-track audit follow-up at
  [`Modernization.md` M0](Modernization.md).**
  It reconciles the current graph and item state before structural or concurrency work.
- **For the existing IL performance campaign**, read the relevant Phase 0–5 plan and then
  its linked status record. M0 owns the known plan/status drift; until it closes, the status
  record is the evidence and the plan's stale next action is not an instruction to repeat
  completed work.
- **For assembly and packaging work**, read [`Assemblies.md`](Assemblies.md),
  [`AssemblySplit.md`](AssemblySplit.md), and their status records. Treat the split as
  implemented with validation remaining, not as unstarted.
- **Track two — phases 6–9 — remains a capability proposal.** Item 6-0 and modernization
  phase M9 must produce a terminal go, narrow-go, or no-go before production VM work starts.
- **For conformance and host capability**, use [`Component.md`](Component.md); M0 assigns a
  dedicated plan/status pair before JavaScript-local concurrency or Worker implementation
  begins.

## Conventions

- **Paths are relative to the `Broiler.JS` root.** Paths into the aggregate repository —
  `tests/octane/`, `scripts/`, `patches/`, `.github/workflows/` — cannot be linked from a
  submodule and are written as bare code spans. There are nine.
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
