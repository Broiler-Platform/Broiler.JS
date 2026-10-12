# Shared JavaScript libraries — status and evidence

The record of what exists for Broiler.JS's half of the shared JavaScript libraries programme. The
plan to act from is [`SharedLibraries.md`](SharedLibraries.md). The programme's own ledger, which
records every milestone across all four repositories, is
`Broiler.VM/docs/javascript-shared-libraries/status.md`; a row there may link here as observed state
of this repository, and neither record advances the other.

Created 2026-10-12, at milestone SJL-0.

## State

| Item | Implemented evidence | State | Next decisive action |
|---|---|---|---|
| **SJL-0** | The revisions and this plan pair, filed on 2026-10-12; the decision-rule files `docs/evidence/sjl-1-001/decision-rule.md`, `docs/evidence/sjl-3-001/decision-rule.md` and `docs/evidence/sjl-3-001/reference-arms.md`; bundle [`docs/evidence/sjl-0-001/`](../evidence/sjl-0-001/README.md) | **In progress** | See the bundle's README for what it retains and what it excludes |
| **SJL-1** | None | **Not started** | The vector schema and validator |
| **SJL-2** | None | **Not started** | The shared folder and its conventions |
| **SJL-3** | The base: bundle `sjl-0-001`. The rules: `docs/evidence/sjl-3-001/` | **Not started** | The algorithms library's number area, after SJL-1 and SJL-2 |
| **SJL-12** | None. Modernization MOD-M2-3 to MOD-M2-5 are open and are this item's in-place half | **Not started** | The parser's ambient-state census |
| **SJL-4** to **SJL-11**, **SJL-17** | None | **Not started** | Each after its predecessors, in the programme's delivery order |
| **SJL-13** to **SJL-16** | None | **Not started** | After SJL-12 |

No item is accepted. One person holds every role the programme names, so no item can be
independently confirmed until a second holder exists, and none is claimed.

## Decision records

Every decision of the programme was taken on 2026-10-11. This repository's records of them:

| Decision | Recorded here by |
|---|---|
| SJL-D-1, SJL-D-2, SJL-D-3, SJL-D-5, SJL-D-6, SJL-D-9, SJL-D-14, SJL-D-16, SJL-D-17, SJL-D-18, SJL-D-23 | [`SharedLibraries.md`](SharedLibraries.md), "The decisions this plan carries", filed 2026-10-12 |
| SJL-D-3's publishing rules | `docs/publishing.md`, at SJL-2.13 (not yet) |
| SJL-D-12's `Ast` and `Parser` disposition, SJL-D-25's public-type dispositions | [`docs/public-api.md`](../public-api.md), at SJL-3.5, each area and SJL-14.4 (not yet) |
| SJL-D-15, SJL-D-19 to SJL-D-22, SJL-D-24 | The area records of SJL-4, SJL-6, SJL-8 and SJL-9 (not yet) |

## SJL-0 evidence map

### Records revised on 2026-10-12

Each carries a dated revision that quotes what it replaced:

- [`docs/architecture/dependencies.md`](../architecture/dependencies.md): no edge between the engines;
  the shared libraries here are consumed by Broiler.VM and reference neither engine; the
  `FrontEnd/Semantics` row becomes the shared frontend; the nested-component sentence corrected.
- [`docs/public-api.md`](../public-api.md): "No Broiler.VM integration is planned for this component"
  revised.
- [`README.md`](README.md): "The bytecode/VM plan is retired from this component" stays; a pointer to
  this plan pair is added.
- [`Assemblies.md`](Assemblies.md), [`Roadmap.md`](Roadmap.md),
  [`ModernizationDelivery.md`](ModernizationDelivery.md), and MOD-M2-7 in
  [`Modernization.md`](Modernization.md): the residual text planning a Broiler.VM JavaScript
  integration shell or composition is withdrawn, with a pointer here.
- [`Modernization.md`](Modernization.md) invariant 3: extended to the Broiler.VM consumer; MOD-M2-3 to
  MOD-M2-5 cross-referenced to SJL-12.
- [`docs/architecture/builtins-vs-clr-types.md`](../architecture/builtins-vs-clr-types.md) §6: the stale
  "vendored `Broiler.DateTime`" corrected.

### Baselines

Bundle [`docs/evidence/sjl-0-001/`](../evidence/sjl-0-001/README.md), the first bundle in this
repository's `docs/evidence/`. Its README names its base commit, what it retains and what it excludes.
