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
| **SJL-0** | Clause 8: the revisions and this plan pair, filed on 2026-10-12 in `a69eeca`. Clause 10: the decision-rule files [`docs/evidence/sjl-1-001/decision-rule.md`](../evidence/sjl-1-001/decision-rule.md), [`docs/evidence/sjl-3-001/decision-rule.md`](../evidence/sjl-3-001/decision-rule.md) and [`docs/evidence/sjl-3-001/reference-arms.md`](../evidence/sjl-3-001/reference-arms.md), with the comparison script and the measurement's workloads, in `1410079`, before any code they judge; they hold no figure. Clause 11: [`SharedLibraries.md`](SharedLibraries.md), "The decisions this plan carries". Clause 9: bundle [`docs/evidence/sjl-0-001/`](../evidence/sjl-0-001/README.md), which retains test262 per path, the package contents and nuspecs, an interim public API listing and the pilot's base measurement at base commit `44a214dc`, and names what it excludes | **In progress** | Clauses 8 to 11 are met on retained records and the bundle, and none is accepted: no second holder exists to review them. The programme's clauses in Broiler.VM and in the component repositories are read by the programme's ledger, not here |
| **SJL-1** | Clause 1: the schema, [`conformance/vectors/SCHEMA.md`](../../conformance/vectors/SCHEMA.md), and the validator in `Broiler.JavaScript.Vectors.Tests`, which refuses every negative control in `conformance/vectors/controls/` for its stated reason and any vector whose provenance is engine-observed only, in `966140a`. Clause 2: the number area, every vector's expected value from the exact reference computation `scripts/conformance/numref.py`, in `966140a`; its differences classified at corpus revisions 2 (`9b0f2dd`) and 3 (`593f2c5`). Clause 3 and this repository's half of clause 6: bundle [`docs/evidence/sjl-1-001/`](../evidence/sjl-1-001/README.md), whose README reads the engine-level run on revision 3 against the predeclared rule as met, with one judgement it states (an `abort:` result counted as the engine's), and holds the negative control the runner reported unclassified. The library-level runner runs against an empty library | **In progress** | The home clauses are met on retained records and none is accepted: no second holder exists to review them. Broiler.VM's clauses 4 to 6 are read by the programme's ledger |
| **SJL-2** | This repository's clauses 7 to 10, in `a84a0b4`, merged in `29be101`: the shared folder [`Broiler.ECMAScript/`](../../Broiler.ECMAScript/README.md) with its normative [conventions](../../Broiler.ECMAScript/docs/conventions.md); rules SL1 to SL10 `Active` with witnesses in `Broiler.ECMAScript.Architecture.Tests`; a public API baseline per library and the folder as its own assurance component, both in the [`Shared libraries`](../../.github/workflows/shared-libraries.yml) workflow; the shared version stream (`eng/pack.ps1 -ProjectSet Shared`, the `ecmascript-v` tag prefix, [`Publish shared libraries`](../../.github/workflows/publish-shared.yml)); the probe pre-release `Broiler.ECMAScript.Algorithms` `0.0.1-probe.1`, contract types only, never published; the Native AOT probe. Bundle [`docs/evidence/sjl-2-001/`](../evidence/sjl-2-001/README.md) reads clauses 7 to 10 as met, each rule and both drift controls watched failing on an injection and passing on revert, and the `Shared libraries` workflow's run on `29be101` passing every job, its logs retained *(corrected 2026-10-12; read "reads clauses 7, 9 and 10 as met and clause 8 as met locally")* | **In progress** | Nothing of this repository's clauses is owed *(corrected 2026-10-12; read "Clause 8's CI half: the first run of the `Shared libraries` workflow.")*. No nuget.org reservation of the `Broiler.ECMAScript` prefix is owed: the owner withdrew it on 2026-10-12, revising decision SJL-D-2 *(corrected 2026-10-12; read "Owed by a release owner: the nuget.org reservation of the `Broiler.ECMAScript` prefix, before anything under it is published.")*. None of it is accepted: no second holder exists to review it. Broiler.VM's clauses 1 to 6 are read by the programme's ledger |
| **SJL-3** | This repository's clauses 1 to 7 and its half of clause 14, merged in `4899f9c` (Broiler-Platform/Broiler.JS#1023) *(corrected 2026-10-13, later the same day; read "on branch `claude/busy-lamport-m3yprr` (Broiler-Platform/Broiler.JS#1023), not yet merged")*. The area verdict in [`SharedLibraries.md`](SharedLibraries.md) (`c0ca98c`); the rules confirmed unchanged (`6d9dea6`); the number area in `Broiler.ECMAScript.Algorithms` with the reference arms (`e79cbd0` to `35be06e`), published to nuget.org as `0.1.0-preview.1` by `Publish shared libraries`; the cut-over (`93df3fd`), with every correction in [`Broiler.JS/CHANGELOG.md`](../../Broiler.JS/CHANGELOG.md) and the dispositions in [`docs/public-api.md`](../public-api.md); and the two duplicates the simplification check found, corrected (`b598028`, `ef67693`). Bundle [`docs/evidence/sjl-3-001/`](../evidence/sjl-3-001/README.md) reads clauses 1 to 6 and 14 as met, test262 per path under part A of its rule among them, and clause 7's pristine consumer and package checks as met *(corrected 2026-10-13; read "The base: bundle `sjl-0-001`. The rules: `docs/evidence/sjl-3-001/`")* | **In progress** *(corrected 2026-10-13; read "**Not started**")* | **Clause 7's measurement half is owed.** Part B of its rule was taken on a machine that is not a controlled lane, which the rule voids, so it waits on a controlled lane; that run's arithmetic would fail clause 5 on `Literals`, the first workload a controlled lane should look at ([`measurement/`](../evidence/sjl-3-001/measurement/README.md)). None of it is accepted: no second holder exists to review it. Broiler.VM's clauses 8 to 13 are read by the programme's ledger *(corrected 2026-10-13; read "The algorithms library's number area, after SJL-1 and SJL-2")* |
| **SJL-12** | None. Modernization MOD-M2-3 to MOD-M2-5 are open and are this item's in-place half | **Not started** | The parser's ambient-state census |
| **SJL-4** | Started on 2026-10-13 by the owner's choice, before SJL-3 is met; the programme's ledger records the choice. Nothing of this repository's clauses exists yet *(added 2026-10-13)* | **In progress** | SJL-4.1: the numerics area's sub-area verdicts and its decision-rule files, before any of its code. SJL-4 may not meet its gate before SJL-3 meets SJL-3's |
| **SJL-5** to **SJL-11**, **SJL-17** | None | **Not started** | Each after its predecessors, in the programme's delivery order *(2026-10-13; this row read "**SJL-4** to **SJL-11**, **SJL-17**")* |
| **SJL-13** to **SJL-16** | None | **Not started** | After SJL-12 |

No item is accepted. One person holds every role the programme names, so no item can be
independently confirmed until a second holder exists, and none is claimed.

## Decision records

Every decision of the programme was taken on 2026-10-11. This repository's records of them:

| Decision | Recorded here by |
|---|---|
| SJL-D-1 to SJL-D-6, SJL-D-9, SJL-D-14, SJL-D-16, SJL-D-17, SJL-D-18, SJL-D-23 | [`SharedLibraries.md`](SharedLibraries.md), "The decisions this plan carries", filed 2026-10-12 |
| SJL-D-3's publishing rules | [`docs/publishing.md`](../publishing.md), "Two version streams", and [`docs/public-api.md`](../public-api.md), "The shared libraries' packages", filed at SJL-2.13 in `a84a0b4` |
| SJL-D-25's public-type dispositions | [`docs/public-api.md`](../public-api.md): the number area's, filed at SJL-3.5 in `93df3fd`; each later area's at its cut-over (not yet) *(added 2026-10-13)* |
| SJL-D-12's `Ast` and `Parser` disposition | [`docs/public-api.md`](../public-api.md), at SJL-14.4 (not yet) *(2026-10-13; this row read "SJL-D-12's `Ast` and `Parser` disposition, SJL-D-25's public-type dispositions" and "[`docs/public-api.md`](../public-api.md), at SJL-3.5, each area and SJL-14.4 (not yet)")* |
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
