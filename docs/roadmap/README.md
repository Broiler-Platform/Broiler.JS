# Broiler.JS roadmap

Every open plan for this engine, and the rules that decide when one of them may close.

## The convention: `X.md` is the plan, `X.status.md` is the evidence

Every plan in this directory is split in two, and the split is the point:

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
the AOT-safe path is 301 lines and 20 `double`-only opcodes. That is a *capability* gap, not
a performance one — and separately, phase 6 would create the interpreter frame that phase
4's item 4-3 could not find and had to design around. The full argument is
[`Roadmap.md` § Track two](Roadmap.md#track-two--the-vm-tier-phases-69).

**It is not a speed-up.** Wherever `Reflection.Emit` works, an interpreter is slower than
IL + RyuJIT. Never put a VM number beside an IL number and call it a win.

**One more plan gates track two**, and it serves track one's packaging work at the same
time:

| Subject | Plan | Evidence |
|---|---|---|
| **The assembly restructure** — `Broiler.JS.Base` / `.Core` / `.IL` / `.Bytecode` | [`Assemblies.md`](Assemblies.md) | [`Assemblies.status.md`](Assemblies.status.md) — a census, nothing started |
| **The `ExpressionCompiler` split** — the restructure's first executable piece | [`AssemblySplit.md`](AssemblySplit.md) | [`AssemblySplit.status.md`](AssemblySplit.status.md) — analyzed in full, not started |

**Why it exists:** `Broiler.JavaScript.ExpressionCompiler` — the IL emitter — has **no
project references**, and the AST, property storage, the parser and the runtime all depend
on it. **No subset of the graph runs JavaScript without dynamic code.** The restructure
removes that edge so an application can reference the IL back end, the bytecode back end, or
both — and a bytecode-only application publishes as Native AOT.

Three documents are not part of either track and are not split:

| | |
|---|---|
| [`Measurement.md`](Measurement.md) | **The gate, and §3 of the campaign.** What may be *claimed* — repeatability bands, the RID matrix, bootstrap profiles, the boundary around the experimental execution modes — plus the protocol, the conformance gates, the standing measurement lessons (§3.5) and every probe's command line (Appendix A). **It governs everything above.** It is not split because it is *all* rules: it has no status. |
| [`Component.md`](Component.md) | The engine's non-performance roadmap: closing the supported test262 failure set, expanding host-mode coverage, finishing RegExp backend adoption, and API/package/preview readiness. |
| [`Archive.md`](Archive.md) | **Both** superseded plans — the engine campaign (P0–P3, phases A–F) and the Octane roadmap (phases 0–5) — merged into `Roadmap.md` on 2026-08-01 and **not back-ported**. Kept for their measurements, their defect narratives, and the scope exclusions later overturned. Where an archive and `Roadmap.md` disagree, `Roadmap.md` is current. |

## Where to start

| If you are… | Read |
|---|---|
| **picking up performance work** | [`Roadmap.md`](Roadmap.md) for the sequencing, then the phase's plan document, then [`Measurement.md` §3.5](Measurement.md#35-standing-measurement-lessons) *before* designing a probe. Those lessons exist because the campaign has repeatedly measured the wrong thing in an instructive way. |
| **deciding whether a number may be published** | [`Measurement.md`](Measurement.md), and the answer is usually *not yet*. |
| **checking a claim** | the matching `*.status.md`. Every figure is attached to the run that produced it. |
| **reproducing a measurement** | [`Measurement.md` Appendix A](Measurement.md#appendix-a--reproducing-the-measurements) — every probe's command line, and the traps each has already cost somebody. |
| **doing conformance, host-mode or packaging work** | [`Component.md`](Component.md). |
| **wondering why a technique is not on the plan** | [the optimization catalogue](Roadmap.md#the-optimization-catalogue--the-design-space-this-plan-was-chosen-out-of), which marks twelve as never scoped and nine as inapplicable to an engine that compiles to IL rather than bytecode. |

## What is actually open

Four things, and only one of them is large:

1. **Phase 1's item 1-1**, the deferral half — the front end, and page-load time. **L.**
2. **Phase 3's item 3-1**, the storage half — re-opened as unmeasured against the eight
   suites the census never ran. **XL, bidding for under 2%.**
3. **Phase 4's item 4-5**, the fixed cost of a call — the phase's largest measured target,
   and blocked on a soundness question rather than on effort.
4. **Phase 5's item 7**, the per-call regex envelope — ~2.4 µs and 2 431 B before any
   matching happens, unstarted.

Plus **phase 0's items 0-7 and 0-8**, which are not engineering: they need an idle physical
machine on three RIDs, and until they land nothing above may be *claimed*.

**Track two — phases 6–9 — is open in a different sense: not started, not scheduled, and
several XL.** Two things there are worth doing now, and neither is large:

- **[`AssemblySplit.md`](AssemblySplit.md)** — the `ExpressionCompiler` model/emitter split,
  planned end to end and **fully analyzed**. It is the precondition for everything in track
  two, it is where four items `Component.md` owes get settled, and it changes no behaviour:
  the diff is `.csproj` files, file moves, and two file cuts. Afterwards an AOT gate can go
  green on the *existing* 20 opcodes, proving the packaging before any VM work starts.
- **Item 6-0**, an S that asks whether a bytecode VM is needed at all — it may cancel the
  whole track, and its answer changes how item 1-1 should be finished.

## Conventions

- **Paths are relative to the `Broiler.JS` root.** Paths into the aggregate repository —
  `tests/octane/`, `scripts/`, `patches/`, `.github/workflows/` — cannot be linked from a
  submodule and are written as bare code spans. There are nine.
- **Section numbering is stable across the split.** §0 and §4 are in `Roadmap.status.md`;
  §1 and §2 are in `Roadmap.md`; §3, §3.5 and Appendix A are in `Measurement.md`;
  Appendix B is in `Roadmap.md`. An existing reference to §3.5 or §4.2a still resolves.
- **A roadmap item stays only while its outcome is open**, and closes only against the gate
  in [`Measurement.md`](Measurement.md): an owner, current evidence, a next action, and an
  objective exit criterion. A checked historical task is not release or conformance
  evidence.
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
